using System;
using LivingRoots.Domain;

namespace LivingRoots.Services
{
    /// <summary>
    /// Adapter implementation for IPathTraversalValidator that uses domain service
    /// </summary>
    public class PathTraversalValidator : IPathTraversalValidator
    {
        private readonly IPathValidationService _pathValidationService;

        public PathTraversalValidator(IPathValidationService pathValidationService)
        {
            _pathValidationService = pathValidationService ?? throw new ArgumentNullException(nameof(pathValidationService));
        }

        /// <summary>
        /// Validates that a path does not contain path traversal patterns.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown if path traversal is detected.</exception>
        public void Validate(string path)
        {
            _pathValidationService.Validate(path);
        }
    }
}