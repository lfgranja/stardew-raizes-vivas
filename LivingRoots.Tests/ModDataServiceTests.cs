using StardewModdingAPI;
using Moq;
using LivingRoots.Services;
using Xunit;
using System;
using Newtonsoft.Json;

namespace LivingRoots.Tests
{
    public class ModDataServiceTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IPathTraversalValidator> _mockPathTraversalValidator;
        private readonly Mock<IFileNameSanitizer> _mockFileNameSanitizer;
        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;
        private readonly Mock<IMonitor> _mockMonitor;

        public ModDataServiceTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockPathTraversalValidator = new Mock<IPathTraversalValidator>();
            _mockFileNameSanitizer = new Mock<IFileNameSanitizer>();
            _mockReservedNameHandler = new Mock<IReservedNameHandler>();
            
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
            
            // Configure the file name sanitizer to return expected sanitized values for testing
            // and throw exceptions for cases that would result in empty strings
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => 
            {
                // Simulate the real sanitization behavior
                if (input.Contains("test/key\\with:invalid|chars"))
                    return "test_key_with_invalid_chars";
                if (input.Contains("file....name"))
                    return "file.name";
                if (input.Contains("  test_key  "))
                    return "test_key";
                if (input.Contains("<>:\"|?*"))
                    throw new ArgumentException("Filename sanitizes to an empty string.", nameof(input)); // This should throw like the real implementation
                if (input.Contains("........."))
                    throw new ArgumentException("Filename sanitizes to an empty string.", nameof(input)); // This should throw like the real implementation
                if (input.Contains("___"))
                    throw new ArgumentException("Filename sanitizes to an empty string.", nameof(input)); // This should throw like the real implementation
                if (input.Contains("   "))
                    throw new ArgumentException("Filename sanitizes to an empty string.", nameof(input)); // This should throw like the real implementation
                return input;
            });
            
            // Configure the reserved name handler to return expected values for testing
            _mockReservedNameHandler.Setup(x => x.Handle(It.IsAny<string>())).Returns<string>(input => 
            {
                // Simulate the real reserved name handling behavior
                if (input.Equals("CON", StringComparison.OrdinalIgnoreCase) && 
                    !input.Contains(".") && !input.Contains("_"))
                    return "CON_";
                if (input.Equals("CON.txt", StringComparison.OrdinalIgnoreCase))
                    return "CON_.txt";
                return input;
            });
            
            // Configure the path traversal validator to not throw for valid paths in most tests
            // but throw for path traversal attempts
            _mockPathTraversalValidator.Setup(x => x.Validate(It.Is<string>(s => s.Contains("../") || s.Contains("..\\") || s.Contains("../../../") || s.Contains("..\\..\\") || s.Contains("http://") || s.Contains("https://")))).Throws<ArgumentException>();
            _mockPathTraversalValidator.Setup(x => x.Validate(It.IsAny<string>())).Verifiable();
        }

        [Fact]
        public void SaveData_WithValidData_CallsWriteJsonFile()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test_key");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key.json", testData), Times.Once);
        }

        [Fact]
        public void SaveData_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, null!));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, ""));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "   "));
        }

        [Fact]
        public void SaveData_WithNullData_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => service.SaveData<object>(null!, "test_key"));
            Assert.Equal("data", exception.ParamName);
        }

        [Fact]
        public void LoadData_WithValidKey_CallsReadJsonFile()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var expectedData = new { Name = "Test", Value = 123 };
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns(expectedData);

            // Act
            var result = service.LoadData<object>("test_key");

            // Assert
            Assert.Equal(expectedData, result);
        }

        [Fact]
        public void LoadData_WithFileNotFound_ReturnsNull()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>(It.IsAny<string>())).Returns(default(object));

            // Act
            var result = service.LoadData<object>("non_existent_key");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void DataExists_WithExistingData_ReturnsTrue()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns(new object());

            // Act
            var result = service.DataExists("test_key");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DataExists_WithNonExistingData_ReturnsFalse()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>(It.IsAny<string>())).Returns(default(object));

            // Act
            var result = service.DataExists("non_existent_key");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RemoveData_WithExistingData_RemovesData()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);

            // Act
            service.RemoveData("test_key");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile<object>("data/test_key.json", null), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithInvalidChars_Sanitizes()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test/key\\with:invalid|chars");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key_with_invalid_chars.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithReservedName_AddsUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "CON");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithReservedNameAndExtension_HandlesCorrectly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "CON.txt");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.txt.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithMultipleDots_Sanitizes()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file....name");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file.name.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithLeadingAndTrailingWhitespace_Trims()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "  test_key  ");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithOnlyWhitespace_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "   "));
        }

        [Fact]
        public void GetFilePath_WithPathTraversal_ThrowsArgumentException()
        {
            // Arrange - Configure the validator to throw for path traversal
            _mockPathTraversalValidator.Setup(x => x.Validate(It.Is<string>(s => s.Contains("../../../")))).Throws<ArgumentException>();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "../../../etc/passwd"));
        }
        
        [Fact]
        public void SanitizeKey_WithEdgeCaseEmptyResults_ThrowsArgumentException()
        {
            // Arrange - The mock is already configured to throw for these cases
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "<>:\"|?*"));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "........."));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "___"));
        }
        
        [Fact]
        public void LoadData_WithJsonParsingError_ReturnsNull()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var jsonException = new Newtonsoft.Json.JsonException("Invalid JSON format");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(jsonException);

            // Act
            var result = service.LoadData<object>("test_key");

            // Assert
            Assert.Null(result);
        }
        [Fact]
        public void LoadData_WithDirectoryNotFoundException_ReturnsNull()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var directoryNotFoundException = new System.IO.DirectoryNotFoundException("Directory does not exist");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(directoryNotFoundException);

            // Act
            var result = service.LoadData<object>("test_key");

            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public void LoadData_WithUnauthorizedAccessException_ReturnsNull()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var unauthorizedAccessException = new System.UnauthorizedAccessException("Access denied");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(unauthorizedAccessException);

            // Act
            var result = service.LoadData<object>("test_key");

            // Assert
            Assert.Null(result);
        }
    }
}
