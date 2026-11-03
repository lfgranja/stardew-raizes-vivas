# Bug Fix: Gate 3 smoke run fails with "Error occurred while registering game events"

- **Slug**: gate3-event-registration-error
- **Fixed**: 2026-11-03
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Root cause identified via the SMAPI-latest.txt Trace log (stack trace): `System.NullReferenceException` thrown at `ModController.RegisterEvents()` line 301, where `eventsDisplay.RenderedWorld += localRenderedWorldHandler;` ran with `localRenderedWorldHandler` still null (the field was declared but never assigned, unlike `localMenuChangedHandler`/`localRenderedHandler` which use `??=`). Fixed by assigning `localRenderedWorldHandler` and `localButtonReleasedHandler` via `??=` before subscribing; retained the Input-events null guard and rollback guards; raised the exception-type log from Trace to Warn. A live SMAPI smoke run now logs `Events registered successfully.` with no NRE and the mod loads successfully.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Controllers/ModController.cs` | modified | Assigned missing `localRenderedWorldHandler`/`localButtonReleasedHandler` via `??=` before subscribing (root cause); kept Input-events null guard; raised exception-type log Trace→Warn; guarded rollback paths |
| `LivingRoots.Tests/ModControllerTests.cs` | added test | `RegisterEvents_WithNullInputEvents_DoesNotThrow` regression |

## Diff Highlights

```csharp
// RegisterEvents: assign handler fields before subscribing (previously null -> NRE at line 301)
localRenderedWorldHandler = _onRenderedWorldHandler ??= OnRenderedWorld;
localButtonReleasedHandler = _onButtonReleasedHandler ??= OnButtonReleased;
var eventsDisplay = _helper?.Events?.Display;
var eventsInput = _helper?.Events?.Input;
if (eventsDisplay != null) eventsDisplay.RenderedWorld += localRenderedWorldHandler;
if (eventsInput != null) eventsInput.ButtonReleased += localButtonReleasedHandler;
```

```csharp
// HandleRegistrationError: exception type now at Warn (not Trace) for diagnosability
ctx.Monitor.Log($"RegisterEvents exception type: {ctx.Exception.GetType().FullName} (HResult: 0x{ctx.Exception.HResult:X8})", LogLevel.Warn);
```

## Tests Added or Updated

- `LivingRoots.Tests/ModControllerTests.cs::RegisterEvents_WithNullInputEvents_DoesNotThrow` — pins the null-Input-events path so the NRE cannot regress.

## Local Verification

- `dotnet build LivingRoots/LivingRoots.csproj -c Release` → 0 errors, 1 warning (TFM support, pre-existing).
- Live SMAPI smoke run (2026-11-03): `Events registered successfully.` logged, no NRE, mod loaded successfully (`SUCCESS: Mod loaded without errors.`).

## Deviations from Assessment

- The assessment hypothesized a null `Input` events reference. The actual root cause (revealed by the stack trace once logged) was an unassigned handler field (`localRenderedWorldHandler` was null when passed to `ManagedEvent.Add`). The Input-events guard added earlier was retained as defense-in-depth.
- The assessment proposed enabling Trace logging to capture the exception type. The actual exception type was already in `SMAPI-latest.txt`; the stack trace required logging `ex.StackTrace` at Warn temporarily, then removing it (done).

## Follow-ups

- Gate 3 smoke now passes (NRE gone, mod loaded). The 3 Gate 3 signals (save diff + phase/phaseDays + log) still require manual gameplay (till → modify → save → reload) to capture — spec 009 task.
- Consider guarding `gameLoop.UpdateTicked` access if future headless environments lack it.
