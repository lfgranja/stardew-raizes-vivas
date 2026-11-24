# Bug Verification: version gate no longer false-hard-fails on the real install

- **Slug**: gate3-version-gate
- **Tested**: 2026-11-04
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The original symptom — `verify-scenario.sh` aborting with exit 19 at the version gate before launching the game — no longer reproduces. `version_gate_classify` now returns `INFO` (rc 0) for the real SMAPI 4.5.2 / Stardew Valley 1.6.15 installation, the runner exits 0, and SMAPI starts a new session. The new regression tests pass and the relevant runner suites remain green; `test-preflight.sh` still has its pre-existing, unrelated failures.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | `version_gate_classify "$GAME_PATH" "$GAME_PATH/Mods/LivingRoots/manifest.json"` | pass | Prints `INFO`, rc 0 (was `HARD-FAIL`, rc 19). |
| Reproduction (post-fix) | `verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json --game-path "<game>" --skip-build` | pass | Exit 0; newest bundle has no `blocked.json` with `version-mismatch`; SMAPI log shows a fresh 4.5.2 / 1.6.15 session. |
| New / updated tests | `SMOKE_SKIP_INTEGRATION=1 bash scripts/tests/test-version-gate.sh` | pass | 10/10 passed, exit 0. Includes directory-form compatible/older-game/older-SMAPI cases and `preflight_version_gate`. |
| Regression suite | `SMOKE_SKIP_INTEGRATION=1 bash scripts/tests/test-verify-cli.sh` | pass | 19/19 passed, exit 0. |
| Regression suite | `SMOKE_SKIP_INTEGRATION=1 bash scripts/tests/test-game-path.sh` | pass | 13/13 passed, exit 0. |
| Regression suite | `SMOKE_SKIP_INTEGRATION=1 bash scripts/tests/test-preflight.sh` | fail | Pre-existing failures (exit 13 instead of expected codes). Confirmed at HEAD before the fix: 47 `not ok`; after the fix: 45. Not caused by this change. |
| Lint / syntax | `bash -n scripts/lib/version-gate.sh scripts/tests/test-version-gate.sh` | pass | Syntax OK. |
| Whitespace | `git diff --check HEAD~1 HEAD` | pass | Clean. |
| Lint | `shellcheck ...` | not-run | `shellcheck` is not installed on this host (the repo's hook skips it in that case). |

## Output Excerpts

```
$ version_gate_classify "$GAME_PATH" "$GAME_PATH/Mods/LivingRoots/manifest.json"
INFO
rc=0
```

```
$ verify-scenario.sh ... --skip-build
RUNNER_EXIT=0
(newest bundle: evidence-bundle-run-20261104T052749Z-08d0 — no blocked.json)
```

```
$ SMOKE_SKIP_INTEGRATION=1 bash scripts/tests/test-version-gate.sh
ok - directory form: a compatible install does not hard-fail
ok - directory form: a compatible install is INFO
ok - preflight_version_gate: a compatible install returns 0
ok - directory form: an older game version still hard-fails (19)
ok - directory form: an older SMAPI version still hard-fails (19)
# tests: 10, passed: 10, failed: 0
```

```
SMAPI log (new session after the fix):
[01:27:53 INFO  SMAPI] SMAPI 4.5.2 with Stardew Valley 1.6.15 build 24356 on Unix 7.2.11.200
[01:27:54 TRACE SMAPI] Log started at 2026-11-04T05:27:54 UTC
```

## Residual Risks

- Gate 3's full 3-signal evidence (soil-health delta, crop-phase delta, SMAPI log) was **not** produced in this validation: the newest bundle has empty `artifacts/` and `attempts/`, and the SMAPI log stops after startup. That is a separate problem from the version gate (likely the runner's launch/capture path or the known event-registration issue) and is outside this bug's scope.
- `test-preflight.sh` remains red on this host for pre-existing reasons; the baseline was confirmed at HEAD (47 failures) vs post-fix (45), so it does not indicate a regression from this fix.
- The fix keeps the gate strict: older game/SMAPI versions still hard-fail (covered by the new tests).
- `gamepath.lock.json` remains a placeholder (`failClosed: true`); it is a potential later exit-19 blocker for runs that do not pass `--no-acquire`, but it was not reached in this validation.
- The fix was exercised against the real Linux installation only; Windows/macOS paths and other SMAPI/SV versions were not tested.

## Recommendation

Close this bug as verified: the version-gate false hard-fail is gone, the runner proceeds past preflight and launches SMAPI, and the regression tests lock the behavior in. Gate 3 itself is still pending — capture the 3 in-game signals and the human attestation (T148) separately.
