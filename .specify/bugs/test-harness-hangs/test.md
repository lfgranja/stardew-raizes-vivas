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
---

## Update 2026-11-03 (deep re-investigation)

A second reproduction run (`timeout 90 bash scripts/tests/test-integration.sh`) reproduced the hang after TC-1's deploy-target assertion, with the process tree showing:
- a live `dotnet build --configuration Release --no-incremental` (real rebuild, ~2 min) and
- a live Stardew/SMAPI launch (`smoke-validation.sh` invoking the real game) that never exited within the 120 s budget, plus
- orphaned `dotnet`/`MSBuild`/`VBCSCompiler` processes from earlier runs (55+ min), indicating resource contention and incomplete reaping of grandchildren.

Conclusion: the residual hang is **not** a trap-deferral bug in the harness. The setsid + group-reap + background-main remediation already in place correctly reaps the direct child and its process group. The remaining hang comes from (a) real-game launch in TC-1 not terminating within the budget and (b) orphaned dotnet/MSBuild grandchildren that do not share the setsid process group and hold build locks/pipes open.

Recommended follow-ups (not applied here, to avoid killing tools owned by other agents/processes):
1. In `scripts/tests/test-integration.sh`, bound the real-game launch in TC-1 with an explicit `--timeout` and a post-launch kill of the game process; do not rely on the script's own 120 s deadline for a real SMAPI session.
2. In `_run_script_impl`, additionally enumerate and terminate descendant `dotnet`/`MSBuild`/`VBCSCompiler` processes by ancestry (not just PGID) when the deadline fires, to clear orphaned grandchildren.
3. Add a pre-suite cleanup that waits for/reaps any lingering smoke-validation/dotnet processes from prior runs before starting.
