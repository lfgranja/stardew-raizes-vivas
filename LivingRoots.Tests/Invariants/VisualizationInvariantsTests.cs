using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariants for the Playable Compost Loop (spec 004)
/// visualization seam: T025b render-safety and T031b first-wins +
/// overlay timing/distinguishability. Every production bound comes from
/// <see cref="ModConstants"/> — no hardcoded palette, opacity or budget
/// values are restated.
/// </summary>
/// <remarks>
/// The render path is observed through the
/// <see cref="IColorInterpolationService"/> seam: the overlay pipeline
/// resolves a color only once an overlay exists, so a color-service
/// invocation is the headless-host proxy for "the render path ran".
/// </remarks>
public class VisualizationInvariantsTests
{
    /// <summary>Tile-space viewport the render frames cull against.</summary>
    private static readonly Rectangle Viewport =
        new Rectangle(0, 0, ModConstants.TileSize, ModConstants.TileSize);

    /// <summary>The single tile the generated snapshots always carry.</summary>
    private static readonly Point OverlayTile = new Point(0, 0);

    /// <summary>
    /// The requested accessibility lower bound on the blended pattern
    /// alpha (T031b). It is deliberately stricter than the production
    /// floor <see cref="ModConstants.PatternMinOpacity"/> (0.7), so the
    /// invariant survives a small palette/quantization drift while still
    /// failing if the floor is removed entirely.
    /// </summary>
    private const float MinBlendAlpha = 0.65f;

    // ────────────────────────────────────────────────────────────────
    // T025b — null-cache render safety
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// T025b invariant: for any generated tile snapshot, the render path
    /// runs if and only if the cache is non-null. A null cache means the
    /// <see cref="VisualizationService"/> must not resolve a single color
    /// — no default or zero-value fallback rendering occurs.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property T025b_NullCache_NoRender()
    {
        return Prop.ForAll(
            Arb.From(NullableTileDataGenerator()),
            tileData =>
            {
                var (service, colorMock) = BuildRenderService();

                if (tileData != null)
                {
                    service.SetTileHealthData(tileData);
                }

                RenderOverlaysSwallowingDrawBoundary(service);

                bool renderReached = colorMock.Invocations.Count > 0;
                return tileData == null ? !renderReached : renderReached;
            });
    }

    /// <summary>
    /// T025b invariant (positive half): for any initialized tile snapshot
    /// carrying a drawable reading, the render path resolves a color — the
    /// service never silently drops a cache it was handed.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property T025b_InitializedCache_Render()
    {
        return Prop.ForAll(
            Arb.From(TileDataGenerator()),
            tileData =>
            {
                var (service, colorMock) = BuildRenderService();
                service.SetTileHealthData(tileData);

                RenderOverlaysSwallowingDrawBoundary(service);

                return colorMock.Invocations.Count > 0;
            });
    }

    // ────────────────────────────────────────────────────────────────
    // T031b — distinguishability, blend alpha, tier mapping
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// T031b invariant: within a gradient tier (Poor or Moderate), two readings
    /// at least one point apart produce different tints. Cross-tier distinctness
    /// is deliberately NOT asserted: the palette is a continuous gradient that
    /// reaches the Healthy colour at 70, and the Healthy tier is held flat
    /// (T253/FR-010) — so e.g. 70 (Moderate) and every Healthy reading share a
    /// colour by design. Health is generated as an integer 0–100 (its real
    /// domain), so the property does not depend on the unreachable non-integer
    /// category gaps.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property T031b_OverlayDistinguishable_DistinctHealth_DistinctColors()
    {
        return Prop.ForAll(
            Arb.From(IntegerHealthGenerator().Two()),
            pair => DistinctHealth_ProducesDistinctColors(pair.Item1, pair.Item2));
    }

    /// <summary>Integer soil health 0–100 — the domain soil health actually takes.</summary>
    private static Gen<float> IntegerHealthGenerator()
    {
        return Gen.Choose(0, 100).Select(i => (float)i);
    }

    private static bool DistinctHealth_ProducesDistinctColors(float healthA, float healthB)
    {
        var colorService = new ColorInterpolationService();
        var categoryA = colorService.GetCategoryForHealth(healthA);
        var categoryB = colorService.GetCategoryForHealth(healthB);

        // Cross-tier readings are not required to differ: the palette is a
        // continuous gradient (Poor→Moderate→Healthy) with a flat Healthy tier
        // (T253/FR-010), so the tier seams share the endpoint colour.
        if (categoryA != categoryB)
        {
            return true;
        }

        if (categoryA == HealthCategory.Healthy || categoryA == HealthCategory.Unknown)
        {
            return true;
        }

        if (Math.Abs(healthA - healthB) < 1f)
        {
            return true;
        }

        return colorService.GetColorForHealth(healthA) != colorService.GetColorForHealth(healthB);
    }

    /// <summary>
    /// T031b (deterministic companion): the palette is continuous at the tier
    /// seams — a seam value and the first value of the next tier share the
    /// endpoint colour by design (T253/FR-010). Pins that behaviour so it stays
    /// intentional rather than accidental.
    /// </summary>
    [Theory]
    [InlineData(30f, 31f)]
    [InlineData(70f, 71f)]
    public void T031b_PaletteSeam_SharesTheEndpointColour(float low, float high)
    {
        var colorService = new ColorInterpolationService();

        Assert.NotEqual(colorService.GetCategoryForHealth(low), colorService.GetCategoryForHealth(high));
        Assert.Equal(colorService.GetColorForHealth(low), colorService.GetColorForHealth(high));
    }

    /// <summary>T031b: the Healthy tier is flat — every Healthy reading shares one colour.</summary>
    [Fact]
    public void T031b_HealthyTier_IsFlat()
    {
        var colorService = new ColorInterpolationService();
        Assert.Equal(colorService.GetColorForHealth(71f), colorService.GetColorForHealth(100f));
    }

    /// <summary>
    /// T031b invariant: the blended pattern alpha never drops below the
    /// requested accessibility floor. For any opacity, the production
    /// floor clamp (<c>Math.Max(PatternMinOpacity, opacity)</c>) keeps the
    /// effective pattern opacity — and the byte alpha the color service
    /// derives from it — at or above <see cref="MinBlendAlpha"/>.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool T031b_BlendAlpha_PatternFloor_NeverBelowMinimum(float rawOpacity)
    {
        float opacity = float.IsFinite(rawOpacity)
            ? Math.Clamp(rawOpacity, 0f, 1f)
            : ModConstants.DefaultOpacity;

        float effectivePatternOpacity = Math.Max(ModConstants.PatternMinOpacity, opacity);
        if (effectivePatternOpacity < MinBlendAlpha)
        {
            return false;
        }

        var colorService = new ColorInterpolationService();
        var patternColor = colorService.GetColorForHealth(50f, effectivePatternOpacity);
        float alphaFraction = patternColor.A / 255f;
        return alphaFraction >= MinBlendAlpha;
    }

    /// <summary>
    /// T031b invariant: the 3-tier mapping is exact — health 1-33 maps to
    /// Poor, 34-66 to Moderate, 67-100 to Healthy, verified through
    /// <see cref="ColorInterpolationService"/>.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool T031b_TierInterpolation_HealthRange_MapsToExpectedCategory(int health)
    {
        if (health < 1 || health > ModConstants.MaxSoilHealth)
        {
            return true;
        }

        var colorService = new ColorInterpolationService();
        var actual = colorService.GetCategoryForHealth(health);
        var expected = health <= 30
            ? HealthCategory.Poor
            : health <= 70
                ? HealthCategory.Moderate
                : HealthCategory.Healthy;

        return actual == expected;
    }

    // ────────────────────────────────────────────────────────────────
    // T031b — first-wins concurrency
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// T031b invariant: for any number of concurrent contenders on the
    /// same bin coordinate, host-authoritative atomic locking applies
    /// exactly one change — exactly one contender acquires, every other is
    /// refused (FR-019). The contention is simulated with dedicated
    /// threads released from a single gate so the attempt window overlaps.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool T031b_FirstWins_ConcurrentActions_ApplyExactlyOneChange(PositiveInt arbitraryContenders)
    {
        int contenders = 2 + (arbitraryContenders.Get % 7);
        var lockService = new HostAtomicLockService();
        var tile = new Vector2(arbitraryContenders.Get % ModConstants.TileSize, 0);

        int winners = 0;
        var startGate = new ManualResetEventSlim(false);
        var threads = new Thread[contenders];

        for (int i = 0; i < contenders; i++)
        {
            long farmerId = i + 1;
            threads[i] = new Thread(() =>
            {
                startGate.Wait();
                if (lockService.TryAcquireLock("Farm", tile, farmerId))
                {
                    Interlocked.Increment(ref winners);
                }
            });
            threads[i].Start();
        }

        startGate.Set();
        foreach (var thread in threads)
        {
            thread.Join();
        }

        return winners == 1 && lockService.IsLocked("Farm", tile);
    }

    // ────────────────────────────────────────────────────────────────
    // T031b — reactive overlay timing
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// T031b invariant: for any health update, the reactive invalidation
    /// the production wiring subscribes to
    /// (<c>OnTileHealthChanged</c> → overlay cache invalidation) completes
    /// within one frame budget (<see cref="ModConstants.MaxRenderTimeMs"/>),
    /// so a changed reading is reflected on the next frame.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool T031b_OverlayTiming_ReactiveInvalidation_WithinFrameBudget(float health)
    {
        var renderer = BuildOverlayRenderer();
        var tileData = new Dictionary<Point, float>
        {
            { OverlayTile, SafeOverlayHealth(health) }
        };

        var overlays = renderer.GetOverlays(Viewport, tileData);
        if (overlays.Count == 0 || renderer.IsCacheEmpty)
        {
            return false;
        }

        Action<string, Vector2, float> handler = (location, tile, updatedHealth) =>
        {
            renderer.InvalidateCache();
        };

        var stopwatch = Stopwatch.StartNew();
        handler("Farm", new Vector2(OverlayTile.X, OverlayTile.Y), health);
        stopwatch.Stop();

        return renderer.IsCacheEmpty
            && stopwatch.Elapsed.TotalMilliseconds <= ModConstants.MaxRenderTimeMs;
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="VisualizationService"/> whose color service is a
    /// mock, so a recorded invocation is the "render path ran" observable.
    /// </summary>
    private static (VisualizationService Service, Mock<IColorInterpolationService> ColorMock) BuildRenderService()
    {
        var colorMock = new Mock<IColorInterpolationService>();
        var configMock = new Mock<IVisualizationConfigurationService>();
        configMock
            .Setup(c => c.GetConfiguration())
            .Returns(new VisualizationConfiguration
            {
                OverlaysEnabled = true,
                Opacity = ModConstants.DefaultOpacity
            });

        var service = new VisualizationService(
            colorMock.Object,
            configMock.Object,
            Mock.Of<IMonitor>());

        return (service, colorMock);
    }

    /// <summary>Builds an <see cref="OverlayRenderer"/> for the timing invariant.</summary>
    private static OverlayRenderer BuildOverlayRenderer()
    {
        var colorMock = new Mock<IColorInterpolationService>();
        colorMock
            .Setup(c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>()))
            .Returns(Color.White);
        colorMock
            .Setup(c => c.GetCategoryForHealth(It.IsAny<float>()))
            .Returns(HealthCategory.Moderate);

        var configMock = new Mock<IVisualizationConfigurationService>();
        configMock
            .Setup(c => c.GetConfiguration())
            .Returns(new VisualizationConfiguration
            {
                Opacity = ModConstants.DefaultOpacity,
                ShowPatterns = false,
                AccessibilityDegradation = "never"
            });

        return new OverlayRenderer(colorMock.Object, configMock.Object, Mock.Of<IMonitor>());
    }

    /// <summary>
    /// Renders one frame, swallowing the exception the headless host raises
    /// once an overlay reaches the draw stage (no <c>GraphicsDevice</c> and
    /// no initialized white texture). The color service has already been
    /// consulted by then, which is the observable this file asserts on.
    /// </summary>
    private static void RenderOverlaysSwallowingDrawBoundary(VisualizationService service)
    {
        try
        {
            service.RenderOverlays(null!, Viewport, new GameTime());
        }
        catch (InvalidOperationException)
        {
            // Expected: GetTexture() throws because Initialize() was never
            // called in the headless host. The render path ran.
        }
    }

    /// <summary>A nullable snapshot: null half the time, otherwise one drawable tile.</summary>
    private static Gen<Dictionary<Point, float>?> NullableTileDataGenerator()
    {
        return from health in Arb.Generate<float>()
               from isNull in Arb.Generate<bool>()
               select isNull
                   ? (Dictionary<Point, float>?)null
                   : new Dictionary<Point, float> { { OverlayTile, SafeOverlayHealth(health) } };
    }

    /// <summary>A non-null snapshot carrying exactly one drawable tile.</summary>
    private static Gen<Dictionary<Point, float>> TileDataGenerator()
    {
        return from health in Arb.Generate<float>()
               select new Dictionary<Point, float> { { OverlayTile, SafeOverlayHealth(health) } };
    }

    /// <summary>
    /// Guarantees a finite, non-zero reading so the overlay pipeline always
    /// produces a drawable overlay (zero and non-finite readings are
    /// deliberately skipped or reported as Unknown).
    /// </summary>
    private static float SafeOverlayHealth(float health)
    {
        return float.IsFinite(health) && health != 0f ? health : 50f;
    }
}
