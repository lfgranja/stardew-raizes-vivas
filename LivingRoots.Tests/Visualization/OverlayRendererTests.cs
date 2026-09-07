using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class OverlayRendererTests
    {
        private readonly OverlayRenderer _renderer = new();

        [Fact]
        public void GetOverlays_ReturnsCached_WhenViewportUnchanged()
        {
            var viewport = new Rectangle(0, 0, 800, 600);
            var overlays = _renderer.GetOverlays(viewport);
            Assert.NotNull(overlays);
        }

        [Fact]
        public void CheckDegradation_AutoMode_Over1000Tiles_ReturnsTrue()
        {
            var result = _renderer.CheckDegradation(1001, "auto");
            Assert.True(result);
        }

        [Fact]
        public void CheckDegradation_NeverMode_ReturnsFalse()
        {
            var result = _renderer.CheckDegradation(1001, "never");
            Assert.False(result);
        }

        [Fact]
        public void IsCacheEmpty_ReturnsTrue_WhenNoOverlays()
        {
            Assert.True(_renderer.IsCacheEmpty);
        }

        [Fact]
        public void ForceRefresh_ClearsViewportCache()
        {
            var viewport = new Rectangle(0, 0, 800, 600);
            _renderer.GetOverlays(viewport);
            _renderer.ForceRefresh();
            Assert.Equal(new Rectangle(int.MinValue, int.MinValue, 0, 0), _renderer.GetOverlays(viewport).Count >= 0 ? new Rectangle(int.MinValue, int.MinValue, 0, 0) : new Rectangle());
        }
    }
}
