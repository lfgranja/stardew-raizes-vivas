using LivingRoots.Domain;
using Moq;

namespace LivingRoots.Tests
{
    public class DomainServiceIntegrationTests
    {
        private readonly Mock<IUnicodeNormalizationService> _mockUnicodeNormalizationService;
        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;
        private readonly IFileNameSanitizationService _fileNameSanitizationService;
        private readonly IPathValidationService _pathValidationService;

        public DomainServiceIntegrationTests()
        {
            _mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            _mockReservedNameHandler = new Mock<IReservedNameHandler>();

            // Setup ReservedNameHandler to return input by default
            _mockReservedNameHandler
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns<string>(s => s);

            _fileNameSanitizationService = new FileNameSanitizationService(
                _mockUnicodeNormalizationService.Object,
                _mockReservedNameHandler.Object
            );

            // Create mock Unicode service for PathValidationService
            var mockUnicodeForPathValidation = new Mock<IUnicodeNormalizationService>();
            mockUnicodeForPathValidation
                .Setup(s => s.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);

            _pathValidationService = new PathValidationService(mockUnicodeForPathValidation.Object);
        }

        [Fact]
        public void Integration_UnicodeNormalizationAndFileNameSanitization_WorksTogether()
        {
            // Arrange
            var input = "tеst.txt"; // Contains Cyrillic 'е' that should be normalized
            var normalized = "test.txt"; // Expected normalized output

            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);

            // Act
            var result = _fileNameSanitizationService.Sanitize(input);

            // Assert
            Assert.Equal("test.txt", result);
            _mockUnicodeNormalizationService.Verify(x => x.Normalize(input), Times.Once);
        }

        [Fact]
        public void Integration_PathValidationAndFileNameSanitization_SupportsValidPaths()
        {
            // This test verifies that sanitized filenames can be used in path validation
            var fileName = "valid_filename.txt";

            // First, sanitize filename
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(fileName)).Returns(fileName);

            var sanitized = _fileNameSanitizationService.Sanitize(fileName);

            // Then validate that it can be part of a path
            _pathValidationService.Validate($"folder/{sanitized}"); // Should not throw

            Assert.Equal("valid_filename.txt", sanitized);
        }

        [Fact]
        public void Integration_WithDangerousExtension_BlocksExtensionAfterSanitization()
        {
            // Arrange
            var input = "malicious.exe";
            var normalized = "malicious.exe"; // No change expected from normalization

            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);

            // Act
            var result = _fileNameSanitizationService.Sanitize(input);

            // Assert
            Assert.Equal("malicious.blocked", result);
        }

        [Fact]
        public void Integration_WithPathTraversalInFilename_DoesNotThrowInSanitizer()
        {
            // Arrange
            var input = "../malicious_file.txt";

            // Setup mock to return a non-null value for path traversal input
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(input);

            // Act - Path traversal should now pass through FileNameSanitizationService
            // and be caught by PathValidationService at a higher level
            var result = _fileNameSanitizationService.Sanitize(input)!;

            // Assert - The filename sanitizer itself should not block this (it's handled elsewhere)
            Assert.Equal("._malicious_file.txt", result); // Path traversal is now handled by PathValidationService
        }
    }
}
