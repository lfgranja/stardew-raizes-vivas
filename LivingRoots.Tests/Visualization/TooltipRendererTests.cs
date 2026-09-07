using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class TooltipRendererTests
    {
        private readonly TooltipRenderer _renderer = new();

        [Fact]
        public void GetTooltip_NewTile_ReturnsTooltip()
        {
            var tooltip = _renderer.GetTooltip(new Point(1, 1), 75f, HealthCategory.Healthy, new Vector2(100, 200));
            Assert.NotNull(tooltip);
            Assert.Contains("75%", tooltip!.Text);
            Assert.Contains("Healthy", tooltip.Text);
        }

        [Fact]
        public void GetTooltip_SameTileWithinThrottle_ReturnsNull()
        {
            _renderer.GetTooltip(new Point(1, 1), 75f, HealthCategory.Healthy, new Vector2(100, 200));
            var tooltip = _renderer.GetTooltip(new Point(1, 1), 80f, HealthCategory.Healthy, new Vector2(100, 200));
            Assert.Null(tooltip);
        }
    }
}
