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
        private readonly Mock<IMonitor> _mockMonitor;

        public PathTraversalValidatorTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
        }

        [Fact]
        public void ValidatePathTraversal_WithValidRelativePath_DoesNotThrow()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.SaveData(testData, path));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void ValidatePathTraversal_WithUrlPath_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.SaveData(testData, path));
            Assert.Contains("Path cannot contain encoded path traversal patterns", exception.Message);
        }

        [Fact]
        public void ValidatePathTraversal_WithValidDotInFilename_DoesNotThrow()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, "file.with.dots");
            service.SaveData(testData, "document.txt");
            service.SaveData(testData, "archive.tar.gz");
        }

        [Fact]
        public void ValidatePathTraversal_WithValidDotAtBeginning_DoesNotThrow()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, ".hidden_file");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/.hidden_file.json", testData), Times.Once);
        }

        [Fact]
        public void ValidatePathTraversal_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            string nullKey = null; // Explicitly assign null to avoid CS8625 warning

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, nullKey));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidatePathTraversal_WithEmptyOrWhitespaceKey_ThrowsArgumentException(string key)
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, key));
        }
    }
}