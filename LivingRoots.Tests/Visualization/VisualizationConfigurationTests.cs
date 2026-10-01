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

            // Assert - configuration defaults are the earth-tone palette from
            // ModConstants (spec clarification 127): #B91C1C / #D97706 / #15803D / #6B7280.
            Assert.Equal(ModConstants.PoorColor.R, config.PoorColor.R);
            Assert.Equal(ModConstants.PoorColor.G, config.PoorColor.G);
            Assert.Equal(ModConstants.PoorColor.B, config.PoorColor.B);
            Assert.Equal(ModConstants.PoorColor.A, config.PoorColor.A);

            Assert.Equal(ModConstants.ModerateColor.R, config.ModerateColor.R);
            Assert.Equal(ModConstants.ModerateColor.G, config.ModerateColor.G);
            Assert.Equal(ModConstants.ModerateColor.B, config.ModerateColor.B);
            Assert.Equal(ModConstants.ModerateColor.A, config.ModerateColor.A);

            Assert.Equal(ModConstants.HealthyColor.R, config.HealthyColor.R);
            Assert.Equal(ModConstants.HealthyColor.G, config.HealthyColor.G);
            Assert.Equal(ModConstants.HealthyColor.B, config.HealthyColor.B);
            Assert.Equal(ModConstants.HealthyColor.A, config.HealthyColor.A);

            Assert.Equal(ModConstants.UnknownColor.R, config.UnknownColor.R);
            Assert.Equal(ModConstants.UnknownColor.G, config.UnknownColor.G);
            Assert.Equal(ModConstants.UnknownColor.B, config.UnknownColor.B);
            Assert.Equal(ModConstants.UnknownColor.A, config.UnknownColor.A);
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
