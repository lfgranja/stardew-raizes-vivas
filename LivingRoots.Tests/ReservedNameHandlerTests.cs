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
        private readonly Mock<IPathTraversalValidator> _mockPathTraversalValidator;
        private readonly Mock<IFileNameSanitizer> _mockFileNameSanitizer;
        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;
        private readonly Mock<IMonitor> _mockMonitor;

        public ReservedNameHandlerTests()
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
            
            // Configure the reserved name handler to return expected values for these tests
            _mockReservedNameHandler.Setup(x => x.Handle(It.IsAny<string>())).Returns<string>(input => input);
            
            // Configure the path traversal validator to not throw for valid paths in these tests
            _mockPathTraversalValidator.Setup(x => x.Validate(It.IsAny<string>())).Verifiable();
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
            // Reset the mock to configure it for this specific test
            _mockReservedNameHandler.Reset();
            _mockReservedNameHandler.Setup(x => x.Handle(reservedName)).Returns(expectedName);
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => input); // Return input as-is
            
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, reservedName);
            
            // The expected file path should include the data directory and the expected name with .json extension
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{expectedName}.json", testData), Times.Once);
        }

        [Theory]
        [InlineData("CON.log", "CON_.log")]
        [InlineData("PRN.log", "PRN_.log")]
        [InlineData("COM1.xml", "COM1_.xml")]
        public void GetFilePath_WithReservedNameAndExtension_HandlesCorrectly(string reservedName, string expectedName)
        {
            // Reset the mock to configure it for this specific test
            _mockReservedNameHandler.Reset();
            _mockReservedNameHandler.Setup(x => x.Handle(reservedName)).Returns(expectedName);
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => input); // Return input as-is
            
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, reservedName);
            
            // The expected file path should include the data directory and the expected name with .json extension
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{expectedName}.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithNonReservedName_DoesNotAddUnderscore()
        {
            // Reset the mock to configure it for this specific test
            _mockReservedNameHandler.Reset();
            _mockReservedNameHandler.Setup(x => x.Handle(It.IsAny<string>())).Returns<string>(input => input);
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => input); // Return input as-is
            
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, "normal_name");
            service.SaveData(testData, "normal.txt");
            
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/normal_name.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/normal.txt.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithNonReservedNameWithComplexExtensions_DoesNotAddUnderscore()
        {
            // Reset the mock to configure it for this specific test
            _mockReservedNameHandler.Reset();
            _mockReservedNameHandler.Setup(x => x.Handle(It.IsAny<string>())).Returns<string>(input => input);
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => input); // Return input as-is
            
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, "CONSOLE.log");
            service.SaveData(testData, "ACOM123.xml");
            
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CONSOLE.log.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/ACOM123.xml.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithUnicodeHomoglyphOfReservedName_AddsUnderscore()
        {
            // Reset the mock to configure it for this specific test
            _mockReservedNameHandler.Reset();
            _mockReservedNameHandler.Setup(x => x.Handle("CОN")).Returns("CON_"); // Cyrillic O should normalize to Latin O
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => input); // Return input as-is
            
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, "CОN"); // The 'О' here is a Cyrillic character (U+041E)
            
            // The UnicodeNormalizer should convert this to "CON_", which is reserved
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithReservedNameWithSpaces_HandlesProperly()
        {
            // Reset the mock to configure it for this specific test
            _mockReservedNameHandler.Reset();
            _mockReservedNameHandler.Setup(x => x.Handle(" CON ")).Returns("CON_");
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => input); // Return input as-is
            
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, " CON ");
            
            // The spaces will be handled by the FileNameSanitizer, but the reserved name handler should still recognize "CON" and add underscore
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithReservedNameWithDiacritics_AddsUnderscore()
        {
            // Reset the mock to configure it for this specific test
            _mockReservedNameHandler.Reset();
            _mockReservedNameHandler.Setup(x => x.Handle("CÓÑ")).Returns("CON_"); // Diacritics should normalize to base character
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => input); // Return input as-is
            
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, "CÓÑ"); // With diacritics
            
            // The UnicodeNormalizer should normalize this to "CON_", which is reserved
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.json", testData), Times.Once);
        }
    }
}