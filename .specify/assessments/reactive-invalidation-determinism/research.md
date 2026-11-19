# Idea Research: Stabilize reactive invalidation tests against wall-clock flakiness

- **Slug**: reactive-invalidation-determinism
- **Created**: 2026-11-19
- **Evidence confidence (overall)**: high

## Users & Demand

- Direct affected stakeholder: maintainer and CI pipeline. `scripts/release-gate.sh` is the local authoritative production gate, and `Build and Test` is a **required** status check on `dev` (`strict: true`, verified live via `gh api` on 2026-11-19). A wall-clock test failure turns a green codebase red on Step 4 (`exit 14`). [source: `scripts/release-gate.sh`, live `gh api` | confidence: high]
- End-user impact: no production behavior defect is implicated; this addresses gate reliability and verification honesty. [source: `SoilHealthVisualizationTests.cs:407-420`, isolated 5x rerun | confidence: high]

## Prior Art

- **Spec 026 (`026-stabilize-overlay-cache`, PR #803, merged at `8214fb9`)**: replaced the wall-clock `Stopwatch <= ModConstants.MaxRenderTimeMs` assertion in `VisualizationCacheInvalidationInvariantsTests.RenderOverlays_ForAnyDisableEnableInterleaving_RebuildsOnFirstFrameAfterReEnableWithinBudget` with a deterministic per-frame work bound (`<= VisibleTileCount`), explicitly assigning wall-clock 16.67ms frame budget evidence to Gate 3 (`scripts/verify-scenario.sh`). [source: `specs/026-stabilize-overlay-cache/spec.md`, `.specify/releases/026-stabilize-overlay-cache/release.md` | confidence: high]
- **Spec 010 (`scripts/verify-scenario.sh`)**: real-game headless verification runner owns end-to-end runtime/frame-budget evidence (Gate 3). [source: `specs/010-headless-verification-runner/` | confidence: high]
- **Brownfield Zone Map**: `LivingRoots.Tests/` is explicitly classified as **Green Zone** ("tight loop: agent edits, dotnet test after each change, no shims"), whereas `LivingRoots/Controllers/` and `LivingRoots/Services/` are Red/Yellow Zones. [source: `docs/governance/brownfield-zones.md:12-23,54-60` | confidence: high]

## Market & Context

- Internal verification determinism issue; no external market dimension applies. Cost of inaction: `scripts/release-gate.sh` can fail non-deterministically under load (observed 2026-11-19T20:12Z on clean `8bba3fe`, blocking release verification for >20 minutes per run). [source: `release-gate.sh` task-25 output | confidence: high]

## Data & Constraints

- **Observed failure (2026-11-19T19:50:27Z–20:12:41Z on `8bba3fe`)**:
  `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" bash scripts/release-gate.sh` passed Steps 1–3 (`dotnet format`, `dotnet restore --locked-mode`, `dotnet build -c Release -m:1 --disable-build-servers`) and failed Step 4 (`exit 14`) with `1 failed, 1998 passed, 4 skipped (2003)`:
  `LivingRoots.Tests.SoilHealthVisualizationTests.OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame` (`LivingRoots.Tests/SoilHealthVisualizationTests.cs:419`) — `LATENCY DEFECT: reactive cache invalidation took 27,346ms, which exceeds the frame budget of 16,67ms.` [source: `release-gate.sh` run output | confidence: high]
- **Isolated 5x reproduction (2026-11-19)**: running the exact failing test 5 times in isolation (`--filter "FullyQualifiedName~OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame"`) passed **5/5** (1–2s each), confirming CPU/GC/JIT scheduling variance under full-suite execution rather than an algorithmic latency bug. [source: `dotnet test` 5x loop | confidence: high]
- **Complete `Stopwatch` / `16.67ms` audit across `LivingRoots.Tests/*.cs`**:
  1. `SoilHealthVisualizationTests.cs:84-96` (`OnTileHealthChanged_SubscribedHandler_InvalidatesOverlayCacheImmediatelyWithinFrameBudget`) — measures cold lambda `handler` call (`_overlayRenderer.InvalidateCache()`) with `Stopwatch <= ModConstants.MaxRenderTimeMs`.
  2. `SoilHealthVisualizationTests.cs:407-420` (`OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame`) — measures `soilHealthService.UpdateHealth(...)` with `Stopwatch <= ModConstants.MaxRenderTimeMs` (**failed today at 27.346ms**).
  3. `SoilHealthVisualizationTests.cs:663-674` (`T143_OverlayDrawableOnly_SuppressesFabricated_UnknownFloorAt1_ReactsWithinFrameBudget`) — measures cold lambda `handler` calling `service.SetTileHealthData(...)` with `Stopwatch <= ModConstants.MaxRenderTimeMs`.
  4. `Invariants/VisualizationInvariantsTests.cs:319-324` (`T031b_OverlayTiming_ReactiveInvalidation_WithinFrameBudget`, `[Property(MaxTest = 50)]`) — measures `renderer.InvalidateCache()` inside 50 FsCheck iterations with `Stopwatch <= ModConstants.MaxRenderTimeMs` (same class as Spec 026).
  5. **Distinct / out-of-scope**: `Visualization/VisualizationPerformanceTests.cs:19-38` warms up 1,000 iterations before measuring 1,000 pure `ColorInterpolationService.GetColorForHealth` calls; `GrowthPerformanceTests.cs:236,275` compares linear vs chunked scan ticks; `VisualizationConstantsTests.cs:79` pins `ModConstants.MaxRenderTimeMs == 16.67`. None of those 3 are un-warmed reactive-invalidation checks and none failed. [source: `grep -rn` + direct reads of all 4 files | confidence: high]

## Evidence Against the Idea

- **Counter-hypothesis 1**: A real performance regression in `SoilHealthService.UpdateHealth` or `OverlayRenderer.InvalidateCache` caused the 27.346ms reading. **Refuted**: `InvalidateCache()` merely sets a flag/clears a list; the exact test passed 5/5 in isolation (1–2s total including xUnit host startup), and `OnTileHealthChanged_ThroughProductionModEntrySubscription` passed its functional `Assert.True(overlayRenderer.IsCacheEmpty)` before hitting line 419.
- **Counter-hypothesis 2**: Removing `Stopwatch` weakens FR-010 ("updates within one frame"). **Mitigated**: replacing wall-clock checks with **synchronous invalidation + immediate next-frame rebuild + bounded work (`<= 1` color-service consultation per visible tile)** tests the real deterministic invariant ("within one frame"), while assigning wall-clock 16.67ms frame timing to Gate 3.

## Gaps & Open Questions

- Resolved during research: `VisualizationPerformanceTests.cs` (explicitly warmed 1,000-iteration benchmark) and `GrowthPerformanceTests.cs` stay untouched to avoid altering unrelated performance benchmarks.

## Sources

- Local repository files (`LivingRoots.Tests/SoilHealthVisualizationTests.cs`, `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs`, `LivingRoots.Tests/Visualization/VisualizationPerformanceTests.cs`, `LivingRoots.Tests/GrowthPerformanceTests.cs`, `specs/026-stabilize-overlay-cache/`, `docs/governance/brownfield-zones.md`).
- Executed local commands (`scripts/release-gate.sh`, isolated 5x `dotnet test`, `gh api` branch protection).
