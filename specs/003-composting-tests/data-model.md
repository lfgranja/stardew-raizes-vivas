# Data Model: Composting State Machine Tests

**Date**: 2026-09-07
**Feature**: Composting State Machine Tests (`specs/003-composting-tests/`)

## Overview

This document defines the data entities, state transitions, and validation rules for the composting system. These models are used by the test suite to verify correct behavior. It also defines the new testable interfaces extracted from `Game1` static dependencies.

---

## Entities

### CompostingBinStateModel

The runtime state of a single composting bin.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `TileX` | `int` | - | X coordinate of the bin's tile |
| `TileY` | `int` | - | Y coordinate of the bin's tile |
| `State` | `CompostingBinState` | `Empty` | Current state (Empty, Processing, Ready) |
| `InputItemId` | `string?` | `null` | QualifiedItemId of the waste item added |
| `InputTimestamp` | `int?` | `null` | Day when waste was added (`ITimeProvider.TotalDays`) — changed from `long?` to `int?` |
| `MaturationLevel` | `int` | `1` | Current maturation level (1-5) |
| `ConsecutiveIdleDays` | `int` | `0` | Days spent in Empty state |
| `ConsecutiveActiveDays` | `int` | `0` | Days spent in Processing state |

**Validation Rules**:
- `MaturationLevel` must be between 1 and `MaturationMaxLevel` (5)
- `ConsecutiveIdleDays` must be between 0 and `MaturationIdleResetDays` (14)
- `InputTimestamp` is required when `State == Processing`

---

### CompostingBinState (Enum)

```csharp
public enum CompostingBinState
{
    Empty = 0,
    Processing = 1,
    Ready = 2
}
```

---

### CompostingBinData

The persistence DTO for composting bin state. **Updated to include `ConsecutiveActiveDays`** (Fix-3).

| Field | Type | Description |
|-------|------|-------------|
| `LocationBinData` | `Dictionary<string, Dictionary<string, CompostingBinStateData>>` | Bins keyed by location name, then tile key |

---

### CompostingBinStateData

The persistence DTO for a single bin. **Updated to include `ConsecutiveActiveDays`** (Fix-3).

| Field | Type | Description |
|-------|------|-------------|
| `State` | `string` | State name (Empty, Processing, Ready) |
| `InputItemId` | `string?` | QualifiedItemId of waste item |
| `InputTimestamp` | `int?` | Day when waste was added (changed from `long?` to `int?`) |
| `MaturationLevel` | `int` | Current maturation level |
| `ConsecutiveIdleDays` | `int` | Days spent idle |
| `ConsecutiveActiveDays` | `int` | Days spent active (FIX-3: added for maturation persistence) |

---

### SoilHealthState

The persistence DTO for soil health.

| Field | Type | Description |
|-------|------|-------------|
| `HealthData` | `Dictionary<LocationName, Dictionary<TileKey, HealthValue>>` | Health values by location and tile |

---

## New Testable Interfaces (Fix-4)

### ITimeProvider

Extracted from `Game1.Date.TotalDays` for time-dependent tests.

```csharp
// Location: LivingRoots/Domain/Interfaces/ITimeProvider.cs
public interface ITimeProvider
{
    int TotalDays { get; }
}
```

**Production implementation**: `TimeProvider` in `LivingRoots/Services/` wraps `Game1.Date.TotalDays`.
**Test implementation**: `TimeProviderStub` with settable `TotalDays` property.

---

### ISeasonProvider

Extracted from `Game1.currentSeason` for season-dependent tests.

```csharp
// Location: LivingRoots/Domain/Interfaces/ISeasonProvider.cs
public interface ISeasonProvider
{
    string CurrentSeason { get; }
}
```

**Production implementation**: `SeasonProvider` in `LivingRoots/Services/` wraps `Game1.currentSeason`.
**Test implementation**: `SeasonProviderStub` with settable `CurrentSeason` property.

---

### IPlayerProvider

Extracted from `Game1.player` for player-dependent tests.

```csharp
// Location: LivingRoots/Domain/Interfaces/IPlayerProvider.cs
public interface IPlayerProvider
{
    Farmer CurrentPlayer { get; }
    Item? CurrentItem { get; }
}
```

**Production implementation**: `PlayerProvider` in `LivingRoots/Services/` wraps `Game1.player`.
**Test implementation**: `PlayerProviderStub` with settable `CurrentItem` property.

---

## State Transitions

### Composting Bin State Machine

```
┌─────────┐    Add valid waste     ┌────────────┐    2 full days elapsed    ┌─────────┐
│  Empty  │ ─────────────────────► │ Processing │ ────────────────────────► │  Ready  │
└─────────┘                        └────────────┘                           └─────────┘
     ▲                                                                              │
     │                                                                              │
     │                                 Collect compost                              │
     └──────────────────────────────────────────────────────────────────────────────┘
```

### Transition Rules

| From | To | Condition | Action |
|------|----|-----------|--------|
| Empty | Processing | `AddWaste()` called with valid item | Set `InputTimestamp = ITimeProvider.TotalDays`, increment idle days reset |
| Processing | Ready | `ITimeProvider.TotalDays - InputTimestamp >= MaturationDays (2)` | Play sound, log transition |
| Ready | Empty | `CollectCompost()` called | Produce `MaturationLevel` compost items, reset state |
| Empty | Empty | `AddWaste()` called with invalid item | No state change (silently ignored) |
| Processing | Processing | `AddWaste()` called | No state change (silently ignored) |
| Ready | Ready | `AddWaste()` called | No state change (silently ignored) |

### Maturation Rules

| Condition | Effect |
|-----------|--------|
| 7 consecutive active days (Processing) | `MaturationLevel += 1` (capped at 5) |
| 14 consecutive idle days (Empty) | `MaturationLevel = 1` |
| Collect compost | Output count = `MaturationLevel` |

---

## Test Fixtures

### TimeProviderStub

Stub implementation of `ITimeProvider` for time-dependent tests.

```csharp
public class TimeProviderStub : ITimeProvider
{
    public int TotalDays { get; set; } = 1;
}
```

### SeasonProviderStub

Stub implementation of `ISeasonProvider` for season-dependent tests.

```csharp
public class SeasonProviderStub : ISeasonProvider
{
    public string CurrentSeason { get; set; } = "spring";
}
```

### PlayerProviderStub

Stub implementation of `IPlayerProvider` for player-dependent tests.

```csharp
public class PlayerProviderStub : IPlayerProvider
{
    public Farmer CurrentPlayer { get; set; } = null!;
    public Item? CurrentItem { get; set; }
}
```

### GameLocationFixture

Helper class to create real `GameLocation` instances with `HoeDirt` tiles.

```csharp
public class GameLocationFixture
{
    public GameLocation Location { get; }
    public List<Vector2> TilledTiles { get; } = new();
    
    public GameLocationFixture(string mapPath = "Maps\\Farm", string name = "Farm")
    {
        Location = new GameLocation(mapPath, name);
    }
    
    public void AddHoeDirtTile(int x, int y, bool bare = true)
    {
        var tile = new Vector2(x, y);
        var hoeDirt = new HoeDirt(0, Location);
        if (!bare) hoeDirt.crop = new Crop(0, x, y);
        Location.terrainFeatures.Add(tile, hoeDirt);
        TilledTiles.Add(tile);
    }
}
```

### CompostingBinFixture

Helper class to create `CompostingBinService` with mock dependencies.

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
    
    public CompostingBinFixture()
    {
        MockModDataService = new Mock<IModDataService>();
        MockSaveIdProvider = new Mock<ISaveIdProvider>();
        MockWasteValidator = new Mock<IOrganicWasteValidator>();
        MockMonitor = new Mock<IMonitor>();
        TimeStub = new TimeProviderStub();
        Factory = new CompostingBinFactory();
        
        Service = new CompostingBinService(
            MockModDataService.Object,
            MockSaveIdProvider.Object,
            MockWasteValidator.Object,
            MockMonitor.Object,
            TimeStub,
            Factory);
    }
}
```

---

## Validation Rules for Tests

### FR-1: CompostingBinService State Machine

| ID | Rule | Test Verification |
|----|------|-------------------|
| FR-1.1 | New bin starts Empty | `GetBinState` returns `Empty` for unknown tile |
| FR-1.2 | Add valid waste → Processing | State changes to `Processing` |
| FR-1.3 | 2 days elapsed → Ready | State changes to `Ready` after `ProcessDayStart` |
| FR-1.4 | Collect → Empty | State returns to `Empty`, returns compost count |
| FR-1.5 | Add to Processing → ignored | State remains `Processing` |
| FR-1.6 | Add to Ready → ignored | State remains `Ready` |
| FR-1.7 | Collect from Empty → 0 | Returns 0, state unchanged |
| FR-1.8 | Collect from Processing → 0 | Returns 0, state unchanged |
| FR-1.9 | Time progression at threshold | Transitions at exactly 2 days |
| FR-1.10 | Full lifecycle | Add → Process → Ready → Collect → Add again |
| FR-1.11 | Maturation increment | Level increases after 7 active days |
| FR-1.12 | Maturation reset | Level resets after 14 idle days |
| FR-1.13 | Output equals maturation level | Collect returns `MaturationLevel` items |

### FR-2: CompostApplicationService

| ID | Rule | Test Verification |
|----|------|-------------------|
| FR-2.1 | Apply compost increases health | Health increases by `RestorationAmount` |
| FR-2.2 | Health capped at max | Health does not exceed `MaxSoilHealth` |
| FR-2.3 | Invalid location returns false | Non-farm, non-Greenhouse returns false |
| FR-2.4 | No compost held returns false | `CurrentItem` is null or non-compost |

### FR-3: SoilDecayService

| ID | Rule | Test Verification |
|----|------|-------------------|
| FR-3.1 | Health decreases over time | Health decreases by `DailyDecayRate × multiplier` |
| FR-3.2 | Health floored at min | Health does not go below `MinSoilHealth` |
| FR-3.3 | Health never negative | Health stays at 0 minimum |
| FR-3.4 | Seasonal variation | Different seasons apply different multipliers |

### FR-4: OrganicWasteValidator

| ID | Rule | Test Verification |
|----|------|-------------------|
| FR-4.1 | Valid items accepted | Categories -74, -75, -79, -80, -81 return true |
| FR-4.2 | Invalid items rejected | Other categories return false |
| FR-4.3 | Null input returns false | `null` item returns false |

### FR-5: SeasonalDecayMultiplier

| ID | Rule | Test Verification |
|----|------|-------------------|
| FR-5.1 | Spring multiplier | Returns 0.5f |
| FR-5.2 | Summer multiplier | Returns 1.5f |
| FR-5.3 | Fall multiplier | Returns 0.5f |
| FR-5.4 | Winter multiplier | Returns 0.0f |
| FR-5.5 | Invalid season | Returns 0.5f (default) |

### FR-6: SaveIdProvider

| ID | Rule | Test Verification |
|----|------|-------------------|
| FR-6.1 | Valid save ID | Returns non-null, non-empty string |
| FR-6.2 | Null save folder | Returns null |
| FR-6.3 | Consistency | Returns same ID across multiple calls |

### FR-7: CompostingBinFactory

| ID | Rule | Test Verification |
|----|------|-------------------|
| FR-7.1 | Creates bin in Empty state | `State == Empty` |
| FR-7.2 | Correct defaults | `MaturationLevel == 1`, tile coordinates match |

---

## Constants Reference

| Constant | Value | Used By |
|----------|-------|---------|
| `MaturationDays` | 2 | CompostingBinService (replaces `ProcessingDurationMinutes = 2880` after Fix-1) |
| `MaturationMaxLevel` | 5 | CompostingBinService |
| `MaturationIdleResetDays` | 14 | CompostingBinService |
| `MaturationIncrementDays` | 7 | CompostingBinService |
| `CompostItemId` | "LivingRoots.Compost" | CompostApplicationService |
| `DailyDecayRate` | 2f | SoilDecayService |
| `RestorationAmount` | 15f | CompostApplicationService |
| `MaxSoilHealth` | 100f | CompostApplicationService, SoilDecayService |
| `MinSoilHealth` | 0f | SoilDecayService |
| `MaxSaveIdLength` | 200 | SaveIdProvider |
