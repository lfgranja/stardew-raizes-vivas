using System;
using LivingRoots.Domain;
using Moq;
using StardewModdingAPI;

namespace LivingRoots.Tests
{
    public class PathTraversalValidatorTests
    {
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IPathTraversalValidator> _mockPathTraversalValidator;
        private readonly Mock<IFileNameSanitizer> _mockFileNameSanitizer;
        private readonly Mock<IMonitor> _mockMonitor;

        public PathTraversalValidatorTests()
        {
            _mockDataHelper = new Mock<IDataHelper>();
            _mockPathTraversalValidator = new Mock<IPathTraversalValidator>();
            _mockFileNameSanitizer = new Mock<IFileNameSanitizer>();
            _mockMonitor = new Mock<IMonitor>();
        }

        [Fact]
        public void Validate_WithValidPath_DoesNotThrow()
        {
            // Arrange
            _mockPathTraversalValidator.Setup(x => x.Validate(It.IsAny<string>())).Verifiable();

            // Act & Assert
            var validator = new PathTraversalValidator();
            validator.Validate("valid/path");
        }

        [Fact]
        public void Validate_WithPathTraversal_ThrowsArgumentException()
        {
            // Arrange
            _mockPathTraversalValidator.Setup(x => x.Validate(It.Is<string>(s => s.Contains(".."))).Throws<ArgumentException>();

            // Act & Assert
            var validator = new PathTraversalValidator();
            Assert.Throws<ArgumentException>(() => validator.Validate("path/../../../dangerous"));
        }
    }
}