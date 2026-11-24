using System;
using System.Collections.Generic;
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
/// Property-based invariant tests for graceful-degradation hysteresis
/// (T116, FR-009, SC-002). For any visible-tile-count sequence,
/// degradation activates only when the count exceeds
/// <see cref="ModConstants.DegradationTileThreshold"/>, recovers only
/// at the lower hysteresis band edge
/// <see cref="ModConstants.DegradationRecoveryThreshold"/>, and the
/// player-facing notification fires exactly once per degradation
/// event, re-arming only at the band edge.
/// </summary>
public class VisualizationDegradationInvariantsTests
{
    private const int TileSpan = 100;
    private const float HealthValue = 40f;
    private const int MaxFrameCount = 25;
    private const string AutoMode = "auto";
    private const string NeverMode = "never";

    // Half the width of the asymmetric hysteresis band, derived from
    // the two ModConstants edges so the directed edge counts below
    // stay in sync with the production tuning seam.
    private static readonly int HalfBandWidth =
        (ModConstants.DegradationTileThreshold - ModConstants.DegradationRecoveryThreshold) / 2;

    // Directed generation around the edges that decide the invariant:
    // the recovery band edge, the degrade threshold, and one step
    // either side of each.
    private static readonly int[] EdgeCounts =
    {
        ModConstants.DegradationRecoveryThreshold - HalfBandWidth,
        ModConstants.DegradationRecoveryThreshold - 1,
        ModConstants.DegradationRecoveryThreshold,
        ModConstants.DegradationRecoveryThreshold + 1,
        ModConstants.DegradationTileThreshold,
        ModConstants.DegradationTileThreshold + 1,
        ModConstants.DegradationTileThreshold + HalfBandWidth
    };

    /// <summary>
    /// Invariant: "never" is the pure escape hatch (FR-009): for any
    /// visible-tile-count sequence, a "never" configuration never
    /// degrades — every tile keeps its accessibility pattern — and
    /// no degradation notification ever fires (SC-002).
    /// </summary>
    /// <remarks>
    /// The invariant is scoped to "never" only (T116, Session
    /// 2026-10-06). "notify" is NOT an escape hatch: it shares the
    /// degradation state machine with "auto" (T136, FR-009), so it
    /// degrades at the same thresholds and announces every degradation
    /// event through exactly one player-facing toast and one Warn log
    /// while every accessibility pattern stays rendered. That
    /// announcement behavior is pinned by the directed
    /// DegradationNotifyTests (one toast + one Warn per event,
    /// re-arming only at the 980 band edge), not by this invariant.
    /// </remarks>
    [Property(MaxTest = 100)]
    public bool GetOverlays_ForAnyVisibleCountSequence_NeverModeNeverDegrades(int[] rawCounts)
    {
        var counts = MapToEdgeCounts(rawCounts);
        var configuration = new VisualizationConfiguration
        {
            AccessibilityDegradation = NeverMode,
            ShowPatterns = true
        };
        var counter = new NotificationCounter();
        var renderer = CreateRenderer(configuration, CreateCountingNotifier(counter));
        var snapshot = new Dictionary<Point, float>();

        var allFramesConsistent = true;
        for (var frame = 0; frame < counts.Length; frame++)
        {
            var count = counts[frame];
            SetTileCount(snapshot, count);
            var viewport = new Rectangle(0, 0, 99 + frame, 99 + frame);
            var overlays = renderer.GetOverlays(viewport, snapshot);

            var frameConsistent = overlays.Count == count
                && overlays.All(overlay => overlay.PatternType == PatternType.Stripes);
            allFramesConsistent &= frameConsistent;
        }

        return allFramesConsistent && counter.Count == 0;
    }

    /// <summary>
    /// Counts player-facing degradation notifications without coupling
    /// to the notification text (SC-002).
    /// </summary>
    private sealed class NotificationCounter
    {
        public int Count { get; private set; }

        public void Record()
        {
            Count++;
        }
    }

    /// <summary>
    /// Invariant: for any visible-tile-count sequence (FR-009, SC-002),
    /// the degradation state follows the asymmetric hysteresis band —
    /// it activates only when the count exceeds
    /// <see cref="ModConstants.DegradationTileThreshold"/> and recovers
    /// only when the count falls to
    /// <see cref="ModConstants.DegradationRecoveryThreshold"/> — and
    /// the player-facing notification fires exactly once per
    /// degradation event, re-arming only at the band edge. Every frame
    /// renders uniformly: all tiles carry the pattern state the single
    /// frame-start decision produced (no mixed-state frames).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool GetOverlays_ForAnyVisibleCountSequence_DegradesAboveThresholdAndRecoversOnlyAtBandEdge(int[] rawCounts)
    {
        var counts = MapToEdgeCounts(rawCounts);
        var configuration = new VisualizationConfiguration
        {
            AccessibilityDegradation = AutoMode,
            ShowPatterns = true
        };
        var counter = new NotificationCounter();
        var renderer = CreateRenderer(configuration, CreateCountingNotifier(counter));
        var snapshot = new Dictionary<Point, float>();

        var expectedDegraded = false;
        var expectedEventCount = 0;
        var allFramesConsistent = true;

        for (var frame = 0; frame < counts.Length; frame++)
        {
            var count = counts[frame];
            SetTileCount(snapshot, count);

            // A unique viewport per frame is a cache miss (FR-010), so
            // the render pass re-presents the frame-start snapshot
            // reference and the frame boundary re-establishes the
            // degradation decision from this count.
            var viewport = new Rectangle(0, 0, 99 + frame, 99 + frame);
            var overlays = renderer.GetOverlays(viewport, snapshot);

            // Hysteresis model: degrade eagerly above the threshold,
            // recover lazily at the lower band edge only.
            var wasDegraded = expectedDegraded;
            var degradesEagerly = count > ModConstants.DegradationTileThreshold;
            var staysDegraded = wasDegraded && count > ModConstants.DegradationRecoveryThreshold;
            expectedDegraded = degradesEagerly || staysDegraded;
            if (expectedDegraded && !wasDegraded)
            {
                expectedEventCount++;
            }

            // The frame-start decision governs the whole frame: every
            // tile carries the same pattern state.
            var expectedPattern = expectedDegraded ? PatternType.None : PatternType.Stripes;
            var frameConsistent = overlays.Count == count
                && overlays.All(overlay => overlay.PatternType == expectedPattern);
            allFramesConsistent &= frameConsistent;
        }

        // The notification fired exactly once per degradation event
        // and re-armed at the band edge, so its count equals the
        // number of false-to-true transitions of the hysteresis state.
        return allFramesConsistent && counter.Count == expectedEventCount;
    }

    /// <summary>
    /// Invariant: in "auto" mode the single decision path activates
    /// degradation only when the visible count strictly exceeds
    /// <see cref="ModConstants.DegradationTileThreshold"/> (FR-009).
    /// The decision state is per-screen instance state (T163,
    /// FR-026, DC-6), so the fresh renderer this invocation
    /// creates starts with an empty per-call-site store and the
    /// decision is evaluated from this count alone, isolated from
    /// the hysteresis carry and from other invocations; the
    /// frame-scoped call-site key keeps the decision keyed to
    /// this call site.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CheckDegradation_AutoMode_ActivatesOnlyAboveThreshold(int rawCount)
    {
        var count = MapToEdgeCount(rawCount);
        var configuration = new VisualizationConfiguration
        {
            AccessibilityDegradation = AutoMode,
            ShowPatterns = true
        };
        var renderer = CreateRenderer(configuration, new Mock<IHudNotifier>());
        var decision = renderer.CheckDegradation(count, AutoMode, "invariant-" + count);
        return decision == (count > ModConstants.DegradationTileThreshold);
    }

    private static OverlayRenderer CreateRenderer(
        VisualizationConfiguration configuration,
        Mock<IHudNotifier> hudNotifier)
    {
        var mockColorService = new Mock<IColorInterpolationService>();
        mockColorService.Setup(c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>()))
            .Returns(Color.White);
        mockColorService.Setup(c => c.GetColorForHealth(It.IsAny<float>()))
            .Returns(Color.White);
        mockColorService.Setup(c => c.GetCategoryForHealth(It.IsAny<float>()))
            .Returns(HealthCategory.Poor);

        var mockConfigService = new Mock<IVisualizationConfigurationService>();
        mockConfigService.Setup(c => c.GetConfiguration()).Returns(configuration);

        var mockMonitor = new Mock<IMonitor>();

        return new OverlayRenderer(
            mockColorService.Object,
            mockConfigService.Object,
            mockMonitor.Object,
            hudNotifier.Object);
    }

    private static Mock<IHudNotifier> CreateCountingNotifier(NotificationCounter counter)
    {
        var hudNotifier = new Mock<IHudNotifier>();
        hudNotifier.Setup(n => n.ShowNotification(It.IsAny<string>()))
            .Callback(counter.Record);
        return hudNotifier;
    }

    /// <summary>
    /// Builds a tile set whose size is exactly the requested count,
    /// laid out row-major inside a viewport large enough to hold every
    /// tile. Health is a fixed non-zero value so every visible tile
    /// produces an overlay whose pattern state exposes the degradation
    /// decision.
    /// </summary>
    private static Dictionary<Point, float> BuildTileDataOfSize(int count)
    {
        var data = new Dictionary<Point, float>(count);
        for (var i = 0; i < count; i++)
        {
            data[new Point(i % TileSpan, i / TileSpan)] = HealthValue;
        }

        return data;
    }

    /// <summary>
    /// Mutates a tile set in place to carry exactly the requested
    /// count, as the soil-health runtime cache mutates its snapshot
    /// between render passes.
    /// </summary>
    private static void SetTileCount(Dictionary<Point, float> data, int count)
    {
        data.Clear();
        for (var i = 0; i < count; i++)
        {
            data[new Point(i % TileSpan, i / TileSpan)] = HealthValue;
        }
    }

    private static int[] MapToEdgeCounts(int[] rawCounts)
    {
        return rawCounts.Select(MapToEdgeCount).Take(MaxFrameCount).ToArray();
    }

    private static int MapToEdgeCount(int rawCount)
    {
        return EdgeCounts[Mod(rawCount, EdgeCounts.Length)];
    }

    private static int Mod(int value, int modulus)
    {
        return ((value % modulus) + modulus) % modulus;
    }
}
