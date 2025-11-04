using StardewModdingAPI;
using Moq;
using LivingRoots.Services;
using Xunit;
using System;

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
        }

        [Fact]
        public void LoadData_WithFileNotFound_ReturnsNull()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);

            // Act
            service.RemoveData("test_key");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile<object>("data/test_key.json", null), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithInvalidChars_Sanitizes()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "   "));
        }

        [Fact]
        public void GetFilePath_WithPathTraversal_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "../../../etc/passwd"));
        }
        
        [Fact]
        public void SanitizeKey_WithEdgeCaseEmptyResults_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "<>:\"|?*"));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "........."));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "___"));
        }
    }
}
