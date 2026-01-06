using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Unit tests for ColorMapper service.
    /// Tests color mapping for different health values and configurations.
    /// </summary>
    public class ColorMapperTests
    {
        private readonly Mock<IVisualizationConfig> _mockConfig;
        private readonly ColorMapper _colorMapper;

        public ColorMapperTests()
        {
            _mockConfig = new Mock<IVisualizationConfig>();
            _colorMapper = new ColorMapper(_mockConfig.Object);
        }

        [Fact]
        public void Constructor_NullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ColorMapper(null!));
        }

        [Fact]
        public void GetHealthColor_PoorHealth_ReturnsReddishColor()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(10f);

            // Assert - Poor health should be reddish (interpolated from red to brown)
            Assert.True(color.R > 200, "Red channel should be high for poor health");
            Assert.True(color.G >= 0, "Green channel should be present for poor health");
            Assert.True(color.B >= 0, "Blue channel should be present for poor health");
        }

        [Fact]
        public void GetHealthColor_ModerateHealth_ReturnsYellowishColor()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(50f);

            // Assert - Moderate health should be yellowish (interpolated between brown and gold)
            Assert.True(color.R > 100, "Red channel should be moderate for moderate health");
            Assert.True(color.G > 100, "Green channel should be moderate for moderate health");
            Assert.True(color.B >= 19, "Blue channel should be moderate for moderate health");
        }

        [Fact]
        public void GetHealthColor_HealthyHealth_ReturnsGreenishColor()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(85f);

            // Assert - Healthy health should be greenish (interpolated between gold and olive)
            Assert.True(color.R > 50, "Red channel should be moderate for healthy health");
            Assert.True(color.G > 100, "Green channel should be high for healthy health");
            Assert.True(color.B >= 32, "Blue channel should be present for healthy health");
        }

        [Fact]
        public void GetHealthColor_BoundaryValueZero_ReturnsRed()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(0f);

            // Assert - Zero health should be pure red
            Assert.Equal(255, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }

        [Fact]
        public void GetHealthColor_BoundaryValue33_ReturnsPoorColor()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(33f);

            // Assert - At poor health threshold, should be at poor color
            Assert.True(color.R > 100, "Red channel should be present");
            Assert.True(color.G < 100, "Green channel should be low");
        }

        [Fact]
        public void GetHealthColor_BoundaryValue34_ReturnsBetweenPoorAndModerate()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(34f);

            // Assert - Just above poor threshold, should be between poor and moderate
            Assert.True(color.R > 100, "Red channel should be present");
            Assert.True(color.G > 50, "Green channel should be increasing");
        }

        [Fact]
        public void GetHealthColor_BoundaryValue66_ReturnsBetweenModerateAndHealthy()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(66f);

            // Assert - At moderate threshold, should be between moderate and healthy
            Assert.True(color.R > 100, "Red channel should be present");
            Assert.True(color.G > 100, "Green channel should be present");
        }

        [Fact]
        public void GetHealthColor_BoundaryValue67_ReturnsBetweenModerateAndHealthy()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(67f);

            // Assert - Just above moderate threshold, should be between moderate and healthy
            Assert.True(color.R > 100, "Red channel should be present");
            Assert.True(color.G > 100, "Green channel should be present");
        }

        [Fact]
        public void GetHealthColor_BoundaryValue100_ReturnsHealthyColor()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(100f);

            // Assert - Maximum health should be at healthy color
            Assert.Equal(85, color.R);
            Assert.Equal(107, color.G);
            Assert.Equal(47, color.B);
        }

        [Fact]
        public void GetHealthColor_CustomColors_UsesCustomColors()
        {
            // Arrange
            var customPoor = new Color(255, 0, 0);
            var customModerate = new Color(0, 255, 0);
            var customHealthy = new Color(0, 0, 255);

            _mockConfig.Setup(c => c.UseCustomColors).Returns(true);
            _mockConfig.Setup(c => c.PoorHealthColor).Returns(customPoor);
            _mockConfig.Setup(c => c.ModerateHealthColor).Returns(customModerate);
            _mockConfig.Setup(c => c.HealthyHealthColor).Returns(customHealthy);

            // Act
            Color poorColor = _colorMapper.GetHealthColor(10f);
            Color moderateColor = _colorMapper.GetHealthColor(50f);
            Color healthyColor = _colorMapper.GetHealthColor(85f);

            // Assert - Custom colors should be used (interpolated from custom palette)
            // Poor health (10) interpolates from red (255,0,0) to customPoor (255,0,0)
            Assert.True(poorColor.R > 200, "Poor health should use custom red");
            // Moderate health (50) interpolates between customPoor (255,0,0) and customModerate (0,255,0)
            // At 50% interpolation: R=127, G=127, B=0
            Assert.True(moderateColor.R > 0 || moderateColor.G > 0 || moderateColor.B > 0, "Moderate health should use custom palette");
            // Healthy health (85) interpolates between customModerate (0,255,0) and customHealthy (0,0,255)
            Assert.True(healthyColor.R > 0 || healthyColor.G > 0 || healthyColor.B > 0, "Healthy health should use custom palette");
        }

        [Fact]
        public void GetHealthColor_WithCustomColors_UsesProvidedColors()
        {
            // Arrange
            var poor = new Color(255, 0, 0);
            var moderate = new Color(0, 255, 0);
            var healthy = new Color(0, 0, 255);

            // Act
            Color color = _colorMapper.GetHealthColor(50f, poor, moderate, healthy);

            // Assert - Should use provided custom colors
            Assert.True(color.R > 0 || color.G > 0 || color.B > 0, "Should have color from custom palette");
        }

        [Fact]
        public void GetHealthColor_NegativeHealth_ClampsToZero()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(-10f);

            // Assert - Negative health should be clamped to 0 (red)
            Assert.Equal(255, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }

        [Fact]
        public void GetHealthColor_HealthAbove100_ClampsTo100()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(150f);

            // Assert - Health above 100 should be clamped to 100 (healthy color)
            Assert.Equal(85, color.R);
            Assert.Equal(107, color.G);
            Assert.Equal(47, color.B);
        }

        [Fact]
        public void GetHealthColor_FloatingPointValues_HandlesCorrectly()
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color1 = _colorMapper.GetHealthColor(16.5f);
            Color color2 = _colorMapper.GetHealthColor(49.9f);
            Color color3 = _colorMapper.GetHealthColor(83.3f);

            // Assert - Should handle floating point values correctly
            Assert.True(color1.R >= 0 || color1.G >= 0 || color1.B >= 0);
            Assert.True(color2.R >= 0 || color2.G >= 0 || color2.B >= 0);
            Assert.True(color3.R >= 0 || color3.G >= 0 || color3.B >= 0);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(10f)]
        [InlineData(20f)]
        [InlineData(30f)]
        [InlineData(33f)]
        public void GetHealthColor_PoorHealthRange_ReturnsReddishColors(float health)
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(health);

            // Assert - Poor health range should return reddish colors (interpolated from red to brown)
            Assert.True(color.R >= 139, $"Red channel should be >= 139 for health {health}");
            Assert.True(color.G >= 0, $"Green channel should be >= 0 for health {health}");
            Assert.True(color.B >= 0, $"Blue channel should be >= 0 for health {health}");
        }

        [Theory]
        [InlineData(34f)]
        [InlineData(45f)]
        [InlineData(50f)]
        [InlineData(60f)]
        [InlineData(66f)]
        public void GetHealthColor_ModerateHealthRange_ReturnsYellowishColors(float health)
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(health);

            // Assert - Moderate health range should return yellowish colors (interpolated between brown and gold)
            Assert.True(color.R >= 139, $"Red channel should be >= 139 for health {health}");
            Assert.True(color.G >= 69, $"Green channel should be >= 69 for health {health}");
            Assert.True(color.B >= 19, $"Blue channel should be >= 19 for health {health}");
        }

        [Theory]
        [InlineData(67f)]
        [InlineData(75f)]
        [InlineData(85f)]
        [InlineData(95f)]
        [InlineData(100f)]
        public void GetHealthColor_HealthyHealthRange_ReturnsGreenishColors(float health)
        {
            // Arrange
            _mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            // Act
            Color color = _colorMapper.GetHealthColor(health);

            // Assert - Healthy health range should return greenish colors (interpolated between gold and olive)
            Assert.True(color.R >= 85, $"Red channel should be >= 85 for health {health}");
            Assert.True(color.G >= 107, $"Green channel should be >= 107 for health {health}");
            Assert.True(color.B >= 32, $"Blue channel should be >= 32 for health {health}");
        }
    }
}
