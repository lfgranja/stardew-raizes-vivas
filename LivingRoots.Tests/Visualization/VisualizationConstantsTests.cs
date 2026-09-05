using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for visualization-related default constants in <see cref="ModConstants"/>.
    /// </summary>
    public class VisualizationConstantsTests
    {
        [Fact]
        public void PoorColor_IsRed()
        {
            var expected = Color.Red;
            Assert.Equal(expected.R, ModConstants.PoorColor.R);
            Assert.Equal(expected.G, ModConstants.PoorColor.G);
            Assert.Equal(expected.B, ModConstants.PoorColor.B);
            Assert.Equal(expected.A, ModConstants.PoorColor.A);
        }

        [Fact]
        public void ModerateColor_IsYellow()
        {
            var expected = Color.Yellow;
            Assert.Equal(expected.R, ModConstants.ModerateColor.R);
            Assert.Equal(expected.G, ModConstants.ModerateColor.G);
            Assert.Equal(expected.B, ModConstants.ModerateColor.B);
            Assert.Equal(expected.A, ModConstants.ModerateColor.A);
        }

        [Fact]
        public void HealthyColor_IsGreen()
        {
            var expected = Color.Green;
            Assert.Equal(expected.R, ModConstants.HealthyColor.R);
            Assert.Equal(expected.G, ModConstants.HealthyColor.G);
            Assert.Equal(expected.B, ModConstants.HealthyColor.B);
            Assert.Equal(expected.A, ModConstants.HealthyColor.A);
        }

        [Fact]
        public void UnknownColor_IsGray()
        {
            var expected = Color.Gray;
            Assert.Equal(expected.R, ModConstants.UnknownColor.R);
            Assert.Equal(expected.G, ModConstants.UnknownColor.G);
            Assert.Equal(expected.B, ModConstants.UnknownColor.B);
            Assert.Equal(expected.A, ModConstants.UnknownColor.A);
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
