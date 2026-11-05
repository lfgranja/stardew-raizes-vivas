# Honesty & Governance Requirements Quality Checklist: Gate 3 Runner Hardening

> **Review status: requirements-text review only.** A `[x]` mark is a requirements-quality judgment about the specification text, not execution evidence. Implementation attestation is pending T149.

**Purpose**: Validate that honesty-ladder preservation, promotion rules, single promotion owner, T146 gating, T148 human E attestation, T138/T145 blocker documentation, claim-overreach prevention, and attestation-boundary/audit-trail requirements are complete, clear, measurable, consistent, and covered.
**Created**: 2026-11-05
**Feature**: [spec.md](../spec.md)

**Review Ownership**: This checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied.

**Note**: Items test whether the *requirements themselves* are well-written — not whether the implementation works.

---

## Honesty Ladder Preservation (partial / machine-pass / verified)

- [ ] CHK001 - Is the three-value honesty ladder (`partial` → `machine-pass` → `verified`) explicitly preserved as a closed enum with no additional states introduced by this feature? [Completeness, Spec §FR-007 / Plan §Constraints]
- [ ] CHK002 - Is the runner's maximum assignable status explicitly capped at `machine-pass`, with `verified` never auto-assigned by any automation? [Measurability, Spec §Assumptions (T148) / Plan §Constraints]

## Promotion Rules & Single Promotion Owner

- [ ] CHK003 - Is the requirement that the runner writes only `partial` or `machine-pass` (never `verified`) stated explicitly and unambiguously? [Clarity, Spec §FR-007 / Plan §Constraints]
- [ ] CHK004 - Is `scripts/lib/promotion.sh` designated as the single owner of all promotion writes, with no other writer permitted? [Completeness, Plan §"Already implemented" (promotion mirrors)]
- [ ] CHK005 - Is the rule that a clean machine pass never overwrites a human attestation (no `verified` → `machine-pass`) stated? [Consistency, Plan §Constraints]
- [ ] CHK006 - Is the condition that a `verified` block must be demoted to `partial` before any machine refresh explicitly specified? [Clarity, Research §Cross-cutting]

## T146 Gating (only after T131–T145 complete)

- [ ] CHK007 - Is T146 explicitly gated on completion of T131–T145, with the dependency recorded as a measurable precondition? [Completeness, Spec §Assumptions / Plan §Open items]
- [ ] CHK008 - Is the failure behavior specified when T131–T145 are not all complete (refuse promotion rather than partial promotion)? [Measurability, Gap]

## T148 Human E Attestation (human-only)

- [ ] CHK009 - Is the requirement that `verified` requires a human `E` attestation with `reviewer`, `reviewDate`, and `sessionId` fields explicitly stated? [Completeness, Spec §Assumptions (T148)]
- [ ] CHK010 - Is the prohibition against any automation assigning or fabricating the `E` honesty level explicitly stated? [Clarity, Plan §Constraints / Research §Cross-cutting]

## T138/T145 Blockers Documented

- [ ] CHK011 - Are T138 (second host) and T145 (farmhand path) explicitly documented as environment-blocked and out of scope for this feature? [Completeness, Spec §Assumptions (final)]
- [ ] CHK012 - Is the rule that farmhand/split-screen runs stay `partial` and are never promotable stated? [Clarity, Research §Cross-cutting / Plan §Constraints]

## No Claim Overreach (verified never auto-assigned)

- [ ] CHK013 - Is the rule that no claim may overstate evidence (e.g., "verified by build" or "verified by N passing tests") explicitly prohibited? [Measurability, Research §Unknown 5]

## Attestation Boundaries & Audit Trail

- [ ] CHK014 - Is the audit trail for promotion transitions (e.g., `promotion-triggers.json`) required to record every status change with provenance? [Completeness, Research §Unknown 5]
- [ ] CHK015 - Is the requirement that red-gate deviation is documented (not fabricated) with a traceable marker specified? [Coverage, Spec §FR-007 / Research §Unknown 5]

---

## Notes

- Items marked `[Gap]` indicate requirements-quality gaps where the spec does not currently define the criterion.
- Traceability references: ≥80% of items cite a spec/plan/research section. CHK008 is the sole `[Gap]` (no explicit failure-mode requirement for incomplete T131–T145).
- Based on the honesty ladder in `specs/010-headless-verification-runner/contracts/test-md-template.md` (C-REL-15) and `specs/010-headless-verification-runner/research.md` (R-10).