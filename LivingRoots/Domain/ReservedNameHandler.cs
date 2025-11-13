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
        /// Handles reserved Windows filenames by appending an underscore to the base name if necessary.
        /// </summary>
        /// <param name="filename">The filename to check for reserved names.</param>
        /// <returns>A filename with reserved names handled appropriately.</returns>
        public string? Handle(string? filename)
        {
            // Handle null/empty strings
            if (string.IsNullOrEmpty(filename))
                return filename;

            // Guard against absolute/UNC paths - return unchanged if Path.IsPathRooted
            if (Path.IsPathRooted(filename))
                return filename;

            // Extract the directory path and filename separately
            string? directoryPath = Path.GetDirectoryName(filename);
            string fullFileName = Path.GetFileName(filename);
            
            // If Path.GetFileName returns an empty string, this means the input ends with a directory separator
            // In this case, we should return the original filename to avoid incorrect mutation of directory paths
            if (string.IsNullOrEmpty(fullFileName))
                return filename;
            
            // Separate the base name from extensions, but handle trailing insignificant dots carefully
            // Find the first dot that separates the actual name from the extension
            int firstDotIndex = -1;
            for (int i = 0; i < fullFileName.Length; i++)
            {
                if (fullFileName[i] == '.')
                {
                    // If this is the first character or follows another dot at the beginning, 
                    // continue looking for the first meaningful dot
                    if (i > 0)
                    {
                        firstDotIndex = i;
                        break;
                    }
                }
            }
            
            string namePart, extensionPart;
            
            if (firstDotIndex >= 0)
            {
                string potentialName = fullFileName.Substring(0, firstDotIndex);
                string potentialExtension = fullFileName.Substring(firstDotIndex);
                
                // Check if the potential extension is made up entirely of insignificant characters
                // If so, it's not a real extension but trailing insignificant characters
                if (IsFullyInsignificant(potentialExtension))
                {
                    // Treat it as part of the name with trailing insignificant chars
                    namePart = fullFileName;
                    extensionPart = "";
                }
                else
                {
                    // This is a real extension
                    namePart = potentialName;
                    extensionPart = potentialExtension;
                }
            }
            else
            {
                namePart = fullFileName;
                extensionPart = "";
            }

            // Process the name part: trim leading spaces and check for reserved names
            string trimmedLeadingSpaces = namePart.TrimStart();
            
            // Get the core name by trimming trailing insignificant characters (dots and spaces)
            string baseNameForCheck = trimmedLeadingSpaces.TrimEnd('.', ' ', '\t');
            
            // Handle the case where the name becomes empty after trimming
            if (string.IsNullOrEmpty(baseNameForCheck))
            {
                // For consistency with most tests: return the original filename to prevent malformed outputs
                // This preserves user input and maintains expected behavior for most scenarios
                return filename;
            }
            
            // Normalize for comparison with reserved names
            string? normalizedForCheck = _unicodeNormalizationService.Normalize(baseNameForCheck);

            // Check if the core name is reserved
            bool isReserved = ReservedWindowsFileNames.Contains(baseNameForCheck) ||
                              (!string.IsNullOrEmpty(normalizedForCheck) && ReservedWindowsFileNames.Contains(normalizedForCheck));

            if (isReserved)
            {
                // For reserved names, construct the result by using the leading spaces from the original name
                // and adding the underscore to the baseNameForCheck (not the original namePart which has trailing chars)
                string leadingSpaces = namePart.Substring(0, namePart.Length - trimmedLeadingSpaces.Length);
                
                // The new name part is: leading spaces + baseNameForCheck (without trailing insignificant chars) + underscore
                string newNamePart = leadingSpaces + baseNameForCheck + "_";
                
                // Build result: directory + new name part + extension (extension was not modified)
                string result;
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    result = directoryPath + Path.DirectorySeparatorChar + newNamePart + extensionPart;
                }
                else
                {
                    result = newNamePart + extensionPart;
                }
                
                return result;
            }

            // If not reserved, return original filename
            return filename;
        }
        
        /// <summary>
        /// Determines if a string is fully insignificant (contains only dots, spaces, and tabs).
        /// </summary>
        /// <param name="input">The string to check</param>
        /// <returns>True if the string is fully insignificant, false otherwise</returns>
        private static bool IsFullyInsignificant(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;
                
            foreach (char c in input)
            {
                if (c != '.' && c != ' ' && c != '\t')
                {
                    return false;
                }
            }
            return true;
        }
    }
}