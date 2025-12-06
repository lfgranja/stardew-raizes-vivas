using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Implementation for handling reserved Windows filenames to prevent conflicts with system files.
    /// This implementation follows the Dependency Inversion Principle by depending on abstractions.
    /// </summary>
    /// <remarks>
    /// Conversion Rules:
    /// 1. Diacritics: Removed from Latin and Greek letters, preserved for other scripts
    /// 2. Security Confusables: Cyrillic lookalikes are converted to Latin equivalents unless they appear in legitimate non-Latin script contexts
    /// 3. Zero-width and bidirectional characters: Removed completely for security
    /// 4. Control characters: Removed completely
    /// 5. Precomposed characters: Simplified to base forms (e.g., 'ø' → 'o')
    /// </remarks>
    public class ReservedNameHandler : IReservedNameHandler
    {
        private static readonly HashSet<string> ReservedWindowsFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        private readonly IUnicodeNormalizationService _unicodeNormalizationService;

        public ReservedNameHandler(IUnicodeNormalizationService unicodeNormalizationService)
        {
            _unicodeNormalizationService = unicodeNormalizationService ?? throw new ArgumentNullException(nameof(unicodeNormalizationService));
        }

        /// <summary>
        /// Handles reserved Windows filenames by appending an underscore to the base name if necessary.
        /// Uses Path.GetFileName and Path.GetDirectoryName for regular paths, with special handling for UNC paths.
        /// For directory paths ending with separators, checks if the last directory component is a reserved name.
        /// </summary>
        /// <param name="filename">The filename or path to check for reserved names.</param>
        /// <returns>A filename or path with reserved names handled appropriately.</returns>
        public string? Handle(string? filename)
        {
            if (string.IsNullOrEmpty(filename)) return filename;

            // First, normalize Unicode characters to handle diacritics and homoglyphs
            string? normalizedInput = _unicodeNormalizationService?.Normalize(filename);
            
            // Security fix: Add null check for normalizedInput to prevent validation bypass
            if (normalizedInput == null)
                throw new ArgumentException("Filename normalization returned null, validation cannot proceed", nameof(filename));

            // Check if this is a UNC path (starts with \\ or //)
            if (IsUncPath(normalizedInput))
            {
                // For UNC paths, we need to manually extract the filename and directory parts
                // since Path.GetFileName and Path.GetDirectoryName don't handle UNC paths consistently across platforms
                string fileName = ExtractFileNameFromUncPath(normalizedInput);
                
                // If extraction returns empty (for directory paths ending with separator), 
                // check if the last directory component is a reserved name
                if (string.IsNullOrEmpty(fileName))
                {
                    // This is a directory path ending with a separator
                    // Extract the last directory component before the separator and check if it's reserved
                    string uncDirectoryPath = ExtractDirectoryPathFromUncPath(normalizedInput) ?? string.Empty;
                    if (!string.IsNullOrEmpty(uncDirectoryPath))
                    {
                        // Extract the last directory component from the directory path
                        string lastDirComponent = ExtractLastPathComponent(uncDirectoryPath, GetUncPathSeparator(uncDirectoryPath));
                        if (!string.IsNullOrEmpty(lastDirComponent) && IsReservedName(lastDirComponent))
                        {
                            // If the last directory component is reserved, modify the directory path
                            string modifiedDirPath = ModifyLastPathComponent(uncDirectoryPath, lastDirComponent, GetUncPathSeparator(uncDirectoryPath));
                            // Return the modified path with the separator still at the end
                            return modifiedDirPath + normalizedInput.Substring(uncDirectoryPath.Length);
                        }
                    }
                    // If no reserved name is found in the last directory component, return original
                    return filename;
                }

                // For UNC paths, extract directory path separately
                string uncDirectoryPath2 = ExtractDirectoryPathFromUncPath(normalizedInput) ?? string.Empty;

                // Process just the filename component for reserved names
                string? processedFileName = ProcessFileNameInternal(fileName);

                // If no change was made to the filename component, return the original path
                if (processedFileName == fileName)
                    return filename;

                // Reconstruct the UNC path with the processed filename component
                // For UNC paths, construct the path manually to preserve the UNC format
                if (!string.IsNullOrEmpty(uncDirectoryPath2))
                {
                    // Always use backslash for path separator in UNC paths
                    return uncDirectoryPath2 + "\\" + processedFileName;
                }

                return processedFileName;
            }
            else
            {
                // For regular paths, extract the filename component using Path methods
                string fileName = Path.GetFileName(normalizedInput);

                // If Path.GetFileName returns empty (for directory paths ending with separator), 
                // check if the last directory component is a reserved name
                if (string.IsNullOrEmpty(fileName))
                {
                    // This is a directory path ending with a separator
                    // Extract the last directory component before the separator and check if it's reserved
                    string regDirectoryPath = Path.GetDirectoryName(normalizedInput) ?? string.Empty;
                    if (!string.IsNullOrEmpty(regDirectoryPath))
                    {
                        // Extract the last directory component from the directory path
                        string lastDirComponent = ExtractLastPathComponent(regDirectoryPath, Path.DirectorySeparatorChar);
                        if (!string.IsNullOrEmpty(lastDirComponent) && IsReservedName(lastDirComponent))
                        {
                            // If the last directory component is reserved, modify the directory path
                            string modifiedDirPath = ModifyLastPathComponent(regDirectoryPath, lastDirComponent, Path.DirectorySeparatorChar);
                            // Return the modified path with the separator still at the end
                            return modifiedDirPath + normalizedInput.Substring(regDirectoryPath.Length);
                        }
                    }
                    // If no reserved name is found in the last directory component, return original
                    return filename;
                }

                // Extract the directory path separately
                string regDirectoryPath2 = Path.GetDirectoryName(normalizedInput) ?? string.Empty;

                // Process just the filename component for reserved names
                string? processedFileName = ProcessFileNameInternal(fileName);

                // If no change was made to the filename component, return the original path
                if (processedFileName == fileName)
                    return filename;

                // Reconstruct the full path with the processed filename component
                if (!string.IsNullOrEmpty(regDirectoryPath2))
                {
                    return Path.Combine(regDirectoryPath2, processedFileName);
                }

                return processedFileName;
            }
        }

        /// <summary>
        /// Extracts the last path component from a directory path
        /// </summary>
        /// <param name="path">The directory path</param>
        /// <param name="separator">The path separator to use</param>
        /// <returns>The last path component</returns>
        private static string ExtractLastPathComponent(string path, char separator)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            
            // Find the last separator in the path
            int lastSeparatorIndex = -1;
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == separator)
                {
                    lastSeparatorIndex = i;
                    break;
                }
            }
            
            if (lastSeparatorIndex == -1)
            {
                // No separator found, the entire path is the component
                return path;
            }
            else if (lastSeparatorIndex == path.Length - 1)
            {
                // Path ends with separator, find the previous separator
                for (int i = lastSeparatorIndex - 1; i >= 0; i--)
                {
                    if (path[i] == separator)
                    {
                        // Extract the component between the two separators
                        return path.Substring(i + 1, lastSeparatorIndex - i - 1);
                    }
                }
                // If only one separator at the beginning, return what's after it
                return path.Substring(lastSeparatorIndex + 1);
            }
            else
            {
                // Return the component after the last separator
                return path.Substring(lastSeparatorIndex + 1);
            }
        }

        /// <summary>
        /// Modifies the last path component by adding an underscore to handle reserved names
        /// </summary>
        /// <param name="path">The original path</param>
        /// <param name="lastComponent">The last component to modify</param>
        /// <param name="separator">The path separator to use</param>
        /// <returns>The modified path with the last component changed</returns>
        private string ModifyLastPathComponent(string path, string lastComponent, char separator)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(lastComponent)) return path;
            
            // Find the last occurrence of the last component in the path
            // We need to replace the last component with lastComponent + "_"
            string modifiedComponent = ProcessFileNameInternal(lastComponent);
            
            // Find the last occurrence of the component in the path
            int lastSeparatorIndex = -1;
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == separator)
                {
                    lastSeparatorIndex = i;
                    break;
                }
            }
            
            if (lastSeparatorIndex == -1)
            {
                // No separator found, the entire path is the component
                return modifiedComponent;
            }
            else
            {
                // Replace the component after the last separator
                string dirBeforeLastComponent = path.Substring(0, lastSeparatorIndex + 1);
                return dirBeforeLastComponent + modifiedComponent;
            }
        }

        /// <summary>
        /// Gets the appropriate path separator for UNC paths
        /// </summary>
        /// <param name="uncPath">The UNC path</param>
        /// <returns>The path separator character</returns>
        private static char GetUncPathSeparator(string uncPath)
        {
            if (uncPath.Length >= 2 && uncPath[0] == '\\' && uncPath[1] == '\\')
            {
                return '\\';
            }
            else if (uncPath.Length >= 2 && uncPath[0] == '/' && uncPath[1] == '/')
            {
                return '/';
            }
            // Default to backslash for UNC paths
            return '\\';
        }

        /// <summary>
        /// Extracts the filename component from a UNC path.
        /// Path.GetFileName doesn't work reliably with UNC paths using backslashes on all platforms.
        /// </summary>
        /// <param name="uncPath">The UNC path to extract the filename from</param>
        /// <returns>The filename component of the UNC path</returns>
        private static string ExtractFileNameFromUncPath(string uncPath)
        {
            // Find the last path separator after the UNC prefix (\\server\share\...)
            // The UNC prefix is at least 2 characters (\\), then there's server and share
            int uncPrefixEnd = -1;
            
            // Skip the initial UNC markers (\\ or //)
            if (uncPath.Length >= 2 && ((uncPath[0] == '\\' && uncPath[1] == '\\') || (uncPath[0] == '/' && uncPath[1] == '/')))
            {
                uncPrefixEnd = 2;
                
                // Find the end of the server name (first separator after \\)
                int serverEnd = -1;
                char separator = uncPath[0]; // Use the same separator as the UNC prefix
                
                for (int i = uncPrefixEnd; i < uncPath.Length; i++)
                {
                    if (uncPath[i] == separator)
                    {
                        serverEnd = i;
                        break;
                    }
                }
                
                if (serverEnd != -1)
                {
                    // Find the end of the share name (second separator after \\)
                    for (int i = serverEnd + 1; i < uncPath.Length; i++)
                    {
                        if (uncPath[i] == separator)
                        {
                            // The filename starts after this separator
                            int fileNameStart = i + 1;
                            if (fileNameStart < uncPath.Length)
                            {
                                return uncPath.Substring(fileNameStart);
                            }
                            break;
                        }
                    }
                }
            }
            
            // If we can't properly parse it as a UNC path, return the original
            return Path.GetFileName(uncPath);
        }

        /// <summary>
        /// Extracts the directory path component from a UNC path.
        /// </summary>
        /// <param name="uncPath">The UNC path to extract the directory path from</param>
        /// <returns>The directory path component of the UNC path</returns>
        private static string? ExtractDirectoryPathFromUncPath(string uncPath)
        {
            // Find the last path separator after the UNC prefix (\\server\share\...)
            // The UNC prefix is at least 2 characters (\\), then there's server and share
            int uncPrefixEnd = -1;
            
            // Skip the initial UNC markers (\\ or //)
            if (uncPath.Length >= 2 && ((uncPath[0] == '\\' && uncPath[1] == '\\') || (uncPath[0] == '/' && uncPath[1] == '/')))
            {
                uncPrefixEnd = 2;
                
                // Find the end of the server name (first separator after \\)
                int serverEnd = -1;
                char separator = uncPath[0]; // Use the same separator as the UNC prefix
                
                for (int i = uncPrefixEnd; i < uncPath.Length; i++)
                {
                    if (uncPath[i] == separator)
                    {
                        serverEnd = i;
                        break;
                    }
                }
                
                if (serverEnd != -1)
                {
                    // Find the end of the share name (second separator after \\)
                    for (int i = serverEnd + 1; i < uncPath.Length; i++)
                    {
                        if (uncPath[i] == separator)
                        {
                            // The directory path ends at this separator
                            int dirEnd = i;
                            if (dirEnd < uncPath.Length)
                            {
                                return uncPath.Substring(0, dirEnd);
                            }
                            break;
                        }
                    }
                }
            }
            
            // If we can't properly parse it as a UNC path, return what Path.GetDirectoryName returns
            return Path.GetDirectoryName(uncPath);
        }

        /// <summary>
        /// Process a filename for reserved name handling
        /// </summary>
        /// <param name="filename">The filename to process</param>
        /// <returns>A filename with reserved names handled appropriately</returns>
        private string ProcessFileNameInternal(string filename)
        {
            // Check if the entire filename consists entirely of insignificant characters (dots, spaces, tabs)
            // This handles cases like " . " where the entire name is insignificant
            string trimmedAll = filename.Trim('.', ' ', '\t');
            if (string.IsNullOrEmpty(trimmedAll))
            {
                // Replace fully insignificant names with a safe placeholder
                return "_";
            }

            // Split the filename to separate the base name from the extension(s)
            // For "COM1.tar.gz", we want to identify "COM1" as the base name and ".tar.gz" as extensions
            // For "COM1.txt", we want to identify "COM1" as the base name and ".txt" as extension
            string baseName = filename;
            string extensionPart = "";

            // Find the first dot that indicates the start of extensions
            // This is more complex than just finding the last dot because we need to handle multiple extensions properly
            int firstExtensionIndex = FindFirstExtensionIndex(filename);
            
            if (firstExtensionIndex != -1)
            {
                baseName = filename.Substring(0, firstExtensionIndex);
                extensionPart = filename.Substring(firstExtensionIndex); // Include the dot in the extension
            }

            // Check if the base name part is a reserved Windows filename
            if (IsReservedName(baseName))
            {
                // If the base name is reserved, add an underscore to make it safe
                // But first, remove any trailing insignificant characters from the extension part
                // to prevent issues like "CON_..." where the trailing dots are still there
                string sanitizedExtension = SanitizeTrailingInsignificantChars(extensionPart);
                
                // Special handling for cases where the base name is reserved due to trailing insignificant characters
                // For example: "CON   " should become "CON_" (not "CON   _")
                if (baseName.Trim('.', ' ', '\t') != baseName)
                {
                    // The base name contains trailing insignificant characters
                    // Extract the significant part and add underscore to that
                    string significantPart = baseName.TrimEnd('.', ' ', '\t');
                    return significantPart + "_" + sanitizedExtension;
                }
                
                // Return the reserved base name with underscore, plus sanitized extension part
                return baseName + "_" + sanitizedExtension;
            }
            
            // Check if the base name part (after trimming insignificant characters) is a reserved Windows filename
            // This handles cases like " CON " where we need to check if "CON" is reserved
            string baseNameTrimmed = baseName.Trim('.', ' ', '\t');
            if (IsReservedName(baseNameTrimmed))
            {
                // If the trimmed base name is reserved, we need to preserve the original format
                // but add an underscore to the reserved part
                // For example: " CON " should become " CON_"
                // For example: "CON   " should become "CON_" (not "CON   _")
                
                // Find the start and end positions of the trimmed part within the original baseName
                int startIndex = FindStartIndexAfterInsignificantChars(baseName);
                int endIndex = FindEndIndexBeforeInsignificantChars(baseName);
                
                // Extract the leading insignificant characters, the core name, and trailing insignificant characters
                string leadingChars = startIndex > 0 ? baseName.Substring(0, startIndex) : "";
                string coreName = baseName.Substring(startIndex, endIndex - startIndex + 1);
                
                // Add underscore to the core reserved name
                string modifiedCore = coreName + "_";
                
                // Combine everything back together with the extension
                // Don't include trailing insignificant characters after the core name
                string sanitizedExtensionForContext = SanitizeTrailingInsignificantChars(extensionPart); 
                return leadingChars + modifiedCore + sanitizedExtensionForContext;
            }
            
            // If not reserved, check if the name part is fully insignificant (consists only of dots, spaces, tabs)
            string trimmedForInsignificantCheck = baseName.Trim('.', ' ', '\t');
            if (string.IsNullOrEmpty(trimmedForInsignificantCheck))
            {
                // Replace fully insignificant names with a safe placeholder
                string sanitizedExtension = SanitizeTrailingInsignificantChars(extensionPart);
                return "_" + sanitizedExtension;
            }
            
            // If not reserved, return the original filename
            return filename;
        }

        /// <summary>
        /// Finds the start index after insignificant characters
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <returns>The index of the first non-insignificant character</returns>
        private static int FindStartIndexAfterInsignificantChars(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c != '.' && c != ' ' && c != '\t')
                    return i;
            }
            return str.Length; // If all characters are insignificant
        }

        /// <summary>
        /// Finds the end index before insignificant characters
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <returns>The index of the last non-insignificant character</returns>
        private static int FindEndIndexBeforeInsignificantChars(string str)
        {
            for (int i = str.Length - 1; i >= 0; i--)
            {
                char c = str[i];
                if (c != '.' && c != ' ' && c != '\t')
                    return i;
            }
            return -1; // If all characters are insignificant
        }

        /// <summary>
        /// Sanitizes trailing insignificant characters (dots, spaces, tabs) from a string
        /// </summary>
        /// <param name="input">The input string to sanitize</param>
        /// <returns>The sanitized string with trailing insignificant characters removed</returns>
        private static string SanitizeTrailingInsignificantChars(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            // Remove trailing insignificant characters (dots, spaces, tabs)
            return input.TrimEnd('.', ' ', '\t');
        }

        /// <summary>
        /// Determines the index where the extension starts in a filename.
        /// For "COM1.tar.gz", this should return the position of the first dot (after "COM1").
        /// For "COM1.txt", this should return the position of the dot (after "COM1").
        /// </summary>
        /// <param name="filename">The filename to analyze</param>
        /// <returns>The index where the extension starts, or -1 if no extension found</returns>
        private static int FindFirstExtensionIndex(string filename)
        {
            // Look for the first dot that indicates the start of an extension
            // This should be after the actual base name, not in the middle of a multipart extension
            // For "COM1.tar.gz", we want to find the first dot after "COM1"
            // For "COM1.txt", we want to find the dot after "COM1"
            
            // Check if the filename starts with a dot (hidden file) - special case
            if (filename.StartsWith("."))
            {
                // Hidden files like ".bashrc" - the extension starts after the first dot
                int firstDotAfterInitial = filename.IndexOf('.', 1);
                if (firstDotAfterInitial > 0)
                {
                    return firstDotAfterInitial;
                }
                return -1; // No extension in a simple hidden file
            }
            
            // For regular filenames, find the first dot that separates the base name from extensions
            // The challenge is to distinguish between a reserved name with a multi-part extension
            // vs. a non-reserved name with a reserved-looking extension part
            for (int i = 0; i < filename.Length; i++)
            {
                if (filename[i] == '.')
                {
                    // Extract the part before this dot to check if it's a reserved name
                    string potentialBaseName = filename.Substring(0, i);
                    
                    // If the potential base name is a reserved name, then this dot marks the start of extensions
                    if (ReservedWindowsFileNames.Contains(potentialBaseName, StringComparer.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                    
                    // Also check if the potential base name starts with a reserved name followed by non-alphanumeric characters
                    // For example, if filename is "COM1.tar.gz", potentialBaseName is "COM1" which is reserved
                    foreach (string reservedName in ReservedWindowsFileNames)
                    {
                        if (potentialBaseName.StartsWith(reservedName, StringComparison.OrdinalIgnoreCase))
                        {
                            int reservedNameLength = reservedName.Length;
                            if (reservedNameLength < potentialBaseName.Length)
                            {
                                // Check if what follows is not alphanumeric (meaning it's not just a longer non-reserved name)
                                char nextChar = potentialBaseName[reservedNameLength];
                                if (!char.IsLetterOrDigit(nextChar))
                                {
                                    // The potential base name contains a reserved name followed by non-alphanumeric chars
                                    // So this dot marks the start of extensions
                                    return i;
                                }
                            }
                            else if (reservedNameLength == potentialBaseName.Length)
                            {
                                // Exact match with reserved name
                                return i;
                            }
                        }
                    }
                }
            }
            
            // No extension found that follows a reserved name pattern
            return -1;
        }

        /// <summary>
        /// Checks if a name is a reserved Windows filename.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if the name is reserved, false otherwise.</returns>
        private static bool IsReservedName(string name)
        {
            // First check if the entire name is a reserved name
            if (ReservedWindowsFileNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                return true;

            // Then check if the name starts with a reserved name followed by non-alphanumeric characters
            // For example, "COM1.txt" - "COM1" is reserved, followed by ".txt"
            foreach (string reservedName in ReservedWindowsFileNames)
            {
                if (name.StartsWith(reservedName, StringComparison.OrdinalIgnoreCase))
                {
                    int reservedNameLength = reservedName.Length;
                    if (reservedNameLength < name.Length)
                    {
                        // Check if what follows is not alphanumeric (meaning it's not just a longer non-reserved name)
                        char nextChar = name[reservedNameLength];
                        if (!char.IsLetterOrDigit(nextChar))
                        {
                            // This is a reserved name with additional characters (like extensions), so it's reserved
                            return true;
                        }
                    }
                    else if (reservedNameLength == name.Length)
                    {
                        // Exact match
                        return true;
                    }
                }
            }

            return false;
        }
        
        /// <summary>
        /// Checks if a path is a UNC path (starts with \\ or //)
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path is a UNC path</returns>
        private static bool IsUncPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length < 2)
                return false;

            return (path[0] == '\\' && path[1] == '\\') ||
                   (path[0] == '/' && path[1] == '/');
        }
    }
}
