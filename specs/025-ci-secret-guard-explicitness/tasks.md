# Tasks: Explicit secret-dependent CI failures

**Feature**: `specs/025-ci-secret-guard-explicitness/spec.md` | **Branch**: `feature/025-ci-secret-guard-explicitness` | **Date**: 2026-11-18

## Phase 1: Setup

- [X] T001 Confirm `.specify/feature.json` anchors `specs/025-ci-secret-guard-explicitness` and the worktree branch matches — `.specify/feature.json`
- [X] T002 [P] Add runtime queue/worktree entries (`.specify/queue/`) to `.gitignore` with an explicit diff — `.gitignore`

## Phase 2: Foundational (blocking prerequisite — RED first)

- [X] T003 Write the failing credential-guard contract test that enumerates all workflow steps referencing `secrets.BUILDDEPS_TOKEN` and requires an `::error::` guard + `exit 1` for every non-best-effort consumer; it MUST fail naming `.github/workflows/build_and_test.yml` before the guard exists — `scripts/quality-gate/tests/test_ci_secret_guards.py`

## Phase 3: User Story 1 — Diagnose a red check (Priority: P1)

**Goal**: a missing credential produces an explicit error before any private download.
**Independent Test**: run the contract test against the real workflow set; it passes only when the build job is guarded.

- [X] T004 [US1] Add the fail-fast `BUILDDEPS_TOKEN is not set` guard with `exit 1` to the `Fetch game assemblies` step before `mkdir`/`gh release download`, reusing the exact wording from `code-style.yml:49` — `.github/workflows/build_and_test.yml`
- [X] T005 [US1] Record the RED (before) and GREEN (after) command outputs with timestamps in `specs/025-ci-secret-guard-explicitness/test.md` — `specs/025-ci-secret-guard-explicitness/test.md`

## Phase 4: User Story 2 — Keep the guard from rotting (Priority: P2)

**Goal**: a future unguarded credential consumer fails a deterministic local check.
**Independent Test**: full test module passes on the real set and detects a synthetic unguarded fixture.

- [X] T006 [P] [US2] Add fixture-based assertions for a synthetic unguarded consumer (must be detected), for the exempt best-effort job with `::warning::`/`continue-on-error: true` (must not be reported), and for the credential-present path (FR-003: guard emits no error and leaves the step body unchanged) — `scripts/quality-gate/tests/test_ci_secret_guards.py`
- [X] T007 [US2] Run the full quality-gate test module and paste the pass count — `scripts/quality-gate/tests/test_ci_secret_guards.py`

## Phase 5: User Story 3 — Know what green CI proves (Priority: P3)

**Goal**: the local gate guide states the local/hosted trust boundary and explicit [NV] markers.
**Independent Test**: read the dedicated section and locate all four required statements.

- [X] T008 [US3] Add a CI trust-boundary section naming `scripts/release-gate.sh` as local authority, `sdd-gate` as the only hosted required check (blind to build/test), the credential-dependent job families, and the [NV] remote-root-cause boundary — `docs/quality-gate.md`

## Phase 6: Polish & cross-cutting

- [X] T009 [P] Run `python3 scripts/ci/sdd-gate.py` and paste stdout + exit code — `scripts/ci/sdd-gate.py`
- [X] T010 [P] Run `GamePath=/home/luis/stardew-assemblies ./scripts/release-gate.sh` and paste the final `ALL STEPS PASSED` line — `scripts/release-gate.sh`
- [X] T011 Verify no runnable C#, locale, or save-data file was touched and that Gate 3 is recorded as N/A, not claimed — `specs/025-ci-secret-guard-explicitness/test.md`
- [X] T012 Write append-only `converge.md` with requirement→artifact traceability after gates are green — `specs/025-ci-secret-guard-explicitness/converge.md`

## Dependencies

- T003 (RED test) blocks T004 (guard) — TDD order is mandatory.
- T004 blocks T005 (evidence must capture both states).
- T001/T002 are independent setup; T006 depends on T003's module; T008 is independent of T004–T007.
- T009/T010 require T004 and T008 complete; T012 requires T009/T010 green.

## Parallel execution examples

- Wave A: T001 and T002 (different files).
- Wave B: T006 and T008 (test module vs docs, disjoint paths).
- Wave C: T009 and T010 (both read-only gates; different processes, run sequentially if host memory is constrained).

## Implementation strategy

MVP is Phase 1 + Phase 2 + User Story 1: the explicit diagnostic plus its RED/GREEN evidence. User Story 2 then hardens the invariant, and User Story 3 records the trust boundary. Each phase leaves the repository in a green, reviewable state. Deliberately excluded: owner-only controls, `verify-scenario.yml`, `release.yml`, dependabot PR triage, jscpd/coverage baselines, all runnable C#.
