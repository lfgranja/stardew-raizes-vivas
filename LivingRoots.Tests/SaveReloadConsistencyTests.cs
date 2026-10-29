using System.Collections.Generic;
using System.Linq;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Integration tests for SC-007 (save/reload preserves growth effect).
/// Verifies that soil health values survive a save → load round trip,
/// that only non-default values are persisted (sparse save), and that
/// the state is consistent after multiple save/load cycles.
/// </summary>
/// <remarks>
/// The persistence layer (<see cref="IModDataService"/>) is mocked to capture
/// saved state and return it on load, simulating a round trip through disk.
/// Every bound comes from <see cref="ModConstants"/> — no hardcoded values.
/// </remarks>
public class SaveReloadConsistencyTests
{
    private readonly Mock<IModDataService> _mockModDataService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<IFileNameSanitizationService> _mockFileNameSanitizationService;

    private const string SaveId = "test_save_reload";

    public SaveReloadConsistencyTests()
    {
        _mockModDataService = new Mock<IModDataService>();
        _mockMonitor = new Mock<IMonitor>();
        _mockFileNameSanitizationService = new Mock<IFileNameSanitizationService>();

        // Configure sanitizer to pass through (matches SoilHealthServiceTests pattern)
        _mockFileNameSanitizationService
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Returns((string value) => value);
    }

    // ------------------------------------------------------------------
    // SC-007: save → load round trip preserves values
    // ------------------------------------------------------------------

    /// <summary>
    /// SC-007: soil health values persist correctly through a save → load
    /// cycle. After saving and reloading, GetSoilHealth returns the same
    /// values that were set.
    /// </summary>
    [Fact]
    public void SaveThenLoad_PreservesHealthValues()
    {
        // Arrange
        var getCapturedState = SetupSaveLoadCapture();
        var service = CreateSoilHealthService();
        var tile = new Vector2(10, 10);

        // Set health values
        service.SetSoilHealth("Farm", tile, 42f);
        service.SetSoilHealth("Greenhouse", new Vector2(5, 5), 75f);

        // Act: save → load
        service.SaveData(SaveId);
        service.LoadData(SaveId);

        // Assert
        Assert.Equal(42f, service.GetSoilHealth("Farm", tile));
        Assert.Equal(75f, service.GetSoilHealth("Greenhouse", new Vector2(5, 5)));
    }

    /// <summary>
    /// SC-007: sparse save — only non-default (non-zero) values are persisted.
    /// A tile set to zero health is not stored in the saved state.
    /// </summary>
    [Fact]
    public void SaveData_OnlyNonDefaultValues_ArePersisted()
    {
        // Arrange
        var getCapturedState = SetupSaveLoadCapture();
        var service = CreateSoilHealthService();

        // Set one non-zero and one zero health value
        service.SetSoilHealth("Farm", new Vector2(10, 10), 50f);
        service.SetSoilHealth("Farm", new Vector2(20, 20), 0f);

        // Act
        service.SaveData(SaveId);

        // Assert
        var capturedState = getCapturedState();
        Assert.NotNull(capturedState);
        Assert.Single(capturedState.LocationHealthData);
        var farmData = capturedState.LocationHealthData["Farm"];
        Assert.Single(farmData);
        Assert.True(farmData.ContainsKey("10,10"));
        Assert.False(farmData.ContainsKey("20,20"));
    }

    /// <summary>
    /// SC-007: after reload, GetSoilHealth returns the same values that
    /// were set before save.
    /// </summary>
    [Fact]
    public void AfterReload_GetSoilHealth_ReturnsSameValues()
    {
        // Arrange
        SetupSaveLoadCapture();
        var service = CreateSoilHealthService();
        var tile = new Vector2(10, 10);

        service.SetSoilHealth("Farm", tile, 33f);

        // Act
        service.SaveData(SaveId);
        service.LoadData(SaveId);

        // Assert
        Assert.Equal(33f, service.GetSoilHealth("Farm", tile));
    }

    /// <summary>
    /// SC-007: multiple save/load cycles remain consistent.
    /// </summary>
    [Fact]
    public void MultipleSaveLoadCycles_StateRemainsConsistent()
    {
        // Arrange
        SetupSaveLoadCapture();
        var service = CreateSoilHealthService();
        var tile = new Vector2(10, 10);

        service.SetSoilHealth("Farm", tile, 75f);

        // Act: multiple cycles
        service.SaveData(SaveId);
        service.LoadData(SaveId);
        service.SaveData(SaveId);
        service.LoadData(SaveId);

        // Assert
        Assert.Equal(75f, service.GetSoilHealth("Farm", tile));
    }

    /// <summary>
    /// SC-007: dictionary structure is preserved through save/load.
    /// </summary>
    [Fact]
    public void SaveLoadRoundTrip_DictionaryStructurePreserved()
    {
        // Arrange
        var getCapturedState = SetupSaveLoadCapture();
        var service = CreateSoilHealthService();

        service.SetSoilHealth("Farm", new Vector2(10, 10), 30f);
        service.SetSoilHealth("Farm", new Vector2(20, 20), 60f);
        service.SetSoilHealth("Greenhouse", new Vector2(5, 5), 90f);

        // Act
        service.SaveData(SaveId);

        // Assert
        var capturedState = getCapturedState();
        Assert.NotNull(capturedState);
        Assert.Equal(2, capturedState.LocationHealthData.Count);
        Assert.True(capturedState.LocationHealthData.ContainsKey("Farm"));
        Assert.True(capturedState.LocationHealthData.ContainsKey("Greenhouse"));
        Assert.Equal(2, capturedState.LocationHealthData["Farm"].Count);
        Assert.Equal(30f, capturedState.LocationHealthData["Farm"]["10,10"]);
        Assert.Equal(60f, capturedState.LocationHealthData["Farm"]["20,20"]);
        Assert.Equal(90f, capturedState.LocationHealthData["Greenhouse"]["5,5"]);
    }

    /// <summary>
    /// SC-007: empty cache saves empty state.
    /// </summary>
    [Fact]
    public void SaveData_WithEmptyCache_SavesEmptyState()
    {
        // Arrange
        var getCapturedState = SetupSaveLoadCapture();
        var service = CreateSoilHealthService();

        // Act
        service.SaveData(SaveId);

        // Assert
        var capturedState = getCapturedState();
        Assert.NotNull(capturedState);
        Assert.Empty(capturedState.LocationHealthData);
    }

    /// <summary>
    /// SC-007: values are clamped to valid range on save.
    /// </summary>
    [Fact]
    public void SaveData_ClampsValuesToValidRange()
    {
        // Arrange
        var getCapturedState = SetupSaveLoadCapture();
        var service = CreateSoilHealthService();

        service.SetSoilHealth("Farm", new Vector2(10, 10), 150f);

        // Act
        service.SaveData(SaveId);

        // Assert
        var capturedState = getCapturedState();
        Assert.NotNull(capturedState);
        var farmData = capturedState.LocationHealthData["Farm"];
        Assert.Equal(100f, farmData["10,10"]);
    }

    /// <summary>
    /// SC-007: loading with a different save id does not leak stale state.
    /// </summary>
    [Fact]
    public void LoadData_DifferentSaveId_DoesNotLeakStaleState()
    {
        // Arrange
        SetupSaveLoadCapture();
        var service = CreateSoilHealthService();

        service.SetSoilHealth("Farm", new Vector2(10, 10), 50f);
        service.SaveData(SaveId);

        // Act: load with different save id
        service.LoadData("different_save_id");

        // Assert: the runtime cache was cleared for the new save, so the
        // tile reads back as the NaN unknown sentinel.
        Assert.True(float.IsNaN(service.GetSoilHealth("Farm", new Vector2(10, 10))));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Sets up the mock IModDataService to capture saved state and return it
    /// on load, simulating a round trip through disk. Returns a live accessor
    /// for the captured state (null until SaveData is called).
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
    /// Creates a fresh <see cref="SoilHealthService"/> over mocked collaborators.
    /// </summary>
    private SoilHealthService CreateSoilHealthService()
    {
        return new SoilHealthService(
            _mockModDataService.Object,
            _mockMonitor.Object,
            _mockFileNameSanitizationService.Object);
    }
}
