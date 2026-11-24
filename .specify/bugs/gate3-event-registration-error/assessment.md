# Bug Assessment: Gate 3 smoke run fails with "Error occurred while registering game events"

- **Slug**: gate3-event-registration-error
- **Created**: 2026-11-03
- **Source**: pasted text (user request) + local file `verification-evidence/gate3-smoke-20261102-0057.log`
- **Verdict**: likely valid, needs reproduction
- **Severity**: high

## Report (verbatim or summarized)

The Gate 3 smoke run (`verification-evidence/gate3-smoke-20261102-0057.log`) shows the mod loading under SMAPI 4.5.2 / Stardew 1.6.15, but during event registration the log records:

```
[Stardew Valley - Living Roots] Error occurred while registering game events.
```

No exception type or HResult appears in the captured log. The run also failed Steam (steamclient.so not found), but the game launched. Gate 3 (3 signals: soil-health JSON delta, crop phase/phaseDays delta, SMAPI log) was never completed.

## Symptom

During `ModController.RegisterEvents()`, an exception is thrown inside the `try` block and caught by `HandleRegistrationError`, which logs `"Error occurred while registering game events."` at `LogLevel.Error` and the exception type/HResult only at `LogLevel.Trace`. Because the default SMAPI log level filters Trace, the actual exception is invisible in the captured smoke log. The mod may partially function (events not registered), which blocks any in-game verification of shipped features.

## Reproduction

1. Launch SMAPI with the Living Roots mod + verification mod (headless or interactive).
2. Observe the SMAPI log for the line `Error occurred while registering game events.`
3. The exception type/HResult is only at Trace level — not captured by default.

[NEEDS CLARIFICATION: exact exception type and stack trace — requires enabling Trace logging for the mod or raising the log level of the registration error.]

## Suspected Code Paths

- `LivingRoots/Controllers/ModController.cs:194` `RegisterEvents()` — entire subscription block wrapped in try/catch; the throw happens somewhere inside.
- `LivingRoots/Controllers/ModController.cs:270-300` — visualization subscription block (`_visualizationService != null`): subscribes `gameLoop.UpdateTicked`, `eventsDisplay.RenderedWorld`, `eventsInput.ButtonReleased`, `eventsDisplay.MenuChanged`, `eventsDisplay.Rendered`, `gameLoop.Saved`. Any null deref here (e.g., `eventsDisplay`/`eventsInput` null guards exist, but `gameLoop` is non-null) could throw.
- `LivingRoots/Controllers/ModController.cs:258-268` — `worldEvents.ObjectListChanged` subscription (guarded by null check).
- `LivingRoots/Controllers/ModController.cs:296` — `_helper.Events.Input.ButtonPressed` (Input events may be null in headless contexts).
- `LivingRoots/ModEntry.cs` — wiring of `ModController` and optional services (visualization, host atomic lock, providers); a mis-wired dependency could be null and throw on access.
- `LivingRoots/Services/Visualization/VisualizationService.cs` — if instantiated, its constructor or `_onUpdateTickedHandler` assignment could throw.

## Root Cause Hypothesis

Confidence: medium. The exception is real (the catch block ran), but the root cause is **not identifiable from the available smoke log** because the exception type/HResult is logged only at `LogLevel.Trace`, which SMAPI filters by default. The most likely candidates, in order:

1. A null dereference in the visualization subscription path (e.g., `_helper.Events.Input` null in the headless smoke environment, or `gameLoop.UpdateTicked` access).
2. A disposed/null dependency captured in the snapshots (`_monitor`, `_helper`) if `RegisterEvents` runs after partial disposal.
3. An exception thrown by a service constructor that was lazily initialized.

The Steam/steamclient.so failure is environmental (Steam not running) and unrelated to event registration.

## Proposed Remediation

**Preferred**: Make the registration failure diagnosable without changing security posture:
- Temporarily enable Trace logging for the Living Roots mod ID via `smapi-internal/config.user.json` (`VerboseLogging: ["lfgranja.LivingRoots"]`) and re-run the smoke to capture the exception type/HResult.
- Alternatively, raise the log level of the registration-error exception type from `LogLevel.Trace` to `LogLevel.Warn` (still no raw message) so default SMAPI logs capture it — this is the smallest code change that unblocks diagnosis.

Once the exception is identified, fix the underlying null deref / wiring issue in `RegisterEvents` (or the offending service).

**Alternatives**:
- Add a dedicated integration test that calls `RegisterEvents()` in a stubbed environment and asserts no exception (requires `ThreadSafeGameLoopEventsStub`).
- Gate the visualization subscription block behind an explicit null check on `_helper.Events.Input`/`Display` (already partially done, but `gameLoop.UpdateTicked` is accessed unconditionally inside the `_visualizationService != null` branch).

**Files likely to change**:
- `LivingRoots/Controllers/ModController.cs` (log level + null guards)
- `LivingRoots/ModEntry.cs` (wiring verification)
- `LivingRoots.Tests/` (regression test for registration)

**Tests to add or update**:
- A test asserting `RegisterEvents()` does not throw when `_visualizationService` is null (headless path).
- A test asserting `RegisterEvents()` does not throw when `helper.Events.Input` is null.

## Risks & Considerations

- Raising log level to Warn increases log noise but does not leak raw exception messages (still compliant with security principle).
- The fix must not weaken the atomic state machine or rollback logic.
- Gate 3 (3 signals) cannot be declared complete until this is resolved and a clean smoke run is captured.
- Steam/steamclient.so absence is environmental, not a code bug; document it as a prerequisite.

## Open Questions

- [NEEDS CLARIFICATION: exact exception type/HResult — requires Trace capture or log-level bump.]
- [NEEDS CLARIFICATION: is `_visualizationService` non-null in the smoke run? Depends on ModEntry wiring for headless/test env.]
- [NEEDS CLARIFICATION: does the game launch fully without Steam, or does the missing steamclient.so abort the run before events fire?]
