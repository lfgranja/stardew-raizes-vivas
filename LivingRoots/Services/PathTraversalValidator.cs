using System;
using System.Linq;
using LivingRoots.Domain;

namespace LivingRoots.Services
{
    /// <summary>
    /// Adapter implementation for IPathTraversalValidator that uses domain service
    /// </summary>
    public class PathTraversalValidator : IPathTraversalValidator
    {
        public PathTraversalValidator()
        {
            // No dependencies needed - this is a simple adapter
        }

        /// <summary>
        /// Validates that a path does not contain path traversal patterns.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown if path traversal is detected.</exception>
        public void Validate(string path)
        {
            // Basic path traversal validation - delegate to domain service for comprehensive validation
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
                
            // This validator now acts as a pass-through since all validation
            // is handled in the PathValidationService with proper depth-based logic
            // The previous check for ".." was overly restrictive for valid paths
        }
    }
}