using System;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Implementation for core mod logic in the domain layer
    /// following the Dependency Inversion Principle
    /// </summary>
    public class ModLogic(IFileNameSanitizationService fileNameSanitizationService, IPathValidationService pathValidationService) : IModLogic
    {
        private readonly IFileNameSanitizationService _fileNameSanitizationService = fileNameSanitizationService ?? throw new ArgumentNullException(nameof(fileNameSanitizationService));
        private readonly IPathValidationService _pathValidationService = pathValidationService ?? throw new ArgumentNullException(nameof(pathValidationService));

        /// <summary>
        /// Sanitizes a filename using domain services
        /// </summary>
        /// <param name="filename">The filename to sanitize</param>
        /// <returns>The sanitized filename</returns>
        public string? SanitizeFileName(string? filename)
        {
            return _fileNameSanitizationService.Sanitize(filename);
        }

        /// <summary>
        /// Validates a path for security concerns
        /// </summary>
        /// <param name="path">The path to validate</param>
        public void ValidatePath(string path)
        {
            _pathValidationService.Validate(path);
        }
    }
}
