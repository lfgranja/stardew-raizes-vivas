using System;
using LivingRoots;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// TDD tests for visualization constants in ModConstants.
    /// These tests verify that the required visualization constants exist
    /// with their exact expected values. This test should FAIL initially
    /// because the constants don't exist yet.
    /// </summary>
    public class VisualizationConstantsTests
    {
        // ──────────────────────────────────────────────
        // Color Constants
        // ──────────────────────────────────────────────

        [Fact]
        public void PoorColor_ShouldBeRed_FF0000()
        {
            // Arrange
            var expected = new Color(255, 0, 0, 255); // #FF0000

            // Act
            var actual = ModConstants.PoorColor;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ModerateColor_ShouldBeYellow_FFFF00()
        {
            // Arrange
            var expected = new Color(255, 255, 0, 255); // #FFFF00

            // Act
            var actual = ModConstants.ModerateColor;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void HealthyColor_ShouldBeGreen_00FF00()
        {
            // Arrange
            var expected = new Color(0, 255, 0, 255); // #00FF00

            // Act
            var actual = ModConstants.HealthyColor;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UnknownColor_ShouldBeGray_808080()
        {
            // Arrange
            var expected = new Color(128, 128, 128, 255); // #808080

            // Act
            var actual = ModConstants.UnknownColor;

            // Assert
            Assert.Equal(expected, actual);
        }

        // ──────────────────────────────────────────────
        // Opacity Constants
        // ──────────────────────────────────────────────

        [Fact]
        public void DefaultOpacity_ShouldBe_0_5f()
        {
            // Arrange
            const float expected = 0.5f;

            // Act
            var actual = ModConstants.DefaultOpacity;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void PatternMinOpacity_ShouldBe_0_7f()
        {
            // Arrange
            const float expected = 0.7f;

            // Act
            var actual = ModConstants.PatternMinOpacity;

            // Assert
            Assert.Equal(expected, actual);
        }

        // ──────────────────────────────────────────────
        // Duration Constants
        // ──────────────────────────────────────────────

        [Fact]
        public void FlashDurationMs_ShouldBe_300()
        {
            // Arrange
            const int expected = 300;

            // Act
            var actual = ModConstants.FlashDurationMs;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TextDurationMs_ShouldBe_1000()
        {
            // Arrange
            const int expected = 1000;

            // Act
            var actual = ModConstants.TextDurationMs;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MaxRenderTimeMs_ShouldBe_16_67()
        {
            // Arrange
            const double expected = 16.67;

            // Act
            var actual = ModConstants.MaxRenderTimeMs;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TooltipDebounceMs_ShouldBe_50()
        {
            // Arrange
            const int expected = 50;

            // Act
            var actual = ModConstants.TooltipDebounceMs;

            // Assert
            Assert.Equal(expected, actual);
        }

        // ──────────────────────────────────────────────
        // Feature Toggle Defaults
        // ──────────────────────────────────────────────

        [Fact]
        public void OverlaysEnabledDefault_ShouldBeTrue()
        {
            // Arrange
            const bool expected = true;

            // Act
            var actual = ModConstants.OverlaysEnabledDefault;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TooltipsEnabledDefault_ShouldBeTrue()
        {
            // Arrange
            const bool expected = true;

            // Act
            var actual = ModConstants.TooltipsEnabledDefault;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void HoeFeedbackEnabledDefault_ShouldBeTrue()
        {
            // Arrange
            const bool expected = true;

            // Act
            var actual = ModConstants.HoeFeedbackEnabledDefault;

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
