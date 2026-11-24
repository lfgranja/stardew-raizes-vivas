# Idea Research: SonarCloud to local gate baseline readiness

- **Slug**: sonarcloud-to-local-gate-baseline
- **Created**: 2026-11-01T07:40:00Z
- **Evidence confidence (overall)**: high

## Users & Demand

- **Maintainer pain**: SonarCloud blocks merges with opaque reliability ratings and lacks actionable file/line diagnostics for local iteration ([source: AGENTS.md - CI section] | cited) (confidence: high)
- **Need for deterministic local gating**: Feature 011 requires build-fail with actionable diagnostics without cloud SaaS or game install ([source: specs/011-local-oss-quality-gate/spec.md FR-001..FR-003] | cited) (confidence: high)

## Prior Art

- **Spec 011 local OSS quality gate**: Replaces SonarCloud with Roslyn analyzers (SonarAnalyzer.CSharp + .NET analyzers), Semgrep CE, Trivy, Coverlet + ReportGenerator, jscpd; CodeQL scheduled; ratchet-based thresholds ([source: specs/011-local-oss-quality-gate/spec.md, plan.md, contracts/] | cited) (confidence: high)
- **Recent bug workflow cleanup**: All OPEN/CONFIRMED SonarCloud issues on dev addressed via /speckit.bug.assess/fix/test (PRs #559, #560, #561 merged) ([source: git log on dev] | cited) (confidence: high)

## Market & Context

- **SonarCloud alternatives**: Open source SAST/dependency/duplication tools (Semgrep, Trivy, jscpd) are widely used for local/CI gating without SaaS lock-in ([source: general OSS practice, reflected in spec 011] | cited) (confidence: high)
- **Cost of doing nothing**: Continue relying on opaque SonarCloud ratings with slower feedback loops and external dependencies ([source: spec 011 problem statement] | cited) (confidence: high)

## Data & Constraints

- **Current baselines (post-fixes)**: `.coverage-baseline` shows lines=79.5%, branches=70.7% (measured 2026-10-30, commit 2b03438). `.jscpd-baseline.json` shows 0.5% duplication (6 clones, 67 lines) measured 2026-11-01 commit 3e44457. ([source: repo files] | cited) (confidence: high)
- **Build constraints**: `build-analysis` job needs pinned `gamepath.zip` via `BUILDDEPS_TOKEN` (Pathoschild.ModBuildConfig requires game assemblies); `static`/`sca` game-free ([source: spec 011 FR-003, AGENTS.md] | cited) (confidence: high)
- **TDD blocker**: Phase 2 tests must be RED before implementing gate modules ([source: specs/011-local-oss-quality-gate/tasks.md T018] | cited) (confidence: high)
- **Analyzer divergence**: Sonar rulesets differ from local analyzers; `AnalysisLevel=6.0-recommended` + `TreatWarningsAsErrors=true` may surface new warnings ([source: intake] | cited) (confidence: medium)

## Evidence Against the Idea

- **Not a 1:1 replacement**: Zero SonarCloud issues doesn't guarantee local gate pass due to different rulesets and thresholds ([source: intake, spec 011] | cited) (confidence: high)
- **Infrastructure still needed**: Even with clean code, tests/workflow/Python modules must be built per spec 011 plan ([source: tasks.md] | cited) (confidence: high)
- **Baselines may shift**: Recent refactorings could affect coverage/duplication; fresh measurement needed ([source: intake] | cited) (confidence: medium)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: Will enabling analyzers introduce warnings not caught by SonarCloud? Need to run probe/build analysis.]
- [NEEDS CLARIFICATION: Are Phase 2 tests committed and RED on the feature branch 011-local-oss-quality-gate?]
- [NEEDS CLARIFICATION: Should we ratchet baselines upward from current values or set them as-is?]

## Sources

- `specs/011-local-oss-quality-gate/spec.md` (host: local, policy: allowlisted)
- `specs/011-local-oss-quality-gate/tasks.md` (host: local, policy: allowlisted)
- `.coverage-baseline` (host: local, policy: allowlisted)
- `.jscpd-baseline.json` (host: local, policy: allowlisted)
- `git log` on dev (host: local, policy: allowlisted)
