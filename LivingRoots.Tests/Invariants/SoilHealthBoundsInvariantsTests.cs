using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// T034a — SC-003 audit for spec 004 (Playable Compost Loop): 100% of the
/// soil-health values an observer can read back through
/// <see cref="SoilHealthService.GetSoilHealth"/> stay inside the inclusive
/// <see cref="ModConstants.MinSoilHealth"/>&nbsp;..&nbsp;<see cref="ModConstants.MaxSoilHealth"/>
/// band, no matter how wild the value written was or which composting-bin
/// lifecycle state the tile sits in. Every bound is sourced from
/// <see cref="ModConstants"/> — no health magic numbers are restated here.
/// </summary>
/// <remarks>
/// <para>
/// The clamp under audit is <c>SoilHealthService.ClampHealthValue</c>: finite
/// values are <c>Math.Clamp</c>ed into range, <c>float.PositiveInfinity</c>
/// maps to <see cref="ModConstants.MaxSoilHealth"/>, <c>float.NegativeInfinity"/>
/// maps to <see cref="ModConstants.MinSoilHealth"/>, and <c>float.NaN</c> is
/// dropped by the sparse cache — so a NaN-authored (or otherwise invalid)
/// tile reads back as the <c>NaN</c> unknown sentinel, which is the absence of
/// a reading rather than a fabricated value. Every finite reading lands inside
/// the band, which is exactly what these invariants assert.
/// </para>
/// <para>
/// Per-case isolation (T280(b) pattern): every property builds a fresh
/// <see cref="TimeProviderStub"/>, <see cref="CompostingBinService"/>,
/// <see cref="SoilHealthService"/> and inventory as method/lambda locals,
/// because a shared service leaks its per-location day-start guard and runtime
/// cache across FsCheck cases.
/// </para>
/// <para>
/// Wild floats are produced by <see cref="AnySoilHealthValue"/> and
/// <see cref="PositiveHealthDelta"/> rather than a raw <c>float</c> parameter:
/// FsCheck's default float arbitrary emits <c>float.MinValue</c>/<c>MaxValue</c>/<c>NaN</c>
/// and its shrinker explores negatives, which produced false failures earlier in
/// this suite.
/// </para>
/// </remarks>
public class SoilHealthBoundsInvariantsTests
{
    /// <summary>
    /// The far-out-of-range magnitude the exhaustive sweep probes beyond the
    /// <see cref="ModConstants.MinSoilHealth"/>/<see cref="ModConstants.MaxSoilHealth"/> bounds.
    /// </summary>
    private const float FarOutOfRangeMagnitude = 1000f;

    /// <summary>
    /// Upper bound on how many day-start advances a driven bin takes to reach
    /// <see cref="CompostingBinState.Ready"/>. Purely a safety cap so a regression
    /// that never matures fails an assertion instead of spinning forever.
    /// </summary>
    private const int MaxMaturationDayAdvances = 100;

    /// <summary>Installs the minimal game state the compost-application path needs.</summary>
    public SoilHealthBoundsInvariantsTests()
    {
        // Compost application (Game1.showRedMessage / playSound) and
        // GameLocationFixture (terrain features) both read Game1 statics; the
        // fixture allocates the process-global stand-ins once, idempotently.
        GameStateFixture.Install();
    }

    // ────────────────────────────────────────────────────────────────
    // SC-003 — property: any float value survives a Set → Get round trip
    // inside the bounds.
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// SC-003: for ANY float value written through
    /// <see cref="SoilHealthService.SetSoilHealth"/> — in-range, out-of-range,
    /// negative, or non-finite — the value read back through
    /// <see cref="SoilHealthService.GetSoilHealth"/> is within
    /// [<see cref="ModConstants.MinSoilHealth"/>, <see cref="ModConstants.MaxSoilHealth"/>].
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SetSoilHealth_ForAnyFloatValue_ObservedHealthWithinBounds()
    {
        return FsCheck.Prop.ForAll(
            Arb.From(AnySoilHealthValue()),
            value =>
            {
                var soil = CreateSoilHealthService();
                var tile = new Vector2(10, 10);

                soil.SetSoilHealth("Farm", tile, value);
                float observed = soil.GetSoilHealth("Farm", tile);

                return float.IsNaN(observed)
                    || (observed >= ModConstants.MinSoilHealth
                        && observed <= ModConstants.MaxSoilHealth);
            });
    }

    /// <summary>
    /// SC-003: for any starting health in range and any strictly-positive delta
    /// — including magnitudes far past the maximum and the ignored +∞ — a single
    /// <see cref="SoilHealthService.UpdateHealth"/> leaves the observed value within
    /// [<see cref="ModConstants.MinSoilHealth"/>, <see cref="ModConstants.MaxSoilHealth"/>].
    /// </summary>
    [Property(MaxTest = 100)]
    public Property UpdateHealth_ForAnyInRangeStartAndPositiveDelta_ObservedHealthWithinBounds()
    {
        return FsCheck.Prop.ForAll(
            Arb.From(FsCheckFixture.HealthValues()),
            Arb.From(PositiveHealthDelta()),
            (start, delta) =>
            {
                var soil = CreateSoilHealthService();
                var tile = new Vector2(10, 10);

                soil.SetSoilHealth("Farm", tile, start);
                soil.UpdateHealth("Farm", tile, delta);
                float observed = soil.GetSoilHealth("Farm", tile);

                return float.IsNaN(observed)
                    || (observed >= ModConstants.MinSoilHealth
                        && observed <= ModConstants.MaxSoilHealth);
            });
    }

    /// <summary>
    /// SC-003: driving the full composting loop — Empty → <see cref="CompostingBinService.AddWaste"/>
    /// → Processing → <see cref="CompostingBinService.ProcessDayStart"/> repeated until
    /// Ready → <see cref="CompostingBinService.CollectCompost"/> — and then applying the
    /// harvested compost through <see cref="CompostApplicationService.TryApplyCompost"/>
    /// keeps the observed soil health within
    /// [<see cref="ModConstants.MinSoilHealth"/>, <see cref="ModConstants.MaxSoilHealth"/>],
    /// for any starting health written to the tile.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FullBinLifecycleThenCompostApply_ForAnyStartHealth_ObservedHealthWithinBounds()
    {
        return FsCheck.Prop.ForAll(
            Arb.From(AnySoilHealthValue()),
            startHealth =>
            {
                // Per-case isolation: an entirely fresh service graph.
                var soil = CreateSoilHealthService();
                var inventory = new RecordingPlayerInventory();
                var timeStub = new TimeProviderStub { TotalDays = 1 };
                var binService = CreateBinService(inventory, timeStub);
                var playerStub = new PlayerProviderStub();
                var appService = new CompostApplicationService(
                    soil,
                    playerStub,
                    new Mock<IMonitor>().Object);

                const string location = "Farm";
                var tile = new Vector2(10, 10);
                var farmFixture = new GameLocationFixture(location);
                farmFixture.AddHoeDirtTile(10, 10);

                // The arbitrary starting point (already clamped on write).
                soil.SetSoilHealth(location, tile, startHealth);

                // Empty → Processing → Ready → exactly one compost, into the inventory.
                DriveBinToState(binService, timeStub, location, tile, CompostingBinState.Ready);
                Assert.Equal(1, binService.CollectCompost(location, tile));
                Assert.Single(inventory.AddedItems);

                // Side-dress the very same tilled tile with the harvested compost.
                // The apply is refused at max health and succeeds below it; either
                // way the observed value must stay inside the band.
                playerStub.CurrentItem = inventory.AddedItems[0];
                appService.TryApplyCompost(farmFixture.Location, tile);

                float observed = soil.GetSoilHealth(location, tile);
                return float.IsNaN(observed)
                    || (observed >= ModConstants.MinSoilHealth
                        && observed <= ModConstants.MaxSoilHealth);
            });
    }

    // ────────────────────────────────────────────────────────────────
    // SC-003 — exhaustive fact sweep of health values.
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// SC-003 (exhaustive): every attainable whole-number health 0..max, the
    /// just-outside boundaries (min−1, max+1), far-out-of-range magnitudes, and
    /// the non-finite triple (NaN / +∞ / −∞) all read back within
    /// [<see cref="ModConstants.MinSoilHealth"/>, <see cref="ModConstants.MaxSoilHealth"/>].
    /// </summary>
    [Fact]
    public void ExhaustiveHealthValueSweep_ObservedValueAlwaysWithinBounds()
    {
        foreach (float value in BuildExhaustiveHealthValues())
        {
            var soil = CreateSoilHealthService();
            var tile = new Vector2(10, 10);

            soil.SetSoilHealth("Farm", tile, value);
            float observed = soil.GetSoilHealth("Farm", tile);

            // A dropped invalid value is the NaN unknown sentinel (no
            // reading), not an out-of-band measured value.
            if (float.IsNaN(observed))
            {
                continue;
            }

            Assert.InRange(observed, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // SC-003 — fact: the invariant holds in every bin lifecycle state.
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// SC-003: across every composting-bin lifecycle state — Empty, Processing and
    /// Ready — a set-then-apply cycle on a tilled farm tile leaves the observed
    /// soil health within
    /// [<see cref="ModConstants.MinSoilHealth"/>, <see cref="ModConstants.MaxSoilHealth"/>].
    /// </summary>
    [Fact]
    public void CompostApply_AcrossEveryBinLifecycleState_ObservedHealthWithinBounds()
    {
        var binStates = new[]
        {
            CompostingBinState.Empty,
            CompostingBinState.Processing,
            CompostingBinState.Ready
        };
        var startingHealths = new[]
        {
            ModConstants.MinSoilHealth,
            ModConstants.MaxSoilHealth / 2f,
            ModConstants.MaxSoilHealth
        };

        foreach (CompostingBinState binState in binStates)
        {
            foreach (float startHealth in startingHealths)
            {
                SetThenApplyInBinState_ObservedHealthWithinBounds(binState, startHealth);
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Drives a bin to <paramref name="binState"/>, then runs a fresh
    /// set-then-apply cycle on a tilled tile and asserts the observed soil
    /// health stays within the SC-003 band.
    /// </summary>
    private static void SetThenApplyInBinState_ObservedHealthWithinBounds(
        CompostingBinState binState,
        float startHealth)
    {
        // Per-case isolation: an entirely fresh service graph.
        var soil = CreateSoilHealthService();
        var inventory = new RecordingPlayerInventory();
        var timeStub = new TimeProviderStub { TotalDays = 1 };
        var binService = CreateBinService(inventory, timeStub);
        var playerStub = new PlayerProviderStub();
        var appService = new CompostApplicationService(
            soil,
            playerStub,
            new Mock<IMonitor>().Object);

        const string location = "Farm";
        var tile = new Vector2(10, 10);
        var farmFixture = new GameLocationFixture(location);
        farmFixture.AddHoeDirtTile(10, 10);

        DriveBinToState(binService, timeStub, location, tile, binState);
        Assert.Equal(binState, binService.GetBinState(location, tile));

        soil.SetSoilHealth(location, tile, startHealth);
        playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        appService.TryApplyCompost(farmFixture.Location, tile);

        float observed = soil.GetSoilHealth(location, tile);
        Assert.InRange(observed, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
    }

    /// <summary>
    /// Builds the exhaustive health-value list: 0..<see cref="ModConstants.MaxSoilHealth"/>,
    /// the just-outside boundaries, the far-out-of-range magnitudes, and the
    /// non-finite triple.
    /// </summary>
    private static List<float> BuildExhaustiveHealthValues()
    {
        var values = new List<float>();

        for (int health = (int)ModConstants.MinSoilHealth; health <= (int)ModConstants.MaxSoilHealth; health++)
        {
            values.Add(health);
        }

        values.Add(ModConstants.MinSoilHealth - 1f);
        values.Add(ModConstants.MaxSoilHealth + 1f);
        values.Add(-FarOutOfRangeMagnitude);
        values.Add(FarOutOfRangeMagnitude);
        values.Add(float.NaN);
        values.Add(float.PositiveInfinity);
        values.Add(float.NegativeInfinity);

        return values;
    }

    /// <summary>
    /// A deliberately "wild" health value: dense in-range values, out-of-range
    /// magnitudes and negatives, plus the non-finite triple NaN / +∞ / −∞. These
    /// are exactly the inputs the [<see cref="ModConstants.MinSoilHealth"/>,
    /// <see cref="ModConstants.MaxSoilHealth"/>] bound must survive.
    /// </summary>
    private static Gen<float> AnySoilHealthValue()
    {
        var inRange = FsCheckFixture.HealthValues();
        var outOfRange = Gen.Choose(-1000, 1000).Select(i => (float)i);
        var nonFinite = Gen.Elements(new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity });

        return Gen.Frequency<float>(
            new WeightAndValue<Gen<float>>(6, inRange),
            new WeightAndValue<Gen<float>>(3, outOfRange),
            new WeightAndValue<Gen<float>>(1, nonFinite));
    }

    /// <summary>
    /// Any strictly-positive delta: small steps through magnitudes that blow far
    /// past <see cref="ModConstants.MaxSoilHealth"/> (forcing the clamp), plus +∞,
    /// which <see cref="SoilHealthService.UpdateHealth"/> ignores — leaving the
    /// clamped start behind, still in range.
    /// </summary>
    private static Gen<float> PositiveHealthDelta()
    {
        var finite = Gen.Choose(1, 1000000).Select(i => (float)i);
        var infinite = Gen.Elements(new[] { float.PositiveInfinity });

        return Gen.Frequency<float>(
            new WeightAndValue<Gen<float>>(4, finite),
            new WeightAndValue<Gen<float>>(1, infinite));
    }

    /// <summary>
    /// Advances a bin to <paramref name="targetState"/>. Empty leaves it untouched;
    /// Processing deposits one waste item (0 elapsed days keeps it Processing);
    /// Ready deposits waste then runs <see cref="CompostingBinService.ProcessDayStart"/>
    /// day-by-day until the batch matures.
    /// </summary>
    private static void DriveBinToState(
        CompostingBinService binService,
        TimeProviderStub timeStub,
        string location,
        Vector2 tile,
        CompostingBinState targetState)
    {
        if (targetState == CompostingBinState.Empty)
        {
            return; // No waste deposited — the bin is, and stays, Empty.
        }

        binService.AddWaste(location, tile, ItemFactory.CreateValidWasteItem());
        if (targetState == CompostingBinState.Processing)
        {
            return; // Fresh batch: zero elapsed days keeps it Processing.
        }

        int day = timeStub.TotalDays;
        int guard = 0;
        while (binService.GetBinState(location, tile) == CompostingBinState.Processing && guard < MaxMaturationDayAdvances)
        {
            guard++;
            day++;
            timeStub.TotalDays = day;
            binService.ProcessDayStart(location);
        }
    }

    /// <summary>Constructs a fresh <see cref="SoilHealthService"/> over mocked collaborators.</summary>
    private static SoilHealthService CreateSoilHealthService()
    {
        return new SoilHealthService(
            new Mock<IModDataService>().Object,
            new Mock<IMonitor>().Object,
            new Mock<IFileNameSanitizationService>().Object);
    }

    /// <summary>Constructs a fresh <see cref="CompostingBinService"/> whose validator accepts every waste item.</summary>
    private static CompostingBinService CreateBinService(IPlayerInventory inventory, ITimeProvider timeProvider)
    {
        var mockWasteValidator = new Mock<IOrganicWasteValidator>();
        mockWasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

        return new CompostingBinService(
            new Mock<IModDataService>().Object,
            new Mock<ISaveIdProvider>().Object,
            mockWasteValidator.Object,
            new Mock<IMonitor>().Object,
            timeProvider,
            new CompostingBinFactory(),
            inventory);
    }
}

// T280(b) note: every property here builds fresh TimeProviderStub +
// CompostingBinService + SoilHealthService (+ inventory) as method/lambda
// locals per FsCheck case — a shared service graph leaks its per-location
// day-start guard and runtime cache across cases.
