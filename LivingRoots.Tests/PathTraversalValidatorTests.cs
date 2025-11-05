using System;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class PathTraversalValidatorTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IPathTraversalValidator> _mockPathTraversalValidator;
        private readonly Mock<IFileNameSanitizer> _mockFileNameSanitizer;
        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;
        private readonly Mock<IMonitor> _mockMonitor;

        public PathTraversalValidatorTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockPathTraversalValidator = new Mock<IPathTraversalValidator>();
            _mockFileNameSanitizer = new Mock<IFileNameSanitizer>();
            _mockReservedNameHandler = new Mock<IReservedNameHandler>();
            
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
            
            // Configure the file name sanitizer to return the input as-is for these tests
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => input);
            
            // Configure the reserved name handler to return the input as-is for these tests
            _mockReservedNameHandler.Setup(x => x.Handle(It.IsAny<string>())).Returns<string>(input => input);
            
            // Default setup for the path traversal validator - no exception by default
            _mockPathTraversalValidator.Setup(x => x.Validate(It.IsAny<string>())).Verifiable();
        }

        [Fact]
        public void ValidatePathTraversal_WithValidRelativePath_DoesNotThrow()
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, "valid_key");
            service.SaveData(testData, "key.with.dots");
            service.SaveData(testData, "key-with-special_chars");
        }

        [Theory]
        [InlineData("../../../etc/passwd")]
        [InlineData("..\\..\\windows\\system32")]
        [InlineData("..\\../mixed/traversal")]
        [InlineData("../secret")]
        [InlineData("normal/../../../path")]
        public void ValidatePathTraversal_WithPathTraversalPatterns_ThrowsArgumentException(string path)
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.SaveData(testData, path));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Theory]
        [InlineData("/absolute/path")]
        [InlineData("C:\\Windows\\System32")]
        public void ValidatePathTraversal_WithAbsolutePaths_ThrowsArgumentException(string path)
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.SaveData(testData, path));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void ValidatePathTraversal_WithUrlPath_ThrowsArgumentException()
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.SaveData(testData, "http://example.com/file"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Theory]
        [InlineData("..%2F..%2Fsecret")]
        [InlineData("..%5C..%5Csecret")]
        public void ValidatePathTraversal_WithEncodedPathTraversal_ThrowsArgumentException(string path)
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.SaveData(testData, path));
            Assert.Contains("Path cannot contain encoded path traversal patterns", exception.Message);
        }

        [Fact]
        public void ValidatePathTraversal_WithValidDotInFilename_DoesNotThrow()
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, "file.with.dots");
            service.SaveData(testData, "document.txt");
            service.SaveData(testData, "archive.tar.gz");
        }

        [Fact]
        public void ValidatePathTraversal_WithValidDotAtBeginning_DoesNotThrow()
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, ".hidden_file");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/.hidden_file.json", testData), Times.Once);
        }

        [Fact]
        public void ValidatePathTraversal_WithNullKey_ThrowsArgumentException()
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };
            string? nullKey = null; // Explicitly assign null to avoid CS8625 warning

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, nullKey!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidatePathTraversal_WithEmptyOrWhitespaceKey_ThrowsArgumentException(string key)
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, key));
        }

        [Theory]
        [InlineData("   ../../../etc/passwd")]
        [InlineData("../../secret   ")]
        [InlineData("   ../normal/path   ")]
        [InlineData("\t../../../etc/passwd\t")]
        [InlineData("\n../../secret\n")]
        public void ValidatePathTraversal_WithWhitespaceAndTraversalPatterns_ThrowsArgumentException(string path)
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.SaveData(testData, path));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Theory]
        [InlineData("   /absolute/path")]
        [InlineData("C:\\Windows\\System32   ")]
        [InlineData("   http://example.com/file   ")]
        public void ValidatePathTraversal_WithWhitespaceAndAbsolutePaths_ThrowsArgumentException(string path)
        {
            // Arrange - Use the real PathTraversalValidator implementation
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.SaveData(testData, path));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }
        [Fact]
        public void Validate_HiddenFiles_AreAllowed()
        {
            // Arrange
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert - Hidden files should be allowed
            service.SaveData(testData, ".env");
            service.SaveData(testData, ".gitignore");
            service.SaveData(testData, ".hidden_file");
            service.SaveData(testData, ".config.json");
            service.SaveData(testData, "normal/.hidden");
        }

        [Fact]
        public void Validate_ExplicitDotSegments_AreBlocked()
        {
            // Arrange
            var realValidator = new PathTraversalValidator();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, realValidator, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert - Explicit "." segments should be blocked
            var exception1 = Assert.Throws<ArgumentException>(() => service.SaveData(testData, "./file"));
            Assert.Contains("Path cannot contain relative path navigation", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => service.SaveData(testData, "file/./file2"));
            Assert.Contains("Path cannot contain relative path navigation", exception2.Message);
            
            var exception3 = Assert.Throws<ArgumentException>(() => service.SaveData(testData, "file/."));
            Assert.Contains("Path cannot contain relative path navigation", exception3.Message);
        }
    }
}