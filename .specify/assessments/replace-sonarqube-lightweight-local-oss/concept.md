# Concept: Replace SonarQube/SonarCloud assessments with a lightweight, local, OSS quality stack

- **Slug**: replace-sonarqube-lightweight-local-oss
- **Created**: 2026-10-29
- **Recommended option**: Option B — "Composed local stack (full replacement)"

## Options

### Option A — Roslyn-first local gate (smallest thing that could work)
- **Sketch**: Add `SonarAnalyzer.CSharp` and `Microsoft.CodeAnalysis.NetAnalyzers` as NuGet references to both `LivingRoots.csproj` and `LivingRoots.Tests.csproj`, enable `AnalysisLevel=latest`, `AnalysisMode=All`, and `TreatWarningsAsErrors=true`, then run `dotnet build` locally and fix whatever becomes a compile error. Keep SonarCloud running in parallel unchanged. The user-facing change is that reliability defects become build errors with file/line diagnostics instead of an opaque rating.
- **Appetite**: small (days)
- **Trade-offs**: Wins determinism, speed, offline execution, and concrete diagnostics; directly attacks the "C Reliability Rating" cause. Sacrifices nothing from SonarCloud (kept in parallel). Risk: unknown number of warnings may surface (could be large); may reveal pre-existing issues that block the build until fixed.
- **Rabbit holes**: Fixing a large volume of newly-surfaced warnings; tuning which rules to treat as errors vs. warnings; false positives in generated/ModBuildConfig code.

### Option B — Composed local stack (full replacement) — RECOMMENDED
- **Sketch**: Everything in Option A, plus a new GitHub Actions workflow (no game dependency) running Semgrep CE (SAST), CodeQL (deep SAST, scheduled or per-PR), Coverlet + ReportGenerator (coverage threshold), and jscpd (duplication threshold), with a small aggregator script that fails the job if any threshold is violated. SonarCloud is removed only after 2–4 weeks of green runs. Thresholds start from the repository's current measured baseline and ratchet upward (never regress) over time — i.e., the "ratchet" pattern: measure current values, set the gate at that level, and only raise it; never adopt an external benchmark (e.g., SonarQube's "A") as the initial bar.
- **Appetite**: medium (weeks)
- **Trade-offs**: Wins full coverage of all Sonar dimensions locally and offline; loses SonarCloud's unified dashboard/trend history and native PR decoration (must be re-implemented via Actions). Risks: maintenance burden of composing multiple tools; Semgrep CE is single-file by default (weaker taint); CodeQL is slower; threshold tuning requires baseline data that does not yet exist.
- **Rabbit holes**:
  - **Threshold calibration without a baseline**: setting thresholds before measuring current coverage/duplication risks an immediately-failing gate. Mitigation: measure first, set the gate at the current value, ratchet up.
  - **CodeQL runtime on a small repo**: CodeQL must build a database (requires compiling the .NET project) and run queries — minutes, not seconds, even on a small repo. Mitigation: use Semgrep CE (seconds) as the per-PR fast gate; run CodeQL as a scheduled/deeper scan or async per-PR.
  - **Writing/maintaining the aggregator script**: a script that collects Semgrep/CodeQL/Coverlet/jscpd results and fails on threshold violations. Mitigation: start with per-tool `--fail-on` flags before building a custom aggregator.
  - **PR-comment decoration**: SonarCloud posts inline comments on the PR diff automatically; local tools do not. Mitigation: add explicit Actions (e.g., `github/codeql-action` for the Security tab, `semgrep-action` with PR comments, or `marocchino/sticky-pull-request-comment`) to restore inline feedback.

### Option C — Keep SonarCloud (do nothing / status quo)
- **Sketch**: Retain SonarCloud as the sole quality gate; address the reliability rating by fixing the underlying C# issues (null refs, exception handling) identified via the SonarCloud dashboard, possibly guided by Roslyn analyzers locally but without changing the gate.
- **Appetite**: small (days to weeks, depending on issue count)
- **Trade-offs**: Wins zero migration effort and preserves dashboard/trends/PR decoration. Sacrifices control, offline capability, and keeps the opaque rating as the merge blocker. Risks: the reliability rating may remain flaky; no deterministic local gate; no SAST/coverage/duplication enforcement beyond what SonarCloud provides on its free tier.
- **Rabbit holes**: Chasing an opaque aggregate rating; SonarCloud rule tuning; dependence on cloud availability.

### Option D — Managed alternative (buy instead of build)
- **Sketch**: Replace SonarCloud with a managed, lower-cost alternative such as Codacy or DeepSource (free tiers for OSS/small teams), keeping a single SaaS but switching provider.
- **Appetite**: small (days)
- **Trade-offs**: Wins a single managed dashboard with less maintenance than Option B, and potentially better autofix/PR decoration. Sacrifices local/offline execution and introduces a new vendor and pricing model; still a cloud dependency and an opaque aggregate gate. Not aligned with the "local, native, OSS, free" intent.
- **Rabbit holes**: Vendor lock-in; free-tier limits; migration of historical data.

## Recommendation

Recommend **Option B — Composed local stack (full replacement)**. Rationale: the problem is not merely the opaque rating but the absence of a deterministic, local, offline quality gate; Option B is the only option that covers all Sonar dimensions without a cloud dependency, while keeping SonarCloud in parallel during a 2–4 week validation window mitigates the risk of losing dashboard/trend/PR-decoration value. Option A is a useful first increment (and is subsumed by B), Option C leaves the core problem unaddressed, and Option D contradicts the stated intent of local/OSS/free.

This recommendation is tied to the problem's success metrics: it creates a deterministic local gate with concrete file/line diagnostics (metrics 1–2), adds SAST/coverage/duplication enforcement (metrics 3–5), and removes cloud dependence (metric 6), while the parallel-run window protects against regressions in trend/PR value.

## Out of Scope (for the recommended option)

- Removing SonarCloud before the 2–4 week green-run validation (kept in parallel).
- Mutation testing (Stryker.NET) — optional, separate.
- Changing the `Build and Test` workflow's SMAPI dependency.
- Enterprise/compliance features (SAML, audit trails, portfolio management).
- Any gameplay, mod behavior, or feature changes.

## Assumptions to Validate

- The number of warnings surfaced by enabling the analyzers is small enough to fix in days (unknown; must be measured in Phase 1).
- `SonarAnalyzer.CSharp` and `NetAnalyzers` do not produce excessive false positives on this codebase (must be measured).
- The maintainer can tolerate a temporary period where both SonarCloud and local analyzers run (parallel cost: none, since both are free).
- Fixing the surfaced warnings will actually move the SonarCloud reliability rating to `A` (assumption; SonarCloud's rating logic is not fully transparent).
- Baseline measurements (coverage, duplication) can be captured before thresholds are set (ratchet approach).
- No LGPL-2.1 (Semgrep) or other license conflicts affect the mod's distribution (needs legal check).
- CodeQL's runtime is acceptable as a scheduled/deeper scan, with Semgrep CE as the fast per-PR gate.