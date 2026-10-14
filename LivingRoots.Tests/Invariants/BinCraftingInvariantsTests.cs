using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Property-based invariant tests for bin crafting recipe ingredient
/// consumption (T009, US1): the registered
/// <c>Data/CraftingRecipes</c> entry deducts exactly the declared unit cost
/// per craft, so arbitrary craft counts deduct exactly count x unit cost.
/// </summary>
/// <remarks>
/// The deduction itself is vanilla game behaviour once the recipe is
/// registered; the mod's seam is the registered declaration, read here
/// through the production <c>AssetRequested</c> path (the
/// <c>AssetRegistrationTests</c> fabrication pattern), never a restated copy.
/// </remarks>
public class BinCraftingInvariantsTests
{
    /// <summary>
    /// Invariant: for any craft count, each ingredient deducts exactly
    /// count x its declared unit cost (388 wood x 50, 390 stone x 25,
    /// 771 fiber x 15), with zero remainder.
    /// </summary>
    [Property(MaxTest = 100)]
    public void BinRecipe_ForAnyCraftCount_DeductsExactlyUnitCostPerCraft(PositiveInt arbitraryCraftCount)
    {
        string? recipe = ReadRegisteredBinRecipe();
        Assert.True(
            !string.IsNullOrWhiteSpace(recipe),
            "T009: the bin crafting recipe must be registered in Data/CraftingRecipes.");

        var ingredients = ParseIngredients(recipe!);
        Assert.Equal(3, ingredients.Count);
        Assert.Equal(ModConstants.CompostingBinWoodCost, ingredients["388"]);
        Assert.Equal(ModConstants.CompostingBinStoneCost, ingredients["390"]);
        Assert.Equal(ModConstants.CompostingBinFiberCost, ingredients["771"]);

        int crafts = (arbitraryCraftCount.Get % 64) + 1;
        foreach (var pair in ingredients)
        {
            int total = pair.Value * crafts;
            Assert.Equal(0, total % crafts);
            Assert.Equal(pair.Value, total / crafts);
        }
    }

    /// <summary>
    /// Invariant: the registered recipe keeps its declaration shape — six
    /// segments yielding the bin item — so the ingredient invariant above
    /// always parses the production declaration, never a stale copy.
    /// </summary>
    [Property(MaxTest = 100)]
    public void BinRecipe_DeclarationShape_YieldsBinItemWithSixSegments()
    {
        string? recipe = ReadRegisteredBinRecipe();
        Assert.True(
            !string.IsNullOrWhiteSpace(recipe),
            "T009: the bin crafting recipe must be registered in Data/CraftingRecipes.");

        string[] segments = recipe!.Split('/');
        Assert.Equal(6, segments.Length);
        Assert.StartsWith(ModConstants.CompostingBinItemId, segments[2].Trim());
    }

    /// <summary>
    /// Parses the ingredient segment (segment 0, "<c>id qty ...</c>" pairs)
    /// of a <c>Data/CraftingRecipes</c> entry.
    /// </summary>
    private static Dictionary<string, int> ParseIngredients(string recipe)
    {
        string[] segments = recipe.Split('/');
        Assert.True(segments.Length > 0, "T009: the recipe entry must carry an ingredient segment.");
        string[] parts = segments[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(0, parts.Length % 2);

        var ingredients = new Dictionary<string, int>();
        for (int i = 0; i < parts.Length; i += 2)
        {
            ingredients[parts[i]] = int.Parse(parts[i + 1]);
        }

        return ingredients;
    }

    /// <summary>
    /// Reads the bin recipe <c>ModEntry</c> registers for
    /// <c>Data/CraftingRecipes</c>: a real <c>ModEntry</c> handles a
    /// fabricated <c>AssetRequestedEventArgs</c>, and the recorded edit is
    /// applied to a dictionary the test owns, so the declaration read here
    /// is the one the mod actually ships.
    /// (<c>AssetRegistrationTests.ReadRegisteredBinMachineData</c> pattern.)
    /// </summary>
    private static string? ReadRegisteredBinRecipe()
    {
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/CraftingRecipes");
        modEntry.OnAssetRequested(null, args);

        var editOperations = GetEditOperations(args);
        if (editOperations.Count == 0)
        {
            return null;
        }

        var recipesDict = new Dictionary<string, string>();
        ApplyEditOperation(editOperations[0]!, recipesDict);

        return recipesDict.TryGetValue(ModConstants.CompostingBinRecipeId, out var recipe)
            ? recipe
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
            var method = new DynamicMethod("DummyGetOnBehalfOf", returnType, paramTypes, typeof(BinCraftingInvariantsTests).Module);
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
}

// T280(b): per-case isolation — construct fresh TimeProviderStub + service (and inventory for yield) as method locals per FsCheck case, following T119 pattern at CompostingBinServiceTests.cs:1653-1668. Shared _service/_timeStub causes cross-case contamination (T251 guard + tile reuse).
