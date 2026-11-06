# Feature Specification: Joja / Agrochemical Tradeoff (MVP)

**Feature Branch**: `017-joja-agrochemical-tradeoff`

**Created**: 2026-11-06

**Status**: Draft

**Input**: User description: "Specify (do not implement) the first controlled Joja/agrochemical destructive-decision slice from the GDD: an industrial/chemical input (Joja-like framing) that provides an immediate short-term gain but degrades soil health, affecting the existing soil → growth/quality loop. MVP scope: 1 item/rule, mod-intercepted application (not a vanilla fertilizer-hook MVP), host-authoritative shared-farm behavior, explicit no double-counting with existing soil/harvest outcomes, persistence/validation, multiplayer-safe boundaries, legible before/after telegraphing, first-pass tunable values, no mandatory new assets, and honest Gate 3 verification criteria. Grounded in `.specify/assessments/joja-agrochemical-tradeoff/` (intake, research, problem, concept, decision — verdict GO, Option A)."

## Clarifications

### Session 2026-11-06

- Q: Should MVP short-term gain remain confirmation/proxy only (no mechanical buff to 014/015 soil-driven quality/growth and no vanilla fertilizer-tier interaction), with stronger temptation deferred until playtest evidence? → A: **Yes (C1:A).** MVP gain is immediate, attributable shortcut confirmation and Joja-framed choice value only; it MUST NOT alter existing soil-driven quality outcomes, growth outcomes, or vanilla fertilizer-tier behavior. Stronger mechanical short-term benefits are follow-up only if playtest shows insufficient temptation and they can be specified with an explicit no-double-counting rule.
- Q: When target soil is unmeasured (`NaN`/no prior record), what explicit baseline/result rule prevents an invisible no-op or misleading state (FR-006)? → A: **C2:B (neutral explicit baseline).** Resolve unmeasured/`NaN` prior soil to an explicit neutral baseline of `MaxSoilHealth` (100, unmeasured/default healthy state), compute `postPenalty = clamp(baseline - penalty, MinSoilHealth, MaxSoilHealth)`, and record an explicit measured post-penalty result. The implementation MUST NOT produce an invisible no-op for unknown soil and MUST NOT rely on a negative-delta mutation path that can leave unknown soil unmeasured. Planning must confirm this neutral-baseline assumption remains consistent with existing gameplay/persistence expectations.
- Q: What first-pass tunable soil penalty magnitude should planning use as the provisional starting point before playtest calibration? → A: **C3:A, `P=10` (provisional, tunable).** Starting value is 10 soil-health points, bounded 0–100, subject to playtest calibration and tier-boundary review (31/71). Not a final balance claim.
- Q: In MVP, how should a farmhand unsupported agrochemical-application attempt be handled? → A: **C4:A.** Explicit refusal on farmhand/unsupported paths before any soil mutation, item consumption, or false-success feedback, with clear localized messaging and no unsafe local soil mutation. Host-authoritative MVP only; farmhand intent/ack host-mediated protocol is D1/follow-up.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Make a Consequential Shortcut Choice (Priority: P1)

As a player managing farm soil, I can choose to use a Joja-framed agrochemical shortcut on farm soil, receive an immediate understandable confirmation of that shortcut, and see that the choice harms soil health in a legible, recoverable way, so that soil management becomes a meaningful decision rather than a one-directional bonus.

**Why this priority**: This closes the missing GDD decision loop. Without a destructive option, the existing positive soil loop has little strategic tension and the mod’s tradeoff identity is incomplete.

**Independent Test**: Can be fully tested in a host-controlled scenario by applying the Joja/agrochemical input once to eligible soil, observing immediate before/after player feedback, and confirming soil health decreases by the specified bounded amount while remaining valid and recoverable through existing regenerative play.

**Acceptance Scenarios**:

1. **Given** eligible farm soil with a known or measurable soil condition and the player has the Joja/agrochemical input available, **When** the player applies it through the supported MVP interaction, **Then** the system confirms the shortcut was applied with clear, immediate feedback and records a bounded soil-health penalty for that soil.
2. **Given** the same soil after a successful application, **When** the player inspects available soil feedback or continues normal play, **Then** the soil consequence is understandable (not hidden or silently delayed in MVP) and the soil remains within valid bounds.
3. **Given** soil that has been harmed by the agrochemical choice, **When** the player uses existing regenerative soil-care options over time, **Then** recovery remains possible; the feature does not create an irreversible dead-end state by itself.

---

### User Story 2 - Understand the Tradeoff Before Committing (Priority: P1)

As a player, I can understand before using the Joja/agrochemical input that it is a shortcut with soil consequences, and I can understand after using it what happened, so that the choice feels fair and fits the mod’s cozy, non-punitive educational tone.

**Why this priority**: Opaque or punitive feedback would undermine trust and make the mechanic feel unfair. Legibility is required for the tradeoff to teach and to support informed choice.

**Independent Test**: Can be tested by reviewing the item’s presented description/acquisition framing and by performing one successful and one refused application, confirming that before-use information, after-use consequence feedback, and refusal feedback are clear and localized for supported player-facing text.

**Acceptance Scenarios**:

1. **Given** the player views the Joja/agrochemical input before using it, **When** they read its player-facing description/framing, **Then** it clearly communicates that it is an industrial shortcut with a soil-health cost.
2. **Given** a successful application, **When** the action completes, **Then** the player receives immediate consequence feedback that attributes the soil harm to that choice.
3. **Given** an application that cannot be completed under MVP rules, **When** the player attempts it, **Then** the player receives an explicit refusal reason and no misleading “success” feedback.

---

### User Story 3 - Preserve Trustworthy Shared-Farm Behavior (Priority: P1)

As a host or farmhand in a shared farm, I experience Joja/agrochemical soil effects that are consistent with the mod’s host-authoritative multiplayer rules, so that shared soil state is not corrupted by conflicting actions or hidden local-only changes.

**Why this priority**: Multiplayer is a first-class requirement. Soil mutations must not create a second source of truth, silent conflicts, or false verification claims.

**Independent Test**: Can be tested in host-authoritative scenarios supported by current verification constraints: host-applied changes produce the expected soil result and follow-up shared-state synchronization behavior; unsupported farmhand-initiated paths are explicitly refused without unsafe local soil mutation. Real farmhand convergence (D1) is documented as follow-up evidence, not claimed complete.

**Acceptance Scenarios**:

1. **Given** a host player performs a valid application in a supported shared-farm scenario, **When** the application succeeds, **Then** the soil mutation is applied authoritatively and the existing shared soil-state synchronization boundary is respected.
2. **Given** a farmhand attempts an application path that is not safely supported in this MVP, **When** the attempt occurs, **Then** the system refuses it explicitly before any soil mutation, item consumption, or false-success feedback, and does not create an unsafe local-only soil mutation or false success state.
3. **Given** concurrent or conflicting soil-changing actions are possible, **When** a conflict prevents a valid application, **Then** the conflict is handled safely with an explicit outcome and no silent last-write-wins corruption of shared soil state.

---

### User Story 4 - Verify the MVP Honestly (Priority: P2)

As a maintainer/verifier, I can evaluate the feature with explicit, testable acceptance criteria and honest evidence boundaries, so that progress is real and not overstated.

**Why this priority**: The project requires TDD and Gate 3 honesty. Host-only machine evidence can be useful now, but it must not be presented as human-verified or D1-complete.

**Independent Test**: Can be tested by reviewing the spec’s acceptance criteria and verification notes: automated/unit/property-style checks are defined for logic and invariants; host-only end-to-end verification criteria are defined where feasible; human `verified` and D1 evidence are explicitly marked pending/follow-up unless actually performed.

**Acceptance Scenarios**:

1. **Given** the MVP behavior is implemented later according to this spec, **When** automated tests run, **Then** they cover successful application, refusal, bounds, explicit baseline handling, and no-double-counting/composition boundaries defined for the MVP.
2. **Given** a host-only end-to-end verification run is feasible, **When** it passes, **Then** it is recorded as host-only machine evidence only, not as human `verified` or farmhand/D1 attestation.
3. **Given** verification depends on unavailable D1/human evidence, **When** reporting status, **Then** those items remain explicitly pending rather than implied complete.

---

### Edge Cases

- Applying the input to soil that has no previously measured soil value: the system must use an explicit neutral baseline of maximum soil health (100), record a bounded measured post-penalty result (for the provisional 10-point penalty, 90 unless clamped), and must not silently do nothing or leave the soil unmeasured because the prior value was unknown.
- Applying the input when soil is already at minimum health: the result remains within valid bounds and the player receives an honest outcome.
- Applying the input when soil is near a tier/category boundary: the resulting change may cross a boundary, but the change must follow the same bounded/validated rules as other soil mutations.
- Repeated applications: each successful application follows the same immediate, bounded rule; MVP does not introduce hidden stacking/cooldown state unless later specified.
- Attempting to apply to ineligible targets (non-soil, unsupported location/state, missing input, or unsupported multiplayer path): the action is refused with clear feedback before any soil mutation, item consumption, or false-success feedback, and with no unsafe partial mutation.
- Compost/regenerative recovery after agrochemical harm: recovery remains possible through existing soil-care behavior; the agrochemical feature must not bypass normal soil validation.
- Concurrent/shared-state conflict: only a valid authoritative outcome is committed; conflicting attempts receive explicit refusal/failure outcomes and do not corrupt shared state.
- Save/load after application: the recorded soil result persists and reloads consistently within existing soil persistence expectations.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The MVP MUST introduce exactly one controlled Joja/agrochemical tradeoff rule centered on one Joja-framed input, scoped to soil-health impact within existing Living Roots soil behavior.
- **FR-002**: The input MUST be presented as an industrial shortcut with a soil consequence before use, using clear player-facing framing consistent with the mod’s cozy, non-punitive tone.
- **FR-003**: The MVP interaction MUST be mod-controlled. It MUST NOT require or claim vanilla fertilizer-slot interoperability, and it MUST NOT introduce a vanilla fertilizer application hook as part of this MVP.
- **FR-004**: A successful application MUST provide an immediate, clearly communicated short-term confirmation of the shortcut choice. In MVP this confirmation MUST NOT alter existing soil-driven quality outcomes, existing soil-driven growth outcomes, or vanilla fertilizer-tier behavior; no double-counting is permitted.
- **FR-005**: A successful application MUST apply an immediate, bounded soil-health penalty using a first-pass tunable value. The penalty MUST respect existing soil validity rules (within allowed bounds) and be recoverable through existing regenerative soil-care systems.
- **FR-006**: When a target soil tile has no previously measured soil value at application time, the system MUST resolve an explicit neutral pre-application baseline of maximum soil health (100, unmeasured/default healthy state), compute a bounded post-penalty result as `clamp(baseline - penalty, minimum, maximum)`, and record an explicit measured post-penalty result. It MUST NOT produce an invisible no-op or leave the soil unmeasured due to unknown/unmeasured prior state.
- **FR-007**: The system MUST provide immediate, legible after-use feedback that attributes the soil consequence to the Joja/agrochemical choice.
- **FR-008**: The system MUST provide explicit refusal feedback for ineligible or unsupported attempts, including unsupported MVP multiplayer paths. Unsupported farmhand attempts MUST be refused before any soil mutation, item consumption, or false-success feedback. Refusals MUST NOT present misleading success and MUST NOT create unsafe partial soil mutations.
- **FR-009**: Shared-farm soil mutation from this feature MUST follow host-authoritative rules and MUST NOT create a second source of truth. The MVP supports host-authoritative application only; farmhand attempts on unsupported paths are explicitly refused. Full farmhand intent/acknowledgement/host-mediated commit and D1 convergence are follow-up work unless separately evidenced.
- **FR-010**: Conflicting or invalid shared-state changes MUST resolve safely. The feature MUST NOT rely on silent last-write-wins behavior for shared soil state.
- **FR-011**: The feature MUST preserve existing soil persistence/validation expectations: valid bounded values persist across save/load and integrate with the existing soil system without rewriting historical feature behavior.
- **FR-012**: Player-facing text introduced or changed for this MVP MUST be localized according to project expectations for supported languages touched by the change, with clear parity handling for default/English/Portuguese where applicable.
- **FR-013**: The MVP MUST NOT require new art or audio assets. It may rely on existing presentation patterns and clear text/procedural feedback.
- **FR-014**: The MVP MUST include tunable values for first-pass balancing (at minimum the soil penalty magnitude and any MVP-scoped constants required for safe bounds/behavior). The provisional first-pass penalty magnitude starts at 10 soil-health points, bounded 0–100, and remains tunable pending playtest calibration and tier-boundary review (31/71). Not a final balance claim.
- **FR-015**: Verification for this feature MUST follow project quality gates and honesty rules: automated tests for behavior/invariants; host-only end-to-end evidence may be recorded as machine evidence only; human `verified` and D1/farmhand convergence remain separate attestation steps.
- **FR-016**: The MVP MUST explicitly exclude monoculture and other destructive mechanics, vanilla fertilizer-hook interop, full Joja economy/content expansion, and any claim of final balance.

### Assumptions

- The accepted assessment direction is authoritative for MVP boundaries: Option A (mod-intercepted, host-authoritative, legible tradeoff, no Harmony vanilla-fertilizer hook in MVP, no D1/`verified` overclaim).
- The immediate short-term gain in MVP is a clear, attributable shortcut confirmation and Joja-framed choice value; it provides no mechanical buff to existing soil-driven quality/growth calculations and does not interact with vanilla fertilizer-tier behavior. This avoids double-counting risk; stronger mechanical short-term benefits may be assessed later if playtest shows insufficient temptation (clarified C1:A).
- The Joja/agrochemical input is a single custom, Joja-aligned item/rule in MVP framing. Exact acquisition/catalog text and classification details are finalized during planning/implementation but must preserve clear before-use telegraphing and the mod-controlled, non-vanilla-fertilizer-slot boundary.
- Unmeasured/`NaN` soil uses an explicit neutral baseline of maximum soil health (100) for MVP penalty calculation, producing an explicit measured post-penalty result; planning must confirm this assumption against existing gameplay/persistence expectations (clarified C2:B).
- The provisional first-pass soil penalty magnitude is 10 soil-health points (tunable, bounded 0–100). Initial tuning and tier-boundary impact (including existing soil category thresholds at 31/71) require playtest calibration and are not treated as balanced by this spec (clarified C3:A).
- Existing regenerative systems remain the recovery path; no new recovery mechanic is required for MVP.
- Host-only automated end-to-end verification is the highest machine-evidence level expected in MVP under current constraints; D1 real-farmhand convergence and human `verified` attestation are follow-up evidence.
- Any future work that depends on vanilla fertilizer application commit/probe behavior is gated by exact target-build verification and is outside this MVP.

### Dependencies

- Existing Living Roots soil-health behavior, including valid bounds, persistence expectations, and soil-state feedback/synchronization concepts.
- Existing soil consequence loop (014/015) and its composition boundaries, so the MVP gain/penalty does not double-count or silently rewrite established outcomes.
- Existing host-authoritative multiplayer and shared-state safety rules (project constitution Principle VI).
- Existing project verification workflow and Gate 3 honesty/provenance rules.

### Key Entities *(include if feature involves data)*

- **Joja/Agrochemical Input**: The single Joja-framed shortcut item/rule available in MVP; carries player-facing telegraphing and is used through a supported mod-controlled interaction.
- **Soil Target**: A farm soil location eligible for soil-health evaluation/mutation; may have a previously measured soil value or be unmeasured at application time.
- **Soil Health Record**: The bounded, persisted soil condition associated with a soil target; updated by successful authoritative application and observable through existing soil feedback patterns.
- **Application Outcome**: The player-visible and system-recorded result of an attempt: successful shortcut application with soil penalty, or explicit refusal with no unsafe partial mutation.
- **Verification Evidence**: Test and run artifacts used to support claims about MVP behavior; distinguished as automated, host-only machine evidence, human verification, or D1 follow-up.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of successful MVP application scenarios defined in this spec result in an immediate, player-visible shortcut confirmation and an immediate, bounded soil-health penalty with no silent no-op.
- **SC-002**: 100% of specified refusal scenarios produce an explicit refusal outcome with no misleading success feedback and no unsafe partial soil mutation.
- **SC-003**: 100% of specified soil-state updates from this feature remain within valid soil bounds and remain compatible with save/load persistence expectations.
- **SC-004**: In review/playtest of player-facing text and feedback, at least 90% of participants can correctly identify both the short-term shortcut and the soil-health cost before or immediately after a first use.
- **SC-005**: MVP verification artifacts can classify every claim as automated evidence, host-only machine evidence, human verification pending/complete, or D1 pending/complete, with zero claims promoted above their evidence level.
- **SC-006**: The MVP scope contains no required vanilla fertilizer-hook interop, no monoculture behavior, no mandatory new art/audio assets, and no unresolved double-counting rule against existing soil-driven quality/growth outcomes.