# CI Workflow & Integration Requirements Quality Checklist: Local OSS Quality Gate

**Purpose**: Validate the quality, clarity, completeness, and measurability of the CI workflow and integration requirements before implementation.
**Created**: 2026-10-29
**Feature**: [spec.md](../spec.md)

**Note**: This custom checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied. It does NOT mean implementation work is complete.

## Requirement Completeness

- [ ] CHK001 Are all CI workflow steps (checkout, setup, build, Semgrep, CodeQL, SCA, coverage, duplication) explicitly listed? [Completeness, Spec §FR-003, Plan §contracts]
- [ ] CHK002 Is the trigger configuration (pull_request, push to dev/main) specified? [Completeness, Plan §contracts]
- [ ] CHK003 Is the runner type (ubuntu-latest, standard, no game dependency) specified? [Completeness, Spec §FR-003]
- [ ] CHK004 Are requirements for the SonarCloud parallel-run period (2–4 weeks) specified? [Completeness, Spec §FR-010]
- [ ] CHK005 Are requirements for removing SonarCloud after validation specified? [Completeness, Spec §FR-010, Clarifications]
- [ ] CHK006 Are requirements for the workflow's secrets (BUILDDEPS_TOKEN for CodeQL) documented? [Completeness, Plan §contracts]

## Requirement Clarity

- [ ] CHK007 Is the fast-gate vs. deep-scan split (Semgrep per-PR, CodeQL scheduled) clearly defined? [Clarity, Spec §FR-004, FR-005, Clarifications]
- [ ] CHK008 Is the SonarCloud removal criterion ("2–4 weeks of green runs") quantified? [Clarity, Spec §FR-010]
- [ ] CHK009 Is the workflow's blocking behavior (which steps fail the job) specified? [Clarity, Plan §contracts]
- [ ] CHK010 Is the independence from the dormant Build and Test workflow explicitly stated? [Clarity, Spec §Assumptions]

## Requirement Consistency

- [ ] CHK011 Do the CI requirements align with the constitution's Gate 1 (build/test pass)? [Consistency, Constitution §Quality Gates]
- [ ] CHK012 Are the CI requirements consistent with the non-goal of not changing the Build and Test workflow? [Consistency, Spec §Non-Goals]
- [ ] CHK013 Do the CodeQL requirements not conflict with the fast-gate time budget? [Consistency, Spec §SC-001]

## Acceptance Criteria Quality

- [ ] CHK014 Is SC-001 measurable and verifiable (job runs on standard runner in bounded time)? [Acceptance Criteria, SC-001]
- [ ] CHK015 Is SC-006 measurable and verifiable (file/line diagnostics from CI)? [Acceptance Criteria, SC-006]
- [ ] CHK016 Is SC-007 measurable and verifiable (removal after 2–4 weeks green)? [Acceptance Criteria, SC-007]

## Scenario Coverage

- [ ] CHK017 Are requirements defined for the scenario where the quality gate job fails on a PR? [Coverage, Edge Cases]
- [ ] CHK018 Are requirements defined for the scenario where a check is flaky (intermittent failure)? [Coverage, Gap]
- [ ] CHK019 Are requirements defined for the scenario where the workflow is updated (version bumps)? [Coverage, Gap]
- [ ] CHK020 Are requirements defined for the scenario where SonarCloud is removed (transition validation)? [Coverage, Spec §FR-010]

## Edge Case Coverage

- [ ] CHK021 Is the fallback behavior specified when a GitHub Action is unavailable? [Edge Cases, Gap]
- [ ] CHK022 Is the behavior specified when the build-deps token is missing (CodeQL)? [Edge Cases, Plan §contracts]
- [ ] CHK023 Are requirements defined for handling workflow failures unrelated to code quality? [Edge Cases, Gap]

## Non-Functional Requirements

- [ ] CHK024 Are CI time requirements for the fast gate specified (bounded)? [NFR, Spec §SC-001]
- [ ] CHK025 Are requirements for workflow maintainability (single file, clear steps) specified? [NFR, Gap]
- [ ] CHK026 Are requirements for auditability (pinned versions, logged steps) specified? [NFR, Gap]

## Dependencies & Assumptions

- [ ] CHK027 Is the assumption that the new workflow does not require game assemblies documented? [Assumptions, Spec §Assumptions]
- [ ] CHK028 Is the dependency on the assessment research (tool choices) documented? [Dependencies, Plan §research]
- [ ] CHK029 Are the open questions from the assessment carried forward and tracked? [Dependencies, Spec §Assumptions]

## Ambiguities & Conflicts

- [ ] CHK030 Is there any ambiguity in whether the quality gate blocks merge (branch protection)? [Ambiguity, Spec §FR-003]
- [ ] CHK031 Is the term "standard runner" defined (no special hardware/game install)? [Clarity, Spec §FR-003]

## Notes

- Items are numbered sequentially for easy reference.
- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- `/speckit-implement` reads checklist state as a gate and must not modify markers.