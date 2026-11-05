# Verification Integrity Checklist: Gate 3 Runner Hardening

**Purpose**: Validate that the requirements governing manifest schema validity, detached SHA-256 anchor enforcement, `subject[]` completeness, verifier rejection behavior, claims-before-manifest ordering, single-producer rule, retention-receipt coverage, and schema version bump necessity are complete, clear, consistent, measurable, and cover edge cases.
**Created**: 2026-11-05
**Feature**: [spec.md](../spec.md)

**Note**: This custom checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied. It does not mean implementation work is complete.

## Manifest Schema Validity (FR-003, FR-004)

- [ ] CHK001 Are the "claims/manifest honesty" and "runId agreement" checks (FR-004) defined with explicit fields compared and failure modes, or are they left implicit? [Gap][FR-004]
- [ ] CHK002 Does the spec state whether schema validation failure is fail-closed when `jsonschema` is missing, or is that only implied by plan/research? [Ambiguity][FR-004]
- [ ] CHK003 Are all rejection paths (missing manifest/anchor/claims, digest mismatch, schema failure, uncovered file, runId mismatch) required to return exit 19 with a named refusal site? [FR-004, FR-005]

## Detached SHA-256 Anchor Enforcement

- [ ] CHK004 Is the computation order of `manifest.sha256` (after manifest bytes finalized) stated as a hard requirement rather than an implementation note? [data-model.md §2]
- [ ] CHK005 Are the refusal-site message and exit code for a digest mismatch specified precisely enough to be tested? [contract:verifier-completeness.md §44]

## subject[] Completeness (FR-005, SC-001)

- [ ] CHK006 Is the SKIP set enumerated completely and authoritatively in the requirements, including the rule that widening it requires a contract update? [FR-005, contract:verifier-completeness.md §SKIP set]
- [ ] CHK007 Is the requirement that every regular file (including hidden/dot files other than the two named work files) must be covered stated unambiguously? [Ambiguity][FR-005]
- [ ] CHK008 Are empty directories explicitly addressed as not requiring coverage? [contract:verifier-completeness.md §Edge cases]
- [ ] CHK009 Is SC-001 (uncovered file → exit 19) measurable and testable with a concrete fixture described in requirements? [SC-001]

## Verifier Rejection Behavior (exit 19 on violation)

- [ ] CHK010 Is the distinction between exit 19 (ERRORED) and exit 21 (claim overreach) defined with a decision rule a reviewer can apply? [contract:verifier-completeness.md §6]

## Claims-Before-Manifest Ordering (FR-006)

- [ ] CHK011 Is the ordering requirement ("claims.json written before manifest input assembly") stated as a MUST with a verifiable condition? [FR-006]
- [ ] CHK012 Are the consequences of violating the ordering (uncovered claims.json escaping the anchor) specified as a verifier-detectable failure? [Gap][FR-006]

## Single-Producer Rule (ManifestBuilder only)

- [ ] CHK013 Is the single-producer rule for `manifest.json` and `manifest.sha256` stated as a requirement with enforcement expectation? [FR-003, data-model.md §2]

## Retention-Receipt Coverage Gap

- [ ] CHK014 Is the chosen approach (move retention-receipt generation before manifest input assembly) recorded as a requirement with explicit ordering? [Gap][research.md §Unknown 4]
- [ ] CHK015 Is it specified that `retention_export()` may remain post-finalization because exports live outside the bundle? [Assumption][data-model.md §Retention Receipt Coverage]

## Schema Version Bump Necessity

- [ ] CHK016 Is the decision that no schema version bump is required for subject completeness justified, with the trigger for a future bump stated? [FR-005, data-model.md §1]

## Notes

- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- Leave items unchecked when they still require clarification, correction, or reviewer evaluation.
- Items are numbered sequentially for easy reference.
