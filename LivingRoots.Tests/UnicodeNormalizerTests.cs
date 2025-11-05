using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class UnicodeNormalizerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IPathTraversalValidator> _mockPathTraversalValidator;
        private readonly Mock<IFileNameSanitizer> _mockFileNameSanitizer;
        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly UnicodeNormalizer _unicodeNormalizer;

        public UnicodeNormalizerTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockPathTraversalValidator = new Mock<IPathTraversalValidator>();
            _mockFileNameSanitizer = new Mock<IFileNameSanitizer>();
            _mockReservedNameHandler = new Mock<IReservedNameHandler>();
            
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
            
            // Create a real UnicodeNormalizer instance for testing
            _unicodeNormalizer = new UnicodeNormalizer();
            
            // Configure the file name sanitizer mock to use real sanitization behavior
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => 
            {
                if (string.IsNullOrWhiteSpace(input))
                    return input;

                if (input.Contains('\0'))
                    throw new ArgumentException("Filename cannot contain null characters.", nameof(input));

                // Normalize Unicode characters
                string normalized = _unicodeNormalizer.Normalize(input);

                // Sanitize characters by replacing invalid ones
                string sanitized = SanitizeInvalidCharacters(normalized);

                // Process consecutive dots
                string processed = ProcessConsecutiveDots(sanitized);

                // Determine if this should be treated as a hidden file
                bool shouldBeHiddenFile = ShouldPreserveHiddenFilePrefix(input, processed);

                // Trim leading/trailing problematic characters
                string trimmed = processed.Trim('_', ' ', '.');

                // Preserve leading dots for hidden files
                if (shouldBeHiddenFile && !trimmed.StartsWith(".") && !string.IsNullOrEmpty(trimmed))
                {
                    trimmed = "." + trimmed;
                }

                // Apply truncation
                string truncated = TruncateToMaxLength(trimmed);

                // Final cleanup after truncation
                string result = PerformFinalCleanup(truncated, shouldBeHiddenFile);

                if (string.IsNullOrWhiteSpace(result))
                    throw new ArgumentException("Filename sanitizes to an empty string.", nameof(input));

                return result;
            });
            
            // Configure the reserved name handler to return the input as-is for these tests
            _mockReservedNameHandler.Setup(x => x.Handle(It.IsAny<string>())).Returns<string>(input => input);
            
            // Configure the path traversal validator to not throw for valid paths in these tests
            _mockPathTraversalValidator.Setup(x => x.Validate(It.IsAny<string>())).Verifiable();
        }

        /// <summary>
        /// Sanitizes invalid characters by replacing them with underscores.
        /// </summary>
        private static string SanitizeInvalidCharacters(string input)
        {
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
        private static bool ShouldPreserveHiddenFilePrefix(string originalFilename, string processedFilename)
        {
            return originalFilename.StartsWith(".") && !string.IsNullOrEmpty(processedFilename.Trim('_', ' ', '.'));
        }

        /// <summary>
        /// Truncates the filename to the maximum allowed length, handling hidden files properly.
        /// </summary>
        private static string TruncateToMaxLength(string filename)
        {
            const int MaxFileNameLength = 240;
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
        private static string PerformFinalCleanup(string filename, bool shouldBeHiddenFile)
        {
            const int MaxFileNameLength = 240;
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

        [Fact]
        public void SanitizeKey_WithUnicodeDiacritics_RemovesAccentsProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "café");
            service.SaveData(testData, "naïve");
            service.SaveData(testData, "résumé");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/naive.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/resume.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithUnicodeHomoglyphs_ConvertsToLatin()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "tеst"); // Cyrillic 'е'
            service.SaveData(testData, "аpple"); // Cyrillic 'а'
            service.SaveData(testData, "соmputer"); // Cyrillic 'с' and 'о'
            service.SaveData(testData, "рeach"); // Cyrillic 'р'
            service.SaveData(testData, "хtml"); // Cyrillic 'х'

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/apple.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/computer.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/peach.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/html.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithCombinedUnicodeDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "cafe\u0301");
            service.SaveData(testData, "naif\u0308");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/naif.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithPrecomposedUnicodeDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "café");
            service.SaveData(testData, "naïve");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/naive.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithZeroWidthUnicodeCharacters_RemovesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test\u200Bzwsp\u200Czwnj\u200Dzwj");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/testzwspzwnjzwj.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithHebrewText_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test שלום");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test שלום.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithArabicText_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test كتاب");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test كتاب.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithChineseText_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test 你好");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test 你好.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithEmoji_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };
            var emoji = char.ConvertFromUtf32(0x1F600);

            // Act
            service.SaveData(testData, $"test {emoji} smile");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/test {emoji} smile.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMixedUnicodeAndDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "café тест naïve");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe тecт naive.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithConsecutiveDiacritics_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "a\u0300\u0301b");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/ab.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithGreekTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "μέντι");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/μεντι.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithTurkishTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "göçmen");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/gocmen.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithThaiTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "คํา");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/คํา.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithDevanagariText_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "नमस्ते");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/नमस्ते.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMultipleNormalizationForms_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            string nfcForm = "café";
            string nfdForm = "cafe\u0301";

            service.SaveData(testData, nfcForm);
            service.SaveData(testData, nfdForm);

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe.json", testData), Times.Exactly(2));
        }

        [Fact]
        public void SanitizeKey_WithHomoglyphAndDiacriticsTogether_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "tеst\u0301");
            service.SaveData(testData, "cafe\u0435\u0301");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafeé.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithControlAndFormatUnicodeChars_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test\u200Estart");
            service.SaveData(testData, "test\u200Fend");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/teststart.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/testend.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithVeryLongUnicodeString_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            string input = new string('a', 100) + "café" + new string('b', 100) + "naïve" + new string('c', 100);
            service.SaveData(testData, input);

            // Assert
            string expected = new string('a', 100) + "cafe" + new string('b', 100) + "naive" + new string('c', 31);
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{expected}.json", testData), Times.Once);
        }
    }
}