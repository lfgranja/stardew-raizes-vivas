using StardewModdingAPI;
using Moq;
using LivingRoots.Services;
using LivingRoots.Domain;
using Xunit;
using System;
using Newtonsoft.Json;

namespace LivingRoots.Tests
{
    public class ModDataServiceTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IModLogic> _mockModLogic;
        private readonly Mock<IMonitor> _mockMonitor;

        public ModDataServiceTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockModLogic = new Mock<IModLogic>();
            
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
            
            // Configure the mod logic to return expected sanitized values for testing
            // and throw exceptions for cases that would result in empty strings
            _mockModLogic.Setup(x => x.SanitizeFileName(It.IsAny<string>())).Returns<string>(input => 
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
            
            // Configure the path validation to not throw for valid paths in most tests
            // but throw for path traversal attempts
            _mockModLogic.Setup(x => x.ValidatePath(It.Is<string>(s => s.Contains("../") || s.Contains("..\\") || s.Contains("../../../") || s.Contains("..\\..\\") || s.Contains("http://") || s.Contains("https://")))).Throws<ArgumentException>();
            _mockModLogic.Setup(x => x.ValidatePath(It.IsAny<string>())).Verifiable();
        }

        [Fact]
        public void SaveData_WithValidData_CallsWriteJsonFile()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => service.SaveData<object>(null!, "test_key"));
            Assert.Equal("data", exception.ParamName);
        }

        [Fact]
        public void LoadData_WithValidKey_CallsReadJsonFile()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);

            // Act
            service.RemoveData("test_key");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile<object>("data/test_key.json", null), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithInvalidChars_Sanitizes()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test/key\\with:invalid|chars");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key_with_invalid_chars.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithPathTraversal_ThrowsArgumentException()
        {
            // Arrange - Configure the validator to throw for path traversal
            _mockModLogic.Setup(x => x.ValidatePath(It.Is<string>(s => s.Contains("../../../")))).Throws<ArgumentException>();
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "../../../etc/passwd"));
        }
        
        [Fact]
        public void SanitizeKey_WithEdgeCaseEmptyResults_ThrowsArgumentException()
        {
            // Arrange - The mock is already configured to throw for these cases
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var unauthorizedAccessException = new System.UnauthorizedAccessException("Access denied");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(unauthorizedAccessException);

            // Act
            var result = service.LoadData<object>("test_key");

            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public void LoadData_WithIOException_ReturnsNull()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var ioException = new System.IO.IOException("Access denied or file locked");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(ioException);

            // Act
            var result = service.LoadData<object>("test_key");

            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public void DataExists_WithIOException_ReturnsFalse()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var ioException = new System.IO.IOException("Access denied or file locked");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(ioException);

            // Act
            var result = service.DataExists("test_key");

            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void LoadData_WithFileNotFoundException_LogsSanitizedKey()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var fileNotFoundException = new System.IO.FileNotFoundException("File not found");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(fileNotFoundException);

            // Act
            service.LoadData<object>("test_key");

            // Assert - Verify that the log message contains the sanitized key, not the raw key
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("test_key") && !msg.Contains("test_key.json")),
                LogLevel.Trace), Times.Once);
        }
        
        [Fact]
        public void LoadData_WithIOException_LogsSanitizedKey()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var ioException = new System.IO.IOException("IO error");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(ioException);

            // Act
            service.LoadData<object>("test_key");

            // Assert - Verify that the log message contains the sanitized key, not the raw key
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("test_key") && !msg.Contains("test_key.json")),
                LogLevel.Warn), Times.Once);
        }
        
        [Fact]
        public void LoadData_WithJsonException_LogsSanitizedKey()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var jsonException = new Newtonsoft.Json.JsonException("JSON error");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(jsonException);

            // Act
            service.LoadData<object>("test_key");

            // Assert - Verify that the log message contains the sanitized key, not the raw key
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("test_key") && !msg.Contains("test_key.json")),
                LogLevel.Error), Times.Once);
        }
        
        [Fact]
        public void DataExists_WithFileNotFoundException_LogsSanitizedKey()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var fileNotFoundException = new System.IO.FileNotFoundException("File not found");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(fileNotFoundException);

            // Act
            service.DataExists("test_key");

            // Assert - Verify that the log message contains the sanitized key, not the raw key
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("test_key") && !msg.Contains("test_key.json")),
                LogLevel.Trace), Times.Once);
        }
        
        [Fact]
        public void DataExists_WithIOException_LogsSanitizedKey()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var ioException = new System.IO.IOException("IO error");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(ioException);

            // Act
            service.DataExists("test_key");

            // Assert - Verify that the log message contains the sanitized key, not the raw key
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("test_key") && !msg.Contains("test_key.json")),
                LogLevel.Warn), Times.Once);
        }
        
        [Fact]
        public void DataExists_WithJsonException_LogsSanitizedKey()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var jsonException = new Newtonsoft.Json.JsonException("JSON error");
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(jsonException);

            // Act
            service.DataExists("test_key");

            // Assert - Verify that the log message contains the sanitized key, not the raw key
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("test_key") && !msg.Contains("test_key.json")),
                LogLevel.Warn), Times.Once);
        }
        
        [Fact]
        public void SaveData_WithIOException_LogsSanitizedPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var testData = new { Name = "Test", Value = 123 };
            var ioException = new System.IO.IOException("IO error");
            _mockDataHelper.Setup(x => x.WriteJsonFile("data/test_key.json", testData)).Throws(ioException);

            // Act & Assert
            Assert.Throws<System.IO.IOException>(() => service.SaveData(testData, "test_key"));

            // Assert - Verify that the log message contains the full sanitized path
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("data/test_key.json")),
                LogLevel.Warn), Times.Once);
        }
        
        [Fact]
        public void SaveData_WithJsonException_LogsSanitizedPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            var testData = new { Name = "Test", Value = 123 };
            var jsonException = new Newtonsoft.Json.JsonException("JSON error");
            _mockDataHelper.Setup(x => x.WriteJsonFile("data/test_key.json", testData)).Throws(jsonException);

            // Act & Assert
            Assert.Throws<Newtonsoft.Json.JsonException>(() => service.SaveData(testData, "test_key"));

            // Assert - Verify that the log message contains the full sanitized path
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("data/test_key.json")),
                LogLevel.Error), Times.Once);
        }
    }
}
