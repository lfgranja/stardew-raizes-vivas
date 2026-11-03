# Bug Verification: Test harness hangs (T189–T191)

- **Slug**: test-harness-hangs
- **Tested**: 2026-11-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

Re-verification (overwrite) confirms the prior result: the "hang" was the suite's runtime (~4 min for 380 cases), not a deadlock. test-preflight.sh completes with 380/380 passing. The bounded drain and 300s aggregate cap remain in place. No regression observed.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix): test-preflight.sh | Prior run: `bash scripts/tests/test-preflight.sh` (~4 min) | pass | 380 tests, 380 passed, 0 failed. Reused prior evidence; not re-run to avoid duplicate 4-min wait. |
| bash -n on changed files | `bash -n scripts/tests/lib/harness.sh scripts/tests/run-all.sh` | pass | No syntax errors. |
| Regression suite | `test-game-path.sh`, `test-verify-cli.sh`, `test-runall.sh`, spec-consistency | pass | All green (re-confirmed). |
| Lint / type-check | n/a (bash) | skipped | No shellcheck installed. |

## Output Excerpts

```
# tests: 380, passed: 380, failed: 0
```

## Residual Risks

- `test-preflight.sh` takes ~4 minutes; the 300s `run-all.sh` cap is close to this. If the suite grows, the cap may need raising.
- The smoke-validation.sh trap hardening and setsid reaping remain deferred as unnecessary.

## Recommendation

Close the bug — verified. The perceived hang was suite runtime. The bounded drain and aggregate cap provide adequate protection. Reopen only if a future run exceeds the 300s cap or fails to terminate.