# Feature Specification: Local OSS Quality Gate (replacing SonarCloud assessments)

**Feature Branch**: `011-local-oss-quality-gate`

**Created**: 2026-10-29

**Status**: Final — clarified and design-corrected 2026-10-29

**Input**: User description: "Replace the SonarQube/SonarCloud code-quality assessments with a lightweight, local, native, open-source, and free quality gate for the stardew-raizes-vivas .NET 6 mod, so that reliability/security/maintainability/duplication/coverage are enforced deterministically on a standard CI runner without a cloud SaaS or a game install."

## Clarifications

### Session 2026-10-29

- Q: Is a trend dashboard and inline PR decoration required, or is build-fail with file/line diagnostics sufficient? → A: Build-fail sufficient; no trend dashboard or inline PR decoration required.
- Q: Should SonarCloud be removed entirely after validation, or kept in parallel indefinitely? → A: Remove SonarCloud after 2–4 weeks of green local runs; do not keep it in parallel indefinitely.
- Q: Should CodeQL run per-PR or on a schedule? → A: Semgrep per-PR (fast gate); CodeQL scheduled (nightly/weekly) or on merge to main (deep scan).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic reliability gate on every build (Priority: P1)

As the maintainer, I want null references, unhandled exceptions, and other reliability defects to fail the build with a file and line number, so that I stop relying on an opaque SonarCloud reliability rating that blocks merges without telling me what to fix.

**Why this priority**: This is the core pain — the SonarCloud "C Reliability Rating on New Code" repeatedly blocks merges (run 123455233388) and provides no actionable location. Converting these defects into compile errors is the highest-value, lowest-risk step.

**Independent Test**: Enable the analyzers on both projects, run `dotnet build`, and confirm that any reliability defect produces a build error naming the file and line; a clean build passes.

**Acceptance Scenarios**:

1. **Given** a project configured with the reliability analyzers and `TreatWarningsAsErrors`, **When** the code contains a null dereference or unhandled exception risk, **Then** `dotnet build` fails and reports the offending file and line.
2. **Given** the same configuration, **When** the code is clean of such defects, **Then** `dotnet build` succeeds.
3. **Given** a pull request introducing a reliability defect, **When** CI runs the build, **Then** the PR check fails before merge.

### User Story 2 - Security and dependency scanning on every PR (Priority: P2)

As the maintainer, I want automated SAST and dependency-vulnerability scanning on every pull request, so that security issues are caught locally and offline-capable rather than only through a cloud service.

**Why this priority**: Security is the second Sonar dimension; the chosen stack provides fast (Semgrep) and deep (CodeQL) coverage for a public repo at no cost.

**Independent Test**: Open a PR with a known vulnerable pattern or outdated dependency and confirm the security scan fails the check; a clean PR passes.

**Acceptance Scenarios**:

1. **Given** a PR introducing a security anti-pattern, **When** the security scan runs, **Then** the PR check fails with the finding.
2. **Given** a PR with a vulnerable dependency, **When** dependency scanning runs, **Then** the PR check fails.
3. **Given** a clean PR, **When** all security scans run, **Then** the checks pass.

### User Story 3 - Coverage and duplication thresholds (Priority: P3)

As the maintainer, I want code-coverage and duplication thresholds enforced in CI, so that test quality and code reuse do not silently regress.

**Why this priority**: Completes the Sonar dimension coverage (coverage + duplication) and prevents silent regression; lower priority because it depends on baseline measurement.

**Independent Test**: Run the coverage and duplication tools on the current codebase, record the baseline, set thresholds at the baseline, and confirm a regression fails the check while the baseline passes.

**Acceptance Scenarios**:

1. **Given** a baseline coverage measurement, **When** a change drops coverage below the threshold, **Then** the CI check fails.
2. **Given** a baseline duplication measurement, **When** a change raises duplication above the threshold, **Then** the CI check fails.
3. **Given** thresholds set at the current baseline, **When** the baseline code runs, **Then** all checks pass.

### Edge Cases

- What happens when the analyzer surfaces a large number of pre-existing warnings? (Phase 1 must measure and decide whether to fix or suppress with justification.)
- How does the system handle a false positive from an analyzer? (Allow targeted suppression with a documented reason; do not blanket-disable rules.)
- What happens when a security scan (CodeQL) exceeds acceptable CI time? (Run CodeQL as a scheduled/deeper scan; keep Semgrep as the fast per-PR gate.)
- What happens when coverage cannot be measured because tests fail to build? (The build/coverage job must fail and report the build error first.)
- How is a threshold updated when code quality improves? (Ratchet: threshold only moves upward, never downward, without explicit justification.)

## Non-Goals

- A trend dashboard, historical charts, or inline PR comment decoration.
- Removing SonarCloud in this feature; removal is a separate follow-up after the 2–4 week parallel-run window.
- Mutation testing (Stryker.NET).
- New-code-only scoping for Semgrep or Trivy; the gate scans the whole repository scope and records the parity gap.
- Runtime/gameplay, domain, or persistence changes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST enable reliability and code-quality analyzers (including Sonar's own rules via the Sonar analyzer package and the .NET SDK analyzers) on both the mod project and the test project, so that defects are reported at build time.
- **FR-002**: The system MUST treat analyzer warnings as build errors (`TreatWarningsAsErrors`), so that a reliability defect fails `dotnet build`.
- **FR-003**: The system MUST provide a CI workflow on a standard runner. The `static` and `sca` jobs MUST require no Stardew Valley, SMAPI, or game assemblies. The `build-analysis` job (analyzers, tests, coverage) MUST materialise the repository's pinned `gamepath.zip` build-deps release via `BUILDDEPS_TOKEN` and MUST fail closed with an explicit message when that token is unavailable; it MUST NOT require a game installation.
- **FR-004**: The system MUST run a fast SAST scan with the pinned Semgrep CE CLI on every pull request, completing in under two minutes on this repository.
- **FR-005**: The system MUST run a deeper SAST scan (CodeQL) for C# on a schedule (nightly/weekly) or on manual dispatch, using `build-mode: none` with no game assemblies or token; it MUST NOT be a per-PR blocking gate.
- **FR-006**: The system MUST run dependency-vulnerability scanning with a pinned Trivy release against the committed NuGet lock files in CI.
- **FR-007**: The system MUST measure code coverage (Coverlet) and enforce a threshold, with ReportGenerator 5.5.12 for reporting.
- **FR-008**: The system MUST measure C# code duplication with pinned jscpd and enforce a threshold; non-C# formats are out of scope for the gate.
- **FR-009**: Thresholds for coverage and duplication MUST be set from the current measured baseline and only ratchet in the improving direction (coverage up, duplication down) unless explicitly justified.
- **FR-010**: SonarCloud MUST remain in parallel during a validation period of 2–4 weeks of green `Quality Gate` runs. A green run means every blocking job (`static`, `sca`, `build-analysis`, `gate`) concluded `success` on a pull request or on a push to `dev`/`main`, with no skipped blocking job and no accepted-risk exception.
- **FR-011**: The system MUST produce actionable diagnostics without an external dashboard: code-level findings (analyzers, Semgrep, CodeQL) MUST include file and line; dependency findings MUST include package, installed version, CVE, and target file; coverage findings MUST name the lowest-covered classes; duplication findings MUST include `file:line` for both clone sides; configuration failures MUST include an explicit error message.
- **FR-012**: The system MUST allow targeted, documented suppression of false positives without blanket-disabling rules.

### Key Entities

- **Quality Gate Job**: A CI job running on a standard runner, aggregating the blocking analyzer build, Semgrep, dependency scan, coverage, and duplication results into a pass/fail. The scheduled CodeQL deep scan is a separate non-blocking job.
- **Baseline Measurement**: The recorded current values of coverage and duplication, used to set initial thresholds.
- **Threshold (Ratchet)**: A value that only moves upward; a regression fails the check.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new quality-gate CI workflow runs on every pull request and push on a standard runner. The `static` and `sca` jobs require no `STARDEW_GAME_PATH` and complete within 10 minutes. The `build-analysis` job sources the pinned build-deps release via `BUILDDEPS_TOKEN` and completes within 20 minutes. Per-job and total durations are recorded on each run.
- **SC-002**: `dotnet build` with analyzers enabled and `TreatWarningsAsErrors` passes on the clean codebase (0 reliability defects), and any introduced defect fails the build naming file and line.
- **SC-003**: Security scans report 0 HIGH/CRITICAL findings across the scanned scope for a clean pull request: Semgrep scans the whole `LivingRoots/` tree, Trivy scans the committed `packages.lock.json` files, and the scheduled CodeQL scan covers the whole C# codebase. New-code-only scoping is not provided; the gap is recorded in the parity evidence.
- **SC-004**: Coverage does not drop below the ratcheted baseline threshold; a regression fails the check.
- **SC-005**: Duplication does not exceed the ratcheted baseline threshold; a regression fails the check.
- **SC-006**: The maintainer can identify the offending file and line from CI output without opening an external dashboard.
- **SC-007 (DEFERRED)**: After 2–4 weeks of green local runs, SonarCloud can be removed without loss of the quality signal. This feature produces the removal checklist, rollout start date, and green-run criteria; the removal itself is a follow-up and MUST NOT be performed here.

## Assumptions

- The number of warnings surfaced by enabling the analyzers is manageable (to be measured in Phase 1); if not, Phase 1 becomes a remediation task.
- `SonarAnalyzer.CSharp` and the .NET SDK analyzers do not produce excessive false positives on this codebase (to be validated by the Phase 1 build).
- The maintainer accepts a temporary period where both SonarCloud and the local gate run in parallel (no cost, both free).
- Baseline coverage and duplication values can be captured from the current codebase before thresholds are set.
- CodeQL's runtime is acceptable as a deeper/scheduled scan, with Semgrep as the fast per-PR gate.
- No license conflict (e.g., LGPL-2.1 Semgrep engine) affects the mod's distribution (to be verified).
- The `Build and Test` workflow remains unchanged. It is gated by `vars.BUILDDEPS_ENABLED` and has recorded green runs; it is not dormant. The new quality gate is a separate workflow and check, and reuses the same build-deps materialisation pattern.
- No trend dashboard or inline PR decoration is required; build-fail with file/line diagnostics is sufficient.
- SonarCloud is removed after 2–4 weeks of green local runs; it is not kept in parallel indefinitely.