using System;
using System.Linq;
using System.Reflection;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// 017 agrochemical application service tests. The C2/C3 contract below is
/// implemented by <see cref="AgrochemicalApplicationService"/>; a green suite here
/// is the implementation evidence. Tests marked T064 pin the boolean-commit
/// seam: both <c>SetSoilHealth</c> and <c>TrySetSoilHealth</c> call
/// <c>SetHealthInternal</c>; only <c>TrySetSoilHealth</c> propagates the
/// commit/refusal signal.
/// </summary>
public class AgrochemicalApplicationServiceTests
{
    private static readonly Vector2 Tile = new Vector2(10, 10);

    private readonly GameLocationFixture _fixture = new GameLocationFixture("Farm", isFarm: true);

    // ------------------------------------------------------------------
    // T008: measured baseline success path (FR-004, FR-005, FR-006, SC-001)
    // ------------------------------------------------------------------

    /// <summary>
    /// T008: a measured in-range baseline applies the configured penalty as an
    /// explicit absolute write. A full 10-point penalty must not report clamping.
    /// </summary>
    [Theory]
    [InlineData(50f, 40f)]
    [InlineData(100f, 90f)]
    public void MeasuredBaseline_AppliesExplicitPenalty(float baseline, float expectedPostPenalty)
    {
        // Arrange
        var mockSoil = CreateSoilMock(baseline);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, Tile);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.Success, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalApplied, result.MessageKey);
        Assert.Equal(baseline, result.BaselineUsed, 4);
        Assert.Equal(expectedPostPenalty, result.PostPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.RequestedPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.AppliedPenalty, 4);
        Assert.False(result.WasClamped);
        Assert.False(result.BaselineWasUnknown);

        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", new Point((int)Tile.X, (int)Tile.Y), expectedPostPenalty),
            Times.Once);
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }

    /// <summary>
    /// Rule 11: applying twice follows the same bounded rule each time. There
    /// is no hidden cooldown, stacking, or one-shot state: the second call sees
    /// the measured 40 and applies the same 10-point penalty to reach 30.
    /// </summary>
    [Fact]
    public void RepeatedApplication_AppliesSameBoundedRule_WithoutHiddenState()
    {
        // Arrange: a stateful soil mock so each Apply reads the previous write.
        var currentHealth = 50f;
        var mockSoil = new Mock<ISoilHealthService>();
        mockSoil
            .Setup(x => x.GetSoilHealth("Farm", Tile))
            .Returns(() => currentHealth);
        mockSoil
            .Setup(x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), It.IsAny<float>()))
            .Callback<string, Point, float>((_, _, value) => currentHealth = value)
            .Returns(true);

        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var first = service.Apply(_fixture.Location, Tile);
        var second = service.Apply(_fixture.Location, Tile);

        // Assert
        Assert.True(first.Succeeded);
        Assert.Equal(50f, first.BaselineUsed, 4);
        Assert.Equal(40f, first.PostPenalty, 4);
        Assert.Equal(10f, first.AppliedPenalty, 4);
        Assert.False(first.WasClamped);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalApplied, first.MessageKey);

        Assert.True(second.Succeeded);
        Assert.Equal(40f, second.BaselineUsed, 4);
        Assert.Equal(30f, second.PostPenalty, 4);
        Assert.Equal(10f, second.AppliedPenalty, 4);
        Assert.False(second.WasClamped);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalApplied, second.MessageKey);

        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", new Point((int)Tile.X, (int)Tile.Y), 40f),
            Times.Once);
        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", new Point((int)Tile.X, (int)Tile.Y), 30f),
            Times.Once);
        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), It.IsAny<float>()),
            Times.Exactly(2));
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // T009: unknown/NaN baseline resolves to MaxSoilHealth (FR-006, SC-001)
    // ------------------------------------------------------------------

    /// <summary>
    /// T009: an unmeasured tile (NaN sentinel) is treated as a known-equivalent
    /// 100-point baseline, written as an absolute 90, and never decremented via
    /// <c>UpdateHealth</c> — a negative delta from an unknown baseline would
    /// silently produce a no-op or an unbounded value.
    /// </summary>
    [Fact]
    public void UnknownBaseline_ResolvesToMaxHealth_AndWritesMeasuredNinety()
    {
        // Arrange
        var mockSoil = CreateSoilMock(float.NaN);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, Tile);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.Success, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalAppliedUnknownBaseline, result.MessageKey);
        Assert.Equal(ModConstants.MaxSoilHealth, result.BaselineUsed, 4);
        Assert.Equal(90f, result.PostPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.RequestedPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.AppliedPenalty, 4);
        Assert.False(result.WasClamped);
        Assert.True(result.BaselineWasUnknown);

        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", new Point((int)Tile.X, (int)Tile.Y), 90f),
            Times.Once);
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.Is<float>(delta => delta < 0f)),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // T010: property invariants for finite measured baselines and unknowns
    // ------------------------------------------------------------------

    /// <summary>
    /// T010: for any finite baseline in [0, 100], the post-penalty value stays
    /// bounded and the reported applied delta equals the real clamped delta.
    /// A measured zero is refused (already at the floor) without a write.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool MeasuredBaseline_ProducesBoundedHonestPenalty(NonNegativeInt baselineHundredths)
    {
        // 0.00 through 100.00 inclusive, in hundredths.
        var baseline = (baselineHundredths.Get % 10001) / 100f;
        var mockSoil = CreateSoilMock(baseline);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        var result = service.Apply(_fixture.Location, Tile);

        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);

        if (baseline == 0f)
        {
            mockSoil.Verify(
                x => x.TrySetSoilHealth(It.IsAny<string>(), It.IsAny<Point>(), It.IsAny<float>()),
                Times.Never);

            return result.Outcome == AgrochemicalApplicationOutcome.RefusedSoilAtMinimum
                && !result.Succeeded
                && result.MessageKey == ModConstants.TranslationKeyAgrochemicalSoilAtMinimum
                && result.BaselineUsed == 0f
                && result.PostPenalty == 0f
                && result.RequestedPenalty == ModConstants.AgrochemicalSoilPenalty
                && result.AppliedPenalty == 0f
                && result.WasClamped
                && !result.BaselineWasUnknown;
        }

        var expectedPostPenalty = Math.Clamp(
            baseline - ModConstants.AgrochemicalSoilPenalty,
            ModConstants.MinSoilHealth,
            ModConstants.MaxSoilHealth);
        var expectedAppliedPenalty = baseline - expectedPostPenalty;

        mockSoil.Verify(
            x => x.TrySetSoilHealth(
                "Farm",
                new Point((int)Tile.X, (int)Tile.Y),
                It.Is<float>(value => Math.Abs(value - expectedPostPenalty) < 0.0001f)),
            Times.Once);

        return result.Succeeded
            && result.Outcome == AgrochemicalApplicationOutcome.Success
            && result.BaselineUsed >= ModConstants.MinSoilHealth
            && result.BaselineUsed <= ModConstants.MaxSoilHealth
            && Math.Abs(result.BaselineUsed - baseline) < 0.0001f
            && result.PostPenalty >= ModConstants.MinSoilHealth
            && result.PostPenalty <= ModConstants.MaxSoilHealth
            && Math.Abs(result.PostPenalty - expectedPostPenalty) < 0.0001f
            && Math.Abs(result.AppliedPenalty - expectedAppliedPenalty) < 0.0001f
            && result.AppliedPenalty >= 0f
            && result.AppliedPenalty <= ModConstants.AgrochemicalSoilPenalty
            && result.RequestedPenalty == ModConstants.AgrochemicalSoilPenalty
            && result.WasClamped == (result.AppliedPenalty < ModConstants.AgrochemicalSoilPenalty)
            && !result.BaselineWasUnknown;
    }

    /// <summary>
    /// T010: the unknown baseline is not a random or inferred value; every
    /// application over an unknown tile uses exactly <see cref="ModConstants.MaxSoilHealth"/>.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool UnknownBaseline_AlwaysUsesExactlyMaxSoilHealth()
    {
        var mockSoil = CreateSoilMock(float.NaN);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        var result = service.Apply(_fixture.Location, Tile);

        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", new Point((int)Tile.X, (int)Tile.Y), 90f),
            Times.Once);
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);

        return result.Succeeded
            && result.BaselineWasUnknown
            && result.BaselineUsed == ModConstants.MaxSoilHealth
            && result.PostPenalty == 90f
            && result.RequestedPenalty == ModConstants.AgrochemicalSoilPenalty
            && result.AppliedPenalty == ModConstants.AgrochemicalSoilPenalty
            && !result.WasClamped
            && result.MessageKey == ModConstants.TranslationKeyAgrochemicalAppliedUnknownBaseline;
    }

    // ------------------------------------------------------------------
    // T012: C1 no-double-counting boundary (FR-003, FR-004, SC-006)
    // ------------------------------------------------------------------

    /// <summary>
    /// T012: the service's only collaborator is <see cref="ISoilHealthService"/>.
    /// It has no member that could reach the 014/015 quality or growth mechanics,
    /// vanilla fertilizer tiers, or Harmony fertilizer hooks, and a successful
    /// application never routes the penalty through <c>UpdateHealth</c>.
    /// These are reflection and mock contracts, not source-text scans.
    /// </summary>
    [Fact]
    public void C1_ServiceBoundary_OnlyDependsOnSoilHealth_AndNeverUsesUpdateHealth()
    {
        // Arrange
        var serviceType = typeof(AgrochemicalApplicationService);
        var constructors = serviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Assert: exactly one constructor dependency, the soil-health seam.
        Assert.Single(constructors);
        var parameters = constructors[0].GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(ISoilHealthService), parameters[0].ParameterType);

        // Assert: no declared member exposes or names quality/growth/fertilizer machinery.
        var forbiddenMemberTokens = new[]
        {
            "HarvestQuality",
            "Harmony",
            "Fertilizer",
            "PhaseDays",
            "Growth"
        };
        var memberNames = serviceType
            .GetMembers(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Select(member => member.Name)
            .ToArray();

        foreach (var token in forbiddenMemberTokens)
        {
            Assert.DoesNotContain(
                memberNames,
                memberName => memberName.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        var mockSoil = CreateSoilMock(50f);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, Tile);

        // Assert: the successful path is soil-health-only.
        Assert.True(result.Succeeded);
        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", new Point((int)Tile.X, (int)Tile.Y), 40f),
            Times.Once);
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // T013: boundary-honest clamping at and near the floor
    // ------------------------------------------------------------------

    /// <summary>
    /// T013: a measured 5 can only take a 5-point penalty. The result exposes
    /// the requested 10, the applied 5, and the clamped message rather than
    /// implying the full nominal penalty was applied.
    /// </summary>
    [Fact]
    public void MeasuredFive_ClampsPenaltyAndReportsAppliedDelta()
    {
        // Arrange
        var mockSoil = CreateSoilMock(5f);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, Tile);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.Success, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalAppliedClamped, result.MessageKey);
        Assert.Equal(5f, result.BaselineUsed, 4);
        Assert.Equal(0f, result.PostPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.RequestedPenalty, 4);
        Assert.Equal(5f, result.AppliedPenalty, 4);
        Assert.True(result.WasClamped);
        Assert.False(result.BaselineWasUnknown);

        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", new Point((int)Tile.X, (int)Tile.Y), 0f),
            Times.Once);
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }

    /// <summary>
    /// T013: measured zero is refused as already at the floor and must not
    /// attempt a write, rather than claiming a successful 10-point penalty.
    /// </summary>
    [Fact]
    public void MeasuredZero_RefusesAtSoilMinimum_WithoutWrites()
    {
        // characterization: already green
        // Arrange
        var mockSoil = CreateSoilMock(0f);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, Tile);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.RefusedSoilAtMinimum, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalSoilAtMinimum, result.MessageKey);
        Assert.Equal(0f, result.BaselineUsed, 4);
        Assert.Equal(0f, result.PostPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.RequestedPenalty, 4);
        Assert.Equal(0f, result.AppliedPenalty, 4);
        Assert.True(result.WasClamped);
        Assert.False(result.BaselineWasUnknown);

        mockSoil.Verify(
            x => x.SetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
        mockSoil.Verify(
            x => x.TrySetSoilHealth(It.IsAny<string>(), It.IsAny<Point>(), It.IsAny<float>()),
            Times.Never);
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }

    /// <summary>
    /// T013: every baseline strictly between the floor and the nominal penalty
    /// applies exactly the available delta, lands on zero, is flagged clamped,
    /// and selects the clamped message.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SubPenaltyBaseline_AppliesOnlyAvailableDelta(NonNegativeInt baselineHundredths)
    {
        // 0.01 through 9.99, strictly between MinSoilHealth and the penalty.
        var baseline = ((baselineHundredths.Get % 999) + 1) / 100f;
        var mockSoil = CreateSoilMock(baseline);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        var result = service.Apply(_fixture.Location, Tile);

        mockSoil.Verify(
            x => x.TrySetSoilHealth(
                "Farm",
                new Point((int)Tile.X, (int)Tile.Y),
                It.Is<float>(value => Math.Abs(value) < 0.0001f)),
            Times.Once);
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);

        return result.Succeeded
            && Math.Abs(result.BaselineUsed - baseline) < 0.0001f
            && result.PostPenalty == 0f
            && Math.Abs(result.AppliedPenalty - baseline) < 0.0001f
            && result.RequestedPenalty == ModConstants.AgrochemicalSoilPenalty
            && result.WasClamped
            && !result.BaselineWasUnknown
            && result.MessageKey == ModConstants.TranslationKeyAgrochemicalAppliedClamped;
    }

    // ------------------------------------------------------------------
    // Invalid target (contract rule 10)
    // ------------------------------------------------------------------

    /// <summary>
    /// Rule 10: a null location is an invalid target; no soil read or write
    /// may occur and the result reports the full requested penalty as unapplied.
    /// </summary>
    [Fact]
    public void InvalidTarget_NullLocation_RefusesWithoutSoilAccess()
    {
        // Arrange
        var mockSoil = CreateSoilMock(50f);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(null!, Tile);

        // Assert
        AssertInvalidTargetResult(result);
        VerifyNoSoilAccess(mockSoil);
    }

    /// <summary>
    /// Rule 10: a null/blank location name is an invalid target; no soil read
    /// or write may occur.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidTarget_BlankLocationName_RefusesWithoutSoilAccess(string locationName)
    {
        // Arrange
        var fixture = new GameLocationFixture(locationName, isFarm: true);
        var mockSoil = CreateSoilMock(50f);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(fixture.Location, Tile);

        // Assert
        AssertInvalidTargetResult(result);
        VerifyNoSoilAccess(mockSoil);
    }

    /// <summary>
    /// T099 (FR-008/FR-006, Phase 10 converge): a location name longer than
    /// <see cref="ModConstants.MaxLocationNameLength"/> cannot be persisted or synchronized
    /// (the persistence layer refuses it), so the application must treat it as an
    /// invalid target — never as a 0f floor reading that falls into the soil-at-minimum
    /// branch. No soil read or write may occur; the overlong-name guard refuses at
    /// the top of <see cref="IAgrochemicalApplicationService.Apply"/> with
    /// <see cref="AgrochemicalApplicationOutcome.RefusedInvalidTarget"/>.
    /// </summary>
    [Fact]
    public void Apply_OverlongLocationName_ReturnsRefusedInvalidTarget()
    {
        // Arrange: one character over the persistence-layer limit. The real
        // SoilHealthService answers 0f for this location (and never persists it),
        // which is exactly the sentinel that misroutes into RefusedSoilAtMinimum.
        var overlongName = new string('A', ModConstants.MaxLocationNameLength + 1);
        var fixture = new GameLocationFixture(overlongName, isFarm: true);
        var mockSoil = new Mock<ISoilHealthService>();
        mockSoil
            .Setup(x => x.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()))
            .Returns(0f); // mirrors the real service's invalid-query sentinel
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(fixture.Location, Tile);

        // Assert
        Assert.Equal(AgrochemicalApplicationOutcome.RefusedInvalidTarget, result.Outcome);
        Assert.False(result.Succeeded);
        Assert.NotEqual(AgrochemicalApplicationOutcome.RefusedSoilAtMinimum, result.Outcome);
        VerifyNoSoilAccess(mockSoil);
    }

    // ------------------------------------------------------------------
    // T064: boolean-commit seam (ISoilHealthService.TrySetSoilHealth)
    // ------------------------------------------------------------------

    /// <summary>
    /// T064 (FR-008/FR-010): when the commit seam reports the tile was
    /// rejected (e.g. capacity limit or an unmappable tile), the service must
    /// fail the application — never report Success — and must not continue as
    /// if the write landed. With Moq the seam is mocked, so no game state is
    /// involved. Item consumption, feedback, and broadcast stay controller-owned
    /// paths and are asserted by the controller tests, not here.
    /// </summary>
    [Fact]
    public void TrySetSoilHealth_ReturnsFalse_ApplicationFailsInsteadOfSuccess()
    {
        // Arrange
        var mockSoil = CreateSoilMock(50f);
        mockSoil
            .Setup(x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), It.IsAny<float>()))
            .Returns(false);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, Tile);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEqual(AgrochemicalApplicationOutcome.Success, result.Outcome);
        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), It.IsAny<float>()),
            Times.Once);
    }

    /// <summary>
    /// T064: when the commit seam reports the measured write landed, the
    /// service reports the committed outcome and routes the commit through
    /// <c>TrySetSoilHealth</c> exactly once.
    /// </summary>
    [Fact]
    public void TrySetSoilHealth_ReturnsTrue_CommitsThroughSeamAndReportsSuccess()
    {
        // Arrange
        var expectedPostPenalty = 40f; // measured 50 - C3 provisional penalty of 10
        var mockSoil = CreateSoilMock(50f);
        mockSoil
            .Setup(x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), expectedPostPenalty))
            .Returns(true);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, Tile);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.Success, result.Outcome);
        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), expectedPostPenalty),
            Times.Once);
    }

    /// <summary>
    /// T064 capacity rejection: <see cref="ModConstants.MaxTilesPerLocation"/>
    /// is 500 tiles per location (DoS guard). Simulating the real overflow here
    /// would require allocating 500 tiles into the cache, so the capacity-refusal
    /// signal is mocked at the seam: the real service returns <c>false</c> from
    /// <c>TrySetSoilHealth</c> when a new tile would exceed the limit, and the
    /// application must fail honestly in that case. Item consumption and
    /// feedback stay controller-owned and are not asserted here.
    /// </summary>
    /// <remarks>
    /// T097 (Phase 10 converge): the commit-drop refusal uses the dedicated
    /// <see cref="AgrochemicalApplicationOutcome.RefusedCommitFailed"/> class with the
    /// <see cref="ModConstants.TranslationKeyAgrochemicalCommitFailed"/> key — never the
    /// action-conflict key and never a success outcome.
    /// </remarks>
    [Fact]
    public void TrySetSoilHealth_CapacityRejection_MockedAtSeam_FailsHonestly()
    {
        // Arrange
        var mockSoil = CreateSoilMock(50f);
        mockSoil
            .Setup(x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), It.IsAny<float>()))
            .Returns(false); // stands in for the TileLimitExceeded capacity refusal
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, Tile);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEqual(AgrochemicalApplicationOutcome.Success, result.Outcome);
        Assert.Equal(AgrochemicalApplicationOutcome.RefusedCommitFailed, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalCommitFailed, result.MessageKey);
        Assert.NotEqual(ModConstants.TranslationKeyAgrochemicalActionConflict, result.MessageKey);
        // The computation did occur; only the commit failed — the honest carried data stays.
        Assert.Equal(50f, result.BaselineUsed, 4);
        Assert.Equal(40f, result.PostPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.RequestedPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.AppliedPenalty, 4);
        Assert.False(result.WasClamped);
        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), It.IsAny<float>()),
            Times.Once);
    }

    /// <summary>
    /// T097 (Phase 10 converge): the same commit-drop rule on the
    /// bounds-reduced path. A measured 5 clamps the penalty to 5; when the seam
    /// then refuses the measured write (capacity), the application must refuse with
    /// <see cref="AgrochemicalApplicationOutcome.RefusedCommitFailed"/> and the
    /// commit-failed key — never a zero-delta Success, and never the action-conflict
    /// key — while still carrying the honestly clamped computation.
    /// </summary>
    [Fact]
    public void TrySetSoilHealth_CommitDropOnClampedPath_RefusesWithCommitFailedKey()
    {
        // Arrange: baseline 5 -> postPenalty 0, applied 5, clamped — then the seam refuses.
        var mockSoil = CreateSoilMock(5f);
        mockSoil
            .Setup(x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), It.IsAny<float>()))
            .Returns(false);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, Tile);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.RefusedCommitFailed, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalCommitFailed, result.MessageKey);
        Assert.NotEqual(ModConstants.TranslationKeyAgrochemicalActionConflict, result.MessageKey);
        Assert.NotEqual(AgrochemicalApplicationOutcome.RefusedConflict, result.Outcome);
        Assert.Equal(5f, result.BaselineUsed, 4);
        Assert.Equal(0f, result.PostPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.RequestedPenalty, 4);
        Assert.Equal(5f, result.AppliedPenalty, 4);
        Assert.True(result.WasClamped);
        mockSoil.Verify(
            x => x.TrySetSoilHealth("Farm", It.IsAny<Point>(), It.IsAny<float>()),
            Times.Once);
    }

    // ------------------------------------------------------------------
    // T074: refusal result honesty (FR-005, FR-008)
    // ------------------------------------------------------------------

    /// <summary>
    /// T074: an invalid-target refusal never read the soil, so it must not
    /// claim a clamped outcome; <c>CreateInvalidTargetResult</c> reports
    /// <c>WasClamped = false</c>.
    /// </summary>
    [Fact]
    public void InvalidTarget_Refusal_DoesNotClaimWasClamped()
    {
        // Arrange
        var service = new AgrochemicalApplicationService(CreateSoilMock(50f).Object);

        // Act
        var result = service.Apply(null!, Tile);

        // Assert
        Assert.False(result.WasClamped);
    }

    /// <summary>
    /// T074: <c>BaselineUsed</c> on the invalid-target refusal is the
    /// documented NaN sentinel ("no baseline was resolved"), not a fabricated
    /// 0f reading. The RefusedSoilAtMinimum
    /// path keeps its real measured 0f baseline and is covered separately.
    /// </summary>
    [Fact]
    public void InvalidTarget_Refusal_BaselineUsedIsNaNSentinel()
    {
        // Arrange
        var service = new AgrochemicalApplicationService(CreateSoilMock(50f).Object);

        // Act
        var result = service.Apply(null!, Tile);

        // Assert
        Assert.True(float.IsNaN(result.BaselineUsed), "invalid-target refusal must carry the NaN BaselineUsed sentinel");
    }

    // ------------------------------------------------------------------
    // T075: unmappable tile is RefusedInvalidTarget, not RefusedSoilAtMinimum
    // ------------------------------------------------------------------

    /// <summary>
    /// T075 (FR-008/FR-010): a tile that cannot be mapped (NaN coordinates)
    /// is an invalid target, not a floor reading. The real <c>SoilHealthService</c>
    /// reports 0f for a malformed query; the service refuses this case before
    /// any soil read or write and answers <see cref="AgrochemicalApplicationOutcome.RefusedInvalidTarget"/>.
    /// </summary>
    [Fact]
    public void Apply_UnmappableTile_ReturnsRefusedInvalidTarget_NotRefusedSoilAtMinimum()
    {
        // Arrange: mirror the real service's invalid-query contract (returns 0f).
        var invalidTile = new Vector2(float.NaN, float.NaN);
        var mockSoil = new Mock<ISoilHealthService>();
        mockSoil
            .Setup(x => x.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()))
            .Returns(0f);
        var service = new AgrochemicalApplicationService(mockSoil.Object);

        // Act
        var result = service.Apply(_fixture.Location, invalidTile);

        // Assert
        Assert.Equal(AgrochemicalApplicationOutcome.RefusedInvalidTarget, result.Outcome);
        Assert.False(result.Succeeded);
        mockSoil.Verify(
            x => x.SetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
        mockSoil.Verify(
            x => x.TrySetSoilHealth(It.IsAny<string>(), It.IsAny<Point>(), It.IsAny<float>()),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // T098: penalty magnitude pin (FR-014, FR-012)
    // ------------------------------------------------------------------

    /// <summary>
    /// T098 characterization: the tunable penalty must stay within (0, 100] so the
    /// shipped "10" copy and the FR-005 bounds agree; pinning the range here turns
    /// a silent penalty edit into a test failure.
    /// </summary>
    [Fact]
    public void AgrochemicalSoilPenalty_IsWithinZeroAndHundred()
    {
        Assert.True(ModConstants.AgrochemicalSoilPenalty > 0f);
        Assert.True(ModConstants.AgrochemicalSoilPenalty <= 100f);
    }

    // ------------------------------------------------------------------
    // T100: real-seam coverage (bound guard); capacity fill deliberately skipped
    // ------------------------------------------------------------------

    /// <summary>
    /// T100 characterization (FR-005): the real <see cref="SoilHealthService"/> rejects
    /// a tile outside the persisted-grid magnitude bound
    /// (<see cref="ModConstants.MaxAbsoluteTileCoordinate"/>) from
    /// <c>TrySetSoilHealth</c> — returns <c>false</c> and leaves the cache untouched.
    /// The 500-tile capacity fill is intentionally not simulated (allocation cost);
    /// the capacity signal is covered at the seam by the T064/T097 mocked-seam tests.
    /// Sub-1e-4 postPenalty storage semantics (store explicit 0f vs keep the
    /// sparse-cache drop) were decided 2026-11-06 (decision (1): the sparse-cache
    /// drop rule is kept unchanged; sub-1e-4 results never persist across save/load,
    /// exact 0f survives only in-memory within a session). See the "Decision
    /// 2026-11-06 — sub-1e-4 postPenalty storage semantics" section of
    /// verification/measured-zero-persistence.md. Not pinned here by test.
    /// </summary>
    [Fact]
    public void RealSeam_TrySetSoilHealth_OutOfBoundTile_ReturnsFalse_WithoutCacheMutation()
    {
        // Arrange: real service over a mocked persistence seam (no disk, no save).
        var mockModDataService = new Mock<IModDataService>();
        var mockMonitor = new Mock<IMonitor>();
        var mockFileNameSanitization = new Mock<IFileNameSanitizationService>();
        mockFileNameSanitization
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Returns((string value) => value);
        var soilService = new SoilHealthService(
            mockModDataService.Object,
            mockMonitor.Object,
            mockFileNameSanitization.Object);
        var outOfBoundTile = new Point(ModConstants.MaxAbsoluteTileCoordinate + 1, 10);

        // Act
        var accepted = soilService.TrySetSoilHealth("Farm", outOfBoundTile, 40f);

        // Assert
        Assert.False(accepted);
        Assert.Empty(soilService.Snapshot());
        Assert.False(soilService.TryGetSoilHealth(
            "Farm",
            new Vector2(outOfBoundTile.X, outOfBoundTile.Y),
            out _));
    }

    private static Mock<ISoilHealthService> CreateSoilMock(float currentHealth)
    {
        var mockSoil = new Mock<ISoilHealthService>();
        mockSoil
            .Setup(x => x.GetSoilHealth("Farm", Tile))
            .Returns(currentHealth);

        // 017 T064: the commit now flows through the boolean-commit seam. Default it
        // to "committed" so success-path tests measure the domain rule, not Moq's
        // default(bool) = false. Refusal tests override this setup after returning.
        mockSoil
            .Setup(x => x.TrySetSoilHealth(It.IsAny<string>(), It.IsAny<Point>(), It.IsAny<float>()))
            .Returns(true);
        return mockSoil;
    }

    private static void AssertInvalidTargetResult(AgrochemicalApplicationResult result)
    {
        // T074: a refusal that never read the soil carries no baseline and must
        // not claim a clamped outcome. BaselineUsed/PostPenalty/AppliedPenalty use
        // the documented NaN sentinels ("no value was resolved"), and WasClamped
        // stays false.
        Assert.False(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.RefusedInvalidTarget, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalInvalidTarget, result.MessageKey);
        Assert.True(float.IsNaN(result.BaselineUsed), "refusal on the invalid-target path must carry the NaN BaselineUsed sentinel");
        Assert.True(float.IsNaN(result.PostPenalty), "refusal on the invalid-target path must carry the NaN PostPenalty sentinel");
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.RequestedPenalty, 4);
        Assert.True(float.IsNaN(result.AppliedPenalty), "refusal on the invalid-target path must carry the NaN AppliedPenalty sentinel");
        Assert.False(result.WasClamped);
        Assert.False(result.BaselineWasUnknown);
    }

    private static void VerifyNoSoilAccess(Mock<ISoilHealthService> mockSoil)
    {
        mockSoil.Verify(
            x => x.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()),
            Times.Never);
        mockSoil.Verify(
            x => x.SetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
        mockSoil.Verify(
            x => x.TrySetSoilHealth(It.IsAny<string>(), It.IsAny<Point>(), It.IsAny<float>()),
            Times.Never);
        mockSoil.Verify(
            x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }
}
