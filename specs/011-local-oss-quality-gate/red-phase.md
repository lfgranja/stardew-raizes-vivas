# RED Phase Evidence — Test-First Gate

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29
**Commit at capture**: `a08e41d`

## Scope

Every user story test was authored before any gate implementation. The Python package contains only `__init__.py`; `.github/workflows/quality-gate.yml` does not exist.

| Test file | Story | Test functions |
|---|---|---|
| `scripts/quality-gate/tests/test_baseline.py` | shared | 18 |
| `scripts/quality-gate/tests/test_coverage_gate.py` | US3 | 12 |
| `scripts/quality-gate/tests/test_duplication_gate.py` | US3 | 10 |
| `scripts/quality-gate/tests/test_security_gate.py` | US2 | 9 |
| `scripts/quality-gate/tests/test_workflow_contract.py` | US2 | 10 |
| `scripts/quality-gate/tests/test_workflow_build_analysis.py` | US3 | 6 |
| `scripts/quality-gate/tests/test_cli.py` | shared | 13 |
| **Total** | | **78** |

Property-based tests use Hypothesis with `max_examples=100` and `deadline=None`.

## Command 1 — pytest

```bash
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m pytest \
  scripts/quality-gate/tests -q --continue-on-collection-errors
```

Result: **29 failed, 4 errors**, exit code 1. `--continue-on-collection-errors` is required so the workflow and CLI tests also run; without it pytest stops at the first four import errors.

### Failure classification

| Count | Tests | RED reason | Intended? |
|---|---|---|---|
| 4 | `test_baseline.py`, `test_coverage_gate.py`, `test_duplication_gate.py`, `test_security_gate.py` | `ModuleNotFoundError: No module named quality_gate.baseline / coverage / duplication / security / diagnostics` | Yes — implementation not written yet |
| 13 | `test_cli.py` | `No module named quality_gate.__main__; 'quality_gate' is a package and cannot be directly executed` | Yes — CLI not written yet |
| 16 | `test_workflow_contract.py`, `test_workflow_build_analysis.py` | `AssertionError: .../.github/workflows/quality-gate.yml must exist` | Yes — workflow not written yet |

No failure is caused by a syntax error: `python -m compileall -q scripts/quality-gate` exits 0.

### Exact CLI reason

```text
E  AssertionError: .../.venv-quality-gate/bin/python: No module named quality_gate.__main__;
   'quality_gate' is a package and cannot be directly executed
```

### Exact workflow reason

```text
E  AssertionError: .../.github/workflows/quality-gate.yml must exist
```

## Command 2 — analyzer probe

```bash
bash scripts/quality-gate/analyzer-probe.sh
```

Result: exit code 1. The build succeeds because analyzers and `TreatWarningsAsErrors` are not enabled yet, which is exactly the pre-US1 state:

```text
    3 Aviso(s)
    0 Erro(s)

FAIL: the analyzer gate did not fail on the probe (build succeeded).
```

The probe file `LivingRoots/__AnalyzerGateProbe.cs` is removed by the `trap` on every exit path; `git status --porcelain` shows no probe file after the run.

## Implementation absence check

```text
$ ls scripts/quality-gate/quality_gate/
__init__.py

$ ls .github/workflows/
build_and_test.yml  code-style.yml  protected-paths.yml  security-aggregation.yml
```

`quality-gate.yml` is absent and `quality_gate/*.py` contains only the empty package marker.

## Requirements coverage asserted by the suite

| Requirement | Test(s) |
|---|---|
| FR-001 analyzer reliability gate | `test_workflow_build_analysis.py`, `analyzer-probe.sh` |
| FR-002 warnings-as-errors | probe script + US1 clean build (Phase 3) |
| FR-003 per-job game dependency | `test_static_and_sca_do_not_require_the_game`, `test_build_analysis_materialises_the_pinned_build_deps_and_fails_closed` |
| FR-004 Semgrep per PR | `test_semgrep_uses_the_pinned_cli_not_the_archived_action` |
| FR-005 CodeQL scheduled | `test_codeql_is_scheduled_only_and_buildless` |
| FR-006 Trivy SCA | `test_trivy_is_pinned_and_lets_the_python_gate_own_the_exit_code`, `test_security_gate.py` |
| FR-007 coverage | `test_coverage_gate.py`, `test_build_analysis_collects_coverage_and_runs_the_gate` |
| FR-008 C# duplication | `test_duplication_gate.py`, workflow jscpd step |
| FR-009 measured ratchets | `test_baseline.py` ratchet properties, `test_cli.py` ratchet commands |
| FR-010 SonarCloud parallel window | `test_workflow_contract.py` trigger/permissions + deferred removal task |
| FR-011 actionable diagnostics | all gate tests assert `file:line`, package/CVE/target, class, or clone pair |
| FR-012 documented suppressions | US1 remediation tasks + `docs/quality-gate.md` (Phase 6) |
| SC-001 bounded jobs | `test_build_analysis_job_exists_on_a_standard_runner` (timeout), CI evidence in Phase 7 |
| SC-002 clean build + defect fails | `analyzer-probe.sh`, Phase 3 |
| SC-003 zero HIGH/CRITICAL | `test_security_gate.py` |
| SC-004 coverage regression fails | `test_coverage_gate.py`, `test_cli.py` |
| SC-005 duplication regression fails | `test_duplication_gate.py`, `test_cli.py` |
| SC-006 location without a dashboard | diagnostic assertions in every gate test |
| SC-007 deferred removal | `sonarcloud-removal-checklist.md` (Phase 6) |

## Reviewer verdict (T018)

_Pending fresh-context review._
