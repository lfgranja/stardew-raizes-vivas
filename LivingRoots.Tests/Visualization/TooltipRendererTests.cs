using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for <see cref="TooltipRenderer"/>.
    /// Verifies known-state tooltip text, the unknown-state text (FR-014),
    /// update throttling and leading-edge updates (FR-015), and rejection of
    /// out-of-bounds cursor tiles.
    /// </summary>
    public class TooltipRendererTests
    {
        private const double FrameMilliseconds = 16d;
        private const double TimeOriginMs = -1000d;

        private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IColorInterpolationService> _mockColorService;
        private readonly TooltipRenderer _renderer;

        public TooltipRendererTests()
        {
            _mockConfigService = new Mock<IVisualizationConfigurationService>();
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration
            {
                TooltipsEnabled = true
            });

            _mockMonitor = new Mock<IMonitor>();

            _mockColorService = new Mock<IColorInterpolationService>();
            _mockColorService.Setup(c => c.GetColorForHealth(It.IsAny<float>())).Returns(Color.White);
            _mockColorService.Setup(c => c.GetCategoryForHealth(It.IsAny<float>())).Returns(HealthCategory.Healthy);

            _renderer = new TooltipRenderer(
                _mockConfigService.Object,
                _mockMonitor.Object,
                _mockColorService.Object);
        }

        private static GameTime BuildGameTime(double elapsedMs)
        {
            return new GameTime(
                TimeSpan.FromMilliseconds(TimeOriginMs + elapsedMs),
                TimeSpan.FromMilliseconds(FrameMilliseconds));
        }

        [Fact]
        public void GetTooltip_KnownHealth_ReturnsPercentageAndCategory()
        {
            // Arrange
            var cursorPosition = new Vector2(100, 200);
            var cursorTile = new Point(3, 4);
            var tileHealthData = new Dictionary<Point, float> { { cursorTile, 75f } };

            // Act
            var tooltip = _renderer.GetTooltip(cursorPosition, cursorTile, tileHealthData, BuildGameTime(0));

            // Assert
            Assert.NotNull(tooltip);
            Assert.NotNull(tooltip!.Text);
            Assert.Contains("75%", tooltip.Text);
            Assert.Contains(HealthCategory.Healthy.ToString(), tooltip.Text);
            Assert.Equal(cursorPosition, tooltip.Position);
        }

        /// <summary>
        /// FR-014: a hovered tile with no health data must render the unknown-state
        /// tooltip, which deliberately omits any percentage.
        /// </summary>
        [Fact]
        public void GetTooltip_TileAbsentFromData_ReturnsUnknownTextWithoutPercentage()
        {
            // Arrange
            var cursorTile = new Point(7, 8);
            var tileHealthData = new Dictionary<Point, float> { { new Point(9, 9), 60f } };

            // Act
            var tooltip = _renderer.GetTooltip(new Vector2(50, 60), cursorTile, tileHealthData, BuildGameTime(0));

            // Assert
            Assert.NotNull(tooltip);
            Assert.Equal("Soil Health: Unknown", tooltip!.Text);
            Assert.DoesNotContain("%", tooltip.Text);
        }

        /// <summary>
        /// FR-015: repeating the same tile inside the debounce window is suppressed.
        /// Game time is driven from a negative origin because the renderer's initial
        /// sentinel timestamp is <c>TimeSpan.MinValue</c>, which cannot be subtracted
        /// from a non-negative total game time.
        /// </summary>
        [Fact]
        public void GetTooltip_SameTileWithinDebounceWindow_ReturnsNull()
        {
            // Arrange
            var cursorTile = new Point(2, 2);
            var tileHealthData = new Dictionary<Point, float> { { cursorTile, 80f } };

            // Act
            var first = _renderer.GetTooltip(new Vector2(10, 10), cursorTile, tileHealthData, BuildGameTime(0));
            var throttled = _renderer.GetTooltip(
                new Vector2(12, 10),
                cursorTile,
                tileHealthData,
                BuildGameTime(ModConstants.TooltipDebounceMs - 1));

            // Assert
            Assert.NotNull(first);
            Assert.Null(throttled);
        }

        /// <summary>
        /// FR-015: once the debounce window has elapsed, the same tile refreshes again.
        /// Game time is driven from a negative origin for the reason documented on
        /// <see cref="GetTooltip_SameTileWithinDebounceWindow_ReturnsNull"/>.
        /// </summary>
        [Fact]
        public void GetTooltip_SameTileAfterDebounceWindow_ReturnsTooltip()
        {
            // Arrange
            var cursorTile = new Point(5, 5);
            var tileHealthData = new Dictionary<Point, float> { { cursorTile, 30f } };

            // Act
            var first = _renderer.GetTooltip(new Vector2(10, 10), cursorTile, tileHealthData, BuildGameTime(0));
            var refreshed = _renderer.GetTooltip(
                new Vector2(12, 10),
                cursorTile,
                tileHealthData,
                BuildGameTime(ModConstants.TooltipDebounceMs));

            // Assert
            Assert.NotNull(first);
            Assert.NotNull(refreshed);
            Assert.Contains("30%", refreshed!.Text);
        }

        /// <summary>
        /// FR-015 leading edge: entering a different tile updates immediately even
        /// though the debounce window since the previous update has not elapsed.
        /// </summary>
        [Fact]
        public void GetTooltip_DifferentTileWithinDebounceWindow_UpdatesImmediately()
        {
            // Arrange
            var firstTile = new Point(2, 2);
            var secondTile = new Point(3, 2);
            var tileHealthData = new Dictionary<Point, float>
            {
                { firstTile, 80f },
                { secondTile, 45f }
            };

            // Act
            var first = _renderer.GetTooltip(new Vector2(10, 10), firstTile, tileHealthData, BuildGameTime(0));
            var second = _renderer.GetTooltip(
                new Vector2(20, 10),
                secondTile,
                tileHealthData,
                BuildGameTime(ModConstants.TooltipDebounceMs - 1));

            // Assert
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Contains("45%", second!.Text);
        }

        [Fact]
        public void GetTooltip_NegativeCursorTile_ReturnsNull()
        {
            // Arrange
            var tileHealthData = new Dictionary<Point, float> { { new Point(-1, 4), 70f } };

            // Act
            var negativeX = _renderer.GetTooltip(new Vector2(0, 0), new Point(-1, 4), tileHealthData, BuildGameTime(0));
            var negativeY = _renderer.GetTooltip(new Vector2(0, 0), new Point(4, -1), tileHealthData, BuildGameTime(0));

            // Assert
            Assert.Null(negativeX);
            Assert.Null(negativeY);
        }
    }
}
