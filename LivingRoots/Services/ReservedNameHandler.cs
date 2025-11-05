using System;
using System.Collections.Generic;
using System.IO;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation for handling reserved Windows filenames to prevent conflicts with system files.
    /// </summary>
    public class ReservedNameHandler : IReservedNameHandler
    {
        private readonly IUnicodeNormalizer _unicodeNormalizer;
        private static readonly HashSet<string> ReservedWindowsFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public ReservedNameHandler(IUnicodeNormalizer unicodeNormalizer)
        {
            _unicodeNormalizer = unicodeNormalizer ?? throw new ArgumentNullException(nameof(unicodeNormalizer));
        }

        /// <summary>
        /// Handles reserved Windows filenames by appending an underscore to the base name if necessary.
        /// </summary>
        /// <param name="filename">The filename to check for reserved names.</param>
        /// <returns>A filename with reserved names handled appropriately.</returns>
        public string Handle(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return filename;

            // Extract the directory path and filename separately
            string? directoryPath = Path.GetDirectoryName(filename);
            string fullFileName = Path.GetFileName(filename);
            
            // Find the last dot to separate the base name from the extension
            // For "CON.txt.bak", the reserved name is "CON" and extension is ".txt.bak"
            int lastDotIndex = fullFileName.LastIndexOf('.');
            string namePart, extensionPart;
            
            if (lastDotIndex > 0)
            {
                namePart = fullFileName.Substring(0, lastDotIndex);
                extensionPart = fullFileName.Substring(lastDotIndex); // includes the dot
            }
            else
            {
                namePart = fullFileName;
                extensionPart = "";
            }

            // Trim the name part to handle cases like " CON " that should be treated as "CON"
            string trimmedNamePart = namePart.Trim();
            
            bool isReserved = ReservedWindowsFileNames.Contains(trimmedNamePart) 
                              || ReservedWindowsFileNames.Contains(_unicodeNormalizer.Normalize(trimmedNamePart));

            if (isReserved)
            {
                string modifiedFileName = namePart + "_" + extensionPart;
                return !string.IsNullOrEmpty(directoryPath)
                    ? Path.Combine(directoryPath, modifiedFileName)
                    : modifiedFileName;
            }

            return filename;
        }
    }
}