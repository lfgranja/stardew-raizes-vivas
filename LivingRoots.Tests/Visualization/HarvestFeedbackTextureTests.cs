using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class HarvestFeedbackTextureTests
    {
        [Fact]
        public void CreateMarkerPoorPixels_Returns16x16WithVisiblePixels()
        {
            var pixels = HarvestFeedbackTextures.CreateMarkerPoorPixels();

            Assert.NotNull(pixels);
            Assert.Equal(16 * 16, pixels.Length);
            Assert.Contains(pixels, c => c.A > 0);
        }

        [Fact]
        public void CreateSparkleLivingPixels_Returns16x16WithVisiblePixels()
        {
            var pixels = HarvestFeedbackTextures.CreateSparkleLivingPixels();

            Assert.NotNull(pixels);
            Assert.Equal(16 * 16, pixels.Length);
            Assert.Contains(pixels, c => c.A > 0);
        }

        [Property(MaxTest = 50)]
        public Property MarkerPixels_AreDeterministic()
        {
            return Prop.ForAll(Arb.Default.PositiveInt(), _ =>
            {
                var a = HarvestFeedbackTextures.CreateMarkerPoorPixels();
                var b = HarvestFeedbackTextures.CreateMarkerPoorPixels();
                return a.SequenceEqual(b);
            });
        }

        [Property(MaxTest = 50)]
        public Property SparklePixels_AreDeterministic()
        {
            return Prop.ForAll(Arb.Default.PositiveInt(), _ =>
            {
                var a = HarvestFeedbackTextures.CreateSparkleLivingPixels();
                var b = HarvestFeedbackTextures.CreateSparkleLivingPixels();
                return a.SequenceEqual(b);
            });
        }
    }
}
