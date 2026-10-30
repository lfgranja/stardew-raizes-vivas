# RED Phase Evidence — Test-First Gate

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29
**Commit at capture**: `1fd835b` (refreshed evidence after the sixth-review remediation; the original capture was `38e7fa7`)

## Scope

Every user story test was authored before any gate implementation. The Python package contains only `__init__.py`; `.github/workflows/quality-gate.yml` does not exist.

| Test file | Story | Test functions |
|---|---|---|
| `scripts/quality-gate/tests/test_baseline.py` | shared | 18 |
| `scripts/quality-gate/tests/test_coverage_gate.py` | US3 | 15 |
| `scripts/quality-gate/tests/test_duplication_gate.py` | US3 | 11 |
| `scripts/quality-gate/tests/test_security_gate.py` | US2 | 10 |
| `scripts/quality-gate/tests/test_analyzer_config.py` | US1 | 26 |
| `scripts/quality-gate/tests/test_workflow_contract.py` | US2 | 16 |
| `scripts/quality-gate/tests/test_workflow_build_analysis.py` | US3 | 10 |
| `scripts/quality-gate/tests/test_cli.py` | shared | 15 |
| **Total** | | **121** |

Property-based tests use Hypothesis with `max_examples=100` and `deadline=None`.

## Command 1 — pytest

```bash
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m pytest \
  scripts/quality-gate/tests -q --continue-on-collection-errors
```

Result: **43 failed, 31 passed, 4 collection errors**, exit code 1. `--continue-on-collection-errors` is required so the workflow and CLI tests also run; without it pytest stops at the first four import errors. The four uncollectable modules contain 54 test functions (18 + 15 + 11 + 10) that are not counted among the 74 executed cases.

### Failure classification

| Count | Tests | RED reason | Intended? |
|---|---|---|---|
| 4 modules (54 functions) | `test_baseline.py`, `test_coverage_gate.py`, `test_duplication_gate.py`, `test_security_gate.py` | Exact per-module errors below | Yes — implementation not written yet |
| 15 | `test_cli.py` | `No module named quality_gate.__main__; 'quality_gate' is a package and cannot be directly executed` | Yes — CLI not written yet |
| 15 | `test_workflow_contract.py` | 14 × `AssertionError: .../.github/workflows/quality-gate.yml must exist`; 1 × `LivingRoots/packages.lock.json must exist` | Yes — workflow and lock files not written yet |
| 9 | `test_workflow_build_analysis.py` | `AssertionError: .../.github/workflows/quality-gate.yml must exist` (no lock-file assertion in this file) | Yes — workflow not written yet |
| 4 | `test_analyzer_config.py` | Empty `Directory.Build.props`, no analyzer package references, 4 unjustified suppressions | Yes — US1 not implemented yet |

The 31 passing cases are policy checks that do not require the gate: project files exist, no global `NoWarn` is present, the probe names both projects, the suppression/editorconfig/scanner-policy fixtures behave correctly, the ReportGenerator pin is already committed, and the SARIF fork-guard evaluator has its own truth table. They are not evidence that the gate works.

### Exact collection errors

```text
test_baseline.py:10        ImportError: cannot import name 'baseline' from 'quality_gate'
test_coverage_gate.py:11   ModuleNotFoundError: No module named 'quality_gate.baseline'
test_duplication_gate.py:11 ModuleNotFoundError: No module named 'quality_gate.baseline'
test_security_gate.py:9    ModuleNotFoundError: No module named 'quality_gate.diagnostics'
```

These are the intended missing-implementation imports, not malformed test code; `python -m compileall -q scripts/quality-gate` exits 0 and the same modules import cleanly once T037–T040 exist.

No failure is caused by a syntax error: `python -m compileall -q scripts/quality-gate` exits 0.

### Exact CLI reason

```text
E  AssertionError: .../.venv-quality-gate/bin/python: No module named quality_gate.__main__;
   'quality_gate' is a package and cannot be directly executed
```

### Exact workflow reason

```text
E  AssertionError: .../.github/workflows/quality-gate.yml must exist
E  AssertionError: LivingRoots/packages.lock.json must exist
```

### Exact analyzer-configuration reason

```text
E  AssertionError: assert None == 'latest'
E  AssertionError: .../LivingRoots/LivingRoots.csproj must reference SonarAnalyzer.CSharp
E  AssertionError: .../LivingRoots.Tests/LivingRoots.Tests.csproj must reference SonarAnalyzer.CSharp
E  AssertionError: /home/luis/development/stardew-raizes-vivas/LivingRoots.Tests/BinPlacementTests.cs:242:
   #pragma warning disable AvoidNetField has no justified reason naming the rule
```

## Command 2 — analyzer probe

```bash
bash scripts/quality-gate/analyzer-probe.sh
```

Result: exit code 1. Both builds succeed because analyzers, `TreatWarningsAsErrors`, and lock files are not enabled yet, which is exactly the pre-US1 state. The probe's `CS0219` fires in both projects, alongside the pre-existing warnings that US1 must remediate:

```text
--- probing LivingRoots/LivingRoots.csproj ---
LivingRoots/__AnalyzerGateProbe.cs(8,17): warning CS0219: A variável "unused" é atribuída, mas seu valor nunca é usado
FAIL: the analyzer gate did not fail on the probe in LivingRoots/LivingRoots.csproj (build succeeded).
--- probing LivingRoots.Tests/LivingRoots.Tests.csproj ---
LivingRoots.Tests/__AnalyzerGateProbe.cs(8,17): warning CS0219: A variável "unused" é atribuída, mas seu valor nunca é usado
FAIL: the analyzer gate did not fail on the probe in LivingRoots.Tests/LivingRoots.Tests.csproj (build succeeded).
```

The pre-existing warnings are 2 in production (`CS8602` at `CompostingBinService.cs:122`, `CS0162` at `VisualizationService.cs:604`) and 15 `CS8602` in `SoilHealthPersistenceInvariantsTests.cs`; the probe adds one `CS0219` per project. They are the US1 triage input, not gate evidence.

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
| FR-004 Semgrep per PR | `test_semgrep_uses_the_pinned_cli_not_the_archived_action`, `test_pull_requests_and_pushes_are_not_path_filtered` | Semgrep's own `--error` output carries `file:line: rule`; T061 asserts a clean scan under 120 seconds and T066 records the step duration |
| FR-005 CodeQL scheduled | `test_codeql_is_scheduled_only_and_buildless` | |
| FR-006 Trivy SCA | `test_trivy_is_pinned_and_lets_the_python_gate_own_the_exit_code`, `test_trivy_targets_the_committed_lock_files`, `test_security_gate.py` | |
| FR-007 coverage | `test_coverage_gate.py`, `test_build_analysis_collects_coverage_and_runs_the_gate`, `test_reportgenerator_and_coverlet_are_pinned` | |
| FR-008 C# duplication | `test_duplication_gate.py`, `test_jscpd_is_pinned_csharp_only_and_gated` | |
| FR-009 measured ratchets | `test_baseline.py` ratchet properties, `test_cli.py` improve/equal/weaken cases | |
| FR-010 SonarCloud parallel window | T053 (documented 2–4 week parallel-run/removal timeline), T069 (records rollout start and earliest removal date) | Operational rollout requirement, not a code-level test; SC-007 removal is deferred |
| FR-011 actionable diagnostics | exact `::error file=...,line=...` and `::error title=...` assertions in `test_coverage_gate.py`, `test_duplication_gate.py`, `test_cli.py`; SCA asserts package/CVE/target | Semgrep findings are emitted natively by the CLI, not re-parsed |
| FR-012 documented suppressions | `test_analyzer_config.py::test_every_repository_suppression_is_documented` plus positive/negative fixtures | Red until T022 |
| SC-001 bounded jobs | `test_jobs_run_on_a_standard_ubuntu_runner_with_bounded_time`, `test_build_analysis_job_exists_on_a_standard_runner` | Duration evidence is deferred to T066; the test asserts only the timeouts |
| SC-002 clean build + defect fails | T024/T057 clean build (`dotnet build ... -c Release` with 0 warnings/errors); T025/T059 probe (`analyzer-probe.sh` exits 0 after both probe builds fail) | Both halves are required |
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

## T018 second review resolution

The second fresh-context review found 18 gaps. Each is resolved as follows.

| # | Gap | Resolution |
|---|---|---|
| 1 | Phase 2 left open | T017 uses the required `--continue-on-collection-errors` flag and records the current measured counts above; T018 must append `Reviewer verdict: PASS — <date>` before T019 |
| 2 | US2 test reaches into `build-analysis` | `test_workflow_contract.py` asserts only `static`/`sca`/`codeql`/`gate` and never names `build-analysis`; `test_workflow_build_analysis.py` owns it |
| 3 | T033 could not pass before US3 | US2 now closes with only its own jobs; the extended gate is asserted by US3 |
| 4 | T041/T042 ran before their inputs | Chain is T043 → T044 → T042 → T041 → T045 → T046 |
| 5 | Lock files built before analyzers | T028 depends on T019 and T024 |
| 6 | Trivy fixture version | Action SHA `ed142fd0…` (v0.36.0) with CLI `v0.76.1`, asserted separately |
| 7 | Semgrep findings did not reach the log | A second `if: always()` text step runs without `--sarif`/`--output` |
| 8 | CodeQL produced no annotations | `output: codeql-results.sarif` plus a SARIF-to-`::error file,line` step, both asserted |
| 9 | Token preflight was grepped, not executed | `test_build_deps_preflight_exits_one_without_a_token` runs the step with `GH_TOKEN=""` and a poisoned `gh` |
| 10 | Coverage fixture accepted empty assemblies | `coverage.assemblies` must be non-empty; the load fixture now contains an assembly and class |
| 11 | jscpd totals were optional in the fixture | `statistics.total.duplicates`/`.lines` and both clone `start` fields are asserted |
| 12 | Root `.editorconfig` could disable rules with a comment | Root blanket disables are rejected even with a comment; only folder/file-scoped overrides are allowed |
| 13 | Scanner suppressions were untested | `nosemgrep`, `.semgrepignore`, and `.trivyignore` fixtures must fail the policy |
| 14 | Ratchet semantics were ambiguous | Improve = write/exit 0; equal = no-op/exit 0; weaken = exit 2 with a byte-identical file |
| 15 | Atomic-write evidence was narrow | Failure leaves the target byte-identical and the directory with no extra file |
| 16 | FR-004 had no owner | The `< 120s` Semgrep assertion is explicitly deferred to T061/T066 |
| 17 | FR-009 baseline chain was implicit | T043 reports are compared in T045 and T063; no hand-typed estimates |
| 18 | T071 audit scope was vague | Implementation commits only (`f307b1f..HEAD`, excluding `specs/**` and `.specify/**`) |

## T018 third review resolution

The third fresh-context review found 7 blocking gaps. Each is resolved as follows.

| # | Gap | Resolution |
|---|---|---|
| 1 | Unary `!` broke the SARIF guard evaluator and was misrecorded as a syntax error | `evaluate_sarif_guard` rewrites unary `!` (but not `!=`) to `not`; the evaluator truth table passes and the failure classification no longer claims a syntax error |
| 2 | `test_workflow_contract.py` still contained the literal `build-analysis` | The comment now says "the US3 job"; grep for `build-analysis` returns nothing |
| 3 | Ratchet-weaken tests did not assert a byte-identical baseline | Both tests capture `read_bytes()` and the directory listing, then assert equality after exit 2 |
| 4 | No empty-`assemblies` negative and invalid-value test could pass for the wrong reason | `{"assemblies": []}` is a malformed case; the invalid-value payload includes a valid assembly/class |
| 5 | jscpd invalid-percentage test could pass because required totals were missing | The payload supplies `duplicates`/`lines`; new malformed cases omit only `duplicates` or only `lines`, and a clone missing `firstFile.start` |
| 6 | Pragma regex skipped trailing-comment suppressions | The regex accepts an optional trailing `//` or `/*...*/`; same-line justification is now reachable, with positive and empty-comment negatives |
| 7 | Gate was not tested to fail closed on `build-analysis` | `test_gate_fails_closed_when_build_analysis_does_not_succeed` executes the rendered script for `failure`, `skipped`, and `cancelled` |

## T018 fourth review resolution

The fourth review reproduced the suite and found 8 documentation, traceability, and coverage gaps. Each is resolved as follows.

| # | Gap | Resolution |
|---|---|---|
| 1 | Failure classification misattributed the lock-file failure | `test_workflow_contract.py` is now 14 missing-workflow + 1 missing lock file; `test_workflow_build_analysis.py` is 9 missing-workflow with no lock-file assertion |
| 2 | Analyzer reason block listed a workflow failure and omitted a project assertion | The block now shows both csproj SonarAnalyzer assertions; the lock-file failure moved to the workflow block |
| 3 | Probe warning counts were wrong | 2 pre-existing production warnings and 15 pre-existing test warnings, plus one probe `CS0219` per project |
| 4 | Counts and capture commit were stale | Total is 121 functions / 43 failed / 31 passed / 4 errors (see above); the capture commit is refreshed after this remediation |
| 5 | The second Semgrep text step was unasserted | `test_semgrep_prints_findings_with_file_and_line` requires exactly one SARIF step and one `if: always()` text step with no `--output` |
| 6 | FR-010 traceability was vague | The row names T053 and T069 and states that FR-010 is operational, not a code-level test |
| 7 | Parallel tasks lacked `[P]` | T010–T013, T038, and T039 are marked `[P]` in line with the declared waves |
| 8 | T041 was not self-contained | T041 now names the repository, tag, asset, `GamePath`, unzip/move steps, and required-assembly check |

## T018 fifth review resolution

The fifth fresh-context review reproduced the suite and found 6 blocking gaps. Each is resolved as follows.

| # | Gap | Resolution |
|---|---|---|
| 1 | RED failure attribution was not exact, and T017 forbade the very import errors it intends | The exact per-module `ImportError`/`ModuleNotFoundError` block is recorded above; T017 now forbids only an unintended syntax error, test bug, or wrong-reason pass |
| 2 | The probe contract was inverted in T025/T059 and SC-002 | T016/T025/T059/Phase 3 now state that each internal probe build must exit non-zero while the script itself exits 0 with `PASS` (and exits 1 pre-US1); SC-002 cites both T024/T057 clean build and T025/T059 probe |
| 3 | Suppression policy had holes for bare pragma, unrelated above-comments, and rule-only editorconfig comments | The scanner matches a bare `#pragma warning disable`; `comment_justifies` requires every rule ID plus a reason of at least 3 characters; negatives cover all three cases |
| 4 | The exact `GH_TOKEN` mapping was not asserted | The test asserts `step_env.get("GH_TOKEN") == "${{ secrets.BUILDDEPS_TOKEN }}"` |
| 5 | T041 was not self-contained and omitted prerequisites | T041 names checkout, setup-dotnet, the build-deps env/commands, all six required assemblies, and requires T028, T029–T031, T040, T042, and T044; contract rows 12a/12b/13a and the workflow test assert checkout/setup-dotnet |
| 6 | The coverage workflow invocation was under-asserted | The test requires both `--summary coverage-report/Summary.json` and `--baseline .coverage-baseline` |

## T018 sixth review resolution

The sixth fresh-context review reproduced the suite and found 10 gaps. Each is resolved as follows.

| # | Gap | Resolution |
|---|---|---|
| 1 | The six-assembly preflight was untested | `test_build_analysis_materialises_the_pinned_build_deps_and_fails_closed` asserts `unzip -o`, `mv -f`, `smapi-internal`, all six assembly names, and the `::error`/`exit 1` preflight |
| 2 | Gate/CodeQL steps could use `continue-on-error` | The blocking markers now include `needs.` and `codeql-action`, and the job-level `continue-on-error` is rejected for `static`/`sca`/`codeql`/`gate` |
| 3 | `pull_request` was not enforced as all branches/types | The trigger test rejects `branches`, `branches-ignore`, and `types` on `pull_request` |
| 4 | T059 used `__AnalyzerGateProbe.cs:<line>` | T059 now requires `__AnalyzerGateProbe.cs(<line>,<column>)` |
| 5 | The quickstart inverted the probe exit semantics | It now states that each internal build fails while the script exits 0 and prints `PASS` |
| 6 | T042 lacked its prerequisites | T042 is explicitly after T029, T040, and T044 |
| 7 | Action and SDK pins were not exact | The test requires `actions/checkout@v4`, `actions/setup-dotnet@v4`, and `dotnet-version: 10.0.x` |
| 8 | Step order was unasserted | Tests assert requirements install before Semgrep, CodeQL init before analyze before annotations, ReportGenerator before the coverage gate, and jscpd before the duplication gate |
| 9 | The probe test was presence-only | `test_the_probe_builds_both_projects` runs the real script against a stub `dotnet`, proving two build invocations with both project paths and cleanup |
| 10 | The capture commit lagged HEAD | The capture commit is `1fd835b`; the test/probe tree is identical to the reviewed run |

## Reviewer verdict (T018)

_Pending re-review after the sixth gap remediation._
