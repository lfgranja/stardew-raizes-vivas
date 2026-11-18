# Problem Definition: Stabilize overlay cache invalidation under interleaved events

- **Slug**: overlay-cache-interleaving
- **Created**: 2026-11-18
- **Inputs used**: intake.md | research.md

## Problem Statement

The maintainer's required `Build and Test` check on `dev` fails intermittently because a property test decides cache-invalidation correctness from wall-clock elapsed time; under normal runner load a correct frame exceeds the 16.67 ms budget, so a green code path is reported as a product defect. This matters now because a red required check blocks every merge into `dev`.

## Affected Users & Stakeholders

- **Users**: maintainer/automation pipeline — a required check flips red on ~4 of 5 runs, forcing reruns and blocking delivery.
- **Stakeholders**: repository owner (`lfgranja`) — owns branch protection and release decisions; no approver is currently required, so a deterministic check is sufficient authority.

## Goals

- Make the interleaving invariant's verdict depend only on cache-invalidation correctness, not on machine load.
- Preserve a meaningful bound on per-frame work so FR-009 is not silently weakened.
- Keep the fix test-only, so no Gate 3 real-SMAPI evidence is owed.

## Non-Goals

- Changing `VisualizationService`/`OverlayRenderer` production cache logic (instrumentation showed no defect there).
- Changing branch protection, promoting other checks, enabling auto-merge, or merging the 7 owner/dependabot PRs.
- Adding wall-clock benchmarks, public release/tag actions, D1 sessions, or human Gate 3 attestation.

## Success Metrics

- The property passes **5/5** consecutive local runs with distinct FsCheck seeds (baseline: 1/5).
- Full `dotnet test -c Release` suite green (baseline: 1 failed / 2003 on the dev push).
- `Build and Test` required check green on the PR and on the subsequent `dev` push (baseline: RED on run `44309131539`).

## Cost of Inaction

Every push to `dev` keeps an ~80% chance of failing the required check, blocking autonomous merges and consuming maintainer attention on false positives.

## Open Questions

- [NEEDS CLARIFICATION: whether to retain a separate wall-clock benchmark outside the property — recorded as debt, not required for this fix.]
