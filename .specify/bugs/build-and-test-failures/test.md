# Bug Verification: "Build and Test" red — 3 failing tests fixed

- **Slug**: build-and-test-failures
- **Tested**: 2026-10-19
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The three reported failures no longer reproduce, and the `Build and Test` workflow is **green on `dev`**. The fix was delivered via PR #285 (`fix/build-and-test-failures` → `dev`, merge commit `8f30f22`); all four checks on the merge commit report `success`, including `Build and Test`. One earlier CI run flaked on the pre-existing `T031b` property test (unrelated to this fix) and passed on re-run — see Residual Risks.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — the 3 reported failures | `dotnet test --filter "FullyQualifiedName~UnicodeNormalizer|FullyQualifiedName~ProductionSourceGuardTests"` | **pass** | 47/47 (was 3 failing). |
| New / updated tests | same run (2 added normalizer tests) | **pass** | `UnicodeNormalizer_NormalizesFullwidthDigitsAndPunctuation`, `UnicodeNormalizer_PreservesNonLatinScript`. |
| Regression suite | `dotnet test Stardew-LivingRoots.sln --no-build -c Release` | **pass** | 1268 passed, 1 skipped, 0 failed. |
| Build | `dotnet build -c Release` | **pass** | 0 errors, 15 pre-existing warnings. |
| Lint / format | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** | exit 0. |
| **CI reproduction — `Build and Test` on the PR** | PR #285 check runs | **pass** | First run failed on the flaky `T031b` (1267 passed / 1 failed — the 3 reported tests passed); re-run: `success`. |
| **CI reproduction — `Build and Test` on `dev`** | `gh api …/commits/8f30f22/check-runs` | **pass** | Merge commit `8f30f22`: `Build and Test: success`, `Check Formatting: success`, `Check Protected Paths: success`, `SonarCloud Code Analysis: success`. |

## Output Excerpts

PR #285 first CI run (the 3 reported tests pass; only the pre-existing flaky test fails):

```
Total tests: 1269   Passed: 1267   Failed: 1
[FAIL] LivingRoots.Tests.Invariants.VisualizationInvariantsTests.T031b_OverlayDistinguishable_DistinctHealth_DistinctColors
```

`dev` merge commit `8f30f22` check runs:

```
SonarCloud Code Analysis: completed success
Check Protected Paths:    completed success
Build and Test:           completed success
Check Formatting:         completed success
```

Local full suite:

```
Aprovado! – Com falha: 0, Aprovado: 1268, Ignorado: 1, Total: 1269
```

## Residual Risks

- **Pre-existing flaky property tests remain.** The first CI run on PR #285 failed on `T031b_OverlayDistinguishable_DistinctHealth_DistinctColors` (a real `ColorInterpolationService` category gap at `(70, 71)`: `GetCategoryForHealth` returns `Unknown` while `GetColorForHealth` returns the Healthy colour) and passed on re-run; `T222_GetOverlays_HealthZero_RendersZeroOverlays` (FsCheck edge-case floats) can flake similarly. Neither is caused by this fix, but together they mean `Build and Test` can still go red intermittently (~1 in 3 full-suite runs observed). **This is the last blocker to a reliably green CI and should be filed as its own bug.**
- The `MaxTilledTiles` wiring is a production change in the crop-growth path (output-preserving by construction, but not exercised by a live-game run).
- 11 `CS8602` warnings in `LivingRoots.Tests/Invariants/SoilHealthPersistenceInvariantsTests.cs` remain (pre-existing).

## Recommendation

**Close the bug — verified end-to-end.** The three reported failures are fixed and `Build and Test` is green on `dev` (`8f30f22`). File a separate bug for the flaky property tests (`T031b`, `T222`) so the workflow stops flaking red; until then, a red `Build and Test` may be that flakiness rather than a regression.
