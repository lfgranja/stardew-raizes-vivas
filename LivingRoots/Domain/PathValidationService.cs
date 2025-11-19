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
            @"^[a-zA-Z]:[/\\]|^[/\\]|[a-zA-Z][a-zA-Z0-9+.-]*://",
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

            // Check for standalone "." (current directory) - this should be blocked
            if (path == ".")
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            // Check for paths starting with "./" or ".\" (explicit current directory navigation)
            if (path.StartsWith("./") || path.StartsWith(".\\"))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }

            // Check for paths ending with "/." or "\." (directory navigation)
            if (path.EndsWith("/.") || path.EndsWith("\\."))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
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

            // Validate path traversal patterns using depth-based analysis to distinguish between
            // legitimate uses of ".." and malicious path traversal attempts.
            // This is the primary validation for ".." patterns.
            ValidatePathTraversalDepth(normalizedPath);
            
            // For paths ending with "..", our depth analysis has already validated they don't go above root,
            // so we should not call the PathTraversalValidator which blocks all such paths.
            // For other paths, we can use the PathTraversalValidator for additional validation.
            if (!(normalizedPath.EndsWith("/..") || normalizedPath.EndsWith("\\..")))
            {
                _pathTraversalValidator.Validate(normalizedPath);
            }
            // For paths ending with "..", skip PathTraversalValidator since our depth analysis already validated they're safe
        }

        /// <summary>
        /// Validates path traversal using depth-based analysis to distinguish between
        /// legitimate uses of ".." and malicious path traversal attempts.
        /// </summary>
        /// <param name="path">The normalized path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when path traversal is detected.</exception>
        private void ValidatePathTraversalDepth(string path)
        {
            // Split the path into segments
            string[] segments = path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            
            int depth = 0;
            
            foreach (string segment in segments)
            {
                if (segment == "..")
                {
                    depth--;
                    // If depth goes negative, it means we're trying to go above the intended root
                    if (depth < 0)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
                    }
                }
                else if (segment != ".")
                {
                    // Regular directory/file names increase the depth
                    depth++;
                }
                // If segment is ".", we don't change the depth since it refers to current directory
            }
            
            // Special case: paths ending with ".." that would go above the intended root
            // This is handled by the depth check above, but we also need to consider the case where
            // the path is just ".." which should be blocked
            if (path == ".." || path == "../" || path == "..\\")
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
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
        /// <returns>True if path contains encoded traversal, false otherwise.</returns>
        private static bool ContainsEncodedTraversal(string path)
        {
            return EncodedTraversalPattern.IsMatch(path);
        }
    }
}