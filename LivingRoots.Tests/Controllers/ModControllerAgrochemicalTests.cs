using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// 017 controller orchestration tests (T014-T020, T078, T082, T085).
/// </summary>
/// <remarks>
/// <para>
/// These tests pin the controller-owned ordering for the agrochemical path:
/// authority, target, input, lock, service call, consume-after-commit,
/// feedback, then FR-009 broadcast. <c>ModController.TryApplyAgrochemicalWithLock</c>
/// implements that contract and the suite is the evidence for it.
/// <see cref="TryApplyAgrochemical_WithoutService_FailsClosedWithLocalizedRefusalAndLog"/>
/// pins the T078 specification for the degenerate composition with a
/// missing application service (FR-008, FR-013) and holds green against the
/// fail-closed branch.
/// Tests marked <c>// characterization</c> pin behavior that already holds
/// against the current implementation. No test expects or accepts a
/// <see cref="NotImplementedException"/>.
/// </para>
/// <para>
/// The feedback seam is replaced with a capture delegate in every arrange block.
/// <c>IModHelper.Translation</c> is a loose mock returning <c>null</c>, so the
/// observed message is the raw translation key; that makes the controller's
/// key selection (service result versus hardcoded text) directly observable.
/// </para>
/// </remarks>
public class ModControllerAgrochemicalTests
{
    private static readonly Vector2 Tile = new(10, 10);
    private const long FarmerId = 123L;
    private const string ServiceConflictMessageKey = "message.agrochemical.service-conflict";

    // ---------------------------------------------------------------------
    // T014 - unsupported authority
    // ---------------------------------------------------------------------

    /// <summary>
    /// T014 (FR-008, C4): a farmhand attempt is refused before the lock, the
    /// service, soil mutation, item consumption, or any false-success feedback
    /// or FR-009 broadcast.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnUnsupportedAuthority_RefusesBeforeServiceLockMutationOrConsumption()
    {
        var harness = new ControllerHarness();
        harness.Controller.IsMainPlayerCheck = () => false;
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.False(result, "C4: farmhand/unsupported authority must refuse, not report success.");
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Never,
            "C4: authority refusal must happen before the application service is consulted.");
        harness.Lock.Verify(
            x => x.TryAcquireLock(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<long>()),
            Times.Never,
            "C4: authority refusal must happen before any lock is acquired.");
        AssertNoControllerSoilWrites(harness.Soil);
        Assert.Equal(3, heldItem.Stack);
        AssertSingleRefusal(harness.Feedback, ModConstants.TranslationKeyAgrochemicalUnsupportedAuthority);
        Assert.DoesNotContain(harness.Feedback, entry => !entry.IsRefusal);
        AssertNoBroadcast(harness.Multiplayer);
    }

    // ---------------------------------------------------------------------
    // T015 - host success ordering and side effects
    // ---------------------------------------------------------------------

    /// <summary>
    /// T015 (FR-004, FR-009, SC-006): host success acquires and releases the
    /// tile lock, calls the service exactly once, consumes exactly one held
    /// item only after the service commit, shows success feedback, and then
    /// broadcasts the FR-009 snapshot. The actual <see cref="HoeDirt"/> is
    /// untouched apart from soil health owned by the service: no fertilizer
    /// write and no crop phase mutation.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnHostSuccess_OrdersLockServiceReleaseFeedbackThenBroadcastAndConsumesOnce()
    {
        var harness = new ControllerHarness(hasCrop: true);
        var order = new List<string>();
        int stackAtService = -1;
        int stackAtFeedback = -1;

        GameLocation location = harness.Fixture.Location;
        HoeDirt hoeDirt = Assert.IsType<HoeDirt>(location.terrainFeatures[Tile]);
        int phaseDaysBefore = hoeDirt.crop.phaseDays.Count;

        harness.Lock
            .Setup(x => x.TryAcquireLock("Farm", Tile, FarmerId))
            .Callback(() => order.Add("acquire"))
            .Returns(true);
        harness.Lock
            .Setup(x => x.ReleaseLock("Farm", Tile, FarmerId))
            .Callback(() => order.Add("release"));
        harness.Agro
            .Setup(x => x.Apply(location, Tile))
            .Callback(() =>
            {
                order.Add("service");
                stackAtService = harness.Player.CurrentItem!.Stack;
            })
            .Returns(CreateBaseline50Success());
        harness.Controller.ShowAgrochemicalFeedback = (message, isRefusal) =>
        {
            order.Add("feedback");
            stackAtFeedback = harness.Player.CurrentItem!.Stack;
            harness.Feedback.Add((message, isRefusal));
        };
        harness.Multiplayer
            .Setup(m => m.SendMessage(
                It.IsAny<SoilHealthSyncMessage>(),
                ModController.SoilHealthSyncMessageType,
                It.IsAny<string[]>(),
                It.IsAny<long[]>()))
            .Callback(() => order.Add("broadcast"));

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(location, Tile, FarmerId);

        Assert.True(result, "The authoritative host path must report success.");
        Assert.Equal(
            new[] { "acquire", "service", "release", "feedback", "broadcast" },
            order);
        Assert.Equal(3, stackAtService);
        Assert.Equal(2, stackAtFeedback);
        harness.Agro.Verify(x => x.Apply(location, Tile), Times.Once);
        harness.Lock.Verify(x => x.TryAcquireLock("Farm", Tile, FarmerId), Times.Once);
        harness.Lock.Verify(x => x.ReleaseLock("Farm", Tile, FarmerId), Times.Once);
        Assert.Equal(2, harness.Player.CurrentItem!.Stack);
        AssertSingleSuccess(harness.Feedback, ModConstants.TranslationKeyAgrochemicalApplied);
        AssertSingleBroadcast(harness.Multiplayer);
        AssertNoControllerSoilWrites(harness.Soil);

#pragma warning disable AvoidNetField // AvoidNetField: direct fertilizer slot inspection is required to pin the no-write contract.
        Assert.True(
            string.IsNullOrEmpty(hoeDirt.fertilizer.Value),
            "C1/FR-003: the agrochemical path must not write HoeDirt.fertilizer.");
#pragma warning restore AvoidNetField
        Assert.Equal(phaseDaysBefore, hoeDirt.crop.phaseDays.Count);
    }

    // ---------------------------------------------------------------------
    // T016 - lock contention and service-level conflict refusals
    // ---------------------------------------------------------------------

    /// <summary>
    /// T016 (FR-010, SC-002): a lost first-wins lock race refuses without a
    /// service call, item consumption, soil write, or broadcast.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnLockContention_RefusesWithoutServiceMutationOrConsumption()
    {
        var harness = new ControllerHarness();
        harness.Lock
            .Setup(x => x.TryAcquireLock(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<long>()))
            .Returns(false);
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.False(result, "FR-010: a lost lock race must fail closed.");
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Never,
            "Lock contention must be refused before the application service is called.");
        harness.Lock.Verify(
            x => x.ReleaseLock(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<long>()),
            Times.Never,
            "A lock that was never acquired must not be released.");
        Assert.Equal(3, heldItem.Stack);
        AssertSingleRefusal(harness.Feedback, ModConstants.TranslationKeyAgrochemicalActionConflict);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T016 (FR-019): a controller without a host atomic lock service cannot
    /// arbitrate contention, so the write path fails closed with the same
    /// conflict refusal instead of guessing.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_WithoutLockService_FailsClosedWithConflictRefusal()
    {
        var harness = new ControllerHarness(includeLockService: false);
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.False(result, "FR-019: no lock service means contention cannot be resolved, so the write must fail closed.");
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Never,
            "No lock service must refuse before the application service is called.");
        Assert.Equal(3, heldItem.Stack);
        AssertSingleRefusal(harness.Feedback, ModConstants.TranslationKeyAgrochemicalActionConflict);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T078 (FR-008, FR-013): a controller with no agrochemical application
    /// service fails closed with a warning log plus a localized refusal — the
    /// same fail-closed refusal shape as the missing-lock-service branch —
    /// instead of a silent <c>false</c>, and still cannot touch the lock, the
    /// held item, soil, or the FR-009 broadcast.
    /// </summary>
    /// <remarks>
    /// Green against the current fail-closed branch: the null-service path
    /// logs a Warn and refuses before any lock or service work happens. Uses the
    /// <see cref="ModConstants.TranslationKeyAgrochemicalActionConflict"/> key
    /// because the spec defines no dedicated composition-failure key and the
    /// no-lock-service fail-closed branch already uses it for an unavailable
    /// mechanism; introducing a new key is production/i18n scope.
    /// </remarks>
    [Fact]
    public void TryApplyAgrochemical_WithoutService_FailsClosedWithLocalizedRefusalAndLog()
    {
        var harness = new ControllerHarness(includeAgrochemicalService: false);
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.False(result, "A missing application service must fail closed.");
        harness.Lock.Verify(
            x => x.TryAcquireLock(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<long>()),
            Times.Never,
            "The null-service guard must short-circuit before any lock is acquired.");
        Assert.Equal(3, heldItem.Stack);
        AssertSingleRefusal(harness.Feedback, ModConstants.TranslationKeyAgrochemicalActionConflict);
        harness.Monitor.Verify(
            m => m.Log(It.IsAny<string>(), LogLevel.Warn),
            Times.AtLeastOnce,
            "A missing application service must be surfaced as a composition failure in the log, not swallowed.");
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T016: a service-level <see cref="AgrochemicalApplicationOutcome.RefusedConflict"/>
    /// result is surfaced with the service's own message key and has no
    /// post-commit side effects.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnServiceConflictRefusal_DoesNotConsumeOrBroadcast()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateRefusal(AgrochemicalApplicationOutcome.RefusedConflict, ServiceConflictMessageKey));
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.False(result, "A service conflict refusal must not be reported as success.");
        Assert.Equal(3, heldItem.Stack);
        AssertSingleRefusal(harness.Feedback, ServiceConflictMessageKey);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    // ---------------------------------------------------------------------
    // T017 - target, input, and boundary refusals
    // ---------------------------------------------------------------------

    /// <summary>
    /// T017 (FR-008): a non-farm, non-Greenhouse location is an invalid target.
    /// The refusal happens before the lock and service, leaving the item alone.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnNonFarmLocation_RefusesInvalidTargetBeforeService()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        var town = new GameLocationFixture("Town", isFarm: false);
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(town.Location, Tile, FarmerId);

        Assert.False(result, "Only farm land or the Greenhouse is a valid target.");
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Never);
        harness.Lock.Verify(
            x => x.TryAcquireLock(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<long>()),
            Times.Never);
        Assert.Equal(3, heldItem.Stack);
        AssertSingleRefusal(harness.Feedback, ModConstants.TranslationKeyAgrochemicalInvalidTarget);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T017 (FR-008): a farm tile that is not tilled soil is an invalid target.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnUntilledFarmTile_RefusesInvalidTargetBeforeService()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        var untiledTile = new Vector2(11, 11);
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            untiledTile,
            FarmerId);

        Assert.False(result, "A tile without HoeDirt is not a valid agrochemical target.");
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Never);
        harness.Lock.Verify(
            x => x.TryAcquireLock(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<long>()),
            Times.Never);
        Assert.Equal(3, heldItem.Stack);
        AssertSingleRefusal(harness.Feedback, ModConstants.TranslationKeyAgrochemicalInvalidTarget);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T017 (FR-008): holding nothing is the missing-input refusal.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_WithNoHeldItem_RefusesMissingInputBeforeService()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        harness.Player.CurrentItem = null;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.False(result, "No held agrochemical input must refuse.");
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Never);
        harness.Lock.Verify(
            x => x.TryAcquireLock(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<long>()),
            Times.Never);
        Assert.Null(harness.Player.CurrentItem);
        AssertSingleRefusal(harness.Feedback, ModConstants.TranslationKeyAgrochemicalMissingInput);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T017 (FR-008): holding a different item is the missing-input refusal,
    /// and that item is not consumed.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_WithWrongHeldItem_RefusesMissingInputBeforeService()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        Item wrongItem = ItemFactory.CreateItem("Object.Stone");
        wrongItem.Stack = 3;
        harness.Player.CurrentItem = wrongItem;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.False(result, "A non-agrochemical held item must refuse.");
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Never);
        harness.Lock.Verify(
            x => x.TryAcquireLock(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<long>()),
            Times.Never);
        Assert.Equal(3, wrongItem.Stack);
        AssertSingleRefusal(harness.Feedback, ModConstants.TranslationKeyAgrochemicalMissingInput);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T017 (FR-005, FR-007): a service refusal at minimum soil health is a
    /// refusal, not a partial success: no consumption and no broadcast.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnSoilAtMinimumRefusal_DoesNotConsumeOrBroadcast()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateRefusal(
                AgrochemicalApplicationOutcome.RefusedSoilAtMinimum,
                ModConstants.TranslationKeyAgrochemicalSoilAtMinimum));
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.False(result, "Soil already at the minimum cannot report success.");
        Assert.Equal(3, heldItem.Stack);
        AssertSingleRefusal(harness.Feedback, ModConstants.TranslationKeyAgrochemicalSoilAtMinimum);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T017 (FR-005): a clamped success still succeeds, consumes exactly one
    /// item, and uses the clamped feedback key.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnClampedSuccess_ConsumesOneAndUsesClampedFeedback()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateClampedBaseline5Success());
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.True(result, "A clamped positive outcome is still a successful application.");
        Assert.Equal(2, heldItem.Stack);
        AssertSingleSuccess(harness.Feedback, ModConstants.TranslationKeyAgrochemicalAppliedClamped);
        AssertSingleBroadcast(harness.Multiplayer);
        AssertNoControllerSoilWrites(harness.Soil);
    }

    // ---------------------------------------------------------------------
    // T018 - boundary-honest feedback derived from the service result
    // ---------------------------------------------------------------------

    /// <summary>
    /// T018 (FR-005, FR-007): when the service reports a clamped outcome the
    /// controller repeats the service's clamped key and does not claim the
    /// full nominal penalty was applied.
    /// </summary>
    /// <remarks>
    /// The feedback text is selected from the service result, not rebuilt from
    /// a hardcoded full-penalty claim. The service result carries <c>WasClamped</c>
    /// and the applied penalty, so this test pins the controller to that result
    /// rather than to a second calculation in the controller.
    /// </remarks>
    [Fact]
    public void TryApplyAgrochemical_OnClampedSuccess_FeedbackUsesClampedKeyNotFullPenaltyClaim()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateClampedBaseline5Success());
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.True(result);
        Assert.Equal(2, heldItem.Stack);
        (string Message, bool IsRefusal) feedback = Assert.Single(harness.Feedback);
        Assert.False(feedback.IsRefusal);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalAppliedClamped, feedback.Message);
        Assert.NotEqual(ModConstants.TranslationKeyAgrochemicalApplied, feedback.Message);
        AssertSingleBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T018 (C2, FR-006): an unknown-baseline success uses the dedicated
    /// unknown-baseline key, never the measured full-penalty confirmation.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnUnknownBaselineSuccess_FeedbackUsesUnknownBaselineKey()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateUnknownBaselineSuccess());
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.True(result);
        Assert.Equal(2, heldItem.Stack);
        (string Message, bool IsRefusal) feedback = Assert.Single(harness.Feedback);
        Assert.False(feedback.IsRefusal);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalAppliedUnknownBaseline, feedback.Message);
        Assert.NotEqual(ModConstants.TranslationKeyAgrochemicalApplied, feedback.Message);
        AssertSingleBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T018 (FR-005, FR-007): a soil-at-minimum refusal uses the dedicated
    /// refusal key and performs no post-commit side effects.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_OnSoilAtMinimumRefusal_FeedbackUsesSoilAtMinimumKey()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateRefusal(
                AgrochemicalApplicationOutcome.RefusedSoilAtMinimum,
                ModConstants.TranslationKeyAgrochemicalSoilAtMinimum));
        Item heldItem = harness.Player.CurrentItem!;

        bool result = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.False(result);
        Assert.Equal(3, heldItem.Stack);
        (string Message, bool IsRefusal) feedback = Assert.Single(harness.Feedback);
        Assert.True(feedback.IsRefusal);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalSoilAtMinimum, feedback.Message);
        AssertNotSuccessFeedback(harness.Feedback);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    // ---------------------------------------------------------------------
    // T019 - feedback failures and null fallback
    // ---------------------------------------------------------------------

    /// <summary>
    /// T019 (FR-007, FR-013, SC-002): feedback throwing after a real commit
    /// must not escape, undo the commit, consume a second item, re-call the
    /// service, or suppress the post-commit broadcast.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_WithThrowingSuccessFeedback_CommitsConsumesBroadcastsAndDoesNotThrow()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        harness.Controller.ShowAgrochemicalFeedback = (_, _) => throw new InvalidOperationException("hud down");
        Item heldItem = harness.Player.CurrentItem!;
        bool result = false;

        Exception? escaped = Record.Exception(
            () => result = harness.Controller.TryApplyAgrochemicalWithLock(
                harness.Fixture.Location,
                Tile,
                FarmerId));

        Assert.Null(escaped);
        Assert.True(result);
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Once);
        Assert.Equal(2, heldItem.Stack);
        AssertSingleBroadcast(harness.Multiplayer);
        AssertNoControllerSoilWrites(harness.Soil);
    }

    /// <summary>
    /// T019: a null feedback delegate is a silent safe fallback. The committed
    /// outcome still consumes exactly one item and broadcasts once.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_WithNullFeedback_CommitsConsumesAndBroadcasts()
    {
        var harness = new ControllerHarness();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        harness.Controller.ShowAgrochemicalFeedback = null;
        Item heldItem = harness.Player.CurrentItem!;
        bool result = false;

        Exception? escaped = Record.Exception(
            () => result = harness.Controller.TryApplyAgrochemicalWithLock(
                harness.Fixture.Location,
                Tile,
                FarmerId));

        Assert.Null(escaped);
        Assert.True(result);
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Once);
        Assert.Equal(2, heldItem.Stack);
        AssertSingleBroadcast(harness.Multiplayer);
        AssertNoControllerSoilWrites(harness.Soil);
    }

    /// <summary>
    /// T019 (C4): a refusal whose feedback path throws still refuses safely:
    /// no service call, no consumption, no broadcast, and no escaped exception.
    /// </summary>
    [Fact]
    public void TryApplyAgrochemical_WithThrowingFeedbackOnAuthorityRefusal_DoesNotMutateOrThrow()
    {
        var harness = new ControllerHarness();
        harness.Controller.IsMainPlayerCheck = () => false;
        harness.Controller.ShowAgrochemicalFeedback = (_, _) => throw new InvalidOperationException("hud down");
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());
        Item heldItem = harness.Player.CurrentItem!;
        bool result = true;

        Exception? escaped = Record.Exception(
            () => result = harness.Controller.TryApplyAgrochemicalWithLock(
                harness.Fixture.Location,
                Tile,
                FarmerId));

        Assert.Null(escaped);
        Assert.False(result);
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Never);
        Assert.Equal(3, heldItem.Stack);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    /// <summary>
    /// T082 (FR-007, FR-008): the service returns a refusal outcome and the
    /// feedback presentation throws. No commit happened (the service refused),
    /// so the controller must not consume, broadcast, or write soil, and the
    /// exception must not escape. Mirrors the authority-refusal throwing case.
    /// </summary>
    // characterization — the current controller swallows the feedback exception
    // on the refusal path; this test pins that fallback ordering.
    [Fact]
    public void TryApplyAgrochemical_WithThrowingFeedbackOnServiceRefusal_DoesNotMutateConsumeOrBroadcast()
    {
        var harness = new ControllerHarness();
        var order = new List<string>();
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Callback(() => order.Add("service"))
            .Returns(CreateRefusal(
                AgrochemicalApplicationOutcome.RefusedSoilAtMinimum,
                ModConstants.TranslationKeyAgrochemicalSoilAtMinimum));
        harness.Controller.ShowAgrochemicalFeedback = (_, _) =>
        {
            order.Add("feedback");
            throw new InvalidOperationException("hud down");
        };
        Item heldItem = harness.Player.CurrentItem!;
        bool result = true;

        Exception? escaped = Record.Exception(
            () => result = harness.Controller.TryApplyAgrochemicalWithLock(
                harness.Fixture.Location,
                Tile,
                FarmerId));

        Assert.Null(escaped);
        Assert.False(result, "A service refusal must not be reported as success.");
        Assert.Equal(new[] { "service", "feedback" }, order);
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Once,
            "The service was consulted exactly once; its refusal must end the path.");
        Assert.Equal(3, heldItem.Stack);
        AssertNoControllerSoilWrites(harness.Soil);
        AssertNoBroadcast(harness.Multiplayer);
    }

    // ---------------------------------------------------------------------
    // T085 - repeated applications on the same held stack
    // ---------------------------------------------------------------------

    /// <summary>
    /// T085 (spec edge case: repeated applications; FR-005 partial): two
    /// chained applications on the same held stack (4 → 3 → 2) consume two
    /// items, run the service twice, release the lock twice, show success
    /// feedback twice, and broadcast exactly twice — with no hidden one-shot
    /// or cooldown state suppressing the second application.
    /// </summary>
    // characterization — repeated-application behavior already holds against
    // the current controller; this test pins it against regressions.
    [Fact]
    public void TryApplyAgrochemical_ChainedTwoApplicationsOnSameStack_ConsumeTwiceBroadcastTwiceAndKeepWorking()
    {
        var harness = new ControllerHarness();
        harness.Player.CurrentItem!.Stack = 4;
        harness.Agro
            .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
            .Returns(CreateBaseline50Success());

        bool first = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);
        bool second = harness.Controller.TryApplyAgrochemicalWithLock(
            harness.Fixture.Location,
            Tile,
            FarmerId);

        Assert.True(first, "The first application on a stack of 4 must succeed.");
        Assert.True(second, "The second application on the same held stack must not be blocked by hidden one-shot state.");
        harness.Agro.Verify(
            x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()),
            Times.Exactly(2));
        harness.Lock.Verify(
            x => x.TryAcquireLock("Farm", Tile, FarmerId),
            Times.Exactly(2));
        harness.Lock.Verify(
            x => x.ReleaseLock("Farm", Tile, FarmerId),
            Times.Exactly(2));
        Assert.Equal(2, harness.Player.CurrentItem!.Stack);
        Assert.Equal(2, harness.Feedback.Count);
        Assert.All(
            harness.Feedback,
            entry => Assert.False(entry.IsRefusal, "Each successful application shows success feedback."));
        AssertBroadcastCount(harness.Multiplayer, 2);
        AssertNoControllerSoilWrites(harness.Soil);
    }

    // ---------------------------------------------------------------------
    // T020 - refusal invariants for every service refusal class
    // ---------------------------------------------------------------------

    /// <summary>
    /// T020 (SC-002): for every service refusal outcome, with a valid target,
    /// held input, and available lock, the controller returns false, consumes
    /// nothing, emits only refusal feedback, broadcasts nothing, and never
    /// writes soil health itself.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ForAnyServiceRefusalOutcome_NoConsumptionNoSuccessFeedbackNoBroadcastNoSoilWrite()
    {
        Gen<AgrochemicalApplicationOutcome> refusalOutcomes = Gen.Elements(
            AgrochemicalApplicationOutcome.RefusedInvalidTarget,
            AgrochemicalApplicationOutcome.RefusedMissingInput,
            AgrochemicalApplicationOutcome.RefusedConflict,
            AgrochemicalApplicationOutcome.RefusedSoilAtMinimum);

        return FsCheck.Prop.ForAll(refusalOutcomes.ToArbitrary(), outcome =>
        {
            // Fresh mocks and a fresh game location per generated case keep the
            // property from sharing consumption, lock, or feedback state.
            var harness = new ControllerHarness();
            harness.Agro
                .Setup(x => x.Apply(It.IsAny<GameLocation>(), It.IsAny<Vector2>()))
                .Returns(CreateRefusal(outcome, RefusalMessageKeyFor(outcome)));
            Item heldItem = harness.Player.CurrentItem!;

            bool result = harness.Controller.TryApplyAgrochemicalWithLock(
                harness.Fixture.Location,
                Tile,
                FarmerId);

            bool refused = !result;
            bool serviceCalledOnce = harness.Agro.Invocations.Count == 1;
            bool itemNotConsumed = heldItem.Stack == 3;
            bool refusalFeedbackOnly = harness.Feedback.Count == 1 && harness.Feedback[0].IsRefusal;
            bool noSuccessFeedback = harness.Feedback.All(feedback => feedback.IsRefusal);
            bool noBroadcast = harness.Multiplayer.Invocations.Count == 0;
            bool noControllerSoilWrite = HasNoSoilWriteInvocation(harness.Soil);

            return refused
                && serviceCalledOnce
                && itemNotConsumed
                && refusalFeedbackOnly
                && noSuccessFeedback
                && noBroadcast
                && noControllerSoilWrite;
        });
    }

    // ---------------------------------------------------------------------
    // Test harness and factories
    // ---------------------------------------------------------------------

    /// <summary>
    /// Builds a loose-mock controller plus one valid farm location with a tilled
    /// tile at <see cref="Tile"/>, a three-item agrochemical stack in the held
    /// slot, and the FR-009 broadcast prerequisites wired.
    /// </summary>
    private sealed class ControllerHarness
    {
        public Mock<IModHelper> Helper { get; } = new();
        public Mock<IMonitor> Monitor { get; } = new();
        public Mock<IManifest> Manifest { get; } = new();
        public Mock<ISoilHealthService> Soil { get; } = new();
        public Mock<ISaveIdProvider> SaveIdProvider { get; } = new();
        public Mock<ICompostingBinService> CompostingBinService { get; } = new();
        public Mock<ISoilDecayService> SoilDecayService { get; } = new();
        public Mock<IAgrochemicalApplicationService> Agro { get; } = new();
        public Mock<IHostAtomicLockService> Lock { get; } = new();
        public Mock<IMultiplayerHelper> Multiplayer { get; } = new();
        public PlayerProviderStub Player { get; } = new();
        public List<(string Message, bool IsRefusal)> Feedback { get; } = new();
        public GameLocationFixture Fixture { get; }
        public ModController Controller { get; }

        public ControllerHarness(
            bool hasCrop = false,
            bool includeLockService = true,
            bool includeAgrochemicalService = true)
        {
            Manifest.Setup(x => x.UniqueID).Returns("test.mod.id");

            Fixture = new GameLocationFixture("Farm", isFarm: true);
            Fixture.AddHoeDirtTile(10, 10, hasCrop);
            Player.CurrentItem = CreateAgrochemicalStack();

            Helper.Setup(x => x.Multiplayer).Returns(Multiplayer.Object);
            SaveIdProvider.Setup(x => x.GetSaveId()).Returns("save1");
            Soil.Setup(x => x.Snapshot()).Returns(Array.Empty<SoilHealthSnapshotEntry>());
            Lock
                .Setup(x => x.TryAcquireLock(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<long>()))
                .Returns(true);

            Controller = new ModController(
                Helper.Object,
                Monitor.Object,
                Manifest.Object,
                Soil.Object,
                SaveIdProvider.Object,
                CompostingBinService.Object,
                SoilDecayService.Object,
                hostAtomicLockService: includeLockService ? Lock.Object : null,
                playerProvider: Player,
                agrochemicalApplicationService: includeAgrochemicalService ? Agro.Object : null);

            Controller.IsMainPlayerCheck = () => true;
            Controller.GetCurrentDay = () => 5;
            Controller.ShowAgrochemicalFeedback = (message, isRefusal) => Feedback.Add((message, isRefusal));
        }

        private static Item CreateAgrochemicalStack()
        {
            Item item = ItemFactory.CreateItem(ModConstants.AgrochemicalItemId);
            item.Stack = 3;
            return item;
        }
    }

    private static AgrochemicalApplicationResult CreateBaseline50Success()
    {
        return new AgrochemicalApplicationResult(
            AgrochemicalApplicationOutcome.Success,
            ModConstants.TranslationKeyAgrochemicalApplied,
            50f,
            40f,
            ModConstants.AgrochemicalSoilPenalty,
            ModConstants.AgrochemicalSoilPenalty,
            false,
            false);
    }

    private static AgrochemicalApplicationResult CreateClampedBaseline5Success()
    {
        return new AgrochemicalApplicationResult(
            AgrochemicalApplicationOutcome.Success,
            ModConstants.TranslationKeyAgrochemicalAppliedClamped,
            5f,
            ModConstants.MinSoilHealth,
            ModConstants.AgrochemicalSoilPenalty,
            5f,
            true,
            false);
    }

    private static AgrochemicalApplicationResult CreateUnknownBaselineSuccess()
    {
        return new AgrochemicalApplicationResult(
            AgrochemicalApplicationOutcome.Success,
            ModConstants.TranslationKeyAgrochemicalAppliedUnknownBaseline,
            ModConstants.MaxSoilHealth,
            90f,
            ModConstants.AgrochemicalSoilPenalty,
            ModConstants.AgrochemicalSoilPenalty,
            false,
            true);
    }

    private static AgrochemicalApplicationResult CreateRefusal(
        AgrochemicalApplicationOutcome outcome,
        string messageKey)
    {
        return new AgrochemicalApplicationResult(
            outcome,
            messageKey,
            ModConstants.MinSoilHealth,
            ModConstants.MinSoilHealth,
            ModConstants.AgrochemicalSoilPenalty,
            ModConstants.MinSoilHealth,
            true,
            false);
    }

    private static string RefusalMessageKeyFor(AgrochemicalApplicationOutcome outcome)
    {
        switch (outcome)
        {
            case AgrochemicalApplicationOutcome.RefusedInvalidTarget:
                return ModConstants.TranslationKeyAgrochemicalInvalidTarget;
            case AgrochemicalApplicationOutcome.RefusedMissingInput:
                return ModConstants.TranslationKeyAgrochemicalMissingInput;
            case AgrochemicalApplicationOutcome.RefusedConflict:
                return ModConstants.TranslationKeyAgrochemicalActionConflict;
            case AgrochemicalApplicationOutcome.RefusedSoilAtMinimum:
                return ModConstants.TranslationKeyAgrochemicalSoilAtMinimum;
            default:
                return "message.agrochemical.unexpected-refusal";
        }
    }

    private static void AssertSingleRefusal(
        IReadOnlyList<(string Message, bool IsRefusal)> feedback,
        string expectedMessageKey)
    {
        (string Message, bool IsRefusal) entry = Assert.Single(feedback);
        Assert.True(entry.IsRefusal, "A refusal outcome must be displayed as refusal feedback.");
        Assert.Equal(expectedMessageKey, entry.Message);
    }

    private static void AssertSingleSuccess(
        IReadOnlyList<(string Message, bool IsRefusal)> feedback,
        string expectedMessageKey)
    {
        (string Message, bool IsRefusal) entry = Assert.Single(feedback);
        Assert.False(entry.IsRefusal, "A success outcome must be displayed as success feedback.");
        Assert.Equal(expectedMessageKey, entry.Message);
    }

    private static void AssertNotSuccessFeedback(
        IReadOnlyList<(string Message, bool IsRefusal)> feedback)
    {
        Assert.All(
            feedback,
            entry => Assert.True(entry.IsRefusal, "A refusal path must not emit success feedback."));
    }

    private static void AssertNoControllerSoilWrites(Mock<ISoilHealthService> soil)
    {
        soil.Verify(
            x => x.SetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never,
            "The controller must not call SetSoilHealth; the service owns the measured write.");
        soil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never,
            "The controller must not call UpdateHealth.");
    }

    private static bool HasNoSoilWriteInvocation(Mock<ISoilHealthService> soil)
    {
        return soil.Invocations.All(
            invocation => invocation.Method.Name != nameof(ISoilHealthService.SetSoilHealth)
                && invocation.Method.Name != nameof(ISoilHealthService.UpdateHealth));
    }

    private static void AssertSingleBroadcast(Mock<IMultiplayerHelper> multiplayer)
    {
        AssertBroadcastCount(multiplayer, 1);
    }

    private static void AssertBroadcastCount(Mock<IMultiplayerHelper> multiplayer, int expected)
    {
        multiplayer.Verify(
            m => m.SendMessage(
                It.IsAny<SoilHealthSyncMessage>(),
                ModController.SoilHealthSyncMessageType,
                It.IsAny<string[]>(),
                It.IsAny<long[]>()),
            Times.Exactly(expected),
            "FR-009: the host broadcasts exactly one soil-health snapshot per successful commit.");
    }

    private static void AssertNoBroadcast(Mock<IMultiplayerHelper> multiplayer)
    {
        multiplayer.Verify(
            m => m.SendMessage(
                It.IsAny<SoilHealthSyncMessage>(),
                ModController.SoilHealthSyncMessageType,
                It.IsAny<string[]>(),
                It.IsAny<long[]>()),
            Times.Never,
            "A refused or failed attempt must not broadcast an FR-009 success snapshot.");
    }
}
