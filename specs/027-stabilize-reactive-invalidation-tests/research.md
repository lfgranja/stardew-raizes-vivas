# Phase 0 Research: Stabilize reactive invalidation tests against wall-clock flakiness

- **Feature**: `027-stabilize-reactive-invalidation-tests`
- **Date**: 2026-11-19

## Summary of Findings

1. **Observed failure in authoritative gate**: On `dev` `8bba3fe`, `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" bash scripts/release-gate.sh` failed Step 4 (`exit 14`, 1998 passed / 1 failed / 4 skipped / 2003) at `LivingRoots.Tests/SoilHealthVisualizationTests.cs:419` (`OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame`): `LATENCY DEFECT: reactive cache invalidation took 27,346ms, which exceeds the frame budget of 16,67ms.`
2. **Isolation proof**: Running that exact test 5 times in isolation passed 5/5 (1–2s per `dotnet test` invocation), confirming host/GC/JIT load timing rather than production cache defects.
3. **Complete repository audit (`grep -rn`)**: Exactly four reactive-invalidation tests in `LivingRoots.Tests/` assert `Stopwatch.Elapsed.TotalMilliseconds <= ModConstants.MaxRenderTimeMs`:
   - `SoilHealthVisualizationTests.cs:84-96` (`OnTileHealthChanged_SubscribedHandler_InvalidatesOverlayCacheImmediatelyWithinFrameBudget`)
   - `SoilHealthVisualizationTests.cs:407-420` (`OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame`)
   - `SoilHealthVisualizationTests.cs:663-674` (`T143_OverlayDrawableOnly_SuppressesFabricated_UnknownFloorAt1_ReactsWithinFrameBudget`)
   - `Invariants/VisualizationInvariantsTests.cs:319-324` (`T031b_OverlayTiming_ReactiveInvalidation_WithinFrameBudget`, `[Property(MaxTest = 50)]`)
4. **System.Diagnostics usage**: Only `Stopwatch` in those four methods uses `System.Diagnostics` in either file; both `using System.Diagnostics;` lines must be removed to satisfy `dotnet format` / IDE0005 under `TreatWarningsAsErrors=true`.
5. **Untouched siblings**: `VisualizationPerformanceTests.cs` (warmed 1,000-iteration loop), `GrowthPerformanceTests.cs` (comparative linear vs chunked scan), and `VisualizationConstantsTests.cs` (`16.67` constant check) are distinct and preserved unchanged.
