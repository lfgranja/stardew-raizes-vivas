# Bug Fix: Test harness hangs (T189–T191)

- **Slug**: test-harness-hangs
- **Fixed**: 2026-11-04 (revision; supersedes the 2026-11-03 fix)
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The original hang was addressed on 2026-11-03 by backgrounding `main` and adding explicit
process-group reaping. That change introduced a second defect: the background job lost the ERR
trap and a bare `wait` tripped it, so every documented non-zero exit was reported as `13` (or as
an unreported `1`). The fix here restores exit-code propagation and separates "slow suite" from
"hung suite" in the runner.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `scripts/smoke-validation.sh` | `main` job re-installs the ERR trap; owning shell records `BASH_SUBSHELL`; `wait` captures status with `||`; `on_error` guard is depth-aware | Restores `14`/`5`/`12`/`10`/`9` exit codes and the exit-13 internal-error contract |
| `scripts/tests/run-all.sh` | per-file deadline is `RUNNER_FILE_TIMEOUT_SECS` (default 2700 s, `RUN_ALL_FILE_TIMEOUT_SECS` override); `124`/`137` reported as `HANG` | `test-logparse.sh` needs ~12–18 min because every row rebuilds (`--no-incremental`, ~21 s) |

## Diff Highlights

```bash
# smoke-validation.sh (tail)
run_main_in_background() {
    LR_ERROR_SHELL_DEPTH="$BASH_SUBSHELL"
    trap 'on_error "$?" "$LINENO" "${FUNCNAME[0]:-<top-level>}"' ERR
    main "$@"
}
run_main_in_background "$@" &
main_pid=$!
main_status=0
wait "$main_pid" || main_status=$?
exit "$main_status"
```

```bash
# run-all.sh
TMPDIR="${tmpdir}" exec timeout --kill-after=5 "${RUNNER_FILE_TIMEOUT_SECS}" bash -- "${test_file}"
# status 124/137 => HANG + "[deadline fired after Ns]"
```

## Tests

- `test-cli.sh`: 13 failures → 1. T138 and T149 now pass (one report, exit 13, matching code).
  The remaining failure is T128's wall-clock bound (15 s vs `<= 14`); the exit code is still
  `124`/`137`, so the deadline fired and no process survived.
- `test-preflight.sh`: re-run after the fix; exit-code cases that reported `13` now report the
  expected `9`/`10`/`14`.
- `test-save-preservation.sh` through `run-all.sh`: still passes.

## Deviations from Assessment

- The build-reuse optimization was rejected: spec 008 FR-015 forbids a user-facing environment
  switch in `smoke-validation.sh`, so the suite keeps the real rebuild and the runner's deadline
  was sized for it instead.

## Follow-ups

- Re-derive T128's elapsed bound (or shorten the interrupt grace) so `test-cli.sh` is fully green.
- Re-run and record `test-integration.sh` separately; it is opt-out and the slowest suite.
- Add a regression case for exit-code propagation through the background job (the T138/T149
  cases already cover the ERR path; a plain `--no-such-option` → `14` case covers the `wait` path).
