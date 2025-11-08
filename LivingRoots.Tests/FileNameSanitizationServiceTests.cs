using System;
using Moq;
using LivingRoots.Domain;
using Xunit;

namespace LivingRoots.Tests
{
    public class FileNameSanitizationServiceTests
    {
        private readonly Mock<IUnicodeNormalizationService> _mockUnicodeNormalizationService;
        private readonly FileNameSanitizationService _service;

        public FileNameSanitizationServiceTests()
        {
            _mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            _service = new FileNameSanitizationService(_mockUnicodeNormalizationService.Object);
        }

        [Fact]
        public void Constructor_WithNullUnicodeNormalizationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileNameSanitizationService(null));
        }

        [Fact]
        public void Sanitize_WithNullFilename_ReturnsNull()
        {
            // Act
            var result = _service.Sanitize(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Sanitize_WithEmptyFilename_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(""));
            Assert.Contains("Filename cannot be empty or whitespace-only", exception.Message);
        }

        [Fact]
        public void Sanitize_WithWhitespaceOnlyFilename_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("   "));
            Assert.Contains("Filename cannot be empty or whitespace-only", exception.Message);
        }

        [Fact]
        public void Sanitize_WithNullCharacter_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("test\0file"));
            Assert.Contains("Filename cannot contain null characters", exception.Message);
        }

        [Fact]
        public void Sanitize_WithPathTraversalSequences_ThrowsArgumentException()
        {
            // Test ../ path traversal
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Sanitize("../test"));
            Assert.Contains("Filename cannot contain path traversal sequences.", exception1.Message);

            // Test ..\ path traversal
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Sanitize("..\\test"));
            Assert.Contains("Filename cannot contain path traversal sequences.", exception2.Message);

            // Test starts with ..
            var exception3 = Assert.Throws<ArgumentException>(() => _service.Sanitize("..test"));
            Assert.Contains("Filename cannot contain path traversal sequences.", exception3.Message);

            // Test ends with ..
            var exception4 = Assert.Throws<ArgumentException>(() => _service.Sanitize("test.."));
            Assert.Contains("Filename cannot contain path traversal sequences.", exception4.Message);
        }

        [Fact]
        public void Sanitize_WithValidFilename_ReturnsSanitizedFilename()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.txt"))
                .Returns("test.txt");

            // Act
            var result = _service.Sanitize("test.txt");

            // Assert
            Assert.Equal("test.txt", result);
            _mockUnicodeNormalizationService.Verify(x => x.Normalize("test.txt"), Times.Once);
        }

        [Fact]
        public void Sanitize_WithInvalidCharacters_RemovesInvalidCharacters()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test<>.txt"))
                .Returns("test<>.txt");

            // Act
            var result = _service.Sanitize("test<>.txt");

            // Assert
            Assert.Equal("test.txt", result);
        }

        [Fact]
        public void Sanitize_WithConsecutiveDots_RemovesConsecutiveDots()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test...file.txt"))
                .Returns("test...file.txt");

            // Act
            var result = _service.Sanitize("test...file.txt");

            // Assert
            Assert.Equal("test.file.txt", result);
        }

        [Fact]
        public void Sanitize_WithHiddenFile_PreservesLeadingDot()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".hidden_file.txt"))
                .Returns(".hidden_file.txt");

            // Act
            var result = _service.Sanitize(".hidden_file.txt");

            // Assert
            Assert.Equal(".hidden_file.txt", result);
        }

        [Fact]
        public void Sanitize_WithHiddenFileStartingWithInvalidChars_PreservesLeadingDot()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".<hidden_file.txt"))
                .Returns(".<hidden_file.txt");

            // Act
            var result = _service.Sanitize(".<hidden_file.txt");

            // Assert
            Assert.Equal(".hidden_file.txt", result);
        }

        [Fact]
        public void Sanitize_WithBlockedExtension_AddsBlockedIndicator()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.exe"))
                .Returns("test.exe");

            // Act
            var result = _service.Sanitize("test.exe");

            // Assert
            Assert.Equal("test.exe_blocked", result);
        }

        [Fact]
        public void Sanitize_WithValidExtension_KeepsExtension()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.txt"))
                .Returns("test.txt");

            // Act
            var result = _service.Sanitize("test.txt");

            // Assert
            Assert.Equal("test.txt", result);
        }

        [Fact]
        public void Sanitize_WithFileNameThatBecomesDot_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("."))
                .Returns(".");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("."));
            Assert.Contains("Filename cannot contain path traversal sequences.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithFileNameThatBecomesDotDot_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".."))
                .Returns("..");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".."));
            Assert.Contains("Filename cannot contain path traversal sequences.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithLongFilename_TruncatesToMaxLength()
        {
            // Arrange
            var longFilename = new string('a', 300) + ".txt";
            var expected = new string('a', 240) + ".txt";
            
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);

            // Act
            var result = _service.Sanitize(longFilename);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Sanitize_WithHiddenLongFilename_TruncatesToMaxLength()
        {
            // Arrange
            var longHiddenFilename = "." + new string('a', 300) + ".txt";
            var expected = "." + new string('a', 239) + ".txt";
            
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);

            // Act
            var result = _service.Sanitize(longHiddenFilename);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Sanitize_WithUnicodeCharacters_AppliesNormalization()
        {
            // Arrange
            var input = "tëst.txt";
            var normalized = "test.txt";
            
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(input))
                .Returns(normalized);

            // Act
            var result = _service.Sanitize(input);

            // Assert
            Assert.Equal("test.txt", result);
        }

        [Fact]
        public void Sanitize_WithResultThatBecomesEmpty_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("..."))
                .Returns("...");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("..."));
            Assert.Contains("Filename cannot contain path traversal sequences.", exception.Message);
        }
    }
}