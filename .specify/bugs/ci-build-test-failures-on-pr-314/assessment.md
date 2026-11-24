# Bug Assessment: CI build/test failures on PR #314 (run 39952935432)

- **Slug**: ci-build-test-failures-on-pr-314
- **Created**: 2026-10-28
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/39952935432/job/120411617581?pr=314
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

GitHub Actions "Build and Test" job failed with 128 errors compiling tests due to missing members/constants/methods introduced by recent changes or tests expecting APIs that don't exist in current codebase.

## Symptom

Tests fail to compile: references to `ModConstants.SoilGrowthPoorMultiplier`, `ModConstants.SoilGrowthHealthyMultiplier`, `ModConstants.SoilQualityHealthyBoost`, `ModConstants.SoilExtraHarvestHealthyBoost` missing; `ModController.ComputeHarvestQualityRoll` / `ComputeExtraHarvestProb` missing; `OverlayRenderer` constructor expects `IHudNotifier`; `CompostingBinService.OnBinPlaced` signature changed; `VisualizationService` constructor expects `IHudNotifier`; `CheckDegradation` called as static incorrectly in tests.

## Reproduction

1. Run CI on PR #314 (or locally `dotnet build Stardew-LivingRoots.sln --configuration Release` with tests)
2. Build fails during test project compilation

## Suspected Code Paths

- `LivingRoots/ModConstants.cs` - missing constants
- `LivingRoots/Controllers/ModController.cs` - missing methods
- `LivingRoots/Services/Visualization/OverlayRenderer.cs`, `VisualizationService.cs` - constructor changes
- `LivingRoots/Services/CompostingBinService.cs` - API changes
- Tests expecting old APIs

## Root Cause Hypothesis

Tests were added/modified expecting newer API surface (growth/quality/extra harvest helpers, hud notifier injection) but implementation wasn't updated to match, or vice versa. The changes in this PR (verification runner) may have triggered test compilation issues or tests are out of sync with current implementation.

Confidence: medium

## Proposed Remediation

**Preferred**: Update tests to match current API contracts, or add missing members to implementation if they are required by intended design. Need to inspect current state of ModConstants/ModController to see if these were removed or renamed.

**Files likely to change**:
- `LivingRoots/ModConstants.cs` (add missing constants if needed)
- `LivingRoots/Controllers/ModController.cs` (add missing methods if needed)
- Tests in `LivingRoots.Tests/Invariants/*`, `LivingRoots.Tests/Visualization/*`, `LivingRoots.Tests/SoilGrowthHelperTests.cs`

**Tests to add or update**: Fix compilation errors in tests.

## Risks & Considerations

- May need to understand intended design - check git history for these members
- Could break intended behavior if adding back removed functionality

## Open Questions

- [NEEDS CLARIFICATION: Were these members removed intentionally? Check recent changes.]
