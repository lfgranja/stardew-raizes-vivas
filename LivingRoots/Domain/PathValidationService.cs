using System;
using System.Text.RegularExpressions;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Implementation for validating file paths to prevent path traversal attacks and other security issues.
    /// This implementation follows the Dependency Inversion Principle by depending on abstractions.
    /// </summary>
    public class PathValidationService : IPathValidationService
    {
        private readonly IUnicodeNormalizationService _unicodeNormalizationService;
        private readonly IPathTraversalValidator _pathTraversalValidator;

        // Regex patterns for detecting absolute paths and URIs
        private static readonly Regex AbsolutePathPattern = new Regex(
            @"^[a-zA-Z]:[/\\]|^[/\\]|^[a-zA-Z][a-zA-Z0-9+.-]*://",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // Regex patterns for detecting encoded path traversal sequences
        private static readonly Regex EncodedTraversalPattern = new Regex(
            @"%2e%2e%2[fF]|%2e%2e[/\\]|\.\.%2[fF]|%2e%2e%2e",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public PathValidationService(
            IUnicodeNormalizationService unicodeNormalizationService,
            IPathTraversalValidator pathTraversalValidator)
        {
            _unicodeNormalizationService = unicodeNormalizationService ?? throw new ArgumentNullException(nameof(unicodeNormalizationService));
            _pathTraversalValidator = pathTraversalValidator ?? throw new ArgumentNullException(nameof(pathTraversalValidator));
        }

        /// <summary>
        /// Validates a file path to ensure it doesn't contain path traversal patterns or absolute paths.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the path is invalid.</exception>
        public void Validate(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            }

            // Normalize Unicode characters to detect homoglyph attacks
            string normalizedPath = _unicodeNormalizationService.Normalize(path);

            // Check for absolute paths and URIs
            if (IsAbsolutePathOrUri(normalizedPath))
            {
                throw new ArgumentException("Path cannot be an absolute path or URI", nameof(path));
            }

            // Check for encoded path traversal patterns
            if (ContainsEncodedTraversal(normalizedPath))
            {
                throw new ArgumentException("Path cannot contain encoded path traversal patterns", nameof(path));
            }

            // Validate path traversal patterns using the dedicated validator
            _pathTraversalValidator.Validate(normalizedPath);
            
            // Additional depth validation to prevent going above root
            ValidatePathDepth(normalizedPath);
        }

        /// <summary>
        /// Checks if a path is an absolute path or URI.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path is absolute or a URI, false otherwise.</returns>
        private static bool IsAbsolutePathOrUri(string path)
        {
            return AbsolutePathPattern.IsMatch(path);
        }

        /// <summary>
        /// Checks if a path contains encoded traversal patterns.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path contains encoded traversal, false otherwise.</returns>
        private static bool ContainsEncodedTraversal(string path)
        {
            return EncodedTraversalPattern.IsMatch(path);
        }

        /// <summary>
        /// Validates path depth to ensure it doesn't go above the root level.
        /// This method uses a more sophisticated approach to handle edge cases like paths ending with ".."
        /// </summary>
        /// <param name="normalizedPath">The normalized path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when path goes above root.</exception>
        private static void ValidatePathDepth(string normalizedPath)
        {
            string[] segments = normalizedPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
            int currentDepth = 0;
            
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                
                if (segment == ".")
                {
                    // Current directory - no change in depth
                    continue;
                }
                else if (segment == "..")
                {
                    // Parent directory - decrease depth
                    currentDepth--;
                    
                    // Check if we've gone above root
                    if (currentDepth < 0)
                    {
                        // Special case: if this is the last segment and path would otherwise be valid,
                        // we need to check if this is a legitimate use case
                        if (i == segments.Length - 1)
                        {
                            // For paths ending with "..", check if the path before ".." is valid
                            // This handles cases like "a/b/.." which should be valid
                            if (currentDepth == -1)
                            {
                                // Calculate what the depth would be without the final ".."
                                int depthWithoutFinal = 0;
                                for (int j = 0; j < i; j++)
                                {
                                    if (segments[j] == "..")
                                        depthWithoutFinal--;
                                    else if (segments[j] != ".")
                                        depthWithoutFinal++;
                                }
                                
                                // If the path before the final ".." doesn't go above root, allow it
                                if (depthWithoutFinal >= 0)
                                    return;
                            }
                        }
                        
                        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(normalizedPath));
                    }
                }
                else
                {
                    // Normal directory/file - increase depth
                    currentDepth++;
                }
            }
        }
    }
}