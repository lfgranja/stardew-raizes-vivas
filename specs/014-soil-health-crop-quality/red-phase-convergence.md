# Spec 014 Phase 9 — RED Convergence Gate (T117)

**Date**: 2026-11-05
**RED commit range**: `2686e10..4a6cf79` (T111–T116 test commits)
**Build command**: `GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln --configuration Release` (success, 0 errors)
**Test command**: `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release`
**Result**: 50 failed, 1691 passed, 4 ignored (1745 total). All 50 failures are in the new T111–T116 tests or the narrowed guard/parity tests and fail for the intended reasons below.

## RED evidence by task

- **T111** `LivingRoots.Tests/HarvestQuality/HarvestQualityMathTests.cs` — `HarvestQualityMath.ComputeBaselineProbability/ComputeSilverProbability/ComputeIridiumProbability` throw `NotImplementedException`; `BoostQualityProbability` does not clamp to `[0,1]` for Healthy boosts near the upper bound.
- **T112** `LivingRoots.Tests/HarvestQuality/HarvestFeedbackPlanTests.cs` — `IHarvestQualityService.BuildFeedbackPlan` throws `NotImplementedException`; extra-only harvests currently collapse into `LivingQuality` because `ResolveFeedback` conflates the two flags.
- **T113** `LivingRoots.Tests/HarvestQuality/HarvestInventoryTargetingTests.cs` — `HarvestInventoryDelta.Snapshot/Diff/FindPrimaryAddedSlot/IsFull/SelectTargetInventory` throw `NotImplementedException`; blocked harvests have no primary-slot fallback yet.
- **T114** `LivingRoots.Tests/HarvestQuality/SoilHealthSyncTests.cs` and `LivingRoots.Tests/Verification/CrossModCommunicationGuardTests.cs` — `SoilHealthSync.Create/Apply` throw `NotImplementedException`; `LivingRoots` has no host-scoped `SendMessage`/`ModMessageReceived` soil-health sync channel.
- **T115** `LivingRoots.Tests/CompostingBinServiceTests.cs` + `LivingRoots.Tests/Fixtures/RecordingPlayerInventory.cs` — `CollectCompost` ignores the capacity dry-run and the injectable `outputCount` provider; partial adds are not rolled back; `CompostingBinStateData` has no durable `OutputGranted` field.
- **T116** `LivingRoots.Tests/Verification/HealthTierClassifierTests.cs` and `scripts/tests/test-health-tier-parity.sh` — `HealthTierClassifier` maps measured `0` to `nonParticipant` and uses 34/67 edges; `scripts/lib/verdict.sh` mirrors 34/67 and `v <= 0 -> nonParticipant`.

## Gate outcome

T117 is satisfied: every T111–T116 test fails for the intended reason. No task T118+ may begin until this gate is recorded; implementation wave starts now.
