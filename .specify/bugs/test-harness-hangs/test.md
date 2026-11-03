# Bug Verification: Test harness hangs (T189–T191)

- **Slug**: test-harness-hangs
- **Tested**: 2026-11-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: failed

## Summary

The harness-level changes (bounded drain in `_run_script_impl`, 300s per-suite cap in `run-all.sh`) were applied, but the original symptom still reproduces: `test-preflight.sh` hangs after TC-24 (FR-012 item 8, restricted-PATH host-tool sweep) and never reaches `finish_tests`. The hang is in the `run_preflight_env` invocation of `smoke-validation.sh` under a restricted PATH; `timeout --kill-after=5 120` does not reap the child within the deadline (observed 2m35s). The bounded drain alone did not resolve the hang, indicating the root cause is not solely the grandchild-held-pipe EOF (mechanism 2) but also trap deferral / process-group escape (mechanisms 1 and 3).

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix): test-preflight.sh | `timeout 120 bash scripts/tests/test-preflight.sh` | fail | Still hangs; killed at 120s. |
| Reproduction (post-fix): smoke-validation.sh (restricted PATH) | `timeout 10 … scripts/smoke-validation.sh --timeout 5 …` | pass | Exits 9 immediately when run directly. |
| bash -n on changed files | `bash -n scripts/tests/lib/harness.sh scripts/tests/run-all.sh` | pass | No syntax errors. |
| New/updated tests | (none added) | not-run | Deferred. |
| Regression suite | `test-game-path.sh`, `test-verify-cli.sh`, `test-runall.sh`, spec-consistency | pass | Unaffected by the fix. |
| Lint / type-check | n/a (bash) | skipped | No shellcheck installed in env. |

## Output Excerpts

```
+ env … PATH=…/fixtures/block:…/fixtures/bin:… timeout --kill-after=5 120 \
    /home/…/scripts/smoke-validation.sh --timeout 5 --grace-period 2 --build-timeout 5 \
    --game-path /tmp/…/fixtures/game
# (no further output; process hung for 2m35s, killed manually)
```

`test-preflight.sh` reached the FR-012 item 8 section (missing host tool Xvfb) and stalled inside the `run_preflight_env` invocation.

## Residual Risks

- The smoke-validation.sh signal trap (`forward_signal` for INT/TERM/HUP, EXIT trap `on_exit`) may be deferring SIGTERM because Bash does not run a trap while a foreground command is blocked, so `timeout`'s SIGTERM is never handled and the process only dies via SIGKILL after `--kill-after=5` — which, per the observation, did not fire. Possible causes: the child is in an uninterruptible wait, or `timeout`'s group signaling did not reach the process.
- Grandchildren (Xvfb, dotnet) may have escaped the process group via `setsid`, surviving the group kill and holding the output pipe open; the bounded `timeout 5 cat` should have covered this, so the hang is likely not solely pipe-EOF.
- The per-suite 300s cap in `run-all.sh` mitigates aggregate stalls but does not fix the underlying hang.

## Recommendation

Reopen and re-assess: the bounded-drain fix is insufficient. The next iteration must (a) confirm whether `smoke-validation.sh`'s ERR/EXIT traps are actually firing under `timeout`, (b) add explicit process-group reaping (`kill -- -pgid` with a bounded `wait`) in `_run_script_impl`, and (c) consider running the script under test with `setsid` so the whole tree is addressable. Re-run `/speckit.bug.assess` with this new evidence, or apply the deferred smoke-validation.sh trap hardening from the assessment before re-testing.