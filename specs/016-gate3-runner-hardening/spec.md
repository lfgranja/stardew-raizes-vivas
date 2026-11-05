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

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST align the isolated save's folder name and inner save file name during `save_prepare`, or fail loudly with a named cause.
- **FR-002**: The system MUST capture the full SMAPI log in `artifacts/smapi-log.txt` (or a documented, configurable excerpt level), never a silently filtered subset.
- **FR-003**: The system MUST produce a manifest that satisfies `contracts/evidence-bundle.schema.json` via the single producer `ManifestBuilder`, with a detached `manifest.sha256` anchor.
- **FR-004**: The offline verifier MUST validate the manifest schema, digest, subject entries, claims/manifest honesty, and runId agreement before returning 0.
- **FR-005**: The offline verifier MUST reject any bundle where a file exists that is not covered by `subject[]`.
- **FR-006**: The system MUST write `claims.json` before assembling the manifest input, so `subject[]` covers it.
- **FR-007**: The system MUST record genuine red-gate evidence (failing assertions) for T130, or document why it cannot be reproduced.
- **FR-008**: The system MUST preserve the original `HOME` for Python helpers that import user-installed modules when the save isolation rewrites it.

### Key Entities

- **Evidence bundle**: the run directory containing `manifest.json`, `claims.json`, `artifacts/`, `attempts/`, and CI reports.
- **Manifest**: the schema-validated, single-producer artifact with a detached SHA-256 anchor.
- **Subject entry**: a `{name, digest}` pair covering a bundle file.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A bundle with an uncovered file is rejected by the verifier with exit 19.
- **SC-002**: Every red-gate log contains at least one failing assertion.
- **SC-003**: The save-name alignment is covered by an automated test that fails when the fix is reverted.
- **SC-004**: The full SMAPI log capture is covered by an automated test that fails when the excerpt is reintroduced.

## Assumptions

- The save-name mismatch is the root cause of the load failure (confirmed by the successful 2026-11-05 run).
- The three-pattern excerpt is the only filtering; widening to the full log does not break threshold checks.
- `subject[]` completeness may require a schema version bump; to be decided during planning.
- T138 (second host) and T145 (farmhand path) remain environment-blocked and are out of scope for this spec.