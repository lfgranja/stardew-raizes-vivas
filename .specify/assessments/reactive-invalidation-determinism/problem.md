# Problem Definition: Stabilize reactive invalidation tests against wall-clock flakiness

- **Slug**: reactive-invalidation-determinism
- **Created**: 2026-11-19
- **Inputs used**: intake.md | research.md

## Problem Statement

The authoritative local release gate (`scripts/release-gate.sh`) failed Step 4 on `8bba3fe` (`exit 14`, 1998 passed / 1 failed / 4 skipped) because `SoilHealthVisualizationTests.OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame` timed a synchronous cache invalidation with `Stopwatch` and failed at `27.346ms > 16.67ms`. Four reactive-invalidation unit/property assertions across `SoilHealthVisualizationTests.cs` and `VisualizationInvariantsTests.cs` decide correctness via un-warmed wall-clock thresholds (`<= ModConstants.MaxRenderTimeMs`), making `release-gate.sh` and the required `Build and Test` check sensitive to host CPU/GC/JIT jitter rather than actual cache behavior.

## Affected Users & Stakeholders

- **Users**: maintainer and CI/release pipeline — false-red Step 4 blocks release verification and consumes >20 minutes per full gate cycle.
- **Stakeholders**: repository owner (`lfgranja`) — relies on deterministic `release-gate.sh` and required `Build and Test` / `sdd-gate` checks.

## Goals

- Eliminate all wall-clock (`Stopwatch <= ModConstants.MaxRenderTimeMs`) assertions from the four reactive-invalidation tests in `SoilHealthVisualizationTests.cs` and `VisualizationInvariantsTests.cs`.
- Preserve strong, deterministic FR-010/FR-009 invariants: synchronous invalidation before update returns, zero redundant color lookups during invalidation, immediate next-frame rebuild reflecting updated state, and `<= 1` color-service consultation per visible tile on rebuild.
- Confine edits strictly to `LivingRoots.Tests/` + SDD/docs (zero `LivingRoots/` production diff; Gate 3 N/A).

## Non-Goals

- Modifying any production code under `LivingRoots/`.
- Removing `ModConstants.MaxRenderTimeMs` (`16.67`), `VisualizationConstantsTests`, warmed `VisualizationPerformanceTests`, or `GrowthPerformanceTests`.
- Executing owner-gated actions (tags, public releases, Nexus upload, branch protection, D1 live session, human Gate 3 attestation).

## Success Metrics

- Zero `Stopwatch` or `<= ModConstants.MaxRenderTimeMs` assertions remain in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` and `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs`.
- The 4 updated tests pass **5/5** consecutive local runs.
- `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" bash scripts/release-gate.sh` passes **ALL STEPS PASSED** (exit 0, 1999 passed / 0 failed / 4 skipped / 2003 total).
- `bash scripts/ci/aggregate-gate.sh` passes **ALL STEPS PASSED** (exit 0).
- `git diff --name-only origin/dev -- LivingRoots/` is empty.

## Cost of Inaction

`scripts/release-gate.sh` remains unreliable under normal machine load, repeating the exact failure class that Spec 026 only resolved in one of the five wall-clock reactive-invalidation tests.

## Open Questions

- None blocking; separate dedicated performance benchmarks outside unit/property gates remain deferred debt (consistent with Spec 026).
