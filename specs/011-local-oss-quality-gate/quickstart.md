# Quickstart: Local OSS Quality Gate

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29

Validation guide for the quality gate. Run these locally or in CI to confirm the feature works.

## Prerequisites
- .NET 6+ SDK (10.x works with roll-forward).
- Stardew Valley installed (for build) OR use the build-deps token.
- (Optional) `jscpd` via `npx`, `semgrep` CLI, `reportgenerator` tool.

## 1. Analyzer gate (Phase 1)

```bash
# Add analyzers to both csproj (see data-model.md Analyzer Configuration)
# Then:
dotnet build Stardew-LivingRoots.sln --nologo
# Expected: 0 errors on clean code; any reliability defect fails with file:line.
```

Validation: introduce a deliberate null dereference → build fails naming file/line → revert.

## 2. Semgrep (fast SAST)

```bash
semgrep --config p/csharp LivingRoots/
# or via GitHub Action
```

Expected: completes in seconds; 0 high/critical findings on clean code.

## 3. CodeQL (deep SAST, scheduled)

Run via GitHub Actions (scheduled). Requires `BUILDDEPS_TOKEN` to fetch Stardew assemblies for the build step. Expected: completes in 3–5 min; results in Security tab.

## 4. Coverage baseline & ratchet

```bash
dotnet test Stardew-LivingRoots.sln --collect:"XPlat Code Coverage" --results-directory ./coverage
# Parse coverage.cobertura.xml -> line% / branch%
# Record in .coverage-baseline
```

Validation: drop coverage below floor → CI fails; raise coverage → baseline ratchets up.

## 5. Duplication baseline & ratchet

```bash
npx jscpd LivingRoots/ --min-lines 5 --min-tokens 50
# Record duplication % in .jscpd-baseline.json
```

Validation: introduce a clone → CI fails (new clone); existing clones tolerated.

## 6. Full quality gate (CI)

Push a PR and confirm `.github/workflows/quality-gate.yml` runs all steps on a standard runner without `STARDEW_GAME_PATH` (except CodeQL scheduled run).

## Success criteria check
- [ ] SC-001: job runs on standard runner in bounded time.
- [ ] SC-002: `dotnet build` passes clean; defect fails with file:line.
- [ ] SC-003: 0 high/critical security findings on clean PR.
- [ ] SC-004: coverage regression fails.
- [ ] SC-005: duplication regression fails.
- [ ] SC-006: file/line diagnostics available from CI.
- [ ] SC-007: after 2–4 weeks green, SonarCloud removed.