# Research: Soil Health Tooltip Behavior on Non-Tilled Tiles

**Date:** 2026-09-12
**Spec:** `001-soil-health-visualization/spec.md` — FR-002, FR-014, User Story 2
**Question:** When the cursor is over a non-tilled tile within the game world (e.g., grass, path, water), should a soil health tooltip show anything?

---

## 1. Executive Summary

**Non-tilled tiles (grass, path, water, etc.) should NOT show a soil health tooltip.** The visualization system is explicitly scoped to tilled soil tiles per FR-002 and the "Soil Health Tile" definition. The current codebase already implements this correctly through a dictionary-lookup pattern: only tiles present in the `_tileHealthData` dictionary (which represents tilled tiles with health data) are eligible for tooltips. Non-tilled tiles are never added to this dictionary, so they naturally produce no tooltip.

---

## 2. Spec Scope Analysis

### 2.1 What the Spec Defines

The spec is unambiguous about the scope of tooltips:

**FR-002** (`spec.md:165`):
> System MUST display hover tooltips showing soil health percentage and status text **for tilled tiles**.

**Soil Health Tile definition** (`spec.md:199`):
> A **tilled soil position** with an associated health value (0-100) that determines overlay color and tooltip content.

This definition explicitly requires:
1. A **tilled soil position** — non-tilled tiles (grass, path, water) are excluded by definition
2. An **associated health value** — tiles without health data are not "Soil Health Tiles"

**User Story 2** (`spec.md:31-43`):
> As a player, I want to hover over a **tilled soil tile** and see its exact health percentage and status text...

The acceptance scenarios consistently reference "tilled soil tile" as the precondition.

### 2.2 Out of Scope

The spec's Out of Scope section (`spec.md:241`) confirms:
> Soil health value modification through visualization (visualization is read-only)

This confirms the visualization system is purely informational and scoped to the soil health domain — it should not interact with non-tiled tiles.

---

## 3. Current Codebase Analysis

### 3.1 TooltipRenderer.cs — Dictionary Lookup Pattern

**File:** `LivingRoots/Services/Visualization/TooltipRenderer.cs`

The `GetTooltip` method (lines 69-118) determines whether to show a tooltip:

```csharp
// Line 79: Convert cursor position to tile coordinates
var tile = CursorToTile(cursorPosition);

// Lines 82-86: If tile is NOT in the dictionary, return null (no tooltip)
if (!tileHealthData.ContainsKey(tile))
{
    _lastTooltipTile = UninitializedTile;
    return null;
}
```

**Key finding:** The method only shows tooltips for tiles that exist in the `tileHealthData` dictionary. Non-tilled tiles are never added to this dictionary (see Section 3.3 below), so they naturally produce no tooltip.

### 3.2 VisualizationService.cs — Same Dictionary Lookup

**File:** `LivingRoots/Services/Visualization/VisualizationService.cs`

The `RenderTooltip` method (lines 131-172) implements the same pattern:

```csharp
// Line 141: Convert screen position to tile coordinates
var tilePos = ScreenToTile(cursorPosition);

// Lines 144-151: Look up health data for the tile under cursor
float healthValue;
lock (_dataLock)
{
    if (!_tileHealthData.TryGetValue(tilePos, out healthValue))
    {
        return; // No data for this tile, no tooltip
    }
}
```

**Key finding:** Same pattern — if a tile is not in the `_tileHealthData` dictionary, no tooltip renders. This is the primary gating mechanism.

### 3.3 ModController.cs — Data Pipeline (Incomplete)

**File:** `LivingRoots/Controllers/ModController.cs`

The `OnRenderedWorld` handler (lines 771-794) is responsible for triggering visualization rendering:

```csharp
private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
{
    if (IsDisposed() || _visualizationService == null) return;
    try
    {
        // ...
        var viewport = new Microsoft.Xna.Framework.Rectangle(0, 0, 100, 100);
        var gameTime = _lastGameTime;
        _visualizationService.RenderOverlays(e.SpriteBatch, viewport, gameTime);
        _visualizationService.RenderTooltip(e.SpriteBatch, new Microsoft.Xna.Framework.Vector2(0, 0), gameTime);
        _visualizationService.RenderHoeFeedback(e.SpriteBatch, gameTime);
    }
    // ...
}
```

**Key finding:** The data pipeline is **incomplete** — `SetTileHealthData` is never called from `OnRenderedWorld`. This means the visualization system currently has no data to render. When implemented, the data provider must only add tilled tiles to the dictionary, ensuring non-tilled tiles are excluded.

### 3.4 OverlayRenderer.cs — Same Pattern for Overlays

**File:** `LivingRoots/Services/Visualization/OverlayRenderer.cs`

The `GetOverlays` method (lines 41-100) only processes tiles present in the input dictionary:

```csharp
// Lines 44-47: Empty dictionary = no overlays
if (tileHealthData == null || tileHealthData.Count == 0)
{
    return new List<TileOverlay>();
}
```

This confirms the consistent pattern: tiles not in the dictionary get no visualization treatment.

### 3.5 SoilHealthService.cs — Sparse Cache Pattern

**File:** `LivingRoots/Services/SoilHealthService.cs`

The sparse cache deliberately does not store zero/default values (line 484, 665). This means:

- A tilled tile that was never interacted with → no cache entry
- A tilled tile that decayed to 0% → no cache entry (removed when value reached 0)
- A non-tilled tile → never has a cache entry (only tilled tiles can have soil health)

The `GetSoilHealth` method (lines 521-542) returns `0f` for missing entries, but the tooltip system does not query this directly — it uses the pre-built dictionary.

---

## 4. How Other Stardew Valley Mods Handle This

### 4.1 Informant Mod

**Source:** [github.com/gottyduke/stardew-informant](https://github.com/gottyduke/stardew-informant) | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/21286)

Informant displays tooltips for objects and terrain features that have registered tooltip generators. Its approach:

- **Only shows tooltips for relevant objects**: Crops, fruit trees, machines, and trees have tooltip providers. Other objects (grass, paths, water) have no provider → no tooltip.
- **Configurable scope**: The `HideMachineTooltips` config option demonstrates the pattern: "ForNonMachines" shows tooltips only on machines that do work, "Never" shows on every item, "ForChests" hides for chests.
- **API-based registration**: Other mods can register tooltip generators for specific object types via `AddObjectTooltipGenerator` and `AddTerrainFeatureTooltipGenerator`.

**Relevance to Living Roots:** Informant's pattern confirms the convention: tooltips only appear for tiles/objects that have registered data providers. Non-relevant tiles show nothing.

### 4.2 UI Info Suite 2

**Source:** [github.com/Annosz/UIInfoSuite2](https://github.com/Annosz/UIInfoSuite2) | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/43127)

UI Info Suite 2 provides on-screen information about crops, machines, and other game elements:

- **Crop tooltips**: Only appear for crops (tiled soil with a planted seed), not for bare tilled soil or grass
- **Machine tooltips**: Only appear for machines (craftables with production), not for other objects
- **No tooltip for non-relevant tiles**: Grass, paths, water, and decorations produce no tooltip

**Relevance:** Confirms the pattern: tooltips are context-specific and only appear for tiles with relevant data.

### 4.3 Data Layers

**Source:** [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/1691)

Data Layers renders overlay tiles based on data layers (e.g., sprinkler coverage, tillable soil):

- **Only renders for tiles in the active layer**: If a tile is not part of the active data layer, no overlay appears
- **Toggleable layers**: Each layer can be enabled/disabled independently

**Relevance:** Confirms the pattern: overlays and tooltiles are data-driven — tiles without data get no visualization.

---

## 5. Analysis: Should Non-Tilled Tiles Show a Tooltip?

### 5.1 Arguments For Showing Nothing (Recommended)

1. **Spec compliance**: FR-002 explicitly scopes tooltips to "tilled tiles." Showing a tooltip on grass, path, or water would violate the spec.

2. **User experience**: Showing "Soil Health: N/A" or "Soil Health: Unknown" on every non-tilled tile would be confusing and intrusive. Players don't expect soil health information on water or paths.

3. **Consistency with overlay rendering**: The overlay system only renders on tiles in the dictionary. If overlays don't appear on non-tilled tiles, tooltips shouldn't either.

4. **Established mod conventions**: Informant, UI Info Suite 2, and Data Layers all follow the pattern: tooltips/visualizations only appear for relevant tiles.

5. **The "Soil Health Tile" definition**: Non-tilled tiles are explicitly excluded by the definition ("A tilled soil position with an associated health value").

### 5.2 Arguments For Showing Something (Not Recommended)

1. **Discoverability**: A tooltip like "Not tilled soil" could teach players that tooltips only work on tilled tiles. However, this is better addressed through an in-game legend or tutorial (currently out of scope per `spec.md:243`).

2. **Debugging**: Showing "no data" could help debug visualization issues. However, this can be achieved through logging (already present in the codebase at `LogLevel.Trace`).

### 5.3 Edge Cases to Consider

| Tile Type | Current Behavior | Spec-Compliant Behavior |
|-----------|-----------------|------------------------|
| Grass | Not in dictionary → no tooltip | No tooltip ✓ |
| Path/Stone | Not in dictionary → no tooltip | No tooltip ✓ |
| Water | Not in dictionary → no tooltip | No tooltip ✓ |
| Tilled soil with health data | In dictionary → tooltip shows | Tooltip shows ✓ |
| Tilled soil, never interacted | Not in dictionary → no tooltip | Should show "Unknown" per FR-014* |
| Tilled soil, decayed to 0% | Not in dictionary → no tooltip | Should show "Unknown" per FR-014* |

*Note: The tilled-but-no-cache-entry case is addressed by the separate research document `research-cache-vs-unknown.md` which recommends adding `TryGetSoilHealth` and passing `float.NaN` for missing entries. This ensures tilled tiles show as "Unknown" (gray overlay + "Soil Health: Unknown" tooltip) per FR-014, while non-tilled tiles remain excluded.

---

## 6. Recommendation

### 6.1 Do Not Show Tooltips on Non-Tilled Tiles

**Non-tilled tiles (grass, path, water, decorations, etc.) should NOT show a soil health tooltip.** This is consistent with:

- FR-002's explicit scope ("for tilled tiles")
- The Soil Health Tile definition ("a tilled soil position")
- The current dictionary-lookup pattern in the codebase
- Established conventions from Informant, UI Info Suite 2, and Data Layers

### 6.2 Implementation Guidance

The current codebase already implements this correctly. When completing the visualization data pipeline:

1. **Data provider must query tilled tiles from game state**: Use `Game1.currentLocation.terrainFeatures` to find `HoeDirt` tiles.
2. **Only add tilled tiles to the `_tileHealthData` dictionary**: Non-tilled tiles should never be added.
3. **For tilled tiles with no cache entry**: Add with `float.NaN` to trigger Unknown state rendering per FR-014.
4. **Do not add non-tilled tiles**: The dictionary-lookup gating mechanism naturally excludes them.

### 6.3 Verification Checklist

- [ ] Hovering over grass → no tooltip
- [ ] Hovering over path/stone → no tooltip
- [ ] Hovering over water → no tooltip
- [ ] Hovering over tilled soil with health data → tooltip shows percentage and category
- [ ] Hovering over tilled soil with no cache entry → tooltip shows "Soil Health: Unknown"
- [ ] Hovering over non-tilled tile with cursor outside tile bounds → no tooltip (per FR-002)

---

## 7. Sources

| # | Source | Location |
|---|--------|----------|
| 1 | `LivingRoots/Services/Visualization/TooltipRenderer.cs` | Lines 79-86 — Dictionary lookup gates tooltip display |
| 2 | `LivingRoots/Services/Visualization/VisualizationService.cs` | Lines 141-151 — Same dictionary lookup pattern |
| 3 | `LivingRoots/Controllers/ModController.cs` | Lines 771-794 — `OnRenderedWorld` data pipeline (incomplete) |
| 4 | `LivingRoots/Services/Visualization/OverlayRenderer.cs` | Lines 41-47 — Empty dictionary = no overlays |
| 5 | `LivingRoots/Services/SoilHealthService.cs` | Lines 521-542 — `GetSoilHealth` returns 0 for missing entries |
| 6 | `specs/001-soil-health-visualization/spec.md` | Line 165 — FR-002: Tooltips for tilled tiles |
| 7 | `specs/001-soil-health-visualization/spec.md` | Line 199 — Soil Health Tile definition |
| 8 | `specs/001-soil-health-visualization/spec.md` | Lines 31-43 — User Story 2 scope |
| 9 | `specs/001-soil-health-visualization/spec.md` | Lines 239-245 — Out of Scope |
| 10 | `specs/001-soil-health-visualization/research-cache-vs-unknown.md` | Full document — Sparse cache vs unknown state analysis |
| 11 | `specs/001-soil-health-visualization/research/tooltip-unknown-state.md` | Full document — Unknown state tooltip format |
| 12 | [Informant - Stardew Valley](https://github.com/gottyduke/stardew-informant) | Tooltip pattern: only relevant objects get tooltips |
| 13 | [UI Info Suite 2](https://github.com/Annosz/UIInfoSuite2) | Context-specific tooltips for crops/machines |
| 14 | [Data Layers](https://www.nexusmods.com/stardewvalley/mods/1691) | Data-driven overlays only for relevant tiles |

---

## 8. Decision

| Question | Answer |
|----------|--------|
| Should non-tilled tiles (grass, path, water) show a soil health tooltip? | **No** |
| Does the current codebase handle this correctly? | **Yes** — the dictionary-lookup pattern naturally excludes non-tilled tiles |
| Does this align with the spec? | **Yes** — FR-002 scopes tooltips to "tilled tiles" only |
| Does this align with other Stardew Valley mods? | **Yes** — Informant, UI Info Suite 2, and Data Layers all scope tooltips to relevant tiles |
| What is required to maintain this behavior? | Ensure the data pipeline only adds tilled tiles to `_tileHealthData` |

**Non-tilled tiles should not show a soil health tooltip. The current dictionary-lookup pattern in `TooltipRenderer.cs` (line 82) and `VisualizationService.cs` (line 147) already implements this correctly — tiles not in the dictionary produce no tooltip.**
