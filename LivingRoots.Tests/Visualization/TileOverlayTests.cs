using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for the TileOverlay domain model.
    /// Verifies constructor behavior, property storage, and default state.
    /// </summary>
    public class TileOverlayTests
    {
        // ──────────────────────────────────────────────
        // Constructor
        // ──────────────────────────────────────────────

        [Fact]
        public void Constructor_ShouldSetTilePosition()
        {
            // Arrange
            var position = new Point(10, 20);
            var color = new Color(255, 0, 0, 255);
            var pattern = PatternType.Solid;
            var health = 50f;
            var category = HealthCategory.Moderate;

            // Act
            var overlay = new TileOverlay(position, color, pattern, health, category);

            // Assert
            Assert.Equal(position, overlay.TilePosition);
        }

        [Fact]
        public void Constructor_ShouldSetColor()
        {
            // Arrange
            var position = new Point(5, 5);
            var color = new Color(0, 255, 0, 200);
            var pattern = PatternType.Dots;
            var health = 80f;
            var category = HealthCategory.Healthy;

            // Act
            var overlay = new TileOverlay(position, color, pattern, health, category);

            // Assert
            Assert.Equal(color, overlay.Color);
        }

        [Fact]
        public void Constructor_ShouldSetPatternType()
        {
            // Arrange
            var position = new Point(1, 1);
            var color = new Color(0, 0, 0, 255);
            var pattern = PatternType.Stripes;
            var health = 25f;
            var category = HealthCategory.Poor;

            // Act
            var overlay = new TileOverlay(position, color, pattern, health, category);

            // Assert
            Assert.Equal(pattern, overlay.PatternType);
        }

        [Fact]
        public void Constructor_ShouldSetHealthValue()
        {
            // Arrange
            var position = new Point(3, 7);
            var color = new Color(128, 128, 128, 255);
            var pattern = PatternType.None;
            var health = 33.3f;
            var category = HealthCategory.Unknown;

            // Act
            var overlay = new TileOverlay(position, color, pattern, health, category);

            // Assert
            Assert.Equal(33.3f, overlay.HealthValue);
        }

        [Fact]
        public void Constructor_ShouldSetCategory()
        {
            // Arrange
            var position = new Point(0, 0);
            var color = new Color(255, 255, 0, 255);
            var pattern = PatternType.Solid;
            var health = 66f;
            var category = HealthCategory.Moderate;

            // Act
            var overlay = new TileOverlay(position, color, pattern, health, category);

            // Assert
            Assert.Equal(category, overlay.Category);
        }

        [Fact]
        public void Constructor_ShouldSetAllValues()
        {
            // Arrange
            var position = new Point(15, 25);
            var color = new Color(100, 200, 50, 180);
            var pattern = PatternType.Dots;
            var health = 42.5f;
            var category = HealthCategory.Moderate;

            // Act
            var overlay = new TileOverlay(position, color, pattern, health, category);

            // Assert
            Assert.Equal(position, overlay.TilePosition);
            Assert.Equal(color, overlay.Color);
            Assert.Equal(pattern, overlay.PatternType);
            Assert.Equal(42.5f, overlay.HealthValue);
            Assert.Equal(category, overlay.Category);
        }

        // ──────────────────────────────────────────────
        // Property Access
        // ──────────────────────────────────────────────

        [Fact]
        public void TilePosition_ShouldBePointType()
        {
            // Arrange
            var overlay = new TileOverlay(
                new Point(0, 0),
                Color.White,
                PatternType.None,
                0f,
                HealthCategory.Unknown);

            // Act & Assert
            Assert.IsType<Point>(overlay.TilePosition);
        }

        [Fact]
        public void Color_ShouldBeColorType()
        {
            // Arrange
            var overlay = new TileOverlay(
                new Point(0, 0),
                Color.White,
                PatternType.None,
                0f,
                HealthCategory.Unknown);

            // Act & Assert
            Assert.IsType<Color>(overlay.Color);
        }

        [Fact]
        public void PatternType_ShouldBePatternTypeEnum()
        {
            // Arrange
            var overlay = new TileOverlay(
                new Point(0, 0),
                Color.White,
                PatternType.Solid,
                0f,
                HealthCategory.Unknown);

            // Act & Assert
            Assert.IsType<PatternType>(overlay.PatternType);
        }

        [Fact]
        public void HealthValue_ShouldBeFloatType()
        {
            // Arrange
            var overlay = new TileOverlay(
                new Point(0, 0),
                Color.White,
                PatternType.None,
                50f,
                HealthCategory.Unknown);

            // Act & Assert
            Assert.IsType<float>(overlay.HealthValue);
        }

        [Fact]
        public void Category_ShouldBeHealthCategoryEnum()
        {
            // Arrange
            var overlay = new TileOverlay(
                new Point(0, 0),
                Color.White,
                PatternType.None,
                0f,
                HealthCategory.Healthy);

            // Act & Assert
            Assert.IsType<HealthCategory>(overlay.Category);
        }

        // ──────────────────────────────────────────────
        // Default State (via property initialization)
        // ──────────────────────────────────────────────

        [Fact]
        public void DefaultState_ShouldHaveDefaultTilePosition()
        {
            // Arrange & Act
            var overlay = new TileOverlay(
                default(Point),
                default(Color),
                default(PatternType),
                default(float),
                default(HealthCategory));

            // Assert
            Assert.Equal(default(Point), overlay.TilePosition);
        }

        [Fact]
        public void DefaultState_ShouldHaveDefaultColor()
        {
            // Arrange & Act
            var overlay = new TileOverlay(
                default(Point),
                default(Color),
                default(PatternType),
                default(float),
                default(HealthCategory));

            // Assert
            Assert.Equal(default(Color), overlay.Color);
        }

        [Fact]
        public void DefaultState_ShouldHaveDefaultPatternType()
        {
            // Arrange & Act
            var overlay = new TileOverlay(
                default(Point),
                default(Color),
                default(PatternType),
                default(float),
                default(HealthCategory));

            // Assert
            Assert.Equal(default(PatternType), overlay.PatternType);
        }

        [Fact]
        public void DefaultState_ShouldHaveZeroHealthValue()
        {
            // Arrange & Act
            var overlay = new TileOverlay(
                default(Point),
                default(Color),
                default(PatternType),
                default(float),
                default(HealthCategory));

            // Assert
            Assert.Equal(0f, overlay.HealthValue);
        }

        [Fact]
        public void DefaultState_ShouldHaveDefaultCategory()
        {
            // Arrange & Act
            var overlay = new TileOverlay(
                default(Point),
                default(Color),
                default(PatternType),
                default(float),
                default(HealthCategory));

            // Assert
            Assert.Equal(HealthCategory.Poor, overlay.Category); // default enum value (0)
        }
    }
}
