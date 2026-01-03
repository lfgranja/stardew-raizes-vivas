using System;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for core mod logic in the domain layer
    /// following the Dependency Inversion Principle
    /// </summary>
    public interface IModLogic
    {
        /// <summary>
        /// Sanitizes a filename using domain services
        /// </summary>
        /// <param name="filename">The filename to sanitize</param>
        /// <returns>The sanitized filename</returns>
        string? SanitizeFileName(string? filename);

        /// <summary>
        /// Validates a path for security concerns
        /// </summary>
        /// <param name="path">The path to validate</param>
        void ValidatePath(string path);
    }
}
