# Feature Specification: Gate 3 Runner Hardening

**Feature Branch**: `016-gate3-runner-hardening`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "runner-repair/quality work that actually unblocked Gate 3 (save-load alignment, full SMAPI log capture, manifest schema enforcement)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Regression guards for the three defects that caused the misdiagnosis (Priority: P1)

As a maintainer of Living Roots, I want the three defects that blocked Gate 3 (save-name mismatch, two-line SMAPI excerpt, schema-invalid manifest) to be impossible to reintroduce silently, so that a future change cannot quietly bring back the multi-day diagnosis cycle.

**Why this priority**: The repair already unblocked `machine-pass` (achieved 2026-11-05). Without regression guards, the same defect class can return unnoticed and devalue every future attestation.

**Independent Test**: Can be fully tested by reverting each fix in isolation and confirming the corresponding test fails, then restoring the fix and confirming it passes.

**Acceptance Scenarios**:

1. **Given** a milestone save whose folder name and inner save file name disagree, **When** `save_prepare` runs, **Then** it either aligns the names or fails loudly with a named cause (never silently returns to title).
2. **Given** a bundle whose `artifacts/smapi-log.txt` is a filtered excerpt rather than the full log, **When** the evidence verifier runs, **Then** the run is rejected or flagged.
3. **Given** a bundle whose `manifest.json` is not schema-valid, **When** the offline verifier runs, **Then** it returns 19 with the exact refusal site.

---

### User Story 2 - Complete subject coverage in the evidence bundle (Priority: P2)

As a maintainer, I want every file in an evidence bundle to be covered by the manifest's `subject[]` (not just the artifacts), so that a file added after finalization cannot escape the detached SHA-256 anchor.

**Why this priority**: The current verifier only checks `artifacts ⊆ subject`, leaving `retention-receipt.json` and any post-finalization file uncovered. Closing this gap makes the anchor tamper-evident for the whole bundle.

**Independent Test**: Can be tested by adding an extra file to a valid bundle and confirming the verifier rejects it.

**Acceptance Scenarios**:

1. **Given** a valid bundle, **When** an additional file is added that is not listed in `subject[]`, **Then** the verifier returns 19.
2. **Given** a bundle where `subject[]` covers every file, **When** the verifier runs, **Then** it returns 0.

---

### User Story 3 - Genuine red-gate evidence (Priority: P3)

As a reviewer, I want the red-gate logs to show the intended failing assertion rather than a post-implementation green run, so that the Phase 6 attestation is honest.

**Why this priority**: T130 is currently open because the recorded logs are green post-implementation runs, not true RED gates. This undermines the Phase 6 attestation.

**Independent Test**: Can be tested by confirming each red-gate log contains a failing assertion line.

**Acceptance Scenarios**:

1. **Given** a red-gate log, **When** inspected, **Then** it contains at least one `not ok` or `FAIL` line representing the intended failing assertion.

---

### Edge Cases

- What happens when a new file type is added to the bundle layout? The verifier must reject unless `subject[]` is updated.
- How does the system handle a bundle where `retention-receipt.json` was written after finalization? The verifier must detect the uncovered file.
- What happens when the SMAPI log grows beyond a reasonable size? Capture level should be documented, not silently truncated.

## Clarifications

### Session 2026-11-05

- Q: Should the SMAPI log capture be full or configurable? → A: Full capture is the default; any excerpting is an explicit, documented, opt-in level (never a silent filter). Size caps via density ranking apply only to LLM diagnosis, with the full log retained as a forensic secondary artifact.
- Q: Should `subject[]` cover every bundle file or only artifacts? → A: Every bundle file must be covered. The verifier enforces this as a filesystem rule (no schema version bump required); a schema bump is only needed if the field set changes.
- Q: Should red-gate evidence be reconstructed or documented as a deviation? → A: Document the deviation. True RED gates cannot be reconstructed because tests were written alongside/after implementation; the tests are regression guards, not TDD RED evidence. Marking unverified (not fabricating) follows the tdd-guardian "absence → unverified, never violation" policy.
- Q: Should the SKIP set include all dotfiles or only named work files? → A: Only the named work files (`manifest.json`, `manifest.sha256`, `.manifest-input.json`, `.attempts_list`). The verifier code is authoritative; data-model and contract must match.
- Q: What is the failure behavior when T131-T145 are not all complete? → A: Refuse promotion (not partial promotion). `RUN_EXIT` must be 0; all gates must pass before `promotion_apply` is invoked.
- Q: Should `retention-receipt.json` be covered by `subject[]`? → A: Yes. Move receipt generation before manifest input assembly so the filesystem walk covers it.
- Q: What is the opt-in excerpt selection mechanism? → A: Environment variable `RUNNER_SMAPI_CAPTURE=full|excerpt` (default `full`). Matches the existing `RUNNER_*` env-var convention. No schema change required.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST align the isolated save's folder name and inner save file name during `save_prepare`, or fail loudly with a named cause.
- **FR-002**: The system MUST capture the full SMAPI log in `artifacts/smapi-log.txt` as the default. Any excerpting must be an explicit, documented, opt-in capture level controlled via the `RUNNER_SMAPI_CAPTURE` environment variable (`full` | `excerpt`, default `full`), never a silent filter. Size caps via density ranking apply only to LLM diagnosis, with the full log retained as a forensic secondary artifact.
- **FR-003**: The system MUST produce a manifest that satisfies `contracts/evidence-bundle.schema.json` via the single producer `ManifestBuilder`, with a detached `manifest.sha256` anchor.
- **FR-004**: The offline verifier MUST validate the manifest schema, digest, subject entries, claims/manifest honesty, runId agreement, and subject-completeness (every bundle file covered) before returning 0.
- **FR-005**: The offline verifier MUST reject any bundle where a file exists that is not covered by `subject[]`. The SKIP set (files excluded from `subject[]`) is: `manifest.json`, `manifest.sha256`, `.manifest-input.json`, `.attempts_list`. This is enforced as a filesystem rule; no schema version bump is required unless the field set changes.
- **FR-006**: The system MUST write `claims.json` before assembling the manifest input, so `subject[]` covers it.
- **FR-007**: The system MUST document in each red-gate log that implementation pre-existed and that genuine RED evidence cannot be reconstructed retroactively; true RED gates are not fabricated. The tests are treated as regression guards, not TDD RED evidence. SC-002 is satisfied when each log contains either a failing assertion or a documented deviation statement.
- **FR-008**: The system MUST preserve the original `HOME` for Python helpers that import user-installed modules when the save isolation rewrites it.
- **FR-009**: The system MUST ensure `retention-receipt.json` is covered by `subject[]`, by moving receipt generation before manifest input assembly (or an equivalent ordering that guarantees coverage).
- **FR-010**: When T131-T145 are not all complete, the system MUST refuse promotion (not partial promotion). `RUN_EXIT` must be 0 and all required gates must pass before `promotion_apply` is invoked.

### Key Entities

- **Evidence bundle**: the run directory containing `manifest.json`, `claims.json`, `artifacts/`, `attempts/`, and CI reports.
- **Manifest**: the schema-validated, single-producer artifact with a detached SHA-256 anchor.
- **Subject entry**: a `{name, digest}` pair covering a bundle file.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A bundle with an uncovered file is rejected by the verifier with exit 19.
- **SC-002**: Every red-gate log contains either a failing assertion or a documented deviation statement (implementation pre-existed; genuine RED evidence cannot be reconstructed).
- **SC-003**: The save-name alignment is covered by an automated test that fails when the fix is reverted.
- **SC-004**: The full SMAPI log capture is covered by an automated test that fails when the excerpt is reintroduced.

## Assumptions

- The save-name mismatch is the root cause of the load failure (confirmed by the successful 2026-11-05 run).
- The three-pattern excerpt is the only filtering; widening to the full log does not break threshold checks.
- `subject[]` completeness may require a schema version bump; to be decided during planning.
- Red-gate evidence is documented as a deviation, not reconstructed (decision accepted 2026-11-05).
- T138 (second host) and T145 (farmhand path) remain environment-blocked and are out of scope for this spec.