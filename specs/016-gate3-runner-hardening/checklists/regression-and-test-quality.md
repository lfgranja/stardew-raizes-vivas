# Requirements Quality Checklist: Regression and Test Quality (Gate 3 Runner Hardening)

**Purpose**: Validate that the regression-guard, red-gate, and coverage requirements for Feature 016 are complete, clear, consistent, and measurable — as a "unit test" for the requirements themselves.
**Created**: 2026-11-05
**Feature**: [spec.md](../spec.md)

**Note**: This checklist tests whether requirements are well-written, not whether implementation works. Items are left unchecked for review.

## Requirement Completeness

- [ ] CHK001 - Are the three defects (save-name mismatch, two-line SMAPI excerpt, schema-invalid manifest) each mapped to a named regression guard with an explicit failure mode? [Completeness, Spec §FR-001, §FR-002, §FR-003, §US1]
- [ ] CHK002 - Is the revert → fail → restore → pass pattern specified as the required acceptance methodology for every regression guard? [Completeness, Spec §US1 Independent Test, §SC-003, §SC-004]
- [ ] CHK003 - Is the red-gate deviation documentation (T130) required per log, including the "implementation pre-existed" and "not fabricated" statements? [Completeness, Spec §FR-007, §US3]
- [ ] CHK004 - Is the `claims.json`-before-manifest ordering (FR-006) specified as a testable, verifiable condition? [Completeness, Spec §FR-006]
- [ ] CHK005 - Is the original `HOME` preservation (FR-008) specified with an observable verification criterion? [Completeness, Spec §FR-008]

## Requirement Clarity

- [ ] CHK006 - Is the save-name alignment failure mode ("align or fail loudly with a named cause") quantified with a specific exit code and message? [Clarity, Spec §FR-001, §US1 AS1]
- [ ] CHK007 - Is the SMAPI capture-level contract (full default, documented opt-in excerpt) unambiguous about which artifact must remain full? [Clarity, Spec §FR-002, Clarifications §2026-11-05 Q1, Research §Unknown 2]
- [ ] CHK008 - Is the red-gate deviation marker format (required text/structure) specified so reviewers can verify it consistently? [Clarity, Spec §FR-007, §US3 AS1]
- [ ] CHK009 - Is the subject[] completeness rule specified as a filesystem rule requiring no schema version bump unless the field set changes? [Clarity, Spec §FR-005, Clarifications §2026-11-05 Q2]

## Requirement Consistency

- [ ] CHK010 - Is SC-002 (every red-gate log contains a failing assertion) reconciled with FR-007 (no fabricated RED evidence) via the documented reinterpretation? [Consistency, Spec §SC-002, §FR-007, Research §Unknown 5]
- [ ] CHK011 - Are the regression guards required to follow TDD tests-first ordering per constitution Principle IV? [Consistency, Plan §Constitution Check Principle IV, Spec §FR-007]
- [ ] CHK012 - Is the verifier completeness guard (SC-001) consistent with FR-005's filesystem rule and FR-004's pre-return-0 validation list? [Consistency, Spec §SC-001, §FR-004, §FR-005]

## Acceptance Criteria Quality

- [ ] CHK013 - Is SC-003 (save-name alignment covered by an automated test that fails when the fix is reverted) measurable with a concrete revert trigger and pass/fail signal? [Measurability, Spec §SC-003]
- [ ] CHK014 - Is SC-004 (full SMAPI log capture covered by an automated test that fails when the excerpt is reintroduced) measurable with a concrete reintroduction trigger? [Measurability, Spec §SC-004]

## Edge Case Coverage

- [ ] CHK015 - Are the two specified edge cases (new file type added to bundle layout; retention-receipt written after finalization) each mapped to an explicit verifier rejection with exit 19? [Edge Case, Spec §Edge Cases, §US2 AS1, §SC-001]

## Traceability

- [ ] CHK016 - Does ≥80% of checklist items include at least one traceability reference (FR-###, SC-###, US#, or plan/constitution citation)? [Traceability]

## Notes

- Items left unchecked for reviewer confirmation.
- `[Gap]` would indicate a requirement potentially missing from the spec; none flagged here beyond the reconciliations noted.
- `/speckit-implement` reads checklist checkbox state as a gate but does not modify markers.