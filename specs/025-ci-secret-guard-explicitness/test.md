# Test Evidence — 025-ci-secret-guard-explicitness

- **Date**: 2026-11-18
- **Worktree**: `.specify/queue/worktrees/ci-secret-guard` @ `origin/dev` `af12158`
- **Gate 3 real-SMAPI**: **N/A** — no runnable C#, game state, asset or locale change. Not claimed.

## RED → GREEN (TDD evidence, constitution Principle IV)

Before the workflow guard (after the contract test was written):

```
PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests/test_ci_secret_guards.py -q
Pytest: 4 passed, 1 failed in 3.09s
E AssertionError: unguarded credential consumers: build_and_test.yml:build-test:Fetch game assemblies
```

After adding the guard to `.github/workflows/build_and_test.yml`:

```
PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests/test_ci_secret_guards.py -q
Pytest: 5 passed in 2.64s

grep -rn 'BUILDDEPS_TOKEN is not set' .github/workflows/
  build_and_test.yml:79   ::error::
  code-style.yml:49       ::error::
  quality-gate.yml:219    ::error::
  verify-scenario.yml:58  ::warning::  (intentional best-effort, excluded)
```

## Full Python quality-gate suite — pre-existing debt comparison

Export of `origin/dev` baseline vs this branch (same command):

| Checkout | Result |
|----------|--------|
| `origin/dev` clean export (`/tmp/opencode/base-check`) | 12 failed / 161 passed |
| feature worktree | 11 failed / 167 passed |
| diff | **0 new failures**; 5 new tests pass; 1 pre-existing failure resolved (`test_trivy_targets_the_committed_lock_files`); 11 pre-existing failures remain |

The remaining failures are in untouched files (`test_analyzer_config.py`, `test_baseline.py`, `test_workflow_build_analysis.py`, `test_workflow_contract.py`) and reflect stale version pins/suppression-policy expectations. The hosted `Quality Gate` workflow does not invoke pytest (it runs the `quality_gate` CLI).

## .NET gates (worktree)

```
dotnet format --verify-no-changes                  exit 0
dotnet restore --locked-mode                       exit 0
dotnet build -c Release --no-restore -m:1          exit 0
dotnet test -c Release --no-build (first run)      exit 1 — 1 FsCheck property failure (flaky seed)
  isolated reruns of that property: 1 fail, 2 pass
dotnet test -c Release --no-build (rerun)          exit 0 — 1999 passed / 0 failed / 4 skipped (2003)
./scripts/verify-scenario.sh --check-docs          exit 0
./scripts/check-protected-paths.sh                 exit 0
```

## SDD gates

```
python3 scripts/ci/sdd-gate.py   → sdd-gate: PASS (0 errors, 28 warnings)
```

The only 025-specific warning (unchecked tasks) disappeared after all 12 tasks were marked `[X]`; before that it was PASS with 29 warnings, and before R4/R7 artifacts existed it correctly reported 2 errors.

## [NV] boundary

Remote step-level causes for PRs #790–#796 were not re-read from remote logs. No remote status is promoted to verified here.
