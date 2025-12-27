# PR #72 Round 106 Fixes - Architectural Plan

## Executive Summary

This architectural plan addresses code review feedback from PR #72 Round 106. It focuses on fixing documentation inaccuracies, improving test robustness and determinism, and refining helper methods in the test suite. The plan covers five specific issues identified in the review.

## Issues Analysis

### MEDIUM SEVERITY: Broken Releases Link in README
- **File**: `README.md`
- **Issue**: The GitHub Releases link points to the old repository name "Stardew-LivingRoots" instead of "stardew-raizes-vivas".
- **Impact**: Users cannot easily access the latest releases, leading to poor user experience.
- **Root Cause**: The repository URL was updated but the documentation link was not.

### MEDIUM SEVERITY: Unsafe Constructor Selection in Test Helper
- **File**: `LivingRoots.Tests/ModControllerTests.cs`
- **Issue**: `CreateInstanceWithFallback` attempts to use constructors that have required reference-type parameters by passing `Type.Missing` or similar, which may fail or produce invalid objects.
- **Impact**: Flaky tests or `TargetInvocationException` when a constructor receives unexpected arguments.
- **Root Cause**: The helper method lacks a check to skip constructors that cannot be satisfied with default values, optional parameters, or value type defaults.

### LOW SEVERITY: Non-Deterministic Concurrency Test
- **File**: `LivingRoots.Tests/SoilHealthServiceTests.cs`
- **Issue**: `ThreadSafety_MultipleThreadsAccessingService_DoesNotThrow` uses overlapping tile ranges for concurrent tasks. While verifying no exceptions is useful, overlapping writes to the same keys makes the final state non-deterministic and could hide race conditions related to dictionary expansion vs entry modification.
- **Impact**: Reduced test value and potential flakiness.
- **Root Cause**: All threads calculate tiles using `j % 10` and `j / 10`, resulting in collisions.

### LOW SEVERITY: Misleading Feature Claims
- **File**: `README.md`
- **Issue**: The README lists features like "Visual indicators", "Health degrades over time", and "Composting" as if they are implemented, but they are currently planned features.
- **Impact**: Misleads users about the current state of the mod.
- **Root Cause**: Documentation reflects the final vision rather than the current implementation status.

### LOW SEVERITY: Missing Trailing Newline
- **File**: `LivingRoots/docs/architectural_and_refactor_plans/pr72_round106_fixes_architectural_plan.md` (Self)
- **Issue**: Architectural plan files should end with a trailing newline.
- **Impact**: Minor linting/formatting violation.
- **Root Cause**: File generation often omits the final newline.

## Proposed Solutions

### Solution 1: Update README Links (MEDIUM)
Update `README.md` to point to the correct repository URL.

```markdown
- [GitHub Releases](https://github.com/lfgranja/Stardew-LivingRoots/releases)
+ [GitHub Releases](https://github.com/lfgranja/stardew-raizes-vivas/releases)
```

### Solution 2: Safe Constructor Selection (MEDIUM)
Modify `CreateInstanceWithFallback` in `LivingRoots.Tests/ModControllerTests.cs` to explicitly skip constructors with required reference-type parameters.

```csharp
foreach (var constructor in constructors)
{
    var parameters = constructor.GetParameters();

    // Skip constructors that require reference types we can't satisfy
    if (parameters.Any(p => !p.IsOptional && !p.HasDefaultValue && !p.ParameterType.IsValueType))
    {
        continue;
    }

    try
    {
        // ... existing instantiation logic ...
    }
    // ...
}
```

### Solution 3: Deterministic Concurrency Test (LOW)
Update `ThreadSafety_MultipleThreadsAccessingService_DoesNotThrow` in `LivingRoots.Tests/SoilHealthServiceTests.cs` to use disjoint tile sets for each thread.

```csharp
for (int i = 0; i < 10; i++)
{
    int workerId = i; // Capture loop variable
    var task = System.Threading.Tasks.Task.Run(() =>
    {
        try
        {
            for (int j = 0; j < 100; j++)
            {
                // Use workerId to ensure unique tiles for each thread
                var tile = new Vector2((workerId * 100) + j, workerId);
                // ... existing logic ...
            }
        }
        // ...
    });
    tasks.Add(task);
}
```

### Solution 4: Clarify Feature Status (LOW)
Update `README.md` to mark unimplemented features as planned.

```markdown
### Soil Health System
- Each farm tile has a persistent soil health value (0-100%)
- Health values are saved and loaded with the game
- Visual indicators show soil health status (Planned)
- Health degrades over time when soil is left bare (Planned)
- Health improves with compost application (Planned)
```

### Solution 5: Ensure Trailing Newline (LOW)
Ensure this document and future documents end with a single newline character.

## Implementation Strategy (TDD)

1.  **Test Updates**:
    -   Update `SoilHealthServiceTests.cs` to use the deterministic logic.
    -   Verify `ModControllerTests.cs` helper improvement (can be verified by existing tests passing, or adding a specific test case for a class with an unsatisfiable constructor).

2.  **Code Fixes**:
    -   Apply the fix to `CreateInstanceWithFallback` in `ModControllerTests.cs`.

3.  **Documentation Updates**:
    -   Update `README.md` links and feature descriptions.

## Risk Assessment

-   **Test Helper Changes**: Modifying `CreateInstanceWithFallback` could potentially break other tests if they relied on the "best effort" instantiation of partial objects.
    -   *Mitigation*: Run all tests in `ModControllerTests` to ensure no regressions.
-   **Documentation Accuracy**: Marking features as "Planned" might be incorrect if some *are* partially implemented.
    -   *Mitigation*: Verify implementation status by checking `SoilHealthService` and `ModController` logic (confirmed: visual indicators and degradation logic are not present in current visible files).

## Testing Strategy

-   **Manual Verification**: Check `README.md` links manually or via browser tool if available (not needed for text change).
-   **Unit Tests**: Run `dotnet test` to verify `LivingRoots.Tests`.
    -   Specifically check `ThreadSafety_MultipleThreadsAccessingService_DoesNotThrow` passes.
    -   Check `CreateInstanceWithFallback` behavior.

## Compliance

-   **SOLID**: Improvements to test helpers respect Single Responsibility (helper does one thing: create instances safely).
-   **DRY**: Reusing the helper correctly avoids duplication in test setup.
-   **KISS**: The documentation fixes are simple text replacements. The test fix uses simple arithmetic for unique IDs.
-   **YAGNI**: We are only fixing what is broken/misleading, not adding new features.
-   **DDD**: N/A for these specific changes (mostly test/docs), but accurate documentation supports the domain understanding.

## Dependencies and Implementation Order

1.  **ModControllerTests.cs**: Fix `CreateInstanceWithFallback`.
2.  **SoilHealthServiceTests.cs**: Fix concurrency test.
3.  **README.md**: Fix links and feature claims.
4.  **Final Verification**: Run all tests.
