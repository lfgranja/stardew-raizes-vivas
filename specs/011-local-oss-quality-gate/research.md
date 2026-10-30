# Research: Local OSS Quality Gate

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29
**Consolidated from**: `.specify/assessments/replace-sonarqube-lightweight-local-oss/research.md` + codebase/online agent findings (2026-10-29)

## Decisions

### D1 — Analyzer packages and mode
- **Decision**: Add `SonarAnalyzer.CSharp` and `Microsoft.CodeAnalysis.NetAnalyzers` to both csproj; `AnalysisLevel=latest`; `AnalysisMode=Recommended` (not `All`); `TreatWarningsAsErrors=true`.
- **Rationale**: `Recommended` is the curated, sane starting set; `All` lights up 200+ rules and creates excessive noise. Measured: 155 warnings would become errors on first run (129 tests, 26 LivingRoots). NetAnalyzers contributes 0 CA diagnostics under `Recommended`; switch to `AllEnabledByDefault` only if CA rules are desired.
- **Alternatives**: `AnalysisMode=All` (rejected: too noisy for brownfield); `None` + opt-in (rejected: defeats the purpose).

### D2 — False-positive handling
- **Decision**: Targeted `.editorconfig` per-folder overrides + `[SuppressMessage]` with justification; `WarningsNotAsErrors` as escape valve; never global `NoWarn`; avoid "monster GlobalSuppressions".
- **Rationale**: Community consensus (csharpstack.dev, SonarSource docs) and in-com guidance (target FP rate <10%).
- **Alternatives**: Global `NoWarn` (rejected: hides drift).

### D3 — SAST tooling
- **Decision**: Semgrep CE CLI 1.182.1 per PR (fast, ~10s; 6 `p/csharp` rules); CodeQL scheduled nightly/weekly or on manual dispatch (deep, 3–5 min) with `build-mode: none`, no game assemblies and no `BUILDDEPS_TOKEN`.
- **Rationale**: CodeQL's C# `build-mode: none` removes the build-dependency that made the original plan complex; Semgrep gives seconds-level PR feedback. The archived `semgrep/semgrep-action` is not used.
- **Alternatives**: CodeQL per-PR (rejected: slower feedback, build-deps complexity); Semgrep only (rejected: no cross-file taint).

### D4 — SCA (dependency scanning)
- **Decision**: Trivy 0.76.1 in CI, scanning the committed `packages.lock.json` files.
- **Rationale**: Free, OSS, covers NuGet lock files; one pinned binary, JSON output that the Python gate can annotate.

### D5 — Coverage & duplication
- **Decision**: Coverlet + ReportGenerator 5.5.12 (coverage); jscpd 5.4.2 with `--format csharp` (duplication); thresholds ratcheted from the T043 re-measurement. Current measurements: 79.1% lines / 70.4% branches (one-decimal `Summary.json`) and 0.48% C# duplication (6 clones, 64 lines).
- **Rationale**: .NET standard tooling; ratchet pattern avoids big-bang targets and prevents regression. The one-decimal coverage precision requires a 0.05 epsilon.
- **Alternatives**: PMD CPD (rejected for C# in favor of jscpd's native GitHub Action and lower setup).

### D6 — License/compliance
- **Decision**: No compliance impact from using Semgrep CE (LGPL-2.1) in CI; pin Semgrep version in workflow for audit. Note Semgrep Rules License v1.0 restricts vendor/SaaS use of Semgrep-maintained rules (not applicable here).
- **Rationale**: LGPL obligations attach to distribution, not invocation; FSF GNU FAQ + Semgrep docs.

### D7 — SonarCloud removal
- **Decision**: Keep SonarCloud in parallel for 2–4 weeks of green `Quality Gate` runs (all blocking jobs success, no skipped blocking job), then remove in a follow-up. No trend dashboard or inline PR decoration required (build-fail sufficient).
- **Rationale**: Validated by clarifications (2026-10-29); reduces risk while confirming local gate equivalence.

## Open Items Resolved by Clarification
- Q1 (decoration): build-fail sufficient.
- Q2 (removal vs parallel): remove after 2–4 weeks.
- Q3 (CodeQL scheduling): scheduled/deep scan.

## Sources
See `.specify/assessments/replace-sonarqube-lightweight-local-oss/research.md` and agent findings (2026-10-29) for full citations. Key sources:
- Microsoft Learn — Code analysis overview / MSBuild props: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview
- SonarAnalyzer.CSharp: https://www.nuget.org/packages/sonaranalyzer.csharp/
- Semgrep vs CodeQL (2026): https://appsecsanta.com/sast-tools/semgrep-vs-codeql
- Google coverage best practices (Ivanković FSE 2019): https://homes.cs.washington.edu/~rjust/publ/google_coverage_fse_2019.pdf
- jscpd: https://github.com/kucherenko/jscpd
- Coverlet: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage
- FSF GNU FAQ (LGPL): https://www.gnu.org/licenses/gpl-faq.en.html
- Semgrep legal/terms: https://semgrep.dev/legal/terms
## Analyzer Configuration (T026)

- AnalysisLevel pinned to `6.0-recommended` (Q9).
- SonarAnalyzer.CSharp 10.35.0.4138 and Microsoft.CodeAnalysis.NetAnalyzers 10.0.403 with `PrivateAssets=all`.
- Pre-change baseline: 98 diagnostics (mod project only; test project clean after scoped suppression).
- ModBuildConfig analyzer coexists; CA1707 suppressed in tests via scoped `.editorconfig`.
