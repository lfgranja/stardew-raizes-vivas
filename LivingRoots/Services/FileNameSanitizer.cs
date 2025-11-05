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
        private const int MaxPathLength = 260; // Standard Windows path length limit
        private const int MaxFileNameLength = 240; // Maximum filename length for truncation tests
        private const int JsonExtensionLength = 5; // Length of ".json" extension
        
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
        public string Sanitize(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return filename;

            if (filename.Contains('\0'))
                throw new ArgumentException("Filename cannot contain null characters.", nameof(filename));

            // First, normalize the Unicode characters to handle diacritics, homoglyphs, etc.
            string normalized = _unicodeNormalizer.Normalize(filename);

            // Apply sanitization processing (replacing invalid characters with underscores, etc.)
            var sanitizedBuilder = new StringBuilder();
            
            for (int i = 0; i < normalized.Length; i++)
            {
                char c = normalized[i];
                
                // Handle surrogate pairs (emojis and other multi-byte Unicode characters)
                if (char.IsHighSurrogate(c) && i + 1 < normalized.Length && char.IsLowSurrogate(normalized[i + 1]))
                {
                    // Preserve valid surrogate pairs (emojis, etc.)
                    char lowSurrogate = normalized[i + 1];
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

            string sanitized = sanitizedBuilder.ToString();
            
            // Process consecutive dots: replace multiple consecutive dots with a single dot
            StringBuilder processedBuilder = new StringBuilder();
            int consecutiveDotCount = 0;
            
            for (int i = 0; i < sanitized.Length; i++)
            {
                char c = sanitized[i];
                
                if (c == '.')
                {
                    consecutiveDotCount++;
                }
                else
                {
                    // If we had consecutive dots, add a single dot for each group
                    if (consecutiveDotCount > 0)
                    {
                        // Add one dot for each group of consecutive dots
                        processedBuilder.Append('.');
                        consecutiveDotCount = 0;
                    }
                    processedBuilder.Append(c);
                }
            }
            
            // Handle any trailing consecutive dots
            if (consecutiveDotCount > 0)
            {
                processedBuilder.Append('.');
            }
            
            string processed = processedBuilder.ToString();
            
            // Trim leading and trailing underscores, spaces, and dots
            string trimmed = processed.Trim('_', ' ', '.');
            
            // Preserve leading dots for hidden files
            if (filename.StartsWith(".") && !trimmed.StartsWith(".") && !string.IsNullOrEmpty(trimmed))
            {
                trimmed = "." + trimmed;
            }
            
            // Now handle truncation after all other processing
            if (trimmed.Length > MaxFileNameLength)
            {
                // If the trimmed string is too long, we need to truncate
                // For hidden files, we need to account for the dot
                if (trimmed.StartsWith("."))
                {
                    // Keep the dot and truncate the content part
                    string contentPart = trimmed.Substring(1);
                    string truncatedContent = SafeSubstring(contentPart, 0, MaxFileNameLength - 1);
                    trimmed = "." + truncatedContent;
                }
                else
                {
                    // Truncate to max length
                    trimmed = SafeSubstring(trimmed, 0, MaxFileNameLength);
                }
            }
            
            // FINAL check: if the result is exactly at MaxFileNameLength, we should not trim anything else
            // But if it's shorter than MaxFileNameLength and we have leading/trailing chars to trim,
            // we can trim them. However, if it's at the max length, we must preserve it.
            if (trimmed.Length == MaxFileNameLength)
            {
                // If already at max length, we can't trim more without going below the limit
                // But we should ensure it doesn't end with problematic characters if possible
                // Actually, if it's at max length, we have to accept it as is to maintain the length
            }
            else
            {
                // If not at max length, we can trim again if needed
                string doubleTrimmed = trimmed.Trim('_', ' ', '.');
                if (!string.IsNullOrEmpty(doubleTrimmed) && !doubleTrimmed.Equals(trimmed))
                {
                    // If trimming changed the string and it's not empty, use the trimmed version
                    trimmed = doubleTrimmed;
                    
                    // If it was a hidden file and we lost the dot, add it back
                    if (filename.StartsWith(".") && !trimmed.StartsWith(".") && trimmed.Length < MaxFileNameLength)
                    {
                        trimmed = "." + trimmed;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));

            // Final check to ensure we don't exceed the max length
            if (trimmed.Length > MaxFileNameLength)
            {
                if (trimmed.StartsWith("."))
                {
                    // For hidden files, keep the dot and truncate the rest
                    trimmed = "." + SafeSubstring(trimmed.Substring(1), 0, MaxFileNameLength - 1);
                }
                else
                {
                    trimmed = SafeSubstring(trimmed, 0, MaxFileNameLength);
                }
            }

            return trimmed;
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