# Bug Fix: SonarCloud S2699 — test method with no assertion (SoilHealthServiceContractTests)

- **Slug**: contract-test-missing-assertion
- **Fixed**: 2026-11-01T04:45:00Z
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Added a single assertion to `OnTileHealthChanged_DoesNotFireWhenNoSubscriber` in `SoilHealthServiceContractTests.cs` verifying that `SetSoilHealth(50)` followed by `UpdateHealth(+10)` on a service with no subscriber yields health `60`. This satisfies SonarCloud rule `csharpsquid:S2699` (at least one assertion per test) and strengthens the test to catch regressions in the no-subscriber mutation path, while preserving its original intent (no throw when no subscriber is registered).

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots.Tests/ContractTests/SoilHealthServiceContractTests.cs` | modified | Added `Assert.Equal(60f, service.GetSoilHealth("Farm", tile), 5);` to `OnTileHealthChanged_DoesNotFireWhenNoSubscriber` |

## Diff Highlights

```diff
             service.SetSoilHealth("Farm", tile, 50f);
             service.UpdateHealth("Farm", tile, 10f);
+            Assert.Equal(60f, service.GetSoilHealth("Farm", tile), 5);
         }
```

## Tests Added or Updated

- `LivingRoots.Tests/ContractTests/SoilHealthServiceContractTests::OnTileHealthChanged_DoesNotFireWhenNoSubscriber` — now asserts the resulting health value (60f), proving the no-subscriber mutation path runs to completion and mutates state.

## Local Verification

- Commands run: `dotnet build LivingRoots.Tests/LivingRoots.Tests.csproj` → build succeeded (0 errors).
- Commands run: `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build --filter "FullyQualifiedName~SoilHealthServiceContractTests"` → 13 passed, 0 failed.
- Commands run: `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build` → 1443 passed, 4 skipped, 0 failed (no regressions).

## Deviations from Assessment

None — the applied change matches the assessment's preferred remediation exactly (added `Assert.Equal(60f, service.GetSoilHealth("Farm", tile), 5);`).

## Follow-ups

- Re-run SonarCloud analysis to confirm issue `AaFPP3NvEHNTGUcquNLc` resolves to `CLOSED`.
- Consider applying the same assertion pattern to any other test methods flagged by S2699 in the backlog.