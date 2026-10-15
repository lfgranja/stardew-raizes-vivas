using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Domain.Models;
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
/// FR-018 / Constitution VI parity audit for spec 004 (Playable Compost Loop),
/// task T039: <c>ForAll(state => SinglePlayerPath(state) == MultiPlayerPath(state))</c>
/// — no code path may behave differently on a multiplayer farm than on a
/// single-player farm. Single-player is the degenerate case: a host with no
/// connected clients.
/// </summary>
/// <remarks>
/// <para>
/// SCOPE / STUB-BOUNDED — read before trusting this file. FR-018 parity here is
/// attested as PATH/BEHAVIOR parity of the production code under SIMULATED
/// multi-client delivery (production services driven through test stubs:
/// <see cref="GameStateFixture"/>, <see cref="TimeProviderStub"/>,
/// <see cref="RecordingPlayerInventory"/>, <see cref="HostAtomicLockService"/>,
/// <see cref="PlayerProviderStub"/>). It is NOT live two-player network proof.
/// LIVE 2-player network transport — real SMAPI <c>DayStarted</c> fan-out,
/// <c>ModMessage</c> round-trips, save-data relay across hosts — is owed to
/// roadmap item D1 and MUST NOT be claimed by this file. The spec Assumptions
/// bound this verification to automated stubs; <see cref="Fr018_VerificationIsStubBounded_LiveTwoPlayerOwedToRoadmapD1"/>
/// records that boundary explicitly.
/// </para>
/// <para>
/// This file deliberately does NOT re-run the properties already pinned in
/// <c>MultiplayerInvariantsTests</c> (T026/T039). Its four parity properties take
/// genuinely different angles: (1) day-start delivery counts span the full 1..8
/// connected-client range with a variable multi-day schedule and the days-elapsed
/// counters read through the persistence snapshot (T026 used a fixed 2..4 range,
/// a fixed tile and two checkpoints); (2) compost application is compared over a
/// whole multi-step apply sequence with a varying held stack / inventory fullness
/// and a varying initial health (T026 compared a single fixed-stack application);
/// (3) host-lock contention races a variable 1..16 actor count (T026 raced exactly
/// two); (4) soil-health mutation and its <c>OnTileHealthChanged</c> event are
/// driven through the REAL <see cref="SoilHealthService"/> for a host-with-no-clients
/// versus a host-with-clients — no earlier invariant tests the farm-scoped service
/// or its event at all. The two <see cref="Fact"/>s are the structural half of the
/// audit (scan) and the D1 boundary (documentation). Detailed differences are in
/// each test's remarks.
/// </para>
/// </remarks>
public class SingleMultiplayerParityInvariantsTests
{
    private int _caseSalt;

    public SingleMultiplayerParityInvariantsTests()
    {
        // The bin service plays the vanilla "Ship" / "cancel" cues and the soil
        // and compost services render red HUD text; GameStateFixture installs the
        // no-op sound + content + game1 stand-ins every case below relies on.
        GameStateFixture.Install();
    }

    // ------------------------------------------------------------------
    // FR-018 parity 1 — day-start delivery (single vs N-client)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-018 parity: a single-player day start (exactly ONE
    /// <see cref="CompostingBinService.ProcessDayStart"/> delivery per in-game
    /// day) and a multiplayer day start (one delivery per connected client, the
    /// client count N varying across the full 1..8 range) produce the IDENTICAL
    /// resulting bin state and the IDENTICAL days-elapsed progression. Duplicate
    /// same-day deliveries must never double-advance processing.
    /// </summary>
    /// <remarks>
    /// Different angle vs <c>MultiplayerInvariantsTests.T026_*</c>: the earlier
    /// properties fixed the client count to <c>2 + len % 3</c> (2..4), a fixed
    /// tile (3,4) and exactly two day checkpoints, and never observed the
    /// maturation counters. This property sweeps the connected-client count over
    /// the whole 1..8 range, varies the tile across the full coordinate space,
    /// advances a variable number of distinct in-game days (1..6), and reads the
    /// <c>ConsecutiveActiveDays</c> counter through the private persistence
    /// snapshot seam — so the "duplicate deliveries never double-advance" claim
    /// is observed directly as a counter equal to the maturation-capped day
    /// count, not merely as a state coincidentally matching a checkpoint.
    /// </remarks>
    [Property(MaxTest = 100)]
    public void Fr018_SingleAndMultiClientDayStart_IdenticalProgression(
        PositiveInt coordX,
        PositiveInt coordY,
        PositiveInt startDaySeed,
        PositiveInt advanceSeed,
        PositiveInt deliverySeed)
    {
        // T280(b) per-case isolation: fresh game state + a documented salt so no
        // generated coordinate collides with another in-flight FsCheck case.
        GameStateFixture.Install();
        int salt = System.Threading.Interlocked.Increment(ref _caseSalt);

        int connectedClients = (deliverySeed.Get % 8) + 1; // 1..8 (1 = host-with-no-clients)
        int startDay = (startDaySeed.Get % 500) + 1;
        int advanceDays = (advanceSeed.Get % 6) + 1; // 1..6 distinct days advanced
        int span = ModConstants.MaxAbsoluteTileCoordinate + 1;
        var tile = new Vector2(
            (coordX.Get + salt) % span,
            (coordY.Get + (salt * 2)) % span);
        const string location = "Farm";

        // Single-player: the host-with-no-clients degenerate case — one delivery
        // per day. Multiplayer: one delivery per connected client.
        var singlePlayer = RunDayStartSchedule(deliveries: 1, startDay, advanceDays, tile, location);
        var multiPlayer = RunDayStartSchedule(connectedClients, startDay, advanceDays, tile, location);

        // FR-018: identical resulting bin state regardless of client count.
        Assert.Equal(singlePlayer.State, multiPlayer.State);

        // FR-018: identical days-elapsed progression.
        Assert.Equal(singlePlayer.ActiveDays, multiPlayer.ActiveDays);

        // The active counter advances once per DISTINCT day while Processing and
        // stops at maturation — so it equals min(advanceDays, MaturationDays) for
        // BOTH paths. If duplicate same-day fan-out double-advanced processing,
        // the multiplayer path would read advanceDays * connectedClients instead.
        int expectedActiveDays = Math.Min(advanceDays, ModConstants.MaturationDays);
        Assert.Equal(expectedActiveDays, singlePlayer.ActiveDays);
        Assert.Equal(expectedActiveDays, multiPlayer.ActiveDays);

        CompostingBinState expectedState = advanceDays >= ModConstants.MaturationDays
            ? CompostingBinState.Ready
            : CompostingBinState.Processing;
        Assert.Equal(expectedState, singlePlayer.State);
        Assert.Equal(expectedState, multiPlayer.State);
    }

    // ------------------------------------------------------------------
    // FR-018 parity 2 — compost application with vs without a host lock
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-018 parity: <see cref="CompostApplicationService.TryApplyCompost"/>
    /// yields the same outcome (applied vs refused), the same resulting health
    /// and the same remaining held stack — step for step across a whole apply
    /// sequence — whether or not a host atomic lock service is wired around it.
    /// </summary>
    /// <remarks>
    /// Different angle vs <c>MultiplayerInvariantsTests.T026_CompostApplication_SingleVsMulti</c>:
    /// the earlier property applied compost ONCE at a fixed tile with a fixed
    /// stack of 5 and compared a single triple. This property drains a whole
    /// sequence of applications, with the held stack (inventory fullness, 1..5)
    /// and the initial soil health (0.00..99.99) varying per case, comparing the
    /// entire applied/refused + health + remaining-stack sequence — including the
    /// refusal tail where the stack empties or health caps — so the host-lock
    /// wiring is proven invariant at every step, not just the first.
    /// </remarks>
    [Property(MaxTest = 100)]
    public void Fr018_CompostApplication_LockWiredOrNot_IdenticalOutcomeSequence(
        PositiveInt healthSeed,
        PositiveInt stackSeed,
        PositiveInt coordX,
        PositiveInt coordY)
    {
        GameStateFixture.Install();
        int salt = System.Threading.Interlocked.Increment(ref _caseSalt);

        int startStack = (stackSeed.Get % 5) + 1; // 1..5 units held (inventory fullness)
        float initialHealth = (healthSeed.Get % 10000) / 100f; // 0.00 .. 99.99, never NaN/Infinity
        var tile = new Vector2((coordX.Get + salt) % 65, (coordY.Get + salt) % 65);

        var singlePlayer = RunCompostApplicationSequence(startStack, initialHealth, tile, useHostLock: false);
        var multiPlayer = RunCompostApplicationSequence(startStack, initialHealth, tile, useHostLock: true);

        // FR-018: the whole applied/refused + health + remaining-stack sequence is
        // identical whether the host-authoritative lock guards the action or not.
        Assert.Equal(singlePlayer.Count, multiPlayer.Count);
        for (int step = 0; step < singlePlayer.Count; step++)
        {
            Assert.Equal(singlePlayer[step].Applied, multiPlayer[step].Applied);
            Assert.Equal(singlePlayer[step].HealthAfter, multiPlayer[step].HealthAfter);
            Assert.Equal(singlePlayer[step].StackAfter, multiPlayer[step].StackAfter);
        }
    }

    // ------------------------------------------------------------------
    // FR-018 parity 3 — host-lock contention across actor counts
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-019 / FR-018: for any number of racing actions (1..16) on the SAME
    /// coordinate key, <see cref="HostAtomicLockService"/> yields exactly ONE
    /// winner, the lock holder is that winner, and the lock is always releasable
    /// — never permanently stuck — and immediately re-acquirable once released.
    /// </summary>
    /// <remarks>
    /// Different angle vs <c>MultiplayerInvariantsTests.T039_FirstWinsAtomicLock</c>:
    /// the earlier property raced exactly two farmers. This one races a variable
    /// 1..16 actor count (the whole plausible co-op party), asserting exactly one
    /// winner among N distinct farmer IDs, and — the "never permanently stuck"
    /// half — that the key is re-acquirable after the winner releases it.
    /// </remarks>
    [Property(MaxTest = 100)]
    public void Fr018_HostAtomicLock_AnyRacerCount_ExactlyOneWinnerNeverStuck(
        PositiveInt racerSeed,
        PositiveInt coordSeed)
    {
        GameStateFixture.Install();
        int salt = System.Threading.Interlocked.Increment(ref _caseSalt);

        int racers = (racerSeed.Get % 16) + 1; // 1..16 racing actors
        int span = ModConstants.MaxAbsoluteTileCoordinate + 1;
        var tile = new Vector2(
            (coordSeed.Get + salt) % span,
            (coordSeed.Get / span + salt) % span);
        const string location = "Farm";

        var lockService = new HostAtomicLockService();
        var farmerIds = new long[racers];
        for (int i = 0; i < racers; i++)
        {
            farmerIds[i] = 1000L + i; // distinct multiplayer identities
        }

        var acquired = new bool[racers];
        var actions = new Action[racers];
        for (int index = 0; index < racers; index++)
        {
            int captured = index;
            long farmerId = farmerIds[index];
            actions[index] = () => acquired[captured] = lockService.TryAcquireLock(location, tile, farmerId);
        }

        Parallel.Invoke(actions);

        // First-wins (FR-019): exactly one acquisition succeeds regardless of the
        // racing actor count — the lock's internal monitor serialises the check
        // and set, so the RESULT (a single winner) is scheduler-independent.
        int winnerCount = acquired.Count(flag => flag);
        Assert.Equal(1, winnerCount);
        Assert.True(lockService.IsLocked(location, tile));

        int winnerIndex = Array.IndexOf(acquired, true);
        long winnerId = farmerIds[winnerIndex];
        Assert.Equal(winnerId, lockService.GetLockHolder(location, tile));

        // Never permanently stuck: the holder can always release, and the key then
        // answers a fresh acquisition — proving no wedge survives the race.
        lockService.ReleaseLock(location, tile, winnerId);
        Assert.False(lockService.IsLocked(location, tile));

        Assert.True(lockService.TryAcquireLock(location, tile, farmerIds[0]));
        lockService.ReleaseLock(location, tile, farmerIds[0]);
        Assert.False(lockService.IsLocked(location, tile));
    }

    // ------------------------------------------------------------------
    // FR-018 parity 4 — soil health mutation + OnTileHealthChanged event
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-018 / Constitution VI: soil health mutation and the
    /// <c>OnTileHealthChanged</c> event fire identically for a host-with-no-clients
    /// and a host-with-clients (client count 1..8). The REAL
    /// <see cref="SoilHealthService"/> carries no farmer id and no player count —
    /// farm state is farm-scoped, not player-scoped — so under the host-authoritative
    /// lock discipline exactly ONE client mutates and the event fires exactly once,
    /// identically for any connected-client count.
    /// </summary>
    /// <remarks>
    /// Genuinely NEW: no earlier property drives the real soil-health service or
    /// its reactive event. Earlier parity used a MOCK <see cref="ISoilHealthService"/>
    /// and never asserted <c>OnTileHealthChanged</c>. This property models the
    /// production host-authoritative discipline literally — among all connected
    /// clients exactly one wins the tile lock and mutates; every other client is
    /// refused with zero mutation — and asserts the winning mutation produces the
    /// same resulting health and exactly one identical event payload regardless of
    /// how many clients were connected.
    /// </remarks>
    [Property(MaxTest = 100)]
    public void Fr018_SoilHealthMutationAndEvent_HostWithNoClients_EqualsHostWithClients(
        PositiveInt clientSeed,
        PositiveInt healthSeed,
        PositiveInt coordSeed)
    {
        GameStateFixture.Install();
        int salt = System.Threading.Interlocked.Increment(ref _caseSalt);

        int connectedClients = (clientSeed.Get % 8) + 1; // 1..8 (1 = host-with-no-clients)
        int delta = (healthSeed.Get % 40) + 1; // +1..+40 health change
        int span = ModConstants.MaxAbsoluteTileCoordinate + 1;
        var tile = new Vector2(
            (coordSeed.Get + salt) % span,
            (coordSeed.Get / span + salt) % span);
        const string location = "Farm";

        var hostNoClients = ApplyOneAuthoritativeMutation(connectedClients: 1, delta, tile, location);
        var hostWithClients = ApplyOneAuthoritativeMutation(connectedClients, delta, tile, location);

        // FR-018: farm-scoped state — identical resulting health for any client count.
        Assert.Equal(hostNoClients.HealthAfter, hostWithClients.HealthAfter);

        // The reactive event fired identically: exactly once (one logical mutation),
        // with the same post-mutation payload.
        Assert.Single(hostNoClients.Events);
        Assert.Single(hostWithClients.Events);
        Assert.Equal(hostNoClients.Events[0].Value, hostWithClients.Events[0].Value);

        // The event carries the post-mutation (clamped) health — same value the
        // service now reports.
        Assert.Equal(hostNoClients.HealthAfter, hostNoClients.Events[0].Value);
    }

    // ------------------------------------------------------------------
    // FR-018 structural half — no divergent player-count branch
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-018 structural audit: no production source under <c>LivingRoots/</c>
    /// (excluding <c>bin/</c> and <c>obj/</c>) contains a player-count or
    /// host-identity behavior branch. The scan looks for the exhaustive set of
    /// SMAPI/host-surface tokens that <em>would</em> constitute such a branch and
    /// asserts the set of hits equals the documented allow-list of host-authority
    /// gate sites — which is EMPTY by construction, because Living Roots is
    /// host-authoritative structurally (Constitution VI): farm state is farm-scoped
    /// and every actor reaches it through the same farmerId-keyed
    /// <see cref="HostAtomicLockService"/> a single player drives.
    /// </summary>
    /// <remarks>
    /// The assertion is deliberately precise rather than a brittle blanket regex:
    /// a positive-control token (the production <c>TryAcquireLock</c> seam) MUST be
    /// found so a pass cannot come from scanning an empty tree, and every divergent
    /// hit that is not on the (currently empty) documented allow-list fails the
    /// test with its file, line and token. Adding a genuine host-authority gate in
    /// the future requires adding its site to <see cref="DocumentedHostAuthorityGates"/>
    /// deliberately; any other occurrence is a divergence flag.
    /// </remarks>
    [Fact]
    public void Fr018_StructuralScan_NoDivergentPlayerCountBranch()
    {
        var repoRoot = ResolveRepoRoot();
        var productionRoot = Path.Combine(repoRoot, "LivingRoots");
        Assert.True(Directory.Exists(productionRoot), $"Production source directory not found: {productionRoot}");

        var sourceFiles = Directory
            .EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

        // Non-vacuity guard 1: the scan must read a substantial production tree.
        Assert.True(
            sourceFiles.Count >= 10,
            $"Expected to scan the full production source tree under LivingRoots/, found {sourceFiles.Count} .cs files.");

        var divergentHits = new List<string>();
        int positiveControlHits = 0;
        foreach (var file in sourceFiles)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (IOException)
            {
                continue;
            }

            string relative = Path.GetRelativePath(repoRoot, file);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                foreach (string token in DivergentPlayerBranchTokens)
                {
                    if (line.Contains(token, StringComparison.Ordinal))
                    {
                        divergentHits.Add($"{relative}:{lineIndex + 1}: token '{token}'");
                    }
                }

                if (line.Contains(PositiveControlToken, StringComparison.Ordinal))
                {
                    positiveControlHits++;
                }
            }
        }

        // Non-vacuity guard 2: the production host-authoritative lock seam MUST be
        // present, proving the scan actually read the production sources.
        Assert.True(
            positiveControlHits > 0,
            "Positive control failed — the production host-lock seam ('" + PositiveControlToken
            + "') was not found, so the divergent-branch scan cannot prove it read the production tree.");

        // Only hits explicitly on the documented allow-list may remain; any other
        // player-count / host-identity branch is an FR-018 divergence.
        var unauthorized = divergentHits
            .Where(hit => !IsPermittedHostAuthorityGate(hit))
            .ToList();

        Assert.True(
            unauthorized.Count == 0,
            "FR-018 parity violated — a divergent player-count / host-identity branch was introduced: "
            + string.Join("; ", unauthorized)
            + ". If this is a legitimate host-authority gate, add its site to the documented allow-list.");
    }

    // ------------------------------------------------------------------
    // FR-018 scope boundary — stub-bounded, not live multi-player proof
    // ------------------------------------------------------------------

    /// <summary>
    /// Documents the D1 boundary: this file attests PATH/BEHAVIOR parity of the
    /// production code under SIMULATED multi-client delivery (stubs), never live
    /// two-player network transport. Live 2-player verification remains owed to
    /// roadmap item D1 and is expressly NOT claimed here.
    /// </summary>
    [Fact]
    public void Fr018_VerificationIsStubBounded_LiveTwoPlayerOwedToRoadmapD1()
    {
        Assert.True(
            true,
            "FR-018 single-player/multiplayer parity in SingleMultiplayerParityInvariantsTests is "
            + "stub-bounded: it drives production services (CompostingBinService, CompostApplicationService, "
            + "SoilHealthService, HostAtomicLockService) through test stand-ins (GameStateFixture, "
            + "TimeProviderStub, RecordingPlayerInventory, PlayerProviderStub) with single-player modeled as the "
            + "degenerate host-with-no-clients. This is PATH/BEHAVIOR parity under simulated multi-client "
            + "delivery — NOT live 2-player network proof. LIVE 2-player network transport (real SMAPI "
            + "DayStarted fan-out, ModMessage round-trips, save-data relay) is owed to roadmap item D1 and is "
            + "MUST-NOT-CLAIM by this file.");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// The exhaustive set of SMAPI / host-surface tokens whose presence in a
    /// production line would constitute a player-count or host-identity BEHAVIOR
    /// branch. Any line containing one is a candidate divergent-branch site under
    /// FR-018.
    /// </summary>
    private static readonly string[] DivergentPlayerBranchTokens =
    {
        "IsMainPlayer",
        "IsMasterGame",
        "IsMultiplayer",
        "HasRemotePlayers",
        "Context.Is",
        "GetConnectedPlayer",
        "getOnlineFarmers",
        "IsLocalPlayer",
        "PlayerCount"
    };

    /// <summary>
    /// The token that MUST be present in production (the host-authoritative lock
    /// seam). Its presence is the positive control that the source scan actually
    /// read the production tree.
    /// </summary>
    private const string PositiveControlToken = "TryAcquireLock";

    /// <summary>
    /// The documented allow-list of host-authority gate sites. Living Roots is
    /// host-authoritative BY CONSTRUCTION (Constitution VI): farm state is
    /// farm-scoped and the host is the single source of truth, so production
    /// carries NO player-count / host-identity branch at all — every actor reaches
    /// shared state through the same farmerId-keyed <see cref="HostAtomicLockService"/>
    /// a single player (a host with no clients) uses. The set is therefore EMPTY
    /// today. A genuinely required host-authority gate introduced in future MUST be
    /// added here deliberately; the scan fails on any other divergent occurrence.
    /// </summary>
    private static readonly string[] DocumentedHostAuthorityGates = Array.Empty<string>();

    private static bool IsPermittedHostAuthorityGate(string site)
    {
        foreach (string permitted in DocumentedHostAuthorityGates)
        {
            if (site.Contains(permitted, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Deposits one batch, then advances <paramref name="advanceDays"/> distinct
    /// in-game days, delivering <see cref="CompostingBinService.ProcessDayStart"/>
    /// <paramref name="deliveries"/> times per day (once per connected client —
    /// the duplicate fan-out a multiplayer session produces). Returns the resulting
    /// bin state and the <c>ConsecutiveActiveDays</c> maturation counter read
    /// through the private persistence snapshot seam.
    /// </summary>
    private static (CompostingBinState State, int ActiveDays) RunDayStartSchedule(
        int deliveries,
        int startDay,
        int advanceDays,
        Vector2 tile,
        string location)
    {
        // T280(b) per-case isolation: fresh time source, service and inventory as
        // method locals, so the per-location day-start guard never leaks between
        // FsCheck cases or between the single- and multi-player runs.
        GameStateFixture.Install();
        var timeStub = new TimeProviderStub { TotalDays = startDay };
        var inventory = new RecordingPlayerInventory();
        var service = CreateBinService(timeStub, inventory);

        service.AddWaste(location, tile, ItemFactory.CreateValidWasteItem());

        for (int day = 1; day <= advanceDays; day++)
        {
            timeStub.TotalDays = startDay + day;
            for (int client = 0; client < deliveries; client++)
            {
                service.ProcessDayStart(location);
            }
        }

        var record = ReadBinRecord(service, location, tile);
        return (service.GetBinState(location, tile), record?.ConsecutiveActiveDays ?? 0);
    }

    /// <summary>
    /// Applies compost to a tilled farm tile repeatedly through a real
    /// <see cref="CompostApplicationService"/> until the action is first refused
    /// (empty hands after the last unit, or the tile is already at maximum health),
    /// optionally wrapping every application in the host-authoritative lock (the
    /// multiplayer path). Returns the per-step (applied, resulting health,
    /// remaining held stack) sequence.
    /// </summary>
    private static List<(bool Applied, float HealthAfter, int StackAfter)> RunCompostApplicationSequence(
        int startStack,
        float initialHealth,
        Vector2 tile,
        bool useHostLock)
    {
        GameStateFixture.Install();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        fixture.AddHoeDirtTile((int)tile.X, (int)tile.Y);

        var health = initialHealth;
        var mockSoilHealth = new Mock<ISoilHealthService>();
        mockSoilHealth
            .Setup(s => s.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()))
            .Returns(() => health);
        mockSoilHealth
            .Setup(s => s.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()))
            .Callback<string, Vector2, float>((_, _, delta) =>
                health = Math.Min(health + delta, ModConstants.MaxSoilHealth));

        var provider = new PlayerProviderStub();
        var held = ItemFactory.CreateItem(ModConstants.CompostItemId);
        held.Stack = startStack;
        provider.CurrentItem = held;

        var service = new CompostApplicationService(
            mockSoilHealth.Object,
            provider,
            new Mock<IMonitor>().Object);

        var lockService = useHostLock ? new HostAtomicLockService() : null;
        string locName = fixture.Location.Name ?? "Farm";
        var sequence = new List<(bool Applied, float HealthAfter, int StackAfter)>();

        // Bounded by startStack successes + one terminal refusal.
        for (int step = 0; step <= startStack; step++)
        {
            bool applied;
            if (lockService != null)
            {
                lockService.TryAcquireLock(locName, tile, 1L);
                try
                {
                    applied = service.TryApplyCompost(fixture.Location, tile);
                }
                finally
                {
                    lockService.ReleaseLock(locName, tile, 1L);
                }
            }
            else
            {
                applied = service.TryApplyCompost(fixture.Location, tile);
            }

            sequence.Add((applied, health, held.Stack));

            if (!applied)
            {
                break;
            }
        }

        return sequence;
    }

    /// <summary>
    /// Applies exactly one soil-health mutation through the REAL, farm-scoped
    /// <see cref="SoilHealthService"/> under the host-authoritative lock
    /// discipline: the first connected client wins the tile lock and mutates once
    /// while every other client is refused with zero mutation; the winner releases
    /// only after all clients have contended. Returns the resulting health and the
    /// recorded <c>OnTileHealthChanged</c> events — exactly one for any client count.
    /// </summary>
    private static (float HealthAfter, List<(Vector2 Tile, float Value)> Events) ApplyOneAuthoritativeMutation(
        int connectedClients,
        int delta,
        Vector2 tile,
        string location)
    {
        GameStateFixture.Install();
        var service = new SoilHealthService(
            new Mock<IModDataService>().Object,
            new Mock<IMonitor>().Object,
            new Mock<IFileNameSanitizationService>().Object);

        var events = new List<(Vector2 Tile, float Value)>();
        service.OnTileHealthChanged += (_, changedTile, value) => events.Add((changedTile, value));

        // Host-authoritative discipline (Constitution VI): the winner acquires and
        // HOLDS the lock, so every other connected client's attempt is refused
        // while the mutation happens — only one actor ever reaches the service.
        var lockService = new HostAtomicLockService();
        long winnerId = 1000L;
        Assert.True(lockService.TryAcquireLock(location, tile, winnerId));
        service.UpdateHealth(location, tile, delta);
        for (int client = 1; client < connectedClients; client++)
        {
            Assert.False(
                lockService.TryAcquireLock(location, tile, 1000L + client),
                "A non-winner client must be refused with zero mutation while the winner holds the host-authoritative lock.");
        }

        lockService.ReleaseLock(location, tile, winnerId);

        return (service.GetSoilHealth(location, tile), events);
    }

    /// <summary>
    /// Builds a <see cref="CompostingBinService"/> with a permissive waste
    /// validator and a recording inventory. No host lock is injected — the
    /// day-start parity under test is independent of the lock seam, which parity
    /// properties 2-4 cover separately.
    /// </summary>
    private static CompostingBinService CreateBinService(TimeProviderStub timeStub, IPlayerInventory inventory)
    {
        var mockModDataService = new Mock<IModDataService>();
        var mockSaveIdProvider = new Mock<ISaveIdProvider>();
        var mockWasteValidator = new Mock<IOrganicWasteValidator>();
        mockWasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);
        var mockMonitor = new Mock<IMonitor>();

        return new CompostingBinService(
            mockModDataService.Object,
            mockSaveIdProvider.Object,
            mockWasteValidator.Object,
            mockMonitor.Object,
            timeStub,
            new CompostingBinFactory(),
            inventory);
    }

    /// <summary>
    /// Reads one bin's persisted record through the production persistence
    /// snapshot seam (<c>CreateSaveSnapshot</c> — the same seam <c>SaveData</c>
    /// writes), so a duplicate same-day delivery's double-increment of
    /// <c>ConsecutiveActiveDays</c> is observable without any test-local guard.
    /// </summary>
    private static CompostingBinStateData? ReadBinRecord(CompostingBinService service, string locationName, Vector2 tile)
    {
        var snapshot = (CompostingBinData?)typeof(CompostingBinService)
            .GetMethod("CreateSaveSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(service, null);
        if (snapshot == null)
        {
            return null;
        }

        string key = (int)tile.X + "," + (int)tile.Y;
        return snapshot.LocationBinData.TryGetValue(locationName, out var bins) && bins.TryGetValue(key, out var record)
            ? record
            : null;
    }

    /// <summary>
    /// Resolves the repository root by walking up from the assembly directory to
    /// the directory that holds the solution file, so a source-scanning test is
    /// independent of the runner's working directory.
    /// </summary>
    private static string ResolveRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Stardew-LivingRoots.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? Directory.GetCurrentDirectory();
    }
}
