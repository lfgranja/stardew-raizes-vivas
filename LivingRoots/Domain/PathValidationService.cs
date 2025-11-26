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
            @"(?:%2e%2e%2[fF]|%2e%2e[/\\]|%2e%2e%00|%%32[eE]%%32[fF]|%25%32%65%25%32%65%25%32%66|%252[eE]%252[eE][/\\%3F%5C%2F]|%c0%ae%c0%ae|%e0%80%ae%e0%80%ae|%f0%80%80%ae%f0%80%80%ae|%c0%2e%c0%2e|%c0%2[fF]|%c0%5[cC]|%c0%af|%e2%80%a5%e2%80%a5%e2%80%a5|%ef%bc%8[fF]%ef%bc%8[eE]%ef%bc%8[eE]%ef%bc%8[fF]|%ef%bc%9[cC]%ef%bc%9[eE]%ef%bc%9[cC]%ef%bc%9[eE]|\.%252[eE]|%252[eE]\.|%252[eE]%252[eE]|\.%00\.|%00\.\.|%u02e%u02e%u002[fF]|%u002e%u002e%u005[cC]|%uff0[eE]%uff0[eE]|%u2024%u2024|%u2025%u2025|%u2026%u2026|%u302e%u3002|%uff0[fF]|%uff3[cC]|%u221[56])",
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
            string? normalizedPath = _unicodeNormalizationService.Normalize(path);
            
            // Security fix: Add null check for normalizedPath to prevent validation bypass
            if (normalizedPath == null)
                throw new ArgumentException("Path normalization returned null, validation cannot proceed", nameof(path));

            // Run all validation checks - include essential security validations that were previously in separate methods
            ValidateStandaloneDot(normalizedPath);
            ValidateStandaloneDotDot(normalizedPath);
            ValidateDotSlashAtStart(normalizedPath);
            ValidateDotDotSlashAtStart(normalizedPath);
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
            // Canonicalize separators
            string normalized = path.Replace('\\', '/');

            // Map dot-homoglyphs used in traversal tricks to ASCII '.'
            // U+2024 (ONE DOT LEADER), U+2025 (TWO DOT LEADER), U+2026 (HORIZONTAL ELLIPSIS), U+FF0E (FULLWIDTH FULL STOP)
            normalized = normalized
                .Replace('\u2024', '.')
                .Replace('\u2025', '.')
                .Replace('\u2026', '.')
                .Replace('\uFF0E', '.');

            // Split into segments ignoring empty parts from repeated separators
            string[] segments = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            
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
            // Need to apply the same normalization to the special case checks
            string specialCaseNormalized = path.Replace('\\', '/').Replace('\u2024', '.').Replace('\u2025', '.').Replace('\u2026', '.').Replace('\uFF0E', '.');
            if (specialCaseNormalized.Equals("..", StringComparison.Ordinal) || specialCaseNormalized.Equals("../", StringComparison.Ordinal))
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
            // Apply the same normalization for consistency
            string normalized = path.Replace('\\', '/').Replace('\u2024', '.').Replace('\u2025', '.').Replace('\u2026', '.').Replace('\uFF0E', '.');
            if (normalized.Equals(".", StringComparison.Ordinal))
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
            // Apply the same normalization for consistency
            string normalized = path.Replace('\\', '/').Replace('\u2024', '.').Replace('\u2025', '.').Replace('\u2026', '.').Replace('\uFF0E', '.');
            if (normalized.Equals("..", StringComparison.Ordinal) || normalized.Equals("../", StringComparison.Ordinal) || normalized.Equals("..\\", StringComparison.Ordinal))
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
            // Apply the same normalization for consistency
            string normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("./", StringComparison.Ordinal))
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
            // Apply the same normalization for consistency
            string normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("../", StringComparison.Ordinal))
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