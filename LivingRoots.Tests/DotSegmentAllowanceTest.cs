using System;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;
using LivingRoots.Domain;

namespace LivingRoots.Tests
{
    public class DotSegmentAllowanceTest
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IMonitor> _mockMonitor;

        public DotSegmentAllowanceTest()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
        }

        [Fact]
        public void PathValidation_AllowsValidDotSegments()
        {
            // Arrange
            var realValidator = new PathValidationService();
            var modLogic = new ModLogic(new FileNameSanitizationService(new UnicodeNormalizationService()), realValidator);
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, modLogic);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert - These should all be allowed as they are valid paths with safe dot segments
            var ex1 = Record.Exception(() => service.SaveData(testData, "file/./file2"));
            Assert.Null(ex1);

            var ex2 = Record.Exception(() => service.SaveData(testData, "path/to/./file.txt"));
            Assert.Null(ex2);

            var ex3 = Record.Exception(() => service.SaveData(testData, "normal/.hidden"));
            Assert.Null(ex3);

            var ex4 = Record.Exception(() => service.SaveData(testData, "folder/./subfolder/file"));
            Assert.Null(ex4);

            var ex5 = Record.Exception(() => service.SaveData(testData, "file/."));
            Assert.Null(ex5);
        }

        [Fact]
        public void PathValidation_BlocksInvalidDotPaths()
        {
            // Arrange
            var realValidator = new PathValidationService();
            var modLogic = new ModLogic(new FileNameSanitizationService(new UnicodeNormalizationService()), realValidator);
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, modLogic);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert - These should be blocked as they represent directory navigation
            var exception1 = Assert.Throws<ArgumentException>(() => service.SaveData(testData, "."));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);

            var exception2 = Assert.Throws<ArgumentException>(() => service.SaveData(testData, "./file"));
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);

            var exception3 = Assert.Throws<ArgumentException>(() => service.SaveData(testData, "../file"));
            Assert.Contains("Path cannot contain path traversal patterns", exception3.Message);
        }
    }
}