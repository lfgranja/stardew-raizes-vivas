# Specification Quality Checklist: Heirloom Seeds / Sementes Crioulas Pipeline (Crioulo Parsnip)

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-09
**Feature**: [specs/020-heirloom-seeds-pipeline/spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs) — the spec names seams at the *behavioral contract* level (Harmony harvest seam, `HoeDirt.plant`, host-authority) as requirements, not as code style; all FRs are testable behaviors.
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain (decisions recorded in Clarifications 2026-11-09; open validation outcomes are recorded as explicit fallback rules, not open questions)
- [x] Requirements are testable and unambiguous (FR-001..FR-018 each carry observable asserts)
- [x] Success criteria are measurable (SC-001..SC-007; counts, suite gates, locale parity, machine-pass)
- [x] Success criteria are technology-agnostic (no CI toolchain specifics beyond runner-neutral gates; single build/test command noted only where the project itself defines them)
- [x] All acceptance scenarios are defined (US1–US4; Given/When/Then; host + farmhand + refund flows)
- [x] Edge cases are identified (unknown/NaN, bounds, inventory-full, MP conflict, junk cart, save-without-mod)
- [x] Scope is clearly bounded (one crop; no Elena; no SeedShop; no release/tag; placeholder art; no public channel)
- [x] Dependencies and assumptions identified (SDV/SMAPI floor; Traveler semantics; FR-009/A8 sync; sparse-zero limitation; art)

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria (mapped to US/SC in the verification notes below)
- [x] User scenarios cover primary flows (acquire → sow → harvest → save → understand → verify)
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification (would-be implementation specifics live in Clarifications as *recorded decisions*, explicitly flagged as 013/017-style spec contracts)

## Verification notes (specner / reviewer)

- FR-F mapping: FR-001/002/005 → SC-001/SC-007; FR-003 → US1-AC1; FR-006/007/008/012 → SC-003/SC-006; FR-009 → SC-002; FR-010/011 → SC-005; FR-014 → SC-004/SC-006; FR-015/016/017 → SC-006 (honesty + docs).
- Pending-by-design (do not check off without evidence): SC-004 D1/farmhand live convergence; SC-004 human E; SC-004 SC-heirloom-comprehension (qualitative follow-up protocol, if ever run).
- Checklist lifecycle: reviewer-owned markers; `/speckit.implement` must not modify this file.

## Notes

- Any unchecked item blocks `/speckit-clarify` exit cleanliness; report gaps rather than checking unreviewed items.
- Deliberate MVP descoping (one crop, no Elena, no SeedShop, no release/tag, placeholder art) is intentional and consistent with the assess decision (GO, Option B); scope creep back to Option A is the principal review failure mode.
