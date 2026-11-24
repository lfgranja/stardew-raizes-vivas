# Idea Intake: Stabilize overlay cache invalidation under interleaved events

- **Slug**: overlay-cache-interleaving
- **Created**: 2026-11-18
- **Source**: repo path `LivingRoots.Tests/Invariants/VisualizationCacheInvalidationInvariantsTests.cs` + live CI run `44309131539` (GitHub Actions, `gh run view`)
- **Type**: fix

## Idea (as captured)

> "CI red on dev push after PR 800: sole failure = FsCheck property `RenderOverlays_ForAnyDisableEnableInterleaving_RebuildsOnFirstFrameAfterReEnableWithinBudget`. Local reproduction fails 4 of 5 runs with distinct seeds and a 23-event counterexample. `Build and Test` is a required check on dev, so a red mainline blocks every merge."

## Restated

The overlay-list cache invalidation invariant is not stable for arbitrary disable/enable, mutation and configuration-change interleavings. The proposal is to diagnose the failing assertion and make the invariant hold deterministically so the required `Build and Test` check stays green on mainline.

## Origin & Context

- **Raised by**: autonomous orchestration session `orch-lrv-20261118-c3f1` (BLOCO 5, candidate C1, score 6.25/25).
- **Trigger**: live CI run `44309131539` on `dev` at `4b54c30` reported 2003 tests / 1998 passed / **1 failed** / 4 skipped; the sole failure was this property (FsCheck StdGen `(1549256609,297700084)`). Local reruns on `30ce5c9`: 4/5 failed.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: which of the property's assertions actually returns false for the counterexample?]
- [NEEDS CLARIFICATION: is the defect in production cache invalidation or in the test's interleaving model?]
- [NEEDS CLARIFICATION: if runnable C# changes, which Gate 3 evidence is obtainable headlessly vs owner-gated human/D1?]
