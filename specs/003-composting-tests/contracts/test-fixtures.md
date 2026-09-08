# Test Fixture Contracts

**Date**: 2026-09-07
**Feature**: Composting State Machine Tests (`specs/003-composting-tests/`)

## Overview

This document defines the test fixture contracts and helper patterns for the composting test suite. These contracts ensure consistency across test files and provide reusable patterns for common testing scenarios. All time/season/player dependencies use stub implementations of the extracted interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`).

---

## Contract 1: TimeProviderStub

### Purpose
Stub implementation of `ITimeProvider` for time-dependent tests. Replaces direct `Game1.Date.TotalDays` access.

### Interface

```csharp
public class TimeProviderStub : ITimeProvider
{
    public int TotalDays { get; set; } = 1;
}
```

### Behavior
- **TotalDays**: Settable property to control the current day in tests
- No dependencies on `Game1` or any game state

### Usage

```csharp
[Fact]
public void ProcessDayStart_AfterTwoDays_TransitionsToReady()
{
    var timeStub = new TimeProviderStub { TotalDays = 1 };
    var fixture = new CompostingBinFixture(timeStub);
    fixture.Service.AddWaste("Farm", new Vector2(10, 10), validItem);
    
    timeStub.TotalDays = 3;
    fixture.Service.ProcessDayStart("Farm");
    
    Assert.Equal(CompostingBinState.Ready, 
        fixture.Service.GetBinState("Farm", new Vector2(10, 10)));
}
```

### Guarantees
- No `Game1` dependency — works in pure unit test context
- Thread-safe for single-threaded test execution
- Deterministic — value only changes when test sets it

---

## Contract 2: SeasonProviderStub

### Purpose
Stub implementation of `ISeasonProvider` for season-dependent tests. Replaces direct `Game1.currentSeason` access.

### Interface

```csharp
public class SeasonProviderStub : ISeasonProvider
{
    public string CurrentSeason { get; set; } = "spring";
}
```

### Behavior
- **CurrentSeason**: Settable property to control the current season in tests
- No dependencies on `Game1` or any game state

### Usage

```csharp
[Fact]
public void ProcessDayStart_SummerDecay_HigherThanSpring()
{
    var springStub = new SeasonProviderStub { CurrentSeason = "spring" };
    var summerStub = new SeasonProviderStub { CurrentSeason = "summer" };
    // ... test both and compare decay amounts
}
```

### Guarantees
- No `Game1` dependency — works in pure unit test context
- Deterministic — value only changes when test sets it

---

## Contract 3: PlayerProviderStub

### Purpose
Stub implementation of `IPlayerProvider` for player-dependent tests. Replaces direct `Game1.player` access.

### Interface

```csharp
public class PlayerProviderStub : IPlayerProvider
{
    public Farmer CurrentPlayer { get; set; } = null!;
    public Item? CurrentItem { get; set; }
}
```

### Behavior
- **CurrentPlayer**: Settable property to control the current player in tests
- **CurrentItem**: Settable property to control the player's held item
- No dependencies on `Game1` or any game state

### Usage

```csharp
[Fact]
public void TryApplyCompost_NoCompostHeld_ReturnsFalse()
{
    var playerStub = new PlayerProviderStub { CurrentItem = null };
    var fixture = new CompostApplicationServiceFixture(playerStub);
    
    var result = fixture.Service.TryApplyCompost(location, new Vector2(10, 10));
    
    Assert.False(result);
}
```

### Guarantees
- No `Game1` dependency — works in pure unit test context
- No player disposal semantics issues (unlike `Game1.player` setter)

---

## Contract 4: GameLocationFixture

### Purpose
Create real `GameLocation` instances with `HoeDirt` tiles for location-iteration tests.

### Interface

```csharp
public class GameLocationFixture
{
    public GameLocation Location { get; }
    public List<Vector2> TilledTiles { get; }
    
    public GameLocationFixture(string mapPath = "Maps\\Farm", string name = "Farm");
    public void AddHoeDirtTile(int x, int y, bool bare = true);
}
```

### Behavior
- **Constructor**: Creates new `GameLocation` with given map path and name (correct signature: `GameLocation(string mapPath, string name)`)
- **AddHoeDirtTile**: Adds a `HoeDirt` tile at specified coordinates
  - `bare = true`: Tile has no crop (default)
  - `bare = false`: Tile has a crop (for non-bare tests)

### Usage

```csharp
[Fact]
public void ProcessDayStart_BareTiles_DecaysHealth()
{
    var fixture = new GameLocationFixture("Maps\\Farm", "Farm");
    fixture.AddHoeDirtTile(10, 10, bare: true);
    fixture.AddHoeDirtTile(11, 10, bare: true);
    
    // Use fixture.Location in test...
}
```

### Guarantees
- Location is fully initialized with terrain features
- Tilled tiles list tracks all added tiles for cleanup
- Compatible with `SoilDecayService` location iteration
- Uses correct constructor signature `GameLocation(string mapPath, string name)`

---

## Contract 5: CompostingBinFixture

### Purpose
Create `CompostingBinService` with mock dependencies for state machine tests.

### Interface

```csharp
public class CompostingBinFixture
{
    public Mock<IModDataService> MockModDataService { get; }
    public Mock<ISaveIdProvider> MockSaveIdProvider { get; }
    public Mock<IOrganicWasteValidator> MockWasteValidator { get; }
    public Mock<IMonitor> MockMonitor { get; }
    public TimeProviderStub TimeStub { get; }
    public CompostingBinFactory Factory { get; }
    public CompostingBinService Service { get; }
    
    public CompostingBinFixture(TimeProviderStub? timeStub = null);
}
```

### Behavior
- **Constructor**: Creates all mock objects and injects into `CompostingBinService`
- All mocks use default behavior (no setup required for basic tests)
- `TimeStub` is settable for time-dependent tests
- `Factory` is the real `CompostingBinFactory` (not mocked)
- Service is ready to use immediately

### Usage

```csharp
[Fact]
public void AddWaste_ValidItem_TransitionsToProcessing()
{
    var fixture = new CompostingBinFixture();
    var validItem = new Item { QualifiedItemId = "Object.Sap" };
    fixture.MockWasteValidator.Setup(x => x.IsValidOrganicWaste(validItem)).Returns(true);
    
    fixture.Service.AddWaste("Farm", new Vector2(10, 10), validItem);
    
    Assert.Equal(CompostingBinState.Processing, 
        fixture.Service.GetBinState("Farm", new Vector2(10, 10)));
}
```

### Guarantees
- All dependencies are mocked or stubbed
- Service is isolated from external systems
- Mocks are accessible for verification
- TimeStub is accessible for time control

---

## Contract 6: CompostApplicationServiceFixture

### Purpose
Create `CompostApplicationService` with mock dependencies for compost application tests.

### Interface

```csharp
public class CompostApplicationServiceFixture
{
    public Mock<ISoilHealthService> MockSoilHealthService { get; }
    public Mock<IMonitor> MockMonitor { get; }
    public PlayerProviderStub PlayerStub { get; }
    public CompostApplicationService Service { get; }
    
    public CompostApplicationServiceFixture(PlayerProviderStub? playerStub = null);
}
```

### Usage

```csharp
[Fact]
public void TryApplyCompost_ValidTile_IncreasesHealth()
{
    var fixture = new CompostApplicationServiceFixture();
    fixture.MockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", new Vector2(10, 10))).Returns(50f);
    
    var location = new GameLocation("Maps\\Farm", "Farm");
    var result = fixture.Service.TryApplyCompost(location, new Vector2(10, 10));
    
    Assert.True(result);
    fixture.MockSoilHealthService.Verify(x => x.UpdateHealth("Farm", new Vector2(10, 10), ModConstants.RestorationAmount));
}
```

---

## Contract 7: SoilDecayServiceFixture

### Purpose
Create `SoilDecayService` with mock dependencies for decay calculation tests.

### Interface

```csharp
public class SoilDecayServiceFixture
{
    public Mock<ISoilHealthService> MockSoilHealthService { get; }
    public Mock<IMonitor> MockMonitor { get; }
    public SeasonProviderStub SeasonStub { get; }
    public SeasonalDecayMultiplier SeasonalMultiplier { get; }
    public SoilDecayService Service { get; }
    
    public SoilDecayServiceFixture(SeasonProviderStub? seasonStub = null);
}
```

### Usage

```csharp
[Fact]
public void ProcessDayStart_BareTile_DecaysByDailyRate()
{
    var fixture = new SoilDecayServiceFixture();
    var location = new GameLocation("Maps\\Farm", "Farm");
    var tile = new Vector2(10, 10);
    var hoeDirt = new HoeDirt(0, location);
    hoeDirt.crop = null;
    location.terrainFeatures.Add(tile, hoeDirt);
    
    fixture.MockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);
    fixture.SeasonStub.CurrentSeason = "spring";
    
    fixture.Service.ProcessDayStart("Farm");
    
    fixture.MockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, -1f)); // 2f * 0.5f = 1f
}
```

---

## Contract 8: SaveIdProviderFixture

### Purpose
Create `SaveIdProvider` with controlled `Constants.SaveFolderName` values.

### Interface

```csharp
public class SaveIdProviderFixture : IDisposable
{
    public SaveIdProvider Provider { get; }
    
    public SaveIdProviderFixture(string? saveFolderName);
    public void Dispose();
}
```

### Behavior
- **Constructor**: Sets `Constants.SaveFolderName` via reflection
- **Dispose()**: Restores original value

### Usage

```csharp
[Fact]
public void GetSaveId_WithValidSaveFolder_ReturnsId()
{
    using var fixture = new SaveIdProviderFixture("test_save_id");
    
    var result = fixture.Provider.GetSaveId();
    
    Assert.Equal("test_save_id", result);
}

[Fact]
public void GetSaveId_WithNullSaveFolder_ReturnsNull()
{
    using var fixture = new SaveIdProviderFixture(null);
    
    var result = fixture.Provider.GetSaveId();
    
    Assert.Null(result);
}
```

---

## Contract 9: ThreadSafeGameLoopEventsStub

### Purpose
Existing stub for testing async event handling. Already available in `LivingRoots.Tests`.

### Interface (Existing)

```csharp
public class ThreadSafeGameLoopEventsStub : IGameLoopEvents
{
    public int GameLaunchedAddCount { get; }
    public int GameLaunchedRemoveCount { get; }
    public int SaveLoadedAddCount { get; }
    public int SaveLoadedRemoveCount { get; }
    public int SavingAddCount { get; }
    public int SavingRemoveCount { get; }
    
    // Event implementations...
}
```

### Usage

```csharp
[Fact]
public void RegisterEvents_SubscribesToDayStarted()
{
    var stub = new ThreadSafeGameLoopEventsStub();
    // ... setup controller with stub ...
    
    controller.RegisterEvents();
    
    Assert.Equal(1, stub.DayStartedAddCount);
}
```

---

## Contract 10: ItemFactory

### Purpose
Create test items with specific properties for waste validation tests.

### Interface

```csharp
public static class ItemFactory
{
    public static Item CreateItem(string qualifiedItemId, int category = 0);
    public static Item CreateValidWasteItem();  // Category = -74 (Seeds)
    public static Item CreateInvalidWasteItem(); // Category = -999
    public static Item CreateNullItem();         // Returns null!
}
```

### Usage

```csharp
[Fact]
public void IsValidOrganicWaste_ValidItem_ReturnsTrue()
{
    var item = ItemFactory.CreateValidWasteItem();
    var validator = new OrganicWasteValidator(_mockMonitor.Object);
    
    var result = validator.IsValidOrganicWaste(item);
    
    Assert.True(result);
}
```

---

## Test File Conventions

### File Naming
- One test file per subject: `{Subject}Tests.cs`
- Placed in `LivingRoots.Tests/` root directory

### Class Naming
- Test class name matches file name: `CompostingBinServiceTests`
- Test namespace: `LivingRoots.Tests`

### Method Naming
- Pattern: `{Method}_{State}_{ExpectedResult}`
- Example: `AddWaste_ValidItem_TransitionsToProcessing`

### Test Structure
1. **Arrange**: Set up fixtures, mocks, and initial state
2. **Act**: Call the method under test
3. **Assert**: Verify expected outcome

### Required Using Statements

```csharp
using Xunit;
using Moq;
using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Domain.Services;
using StardewValley;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;
```

---

## Dependency Injection Pattern

All test fixtures follow constructor injection:

```csharp
public class ServiceTests
{
    private readonly Mock<IDependency> _mockDep;
    private readonly ServiceUnderTest _service;
    
    public ServiceTests()
    {
        _mockDep = new Mock<IDependency>();
        _service = new ServiceUnderTest(_mockDep.Object);
    }
}
```

---

## Mock Verification Pattern

```csharp
// Verify method was called
_mockDep.Verify(x => x.Method(), Times.Once);

// Verify method was never called
_mockDep.Verify(x => x.Method(), Times.Never);

// Verify method was called with specific arguments
_mockDep.Verify(x => x.Method(expectedArg), Times.Once);

// Verify property getter was accessed
_mockDep.VerifyGet(x => x.Property, Times.Once);

// Verify property setter was accessed
_mockDep.VerifySet(x => x.Property = expectedValue, Times.Once);
```

---

## Exception Testing Pattern

```csharp
[Fact]
public void Constructor_NullDependency_ThrowsArgumentNullException()
{
    Assert.Throws<ArgumentNullException>(() => new ServiceUnderTest(null!));
}

[Fact]
public void Method_InvalidArgument_ThrowsArgumentException()
{
    var ex = Assert.Throws<ArgumentException>(() => _service.Method(invalidArg));
    Assert.Contains("expected message", ex.Message);
}
```

---

## Async Testing Pattern

```csharp
[Fact]
public async Task MethodAsync_ReturnsExpectedResult()
{
    // Arrange
    _mockDep.Setup(x => x.MethodAsync()).ReturnsAsync(expectedResult);
    
    // Act
    var result = await _service.MethodAsync();
    
    // Assert
    Assert.Equal(expectedResult, result);
}
```
