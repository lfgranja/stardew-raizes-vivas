# Bug Fix: flaky property tests (T031b + T222)

- **Slug**: flaky-property-tests
- **Fixed**: 2026-10-19
- **Assessment**: ./assessment.md
- **Status**: applied

> Supersedes the earlier `not-applied` report for this slug (kept in git history). The prior attempt was halted because the first assessment's T031b premise was disproven; this report implements the corrected re-assessment.

## Summary

Corrected the two flaky property tests to match the code's actual behaviour: **T031b**'s property wrongly required distinct tints across categories, which the continuous gradient + flat Healthy tier (`T253/FR-010`) never guarantees — so the property was fixed (cross-tier distinctness dropped, same-tier gradient kept) and its generator constrained to integer health; **T222**'s sibling generator was constrained to positive health so it can no longer emit `0`. No production code changed.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` | modified | T031b: property corrected; integer generator added; deterministic seam `[Theory]` + flat-Healthy `[Fact]` added; dead `IsMeasurable` helper removed. |
| `LivingRoots.Tests/Visualization/OverlayRendererTests.cs` | modified | T222: property now uses the existing `PositiveHealthGenerator()` via `Prop.ForAll`. |

## Diff Highlights

```csharp
// T031b — the property no longer requires cross-tier distinctness
if (categoryA != categoryB)
{
    return true; // continuous palette + flat Healthy tier (T253/FR-010)
}
...
return colorService.GetColorForHealth(healthA) != colorService.GetColorForHealth(healthB);
```

```csharp
// T222 — the sibling is positive (1-100), so it can never be the skipped 0
[Property(MaxTest = 100)]
public Property T222_GetOverlays_HealthZero_RendersZeroOverlays()
{
    return Prop.ForAll(
        Arb.From(PositiveHealthGenerator()),
        tileHealth => ZeroTile_RendersZeroOverlays(tileHealth));
}
```

## Tests Added or Updated

- `VisualizationInvariantsTests.T031b_OverlayDistinguishable_DistinctHealth_DistinctColors` — rewritten: same-tier gradient distinctness only, integer health `0–100`.
- `VisualizationInvariantsTests.T031b_PaletteSeam_SharesTheEndpointColour(30,31)` and `(70,71)` — pins that the tier seams share the endpoint colour (categories differ, colours equal) so the behaviour is intentional.
- `VisualizationInvariantsTests.T031b_HealthyTier_IsFlat` — pins that `GetColorForHealth(71) == GetColorForHealth(100)`.
- `OverlayRendererTests.T222_GetOverlays_HealthZero_RendersZeroOverlays` — now generates strictly positive health.

## Local Verification

- Build: `GamePath=… dotnet build Stardew-LivingRoots.sln -c Release` → **0 errors**.
- Targeted: `dotnet test --filter "…VisualizationInvariantsTests|…OverlayRendererTests"` → **27/27 passed**.
- Regression: full suite run **5 consecutive times** → `1271 passed, 1 skipped, 0 failed` each time (the prior flakiness was ~1 in 3).
- Format gate: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → **exit 0**.
- Manual: confirmed against the built assembly that the 29 violating integer pairs `(70, k)`, `k ∈ 72..100`, are now exempt (cross-tier) and that same-tier gradient pairs still differ.

## Deviations from Assessment

- **Scope note (small expansion):** the dead private helper `IsMeasurable` in `VisualizationInvariantsTests` was removed. It was used only by the old T031b guard; with the integer-domain generator the guard is unnecessary, and leaving the helper would trip Sonar `S1144` (unused private member). Not listed in the assessment's "Files likely to change", but within the same file and required to avoid a new static-analysis issue.
- Otherwise the fix follows the assessment's **Preferred** remediation exactly (T031b: drop cross-tier clause + integer generator; T222: positive generator). The two `[NEEDS CLARIFICATION]` items were resolved in the assessment's Clarifications (keep the continuous/flat palette; leave the category gaps as-is) before this fix ran.

## Follow-ups

- The non-integer category gaps `(30,31)`/`(70,71)` remain (intentionally, per Clarifications); harmless for integer health.
- `Build and Test` should now stay green on pushes (verify in `/speckit.bug.test`).
- No production code changed, so no runtime/behaviour risk beyond the test suite.
