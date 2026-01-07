using LivingRoots.Domain;

namespace LivingRoots.Services
{
    /// <summary>
    /// Adapter implementation for IFileNameSanitizer that uses domain service
    /// </summary>
    public class FileNameSanitizer(IFileNameSanitizationService fileNameSanitizationService)
        : IFileNameSanitizer
    {
        private readonly IFileNameSanitizationService _fileNameSanitizationService =
            fileNameSanitizationService
            ?? throw new ArgumentNullException(nameof(fileNameSanitizationService));

        /// <summary>
        /// Sanitizes a filename by removing or replacing invalid characters and handling security concerns.
        /// </summary>
        /// <param name="filename">The filename to sanitize.</param>
        /// <returns>The sanitized filename.</returns>
        /// <exception cref="ArgumentException">Thrown when filename sanitizes to an empty string or is too long.</exception>
        public string? Sanitize(string? filename)
        {
            return _fileNameSanitizationService.Sanitize(filename);
        }
    }
}
