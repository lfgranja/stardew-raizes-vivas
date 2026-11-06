# Multiplayer Authority Checklist: 017 Joja / Agrochemical Tradeoff (MVP)

**Purpose**: Requirements-quality review for multiplayer authority, refusal ordering, synchronization, and safety boundaries in spec 017.
**Created**: 2026-11-06
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [research.md](../research.md) | [data-model.md](../data-model.md) | [contracts/multiplayer-gate3.md](../contracts/multiplayer-gate3.md) | [contracts/agrochemical-application-service.md](../contracts/agrochemical-application-service.md)

**Note**: This checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied.
**Marker Semantics**: `[x]` means the criterion has been reviewed and satisfied for requirements quality. It does not mean implementation work is complete.

## Requirement Completeness

- [x] CHK001 Are host-authoritative requirements for shared soil mutation fully specified, including the authoritative actor, permitted mutation path, and prohibition of a second source of truth? [Completeness, Spec §FR-009, Plan §Technical Context, Contracts §multiplayer-gate3.md, Constitution VI]
- [x] CHK002 Are requirements defined for every mandatory effect of a successful host application in shared-farm context (lock/conflict handling, measured soil commit, item consumption ordering, confirmation feedback, and post-success snapshot broadcast)? [Completeness, Spec §US3-AS1, Contracts §multiplayer-gate3.md, Contracts §agrochemical-application-service.md]
- [x] CHK003 Are unsupported farmhand/MVP-path refusal requirements complete, explicitly covering refusal before soil mutation, item consumption, and false-success feedback? [Completeness, Spec §FR-008, Spec §FR-009, Spec §Clarifications C4:A, Contracts §multiplayer-gate3.md]
- [x] CHK004 Are requirements specified for missing authority/lock safety behavior, including fail-closed expectations for authoritative operations? [Completeness, Contracts §multiplayer-gate3.md]
- [x] CHK005 Are FR-009 snapshot synchronization requirements complete for both host broadcast timing and farmhand/apply boundaries after host success? [Completeness, Spec §FR-009, Spec §US3-AS1, Contracts §multiplayer-gate3.md, Plan §Summary]
- [x] CHK006 Are requirements defined for what must remain unchanged on refusal (inventory/item count, local/shared soil state, and absence of success/broadcast signals)? [Completeness, Spec §FR-008, Data-Model §Application Outcome, Contracts §agrochemical-application-service.md]
- [x] CHK007 Are follow-up boundaries (farmhand intent/ack host-mediated protocol and D1 real-farmhand convergence) explicitly documented as out of MVP claim scope while still present in requirements context? [Completeness, Spec §FR-009, Spec §Assumptions, Contracts §multiplayer-gate3.md]

## Requirement Clarity

- [x] CHK008 Is "host-authoritative MVP" quantified with clear scope boundaries for what farmhand actors can and cannot do in 017? [Clarity, Spec §FR-009, Spec §Clarifications C4:A, Plan §Summary]
- [x] CHK009 Is the refusal ordering "before any soil mutation, item consumption, or false-success feedback" defined precisely enough to identify the protected side effects and their required sequence? [Clarity, Spec §FR-008, Spec §US3-AS2, Contracts §agrochemical-application-service.md]
- [x] CHK010 Is "no second source of truth" clarified with concrete state domains included/excluded (shared soil mutation authority, save/SMAPI messaging patterns, and disallowed local authoritative divergence)? [Clarity, Spec §FR-009, Plan §Technical Context, Contracts §multiplayer-gate3.md, Constitution VI]
- [x] CHK011 Are "explicit refusal," "misleading success," and "unsafe local-only soil mutation" defined with observable requirement-level criteria rather than subjective language? [Clarity, Spec §FR-008, Spec §US3-AS2, Data-Model §Application Outcome]
- [x] CHK012 Is the FR-009 "existing shared soil-state synchronization boundary" reference specific enough for reviewers to determine compatibility without inferring undocumented behavior? [Clarity, Spec §US3-AS1, Spec §FR-009, Plan §Summary, Gap]
- [x] CHK013 Are claim/evidence levels (automated, host-only machine evidence, human `verified`, D1 farmhand convergence) distinguished with non-overlapping definitions and promotion boundaries? [Clarity, Spec §FR-015, Spec §US4-AS2, Contracts §multiplayer-gate3.md]

## Requirement Consistency

- [x] CHK014 Are authority and refusal requirements consistent across spec, data model, and both contracts for unsupported farmhand attempts? [Consistency, Spec §FR-008, Spec §FR-009, Data-Model §Application Outcome, Contracts §multiplayer-gate3.md, Contracts §agrochemical-application-service.md]
- [x] CHK015 Are first-wins/conflict requirements consistent between per-tile lock behavior, refusal outcomes, and the prohibition on silent last-write-wins corruption? [Consistency, Spec §FR-010, Spec §US3-AS3, Plan §Technical Context, Contracts §multiplayer-gate3.md, Contracts §agrochemical-application-service.md]
- [x] CHK016 Are post-success synchronization requirements consistent about broadcasting only after successful host commit and never on refusal/failure/conflict? [Consistency, Spec §US3-AS1, Data-Model §State transitions, Contracts §multiplayer-gate3.md, Contracts §agrochemical-application-service.md]
- [x] CHK017 Are MVP boundary statements consistent in presenting farmhand intent/ack as follow-up while still requiring safe explicit refusal in the current release? [Consistency, Spec §Clarifications C4:A, Spec §FR-009, Spec §Assumptions, Research §Decision summary, Plan §Constitution Check]

## Acceptance Criteria & Scenario Coverage

- [x] CHK018 Can multiplayer authority and refusal requirements be objectively evaluated through acceptance criteria that distinguish successful host paths, unsupported farmhand paths, and conflict refusals? [Acceptance Criteria, Spec §US3, Spec §SC-002, Contracts §multiplayer-gate3.md]
- [x] CHK019 Are acceptance criteria defined for synchronization compatibility after host success without implying human `verified` or D1 farmhand convergence claims? [Acceptance Criteria, Spec §US3-AS1, Spec §FR-015, Spec §SC-005, Contracts §multiplayer-gate3.md]
- [x] CHK020 Are primary, alternate, and exception multiplayer flows all covered by requirements (host success, farmhand unsupported refusal, per-tile contention, and unsupported/invalid attempt classes)? [Coverage, Spec §US3, Spec §FR-008, Spec §FR-010, Contracts §agrochemical-application-service.md]
- [x] CHK021 Are recovery/rollback requirements specified for failed or refused multiplayer attempts so shared state, item state, and player feedback return to a safe, non-misleading condition? [Coverage, Spec §FR-008, Spec §SC-002, Data-Model §Application Outcome, Gap]

## Edge Case & Safety Coverage

- [x] CHK022 Are edge-case requirements defined for concurrent or conflicting soil-changing actions across supported and unsupported multiplayer paths, including explicit outcomes for each losing/unsupported actor? [Edge Case, Spec §Edge Cases, Spec §US3-AS3, Spec §FR-010]
- [x] CHK023 Are requirements specified for save/load and shared-state consistency after authoritative host success, ensuring persisted soil results remain compatible with existing multiplayer synchronization expectations? [Edge Case, Spec §Edge Cases, Spec §FR-011, Spec §Dependencies, Gap]
- [x] CHK024 Are safety-boundary requirements sufficient to prevent misleading trust signals, including prohibition of false-success feedback, refusal-path broadcasts, and unsafe local soil mutation under contention or unsupported authority? [Non-Functional, Spec §FR-008, Spec §FR-009, Spec §SC-002, Contracts §multiplayer-gate3.md]
- [x] CHK025 Does the specification acknowledge planning constraints where in-process locking is not cross-client arbitration, and does it limit multiplayer claims accordingly? [Non-Functional, Plan §Technical Context, Plan §Constitution Check, Research §Decision summary, Contracts §multiplayer-gate3.md, Assumption]

## Dependencies, Assumptions & Conflicts

- [x] CHK026 Are dependencies on existing host-authoritative multiplayer rules, FR-009 soil snapshot sync concepts, and project verification honesty rules documented with requirement-level impact? [Dependency, Spec §Dependencies, Spec §FR-009, Spec §FR-015, Plan §Technical Context]
- [x] CHK027 Are assumptions about host-only MVP support, follow-up intent/ack/D1 evidence, and in-process lock limitations validated and bounded so they do not silently expand the claimed multiplayer safety model? [Assumption, Spec §Assumptions, Plan §Constitution Check, Research §Gaps, Contracts §multiplayer-gate3.md]
- [x] CHK028 Is there any unresolved conflict between "Multiplayer First-Class" expectations and the MVP boundary that refuses unsupported farmhand paths, and are acceptance/claim boundaries explicit enough to prevent overstatement? [Conflict, Spec §FR-009, Spec §FR-015, Plan §Constitution Check, Contracts §multiplayer-gate3.md, Constitution VI]

## Notes

- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- Leave items unchecked when they still require clarification, correction, or reviewer evaluation.
- `/speckit.implement` reads checklist checkbox state as a gate and must not modify markers.
- `checklists/requirements.md` has a separate built-in lifecycle maintained by `/speckit.specify` and `/speckit.clarify`.
- Add findings inline next to affected `CHK###` items and link the specific spec/plan/contract passage reviewed.
- Items are numbered sequentially for easy reference.