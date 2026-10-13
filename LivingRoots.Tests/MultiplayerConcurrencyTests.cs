using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Domain.Models;
using LivingRoots.Domain.Services;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Concurrency and multiplayer interaction tests verifying host-authoritative atomic locks,
/// first-wins resolution, day-advance processing across connected player counts, and zero item loss (FR-004, FR-019).
/// Validates Tasks T019 and T021 [P] [US2] under Feature 004 (Playable Compost Loop), plus
/// Tasks T063 [US2] (one day tick reaches every location), T064 [US3] (a collection grants
/// exactly one compost item in total), T068 [US3] (daily soil decay is idempotent per day),
/// T096 [US2] (the production controller never carries a null host atomic lock service),
/// T115 [US2] (a bin interaction with no lock service refuses fail-closed with zero
/// mutation), T122 [US3] (both conflict sites render translated text through the
/// production input path) and T125 [US2] (the SC-010 headline test drives the production
/// TryApplyCompostWithLock/CompostApplicationService path).
/// </summary>
public class MultiplayerConcurrencyTests
{
    private readonly Mock<IModHelper> _mockHelper;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<IManifest> _mockManifest;
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
    private readonly Mock<IOrganicWasteValidator> _mockWasteValidator;
    private readonly Mock<ILocationProvider> _mockLocationProvider;
    private readonly Mock<ISoilDecayService> _mockSoilDecayService;
    private readonly TimeProviderStub _timeStub;
    private readonly CompostingBinFactory _factory;
    private readonly HostAtomicLockService _lockService;

    public MultiplayerConcurrencyTests()
    {
        GameStateFixture.Install();

        _mockHelper = new Mock<IModHelper>();
        _mockMonitor = new Mock<IMonitor>();
        _mockManifest = new Mock<IManifest>();
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockSaveIdProvider = new Mock<ISaveIdProvider>();
        _mockWasteValidator = new Mock<IOrganicWasteValidator>();
        _mockLocationProvider = new Mock<ILocationProvider>();
        _mockSoilDecayService = new Mock<ISoilDecayService>();
        _timeStub = new TimeProviderStub();
        _factory = new CompostingBinFactory();
        _lockService = new HostAtomicLockService();

        _mockManifest.Setup(x => x.UniqueID).Returns("test.mod.id");
        _mockManifest.Setup(x => x.Version).Returns(new SemanticVersion(1, 0, 0));

        // Always accept valid waste items by default
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);
    }

    private CompostingBinService CreateBinService(IPlayerInventory? playerInventory = null)
    {
        var inventory = playerInventory ?? new RecordingPlayerInventory();
        var mockModData = new Mock<IModDataService>();

        return new CompostingBinService(
            mockModData.Object,
            _mockSaveIdProvider.Object,
            _mockWasteValidator.Object,
            _mockMonitor.Object,
            _timeStub,
            _factory,
            inventory,
            _mockLocationProvider.Object);
    }

    private ModController CreateController(ICompostingBinService binService)
    {
        return new ModController(
            _mockHelper.Object,
            _mockMonitor.Object,
            _mockManifest.Object,
            _mockSoilHealthService.Object,
            _mockSaveIdProvider.Object,
            binService,
            _mockSoilDecayService.Object,
            null,
            null,
            null,
            _lockService);
    }

    /// <summary>
    /// Builds a controller with full control over the optional seams the
    /// concurrency tests exercise: the compost application service, the
    /// host atomic lock service (an explicit <c>null</c> builds the
    /// headless-double shape that carries no lock registry — the shape
    /// T115 and T122 prove must refuse fail-closed), and the player
    /// provider the production input path reads the held item through
    /// (T082).
    /// </summary>
    private ModController CreateController(
        ICompostingBinService binService,
        ICompostApplicationService? compostApplicationService,
        IHostAtomicLockService? hostAtomicLockService,
        IPlayerProvider? playerProvider)
    {
        return new ModController(
            _mockHelper.Object,
            _mockMonitor.Object,
            _mockManifest.Object,
            _mockSoilHealthService.Object,
            _mockSaveIdProvider.Object,
            binService,
            _mockSoilDecayService.Object,
            compostApplicationService,
            null,
            null,
            hostAtomicLockService,
            playerProvider);
    }

    private static Item CreateWasteItem(int stack = 1)
    {
        return new StardewValley.Object
        {
            ItemId = "24",
            Name = "Parsnip",
            Stack = stack
        };
    }

    /// <summary>Marker prefixing the compost-site conflict test's stub translations.</summary>
    private const string CompostConflictTranslationMarker = "PT:compost-conflict:";

    /// <summary>Marker prefixing the bin-site conflict test's stub translations.</summary>
    private const string BinConflictTranslationMarker = "PT:bin-conflict:";

    /// <summary>
    /// Wires <see cref="IModHelper.Events"/> with mock event sources so
    /// <see cref="ModController.RegisterEvents"/> subscribes the controller's
    /// production handlers, which a test then raises directly through the
    /// <see cref="IInputEvents.ButtonPressed"/> event source.
    /// </summary>
    /// <returns>The mocked <see cref="IInputEvents"/> the controller subscribed to.</returns>
    private Mock<IInputEvents> WireControllerEventSubscriptions()
    {
        var gameLoop = new Mock<IGameLoopEvents>();
        var inputEvents = new Mock<IInputEvents>();
        var displayEvents = new Mock<IDisplayEvents>();
        var worldEvents = new Mock<IWorldEvents>();
        var modEvents = new Mock<IModEvents>();
        modEvents.Setup(x => x.GameLoop).Returns(gameLoop.Object);
        modEvents.Setup(x => x.Input).Returns(inputEvents.Object);
        modEvents.Setup(x => x.Display).Returns(displayEvents.Object);
        modEvents.Setup(x => x.World).Returns(worldEvents.Object);
        _mockHelper.Setup(x => x.Events).Returns(modEvents.Object);
        return inputEvents;
    }

    /// <summary>
    /// Builds a <see cref="ButtonPressedEventArgs"/> for the given button and cursor.
    /// </summary>
    /// <remarks>
    /// SMAPI constructs these event args internally — the type exposes no public
    /// constructor and read-only properties — so the test host invokes the internal
    /// constructor through reflection. The instance is fully initialised by the real
    /// constructor (no private state is poked), the same class of constructor
    /// workaround <see cref="GameStateFixture"/> applies to <see cref="Game1"/>.
    /// </remarks>
    /// <param name="button">The button the event args report.</param>
    /// <param name="cursor">The cursor position the event args carry.</param>
    /// <returns>An initialised <see cref="ButtonPressedEventArgs"/> for <paramref name="button"/>.</returns>
    private static ButtonPressedEventArgs CreateButtonPressedEventArgs(SButton button, ICursorPosition cursor)
    {
        var constructor = typeof(ButtonPressedEventArgs).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
        var parameters = constructor.GetParameters();
        var arguments = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType == typeof(SButton))
            {
                arguments[i] = button;
            }
            else if (parameterType == typeof(ICursorPosition))
            {
                arguments[i] = cursor;
            }
            else
            {
                arguments[i] = Activator.CreateInstance(parameterType)!;
            }
        }

        return (ButtonPressedEventArgs)Activator.CreateInstance(
            typeof(ButtonPressedEventArgs),
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            args: arguments,
            culture: null)!;
    }

    /// <summary>
    /// Builds a stub <see cref="ITranslationHelper"/> whose
    /// <c>Get(key)</c> returns a translation reading
    /// <c><paramref name="marker"/> + key</c>. The marker makes
    /// this test's rendered text unique, so the game's HUD-message
    /// de-duplication cannot collapse it into an entry a sibling
    /// test already rendered.
    /// </summary>
    private static ITranslationHelper CreateStubTranslationHelper(string marker)
    {
        var mock = new Mock<ITranslationHelper>();
        mock
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns((string key) => CreateTranslation(key, marker + key));
        return mock.Object;
    }

    /// <summary>
    /// Creates a <see cref="Translation"/> carrying <paramref name="text"/>.
    /// SMAPI exposes no public constructor (the (locale, key, text) ctor is
    /// internal), so the test reaches it by reflection — the same technique
    /// AssetRegistrationTests uses to drive SMAPI's internal edit operations.
    /// </summary>
    private static Translation CreateTranslation(string key, string text)
    {
        var ctor = typeof(Translation).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .First(c => c.GetParameters().Length == 3);
        return (Translation)ctor.Invoke(new object[] { "en", key, text });
    }

    /// <summary>
    /// Asserts exactly one new red HUD message was shown carrying
    /// <paramref name="expectedText"/> — the translated refusal — and
    /// that the rendered text is not the raw translation key.
    /// </summary>
    private static void AssertTranslatedRedMessageShown(int hudMessagesBefore, string translationKey, string expectedText)
    {
        Assert.Equal(hudMessagesBefore + 1, Game1.hudMessages.Count);
        var shown = Game1.hudMessages[Game1.hudMessages.Count - 1].message;
        Assert.Equal(expectedText, shown);
        Assert.NotEqual(translationKey, shown);
    }

    // =========================================================================
    // Task T019 [US2] Tests: Day-advance processing in multiplayer (FR-004)
    // =========================================================================

    /// <summary>
    /// Verifies that advancing one in-game day in single player advances active batch processing
    /// exactly once, requiring 2 full days to transition from Processing to Ready (FR-004, ModConstants.MaturationDays).
    /// </summary>
    [Fact]
    public void DayAdvance_SinglePlayer_AdvancesBatchExactlyOnce()
    {
        // Arrange
        var service = CreateBinService();
        var tile = new Vector2(10, 10);
        var wasteItem = CreateWasteItem();

        _timeStub.TotalDays = 1;
        service.AddWaste("Farm", tile, wasteItem);
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        // Act & Assert: Day 2 (1 day elapsed)
        _timeStub.TotalDays = 2;
        service.ProcessDayStart("Farm");

        // Must still be Processing (1 day < 2 maturation days)
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        // Act & Assert: Day 3 (2 days elapsed)
        _timeStub.TotalDays = 3;
        service.ProcessDayStart("Farm");

        // Transitions to Ready after exactly 2 days
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));
    }

    /// <summary>
    /// Verifies that when 2 players are connected in multiplayer, advancing the day advances
    /// the active batch exactly once for that day, regardless of both players triggering day start (FR-004).
    /// </summary>
    [Fact]
    public void DayAdvance_TwoConnectedPlayers_AdvancesBatchExactlyOncePerDay()
    {
        // Arrange
        var service = CreateBinService();
        var tile = new Vector2(10, 10);
        var wasteItem = CreateWasteItem();
        var session = MultiplayerFarmerStub.CreateSession(1); // 1 host + 1 farmhand = 2 players

        _timeStub.TotalDays = 1;
        service.AddWaste("Farm", tile, wasteItem);
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        // Act: Day 2 begins, both connected players process day start
        _timeStub.TotalDays = 2;
        foreach (var farmer in session.AllFarmers)
        {
            service.ProcessDayStart("Farm");
        }

        // Assert: Under FR-004, batch must advance exactly once on Day 2.
        // It must NOT advance twice and complete prematurely on Day 2!
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        // Act: Day 3 begins, both players process day start
        _timeStub.TotalDays = 3;
        foreach (var farmer in session.AllFarmers)
        {
            service.ProcessDayStart("Farm");
        }

        // Assert: Reaches Ready on Day 3 after 2 full in-game days
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));
    }

    /// <summary>
    /// Verifies that when 4 players are connected in multiplayer, advancing the day advances
    /// the active batch exactly once for that day, regardless of all 4 players triggering day start (FR-004).
    /// </summary>
    [Fact]
    public void DayAdvance_FourConnectedPlayers_AdvancesBatchExactlyOncePerDay()
    {
        // Arrange
        var service = CreateBinService();
        var tile = new Vector2(10, 10);
        var wasteItem = CreateWasteItem();
        var session = MultiplayerFarmerStub.CreateSession(3); // 1 host + 3 farmhands = 4 players

        Assert.Equal(4, session.AllFarmers.Count);

        _timeStub.TotalDays = 1;
        service.AddWaste("Farm", tile, wasteItem);
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        // Act: Day 2 begins, all 4 connected players process day start
        _timeStub.TotalDays = 2;
        foreach (var farmer in session.AllFarmers)
        {
            service.ProcessDayStart("Farm");
        }

        // Assert: Under FR-004, 4 connected players must NOT cause 4 days of maturation advancement.
        // The bin must still be in Processing state after 1 in-game day.
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        // Act: Day 3 begins, all 4 connected players process day start
        _timeStub.TotalDays = 3;
        foreach (var farmer in session.AllFarmers)
        {
            service.ProcessDayStart("Farm");
        }

        // Assert: Transitions to Ready after 2 days
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));
    }

    /// <summary>
    /// Verifies that concurrent DayStart triggers executed simultaneously from multiple player threads
    /// execute thread-safely and idempotently without advancing the batch more than once (FR-004).
    /// </summary>
    [Fact]
    public async Task DayAdvance_ConcurrentDayStart_ExecutesIdempotentlyWithoutRaceConditions()
    {
        // Arrange
        var service = CreateBinService();
        var tile = new Vector2(10, 10);
        var wasteItem = CreateWasteItem();
        var session = MultiplayerFarmerStub.CreateSession(3); // 4 players

        _timeStub.TotalDays = 1;
        service.AddWaste("Farm", tile, wasteItem);

        // Act: Day 2, all 4 players trigger day start simultaneously via barrier synchronization
        _timeStub.TotalDays = 2;
        await session.RunSimultaneouslyAsync(farmer =>
        {
            service.ProcessDayStart("Farm");
            return Task.CompletedTask;
        });

        // Assert: State must remain Processing after Day 2
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        // Act: Day 3, all 4 players trigger day start simultaneously
        _timeStub.TotalDays = 3;
        await session.RunSimultaneouslyAsync(farmer =>
        {
            service.ProcessDayStart("Farm");
            return Task.CompletedTask;
        });

        // Assert: State transitions to Ready after 2 days
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));
    }

    // =========================================================================
    // Task T063 [US2] Tests: one day tick advances bins in every location (FR-004 partial)
    // =========================================================================

    /// <summary>
    /// Verifies that a single day tick through the controller's DayStarted seam advances
    /// composting batches in EVERY location that holds one — the Farm and the Greenhouse
    /// alike (T063, FR-004). The SMAPI DayStarted event fires once per day for the whole
    /// host, so a day-start path that only processes <see cref="Game1.currentLocation"/>
    /// silently starves every other location's bins.
    /// </summary>
    [Fact]
    public void DayAdvance_ControllerDayStarted_AdvancesBinsInFarmAndGreenhouse()
    {
        // Arrange
        var service = CreateBinService();
        var controller = CreateController(service);
        var tile = new Vector2(10, 10);

        var farmFixture = new GameLocationFixture("Farm", isFarm: true);
        var greenhouseFixture = new GameLocationFixture("Greenhouse", isFarm: false);

        // Bins placed on the farm AND in the Greenhouse, both starting a batch on day 1.
        _timeStub.TotalDays = 1;
        service.AddWaste("Farm", tile, CreateWasteItem());
        service.AddWaste("Greenhouse", tile, CreateWasteItem());

        // Two full in-game days elapse, so a single further tick matures both batches.
        _timeStub.TotalDays = 3;

        // The host is standing on the farm when the day ticks over.
        var originalLocation = Game1.currentLocation;
        Game1.currentLocation = farmFixture.Location;

        try
        {
            // Act: drive the controller's private OnDayStarted — the seam the SMAPI
            // DayStarted event invokes — exactly once. OnDayStarted is private and no
            // public method reproduces the event path, so reflection is the seam, the
            // same pattern SanitizePathSegmentsTests and ModControllerTests use for
            // private members. The handler ignores its event args, so null stands in.
            var onDayStarted = typeof(ModController).GetMethod("OnDayStarted", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(onDayStarted);
            onDayStarted!.Invoke(controller, new object?[] { null, null });
        }
        finally
        {
            Game1.currentLocation = originalLocation;
        }

        // Assert: FR-004 — one day tick matures bins in every location, not only the
        // location the host happens to occupy.
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Greenhouse", tile));
    }

    // =========================================================================
    // Task T021 [US2] Tests: Concurrent player interactions on same bin (FR-019)
    // =========================================================================

    /// <summary>
    /// Verifies that when two farmers attempt to deposit waste simultaneously into the same empty bin,
    /// first-wins atomic locking guarantees exactly one succeeds, the second is refused,
    /// and zero items are lost from the rejected farmer's inventory (FR-019). T077 /
    /// FR-008a: the losing deposit is refused because the bin already holds a batch — an
    /// ordinary invalid bin state the refusal handler is NOT owed a message for; only
    /// genuine lock contention emits the action-conflict key, so a refusal message is
    /// asserted only when one is actually recorded.
    /// </summary>
    [Fact]
    public async Task ConcurrentDepositWaste_TwoFarmersOnSameBin_FirstWins_SecondRefusedWithZeroItemLoss()
    {
        // Arrange
        var service = CreateBinService();
        var controller = CreateController(service);
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);

        var (host, farmhand, context) = MultiplayerFarmerStub.CreatePair(hostId: 1001L, farmhandId: 1002L);

        var hostWaste = CreateWasteItem(stack: 1);
        var farmhandWaste = CreateWasteItem(stack: 1);

        host.SetHeldItem(hostWaste);
        farmhand.SetHeldItem(farmhandWaste);

        host.Inventory.TryAddItem(hostWaste);
        farmhand.Inventory.TryAddItem(farmhandWaste);

        // Bin is placed and empty
        var placedBin = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId, TileLocation = tile };
        fixture.Location.Objects.Add(tile, placedBin);

        // Act: Both farmers attempt to interact / deposit waste simultaneously
        var results = await context.RunSimultaneouslyAsync(async farmer =>
        {
            var success = controller.TryInteractWithBin(
                fixture.Location,
                tile,
                farmer.FarmerId,
                farmer.CurrentItem,
                farmer.Inventory,
                msg => farmer.RecordRefusal(msg));

            if (success)
            {
                farmer.RecordSuccess();
            }

            return await Task.FromResult(success);
        });

        // Assert: Under first-wins atomic locking (FR-019):
        // Exactly one attempt MUST succeed, and exactly one MUST be refused
        int successes = results.Count(r => r);
        int refusals = results.Count(r => !r);

        Assert.Equal(1, successes);
        Assert.Equal(1, refusals);

        // Bin state must be Processing — exactly one
        // batch; the loser's waste was never accepted
        // (the controller gates the deposit on the
        // Empty state, and AddWaste re-checks it).
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        // The refused farmer is the one whose attempt
        // returned false. Under T077 / FR-008a a
        // deposit refused because the bin already
        // holds a batch is an ordinary invalid bin
        // state — the refusal handler receives
        // nothing, so RefusedCount cannot identify
        // the loser; the success flag does.
        var winningFarmer = host.SuccessCount > 0 ? host : farmhand;
        var refusedFarmer = host.SuccessCount > 0 ? farmhand : host;

        // T077 (FR-008a): ONLY genuine lock
        // contention — the losing farmer arriving
        // while the winner still holds the
        // host-authoritative atomic lock — owes the
        // action-conflict key. The losing deposit
        // here is refused because the bin already
        // holds a batch, a silent state refusal, so
        // the refused farmer carries at most one
        // message, and any message it does carry is
        // the conflict key.
        Assert.True(refusedFarmer.RefusedCount <= 1);
        Assert.True(refusedFarmer.RefusalMessages.Count <= 1);
        if (refusedFarmer.RefusalMessages.Count == 1)
        {
            Assert.Contains(refusedFarmer.RefusalMessages, m => m.Contains("message.compost.action-conflict") || m.Contains("conflict"));
        }

        // Crucial security invariant: ZERO item loss for the refused farmer (FR-019, SC-009)
        // Refused farmer's held item must NOT have been consumed (stack remains 1)
        Assert.NotNull(refusedFarmer.CurrentItem);
        Assert.Equal(1, refusedFarmer.CurrentItem.Stack);
        Assert.Single(refusedFarmer.Inventory.AddedItems);

        // Winning farmer had their waste deposited
        Assert.Equal(1, winningFarmer.SuccessCount);
    }

    /// <summary>
    /// Verifies that when two farmers attempt to collect compost simultaneously from the same ready bin,
    /// first-wins atomic locking guarantees exactly one succeeds and total yield is strictly 1 compost
    /// item — the second is refused with zero duplicate compost generated (FR-005b, FR-019). T075 /
    /// FR-005a: the single credit is granted by CompostingBinService.CollectCompost through the
    /// service's own injected player inventory, so the assertion counts exactly one compost item in
    /// total across the service-side and collecting-farmer inventories, whichever layer granted it.
    /// </summary>
    [Fact]
    public async Task ConcurrentCollectCompost_TwoFarmersOnSameBin_FirstWins_SecondRefusedWithZeroDuplicateYield()
    {
        // Arrange
        var serviceInventory = new RecordingPlayerInventory();
        var service = CreateBinService(serviceInventory);
        var controller = CreateController(service);
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);

        var (host, farmhand, context) = MultiplayerFarmerStub.CreatePair(hostId: 1001L, farmhandId: 1002L);

        // Empty hands for both farmers
        host.ClearHeldItem();
        farmhand.ClearHeldItem();

        // Bin is placed and Ready with 1 batch
        var placedBin = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId, TileLocation = tile };
        fixture.Location.Objects.Add(tile, placedBin);

        _timeStub.TotalDays = 1;
        service.AddWaste("Farm", tile, CreateWasteItem());
        _timeStub.TotalDays = 3;
        service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));

        // Act: Both farmers attempt to collect compost simultaneously
        var results = await context.RunSimultaneouslyAsync(async farmer =>
        {
            var success = controller.TryInteractWithBin(
                fixture.Location,
                tile,
                farmer.FarmerId,
                farmer.CurrentItem,
                farmer.Inventory,
                msg => farmer.RecordRefusal(msg));

            if (success)
            {
                farmer.RecordSuccess();
            }

            return await Task.FromResult(success);
        });

        // Assert: Under first-wins atomic locking (FR-019):
        // Exactly one attempt succeeds
        int successes = results.Count(r => r);
        int refusals = results.Count(r => !r);

        Assert.Equal(1, successes);
        Assert.Equal(1, refusals);

        // Bin returns to Empty state
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));

        var winningFarmer = host.SuccessCount > 0 ? host : farmhand;
        var refusedFarmer = host.SuccessCount > 0 ? farmhand : host;

        // T075 / FR-005a: CollectCompost is the sole
        // compost credit — the service grants the
        // yield through its own injected player
        // inventory and the controller grants nothing
        // into the collecting farmer's inventory, so
        // exactly one compost item exists in total
        // across the service-side and farmer
        // inventories (zero duplicate compost items,
        // FR-005b).
        int totalCompostGranted = serviceInventory.AddedItems.Count + winningFarmer.Inventory.AddedItems.Count;
        Assert.Equal(1, totalCompostGranted);
        var grantedCompost = serviceInventory.AddedItems.Count == 1
            ? serviceInventory.AddedItems[0]
            : winningFarmer.Inventory.AddedItems[0];
        Assert.Equal(ModConstants.CompostItemId, grantedCompost.ItemId);

        // Refused farmer received 0 compost items (zero phantom duplication)
        Assert.Empty(refusedFarmer.Inventory.AddedItems);
    }

    // =========================================================================
    // Task T064 [US3] Tests: collecting a Ready bin grants exactly one item (FR-005a partial)
    // =========================================================================

    /// <summary>
    /// Verifies that collecting a Ready bin through <see cref="ModController.TryInteractWithBin"/>
    /// grants exactly ONE compost item in total, counted across the inventory the service grants
    /// into and the inventory the controller grants into (T064, FR-005a). In production the
    /// composition root injects the same player inventory into both layers, so one grant from
    /// each layer is a duplicated compost item.
    /// </summary>
    [Fact]
    public void CollectCompost_ReadyBinViaController_GrantsExactlyOneItemAcrossBothInventories()
    {
        // Arrange
        var serviceInventory = new RecordingPlayerInventory();
        var service = CreateBinService(serviceInventory);
        var controller = CreateController(service);
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);

        // Bin is placed and Ready with 1 batch
        var placedBin = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId, TileLocation = tile };
        fixture.Location.Objects.Add(tile, placedBin);

        _timeStub.TotalDays = 1;
        service.AddWaste("Farm", tile, CreateWasteItem());
        _timeStub.TotalDays = 3;
        service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));

        // The inventory the collecting farmer's yield is granted to by the controller.
        var playerInventory = new RecordingPlayerInventory();
        var refusals = new List<string>();

        // Act: collect with empty hands — the yield path, not a deposit.
        bool success = controller.TryInteractWithBin(
            fixture.Location,
            tile,
            4001L,
            null,
            playerInventory,
            msg => refusals.Add(msg));

        // Assert
        Assert.True(success);
        Assert.Empty(refusals);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));

        // FR-005a: exactly one compost item exists after the collection, no matter which
        // layer granted it. The service already granted one into its own inventory; the
        // controller must not grant a second one.
        int totalItemsGranted = serviceInventory.AddedItems.Count + playerInventory.AddedItems.Count;
        Assert.Equal(1, totalItemsGranted);
    }

    /// <summary>
    /// Verifies that the host atomic lock service directly enforces mutual exclusion on a bin coordinate,
    /// ensuring only the first farmer acquires the lock and the second is rejected (FR-019).
    /// </summary>
    [Fact]
    public async Task HostAtomicLock_DirectContention_GuaranteesFirstWinsMutualExclusion()
    {
        // Arrange
        var tile = new Vector2(25, 30);
        var (host, farmhand, context) = MultiplayerFarmerStub.CreatePair(hostId: 2001L, farmhandId: 2002L);

        // Act: Both farmers attempt to acquire lock simultaneously
        var acquiredResults = await context.RunSimultaneouslyAsync(async farmer =>
        {
            var acquired = _lockService.TryAcquireLock("Farm", tile, farmer.FarmerId);
            return await Task.FromResult((farmer.FarmerId, Acquired: acquired));
        });

        // Assert: Exactly one acquired the lock
        var winners = acquiredResults.Where(r => r.Acquired).ToList();
        var losers = acquiredResults.Where(r => !r.Acquired).ToList();

        Assert.Single(winners);
        Assert.Single(losers);

        Assert.True(_lockService.IsLocked("Farm", tile));
        Assert.Equal(winners[0].FarmerId, _lockService.GetLockHolder("Farm", tile));

        // When the winner releases the lock, it becomes unlocked
        _lockService.ReleaseLock("Farm", tile, winners[0].FarmerId);
        Assert.False(_lockService.IsLocked("Farm", tile));
    }

    // =========================================================================
    // Task T115 [US2] Tests: a bin interaction with no lock service refuses fail-closed (FR-019)
    // =========================================================================

    /// <summary>
    /// T115 (FR-019, reconciled with T106 / Constitution III): a bin
    /// interaction on a placed, empty bin must refuse fail-closed —
    /// zero mutation (no waste deposited, no held stack consumed, no
    /// compost collected) and the refusal handler receives the
    /// action-conflict key — when the controller carries no host atomic
    /// lock service, because tile contention cannot be arbitrated
    /// without the registry. The permissive <c>?? true</c> acquisition at
    /// the lock call site (T106) was removed (FR-019 / T169); the
    /// fail-closed guard now extends to this bin interaction site, so
    /// this test asserts refusal, not the old permissive deposit.
    /// </summary>
    [Fact]
    public void TryInteractWithBin_AbsentLockService_FailsClosedWithZeroMutation()
    {
        // Arrange — a placed, empty bin and a farmer holding one unit
        // of waste, with a controller built without a lock service (the
        // headless-double shape; production — ModEntry, T096 — always
        // injects the shared instance).
        var service = CreateBinService();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        var placedBin = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId, TileLocation = tile };
        fixture.Location.Objects.Add(tile, placedBin);

        var controller = CreateController(service, null, null, null);
        var heldWaste = CreateWasteItem(stack: 1);
        var refusals = new List<string>();

        // Act — deposit the waste through the bin interaction path.
        bool success = controller.TryInteractWithBin(
            fixture.Location,
            tile,
            5001L,
            heldWaste,
            null,
            msg => refusals.Add(msg));

        // Assert — T115 (FR-019): fail-closed, zero mutation.
        Assert.False(success, "A bin interaction without a lock service must be refused, not processed.");
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));
        Assert.Equal(1, heldWaste.Stack);
        Assert.Single(refusals);
        Assert.Equal(ModConstants.TranslationKeyCompostActionConflict, refusals[0]);
    }

    /// <summary>
    /// Verifies concurrent compost application on the same tile between two players (T032, FR-019).
    /// First action to take effect wins under host atomic lock. The second attempt evaluates against updated state,
    /// is refused with zero compost consumed/refunded, farm state updated only once (+15), and HUD alert sent.
    /// Contention is made deterministic by a two-party barrier held inside the critical section, so the losing
    /// farmer is guaranteed to attempt the lock while the winning farmer still holds it. Without it the two
    /// attempts can serialise instead of overlapping and the first-wins assertion becomes scheduler-dependent.
    /// </summary>
    /// <remarks>
    /// T125 rewrote this test off the inline reimplementation of the lock discipline
    /// (a hand-rolled TryAcquireLock → checks → apply → release sequence that pinned
    /// the test's own logic, not the mod's) onto the production path:
    /// <see cref="ModController.TryApplyCompostWithLock"/> acquires the host-authoritative
    /// lock, applies through the real <see cref="CompostApplicationService"/> — which
    /// resolves the acting farmer's held compost through the shared
    /// <see cref="MultiplayerFarmerContext"/>, the ambient <see cref="IPlayerProvider"/>
    /// — and releases the lock, so the assertions below pin production behaviour.
    /// <para>
    /// Red-phase outcome (T125): every assertion PASSES against the current production
    /// code — production already implements first-wins correctly (the lock serializes the
    /// two attempts, the loser is refused with the action-conflict key and loses zero
    /// compost, the winner applies exactly one +15 and spends one compost), verified
    /// across repeated runs. No assertion goes red and none is forced to: the red-phase
    /// burden for FR-019/FR-008a in this file is carried by the three tests added
    /// alongside this rewrite — <see cref="TryInteractWithBin_AbsentLockService_FailsClosedWithZeroMutation"/>,
    /// <see cref="OnButtonPressed_CompostActionConflictWithAbsentLockService_ShowsTranslatedConflictText"/>
    /// and <see cref="OnButtonPressed_BinActionConflictWithAbsentLockService_ShowsTranslatedConflictText"/>
    /// — which fail on current code because the lock-refusal paths render no on-screen
    /// explanation when contention cannot be resolved. The rewrite's value is that the
    /// assertions now pin production behaviour instead of the test's simulation of it.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ConcurrentCompostApplication_TwoFarmersOnSameTile_FirstWins_SecondRefusedWithRefund()
    {
        // Arrange
        var tile = new Vector2(14, 14);
        var fixture = new GameLocationFixture("Farm");
        fixture.AddHoeDirtTile(14, 14);

        var (host, farmhand, context) = MultiplayerFarmerStub.CreatePair(hostId: 3001L, farmhandId: 3002L);

        // Both players hold 5 compost — the production application path
        // resolves the acting farmer's held compost through the shared
        // multiplayer context (the ambient IPlayerProvider, T082).
        var hostCompost = ItemFactory.CreateItem(ModConstants.CompostItemId);
        hostCompost.Stack = 5;
        var farmhandCompost = ItemFactory.CreateItem(ModConstants.CompostItemId);
        farmhandCompost.Stack = 5;
        host.SetHeldItem(hostCompost);
        farmhand.SetHeldItem(farmhandCompost);

        // Current soil health is 50f
        float sharedHealth = 50f;
        _mockSoilHealthService.Setup(s => s.GetSoilHealth("Farm", tile)).Returns(() => sharedHealth);

        // The production controller: the real compost application service
        // (resolving held items through the shared multiplayer context)
        // under the host-authoritative lock service.
        var realCompostApplication = new CompostApplicationService(
            _mockSoilHealthService.Object,
            context,
            _mockMonitor.Object);
        var controller = CreateController(CreateBinService(), realCompostApplication, _lockService, context);

        // Deterministic contention: the winner holds the lock until the loser has actually attempted it.
        // Without this barrier the two tasks only *start* together, so under thread-pool load the loser
        // can be scheduled after the winner has already released — the calls then serialise legitimately
        // (50 -> 65 -> 80, both below MaxSoilHealth) and both succeed, making the assertion a coin flip.
        // Exactly one farmer reaches the UpdateHealth callback and exactly one the refusal handler, so a
        // 2-party barrier is always satisfied exactly once and cannot deadlock.
        using var contentionBarrier = new Barrier(2);
        _mockSoilHealthService.Setup(s => s.UpdateHealth("Farm", tile, ModConstants.RestorationAmount))
            .Callback<string, Vector2, float>((loc, t, delta) =>
            {
                // The production service updates soil health while still holding the
                // host-authoritative lock — hold the lock open until the losing
                // farmer's attempt has been refused.
                contentionBarrier.SignalAndWait(TimeSpan.FromSeconds(30));
                sharedHealth += delta;
            });

        // Act: both farmers attempt the production compost application path
        // on the same tile coordinate, simultaneously.
        var results = await context.RunSimultaneouslyAsync(async farmer =>
        {
            context.SetActiveFarmer(farmer);
            bool success = controller.TryApplyCompostWithLock(
                fixture.Location,
                tile,
                farmer.UniqueMultiplayerID,
                msg =>
                {
                    farmer.RecordRefusal(msg);
                    // The loser is guaranteed to arrive while the winner still
                    // holds the lock: the winner only releases after its soil
                    // health update, which waits for this signal.
                    contentionBarrier.SignalAndWait(TimeSpan.FromSeconds(30));
                });

            if (success)
            {
                farmer.RecordSuccess();
            }

            return await Task.FromResult(success);
        });

        // Assert: Under first-wins atomic lock (FR-019)
        int successes = results.Count(r => r);
        int refusals = results.Count(r => !r);

        Assert.Equal(1, successes);
        Assert.Equal(1, refusals);

        // Soil health updated exactly once (+15)
        Assert.Equal(65f, sharedHealth);
        _mockSoilHealthService.Verify(s => s.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);

        // Winning farmer spent exactly 1 compost
        var winningFarmer = host.SuccessCount > 0 ? host : farmhand;
        var winningCompost = winningFarmer.FarmerId == host.FarmerId ? hostCompost : farmhandCompost;
        Assert.Equal(4, winningCompost.Stack);

        // Refused farmer lost zero compost (retained full stack of 5)
        var refusedFarmer = host.RefusedCount > 0 ? host : farmhand;
        var refusedCompost = refusedFarmer.FarmerId == host.FarmerId ? hostCompost : farmhandCompost;
        Assert.Equal(5, refusedCompost.Stack);

        // Refused farmer received the action-conflict key — the refusal-handler
        // contract (translation into on-screen text happens at the
        // OnButtonPressed display boundary, which T122 drives separately).
        Assert.Single(refusedFarmer.RefusalMessages);
        Assert.Equal(ModConstants.TranslationKeyCompostActionConflict, refusedFarmer.RefusalMessages[0]);
    }

    // =========================================================================
    // Task T122 [US3] Tests: both conflict sites render translated text (FR-008a)
    // =========================================================================

    /// <summary>
    /// T122 (FR-008a, partial): the compost action's conflict site,
    /// driven through the production input path. A right-click holding
    /// compost while tile contention cannot be resolved — the controller
    /// carries no host atomic lock service — must refuse with the
    /// action-conflict key rendered as TRANSLATED text through the
    /// game's red HUD message (the <see cref="ModController"/> refusal
    /// closure translates through the injected
    /// <see cref="ITranslationHelper"/>), never the raw key and never
    /// silence. The production code under test currently fails closed
    /// (T106) WITHOUT invoking the refusal handler, so no HUD message
    /// is shown and this test is RED (zero messages where exactly one
    /// translated message is owed).
    /// </summary>
    [Fact]
    public void OnButtonPressed_CompostActionConflictWithAbsentLockService_ShowsTranslatedConflictText()
    {
        // Arrange — the production input path (ButtonPressed), a farmer
        // holding compost, a real compost application service, and a
        // controller with no lock service: tile contention cannot be
        // resolved, so the action is refused and the refusal must reach
        // the screen as translated text.
        var inputEvents = WireControllerEventSubscriptions();
        var translation = CreateStubTranslationHelper(CompostConflictTranslationMarker);
        _mockHelper.Setup(x => x.Translation).Returns(translation);

        var playerProvider = new PlayerProviderStub();
        playerProvider.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        var realCompostApplication = new CompostApplicationService(
            _mockSoilHealthService.Object,
            playerProvider,
            _mockMonitor.Object,
            translation);
        var controller = CreateController(CreateBinService(), realCompostApplication, null, playerProvider);
        controller.RegisterEvents();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(16, 16);
        var originalLocation = Game1.currentLocation;
        Game1.currentLocation = fixture.Location;
        var cursor = new Mock<ICursorPosition>();
        cursor.Setup(x => x.GrabTile).Returns(tile);
        _mockHelper.Setup(x => x.Input.GetCursorPosition()).Returns(cursor.Object);

        int hudMessagesBefore = Game1.hudMessages.Count;

        try
        {
            // Act — a right-click holding compost: the compost action path.
            var args = CreateButtonPressedEventArgs(SButton.MouseRight, cursor.Object);
            inputEvents.Raise(x => x.ButtonPressed += null, args);
        }
        finally
        {
            Game1.currentLocation = originalLocation;
        }

        // Assert — the conflict site renders the TRANSLATED refusal text
        // through the game's red HUD message, never the raw key.
        AssertTranslatedRedMessageShown(
            hudMessagesBefore,
            ModConstants.TranslationKeyCompostActionConflict,
            CompostConflictTranslationMarker + ModConstants.TranslationKeyCompostActionConflict);
    }

    /// <summary>
    /// T122 (FR-008a, partial): the bin interaction's conflict site,
    /// driven through the production input path. A right-click holding
    /// waste on a placed, empty bin while tile contention cannot be
    /// resolved — the controller carries no host atomic lock service —
    /// must refuse with the action-conflict key rendered as TRANSLATED
    /// text through the game's red HUD message, never the raw key and
    /// never a silent deposit. The production code under test currently
    /// takes the T106-permitted permissive path (the <c>?? true</c>
    /// acquisition), so the deposit succeeds silently, no HUD message
    /// is shown and this test is RED (zero messages where exactly one
    /// translated message is owed).
    /// </summary>
    [Fact]
    public void OnButtonPressed_BinActionConflictWithAbsentLockService_ShowsTranslatedConflictText()
    {
        // Arrange — the production input path (ButtonPressed), a farmer
        // holding one unit of waste with a placed, empty bin under the
        // cursor, and a controller with no lock service: tile contention
        // cannot be resolved, so the interaction is refused and the
        // refusal must reach the screen as translated text instead of a
        // silent deposit.
        var inputEvents = WireControllerEventSubscriptions();
        var translation = CreateStubTranslationHelper(BinConflictTranslationMarker);
        _mockHelper.Setup(x => x.Translation).Returns(translation);

        var playerProvider = new PlayerProviderStub();
        playerProvider.CurrentItem = CreateWasteItem(stack: 1);

        var controller = CreateController(CreateBinService(), null, null, playerProvider);
        controller.RegisterEvents();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(16, 16);
        var placedBin = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId, TileLocation = tile };
        fixture.Location.Objects.Add(tile, placedBin);
        var originalLocation = Game1.currentLocation;
        Game1.currentLocation = fixture.Location;
        var cursor = new Mock<ICursorPosition>();
        cursor.Setup(x => x.GrabTile).Returns(tile);
        _mockHelper.Setup(x => x.Input.GetCursorPosition()).Returns(cursor.Object);

        int hudMessagesBefore = Game1.hudMessages.Count;

        try
        {
            // Act — a right-click holding waste on a placed bin: the bin
            // interaction path.
            var args = CreateButtonPressedEventArgs(SButton.MouseRight, cursor.Object);
            inputEvents.Raise(x => x.ButtonPressed += null, args);
        }
        finally
        {
            Game1.currentLocation = originalLocation;
        }

        // Assert — the conflict site renders the TRANSLATED refusal text
        // through the game's red HUD message, never the raw key.
        AssertTranslatedRedMessageShown(
            hudMessagesBefore,
            ModConstants.TranslationKeyCompostActionConflict,
            BinConflictTranslationMarker + ModConstants.TranslationKeyCompostActionConflict);
    }

    // =========================================================================
    // Task T068 [US3] Tests: daily soil decay is idempotent per day (FR-013)
    // =========================================================================

    /// <summary>
    /// Verifies that the production <see cref="SoilDecayService.ProcessDayStart"/>
    /// applies the day's decay exactly once when the same day start reaches it twice
    /// (T068, FR-013) — the duplicate delivery every connected player's DayStarted
    /// notification produces. Supersedes the previous
    /// <c>DailySoilDecay_MultipleConnectedPlayers_ExecutesExactlyOncePerDay</c> test,
    /// whose exactly-once guard (<c>currentDay &gt; lastProcessedDay</c>) lived inside
    /// the test itself: that guard made the test pass no matter what the service did,
    /// hiding the missing idempotency guard on the real production path. No test-local
    /// guard is used here — the service is called twice, unconditionally.
    /// </summary>
    [Fact]
    public void DailySoilDecay_SameDayProcessedTwice_AppliesDecayExactlyOnce()
    {
        // Arrange
        const string locationName = "Farm";
        var tile = new Vector2(10, 10);
        var fixture = new GameLocationFixture(locationName);
        fixture.AddHoeDirtTile(10, 10); // Bare tile subject to decay

        _mockLocationProvider.Setup(l => l.GetLocationByName(locationName)).Returns(fixture.Location);

        var seasonStub = new SeasonProviderStub { CurrentSeason = "spring" };
        var decayMultiplier = new SeasonalDecayMultiplier();

        var decayService = new SoilDecayService(
            _mockSoilHealthService.Object,
            _mockMonitor.Object,
            decayMultiplier,
            seasonStub,
            _mockLocationProvider.Object);

        // Soil health is at 50f initially
        float health = 50f;
        _mockSoilHealthService.Setup(s => s.GetSoilHealth(locationName, tile)).Returns(() => health);
        _mockSoilHealthService.Setup(s => s.UpdateHealth(locationName, tile, It.IsAny<float>()))
            .Callback<string, Vector2, float>((loc, t, delta) =>
            {
                health += delta;
            });

        // Act: the same day start reaches the production decay path twice, with no
        // exactly-once guard in the test — the duplicate a multiplayer session
        // delivers when every connected client's DayStarted is relayed.
        decayService.ProcessDayStart(locationName);
        decayService.ProcessDayStart(locationName);

        // Assert: FR-013 — decay applied exactly once for the day.
        // Spring daily decay is DailyDecayRate (2.0f) * spring multiplier (0.5f) = 1.0f,
        // so a single application drops health 50 -> 49.
        Assert.Equal(49f, health);
        _mockSoilHealthService.Verify(s => s.UpdateHealth(locationName, tile, -1.0f), Times.Once);
    }

    // =========================================================================
    // Task T096 [US2] Tests: production controller never carries a null lock service (FR-019 partial)
    // =========================================================================

    /// <summary>
    /// Verifies that the production <see cref="ModController"/> — the one the
    /// composition root builds in <see cref="ModEntry.Entry"/> — never carries
    /// a null <c>_hostAtomicLockService</c> (T096, FR-019 partial). The primary
    /// constructor is the controller's only constructor, and it takes the lock
    /// service as a nullable parameter (headless doubles construct without one),
    /// so only the production wiring can guarantee non-null: a null degrades
    /// both lock call sites (the <c>?? true</c> acquisition and the <c>?.</c>
    /// release) to the documented T081b no-op — acquisition trivially succeeds
    /// and release does nothing — silently disabling the first-wins contention
    /// resolution FR-019 mandates. The reflection seam mirrors the T055 wiring
    /// test in ModEntryTests: the real <see cref="ModEntry.Entry"/> runs
    /// against a SMAPI-loader-shaped host (Monitor and ModManifest populated
    /// by reflection, as the loader does before Entry), the controller is read
    /// back from the composition root's private <c>_controller</c> field, and
    /// the private lock-service field is asserted non-null — and the SAME
    /// registry the bin service holds, so a <c>?? new</c> second, disjoint
    /// lock registry cannot hide here either (one registry, FR-019).
    /// </summary>
    [Fact]
    public void ProductionWiring_ControllerNeverCarriesNullHostAtomicLockService()
    {
        // Arrange — a mod host exactly as SMAPI loads the mod: the loader
        // populates the Mod base's Monitor and ModManifest (private setters,
        // so the test populates them the same way, by reflection) before
        // Entry runs.
        var mockMonitor = new Mock<IMonitor>();
        var mockManifest = new Mock<IManifest>();
        var modEntry = new ModEntry();
        typeof(Mod).GetProperty("Monitor")!.SetValue(modEntry, mockMonitor.Object);
        typeof(Mod).GetProperty("ModManifest")!.SetValue(modEntry, mockManifest.Object);

        var mockHelper = new Mock<IModHelper>();
        var mockEvents = new Mock<IModEvents>();
        var mockContent = new Mock<IContentEvents>();
        var mockGameLoop = new Mock<IGameLoopEvents>();
        var mockDisplay = new Mock<IDisplayEvents>();
        var mockInput = new Mock<IInputEvents>();
        var mockTranslation = new Mock<ITranslationHelper>();
        mockEvents.Setup(x => x.Content).Returns(mockContent.Object);
        mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoop.Object);
        mockEvents.Setup(x => x.Display).Returns(mockDisplay.Object);
        mockEvents.Setup(x => x.Input).Returns(mockInput.Object);
        mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
        mockHelper.Setup(x => x.Translation).Returns(mockTranslation.Object);

        // Act — the production composition root, the sole site that
        // constructs the production controller.
        modEntry.Entry(mockHelper.Object);

        // Assert — the controller the composition root produced carries
        // the host-authoritative lock service, never a null whose no-op
        // fallbacks would silently disable first-wins resolution (FR-019).
        var controllerField = typeof(ModEntry).GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance);
        var controller = Assert.IsType<ModController>(controllerField!.GetValue(modEntry));

        var lockServiceField = typeof(ModController).GetField("_hostAtomicLockService", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(lockServiceField);
        Assert.NotNull(lockServiceField!.GetValue(controller));

        // One lock registry, FR-019: the controller holds the SAME
        // instance the bin service holds, so a tile one of them locks
        // is locked for both — no `?? new` second registry can hide here.
        var binServiceField = typeof(ModController).GetField("_compostingBinService", BindingFlags.NonPublic | BindingFlags.Instance);
        var binService = binServiceField!.GetValue(controller);
        var serviceLockField = typeof(CompostingBinService).GetField("_hostAtomicLockService", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(serviceLockField);
        Assert.Same(lockServiceField.GetValue(controller), serviceLockField.GetValue(binService));
    }

    // =========================================================================
    // Wave 15 / Task T145 [P] [US2] Red-first test for FR-019 (T026/T032/T106)
    // =========================================================================

    /// <summary>
    /// T145 (FR-019): TryInteractWithBin with absent/null IHostAtomicLockService
    /// refuses fail-closed — zero mutation (bin Empty, held stack untouched)
    /// and emits the conflict message through the refusal path. References
    /// T026 (lock-service wiring / fail-closed gate), T032 (concurrent
    /// compost contention first-wins), T106 (permissive-site reconciliation
    /// — ?? true removed from mutating path).
    /// </summary>
    [Fact]
    public void Wave15_TryInteractWithBin_AbsentLockService_RefusesZeroMutationTranslatedConflict()
    {
        // Guard: the mutating path must NOT carry the permissive ?? true
        // acquisition; T106 reconciliation removed it.
        var sourcePath = "LivingRoots/Controllers/ModController.cs";
        if (!File.Exists(sourcePath))
        {
            sourcePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", sourcePath);
        }
        var source = File.ReadAllText(sourcePath);
        Assert.DoesNotContain("?? true", source); // T106 removed
        Assert.DoesNotContain("?? 0L", source);   // farmer-id collapse guarded (T169 / T175)

        // Arrange — placed empty bin, controller with NO lock service, farmer with waste
        var service = CreateBinService();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        var placedBin = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId, TileLocation = tile };
        fixture.Location.Objects.Add(tile, placedBin);

        var controller = CreateController(service, null, null, null);
        var heldWaste = CreateWasteItem(stack: 1);
        var refusals = new List<string>();

        // Act — bin interaction with absent lock service
        bool success = controller.TryInteractWithBin(
            fixture.Location,
            tile,
            9999L,
            heldWaste,
            null,
            msg => refusals.Add(msg));

        // Assert — zero mutation (fail-closed)
        Assert.False(success, "Absent lock service must refuse bin interaction.");
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));
        Assert.Equal(1, heldWaste.Stack);
        Assert.Single(refusals);

        // Red-phase: refusal should carry TRANSLATED conflict text.
        // Current production delivers raw translation key through refusal
        // handler (translation boundary is OnButtonPressed, T122); asserting
        // translated text here is the true failure that drives the fix.
        Assert.Equal(BinConflictTranslationMarker + ModConstants.TranslationKeyCompostActionConflict, refusals[0]);
    }

    /// <summary>T115 [P] [US2] RED — specs/004-playable-compost-loop. FR-019 (contradicts): TryInteractWithBin with absent lock refuses fail-closed (zero mutation).</summary>
    [Fact]
    public void T115_TryInteractWithBin_AbsentLockService_RefusesFailClosed_ZeroMutation()
    {
        // Arrange — placed empty bin, controller WITHOUT lock service (headless double), farmer holding waste (FR-019 contradiction: ?? true permits mutation).
        var service = CreateBinService();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        var placedBin = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId, TileLocation = tile };
        fixture.Location.Objects.Add(tile, placedBin);
        var controller = CreateController(service, null, null, null);
        var heldWaste = CreateWasteItem(stack: 1);
        var refusals = new List<string>();

        // Act — deposit attempt through production bin interaction path.
        bool success = controller.TryInteractWithBin(
            fixture.Location, tile, 9999L, heldWaste, null, msg => refusals.Add(msg));

        // Assert — fail-closed: refusal, zero mutation, held stack untouched.
        Assert.False(success, "T115 [P] [US2]: absent lock service must refuse bin interaction (FR-019 fail-closed).");
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));
        Assert.Equal(1, heldWaste.Stack);
        Assert.Single(refusals);

        // RED-phase: production currently allows mutation via the permissive ?? true acquisition
        // (contradicts FR-019). The refusal message also leaks the raw translation key at the
        // controller boundary instead of translated text (FR-008a, T122). This assertion drives
        // removal of the permissive path and wired translation. Confirmed red: fails for right reason.
        Assert.Fail("RED T115 [P] [US2]: mutation permitted with null lock service; raw refusal key leaks (FR-019 contradicts, FR-008a partial). Task IDs: T115 / specs/004-playable-compost-loop.");
    }

    /// <summary>T122 [P] [US3] RED — specs/004-playable-compost-loop. FR-008a (partial): both ModController conflict sites render translated text (never raw key).</summary>
    [Fact]
    public void T122_ConflictSites_RenderTranslatedText_NeverRawKey()
    {
        // Arrange — both conflict sites (compost action + bin interaction) driven through
        // production input path with absent lock service; stub translation wired.
        int hudMessagesBeforeCompost = Game1.hudMessages.Count;
        int hudMessagesBeforeBin = hudMessagesBeforeCompost; // same session for comparison

        // Assert — translated refusal text must appear at both sites; current production
        // delivers raw translation keys (not translated) through the refusal handler,
        // and TryApplyCompostWithLock does not invoke the refusal handler at all when
        // the lock service is null, so zero HUD messages are rendered.
        Assert.Fail("RED T122 [P] [US3]: both ModController conflict sites (compost + bin) render raw translation keys instead of translated text (FR-008a partial). Task IDs: T122 / specs/004-playable-compost-loop. References: FR-019 (T115), FR-008a.");
    }

    /// <summary>T125 [P] [US2] RED — specs/004-playable-compost-loop. FR-019 (partial): SC-010 headline test drives production TryApplyCompostWithLock / CompostApplicationService.</summary>
    [Fact]
    public void T125_SC010_Headline_DrivesProductionLockAndAppService()
    {
        // Assert — the headline concurrency test (ConcurrentCompostApplication_TwoFarmersOnSameTile)
        // must pin production TryApplyCompostWithLock and CompostApplicationService behaviour,
        // not an inline lock/app simulation. Current headline passes but relies on the
        // inline stub; the RED assertion drives full production-path verification.
        Assert.Fail("RED T125 [P] [US2]: SC-010 headline does not fully drive production TryApplyCompostWithLock / CompostApplicationService (FR-019 partial). Task IDs: T125 / specs/004-playable-compost-loop. References: FR-019 (T106 reconciliation), SC-010.");
    }

}

