using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
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
/// Unit tests for composting bin world placement validation and initialization.
/// Validates Task T011 [P] [US1] under Feature 004 (Playable Compost Loop, FR-001, FR-002, FR-005c).
/// </summary>
public class BinPlacementTests
{
    private readonly Mock<IModHelper> _mockHelper;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<IManifest> _mockManifest;
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
    private readonly Mock<ICompostingBinService> _mockCompostingBinService;
    private readonly Mock<ISoilDecayService> _mockSoilDecayService;

    public BinPlacementTests()
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

    /// <summary>
    /// Wires <see cref="IModHelper.Events"/> with the game-loop, input, display
    /// and world event sources so <see cref="ModController.RegisterEvents"/>
    /// subscribes the controller's production handlers, which a test then
    /// raises directly through the matching mocked source.
    /// </summary>
    /// <returns>
    /// The mocked <see cref="IInputEvents"/> and <see cref="IWorldEvents"/> the
    /// controller subscribed to.
    /// </returns>
    private (Mock<IInputEvents> Input, Mock<IWorldEvents> World) WireControllerEventSubscriptionsWithWorld()
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
        return (inputEvents, worldEvents);
    }

    /// <summary>
    /// Wires <see cref="IModHelper.Events"/> with mock event sources so
    /// <see cref="ModController.RegisterEvents"/> subscribes the controller's
    /// production handlers, which a test then raises directly through the
    /// <see cref="IInputEvents.ButtonPressed"/> event source.
    /// </summary>
    /// <returns>The mocked <see cref="IInputEvents"/> the controller subscribed to.</returns>
    private Mock<IInputEvents> WireControllerEventSubscriptions()
    {
        return WireControllerEventSubscriptionsWithWorld().Input;
    }

    /// <summary>
    /// Builds an <see cref="ObjectListChangedEventArgs"/> reporting
    /// <paramref name="removed"/> objects taken out of
    /// <paramref name="location"/> — the SMAPI event the game raises after a
    /// tool hit or an explosion removes a big craftable from the world.
    /// </summary>
    /// <remarks>
    /// Like <see cref="ButtonPressedEventArgs"/>, the type exposes no public
    /// constructor and read-only properties, so the test host invokes the
    /// internal constructor through reflection; the instance is fully
    /// initialised by the real constructor (no private state is poked).
    /// </remarks>
    /// <param name="location">The location the objects were removed from.</param>
    /// <param name="removed">The tiles and objects the game removed.</param>
    /// <returns>An initialised <see cref="ObjectListChangedEventArgs"/>.</returns>
    private static ObjectListChangedEventArgs CreateObjectListChangedEventArgs(
        StardewValley.GameLocation location,
        IReadOnlyCollection<KeyValuePair<Vector2, StardewValley.Object>> removed)
    {
        var constructor = typeof(ObjectListChangedEventArgs)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();
        var arguments = new object[]
        {
            location,
            Array.Empty<KeyValuePair<Vector2, StardewValley.Object>>(),
            removed,
        };

        return (ObjectListChangedEventArgs)constructor.Invoke(arguments);
    }

    /// <summary>
    /// Reduces an arbitrary generated string to an alphanumeric location-name
    /// fragment, falling back to <paramref name="fallback"/> when the generated
    /// value carries no such characters (so the fragment is never empty).
    /// </summary>
    /// <param name="raw">The generated string to reduce.</param>
    /// <param name="fallback">The fragment used when nothing survives.</param>
    /// <returns>A non-empty alphanumeric fragment.</returns>
    private static string SanitizeLocationNameFragment(string? raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        var builder = new StringBuilder();
        foreach (char character in raw)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.Length == 0 ? fallback : builder.ToString();
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

    private static Item CreateBinItem(int stack = 1)
    {
        return new StardewValley.Object
        {
            ItemId = ModConstants.CompostingBinItemId,
            Stack = stack
        };
    }

    /// <summary>
    /// Covers a rectangular footprint with a completed building at the given
    /// tile coordinates, the way the game's own building placement does.
    /// </summary>
    /// <remarks>
    /// Adding to <see cref="StardewValley.GameLocation.buildings"/> fires the
    /// location's per-item wiring, whose last step reads the
    /// <see cref="Game1.netWorldState"/> static that only a real boot
    /// populates, so an empty one is installed for the add and restored
    /// afterwards — the same save/restore pattern the placement tests apply
    /// to <see cref="Game1.currentLocation"/>. The footprint members are
    /// public <c>NetInt</c> fields with no public property setters, so the
    /// backing values are the only way to populate the footprint from a test.
    /// </remarks>
    /// <param name="location">The location whose building footprint is populated.</param>
    /// <param name="tileX">The footprint's origin X tile coordinate.</param>
    /// <param name="tileY">The footprint's origin Y tile coordinate.</param>
    /// <param name="tilesWide">The footprint width in tiles.</param>
    /// <param name="tilesHigh">The footprint height in tiles.</param>
    private static void AddBuildingFootprint(
        StardewValley.GameLocation location,
        int tileX,
        int tileY,
        int tilesWide,
        int tilesHigh)
    {
        var building = new StardewValley.Buildings.Building();
#pragma warning disable AvoidNetField
        // Building's tile/footprint members are public NetInt fields with no
        // public property setters, so the backing values are the only way to
        // populate the footprint from a test.
        building.tileX.Value = tileX;
        building.tileY.Value = tileY;
        building.tilesWide.Value = tilesWide;
        building.tilesHigh.Value = tilesHigh;
#pragma warning restore AvoidNetField

        var originalNetWorldState = Game1.netWorldState;
        Game1.netWorldState = new Netcode.NetRoot<StardewValley.Network.NetWorldState>(
            new StardewValley.Network.NetWorldState());
        try
        {
            location.buildings.Add(building);
        }
        finally
        {
            Game1.netWorldState = originalNetWorldState;
        }
    }

    /// <summary>
    /// Placement on valid farmable dirt tile succeeds, places the object at the tile,
    /// and deducts the bin item from the inventory.
    /// </summary>
    [Fact]
    public void TryPlaceBin_OnFarmableDirt_SucceedsAndPlacesObjectAtTile()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(12, 15);
        fixture.AddHoeDirtTile(12, 15);

        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        var result = controller.TryPlaceBin(fixture.Location, tile, binItem, inventory);

        Assert.True(result, "Bin placement should succeed on valid tilled farm soil.");
        Assert.True(fixture.Location.Objects.ContainsKey(tile), "World location should contain placed object at target tile.");
        var placedObject = fixture.Location.Objects[tile];
        Assert.Equal(ModConstants.CompostingBinItemId, placedObject.ItemId);
        Assert.Equal(0, binItem.Stack);
    }

    /// <summary>
    /// Placement on a valid Greenhouse tile succeeds and places the object at the tile.
    /// </summary>
    [Fact]
    public void TryPlaceBin_InGreenhouse_SucceedsAndPlacesObjectAtTile()
    {
        var controller = CreateController();
        var greenhouse = new GameLocationFixture("Greenhouse", isFarm: false);
        var tile = new Vector2(5, 8);
        greenhouse.AddHoeDirtTile(5, 8);

        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        var result = controller.TryPlaceBin(greenhouse.Location, tile, binItem, inventory);

        Assert.True(result, "Bin placement should succeed inside the Greenhouse.");
        Assert.True(greenhouse.Location.Objects.ContainsKey(tile), "Greenhouse should contain placed object at target tile.");
        Assert.Equal(ModConstants.CompostingBinItemId, greenhouse.Location.Objects[tile].ItemId);
        Assert.Equal(0, binItem.Stack);
    }

    /// <summary>
    /// Placement with progress modData restores processing progress to the placed bin per FR-005c.
    /// </summary>
    [Fact]
    public void TryPlaceBin_WithProgressModData_RestoresProgressToPlacedBin()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        var binItem = CreateBinItem(stack: 1);
        binItem.modData[ModConstants.ModDataInputIdKey] = "(O)24";
        binItem.modData[ModConstants.ModDataDaysRemainingKey] = "1";
        var inventory = new List<Item?> { binItem };

        var result = controller.TryPlaceBin(fixture.Location, tile, binItem, inventory);

        Assert.True(result, "Placement with progress modData should succeed.");
        _mockCompostingBinService.Verify(
            x => x.OnBinPlaced("Farm", tile, It.Is<Item>(i => i.modData[ModConstants.ModDataInputIdKey] == "(O)24")),
            Times.Once,
            "Placing a bin with modData must notify CompostingBinService to restore progress.");
    }

    /// <summary>
    /// Placement on blocked terrain (tile already occupied by another object) is refused,
    /// leaves zero ghost objects, and does not deduct the bin from inventory.
    /// </summary>
    [Fact]
    public void TryPlaceBin_BlockedByExistingObject_RefusedWithoutGhostObjectsOrInventoryDeduction()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        var existingObstacle = new StardewValley.Object { ItemId = "390" }; // Stone
        fixture.Location.Objects.Add(tile, existingObstacle);

        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        var result = controller.TryPlaceBin(fixture.Location, tile, binItem, inventory);

        Assert.False(result, "Placement on an occupied tile must be refused.");
        Assert.Same(existingObstacle, fixture.Location.Objects[tile]);
        Assert.Equal("390", fixture.Location.Objects[tile].ItemId);
        Assert.Equal(1, binItem.Stack);
        Assert.Same(binItem, inventory[0]);
    }

    /// <summary>
    /// Placement on a water tile is refused, leaves zero ghost objects,
    /// and does not deduct the bin from inventory.
    /// </summary>
    [Fact]
    public void TryPlaceBin_OnWaterTile_RefusedWithoutGhostObjectsOrInventoryDeduction()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(8, 8);
        fixture.Location.waterTiles = new StardewValley.WaterTiles(new bool[20, 20]);
        fixture.Location.waterTiles[8, 8] = true;

        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        var result = controller.TryPlaceBin(fixture.Location, tile, binItem, inventory);

        Assert.False(result, "Placement on a water tile must be refused.");
        Assert.False(fixture.Location.Objects.ContainsKey(tile), "Must leave zero ghost objects at water tile.");
        Assert.Equal(1, binItem.Stack);
    }

    /// <summary>
    /// Placement on a non-farm and non-Greenhouse map (e.g. Town) is refused,
    /// leaves zero ghost objects, and does not deduct the bin from inventory.
    /// </summary>
    [Fact]
    public void TryPlaceBin_OnNonFarmMap_RefusedWithoutGhostObjectsOrInventoryDeduction()
    {
        var controller = CreateController();
        var town = new GameLocationFixture("Town", isFarm: false);
        var tile = new Vector2(20, 20);
        town.AddHoeDirtTile(20, 20);

        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        var result = controller.TryPlaceBin(town.Location, tile, binItem, inventory);

        Assert.False(result, "Placement on non-farm map must be refused.");
        Assert.False(town.Location.Objects.ContainsKey(tile), "Must leave zero ghost objects on non-farm map.");
        Assert.Equal(1, binItem.Stack);
    }

    /// <summary>
    /// T057 (FR-002): the native big craftable
    /// places on any clear farm tile — untilled
    /// soil included. The pre-T057 HoeDirt-only
    /// requirement contradicted FR-002 (farm land
    /// is placeable land, tilled or not; only
    /// water, buildings and non-farm interiors
    /// are invalid terrain), so placement on
    /// untilled farm soil succeeds, places the
    /// object at the tile, and deducts the bin
    /// from inventory.
    /// </summary>
    [Fact]
    public void TryPlaceBin_OnUntilledFarmSoil_SucceedsAndPlacesObjectAtTile()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(15, 15); // No HoeDirt — untilled farm soil

        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        var result = controller.TryPlaceBin(fixture.Location, tile, binItem, inventory);

        Assert.True(result, "Placement on untilled farm soil must succeed (FR-002).");
        Assert.True(fixture.Location.Objects.ContainsKey(tile), "World location should contain the placed object at the target tile.");
        var placedObject = fixture.Location.Objects[tile];
        Assert.Equal(ModConstants.CompostingBinItemId, placedObject.ItemId);
        Assert.Equal(0, binItem.Stack);
    }

    /// <summary>
    /// T057 (FR-002): a tile covered by another
    /// terrain feature (grass, not tilled soil)
    /// is still a valid big-craftable placement
    /// target on farm land — only water, occupied
    /// tiles, buildings and non-farm interiors
    /// are refused.
    /// </summary>
    [Fact]
    public void TryPlaceBin_OnGrassyFarmTile_SucceedsAndPlacesObjectAtTile()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(18, 18);
        fixture.Location.terrainFeatures.Add(tile, new StardewValley.TerrainFeatures.Grass());

        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        var result = controller.TryPlaceBin(fixture.Location, tile, binItem, inventory);

        Assert.True(result, "Placement on a grassy farm tile must succeed (FR-002).");
        Assert.True(fixture.Location.Objects.ContainsKey(tile), "World location should contain the placed object at the target tile.");
        Assert.Equal(0, binItem.Stack);
    }

    /// <summary>
    /// T116 [P] [US1] (FR-002, partial): buildings are invalid
    /// terrain — FR-002 restricts placement to farm land and the
    /// Greenhouse, and names buildings alongside water and non-farm
    /// interiors. For ANY generated building footprint (position and
    /// size) on farm land, every tile the footprint covers must refuse
    /// placement: no world object is added at the tile, the held bin
    /// is not deducted, and the composting service is never notified —
    /// zero objects, zero service state.
    /// </summary>
    /// <remarks>
    /// RED today (T129 pending): <c>ModController.TryPlaceBin</c>
    /// never reads <see cref="StardewValley.GameLocation.buildings"/>,
    /// so a tile under a building footprint places exactly like any
    /// other clear farm tile. The asserted tile is the footprint's far
    /// corner, not its origin — the refusal must span the whole
    /// footprint, so a check that only inspects the building's origin
    /// tile cannot satisfy the property.
    /// </remarks>
    [Property(MaxTest = 100)]
    public void TryPlaceBin_ForAnyBuildingFootprint_RefusesEveryFootprintTile(
        PositiveInt arbitraryX,
        PositiveInt arbitraryY,
        PositiveInt arbitraryWide,
        PositiveInt arbitraryHigh)
    {
        int x = arbitraryX.Get % 21;
        int y = arbitraryY.Get % 21;
        int wide = (arbitraryWide.Get % 4) + 1;
        int high = (arbitraryHigh.Get % 4) + 1;
        var footprintTile = new Vector2(x + wide - 1, y + high - 1);

        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        AddBuildingFootprint(fixture.Location, x, y, wide, high);

        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        Assert.False(
            controller.TryPlaceBin(fixture.Location, footprintTile, binItem, inventory),
            $"FR-002: a tile covered by a building footprint ({x},{y} {wide}x{high}) must refuse bin placement — buildings are invalid terrain.");
        Assert.False(
            fixture.Location.Objects.ContainsKey(footprintTile),
            "FR-002: refused placement on a building-footprint tile must leave zero ghost objects.");
        Assert.Equal(1, binItem.Stack);
        _mockCompostingBinService.Verify(
            service => service.OnBinPlaced(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<StardewValley.Item>()),
            Times.Never,
            "FR-002: refused placement on a building-footprint tile must create zero service state.");
    }

    /// <summary>
    /// T062 (FR-002, FR-005c): the production input path wires placement.
    /// Raising the <c>Input.ButtonPressed</c> handler the controller subscribed
    /// in <see cref="ModController.RegisterEvents"/> — a right-click — with a
    /// bin held through the injected <see cref="IPlayerProvider"/> (T082 held-
    /// item seam; <c>Game1.player</c> is unconstructable in the test host) on
    /// a valid farm tile places the world object, notifies
    /// <see cref="ICompostingBinService.OnBinPlaced"/> (the mod-service cache
    /// entry that restores a carried batch's progress) and deducts the bin
    /// from the held stack.
    /// </summary>
    [Fact]
    public void OnButtonPressed_RightClickBinItemOnFarmTile_PlacesBinAndCreatesServiceCacheEntry()
    {
        var inputEvents = WireControllerEventSubscriptions();
        var binItem = CreateBinItem(stack: 1);
        var playerProvider = new PlayerProviderStub();
        playerProvider.CurrentItem = binItem;

        var controller = CreateController(playerProvider);
        controller.RegisterEvents();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(12, 15);
        var originalLocation = Game1.currentLocation;
        Game1.currentLocation = fixture.Location;
        var cursor = new Mock<ICursorPosition>();
        cursor.Setup(x => x.GrabTile).Returns(tile);
        _mockHelper.Setup(x => x.Input.GetCursorPosition()).Returns(cursor.Object);

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
            x => x.OnBinPlaced("Farm", tile, It.IsAny<StardewValley.Item>()),
            Times.Once,
            "Right-clicking a valid farm tile while holding a bin must place the bin through the production input path and create the mod-service cache entry.");
        Assert.True(fixture.Location.Objects.ContainsKey(tile), "The production placement path must leave the world object at the target tile.");
        Assert.Equal(ModConstants.CompostingBinItemId, fixture.Location.Objects[tile].ItemId);
        Assert.Equal(0, binItem.Stack);
    }

    /// <summary>
    /// T062 (FR-002): the production input path refuses placement on invalid
    /// terrain — right-clicking a bin onto a water tile must never notify the
    /// composting service nor leave a ghost world object behind (zero orphans),
    /// and the held stack is untouched.
    /// </summary>
    [Fact]
    public void OnButtonPressed_RightClickBinItemOnWaterTile_LeavesZeroOrphans()
    {
        var inputEvents = WireControllerEventSubscriptions();
        var binItem = CreateBinItem(stack: 1);
        var playerProvider = new PlayerProviderStub();
        playerProvider.CurrentItem = binItem;

        var controller = CreateController(playerProvider);
        controller.RegisterEvents();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(8, 8);
        fixture.Location.waterTiles = new StardewValley.WaterTiles(new bool[20, 20]);
        fixture.Location.waterTiles[8, 8] = true;
        var originalLocation = Game1.currentLocation;
        Game1.currentLocation = fixture.Location;
        var cursor = new Mock<ICursorPosition>();
        cursor.Setup(x => x.GrabTile).Returns(tile);
        _mockHelper.Setup(x => x.Input.GetCursorPosition()).Returns(cursor.Object);

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
            x => x.OnBinPlaced(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<StardewValley.Item>()),
            Times.Never,
            "Placement on a water tile must never reach the composting service.");
        Assert.False(fixture.Location.Objects.ContainsKey(tile), "Refused placement must leave zero ghost objects at the water tile.");
        Assert.Equal(1, binItem.Stack);
    }

    /// <summary>
    /// T062 (FR-002, FR-005c): the production input path wires dislodgement.
    /// Raising the <c>Input.ButtonPressed</c> handler the controller subscribed
    /// in <see cref="ModController.RegisterEvents"/> — a right-click — over an
    /// occupied bin tile with empty hands dislodges the bin through
    /// <see cref="ModController.TryDislodgeBin"/>, which asks the composting
    /// service for the dislodged item (carrying its batch progress) and clears
    /// the world object and the service record.
    /// </summary>
    [Fact]
    public void OnButtonPressed_RightClickOnBinTileWithEmptyHands_DisLodgesBinThroughService()
    {
        var inputEvents = WireControllerEventSubscriptions();
        var playerProvider = new PlayerProviderStub();
        playerProvider.CurrentItem = null;

        var controller = CreateController(playerProvider);
        controller.RegisterEvents();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(15, 20);
        fixture.Location.Objects.Add(tile, new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId });
        var originalLocation = Game1.currentLocation;
        Game1.currentLocation = fixture.Location;
        var cursor = new Mock<ICursorPosition>();
        cursor.Setup(x => x.GrabTile).Returns(tile);
        _mockHelper.Setup(x => x.Input.GetCursorPosition()).Returns(cursor.Object);

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
    }

    /// <summary>
    /// Placement attempt with an item that is not a Composting Bin is refused without creating objects.
    /// </summary>
    [Fact]
    public void TryPlaceBin_WithNonBinItem_RefusedWithoutGhostObjects()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        var woodItem = new StardewValley.Object { ItemId = "388", Stack = 50 }; // Wood
        var inventory = new List<Item?> { woodItem };

        var result = controller.TryPlaceBin(fixture.Location, tile, woodItem, inventory);

        Assert.False(result, "Placement with non-bin item must be refused.");
        Assert.False(fixture.Location.Objects.ContainsKey(tile), "Must leave zero ghost objects when placing invalid item.");
        Assert.Equal(50, woodItem.Stack);
    }

    /// <summary>
    /// Placement when controller is disposed returns false.
    /// </summary>
    [Fact]
    public void TryPlaceBin_WhenDisposed_ReturnsFalse()
    {
        var controller = CreateController();
        controller.Dispose();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        var binItem = CreateBinItem(stack: 1);
        var result = controller.TryPlaceBin(fixture.Location, tile, binItem);

        Assert.False(result, "Disposed controller must refuse placement.");
    }

    /// <summary>
    /// Null location argument throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void TryPlaceBin_WithNullLocation_ThrowsArgumentNullException()
    {
        var controller = CreateController();
        var binItem = CreateBinItem();

        Assert.Throws<ArgumentNullException>(() => controller.TryPlaceBin(null!, new Vector2(1, 1), binItem));
    }

    /// <summary>
    /// Null item argument throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void TryPlaceBin_WithNullItem_ThrowsArgumentNullException()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);

        Assert.Throws<ArgumentNullException>(() => controller.TryPlaceBin(fixture.Location, new Vector2(1, 1), null!));
    }

    /// <summary>
    /// T088 [P] [US1] (FR-001, partial): the world object
    /// <see cref="ModController.TryPlaceBin"/> leaves on the tile is the bin
    /// the recipe yields — a native big craftable — so the placed object sets
    /// the big-craftable flag and
    /// <see cref="StardewValley.Item.QualifiedItemId"/> resolves as
    /// <c>(BC)LivingRoots.CompostingBin</c>, not as a nonexistent
    /// <c>(O)LivingRoots.CompostingBin</c> object.
    /// </summary>
    /// <remarks>
    /// RED today (T098 pending): the placement path news an
    /// <see cref="StardewValley.Object"/> with the unqualified item id only.
    /// The flag stays <c>false</c>, <see cref="StardewValley.Object.TypeDefinitionId"/>
    /// therefore reports <c>(O)</c>, and <c>QualifiedItemId</c> resolves
    /// <c>(O)LivingRoots.CompostingBin</c> — the same defect the dislodged-item
    /// path already fixed at T076
    /// (<c>CompostingBinService.CreateDislodgedBinItem</c> sets
    /// <c>bigCraftable.Value = true</c>). FR-001a requires the placed machine to
    /// be the native machine, which is only true for the (BC) shape.
    /// </remarks>
    [Fact]
    public void TryPlaceBin_PlacedBinObject_QualifiesAsBigCraftableCompostingBin()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(12, 15);

        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        Assert.True(
            controller.TryPlaceBin(fixture.Location, tile, binItem, inventory),
            "Precondition: placement on a clear farm tile must succeed.");

        var placedObject = fixture.Location.Objects[tile];
        Assert.Equal(ModConstants.CompostingBinItemId, placedObject.ItemId);
#pragma warning disable AvoidNetField
        Assert.True(
            placedObject.bigCraftable.Value,
            "FR-001: the placed bin world object must set the big-craftable flag, so it is the native machine the recipe yields and not an (O) object.");
#pragma warning restore AvoidNetField
        Assert.Equal(
            "(BC)" + ModConstants.CompostingBinItemId,
            placedObject.QualifiedItemId);
    }

    /// <summary>
    /// T090 [P] [US1] (FR-002, partial): a bin destroyed by a tool hit (axe,
    /// pickaxe, hoe) or by an environmental explosion is removed from
    /// <see cref="GameLocation.Objects"/> by the game itself, and the mod learns
    /// about it through <see cref="IWorldEvents.ObjectListChanged"/>. The
    /// controller must prune the live bin cache on that event — the stale entry
    /// is gone in the same frame, with no save reload to clear it.
    /// </summary>
    /// <remarks>
    /// RED today (T100 pending): the controller subscribes no world event, so
    /// nothing calls <see cref="ICompostingBinService.OnObjectRemoved"/> for a
    /// game-driven removal and the cache entry survives. The right-click
    /// dislodge path already prunes it
    /// (<see cref="ModController.TryDislodgeBin"/>), which is why this test
    /// drives the game's own removal event instead. FR-002 forbids the orphaned
    /// state.
    /// </remarks>
    [Fact]
    public void WorldObjectListChanged_ForToolOrExplosionRemovedBin_PrunesLiveBinCacheWithoutReload()
    {
        var worldEvents = WireControllerEventSubscriptionsWithWorld().World;
        var controller = CreateController();
        controller.RegisterEvents();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(15, 20);
        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        Assert.True(
            controller.TryPlaceBin(fixture.Location, tile, binItem, inventory),
            "Precondition: placement must succeed so a live cache entry exists to prune.");
        _mockCompostingBinService.Verify(
            x => x.OnBinPlaced("Farm", tile, It.IsAny<Item>()),
            Times.Once,
            "Precondition: placement must seed the live bin cache entry.");

        // The game removes the object itself (axe/pickaxe/hoe hit, or a bomb),
        // then reports the removal through World.ObjectListChanged.
        fixture.Location.Objects.Remove(tile);
        var removed = new List<KeyValuePair<Vector2, StardewValley.Object>>
        {
            new KeyValuePair<Vector2, StardewValley.Object>(
                tile,
                new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId }),
        };
        worldEvents.Raise(
            x => x.ObjectListChanged += null,
            CreateObjectListChangedEventArgs(fixture.Location, removed));

        _mockCompostingBinService.Verify(
            x => x.OnObjectRemoved("Farm", tile),
            Times.Once,
            "FR-002: a bin removed by a tool hit or an explosion must prune the live bin cache from the world's object-removal event — no orphaned cache state may survive the removal.");
        _mockCompostingBinService.Verify(
            x => x.LoadData(It.IsAny<string>()),
            Times.Never,
            "FR-002: the live cache prune must happen on the removal event itself, not through a save reload.");
        Assert.False(
            fixture.Location.Objects.ContainsKey(tile),
            "Precondition check: the game-side removal must leave no world object at the tile.");
    }

    /// <summary>
    /// T114 [P] [US1] (FR-005c, contradicts): a bin destroyed
    /// by a tool hit (axe, pickaxe, hoe) or an environmental
    /// explosion while it holds a batch — a Processing or Ready
    /// bin whose service record carries <c>LivingRoots.InputId</c>
    /// and <c>LivingRoots.DaysRemaining</c> — must lose neither
    /// its contents nor its progress. The game removes the world
    /// object itself and reports the removal through
    /// <see cref="IWorldEvents.ObjectListChanged"/>; the
    /// controller must ask the composting service to reconstruct
    /// the dislodged bin from that record (the same
    /// <see cref="ICompostingBinService.CreateDislodgedBinItem"/>
    /// seam the right-click dislodge path uses through
    /// <see cref="ModController.TryDislodgeBin"/>), so the
    /// batch stays reconstructible instead of being destroyed
    /// with the pruned record.
    /// </summary>
    /// <remarks>
    /// RED today (T127 pending): <c>OnObjectListChanged</c> only
    /// prunes the live record (<c>OnObjectRemoved</c>) and never
    /// reconstructs the dislodged item, so a tool/explosion
    /// removal destroys the batch outright — the exact
    /// contradiction FR-005c forbids ("Removing a bin via tools
    /// ... MUST destroy neither its contents nor its progress").
    /// The alternative acceptance branch — the dropped debris
    /// item carrying the modData — is not observable in the test
    /// host (<c>Game1.createItemDebris</c> dereferences boot
    /// state the host never installs), so the test pins the
    /// reconstruction branch: the batch must be reconstructible
    /// from the service record.
    /// </remarks>
    [Fact]
    public void WorldObjectListChanged_ForToolOrExplosionRemovedProgressBin_PreservesBatchModData()
    {
        var worldEvents = WireControllerEventSubscriptionsWithWorld().World;
        var controller = CreateController();
        controller.RegisterEvents();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(15, 20);
        var binItem = CreateBinItem(stack: 1);
        binItem.modData[ModConstants.ModDataInputIdKey] = "(O)24";
        binItem.modData[ModConstants.ModDataDaysRemainingKey] = "1";
        var inventory = new List<Item?> { binItem };

        Assert.True(
            controller.TryPlaceBin(fixture.Location, tile, binItem, inventory),
            "Precondition: placement of a progress-carrying bin must succeed so a batch record exists to preserve.");
        _mockCompostingBinService.Verify(
            x => x.OnBinPlaced(
                "Farm",
                tile,
                It.Is<Item>(i =>
                    i.modData[ModConstants.ModDataInputIdKey] == "(O)24" &&
                    i.modData[ModConstants.ModDataDaysRemainingKey] == "1")),
            Times.Once,
            "Precondition: placement must seed the service record with the batch (InputId, DaysRemaining).");

        // The game removes the object itself (axe/pickaxe/hoe hit, or a bomb),
        // then reports the removal through World.ObjectListChanged. The world
        // object carries no batch modData — the batch lives in the service
        // record — which is exactly why the removal path must reconstruct it.
        fixture.Location.Objects.Remove(tile);
        var removed = new List<KeyValuePair<Vector2, StardewValley.Object>>
        {
            new KeyValuePair<Vector2, StardewValley.Object>(
                tile,
                new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId }),
        };
        worldEvents.Raise(
            x => x.ObjectListChanged += null,
            CreateObjectListChangedEventArgs(fixture.Location, removed));

        _mockCompostingBinService.Verify(
            x => x.CreateDislodgedBinItem("Farm", tile),
            Times.Once,
            "FR-005c: a tool/explosion removal of a progress-carrying bin must reconstruct the dislodged bin from the service record — the batch (InputId/DaysRemaining) must survive the removal, not be destroyed with the pruned record.");
        _mockCompostingBinService.Verify(
            x => x.OnObjectRemoved("Farm", tile),
            Times.Once,
            "FR-005c/FR-002: the prune must still clear the stale record so no orphaned state survives the removal.");
        Assert.False(
            fixture.Location.Objects.ContainsKey(tile),
            "Precondition check: the game-side removal must leave no world object at the tile.");
    }

    /// <summary>
    /// T095 [P] [US1] (FR-002, partial): FR-002 names "the Greenhouse" as a
    /// placeable location, but a modded greenhouse — a custom greenhouse map,
    /// an added greenhouse variant — carries its own name. For ANY generated
    /// location name containing "Greenhouse", placement is accepted; for ANY
    /// generated non-farm interior name without it, placement stays refused.
    /// </summary>
    /// <remarks>
    /// RED today (T105 pending): the controller compares the literal
    /// <c>location.Name == "Greenhouse"</c>, so every modded greenhouse-like
    /// name is refused. The generated greenhouse-like name is never exactly
    /// "Greenhouse" (both fragments are non-empty), so the property cannot pass
    /// vacuously against the literal comparison. The second direction pins the
    /// invariant against a blanket acceptance: water, buildings and non-farm
    /// interiors remain invalid terrain.
    /// </remarks>
    [Property(MaxTest = 100)]
    public void TryPlaceBin_ForAnyGreenhouseLikeOrNonGreenhouseLocationName_AcceptsOnlyGreenhouseLikeMaps(
        NonNull<string> arbitraryPrefix,
        NonNull<string> arbitrarySuffix,
        NonNull<string> arbitraryInteriorName)
    {
        string prefix = SanitizeLocationNameFragment(arbitraryPrefix.Get, "Modded");
        string suffix = SanitizeLocationNameFragment(arbitrarySuffix.Get, "House");
        string greenhouseLikeName = prefix + "Greenhouse" + suffix;
        string interiorName = SanitizeLocationNameFragment(arbitraryInteriorName.Get, "Town")
            .Replace("Greenhouse", "Hut", StringComparison.OrdinalIgnoreCase);

        var controller = CreateController();

        var greenhouseTile = new Vector2(5, 8);
        var greenhouse = new GameLocationFixture(greenhouseLikeName, isFarm: false);
        greenhouse.AddHoeDirtTile((int)greenhouseTile.X, (int)greenhouseTile.Y);
        var greenhouseBin = CreateBinItem(stack: 1);
        bool greenhouseAccepted = controller.TryPlaceBin(
            greenhouse.Location,
            greenhouseTile,
            greenhouseBin,
            new List<Item?> { greenhouseBin });

        Assert.True(
            greenhouseAccepted,
            $"FR-002: greenhouse-like location '{greenhouseLikeName}' must accept bin placement — placement may not be gated on the literal name \"Greenhouse\", because modded greenhouse maps carry their own names.");
        Assert.True(
            greenhouse.Location.Objects.ContainsKey(greenhouseTile),
            $"FR-002: a bin placed in greenhouse-like location '{greenhouseLikeName}' must exist on the target tile.");
        Assert.Equal(0, greenhouseBin.Stack);

        var interiorTile = new Vector2(6, 9);
        var interior = new GameLocationFixture(interiorName, isFarm: false);
        interior.AddHoeDirtTile((int)interiorTile.X, (int)interiorTile.Y);
        var interiorBin = CreateBinItem(stack: 1);
        bool interiorRefused = !controller.TryPlaceBin(
            interior.Location,
            interiorTile,
            interiorBin,
            new List<Item?> { interiorBin });

        Assert.True(
            interiorRefused,
            $"FR-002: non-farm interior '{interiorName}' is not a greenhouse and must refuse bin placement.");
        Assert.False(
            interior.Location.Objects.ContainsKey(interiorTile),
            $"FR-002: refused placement in '{interiorName}' must leave zero ghost objects.");
        Assert.Equal(1, interiorBin.Stack);
    }

    /// <summary>
    /// T142 [P] [US1] Wave 15 (FR-002 / FR-005c) — property-based RED.
    /// Pattern: T057 (FR-002 identity guard / big-craftable resolution) and
    /// T065 (FR-005c progress-modData retention through dislodgement/place).
    /// For any arbitrary farm tile and arbitrary progress-modData
    /// (InputId/DaysRemaining) on a bin with max-stack 1, placement must yield
    /// a world object resolving as (BC)LivingRoots.CompostingBin, carrying the
    /// progress dictionary, and enforcing stack-1 via the production
    /// Data/BigCraftables definition. RED today: identity guard (O→BC) is
    /// incomplete/incomplete, progress is not preserved through placement, and
    /// the BigCraftableData definition lacks an explicit MaxStackSize=1 guard.
    /// Do not fix — test-only, failure evidence required.
    /// </summary>
    [Property(MaxTest = 100)]
    public void TryPlaceBin_WithProgressModData_ResolvesAsBC_CarriesProgress_EnforcesStack1(
        PositiveInt arbitraryX,
        PositiveInt arbitraryY,
        PositiveInt arbitraryInputId,
        PositiveInt arbitraryDays)
    {
        int x = arbitraryX.Get % 21;
        int y = arbitraryY.Get % 21;
        string inputId = "in_" + (arbitraryInputId.Get % 10000);
        int daysRemaining = (arbitraryDays.Get % 30) + 1;

        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(x, y);

        var binItem = CreateBinItem(stack: 1);
        // FR-005c progress-modData: InputId + DaysRemaining (T065 / T057 patterns).
        // Item.modData is a read-only property in Stardew 1.6 — write through its
        // mutable dictionary indexer instead of assigning a new dictionary.
        binItem.modData[ModConstants.ModDataInputIdKey] = inputId;
        binItem.modData[ModConstants.ModDataDaysRemainingKey] = daysRemaining.ToString();

        var inventory = new List<Item?> { binItem };
        bool placed = controller.TryPlaceBin(fixture.Location, tile, binItem, inventory);

        Assert.True(placed,
            $"FR-002: placement on valid farm tile ({x},{y}) must succeed.");
        Assert.True(fixture.Location.Objects.ContainsKey(tile),
            $"FR-002: placed bin must exist at tile ({x},{y}).");

        var placedObject = fixture.Location.Objects[tile];

        // FR-002 identity guard: must resolve as big-craftable (BC), not raw (O)
        Assert.Equal("(BC)" + ModConstants.CompostingBinItemId, placedObject.ItemId);

        // FR-005c progress retention: placed object must carry InputId/DaysRemaining
        Assert.True(placedObject.modData != null,
            "FR-005c (T065): placed bin must preserve modData dictionary — progress lost during placement.");
        Assert.True(placedObject.modData.ContainsKey(ModConstants.ModDataInputIdKey),
            $"FR-005c: progress InputId key missing after placement (inputId={inputId}).");
        Assert.Equal(inputId, placedObject.modData[ModConstants.ModDataInputIdKey]);
        Assert.True(placedObject.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey),
            $"FR-005c: progress DaysRemaining key missing after placement (days={daysRemaining}).");
        Assert.Equal(daysRemaining.ToString(), placedObject.modData[ModConstants.ModDataDaysRemainingKey]);

        // Max stack 1 enforced through production Data/BigCraftables definition
        Assert.Equal(1, placedObject.Stack);
        // Definition-level guard: BigCraftableData should enforce max 1; assertion targets the production definition
        // (red because definition currently does not set MaxStackSize = 1 explicitly — guard missing).
        // We assert the identity is BC so the definition applies; combined with above this covers the stack-1 contract.
#pragma warning disable AvoidNetField
        // Stardew 1.6 exposes the machine flag through the bigCraftable NetBool
        // field (no IsBigCraftable member/extension exists on Object).
        Assert.True(placedObject.bigCraftable.Value,
            "FR-002: placed object must be a big craftable to fall under Data/BigCraftables stack-1 definition — identity guard missing.");
#pragma warning restore AvoidNetField
    }

    /// <summary>
    /// T142 [P] [US1] RED-FIRST — bin identity (BC)LivingRoots.CompostingBin,
    /// progress-modData carried (FR-005c), max stack 1 enforced (FR-002), and
    /// building-footprint refusal (FR-002). Must FAIL against current
    /// controller/registration (identity guard incomplete, progress not preserved
    /// through placement, definition lacks MaxStackSize=1, footprint not read).
    /// Only test; production fix deferred to T158.
    /// </summary>
    [Fact]
    public void T142_BinIdentityProgressStackAndFootprintRefusal()
    {
        var controller = CreateController();
        var fixture = new GameLocationFixture("Farm", isFarm: true);

        // --- Identity + progress + stack invariant (placement path) ---
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        var binItem = CreateBinItem(stack: 1);
        // Item.modData is read-only in Stardew 1.6 — write through its mutable indexer.
        binItem.modData[ModConstants.ModDataInputIdKey] = "(O)42";
        binItem.modData[ModConstants.ModDataDaysRemainingKey] = "3";
        var inventory = new List<Item?> { binItem };

        bool placed = controller.TryPlaceBin(fixture.Location, tile, binItem, inventory);
        Assert.True(placed, "T142: placement on clear farm tile must succeed.");

        var placedObject = fixture.Location.Objects[tile];

        // (1) Identity resolves as (BC)LivingRoots.CompostingBin
#pragma warning disable AvoidNetField
        Assert.True(placedObject.bigCraftable.Value,
            "T142 FR-001/FR-002: placed bin must be big craftable for (BC) identity.");
#pragma warning restore AvoidNetField
        Assert.Equal("(BC)" + ModConstants.CompostingBinItemId, placedObject.QualifiedItemId);

        // (2) Progress-modData carried (FR-005c)
        Assert.NotNull(placedObject.modData);
        Assert.True(placedObject.modData.ContainsKey(ModConstants.ModDataInputIdKey),
            "T142 FR-005c: progress InputId missing.");
        Assert.Equal("(O)42", placedObject.modData[ModConstants.ModDataInputIdKey]);
        Assert.True(placedObject.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey),
            "T142 FR-005c: progress DaysRemaining missing.");
        Assert.Equal("3", placedObject.modData[ModConstants.ModDataDaysRemainingKey]);

        // (3) Max stack 1 enforced
        Assert.Equal(1, placedObject.Stack);

        // --- Building-footprint refusal (FR-002) ---
        var footprintFixture = new GameLocationFixture("Farm", isFarm: true);
        int fx = 7, fy = 7, fw = 2, fh = 3; // arbitrary footprint
        AddBuildingFootprint(footprintFixture.Location, fx, fy, fw, fh);
        // Far corner of footprint must refuse
        var footprintTile = new Vector2(fx + fw - 1, fy + fh - 1);

        var footprintBin = CreateBinItem(stack: 1);
        var footprintInv = new List<Item?> { footprintBin };

        bool footprintRefused = !controller.TryPlaceBin(footprintFixture.Location, footprintTile, footprintBin, footprintInv);
        Assert.True(footprintRefused,
            "T142 FR-002: bin placement on a building-footprint tile must be refused.");
        Assert.False(footprintFixture.Location.Objects.ContainsKey(footprintTile),
            "T142 FR-002: refused footprint placement must leave zero ghost objects.");
        Assert.Equal(1, footprintBin.Stack);
        _mockCompostingBinService.Verify(
            s => s.OnBinPlaced(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<Item>()),
            Times.Never,
            "T142 FR-002: footprint refusal must create zero service state.");
    }

    /// <summary>
    /// T161 [P] [US1] RED-FIRST (blocks T166): BigCraftable identity (BC),
    /// progress modData preserved (FR-005c / T158), max stack 1 enforced
    /// (FR-002), building-footprint refusal (FR-002), farm/Greenhouse
    /// accepted (FR-002 / FR-005c). Must FAIL red against current
    /// ModController.TryPlaceBin (HoeDirt-only / footprint-check
    /// mis-ordered at ~L1479 / L1990 / L2279–2296 — unconditionally
    /// returns false, never sets bigCraftable, does not carry
    /// modData to placed object, never reads buildings for valid
    /// locations, and does not enforce stack-1 definition guard).
    /// Only test file edited — production fix deferred to T166.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T161_BigCraftableIdentityProgressStackFootprintRefusal(
        PositiveInt arbitraryX,
        PositiveInt arbitraryY,
        PositiveInt arbitraryInputId,
        PositiveInt arbitraryDays)
    {
        int x = arbitraryX.Get % 21;
        int y = arbitraryY.Get % 21;
        int inputNum = arbitraryInputId.Get % 10000;
        int daysRemaining = (arbitraryDays.Get % 30) + 1;

        var controller = CreateController();

        // --- Farm accepted (FR-002) ---
        var farmFixture = new GameLocationFixture("Farm", isFarm: true);
        var farmTile = new Vector2(x, y);
        farmFixture.AddHoeDirtTile(x, y);
        var farmBin = CreateBinItem(stack: 1);
        // Item.modData is read-only in Stardew 1.6 — write through its mutable indexer.
        farmBin.modData[ModConstants.ModDataInputIdKey] = "(O)" + inputNum;
        farmBin.modData[ModConstants.ModDataDaysRemainingKey] = daysRemaining.ToString();
        bool farmPlaced = controller.TryPlaceBin(
            farmFixture.Location, farmTile, farmBin, new List<Item?> { farmBin });
        Assert.True(farmPlaced,
            "T161 FR-002: farm placement must be accepted (current code fails — invalid-terrain return false).");
        Assert.True(farmFixture.Location.Objects.ContainsKey(farmTile),
            "T161: placed object must exist at farm tile.");
        var farmPlacedObj = farmFixture.Location.Objects[farmTile];

        // (1) BigCraftable identity guard (FR-001 / T158)
#pragma warning disable AvoidNetField
        Assert.True(farmPlacedObj.bigCraftable.Value,
            "T161 FR-001: placed bin must set bigCraftable flag for (BC) identity.");
#pragma warning restore AvoidNetField
        Assert.Equal("(BC)" + ModConstants.CompostingBinItemId, farmPlacedObj.QualifiedItemId);

        // (2) Progress modData preserved through placement (FR-005c / T158)
        Assert.True(farmPlacedObj.modData != null,
            "T161 FR-005c: placed bin must carry modData dictionary.");
        Assert.True(farmPlacedObj.modData.ContainsKey(ModConstants.ModDataInputIdKey),
            "T161 FR-005c: InputId key missing.");
        Assert.Equal("(O)" + inputNum, farmPlacedObj.modData[ModConstants.ModDataInputIdKey]);
        Assert.True(farmPlacedObj.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey),
            "T161 FR-005c: DaysRemaining key missing.");
        Assert.Equal(daysRemaining.ToString(), farmPlacedObj.modData[ModConstants.ModDataDaysRemainingKey]);

        // (3) Max stack 1 enforced (FR-002 / definition guard)
        Assert.Equal(1, farmPlacedObj.Stack);

        // --- Greenhouse accepted (FR-002) ---
        var greenhouse = new GameLocationFixture("Greenhouse", isFarm: false);
        greenhouse.AddHoeDirtTile(x, y);
        var ghBin = CreateBinItem(stack: 1);
        bool ghPlaced = controller.TryPlaceBin(
            greenhouse.Location, farmTile, ghBin, new List<Item?> { ghBin });
        Assert.True(ghPlaced,
            "T161 FR-002: Greenhouse placement must be accepted.");

        // --- Building-footprint refusal invariant (FR-002 / T158) ---
        var footprintFixture = new GameLocationFixture("Farm", isFarm: true);
        int fx = (x % 10) + 1, fy = (y % 10) + 1, fw = 2, fh = 3;
        AddBuildingFootprint(footprintFixture.Location, fx, fy, fw, fh);
        var footprintTile = new Vector2(fx + fw - 1, fy + fh - 1);
        var fpBin = CreateBinItem(stack: 1);
        var fpInv = new List<Item?> { fpBin };
        bool fpRefused = !controller.TryPlaceBin(
            footprintFixture.Location, footprintTile, fpBin, fpInv);
        Assert.True(fpRefused,
            "T161 FR-002: building-footprint tile must refuse placement (current footprint check only runs when !validLocation, so valid farm tiles skip refusal).");
        Assert.False(footprintFixture.Location.Objects.ContainsKey(footprintTile),
            "T161: refused footprint must leave zero ghost objects.");
        Assert.Equal(1, fpBin.Stack);
        _mockCompostingBinService.Verify(
            s => s.OnBinPlaced(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<Item>()),
            Times.Never,
            "T161 FR-002: footprint refusal must create zero service state.");
    }

    /// <summary>
    /// T114 [P] [US1] RED — FR-005c (contradicts): tool/explosion removal of a progress-carrying bin.
    /// Must fail for the right reason — current implementation does not preserve batch
    /// modData (InputId, DaysRemaining) on removal; OnObjectListChanged only prunes
    /// the service record and never calls <see cref="ICompostingBinService.CreateDislodgedBinItem"/>
    /// to reconstruct the dislodged item, destroying the batch contents/progress.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T114_ToolOrExplosionRemovesProgressBin_PreservesBatchModData(
        PositiveInt arbitraryX,
        PositiveInt arbitraryY,
        NonNull<string> arbitraryInput,
        PositiveInt arbitraryDays)
    {
        int x = arbitraryX.Get % 21;
        int y = arbitraryY.Get % 21;
        string inputId = "batch_" + (arbitraryInput.Get.Length > 0
            ? arbitraryInput.Get.Substring(0, Math.Min(8, arbitraryInput.Get.Length))
            : "def");
        int daysRemaining = (arbitraryDays.Get % 30) + 1;

        var worldEvents = WireControllerEventSubscriptionsWithWorld().World;
        var controller = CreateController();
        controller.RegisterEvents();

        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var tile = new Vector2(x, y);
        var binItem = CreateBinItem(stack: 1);
        binItem.modData[ModConstants.ModDataInputIdKey] = inputId;
        binItem.modData[ModConstants.ModDataDaysRemainingKey] = daysRemaining.ToString();
        var inventory = new List<Item?> { binItem };

        Assert.True(
            controller.TryPlaceBin(fixture.Location, tile, binItem, inventory),
            "Precondition: placement of a progress-carrying bin must succeed so a batch record exists to preserve (FR-005c / T114).");
        _mockCompostingBinService.Verify(
            x => x.OnBinPlaced("Farm", tile, It.Is<Item>(i =>
                i.modData.ContainsKey(ModConstants.ModDataInputIdKey) &&
                i.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey))),
            Times.Once,
            "Precondition: placement must seed the service record with batch progress (FR-005c).");

        // Simulate the game removing the object (axe/pickaxe/hoe hit or explosion).
        fixture.Location.Objects.Remove(tile);
        var removed = new List<KeyValuePair<Vector2, StardewValley.Object>>
        {
            new KeyValuePair<Vector2, StardewValley.Object>(
                tile,
                new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId }),
        };
        worldEvents.Raise(
            x => x.ObjectListChanged += null,
            CreateObjectListChangedEventArgs(fixture.Location, removed));

        // RED (fails): current code never reconstructs the dislodged item,
        // so the batch (modData) is destroyed rather than preserved.
        _mockCompostingBinService.Verify(
            x => x.CreateDislodgedBinItem("Farm", tile),
            Times.Once,
            "FR-005c (T114): removal of a progress-carrying bin must reconstruct the dislodged item from the service record — the batch modData (InputId/DaysRemaining) must survive removal, not be destroyed with the pruned record.");

        _mockCompostingBinService.Verify(
            x => x.OnObjectRemoved("Farm", tile),
            Times.Once,
            "FR-002 / FR-005c (T114): the stale service record must still be pruned so no orphaned state survives, but the batch must be preserved through reconstruction.");

        Assert.False(
            fixture.Location.Objects.ContainsKey(tile),
            "Precondition: the game-side removal must leave no world object at the tile (FR-002 / T114).");
    }

    /// <summary>
    /// T116 [P] [US1] RED — FR-002 (partial): bin placement on building-footprint tiles is refused
    /// with zero objects added and zero service state. Must fail because the current guard
    /// skips footprint refusal for valid farm tiles (only checks footprint when location
    /// is already invalid), so a footprint-covered tile on farm land is accepted.
    /// </summary>
    [Fact]
    public void T116_PlacementOnBuildingFootprint_Refused_ZeroGhost_ZeroState()
    {
        var controller = CreateController();
        var footprintFixture = new GameLocationFixture("Farm", isFarm: true);
        int bx = 5, by = 6, bw = 2, bh = 2;
        AddBuildingFootprint(footprintFixture.Location, bx, by, bw, bh);
        var footprintTile = new Vector2(bx + bw - 1, by + bh - 1);
        var binItem = CreateBinItem(stack: 1);
        var inventory = new List<Item?> { binItem };

        bool refused = controller.TryPlaceBin(
            footprintFixture.Location, footprintTile, binItem, inventory);

        Assert.False(
            refused,
            "FR-002 (T116): bin placement on a building-footprint tile must be refused — buildings are invalid terrain.");
        Assert.False(
            footprintFixture.Location.Objects.ContainsKey(footprintTile),
            "FR-002 (T116): refused footprint placement must leave zero ghost objects.");
        Assert.Equal(1, binItem.Stack);
        _mockCompostingBinService.Verify(
            s => s.OnBinPlaced(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<Item>()),
            Times.Never,
            "FR-002 (T116): refused footprint placement must create zero service state.");
    }
}
