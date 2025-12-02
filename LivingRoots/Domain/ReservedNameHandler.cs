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
    /// 
    /// IMPROVEMENTS SUMMARY:
    /// - Simplified UNC path handling by using System.Uri for cross-platform compatibility
    /// - Reduced complex manual path parsing while still ensuring cross-platform compatibility
    /// - Maintained security by ensuring proper filename component processing
    /// </summary>
    public class ReservedNameHandler : IReservedNameHandler
    {
        private readonly IUnicodeNormalizationService _unicodeNormalizationService;
        private static readonly HashSet<string> ReservedWindowsFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public ReservedNameHandler(IUnicodeNormalizationService unicodeNormalizationService)
        {
            _unicodeNormalizationService = unicodeNormalizationService ?? throw new ArgumentNullException(nameof(unicodeNormalizationService));
        }

        /// <summary>
        /// Handles reserved Windows filenames by appending an underscore to base name if necessary.
        /// Uses System.Uri for proper UNC path handling and cross-platform compatibility.
        /// </summary>
        /// <param name="filename">The filename to check for reserved names.</param>
        /// <returns>A filename with reserved names handled appropriately.</returns>
        public string? Handle(string? filename)
        {
            if (string.IsNullOrEmpty(filename)) return filename;

            // For UNC paths, we'll use System.Uri to properly extract the filename part
            if (IsUncPath(filename))
            {
                // Use Uri to properly handle UNC paths
                if (TryGetFileNameFromUncPath(filename, out string? fileName))
                {
                    string? processedFileName = ProcessFileName(fileName);
                    
                    if (processedFileName == fileName)
                    {
                        // No change needed, return original
                        return filename;
                    }
                    else
                    {
                        // Replace the filename part in the original path
                        return ReplaceFileNameInPath(filename, fileName, processedFileName);
                    }
                }
                else
                {
                    // If we can't extract the filename from UNC path, process the whole path as filename
                    string? processedFileName = ProcessFileName(filename);
                    if (processedFileName == filename)
                        return filename;
                    else
                        return processedFileName;
                }
            }
            else
            {
                // For non-UNC paths, use Path methods for directory and filename extraction
                string directoryPath = Path.GetDirectoryName(filename) ?? string.Empty;
                string fileName = Path.GetFileName(filename);

                // If Path.GetFileName returns empty (for directory paths ending with separator), return original
                if (string.IsNullOrEmpty(fileName)) return filename;

                string? processedFileName = ProcessFileName(fileName);

                // If no change was made, return original
                if (processedFileName == null || processedFileName == fileName)
                    return filename;

                // Use Path.Combine to properly reconstruct the path with appropriate separators for the platform
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    return Path.Combine(directoryPath, processedFileName);
                }

                return processedFileName;
            }
        }

        /// <summary>
        /// Uses System.Uri to extract the filename from a UNC path
        /// </summary>
        /// <param name="uncPath">The UNC path to process</param>
        /// <param name="fileName">The extracted filename</param>
        /// <returns>True if the filename was successfully extracted</returns>
        private static bool TryGetFileNameFromUncPath(string uncPath, out string? fileName)
        {
            fileName = null;
            
            try
            {
                // Convert UNC path to file URI format: \\server\share\path -> file://server/share/path
                // Properly handle UNC paths by creating a file URI
                string fileUriString;
                if (uncPath.StartsWith(@"\\"))
                {
                    fileUriString = "file://" + uncPath.Substring(2).Replace('\\', '/');
                }
                else if (uncPath.StartsWith("//"))
                {
                    fileUriString = "file://" + uncPath.Substring(2);
                }
                else
                {
                    // Not a UNC path, return false to use fallback
                    return false;
                }
                
                // Create a Uri from the file URI string
                Uri uri = new Uri(fileUriString);
                
                // Get the filename from the URI - use the URI segments to get the last segment
                var segments = uri.Segments;
                if (segments.Length > 0)
                {
                    fileName = segments[segments.Length - 1]; // Get the last segment which is the filename
                    // URL decode the filename to handle any encoded characters
                    fileName = Uri.UnescapeDataString(fileName);
                }
                
                return !string.IsNullOrEmpty(fileName);
            }
            catch (UriFormatException)
            {
                // If URI creation fails, fall back to manual parsing
                return TryGetFileNameFromUncPathManual(uncPath, out fileName);
            }
            catch
            {
                // If any other error occurs during parsing, return false
                return false;
            }
        }

        /// <summary>
        /// Manually extract filename from UNC path as a fallback
        /// </summary>
        /// <param name="uncPath">The UNC path to process</param>
        /// <param name="fileName">The extracted filename</param>
        /// <returns>True if the filename was successfully extracted</returns>
        private static bool TryGetFileNameFromUncPathManual(string uncPath, out string? fileName)
        {
            fileName = null;
            
            try
            {
                // Find the last directory separator after the UNC prefix (\\server\share)
                int uncPrefixEnd = -1;
                // Find the end of the UNC prefix (first two backslashes)
                int backslashCount = 0;
                for (int i = 0; i < uncPath.Length; i++)
                {
                    if (uncPath[i] == '\\' || uncPath[i] == '/')
                    {
                        backslashCount++;
                        if (backslashCount == 2)
                        {
                            // Skip past the server name and find the end of the share name
                            for (int j = i + 1; j < uncPath.Length; j++)
                            {
                                if (uncPath[j] == '\\' || uncPath[j] == '/')
                                {
                                    uncPrefixEnd = j;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                
                if (uncPrefixEnd == -1)
                {
                    // This doesn't look like a proper UNC path, treat the part after \\ or // as filename
                    if (uncPath.Length > 2)
                    {
                        fileName = uncPath.Substring(2);
                    }
                    return fileName != null;
                }
                
                // Extract the filename part (everything after the last separator)
                int lastSeparatorPos = -1;
                for (int i = uncPath.Length - 1; i >= uncPrefixEnd; i--)
                {
                    if (uncPath[i] == '\\' || uncPath[i] == '/')
                    {
                        lastSeparatorPos = i;
                        break;
                    }
                }
                
                if (lastSeparatorPos > uncPrefixEnd)
                {
                    fileName = uncPath.Substring(lastSeparatorPos + 1);
                }
                else
                {
                    // No additional separators after the share name, treat the rest as filename
                    fileName = uncPath.Substring(uncPrefixEnd);
                }
                
                return !string.IsNullOrEmpty(fileName);
            }
            catch
            {
                // If any error occurs during parsing, return false
                return false;
            }
        }

        /// <summary>
        /// Replaces the filename part in a UNC path with a new filename
        /// </summary>
        /// <param name="originalPath">The original UNC path</param>
        /// <param name="oldFileName">The old filename to replace</param>
        /// <param name="newFileName">The new filename to use</param>
        /// <returns>The path with the filename replaced</returns>
        private static string ReplaceFileNameInPath(string originalPath, string oldFileName, string newFileName)
        {
            // Find the last occurrence of the filename in the path
            // We need to be careful to replace only the filename part, not any directory names that might match
            int lastSeparatorIndex = -1;
            for (int i = originalPath.Length - 1; i >= 0; i--)
            {
                if (originalPath[i] == '\\' || originalPath[i] == '/')
                {
                    lastSeparatorIndex = i;
                    break;
                }
            }
            
            if (lastSeparatorIndex >= 0)
            {
                // The filename part is everything after the last separator
                string directoryPath = originalPath.Substring(0, lastSeparatorIndex + 1);
                return directoryPath + newFileName;
            }
            else
            {
                // If there's no separator, the whole path is the filename
                return newFileName;
            }
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

        /// <summary>
        /// Process a filename for reserved name handling
        /// </summary>
        /// <param name="filename">The filename to process</param>
        /// <returns>A filename with reserved names handled appropriately</returns>
        private string ProcessFileName(string filename)
        {
            // Check if the entire filename consists entirely of insignificant characters (dots, spaces, tabs)
            // This handles cases like " . " where the entire name is insignificant
            string trimmedAll = filename.Trim('.', ' ', '\t');
            if (string.IsNullOrEmpty(trimmedAll))
            {
                // Replace fully insignificant names with a safe placeholder
                return "_";
            }

            // Separate name from extension properly
            (string namePart, string extensionPart) = ExtractNameAndExtension(filename);

            // Check if the name part consists entirely of insignificant characters (dots, spaces, tabs)
            string trimmedNamePartAll = namePart.Trim('.', ' ', '\t');
            if (string.IsNullOrEmpty(trimmedNamePartAll))
            {
                // Replace fully insignificant names with a safe placeholder
                return "_" + extensionPart;
            }

            // Extract the core name (without leading/trailing insignificant chars) and preserve context
            (string coreName, string leadingChars) = ExtractCoreNameAndLeadingChars(namePart);

            // Check if the core name (before trimming) starts with a reserved name
            // This handles cases like "COM1.tar" where "COM1" is reserved but embedded in a longer name
            (string actualCoreName, bool isReserved) = CheckForReservedNameStart(coreName);

            if (!isReserved)
            {
                // Check if the core name is reserved (full match)
                (actualCoreName, isReserved) = CheckForReservedNameFullMatch(coreName, actualCoreName, isReserved);
            }

            // If not found as original, try normalization to detect homoglyphs and combining marks
            if (!isReserved)
            {
                (actualCoreName, isReserved) = CheckForNormalizedReservedNames(coreName, actualCoreName, isReserved);
            }

            // Additional check: normalize the core name with combining marks removed for more thorough detection
            if (!isReserved)
            {
                (actualCoreName, isReserved) = CheckForReservedNamesWithoutCombiningMarks(coreName, actualCoreName, isReserved);
            }

            if (isReserved)
            {
                return ConstructReservedNameResult(leadingChars, actualCoreName, coreName, extensionPart);
            }

            // If not reserved, check if the name part is fully insignificant (consists only of dots, spaces, tabs)
            string trimmedForInsignificantCheck = namePart.Trim('.', ' ', '\t');
            if (string.IsNullOrEmpty(trimmedForInsignificantCheck))
            {
                // Replace fully insignificant names with a safe placeholder
                return "_" + extensionPart;
            }

            // If not reserved, return the original filename
            return filename;
        }

        /// <summary>
        /// Extracts the name part and extension part from a filename
        /// </summary>
        /// <param name="filename">The filename to process</param>
        /// <returns>Tuple containing name part and extension part</returns>
        private static (string namePart, string extensionPart) ExtractNameAndExtension(string filename)
        {
            string namePart, extensionPart = "";

            int lastDotIndex = filename.LastIndexOf('.');
            // Only consider it an extension if the dot is not at beginning or end and there's content after it
            if (lastDotIndex > 0 && lastDotIndex < filename.Length - 1)
            {
                namePart = filename.Substring(0, lastDotIndex);
                extensionPart = filename.Substring(lastDotIndex);
            }
            else
            {
                namePart = filename;
            }

            return (namePart, extensionPart);
        }

        /// <summary>
        /// Extracts the core name (without leading/trailing insignificant chars) and leading characters
        /// </summary>
        /// <param name="namePart">The name part to process</param>
        /// <returns>Tuple containing core name and leading characters</returns>
        private static (string coreName, string leadingChars) ExtractCoreNameAndLeadingChars(string namePart)
        {
            // Get the core name by trimming both leading and trailing insignificant characters
            string coreName = namePart.Trim('.', ' ', '\t');

            // Get the leading chars that were trimmed
            int leadingCharsLength = 0;
            for (int i = 0; i < namePart.Length; i++)
            {
                char c = namePart[i];
                if (c == '.' || c == ' ' || c == '\t')
                    leadingCharsLength++;
                else
                    break;
            }
            string leadingChars = namePart.Substring(0, leadingCharsLength);

            return (coreName, leadingChars);
        }

        /// <summary>
        /// Checks if the core name starts with a reserved name
        /// </summary>
        /// <param name="coreName">The core name to check</param>
        /// <returns>Tuple containing actual core name and whether it's reserved</returns>
        private static (string actualCoreName, bool isReserved) CheckForReservedNameStart(string coreName)
        {
            string actualCoreName = coreName;
            bool isReserved = false;

            // First, check if the original core name starts with a reserved name (for embedded cases)
            foreach (string reservedName in ReservedWindowsFileNames)
            {
                if (coreName.StartsWith(reservedName, StringComparison.OrdinalIgnoreCase))
                {
                    // Verify that what follows is not alphanumeric (to avoid false positives)
                    int reservedNameLength = reservedName.Length;
                    if (reservedNameLength <= coreName.Length)
                    {
                        if (reservedNameLength == coreName.Length)
                        {
                            // Exact match
                            isReserved = true;
                            actualCoreName = coreName.Substring(0, reservedNameLength); // Use original case
                            break;
                        }
                        else
                        {
                            // Check the character after the reserved name
                            char nextChar = coreName[reservedNameLength];
                            if (!char.IsLetterOrDigit(nextChar))
                            {
                                // The reserved name is followed by a non-alphanumeric character, so it's a match
                                isReserved = true;
                                actualCoreName = coreName.Substring(0, reservedNameLength); // Use original case
                                break;
                            }
                        }
                    }
                }
            }

            return (actualCoreName, isReserved);
        }

        /// <summary>
        /// Checks if the core name is reserved with a full match
        /// </summary>
        /// <param name="coreName">The core name to check</param>
        /// <param name="actualCoreName">The current actual core name</param>
        /// <param name="isReserved">The current reserved status</param>
        /// <returns>Tuple containing actual core name and whether it's reserved</returns>
        private static (string actualCoreName, bool isReserved) CheckForReservedNameFullMatch(string coreName, string actualCoreName, bool isReserved)
        {
            if (!isReserved)
            {
                // Check if the core name is reserved (full match)
                foreach (string reservedName in ReservedWindowsFileNames)
                {
                    if (string.Equals(coreName, reservedName, StringComparison.OrdinalIgnoreCase))
                    {
                        isReserved = true;
                        actualCoreName = coreName; // Use the original case
                        break;
                    }
                }
            }

            return (actualCoreName, isReserved);
        }

        /// <summary>
        /// Checks for reserved names using normalization to detect homoglyphs
        /// </summary>
        /// <param name="coreName">The core name to check</param>
        /// <param name="actualCoreName">The current actual core name</param>
        /// <param name="isReserved">The current reserved status</param>
        /// <returns>Tuple containing actual core name and whether it's reserved</returns>
        private (string actualCoreName, bool isReserved) CheckForNormalizedReservedNames(string coreName, string actualCoreName, bool isReserved)
        {
            if (!isReserved)
            {
                // Normalize the core name to detect homoglyphs and combining marks that might be used to bypass detection
                string? normalizedCore = _unicodeNormalizationService.Normalize(coreName);

                if (normalizedCore != null && !string.Equals(coreName, normalizedCore, StringComparison.Ordinal))
                {
                    // It's a homoglyph - check if the normalized form is a reserved name
                    foreach (string reservedName in ReservedWindowsFileNames)
                    {
                        if (string.Equals(normalizedCore, reservedName, StringComparison.OrdinalIgnoreCase))
                        {
                            // This is a homoglyph of a reserved name
                            isReserved = true;
                            actualCoreName = normalizedCore; // Use the normalized form
                            break;
                        }
                    }

                    // Also check for embedded reserved names in the normalized form
                    if (!isReserved)
                    {
                        foreach (string reservedName in ReservedWindowsFileNames)
                        {
                            if (normalizedCore.StartsWith(reservedName, StringComparison.OrdinalIgnoreCase))
                            {
                                int reservedNameLength = reservedName.Length;
                                if (reservedNameLength <= normalizedCore.Length)
                                {
                                    if (reservedNameLength == normalizedCore.Length)
                                    {
                                        // Exact match with normalized form
                                        isReserved = true;
                                        actualCoreName = normalizedCore; // Use the normalized form
                                        break;
                                    }
                                    else
                                    {
                                        // Check the character after the reserved name in normalized form
                                        char nextChar = normalizedCore[reservedNameLength];
                                        if (!char.IsLetterOrDigit(nextChar))
                                        {
                                            // The reserved name is followed by a non-alphanumeric character in normalized form
                                            isReserved = true;
                                            actualCoreName = normalizedCore.Substring(0, reservedNameLength); // Use the matching part
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (actualCoreName, isReserved);
        }

        /// <summary>
        /// Checks for reserved names without combining marks
        /// </summary>
        /// <param name="coreName">The core name to check</param>
        /// <param name="actualCoreName">The current actual core name</param>
        /// <param name="isReserved">The current reserved status</param>
        /// <returns>Tuple containing actual core name and whether it's reserved</returns>
        private static (string actualCoreName, bool isReserved) CheckForReservedNamesWithoutCombiningMarks(string coreName, string actualCoreName, bool isReserved)
        {
            // Additional check: normalize the core name with combining marks removed for more thorough detection
            if (!isReserved)
            {
                // Remove combining marks to detect attempts to bypass using diacritics
                string nameWithoutCombiningMarks = RemoveCombiningMarks(coreName);

                if (!string.Equals(nameWithoutCombiningMarks, coreName, StringComparison.Ordinal))
                {
                    // The name had combining marks, so check if the stripped version matches a reserved name
                    foreach (string reservedName in ReservedWindowsFileNames)
                    {
                        if (string.Equals(nameWithoutCombiningMarks, reservedName, StringComparison.OrdinalIgnoreCase))
                        {
                            isReserved = true;
                            actualCoreName = coreName; // Keep the original with combining marks for processing
                            break;
                        }
                    }

                    // Also check for embedded names without combining marks
                    if (!isReserved)
                    {
                        foreach (string reservedName in ReservedWindowsFileNames)
                        {
                            if (nameWithoutCombiningMarks.StartsWith(reservedName, StringComparison.OrdinalIgnoreCase))
                            {
                                int reservedNameLength = reservedName.Length;
                                if (reservedNameLength <= nameWithoutCombiningMarks.Length)
                                {
                                    if (reservedNameLength == nameWithoutCombiningMarks.Length)
                                    {
                                        // Exact match without combining marks
                                        isReserved = true;
                                        actualCoreName = coreName; // Keep the original with combining marks
                                        break;
                                    }
                                    else
                                    {
                                        // Check the character after the reserved name in the combining-mark-stripped form
                                        char nextChar = nameWithoutCombiningMarks[reservedNameLength];
                                        if (!char.IsLetterOrDigit(nextChar))
                                        {
                                            // The reserved name is followed by a non-alphanumeric character in the stripped form
                                            isReserved = true;
                                            actualCoreName = coreName; // Keep the original with combining marks
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (actualCoreName, isReserved);
        }

        /// <summary>
        /// Constructs the result for a reserved name
        /// </summary>
        /// <param name="leadingChars">The leading characters from the original name</param>
        /// <param name="actualCoreName">The actual core name that was detected as reserved</param>
        /// <param name="coreName">The original core name</param>
        /// <param name="extensionPart">The extension part of the filename</param>
        /// <returns>The constructed filename with reserved name handled</returns>
        private static string ConstructReservedNameResult(string leadingChars, string actualCoreName, string coreName, string extensionPart)
        {
            // For reserved names, construct the result by using leading characters from the original name
            // and adding an underscore to the base name

            // Determine the base name to use in the result
            // If we detected it as a homoglyph, actualCoreName is already the normalized form
            // Otherwise, it's the original form
            string coreNameForResult = actualCoreName;

            // The new name part is: leading characters + core name (normalized if it was a homoglyph) + underscore + rest of name after reserved part
            // For homoglyphs that matched exactly, there's no "rest" - for embedded matches, there might be
            string restOfName = coreName.Length > actualCoreName.Length ? coreName.Substring(actualCoreName.Length) : "";

            // For homoglyphs, we use the normalized form, so no need to normalize again
            string newNamePart = leadingChars + coreNameForResult + "_" + restOfName;

            return newNamePart + extensionPart;
        }

        /// <summary>
        /// Removes combining marks from a string to detect attempts to bypass reserved name checks using diacritics.
        /// </summary>
        /// <param name="input">The input string to process</param>
        /// <returns>The string with combining marks removed</returns>
        private static string RemoveCombiningMarks(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder();
            foreach (char c in input)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark &&
                    category != UnicodeCategory.SpacingCombiningMark &&
                    category != UnicodeCategory.EnclosingMark)
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }
    }
}
