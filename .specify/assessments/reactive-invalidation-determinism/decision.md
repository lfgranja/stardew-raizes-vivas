# Decision: Stabilize reactive invalidation tests against wall-clock flakiness

- **Slug**: reactive-invalidation-determinism
- **Decided**: 2026-11-19
- **Verdict**: go
- **Artifacts reviewed**: intake.md | research.md | problem.md | concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Authoritative `scripts/release-gate.sh` failed Step 4 (exit 14) on `8bba3fe` at `SoilHealthVisualizationTests.cs:419` (`27.346ms > 16.67ms`), while isolated 5x reruns passed 5/5. |
| Evidence strength | strong | Exact command logs, isolated 5/5 rerun, complete `grep` audit of all `Stopwatch` / `16.67ms` assertions across `LivingRoots.Tests/`, and Spec 026 precedent. |
| Value vs. inaction | strong | Restores deterministic execution of `release-gate.sh` and protects required `Build and Test` check from 4 remaining wall-clock hazards. |
| Feasibility / appetite | strong | Small Green-Zone change strictly inside 2 test files; zero production C# touched. |
| Strategic fit | strong | Aligns directly with Spec 026 (`026-stabilize-overlay-cache`) and Constitution Principles IV (TDD determinism) and V (Simplicity/YAGNI). |
| Risk posture | strong | Zero `LivingRoots/` diff means Gate 3 is N/A; synchronous invalidation + next-frame bounded rebuild preserves FR-010/FR-009 semantics. |

## Verdict & Rationale

**go.** All criteria rate `strong`, evidence is directly executed and verified on `8bba3fe`, and Option A cleanly eliminates the observed `release-gate.sh` failure and its 3 sibling hazards in 2 Green-Zone test files without touching production code or requiring any owner-gated action.

## If go — Handoff to `/speckit-specify`

- **Problem**: Four reactive-invalidation tests across `LivingRoots.Tests/SoilHealthVisualizationTests.cs` and `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` assert un-warmed `Stopwatch <= ModConstants.MaxRenderTimeMs` (`16.67ms`), causing intermittent false-RED failures in `release-gate.sh` and `Build and Test`.
- **Chosen approach**: Option A — replace wall-clock checks in all 4 tests with deterministic synchronous invalidation (`IsCacheEmpty == true`), zero extra consultations during invalidation, and immediate next-frame rebuild bounded to `<= 1` consultation per visible tile, explicitly documenting Gate 3 ownership of wall-clock 16.67ms frame timing.
- **In scope / out of scope**: In scope = those 4 test methods + unused `using System.Diagnostics;` cleanup + SDD trail. Out of scope = `LivingRoots/` production files, `VisualizationPerformanceTests.cs`, `GrowthPerformanceTests.cs`, CI workflow edits, owner-gated release/D1/Gate 3 actions.
- **Success metrics**: 0 `Stopwatch` / `MaxRenderTimeMs` timing assertions in those 2 files; 5/5 consecutive passes for updated tests; `scripts/release-gate.sh` ALL STEPS PASSED; `scripts/ci/aggregate-gate.sh` ALL STEPS PASSED; empty `LivingRoots/` diff.
- **Carried-forward open questions**: none blocking; separate runtime performance benchmark remains deferred debt.
