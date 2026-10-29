# RED Phase Evidence — Test-First Gate

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29
**Commit at capture**: pending (working tree after the T018 gap remediation)

## Scope

Every user story test was authored before any gate implementation. The Python package contains only `__init__.py`; `.github/workflows/quality-gate.yml` does not exist.

| Test file | Story | Test functions |
|---|---|---|
| `scripts/quality-gate/tests/test_baseline.py` | shared | 18 |
| `scripts/quality-gate/tests/test_coverage_gate.py` | US3 | 15 |
| `scripts/quality-gate/tests/test_duplication_gate.py` | US3 | 11 |
| `scripts/quality-gate/tests/test_security_gate.py` | US2 | 9 |
| `scripts/quality-gate/tests/test_analyzer_config.py` | US1 | 10 |
| `scripts/quality-gate/tests/test_workflow_contract.py` | US2 | 14 |
| `scripts/quality-gate/tests/test_workflow_build_analysis.py` | US3 | 8 |
| `scripts/quality-gate/tests/test_cli.py` | shared | 15 |
| **Total** | | **100** |

Property-based tests use Hypothesis with `max_examples=100` and `deadline=None`.

## Command 1 — pytest

```bash
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m pytest \
  scripts/quality-gate/tests -q --continue-on-collection-errors
```

Result: **41 failed, 11 passed, 4 errors**, exit code 1. `--continue-on-collection-errors` is required so the workflow and CLI tests also run; without it pytest stops at the first four import errors.

### Failure classification

| Count | Tests | RED reason | Intended? |
|---|---|---|---|
| 4 | `test_baseline.py`, `test_coverage_gate.py`, `test_duplication_gate.py`, `test_security_gate.py` | `ModuleNotFoundError: No module named quality_gate.baseline / coverage / duplication / security / diagnostics` | Yes — implementation not written yet |
| 15 | `test_cli.py` | `No module named quality_gate.__main__; 'quality_gate' is a package and cannot be directly executed` | Yes — CLI not written yet |
| 14 | `test_workflow_contract.py` | `AssertionError: .../.github/workflows/quality-gate.yml must exist` | Yes — workflow not written yet |
| 7 | `test_workflow_build_analysis.py` | `AssertionError: .../.github/workflows/quality-gate.yml must exist` (1 lock-file assertion) | Yes — workflow and lock files not written yet |
| 5 | `test_analyzer_config.py` | Empty `Directory.Build.props`, no analyzer package references, 4 unjustified suppressions | Yes — US1 not implemented yet |

The 11 passing tests are policy checks that do not require the gate: project files exist, no `NoWarn` is present, the probe names both projects, and the four synthetic suppression fixtures prove the policy scanner accepts justified suppressions and rejects unjustified ones. They are not evidence that the gate works.

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

### Exact analyzer-configuration reason

```text
E  AssertionError: assert None == 'latest'
E  AssertionError: assert 'SonarAnalyzer.CSharp' in {'Pathoschild.Stardew.ModBuildConfig': ...}
E  AssertionError: LivingRoots/packages.lock.json must exist
E  AssertionError: /home/luis/development/stardew-raizes-vivas/LivingRoots.Tests/BinPlacementTests.cs:242:
   #pragma warning disable AvoidNetField has no justification comment
```

## Command 2 — analyzer probe

```bash
bash scripts/quality-gate/analyzer-probe.sh
```

Result: exit code 1. Both builds succeed because analyzers and `TreatWarningsAsErrors` are not enabled yet, which is exactly the pre-US1 state:

```text
--- probing LivingRoots/LivingRoots.csproj ---
FAIL: the analyzer gate did not fail on the probe in LivingRoots/LivingRoots.csproj (build succeeded).
--- probing LivingRoots.Tests/LivingRoots.Tests.csproj ---
FAIL: the analyzer gate did not fail on the probe in LivingRoots.Tests/LivingRoots.Tests.csproj (build succeeded).
```

The probe files `LivingRoots/__AnalyzerGateProbe.cs` and `LivingRoots.Tests/__AnalyzerGateProbe.cs` are removed by the `trap` on every exit path; `git status --porcelain` shows no probe file after the run.

## Implementation absence check

```text
$ ls scripts/quality-gate/quality_gate/
__init__.py

$ ls .github/workflows/
build_and_test.yml  code-style.yml  protected-paths.yml  security-aggregation.yml
```

`quality-gate.yml` is absent and `quality_gate/*.py` contains only the empty package marker. `LivingRoots/packages.lock.json` and `LivingRoots.Tests/packages.lock.json` do not exist yet.

## Requirements coverage asserted by the suite

| Requirement | Test(s) | Note |
|---|---|---|
| FR-001 analyzer reliability gate | `test_analyzer_config.py` (both projects, package pins), `test_workflow_build_analysis.py`, `analyzer-probe.sh` | Probe builds both projects |
| FR-002 warnings-as-errors | `test_analyzer_config.py::test_build_props_enables_the_required_analyzer_settings`, probe script + clean build (Phase 3) | |
| FR-003 per-job game dependency | `test_static_and_sca_do_not_require_the_game`, `test_build_analysis_materialises_the_pinned_build_deps_and_fails_closed` | |
| FR-004 Semgrep per PR | `test_semgrep_uses_the_pinned_cli_not_the_archived_action`, `test_pull_requests_and_pushes_are_not_path_filtered` | Semgrep's own `--error` output carries `file:line: rule`; T061 asserts it |
| FR-005 CodeQL scheduled | `test_codeql_is_scheduled_only_and_buildless` | |
| FR-006 Trivy SCA | `test_trivy_is_pinned_and_lets_the_python_gate_own_the_exit_code`, `test_trivy_targets_the_committed_lock_files`, `test_security_gate.py` | |
| FR-007 coverage | `test_coverage_gate.py`, `test_build_analysis_collects_coverage_and_runs_the_gate`, `test_reportgenerator_and_coverlet_are_pinned` | |
| FR-008 C# duplication | `test_duplication_gate.py`, `test_jscpd_is_pinned_csharp_only_and_gated` | |
| FR-009 measured ratchets | `test_baseline.py` ratchet properties, `test_cli.py` improve/equal/weaken cases | |
| FR-010 SonarCloud parallel window | `test_workflow_contract.py` trigger/permissions + deferred removal task | |
| FR-011 actionable diagnostics | exact `::error file=...,line=...` and `::error title=...` assertions in `test_coverage_gate.py`, `test_duplication_gate.py`, `test_cli.py`; SCA asserts package/CVE/target | Semgrep findings are emitted natively by the CLI, not re-parsed |
| FR-012 documented suppressions | `test_analyzer_config.py::test_every_repository_suppression_is_documented` plus positive/negative fixtures | Red until T022 |
| SC-001 bounded jobs | `test_jobs_run_on_a_standard_ubuntu_runner_with_bounded_time`, `test_build_analysis_job_exists_on_a_standard_runner` | Duration evidence is deferred to T066; the test asserts only the timeouts |
| SC-002 clean build + defect fails | `analyzer-probe.sh`, Phase 3 | |
| SC-003 zero HIGH/CRITICAL | `test_security_gate.py` | |
| SC-004 coverage regression fails | `test_coverage_gate.py`, `test_cli.py` | |
| SC-005 duplication regression fails | `test_duplication_gate.py`, `test_cli.py` | |
| SC-006 location without a dashboard | diagnostic assertions in every gate test | |
| SC-007 deferred removal | `sonarcloud-removal-checklist.md` (Phase 6) | DEFERRED: SonarCloud is not removed by this feature |

## T018 review resolution

The first fresh-context review returned **FAIL** with 17 gaps. Each is resolved as follows.

| # | Gap | Resolution |
|---|---|---|
| 1 | Test-project analyzer configuration never asserted | `test_analyzer_config.py` parses `Directory.Build.props` and both csproj files; the probe builds both projects |
| 2 | No jscpd workflow test | `test_workflow_build_analysis.py::test_jscpd_is_pinned_csharp_only_and_gated` |
| 3 | Suppression policy untested | `test_analyzer_config.py` scans the repository and has positive/negative fixtures |
| 4 | Trivy scan target untested | `test_workflow_contract.py` asserts `scan-ref`, `output`, `skip-dirs`, and lock-file existence/tracking |
| 5 | Trivy SHA-vs-version contradiction | Commit-SHA pin `ed142fd0673e97e23eac54620cfb913e5ce36c25`, version `0.36.0` in the step name; `tasks.md` and the contract updated |
| 6 | ReportGenerator/Coverlet pins untested | `test_workflow_build_analysis.py::test_reportgenerator_and_coverlet_are_pinned` |
| 7 | Annotation format and Semgrep SARIF under-tested | Exact annotation assertions; `--sarif`/`--output semgrep.sarif` assertions; Semgrep's native `file:line` output is verified by T061 |
| 8 | Timeouts, path filters, durations incomplete | Every job has a bounded timeout; PR/push path filters are forbidden; duration evidence deferred to T066 |
| 9 | Fork SARIF guard vacuous | `evaluate_sarif_guard` truth table: fork PR skip, same-repo PR upload, push/schedule upload |
| 10 | Aggregate gate fail-closed weak | `run_gate` executes the rendered gate script for `success`, `failure`, `cancelled`, and `skipped` |
| 11 | Build-analysis token/asset test conflicts with env pattern | Asserts `GH_TOKEN: ${{ secrets.BUILDDEPS_TOKEN }}`, the repo/tag/asset values, `gh release download`, `GamePath`, and the fail-closed message |
| 12 | `continue-on-error` conflicts with non-blocking SARIF upload | Prohibition scoped to blocking steps only |
| 13 | Coverage epsilon not pinned | `assert COVERAGE_EPSILON == 0.05`, just-inside/just-outside boundary cases, above-floor property |
| 14 | Security parser property vacuous | Compares count and every field against the generated input |
| 15 | Wrong ReportGenerator class schema | Fixtures use `coverage` and no `filename`; `test_cli.py` updated |
| 16 | Ratchet exit-code contradiction | Documented policy: improvement `0`, equal no-op `0`, weakening `2` with the file unchanged; CLI tests cover all three |
| 17 | Precision, `os.replace`, permissions, CodeQL | Coverage precision `0.1`/tolerance `0.05`; duplication `0.01`/`0.005`; `os.replace` mandated; exact per-job permissions; CodeQL asserts no `push` and no `pull_request` |

## Reviewer verdict (T018)

_Pending re-review after the gap remediation._
