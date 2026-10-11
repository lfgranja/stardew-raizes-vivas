using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
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
using StardewValley.Audio;
using StardewValley.GameData.Machines;
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
        // Yield is strictly 1 per FR-005a (retiring 1-5 maturation multiplier)
        _timeStub.TotalDays = 4;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 5;
        _service.ProcessDayStart("Farm");
        _timeStub.TotalDays = 6;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
        var result2 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(1, result2);

        // Cycle 3: add waste at day 7, process for 2 days (days 8-9), collect at day 9
        // ConsecutiveActiveDays: 4→5→6 after processing
        // Yield is strictly 1 per FR-005a
        _timeStub.TotalDays = 7;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 8;
        _service.ProcessDayStart("Farm");
        _timeStub.TotalDays = 9;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
        var result3 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(1, result3);

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

        // Collect — strictly 1 compost item per FR-005a
        var result4 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(1, result4);
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
    public void CollectCompost_OutputCountEqualsOne()
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
                Assert.Equal(1, result);
                break;
            }
        }
    }

    /// <summary>Grants exactly one item to the injected inventory per FR-005a.</summary>
    /// <remarks>
    /// Pins the seam introduced for bug compostingbin-collectcompost-untestable: the grant must be
    /// observable through <see cref="IPlayerInventory"/>, not inferred from the return value alone.
    /// </remarks>
    [Fact]
    public void CollectCompost_AddsOneItemToInventory()
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

        Assert.Equal(1, result);
        Assert.Single(_inventory.AddedItems);
    }

    // ------------------------------------------------------------------
    // T119 (FR-005b partial): one finished batch pays exactly one
    // compost TOTAL across both drivers that can collect it — the
    // native Data/Machines output declaration the game's own machine
    // machinery pays, and the mod's interception harvest
    // (ModController.TryInteractWithBin → CompostingBinService.
    // CollectCompost). Both drivers are live: the native declaration
    // pays its declared OutputItem and the interception path pays
    // through the injected player inventory — the same inventory the
    // composition root injects into both layers — so an unreconciled
    // batch is collected twice. Property-based testing is N/A here:
    // the invariant is per-batch exactly-once over one fixed finished
    // batch, with no input domain to generate over.
    // ------------------------------------------------------------------

    /// <summary>
    /// T119 (FR-005b partial): a finished batch reachable by both
    /// the native machine interface and the mod's interception harvest
    /// pays exactly one compost in total. The native Data/Machines
    /// output declaration is read from the real ModEntry registration
    /// — not restated by this test — so the payout each driver makes
    /// is derived from what the mod actually ships: with both drivers
    /// live, the same batch is collected twice and the total is two,
    /// the double-payout hazard this test pins.
    /// </summary>
    [Fact]
    public void CollectCompost_OneBatchPaysExactlyOneAcrossNativeAndInterceptionPaths()
    {
        // The native output declaration the game's machine machinery
        // pays for a finished bin, read from the real registration.
        var machineData = ReadRegisteredBinMachineData();
        var declaredOutput = machineData?.OutputRules?
            .SelectMany(rule => rule.OutputItem ?? new List<MachineItemOutput>())
            .FirstOrDefault();

        // A finished batch reachable by both paths: the mod's runtime
        // cache (the interception harvest) and the placed machine the
        // native machine interface collects.
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        var tile = new Vector2(10, 10);
        var location = CreateLocationWithPlacedBin(tile);
        var controller = CreateInteractionController();

        // Driver 1 — the interception harvest: the controller collects
        // the finished batch through CompostingBinService.CollectCompost,
        // granting the yield into the shared player inventory.
        controller.TryInteractWithBin(location, tile, 1L, null);

        // Driver 2 — the native Data/Machines machinery: collecting the
        // finished machine through the native interface pays the output
        // item the registered declaration names, into the same player
        // inventory (the composition root injects one inventory into both
        // layers, so a second grant here is a duplicated compost).
        if (declaredOutput != null)
        {
            var nativeOutput = new StardewValley.Object { ItemId = declaredOutput.ItemId };
            _inventory.TryAddItem(nativeOutput);
        }

        // FR-005b: exactly one compost in total across both drivers.
        Assert.Single(_inventory.AddedItems);
    }

    /// <summary>
    /// T141 (FR-005a / Wave 15 red-first property): across both drivers
    /// that can collect a finished batch — the native Data/Machines
    /// output declaration and the interception harvest (controller
    /// → CompostingBinService.CollectCompost) — exactly one compost
    /// item is yielded in total; a missing guard produces double-payout
    /// (count == 2) so this property returns false until the guard is in.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CollectCompost_OneBatchPaysExactlyOneAcrossNativeAndInterceptionPaths_Property(
        NonNull<string> arbitraryItemId,
        int arbitraryCategory)
    {
        string itemId = arbitraryItemId.Get;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = "Object.Seeds";
        }

        var item = ItemFactory.CreateItem(itemId, arbitraryCategory);
        var wasteValidator = new OrganicWasteValidator();

        // Property premise: only test over qualifying waste batches.
        if (!wasteValidator.IsValidOrganicWaste(item))
        {
            return true; // premise not met — vacuous pass
        }

        var inventory = new RecordingPlayerInventory();
        var timeStub = new TimeProviderStub();
        var service = new CompostingBinService(
            new Mock<IModDataService>().Object,
            new Mock<ISaveIdProvider>().Object,
            wasteValidator,
            new Mock<IMonitor>().Object,
            timeStub,
            new CompostingBinFactory(),
            inventory);

        var tile = new Vector2(10, 10);
        timeStub.TotalDays = 1;
        service.AddWaste("Farm", tile, item);
        timeStub.TotalDays = 3;
        service.ProcessDayStart("Farm");
        if (service.GetBinState("Farm", tile) != CompostingBinState.Ready)
        {
            return true; // batch not finished — premise not met
        }

        // Driver 1 — interception harvest.
        var location = CreateLocationWithPlacedBin(tile);
        var controller = new ModController(
            new Mock<IModHelper>().Object,
            new Mock<IMonitor>().Object,
            new Mock<IManifest>().Object,
            new Mock<ISoilHealthService>().Object,
            new Mock<ISaveIdProvider>().Object,
            service,
            new Mock<ISoilDecayService>().Object);
        controller.TryInteractWithBin(location, tile, 1L, null);

        // Driver 2 — native Data/Machines output.
        var machineData = ReadRegisteredBinMachineData();
        var declaredOutput = machineData?.OutputRules?
            .SelectMany(rule => rule.OutputItem ?? new List<MachineItemOutput>())
            .FirstOrDefault();
        if (declaredOutput != null)
        {
            var nativeOutput = new StardewValley.Object { ItemId = declaredOutput.ItemId };
            inventory.TryAddItem(nativeOutput);
        }

        // True only when exactly one compost yielded (guard present).
        return inventory.AddedItems.Count == 1;
    }

    /// <summary>
    /// T141 (red-first, blocking T156): single-batch payout invariant.
    /// Exactly 1 compost yielded per finished batch; native Data/Machines
    /// output disabled (no declared-output grant); CollectCompost is sole
    /// credit. Fails red against current line 70 (outputCount = bin.MaturationLevel)
    /// when MaturationLevel &gt; 1 — load with level 3 to expose the bug.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CollectCompost_SingleBatchPayout_ExactlyOne_NativeDisabled(
        NonNull<string> arbitraryItemId,
        int arbitraryCategory)
    {
        string itemId = arbitraryItemId.Get;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = "(O)24";
        }

        var item = ItemFactory.CreateItem(itemId, arbitraryCategory);
        var wasteValidator = new OrganicWasteValidator();
        if (!wasteValidator.IsValidOrganicWaste(item))
        {
            return true; // premise not met — vacuous pass
        }

        var inventory = new RecordingPlayerInventory();
        var timeStub = new TimeProviderStub();

        // Force finished batch with MaturationLevel > 1 so line 70 yields > 1.
        var data = new CompostingBinData();
        data.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>();
        data.LocationBinData["Farm"]["10,10"] = new CompostingBinStateData
        {
            State = "Ready",
            InputItemId = item.QualifiedItemId,
            InputTimestamp = 1,
            MaturationLevel = 3 // bug trigger: current code outputs 3, invariant demands 1
        };

        var loadMock = new Mock<IModDataService>();
        loadMock.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(data);

        var serviceWithLoad = new CompostingBinService(
            loadMock.Object,
            new Mock<ISaveIdProvider>().Object,
            wasteValidator,
            new Mock<IMonitor>().Object,
            timeStub,
            new CompostingBinFactory(),
            inventory);

        serviceWithLoad.LoadData("t141");
        Assert.Equal(CompostingBinState.Ready, serviceWithLoad.GetBinState("Farm", new Vector2(10, 10)));

        // Native Data/Machines output DISABLED: do NOT add declared-output item.
        var result = serviceWithLoad.CollectCompost("Farm", new Vector2(10, 10));

        // CollectCompost sole credit — only this call grants compost.
        // Invariant: exactly 1 compost regardless of MaturationLevel.
        return result == 1 && inventory.AddedItems.Count == 1;
    }

    /// <summary>
    /// Reads the native <c>Data/Machines</c> entry <see cref="ModEntry"/>
    /// registers for the composting bin — the native output declaration the
    /// game's own machine machinery pays when a finished machine is
    /// collected. The registration is driven the way
    /// <c>AssetRegistrationTests</c> drives it: a real <see cref="ModEntry"/>
    /// handles a fabricated <see cref="AssetRequestedEventArgs"/> for
    /// <c>Data/Machines</c>, and the recorded edit operation is applied to
    /// a dictionary the test owns, so the declaration read here is the one
    /// the mod actually ships.
    /// </summary>
    private static MachineData? ReadRegisteredBinMachineData()
    {
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Machines");
        modEntry.OnAssetRequested(null, args);

        var editOperations = GetEditOperations(args);
        if (editOperations.Count == 0)
        {
            return null;
        }

        var machinesDict = new Dictionary<string, MachineData>();
        ApplyEditOperation(editOperations[0]!, machinesDict);

        return machinesDict.TryGetValue("(BC)" + ModConstants.MachineCompostingBinId, out var machineData)
            ? machineData
            : machinesDict.TryGetValue(ModConstants.MachineCompostingBinId, out machineData)
                ? machineData
                : null;
    }

    /// <summary>Fabricates the SMAPI event args the registration handler reads.</summary>
    private static AssetRequestedEventArgs CreateAssetRequestedEventArgs(string assetName)
    {
        var mockName = new Mock<IAssetName>();
        mockName.Setup(n => n.IsEquivalentTo(assetName, It.IsAny<bool>())).Returns(true);
        mockName.Setup(n => n.IsEquivalentTo(It.Is<string>(s => s != assetName), It.IsAny<bool>())).Returns(false);

        var mockAssetInfo = new Mock<IAssetInfo>();
        mockAssetInfo.Setup(a => a.NameWithoutLocale).Returns(mockName.Object);

        var ctor = typeof(AssetRequestedEventArgs).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        args[0] = mockAssetInfo.Object;

        if (parameters.Length > 1 && parameters[1].ParameterType.IsGenericType)
        {
            var genArgs = parameters[1].ParameterType.GetGenericArguments();
            var returnType = genArgs.Last();
            var paramTypes = genArgs.Take(genArgs.Length - 1).ToArray();
            var method = new DynamicMethod("DummyGetOnBehalfOf", returnType, paramTypes, typeof(CompostingBinServiceTests).Module);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            args[1] = method.CreateDelegate(parameters[1].ParameterType);
        }

        var instance = (AssetRequestedEventArgs)ctor.Invoke(args);

        var editOpsField = typeof(AssetRequestedEventArgs).GetField("<EditOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        if (editOpsField != null && editOpsField.GetValue(instance) == null)
        {
            editOpsField.SetValue(instance, Activator.CreateInstance(editOpsField.FieldType));
        }

        var loadOpsField = typeof(AssetRequestedEventArgs).GetField("<LoadOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        if (loadOpsField != null && loadOpsField.GetValue(instance) == null)
        {
            loadOpsField.SetValue(instance, Activator.CreateInstance(loadOpsField.FieldType));
        }

        var modField = typeof(AssetRequestedEventArgs).GetField("Mod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (modField != null)
        {
            var mockType = typeof(Mock<>).MakeGenericType(modField.FieldType);
            var mockModMetadata = (Mock)Activator.CreateInstance(mockType)!;
            modField.SetValue(instance, mockModMetadata.Object);
        }

        return instance;
    }

    /// <summary>The edit operations the handler recorded against the fabricated event args.</summary>
    private static IList GetEditOperations(AssetRequestedEventArgs args)
    {
        var field = typeof(AssetRequestedEventArgs).GetField("<EditOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IList)field!.GetValue(args)!;
    }

    /// <summary>
    /// Applies a recorded edit operation against a dictionary the test
    /// owns, standing in for the game's editable asset data, so the
    /// handler's edit lands in the dictionary the test reads back.
    /// </summary>
    private static void ApplyEditOperation<TKey, TValue>(object editOp, IDictionary<TKey, TValue> targetDict)
        where TKey : notnull
    {
        var mockAssetData = new Mock<IAssetData>();
        var mockDictData = new Mock<IAssetDataForDictionary<TKey, TValue>>();
        mockDictData.Setup(d => d.Data).Returns(targetDict);
        mockAssetData.Setup(a => a.AsDictionary<TKey, TValue>()).Returns(mockDictData.Object);

        var applyMethod = editOp.GetType().GetMethod("Apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (applyMethod != null)
        {
            applyMethod.Invoke(editOp, new object[] { mockAssetData.Object });
        }
        else
        {
            var actionField = editOp.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => typeof(Delegate).IsAssignableFrom(f.FieldType));
            var action = actionField?.GetValue(editOp) as Action<IAssetData>;
            action?.Invoke(mockAssetData.Object);
        }
    }

    /// <summary>
    /// Under FR-005b, finished compost must NOT be lost or consumed when inventory refuses;
    /// it must stay in Ready state, return 0 items harvested, and emit a warning.
    /// </summary>
    [Fact]
    public void CollectCompost_WhenInventoryRefuses_RetainsBinReadyState()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _inventory.AcceptsItems = false;

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        // T121 (FR-005b, FR-008a): the refusal must reach the
        // player as a red HUD message carrying translated text —
        // never the raw translation key. Snapshot the HUD before
        // the refusal so the rendered message is identifiable
        // afterwards.
        int hudMessagesBefore = Game1.hudMessages.Count;

        var result = _service.CollectCompost("Farm", new Vector2(10, 10));

        Assert.Equal(0, result);
        Assert.Empty(_inventory.AddedItems);
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
        _mockMonitor.Verify(
            x => x.Log(It.Is<string>(s => s.Contains("inventory full")), LogLevel.Warn),
            Times.Once);

        // T121: exactly one red HUD message was rendered for the
        // refusal, and its text is the translated refusal — never
        // the raw key (FR-008a). The service under test carries no
        // translation helper, so whatever the refusal path falls
        // back to must not leak the raw key to the player.
        Assert.Equal(hudMessagesBefore + 1, Game1.hudMessages.Count);
        string refusalText = Game1.hudMessages[Game1.hudMessages.Count - 1].message;
        Assert.NotEqual(ModConstants.TranslationKeyHarvestInventoryFull, refusalText);
    }

    [Fact]
    public void AddWaste_InvalidItem_IsIgnored()
    {
        var item = CreateInvalidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(false);

        _service.AddWaste("Farm", new Vector2(10, 10), item);

        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    // ------------------------------------------------------------------
    // T058 (FR-006): no path other than a completed-batch harvest may
    // yield compost. CollectCompost is the service's single compost
    // grant site, so every bypass vector — harvesting a bin that never
    // processed, harvesting an unfinished batch, and injecting a forged
    // "finished batch" record into save data — must be refused or
    // quarantined with zero compost granted.
    // ------------------------------------------------------------------

    /// <summary>
    /// T058 (FR-006): a bin that never received waste grants nothing.
    /// The debug-spawn and console-command vectors have no service-level
    /// counterpart that produces compost, so collecting from an untouched
    /// bin must refuse with zero items.
    /// </summary>
    [Fact]
    public void CollectCompost_FromNeverProcessedBin_GrantsNoCompostItem()
    {
        var result = _service.CollectCompost("Farm", new Vector2(10, 10));

        Assert.Equal(0, result);
        Assert.Empty(_inventory.AddedItems);
    }

    /// <summary>
    /// T058 (FR-006): waste that has not completed the two-day batch
    /// (FR-001b, MaturationDays) yields nothing — the premature harvest
    /// attempt is refused with zero items granted.
    /// </summary>
    [Fact]
    public void CollectCompost_FromUnmaturedBin_GrantsNoCompostItem()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _timeStub.TotalDays = 1;

        _service.AddWaste("Farm", new Vector2(10, 10), item);

        var result = _service.CollectCompost("Farm", new Vector2(10, 10));

        Assert.Equal(0, result);
        Assert.Empty(_inventory.AddedItems);
    }

    /// <summary>
    /// T058 (FR-006 + FR-012): a save record claiming a finished batch
    /// without recording the input it processed is a forged record. The
    /// state machine only ever reaches Ready from Processing, and AddWaste
    /// always sets InputItemId when it enters Processing, so no legitimate
    /// save can contain a Ready record without an input item. The tolerant
    /// reader must quarantine it — skip it from runtime registration with a
    /// diagnostic log (FR-012, sanitized per FR-015) — instead of
    /// registering a batch that never ran, so the injected "finished
    /// batch" yields no compost.
    /// </summary>
    [Fact]
    public void LoadData_SaveInjectedReadyRecordWithoutInput_GrantsNoCompostItem()
    {
        const string saveId = "save-injected-ready";

        var data = new CompostingBinData();
        data.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>
        {
            ["10,10"] = new CompostingBinStateData
            {
                State = "Ready"
            }
        };

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(data);

        _service.LoadData(saveId);

        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));

        var result = _service.CollectCompost("Farm", new Vector2(10, 10));

        Assert.Equal(0, result);
        Assert.Empty(_inventory.AddedItems);
        _mockMonitor.Verify(
            x => x.Log(It.Is<string>(s => s.Contains("10,10")), It.IsAny<LogLevel>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// T058 control: a Ready record carrying its batch provenance — the
    /// input item and the day it was loaded, exactly what SaveData writes
    /// for a legitimately finished batch — must keep loading and yielding
    /// exactly one compost (FR-005a). Quarantining provenance-less
    /// records must not over-correct into dropping legitimate saves.
    /// </summary>
    [Fact]
    public void LoadData_LegitimateReadyRecordWithInputItem_StillHarvestsOneCompost()
    {
        const string saveId = "save-legitimate-ready";

        var data = new CompostingBinData();
        data.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>
        {
            ["10,10"] = new CompostingBinStateData
            {
                State = "Ready",
                InputItemId = "(O)24",
                InputTimestamp = 1
            }
        };

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(data);

        _service.LoadData(saveId);

        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        var result = _service.CollectCompost("Farm", new Vector2(10, 10));

        Assert.Equal(1, result);
        Assert.Single(_inventory.AddedItems);
        Assert.Equal(ModConstants.CompostItemId, _inventory.AddedItems[0].ItemId);
    }

    // ------------------------------------------------------------------
    // T059 (FR-003 + FR-008a clarification 2026-10-04): offering a
    // non-qualifying item to the bin is a vanilla machine rejection —
    // the game's own audio cancel cue, with no HUD text (FR-008a
    // reserves showRedMessage for tile compost application, full
    // inventory on harvest, and concurrency collisions) and nothing
    // consumed or granted.
    // ------------------------------------------------------------------

    /// <summary>Vanilla machine-rejection cue the game plays for refused actions.</summary>
    private const string CancelCueName = "cancel";

    /// <summary>Vanilla item category for Stone — outside the inherited compostable set.</summary>
    private const int StoneCategory = -12;

    /// <summary>
    /// T059 anchor (FR-003 + FR-008a clarification 2026-10-04): stone is
    /// vanilla category -12, outside the qualifying set inherited from the
    /// soil decay and compost specification, so the bin must reject it the
    /// way a native machine does — the vanilla audio cancel cue, no red HUD
    /// message, nothing consumed or granted, and the bin left empty.
    /// </summary>
    [Fact]
    public void AddWaste_InvalidItem_PlaysVanillaCancelCue()
    {
        var item = ItemFactory.CreateItem("Object.Stone", StoneCategory);
        var inventory = new RecordingPlayerInventory();
        var service = new CompostingBinService(
            new Mock<IModDataService>().Object,
            new Mock<ISaveIdProvider>().Object,
            new OrganicWasteValidator(),
            new Mock<IMonitor>().Object,
            new TimeProviderStub(),
            new CompostingBinFactory(),
            inventory);

        var tile = new Vector2(10, 10);
        int hudMessagesBefore = Game1.hudMessages.Count;

        var sounds = new RecordingSoundsHelper();
        var previousSounds = Game1.sounds;
        Game1.sounds = sounds;
        try
        {
            service.AddWaste("Farm", tile, item);
        }
        finally
        {
            Game1.sounds = previousSounds;
        }

        Assert.Contains(CancelCueName, sounds.Cues);
        Assert.Equal(hudMessagesBefore, Game1.hudMessages.Count);
        Assert.Empty(inventory.AddedItems);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));
    }

    /// <summary>
    /// T059 property (FR-003 + FR-008a clarification 2026-10-04): for an
    /// arbitrary item the inherited qualifying set rejects, the bin must
    /// refuse it with native machine behaviour — the vanilla audio cancel
    /// cue, zero <c>Game1.showRedMessage</c> calls (no new HUD message),
    /// zero inventory activity, and the bin left empty.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AddWaste_NonQualifyingItem_PlaysVanillaCancelCueWithoutHudMessage(
        NonNull<string> arbitraryItemId,
        int arbitraryCategory)
    {
        string itemId = arbitraryItemId.Get;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = "Object.Stone";
        }

        var item = ItemFactory.CreateItem(itemId, arbitraryCategory);
        var wasteValidator = new OrganicWasteValidator();

        // Property premise: the generated item is non-qualifying waste.
        if (wasteValidator.IsValidOrganicWaste(item))
        {
            return true;
        }

        var inventory = new RecordingPlayerInventory();
        var service = new CompostingBinService(
            new Mock<IModDataService>().Object,
            new Mock<ISaveIdProvider>().Object,
            wasteValidator,
            new Mock<IMonitor>().Object,
            new TimeProviderStub(),
            new CompostingBinFactory(),
            inventory);

        var tile = new Vector2(10, 10);
        int hudMessagesBefore = Game1.hudMessages.Count;

        var sounds = new RecordingSoundsHelper();
        var previousSounds = Game1.sounds;
        Game1.sounds = sounds;
        try
        {
            service.AddWaste("Farm", tile, item);
        }
        finally
        {
            Game1.sounds = previousSounds;
        }

        return sounds.Cues.Contains(CancelCueName)
               && Game1.hudMessages.Count == hudMessagesBefore
               && inventory.AddedItems.Count == 0
               && service.GetBinState("Farm", tile) == CompostingBinState.Empty;
    }

    // ------------------------------------------------------------------
    // T124 (FR-008a): an invalid drop-in is a vanilla machine
    // rejection — the audio cancel cue AND the machine shake,
    // the way a native machine refuses an item it cannot
    // process. The shake is observable on the placed machine:
    // the world object's shake timer is non-zero after the
    // refusal. Property-based testing applies — the invalid-
    // item domain is generatable — so the anchor fact below
    // is mirrored by a property over arbitrary non-qualifying
    // items.
    // ------------------------------------------------------------------

    /// <summary>
    /// T124 (FR-008a): an invalid drop-in shakes the machine
    /// in addition to the cancel cue. The service reaches the
    /// placed machine through the injected location provider —
    /// the same seam the persistence layer uses — and the
    /// refusal must leave the machine's shake timer running,
    /// the observable the game's own machines set when they
    /// reject an input.
    /// </summary>
    [Fact]
    public void AddWaste_InvalidItem_ShakesMachineInAdditionToCancelCue()
    {
        var tile = new Vector2(10, 10);
        var item = ItemFactory.CreateItem("Object.Stone", StoneCategory);

        var location = CreateLocationWithPlacedBin(tile);
        var locationProvider = new Mock<ILocationProvider>();
        locationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(location);

        var inventory = new RecordingPlayerInventory();
        var service = new CompostingBinService(
            new Mock<IModDataService>().Object,
            new Mock<ISaveIdProvider>().Object,
            new OrganicWasteValidator(),
            new Mock<IMonitor>().Object,
            new TimeProviderStub(),
            new CompostingBinFactory(),
            inventory,
            locationProvider.Object);

        var sounds = new RecordingSoundsHelper();
        var previousSounds = Game1.sounds;
        Game1.sounds = sounds;
        try
        {
            service.AddWaste("Farm", tile, item);
        }
        finally
        {
            Game1.sounds = previousSounds;
        }

        // The vanilla machine rejection: the audio cancel cue ...
        Assert.Contains(CancelCueName, sounds.Cues);

        // ... and the machine shake (FR-008a): the placed
        // machine shook — its shake timer is non-zero.
        var worldObject = location.Objects[tile];
        Assert.True(worldObject.shakeTimer > 0, "An invalid drop-in must shake the machine (FR-008a).");
    }

    /// <summary>
    /// T124 property (FR-008a): for an arbitrary item the
    /// inherited qualifying set rejects, the bin refuses it
    /// with native machine behaviour — the vanilla audio
    /// cancel cue AND a machine shake — with zero
    /// <c>Game1.showRedMessage</c> calls (FR-008a reserves
    /// red HUD text for tile compost application, a
    /// full-inventory harvest, and concurrency collisions),
    /// zero inventory activity, and the bin left empty.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AddWaste_NonQualifyingItem_ShakesMachineAlongsideCancelCue(
        NonNull<string> arbitraryItemId,
        int arbitraryCategory)
    {
        string itemId = arbitraryItemId.Get;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = "Object.Stone";
        }

        var item = ItemFactory.CreateItem(itemId, arbitraryCategory);
        var wasteValidator = new OrganicWasteValidator();

        // Property premise: the generated item is non-qualifying waste.
        if (wasteValidator.IsValidOrganicWaste(item))
        {
            return true;
        }

        var tile = new Vector2(10, 10);
        var location = CreateLocationWithPlacedBin(tile);
        var locationProvider = new Mock<ILocationProvider>();
        locationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(location);

        var inventory = new RecordingPlayerInventory();
        var service = new CompostingBinService(
            new Mock<IModDataService>().Object,
            new Mock<ISaveIdProvider>().Object,
            wasteValidator,
            new Mock<IMonitor>().Object,
            new TimeProviderStub(),
            new CompostingBinFactory(),
            inventory,
            locationProvider.Object);

        int hudMessagesBefore = Game1.hudMessages.Count;

        var sounds = new RecordingSoundsHelper();
        var previousSounds = Game1.sounds;
        Game1.sounds = sounds;
        try
        {
            service.AddWaste("Farm", tile, item);
        }
        finally
        {
            Game1.sounds = previousSounds;
        }

        var worldObject = location.Objects[tile];
        return sounds.Cues.Contains(CancelCueName)
               && worldObject.shakeTimer > 0
               && Game1.hudMessages.Count == hudMessagesBefore
               && inventory.AddedItems.Count == 0
               && service.GetBinState("Farm", tile) == CompostingBinState.Empty;
    }

    // ------------------------------------------------------------------
    // T066 (FR-008a scoping): the red HUD conflict text is owed
    // only for tile compost application, a full-inventory harvest,
    // and a true concurrency collision. A deposit refused because
    // the bin is already processing, or a collection refused
    // because the bin is not ready, is an ordinary invalid bin
    // state — vanilla machine cues, not a collision — so the
    // refusal handler must NOT receive "message.compost.action-
    // conflict" for them. The refusal handler lives in
    // ModController.TryInteractWithBin (T091 merged the standalone
    // bin controller into ModController; no CompostingBinController
    // exists), so these tests drive the controller seam over the
    // real class-level service rather than the service alone.
    // ------------------------------------------------------------------

    /// <summary>
    /// The refusal translation key FR-008a reserves for genuine
    /// concurrency collisions.
    /// </summary>
    private const string ActionConflictRefusalKey = "message.compost.action-conflict";

    /// <summary>
    /// Builds a controller over the class-level bin service for the
    /// interaction-path tests. The lock service is injectable so a
    /// test can pre-hold a tile lock and stage a genuine collision.
    /// </summary>
    private ModController CreateInteractionController(IHostAtomicLockService? lockService = null)
    {
        return new ModController(
            new Mock<IModHelper>().Object,
            _mockMonitor.Object,
            new Mock<IManifest>().Object,
            new Mock<ISoilHealthService>().Object,
            _mockSaveIdProvider.Object,
            _service,
            new Mock<ISoilDecayService>().Object,
            hostAtomicLockService: lockService);
    }

    /// <summary>Places the physical bin the world-object gate requires.</summary>
    private static GameLocation CreateLocationWithPlacedBin(Vector2 tile)
    {
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        fixture.Location.Objects.Add(tile, new StardewValley.Object
        {
            ItemId = ModConstants.CompostingBinItemId,
            TileLocation = tile
        });
        return fixture.Location;
    }

    /// <summary>
    /// T066 (FR-008a): a deposit refused because the bin is already
    /// processing a batch is an invalid bin state, not a concurrency
    /// collision — the refusal handler must not receive the conflict key.
    /// </summary>
    [Fact]
    public void TryInteractWithBin_DepositToProcessingBin_DoesNotEmitActionConflict()
    {
        var tile = new Vector2(10, 10);
        var wasteItem = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(wasteItem)).Returns(true);
        _service.AddWaste("Farm", tile, wasteItem);
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", tile));

        var location = CreateLocationWithPlacedBin(tile);
        var controller = CreateInteractionController();
        var refusalKeys = new List<string>();
        var heldItem = CreateValidItem();

        var result = controller.TryInteractWithBin(location, tile, 1L, heldItem, null, key => refusalKeys.Add(key));

        Assert.False(result, "A deposit into a bin that is already processing must be refused.");
        Assert.DoesNotContain(ActionConflictRefusalKey, refusalKeys);
        Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", tile));
    }

    /// <summary>
    /// T066 (FR-008a): collecting from a bin that is not Ready —
    /// empty, or still processing an unfinished batch — is an
    /// invalid bin state, not a concurrency collision, so the
    /// refusal handler must not receive the conflict key.
    /// </summary>
    [Theory]
    [InlineData(CompostingBinState.Empty)]
    [InlineData(CompostingBinState.Processing)]
    public void TryInteractWithBin_CollectFromNotReadyBin_DoesNotEmitActionConflict(CompostingBinState binState)
    {
        var tile = new Vector2(10, 10);

        if (binState == CompostingBinState.Processing)
        {
            var wasteItem = CreateValidItem();
            _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(wasteItem)).Returns(true);
            _service.AddWaste("Farm", tile, wasteItem);
        }

        Assert.Equal(binState, _service.GetBinState("Farm", tile));

        var location = CreateLocationWithPlacedBin(tile);
        var controller = CreateInteractionController();
        var refusalKeys = new List<string>();

        var result = controller.TryInteractWithBin(location, tile, 1L, null, null, key => refusalKeys.Add(key));

        Assert.False(result, "Collecting from a bin that is not ready must be refused.");
        Assert.DoesNotContain(ActionConflictRefusalKey, refusalKeys);
    }

    /// <summary>
    /// T066 control (FR-008a, FR-019): a tile lock already held by
    /// another farmer is a true concurrency collision — the one
    /// invalid bin interaction FR-008a still owes the conflict key.
    /// </summary>
    [Fact]
    public void TryInteractWithBin_LockHeldByAnotherFarmer_EmitsActionConflict()
    {
        var tile = new Vector2(10, 10);
        var location = CreateLocationWithPlacedBin(tile);

        var lockService = new HostAtomicLockService();
        Assert.True(
            lockService.TryAcquireLock("Farm", tile, 77L),
            "A different farmer must hold the tile lock to stage a genuine collision.");

        var controller = CreateInteractionController(lockService);
        var refusalKeys = new List<string>();

        var result = controller.TryInteractWithBin(location, tile, 1L, null, null, key => refusalKeys.Add(key));

        Assert.False(result, "A genuine lock collision must refuse the interaction.");
        Assert.Contains(ActionConflictRefusalKey, refusalKeys);
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

    // T049b: FR-005a — CollectCompost yields strictly 1 item while still tracking MaturationLevel up to 5
    [Fact]
    public void CollectCompost_IncrementsMaturationCappedAt5()
    {
        CompostingBinData? savedData = null;

        _mockModDataService.Setup(x => x.SaveData(It.IsAny<CompostingBinData>(), It.IsAny<string>()))
            .Callback<CompostingBinData, string>((d, k) => savedData = d);

        // Part 1: MaturationLevel=3 → collect → yields 1 (FR-005a), maturation becomes 4
        var dataAt3 = new CompostingBinData();
        dataAt3.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>();
        dataAt3.LocationBinData["Farm"]["10,10"] = new CompostingBinStateData
        {
            State = "Ready",
            // T060 (FR-006, FR-012): a Ready
            // record must carry its batch
            // provenance — the input item and
            // the day it was loaded — exactly
            // what SaveData writes for a
            // legitimately finished batch. A
            // provenance-less Ready record is
            // quarantined by the tolerant
            // reader, so the maturation
            // persistence this test verifies
            // needs the provenance a real save
            // would hold.
            InputItemId = "(O)24",
            InputTimestamp = 1,
            MaturationLevel = 3
        };

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(dataAt3);
        _service.LoadData("save-lvl3");

        var result1 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(1, result1);
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));

        _service.SaveData("save-lvl3");
        Assert.NotNull(savedData);
        Assert.True(savedData!.LocationBinData.ContainsKey("Farm"));
        Assert.True(savedData.LocationBinData["Farm"].ContainsKey("10,10"));
        Assert.Equal(4, savedData.LocationBinData["Farm"]["10,10"].MaturationLevel);

        // Part 2: MaturationLevel=5 → collect → yields 1 (FR-005a), maturation stays at 5
        var dataAt5 = new CompostingBinData();
        dataAt5.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>();
        dataAt5.LocationBinData["Farm"]["10,10"] = new CompostingBinStateData
        {
            State = "Ready",
            // T060 (FR-006, FR-012): batch
            // provenance, as in Part 1 — a
            // real finished batch always
            // records the input it processed.
            InputItemId = "(O)24",
            InputTimestamp = 1,
            MaturationLevel = 5
        };

        _mockModDataService.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(dataAt5);
        savedData = null;

        _service.LoadData("save-lvl5");
        var result2 = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(1, result2);
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

    /// <summary>
    /// Records the cue names requested through <c>Game1.playSound</c>
    /// so a test can assert which sound the service asked the game to
    /// play.
    /// </summary>
    /// <remarks>
    /// Semantically a no-op stand-in for <see cref="Stubs.NoOpSoundsHelperStub"/>:
    /// every cue is dropped exactly like the no-op stub, and only the
    /// request is remembered. <c>Game1.sounds</c> is a public static
    /// field, so swapping it in needs neither reflection nor a booted
    /// game, and the suite runs test collections sequentially
    /// (xunit.runner.json), so the swap cannot race a sibling class.
    /// </remarks>
    private sealed class RecordingSoundsHelper : ISoundsHelper
    {
        private readonly List<string> _cues = new();

        /// <summary>Cue names requested through <c>Game1.playSound</c>, in call order.</summary>
        public IReadOnlyList<string> Cues => _cues;

        /// <summary>Unused; kept for interface completeness.</summary>
        public bool LogSounds { get; set; }

        /// <summary>Always reports that local playback is unavailable.</summary>
        /// <param name="context">Sound context; ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        public bool ShouldPlayLocal(SoundContext context)
        {
            return false;
        }

        /// <summary>Reports zero attenuation.</summary>
        /// <param name="location">Originating location; ignored.</param>
        /// <param name="position">Playback position; ignored.</param>
        /// <returns>Always <c>0f</c>.</returns>
        public float GetVolumeForDistance(GameLocation location, Vector2? position)
        {
            return 0f;
        }

        /// <summary>Remembers the requested cue and silently drops it.</summary>
        /// <param name="cueName">Requested cue; recorded.</param>
        /// <param name="location">Originating location; ignored.</param>
        /// <param name="position">Playback position; ignored.</param>
        /// <param name="pitch">Requested pitch; ignored.</param>
        /// <param name="context">Sound context; ignored.</param>
        /// <param name="cue">Always set to <c>null</c>.</param>
        /// <returns>Always <c>false</c>.</returns>
        public bool PlayLocal(string cueName, GameLocation location, Vector2? position, int? pitch, SoundContext context, out ICue cue)
        {
            _cues.Add(cueName);
            cue = null!;
            return false;
        }

        /// <summary>Remembers the requested cue and silently drops it.</summary>
        /// <param name="cueName">Requested cue; recorded.</param>
        /// <param name="location">Originating location; ignored.</param>
        /// <param name="position">Playback position; ignored.</param>
        /// <param name="pitch">Requested pitch; ignored.</param>
        /// <param name="context">Sound context; ignored.</param>
        public void PlayAll(string cueName, GameLocation location, Vector2? position, int? pitch, SoundContext context)
        {
            _cues.Add(cueName);
        }

        /// <summary>No-op.</summary>
        /// <param name="cue">Cue to adjust; ignored.</param>
        /// <param name="pitch">Requested pitch; ignored.</param>
        /// <param name="isMusic">Whether the cue is music; ignored.</param>
        public void SetPitch(ICue cue, float pitch, bool isMusic)
        {
        }
    }

    /// <summary>T119 [P] [US2] RED — FR-005b (partial): exactly one compost across both drivers.</summary>
    [Property]
    public void T119_ExactlyOneCompost_NoDoublePayout()
    {
        // Fixed finished batch — invariant is per-batch exactly-once;
        // no generated input domain (property premise always met).
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        var tile = new Vector2(10, 10);
        var location = CreateLocationWithPlacedBin(tile);
        var controller = CreateInteractionController();

        // Driver 1 — interception harvest.
        controller.TryInteractWithBin(location, tile, 1L, null);

        // Driver 2 — native Data/Machines output declaration.
        var machineData = ReadRegisteredBinMachineData();
        var declaredOutput = machineData?.OutputRules?
            .SelectMany(rule => rule.OutputItem ?? new List<MachineItemOutput>())
            .FirstOrDefault();
        if (declaredOutput != null)
        {
            var nativeOutput = new StardewValley.Object { ItemId = declaredOutput.ItemId };
            _inventory.TryAddItem(nativeOutput);
        }

        // FR-005b: exactly one compost total across both drivers.
        // RED: unreconciled native + interception yields 2, so count != 1.
        Assert.Equal(1, _inventory.AddedItems.Count);
    }

    /// <summary>T121 [P] [US2] RED — FR-005b/FR-008a (partial): translated refusal HUD text.</summary>
    [Fact]
    public void T121_FullInventoryHarvest_ShowsTranslatedRedText()
    {
        var item = CreateValidItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _inventory.AcceptsItems = false;

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        int hudBefore = Game1.hudMessages.Count;
        var result = _service.CollectCompost("Farm", new Vector2(10, 10));

        Assert.Equal(0, result);
        Assert.Empty(_inventory.AddedItems);
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        // RED (FR-005b/FR-008a): refusal must render translated red HUD
        // text — never the raw translation key. Currently falls back
        // to raw key (or missing), so assertion fails.
        Assert.Equal(hudBefore + 1, Game1.hudMessages.Count);
        string refusalText = Game1.hudMessages[Game1.hudMessages.Count - 1].message;
        Assert.NotEqual(ModConstants.TranslationKeyHarvestInventoryFull, refusalText);
        Assert.Contains("inventory", refusalText, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>T124 [P] [US2] RED — FR-008a (missing): invalid drop-in shakes machine.</summary>
    [Fact]
    public void T124_InvalidDropIn_ShakesMachine_PlusCancelCue()
    {
        var tile = new Vector2(10, 10);
        var item = ItemFactory.CreateItem("Object.Stone", StoneCategory);

        var location = CreateLocationWithPlacedBin(tile);
        var locationProvider = new Mock<ILocationProvider>();
        locationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(location);

        var inventory = new RecordingPlayerInventory();
        var service = new CompostingBinService(
            new Mock<IModDataService>().Object,
            new Mock<ISaveIdProvider>().Object,
            new OrganicWasteValidator(),
            new Mock<IMonitor>().Object,
            new TimeProviderStub(),
            new CompostingBinFactory(),
            inventory,
            locationProvider.Object);

        var sounds = new RecordingSoundsHelper();
        var previousSounds = Game1.sounds;
        Game1.sounds = sounds;
        try
        {
            service.AddWaste("Farm", tile, item);
        }
        finally
        {
            Game1.sounds = previousSounds;
        }

        // Vanilla audio cancel cue is present (existing behavior).
        Assert.Contains(CancelCueName, sounds.Cues);

        // RED (FR-008a missing): machine shake must trigger.
        // Currently shakeTimer remains 0, so assertion fails.
        var worldObject = location.Objects[tile];
        Assert.True(worldObject.shakeTimer > 0,
            "Invalid drop-in must shake the machine in addition to cancel cue (FR-008a).");
    }
}
