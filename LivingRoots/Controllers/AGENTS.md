# CONTROLLERS

SMAPI event handlers. Thread-safe orchestration layer between game events and domain services.

## Files

| File | Role |
|------|------|
| `ModController.cs` | Main orchestrator: GameLaunched, SaveLoaded, Saving, RenderedWorld, ButtonReleased, UpdateTicked. Atomic flag state machine with rollback. |
| `CompostingBinController.cs` | Handles composting bin interactions (ButtonPressed). |

## Conventions

- **Re-entrancy protection**: All event handlers must use `ExecuteWithConcurrencyGuard(flag, name, action)` — never call handler logic directly.
- **Atomic state flags**: Use `Interlocked` operations on `_state` (`EventsRegisteredFlag`, `CommandRegisteredFlag`, `DisposedFlag`, `RegisteringFlag`, `UnregisteringFlag`, `OnSaveLoadedExecutingFlag`, `OnSavingExecutingFlag`). Never read/write `_state` directly without `Volatile.Read`/`Interlocked`.
- **Console commands**: Register via `_helper.ConsoleCommands.Add()` inside `lock (_commandLock)` with `CommandRegisteredFlag` deduplication.
- **Visualization events**: Only subscribe to `RenderedWorld`, `ButtonReleased`, `UpdateTicked` when `_visualizationService != null`.
- **Rollback**: If any unsubscription fails, re-subscribe successfully removed handlers (all-or-nothing). Implemented in `ExecuteRollback`.

## Anti-Patterns

- DO NOT subscribe to events outside of `RegisterEvents()`.
- DO NOT access disposed dependencies without null checks — always use `IsDisposed()` or null-conditional operators.
- DO NOT expose raw exception messages in logs — log `ex.GetType().FullName` and `HResult` at `LogLevel.Trace` only.
