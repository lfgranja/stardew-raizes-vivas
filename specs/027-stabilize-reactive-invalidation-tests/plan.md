# Implementation Plan: Stabilize reactive invalidation tests against wall-clock flakiness

**Branch**: `feature/027-stabilize-reactive-invalidation-tests` | **Date**: 2026-11-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/027-stabilize-reactive-invalidation-tests/spec.md`

## Summary

Replace all four remaining reactive-invalidation `Stopwatch <= ModConstants.MaxRenderTimeMs` (`16.67ms`) assertions across `LivingRoots.Tests/SoilHealthVisualizationTests.cs` and `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` with deterministic synchronous-invalidation, next-frame rebuild, and bounded color-service consultation checks (`<= 1` consultation per visible tile), while documenting Gate 3 (`scripts/verify-scenario.sh`) as owner of wall-clock frame timing. Zero production code changes.

## Technical Context

**Language/Version**: C# latest, .NET 6 (`LivingRoots.Tests`)
**Primary Dependencies**: xUnit, Moq, FsCheck.Xunit 2.16.6, `Microsoft.Xna.Framework`
**Storage**: N/A (test-only change)
**Testing**: `dotnet test Stardew-LivingRoots.sln --configuration Release -m:1` + `scripts/release-gate.sh` + `scripts/ci/aggregate-gate.sh` (with local `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley"`)
**Target Platform**: Linux / GitHub Actions headless test runner
**Project Type**: SMAPI mod test suite (`LivingRoots.Tests`)
**Performance Goals**: 100% deterministic pass/fail verdict independent of host CPU/GC/JIT jitter (`SC-001`–`SC-003`)
**Constraints**: `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, block-bodied methods only (`.editorconfig`), no unused `using` directives, zero edits under `LivingRoots/`
**Scale/Scope**: 4 test methods + 1 helper across 2 Green-Zone files in `LivingRoots.Tests/`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment |
|---|---|
| I. Domain-Driven Design | Pass — zero domain/service/controller boundaries crossed; test project only |
| II. Security-First Data Handling | Pass — no filesystem paths, save parsing, or soil-health bounds altered |
| III. Async-Only Concurrency | Pass — synchronous unit/property tests only; no blocking async calls added |
| IV. Test-Driven Development | Pass — RED observed in `release-gate.sh` Step 4 (`27.346ms > 16.67ms`), isolated repro recorded, deterministic invariants replace flaky clock assertions |
| V. Simplicity and YAGNI | Pass — minimal, direct edits inside the 2 named test files |
| VI. Multiplayer As A First-Class Requirement | Pass — no shared farm state or multiplayer paths touched |
| Gate 1 / Gate 2 / Gate 3 | Gate 1 & Gate 2 executed via `scripts/release-gate.sh`; **Gate 3 N/A** (zero runnable code under `LivingRoots/` modified) |

## Project Structure

### Documentation (this feature)

```text
specs/027-stabilize-reactive-invalidation-tests/
├── spec.md              # Feature specification
├── clarify.md           # R3 clarification record (0/3 used)
├── plan.md              # This file
├── research.md          # Phase 0 audit & root-cause analysis
├── data-model.md        # Phase 1 test observable entities
├── quickstart.md        # Deterministic verification steps
├── checklists/          # Requirements quality checklist
├── tasks.md             # Dependency-ordered implementation tasks
├── analyze.md           # Read-only cross-artifact analysis report
├── test.md              # Captured test execution outputs
└── converge.md          # Append-only traceability convergence record
```

### Source Code (repository root)

```text
LivingRoots.Tests/
├── SoilHealthVisualizationTests.cs                  # 3 reactive-invalidation methods updated + unused System.Diagnostics removed
└── Invariants/
    └── VisualizationInvariantsTests.cs              # T031b updated + tracked renderer helper + unused System.Diagnostics removed
```

**Structure Decision**: Strictly confined to the 2 Green-Zone test files above (`docs/governance/brownfield-zones.md`). `LivingRoots/`, `VisualizationPerformanceTests.cs`, `GrowthPerformanceTests.cs`, and `VisualizationConstantsTests.cs` remain untouched.

## Complexity Tracking

No constitution violations. No new abstractions or production dependencies introduced.
