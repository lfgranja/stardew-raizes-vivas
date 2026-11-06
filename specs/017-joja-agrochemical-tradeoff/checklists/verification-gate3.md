# Verification, Gate 3 Honesty & Testability Requirements Quality Checklist: Joja / Agrochemical Tradeoff (MVP)

**Purpose**: Unit-test the quality, clarity, completeness, consistency, and measurability of verification, Gate 3 honesty, testability, and evidence/provenance requirements for feature 017 (requirements authoring quality only).
**Created**: 2026-11-06
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [research.md](../research.md) | [data-model.md](../data-model.md) | [contracts/multiplayer-gate3.md](../contracts/multiplayer-gate3.md) | [quickstart.md](../quickstart.md)

**Note**: This checklist is reviewer-owned and evaluates the requirements text, not implementation behavior or test execution results.

## Requirement Completeness

- [ ] CHK001 Are the automated test-coverage expectation requirements complete for every MVP-critical behavior class (successful application, refusal, bounds, explicit NaN/unmeasured baseline, and no-double-counting/composition boundaries)? [Completeness, Spec §FR-015, Spec §User Story 4, Contracts §017 MVP verification targets]
- [ ] CHK002 Are the required ordering/causality expectations for refusal outcomes fully specified as testable requirements (no soil mutation, no item consumption, and no false-success feedback before/without an authoritative success)? [Completeness, Spec §FR-008, Spec §FR-009, Spec §SC-002, Contracts §Required behavior, Data-Model §Application Outcome]
- [ ] CHK003 Are all evidence classification levels required for MVP claims explicitly defined (automated, host-only machine evidence/`machine-pass`, human `verified` pending/complete, D1 farmhand convergence pending/complete)? [Completeness, Spec §FR-015, Spec §SC-005, Spec §Verification Evidence, Contracts §Claim levels, Data-Model §Verification Evidence]
- [ ] CHK004 Are provenance and historical-evidence preservation requirements specified so prior artifacts/evidence cannot be rewritten, overwritten, or retroactively upgraded by later MVP work? [Gap, Spec §FR-011, Spec §Dependencies, Plan §Summary, Contracts §Gate 3 / verification honesty]
- [ ] CHK005 Are explicit requirements documented that `machine-pass`/host-only machine evidence is never equivalent to human `verified` and never satisfies D1/farmhand-convergence claims without separate evidence? [Completeness, Spec §FR-015, Spec §User Story 4, Spec §SC-005, Contracts §Claim levels, Contracts §Prohibitions, Quickstart §Host-only Gate 3]

## Requirement Clarity

- [ ] CHK006 Is the explicit NaN/unmeasured baseline rule quantified with unambiguous inputs, baseline value, formula, bounds, and measured-result obligation (`NaN`/no prior record → baseline 100, `clamp(baseline - penalty, Min, Max)`, explicit recorded post-penalty result)? [Clarity, Spec §Clarifications C2, Spec §FR-006, Spec §Edge Cases, Data-Model §Validation rules, Research §Decision summary]
- [ ] CHK007 Is the no-double-counting/composition boundary requirement specific about which outcomes MVP gain must not alter (existing soil-driven quality outcomes, existing soil-driven growth outcomes, vanilla fertilizer-tier behavior) and what remains allowed through normal existing soil rules? [Clarity, Spec §Clarifications C1, Spec §FR-004, Spec §SC-006, Data-Model §Validation rules, Research §No double-counting boundaries]
- [ ] CHK008 Is the host-only end-to-end feasibility/honesty boundary clearly stated as "feasible where supported, recorded as host-only machine evidence only, and not promotable to human `verified` or D1"? [Clarity, Spec §FR-015, Spec §User Story 4, Plan §Summary, Contracts §017 MVP verification targets, Quickstart §Host-only Gate 3]
- [ ] CHK009 Are the provisional tuning requirements for `P=10` explicitly marked as first-pass/tunable/playtest-calibrated rather than final balance claims, including bounded range and tier-boundary review context? [Clarity, Spec §Clarifications C3, Spec §FR-014, Spec §Assumptions, Data-Model §Tunables, Research §Decision summary, Contracts §Prohibitions]

## Requirement Consistency

- [ ] CHK010 Are Gate 3 evidence levels, terminology, and promotion boundaries consistent across spec, plan, data model, multiplayer/Gate 3 contract, and quickstart (`automated`, `host-only machine evidence`/`machine-pass`, human `verified`, D1 pending/complete)? [Consistency, Spec §FR-015, Spec §SC-005, Plan §Summary, Data-Model §Verification Evidence, Contracts §Claim levels, Quickstart §S1-S6]
- [ ] CHK011 Are C4 farmhand refusal requirements consistent in required timing and forbidden side effects across all artifacts (before soil mutation, item consumption, and false-success feedback; no unsafe local authoritative mutation)? [Consistency, Spec §Clarifications C4, Spec §FR-008, Spec §FR-009, Spec §SC-002, Plan §Technical Context, Data-Model §State transitions, Contracts §Required behavior]
- [ ] CHK012 Is the "confirmation/proxy-only gain" requirement consistent with no-double-counting, scope-exclusion, and later-outcome wording so the spec does not imply a mechanical buff while still allowing normal soil-rule consequences? [Consistency, Spec §FR-004, Spec §SC-001, Spec §SC-006, Plan §Summary, Data-Model §Validation rules, Research §Decision summary]
- [ ] CHK013 Do scenario IDs, assumptions, dependencies, and quickstart validation references remain traceably aligned with FR/SC/clarification identifiers, without introducing orphaned or contradictory requirement anchors? [Consistency, Traceability, Spec §Clarifications, Spec §FR-001-FR-016, Spec §SC-001-SC-006, Plan §Phase 1, Quickstart §Validation scenarios]

## Acceptance Criteria Quality

- [ ] CHK014 Can SC-001 through SC-006 be objectively evaluated with measurable pass/fail criteria, explicit denominators/scope, and evidence-level thresholds that avoid subjective judgment? [Acceptance Criteria, Spec §SC-001-SC-006, Spec §FR-015, Contracts §Claim levels]
- [ ] CHK015 Are acceptance scenarios and user-story tests traceable to the functional requirements they are intended to substantiate, especially for automated test expectations, MP refusal safety, and Gate 3 honesty boundaries? [Acceptance Criteria, Traceability, Spec §User Story 1, Spec §User Story 2, Spec §User Story 3, Spec §User Story 4, Spec §FR-004-FR-016]
- [ ] CHK016 Is SC-004's playtest measurability requirement sufficiently objective as a requirement (participant scope, sample/selection expectations, or minimum evidence format) rather than an unbounded qualitative claim? [Ambiguity, Acceptance Criteria, Spec §SC-004, Spec §FR-002, Spec §FR-007, Quickstart §Not validated by this MVP quickstart]

## Scenario Coverage

- [ ] CHK017 Are primary, alternate, refusal, host-authoritative, unsupported-farmhand, and host-only verification evidence scenarios all represented by requirements-quality coverage without leaving any MVP claim class untestable? [Coverage, Spec §User Stories 1-4, Spec §FR-004-FR-010, Spec §FR-015, Quickstart §S1-S6]
- [ ] CHK018 Are requirements present for classifying outcomes when host-only end-to-end verification is feasible versus infeasible/unavailable, including what may still be claimed and what must remain pending? [Coverage, Gap, Spec §FR-015, Spec §Assumptions, Plan §Summary, Contracts §017 MVP verification targets, Quickstart §Host-only Gate 3]

## Edge Case Coverage

- [ ] CHK019 Are edge-case requirements complete for unknown/unmeasured soil, minimum/maximum bounds, tier-boundary crossings, repeated applications, ineligible targets, save/load persistence, and concurrent/conflicting actions with explicit testable outcomes? [Edge Case, Spec §Edge Cases, Spec §FR-005-FR-011, Data-Model §Validation rules, Data-Model §State transitions]
- [ ] CHK020 Are zero-state and boundary exception requirements objectively defined (explicit measured zero is preserved/distinguished from Unknown, and refusals/conflicts produce no partial mutation or consumption side effects)? [Edge Case, Clarity, Spec §Edge Cases, Spec §FR-006, Spec §FR-008, Spec §FR-010, Data-Model §Soil Health Record, Research §Soil mutation invariants]

## Dependencies & Assumptions

- [ ] CHK021 Are external dependencies and verification constraints documented with requirement impact and explicit "pending/follow-up" boundaries where D1, human attestation, runner availability, or fertilizer-hook build facts are unavailable? [Dependencies, Assumption, Spec §Dependencies, Spec §Assumptions, Plan §Technical Context, Contracts §Boundaries, Quickstart §Not validated by this MVP quickstart]
- [ ] CHK022 Is the assumption that host-only automated end-to-end verification is the highest machine-evidence level under current constraints stated with enough specificity to prevent overclaiming live farmhand convergence or human verification? [Assumption, Spec §Assumptions, Spec §FR-015, Plan §Summary, Contracts §Claim levels, Contracts §Prohibitions]

## Ambiguities & Conflicts

- [ ] CHK023 Are scope exclusions stated as objectively testable requirement boundaries (no Harmony fertilizer hook, no monoculture, no vanilla fertilizer-slot interop, no mandatory assets, no final balance claim) with no conflicting permissive wording elsewhere? [Ambiguity, Conflict, Spec §FR-003, Spec §FR-013, Spec §FR-016, Spec §SC-006, Plan §Technical Context, Contracts §Prohibitions, Research §Planning gaps]
- [ ] CHK024 Is there any unresolved ambiguity between "immediate shortcut confirmation/proxy-only gain" and future temptation/playtest language that could be misread as allowing a mechanical MVP buff, hidden stacking, or balance certainty? [Conflict, Ambiguity, Spec §Clarifications C1, Spec §Clarifications C3, Spec §FR-004, Spec §FR-014, Spec §FR-016, Spec §Edge Cases]

## Traceability

- [ ] CHK025 Do the requirements provide a complete traceability chain from FRs/SCs/clarifications to acceptance scenarios, edge cases, data-model invariants, contracts, and quickstart validation scenarios for Gate 3/testability claims? [Traceability, Spec §FR-001-FR-016, Spec §SC-001-SC-006, Spec §Edge Cases, Plan §Phase 1, Data-Model §Validation rules, Contracts §017 MVP verification targets, Quickstart §Validation scenarios]
- [ ] CHK026 Is there an explicit requirement or convention for linking each MVP verification claim to its evidence class, source artifact, and pending/complete status so reviewers can audit honesty without inferring implementation details? [Traceability, Gap, Spec §FR-015, Spec §SC-005, Spec §Verification Evidence, Contracts §Claim levels, Quickstart §Host-only Gate 3]

## Notes

- Check items off as completed: `[x]`
- Record findings, requirement IDs, or proposed requirement edits inline next to each item.
- This checklist intentionally focuses on requirement quality for verification, Gate 3 honesty, testability, evidence classification, scope exclusions, and provenance boundaries.