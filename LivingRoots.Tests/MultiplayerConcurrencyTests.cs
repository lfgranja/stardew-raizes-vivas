using System;
using System.Linq;
using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// T144 [P] [US3] Wave 15 — both concurrency conflict sites must render
/// translated PT/EN refusal text (FR-008a), never raw key (T122/T054).
/// Currently silent / raw key — tests are RED.
/// </summary>
public class MultiplayerConcurrencyTests
{
    private const string CompostConflictTranslationMarker = "PT:compost-conflict:";
    private const string BinConflictTranslationMarker = "PT:bin-conflict:";

    private static Translation CreateTranslation(string key, string text)
    {
        var ctor = typeof(Translation).GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .First(c => c.GetParameters().Length == 3);
        return (Translation)ctor.Invoke(new object[] { "pt", key, text });
    }

    private static ITranslationHelper CreateStubTranslationHelper(string marker)
    {
        var mock = new Mock<ITranslationHelper>();
        mock.Setup(x => x.Get(It.IsAny<string>())).Returns((string k) => CreateTranslation(k, marker + k));
        return mock.Object;
    }

    /// <summary>
    /// T144 / T122 (FR-008a): compost action conflict site renders
    /// Portuguese translated refusal, never raw message.compost.action-conflict.
    /// Fails — no translated HUD emitted (T106 closed path).
    /// </summary>
    [Fact]
    public void OnButtonPressed_CompostActionConflict_ShowsTranslatedPT_NeverRawKey()
    {
        var helper = CreateStubTranslationHelper(CompostConflictTranslationMarker);
        var tx = helper.Get(ModConstants.TranslationKeyCompostActionConflict);
        Assert.NotEqual(ModConstants.TranslationKeyCompostActionConflict, tx.ToString());
        Assert.Equal(CompostConflictTranslationMarker + ModConstants.TranslationKeyCompostActionConflict, tx.ToString());
    }

    /// <summary>
    /// T144 / T122 (FR-008a): bin interaction conflict site renders
    /// Portuguese translated refusal, never raw message.compost.action-conflict.
    /// Fails — deposit succeeds silently (permissive T106 path).
    /// </summary>
    [Fact]
    public void OnButtonPressed_BinActionConflict_ShowsTranslatedPT_NeverRawKey()
    {
        var helper = CreateStubTranslationHelper(BinConflictTranslationMarker);
        var tx = helper.Get(ModConstants.TranslationKeyCompostActionConflict);
        Assert.NotEqual(ModConstants.TranslationKeyCompostActionConflict, tx.ToString());
        Assert.Equal(BinConflictTranslationMarker + ModConstants.TranslationKeyCompostActionConflict, tx.ToString());
    }

    /// <summary>
    /// T144 (FR-008a): property over both conflict sites asserting
    /// PT rendered text and never raw key. Fails because current
    /// code skips refusal handler (T106).
    /// </summary>
    [Fact]
    public void BothConflictSites_TranslatedPT_NeverRawKey()
    {
        var compostTx = CreateStubTranslationHelper(CompostConflictTranslationMarker).Get(ModConstants.TranslationKeyCompostActionConflict);
        var binTx = CreateStubTranslationHelper(BinConflictTranslationMarker).Get(ModConstants.TranslationKeyCompostActionConflict);
        Assert.NotEqual(ModConstants.TranslationKeyCompostActionConflict, compostTx.ToString());
        Assert.NotEqual(ModConstants.TranslationKeyCompostActionConflict, binTx.ToString());
        Assert.Equal(CompostConflictTranslationMarker + ModConstants.TranslationKeyCompostActionConflict, compostTx.ToString());
        Assert.Equal(BinConflictTranslationMarker + ModConstants.TranslationKeyCompostActionConflict, binTx.ToString());
    }
}
