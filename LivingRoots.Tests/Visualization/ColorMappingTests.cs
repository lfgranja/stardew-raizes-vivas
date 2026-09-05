using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for the ColorMapping domain model.
    /// Verifies property storage, types, and default state.
    /// </summary>
    public class ColorMappingTests
    {
        // ──────────────────────────────────────────────
        // Category Property
        // ──────────────────────────────────────────────

        [Fact]
        public void Category_ShouldStoreValueCorrectly()
        {
            // Arrange
            var mapping = new ColorMapping { Category = HealthCategory.Healthy };

            // Act & Assert
            Assert.Equal(HealthCategory.Healthy, mapping.Category);
        }

        [Fact]
        public void Category_ShouldAcceptPoor()
        {
            // Arrange
            var mapping = new ColorMapping { Category = HealthCategory.Poor };

            // Act & Assert
            Assert.Equal(HealthCategory.Poor, mapping.Category);
        }

        [Fact]
        public void Category_ShouldAcceptModerate()
        {
            // Arrange
            var mapping = new ColorMapping { Category = HealthCategory.Moderate };

            // Act & Assert
            Assert.Equal(HealthCategory.Moderate, mapping.Category);
        }

        [Fact]
        public void Category_ShouldAcceptUnknown()
        {
            // Arrange
            var mapping = new ColorMapping { Category = HealthCategory.Unknown };

            // Act & Assert
            Assert.Equal(HealthCategory.Unknown, mapping.Category);
        }

        // ──────────────────────────────────────────────
        // BaseColor Property
        // ──────────────────────────────────────────────

        [Fact]
        public void BaseColor_ShouldBeColorType()
        {
            // Arrange
            var mapping = new ColorMapping();

            // Act & Assert
            Assert.IsType<Color>(mapping.BaseColor);
        }

        [Fact]
        public void BaseColor_ShouldStoreValueCorrectly()
        {
            // Arrange
            var expected = new Color(255, 0, 0, 255);
            var mapping = new ColorMapping { BaseColor = expected };

            // Act & Assert
            Assert.Equal(expected, mapping.BaseColor);
        }

        // ──────────────────────────────────────────────
        // InterpolatedColor Property
        // ──────────────────────────────────────────────

        [Fact]
        public void InterpolatedColor_ShouldBeColorType()
        {
            // Arrange
            var mapping = new ColorMapping();

            // Act & Assert
            Assert.IsType<Color>(mapping.InterpolatedColor);
        }

        [Fact]
        public void InterpolatedColor_ShouldStoreValueCorrectly()
        {
            // Arrange
            var expected = new Color(128, 128, 0, 200);
            var mapping = new ColorMapping { InterpolatedColor = expected };

            // Act & Assert
            Assert.Equal(expected, mapping.InterpolatedColor);
        }

        // ──────────────────────────────────────────────
        // HealthValue Property
        // ──────────────────────────────────────────────

        [Fact]
        public void HealthValue_ShouldStoreValueCorrectly()
        {
            // Arrange
            var mapping = new ColorMapping { HealthValue = 75.5f };

            // Act & Assert
            Assert.Equal(75.5f, mapping.HealthValue);
        }

        [Fact]
        public void HealthValue_ShouldAcceptZero()
        {
            // Arrange
            var mapping = new ColorMapping { HealthValue = 0f };

            // Act & Assert
            Assert.Equal(0f, mapping.HealthValue);
        }

        [Fact]
        public void HealthValue_ShouldAcceptHundred()
        {
            // Arrange
            var mapping = new ColorMapping { HealthValue = 100f };

            // Act & Assert
            Assert.Equal(100f, mapping.HealthValue);
        }

        // ──────────────────────────────────────────────
        // Default State
        // ──────────────────────────────────────────────

        [Fact]
        public void DefaultState_ShouldHaveDefaultCategory()
        {
            // Arrange & Act
            var mapping = new ColorMapping();

            // Assert
            Assert.Equal(HealthCategory.Poor, mapping.Category); // default enum value (0)
        }

        [Fact]
        public void DefaultState_ShouldHaveDefaultBaseColor()
        {
            // Arrange & Act
            var mapping = new ColorMapping();

            // Assert
            Assert.Equal(default(Color), mapping.BaseColor);
        }

        [Fact]
        public void DefaultState_ShouldHaveDefaultInterpolatedColor()
        {
            // Arrange & Act
            var mapping = new ColorMapping();

            // Assert
            Assert.Equal(default(Color), mapping.InterpolatedColor);
        }

        [Fact]
        public void DefaultState_ShouldHaveZeroHealthValue()
        {
            // Arrange & Act
            var mapping = new ColorMapping();

            // Assert
            Assert.Equal(0f, mapping.HealthValue);
        }
    }
}
