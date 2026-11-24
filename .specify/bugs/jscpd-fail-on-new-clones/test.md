# Bug Verification: jscpd `--fail-on-new-clones` aborted the static job

- **Slug**: jscpd-fail-on-new-clones
- **Tested**: 2026-11-01T01:10:00Z
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The original symptom no longer reproduces: the fixed workflow command exits 0 and writes `jscpd-report/jscpd-report.json`, the duplication gate consumes it successfully, and `Quality Gate / static` passed in CI on PR #546. The unfixed command form still fails with the exact original error, confirming the flag was the trigger. No regressions attributable to this fix were found; the four failing quality-gate tests and the red `build-analysis`/`gate` jobs reproduce on `origin/dev` and belong to separate fronts.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | Exact `run:` line from `.github/workflows/quality-gate.yml` executed in a detached worktree: `npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 --reporters json --output jscpd-report --silent` | pass | Exit 0, `Found 6 exact clones with 67(0.49%) duplicated lines in 54 files`, report written |
| Reproduction probe (pre-fix) | Same command plus `--fail-on-new-clones` | pass (expected failure) | Exit 1, `Error: --fail-on-new-clones requires --baseline <file>, --baseline-from-ref <ref> or --changed` — confirms the flag, not the scan, caused the abort |
| Duplication gate | `PYTHONPATH=scripts/quality-gate python -m quality_gate duplication --report jscpd-report/jscpd-report.json --baseline .jscpd-baseline.json` | pass | Exit 0 with the ratcheted baseline |
| New / updated tests | `pytest scripts/quality-gate/tests/test_workflow_build_analysis.py::test_jscpd_is_pinned_csharp_only_and_gated scripts/quality-gate/tests/test_duplication_gate.py -q` | pass | `22 passed` |
| Regression suite | `pytest scripts/quality-gate/tests -q` | pass with pre-existing failures | `167 passed, 4 failed`; all four failures reproduce unchanged on `origin/dev` in a clean worktree |
| CI — static job | Run `41258811867`, job `Quality Gate / static` (id `124707610130`) | pass | jscpd and the duplication gate both green |
| CI — aggregate gate | Run `41258811867`, job `Quality Gate / gate` | fail (unrelated) | `build-analysis` fails at `Build`; aggregation only reacts to it |
| CI — Build and Test | Run `41258811865` | fail (unrelated) | FsCheck API compile errors (`Arb`, `Prop`), addressed by PR #537 |
| Lint / type-check | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | skipped | No C# files changed; the solution has pre-existing formatting failures in untouched files (PR #544 / #545 territory) |

## Output Excerpts

Post-fix jscpd command:

```
Duplications detection: Found 6 exact clones with 67(0.49%) duplicated lines in 54 (1 formats) files.
JSON report saved to jscpd-report/jscpd-report.json
fixed_cmd_exit=0
```

Pre-fix command form:

```
Error: --fail-on-new-clones requires --baseline <file>, --baseline-from-ref <ref> or --changed
old_cmd_exit=1
```

Quality-gate test suite:

```
4 failed, 167 passed in 13.47s
```

CI job summary for PR #546:

```
✓ Quality Gate / static in 44s (ID 124707610130)
X Quality Gate / build-analysis in 39s (ID 124707610023)
   X Build
   - Test and coverage
```

CI static job log:

```
Run npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 --reporters json --output jscpd-report --silent
Duplications detection: Found 6 exact clones with 67(0.49%) duplicated lines in 54 (1 formats) files.
JSON report saved to jscpd-report/jscpd-report.json
Run python -m quality_gate duplication --report jscpd-report/jscpd-report.json --baseline .jscpd-baseline.json
```

## Residual Risks

- **Q20 is deferred, not implemented**: jscpd no longer fails on individual new clones; only the aggregate ceiling is enforced. Re-enabling requires a committed fingerprint baseline at a distinct path.
- **Baseline weakening**: the ceiling moved from 0.42% to 0.5% (measured 0.49%). Q19's written weakening procedure still needs its approval/expiry record if the repository applies it to ratchets.
- **The aggregate `Quality Gate / gate` is still red on `dev`** for unrelated reasons: `build-analysis` fails at `Build` (FsCheck `Arb`/`Prop`, PR #537) and the coverage gate fails without the exclusions (PR #543). Merging this fix alone does not make `dev` green.
- **The invariant test is not executed by CI**: `Quality Gate` does not run the Python test suite; the new guard runs locally and in any workflow that invokes `pytest scripts/quality-gate/tests`.
- CI was exercised on the PR branch, not on a post-merge `dev` run.

## Recommendation

Close the bug — the reported failure is verified fixed: the reproduction no longer occurs locally and `Quality Gate / static` is green on PR #546. Keep the aggregate-gate failures tracked separately (`build-analysis`/FsCheck in PR #537, coverage exclusions in PR #543, formatting in PR #544), and schedule the Q20 fingerprint-baseline follow-up if per-clone detection is required.
