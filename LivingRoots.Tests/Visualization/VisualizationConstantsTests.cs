using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for visualization-related default constants in <see cref="ModConstants"/>.
    /// Pins the canonical palette to the earth tones declared in
    /// <c>specs/001-soil-health-visualization/spec.md</c> (clarification 127, 2026-09-13)
    /// and <c>data-model.md</c> ("Palette authority" header). This is the only place the
    /// hex values are written down, so an accidental palette change fails here.
    /// </summary>
    public class VisualizationConstantsTests
    {
        [Fact]
        public void PoorColor_IsEarthToneRed()
        {
            // Spec palette (clarification 127): #B91C1C, opaque.
            Assert.Equal(185, ModConstants.PoorColor.R);
            Assert.Equal(28, ModConstants.PoorColor.G);
            Assert.Equal(28, ModConstants.PoorColor.B);
            Assert.Equal(255, ModConstants.PoorColor.A);
        }

        [Fact]
        public void ModerateColor_IsEarthToneAmber()
        {
            // Spec palette (clarification 127): #D97706, opaque.
            Assert.Equal(217, ModConstants.ModerateColor.R);
            Assert.Equal(119, ModConstants.ModerateColor.G);
            Assert.Equal(6, ModConstants.ModerateColor.B);
            Assert.Equal(255, ModConstants.ModerateColor.A);
        }

        [Fact]
        public void HealthyColor_IsEarthToneForestGreen()
        {
            // Spec palette (clarification 127): #15803D, opaque.
            Assert.Equal(21, ModConstants.HealthyColor.R);
            Assert.Equal(128, ModConstants.HealthyColor.G);
            Assert.Equal(61, ModConstants.HealthyColor.B);
            Assert.Equal(255, ModConstants.HealthyColor.A);
        }

        [Fact]
        public void UnknownColor_IsEarthToneSlateGray()
        {
            // Spec palette (clarification 127): #6B7280, opaque.
            Assert.Equal(107, ModConstants.UnknownColor.R);
            Assert.Equal(114, ModConstants.UnknownColor.G);
            Assert.Equal(128, ModConstants.UnknownColor.B);
            Assert.Equal(255, ModConstants.UnknownColor.A);
        }

        [Fact]
        public void DefaultOpacity_IsHalf()
        {
            Assert.Equal(0.5f, ModConstants.DefaultOpacity);
        }

        [Fact]
        public void PatternMinOpacity_IsSeventyPercent()
        {
            Assert.Equal(0.7f, ModConstants.PatternMinOpacity);
        }

        [Fact]
        public void FlashDurationMs_Is300()
        {
            Assert.Equal(300, ModConstants.FlashDurationMs);
        }

        [Fact]
        public void TextDurationMs_Is1000()
        {
            Assert.Equal(1000, ModConstants.TextDurationMs);
        }

        [Fact]
        public void MaxRenderTimeMs_Is16Point67()
        {
            Assert.Equal(16.67, ModConstants.MaxRenderTimeMs);
        }

        [Fact]
        public void TooltipDebounceMs_Is50()
        {
            Assert.Equal(50, ModConstants.TooltipDebounceMs);
        }

        [Fact]
        public void OverlaysEnabledDefault_IsTrue()
        {
            Assert.True(ModConstants.OverlaysEnabledDefault);
        }

        [Fact]
        public void TooltipsEnabledDefault_IsTrue()
        {
            Assert.True(ModConstants.TooltipsEnabledDefault);
        }

        [Fact]
        public void HoeFeedbackEnabledDefault_IsTrue()
        {
            Assert.True(ModConstants.HoeFeedbackEnabledDefault);
        }
    }
}
