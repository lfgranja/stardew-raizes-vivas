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

            // Use Path.GetFileName and Path.GetDirectoryName for all paths (both UNC and regular)
            string? directoryPath = Path.GetDirectoryName(filename);
            string fullFileName = Path.GetFileName(filename);

            // On Unix-like systems, UNC paths starting with \\ may not be handled properly by Path.GetDirectoryName
            // So we need to handle them manually if Path.GetDirectoryName returns null/empty for UNC paths
            if (string.IsNullOrEmpty(directoryPath) && !string.IsNullOrEmpty(filename) && filename.StartsWith(@"\\"))
            {
                // For UNC paths, manually extract the filename to ensure correct behavior across platforms
                int lastBackslashIndex = filename.LastIndexOf('\\');
                if (lastBackslashIndex >= 0 && lastBackslashIndex < filename.Length - 1)
                {
                    directoryPath = filename.Substring(0, lastBackslashIndex);
                    fullFileName = filename.Substring(lastBackslashIndex + 1);
                }
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
                // For UNC paths, we need to ensure proper path reconstruction
                if (filename.StartsWith(@"\\"))
                {
                    // If the original was a UNC path, ensure we reconstruct it properly
                    // Use backslashes consistently for UNC paths
                    return directoryPath + "\\" + processedFileName;
                }
                else
                {
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

            // Check if the name part consists entirely of insignificant characters (dots, spaces, tabs)
            string trimmedNamePartAll = namePart.Trim('.', ' ', '\t');
            if (string.IsNullOrEmpty(trimmedNamePartAll))
            {
                // Replace fully insignificant names with a safe placeholder
                return "_" + extensionPart;
            }
            
            // Extract the core name (without leading/trailing insignificant chars) and preserve context
            // First, get the core name by trimming both leading and trailing insignificant characters
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
            
            // Get the trailing chars that were trimmed (from the end of the name after removing leading chars)
            string remainingAfterLeadingTrim = namePart.Substring(leadingCharsLength);
            int trailingCharsLength = 0;
            for (int i = remainingAfterLeadingTrim.Length - 1; i >= 0; i--)
            {
                char c = remainingAfterLeadingTrim[i];
                if (c == '.' || c == ' ' || c == '\t')
                    trailingCharsLength++;
                else
                    break;
            }
            
            // Check if the core name (before trimming) starts with a reserved name
            // This handles cases like "COM1.tar" where "COM1" is reserved but embedded in a longer name
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

            // If not found as original, try normalization to detect homoglyphs and combining marks
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

            if (isReserved)
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