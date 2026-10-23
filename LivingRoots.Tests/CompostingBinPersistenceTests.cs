using System;
using System.Collections.Generic;
using System.Linq;
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
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Unit tests for composting bin persistence verifying tolerant reader fault isolation,
/// obsolete field ignorance, orphaned record pruning, and exception sanitization (FR-002, FR-012, FR-015).
/// Validates Task T020 [P] [US2] under Feature 004 (Playable Compost Loop).
/// </summary>
public class CompostingBinPersistenceTests
{
    private readonly Mock<IModDataService> _mockModDataService;
    private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
    private readonly Mock<IOrganicWasteValidator> _mockWasteValidator;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<ILocationProvider> _mockLocationProvider;
    private readonly Mock<IModHelper> _mockHelper;
    private readonly Mock<IManifest> _mockManifest;
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<ISoilDecayService> _mockSoilDecayService;
    private readonly TimeProviderStub _timeStub;
    private readonly CompostingBinFactory _factory;
    private readonly RecordingPlayerInventory _inventory;
    private readonly CompostingBinService _service;

    public CompostingBinPersistenceTests()
    {
        GameStateFixture.Install();

        _mockModDataService = new Mock<IModDataService>();
        _mockSaveIdProvider = new Mock<ISaveIdProvider>();
        _mockWasteValidator = new Mock<IOrganicWasteValidator>();
        _mockMonitor = new Mock<IMonitor>();
        _mockLocationProvider = new Mock<ILocationProvider>();
        _mockHelper = new Mock<IModHelper>();
        _mockManifest = new Mock<IManifest>();
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockSoilDecayService = new Mock<ISoilDecayService>();
        _timeStub = new TimeProviderStub();
        _factory = new CompostingBinFactory();
        _inventory = new RecordingPlayerInventory();

        _service = new CompostingBinService(
            _mockModDataService.Object,
            _mockSaveIdProvider.Object,
            _mockWasteValidator.Object,
            _mockMonitor.Object,
            _timeStub,
            _factory,
            _inventory,
            _mockLocationProvider.Object);
    }

    /// <summary>A controller wired to the real bin service, for the
    /// held/debris columns of the round-trip matrix (T056).</summary>
    private ModController CreateController()
    {
        return new ModController(
            _mockHelper.Object,
            _mockMonitor.Object,
            _mockManifest.Object,
            _mockSoilHealthService.Object,
            _mockSaveIdProvider.Object,
            _service,
            _mockSoilDecayService.Object);
    }

    private static StardewValley.Object CreatePlacedBinObject(Vector2 tile, string itemId = ModConstants.CompostingBinItemId)
    {
        return new StardewValley.Object
        {
            ItemId = itemId,
            TileLocation = tile
        };
    }

    /// <summary>
    /// Verifies that the tolerant reader gracefully skips/quarantines corrupt JSON records
    /// without crashing the load of subsequent valid records (FR-012, FR-014).
    /// </summary>
    [Fact]
    public void LoadData_CorruptRecord_SkipsCorruptRecordAndLoadsSubsequentValidRecords()
    {
        // Arrange
        const string saveId = "test_save_corrupt";
        const string key = ModConstants.CompostingBinKeyPrefix + saveId;

        var binData = new CompostingBinData();
        var farmBins = new Dictionary<string, CompostingBinStateData>
        {
            // Corrupt entry with invalid enum state string
            ["10,10"] = new CompostingBinStateData
            {
                State = "CorruptedGarbageState",
                InputItemId = "invalid_item",
                InputTimestamp = 1
            },
            // Valid entry that should still load despite the preceding corrupt entry
            ["20,20"] = new CompostingBinStateData
            {
                State = "Processing",
                InputItemId = "(O)24",
                InputTimestamp = 5
            }
        };
        binData.LocationBinData["Farm"] = farmBins;

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(key)).Returns(binData);

        var farmFixture = new GameLocationFixture("Farm", isFarm: true);
        farmFixture.Location.Objects.Add(new Vector2(20, 20), CreatePlacedBinObject(new Vector2(20, 20)));
        _mockLocationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(farmFixture.Location);

        // Act
        _service.LoadData(saveId);

        // Assert
        // Corrupt entry at (10, 10) must be skipped/quarantined (defaults to Empty)
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));

        // Valid entry at (20, 20) must be loaded successfully
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(20, 20)));
    }

    /// <summary>
    /// Verifies that a record with an unparseable tile coordinate key is skipped
    /// without crashing or preventing subsequent valid records from loading (FR-012).
    /// </summary>
    [Fact]
    public void LoadData_UnparseableTileKey_SkipsUnparseableKeyAndLoadsValidRecords()
    {
        // Arrange
        const string saveId = "test_save_bad_key";
        const string key = ModConstants.CompostingBinKeyPrefix + saveId;

        var binData = new CompostingBinData();
        var farmBins = new Dictionary<string, CompostingBinStateData>
        {
            // Bad coordinate key
            ["not_a_valid_coord"] = new CompostingBinStateData
            {
                State = "Processing",
                InputItemId = "(O)24",
                InputTimestamp = 1
            },
            // Valid coordinate key
            ["15,15"] = new CompostingBinStateData
            {
                State = "Ready",
                InputItemId = "(O)24",
                InputTimestamp = 1
            }
        };
        binData.LocationBinData["Farm"] = farmBins;

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(key)).Returns(binData);

        var farmFixture = new GameLocationFixture("Farm", isFarm: true);
        farmFixture.Location.Objects.Add(new Vector2(15, 15), CreatePlacedBinObject(new Vector2(15, 15)));
        _mockLocationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(farmFixture.Location);

        // Act
        _service.LoadData(saveId);

        // Assert
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(15, 15)));
    }

    /// <summary>
    /// Verifies that a null location payload aborts only that location
    /// while the remaining locations still load (FR-012). The tolerant
    /// reader's fault isolation is per-location, not per-save: one
    /// corrupt location entry must not take the whole load down.
    /// </summary>
    [Fact]
    public void LoadData_NullLocationPayload_AbortsOnlyThatLocationAndLoadsRemaining()
    {
        // Arrange
        const string saveId = "test_save_null_location";
        const string key = ModConstants.CompostingBinKeyPrefix + saveId;

        var binData = new CompostingBinData();
        // The null payload is listed first: dictionary enumeration keeps
        // insertion order, so the corrupt location is encountered before
        // the valid one — the abort must not swallow it.
        binData.LocationBinData["NullLocation"] = null!;
        binData.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>
        {
            ["10,10"] = new CompostingBinStateData
            {
                State = "Processing",
                InputItemId = "(O)24",
                InputTimestamp = 1
            }
        };

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(key)).Returns(binData);

        var farmFixture = new GameLocationFixture("Farm", isFarm: true);
        farmFixture.Location.Objects.Add(new Vector2(10, 10), CreatePlacedBinObject(new Vector2(10, 10)));
        _mockLocationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(farmFixture.Location);

        // Act
        _service.LoadData(saveId);

        // Assert
        // Only the null-payload location is aborted; the remaining
        // location's record must still load (FR-012).
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    /// <summary>
    /// Verifies that obsolete schema fields (MaturationLevel, ConsecutiveActiveDays, ConsecutiveIdleDays)
    /// from prior mod versions are ignored without error, defaulting to single-item batch yield (FR-005a, FR-012).
    /// </summary>
    [Fact]
    public void LoadData_ObsoleteSchemaFields_IgnoredAndDefaultsToSingleBatchYield()
    {
        // Arrange
        const string saveId = "test_save_obsolete_fields";
        const string key = ModConstants.CompostingBinKeyPrefix + saveId;

        var binData = new CompostingBinData();
        var farmBins = new Dictionary<string, CompostingBinStateData>
        {
            ["12,12"] = new CompostingBinStateData
            {
                State = "Ready",
                InputItemId = "(O)24",
                InputTimestamp = 1,
                // Obsolete legacy fields from prior mod specifications
                MaturationLevel = 5,
                ConsecutiveActiveDays = 20,
                ConsecutiveIdleDays = 10
            }
        };
        binData.LocationBinData["Farm"] = farmBins;

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(key)).Returns(binData);

        var farmFixture = new GameLocationFixture("Farm", isFarm: true);
        farmFixture.Location.Objects.Add(new Vector2(12, 12), CreatePlacedBinObject(new Vector2(12, 12)));
        _mockLocationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(farmFixture.Location);

        // Act
        _service.LoadData(saveId);

        // Assert
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(12, 12)));

        // Harvesting must yield strictly 1 compost item, ignoring legacy MaturationLevel = 5 (FR-005a)
        int yieldedCount = _service.CollectCompost("Farm", new Vector2(12, 12));
        Assert.Equal(1, yieldedCount);
    }

    /// <summary>
    /// Verifies that disconnected save records where no physical object exists at that location/tile
    /// in the world (GameLocation.Objects) are pruned as orphaned state to prevent phantom machines (FR-002, FR-012).
    /// </summary>
    [Fact]
    public void LoadData_OrphanedBinRecord_PrunesDisconnectedRecordWhenNoWorldObjectExists()
    {
        // Arrange
        const string saveId = "test_save_orphaned";
        const string key = ModConstants.CompostingBinKeyPrefix + saveId;

        var binData = new CompostingBinData();
        var farmBins = new Dictionary<string, CompostingBinStateData>
        {
            // Bin at (10, 10) corresponds to a real placed machine
            ["10,10"] = new CompostingBinStateData
            {
                State = "Processing",
                InputItemId = "(O)24",
                InputTimestamp = 1
            },
            // Bin at (30, 30) is orphaned: player broke/removed it, but save record persisted
            ["30,30"] = new CompostingBinStateData
            {
                State = "Processing",
                InputItemId = "(O)24",
                InputTimestamp = 1
            }
        };
        binData.LocationBinData["Farm"] = farmBins;

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(key)).Returns(binData);

        var farmFixture = new GameLocationFixture("Farm", isFarm: true);
        // Only (10, 10) exists in the world; (30, 30) has no object
        farmFixture.Location.Objects.Add(new Vector2(10, 10), CreatePlacedBinObject(new Vector2(10, 10)));
        _mockLocationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(farmFixture.Location);

        // Act
        _service.LoadData(saveId);

        // Assert
        // The real placed bin at (10, 10) must be loaded
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));

        // The disconnected bin at (30, 30) must be pruned (not in runtime cache)
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(30, 30)));

        // Furthermore, saving data must not persist the pruned orphaned bin
        _service.SaveData(saveId);
        _mockModDataService.Verify(x => x.SaveData(
            It.Is<CompostingBinData>(data =>
                data.LocationBinData.ContainsKey("Farm") &&
                data.LocationBinData["Farm"].ContainsKey("10,10") &&
                !data.LocationBinData["Farm"].ContainsKey("30,30")),
            key), Times.Once);
    }

    /// <summary>
    /// Verifies that disconnected save records where a different object (not a composting bin) exists
    /// at the tile are also pruned as orphaned state (FR-002, FR-012).
    /// </summary>
    [Fact]
    public void LoadData_OrphanedBinRecord_PrunesDisconnectedRecordWhenDifferentObjectAtTile()
    {
        // Arrange
        const string saveId = "test_save_different_object";
        const string key = ModConstants.CompostingBinKeyPrefix + saveId;

        var binData = new CompostingBinData();
        var farmBins = new Dictionary<string, CompostingBinStateData>
        {
            ["18,18"] = new CompostingBinStateData
            {
                State = "Processing",
                InputItemId = "(O)24",
                InputTimestamp = 1
            }
        };
        binData.LocationBinData["Farm"] = farmBins;

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(key)).Returns(binData);

        var farmFixture = new GameLocationFixture("Farm", isFarm: true);
        // Place a Stone (ItemId 390) instead of a CompostingBin at (18, 18)
        farmFixture.Location.Objects.Add(new Vector2(18, 18), CreatePlacedBinObject(new Vector2(18, 18), "390"));
        _mockLocationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(farmFixture.Location);

        // Act
        _service.LoadData(saveId);

        // Assert
        // The bin state must be pruned because tile does not contain a CompostingBin
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(18, 18)));
    }

    /// <summary>
    /// Verifies that persistence exceptions are logged with sanitized type/hresult
    /// and never expose raw exception messages (FR-015).
    /// </summary>
    [Fact]
    public void LoadData_OnException_UsesSanitizedLoggingWithoutRawExceptionMessage()
    {
        // Arrange
        const string saveId = "test_save_exception";
        const string key = ModConstants.CompostingBinKeyPrefix + saveId;
        const string sensitiveRawMessage = "Fatal database disk read error at /secret/system/root/data.dat: 0xDEADBEEF";

        _mockModDataService
            .Setup(x => x.LoadData<CompostingBinData>(key))
            .Throws(new InvalidOperationException(sensitiveRawMessage));

        // Act
        _service.LoadData(saveId);

        // Assert
        // Confirm no unhandled crash occurred and monitor was called
        _mockMonitor.Verify(
            x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()),
            Times.AtLeastOnce,
            "Must log diagnostic failure info.");

        // Strict security assertion: raw exception message MUST NOT appear anywhere in the log output (FR-015)
        foreach (var invocation in _mockMonitor.Invocations.Where(inv => inv.Method.Name == nameof(IMonitor.Log)))
        {
            var loggedMessage = invocation.Arguments[0]?.ToString() ?? string.Empty;
            Assert.DoesNotContain(sensitiveRawMessage, loggedMessage);
            Assert.DoesNotContain("0xDEADBEEF", loggedMessage);
            Assert.DoesNotContain("/secret/system", loggedMessage);
        }
    }

    // ------------------------------------------------------------------
    // T056 (SC-004): the round-trip matrix — every bin
    // state (Empty, Processing, Ready) survives a
    // save/load cycle in every bin situation: placed
    // in the world (state persisted beside the world
    // object), held in the player inventory (progress
    // serialized into Item.modData), and dropped as
    // floating debris (the same item object the
    // dislodgement hands to the debris callback).
    // Neither lost nor invented values (FR-012).
    // ------------------------------------------------------------------

    /// <summary>The matrix location and tile every column shares.</summary>
    private const string MatrixLocationName = "Farm";
    private static readonly Vector2 MatrixTile = new(10, 10);

    /// <summary>
    /// Brings the real service to the given state at the
    /// matrix tile: Empty is the fresh default, Processing
    /// is a batch loaded on day 1, Ready is that batch
    /// after two full days and the day-start tick (invoked
    /// twice — the per-location guard makes the repeat a
    /// no-op, proving the exactly-once rule holds per
    /// location while the Greenhouse matures alongside
    /// the farm).
    /// </summary>
    private void SetUpBinState(CompostingBinState state)
    {
        _timeStub.TotalDays = 1;

        if (state == CompostingBinState.Empty)
        {
            return;
        }

        var wasteItem = ItemFactory.CreateValidWasteItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(wasteItem)).Returns(true);
        _service.AddWaste(MatrixLocationName, MatrixTile, wasteItem);

        if (state == CompostingBinState.Processing)
        {
            return;
        }

        _timeStub.TotalDays = 3;
        _service.ProcessDayStart(MatrixLocationName);
        _service.ProcessDayStart(MatrixLocationName);
    }

    /// <summary>Places the matrix world object the dislodgement paths require.</summary>
    private void PlaceMatrixWorldObject(GameLocation location)
    {
        // Idempotent: the dislodgement columns remove the
        // world object each time, so a stale object at the
        // tile (the placed column leaves one behind) is
        // replaced rather than double-added.
        location.Objects.Remove(MatrixTile);
        location.Objects.Add(MatrixTile, new StardewValley.Object
        {
            ItemId = ModConstants.CompostingBinItemId,
            TileLocation = MatrixTile
        });
    }

    /// <summary>
    /// T056 (SC-004): the full round-trip matrix. For every
    /// bin state, the placed column persists and reloads
    /// through the save data beside a world object; the
    /// held column travels through Item.modData (the bin
    /// picked up into the player inventory); and the debris
    /// column travels through the identical item object the
    /// dislodgement drops as floating world debris when the
    /// inventory is full. Each column restores the state it
    /// started from — a carried finished batch restores as
    /// Ready (zero days remaining is a finished batch, the
    /// domain CompostingBinStateModel semantics), so a bin
    /// cannot lose or reset its batch by being moved.
    /// </summary>
    [Theory]
    [InlineData(CompostingBinState.Empty)]
    [InlineData(CompostingBinState.Processing)]
    [InlineData(CompostingBinState.Ready)]
    public void RoundTrip_BinStateMatrix_EveryStateSurvivesSaveLoad_PlacedHeldAndDebris(
        CompostingBinState expectedState)
    {
        const string saveId = "roundtrip-matrix";
        const string key = ModConstants.CompostingBinKeyPrefix + saveId;

        // ---- Situation 1: placed in the world ----
        SetUpBinState(expectedState);

        CompostingBinData? savedData = null;
        _mockModDataService.Setup(x => x.SaveData(It.IsAny<CompostingBinData>(), It.IsAny<string>()))
            .Callback<CompostingBinData, string>((d, k) => savedData = d);
        _service.SaveData(saveId);

        var farmFixture = new GameLocationFixture(MatrixLocationName, isFarm: true);
        PlaceMatrixWorldObject(farmFixture.Location);
        _mockLocationProvider.Setup(x => x.GetLocationByName(MatrixLocationName)).Returns(farmFixture.Location);

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(key)).Returns(savedData);
        _service.LoadData(saveId);

        Assert.Equal(expectedState, _service.GetBinState(MatrixLocationName, MatrixTile));

        // ---- Situation 2: held in the player inventory ----
        SetUpBinState(expectedState);
        PlaceMatrixWorldObject(farmFixture.Location);

        var controller = CreateController();
        var dislodged = controller.TryDislodgeBin(farmFixture.Location, MatrixTile, _inventory);
        Assert.True(dislodged, "Dislodging the matrix bin must succeed so the held column can carry its state.");
        var heldItem = Assert.Single(_inventory.AddedItems);
        Assert.Equal(ModConstants.CompostingBinItemId, heldItem.ItemId);

        // The game serializes the held item and its modData
        // into the save; placing the bin back down restores
        // the progress it had (FR-005c).
        _service.OnBinPlaced(MatrixLocationName, MatrixTile, heldItem);
        Assert.Equal(expectedState, _service.GetBinState(MatrixLocationName, MatrixTile));

        // ---- Situation 3: dropped as floating debris ----
        SetUpBinState(expectedState);
        PlaceMatrixWorldObject(farmFixture.Location);

        var fullInventory = new RecordingPlayerInventory { AcceptsItems = false };
        Item? debrisItem = null;
        dislodged = controller.TryDislodgeBin(
            farmFixture.Location,
            MatrixTile,
            fullInventory,
            (item, _, _) => debrisItem = item);
        Assert.True(dislodged, "Dislodging the matrix bin must succeed even when the inventory is full.");
        Assert.Empty(fullInventory.AddedItems);
        Assert.NotNull(debrisItem);
        Assert.Equal(ModConstants.CompostingBinItemId, debrisItem!.ItemId);

        // The debris is the same item object the dislodgement
        // created, modData intact; the game saves floating
        // debris, and picking it up and placing it restores
        // the state it carried.
        _service.OnBinPlaced(MatrixLocationName, MatrixTile, debrisItem);
        Assert.Equal(expectedState, _service.GetBinState(MatrixLocationName, MatrixTile));
    }

    /// <summary>
    /// T070 (FR-012): a stored location the provider omits — a
    /// Greenhouse that is only lazily loaded and therefore absent
    /// from the location list at load time — still holds real bins.
    /// The tolerant reader must retain those entries instead of
    /// pruning them as orphans: pruning would silently destroy a
    /// batch the player left in a not-yet-visited location.
    /// </summary>
    [Fact]
    public void LoadData_ProviderOmitsStoredLocation_RetainsEntriesInsteadOfPruning()
    {
        // Arrange
        const string saveId = "test_save_lazy_location";
        const string key = ModConstants.CompostingBinKeyPrefix + saveId;

        var binData = new CompostingBinData();
        binData.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>
        {
            ["10,10"] = new CompostingBinStateData
            {
                State = "Processing",
                InputItemId = "(O)24",
                InputTimestamp = 1
            }
        };
        binData.LocationBinData["Greenhouse"] = new Dictionary<string, CompostingBinStateData>
        {
            ["5,5"] = new CompostingBinStateData
            {
                State = "Processing",
                InputItemId = "(O)24",
                InputTimestamp = 1
            },
            ["6,6"] = new CompostingBinStateData
            {
                State = "Ready",
                InputItemId = "(O)24",
                InputTimestamp = 2
            }
        };

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(key)).Returns(binData);

        var farmFixture = new GameLocationFixture("Farm", isFarm: true);
        farmFixture.Location.Objects.Add(new Vector2(10, 10), CreatePlacedBinObject(new Vector2(10, 10)));
        // The Greenhouse is deliberately not registered with the
        // provider — the omission a lazily-loaded location causes.
        _mockLocationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(farmFixture.Location);

        // Act
        _service.LoadData(saveId);

        // Assert
        // The provider-backed location loads normally.
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));

        // The omitted location's entries are retained, not pruned.
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Greenhouse", new Vector2(5, 5)));
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Greenhouse", new Vector2(6, 6)));

        // A save round-trip carries the retained entries forward.
        _service.SaveData(saveId);
        _mockModDataService.Verify(x => x.SaveData(
            It.Is<CompostingBinData>(data =>
                data.LocationBinData.ContainsKey("Greenhouse") &&
                data.LocationBinData["Greenhouse"].ContainsKey("5,5") &&
                data.LocationBinData["Greenhouse"].ContainsKey("6,6")),
            key), Times.Once);
    }
}
