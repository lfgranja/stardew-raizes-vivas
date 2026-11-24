# UX Telegraphing, Legibility & Localization Requirements Quality Checklist: Joja / Agrochemical Tradeoff (MVP)

**Purpose**: Validate completeness, clarity, consistency, measurability, and coverage of requirements for before-use telegraphing, after-use attributed feedback, refusal messaging, cozy/non-punitive framing, legibility (including unknown-baseline and bounded/recoverable consequence), localization parity, and scope clarity for spec 017.
**Created**: 2026-11-06
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [research.md](../research.md) | [data-model.md](../data-model.md) | [contracts/content-registration.md](../contracts/content-registration.md)

**Note**: This custom checklist is generated for reviewer use before implementation and Gate 3 evidence work.
**Review Ownership**: This checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied.
**Marker Semantics**: `[x]` means the criterion has been reviewed and satisfied for requirements quality. It does not mean implementation work is complete.

## Requirement Completeness

- [x] CHK001 Are before-use player-facing requirements fully specified for the Joja/agrochemical input, including that description/framing communicates both “industrial shortcut” and “soil-health cost”? [Completeness, Spec §FR-002, Spec §User Story 2, Contracts §Telegraphed tradeoff copy requirements]
- [x] CHK002 Are after-use feedback requirements specified to require immediate, legible attribution of soil harm to the Joja/agrochemical choice (confirmation/proxy only, not a mechanical buff)? [Completeness, Spec §FR-004, Spec §FR-007, Spec §C1:A, Contracts §Localization contract]
- [x] CHK003 Are refusal-feedback requirements complete for all specified refusal classes (ineligible target, unsupported MVP multiplayer path/farmhand attempt, missing input/conflict as applicable), including explicit reason and no misleading success? [Completeness, Spec §FR-008, Spec §Edge Cases, Spec §User Story 2-3, Data-Model §Application Outcome]
- [x] CHK004 Are legibility requirements defined for the unknown-baseline case so the player-facing outcome explains or remains understandable when unmeasured/`NaN` soil is resolved to the neutral baseline and a measured post-penalty result is recorded? [Completeness, Spec §C2:B, Spec §FR-006, Spec §Edge Cases, Gap] — Resolved 2026-11-06: FR-006/FR-007, edge cases, checklist-resolution clarification, and content contract require understandable feedback that MUST NOT imply prior measurement history or a previously known soil value.
- [x] CHK005 Are localization requirements complete for every touched player-facing text surface in MVP (item name/description/framing, success/attribution feedback, refusal reasons) across `default.json`, `en.json`, and `pt.json`? [Completeness, Spec §FR-012, Contracts §Localization contract, Plan §Technical Context]

## Requirement Clarity

- [x] CHK006 Is the cozy/non-punitive tone requirement (“recompensar, nunca pregar” spirit / informative, no shaming) defined with sufficient specificity to distinguish acceptable from unacceptable player-facing wording? [Clarity, Spec §FR-002, Spec §User Story 2, Contracts §Telegraphed tradeoff copy requirements, Ambiguity] — Resolved 2026-11-06: FR-002, checklist-resolution clarification, and content contract define cozy/non-punitive as factual/informative/no shaming or moralizing; acceptable copy states facts/consequences/options.
- [x] CHK007 Is “immediate, legible after-use feedback” quantified or bounded with objective criteria (timing, content elements, attribution target) rather than left as a general quality statement? [Clarity, Spec §FR-007, Spec §SC-001, Spec §User Story 2, Ambiguity] — Resolved 2026-11-06: FR-007, checklist-resolution clarification, and content contract require immediate feedback after authoritative outcome known with minimum elements (choice/action attribution + soil consequence direction) and no 014/015/vanilla-tier/C2 history implication.
- [x] CHK008 Are “explicit refusal reason” requirements specific enough to define minimum message content (cause, action outcome, and next-step implication where relevant) without prescribing implementation UI mechanics? [Clarity, Spec §FR-008, Contracts §Telegraphed tradeoff copy requirements, Ambiguity] — Resolved 2026-11-06: FR-008, edge cases, checklist-resolution clarification, and content contract define minimum refusal structure (cause, no-success/non-application, next-step implication where relevant).
- [x] CHK009 Is “bounded/recoverable consequence” expressed in player-legible requirement terms (what the player must be able to understand) distinct from internal soil-value validity rules? [Clarity, Spec §FR-005, Spec §User Story 1, Contracts §Telegraphed tradeoff copy requirements, Gap] — Resolved 2026-11-06: FR-005, checklist-resolution clarification, and content contract require player-legible bounded/recoverable communication distinct from internal numeric validity; honest outcomes when bounds reduce visible effect.
- [x] CHK010 Is the acquisition/Joja-only framing scope stated clearly enough to separate required MVP player-facing framing from optional/deferred catalog/shop mechanics (`Data/Shops` not required by default)? [Clarity, Spec §FR-016, Spec §Assumptions, Contracts §Registration boundaries, Research §Planning gaps]

## Requirement Consistency

- [x] CHK011 Are C1 confirmation/proxy-only requirements consistent across spec, plan, data model, and content contract so that shortcut gain is never described as a mechanical buff to 014/015 soil-driven quality/growth or vanilla fertilizer-tier behavior? [Consistency, Spec §C1:A, Spec §FR-004, Plan §Summary, Data-Model §Application Outcome, Contracts §Localization contract]
- [x] CHK012 Are unknown-baseline requirements consistent between C2/FR-006’s neutral baseline (`MaxSoilHealth` 100) plus explicit measured post-penalty result and any player-facing legibility expectations for unmeasured soil? [Consistency, Spec §C2:B, Spec §FR-006, Spec §Edge Cases, Data-Model §Validation rules, Conflict] — Note: Technical baseline/write rules are consistent; legibility expectations are thin (addressed under CHK004) but not conflicting.
- [x] CHK013 Are farmhand/unsupported-path refusal requirements consistent across FR-008, FR-009, user scenarios, data model, and content localization so that refusal occurs before mutation/consumption/false-success and messaging remains non-misleading? [Consistency, Spec §FR-008, Spec §FR-009, Spec §User Story 3, Data-Model §Application Outcome, Contracts §Localization contract]
- [x] CHK014 Is the “no mandatory new art/audio assets” constraint consistent with telegraphing/feedback requirements so text/procedural presentation suffices without implying required new assets? [Consistency, Spec §FR-013, Plan §Technical Context, Contracts §Item/content contract]

## Acceptance Criteria Quality

- [x] CHK015 Can SC-004’s “at least 90% of participants can correctly identify both the short-term shortcut and the soil-health cost” be objectively evaluated from specified method, participant/context criteria, prompt timing, and pass/fail scoring rules? [Acceptance Criteria, Spec §SC-004, Spec §User Story 2, Gap] — Resolved 2026-11-06: SC-004 defines a minimal playtest protocol (participant/context scope, before/immediately-after first-use timing, prompt to identify shortcut + soil-health cost, binary pass requiring both, evidence note with sample size/pass count) and explicitly classifies as playtest follow-up/pending if not run.
- [x] CHK016 Are acceptance scenarios and success criteria traceable to measurable player-facing evidence for before-use telegraphing, after-use attribution, and refusal clarity (not only internal soil-state outcomes)? [Acceptance Criteria, Spec §User Story 1-2, Spec §SC-001, Spec §SC-002, Spec §SC-004, Contracts §Telegraphed tradeoff copy requirements]
- [x] CHK017 Are localization parity acceptance expectations objectively verifiable for touched 017 keys (presence/intent parity across default/en/pt, including refusal and attribution messages)? [Acceptance Criteria, Spec §FR-012, Contracts §Localization contract, Plan §Technical Context]

## Scenario Coverage

- [x] CHK018 Are primary, alternate, and refusal UX scenarios sufficiently covered for a first successful use, a repeat application context, and unsupported farmhand/invalid attempts with corresponding feedback requirements? [Coverage, Spec §User Story 1-3, Spec §Edge Cases, Spec §SC-001, Spec §SC-002]
- [x] CHK019 Are requirements defined for how bounded/recoverable consequence is communicated across immediate post-application feedback and continued normal play inspection (without hidden or silently delayed MVP consequence)? [Coverage, Spec §User Story 1, Spec §FR-005, Spec §FR-007, Contracts §Telegraphed tradeoff copy requirements, Gap] — Resolved 2026-11-06: FR-005/FR-007, US1, checklist-resolution clarification, and content contract require immediate attributed feedback plus player-legible bounded/recoverable consequence in continued normal play through existing soil feedback patterns (no hidden/silently delayed MVP consequence, no new UI/assets required).

## Edge Case Coverage

- [x] CHK020 Are edge-case player-facing requirements specified for minimum-health and tier-boundary outcomes so messages remain honest and non-misleading when the visible soil impact differs from the nominal penalty? [Edge Case, Spec §Edge Cases, Spec §FR-005, Spec §FR-006, Data-Model §Validation rules, Gap] — Resolved 2026-11-06: FR-005, edge cases, checklist-resolution clarification, and content contract require honest/non-misleading outcomes when bounds reduce observed effect (no claim of full nominal penalty when clamped).
- [x] CHK021 Are edge-case requirements defined for the unmeasured/unknown-baseline application path so feedback avoids implying prior measurement history or an invisible no-op? [Edge Case, Spec §C2:B, Spec §FR-006, Spec §SC-001, Spec §Edge Cases, Gap] — Resolved 2026-11-06: FR-006/FR-007, edge cases, checklist-resolution clarification, and content contract explicitly prohibit implying prior measurement history/previously known value or invisible no-op.

## Non-Functional Requirements

- [x] CHK022 Are accessibility/legibility requirements specified as player-facing requirement criteria (readability/understandability of shortcut, cost, attribution, and refusal), not only as tone guidance? [Non-Functional, Spec §User Story 2, Spec §FR-002, Spec §FR-007, Spec §FR-008, Contracts §Telegraphed tradeoff copy requirements, Gap] — Resolved 2026-11-06: FR-002/FR-007, checklist-resolution clarification, and content contract require understandable text/procedural feedback with critical outcome meaning not conveyed by color/asset presence alone; tone guidance does not replace legibility.
- [x] CHK023 Are localization/usability non-functional requirements defined for parity quality across default/en/pt (equivalent meaning for shortcut/cost/attribution/refusal, including tone) rather than only key presence? [Non-Functional, Spec §FR-012, Contracts §Localization contract, Assumption] — Resolved 2026-11-06: FR-012, checklist-resolution clarification, and content contract require meaning/tone parity for touched shortcut/cost/attribution/refusal keys across default/en/pt (exact wording may differ).

## Dependencies & Assumptions

- [x] CHK024 Are dependencies and assumptions about existing soil feedback/synchronization patterns and existing regenerative recovery paths documented clearly enough that 017’s UX legibility requirements do not create unstated dependence on new UI/assets or new recovery mechanics? [Dependencies & Assumptions, Spec §Dependencies, Spec §Assumptions, Spec §FR-013, Plan §Technical Context, Contracts §Item/content contract]
- [x] CHK025 Is the assumption that clear text/procedural feedback can satisfy telegraphing and attribution without mandatory new assets validated or bounded by an explicit fallback/clarity requirement for cases where specified feedback cannot be shown as intended? [Assumption, Spec §FR-013, Contracts §Item/content contract, Plan §Technical Context, Gap] — Resolved 2026-11-06: FR-007/FR-013, edge case, checklist-resolution clarification, and content contract require non-misleading outcome preservation when normal feedback cannot be shown; no false success; absence of HUD message is not success.

## Ambiguities & Conflicts

- [x] CHK026 Is there any unresolved ambiguity or conflict between “Joja-aligned acquisition framing” (player-facing) and the explicit MVP boundary that exact acquisition/catalog/shop mechanics may be finalized later and `Data/Shops` is out by default? [Ambiguity, Spec §Assumptions, Spec §FR-016, Contracts §Registration boundaries, Research §Planning gaps, Conflict] — Note: Boundary is explicit: framing required, mechanics deferred; no unresolved conflict.

## Notes

- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- Leave items unchecked when they still require clarification, correction, or reviewer evaluation.
- This checklist tests requirement quality only; it must not be used as an implementation or QA behavior checklist.
- Traceability target: ≥80% of items include at least one of `[Spec §...]`, `[Plan §...]`, `[Contracts §...]`, `[Research §...]`, `[Data-Model §...]`, `[Gap]`, `[Ambiguity]`, `[Conflict]`, or `[Assumption]`.
- Items are numbered sequentially (`CHK001`–`CHK026`) for review reference.
- Checklist-resolution pass 2026-11-06: **26/26 checked**. All prior candidates resolved in `spec.md` (FR-002/005-009/012/013/015, edge cases, SC-004 protocol), `contracts/content-registration.md`, `contracts/multiplayer-gate3.md`, `plan.md`, `data-model.md`, and `quickstart.md`.
