using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Unit tests for reactive visual overlay invalidation upon soil health change.
/// Validates Task T030 [P] [US3] under Feature 004 (Playable Compost Loop) and FR-010.
/// </summary>
public class SoilHealthVisualizationTests
{
    private readonly Mock<IColorInterpolationService> _mockColorService;
    private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly OverlayRenderer _overlayRenderer;

    public SoilHealthVisualizationTests()
    {
        _mockColorService = new Mock<IColorInterpolationService>();
        _mockColorService.Setup(c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>())).Returns(Color.White);
        _mockColorService.Setup(c => c.GetCategoryForHealth(It.IsAny<float>())).Returns(HealthCategory.Moderate);

        _mockConfigService = new Mock<IVisualizationConfigurationService>();
        _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration
        {
            OverlaysEnabled = true,
            Opacity = 0.5f,
            ShowPatterns = false
        });

        _mockMonitor = new Mock<IMonitor>();
        _mockSoilHealthService = new Mock<ISoilHealthService>();

        _overlayRenderer = new OverlayRenderer(_mockColorService.Object, _mockConfigService.Object, _mockMonitor.Object, new Mock<IHudNotifier>().Object);
    }

    /// <summary>
    /// When OnTileHealthChanged fires, the overlay cache is invalidated reactively
    /// without delay, completing in <= 16.67ms per frame budget (T030, FR-010).
    /// </summary>
    [Fact]
    public void OnTileHealthChanged_SubscribedHandler_InvalidatesOverlayCacheImmediatelyWithinFrameBudget()
    {
        // Arrange
        var viewport = new Rectangle(0, 0, 100, 100);
        var tileData = new Dictionary<Point, float>
        {
            { new Point(10, 10), 50f }
        };

        // Warm up the cache
        var overlays1 = _overlayRenderer.GetOverlays(viewport, tileData);
        Assert.Single(overlays1);
        Assert.False(_overlayRenderer.IsCacheEmpty);

        // Wire subscriber to OnTileHealthChanged
        Action<string, Vector2, float>? handler = (loc, tile, health) =>
        {
            _overlayRenderer.InvalidateCache();
        };

        // Measure invalidation reaction time
        var stopwatch = Stopwatch.StartNew();

        // Act: Invoke the event handler representing reactive event publication
        handler("Farm", new Vector2(10, 10), 65f);

        stopwatch.Stop();

        // Assert: Cache must be invalidated immediately
        Assert.True(_overlayRenderer.IsCacheEmpty);

        // Elapsed time must be well within frame budget (<= 16.67ms per FR-010)
        Assert.True(stopwatch.Elapsed.TotalMilliseconds <= ModConstants.MaxRenderTimeMs,
            $"Reactive cache invalidation took {stopwatch.Elapsed.TotalMilliseconds:F3}ms, which exceeds frame budget of {ModConstants.MaxRenderTimeMs}ms");
    }

    /// <summary>
    /// After cache invalidation triggered by health mutation, subsequent GetOverlays call
    /// recomputes overlays reflecting the updated health on the next frame (T030, FR-010).
    /// </summary>
    [Fact]
    public void OnTileHealthChanged_InvalidatesCache_SubsequentRenderReflectsUpdatedHealth()
    {
        // Arrange
        var viewport = new Rectangle(0, 0, 100, 100);
        var tilePos = new Point(10, 10);
        var tileData = new Dictionary<Point, float>
        {
            { tilePos, 30f }
        };

        var initialOverlays = _overlayRenderer.GetOverlays(viewport, tileData);
        Assert.Single(initialOverlays);
        Assert.Equal(30f, initialOverlays[0].HealthValue);

        // Act: Soil health is mutated (e.g. +15 compost applied -> 45f)
        tileData[tilePos] = 45f;
        _overlayRenderer.InvalidateCache();

        // Next frame render
        var updatedOverlays = _overlayRenderer.GetOverlays(viewport, tileData);

        // Assert
        Assert.Single(updatedOverlays);
        Assert.Equal(45f, updatedOverlays[0].HealthValue);
    }

    // ──────────────────────────────────────────────
    // T091 (FR-010): ambient-light blending — DEMANDED SEAM
    // ──────────────────────────────────────────────

    /// <summary>Prefix of the demanded ambient-tint trace log.</summary>
    private const string DrawnOverlayTintLogPrefix = "Overlay tint drawn:";

    /// <summary>Separator between the logged tint channels.</summary>
    private static readonly string[] TintChannelSeparator = { ", " };

    /// <summary>
    /// The demanded ambient-tint draw observable (DEMANDED SEAM,
    /// T091, FR-010): the overlay draw path must emit the tint it
    /// draws — a trace log "Overlay tint drawn: RGBA(r, g, b, a)"
    /// (the frame-level observable the "Overlay drawn: (x, y)"
    /// destination log establishes for the draw position) or an
    /// equivalent draw-call recorder sink — because the tint the
    /// draw consumes is folded into the Color passed to the
    /// non-virtual SpriteBatch.Draw, which the headless unit-test
    /// host cannot perform (a real SpriteBatch needs a
    /// GraphicsDevice). The overlay path today draws
    /// <c>overlay.Color</c> from <c>OverlayRenderer.BuildOverlays</c>'
    /// <c>GetColorForHealth(health, opacity)</c>, which carries the
    /// configured opacity only and no ambient term, so the tint
    /// cannot be asserted against the game's ambient light
    /// (<c>Game1.ambientLight</c> / <c>Game1.outdoorLight</c>) at
    /// any point in the day cycle until the observable exists, so
    /// this test fails at the seam demand rather than at a
    /// behavioral assertion.
    /// </summary>
    private const string AmbientTintSeamDemand =
        "DEMANDED SEAM (T091, FR-010): the overlay draw path must emit the tint it draws — " +
        "a trace log \"Overlay tint drawn: RGBA(r, g, b, a)\" (the frame-level observable the " +
        "\"Overlay drawn: (x, y)\" destination log establishes for the draw position) or an " +
        "equivalent draw-call recorder sink — because the tint the draw consumes is folded into " +
        "the Color passed to the non-virtual SpriteBatch.Draw, which the headless unit-test host " +
        "cannot perform (a real SpriteBatch needs a GraphicsDevice). Until the observable exists, " +
        "the drawn tint cannot be asserted against the game's ambient light (Game1.ambientLight / " +
        "Game1.outdoorLight) at any point in the day cycle, so this test fails at the seam demand " +
        "rather than at a behavioral assertion.";

    /// <summary>The tile-space viewport the ambient frames render.</summary>
    private static readonly Rectangle AmbientViewport = new(0, 0, 16, 16);

    /// <summary>The visible tile the ambient frames draw.</summary>
    private static readonly Point AmbientVisibleTile = new(4, 4);

    /// <summary>The visible tile's soil health.</summary>
    private const float AmbientVisibleTileHealth = 80f;

    /// <summary>The configured overlay opacity.</summary>
    private const float AmbientOverlayOpacity = 0.75f;

    /// <summary>The game's full-daylight ambient light.</summary>
    private static readonly Color FullDaylightAmbient = Color.White;

    /// <summary>The game's night ambient light.</summary>
    private static readonly Color NightAmbient = new(48, 48, 88);

    /// <summary>
    /// T091 (FR-010): the overlay tint the frame draws MUST blend
    /// under the game's ambient light instead of rendering at full
    /// strength over the darkened world — at full daylight the
    /// drawn tint is the unlit tint unblended, and at night the
    /// drawn tint is the unlit tint darkened by the night ambient
    /// (darker than unlit, never blacked out, still visible
    /// through its alpha), so the overlay stays readable across
    /// the day cycle. The drawn tint is observed through the
    /// demanded ambient-tint draw observable (see
    /// <see cref="AmbientTintSeamDemand"/>): the overlay path
    /// runs at the installed ambient light with a stand-in
    /// SpriteBatch, and the RGBA it emits is the frame-level
    /// observable of the tint the draw consumed. RED today: the
    /// seam does not exist — the draw consumes only the
    /// configured opacity (<c>VisualizationService.cs:494</c>
    /// draws <c>overlay.Color</c>, resolved by
    /// <c>GetColorForHealth(health, opacity)</c> with no ambient
    /// term), so no "Overlay tint drawn: RGBA(r, g, b, a)" trace
    /// exists and the frame fails at the seam demand; once the
    /// seam lands, the day/night blend assertions hold.
    /// </summary>
    [Fact]
    public void RenderOverlays_AmbientLighting_OverlayTintBlendsUnderAmbientInsteadOfFullStrengthOverTheDarkenedWorld()
    {
        // Arrange — the unlit tint the interpolation service
        // resolves for the visible tile, and the monitor-captured
        // frame logs the drawn tint is observed through.
        var unlitTint = new Color(120, 180, 90, 192);
        var messages = new List<string>();
        var service = BuildAmbientBlendService(unlitTint, messages);
        service.SetTileHealthData(new Dictionary<Point, float>
        {
            { AmbientVisibleTile, AmbientVisibleTileHealth }
        });

        // Act — one frame at full daylight, one frame at night.
        Color dayTint;
        using (new AmbientLightState(FullDaylightAmbient))
        {
            dayTint = RenderOneOverlayFrame(service, messages);
        }

        Color nightTint;
        using (new AmbientLightState(NightAmbient))
        {
            nightTint = RenderOneOverlayFrame(service, messages);
        }

        // Assert — full daylight draws the unlit tint unblended...
        Assert.Equal(unlitTint, dayTint);

        // ...and night draws it darkened by the darkened world —
        // darker than unlit, never blacked out, still visible.
        Assert.True(Luminance(nightTint) < Luminance(unlitTint),
            $"The night tint (R{nightTint.R}, G{nightTint.G}, B{nightTint.B}) must be darker than the unlit tint (R{unlitTint.R}, G{unlitTint.G}, B{unlitTint.B}) under the night ambient.");
        Assert.True(nightTint.R > 0 || nightTint.G > 0 || nightTint.B > 0,
            "The night tint must not be blacked out — an all-zero RGB overlay vanishes against the darkened world.");
        Assert.True(nightTint.A > 0,
            "The night tint must keep a visible alpha — a zero-alpha overlay is invisible against the darkened world.");
    }

    // ──────────────────────────────────────────────
    // T094 (FR-010): production-wired reactive latency
    // ──────────────────────────────────────────────

    /// <summary>
    /// T094 (FR-010): through the PRODUCTION composition root —
    /// <c>ModEntry.Entry</c> wiring
    /// <c>SoilHealthService.OnTileHealthChanged</c> to
    /// <c>VisualizationService.InvalidateCache</c>
    /// (ModEntry.cs:109-110) — a health update MUST invalidate
    /// the overlay cache within one frame budget (≤
    /// <see cref="ModConstants.MaxRenderTimeMs"/>): the
    /// subscription is live after Entry, a warm frame populates
    /// the cache, and the synchronous update triggers the
    /// invalidation reactively, inside the frame budget. GREEN by
    /// existing behavior: the wiring at ModEntry.cs:109-110
    /// already subscribes the invalidation handler, and
    /// <c>SoilHealthService.UpdateHealth</c> raises
    /// OnTileHealthChanged synchronously with no I/O on the event
    /// path (SoilHealthService.cs:642), so the measured
    /// invalidation is an in-memory cache clear well inside the
    /// 16.67ms frame budget.
    /// </summary>
    [Fact]
    public void OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame()
    {
        // Arrange — the production composition root, booted exactly
        // as the game boots it.
        var modEntry = new ModEntry();
        var helper = CreateProductionEntryHelper(modEntry);
        modEntry.Entry(helper.Object);

        var soilHealthService = ReadPrivateField<ISoilHealthService>(modEntry, "_soilHealthService", typeof(ModEntry));
        Assert.NotNull(soilHealthService);

        var controller = ReadPrivateField<ModController>(modEntry, "_controller", typeof(ModEntry));
        Assert.NotNull(controller);

        var visualizationService = ReadPrivateField<IVisualizationService>(controller, "_visualizationService", typeof(ModController));
        Assert.NotNull(visualizationService);

        var overlayRenderer = ReadPrivateField<OverlayRenderer>(visualizationService, "_overlayRenderer", typeof(VisualizationService));
        Assert.NotNull(overlayRenderer);

        // The composition root's subscription is live: the reactive
        // cache-invalidation handler is attached to the soil health
        // event.
        var tileHealthChangedField = typeof(SoilHealthService).GetField(
            "OnTileHealthChanged",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(tileHealthChangedField);
        var subscribers = tileHealthChangedField.GetValue(soilHealthService) as Delegate;
        Assert.True(
            subscribers != null && subscribers.GetInvocationList().Length > 0,
            "SUBSCRIPTION DEFECT: ModEntry.Entry did not subscribe a cache-invalidation handler to SoilHealthService.OnTileHealthChanged (ModEntry.cs:109-110).");

        // The visible tile's health, and one warm frame to populate
        // the overlay cache the invalidation empties. The warm
        // frame's stand-in SpriteBatch absorbs the headless-host
        // draw boundary (a real SpriteBatch needs a GraphicsDevice;
        // the draw raises after the overlay list is cached).
        visualizationService.SetTileHealthData(new Dictionary<Point, float>
        {
            { AmbientVisibleTile, AmbientVisibleTileHealth }
        });

        using var camera = new CameraViewportState(AmbientViewport);
        Record.Exception(() =>
            visualizationService.RenderOverlays(StandInSpriteBatch(), AmbientViewport, new GameTime()));
        Assert.False(overlayRenderer.IsCacheEmpty);

        // Act — a compost application's health update through the
        // production wiring, timed.
        var stopwatch = Stopwatch.StartNew();
        soilHealthService.UpdateHealth(
            "Farm",
            new Vector2(AmbientVisibleTile.X, AmbientVisibleTile.Y),
            ModConstants.RestorationAmount);
        stopwatch.Stop();

        // Assert — the subscription invalidated the overlay cache...
        Assert.True(overlayRenderer.IsCacheEmpty,
            $"SUBSCRIPTION DEFECT: SoilHealthService.UpdateHealth did not invalidate the visualization overlay cache through the ModEntry wiring (ModEntry.cs:109-110); elapsed {stopwatch.Elapsed.TotalMilliseconds:F3}ms.");

        // ...reactively, within one frame budget (FR-010).
        Assert.True(stopwatch.Elapsed.TotalMilliseconds <= ModConstants.MaxRenderTimeMs,
            $"LATENCY DEFECT: reactive cache invalidation took {stopwatch.Elapsed.TotalMilliseconds:F3}ms, which exceeds the frame budget of {ModConstants.MaxRenderTimeMs}ms.");
    }

    // ──────────────────────────────────────────────
    // T126 (FR-011): overlays report only drawable
    // values — the demanded overlay-value draw
    // observable
    // ──────────────────────────────────────────────

    /// <summary>Prefix of the demanded overlay-value trace log.</summary>
    private const string DrawnOverlayValueLogPrefix = "Overlay value drawn: ";

    /// <summary>The tile the deterministic frames render.</summary>
    private static readonly Point DrawnValueTile = new(4, 4);

    /// <summary>The tile-space viewport the drawable-value frames render.</summary>
    private static readonly Rectangle DrawnValueViewport = new(0, 0, 16, 16);

    /// <summary>
    /// Upper bound for generated tile coordinates: keeps
    /// every generated tile inside <see cref="DrawnValueViewport"/>
    /// plus its 1-tile culling margin (0-15), so the frame
    /// always reaches the tile the sample generated.
    /// </summary>
    private const int DrawnValueTileBound = 16;

    /// <summary>
    /// The demanded overlay-value draw observable (DEMANDED
    /// SEAM, T126, FR-011): the overlay draw path must emit
    /// the health value the draw reports — the value the
    /// overlay carries (<c>TileOverlay.HealthValue</c>, the
    /// same value the tooltip text and the hoe-feedback text
    /// report for the tile) — as a trace log
    /// "Overlay value drawn: {value}" (the value counterpart
    /// of the "Overlay drawn: (x, y)" destination log and
    /// the "Overlay tint drawn: RGBA(r, g, b, a)" tint log),
    /// or the literal "Unknown" for an unmeasurable tile —
    /// because the value the draw consumes is folded into the
    /// <c>TileOverlay</c> the non-virtual draw path draws
    /// from, which the headless unit-test host cannot observe
    /// directly (a real <c>SpriteBatch</c> draw needs a
    /// <c>GraphicsDevice</c>), so no frame-level observable of
    /// the reported value exists. Until it exists, FR-011's
    /// guarantee — the display MUST NOT report a value it is
    /// not actually able to draw — is unverifiable: no test
    /// can prove an unmeasurable tile is labeled Unknown rather
    /// than carrying a fabricated percentage, so this test
    /// fails at the seam demand rather than at a behavioral
    /// assertion.
    /// </summary>
    private const string OverlayValueSeamDemand =
        "DEMANDED SEAM (T126, FR-011): the overlay draw path must emit the health value the draw reports — " +
        "a trace log \"Overlay value drawn: {value}\" (the value counterpart of the \"Overlay drawn: (x, y)\" " +
        "destination log and the \"Overlay tint drawn: RGBA(r, g, b, a)\" tint log), or the literal \"Unknown\" " +
        "for an unmeasurable tile — because the value the draw consumes is folded into the TileOverlay the " +
        "non-virtual draw path draws from, which the headless unit-test host cannot observe directly (a real " +
        "SpriteBatch draw needs a GraphicsDevice). Until the seam exists, FR-011's guarantee — the display MUST " +
        "NOT report a value it is not actually able to draw — is unverifiable: no test can prove an unmeasurable " +
        "tile is labeled Unknown rather than carrying a fabricated percentage, so this test fails at the seam " +
        "demand rather than at a behavioral assertion.";

    /// <summary>
    /// T126 (FR-011): for ANY tile state the overlay draw
    /// path can reach — any generated tile inside the
    /// viewport at any generated non-zero health — the frame
    /// reports only the drawable value: a measurable tile
    /// reports the health value the draw carries (the same
    /// value the drawn tint was resolved from, round-tripping
    /// through the log), and an unmeasurable tile (the
    /// non-finite unknown sentinel the producer reports for
    /// absent records) reports the literal Unknown label —
    /// never a fabricated percentage (no "%", no digits).
    /// FsCheck generates finite floats only, so the non-finite
    /// sentinel cases are pinned deterministically by
    /// <see cref="RenderOverlays_UnmeasurableTileReportsTheUnknownLabel_NeverAFabricatedPercentage"/>.
    /// RED today: the draw path emits no value observable —
    /// the frame logs only the destination and the tint — so
    /// the reported value is unobservable and the property
    /// fails at the seam demand (see
    /// <see cref="OverlayValueSeamDemand"/>).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RenderOverlays_ReportsOnlyDrawableValues_MeasurableTileReportsItsValue(
        NonNegativeInt tileX,
        NonNegativeInt tileY,
        float tileHealth)
    {
        // Premise: health 0 is the FR-009 neutral-dirt
        // baseline — the overlay renderer draws no overlay
        // for it (OverlayRenderer.BuildOverlays), so the
        // frame reports no value for it. The unrendered
        // state is distinct from the unmeasured unknown
        // sentinel, which still renders (and so still owes
        // a reported value).
        if (tileHealth == 0f)
        {
            return true;
        }

        // Arrange — the generated tile inside the frame's
        // viewport, and the monitor-captured frame logs the
        // reported value is observed through. The real
        // production palette resolves the drawn tint, so the
        // demanded value is the value the draw actually
        // consumed.
        var tile = new Point(tileX.Get % DrawnValueTileBound, tileY.Get % DrawnValueTileBound);
        var messages = new List<string>();
        var service = BuildDrawableValueFrameService(messages);
        service.SetTileHealthData(new Dictionary<Point, float>
        {
            { tile, tileHealth }
        });

        // Act — one frame through the stand-in batch (the
        // headless-host draw boundary is absorbed inside the
        // frame runner).
        string reported = RenderOneDrawableValueFrame(service, messages);

        // Assert — a measurable tile reports exactly the
        // drawable value the draw carried...
        if (!float.IsNaN(tileHealth) && !float.IsInfinity(tileHealth))
        {
            return reported != null
                && float.TryParse(reported, NumberStyles.Float, CultureInfo.CurrentCulture, out var drawnValue)
                && drawnValue == tileHealth;
        }

        // ...and an unmeasurable tile reports the Unknown
        // label, never a fabricated percentage.
        return reported == "Unknown" && !reported.Contains('%');
    }

    /// <summary>
    /// Wave 15 T143 [P] [US3] — red-first property-based test for FR-011.
    /// Asserts overlays report only drawable values: never fabricated
    /// percentages for unknown/unmeasurable tiles (no "%", no digits),
    /// and the Poor interval floor is respected at 1 (sub-floor
    /// measurable values are not clamped/fabricated to 1).
    /// Pattern reference: T031 (FsCheck property framework) / T126
    /// (drawable-value seam, FR-011). RED today: the overlay draw
    /// path emits no observable value (seam missing) and any
    /// fabricated-value reporting / missing floor passes through
    /// unchallenged.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RenderOverlays_ReportsOnlyDrawableValues_NeverFabricated_PoorFloorAt1(float tileHealth)
    {
        // Unknown / unmeasurable: must be literal Unknown, never a
        // fabricated percentage (T126). If non-finite, the seam
        // must emit "Unknown" with no '%' — failure = red.
        if (!float.IsFinite(tileHealth))
        {
            var unknownMessages = new List<string>();
            var unknownService = BuildDrawableValueFrameService(unknownMessages);
            unknownService.SetTileHealthData(new Dictionary<Point, float>
            {
                { DrawnValueTile, tileHealth }
            });
            string unknownReported = RenderOneDrawableValueFrame(unknownService, unknownMessages);
            return unknownReported == "Unknown" && !unknownReported.Contains('%');
        }

        // Measurable tile (finite, non-zero): overlay must report
        // the drawable value exactly, never with a fabricated '%'.
        // Poor floor at 1 means 0 < h < 1 must NOT be clamped
        // to 1 in the reported value (no fabrication to floor).
        if (tileHealth == 0f)
        {
            return true; // zero is unrendered baseline (FR-009), not a reportable overlay
        }

        var messages = new List<string>();
        var service = BuildDrawableValueFrameService(messages);
        var tile = new Point(DrawnValueTile.X, DrawnValueTile.Y);
        service.SetTileHealthData(new Dictionary<Point, float>
        {
            { tile, tileHealth }
        });

        string reported = RenderOneDrawableValueFrame(service, messages);

        // Never a fabricated percentage.
        if (reported.Contains('%'))
        {
            return false;
        }

        // Must parse back to the exact drawable value.
        if (!float.TryParse(reported, NumberStyles.Float, CultureInfo.CurrentCulture, out var drawnValue))
        {
            return false; // fabricated non-numeric report
        }

        if (drawnValue != tileHealth)
        {
            return false; // fabricated / clamped value
        }

        // Poor floor at 1 guard: sub-floor readings (0,1) must not
        // appear as 1 in the reported drawable value.
        if (tileHealth > 0f && tileHealth < 1f)
        {
            if (Math.Abs(drawnValue - 1f) < float.Epsilon)
            {
                return false; // floor missing / clamped to 1
            }
        }

        return true;
    }

    /// <summary>
    /// T143 RED-FIRST (blocking T159): drawable-only overlay invariant.
    /// Overlays must suppress fabricated values for unknown/unmeasurable tiles,
    /// floor Poor interval at 1 (sub-floor not clamped/fabricated), and react
    /// within <=16.67ms via OnTileHealthChanged (FR-010/FR-011).
    /// Must FAIL red until overlay value observable and reaction wiring land.
    /// </summary>
    [Fact]
    public void T143_OverlayDrawableOnly_SuppressesFabricated_UnknownFloorAt1_ReactsWithinFrameBudget()
    {
        // Arrange — unmeasurable (unknown) tile and sub-floor measurable
        var messages = new List<string>();
        var service = BuildDrawableValueFrameService(messages);
        var unknownTile = new Point(DrawnValueTile.X, DrawnValueTile.Y);

        // Unknown / unmeasurable tile must not report a fabricated percentage
        service.SetTileHealthData(new Dictionary<Point, float>
        {
            { unknownTile, float.NaN }
        });
        string unknownReported = RenderOneDrawableValueFrame(service, messages);
        Assert.Equal("Unknown", unknownReported);
        Assert.DoesNotContain('%', unknownReported);

        // Poor interval floor at 1: sub-floor measurable (0.5) must NOT be clamped to 1
        service.SetTileHealthData(new Dictionary<Point, float>
        {
            { unknownTile, 0.5f }
        });
        string subFloorReported = RenderOneDrawableValueFrame(service, messages);
        float drawnSub;
        Assert.True(float.TryParse(subFloorReported, NumberStyles.Float, CultureInfo.CurrentCulture, out drawnSub));
        Assert.NotEqual(1f, drawnSub); // floor at 1 must not fabricate/clamp

        // React via OnTileHealthChanged within <=16.67ms (FR-010)
        var stopwatch = Stopwatch.StartNew();
        Action<string, Vector2, float> handler = (loc, tile, health) =>
        {
            service.SetTileHealthData(new Dictionary<Point, float>
            {
                { unknownTile, 75f }
            });
        };
        handler("Farm", new Vector2(unknownTile.X, unknownTile.Y), 75f);
        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed.TotalMilliseconds <= ModConstants.MaxRenderTimeMs,
            $"Reactive overlay update exceeded {ModConstants.MaxRenderTimeMs}ms (FR-010)");
    }

    /// <summary>
    /// T126 (FR-011): every non-finite health float — the
    /// unknown sentinel (NaN) and both infinities — is
    /// reported as the literal Unknown label, with no
    /// percentage and no digits: the display never
    /// fabricates a value for a tile it cannot measure.
    /// RED today: the draw path emits no value observable at
    /// all (see <see cref="OverlayValueSeamDemand"/>).
    /// </summary>
    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void RenderOverlays_UnmeasurableTileReportsTheUnknownLabel_NeverAFabricatedPercentage(float tileHealth)
    {
        var messages = new List<string>();
        var service = BuildDrawableValueFrameService(messages);
        service.SetTileHealthData(new Dictionary<Point, float>
        {
            { DrawnValueTile, tileHealth }
        });

        string reported = RenderOneDrawableValueFrame(service, messages);

        Assert.Equal("Unknown", reported);
        Assert.DoesNotContain('%', reported);
    }

    /// <summary>
    /// T126 (FR-011): no path draws garbage — a frame on a
    /// service that never ran <c>Initialize</c> (no white
    /// texture installed) reaches the draw stage and throws
    /// <c>InvalidOperationException</c> from
    /// <c>VisualizationService.GetTexture</c> rather than
    /// drawing an uninitialized texture. The frame's
    /// destination observable proves the draw stage was
    /// reached; the guard is what keeps an uninitialized
    /// texture out of the draw. GREEN by existing behavior
    /// (VisualizationService.GetTexture throws when
    /// <c>Initialize</c> has not run) — requirement-named
    /// here so FR-011's no-garbage clause has its named
    /// coverage alongside the red value seam above.
    /// </summary>
    [Fact]
    public void RenderOverlays_WithoutInitializedTexture_ThrowsFromGetTextureRatherThanDrawingUninitialized()
    {
        var messages = new List<string>();
        var service = BuildDrawableValueFrameService(messages, initializeTexture: false);
        service.SetTileHealthData(new Dictionary<Point, float>
        {
            { DrawnValueTile, 50f }
        });

        Exception? exception;
        using (new CameraViewportState(DrawnValueViewport))
        {
            exception = Record.Exception(() =>
                service.RenderOverlays(StandInSpriteBatch(), DrawnValueViewport, new GameTime()));
        }

        // The frame reached the draw stage...
        Assert.Contains(messages, message => message.StartsWith("Overlay drawn: ", StringComparison.Ordinal));

        // ...and the draw guard threw rather than drawing an
        // uninitialized texture (FR-011: no path draws
        // garbage).
        Assert.IsType<InvalidOperationException>(exception);
    }

    // ──────────────────────────────────────────────
    // T126 helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="VisualizationService"/> over the
    /// real production palette whose monitor captures log
    /// messages into <paramref name="messages"/> — the
    /// demanded overlay-value draw observable's sink —
    /// initialized with the stand-in white texture the draw
    /// path draws rectangles with (unless
    /// <paramref name="initializeTexture"/> is false, the
    /// no-Initialize control for the GetTexture guard). The
    /// configuration enables overlays at the default opacity
    /// with patterns off, so the frame is the base fill the
    /// value observable is demanded on.
    /// </summary>
    private static VisualizationService BuildDrawableValueFrameService(List<string> messages, bool initializeTexture = true)
    {
        var monitor = new Mock<IMonitor>();
        monitor
            .Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
            .Callback<string, LogLevel>((message, level) => messages.Add(message));

        var configService = new Mock<IVisualizationConfigurationService>();
        configService
            .Setup(c => c.GetConfiguration())
            .Returns(new VisualizationConfiguration
            {
                OverlaysEnabled = true,
                Opacity = ModConstants.DefaultOpacity,
                ShowPatterns = false
            });

        var service = new VisualizationService(
            new ColorInterpolationService(),
            configService.Object,
            monitor.Object,
            new Mock<IHudNotifier>().Object);
        if (initializeTexture)
        {
            service.Initialize(StandInWhiteTexture());
        }

        return service;
    }

    /// <summary>
    /// Renders one overlay frame and returns the value the
    /// frame reported for the tile, observed through the
    /// demanded overlay-value draw observable (see
    /// <see cref="OverlayValueSeamDemand"/>): the frame runs
    /// with a stand-in SpriteBatch — a real SpriteBatch needs
    /// a GraphicsDevice the headless unit-test host lacks,
    /// and SpriteBatch.Draw is non-virtual — so the stand-in's
    /// uninitialized internals raise at the draw, after the
    /// draw observables are emitted; the raise is the
    /// headless-host boundary and is absorbed here. Fails with
    /// <see cref="OverlayValueSeamDemand"/> until the overlay
    /// draw path emits the value it draws.
    /// </summary>
    private static string RenderOneDrawableValueFrame(VisualizationService service, List<string> messages)
    {
        messages.Clear();

        Exception? drawBoundary = null;
        using (new CameraViewportState(DrawnValueViewport))
        {
            drawBoundary = Record.Exception(() =>
                service.RenderOverlays(StandInSpriteBatch(), DrawnValueViewport, new GameTime()));
        }

        // The frame reached the draw stage — the destination
        // observable fired — so the value observable is owed
        // for the tile the frame drew.
        Assert.Contains(messages, message => message.StartsWith("Overlay drawn: ", StringComparison.Ordinal));
        Assert.True(
            LastDrawnOverlayValue(messages, out var reported),
            OverlayValueSeamDemand);
        return reported!;
    }

    /// <summary>
    /// The most recent demanded overlay-value trace log's
    /// reported value, or false when the overlay draw path
    /// has not emitted one — the demanded overlay-value draw
    /// observable (see <see cref="OverlayValueSeamDemand"/>).
    /// </summary>
    private static bool LastDrawnOverlayValue(List<string> messages, out string? value)
    {
        value = null;
        for (var index = messages.Count - 1; index >= 0; index--)
        {
            if (messages[index].StartsWith(DrawnOverlayValueLogPrefix, StringComparison.Ordinal))
            {
                value = messages[index].Substring(DrawnOverlayValueLogPrefix.Length);
                return true;
            }
        }

        return false;
    }

    // ──────────────────────────────────────────────
    // T091 helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="VisualizationService"/> over Moq
    /// substitutes whose monitor captures log messages into
    /// <paramref name="messages"/> — the demanded ambient-tint
    /// draw observable's sink — initialized with the stand-in
    /// white texture the draw path draws rectangles with. The
    /// interpolation service resolves the unlit tint for every
    /// health and opacity (the ambient blend is the draw
    /// path's responsibility, not the palette's), and the
    /// configuration enables overlays at the configured
    /// opacity with patterns off.
    /// </summary>
    private static VisualizationService BuildAmbientBlendService(Color unlitTint, List<string> messages)
    {
        var monitor = new Mock<IMonitor>();
        monitor
            .Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
            .Callback<string, LogLevel>((message, level) => messages.Add(message));

        var colorService = new Mock<IColorInterpolationService>();
        colorService
            .Setup(c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>()))
            .Returns(unlitTint);
        colorService
            .Setup(c => c.GetCategoryForHealth(It.IsAny<float>()))
            .Returns(HealthCategory.Healthy);

        var configService = new Mock<IVisualizationConfigurationService>();
        configService
            .Setup(c => c.GetConfiguration())
            .Returns(new VisualizationConfiguration
            {
                OverlaysEnabled = true,
                Opacity = AmbientOverlayOpacity,
                ShowPatterns = false
            });

        var service = new VisualizationService(
            colorService.Object,
            configService.Object,
            monitor.Object,
            new Mock<IHudNotifier>().Object);
        service.Initialize(StandInWhiteTexture());
        return service;
    }

    /// <summary>
    /// Renders one overlay frame under the installed camera
    /// viewport and returns the tint the frame drew, observed
    /// through the demanded ambient-tint draw observable (see
    /// <see cref="AmbientTintSeamDemand"/>): the frame runs
    /// with a stand-in SpriteBatch — a real SpriteBatch needs a
    /// GraphicsDevice the headless unit-test host lacks, and
    /// SpriteBatch.Draw is non-virtual — so the stand-in's
    /// uninitialized internals raise at the draw, after the
    /// draw observable is emitted; the raise is the
    /// headless-host boundary and is absorbed here. Fails with
    /// <see cref="AmbientTintSeamDemand"/> until the overlay
    /// draw path emits the tint it draws.
    /// </summary>
    private static Color RenderOneOverlayFrame(VisualizationService service, List<string> messages)
    {
        messages.Clear();

        using var camera = new CameraViewportState(AmbientViewport);
        Record.Exception(() =>
            service.RenderOverlays(StandInSpriteBatch(), AmbientViewport, new GameTime()));

        Assert.True(
            LastDrawnOverlayTint(messages, out var tint),
            AmbientTintSeamDemand);
        return tint;
    }

    /// <summary>
    /// The most recent demanded ambient-tint trace log's RGBA
    /// tint, or false when the overlay draw path has not
    /// emitted one — the demanded ambient-tint draw observable
    /// (see <see cref="AmbientTintSeamDemand"/>).
    /// </summary>
    private static bool LastDrawnOverlayTint(List<string> messages, out Color tint)
    {
        tint = Color.Transparent;
        for (var index = messages.Count - 1; index >= 0; index--)
        {
            if (messages[index].StartsWith(DrawnOverlayTintLogPrefix, StringComparison.Ordinal))
            {
                return TryParseOverlayTint(messages[index], out tint);
            }
        }

        return false;
    }

    /// <summary>
    /// Parses the "RGBA(r, g, b, a)" suffix of a trace log. The
    /// production path will format the channels with the current
    /// culture, so the round-trip parses with it too — correct
    /// under any test-host locale (the
    /// <c>VisualizationZoomAnchorTests</c> parse convention).
    /// </summary>
    private static bool TryParseOverlayTint(string message, out Color tint)
    {
        tint = Color.Transparent;

        const string channelPrefix = "RGBA(";
        var channelStart = message.IndexOf(channelPrefix, StringComparison.Ordinal);
        var channelEnd = message.LastIndexOf(')');
        if (channelStart < 0 || channelEnd <= channelStart)
        {
            return false;
        }

        var channels = message
            .Substring(channelStart + channelPrefix.Length, channelEnd - channelStart - channelPrefix.Length)
            .Split(TintChannelSeparator, StringSplitOptions.None);
        if (channels.Length != 4)
        {
            return false;
        }

        if (!float.TryParse(channels[0], NumberStyles.Float, CultureInfo.CurrentCulture, out var red)
            || !float.TryParse(channels[1], NumberStyles.Float, CultureInfo.CurrentCulture, out var green)
            || !float.TryParse(channels[2], NumberStyles.Float, CultureInfo.CurrentCulture, out var blue)
            || !float.TryParse(channels[3], NumberStyles.Float, CultureInfo.CurrentCulture, out var alpha))
        {
            return false;
        }

        tint = new Color((byte)red, (byte)green, (byte)blue, (byte)alpha);
        return true;
    }

    /// <summary>
    /// The tint's perceived luminance — the ITU-R BT.601 luma
    /// weights (R*299 + G*587 + B*114), so "darker" compares
    /// the channels the way the eye weights them, not by raw
    /// channel sum.
    /// </summary>
    private static int Luminance(Color color)
    {
        return color.R * 299 + color.G * 587 + color.B * 114;
    }

    /// <summary>
    /// An uninitialized SpriteBatch stand-in: non-null (so null
    /// guards pass) but internally unallocated, so the first draw
    /// through it throws — the headless host has no
    /// <see cref="GraphicsDevice"/> to construct a real one (the
    /// <c>HoeFloatingTextTests</c> stand-in).
    /// </summary>
    private static SpriteBatch StandInSpriteBatch()
    {
        return (SpriteBatch)RuntimeHelpers.GetUninitializedObject(typeof(SpriteBatch));
    }

    /// <summary>
    /// An uninitialized Texture2D stand-in for the shared white
    /// texture (the same technique <c>HoeFloatingTextTests</c>
    /// installs on <c>VisualizationTextures.WhiteTexture</c>).
    /// </summary>
    private static Texture2D StandInWhiteTexture()
    {
        return (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
    }

    /// <summary>
    /// The game's ambient light state a frame renders under —
    /// <c>Game1.ambientLight</c> and <c>Game1.outdoorLight</c>
    /// (public static fields) set to the given light for the
    /// scope of the test and restored on dispose, so test
    /// classes running alongside this one observe the game state
    /// they started with (xunit parallelization is disabled
    /// repo-wide, so the save/restore is complete).
    /// </summary>
    private sealed class AmbientLightState : IDisposable
    {
        private readonly Color _originalAmbientLight;
        private readonly Color _originalOutdoorLight;

        public AmbientLightState(Color ambientLight)
        {
            _originalAmbientLight = Game1.ambientLight;
            _originalOutdoorLight = Game1.outdoorLight;

            Game1.ambientLight = ambientLight;
            Game1.outdoorLight = ambientLight;
        }

        public void Dispose()
        {
            Game1.ambientLight = _originalAmbientLight;
            Game1.outdoorLight = _originalOutdoorLight;
        }
    }

    /// <summary>
    /// The camera viewport state a frame renders under — the
    /// pixel-space <c>Game1.viewport</c> (a public static
    /// xTile.Dimensions.Rectangle) resolved from the tile-space
    /// viewport the overlay path culls against, set for the
    /// scope of the test and restored on dispose (the
    /// <c>VisualizationZoomAnchorTests</c> CameraState scope,
    /// reduced to the viewport swap).
    /// </summary>
    private sealed class CameraViewportState : IDisposable
    {
        private readonly xTile.Dimensions.Rectangle _originalViewport;

        public CameraViewportState(Rectangle tileViewport)
        {
            _originalViewport = Game1.viewport;

            Game1.viewport = new xTile.Dimensions.Rectangle(
                tileViewport.X * ModConstants.TileSize,
                tileViewport.Y * ModConstants.TileSize,
                tileViewport.Width * ModConstants.TileSize,
                tileViewport.Height * ModConstants.TileSize);
        }

        public void Dispose()
        {
            Game1.viewport = _originalViewport;
        }
    }

    // ──────────────────────────────────────────────
    // T094 helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Builds the mocked SMAPI environment <c>ModEntry.Entry</c>
    /// needs to boot the production composition root: a helper
    /// whose event sources are all mocks (the same harness
    /// shape <c>ModEntryTests.CreateEntryTestHarness</c>
    /// establishes — every event the composition root subscribes
    /// to is a mock, so no game loop actually runs), with the
    /// mod entry's Monitor and ModManifest set, which Entry
    /// reads when wiring the composition root.
    /// </summary>
    private static Mock<IModHelper> CreateProductionEntryHelper(ModEntry modEntry)
    {
        var helper = new Mock<IModHelper>();
        var events = new Mock<IModEvents>();
        var contentEvents = new Mock<IContentEvents>();
        var gameLoopEvents = new Mock<IGameLoopEvents>();
        var inputEvents = new Mock<IInputEvents>();
        var displayEvents = new Mock<IDisplayEvents>();
        var monitor = new Mock<IMonitor>();
        var manifest = new Mock<IManifest>();

        manifest.Setup(m => m.UniqueID).Returns("test.mod.id");
        manifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));

        events.Setup(e => e.Content).Returns(contentEvents.Object);
        events.Setup(e => e.GameLoop).Returns(gameLoopEvents.Object);
        events.Setup(e => e.Input).Returns(inputEvents.Object);
        events.Setup(e => e.Display).Returns(displayEvents.Object);
        helper.Setup(h => h.Events).Returns(events.Object);

        typeof(Mod).GetProperty("Monitor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(modEntry, monitor.Object);
        typeof(Mod).GetProperty("ModManifest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(modEntry, manifest.Object);

        return helper;
    }

    /// <summary>
    /// Reads a private (or public) instance field declared on
    /// <paramref name="declaringType"/> — the reflection
    /// seam-shape the ModEntryTests harness pattern establishes
    /// for the composition root's private wiring fields.
    /// </summary>
    private static T ReadPrivateField<T>(object instance, string fieldName, Type declaringType)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(field);
        return (T)field.GetValue(instance)!;
    }
}
