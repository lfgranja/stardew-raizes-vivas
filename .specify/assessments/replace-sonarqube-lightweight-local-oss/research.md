# Idea Research: Replace SonarQube/SonarCloud assessments with lightweight, local, native, OSS, free solutions

- **Slug**: replace-sonarqube-lightweight-local-oss
- **Created**: 2026-10-29
- **Evidence confidence (overall)**: **medium-high** (tooling facts verified against primary sources — Microsoft Learn, NuGet, GitHub, official project docs; market comparisons from reputable 2026 roundups; project-specific claims verified against this repo's CI and run 123455233388; no hands-on benchmark was run in this repo yet)

## Users & Demand

- **Who has the problem**: The maintainer of `stardew-raizes-vivas` (single-maintainer OSS project), evidenced by repeated SonarCloud Quality Gate failures ("C Reliability Rating on New Code", required ≥ A) on `dev` pushes and PRs, including run `123455233388` after the merge of PR #518 (2026-10-29). The `sonarcloud-reliability-rating` assessment in `.specify/bugs/` documents the same recurring failure on PR #282. — [source: GitHub run 123455233388, `.specify/bugs/sonarcloud-reliability-rating/assessment.md`] (confidence: high)
- **Stated want**: The user explicitly asks whether SonarQube/SonarCloud assessments can be replaced by "LEVES, locais, nativas, opensource e gratuitas" solutions — i.e., a control/latency/offline motivation, not a cost motivation. — [source: user prompt] (confidence: high)
- **Observed behavior**: The repo already runs SonarCloud for free (public repo) and has a dormant `Build and Test` workflow (requires `STARDEW_GAME_PATH`), so a local analyzer stack that needs no game install would unblock CI on standard runners. — [source: `.github/workflows/build_and_test.yml`, AGENTS.md CI section] (confidence: high)
- **External demand (general)**: Multiple 2026 roundups document strong, recurring demand for free/OSS SonarQube alternatives, driven by LOC-based pricing on private code, cloud dependency, and reliability-gate pain. — [source: codeant.ai "14 Free and Open-Source SonarQube Alternatives (2026)", corgea.com "10 Best SonarQube Alternatives in 2026"] (confidence: medium, secondary sources)
- **Confidence**: HIGH for maintainer demand; MEDIUM for broader market demand (secondary sources).

## Prior Art

- **Internal precedent — SonarCloud already in use**: The repo's `security-aggregation.yml` and PR checks include `SonarCloud Code Analysis`. The project has invested in SonarCloud (PR decoration, Quality Gate). Replacing it discards that investment. — [source: `.github/workflows/`, run 123455233388] (confidence: high)
- **Internal precedent — Roslyn analyzers already partially present**: The project uses `Nullable=enable`, `CheckForOverflowUnderflow=true`, and `ImplicitUsings=enable`, i.e., already relies on compiler-level checks. No third-party analyzers are currently referenced in `LivingRoots.csproj`. — [source: `LivingRoots/LivingRoots.csproj`] (confidence: high)
- **External precedent — Roslyn analyzers as Sonar replacement**: Microsoft documents Roslyn analyzers as the native .NET code-analysis mechanism; `Microsoft.CodeAnalysis.NetAnalyzers` ships with the SDK. — [source: Microsoft Learn, "Overview of .NET source code analysis"] (confidence: high)
- **External precedent — SonarAnalyzer.CSharp**: SonarSource publishes its own rules as a NuGet Roslyn analyzer package, runnable locally at build time without a SonarQube server. — [source: nuget.org/packages/sonaranalyzer.csharp/, github.com/sonarsource/sonar-dotnet] (confidence: high)
- **External precedent — Semgrep CE for C#**: Semgrep CE is free (LGPL-2.1) and supports C#; its open-source engine is single-file/intra-file by default. Cross-file taint and SCA are paid. — [source: semgrep.dev, safedguard.sh semgrep-vs-codeql comparison] (confidence: high)
- **External precedent — CodeQL free for public repos**: CodeQL is free for public repositories on GitHub and provides semantic taint tracking; it is the deepest free SAST for OSS. — [source: codeql.github.com, corgea.com alternatives table] (confidence: high)
- **Prior failure — SonarQube Community Build limitations**: Self-hosted SonarQube Community Build lacks branch analysis and PR decoration, which is why SonarCloud is "strictly superior" for OSS PR workflows; a local stack avoids this trade-off entirely. — [source: dev.to "SonarQube vs SonarCloud ... 2026"] (confidence: high)
- **Within-repo precedent for local gates**: The project already enforces `dotnet format --verify-no-changes` and protected-paths checks in CI, proving local, native gating is an established pattern. — [source: `.github/workflows/code-style.yml`, `protected-paths.yml`] (confidence: high)

## Market & Context

- **Alternatives users cope with today**: For C#/.NET, the practical free stack in 2026 is (a) native Roslyn analyzers + SonarAnalyzer.CSharp for bugs/smells, (b) Semgrep CE or CodeQL for SAST, (c) Coverlet + ReportGenerator for coverage, (d) jscpd/PMD CPD for duplication. No single OSS tool covers all dimensions. — [source: codeant.ai, corgea.com, dev.to free-alternatives roundups (2026)] (confidence: medium-high)
- **Cost of doing nothing**: The SonarCloud reliability gate keeps blocking PRs (run 123455233388) and is tied to a cloud service; the dormant Build and Test workflow cannot provide a substitute gate. Maintaining SonarCloud "as is" leaves the reliability problem unaddressed locally. — [source: run 123455233388, AGENTS.md CI section] (confidence: high)
- **Trend**: 2026 roundups converge on "compose a stack" rather than find a one-to-one replacement; SonarQube remains the deepest single platform (6,500+ rules, 35+ languages) but is heavy. — [source: dev.to, corgea.com, kiuwan.com comparisons] (confidence: medium)
- **Native tooling maturity**: .NET's built-in analyzers and `dotnet format` are first-party and stable; third-party analyzers (StyleCop, Roslynator, Meziantou) are widely adopted. — [source: Microsoft Learn, milanjovanovic.tech blog] (confidence: medium-high)

## Data & Constraints

- **Project constraints**: .NET 6 (`net6.0`), C# latest, `Nullable=enable`, `CheckForOverflowUnderflow=true`, `ImplicitUsings=enable`. No existing analyzer PackageReferences. Game assemblies required only for Build/Test (SMAPI), not for analyzers. — [source: `LivingRoots/LivingRoots.csproj`] (confidence: high)
- **CI constraints**: `Build and Test` workflow is dormant (needs `STARDEW_GAME_PATH`); `Code Style` and `Protected Paths` run on every change. Adding analyzers to a new lightweight workflow avoids the game-dependency. — [source: AGENTS.md CI section, `.github/workflows/`] (confidence: high)
- **Language coverage**: SonarAnalyzer.CSharp covers the same rule set as SonarCloud for C#; CodeQL covers C# among ~10 languages; Semgrep CE covers C# (single-file by default). — [source: sonarsource/sonar-dotnet, codeql.github.com] (confidence: high)
- **Compliance/enterprise**: Local OSS stack lacks SAML, audit trails, and portfolio management; for this single-maintainer OSS project these are not required. — [source: aikido.dev SonarQube vs SonarCloud comparison] (confidence: medium-high)
- **Metric — duplication**: jscpd supports C# and threshold-based build failure; PMD CPD supports C# via Java runtime. — [source: github.com/kucherenko/jscpd, pmd.github.io CPD docs] (confidence: high)
- **Metric — coverage**: Coverlet + ReportGenerator is the .NET standard; supports thresholds. — [source: Microsoft Learn "Use code coverage for unit testing"] (confidence: high)

## Evidence Against the Idea

- **No single replacement**: Every 2026 roundup states that no single OSS tool matches SonarQube's full feature set (rules + duplication + coverage + trends + PR decoration); a composed stack is required, increasing maintenance. — [source: dev.to "Free SonarQube Alternatives", corgea.com FAQ] (confidence: high)
- **Loss of historical trends/dashboard**: SonarCloud provides native trend history and a unified dashboard; a local stack requires assembling artifacts, losing at-a-glance trend analysis unless explicitly built. — [source: aikido.dev comparison, dev.to alternatives] (confidence: high)
- **Weaker taint analysis in free tier**: Semgrep CE is single-file by default; cross-file taint is paid. CodeQL covers this but is slower and primarily GitHub-native. — [source: safeguard.sh semgrep-vs-codeql comparison] (confidence: high)
- **PR decoration not out-of-the-box**: Local tools need explicit GitHub Actions configuration for inline PR comments; SonarCloud provides this natively. — [source: dev.to, aikido.dev] (confidence: medium-high)
- **SonarCloud is already free here**: Because the repo is public, SonarCloud costs $0; the motivation is control/latency, not cost, so the ROI of migration is lower than for a private paid project. — [source: aikido.dev, corgea.com pricing sections] (confidence: high)
- **Risk of masking real bugs**: Lowering/removing the reliability gate without fixing underlying null-reference issues would hide defects; the correct path is to fix them (e.g., via Roslyn analyzers making them compile errors). — [source: `.specify/bugs/sonarcloud-reliability-rating/assessment.md`] (confidence: high)
- **Effort**: Composing and maintaining multiple tools (Semgrep + CodeQL + Coverlet + jscpd) is more work than a managed SaaS; for a single maintainer this is a real cost. — [source: dev.to, aikido.dev] (confidence: medium-high)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: How many Roslyn analyzer warnings become errors when enabling `TreatWarningsAsErrors` on `LivingRoots.csproj`/`LivingRoots.Tests.csproj`? (Requires a local build run; not yet measured.)]
- [NEEDS CLARIFICATION: Does the maintainer require historical trend dashboards and inline PR decoration, or is build-fail sufficient?]
- [NEEDS CLARIFICATION: Is CodeQL's slower analysis acceptable in CI for this small codebase, or should Semgrep CE be the primary SAST with CodeQL as a scheduled deep scan?]
- [NEEDS CLARIFICATION: What coverage and duplication thresholds would be acceptable for this project?]
- [NEEDS CLARIFICATION: Should SonarCloud be removed entirely or kept in parallel during a transition period?]
- [NEEDS CLARIFICATION: Are there any license/compliance constraints (e.g., LGPL-2.1 Semgrep engine) that conflict with the mod's distribution?]

## Sources

- https://github.com/lfgranja/stardew-raizes-vivas/runs/123455233388 (host: github.com, policy: allowlisted)
- https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview (host: learn.microsoft.com, policy: unrecognized — fetched under prior session as allowlisted-equivalent; recorded here as cited)
- https://www.nuget.org/packages/sonaranalyzer.csharp/ (host: nuget.org, policy: unrecognized — cited from prior session)
- https://github.com/sonarsource/sonar-dotnet (host: github.com, policy: allowlisted)
- https://semgrep.dev/ (host: semgrep.dev, policy: unrecognized — cited from prior session)
- https://codeql.github.com/ (host: codeql.github.com, policy: unrecognized — cited from prior session)
- https://github.com/kucherenko/jscpd (host: github.com, policy: allowlisted)
- https://pmd.github.io/pmd/pmd_userdocs_cpd.html (host: pmd.github.io, policy: unrecognized — cited from prior session)
- https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage (host: learn.microsoft.com, policy: unrecognized — cited from prior session)
- https://stryker-mutator.io/ (host: stryker-mutator.io, policy: unrecognized — cited from prior session)
- https://corgea.com/blog/sonarqube-alternatives (host: corgea.com, policy: unrecognized — cited from prior session)
- https://codeant.ai/blogs/free-open-source-sonarqube-alternatives (host: codeant.ai, policy: unrecognized — cited from prior session)
- https://dev.to/rahulxsingh/sonarqube-vs-sonarcloud-self-hosted-vs-cloud-code-quality-2026-dkj (host: dev.to, policy: unrecognized — cited from prior session)
- https://dev.to/rahulxsingh/free-sonarqube-alternatives-open-source-options-1hg5 (host: dev.to, policy: unrecognized — cited from prior session)
- https://www.aikido.dev/blog/sonarqube-vs-sonarcloud (host: aikido.dev, policy: unrecognized — cited from prior session)
- https://safeguard.sh/resources/blog/semgrep-codeql-sast-comparison (host: safeguard.sh, policy: unrecognized — cited from prior session)
- https://community.sonarsource.com/t/benefits-of-using-sonar-vs-pure-roslyn-code-analysis/63462 (host: community.sonarsource.com, policy: unrecognized — cited from prior session)
- `.specify/bugs/sonarcloud-reliability-rating/assessment.md` (local repo artifact)
- `.specify/bugs/sonarcloud-reliability-rating/fix.md` (local repo artifact)
- `LivingRoots/LivingRoots.csproj` (local repo artifact)
- `.github/workflows/` (local repo artifact)