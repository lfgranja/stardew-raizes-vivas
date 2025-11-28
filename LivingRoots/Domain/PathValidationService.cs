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

            // Run all essential validation checks
            ValidateAbsolutePathOrUri(normalizedPath);
            ValidateEncodedTraversal(normalizedPath);
            ValidatePathTraversalDepth(normalizedPath);
        }

        /// <summary>
        /// Normalizes a path by canonicalizing separators and mapping dot-homoglyphs to ASCII '.'
        /// </summary>
        /// <param name="path">The path to normalize</param>
        /// <returns>The normalized path</returns>
        private string NormalizePath(string path)
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
            
            return normalized;
        }

        /// <summary>
        /// Validates path traversal using depth-based analysis to distinguish between
        /// legitimate uses of ".." and malicious path traversal attempts.
        /// </summary>
        /// <param name="path">The normalized path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when path traversal is detected.</exception>
        private void ValidatePathTraversalDepth(string path)
        {
            string normalized = NormalizePath(path);
            
            // Check for standalone "." - this should still be blocked as it represents current directory traversal
            if (normalized.Equals(".", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            // Check for standalone "./" - this should be blocked as it represents current directory navigation
            if (normalized.Equals("./", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            // Block any path that starts with "./" as this represents explicit current directory navigation
            if (normalized.StartsWith("./", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            // Check for standalone "..", "../", or "..\"
            if (normalized.Equals("..", StringComparison.Ordinal) || 
                normalized.Equals("../", StringComparison.Ordinal) || 
                normalized.Equals("..\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            // Split into segments ignoring empty parts from repeated separators
            string[] segments = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Add a hard cap to prevent excessive processing of pathological inputs
            // Increased from 100 to allow more reasonable paths while still preventing abuse
            const int MaxSegments = 1000;
            if (segments.Length > MaxSegments)
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
            
            int depth = 0;
            
            foreach (string segment in segments)
            {
                // Check for integer overflow before decrementing
                if (segment.Equals("..", StringComparison.Ordinal))
                {
                    // Prevent integer underflow by checking bounds
                    if (depth <= int.MinValue + 1)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
                    }
                    depth--;
                    // If depth goes negative, it means we're trying to go above the intended root
                    if (depth < 0)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
                    }
                }
                else if (!segment.Equals(".", StringComparison.Ordinal))
                {
                    // Check for integer overflow before incrementing
                    if (depth >= int.MaxValue - 1)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
                    }
                    // Regular directory/file names increase the depth
                    depth++;
                }
                // If segment is ".", we don't change the depth since it refers to current directory
            }
            
            // Remove the arbitrary depth cap of 10 that was limiting legitimate use cases
            // The depth < 0 check already prevents traversal above root
            // This allows deeper, legitimate directory structures
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