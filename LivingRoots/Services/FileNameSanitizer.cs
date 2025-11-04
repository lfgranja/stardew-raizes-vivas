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
                throw new ArgumentException("Filename cannot be null or whitespace.", nameof(filename));

            if (filename.Contains('\0'))
                throw new ArgumentException("Filename cannot contain null characters.", nameof(filename));

            string normalized = _unicodeNormalizer.Normalize(filename);
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
            string trimmed = processed.Trim('_', ' ', '.');
            
            // Preserve leading dots for hidden files
            if (filename.StartsWith(".") && !trimmed.StartsWith(".") && !string.IsNullOrEmpty(trimmed))
            {
                trimmed = "." + trimmed;
            }
            
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));

            if (trimmed.Length > MaxPathLength)
                trimmed = trimmed.Substring(0, MaxPathLength);

            return trimmed;
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