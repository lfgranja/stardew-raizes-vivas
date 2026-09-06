using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationConfigurationTests
    {
        [Fact]
        public void Defaults_AreCorrect()
        {
            var config = new VisualizationConfiguration();
            Assert.True(config.OverlaysEnabled);
            Assert.True(config.TooltipsEnabled);
            Assert.True(config.HoeFeedbackEnabled);
            Assert.Equal(0.5f, config.Opacity);
            Assert.True(config.ShowPatterns);
            Assert.Equal("auto", config.AccessibilityDegradation);
        }

        [Fact]
        public void PropertySetters_Work()
        {
            var config = new VisualizationConfiguration
            {
                OverlaysEnabled = false,
                TooltipsEnabled = false,
                HoeFeedbackEnabled = false,
                Opacity = 0.8f,
                ShowPatterns = false,
                AccessibilityDegradation = "never"
            };
            Assert.False(config.OverlaysEnabled);
            Assert.Equal(0.8f, config.Opacity);
            Assert.Equal("never", config.AccessibilityDegradation);
        }
    }
}
