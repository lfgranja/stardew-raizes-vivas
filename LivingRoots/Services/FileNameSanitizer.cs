using System;
using System.IO;
using System.Linq;
using System.Text;
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

            // Normalize Unicode characters
            string normalized = _unicodeNormalizer.Normalize(filename);

            // Sanitize characters by replacing invalid ones
            string sanitized = SanitizeInvalidCharacters(normalized);

            // Process consecutive dots
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

            return result;
        }

        /// <summary>
        /// Sanitizes invalid characters by replacing them with underscores.
        /// </summary>
        /// <param name="input">The input string to sanitize</param>
        /// <returns>The sanitized string</returns>
        private static string SanitizeInvalidCharacters(string? input)
        {
            if (input == null)
                return null!;
                
            var sanitizedBuilder = new StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                
                // Handle surrogate pairs (emojis and other multi-byte Unicode characters)
                if (char.IsHighSurrogate(c) && i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                {
                    // Preserve valid surrogate pairs (emojis, etc.)
                    char lowSurrogate = input[i + 1];
                    int codePoint = char.ConvertToUtf32(c, lowSurrogate);
                    
                    // Check if this is a valid Unicode character
                    if (char.GetUnicodeCategory(char.ConvertFromUtf32(codePoint)[0]) != UnicodeCategory.OtherNotAssigned)
                    {
                        sanitizedBuilder.Append(c);
                        sanitizedBuilder.Append(lowSurrogate);
                        i++; // Skip the low surrogate as we've already processed it
                    }
                    else
                    {
                        // Replace invalid surrogate pairs with underscore
                        if (sanitizedBuilder.Length == 0 || sanitizedBuilder[sanitizedBuilder.Length - 1] != '_')
                            sanitizedBuilder.Append('_');
                    }
                    continue;
                }
                else if (IsInvalidOrProblematicChar(c))
                {
                    // Only add underscore if it's not already the last character
                    if (sanitizedBuilder.Length == 0 || sanitizedBuilder[sanitizedBuilder.Length - 1] != '_')
                        sanitizedBuilder.Append('_');
                }
                else
                {
                    // Add valid characters including Unicode letters, numbers, etc.
                    sanitizedBuilder.Append(c);
                }
            }

            return sanitizedBuilder.ToString();
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
    }
}