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
    public class VisualizationServiceTooltipTests
    {
        private static readonly Vector2 CursorPosition = new Vector2(100f, 200f);
        private static readonly Point TrackedTile = new Point(5, 5);

        private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
        private readonly Mock<IColorInterpolationService> _mockColorService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly VisualizationService _service;

        public VisualizationServiceTooltipTests()
        {
            _mockConfigService = new Mock<IVisualizationConfigurationService>();
            _mockColorService = new Mock<IColorInterpolationService>();
            _mockMonitor = new Mock<IMonitor>();
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { TooltipsEnabled = true });
            _service = new VisualizationService(_mockColorService.Object, _mockConfigService.Object, _mockMonitor.Object);
        }

        /// <summary>
        /// FR-007: with tooltips disabled the service returns on its own configuration
        /// read, so <c>TooltipRenderer</c> is never asked for a tooltip, never logs, and
        /// no draw call is attempted. The cursor tile is set to a real tile first so the
        /// only possible reason for a silent frame is the disabled toggle.
        /// </summary>
        [Fact]
        public void RenderTooltip_WhenDisabled_DoesNotRender()
        {
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { TooltipsEnabled = false });
            _service.UpdateCursorTile(TrackedTile);

            var exception = Record.Exception(() => _service.RenderTooltip(null!, CursorPosition, new GameTime()));

            Assert.Null(exception);
            _mockConfigService.Verify(c => c.GetConfiguration(), Times.Once);
            _mockMonitor.Verify(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.Never);
        }

        /// <summary>
        /// Verifies that <c>UpdateCursorTile</c> actually feeds the tracked tile into
        /// <c>TooltipRenderer</c>: with the cursor on a real tile that has no health data,
        /// the renderer takes its unknown-state branch (FR-014) and logs that tile.
        /// </summary>
        /// <remarks>
        /// Known unit-test limitation: rendering then reaches <c>DrawTooltip</c>, which reads
        /// <c>Game1.smallFont</c> — null outside the running game — so the frame ends in a
        /// <see cref="NullReferenceException"/>. The renderer log is emitted before that XNA
        /// boundary and is the observable proof that the cursor tile was used. A real
        /// <c>SpriteBatch</c> needs a graphics device, hence the <c>null</c> argument.
        /// </remarks>
        [Fact]
        public void UpdateCursorTile_SetsPosition()
        {
            _service.UpdateCursorTile(TrackedTile);

            var exception = Record.Exception(() => _service.RenderTooltip(null!, CursorPosition, new GameTime()));

            Assert.IsType<NullReferenceException>(exception);
            _mockMonitor.Verify(m => m.Log("Tooltip unknown for tile (5, 5)", LogLevel.Trace), Times.Once);
        }

        /// <summary>
        /// <c>ClearCursorTile</c> resets the tracked tile to (-1, -1), which
        /// <c>TooltipRenderer</c> reads as "cursor outside the world". The renderer returns
        /// before logging or drawing, so the frame completes cleanly even though tooltips are
        /// enabled and the hovered tile still has no health data — the direct contrast with
        /// <see cref="UpdateCursorTile_SetsPosition"/>.
        /// </summary>
        [Fact]
        public void ClearCursorTile_ClearsPosition()
        {
            _service.UpdateCursorTile(TrackedTile);
            var withTrackedTile = Record.Exception(() => _service.RenderTooltip(null!, CursorPosition, new GameTime()));
            Assert.IsType<NullReferenceException>(withTrackedTile);
            _mockMonitor.Verify(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.Once);

            _mockMonitor.Invocations.Clear();
            _service.ClearCursorTile();
            var afterClear = Record.Exception(() => _service.RenderTooltip(null!, CursorPosition, new GameTime()));

            Assert.Null(afterClear);
            _mockMonitor.Verify(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.Never);
        }
    }
}
