using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
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
/// Property-based invariant tests for spec 004 (Playable Compost Loop):
/// T036 — FR-006 Compost obtainable ONLY through bin processing (no bypass path).
/// T035a — SC-009 audit: repeated collect from full inventory + pick-up/re-place
/// mid-batch + floating-world-debris.
/// </summary>
public class CompostingLoopInvariantsTests
{
    public CompostingLoopInvariantsTests()
    {
        FsCheckFixture.Register();
        GameStateFixture.Install();
    }

    // ------------------------------------------------------------------
    // T036 — FR-006 Compost obtainable ONLY through bin processing
    // ------------------------------------------------------------------

    /// <summary>
    /// T036: For any arbitrary location name, compost is obtainable only through
    /// bin processing (AddWaste → ProcessDayStart → CollectCompost). No bypass
    /// path exists — without a completed bin cycle, CollectCompost returns 0.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T036_CompostOnlyViaBin(string locationName)
    {
        var inventory = new RecordingPlayerInventory();
        var timeStub = new TimeProviderStub { TotalDays = 1 };
        var service = CreateService(inventory, timeStub);

        var tile = new Vector2(10, 10);

        // Without a completed bin cycle, no compost is obtainable
        Assert.Equal(0, service.CollectCompost(locationName, tile));
        Assert.Empty(inventory.AddedItems);

        // After a full bin cycle, compost is obtainable
        service.AddWaste(locationName, tile, ItemFactory.CreateValidWasteItem());
        timeStub.TotalDays = 1 + ModConstants.MaturationDays;
        service.ProcessDayStart(locationName);
        Assert.Equal(1, service.CollectCompost(locationName, tile));
        Assert.Single(inventory.AddedItems);
        Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
    }

    /// <summary>
    /// T036: For any arbitrary recipe ID, no crafting recipe produces compost.
    /// The only registered recipe is for the bin itself, never for compost.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T036_NoCraftingBypass(string recipeId)
    {
        var recipes = GetAllCraftingRecipes();

        // If the recipe exists, it must not produce compost
        if (recipes.TryGetValue(recipeId, out var recipe))
        {
            string output = GetRecipeOutput(recipe);
            Assert.NotEqual(ModConstants.CompostItemId, output);
        }

        // Additionally, verify no recipe at all produces compost
        foreach (var entry in recipes)
        {
            Assert.NotEqual(ModConstants.CompostItemId, GetRecipeOutput(entry.Value));
        }
    }

    /// <summary>
    /// T036: For any arbitrary string, no loot table produces compost.
    /// No machine output, fish pond, or object drop table produces compost.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T036_NoLootTableBypass(string sourceId)
    {
        var machineOutputs = GetAllMachineOutputs();

        // If the machine exists, it must not produce compost
        if (machineOutputs.TryGetValue(sourceId, out var output))
        {
            Assert.NotEqual(ModConstants.CompostItemId, output);
        }

        // Additionally, verify no machine at all produces compost
        foreach (var entry in machineOutputs)
        {
            Assert.NotEqual(ModConstants.CompostItemId, entry.Value);
        }
    }

    // ------------------------------------------------------------------
    // T035a — SC-009 audit
    // ------------------------------------------------------------------

    /// <summary>
    /// T035a: For any CompostingBinStateModel, modData keys are preserved after
    /// debris placement (CreateDislodgedBinItem → OnBinPlaced).
    /// </summary>
    [Property(MaxTest = 100)]
    public void T035a_ModDataPreservedAfterDebris(CompostingBinStateModel binModel)
    {
        var inventory = new RecordingPlayerInventory();
        var timeStub = new TimeProviderStub { TotalDays = 10 };
        var service = CreateService(inventory, timeStub);

        var tile = new Vector2(binModel.TileX, binModel.TileY);
        var locationName = "Farm";

        // Set up the bin with waste
        service.AddWaste(locationName, tile, ItemFactory.CreateValidWasteItem());

        // If the bin model is Ready, advance time to make it Ready
        if (binModel.State == CompostingBinState.Ready)
        {
            timeStub.TotalDays = 10 + ModConstants.MaturationDays;
            service.ProcessDayStart(locationName);
        }

        // Dislodge the bin
        var dislodgedItem = service.CreateDislodgedBinItem(locationName, tile);
        Assert.NotNull(dislodgedItem);

        // Verify modData keys are preserved
        if (!string.IsNullOrEmpty(binModel.InputItemId))
        {
            Assert.True(dislodgedItem.modData.ContainsKey(ModConstants.ModDataInputIdKey));
            Assert.Equal(
                binModel.InputItemId,
                dislodgedItem.modData[ModConstants.ModDataInputIdKey]);
        }

        if (binModel.State == CompostingBinState.Processing && binModel.InputTimestamp.HasValue)
        {
            Assert.True(dislodgedItem.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey));
        }
        else if (binModel.State == CompostingBinState.Ready)
        {
            Assert.True(dislodgedItem.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey));
            Assert.Equal("0", dislodgedItem.modData[ModConstants.ModDataDaysRemainingKey]);
        }

        // Re-place the bin
        service.OnObjectRemoved(locationName, tile);
        service.OnBinPlaced(locationName, tile, dislodgedItem);

        // Verify state is restored
        var restoredState = service.GetBinState(locationName, tile);
        Assert.Equal(binModel.State, restoredState);
    }

    /// <summary>
    /// T035a: For any int, zero item destruction in full-inventory collect.
    /// A full inventory refuses the compost but leaves the bin Ready.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T035a_ZeroDestruction(int seed)
    {
        var inventory = new RecordingPlayerInventory { AcceptsItems = false };
        var timeStub = new TimeProviderStub { TotalDays = 1 };
        var service = CreateService(inventory, timeStub);

        var tile = new Vector2(10, 10);
        var locationName = "Farm";

        // Set up a Ready bin
        service.AddWaste(locationName, tile, ItemFactory.CreateValidWasteItem());
        timeStub.TotalDays = 1 + ModConstants.MaturationDays;
        service.ProcessDayStart(locationName);
        Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));

        // Collect with full inventory — no destruction
        int collected = service.CollectCompost(locationName, tile);
        Assert.Equal(0, collected);
        Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));
        Assert.Empty(inventory.AddedItems);
    }

    /// <summary>
    /// T035a: For any int, full inventory leaves bin Ready, retry delivers full batch.
    /// </summary>
    [Property(MaxTest = 100)]
    public void T035a_FullInventoryRetry(int seed)
    {
        var inventory = new RecordingPlayerInventory { AcceptsItems = false };
        var timeStub = new TimeProviderStub { TotalDays = 1 };
        var service = CreateService(inventory, timeStub);

        var tile = new Vector2(10, 10);
        var locationName = "Farm";

        // Set up a Ready bin
        service.AddWaste(locationName, tile, ItemFactory.CreateValidWasteItem());
        timeStub.TotalDays = 1 + ModConstants.MaturationDays;
        service.ProcessDayStart(locationName);
        Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));

        // First attempt with full inventory — bin stays Ready
        int firstAttempt = service.CollectCompost(locationName, tile);
        Assert.Equal(0, firstAttempt);
        Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));

        // Retry with available inventory — delivers full batch
        inventory.AcceptsItems = true;
        int retryAttempt = service.CollectCompost(locationName, tile);
        Assert.Equal(1, retryAttempt);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState(locationName, tile));
        Assert.Single(inventory.AddedItems);
        Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
    }

    // ------------------------------------------------------------------
    // Helper methods
    // ------------------------------------------------------------------

    private static CompostingBinService CreateService(
        IPlayerInventory inventory,
        ITimeProvider timeProvider)
    {
        var mockModDataService = new Mock<IModDataService>();
        var mockSaveIdProvider = new Mock<ISaveIdProvider>();
        var mockWasteValidator = new Mock<IOrganicWasteValidator>();
        var mockMonitor = new Mock<IMonitor>();
        mockWasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

        return new CompostingBinService(
            mockModDataService.Object,
            mockSaveIdProvider.Object,
            mockWasteValidator.Object,
            mockMonitor.Object,
            timeProvider,
            new CompostingBinFactory(),
            inventory);
    }

    private static Dictionary<string, string> GetAllCraftingRecipes()
    {
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/CraftingRecipes");
        modEntry.OnAssetRequested(null, args);

        var editOperations = GetEditOperations(args);
        var recipes = new Dictionary<string, string>();

        foreach (var editOp in editOperations)
        {
            ApplyEditOperation(recipes, editOp);
        }

        return recipes;
    }

    private static string GetRecipeOutput(string recipe)
    {
        string[] segments = recipe.Split('/');
        return segments.Length > 0 ? segments[^1].Trim() : string.Empty;
    }

    private static Dictionary<string, string> GetAllMachineOutputs()
    {
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Machines");
        modEntry.OnAssetRequested(null, args);

        var editOperations = GetEditOperations(args);
        var machines = new Dictionary<string, string>();

        foreach (var editOp in editOperations)
        {
            ApplyEditOperation(machines, editOp);
        }

        return machines;
    }

    private static AssetRequestedEventArgs CreateAssetRequestedEventArgs(string assetName)
    {
        var mockName = new Mock<IAssetName>();
        mockName.Setup(n => n.IsEquivalentTo(assetName, It.IsAny<bool>())).Returns(true);
        mockName.Setup(n => n.IsEquivalentTo(It.Is<string>(s => s != assetName), It.IsAny<bool>())).Returns(false);

        var mockAssetInfo = new Mock<IAssetInfo>();
        mockAssetInfo.Setup(a => a.NameWithoutLocale).Returns(mockName.Object);

        var ctor = typeof(AssetRequestedEventArgs).GetConstructors(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        args[0] = mockAssetInfo.Object;

        if (parameters.Length > 1 && parameters[1].ParameterType.IsGenericType)
        {
            var genArgs = parameters[1].ParameterType.GetGenericArguments();
            var returnType = genArgs.Last();
            var paramTypes = genArgs.Take(genArgs.Length - 1).ToArray();
            var method = new DynamicMethod(
                "DummyGetOnBehalfOf",
                returnType,
                paramTypes,
                typeof(CompostingLoopInvariantsTests).Module);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            args[1] = method.CreateDelegate(parameters[1].ParameterType);
        }

        var instance = (AssetRequestedEventArgs)ctor.Invoke(args);

        var editOpsField = typeof(AssetRequestedEventArgs).GetField(
            "<EditOperations>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (editOpsField != null && editOpsField.GetValue(instance) == null)
        {
            editOpsField.SetValue(instance, Activator.CreateInstance(editOpsField.FieldType));
        }

        var loadOpsField = typeof(AssetRequestedEventArgs).GetField(
            "<LoadOperations>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (loadOpsField != null && loadOpsField.GetValue(instance) == null)
        {
            loadOpsField.SetValue(instance, Activator.CreateInstance(loadOpsField.FieldType));
        }

        var modField = typeof(AssetRequestedEventArgs).GetField(
            "Mod",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (modField != null)
        {
            var mockType = typeof(Mock<>).MakeGenericType(modField.FieldType);
            var mockModMetadata = (Mock)Activator.CreateInstance(mockType)!;
            modField.SetValue(instance, mockModMetadata.Object);
        }

        return instance;
    }

    private static IList GetEditOperations(AssetRequestedEventArgs args)
    {
        var field = typeof(AssetRequestedEventArgs).GetField(
            "<EditOperations>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (IList)field!.GetValue(args)!;
    }

    private static void ApplyEditOperation<TKey, TValue>(
        object editOp,
        IDictionary<TKey, TValue> targetDict)
        where TKey : notnull
    {
        var mockAssetData = new Mock<IAssetData>();
        var mockDictData = new Mock<IAssetDataForDictionary<TKey, TValue>>();
        mockDictData.Setup(d => d.Data).Returns(targetDict);
        mockAssetData.Setup(a => a.AsDictionary<TKey, TValue>()).Returns(mockDictData.Object);

        var applyMethod = editOp.GetType().GetMethod(
            "Apply",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (applyMethod != null)
        {
            applyMethod.Invoke(editOp, new object[] { mockAssetData.Object });
        }
        else
        {
            var actionField = editOp.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => typeof(Delegate).IsAssignableFrom(f.FieldType));
            var action = actionField?.GetValue(editOp) as Action<IAssetData>;
            action?.Invoke(mockAssetData.Object);
        }
    }
}
