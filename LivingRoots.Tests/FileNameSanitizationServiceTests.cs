using System;
using Moq;
using LivingRoots.Domain;
using Xunit;

namespace LivingRoots.Tests
{
    public class FileNameSanitizationServiceTests
    {
        private readonly Mock<IUnicodeNormalizationService> _mockUnicodeNormalizationService;
        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;
        private readonly FileNameSanitizationService _service;

        public FileNameSanitizationServiceTests()
        {
            _mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            _mockReservedNameHandler = new Mock<IReservedNameHandler>();
            
            // Setup the ReservedNameHandler to return the input by default
            _mockReservedNameHandler
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns<string>(s => s);
                
            _service = new FileNameSanitizationService(_mockUnicodeNormalizationService.Object, _mockReservedNameHandler.Object);
        }

        [Fact]
        public void Constructor_WithNullUnicodeNormalizationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileNameSanitizationService(null!, _mockReservedNameHandler.Object));
        }

        [Fact]
        public void Constructor_WithNullReservedNameHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileNameSanitizationService(_mockUnicodeNormalizationService.Object, null!));
        }

        [Fact]
        public void Sanitize_WithNullFilename_ReturnsNull()
        {
            // Act
            var result = _service.Sanitize(null!);

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
        public void Sanitize_WithHiddenNameThatWouldCollapseToDot_ThrowsArgumentException()
        {
            // This test addresses the first issue: Prevent Hidden-Name Collapsing to "."
            // A hidden filename like ".   " should be sanitized to ".", which is invalid
            // We need to add a check to throw an ArgumentException if the content of a hidden file becomes empty after sanitization

            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".   "))
                .Returns(".   ");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".   "));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithHiddenNameThatWouldCollapseToDotWithOtherChars_ThrowsArgumentException()
        {
            // Another variation of the first issue
            // A hidden filename like ".<>" should become "." after sanitization, which is invalid

            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".<>"))
                .Returns(".<>");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".<>"));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithPathTraversalSequences_HandlesProperly()
        {
            // Setup the mock to return a non-null value for path traversal inputs
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("../test"))
                .Returns("../test");
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("..\\test"))
                .Returns("..\\test");
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("..test"))
                .Returns("..test");
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.."))
                .Returns("test..");
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.Is<string>(s => s != "../test" && s != "..\\test" && s != "..test" && s != "test..")))
                .Returns<string>(s => s);

            // Test ../ path traversal - this may result in an exception or a transformed result
            // Path traversal should be handled by PathValidationService at a higher level
            try
            {
                var result1 = _service.Sanitize("../test");
                // If no exception, verify it's properly transformed
                Assert.NotEqual("../test", result1); // Should be transformed
            }
            catch (ArgumentException ex)
            {
                // Exception is acceptable for path traversal attempts
                Assert.Contains("sanitizes", ex.Message.ToLower());
            }

            // Test ..\ path traversal - this may result in an exception or a transformed result
            try
            {
                var result2 = _service.Sanitize("..\\test");
                // If no exception, verify it's properly transformed
                Assert.NotEqual("..\\test", result2); // Should be transformed
            }
            catch (ArgumentException ex)
            {
                // Exception is acceptable for path traversal attempts
                Assert.Contains("sanitizes", ex.Message.ToLower());
            }

            // Test starts with .. - this may result in an exception or a transformed result
            try
            {
                var result3 = _service.Sanitize("..test");
                // If no exception, verify it's properly transformed
                Assert.NotEqual("..test", result3); // Should be transformed
            }
            catch (ArgumentException ex)
            {
                // Exception is acceptable for path traversal attempts
                Assert.Contains("sanitizes", ex.Message.ToLower());
            }

            // Test ends with .. - should work normally (results in "test")
            var result4 = _service.Sanitize("test..");
            Assert.Equal("test", result4);
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
            Assert.Equal("test.blocked", result); // Updated to reflect the fix
        }

        [Fact]
        public void Sanitize_WithPathTraversalShouldBeHandledByPathValidationService()
        {
            // Arrange - Path traversal should be handled by PathValidationService, not FileNameSanitizationService
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("normal_file.txt"))
                .Returns("normal_file.txt");

            // Act
            var result = _service.Sanitize("normal_file.txt");

            // Assert - Normal files should still be processed correctly
            Assert.Equal("normal_file.txt", result);
        }

        [Fact]
        public void Sanitize_WhenNormalizationReturnsNull_ThrowsArgumentException()
        {
            // Arrange
            var mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            var mockReservedNameHandler = new Mock<IReservedNameHandler>();
            
            // Setup the Unicode normalization service to return null
            mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns((string?)null);
                
            // Setup the ReservedNameHandler to return the input by default
            mockReservedNameHandler
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            var service = new FileNameSanitizationService(
                mockUnicodeNormalizationService.Object, 
                mockReservedNameHandler.Object);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.Sanitize("test.txt"));
            Assert.Contains("Normalized filename is null", exception.Message);
        }

        [Fact]
        public void Sanitize_WithBlockedExtension_RemovesDangerousExtensionInsteadOfAppendingBlocked()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.exe"))
                .Returns("test.exe");

            // Act
            var result = _service.Sanitize("test.exe");

            // Assert - The current implementation produces "test_blocked.exe" which is still dangerous
            // The fixed implementation should produce "test.blocked" or "test_safe"
            Assert.Equal("test.blocked", result); // This test will fail with current implementation
        }
        
        [Fact]
        public void Sanitize_WithMultipleBlockedExtensions_RemovesDangerousExtension()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.dll"))
                .Returns("test.dll");

            // Act
            var result = _service.Sanitize("test.dll");

            // Assert - Should not keep the dangerous extension
            Assert.Equal("test.blocked", result); // This test will fail with current implementation
        }
        
        [Fact]
        public void Sanitize_WithBlockedExtensionInComplexName_RemovesDangerousExtension()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("my_file.exe"))
                .Returns("my_file.exe");

            // Act
            var result = _service.Sanitize("my_file.exe");

            // Assert - Should not keep the dangerous extension
            Assert.Equal("my_file.blocked", result); // This test will fail with current implementation
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
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
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
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
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
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }
        
        [Fact]
        public void Sanitize_WithReservedNameHandlerReturningNull_ThrowsArgumentException()
        {
            // Arrange
            var mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            var mockReservedNameHandler = new Mock<IReservedNameHandler>();
            
            // Setup the reserved name handler to return null
            mockReservedNameHandler
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns((string?)null);
            
            var service = new FileNameSanitizationService(
                mockUnicodeNormalizationService.Object, 
                mockReservedNameHandler.Object);
            
            // Setup Unicode normalization to return the input
            mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(input => input);
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.Sanitize("test.txt"));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }
    }
}