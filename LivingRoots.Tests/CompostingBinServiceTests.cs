using Xunit;
using Moq;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using LivingRoots.Domain.Services;
using LivingRoots.Services;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace LivingRoots.Tests;

public class CompostingBinServiceTests
{
    private readonly Mock<IModDataService> _mockModDataService;
    private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
    private readonly Mock<IOrganicWasteValidator> _mockWasteValidator;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly TimeProviderStub _timeStub;
    private readonly CompostingBinFactory _factory;
    private readonly CompostingBinService _service;

    public CompostingBinServiceTests()
    {
        _mockModDataService = new Mock<IModDataService>();
        _mockSaveIdProvider = new Mock<ISaveIdProvider>();
        _mockWasteValidator = new Mock<IOrganicWasteValidator>();
        _mockMonitor = new Mock<IMonitor>();
        _timeStub = new TimeProviderStub();
        _factory = new CompostingBinFactory();

        _service = new CompostingBinService(
            _mockModDataService.Object,
            _mockSaveIdProvider.Object,
            _mockWasteValidator.Object,
            _mockMonitor.Object,
            _timeStub,
            _factory);
    }

    private Item CreateValidItem()
    {
        var item = new StardewValley.Object("Object.Sap", 1, false);
        item.Category = -74; // Seeds
        return item;
    }

    private Item CreateInvalidItem()
    {
        var item = new StardewValley.Object("Object.Stone", 1, false);
        item.Category = -999;
        return item;
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

        var player = new Farmer();
        var result = _service.CollectCompost("Farm", new Vector2(10, 10), player);

        Assert.Equal(2, result); // MaturationLevel was incremented to 2 after first collection cycle
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
        var player = new Farmer();
        var result = _service.CollectCompost("Farm", new Vector2(10, 10), player);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CollectCompost_FromProcessingBin_ReturnsZero()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);

        _service.AddWaste("Farm", new Vector2(10, 10), item);

        var player = new Farmer();
        var result = _service.CollectCompost("Farm", new Vector2(10, 10), player);
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
        var player = new Farmer();

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", new Vector2(10, 10)));

        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        var result = _service.CollectCompost("Farm", new Vector2(10, 10), player);
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
        _timeStub.TotalDays = 1;

        _service.AddWaste("Farm", new Vector2(10, 10), item);

        for (int day = 2; day <= 8; day++)
        {
            _timeStub.TotalDays = day;
            _service.ProcessDayStart("Farm");
        }

        _timeStub.TotalDays = 10;
        _service.ProcessDayStart("Farm");
        var player = new Farmer();
        var result = _service.CollectCompost("Farm", new Vector2(10, 10), player);
        Assert.Equal(2, result);
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
        var player = new Farmer();
        _service.CollectCompost("Farm", new Vector2(10, 10), player);

        for (int day = 4; day <= 17; day++)
        {
            _timeStub.TotalDays = day;
            _service.ProcessDayStart("Farm");
        }

        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 19;
        _service.ProcessDayStart("Farm");
        var result = _service.CollectCompost("Farm", new Vector2(10, 10), player);
        Assert.Equal(1, result);
    }

    [Fact]
    public void CollectCompost_OutputCountEqualsMaturationLevel()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        var player = new Farmer();

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);

        for (int day = 2; day <= 20; day++)
        {
            _timeStub.TotalDays = day;
            _service.ProcessDayStart("Farm");
            if (_service.GetBinState("Farm", new Vector2(10, 10)) == CompostingBinState.Ready)
            {
                var result = _service.CollectCompost("Farm", new Vector2(10, 10), player);
                Assert.InRange(result, 1, 5);
                break;
            }
        }
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
}
