using System.Text;

namespace LivingRoots.Services
{
    /// <summary>
    /// Interface for sanitizing filenames to make them safe for file system operations
    /// </summary>
    public interface IFileNameSanitizer
    {
        /// <summary>
        /// Sanitizes a filename by removing or replacing invalid characters and handling security concerns
        /// </summary>
        /// <param name="filename">The filename to sanitize</param>
        /// <returns>The sanitized filename</returns>
        /// <exception cref="ArgumentException">Thrown when the filename sanitizes to an empty string</exception>
        string Sanitize(string filename);
    }
}