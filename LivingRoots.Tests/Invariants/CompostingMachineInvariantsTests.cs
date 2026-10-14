using System.Threading;
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

namespace LivingRoots.Tests;

/// <summary>
/// Property-based invariant tests for machine state transitions (T016, US2):
/// Empty + Waste → Processing → Ready after exactly
/// <see cref="ModConstants.MaturationDays"/> days, with output quantity
/// strictly == 1.
/// </summary>
public class CompostingMachineInvariantsTests
{
    private readonly Mock<IOrganicWasteValidator> _mockWasteValidator;
    private readonly TimeProviderStub _timeStub;
    private readonly CompostingBinService _service;
    private int _caseSalt;

    public CompostingMachineInvariantsTests()
    {
        // The service calls Game1.playSound on maturation and collection.
        GameStateFixture.Install();

        var mockModDataService = new Mock<IModDataService>();
        var mockSaveIdProvider = new Mock<ISaveIdProvider>();
        _mockWasteValidator = new Mock<IOrganicWasteValidator>();
        var mockMonitor = new Mock<IMonitor>();
        _timeStub = new TimeProviderStub();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

        _service = new CompostingBinService(
            mockModDataService.Object,
            mockSaveIdProvider.Object,
            _mockWasteValidator.Object,
            mockMonitor.Object,
            _timeStub,
            new CompostingBinFactory(),
            new RecordingPlayerInventory());
    }

    /// <summary>
    /// Invariant: for any start day and any tile, Empty + waste becomes
    /// Processing immediately, stays Processing until exactly
    /// <see cref="ModConstants.MaturationDays"/> days elapse, then becomes
    /// Ready — never earlier.
    /// </summary>
    [Property(MaxTest = 100)]
    public void MachineState_EmptyPlusWaste_ProcessingThenReadyAfterExactlyTwoDays(
        PositiveInt arbitraryStartDay,
        PositiveInt arbitraryX,
        PositiveInt arbitraryY)
    {
        int startDay = 1 + (arbitraryStartDay.Get % 1000);
        var tile = new Vector2(
            (arbitraryX.Get + Interlocked.Increment(ref _caseSalt)) % 40,
            arbitraryY.Get % 40);

        _timeStub.TotalDays = startDay;
        _service.AddWaste("Farm", tile, ItemFactory.CreateValidWasteItem());
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", tile));

        _timeStub.TotalDays = startDay + ModConstants.MaturationDays - 1;
        _service.ProcessDayStart("Farm");
        Assert.Equal(
            CompostingBinState.Processing,
            _service.GetBinState("Farm", tile));

        _timeStub.TotalDays = startDay + ModConstants.MaturationDays;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", tile));
    }

    /// <summary>
    /// Invariant: for any start day and any tile, collecting a Ready batch
    /// yields strictly one compost item and returns the bin to Empty.
    /// </summary>
    [Property(MaxTest = 100)]
    public void MachineYield_ReadyBatch_CollectsExactlyOneCompost(
        PositiveInt arbitraryStartDay,
        PositiveInt arbitraryX,
        PositiveInt arbitraryY)
    {
        int startDay = 1 + (arbitraryStartDay.Get % 1000);
        var tile = new Vector2(
            (arbitraryX.Get + Interlocked.Increment(ref _caseSalt)) % 40,
            arbitraryY.Get % 40);

        _timeStub.TotalDays = startDay;
        _service.AddWaste("Farm", tile, ItemFactory.CreateValidWasteItem());
        _timeStub.TotalDays = startDay + ModConstants.MaturationDays;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", tile));

        int collected = _service.CollectCompost("Farm", tile);
        Assert.Equal(1, collected);
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", tile));
    }
}
