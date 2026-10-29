# Implementation Plan: Local OSS Quality Gate (replacing SonarCloud assessments)

**Branch**: `011-local-oss-quality-gate` | **Date**: 2026-10-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-local-oss-quality-gate/spec.md`

**Note**: This plan was filled in by the `/speckit-plan` command.

## Summary

Replace the SonarCloud code-quality gate (opaque reliability rating, cloud-dependent) with a deterministic, local, open-source quality gate for the .NET 6 Stardew Valley mod: Roslyn analyzers (`SonarAnalyzer.CSharp` + `Microsoft.CodeAnalysis.NetAnalyzers`) with `TreatWarningsAsErrors` on both projects, plus a new GitHub Actions workflow (no game/SMAPI dependency) running Semgrep (fast SAST), CodeQL (deep SAST, scheduled), OWASP Dependency-Check/Trivy (SCA), Coverlet+ReportGenerator (coverage), and jscpd (duplication), with ratcheted thresholds measured from the current baseline. SonarCloud stays in parallel for 2–4 weeks of green runs before removal.

## Technical Context

**Language/Version**: C# 11 / .NET 6 (mod) + .NET 6 test project; CI YAML.

**Primary Dependencies**: `SonarAnalyzer.CSharp` (NuGet), `Microsoft.CodeAnalysis.NetAnalyzers` (NuGet), Semgrep CE (GitHub Action), CodeQL (GitHub Action), Coverlet 6.0.0 (already referenced), ReportGenerator (tool), jscpd (npm action), OWASP Dependency-Check or Trivy (GitHub Action).

**Storage**: N/A (no new persistence; baseline measurements stored as committed files, e.g. `.coverage-baseline` / `.jscpd-baseline.json`).

**Testing**: `dotnet build` / `dotnet test` (existing suite, 1436 tests) must remain green; new quality-gate job is itself validated by CI.

**Target Platform**: GitHub Actions (ubuntu-latest) standard runner; local dev on any machine with .NET 6+.

**Project Type**: CI/tooling + csproj configuration change (no game code changes).

**Performance Goals**: Fast gate (analyzers + Semgrep) completes in a few minutes; CodeQL deep scan runs on a schedule (3–5 min).

**Constraints**: No `STARDEW_GAME_PATH` / SMAPI dependency for the quality gate; analyzers must not break `dotnet ef`/ModBuildConfig tooling; no global `NoWarn` without justification.

**Scale/Scope**: 2 csproj files, ~1 new workflow file, ~2 baseline files, documentation.

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
└── quality-gate.yml     # NEW: analyzer build + Semgrep + CodeQL + SCA + coverage + jscpd

LivingRoots/
└── LivingRoots.csproj   # MODIFIED: add analyzer PackageReferences + properties

LivingRoots.Tests/
└── LivingRoots.Tests.csproj  # MODIFIED: same

.coverage-baseline        # NEW: ratchet baseline (coverage %)
.jscpd-baseline.json     # NEW: ratchet baseline (duplication %)
Directory.Build.props     # NEW (optional): solution-wide analyzer config
```

**Structure Decision**: CI/tooling change only. No new source directories; edits to two csproj files and one new workflow. Baseline files committed at repo root to enable ratcheting.

## Complexity Tracking

No violations — table intentionally empty.

---

## Phase 0: Research (consolidated)

See `research.md` in this directory (consolidated from assessment research + agent findings). Summary of decisions:

- **D1 — Analyzers**: `SonarAnalyzer.CSharp` + `Microsoft.CodeAnalysis.NetAnalyzers`; `AnalysisMode=Recommended` (or `latest-recommended`) initially; `TreatWarningsAsErrors=true`. Measurement: 155 warnings would become errors (129 tests, 26 LivingRoots); NetAnalyzers contributes 0 CA diagnostics unless `AnalysisMode=AllEnabledByDefault`.
- **D2 — False positives**: targeted `.editorconfig` per-folder overrides + `[SuppressMessage]` with justification; never global `NoWarn`; avoid "monster GlobalSuppressions".
- **D3 — SAST**: Semgrep CE per-PR (~10s); CodeQL scheduled (3–5 min, requires build deps token).
- **D4 — Coverage/duplication**: ratchet from measured baseline (79.73% lines / 70.78% branches; 2.37% duplication); Semgrep per-PR + CodeQL scheduled.
- **D5 — License**: no compliance impact; pin Semgrep version for audit.
- **D6 — Thresholds**: initial floors = measured baseline; ratchet upward only.

## Phase 1: Design & Contracts

See `data-model.md`, `contracts/`, `quickstart.md` in this directory. Summary:

- **Entities**: Quality Gate Job, Baseline Measurement, Threshold (Ratchet) — see data-model.md.
- **Contracts**: `contracts/quality-gate-workflow.md` describing the workflow steps and thresholds.
- **Quickstart**: `quickstart.md` with validation scenarios (run analyzers locally, run Semgrep, measure coverage/duplication, verify ratchet).
- **No external API contracts** (purely internal CI change).