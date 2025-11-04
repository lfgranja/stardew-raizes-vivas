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

            // Extract the full filename without path
            string fullFileName = Path.GetFileName(filename);
            
            // Find the first dot to separate the actual name from extensions
            // For "CON.txt.bak", the reserved name is "CON" and extensions are ".txt.bak"
            int firstDotIndex = fullFileName.IndexOf('.');
            string namePart, extensionPart;
            
            if (firstDotIndex > 0)
            {
                namePart = fullFileName.Substring(0, firstDotIndex);
                extensionPart = fullFileName.Substring(firstDotIndex); // includes the dot(s)
            }
            else
            {
                namePart = fullFileName;
                extensionPart = "";
            }

            // Normalize the name part to check for homoglyphs
            string normalizedBaseName = _unicodeNormalizer.Normalize(namePart);

            // Check if the base name (without extension) matches a reserved name
            if (ReservedWindowsFileNames.Contains(normalizedBaseName))
            {
                // Reconstruct the filename with an underscore before the extension
                if (!string.IsNullOrEmpty(extensionPart))
                {
                    return $"{namePart}_{extensionPart}";
                }
                else
                {
                    return $"{namePart}_";
                }
            }

            return filename;
        }
    }
}