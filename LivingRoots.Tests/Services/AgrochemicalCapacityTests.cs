using System.Collections.Generic;
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
/// 017 T114 (owed by T100): real-service capacity-fill characterization tests.
/// The 500-tile (<see cref="ModConstants.MaxTilesPerLocation"/>) and 50-location
/// (<see cref="ModConstants.MaxLocationsPerSave"/>) guards live inside
/// <see cref="SoilHealthService"/>'s
/// private measured-write path; the cheap, faithful way to fill the runtime cache is the
/// existing <see cref="SoilHealthService.LoadData(string)"/> seam: the save payload
/// mirrors what <see cref="SoilHealthService.SaveData(string)"/> writes
/// (<c>"x,y"</c> keys with non-zero measured values — zero-valued entries are
/// dropped on load), so a location seeded with exactly
/// <see cref="ModConstants.MaxTilesPerLocation"/> entries arrives at the cap
/// without allocating 500 physical game tiles. These tests then exercise the real
/// commit path end to end: <see cref="AgrochemicalApplicationService.Apply"/> →
/// <see cref="ISoilHealthService.TrySetSoilHealth(string, Point, float)"/> → the
/// real guard. No production code is touched; item consumption and feedback are
/// controller-owned and deliberately not asserted.
/// </summary>
public class AgrochemicalCapacityTests
{
    private const string SaveId = "test_agrochemical_capacity";

    private readonly Mock<IModDataService> _mockModDataService = new();
    private readonly Mock<IMonitor> _mockMonitor = new();
    private readonly Mock<IFileNameSanitizationService> _mockFileNameSanitizationService = new();

    public AgrochemicalCapacityTests()
    {
        // Pass-through sanitizer, matching the existing save/load test seam.
        _mockFileNameSanitizationService
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Returns((string value) => value);
    }

    // ------------------------------------------------------------------
    // T114: TileLimitExceeded refusal on a new tile at the per-location cap
    // ------------------------------------------------------------------

    /// <summary>
    /// T114 (a): the real <see cref="SoilHealthService"/> seeded through the
    /// <see cref="SoilHealthService.LoadData(string)"/> seam with one Farm location
    /// holding exactly <see cref="ModConstants.MaxTilesPerLocation"/> measured 50-point
    /// tiles. A new tile in that location cannot grow the cache, so
    /// <see cref="ISoilHealthService.TrySetSoilHealth(string, Point, float)"/> refuses
    /// and the application reports <see cref="AgrochemicalApplicationOutcome.RefusedCommitFailed"/>
    /// with <see cref="ModConstants.TranslationKeyAgrochemicalCommitFailed"/> — never success,
    /// never the action-conflict key. The previously existing tiles stay untouched.
    /// </summary>
    [Fact]
    public void TileLimitExceeded_NewTileAtCap_ApplyRefusesWithCommitFailed()
    {
        // Arrange: seed one Farm location at exactly the per-location cap.
        SeedLoadData(BuildStateWithFarmAtTileLimit());
        var service = CreateSoilHealthService();
        service.LoadData(SaveId);
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var agrochemical = new AgrochemicalApplicationService(service);

        var newTile = new Point(ModConstants.MaxTilesPerLocation + 100, 0);

        // Precondition: the seeded Farm is at exactly the cap, all measured 50.
        Assert.Equal(ModConstants.MaxTilesPerLocation, service.Snapshot().Count);

        // Act: the commit seam refuses a new tile at the cap.
        Assert.False(service.TrySetSoilHealth("Farm", newTile, 90f));

        var result = agrochemical.Apply(fixture.Location, new Vector2(newTile.X, newTile.Y));

        // Assert: refused honestly via the commit-drop class; the computation did occur.
        Assert.False(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.RefusedCommitFailed, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalCommitFailed, result.MessageKey);
        Assert.NotEqual(ModConstants.TranslationKeyAgrochemicalActionConflict, result.MessageKey);
        Assert.Equal(ModConstants.MaxSoilHealth, result.BaselineUsed, 4);
        Assert.Equal(ModConstants.MaxSoilHealth - ModConstants.AgrochemicalSoilPenalty, result.PostPenalty, 4);
        Assert.Equal(ModConstants.AgrochemicalSoilPenalty, result.RequestedPenalty, 4);
        Assert.True(result.BaselineWasUnknown);

        // Previously existing tiles are untouched: count and values unchanged.
        Assert.Equal(ModConstants.MaxTilesPerLocation, service.Snapshot().Count);
        Assert.Equal(50f, service.GetSoilHealth("Farm", new Vector2(0, 0)), 4);
        Assert.Equal(50f, service.GetSoilHealth("Farm", new Vector2(ModConstants.MaxTilesPerLocation - 1, 0)), 4);
        Assert.False(service.TryGetSoilHealth("Farm", new Vector2(newTile.X, newTile.Y), out _));
    }

    // ------------------------------------------------------------------
    // T114: updates to an existing tracked tile are allowed at the cap
    // ------------------------------------------------------------------

    /// <summary>
    /// T114 (b): at exactly <see cref="ModConstants.MaxTilesPerLocation"/> tiles, an
    /// application targeting an existing tracked tile needs no capacity growth, so the
    /// commit lands and the application reports <see cref="AgrochemicalApplicationOutcome.Success"/>.
    /// Updates, not additions, are what the guard permits at the cap.
    /// </summary>
    [Fact]
    public void TileLimitExceeded_ExistingTileAtCap_ApplySucceeds()
    {
        // Arrange: seed one Farm location at exactly the per-location cap.
        SeedLoadData(BuildStateWithFarmAtTileLimit());
        var service = CreateSoilHealthService();
        service.LoadData(SaveId);
        var fixture = new GameLocationFixture("Farm", isFarm: true);
        var agrochemical = new AgrochemicalApplicationService(service);

        var existingTile = new Vector2(0, 0);

        // Act: apply to a tracked seed tile (measured 50 before the penalty).
        var result = agrochemical.Apply(fixture.Location, existingTile);

        // Assert: the update to the existing tile is allowed at the cap.
        Assert.True(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.Success, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalApplied, result.MessageKey);
        Assert.Equal(50f, result.BaselineUsed, 4);
        Assert.Equal(50f - ModConstants.AgrochemicalSoilPenalty, result.PostPenalty, 4);
        Assert.False(result.WasClamped);
        Assert.False(result.BaselineWasUnknown);
        Assert.Equal(50f - ModConstants.AgrochemicalSoilPenalty, service.GetSoilHealth("Farm", existingTile), 4);

        // The cap is still exactly the cap: no tile was added.
        Assert.Equal(ModConstants.MaxTilesPerLocation, service.Snapshot().Count);
    }

    // ------------------------------------------------------------------
    // T114 (bonus): LocationLimitExceeded refusal for a 51st location
    // ------------------------------------------------------------------

    /// <summary>
    /// T114 (c): the runtime cache seeded through the <see cref="SoilHealthService.LoadData(string)"/>
    /// seam with exactly <see cref="ModConstants.MaxLocationsPerSave"/> locations (one measured tile
    /// each — a location with no valid entries never enters the cache). A new, untracked location
    /// cannot be added, so <see cref="ISoilHealthService.TrySetSoilHealth(string, Point, float)"/>
    /// refuses and the application reports <see cref="AgrochemicalApplicationOutcome.RefusedCommitFailed"/>
    /// with <see cref="ModConstants.TranslationKeyAgrochemicalCommitFailed"/>.
    /// </summary>
    [Fact]
    public void LocationLimitExceeded_NewLocationAtCap_ApplyRefusesWithCommitFailed()
    {
        // Arrange: seed 50 locations, each holding one measured tile.
        SeedLoadData(BuildStateWithLocationsAtLimit());
        var service = CreateSoilHealthService();
        service.LoadData(SaveId);
        var fixture = new GameLocationFixture("OverflowLocation", isFarm: false);
        var agrochemical = new AgrochemicalApplicationService(service);

        // Precondition: the cache holds exactly the location cap.
        Assert.Equal(ModConstants.MaxLocationsPerSave, service.Snapshot().Count);

        // Act: the commit seam refuses to create a 51st location.
        Assert.False(service.TrySetSoilHealth("OverflowLocation", new Point(0, 0), 90f));

        var result = agrochemical.Apply(fixture.Location, new Vector2(0, 0));

        // Assert: refused honestly via the commit-drop class.
        Assert.False(result.Succeeded);
        Assert.Equal(AgrochemicalApplicationOutcome.RefusedCommitFailed, result.Outcome);
        Assert.Equal(ModConstants.TranslationKeyAgrochemicalCommitFailed, result.MessageKey);
        Assert.NotEqual(ModConstants.TranslationKeyAgrochemicalActionConflict, result.MessageKey);
        Assert.Equal(ModConstants.MaxSoilHealth, result.BaselineUsed, 4);
        Assert.Equal(ModConstants.MaxSoilHealth - ModConstants.AgrochemicalSoilPenalty, result.PostPenalty, 4);
        Assert.True(result.BaselineWasUnknown);

        // The seeded locations are untouched: no location was added, no tile changed.
        Assert.Equal(ModConstants.MaxLocationsPerSave, service.Snapshot().Count);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Points the mocked persistence seam at the pre-built snapshot so
    /// <see cref="SoilHealthService.LoadData(string)"/> imports it directly —
    /// this is the cheap path; no physical tiles are allocated.
    /// </summary>
    private void SeedLoadData(SoilHealthState state)
    {
        _mockModDataService
            .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
            .Returns(state);
    }

    /// <summary>
    /// One Farm location holding exactly <see cref="ModConstants.MaxTilesPerLocation"/>
    /// measured 50-point tiles, in the same <c>"x,y"</c> key shape that
    /// <see cref="SoilHealthService.SaveData(string)"/> writes (and that
    /// <see cref="SoilHealthService.LoadData(string)"/> parses). Tiles sit at
    /// <c>(i, 0)</c> for <c>i</c> in <c>[0, MaxTilesPerLocation)</c>, all within the
    /// <see cref="ModConstants.MaxAbsoluteTileCoordinate"/> bound.
    /// </summary>
    private static SoilHealthState BuildStateWithFarmAtTileLimit()
    {
        var tiles = new Dictionary<string, float>(ModConstants.MaxTilesPerLocation);
        for (var i = 0; i < ModConstants.MaxTilesPerLocation; i++)
        {
            tiles[i + ",0"] = 50f;
        }

        return new SoilHealthState
        {
            LocationHealthData = new Dictionary<string, Dictionary<string, float>>
            {
                ["Farm"] = tiles
            }
        };
    }

    /// <summary>
    /// <see cref="ModConstants.MaxLocationsPerSave"/> locations, each holding one
    /// measured 50-point tile at <c>(0, 0)</c>. A location with zero valid entries is
    /// dropped by the load path, so each seed location carries one tile; the global
    /// <c>MaxTilesPerSave</c> bound is untouched.
    /// </summary>
    private static SoilHealthState BuildStateWithLocationsAtLimit()
    {
        var locations = new Dictionary<string, Dictionary<string, float>>(ModConstants.MaxLocationsPerSave);
        for (var i = 0; i < ModConstants.MaxLocationsPerSave; i++)
        {
            locations["SeedLoc" + i] = new Dictionary<string, float>
            {
                ["0,0"] = 50f
            };
        }

        return new SoilHealthState
        {
            LocationHealthData = locations
        };
    }

    private SoilHealthService CreateSoilHealthService()
    {
        return new SoilHealthService(
            _mockModDataService.Object,
            _mockMonitor.Object,
            _mockFileNameSanitizationService.Object);
    }
}
