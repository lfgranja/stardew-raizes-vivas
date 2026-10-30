# Coverage & Duplication Requirements Quality Checklist: Local OSS Quality Gate

**Purpose**: Validate the quality, clarity, completeness, and measurability of the coverage and duplication threshold requirements before implementation.
**Created**: 2026-10-29
**Feature**: [spec.md](../spec.md)

**Note**: This custom checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied. It does NOT mean implementation work is complete.

## Requirement Completeness

- [x] CHK001 Are the coverage measurement tool (Coverlet) and reporting tool (ReportGenerator) explicitly specified? [Completeness, Spec §FR-007]
- [x] CHK002 Is the duplication measurement tool (jscpd or PMD CPD) explicitly specified? [Completeness, Spec §FR-008]
- [x] CHK003 Are requirements for how the baseline is measured and recorded specified? [Completeness, Spec §FR-009, Gap]
- [x] CHK004 Are requirements for the ratchet mechanism (threshold only increases) specified? [Completeness, Spec §FR-009]
- [x] CHK005 Is the scope of measurement (production assembly only vs. including tests) defined? [Completeness, Gap]
- [ ] CHK006 Are requirements for excluding generated/boilerplate code from coverage specified? [Completeness, Gap]
- [x] CHK007 Are requirements for the initial threshold values or the process to derive them specified? [Completeness, Spec §FR-009]

## Requirement Clarity

- [x] CHK008 Is "coverage" defined with a specific metric (line vs. branch vs. method)? [Clarity, Spec §SC-004]
- [x] CHK009 Is "duplication" defined with a specific metric (lines vs. tokens vs. clones)? [Clarity, Spec §SC-005]
- [ ] CHK010 Is the ratchet rule ("only increases") quantified with a clear trigger (main-branch improvement)? [Clarity, Spec §FR-009]
- [ ] CHK011 Is the process for lowering a threshold (justification required) explicitly defined? [Clarity, Spec §FR-009]

## Requirement Consistency

- [x] CHK012 Do the coverage requirements align with the TDD principle (IV) in the constitution? [Consistency, Constitution §IV]
- [x] CHK013 Are the duplication requirements consistent with the simplicity/YAGNI principle? [Consistency, Constitution §V]
- [x] CHK014 Do the ratchet requirements not conflict with the non-goal of no gameplay changes? [Consistency, Spec §Non-Goals]

## Acceptance Criteria Quality

- [x] CHK015 Is SC-004 measurable and verifiable (coverage does not drop below threshold)? [Acceptance Criteria, SC-004]
- [x] CHK016 Is SC-005 measurable and verifiable (duplication does not exceed threshold)? [Acceptance Criteria, SC-005]
- [x] CHK017 Are the acceptance scenarios for US3 testable independently (introduce regression → check fails)? [Acceptance Criteria, US3]

## Scenario Coverage

- [x] CHK018 Are requirements defined for the scenario where coverage cannot be measured (build fails)? [Coverage, Edge Cases]
- [ ] CHK019 Are requirements defined for how the total duplication ceiling treats newly introduced clones versus legacy clones? [Coverage, Edge Cases]
- [x] CHK020 Are requirements defined for the scenario where the baseline is first established (initial run)? [Coverage, Gap]
- [ ] CHK021 Are requirements defined for the scenario where a threshold is manually overridden? [Coverage, Gap]

## Edge Case Coverage

- [ ] CHK022 Is the fallback behavior specified when the coverage tool fails to produce output? [Edge Cases, Gap]
- [ ] CHK023 Is the behavior specified when the duplication tool encounters unsupported file types? [Edge Cases, Gap]
- [ ] CHK024 Are requirements defined for handling partial/incremental coverage data? [Edge Cases, Gap]

## Non-Functional Requirements

- [x] CHK025 Are CI time requirements for coverage/duplication measurement specified? [NFR, Spec §SC-001]
- [x] CHK026 Are requirements for storing and versioning baseline files specified? [NFR, Gap]

## Dependencies & Assumptions

- [ ] CHK027 Is the assumption that the measured baseline (79.1% lines, one-decimal) is representative and stable validated? [Assumptions, Research]
- [x] CHK028 Is the dependency on Coverlet 6.0.0 (already in test project) documented? [Dependencies, LivingRoots.Tests.csproj]
- [ ] CHK029 Are the research-recommended thresholds (60–70% initial floor) reconciled with the ratchet-from-baseline approach? [Dependencies, Research]

## Ambiguities & Conflicts

- [x] CHK030 Is there any ambiguity in whether thresholds are per-PR or aggregate (main branch)? [Ambiguity, Spec §FR-009]
- [x] CHK031 Is the term "baseline" used consistently across spec, plan, and data model? [Consistency, Terminology]

## Notes

- Items are numbered sequentially for easy reference.
- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- `/speckit-implement` reads checklist state as a gate and must not modify markers.

## Unchecked Items — Reasons and Research Questions

- **CHK006** — No requirement addresses excluding generated/boilerplate code from coverage. *Question: should the gate configure Coverlet exclusion filters (e.g., generated files) and document them?*
- **CHK010** — The ratchet is manual (`ratchet-*` commands); no quantified trigger (e.g., automatic ratchet on main-branch improvement) is specified. *Question: should ratcheting be automated on merge to main, or remain a reviewed manual commit?*
- **CHK011** — "Unless explicitly justified" is stated, but no documented process/steps for lowering a threshold is defined. *Question: what evidence and approval are required to weaken a threshold?*
- **CHK019** — The ceiling is an aggregate total percentage; no distinction between newly introduced clones and legacy clones. *Question: should the gate report or gate on new clones separately (parity gap recorded in sonarcloud-removal-checklist)?*
- **CHK021** — No requirement defines a manual override path for a threshold. *Question: under what conditions may a threshold be overridden, and how is it recorded?*
- **CHK022** — No fallback behavior is specified when the coverage tool fails to produce output. *Question: should this be a configuration error (exit 2) with an explicit message, or a hard fail?*
- **CHK023** — No behavior is specified for unsupported file types in the duplication tool. *Question: should unsupported files be skipped with a warning or fail the gate?*
- **CHK024** — No handling defined for partial/incremental coverage data. *Question: should partial reports be rejected as configuration errors?*
- **CHK027** — The 79.1% line-coverage baseline is a stated measurement, not yet validated as representative/stable (pending T043 re-measurement). *Question: what sample/period validates representativeness?*
- **CHK029** — Research-recommended 60–70% initial floors are not reconciled with the ratchet-from-baseline approach (baseline ~79%). *Question: should the initial floor be the measured baseline or the research-recommended floor, and how is the delta justified?*
