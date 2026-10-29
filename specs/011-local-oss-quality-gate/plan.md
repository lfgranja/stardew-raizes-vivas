# Implementation Plan: Local OSS Quality Gate (replacing SonarCloud assessments)

**Branch**: `011-local-oss-quality-gate` | **Date**: 2026-10-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-local-oss-quality-gate/spec.md`

**Note**: This plan was filled in by the `/speckit-plan` command.

## Summary

Replace the SonarCloud code-quality gate (opaque reliability rating, cloud-dependent) with a deterministic, local, open-source quality gate for the .NET 6 Stardew Valley mod. Roslyn analyzers (`SonarAnalyzer.CSharp` 10.35.0.4138 + `Microsoft.CodeAnalysis.NetAnalyzers` 10.0.403) with `TreatWarningsAsErrors` run in a `build-analysis` job that materialises the repository's pinned `gamepath.zip` build-deps release via `BUILDDEPS_TOKEN`. A separate `static` job runs Semgrep CE 1.182.1, Trivy 0.76.1, and jscpd 5.4.2 with no game dependency. CodeQL runs scheduled with `build-mode: none`. Coverage uses Coverlet + ReportGenerator 5.5.12. Thresholds are measured baselines that ratchet only in the improving direction. SonarCloud stays in parallel for 2–4 weeks of green runs before removal.

## Technical Context

**Language/Version**: C# 11 / .NET 6 (mod) + .NET 6 test project; CI YAML.

**Primary Dependencies**: `SonarAnalyzer.CSharp` 10.35.0.4138, `Microsoft.CodeAnalysis.NetAnalyzers` 10.0.403, Semgrep CE CLI 1.182.1, CodeQL Action v4 (`build-mode: none`), Trivy CLI 0.76.1 / `aquasecurity/trivy-action` v0.36.0, Coverlet 6.0.0 (already referenced), ReportGenerator 5.5.12 (local dotnet tool), jscpd 5.4.2 (npm).

**Storage**: N/A (no new persistence; baseline measurements stored as committed files, `.coverage-baseline` / `.jscpd-baseline.json`, updated manually via the `ratchet-*` commands; raw evidence under `specs/011-local-oss-quality-gate/baseline/`).

**Testing**: The existing suite (1436 tests) must remain green. The gate logic is a Python package under `scripts/quality-gate/` with a pytest + Hypothesis suite written first in Phase 2 (RED gate); property tests cover thresholds, baseline IO, ratchet direction, and severity filtering. The workflow is validated by contract tests that parse the YAML.

**Target Platform**: GitHub Actions (ubuntu-latest) standard runner; local dev on any machine with .NET 6+.

**Project Type**: CI/tooling + csproj configuration change (no game code changes).

**Performance Goals**: `static` + `sca` ≤ 10 minutes (Semgrep, Trivy, jscpd); `build-analysis` ≤ 20 minutes (restore, build, test, coverage); CodeQL scheduled deep scan 3–5 minutes. Durations are recorded on each run.

**Constraints**: `static`/`sca` require no `STARDEW_GAME_PATH`, SMAPI, or game assemblies; `build-analysis` requires `BUILDDEPS_TOKEN` and fails closed without it. Analyzers must not break `dotnet ef`/ModBuildConfig tooling; no global `NoWarn` without justification. ReportGenerator `Summary.json` is one-decimal, so the coverage gate uses a 0.05 epsilon.

**Scale/Scope**: 2 csproj files, 1 workflow, 1 Python package (~7 modules + 8 test modules), 2 baseline files, docs, and verification evidence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I (DDD)**: Not affected — no domain code changes.
- **Principle II (Security-First)**: Improved — SAST (Semgrep/CodeQL) and dependency scanning added; path sanitization untouched.
- **Principle III (Async-Only)**: Not affected — no async code changes.
- **Principle IV (TDD)**: Respected — existing tests remain; coverage gate enforces test quality.
- **Principle V (Simplicity/YAGNI)**: Respected — start with Roslyn analyzers (smallest), compose stack incrementally; no premature abstractions.
- **Principle VI (Multiplayer)**: Not affected.

**Quality Gates**:
- Gate 1 (Static Checks): `dotnet build`/`dotnet test`/`dotnet format` must pass — the new analyzers make this gate stricter, aligned with Gate 1.
- Gate 2 (Product-Level): No user-facing changes; N/A.
- Gate 3 (E2E): No runnable game code changes; N/A (quality gate is CI-only).

**Verdict**: PASS — no violations. No Complexity Tracking entries needed.

## Project Structure

### Documentation (this feature)

```text
specs/011-local-oss-quality-gate/
├── plan.md              # This file
├── research.md          # Phase 0 output (consolidated)
├── data-model.md        # Phase 1 output (baseline/threshold entities)
├── quickstart.md        # Phase 1 output (validation guide)
├── contracts/           # Phase 1 output (workflow contract)
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
.github/workflows/
└── quality-gate.yml     # NEW: static (Semgrep/Trivy/jscpd) + sca + build-analysis + gate + scheduled CodeQL

scripts/quality-gate/
├── pyproject.toml
├── requirements-dev.txt        # pytest, hypothesis, PyYAML
├── requirements-ci.txt         # semgrep (pinned)
├── README.md
├── quality_gate/
│   ├── __init__.py
│   ├── __main__.py
│   ├── baseline.py             # parse/validate/ratchet baselines
│   ├── coverage.py             # ReportGenerator Summary.json gate
│   ├── duplication.py          # jscpd report gate
│   ├── security.py             # Trivy report gate
│   ├── diagnostics.py          # GitHub annotations + ordering
│   └── cli.py                  # subcommands + exit codes
└── tests/
    ├── conftest.py
    ├── test_baseline.py
    ├── test_coverage_gate.py
    ├── test_duplication_gate.py
    ├── test_security_gate.py
    ├── test_workflow_contract.py          # US2 workflow assertions
    ├── test_workflow_build_analysis.py    # US3 workflow assertions
    └── test_cli.py

dotnet-tools.json        # NEW: pinned ReportGenerator 5.5.12
Directory.Build.props    # NEW: solution-wide analyzer config (dotnet new buildprops)
LivingRoots/LivingRoots.csproj             # MODIFIED: analyzers + lock file
LivingRoots.Tests/LivingRoots.Tests.csproj # MODIFIED: analyzers + lock file
.coverage-baseline       # NEW: ratchet baseline (line/branch coverage)
.jscpd-baseline.json     # NEW: ratchet baseline (C# duplication)
```

**Structure Decision**: CI/tooling change only. No new game source directories. The gate logic lives in a small Python package so thresholds and report parsing are unit-testable without a game install; the workflow is a single file with one owner. Baseline files are committed at repo root to enable ratcheting.

## Complexity Tracking

No violations — table intentionally empty.

---

## Phase 0: Research (consolidated)

See `research.md` in this directory (consolidated from assessment research + agent findings). Summary of decisions:

- **D1 — Analyzers**: `SonarAnalyzer.CSharp` 10.35.0.4138 + `Microsoft.CodeAnalysis.NetAnalyzers` 10.0.403; `AnalysisLevel=latest`, `AnalysisMode=Recommended`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`. Measured pre-change: 155 warnings would become errors (129 tests, 26 LivingRoots); NetAnalyzers contributes 0 CA diagnostics beyond the SDK defaults. `EnforceCodeStyleInBuild=true` may add diagnostics, so T020 stops and re-plans if the count changes materially.
- **D2 — False positives**: targeted `.editorconfig` per-folder overrides + `[SuppressMessage]` with justification; never global `NoWarn`; avoid "monster GlobalSuppressions".
- **D3 — SAST**: Semgrep CE CLI 1.182.1 per PR (~10s; 6 `p/csharp` rules; 0 findings on 78 files today). CodeQL Action v4 scheduled with `build-mode: none`, no `BUILDDEPS_TOKEN`, 3–5 minutes; it is not a per-PR blocking gate. The archived `semgrep/semgrep-action` is not used.
- **D4 — Coverage/duplication**: initial floors/ceilings come from the T043 re-measurement, not from estimates. Current measurements: ReportGenerator 5.5.12 `summary.linecoverage` 79.1% / `summary.branchcoverage` 70.4% (one-decimal); jscpd 5.4.2 C#-only 0.48% (6 clones, 64 lines). The all-format jscpd figure (1.47%, 38 clones, 510 lines) is not the gate metric.
- **D5 — License**: no compliance impact; pin Semgrep, Trivy, jscpd, and action versions for audit. Semgrep CE is LGPL-2.1 and is used as a CI tool, not distributed with the mod.
- **D6 — Thresholds**: initial floors = measured baseline; ratchet upward for coverage and downward for duplication. Ratchets are manual (`ratchet-*` commands) and reviewed; CI only enforces.
- **D7 — Coverage precision**: ReportGenerator `Summary.json` rounds to one decimal, so the gate compares with a 0.05 epsilon and the tests assert that precision explicitly.
- **D8 — Workflow contract tests**: split by story into `test_workflow_contract.py` (US2: static/sca/CodeQL) and `test_workflow_build_analysis.py` (US3: build-analysis/gate), so US2 can be green before the US3 job exists.

## Phase 1: Design & Contracts

See `data-model.md`, `contracts/`, `quickstart.md` in this directory. Summary:

- **Entities**: Quality Gate Job, Baseline Measurement, Threshold (Ratchet) — see data-model.md.
- **Contracts**: `contracts/quality-gate-workflow.md` describing the workflow steps and thresholds.
- **Quickstart**: `quickstart.md` with validation scenarios (run analyzers locally, run Semgrep, measure coverage/duplication, verify ratchet).
- **No external API contracts** (purely internal CI change).