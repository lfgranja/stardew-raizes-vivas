using System;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// 017 persistence tests (T011). These exercise the existing
/// <see cref="ISoilHealthService.SaveData"/>/<see cref="ISoilHealthService.LoadData"/>
/// seam with a mocked <see cref="IModDataService"/> capture, mirroring
/// <see cref="SaveReloadConsistencyTests"/>. The implementation passes and a green
/// suite here is the evidence. This is mocked-seam evidence only:
/// it does not touch disk, a real save file, or a running game.
/// </summary>
public class AgrochemicalPersistenceTests
{
    private const string SaveId = "test_agrochemical_roundtrip";

    private static readonly Vector2 Tile = new Vector2(10, 10);

    private readonly Mock<IModDataService> _mockModDataService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<IFileNameSanitizationService> _mockFileNameSanitizationService;

    public AgrochemicalPersistenceTests()
    {
        _mockModDataService = new Mock<IModDataService>();
        _mockMonitor = new Mock<IMonitor>();
        _mockFileNameSanitizationService = new Mock<IFileNameSanitizationService>();

        // Pass-through sanitizer, matching the existing save/load test seam.
        _mockFileNameSanitizationService
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Returns((string value) => value);
    }

    // ------------------------------------------------------------------
    // T011: unknown/NaN baseline becomes measured 90 and survives the seam
    // ------------------------------------------------------------------

    /// <summary>
    /// T011: a never-measured tile is unknown (NaN) before application. The
    /// service resolves the baseline to 100, writes an explicit measured 90,
    /// and the existing save/load seam round-trips that measured value through
    /// a fresh <see cref="SoilHealthService"/> as 90 — not as Unknown.
    /// </summary>
    [Fact]
    public void UnknownBaseline_Apply_SaveReload_PersistsMeasuredNinety()
    {
        // Arrange
        var getCapturedState = SetupSaveLoadCapture();
        var service = CreateSoilHealthService();
        var fixture = new GameLocationFixture("Farm", isFarm: true);

        Assert.True(float.IsNaN(service.GetSoilHealth("Farm", Tile)));
        Assert.False(service.TryGetSoilHealth("Farm", Tile, out _));

        var agrochemicalService = new AgrochemicalApplicationService(service);

        // Act: apply to the unknown tile.
        var result = agrochemicalService.Apply(fixture.Location, Tile);

        // Assert: the application produced a measured 90 in the live cache.
        Assert.True(result.Succeeded);
        Assert.True(result.BaselineWasUnknown);
        Assert.Equal(ModConstants.MaxSoilHealth, result.BaselineUsed, 4);
        Assert.Equal(90f, result.PostPenalty, 4);
        Assert.Equal(90f, service.GetSoilHealth("Farm", Tile), 4);

        // Act: round trip through the existing save/load seam.
        service.SaveData(SaveId);

        var capturedState = getCapturedState();
        Assert.NotNull(capturedState);
        Assert.True(capturedState.LocationHealthData.ContainsKey("Farm"));
        Assert.Equal(90f, capturedState.LocationHealthData["Farm"]["10,10"], 4);

        var reloadedService = CreateSoilHealthService();
        reloadedService.LoadData(SaveId);

        // Assert: 90 is measured after reload, not the unknown sentinel.
        Assert.Equal(90f, reloadedService.GetSoilHealth("Farm", Tile), 4);
        Assert.True(reloadedService.TryGetSoilHealth("Farm", Tile, out var reloadedHealth));
        Assert.Equal(90f, reloadedHealth, 4);
        Assert.InRange(
            reloadedService.GetSoilHealth("Farm", Tile),
            ModConstants.MinSoilHealth,
            ModConstants.MaxSoilHealth);
    }

    // ------------------------------------------------------------------
    // T011: measured baseline case
    // ------------------------------------------------------------------

    /// <summary>
    /// T011: a measured baseline of 50 must persist its measured post-penalty
    /// value of 40 across the same existing save/load seam.
    /// </summary>
    [Fact]
    public void MeasuredBaseline_Apply_SaveReload_PersistsMeasuredForty()
    {
        // Arrange
        var getCapturedState = SetupSaveLoadCapture();
        var service = CreateSoilHealthService();
        var fixture = new GameLocationFixture("Farm", isFarm: true);

        service.SetSoilHealth("Farm", Tile, 50f);
        Assert.Equal(50f, service.GetSoilHealth("Farm", Tile), 4);

        var agrochemicalService = new AgrochemicalApplicationService(service);

        // Act
        var result = agrochemicalService.Apply(fixture.Location, Tile);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.BaselineWasUnknown);
        Assert.Equal(50f, result.BaselineUsed, 4);
        Assert.Equal(40f, result.PostPenalty, 4);
        Assert.Equal(40f, service.GetSoilHealth("Farm", Tile), 4);

        service.SaveData(SaveId);

        var capturedState = getCapturedState();
        Assert.NotNull(capturedState);
        Assert.True(capturedState.LocationHealthData.ContainsKey("Farm"));
        Assert.Equal(40f, capturedState.LocationHealthData["Farm"]["10,10"], 4);

        var reloadedService = CreateSoilHealthService();
        reloadedService.LoadData(SaveId);

        Assert.Equal(40f, reloadedService.GetSoilHealth("Farm", Tile), 4);
        Assert.True(reloadedService.TryGetSoilHealth("Farm", Tile, out var reloadedHealth));
        Assert.Equal(40f, reloadedHealth, 4);
    }

    // ------------------------------------------------------------------
    // T011: sparse-save semantics are preserved
    // ------------------------------------------------------------------

    /// <summary>
    /// T011 sparse semantics: applying to a baseline of 5 clamps the result to
    /// zero. The existing <see cref="SoilHealthService"/> save seam drops
    /// zero-valued entries, so the captured state has no tile and a fresh load
    /// reads Unknown. This documents the existing sparse seam, not real disk I/O.
    /// </summary>
    [Fact]
    public void SubPenaltyBaseline_Apply_Save_OmitsZeroFromSparseState_AndReloadsUnknown()
    {
        // Arrange
        var getCapturedState = SetupSaveLoadCapture();
        var service = CreateSoilHealthService();
        var fixture = new GameLocationFixture("Farm", isFarm: true);

        service.SetSoilHealth("Farm", Tile, 5f);

        var agrochemicalService = new AgrochemicalApplicationService(service);

        // Act
        var result = agrochemicalService.Apply(fixture.Location, Tile);

        // Assert: the bounded result is zero and is held as a measured zero.
        Assert.True(result.Succeeded);
        Assert.Equal(5f, result.BaselineUsed, 4);
        Assert.Equal(0f, result.PostPenalty, 4);
        Assert.Equal(5f, result.AppliedPenalty, 4);
        Assert.True(result.WasClamped);
        Assert.Equal(0f, service.GetSoilHealth("Farm", Tile), 4);

        service.SaveData(SaveId);

        // Existing sparse-save behavior: zero values are omitted from the state.
        var capturedState = getCapturedState();
        Assert.NotNull(capturedState);
        Assert.False(
            capturedState.LocationHealthData.ContainsKey("Farm")
            && capturedState.LocationHealthData["Farm"].ContainsKey("10,10"));

        var reloadedService = CreateSoilHealthService();
        reloadedService.LoadData(SaveId);

        // The sparse seam cannot distinguish a dropped zero from never-measured.
        Assert.True(float.IsNaN(reloadedService.GetSoilHealth("Farm", Tile)));
        Assert.False(reloadedService.TryGetSoilHealth("Farm", Tile, out _));
    }

    // ------------------------------------------------------------------
    // T071: recovery after agrochemical harm stays bounded and valid
    // ------------------------------------------------------------------

    /// <summary>
    /// T071 (US1/AC3, edge case "Compost/regenerative recovery after agrochemical
    /// harm"): a never-measured tile is penalized by the agrochemical to a measured
    /// 90, then the existing regenerative path (<see cref="CompostApplicationService"/>)
    /// heals it. The recovered value must stay within the soil bounds, must not be
    /// NaN, must equal what the regenerative path computes
    /// (<c>min(MaxSoilHealth, current + RestorationAmount)</c>) — i.e. the recovery
    /// does not bypass normal soil validation — and must survive the save/load seam
    /// as a measured value.
    /// </summary>
    [Fact]
    public void UnknownBaseline_ApplyThenCompost_RecoveryStaysWithinBounds()
    {
        // characterization: already green
        // Arrange
        var getCapturedState = SetupSaveLoadCapture();
        var service = CreateSoilHealthService();
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        fixture.AddHoeDirtTile(10, 10);
        var playerStub = new PlayerProviderStub
        {
            CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId)
        };
        var compost = new CompostApplicationService(service, playerStub, _mockMonitor.Object);
        var agrochemical = new AgrochemicalApplicationService(service);

        // Act 1: apply the agrochemical to the never-measured tile.
        var applyResult = agrochemical.Apply(fixture.Location, Tile);
        Assert.True(applyResult.Succeeded);
        Assert.Equal(90f, applyResult.PostPenalty, 4);

        // Act 2: the regenerative path heals the same tile.
        var compostApplied = compost.TryApplyCompost(fixture.Location, Tile);

        // Assert: recovery remains possible and stays within validity rules.
        Assert.True(compostApplied);
        var expected = Math.Min(90f + ModConstants.RestorationAmount, ModConstants.MaxSoilHealth);
        var recovered = service.GetSoilHealth("Farm", Tile);
        Assert.InRange(recovered, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        Assert.Equal(expected, recovered, 4);
        Assert.False(float.IsNaN(recovered));
        Assert.True(recovered >= applyResult.PostPenalty, "recovery must move soil back toward the bound, never below the measured post-penalty value");

        // The save/load seam round-trips the recovered measured value.
        service.SaveData(SaveId);
        var capturedState = getCapturedState();
        Assert.NotNull(capturedState);
        Assert.Equal(expected, capturedState!.LocationHealthData["Farm"]["10,10"], 4);
    }

    // ------------------------------------------------------------------
    // Helpers (same mocked seam as SaveReloadConsistencyTests)
    // ------------------------------------------------------------------

    /// <summary>
    /// Captures the state handed to <see cref="IModDataService.SaveData{T}"/> and
    /// returns it from <see cref="IModDataService.LoadData{T}"/> for the same key.
    /// Returns a live accessor for the captured state (null until save).
    /// </summary>
    private Func<SoilHealthState?> SetupSaveLoadCapture()
    {
        SoilHealthState? capturedState = null;
        string? savedKey = null;

        _mockModDataService
            .Setup(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
            .Callback<SoilHealthState, string>((state, key) =>
            {
                capturedState = state;
                savedKey = key;
            });

        _mockModDataService
            .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
            .Returns((string key) => key == savedKey ? capturedState : null);

        return () => capturedState;
    }

    /// <summary>
    /// Creates a fresh <see cref="SoilHealthService"/> over the shared mocked seam.
    /// </summary>
    private SoilHealthService CreateSoilHealthService()
    {
        return new SoilHealthService(
            _mockModDataService.Object,
            _mockMonitor.Object,
            _mockFileNameSanitizationService.Object);
    }
}
