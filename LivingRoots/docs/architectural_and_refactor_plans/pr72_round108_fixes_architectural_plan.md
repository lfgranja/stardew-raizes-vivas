# PR #72 Round 108 Code Review Fixes Architectural Plan

## Overview
This document outlines the plan to address the issues identified in Round 108 of the code review for PR #72. The fixes cover high, medium, and low severity issues, focusing on data integrity, thread safety, and test reliability.

## 1. HIGH SEVERITY: Fix Data Loss Risk in LoadData

### Problem Analysis
In `SoilHealthService.LoadData`, when the `MaxTilesPerLocation` limit is exceeded, the current implementation logs a warning and stops processing tiles for that location. However, it continues to load the partial data for that location and proceeds to other locations. This silent truncation results in data loss for the user, as the saved state will be incomplete.

### Proposed Solution
Modify `SoilHealthService.LoadData` to abort the entire load operation if any per-location tile limit is exceeded. The runtime cache should be cleared to prevent serving partial or corrupted data.

### Implementation Strategy
1.  In `LoadData`, inside the tile processing loop:
    *   If `tileCount > ModConstants.MaxTilesPerLocation`:
        *   Log a **Critical** error.
        *   Set a failure flag.
        *   Break out of the inner loop (location processing).
2.  Check the failure flag in the outer loop:
    *   If set, break out of the outer loop.
3.  After the loops:
    *   If the failure flag is set, clear `_runtimeCache` (wrapped in a lock).
    *   Return immediately.

### Code Example (Conceptual)
```csharp
bool loadFailed = false;

foreach (var locationEntry in locations)
{
    // ... existing code ...

    foreach (var tileEntry in locationEntry.Value)
    {
        tileCount++;
        if (tileCount > ModConstants.MaxTilesPerLocation)
        {
             _monitor.Log($"Tile count limit ({ModConstants.MaxTilesPerLocation}) exceeded for location '{truncatedLocationName}'. Aborting load to prevent data loss.", LogLevel.Alert);
             loadFailed = true;
             break;
        }
        // ...
    }

    if (loadFailed) break;
}

if (loadFailed)
{
    lock (_lock)
    {
        _runtimeCache.Clear();
    }
    return;
}
```

### Risk Assessment
*   **Risk**: Users with legitimately massive saves (exceeding 500 tiles/location) will lose access to their soil health data.
*   **Mitigation**: The limit is a security/DoS protection measure. 500 tiles per location is a generous limit for this specific mod data. The alternative (partial load) is worse as it corrupts state.

## 2. MEDIUM SEVERITY: Implement Rollback Mechanism in UnregisterEvents

### Problem Analysis
The `ModController.UnregisterEvents` method attempts to unsubscribe from multiple events (`GameLaunched`, `SaveLoaded`, `Saving`). If one unsubscription fails (throws an exception), the method continues, but the state might be inconsistent. Specifically, if we partially unsubscribe, we might leave the controller in a state where it thinks it's unregistered but some handlers are still active, or vice versa.

### Proposed Solution
Implement an all-or-nothing rollback mechanism. If any unsubscription fails, attempt to re-subscribe any handlers that were successfully removed in this pass, to restore the previous state (consistent with "registered").

### Implementation Strategy
1.  Track success of each unsubscription.
2.  If any unsubscription fails:
    *   Log the error.
    *   Attempt to re-subscribe the handlers that were successfully removed.
    *   Restore the `EventsRegisteredFlag`.
    *   Log a warning that unregistration failed and state was restored.

### Code Example (Conceptual)
```csharp
bool gameLaunchedRemoved = SafeUnsubscribe(...);
bool saveLoadedRemoved = SafeUnsubscribe(...);
bool savingRemoved = SafeUnsubscribe(...);

if (!gameLaunchedRemoved || !saveLoadedRemoved || !savingRemoved)
{
    // Rollback
    if (gameLaunchedRemoved) gameLoop.GameLaunched += gameLaunchedHandler;
    if (saveLoadedRemoved) gameLoop.SaveLoaded += saveLoadedHandler;
    if (savingRemoved) gameLoop.Saving += savingHandler;

    // Restore flag
    Interlocked.Or(ref _state, EventsRegisteredFlag);
    return;
}
```

### Risk Assessment
*   **Risk**: Re-subscription might also fail (though unlikely if unsubscription just succeeded).
*   **Mitigation**: Wrap re-subscription in try-catch blocks to ensure we don't crash during rollback.

## 3. MEDIUM SEVERITY: Refactor DoS Protection Test

### Problem Analysis
The test `SoilHealthServiceTests.LoadData_DosProtectionCountsProcessedEntriesNotJustSaved` asserts that specific entries are not loaded. This assumes a deterministic order of dictionary processing, which is not guaranteed in .NET. This makes the test flaky.

### Proposed Solution
Instead of checking for *specific* unloaded entries, assert on the *total count* of loaded entries. The test should verify that the number of loaded tiles equals the `MaxTilesPerLocation` limit (or 0 if we implement the High Severity fix above).

*Note: Since we are implementing the High Severity fix (abort on limit), this test will actually need to verify that **ZERO** entries are loaded when the limit is exceeded.*

### Implementation Strategy
1.  Update `LoadData_DosProtectionCountsProcessedEntriesNotJustSaved` to assert that `GetSoilHealth` returns 0 for *all* tiles (or verify cache is empty via reflection/helper) because the load should abort.

## 4. MEDIUM SEVERITY: Fix Disjoint Range Computation in Thread Safety Test

### Problem Analysis
In `SoilHealthServiceTests.ThreadSafety_WithDisjointTileRanges_DoesNotThrow`, the tile X coordinate is calculated as `(workerId * 10) + j`. Since `j` goes from 0 to 99, the ranges overlap.
*   Worker 0: 0-99
*   Worker 1: 10-109 (Overlaps 10-99 with Worker 0)

### Proposed Solution
Change the multiplier to ensure disjoint ranges.
`int x = (workerId * 100) + j;`

### Implementation Strategy
1.  Update the calculation in `LivingRoots.Tests/SoilHealthServiceTests.cs`.

## 5. LOW SEVERITY: Refactor README.md Path Resolution

### Problem Analysis
`AppContext.BaseDirectory` can behave differently across different test runners and platforms, potentially leading to `FileNotFoundException` when trying to locate the `README.md` file relative to the test assembly.

### Proposed Solution
Use `Directory.GetCurrentDirectory()` which is often more reliable for test execution contexts, or implement a robust search up the directory tree. Also, add a `File.Exists` assertion to fail fast with a clear message.

### Implementation Strategy
1.  Update `ModEntryTests.Readme_ContainsCorrectGitHubReleasesLink`.
2.  Add `Assert.True(File.Exists(readmePath), ...)` before reading.

## 6. LOW SEVERITY: Use Reflection for _state Field Access

### Problem Analysis
Tests currently access `ModController._state` directly (likely via `InternalsVisibleTo`). If the field's access modifier is changed to `private` (as it should be for encapsulation), tests will break.

### Proposed Solution
Use reflection to access the `_state` field in tests, ensuring robustness against access modifier changes.

### Implementation Strategy
1.  Update `ModControllerTests` to use `typeof(ModController).GetField("_state", ...)` for all state verifications.

## 7. LOW SEVERITY: Add Timeout to Task.WaitAll

### Problem Analysis
Concurrency tests use `Task.WaitAll(tasks)` which can hang indefinitely if a deadlock occurs or a task never completes.

### Proposed Solution
Add a timeout to `Task.WaitAll` (e.g., 30 seconds) to ensure the test suite fails fast rather than hanging.

### Implementation Strategy
1.  Update all concurrency tests in `SoilHealthServiceTests.cs` and `ModControllerTests.cs`.
2.  Use `Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(30))` or similar overload.

## Compliance with Principles
*   **SOLID**:
    *   **SRP**: `LoadData` is now strictly responsible for loading *valid* data or nothing.
    *   **OCP**: Changes are extensions or fixes to existing logic.
*   **DRY**: Re-use existing rollback logic or helpers where possible.
*   **KISS**: The "abort on limit" logic is simpler than "partial load with warnings".
*   **YAGNI**: No new features, just fixes.
*   **DDD**: Preserves the domain invariant that a Location cannot have > 500 tiles.

## Testing Strategy
1.  **Unit Tests**:
    *   Verify `LoadData` returns empty/clears cache when limit exceeded.
    *   Verify `UnregisterEvents` restores state on failure (using mocks to simulate failure).
    *   Verify thread safety tests pass with corrected ranges and timeouts.
    *   Verify README test passes locally.
2.  **Manual Verification**:
    *   Review logs for correct error messages.
