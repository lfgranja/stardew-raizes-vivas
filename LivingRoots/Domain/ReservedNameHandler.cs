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
                return directoryPath + processedFileName;
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
            string trimmedNamePartAll = namePart.Trim('.', ' ', '\t');
            if (string.IsNullOrEmpty(trimmedNamePartAll))
            {
                // Replace fully insignificant names with a safe placeholder
                return "_" + extensionPart;
            }
            
            // Get the core name by trimming trailing insignificant characters (dots and spaces)
            string baseNameForCheck = namePart.TrimEnd('.', ' ', '\t');
            
            // Get the portion before trimming to preserve leading spaces
            string leadingChars = namePart.Substring(0, namePart.Length - baseNameForCheck.Length);
            
            // Further trim leading spaces and dots to get the core name for checking
            baseNameForCheck = baseNameForCheck.TrimStart(' ', '\t', '.');
            
            // Normalize for comparison with reserved names
            string? normalizedForCheck = _unicodeNormalizationService.Normalize(baseNameForCheck);

            // Check if the core name is reserved
            bool isReserved = ReservedWindowsFileNames.Contains(baseNameForCheck) ||
                              (!string.IsNullOrEmpty(normalizedForCheck) && ReservedWindowsFileNames.Contains(normalizedForCheck));

            if (isReserved)
            {
                // For reserved names, construct the result by using leading characters from the original name
                // and adding an underscore to the normalized base name (for security)
                
                // Use the normalized form of the base name to prevent homoglyph spoofing
                string baseNameForResult = !string.IsNullOrEmpty(normalizedForCheck) ? normalizedForCheck : baseNameForCheck;
                
                // The new name part is: leading characters + base name (normalized if changed) + underscore
                string newNamePart = leadingChars + baseNameForResult + "_";
                
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
    }
}