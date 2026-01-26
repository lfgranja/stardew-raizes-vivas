using LivingRoots.Domain;
using Moq;

namespace LivingRoots.Tests
{
    public class DotSegmentSecurityTests
    {
        private readonly Mock<IUnicodeNormalizationService> _mockUnicodeNormalizationService;
        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;
        private readonly FileNameSanitizationService _service;

        public DotSegmentSecurityTests()
        {
            _mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            _mockReservedNameHandler = new Mock<IReservedNameHandler>();

            // Setup the ReservedNameHandler to return the input by default
            _mockReservedNameHandler
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns<string>(s => s);

            _service = new FileNameSanitizationService(
                _mockUnicodeNormalizationService.Object,
                _mockReservedNameHandler.Object
            );
        }

        [Fact]
        public void Sanitize_WithFilenameThatSanitizesToDot_ThrowsArgumentException()
        {
            // Arrange - filename that would sanitize to "."
            _mockUnicodeNormalizationService.Setup(x => x.Normalize("...")).Returns("...");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("..."));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithFilenameThatSanitizesToDotDot_ThrowsArgumentException()
        {
            // Arrange - filename that would sanitize to ".."
            _mockUnicodeNormalizationService.Setup(x => x.Normalize("..")).Returns("..");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".."));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithFilenameThatBecomesDotAfterCharacterSanitization_ThrowsArgumentException()
        {
            // Arrange - filename that becomes "." after invalid character removal
            _mockUnicodeNormalizationService.Setup(x => x.Normalize("<>")).Returns("<>");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("<>"));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithFilenameThatBecomesDotDotAfterCharacterSanitization_ThrowsArgumentException()
        {
            // Arrange - filename that becomes ".." after invalid character removal
            _mockUnicodeNormalizationService.Setup(x => x.Normalize("<..>")).Returns("<..>");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("<..>"));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithFilenameThatBecomesDotAfterTruncationAndCleanup_ThrowsArgumentException()
        {
            // Arrange - a complex case where the filename might become ".." after all processing
            _mockUnicodeNormalizationService.Setup(x => x.Normalize("...   ")).Returns("...   ");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("...   "));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithHiddenFileThatBecomesDotAfterProcessing_ThrowsArgumentException()
        {
            // Arrange - hidden file that becomes "." after processing
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(".<>")).Returns(".<>");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".<>"));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithHiddenFileThatBecomesDotDotAfterProcessing_ThrowsArgumentException()
        {
            // Arrange - hidden file that becomes ".." after processing
            _mockUnicodeNormalizationService.Setup(x => x.Normalize("..<")).Returns("..<");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("..<"));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithValidFileNameContainingDots_DoesNotThrow()
        {
            // Arrange - valid filename with legitimate dots should pass
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("file.name.txt"))
                .Returns("file.name.txt");

            // Act
            var result = _service.Sanitize("file.name.txt");

            // Assert
            Assert.Equal("file.name.txt", result);
        }

        [Fact]
        public void Sanitize_WithValidHiddenFile_DoesNotThrow()
        {
            // Arrange - valid hidden file should pass
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(".config")).Returns(".config");

            // Act
            var result = _service.Sanitize(".config");

            // Assert
            Assert.Equal(".config", result);
        }
    }
}
