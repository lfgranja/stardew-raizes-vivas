using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Implementation for sanitizing filenames to make them safe for file system operations.
    /// This implementation follows the Dependency Inversion Principle by depending on abstractions.
    /// </summary>
    public class FileNameSanitizationService : IFileNameSanitizationService
    {
        private const int MaxFileNameLength = 240; // Maximum filename length for truncation tests
        
        private readonly IUnicodeNormalizationService _unicodeNormalizationService;
        private readonly IReservedNameHandler _reservedNameHandler;

        public FileNameSanitizationService(IUnicodeNormalizationService unicodeNormalizationService, IReservedNameHandler reservedNameHandler)
        {
            _unicodeNormalizationService = unicodeNormalizationService ?? throw new ArgumentNullException(nameof(unicodeNormalizationService));
            _reservedNameHandler = reservedNameHandler ?? throw new ArgumentNullException(nameof(reservedNameHandler));
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
            
            // Check for path traversal sequences that should be blocked
            // Check for path traversal sequences that should be blocked early (these are obvious cases)
            if (filename.Contains("../", StringComparison.Ordinal) || 
                filename.Contains("..\\", StringComparison.Ordinal) ||
                filename.Contains("..\\/", StringComparison.Ordinal) ||
                filename.Contains("../\\", StringComparison.Ordinal))
                throw new ArgumentException("Filename cannot contain path traversal sequences.", nameof(filename));
            
            // Check for suspicious path traversal patterns using a dedicated method
            if (IsSuspiciousPathTraversalPattern(filename))
            {
                throw new ArgumentException("Filename cannot contain path traversal sequences.", nameof(filename));
            }

            // Normalize Unicode characters first using the domain service
            string normalized = _unicodeNormalizationService.Normalize(filename) ?? filename;
            
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
            if (shouldBeHiddenFile && processed.StartsWith(".", StringComparison.Ordinal))
            {
                // For hidden files, preserve the leading dot and only trim the rest
                string contentAfterDot = processed.Substring(1);
                string trimmedContent = contentAfterDot.TrimEnd('_', ' ', '.');
                
                // Special handling for the case where the original filename started with a dot followed by 
                // invalid characters that were converted to underscores during sanitization.
                // For example: ".<hidden_file.txt" -> "._hidden_file.txt" -> ".hidden_file.txt"
                if (contentAfterDot.Length > 0 && contentAfterDot[0] == '_' && filename.Length > 1)
                {
                    // Check if the original character after the dot was an invalid character
                    char originalCharAfterDot = filename[1];
                    if (IsInvalidOrProblematicChar(originalCharAfterDot))
                    {
                        // Remove leading underscore since it came from sanitizing an invalid character
                        trimmedContent = contentAfterDot.TrimStart('_').TrimEnd('_', ' ', '.');
                    }
                }
                
                trimmed = "." + trimmedContent;
            }
            else
            {
                // For non-hidden files, trim from both ends
                trimmed = processed.Trim('_', ' ', '.');
            }

            // Preserve leading dots for hidden files if not already present and content is not empty
            if (shouldBeHiddenFile && !trimmed.StartsWith(".", StringComparison.Ordinal) && !string.IsNullOrEmpty(trimmed))
            {
                trimmed = "." + trimmed;
            }

            // Apply truncation
            string truncated = TruncateToMaxLength(trimmed);

            // Final cleanup after truncation
            string result = PerformFinalCleanup(truncated, shouldBeHiddenFile);

            // Check for empty result after all processing is done
            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));
                
            // Check for path traversal patterns that should result in empty string message
            // But first check if it's exactly "." or ".." which should give the empty string error
            if (result == "." || result == "..")
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));
            
            // Only check for path traversal patterns if result is not empty
            // Allow hidden files (single leading dot followed by non-dot content) but block other traversal patterns
            if (!string.IsNullOrEmpty(result))
            {
                // Check for path traversal patterns, but allow hidden files (single dot at start followed by non-dot content)
                bool isHiddenFile = result.Length > 1 && result.StartsWith(".", StringComparison.Ordinal) && result[1] != '.';
                
                if (!isHiddenFile && 
                    (result.Contains(".._", StringComparison.Ordinal) || 
                     (result.StartsWith("..", StringComparison.Ordinal) && result.Length > 2 && result[2] != '.') ||
                     (result.EndsWith("..", StringComparison.Ordinal) && result.Length > 2 && result[result.Length-3] != '.')))
                {
                    throw new ArgumentException("Filename cannot contain path traversal sequences.", nameof(filename));
                }
            }

            // Add extension back if it was present and safe
            if (!string.IsNullOrEmpty(extension))
            {
                // Check if the extension is in the blocked list
                if (IsBlockedExtension(extension))
                {
                    // Replace dangerous extension with a safe indicator
                    // Ensure base has no trailing dots before appending any suffix/extension
                    result = result.TrimEnd('.');
                    // Produce name_blocked.ext with _blocked before the extension for security
                    result = $"{result}_blocked{extension}";
                }
                else
                {
                    result = $"{result}{extension}";
                }
            }

            // Handle reserved Windows filenames
            string? reservedResult = _reservedNameHandler.Handle(result);
            
            // Check if the reserved name handler returned null
            if (reservedResult == null)
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));
                
            result = reservedResult;

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
                return string.Empty;
                
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
            // If original starts with a dot and the processed filename is not empty,
            // we should preserve the leading dot
            return originalFilename.StartsWith(".", StringComparison.Ordinal) && !string.IsNullOrEmpty(processedFilename);
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
            if (filename.StartsWith(".", StringComparison.Ordinal))
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
            if (shouldBeHiddenFile && !postTruncationTrimmed.StartsWith(".", StringComparison.Ordinal) && !string.IsNullOrEmpty(postTruncationTrimmed))
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
                // Check that the last dot is not part of a directory path segment
                // Look for directory separators after the last dot to ensure it's really an extension
                string potentialExtension = filename.Substring(lastDotIndex);
                
                // Check if the extension portion contains directory separators
                // This prevents cases like "file/path.ext" where the extension detection would be wrong
                if (potentialExtension.Contains('/', StringComparison.Ordinal) || potentialExtension.Contains('\\', StringComparison.Ordinal))
                {
                    return string.Empty; // Not a valid extension if it contains path separators
                }
                
                // Make sure the part after the last dot looks like an extension (not part of a directory path)
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
                // Check that the last dot is not part of a directory path segment
                // Look for directory separators after the last dot to ensure it's really an extension
                string potentialExtension = filename.Substring(lastDotIndex);
                
                // Check if the extension portion contains directory separators
                // This prevents cases like "file/path.ext" where the extension detection would be wrong
                if (potentialExtension.Contains('/', StringComparison.Ordinal) || potentialExtension.Contains('\\', StringComparison.Ordinal))
                {
                    return filename; // Return original if it's not a valid extension
                }
                
                string extension = filename.Substring(lastDotIndex);
                if (extension.IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
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
        
        /// <summary>
        /// Checks if the filename contains suspicious path traversal patterns.
        /// Block strings that start with ".." followed by anything other than a path separator or whitespace (like "..test", ".. ", etc.)
        /// Block strings that end with ".." preceded by anything other than a path separator or whitespace (like "test..", " ..", etc.)
        /// However, allow pure sequences of dots like "..." to be processed normally, as they'll be handled as empty strings later
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if the filename contains suspicious path traversal patterns, false otherwise</returns>
        private static bool IsSuspiciousPathTraversalPattern(string filename)
        {
            return (filename.Length > 2 && filename.StartsWith("..") && 
                 !(filename[2] == '/' || filename[2] == '\\' || char.IsWhiteSpace(filename[2])) &&
                 !filename.All(c => c == '.')) ||  // Allow pure sequences of dots
                (filename.Length > 2 && filename.EndsWith("..") && 
                 !(filename[filename.Length-3] == '/' || filename[filename.Length-3] == '\\' || char.IsWhiteSpace(filename[filename.Length-3])) &&
                 !filename.All(c => c == '.'));  // Allow pure sequences of dots
        }
    }
}