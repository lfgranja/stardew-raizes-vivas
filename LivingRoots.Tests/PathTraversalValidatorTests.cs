using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;

#nullable disable

namespace LivingRoots.Tests
{
    public class PathTraversalValidatorTests
    {
        [Fact]
        public void Validate_WithValidPath_DoesNotThrow()
        {
            // Arrange
            var mockPathValidationService = new Mock<IPathValidationService>();
            var validator = new PathTraversalValidator(mockPathValidationService.Object);

            // Act & Assert - should not throw
            validator.Validate("valid/path");
            validator.Validate("data/2024/../2023/file.json"); // This should now be allowed
            validator.Validate("folder/subfolder/file.txt");
        }

        [Fact]
        public void Validate_WithNullPath_ThrowsArgumentException()
        {
            // Arrange
            var mockPathValidationService = new Mock<IPathValidationService>();
            var validator = new PathTraversalValidator(mockPathValidationService.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => validator.Validate(null));
        }

        [Fact]
        public void Validate_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var mockPathValidationService = new Mock<IPathValidationService>();
            var validator = new PathTraversalValidator(mockPathValidationService.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => validator.Validate(""));
        }

        [Fact]
        public void Validate_WithWhitespacePath_ThrowsArgumentException()
        {
            // Arrange
            var mockPathValidationService = new Mock<IPathValidationService>();
            var validator = new PathTraversalValidator(mockPathValidationService.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => validator.Validate("   "));
        }
    }
}
