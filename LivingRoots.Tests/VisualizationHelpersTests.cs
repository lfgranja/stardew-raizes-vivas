using LivingRoots.Services;
using Microsoft.Xna.Framework;
using StardewValley;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Unit tests for VisualizationHelpers utility class.
    /// Tests helper methods for health level text, tile visibility, and viewport operations.
    /// </summary>
    public class VisualizationHelpersTests
    {
        [Theory]
        [InlineData(0f)]
        [InlineData(10f)]
        [InlineData(20f)]
        [InlineData(33f)]
        public void GetHealthLevelText_PoorHealthRange_ReturnsPoor(float health)
        {
            // Act
            string result = VisualizationHelpers.GetHealthLevelText(health);

            // Assert
            Assert.Equal("Poor", result);
        }

        [Theory]
        [InlineData(34f)]
        [InlineData(45f)]
        [InlineData(50f)]
        [InlineData(60f)]
        public void GetHealthLevelText_ModerateHealthRange_ReturnsModerate(float health)
        {
            // Act
            string result = VisualizationHelpers.GetHealthLevelText(health);

            // Assert
            Assert.Equal("Moderate", result);
        }

        [Theory]
        [InlineData(67f)]
        [InlineData(75f)]
        [InlineData(85f)]
        [InlineData(95f)]
        [InlineData(100f)]
        public void GetHealthLevelText_HealthyHealthRange_ReturnsHealthy(float health)
        {
            // Act
            string result = VisualizationHelpers.GetHealthLevelText(health);

            // Assert
            Assert.Equal("Healthy", result);
        }

        [Fact]
        public void GetHealthLevelText_NegativeHealth_ClampsToPoor()
        {
            // Act
            string result = VisualizationHelpers.GetHealthLevelText(-10f);

            // Assert
            Assert.Equal("Poor", result);
        }

        [Fact]
        public void GetHealthLevelText_HealthAbove100_ClampsToHealthy()
        {
            // Act
            string result = VisualizationHelpers.GetHealthLevelText(150f);

            // Assert
            Assert.Equal("Healthy", result);
        }

        [Fact]
        public void GetHealthLevelText_BoundaryValues_ReturnsCorrectCategory()
        {
            // Act & Assert
            Assert.Equal("Poor", VisualizationHelpers.GetHealthLevelText(0f));
            Assert.Equal("Poor", VisualizationHelpers.GetHealthLevelText(33f));
            Assert.Equal("Moderate", VisualizationHelpers.GetHealthLevelText(34f));
            Assert.Equal("Moderate", VisualizationHelpers.GetHealthLevelText(66f));
            Assert.Equal("Healthy", VisualizationHelpers.GetHealthLevelText(67f));
            Assert.Equal("Healthy", VisualizationHelpers.GetHealthLevelText(100f));
        }

        [Fact]
        public void IsTileVisible_WithViewport_ReturnsCorrectVisibility()
        {
            // Arrange
            var viewport = new Microsoft.Xna.Framework.Rectangle(0, 0, 640, 480);
            var visibleTile = new Vector2(5, 5);
            var invisibleTile = new Vector2(20, 20);

            // Act
            bool isVisible = VisualizationHelpers.IsTileVisible(visibleTile, viewport);
            bool isInvisible = VisualizationHelpers.IsTileVisible(invisibleTile, viewport);

            // Assert
            Assert.True(isVisible, "Tile within viewport should be visible");
            Assert.False(isInvisible, "Tile outside viewport should not be visible");
        }

        [Fact]
        public void IsTileVisible_TileAtViewportEdge_ReturnsTrue()
        {
            // Arrange
            var viewport = new Microsoft.Xna.Framework.Rectangle(0, 0, 640, 480);
            var edgeTile = new Vector2(0, 0);

            // Act
            bool isVisible = VisualizationHelpers.IsTileVisible(edgeTile, viewport);

            // Assert
            Assert.True(isVisible, "Tile at viewport edge should be visible");
        }

        [Fact]
        public void IsTileVisible_TilePartiallyOverlapping_ReturnsTrue()
        {
            // Arrange
            var viewport = new Microsoft.Xna.Framework.Rectangle(0, 0, 640, 480);
            var partialTile = new Vector2(9, 7); // Partially overlapping at edge

            // Act
            bool isVisible = VisualizationHelpers.IsTileVisible(partialTile, viewport);

            // Assert
            Assert.True(isVisible, "Partially overlapping tile should be visible");
        }

        [Fact]
        public void GetViewportBounds_ReturnsValidRectangle()
        {
            // Arrange - Use reflection to set Game1.viewport to avoid requiring game runtime state
            var viewportField = typeof(Game1).GetField(
                "viewport",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
            );
            var uiViewportField = typeof(Game1).GetField(
                "uiViewport",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
            );

            var originalViewport = viewportField?.GetValue(null);
            var originalUiViewport = uiViewportField?.GetValue(null);

            try
            {
                // Set up test viewport - Game1.viewport is xTile.Dimensions.Rectangle
                var testViewport = new xTile.Dimensions.Rectangle(0, 0, 800, 600);
                viewportField?.SetValue(null, testViewport);
                uiViewportField?.SetValue(null, testViewport);

                // Act
                var bounds = VisualizationHelpers.GetViewportBounds();

                // Assert
                Assert.Equal(800, bounds.Width);
                Assert.Equal(600, bounds.Height);
            }
            finally
            {
                // Restore original values
                viewportField?.SetValue(null, originalViewport);
                uiViewportField?.SetValue(null, originalUiViewport);
            }
        }

        [Fact]
        public void GetTileScreenPosition_ReturnsCorrectPosition()
        {
            // Arrange
            var tile = new Vector2(5, 10);

            // Act
            var position = VisualizationHelpers.GetTileScreenPosition(tile);

            // Assert
            Assert.Equal(320, position.X); // 5 * 64
            Assert.Equal(640, position.Y); // 10 * 64
        }

        [Fact]
        public void GetTileScreenPosition_WithNegativeCoordinates_ReturnsCorrectPosition()
        {
            // Arrange
            var tile = new Vector2(-3, -2);

            // Act
            var position = VisualizationHelpers.GetTileScreenPosition(tile);

            // Assert
            Assert.Equal(-192, position.X); // -3 * 64
            Assert.Equal(-128, position.Y); // -2 * 64
        }

        [Fact]
        public void GetTileScreenPosition_WithZeroCoordinates_ReturnsZero()
        {
            // Arrange
            var tile = new Vector2(0, 0);

            // Act
            var position = VisualizationHelpers.GetTileScreenPosition(tile);

            // Assert
            Assert.Equal(0, position.X);
            Assert.Equal(0, position.Y);
        }

        [Fact]
        public void IsValidTile_ValidCoordinates_ReturnsTrue()
        {
            // Arrange
            var validTile = new Vector2(10, 20);

            // Act
            bool isValid = VisualizationHelpers.IsValidTile(validTile);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidTile_WithNaN_ReturnsFalse()
        {
            // Arrange
            var nanTile = new Vector2(float.NaN, 10);

            // Act
            bool isValid = VisualizationHelpers.IsValidTile(nanTile);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidTile_WithInfinity_ReturnsFalse()
        {
            // Arrange
            var infinityTile = new Vector2(float.PositiveInfinity, 10);

            // Act
            bool isValid = VisualizationHelpers.IsValidTile(infinityTile);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidTile_WithNegativeInfinity_ReturnsFalse()
        {
            // Arrange
            var negInfinityTile = new Vector2(10, float.NegativeInfinity);

            // Act
            bool isValid = VisualizationHelpers.IsValidTile(negInfinityTile);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidTile_WithExtremeValues_ReturnsFalse()
        {
            // Arrange
            var extremeTile = new Vector2(20000, 20000);

            // Act
            bool isValid = VisualizationHelpers.IsValidTile(extremeTile);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidTile_WithBoundaryValues_ReturnsTrue()
        {
            // Arrange
            var boundaryTile = new Vector2(10000, 10000);

            // Act
            bool isValid = VisualizationHelpers.IsValidTile(boundaryTile);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ClampHealth_ValidRange_ReturnsSameValue()
        {
            // Arrange
            float health = 50f;

            // Act
            float result = VisualizationHelpers.ClampHealth(health);

            // Assert
            Assert.Equal(50f, result);
        }

        [Fact]
        public void ClampHealth_NegativeValue_ClampsToZero()
        {
            // Arrange
            float health = -10f;

            // Act
            float result = VisualizationHelpers.ClampHealth(health);

            // Assert
            Assert.Equal(0f, result); // -10 is clamped to 0 (MinSoilHealth)
        }

        [Fact]
        public void ClampHealth_ValueAbove100_ClampsTo100()
        {
            // Arrange
            float health = 150f;

            // Act
            float result = VisualizationHelpers.ClampHealth(health);

            // Assert
            Assert.Equal(100f, result);
        }

        [Fact]
        public void ClampHealth_FloatingPointValues_ReturnsClampedValues()
        {
            // Arrange
            float health = 99.9f;

            // Act
            float result = VisualizationHelpers.ClampHealth(health);

            // Assert
            Assert.Equal(99.9f, result);
        }

        [Fact]
        public void ClampHealth_BoundaryValues_ReturnsCorrectValues()
        {
            // Act & Assert
            Assert.Equal(0f, VisualizationHelpers.ClampHealth(0f)); // 0 is already at the minimum boundary
            Assert.Equal(100f, VisualizationHelpers.ClampHealth(100f)); // 100 is at the maximum boundary
        }

        [Fact]
        public void GetTileFromScreenPosition_ReturnsCorrectTile()
        {
            // Arrange
            var screenPosition = new Vector2(320, 640);

            // Act
            var tile = VisualizationHelpers.GetTileFromScreenPosition(screenPosition);

            // Assert
            Assert.Equal(5, tile.X); // 320 / 64 = 5
            Assert.Equal(10, tile.Y); // 640 / 64 = 10
        }

        [Fact]
        public void GetTileFromScreenPosition_WithPartialPixels_ReturnsFlooredTile()
        {
            // Arrange
            var screenPosition = new Vector2(350, 680);

            // Act
            var tile = VisualizationHelpers.GetTileFromScreenPosition(screenPosition);

            // Assert
            Assert.Equal(5, tile.X); // floor(350 / 64) = 5
            Assert.Equal(10, tile.Y); // floor(680 / 64) = 10
        }

        [Fact]
        public void GetTileFromScreenPosition_WithNegativeCoordinates_ReturnsCorrectTile()
        {
            // Arrange
            var screenPosition = new Vector2(-100, -200);

            // Act
            var tile = VisualizationHelpers.GetTileFromScreenPosition(screenPosition);

            // Assert
            Assert.Equal(-2, tile.X); // floor(-100 / 64) = -2
            Assert.Equal(-4, tile.Y); // floor(-200 / 64) = -4
        }

        [Fact]
        public void ApplyOpacity_ValidOpacity_ReturnsColorWithOpacity()
        {
            // Arrange
            var color = new Color(255, 128, 64, 255);
            float opacity = 0.5f;

            // Act
            var result = VisualizationHelpers.ApplyOpacity(color, opacity);

            // Assert
            Assert.Equal(255, result.R);
            Assert.Equal(128, result.G);
            Assert.Equal(64, result.B);
            Assert.Equal(127, result.A); // 255 * 0.5 = 127.5, truncated to 127
        }

        [Fact]
        public void ApplyOpacity_OpacityZero_ReturnsTransparent()
        {
            // Arrange
            var color = new Color(255, 128, 64, 255);
            float opacity = 0f;

            // Act
            var result = VisualizationHelpers.ApplyOpacity(color, opacity);

            // Assert - Only alpha channel changes, RGB stays the same
            Assert.Equal(255, result.R);
            Assert.Equal(128, result.G);
            Assert.Equal(64, result.B);
            Assert.Equal(0, result.A);
        }

        [Fact]
        public void ApplyOpacity_OpacityOne_ReturnsFullOpacity()
        {
            // Arrange
            var color = new Color(255, 128, 64, 255);
            float opacity = 1f;

            // Act
            var result = VisualizationHelpers.ApplyOpacity(color, opacity);

            // Assert - Only alpha channel changes, RGB stays the same
            Assert.Equal(255, result.R);
            Assert.Equal(128, result.G);
            Assert.Equal(64, result.B);
            Assert.Equal(255, result.A);
        }

        [Fact]
        public void ApplyOpacity_OpacityAboveOne_ClampsToOne()
        {
            // Arrange
            var color = new Color(255, 128, 64, 255);
            float opacity = 1.5f;

            // Act
            var result = VisualizationHelpers.ApplyOpacity(color, opacity);

            // Assert - Only alpha channel changes, RGB stays the same
            Assert.Equal(255, result.R);
            Assert.Equal(128, result.G);
            Assert.Equal(64, result.B);
            Assert.Equal(255, result.A);
        }

        [Fact]
        public void ApplyOpacity_NegativeOpacity_ClampsToZero()
        {
            // Arrange
            var color = new Color(255, 128, 64, 255);
            float opacity = -0.5f;

            // Act
            var result = VisualizationHelpers.ApplyOpacity(color, opacity);

            // Assert - Only alpha channel changes, RGB stays the same
            Assert.Equal(255, result.R);
            Assert.Equal(128, result.G);
            Assert.Equal(64, result.B);
            Assert.Equal(0, result.A);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(10f)]
        [InlineData(20f)]
        [InlineData(30f)]
        [InlineData(33f)]
        public void GetHealthCategory_PoorHealthRange_ReturnsPoor(float health)
        {
            // Arrange
            var category = VisualizationHelpers.GetHealthCategory(health);

            // Assert
            Assert.Equal(HealthCategory.Poor, category);
        }

        [Theory]
        [InlineData(34f)]
        [InlineData(45f)]
        [InlineData(50f)]
        [InlineData(60f)]
        public void GetHealthCategory_ModerateHealthRange_ReturnsModerate(float health)
        {
            // Arrange
            var category = VisualizationHelpers.GetHealthCategory(health);

            // Assert
            Assert.Equal(HealthCategory.Moderate, category);
        }

        [Theory]
        [InlineData(67f)]
        [InlineData(75f)]
        [InlineData(85f)]
        [InlineData(95f)]
        [InlineData(100f)]
        public void GetHealthCategory_HealthyHealthRange_ReturnsHealthy(float health)
        {
            // Arrange
            var category = VisualizationHelpers.GetHealthCategory(health);

            // Assert
            Assert.Equal(HealthCategory.Healthy, category);
        }

        [Fact]
        public void GetHealthCategory_NegativeHealth_ClampsToPoor()
        {
            // Act
            var category = VisualizationHelpers.GetHealthCategory(-10f);

            // Assert
            Assert.Equal(HealthCategory.Poor, category);
        }

        [Fact]
        public void GetHealthCategory_HealthAbove100_ClampsToHealthy()
        {
            // Act
            var category = VisualizationHelpers.GetHealthCategory(150f);

            // Assert
            Assert.Equal(HealthCategory.Healthy, category);
        }

        [Fact]
        public void GetHealthCategory_BoundaryValues_ReturnsCorrectCategory()
        {
            // Act & Assert
            Assert.Equal(HealthCategory.Poor, VisualizationHelpers.GetHealthCategory(0f));
            Assert.Equal(HealthCategory.Poor, VisualizationHelpers.GetHealthCategory(33f));
            Assert.Equal(HealthCategory.Moderate, VisualizationHelpers.GetHealthCategory(34f));
            Assert.Equal(HealthCategory.Moderate, VisualizationHelpers.GetHealthCategory(66f));
            Assert.Equal(HealthCategory.Healthy, VisualizationHelpers.GetHealthCategory(67f));
            Assert.Equal(HealthCategory.Healthy, VisualizationHelpers.GetHealthCategory(100f));
        }

        [Fact]
        public void GetHealthLevelText_WithFloatingPointValues_PoorRange_ReturnsPoor()
        {
            // Act
            string result = VisualizationHelpers.GetHealthLevelText(16.5f);

            // Assert
            Assert.Equal("Poor", result);
        }

        [Fact]
        public void GetHealthLevelText_WithFloatingPointValues_ModerateRange_ReturnsModerate()
        {
            // Act
            string result = VisualizationHelpers.GetHealthLevelText(49.9f);

            // Assert
            Assert.Equal("Moderate", result);
        }

        [Fact]
        public void GetHealthLevelText_WithFloatingPointValues_HealthyRange_ReturnsHealthy()
        {
            // Act
            string result = VisualizationHelpers.GetHealthLevelText(83.3f);

            // Assert
            Assert.Equal("Healthy", result);
        }
    }
}
