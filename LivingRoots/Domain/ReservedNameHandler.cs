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

            // Guard against absolute/UNC paths: preserve directory, still handle reserved leaf names
            if (Path.IsPathRooted(filename))
            {
                string? directoryPath = Path.GetDirectoryName(filename);
                string fullFileName = Path.GetFileName(filename);

                // If there's no file name (e.g., path ends with separator), return unchanged
                if (string.IsNullOrEmpty(fullFileName))
                    return filename;

                // Reuse existing logic by processing just the leaf name
                string? processed = ProcessFileName(fullFileName);

                // If no change or processing failed, return original
                if (processed == null || processed == fullFileName)
                    return filename;

                // Rebuild rooted path with processed filename
                return !string.IsNullOrEmpty(directoryPath)
                    ? Path.Combine(directoryPath, processed)
                    : processed;
            }

            // For non-rooted paths, process using the same logic as before
            return ProcessFileName(filename);
        }
        
        /// <summary>
        /// Process a filename for reserved name handling (original non-rooted path logic)
        /// </summary>
        /// <param name="filename">The filename to process</param>
        /// <returns>A filename with reserved names handled appropriately</returns>
        private string? ProcessFileName(string filename)
        {
            // Extract directory path and filename separately
            string? directoryPath = Path.GetDirectoryName(filename);
            string fullFileName = Path.GetFileName(filename);
            
            // If Path.GetFileName returns an empty string, this means input ends with a directory separator
            // In this case, we should return the original filename to avoid incorrect mutation of directory paths
            if (string.IsNullOrEmpty(fullFileName))
                return filename;
            
            // Separate base name from extensions, but handle trailing insignificant dots carefully
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

            // Process name part: trim leading spaces and check for reserved names
            string trimmedLeadingSpaces = namePart.TrimStart();
            
            // Get the core name by trimming trailing insignificant characters (dots and spaces)
            string baseNameForCheck = trimmedLeadingSpaces.TrimEnd('.', ' ', '\t');
            
            // Handle the case where the name becomes empty after trimming
            if (string.IsNullOrEmpty(baseNameForCheck))
            {
                // Replace fully insignificant names with a safe placeholder, preserving directory and extension
                string safeName = "_";
                if (!string.IsNullOrEmpty(directoryPath))
                    return directoryPath + Path.DirectorySeparatorChar + safeName + extensionPart;
                else
                    return safeName + extensionPart;
            }
            
            // Normalize for comparison with reserved names
            string? normalizedForCheck = _unicodeNormalizationService.Normalize(baseNameForCheck);

            // Check if the core name is reserved
            bool isReserved = ReservedWindowsFileNames.Contains(baseNameForCheck) ||
                              (!string.IsNullOrEmpty(normalizedForCheck) && ReservedWindowsFileNames.Contains(normalizedForCheck));

            if (isReserved)
            {
                // For reserved names, construct the result by using leading spaces from the original name
                // and adding an underscore to the normalized base name (for security)
                string leadingSpaces = namePart.Substring(0, namePart.Length - trimmedLeadingSpaces.Length);
                
                // Use the normalized form if it exists, otherwise use the original base name
                string baseNameForResult = !string.IsNullOrEmpty(normalizedForCheck) ? normalizedForCheck : baseNameForCheck;
                
                // The new name part is: leading spaces + base name (normalized if changed) + underscore
                string newNamePart = leadingSpaces + baseNameForResult + "_";
                
                // Build the result: directory + new name part + extension (extension was not modified)
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

            // If not reserved, return the original filename
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