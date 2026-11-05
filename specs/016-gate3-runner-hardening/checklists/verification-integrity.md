# Verification Integrity Checklist: Gate 3 Runner Hardening

**Purpose**: Validate that the requirements governing manifest schema validity, detached SHA-256 anchor enforcement, `subject[]` completeness, verifier rejection behavior, claims-before-manifest ordering, single-producer rule, retention-receipt coverage, and schema version bump necessity are complete, clear, consistent, measurable, and cover edge cases.
**Created**: 2026-11-05
**Feature**: [spec.md](../spec.md)

**Note**: This custom checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied. It does not mean implementation work is complete.

## Manifest Schema Validity (FR-003, FR-004)

- [x] CHK001 Are the "claims/manifest honesty" and "runId agreement" checks (FR-004) defined with explicit fields compared and failure modes, or are they left implicit? [Gap][FR-004] — Satisfied: data-model.md §1 (Evidence Bundle) enumerates runId match, honestyLevel match, and claimsFile = "claims.json"; contract §6 gives failure modes (exit 19, or 21 for claim overreach).
- [ ] CHK002 Does the spec state whether schema validation failure is fail-closed when `jsonschema` is missing, or is that only implied by plan/research? [Ambiguity][FR-004] — Note: fail-closed-on-missing-validator is stated only in research.md §Unknown 3; spec FR-004 and the contract do not state it. Promote to FR-004/contract.
- [ ] CHK003 Are all rejection paths (missing manifest/anchor/claims, digest mismatch, schema failure, uncovered file, runId mismatch) required to return exit 19 with a named refusal site? [FR-004, FR-005] — Note: exit 19 is required for all paths, but only the uncovered-file refusal site is exemplified in contract §44; digest/missing/schema/runId refusal-site message formats are unspecified.
- [x] CHK004 Is the computation order of `manifest.sha256` (after manifest bytes finalized) stated as a hard requirement rather than an implementation note? [data-model.md §2] — Satisfied: data-model.md §2 "Producer invariants" states the anchor is computed over finalized bytes written after close and that a producer writing it first is invalid.

## Detached SHA-256 Anchor Enforcement

- [ ] CHK005 Are the refusal-site message and exit code for a digest mismatch specified precisely enough to be tested? [contract:verifier-completeness.md §44] — Note: contract §44 exemplifies only the uncovered-file message; no digest-mismatch message format is given.

## subject[] Completeness (FR-005, SC-001)

- [x] CHK006 Is the SKIP set enumerated completely and authoritatively in the requirements, including the rule that widening it requires a contract update? [FR-005, contract:verifier-completeness.md §SKIP set] — Satisfied: contract §SKIP set lists the four exempt paths, states "No other path is exempt," and edge case requires a contract update to widen.
- [ ] CHK007 Is the requirement that every regular file (including hidden/dot files other than the two named work files) must be covered stated unambiguously? [Ambiguity][FR-005] — Note: data-model.md §SKIP set table marks "Any other dot-prefixed file" as SKIP, conflicting with contract §Edge cases which requires all dotfiles except the two named work files to be covered. Reconcile the two.
- [x] CHK008 Are empty directories explicitly addressed as not requiring coverage? [contract:verifier-completeness.md §Edge cases] — Satisfied: contract §Edge cases states empty directories do not count as files and require no coverage.
- [ ] CHK009 Is SC-001 (uncovered file → exit 19) measurable and testable with a concrete fixture described in requirements? [SC-001] — Note: SC-001 and US2 give a Given/When/Then but no concrete fixture file name/content; test author must invent the added file.

## Verifier Rejection Behavior (exit 19 on violation)

- [ ] CHK010 Is the distinction between exit 19 (ERRORED) and exit 21 (claim overreach) defined with a decision rule a reviewer can apply? [contract:verifier-completeness.md §6] — Note: contract §6 assigns 21 to "claim overreach" but does not define what constitutes overreach vs. ordinary mismatch.

## Claims-Before-Manifest Ordering (FR-006)

- [x] CHK011 Is the ordering requirement ("claims.json written before manifest input assembly") stated as a MUST with a verifiable condition? [FR-006] — Satisfied: FR-006 states the MUST and the verifiable condition (subject[] covers claims.json).
- [ ] CHK012 Are the consequences of violating the ordering (uncovered claims.json escaping the anchor) specified as a verifier-detectable failure? [Gap][FR-006] — Note: detectability follows implicitly from the completeness rule, but no artifact explicitly ties an ordering violation to a verifier failure.

## Single-Producer Rule (ManifestBuilder only)

- [x] CHK013 Is the single-producer rule for `manifest.json` and `manifest.sha256` stated as a requirement with enforcement expectation? [FR-003, data-model.md §2] — Satisfied: FR-003 requires production via the single producer ManifestBuilder; data-model §2 states ManifestBuilder is the only component allowed to create/modify both.

## Retention-Receipt Coverage Gap

- [ ] CHK014 Is the chosen approach (move retention-receipt generation before manifest input assembly) recorded as a requirement with explicit ordering? [Gap][research.md §Unknown 4] — Note: recorded as a chosen approach in data-model/research, but not as a MUST in spec FRs; ordering is not a stated requirement.
- [x] CHK015 Is it specified that `retention_export()` may remain post-finalization because exports live outside the bundle? [Assumption][data-model.md §Retention Receipt Coverage] — Satisfied: data-model §Retention Receipt Coverage explicitly permits post-finalization export.

## Schema Version Bump Necessity

- [x] CHK016 Is the decision that no schema version bump is required for subject completeness justified, with the trigger for a future bump stated? [FR-005, data-model.md §1] — Satisfied: justification (filesystem rule, unchanged field set) and trigger (field-set change) are stated in research.md §Unknown 4 and data-model §1.

## Notes

- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- Leave items unchecked when they still require clarification, correction, or reviewer evaluation.
- Items are numbered sequentially for easy reference.