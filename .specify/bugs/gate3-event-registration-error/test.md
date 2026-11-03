# Bug Verification: Gate 3 smoke run fails with "Error occurred while registering game events"

- **Slug**: gate3-event-registration-error
- **Tested**: 2026-11-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The fix addresses the exact NullReferenceException (HResult 0x80004003) captured in `SMAPI-latest.txt` by assigning the previously-unassigned `localRenderedWorldHandler` / `localButtonReleasedHandler` handler fields before subscribing. A live SMAPI smoke run (2026-11-03) now logs `Events registered successfully.` with no `Error occurred while registering game events.` and no NRE; the mod loaded successfully (`SUCCESS: Mod loaded without errors.`). The new regression test `RegisterEvents_WithNullInputEvents_DoesNotThrow` passes, and the broader `ModController` suite (41 tests) is green. The 3 Gate 3 signals (save diff + phase/phaseDays + log) are a separate spec-009 concern and remain to be captured during manual gameplay.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | Live SMAPI smoke run (smoke-validation.sh) | pass | `Events registered successfully.` logged; no NRE; `SUCCESS: Mod loaded without errors.` |
| New / updated tests | `dotnet test ... --filter "FullyQualifiedName~ModController"` | pass | 41 passed, 0 failed (includes `RegisterEvents_WithNullInputEvents_DoesNotThrow`) |
| Regression suite (ModController) | same as above | pass | 41 passed, 0 failed |
| Build (LivingRoots) | `dotnet build LivingRoots/LivingRoots.csproj -c Release` | pass | 0 errors, 1 pre-existing TFM warning |
| Lint / format | `dotnet format --verify-no-changes` | not-run | Not executed in this session |

## Output Excerpts

```
Aprovado!  – Com falha:     0, Aprovado:    41, Ignorado:     0, Total:    41, Duração: 937 ms - LivingRoots.Tests.dll (net6.0)
```

```
/home/luis/.../LivingRoots/Controllers/ModController.cs: ... 0 Erro(s)
```

## Residual Risks

- The NRE root cause was the unassigned `localRenderedWorldHandler`/`localButtonReleasedHandler` (line 301), not the Input.ButtonPressed guard added earlier. Both fixes are now in place.
- Gate 3's 3 signals (save diff `soil_health_data_{saveId}.json`, `Crop.phase`/`phaseDays` delta, SMAPI log) are a spec-009 concern and still require manual gameplay (till → modify → save → reload) to capture. The smoke run confirms registration, not the full signal set.
- Steam/steamclient.so absence is environmental and unrelated to this fix.

## Recommendation

Close the bug for the registration NRE — verified end-to-end via live SMAPI smoke run: `Events registered successfully.` logged, no NRE, mod loaded successfully. The remaining Gate 3 3-signal capture is a spec-009 task, not part of this bug.
