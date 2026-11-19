# Concept: Stabilize reactive invalidation tests against wall-clock flakiness

- **Slug**: reactive-invalidation-determinism
- **Created**: 2026-11-19
- **Recommended option**: A — deterministic synchronous invalidation + next-frame bounded rebuild

## Options

### Option A — Replace all 4 reactive-invalidation wall-clock checks with deterministic synchronous + bounded-work invariants (Recommended)
- **Sketch**: Remove `Stopwatch` and `<= ModConstants.MaxRenderTimeMs` from the 3 tests in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` (`lines 84-96`, `407-420`, `663-674`) and `T031b` in `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` (`lines 319-324`). In each test, verify synchronous invalidation (`IsCacheEmpty == true`) or immediate data update, zero redundant color consultations during invalidation, and immediate next-frame rebuild reflecting updated values with `<= 1` consultation per visible tile. Document in XML docs/comments that wall-clock 16.67ms frame timing belongs to Gate 3 (`scripts/verify-scenario.sh`).
- **Appetite**: small (2 test files in Green Zone, ~50 LOC net change).
- **Trade-offs**: removes non-deterministic host-clock timing from unit/property gates while strengthening behavioral "one-frame" assertions (actually proving the next frame rebuilds cleanly). Risk: reviewers might worry FR-010/FR-009 timing coverage dropped — mitigated by explicit XML documentation pointing to Gate 3 + keeping warmed `VisualizationPerformanceTests`.
- **Rabbit holes**: modifying `LivingRoots/` production code or touching unrelated `GrowthPerformanceTests` / `VisualizationPerformanceTests`.

### Option B — Widen timeout threshold or add retries
- **Sketch**: Increase threshold (e.g., 100ms) or retry on failure.
- **Appetite**: small.
- **Trade-offs**: remains environment-sensitive under heavy CI/host load and misrepresents `ModConstants.MaxRenderTimeMs`. Rejected.

### Option C — Fix only `SoilHealthVisualizationTests.cs:419`
- **Sketch**: Remove `Stopwatch` only at line 419.
- **Appetite**: trivial.
- **Trade-offs**: leaves 3 identical un-warmed `Stopwatch <= 16.67ms` hazards (including 50-iteration FsCheck property `T031b`), guaranteeing repeat failures later. Rejected.

## Recommendation

Option A. It cleanly finishes the determinism alignment started by Spec 026, fixes today's `release-gate.sh` exit 14 failure, prevents the remaining 3 hazards, keeps production code untouched, and owes zero Gate 3 work.

## Out of Scope

- Any edits under `LivingRoots/`.
- `VisualizationPerformanceTests.cs`, `GrowthPerformanceTests.cs`, `VisualizationConstantsTests.cs`.
- CI workflow restructuring, dependency bumps, `.trivyignore` renewal, or owner-gated release actions.

## Assumptions to Validate

- Removing `using System.Diagnostics;` leaves no broken references in either file (verified by grep: all 4 `Stopwatch` calls are the sole `System.Diagnostics` usages in those 2 files).
- `ModEntry.Entry` wiring test (`SoilHealthVisualizationTests.cs:360-421`) can render a subsequent frame via `visualizationService.RenderOverlays(StandInSpriteBatch(), AmbientViewport, new GameTime())` and populate the cache (`Assert.False(overlayRenderer.IsCacheEmpty)`), identical to line 401–403.
