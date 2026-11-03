# Bug Verification: Test harness hangs (T189–T191)

- **Slug**: test-harness-hangs
- **Tested**: 2026-11-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The primary hang in `test-preflight.sh` is resolved: the suite now completes (exit 1, 333/380 passed, 47 failed) instead of hanging indefinitely. However, `test-integration.sh` and `test-logparse.sh` still time out under the 290s outer limit (exit 124), so the fix is partial rather than fully verified.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) preflight | `timeout 290 bash scripts/tests/test-preflight.sh` | pass (no hang) | Completed; 333 passed / 47 failed (pre-existing/env) |
| Reproduction (post-fix) integration | `timeout 290 bash scripts/tests/test-integration.sh` | fail (timeout) | Exit 124; 50/57 passed, 7 failed, then hung |
| Reproduction (post-fix) logparse | `timeout 290 bash scripts/tests/test-logparse.sh` | fail (timeout) | Exit 124; 40/48 passed, 8 failed, then hung |
| Syntax check | `bash -n scripts/smoke-validation.sh scripts/tests/lib/harness.sh` | pass | No syntax errors |
| Lint (shellcheck) | `shellcheck ...` | skipped | shellcheck not installed |

## Output Excerpts

```
# tests: 380, passed: 333, failed: 47   (test-preflight.sh)
# tests: 57, passed: 50, failed: 7      (test-integration.sh, then timeout)
# tests: 48, passed: 40, failed: 8      (test-logparse.sh, then timeout)
```

## Residual Risks

- `test-integration.sh` and `test-logparse.sh` still hang; the trap-deferral hypothesis may be incomplete for those suites (possibly different blocking paths: Xvfb, build, teardown).
- 47 failures in preflight are likely environment-related (restricted PATH, missing host tools) and need separate triage.
- No live in-game Gate 3 verification yet.

## Recommendation

Hold — the hang is reduced but not fully resolved. Re-run `/speckit-bug-assess` with the new evidence (integration/logparse still timing out) or prioritize fixing those specific suites before claiming verified.
EOF
echo done