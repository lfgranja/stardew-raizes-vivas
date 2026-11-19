# Phase 0 Research: 025-ci-secret-guard-explicitness

All Technical Context items were resolved from deterministic local reads; no NEEDS CLARIFICATION remain.

## R1 — Which job lacks the guard?

- **Decision**: only `.github/workflows/build_and_test.yml:75` sets `GH_TOKEN: ${{ secrets.BUILDDEPS_TOKEN }}` without an explicit empty-token check.
- **Rationale**: direct grep of `.github/workflows/` shows guards at `code-style.yml:48-51` and `quality-gate.yml:218-221`; `verify-scenario.yml:58` is an intentional `::warning::` under `continue-on-error: true`; `release.yml:51` is tag-triggered and owner-gated.
- **Alternatives considered**: trust the subagent claim that `quality-gate.yml` also lacked a guard — rejected after the source file disproved it. Guard every consumer including `release.yml` — rejected as owner-only scope creep.

## R2 — What exact diagnostic to use?

- **Decision**: reuse verbatim `echo "::error::BUILDDEPS_TOKEN is not set; cannot download the private build-deps release."` followed by `exit 1`, placed immediately after `set -euo pipefail` and before `mkdir`/`gh release download`.
- **Rationale**: uniform diagnostics; matches the two guarded jobs; fail-closed per `.specify/bugs/ci-skipped-job-false-green/assessment.md`.
- **Alternatives considered**: a generic "build failed" message — rejected as ambiguous; `::warning::` — rejected because missing credentials must fail the build job.

## R3 — How to enforce the invariant?

- **Decision**: a new pytest module that parses the workflow YAMLs, finds steps whose `env.GH_TOKEN` references `secrets.BUILDDEPS_TOKEN`, and asserts each has a token check with `::error::` and `exit 1`; exclude jobs with `continue-on-error: true` or whose guard uses `::warning::` by design.
- **Rationale**: the existing quality-gate test suite already parses `quality-gate.yml` with PyYAML and uses Hypothesis; no new dependency, and a regression becomes a deterministic local failure rather than a convention someone must remember.
- **Alternatives considered**: shell `grep` in CI — rejected as brittle and not testable as a unit; documenting the convention only — rejected because it fails success metric SC-002.

## R4 — What should the docs boundary say?

- **Decision**: add a dedicated section to `docs/quality-gate.md` naming `scripts/release-gate.sh` as the local authoritative gate, `sdd-gate` as the only hosted required check (blind to build/test), the three credential-dependent job families, and an explicit [NV] marker for remote step-level causes of currently red dependency-update PRs.
- **Rationale**: the assessment and live `gh api` protection read establish exactly this boundary; stating it prevents over-trusting a green required check.
- **Alternatives considered**: a new standalone document — rejected as fragmentation; editing `OBJETIVO.md`/`ROADMAP.md` — rejected because the owner's 10 unpushed local commits touch those files.

## R5 — Testing order

- **Decision**: write `test_ci_secret_guards.py` first, run it against the unguarded workflow to observe a red failure naming `build_and_test.yml`, then apply the guard and rerun to green.
- **Rationale**: constitution Principle IV (Red-Green-Refactor) and Bloco 6's TDD requirement.
- **Alternatives considered**: apply the guard first — rejected; would leave the test unable to demonstrate it catches the original defect.
