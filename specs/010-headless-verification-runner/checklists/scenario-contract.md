# Scenario Contract & Schema Requirements Quality Checklist: Headless Game Verification Runner

**Purpose**: Validate that scenario-definition requirements (fields, thresholds, steps, injection, acceptance criteria, edge cases, schema evolution, reusability, readability) are complete, clear, measurable, consistent, and covered.
**Created**: 2026-10-26
**Feature**: [spec.md](../spec.md)

**Review Ownership**: This checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied.

**Note**: Items test whether the *requirements themselves* are well-written — not whether the implementation works.

---

## Field Completeness & Required vs. Optional

- [x] CHK001 - Is the required field set explicitly declared in the schema's `required` arrays at every object level, and mirrored in `data-model.md`? [Completeness, Spec §FR-007 / C-SCN-36]
- [x] CHK002 - For each optional field, is either a default applied on omission or explicit absence semantics documented? [Clarity, Spec §FR-007 / C-SCN-01]
- [x] CHK003 - Are `required` and `type` specified independently so null cannot pass a required check? [Measurability, Gap]
- [x] CHK004 - Does the schema reject unknown properties (`additionalProperties: false`) so misspelled fields fail loudly? [Consistency, Spec §FR-007]
- [x] CHK005 - Is each field's `description` sufficient for a human author to decide inclusion? [Clarity, Spec §FR-007]
- [x] CHK006 - Are IEEE 829 Test Case Specification minimum fields (identifier, test items, inputs, outputs, environment, procedural requirements, intercase dependencies) all mapped to schema properties? [Coverage, Gap]
- [x] CHK007 - Are conditional requirements (fields required only in certain contexts) documented with `if/then/else` or `dependentRequired`? [Completeness, Gap]

## Threshold Definitions (Boundaries, Multipliers, Sentinels)

- [x] CHK008 - Is boundary inclusivity explicitly encoded (`minimum`/`maximum` vs. `exclusiveMinimum`/`exclusiveMaximum`), not just prose? [Clarity, Spec §FR-010]
- [x] CHK009 - Is every multiplier a typed number with min/max and its base (what it multiplies) documented? [Measurability, Spec §FR-010]
- [x] CHK010 - Are sentinels (99999, -1, NaN, missing key) defined as named constants and enumerated in `enum`? [Consistency, Spec §FR-010]
- [x] CHK011 - Is the distinction between "missing key" (non-participant) and "health <= 0" (non-participant) vs. "Poor" (positive < 34) unambiguous? [Clarity, Spec §FR-010]
- [x] CHK012 - Are NaN-tile handling requirements specified (skip, no change) with the JSON representation documented? [Edge Case, Spec §FR-010]
- [x] CHK013 - Could two independent readers compute the same pass/fail result from the threshold definition alone? [Measurability, Spec §FR-010]
- [x] CHK014 - Are boundary-value analysis cases (min-1, min, min+1, max-1, max, max+1, sentinel) required in the test suite? [Coverage, Gap]

## Step Determinism & Reproducibility

- [x] CHK015 - Is every step input an explicit field (no hidden global state, time, or RNG seed)? [Completeness, Spec §FR-006]
- [x] CHK016 - Are step order and inter-step dependencies explicitly declared (ordered array is authoritative; `dependsOn` intentionally absent)? [Clarity, Spec §FR-006 / C-SCN-21]
- [x] CHK017 - Are expected outputs exact values or ranged values with explicit tolerance (0.0001f)? [Measurability, Spec §FR-009]
- [x] CHK018 - Is the execution environment (game/SMAPI/mod versions, save ID, config) captured in scenario metadata? [Completeness, Spec §FR-002]
- [x] CHK019 - Is idempotency required (re-running yields same result) with non-determinism explicitly declared? [Consistency, Spec §FR-006]
- [x] CHK020 - Are scenarios limited to a manageable maximum (schema `maxItems: 5`; 3–5 recommended) with decomposition guidance for longer ones? [Clarity, Gap]

## Injection Mechanism Contracts

- [x] CHK021 - Is every dependency an explicit injection point with a type (not a hidden global)? [Completeness, Spec §FR-006]
- [x] CHK022 - Does the scenario declare which test-double kind is used (stub/mock/fake/spy) and its behavioral contract? — **N/A**: the runner uses the real game and has no test doubles (C-SCN-23). [Clarity, Gap]
- [x] CHK023 - Is the injection lifecycle (per-scenario vs. shared) and teardown explicitly specified? [Coverage, Gap]
- [x] CHK024 - Is the injection interface versioned with breaking changes flagged? [Consistency, Spec §FR-009 (ILivingRootsModApi)]
- [x] CHK025 - Can injection be overridden without modifying the scenario definition (for different environments)? [Flexibility, Gap]

## Acceptance Criteria Measurability

- [x] CHK026 - Is each acceptance criterion evaluable as pass/fail by machine or human with no judgment call? [Measurability, Spec §SC-001..014]
- [x] CHK027 - Do acceptance criteria avoid implementation-specific language? [Clarity, Spec §US1..US3]
- [x] CHK028 - Are non-functional criteria (5 ms, 14-day window) stated with numeric bounds and units? [Measurability, Spec §FR-013]
- [x] CHK029 - Are negative/failure scenarios (crash, incomplete signals, version mismatch) included alongside positive ones? [Coverage, Spec §Edge Cases]
- [x] CHK030 - Is there a traceability link from criterion → requirement → test case? [Traceability, Spec §SC-001..014]

## Edge Cases (Missing Data, NaN, Sentinels)

- [x] CHK031 - For each field, is absence vs. null vs. both documented with distinct meanings? [Clarity, Spec §FR-010]
- [x] CHK032 - If NaN is used, is its JSON representation documented and validated? [Edge Case, Spec §FR-010]
- [x] CHK033 - Is there a sentinel registry with every sentinel in `enum`? [Consistency, Gap]
- [x] CHK034 - Does the test suite cover at least one value from each equivalence class (valid + invalid)? [Coverage, Gap]
- [x] CHK035 - For sparse/default-omitted data (no zero health values), is the default documented and applied on read? [Completeness, Spec §FR-010]

## Schema Evolution & Versioning

- [x] CHK036 - Does every schema declare `$schema` (dialect) and a versioned `$id`? [Completeness, Spec §FR-007]
- [x] CHK037 - Is the version bump type justified by the actual change (integer major only; SemVer is N/A for this schema contract)? [Clarity, Gap]
- [x] CHK038 - Is a compatibility mode (backward/forward/full/transitive) declared and enforced? [Consistency, Gap]
- [x] CHK039 - Are published schema versions immutable (new version = new file/URI)? [Measurability, Spec §FR-007]
- [x] CHK040 - Is there a documented migration path for breaking changes? [Coverage, Gap]

## Cross-Feature Reusability

- [x] CHK041 - Are common sub-schemas extracted into `$defs` and referenced via `$ref`? [Consistency, Spec §FR-007]
- [x] CHK042 - Is scenario data parameterized (outline/examples/matrix) rather than copy-pasted? — **N/A for Phase 1** under the Rule of Three: each feature owns one scenario, so copy-paste duplication has not been observed; revisit when ≥3 features share a parameterized shape (C-SCN-08). [Completeness, Spec §FR-007]
- [x] CHK043 - Are shared preconditions factored into a Background/common block? [Clarity, Gap]
- [x] CHK044 - Can the same contract be independently validated by at least two implementations (Phase 1: two independent validators; a second runtime is deferred per C-SCN-38)? [Measurability, Spec §FR-022]
- [x] CHK045 - Is there a keyword/step library that scenarios reference rather than inline? — **N/A for Phase 1**: composite keyword/step libraries are excluded from schemaVersion 2 and deferred until ≥3 features reuse a composite sequence (C-SCN-32). [Coverage, Gap]

## Human Readability vs. Machine Validity

- [x] CHK046 - Can a domain expert (non-developer) read the scenario and understand the intent? [Clarity, Spec §FR-007]
- [x] CHK047 - Does every prose description have a corresponding machine-evaluable constraint? [Measurability, Spec §FR-007]
- [x] CHK048 - Are `title`, `description`, and `examples` present for every field? [Completeness, Spec §FR-007]
- [x] CHK049 - Is the scenario short (≤5 steps) with decomposition guidance for longer ones? [Clarity, Gap]
- [x] CHK050 - Would removing all human-readable text leave a fully validatable schema? [Measurability, Spec §FR-007]

## Cross-Cutting Requirement-Quality (ISO 29148 / NASA)

- [x] CHK051 - Are requirements necessary (would removing any field/step leave a gap)? [Completeness, Spec §FR-007]
- [x] CHK052 - Are requirements unambiguous (single interpretation)? [Clarity, Spec §FR-007]
- [x] CHK053 - Are requirements consistent (no conflicts between sections)? [Consistency, Spec §FR-007..FR-023]
- [x] CHK054 - Are requirements verifiable/testable (pass/fail procedure exists)? [Measurability, Spec §SC-001..014]
- [x] CHK055 - Are requirements traceable (link to requirement ID and test case ID)? [Traceability, Spec §SC-001..014]

## Notes

- Audit 2026-10-27: 41 PASS / 11 FAIL / 3 N/A. Reopened CHK011, CHK013, CHK020, CHK026, CHK034, CHK044, CHK047, CHK050, CHK051, CHK052, CHK053; all re-checked PASS after fixes.

- Items marked `[Gap]` indicate requirements-quality gaps where the spec does not currently define the criterion.
- Based on JSON Schema spec (draft 2020-12), ISO/IEC/IEEE 29119, IEEE 829, ISO/IEC/IEEE 29148, TestML, Cucumber/Gherkin, US 9,208,064, NASA SWEHB.