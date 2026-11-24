# Release Record: Stabilize reactive invalidation tests against wall-clock flakiness

- **Feature**: `027-stabilize-reactive-invalidation-tests`
- **Date**: 2026-11-19
- **Stage**: R7 (`release`) — R4 (`validate.md`) APPROVED above
- **SemVer**: **0.1.2** (PATCH — test-only determinism fix; zero production behavior, manifest, API, or save-format changes)
- **Publication**: repo PR draft only — **no git tag, no public GitHub/Nexus release, no auto-merge** (owner-gated)

## Changelog (0.1.2)

- **FR-001, FR-003, FR-004, FR-005 / T005, T006, T007, T009** (`LivingRoots.Tests/SoilHealthVisualizationTests.cs`, commit `4e5bb77`): removed wall-clock `Stopwatch <= ModConstants.MaxRenderTimeMs` assertions and unused `using System.Diagnostics;` from `OnTileHealthChanged_SubscribedHandler_InvalidatesOverlayCacheImmediatelyWithinFrameBudget`, `OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame`, and `T143_OverlayDrawableOnly_SuppressesFabricated_UnknownFloorAt1_ReactsWithinFrameBudget`; replaced with synchronous `IsCacheEmpty` checks, bounded consultation verification, and immediate next-frame rebuild assertions, documenting Gate 3 ownership of 16.67ms frame timing.
- **FR-002, FR-003, FR-004, FR-005 / T008, T009** (`LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs`, commit `aed6322`): replaced wall-clock `Stopwatch` check in `T031b_OverlayTiming_ReactiveInvalidation_WithinFrameBudget` with block-bodied consultation-tracked renderer assertions (`1` warm, `0` on invalidation, `1` on next-frame rebuild), preserved `[Property(MaxTest = 50)]`, removed unused `using System.Diagnostics;`, and documented Gate 3 timing ownership.
- **Bug / failure class resolved**: intermittent `scripts/release-gate.sh` Step 4 (`exit 14`) failure on `SoilHealthVisualizationTests.cs:419` (`LATENCY DEFECT: reactive cache invalidation took 27,346ms, which exceeds the frame budget of 16,67ms.`) and three sibling wall-clock timing assertions in reactive-invalidation tests.
- **FR-006 / T010, T011**: zero changes to `LivingRoots/`, `LivingRoots/i18n/`, `VisualizationPerformanceTests.cs`, `GrowthPerformanceTests.cs`, or `VisualizationConstantsTests.cs`; append-only verified-state reconciliation recorded in `OBJETIVO.md` (`a8b1efa`) and `ROADMAP.md` (`c665470`).

## Evidence

- R4 validation **APPROVED** at `.specify/validations/027-stabilize-reactive-invalidation-tests/validate.md`.
- Isolated verifier (`aed6322`): 5/5 consecutive passes (`4/4` each), Release build `0 Erro(s)`, `0` `Stopwatch` timing assertions remaining, empty `LivingRoots/` diff.
- Full Release test suite: `1999 passed / 0 failed / 4 skipped (2003)`; `dotnet format --verify-no-changes` exit `0`; `dotnet restore --locked-mode` exit `0`; launch-free smoke `51 files / 50 passed / 0 failed / 1 skipped`; `--check-docs` exit `0`; `check-protected-paths.sh` exit `0`; `i18n-parity-check.py` PASS.
- Remote PR CI status: **[NV]** until live GitHub Actions checks complete on the opened PR.

## Rollback Plan

Revert the two Green-Zone test commits without touching production code, saves, or configuration:
```bash
git revert --no-edit aed6322 4e5bb77
```
Restores the prior `Stopwatch` assertions verbatim with zero runtime impact on `LivingRoots.dll`.

## Owner-Only Boundaries (Not Performed)

Tag creation (`0.2.0-beta` / `0.1.2`), public GitHub/Nexus release, branch protection edits, secrets (`BUILDDEPS_TOKEN` / `RUNNER_AUDIT_STORE`), auto-merge, D1 two-player live session, human Gate 3 attestation, SC-004 comprehension test.
