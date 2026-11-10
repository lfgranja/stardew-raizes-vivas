using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using LivingRoots;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Spec 020 registration contract tests (FR-001..FR-005, US1 acquisition path).
/// </summary>
/// <remarks>
/// TDD-red first: these tests pin the crioulo seed/produce/crop/traveler-shop
/// contract before <c>ModEntry.OnAssetRequested</c> grows the new branches.
/// They mirror the <see cref="AgrochemicalRegistrationTests"/> /
/// <see cref="AssetRegistrationTests"/> patterns: drive <c>OnAssetRequested</c>
/// through the reflection seam, then apply the edit operations against
/// in-memory dictionaries. See
/// <c>specs/020-heirloom-seeds-pipeline/contracts/heirloom-seed-registration.md</c>.
/// </remarks>
public class HeirloomRegistrationTests
{
    // ------------------------------------------------------------------
    // Constants sanity (guards against accidental ID renames)
    // ------------------------------------------------------------------

    [Fact]
    public void Constants_HeirloomIdsAndPricesAreAsSpecified()
    {
        Assert.Equal("LivingRoots.HeirloomParsnipSeeds", ModConstants.HeirloomItemId);
        Assert.Equal("(O)LivingRoots.HeirloomParsnipSeeds", ModConstants.QualifiedHeirloomItemId);
        Assert.Equal("LivingRoots.HeirloomParsnip", ModConstants.HeirloomProduceItemId);
        Assert.Equal(35, ModConstants.HeirloomSeedPrice);
        Assert.Equal(45, ModConstants.HeirloomProducePrice);
        Assert.Equal(2f, ModConstants.HeirloomPlantBonus);
        Assert.Equal(1f, ModConstants.HeirloomHarvestBonus);
        Assert.Equal(1, ModConstants.HeirloomSavedSeedYield);
        Assert.Equal("heirloom_seed", ModConstants.HeirloomSeedTag);
        Assert.Equal("heirloom_produce", ModConstants.HeirloomProduceTag);
    }

    [Fact]
    public void Constants_AssetNamesFollowModsConvention()
    {
        Assert.Equal("Mods/lfgranja.LivingRoots/HeirloomSeeds", ModConstants.HeirloomSeedsTextureAssetName);
        Assert.Equal("Mods/lfgranja.LivingRoots/HeirloomProduce", ModConstants.HeirloomProduceTextureAssetName);
        Assert.Equal("Mods/lfgranja.LivingRoots/HeirloomCrops", ModConstants.HeirloomCropsTextureAssetName);
    }

    // ------------------------------------------------------------------
    // Data/Objects — seed + produce entries
    // ------------------------------------------------------------------

    [Fact]
    public void OnAssetRequested_Objects_RegistersHeirloomSeedEntryWithSeedsTypeNegative74()
    {
        var dict = ApplyObjectsEdit();
        Assert.True(dict.TryGetValue(ModConstants.HeirloomItemId, out var entry), "seed entry must be keyed by the unqualified ID");
        Assert.Equal("Seeds", entry.Type);
        Assert.Equal(-74, entry.Category);
        Assert.Equal(ModConstants.HeirloomSeedPrice, entry.Price);
        Assert.Equal(ModConstants.HeirloomSeedsTextureAssetName, entry.Texture);
        Assert.Equal(0, entry.SpriteIndex);
        var tags = entry.ContextTags ?? new List<string>();
        Assert.Contains(ModConstants.HeirloomSeedTag, tags);
        Assert.DoesNotContain("compostable_item", tags);
    }

    [Fact]
    public void OnAssetRequested_Objects_RegistersHeirloomProduceEntryVegetableAndCompostable()
    {
        var dict = ApplyObjectsEdit();
        Assert.True(dict.TryGetValue(ModConstants.HeirloomProduceItemId, out var entry), "produce entry must be keyed by the unqualified ID");
        Assert.Equal("Basic", entry.Type);
        Assert.Equal(-75, entry.Category);
        Assert.Equal(ModConstants.HeirloomProducePrice, entry.Price);
        Assert.True(entry.ContextTags != null && entry.ContextTags.Contains("compostable_item"), "produce must carry compostable_item (FR-010)");
        Assert.True(entry.ContextTags != null && entry.ContextTags.Contains(ModConstants.HeirloomProduceTag), "produce must carry heirloom_produce");
    }

    [Fact]
    public void OnAssetRequested_Objects_ExactlyTwoNewHeirloomEntriesAndNoCollisionWithVanillaKeys()
    {
        var dict = ApplyObjectsEdit();
        var heirloomKeys = dict.Keys
            .Where(k => k.Contains("Heirloom", StringComparison.Ordinal))
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, heirloomKeys.Length);
        Assert.All(heirloomKeys, k => Assert.StartsWith("LivingRoots.Heirloom", k, StringComparison.Ordinal));
    }

    [Fact]
    public void OnAssetRequested_Objects_SeedDisplayNameAndDescriptionRouteThroughTranslationKeys()
    {
        var modEntry = new ModEntry();
        SetHelper(modEntry, CreateStubHelperWithTranslation());
        var args = CreateAssetRequestedEventArgs("Data/Objects");
        modEntry.OnAssetRequested(null, args);
        var dict = ApplyEditOperations(args);
        var entry = dict[ModConstants.HeirloomItemId];
        Assert.Equal(TranslationMarker + ModConstants.HeirloomSeedNameKey, entry.DisplayName);
        Assert.Equal(TranslationMarker + ModConstants.HeirloomSeedDescriptionKey, entry.Description);
        var produce = dict[ModConstants.HeirloomProduceItemId];
        Assert.Equal(TranslationMarker + ModConstants.HeirloomProduceNameKey, produce.DisplayName);
        Assert.Equal(TranslationMarker + ModConstants.HeirloomProduceDescriptionKey, produce.Description);
    }

    // ------------------------------------------------------------------
    // Data/Crops
    // ------------------------------------------------------------------

    [Fact]
    public void OnAssetRequested_Crops_RegistersEntryKeyedBySeedIdWithValidInvariants()
    {
        var dict = ApplyCropsEdit();
        Assert.True(dict.TryGetValue(ModConstants.HeirloomItemId, out var crop), "Data/Crops key must equal the seed item ID (1.6 contract)");
        Assert.Equal(new List<string> { "spring" }, crop.Seasons.Select(s => s.ToString().ToLowerInvariant()).ToList());
        Assert.Equal(new List<int> { 1, 2, 2, 2 }, crop.DaysInPhase.ToList());
        Assert.Equal(7, crop.DaysInPhase.Sum());
        Assert.Equal(-1, crop.RegrowDays);
        Assert.False(crop.IsRaised);
        Assert.True(crop.NeedsWatering);
        Assert.Equal(ModConstants.HeirloomProduceItemId, crop.HarvestItemId);
        Assert.Equal(1, crop.HarvestMinStack);
        Assert.Equal(1, crop.HarvestMaxStack);
        Assert.Equal(StardewValley.GameData.Crops.HarvestMethod.Grab, crop.HarvestMethod);
        Assert.Equal(ModConstants.HeirloomCropsTextureAssetName, crop.Texture);
        Assert.Equal(0, crop.SpriteIndex);
        Assert.True(crop.CountForMonoculture);
        Assert.True(crop.CountForPolyculture);
    }

    // ------------------------------------------------------------------
    // Data/Shops — Traveler entry; SeedShop must NOT be edited
    // ------------------------------------------------------------------

    private static Dictionary<string, ShopData> BuildVanillaLikeShopsDict()
    {
        return new Dictionary<string, ShopData>(StringComparer.Ordinal)
        {
            ["Traveler"] = new ShopData
            {
                Items = new List<ShopItemData>
                {
                    new ShopItemData { Id = "(O)24", ItemId = "(O)24", Price = 30, AvailableStock = 5 },
                    new ShopItemData { Id = "(O)495", ItemId = "(O)495", Price = 50, AvailableStock = 1 }
                }
            },
            ["SeedShop"] = new ShopData
            {
                Items = new List<ShopItemData>
                {
                    new ShopItemData { Id = "(O)472", ItemId = "(O)472", Price = 20, AvailableStock = -1 }
                }
            }
        };
    }

    [Fact]
    public void OnAssetRequested_Shops_TravelerEntryIsAppendedPlayerLimitedAndSpringGatedAndExistingStockPreserved()
    {
        var dict = ApplyShopsEdit(BuildVanillaLikeShopsDict());
        var traveler = dict["Traveler"];
        var seedEntry = traveler.Items.SingleOrDefault(i => string.Equals(i.Id, ModConstants.HeirloomItemId, StringComparison.Ordinal));
        Assert.NotNull(seedEntry);
        Assert.Equal(StardewValley.GameData.Shops.LimitedStockMode.Player, seedEntry.AvailableStockLimit);
        Assert.False(string.IsNullOrWhiteSpace(seedEntry.Condition), "FR-003: the Traveler entry must carry an explicit condition (SEASON Spring) or the fallback must be disclosed in test evidence");
        Assert.Equal(3, traveler.Items.Count); // two vanilla-like entries preserved + heirloom appended
    }

    [Fact]
    public void OnAssetRequested_Shops_SeedShopIsNeverModified()
    {
        var dict = ApplyShopsEdit(BuildVanillaLikeShopsDict());
        var seedShop = dict["SeedShop"];
        Assert.Single(seedShop.Items);
        Assert.Equal("(O)472", seedShop.Items[0].ItemId);
    }

    [Fact]
    public void OnAssetRequested_Shops_TravelerItemIdResolvesToQualifiedHeirloomSeed()
    {
        var dict = ApplyShopsEdit(BuildVanillaLikeShopsDict());
        var seedEntry = dict["Traveler"].Items.Single(i => string.Equals(i.Id, ModConstants.HeirloomItemId, StringComparison.Ordinal));
        Assert.Equal(ModConstants.QualifiedHeirloomItemId, seedEntry.ItemId);
    }

    // ------------------------------------------------------------------
    // Test seam and factories (mirrors AgrochemicalRegistrationTests)
    // ------------------------------------------------------------------

    private const string TranslationMarker = "T-Heirloom:";


    private static Dictionary<string, ObjectData> ApplyObjectsEdit()
    {
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Objects");
        modEntry.OnAssetRequested(null, args);
        return ApplyEditOperationsTyped<ObjectData>(args);
    }

    private static Dictionary<string, CropData> ApplyCropsEdit()
    {
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Crops");
        modEntry.OnAssetRequested(null, args);
        return ApplyEditOperationsTyped<CropData>(args);
    }

    private static Dictionary<string, ShopData> ApplyShopsEdit(Dictionary<string, ShopData> seedDict)
    {
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Shops");
        modEntry.OnAssetRequested(null, args);
        var editOperations = GetEditOperations(args).Cast<object>().ToList();
        foreach (var op in editOperations)
        {
            ApplyDictionaryEditOperationTyped(op, seedDict);
        }

        return seedDict;
    }

    private static void ApplyDictionaryEditOperationTyped<TValue>(object editOp, IDictionary<string, TValue> targetDict)
        where TValue : notnull
    {
        var mockAssetData = new Mock<IAssetData>();
        var mockDictData = new Mock<IAssetDataForDictionary<string, TValue>>();
        mockDictData.Setup(d => d.Data).Returns(targetDict);
        mockAssetData.Setup(a => a.AsDictionary<string, TValue>()).Returns(mockDictData.Object);

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

    private static Dictionary<string, TValue> ApplyEditOperationsTyped<TValue>(AssetRequestedEventArgs args)
        where TValue : notnull
    {
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var dict = new Dictionary<string, TValue>();
        foreach (var op in editOperations)
        {
            ApplyDictionaryEditOperationTyped(op!, dict);
        }

        return dict;
    }

    private static Dictionary<string, ObjectData> ApplyEditOperations(AssetRequestedEventArgs args)
    {
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var dict = new Dictionary<string, ObjectData>();
        foreach (var op in editOperations)
        {
            ApplyDictionaryEditOperationTyped(op!, dict);
        }

        return dict;
    }

    private static void SetHelper(ModEntry modEntry, IModHelper helper)
    {
        var field = typeof(ModEntry).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.True(field != null, "Expected ModEntry to hold the injected IModHelper in a private '_helper' field (reflection seam).");
        field!.SetValue(modEntry, helper);
    }

    private static IModHelper CreateStubHelperWithTranslation()
    {
        var translation = new Mock<ITranslationHelper>();
        translation
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns((string key) => CreateTranslation(key, TranslationMarker + key));

        var helper = new Mock<IModHelper>();
        helper.Setup(h => h.Translation).Returns(translation.Object);
        return helper.Object;
    }

    private static Translation CreateTranslation(string key, string text)
    {
        var ctor = typeof(Translation).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .First(c => c.GetParameters().Length == 3);
        return (Translation)ctor.Invoke(new object[] { "en", key, text });
    }

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
            var method = new DynamicMethod("DummyGetOnBehalfOf", returnType, paramTypes, typeof(HeirloomRegistrationTests).Module);
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

    private static IList GetEditOperations(AssetRequestedEventArgs args)
    {
        var field = typeof(AssetRequestedEventArgs).GetField("<EditOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IList)field!.GetValue(args)!;
    }
}
