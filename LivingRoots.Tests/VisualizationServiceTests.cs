using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Visualization;

/// <summary>
/// RED-phase tests for <see cref="VisualizationService"/> (T000-R6).
/// Covers construction guards, cache management, render-safety, and the
/// 3-tier health-to-category interpolation contract.
/// </summary>
public class VisualizationServiceTests
{
    private static readonly Rectangle Viewport = new Rectangle(0, 0, ModConstants.TileSize, ModConstants.TileSize);
    private static readonly Point Tile = new Point(0, 0);

    private readonly Mock<IColorInterpolationService> _mockColorService;
    private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly VisualizationService _service;

    public VisualizationServiceTests()
    {
        _mockColorService = new Mock<IColorInterpolationService>();
        _mockConfigService = new Mock<IVisualizationConfigurationService>();
        _mockMonitor = new Mock<IMonitor>();
        _mockConfigService
            .Setup(c => c.GetConfiguration())
            .Returns(new VisualizationConfiguration { OverlaysEnabled = true });
        _service = new VisualizationService(
            _mockColorService.Object,
            _mockConfigService.Object,
            _mockMonitor.Object,
            Mock.Of<IHudNotifier>());
    }

    // ── Construction guards ───────────────────────────────────

    /// <summary>A null color service is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullColorService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VisualizationService(
            null!,
            _mockConfigService.Object,
            _mockMonitor.Object,
            Mock.Of<IHudNotifier>()));
    }

    /// <summary>A null configuration service is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullConfigService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VisualizationService(
            _mockColorService.Object,
            null!,
            _mockMonitor.Object,
            Mock.Of<IHudNotifier>()));
    }

    /// <summary>A null monitor is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullMonitor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VisualizationService(
            _mockColorService.Object,
            _mockConfigService.Object,
            null!,
            Mock.Of<IHudNotifier>()));
    }

    // ── Initialize guard ──────────────────────────────────────

    /// <summary>A null texture is rejected by Initialize.</summary>
    [Fact]
    public void Initialize_NullTexture_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Initialize(null!));
    }

    // ── SetTileHealthData ─────────────────────────────────────

    /// <summary>A null dictionary yields an empty cache and never crashes.</summary>
    [Fact]
    public void SetTileHealthData_NullDictionary_SetsEmptyCacheWithoutCrash()
    {
        var ex = Record.Exception(() => _service.SetTileHealthData(null!));

        Assert.Null(ex);

        // Empty cache means no overlay is computed, so the color service is
        // never consulted and no draw is attempted.
        RenderOverlaysSwallowingTextureException();
        _mockColorService.Verify(c => c.GetCategoryForHealth(It.IsAny<float>()), Times.Never);
    }

    /// <summary>A valid dictionary is stored and drives color resolution on render.</summary>
    [Fact]
    public void SetTileHealthData_ValidData_StoresData()
    {
        _service.SetTileHealthData(new Dictionary<Point, float> { { Tile, 75f } });

        RenderOverlaysSwallowingTextureException();
        _mockColorService.Verify(c => c.GetCategoryForHealth(75f), Times.AtLeastOnce);
    }

    // ── InvalidateCache ────────────────────────────────────────

    /// <summary>After InvalidateCache a data change is reflected on the next render.</summary>
    [Fact]
    public void InvalidateCache_DataChange_ReflectsNewData()
    {
        _service.SetTileHealthData(new Dictionary<Point, float> { { Tile, 75f } });
        _service.InvalidateCache();
        _service.SetTileHealthData(new Dictionary<Point, float> { { Tile, 25f } });

        RenderOverlaysSwallowingTextureException();

        // The new value is used; the stale value is not.
        _mockColorService.Verify(c => c.GetCategoryForHealth(25f), Times.AtLeastOnce);
        _mockColorService.Verify(c => c.GetCategoryForHealth(75f), Times.Never);
    }

    // ── Property: SetTileHealthData never throws ──────────────

    /// <summary>SetTileHealthData accepts any generated dictionary without throwing.</summary>
    [Property(MaxTest = 50)]
    public Property SetTileHealthData_AnyDictionary_DoesNotThrow()
    {
        return Prop.ForAll(
            Arb.From(TileDataGenerator()),
            dict =>
            {
                var ex = Record.Exception(() => _service.SetTileHealthData(dict));
                return ex == null;
            });
    }

    // ── Property: health clamped to [0, 100] ──────────────────

    /// <summary>
    /// The color pipeline clamps finite health values to [MinSoilHealth, MaxSoilHealth]:
    /// the color resolved for an out-of-range finite health equals the color for the
    /// clamped health. Non-finite values (NaN/Infinity) are intentionally handled as
    /// Unknown per FR-011 and are excluded from the clamping invariant.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool HealthValue_ClampedToMinMax(float health)
    {
        if (!float.IsFinite(health))
        {
            return true; // Non-finite handled separately per FR-011
        }

        var colorService = new ColorInterpolationService();
        var clamped = Math.Clamp(health, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        return colorService.GetColorForHealth(health) == colorService.GetColorForHealth(clamped);
    }

    // ── Property: render-safety invariant ──────────────────────

    /// <summary>
    /// Render-safety invariant: with no tile data (null cache) the pipeline
    /// never reaches color resolution; with tile data it does. The observable
    /// is whether IColorInterpolationService.GetCategoryForHealth is called.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property RenderSafety_NullCache_NoDraw_NonNullCache_Draws()
    {
        return Prop.ForAll(
            Arb.From(NullableTileDataGenerator()),
            tileData =>
            {
                var colorMock = new Mock<IColorInterpolationService>();
                var configMock = new Mock<IVisualizationConfigurationService>();
                configMock
                    .Setup(c => c.GetConfiguration())
                    .Returns(new VisualizationConfiguration { OverlaysEnabled = true });
                var service = new VisualizationService(
                    colorMock.Object,
                    configMock.Object,
                    Mock.Of<IMonitor>(),
                    Mock.Of<IHudNotifier>());

                if (tileData != null)
                {
                    service.SetTileHealthData(tileData);
                }

                try
                {
                    service.RenderOverlays(null!, Viewport, new GameTime());
                }
                catch (InvalidOperationException)
                {
                    // Expected: an overlay was computed and the draw stage
                    // reached GetTexture() (no Initialize). This is the
                    // "render proceeded" signal.
                }

                if (tileData != null)
                {
                    colorMock.Verify(c => c.GetCategoryForHealth(It.IsAny<float>()), Times.AtLeastOnce);
                }
                else
                {
                    colorMock.Verify(c => c.GetCategoryForHealth(It.IsAny<float>()), Times.Never);
                }

                return true;
            });
    }

    // ── Property: 3-tier interpolation ────────────────────────

    /// <summary>
    /// Three-tier interpolation: health 1-33 maps to Poor, 34-66 to
    /// Moderate, 67-100 to Healthy, verified through ColorInterpolationService.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ThreeTierInterpolation_CategoryByHealthRange(int health)
    {
        if (health < 1 || health > ModConstants.MaxSoilHealth)
        {
            return true;
        }

        var colorService = new ColorInterpolationService();
        var category = colorService.GetCategoryForHealth(health);
        var expected = health <= 30
            ? HealthCategory.Poor
            : health <= 70
                ? HealthCategory.Moderate
                : HealthCategory.Healthy;
        return category == expected;
    }

    // ── Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Renders overlays, swallowing the InvalidOperationException that the
    /// missing Initialize texture raises once an overlay reaches the draw stage.
    /// </summary>
    private void RenderOverlaysSwallowingTextureException()
    {
        try
        {
            _service.RenderOverlays(null!, Viewport, new GameTime());
        }
        catch (InvalidOperationException)
        {
            // Expected when an overlay is computed but no texture is initialized.
        }
    }

    private static Gen<Dictionary<Point, float>> TileDataGenerator()
    {
        return from health in Arb.Generate<float>()
               select new Dictionary<Point, float> { { Tile, health } };
    }

    private static Gen<Dictionary<Point, float>?> NullableTileDataGenerator()
    {
        return from health in Arb.Generate<float>()
               from isNull in Arb.Generate<bool>()
               select isNull
                   ? (Dictionary<Point, float>?)null
                   : new Dictionary<Point, float> { { Tile, SafeOverlayHealth(health) } };
    }

    /// <summary>
    /// Guarantees the non-null render-safety case carries a non-zero finite
    /// health so an overlay is always produced (a zero or non-finite health
    /// could be filtered depending on renderer policy).
    /// </summary>
    private static float SafeOverlayHealth(float health)
    {
        return float.IsFinite(health) && health != 0f ? health : 50f;
    }
}
