# Release — 025-ci-secret-guard-explicitness

- **Spec:** `specs/025-ci-secret-guard-explicitness/spec.md`
- **Feature:** Explicit diagnostics and regression guard for secret-dependent CI jobs
- **Gate:** R7 (SemVer, changelog, rollback, communication)
- **Mod version:** unchanged — `0.1.0` (no gameplay, save-data or locale change)
- **Change-level classification:** internal tooling PATCH — no public release artifact, no tag, no publication
- **Date:** 2026-11-18
- **Type:** pre-release record (not published)

## SemVer assessment

No mod version bump: the shipped game artifact is byte-for-byte unchanged in behavior. The change is a PATCH-level improvement to repository CI tooling and documentation only. Any future version bump/tag remains an owner decision (spec 019 T006/T007).

## Changelog

- 025-ci-secret-guard-explicitness: explicit fail-fast diagnostic (`::error::BUILDDEPS_TOKEN is not set`) added to the build job (FR-001..FR-004; tasks T004–T005).
- 025-ci-secret-guard-explicitness: deterministic workflow-contract test prevents future unguarded credential consumers, with explicit best-effort and owner-gated exemptions (FR-005..FR-006; tasks T003, T006–T007).
- 025-ci-secret-guard-explicitness: local quality-gate guide now states the local/hosted trust boundary and the [NV] remote-claim rule (FR-007..FR-008; task T008).
- Related bug precedents honoured, not modified: `.specify/bugs/ci-skipped-job-false-green`, `.specify/bugs/build-and-test-failures`, `.specify/bugs/check-formatting-ci-failure`.

## Rollback

Revert the merge commit for this feature into `dev/`. No save schema, manifest, asset or locale migration exists, so rollback is safe. After rollback, `scripts/release-gate.sh` and `scripts/ci/sdd-gate.py` must be re-run and stay green. Restoring the pre-change state returns the build job to an opaque credential failure — accepted as the known prior behavior, not a data loss.

## Communication

- No public announcement, tag, GitHub Release, Nexus upload, or mod-version bump. This record is the communication artifact.
- Open dependabot PRs are NOT touched by this change; their red status remains [NV] at step level.

## Veredito

RELEASED (repository-scoped only; not published)
