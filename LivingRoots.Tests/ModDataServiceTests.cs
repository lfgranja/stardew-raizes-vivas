using StardewModdingAPI;
using Moq;
using LivingRoots.Services;
using Xunit;
using System.IO;


namespace LivingRoots.Tests
{
    public class ModDataServiceTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IMonitor> _mockMonitor;

        public ModDataServiceTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
        }
        
        
        [Fact]
        public void SaveData_WithValidData_CallsWriteJsonFile()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => service.SaveData<object>(null!, "test_key"));
            Assert.Equal("data", exception.ParamName);
        }

        [Fact]
        public void LoadData_WithValidKey_CallsReadJsonFile()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var expectedData = new { Name = "Test", Value = 123 };
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns(expectedData);
            
            // Act
            var result = service.LoadData<object>("test_key");
            
            // Assert
            Assert.Equal(expectedData, result);
            _mockDataHelper.Verify(x => x.ReadJsonFile<object>("data/test_key.json"), Times.Once);
        }

        [Fact]
        public void LoadData_WithFileNotFoundException_ReturnsNull()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(new System.IO.FileNotFoundException());
            
            // Act
            var result = service.LoadData<object>("test_key");
            
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void LoadData_WithJsonException_ThrowsException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(new Newtonsoft.Json.JsonException());
            
            // Act & Assert
            Assert.Throws<Newtonsoft.Json.JsonException>(() => service.LoadData<object>("test_key"));
        }

        [Fact]
        public void LoadData_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.LoadData<object>(null!));
            Assert.Throws<ArgumentException>(() => service.LoadData<object>(""));
            Assert.Throws<ArgumentException>(() => service.LoadData<object>("   "));
        }

        [Fact]
        public void DataExists_WithExistingData_ReturnsTrue()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns(new object());
            
            // Act
            var result = service.DataExists("test_key");
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DataExists_WithFileNotFoundException_ReturnsFalse()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(new System.IO.FileNotFoundException());
            
            // Act
            var result = service.DataExists("test_key");
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DataExists_WithJsonException_ThrowsException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(new Newtonsoft.Json.JsonException());
            
            // Act & Assert
            Assert.Throws<Newtonsoft.Json.JsonException>(() => service.DataExists("test_key"));
        }

        [Fact]
        public void RemoveData_WithExistingData_RemovesData()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act
            service.RemoveData("test_key");
            
            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile<object>("data/test_key.json", null), Times.Once);
        }

        [Fact]
        public void RemoveData_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.RemoveData(null!));
            Assert.Throws<ArgumentException>(() => service.RemoveData(""));
            Assert.Throws<ArgumentException>(() => service.RemoveData("   "));
        }
        
        [Fact]
        public void GetFilePath_WithInvalidCharacters_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act
            service.SaveData(testData, "test/key\\with:invalid|chars");

            // Assert - the invalid characters should be replaced with underscores
            // The exact expected path should have all invalid characters replaced
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key_with_invalid_chars.json", testData), Times.Once);
        }

        [Fact]
        public void DataExists_WithDirectoryPathAndExistingFile_ReturnsTrue()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockDataHelper = new Mock<IDataHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var testDirectory = Path.Combine(Path.GetTempPath(), "LivingRootsTest");
            
            // Setup the mock to return a directory path and mock the ReadJsonFile to return an object
            mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
            mockHelper.Setup(x => x.DirectoryPath).Returns(testDirectory);
            mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns(new object());
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object);
            
            // Act
            var result = service.DataExists("test_key");
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DataExists_WithDirectoryPathAndNonExistingFile_ReturnsFalse()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockDataHelper = new Mock<IDataHelper>();
            var mockMonitor = new Mock<IMonitor>();
            
            // Setup the mock to return a directory path and mock the ReadJsonFile to throw FileNotFoundException
            mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
            mockHelper.Setup(x => x.DirectoryPath).Returns(Path.GetTempPath());
            mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/non_existing_key.json")).Throws(new System.IO.FileNotFoundException());
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object);
            
            // Act
            var result = service.DataExists("non_existing_key");
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DataExists_WithDirectoryPathAndZeroLengthFile_ThrowsException()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockDataHelper = new Mock<IDataHelper>();
            var mockMonitor = new Mock<IMonitor>();
            
            // Setup the mock to return a directory path and mock the ReadJsonFile to throw JsonException for empty content
            mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
            mockHelper.Setup(x => x.DirectoryPath).Returns(Path.GetTempPath());
            mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/empty_key.json")).Throws(new Newtonsoft.Json.JsonException());
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object);
            
            // Act & Assert - Should still throw JsonException as per requirements
            Assert.Throws<Newtonsoft.Json.JsonException>(() => service.DataExists("empty_key"));
        }

        [Fact]
        public void RemoveData_WithDirectoryPath_RemovesDataThroughSMAPI()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockDataHelper = new Mock<IDataHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var testDirectory = Path.Combine(Path.GetTempPath(), "LivingRootsTest");
            
            // Setup the mock to return a directory path
            mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
            mockHelper.Setup(x => x.DirectoryPath).Returns(testDirectory);
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object);
            
            // Act
            service.RemoveData("remove_test");
            
            // Assert - should call WriteJsonFile with null through SMAPI API
            mockDataHelper.Verify(x => x.WriteJsonFile<object>("data/remove_test.json", null), Times.Once);
            
            // The testDirectory is not actually created in this test, so cleanup is unnecessary
        }
        
        [Fact]
        public void GetFilePath_WithAbsolutePath_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act & Assert - Test with platform-appropriate absolute path
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "/absolute/path")); // Unix-style absolute path
        }
        
        [Fact]
        public void LoadData_WithAbsolutePath_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert - Test with platform-appropriate absolute path
            Assert.Throws<ArgumentException>(() => service.LoadData<object>("/absolute/path")); // Unix-style absolute path
        }
        
        [Fact]
        public void DataExists_WithAbsolutePath_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert - Test with platform-appropriate absolute path
            Assert.Throws<ArgumentException>(() => service.DataExists("/absolute/path")); // Unix-style absolute path
        }
        
        [Fact]
        public void RemoveData_WithAbsolutePath_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert - Test with platform-appropriate absolute path
            Assert.Throws<ArgumentException>(() => service.RemoveData("/absolute/path")); // Unix-style absolute path
        }
    }
}