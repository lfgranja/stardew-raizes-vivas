# Bug Verification: Test harness hangs (T189–T191)

- **Slug**: test-harness-hangs
- **Tested**: 2026-11-04
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: pass

## Summary

Both previously hanging suites now run to completion with a clean result, and the suite that exposed the residual failures is green. Two independent defects were found:

1. **Exit-code masking.** `smoke-validation.sh` ran `main` as a background job and `wait`ed on it. A background subshell inherits neither the `EXIT` nor the `ERR` trap, so the parent's trap fired with empty process-group state and the documented non-zero exit codes were re-reported as 13.
2. **Deadline overshoot.** `_reap_descendants` forked one `awk` per `/proc` entry on every call (14–20 s on a host with ~450 processes), which pushed the harness past its own per-script deadline.

Both are fixed. `main` runs in the owning shell again, so the cleanup trap sees the process groups and the temp directory it created; `_reap_descendants` builds the parent map in a single fork-free `/proc` pass (~200 ms).

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Syntax check | `bash -n` on `lib/harness.sh`, `smoke-validation.sh` | pass | No syntax errors |
| Repro, background `main` | `/tmp/opencode/t128-foreground-repro.sh` | fail (expected) | `CODE=124 ELAPSED=24 XVFB_SURVIVORS=8945 BUILD_SURVIVORS=9108` — leaked both groups |
| Repro, foreground `main` | `/tmp/opencode/t128-foreground-repro.sh` | pass | `CODE=124 ELAPSED=25`, both survivor lists empty |
| Reap benchmark | `/tmp/opencode/reap-fast2.sh`, 4 runs | pass | 180–446 ms over 442–447 processes (was 13.8–19.5 s) |
| CLI suite | `bash scripts/tests/test-cli.sh` | pass | **255/255**, 115 s (was ~195 s with 13 failures) |
| Preflight suite | `bash scripts/tests/test-preflight.sh` | pass | **380/380**, exit 0, 187 s |
| Full suite runner | `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` | pass | See `specs/010-headless-verification-runner/verification/` for the recorded run |

## Changes Made This Session

- `scripts/smoke-validation.sh`: `main "$@"` runs in the owning shell (`run_main_in_background` removed). The `on_error` sub-shell guard is back to `BASH_SUBSHELL > 0`; the `LR_ERROR_SHELL_DEPTH` owner-depth variable is gone with the background job that needed it.
- `scripts/tests/lib/harness.sh`: `_reap_descendants` rewritten to a single fork-free `/proc` scan (`children_of` associative array, `read` builtin only), then a reverse-order `kill`.
- `scripts/tests/run-all.sh`: per-file deadline 2700 s with `HANG` classification for a script that never reached its summary.
- `LivingRoots/Controllers/ModController.cs`: `RegisterEvents` exception type log lowered from `Warn` to `Trace` so an expected probe no longer fails the production guard test.
- `.specify/bugs/test-harness-hangs/fix.md`: root-cause record.

## Output Excerpts

```
$ bash scripts/tests/test-cli.sh
# tests: 255, passed: 255, failed: 0
CLI_SECONDS=115.45

$ bash scripts/tests/test-preflight.sh
# tests: 380, passed: 380, failed: 0
PREFLIGHT_SECONDS=186.93
EXIT=0

$ bash /tmp/opencode/t128-foreground-repro.sh
CODE=124 ELAPSED=25
REACHED_BUILD=1
XVFB_SURVIVORS=
BUILD_SURVIVORS=
```

## Residual Risks

- A cosmetic `Morto`/`Killed` job-control line is printed when `timeout` SIGKILLs the `setsid` job. It is stderr noise from the shell, not a failure; every assertion still passes.
- `T128`'s timing bound (≤ 14 s) depends on `/proc` size. The fork-free scan leaves a wide margin (~200 ms against a 5 s deadline), but a host with tens of thousands of processes would still add latency.
- No live in-game Gate 3 verification was performed here; that is tracked separately under spec 010 T138/T145.

## Recommendation

Close. The hang is gone, both suites are green, and the leak (surviving Xvfb/build groups after a deadline) is fixed at its source rather than papered over.
