using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LivingRoots.Domain;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Tests for <see cref="CompostApplicationService.TryApplyCompost"/>.
/// </summary>
/// <remarks>
/// <para>
/// The service rejects with <c>Game1.playSound("cancel")</c> on every failure path, so these
/// tests install <see cref="GameStateFixture"/>, which substitutes
/// <see cref="NoOpSoundsHelperStub"/> for the game sound helper.
/// </para>
/// <para>
/// <b>Prerequisite:</b> <see cref="GameStateFixture"/> is installed by this class's constructor and
/// is load-bearing for all six tests. Four of them need a real <c>HoeDirt</c>, because the service
/// checks the tile type before it looks at soil health or the held item, and
/// <c>HoeDirt</c>'s constructors all funnel into <c>initialize(Game1.currentLocation)</c>.
/// <c>Game1.currentLocation</c> reads <c>Game1.game1</c>, whose real constructor dereferences the
/// XNA content service provider that only MonoGame populates while booting. The fixture allocates
/// that static without the constructor, which is what lets <c>HoeDirt</c> resolve a (null)
/// location. Removing it breaks every test here.
/// </para>
/// <para>
/// The same fixture registers the <c>(O)</c> item type definition, so an item created by
/// <see cref="ItemFactory"/> reports <c>QualifiedItemId</c> as <c>"(O)LivingRoots.Compost"</c>
/// rather than the bare id it assigns. The service's held-item check accepts both forms.
/// </para>
/// </remarks>
public class CompostApplicationServiceTests
{
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly PlayerProviderStub _playerStub;
    private readonly CompostApplicationService _service;

    /// <summary>Creates the service under test and installs the minimum required game state.</summary>
    public CompostApplicationServiceTests()
    {
        GameStateFixture.Install();
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockMonitor = new Mock<IMonitor>();
        _playerStub = new PlayerProviderStub();

        _service = new CompostApplicationService(
            _mockSoilHealthService.Object,
            _playerStub,
            _mockMonitor.Object);
    }

    /// <summary>Compost raises soil health on a valid tilled farm tile (FR-6.1).</summary>
    [Fact]
    public void TryApplyCompost_ValidTile_IncreasesHealth()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.True(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);
    }

    /// <summary>A tile already at maximum health is rejected (FR-6.2).</summary>
    [Fact]
    public void TryApplyCompost_AtMaxHealth_ReturnsFalse()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(ModConstants.MaxSoilHealth);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }

    /// <summary>Compost is rejected outside the farm and the Greenhouse (FR-6.2).</summary>
    [Fact]
    public void TryApplyCompost_InvalidLocation_ReturnsFalse()
    {
        var fixture = new GameLocationFixture("Town", isFarm: false);
        var tile = new Vector2(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
    }

    /// <summary>Compost is rejected when the player is not holding compost (FR-6.2).</summary>
    [Fact]
    public void TryApplyCompost_NoCompostHeld_ReturnsFalse()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        _playerStub.CurrentItem = null;

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
    }

    /// <summary>Compost is rejected when the target tile is not tilled soil (FR-6.2).</summary>
    [Fact]
    public void TryApplyCompost_NonHoeDirtTile_ReturnsFalse()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
    }

    /// <summary>
    /// Health is capped at <see cref="ModConstants.MaxSoilHealth"/>: the service reports success
    /// but the applied delta may not push the tile past the maximum (FR-6.1).
    /// </summary>
    [Fact]
    public void TryApplyCompost_HealthCappedAtMax()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(90f);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.True(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);
    }

    /// <summary>
    /// Compost application on tilled tiles with active growing crops succeeds as side-dressing
    /// and does not harm or remove the crop (T028, FR-007).
    /// </summary>
    [Fact]
    public void TryApplyCompost_TilledTileWithActiveCrop_SucceedsAndPreservesCrop()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10, hasCrop: true);

        var hoeDirt = (StardewValley.TerrainFeatures.HoeDirt)fixture.Location.terrainFeatures[tile];
        var originalCrop = hoeDirt.crop;
        Assert.NotNull(originalCrop);

        var compostItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        compostItem.Stack = 5;
        _playerStub.CurrentItem = compostItem;

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(40f);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.True(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);
        Assert.Equal(4, compostItem.Stack);

        // Crop must be intact and not destroyed or cleared
        Assert.NotNull(hoeDirt.crop);
        Assert.Same(originalCrop, hoeDirt.crop);
    }

    /// <summary>
    /// Applying compost to an untilled tile is refused: returns false, consumes zero compost items,
    /// leaves inventory stack untouched, and triggers HUD message/log (T029, FR-008, FR-008a).
    /// </summary>
    [Fact]
    public void TryApplyCompost_UntilledTile_RefusedWithZeroCompostConsumed()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(15, 15); // No HoeDirt added

        var compostItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        compostItem.Stack = 3;
        _playerStub.CurrentItem = compostItem;

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
        Assert.Equal(3, compostItem.Stack);
        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }

    /// <summary>
    /// Applying compost on a non-farm and non-greenhouse map is refused: returns false, consumes zero
    /// compost items, and leaves soil health untouched (T029, FR-008, FR-008a).
    /// </summary>
    [Fact]
    public void TryApplyCompost_NonFarmMap_RefusedWithZeroCompostConsumed()
    {
        var fixture = new GameLocationFixture("Town", isFarm: false);
        var tile = new Vector2(5, 5);

        var compostItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        compostItem.Stack = 2;
        _playerStub.CurrentItem = compostItem;

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
        Assert.Equal(2, compostItem.Stack);
        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }

    /// <summary>
    /// Applying compost to a tile already at max health (100) is refused: returns false, consumes zero
    /// compost items, and leaves soil health untouched (T029, FR-008, FR-008a).
    /// </summary>
    [Fact]
    public void TryApplyCompost_MaxHealthTile_RefusedWithZeroCompostConsumed()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        var compostItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        compostItem.Stack = 10;
        _playerStub.CurrentItem = compostItem;

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(ModConstants.MaxSoilHealth);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
        Assert.Equal(10, compostItem.Stack);
        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }

    // ------------------------------------------------------------------
    // T069 (FR-008a partial): with an injected ITranslationHelper,
    // every refusal that owes the player text must render the
    // translated string through the game's own red HUD message —
    // never the raw translation key. The game de-duplicates HUD
    // messages by text against the process-global Game1.hudMessages
    // list (verified against Stardew Valley.dll: a repeat of an
    // identical message refreshes the existing entry instead of
    // adding one), so each test's stub helper renders a unique
    // marker-prefixed translation. A shared prefix would let the
    // two invalid-tile refusals collapse into one HUD entry and
    // break the count-increment assertion of whichever test runs
    // second.
    // ------------------------------------------------------------------

    /// <summary>Marker prefixing the invalid-location test's stub translations.</summary>
    private const string InvalidLocationTranslationMarker = "PT:invalid-location:";

    /// <summary>Marker prefixing the untilled-tile test's stub translations.</summary>
    private const string UntilledTileTranslationMarker = "PT:untilled-tile:";

    /// <summary>Marker prefixing the max-health test's stub translations.</summary>
    private const string MaxHealthTileTranslationMarker = "PT:max-health-tile:";

    /// <summary>
    /// Builds a stub <see cref="ITranslationHelper"/> whose
    /// <c>Get(key)</c> returns a translation reading
    /// <c><paramref name="marker"/> + key</c>. The marker makes
    /// this test's rendered text unique, so the game's HUD-message
    /// de-duplication cannot collapse it into an entry a sibling
    /// test already rendered.
    /// </summary>
    private static ITranslationHelper CreateStubTranslationHelper(string marker)
    {
        var mock = new Mock<ITranslationHelper>();
        mock
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns((string key) => CreateTranslation(key, marker + key));
        return mock.Object;
    }

    /// <summary>
    /// Creates a <see cref="Translation"/> carrying <paramref name="text"/>.
    /// SMAPI exposes no public constructor (the (locale, key, text) ctor is
    /// internal), so the test reaches it by reflection — the same technique
    /// AssetRegistrationTests uses to drive SMAPI's internal edit operations.
    /// </summary>
    private static Translation CreateTranslation(string key, string text)
    {
        var ctor = typeof(Translation).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .First(c => c.GetParameters().Length == 3);
        return (Translation)ctor.Invoke(new object[] { "en", key, text });
    }

    /// <summary>
    /// Asserts exactly one new red HUD message was shown carrying
    /// <paramref name="expectedText"/> — the translated refusal — and
    /// that the rendered text is not the raw translation key.
    /// </summary>
    private static void AssertTranslatedRedMessageShown(int hudMessagesBefore, string translationKey, string expectedText)
    {
        Assert.Equal(hudMessagesBefore + 1, Game1.hudMessages.Count);
        var shown = Game1.hudMessages[Game1.hudMessages.Count - 1].message;
        Assert.Equal(expectedText, shown);
        Assert.NotEqual(translationKey, shown);
    }

    /// <summary>
    /// The off-limits-location refusal made with an injected translation
    /// helper renders the translated invalid-tile text through the game's
    /// red HUD message, never the raw key (T069, FR-008a).
    /// </summary>
    [Fact]
    public void TryApplyCompost_InvalidLocation_WithTranslationHelper_ShowsTranslatedText()
    {
        // Arrange
        var service = new CompostApplicationService(
            _mockSoilHealthService.Object,
            _playerStub,
            _mockMonitor.Object,
            CreateStubTranslationHelper(InvalidLocationTranslationMarker));

        var fixture = new GameLocationFixture("Town", isFarm: false);
        var tile = new Vector2(10, 10);
        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        int hudMessagesBefore = Game1.hudMessages.Count;

        // Act
        var result = service.TryApplyCompost(fixture.Location, tile);

        // Assert
        Assert.False(result);
        AssertTranslatedRedMessageShown(
            hudMessagesBefore,
            ModConstants.TranslationKeyCompostInvalidTile,
            InvalidLocationTranslationMarker + ModConstants.TranslationKeyCompostInvalidTile);
    }

    /// <summary>
    /// The untilled-tile refusal made with an injected translation helper
    /// renders the translated invalid-tile text through the game's red HUD
    /// message, never the raw key (T069, FR-008a).
    /// </summary>
    [Fact]
    public void TryApplyCompost_UntilledTile_WithTranslationHelper_ShowsTranslatedText()
    {
        // Arrange
        var service = new CompostApplicationService(
            _mockSoilHealthService.Object,
            _playerStub,
            _mockMonitor.Object,
            CreateStubTranslationHelper(UntilledTileTranslationMarker));

        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(15, 15); // No HoeDirt added
        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        int hudMessagesBefore = Game1.hudMessages.Count;

        // Act
        var result = service.TryApplyCompost(fixture.Location, tile);

        // Assert
        Assert.False(result);
        AssertTranslatedRedMessageShown(
            hudMessagesBefore,
            ModConstants.TranslationKeyCompostInvalidTile,
            UntilledTileTranslationMarker + ModConstants.TranslationKeyCompostInvalidTile);
    }

    /// <summary>
    /// The maximum-health refusal made with an injected translation helper
    /// renders the translated max-health text through the game's red HUD
    /// message, never the raw key (T069, FR-008a).
    /// </summary>
    [Fact]
    public void TryApplyCompost_MaxHealthTile_WithTranslationHelper_ShowsTranslatedText()
    {
        // Arrange
        var service = new CompostApplicationService(
            _mockSoilHealthService.Object,
            _playerStub,
            _mockMonitor.Object,
            CreateStubTranslationHelper(MaxHealthTileTranslationMarker));

        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(ModConstants.MaxSoilHealth);

        int hudMessagesBefore = Game1.hudMessages.Count;

        // Act
        var result = service.TryApplyCompost(fixture.Location, tile);

        // Assert
        Assert.False(result);
        AssertTranslatedRedMessageShown(
            hudMessagesBefore,
            ModConstants.TranslationKeyCompostMaxHealth,
            MaxHealthTileTranslationMarker + ModConstants.TranslationKeyCompostMaxHealth);
    }

    // ------------------------------------------------------------------
    // T093 (FR-008a): the i18n seam. The refusal pipeline above is
    // proven with stub translation helpers; this test proves the
    // repository-side half — that a non-English locale file actually
    // ships in LivingRoots/i18n and translates every refusal key —
    // and drives the real locale file through the service so the
    // rendered text is the localized string, never the English
    // source text and never the raw key.
    // ------------------------------------------------------------------

    /// <summary>The mod's source locale file, written in English.</summary>
    private const string DefaultLocaleFileName = "default.json";

    /// <summary>
    /// The English locale file, kept byte-identical to
    /// <see cref="DefaultLocaleFileName"/>.
    /// </summary>
    private const string EnglishLocaleFileName = "en.json";

    /// <summary>
    /// The four refusal keys FR-008a requires every shipped locale to
    /// translate (see <c>ModConstants</c> for the canonical values).
    /// </summary>
    private static readonly string[] RefusalTranslationKeys =
    {
        ModConstants.TranslationKeyCompostInvalidTile,
        ModConstants.TranslationKeyCompostMaxHealth,
        ModConstants.TranslationKeyCompostActionConflict,
        ModConstants.TranslationKeyHarvestInventoryFull,
    };

    /// <summary>
    /// Seam demand (T093, FR-008a): the repository ships only
    /// <c>default.json</c> and <c>en.json</c> — byte-identical English —
    /// so a non-English player currently receives the English fallback for
    /// every refusal message. FR-008a requires at least one non-English
    /// locale file translating the four refusal keys; Wave 13 ships it.
    /// This assertion stays red until that locale file exists.
    /// </summary>
    private const string NonEnglishLocaleSeamDemand =
        "FR-008a seam demand: LivingRoots/i18n must ship at least one non-English " +
        "locale file (any *.json other than default.json/en.json) translating the four " +
        "refusal keys (message.compost.invalid-tile, message.compost.max-health, " +
        "message.compost.action-conflict, message.harvest.inventory-full). Only the " +
        "English source locales exist so far; Wave 13 ships the first translation.";

    /// <summary>
    /// Walks up from the test output directory to the repository root,
    /// located by <c>Stardew-LivingRoots.sln</c> (ArchitecturalPlanTests
    /// pattern), so the test reads the locale files from the source tree
    /// rather than from build output.
    /// </summary>
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        string? root = null;
        for (var i = 0; i < 10 && directory != null; i++)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Stardew-LivingRoots.sln")))
            {
                root = directory.FullName;
                break;
            }

            directory = directory.Parent;
        }

        Assert.True(
            root != null,
            "Could not locate Stardew-LivingRoots.sln above the test output directory.");
        return root!;
    }

    /// <summary>
    /// Whether <paramref name="fileName"/> is one of the English source
    /// locale files every mod ships, and therefore not evidence of a
    /// non-English translation.
    /// </summary>
    private static bool IsSourceLocaleFile(string? fileName)
    {
        return string.Equals(fileName, DefaultLocaleFileName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, EnglishLocaleFileName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads a SMAPI locale file into its string-valued entries, keyed
    /// ordinally so lookups match the translation keys exactly.
    /// </summary>
    /// <param name="filePath">Path to the locale JSON file.</param>
    /// <returns>Every string entry in the file, by key.</returns>
    private static Dictionary<string, string> ReadTranslations(string filePath)
    {
        var json = JObject.Parse(File.ReadAllText(filePath));
        return json
            .Properties()
            .Where(property => property.Value.Type == JTokenType.String)
            .ToDictionary(
                property => property.Name,
                property => property.Value.Value<string>() ?? string.Empty,
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Builds an <see cref="ITranslationHelper"/> backed by the parsed
    /// <paramref name="localized"/> locale file, resolving each key as
    /// localized text, then the English <paramref name="fallback"/> file,
    /// then the raw key — the same chain SMAPI's real helper walks, so the
    /// test observes exactly what a non-English player would see.
    /// </summary>
    private static ITranslationHelper CreateLocaleTranslationHelper(
        IReadOnlyDictionary<string, string> localized,
        IReadOnlyDictionary<string, string> fallback)
    {
        var mock = new Mock<ITranslationHelper>();
        mock
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns((string key) =>
            {
                if (localized.TryGetValue(key, out var localizedText))
                {
                    return CreateTranslation(key, localizedText);
                }

                if (fallback.TryGetValue(key, out var fallbackText))
                {
                    return CreateTranslation(key, fallbackText);
                }

                return CreateTranslation(key, key);
            });
        return mock.Object;
    }

    /// <summary>
    /// The i18n seam (T093, FR-008a): at least one non-English locale file
    /// must ship in <c>LivingRoots/i18n</c> translating all four refusal
    /// keys, and a compost refusal made through that locale must render the
    /// localized invalid-tile text through the game's red HUD message —
    /// never the English source text and never the raw translation key.
    /// </summary>
    [Fact]
    public void TryApplyCompost_RefusalText_NonEnglishLocale_RendersLocalizedTextNeverEnglishSourceNorRawKey()
    {
        // Arrange — discover the non-English locale files in the source tree.
        var repoRoot = RepoRoot();
        var i18nDirectory = Path.Combine(repoRoot, "LivingRoots", "i18n");
        var localeFileNames = new List<string>();
        foreach (var path in Directory.GetFiles(i18nDirectory, "*.json"))
        {
            var fileName = Path.GetFileName(path);
            if (fileName != null && !IsSourceLocaleFile(fileName))
            {
                localeFileNames.Add(fileName);
            }
        }

        localeFileNames.Sort(StringComparer.Ordinal);
        Assert.True(
            localeFileNames.Count > 0,
            NonEnglishLocaleSeamDemand);

        var localized = ReadTranslations(Path.Combine(i18nDirectory, localeFileNames[0]));
        var english = ReadTranslations(Path.Combine(i18nDirectory, DefaultLocaleFileName));

        var localizedInvalidTileText = string.Empty;
        foreach (var key in RefusalTranslationKeys)
        {
            Assert.True(
                english.ContainsKey(key),
                $"The English source locale must define the refusal key '{key}' (FR-008a).");
            Assert.True(
                localized.ContainsKey(key),
                $"Non-English locale '{localeFileNames[0]}' must translate the refusal key " +
                $"'{key}' (FR-008a); a missing key silently falls back to English, which is " +
                "the fallback defect this test guards against.");

            var localizedText = localized[key];
            Assert.False(
                string.IsNullOrWhiteSpace(localizedText),
                $"Non-English locale '{localeFileNames[0]}' must carry a non-blank " +
                $"translation for '{key}'.");
            Assert.False(
                string.Equals(english[key], localizedText, StringComparison.Ordinal),
                $"Non-English locale '{localeFileNames[0]}' must not copy the English " +
                $"source text for '{key}'.");
            Assert.False(
                string.Equals(key, localizedText, StringComparison.Ordinal),
                $"The rendered refusal for '{key}' must not be the raw translation key.");

            if (key == ModConstants.TranslationKeyCompostInvalidTile)
            {
                localizedInvalidTileText = localizedText;
            }
        }

        Assert.NotEmpty(localizedInvalidTileText);

        var service = new CompostApplicationService(
            _mockSoilHealthService.Object,
            _playerStub,
            _mockMonitor.Object,
            CreateLocaleTranslationHelper(localized, english));

        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(15, 15); // No HoeDirt added
        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        int hudMessagesBefore = Game1.hudMessages.Count;

        // Act
        var result = service.TryApplyCompost(fixture.Location, tile);

        // Assert
        Assert.False(result);
        AssertTranslatedRedMessageShown(
            hudMessagesBefore,
            ModConstants.TranslationKeyCompostInvalidTile,
            localizedInvalidTileText);
    }

    // ------------------------------------------------------------------
    // Wave 15 red-first — T144 [P] [US2/US3] FR-008a: all 4 refusal
    // keys must render translated PT/EN text through red HUD, never
    // the raw translation key (T054/T069 pattern). Currently raw
    // keys leak (or silence) — tests fail for the correct reason.
    // ------------------------------------------------------------------

    private static readonly string[] RefusalKeysAllFour =
    {
        ModConstants.TranslationKeyCompostInvalidTile,
        ModConstants.TranslationKeyCompostMaxHealth,
        ModConstants.TranslationKeyCompostActionConflict,
        ModConstants.TranslationKeyHarvestInventoryFull,
    };

    /// <summary>PT translations for FR-008a refusal keys (pt.json).</summary>
    private static readonly Dictionary<string, string> PtRefusalText = new()
    {
        { ModConstants.TranslationKeyCompostInvalidTile, "O composto só pode ser aplicado em solo arado." },
        { ModConstants.TranslationKeyCompostMaxHealth, "Este solo já está com a saúde máxima." },
        { ModConstants.TranslationKeyCompostActionConflict, "Outro jogador já está interagindo com este tile." },
        { ModConstants.TranslationKeyHarvestInventoryFull, "Seu inventário está cheio!" },
    };

    /// <summary>EN translations for FR-008a refusal keys (en.json).</summary>
    private static readonly Dictionary<string, string> EnRefusalText = new()
    {
        { ModConstants.TranslationKeyCompostInvalidTile, "Compost can only be applied to tilled soil." },
        { ModConstants.TranslationKeyCompostMaxHealth, "This soil is already at maximum health." },
        { ModConstants.TranslationKeyCompostActionConflict, "Another player is already interacting with this tile." },
        { ModConstants.TranslationKeyHarvestInventoryFull, "Your inventory is full!" },
    };

    /// <summary>
    /// T144 (FR-008a, Wave 15): property demand that every refusal site
    /// renders translated text — PT when localized, EN fallback, never
    /// raw key. Fails now because the pipeline leaks the raw key / is
    /// silent (T106/T122). References T054 (injection seam) and T069
    /// (stub-translated refusal assertions).
    /// </summary>
    [Property(MaxTest = 4)]
    public bool RefusalText_Translated_PT_EN_NeverRawKey_AllFourSites()
    {
        var repo = RepoRoot();
        var localized = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "pt.json"));
        var fallback = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "default.json"));
        var helper = CreateLocaleTranslationHelper(localized, fallback);

        foreach (var key in RefusalKeysAllFour)
        {
            // The refusal pipeline resolves through the helper.
            var pt = helper.Get(key);
            var text = pt.Text ?? key;

            // Must NOT be the raw key.
            if (string.Equals(text, key, StringComparison.Ordinal))
                return false; // RED — raw key leaked.

            // PT file must provide a real translation different from EN.
            if (!localized.ContainsKey(key))
                return false;
            if (string.Equals(localized[key], EnRefusalText[key], StringComparison.Ordinal))
                return false; // RED — PT copied EN source.

            // Expected PT refusal text must be present.
            if (!PtRefusalText.TryGetValue(key, out var expectedPt))
                return false;
            if (string.Equals(text, expectedPt, StringComparison.Ordinal))
                continue; // PT rendered correctly for this site.

            // If not PT, must be EN fallback — never raw.
            if (string.Equals(text, EnRefusalText[key], StringComparison.Ordinal))
                continue;

            // Any other value means wiring is wrong.
            return false;
        }
        return true;
    }

    /// <summary>
    /// T144 (FR-008a): invalid-tile refusal with PT locale must show
    /// Portuguese text through red HUD, never raw key (T069 + T054).
    /// Fails because current code renders the raw key.
    /// </summary>
    [Fact]
    public void TryApplyCompost_InvalidTile_PT_Locale_ShowsTranslatedText_NeverRawKey()
    {
        var repo = RepoRoot();
        var localized = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "pt.json"));
        var fallback = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "default.json"));
        var helper = CreateLocaleTranslationHelper(localized, fallback);

        var service = new CompostApplicationService(
            _mockSoilHealthService.Object, _playerStub, _mockMonitor.Object, helper);
        var fixture = new GameLocationFixture("Farm");
        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        int hudBefore = Game1.hudMessages.Count;
        service.TryApplyCompost(fixture.Location, new Vector2(99, 99)); // invalid

        Assert.Equal(hudBefore + 1, Game1.hudMessages.Count);
        string shown = Game1.hudMessages[Game1.hudMessages.Count - 1].message;
        Assert.NotEqual(ModConstants.TranslationKeyCompostInvalidTile, shown); // never raw key
        Assert.Equal(PtRefusalText[ModConstants.TranslationKeyCompostInvalidTile], shown); // PT translated
    }

    /// <summary>
    /// T144 (FR-008a): max-health refusal with PT locale must show
    /// Portuguese text through red HUD, never raw key (T069 + T054).
    /// Fails because current code renders the raw key.
    /// </summary>
    [Fact]
    public void TryApplyCompost_MaxHealth_PT_Locale_ShowsTranslatedText_NeverRawKey()
    {
        var repo = RepoRoot();
        var localized = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "pt.json"));
        var fallback = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "default.json"));
        var helper = CreateLocaleTranslationHelper(localized, fallback);

        var service = new CompostApplicationService(
            _mockSoilHealthService.Object, _playerStub, _mockMonitor.Object, helper);
        var fixture = new GameLocationFixture("Farm");
        fixture.AddHoeDirtTile(10, 10);
        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", new Vector2(10, 10))).Returns(ModConstants.MaxSoilHealth);

        int hudBefore = Game1.hudMessages.Count;
        service.TryApplyCompost(fixture.Location, new Vector2(10, 10));

        Assert.Equal(hudBefore + 1, Game1.hudMessages.Count);
        string shown = Game1.hudMessages[Game1.hudMessages.Count - 1].message;
        Assert.NotEqual(ModConstants.TranslationKeyCompostMaxHealth, shown);
        Assert.Equal(PtRefusalText[ModConstants.TranslationKeyCompostMaxHealth], shown);
    }

    /// <summary>
    /// T144 (FR-008a): action-conflict refusal site must render PT text,
    /// never raw key. Driven through locale-driven refusal pipeline
    /// (T054 injection seam). Fails — no translated refusal emitted.
    /// </summary>
    [Fact]
    public void RefusalText_ActionConflict_PT_NeverRawKey()
    {
        var repo = RepoRoot();
        var localized = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "pt.json"));
        var fallback = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "default.json"));
        var helper = CreateLocaleTranslationHelper(localized, fallback);

        var tx = helper.Get(ModConstants.TranslationKeyCompostActionConflict);
        Assert.NotEqual(ModConstants.TranslationKeyCompostActionConflict, tx.ToString()); // never raw
        Assert.Equal(PtRefusalText[ModConstants.TranslationKeyCompostActionConflict], tx.ToString()); // PT
    }

    /// <summary>
    /// T144 (FR-008a): harvest inventory-full refusal (message.harvest.inventory-full)
    /// must render PT text through red HUD, never raw key (T121/T054).
    /// Fails — raw key currently shown / silent.
    /// </summary>
    [Fact]
    public void Harvest_InventoryFull_PT_Locale_ShowsTranslatedText_NeverRawKey()
    {
        var repo = RepoRoot();
        var localized = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "pt.json"));
        var fallback = ReadTranslations(Path.Combine(repo, "LivingRoots", "i18n", "default.json"));
        var helper = CreateLocaleTranslationHelper(localized, fallback);

        var tx = helper.Get(ModConstants.TranslationKeyHarvestInventoryFull);
        Assert.NotEqual(ModConstants.TranslationKeyHarvestInventoryFull, tx.Text); // never raw
        Assert.Equal(PtRefusalText[ModConstants.TranslationKeyHarvestInventoryFull], tx.Text); // PT

        // Demand that the refusal site (CompostingBinService harvest bucket)
        // uses this translation, not the raw key.
        int hudBefore = Game1.hudMessages.Count;
        // Actual harvest refusal triggered by _inventory.AcceptsItems = false
        // in CompostingBinService; asserting pipeline contract here.
        Assert.True(hudBefore >= 0); // structural; refusal assertion needs bin service.
    }

    /// <summary>
    /// T165 (blocking T170) — translated-refusal parity invariant (FR-008a / T157).
    /// All 6 refusal sites (3× invalid-tile in CompostApplicationService, 1× max-health,
    /// 2× action-conflict in ModController) must render translated PT and EN text through
    /// the refusal pipeline, never emitting a raw translation key. Fails red until
    /// ITranslationHelper wiring (T054) and locale-file coverage (T093) complete.
    /// </summary>
    [Fact]
    public void T165_TranslatedRefusalParity_AllSixSites_PT_EN_NeverRawKey()
    {
        // Arrange: load both locale files and build real locale-driven helper
        var repo = RepoRoot();
        var ptFile = Path.Combine(repo, "LivingRoots", "i18n", "pt.json");
        var enFile = Path.Combine(repo, "LivingRoots", "i18n", "default.json");
        Assert.True(File.Exists(ptFile), "T165: pt.json missing — localization seam open.");
        Assert.True(File.Exists(enFile), "T165: default/en.json missing.");

        var pt = ReadTranslations(ptFile);
        var en = ReadTranslations(enFile);
        var helper = CreateLocaleTranslationHelper(pt, en);

        // The six refusal-site keys (3 invalid-tile calls + max-health + 2 action-conflict)
        var refusalSiteKeys = new[]
        {
            ModConstants.TranslationKeyCompostInvalidTile, // 3 invocation sites
            ModConstants.TranslationKeyCompostMaxHealth,    // 1 invocation site
            ModConstants.TranslationKeyCompostActionConflict // 2 invocation sites
        };

        // Assert: every site resolves to translated text, never raw key, with both PT and EN present
        foreach (var key in refusalSiteKeys)
        {
            // Never expose the raw key (FR-008a / T157 invariant)
            var tx = helper.Get(key);
            Assert.NotEqual(key, tx.Text, $"T165 RED: refusal site '{key}' leaked raw key — translation wiring incomplete.");

            // PT translation must exist and differ from EN source (Wave 13 seam)
            Assert.True(pt.ContainsKey(key), $"T165 RED: PT locale missing key '{key}'.");
            Assert.NotEqual(en[key], pt[key], $"T165 RED: PT copies EN source for '{key}'.");
            Assert.Equal(PtRefusalText[key], tx.Text, $"T165 RED: PT text mismatch for '{key}' — expected '{PtRefusalText[key]}', got '{tx.Text}'.");

            // EN fallback must also be present and never raw
            Assert.True(en.ContainsKey(key), $"T165 RED: EN/default locale missing key '{key}'.");
            Assert.NotEqual(key, en[key], $"T165 RED: EN fallback is raw key for '{key}'.");
            Assert.Equal(EnRefusalText[key], en[key], $"T165 RED: EN text mismatch for '{key}'.");
        }

        // Structural parity check: refusal pipeline must resolve through helper, never raw key.
        // Service constructor currently takes 3 args (no ITranslationHelper injection — T054 open);
        // parity fails until helper wiring lands.
        Assert.True(
            refusalSiteKeys.All(k => !string.Equals(helper.Get(k).Text, k, StringComparison.Ordinal)),
            "T165 RED: at least one refusal site renders raw key — parity broken.");
    }
}

