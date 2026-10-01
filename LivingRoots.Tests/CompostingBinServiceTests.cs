using System.Collections.Generic;
using System.Threading.Tasks;
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

public class CompostingBinServiceTests
{
    private readonly Mock<IModDataService> _mockModDataService;
    private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
    private readonly Mock<IOrganicWasteValidator> _mockWasteValidator;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly TimeProviderStub _timeStub;
    private readonly CompostingBinFactory _factory;
    private readonly RecordingPlayerInventory _inventory;
    private readonly CompostingBinService _service;

    public CompostingBinServiceTests()
    {
        // The service calls Game1.playSound on maturation and collection. Without this the class
        // only passes when a sibling test class happens to have installed the sound stub first.
        GameStateFixture.Install();

        _mockModDataService = new Mock<IModDataService>();
        _mockSaveIdProvider = new Mock<ISaveIdProvider>();
        _mockWasteValidator = new Mock<IOrganicWasteValidator>();
        _mockMonitor = new Mock<IMonitor>();
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
            _inventory);
    }

    /// <summary>Creates an item the validator accepts (Seeds category).</summary>
    private Item CreateValidItem()
    {
        return ItemFactory.CreateValidWasteItem();
    }

    /// <summary>Creates an item the validator rejects (unknown category).</summary>
    private Item CreateInvalidItem()
    {
        return ItemFactory.CreateInvalidWasteItem();
    }

    [Fact]
    public void GetBinState_NewBin_ReturnsEmpty()
    {
        var state = _service.GetBinState("Farm", new Vector2(10, 10));
        Assert.Equal(CompostingBinState.Empty, state);
    }

    [Fact]
    public void AddWaste_ValidItem_TransitionsToProcessing()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        _service.AddWaste("Farm", new Vector2(10, 10), item);

        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    [Fact]
    public void ProcessDayStart_AfterTwoDays_TransitionsToReady()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _timeStub.TotalDays = 1;

        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    [Fact]
    public void CollectCompost_FromReadyBin_ReturnsCompostCount()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _timeStub.TotalDays = 1;

        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        var result = _service.CollectCompost("Farm", new Vector2(10, 10));

        Assert.Equal(1, result); // Returns MaturationLevel (1) BEFORE incrementing to 2
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    [Fact]
    public void AddWaste_ToProcessingBin_IsIgnored()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _service.AddWaste("Farm", new Vector2(10, 10), item);

        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    [Fact]
    public void AddWaste_ToReadyBin_IsIgnored()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _timeStub.TotalDays = 1;

        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");
        _service.AddWaste("Farm", new Vector2(10, 10), item);

        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    [Fact]
    public void CollectCompost_FromEmptyBin_ReturnsZero()
    {
        var result = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(0, result);
    }

    [Fact]
    public void CollectCompost_FromProcessingBin_ReturnsZero()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        _service.AddWaste("Farm", new Vector2(10, 10), item);

        var result = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(0, result);
    }

    [Fact]
    public void ProcessDayStart_AtThreshold_TransitionsCorrectly()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _timeStub.TotalDays = 1;

        _service.AddWaste("Farm", new Vector2(10, 10), item);

        _timeStub.TotalDays = 2;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));

        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    [Fact]
    public void FullLifecycle_AddProcessReadyCollectAddAgain()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));

        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        var result = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.True(result > 0);
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));

        _service.AddWaste("Farm", new Vector2(10, 10), item);
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    [Fact]
    public void ProcessDayStart_SevenActiveDays_IncrementsMaturation()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        // Cycle 1: add waste at day 1, process for 2 days (days 2-3), collect at day 3
        // ConsecutiveActiveDays: 0→1→2 after processing
        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 2;
        _service.ProcessDayStart("Farm");
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
        var result1 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(1, result1);

        // Cycle 2: add waste at day 4, process for 2 days (days 5-6), collect at day 6
        // ConsecutiveActiveDays: 2→3→4 after processing
        // MaturationLevel: 2→3 (CollectCompost returns 2)
        _timeStub.TotalDays = 4;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 5;
        _service.ProcessDayStart("Farm");
        _timeStub.TotalDays = 6;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
        var result2 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(2, result2); // Returns MaturationLevel (2) AFTER cycle 1 incremented it from 1

        // Cycle 3: add waste at day 7, process for 2 days (days 8-9), collect at day 9
        // ConsecutiveActiveDays: 4→5→6 after processing
        // MaturationLevel: 3→4 (CollectCompost returns 3)
        _timeStub.TotalDays = 7;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 8;
        _service.ProcessDayStart("Farm");
        _timeStub.TotalDays = 9;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
        var result3 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(3, result3); // Returns MaturationLevel (3) AFTER cycle 2 incremented it from 2

        // Cycle 4: add waste at day 10, process for 1 day (day 11)
        // ConsecutiveActiveDays: 6→7 → maturation increments! Level 4→5, reset to 0
        _timeStub.TotalDays = 10;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 11;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));

        // Process one more day to transition to Ready
        _timeStub.TotalDays = 12;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        // Collect — should return 5 (MaturationLevel is now 5)
        var result4 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(5, result4); // Returns MaturationLevel (5) AFTER 7-day maturation incremented it from 4
    }

    [Fact]
    public void ProcessDayStart_FourteenIdleDays_ResetsMaturation()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _timeStub.TotalDays = 1;

        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");
        _service.CollectCompost("Farm", new Vector2(10, 10));

        for (int day = 4; day <= 17; day++)
        {
            _timeStub.TotalDays = day;
            _service.ProcessDayStart("Farm");
        }

        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 19;
        _service.ProcessDayStart("Farm");
        var result = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(1, result);
    }

    [Fact]
    public void CollectCompost_OutputCountEqualsMaturationLevel()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);

        for (int day = 2; day <= 20; day++)
        {
            _timeStub.TotalDays = day;
            _service.ProcessDayStart("Farm");
            if (_service.GetBinState("Farm", new Vector2(10, 10)) == CompostingBinState.Ready)
            {
                var result = _service.CollectCompost("Farm", new Vector2(10, 10));
                Assert.InRange(result, 1, 5);
                break;
            }
        }
    }

    /// <summary>Grants exactly one item per maturation level to the injected inventory.</summary>
    /// <remarks>
    /// Pins the seam introduced for bug compostingbin-collectcompost-untestable: the grant must be
    /// observable through <see cref="IPlayerInventory"/>, not inferred from the return value alone.
    /// </remarks>
    [Fact]
    public void CollectCompost_AddsOneItemPerMaturationLevel()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);

        var result = 0;
        for (int day = 2; day <= 20 && result == 0; day++)
        {
            _timeStub.TotalDays = day;
            _service.ProcessDayStart("Farm");
            result = _service.CollectCompost("Farm", new Vector2(10, 10));
        }

        Assert.InRange(result, 1, ModConstants.MaturationMaxLevel);
        Assert.Equal(result, _inventory.AddedItems.Count);
    }

    /// <summary>
    /// A refused grant still consumes the bin, matching the pre-seam behaviour where
    /// <c>addItemToInventoryBool</c>'s return value was ignored.
    /// </summary>
    [Fact]
    public void CollectCompost_WhenInventoryRefuses_StillConsumesBin()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _inventory.AcceptsItems = false;

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        var result = _service.CollectCompost("Farm", new Vector2(10, 10));

        Assert.Equal(1, result);
        Assert.Empty(_inventory.AddedItems);
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));
        _mockMonitor.Verify(
            x => x.Log(It.Is<string>(s => s.Contains("inventory refused compost")), LogLevel.Warn),
            Times.Once);
    }

    [Fact]
    public void AddWaste_InvalidItem_IsIgnored()
    {
        var item = CreateInvalidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(false);

        _service.AddWaste("Farm", new Vector2(10, 10), item);

        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    [Fact]
    public void ProcessDayStart_EmptyLocation_IsNoOp()
    {
        _service.ProcessDayStart("NonExistentLocation");
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("NonExistentLocation", new Vector2(10, 10)));
    }

    // T110: OnObjectRemoved removes bin from runtime cache
    [Fact]
    public void OnObjectRemoved_BinExists_RemovesFromCache()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        _service.AddWaste("Farm", new Vector2(10, 10), item);
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));

        _service.OnObjectRemoved("Farm", new Vector2(10, 10));
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    // T049b: FR-1.14 — CollectCompost increments MaturationLevel (capped at 5)
    [Fact]
    public void CollectCompost_IncrementsMaturationCappedAt5()
    {
        CompostingBinData? savedData = null;

        _mockModDataService.Setup(x => x.SaveData(It.IsAny<CompostingBinData>(), It.IsAny<string>()))
            .Callback<CompostingBinData, string>((d, k) => savedData = d);

        // Part 1: MaturationLevel=3 → collect → should become 4
        var dataAt3 = new CompostingBinData();
        dataAt3.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>();
        dataAt3.LocationBinData["Farm"]["10,10"] = new CompostingBinStateData
        {
            State = "Ready",
            MaturationLevel = 3
        };

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(dataAt3);
        _service.LoadData("save-lvl3");

        var result1 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(3, result1);
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));

        _service.SaveData("save-lvl3");
        Assert.NotNull(savedData);
        Assert.True(savedData!.LocationBinData.ContainsKey("Farm"));
        Assert.True(savedData.LocationBinData["Farm"].ContainsKey("10,10"));
        Assert.Equal(4, savedData.LocationBinData["Farm"]["10,10"].MaturationLevel);

        // Part 2: MaturationLevel=5 → collect → should stay at 5 (cap)
        var dataAt5 = new CompostingBinData();
        dataAt5.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>();
        dataAt5.LocationBinData["Farm"]["10,10"] = new CompostingBinStateData
        {
            State = "Ready",
            MaturationLevel = 5
        };

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(dataAt5);
        savedData = null;

        _service.LoadData("save-lvl5");
        var result2 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(5, result2);
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));

        _service.SaveData("save-lvl5");
        Assert.NotNull(savedData);
        Assert.True(savedData!.LocationBinData.ContainsKey("Farm"));
        Assert.True(savedData.LocationBinData["Farm"].ContainsKey("10,10"));
        Assert.Equal(5, savedData.LocationBinData["Farm"]["10,10"].MaturationLevel);
    }

    // T050: US1 edge case — concurrent access maintains consistency
    [Fact]
    public async Task AddWaste_ConcurrentAccess_MaintainsConsistency()
    {
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

        int taskCount = 10;
        int iterationsPerTask = 100;

        var tasks = new List<Task>();
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerTask; j++)
                {
                    var localItem = CreateValidItem();
                    _service.AddWaste("Farm", new Vector2(10, 10), localItem);
                    _service.CollectCompost("Farm", new Vector2(10, 10));
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Invariant: bin is in a defined enum state
        var state = _service.GetBinState("Farm", new Vector2(10, 10));
        Assert.True(Enum.IsDefined(typeof(CompostingBinState), state));

        // Invariant: MaturationLevel in [1, 5]
        CompostingBinData? savedData = null;
        _mockModDataService.Setup(x => x.SaveData(It.IsAny<CompostingBinData>(), It.IsAny<string>()))
            .Callback<CompostingBinData, string>((d, k) => savedData = d);
        _service.SaveData("concurrent-test");

        if (savedData != null && savedData.LocationBinData.ContainsKey("Farm") && savedData.LocationBinData["Farm"].ContainsKey("10,10"))
        {
            var binData = savedData.LocationBinData["Farm"]["10,10"];
            Assert.InRange(binData.MaturationLevel, 1, 5);
            Assert.True(Enum.IsDefined(typeof(CompostingBinState), Enum.Parse<CompostingBinState>(binData.State)));
        }
    }

    // T050b: FR-1.11 + Fix-3 integration — maturation persists across session
    [Fact]
    public void SaveLoad_MaturationPersistsAcrossSession()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        // Drive bin through first cycle: add waste, process to Ready, collect (increments maturation to 2)
        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);

        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        var result = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(1, result);

        // Drive bin through second cycle: add waste, process for 2 days
        _timeStub.TotalDays = 4;
        _service.AddWaste("Farm", new Vector2(10, 10), item);

        _timeStub.TotalDays = 5;
        _service.ProcessDayStart("Farm");
        _timeStub.TotalDays = 6;
        _service.ProcessDayStart("Farm");

        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        // Save data
        CompostingBinData? savedData = null;
        _mockModDataService.Setup(x => x.SaveData(It.IsAny<CompostingBinData>(), It.IsAny<string>()))
            .Callback<CompostingBinData, string>((d, k) => savedData = d);
        _service.SaveData("test-save");
        Assert.NotNull(savedData);

        // Verify saved data has correct maturation values
        var binData = savedData!.LocationBinData["Farm"]["10,10"];
        Assert.Equal("Ready", binData.State);
        Assert.Equal(2, binData.MaturationLevel);
        Assert.Equal(3, binData.ConsecutiveActiveDays);

        // Create new service instance with fresh mocks
        var newMockModDataService = new Mock<IModDataService>();
        var newMockSaveIdProvider = new Mock<ISaveIdProvider>();
        var newMockWasteValidator = new Mock<IOrganicWasteValidator>();
        var newMockMonitor = new Mock<IMonitor>();
        var newTimeStub = new TimeProviderStub();
        var newFactory = new CompostingBinFactory();
        var newInventory = new RecordingPlayerInventory();

        newMockModDataService.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(savedData);

        var newService = new CompostingBinService(
            newMockModDataService.Object,
            newMockSaveIdProvider.Object,
            newMockWasteValidator.Object,
            newMockMonitor.Object,
            newTimeStub,
            newFactory,
            newInventory);

        // Load and re-save to verify persistence
        CompostingBinData? reloadedData = null;
        newMockModDataService.Setup(x => x.SaveData(It.IsAny<CompostingBinData>(), It.IsAny<string>()))
            .Callback<CompostingBinData, string>((d, k) => reloadedData = d);

        newService.LoadData("test-save");
        newService.SaveData("test-save");

        Assert.NotNull(reloadedData);
        Assert.True(reloadedData!.LocationBinData.ContainsKey("Farm"));
        Assert.True(reloadedData.LocationBinData["Farm"].ContainsKey("10,10"));

        var reloadedBinData = reloadedData.LocationBinData["Farm"]["10,10"];
        Assert.Equal("Ready", reloadedBinData.State);
        Assert.Equal(2, reloadedBinData.MaturationLevel);
        Assert.Equal(3, reloadedBinData.ConsecutiveActiveDays);
    }
}
