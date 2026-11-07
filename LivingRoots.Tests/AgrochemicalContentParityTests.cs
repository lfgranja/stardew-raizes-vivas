using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// 017 i18n/content parity tests for the Joja/agrochemical tradeoff (T021-T022, plus
/// remediation RED coverage T065/T068/T081).
/// </summary>
/// <remarks>
/// <para>
/// Every assertion reads the three shipped locale files in <c>LivingRoots/i18n</c> and checks
/// the exact key set exposed by <see cref="ModConstants"/>. The 017 keys shipped in T031-T034,
/// so the original T021-T022 assertions are green regression guards. Assertions added during
/// the 017 remediation (T065 refusal structure, T068 bounded/recoverable legibility, T081
/// Portuguese magnitude/comparison copy) pin the updated copy contract and are expected RED
/// until Wave B ships the new strings.
/// </para>
/// <para>
/// T022 checks are contract-driven lexical proxies for the content contract: they prove the
/// shipped strings avoid mechanical-buff and shaming vocabulary, but they cannot prove semantic
/// meaning, nuance or tone. Manual review remains required (T052) and these tests must not be
/// reported as satisfying it.
/// </para>
/// <para>
/// Fallback behavior is deliberately out of scope here. The runtime fallback for
/// <c>ShowAgrochemicalFeedback</c> throwing or returning null (no false success, no duplicate
/// mutation/consumption/broadcast) is covered by controller tests T019 and must not be inferred
/// from i18n parity alone.
/// </para>
/// </remarks>
public class AgrochemicalContentParityTests
{
    private static readonly string[] LocaleFileNames =
    {
        "default.json",
        "en.json",
        "pt.json"
    };

    private static readonly string[] RequiredAgrochemicalKeys =
    {
        ModConstants.AgrochemicalItemNameKey,
        ModConstants.AgrochemicalItemDescriptionKey,
        ModConstants.TranslationKeyAgrochemicalApplied,
        ModConstants.TranslationKeyAgrochemicalAppliedClamped,
        ModConstants.TranslationKeyAgrochemicalAppliedUnknownBaseline,
        ModConstants.TranslationKeyAgrochemicalInvalidTarget,
        ModConstants.TranslationKeyAgrochemicalMissingInput,
        ModConstants.TranslationKeyAgrochemicalActionConflict,
        ModConstants.TranslationKeyAgrochemicalUnsupportedAuthority,
        ModConstants.TranslationKeyAgrochemicalSoilAtMinimum
    };

    private static readonly string[] SuccessAgrochemicalKeys =
    {
        ModConstants.TranslationKeyAgrochemicalApplied,
        ModConstants.TranslationKeyAgrochemicalAppliedClamped,
        ModConstants.TranslationKeyAgrochemicalAppliedUnknownBaseline
    };

    private static readonly string[] RefusalAgrochemicalKeys =
    {
        ModConstants.TranslationKeyAgrochemicalInvalidTarget,
        ModConstants.TranslationKeyAgrochemicalMissingInput,
        ModConstants.TranslationKeyAgrochemicalActionConflict,
        ModConstants.TranslationKeyAgrochemicalUnsupportedAuthority,
        ModConstants.TranslationKeyAgrochemicalSoilAtMinimum
    };

    private static readonly Dictionary<string, string[]> EnglishRefusalCauseTokens =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [ModConstants.TranslationKeyAgrochemicalInvalidTarget] = new[] { "tilled", "soil" },
            [ModConstants.TranslationKeyAgrochemicalMissingInput] = new[] { "agrochemical", "hold", "Joja" },
            [ModConstants.TranslationKeyAgrochemicalActionConflict] = new[] { "player", "tile" },
            [ModConstants.TranslationKeyAgrochemicalUnsupportedAuthority] = new[] { "host", "farmhand", "multiplayer" },
            [ModConstants.TranslationKeyAgrochemicalSoilAtMinimum] = new[] { "minimum", "already", "lowest" }
        };

    private static readonly Dictionary<string, string[]> PortugueseRefusalCauseTokens =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [ModConstants.TranslationKeyAgrochemicalInvalidTarget] = new[] { "arado", "solo" },
            [ModConstants.TranslationKeyAgrochemicalMissingInput] =
                new[] { "agroquímico", "agroquimico", "segur", "Joja" },
            [ModConstants.TranslationKeyAgrochemicalActionConflict] = new[] { "jogador", "tile" },
            [ModConstants.TranslationKeyAgrochemicalUnsupportedAuthority] = new[] { "anfitri", "host", "jogador" },
            [ModConstants.TranslationKeyAgrochemicalSoilAtMinimum] = new[] { "mínim", "minim", "já", "ja" }
        };

    private static readonly string[] EnglishRefusalNoSuccessTokens =
    {
        "not applied",
        "no success",
        "not used",
        "nothing happened"
    };

    private static readonly string[] PortugueseRefusalNoSuccessTokens =
    {
        "não foi aplicado",
        "não afetou",
        "nenhuma aplicação",
        "não consumido"
    };

    private static readonly string[] EnglishRefusalNextStepTokens =
    {
        "try",
        "again",
        "wait",
        "host",
        "regenerate",
        "care"
    };

    private static readonly string[] PortugueseRefusalNextStepTokens =
    {
        "tente",
        "tentar",
        "novamente",
        "de novo",
        "aguarde",
        "espere",
        "anfitri",
        "regener",
        "cuidado"
    };

    private static readonly string[] EnglishBoundedRecoverableTokens =
    {
        "recover",
        "care",
        "compost",
        "bounce back",
        "bounded",
        "temporary"
    };

    private static readonly string[] PortugueseBoundedRecoverableTokens =
    {
        "recuper",
        "cuidado",
        "compost",
        "limitad",
        "tempor"
    };

    private static readonly string[] PortugueseComparisonBoundTokens =
    {
        "menor",
        "que a penalidade",
        "completo",
        "inteira",
        "máxima"
    };

    // T097: Wave B introduces ModConstants.TranslationKeyAgrochemicalCommitFailed alongside the
    // key. It is deliberately referenced only through the constants-file token check below so
    // this file compiles (and the suite runs) before the constant exists; a direct reference
    // would break compilation of the whole test project ahead of Wave B.
    private const string CommitFailedTranslationKey = "message.agrochemical.commit-failed";

    private static readonly string[] EnglishCommitFailedNoSuccessTokens =
    {
        "not applied",
        "nothing happened",
        "failed",
        "could not",
        "did not"
    };

    private static readonly string[] PortugueseCommitFailedNoSuccessTokens =
    {
        "não foi aplicado",
        "não afetou",
        "nenhuma aplicação",
        "falhou",
        "não pôde",
        "não foi possível"
    };

    private static readonly string[] EnglishCommitFailedCauseTokens =
    {
        "could not be committed",
        "failed to apply",
        "not be saved",
        "commit"
    };

    private static readonly string[] PortugueseCommitFailedCauseTokens =
    {
        "não pôde ser aplicad",
        "não foi possível",
        "não foi salva",
        "falha",
        "falhou",
        "commit",
        "confirmar"
    };

    // Claim-of-consumption / claim-of-success tokens chosen so they cannot occur inside a
    // negated denial (e.g. "was not consumed", "não foi aplicado"); the required no-success
    // phrasing is the primary guard against success claims.
    private static readonly string[] EnglishCommitFailedNoClaimTokens =
    {
        "was consumed",
        "has been consumed",
        "is used up",
        "was applied",
        "applied successfully",
        "successfully applied",
        "succeeded"
    };

    private static readonly string[] PortugueseCommitFailedNoClaimTokens =
    {
        "consumido com sucesso",
        "utilizado com sucesso",
        "aplicado com sucesso",
        "com êxito"
    };

    private static readonly string[] EnglishCommitFailedNoBuffTokens =
    {
        "quality",
        "extra harvest",
        "growth",
        "fertilizer tier",
        "phase days"
    };

    private static readonly string[] PortugueseCommitFailedNoBuffTokens =
    {
        "qualidade",
        "colheita extra",
        "crescimento",
        "fertilizante",
        "fase"
    };

    private static readonly string[] EnglishMissingInputDiagnosisTokens =
    {
        "holding",
        "you need to be holding",
        "you are not holding",
        "hold a joja"
    };

    private static readonly string[] PortugueseMissingInputDiagnosisTokens =
    {
        "segurando",
        "você precisa segurar",
        "não está segurando",
        "segure um agroquímico"
    };

    private static readonly string[] ShamingTokens =
    {
        "stupid",
        "idiot",
        "shame",
        "greedy",
        "ruin",
        "destroy",
        "burro",
        "idiota",
        "vergonha",
        "ganancioso",
        "arruinar",
        "destruir"
    };

    /// <summary>
    /// T021 (FR-012): every 017 key from <see cref="ModConstants"/> must ship in every
    /// supported locale as a non-empty string.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void RequiredAgrochemicalKeys_ExistWithNonEmptyStringValues(string localeFileName)
    {
        var locale = LoadLocale(localeFileName);

        foreach (var key in RequiredAgrochemicalKeys)
        {
            RequireNonEmptyString(locale, localeFileName, key);
        }
    }

    /// <summary>
    /// T021 (FR-012): all three locale files must be JSON objects and expose the same 017 key
    /// set. Only the required 017 subset is compared; unrelated locale entries may differ.
    /// </summary>
    /// <remarks>
    /// <c>JObject.Parse</c> is also the object-shape assertion: an absent, malformed, or non-object
    /// document throws and fails this test before the key-set comparison.
    /// </remarks>
    [Fact]
    public void LocaleFiles_ParseAsJsonObjects_AndExposeIdentical017KeySets()
    {
        var expectedKeys = new HashSet<string>(RequiredAgrochemicalKeys, StringComparer.Ordinal);
        var observedKeysByLocale = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var localeFileName in LocaleFileNames)
        {
            var locale = LoadLocale(localeFileName);
            var observedKeys = new HashSet<string>(
                RequiredAgrochemicalKeys.Where(key => locale.Property(key) != null),
                StringComparer.Ordinal);
            observedKeysByLocale[localeFileName] = observedKeys;

            var missingKeys = expectedKeys
                .Except(observedKeys, StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();
            Assert.True(
                missingKeys.Length == 0,
                $"017 i18n parity: '{localeFileName}' is missing required agrochemical key(s): " +
                $"[{string.Join(", ", missingKeys)}]. default.json, en.json and pt.json must all " +
                "expose the same 017 key set.");
        }

        var referenceLocaleFileName = LocaleFileNames[0];
        var referenceKeys = observedKeysByLocale[referenceLocaleFileName];
        foreach (var localeFileName in LocaleFileNames.Skip(1))
        {
            var observedKeys = observedKeysByLocale[localeFileName];
            var missingFromLocale = referenceKeys
                .Except(observedKeys, StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();
            var extraInLocale = observedKeys
                .Except(referenceKeys, StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                missingFromLocale.Length == 0 && extraInLocale.Length == 0,
                $"017 i18n parity: '{referenceLocaleFileName}' and '{localeFileName}' must expose " +
                "the same 017 key set. " +
                $"Only in '{referenceLocaleFileName}': [{string.Join(", ", missingFromLocale)}]. " +
                $"Only in '{localeFileName}': [{string.Join(", ", extraInLocale)}].");
        }
    }

    /// <summary>
    /// T021 (FR-002/FR-012): the item description must telegraph both the Joja/industrial
    /// shortcut and its soil-health cost, using locale-appropriate vocabulary.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void ItemDescription_TelegraphsSoilHealthCostAndIndustrialShortcut(string localeFileName)
    {
        const string key = ModConstants.AgrochemicalItemDescriptionKey;
        var description = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, key);

        if (IsPortuguese(localeFileName))
        {
            Assert.True(
                ContainsToken(description, "solo"),
                $"017 i18n ({localeFileName}, '{key}'): the Portuguese item description must " +
                $"name the soil ('solo') cost; got \"{description}\".");
            Assert.True(
                ContainsAnyToken(description, new[] { "saúde", "saude" }),
                $"017 i18n ({localeFileName}, '{key}'): the Portuguese item description must " +
                $"name the soil-health ('saúde'/'saude') cost; got \"{description}\".");
            AssertContainsAny(
                localeFileName,
                key,
                description,
                "the Joja/industrial shortcut",
                "atalho",
                "industrial",
                "Joja");
            return;
        }

        Assert.True(
            ContainsToken(description, "soil"),
            $"017 i18n ({localeFileName}, '{key}'): the item description must name the soil cost; " +
            $"got \"{description}\".");
        Assert.True(
            ContainsToken(description, "health"),
            $"017 i18n ({localeFileName}, '{key}'): the item description must name the soil-health " +
            $"cost; got \"{description}\".");
        AssertContainsAny(
            localeFileName,
            key,
            description,
            "the Joja/industrial shortcut",
            "shortcut",
            "industrial",
            "Joja");
    }

    /// <summary>
    /// T021 (FR-008/FR-012): every refusal key must exist, be non-empty, and name its
    /// refusal cause using key-specific vocabulary in every locale.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void RefusalMessages_AreNonEmptyAndNameTheirRefusalCause(string localeFileName)
    {
        var locale = LoadLocale(localeFileName);
        var portuguese = IsPortuguese(localeFileName);
        var tokensByKey = portuguese ? PortugueseRefusalCauseTokens : EnglishRefusalCauseTokens;

        foreach (var key in RefusalAgrochemicalKeys)
        {
            var value = RequireNonEmptyString(locale, localeFileName, key);
            AssertContainsAny(
                localeFileName,
                key,
                value,
                "the refusal cause/class in player-understandable terms",
                tokensByKey[key]);
        }
    }

    /// <summary>
    /// T065 (FR-008, contract refusal minimum structure): every refusal key in every locale
    /// must state (1) the cause/class in player-understandable terms, (2) that the agrochemical
    /// was not applied / no success occurred, and (3) a next-step implication. Refusals must
    /// never read as success; drift is caught per locale/key.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    /// <param name="key">Refusal translation key under test.</param>
    [Theory]
    [InlineData("default.json", ModConstants.TranslationKeyAgrochemicalInvalidTarget)]
    [InlineData("default.json", ModConstants.TranslationKeyAgrochemicalMissingInput)]
    [InlineData("default.json", ModConstants.TranslationKeyAgrochemicalActionConflict)]
    [InlineData("default.json", ModConstants.TranslationKeyAgrochemicalUnsupportedAuthority)]
    [InlineData("default.json", ModConstants.TranslationKeyAgrochemicalSoilAtMinimum)]
    [InlineData("en.json", ModConstants.TranslationKeyAgrochemicalInvalidTarget)]
    [InlineData("en.json", ModConstants.TranslationKeyAgrochemicalMissingInput)]
    [InlineData("en.json", ModConstants.TranslationKeyAgrochemicalActionConflict)]
    [InlineData("en.json", ModConstants.TranslationKeyAgrochemicalUnsupportedAuthority)]
    [InlineData("en.json", ModConstants.TranslationKeyAgrochemicalSoilAtMinimum)]
    [InlineData("pt.json", ModConstants.TranslationKeyAgrochemicalInvalidTarget)]
    [InlineData("pt.json", ModConstants.TranslationKeyAgrochemicalMissingInput)]
    [InlineData("pt.json", ModConstants.TranslationKeyAgrochemicalActionConflict)]
    [InlineData("pt.json", ModConstants.TranslationKeyAgrochemicalUnsupportedAuthority)]
    [InlineData("pt.json", ModConstants.TranslationKeyAgrochemicalSoilAtMinimum)]
    public void RefusalMessages_StateCause_NoSuccessAndNextStep(string localeFileName, string key)
    {
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, key);
        var portuguese = IsPortuguese(localeFileName);

        var causeTokens = portuguese ? PortugueseRefusalCauseTokens[key] : EnglishRefusalCauseTokens[key];
        AssertContainsAny(
            localeFileName,
            key,
            value,
            "the refusal cause/class in player-understandable terms",
            causeTokens);
        AssertContainsAny(
            localeFileName,
            key,
            value,
            "that the agrochemical was not applied / no success occurred",
            portuguese ? PortugueseRefusalNoSuccessTokens : EnglishRefusalNoSuccessTokens);
        AssertContainsAny(
            localeFileName,
            key,
            value,
            "a next-step implication for the player",
            portuguese ? PortugueseRefusalNextStepTokens : EnglishRefusalNextStepTokens);
    }

    /// <summary>
    /// T068 (contract bounded/recoverable player legibility): every locale must communicate that
    /// the soil-health consequence is bounded and recoverable through existing soil-care systems,
    /// in either the item description or the applied-success message. Wave B may change either
    /// key; at least one must carry the element per locale.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void BoundedRecoverableElement_AppearsInDescriptionOrAppliedMessage(string localeFileName)
    {
        var locale = LoadLocale(localeFileName);
        var tokens = IsPortuguese(localeFileName)
            ? PortugueseBoundedRecoverableTokens
            : EnglishBoundedRecoverableTokens;

        var descriptionKey = ModConstants.AgrochemicalItemDescriptionKey;
        var appliedKey = ModConstants.TranslationKeyAgrochemicalApplied;
        var description = RequireNonEmptyString(locale, localeFileName, descriptionKey);
        var applied = RequireNonEmptyString(locale, localeFileName, appliedKey);

        Assert.True(
            ContainsAnyToken(description, tokens) || ContainsAnyToken(applied, tokens),
            $"017 i18n ({localeFileName}): either '{descriptionKey}' or '{appliedKey}' must communicate " +
            "that the consequence is bounded and recoverable through existing soil-care systems; " +
            $"expected at least one of [{string.Join(", ", tokens)}] in one of those values, but " +
            $"'{descriptionKey}' was \"{description}\" and '{appliedKey}' was \"{applied}\".");
    }

    /// <summary>
    /// T081 (FR-004/FR-012, pt parity with en/default magnitude): the Portuguese applied message
    /// must state the applied magnitude ('10') so the soil consequence is legible, mirroring the
    /// en/default copy.
    /// </summary>
    [Fact]
    public void PortugueseAppliedMessage_IncludesMagnitudeToken()
    {
        const string localeFileName = "pt.json";
        var key = ModConstants.TranslationKeyAgrochemicalApplied;
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, key);

        Assert.True(
            ContainsToken(value, "10"),
            $"017 i18n ({localeFileName}, '{key}'): the applied message must state the applied " +
            $"magnitude ('10'); got \"{value}\".");
    }

    /// <summary>
    /// T081 (FR-004/FR-006, boundary honesty): the Portuguese clamped-success message must bound
    /// the applied delta against the full penalty in comparison terms, not only in absolute terms.
    /// </summary>
    [Fact]
    public void PortugueseAppliedClampedMessage_ComparesAgainstFullPenalty()
    {
        const string localeFileName = "pt.json";
        var key = ModConstants.TranslationKeyAgrochemicalAppliedClamped;
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, key);

        AssertContainsAny(
            localeFileName,
            key,
            value,
            "a comparison bound against the full penalty",
            PortugueseComparisonBoundTokens);
    }

    /// <summary>
    /// T021 (FR-004/FR-006/FR-007): success copy must state an applied soil consequence,
    /// acknowledge a clamped/partial outcome, and describe an unknown baseline without
    /// inventing measurement history.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void SuccessMessages_StateAnAppliedSoilConsequence(string localeFileName)
    {
        var locale = LoadLocale(localeFileName);

        if (!IsPortuguese(localeFileName))
        {
            var appliedKey = ModConstants.TranslationKeyAgrochemicalApplied;
            var applied = RequireNonEmptyString(locale, localeFileName, appliedKey);
            Assert.True(
                ContainsToken(applied, "soil"),
                $"017 i18n ({localeFileName}, '{appliedKey}'): success copy must attribute the " +
                $"consequence to the soil; got \"{applied}\".");
            AssertContainsAny(
                localeFileName,
                appliedKey,
                applied,
                "an applied/now soil-health consequence",
                "applied",
                "now");

            var clampedKey = ModConstants.TranslationKeyAgrochemicalAppliedClamped;
            var clamped = RequireNonEmptyString(locale, localeFileName, clampedKey);
            AssertContainsAny(
                localeFileName,
                clampedKey,
                clamped,
                "the clamped/partial applied outcome",
                "partial",
                "only",
                "applied");
            AssertNoFullNominalPenaltyClaim(localeFileName, clampedKey, clamped);
        }

        var unknownBaselineKey = ModConstants.TranslationKeyAgrochemicalAppliedUnknownBaseline;
        var unknownBaseline = RequireNonEmptyString(locale, localeFileName, unknownBaselineKey);
        if (IsPortuguese(localeFileName))
        {
            AssertContainsAny(
                localeFileName,
                unknownBaselineKey,
                unknownBaseline,
                "a no-history/unmeasured baseline signal",
                "sem leitura",
                "não medid",
                "nao medid",
                "medi");
            return;
        }

        AssertContainsAny(
            localeFileName,
            unknownBaselineKey,
            unknownBaseline,
            "a no-history/unmeasured baseline signal",
            "no prior",
            "unmeasured",
            "reading");
    }

    /// <summary>
    /// T022 (C1, FR-003/FR-004/SC-006): success and clamped success copy is confirmation and
    /// attribution only. It must not claim a quality, growth, extra-harvest, or fertilizer-tier
    /// mechanical buff.
    /// </summary>
    /// <remarks>
    /// Contract-driven lexical proxy only: this proves the shipped strings avoid mechanical-buff
    /// vocabulary. It does not prove the runtime path avoids 014/015 APIs, <c>HoeDirt.fertilizer</c>
    /// writes, or Harmony fertilizer hooks; those boundaries belong to service/controller tests
    /// (T012/T015) and manual semantic review (T052).
    /// </remarks>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void SuccessMessages_MakeNoMechanicalBuffClaims_C1ConfirmationOnly(string localeFileName)
    {
        var locale = LoadLocale(localeFileName);
        var forbiddenTokens = IsPortuguese(localeFileName)
            ? new[] { "qualidade", "colheita extra", "crescimento", "fertilizante", "fase" }
            : new[] { "quality", "extra harvest", "growth", "fertilizer tier", "phase days" };

        foreach (var key in SuccessAgrochemicalKeys)
        {
            var value = RequireNonEmptyString(locale, localeFileName, key);
            AssertContainsNone(
                localeFileName,
                key,
                value,
                "C1 no-double-counting: success/confirmation copy must attribute the soil consequence " +
                "only and must not claim a quality, growth, extra-harvest, or fertilizer-tier mechanical buff",
                forbiddenTokens);
        }
    }

    /// <summary>
    /// T022 (FR-002/FR-012): every 017 value in every locale must stay cozy and non-punitive;
    /// no shaming, insulting, or moralizing vocabulary.
    /// </summary>
    /// <remarks>
    /// Contract-driven lexical proxy only: tone and nuance still require manual review (T052).
    /// The absence of these tokens does not by itself prove the copy is cozy or non-punitive.
    /// </remarks>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void AllAgrochemicalCopy_UsesCozyNonPunitiveTone(string localeFileName)
    {
        var locale = LoadLocale(localeFileName);

        foreach (var key in RequiredAgrochemicalKeys)
        {
            var value = RequireNonEmptyString(locale, localeFileName, key);
            AssertContainsNone(
                localeFileName,
                key,
                value,
                "cozy/non-punitive tone: text must not shame, insult, or moralize the player",
                ShamingTokens);
        }
    }

    /// <summary>
    /// T022 (FR-005/FR-007, boundary honesty): clamped success copy must acknowledge a bounded or
    /// partial applied outcome and must not claim the full nominal 10-point penalty.
    /// </summary>
    /// <remarks>
    /// Copy alone is not the runtime guard: the controller's feedback mapping also derives its
    /// message from <c>AppliedPenalty</c>/<c>WasClamped</c> on the service result, so a clamped
    /// outcome cannot be presented as a full nominal penalty. These assertions only stop the
    /// shipped strings from asserting the opposite; service/controller boundary tests (T013/T018)
    /// prove runtime honesty, and manual semantic review remains T052.
    /// </remarks>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void ClampedSuccessMessage_AcknowledgesBoundsAndAvoidsFullPenaltyClaim(string localeFileName)
    {
        var key = ModConstants.TranslationKeyAgrochemicalAppliedClamped;
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, key);

        if (IsPortuguese(localeFileName))
        {
            AssertContainsAny(
                localeFileName,
                key,
                value,
                "a bounded/partial applied-delta signal",
                "aplicad",
                "parcial",
                "apenas",
                "limitad");
        }
        else
        {
            AssertContainsAny(
                localeFileName,
                key,
                value,
                "a bounded/partial applied-delta signal",
                "applied",
                "partial",
                "only",
                "limited");
        }

        AssertNoFullNominalPenaltyClaim(localeFileName, key, value);
    }

    /// <summary>
    /// T022 (C2, FR-006): the unknown-baseline success key must say the prior soil value was not
    /// measured and must not imply a remembered prior reading.
    /// </summary>
    /// <remarks>
    /// Contract-driven lexical proxy only. Manual semantic review (T052) must still confirm the
    /// copy cannot be read as a prior known value; this test only rejects the listed phrasings.
    /// </remarks>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void UnknownBaselineMessage_RequiresNoHistorySignalAndInventsNoPriorReading(string localeFileName)
    {
        var key = ModConstants.TranslationKeyAgrochemicalAppliedUnknownBaseline;
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, key);

        if (IsPortuguese(localeFileName))
        {
            AssertContainsAny(
                localeFileName,
                key,
                value,
                "a no-history signal ('sem leitura'/'não medid'/'nao medid')",
                "sem leitura",
                "não medid",
                "nao medid");
            AssertContainsNone(
                localeFileName,
                key,
                value,
                "C2 unknown-baseline legibility: copy must not imply a remembered prior soil value or reading",
                new[] { "leitura anterior", "seu solo estava", "antes era" });
            return;
        }

        AssertContainsAny(
            localeFileName,
            key,
            value,
            "a no-history signal ('no prior'/'unmeasured'/'no reading')",
            "no prior",
            "unmeasured",
            "no reading");
        AssertContainsNone(
            localeFileName,
            key,
            value,
            "C2 unknown-baseline legibility: copy must not imply a remembered prior soil value or reading",
            new[] { "previous reading", "your soil was", "before it was" });
    }

    /// <summary>
    /// T097 (FR-008, commit-failure honesty): the dedicated commit-failure refusal key must ship
    /// in every locale as a non-empty string. Wave B introduces
    /// <c>ModConstants.TranslationKeyAgrochemicalCommitFailed</c> and the
    /// <c>message.agrochemical.commit-failed</c> locale entries; this assertion is expected RED
    /// until then.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void CommitFailedRefusal_ShipsInAllLocalesAsNonEmptyString(string localeFileName)
    {
        RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, CommitFailedTranslationKey);
    }

    /// <summary>
    /// T097 (FR-008, refusal minimum structure): the commit-failure refusal must state (1) an
    /// explicit cause reference to a commit/apply failure, (2) that the application did not
    /// happen / no success occurred, and (3) a next-step implication (try again, wait, or ask the
    /// host). Expected RED until Wave B ships the copy.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void CommitFailedRefusal_StatesCause_NoSuccessAndNextStep(string localeFileName)
    {
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, CommitFailedTranslationKey);
        var portuguese = IsPortuguese(localeFileName);

        AssertContainsAny(
            localeFileName,
            CommitFailedTranslationKey,
            value,
            "an explicit cause reference to a commit/apply failure",
            portuguese ? PortugueseCommitFailedCauseTokens : EnglishCommitFailedCauseTokens);
        AssertContainsAny(
            localeFileName,
            CommitFailedTranslationKey,
            value,
            "that the application failed / did not happen (no-success)",
            portuguese ? PortugueseCommitFailedNoSuccessTokens : EnglishCommitFailedNoSuccessTokens);
        AssertContainsAny(
            localeFileName,
            CommitFailedTranslationKey,
            value,
            "a next-step implication for the player (try again, wait, or ask the host)",
            portuguese ? PortugueseRefusalNextStepTokens : EnglishRefusalNextStepTokens);
    }

    /// <summary>
    /// T097 (FR-008, no false success): the commit-failure refusal must never read as having
    /// consumed the input or applied successfully. Claim-of-success/claim-of-consumption tokens
    /// are chosen so they cannot occur inside a negated denial; the required no-success phrasing
    /// in <see cref="CommitFailedRefusal_StatesCause_NoSuccessAndNextStep"/> is the primary
    /// guard. Expected RED until Wave B ships the copy.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void CommitFailedRefusal_MakesNoConsumedOrSuccessClaim(string localeFileName)
    {
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, CommitFailedTranslationKey);

        AssertContainsNone(
            localeFileName,
            CommitFailedTranslationKey,
            value,
            "no false success: copy must not claim the input was consumed or that application succeeded",
            IsPortuguese(localeFileName) ? PortugueseCommitFailedNoClaimTokens : EnglishCommitFailedNoClaimTokens);
    }

    /// <summary>
    /// T097 (C1-style, FR-003/FR-004/SC-006): the commit-failure refusal must not claim a
    /// quality, growth, extra-harvest, or fertilizer-tier mechanical buff; it is a refusal, not a
    /// confirmation. Mirrors <see cref="SuccessMessages_MakeNoMechanicalBuffClaims_C1ConfirmationOnly"/>.
    /// Expected RED until Wave B ships the copy.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void CommitFailedRefusal_MakesNoMechanicalBuffClaims(string localeFileName)
    {
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, CommitFailedTranslationKey);

        AssertContainsNone(
            localeFileName,
            CommitFailedTranslationKey,
            value,
            "C1 no-double-counting: refusal copy must not claim a quality, growth, extra-harvest, or fertilizer-tier mechanical buff",
            IsPortuguese(localeFileName) ? PortugueseCommitFailedNoBuffTokens : EnglishCommitFailedNoBuffTokens);
    }

    /// <summary>
    /// T097 (FR-002/FR-012): the commit-failure refusal must stay cozy and non-punitive; no
    /// shaming, insulting, or moralizing vocabulary. Mirrors the
    /// <see cref="AllAgrochemicalCopy_UsesCozyNonPunitiveTone"/> tone sweep (which iterates
    /// <see cref="RequiredAgrochemicalKeys"/> and therefore does not cover the new key). Expected
    /// RED until Wave B ships the copy.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void CommitFailedRefusal_UsesCozyNonPunitiveTone(string localeFileName)
    {
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, CommitFailedTranslationKey);

        AssertContainsNone(
            localeFileName,
            CommitFailedTranslationKey,
            value,
            "cozy/non-punitive tone: text must not shame, insult, or moralize the player",
            ShamingTokens);
    }

    /// <summary>
    /// T097 (FR-008): <c>LivingRoots/Constants.cs</c> must declare
    /// <c>TranslationKeyAgrochemicalCommitFailed</c> bound to
    /// <c>message.agrochemical.commit-failed</c> so the key is part of <see cref="ModConstants"/>.
    /// Written as a constants-file token check (like the production-source guards in
    /// <c>ProductionSourceGuardTests</c>) so the suite compiles before Wave B lands the constant.
    /// Expected RED until Wave B extends <c>Constants.cs</c>.
    /// </summary>
    [Fact]
    public void ConstantsFile_DeclaresCommitFailedTranslationKeyBoundToMessageKey()
    {
        var constantsPath = Path.Combine(LocateRepoRoot(), "LivingRoots", "Constants.cs");
        Assert.True(File.Exists(constantsPath), $"Constants.cs not found at {constantsPath}.");
        var content = File.ReadAllText(constantsPath);

        Assert.True(
            content.Contains("TranslationKeyAgrochemicalCommitFailed", StringComparison.Ordinal),
            "LivingRoots/Constants.cs must declare TranslationKeyAgrochemicalCommitFailed " +
            "(T097: the commit-failure refusal key is part of ModConstants).");
        Assert.True(
            content.Contains(CommitFailedTranslationKey, StringComparison.Ordinal),
            $"LivingRoots/Constants.cs must bind TranslationKeyAgrochemicalCommitFailed to " +
            $"'{CommitFailedTranslationKey}'.");
    }

    /// <summary>
    /// T104 (FR-002/FR-008, explicit-diagnosis phrasing): the <c>missing-input</c> refusal must
    /// diagnose the holding condition addressed to the player, not issue a bare instruction.
    /// Requiring the player-addressed pronoun (<c>you</c>/<c>você</c>) distinguishes an explicit
    /// diagnosis ("You need to be holding…") from the current requirement-style copy ("Hold a
    /// Joja agrochemical…"), which lacks it. Expected RED until Wave B tightens the copy.
    /// Existing missing-input assertions are intentionally unchanged.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    [Theory]
    [InlineData("default.json")]
    [InlineData("en.json")]
    [InlineData("pt.json")]
    public void MissingInputRefusal_NamesTheHoldingConditionAsExplicitDiagnosis(string localeFileName)
    {
        var key = ModConstants.TranslationKeyAgrochemicalMissingInput;
        var value = RequireNonEmptyString(LoadLocale(localeFileName), localeFileName, key);

        if (IsPortuguese(localeFileName))
        {
            AssertContainsAny(
                localeFileName,
                key,
                value,
                "the holding condition in explicit-diagnosis phrasing ('você precisa estar segurando…' / 'não está segurando…')",
                PortugueseMissingInputDiagnosisTokens);
            Assert.True(
                ContainsToken(value, "você"),
                $"017 i18n ({localeFileName}, '{key}'): the missing-input refusal must address the " +
                $"player as 'você' (explicit diagnosis, not a bare instruction like \"Segure um agroquímico…\"); " +
                $"got \"{value}\".");
            return;
        }

        AssertContainsAny(
            localeFileName,
            key,
            value,
            "the holding condition in explicit-diagnosis phrasing ('you need to be holding…' / 'you are not holding…')",
            EnglishMissingInputDiagnosisTokens);
        Assert.True(
            ContainsToken(value, "you"),
            $"017 i18n ({localeFileName}, '{key}'): the missing-input refusal must address the player " +
            $"as 'you' (explicit diagnosis, not a bare instruction like \"Hold a Joja agrochemical…\"); " +
            $"got \"{value}\".");
    }

    /// <summary>
    /// Walks up from the current working directory until <c>Stardew-LivingRoots.sln</c> is found.
    /// </summary>
    /// <returns>The repository root directory.</returns>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when no directory in the current working directory's ancestry contains the solution.
    /// </exception>
    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Stardew-LivingRoots.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root: no 'Stardew-LivingRoots.sln' was found while " +
            $"walking up from '{Directory.GetCurrentDirectory()}'.");
    }

    /// <summary>
    /// Reads a locale file from the source tree with <see cref="File.ReadAllText(string)"/> and
    /// parses it as a JSON object with <see cref="JObject.Parse(string)"/>.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    /// <returns>The parsed locale object.</returns>
    private static JObject LoadLocale(string localeFileName)
    {
        var path = Path.Combine(LocateRepoRoot(), "LivingRoots", "i18n", localeFileName);
        Assert.True(
            File.Exists(path),
            $"017 i18n: expected locale file '{path}' to exist in the source tree.");
        return JObject.Parse(File.ReadAllText(path));
    }

    /// <summary>
    /// Requires <paramref name="key"/> to exist in <paramref name="locale"/> as a non-empty
    /// string whose trimmed value is also non-empty.
    /// </summary>
    /// <param name="locale">Parsed locale object.</param>
    /// <param name="localeFileName">Locale file name used in failure messages.</param>
    /// <param name="key">Required translation key.</param>
    /// <returns>The non-empty translation value.</returns>
    private static string RequireNonEmptyString(JObject locale, string localeFileName, string key)
    {
        var token = locale[key];
        Assert.True(
            token != null,
            $"017 i18n parity: '{localeFileName}' is missing the required agrochemical key '{key}'.");
        var nonNullToken = token!;
        Assert.True(
            nonNullToken.Type == JTokenType.String,
            $"017 i18n parity: '{localeFileName}' key '{key}' must be a JSON string, but was " +
            $"'{nonNullToken.Type}'.");
        var value = nonNullToken.Value<string>();
        Assert.False(
            string.IsNullOrWhiteSpace(value),
            $"017 i18n parity: '{localeFileName}' key '{key}' must have a non-empty trimmed string " +
            "value.");
        return value!;
    }

    /// <summary>
    /// Whether the locale is Portuguese, which uses the locale-specific token sets below.
    /// </summary>
    /// <param name="localeFileName">Locale file name relative to <c>LivingRoots/i18n</c>.</param>
    /// <returns><see langword="true"/> for <c>pt.json</c>.</returns>
    private static bool IsPortuguese(string localeFileName)
    {
        return string.Equals(localeFileName, "pt.json", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Case-insensitive single-token containment.
    /// </summary>
    /// <param name="text">Text to inspect.</param>
    /// <param name="token">Token to find.</param>
    /// <returns><see langword="true"/> when the token occurs case-insensitively.</returns>
    private static bool ContainsToken(string text, string token)
    {
        return text.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Case-insensitive any-of containment.
    /// </summary>
    /// <param name="text">Text to inspect.</param>
    /// <param name="tokens">Accepted tokens.</param>
    /// <returns><see langword="true"/> when at least one token occurs.</returns>
    private static bool ContainsAnyToken(string text, string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (ContainsToken(text, token))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Asserts that a translation value contains at least one of the required tokens.
    /// </summary>
    /// <param name="localeFileName">Locale file name used in failure messages.</param>
    /// <param name="key">Translation key used in failure messages.</param>
    /// <param name="value">Translation value under test.</param>
    /// <param name="requirement">Human-readable requirement description.</param>
    /// <param name="tokens">Accepted tokens.</param>
    private static void AssertContainsAny(
        string localeFileName,
        string key,
        string value,
        string requirement,
        params string[] tokens)
    {
        Assert.True(
            ContainsAnyToken(value, tokens),
            $"017 i18n ({localeFileName}, '{key}'): value must mention {requirement}; expected at " +
            $"least one of [{string.Join(", ", tokens)}], but was \"{value}\".");
    }

    /// <summary>
    /// Asserts that a translation value contains none of the forbidden tokens.
    /// </summary>
    /// <param name="localeFileName">Locale file name used in failure messages.</param>
    /// <param name="key">Translation key used in failure messages.</param>
    /// <param name="value">Translation value under test.</param>
    /// <param name="requirement">Human-readable requirement description.</param>
    /// <param name="forbiddenTokens">Rejected tokens.</param>
    private static void AssertContainsNone(
        string localeFileName,
        string key,
        string value,
        string requirement,
        params string[] forbiddenTokens)
    {
        var matchedTokens = forbiddenTokens
            .Where(token => ContainsToken(value, token))
            .ToArray();
        Assert.True(
            matchedTokens.Length == 0,
            $"017 i18n ({localeFileName}, '{key}'): {requirement}; found forbidden token(s) " +
            $"[{string.Join(", ", matchedTokens)}] in \"{value}\".");
    }

    /// <summary>
    /// Asserts that clamped success copy does not claim the full nominal 10-point penalty was
    /// applied.
    /// </summary>
    /// <param name="localeFileName">Locale file name used in failure messages.</param>
    /// <param name="key">Translation key used in failure messages.</param>
    /// <param name="value">Translation value under test.</param>
    private static void AssertNoFullNominalPenaltyClaim(string localeFileName, string key, string value)
    {
        if (IsPortuguese(localeFileName))
        {
            AssertContainsNone(
                localeFileName,
                key,
                value,
                "boundary honesty: clamped copy must not claim the full nominal 10-point penalty was applied",
                "10 pontos",
                "todos os 10");
            return;
        }

        AssertContainsNone(
            localeFileName,
            key,
            value,
            "boundary honesty: clamped copy must not claim the full nominal 10-point penalty was applied",
            "full 10",
            "all 10",
            "10 points");
    }
}
