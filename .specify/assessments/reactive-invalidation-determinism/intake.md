# Idea Intake: Stabilize reactive invalidation tests against wall-clock flakiness

- **Slug**: reactive-invalidation-determinism
- **Created**: 2026-11-19
- **Source**: repo paths `LivingRoots.Tests/SoilHealthVisualizationTests.cs` & `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` + `scripts/release-gate.sh` exit 14 run on `8bba3fe` (2026-11-19T19:50:27Z–20:12:41Z)
- **Type**: fix

## Idea (as captured)

> "Authoritative target gate `scripts/release-gate.sh` failed Step 4 (exit 14) on `8bba3fe` with 1998 passed / 1 failed / 4 skipped (2003): `LivingRoots.Tests.SoilHealthVisualizationTests.OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame` failed with `LATENCY DEFECT: reactive cache invalidation took 27,346ms, which exceeds the frame budget of 16,67ms.` An isolated 5x rerun passed 5/5 (1–2s each), while audit grep across `LivingRoots.Tests/` shows four remaining reactive-invalidation `Stopwatch <= ModConstants.MaxRenderTimeMs` assertions across `SoilHealthVisualizationTests.cs` (lines 95, 419, 673) and `VisualizationInvariantsTests.cs` (line 324)."

## Restated

Eliminate wall-clock elapsed-time (`<= 16.67ms`) flakiness in the remaining reactive overlay cache-invalidation tests in `LivingRoots.Tests` so the authoritative `scripts/release-gate.sh` and required `Build and Test` check evaluate deterministic cache behavior rather than runner CPU/GC/JIT jitter.

## Origin & Context

- **Raised by**: autonomous orchestration cycle 027 (BLOCO 0.b / BLOCO 5, candidate C1, score 12.50/25 = 50.0%).
- **Trigger**: fresh execution of `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" bash scripts/release-gate.sh` on `dev` `8bba3fe` exited 14 at Step 4 due to `SoilHealthVisualizationTests.cs:419` exceeding `16.67ms` (`27.346ms`) under full-suite execution load.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: which exact assertions inside `SoilHealthVisualizationTests.cs` and `VisualizationInvariantsTests.cs` enforce wall-clock timing versus real cache-invalidation correctness?]
- [NEEDS CLARIFICATION: are other `Stopwatch` tests in `LivingRoots.Tests/` (`VisualizationPerformanceTests.cs`, `GrowthPerformanceTests.cs`) affected or distinct by design?]
- [NEEDS CLARIFICATION: what deterministic invariant preserves FR-010/FR-009 semantics without modifying production `LivingRoots/` code?]
