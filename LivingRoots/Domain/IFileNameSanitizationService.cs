using System;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for filename sanitization service in the domain layer
    /// following the Dependency Inversion Principle
    /// </summary>
    public interface IFileNameSanitizationService
    {
        /// <summary>
        /// Sanitizes a filename by removing or replacing invalid characters and handling security concerns.
        /// </summary>
        /// <param name="filename">The filename to sanitize.</param>
        /// <returns>The sanitized filename.</returns>
        /// <exception cref="ArgumentException">Thrown when filename sanitizes to an empty string or is too long.</exception>
        string? Sanitize(string? filename);
    }
}
