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
        public string? Handle(string? filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return filename;

            // Extract the directory path and filename separately
            string? directoryPath = Path.GetDirectoryName(filename);
            string fullFileName = Path.GetFileName(filename);
            
            // Find the first dot to separate the base name from all extensions
            // For "CON.txt.bak", the reserved name is "CON" and extension is ".txt.bak"
            int firstDotIndex = fullFileName.IndexOf('.');
            string namePart, extensionPart;
            
            if (firstDotIndex > 0)
            {
                namePart = fullFileName.Substring(0, firstDotIndex);
                extensionPart = fullFileName.Substring(firstDotIndex); // includes the dot
            }
            else
            {
                namePart = fullFileName;
                extensionPart = "";
            }

            // Trim leading spaces for initial check
            string trimmedLeadingSpaces = namePart.TrimStart();
            
            // Trim trailing dots and spaces since Windows treats them as insignificant
            // This gives us the core name to check against reserved names
            string baseNameForCheck = trimmedLeadingSpaces.TrimEnd('.', ' ', '\t');
            
            // Get the trailing dots and spaces that were removed
            int trailingInsignificantLength = trimmedLeadingSpaces.Length - baseNameForCheck.Length;
            string trailingDotsAndSpaces = trailingInsignificantLength > 0 ? 
                trimmedLeadingSpaces.Substring(trimmedLeadingSpaces.Length - trailingInsignificantLength) : "";
            
            bool isReserved = !string.IsNullOrEmpty(baseNameForCheck) && 
                              (ReservedWindowsFileNames.Contains(baseNameForCheck) 
                              || ReservedWindowsFileNames.Contains(_unicodeNormalizer.Normalize(baseNameForCheck)));

            if (isReserved)
            {
                // Insert underscore after core name: leading spaces + core name + underscore + trailing dots/spaces
                // This handles both trailing spaces and trailing dots consistently according to Windows behavior
                string leadingSpaces = namePart.Substring(0, namePart.Length - trimmedLeadingSpaces.Length);
                string modifiedNamePart = leadingSpaces + baseNameForCheck + "_" + trailingDotsAndSpaces;
                string modifiedFileName = modifiedNamePart + extensionPart;
                return !string.IsNullOrEmpty(directoryPath)
                    ? Path.Combine(directoryPath, modifiedFileName)
                    : modifiedFileName;
            }

            return filename;
        }
    }
}