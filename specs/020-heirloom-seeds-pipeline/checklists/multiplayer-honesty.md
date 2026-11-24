# Multiplayer & Honesty Checklist: Heirloom Seeds / Sementes Crioulas Pipeline (Crioulo Parsnip)

**Purpose**: Unit tests for the requirements — validate that the spec/plan fully and unambiguously pin down (a) multiplayer authority for crioulo soil effects, (b) honesty of evidence and claims, and (c) the distribution/shop contract, before implementation locks them in.
**Created**: 2026-11-09
**Feature**: [specs/020-heirloom-seeds-pipeline/spec.md](../spec.md) — reviewer-owned artifact

**Review Ownership**: This checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied. `[x]` does not mean implementation is done. `/speckit.implement` must not modify markers here.

## Multiplayer Authority Requirements

- [ ] CHK001 Is the host-authoritative write path for `HeirloomPlantBonus` specified completely enough to be verified without the author present? [Completeness, Spec §FR-006]
- [ ] CHK002 Is the farmhand path (request → host validates/applies/broadcasts) specified with explicit failure semantics when the host is unreachable? [Completeness, Spec §FR-012, Clarifications A3]
- [ ] CHK003 Are the message type name, envelope scope (`modIDs`), and schema fields for the farmhand request specified normatively? [Clarity, Contracts HE-1]
- [ ] CHK004 Is the idempotency/correlation rule for repeated `HeirloomPlantBonusRequest` deliveries specified (double-apply prevention)? [Clarity, Contracts HE-1]
- [ ] CHK005 Do the plant-bonus and harvest-bonus authority requirements conflict with each other anywhere? [Consistency, Spec §FR-006 vs §FR-007]
- [ ] CHK006 Is the first-wins/lock semantics on concurrent host+farmhand tile interaction stated for this slice (not only by reference to 017)? [Completeness, Spec §FR-012]
- [ ] CHK007 Is it specified which outcomes are allowed when host applies the request out of turn or on a drifted tile state? [Edge Case, Contracts HE-1]
- [ ] CHK008 Is Item.modData usage (if any) explicitly bounded — sync risk, size, and migration behavior? [Assumption, Plan §D6/research D-items]
- [ ] CHK009 Is the Junimo exemption (no seed save; 014 semantics) stated consistently in FR-009 and Clarifications? [Consistency, Spec §FR-009]
- [ ] CHK010 Does the spec say what happens when a farmhand harvests while the host is in a different location/day pipeline? [Gap, Spec §Edge Cases vs §FR-012]

## Honesty / Evidence Requirements

- [ ] CHK011 Does the spec forbid producing `human-verified` or `d1-farmhand-convergence` claims in this slice, and is that ban unambiguous? [Completeness, Spec §FR-014]
- [ ] CHK012 Is the `machine-pass` host-only evidence class (honesty R) for the new Gate 3 scenario specified with its expected signals? [Clarity, Contracts HE-6]
- [ ] CHK013 Are D1/human/SC-comprehension statuses explicitly carried as `pending` (not `partial`/`pass`) in the spec's verification story? [Completeness, Spec §SC-006]
- [ ] CHK014 Is placeholder-art disclosure required by the output docs (validate/release/reports), not optional? [Completeness, Spec §FR-017]
- [ ] CHK015 Is the "no release/tag/publish this slice" boundary stated as a requirement rather than a preference? [Clarity, Spec §FR-015]
- [ ] CHK016 Are abstractions (`+2`/`+1` soil writes) forbidden from being presented as agronomic constants in all player-facing text? [Consistency, Spec §FR-011]
- [ ] CHK017 Is nitrogen-fixation vocabulary (and any "always nutritious" claims) explicitly barred from the i18n copy requirements? [Completeness, Spec §FR-011, research §Agronomy]

## Distribution & Shop Contract Requirements

- [ ] CHK018 Is the `Traveler` entry's `AvailableStockLimit` requirement (`Player`) testable and specified? [Clarity, Spec §FR-003]
- [ ] CHK019 Is the fallback if `SEASON Spring` condition is unsupported at runtime specified (not silently dropped)? [Edge Case, Contracts C-4/FR-003]
- [ ] CHK020 Is it specified that Pierre/Joja shops are untouched, in a way that can be asserted (not merely intended)? [Clarity, Spec §FR-004]
- [ ] CHK021 Is the seed-save guarantee (exactly 1 per harvest) specified without ambiguity about stack size, chance, or farming-level scaling? [Clarity, Spec §FR-009]
- [ ] CHK022 Are the cart/Stock odds and cart-availability limitations documented as tuning (not correctness)? [Assumption, Spec §Edge Cases]
- [ ] CHK023 Is the Seed Maker path role (produce→seeds) documented as optional recovery, never as the only acquisition path? [Completeness, research §D1]

## Requirements-Quality Crosscut

- [ ] CHK024 Can every SC-00n be objectively checked against artifacts (tests, logs, bundles) without the author's presence? [Measurability, Spec §SC-001..SC-007]
- [ ] CHK025 Are FR-to-SC traceability links recorded (as in the spec's Verification notes)? [Traceability, checklists/requirements.md]
- [ ] CHK026 Is there an explicit cap on what "done" means for claim promotion this cycle? [Clarity, Contracts HE-6]

## Notes

- Items unchecked by design; reviewer owns markers.
- High-risk items: CHK004 (double-apply), CHK002 (unreachable host), CHK031-class drift between HE-1 and FR-006 if wording changes.
- If any answer is "no", fix the requirement text, not the checklist.
