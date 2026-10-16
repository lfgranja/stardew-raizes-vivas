# Specification Quality Checklist: Soil Health → Crop Growth

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-15
**Feature**: [spec.md](spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs) — the spec references `Crop.newDay` and `HoeDirt.GetFertilizerSpeedBoost()` only as assumption descriptions (A1), not as code design; no `Harmony.Patch` syntax, no `ModConstants` values, no C# structure proposed.
- [x] Focused on user value and business needs — Problem (measurement without gameplay), Success Criteria (growth/quality/extra-yield change, manual play-test), Non-Goals (no NPC, no asset, no config, no multiplayer transport).
- [x] Written for non-technical stakeholders — every success criterion describes what the player observes (faster growth, better harvest, manual loop confirmation), not internal method names.
- [x] All mandatory sections completed — Problem, User Scenarios & Testing (mandatory), Requirements, Key Entities, Success Criteria, Non-Goals, Dependencies & Assumptions, Open Questions.

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain — 4 of 5 resolved in `Clarifications` (Q1 hook, Q2 scale, Q3 growth multipliers, Q4 quality/extra-yield); 1 deferred (`Rollback / ReGen Valley`, requires `SC-008` verification before plan — documented in `Dependencies` / `Open Questions`). Per stage rules, deferred item is verification prerequisite, not spec ambiguity.
- [x] Requirements are testable and unambiguous — FR-001 through FR-009 each have measurable targets; SC-001 through SC-008 are quantitative (time difference) or qualitative (manual confirmation) but verifiable.
- [x] Success criteria are measurable — SC-001 to SC-005 are time/count/roll metrics; SC-006 (overlay) and SC-007 (save/reload) are existing behavior preserved; SC-008 is manual.
- [x] Success criteria are technology-agnostic — no `dotnet test`, `Harmony`, `ModConstants`, `Crop.newDay` syntax appears in criteria; only the *outcome* (growth time, quality roll, extra yield) is specified.
- [x] All acceptance scenarios are defined — US1 (growth), US2 (quality), US3 (extra harvest), US4 (trust pipeline / verification) — each with given/when/then.
- [x] Edge cases are identified — Medium-band parity, full-inventory harvest (FR-005b), dislodgement mid-batch (FR-005c), concurrent action (FR-019 / Principle VI), zero-health baseline (FR-009 — neutral dirt, no overlay), save/reload of progress, corrupt save entry quarantine (FR-012 / FR-014 / FR-015).
- [x] Scope is clearly bounded — Non-Goals list 10 exclusions; no new asset/item/recipe/character/building/quest; no `ContentPatcher`; no `config.json` / GMCM.
- [x] Dependencies and assumptions identified — A1 through A5 (hook, composition, scale, multi-mod, manual play-test); Requires (spec 004, 001, 002; rollback fix; band decision); Dependencies (spike of hook, `CollectCompost` fix, band reconciliation).

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria — FR-001 to FR-009 map to SC-001 to SC-008.
- [x] User scenarios cover primary flows — US1, US2, US3 cover grow / quality / extra; US4 covers verification (the only gap).
- [x] Feature meets measurable outcomes defined in Success Criteria — yes, by design (SC-001–005 measurable; SC-008 manual).
- [x] No implementation details leak into specification — confirmed clean (FR-001/002/003 reference `ModConstants` names only, no numerical values; assumptions reference `Crop.newDay` / `HoeDirt.GetFertilizerSpeedBoost()` as option descriptions only; calibrated values documented in `Clarifications` 2026-10-15, not in functional design or success criteria).

## Validation Iteration

- **Iteration 2** (post-clarification): 4 of 5 clarifications resolved (Q1–Q4); 1 deferred (`Rollback / ReGen Valley`, requires `SC-008` verification — documented in `Dependencies` verification gate / `Open Questions` / `Clarifications`). Checklist: 16/16 content-quality items passing (line 30 now checked); no regressions; 0 newly failing.
- **Issues found**: None blocking. Deferred item (rollback verification) is a prerequisite from spec 004, not a spec ambiguity — it does not block clarification completeness.
- **Resolution**: All 5 questions answered/recorded; 4 integrated into `spec.md`; 1 deferred with verification gate. `Clarifications` section created; `Open Questions` updated; checklist verified.
- **Status**: **Ready for `/speckit.plan`** — with condition: `SC-008` (spec 004 rollback verification) must pass before any yield-increase implementation activates (`FR-005`; `Dependencies` verification gate). `ReGen Valley` interaction is documented (`Assumes`), not solved, consistent with scope.

## Notes

- The feature is intentionally narrow (Option A — hook only) to match the assessment's `needs-clarification` verdict; a broader scope (Option B — mulch + pipeline + quality) is documented in `concept.md` but excluded from this spec.
- The `ReGen Valley` competitor (Nexus 39740) is noted as a reference in `research.md`; this spec does not claim novelty over it — the differentiation (agroecological framing, multiplayer correctness, honest documentation) is out of scope for this feature.
- The `CollectCompost` rollback fix (prerequisite) is not part of this spec's scope — it must be completed separately, per `problem.md` and `concept.md`.
