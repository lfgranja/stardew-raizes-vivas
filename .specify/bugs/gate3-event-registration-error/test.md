# Bug Verification: Gate 3 smoke run fails with "Error occurred while registering game events"

- **Slug**: gate3-event-registration-error
- **Tested**: 2026-11-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The fix addresses the exact NullReferenceException (HResult 0x80004003) captured in `SMAPI-latest.txt` by null-guarding the unguarded `_helper.Events.Input.ButtonPressed` subscription in `ModController.RegisterEvents()`. The new regression test `RegisterEvents_WithNullInputEvents_DoesNotThrow` passes, and the broader `ModController` suite (41 tests) is green. However, the original reproduction (a live SMAPI smoke run) was not re-executed in this session (no display/Steam), so the 3 Gate 3 signals were not captured. Result downgraded from `verified` to `partial` per guardrail.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | Live SMAPI smoke run | skipped | No display/Steam in this environment; smoke log (`verification-evidence/gate3-smoke-20261102-0057.log`) not re-run |
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

- The original symptom (NRE during `RegisterEvents`) was identified from the Trace-level entry in `SMAPI-latest.txt`, not reproduced post-fix in a live game. The fix targets the exact line (`_helper.Events.Input.ButtonPressed`) and the NRE type matches, but a live smoke run is still required to confirm.
- Gate 3's 3 signals (save diff `soil_health_data_{saveId}.json`, `Crop.phase`/`phaseDays` delta, SMAPI log) were not captured. "Verified" status for shipped features remains blocked until Gate 3 runs.
- Steam/steamclient.so absence is environmental and unrelated to this fix.

## Recommendation

Hold — the code-level fix and regression test are in place and green, but the original reproduction (live SMAPI smoke run) was not re-executed in this session. Run the Gate 3 smoke with `VerboseLogging` for `lfgranja.LivingRoots` and confirm the NRE line is gone and the 3 signals are captured before marking this bug closed. If the NRE persists, re-run `/speckit.bug.assess` with the new Trace evidence.
