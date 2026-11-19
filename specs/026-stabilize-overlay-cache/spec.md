# Feature Specification: Stabilize overlay cache invalidation property

**Feature Branch**: `feature/026-stabilize-overlay-cache`

**Created**: 2026-11-18

**Status**: Draft

**Input**: Decision go (`.specify/assessments/overlay-cache-interleaving/decision.md`) — remove wall-clock dependence from the interleaving property while keeping a deterministic bound on per-frame overlay work.

## Clarifications

### Session 2026-11-18

- No critical ambiguities detected worth formal clarification. 0 of the R3 maximum of 3 questions were asked; the spec's scope, success metrics, and edge cases are already testable.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Trust the required check (Priority: P1)

A merge operator pushes to `dev` and expects the required `Build and Test` check to report the true state of the cache-invalidation invariant. Today the check flips red on a correct code path because a mocked unit frame occasionally exceeds 16.67 ms of wall-clock time under runner load.

**Why this priority**: It is the only required check currently red on mainline and it blocks every merge.

**Independent Test**: Run the interleaving property repeatedly with different generated seeds; the verdict must be identical for identical code regardless of runner load.

**Acceptance Scenarios**:

1. **Given** any FsCheck-generated event interleaving, **When** the property evaluates cache invalidation, **Then** its verdict is determined by cache-consistency observations only.
2. **Given** a state change (tile mutation, configuration change, or re-enable), **When** the next frame renders, **Then** the color service is consulted exactly once per visible tile.
3. **Given** overlays are disabled, **When** a frame renders, **Then** the color service is not consulted at all.

---

### User Story 2 - Keep a meaningful frame-work bound (Priority: P2)

A reviewer needs the removed wall-clock assertion replaced by an equivalent *deterministic* guarantee, so the FR-009 frame-budget concern is not silently dropped.

**Why this priority**: Prevent a false "the budget is no longer tested" conclusion.

**Independent Test**: Inspect the property: each enabled frame must cap color-service consultations at the visible tile count, and the test documentation must point to the verification runner as the owner of wall-clock budget evidence.

**Acceptance Scenarios**:

1. **Given** any enabled frame in the interleaving, **When** the frame completes, **Then** the number of color-service consultations is at most the visible tile count.
2. **Given** the property source, **When** a reviewer reads it, **Then** a comment states that wall-clock budget is Gate 3 verification-runner evidence, not unit-property evidence.

---

### User Story 3 - Preserve the rest of the suite (Priority: P3)

The maintainer reruns the full Release test suite and sees no new failures, and no production rendering behavior changes.

**Why this priority**: Confines the blast radius to one test file.

**Independent Test**: Full `dotnet test -c Release` after the change; diff review showing no files under `LivingRoots/` changed.

**Acceptance Scenarios**:

1. **Given** the full test suite, **When** it runs on the feature branch, **Then** it reports zero failed tests.
2. **Given** the change set, **When** reviewed, **Then** no runnable production code is modified.

---

## Requirements *(mandatory)*

- **FR-001**: The interleaving property MUST NOT use elapsed wall-clock time in its pass/fail decision.
- **FR-002**: The property MUST assert exact per-frame color-service consultation counts for state-change and disabled frames, as it does today.
- **FR-003**: The property MUST assert a per-frame upper bound of one color-service consultation per visible tile.
- **FR-004**: The property documentation MUST state that wall-clock frame budget belongs to the Gate 3 verification runner.
- **FR-005**: The change MUST be confined to the property test file and its SDD artifacts.
- **FR-006**: The property MUST keep its `MaxTest = 100` randomized coverage and the same four event kinds.

## Success Criteria *(measurable)*

- **SC-001**: 5 out of 5 consecutive local runs of the property pass with distinct seeds (baseline 1/5).
- **SC-002**: Full Release test suite reports 0 failed tests (baseline 1 failed / 2003).
- **SC-003**: The required `Build and Test` check is green on the PR and on the `dev` push after merge (baseline RED).
- **SC-004**: Diff contains zero changes under `LivingRoots/` (production code).

## Key Entities

- **Interleaving event**: one of tile mutation, configuration change, disable, or re-enable, drawn by FsCheck.
- **Visible tile**: a tile inside the fixed 4×3 viewport; 12 per frame.
- **Color-service consultation**: one call to `GetCategoryForHealth`, recorded by the test mock.
- **Frame budget**: the 16.67 ms wall-clock limit owned by Gate 3, not by this property.

## Assumptions

- Runner load and GC pauses can add >16.67 ms to a mocked unit frame; confirmed by `DIAG:budget` instrumentation.
- The production cache key is correct; instrumentation showed all consistency assertions passing.

## Dependencies

- None external. Gate 3 evidence remains owned by `scripts/verify-scenario.sh`.
