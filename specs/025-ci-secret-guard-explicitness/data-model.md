# Phase 1 Data Model: 025-ci-secret-guard-explicitness

No persisted data. All "entities" are metadata derived from workflow files at test time.

## CredentialDependentStep

- **declaring_file** — path of the workflow file (e.g. `.github/workflows/build_and_test.yml`).
- **job_name** — enclosing job id (e.g. `build-test`).
- **step_name** — display name of the step consuming the credential.
- **guard_present** — boolean; true when the step body contains an empty-token check emitting `::error::` and `exit 1`.
- **best_effort** — boolean; true when the enclosing job declares `continue-on-error: true` or the step uses the documented `::warning::` path.
- **Validation rules**:
  - `best_effort == false` MUST imply `guard_present == true`.
  - A step with an empty-string credential is treated as missing.
  - The guard MUST precede the first private-download command in the step body.

## LocalAuthoritativeGate

- **name** — `scripts/release-gate.sh`.
- **steps** — seven deterministic steps: format, locked restore, Release build, full tests, launch-free smoke, docs markers, protected paths.
- **Relationship** — is the local source of truth for "production-deployable code"; independent of hosted CI.

## HostedRequiredCheck

- **name** — `sdd-gate`.
- **properties** — required by branch protection on `dev`, `strict: true`, no approving reviews required, no secrets, no .NET.
- **Relationship** — is a hosted merge prerequisite but does NOT imply Gate 1 (build/test/format) or Gate 3 (real game) passed.

## State Transitions

None. Workflow files are static configuration; the test derives facts by reading them. The only "transition" is the intended one: an unguarded step becomes guarded, changing `guard_present` from false to true.
