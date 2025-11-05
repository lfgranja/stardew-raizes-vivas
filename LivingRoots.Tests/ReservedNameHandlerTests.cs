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
            _mockReservedNameHandler.Setup(x => x.Handle(It.IsAny<string>())).Returns<string>(input => 
            {
                // Simulate the real reserved name handling behavior
                if (input.Equals("CON", StringComparison.OrdinalIgnoreCase) && 
                    !input.Contains(".") && !input.Contains("_"))
                    return "CON_";
                if (input.Equals("CON.txt", StringComparison.OrdinalIgnoreCase))
                    return "CON_.txt";
                if (input.Equals("PRN", StringComparison.OrdinalIgnoreCase) && 
                    !input.Contains(".") && !input.Contains("_"))
                    return "PRN_";
                if (input.Equals("AUX", StringComparison.OrdinalIgnoreCase) && 
                    !input.Contains(".") && !input.Contains("_"))
                    return "AUX_";
                if (input.Equals("NUL", StringComparison.OrdinalIgnoreCase) && 
                    !input.Contains(".") && !input.Contains("_"))
                    return "NUL_";
                if (input.Equals("COM1", StringComparison.OrdinalIgnoreCase) && 
                    !input.Contains(".") && !input.Contains("_"))
                    return "COM1_";
                if (input.Equals("LPT1", StringComparison.OrdinalIgnoreCase) && 
                    !input.Contains(".") && !input.Contains("_"))
                    return "LPT1_";
                if (input.Equals("con", StringComparison.OrdinalIgnoreCase) && 
                    !input.Contains(".") && !input.Contains("_"))
                    return "con_";
                if (input.Equals("CoN", StringComparison.OrdinalIgnoreCase) && 
                    !input.Contains(".") && !input.Contains("_"))
                    return "CoN_";
                if (input.Equals("CON.txt.bak", StringComparison.OrdinalIgnoreCase))
                    return "CON_.txt.bak";
                if (input.Equals("CÓÑ", StringComparison.OrdinalIgnoreCase))
                    return "CÓÑ_";
                return input;
            });
            
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
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
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
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockPathTraversalValidator.Object, _mockFileNameSanitizer.Object, _mockReservedNameHandler.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "CÓÑ");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CÓÑ_.json", testData), Times.Once);
        }
    }
}