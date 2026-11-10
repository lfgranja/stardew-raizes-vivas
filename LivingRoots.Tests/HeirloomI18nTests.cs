using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Spec 020 i18n parity tests (FR-011, US3): the crioulo item/seed copy and the
/// heirloom feedback messages ship in <c>default.json</c>, <c>en.json</c>, and
/// <c>pt.json</c> with meaning parity and an agronomically honest vocabulary.
/// </summary>
/// <remarks>
/// These are contract-driven lexical proxies. They prove the shipped strings avoid
/// forbidden vocabulary (nitrogen claims; Pierre vilification; crioulo triumphalism
/// that would instruct the player wrongly) and that all three locales carry every
/// new key. They cannot prove semantic meaning, nuance, or tone — manual review
/// remains required for copy quality. See research/2026-11-09-heirloom-seeds-pipeline.md
/// §7 (honest game-design mapping).
/// </remarks>
public class HeirloomI18nTests
{
    private static readonly string[] LocaleFileNames = { "default.json", "en.json", "pt.json" };

    private static readonly string[] RequiredHeirloomKeys =
    {
        ModConstants.HeirloomSeedNameKey,
        ModConstants.HeirloomSeedDescriptionKey,
        ModConstants.HeirloomProduceNameKey,
        ModConstants.HeirloomProduceDescriptionKey,
        ModConstants.TranslationKeyHeirloomSeedSaved,
        ModConstants.TranslationKeyHeirloomSeedSavedInventoryFull,
        ModConstants.TranslationKeyHeirloomPlantBonusAuthorityUnsupported
    };

    // Nitrogen-fixation claims are only valid for legumes. The crioulo parsnip is a
    // root/vegetable (Apiaceae), never a legume: these tokens must not appear in its
    // player-facing copy in any locale.
    private static readonly string[] ForbiddenNitrogenTokens =
    {
        "fix nitrogen", "fixes nitrogen", "fixing nitrogen", "nitrogen fixation",
        "nitrogen-fixing", "nitrogênio", "fixar nitrogênio", "fixador de nitrogênio",
        "fijar nitrógeno", "fijación de nitrógeno", "azoto", "azinheiro"
    };

    // Pierre must remain a valid choice for high-input systems. Copy that vilifies
    // hybrids violates the project's framing rules.
    private static readonly string[] ForbiddenPierreVilificationTokens =
    {
        "evil", "mal", "bad Pierre", "Pierre está errado", "Pierre is wrong",
        "slave", "slavery", "esclav", "escraviz", "monopoly man", "capitalista"
    };

    private static string LocaleRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, "LivingRoots", "i18n");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException("Could not locate LivingRoots/i18n from test base directory.");
    }

    [Fact]
    public void AllHeirloomKeysExistInAllThreeLocales()
    {
        var root = LocaleRoot();
        foreach (var file in LocaleFileNames)
        {
            var path = Path.Combine(root, file);
            Assert.True(File.Exists(path), $"missing locale file {path}");
            var json = JObject.Parse(File.ReadAllText(path));
            foreach (var key in RequiredHeirloomKeys)
            {
                Assert.True(json.Property(key) != null, $"{file}: missing key '{key}'");
                var value = json[key]?.ToString();
                Assert.False(string.IsNullOrWhiteSpace(value), $"{file}: key '{key}' is empty");
            }
        }
    }

    [Fact]
    public void HeirloomCopyUsesNoNitrogenTokensInAnyLocale()
    {
        var root = LocaleRoot();
        foreach (var file in LocaleFileNames)
        {
            var json = JObject.Parse(File.ReadAllText(Path.Combine(root, file)));
            foreach (var key in RequiredHeirloomKeys)
            {
                var value = (json[key]?.ToString() ?? string.Empty).ToLowerInvariant();
                foreach (var token in ForbiddenNitrogenTokens)
                {
                    Assert.False(
                        value.Contains(token.ToLowerInvariant(), StringComparison.Ordinal),
                        $"{file} '{key}' contains forbidden nitrogen-fixation vocabulary '{token}' (not a legume; crioulo advantage is stability/quality, never N fixation).");
                }
            }
        }
    }

    [Fact]
    public void HeirloomCopyVilifiesPierreNowhere()
    {
        var root = LocaleRoot();
        foreach (var file in LocaleFileNames)
        {
            var json = JObject.Parse(File.ReadAllText(Path.Combine(root, file)));
            foreach (var key in RequiredHeirloomKeys)
            {
                var value = (json[key]?.ToString() ?? string.Empty).ToLowerInvariant();
                foreach (var token in ForbiddenPierreVilificationTokens)
                {
                    Assert.False(
                        value.Contains(token.ToLowerInvariant(), StringComparison.Ordinal),
                        $"{file} '{key}' contains forbidden Pierre/hybrid vilification vocabulary '{token}'.");
                }
            }
        }
    }

    [Fact]
    public void PortugueseCopyNamesTheCriouloCategoryCorrectly()
    {
        var root = LocaleRoot();
        var pt = JObject.Parse(File.ReadAllText(Path.Combine(root, "pt.json")));
        // The seed must be named as a crioula seed in PT (not "herança"/"ancestral"
        // calques or hybrid copy): the PT vocabulary is institucionais (Lei 10.711/2003).
        Assert.Contains("crioula", (pt[ModConstants.HeirloomSeedNameKey]?.ToString() ?? string.Empty).ToLowerInvariant(), StringComparison.Ordinal);
        Assert.Contains("crioulo", (pt[ModConstants.HeirloomProduceNameKey]?.ToString() ?? string.Empty).ToLowerInvariant(), StringComparison.Ordinal);
    }

    [Fact]
    public void EnglishAndPortugueseCopyStayFatualAboutTheGrowthTradeoff()
    {
        var root = LocaleRoot();
        // The 7-day crioulo cycle is slower than vanilla parsnip (4 days). Copy must not
        // promise "faster" growth; it must be compatible with a slower tradeoff framing.
        // We check the two locales that make explicit speed claims if any exist.
        foreach (var file in new[] { "en.json", "pt.json" })
        {
            var json = JObject.Parse(File.ReadAllText(Path.Combine(root, file)));
            var seedDescription = (json[ModConstants.HeirloomSeedDescriptionKey]?.ToString() ?? string.Empty).ToLowerInvariant();
            Assert.False(seedDescription.Contains("faster growth", StringComparison.Ordinal), $"{file} seed description must not promise faster growth (crioulo is slower by design).");
            Assert.False(seedDescription.Contains("crescimento mais rápido", StringComparison.Ordinal), $"{file} seed description must not promise faster growth (crioulo is slower by design).");
        }
    }

    [Fact]
    public void AllLocalesExposeExactlyTheSameHeirloomKeySet()
    {
        var root = LocaleRoot();
        var keySets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var file in LocaleFileNames)
        {
            var json = JObject.Parse(File.ReadAllText(Path.Combine(root, file)));
            keySets[file] = new HashSet<string>(
                RequiredHeirloomKeys.Where(key => json.Property(key) != null),
                StringComparer.Ordinal);
        }

        var reference = keySets[LocaleFileNames[0]];
        foreach (var file in LocaleFileNames.Skip(1))
        {
            Assert.Equal(reference, keySets[file]);
        }
    }
}
