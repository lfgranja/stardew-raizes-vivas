using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class ColorMappingTests
    {
        [Fact]
        public void CategoryProperty_CanBeSetAndGet()
        {
            // Arrange
            var mapping = new ColorMapping { Category = HealthCategory.Moderate };

            // Assert
            Assert.Equal(HealthCategory.Moderate, mapping.Category);
        }

        [Fact]
        public void BaseColor_IsColorType()
        {
            // Arrange
            var mapping = new ColorMapping { BaseColor = Color.Red };

            // Assert
            Assert.Equal(Color.Red, mapping.BaseColor);
        }

        [Fact]
        public void InterpolatedColor_IsColorType()
        {
            // Arrange
            var mapping = new ColorMapping { InterpolatedColor = Color.Yellow };

            // Assert
            Assert.Equal(Color.Yellow, mapping.InterpolatedColor);
        }

        [Fact]
        public void HealthValue_StoredCorrectly()
        {
            // Arrange
            var mapping = new ColorMapping { HealthValue = 42.5f };

            // Assert
            Assert.Equal(42.5f, mapping.HealthValue);
        }

        [Fact]
        public void DefaultState_HasDefaultValues()
        {
            // Arrange
            var mapping = new ColorMapping();

            // Assert
            Assert.Equal(HealthCategory.Unknown, mapping.Category);
            Assert.Equal(default(Color), mapping.BaseColor);
            Assert.Equal(default(Color), mapping.InterpolatedColor);
            Assert.Equal(0f, mapping.HealthValue);
        }
    }
}
