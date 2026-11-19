# Tasks: Stabilize reactive invalidation tests against wall-clock flakiness

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Branch**: `feature/027-stabilize-reactive-invalidation-tests`

## Phase 1: Setup

- [x] T001 Confirm base `dev` anchor `8bba3fe`, feature directory `specs/027-stabilize-reactive-invalidation-tests`, and Green Zone scope (`LivingRoots.Tests/`)
- [x] T002 Record BLOCO 5 80/20 decision in `.specify/memory/STATE.md` before BLOCO 6 execution

## Phase 2: Foundational (RED Evidence & Audit)

- [x] T003 Capture baseline `release-gate.sh` exit-14 failure at `LivingRoots.Tests/SoilHealthVisualizationTests.cs:419` (`27.346ms > 16.67ms`) and 5/5 isolated rerun pass
- [x] T004 Audit all `Stopwatch` / `ModConstants.MaxRenderTimeMs` occurrences across `LivingRoots.Tests/` to isolate the 4 reactive-invalidation targets from `VisualizationPerformanceTests` and `GrowthPerformanceTests`

## Phase 3: User Story 1 & 2 — Eliminate wall-clock assertions & preserve deterministic invariants (P1, P2)

**Goal**: Make all four reactive-invalidation tests deterministic while verifying synchronous invalidation, next-frame rebuild, and `<= 1` consultation per visible tile, with Gate 3 ownership documented. **Independent test**: 5/5 isolated runs + `grep` verification.

- [x] T005 [US1] [US2] Replace `Stopwatch` assertion in `OnTileHealthChanged_SubscribedHandler_InvalidatesOverlayCacheImmediatelyWithinFrameBudget` (`LivingRoots.Tests/SoilHealthVisualizationTests.cs`) with synchronous `IsCacheEmpty` check, `Times.Once`/`Times.Exactly(2)` consultation checks, next-frame rebuild assertion, and Gate 3 XML doc note (`FR-001`, `FR-003`, `FR-004`)
- [x] T006 [US1] [US2] Replace `Stopwatch` assertion in `OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame` (`LivingRoots.Tests/SoilHealthVisualizationTests.cs`) with synchronous `IsCacheEmpty` check, next-frame `RenderOverlays` cache rebuild verification, and Gate 3 XML doc note (`FR-001`, `FR-003`, `FR-004`)
- [x] T007 [US1] [US2] Replace `Stopwatch` assertion in `T143_OverlayDrawableOnly_SuppressesFabricated_UnknownFloorAt1_ReactsWithinFrameBudget` (`LivingRoots.Tests/SoilHealthVisualizationTests.cs`) with next-frame drawable-value verification (`75f`, no `%`) and Gate 3 XML doc note (`FR-001`, `FR-003`, `FR-004`)
- [x] T008 [US1] [US2] Replace `Stopwatch` assertion in `T031b_OverlayTiming_ReactiveInvalidation_WithinFrameBudget` (`LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs`) with block-bodied tracked-renderer consultation checks (`1` warm, `0` on invalidate, `1` on rebuild), preserve `[Property(MaxTest = 50)]`, and document Gate 3 ownership (`FR-002`, `FR-003`, `FR-004`)
- [x] T009 [US1] Remove unused `using System.Diagnostics;` directives in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` and `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` and verify block-bodied `.editorconfig` compliance (`FR-005`)

## Phase 4: User Story 3 & Verification (P3)

**Goal**: Zero production diff and full authoritative gate pass.

- [x] T010 [US3] Verify via isolated verifier subagent: `git diff --name-only origin/dev -- LivingRoots/` empty (`FR-006`, `SC-004`), zero `Stopwatch` timing checks remaining (`SC-001`), Release build 0 errors, and 5/5 consecutive passes of the updated tests (`SC-002`)
- [x] T011 [US1] [US3] Run full local authoritative gates (`scripts/ci/aggregate-gate.sh`, `scripts/release-gate.sh`, `scripts/ci/i18n-parity-check.py`, `pytest scripts/quality-gate/tests`) and record outputs (`SC-003`)
- [ ] T012 Complete `converge.md` (append-only), `test.md`, R4 `validate.md` (`APPROVED`), and R7 `release.md`, then deliver reviewable PR targeting `dev/` via `gh` (no merge)

## Dependencies

- T001–T004 → T005–T009 → T010 → T011 → T012.
- Implementation touches only 2 Green-Zone test files; verified by 1 isolated read-only verifier subagent before full `release-gate.sh`.
