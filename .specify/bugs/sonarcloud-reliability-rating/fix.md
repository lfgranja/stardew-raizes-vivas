# Bug Fix: SonarCloud Reliability Rating Failed (C vs A)

- **Slug**: sonarcloud-reliability-rating
- **Fixed**: 2026-10-18
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Isolated the four `BUG`-type issues SonarCloud reported for the `dev` branch (the cause of `C Reliability Rating on New Code`, required ≥ A) via the public SonarCloud API, then removed all four: two exact-float comparisons (S1244) in `OverlayRenderer`, an always-true branch (S2583) in `CompostingBinService`, and a dataflow false positive (S4158) in `ModController`. With zero `BUG` issues left, the reliability rating on new code returns to A.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Services/Visualization/OverlayRenderer.cs` | modified | S1244 ×2 — replaced exact float equality with a new `ApproximatelyEqual` range check in `DataEqual` and `ConfigEqual`. The helper preserves the non-finite sentinels (NaN, ±∞) exactly, which the naive `Math.Abs` range check breaks (see Deviations). |
| `LivingRoots/Services/CompostingBinService.cs` | modified | S2583 — removed the `if (granted)` guard that was always true at that point, and the now-dead `granted` flag; the success path is unconditional after the inventory-full early return. |
| `LivingRoots/Controllers/ModController.cs` | modified | S4158 — added a justified `//NOSONAR` on the flagged `ContainsKey` call (vendor-confirmed false positive). |
| `LivingRoots/ModConstants.cs` | modified | Added `FloatComparisonEpsilon = 0.0001f` so the S1244 tolerance is a named constant, per the project's "no hardcoded values" convention. |
| `LivingRoots.Tests/SoilHealthVisualizationTests.cs` | modified | Added 3 regression tests pinning the epsilon semantics and the non-finite sentinel handling. |

## Diff Highlights

```csharp
// OverlayRenderer.cs — S1244 fix with correct non-finite handling
private static bool ApproximatelyEqual(float left, float right)
{
    if (!float.IsFinite(left) || !float.IsFinite(right))
    {
        return float.IsNaN(left) == float.IsNaN(right)
            && float.IsPositiveInfinity(left) == float.IsPositiveInfinity(right)
            && float.IsNegativeInfinity(left) == float.IsNegativeInfinity(right);
    }

    return Math.Abs(left - right) <= ModConstants.FloatComparisonEpsilon;
}
```

```csharp
// CompostingBinService.cs — S2583: the guard was always true after the early return
if (inventoryFull) { /* ... */ return 0; }

Game1.playSound("Ship");
_monitor.Log($"Bin at ({tile.X}, {tile.Y}): Ready → Empty, produced 1 compost", LogLevel.Trace);
return 1;
```

```csharp
// ModController.cs — S4158 (false positive: the collection is filled by the loop above)
if (!currentMatureCrops.ContainsKey(kvp.Key)) //NOSONAR
```

## SonarCloud issues addressed

| Key | Rule | File:line | Message |
|-----|------|-----------|---------|
| `AaE9aqcK7UwoqUY3hnjW` | csharpsquid:S1244 | `OverlayRenderer.cs:217` | Do not check floating point equality with exact values, use a range instead. |
| `AaE9aqcK7UwoqUY3hnjV` | csharpsquid:S1244 | `OverlayRenderer.cs:240` | Do not check floating point equality with exact values, use a range instead. |
| `AaE9aqfr7UwoqUY3hnjY` | csharpsquid:S2583 | `CompostingBinService.cs:255` | Change this condition so that it does not always evaluate to 'True'. |
| `AaFPigg9XwLrZRhmtpYn` | csharpsquid:S4158 | `ModController.cs:1953` | Remove this call, the collection is known to be empty here. |

Source of truth: `https://sonarcloud.io/api/issues/search?componentKeys=lfgranja_stardew-raizes-vivas&branch=dev&resolved=false&types=BUG` → `total: 4`.

## Tests Added or Updated

- `SoilHealthVisualizationTests.GetOverlays_HealthWithinTolerance_IsCacheHit_BeyondTolerance_IsMiss` — health values within `FloatComparisonEpsilon` reuse the cached overlay list; values beyond it recompute. Fails if the comparison reverts to exact equality.
- `SoilHealthVisualizationTests.GetOverlays_OpacityWithinTolerance_IsCacheHit` — an opacity within tolerance is the same configuration (cache hit).
- `SoilHealthVisualizationTests.GetOverlays_NonFiniteSentinels_PreserveCacheSemantics` — NaN cache-hits against NaN but misses against a finite value. This locks the exact regression the first `Math.Abs`-only attempt introduced (see Deviations).

## Local Verification

- Build: `GamePath=/tmp/opencode/gamepath dotnet build Stardew-LivingRoots.sln --configuration Release` → **0 errors**, 15 pre-existing warnings.
- Format gate: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → **exit 0**.
- Tests (full suite): `dotnet test --no-build -c Release` → **1262 passed, 1 skipped, 4 failed**.
  - The 4 failures are **all pre-existing and unrelated** to this change:
    - 3 persistent (reproduced with my changes stashed on the same commit): `SecuritySanitizationInvariantsTests.UnicodeNormalizer_NormalizesFullwidthCharacters`, `ProductionSourceGuardTests.FalseComment_InvalidDropInShake_MatchesCode`, `ProductionSourceGuardTests.ProductionSources_NoUnreferencedConstantsSurvive` (the latter two fail with `Constants.cs not found`).
    - 1 intermittent FsCheck property test (`T031b_OverlayDistinguishable_DistinctHealth_DistinctColors` or `T222_GetOverlays_HealthZero_RendersZeroOverlays`), each of which touches `ColorInterpolationService` / skips the changed comparison path. The T031b counterexample `(87.47000122f, 70.93000031f)` reproduces a real pre-existing bug: `GetCategoryForHealth` has a gap at (70, 71) that returns `Unknown`, while `GetColorForHealth` maps the same value to the Healthy color.
- Tests (affected classes, isolated): `SoilHealthVisualizationTests` **18/18**, `CompostingBinServiceTests` **47/47**, visualization + compost invariants **15/15**.
- The original regression this change first introduced (`T143_OverlayDrawableOnly_…`) fails 5/5 before the `ApproximatelyEqual` helper and passes after it.

## Deviations from Assessment

1. **Root cause was four specific Sonar issues, not the speculative reliability themes in the assessment.** The assessment guessed `ModController.cs`, `SoilHealthService.cs` and `VisualizationService.cs` and proposed rollback/persistence reliability work. Consulting the SonarCloud API (`types=BUG`) gave the exact, complete set: `ModController.cs:1953`, `CompostingBinService.cs:255`, `OverlayRenderer.cs:217`, `OverlayRenderer.cs:240`. `SoilHealthService.cs` and `VisualizationService.cs` are **not** implicated. Scope was expanded to `CompostingBinService.cs` and `OverlayRenderer.cs` (newly discovered evidence), and narrowed away from `SoilHealthService.cs`/`VisualizationService.cs`.
2. **S4158 is a false positive, fixed by suppression rather than a code change.** SonarSource's own maintainer documents that the rule's dataflow engine cannot see collections populated inside a `foreach` loop and calls it a known false positive (`sonar-dotnet#1042`, duplicate of `#767`). `currentMatureCrops` *is* populated by the preceding loop, so there is no code change that removes the finding without contorting correct code. The chosen remediation is a line-scoped `//NOSONAR` with the reason inline. The vendor-recommended alternative — marking it "Won't Fix / False Positive" in the SonarCloud UI — is not reproducible from the repository, so the in-repo comment is the durable mechanism.
3. **First S1244 attempt introduced a NaN regression and was corrected.** A bare `Math.Abs(a - b) > epsilon` check treats `NaN` as equal to everything (`Math.Abs(NaN - x)` is `NaN`, and every `NaN` comparison is false), so a NaN "Unknown" frame would cache-hit against a finite frame and redraw a stale overlay. This was caught by the existing `T143` test (failed 5/5). The final `ApproximatelyEqual` helper handles the non-finite sentinels explicitly and `T143` passes.
4. **Added `ModConstants.FloatComparisonEpsilon` (a fourth source file).** The project convention forbids hardcoded values; the existing inline `0.0001f` usages (in `ModController`/`SoilHealthService`) were left untouched to keep the diff scoped.

## Follow-ups

- **Pre-existing flaky property tests (out of scope).** `T031b_OverlayDistinguishable_DistinctHealth_DistinctColors` intermittently fails on a real `ColorInterpolationService` bug: the category thresholds `[0,30] / [31,70] / [71,100]` leave the gap `(70, 71)` returning `Unknown`, while `GetColorForHealth` returns the Healthy color for the same value. `T222_GetOverlays_HealthZero_RendersZeroOverlays` intermittently fails when FsCheck generates an edge-case float for the sibling tile. Both predate this change and should be filed as their own bug.
- **Pre-existing persistent test failures (out of scope).** `UnicodeNormalizer_NormalizesFullwidthCharacters` (expects `"ABC"`, gets `"ＡＢＣ"`) and the two `ProductionSourceGuardTests` that hard-fail with `Constants.cs not found at …/LivingRoots/Constants.cs`.
- **Optional:** migrate the four inline `0.0001f` float comparisons in `ModController.cs`/`SoilHealthService.cs` to `ModConstants.FloatComparisonEpsilon` so the tolerance has one definition.
- **Optional:** if the team prefers no suppressions, replace the `//NOSONAR` with a "False Positive" marking in the SonarCloud UI and remove the comment.
