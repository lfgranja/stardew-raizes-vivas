# Idea Intake: Make secret-dependent CI failures explicit

- **Slug**: 025-ci-secret-guard-explicitness
- **Created**: 2026-11-18T10:52:29Z
- **Source**: repo path `.specify/memory/STATE.md` (BLOCO 5 decision block, committed as `4729bef`) + live `gh pr list` check rollup on `github.com/lfgranja/stardew-raizes-vivas` (observed 2026-11-18)
- **Type**: improvement

## Idea (as captured)

> "Make secret-dependent CI failures explicit (CI elevation; workflows + docs + deterministic test)" — autonomous orchestration BLOCO 5 decision, 2026-11-18.
>
> Observed context: six open dependabot PRs (#790–#796) are RED on `Build and Test`, `Check Formatting`, `Quality Gate / build-analysis` and `Quality Gate / gate`, while `sdd-gate` and `Quality Gate / static` are SUCCESS. `.github/workflows/code-style.yml:48-51` emits an explicit `::error::BUILDDEPS_TOKEN is not set`, but `.github/workflows/build_and_test.yml:73-112` and `.github/workflows/quality-gate.yml:213-228` consume the secret without that check. [NV: remote step logs were not re-read during intake.]

## Restated

Add fail-fast, explicit diagnostics to every CI job that depends on the private `BUILDDEPS_TOKEN`, so a missing secret is distinguishable from a source regression, and document the CI trust boundary locally. No new player-facing feature is proposed.

## Origin & Context

- **Raised by**: autonomous orchestration session `orch-lrv-20261118-c3f1` (no human request in this cycle; BLOCO 5 selected this candidate under the "CI elevation first" priority rule).
- **Trigger**: 6 open RED dependabot PRs plus an asymmetric guard already present in one workflow; BLOCO 0.a confirmed the target's authoritative gate is `scripts/release-gate.sh`, not the factory's `check_production_gate.py`.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: are all six RED PRs caused by secret isolation for the Dependabot actor, or do some carry genuine toolchain regressions? Remote evidence is [NV] and is not needed to justify explicit diagnostics.]
- [NEEDS CLARIFICATION: should the guard also cover `dependency-check.yml` and `verify-scenario.yml`? To be answered by research, not guessed here.]
- [NEEDS CLARIFICATION: does documenting the boundary in `docs/quality-gate.md` conflict with the owner's unpushed local docs commits? Scoping must keep paths disjoint.]
