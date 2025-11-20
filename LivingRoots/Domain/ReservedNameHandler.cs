using System;
using System.Collections.Generic;
using System.IO;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Implementation for handling reserved Windows filenames to prevent conflicts with system files.
    /// This implementation follows the Dependency Inversion Principle by depending on abstractions.
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
        /// </summary>
        /// <param name="filename">The filename to check for reserved names.</param>
        /// <returns>A filename with reserved names handled appropriately.</returns>
        public string? Handle(string? filename)
        {
            // Handle null/empty strings
            if (string.IsNullOrEmpty(filename))
                return filename;

            // For UNC paths (starting with \\) and rooted paths, extract the actual filename
            string? directoryPath = null;
            string fullFileName = filename;

            // Check if this is a UNC path (starts with \\) or if Path.GetFileName can extract the filename
            if (filename.StartsWith(@"\\"))
            {
                // For UNC paths, manually extract the filename by finding the last backslash
                int lastBackslashIndex = filename.LastIndexOf('\\');
                if (lastBackslashIndex >= 0 && lastBackslashIndex < filename.Length - 1)
                {
                    directoryPath = filename.Substring(0, lastBackslashIndex + 1);
                    fullFileName = filename.Substring(lastBackslashIndex + 1);
                }
            }
            else
            {
                // For regular paths, use Path.GetFileName and Path.GetDirectoryName
                directoryPath = Path.GetDirectoryName(filename);
                fullFileName = Path.GetFileName(filename);
            }

            // If Path.GetFileName or our manual extraction returns an empty string, 
            // this means input ends with a directory separator
            // In this case, we should return the original filename to avoid incorrect mutation of directory paths
            if (string.IsNullOrEmpty(fullFileName))
                return filename;

            // Process the filename component separately
            string? processedFileName = ProcessFileName(fullFileName);

            // If no change or processing failed, return original
            if (processedFileName == null || processedFileName == fullFileName)
                return filename;

            // Rebuild path with processed filename
            if (!string.IsNullOrEmpty(directoryPath))
            {
                // Handle UNC paths specially to avoid adding extra slashes
                if (directoryPath.StartsWith(@"\\"))
                {
                    return directoryPath + processedFileName;
                }
                else
                {
                    // Use Path.Combine to ensure proper path separators are used
                    return Path.Combine(directoryPath, processedFileName);
                }
            }
            else
                return processedFileName;
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
            string namePart, extensionPart = "";
            
            int lastDotIndex = filename.LastIndexOf('.');
            // Only consider it an extension if the dot is not at the beginning or end and there's content after it
            if (lastDotIndex > 0 && lastDotIndex < filename.Length - 1)
            {
                namePart = filename.Substring(0, lastDotIndex);
                extensionPart = filename.Substring(lastDotIndex);
            }
            else
            {
                namePart = filename;
            }

            // Check if the name part consists entirely of insignificant characters (dots, spaces, tabs)
            // This means after trimming all dots, spaces, and tabs, there's nothing left
            trimmedAll = namePart.Trim('.', ' ', '\t');
            if (string.IsNullOrEmpty(trimmedAll))
            {
                // Replace fully insignificant names with a safe placeholder
                return "_" + extensionPart;
            }
            
            // Check for reserved names by looking for them in the name part
            // We need to handle several cases:
            // 1. Exact reserved name: "CON" -> "CON_"
            // 2. Reserved name with extension: "CON.txt" -> "CON_.txt"
            // 3. Reserved name with leading/trailing chars: " CON " -> " CON_"
            // 4. Reserved name with trailing insignificant chars: "CON..." -> "CON_"
            // 5. Unicode homoglyphs: "CОN" (with Cyrillic O) -> "CON_"
            
            foreach (string reservedName in ReservedWindowsFileNames)
            {
                // Check if the name part contains the reserved name
                int startIndex = FindCaseInsensitiveIndex(namePart, reservedName);
                if (startIndex >= 0)
                {
                    // Check if the reserved name is the entire name part or if it's followed by insignificant characters only
                    string afterReserved = namePart.Substring(startIndex + reservedName.Length);
                    string beforeReserved = namePart.Substring(0, startIndex);
                    
                    // If the part after the reserved name is only insignificant characters (dots, spaces, tabs), 
                    // we should treat this as a reserved name with trailing insignificant characters
                    string trimmedAfter = afterReserved.Trim('.', ' ', '\t');
                    if (string.IsNullOrEmpty(trimmedAfter))
                    {
                        // The reserved name is followed only by insignificant characters
                        // Return the original string but with an underscore added right after the reserved name
                        string originalCasedReserved = namePart.Substring(startIndex, reservedName.Length);
                        return beforeReserved + originalCasedReserved + "_" + extensionPart;
                    }
                    
                    // If the part after the reserved name is a dot (extension), handle it specially
                    if (afterReserved.StartsWith("."))
                    {
                        // This is a reserved name followed by an extension
                        string originalCasedReserved = namePart.Substring(startIndex, reservedName.Length);
                        string extensionAfterReserved = afterReserved; // includes the dot and anything after it
                        return beforeReserved + originalCasedReserved + "_" + extensionAfterReserved + extensionPart;
                    }
                    
                    // If the reserved name is part of a larger name that is not just followed by insignificant chars,
                    // then it's not a pure reserved name and should not be treated as such
                    // e.g., "CONSOLE" contains "CON" but should not be treated as reserved
                }
            }
            
            // Check for reserved names after Unicode normalization (for homoglyphs and diacritics)
            // This handles cases where the name normalizes to a reserved name
            string baseNameForCheck = namePart.Trim('.', ' ', '\t');
            string? normalizedBaseName = _unicodeNormalizationService.Normalize(baseNameForCheck);
            
            if (!string.IsNullOrEmpty(normalizedBaseName) && ReservedWindowsFileNames.Contains(normalizedBaseName))
            {
                // The normalized name is a reserved name, so we need to handle it
                // For homoglyphs and diacritics, return the normalized reserved name with underscore
                return normalizedBaseName + "_" + extensionPart;
            }
            
            // If not reserved and not fully insignificant, return the original filename
            return filename;
        }
        
        /// <summary>
        /// Find the index of a substring within a string, ignoring case
        /// </summary>
        /// <param name="source">The source string</param>
        /// <param name="value">The value to search for</param>
        /// <returns>The index of the value in the source string, or -1 if not found</returns>
        private static int FindCaseInsensitiveIndex(string source, string value)
        {
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }
    }
}