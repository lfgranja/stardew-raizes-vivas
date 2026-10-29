# Contract: Quality Gate CI Workflow

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29

## Workflow: `.github/workflows/quality-gate.yml`

**Trigger**: `pull_request` (all) and `push` to `dev`/`main`.

**Runner**: `ubuntu-latest` (standard; no game/SMAPI dependency).

## Steps (contract)

| # | Step | Tool | Blocking? | Notes |
|---|---|---|---|---|
| 1 | Checkout | actions/checkout | yes | |
| 2 | Setup .NET | actions/setup-dotnet | yes | net6.0 |
| 3 | Restore & Build (analyzers) | `dotnet build` | yes | Analyzers run; warnings-as-errors. No game assemblies needed. |
| 4 | Semgrep (SAST) | semgrep/semgrep-action | yes | Fast (~10s); rules `p/csharp`. |
| 5 | CodeQL (SAST) | github/codeql-action | scheduled only | Deep scan; nightly/weekly or on merge to main. |
| 6 | Dependency scan (SCA) | OWASP Dependency-Check or Trivy | yes | Fail on high/critical. |
| 7 | Test + Coverage | `dotnet test --collect:"XPlat Code Coverage"` | yes | Coverlet. |
| 8 | ReportGenerator | reportgenerator | yes | Produces coverage summary. |
| 9 | Coverage ratchet check | script | yes | Fail if coverage < floor. |
| 10 | jscpd (duplication) | kucherenko/jscpd | yes | Baseline/new-clone mode; fail on new clones. |
| 11 | Duplication ratchet check | script | yes | Fail if duplication > ceiling. |

## Thresholds (initial, from baseline)

| Metric | Direction | Initial floor/ceiling | Ratchet |
|---|---|---|---|
| Line coverage | min | 79.73% (measured baseline) | up |
| Branch coverage | min | 70.78% (measured baseline) | up |
| Duplication (C# lines) | max | 2.37% (measured baseline) | down |
| Security findings (high/critical) | max | 0 | n/a |
| Reliability (analyzer warnings) | max | 0 (warnings-as-errors) | n/a |

## Exit codes / failure behavior
- Any failing step fails the job and blocks merge (branch protection can require this check).
- Diagnostics include file/line for analyzer findings (FR-011).
- Suppressions must be documented (FR-012).

## Inputs / secrets
- None required for the fast gate (Semgrep, analyzers, coverage, jscpd).
- CodeQL scheduled run requires `BUILDDEPS_TOKEN` (same as Build and Test) to fetch Stardew assemblies for the build step.

## Out of scope for this contract
- Mutation testing (Stryker.NET) — optional, separate.
- Inline PR comment decoration — not required (build-fail sufficient).