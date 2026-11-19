# Release Record: Stabilize overlay cache invalidation property

- **Feature**: 026-stabilize-overlay-cache
- **Date**: 2026-11-18
- **Stage**: R7 (release) — R4 approved above
- **SemVer**: **0.1.1** (PATCH — test-only determinism fix; no player-facing behavior, no API change)
- **Publication**: draft only — **no tag, no public release, no Nexus upload** (owner-gated; not simulated)

## Changelog (0.1.1)

- **FR-001..FR-006 / T005, T006, T007, T011** — replaced wall-clock frame-budget assertion in the overlay interleaving property with a deterministic per-frame consultation cap; preserved randomized coverage (`MaxTest = 100`) and the four event kinds; documented Gate 3 ownership of wall-clock budget.
- **bug class** — intermittent red `Build and Test` required check on `dev` (CI run `44309131539`, FsCheck StdGen `1549256609`) caused by runner-load timing, not by a product defect.
- No gameplay, i18n, save-format, or multiplayer behavior changed.

## Evidence

- R4 validation APPROVED at `.specify/validations/026-stabilize-overlay-cache/validate.md`.
- 5/5 property runs; full Release suite 1999 passed / 0 failed / 4 skipped; format exit 0; zero production diff.
- Live CI result on the PR and post-merge `dev` push: **[NV] — to be observed in Bloco 7**.

## Rollback

Single-file revert of `LivingRoots.Tests/Invariants/VisualizationCacheInvalidationInvariantsTests.cs` to `origin/dev 4b54c30` restores the previous property. No production rollback, save migration, or configuration change is involved. If the required check still fails after rollback, the cause is external to this change (see assessment research.md).

## Owner-only, not performed

Tag/public release, Nexus channel, branch protection, secrets, auto-merge, D1 live session, human Gate 3 attestation, SC-004.
