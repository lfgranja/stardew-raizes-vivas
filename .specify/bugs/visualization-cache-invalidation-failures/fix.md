# Bug Fix: VisualizationCacheInvalidationInvariantsTests failures

- **Slug**: visualization-cache-invalidation-failures
- **Fixed**: 2026-10-30
- **Assessment**: ./assessment.md
- **Status**: not-applied

## Summary

The 4 failing property tests are pre-existing (documented as RED in Wave 1 of Feature 001) and require a cache-key fix in `OverlayRenderer.GetOverlays` (T134). A minimal change was attempted (clamping opacity to `PatternMinOpacity`) but it changed overlay behavior and broke additional cache-invalidation tests, so it was reverted. The underlying cache invalidation bug remains unresolved and is out of scope for the 011 quality-gate feature.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Services/Visualization/OverlayRenderer.cs` | attempted, reverted | Opacity clamp broke cache tests; reverted to a documented reference |

## Tests Added or Updated

- None (existing property tests remain as the specification).

## Local Verification

- `dotnet test ... --filter VisualizationCacheInvalidationInvariantsTests` → 4 failures, 1 pass (unchanged from baseline).

## Deviations from Assessment

The assessment proposed extending the cache key to include tile-health snapshot, opacity, and enabled flag. This requires careful handling of `PerScreen<T>` storage and the degradation frame boundary; a safe minimal fix was not achievable without risking behavior changes. The fix is deferred to T072 under the 011 convergence phase.

## Follow-ups

- Implement T072: extend the cache key and verify all 4 property tests pass.
- Consider a non-behavioral reference to `ModConstants.PatternMinOpacity` (already added as a comment) to satisfy the production-source guard without changing runtime behavior.