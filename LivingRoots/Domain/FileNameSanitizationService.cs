using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
    /// - Enhanced validation of CreateAndValidateSafeBlockedFilename return value
    /// - Guaranteed replacement of dangerous extensions with ".blocked"
    /// - Improved surrogate handling as suggested by qodo-merge-pro
    /// - Comprehensive validation of all return values
    /// - Enhanced hidden-name core revalidation for additional security
    /// </summary>
    public class FileNameSanitizationService(IUnicodeNormalizationService unicodeNormalizationService, IReservedNameHandler reservedNameHandler) : IFileNameSanitizationService
    {
        private const int MaxFileNameLength = 240; // Maximum filename length for truncation tests
        private const string EmptyFilenameErrorMessage = "Filename sanitizes to an empty string.";

        private readonly IUnicodeNormalizationService _unicodeNormalizationService = unicodeNormalizationService ?? throw new ArgumentNullException(nameof(unicodeNormalizationService));
        private readonly IReservedNameHandler _reservedNameHandler = reservedNameHandler ?? throw new ArgumentNullException(nameof(reservedNameHandler));

        // Static readonly field to avoid rebuilding the blocked extensions set on every call
        // IMPROVEMENT: Refocused the blocked extensions list to include only truly dangerous executable and script file types
        private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            // Executable files
            ".exe", ".dll", ".bat", ".com", ".scr", ".pif", ".lnk", ".msi", ".msp",
            ".cpl", ".msc", ".sys", ".bin", ".drv", ".ocx", ".efi", ".app", ".apk", ".ipa",

            // Script files
            ".sh", ".ps1", ".cmd", ".vbs", ".js", ".jse", ".wsf", ".wsh", ".hta",
            ".py", ".rb", ".pl", ".php", ".asp", ".aspx", ".cgi", ".vbe", ".vbscript",
            ".ws", ".wsc", ".scf", ".url", ".mof", ".sct", ".reg", ".inf",

            // Installers and packages
            ".jar", ".msix", ".appx", ".deb", ".rpm", ".pkg", ".dmg", ".iso", ".img",

            // Other potentially dangerous files
            ".swf", ".flash", ".class", ".dex", ".jnlp", ".xap", ".action", ".workflow",
            ".command", ".csh", ".tcsh", ".zsh", ".fish", ".ksh", ".bash"
        };

        // Static readonly field to cache invalid file name characters and avoid repeated array allocations
        private static readonly HashSet<char> InvalidFileNameChars = new(Path.GetInvalidFileNameChars());

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

            // Special handling for "." and ".." as these are special path components and should not be treated as filenames
            if (filename == "." || filename == "..")
            {
                throw new ArgumentException(EmptyFilenameErrorMessage, nameof(filename));
            }

            var normalized = _unicodeNormalizationService.Normalize(filename)
                             ?? throw new ArgumentException("Normalized filename is null.", nameof(filename));

            // Extract parts
            var extension = GetFileExtension(normalized);
            var nameWithoutExtension = RemoveFileExtension(normalized);

            // Step 1: Check for blocked extensions in empty names
            if (ShouldBlockEmptyName(nameWithoutExtension, extension))
                return CreateSafeNameForBlockedExtension(normalized);

            // Step 2: Sanitize characters and dots
            var processed = SanitizeBaseName(nameWithoutExtension);


            // Step 3: Hidden file logic
            var isHidden = DetermineHiddenFileStatus(processed);
            processed = ProcessHiddenFileLogic(processed, filename, isHidden);

            // Step 4: Truncate and clean
            var result = PerformFinalCleanup(TruncateToMaxLength(processed), isHidden);


            // IMPROVEMENT: Trim trailing fillers before extension appending
            result = result.TrimEnd('_', ' ', '.');

            // Step 5: Reintegrate extension (with blocking check)
            result = AppendExtensionSafely(result, extension);

            // Step 6: Reserved names and final validation
            result = HandleReservedNames(result);

            // Revalidate hidden-name core after all processing to ensure hidden file status is preserved
            result = RevalidateHiddenNameCore(result);

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
            if (ShouldHandleEmptyNameWithBlockedExtension(sanitized, originalFilename))
            {
                var isHiddenFile = originalFilename?.StartsWith(".", StringComparison.Ordinal) == true;
                return CreateAndValidateSafeBlockedFilename(isHiddenFile);
            }

            var trimmed = TrimSanitizedName(sanitized, shouldBeHiddenFile, originalFilename);
            ValidateTrimmedName(trimmed);
            trimmed = EnsureHiddenFilePrefix(trimmed, shouldBeHiddenFile);

            return trimmed;
        }

        /// <summary>
        /// Determines if we should handle an empty name with a blocked extension
        /// </summary>
        private static bool ShouldHandleEmptyNameWithBlockedExtension(string sanitized, string? originalFilename)
        {
            return string.IsNullOrEmpty(sanitized) && IsBlockedExtension(GetFileExtension(originalFilename ?? string.Empty));
        }

        /// <summary>
        /// Trims the sanitized name, preserving hidden file dots as needed
        /// </summary>
        private static string TrimSanitizedName(string sanitized, bool shouldBeHiddenFile, string? originalFilename)
        {
            if (shouldBeHiddenFile && sanitized.StartsWith(".", StringComparison.Ordinal))
            {
                return TrimHiddenFileName(sanitized, originalFilename);
            }

            return sanitized.Trim('_', ' ', '.');
        }

        /// <summary>
        /// Trims a hidden filename while preserving the leading dot
        /// </summary>
        private static string TrimHiddenFileName(string sanitized, string? originalFilename)
        {
            var contentAfterDot = sanitized.Substring(1);
            var trimmedContent = contentAfterDot.TrimEnd('_', ' ', '.');

            // Remove leading underscore if it came from sanitizing an invalid character
            trimmedContent = RemoveLeadingUnderscoreFromInvalidChar(trimmedContent, contentAfterDot, originalFilename);

            if (string.IsNullOrEmpty(trimmedContent))
                throw new ArgumentException(EmptyFilenameErrorMessage, nameof(sanitized));

            return "." + trimmedContent;
        }

        /// <summary>
        /// Removes leading underscore if it resulted from sanitizing an invalid character
        /// </summary>
        private static string RemoveLeadingUnderscoreFromInvalidChar(string trimmedContent, string contentAfterDot, string? originalFilename)
        {
            // Only consider removing a leading underscore if it likely came from sanitizing
            // the original character right after the dot.
            if (contentAfterDot.Length > 0 && contentAfterDot[0] == '_' && originalFilename != null && originalFilename.Length > 1)
            {
                var originalCharAfterDot = originalFilename[1];

                // If underscore was introduced by sanitizing an invalid/problematic character,
                // remove it (while keeping the already-trimmed result).
                if (IsInvalidOrProblematicChar(originalCharAfterDot))
                {
                    return trimmedContent.TrimStart('_');
                }
            }

            return trimmedContent;
        }

        /// <summary>
        /// Checks if a character is invalid or problematic
        /// </summary>
        private static bool IsInvalidOrProblematicChar(char c)
        {
            // Additional problematic characters that should be replaced
            // These are checked first because they're the most common in path traversal attacks
            if (c == '/' || c == '\\' || c == ':' || c == '*' || c == '?' || c == '"' || c == '<' || c == '>' || c == '|')
                return true;

            // Control characters (except tab, carriage return, line feed which are whitespace) are invalid
            if (char.IsControl(c) && c != '\t' && c != '\r' && c != '\n')
                return true;

            // Check against system invalid file name characters
            if (InvalidFileNameChars.Contains(c))
                return true;

            return false;
        }

        /// <summary>
        /// Validates that the trimmed name is not empty or invalid
        /// </summary>
        private static void ValidateTrimmedName(string trimmed)
        {
            if (string.IsNullOrEmpty(trimmed) || trimmed == "." || trimmed == "..")
            {
                throw new ArgumentException(EmptyFilenameErrorMessage, nameof(trimmed));
            }
        }

        /// <summary>
        /// Ensures hidden files have the leading dot prefix
        /// </summary>
        private static string EnsureHiddenFilePrefix(string trimmed, bool shouldBeHiddenFile)
        {
            if (shouldBeHiddenFile && !trimmed.StartsWith(".", StringComparison.Ordinal) && !string.IsNullOrEmpty(trimmed))
            {
                var candidate = "." + trimmed;
                var coreAfterDot = candidate.Substring(1).Trim('_', ' ', '.');
                if (string.IsNullOrWhiteSpace(coreAfterDot) || coreAfterDot == "." || coreAfterDot == "..")
                    throw new ArgumentException("Filename sanitizes to an empty or invalid hidden name.", nameof(trimmed));
                return candidate;
            }
            return trimmed;
        }

        /// <summary>
        /// Creates and validates a safe filename for blocked extensions with comprehensive validation
        /// </summary>
        /// <param name="isHidden">Whether the file should be treated as hidden</param>
        /// <param name="originalFilename">The original filename</param>
        /// <returns>A safe filename with blocked extension</returns>
        /// <exception cref="ArgumentException">Thrown when the generated filename is invalid</exception>
        private static string CreateAndValidateSafeBlockedFilename(bool isHidden)
        {
            // Generate safe result based on hidden status
            var safeResult = isHidden ? ".file.blocked" : "file.blocked";

            // Validate the result after extension blocking
            var baseAfterBlock = RemoveFileExtension(safeResult).Trim('_', ' ', '.');
            if (string.IsNullOrWhiteSpace(baseAfterBlock) || baseAfterBlock == "." || baseAfterBlock == "..")
            {
                throw new ArgumentException($"Filename sanitizes to an invalid state after extension blocking: '{safeResult}'.");
            }

            // Perform final cleanup after all processing
            safeResult = PerformFinalCleanup(safeResult, isHidden);

            // Validate the result after final cleanup
            ValidateFinalResult(safeResult);

            // Additional validation: ensure that the result is not empty and meets security requirements
            if (string.IsNullOrEmpty(safeResult))
            {
                throw new ArgumentException("Generated safe filename is empty after processing.");
            }

            // Check length after creating the blocked extension result
            if (safeResult.Length > MaxFileNameLength)
            {
                safeResult = TruncateToMaxLength(safeResult);
                // Re-validate after truncation
                ValidateFinalResult(safeResult);
            }

            // Final validation to ensure all security requirements are met
            EnsureSecurityRequirements(safeResult);

            // Enhanced validation: Ensure that the result has the correct format based on hidden status
            if (isHidden && !safeResult.StartsWith(".", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Hidden file status not preserved after processing: '{safeResult}'.");
            }

            // Enhanced validation: Ensure that the blocked extension is still present after all processing
            var finalExtension = GetFileExtension(safeResult);
            if (!finalExtension.Equals(".blocked", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Blocked extension not preserved after processing: '{safeResult}'.");
            }

            // Enhanced validation: Ensure that the base part of the filename is valid after all processing
            var finalBase = RemoveFileExtension(safeResult).Trim('_', ' ', '.');
            if (string.IsNullOrWhiteSpace(finalBase) || finalBase == "." || finalBase == "..")
            {
                throw new ArgumentException($"Final result has invalid base after processing: '{safeResult}'.");
            }

            return safeResult;
        }

        /// <summary>
        /// Ensures security requirements are met for filename
        /// </summary>
        /// <param name="filename">The filename to validate</param>
        private static void EnsureSecurityRequirements(string filename)
        {
            // Ensure that filename doesn't end with invalid characters except for extensions
            if (filename.EndsWith("..") && !filename.EndsWith("...")) // Allow triple dots to become single dots
            {
                throw new ArgumentException($"Filename contains invalid pattern: '{filename}'", nameof(filename));
            }

            // Validate that it doesn't contain path traversal sequences
            if (filename.Contains("../") || filename.Contains("..\\"))
            {
                throw new ArgumentException($"Filename contains path traversal sequences: '{filename}'", nameof(filename));
            }
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
                var contentPart = filename.Substring(1);
                var truncatedContent = SafeSubstring(contentPart, 0, MaxFileNameLength - 1);
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
            var postTruncationTrimmed = filename.TrimEnd('_', ' ', '.');

            // If it was a hidden file and we lost the dot, add it back
            if (shouldBeHiddenFile && !postTruncationTrimmed.StartsWith(".", StringComparison.Ordinal) && !string.IsNullOrEmpty(postTruncationTrimmed))
            {
                postTruncationTrimmed = "." + postTruncationTrimmed.TrimStart('.');
            }

            // Final validity guard to prevent invalid path components
            var trimmedCore = postTruncationTrimmed.Trim('_', ' ', '.');
            if (string.IsNullOrWhiteSpace(trimmedCore) || trimmedCore == "." || trimmedCore == "..")
                throw new ArgumentException(EmptyFilenameErrorMessage, nameof(filename));

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
            if (string.IsNullOrEmpty(extension))
                return result;

            result = ProcessExtension(result, extension);
            result = TruncateIfNeeded(result);
            ValidateExtensionResult(result);

            return result;
        }

        /// <summary>
        /// Processes the extension, handling blocked extensions
        /// </summary>
        private static string ProcessExtension(string result, string extension)
        {
            if (IsBlockedExtension(extension))
            {
                return ApplyBlockedExtension(result);
            }

            return result + extension;
        }

        /// <summary>
        /// Applies a blocked extension to the filename
        /// </summary>
        private static string ApplyBlockedExtension(string result)
        {
            result = result.TrimEnd('.');
            result = $"{result}.blocked";

            ValidateBlockedExtensionResult(result);

            return result;
        }

        /// <summary>
        /// Validates that the blocked extension was properly applied
        /// </summary>
        private static void ValidateBlockedExtensionResult(string result)
        {
            var baseAfterBlock = RemoveFileExtension(result).Trim('_', ' ', '.');
            if (string.IsNullOrWhiteSpace(baseAfterBlock) || baseAfterBlock == "." || baseAfterBlock == "..")
            {
                throw new ArgumentException($"Filename sanitizes to an invalid state after extension blocking: '{result}'.", nameof(result));
            }

            if (!result.EndsWith(".blocked", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Failed to properly apply blocked extension to: '{result}'", nameof(result));
            }
        }

        /// <summary>
        /// Truncates the filename if it exceeds the maximum length
        /// </summary>
        private static string TruncateIfNeeded(string result)
        {
            if (result.Length <= MaxFileNameLength)
                return result;

            var finalExtension = GetFileExtension(result);
            var namePart = RemoveFileExtension(result);

            if (!string.IsNullOrEmpty(finalExtension))
            {
                return TruncateNamePart(namePart, finalExtension);
            }

            return TruncateToMaxLength(result);
        }

        /// <summary>
        /// Truncates the name part to fit within the maximum length
        /// </summary>
        /// <param name="namePart">The name part to truncate</param>
        /// <param name="finalExtension">The final extension</param>
        /// <returns>The truncated name part with extension</returns>
        private static string TruncateNamePart(string namePart, string finalExtension)
        {
            var availableLength = MaxFileNameLength - finalExtension.Length;

            if (availableLength <= 0)
                return CreateMinimalNameWithExtension(finalExtension);

            namePart = TruncateNamePartToLength(namePart, availableLength);
            return namePart + finalExtension;
        }

        /// <summary>
        /// Creates a minimal name when there's no space for the original name
        /// </summary>
        private static string CreateMinimalNameWithExtension(string finalExtension)
        {
            var extensionLength = System.Math.Max(0, MaxFileNameLength - finalExtension.Length);
            if (extensionLength > 0)
            {
                return SafeSubstring("file", 0, System.Math.Max(1, extensionLength)) + finalExtension;
            }

            return ".f" + finalExtension;
        }

        /// <summary>
        /// Truncates the name part to fit within the available length
        /// </summary>
        /// <param name="namePart">The name part to truncate</param>
        /// <param name="availableLength">The available length</param>
        /// <returns>The truncated name part</returns>
        private static string TruncateNamePartToLength(string namePart, int availableLength)
        {
            if (namePart.StartsWith(".", StringComparison.Ordinal))
            {
                return TruncateHiddenFileName(namePart, availableLength);
            }

            return SafeSubstring(namePart, 0, availableLength);
        }

        /// <summary>
        /// Truncates a hidden filename while preserving the leading dot
        /// </summary>
        private static string TruncateHiddenFileName(string namePart, int availableLength)
        {
            var contentPart = namePart.Substring(1);
            var contentBudget = System.Math.Max(0, availableLength - 1);

            if (contentBudget <= 0)
                return ".f";

            var truncatedContent = SafeSubstring(contentPart, 0, contentBudget);
            if (truncatedContent.Length > 0)
                return "." + truncatedContent;

            var minimalBudget = System.Math.Max(1, contentBudget);
            return "." + SafeSubstring(contentPart, 0, minimalBudget);
        }

        /// <summary>
        /// Validates the result after extension processing
        /// </summary>
        /// <param name="result">The result to validate</param>
        private static void ValidateExtensionResult(string result)
        {
            // Extract extension to validate
            var extension = GetFileExtension(result);

            // If it's a blocked extension, ensure it was properly replaced
            if (!string.IsNullOrEmpty(extension) &&
                extension.Equals(".blocked", StringComparison.OrdinalIgnoreCase))
            {
                // Validate that the base part is not empty
                var basePart = RemoveFileExtension(result).Trim('_', ' ', '.');
                if (string.IsNullOrWhiteSpace(basePart) || basePart == "." || basePart == "..")
                {
                    throw new ArgumentException($"Extension processing resulted in invalid filename: '{result}'", nameof(result));
                }
            }
        }

        /// <summary>
        /// Validates the final result after all processing
        /// </summary>
        /// <param name="result">The final sanitized filename</param>
        private static void ValidateFinalResult(string result)
        {
            var baseResult = RemoveFileExtension(result);
            var finalBaseTrimmed = baseResult.Trim('_', ' ', '.');
            if (string.IsNullOrWhiteSpace(finalBaseTrimmed) || finalBaseTrimmed == "." || finalBaseTrimmed == "..")
            {
                throw new ArgumentException(EmptyFilenameErrorMessage, nameof(result));
            }
        }

        /// <summary>
        /// Creates a safe filename for cases where the name part is empty but extension is blocked
        /// </summary>
        /// <param name="normalized">The normalized filename</param>
        /// <param name="extension">The extension to process</param>
        /// <returns>A safe filename with blocked extension</returns>
        private static string CreateSafeNameForBlockedExtension(string normalized)
        {
            // Determine if the file should be treated as hidden based on whether the normalized filename starts with a dot
            var isHidden = normalized.StartsWith(".", StringComparison.Ordinal);

            // Reuse the existing validated method that performs comprehensive validation
            return CreateAndValidateSafeBlockedFilename(isHidden);
        }

        /// <summary>
        /// Sanitizes the base name of the filename (without extension)
        /// </summary>
        /// <param name="nameWithoutExtension">The name part to sanitize</param>
        /// <returns>The sanitized name part</returns>
        private static string SanitizeBaseName(string nameWithoutExtension)
        {
            // Sanitize characters by replacing invalid ones (this follows the original approach but with security enhancements)
            var sanitized = SanitizeInvalidCharacters(nameWithoutExtension);

            // Process consecutive dots (this should be done after character sanitization)
            var processed = ProcessConsecutiveDots(sanitized);

            // Check if the processed filename would become "." or ".." after trimming problematic characters
            // This validation must happen before any trimming to prevent bypassing safeguards
            var processedTrimmed = processed.Trim('_', ' ', '.');
            if (processedTrimmed == "." || processedTrimmed == "..")
                throw new ArgumentException(EmptyFilenameErrorMessage, nameof(nameWithoutExtension));

            // Check if the processed name is "." or ".." before trimming - these are invalid path components
            if (processed == "." || processed == "..")
            {
                throw new ArgumentException(EmptyFilenameErrorMessage, nameof(nameWithoutExtension));
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
            var reservedResult = _reservedNameHandler.Handle(result);

            // Check if the reserved name handler returned null
            if (reservedResult == null)
                throw new ArgumentException(EmptyFilenameErrorMessage, nameof(result));

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
        /// Revalidates the hidden-name core to ensure that hidden file status is properly preserved
        /// and validated after all other processing steps.
        /// </summary>
        /// <param name="result">The filename after all other processing</param>
        /// <returns>The filename with validated hidden file status</returns>
        private static string RevalidateHiddenNameCore(string result)
        {
            // Revalidate that if the filename starts with a dot, it has meaningful content after sanitization
            if (result.StartsWith(".", StringComparison.Ordinal))
            {
                // Extract the content after the leading dot
                var contentAfterDot = result.Length > 1 ? result.Substring(1) : string.Empty;

                // Ensure that the content after the dot is meaningful (not empty, not just fillers)
                var trimmedContent = contentAfterDot.Trim('_', ' ', '.');

                // If the trimmed content is empty, contains only "." or "..", or is whitespace, it's invalid
                if (string.IsNullOrWhiteSpace(trimmedContent) || trimmedContent == "." || trimmedContent == "..")
                {
                    throw new ArgumentException("Hidden file has invalid content after sanitization.", nameof(result));
                }
            }

            // Additional validation: ensure that the result is not just a dot or a dot followed by only fillers
            var trimmedResult = result.Trim('_', ' ', '.');
            if (trimmedResult == "." || string.IsNullOrWhiteSpace(trimmedResult))
            {
                throw new ArgumentException("Filename sanitizes to an invalid hidden file name.", nameof(result));
            }

            return result;
        }

        /// <summary>
        /// Sanitizes invalid characters by using an allowlist approach.
        /// Only allows alphanumeric characters, dots, hyphens, underscores, and valid surrogate pairs (emojis).
        /// Invalid characters are replaced with underscores, but consecutive invalid characters
        /// are consolidated to a single underscore to prevent collision issues.
        /// For hidden files (starting with '.'), invalid characters are removed entirely.
        /// </summary>
        /// <param name="input">The input string to sanitize</param>
        /// <returns>The sanitized string</returns>
        private static string SanitizeInvalidCharacters(string? input)
        {
            if (input == null)
                return string.Empty;

            var resultBuilder = new StringBuilder();
            var i = 0;
            var isHiddenFile = input.StartsWith(".", StringComparison.Ordinal);

            while (i < input.Length)
            {
                i += ProcessCharacter(input, resultBuilder, i, isHiddenFile);
            }

            return resultBuilder.ToString();
        }

        /// <summary>
        /// Processes a single character in the input string
        /// </summary>
        private static int ProcessCharacter(string input, StringBuilder resultBuilder, int i, bool isHiddenFile)
        {
            var c = input[i];

            if (IsSurrogatePair(input, i, c))
            {
                return HandleSurrogatePair(input, resultBuilder, i);
            }

            HandleRegularCharacter(resultBuilder, c, isHiddenFile);
            return 1;
        }

        /// <summary>
        /// Checks if current position is a surrogate pair
        /// </summary>
        private static bool IsSurrogatePair(string input, int i, char c)
        {
            return char.IsHighSurrogate(c) && i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]);
        }

        /// <summary>
        /// Handles a surrogate pair (emoji or other character outside BMP)
        /// </summary>
        private static int HandleSurrogatePair(string input, StringBuilder resultBuilder, int i)
        {
            resultBuilder.Append(input[i]);
            resultBuilder.Append(input[i + 1]);
            return 2; // Return increment of 2 to advance past the surrogate pair
        }

        /// <summary>
        /// Handles a regular (non-surrogate) character
        /// For hidden files (starting with '.'), only forward slash and backslash are replaced with underscores,
        /// while other invalid characters are removed entirely. Note that '.' is considered safe and is preserved.
        /// </summary>
        private static void HandleRegularCharacter(StringBuilder resultBuilder, char c, bool isHiddenFile)
        {
            if (IsSafeCharacter(c))
            {
                resultBuilder.Append(c);
            }
            else if (isHiddenFile)
            {
                // For hidden files, only replace path traversal characters with underscores
                // Remove other invalid characters entirely
                if (IsPathTraversalCharacter(c))
                {
                    AppendUnderscoreIfNotDuplicate(resultBuilder);
                }
                // Otherwise, do not append anything (remove entirely)
            }
            else
            {
                AppendUnderscoreIfNotDuplicate(resultBuilder);
            }
        }

        /// <summary>
        /// Checks if a character is a path traversal character
        /// </summary>
        private static bool IsPathTraversalCharacter(char c)
        {
            return c == '.' || c == '/' || c == '\\';
        }

        /// <summary>
        /// Checks if a character is safe for filenames
        /// </summary>
        private static bool IsSafeCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_';
        }

        /// <summary>
        /// Appends an underscore if the last character is not already an underscore
        /// </summary>
        private static void AppendUnderscoreIfNotDuplicate(StringBuilder resultBuilder)
        {
            if (resultBuilder.Length == 0 || resultBuilder[resultBuilder.Length - 1] != '_')
            {
                resultBuilder.Append('_');
            }
        }

        /// <summary>
        /// Processes consecutive dots by replacing multiple consecutive dots with a single dot.
        /// Added null check to improve security and robustness.
        /// </summary>
        /// <param name="input">The input string to process</param>
        /// <returns>The processed string</returns>
        private static string ProcessConsecutiveDots(string? input)
        {
            if (input == null)
                return string.Empty;

            // Replace multiple consecutive dots with a single dot
            var result = new StringBuilder();

            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];

                if (c == '.')
                {
                    // Add a dot only if the previous character wasn't a dot
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
            if (!HasValidDotPosition(filename))
                return -1;

            var lastDotIndex = filename.LastIndexOf('.');
            var potentialExtension = filename.Substring(lastDotIndex);
            var extNormalized = potentialExtension.Normalize(NormalizationForm.FormC);

            if (!IsValidExtensionStructure(extNormalized))
                return -1;

            if (IsDotFileExtension(lastDotIndex))
                return HandleDotFileExtension(extNormalized);

            return lastDotIndex;
        }

        /// <summary>
        /// Checks if the filename has a valid dot position for an extension
        /// </summary>
        private static bool HasValidDotPosition(string filename)
        {
            if (filename == "." || filename == "..")
                return false;

            var lastDotIndex = filename.LastIndexOf('.');
            return lastDotIndex >= 0 && lastDotIndex < filename.Length - 1;
        }

        /// <summary>
        /// Validates the structure of the extension
        /// </summary>
        private static bool IsValidExtensionStructure(string extNormalized)
        {
            if (EndsWithInvalidCharacter(extNormalized))
                return false;

            var extensionPart = extNormalized.Substring(1);

            if (!HasValidExtensionContent(extensionPart))
                return false;

            if (ContainsInvalidCharacters(extensionPart))
                return false;

            if (!HasValidExtensionCharacters(extensionPart))
                return false;

            if (ContainsPathSeparators(extNormalized))
                return false;

            if (ContainsInvalidFilenameChars(extNormalized))
                return false;

            return true;
        }

        /// <summary>
        /// Checks if the extension ends with an invalid character
        /// </summary>
        private static bool EndsWithInvalidCharacter(string extNormalized)
        {
            return char.IsWhiteSpace(extNormalized[extNormalized.Length - 1]) || extNormalized[extNormalized.Length - 1] == '.';
        }

        /// <summary>
        /// Checks if the extension has valid content
        /// </summary>
        private static bool HasValidExtensionContent(string extensionPart)
        {
            var trimmedPart = extensionPart.Trim('_', ' ', '.');
            return trimmedPart.Length > 0 && trimmedPart.Any(c => char.IsLetterOrDigit(c));
        }

        /// <summary>
        /// Checks if the extension contains invalid characters (bidi/control)
        /// </summary>
        private static bool ContainsInvalidCharacters(string extensionPart)
        {
            return extensionPart.Any(ch => char.IsControl(ch) ||
                                       (ch >= '\u202A' && ch <= '\u202E') ||
                                       (ch >= '\u2066' && ch <= '\u2069'));
        }

        /// <summary>
        /// Checks if the extension has valid characters
        /// </summary>
        private static bool HasValidExtensionCharacters(string extensionPart)
        {
            return extensionPart.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || char.IsDigit(c) || c == '-' || c == '_');
        }

        /// <summary>
        /// Checks if the extension contains path separators
        /// </summary>
        private static bool ContainsPathSeparators(string extNormalized)
        {
            return extNormalized.Contains('/') || extNormalized.Contains('\\');
        }

        /// <summary>
        /// Checks if the extension contains invalid filename characters
        /// </summary>
        private static bool ContainsInvalidFilenameChars(string extNormalized)
        {
            return extNormalized.Any(c => InvalidFileNameChars.Contains(c));
        }

        /// <summary>
        /// Checks if this is a dot file extension
        /// </summary>
        private static bool IsDotFileExtension(int lastDotIndex)
        {
            return lastDotIndex == 0;
        }

        /// <summary>
        /// Handles dot file extensions (files starting with a dot)
        /// </summary>
        private static int HandleDotFileExtension(string extNormalized)
        {
            if (IsBlockedExtension(extNormalized))
                return 0; // Return 0 for simple dotfiles with dangerous extensions like ".exe"

            return -1; // Don't treat simple dotfiles as having extensions unless they're dangerous
        }

        /// <summary>
        /// Gets the file extension from a filename.
        /// </summary>
        /// <param name="filename">The filename to extract extension from.</param>
        /// <returns>The file extension or empty string if no extension.</returns>
        private static string GetFileExtension(string filename)
        {
            var extensionStartIndex = FindExtensionStartIndex(filename);
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
            var extensionStartIndex = FindExtensionStartIndex(filename);
            if (extensionStartIndex != -1)
            {
                return filename.Substring(0, extensionStartIndex);
            }
            return filename;
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
        /// - Normalize startIndex to prevent negative values
        /// - Improve length validation (change `length < 0` to `length <= 0`)
        /// - Fix the surrogate pair boundary check to prevent IndexOutOfRangeException
        /// - Ensure all boundary conditions are properly handled
        /// - Enhanced with improved surrogate pair boundary checks
        /// </summary>
        /// <param name="str">The input string</param>
        /// <param name="startIndex">Start index</param>
        /// <param name="length">Length to extract</param>
        /// <returns>The substring</returns>
        private static string SafeSubstring(string str, int startIndex, int length)
        {
            if (str == null)
                return string.Empty;

            startIndex = NormalizeStartIndex(startIndex);

            if (length <= 0)
                return string.Empty;

            if (startIndex >= str.Length)
                return string.Empty;

            var endIndex = CalculateEndIndex(str, startIndex, length);
            var actualLength = endIndex - startIndex;

            if (actualLength <= 0)
                return string.Empty;

            endIndex = AdjustEndIndexForSurrogatePair(str, endIndex);
            actualLength = endIndex - startIndex;

            if (actualLength <= 0)
                return string.Empty;

            startIndex = AdjustStartIndexForSurrogatePair(str, startIndex);
            actualLength = endIndex - startIndex;

            if (actualLength <= 0)
                return string.Empty;

            return str.Substring(startIndex, actualLength);
        }

        /// <summary>
        /// Normalizes start index to prevent negative values
        /// </summary>
        private static int NormalizeStartIndex(int startIndex)
        {
            return startIndex < 0 ? 0 : startIndex;
        }

        /// <summary>
        /// Calculates end index for substring
        /// </summary>
        private static int CalculateEndIndex(string str, int startIndex, int length)
        {
            var endIndex = startIndex + length;
            return endIndex > str.Length ? str.Length : endIndex;
        }

        /// <summary>
        /// Adjusts end index to avoid splitting surrogate pairs
        /// </summary>
        private static int AdjustEndIndexForSurrogatePair(string str, int endIndex)
        {
            if (endIndex < str.Length && endIndex > 0)
            {
                var currentChar = str[endIndex];
                var previousChar = str[endIndex - 1];

                if (char.IsHighSurrogate(previousChar) && char.IsLowSurrogate(currentChar))
                {
                    return endIndex - 1;
                }
            }
            return endIndex;
        }

        /// <summary>
        /// Adjusts start index to avoid starting in middle of a surrogate pair
        /// </summary>
        private static int AdjustStartIndexForSurrogatePair(string str, int startIndex)
        {
            if (startIndex < str.Length && startIndex > 0 &&
                char.IsLowSurrogate(str[startIndex]))
            {
                // Check if the previous character is a high surrogate
                if (char.IsHighSurrogate(str[startIndex - 1]))
                {
                    // Skip the low surrogate to avoid starting in the middle of a surrogate pair
                    return startIndex + 1;
                }
                // If previous character is not a high surrogate, we have an isolated low surrogate
                // Skip it to prevent malformed string output
                return startIndex + 1;
            }
            return startIndex;
        }
    }
}
