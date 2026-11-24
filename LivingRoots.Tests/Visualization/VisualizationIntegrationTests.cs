using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationIntegrationTests
    {
        [Fact]
        public void FullPipeline_ColorInterpolation_Correct()
        {
            var service = new ColorInterpolationService();
            var poorColor = service.GetColorForHealth(0);
            var healthyColor = service.GetColorForHealth(100);
            Assert.NotEqual(poorColor, healthyColor);
        }

        [Fact]
        public void FullPipeline_OverlayRendering_WithPatterns()
        {
            var overlay = new TileOverlay(new Point(5, 5), Color.Red, PatternType.Stripes, 25f, HealthCategory.Poor);
            Assert.Equal(PatternType.Stripes, overlay.PatternType);
        }
    }
}
