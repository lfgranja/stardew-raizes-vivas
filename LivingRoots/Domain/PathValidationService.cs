using System;
using System.Text.RegularExpressions;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Implementation for validating file paths to prevent path traversal attacks and other security issues.
    /// All path validation logic is consolidated in this service to reduce redundancy.
    /// </summary>
    public class PathValidationService : IPathValidationService
    {
        private readonly IUnicodeNormalizationService _unicodeNormalizationService;

        // Regex patterns for detecting absolute paths and URIs
        private static readonly Regex AbsolutePathPattern = new Regex(
            @"^[a-zA-Z]:[/\\]|^[/\\]|[a-zA-Z][a-zA-Z0-9+.-]*://",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );


        // Regex patterns for detecting encoded path traversal sequences
        // Enhanced to detect more path traversal attack variations including Unicode escapes, URL encoding variations, and hex encodings
        // Improved to reduce false positives by being more specific about dangerous patterns
        private static readonly Regex EncodedTraversalPattern = new Regex(
            @"(?:%2e%2e%2[fF]|%2e%2e[/\\]|%2e%2e%00|%%32[eE]%%32[fF]|%25%32%65%25%32%65%25%32%66|%252[eE]%252[eE][/\\%3F%5C%2F]|%c0%ae%c0%ae|%e0%80%ae%e0%80%ae|%f0%80%80%ae%f0%80%80%ae|%c0%2e%c0%2e|%c0%2[fF]|%c0%5[cC]|%c0%af|%e2%80%a5%e2%80%a5%e2%80%a5|%ef%bc%8[fF]%ef%bc%8[eE]%ef%bc%8[eE]%ef%bc%8[fF]|%ef%bc%9[cC]%ef%bc%9[eE]%ef%bc%9[cC]%ef%bc%9[eE]|\.%252[eE]|%252[eE]\.|%252[eE]%252[eE]|\.%00\.|%00\.\.|%u002e%u002e%u002[fF]|%u002e%u002e%u005[cC]|%uff0[eE]%uff0[eE]|%u2024%u2024|%u2025%u2025|%u2026%u2026|%u302e%u3002|%uff0[fF]|%uff3[cC]|%u221[56])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public PathValidationService(
            IUnicodeNormalizationService unicodeNormalizationService)
        {
            _unicodeNormalizationService = unicodeNormalizationService ?? throw new ArgumentNullException(nameof(unicodeNormalizationService));
        }

        /// <summary>
        /// Validates a file path to ensure it doesn't contain path traversal patterns or absolute paths.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the path is invalid.</exception>
        public void Validate(string path)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            
            // Normalize Unicode characters to detect homoglyph attacks - this must be done first
            string normalizedPath = _unicodeNormalizationService.Normalize(path);

            // Run all validation checks
            ValidateStandaloneDot(normalizedPath);

            ValidateStandaloneDotDot(normalizedPath);  // Added this call to remove the duplicate method
            ValidateDotSlashAtStart(normalizedPath);
            ValidateDotDotSlashAtStart(normalizedPath);
            ValidateMixedDotTraversal(normalizedPath);
            ValidateAbsolutePathOrUri(normalizedPath);
            ValidateEncodedTraversal(normalizedPath);
            ValidatePathTraversalDepth(normalizedPath);
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
                if (segment.Equals("..", StringComparison.Ordinal))
                {
                    depth--;
                    // If depth goes negative, it means we're trying to go above the intended root
                    if (depth < 0)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
                    }
                }
                else if (!segment.Equals(".", StringComparison.Ordinal))
                {
                    // Regular directory/file names increase the depth
                    depth++;
                }
                // If segment is ".", we don't change the depth since it refers to current directory
            }
            
            // Special case: paths that are just ".." which should be blocked
            if (path.Equals("..", StringComparison.Ordinal) || path.Equals("../", StringComparison.Ordinal) || path.Equals("..\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }

        /// <summary>
        /// Validates that a path is not a standalone "." (current directory)
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when path is a standalone "."</exception>
        private void ValidateStandaloneDot(string path)
        {
            if (path.Equals(".", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }

        /// <summary>
        /// Validates that a path is not a standalone ".." (parent directory)
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when path is a standalone ".."</exception>

        private void ValidateStandaloneDotDot(string path)
        {
            if (path.Equals("..", StringComparison.Ordinal) || path.Equals("../", StringComparison.Ordinal) || path.Equals("..\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }

        /// <summary>
        /// Validates that a path does not start with "./" or ".\" (explicit current directory navigation at the beginning)
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when path starts with "./" or ".\"</exception>
        private void ValidateDotSlashAtStart(string path)
        {
            if (path.StartsWith("./", StringComparison.Ordinal) || path.StartsWith(".\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }

        /// <summary>
        /// Validates that a path does not start with "../" or "..\" (explicit parent directory navigation at the beginning)
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when path starts with "../" or "..\"</exception>
        private void ValidateDotDotSlashAtStart(string path)
        {
            if (path.StartsWith("../", StringComparison.Ordinal) || path.StartsWith("..\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }

        /// <summary>
        /// Validates that a path does not contain mixed patterns that combine current directory with traversal like "./../file.txt" or ".\\..\\file.txt"
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when path contains mixed dot traversal patterns</exception>
        private void ValidateMixedDotTraversal(string path)
        {
            if (path.StartsWith("./../") || path.StartsWith(".\\..\\"))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }

        /// <summary>
        /// Checks if a path is an absolute path or URI.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <exception cref="ArgumentException">Thrown when path is absolute or URI</exception>
        private void ValidateAbsolutePathOrUri(string path)
        {
            if (AbsolutePathPattern.IsMatch(path))
            {
                throw new ArgumentException("Path cannot be an absolute path or URI", nameof(path));
            }
        }

        /// <summary>
        /// Checks if a path contains encoded traversal patterns.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <exception cref="ArgumentException">Thrown when path contains encoded traversal</exception>
        private void ValidateEncodedTraversal(string path)
        {
            if (EncodedTraversalPattern.IsMatch(path))
            {
                throw new ArgumentException("Path cannot contain encoded path traversal patterns", nameof(path));
            }
        }
    }
}