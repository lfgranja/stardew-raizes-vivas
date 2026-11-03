# Living Roots — Agent Guide

Stardew Valley agroecological mod (.NET 6 / C# latest) via SMAPI. DDD layers: `Domain/` (interfaces, models, logic) → `Services/` (impls, persistence) → `Controllers/` (SMAPI event handlers). Composition root: `LivingRoots/ModEntry.cs`.

## Commands

```bash
# Build (requires game assemblies — see below)
dotnet build Stardew-LivingRoots.sln --configuration Release

# Tests
dotnet test Stardew-LivingRoots.sln --configuration Release
dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --configuration Release
dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~SoilHealthServiceTests"

# Format
dotnet format Stardew-LivingRoots.sln --verify-no-changes
dotnet format Stardew-LivingRoots.sln

# Quality gate (Python)
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m pytest scripts/quality-gate/tests -q
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m quality_gate coverage --summary <path>/Summary.json --baseline .coverage-baseline
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m quality_gate duplication --report <path>/jscpd-report.json --baseline .jscpd-baseline.json
.venv-quality-gate/bin/semgrep scan --config p/csharp --error --severity ERROR --metrics off LivingRoots/

# Smoke suite (launch-free)
SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh
```

lefthook: pre-commit runs `dotnet format --verify-no-changes` + shell syntax/static checks; pre-push runs `dotnet test` + smoke suite.

## Prerequisite: game assemblies

`Pathoschild.Stardew.ModBuildConfig` refuses to build without a `GamePath` holding Stardew's managed assemblies. Local: set `<GamePath>` in `LivingRoots.csproj` (or env `GamePath`). CI `Build and Test` stays dormant unless `BUILDDEPS_ENABLED=true` + `BUILDDEPS_TOKEN`; do not make `Check Formatting` required alone — it would satisfy branch protection with zero tests. Quality Gate (`Quality Gate / gate`) is the merge-blocking check.

## Architecture

- `LivingRoots/Constants.cs` — `ModConstants` is the single source of all magic numbers; no hardcoded tile coords/health values elsewhere.
- `LivingRoots/Domain/` — interfaces in `Domain/`, implementations in `Services/` (DDD).
- `LivingRoots/Controllers/ModController.cs` — SMAPI event orchestrator; atomic bit-flag state machine; use `ExecuteWithConcurrencyGuard` for re-entrancy; snapshot data outside locks, then do I/O.
- `LivingRoots/Services/ModDataService.cs` — persistence + `SanitizePathSegments()` (path traversal, homoglyph, Unicode FormC normalization).
- `LivingRoots/Patches/` — Harmony patches via code API only (static methods, try/catch fallback); patch logic lives in `HarvestQualityService` seam.
- `LivingRoots.Tests/` — xUnit + Moq + FsCheck; one file per subject `{Subject}Tests.cs`.

## Conventions (non-obvious)

- Sparse cache: never store default (0) health values.
- Async-only I/O: never `.Result`/`.Wait()`.
- No expression-bodied methods (`.editorconfig` forbids); overflow checking on project-wide (`CheckForOverflowUnderflow=true`).
- `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true` — build is strict.
- Multiplayer is first-class (constitution Principle VI): farm state is host-authoritative/farm-scoped; atomic per-tile/per-machine locks with first-wins; single-player is the degenerate case. No second source of truth.
- Security: never log raw exception messages; validate soil health 0–100 via `ModConstants`.

## Anti-patterns

- `.Result`/`.Wait()` on async calls.
- Hardcoded tile coords or health values (use `ModConstants`).
- Storing zero health in runtime cache.
- Exposing raw exception messages in logs.
- Expression-bodied methods.
- Suppressing overflow checks.

## Process

Process #6: after every file edit run `git add <file> && git commit -m "feat/fix/docs: <summary> — <files> — <why>"` (hook: `.specify/scripts/atomic-commit-hook.sh`).

## Verification

Gate 3 (end-to-end) applies to any change touching runnable code: real SMAPI session + 3 signals (soil-health JSON delta, crop phase/phaseDays delta, SMAPI log). Never claim "verified by build / N passed." See `VERIFICATION_PROTOCOL.md`.

## Where to look

| Task | Location |
|------|----------|
| New game event handler | `Controllers/ModController.cs` |
| New domain concept | `Domain/` (interface) + `Services/` (impl) |
| Soil health limits | `Constants.cs` (`ModConstants`) |
| New console command | `ModController.RegisterConsoleCommand()` |
| New render effect | `Services/Visualization/` via `IVisualizationService` |
| Security validation | `ModDataService.SanitizePathSegments()` |
| Harmony harvest seam | `Domain/HarvestQualityService` + `Patches/` |
