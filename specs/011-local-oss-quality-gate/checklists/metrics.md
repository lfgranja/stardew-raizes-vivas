# Coverage & Duplication Requirements Quality Checklist: Local OSS Quality Gate

**Purpose**: Validate the quality, clarity, completeness, and measurability of the coverage and duplication threshold requirements before implementation.
**Created**: 2026-10-29
**Feature**: [spec.md](../spec.md)

**Note**: This custom checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied. It does NOT mean implementation work is complete.

## Requirement Completeness

- [ ] CHK001 Are the coverage measurement tool (Coverlet) and reporting tool (ReportGenerator) explicitly specified? [Completeness, Spec §FR-007]
- [ ] CHK002 Is the duplication measurement tool (jscpd or PMD CPD) explicitly specified? [Completeness, Spec §FR-008]
- [ ] CHK003 Are requirements for how the baseline is measured and recorded specified? [Completeness, Spec §FR-009, Gap]
- [ ] CHK004 Are requirements for the ratchet mechanism (threshold only increases) specified? [Completeness, Spec §FR-009]
- [ ] CHK005 Is the scope of measurement (production assembly only vs. including tests) defined? [Completeness, Gap]
- [ ] CHK006 Are requirements for excluding generated/boilerplate code from coverage specified? [Completeness, Gap]
- [ ] CHK007 Are requirements for the initial threshold values or the process to derive them specified? [Completeness, Spec §FR-009]

## Requirement Clarity

- [ ] CHK008 Is "coverage" defined with a specific metric (line vs. branch vs. method)? [Clarity, Spec §SC-004]
- [ ] CHK009 Is "duplication" defined with a specific metric (lines vs. tokens vs. clones)? [Clarity, Spec §SC-005]
- [ ] CHK010 Is the ratchet rule ("only increases") quantified with a clear trigger (main-branch improvement)? [Clarity, Spec §FR-009]
- [ ] CHK011 Is the process for lowering a threshold (justification required) explicitly defined? [Clarity, Spec §FR-009]

## Requirement Consistency

- [ ] CHK012 Do the coverage requirements align with the TDD principle (IV) in the constitution? [Consistency, Constitution §IV]
- [ ] CHK013 Are the duplication requirements consistent with the simplicity/YAGNI principle? [Consistency, Constitution §V]
- [ ] CHK014 Do the ratchet requirements not conflict with the non-goal of no gameplay changes? [Consistency, Spec §Non-Goals]

## Acceptance Criteria Quality

- [ ] CHK015 Is SC-004 measurable and verifiable (coverage does not drop below threshold)? [Acceptance Criteria, SC-004]
- [ ] CHK016 Is SC-005 measurable and verifiable (duplication does not exceed threshold)? [Acceptance Criteria, SC-005]
- [ ] CHK017 Are the acceptance scenarios for US3 testable independently (introduce regression → check fails)? [Acceptance Criteria, US3]

## Scenario Coverage

- [ ] CHK018 Are requirements defined for the scenario where coverage cannot be measured (build fails)? [Coverage, Edge Cases]
- [ ] CHK019 Are requirements defined for the scenario where duplication is introduced by a new clone vs. legacy clones? [Coverage, Edge Cases]
- [ ] CHK020 Are requirements defined for the scenario where the baseline is first established (initial run)? [Coverage, Gap]
- [ ] CHK021 Are requirements defined for the scenario where a threshold is manually overridden? [Coverage, Gap]

## Edge Case Coverage

- [ ] CHK022 Is the fallback behavior specified when the coverage tool fails to produce output? [Edge Cases, Gap]
- [ ] CHK023 Is the behavior specified when the duplication tool encounters unsupported file types? [Edge Cases, Gap]
- [ ] CHK024 Are requirements defined for handling partial/incremental coverage data? [Edge Cases, Gap]

## Non-Functional Requirements

- [ ] CHK025 Are CI time requirements for coverage/duplication measurement specified? [NFR, Spec §SC-001]
- [ ] CHK026 Are requirements for storing and versioning baseline files specified? [NFR, Gap]

## Dependencies & Assumptions

- [ ] CHK027 Is the assumption that the measured baseline (79.1% lines, one-decimal) is representative and stable validated? [Assumptions, Research]
- [ ] CHK028 Is the dependency on Coverlet 6.0.0 (already in test project) documented? [Dependencies, LivingRoots.Tests.csproj]
- [ ] CHK029 Are the research-recommended thresholds (60–70% initial floor) reconciled with the ratchet-from-baseline approach? [Dependencies, Research]

## Ambiguities & Conflicts

- [ ] CHK030 Is there any ambiguity in whether thresholds are per-PR or aggregate (main branch)? [Ambiguity, Spec §FR-009]
- [ ] CHK031 Is the term "baseline" used consistently across spec, plan, and data model? [Consistency, Terminology]

## Notes

- Items are numbered sequentially for easy reference.
- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- `/speckit-implement` reads checklist state as a gate and must not modify markers.