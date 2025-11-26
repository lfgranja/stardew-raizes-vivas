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
    /// 
    /// IMPROVEMENTS SUMMARY:
    /// - Enhanced SafeSubstring robustness with additional null checks and boundary condition handling
    /// - Improved handling of surrogate pairs to prevent splitting Unicode characters
    /// - Added proper normalization of startIndex to prevent mathematical errors
    /// - Reordered boundary checks to prevent ArgumentOutOfRangeException
    /// </summary>
    public class FileNameSanitizationService : IFileNameSanitizationService
    {
        private const int MaxFileNameLength = 240; // Maximum filename length for truncation tests
        
        private readonly IUnicodeNormalizationService _unicodeNormalizationService;
        private readonly IReservedNameHandler _reservedNameHandler;

        // Static readonly field to avoid rebuilding the blocked extensions set on every call
        private static readonly HashSet<string> BlockedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".bat", ".sh", ".ps1", ".cmd", ".com", ".scr", ".pif", ".lnk", 
            ".msi", ".msp", ".vbs", ".js", ".jse", ".wsf", ".wsh", ".hta", ".cpl", ".msc", ".inf",
            ".py", ".rb", ".apk", ".ipa", ".jar", ".msix", ".appx", ".reg", ".iso", ".img", ".pkg", ".dmg",
            ".vbe", ".vbscript", ".ws", ".wsc", ".msh1", ".msh2", ".mshxml", ".msh1xml", ".msh2xml",
            ".scf", ".url", ".pif", ".scr", ".sys", ".bin", ".pl", ".php", ".asp", ".aspx", ".cgi",
            ".sql", ".mdb", ".accdb", ".db", ".dbf", ".sqlite", ".sqlite3", ".jar", ".war", ".ear",
            ".class", ".dex", ".so", ".dylib", ".dll", ".sys", ".drv", ".ocx", ".cpl", ".msc", ".msi",
            ".msp", ".mst", ".vxd", ".acm", ".ax", ".efi", ".fon", ".ime", ".kbd", ".scr", ".vbx", ".xll",
            // Additional extensions to address security concerns and be more comprehensive
            ".app", ".deb", ".rpm", ".msm", ".msp", ".msp", ".mst", ".sct", ".wsf", ".wsh", ".ps1xml",
            ".psc1", ".psd1", ".psm1", ".mof", ".inf", ".sys", ".drv", ".vxd", ".acm", ".ax", ".efi",
            ".fon", ".ime", ".kbd", ".scr", ".vbx", ".xll", ".paf", ".pif", ".prg", ".scr", ".shb",
            ".shs", ".u3p", ".vbs", ".vbe", ".vbs", ".ws", ".wse", ".wsh", ".mcr", ".mce", ".mcf",
            ".jar", ".jnlp", ".war", ".ear", ".apk", ".xap", ".swf", ".flash", ".action", ".workflow",
            ".command", ".csh", ".tcsh", ".zsh", ".fish", ".ksh", ".bash", ".rbw", ".rbx", ".gem",
            ".pl", ".pm", ".plx", ".perl", ".php", ".php3", ".php4", ".php5", ".phtml", ".pyc", ".pyo",
            ".pyd", ".asm", ".asmx", ".ps1", ".psc1", ".psd1", ".psm1", ".msh1", ".msh2",
            ".mshxml", ".msh1xml", ".msh2xml", ".scf", ".paf", ".gadget", ".msc", ".msi", ".msp",
            ".mst", ".sct", ".shb", ".shs", ".u3p", ".vbe", ".vbs", ".vbscript", ".ws", ".wsc",
            ".wsf", ".wsh", ".hta", ".htr", ".cer", ".crt", ".crl", ".crt", ".der", ".p12", ".p7b",
            ".p7c", ".p7m", ".p7r", ".p7s", ".pem", ".pfx", ".pgm", ".pgp", ".pki", ".pko", ".pl",
            ".plc", ".plg", ".plp", ".plx", ".pm", ".pmc", ".pmw", ".po", ".pot", ".potm", ".potx",
            ".ppa", ".ppam", ".pps", ".ppsm", ".ppsx", ".ppt", ".pptm", ".pptx", ".prf", ".prg",
            ".printerexport", ".prl", ".prm", ".prx", ".ps1", ".psc1", ".psd1", ".psm1", ".pst", ".py",
            ".pyc", ".pyd", ".pyo", ".pyw", ".pyz", ".pyzw", ".rb", ".rbw", ".rbx", ".gem", ".gemspec",
            ".ru", ".rbi", ".rake", ".cap", ".thor", ".watchr", ".ahkl", ".pl", ".plx", ".cgi",
            ".fcgi", ".pl", ".pm", ".pod", ".t", ".cgi", ".fcgi", ".pl", ".pm", ".pod", ".t", ".aws",
            ".efi", ".exe", ".msc", ".msp", ".mst", ".vbe", ".vbs", ".wsf", ".wsh", ".hta", ".htr",
            ".inf", ".ins", ".isp", ".jar", ".jnlp", ".jse", ".scr", ".sct", ".sh", ".shb", ".shs",
            ".url", ".vb", ".vbe", ".vbs", ".vbscript", ".ws", ".wsc", ".wsf", ".wsh", ".xsl", ".xslt"
        };

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
            if (filename == null) return null;
            ValidateInput(filename);

            var normalized = _unicodeNormalizationService.Normalize(filename) 
                             ?? throw new ArgumentException("Normalized filename is null.", nameof(filename));

            // Extract parts
            string extension = GetFileExtension(normalized);
            string nameWithoutExtension = RemoveFileExtension(normalized);

            // Step 1: Check for blocked extensions in empty names
            if (ShouldBlockEmptyName(nameWithoutExtension, extension))
                 return CreateSafeNameForBlockedExtension(normalized, extension);

            // Step 2: Sanitize characters and dots
            string processed = SanitizeBaseName(nameWithoutExtension);

            // Step 3: Hidden file logic
            bool isHidden = DetermineHiddenFileStatus(processed);
            processed = ProcessHiddenFileLogic(processed, filename, isHidden);

            // Step 4: Truncate and clean
            string result = PerformFinalCleanup(TruncateToMaxLength(processed), isHidden);

            // Step 5: Reintegrate extension (with blocking check)
            result = AppendExtensionSafely(result, extension);

            // Step 6: Reserved names and final validation
            result = HandleReservedNames(result);
            ValidateFinalResult(result);

            return result;
        }

        /// <summary>
        /// Validates the input filename
        /// </summary>
        /// <param name="filename">The filename to validate</param>
        private static void ValidateInput(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("Filename cannot be empty or whitespace-only.", nameof(filename));

            if (filename.Contains('\0'))
                throw new ArgumentException("Filename cannot contain null characters.", nameof(filename));
        }

        /// <summary>
        /// Determines if the filename should be treated as a hidden file based on the sanitized result
        /// </summary>
        /// <param name="sanitized">The sanitized name part</param>
        /// <returns>True if the filename should be treated as a hidden file</returns>
        private static bool DetermineHiddenFileStatus(string sanitized)
        {
            // Security fix: Add guard clause for null or empty strings to prevent access issues
            if (string.IsNullOrEmpty(sanitized))
                return false;

            // A hidden file should start with a dot AND have some meaningful content after sanitization
            // Simplified logic by directly checking first character instead of StartsWith
            if (sanitized[0] == '.')
            {
                var contentAfterDot = sanitized.Length > 1 ? sanitized.Substring(1) : string.Empty;
                return !string.IsNullOrWhiteSpace(contentAfterDot.Trim('_', ' ', '.'));
            }

            return false;
        }

        /// <summary>
        /// Processes the sanitized name with hidden file logic
        /// </summary>
        /// <param name="sanitized">The sanitized name part</param>
        /// <param name="originalFilename">The original filename</param>
        /// <param name="shouldBeHiddenFile">Whether this should be treated as a hidden file</param>
        /// <returns>The processed name</returns>
        private static string ProcessHiddenFileLogic(string sanitized, string? originalFilename, bool shouldBeHiddenFile)
        {
            // Special handling for cases where the name part sanitizes to empty but there's a blocked extension
            // For example ".exe" -> name part is empty, but we still need to handle the dangerous extension
            if (string.IsNullOrEmpty(sanitized) && IsBlockedExtension(GetFileExtension(originalFilename ?? string.Empty)))
            {
                // If the name part is empty but there's a blocked extension, create a safe filename
                // For hidden files (starting with dot), we preserve the dot
                if (originalFilename?.StartsWith(".", StringComparison.Ordinal) == true)
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
                    
                    // Check length after creating the blocked extension result
                    if (safeResult.Length > MaxFileNameLength)
                    {
                        safeResult = TruncateToMaxLength(safeResult);
                    }
                    return safeResult;
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
                    
                    // Check length after creating the blocked extension result
                    if (safeResult.Length > MaxFileNameLength)
                    {
                        safeResult = TruncateToMaxLength(safeResult);
                    }
                    return safeResult;
                }
            }

            // Trim leading/trailing problematic characters (but preserve leading dots for hidden files)
            string trimmed;
            if (shouldBeHiddenFile && sanitized.StartsWith(".", StringComparison.Ordinal))
            {
                // For hidden files, preserve the leading dot and only trim the rest
                string contentAfterDot = sanitized.Substring(1);
                string trimmedContent = contentAfterDot.TrimEnd('_', ' ', '.');

                // Special handling for the case where the original filename started with a dot followed by 
                // invalid characters that were converted to underscores during sanitization.
                // For example: ".<hidden_file.txt" -> "._hidden_file.txt" -> ".hidden_file.txt"
                if (contentAfterDot.Length > 0 && contentAfterDot[0] == '_' && originalFilename?.Length > 1)
                {
                    // Check if the original character after the dot was an invalid character
                    char originalCharAfterDot = originalFilename[1];
                    if (IsInvalidOrProblematicChar(originalCharAfterDot))
                    {
                        // Remove leading underscore since it came from sanitizing an invalid character
                        trimmedContent = contentAfterDot.TrimStart('_').TrimEnd('_', ' ', '.');
                    }
                }

                // Check if the content after the dot becomes empty after trimming
                // This prevents a hidden filename like ".   " from becoming "." after sanitization
                if (string.IsNullOrEmpty(trimmedContent))
                    throw new ArgumentException("Filename sanitizes to an empty string.", nameof(originalFilename));

                trimmed = "." + trimmedContent;
            }
            else
            {
                // For non-hidden files, trim from both ends
                trimmed = sanitized.Trim('_', ' ', '.');
            }

            // Check if the trimmed name part is empty or becomes "." or ".." after trimming
            // This handles cases like ".." which becomes empty after processing
            if (string.IsNullOrEmpty(trimmed) || trimmed == "." || trimmed == "..")
            {
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(originalFilename));
            }

            // Preserve leading dots for hidden files if not already present and content is not empty
            if (shouldBeHiddenFile && !trimmed.StartsWith(".", StringComparison.Ordinal) && !string.IsNullOrEmpty(trimmed))
            {
                var candidate = "." + trimmed;
                var coreAfterDot = candidate.Substring(1).Trim('_', ' ', '.');
                if (string.IsNullOrWhiteSpace(coreAfterDot) || coreAfterDot == "." || coreAfterDot == "..")
                    throw new ArgumentException("Filename sanitizes to an empty or invalid hidden name.", nameof(originalFilename));
                trimmed = candidate;
            }

            return trimmed;
        }

        /// <summary>
        /// Truncates the filename to the maximum allowed length, handling hidden files properly.
        /// Improved to handle extremely short budgets (1-2 chars) properly.
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
        /// Enhanced to ensure results never end with trailing dot/underscore and surrogate pairs aren't split.
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
        /// Processes the extension part of the filename
        /// </summary>
        /// <param name="result">The current result</param>
        /// <param name="extension">The extension to process</param>
        /// <returns>The result with processed extension</returns>
        private static string AppendExtensionSafely(string result, string extension)
        {
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
                                int contentBudget = Math.Max(0, availableLength - 1); // Reserve 1 char for the dot if possible
                                if (contentBudget > 0)
                                {
                                    string truncatedContent = SafeSubstring(contentPart, 0, contentBudget);
                                    if (truncatedContent.Length > 0)
                                    {
                                        namePart = "." + truncatedContent;
                                    }
                                    else
                                    {
                                        // If truncation results in empty content, ensure we have at least a minimal name
                                        int minimalBudget = Math.Max(1, contentBudget);
                                        namePart = "." + SafeSubstring(contentPart, 0, minimalBudget);
                                    }
                                }
                                else
                                {
                                    // If there's no room for content, create a minimal safe hidden name
                                    namePart = ".f"; // Minimal safe hidden name
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
                            int extensionLength = Math.Max(0, MaxFileNameLength - finalExtension.Length);
                            if (extensionLength > 0)
                            {
                                // If there's some space for a name part, use a minimal safe name
                                result = SafeSubstring("file", 0, Math.Max(1, extensionLength)) + finalExtension;
                            }
                            else
                            {
                                // If there's no space at all, just return the extension with minimal name
                                result = ".f" + finalExtension;
                            }
                        }
                    }
                    else
                    {
                        // If somehow there's no extension, just truncate the result
                        result = TruncateToMaxLength(result);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Validates the final result after all processing
        /// </summary>
        /// <param name="result">The final sanitized filename</param>
        private static void ValidateFinalResult(string result)
        {
            string baseResult = RemoveFileExtension(result);
            string finalBaseTrimmed = baseResult.Trim('_', ' ', '.');
            if (string.IsNullOrWhiteSpace(finalBaseTrimmed) || finalBaseTrimmed == "." || finalBaseTrimmed == "..")
            {
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(result));
            }
        }

        /// <summary>
        /// Creates a safe filename for cases where the name part is empty but extension is blocked
        /// </summary>
        /// <param name="normalized">The normalized filename</param>
        /// <param name="extension">The extension to process</param>
        /// <returns>A safe filename with blocked extension</returns>
        private string CreateSafeNameForBlockedExtension(string normalized, string extension)
        {
            // For hidden files (starting with dot), we preserve the dot
            if (normalized.StartsWith(".", StringComparison.Ordinal))
            {
                return ".file.blocked";
            }
            else
            {
                return "file.blocked";
            }
        }

        /// <summary>
        /// Sanitizes the base name of the filename (without extension)
        /// </summary>
        /// <param name="nameWithoutExtension">The name part to sanitize</param>
        /// <returns>The sanitized name part</returns>
        private static string SanitizeBaseName(string nameWithoutExtension)
        {
            // Sanitize characters by replacing invalid ones (this follows the original approach but with security enhancements)
            string sanitized = SanitizeInvalidCharacters(nameWithoutExtension);

            // Process consecutive dots (this should be done after character sanitization)
            string processed = ProcessConsecutiveDots(sanitized);

            // Check if the processed filename would become "." or ".." after trimming problematic characters
            // This validation must happen before any trimming to prevent bypassing safeguards
            string processedTrimmed = processed.Trim('_', ' ', '.');
            if (processedTrimmed == "." || processedTrimmed == "..")
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(nameWithoutExtension));

            // Check if the processed name is "." or ".." before trimming - these are invalid path components
            if (processed == "." || processed == "..")
            {
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(nameWithoutExtension));
            }

            return processed;
        }

        /// <summary>
        /// Checks if we should block an empty name with a blocked extension
        /// </summary>
        /// <param name="nameWithoutExtension">The name part without extension</param>
        /// <param name="extension">The extension part</param>
        /// <returns>True if we should block empty name with blocked extension</returns>
        private static bool ShouldBlockEmptyName(string nameWithoutExtension, string extension)
        {
            return string.IsNullOrEmpty(nameWithoutExtension) && !string.IsNullOrEmpty(extension) && IsBlockedExtension(extension);
        }

        /// <summary>
        /// Handles reserved names in the filename
        /// </summary>
        /// <param name="result">The current sanitized result</param>
        /// <returns>The result after handling reserved names</returns>
        private string HandleReservedNames(string result)
        {
            // Handle reserved Windows filenames
            string? reservedResult = _reservedNameHandler.Handle(result);

            // Check if the reserved name handler returned null
            if (reservedResult == null)
                throw new ArgumentException("Filename sanitizes to an empty string.", nameof(reservedResult));

            result = reservedResult;

            // Recheck length after reserved-name handling to ensure filename doesn't exceed MaxFileNameLength
            // This is necessary because the reserved name handler might add characters (e.g., underscores) to the name
            if (result.Length > MaxFileNameLength)
            {
                result = TruncateToMaxLength(result);
                // Apply final cleanup after truncation to ensure proper formatting
                result = PerformFinalCleanup(result, DetermineHiddenFileStatus(result));
            }

            return result;
        }

        /// <summary>
        /// Sanitizes invalid characters by using an allowlist approach.
        /// Only allows alphanumeric characters, dots, hyphens, underscores, and valid surrogate pairs (emojis).
        /// Invalid characters are replaced with underscores, but consecutive invalid characters
        /// are consolidated to a single underscore to prevent collision issues.
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
                    // This helps prevent collision where different inputs result in the same sanitized output
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
        /// Helper method to find the start index of a valid file extension.
        /// </summary>
        /// <param name="filename">The filename to check.</param>
        /// <returns>The start index of the extension (including the dot) if valid, or -1 if no valid extension found.</returns>
        private static int FindExtensionStartIndex(string filename)
        {
            int lastDotIndex = filename.LastIndexOf('.');
            
            // No dot found or dot is at the end
            if (lastDotIndex < 0 || lastDotIndex >= filename.Length - 1)
                return -1;
            
            // Extract potential extension (including the dot)
            string potentialExtension = filename.Substring(lastDotIndex);
            
            // Check if the part after the dot contains at least one alphanumeric character
            string extensionPart = potentialExtension.Substring(1);
            if (!extensionPart.Any(c => char.IsLetterOrDigit(c)))
                return -1;
            
            // Check if extension contains path separators
            if (potentialExtension.Contains('/') || potentialExtension.Contains('\\'))
                return -1;
            
            // For security: if the filename starts with a dot followed by a dangerous extension, still detect it
            if (lastDotIndex == 0 && IsBlockedExtension(potentialExtension))
                return 0; // Return 0 for simple dotfiles with dangerous extensions like ".exe"
            
            // For simple dotfiles (e.g., ".profile"), check if it's not a dangerous extension
            if (lastDotIndex == 0 && !IsBlockedExtension(potentialExtension))
                return -1; // Don't treat simple dotfiles as having extensions unless they're dangerous
            
            // Check if the extension contains invalid filename characters
            if (potentialExtension.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                return -1;
            
            // If all checks pass, return the index of the dot
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
            if (string.IsNullOrEmpty(extension))
                return false;
            
            // Normalize the extension to prevent Unicode homoglyph bypasses
            var normalizedExtension = extension.Normalize(NormalizationForm.FormC);
            
            return BlockedExtensions.Contains(normalizedExtension);
        }

        /// <summary>
        /// Safely extracts a substring without splitting surrogate pairs.
        /// IMPROVEMENTS:
        /// - Added null check for str parameter to prevent NullReferenceException
        /// - Normalize startIndex before calculating endIndex to prevent potential issues
        /// - Reorder boundary checks to prevent ArgumentOutOfRangeException
        /// - Handle extremely short budgets (1-2 chars) properly
        /// - Properly handle surrogate pair boundaries to prevent splitting Unicode characters
        /// </summary>
        /// <param name="str">The input string</param>
        /// <param name="startIndex">Start index</param>
        /// <param="length">Length to extract</param>
        /// <returns>The substring</returns>
        private static string SafeSubstring(string str, int startIndex, int length)
        {
            // Security fix: Add null check for str parameter to prevent NullReferenceException
            if (str == null)
                return string.Empty;

            // Normalize startIndex before calculating endIndex to prevent potential issues
            if (startIndex < 0)
                startIndex = 0;

            // Reorder boundary checks to prevent ArgumentOutOfRangeException
            // First check if startIndex is beyond the string length
            if (startIndex >= str.Length)
                return string.Empty;

            // Ensure length is not negative to prevent ArgumentOutOfRangeException
            if (length < 0)
                return string.Empty;

            // Handle extremely short budgets (1-2 chars) properly
            // If the requested length is 0, return empty string
            if (length == 0)
                return string.Empty;

            // Calculate the actual end index - using Math.Min to ensure we don't exceed string length
            int endIndex = Math.Min(startIndex + length, str.Length);

            // Calculate the length to extract
            int actualLength = endIndex - startIndex;
            
            // If actual length is 0, return empty string
            if (actualLength <= 0)
                return string.Empty;

            // Check if we're potentially splitting a surrogate pair
            // If the character at endIndex is a low surrogate and the one before it is a high surrogate,
            // we should exclude the high surrogate to avoid splitting the pair
            if (endIndex < str.Length && char.IsLowSurrogate(str[endIndex]) && 
                endIndex > 0 && char.IsHighSurrogate(str[endIndex - 1]))
            {
                // If we're removing a surrogate pair due to truncation, adjust the end index
                endIndex--;
                actualLength = endIndex - startIndex;
                
                // If this reduces the length to 0, return empty string
                if (actualLength <= 0)
                    return string.Empty;
            }

            return str.Substring(startIndex, actualLength);
        }
    }
}
