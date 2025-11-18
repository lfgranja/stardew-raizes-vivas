using System;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests
{
    public class PathTraversalValidatorTests
    {
        [Fact]
        public void Validate_WithValidPath_DoesNotThrow()
        {
            // Arrange
            var validator = new PathTraversalValidator();

            // Act & Assert - should not throw
            validator.Validate("valid/path");
            validator.Validate("data/2024/../2023/file.json"); // This should now be allowed
            validator.Validate("folder/subfolder/file.txt");
        }

        [Fact]
        public void Validate_WithNullPath_ThrowsArgumentException()
        {
            // Arrange
            var validator = new PathTraversalValidator();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => validator.Validate(null));
        }

        [Fact]
        public void Validate_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var validator = new PathTraversalValidator();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => validator.Validate(""));
        }

        [Fact]
        public void Validate_WithWhitespacePath_ThrowsArgumentException()
        {
            // Arrange
            var validator = new PathTraversalValidator();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => validator.Validate("   "));
        }
    }
}