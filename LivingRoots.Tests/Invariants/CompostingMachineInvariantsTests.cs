using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
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

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariant tests verifying machine state transitions, processing duration,
/// and single compost yield for the composting bin machine.
/// Validates Task T016 [P] [US2] under Feature 004 (Playable Compost Loop).
/// </summary>
public class CompostingMachineInvariantsTests
{
    private static (CompostingBinService Service, TimeProviderStub TimeStub, RecordingPlayerInventory Inventory, Mock<IModDataService> ModDataMock) CreateTestContext()
    {
        GameStateFixture.Install();
        var mockModDataService = new Mock<IModDataService>();
        var mockSaveIdProvider = new Mock<ISaveIdProvider>();
        var mockWasteValidator = new Mock<IOrganicWasteValidator>();
        mockWasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);
        var mockMonitor = new Mock<IMonitor>();
        var timeStub = new TimeProviderStub();
        var factory = new CompostingBinFactory();
        var inventory = new RecordingPlayerInventory();

        var service = new CompostingBinService(
            mockModDataService.Object,
            mockSaveIdProvider.Object,
            mockWasteValidator.Object,
            mockMonitor.Object,
            timeStub,
            factory,
            inventory);

        return (service, timeStub, inventory, mockModDataService);
    }

    private static int GetDaysRemaining(CompostingBinService service, string locationName, Vector2 tile)
    {
        var state = service.GetBinState(locationName, tile);
        if (state != CompostingBinState.Processing)
        {
            return 0;
        }

        var dislodged = service.CreateDislodgedBinItem(locationName, tile);
        if (dislodged.modData != null &&
            dislodged.modData.TryGetValue(ModConstants.ModDataDaysRemainingKey, out var daysStr) &&
            int.TryParse(daysStr, out var days))
        {
            return days;
        }

        return 0;
    }

    [Property(MaxTest = 100)]
    public bool EmptyPlusValidWaste_TransitionsToProcessing_WithTwoDaysRemaining(NonNull<string> arbitraryWasteId)
    {
        string wasteId = arbitraryWasteId.Get;
        if (string.IsNullOrWhiteSpace(wasteId))
        {
            wasteId = "Object.Sap";
        }

        var (service, _, _, _) = CreateTestContext();
        var tile = new Vector2(10, 10);
        var wasteItem = ItemFactory.CreateItem(wasteId, -74);

        var initialState = service.GetBinState("Farm", tile);
        int initialDays = GetDaysRemaining(service, "Farm", tile);

        service.AddWaste("Farm", tile, wasteItem);

        var processingState = service.GetBinState("Farm", tile);
        int processingDays = GetDaysRemaining(service, "Farm", tile);

        return initialState == CompostingBinState.Empty &&
               initialDays == 0 &&
               processingState == CompostingBinState.Processing &&
               processingDays == ModConstants.MaturationDays;
    }

    [Property(MaxTest = 100)]
    public bool AdvancingOneDay_KeepsProcessing_WithOneDayRemaining(NonNull<string> arbitraryWasteId)
    {
        string wasteId = arbitraryWasteId.Get;
        if (string.IsNullOrWhiteSpace(wasteId))
        {
            wasteId = "Object.Sap";
        }

        var (service, timeStub, _, _) = CreateTestContext();
        var tile = new Vector2(10, 10);
        var wasteItem = ItemFactory.CreateItem(wasteId, -74);

        service.AddWaste("Farm", tile, wasteItem);

        timeStub.TotalDays += 1;
        service.ProcessDayStart("Farm");

        var state = service.GetBinState("Farm", tile);
        int daysRemaining = GetDaysRemaining(service, "Farm", tile);

        return state == CompostingBinState.Processing &&
               daysRemaining == 1;
    }

    [Property(MaxTest = 100)]
    public bool AdvancingTwoDays_TransitionsToReady_WithZeroDaysRemaining(NonNull<string> arbitraryWasteId)
    {
        string wasteId = arbitraryWasteId.Get;
        if (string.IsNullOrWhiteSpace(wasteId))
        {
            wasteId = "Object.Sap";
        }

        var (service, timeStub, _, _) = CreateTestContext();
        var tile = new Vector2(10, 10);
        var wasteItem = ItemFactory.CreateItem(wasteId, -74);

        service.AddWaste("Farm", tile, wasteItem);

        timeStub.TotalDays += 1;
        service.ProcessDayStart("Farm");

        timeStub.TotalDays += 1;
        service.ProcessDayStart("Farm");

        var state = service.GetBinState("Farm", tile);
        int daysRemaining = GetDaysRemaining(service, "Farm", tile);

        return state == CompostingBinState.Ready &&
               daysRemaining == 0;
    }

    [Property(MaxTest = 100)]
    public bool ProcessingDuration_IsStrictlyTwoFullDays_ForArbitraryValidWasteItems(
        NonNull<string> arbitraryWasteId,
        PositiveInt startDayGen)
    {
        string wasteId = arbitraryWasteId.Get;
        if (string.IsNullOrWhiteSpace(wasteId))
        {
            wasteId = "Object.Sap";
        }

        int startDay = (startDayGen.Get % 500) + 1;
        var (service, timeStub, _, _) = CreateTestContext();
        timeStub.TotalDays = startDay;

        var tile = new Vector2(10, 10);
        var wasteItem = ItemFactory.CreateItem(wasteId, -74);

        service.AddWaste("Farm", tile, wasteItem);

        // Day 0: processing duration not satisfied (0 elapsed) -> Processing, 2 days left
        bool day0Valid = service.GetBinState("Farm", tile) == CompostingBinState.Processing &&
                         GetDaysRemaining(service, "Farm", tile) == 2;

        // Day 1: processing duration not satisfied (1 elapsed < 2) -> Processing, 1 day left
        timeStub.TotalDays = startDay + 1;
        service.ProcessDayStart("Farm");
        bool day1Valid = service.GetBinState("Farm", tile) == CompostingBinState.Processing &&
                         GetDaysRemaining(service, "Farm", tile) == 1;

        // Day 2: processing duration satisfied (2 full days elapsed) -> Ready, 0 days left
        timeStub.TotalDays = startDay + 2;
        service.ProcessDayStart("Farm");
        bool day2Valid = service.GetBinState("Farm", tile) == CompostingBinState.Ready &&
                         GetDaysRemaining(service, "Farm", tile) == 0;

        return day0Valid && day1Valid && day2Valid;
    }

    [Property(MaxTest = 50)]
    public bool HarvestOutputQuantity_IsStrictlyOne_RetiringMaturationMultiplier(
        NonNull<string> arbitraryWasteId,
        PositiveInt cycleCountGen)
    {
        string wasteId = arbitraryWasteId.Get;
        if (string.IsNullOrWhiteSpace(wasteId))
        {
            wasteId = "Object.Sap";
        }

        int cycles = (cycleCountGen.Get % 4) + 2; // 2 to 5 cycles to test maturation accumulation
        var (service, timeStub, inventory, _) = CreateTestContext();
        var tile = new Vector2(10, 10);

        int currentDay = 1;
        timeStub.TotalDays = currentDay;

        for (int c = 0; c < cycles; c++)
        {
            var wasteItem = ItemFactory.CreateItem(wasteId, -74);
            service.AddWaste("Farm", tile, wasteItem);

            currentDay += 1;
            timeStub.TotalDays = currentDay;
            service.ProcessDayStart("Farm");

            currentDay += 1;
            timeStub.TotalDays = currentDay;
            service.ProcessDayStart("Farm");

            if (service.GetBinState("Farm", tile) != CompostingBinState.Ready)
            {
                return false;
            }

            int countBefore = inventory.AddedItems.Count;
            int yield = service.CollectCompost("Farm", tile);
            int countAfter = inventory.AddedItems.Count;

            // Output quantity must be strictly 1 per FR-005a
            if (yield != 1 || (countAfter - countBefore) != 1)
            {
                return false;
            }

            if (service.GetBinState("Farm", tile) != CompostingBinState.Empty)
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 50)]
    public bool HarvestOutputQuantity_IsStrictlyOne_ForArbitraryInitialMaturationLevels(
        NonNull<string> arbitraryWasteId,
        PositiveInt maturationGen)
    {
        string wasteId = arbitraryWasteId.Get;
        if (string.IsNullOrWhiteSpace(wasteId))
        {
            wasteId = "Object.Sap";
        }

        int maturationLevel = (maturationGen.Get % 5) + 1; // 1 to 5
        var (service, _, inventory, modDataMock) = CreateTestContext();
        var tile = new Vector2(10, 10);

        var data = new CompostingBinData();
        data.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>
        {
            ["10,10"] = new CompostingBinStateData
            {
                State = "Ready",
                MaturationLevel = maturationLevel,
                InputItemId = wasteId
            }
        };

        modDataMock.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(data);
        service.LoadData("save-maturation-test");

        int yield = service.CollectCompost("Farm", tile);

        // Per FR-005a, maturation multiplier (1-5) is retired; output must be strictly 1.
        return yield == 1 &&
               inventory.AddedItems.Count == 1 &&
               inventory.AddedItems[0].ItemId == ModConstants.CompostItemId &&
               service.GetBinState("Farm", tile) == CompostingBinState.Empty;
    }

    [Property(MaxTest = 100)]
    public bool CompostingBinStateModel_ModDataSerialization_PreservesProgressInvariants(
        NonNull<string> arbitraryWasteId,
        NonNegativeInt daysRemainingGen)
    {
        string wasteId = arbitraryWasteId.Get;
        if (string.IsNullOrWhiteSpace(wasteId))
        {
            wasteId = "(O)24";
        }

        int daysRemaining = daysRemainingGen.Get % 10;
        var model = new CompostingBinStateModel
        {
            InputItemId = wasteId,
            DaysRemaining = daysRemaining
        };

        var modData = new Dictionary<string, string>();
        model.WriteToModData(modData);

        var restored = new CompostingBinStateModel();
        restored.PopulateFromModData(modData);

        bool wasteMatches = restored.InputItemId == wasteId;
        bool daysMatches = restored.DaysRemaining == daysRemaining;
        bool stateMatches = daysRemaining <= 0
            ? restored.State == CompostingBinState.Ready
            : restored.State == CompostingBinState.Processing;

        return wasteMatches && daysMatches && stateMatches;
    }

    [Fact]
    public void MachineLifecycle_DeterministicTwoDayTransition_OutputStrictlyOne()
    {
        var (service, timeStub, inventory, _) = CreateTestContext();
        var tile = new Vector2(10, 10);
        var wasteItem = ItemFactory.CreateValidWasteItem();

        // 1. Initially Empty with DaysRemaining = 0
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));
        Assert.Equal(0, GetDaysRemaining(service, "Farm", tile));

        // 2. Add waste -> Processing with DaysRemaining = 2
        service.AddWaste("Farm", tile, wasteItem);
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));
        Assert.Equal(2, GetDaysRemaining(service, "Farm", tile));

        // 3. Advance 1 day -> Processing with DaysRemaining = 1
        timeStub.TotalDays += 1;
        service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));
        Assert.Equal(1, GetDaysRemaining(service, "Farm", tile));

        // Attempting collection mid-processing yields 0
        Assert.Equal(0, service.CollectCompost("Farm", tile));
        Assert.Empty(inventory.AddedItems);

        // 4. Advance 2nd day -> Ready with DaysRemaining = 0
        timeStub.TotalDays += 1;
        service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));
        Assert.Equal(0, GetDaysRemaining(service, "Farm", tile));

        // 5. Collect compost -> Yield strictly == 1
        int yield = service.CollectCompost("Farm", tile);
        Assert.Equal(1, yield);
        Assert.Single(inventory.AddedItems);
        Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));
        Assert.Equal(0, GetDaysRemaining(service, "Farm", tile));

        // 6. Cycle 2: Output quantity remains strictly == 1 (retiring 1-5 multiplier per FR-005a)
        service.AddWaste("Farm", tile, wasteItem);
        timeStub.TotalDays += 1;
        service.ProcessDayStart("Farm");
        timeStub.TotalDays += 1;
        service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));

        int cycle2Yield = service.CollectCompost("Farm", tile);
        Assert.Equal(1, cycle2Yield);
        Assert.Equal(2, inventory.AddedItems.Count);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));
    }
}
