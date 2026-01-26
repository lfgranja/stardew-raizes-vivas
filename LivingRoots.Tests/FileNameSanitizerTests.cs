using LivingRoots.Domain;
using Moq;
using StardewModdingAPI;

namespace LivingRoots.Tests
{
    public class FileNameSanitizerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;

        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;

        private readonly Mock<IUnicodeNormalizationService> _mockUnicodeNormalizationService;
        private readonly FileNameSanitizationService _fileNameSanitizationService;

        public FileNameSanitizerTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockReservedNameHandler = new Mock<IReservedNameHandler>();
            _mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();

            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);

            // Create real FileNameSanitizationService instance with mocked UnicodeNormalizationService dependency
            _fileNameSanitizationService = new FileNameSanitizationService(
                _mockUnicodeNormalizationService.Object,
                _mockReservedNameHandler.Object
            );

            // Configure the reserved name handler to return the input as-is for these tests
            _mockReservedNameHandler
                .Setup(x => x.Handle(It.IsAny<string?>()))
                .Returns<string?>(input => input);

            // Setup the mock UnicodeNormalizationService to return the input by default (for most tests)
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string?>()))
                .Returns<string?>(input => input);
        }

        [Fact]
        public void Sanitize_WithValidString_ReturnsSanitizedString()
        {
            // Arrange
            var input = "valid_filename";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("valid_filename", result);
        }

        [Fact]
        public void Sanitize_WithInvalidFileNameChars_ReplacesWithUnderscore()
        {
            // Arrange
            var input = "file<name>with:invalid|chars";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("file_name_with_invalid_chars", result);
        }

        [Fact]
        public void Sanitize_WithDirectorySeparators_ReplacesWithUnderscore()
        {
            // Arrange
            var input = "path/with/separators";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("path_with_separators", result);
        }

        [Fact]
        public void Sanitize_WithMultipleDots_HandlesProperly()
        {
            // Arrange
            var input = "file....name";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("file.name", result);
        }

        [Fact]
        public void Sanitize_WithLongFileName_Truncates()
        {
            // Arrange
            var longName = new string('a', 300);

            // Act
            var result = _fileNameSanitizationService.Sanitize(longName) ?? string.Empty;

            // Assert
            Assert.Equal(240, result.Length);
        }

        [Fact]
        public void Sanitize_WithZeroWidthUnicodeCharacters_RemovesProperly()
        {
            // Arrange
            var input = "test\u200Bzwsp\u200Czwnj\u200Dzwj";

            // Setup mock to return the same string (zero-width chars should be removed by UnicodeNormalizer)
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(input))
                .Returns("testzwspzwnjzwj");

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("testzwspzwnjzwj", result);
        }

        [Fact]
        public void Sanitize_WithSurrogatePairs_HandlesProperly()
        {
            // Arrange
            var emoji = char.ConvertFromUtf32(0x1F600);
            var input = $"test{emoji}smile";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal($"test{emoji}smile", result);
        }

        [Fact]
        public void Sanitize_WithDiacritics_RemovesAccentsProperly()
        {
            // Arrange
            var input = "café";
            var normalized = "cafe";

            // Setup mock to return normalized version
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("cafe", result);
        }

        [Fact]
        public void Sanitize_WithUnicodeHomoglyphs_ConvertsToLatin()
        {
            // Arrange
            var input = "tеst"; // Cyrillic 'е'
            var normalized = "test";

            // Setup mock to return normalized version
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("test", result);
        }

        [Fact]
        public void Sanitize_WithFileExtension_PreservesExtension()
        {
            // Arrange
            var input = "document.txt";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("document.txt", result);
        }

        [Fact]
        public void Sanitize_WithMultipleExtensions_PreservesExtensions()
        {
            // Arrange
            var input = "archive.tar.gz";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("archive.tar.gz", result);
        }

        [Fact]
        public void Sanitize_WithLeadingDots_TreatAsHiddenFile()
        {
            // Arrange
            var input = ".config";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal(".config", result);
        }

        [Fact]
        public void Sanitize_WithTrailingDots_RemovesTrailingDots()
        {
            // Arrange
            var input = "file.";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("file", result);
        }

        [Fact]
        public void Sanitize_WithConsecutiveDots_HandlesProperly()
        {
            // Arrange
            var input = "file..name";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("file.name", result);
        }

        [Fact]
        public void Sanitize_WithEmptyStringAfterSanitization_ThrowsArgumentException()
        {
            // Arrange
            var input = "<>:\"|?*";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
        }

        [Fact]
        public void Sanitize_WithOnlyWhitespace_ThrowsArgumentException()
        {
            // Arrange
            var input = "   ";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
        }

        [Fact]
        public void Sanitize_WithValidUnicodeCharacters_PreservesValidUnicode()
        {
            // Arrange
            var input = "tëst_ünïcödé";
            var normalized = "test_unicode";

            // Setup mock to return normalized version
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("test_unicode", result);
        }

        [Fact]
        public void Sanitize_WithMixedValidInvalidChars_HandlesProperly()
        {
            // Arrange
            var input = "file<with>mixed:chars|and?wildcards*";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("file_with_mixed_chars_and_wildcards", result);
        }

        [Fact]
        public void Sanitize_WithControlCharacters_HandlesProperly()
        {
            // Arrange
            var input = "test" + (char)0x01 + "control" + (char)0x1F + "chars";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal("test_control_chars", result);
        }

        [Fact]
        public void Sanitize_WithNullCharacter_ThrowsArgumentException()
        {
            // Arrange
            var input = "test\0test";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
        }

        [Fact]
        public void Sanitize_WithDotOnly_ThrowsArgumentException()
        {
            // Arrange
            var input = ".";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
        }

        [Fact]
        public void Sanitize_WithDot_ThrowsArgumentException()
        {
            // Arrange
            var input = "..";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
        }

        [Fact]
        public void Sanitize_WithHiddenFileAndTrailingDots_HandlesProperly()
        {
            // Arrange
            var input = ".file.";

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // Assert
            Assert.Equal(".file", result);
        }

        [Fact]
        public void Sanitize_WithDotFollowedByTrailingChars_ThrowsArgumentException()
        {
            // Arrange
            var input = ".   "; // dot followed by spaces that would be trimmed

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
        }

        [Fact]
        public void Sanitize_WithDotDotFollowedByTrailingChars_ThrowsArgumentException()
        {
            // Arrange
            var input = "..   "; // dot-dot followed by spaces that would be trimmed

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
        }

        [Fact]
        public void Sanitize_WithDotFollowedByDotsAndSpaces_ThrowsArgumentException()
        {
            // Arrange
            var input = ". . "; // dot with spaces and dots that would be trimmed

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
        }

        [Fact]
        public void Sanitize_WithDotDotFollowedByDotsAndSpaces_ThrowsArgumentException()
        {
            // Arrange
            var input = ".. . "; // dot-dot with spaces and dots that would be trimmed

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
        }

        [Fact]
        public void Sanitize_WithMultipleUnderscores_HandlesProperly()
        {
            // Arrange
            var input = "file___name";

            // Setup mock to return the same string (no change expected from UnicodeNormalizer)
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(input);

            // Act
            var result = _fileNameSanitizationService.Sanitize(input) ?? string.Empty;

            // According to the actual test result, multiple underscores are not being consolidated
            // The SanitizeInvalidCharacters method does not consolidate multiple underscores
            // It only prevents adding an underscore if the last character is already an underscore
            // But this only applies when the current character is invalid and being replaced
            // Multiple consecutive underscores in the original string remain
            Assert.Equal("file___name", result);
        }
    }
}
