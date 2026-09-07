using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class TileOverlayTests
    {
        [Fact]
        public void Properties_CanBeSetAndGet()
        {
            // Arrange
            var overlay = new TileOverlay
            {
                TilePosition = new Point(10, 20),
                Color = Color.Red,
                PatternType = PatternType.Stripes,
                HealthValue = 25.0f,
                Category = HealthCategory.Poor
            };

            // Assert
            Assert.Equal(new Point(10, 20), overlay.TilePosition);
            Assert.Equal(Color.Red, overlay.Color);
            Assert.Equal(PatternType.Stripes, overlay.PatternType);
            Assert.Equal(25.0f, overlay.HealthValue);
            Assert.Equal(HealthCategory.Poor, overlay.Category);
        }

        [Fact]
        public void Constructor_SetsValues()
        {
            // Arrange & Act
            var overlay = new TileOverlay
            {
                TilePosition = new Point(5, 15),
                Color = Color.Green,
                PatternType = PatternType.Solid,
                HealthValue = 80.0f,
                Category = HealthCategory.Healthy
            };

            // Assert
            Assert.Equal(5, overlay.TilePosition.X);
            Assert.Equal(15, overlay.TilePosition.Y);
            Assert.Equal(Color.Green, overlay.Color);
            Assert.Equal(PatternType.Solid, overlay.PatternType);
            Assert.Equal(80.0f, overlay.HealthValue);
            Assert.Equal(HealthCategory.Healthy, overlay.Category);
        }

        [Fact]
        public void DefaultState_HasDefaultValues()
        {
            // Arrange
            var overlay = new TileOverlay();

            // Assert
            Assert.Equal(default(Point), overlay.TilePosition);
            Assert.Equal(default(Color), overlay.Color);
            Assert.Equal(PatternType.None, overlay.PatternType);
            Assert.Equal(0f, overlay.HealthValue);
            Assert.Equal(HealthCategory.Poor, overlay.Category); // enum default = 0 = Poor
        }
    }
}
