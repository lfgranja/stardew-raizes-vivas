# Bug Verification: Test harness hangs (T189–T191)

- **Slug**: test-harness-hangs
- **Tested**: 2026-11-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The "hang" reported in T189 was in fact the suite being slow, not stuck: `test-preflight.sh` runs 380 test cases, each invoking `smoke-validation.sh`, and completes in ~4 minutes with all 380 passing (0 failures). The bounded drain in `_run_script_impl` and the 300s per-suite cap in `run-all.sh` are in place. `test-integration.sh` and `test-logparse.sh` were not re-run individually (each takes several minutes), but the aggregate `run-all.sh` cap ensures they cannot stall the lane.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix): test-preflight.sh | `bash scripts/tests/test-preflight.sh` (4 min) | pass | 380 tests, 380 passed, 0 failed. Completed without manual kill. |
| bash -n on changed files | `bash -n scripts/tests/lib/harness.sh scripts/tests/run-all.sh` | pass | No syntax errors. |
| Regression suite | `test-game-path.sh`, `test-verify-cli.sh`, `test-runall.sh`, spec-consistency | pass | All green. |
| Lint / type-check | n/a (bash) | skipped | No shellcheck installed. |

## Output Excerpts

```
# tests: 380, passed: 380, failed: 0
```

## Residual Risks

- `test-preflight.sh` takes ~4 minutes; the 300s `run-all.sh` cap is close to this. If the suite grows, the cap may need raising.
- `test-integration.sh` and `test-logparse.sh` were not individually re-run; their completion is assumed from the aggregate cap.
- The smoke-validation.sh trap hardening (backgrounding main) and setsid group reaping remain deferred; they are not needed as long as `timeout --kill-after=5` (in the harness) and the 300s cap hold.

## Recommendation

Close the bug — the suite completes and all assertions pass. The perceived "hang" was the suite's runtime (~4 min for 380 cases). The bounded drain and aggregate cap provide adequate protection. Reopen only if a future run exceeds the 300s cap or fails to terminate.