using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariant tests for soil health persistence (SC-007).
/// Verifies that SoilHealthState sparse save only persists non-default
/// (non-zero) values, that HealthValue is always clamped to [0, 100],
/// that NaN/Infinity values are handled gracefully, that save/load
/// roundtrips preserve values, and that the dictionary structure
/// (location → tile → value) is consistent.
/// </summary>
/// <remarks>
/// Uses FsCheck for property-based testing. FsCheckFixture.Register() is
/// called in the constructor so FsCheck.Xunit.Property-attributed methods
/// can take fixture types directly as parameters. Per-case isolation
/// (T280(b) pattern): fresh service instances per property case.
/// </remarks>
public class SoilHealthPersistenceInvariantsTests
{
    private readonly Mock<IModDataService> _mockModDataService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<IFileNameSanitizationService> _mockFileNameSanitizationService;

    public SoilHealthPersistenceInvariantsTests()
    {
        FsCheckFixture.Register();

        _mockModDataService = new Mock<IModDataService>();
        _mockMonitor = new Mock<IMonitor>();
        _mockFileNameSanitizationService = new Mock<IFileNameSanitizationService>();
    }

    // ------------------------------------------------------------------
    // Sparse save invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: for any non-zero health value, after save → load the value
    /// is preserved. Only non-default (non-zero) values are persisted.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SaveLoad_NonZeroValue_Preserved(float nonZeroHealth)
    {
        float clamped = ClampForTest(nonZeroHealth);

        if (Math.Abs(clamped) < 0.0001f)
        {
            return; // Zero values are not persisted (sparse save)
        }

        var (service, GetCapturedState) = SetupSaveLoadCapture();
        var tile = new Vector2(10, 10);

        service.SetSoilHealth("Farm", tile, clamped);
        service.SaveData("invariant_save");

        Assert.NotNull(GetCapturedState());
        Assert.True(GetCapturedState().LocationHealthData.ContainsKey("Farm"));
        var farmData = GetCapturedState().LocationHealthData["Farm"];
        Assert.True(farmData.ContainsKey("10,10"));
        Assert.Equal(clamped, farmData["10,10"]);
    }

    /// <summary>
    /// Invariant: zero health values are NOT persisted (sparse save).
    /// Setting health to 0 results in no entry in the saved state.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SaveData_ZeroValue_NotPersisted(int arbitraryX, int arbitraryY)
    {
        int x = arbitraryX % 100;
        int y = arbitraryY % 100;
        var (service, GetCapturedState) = SetupSaveLoadCapture();
        var tile = new Vector2(x, y);

        service.SetSoilHealth("Farm", tile, 0f);
        service.SaveData("invariant_save");

        Assert.NotNull(GetCapturedState());
        if (GetCapturedState().LocationHealthData.ContainsKey("Farm"))
        {
            Assert.False(GetCapturedState().LocationHealthData["Farm"].ContainsKey($"{x},{y}"));
        }
    }

    // ------------------------------------------------------------------
    // Clamping invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: for any finite health value, after save the persisted
    /// value is within [ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth].
    /// </summary>
    [Property(MaxTest = 100)]
    public void SaveData_AnyValue_PersistedValueWithinBounds(float health)
    {
        if (!float.IsFinite(health))
        {
            return;
        }

        var (service, GetCapturedState) = SetupSaveLoadCapture();
        var tile = new Vector2(10, 10);

        service.SetSoilHealth("Farm", tile, health);
        service.SaveData("invariant_save");

        Assert.NotNull(GetCapturedState());
        if (GetCapturedState().LocationHealthData.ContainsKey("Farm") &&
            GetCapturedState().LocationHealthData["Farm"].ContainsKey("10,10"))
        {
            float persisted = GetCapturedState().LocationHealthData["Farm"]["10,10"];
            Assert.InRange(persisted, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        }
    }

    // ------------------------------------------------------------------
    // NaN / Infinity handling invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: NaN and Infinity values are handled gracefully — they
    /// either result in no persisted entry or in a clamped valid value.
    /// Never a NaN/Infinity in the saved state.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SaveData_NaNOrInfinity_NeverPersistedAsNonFinite()
    {
        var nonFiniteGen = Gen.Elements(float.NaN, float.PositiveInfinity, float.NegativeInfinity);

        Prop.ForAll(Arb.From(nonFiniteGen), nonFiniteHealth =>
        {
            var (service, GetCapturedState) = SetupSaveLoadCapture();
            var tile = new Vector2(10, 10);

            service.SetSoilHealth("Farm", tile, nonFiniteHealth);
            service.SaveData("invariant_save");

            if (GetCapturedState() == null) return false;
            if (!GetCapturedState().LocationHealthData.ContainsKey("Farm")) return true;
            if (!GetCapturedState().LocationHealthData["Farm"].ContainsKey("10,10")) return true;

            float persisted = GetCapturedState().LocationHealthData["Farm"]["10,10"];
            return float.IsFinite(persisted);
        }).QuickCheckThrowOnFailure();
    }

    // ------------------------------------------------------------------
    // Save/load roundtrip invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: after a save/load roundtrip, GetSoilHealth returns
    /// the same value that was set, for any in-range health value.
    /// </summary>
    [Property(MaxTest = 100)]
    public void RoundTrip_InRangeValue_Preserved(float inRangeHealth)
    {
        float clamped = ClampForTest(inRangeHealth);

        if (Math.Abs(clamped) < 0.0001f)
        {
            return; // Zero values are not persisted
        }

        var (service, _) = SetupSaveLoadCapture();
        var tile = new Vector2(10, 10);

        service.SetSoilHealth("Farm", tile, clamped);
        service.SaveData("invariant_save");
        service.LoadData("invariant_save");

        Assert.Equal(clamped, service.GetSoilHealth("Farm", tile));
    }

    // ------------------------------------------------------------------
    // Dictionary structure invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: the persisted dictionary structure is consistent —
    /// location → tile key ("X,Y") → value. Multiple locations and tiles
    /// are preserved independently.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SaveData_MultipleLocationsAndTiles_StructureConsistent(float healthA, float healthB)
    {
        float clampedA = ClampForTest(healthA);
        float clampedB = ClampForTest(healthB);

        if (Math.Abs(clampedA) < 0.0001f || Math.Abs(clampedB) < 0.0001f)
        {
            return; // Both must be non-zero to test persistence
        }

        var (service, GetCapturedState) = SetupSaveLoadCapture();

        service.SetSoilHealth("Farm", new Vector2(10, 10), clampedA);
        service.SetSoilHealth("Greenhouse", new Vector2(5, 5), clampedB);
        service.SaveData("invariant_save");

        Assert.NotNull(GetCapturedState());
        Assert.Equal(2, GetCapturedState().LocationHealthData.Count);
        Assert.True(GetCapturedState().LocationHealthData.ContainsKey("Farm"));
        Assert.True(GetCapturedState().LocationHealthData.ContainsKey("Greenhouse"));
        Assert.Equal(clampedA, GetCapturedState().LocationHealthData["Farm"]["10,10"]);
        Assert.Equal(clampedB, GetCapturedState().LocationHealthData["Greenhouse"]["5,5"]);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Clamps a test value to the valid health range, handling NaN/Infinity.
    /// </summary>
    private static float ClampForTest(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return 0f;
        }

        return Math.Clamp(value, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
    }

    /// <summary>
    /// Sets up a fresh SoilHealthService with save/load capture, returning
    /// the service and a live accessor for the captured state (null until
    /// SaveData is called). The sanitizer mock is configured to pass through
    /// values unchanged, matching the pattern used by SoilHealthServiceTests.
    /// </summary>
    private (SoilHealthService Service, Func<SoilHealthState?> GetCapturedState) SetupSaveLoadCapture()
    {
        SoilHealthState? capturedState = null;

        _mockFileNameSanitizationService
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Returns((string value) => value);

        _mockModDataService
            .Setup(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
            .Callback<SoilHealthState, string>((state, key) =>
            {
                capturedState = state;
            });

        _mockModDataService
            .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
            .Returns(() => capturedState);

        var service = new SoilHealthService(
            _mockModDataService.Object,
            _mockMonitor.Object,
            _mockFileNameSanitizationService.Object);

        return (service, () => capturedState);
    }
}
