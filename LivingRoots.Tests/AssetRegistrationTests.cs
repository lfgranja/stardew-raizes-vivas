using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain.Services;
using LivingRoots.Tests.Fixtures;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Unit and contract tests verifying SMAPI content asset registration contracts
/// for recipes, objects, and machine rules.
/// Validates Task T010 [P] [US1] under Feature 004 (Playable Compost Loop).
/// </summary>
public class AssetRegistrationTests
{
    public const string ExpectedCraftingRecipe = "388 50 390 25 771 15/Home/LivingRoots.CompostingBin 1/true/default/Composting Bin";

    // T089 (FR-001a): the bin texture and progress sprite assets the native
    // machine interface renders from, served as named game assets so the
    // game's own content pipeline resolves them — the T078 convention
    // (Mods/<UniqueID>/<name>, manifest UniqueID lfgranja.LivingRoots).
    // T099 owns promoting these literals into ModConstants
    // (CompostingBinTextureAssetName and the two progress-sprite constants);
    // until then this test pins the expected names so the wiring cannot
    // silently drift.
    private const string BinTextureAssetName = "Mods/lfgranja.LivingRoots/CompostingBin";
    private const string BinProcessingSpriteAssetName = "Mods/lfgranja.LivingRoots/CompostingBin/Processing";
    private const string BinReadySpriteAssetName = "Mods/lfgranja.LivingRoots/CompostingBin/Ready";

    // T089 (FR-001a): the mod-folder-relative files the assets above resolve
    // to (LoadFromModFile paths, mirroring T078's "Assets/compost.png").
    private const string BinTextureModFilePath = "Assets/composting_bin_empty.png";
    private const string BinProcessingSpriteModFilePath = "Assets/composting_bin_processing.png";
    private const string BinReadySpriteModFilePath = "Assets/composting_bin_ready.png";

    [Fact]
    public void OnAssetRequested_CraftingRecipes_RegistersCompostingBinRecipeContract()
    {
        // Arrange
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/CraftingRecipes");

        // Act
        modEntry.OnAssetRequested(null, args);

        // Assert
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var recipeDict = new Dictionary<string, string>();
        ApplyEditOperation(editOperations[0]!, recipeDict);

        Assert.True(recipeDict.ContainsKey(ModConstants.CompostingBinRecipeId), $"Expected dictionary to contain recipe key '{ModConstants.CompostingBinRecipeId}'.");
        Assert.Equal(ExpectedCraftingRecipe, recipeDict[ModConstants.CompostingBinRecipeId]);
    }

    [Fact]
    public void OnAssetRequested_Objects_RegistersCompostDataContract()
    {
        // Arrange
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Objects");

        // Act
        modEntry.OnAssetRequested(null, args);

        // Assert
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var objectsDict = new Dictionary<string, ObjectData>();
        ApplyEditOperation(editOperations[0]!, objectsDict);

        // T053: the composting bin moved to Data/BigCraftables —
        // Data/Objects must not contain it (double registration
        // would fork one concept into two item types).
        Assert.False(objectsDict.ContainsKey(ModConstants.CompostingBinItemId), $"Expected Data/Objects to NOT contain key '{ModConstants.CompostingBinItemId}' (bin is a big craftable).");

        // Verify Compost item registration
        Assert.True(objectsDict.ContainsKey(ModConstants.CompostItemId), $"Expected Data/Objects to contain key '{ModConstants.CompostItemId}'.");
        var compostData = objectsDict[ModConstants.CompostItemId];
        Assert.NotNull(compostData);
        Assert.False(string.IsNullOrWhiteSpace(compostData.Name), "Compost object Name must not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(compostData.Description), "Compost object Description must not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(compostData.Type), "Compost object Type must not be empty.");
        Assert.Equal(ModConstants.CompostCategoryId, compostData.Category);
        Assert.True(compostData.Price >= 0, "Compost object Price must be non-negative.");
    }

    [Fact]
    public void OnAssetRequested_Objects_CompostCarriesIconTextureAndStackSize()
    {
        // Arrange
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Objects");

        // Act
        modEntry.OnAssetRequested(null, args);

        // Assert
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var objectsDict = new Dictionary<string, ObjectData>();
        ApplyEditOperation(editOperations[0]!, objectsDict);

        Assert.True(objectsDict.ContainsKey(ModConstants.CompostItemId), $"Expected Data/Objects to contain key '{ModConstants.CompostItemId}'.");
        var compostData = objectsDict[ModConstants.CompostItemId];
        Assert.NotNull(compostData);

        // T067 (FR-005 partial): the compost entry must carry its own
        // icon/texture data. ObjectData in Stardew 1.6 exposes a Texture
        // asset path and a SpriteIndex into it (verified against
        // StardewValley.GameData.dll); when Texture is unset the game
        // falls back to the vanilla springobjects sheet at index 0, so
        // the entry is only honest if it names its own texture.
        Assert.False(string.IsNullOrWhiteSpace(compostData.Texture), "Compost object must register its own Texture asset path (not the springobjects #0 fallback).");
        Assert.True(compostData.SpriteIndex >= 0, "Compost object must define a SpriteIndex into its Texture.");

        // Stack size: the 1.6 ObjectData schema carries no per-entry
        // stack field (verified against the game assembly), so the mod's
        // defined compost stack size is the ModConstants constant — it
        // must be a positive, defined value.
        Assert.True(ModConstants.CompostStackMax > 0, "Compost must have a defined maximum stack size.");
    }

    [Fact]
    public void OnAssetRequested_BigCraftables_RegistersCompostingBinMachineContract()
    {
        // Arrange
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/BigCraftables");

        // Act
        modEntry.OnAssetRequested(null, args);

        // Assert
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var bigCraftablesDict = new Dictionary<string, BigCraftableData>();
        ApplyEditOperation(editOperations[0]!, bigCraftablesDict);

        // T053 (FR-001a): the bin is a native big craftable, keyed
        // by the unqualified item ID.
        Assert.True(bigCraftablesDict.ContainsKey(ModConstants.CompostingBinItemId), $"Expected Data/BigCraftables to contain key '{ModConstants.CompostingBinItemId}'.");
        var binData = bigCraftablesDict[ModConstants.CompostingBinItemId];
        Assert.NotNull(binData);
        Assert.False(string.IsNullOrWhiteSpace(binData.Name), "Bin big craftable Name must not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(binData.DisplayName), "Bin big craftable DisplayName must not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(binData.Description), "Bin big craftable Description must not be empty.");
        Assert.True(binData.Price >= 0, "Bin big craftable Price must be non-negative.");
        // Fragility 0 = pick up with any tool (FR-005c).
        Assert.Equal(0, binData.Fragility);
        // Placeable on the farm (outdoors) and in the Greenhouse (indoors) (FR-002).
        Assert.True(binData.CanBePlacedIndoors, "Bin must be placeable indoors (Greenhouse).");
        Assert.True(binData.CanBePlacedOutdoors, "Bin must be placeable outdoors (farm).");
    }

    /// <summary>
    /// T111 (FR-005a): the bin's player-facing description must state
    /// the single-yield contract — one completed batch yields exactly
    /// one compost — and must carry no increasing-output promise. The
    /// maturation-level multiplier settled in the soil decay and compost
    /// specification (one to five compost items according to how long a
    /// bin had been kept busy) is superseded by FR-005a and MUST NOT be
    /// implemented, so the description may not advertise output that
    /// grows with use. Property-based testing is N/A here (Phase 12
    /// rule 1): the description is a fixed player-facing string literal
    /// registered in Data/BigCraftables, with no input domain to
    /// generate over, so the assertions run against that literal.
    /// </summary>
    [Fact]
    public void OnAssetRequested_BigCraftables_BinDescriptionStatesSingleYield()
    {
        // Arrange
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/BigCraftables");

        // Act
        modEntry.OnAssetRequested(null, args);

        // Assert
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var bigCraftablesDict = new Dictionary<string, BigCraftableData>();
        ApplyEditOperation(editOperations[0]!, bigCraftablesDict);

        Assert.True(bigCraftablesDict.ContainsKey(ModConstants.CompostingBinItemId), $"Expected Data/BigCraftables to contain key '{ModConstants.CompostingBinItemId}'.");
        var binData = bigCraftablesDict[ModConstants.CompostingBinItemId];
        Assert.NotNull(binData);
        Assert.False(string.IsNullOrWhiteSpace(binData.Description), "Bin big craftable Description must not be empty.");
        var description = binData.Description;

        // Single-yield statement (FR-005a): the description must tell
        // the player that one completed batch yields exactly one compost.
        Assert.True(
            description.Contains("one compost", StringComparison.OrdinalIgnoreCase),
            $"FR-005a (T111): the bin BigCraftableData description must state that one batch yields exactly one compost (single yield) — the retired 1-5 maturation multiplier MUST NOT be implemented, so the batch output is pinned to one compost. Actual description: \"{description}\". T112 must rewrite the description in LivingRoots/ModEntry.cs to state the single-yield contract.");

        // No increasing-output promise (FR-005a): the retired multiplier
        // promised output that grew with continuous use — the description
        // must not promise more compost for longer-running bins.
        Assert.False(
            description.Contains("increase", StringComparison.OrdinalIgnoreCase),
            $"FR-005a (T111): the bin BigCraftableData description must not promise increasing output — the 1-5 maturation multiplier is superseded by FR-005a and MUST NOT be implemented, so the description may not advertise output that grows with use. Actual description: \"{description}\". T112 must rewrite the description in LivingRoots/ModEntry.cs removing the retired-multiplier promise.");
    }

    [Fact]
    public void OnAssetRequested_Machines_RegistersCompostingBinWithNativeMachineOutputRules()
    {
        // Arrange
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Machines");

        // Act
        modEntry.OnAssetRequested(null, args);

        // Assert
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var machinesDict = new Dictionary<string, MachineData>();
        ApplyEditOperation(editOperations[0]!, machinesDict);

        string rawKey = ModConstants.MachineCompostingBinId;
        string bcKey = "(BC)" + ModConstants.MachineCompostingBinId;

        bool hasMachine = machinesDict.ContainsKey(rawKey) || machinesDict.ContainsKey(bcKey);
        Assert.True(hasMachine, $"Expected Data/Machines to contain key '{rawKey}' or '{bcKey}'.");

        var machineData = machinesDict.ContainsKey(rawKey) ? machinesDict[rawKey] : machinesDict[bcKey];
        Assert.NotNull(machineData);
        Assert.NotNull(machineData.OutputRules);
        Assert.NotEmpty(machineData.OutputRules);
    }

    [Fact]
    public void OnAssetRequested_Machines_NativeDaysUntilReadyPinsModMaturationClock()
    {
        // Arrange
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Machines");

        // Act
        modEntry.OnAssetRequested(null, args);

        // Assert
        var editOperations = GetEditOperations(args);
        Assert.NotEmpty(editOperations);

        var machinesDict = new Dictionary<string, MachineData>();
        ApplyEditOperation(editOperations[0]!, machinesDict);

        string rawKey = ModConstants.MachineCompostingBinId;
        string bcKey = "(BC)" + ModConstants.MachineCompostingBinId;

        bool hasMachine = machinesDict.ContainsKey(rawKey) || machinesDict.ContainsKey(bcKey);
        Assert.True(hasMachine, $"Expected Data/Machines to contain key '{rawKey}' or '{bcKey}'.");

        var machineData = machinesDict.ContainsKey(rawKey) ? machinesDict[rawKey] : machinesDict[bcKey];
        Assert.NotNull(machineData);
        Assert.NotNull(machineData.OutputRules);
        Assert.NotEmpty(machineData.OutputRules);

        // T071 (FR-001b partial): machine-clock coherence pin. The native
        // Data/Machines rule's DaysUntilReady and the mod's day-start
        // maturation — CompostingBinService.ProcessDayStart matures a
        // Processing batch once elapsed >= ModConstants.MaturationDays —
        // both read the same constant, so the two clocks cannot drift.
        // Pin the native value to that constant, and the shared duration
        // to exactly 2 in-game days: no double maturation, no shortcut.
        Assert.Equal(ModConstants.MaturationDays, machineData.OutputRules[0].DaysUntilReady);
        Assert.Equal(2, ModConstants.MaturationDays);
    }

    /// <summary>
    /// The validator's qualifying numeric categories
    /// (OrganicWasteValidator.cs:36-44): seeds, vegetables,
    /// fruits, flowers, and forage/greens.
    /// </summary>
    private static readonly int[] ParityQualifyingCategories = { -74, -75, -79, -80, -81 };

    /// <summary>
    /// Non-qualifying categories the generator also produces:
    /// the untagged default, out-of-set values, and the
    /// neighbouring category bands just outside the
    /// qualifying set.
    /// </summary>
    private static readonly int[] ParityNonQualifyingCategories = { 0, -1, -73, -76, -999, 100 };

    /// <summary>
    /// T120 (FR-003 partial): the native <c>Data/Machines</c>
    /// input filter and the domain validator must accept
    /// exactly the same items. The qualifying set is owned
    /// by the soil decay and compost specification and is
    /// inherited by FR-003 unchanged, so this feature MUST
    /// NOT restate, extend, narrow, or contradict it — and
    /// the two definitions the mod maintains today must
    /// accept the same items, or the native machine path
    /// and the mod's <c>AddWaste</c> path disagree about
    /// what compostable material is. Today the definitions
    /// are maintained independently — the machine rule's
    /// triggers are keyed on context tags (ModEntry.cs:381-389:
    /// category_seeds, category_vegetable, category_fruits,
    /// category_flowers, category_greens, compostable_item,
    /// each ANDed with !not_compostable) while the validator
    /// is keyed on the numeric Category field plus the
    /// inclusion and exclusion tags (OrganicWasteValidator.cs:24-46)
    /// — and no test ties them together. This property
    /// generates arbitrary items (an id, a numeric category
    /// drawn from both category bands, and a context-tag set
    /// drawn from the shared tag universe) and asserts the
    /// two definitions agree on every one: every
    /// validator-accepted item passes the machine rule and
    /// vice versa — no drift, no narrow, no extend. RED
    /// today: the definitions are untied, so the generator
    /// produces counterexamples in both directions — a
    /// qualifying-category item whose object-data entry
    /// carries no category tag (the validator accepts it by
    /// category, the machine rule rejects it for lack of the
    /// tag) and a tag-carrying item outside the qualifying
    /// categories (the machine rule accepts it by tag, the
    /// validator rejects it by category). T133 must derive
    /// the Data/Machines RequiredTags from the single
    /// validator-owned waste set (or pin parity) so the
    /// inherited qualifying set is accepted by exactly one
    /// definition.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool OnAssetRequested_Machines_RequiredTagsAcceptExactlyTheOrganicWasteValidatorSet(
        string arbitraryItemId,
        int arbitraryCategory,
        bool carriesSeedsTag,
        bool carriesVegetableTag,
        bool carriesFruitsTag,
        bool carriesFlowersTag,
        bool carriesGreensTag,
        bool carriesCompostableTag,
        bool carriesNotCompostableTag)
    {
        // The category is drawn from the union of the
        // qualifying and non-qualifying bands so the
        // generator exercises both acceptance directions
        // (a raw int almost never lands on one of the five
        // qualifying categories).
        var categoryPool = ParityQualifyingCategories.Concat(ParityNonQualifyingCategories).ToArray();
        var category = categoryPool[(arbitraryCategory & int.MaxValue) % categoryPool.Length];

        // The context-tag set the item's object-data entry
        // carries — the shared tag universe both definitions
        // speak.
        var tagBits = (carriesSeedsTag ? 1 : 0)
            | (carriesVegetableTag ? 2 : 0)
            | (carriesFruitsTag ? 4 : 0)
            | (carriesFlowersTag ? 8 : 0)
            | (carriesGreensTag ? 16 : 0)
            | (carriesCompostableTag ? 32 : 0)
            | (carriesNotCompostableTag ? 64 : 0);
        var contextTags = new List<string>();
        if (carriesSeedsTag)
        {
            contextTags.Add("category_seeds");
        }

        if (carriesVegetableTag)
        {
            contextTags.Add("category_vegetable");
        }

        if (carriesFruitsTag)
        {
            contextTags.Add("category_fruits");
        }

        if (carriesFlowersTag)
        {
            contextTags.Add("category_flowers");
        }

        if (carriesGreensTag)
        {
            contextTags.Add("category_greens");
        }

        if (carriesCompostableTag)
        {
            contextTags.Add("compostable_item");
        }

        if (carriesNotCompostableTag)
        {
            contextTags.Add("not_compostable");
        }

        // The item-definition pipeline resolves context tags
        // through the object-data table, keyed by the
        // unqualified id — the truthful stand-in for the
        // game's content pipeline, which never boots in a
        // test host (GameStateFixture's documented role).
        // The id is deterministic per (category, tag set) so
        // a re-generated combination re-registers
        // idempotently instead of polluting the table.
        GameStateFixture.Install();
        var sanitizedId = new string((arbitraryItemId ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        var itemId = $"parity{sanitizedId}{category}{tagBits}";
        Game1.objectData[itemId] = new ObjectData { ContextTags = contextTags };
        var item = new StardewValley.Object { ItemId = itemId, Category = category };

        // The two independently-maintained definitions.
        var validatorAccepts = new OrganicWasteValidator().IsValidOrganicWaste(item);
        var machineAccepts = MachineRuleAccepts(RegisteredCompostingBinMachineData(), item);

        // Parity: every validator-accepted item passes the
        // machine rule and vice versa — no drift, no narrow,
        // no extend.
        return validatorAccepts == machineAccepts;
    }

    /// <summary>
    /// T120 (FR-003 partial): the drift the parity property
    /// finds, pinned deterministically in the
    /// validator-accepts / machine-rejects direction. A
    /// vegetable-category item whose object-data entry
    /// carries no context tags — the shape an item has
    /// until its Data/Objects entry lists the category tag
    /// — is accepted by the validator (Category -75) and
    /// rejected by the native machine rule, whose Vegetables
    /// trigger requires the category_vegetable context tag
    /// the validator's category-based acceptance does not
    /// place on the item. RED today: the two definitions
    /// are maintained independently with no tying test.
    /// T133 derives the Data/Machines RequiredTags from the
    /// single validator-owned waste set (or pins parity).
    /// </summary>
    [Fact]
    public void OnAssetRequested_Machines_RequiredTagsDrift_VegetableCategoryItemTheValidatorAccepts()
    {
        GameStateFixture.Install();
        const string itemId = "parity.drift.vegetable";
        Game1.objectData[itemId] = new ObjectData { ContextTags = new List<string>() };
        var item = new StardewValley.Object { ItemId = itemId, Category = -75 };

        var validatorAccepts = new OrganicWasteValidator().IsValidOrganicWaste(item);
        var machineAccepts = MachineRuleAccepts(RegisteredCompostingBinMachineData(), item);

        Assert.True(validatorAccepts, "The validator accepts a vegetable-category item (OrganicWasteValidator.cs:39, Category -75).");
        Assert.True(
            machineAccepts,
            "FR-003 (T120): the native Data/Machines rule rejects a vegetable-category item the validator accepts. " +
            "The rule's Vegetables trigger (ModEntry.cs:384) requires the category_vegetable context tag, which the " +
            "validator's category-based acceptance (OrganicWasteValidator.cs:39, Category -75) does not place on the " +
            "item — the two definitions are maintained independently (ModEntry.cs:381-389 vs OrganicWasteValidator.cs:24-46) " +
            "with no tying test, so the native machine path and the mod's AddWaste path accept different items. " +
            "T133 must derive the Data/Machines RequiredTags from the single validator-owned waste set (or pin parity) " +
            "so the qualifying set FR-003 inherits is accepted by exactly one definition.");
    }

    /// <summary>
    /// T120 (FR-003 partial): the drift the parity property
    /// finds, pinned deterministically in the
    /// machine-accepts / validator-rejects direction. An
    /// item carrying the category_vegetable context tag
    /// while its numeric Category sits outside the
    /// qualifying set is accepted by the native machine
    /// rule (its triggers key on tags, not on the numeric
    /// category) and rejected by the validator — the tag
    /// rule extends the inherited qualifying set beyond
    /// what the validator accepts. RED today: the two
    /// definitions are maintained independently with no
    /// tying test. T133 derives the Data/Machines
    /// RequiredTags from the single validator-owned waste
    /// set (or pins parity).
    /// </summary>
    [Fact]
    public void OnAssetRequested_Machines_RequiredTagsDrift_TagCarryingItemTheValidatorRejects()
    {
        GameStateFixture.Install();
        const string itemId = "parity.drift.tagged";
        Game1.objectData[itemId] = new ObjectData { ContextTags = new List<string> { "category_vegetable" } };
        var item = new StardewValley.Object { ItemId = itemId, Category = 0 };

        var validatorAccepts = new OrganicWasteValidator().IsValidOrganicWaste(item);
        var machineAccepts = MachineRuleAccepts(RegisteredCompostingBinMachineData(), item);

        Assert.False(validatorAccepts, "The validator rejects a category-0 item (OrganicWasteValidator.cs:43, Category outside the qualifying set).");
        Assert.False(
            machineAccepts,
            "FR-003 (T120): the native Data/Machines rule accepts a category-0 item carrying the category_vegetable " +
            "tag that the validator rejects — the rule's tag-based triggers accept any item carrying a qualifying " +
            "category tag regardless of its numeric Category (ModEntry.cs:384), extending the inherited qualifying set " +
            "(OrganicWasteValidator.cs:36-44) beyond what the validator accepts. The two definitions are maintained " +
            "independently (ModEntry.cs:381-389 vs OrganicWasteValidator.cs:24-46) with no tying test, so the native " +
            "machine path and the mod's AddWaste path accept different items. T133 must derive the Data/Machines " +
            "RequiredTags from the single validator-owned waste set (or pin parity) so the qualifying set FR-003 " +
            "inherits is accepted by exactly one definition.");
    }

    /// <summary>
    /// The composting bin's registered <c>Data/Machines</c>
    /// processing rule, read through the same asset-edit
    /// harness the other machine-contract tests use.
    /// </summary>
    /// <returns>The registered machine data for the bin.</returns>
    private MachineData RegisteredCompostingBinMachineData()
    {
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Machines");

        modEntry.OnAssetRequested(null, args);

        var editOperations = GetEditOperations(args);
        var machinesDict = new Dictionary<string, MachineData>();
        ApplyEditOperation(editOperations[0]!, machinesDict);

        string bcKey = "(BC)" + ModConstants.MachineCompostingBinId;
        return machinesDict[bcKey];
    }

    /// <summary>
    /// Evaluates the registered <c>Data/Machines</c> output
    /// rule against an item the way the game's native
    /// machine interface does: a trigger rule matches when
    /// the item satisfies every tag in its
    /// <c>RequiredTags</c> — a bare tag must be present, a
    /// "!"-prefixed tag must be absent — and the output
    /// rule matches when any trigger rule matches (each
    /// trigger ANDs its tags; any matching trigger starts
    /// the rule).
    /// </summary>
    /// <param name="machineData">The registered machine rule.</param>
    /// <param name="item">The item the machine evaluates.</param>
    /// <returns>True when the native rule accepts the item.</returns>
    private static bool MachineRuleAccepts(MachineData machineData, Item item)
    {
        if (machineData?.OutputRules == null)
        {
            return false;
        }

        foreach (var outputRule in machineData.OutputRules)
        {
            if (outputRule?.Triggers == null)
            {
                continue;
            }

            foreach (var trigger in outputRule.Triggers)
            {
                if (trigger?.RequiredTags == null)
                {
                    continue;
                }

                var matches = true;
                foreach (var requiredTag in trigger.RequiredTags)
                {
                    var negated = requiredTag.StartsWith("!", StringComparison.Ordinal);
                    var tag = negated ? requiredTag[1..] : requiredTag;
                    var carriesTag = item.HasContextTag(tag);
                    if (negated ? carriesTag : !carriesTag)
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// T089 (FR-001a partial): the bin engages the native game machine
    /// interface — a Data/BigCraftables entry plus Data/Machines processing
    /// rules (Triggers + DaysUntilReady = MaturationDays) — with the bin
    /// texture and its progress sprites wired into the game content pipeline
    /// as named mod assets that resolve to real files under LivingRoots/Assets,
    /// rather than a bespoke interface supplied by this mod.
    ///
    /// Green-by-existing-behavior legs (seams wired by T053/T071, kept here
    /// to document the native machine contract): the BigCraftables entry with
    /// its placement/fragility fields, and the Data/Machines rule with
    /// Triggers and DaysUntilReady pinned to ModConstants.MaturationDays.
    ///
    /// Red legs (unwired today; T099 owns the wiring): BigCraftableData.Texture
    /// on the bin entry, and the ModEntry.OnAssetRequested LoadFromModFile
    /// branches serving the bin texture asset and the two progress sprite
    /// assets from the mod folder.
    /// </summary>
    [Fact]
    public void OnAssetRequested_NativeMachine_BinEngagesMachineInterfaceWithWiredBinTextureAndProgressSprites()
    {
        // Arrange
        var modEntry = new ModEntry();

        // Leg 1 — native big craftable entry (green by T053 wiring).
        var bigCraftablesArgs = CreateAssetRequestedEventArgs("Data/BigCraftables");
        modEntry.OnAssetRequested(null, bigCraftablesArgs);

        var bigCraftablesOps = GetEditOperations(bigCraftablesArgs);
        Assert.True(bigCraftablesOps.Count > 0, "Expected Data/BigCraftables edit operations for the composting bin.");
        var bigCraftablesDict = new Dictionary<string, BigCraftableData>();
        ApplyEditOperation(bigCraftablesOps[0]!, bigCraftablesDict);

        Assert.True(bigCraftablesDict.ContainsKey(ModConstants.CompostingBinItemId), $"Expected Data/BigCraftables to contain key '{ModConstants.CompostingBinItemId}'.");
        var binData = bigCraftablesDict[ModConstants.CompostingBinItemId];
        Assert.NotNull(binData);
        Assert.False(string.IsNullOrWhiteSpace(binData.Name), "Bin big craftable Name must not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(binData.DisplayName), "Bin big craftable DisplayName must not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(binData.Description), "Bin big craftable Description must not be empty.");
        Assert.True(binData.Price >= 0, "Bin big craftable Price must be non-negative.");
        // Fragility 0 = pick up with any tool (FR-005c).
        Assert.Equal(0, binData.Fragility);
        // Placeable on the farm (outdoors) and in the Greenhouse (indoors) (FR-002).
        Assert.True(binData.CanBePlacedIndoors, "Bin must be placeable indoors (Greenhouse).");
        Assert.True(binData.CanBePlacedOutdoors, "Bin must be placeable outdoors (farm).");

        // Leg 2 (RED) — the bin entry must carry its own texture asset path
        // so the native machine interface renders the mod's bin instead of
        // falling back to the vanilla big-craftable sheet, and the path must
        // be the asset name ModEntry serves below (T078 convention).
        Assert.False(
            string.IsNullOrWhiteSpace(binData.Texture),
            $"FR-001a (T089): the bin big craftable entry leaves Texture null, so the native machine interface falls back to the vanilla big-craftable sheet instead of the mod's bin texture. Missing wiring: BigCraftableData.Texture must be set to '{BinTextureAssetName}' (T099 introduces ModConstants.CompostingBinTextureAssetName).");
        Assert.True(
            binData.Texture == BinTextureAssetName,
            $"FR-001a (T089): the bin big craftable Texture must name the same asset ModEntry serves ('{BinTextureAssetName}') so the game content pipeline resolves it to the mod's bin texture. Expected '{BinTextureAssetName}', actual '{(binData.Texture == null ? "null" : binData.Texture)}'.");

        // Leg 3 — native Data/Machines processing rules (green by T053/T071 wiring).
        var machinesArgs = CreateAssetRequestedEventArgs("Data/Machines");
        modEntry.OnAssetRequested(null, machinesArgs);

        var machinesOps = GetEditOperations(machinesArgs);
        Assert.True(machinesOps.Count > 0, "Expected Data/Machines edit operations for the composting bin.");
        var machinesDict = new Dictionary<string, MachineData>();
        ApplyEditOperation(machinesOps[0]!, machinesDict);

        string rawKey = ModConstants.MachineCompostingBinId;
        string bcKey = "(BC)" + ModConstants.MachineCompostingBinId;
        Assert.True(machinesDict.ContainsKey(rawKey) || machinesDict.ContainsKey(bcKey), $"Expected Data/Machines to contain key '{rawKey}' or '{bcKey}'.");
        var machineData = machinesDict.ContainsKey(rawKey) ? machinesDict[rawKey] : machinesDict[bcKey];
        Assert.NotNull(machineData);
        Assert.NotNull(machineData.OutputRules);
        Assert.True(machineData.OutputRules.Count > 0, "Expected at least one Data/Machines output rule for the composting bin.");
        // FR-001a: the rule must engage the native trigger interface — the
        // game itself starts the batch when a qualifying item is placed in
        // the machine (ItemPlacedInMachine).
        Assert.NotNull(machineData.OutputRules[0].Triggers);
        Assert.True(machineData.OutputRules[0].Triggers.Count > 0, "FR-001a (T089): the native Data/Machines rule must declare Triggers so the game's own machine interface starts the batch when organic waste is placed in the bin.");
        // FR-001b: the native processing duration is pinned to the mod's
        // maturation clock — no double maturation, no shortcut.
        Assert.Equal(ModConstants.MaturationDays, machineData.OutputRules[0].DaysUntilReady);

        // Leg 4 (RED) — the bin texture asset must be served from the mod
        // folder so the BigCraftableData.Texture path above resolves.
        var binTextureArgs = CreateAssetRequestedEventArgs(BinTextureAssetName);
        modEntry.OnAssetRequested(null, binTextureArgs);

        var binTextureLoads = GetLoadOperations(binTextureArgs);
        Assert.True(
            binTextureLoads.Count > 0,
            $"FR-001a (T089): ModEntry.OnAssetRequested recorded no load operation for the bin texture asset '{BinTextureAssetName}' — the bin texture wiring is missing. T099 must add an e.LoadFromModFile<Texture2D>(\"{BinTextureModFilePath}\", AssetLoadPriority.Medium) branch keyed on e.NameWithoutLocale.IsEquivalentTo(\"{BinTextureAssetName}\").");
        var binTexturePath = GetLoadOperationRelativePath(binTextureLoads[0]!);
        Assert.True(
            binTexturePath == BinTextureModFilePath,
            $"FR-001a (T089): the bin texture asset '{BinTextureAssetName}' must load from mod file '{BinTextureModFilePath}'. Expected '{BinTextureModFilePath}', actual '{binTexturePath}'.");
        Assert.True(
            File.Exists(Path.Combine(GetModDirectory(), binTexturePath)),
            $"FR-001a (T089): the bin texture load path '{binTexturePath}' does not resolve to a real file under LivingRoots/Assets/.");

        // Leg 5 (RED) — the progress sprites must be served as game assets
        // so the native machine interface can display the processing and
        // ready states from the mod's own sprites.
        var processingArgs = CreateAssetRequestedEventArgs(BinProcessingSpriteAssetName);
        modEntry.OnAssetRequested(null, processingArgs);

        var processingLoads = GetLoadOperations(processingArgs);
        Assert.True(
            processingLoads.Count > 0,
            $"FR-001a (T089): ModEntry.OnAssetRequested recorded no load operation for the bin processing-progress sprite asset '{BinProcessingSpriteAssetName}' — the progress sprite wiring is missing. T099 must add an e.LoadFromModFile<Texture2D>(\"{BinProcessingSpriteModFilePath}\", AssetLoadPriority.Medium) branch keyed on e.NameWithoutLocale.IsEquivalentTo(\"{BinProcessingSpriteAssetName}\").");
        var processingPath = GetLoadOperationRelativePath(processingLoads[0]!);
        Assert.True(
            processingPath == BinProcessingSpriteModFilePath,
            $"FR-001a (T089): the processing-progress sprite asset '{BinProcessingSpriteAssetName}' must load from mod file '{BinProcessingSpriteModFilePath}'. Expected '{BinProcessingSpriteModFilePath}', actual '{processingPath}'.");
        Assert.True(
            File.Exists(Path.Combine(GetModDirectory(), processingPath)),
            $"FR-001a (T089): the processing-progress sprite load path '{processingPath}' does not resolve to a real file under LivingRoots/Assets/.");

        var readyArgs = CreateAssetRequestedEventArgs(BinReadySpriteAssetName);
        modEntry.OnAssetRequested(null, readyArgs);

        var readyLoads = GetLoadOperations(readyArgs);
        Assert.True(
            readyLoads.Count > 0,
            $"FR-001a (T089): ModEntry.OnAssetRequested recorded no load operation for the bin ready-progress sprite asset '{BinReadySpriteAssetName}' — the progress sprite wiring is missing. T099 must add an e.LoadFromModFile<Texture2D>(\"{BinReadySpriteModFilePath}\", AssetLoadPriority.Medium) branch keyed on e.NameWithoutLocale.IsEquivalentTo(\"{BinReadySpriteAssetName}\").");
        var readyPath = GetLoadOperationRelativePath(readyLoads[0]!);
        Assert.True(
            readyPath == BinReadySpriteModFilePath,
            $"FR-001a (T089): the ready-progress sprite asset '{BinReadySpriteAssetName}' must load from mod file '{BinReadySpriteModFilePath}'. Expected '{BinReadySpriteModFilePath}', actual '{readyPath}'.");
        Assert.True(
            File.Exists(Path.Combine(GetModDirectory(), readyPath)),
            $"FR-001a (T089): the ready-progress sprite load path '{readyPath}' does not resolve to a real file under LivingRoots/Assets/.");
    }

    [Fact]
    public void OnAssetRequested_UnrelatedAsset_DoesNotRegisterAnyEdits()
    {
        // Arrange
        var modEntry = new ModEntry();
        var args = CreateAssetRequestedEventArgs("Data/Bundles");

        // Act
        modEntry.OnAssetRequested(null, args);

        // Assert
        var editOperations = GetEditOperations(args);
        Assert.Empty(editOperations);
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
            var method = new DynamicMethod("DummyGetOnBehalfOf", returnType, paramTypes, typeof(AssetRegistrationTests).Module);
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

    private static IList GetLoadOperations(AssetRequestedEventArgs args)
    {
        var field = typeof(AssetRequestedEventArgs).GetField("<LoadOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IList)field!.GetValue(args)!;
    }

    /// <summary>
    /// Reads the mod-folder-relative file path a recorded load operation
    /// serves. SMAPI's AssetRequestedEventArgs.LoadFromModFile captures the
    /// relative path in the GetData closure's compiler-generated display
    /// class, so the path is reachable only through reflection
    /// (AssetLoadOperation is an internal SMAPI type).
    /// </summary>
    private static string GetLoadOperationRelativePath(object loadOperation)
    {
        var getDataProperty = loadOperation.GetType().GetProperty("GetData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.True(getDataProperty != null, "Expected the recorded load operation to expose its GetData delegate.");
        var getData = (Delegate)getDataProperty!.GetValue(loadOperation)!;
        var closure = getData.Target;
        Assert.True(closure != null, "Expected the load operation's GetData delegate to be a closure capturing the load path.");
        var relativePathField = closure!.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(f => f.FieldType == typeof(string) && f.Name == "relativePath");
        Assert.True(relativePathField != null, "Expected the load operation closure to capture a 'relativePath' string field (SMAPI LoadFromModFile).");
        return (string)relativePathField!.GetValue(closure)!;
    }

    /// <summary>
    /// Walks up from the test output directory to the repository root and
    /// returns the mod project folder (LivingRoots/), the base the mod's
    /// LoadFromModFile paths are relative to.
    /// </summary>
    private static string GetModDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? projectRoot = null;
        for (int i = 0; i < 10 && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Stardew-LivingRoots.sln")))
            {
                projectRoot = dir.FullName;
                break;
            }

            dir = dir.Parent;
        }

        Assert.True(projectRoot != null, "Could not locate the repository root (Stardew-LivingRoots.sln) from the test output directory.");
        return Path.Combine(projectRoot!, "LivingRoots");
    }
}
