# Research: Cache Missing Entry vs. Unknown State for Tilled Tiles

**Date:** 2026-09-11
**Spec:** `001-soil-health-visualization/spec.md` — FR-014, FR-002, FR-003
**Question:** When the visualization system needs to render a tilled tile that has no entry in the sparse soil health cache, should it be treated as 0% health (Poor category, red overlay) or as unknown data (gray overlay #808080)?

---

## 1. Executive Summary

The current codebase has a **fundamental ambiguity** at the intersection of the sparse cache pattern and the visualization system: `SoilHealthService.GetSoilHealth()` returns `0f` for tiles with no cache entry, which the visualization system would render as Poor (red) — but FR-014 mandates gray for tiles where "soil health data is unavailable." The sparse cache deliberately does not store zero values, making it **impossible to distinguish "never interacted with" from "decayed to 0%"** through the existing API.

**Recommendation:** Tilled tiles with no cache entry MUST be rendered as unknown (gray #808080) per FR-014. This requires:
1. Adding a `TryGetSoilHealth` method to `ISoilHealthService` that can distinguish "no data" from "0% health"
2. Updating the visualization data flow to query tilled tiles from game state and use the new method
3. Keeping the existing `GetSoilHealth` backward-compatible (returns 0 for missing) for decay/compost logic

---

## 2. The Core Problem

### 2.1 Sparse Cache Pattern

The sparse cache pattern is explicitly documented in AGENTS.md:

> **Sparse cache**: don't store default (0) health values — keeps save files small

The implementation enforces this in two places:

**Save path** (`SoilHealthService.cs:484`):
```csharp
// Only save non-zero values to prevent bloating the save file with default values
if (Math.Abs(clampedValue) > 0.0001f) // Using epsilon comparison for floating point
{
    tileDict[tileKey] = clampedValue;
}
```

**Set path** (`SoilHealthService.cs:665`):
```csharp
// Don't store default values; keep the cache sparse to prevent unbounded growth.
if (Math.Abs(clampedValue) < 0.0001f) // Using epsilon comparison for floating point
{
    if (_runtimeCache.TryGetValue(locationName, out var existingTiles) &&
        existingTiles.Remove(tilePoint) &&
        existingTiles.Count == 0)
    {
        _runtimeCache.Remove(locationName);
    }
    return SoilHealthOperationResult.Success;
}
```

**Load path** (`SoilHealthService.cs:319`):
```csharp
if (processingResult.IsSuccess &&
    processingResult.TilePoint.HasValue &&
    processingResult.HealthValue.HasValue &&
    Math.Abs(processingResult.HealthValue.Value) > 0.0001f) // Using epsilon comparison for floating point
{
    tileDict[processingResult.TilePoint.Value] = processingResult.HealthValue.Value;
}
```

### 2.2 GetSoilHealth Returns 0 for Missing Entries

The query API collapses "no data" and "0% health" into a single return value:

**`SoilHealthService.cs:521-542`**:
```csharp
public float GetSoilHealth(string locationName, Vector2 tile)
{
    // ...
    float result;
    lock (_lock)
    {
        if (_runtimeCache.TryGetValue(locationName, out var tiles) && tiles.TryGetValue(tilePoint, out var health))
        {
            result = health;
        }
        else
        {
            result = 0f; // Return default (Poor Soil) if no data exists
        }
    }
    return result;
}
```

The comment "Return default (Poor Soil) if no data exists" confirms the ambiguity: the method cannot distinguish between "tile has 0% health" and "tile has no health data."

### 2.3 The Interface Has No "TryGet" Pattern

The `ISoilHealthService` interface (`Domain/ISoilHealthService.cs`) exposes only:

```csharp
float GetSoilHealth(string locationName, Vector2 tile);
```

There is no `TryGetSoilHealth` or `HasSoilHealthData` method that would allow callers to distinguish "no data" from "0% health."

---

## 3. How the Visualization System Consumes Health Data

### 3.1 VisualizationService Receives a Pre-Built Dictionary

The `VisualizationService` does not query `ISoilHealthService` directly. It receives a pre-built dictionary via `SetTileHealthData`:

**`VisualizationService.cs:68-74`**:
```csharp
public void SetTileHealthData(Dictionary<Point, float> tileHealthData)
{
    lock (_dataLock)
    {
        _tileHealthData = tileHealthData ?? new Dictionary<Point, float>();
    }
}
```

This means the **caller** is responsible for building the dictionary of tile health data. The visualization system only renders overlays for tiles present in this dictionary.

### 3.2 OverlayRenderer Only Processes Tiles in the Dictionary

**`OverlayRenderer.cs:41-47`**:
```csharp
public List<TileOverlay> GetOverlays(Rectangle viewport, Dictionary<Point, float> tileHealthData)
{
    // FR-021: zero tiles = no-op with minimal resource usage, no cache allocation
    if (tileHealthData == null || tileHealthData.Count == 0)
    {
        return new List<TileOverlay>();
    }
    // ...
}
```

If a tilled tile is not in the dictionary, **no overlay is rendered at all** — not red, not gray, nothing.

### 3.3 Unknown Rendering Only Triggers for NaN/Infinity

The `OverlayRenderer` renders gray unknown overlays only when the health value is NaN or Infinity:

**`OverlayRenderer.cs:148-158`**:
```csharp
// FR-014: render Unknown color when health data is unavailable
if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
{
    var unknownColor = new Color(
        ModConstants.UnknownColor.R,
        ModConstants.UnknownColor.G,
        ModConstants.UnknownColor.B,
        (byte)(opacity * 255f));
    overlays.Add(new TileOverlay(tilePos, unknownColor, PatternType.None, healthValue, HealthCategory.Unknown));
    continue;
}
```

A `0f` value from `GetSoilHealth` would be treated as Poor (red), not Unknown (gray).

### 3.4 The Data Flow Gap in ModController

The `ModController.OnRenderedWorld` handler does **not** call `SetTileHealthData` at all:

**`ModController.cs:771-794`**:
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
        // ...
    }
    // ...
}
```

The visualization data pipeline is **incomplete** — there is no code that:
1. Queries game state for tilled tiles (HoeDirt terrain features)
2. Queries `ISoilHealthService` for each tilled tile's health
3. Builds the `Dictionary<Point, float>` for `SetTileHealthData`

This means the cache-vs-unknown question is not just theoretical — it is a **design gap that must be resolved during implementation**.

---

## 4. Evidence from the Spec

### 4.1 FR-014 Mandates Gray for Unavailable Data

> **FR-014**: System MUST render a neutral gray overlay for tiles where soil health data is unavailable or hasn't loaded yet. [`spec.md:177`]

The spec does not say "for tiles with 0% health" — it says "where soil health data is unavailable." A tilled tile with no cache entry has unavailable data.

### 4.2 Soil Health Tile Definition

> **Soil Health Tile**: A tilled soil position with an associated health value (0-100) that determines overlay color and tooltip content. [`spec.md:191`]

This definition implies that a tile must have an "associated health value" to be a Soil Health Tile. But the sparse cache doesn't store 0 values, so a tilled tile that was never interacted with has no associated health value in the cache.

### 4.3 Session 2026-09-08: Single Gray for All Unknown States

> Q: Should the spec distinguish between "data is loading" (transient) and "data will never be available" (permanent) visually, or is a single gray overlay sufficient for both? → A: Single gray overlay — both loading and no-data states show the same neutral gray (#808080) overlay. [`spec.md:133`]

This confirms that "no data" (which includes "no cache entry") should be gray, not red.

### 4.4 FR-002 and FR-003: Unknown State for Tooltips and Hoe Feedback

> **FR-002**: For tiles where soil health data is unavailable (unknown state per FR-014), the tooltip MUST display "Soil Health: Unknown" (without a percentage) [`spec.md:158`]

> **FR-003**: For tiles where soil health data is unavailable (unknown state per FR-014), the flash color MUST use the gray "unknown" color (#808080) and the floating text MUST display "Soil Health: Unknown" [`spec.md:159`]

Both FR-002 and FR-003 explicitly reference FR-014's unknown state for tiles where data is unavailable.

---

## 5. Evidence from the Codebase

### 5.1 HealthCategory.Unknown Is Already Defined

**`Domain/Visualization/HealthCategory.cs:7-13`**:
```csharp
public enum HealthCategory
{
    Poor = 0,
    Moderate = 1,
    Healthy = 2,
    Unknown = 3
}
```

The `Unknown` category is a first-class enum value, confirming it is a valid state.

### 5.2 ColorInterpolationService Handles NaN/Infinity as Unknown

**`ColorInterpolationService.cs:28-33`**:
```csharp
if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
{
    var unknown = ModConstants.UnknownColor;
    unknown.A = (byte)(opacity * 255f);
    return unknown;
}
```

**`ColorInterpolationService.cs:49-50`**:
```csharp
if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
    return HealthCategory.Unknown;
```

The infrastructure for rendering unknown tiles exists but is gated on NaN/Infinity — not on "no cache entry."

### 5.3 ModConstants.UnknownColor Is Defined

**`Constants.cs:34`**:
```csharp
public static readonly Color UnknownColor = new Color(107, 114, 128); // #6B7280
```

Note: The actual constant is `#6B7280` (slate gray), not `#808080` (pure gray) as stated in the spec. The spec's research files also reference `Color.Gray` (#808080). This is a minor discrepancy but the intent is clear: a neutral gray for unknown.

### 5.4 Existing Tests Confirm the Ambiguity

**`SoilHealthServiceTests.cs:395`**:
```csharp
Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(99, 99))); // Non-existent tile should return default value
```

The test confirms that non-existent tiles return `0f`, but the comment says "default value" — not "0% health." This is the ambiguity in action.

### 5.5 SoilDecayService Treats Missing as 0

**`SoilDecayService.cs:61-62`**:
```csharp
var currentHealth = _soilHealthService.GetSoilHealth(locationName, tile);
var newHealth = Math.Max(currentHealth - decayAmount, 0f);
```

The decay service reads the current health and subtracts decay. For a tile with no cache entry, `GetSoilHealth` returns 0, so `newHealth` = `Math.Max(0 - decayAmount, 0)` = 0. No decay is applied (correct behavior — you can't decay what doesn't exist). This logic works correctly with the current API.

### 5.6 CompostApplicationService Treats Missing as 0

**`CompostApplicationService.cs:37-38`**:
```csharp
var currentHealth = _soilHealthService.GetSoilHealth(locationName, tile);
if (currentHealth >= ModConstants.MaxSoilHealth)
```

For a tile with no cache entry, `currentHealth` = 0, which is < 100, so compost can be applied. This is also correct behavior.

---

## 6. Analysis: The Two Interpretations

### 6.1 Interpretation A: "No Entry" = 0% Health (Poor, Red)

**Argument:** The existing `GetSoilHealth` returns 0 for missing entries, and 0% health is Poor category. The comment at line 538 says "Return default (Poor Soil) if no data exists."

**Problems:**
1. Violates FR-014, which mandates gray for "data unavailable"
2. Misleading: a freshly tilled tile that was never interacted with would show red, implying it's degraded
3. Inconsistent with the spec's definition of Soil Health Tile ("with an associated health value")
4. The sparse cache pattern means 0 is never stored, so "no entry" ≠ "0% health" in the data model

### 6.2 Interpretation B: "No Entry" = Unknown (Gray)

**Argument:** FR-014 mandates gray for unavailable data. The sparse cache doesn't store 0 values, so "no entry" means "no data." The spec defines Soil Health Tile as having an associated health value — if there's no cache entry, there's no associated value.

**Problems:**
1. Requires a new API to distinguish "no data" from "0% health"
2. Requires the visualization data flow to be fully implemented (currently incomplete)
3. Edge case: a tile that genuinely decayed to 0% health would have no cache entry (since 0 is not stored), so it would show gray instead of red. But this is arguably correct — once health reaches 0, the tile is indistinguishable from "never interacted with" in the sparse cache.

---

## 7. The Critical Edge Case: Decayed to 0% vs. Never Interacted

The sparse cache pattern creates an unavoidable ambiguity:

1. **Tile A**: Tilled, never interacted with → no cache entry
2. **Tile B**: Tilled, had 50% health, decayed to 0% → cache entry removed (because 0 is not stored)

Both tiles have no cache entry. Both would return 0 from `GetSoilHealth`. Both would render as gray under Interpretation B.

**Is this acceptable?** Yes, because:
- The sparse cache is a deliberate design choice to keep saves small
- A tile at 0% health is functionally identical to an untilled tile (no health benefit)
- The decay service already handles this correctly (no decay applied to missing entries)
- The spec's definition of Soil Health Tile implies a tile must have an associated health value to be rendered with a health-based color

---

## 8. Recommended Solution

### 8.1 Add TryGetSoilHealth to ISoilHealthService

Add a new method that distinguishes "has data" from "no data":

```csharp
// New method in ISoilHealthService
bool TryGetSoilHealth(string locationName, Vector2 tile, out float healthValue);
```

- Returns `true` + health value when the tile has a cache entry
- Returns `false` + `healthValue = 0f` when the tile has no cache entry

### 8.2 Update the Visualization Data Flow

The ModController (or a dedicated data provider) must:
1. Query game state for all tilled tiles (HoeDirt terrain features) in the current location
2. For each tilled tile, call `TryGetSoilHealth`
3. If `true` → add to dictionary with the health value
4. If `false` → add to dictionary with `float.NaN` (to trigger Unknown/gray rendering)
5. Pass the dictionary to `SetTileHealthData`

### 8.3 Keep GetSoilHealth Backward-Compatible

The existing `GetSoilHealth` method should continue to return 0 for missing entries. This preserves correct behavior for:
- `SoilDecayService` (no decay on missing tiles)
- `CompostApplicationService` (compost can be applied to missing tiles)
- Any other existing callers

### 8.4 Rendering Logic

The `OverlayRenderer` already handles NaN/Infinity as Unknown (gray). By passing `float.NaN` for tiles with no cache entry, the existing rendering logic will correctly render them as gray per FR-014.

---

## 9. Implementation Impact

| Component | Change Required |
|-----------|----------------|
| `ISoilHealthService` | Add `TryGetSoilHealth` method |
| `SoilHealthService` | Implement `TryGetSoilHealth` |
| `ModController` | Implement data flow: query tilled tiles → build dictionary → call `SetTileHealthData` |
| `OverlayRenderer` | No change needed (already handles NaN as Unknown) |
| `ColorInterpolationService` | No change needed (already handles NaN as Unknown) |
| `TooltipRenderer` | No change needed (already returns null for tiles not in dictionary) |
| `VisualizationService` | No change needed |

---

## 10. Summary

| Question | Answer |
|----------|--------|
| Should tilled tiles with no cache entry render as Poor (red)? | **No** — violates FR-014 |
| Should tilled tiles with no cache entry render as Unknown (gray)? | **Yes** — per FR-014 |
| Can the current API distinguish "no data" from "0%"? | **No** — `GetSoilHealth` returns 0 for both |
| What is required to implement this correctly? | Add `TryGetSoilHealth` + implement visualization data flow |
| Does the sparse cache pattern make "decayed to 0%" ambiguous? | **Yes** — but this is an acceptable tradeoff |
| What sentinel value should represent "no data" in the visualization dictionary? | `float.NaN` — already handled by `OverlayRenderer` |

**Tilled tiles with no entry in the sparse soil health cache MUST be treated as unknown data (gray overlay #6B7280) per FR-014, not as 0% health (Poor, red).** This requires adding a `TryGetSoilHealth` method to distinguish "no data" from "0% health" and implementing the visualization data flow to query tilled tiles from game state.

---

## 11. Sources

| # | Source | Location |
|---|--------|----------|
| 1 | `LivingRoots/Services/SoilHealthService.cs` | Lines 521-542 — `GetSoilHealth` returns 0 for missing entries |
| 2 | `LivingRoots/Services/SoilHealthService.cs` | Lines 484-487 — Save path skips zero values |
| 3 | `LivingRoots/Services/SoilHealthService.cs` | Lines 665-674 — Set path removes zero values from cache |
| 4 | `LivingRoots/Services/SoilHealthService.cs` | Lines 316-322 — Load path skips zero values |
| 5 | `LivingRoots/Domain/ISoilHealthService.cs` | Lines 5-13 — Interface has no TryGet pattern |
| 6 | `LivingRoots/Services/Visualization/VisualizationService.cs` | Lines 68-74 — `SetTileHealthData` receives pre-built dictionary |
| 7 | `LivingRoots/Services/Visualization/OverlayRenderer.cs` | Lines 41-47 — Only processes tiles in dictionary |
| 8 | `LivingRoots/Services/Visualization/OverlayRenderer.cs` | Lines 148-158 — NaN/Infinity → Unknown (gray) |
| 9 | `LivingRoots/Services/Visualization/ColorInterpolationService.cs` | Lines 28-33 — NaN/Infinity → UnknownColor |
| 10 | `LivingRoots/Services/Visualization/ColorInterpolationService.cs` | Lines 49-50 — NaN/Infinity → Unknown category |
| 11 | `LivingRoots/Domain/Visualization/HealthCategory.cs` | Lines 7-13 — `Unknown = 3` enum value |
| 12 | `LivingRoots/Constants.cs` | Line 34 — `UnknownColor = #6B7280` |
| 13 | `LivingRoots/Controllers/ModController.cs` | Lines 771-794 — No `SetTileHealthData` call |
| 14 | `LivingRoots/Services/SoilDecayService.cs` | Lines 61-62 — Decay treats missing as 0 |
| 15 | `LivingRoots/Services/CompostApplicationService.cs` | Lines 37-38 — Compost treats missing as 0 |
| 16 | `LivingRoots.Tests/SoilHealthServiceTests.cs` | Line 395 — Test confirms missing tile returns 0 |
| 17 | `specs/001-soil-health-visualization/spec.md` | Line 177 — FR-014: gray for unavailable data |
| 18 | `specs/001-soil-health-visualization/spec.md` | Line 191 — Soil Health Tile definition |
| 19 | `specs/001-soil-health-visualization/spec.md` | Line 133 — Session 2026-09-08: single gray for all unknown |
| 20 | `specs/001-soil-health-visualization/spec.md` | Lines 158-159 — FR-002/FR-003: unknown state rendering |
| 21 | `AGENTS.md` | Conventions section — "Sparse cache: don't store default (0) health values" |
