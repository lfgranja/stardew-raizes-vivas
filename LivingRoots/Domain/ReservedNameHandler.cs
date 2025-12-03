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
        /// Uses System.Uri for proper UNC path handling and cross-platform compatibility.
        /// For directory paths ending with separators, returns the original path unchanged.
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

            // For UNC paths (starting with \\ or //), we need special handling
            if (IsUncPath(normalizedInput))
            {
                // Extract the filename component from the UNC path
                string fileName = Path.GetFileName(normalizedInput);

                // If Path.GetFileName returns empty (for directory paths ending with separator), return original
                if (string.IsNullOrEmpty(fileName)) return filename;

                // For UNC paths, extract directory path separately
                string directoryPath = Path.GetDirectoryName(normalizedInput) ?? string.Empty;

                // Process just the filename component for reserved names
                string? processedFileName = ProcessFileNameInternal(fileName);

                // If the filename component didn't change, return the original path
                if (processedFileName == fileName)
                    return filename;

                // Reconstruct the UNC path with the processed filename component
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    return Path.Combine(directoryPath, processedFileName);
                }

                return processedFileName;
            }
            else
            {
                // For regular paths, extract the filename component using Path methods
                string fileName = Path.GetFileName(normalizedInput);

                // If Path.GetFileName returns empty (for directory paths ending with separator), return original
                if (string.IsNullOrEmpty(fileName)) return filename;

                // Extract the directory path separately
                string directoryPath = Path.GetDirectoryName(normalizedInput) ?? string.Empty;

                // Process just the filename component for reserved names
                string? processedFileName = ProcessFileNameInternal(fileName);

                // If no change was made to the filename component, return the original path
                if (processedFileName == null || processedFileName == fileName)
                    return filename;

                // Reconstruct the full path with the processed filename component
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    return Path.Combine(directoryPath, processedFileName);
                }

                return processedFileName;
            }
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
                // Return the reserved base name with underscore, plus extension part
                return baseName + "_" + extensionPart;
            }
            
            // If not reserved, check if the name part is fully insignificant (consists only of dots, spaces, tabs)
            string trimmedForInsignificantCheck = baseName.Trim('.', ' ', '\t');
            if (string.IsNullOrEmpty(trimmedForInsignificantCheck))
            {
                // Replace fully insignificant names with a safe placeholder
                return "_" + extensionPart;
            }
            
            // If not reserved, return the original filename
            return filename;
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
