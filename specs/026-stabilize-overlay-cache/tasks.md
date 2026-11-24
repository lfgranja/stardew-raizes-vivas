# Tasks: Stabilize overlay cache invalidation property

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Branch**: `026-stabilize-overlay-cache`

## Phase 1: Setup

- [x] T001 Confirm base `origin/dev 4b54c30` and ledger ownership in `.specify/queue/concurrency.jsonl`
- [x] T002 Record BLOCO 5 decision in `.specify/memory/STATE.md` (done before BLOCO 6)

## Phase 2: Foundational (blocking)

- [x] T003 Reproduce RED: property fails 4/5 local runs; capture seed/counterexample
- [x] T004 Diagnose with temporary instrumentation in `LivingRoots.Tests/Invariants/VisualizationCacheInvalidationInvariantsTests.cs`; restore original from backup

## Phase 3: User Story 1 — Trust the required check (P1)

**Goal**: verdict depends on cache consistency only. **Independent test**: 5/5 property runs.

- [x] T005 [US1] Remove `Stopwatch` and elapsed-time assertion from the property in `LivingRoots.Tests/Invariants/VisualizationCacheInvalidationInvariantsTests.cs`
- [x] T006 [US1] Add per-frame consultation count and assert state-change frames consult exactly once per visible tile in `LivingRoots.Tests/Invariants/VisualizationCacheInvalidationInvariantsTests.cs`

## Phase 4: User Story 2 — Deterministic frame-work bound (P2)

**Goal**: preserve bounded-work guarantee. **Independent test**: source review + passing property.

- [x] T007 [P] [US2] Cap consultations per enabled frame at `VisibleTileCount` and document Gate 3 ownership in `LivingRoots.Tests/Invariants/VisualizationCacheInvalidationInvariantsTests.cs`
- [x] T011 [P] [US1] Preserve `MaxTest = 100` and the four event-kind constants unchanged in `LivingRoots.Tests/Invariants/VisualizationCacheInvalidationInvariantsTests.cs`

## Phase 5: User Story 3 - Preserve the rest of the suite (P3)

**Goal**: no production change, full suite green. **Independent test**: `dotnet test -c Release` + diff scope.

- [x] T008 [US3] Run full Release test suite and `dotnet format --verify-no-changes`
- [x] T009 [US3] Verify `git diff --name-only origin/dev` contains no path under `LivingRoots/`

## Phase 6: Polish & Cross-Cutting

- [x] T010 Run `bash scripts/ci/aggregate-gate.sh`, converge (append-only), R4 validate, R7 release, commit and deliver PR

## Dependencies

- T003 → T004 → T005/T006 → T007 → T008/T009 → T010.
- One agent (orchestrator) + one isolated verifier; single file touched, so no parallel file conflict.

## Implementation Strategy

MVP = T005+T006 (the P1 story). T007 preserves FR-009 semantics. No production code, so Gate 3 N/A. Commit atomically per file.
