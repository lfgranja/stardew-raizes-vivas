# Implementation Plan: Stabilize overlay cache invalidation property

**Branch**: `026-stabilize-overlay-cache` | **Date**: 2026-11-18 | **Spec**: [spec.md](spec.md)

**Input**: spec.md + decision go (`.specify/assessments/overlay-cache-interleaving/decision.md`)

## Summary

Replace the wall-clock elapsed-time assertion in one FsCheck property with a deterministic per-frame work bound. No production code changes; Gate 3 real-SMAPI evidence is not owed.

## Technical Context

- **Language/version**: C# latest, .NET 6 (target of `LivingRoots.Tests`)
- **Primary dependencies**: xUnit, Moq, FsCheck.Xunit 2.16.6, Microsoft.Xna.Framework
- **Storage**: none (test-only change)
- **Testing**: `dotnet test Stardew-LivingRoots.sln -c Release` with `GamePath=/home/luis/stardew-assemblies` on this host
- **Target platform**: test runner (Linux); no runtime game change
- **Project type**: single test-file modification inside an SMAPI mod solution
- **Performance goals**: verdict independent of runner load (SC-001); bounded consultations per frame (FR-003)
- **Constraints**: no expression-bodied methods; overflow checking project-wide; `TreatWarningsAsErrors=true`; one test file per subject

## Constitution Check

| Principle | Assessment |
|-----------|------------|
| I. DDD layering | Pass — no layer crossed; test file only |
| II. Security-first | Pass — no input paths, no health values changed |
| III. Async-only concurrency | Pass — no async code touched |
| IV. TDD | Pass — RED reproduced and diagnosed before edit |
| V. Simplicity/YAGNI | Pass — smallest change that removes nondeterminism |
| VI. Multiplayer first-class | Pass — no shared farm state touched; `PerScreen` behavior untouched |

Gate 3: N/A (no runnable-code change). Gate 1/2 apply and are executed in Bloco 7.

## Phase 0: Research

`research.md` resolves the single technical decision (deterministic work bound vs. widened wall-clock threshold) with instrumented evidence. No NEEDS CLARIFICATION remain.

## Phase 1: Design

- `data-model.md`: entities are test observations (interleaving event, visible tile, consultation count, frame-work bound).
- `contracts/`: skipped — the feature is purely internal to one test file; no external interface.
- `quickstart.md`: deterministic validation recipe (5 property runs + full suite + diff scope check).

## Phase 2: Implementation outline

1. RED: reproduced failure with `DIAG:budget`; full suite baseline 1 failed / 2003.
2. Edit `VisualizationCacheInvalidationInvariantsTests.cs`: remove `Stopwatch` timing and its `using`; count consultations per enabled frame and assert `<= VisibleTileCount`; update XML doc to point wall-clock budget to Gate 3.
3. GREEN: property 5/5 runs; full suite green; `dotnet format --verify-no-changes`.
4. Commit, converge (append-only), R4 validate, R7 release.

## Complexity Tracking

No violations. No new abstractions, no production change.
