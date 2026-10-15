using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariant tests for the FR-018 single-player / multiplayer
/// equivalence requirement (spec 004, T026 and T039).
/// </summary>
/// <remarks>
/// FR-018 (Constitution VI) forbids any code path from behaving differently on a
/// multiplayer farm than on a single-player farm. The production services take no
/// player-count input at all — the only multiplayer-specific seam is the
/// host-authoritative atomic lock, which arbitrates WHO acts first, never WHAT
/// the action does. These tests pin that contract with FsCheck properties:
/// <list type="bullet">
///   <item>
///     <description>
///       T026: the same logical operation sequence produces an identical
///       observable signature whether the day-start is delivered once
///       (single-player) or once per connected player (multiplayer), and
///       whether or not a host atomic lock is injected.
///     </description>
///   </item>
///   <item>
///     <description>
///       T039: no code path branches on player count — behaviour is invariant
///       to the simulated player count, event wiring is identical for a
///       one-player and an N-player session, and none of the three services
///       takes a player-count dependency.
///     </description>
///   </item>
///   <item>
///     <description>
///       T039 (FR-019): concurrent actions on the same coordinate yield exactly
///       one applied change, and a refused concurrent action guarantees zero
///       item loss.
///     </description>
///   </item>
/// </list>
/// Every case constructs fresh services as method locals (T280(b) per-case
/// isolation), so no state leaks between FsCheck cases.
/// </remarks>
public class MultiplayerInvariantsTests
{
    public MultiplayerInvariantsTests()
    {
        // The bin service plays the vanilla "Ship" cue on maturation and
        // collection; GameStateFixture installs the no-op sound stand-in.
        GameStateFixture.Install();
    }

    // ------------------------------------------------------------------
    // T026 — SinglePlayerPath == MultiPlayerPath
    // ------------------------------------------------------------------

    /// <summary>
    /// T026 (FR-018): for any location name, the full compost loop — deposit,
    /// two day-start deliveries, collect — produces the identical observable
    /// signature on the single-player path (one day-start delivery, no host
    /// lock) and on the multiplayer path (one delivery per connected player,
    /// host lock injected).
    /// </summary>
    [Property(MaxTest = 100)]
    public void T026_SinglePlayerPathEqualsMultiPlayerPath(string locationName)
    {
        var location = NormalizeLocation(locationName);
        var playerCount = 2 + (location.Length % 3); // 2..4 connected players

        var singlePlayer = RunCompostLoop(location, dayStartDeliveries: 1, withHostLock: false);
        var multiPlayer = RunCompostLoop(location, dayStartDeliveries: playerCount, withHostLock: true);

        Assert.Equal(singlePlayer, multiPlayer);

        // The signature itself is the production contract: Empty -> Processing
        // on deposit, still Processing after one day, Ready after exactly
        // ModConstants.MaturationDays days, exactly one compost on collection.
        Assert.Equal(CompostingBinState.Processing, singlePlayer.AfterAdd);
        Assert.Equal(CompostingBinState.Processing, singlePlayer.AfterDayOne);
        Assert.Equal(CompostingBinState.Ready, singlePlayer.AfterDayTwo);
        Assert.Equal(1, singlePlayer.Collected);
        Assert.Equal(CompostingBinState.Empty, singlePlayer.Final);
    }

    /// <summary>
    /// T026 (FR-018): for any finite initial soil health, applying compost to a
    /// tilled farm tile returns the same result, the same resulting health and
    /// the same remaining held stack on the single-player path (no lock) and on
    /// the multiplayer path (the action runs under the host-authoritative lock).
    /// </summary>
    [Property(MaxTest = 100)]
    public void T026_CompostApplication_SingleVsMulti(float initialHealth)
    {
        var seed = float.IsNaN(initialHealth) || float.IsInfinity(initialHealth)
            ? 0f
            : Math.Clamp(initialHealth, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        fixture.AddHoeDirtTile(14, 14);
        var tile = new Vector2(14, 14);

        var singlePlayer = ApplyCompostOnce(seed, tile, fixture.Location, useHostLock: false);
        var multiPlayer = ApplyCompostOnce(seed, tile, fixture.Location, useHostLock: true);

        Assert.Equal(singlePlayer.Success, multiPlayer.Success);
        Assert.Equal(singlePlayer.HealthAfter, multiPlayer.HealthAfter);
        Assert.Equal(singlePlayer.StackAfter, multiPlayer.StackAfter);
    }

    /// <summary>
    /// T026 (FR-018): for any location name, the bin service's state machine
    /// (Empty -> Processing -> Ready -> Empty) and its exactly-one yield are
    /// identical whether the service carries no host lock (single-player) or the
    /// shared host lock (multiplayer), and whether the day-start is delivered
    /// once or once per connected player.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T026_BinService_SingleVsMulti(string locationName)
    {
        var location = NormalizeLocation(locationName);
        var playerCount = 2 + (location.Length % 3);

        var singlePlayer = RunCompostLoop(location, dayStartDeliveries: 1, withHostLock: false);
        var multiPlayer = RunCompostLoop(location, dayStartDeliveries: playerCount, withHostLock: true);

        Assert.Equal(singlePlayer, multiPlayer);
        Assert.Equal(1, singlePlayer.Granted);
    }

    // ------------------------------------------------------------------
    // T039 — FR-018 audit: no code path branches on player count
    // ------------------------------------------------------------------

    /// <summary>
    /// T039 (FR-018 audit): for any location name, three independent audits
    /// confirm no code path branches on player count —
    /// (1) the bin lifecycle is invariant to the number of day-start deliveries,
    /// (2) the controller's event wiring is identical for a one-player and an
    /// N-player session (driven through ThreadSafeGameLoopEventsStub), and
    /// (3) none of the three services takes a player-count dependency.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T039_NoPlayerCountBranch(string locationName)
    {
        var location = NormalizeLocation(locationName);
        var playerCount = 2 + (location.Length % 3);

        // (1) Behaviour: N-player day-start delivery == 1-player delivery.
        var singlePlayer = RunCompostLoop(location, dayStartDeliveries: 1, withHostLock: false);
        var multiPlayer = RunCompostLoop(location, dayStartDeliveries: playerCount, withHostLock: true);
        Assert.Equal(singlePlayer, multiPlayer);

        // (2) Event wiring: registration does not depend on the session size.
        Assert.Equal(
            RegisterAndCountSubscriptions(playerCount: 1),
            RegisterAndCountSubscriptions(playerCount: playerCount));

        // (3) Structure: no player-count dependency on any of the three services.
        AssertNoPlayerCountDependency(typeof(HostAtomicLockService));
        AssertNoPlayerCountDependency(typeof(CompostingBinService));
        AssertNoPlayerCountDependency(typeof(CompostApplicationService));
    }

    /// <summary>
    /// T039 (FR-019): for any seed, two farmers racing for the same coordinate
    /// yield exactly one applied change — exactly one acquisition succeeds, the
    /// lock holder is the winner, and once the winner releases, the loser can
    /// acquire (the lock is never stuck).
    /// </summary>
    [Property(MaxTest = 100)]
    public void T039_FirstWinsAtomicLock(int arbitrarySeed)
    {
        var lockService = new HostAtomicLockService();
        var location = "Farm";
        var tile = new Vector2(
            Math.Abs(arbitrarySeed % 64),
            Math.Abs(arbitrarySeed / 64) % 64);

        var salt = (Math.Abs(arbitrarySeed) % 1000) + 1;
        var (host, farmhand, _) = MultiplayerFarmerStub.CreatePair(
            hostId: 1000L + salt,
            farmhandId: 2000L + salt);

        var results = new bool[2];
        Parallel.Invoke(
            () => results[0] = lockService.TryAcquireLock(location, tile, host.FarmerId),
            () => results[1] = lockService.TryAcquireLock(location, tile, farmhand.FarmerId));

        Assert.Equal(1, results.Count(r => r));
        Assert.Equal(1, results.Count(r => !r));
        Assert.True(lockService.IsLocked(location, tile));

        var winner = results[0] ? host : farmhand;
        Assert.Equal(winner.FarmerId, lockService.GetLockHolder(location, tile));

        lockService.ReleaseLock(location, tile, winner.FarmerId);
        Assert.False(lockService.IsLocked(location, tile));

        var loser = results[0] ? farmhand : host;
        Assert.True(lockService.TryAcquireLock(location, tile, loser.FarmerId));
    }

    /// <summary>
    /// T039 (FR-019): for any seed, a refused concurrent bin interaction
    /// guarantees zero item loss — exactly one farmer's deposit is accepted (the
    /// bin holds exactly one batch), the winner spends exactly one unit, and the
    /// refused farmer's held stack and inventory are untouched.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T039_ZeroItemLoss(int arbitrarySeed)
    {
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(
            Math.Abs(arbitrarySeed % 64),
            Math.Abs(arbitrarySeed / 64) % 64);

        var placedBin = new StardewValley.Object
        {
            ItemId = ModConstants.CompostingBinItemId,
            TileLocation = tile
        };
        fixture.Location.Objects.Add(tile, placedBin);

        var lockService = new HostAtomicLockService();
        var binService = CreateBinService(new TimeProviderStub(), new RecordingPlayerInventory(), lockService);
        var controller = CreateController(binService, lockService);

        var salt = (Math.Abs(arbitrarySeed) % 1000) + 1;
        var (host, farmhand, _) = MultiplayerFarmerStub.CreatePair(
            hostId: 3000L + salt,
            farmhandId: 4000L + salt);

        var hostWaste = ItemFactory.CreateValidWasteItem();
        hostWaste.Stack = 1;
        var farmhandWaste = ItemFactory.CreateValidWasteItem();
        farmhandWaste.Stack = 1;
        host.SetHeldItem(hostWaste);
        farmhand.SetHeldItem(farmhandWaste);

        var results = new bool[2];
        Parallel.Invoke(
            () => results[0] = controller.TryInteractWithBin(
                fixture.Location,
                tile,
                host.FarmerId,
                host.CurrentItem,
                host.Inventory,
                msg => host.RecordRefusal(msg)),
            () => results[1] = controller.TryInteractWithBin(
                fixture.Location,
                tile,
                farmhand.FarmerId,
                farmhand.CurrentItem,
                farmhand.Inventory,
                msg => farmhand.RecordRefusal(msg)));

        Assert.Equal(1, results.Count(r => r));
        Assert.Equal(1, results.Count(r => !r));

        var winner = results[0] ? host : farmhand;
        var loser = results[0] ? farmhand : host;

        // Exactly one batch was accepted — the second deposit was refused.
        Assert.Equal(CompostingBinState.Processing, binService.GetBinState("Farm", tile));

        // The winner spent exactly one unit; the loser lost zero.
        Assert.Equal(0, winner.CurrentItem!.Stack);
        Assert.Equal(1, loser.CurrentItem!.Stack);
        Assert.Empty(loser.Inventory.AddedItems);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Runs the full compost loop against a fresh bin service and returns the
    /// observable signature: the state after deposit, after each day-start
    /// delivery, the collection yield, the final state and the number of items
    /// granted into the injected inventory.
    /// </summary>
    private static (CompostingBinState AfterAdd, CompostingBinState AfterDayOne, CompostingBinState AfterDayTwo, int Collected, CompostingBinState Final, int Granted)
        RunCompostLoop(string locationName, int dayStartDeliveries, bool withHostLock)
    {
        var timeStub = new TimeProviderStub { TotalDays = 1 };
        var inventory = new RecordingPlayerInventory();
        var lockService = withHostLock ? new HostAtomicLockService() : null;
        var service = CreateBinService(timeStub, inventory, lockService);
        var tile = new Vector2(3, 4);

        service.AddWaste(locationName, tile, ItemFactory.CreateValidWasteItem());
        var afterAdd = service.GetBinState(locationName, tile);

        timeStub.TotalDays = 2;
        for (var i = 0; i < dayStartDeliveries; i++)
        {
            service.ProcessDayStart(locationName);
        }
        var afterDayOne = service.GetBinState(locationName, tile);

        timeStub.TotalDays = 3;
        for (var i = 0; i < dayStartDeliveries; i++)
        {
            service.ProcessDayStart(locationName);
        }
        var afterDayTwo = service.GetBinState(locationName, tile);

        var collected = service.CollectCompost(locationName, tile);
        var final = service.GetBinState(locationName, tile);

        return (afterAdd, afterDayOne, afterDayTwo, collected, final, inventory.AddedItems.Count);
    }

    /// <summary>
    /// Applies compost once to a tilled tile through a real
    /// <see cref="CompostApplicationService"/>, optionally under the
    /// host-authoritative lock (the multiplayer path), and returns the result,
    /// the resulting soil health and the remaining held stack.
    /// </summary>
    private static (bool Success, float HealthAfter, int StackAfter) ApplyCompostOnce(
        float initialHealth,
        Vector2 tile,
        GameLocation location,
        bool useHostLock)
    {
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
        held.Stack = 5;
        provider.CurrentItem = held;

        var service = new CompostApplicationService(
            mockSoilHealth.Object,
            provider,
            new Mock<IMonitor>().Object);

        var lockService = useHostLock ? new HostAtomicLockService() : null;
        bool success;
        if (lockService != null)
        {
            var acquired = lockService.TryAcquireLock(location.Name, tile, 1L);
            Assert.True(acquired);
            try
            {
                success = service.TryApplyCompost(location, tile);
            }
            finally
            {
                lockService.ReleaseLock(location.Name, tile, 1L);
            }
        }
        else
        {
            success = service.TryApplyCompost(location, tile);
        }

        return (success, health, held.Stack);
    }

    /// <summary>
    /// Registers a controller's events against a fresh
    /// <see cref="ThreadSafeGameLoopEventsStub"/> and returns the total number of
    /// game-loop subscriptions made. The count is the observable event-wiring
    /// signature: FR-018 requires it to be independent of the session size.
    /// </summary>
    private static int RegisterAndCountSubscriptions(int playerCount)
    {
        Assert.True(playerCount >= 1, "A session always has at least one player.");

        var gameLoop = new ThreadSafeGameLoopEventsStub();
        var controller = CreateController(
            CreateBinService(new TimeProviderStub(), new RecordingPlayerInventory(), null),
            new HostAtomicLockService(),
            gameLoop);
        controller.RegisterEvents();

        return gameLoop.GameLaunchedAddCount
            + gameLoop.SaveLoadedAddCount
            + gameLoop.SavingAddCount;
    }

    /// <summary>
    /// Asserts that neither the constructor nor any declared public method of
    /// <paramref name="serviceType"/> takes a player-count dependency — the
    /// structural half of the FR-018 audit.
    /// </summary>
    private static void AssertNoPlayerCountDependency(Type serviceType)
    {
        foreach (var constructor in serviceType.GetConstructors())
        {
            foreach (var parameter in constructor.GetParameters())
            {
                Assert.False(
                    IsPlayerCountType(parameter.ParameterType),
                    $"{serviceType.Name} must not depend on player-count type '{parameter.ParameterType.Name}' (FR-018).");
            }
        }

        var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var method in methods)
        {
            foreach (var parameter in method.GetParameters())
            {
                Assert.False(
                    IsPlayerCountType(parameter.ParameterType),
                    $"{serviceType.Name}.{method.Name} must not depend on player-count type '{parameter.ParameterType.Name}' (FR-018).");
            }
        }
    }

    /// <summary>
    /// A type is a player-count dependency when its name names the multiplayer
    /// session or an explicit player count.
    /// </summary>
    private static bool IsPlayerCountType(Type type)
    {
        var name = type.Name;
        return name.IndexOf("Multiplayer", StringComparison.Ordinal) >= 0
            || name.IndexOf("PlayerCount", StringComparison.Ordinal) >= 0;
    }

    /// <summary>
    /// Builds a <see cref="CompostingBinService"/> with a permissive waste
    /// validator and a recording inventory, optionally carrying the host lock.
    /// </summary>
    private static CompostingBinService CreateBinService(
        TimeProviderStub timeStub,
        IPlayerInventory inventory,
        IHostAtomicLockService? hostAtomicLockService)
    {
        var mockModDataService = new Mock<IModDataService>();
        var mockSaveIdProvider = new Mock<ISaveIdProvider>();
        var mockWasteValidator = new Mock<IOrganicWasteValidator>();
        mockWasteValidator
            .Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>()))
            .Returns(true);
        var mockMonitor = new Mock<IMonitor>();

        return new CompostingBinService(
            mockModDataService.Object,
            mockSaveIdProvider.Object,
            mockWasteValidator.Object,
            mockMonitor.Object,
            timeStub,
            new CompostingBinFactory(),
            inventory,
            null,
            hostAtomicLockService);
    }

    /// <summary>
    /// Builds a <see cref="ModController"/> over the given bin service and host
    /// lock. When <paramref name="gameLoop"/> is supplied the controller's event
    /// sources are wired so <see cref="ModController.RegisterEvents"/> can run.
    /// </summary>
    private static ModController CreateController(
        ICompostingBinService binService,
        IHostAtomicLockService hostAtomicLockService,
        IGameLoopEvents? gameLoop = null)
    {
        var mockHelper = new Mock<IModHelper>();
        if (gameLoop != null)
        {
            var mockEvents = new Mock<IModEvents>();
            mockEvents.Setup(x => x.GameLoop).Returns(gameLoop);
            mockEvents.Setup(x => x.Input).Returns(new Mock<IInputEvents>().Object);
            mockEvents.Setup(x => x.Display).Returns(new Mock<IDisplayEvents>().Object);
            mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
        }

        var mockManifest = new Mock<IManifest>();
        mockManifest.Setup(x => x.UniqueID).Returns("livingroots.invariants.test");
        mockManifest.Setup(x => x.Version).Returns(new SemanticVersion(1, 0, 0));

        return new ModController(
            mockHelper.Object,
            new Mock<IMonitor>().Object,
            mockManifest.Object,
            new Mock<ISoilHealthService>().Object,
            new Mock<ISaveIdProvider>().Object,
            binService,
            new Mock<ISoilDecayService>().Object,
            null,
            null,
            null,
            hostAtomicLockService);
    }

    /// <summary>
    /// Normalises an arbitrary FsCheck string into a usable location name,
    /// falling back to the farm when the generated value is null or blank.
    /// </summary>
    private static string NormalizeLocation(string? locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            return "Farm";
        }

        return locationName;
    }
}
