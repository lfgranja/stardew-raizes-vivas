# Bug Assessment: Soil Health tooltip shows "Unknown" and flashes constantly on all cursor positions

- **Slug**: soil-health-unknown-tooltip-flashing
- **Created**: 2026-11-06
- **Source**: pasted text (user report)
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

> "Soil Health: Unknown" - keeps flashing FOREVER constantly under cursor tooltip, even when it's not applyable (no tillable soil under cursor).
> It never showed anything different from "Soil Health: Unknown", even when tilled and watered soil.

## Symptom

When visualization tooltips are enabled, the cursor tooltip displays `Soil Health: Unknown` and appears to flash/flicker continuously wherever the cursor moves, including over tiles where soil-health information is not applicable (non-tilled ground, paths, grass, etc.). On tilled/watered `HoeDirt` tiles that have no stored soil-health record, the tooltip also remains `Soil Health: Unknown` indefinitely, never showing a percentage/category.

**Observed behavior:** persistent flashing `Soil Health: Unknown` tooltip on inapplicable tiles and on tilled tiles with no data.
**Expected behavior (per spec intent):** no soil-health tooltip on non-tilled/non-applicable tiles (`specs/001-soil-health-visualization/research-non-tilled-tooltip.md`; FR-002 scopes tooltips to tilled tiles). For tilled tiles with unavailable data, show a stable `Soil Health: Unknown` tooltip without flashing (FR-014). For tilled tiles with known/measured health, show `Soil Health: {percentage}% ({category})`.

## Reproduction

1. Load a save with the Living Roots mod and visualization tooltips enabled (default config enables tooltips).
2. Move the cursor across the farm over non-tilled tiles (grass, path, flooring, empty ground, etc.).
3. Observe the tooltip under the cursor: it shows `Soil Health: Unknown` and flickers/flashes repeatedly.
4. Till soil (and water it, as reported) so a `HoeDirt` tile exists with no soil-health record created by compost/decay initialization.
5. Hover that tilled/watered tile.
6. Observe the tooltip remains `Soil Health: Unknown` (no percentage/category), and still flashes.

[NEEDS CLARIFICATION: exact in-game conditions where a `HoeDirt` tile should receive an initial measured soil-health value (tilling hook, default baseline, or only after compost/decay interaction). The report states "tilled and watered soil"; watering alone is not modeled as a soil-health mutation in the current services.]

## Suspected Code Paths

- `LivingRoots/Services/Visualization/TooltipRenderer.cs:70-131` — `GetTooltip()` converts a missing `tileHealthData` key to `float.NaN` (`:99`) and returns `CreateUnknownTooltip()` for NaN/Infinity (`:101-104`). It does **not** return `null` when the cursor tile is absent from the visualization snapshot. Result: any cursor tile not present in `tileHealthData` is rendered as "unknown" instead of "not applicable / no tooltip".
- `LivingRoots/Services/Visualization/TooltipRenderer.cs:85-97` — FR-015 per-tile throttle records `lastUpdateTimeByTile[cursorTile]` on every non-throttled call and returns `null` while inside the 50ms window (`ModConstants.TooltipDebounceMs`). Because `VisualizationService.RenderTooltip()` draws only when `GetTooltip()` returns non-null (`VisualizationService.cs:246-252`), the tooltip is suppressed on throttled frames and redrawn after the window expires, producing visible flicker/flashing while the cursor remains over the same tile.
- `LivingRoots/Controllers/ModController.cs:1569-1588` (`UpdateTileHealthData`) — builds the per-frame visualization snapshot from `location.terrainFeatures.Pairs`, but only for `HoeDirt` tiles (`:1578-1582`). For each `HoeDirt`, it stores `_soilHealthService.GetSoilHealth(...)` (`:1584-1585`). Tiles that are not `HoeDirt` terrain features are never added to the snapshot; combined with `TooltipRenderer`'s missing-key => NaN behavior, this makes the tooltip show "Unknown" on non-applicable tiles.
- `LivingRoots/Services/SoilHealthService.cs:514-543` (`GetSoilHealth`) — returns `float.NaN` when no record exists for a tile (T135/FR-014, `:535-539`). `TryGetSoilHealth` (`:545-563`) distinguishes missing entries from stored values. A `HoeDirt` tile with no initialized soil-health record therefore appears as NaN in the visualization snapshot, yielding `Soil Health: Unknown` in the tooltip (and an unknown overlay path in `OverlayRenderer`, `OverlayRenderer.cs:754-762`). The report's "tilled and watered soil never showed anything different" is consistent with no soil-health record being created for those tiles.
- `LivingRoots/Controllers/ModController.cs:1740-1793` (`OnRendered`, screen-space) — calls `UpdateCursorTile(...)` and `RenderTooltip(...)` every frame (`:1773-1777`), so throttle behavior and scope mistakes are visible continuously.
- `LivingRoots/Services/Visualization/VisualizationService.cs:231-253` — `RenderTooltip()` snapshots tile data, delegates to `TooltipRenderer.GetTooltip()`, and draws only when data is non-null; no cached/sticky tooltip is retained across throttled frames.
- `LivingRoots/Services/Visualization/OverlayRenderer.cs:754-762` — renders the configured unknown overlay for NaN/Infinity values. If HoeDirt tiles are pushed as NaN (no record), overlays will show unknown state; if non-HoeDirt terrain features were ever included, they would also render unknown. Current controller filters to `HoeDirt` only, but the tooltip path is not equivalently scoped.
- Spec/research references showing intended behavior divergence:
  - `specs/001-soil-health-visualization/research-non-tilled-tooltip.md:11,281` — states non-tilled tiles should not show a soil-health tooltip and claims dictionary lookup excludes them; current `TooltipRenderer` no longer does this.
  - `specs/001-soil-health-visualization/research-cache-vs-unknown.md:383-384` — expected `TooltipRenderer` to return `null` for tiles not in the dictionary while tilled tiles with no cache entry are represented as `NaN` in the visualization dictionary.

## Root Cause Hypothesis

**Confidence: high (code-evidenced), with one medium-confidence UX/product sub-question.**

1. **Tooltip scope bug (primary, explains "Unknown everywhere, even when not applicable"):** `TooltipRenderer.GetTooltip()` treats *absence from `tileHealthData`* the same as *unknown soil-health data* by coercing missing keys to `NaN` and returning `CreateUnknownTooltip()`. The visualization snapshot intentionally contains only `HoeDirt` tiles (`ModController.UpdateTileHealthData` filters non-HoeDirt terrain features). Therefore, every non-HoeDirt/non-applicable cursor tile is missing from the snapshot and is incorrectly rendered as `Soil Health: Unknown`. The correct behavior is to return `null` for tiles not present in the snapshot, while still rendering `Soil Health: Unknown` for snapshot entries whose value is NaN/Infinity (tilled tile, data unavailable).

2. **Throttle-render interaction bug (primary, explains "flashing forever constantly"):** FR-015 throttling is implemented as a suppression that returns `null` inside the 50ms per-tile window. `VisualizationService.RenderTooltip()` interprets `null` as "draw nothing", so the same tooltip alternates between drawn and not drawn on successive frames (~20Hz blink) while hovering one tile. Throttling should limit recomputation/state updates, not hide an already-valid tooltip. A cached last tooltip (or equivalent stable render path) is needed.

3. **Tilled HoeDirt remains NaN/Unknown when no soil-health record exists (explains "never showed anything different... even when tilled and watered soil"):** `SoilHealthService.GetSoilHealth()` returns `NaN` for missing records (sparse cache design). `UpdateTileHealthData()` passes that NaN through for HoeDirt tiles. If no code path initializes a measured soil-health value on tilling (or another defined baseline event), newly tilled/watered soil will remain unknown until an operation that creates a record (e.g., compost application via `Set/UpdateHealth`, depending on flow) occurs. This may be partly intentional per FR-014's "unavailable data" state, but the player-facing result reported is confusing; it needs product/spec clarification: should new HoeDirt start at an unknown state, a default baseline, or inherit/migrate a defined initial value?

## Proposed Remediation

**Preferred (targeted fix, preserves FR-014/FR-015 intent):**

1. **Fix tooltip applicability gating in `TooltipRenderer.GetTooltip`:** look up the cursor tile without coercing missing keys to NaN. If the tile is not present in `tileHealthData`, return `null` (no tooltip). If present and value is NaN/Infinity or resolves to `Unknown`, return `CreateUnknownTooltip` for that tilled tile. Keep the negative-cursor guard and tooltip-enabled/config checks.
2. **Fix flashing by separating throttle from visibility:** cache the last successfully computed `TooltipData` (and/or the last rendered tile) in `TooltipRenderer` or `VisualizationService`. When throttled for the same tile, return/render the cached tooltip instead of `null`; only suppress recomputation. On tile change (leading edge), compute immediately. On cursor leave / no applicable tile / tooltips disabled / menu pause behavior, clear cache and return `null`.
3. **Clarify and, if needed, fix HoeDirt initialization semantics:** determine intended baseline for newly tilled soil. If new HoeDirt should have a known starting value, add an initialization path (e.g., on terrain-feature creation/tilling event or first observation) that creates a record without violating sparse-cache/multiplayer rules. If "Unknown until measured" is intentional, keep NaN behavior but ensure UX/docs/tests make it explicit; the current report suggests players expect tilled/watered soil to show a value.
4. **Add regression tests:** missing-key cursor tile returns `null`; present NaN HoeDirt returns `Soil Health: Unknown`; throttled same-tile frame does not hide an existing tooltip; HoeDirt initialization behavior matches the clarified spec.

**Alternatives:**
- **Gate on game state in renderer:** check `Game1.currentLocation.terrainFeatures` for `HoeDirt` before showing tooltip. *Trade-off:* couples renderer to live game state and breaks headless/test patterns; snapshot-driven rendering is cleaner. Prefer fixing snapshot semantics.
- **Populate snapshot with explicit "not applicable" sentinel for all visible tiles:** avoids missing-key ambiguity. *Trade-off:* larger per-frame allocations and unnecessary work; conflicts with current filtered snapshot design.
- **Disable throttle return-null only:** keep current scope bug. *Trade-off:* stops flashing but still shows Unknown on inapplicable tiles.

## Files Likely to Change

- `LivingRoots/Services/Visualization/TooltipRenderer.cs`
- `LivingRoots/Services/Visualization/VisualizationService.cs` (if tooltip cache/last-render state is owned here)
- `LivingRoots/Controllers/ModController.cs` (only if HoeDirt baseline initialization or snapshot semantics change)
- `LivingRoots/Services/SoilHealthService.cs` and/or `LivingRoots/Domain/ISoilHealthService.cs` (only if initialization/default-record behavior is changed)
- `LivingRoots.Tests/Visualization/TooltipRendererTests.cs`
- `LivingRoots.Tests/Visualization/VisualizationServiceTooltipTests.cs`
- Possible new/updated controller or soil-health initialization tests, depending on clarified HoeDirt baseline behavior

## Tests to Add or Update

- Tooltip returns `null` when cursor tile is absent from `tileHealthData` (non-applicable tile / not in snapshot).
- Tooltip returns `Soil Health: Unknown` (no `%`, no category) when cursor tile is present in `tileHealthData` with `NaN` or `Infinity`.
- Tooltip continues to render the same stable tooltip during FR-015 throttle window for the same tile (no frame-to-frame disappearance/flashing).
- Leading-edge update still occurs when entering a new tile; per-tile throttle state remains isolated per tile.
- HoeDirt with no soil-health record: expected result per clarified spec (unknown vs initialized baseline). Add/adjust test after product decision.
- Integration-oriented visualization test: `ModController.UpdateTileHealthData` snapshot + renderer behavior does not produce Unknown tooltip for non-HoeDirt cursor tiles.

## Risks & Considerations

- **Spec/product ambiguity:** "tilled and watered soil never showed anything different" may reflect missing HoeDirt initialization rather than a rendering bug alone. Fixing only tooltip scope/throttle may leave tilled unknown-data cases stable-but-unknown. Confirm intended baseline before changing soil-health semantics.
- **FR-015 compliance:** changing throttle behavior must preserve "minimum 50ms between tooltip updates" / rapid-movement suppression intent while avoiding visible flicker. Tests should pin the intended observable behavior.
- **Multiplayer/host authority (Constitution VI):** any soil-health initialization on HoeDirt creation must be host-authoritative or otherwise safe; avoid creating divergent per-client farm state.
- **Sparse cache invariants:** do not store default 0 values unintentionally if the project treats 0 specially; follow existing `SoilHealthService`/`TryGetSoilHealth` semantics and `research-cache-vs-unknown.md`.
- **Performance:** tooltip caching must remain cheap per frame; avoid per-frame allocations in the hot render path where practical.
- **Gate 3 verification:** this is runnable visualization/gameplay behavior. After a fix, follow `VERIFICATION_PROTOCOL.md` / Gate 3 requirements (real SMAPI session + relevant signals) before claiming verified.

## Open Questions

- [NEEDS CLARIFICATION: What is the intended soil-health value/state for a newly tilled `HoeDirt` tile before compost/decay creates an explicit record? Should it be `Unknown` (current sparse-cache/NaN model), a measured default baseline (e.g., 0 or another value), or initialized by a specific game event?]
- [NEEDS CLARIFICATION: Is watering expected to affect soil-health visualization/data, or is "tilled and watered" in the report only describing tile state?]
- [NEEDS CLARIFICATION: Should FR-015 throttle suppress recomputation only, while continuing to render the last tooltip, for all throttle implementations in this feature?]
