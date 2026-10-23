using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariant tests for overlay-list cache
/// invalidation (T146 [P] [US1], Feature 001 Soil Health
/// Visualization; FR-010). FR-010 requires: the system
/// caches computed health values, invalidating the cache
/// on game state changes (tile modification events, save
/// load); cache maintenance applies when overlays are
/// enabled; when overlays are disabled no rebuild occurs
/// (the disabled toggle suppresses the visualization's
/// own derived work per FR-023); and the overlay-list
/// cache is rebuilt on the first frame after re-enable.
/// FR-010's own verification: modify a tile health value
/// and confirm the overlay color updates within one frame
/// without manual refresh.
///
/// Wave 1 (TDD Red) status — Wave 2 fix-forward is T134:
/// RED: <see cref="GetOverlays_ForAnyTileHealthMutationAtFixedViewport_ReflectsMutatedHealthWithoutManualRefresh"/>,
/// <see cref="GetOverlays_ForAnyOpacityChangeAtFixedViewport_ReflectsNewOpacity"/>,
/// and <see cref="RenderOverlays_ForAnyDisableEnableInterleaving_RebuildsOnFirstFrameAfterReEnableWithinBudget"/>
/// fail because OverlayRenderer.GetOverlays keys its cache
/// on the viewport and map bounds alone: a tile-health
/// mutation, a configuration (opacity) change, or a
/// disable→re-enable cycle at an unchanged viewport is
/// served the stale cached list, so health and opacity
/// edits stay invisible until the camera moves or a caller
/// manually invokes InvalidateCache. The fix lands in
/// OverlayRenderer/VisualizationService (T134).
/// GREEN: the InvalidateCache seam (the call the
/// OnTileHealthChanged wiring and the save/load handlers
/// make) forces recomputation on the next call, and the
/// disabled toggle performs no overlay computation.
/// </summary>
public class VisualizationCacheInvalidationInvariantsTests
{
    /// <summary>
    /// The tile grid every scenario renders: 4x3 tiles at
    /// (0..3, 0..2), all inside the fixed viewport.
    /// </summary>
    private const int TileGridWidth = 4;
    private const int TileGridHeight = 3;
    private const int VisibleTileCount = TileGridWidth * TileGridHeight;

    /// <summary>
    /// The warm-frame health value. Mutations derive
    /// health in [1, 49], so every mutation is a real
    /// change from the warm value (and never the
    /// zero-health skip the render path applies).
    /// </summary>
    private const float WarmHealth = 50f;

    /// <summary>
    /// The warm-frame opacity for the opacity-change
    /// property (a distinct value inside the [0, 1]
    /// configured domain).
    /// </summary>
    private const float WarmOpacity = 0.2f;

    /// <summary>
    /// Interleaving event kinds for the disable/enable
    /// property: tile-health mutation, configuration
    /// (opacity) change, overlays disabled, overlays
    /// re-enabled.
    /// </summary>
    private const int EventKindCount = 4;
    private const int EventMutate = 0;
    private const int EventConfigChange = 1;
    private const int EventDisable = 2;
    private const int EventEnable = 3;

    /// <summary>Bounds the generated interleaving length.</summary>
    private const int MaxEvents = 24;

    /// <summary>
    /// The fixed viewport (tile units) covering the whole
    /// grid: 4x3 tiles at 64px per tile.
    /// </summary>
    private static readonly Rectangle Viewport = new(
        0,
        0,
        TileGridWidth * ModConstants.TileSize,
        TileGridHeight * ModConstants.TileSize);

    private static readonly IColorInterpolationService ColorService = new ColorInterpolationService();

    /// <summary>
    /// Invariant: for ANY sequence of tile-health
    /// mutations, the overlay list at an UNCHANGED
    /// viewport reflects the mutated health on the very
    /// next render — within one frame, without any manual
    /// refresh (FR-010's verification: "modify a tile
    /// health value and confirm overlay color updates
    /// within one frame without manual refresh"). Each
    /// mutation presents a fresh snapshot instance,
    /// exactly as the frame feed does (ModController
    /// builds a new dictionary per rendered frame).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool GetOverlays_ForAnyTileHealthMutationAtFixedViewport_ReflectsMutatedHealthWithoutManualRefresh(int[] mutations)
    {
        var renderer = CreateRenderer(new VisualizationConfiguration());
        var snapshot = BuildSnapshot(WarmHealth);

        // Warm the cache at this viewport (FR-010: cache
        // computed values to avoid redundant calculations).
        renderer.GetOverlays(Viewport, snapshot);

        var eventCount = Math.Min(mutations.Length, MaxEvents);
        for (var i = 0; i < eventCount; i++)
        {
            var tile = TileAt(i % VisibleTileCount);
            var mutatedHealth = 1f + (Math.Abs(mutations[i]) % 49);
            var expectedColor = ColorService.GetColorForHealth(mutatedHealth, ModConstants.DefaultOpacity);

            snapshot = Mutate(snapshot, tile, mutatedHealth);

            var overlays = renderer.GetOverlays(Viewport, snapshot);
            var overlay = overlays.Single(o => o.TilePosition == tile);

            if (overlay.Color != expectedColor)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Invariant: for ANY sequence of configuration
    /// (opacity) changes, the overlay list at an UNCHANGED
    /// viewport carries the newly configured opacity on the
    /// very next render — cache maintenance applies when
    /// overlays are enabled, and a configuration change
    /// invalidates the overlay-list cache (FR-010).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool GetOverlays_ForAnyOpacityChangeAtFixedViewport_ReflectsNewOpacity(float[] opacities)
    {
        var config = new VisualizationConfiguration { Opacity = WarmOpacity };
        var mockConfigService = new Mock<IVisualizationConfigurationService>();
        mockConfigService.Setup(c => c.GetConfiguration()).Returns(() => config);
        var mockMonitor = new Mock<IMonitor>();
        var renderer = new OverlayRenderer(ColorService, mockConfigService.Object, mockMonitor.Object, new Mock<IHudNotifier>().Object);

        var snapshot = BuildSnapshot(WarmHealth);

        // Warm the cache at this viewport with the warm opacity.
        renderer.GetOverlays(Viewport, snapshot);

        var eventCount = Math.Min(opacities.Length, MaxEvents);
        for (var i = 0; i < eventCount; i++)
        {
            // The configured opacity domain is [0, 1]
            // (data-model.md VisualizationConfiguration);
            // values outside it are replaced by defaults
            // before reaching the renderer (FR-006).
            var newOpacity = Math.Clamp(opacities[i], 0f, 1f);
            if (Math.Abs(newOpacity - config.Opacity) < 0.0001f)
            {
                // Ensure the step is a real change so the
                // property exercises a genuine invalidation.
                newOpacity = (newOpacity + 0.37f) % 1f;
            }

            config.Opacity = newOpacity;

            var overlays = renderer.GetOverlays(Viewport, snapshot);
            var expectedColor = ColorService.GetColorForHealth(WarmHealth, newOpacity);
            foreach (var overlay in overlays)
            {
                if (overlay.Color != expectedColor)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Invariant: for ANY save/load + configuration-change
    /// + re-enable interleaving, the overlay-list cache
    /// invalidates on game state changes and on
    /// configuration change, no rebuild occurs while
    /// overlays are disabled, and the first frame after
    /// re-enable rebuilds the overlay list from the
    /// current data within the FR-009 frame budget
    /// (<see cref="ModConstants.MaxRenderTimeMs"/>) —
    /// FR-010. A frame that follows a state change
    /// (mutation, configuration change, or re-enable)
    /// must recompute: the color service is consulted
    /// with the current tile health values. A frame
    /// following no state change may legitimately be
    /// served from the cache.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RenderOverlays_ForAnyDisableEnableInterleaving_RebuildsOnFirstFrameAfterReEnableWithinBudget(int[] rawEvents)
    {
        var consultedHealths = new List<float>();
        var mockColorService = new Mock<IColorInterpolationService>();
        mockColorService.Setup(c => c.GetCategoryForHealth(It.IsAny<float>()))
            .Returns<float>(health =>
            {
                consultedHealths.Add(health);
                return HealthCategory.Poor;
            });
        mockColorService.Setup(c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>()))
            .Returns(Color.Red);
        mockColorService.Setup(c => c.GetColorForHealth(It.IsAny<float>()))
            .Returns(Color.Red);

        var config = new VisualizationConfiguration { Opacity = ModConstants.DefaultOpacity };
        var mockConfigService = new Mock<IVisualizationConfigurationService>();
        mockConfigService.Setup(c => c.GetConfiguration()).Returns(() => config);
        var mockMonitor = new Mock<IMonitor>();

        var service = new VisualizationService(
            mockColorService.Object,
            mockConfigService.Object,
            mockMonitor.Object,
            new Mock<IHudNotifier>().Object);
        var snapshot = BuildSnapshot(WarmHealth);

        var enabled = true;
        var stateChangedSinceLastFrame = true;
        var allFramesConsistent = true;
        var allFramesWithinBudget = true;

        // The warm frame: overlays enabled, initial data.
        service.SetTileHealthData(snapshot);
        var warmException = Record.Exception(() => service.RenderOverlays(null!, Viewport, new GameTime()));

        // The seeded tiles produce overlays, so the frame
        // reaches the draw stage, which throws from
        // GetTexture() before a null SpriteBatch is
        // dereferenced (the existing render-test signal
        // that the pipeline ran).
        allFramesConsistent &= warmException is InvalidOperationException;
        allFramesConsistent &= ConsultedExactly(consultedHealths, snapshot);

        // The warm frame consumed the initial state
        // establishment (the SetTileHealthData feed is the
        // state change and the warm frame is the frame that
        // rebuilt from it, asserted above). No state change
        // is pending for the first event's frame, so the
        // flag starts the loop cleared — a first event that
        // changes nothing (a bare enable) is legitimately
        // served from the cache.
        stateChangedSinceLastFrame = false;

        var eventCount = Math.Min(rawEvents.Length, MaxEvents);
        for (var i = 0; i < eventCount; i++)
        {
            var kind = Mod(rawEvents[i], EventKindCount);
            switch (kind)
            {
                case EventMutate:
                    var tile = TileAt(i % VisibleTileCount);
                    var mutatedHealth = 1f + (Math.Abs(rawEvents[i]) % 49);
                    if (snapshot.TryGetValue(tile, out var currentHealth) && mutatedHealth == currentHealth)
                    {
                        // Ensure the step is a real change
                        // so the property exercises a genuine
                        // invalidation (the same guard the
                        // configuration-change event applies):
                        // writing a tile's current value
                        // changes no data, the tile-health
                        // generation fingerprint is unchanged,
                        // and production correctly serves the
                        // cache. The shifted value stays in
                        // the mutation domain [1, 49] and
                        // differs from the current value by
                        // construction.
                        mutatedHealth = mutatedHealth == 49f ? 1f : mutatedHealth + 1f;
                    }

                    snapshot = Mutate(snapshot, tile, mutatedHealth);
                    // The mutated values must reach the service:
                    // production feeds a fresh snapshot per rendered
                    // frame (the controller's per-frame
                    // SetTileHealthData) and OnTileHealthChanged
                    // invalidates the cache, so the frame after a
                    // tile modification rebuilds from the mutated
                    // data. Without the feed the service would keep
                    // rendering the warm data and the asserted
                    // rebuild would consult stale healths.
                    service.SetTileHealthData(snapshot);
                    stateChangedSinceLastFrame = true;
                    break;

                case EventConfigChange:
                    var newOpacity = Math.Clamp(Math.Abs(rawEvents[i]) / 100f, 0f, 1f);
                    if (Math.Abs(newOpacity - config.Opacity) < 0.0001f)
                    {
                        // Ensure the step is a real change
                        // so the property exercises a
                        // genuine invalidation.
                        newOpacity = (newOpacity + 0.37f) % 1f;
                    }

                    config.Opacity = newOpacity;
                    stateChangedSinceLastFrame = true;
                    break;

                case EventDisable:
                    config.OverlaysEnabled = false;
                    enabled = false;
                    break;

                default:
                    config.OverlaysEnabled = true;
                    if (!enabled)
                    {
                        enabled = true;
                        // FR-010: the overlay-list cache is
                        // rebuilt on the first frame after
                        // re-enable — the disable→enable
                        // transition. A bare enable while
                        // overlays are already enabled flips
                        // nothing, and production correctly
                        // serves the cache, so that frame is
                        // not a state change.
                        stateChangedSinceLastFrame = true;
                    }
                    break;
            }

            if (!enabled)
            {
                // FR-010: when overlays are disabled no
                // rebuild occurs — the disabled toggle
                // suppresses the visualization's own
                // derived work end-to-end.
                var disabledConsultations = consultedHealths.Count;
                var disabledException = Record.Exception(() => service.RenderOverlays(null!, Viewport, new GameTime()));
                allFramesConsistent &= disabledException is null;
                allFramesConsistent &= consultedHealths.Count == disabledConsultations;
                continue;
            }

            var consultationCount = consultedHealths.Count;
            var stopwatch = Stopwatch.StartNew();
            var exception = Record.Exception(() => service.RenderOverlays(null!, Viewport, new GameTime()));
            stopwatch.Stop();

            // The frame reaches the draw stage (the seeded
            // tiles produce overlays in every enabled state).
            allFramesConsistent &= exception is InvalidOperationException;

            if (stateChangedSinceLastFrame)
            {
                // FR-010: the cache invalidates on game
                // state changes (tile modification events,
                // save load) and on configuration change,
                // and the first frame after re-enable
                // rebuilds — so the frame recomputes the
                // overlays from the current data.
                var newlyConsulted = consultedHealths.Skip(consultationCount).ToList();
                allFramesConsistent &= ConsultedExactly(newlyConsulted, snapshot);
                stateChangedSinceLastFrame = false;
            }

            // The rebuild completes within the FR-009 frame
            // budget.
            allFramesWithinBudget &= stopwatch.Elapsed.TotalMilliseconds <= ModConstants.MaxRenderTimeMs;
        }

        return allFramesConsistent && allFramesWithinBudget;
    }

    /// <summary>
    /// Invariant: the invalidation seam — the call the
    /// soil-health change wiring (ModEntry's
    /// OnTileHealthChanged handler) and the save/load
    /// handlers make — forces recomputation on the next
    /// render at an unchanged viewport, reflecting the
    /// current data (FR-010: "invalidating the cache on
    /// game state changes (tile modification events, save
    /// load)"). GREEN by existing behavior; this pins the
    /// seam T134's automatic invalidation builds on.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool GetOverlays_AfterInvalidateCache_ReflectsCurrentData(int[] mutations)
    {
        var renderer = CreateRenderer(new VisualizationConfiguration());
        var snapshot = BuildSnapshot(WarmHealth);

        renderer.GetOverlays(Viewport, snapshot);

        var eventCount = Math.Min(mutations.Length, MaxEvents);
        for (var i = 0; i < eventCount; i++)
        {
            var tile = TileAt(i % VisibleTileCount);
            var mutatedHealth = 1f + (Math.Abs(mutations[i]) % 49);
            var expectedColor = ColorService.GetColorForHealth(mutatedHealth, ModConstants.DefaultOpacity);

            snapshot = Mutate(snapshot, tile, mutatedHealth);

            // The manual invalidation seam (save load, tile
            // modification events).
            renderer.InvalidateCache();

            var overlays = renderer.GetOverlays(Viewport, snapshot);
            var overlay = overlays.Single(o => o.TilePosition == tile);

            if (overlay.Color != expectedColor)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// FR-010: "when overlays are disabled no rebuild
    /// occurs" — a disabled frame performs no overlay
    /// computation at all: the color service is never
    /// consulted and the frame returns before any overlay
    /// list is built (the disabled toggle suppresses the
    /// visualization's own derived work per FR-023).
    /// GREEN by existing behavior.
    /// </summary>
    [Fact]
    public void RenderOverlays_WhenOverlaysDisabled_PerformsNoOverlayComputation()
    {
        var mockColorService = new Mock<IColorInterpolationService>();
        var config = new VisualizationConfiguration { OverlaysEnabled = false };
        var mockConfigService = new Mock<IVisualizationConfigurationService>();
        mockConfigService.Setup(c => c.GetConfiguration()).Returns(() => config);
        var mockMonitor = new Mock<IMonitor>();

        var service = new VisualizationService(
            mockColorService.Object,
            mockConfigService.Object,
            mockMonitor.Object,
            new Mock<IHudNotifier>().Object);
        service.SetTileHealthData(BuildSnapshot(WarmHealth));

        var exception = Record.Exception(() => service.RenderOverlays(null!, Viewport, new GameTime()));

        Assert.Null(exception);
        mockColorService.Verify(c => c.GetCategoryForHealth(It.IsAny<float>()), Times.Never);
        mockColorService.Verify(c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>()), Times.Never);
        mockColorService.Verify(c => c.GetColorForHealth(It.IsAny<float>()), Times.Never);
    }

    private static OverlayRenderer CreateRenderer(VisualizationConfiguration configuration)
    {
        var mockConfigService = new Mock<IVisualizationConfigurationService>();
        mockConfigService.Setup(c => c.GetConfiguration()).Returns(configuration);
        var mockMonitor = new Mock<IMonitor>();

        return new OverlayRenderer(ColorService, mockConfigService.Object, mockMonitor.Object, new Mock<IHudNotifier>().Object);
    }

    /// <summary>
    /// Builds a tile-health snapshot carrying every grid
    /// tile at the given health value.
    /// </summary>
    private static Dictionary<Point, float> BuildSnapshot(float health)
    {
        var snapshot = new Dictionary<Point, float>(VisibleTileCount);
        for (var i = 0; i < VisibleTileCount; i++)
        {
            snapshot[TileAt(i)] = health;
        }

        return snapshot;
    }

    /// <summary>
    /// Returns a fresh snapshot instance with one tile's
    /// health mutated — exactly as the frame feed presents
    /// a tile modification (ModController builds a new
    /// dictionary per rendered frame).
    /// </summary>
    private static Dictionary<Point, float> Mutate(
        Dictionary<Point, float> snapshot,
        Point tile,
        float newHealth)
    {
        var mutated = new Dictionary<Point, float>(snapshot);
        mutated[tile] = newHealth;
        return mutated;
    }

    /// <summary>
    /// The grid tile at the linear index (row-major).
    /// </summary>
    private static Point TileAt(int index)
    {
        return new Point(index % TileGridWidth, index / TileGridWidth);
    }

    /// <summary>
    /// Multiset equality between the consulted health
    /// values and the snapshot's tile health values: a
    /// rebuild consults the color service exactly once per
    /// visible tile, with the current health values.
    /// </summary>
    private static bool ConsultedExactly(List<float> consulted, Dictionary<Point, float> snapshot)
    {
        if (consulted.Count != snapshot.Count)
        {
            return false;
        }

        var expected = new List<float>(snapshot.Values);
        expected.Sort();
        var actual = new List<float>(consulted);
        actual.Sort();

        for (var i = 0; i < expected.Count; i++)
        {
            if (expected[i] != actual[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int Mod(int value, int modulus)
    {
        return ((value % modulus) + modulus) % modulus;
    }
}
