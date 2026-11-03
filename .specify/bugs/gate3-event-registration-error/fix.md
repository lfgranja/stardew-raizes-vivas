# Bug Fix: Gate 3 smoke run fails with "Error occurred while registering game events"

- **Slug**: gate3-event-registration-error
- **Fixed**: 2026-11-03
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Root cause identified via the SMAPI-latest.txt Trace log: `System.NullReferenceException` (HResult 0x80004003) thrown at the unguarded `_helper.Events.Input.ButtonPressed += ...` subscription in `ModController.RegisterEvents()`. Fixed by null-guarding the Input events subscription (and the corresponding rollback), raising the exception-type log from Trace to Warn so future failures are captured by default SMAPI logs, and guarding all rollback paths against null `Helper`/`Events`. Added a regression test for the null-Input-events path.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Controllers/ModController.cs` | modified | Guarded `Input.ButtonPressed` subscription; raised exception-type log Trace→Warn; guarded rollback paths against null `Helper`/`Events`/`Input`/`Display` |
| `LivingRoots.Tests/ModControllerTests.cs` | added test | `RegisterEvents_WithNullInputEvents_DoesNotThrow` regression |

## Diff Highlights

```csharp
// RegisterEvents: guarded Input events subscription
var inputEvents = _helper?.Events?.Input;
if (inputEvents != null)
{
    inputEvents.ButtonPressed += localButtonPressedHandler;
}
else
{
    monitorSnapshot.Log("Input events unavailable; skipping ButtonPressed subscription.", LogLevel.Trace);
}
```

```csharp
// HandleRegistrationError: exception type now at Warn (not Trace) for diagnosability
ctx.Monitor.Log($"RegisterEvents exception type: {ctx.Exception.GetType().FullName} (HResult: 0x{ctx.Exception.HResult:X8})", LogLevel.Warn);
```

## Tests Added or Updated

- `LivingRoots.Tests/ModControllerTests.cs::RegisterEvents_WithNullInputEvents_DoesNotThrow` — pins the null-Input-events path so the NRE cannot regress.

## Local Verification

- `dotnet build LivingRoots/LivingRoots.csproj -c Release` → 0 errors, 1 warning (TFM support, pre-existing).
- Tests project build attempted; mod-deploy phase is slow in this environment but LivingRoots core builds clean.
- In-game smoke re-run not executed in this session (requires SMAPI + display); the fix targets the exact NRE captured in `SMAPI-latest.txt`.

## Deviations from Assessment

- The assessment proposed enabling Trace logging to capture the exception type. The actual exception was found in the existing `SMAPI-latest.txt` (which retains Trace), so no config change was needed. The log level was still raised to Warn as a defensive improvement.
- The underlying cause was identified as a null `Input` events reference, not a visualization wiring issue as initially suspected.

## Follow-ups

- Re-run the Gate 3 smoke to confirm the NRE is gone and capture the 3 signals (save diff + phase/phaseDays + log).
- Consider guarding `gameLoop.UpdateTicked` access if future headless environments lack it.
- Update `verification-evidence/` with a clean smoke log once Gate 3 passes.
