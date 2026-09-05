# Remediation: FR-013 Concurrency Requirement Clarification

**Date**: 2026-09-05
**Status**: Proposed
**Related**: FR-013, FR-017, FR-019, FR-020, Constitution Principle III

---

## 1. Problem Statement

FR-013 currently states:

> "All concurrency MUST use async/await patterns per project constitution — no .Result or .Wait() blocking."

This creates a **technical tension** because:

1. **MonoGame/XNA `SpriteBatch` rendering is inherently synchronous and not thread-safe** — it must run on the game loop thread. There is no "awaitable" render operation.
2. **SMAPI event handlers are synchronous** — the existing `ModController` uses `Interlocked` flag-based state machines, not async/await, for all game-loop coordination.
3. **The constitution's Principle III** says "Async/await is the only acceptable pattern for **asynchronous operations**" — the qualifier matters. Game-loop-thread synchronization is not an "asynchronous operation" in the I/O sense; it is same-thread coordination that must complete within a single frame (≤16.67ms).

The current FR-013 wording, taken literally, would forbid the lock-free `Interlocked` patterns that the existing `ModController` already uses and that the plan.md correctly identifies as the appropriate mechanism for render-thread coordination.

---

## 2. Scenario Analysis

### Scenario 1: Health Value Changing Mid-Render (FR-020)

| Aspect | Analysis |
|--------|----------|
| **Nature** | Synchronous game-loop-thread coordination |
| **I/O involved?** | No — health values are in-memory state |
| **Threading model** | Both the value change and the render happen on the game loop thread; the concern is frame-to-frame consistency, not cross-thread synchronization |
| **Constitution mapping** | Principle III does not apply — there is no asynchronous operation to await |
| **Appropriate mechanism** | Lock-free state snapshot: `Volatile.Read` of a health-value version counter before rendering begins; if the counter changes mid-frame, complete the current frame with the snapshot and apply the new value on the next frame |
| **Existing precedent** | `ModController.ExecuteWithConcurrencyGuard()` uses `Interlocked.CompareExchange` on a `_state` flag field — same pattern, same thread |

**Verdict**: This scenario requires **synchronous coordination only**. Async/await is irrelevant because there is no I/O and no second thread. The constitution's async rule does not apply.

---

### Scenario 2: Config Hot-Reload During Active Rendering (FR-019)

| Aspect | Analysis |
|--------|----------|
| **Nature** | Mixed — I/O-bound file read + synchronous state application |
| **I/O involved?** | Yes — reading the configuration file from disk is true async I/O |
| **Threading model** | File watch fires (potentially on a thread-pool thread); config must be applied atomically to the render loop |
| **Constitution mapping** | The **file read** is an asynchronous operation → Principle III applies → must use async/await |
| **Appropriate mechanism** | 1. `await ReadAllTextAsync()` for loading the config file (async I/O, no `.Result`).<br>2. Parse the JSON on the calling thread (synchronous, fast).<br>3. `Interlocked.Exchange(ref _activeConfig, newConfig)` to atomically swap the configuration reference on the game loop thread.<br>4. Render loop reads `_activeConfig` via `Volatile.Read` each frame. |
| **Existing precedent** | `ModController.OnSaveLoaded` calls `_soilHealthService.LoadData(saveId)` synchronously — but that service wraps SMAPI's `IModDataService.Load()`, which is synchronous by SMAPI design. For true file I/O outside SMAPI's data API, async/await is required. |

**Verdict**: This scenario has **two distinct phases** with different rules:
- **Phase A (file read)**: Async I/O → must use async/await per Principle III.
- **Phase B (atomic apply)**: Game-loop coordination → lock-free `Interlocked.Exchange`, not async/await.

---

### Scenario 3: Save/Load Pausing Rendering (FR-017)

| Aspect | Analysis |
|--------|----------|
| **Nature** | Mixed — I/O-bound save/load + synchronous pause/resume signaling |
| **I/O involved?** | Yes — persisting soil health data to disk is true async I/O |
| **Threading model** | SMAPI `Saving`/`SaveLoaded` events fire on the game loop thread; the render loop must pause before the save begins and resume after it completes |
| **Constitution mapping** | The **data persistence** is an asynchronous operation → Principle III applies → must use async/await if the save implementation uses async I/O |
| **Appropriate mechanism** | 1. Set an atomic `_renderPaused` flag via `Interlocked` before save begins (synchronous, game-loop thread).<br>2. Render loop checks `_renderPaused` via `Volatile.Read` each frame and skips rendering when set.<br>3. The save operation itself uses `await SaveDataAsync()` if the underlying I/O supports it.<br>4. After save completes, clear the flag via `Interlocked` and refresh render data. |
| **Existing precedent** | `ModController.OnSaving` uses `ExecuteWithConcurrencyGuard(OnSavingExecutingFlag, ...)` — an `Interlocked`-based re-entrancy guard on the game loop thread. |

**Verdict**: This scenario has **two distinct phases**:
- **Phase A (pause/resume flag)**: Game-loop coordination → `Interlocked` flag, not async/await.
- **Phase B (data persistence)**: Async I/O → must use async/await per Principle III.

---

## 3. Key Insight: What "Asynchronous" Means in the Constitution

Constitution Principle III states:

> "Async/await is the only acceptable pattern for **asynchronous operations**."

The critical qualifier is **"asynchronous operations"** — in the .NET/SMAPI context, this refers to **I/O-bound work** (file reads, network calls, database queries) where the thread would otherwise block waiting for an external resource.

**Game-loop-thread coordination is NOT an "asynchronous operation"** because:
- It involves no I/O.
- It happens on a single thread (the game loop thread).
- The goal is frame consistency (completing the current frame with a snapshot), not offloading work to another thread.
- `async/await` on the game loop thread would actually be harmful — `await` would yield control back to the game loop, allowing the frame to continue with potentially inconsistent state.

The existing `ModController` demonstrates the correct pattern: `Interlocked` operations, `Volatile.Read`, and atomic flag-based state machines for all game-loop coordination. This is **not a violation** of Principle III — it is the correct application of it.

---

## 4. Proposed FR-013 Rewording

### Current FR-013

> **FR-013**: System MUST handle the following concurrent scenarios safely without deadlocks or race conditions: (1) health value changing mid-render (per FR-020 frame consistency), (2) config hot-reload during active rendering (per FR-019 atomic application), (3) save/load pausing rendering (per FR-017 pause/resume). All concurrency MUST use async/await patterns per project constitution — no .Result or .Wait() blocking. *Verification: Trigger each concurrent scenario 100 times in tests and confirm no deadlocks, UI freezes, or state corruption.*

### Proposed FR-013

> **FR-013**: System MUST handle the following concurrent scenarios safely without deadlocks or race conditions: (1) health value changing mid-render (per FR-020 frame consistency), (2) config hot-reload during active rendering (per FR-019 atomic application), (3) save/load pausing rendering (per FR-017 pause/resume).
>
> **Concurrency rules by operation type:**
> - **I/O-bound operations** (config file load, save/load data persistence): MUST use async/await patterns per Constitution Principle III — no `.Result` or `.Wait()` blocking.
> - **Game-loop-thread coordination** (render state snapshots, atomic config swaps, pause/resume flags): MUST use lock-free or atomic synchronization (`Interlocked`, `Volatile.Read`, atomic flag-based state machines) on the game loop thread. Async/await is not applicable to same-thread frame coordination.
>
> *Verification: Trigger each concurrent scenario 100 times in tests. For I/O scenarios: confirm no `.Result`/`.Wait()` calls via code inspection and no deadlocks in test output. For game-loop coordination scenarios: confirm no mixed-state frames, no UI freezes, and no state corruption via frame-level assertions.*

---

## 5. Alignment with Constitution

| Principle | Alignment |
|-----------|-----------|
| **III. Async-Only Concurrency** | The proposed rewording **strengthens** Principle III by clarifying its scope: it applies to I/O-bound asynchronous operations, not to same-thread game-loop coordination. The existing `ModController` patterns (`Interlocked`, `Volatile.Read`) are the correct tool for game-loop coordination and do not violate the constitution. |
| **IV. Test-Driven Development** | The proposed verification is now actionable: code inspection for `.Result`/`.Wait()` on I/O paths, and frame-level assertions for game-loop coordination. |
| **V. Simplicity and YAGNI** | No new abstractions are introduced. The rewording explicitly endorses the existing `ModController` pattern rather than requiring a new async-compatible wrapper for rendering. |

---

## 6. Implementation Guidance for Developers

### Pattern A: Game-Loop Coordination (Scenarios 1, 2-phase-B, 3-phase-A)

Use the existing `ModController` pattern:

```csharp
// Atomic flag for pause/resume
private int _renderPausedFlag = 0; // 0 = false, 1 = true

// Set from game loop thread (e.g., in OnSaving handler)
Interlocked.Exchange(ref _renderPausedFlag, 1);

// Check in render loop (game loop thread, synchronous)
if (Volatile.Read(ref _renderPausedFlag) == 1)
    return; // Skip rendering this frame

// Atomic config swap
Interlocked.Exchange(ref _activeConfig, newConfig);

// Render loop reads via Volatile.Read
var config = Volatile.Read(ref _activeConfig);
```

### Pattern B: Async I/O (Scenarios 2-phase-A, 3-phase-B)

```csharp
// Config file load — true async I/O
public async Task<VisualizationConfiguration> LoadConfigAsync()
{
    string json = await File.ReadAllTextAsync(_configPath).ConfigureAwait(false);
    return ParseConfiguration(json); // Synchronous parse after async read
}

// Save data — true async I/O (if the save service supports it)
public async Task SaveDataAsync(string saveId, SoilHealthData data)
{
    string json = JsonSerializer.Serialize(data);
    await File.WriteAllTextAsync(GetSavePath(saveId), json).ConfigureAwait(false);
}
```

### Pattern C: Bridging Async I/O to Game Loop (Scenarios 2 & 3)

```csharp
// In the config file watcher callback (may be on thread-pool thread):
private async void OnConfigChanged(object sender, FileSystemEventArgs e)
{
    var newConfig = await _configService.LoadConfigAsync();
    // Marshal to game loop thread via SMAPI's helper or Interlocked swap
    Interlocked.Exchange(ref _pendingConfig, newConfig);
}

// In the render loop (game loop thread, each frame):
var pending = Interlocked.Exchange(ref _pendingConfig, null);
if (pending != null)
    _activeConfig = pending; // Atomic apply — no mixed-state frames
```

---

## 7. Verification Strategy (Actionable)

| Scenario | Test Approach | Success Criteria |
|----------|---------------|------------------|
| **1. Health value mid-render** | Use `ThreadSafeGameLoopEventsStub` to fire a health-update event during `Draw()` call. Capture frame output before and after. | Current frame completes with old value; next frame shows new value. No torn reads. Run 100 iterations. |
| **2. Config hot-reload** | (a) Code inspection: confirm `LoadConfigAsync` uses `await`, no `.Result`/`.Wait()`. (b) Integration test: trigger file change during render, assert all tiles use same config within one frame. | No `.Result`/`.Wait()` in I/O path. No mixed-state frames. Run 100 iterations. |
| **3. Save/load pause** | (a) Code inspection: confirm save/load uses `await` if async I/O. (b) Integration test: trigger save during render, assert zero overlay draw calls during save window, correct data after load. | No `.Result`/`.Wait()` in I/O path. Zero frames rendered during save. Correct state after resume. Run 100 iterations. |

---

## 8. Summary

| Scenario | I/O? | Constitution Rule | Mechanism |
|----------|------|-------------------|-----------|
| 1. Health value mid-render | No | Principle III N/A (no async operation) | `Interlocked` / `Volatile.Read` snapshot |
| 2. Config hot-reload | Yes (file read) | Principle III applies to I/O | `await ReadAllTextAsync()` + `Interlocked.Exchange` |
| 3. Save/load pause | Yes (data persistence) | Principle III applies to I/O | `Interlocked` flag + `await SaveDataAsync()` |

The proposed FR-013 rewording resolves the tension by:
1. **Preserving the constitution's intent** — async/await is still mandatory for all I/O.
2. **Acknowledging technical reality** — game-loop rendering is synchronous by design and requires lock-free coordination, not async/await.
3. **Making verification actionable** — separating I/O verification (code inspection + deadlock detection) from coordination verification (frame consistency assertions).
