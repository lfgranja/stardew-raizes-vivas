# Living Roots — Agent Guide

Stardew Valley agroecological mod (.NET 6, C# latest) via SMAPI. Layered DDD: `Domain/` (contracts + pure logic) → `Services/` (impls + persistence) → `Controllers/` (SMAPI handlers). Composition root: `LivingRoots/ModEntry.cs` (sole `new` site for services). Authoritative principles: `.specify/memory/constitution.md`. Detailed layer notes: `LivingRoots/AGENTS.md`, `LivingRoots/Domain/AGENTS.md`, `LivingRoots/Services/AGENTS.md`.

## Commands

```bash
# Build (needs game assemblies — see Prerequisite)
GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln --configuration Release

# Tests (all 3 projects)
dotnet test Stardew-LivingRoots.sln --configuration Release
# single subject
dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~SoilHealthServiceTests"

# Format
dotnet format Stardew-LivingRoots.sln --verify-no-changes
dotnet format Stardew-LivingRoots.sln

# Restore (locked)
dotnet restore --locked-mode

# Smoke suite (launch-free)
SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh
# single file
scripts/tests/run-all.sh test-cli.sh

# Quality gate (Python; merge-blocking check is "Quality Gate / gate")
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m pytest scripts/quality-gate/tests -q
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m quality_gate coverage --summary <path>/Summary.json --baseline .coverage-baseline
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m quality_gate duplication --report <path>/jscpd-report.json --baseline .jscpd-baseline.json
.venv-quality-gate/bin/semgrep scan --config p/csharp --error --severity ERROR --metrics off LivingRoots/
```

lefthook: pre-commit runs `dotnet format --verify-no-changes` + shell syntax/static checks; pre-push runs `dotnet test` + smoke suite.

## Prerequisite: game assemblies

`Pathoschild.Stardew.ModBuildConfig` refuses to build without a `GamePath` holding Stardew's managed assemblies. Local: set env `GamePath` (e.g. `/home/luis/stardew-assemblies` or the Steam install) or `<GamePath>` in `LivingRoots.csproj`. CI `Build and Test` stays dormant unless `BUILDDEPS_ENABLED=true` + `BUILDDEPS_TOKEN`; do not make `Check Formatting` required alone. Quality Gate (`Quality Gate / gate`) is merge-blocking.

## Architecture

- `LivingRoots/Constants.cs` — `ModConstants` is the single source of all magic numbers; no hardcoded tile coords/health values elsewhere.
- `LivingRoots/Domain/Interfaces/` (plus a few root `Domain/` contracts) — interfaces; `LivingRoots/Services/` — implementations. Pure domain services live in `LivingRoots/Domain/Services/`.
- `LivingRoots/Controllers/ModController.cs` — SMAPI event orchestrator; atomic bit-flag state machine; use `ExecuteWithConcurrencyGuard` for re-entrancy; snapshot data outside locks, then do I/O.
- `LivingRoots/Services/ModDataService.cs` — persistence + `SanitizePathSegments()` (path traversal, homoglyph, Unicode FormC normalization).
- `LivingRoots/Patches/HarvestPatches.cs` + seam `Domain/IHarvestQualityService.cs` / `Domain/Services/HarvestQualityService.cs` — Harmony patches via code API only (static methods, try/catch fallback).
- `LivingRoots.Tests/` — xUnit + Moq + FsCheck + Microsoft.Coyote; one file per subject `{Subject}Tests.cs`. Also `LivingRoots.Verification` / `.Verification.Runner`.

## Conventions (non-obvious)

- Sparse cache: never store default (0) health values.
- Async-only I/O: never `.Result`/`.Wait()`.
- No expression-bodied methods (`.editorconfig` forbids); overflow checking project-wide (`CheckForOverflowUnderflow=true`).
- `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true` — build is strict; restore with `--locked-mode`.
- Multiplayer is first-class (constitution Principle VI): farm state host-authoritative/farm-scoped; atomic per-tile/per-machine locks with first-wins; single-player is the degenerate case. No second source of truth.
- Security: never log raw exception messages; validate soil health 0–100 via `ModConstants`; sanitize all user-influenced paths.
- Constructor injection only; `ModEntry.cs` is the sole composition root; primary constructors (C# 12) on controllers.

## Anti-patterns

- `.Result`/`.Wait()` on async calls; holding a lock during I/O.
- Hardcoded tile coords or health values (use `ModConstants`).
- Storing zero health in runtime cache.
- Exposing raw exception messages in logs.
- Expression-bodied methods; suppressing overflow checks.
- `new`-ing services outside `ModEntry.cs`.

## Process

Process #6: after every file edit run `git add <file> && git commit -m "feat/fix/docs: <summary> — <files> — <why>"` (hook: `.specify/scripts/atomic-commit-hook.sh`).

## Verification

Gate 3 (end-to-end) applies to any change touching runnable code: real SMAPI session + 3 signals (soil-health JSON delta, crop phase/phaseDays delta, SMAPI log). Never claim "verified by build / N passed." See `VERIFICATION_PROTOCOL.md`.

## Where to look

| Task | Location |
|------|----------|
| New game event handler | `Controllers/ModController.cs` |
| New domain concept | `Domain/Interfaces/` (contract) + `Services/` (impl); pure rules in `Domain/Services/` |
| Soil health limits | `Constants.cs` (`ModConstants`) |
| New console command | `ModController.RegisterConsoleCommand()` |
| New render effect | `Services/Visualization/` via `IVisualizationService` (+ `Domain/Visualization/`) |
| Security validation | `ModDataService.SanitizePathSegments()` |
| Harmony harvest seam | `Patches/HarvestPatches.cs` + `Domain/IHarvestQualityService.cs` |
| Composition / wiring | `ModEntry.cs` |
| Nested layer notes | `LivingRoots/AGENTS.md`, `LivingRoots/Domain/AGENTS.md`, `LivingRoots/Services/AGENTS.md` |
