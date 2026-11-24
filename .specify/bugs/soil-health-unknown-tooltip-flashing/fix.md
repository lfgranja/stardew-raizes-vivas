# Bug Fix: Soil Health tooltip shows "Unknown" and flashes constantly on all cursor positions

- **Slug**: soil-health-unknown-tooltip-flashing
- **Fixed**: 2026-11-07
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Fixed tooltip applicability and FR-015 throttle/render interaction in `TooltipRenderer`: cursor tiles absent from the visualization snapshot now return `null` (no tooltip on non-applicable/non-HoeDirt tiles), while applicable tiles present with `NaN`/`Infinity` still render `Soil Health: Unknown` (FR-014). Throttled same-tile frames now return a cached tooltip with the current cursor position instead of `null`, eliminating the draw/hide flicker while preserving recomputation throttling.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Services/Visualization/TooltipRenderer.cs` | modified | Added `PerScreen` cached tooltip; missing snapshot key → `null` + cache clear; NaN/Infinity present → unknown tooltip; throttle returns cached tooltip (position updated) instead of hiding; cache cleared on tooltips disabled/negative cursor. |
| `LivingRoots.Tests/Visualization/TooltipRendererTests.cs` | modified | Reframed missing-key test to expect `null` (non-applicable); added present-NaN unknown test (FR-014); changed throttle test to expect cached stable tooltip (`Same`, position updated, text intact). |
| `LivingRoots.Tests/Visualization/VisualizationServiceTooltipTests.cs` | modified | Seeded applicable NaN snapshot for unknown-state path; added regression test asserting absent snapshot tile does not render or log. |

## Diff Highlights (optional)

- Applicability gate: `if (!tileHealthData.TryGetValue(cursorTile, out var healthValue)) { _cachedTooltip.Value = null; return null; }` — missing keys are out of scope, not unknown data.
- Stable throttle: on same-tile throttle hit, returns cached tooltip and updates `Data.Position`; only recomputation/logging is throttled.
- Cache is `PerScreen<(Point Tile, TooltipData Data)?>` and cleared on disable, negative cursor, and inapplicable tile.

## Tests Added or Updated

- `LivingRoots.Tests.Visualization.TooltipRendererTests.GetTooltip_TileAbsentFromData_ReturnsNull` — non-applicable cursor tile absent from snapshot must not show a tooltip.
- `LivingRoots.Tests.Visualization.TooltipRendererTests.GetTooltip_TilePresentWithNaN_ReturnsUnknownTextWithoutPercentage` — applicable tile with unavailable data (FR-014) remains `Soil Health: Unknown` without `%`.
- `LivingRoots.Tests.Visualization.TooltipRendererTests.GetTooltip_SameTileWithinDebounceWindow_ReturnsCachedTooltip` — FR-015 throttle no longer hides a valid tooltip; cached instance returned with current position.
- `LivingRoots.Tests.Visualization.VisualizationServiceTooltipTests.RenderTooltip_TileAbsentFromSnapshot_DoesNotRenderOrLog` — end-to-end service path for non-applicable snapshot absence produces no tooltip/log.

## Local Verification

- Commands run: `GamePath=/home/luis/stardew-assemblies dotnet test --configuration Release --filter "FullyQualifiedName~TooltipRendererTests|FullyQualifiedName~VisualizationServiceTooltipTests" --nologo` → **Passed: 11, Failed: 0, Skipped: 0** (LivingRoots.Tests.dll, net6.0). Fresh run in this session (2026-11-07).
- Commands run: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → no formatting changes required (this session).
- Manual checks: not performed in a real SMAPI game session (see `test.md` / residual risks).

## Deviations from Assessment

- **HoeDirt initialization semantics (assessment point 3, medium-confidence): not changed.** `SoilHealthService.GetSoilHealth()` still returns `NaN` for missing records and HoeDirt tiles without an initialized soil-health record still resolve to `Soil Health: Unknown` when present in the visualization snapshot. The assessment flagged this as requiring product/spec clarification ("Unknown until measured" vs default baseline vs initialization event). This fix addresses the reported scope bug ("Unknown everywhere, even when not applicable") and flashing; it does **not** change when/if newly tilled/watered HoeDirt receives a measured baseline. Follow-up decision needed before altering soil-health semantics (Constitution VI / sparse-cache invariants apply).
- **Throttle behavior:** assessment allowed cache in `TooltipRenderer` or `VisualizationService`; implemented in `TooltipRenderer` with `PerScreen` state, consistent with existing per-screen throttle state and SMAPI split-screen safety.

## Follow-ups

- Clarify intended baseline for newly tilled HoeDirt (assessment open question 1) and, if needed, implement host-authoritative initialization with tests.
- Run Gate 3 / real SMAPI verification for this visualization fix (runnable code): confirm no flashing on non-applicable tiles and stable tooltip on applicable tiles, per `VERIFICATION_PROTOCOL.md` / project gates.
- Consider whether `UpdateTileHealthData`/snapshot semantics need explicit "unknown applicable tile" vs "inapplicable tile" documentation to prevent regression.
