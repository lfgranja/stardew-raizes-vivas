# Problem Definition: Quality-gate reliability and dependency on a cloud SaaS for code analysis

- **Slug**: replace-sonarqube-lightweight-local-oss
- **Created**: 2026-10-29
- **Inputs used**: intake.md (yes) | research.md (yes) | user input (yes)

## Problem Statement

The maintainer cannot reliably get pull requests and merges to pass the code-quality gate: SonarCloud's "Reliability Rating on New Code" repeatedly scores `C` against a required `A` (e.g., run 123455233388 after the merge of PR #518), and the rating is an opaque, cumulative signal rather than a concrete, actionable failure. At the same time, the only alternative automated build/test gate (`Build and Test`) is dormant because it requires a Stardew Valley + SMAPI install, so there is no dependable, local, deterministic quality gate that can run on a standard CI runner. The underlying pain is the absence of a fast, offline, deterministic quality gate for this .NET 6 mod; the SonarCloud dependency is the current (failing) embodiment of it.

## Affected Users & Stakeholders

- **Users**: The maintainer (`lfgranja`) — blocked from merging/releasing when the SonarCloud gate fails; unable to get concrete, file-level feedback without consulting an external dashboard.
- **Stakeholders**: Contributors/PR authors — PRs stall on an opaque reliability rating; CI feedback arrives late and depends on a cloud service.
- **Stakeholders**: Downstream players (mod users) — indirectly affected, since blocked PRs delay fixes and features; no direct user-facing impact identified. [NEEDS CLARIFICATION: any external contributors or users affected beyond the maintainer?]

## Goals

- Provide a deterministic, local quality gate that runs on a standard CI runner without game/SMAPI dependencies and fails the build on concrete, file-level defects (null references, unhandled exceptions, security findings, duplication above threshold, coverage below threshold).
- Give the maintainer actionable, in-build diagnostics instead of an opaque aggregate rating.
- Reduce dependence on a single cloud SaaS for code-quality enforcement, so analysis can run offline and is not blocked by network/service availability.
- (Conditional) Preserve or replace the value SonarCloud currently provides (trend history, PR decoration, SAST depth) only if the maintainer needs them.

## Non-Goals

- This assessment does not mandate removing SonarCloud; keeping it in parallel during a transition is acceptable.
- No new game features, gameplay changes, or mod behavior changes are in scope.
- No enterprise/compliance capabilities (SAML, audit trails, portfolio management) are required for this single-maintainer OSS project.
- No commitment to mutation testing (Stryker.NET) in this problem definition; it is an optional later enhancement.
- No redesign of the build system to remove the SMAPI dependency from `Build and Test` (that is a separate concern).

## Success Metrics

- A new lightweight CI job (analyzers + SAST + coverage + duplication) runs on every PR/push on a standard runner without `STARDEW_GAME_PATH` and completes in a bounded time (target: under a few minutes). Baseline: no such job exists today (the only analysis job is SonarCloud, cloud-dependent).
- Zero new reliability defects of severity ≥ "warning treated as error" reach `dev` (i.e., `dotnet build` with `TreatWarningsAsErrors` passes). Baseline: unknown — to be measured in Phase 1 (see research.md gaps).
- Security findings (Semgrep/CodeQL) reported as 0 high/critical on new code. Baseline: unknown (no local SAST today).
- Duplication percentage below an agreed threshold (e.g., 5%). Baseline: unknown.
- Coverage at or above an agreed threshold. Baseline: unknown (no coverage gate today).
- Maintainer can identify the offending file/line from CI output without opening an external dashboard. Baseline: not possible today (SonarCloud rating only).

## Cost of Inaction

- The SonarCloud reliability gate continues to block merges unpredictably, delaying fixes and features.
- The dormant `Build and Test` workflow leaves no automated quality safety net on standard runners.
- Reliability defects (null refs, unhandled exceptions) may accumulate uncaught, because the only signal is an opaque aggregate rating that does not point at specific code.
- Continued dependence on a cloud SaaS for a core quality gate, with no offline fallback.

## Open Questions

- [NEEDS CLARIFICATION: How many Roslyn analyzer warnings become errors when enabling `TreatWarningsAsErrors` on `LivingRoots.csproj`/`LivingRoots.Tests.csproj`? (Requires a local build run; not yet measured.)]
- [NEEDS CLARIFICATION: Does the maintainer require historical trend dashboards and inline PR decoration, or is build-fail sufficient?]
- [NEEDS CLARIFICATION: Is CodeQL's slower analysis acceptable in CI for this small codebase, or should Semgrep CE be the primary SAST with CodeQL as a scheduled deep scan?]
- [NEEDS CLARIFICATION: What coverage and duplication thresholds would be acceptable for this project?]
- [NEEDS CLARIFICATION: Should SonarCloud be removed entirely or kept in parallel during a transition period?]
- [NEEDS CLARIFICATION: Are there any license/compliance constraints (e.g., LGPL-2.1 Semgrep engine) that conflict with the mod's distribution?]
- [NEEDS CLARIFICATION: Are there external contributors or users beyond the maintainer who are affected?]