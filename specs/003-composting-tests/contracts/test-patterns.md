# Test Pattern Contracts

**Date**: 2026-09-07
**Feature**: Composting State Machine Tests (`specs/003-composting-tests/`)

## Overview

This document defines the test pattern contracts for each service under test. Each contract specifies the test scenarios, expected behaviors, and verification methods. All time/season/player dependencies use stub implementations of the extracted interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`).

---

## Contract: CompostingBinServiceTests

### FR Coverage: FR-1.1 through FR-1.13

### Test Scenarios

| Test Name | FR | Scenario | Expected Result |
|-----------|----|----------|-----------------|
| `GetBinState_NewBin_ReturnsEmpty` | FR-1.1 | Query state of unknown tile | Returns `Empty` |
| `AddWaste_ValidItem_TransitionsToProcessing` | FR-1.2 | Add valid waste to empty bin | State = `Processing` |
| `ProcessDayStart_AfterTwoDays_TransitionsToReady` | FR-1.3 | Advance 2 days via `TimeProviderStub`, call `ProcessDayStart` | State = `Ready` |
| `CollectCompost_FromReadyBin_ReturnsCompostCount` | FR-1.4 | Collect from ready bin | Returns `MaturationLevel`, state = `Empty` |
| `AddWaste_ToProcessingBin_IsIgnored` | FR-1.5 | Add waste while processing | State unchanged |
| `AddWaste_ToReadyBin_IsIgnored` | FR-1.6 | Add waste while ready | State unchanged |
| `CollectCompost_FromEmptyBin_ReturnsZero` | FR-1.7 | Collect from empty bin | Returns 0 |
| `CollectCompost_FromProcessingBin_ReturnsZero` | FR-1.8 | Collect from processing bin | Returns 0 |
| `ProcessDayStart_AtThreshold_TransitionsCorrectly` | FR-1.9 | Advance exactly 2 days | Transitions at correct time |
| `FullLifecycle_AddProcessReadyCollectAddAgain` | FR-1.10 | Complete cycle twice | Full cycle works |
| `ProcessDayStart_SevenActiveDays_IncrementsMaturation` | FR-1.11 | 7 consecutive active days | `MaturationLevel += 1` |
| `ProcessDayStart_FourteenIdleDays_ResetsMaturation` | FR-1.12 | 14 consecutive idle days | `MaturationLevel = 1` |
| `CollectCompost_OutputCountEqualsMaturationLevel` | FR-1.13 | Collect at maturation level 3 | Returns 3 compost |
| `AddWaste_InvalidItem_IsIgnored` | FR-1.2 | Add invalid waste item | State unchanged |
| `ProcessDayStart_EmptyLocation_IsNoOp` | - | Process empty location | No exception |

### Verification Methods

```csharp
// State verification
Assert.Equal(CompostingBinState.Processing, _service.GetBinState("Farm", tile));

// Return value verification
Assert.Equal(expectedCount, _service.CollectCompost("Farm", tile, player));

// Mock verification
_mockWasteValidator.Verify(x => x.IsValidOrganicWaste(item), Times.Once);

// Time stub usage
var timeStub = new TimeProviderStub { TotalDays = 1 };
// ... advance time ...
timeStub.TotalDays = 3;
```

---

## Contract: CompostApplicationServiceTests

### FR Coverage: FR-2.1 through FR-2.4

### Test Scenarios

| Test Name | FR | Scenario | Expected Result |
|-----------|----|----------|-----------------|
| `TryApplyCompost_ValidTile_IncreasesHealth` | FR-2.1 | Apply compost to valid tile | Returns true, health increases |
| `TryApplyCompost_AtMaxHealth_ReturnsFalse` | FR-2.2 | Apply compost at max health | Returns false |
| `TryApplyCompost_InvalidLocation_ReturnsFalse` | FR-2.3 | Apply to non-farm location | Returns false |
| `TryApplyCompost_NoCompostHeld_ReturnsFalse` | FR-2.4 | Apply with null/non-compost item | Returns false |
| `TryApplyCompost_NonHoeDirtTile_ReturnsFalse` | - | Apply to non-tilled tile | Returns false |
| `TryApplyCompost_HealthCappedAtMax` | FR-2.2 | Apply near max health | Health capped at 100 |

### Verification Methods

```csharp
// Boolean result
Assert.True(result);
Assert.False(result);

// Health update verification
_mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, ModConstants.RestorationAmount));

// No update when rejected
_mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);

// Player stub usage
var playerStub = new PlayerProviderStub { CurrentItem = compostItem };
```

---

## Contract: SoilDecayServiceTests

### FR Coverage: FR-3.1 through FR-3.4

### Test Scenarios

| Test Name | FR | Scenario | Expected Result |
|-----------|----|----------|-----------------|
| `ProcessDayStart_BareTile_DecaysHealth` | FR-3.1 | Process bare tilled tile | Health decreases |
| `ProcessDayStart_LowHealth_DoesNotGoNegative` | FR-3.2 | Process at very low health | Health stays at 0 |
| `ProcessDayStart_AtZeroHealth_StaysAtZero` | FR-3.3 | Process at zero health | Health stays at 0 |
| `ProcessDayStart_SummerDecay_HigherThanSpring` | FR-3.4 | Compare summer vs spring decay | Summer > Spring |
| `ProcessDayStart_Winter_NoDecay` | FR-3.4 | Process in winter | No decay (0x multiplier) |
| `ProcessDayStart_CoveredTile_NoDecay` | - | Process tile with crop | No decay |

### Verification Methods

```csharp
// Decay amount verification
_mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, -decayAmount));

// No decay in winter
_mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);

// Season stub usage
var seasonStub = new SeasonProviderStub { CurrentSeason = "summer" };
```

---

## Contract: OrganicWasteValidatorTests

### FR Coverage: FR-4.1 through FR-4.3

### Test Scenarios

| Test Name | FR | Scenario | Expected Result |
|-----------|----|----------|-----------------|
| `IsValidOrganicWaste_Seeds_ReturnsTrue` | FR-4.1 | Category -74 (Seeds) | Returns true |
| `IsValidOrganicWaste_Vegetables_ReturnsTrue` | FR-4.1 | Category -75 (Vegetables) | Returns true |
| `IsValidOrganicWaste_Fruits_ReturnsTrue` | FR-4.1 | Category -79 (Fruits) | Returns true |
| `IsValidOrganicWaste_Flowers_ReturnsTrue` | FR-4.1 | Category -80 (Flowers) | Returns true |
| `IsValidOrganicWaste_Forage_ReturnsTrue` | FR-4.1 | Category -81 (Forage) | Returns true |
| `IsValidOrganicWaste_Stone_ReturnsFalse` | FR-4.2 | Category -12 (Stone) | Returns false |
| `IsValidOrganicWaste_Wood_ReturnsFalse` | FR-4.2 | Category -14 (Wood) | Returns false |
| `IsValidOrganicWaste_NullItem_ReturnsFalse` | FR-4.3 | Null item | Returns false |
| `IsValidOrganicWaste_CompostableTag_ReturnsTrue` | - | Item with `compostable_item` tag | Returns true |
| `IsValidOrganicWaste_NotCompostableTag_ReturnsFalse` | - | Item with `not_compostable` tag | Returns false |

### Verification Methods

```csharp
Assert.True(result);
Assert.False(result);
```

---

## Contract: SeasonalDecayMultiplierTests

### FR Coverage: FR-5.1 through FR-5.5

### Test Scenarios

| Test Name | FR | Scenario | Expected Result |
|-----------|----|----------|-----------------|
| `GetMultiplier_Spring_ReturnsHalf` | FR-5.1 | "spring" | Returns 0.5f |
| `GetMultiplier_Summer_ReturnsOneAndHalf` | FR-5.2 | "summer" | Returns 1.5f |
| `GetMultiplier_Fall_ReturnsHalf` | FR-5.3 | "fall" | Returns 0.5f |
| `GetMultiplier_Winter_ReturnsZero` | FR-5.4 | "winter" | Returns 0.0f |
| `GetMultiplier_InvalidSeason_ReturnsDefault` | FR-5.5 | "invalid" | Returns 0.5f (default) |

### Verification Methods

```csharp
Assert.Equal(0.5f, _multiplier.GetMultiplier("spring"));
Assert.Equal(1.5f, _multiplier.GetMultiplier("summer"));
```

---

## Contract: SaveIdProviderTests

### FR Coverage: FR-6.1 through FR-6.3

### Test Scenarios

| Test Name | FR | Scenario | Expected Result |
|-----------|----|----------|-----------------|
| `GetSaveId_WithValidSaveFolder_ReturnsId` | FR-6.1 | Valid save folder | Returns save ID |
| `GetSaveId_WithNullSaveFolder_ReturnsNull` | FR-6.2 | Null save folder | Returns null |
| `GetSaveId_WithEmptySaveFolder_ReturnsNull` | FR-6.2 | Empty save folder | Returns null |
| `GetSaveId_WithWhitespaceSaveFolder_ReturnsNull` | FR-6.2 | Whitespace save folder | Returns null |
| `GetSaveId_WithTooLongSaveFolder_ReturnsNull` | - | Save folder > 200 chars | Returns null |
| `GetSaveId_MultipleCalls_ReturnsSameId` | FR-6.3 | Multiple calls | Returns same ID |

### Verification Methods

```csharp
Assert.Equal("test_save_id", result);
Assert.Null(result);
```

---

## Contract: CompostingBinFactoryTests

### FR Coverage: FR-7.1 through FR-7.2

### Test Scenarios

| Test Name | FR | Scenario | Expected Result |
|-----------|----|----------|-----------------|
| `CreateBin_ReturnsEmptyState` | FR-7.1 | Create new bin | State = Empty |
| `CreateBin_HasCorrectDefaults` | FR-7.2 | Create new bin | MaturationLevel = 1, TileX/Y match |
| `CreateBin_DifferentCoordinates` | FR-7.2 | Create at different coords | TileX/Y match input |

### Verification Methods

```csharp
var bin = _factory.CreateBin(10, 20);
Assert.Equal(CompostingBinState.Empty, bin.State);
Assert.Equal(1, bin.MaturationLevel);
Assert.Equal(10, bin.TileX);
Assert.Equal(20, bin.TileY);
```

---

## Test Execution Contract

### Build Verification

```bash
dotnet build Stardew-LivingRoots.sln --configuration Release
```

### Test Filter

```bash
dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal \
  --filter "FullyQualifiedName~Composting|FullyQualifiedName~SoilDecay|FullyQualifiedName~OrganicWaste|FullyQualifiedName~SeasonalDecay|FullyQualifiedName~SaveIdProvider|FullyQualifiedName~CompostingBinFactory"
```

### Expected Output

```
Passed!  - Failed:     0, Passed:    ~44, Skipped:     0, Total:    ~44
```

### Performance Contract

- All tests complete in under 5 seconds (NFR-1)
- Tests do not depend on external systems (NFR-2)
- Tests are deterministic (NFR-3)
- Tests do not use reflection to access private state (NFR-4)
- Tests follow existing project conventions (NFR-5)
