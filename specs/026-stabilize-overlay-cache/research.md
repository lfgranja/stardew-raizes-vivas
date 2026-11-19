# Phase 0 Research: Deterministic frame-work bound

- **Decision**: replace wall-clock elapsed time with a deterministic cap of one color-service consultation per visible tile per frame.
- **Rationale**: instrumentation showed `DIAG:budget` was the only failing assertion; all cache-consistency assertions passed. Wall-clock time in a mocked unit property cannot honestly represent a 16.67 ms game-frame budget under runner load.
- **Alternatives considered**:
  1. Widen the threshold / retry — rejected: verdict remains environment-dependent and masks real regressions.
  2. Change production cache logic — rejected: no defect found; would owe Gate 3 evidence for no benefit.
  3. Do nothing — rejected: required check stays red ~80% of runs.
- **Dependency check**: FsCheck.Xunit 2.16.6 `Property(MaxTest = 100)` supports reproducible replays but the defect is input-independent (4/5 seeds fail); no seed pinning needed.
- **Evidence**: live CI run `44309131539`; local 5-run reproduction; instrumented `DIAG:budget`; `OverlayRenderer.cs:419-434` cache key; `VisualizationService.cs:190-215` re-enable invalidation.
- **Open questions**: none. Separate wall-clock benchmark recorded as debt, not required.
