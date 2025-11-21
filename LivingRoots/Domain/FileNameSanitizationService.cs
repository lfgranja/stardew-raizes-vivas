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

            // Path traversal checks have been centralized in PathValidationService

            // Normalize Unicode characters first using the domain service
            string? normalized = _unicodeNormalizationService.Normalize(filename);
            
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
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));

            // Determine if this should be treated as a hidden file based on normalized input and actual content
            bool shouldBeHiddenFile = false;
            {
                // Use the normalized filename (without extension) to decide
                var normalizedNameNoExt = RemoveFileExtension(normalized);
                // A hidden file should start with a dot AND have some meaningful content after sanitization
                // Compute using the already sanitized content 'processed'
                if (normalizedNameNoExt.StartsWith(".", StringComparison.Ordinal))
                {
                    var contentAfterDot = processed.Length > 0 && processed[0] == '.'
                        ? processed.Substring(1)
                        : processed;
                    shouldBeHiddenFile = !string.IsNullOrWhiteSpace(contentAfterDot.Trim('_', ' ', '.'));
                }
                // Special case: if the entire filename is just a dot followed by an extension (like .exe),
                // it should be treated as a hidden file
                else if (normalized.StartsWith(".", StringComparison.Ordinal) && string.IsNullOrEmpty(nameWithoutExtension))
                {
                    shouldBeHiddenFile = true;
                }
            }

            // Special handling for cases where the name part sanitizes to empty but there's a blocked extension
            // For example ".exe" -> name part is empty, but we still need to handle the dangerous extension
            if (string.IsNullOrEmpty(processed) && !string.IsNullOrEmpty(extension) && IsBlockedExtension(extension))
            {
                // If the name part is empty but there's a blocked extension, create a safe filename
                // For hidden files (starting with dot), we preserve the dot
                if (normalized.StartsWith(".", StringComparison.Ordinal))
                {
                    // Create a minimal safe hidden filename with blocked extension
                    string safeResult = ".file.blocked";
                    // Validate the result after extension blocking
                    string baseAfterBlock = RemoveFileExtension(safeResult).Trim('_', ' ', '.');
                    if (string.IsNullOrWhiteSpace(baseAfterBlock) || baseAfterBlock == "." || baseAfterBlock == "..")
                    {
                        throw new ArgumentException($"Filename sanitizes to an invalid state after extension blocking: '{safeResult}'.", nameof(safeResult));
                    }
                    // Perform final cleanup after all processing
                    safeResult = PerformFinalCleanup(safeResult, true);
                    // Handle reserved Windows filenames
                    string? hiddenReservedResult = _reservedNameHandler.Handle(safeResult);
                    if (hiddenReservedResult == null)
                        throw new ArgumentException("Filename sanitizes to an empty string.", nameof(hiddenReservedResult));
                    return hiddenReservedResult;
                }
                else
                {
                    // For non-hidden files, create a minimal safe filename
                    string safeResult = "file.blocked";
                    // Validate the result after extension blocking
                    string baseAfterBlock = RemoveFileExtension(safeResult).Trim('_', ' ', '.');
                    if (string.IsNullOrWhiteSpace(baseAfterBlock) || baseAfterBlock == "." || baseAfterBlock == "..")
                    {
                        throw new ArgumentException($"Filename sanitizes to an invalid state after extension blocking: '{safeResult}'.", nameof(safeResult));
                    }
                    // Perform final cleanup after all processing
                    safeResult = PerformFinalCleanup(safeResult, false);
                    // Handle reserved Windows filenames
                    string? nonHiddenReservedResult = _reservedNameHandler.Handle(safeResult);
                    if (nonHiddenReservedResult == null)
                        throw new ArgumentException("Filename sanitizes to an empty string.", nameof(nonHiddenReservedResult));
                    return nonHiddenReservedResult;
                }
            }

            // Check if the processed name is "." or ".." before trimming - these are invalid path components
            if (processed == "." || processed == "..")
            {
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));
            }

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

                // Check if the content after the dot becomes empty after trimming
                // This prevents a hidden filename like ".   " from becoming "." after sanitization
                if (string.IsNullOrEmpty(trimmedContent))
                    throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));

                trimmed = "." + trimmedContent;
            }
            else
            {
                // For non-hidden files, trim from both ends
                trimmed = processed.Trim('_', ' ', '.');
            }

            // Check if the trimmed name part is empty or becomes "." or ".." after trimming
            // This handles cases like ".." which becomes empty after processing
            if (string.IsNullOrEmpty(trimmed) || trimmed == "." || trimmed == "..")
            {
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));
            }

            // Preserve leading dots for hidden files if not already present and content is not empty
            if (shouldBeHiddenFile && !trimmed.StartsWith(".", StringComparison.Ordinal) && !string.IsNullOrEmpty(trimmed))
            {
                var candidate = "." + trimmed;
                var coreAfterDot = candidate.Substring(1).Trim('_', ' ', '.');
                if (string.IsNullOrWhiteSpace(coreAfterDot) || coreAfterDot == "." || coreAfterDot == "..")
                    throw new ArgumentException("Filename sanitizes to an empty or invalid hidden name.", nameof(filename));
                trimmed = candidate;
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

            
            // Add extension back if it was present and safe
            if (!string.IsNullOrEmpty(extension))
            {
                // Check if the extension is in the blocked list
                if (IsBlockedExtension(extension))
                {
                    // Replace dangerous extension entirely with a safe indicator
                    // Ensure base has no trailing dots before appending any suffix/extension
                    result = result.TrimEnd('.');
                    // Replace dangerous extension entirely with a safe extension to prevent security issues
                    result = $"{result}.blocked";
                     
                     // After replacing the extension, ensure the resulting filename base is not empty or invalid
                     // This prevents security vulnerabilities where a filename could be sanitized to an invalid state
                     string baseAfterBlock = RemoveFileExtension(result).Trim('_', ' ', '.');
                     if (string.IsNullOrWhiteSpace(baseAfterBlock) || baseAfterBlock == "." || baseAfterBlock == "..")
                     {
                         throw new ArgumentException($"Filename sanitizes to an invalid state after extension blocking: '{result}'.", nameof(result));
                     }
                }
                else
                {
                    result = $"{result}{extension}";
                }

                // After adding the extension, ensure the total length does not exceed the maximum
                // This is necessary because the truncation happens before extension is added
                if (result.Length > MaxFileNameLength)
                {
                    // Extract the extension again to preserve it during truncation
                    string finalExtension = GetFileExtension(result);
                    string namePart = RemoveFileExtension(result);

                    // Truncate the name part to leave room for the extension
                    if (!string.IsNullOrEmpty(finalExtension))
                    {
                        // Calculate how much space is left for the name part
                        int availableLength = MaxFileNameLength - finalExtension.Length;
                        if (availableLength > 0)
                        {
                            // Truncate the name part to fit within available space
                            if (namePart.StartsWith(".", StringComparison.Ordinal))
                            {
                                // For hidden files, keep the dot and truncate the content part
                                string contentPart = namePart.Substring(1);
                                string truncatedContent = SafeSubstring(contentPart, 0, availableLength - 1);
                                if (truncatedContent.Length > 0)
                                {
                                    namePart = "." + truncatedContent;
                                }
                                else
                                {
                                    // If truncation results in empty content, ensure we have at least a minimal name
                                    namePart = "." + SafeSubstring(contentPart, 0, Math.Max(1, availableLength - 1));
                                }
                            }
                            else
                            {
                                namePart = SafeSubstring(namePart, 0, availableLength);
                            }

                            result = namePart + finalExtension;
                        }
                        else
                        {
                            // If there's no space for the name part, just use a minimal safe name instead
                            result = SafeSubstring("file", 0, Math.Max(1, MaxFileNameLength - finalExtension.Length)) + finalExtension;
                        }
                    }
                    else
                    {
                        // If somehow there's no extension, just truncate the result
                        result = TruncateToMaxLength(result);
                    }
                }
            }

            // Perform final cleanup after all processing including post-extension truncation
            // This ensures that any trailing characters are properly cleaned up after all operations
            result = PerformFinalCleanup(result, shouldBeHiddenFile);

            // Handle reserved Windows filenames
            string? reservedResult = _reservedNameHandler.Handle(result);

            // Check if the reserved name handler returned null
            if (reservedResult == null)
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(reservedResult));

            result = reservedResult;

            // Final check: After all processing, check if the result is empty or invalid
            // This is important for cases like "..exe" where the name part ".." becomes empty after processing
            string baseResult = RemoveFileExtension(result);
            string finalBaseTrimmed = baseResult.Trim('_', ' ', '.');
            if (string.IsNullOrWhiteSpace(finalBaseTrimmed) || finalBaseTrimmed == "." || finalBaseTrimmed == "..")
            {
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));
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

            // Final validity guard to prevent invalid path components
            var trimmedCore = postTruncationTrimmed.Trim('_', ' ', '.');
            if (string.IsNullOrWhiteSpace(trimmedCore) || trimmedCore == "." || trimmedCore == "..")
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(filename));

            // If the final result is still longer than max length, truncate again
            if (postTruncationTrimmed.Length > MaxFileNameLength)
            {
                return TruncateToMaxLength(postTruncationTrimmed);
            }

            return postTruncationTrimmed;
        }

        /// <summary>
        /// Helper method to find the start index of a valid file extension.
        /// </summary>
        /// <param name="filename">The filename to check.</param>
        /// <returns>The start index of the extension (including the dot) if valid, or -1 if no valid extension found.</returns>
        private static int FindExtensionStartIndex(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return -1;
            
            // Look for the last dot that represents a valid extension
            // We need to be careful not to treat directory separators as extension indicators
            int lastDotIndex = -1;
            
            // Find the rightmost dot that is not part of a directory path
            for (int i = filename.Length - 1; i >= 0; i--)
            {
                if (filename[i] == '.')
                {
                    // Check if this dot is followed by at least one alphanumeric character
                    // and is not immediately followed by a path separator
                    if (i < filename.Length - 1 && 
                        !filename[i + 1].Equals('/') && 
                        !filename[i + 1].Equals('\\'))
                    {
                        // Check if there are valid extension characters after this dot
                        string potentialExtension = filename.Substring(i);
                        if (potentialExtension.Length > 1) // Ensure the dot is not at the end
                        {
                            // Check if the part after the dot contains at least one alphanumeric character
                            string extensionPart = potentialExtension.Substring(1);
                            if (extensionPart.Any(c => char.IsLetterOrDigit(c)))
                            {
                                // Check if this extension portion contains directory separators
                                // This prevents cases like "file/path.ext" where the extension detection would be wrong
                                if (!potentialExtension.Contains('/', StringComparison.Ordinal) && 
                                    !potentialExtension.Contains('\\', StringComparison.Ordinal))
                                {
                                    // Additional check: if all characters before this dot are dots, 
                                    // it's not a valid extension unless it's a dangerous one at the beginning
                                    string beforeThisDot = filename.Substring(0, i);
                                    bool onlyDotsBefore = beforeThisDot.All(ch => ch == '.');
                                    
                                    if (onlyDotsBefore)
                                    {
                                        // If all characters before the dot are dots, this is not a valid extension
                                        // unless it's the very beginning of the filename and the extension is dangerous
                                        bool isAtBeginning = i == 0;
                                        bool isDangerousExtension = IsBlockedExtension(potentialExtension);
                                        
                                        if (isAtBeginning && isDangerousExtension)
                                        {
                                            // For security purposes, treat dangerous extensions at the beginning as having an extension
                                            return i;
                                        }
                                        else
                                        {
                                            // Not a valid extension if only dots precede it
                                            continue;
                                        }
                                    }
                                    
                                    // Additional validation: ensure the extension doesn't contain invalid filename characters
                                    if (potentialExtension.IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
                                    {
                                        lastDotIndex = i;
                                        break; // Found the last valid extension
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return lastDotIndex;
        }

        /// <summary>
        /// Gets the file extension from a filename.
        /// </summary>
        /// <param name="filename">The filename to extract extension from.</param>
        /// <returns>The file extension or empty string if no extension.</returns>
        private static string GetFileExtension(string filename)
        {
            int extensionStartIndex = FindExtensionStartIndex(filename);
            if (extensionStartIndex != -1)
            {
                return filename.Substring(extensionStartIndex);
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
            int extensionStartIndex = FindExtensionStartIndex(filename);
            if (extensionStartIndex != -1)
            {
                return filename.Substring(0, extensionStartIndex);
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
