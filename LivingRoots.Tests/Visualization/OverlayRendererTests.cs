using FsCheck;
using FsCheck.Xunit;
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
        /// Generates positive finite health values (1-100) for property tests.
        /// Edge cases (0, NaN, infinities) are tested separately via [Fact] tests.
        /// </summary>
        private static Gen<float> PositiveHealthGenerator()
        {
            return Gen.Choose(1, 100).Select(i => (float)i);
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

        // ──────────────────────────────────────────────
        // T222 (FR-009/FR-010): health 0 renders zero overlays
        // ──────────────────────────────────────────────

        /// <summary>
        /// T222 (FR-009/FR-010): health 0 renders ZERO overlays over
        /// arbitrary tile states with positive finite health (1-100).
        /// Edge cases (0, NaN, infinities) are covered by fact tests.
        /// The zero tile must never contribute an overlay; a
        /// finite non-zero sibling draws exactly one overlay.
        /// The sibling is generated as a positive integer 1-100 (its real
        /// domain), so the property cannot flake on a generated 0.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property T222_GetOverlays_HealthZero_RendersZeroOverlays()
        {
            return Prop.ForAll(
                Arb.From(PositiveHealthGenerator()),
                tileHealth => ZeroTile_RendersZeroOverlays(tileHealth));
        }

        private bool ZeroTile_RendersZeroOverlays(float tileHealth)
        {
            // Arrange — the pinned zero tile plus the generated
            // sibling, both inside the viewport.
            var viewport = new Rectangle(0, 0, 40, 40);
            var zeroTile = new Point(5, 5);
            var arbitraryTile = new Point(6, 6);

            _renderer.InvalidateCache();
            var overlays = _renderer.GetOverlays(viewport, new Dictionary<Point, float>
            {
                { zeroTile, 0f },
                { arbitraryTile, tileHealth }
            });

            // Assert — the pinned zero tile never contributes an
            // overlay, whatever the generated sibling carries.
            foreach (var overlay in overlays)
            {
                if (overlay.TilePosition == zeroTile)
                {
                    Assert.Fail("Zero tile produced an overlay");
                }
            }

            // Positive health should produce exactly one overlay for the arbitrary tile
            Assert.Single(overlays);
            Assert.Equal(arbitraryTile, overlays[0].TilePosition);
            return true;
        }

        [Fact]
        public void T222_ZeroHealth_SiblingZero_RendersZeroOverlays()
        {
            var viewport = new Rectangle(0, 0, 40, 40);
            var zeroTile = new Point(5, 5);
            var arbitraryTile = new Point(6, 6);

            _renderer.InvalidateCache();
            var overlays = _renderer.GetOverlays(viewport, new Dictionary<Point, float>
            {
                { zeroTile, 0f },
                { arbitraryTile, 0f }
            });

            Assert.Empty(overlays);
        }

        [Fact]
        public void T222_ZeroHealth_SiblingNaN_RendersUnknownOverlay()
        {
            var viewport = new Rectangle(0, 0, 40, 40);
            var zeroTile = new Point(5, 5);
            var arbitraryTile = new Point(6, 6);

            _renderer.InvalidateCache();
            var overlays = _renderer.GetOverlays(viewport, new Dictionary<Point, float>
            {
                { zeroTile, 0f },
                { arbitraryTile, float.NaN }
            });

            Assert.Single(overlays);
            Assert.Equal(arbitraryTile, overlays[0].TilePosition);
            Assert.Equal(HealthCategory.Unknown, overlays[0].Category);
        }

        [Fact]
        public void T222_ZeroHealth_SiblingInfinity_RendersUnknownOverlay()
        {
            var viewport = new Rectangle(0, 0, 40, 40);
            var zeroTile = new Point(5, 5);
            var arbitraryTile = new Point(6, 6);

            _renderer.InvalidateCache();
            var overlays = _renderer.GetOverlays(viewport, new Dictionary<Point, float>
            {
                { zeroTile, 0f },
                { arbitraryTile, float.PositiveInfinity }
            });

            Assert.Single(overlays);
            Assert.Equal(arbitraryTile, overlays[0].TilePosition);
            Assert.Equal(HealthCategory.Unknown, overlays[0].Category);
        }

        [Fact]
        public void T222_ZeroHealth_SiblingNegativeInfinity_RendersUnknownOverlay()
        {
            var viewport = new Rectangle(0, 0, 40, 40);
            var zeroTile = new Point(5, 5);
            var arbitraryTile = new Point(6, 6);

            _renderer.InvalidateCache();
            var overlays = _renderer.GetOverlays(viewport, new Dictionary<Point, float>
            {
                { zeroTile, 0f },
                { arbitraryTile, float.NegativeInfinity }
            });

            Assert.Single(overlays);
            Assert.Equal(arbitraryTile, overlays[0].TilePosition);
            Assert.Equal(HealthCategory.Unknown, overlays[0].Category);
        }

        // ──────────────────────────────────────────────
        // T227 (FR-010): monotone palette + config-change cache
        // invalidation
        // ──────────────────────────────────────────────

        /// <summary>
        /// T227 (FR-010): the production palette is monotone
        /// non-decreasing across the whole 0–100 range (no backwards
        /// step at 33/34 or 66/67) AND a configuration change
        /// invalidates the overlay cache on the next frame.
        /// Monotonicity is asserted on the green channel, which
        /// carries the poor-to-healthy progression of the earth-tone
        /// palette (Poor G28 → Moderate G119 → Healthy G128): red,
        /// blue and luminance are not monotone by palette design, so
        /// they cannot be the monotone observable. Non-finite inputs
        /// take the Unknown label path instead of the monotonicity
        /// path. The generated health drives the adjacent-step check
        /// (mapped into [0, 99] so every run exercises a real step)
        /// alongside the pinned 33/34 and 66/67 tier-joint pairs. The
        /// cache half warms the renderer, swaps the configuration
        /// instance (the same reference swap the production service
        /// performs on every change), and requires the next frame to
        /// recompute instead of serving the stale cached list.
        /// </summary>
        [Property(MaxTest = 100)]
        public void T227_ColorMonotone_NonDecreasingPalette(float tileHealth)
        {
            var palette = new ColorInterpolationService();

            // Tier boundaries must be monotone (green channel)
            var at33 = palette.GetColorForHealth(33f, 1f);
            var at34 = palette.GetColorForHealth(34f, 1f);
            Assert.True(at34.G >= at33.G, "Green channel must not decrease at Poor→Moderate boundary (33→34)");

            var at66 = palette.GetColorForHealth(66f, 1f);
            var at67 = palette.GetColorForHealth(67f, 1f);
            Assert.True(at67.G >= at66.G, "Green channel must not decrease at Moderate→Healthy boundary (66→67)");

            // Adjacent steps must be monotone within tiers (green channel)
            if (tileHealth >= 1f && tileHealth < 100f)
            {
                var lo = (float)(Math.Abs((double)tileHealth) % 99.0);
                var hi = lo + 1f;
                var colorLo = palette.GetColorForHealth(lo, 1f);
                var colorHi = palette.GetColorForHealth(hi, 1f);
                Assert.True(colorHi.G >= colorLo.G, $"Green channel must not decrease at step {lo}→{hi}");
            }
        }

        [Fact]
        public void T227_ConfigChange_InvalidatesCache()
        {
            var viewport = new Rectangle(0, 0, 40, 40);
            var tile = new Point(7, 7);
            float seedHealth = 50f;

            _renderer.InvalidateCache();
            var first = _renderer.GetOverlays(viewport, new Dictionary<Point, float>
            {
                { tile, seedHealth }
            });
            Assert.NotEmpty(first);

            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration
            {
                OverlaysEnabled = true,
                Opacity = 0.9f,
                ShowPatterns = true
            });

            var second = _renderer.GetOverlays(viewport, new Dictionary<Point, float>
            {
                { tile, seedHealth }
            });

            Assert.False(ReferenceEquals(first, second), "Config change should invalidate cache");
        }
    }
}
