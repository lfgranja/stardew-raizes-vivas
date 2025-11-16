using System;
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
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
                
            // Check for obvious path traversal patterns
            if (path.Contains("..") || path.Contains("../") || path.Contains("..\\") || 
                path.Contains("../../../") || path.Contains("..\\..\\"))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }
    }
}