using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation for sanitizing filenames to make them safe for file system operations.
    /// </summary>
    public class FileNameSanitizer : IFileNameSanitizer
    {
        private const int MaxFileNameLength = 240; // Maximum filename length for truncation tests
        
        private readonly IUnicodeNormalizer _unicodeNormalizer;

        public FileNameSanitizer(IUnicodeNormalizer unicodeNormalizer)
        {
            _unicodeNormalizer = unicodeNormalizer ?? throw new ArgumentNullException(nameof(unicodeNormalizer));
        }

        /// <summary>
        /// Sanitizes a filename by removing or replacing invalid characters and handling security concerns.
        /// </summary>
        /// <param name="filename">The filename to sanitize.</param>
        /// <returns>The sanitized filename.</returns>
        /// <exception cref="ArgumentException">Thrown when filename sanitizes to an empty string or is too long.</exception>
        public string? Sanitize(string? filename)
        {
            if (filename == null)
                return null;
                
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("Filename cannot be empty or whitespace-only.", nameof(filename));

            if (filename.Contains('\0'))
                throw new ArgumentException("Filename cannot contain null characters.", nameof(filename));

            // Additional security check for potential path traversal
            if (filename.Contains("../") || filename.Contains("..\\") || 
                filename.StartsWith("..") || filename.EndsWith(".."))
                throw new ArgumentException("Filename cannot contain path traversal sequences.", nameof(filename));

            // Normalize Unicode characters first
            string normalized = _unicodeNormalizer.Normalize(filename);
            
            if (normalized == null)
                throw new ArgumentException("Normalized filename is null.", nameof(filename));

            // Extract extension before sanitizing the name
            string extension = GetFileExtension(normalized);
            string nameWithoutExtension = RemoveFileExtension(normalized);

            // Sanitize characters by replacing invalid ones (this follows the original approach but with security enhancements)
            string sanitized = SanitizeInvalidCharacters(nameWithoutExtension);

            // Process consecutive dots (this should be done after character sanitization)
            string processed = ProcessConsecutiveDots(sanitized);

            // Check if the processed filename would become "." or ".." after trimming problematic characters
            // This validation must happen before any trimming to prevent bypassing safeguards
            string processedTrimmed = processed.Trim('_', ' ', '.');
            if (processedTrimmed == "." || processedTrimmed == "..")
                throw new ArgumentException($"Filename sanitizes to invalid path component '{processedTrimmed}'.", nameof(filename));

            // Determine if this should be treated as a hidden file
            bool shouldBeHiddenFile = ShouldPreserveHiddenFilePrefix(filename, processed);

            // Trim leading/trailing problematic characters (but preserve leading dots for hidden files)
            string trimmed;
            if (shouldBeHiddenFile && processed.StartsWith("."))
            {
                // For hidden files, preserve the leading dot and only trim the rest
                string contentAfterDot = processed.Substring(1);
                string trimmedContent = contentAfterDot.TrimEnd('_', ' ', '.');
                trimmed = "." + trimmedContent;
            }
            else
            {
                // For non-hidden files, trim from both ends
                trimmed = processed.Trim('_', ' ', '.');
            }

            // Preserve leading dots for hidden files if not already present and content is not empty
            if (shouldBeHiddenFile && !trimmed.StartsWith(".") && !string.IsNullOrEmpty(trimmed))
            {
                trimmed = "." + trimmed;
            }

            // Apply truncation
            string truncated = TruncateToMaxLength(trimmed);

            // Final cleanup after truncation
            string result = PerformFinalCleanup(truncated, shouldBeHiddenFile);

            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));

            // Final check to ensure we don't return "." or ".." which could lead to path traversal issues
            if (result == "." || result == "..")
                throw new ArgumentException($"Filename sanitizes to invalid path component '{result}'.", nameof(filename));

            // Add extension back if it was present and safe
            if (!string.IsNullOrEmpty(extension))
            {
                // Check if the extension is in the blocked list
                if (IsBlockedExtension(extension))
                {
                    // Replace dangerous extension with a safe indicator
                    result += "_blocked" + extension;
                }
                else
                {
                    result += extension;
                }
            }

            return result;
        }

        /// <summary>
        /// Sanitizes invalid characters by using an allowlist approach.
        /// Only allows alphanumeric characters, dots, hyphens, underscores, and valid surrogate pairs (emojis).
        /// Invalid characters are replaced with underscores, but consecutive invalid characters
        /// are consolidated to a single underscore.
        /// </summary>
        /// <param name="input">The input string to sanitize</param>
        /// <returns>The sanitized string</returns>
        private static string SanitizeInvalidCharacters(string? input)
        {
            if (input == null)
                return null!;
                
            var resultBuilder = new StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                
                // Handle surrogate pairs (needed for emojis and other characters outside BMP)
                if (char.IsHighSurrogate(c) && i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                {
                    // Preserve valid surrogate pairs (emojis, etc.) as they will be handled by Unicode normalization
                    resultBuilder.Append(c);
                    resultBuilder.Append(input[i + 1]);
                    i++; // Skip the low surrogate since we've already processed it
                    continue;
                }
                
                // Only allow safe characters: alphanumeric, dots, hyphens, underscores
                if (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')
                {
                    resultBuilder.Append(c);
                }
                else
                {
                    // Replace invalid characters with underscores, but only if the last character isn't already an underscore
                    if (resultBuilder.Length == 0 || resultBuilder[resultBuilder.Length - 1] != '_')
                    {
                        resultBuilder.Append('_');
                    }
                }
            }

            return resultBuilder.ToString();
        }

        /// <summary>
        /// Processes consecutive dots by replacing multiple consecutive dots with a single dot.
        /// </summary>
        /// <param name="input">The input string to process</param>
        /// <returns>The processed string</returns>
        private static string ProcessConsecutiveDots(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            // Replace multiple consecutive dots with a single dot
            var result = new StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                
                if (c == '.')
                {
                    // Add the dot only if the previous character wasn't a dot
                    if (result.Length == 0 || result[result.Length - 1] != '.')
                    {
                        result.Append('.');
                    }
                    // If previous character was a dot, skip this one
                }
                else
                {
                    result.Append(c);
                }
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Determines if the filename should preserve the hidden file prefix.
        /// </summary>
        /// <param name="originalFilename">The original filename</param>
        /// <param name="processedFilename">The processed filename</param>
        /// <returns>True if the hidden file prefix should be preserved</returns>
        private static bool ShouldPreserveHiddenFilePrefix(string originalFilename, string processedFilename)
        {
            return originalFilename.StartsWith(".") && !string.IsNullOrEmpty(processedFilename.Trim('_', ' ', '.'));
        }

        /// <summary>
        /// Truncates the filename to the maximum allowed length, handling hidden files properly.
        /// </summary>
        /// <param name="filename">The filename to truncate</param>
        /// <returns>The truncated filename</returns>
        private static string TruncateToMaxLength(string filename)
        {
            if (filename.Length <= MaxFileNameLength)
                return filename;

            // If the filename is too long, truncate it
            if (filename.StartsWith("."))
            {
                // For hidden files, keep the dot and truncate the content part
                string contentPart = filename.Substring(1);
                string truncatedContent = SafeSubstring(contentPart, 0, MaxFileNameLength - 1);
                return "." + truncatedContent;
            }
            else
            {
                // Truncate to max length
                return SafeSubstring(filename, 0, MaxFileNameLength);
            }
        }

        /// <summary>
        /// Performs final cleanup after truncation.
        /// </summary>
        /// <param name="filename">The filename after truncation</param>
        /// <param name="shouldBeHiddenFile">Whether this should be treated as a hidden file</param>
        /// <returns>The cleaned up filename</returns>
        private static string PerformFinalCleanup(string filename, bool shouldBeHiddenFile)
        {
            // After truncation, ensure we don't have trailing problematic characters
            string postTruncationTrimmed = filename.TrimEnd('_', ' ', '.');
            
            // If it was a hidden file and we lost the dot, add it back
            if (shouldBeHiddenFile && !postTruncationTrimmed.StartsWith(".") && !string.IsNullOrEmpty(postTruncationTrimmed))
            {
                postTruncationTrimmed = "." + postTruncationTrimmed.TrimStart('.');
            }
            
            // If the final result is still longer than max length, truncate again
            if (postTruncationTrimmed.Length > MaxFileNameLength)
            {
                return TruncateToMaxLength(postTruncationTrimmed);
            }
            
            return postTruncationTrimmed;
        }

        /// <summary>
        /// Gets the file extension from a filename.
        /// </summary>
        /// <param name="filename">The filename to extract extension from.</param>
        /// <returns>The file extension or empty string if no extension.</returns>
        private static string GetFileExtension(string filename)
        {
            int lastDotIndex = filename.LastIndexOf('.');
            if (lastDotIndex > 0 && lastDotIndex < filename.Length - 1) // Ensure dot is not at the beginning or end
            {
                // Make sure the part after the last dot looks like an extension (not part of a directory path)
                string potentialExtension = filename.Substring(lastDotIndex);
                if (potentialExtension.IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
                {
                    return potentialExtension;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Removes the file extension from a filename.
        /// </summary>
        /// <param name="filename">The filename to remove extension from.</param>
        /// <returns>The filename without extension.</returns>
        private static string RemoveFileExtension(string filename)
        {
            int lastDotIndex = filename.LastIndexOf('.');
            if (lastDotIndex > 0 && lastDotIndex < filename.Length - 1)
            {
                string potentialExtension = filename.Substring(lastDotIndex);
                if (potentialExtension.IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
                {
                    return filename.Substring(0, lastDotIndex);
                }
            }
            return filename;
        }

        /// <summary>
        /// Checks if a character is invalid or problematic for filenames.
        /// Uses a blacklist approach to identify problematic characters.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is invalid or problematic, false otherwise.</returns>
        private static bool IsInvalidOrProblematicChar(char c)
        {
            // Control characters (except tab, carriage return, line feed which are whitespace) are invalid
            if (char.IsControl(c) && c != '\t' && c != '\r' && c != '\n')
                return true;
                
            // Check against system invalid file name characters
            if (Path.GetInvalidFileNameChars().Contains(c))
                return true;

            // Additional problematic characters
            switch (c)
            {
                case '<':
                case '>':
                case ':':
                case '"':
                case '/':
                case '\\':
                case '|':
                case '?':
                case '*':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if an extension should be blocked for security.
        /// </summary>
        /// <param name="extension">The extension to check.</param>
        /// <returns>True if the extension should be blocked, false otherwise.</returns>
        private static bool IsBlockedExtension(string extension)
        {
            var blockedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".exe", ".dll", ".bat", ".sh", ".ps1", ".cmd", ".com", ".scr", ".pif", ".lnk", 
                ".msi", ".msp", ".vbs", ".js", ".jse", ".wsf", ".wsh", ".hta", ".cpl", ".msc", ".inf"
            };
            
            return blockedExtensions.Contains(extension);
        }

        /// <summary>
        /// Safely extracts a substring without splitting surrogate pairs.
        /// </summary>
        /// <param name="str">The input string</param>
        /// <param name="startIndex">Start index</param>
        /// <param name="length">Length to extract</param>
        /// <returns>The substring</returns>
        private static string SafeSubstring(string str, int startIndex, int length)
        {
            // Make sure we don't exceed the string length
            if (startIndex >= str.Length)
                return string.Empty;
                
            int endIndex = Math.Min(startIndex + length, str.Length);
            
            // Check if we're potentially splitting a surrogate pair
            // If the character at endIndex is a low surrogate and the one before it is a high surrogate,
            // we should exclude the high surrogate to avoid splitting the pair
            if (endIndex < str.Length && char.IsLowSurrogate(str[endIndex]) && 
                endIndex > 0 && char.IsHighSurrogate(str[endIndex - 1]))
            {
                endIndex--; // Avoid splitting the surrogate pair
            }
            
            return str.Substring(startIndex, endIndex - startIndex);
        }
    }
}