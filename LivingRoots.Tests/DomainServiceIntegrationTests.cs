using System;
using Moq;
using LivingRoots.Domain;
using Xunit;

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
                
            _fileNameSanitizationService = new FileNameSanitizationService(_mockUnicodeNormalizationService.Object, _mockReservedNameHandler.Object);
            _pathValidationService = new PathValidationService();
        }

        [Fact]
        public void Integration_UnicodeNormalizationAndFileNameSanitization_WorksTogether()
        {
            // Arrange
            var input = "tеst.txt"; // Contains Cyrillic 'е' that should be normalized
            var normalized = "test.txt"; // Expected normalized output
            
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(input))
                .Returns(normalized);

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
            
            // First, sanitize the filename
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(fileName))
                .Returns(fileName);
            
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
            
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(input))
                .Returns(normalized);

            // Act
            var result = _fileNameSanitizationService.Sanitize(input);

            // Assert
            Assert.Equal("malicious_blocked.exe", result);
        }

        [Fact]
        public void Integration_WithPathTraversalInFilename_ThrowsArgumentException()
        {
            // Arrange
            var input = "../malicious_file.txt";
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _fileNameSanitizationService.Sanitize(input));
            Assert.Contains("Filename cannot contain path traversal sequences.", exception.Message);
        }
    }
}