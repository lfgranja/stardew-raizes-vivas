# Data Model: Local OSS Quality Gate

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29

This feature introduces no runtime domain entities (no game code changes). The "data" is configuration and baseline measurements committed to the repo.

## Entities

### Baseline Measurement
A recorded snapshot of a quality metric at a point in time, used to set the initial threshold.

| Field | Type | Description |
|---|---|---|
| metric | enum {coverage-lines, coverage-branches, duplication-lines, duplication-tokens} | The metric being baselined |
| value | number | The measured value (percentage) |
| measuredAt | ISO 8601 date | When measured |
| commit | git SHA | The commit measured |
| scope | string | e.g., `LivingRoots/` (production assembly only) |

**Baseline files**:
- `.coverage-baseline` — contains line% and branch% (e.g., `lines=79.73 branches=70.78`).
- `.jscpd-baseline.json` — contains duplication % and clone count.

### Threshold (Ratchet)
A gate value derived from a Baseline Measurement that only moves upward.

| Field | Type | Description |
|---|---|---|
| metric | enum | Same as Baseline |
| floor | number | Minimum acceptable value (current baseline) |
| direction | enum {min, max} | `min` for coverage (higher is better), `max` for duplication (lower is better) |
| ratchet | boolean | If true, the floor auto-raises on improvement (main branch only) |
| justification | string (optional) | Required if the floor is ever lowered |

**Rules**:
- A threshold is never lowered without a documented justification.
- On main-branch CI, if the measured value improves beyond the floor, the baseline file is updated (ratchet).
- On PR CI, the measured value must satisfy the floor; regression fails the job.

### Quality Gate Job
A CI job aggregating all checks.

| Field | Type | Description |
|---|---|---|
| checks | list {analyzer-build, semgrep, codeql, sca, coverage, duplication} | The checks run |
| result | enum {pass, fail} | Aggregate result |
| diagnostics | list {file, line, rule, message} | File/line diagnostics for failures |

### Analyzer Configuration (csproj properties)
| Property | Value | Purpose |
|---|---|---|
| `AnalysisLevel` | `latest` | Use latest analyzer rule set |
| `AnalysisMode` | `Recommended` | Curated rule set (not all 200+) |
| `TreatWarningsAsErrors` | `true` | Warnings become build errors |
| `EnforceCodeStyleInBuild` | `true` | IDE rules in build |
| PackageReference `SonarAnalyzer.CSharp` | latest 10.x | Sonar rules as Roslyn analyzers |
| PackageReference `Microsoft.CodeAnalysis.NetAnalyzers` | 8.x | CA rules (already in SDK 6, pinned for consistency) |

## State Transitions

- Baseline → Threshold: created when baseline is first measured (Phase 1 of implementation).
- Threshold → (ratcheted) Threshold: when a main-branch run improves the metric, the floor raises.
- Quality Gate Job: pass/fail per run; no persistence beyond CI artifacts.

## Validation Rules

- Coverage floor must be ≤ measured baseline at creation time.
- Duplication ceiling must be ≥ measured baseline at creation time.
- No global `NoWarn` without a documented justification.
- Suppressions (`[SuppressMessage]`, `#pragma`) must include a justification comment.