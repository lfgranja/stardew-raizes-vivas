using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationServiceOverlayTests
    {
        // One tile sized viewport keeps the fixture tile provably inside it.
        private static readonly Rectangle Viewport = new Rectangle(0, 0, ModConstants.TileSize, ModConstants.TileSize);
        private static readonly Point VisibleTile = new Point(0, 0);
        private static readonly float VisibleTileHealth = ModConstants.MaxSoilHealth * 0.75f;

        private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
        private readonly Mock<IColorInterpolationService> _mockColorService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly VisualizationService _service;

        public VisualizationServiceOverlayTests()
        {
            _mockConfigService = new Mock<IVisualizationConfigurationService>();
            _mockColorService = new Mock<IColorInterpolationService>();
            _mockMonitor = new Mock<IMonitor>();
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { OverlaysEnabled = true });
            _service = new VisualizationService(_mockColorService.Object, _mockConfigService.Object, _mockMonitor.Object);

            // Seeded so that a live render pass would have an overlay to draw: the
            // "does not render" tests below then fail loudly if a gate is skipped.
            _service.SetTileHealthData(new Dictionary<Point, float> { { VisibleTile, VisibleTileHealth } });
        }

        /// <summary>
        /// A paused service returns before it even reads the configuration, so no overlay
        /// list is built, no color is resolved, and no draw call is attempted.
        /// </summary>
        [Fact]
        public void RenderOverlays_WhenPaused_DoesNotRender()
        {
            _service.PauseRendering();

            var exception = Record.Exception(() => _service.RenderOverlays(null!, Viewport, new GameTime()));

            Assert.Null(exception);
            _mockConfigService.Verify(c => c.GetConfiguration(), Times.Never);
            _mockColorService.Verify(c => c.GetCategoryForHealth(It.IsAny<float>()), Times.Never);
        }

        /// <summary>
        /// FR-007: with overlays disabled the configuration is read exactly once and the
        /// frame returns before any tile overlay is computed.
        /// </summary>
        [Fact]
        public void RenderOverlays_WhenDisabled_DoesNotRender()
        {
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { OverlaysEnabled = false });

            var exception = Record.Exception(() => _service.RenderOverlays(null!, Viewport, new GameTime()));

            Assert.Null(exception);
            _mockConfigService.Verify(c => c.GetConfiguration(), Times.Once);
            _mockColorService.Verify(c => c.GetCategoryForHealth(It.IsAny<float>()), Times.Never);
        }

        /// <summary>
        /// <c>PauseRendering</c> suppresses the whole overlay pipeline and
        /// <c>ResumeRendering</c> restores it. The proof of resuming is that the overlay is
        /// actually built: the color service is consulted for the visible tile, and the
        /// frame then reaches the draw stage.
        /// </summary>
        /// <remarks>
        /// Known unit-test limitation: a real <c>SpriteBatch</c> needs a graphics device, so
        /// the draw stage raises <see cref="InvalidOperationException"/> from
        /// <c>GetTexture()</c> (no texture was supplied via <c>Initialize</c>) before the
        /// null <c>SpriteBatch</c> would be dereferenced. Getting past every early return
        /// to that guard is the observable signal that rendering resumed.
        /// </remarks>
        [Fact]
        public void PauseRendering_SetsIsPaused()
        {
            _service.PauseRendering();

            var whilePaused = Record.Exception(() => _service.RenderOverlays(null!, Viewport, new GameTime()));

            Assert.Null(whilePaused);
            _mockColorService.Verify(c => c.GetCategoryForHealth(It.IsAny<float>()), Times.Never);

            _service.ResumeRendering();
            var afterResume = Record.Exception(() => _service.RenderOverlays(null!, Viewport, new GameTime()));

            Assert.IsType<InvalidOperationException>(afterResume);
            _mockColorService.Verify(c => c.GetCategoryForHealth(VisibleTileHealth), Times.Once);
        }
    }
}
