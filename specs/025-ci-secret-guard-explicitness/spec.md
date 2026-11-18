# Feature Specification: Explicit secret-dependent CI failures

**Feature Branch**: `feature/025-ci-secret-guard-explicitness`

**Created**: 2026-11-18

**Status**: Draft

**Input**: Decision go (`.specify/assessments/025-ci-secret-guard-explicitness/decision.md`) — add a fail-fast diagnostic to the build job that depends on the private build credential, enforce the invariant with a deterministic check, and document the CI trust boundary.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Diagnose a red check without reading raw logs (Priority: P1)

A merge operator opens a pull request whose build check is red. The check output states immediately that the private build credential is unavailable, rather than failing deep inside an unrelated command. The operator can decide to re-run with credentials or to investigate a code defect without guessing.

**Why this priority**: It is the direct cause of the ambiguity recorded in the assessment; without it the other stories are cosmetic.

**Independent Test**: Inspect the check output of the build job when the credential is absent; the explicit credential-missing message must appear before any download or build command.

**Acceptance Scenarios**:

1. **Given** a CI run where the build credential is absent, **When** the build job starts, **Then** the check output contains an explicit error naming the missing credential and the job fails without attempting the private download.
2. **Given** a CI run where the credential is present, **When** the build job starts, **Then** no credential-missing message is emitted and the job proceeds to its existing steps unchanged.

---

### User Story 2 - Keep the guard from rotting (Priority: P2)

A future workflow job is added that also depends on the private credential. A deterministic local check fails until that job carries the same fail-fast diagnostic, so the guarantee does not depend on someone remembering the convention.

**Why this priority**: The current gap exists because two of three consumers had the guard and one did not; an enforced invariant closes that class of drift.

**Independent Test**: Run the deterministic check against the current workflow set (passes), then against a fixture containing an unguarded credential-consuming step (fails).

**Acceptance Scenarios**:

1. **Given** the current workflow set, **When** the deterministic check runs, **Then** it reports success.
2. **Given** a workflow step that consumes the private credential without a fail-fast guard, **When** the deterministic check runs, **Then** it fails and names the offending file or step.
3. **Given** the documented best-effort verification job whose missing-credential path is intentionally a warning, **When** the deterministic check runs, **Then** that job is excluded from the guard requirement.

---

### User Story 3 - Know what a green CI run actually proves (Priority: P3)

A maintainer reads the local quality-gate guide and can state which hosted check is authoritative, which jobs require a private credential, why a dependency-bump run can be red for reasons unrelated to source code, and which claims remain unverified in this session.

**Why this priority**: Prevents over-trusting a green required check that cannot see build or test results, and preserves honest boundaries.

**Independent Test**: Read the local gate guide and locate a section that names the local authoritative gate, the hosted required check, the credential-dependent jobs, and the unconfirmed remote claims.

**Acceptance Scenarios**:

1. **Given** the local gate guide, **When** a maintainer looks for the CI trust boundary, **Then** a dedicated section states local versus hosted authority and marks unverified remote claims explicitly.
2. **Given** the same section, **When** it describes the best-effort verification path, **Then** it states that path is intentionally non-blocking and must not be converted into a required check.

---

### Edge Cases

- Credential is set to an empty string rather than unset: must be treated as missing.
- A credential-consuming job is intentionally best-effort: must remain a warning and be excluded from the fail-fast requirement, not silently upgraded to an error.
- Another session adds a new credential-consuming workflow before this change lands: the deterministic check must be re-run against the real workflow set, and the new consumer resolved rather than ignored.
- The private download succeeds but later steps fail: the new message must not appear, keeping the diagnostic specific to credential absence.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The build job that consumes the private credential MUST emit an explicit error naming the missing credential before any private download or build step.
- **FR-002**: When the credential is unset or empty, that job MUST fail immediately and MUST NOT be reported as skipped or successful.
- **FR-003**: When the credential is present, that job MUST proceed through its pre-existing steps without changed behavior.
- **FR-004**: The fail-fast message MUST match the wording already used by the two guarded jobs, so diagnostics are uniform.
- **FR-005**: A deterministic local check MUST enumerate all workflow steps that consume the private credential and fail if a non-best-effort consumer lacks the guard.
- **FR-006**: The deterministic check MUST exclude the documented best-effort verification job, whose missing-credential path is intentionally a warning.
- **FR-007**: The local quality-gate guide MUST document the local authoritative gate, the hosted required check, the credential-dependent jobs, and the boundary of what hosted CI proves.
- **FR-008**: Unverified remote claims MUST be marked explicitly in that documentation; no remote status may be asserted as verified without a live check.

### Key Entities

- **Credential-dependent step**: a CI step that requires the private build credential to acquire game assemblies; attributes: declaring file, job name, guard present (yes/no), best-effort (yes/no). In scope at decision time: `build-test` (unguarded), `code-style` (guarded), `quality-gate/build-analysis` (guarded). `verify-scenario` is best-effort and excluded; `release` is tag-triggered and owner-gated, therefore excluded.
- **Local authoritative gate**: the repository's deterministic pre-release gate (`scripts/release-gate.sh`) with seven steps; distinct from hosted checks.
- **Hosted required check**: the single branch-protection check that gates merges to the integration branch; currently blind to build and test results.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of non-best-effort credential-dependent steps (baseline: 2 of 3) emit the explicit missing-credential error before first use.
- **SC-002**: The deterministic check passes on the real workflow set and fails on a fixture with an unguarded consumer — two observed outcomes, no inference.
- **SC-003**: The local gate guide contains a dedicated trust-boundary section that names the authoritative local gate, the hosted required check, and at least one explicitly unverified claim.
- **SC-004**: The repository's existing deterministic quality gates remain green after the change, with no new skipped checks and no converted best-effort path.

## Assumptions

- The private credential is repository-scoped and unavailable to forked or automated dependency-update actors; the exact root cause of currently red update PRs is unverified in this session and is not a precondition for explicit diagnostics.
- The best-effort verification job stays non-blocking by design and is excluded from the guard requirement.
- Tag-triggered release automation is owner-controlled and out of scope; no release configuration changes in this feature.
- The deterministic check reuses the YAML parsing facility already present in the quality-gate test suite rather than introducing a new dependency.
- No gameplay, save-data or locale behavior is affected; no player-facing strings are added.
