using LivingRoots;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for the VisualizationConfiguration domain model.
    /// Verifies default values, color mappings, property setters,
    /// and AccessibilityDegradation options.
    /// </summary>
    public class VisualizationConfigurationTests
    {
        // ──────────────────────────────────────────────
        // Default Values
        // ──────────────────────────────────────────────

        [Fact]
        public void OverlaysEnabled_ShouldDefaultToTrue()
        {
            // Arrange & Act
            var config = new VisualizationConfiguration();

            // Assert
            Assert.True(config.OverlaysEnabled);
        }

        [Fact]
        public void TooltipsEnabled_ShouldDefaultToTrue()
        {
            // Arrange & Act
            var config = new VisualizationConfiguration();

            // Assert
            Assert.True(config.TooltipsEnabled);
        }

        [Fact]
        public void HoeFeedbackEnabled_ShouldDefaultToTrue()
        {
            // Arrange & Act
            var config = new VisualizationConfiguration();

            // Assert
            Assert.True(config.HoeFeedbackEnabled);
        }

        [Fact]
        public void Opacity_ShouldDefaultTo_0_5f()
        {
            // Arrange & Act
            var config = new VisualizationConfiguration();

            // Assert
            Assert.Equal(0.5f, config.Opacity);
        }

        [Fact]
        public void ShowPatterns_ShouldDefaultToTrue()
        {
            // Arrange & Act
            var config = new VisualizationConfiguration();

            // Assert
            Assert.True(config.ShowPatterns);
        }

        [Fact]
        public void AccessibilityDegradation_ShouldDefaultToAuto()
        {
            // Arrange & Act
            var config = new VisualizationConfiguration();

            // Assert
            Assert.Equal("auto", config.AccessibilityDegradation);
        }

        // ──────────────────────────────────────────────
        // Color Defaults Match ModConstants
        // ──────────────────────────────────────────────

        [Fact]
        public void PoorColor_ShouldDefaultToModConstantsValue_FF0000()
        {
            // Arrange
            var config = new VisualizationConfiguration();
            var expected = ModConstants.PoorColor;

            // Act
            var actual = config.PoorColor;

            // Assert
            Assert.Equal(expected.R, actual.R);
            Assert.Equal(expected.G, actual.G);
            Assert.Equal(expected.B, actual.B);
            Assert.Equal(expected.A, actual.A);
        }

        [Fact]
        public void ModerateColor_ShouldDefaultToModConstantsValue_FFFF00()
        {
            // Arrange
            var config = new VisualizationConfiguration();
            var expected = ModConstants.ModerateColor;

            // Act
            var actual = config.ModerateColor;

            // Assert
            Assert.Equal(expected.R, actual.R);
            Assert.Equal(expected.G, actual.G);
            Assert.Equal(expected.B, actual.B);
            Assert.Equal(expected.A, actual.A);
        }

        [Fact]
        public void HealthyColor_ShouldDefaultToModConstantsValue_00FF00()
        {
            // Arrange
            var config = new VisualizationConfiguration();
            var expected = ModConstants.HealthyColor;

            // Act
            var actual = config.HealthyColor;

            // Assert
            Assert.Equal(expected.R, actual.R);
            Assert.Equal(expected.G, actual.G);
            Assert.Equal(expected.B, actual.B);
            Assert.Equal(expected.A, actual.A);
        }

        [Fact]
        public void UnknownColor_ShouldDefaultToModConstantsValue_808080()
        {
            // Arrange
            var config = new VisualizationConfiguration();
            var expected = ModConstants.UnknownColor;

            // Act
            var actual = config.UnknownColor;

            // Assert
            Assert.Equal(expected.R, actual.R);
            Assert.Equal(expected.G, actual.G);
            Assert.Equal(expected.B, actual.B);
            Assert.Equal(expected.A, actual.A);
        }

        // ──────────────────────────────────────────────
        // Property Setters
        // ──────────────────────────────────────────────

        [Fact]
        public void OverlaysEnabled_ShouldBeSettable()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.OverlaysEnabled = false;

            // Assert
            Assert.False(config.OverlaysEnabled);
        }

        [Fact]
        public void TooltipsEnabled_ShouldBeSettable()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.TooltipsEnabled = false;

            // Assert
            Assert.False(config.TooltipsEnabled);
        }

        [Fact]
        public void HoeFeedbackEnabled_ShouldBeSettable()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.HoeFeedbackEnabled = false;

            // Assert
            Assert.False(config.HoeFeedbackEnabled);
        }

        [Fact]
        public void Opacity_ShouldBeSettable()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.Opacity = 0.75f;

            // Assert
            Assert.Equal(0.75f, config.Opacity);
        }

        [Fact]
        public void ShowPatterns_ShouldBeSettable()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.ShowPatterns = false;

            // Assert
            Assert.False(config.ShowPatterns);
        }

        [Fact]
        public void PoorColor_ShouldBeSettable()
        {
            // Arrange
            var config = new VisualizationConfiguration();
            var newColor = new ColorDTO(100, 100, 100, 255);

            // Act
            config.PoorColor = newColor;

            // Assert
            Assert.Equal(newColor, config.PoorColor);
        }

        [Fact]
        public void ModerateColor_ShouldBeSettable()
        {
            // Arrange
            var config = new VisualizationConfiguration();
            var newColor = new ColorDTO(100, 100, 100, 255);

            // Act
            config.ModerateColor = newColor;

            // Assert
            Assert.Equal(newColor, config.ModerateColor);
        }

        [Fact]
        public void HealthyColor_ShouldBeSettable()
        {
            // Arrange
            var config = new VisualizationConfiguration();
            var newColor = new ColorDTO(100, 100, 100, 255);

            // Act
            config.HealthyColor = newColor;

            // Assert
            Assert.Equal(newColor, config.HealthyColor);
        }

        [Fact]
        public void UnknownColor_ShouldBeSettable()
        {
            // Arrange
            var config = new VisualizationConfiguration();
            var newColor = new ColorDTO(100, 100, 100, 255);

            // Act
            config.UnknownColor = newColor;

            // Assert
            Assert.Equal(newColor, config.UnknownColor);
        }

        // ──────────────────────────────────────────────
        // AccessibilityDegradation Options
        // ──────────────────────────────────────────────

        [Fact]
        public void AccessibilityDegradation_ShouldAcceptAuto()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.AccessibilityDegradation = "auto";

            // Assert
            Assert.Equal("auto", config.AccessibilityDegradation);
        }

        [Fact]
        public void AccessibilityDegradation_ShouldAcceptNever()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.AccessibilityDegradation = "never";

            // Assert
            Assert.Equal("never", config.AccessibilityDegradation);
        }

        [Fact]
        public void AccessibilityDegradation_ShouldAcceptNotify()
        {
            // Arrange
            var config = new VisualizationConfiguration();

            // Act
            config.AccessibilityDegradation = "notify";

            // Assert
            Assert.Equal("notify", config.AccessibilityDegradation);
        }
    }
}
