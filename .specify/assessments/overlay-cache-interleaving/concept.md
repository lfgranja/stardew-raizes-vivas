# Concept: Stabilize overlay cache invalidation under interleaved events

- **Slug**: overlay-cache-interleaving
- **Created**: 2026-11-18
- **Recommended option**: A — deterministic work bound

## Options

### Option A — Deterministic work bound in the property
- **Sketch**: the interleaving property keeps asserting cache-consistency (one color-service consultation per visible tile after a state change, no consultations while disabled) and replaces the wall-clock elapsed-time check with a deterministic bound on the number of consultations per frame. Wall-clock budget remains verification-runner (Gate 3) evidence.
- **Appetite**: small.
- **Trade-offs**: wins full determinism and preserves a real "bounded work" invariant; sacrifices the pretense that a mocked unit test can measure a 16.67 ms game-frame budget. Risk: a reviewer could read the removed timing check as a weakened FR-009 — mitigated by an explicit comment pointing to Gate 3.
- **Rabbit holes**: refactoring too much of the test file; re-litigating the production cache key (no defect found).

### Option B — Keep wall-clock, widen the threshold / retry
- **Sketch**: multiply the budget by a generous factor or rerun failed cases automatically.
- **Appetite**: small.
- **Trade-offs**: smallest diff, but the verdict stays environment-dependent and flaky under load; retries hide real regressions.
- **Rabbit holes**: threshold bikeshedding; CI runners with wildly different performance; false confidence.

### Option C — Do nothing / rely on manual CI reruns
- **Sketch**: leave the property as is and rerun the required check until green.
- **Appetite**: none.
- **Trade-offs**: zero code change, but the required check stays red ~80% of runs and blocks merges — fails every success metric.
- **Rabbit holes**: normalizing manual reruns as process.

## Recommendation

Option A. It is the only option that makes the required check deterministic (metric: 5/5 local runs) while keeping a meaningful per-frame work bound and leaving runnable C# untouched (no Gate 3 obligation).

## Out of Scope (for the recommended option)

- Production `VisualizationService` / `OverlayRenderer` changes.
- Branch protection, auto-merge, tag/release, D1, human Gate 3, SC-004.
- Wall-clock benchmarks, dependabot PR merges, stash consumption.
- Changes to other property tests beyond the single failing one.

## Assumptions to Validate

- Instrumentation correctly identified `budget` as the sole failing assertion (validated: `DIAG:budget` thrown, all consistency assertions passed).
- A per-frame consultation cap of `VisibleTileCount` is an honest deterministic proxy for bounded work (validate during specify/analyze).
- No other FsCheck property depends on `Stopwatch` in this file (validate: only one occurrence).
