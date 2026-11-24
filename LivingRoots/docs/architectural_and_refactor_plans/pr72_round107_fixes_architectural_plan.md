# PR #72 Round 107 Fixes - Architectural Plan

## 1. Introduction

This document outlines the architectural plan for addressing the issues identified in Round 107 of the PR #72 code review. The focus is on improving thread safety, fixing test reliability, ensuring deterministic behavior, and adhering to SOLID/DRY principles.

## 2. Issue Analysis and Proposed Solutions

### 2.1. HIGH SEVERITY: Eliminate Unregister/Register Race Window in `UnregisterEvents`

**Analysis:**
In `ModController.UnregisterEvents`, the current implementation captures event handler references into local variables *before* setting the `UnregisteringFlag`. This creates a race window where another thread could potentially modify the handlers or the state could change between the snapshot and the flag acquisition. Although `UnregisterEvents` is generally called during disposal or explicit cleanup, ensuring strict thread safety requires that we claim exclusive access to the unregistration process *before* we read the state we intend to modify/unsubscribe.

**Proposed Solution:**
Move the capture of `_onGameLaunchedHandler`, `_onSaveLoadedHandler`, and `_onSavingHandler` to *after* the `UnregisteringFlag` has been successfully set using `Interlocked.CompareExchange`. This ensures that the snapshot reflects the state at the exact moment we have exclusive rights to unregister.

**Implementation Strategy:**
1.  Enter the state-claiming loop to set `UnregisteringFlag`.
2.  Once the flag is set, *then* read the volatile fields into local variables.
3.  Proceed with `SafeUnsubscribe`.

**Code Example:**
```csharp
public void UnregisterEvents()
{
    // 1. Claim unregistration rights first
    int currentState, newState;
    while (true)
    {
        currentState = Volatile.Read(ref _state);
        
        if ((currentState & UnregisteringFlag) != 0)
        {
             _monitor.Log("Event unregistration already in progress, skipping.", LogLevel.Trace);
            return;
        }
        
        // ... calculation of newState ...
        
        if (Interlocked.CompareExchange(ref _state, newState, currentState) == currentState)
        {
            break;
        }
    }

    // 2. Capture handler references AFTER claiming the flag
    // This ensures we are working with the stable state that we own
    var gameLaunchedHandler = Volatile.Read(ref _onGameLaunchedHandler);
    var saveLoadedHandler = Volatile.Read(ref _onSaveLoadedHandler);
    var savingHandler = Volatile.Read(ref _onSavingHandler);
    
    // ... proceed with unsubscription ...
}
```

### 2.2. MEDIUM SEVERITY: Fix Loop Variable Capture in Tests

**Analysis:**
In `SoilHealthServiceTests.ThreadSafety_MultipleThreadsAccessingService_DoesNotThrow` and `ThreadSafety_WithDisjointTileRanges_DoesNotThrow`, the loop variable `i` is captured by the lambda expression passed to `Task.Run`.
```csharp
for (int i = 0; i < 10; i++)
{
    var task = Task.Run(() => {
        int workerId = i; // 'i' is captured here
        // ...
    });
}
```
Because the lambda captures the *variable* `i`, not its value at that moment, all tasks might see the same value of `i` (e.g., 10) if the loop finishes before the tasks start. This defeats the purpose of assigning unique worker IDs and disjoint ranges.

**Proposed Solution:**
Introduce a local variable inside the loop scope to copy the value of `i`.

**Code Example:**
```csharp
for (int i = 0; i < 10; i++)
{
    int localI = i; // Copy value to local variable
    var task = Task.Run(() => {
        int workerId = localI; // Use local variable
        // ...
    });
}
```

### 2.3. MEDIUM SEVERITY: Stabilize Worker Range Isolation

**Analysis:**
This is the same root cause as 2.2 but specifically flagged for `ThreadSafety_WithDisjointTileRanges_DoesNotThrow`. The test is intended to verify that workers operating on different tiles don't interfere, but the loop variable capture means they might all try to operate on the same tiles (or invalid ones), rendering the test invalid or flaky.

**Proposed Solution:**
Apply the same fix as in 2.2: use a local copy of the loop variable.

### 2.4. MEDIUM SEVERITY: Await Worker Tasks Reliably

**Analysis:**
In `ThreadSafety_WithDisjointTileRanges_VerifiesDisjointRanges`, the test uses `tasks.Add(task.ContinueWith(...))`. This adds the *continuation* task to the list being awaited. If the original `task` fails (throws an exception), the continuation still runs (default behavior), and `Task.WhenAll` will likely succeed (unless the continuation itself fails). The exception from the original task might be swallowed or not properly asserted, potentially hiding concurrency bugs.

**Proposed Solution:**
Refactor the test to await the *original* tasks or ensure that exceptions from the original tasks are propagated. A cleaner approach is to perform the result collection inside the main task body (protected by a lock) and await the main task.

**Code Example:**
```csharp
var tasks = new List<Task>();
for (int i = 0; i < 5; i++)
{
    int workerId = i;
    var task = Task.Run(() =>
    {
        try 
        {
            var workerTileList = new List<Vector2>();
            // ... perform work ...
            
            // Store results safely
            lock (lockObj)
            {
                workerTiles.Add((workerId, workerTileList));
            }
        }
        catch (Exception ex)
        {
            lock (lockObj) exceptions.Add(ex);
        }
    });
    tasks.Add(task);
}
await Task.WhenAll(tasks);
```

### 2.5. MEDIUM SEVERITY: Avoid Clearing State via Empty IDs (Explicit Reset)

**Analysis:**
Currently, `SoilHealthService.LoadData` clears the cache if `saveId` is empty or invalid. While functional, this relies on a side effect of `LoadData` to perform a state reset. This violates the Principle of Least Surprise and Single Responsibility Principle. A `LoadData` method should load data, not primarily be used to clear state.

**Proposed Solution:**
1.  Add a `void Reset()` method to the `ISoilHealthService` interface.
2.  Implement `Reset()` in `SoilHealthService` to clear `_runtimeCache`.
3.  Update `LoadData` to call `Reset()` internally when it needs to clear state (e.g., on invalid ID or failure), instead of duplicating the clear logic or relying on recursive calls.
4.  Update `ModController` to call `_soilHealthService.Reset()` explicitly when appropriate (e.g. if no save ID is found).

**Code Example:**
```csharp
// ISoilHealthService.cs
public interface ISoilHealthService
{
    // ...
    void Reset();
}

// SoilHealthService.cs
public void Reset()
{
    lock (_lock)
    {
        _runtimeCache.Clear();
    }
    _monitor.Log("Soil health service state reset.", LogLevel.Trace);
}

public void LoadData(string saveId)
{
    if (string.IsNullOrWhiteSpace(saveId))
    {
        _monitor.Log("...", LogLevel.Warn);
        Reset(); // Use explicit method
        return;
    }
    // ...
}
```

### 2.6. MEDIUM SEVERITY: Prevent Empty-File Test Crashes

**Analysis:**
In `ArchitecturalPlanTests.ArchitecturalPlan_ShouldEndWithTrailingNewline`, the code checks `content[content.Length - 1]`. If the file is empty (0 bytes), accessing index -1 throws an `IndexOutOfRangeException`. This causes the test to crash instead of failing gracefully with a meaningful message.

**Proposed Solution:**
Add a check for file length before accessing the last character.

**Code Example:**
```csharp
var content = File.ReadAllText(filePath);
if (content.Length == 0)
{
    Assert.Fail($"The file {filePath} is empty.");
}
// ... proceed with check
```

### 2.7. LOW SEVERITY: Fix Optional-Parameter Reflection Invocation

**Analysis:**
In `ModControllerTests.CreateInstanceWithFallback`, the logic for handling optional parameters uses `Type.Missing`.
```csharp
if (p.IsOptional) return Type.Missing;
```
However, for C# optional parameters (which have a default value), `Type.Missing` is not the correct value to pass when invoking via reflection if we want the default behavior. We should use `p.DefaultValue`. `Type.Missing` is primarily for COM interop.

**Proposed Solution:**
Check `p.HasDefaultValue` and return `p.DefaultValue`.

**Code Example:**
```csharp
if (p.HasDefaultValue)
    return p.DefaultValue;
// Remove the specific check for p.IsOptional returning Type.Missing unless it's strictly necessary for some edge case not covered by HasDefaultValue
```

### 2.8. LOW SEVERITY: Make README Path Deterministic

**Analysis:**
In `ModEntryTests.Readme_ContainsCorrectGitHubReleasesLink`, the test uses `Directory.GetCurrentDirectory()` and traverses up multiple levels (`..`, `..`, ...). This is fragile because the current working directory depends on the test runner and environment configuration. It may differ between VS Code, Visual Studio, and CI pipelines.

**Proposed Solution:**
Use `AppContext.BaseDirectory` to get the directory where the test assembly is located, and navigate relative to that. This is more reliable.

**Code Example:**
```csharp
var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
```

## 3. Implementation Plan

The implementation will follow a strict TDD approach:

1.  **Refactor Tests (Red/Green/Refactor):**
    *   Fix the loop variable capture in `SoilHealthServiceTests`.
    *   Refactor `ThreadSafety_WithDisjointTileRanges_VerifiesDisjointRanges` to await tasks reliably.
    *   Fix `ArchitecturalPlanTests` to handle empty files.
    *   Fix `ModEntryTests` to use `AppContext.BaseDirectory`.
    *   Fix `CreateInstanceWithFallback` in `ModControllerTests`.

2.  **Interface Update:**
    *   Update `ISoilHealthService` to include `Reset()`.
    *   Update `SoilHealthService` to implement `Reset()`.
    *   Update `SoilHealthService.LoadData` to use `Reset()`.
    *   *Self-Correction:* This change will break `ModControllerTests` and `ModEntryTests` because they mock `ISoilHealthService`. I will need to update the mocks or the tests to account for the new method if strict mocking is used (though usually adding a method doesn't break existing mock setups unless strict behavior is enabled).

3.  **Core Logic Fix:**
    *   Refactor `ModController.UnregisterEvents` to move handler capture after state claiming.

## 4. Risk Assessment

*   **Thread Safety:** Moving the handler capture in `UnregisterEvents` is critical. If done incorrectly, it could lead to `NullReferenceException` if we try to unsubscribe null handlers (though `SafeUnsubscribe` handles nulls). The risk is low if we follow the plan.
*   **Interface Changes:** Adding `Reset()` to `ISoilHealthService` is a breaking change for the interface. Any other consumers (unlikely in this mod structure, but possible) would need to be updated. Mocks in tests might need adjustment.
*   **Test Stability:** The fixes to the tests should *improve* stability. The risk of introducing new instability is low.

## 5. Verification

*   Run all tests in `LivingRoots.Tests`.
*   Specifically run the `ThreadSafety` tests multiple times to ensure the race conditions and loop capture issues are resolved.
*   Verify `UnregisterEvents` logic by careful code review (as unit testing race conditions is probabilistic).
