# Feature Specification: Stabilize reactive invalidation tests against wall-clock flakiness

**Feature Branch**: `feature/027-stabilize-reactive-invalidation-tests`

**Created**: 2026-11-19

**Status**: Draft

**Input**: Decision go (`.specify/assessments/reactive-invalidation-determinism/decision.md`) — eliminate the four remaining reactive-invalidation `Stopwatch <= ModConstants.MaxRenderTimeMs` assertions across `LivingRoots.Tests/SoilHealthVisualizationTests.cs` and `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` while preserving synchronous invalidation, immediate next-frame rebuild, and bounded per-frame work.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic release-gate and required CI execution (Priority: P1)

A maintainer running `scripts/release-gate.sh` locally or awaiting the required `Build and Test` check on `dev` expects the verdict to reflect actual cache-invalidation correctness rather than host CPU, GC, or JIT scheduling variance. Today, `SoilHealthVisualizationTests.OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame` fails intermittently (`27.346ms > 16.67ms`, exit 14) while passing 5/5 in isolation, and three sibling reactive-invalidation tests (`SoilHealthVisualizationTests.cs` lines 95 and 673, plus `VisualizationInvariantsTests.T031b` at line 324) carry the same wall-clock hazard.

**Why this priority**: Directly fixes the observed `release-gate.sh` Step 4 failure on `8bba3fe` and removes all sibling wall-clock traps in reactive-invalidation tests.

**Independent Test**: Run the four updated tests 5 consecutive times and run the full `scripts/release-gate.sh`; all runs must pass deterministically with 0 failed tests.

**Acceptance Scenarios**:

1. **Given** the production `ModEntry.Entry` wiring and a warm overlay cache, **When** `soilHealthService.UpdateHealth(...)` mutates a visible tile's health, **Then** `overlayRenderer.IsCacheEmpty` becomes `true` synchronously before `UpdateHealth` returns and the very next rendered frame rebuilds the overlay cache without depending on elapsed wall-clock milliseconds.
2. **Given** any of the four reactive-invalidation tests under `SoilHealthVisualizationTests` or `VisualizationInvariantsTests`, **When** executed under full-suite load, **Then** its pass/fail verdict never reads `Stopwatch.Elapsed.TotalMilliseconds` or compares elapsed wall-clock time against `ModConstants.MaxRenderTimeMs`.

---

### User Story 2 - Preserve FR-010/FR-009 reactive and bounded-work semantics (Priority: P2)

A code reviewer auditing FR-010 ("updates within one frame without manual refresh") and FR-009 ("bounded per-frame work") needs confidence that removing un-warmed `Stopwatch` assertions does not silently drop synchronous invalidation, next-frame rebuild verification, or bounded color-service consultations, and that wall-clock 16.67ms frame-budget ownership is explicitly documented on Gate 3 (`scripts/verify-scenario.sh`).

**Why this priority**: Prevents weakening test coverage or losing traceability for frame-budget ownership.

**Independent Test**: Inspect all four updated tests and execute them: each verifies synchronous invalidation, zero redundant work during invalidation, immediate next-frame rebuild with updated values and `<= 1` color-service consultation per visible tile, and states Gate 3 ownership of wall-clock timing in XML doc/comments.

**Acceptance Scenarios**:

1. **Given** a warmed `OverlayRenderer` with 1 visible tile, **When** the `OnTileHealthChanged` handler invalidates the cache, **Then** `IsCacheEmpty` is immediately `true` with zero additional `IColorInterpolationService` consultations during invalidation, and the subsequent `GetOverlays` call rebuilds exactly 1 overlay with at most 1 color-service consultation per visible tile.
2. **Given** the T143 drawable-value reactive update test, **When** `SetTileHealthData` updates the visible tile to `75f`, **Then** the very next rendered frame emits `75` without `%` or fabricated values.
3. **Given** FsCheck property `T031b_OverlayTiming_ReactiveInvalidation_WithinFrameBudget` (`MaxTest = 50`), **When** tested across arbitrary `float health` inputs, **Then** each iteration asserts cache warm-up (1 consultation), synchronous invalidation (`IsCacheEmpty == true`, 0 extra consultations), and immediate next-frame rebuild reflecting updated health (`rebuilt.Count == 1 && !IsCacheEmpty`, bounded to 1 consultation on rebuild).

---

### User Story 3 - Zero production blast radius (Priority: P3)

The maintainer verifies that no runnable production code, localization file, or unrelated benchmark test was modified, keeping Gate 3 real-game re-attestation N/A for this fix.

**Why this priority**: Confines changes strictly to the Green Zone (`LivingRoots.Tests/`) per `docs/governance/brownfield-zones.md`.

**Independent Test**: Verify `git diff --name-only origin/dev -- LivingRoots/` is empty and `VisualizationPerformanceTests.cs` / `GrowthPerformanceTests.cs` / `VisualizationConstantsTests.cs` remain untouched.

**Acceptance Scenarios**:

1. **Given** the feature branch diff against `origin/dev`, **When** inspected, **Then** zero files under `LivingRoots/` are modified or created.
2. **Given** `VisualizationPerformanceTests.cs`, `GrowthPerformanceTests.cs`, and `VisualizationConstantsTests.cs`, **When** the full suite runs, **Then** they remain untouched and green.

---

### Edge Cases

- What happens if `SoilHealthService.UpdateHealth` fires for an `OverlayRenderer` whose `ModEntry` subscription is disconnected? `Assert.True(overlayRenderer.IsCacheEmpty)` still fails immediately with `SUBSCRIPTION DEFECT`.
- What happens if `VisualizationInvariantsTests.T031b` receives `NaN`, `Infinity`, or `0f` from FsCheck? `SafeOverlayHealth(health)` normalizes non-finite/zero inputs to `50f`, preserving existing generator semantics across all 50 test cases.
- What happens if `using System.Diagnostics;` becomes unused after removing `Stopwatch` in either file? Compiler/analyzer warnings (`TreatWarningsAsErrors=true`, IDE0005) would fail build/format unless the unused `using` directive is removed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The three reactive-invalidation tests in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` (`OnTileHealthChanged_SubscribedHandler_InvalidatesOverlayCacheImmediatelyWithinFrameBudget`, `OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame`, and `T143_OverlayDrawableOnly_SuppressesFabricated_UnknownFloorAt1_ReactsWithinFrameBudget`) MUST NOT use `Stopwatch` or assert elapsed wall-clock time against `ModConstants.MaxRenderTimeMs`.
- **FR-002**: `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` (`T031b_OverlayTiming_ReactiveInvalidation_WithinFrameBudget`) MUST NOT use `Stopwatch` or assert elapsed wall-clock time against `ModConstants.MaxRenderTimeMs`, while retaining `[Property(MaxTest = 50)]`.
- **FR-003**: Each updated test MUST deterministically verify synchronous invalidation (`IsCacheEmpty == true` where applicable) and immediate next-frame rebuild reflecting updated data, with `<= 1` `IColorInterpolationService` consultation per visible tile on mock-tracked `OverlayRenderer` rebuilds (0 during invalidation).
- **FR-004**: XML documentation/comments on all four updated tests MUST explicitly record that wall-clock 16.67ms frame-budget (`ModConstants.MaxRenderTimeMs`) evidence belongs to Gate 3 (`scripts/verify-scenario.sh`).
- **FR-005**: Unused `using System.Diagnostics;` directives in `SoilHealthVisualizationTests.cs` and `VisualizationInvariantsTests.cs` MUST be removed to satisfy `dotnet format --verify-no-changes` and `TreatWarningsAsErrors=true`, with zero expression-bodied methods added (`.editorconfig` compliance).
- **FR-006**: All code changes MUST remain strictly confined to the two named test files in `LivingRoots.Tests/` plus SDD/doc artifacts (zero changes under `LivingRoots/`).

### Key Entities

- **Reactive Invalidation Event**: `SoilHealthService.OnTileHealthChanged` or handler invocation that empties the `OverlayRenderer` cache synchronously on the calling frame.
- **Next-Frame Rebuild**: The immediate subsequent `GetOverlays` or `RenderOverlays` frame that recomputes overlay entries from mutated health data.
- **Consultation Bound**: Deterministic counter/Moq verification proving invalidation performs 0 color-service calls and next-frame rebuild performs at most 1 color-service consultation per visible tile.
- **Gate 3 Frame Budget**: The 16.67ms (`ModConstants.MaxRenderTimeMs`) real-time budget measured during real-game SMAPI execution under `scripts/verify-scenario.sh`, outside unit/property test scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero occurrences of `Stopwatch` or `MaxRenderTimeMs` timing comparisons remain in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` and `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs`.
- **SC-002**: The four updated tests pass **5/5** consecutive isolated test runs (`dotnet test -c Release --no-build`).
- **SC-003**: `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" bash scripts/release-gate.sh` completes with **`ALL STEPS PASSED`** (`exit 0`, full suite 1999 passed / 0 failed / 4 skipped / 2003 total, format exit 0), and `bash scripts/ci/aggregate-gate.sh` completes with **`ALL STEPS PASSED`** (`exit 0`).
- **SC-004**: `git diff --name-only origin/dev -- LivingRoots/` returns empty (zero production diff).

## Assumptions

- `LivingRoots/ModEntry.cs`, `LivingRoots/Services/SoilHealthService.cs`, and `LivingRoots/Services/Visualization/OverlayRenderer.cs` already implement synchronous cache invalidation correctly, as proven by the 5/5 isolated pass and functional assertions passing prior to line 419 during the `release-gate.sh` run.
- `System.Diagnostics` is not referenced anywhere else in `SoilHealthVisualizationTests.cs` or `VisualizationInvariantsTests.cs` (confirmed via `grep -n`).
- Warmed `VisualizationPerformanceTests.cs` (`1,000` steady-state calls) and `GrowthPerformanceTests.cs` are intentionally distinct and remain untouched.
