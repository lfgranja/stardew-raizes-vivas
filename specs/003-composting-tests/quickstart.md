# Quickstart: Composting State Machine Tests

**Date**: 2026-09-07
**Feature**: Composting State Machine Tests (`specs/003-composting-tests/`)

## Overview

This guide provides runnable validation scenarios to prove the composting test suite works end-to-end. Run these after implementing the prerequisite fixes and test files. All tests use stub implementations of `ITimeProvider`, `ISeasonProvider`, and `IPlayerProvider` — no direct `Game1` access required.

---

## Prerequisites

- .NET 6 SDK
- Stardew Valley game files (for `GameLocation`, `HoeDirt`, `Item` classes)
- SMAPI installed
- Existing `LivingRoots.Tests` project configured

---

## Quick Validation (5 minutes)

### Step 1: Build the test project

```bash
cd /home/luis/development/stardew-raizes-vivas
dotnet build Stardew-LivingRoots.sln --configuration Release
```

**Expected**: Build succeeds with no errors.

### Step 2: Run all composting tests

```bash
dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal --filter "FullyQualifiedName~Composting|FullyQualifiedName~SoilDecay|FullyQualifiedName~OrganicWaste|FullyQualifiedName~SeasonalDecay|FullyQualifiedName~SaveIdProvider|FullyQualifiedName~CompostingBinFactory"
```

**Expected**: All tests pass. Output shows:
```
Passed!  - Failed:     0, Passed:    ~44, Skipped:     0, Total:    ~44
```

### Step 3: Verify test execution time

```bash
dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal --filter "FullyQualifiedName~Composting|FullyQualifiedName~SoilDecay|FullyQualifiedName~OrganicWaste|FullyQualifiedName~SeasonalDecay|FullyQualifiedName~SaveIdProvider|FullyQualifiedName~CompostingBinFactory" --logger "console;verbosity=detailed"
```

**Expected**: All tests complete in under 5 seconds (NFR-1).

---

## Detailed Validation Scenarios

### Scenario 1: State Machine Lifecycle (FR-1.10)

**Goal**: Verify the full composting lifecycle works.

**Test**: `CompostingBinServiceTests.FullLifecycle_AddProcessReadyCollectAddAgain`

**Steps**:
1. Create `CompostingBinFixture`
2. Set `TimeProviderStub.TotalDays = 1`
3. Call `AddWaste("Farm", (10, 10), validItem)`
4. Verify state is `Processing`
5. Set `TimeProviderStub.TotalDays = 3`
6. Call `ProcessDayStart("Farm")`
7. Verify state is `Ready`
8. Call `CollectCompost("Farm", (10, 10), player)`
9. Verify state is `Empty` and returns 1 compost
10. Call `AddWaste("Farm", (10, 10), validItem)` again
11. Verify state is `Processing` again

**Expected Result**: All assertions pass.

---

### Scenario 2: Time Progression at Threshold (FR-1.9)

**Goal**: Verify the bin transitions at exactly 2 full days.

**Test**: `CompostingBinServiceTests.ProcessDayStart_AfterExactlyTwoDays_TransitionsToReady`

**Steps**:
1. Set `TimeProviderStub.TotalDays = 1`
2. Call `AddWaste("Farm", (10, 10), validItem)`
3. Set `TimeProviderStub.TotalDays = 2` (1 day elapsed)
4. Call `ProcessDayStart("Farm")`
5. Verify state is still `Processing`
6. Set `TimeProviderStub.TotalDays = 3` (2 days elapsed)
7. Call `ProcessDayStart("Farm")`
8. Verify state is `Ready`

**Expected Result**: State transitions at exactly 2 days, not before.

---

### Scenario 3: Maturation Increment (FR-1.11)

**Goal**: Verify maturation level increases after 7 active days.

**Test**: `CompostingBinServiceTests.ProcessDayStart_SevenActiveDays_IncrementsMaturationLevel`

**Steps**:
1. Set `TimeProviderStub.TotalDays = 1`
2. Call `AddWaste("Farm", (10, 10), validItem)`
3. For days 2-8: set `TimeProviderStub.TotalDays` and call `ProcessDayStart("Farm")` each day
4. Verify `MaturationLevel` increased from 1 to 2

**Expected Result**: Maturation level increments after 7 consecutive active days.

---

### Scenario 4: Maturation Reset (FR-1.12)

**Goal**: Verify maturation level resets after 14 idle days.

**Test**: `CompostingBinServiceTests.ProcessDayStart_FourteenIdleDays_ResetsMaturationLevel`

**Steps**:
1. Set maturation level to 3 (via multiple cycles)
2. Set `TimeProviderStub.TotalDays = 1`
3. For days 2-15: set `TimeProviderStub.TotalDays` and call `ProcessDayStart("Farm")` each day (bin stays Empty)
4. Verify `MaturationLevel` reset to 1

**Expected Result**: Maturation level resets to 1 after 14 consecutive idle days.

---

### Scenario 5: Invalid Transitions (FR-1.5, FR-1.6)

**Goal**: Verify adding waste to non-Empty bins is silently ignored.

**Test**: `CompostingBinServiceTests.AddWaste_ToProcessingBin_IsIgnored`

**Steps**:
1. Set `TimeProviderStub.TotalDays = 1`
2. Call `AddWaste("Farm", (10, 10), validItem)` → state is `Processing`
3. Call `AddWaste("Farm", (10, 10), validItem)` again
4. Verify state is still `Processing`

**Expected Result**: Second `AddWaste` call is silently ignored.

---

### Scenario 6: Collect from Empty/Processing (FR-1.7, FR-1.8)

**Goal**: Verify collecting from non-Ready bins returns 0.

**Test**: `CompostingBinServiceTests.CollectCompost_FromEmptyBin_ReturnsZero`

**Steps**:
1. Call `CollectCompost("Farm", (10, 10), player)` on empty bin
2. Verify returns 0

**Expected Result**: Returns 0, no state change.

---

### Scenario 7: Seasonal Decay Variation (FR-3.4)

**Goal**: Verify decay rate changes by season.

**Test**: `SoilDecayServiceTests.ProcessDayStart_SummerDecay_HigherThanSpring`

**Steps**:
1. Create `GameLocationFixture("Maps\\Farm", "Farm")` with bare `HoeDirt` tiles
2. Set `SeasonProviderStub.CurrentSeason = "spring"`
3. Call `ProcessDayStart("Farm")`
4. Record health lost
5. Reset health
6. Set `SeasonProviderStub.CurrentSeason = "summer"`
7. Call `ProcessDayStart("Farm")`
8. Record health lost
9. Verify summer decay > spring decay

**Expected Result**: Summer (1.5x) decay is higher than spring (0.5x).

---

### Scenario 8: Soil Health Bounds (FR-3.2, FR-3.3)

**Goal**: Verify health never goes below 0.

**Test**: `SoilDecayServiceTests.ProcessDayStart_LowHealth_DoesNotGoNegative`

**Steps**:
1. Set soil health to very low value (e.g., 1.0)
2. Call `ProcessDayStart("Farm")`
3. Verify health is 0 (not negative)

**Expected Result**: Health clamped at 0.

---

### Scenario 9: Waste Validation (FR-4.1, FR-4.2)

**Goal**: Verify valid/invalid items are correctly categorized.

**Test**: `OrganicWasteValidatorTests.IsValidOrganicWaste_ValidItems_ReturnTrue`

**Steps**:
1. Create item with `Category = -74` (Seeds)
2. Call `IsValidOrganicWaste(item)`
3. Verify returns true
4. Create item with `Category = -999` (Invalid)
5. Call `IsValidOrganicWaste(item)`
6. Verify returns false

**Expected Result**: Valid categories accepted, invalid rejected.

---

### Scenario 10: Save ID Provider (FR-6.1, FR-6.2)

**Goal**: Verify save ID retrieval and null handling.

**Test**: `SaveIdProviderTests.GetSaveId_WithValidSaveFolder_ReturnsId`

**Steps**:
1. Set `Constants.SaveFolderName = "test_save"` via reflection
2. Create `SaveIdProvider`
3. Call `GetSaveId()`
4. Verify returns "test_save"
5. Set `Constants.SaveFolderName = null`
6. Call `GetSaveId()`
7. Verify returns null

**Expected Result**: Returns valid ID when available, null when not.

---

## Test Execution Summary

| Test File | Scenarios Covered | Expected Pass |
|-----------|-------------------|---------------|
| `CompostingBinServiceTests` | 1-6 | 15 tests |
| `CompostApplicationServiceTests` | - | 6 tests |
| `SoilDecayServiceTests` | 7-8 | 6 tests |
| `OrganicWasteValidatorTests` | 9 | 5 tests |
| `SeasonalDecayMultiplierTests` | - | 5 tests |
| `SaveIdProviderTests` | 10 | 4 tests |
| `CompostingBinFactoryTests` | - | 3 tests |
| **Total** | | **~44 tests** |

---

## Troubleshooting

### TimeProviderStub not advancing time

**Symptom**: State never transitions to Ready.

**Solution**: Ensure `TimeProviderStub.TotalDays` is set BEFORE calling `ProcessDayStart()`. The stub value is read at the time of the call.

### SeasonProviderStub not changing decay

**Symptom**: Decay amount same across seasons.

**Solution**: Ensure `SeasonProviderStub.CurrentSeason` is set BEFORE calling `ProcessDayStart()`. Verify the season string matches expected values ("spring", "summer", "fall", "winter").

### Constants.SaveFolderName not settable

**Symptom**: `FieldInfo.SetValue` throws `FieldAccessException`.

**Solution**: Use `BindingFlags.Public | BindingFlags.Static` and ensure the field is not readonly. If readonly, use `GetField` with appropriate flags.

### GameLocation constructor throws

**Symptom**: `ArgumentException` when creating `GameLocation`.

**Solution**: Use the correct constructor signature `GameLocation(string mapPath, string name)` — first parameter is the map file path (e.g., `"Maps\\Farm"`), not the display name.

### ThreadSafeGameLoopEventsStub not found

**Symptom**: `TypeNotFoundException` for `ThreadSafeGameLoopEventsStub`.

**Solution**: Verify the file exists at `LivingRoots.Tests/ThreadSafeGameLoopEventsStub.cs`. If missing, create it from the existing test infrastructure.

---

## Next Steps

1. Review the test files generated by `/speckit-tasks`
2. Implement prerequisite fixes (Fix-1 through Fix-5)
3. Create test files following the patterns in this guide
4. Run the quickstart validation scenarios
5. Verify all tests pass
