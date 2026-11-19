# Quickstart — validate CI secret-guard explicitness

Prerequisites: Python 3.12 with the quality-gate venv, PyYAML available, and a checkout of this feature branch. No game assemblies are needed — no runnable C# changes are part of this feature.

## 1. Red state (before the workflow fix)

```
PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests/test_ci_secret_guards.py -q
```

Expected: FAIL, naming `.github/workflows/build_and_test.yml` / job `build-test` / step `Fetch game assemblies` as an unguarded credential consumer. This is the red-for-the-right-reason evidence required by constitution Principle IV.

## 2. Green state (after the workflow fix)

Same command. Expected: PASS with the current workflow set, including the intentional exclusion of the best-effort verification job.

## 3. Fixture regression check

The test module includes a synthetic unguarded consumer; the full-module run must show both the real-set pass and the fixture failure assertion. Expected: all tests pass as a module because the fixture is asserted to be detected.

## 4. Repository gates

```
python3 scripts/ci/sdd-gate.py
GamePath=/home/luis/stardew-assemblies ./scripts/release-gate.sh
```

Expected: `sdd-gate: PASS (0 errors, ...)` and `release-gate.sh` → `ALL STEPS PASSED`. Gate 3 real-SMAPI verification is N/A for this feature (no runnable code) and must not be claimed as run.

## 5. Manual spot check

```
grep -n 'BUILDDEPS_TOKEN is not set' .github/workflows/{build_and_test,code-style,quality-gate}.yml
```

Expected: three hits after the change (previously two). The `verify-scenario.yml` warning and `release.yml` remain untouched.

## Trust boundary

Remote check status and root causes of dependency-update PRs are [NV] in this cycle unless re-checked live with `gh`; no remote claim is promoted to verified by these local commands.
