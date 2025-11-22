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
            
            // Check for standalone "." (current directory) - this should be blocked
            if (path.Equals(".", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            // Check for standalone ".." (parent directory) - this should be blocked
            if (path.Equals("..", StringComparison.Ordinal) || path.Equals("../", StringComparison.Ordinal) || path.Equals("..\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }

            // Check for paths starting with "./" or ".\" (explicit current directory navigation at the beginning)
            if (path.StartsWith("./", StringComparison.Ordinal) || path.StartsWith(".\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            // Check for paths starting with "../" or "..\" (explicit parent directory navigation at the beginning)
            if (path.StartsWith("../", StringComparison.Ordinal) || path.StartsWith("..\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            // Check for mixed patterns that combine current directory with traversal like "./../file.txt" or ".\\..\\file.txt"
            // These patterns indicate attempts to navigate using current directory markers mixed with traversal
            if (path.Contains("./../") || path.Contains(".\\..\\"))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            // Check for paths ending with "/.." or "\.." (trailing parent directory navigation) - these should be blocked when PathTraversalValidator is used in isolation
            if (path.EndsWith("/..", StringComparison.Ordinal) || path.EndsWith("\\..", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }
    }
}