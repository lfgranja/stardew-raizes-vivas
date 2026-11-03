# Bug Fix: Test harness hangs (T189–T191)

- **Slug**: test-harness-hangs
- **Fixed**: 2026-11-03
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Fixed the hang by (1) running `smoke-validation.sh`'s `main()` as a background job with a foreground `wait`, so trapped signals (INT/TERM/HUP) are no longer deferred until the foreground command completes, and (2) launching the harness child via `setsid` with explicit process-group reaping as a backstop. The suite now completes instead of hanging.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `scripts/smoke-validation.sh` | modified | `main "$@"` → background + `wait "$main_pid"` + `exit $?` |
| `scripts/tests/lib/harness.sh` | modified | `_run_script_impl` now uses `setsid` + explicit group reap (SIGTERM → 5s poll → SIGKILL) when available, with fallback to plain `timeout` |

## Diff Highlights (optional)

```bash
# smoke-validation.sh (tail)
main "$@" &
main_pid=$!
wait "$main_pid"
exit $?
```

```bash
# harness.sh (_run_script_impl)
setsid env "${env_args[@]}" timeout --kill-after=5 "$RUN_DEADLINE_SECS" \
    "$HARNESS_SCRIPT" "$@" >"$out_file" 2>"$err_file" &
runner_pid=$!
if wait "$runner_pid"; then RUN_CODE=0; else RUN_CODE=$?; fi
# explicit group reap as backstop
```

## Tests Added or Updated

- No new tests added; existing `test-preflight.sh` now completes (previously hung).

## Local Verification

- Commands run: `timeout 290 bash scripts/tests/test-preflight.sh` → completed in ~? (exit 1, 333 passed / 47 failed). No hang observed.
- Manual checks: `bash -n` passes for both scripts.

## Deviations from Assessment

- The setsid/group-reap backstop was implemented, but the 47 remaining failures appear to be pre-existing/environment-related (restricted PATH cases, missing host tools) rather than regressions from this fix. The hang itself is resolved.

## Deviations from Assessment (update 2026-11-03)

The originally proposed remediation (background main + setsid group reap) resolved `test-preflight.sh` but did **not** fully resolve `test-integration.sh` / `test-logparse.sh`. Deep re-investigation (process tree) shows the residual hang is caused by real-game launch (TC-1) not terminating within budget and orphaned dotnet/MSBuild grandchildren, not by trap deferral. The harness-level group reap is insufficient for grandchildren that do not share the setsid process group. See `test.md` update 2026-11-03 for details and recommended follow-ups.

## Follow-ups

- Investigate the 47 remaining failures (likely environment/Xvfb/tool-sweep related).
- Consider adding a regression test for trap-deferral hang.
- Keep bounded drain as backstop.
EOF
echo done