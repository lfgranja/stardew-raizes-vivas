# Phase 1 Data Model: Test Observables

- **Feature**: `027-stabilize-reactive-invalidation-tests`
- **Date**: 2026-11-19

No persisted game/save entities change. Test assertions observe:

| Entity | Representation | Deterministic Invariant |
|---|---|---|
| Synchronous invalidation state | `overlayRenderer.IsCacheEmpty` (`bool`) | Becomes `true` immediately when `OnTileHealthChanged` / `UpdateHealth` / `InvalidateCache` completes |
| Color-service consultation count | Moq `Verify(..., Times.*)` / callback counter (`int`) | `0` new consultations during invalidation; `<= 1` per visible tile during next-frame rebuild |
| Next-frame overlay output | `_overlayRenderer.GetOverlays(...)`, `RenderOverlays(...)`, `RenderOneDrawableValueFrame(...)` | Immediately populates non-empty cache / emits updated tile health (`75`) on the very next frame |
| Wall-clock budget (`16.67ms`) | `ModConstants.MaxRenderTimeMs` | Documented as Gate 3 (`scripts/verify-scenario.sh`) responsibility, excluded from unit/property pass-fail |
