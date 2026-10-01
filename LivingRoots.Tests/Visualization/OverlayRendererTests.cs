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
    /// Tests for <see cref="OverlayRenderer"/>.
    /// Verifies viewport culling (FR-009), overlay list caching (FR-010),
    /// graceful degradation thresholds (FR-009), and the zero-tile no-op (FR-021).
    /// </summary>
    public class OverlayRendererTests
    {
        private readonly Mock<IColorInterpolationService> _mockColorService;
        private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly OverlayRenderer _renderer;

        public OverlayRendererTests()
        {
            _mockColorService = new Mock<IColorInterpolationService>();
            _mockColorService.Setup(c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>())).Returns(Color.White);
            _mockColorService.Setup(c => c.GetCategoryForHealth(It.IsAny<float>())).Returns(HealthCategory.Poor);

            _mockConfigService = new Mock<IVisualizationConfigurationService>();
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration());

            _mockMonitor = new Mock<IMonitor>();

            _renderer = new OverlayRenderer(
                _mockColorService.Object,
                _mockConfigService.Object,
                _mockMonitor.Object);
        }

        /// <summary>
        /// Builds a tile health dictionary keyed by the supplied tile positions,
        /// all carrying the same known health value.
        /// </summary>
        private static Dictionary<Point, float> BuildTileData(params Point[] tiles)
        {
            var data = new Dictionary<Point, float>();
            foreach (var tile in tiles)
            {
                data[tile] = 40f;
            }
            return data;
        }

        /// <summary>
        /// Builds a tile set whose size is exactly the requested count, laid out
        /// row-major inside a viewport large enough to hold every tile.
        /// </summary>
        private static Dictionary<Point, float> BuildTileDataOfSize(int count, out Rectangle viewport)
        {
            const int Span = 100;
            viewport = new Rectangle(0, 0, Span, Span);

            var data = new Dictionary<Point, float>(count);
            var placed = 0;
            for (var x = 0; x < Span && placed < count; x++)
            {
                for (var y = 0; y < Span && placed < count; y++)
                {
                    data[new Point(x, y)] = 40f;
                    placed++;
                }
            }

            return data;
        }

        [Fact]
        public void GetOverlays_EmptyTileData_ReturnsEmptyListAndLeavesCacheEmpty()
        {
            // Arrange (FR-021)
            var viewport = new Rectangle(0, 0, 40, 40);
            var tileHealthData = new Dictionary<Point, float>();

            // Act
            var overlays = _renderer.GetOverlays(viewport, tileHealthData);

            // Assert
            Assert.NotNull(overlays);
            Assert.Empty(overlays);
            Assert.True(_renderer.IsCacheEmpty);
            _mockColorService.Verify(
                c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>()),
                Times.Never);
        }

        [Fact]
        public void GetOverlays_CullsTilesOutsideViewport()
        {
            // Arrange (FR-009)
            var viewport = new Rectangle(0, 0, 40, 40);
            var inside = new Point(5, 6);
            var outsideX = new Point(500, 6);
            var outsideY = new Point(5, 500);
            var tileHealthData = BuildTileData(inside, outsideX, outsideY);

            // Act
            var overlays = _renderer.GetOverlays(viewport, tileHealthData);

            // Assert
            Assert.Single(overlays);
            Assert.Equal(inside, overlays[0].TilePosition);
        }

        [Fact]
        public void GetOverlays_UnchangedViewport_ReturnsCachedOverlays()
        {
            // Arrange (FR-010)
            var viewport = new Rectangle(0, 0, 40, 40);
            var tileHealthData = BuildTileData(new Point(1, 1), new Point(2, 2));
            var equalTileHealthData = BuildTileData(new Point(1, 1), new Point(2, 2));

            // Act
            var first = _renderer.GetOverlays(viewport, tileHealthData);
            var second = _renderer.GetOverlays(viewport, equalTileHealthData);

            // Assert
            Assert.Equal(2, first.Count);
            Assert.Same(first, second);
            Assert.False(_renderer.IsCacheEmpty);
        }

        [Fact]
        public void InvalidateCache_ClearsCachedOverlays()
        {
            // Arrange
            var viewport = new Rectangle(0, 0, 40, 40);
            var tileHealthData = BuildTileData(new Point(3, 3));
            _renderer.GetOverlays(viewport, tileHealthData);
            Assert.False(_renderer.IsCacheEmpty);

            // Act
            _renderer.InvalidateCache();

            // Assert
            Assert.True(_renderer.IsCacheEmpty);
        }

        [Fact]
        public void ForceRefresh_EmptiesCache()
        {
            // Arrange
            var viewport = new Rectangle(0, 0, 40, 40);
            var tileHealthData = BuildTileData(new Point(4, 4));
            _renderer.GetOverlays(viewport, tileHealthData);
            Assert.False(_renderer.IsCacheEmpty);

            // Act
            _renderer.ForceRefresh();

            // Assert
            Assert.True(_renderer.IsCacheEmpty);
        }

        [Fact]
        public void CheckDegradation_AutoModeAboveThreshold_ReturnsTrue()
        {
            // Arrange
            var visibleCount = ModConstants.DegradationTileThreshold + 1;

            // Act
            var result = OverlayRenderer.CheckDegradation(visibleCount, "auto");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CheckDegradation_NeverModeAboveThreshold_ReturnsFalse()
        {
            // Arrange
            var visibleCount = ModConstants.DegradationTileThreshold + 1;

            // Act
            var result = OverlayRenderer.CheckDegradation(visibleCount, "never");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CheckDegradation_AutoModeAtOrBelowThreshold_ReturnsFalse()
        {
            // Arrange
            var atThreshold = ModConstants.DegradationTileThreshold;
            var belowThreshold = ModConstants.DegradationTileThreshold - 1;

            // Act
            var atResult = OverlayRenderer.CheckDegradation(atThreshold, "auto");
            var belowResult = OverlayRenderer.CheckDegradation(belowThreshold, "auto");

            // Assert
            Assert.False(atResult);
            Assert.False(belowResult);
        }

        [Fact]
        public void GetOverlays_AutoModeAboveThreshold_DisablesPatterns()
        {
            // Arrange (FR-009)
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration
            {
                AccessibilityDegradation = "auto",
                ShowPatterns = true
            });

            var tileHealthData = BuildTileDataOfSize(ModConstants.DegradationTileThreshold + 1, out var viewport);

            // Act
            var overlays = _renderer.GetOverlays(viewport, tileHealthData);

            // Assert
            Assert.Equal(ModConstants.DegradationTileThreshold + 1, overlays.Count);
            Assert.All(overlays, overlay => Assert.Equal(PatternType.None, overlay.PatternType));
        }

        [Fact]
        public void GetOverlays_AutoModeBelowThreshold_KeepsPatterns()
        {
            // Arrange (FR-009)
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration
            {
                AccessibilityDegradation = "auto",
                ShowPatterns = true
            });

            var tileHealthData = BuildTileDataOfSize(ModConstants.DegradationTileThreshold - 1, out var viewport);

            // Act
            var overlays = _renderer.GetOverlays(viewport, tileHealthData);

            // Assert
            Assert.Equal(ModConstants.DegradationTileThreshold - 1, overlays.Count);
            Assert.All(overlays, overlay => Assert.Equal(PatternType.Stripes, overlay.PatternType));
        }
    }
}
