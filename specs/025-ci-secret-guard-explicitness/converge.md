# Convergence — 025-ci-secret-guard-explicitness

- **Date**: 2026-11-18
- **Mode**: append-only
- **Anchor**: `.specify/feature.json` → `specs/025-ci-secret-guard-explicitness`
- **Scope note**: spec number 025 (< 088) therefore `analyze.md` is not required by the target convention; the read-only analysis was reported in-session and its single coverage gap (FR-003) was fixed at source in `tasks.md` (commit `e421db2`).

## Requirement → artifact traceability

| Requirement | Artifact / evidence | Status |
|-------------|---------------------|--------|
| FR-001 (explicit error before private download) | `.github/workflows/build_and_test.yml:78-82`; guard count 3 `::error::` | done |
| FR-002 (fail closed, never skip/green) | guard body uses `exit 1`; `test_ci_secret_guards.py::test_every_non_best_effort_token_consumer_is_guarded` | done |
| FR-003 (credential-present path unchanged) | `test_ci_secret_guards.py::test_credential_present_path_leaves_existing_commands_unchanged` | done |
| FR-004 (uniform wording) | identical message string in build_and_test / code-style / quality-gate | done |
| FR-005 (deterministic check) | `scripts/quality-gate/tests/test_ci_secret_guards.py`, 5 tests | done |
| FR-006 (best-effort exemption) | `test_best_effort_warning_consumer_is_exempt`, `test_best_effort_job_is_exempt`; tag-only `release.yml` excluded as owner-gated | done |
| FR-007 (trust-boundary docs) | `docs/quality-gate.md` §CI trust boundary | done |
| FR-008 ([NV] discipline) | same section states remote root cause is [NV] | done |
| SC-001 (3 of 3 guarded) | grep: 3 error guards + 1 intentional warning | done |
| SC-002 (real set + fixture) | RED 4/5 → GREEN 5/5; fixture tests pass | done |
| SC-003 (docs section) | present with four required statements | done |
| SC-004 (gates green) | see `test.md` for `sdd-gate` / `release-gate` outputs | see validation |

## User story → artifact traceability

- US1 (diagnose): `build_and_test.yml` guard + `test.md` RED/GREEN evidence.
- US2 (no rot): `test_ci_secret_guards.py` synthetic fixtures + full module run.
- US3 (trust boundary): `docs/quality-gate.md` new section + [NV] marker.

## Deviation record

- The initial subagent claim that `quality-gate.yml` lacked the guard was disproved by direct source read; scope narrowed to `build_and_test.yml` only. Recorded in `research.md` R1 and `decision.md`.
- The full `scripts/quality-gate/tests` suite has 11 pre-existing failures at `origin/dev` (12 before this branch) caused by stale version pins and suppression-policy tests; none are in files touched here. Zero new failures introduced; 5 new tests pass. This is recorded as debt, not silently fixed (separate candidate).
- No runnable C#, locale, or save-data file touched; Gate 3 real-SMAPI is N/A, not claimed.

## Append-only notice

Do not rewrite or renumber this file. Later convergence rounds append new dated sections only.
