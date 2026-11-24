# Decision: Replace SonarQube/SonarCloud assessments with a lightweight, local, OSS quality stack

- **Slug**: replace-sonarqube-lightweight-local-oss
- **Decided**: 2026-10-29
- **Verdict**: go
- **Artifacts reviewed**: intake.md (yes) | research.md (yes) | problem.md (yes) | concept.md (yes)

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | SonarCloud reliability gate repeatedly fails on `dev` (run 123455233388, PR #282) and `Build and Test` is dormant; the pain is documented in-repo. |
| Evidence strength | adequate | Tooling facts verified against primary sources (Microsoft Learn, NuGet, GitHub); market comparisons from 2026 roundups (secondary); no hands-on build benchmark yet. |
| Value vs. inaction | strong | Doing nothing leaves an opaque, blocking gate and no deterministic local safety net on standard runners. |
| Feasibility / appetite | adequate | Option B (medium appetite) is credible and composed of mature, widely used OSS tools; threshold calibration is unmeasured. |
| Strategic fit | adequate | Aligns with the project's OSS/free/local preferences and constraint-driven development; no enterprise/compliance needs. |
| Risk posture | adequate | Key risks (maintenance burden, weaker taint in Semgrep CE, CodeQL runtime, license) identified with mitigations (parallel run, ratchet, fast/deep SAST split). |

## Verdict & Rationale

**go.** The problem is real and well-documented, the evidence is adequate (not thin or assumption-only), and a shaped concept (Option B) is recommended with a credible appetite. The remaining gaps (threshold values, PR-decoration preference, CodeQL acceptability, analyzer warning count) are measurable/decidable during specification and do not block a go; they are carried forward as open questions. A `needs-clarification` verdict was considered but rejected because the blocking unknowns are implementation-level and can be resolved in `/speckit.specify`, not discovery-level.

## If needs-clarification

N/A — verdict is go.

## If go — Handoff to `/speckit.specify`

- **Problem**: The maintainer has no deterministic, local, offline quality gate; SonarCloud's opaque reliability rating blocks merges and no standard-runner gate exists (Build and Test is dormant).
- **Chosen approach**: Option B — Composed local stack (full replacement): Roslyn analyzers (`SonarAnalyzer.CSharp` + `NetAnalyzers` with `TreatWarningsAsErrors`) + Semgrep CE (fast per-PR SAST) + CodeQL (deeper/scheduled SAST) + Coverlet + ReportGenerator (coverage) + jscpd (duplication), with a ratchet-based threshold strategy (measure baseline first, never regress) and SonarCloud kept in parallel for 2–4 weeks before removal.
- **In scope / out of scope**: In scope — analyzer enablement (Phase 1), new CI workflow without game dependency (Phase 2), threshold calibration from baseline, removal of SonarCloud after validation (Phase 3). Out of scope — mutation testing (Stryker.NET), Build and Test SMAPI dependency removal, enterprise/compliance features, gameplay changes.
- **Success metrics**: (1) new lightweight CI job runs on standard runner without `STARDEW_GAME_PATH` and completes in bounded time; (2) `dotnet build` with `TreatWarningsAsErrors` passes (0 reliability defects); (3) 0 high/critical security findings on new code; (4) duplication below threshold; (5) coverage at/above threshold; (6) file/line diagnostics available from CI without an external dashboard.
- **Carried-forward open questions**:
  - [NEEDS CLARIFICATION: How many Roslyn analyzer warnings become errors when enabling `TreatWarningsAsErrors`? (measure in Phase 1)]
  - [NEEDS CLARIFICATION: Does the maintainer require historical trend dashboards and inline PR decoration, or is build-fail sufficient?]
  - [NEEDS CLARIFICATION: Is CodeQL's runtime acceptable, or should Semgrep CE be primary with CodeQL scheduled?]
  - [NEEDS CLARIFICATION: What coverage and duplication thresholds are acceptable? (ratchet from baseline)]
  - [NEEDS CLARIFICATION: Remove SonarCloud entirely or keep in parallel during transition?]
  - [NEEDS CLARIFICATION: Any license/compliance constraints (e.g., LGPL-2.1 Semgrep) affecting mod distribution?]
  - [NEEDS CLARIFICATION: Any external contributors/users beyond the maintainer affected?]