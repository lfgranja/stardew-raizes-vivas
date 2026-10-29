# Data Model: Local OSS Quality Gate

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29

This feature introduces no runtime domain entities (no game code changes). The "data" is configuration and baseline measurements committed to the repo.

## Entities

### Baseline Measurement
A recorded snapshot of a quality metric at a point in time, used to set the initial threshold.

| Field | Type | Description |
|---|---|---|
| metric | enum {coverage-lines, coverage-branches, duplication-percentage} | The metric being baselined |
| value | number | The measured value (percentage) |
| precision | number | 0.1 for ReportGenerator `Summary.json`; 0.01 for jscpd |
| measuredAt | ISO 8601 date | When measured |
| commit | git SHA | The commit measured |
| scope | string | `LivingRoots/` production assembly / C# sources only |

**Baseline files**:
- `.coverage-baseline` — `lines=<measured>` and `branches=<measured>`, measured with ReportGenerator 5.5.12 `Summary.json` (`summary.linecoverage` / `summary.branchcoverage`). Current measurement: `lines=79.1 branches=70.4` (one-decimal rounding).
- `.jscpd-baseline.json` — `percentage`, `duplicates`, `lines`, `measuredAt`, `commit`, `scope`. Current measurement: `percentage=0.48`, `duplicates=6`, `lines=64` with `--format csharp`; the all-format 1.47% figure is not the gate metric.

Values in these files MUST come from generated reports (T043), never typed from estimates.

### Threshold (Ratchet)
A gate value derived from a Baseline Measurement that only moves in the improving direction.

| Field | Type | Description |
|---|---|---|
| metric | enum | Same as Baseline |
| threshold | number | Minimum for coverage (`direction=up`) or maximum for duplication (`direction=down`) |
| direction | enum {up, down} | `up` for coverage, `down` for duplication |
| update | enum {manual} | CI enforces only; `ratchet-*` commands update the committed baseline |
| justification | string (optional) | Required if a threshold is ever weakened |

**Rules**:
- A threshold is never weakened without a documented justification.
- CI compares the measured value with the committed threshold; a regression fails the job.
- `ratchet-coverage` stores `max(old, measured)`; `ratchet-duplication` stores `min(old, measured)`.
- Baseline updates are explicit commits and are reviewed; no main-branch job writes the baseline automatically.

### Quality Gate Job
A CI job (or set of jobs) enforcing the blocking checks.

| Field | Type | Description |
|---|---|---|
| blockingChecks | list {analyzer-build, semgrep, sca, coverage, duplication} | Checks whose failure fails `gate` |
| scheduledDeepScan | enum {codeql} | Non-blocking, scheduled/dispatch only |
| result | enum {pass, fail} | Aggregate result |
| durationSeconds | number | Per-job duration, reported on every run |
| diagnostics | list | See Diagnostics below |

### Diagnostics

| Finding type | Required fields |
|---|---|
| Analyzer / Semgrep / CodeQL | file, line, rule, message |
| Trivy / SCA | package, installedVersion, cve, targetFile, severity |
| Coverage | class or file below the floor, measured value, floor |
| Duplication | clone pair, `file:line` for both sides, duplicated lines |
| Configuration | explicit error message and the offending path/field |

GitHub annotations use `::error file=...,line=...::...` for code findings and `::error title=...::...` for non-file findings.

### Analyzer Configuration (csproj properties)
| Property | Value | Purpose |
|---|---|---|
| `AnalysisLevel` | `latest` | Use latest analyzer rule set |
| `AnalysisMode` | `Recommended` | Curated rule set (not all 200+) |
| `TreatWarningsAsErrors` | `true` | Warnings become build errors |
| `EnforceCodeStyleInBuild` | `true` | IDE rules in build; may add diagnostics beyond the measured 155 |
| PackageReference `SonarAnalyzer.CSharp` | 10.35.0.4138 | Sonar rules as Roslyn analyzers |
| PackageReference `Microsoft.CodeAnalysis.NetAnalyzers` | 10.0.403 | CA rules, pinned for consistency with the SDK |

## State Transitions

- Baseline → Threshold: created when the baseline is first measured (Phase 5, T043–T044).
- Threshold → (ratcheted) Threshold: a maintainer runs `ratchet-*` and commits the improved baseline.
- Quality Gate Job: pass/fail per run; no persistence beyond CI artifacts.

## Validation Rules

- Coverage floor must be ≤ measured baseline at creation time.
- Duplication ceiling must be ≥ measured baseline at creation time.
- Coverage comparison uses a 0.05 epsilon because `Summary.json` is one-decimal.
- No global `NoWarn` without a documented justification.
- Suppressions (`[SuppressMessage]`, `#pragma`) must include a justification comment.
- A baseline file with missing, NaN, or out-of-range values is a configuration error (exit 2), never a pass.
