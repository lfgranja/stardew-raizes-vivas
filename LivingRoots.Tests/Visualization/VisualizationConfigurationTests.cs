using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationConfigurationTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Assert
            Assert.True(config.OverlaysEnabled);
            Assert.True(config.TooltipsEnabled);
            Assert.True(config.HoeFeedbackEnabled);
            Assert.Equal(0.5f, config.Opacity);
            Assert.True(config.ShowPatterns);
            Assert.Equal("auto", config.AccessibilityDegradation);
        }

        [Fact]
        public void DefaultColors_MatchModConstantsValues()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Assert - PoorColor defaults to Red (#FF0000)
            Assert.Equal(255, config.PoorColor.R);
            Assert.Equal(0, config.PoorColor.G);
            Assert.Equal(0, config.PoorColor.B);
            Assert.Equal(255, config.PoorColor.A);

            // ModerateColor defaults to Yellow (#FFFF00)
            Assert.Equal(255, config.ModerateColor.R);
            Assert.Equal(255, config.ModerateColor.G);
            Assert.Equal(0, config.ModerateColor.B);
            Assert.Equal(255, config.ModerateColor.A);

            // HealthyColor defaults to Green (#00FF00)
            Assert.Equal(0, config.HealthyColor.R);
            Assert.Equal(255, config.HealthyColor.G);
            Assert.Equal(0, config.HealthyColor.B);
            Assert.Equal(255, config.HealthyColor.A);

            // UnknownColor defaults to Gray (#808080)
            Assert.Equal(128, config.UnknownColor.R);
            Assert.Equal(128, config.UnknownColor.G);
            Assert.Equal(128, config.UnknownColor.B);
            Assert.Equal(255, config.UnknownColor.A);
        }

        [Fact]
        public void PropertySetters_UpdateValues()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.OverlaysEnabled = false;
            config.TooltipsEnabled = false;
            config.HoeFeedbackEnabled = false;
            config.Opacity = 0.8f;
            config.ShowPatterns = false;
            config.PoorColor = new ColorDTO { R = 100, G = 0, B = 0, A = 255 };

            // Assert
            Assert.False(config.OverlaysEnabled);
            Assert.False(config.TooltipsEnabled);
            Assert.False(config.HoeFeedbackEnabled);
            Assert.Equal(0.8f, config.Opacity);
            Assert.False(config.ShowPatterns);
            Assert.Equal(100, config.PoorColor.R);
        }

        [Theory]
        [InlineData("auto")]
        [InlineData("never")]
        [InlineData("notify")]
        public void AccessibilityDegradation_AcceptsValidValues(string value)
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.AccessibilityDegradation = value;

            // Assert
            Assert.Equal(value, config.AccessibilityDegradation);
        }
    }
}
