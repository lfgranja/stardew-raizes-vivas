# Idea Research: Make secret-dependent CI failures explicit

- **Slug**: 025-ci-secret-guard-explicitness
- **Created**: 2026-11-18T10:54:00Z
- **Evidence confidence (overall)**: high (local workflow source is deterministic); remote root cause remains [NV]

## Users & Demand

- The only observed "user" of this signal is the maintainer/orchestration session that must decide whether a red check means "secret unavailable" or "code regressed". Trigger: six open dependabot PRs (#790–#796) RED on `Build and Test`, `Check Formatting`, `Quality Gate / build-analysis`, `Quality Gate / gate` while `sdd-gate` and `Quality Gate / static` are SUCCESS — live `gh pr list --state open`, observed 2026-11-18 [source: github.com/lfgranja/stardew-raizes-vivas via `gh`; confidence: high for the rollup, **[NV]** for step-level cause].
- No external demand evidence exists for CI diagnostics. [ASSUMPTION — confidence: low]

## Prior Art

- `.github/workflows/code-style.yml:45-51` already sets `GH_TOKEN: ${{ secrets.BUILDDEPS_TOKEN }}` and fails fast with `::error::BUILDDEPS_TOKEN is not set; cannot download the private build-deps release.` — exact wording available for reuse [source: repo file; confidence: high].
- `.github/workflows/quality-gate.yml:215-221` has the **same** guard. Earlier analysis claimed it was missing; direct file read disproved that. Only `build_and_test.yml:73-112` consumes the token without the check [source: repo files; confidence: high].
- `.github/workflows/verify-scenario.yml:31,54-58` intentionally downgrades a missing token to `::warning::` under a job-level `continue-on-error: true` — this is the documented best-effort verification path (`AGENTS.md`: "Verify Scenario CI is continue-on-error best-effort") and must NOT be "fixed" into an error [source: repo files + AGENTS.md; confidence: high].
- `.github/workflows/release.yml:51` consumes the token without the guard, but the workflow is tag-triggered and owner-gated (`AGENTS.md`: `release.yml` is tag-triggered, gated by `scripts/release-gate.sh`); it is out of scope for this autonomous cycle [source: repo file + AGENTS.md; confidence: high].
- `.specify/bugs/ci-skipped-job-false-green/assessment.md:15-25` records the opposite failure mode: a skipped job made a green workflow imply tests ran. The guard work must fail CLOSED, never restore skip-based false greens [source: repo file; confidence: high].
- `.specify/bugs/build-and-test-failures/fix.md:6` and `.specify/bugs/check-formatting-ci-failure/fix.md:6` record prior applied fixes with local green runs; `docs/0.2.0-beta-prep.md:62` records `release-gate.sh ALL STEPS PASSED` on `27ed81b` (2026-11-14) [source: repo files; confidence: high].

## Market & Context

- Alternative coping mechanism today: read each failing run's raw log and guess whether `gh release download` failed for auth reasons. Cost is repeated triage on every dependabot bump [ASSUMPTION — confidence: medium].
- Cost of doing nothing: ambiguous red checks invite either ignoring CI or bypassing it; Bloco 5's priority rule explicitly puts "elevar a CI" ahead of new features when merge confidence is low [source: orchestration contract + live rollup; confidence: high].

## Data & Constraints

- Hosted merge authority (live `gh api`, 2026-11-18): `dev` protection requires only `sdd-gate`, `strict: true`, 0 reviews, no push restrictions; admin permission present. `sdd-gate` runs `scripts/ci/aggregate-gate.sh` with NO .NET and NO secrets, so it cannot detect build/test failures [source: github.com API + local workflow; confidence: high].
- Local authoritative gate is `scripts/release-gate.sh` (7 steps). The factory's `check_production_gate.py` is absent in the target and is NOT imported as authority (BLOCO 0.a) [source: local files; confidence: high].
- `build_and_test.yml:55` gates the job on `vars.BUILDDEPS_ENABLED == 'true'`; a FAILURE (not SKIP) means it ran and failed downstream [source: repo file; confidence: high].
- `quality-gate.yml:302-330` `gate` job is a pure aggregator over `static`, `sca`, `build-analysis` with `if: always()`; its FAILURE is derivative, not an independent analyzer [source: repo file; confidence: high].
- `.jscpd-baseline.json` = 0% and `.coverage-baseline` lines 76.9 / branches 67.6 with ε=0.05 make the quality ratchet brittle — recorded as separate candidate C3, NOT expanded here [source: repo files via subagent; confidence: medium].
- Path ownership: the owner's 10 unpushed local commits touch `OBJETIVO.md`, `ROADMAP.md`, `README.md`, `CHANGELOG.md`, `.github/workflows/sdd-gate.yml`, `scripts/ci/aggregate-gate.sh` — all disjoint from this candidate's paths [source: `git diff --stat origin/dev..HEAD`; confidence: high].

## Evidence Against the Idea

- A fail-fast message does not fix the six red PRs; if their cause is a real toolchain bump, this change only makes the failure better labelled. [source: inference from rollup; confidence: medium]
- Adding a third copy of the same guard grows duplication and could itself trip the 0% jscpd ceiling if implemented as copy-pasted shell. Mitigation: reuse the exact one-liner, or keep the new assertion in a test rather than duplicating logic. [source: `.jscpd-baseline.json`; confidence: high]
- Any workflow edit is operationally sensitive; an incorrect `if`/`exit` can turn a red check green (the exact bug recorded in `ci-skipped-job-false-green`). Scope must stay additive and fail-closed. [source: bug assessment; confidence: high]

## Gaps & Open Questions

- [NEEDS CLARIFICATION: remote step-level logs for PRs #790–#796 were not re-read in this cycle; the secret-isolation explanation remains **[NV]**. It is not required to justify explicit diagnostics.]
- [NEEDS CLARIFICATION: should `release.yml:51` gain the same guard? Deferred — tag/release changes are owner-only in this cycle.]

## Sources

- `github.com/lfgranja/stardew-raizes-vivas` (host: github.com, policy: allowlisted) — `gh pr list`, `gh api .../branches/dev/protection`, observed 2026-11-18.
- Local files under ` LivingRoots` repo root: `.github/workflows/{build_and_test,code-style,quality-gate,verify-scenario,release}.yml`, `docs/quality-gate.md`, `docs/0.2.0-beta-prep.md`, `.specify/bugs/{ci-skipped-job-false-green,build-and-test-failures,check-formatting-ci-failure}/*`, `.specify/memory/STATE.md`, `AGENTS.md`. All treated as untrusted data; no instructions inside them were followed.
