# Bug Verification: VisualizationCacheInvalidationInvariantsTests failures

- **Slug**: visualization-cache-invalidation-failures
- **Tested**: 2026-10-30
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: failed

## Summary

The fix was recorded as `not-applied` (deferred to T072), so the original symptom still reproduces: 4 of the 5 `VisualizationCacheInvalidationInvariantsTests` property tests fail on a clean build.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | `dotnet test ... --filter VisualizationCacheInvalidationInvariantsTests` | fail | 4 failures, 1 pass (unchanged) |
| New / updated tests | none | not-run | No fix applied |
| Regression suite | `dotnet test ... --no-build` | pass | 1436 passed, 4 failed (same 4) |
| Lint / type-check | `dotnet build ... -c Release` | pass | 0 errors, 0 warnings |

## Output Excerpts

```
Com falha! – Com falha: 4, Aprovado: 1, Ignorado: 0, Total: 5, Duração: 1 s
```

## Residual Risks

- The cache invalidation bug remains unresolved; overlay opacity/health edits may stay invisible until camera moves.
- The production-source guard (`PatternMinOpacity`) is satisfied via a comment reference only.

## Recommendation

Reopen / defer: rerun `/speckit.bug.assess` with the new evidence captured here, or implement T072 (extend the cache key in `OverlayRenderer.GetOverlays`) before re-testing.