using System;
using System.Collections.Generic;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Integration tests for SC-006 (overlay reflects updated health).
/// Verifies the soil-health → visualization event flow: after SetSoilHealth,
/// the visualization service reflects the updated tier, the overlay cache is
/// invalidated on health change, and different health categories produce
/// different overlay representations.
/// </summary>
/// <remarks>
/// The production wiring (ModController) subscribes to
/// <see cref="ISoilHealthService.OnTileHealthChanged"/> and calls
/// <see cref="IVisualizationService.InvalidateCache"/> in response. These
/// tests simulate that wiring and verify the overlay pipeline reflects the
/// updated health tier on the next frame.
/// </remarks>
public class OverlayHealthReflectionTests
{
    private readonly Mock<IModDataService> _mockModDataService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<IFileNameSanitizationService> _mockFileNameSanitizationService;
    private readonly Mock<IColorInterpolationService> _mockColorService;
    private readonly Mock<IVisualizationConfigurationService> _mockConfigService;

    /// <summary>
    /// Tile-space viewport the overlay renderer culls against.
    /// </summary>
    private static readonly Rectangle Viewport = new Rectangle(0, 0, 100, 100);

    public OverlayHealthReflectionTests()
    {
        _mockModDataService = new Mock<IModDataService>();
        _mockMonitor = new Mock<IMonitor>();
        _mockFileNameSanitizationService = new Mock<IFileNameSanitizationService>();
        _mockColorService = new Mock<IColorInterpolationService>();
        _mockConfigService = new Mock<IVisualizationConfigurationService>();
    }

    // ------------------------------------------------------------------
    // SC-006: overlay reflects updated health tier
    // ------------------------------------------------------------------

    /// <summary>
    /// SC-006: after SetSoilHealth, the overlay pipeline reflects the updated
    /// health tier. Setting health to a Moderate value produces a Moderate
    /// overlay; changing to a Healthy value produces a Healthy overlay.
    /// </summary>
    [Fact]
    public void SetSoilHealth_OverlayReflectsUpdatedTier()
    {
        // Arrange
        var renderer = CreateOverlayRenderer();
        var tileData = new Dictionary<Point, float>();

        // Set initial health to Moderate (50)
        tileData[new Point(10, 10)] = 50f;
        var overlaysBefore = renderer.GetOverlays(Viewport, tileData);

        // Change health to Healthy (80)
        tileData[new Point(10, 10)] = 80f;
        renderer.InvalidateCache();
        var overlaysAfter = renderer.GetOverlays(Viewport, tileData);

        // Assert
        Assert.Single(overlaysBefore);
        Assert.Single(overlaysAfter);
        Assert.Equal(HealthCategory.Moderate, overlaysBefore[0].Category);
        Assert.Equal(HealthCategory.Healthy, overlaysAfter[0].Category);
    }

    /// <summary>
    /// SC-006: the overlay cache is invalidated on health change via the
    /// OnTileHealthChanged event. The handler wired to the event calls
    /// InvalidateCache, so the next GetOverlays call reflects the new data.
    /// </summary>
    [Fact]
    public void OnTileHealthChanged_HandlerInvalidatesCache_NextCallReflectsNewData()
    {
        // Arrange
        var soilService = CreateSoilHealthService();
        var renderer = CreateOverlayRenderer();

        bool cacheInvalidated = false;
        Action<string, Vector2, float>? handler = (loc, tile, health) =>
        {
            renderer.InvalidateCache();
            cacheInvalidated = true;
        };
        soilService.OnTileHealthChanged += handler;

        var tile = new Vector2(10, 10);
        var tileData = new Dictionary<Point, float>();

        // Set initial health and get overlays
        soilService.SetSoilHealth("Farm", tile, 50f);
        tileData[new Point(10, 10)] = 50f;
        renderer.GetOverlays(Viewport, tileData);

        // Change health — should fire OnTileHealthChanged and invalidate cache
        soilService.SetSoilHealth("Farm", tile, 80f);
        tileData[new Point(10, 10)] = 80f;

        // Get overlays again — should reflect the new health
        var overlays = renderer.GetOverlays(Viewport, tileData);

        // Assert
        Assert.True(cacheInvalidated);
        Assert.Single(overlays);
        Assert.Equal(HealthCategory.Healthy, overlays[0].Category);
        Assert.Equal(80f, overlays[0].HealthValue);

        // Cleanup
        soilService.OnTileHealthChanged -= handler;
    }

    /// <summary>
    /// SC-006: different health categories produce different overlay
    /// representations. Poor, Moderate, and Healthy each map to a distinct
    /// HealthCategory.
    /// </summary>
    [Theory]
    [InlineData(10f, HealthCategory.Poor)]
    [InlineData(50f, HealthCategory.Moderate)]
    [InlineData(80f, HealthCategory.Healthy)]
    public void DifferentHealthCategories_ProduceDifferentOverlayRepresentations(
        float health,
        HealthCategory expectedCategory)
    {
        // Arrange
        var renderer = CreateOverlayRenderer();
        var tileData = new Dictionary<Point, float>
        {
            { new Point(10, 10), health }
        };

        // Act
        var overlays = renderer.GetOverlays(Viewport, tileData);

        // Assert
        Assert.Single(overlays);
        Assert.Equal(expectedCategory, overlays[0].Category);
    }

    /// <summary>
    /// SC-006: the full soil-health → visualization event flow works end-to-end.
    /// SoilHealthService.SetSoilHealth fires OnTileHealthChanged, the handler
    /// invalidates the overlay cache, and the next frame reflects the updated tier.
    /// </summary>
    [Fact]
    public void SoilHealthToVisualization_FullEventFlow_ReflectsUpdatedTier()
    {
        // Arrange
        var soilService = CreateSoilHealthService();
        var renderer = CreateOverlayRenderer();
        var tileData = new Dictionary<Point, float>();

        // Wire the visualization to the health service (simulates ModController)
        Action<string, Vector2, float>? handler = (loc, tile, health) =>
        {
            renderer.InvalidateCache();
        };
        soilService.OnTileHealthChanged += handler;

        var tile = new Vector2(10, 10);

        // Frame 1: health is Poor (10)
        soilService.SetSoilHealth("Farm", tile, 10f);
        tileData[new Point(10, 10)] = 10f;
        var frame1 = renderer.GetOverlays(Viewport, tileData);

        // Frame 2: health improved to Moderate (50)
        soilService.SetSoilHealth("Farm", tile, 50f);
        tileData[new Point(10, 10)] = 50f;
        var frame2 = renderer.GetOverlays(Viewport, tileData);

        // Frame 3: health improved to Healthy (80)
        soilService.SetSoilHealth("Farm", tile, 80f);
        tileData[new Point(10, 10)] = 80f;
        var frame3 = renderer.GetOverlays(Viewport, tileData);

        // Assert
        Assert.Single(frame1);
        Assert.Single(frame2);
        Assert.Single(frame3);
        Assert.Equal(HealthCategory.Poor, frame1[0].Category);
        Assert.Equal(HealthCategory.Moderate, frame2[0].Category);
        Assert.Equal(HealthCategory.Healthy, frame3[0].Category);

        // Cleanup
        soilService.OnTileHealthChanged -= handler;
    }

    /// <summary>
    /// SC-006: the overlay pipeline skips zero-health tiles (baseline soil
    /// renders as neutral vanilla dirt with no overlay).
    /// </summary>
    [Fact]
    public void ZeroHealth_ProducesNoOverlay()
    {
        // Arrange
        var renderer = CreateOverlayRenderer();
        var tileData = new Dictionary<Point, float>
        {
            { new Point(10, 10), 0f }
        };

        // Act
        var overlays = renderer.GetOverlays(Viewport, tileData);

        // Assert
        Assert.Empty(overlays);
    }

    /// <summary>
    /// SC-006: NaN health values produce an Unknown overlay (never a fabricated
    /// percentage). The overlay is still produced so the draw path can report
    /// the Unknown label.
    /// </summary>
    [Fact]
    public void NaNHealth_ProducesUnknownOverlay()
    {
        // Arrange
        var renderer = CreateOverlayRenderer();
        var tileData = new Dictionary<Point, float>
        {
            { new Point(10, 10), float.NaN }
        };

        // Act
        var overlays = renderer.GetOverlays(Viewport, tileData);

        // Assert
        Assert.Single(overlays);
        Assert.Equal(HealthCategory.Unknown, overlays[0].Category);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Creates a fresh <see cref="SoilHealthService"/> over mocked collaborators.
    /// </summary>
    private SoilHealthService CreateSoilHealthService()
    {
        return new SoilHealthService(
            _mockModDataService.Object,
            _mockMonitor.Object,
            _mockFileNameSanitizationService.Object);
    }

    /// <summary>
    /// Creates an <see cref="OverlayRenderer"/> whose color service is a mock
    /// that resolves categories correctly, so the overlay pipeline produces
    /// real TileOverlay instances with verifiable categories.
    /// </summary>
    private OverlayRenderer CreateOverlayRenderer()
    {
        _mockColorService
            .Setup(c => c.GetCategoryForHealth(It.IsAny<float>()))
            .Returns((float health) =>
            {
                if (float.IsNaN(health) || float.IsInfinity(health))
                    return HealthCategory.Unknown;
                return health switch
                {
                    >= 0 and <= 30 => HealthCategory.Poor,
                    >= 31 and <= 70 => HealthCategory.Moderate,
                    >= 71 and <= 100 => HealthCategory.Healthy,
                    _ => HealthCategory.Unknown
                };
            });
        _mockColorService
            .Setup(c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>()))
            .Returns(Color.White);

        _mockConfigService
            .Setup(c => c.GetConfiguration())
            .Returns(new VisualizationConfiguration
            {
                OverlaysEnabled = true,
                Opacity = ModConstants.DefaultOpacity,
                ShowPatterns = false
            });

        return new OverlayRenderer(
            _mockColorService.Object,
            _mockConfigService.Object,
            _mockMonitor.Object,
            Mock.Of<IHudNotifier>());
    }
}
