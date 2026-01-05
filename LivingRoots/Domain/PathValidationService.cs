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
        private static readonly Regex AbsolutePathPattern = new(
            @"^[a-zA-Z]:[/\\]|^[/\\]|[a-zA-Z][a-zA-Z0-9+.-]*://",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );


        // Regex patterns for detecting encoded path traversal sequences
        // Enhanced to detect more path traversal attack variations including Unicode escapes, URL encoding variations, and hex encodings
        // Improved to reduce false positives by being more specific about dangerous patterns
        private static readonly Regex EncodedTraversalPattern = new(
            @"(?:%2e%2e%2[fF]|%2e%2e[/\\]|%2e%2e%0|%%32[eE]%%32[fF]|%25%32%65%25%32%65%25%32%66|%252[eE]%252[eE][/\\%3F%5C%2F]|%c0%ae%c0%ae|%e0%80%ae%e0%80%ae|%f0%80%80%ae%f0%80%80%ae|%c0%2e%c0%2e|%c0%2[fF]|%c0%5[cC]|%c0%af|%e2%80%a5%e2%80%a5%e2%80%a5|%ef%bc%8[fF]%ef%bc%8[eE]%ef%bc%8[eE]%ef%bc%8[fF]|%ef%bc%9[cC]%ef%bc%9[eE]%ef%bc%9[cC]%ef%bc%9[eE]|\.%252[eE]|%252[eE]\.|%252[eE]%252[eE]|\.%00\.|%00\.\.|%u02e%u02e%u002[fF]|%u02e%u002e%u005[cC]|%uff0[eE]%uff0[eE]|%u2024%u2024|%u2025%u2025|%u2026%u2026|%u302e%u3002|%uff0[fF]|%uff3[cC]|%u221[56])",
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
            var normalizedPath = _unicodeNormalizationService.Normalize(path);

            // Security fix: Add null check for normalizedPath to prevent validation bypass
            if (normalizedPath == null)
                throw new ArgumentException("Path normalization returned null, validation cannot proceed", nameof(path));

            // Apply dot-homoglyph normalization for comprehensive security
            var processedPath = NormalizePath(normalizedPath);

            // Run all essential validation checks
            ValidatePathSecurity(processedPath);
            ValidatePathTraversalDepth(processedPath);
            ValidatePathContainment(processedPath);
        }

        /// <summary>
        /// Normalizes a path by canonicalizing separators and mapping dot-homoglyphs to ASCII '.'
        /// </summary>
        /// <param name="path">The path to normalize</param>
        /// <returns>The normalized path</returns>
        private static string NormalizePath(string path)
        {
            // Canonicalize separators - handle various Unicode separators and normalize them to forward slash
            var normalized = path.Replace('\\', '/')
                                   .Replace('\u002f', '/')    // SOLIDUS (/)
                                   .Replace('\u005c', '/')    // REVERSE SOLIDUS (\) - normalized to forward slash
                                   .Replace('\u2044', '/')    // FRACTION SLASH
                                   .Replace('\u2215', '/')    // DIVISION SLASH
                                   .Replace('\ufe6f', '/')    // SMALL REVERSE SOLIDUS
                                   .Replace('\uff0f', '/');   // FULLWIDTH SOLIDUS

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
        private static void ValidatePathTraversalDepth(string path)
        {
            // Check for standalone "." - this should still be blocked as it represents current directory traversal
            if (path.Equals(".", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }

            // Check for standalone "./" - this should be blocked as it represents current directory navigation
            if (path.Equals("./", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }

            // Split into segments ignoring empty parts from repeated separators
            // Use the hardened separator detection to split on all possible separator characters
            var segments = SplitPathSegments(path);

            // Add a hard cap to prevent excessive processing of pathological inputs
            // Increased from 100 to allow more reasonable paths while still preventing abuse
            const int MaxSegments = 10;
            if (segments.Length > MaxSegments)
            {
                throw new ArgumentException("Path contains too many segments", nameof(path));
            }

            var depth = 0;

            foreach (var segment in segments)
            {
                if (IsPathTraversalSegment(segment))
                {
                    depth--;
                    // If depth goes negative, it means we're trying to go above the intended root
                    if (depth < 0)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
                    }
                }
                else if (!IsCurrentDirectorySegment(segment))
                {
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
        /// Validates path containment to ensure the path is properly contained and cannot escape the intended root directory.
        /// This provides an additional layer of security beyond the depth-based analysis.
        /// </summary>
        /// <param name="path">The normalized path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when path containment is violated.</exception>
        private static void ValidatePathContainment(string path)
        {
            // Check for relative path components that could lead outside the intended directory
            // This check looks for patterns that could escape the root directory

            // Check if the path starts with parent directory navigation patterns - removing redundant checks
            if (path.StartsWith("../") || path.StartsWith("..\\"))
            {
                throw new ArgumentException("Path cannot start with parent directory navigation", nameof(path));
            }

            // Additional containment validation: ensure the path doesn't contain patterns that could
            // lead to directory traversal beyond intended boundaries
            var segments = SplitPathSegments(path);

            // Count parent directory references to ensure they don't exceed the number of actual directory levels
            var parentDirCount = 0;
            var actualDirCount = 0;

            foreach (var segment in segments)
            {
                if (IsPathTraversalSegment(segment))
                {
                    parentDirCount++;
                    // If parent directory count exceeds actual directory count, it's a containment violation
                    if (parentDirCount > actualDirCount)
                    {
                        throw new ArgumentException("Path contains invalid containment pattern", nameof(path));
                    }
                }
                else if (!IsCurrentDirectorySegment(segment))
                {
                    // Regular directory/file names increase the actual directory count
                    actualDirCount++;
                    // Reset parent count since we've moved deeper into the directory structure
                    if (parentDirCount > 0)
                    {
                        // Decrement parent count when we go deeper, but don't let it go negative
                        parentDirCount = System.Math.Max(0, parentDirCount - 1);
                    }
                }
            }

            // Final check: if there are more parent directory references than actual directories,
            // the path would attempt to escape the intended root
            if (parentDirCount > actualDirCount)
            {
                throw new ArgumentException("Path contains invalid containment pattern", nameof(path));
            }
        }

        /// <summary>
        /// Splits a path into segments using hardened separator detection to handle various Unicode separators.
        /// </summary>
        /// <param name="path">The path to split into segments</param>
        /// <returns>An array of path segments</returns>
        private static string[] SplitPathSegments(string path)
        {
            // Create a character array that includes all possible path separators
            char[] separators = {
                '/',                    // Forward slash
                '\\',                   // Backslash
                '\u002f',              // SOLIDUS
                '\u005c',              // REVERSE SOLIDUS
                '\u2044',              // FRACTION SLASH
                '\u2215',              // DIVISION SLASH
                '\ufe6f',              // SMALL REVERSE SOLIDUS
                '\uff0f'               // FULLWIDTH SOLIDUS
            };

            return path.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Checks if a path segment is a path traversal segment (represents parent directory navigation).
        /// </summary>
        /// <param name="segment">The path segment to check</param>
        /// <returns>True if the segment represents parent directory navigation, false otherwise</returns>
        private static bool IsPathTraversalSegment(string segment)
        {
            return segment.Equals("..", StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if a path segment represents the current directory.
        /// </summary>
        /// <param name="segment">The path segment to check</param>
        /// <returns>True if the segment represents current directory, false otherwise</returns>
        private static bool IsCurrentDirectorySegment(string segment)
        {
            return segment.Equals(".", StringComparison.Ordinal);
        }

        /// <summary>
        /// Validates path security by checking for absolute paths/URIs and encoded traversal patterns.
        /// Consolidated validation method that combines both security checks.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <exception cref="ArgumentException">Thrown when path is absolute/URI or contains encoded traversal</exception>
        private static void ValidatePathSecurity(string path)
        {
            // Check if path is an absolute path or URI
            if (AbsolutePathPattern.IsMatch(path))
            {
                throw new ArgumentException("Path cannot be an absolute path or URI", nameof(path));
            }

            // Check if path contains encoded traversal patterns
            if (EncodedTraversalPattern.IsMatch(path))
            {
                throw new ArgumentException("Path cannot contain encoded path traversal patterns", nameof(path));
            }
        }
    }
}
