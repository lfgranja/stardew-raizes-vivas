# Bug Fix: PR #518 CI test failures — residual out-of-range health contract

- **Slug**: `ci-test-failures-pr-518`
- **Fixed**: 2026-10-29T04:04:21Z
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The assessment's bulk remediation had already landed locally in commit `16f43c0`: the 83 failing tests from run `40793022307` were reconciled against the branch's governing contracts, and the suite was green. This pass verified that state and closed the assessment's one explicit residual. Finite out-of-range health now clamps to the boundary category in `HealthCategoryExtensions.FromHealthValue`, matching `ColorInterpolationService.GetCategoryForHealth`, the spec 009 data-model clamp rule, and FR-008/SC-007. Documentation, invariant tests, and a cross-resolver parity test were updated so the two APIs cannot silently diverge again.

## Pre-existing Remediation Verified (commit `16f43c0`)

The assessment's preferred remediation was already implemented before this command. Verified areas:

| Area | Landed change |
|------|---------------|
| Soil health | `34/67` bands; measured `0` retained and rendered Poor; unset tiles read `0` but stay undrawable; NaN/Infinity normalization; load/reload isolation. |
| Visualization | Epsilon-based overlay cache and snapshot contracts; drawable-only reporting; palette generation invalidation; logging expectations. |
| Compost bin | Building-footprint placement refusal; pickup to the acting farmer; batch `modData` preservation; fail-closed absent-lock behavior; state-machine lock discipline. |
| Validation / wiring | Validator-owned compostable set; `ModEntry.Entry` fail-soft with sanitized logging; obsolete `CompostingBinData` fields marked `[JsonIgnore]`. |
| Guards / assets / i18n | Corrected `Constants.cs` source paths and comments; `**/` glob translation; fullwidth Unicode normalization; translated degraded-accessibility text; multiplayer parity allow-list. |

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Domain/Visualization/HealthCategory.cs` | modified | Finite values clamp to `[MinSoilHealth, MaxSoilHealth]` before band resolution; NaN/Infinity remain `Unknown`. XML docs now state the clamping contract. |
| `LivingRoots.Tests/Visualization/HealthCategoryTests.cs` | modified | Out-of-range theory renamed and re-pointed at boundary categories (`<0 → Poor`, `>100 → Healthy`). |
| `LivingRoots.Tests/Invariants/SoilHealthCategoryInvariantsTests.cs` | modified | Stale `30/70` class summary corrected to `34/67`; out-of-bounds property expectations now assert clamping. |
| `LivingRoots.Tests/Visualization/ColorInterpolationServiceTests.cs` | modified | Added a 13-case theory asserting `GetCategoryForHealth` and `FromHealthValue` agree across in-range, boundary, out-of-range, NaN, and Infinity inputs. |
| `LivingRoots/Domain/Visualization/AGENTS.md` | modified | Replaced the stale `30/70` band table with the governing spec 009 `34/67` scale and the finite-clamp rule. Scope expansion, documented below. |

## Diff Highlights

The two category resolvers now share one rule — only non-finite values are `Unknown`:

```csharp
if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
{
    return HealthCategory.Unknown;
}

var clamped = Math.Clamp(
    healthValue,
    ModConstants.MinSoilHealth,
    ModConstants.MaxSoilHealth);

if (clamped < ModConstants.PoorModerateBandEdge)
{
    return HealthCategory.Poor;
}

if (clamped < ModConstants.ModerateHealthyBandEdge)
{
    return HealthCategory.Moderate;
}

return HealthCategory.Healthy;
```

The cross-resolver test pins the single contract:

```csharp
[Theory]
[InlineData(-100f)]
[InlineData(-1f)]
[InlineData(0f)]
[InlineData(33f)]
[InlineData(34f)]
[InlineData(66f)]
[InlineData(67f)]
[InlineData(100f)]
[InlineData(101f)]
[InlineData(1000f)]
[InlineData(float.NaN)]
[InlineData(float.PositiveInfinity)]
[InlineData(float.NegativeInfinity)]
public void GetCategoryForHealth_AgreesWithHealthCategoryExtensions(float healthValue)
{
    var fromService = _service.GetCategoryForHealth(healthValue);
    var fromExtension = HealthCategoryExtensions.FromHealthValue(healthValue);
    Assert.Equal(fromExtension, fromService);
}
```

## Tests Added or Updated

- `ColorInterpolationServiceTests.GetCategoryForHealth_AgreesWithHealthCategoryExtensions` — new 13-case theory locking the shared out-of-range contract between the visualization resolver and the domain helper.
- `HealthCategoryTests.FromHealthValue_FiniteOutOfRange_ClampsToBoundaryCategory` — replaces the old `ReturnsUnknown` theory; asserts `-1/-100 → Poor` and `101/1000 → Healthy`.
- `SoilHealthCategoryInvariantsTests.FromHealthValue_OutOfBounds_ClampsToBoundaryCategory` — property-style boundary table updated to the same expectations.
- Existing `ColorInterpolationServiceTests.GetCategoryForHealth_NegativeValue_ClampsToPoor` and `..._ValueAboveHundred_ClampsToHealthy` remain unchanged and now describe the same rule as the helper.

## Local Verification

- Commands run: `dotnet build Stardew-LivingRoots.sln --configuration Release --nologo` → **0 errors**, 17 warnings (pre-existing nullable warnings in `SoilHealthPersistenceInvariantsTests.cs`).
- Targeted: `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build --configuration Release --nologo --filter "FullyQualifiedName~HealthCategory|FullyQualifiedName~ColorInterpolationService"` → **77 passed, 0 failed**.
- Full: `dotnet test Stardew-LivingRoots.sln --no-build --configuration Release --nologo` → **1436 passed, 4 skipped, 0 failed, 1440 total**, 1 m 4 s.
- Format: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → **exit 0**.
- Baseline before this pass (commit `16f43c0`): **1423 passed, 4 skipped, 0 failed, 1427 total**; the delta is the 13 new cross-resolver cases.
- Manual checks: none. No game session or Gate 3 claim is made.

## Deviations from Assessment

1. **The assessment's preferred remediation was already applied in `16f43c0`.** This command verified the landed reconciliation and did not re-implement it. Only the assessment's explicit residual (the out-of-range contract) required new code.
2. **Chose clamping over `Unknown` for finite out-of-range values.** The spec 009 data-model states "HealthValue clamped to [0, 100]" and limits `Unknown` to NaN/Infinity; `ColorInterpolationService` already clamped, and its tests assert that behavior. Unifying the helper on clamping avoids changing the visualization contract and leaves only one out-of-range rule.
3. **Scope expansion: `LivingRoots/Domain/Visualization/AGENTS.md`.** The file still published the superseded `30/70` band table, directly contradicting the `34/67` code and spec 009. It was updated as part of the same documentation-drift class. It was not in the assessment's files list.
4. **No production behavior change in `ColorInterpolationService`.** Its clamping behavior was already correct; the implementation change is confined to the domain helper.
5. **Did not rewrite spec 005 planning artifacts.** Several `specs/005-soil-health-to-crop-growth/` files still mention the superseded `30/70` scale. They are historical planning records; spec 009 and `LivingRoots/Domain/AGENTS.md` declare the supersession. Rewriting them is out of scope for this bug.

## Follow-ups

- Push and trigger a fresh `Build and Test` run for PR #518. The local suite is green, but the assessment explicitly requires CI evidence before calling the failure resolved.
- Run `/speckit.bug.test slug=ci-test-failures-pr-518` to regenerate verification evidence. The old `.specify/bugs/ci-build-test-failures-on-pr-314/test.md` is stale and must not be reused.
- Run the manual Gate 3 protocol if gameplay claims are needed for bin placement/pickup, zero-health rendering, or fail-closed locks; unit tests alone are not Gate 3 evidence.
- Keep `Build and Test` advisory until it produces one genuinely green CI run; only then consider required-status promotion.
- Consider making `HealthCategoryExtensions.FromHealthValue` internal, or routing production callers through it, so the two category resolvers cannot drift again. It currently has no production callers.
