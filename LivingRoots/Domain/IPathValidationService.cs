using System;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for path validation service in the domain layer
    /// following the Dependency Inversion Principle
    /// </summary>
    public interface IPathValidationService
    {
        /// <summary>
        /// Validates that a path does not contain path traversal patterns.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown if path traversal is detected.</exception>
        void Validate(string path);
    }
}
