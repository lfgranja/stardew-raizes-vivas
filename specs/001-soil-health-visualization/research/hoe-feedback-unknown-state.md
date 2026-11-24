# Research: Hoe Feedback on Tiles with Unknown Soil Health Data

**Date:** 2026-09-10
**Spec:** `001-soil-health-visualization/spec.md` — FR-003, FR-014
**Question:** When a player uses a hoe on a tile where soil health data hasn't loaded yet (the tile shows the gray "unknown" overlay per FR-014), what color should the flash effect use and what should the floating text display?

---

## 1. Executive Summary

The codebase already defines a complete "Unknown" state infrastructure: `HealthCategory.Unknown` enum, `ModConstants.UnknownColor` (Gray #808080), `PatternType.None` for unknown tiles, and `ColorInterpolationService` methods that return gray for NaN/Infinity health values. The spec (FR-014) mandates gray (#808080) for unknown overlays. The flash effect and floating text should use the same gray color and display "Unknown" for the category, maintaining visual consistency with the existing unknown overlay rendering.

**Recommendation:** Flash color = Gray (#808080), floating text = `"Soil Health: Unknown"` (without a misleading percentage).

---

## 2. Evidence from the Codebase

### 2.1 The "Unknown" State Is Already Defined

The `HealthCategory` enum includes `Unknown` as a first-class value:

```csharp
// LivingRoots/Domain/Visualization/HealthCategory.cs:7-13
public enum HealthCategory
{
    Poor = 0,
    Moderate = 1,
    Healthy = 2,
    Unknown = 3
}
```

The `ColorInterpolationService` resolves NaN/Infinity health values to `HealthCategory.Unknown` and `ModConstants.UnknownColor`:

```csharp
// LivingRoots/Services/Visualization/ColorInterpolationService.cs:28-33
if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
{
    var unknown = ModConstants.UnknownColor;
    unknown.A = (byte)(opacity * 255f);
    return unknown;
}
```

```csharp
// LivingRoots/Services/Visualization/ColorInterpolationService.cs:49-50
if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
    return HealthCategory.Unknown;
```

### 2.2 The Unknown Color Is Gray (#808080)

```csharp
// LivingRoots/Constants.cs:34
public static readonly Color UnknownColor = Color.Gray; // #808080
```

This matches FR-014's requirement for a neutral gray overlay:

> **FR-014**: System MUST render a neutral gray overlay for tiles where soil health data is unavailable or hasn't loaded yet. […] Load farm before soil health data is available and confirm gray (#808080) overlay renders. [`spec.md:162`]

### 2.3 The OverlayRenderer Uses PatternType.None for Unknown Tiles

```csharp
// LivingRoots/Services/Visualization/OverlayRenderer.cs:151-159
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

The `PatternTypeExtensions` confirms this mapping:

```csharp
// LivingRoots/Domain/Visualization/PatternType.cs:27
HealthCategory.Unknown => PatternType.None,
```

### 2.4 The Flash Effect Already Delegates to ColorInterpolationService

The `HoeFeedbackRenderer` uses `GetColorForHealth` for the flash color:

```csharp
// LivingRoots/Services/Visualization/HoeFeedbackRenderer.cs:65
Color flashColor = _colorInterpolationService.GetColorForHealth(feedback.HealthValue, opacity);
```

The `VisualizationService.DrawHoeFeedback` does the same:

```csharp
// LivingRoots/Services/Visualization/VisualizationService.cs:366
var flashColor = _colorService.GetColorForHealth(feedback.HealthValue, config.Opacity);
```

Since `GetColorForHealth` returns `ModConstants.UnknownColor` for NaN/Infinity, **the flash will already render as gray (#808080) for unknown tiles** — no code change needed for the flash color.

### 2.5 The Floating Text Format Has a Problem for Unknown Values

The `HoeFeedbackRenderer.CreateFeedback` method formats text as:

```csharp
// LivingRoots/Services/Visualization/HoeFeedbackRenderer.cs:120-132
HealthCategory category = _colorInterpolationService.GetCategoryForHealth(healthValue);
int percentage = (int)Math.Clamp(Math.Round(healthValue), 0f, 100f);

return new HoeFeedback
{
    // ...
    HealthText = $"Soil Health: {percentage}% ({category})"
};
```

For `healthValue = NaN`:
- `category` = `HealthCategory.Unknown` → displays as `"Unknown"`
- `percentage` = `(int)Math.Clamp(Math.Round(NaN), 0f, 100f)` = `0` (NaN rounds to 0 in .NET)

This produces: `"Soil Health: 0% (Unknown)"` — a contradictory message that implies both "0% health" (Poor category) and "Unknown" simultaneously.

The same issue exists in `VisualizationService.FormatTooltipText`:

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

For NaN, this produces: `"Soil Health: 0% (Unknown)"` — same contradiction.

---

## 3. Evidence from the Spec

### 3.1 FR-003 (Hoe Feedback)

> **FR-003**: System MUST show flash effect (colored overlay flash at the tile's health category color with alpha decreasing from 1.0 to 0.0 over 300ms) and floating text when a hoe is used on tilled soil tiles [`spec.md:144`]

The phrase "at the tile's health category color" is ambiguous for unknown tiles. However, the research on flash effects clarifies:

> The flash uses the interpolated health category color (Poor=red, Moderate=yellow, Healthy=green) with a linear fade-out over 300ms. […] The existing `HoeFeedbackRenderer.cs` and `VisualizationService.cs` both implement **colored overlay matching context**. [`checklists/research-flash-effect.md:86-96`]

For unknown tiles, the "health category color" is `ModConstants.UnknownColor` (Gray #808080), which is already returned by `GetColorForHealth` for NaN/Infinity values.

### 3.2 FR-014 (Unknown Overlay)

> **FR-014**: System MUST render a neutral gray overlay for tiles where soil health data is unavailable or hasn't loaded yet. […] confirm gray (#808080) overlay renders. [`spec.md:162`]

### 3.3 Tooltip Format Convention

> **Tooltip format**: "Soil Health: {percentage}% ({category})" (e.g., "Soil Health: 75% (Healthy)") [`spec.md:125`]

The spec defines the format with `{percentage}` and `{category}` placeholders. For unknown data, the category is "Unknown" (from `HealthCategory.Unknown` enum name). The percentage is undefined.

### 3.4 Clarification: Single Gray for All Unknown States

> **Session 2026-09-08**: Q: Should the spec distinguish between "data is loading" (transient) and "data will never be available" (permanent) visually, or is a single gray overlay sufficient for both? → A: Single gray overlay — both loading and no-data states show the same neutral gray (#808080) overlay. [`spec.md:118`]

This confirms the design intent: one visual treatment for all unknown states.

---

## 4. Evidence from Stardew Valley / SMAPI Conventions

### 4.1 TemporaryAnimatedSprite for Floating Text

The `CompostApplicationService` demonstrates the standard Stardew Valley pattern for floating text:

```csharp
// LivingRoots/Services/CompostApplicationService.cs:59-72
var sprite = new TemporaryAnimatedSprite(
    null,
    Rectangle.Empty,
    1000f,
    1,
    1,
    worldPosition,
    false,
    false);
sprite.text = $"+{ModConstants.RestorationAmount:F0}";
sprite.color = Color.Green;
sprite.alphaFade = 0.02f;
sprite.motion = new Vector2(0, -0.5f);
location.TemporarySprites.Add(sprite);
```

Key properties: `.text` (string), `.color` (Color), `.alphaFade` (float), `.motion` (Vector2). The `HoeFeedbackRenderer` uses `SpriteBatch.DrawString` instead, which is also valid.

### 4.2 HUDMessage for Unknown/Error States

The `HUDMessage` class uses `Color.Gray` (#808080) as a neutral color for status messages. The `error_type` with `noIcon = true` is the standard pattern for info notifications [`research-notification-mechanisms.md:90-112`].

### 4.3 No Convention for "Unknown" Action Feedback

Stardew Valley's base game does not define visual feedback for actions on tiles with unknown/missing state. The game's own systems (crop quality stars, cactus flower color) always have deterministic values. The mod must define its own convention.

---

## 5. Analysis: What Should Happen

### 5.1 Flash Effect Color

**Recommendation: Gray (#808080) — same as the unknown overlay color.**

Rationale:
- The flash color should match the persistent overlay color on the tile. Since the tile shows a gray overlay (FR-014), a gray flash maintains visual consistency.
- The `ColorInterpolationService` already returns `ModConstants.UnknownColor` for NaN/Infinity, so the flash will naturally render gray without code changes.
- This follows the "colored flash matching context" principle established in the flash effect research [`checklists/research-flash-effect.md:130-140`].

### 5.2 Floating Text Content

**Recommendation: `"Soil Health: Unknown"` (without percentage).**

Rationale:
- Displaying `"Soil Health: 0% (Unknown)"` is contradictory and misleading. 0% implies Poor health, not unknown data.
- The category name "Unknown" already communicates the state clearly.
- Omitting the percentage avoids implying a specific health value where none exists.
- This is consistent with the spec's clarification that unknown = "no info" regardless of reason [`spec.md:118`].

Alternative considered: `"Soil Health: —% (Unknown)"` — uses an em dash to indicate missing data. More explicit but adds visual clutter.

### 5.3 Floating Text Color

**Recommendation: Gray (#808080) — same as the flash and overlay.**

Rationale:
- The text color should match the flash color for visual consistency.
- `GetColorForHealth` already returns gray for NaN/Infinity, so no code change needed.

---

## 6. Implementation Gap

The current `HoeFeedbackRenderer.CreateFeedback` and `VisualizationService.FormatTooltipText` methods do not handle the Unknown case specially. They will produce `"Soil Health: 0% (Unknown)"` which is misleading.

**Required fix:** Add an explicit check for `HealthCategory.Unknown` in the text formatting logic:

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

This fix should be applied in:
1. `HoeFeedbackRenderer.CreateFeedback` (line 131)
2. `VisualizationService.FormatTooltipText` (line 407)
3. `VisualizationService.FormatHealthText` (line 415)

---

## 7. Summary

| Aspect | Current Behavior | Recommended Behavior |
|--------|-----------------|---------------------|
| Flash color | Gray (#808080) via `GetColorForHealth` | Gray (#808080) — no change needed |
| Flash alpha | 1.0 → 0.0 over 300ms | 1.0 → 0.0 over 300ms — no change needed |
| Floating text | `"Soil Health: 0% (Unknown)"` (misleading) | `"Soil Health: Unknown"` (clear) |
| Text color | Gray (#808080) via `GetColorForHealth` | Gray (#808080) — no change needed |
| Text duration | 1000ms | 1000ms — no change needed |

**The flash effect already works correctly** — the `ColorInterpolationService` returns gray for unknown health values. **The floating text needs a small fix** to avoid displaying a misleading "0% (Unknown)" message.

---

## 8. Sources

| # | Source | Location |
|---|--------|----------|
| 1 | `LivingRoots/Domain/Visualization/HealthCategory.cs` | Lines 7-13 — `Unknown = 3` enum value |
| 2 | `LivingRoots/Constants.cs` | Line 34 — `UnknownColor = Color.Gray` (#808080) |
| 3 | `LivingRoots/Services/Visualization/ColorInterpolationService.cs` | Lines 28-33 — NaN/Infinity → UnknownColor |
| 4 | `LivingRoots/Services/Visualization/ColorInterpolationService.cs` | Lines 49-50 — NaN/Infinity → Unknown category |
| 5 | `LivingRoots/Services/Visualization/OverlayRenderer.cs` | Lines 151-159 — Unknown overlay rendering |
| 6 | `LivingRoots/Domain/Visualization/PatternType.cs` | Line 27 — Unknown → None |
| 7 | `LivingRoots/Services/Visualization/HoeFeedbackRenderer.cs` | Line 65 — Flash uses GetColorForHealth |
| 8 | `LivingRoots/Services/Visualization/HoeFeedbackRenderer.cs` | Lines 120-132 — CreateFeedback text format |
| 9 | `LivingRoots/Services/Visualization/VisualizationService.cs` | Lines 366, 398-408, 413-416 — Flash + text formatting |
| 10 | `LivingRoots/Services/CompostApplicationService.cs` | Lines 59-72 — TemporaryAnimatedSprite pattern |
| 11 | `specs/001-soil-health-visualization/spec.md` | Lines 118, 125, 144, 162 — FR-003, FR-014, tooltip format |
| 12 | `specs/001-soil-health-visualization/checklists/research-flash-effect.md` | Lines 86-96, 130-140 — Colored flash convention |
| 13 | `specs/001-soil-health-visualization/research-notification-mechanisms.md` | Lines 90-112 — HUDMessage conventions |
| 14 | [Modding:Common tasks — Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Common_tasks) — HUDMessage API documentation |
