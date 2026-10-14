using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using LivingRoots.Tests.Fixtures;
using Moq;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Tests for <see cref="HudNotifierImpl"/> rendering translated text (FR-008a, F12, T232).
/// </summary>
/// <remarks>
/// <para>
/// Template: modelled on the PT/EN refusal-text test
/// <c>TryApplyCompost_RefusalText_NonEnglishLocale_RendersLocalizedTextNeverEnglishSourceNorRawKey</c>
/// (CompostApplicationServiceTests.cs:572-650, T093) — the same
/// <c>pt.json</c> locale harness (<c>RepoRoot</c>, <c>ReadTranslations</c>,
/// locale-backed <see cref="ITranslationHelper"/>) and the same red-HUD
/// capture: the rendered message must be the localized string, never the
/// English source text and never the raw translation key.
/// </para>
/// <para>
/// <b>Prerequisite:</b> <see cref="GameStateFixture"/> is installed by this
/// class's constructor. <c>Game1.showRedMessage</c> appends to the
/// process-global <c>Game1.hudMessages</c> list, which is what the assertion
/// below reads — the same capture the T069/T093 refusal tests rely on.
/// </para>
/// <para>
/// Fixed key assertion — property-based N/A: one notice, one key, no input
/// domain to generate over.
/// </para>
/// </remarks>
public class HudNotifierTranslationTests
{
    /// <summary>
    /// Proposed i18n key for the accessibility-degradation notice. T258 must
    /// ship exactly this key in <c>LivingRoots/i18n/default.json</c>,
    /// <c>en.json</c> and <c>pt.json</c>; if green chooses another name,
    /// update this constant.
    /// </summary>
    private const string AccessibilityPatternsDegradedKey = "message.accessibility.patterns-hidden";

    /// <summary>Visible-tile count driving the notice (above the degradation threshold).</summary>
    private const int VisibleTileCount = 1500;

    /// <summary>Installs the minimum required game state.</summary>
    public HudNotifierTranslationTests()
    {
        GameStateFixture.Install();
    }

    /// <summary>
    /// Constructs the notifier under test. T258 handoff: production takes no
    /// translation helper today, so this is the parameterless construction;
    /// when T258 adds the <see cref="ITranslationHelper"/>-taking overload,
    /// this factory gains the locale helper and every assertion below stands
    /// unchanged.
    /// </summary>
    /// <returns>The notifier under test.</returns>
    private static IHudNotifier CreateNotifier()
    {
        return new HudNotifierImpl();
    }

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
    /// T232 (FR-008a, F12): the accessibility-degradation notice renders
    /// translated text — a non-English locale returns non-English text,
    /// never the raw key and never the English source text.
    /// </summary>
    /// <remarks>
    /// RED today: <see cref="HudNotifierImpl"/> renders hardcoded English
    /// with no i18n key and no <see cref="ITranslationHelper"/>
    /// (HudNotifierImpl.cs:21), and no locale file ships the notice key —
    /// so the locale-file assertions and the rendered-text equality below
    /// all fail for that reason. Green (T258: helper injection plus the key
    /// in default/en/pt.json) makes every assertion pass.
    /// </remarks>
    [Fact]
    public void NotifyAccessibilityPatternsDegraded_NonEnglishLocale_RendersTranslatedTextNeverRawKeyNorEnglishSource()
    {
        // Arrange — the non-English locale served through a translation helper.
        string repoRoot = RepoRoot();
        var localized = ReadTranslations(Path.Combine(repoRoot, "LivingRoots", "i18n", "pt.json"));
        var fallback = ReadTranslations(Path.Combine(repoRoot, "LivingRoots", "i18n", "default.json"));
        ITranslationHelper localeHelper = CreateLocaleTranslationHelper(localized, fallback);
        string expectedText = localeHelper.Get(AccessibilityPatternsDegradedKey).ToString();

        int hudMessagesBefore = Game1.hudMessages.Count;

        // Act
        CreateNotifier().NotifyAccessibilityPatternsDegraded(VisibleTileCount);

        // Assert — exactly one new red HUD message was shown.
        Assert.Equal(hudMessagesBefore + 1, Game1.hudMessages.Count);
        string shown = Game1.hudMessages[Game1.hudMessages.Count - 1].message;

        // The locale files must ship the notice key with a real non-English
        // translation: present, non-blank, not the English source text, not
        // the raw key.
        Assert.True(
            localized.ContainsKey(AccessibilityPatternsDegradedKey),
            "T232 RED (FR-008a): LivingRoots/i18n/pt.json must ship a non-English translation for '" +
            AccessibilityPatternsDegradedKey + "' (T258). The notice is currently hardcoded English: " +
            "\"Accessibility patterns hidden: too many tiles visible (1500).\"");
        Assert.True(
            fallback.ContainsKey(AccessibilityPatternsDegradedKey),
            "T232 RED (FR-008a): LivingRoots/i18n/default.json must define '" +
            AccessibilityPatternsDegradedKey + "' (T258).");

        string localizedText = localized[AccessibilityPatternsDegradedKey];
        string englishText = fallback[AccessibilityPatternsDegradedKey];
        Assert.False(
            string.IsNullOrWhiteSpace(localizedText),
            "T232 RED (FR-008a): pt.json must carry a non-blank translation for '" +
            AccessibilityPatternsDegradedKey + "'.");
        Assert.False(
            string.Equals(englishText, localizedText, StringComparison.Ordinal),
            "T232 RED (FR-008a): pt.json must not copy the English source text for '" +
            AccessibilityPatternsDegradedKey + "'.");
        Assert.False(
            string.Equals(AccessibilityPatternsDegradedKey, localizedText, StringComparison.Ordinal),
            "T232 RED (FR-008a): the pt.json entry for '" +
            AccessibilityPatternsDegradedKey + "' must not be the raw translation key.");

        // The rendered message is that localized text — never the raw key
        // and never the English source text.
        Assert.Equal(expectedText, shown);
        Assert.Equal(localizedText, shown);
        Assert.NotEqual(AccessibilityPatternsDegradedKey, shown);
        Assert.NotEqual(englishText, shown);
    }
}
