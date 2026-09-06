using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class TileOverlayTests
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            var overlay = new TileOverlay(new Point(5, 10), Color.Red, PatternType.Stripes, 25f, HealthCategory.Poor);
            Assert.Equal(new Point(5, 10), overlay.TilePosition);
            Assert.Equal(Color.Red, overlay.Color);
            Assert.Equal(PatternType.Stripes, overlay.PatternType);
            Assert.Equal(25f, overlay.HealthValue);
            Assert.Equal(HealthCategory.Poor, overlay.Category);
        }
    }
}
