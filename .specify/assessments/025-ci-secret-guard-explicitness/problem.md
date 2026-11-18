# Problem Definition: Ambiguous secret-dependent CI failures

- **Slug**: 025-ci-secret-guard-explicitness
- **Created**: 2026-11-18T10:55:00Z
- **Inputs used**: intake.md yes | research.md yes | user input only no

## Problem Statement

A maintainer or merge operator looking at a red pull-request check cannot tell whether the failure came from an unavailable private build credential or from a genuine source regression: `build_and_test.yml` consumes `BUILDDEPS_TOKEN` without the fail-fast diagnostic that `code-style.yml` and `quality-gate.yml` already emit. This matters now because six dependabot PRs are simultaneously RED while the only hosted required check (`sdd-gate`) is green and runs without either .NET or secrets, so the visible signal is both ambiguous and weak.

## Affected Users & Stakeholders

- **Users**: merge operator / maintainer — must decide whether to trust a red or green run and whether to merge into `dev`.
- **Stakeholders**: repository owner — owns branch protection, secrets and release authorization; benefits from unambiguous failures but must not have those owner-only controls changed by an agent. [source: research.md + AGENTS.md]

## Goals

- Make a missing `BUILDDEPS_TOKEN` immediately identifiable in the check output of every non-best-effort job that consumes it.
- Preserve fail-closed semantics: a missing credential remains a FAILURE, never a skipped or green run.
- Give the local gate guide an explicit statement of what hosted CI proves, what it does not, and which claims remain [NV].
- Keep the change disjoint from the owner's unpushed local docs commits.

## Non-Goals

- Changing branch protection, required checks, secrets, auto-merge, tags, or releases (owner-only).
- Changing the best-effort `verify-scenario.yml` warning path or its `continue-on-error: true`.
- Diagnosing or fixing the six dependabot PRs themselves (other sessions' work).
- Fixing the separate jscpd 0% / coverage-baseline fragility (candidate C3).
- Any runnable C# change (would invoke Gate 3 real-SMAPI obligations).

## Success Metrics

- All non-best-effort workflow steps that set `GH_TOKEN: ${{ secrets.BUILDDEPS_TOKEN }}` emit the explicit `::error::BUILDDEPS_TOKEN is not set` guard before first use (baseline: 2 of 3 such steps — missing in `build_and_test.yml:75`).
- A deterministic test fails if a future workflow adds a token-consuming step without the guard (baseline: no such test exists).
- `docs/quality-gate.md` states the local-vs-hosted trust boundary and the [NV] rule (baseline: local command list only).
- `scripts/release-gate.sh` and `scripts/ci/sdd-gate.py` stay green (baseline: step 1–3 OK on 2026-11-18; sdd-gate PASS 0 errors / 29 warnings).

## Cost of Inaction

Every dependabot bump produces another ambiguous red run; the operator either wastes time reading raw logs or learns to ignore red checks. Since `sdd-gate` is the only required hosted check and it cannot see build/test failures, a future PR with a genuinely broken build could still be merged after a green `sdd-gate`, while local quality gates drift unnoticed. [source: research.md + live `gh api` protection]

## Open Questions

- [NEEDS CLARIFICATION: remote step-level logs for PRs #790–#796 were not re-read; the secret-isolation explanation is [NV]. Explicit diagnostics are justified regardless.]
- [NEEDS CLARIFICATION: should `release.yml:51` gain the same guard? Deferred — tag/release path is owner-only in this cycle.]
