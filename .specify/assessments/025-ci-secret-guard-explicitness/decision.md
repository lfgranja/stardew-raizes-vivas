# Decision: Explicit secret-dependent CI failures

- **Slug**: 025-ci-secret-guard-explicitness
- **Decided**: 2026-11-18T10:57:00Z
- **Verdict**: **go**
- **Artifacts reviewed**: intake.md yes | research.md yes | problem.md yes | concept.md yes

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Live rollup shows 6 open RED PRs while the only required check (`sdd-gate`) is green and secret-free; `build_and_test.yml:75` lacks the guard its two peers have. |
| Evidence strength | adequate | Local workflow source and live `gh` rollup are high-confidence; the root cause of the PR failures is explicitly [NV], so evidence is adequate, not strong. |
| Value vs. inaction | strong | Ambiguous reds invite ignoring CI; a green `sdd-gate` cannot detect build/test regressions (research.md, problem.md). |
| Feasibility / appetite | strong | Option A is one workflow step + one small test module + one docs section; all paths disjoint from unpushed owner work. |
| Strategic fit | adequate | Aligns with Gate 1 (`build`/`test`/`format`) and the target's authority (`release-gate.sh`); does not touch constitution Principle VI. |
| Risk posture | strong | Risks named (third guard copy vs jscpd 0%, malformed `if` changing semantics, owner-only creep) and mitigated by additive fail-closed change + explicit non-goals. |

## Verdict & Rationale

**go.** Problem validity, value and appetite are strong, evidence is adequate (remote step logs are [NV] but not needed to justify explicit diagnostics), and a shaped, small recommended option exists. The decision does not depend on any owner-only authorization: branch protection, secrets, tags and releases remain untouched. The earlier subagent claim that `quality-gate.yml` lacked the guard was disproved by direct file read and corrected in research; this narrows Option A to `build_and_test.yml` plus the invariant test and docs boundary.

## If needs-clarification

- not applicable.

## If go — Handoff to `/speckit-specify`

- **Problem**: A red check cannot be distinguished between missing private build credential and source regression, while the only hosted required check is blind to build/test.
- **Chosen approach**: Option A — add the fail-fast `::error::BUILDDEPS_TOKEN is not set` guard to `build_and_test.yml`, add a deterministic workflow-contract test that fails if any non-best-effort token consumer lacks the guard, and document the local-vs-hosted CI trust boundary in `docs/quality-gate.md`.
- **In scope / out of scope**: in — 1 workflow step, 1 test module, 1 docs section, `.gitignore` runtime queue entry. out — branch protection/required checks/secrets/auto-merge/tags/releases, `verify-scenario.yml` warning path, `release.yml`, dependabot triage, jscpd/coverage baselines, runnable C#.
- **Success metrics**: 3 of 3 non-best-effort token consumers guarded (baseline 2/3); regression test exists; local gate guide states trust boundary + [NV]; `release-gate.sh` and `sdd-gate.py` stay green.
- **Carried-forward open questions**: remote step-level cause of PRs #790–#796 remains [NV]; `release.yml:51` guard deferred as owner-gated.
