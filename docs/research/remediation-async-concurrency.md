# SMAPI Concurrency Model: Game Loop Thread vs. Asynchronous I/O

**Date:** 2026-09-05  
**Purpose:** Research SMAPI's threading and concurrency model to decide whether to amend the project constitution or refactor a functional requirement.  
**Sources:** SMAPI GitHub repository (Pathoschild/SMAPI), SMAPI DeepWiki, Stardew Valley Modding Wiki, SMAPI documentation.

---

## Executive Summary

SMAPI's concurrency model is **fundamentally single-threaded on the game loop thread** for all event callbacks. There is **no use of `Interlocked`/`Volatile.Read` or lock-free atomic patterns** in the SMAPI codebase — the architecture achieves thread safety through:

1. Synchronous event dispatch on the game loop thread
2. Reader-writer locks only for content manager list (background-thread content loading)
3. Explicit suppression of mod events during concurrent game operations (saving, loading)
4. `Game1.IsOnMainThread()` guard for render-target operations

The standard pattern for snapshotting render state is **synchronous snapshot diffs** taken within the same game tick, not lock-free atomic reads.

---

## 1. Does SMAPI Use `Interlocked`/`Volatile.Read` (Lock-Free Atomic Patterns)?

### Finding: NO — SMAPI does not use lock-free atomic patterns internally

A GitHub code search for `Interlocked` and `Volatile` in the Pathoschild/SMAPI repository returned **zero results**. ([Source: GitHub code search](https://github.com/search?q=Interlocked+Volatile+repo%3APathoschild%2FSMAPI&type=code))

Instead, SMAPI achieves thread safety through:

- **`lock` statements** on handler lists (in `ManagedEvent.cs`) for thread-safe subscription/unsubscription
- **`ReaderWriterLockSlim`** in `ContentCoordinator.cs` for the content manager list (which is accessed from background threads)
- **Synchronous execution model** where all event handlers run on the game loop thread

### Relevant Code References

**`ManagedEvent.cs`** — Thread-safe handler management uses `lock`:
```csharp
// From src/SMAPI/Framework/Events/ManagedEvent.cs
public void Add(EventHandler<TEventArgs> handler, IModMetadata mod)
{
    lock (this.Handlers)
    {
        // ...
        this.Handlers.Add(managedHandler);
        this.CachedHandlers = null;
    }
}
```
[Source](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/Events/ManagedEvent.cs)

**`ContentCoordinator.cs`** — Reader-writer locks for background-thread content loading:
```csharp
// From src/SMAPI/Framework/ContentCoordinator.cs
// The game creates content managers on background threads (e.g., during the load screen),
// so thread-safe access is essential to prevent race conditions.
```
[Source](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/ContentCoordinator.cs)

### Implication for Your Constitution

SMAPI does **not** mandate async/await for mod code, nor does it provide precedent for `Interlocked`/`Volatile.Read` patterns for same-thread frame coordination. If your project constitution assumes lock-free atomic patterns are needed for game-loop/render-thread coordination, that assumption is **not grounded in SMAPI precedent**. The synchronous snapshot pattern is the idiomatic approach.

---

## 2. How Do Rendering Events (`RenderedWorld`) Work?

### Finding: Synchronous on the game loop thread, raised mid-draw-call

Rendering events in SMAPI are **synchronous** and run on the **game loop thread** (which is also the draw thread). They are raised from within the game's `_draw` method override in `SGame.cs`.

**`SGame.cs`** — Draw loop integration:
```csharp
// From src/SMAPI/Framework/SGame.cs
protected override void _draw(GameTime gameTime, RenderTarget2D target_screen)
{
    Context.IsInDrawLoop = true;
    try
    {
        base._draw(gameTime, target_screen);  // Game1._draw raises OnRenderingStep/OnRenderedStep
        this.OnRendered(target_screen);       // Raises Rendered event
        this.DrawCrashTimer.Reset();
    }
    catch (Exception ex)
    {
        // Error recovery...
    }
    Context.IsInDrawLoop = false;
}
```
[Source](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/SGame.cs)

**`SCore.cs`** — `RaiseRenderEvent` method (lines ~1000+):
```csharp
// From src/SMAPI/Framework/SCore.cs
private void RaiseRenderEvent<TEventArgs>(...)
{
    if (!@event.HasListeners)
        return;

    bool wasOpen = spriteBatch.IsOpen(this.Reflection);
    bool hadRenderTarget = Game1.graphics.GraphicsDevice.RenderTargetCount > 0;

    // Guard: can't set render target on background thread
    if (!hadRenderTarget && !Game1.IsOnMainThread())
        return;

    try
    {
        if (!wasOpen)
            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        // ... set render target ...
        @event.Raise(eventArgs);  // Synchronous handler invocation
    }
    finally
    {
        // ... cleanup ...
    }
}
```
[Source](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/SCore.cs)

### Event Timing (from `IDisplayEvents.cs` and DeepWiki)

| Event | Timing |
|-------|--------|
| `RenderingWorld` | Before world is drawn — during world render |
| `RenderedWorld` | After world is drawn to sprite batch — before it's rendered to screen |
| `RenderingActiveMenu` | Before menu is drawn — during menu render |
| `RenderedActiveMenu` | After menu is drawn to sprite batch — before rendered to screen |
| `RenderingHud` | Before HUD is drawn — during HUD render |
| `RenderedHud` | After HUD is drawn to sprite batch — before rendered to screen |

[Source: IDisplayEvents.cs](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Events/IDisplayEvents.cs)  
[Source: DeepWiki Events API](https://deepwiki.com/Pathoschild/SMAPI/6.1-events-api)

### Key Observations

1. **Render events are raised synchronously on the game loop thread** — they are NOT async or dispatched to background threads.
2. **Sprite batch is opened temporarily** for the event if needed — mods can draw during `RenderedWorld`, `RenderedActiveMenu`, `RenderedHud`, and `Rendered`.
3. **`Context.IsInDrawLoop`** is a boolean flag set during the draw loop, readable by mods.
4. **Background thread guard**: SMAPI checks `Game1.IsOnMainThread()` before setting render targets — if not on the main thread, the render event is silently skipped.
5. **`RenderedWorld` is the standard event for world overlays** — running after the world is drawn but before menu/HUD, making it ideal for world-space overlays.

### Standard Pattern for Snapshotting State Mid-Frame

SMAPI does **not** use mid-frame snapshots for render events. Instead:

- Render events are raised at a **specific point in the draw pipeline** when the game state is at a known-good configuration
- The sprite batch is opened with `SpriteSortMode.Deferred` and `BlendState.AlphaBlend`
- State that mods need to read (like world position, player position) is read directly from `Game1.player`, `Game1.currentLocation`, etc.
- SMAPI does NOT provide a "snapshot" object for render state — mods read live game state

---

## 3. Synchronous Snapshot Reads vs. Lock-Free Atomic: Community Precedent

### Finding: SMAPI uses synchronous snapshot diffs, NOT lock-free atomics

SMAPI's approach to state consistency is the **Watcher/Snapshot pattern** — a synchronous, single-threaded diff taken each game tick.

**`WatcherSnapshot.cs`** — Synchronous snapshot diff:
```csharp
// From src/SMAPI/Framework/StateTracking/Snapshots/WatcherSnapshot.cs
internal class WatcherSnapshot
{
    public SnapshotDiff<Point> WindowSize { get; } = new();
    public PlayerSnapshot? CurrentPlayer { get; private set; }
    public SnapshotDiff<int> Time { get; } = new();
    // ... more diffs ...

    public void Update(WatcherCore watchers)
    {
        // Synchronous update — no locks, no atomics
        this.WindowSize.Update(watchers.WindowSizeWatcher);
        this.Locale.Update(watchers.LocaleWatcher);
        this.CurrentPlayer?.Update(watchers.CurrentPlayerTracker!);
        // ...
    }
}
```
[Source](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/StateTracking/Snapshots/WatcherSnapshot.cs)

**`SCore.cs` — Update tick snapshot flow** (lines 800-850):
```csharp
// From src/SMAPI/Framework/SCore.cs — OnPlayerInstanceUpdating method
// Update, snapshot, and reset watchers in one atomic operation on the game thread:
instance.Watchers.Update();
instance.WatcherSnapshot.Update(instance.Watchers);
instance.Watchers.Reset();
WatcherSnapshot state = instance.WatcherSnapshot;

// Now use 'state' to raise events with strongly-typed change info
```
[Source](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/SCore.cs)

### The Pattern

1. **Watchers** track raw game state changes during a tick
2. **Snapshot** is taken synchronously (same thread, no contention)
3. **Watchers** are reset for the next tick
4. Events are raised with snapshot data

This is **not lock-free atomic** — it's a **cooperative single-threaded snapshot**. It works because:

- All game logic runs on the game loop thread
- The snapshot is taken at a well-defined point in the update loop
- No other thread modifies game state during the snapshot (SMAPI explicitly suppresses events during concurrent operations)

### Implication

If your project needs render state consistency, the SMAPI-preferred pattern is:
- Read state **synchronously** at a known point in the event pipeline
- Use a **snapshot diff** pattern (capture state → compare → react to changes)
- Do NOT use `Interlocked`/`Volatile.Read` — there's no multi-threaded reader scenario in SMAPI's event model

---

## 4. What Does SMAPI Say About Blocking the Game Loop Thread?

### Finding: SMAPI implicitly discourages blocking; explicitly suppresses events during concurrent operations

SMAPI does not have a single "don't block the game thread" document, but the codebase reveals its stance through **defensive design**:

#### 4a. SMAPI Suppresses Events During Concurrent Game Operations

From `SCore.cs` — `OnPlayerInstanceUpdating`:

```csharp
// From src/SMAPI/Framework/SCore.cs

// While a background task is in progress, the game may make changes to the game
// state while mods are running their code. This is risky, because data changes can
// conflict (e.g. collection changed during enumeration errors) and data may change
// unexpectedly from one mod instruction to the next.
//
// Therefore, we can just run Game1.Update here without raising any SMAPI events.
if (Game1.gameMode == Game1.loadingMode)
{
    events.UnvalidatedUpdateTicking.RaiseEmpty();
    runUpdate();
    events.UnvalidatedUpdateTicked.RaiseEmpty();
    return;
}

// Raise minimal events while saving.
// While the game is writing to the save file in the background, mods can unexpectedly
// fail since they don't have exclusive access to resources.
if (Context.IsSaving())
{
    // ... only raise save-related events ...
    runUpdate();
    return;
}
```
[Source](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/SCore.cs)

**Key insight:** SMAPI treats saving and loading as operations where the game is doing concurrent I/O. Rather than letting mods run code that might conflict with the background save, it **suppresses events entirely**.

#### 4b. SMAPI Warning: Unvalidated Events Bypass Safety Checks

From `EventManager.cs` — the `UnvalidatedUpdateTicking`/`UnvalidatedUpdateTicked` events exist for edge cases but come with a warning:

> **Warning**: Unvalidated events bypass SMAPI's safety checks and should only be used when necessary. Mods using these events receive a `UsesUnvalidatedUpdateTick` warning.

[Source: DeepWiki Events API - Specialized Events](https://deepwiki.com/Pathoschild/SMAPI/6.1-events-api)

From [GitHub Issue #446](https://github.com/Pathoschild/SMAPI/issues/446), Pathoschild stated:
> "I added `SpecialisedEvents.UnvalidatedUpdateTick` for use cases like that. It's the nuclear option of events: it will run on every update tick, including while the game is reading or writing a save or running async code. This is very much 'use at your peril' and will trigger a warning in the SMAPI console."

#### 4c. Render Event Guard for Background Threads

From `SCore.cs`:
```csharp
if (!hadRenderTarget && !Game1.IsOnMainThread())
    return; // can't set render target on background thread
```
[Source](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/SCore.cs)

This guard silently skips render events if the current thread is not the main game thread — SMAPI prefers to skip rendering over crashing or blocking.

#### 4d. Error Isolation Prevents Cascading Failures

From `ManagedEvent.cs`:
```csharp
// Each handler is invoked within a try-catch block
foreach (ManagedEventHandler<TEventArgs> handler in this.GetHandlers())
{
    Context.HeuristicModsRunningCode.Push(handler.SourceMod);
    try
    {
        handler.Handler(null, args);
    }
    catch (Exception ex)
    {
        this.LogError(handler, ex);  // Logs but doesn't crash
    }
    finally
    {
        Context.HeuristicModsRunningCode.TryPop(out _);
    }
}
```
[Source](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/Events/ManagedEvent.cs)

### Official SMAPI Stance Summary

| Concern | SMAPI Approach |
|---------|----------------|
| Blocking the game thread | Not explicitly documented as forbidden, but the entire architecture assumes event handlers are fast and synchronous |
| Async I/O | SMAPI uses `Task.Run` only for non-blocking operations like update checks and content integrity scans. Mod event handlers are never async. |
| Concurrent state access | SMAPI suppresses events during saving/loading rather than providing synchronization primitives |
| Thread safety | Achieved through single-threaded event dispatch + `lock` for subscription management + `ReaderWriterLockSlim` for content loading |

---

## 5. The SMAPI Concurrency Architecture in Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                     Game Loop Thread (Main)                      │
│                                                                   │
│  Game1.Update() ──► SGame.Update() ──► SCore.OnGameUpdating()   │
│       │                                         │                │
│       │                                    OnPlayerInstanceUpdating()
│       │                                         │                │
│       │                              ┌──────────┴──────────┐    │
│       │                              │ Watchers.Update()    │    │
│       │                              │ WatcherSnapshot.Update│   │
│       │                              │ Watchers.Reset()     │    │
│       │                              └──────────┬──────────┘    │
│       │                                         │                │
│       │                              ┌──────────┴──────────┐    │
│       │                              │ Raise Events:       │    │
│       │                              │  - UpdateTicking    │    │
│       │                              │  - Input events     │    │
│       │                              │  - World events     │    │
│       │                              │  - Player events    │    │
│       │                              │  (all synchronous)  │    │
│       │                              └──────────┬──────────┘    │
│       │                                         │                │
│       ▼                                         ▼                │
│  Game1._draw() ──► SGame._draw() ──► RaiseRenderEvent()         │
│                                        │                          │
│                              ┌─────────┴──────────┐             │
│                              │ Render Events:     │             │
│                              │  - RenderingWorld  │             │
│                              │  - RenderedWorld   │             │
│                              │  - RenderedHud     │             │
│                              │  (all synchronous) │             │
│                              └────────────────────┘             │
│                                                                   │
│  ┌─── Suppressed during saving/loading ───┐                     │
│  │  - No mod events raised                │                     │
│  │  - Only UnvalidatedUpdateTick fires    │                     │
│  └────────────────────────────────────────┘                     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Background Threads                            │
│                                                                   │
│  Content Loading: GameContentManager creates content on bg thread│
│    → ContentCoordinator uses ReaderWriterLockSlim for safety    │
│                                                                   │
│  SMAPI Update Checks: Task.Run(CheckForUpdatesAsync)            │
│    → Deliberately single background thread for HTTP calls       │
│                                                                   │
│  Save File I/O: Game1 performs save on background thread        │
│    → SMAPI suppresses all mod events during saving              │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. Recommendations for Your Project Constitution

Based on this research, here are the findings relevant to your constitution/FR decision:

### Finding 1: No Lock-Free Atomic Precedent in SMAPI
SMAPI does not use `Interlocked`/`Volatile.Read` anywhere in its codebase. These patterns are unnecessary because SMAPI's event model is entirely single-threaded for game state access.

**Recommendation:** If your constitution mandates lock-free atomic patterns for frame coordination, that mandate is **not supported by SMAPI precedent**. Consider amending to allow synchronous snapshot patterns instead.

### Finding 2: `RenderedWorld` Is Synchronous on the Game Loop Thread
Render events are raised synchronously from the game's draw method. There is no async rendering pipeline in SMAPI.

**Recommendation:** If your FR assumes async rendering or render-thread decoupling, that assumption is **incompatible with SMAPI's architecture**. Refactor to assume synchronous render event handling.

### Finding 3: Snapshot Diffs Are the Standard Pattern
SMAPI's `WatcherSnapshot` pattern (synchronous update → snapshot → reset) is the canonical approach for state consistency across frames.

**Recommendation:** Adopt or allow the snapshot-diff pattern for render state consistency. This provides deterministic, single-threaded state access without the complexity of lock-free atomics.

### Finding 4: SMAPI Blocks the Game Thread Intentionally
SMAPI raises events synchronously, meaning slow handlers directly impact frame rate. SMAPI mitigates this through error isolation and crash recovery, NOT through async dispatch.

**Recommendation:** If your constitution requires non-blocking guarantees for game-loop operations, it diverges from SMAPI's model. Either:
- Accept synchronous execution (aligns with SMAPI), or
- Introduce a bounded async pattern (diverges from SMAPI, requires justification)

### Finding 5: Concurrent I/O Is Handled by Event Suppression
SMAPI's approach to background I/O (saving/loading) is to suppress mod events entirely, not to provide synchronization primitives.

**Recommendation:** If your FR involves reading state during async I/O operations, do NOT expect SMAPI-style synchronization. The precedent is to pause mod processing during concurrent operations, not to coordinate with them.

---

## 7. Key Source Files Referenced

| File | Purpose |
|------|---------|
| `src/SMAPI/Framework/SGame.cs` | Game loop override, draw loop integration |
| `src/SMAPI/Framework/SCore.cs` | Core event dispatch, render event raising, save/load suppression |
| `src/SMAPI/Framework/Events/ManagedEvent.cs` | Event handler invocation, error isolation |
| `src/SMAPI/Framework/Events/EventManager.cs` | Event registry and categorization |
| `src/SMAPI/Framework/StateTracking/Snapshots/WatcherSnapshot.cs` | Synchronous state snapshot pattern |
| `src/SMAPI/Events/IDisplayEvents.cs` | Render event interface definitions |
| `src/SMAPI/Events/IGameLoopEvents.cs` | Game loop event interface definitions |
| `src/SMAPI/Utilities/PerScreen.cs` | Per-screen state management utility |
| `src/SMAPI/Context.cs` | Game state context (IsInDrawLoop, IsSaving, etc.) |
| `src/SMAPI/Framework/ContentCoordinator.cs` | Thread-safe content loading with ReaderWriterLockSlim |

---

## 8. Citations

- [SMAPI GitHub Repository (Pathoschild/SMAPI)](https://github.com/Pathoschild/SMAPI)
- [SMAPI Events API — DeepWiki](https://deepwiki.com/Pathoschild/SMAPI/6.1-events-api)
- [SMAPI Game Integration — DeepWiki](https://deepwiki.com/Pathoschild/SMAPI/2.2-game-integration)
- [SMAPI Performance and Optimization — DeepWiki](https://deepwiki.com/Pathoschild/SMAPI/7.5-performance-and-optimization)
- [SMAPI Event System — DeepWiki](https://deepwiki.com/Pathoschild/SMAPI/2.3-event-system)
- [Modding:Modder Guide/APIs/Events — Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events)
- [Modding:Modder Guide/APIs/Utilities — Stardew Valley Wiki](https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Utilities)
- [SMAPI Issue #151 — Graphics events render target bug](https://github.com/Pathoschild/SMAPI/issues/151)
- [SMAPI Issue #446 — UnvalidatedUpdateTick for concurrent game code](https://github.com/Pathoschild/SMAPI/issues/446)
- [SMAPI GitHub code search — Interlocked/Volatile (zero results)](https://github.com/search?q=Interlocked+Volatile+repo%3APathoschild%2FSMAPI&type=code)

---

*This document was prepared to support a decision on whether to amend a project constitution or refactor a functional requirement related to async/concurrency patterns in SMAPI mod development.*
