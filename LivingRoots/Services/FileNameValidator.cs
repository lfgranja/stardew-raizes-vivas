using System.Text.RegularExpressions;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation for validating filenames to ensure they meet platform-specific requirements
    /// </summary>
    public class FileNameValidator : IFileNameValidator
    {
        private static readonly HashSet<char> InvalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars())
        {
            '<', '>', ':', '"', '|', '?', '*'
        };

        /// <summary>
        /// Validates that a filename is safe and valid for file system operations
        /// </summary>
        /// <param name="filename">The filename to validate</param>
        /// <exception cref="ArgumentException">Thrown if the filename is invalid</exception>
        public void Validate(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Filename cannot be null or empty.", nameof(filename));

            // Check for null characters which are not allowed in filenames
            if (filename.Contains('\0'))
                throw new ArgumentException("Filename cannot contain null characters.", nameof(filename));

            // Check for invalid filename characters
            foreach (char c in filename)
            {
                if (InvalidFileNameChars.Contains(c))
                {
                    throw new ArgumentException($"Filename contains invalid character: {c}", nameof(filename));
                }
            }

            // Check for control characters (except tab, newline, carriage return)
            foreach (char c in filename)
            {
                if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                {
                    throw new ArgumentException($"Filename contains control character: {c}", nameof(filename));
                }
            }

            // Trim whitespace, dots, and underscores to check if the result would be empty
            string trimmed = filename.Trim(' ', '.', '_');
            if (string.IsNullOrEmpty(trimmed))
            {
                throw new ArgumentException("Filename sanitizes to an empty string, which is not allowed.", nameof(filename));
            }
        }
    }
}