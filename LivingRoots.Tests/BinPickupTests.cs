using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using LivingRoots.Domain.Services;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Unit tests for composting bin pickup, tool/explosion dislodgement, and modData progress preservation.
/// Validates Task T012 [P] [US1] under Feature 004 (Playable Compost Loop, FR-002, FR-005c, SC-009).
/// </summary>
public class BinPickupTests
{
    private readonly Mock<IModHelper> _mockHelper;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<IManifest> _mockManifest;
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
    private readonly Mock<ICompostingBinService> _mockCompostingBinService;
    private readonly Mock<ISoilDecayService> _mockSoilDecayService;

    public BinPickupTests()
    {
        GameStateFixture.Install();

        _mockHelper = new Mock<IModHelper>();
        _mockMonitor = new Mock<IMonitor>();
        _mockManifest = new Mock<IManifest>();
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockSaveIdProvider = new Mock<ISaveIdProvider>();
        _mockCompostingBinService = new Mock<ICompostingBinService>();
        _mockSoilDecayService = new Mock<ISoilDecayService>();

        _mockManifest.Setup(x => x.UniqueID).Returns("test.mod.id");
        _mockManifest.Setup(x => x.Version).Returns(new SemanticVersion(1, 0, 0));
    }

    private ModController CreateController(IPlayerProvider? playerProvider = null)
    {
        return new ModController(
            _mockHelper.Object,
            _mockMonitor.Object,
            _mockManifest.Object,
            _mockSoilHealthService.Object,
            _mockSaveIdProvider.Object,
            _mockCompostingBinService.Object,
            _mockSoilDecayService.Object,
            playerProvider: playerProvider);
    }

    private static StardewValley.Object CreatePlacedBinObject()
    {
        return new StardewValley.Object
        {
            ItemId = ModConstants.CompostingBinItemId,
            Name = "Composting Bin"
        };
    }

    /// <summary>
    /// Dislodging a bin with empty contents yields a normal bin item without progress modData,
    /// removes the world object, and clears state without leaving orphaned entries (FR-002).
    /// </summary>
    [Fact]
    public void TryDislodgeBin_EmptyBin_YieldsNormalBinItemAndCleansUpWorldObject()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        var placedBin = CreatePlacedBinObject();
        fixture.Location.Objects.Add(tile, placedBin);

        var emptyYieldedItem = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
        _mockCompostingBinService.Setup(x => x.GetBinState("Farm", tile)).Returns(CompostingBinState.Empty);
        _mockCompostingBinService.Setup(x => x.CreateDislodgedBinItem("Farm", tile)).Returns(emptyYieldedItem);

        var inventory = new RecordingPlayerInventory();

        var result = controller.TryDislodgeBin(fixture.Location, tile, inventory);

        Assert.True(result, "Dislodging a valid empty bin must succeed.");
        Assert.False(fixture.Location.Objects.ContainsKey(tile), "Dislodged bin must be removed from world Objects.");
        Assert.Single(inventory.AddedItems);
        var yieldedItem = inventory.AddedItems[0];
        Assert.Equal(ModConstants.CompostingBinItemId, yieldedItem.ItemId);
        Assert.False(yieldedItem.modData.ContainsKey(ModConstants.ModDataInputIdKey), "Empty bin item must not contain input ID modData.");
        Assert.False(yieldedItem.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey), "Empty bin item must not contain days remaining modData.");
        _mockCompostingBinService.Verify(x => x.OnObjectRemoved("Farm", tile), Times.Once, "Must notify service of object removal to prevent orphaned state.");
    }

    /// <summary>
    /// Dislodging a bin currently processing waste preserves Item.modData (LivingRoots.InputId,
    /// LivingRoots.DaysRemaining) on the yielded item per FR-005c.
    /// </summary>
    [Fact]
    public void TryDislodgeBin_ProcessingBin_PreservesModDataProgressInYieldedItem()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        var placedBin = CreatePlacedBinObject();
        fixture.Location.Objects.Add(tile, placedBin);

        var processingItem = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
        processingItem.modData[ModConstants.ModDataInputIdKey] = "(O)24"; // Parsnip
        processingItem.modData[ModConstants.ModDataDaysRemainingKey] = "1";

        _mockCompostingBinService.Setup(x => x.GetBinState("Farm", tile)).Returns(CompostingBinState.Processing);
        _mockCompostingBinService.Setup(x => x.CreateDislodgedBinItem("Farm", tile)).Returns(processingItem);

        var inventory = new RecordingPlayerInventory();

        var result = controller.TryDislodgeBin(fixture.Location, tile, inventory);

        Assert.True(result, "Dislodging a processing bin must succeed.");
        Assert.False(fixture.Location.Objects.ContainsKey(tile), "Dislodged bin must be removed from world Objects.");
        Assert.Single(inventory.AddedItems);
        var yieldedItem = inventory.AddedItems[0];
        Assert.Equal(ModConstants.CompostingBinItemId, yieldedItem.ItemId);
        Assert.Equal("(O)24", yieldedItem.modData[ModConstants.ModDataInputIdKey]);
        Assert.Equal("1", yieldedItem.modData[ModConstants.ModDataDaysRemainingKey]);
        _mockCompostingBinService.Verify(x => x.OnObjectRemoved("Farm", tile), Times.Once, "Must clear world object record to prevent duplicate state.");
    }

    /// <summary>
    /// A bin carrying modData progress enforces a maximum stack size of 1 (unstackable) per FR-005c.
    /// An empty bin carries default stack capacity.
    /// </summary>
    [Fact]
    public void DislodgedBin_WithModDataProgress_EnforcesMaxStackSizeOne()
    {
        var mockModData = new Mock<IModDataService>();
        var mockSaveId = new Mock<ISaveIdProvider>();
        var mockWasteVal = new Mock<IOrganicWasteValidator>();
        var mockMon = new Mock<IMonitor>();
        var timeStub = new TimeProviderStub();
        var factory = new CompostingBinFactory();
        var inventory = new RecordingPlayerInventory();

        var service = new CompostingBinService(
            mockModData.Object,
            mockSaveId.Object,
            mockWasteVal.Object,
            mockMon.Object,
            timeStub,
            factory,
            inventory);

        var processingBinItem = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
        processingBinItem.modData[ModConstants.ModDataInputIdKey] = "(O)24";
        processingBinItem.modData[ModConstants.ModDataDaysRemainingKey] = "2";

        var emptyBinItem = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };

        Assert.Equal(1, service.GetMaxStackSize(processingBinItem));
        Assert.True(service.GetMaxStackSize(emptyBinItem) > 1, "Empty bin item without progress must remain stackable.");
    }

    /// <summary>
    /// T117 (FR-005c): the production dislodge path — the
    /// empty-hands right-click site that
    /// <c>ModController.OnButtonPressed</c> (ModController.cs:1880)
    /// invokes to pick a bin up — receives the composting
    /// service's dislodged bin carrying its batch progress
    /// (<see cref="ModConstants.ModDataInputIdKey"/> /
    /// <see cref="ModConstants.ModDataDaysRemainingKey"/>) in
    /// <c>Item.modData</c>. Whatever stack size that yielded
    /// bin carries — a stacked save entry, a stacking-aware
    /// dislodgement — the production path must hand the player's
    /// inventory a single unstackable unit: the stack clamped to
    /// one. The unit-level <c>ICompostingBinService.GetMaxStackSize</c>
    /// contract above does not prove this; the clamp must happen
    /// on the production call path, which today never clamps, so
    /// every carried stack above one passes through unclamped.
    /// FsCheck quantifies over the carried stack size; the
    /// progress modData literals document the progress contract
    /// (fixed-literal assertions on the generated dimension are
    /// N/A — the invariant under test is the clamp).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DislodgedProgressCarryingBin_ThroughProductionPath_ResolvesAsSingleUnstackableItem(
        PositiveInt carriedStack)
    {
        int carried = (carriedStack.Get % 999) + 1; // 1..999: any stack the dislodgement may yield

        var controller = CreateControllerWithFreshBinService(out var binService);
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        fixture.Location.Objects.Add(tile, CreatePlacedBinObject());

        var processingBin = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
        processingBin.modData[ModConstants.ModDataInputIdKey] = "(O)24"; // Parsnip
        processingBin.modData[ModConstants.ModDataDaysRemainingKey] = "2";
        processingBin.Stack = carried;

        binService.Setup(x => x.GetBinState("Farm", tile)).Returns(CompostingBinState.Processing);
        binService.Setup(x => x.CreateDislodgedBinItem("Farm", tile)).Returns(processingBin);
        // The real service returns 1 for a progress-carrying bin; a bare
        // Moq int returns 0, which is not a valid stack cap and would make
        // the clamp assertion vacuous instead of exercising the bound.
        binService.Setup(x => x.GetMaxStackSize(processingBin)).Returns(1);

        var inventory = new RecordingPlayerInventory();

        var result = controller.TryDislodgeBin(fixture.Location, tile, inventory);

        var yielded = inventory.AddedItems.Count == 1 ? inventory.AddedItems[0] : null;
        return result
            && !fixture.Location.Objects.ContainsKey(tile)
            && yielded != null
            && yielded.ItemId == ModConstants.CompostingBinItemId
            && yielded.modData[ModConstants.ModDataInputIdKey] == "(O)24"
            && yielded.modData[ModConstants.ModDataDaysRemainingKey] == "2"
            && yielded.Stack == 1;
    }

    /// <summary>
    /// If the player's inventory is full when a bin is dislodged, the item drops as floating
    /// world debris retaining its modData progress rather than being destroyed per FR-005c and SC-009.
    /// </summary>
    [Fact]
    public void TryDislodgeBin_WhenPlayerInventoryFull_DropsFloatingWorldDebrisWithModData()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        var placedBin = CreatePlacedBinObject();
        fixture.Location.Objects.Add(tile, placedBin);

        var processingItem = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
        processingItem.modData[ModConstants.ModDataInputIdKey] = "(O)88"; // Coconut
        processingItem.modData[ModConstants.ModDataDaysRemainingKey] = "2";

        _mockCompostingBinService.Setup(x => x.GetBinState("Farm", tile)).Returns(CompostingBinState.Processing);
        _mockCompostingBinService.Setup(x => x.CreateDislodgedBinItem("Farm", tile)).Returns(processingItem);

        var fullInventory = new RecordingPlayerInventory { AcceptsItems = false };
        var droppedDebrisList = new List<Item>();

        var result = controller.TryDislodgeBin(
            fixture.Location,
            tile,
            fullInventory,
            (item, pos, loc) => droppedDebrisList.Add(item));

        Assert.True(result, "Dislodge should succeed even when player inventory is completely full.");
        Assert.False(fixture.Location.Objects.ContainsKey(tile), "Machine must be removed from world objects.");
        Assert.Empty(fullInventory.AddedItems);
        Assert.Single(droppedDebrisList);

        var debrisItem = droppedDebrisList[0];
        Assert.NotNull(debrisItem);
        Assert.Equal(ModConstants.CompostingBinItemId, debrisItem.ItemId);
        Assert.Equal("(O)88", debrisItem.modData[ModConstants.ModDataInputIdKey]);
        Assert.Equal("2", debrisItem.modData[ModConstants.ModDataDaysRemainingKey]);
        _mockCompostingBinService.Verify(x => x.OnObjectRemoved("Farm", tile), Times.Once);
    }

    /// <summary>
    /// T118 (FR-005c): a bin dislodged through the production
    /// input path returns to the player's inventory when
    /// inventory space is available — floating world debris is
    /// the full-inventory fallback, never the default
    /// destination. The production entry is the
    /// <c>Input.ButtonPressed</c> handler
    /// (<c>ModController.OnButtonPressed</c>), whose
    /// empty-hands dislodgement site (ModController.cs:1880)
    /// calls <c>TryDislodgeBin(location, binTile)</c> passing
    /// no inventory, so every dislodged bin currently falls
    /// through to <c>Game1.createItemDebris</c> instead of the
    /// player's inventory (RED: the bin never reaches the
    /// farmer's inventory). The farmer follows the
    /// <c>GameStateFixture</c> uninitialized-object pattern
    /// with its public <c>netItems</c> NetRef loaded with a
    /// real <c>Inventory</c>, so the farmer's inventory — the
    /// space the bin must land in — is readable in the headless
    /// test host.
    /// </summary>
    [Fact]
    public void OnButtonPressed_RightClickOnProcessingBinWithEmptyHands_ReturnsBinToPlayerInventory()
    {
        var inputEvents = WireControllerEventSubscriptions();
        var farmer = (StardewValley.Farmer)FormatterServices.GetUninitializedObject(typeof(StardewValley.Farmer));
        var playerId = new Netcode.NetLong();
        playerId.Value = 1L;
        // Farmer's identity and inventory fields are readonly and initialised
        // inline — allocations GetUninitializedObject skips — so they are
        // installed through reflection, the same constructor-bypass the
        // GameStateFixture applies to Game1.game1.
        typeof(StardewValley.Farmer)
            .GetField("uniqueMultiplayerID", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(farmer, playerId);
        typeof(StardewValley.Farmer)
            .GetField("netItems", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(
                farmer,
                new Netcode.NetRef<StardewValley.Inventories.Inventory>
                {
                    Value = new StardewValley.Inventories.Inventory()
                });

        var playerProvider = new PlayerProviderStub
        {
            CurrentItem = null,
            CurrentPlayer = farmer
        };

        var controller = CreateController(playerProvider);
        controller.RegisterEvents();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(15, 20);
        fixture.Location.Objects.Add(tile, CreatePlacedBinObject());

        var processingItem = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
        processingItem.modData[ModConstants.ModDataInputIdKey] = "(O)88"; // Coconut
        processingItem.modData[ModConstants.ModDataDaysRemainingKey] = "2";

        _mockCompostingBinService.Setup(x => x.GetBinState("Farm", tile)).Returns(CompostingBinState.Processing);
        _mockCompostingBinService.Setup(x => x.CreateDislodgedBinItem("Farm", tile)).Returns(processingItem);

        var originalLocation = Game1.currentLocation;
        Game1.currentLocation = fixture.Location;
        var cursor = new Mock<ICursorPosition>();
        cursor.Setup(x => x.GrabTile).Returns(tile);
        _mockHelper.Setup(x => x.Input.GetCursorPosition()).Returns(cursor.Object);

        int debrisBefore = fixture.Location.debris.Count;

        try
        {
            var args = CreateButtonPressedEventArgs(SButton.MouseRight, cursor.Object);
            inputEvents.Raise(x => x.ButtonPressed += null, args);
        }
        finally
        {
            Game1.currentLocation = originalLocation;
        }

        _mockCompostingBinService.Verify(
            x => x.CreateDislodgedBinItem("Farm", tile),
            Times.Once,
            "Right-clicking an occupied bin tile with empty hands must dislodge the bin through the production input path.");
        _mockCompostingBinService.Verify(
            x => x.OnObjectRemoved("Farm", tile),
            Times.Once,
            "Dislodgement must clear the service record to prevent orphaned state.");
        Assert.False(fixture.Location.Objects.ContainsKey(tile), "Dislodgement must remove the world object from the tile.");

        // FR-005c: with inventory space available the bin
        // returns to the player's inventory carrying its batch
        // progress — it is not littered as floating world debris.
        Assert.Single(farmer.Items);
        var returned = farmer.Items[0];
        Assert.Equal(ModConstants.CompostingBinItemId, returned.ItemId);
        Assert.Equal("(O)88", returned.modData[ModConstants.ModDataInputIdKey]);
        Assert.Equal("2", returned.modData[ModConstants.ModDataDaysRemainingKey]);

        // Debris is the full-inventory fallback only: no debris
        // may be created while inventory space was available. In
        // the headless host the debris branch
        // (Game1.createItemDebris) crashes with a
        // NullReferenceException the handler reports through
        // this exact error log, so a clean inventory-first
        // dislodge must also leave the error log untouched.
        Assert.Equal(debrisBefore, fixture.Location.debris.Count);
        _mockMonitor.Verify(
            x => x.Log("Error in button pressed handler.", LogLevel.Error),
            Times.Never,
            "The production dislodge fell through to Game1.createItemDebris instead of returning the bin to the player's inventory.");
    }

    /// <summary>
    /// Dislodging a bin via tool hit or explosion removes the world object and ensures zero
    /// orphaned records remain in the service runtime cache or save data per FR-002.
    /// </summary>
    [Fact]
    public void TryDislodgeBin_ToolOrExplosionDislodge_RemovesBinAndLeavesZeroOrphanedState()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(15, 20);

        var placedBin = CreatePlacedBinObject();
        fixture.Location.Objects.Add(tile, placedBin);

        var normalItem = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
        _mockCompostingBinService.Setup(x => x.CreateDislodgedBinItem("Farm", tile)).Returns(normalItem);

        var inventory = new RecordingPlayerInventory();

        var result = controller.TryDislodgeBin(fixture.Location, tile, inventory);

        Assert.True(result);
        Assert.False(fixture.Location.Objects.ContainsKey(tile), "Object must be removed from tile.");
        _mockCompostingBinService.Verify(x => x.OnObjectRemoved("Farm", tile), Times.Once);
    }

    /// <summary>
    /// Attempting to dislodge when no composting bin exists at the target tile returns false
    /// and performs zero inventory or state mutations.
    /// </summary>
    [Fact]
    public void TryDislodgeBin_WhenNoBinAtTile_ReturnsFalseAndTakesNoAction()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(5, 5); // Empty tile

        var inventory = new RecordingPlayerInventory();

        var result = controller.TryDislodgeBin(fixture.Location, tile, inventory);

        Assert.False(result, "Dislodging a non-existent bin must return false.");
        Assert.Empty(inventory.AddedItems);
        _mockCompostingBinService.Verify(x => x.OnObjectRemoved(It.IsAny<string>(), It.IsAny<Vector2>()), Times.Never);
    }

    /// <summary>
    /// Attempting to dislodge when controller is disposed returns false.
    /// </summary>
    [Fact]
    public void TryDislodgeBin_WhenDisposed_ReturnsFalse()
    {
        var controller = CreateController();
        controller.Dispose();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        fixture.Location.Objects.Add(tile, CreatePlacedBinObject());

        var inventory = new RecordingPlayerInventory();
        var result = controller.TryDislodgeBin(fixture.Location, tile, inventory);

        Assert.False(result, "Disposed controller must refuse dislodgement.");
    }

    /// <summary>
    /// Null location argument throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void TryDislodgeBin_WithNullLocation_ThrowsArgumentNullException()
    {
        var controller = CreateController();

        Assert.Throws<ArgumentNullException>(() => controller.TryDislodgeBin(null!, new Vector2(1, 1)));
    }

    /// <summary>
    /// Direct contract test verifying that CompostingBinService.CreateDislodgedBinItem serializes
    /// modData progress when a bin was actively processing waste.
    /// </summary>
    [Fact]
    public void CompostingBinService_CreateDislodgedBinItem_PreservesActiveBatchProgress()
    {
        var mockModData = new Mock<IModDataService>();
        var mockSaveId = new Mock<ISaveIdProvider>();
        var mockWasteVal = new Mock<IOrganicWasteValidator>();
        var mockMon = new Mock<IMonitor>();
        var timeStub = new TimeProviderStub();
        var factory = new CompostingBinFactory();
        var inventory = new RecordingPlayerInventory();

        var service = new CompostingBinService(
            mockModData.Object,
            mockSaveId.Object,
            mockWasteVal.Object,
            mockMon.Object,
            timeStub,
            factory,
            inventory);

        var tile = new Vector2(10, 10);
        var wasteItem = ItemFactory.CreateValidWasteItem();
        mockWasteVal.Setup(x => x.IsValidOrganicWaste(wasteItem)).Returns(true);

        // Add waste to put bin into Processing state
        service.AddWaste("Farm", tile, wasteItem);
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        var dislodgedItem = service.CreateDislodgedBinItem("Farm", tile);

        Assert.NotNull(dislodgedItem);
        Assert.Equal(ModConstants.CompostingBinItemId, dislodgedItem.ItemId);
        Assert.True(dislodgedItem.modData.ContainsKey(ModConstants.ModDataInputIdKey), "Dislodged item must carry input ID in modData.");
        Assert.True(dislodgedItem.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey), "Dislodged item must carry days remaining in modData.");
        Assert.Equal(1, service.GetMaxStackSize(dislodgedItem));
    }

    /// <summary>
    /// T065 (FR-005c): a bin dislodged while its batch is running resolves
    /// as the (BC) big craftable the recipe yields — not an (O) object —
    /// and carries the batch progress in modData with a maximum stack
    /// size of one.
    /// </summary>
    [Fact]
    public void CompostingBinService_CreateDislodgedBinItem_ResolvesAsBigCraftableCarryingProgressWithStackSizeOne()
    {
        var mockModData = new Mock<IModDataService>();
        var mockSaveId = new Mock<ISaveIdProvider>();
        var mockWasteVal = new Mock<IOrganicWasteValidator>();
        var mockMon = new Mock<IMonitor>();
        var timeStub = new TimeProviderStub();
        var factory = new CompostingBinFactory();
        var inventory = new RecordingPlayerInventory();

        var service = new CompostingBinService(
            mockModData.Object,
            mockSaveId.Object,
            mockWasteVal.Object,
            mockMon.Object,
            timeStub,
            factory,
            inventory);

        var tile = new Vector2(10, 10);
        var wasteItem = ItemFactory.CreateValidWasteItem();
        mockWasteVal.Setup(x => x.IsValidOrganicWaste(wasteItem)).Returns(true);

        // Add waste to put bin into Processing state
        service.AddWaste("Farm", tile, wasteItem);
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        var dislodgedItem = service.CreateDislodgedBinItem("Farm", tile);

        Assert.NotNull(dislodgedItem);
        Assert.Equal(ModConstants.CompostingBinItemId, dislodgedItem.ItemId);
        Assert.Equal(
            "(BC)" + ModConstants.CompostingBinItemId,
            dislodgedItem.QualifiedItemId);
        Assert.True(dislodgedItem.modData.ContainsKey(ModConstants.ModDataInputIdKey), "Dislodged item must carry input ID in modData.");
        Assert.True(dislodgedItem.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey), "Dislodged item must carry days remaining in modData.");
        Assert.Equal(1, service.GetMaxStackSize(dislodgedItem));
    }

    /// <summary>
    /// Builds a controller whose <see cref="ICompostingBinService"/>
    /// mock is created per call. FsCheck runs each property
    /// invocation in isolation, so the per-invocation setups the
    /// T117 property makes must not accumulate on the class-level
    /// mock the [Fact] tests share.
    /// </summary>
    /// <param name="binService">The fresh composting-bin service mock the controller was built with.</param>
    /// <returns>A controller over freshly created mocks.</returns>
    private ModController CreateControllerWithFreshBinService(out Mock<ICompostingBinService> binService)
    {
        var helper = new Mock<IModHelper>();
        var monitor = new Mock<IMonitor>();
        var manifest = new Mock<IManifest>();
        var soilHealthService = new Mock<ISoilHealthService>();
        var saveIdProvider = new Mock<ISaveIdProvider>();
        binService = new Mock<ICompostingBinService>();
        var soilDecayService = new Mock<ISoilDecayService>();

        manifest.Setup(x => x.UniqueID).Returns("test.mod.id");
        manifest.Setup(x => x.Version).Returns(new SemanticVersion(1, 0, 0));

        return new ModController(
            helper.Object,
            monitor.Object,
            manifest.Object,
            soilHealthService.Object,
            saveIdProvider.Object,
            binService.Object,
            soilDecayService.Object);
    }

    /// <summary>
    /// Wires <see cref="IModHelper.Events"/> with mock event
    /// sources so <see cref="ModController.RegisterEvents"/>
    /// subscribes the controller's production handlers, which a
    /// test then raises directly through the
    /// <see cref="IInputEvents.ButtonPressed"/> event source —
    /// the production input path.
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
    /// Builds a <see cref="ButtonPressedEventArgs"/> for the
    /// given button and cursor.
    /// </summary>
    /// <remarks>
    /// SMAPI constructs these event args internally — the type
    /// exposes no public constructor and read-only properties —
    /// so the test host invokes the internal constructor through
    /// reflection. The instance is fully initialised by the real
    /// constructor (no private state is poked), the same class
    /// of constructor workaround <see cref="GameStateFixture"/>
    /// applies to <see cref="Game1"/>.
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
}
