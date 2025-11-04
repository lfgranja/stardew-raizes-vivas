using System;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class ReservedNameHandlerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IMonitor> _mockMonitor;

        public ReservedNameHandlerTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
        }

        [Theory]
        [InlineData("CON", "CON_")]
        [InlineData("PRN", "PRN_")]
        [InlineData("AUX", "AUX_")]
        [InlineData("NUL", "NUL_")]
        [InlineData("COM1", "COM1_")]
        [InlineData("LPT1", "LPT1_")]
        [InlineData("con", "con_")]
        [InlineData("CoN", "CoN_")]
        public void GetFilePath_WithReservedWindowsName_AddsUnderscore(string reservedName, string expectedName)
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, reservedName);

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{expectedName}.json", testData), Times.Once);
        }

        [Theory]
        [InlineData("CON.txt", "CON_.txt")]
        [InlineData("PRN.log", "PRN_.log")]
        [InlineData("COM1.xml", "COM1_.xml")]
        public void GetFilePath_WithReservedNameAndExtension_HandlesCorrectly(string reservedName, string expectedName)
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, reservedName);

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{expectedName}.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithNonReservedName_DoesNotAddUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "normal_name");
            service.SaveData(testData, "normal.txt");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/normal_name.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/normal.txt.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithReservedNameWithComplexExtensions_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "CON.txt.bak");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.txt.bak.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithUnicodeHomoglyphOfReservedName_AddsUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "CОN"); // Cyrillic 'О'

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithPartialReservedName_DoesNotAddUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "CONSOLE");
            service.SaveData(testData, "ACOM123");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CONSOLE.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/ACOM123.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithReservedNameWithSpaces_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, " CON ");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithReservedNameAndDiacritics_AddsUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "CÓÑ");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.json", testData), Times.Once);
        }
    }
}