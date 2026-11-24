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
using StardewValley.GameData.Objects;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// T087 (FR-001, FR-003, US1): registration wiring for the single 017
/// Joja/agrochemical input in <c>Data/Objects</c>.
/// </summary>
/// <remarks>
/// <para>
/// Drives <c>ModEntry.OnAssetRequested</c> through the same
/// <c>AssetRequestedEventArgs</c> reflection seam
/// <see cref="AssetRegistrationTests"/> uses. Data/Objects entry currently
/// registers fine against production code, so these tests are
/// <c>// characterization</c> pins rather than RED tests; they exist so the
/// 017 content contract (exactly one agrochemical entry, no
/// fertilizer/compostable tags, keys localized through the 017 translation
/// keys) cannot silently drift.
/// </para>
/// <para>
/// The name/description routing through the 017 translation keys is observed
/// by reflecting a stub <see cref="IModHelper"/> into <c>ModEntry</c>'s
/// private <c>_helper</c> field; the marker prefix proves
/// <c>DisplayName</c>/<c>Description</c> were produced by
/// <c>LocalizeOrFallback(ModConstants.AgrochemicalItemNameKey /
/// AgrochemicalItemDescriptionKey, ...)</c> and not by the hardcoded
/// fallback literals.
/// </para>
/// </remarks>
public class AgrochemicalRegistrationTests
{
    private const string TranslationMarker = "T087:";

    /// <summary>
    /// T087 (FR-001, FR-003): exactly one agrochemical entry exists in
    /// <c>Data/Objects</c>, keyed by the unqualified ID, with
    /// <c>Type == "Basic"</c>, and the qualified <c>(O)</c> form is not used
    /// as a key.
    /// </summary>
    // characterization — the 017 item is already registered; this pins the contract.
    [Fact]
    public void OnAssetRequested_Objects_RegistersExactlyOneAgrochemicalEntryWithBasicTypeAndUnqualifiedIdKey()
    {
        Assert.Equal("LivingRoots.JojaAgrochemical", ModConstants.AgrochemicalItemId);
        Assert.Equal("(O)LivingRoots.JojaAgrochemical", ModConstants.QualifiedAgrochemicalItemId);

        var objectsDict = ApplyObjectsEdit();

        var agrochemicalKeys = objectsDict.Keys
            .Where(key => key.Contains("JojaAgrochemical", StringComparison.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        Assert.True(
            agrochemicalKeys.Length == 1,
            $"FR-001: exactly one agrochemical entry must exist in Data/Objects; found [{string.Join(", ", agrochemicalKeys)}].");
        Assert.Equal(ModConstants.AgrochemicalItemId, agrochemicalKeys[0]);
        Assert.False(
            objectsDict.ContainsKey(ModConstants.QualifiedAgrochemicalItemId),
            "Data/Objects keys are unqualified item IDs; the qualified (O) form must not be a key.");

        var entry = objectsDict[ModConstants.AgrochemicalItemId];
        Assert.Equal("Basic", entry.Type);
        Assert.False(string.IsNullOrWhiteSpace(entry.Name), "The agrochemical entry must carry a non-empty Name.");
        Assert.False(string.IsNullOrWhiteSpace(entry.Description), "The agrochemical entry must carry a non-empty Description.");
    }

    /// <summary>
    /// T087 (FR-003): the agrochemical entry carries no fertilizer or
    /// compostable <c>ContextTags</c> — it must not enter the fertilizer slot,
    /// the organic-waste/composting validator, or any vanilla regenerative
    /// path.
    /// </summary>
    // characterization — the 017 entry registers without ContextTags today; this pins it.
    [Fact]
    public void OnAssetRequested_Objects_AgrochemicalEntryHasNoFertilizerOrCompostableContextTags()
    {
        var objectsDict = ApplyObjectsEdit();

        var entry = objectsDict[ModConstants.AgrochemicalItemId];
        var tags = entry.ContextTags ?? new List<string>();
        var offending = tags
            .Where(tag => tag.Contains("fertilizer", StringComparison.OrdinalIgnoreCase)
                || tag.Contains("compostable", StringComparison.OrdinalIgnoreCase))
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offending);
    }

    /// <summary>
    /// T087 (FR-002, US2): the agrochemical <c>DisplayName</c> and
    /// <c>Description</c> are produced through the 017 translation keys
    /// (<see cref="ModConstants.AgrochemicalItemNameKey"/> and
    /// <see cref="ModConstants.AgrochemicalItemDescriptionKey"/>), not through
    /// hardcoded literals. Observed via a stub translation helper whose marker
    /// prefix proves <c>LocalizeOrFallback</c> was called with the 017 keys.
    /// </summary>
    // characterization — the registration already routes through LocalizeOrFallback with the 017 keys.
    [Fact]
    public void OnAssetRequested_Objects_AgrochemicalDisplayNameAndDescriptionRouteThrough017TranslationKeys()
    {
        var modEntry = new ModEntry();
        SetHelper(modEntry, CreateStubHelperWithTranslation());

        var args = CreateAssetRequestedEventArgs("Data/Objects");
        modEntry.OnAssetRequested(null, args);

        var objectsDict = ApplyEditOperations(args);

        var entry = objectsDict[ModConstants.AgrochemicalItemId];
        Assert.Equal(TranslationMarker + ModConstants.AgrochemicalItemNameKey, entry.DisplayName);
        Assert.Equal(TranslationMarker + ModConstants.AgrochemicalItemDescriptionKey, entry.Description);
    }

    // ---------------------------------------------------------------------
    // Test seam and factories (mirrors the AssetRegistrationTests pattern)
    // ---------------------------------------------------------------------

    private static Dictionary<string, ObjectData> ApplyObjectsEdit()
    {
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Objects");
        modEntry.OnAssetRequested(null, args);
        return ApplyEditOperations(args);
    }

    private static Dictionary<string, ObjectData> ApplyEditOperations(AssetRequestedEventArgs args)
    {
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var objectsDict = new Dictionary<string, ObjectData>();
        ApplyEditOperation(editOperations[0]!, objectsDict);
        return objectsDict;
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
            var method = new DynamicMethod("DummyGetOnBehalfOf", returnType, paramTypes, typeof(AgrochemicalRegistrationTests).Module);
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
