# Concurrency & State Management Requirements Quality Checklist: Soil Health Visualization

**Purpose**: Validate completeness, clarity, consistency, measurability, and coverage of concurrency and state management requirements before implementation
**Created**: 2026-09-12
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [data-model.md](../data-model.md) | [research.md](../research.md)
**Constitution**: [.specify/memory/constitution.md](../../.specify/memory/constitution.md) (Principle III: Async-Only Concurrency)

**Note**: This checklist tests REQUIREMENTS quality, NOT implementation. Every item evaluates whether requirements are complete, clear, consistent, measurable, and cover all scenarios.
**Review Ownership**: Reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the criterion is satisfied for requirements quality.
**Marker Semantics**: `[x]` means the criterion has been reviewed and satisfied for requirements quality. It does not mean implementation work is complete.

## Frame Consistency Requirements

- [x] CHK001 Are frame consistency requirements defined for mid-render health value changes, specifying that the current frame MUST complete with pre-change state and the updated value reflects no later than the next frame? [Completeness, Spec §FR-013, §FR-020]
- [ ] CHK002 Is the "no mixed-state frames" requirement accompanied by a clear definition of what constitutes a mixed-state frame (e.g., a frame where some tiles render with old health values and others with new)? [Clarity, Spec §FR-013] — REVIEW: FR-013 says only "partially-applied health update"; explicit tile-level definition exists for config (FR-019) but not for health values
- [x] CHK003 Is the frame boundary definition for atomic config application consistent with the frame boundary definition for health value changes (both reference "transition point between consecutive render frames")? [Consistency, Spec §FR-013, §FR-019, §FR-020, §Clarifications]
- [x] CHK004 Are requirements defined for the maximum number of frames a health value change may take to become visible (e.g., "no later than the next frame" = at most 1 frame latency)? [Measurability, Spec §FR-013, §FR-020]
- [ ] CHK005 Are frame consistency requirements defined for the degradation state toggle (when visible tile count crosses the 1,000-threshold mid-frame, does the frame complete with pre-toggle state)? [Coverage, Spec §FR-009, Gap] — REVIEW: FR-009 defines immediate toggle but no mid-frame consistency rule for the threshold crossing

## Config Hot-Reload Requirements

- [x] CHK006 Is the config hot-reload timing requirement quantified with a specific frame boundary definition ("the transition point between consecutive render frames where state changes are applied atomically")? [Clarity, Spec §FR-019, §Clarifications]
- [x] CHK007 Is the atomic configuration application requirement consistent across FR-005 (hot-reload), FR-013 (concurrent scenarios), and FR-019 (atomic application) — all requiring same-frame consistency? [Consistency, Spec §FR-005, §FR-013, §FR-019]
- [x] CHK008 Are requirements defined for the interaction between config hot-reload and menu pause (what happens if the config file changes while rendering is paused — apply on resume or immediately)? [Coverage, Gap — Spec §FR-005, §FR-022] — RESOLVED (clarify 2026-09-13): config applies at the next frame boundary even while paused (FR-022); SC-003 holds unconditionally
- [x] CHK009 Are requirements defined for the interaction between config hot-reload and save/load pause (what happens if the config changes during a save/load operation)? [Coverage, Gap — Spec §FR-005, §FR-017] — RESOLVED (clarify 2026-09-13): config applies at the next frame boundary during the save/load pause window (FR-017/FR-022)
- [ ] CHK010 Is the config hot-reload detection mechanism specified (file watcher, polling, SMAPI event) and is the detection-to-application latency bounded? [Completeness, Spec §FR-005, Gap] — REVIEW: detection mechanism (watcher/polling/event) unspecified; detection latency unbounded, so SC-003's "next frame" anchor is ambiguous

## Save/Load Pause Requirements

- [x] CHK011 Are save/load pause requirements defined with explicit behavior for the pause window (no overlay frames render during the save/load operation)? [Completeness, Spec §FR-017]
- [x] CHK012 Is the resume behavior after save/load pause specified (resume with refreshed data from the save system, not stale pre-pause cache)? [Completeness, Spec §FR-017]
- [x] CHK013 Is the save/load pause trigger mechanism specified (which SMAPI events signal pause and resume — SaveLoaded, SaveSaving, etc.)? [Clarity, Spec §FR-017, Gap] — RESOLVED (clarify 2026-09-13): FR-017 bounds the window with Saving→Saved (save) and SaveLoaded→data-refresh-complete (load)
- [x] CHK014 Are requirements defined for the state of in-flight hoe feedback when a save/load operation begins (is feedback preserved, discarded, or completed before pause)? [Coverage, Gap — Spec §FR-003, §FR-017] — RESOLVED (clarify 2026-09-13): in-flight hoe feedback is discarded when the pause begins (FR-017; data-model HoeFeedback transitions)

## Menu Pause Requirements

- [x] CHK015 Are menu pause requirements defined consistently with the save/load pause pattern (FR-022 states "consistent with FR-017")? [Consistency, Spec §FR-017, §FR-022]
- [x] CHK016 Is the resume timing requirement after menu close quantified (FR-022 verification says "overlays resume within one frame")? [Measurability, Spec §FR-022]
- [x] CHK017 Is the menu pause trigger mechanism specified (which SMAPI event — GameLoop.MenuChanged — and what constitutes "any menu is open")? [Clarity, Spec §FR-022, research.md]
- [ ] CHK018 Are requirements defined for the state of tooltip throttle timers when a menu opens and closes (does the 50ms throttle window persist across menu open/close or reset)? [Coverage, Gap — Spec §FR-015, §FR-022] — REVIEW: unspecified whether the throttle window persists or resets across menu open/close

## Event Handler Lifecycle Requirements

- [x] CHK019 Are event handler lifecycle requirements defined for both subscription (on mod initialization) and unsubscription (on mod disposal)? [Completeness, Spec §FR-012]
- [ ] CHK020 Is the event handler unsubscription requirement specified to handle partial failure scenarios (rollback on unsubscribe failure per AGENTS.md convention)? [Completeness, Gap — AGENTS.md "Rollback on unsubscribe failure"] — REVIEW: rollback requirement exists only as an AGENTS.md convention; absent from FR-012 and all spec requirements
- [x] CHK021 Are requirements defined for the disposal state machine (atomic flags for registration/unregistration/disposal states per Constitution Principle III)? [Completeness, Constitution §III, plan.md]

## Thread Safety Requirements

- [x] CHK022 Is the thread safety approach for same-frame coordination specified (lock-free atomics: Interlocked, Volatile.Read, volatile fields per Constitution Principle III)? [Completeness, Constitution §III, plan.md]
- [x] CHK023 Is the async-only concurrency principle (Constitution Principle III) correctly scoped — async/await for I/O-bound operations, lock-free atomics for same-thread frame coordination? [Consistency, Constitution §III, Spec §FR-013, plan.md]
- [ ] CHK024 Are requirements defined for concurrent tooltip state updates during rapid cursor movement (is the 50ms throttle state shared across threads or confined to the render thread)? [Coverage, Spec §FR-015, Gap] — REVIEW: FR-015 does not specify thread confinement or sharing of the throttle state

## Concurrent Scenario Requirements

- [x] CHK025 Are concurrent scenario testing requirements specified with explicit iteration counts (100 times per scenario per FR-013 verification)? [Measurability, Spec §FR-013]
- [ ] CHK026 Is the "no deadlocks" requirement for concurrent scenarios defined with a bounded time constraint (what is the maximum acceptable time for all operations to complete)? [Clarity, Gap — Spec §FR-013 verification says "bounded time" but does not quantify] — REVIEW: no maximum duration is defined for concurrent-scenario operations to complete
- [ ] CHK027 Is the "no UI freezes" requirement for concurrent scenarios defined with a measurable responsiveness criterion (e.g., game loop continues processing at ≥60 FPS during concurrent scenarios)? [Measurability, Gap — Spec §FR-013 verification says "remains responsive" but does not quantify] — REVIEW: no measurable responsiveness criterion (FPS floor or latency bound) is defined for concurrent scenarios
- [ ] CHK028 Is the "no state corruption" requirement for concurrent scenarios defined with observable validity criteria (e.g., all rendered frames reflect valid, complete state — what constitutes "valid, complete state")? [Clarity, Spec §FR-013 verification] — REVIEW: "valid, complete state" has no operational definition beyond the three scenario no-mixed-state contracts
- [x] CHK029 Are the three specific concurrent scenarios from FR-013 (mid-render health change, config hot-reload during rendering, save/load pausing rendering) each covered by dedicated functional requirements (FR-020, FR-019, FR-017 respectively)? [Coverage, Spec §FR-013, §FR-017, §FR-019, §FR-020]
- [x] CHK030 Are concurrent scenario requirements defined for rapid hoe usage (multiple hoe actions triggering feedback state changes while previous feedback is active — timer restart behavior)? [Coverage, Spec §FR-003, §User Story 3]

## Notes

- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied
- Leave items unchecked when they still require clarification, correction, or reviewer evaluation
- `$speckit-implement` reads checklist checkbox state as a gate and must not modify markers
- Items marked `[Gap]` indicate areas where the spec may lack explicit concurrency requirements
- Items are numbered sequentially for easy reference
