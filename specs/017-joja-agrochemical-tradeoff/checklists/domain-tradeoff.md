# Domain Tradeoff, Soil-State Mutation & Composition Boundaries Checklist: Joja / Agrochemical Tradeoff (MVP)

**Purpose**: Validate the quality of requirements for the MVP Joja/agrochemical domain tradeoff, soil-state mutation semantics, and composition boundaries — ensuring requirements are complete, clear, consistent, measurable, and safely bounded before planning/implementation consumes them.
**Created**: 2026-11-06
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [research.md](../research.md) | [data-model.md](../data-model.md) | [contracts/agrochemical-application-service.md](../contracts/agrochemical-application-service.md)

## Ownership & Marker Semantics

- **Reviewer-owned**: This checklist is a human/reviewer artifact for judging requirement quality. It is not an implementation task list and does not authorize code changes by itself.
- **Marker semantics**: `[ ]` = not yet reviewed; `[x]` = requirement quality judged sufficient for this item; `[!]` = requirement quality concern found (gap, ambiguity, conflict, or weak measurability) requiring spec/plan/contract follow-up. Reviewers should add a short note beside `[!]` items.
- **Scope note**: Items test *requirements writing* only (completeness, clarity, consistency, measurability, coverage, assumptions, ambiguities/conflicts). They do not prescribe implementation behavior, test procedures, or framework/API choices.

## Requirement Completeness

- [ ] CHK001 Are requirements complete for the MVP scope boundary of exactly one Joja-framed agrochemical input/rule and its soil-health impact, including what is explicitly excluded (vanilla fertilizer-hook interop, monoculture, final balance, mandatory new assets)? [Completeness, Spec §FR-001, §FR-003, §FR-013, §FR-016]
- [ ] CHK002 Are all successful-application effects specified as a complete set: shortcut confirmation feedback, bounded soil penalty, measured recorded result, authoritative success ordering, and FR-009-compatible host snapshot sync? [Completeness, Spec §FR-004, §FR-005, §FR-006, §FR-007, §FR-009; Data-Model §Application Outcome]
- [ ] CHK003 Are refusal-class requirements complete for unsupported authority, invalid target, missing/invalid input, and conflict, including the shared prohibition on mutation, item consumption, and false-success feedback? [Completeness, Spec §FR-008; Contracts §Refusal classes; Data-Model §Application Outcome]
- [ ] CHK004 Are recovery requirements complete in specifying that agrochemical harm remains recoverable through existing regenerative soil-care systems without introducing a new recovery mechanic or dead-end state? [Completeness, Spec §FR-005, §Assumptions; Data-Model §Out-of-model]
- [ ] CHK005 Are composition-boundary requirements complete for every adjacent system the MVP must not disturb (014/015 soil-driven quality, 014/015 soil-driven growth, vanilla fertilizer-tier behavior/heuristics, `HoeDirt.fertilizer` writes, Harmony fertilizer hooks)? [Completeness, Spec §FR-003, §FR-004; Plan §Technical Context/Constraints; Contracts §Non-negotiable constraints]

## Requirement Clarity

- [ ] CHK006 Is the C1 "confirmation/proxy-only" short-term gain quantified/qualified clearly enough to distinguish player-visible shortcut confirmation from any mechanical buff to 014/015 quality/growth or vanilla fertilizer-tier behavior? [Clarity, Spec §Clarifications C1:A, §FR-004, §SC-006]
- [ ] CHK007 Is the C2 unmeasured/`NaN` baseline rule specified with unambiguous inputs, formula, and outputs (`baseline = 100`, `postPenalty = clamp(baseline - penalty, MinSoilHealth, MaxSoilHealth)`, explicit measured post-penalty record, no invisible no-op)? [Clarity, Spec §Clarifications C2:B, §FR-006; Data-Model §Validation rules 2-4; Contracts §Success step 2-3]
- [ ] CHK008 Is the C2 requirement constraint against relying on a negative-delta mutation path from unknown/unmeasured soil stated clearly as an observable requirement boundary (absolute measured write required) rather than only an implementation hint? [Clarity, Spec §FR-006; Research §Soil mutation invariants; Data-Model §Validation rule 3; Contracts §Non-negotiable constraints]
- [ ] CHK009 Is the C3 provisional penalty (`P=10`, bounded 0–100, tunable, 31/71 tier-boundary review, not a final balance claim) documented with clear provisional-status language wherever tuning values appear? [Clarity, Spec §Clarifications C3:A, §FR-005, §FR-014; Plan §Summary; Data-Model §Tunables]
- [ ] CHK010 Is the C4 farmhand/unsupported-path refusal ordering specified clearly enough to define the required preconditions before soil mutation, item consumption, and false-success feedback across spec, data model, and contract? [Clarity, Spec §Clarifications C4:A, §FR-008, §FR-009; Data-Model §Application Outcome, §State transitions; Contracts §Refusal classes]

## Requirement Consistency

- [ ] CHK011 Are authority requirements consistent across spec, plan constitution check, data model, and contract in stating host-authoritative MVP mutation, no second source of truth, and farmhand unsupported-path refusal rather than farmhand commit? [Consistency, Spec §FR-008, §FR-009; Plan §Constitution Check VI; Data-Model §Validation rule 6; Contracts §Non-negotiable constraints]
- [ ] CHK012 Are soil-bound and validity requirements consistent in using `[0,100]` / `MinSoilHealth..MaxSoilHealth` semantics, preserving existing soil persistence/validation expectations, and not redefining 014/015 tier categories? [Consistency, Spec §FR-005, §FR-006, §FR-011; Data-Model §Soil Health Record, §Validation rules 1,8; Contracts §Success step 2-4]
- [ ] CHK013 Are no-double-counting requirements consistent between the spec's "no mechanical buff to existing soil-driven quality/growth or vanilla fertilizer-tier behavior" and the contract's explicit prohibitions on `HoeDirt.fertilizer` writes, Harmony fertilizer hooks, and harvest/growth modifier logic? [Consistency, Spec §FR-004, §SC-006; Plan §Constraints; Contracts §Non-negotiable constraints]
- [ ] CHK014 Are conflict-resolution requirements consistent in requiring first-wins/explicit refusal outcomes and prohibiting silent last-write-wins corruption of shared soil state? [Consistency, Spec §FR-010, §User Story 3; Plan §Constraints; Data-Model §Validation rule 7, §State transitions]

## Acceptance Criteria Quality

- [ ] CHK015 Can SC-001 ("immediate, player-visible shortcut confirmation and an immediate, bounded soil-health penalty with no silent no-op") be objectively evaluated against requirement-defined evidence for both feedback and measured soil-result recording? [Acceptance Criteria, Spec §SC-001, §FR-004, §FR-006, §FR-007]
- [ ] CHK016 Can SC-002/SC-003 acceptance criteria be objectively traced to requirement states for each refusal class and every soil-state update (no misleading success, no unsafe partial mutation, valid bounds, save/load compatibility)? [Acceptance Criteria, Spec §SC-002, §SC-003, §FR-008, §FR-011]
- [ ] CHK017 Are Gate 3/honesty acceptance requirements measurable in classifying evidence as automated, host-only machine evidence, human verification pending/complete, or D1 pending/complete without promoting claims above evidence level? [Acceptance Criteria, Spec §FR-015, §SC-005, §User Story 4; Plan §Summary; Data-Model §Verification Evidence]

## Scenario Coverage

- [ ] CHK018 Are primary successful host scenarios specified across pre-use telegraphing, eligible application, immediate confirmation, bounded penalty, post-use consequence legibility, and host-authoritative sync compatibility? [Coverage, Spec §User Stories 1-3, §FR-002, §FR-004, §FR-006, §FR-007, §FR-009]
- [ ] CHK019 Are alternate/exception scenarios fully specified for ineligible targets, missing/invalid input, unsupported multiplayer paths, and conflicts, each with explicit refusal outcomes? [Coverage, Spec §FR-008, §Edge Cases; Contracts §Refusal classes]
- [ ] CHK020 Are recovery and continued-play scenarios specified so that post-penalty soil remains legible and recoverable through existing regenerative systems over time, without introducing a separate recovery mechanic? [Coverage, Spec §User Story 1 scenario 3, §FR-005, §Edge Cases; Data-Model §Out-of-model]

## Edge Case Coverage

- [ ] CHK021 Are edge-case requirements sufficient for unmeasured soil, minimum-health soil, near-tier-boundary soil (31/71), repeated applications, and save/load after application, including the expected bounded/measured-result behavior in each case? [Edge Case, Spec §Edge Cases, §FR-006, §FR-011, §FR-014; Data-Model §Validation rules 1-4,8]
- [ ] CHK022 Are repeated-application requirements explicit enough to prevent hidden stacking, hidden cooldown, or delayed-state assumptions in MVP while preserving the same immediate bounded rule per successful application? [Edge Case, Spec §Edge Cases, §FR-005; Data-Model §State transitions, §Out-of-model; Gap]
- [ ] CHK023 Are conflict/first-wins and authoritative mutation semantics specified so concurrent or contended attempts cannot be interpreted as idempotent silent overwrites, local-only commits, or retry-implied success? [Edge Case, Spec §FR-008, §FR-009, §FR-010; Plan §Constraints; Data-Model §Validation rules 6-7; Contracts §Refusal classes]

## Dependencies & Assumptions

- [ ] CHK024 Are dependencies and assumptions documented with traceable validation impact for existing soil persistence/bounds, 014/015 composition boundaries, host-authoritative/FR-009 sync concepts, provisional C3 tuning (including 31/71 review), and the C2 neutral-baseline assumption? [Dependencies & Assumptions, Spec §Assumptions, §Dependencies; Plan §Technical Context/Constraints; Research §Planning gaps]
- [ ] CHK025 Are follow-up boundaries (D1 real-farmhand convergence, Harmony/vanilla fertilizer-hook work, and playtest calibration for P=10) stated as assumptions/deferred evidence rather than implied MVP guarantees? [Assumption, Spec §Assumptions, §FR-009, §FR-015; Plan §Summary/Constitution Check VI; Research §Planning gaps]

## Ambiguities & Conflicts

- [ ] CHK026 Is there any remaining ambiguity between "immediate short-term gain" as C1 confirmation/proxy-only value and player interpretation of a mechanical advantage, and is that distinction anchored in requirement/acceptance language rather than left to implementation? [Ambiguity, Spec §Clarifications C1:A, §FR-004, §SC-006]
- [ ] CHK027 Is the C2 neutral-baseline assumption (`MaxSoilHealth` 100 for unmeasured/`NaN`) explicitly bounded as a requirement assumption requiring planning confirmation against existing gameplay/persistence expectations, with no conflicting requirement implying unknown-as-zero or refusal-when-unknown behavior? [Ambiguity, Spec §Clarifications C2:B, §Assumptions; Research §Decision summary, §Soil mutation invariants, §Planning gaps; Conflict]
- [ ] CHK028 Do the contracts leave any unresolved conflict about where authoritative success ordering occurs (service result vs controller orchestration for consumption/display/lock/FR-009 broadcast), and is that responsibility split sufficiently specified to avoid contradictory implementation interpretations? [Ambiguity, Contracts §Purpose/Success/Refusal/Result shape; Plan §Project Structure; Gap]

## Notes

- Check items off as `[x]` when requirement quality is sufficient, or mark `[!]` with a brief issue note when follow-up is needed.
- Traceability target: 28/28 items (100%) include references or explicit markers.
- Review priority for this domain: C1 no-double-counting, C2 measured/NaN baseline safety, C4 refusal-before-mutation/consumption, and conflict/authority semantics.