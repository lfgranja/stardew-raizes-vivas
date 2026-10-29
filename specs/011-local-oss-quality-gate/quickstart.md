# Quickstart: Local OSS Quality Gate

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29

Validation guide for the quality gate. Run these locally or in CI to confirm the feature works.

## Prerequisites

- .NET SDK 10.0.x (roll-forward; the solution targets net6.0).
- Python 3.12+ and Node 20+ for the gate scripts and jscpd.
- `static`/`sca` need no Stardew Valley install and no `STARDEW_GAME_PATH`.
- `build-analysis` needs the pinned build-deps release; set `BUILDDEPS_TOKEN` and reuse the `build_and_test.yml` download/preflight commands.
- Local tools: `dotnet tool restore` (ReportGenerator), `.venv-quality-gate/bin/semgrep`, `npx jscpd@5.4.2`, `trivy`.
- From the repository root, export `PYTHONPATH=scripts/quality-gate` so every `python -m quality_gate ...` command below resolves the package.

## 1. Analyzer gate

```bash
dotnet build Stardew-LivingRoots.sln -c Release
```

Pre-remediation (right after T019) this build is expected to FAIL with ~155 analyzer diagnostics (129 tests, 26 LivingRoots). That is the measurement, not a regression. After T021–T024 it must report 0 warnings and 0 errors.

Validation: `bash scripts/quality-gate/analyzer-probe.sh` introduces a temporary violation, asserts a non-zero exit naming `__AnalyzerGateProbe.cs:<line>` and a rule ID, and removes the probe.

## 2. Semgrep (fast SAST)

```bash
.venv-quality-gate/bin/semgrep scan \
  --config p/csharp --error --severity ERROR --metrics off \
  --sarif --output semgrep.sarif LivingRoots/
```

Expected: ~10 seconds; 0 ERROR findings on the clean tree. The pinned version is `semgrep==1.182.1`; `p/csharp` currently runs 6 rules, which is the documented parity gap versus SonarCloud.

## 3. CodeQL (deep SAST, scheduled)

Run via GitHub Actions (`schedule` or `workflow_dispatch`). Uses `github/codeql-action/init@v4` with `languages: csharp` and `build-mode: none`. No `BUILDDEPS_TOKEN` and no game assemblies are required. Expected: 3–5 minutes; results in the Security tab.

## 4. Coverage baseline & ratchet

```bash
dotnet test Stardew-LivingRoots.sln -c Release \
  --collect:"XPlat Code Coverage" --results-directory ./coverage
dotnet tool restore
dotnet reportgenerator \
  -reports:'**/coverage.cobertura.xml' \
  -targetdir:coverage-report \
  -reporttypes:'JsonSummary;TextSummary'
python -m quality_gate coverage \
  --summary coverage-report/Summary.json --baseline .coverage-baseline
```

`Summary.json` exposes `summary.linecoverage` and `summary.branchcoverage` at one-decimal precision (current measurement 79.1 / 70.4), so the gate uses a 0.05 epsilon. Baseline values come from T043, never from the plan estimate.

## 5. Duplication baseline & ratchet

```bash
npx --yes jscpd@5.4.2 LivingRoots/ --format csharp \
  --min-lines 5 --min-tokens 50 --reporters json --output jscpd-report --silent
python -m quality_gate duplication \
  --report jscpd-report/jscpd-report.json --baseline .jscpd-baseline.json
```

The gate metric is total C# duplication (current measurement 0.48%, 6 clones, 64 lines). Non-C# formats are not scanned.

## 6. Trivy (SCA)

```bash
trivy fs --scanners vuln --severity HIGH,CRITICAL \
  --format json --output trivy.json LivingRoots/ LivingRoots.Tests/
python -m quality_gate security --report trivy.json
```

Expected: 0 HIGH/CRITICAL on the committed `packages.lock.json` files. If Trivy or its database is unavailable, record BLOCKED; do not claim a pass.

## 7. Full quality gate (CI)

Push a PR and confirm `.github/workflows/quality-gate.yml` runs `static`, `sca`, `build-analysis`, and the aggregate `gate`. `static`/`sca` need no game install; `build-analysis` fetches the pinned build-deps release with `BUILDDEPS_TOKEN`. The scheduled CodeQL job is separate and non-blocking.

## Success criteria check

- [ ] SC-001: `static`+`sca` ≤ 10 min; `build-analysis` ≤ 20 min; durations recorded.
- [ ] SC-002: `dotnet build` passes clean; a defect fails with file:line.
- [ ] SC-003: 0 HIGH/CRITICAL findings on the scanned scope.
- [ ] SC-004: coverage regression fails.
- [ ] SC-005: duplication regression fails.
- [ ] SC-006: file/line diagnostics available from CI.
- [ ] SC-007: **DEFERRED** — SonarCloud is not removed by this feature; the removal checklist and 2–4 week window are recorded in `sonarcloud-removal-checklist.md`.
