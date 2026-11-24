# Research: Hover Tooltip for Tiles with Unknown Soil Health Data

**Date:** 2026-01-18
**Spec:** `001-soil-health-visualization/spec.md` — FR-002, FR-014
**Question:** When a player hovers over a tilled tile where soil health data hasn't loaded yet (the gray "unknown" overlay per FR-014), what should the tooltip display?

---

## 1. Executive Summary

The codebase defines a complete "Unknown" state infrastructure (`HealthCategory.Unknown`, `ModConstants.UnknownColor`, `PatternType.None`), and the spec already resolved the analogous question for hoe feedback: display `"Soil Health: Unknown"` without a percentage. The same pattern should apply to hover tooltips for visual consistency. The current codebase produces `"Soil Health: 0% (Unknown)"` for NaN values — a contradictory message that implies both "0% health" (Poor) and "Unknown" simultaneously.

**Recommendation:** For unknown-health tiles, the tooltip should display `"Soil Health: Unknown"` (without a percentage), matching the established hoe feedback convention from Session 2026-09-08/10.

---

## 2. The Spec Gap

### 2.1 What the Spec Defines

The spec defines the tooltip format for known-health tiles:

> **Tooltip format**: "Soil Health: {percentage}% ({category})" (e.g., "Soil Health: 75% (Healthy)") [`spec.md:125`]

FR-002 mandates tooltips for tilled tiles:

> **FR-002**: System MUST display hover tooltips showing soil health percentage and status text for tilled tiles. [`spec.md:144`]

But FR-002 does not specify behavior when the percentage is undefined (unknown state).

### 2.2 What the Spec Does NOT Define

The spec has no acceptance scenario for hovering over an unknown-health tile. User Story 2's acceptance scenarios only cover known-health tiles:

> 1. **Given** a tilled soil tile with known health value, **When** the player hovers over it, **Then** a tooltip displays the health percentage and status category [`spec.md:39-43`]

The phrase "with known health value" explicitly limits the scope. No scenario addresses the unknown case.

### 2.3 The Analogous Hoe Feedback Resolution

The spec DOES resolve the analogous question for hoe feedback. Session 2026-09-10 clarified:

> Q: When a player uses a hoe on a tile where soil health data hasn't loaded yet (gray "unknown" overlay per FR-014), what color should the flash effect use and what should the floating text display? → A: Gray flash (#808080) and "Soil Health: Unknown" floating text (without percentage). [`spec.md:97-98`]

This was codified in FR-003:

> **FR-003**: For tiles where soil health data is unavailable (unknown state per FR-014), the flash color MUST use the gray "unknown" color (#808080) and the floating text MUST display "Soil Health: Unknown" (without a percentage) to avoid implying a specific health value where none exists. [`spec.md:145`]

This establishes a clear precedent: **when health data is unknown, omit the percentage and display only "Soil Health: Unknown"**.

---

## 3. Evidence from the Codebase

### 3.1 Current Tooltip Formatting (Three Locations)

All three formatting locations produce the same contradictory message for unknown values:

**Location 1: `VisualizationService.FormatTooltipText`**

```csharp
// LivingRoots/Services/Visualization/VisualizationService.cs:398-408
private static string FormatTooltipText(float healthValue)
{
    var category = healthValue switch
    {
        >= 0 and < 34 => "Poor",
        >= 34 and < 67 => "Moderate",
        >= 67 and <= 100 => "Healthy",
        _ => "Unknown"
    };
    return $"Soil Health: {healthValue:F0}% ({category})";
}
```

For `healthValue = NaN`: `NaN:F0` formats as `"0"` in .NET, producing `"Soil Health: 0% (Unknown)"`.

**Location 2: `TooltipRenderer.GetTooltip`**

```csharp
// LivingRoots/Services/Visualization/TooltipRenderer.cs:107-118
var healthValue = tileHealthData[tile];
var category = _colorService.GetCategoryForHealth(healthValue);
var percentage = Math.Clamp(healthValue, 0f, 100f);
var color = _colorService.GetColorForHealth(healthValue);

var tooltip = new TooltipData
{
    Text = $"Soil Health: {percentage:F0}% ({category})",
    ...
};
```

For `healthValue = NaN`: `Math.Clamp(NaN, 0f, 100f)` returns `0f`, producing `"Soil Health: 0% (Unknown)"`.

**Location 3: `HoeFeedbackRenderer.CreateFeedback`**

```csharp
// LivingRoots/Services/Visualization/HoeFeedbackRenderer.cs:120-131
HealthCategory category = _colorInterpolationService.GetCategoryForHealth(healthValue);
int percentage = (int)Math.Clamp(Math.Round(healthValue), 0f, 100f);

return new HoeFeedback
{
    ...
    HealthText = $"Soil Health: {percentage}% ({category})"
};
```

Same issue: `Math.Round(NaN)` returns `0`, producing `"Soil Health: 0% (Unknown)"`.

### 3.2 The "Unknown" State Infrastructure

The codebase has a complete, well-defined unknown state:

| Component | Location | Behavior |
|-----------|----------|----------|
| `HealthCategory.Unknown` enum | `HealthCategory.cs:12` | `Unknown = 3` — first-class enum value |
| `ModConstants.UnknownColor` | `Constants.cs:34` | `Color.Gray` (#808080) |
| `GetCategoryForHealth(NaN)` | `ColorInterpolationService.cs:49-50` | Returns `HealthCategory.Unknown` |
| `GetColorForHealth(NaN)` | `ColorInterpolationService.cs:28-33` | Returns `ModConstants.UnknownColor` |
| `PatternType.None` for unknown | `PatternType.cs:27` | Unknown tiles get no pattern |
| Overlay rendering for unknown | `OverlayRenderer.cs:151-161` | Gray color + `PatternType.None` + `HealthCategory.Unknown` |

### 3.3 The Contradiction

The current behavior `"Soil Health: 0% (Unknown)"` is contradictory because:
- `0%` implies the tile has Poor health (a definitive value)
- `Unknown` implies the tile's health is undefined
- These two statements cannot both be true

This is the same problem identified in the hoe feedback research, which recommended omitting the percentage entirely.

---

## 4. Evidence from Prior Research

### 4.1 Hoe Feedback Unknown State Research

The companion research document `research/hoe-feedback-unknown-state.md` already analyzed this exact problem for hoe feedback and concluded:

> **Recommendation:** `"Soil Health: Unknown"` (without percentage).
>
> Rationale:
> - Displaying `"Soil Health: 0% (Unknown)"` is contradictory and misleading. 0% implies Poor health, not unknown data.
> - The category name "Unknown" already communicates the state clearly.
> - Omitting the percentage avoids implying a specific health value where none exists.
> - This is consistent with the spec's clarification that unknown = "no info" regardless of reason. [`research/hoe-feedback-unknown-state.md:227-233`]

The same reasoning applies to hover tooltips.

### 4.2 Required Fix (from hoe feedback research)

The hoe feedback research identified the required fix pattern:

```csharp
// Pseudocode for the fix
if (category == HealthCategory.Unknown)
{
    healthText = "Soil Health: Unknown";
}
else
{
    healthText = $"Soil Health: {percentage}% ({category})";
}
```

This fix should be applied to all three formatting locations:
1. `HoeFeedbackRenderer.CreateFeedback` (line 131)
2. `VisualizationService.FormatTooltipText` (line 407)
3. `VisualizationService.FormatHealthText` (line 415)
4. `TooltipRenderer.GetTooltip` (line 114)

---

## 5. Evidence from Stardew Valley / SMAPI Conventions

### 5.1 No Base-Game Convention for Unknown Tooltips

Stardew Valley's base game does not define visual feedback for tiles with unknown/missing state. The game's own systems (crop quality stars, cactus flower color) always have deterministic values. The mod must define its own convention. [`research/hoe-feedback-unknown-state.md:208-210`]

### 5.2 SMAPI Cursor API

SMAPI's `ICursorPosition.Tile` property provides tile coordinates directly. [`research-cursor-mapping.md:22-31`] The tooltip system uses this to map cursor → tile → health data. When health data is NaN/Infinity (unknown), the tooltip formatter must handle this case explicitly.

### 5.3 Consistency with Overlay Rendering

The overlay system already renders unknown tiles as solid gray (`PatternType.None`). A tooltip reading `"Soil Health: Unknown"` is consistent with this visual treatment — both communicate "no data available" without implying a specific value.

---

## 6. Analysis: What Should the Tooltip Display?

### 6.1 Recommended: `"Soil Health: Unknown"` (without percentage)

**Rationale:**
1. **Consistency with hoe feedback**: FR-003 and Session 2026-09-10 already established `"Soil Health: Unknown"` without percentage for hoe feedback on unknown tiles. The hover tooltip should match.
2. **Avoids contradiction**: `"Soil Health: 0% (Unknown)"` is misleading — 0% implies Poor health, not unknown data.
3. **Clear communication**: The word "Unknown" alone communicates the state unambiguously.
4. **Spec alignment**: Session 2026-09-08 established that unknown = "no info" regardless of reason. [`spec.md:118-119`] Omitting the percentage respects this design intent.
5. **Visual consistency**: The gray overlay (FR-014) + gray text + "Unknown" label creates a coherent visual language for the unknown state.

### 6.2 Alternative Considered: `"Soil Health: —% (Unknown)"`

Uses an em dash to indicate missing data explicitly. More explicit but adds visual clutter and is inconsistent with the hoe feedback convention (which omits the percentage entirely).

### 6.3 Alternative Considered: No Tooltip for Unknown Tiles

Some might argue that since FR-002 says "showing soil health percentage and status text," and there is no percentage for unknown tiles, no tooltip should show. However, this would:
- Break player expectation (hovering over a visible overlay produces no information)
- Waste the opportunity to communicate why the tile is gray
- Be inconsistent with the hoe feedback convention (which DOES show text for unknown tiles)

---

## 7. Implementation Gap

The current codebase has four locations that format health text, and none handle the Unknown case specially:

| Location | File | Line | Current Output for NaN |
|----------|------|------|------------------------|
| `FormatTooltipText` | `VisualizationService.cs` | 407 | `"Soil Health: 0% (Unknown)"` |
| `FormatHealthText` | `VisualizationService.cs` | 415 | `"Soil Health: 0% (Unknown)"` |
| `GetTooltip` | `TooltipRenderer.cs` | 114 | `"Soil Health: 0% (Unknown)"` |
| `CreateFeedback` | `HoeFeedbackRenderer.cs` | 131 | `"Soil Health: 0% (Unknown)"` |

**Required fix:** Add an explicit check for `HealthCategory.Unknown` in all four locations:

```csharp
// Pattern for the fix
if (category == HealthCategory.Unknown)
{
    return "Soil Health: Unknown";
}
else
{
    return $"Soil Health: {percentage}% ({category})";
}
```

---

## 8. Summary

| Aspect | Current Behavior | Recommended Behavior |
|--------|-----------------|---------------------|
| Tooltip text (unknown tile) | `"Soil Health: 0% (Unknown)"` (contradictory) | `"Soil Health: Unknown"` (clear) |
| Tooltip background | Gray (#808080) via `GetColorForHealth` | Gray (#808080) — no change needed |
| Tooltip text color | White | White — no change needed |
| Consistency with hoe feedback | Inconsistent (hoe shows "Unknown", tooltip shows "0% (Unknown)") | Consistent — both show "Soil Health: Unknown" |
| Consistency with overlay | Inconsistent (overlay is gray, tooltip implies 0%) | Consistent — both communicate "unknown" |

**The tooltip for unknown-health tiles should display `"Soil Health: Unknown"` without a percentage, matching the hoe feedback convention established in FR-003 and Session 2026-09-10.**

---

## 9. Sources

| # | Source | Location |
|---|--------|----------|
| 1 | `LivingRoots/Services/Visualization/VisualizationService.cs` | Lines 398-408 — `FormatTooltipText` produces "0% (Unknown)" for NaN |
| 2 | `LivingRoots/Services/Visualization/TooltipRenderer.cs` | Lines 107-118 — `GetTooltip` clamps NaN to 0, formats "0% (Unknown)" |
| 3 | `LivingRoots/Services/Visualization/HoeFeedbackRenderer.cs` | Lines 120-131 — `CreateFeedback` rounds NaN to 0, formats "0% (Unknown)" |
| 4 | `LivingRoots/Services/Visualization/VisualizationService.cs` | Lines 413-416 — `FormatHealthText` same pattern |
| 5 | `LivingRoots/Domain/Visualization/HealthCategory.cs` | Lines 7-13 — `Unknown = 3` enum value |
| 6 | `LivingRoots/Constants.cs` | Line 34 — `UnknownColor = Color.Gray` (#808080) |
| 7 | `LivingRoots/Services/Visualization/ColorInterpolationService.cs` | Lines 28-33 — NaN/Infinity → UnknownColor |
| 8 | `LivingRoots/Services/Visualization/ColorInterpolationService.cs` | Lines 49-50 — NaN/Infinity → Unknown category |
| 9 | `LivingRoots/Services/Visualization/OverlayRenderer.cs` | Lines 151-161 — Unknown overlay rendering |
| 10 | `LivingRoots/Domain/Visualization/PatternType.cs` | Line 27 — Unknown → None |
| 11 | `LivingRoots/Domain/Visualization/TooltipData.cs` | Lines 18-21 — `Format` method |
| 12 | `LivingRoots/Domain/IVisualizationService.cs` | Lines 23-29 — `RenderTooltip` contract |
| 13 | `specs/001-soil-health-visualization/spec.md` | Lines 39-43 — User Story 2 (known-health only) |
| 14 | `specs/001-soil-health-visualization/spec.md` | Lines 97-98 — Session 2026-09-10: hoe feedback = "Soil Health: Unknown" |
| 15 | `specs/001-soil-health-visualization/spec.md` | Lines 118-119 — Session 2026-09-08: single gray for all unknown |
| 16 | `specs/001-soil-health-visualization/spec.md` | Line 125 — Tooltip format convention |
| 17 | `specs/001-soil-health-visualization/spec.md` | Line 144 — FR-002 tooltip requirement |
| 18 | `specs/001-soil-health-visualization/spec.md` | Line 145 — FR-003 unknown hoe feedback = "Soil Health: Unknown" |
| 19 | `specs/001-soil-health-visualization/spec.md` | Line 163 — FR-014 gray overlay for unknown |
| 20 | `specs/001-soil-health-visualization/research/hoe-feedback-unknown-state.md` | Lines 227-233 — Prior research recommendation |
| 21 | `specs/001-soil-health-visualization/checklists/research-cursor-mapping.md` | Lines 22-31 — SMAPI ICursorPosition.Tile API |
| 22 | [Modding:Modder Guide/APIs/Input — Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input) — Official ICursorPosition documentation |
