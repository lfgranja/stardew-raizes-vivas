# CI Workflow & Integration Requirements Quality Checklist: Local OSS Quality Gate

**Purpose**: Validate the quality, clarity, completeness, and measurability of the CI workflow and integration requirements before implementation.
**Created**: 2026-10-29
**Feature**: [spec.md](../spec.md)

**Note**: This custom checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied. It does NOT mean implementation work is complete.

## Requirement Completeness

- [x] CHK001 Are all CI workflow steps (checkout, setup, build, Semgrep, CodeQL, SCA, coverage, duplication) explicitly listed? [Completeness, Spec §FR-003, Plan §contracts] — contract table enumerates every step.
- [x] CHK002 Is the trigger configuration (pull_request, push to dev/main) specified? [Completeness, Plan §contracts] — contract Trigger line.
- [x] CHK003 Is the runner type and per-job game dependency (ubuntu-latest; static/sca game-free, build-analysis uses the pinned build-deps) specified? [Completeness, Spec §FR-003] — contract Game dependency section.
- [x] CHK004 Are requirements for the SonarCloud parallel-run period (2–4 weeks) specified? [Completeness, Spec §FR-010] — FR-010 + SC-007.
- [x] CHK005 Are requirements for the deferred SonarCloud removal follow-up specified? [Completeness, Spec §FR-010, Clarifications] — Non-Goals + removal checklist task.
- [x] CHK006 Are requirements for the workflow's secrets (BUILDDEPS_TOKEN for build-analysis; none for CodeQL) documented? [Completeness, Plan §contracts] — contract Inputs/secrets table.
- [ ] CHK018 Are requirements defined for the scenario where a check is flaky (intermittent failure)? [Coverage, Gap] — no retry/rerun/flaky policy specified.
- [ ] CHK019 Are requirements defined for the scenario where the workflow is updated (version bumps)? [Coverage, Gap] — pin policy exists, but no version-bump/change process requirement.
- [ ] CHK021 Is the fallback behavior specified when a GitHub Action is unavailable? [Edge Cases, Gap] — no fallback specified.
- [ ] CHK023 Are requirements defined for handling workflow failures unrelated to code quality? [Edge Cases, Gap] — only aggregate fail-closed on job results; no infra-failure policy.

## Requirement Clarity

- [x] CHK007 Is the fast-gate vs. deep-scan split (Semgrep per-PR, CodeQL scheduled) clearly defined? [Clarity, Spec §FR-004, FR-005, Clarifications] — Clarifications + FR-004/FR-005.
- [x] CHK008 Is the SonarCloud removal criterion ("2–4 weeks of green runs") quantified? [Clarity, Spec §FR-010] — FR-010 defines green run.
- [x] CHK009 Is the workflow's blocking behavior (which steps fail the job) specified? [Clarity, Plan §contracts] — contract Exit codes/failure behavior.
- [x] CHK010 Is the relationship to the gated Build and Test workflow explicitly stated (separate check, shared build-deps pattern, not dormant)? [Clarity, Spec §Assumptions] — Assumptions section.
- [ ] CHK030 Is there any ambiguity in whether the quality gate blocks merge (branch protection)? [Ambiguity, Spec §FR-003] — branch protection is a maintainer action; spec does not mandate required checks, so merge-blocking status is not fully specified.

## Requirement Consistency

- [x] CHK011 Do the CI requirements align with the constitution's Gate 1 (build/test pass)? [Consistency, Constitution §Quality Gates] — Plan Constitution Check.
- [x] CHK012 Are the CI requirements consistent with the non-goal of not changing the Build and Test workflow? [Consistency, Spec §Non-Goals] — Non-Goals + Assumptions.
- [x] CHK013 Do the CodeQL requirements not conflict with the fast-gate time budget? [Consistency, Spec §SC-001] — CodeQL is scheduled, not per-PR.

## Acceptance Criteria Quality

- [x] CHK014 Is SC-001 measurable and verifiable (job runs on standard runner in bounded time)? [Acceptance Criteria, SC-001] — minutes + runner stated.
- [x] CHK015 Is SC-006 measurable and verifiable (file/line diagnostics from CI)? [Acceptance Criteria, SC-006] — diagnostic formats stated.
- [x] CHK016 Is SC-007 measurable and verifiable (removal after 2–4 weeks green)? [Acceptance Criteria, SC-007] — green-run criteria stated.

## Scenario Coverage

- [x] CHK017 Are requirements defined for the scenario where the quality gate job fails on a PR? [Coverage, Edge Cases] — contract failure behavior + Edge Cases.
- [x] CHK020 Are requirements defined for the scenario where SonarCloud is removed (transition validation)? [Coverage, Spec §FR-010] — FR-010 + removal checklist task.
- [x] CHK022 Is the behavior specified when the build-deps token is missing (CodeQL)? [Edge Cases, Plan §contracts] — fail closed with explicit message (contract step 13).

## Edge Case Coverage

## Non-Functional Requirements

- [x] CHK024 Are CI time requirements for the fast gate specified (bounded)? [NFR, Spec §SC-001] — ≤10 min static/sca.
- [x] CHK025 Are requirements for workflow maintainability (single file, clear steps) specified? [NFR, Gap] — plan Structure Decision + contract table.
- [x] CHK026 Are requirements for auditability (pinned versions, logged steps) specified? [NFR, Gap] — pin policy + contract.

## Dependencies & Assumptions

- [x] CHK027 Is the assumption that the new workflow does not require game assemblies documented? [Assumptions, Spec §Assumptions] — Assumptions.
- [x] CHK028 Is the dependency on the assessment research (tool choices) documented? [Dependencies, Plan §research] — plan Phase 0 + research.md.
- [x] CHK029 Are the open questions from the assessment carried forward and tracked? [Dependencies, Spec §Assumptions] — Assumptions list.

## Ambiguities & Conflicts

- [x] CHK031 Is the term "standard runner" defined (no special hardware/game install)? [Clarity, Spec §FR-003] — ubuntu-latest, no game install for static/sca.

## Notes

- Items are numbered sequentially for easy reference.
- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- `/speckit-implement` reads checklist state as a gate and must not modify markers.