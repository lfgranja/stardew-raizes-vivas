using System;
using LivingRoots.Domain;

namespace LivingRoots.Services
{
    /// <summary>
    /// Adapter implementation for IPathTraversalValidator that uses domain service
    /// This class now serves as a simple adapter that delegates to PathValidationService for comprehensive validation
    ///
    /// IMPROVEMENTS SUMMARY:
    /// - Removed redundant validation logic that was duplicated in PathValidationService
    /// - Now acts as a pure adapter/delegate pattern to PathValidationService
    /// - Eliminated code duplication and improved maintainability
    /// - Reduced cognitive load by consolidating validation logic in a single location
    /// </summary>
    public class PathTraversalValidator(IPathValidationService pathValidationService) : IPathTraversalValidator
    {
        private readonly IPathValidationService _pathValidationService = pathValidationService ?? throw new ArgumentNullException(nameof(pathValidationService));

        /// <summary>
        /// Validates that a path does not contain path traversal patterns.
        /// Delegates to PathValidationService to avoid code duplication.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown if path traversal is detected.</exception>
        public void Validate(string path)
        {
            // Basic null/empty check
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            // Delegate to the domain service for comprehensive validation
            // This eliminates redundant validation logic and ensures consistency
            _pathValidationService.Validate(path);
        }
    }
}
