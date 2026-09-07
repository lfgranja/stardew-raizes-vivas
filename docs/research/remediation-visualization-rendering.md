# Visualization Rendering Implementation Research

**Date:** 2026-09-06
**Branch:** feat/002-04-composting-bin-service
**Related Spec:** Visualization Rendering Implementation Spec

---

## Executive Summary

The Living Roots mod's visualization layer has a fully functional overlay rendering pipeline, but **tooltip text and hoe feedback floating text are invisible to players** — `DrawString` calls are commented out. Two additional issues were discovered: hardcoded viewport bounds in `OnRenderedWorld` break overlay positioning, and a `SmallFont` property exists but is never assigned.

## Current State Assessment

### What Works

| Component | File | Status |
|-----------|------|--------|
| `OverlayRenderer.GetOverlays()` | `Services/Visualization/OverlayRenderer.cs` | ✅ Viewport culling, caching, graceful degradation |
| `ColorInterpolationService` | `Services/Visualization/ColorInterpolationService.cs` | ✅ Health→color mapping with caching |
| `VisualizationService.RenderOverlays()` | `Services/Visualization/VisualizationService.cs` | ✅ Draws tile overlays |
| `VisualizationService.DrawOverlay()` | `Services/Visualization/VisualizationService.cs` | ✅ Actual overlay draw calls |
| Hoe feedback flash effect | `Services/Visualization/VisualizationService.cs:355-371` | ✅ Colored rectangle with fading alpha |

### What's Broken/Stubbed

| Component | File | Issue |
|-----------|------|-------|
| Tooltip text rendering | `VisualizationService.cs:339` | `DrawString` commented out |
| Floating text rendering | `VisualizationService.cs:381` | Only logs, no draw call |
| Viewport offset | `ModController.cs:727` | Hardcoded `new Rectangle(0, 0, 100, 100)` |
| Cursor position | `ModController.cs:730` | Hardcoded `new Vector2(0, 0)` |
| SmallFont assignment | `VisualizationTextures.cs:8` | Property declared, never set |

---

## Detailed Findings

### 1. Tooltip Rendering (FR-001)

**Location:** `VisualizationService.cs:319-341` (`DrawTooltip` method)

**Current behavior:**
- Draws a black semi-transparent background rectangle (`Color(0, 0, 0, 200)`)
- Position: cursor + (16, 16) offset
- Size: estimated `text.Length * 8 + 16` wide, `24` tall
- **Does NOT render text** — only logs at `LogLevel.Trace`

**Commented-out code (line 339):**
```csharp
// spriteBatch.DrawString(Game1.smallFont, text, new Vector2(tooltipX + 8, tooltipY + 4), textColor);
```

**Required fix:** Uncomment line 339. Use `Game1.smallFont` directly (confirmed accessible in render context — `HoeFeedbackRenderer.cs:107` already uses it).

**Content format** is already implemented in `FormatTooltipText` (lines 398-408):
```
"Soil Health: {healthValue:F0}% ({category})"
```

Categories: Poor (0-33), Moderate (34-66), Healthy (67-100), Unknown (otherwise).

### 2. Hoe Feedback Flash (FR-002)

**Location:** `VisualizationService.cs:346-383` (`DrawHoeFeedback` method)

**Status:** ✅ Already works correctly.

```csharp
if (elapsedMs < feedback.FlashDuration)
{
    var flashRect = new Rectangle(
        feedback.TilePosition.X * TileSize,
        feedback.TilePosition.Y * TileSize,
        TileSize, TileSize);
    var flashColor = _colorService.GetColorForHealth(feedback.HealthValue, config.Opacity);
    var alpha = (byte)(255f * (1f - (float)elapsedMs / feedback.FlashDuration));
    flashColor.A = alpha;
    spriteBatch.Draw(GetTexture(), flashRect, flashColor);
}
```

Alpha decay formula matches spec: `255 * (1 - elapsedMs / FlashDuration)`.

### 3. Floating Text (FR-003)

**Location:** `VisualizationService.cs:372-381`

**Current behavior:** Position calculated but only logged.

```csharp
var textY = feedback.TilePosition.Y * TileSize - (float)elapsedMs * 0.02f;
var textPos = new Vector2(
    feedback.TilePosition.X * TileSize + TileSize / 2,
    textY);
_monitor.Log($"Hoe feedback text: {feedback.HealthText} at ({textPos.X}, {textPos.Y})", LogLevel.Trace);
```

**Required fix:** Add `DrawString` call after line 381.

**Reference implementation exists in `HoeFeedbackRenderer.cs:102-107`:**
```csharp
var textSize = Game1.smallFont.MeasureString(feedback.HealthText);
var textPos = new Vector2(
    screenPos.X + (TileSize / 2) - (textSize.X / 2),
    screenPos.Y - (float)elapsedMs * 0.02f);
spriteBatch.DrawString(Game1.smallFont, feedback.HealthText, textPos, textColor);
```

### 4. Initialization Guard (FR-004)

**Location:** `VisualizationService.cs:39-47`

```csharp
public void Initialize(Texture2D whiteTexture)
{
    _whiteTexture = whiteTexture ?? throw new ArgumentNullException(nameof(whiteTexture));
}

private Texture2D GetTexture()
{
    return _whiteTexture ?? throw new InvalidOperationException(
        "VisualizationService not initialized. Call Initialize() first.");
}
```

**Status:** Guard exists — `GetTexture()` throws if `_whiteTexture` is null. Render methods that call `GetTexture()` for the background rectangle will throw, but the spec requires they no-op instead.

**Required fix:** Add null checks at the top of `DrawTooltip` and `DrawHoeFeedback`:
```csharp
if (_whiteTexture == null) return;
```

### 5. Viewport Offset Issue (Critical Bug)

**Location:** `ModController.cs:727`

```csharp
var viewport = new Microsoft.Xna.Framework.Rectangle(0, 0, 100, 100);
```

**Impact:** Overlay rendering uses `viewport.X` and `viewport.Y` for screen coordinate calculation:
```csharp
var screenX = (overlay.TilePosition.X - viewport.X) * TileSize;
var screenY = (overlay.TilePosition.Y - viewport.Y) * TileSize;
```

With viewport `(0, 0, 100, 100)`, only tiles at positions 0–100 render correctly. Any tile outside this range is offset incorrectly.

**Required fix:** Use `Game1.viewport` (or `Game1.gameViewport`) instead of hardcoded values.

### 6. Cursor Position Issue

**Location:** `ModController.cs:730`

```csharp
_visualizationService.RenderTooltip(e.SpriteBatch, new Microsoft.Xna.Framework.Vector2(0, 0), gameTime);
```

The cursor position is hardcoded to `(0, 0)` instead of reading the actual cursor position.

**Required fix:** Use `_helper.Input.GetCursorPosition()` (already used elsewhere in the file at line 751).

### 7. SmallFont Property

**Location:** `VisualizationTextures.cs:8`

```csharp
public static SpriteFont? SmallFont { get; set; }
```

**Status:** Declared but never assigned. Natural assignment location is `OnRenderedWorld` (line 720) alongside `WhiteTexture` initialization.

### 8. Configuration Toggles

**File:** `Domain/Visualization/VisualizationConfiguration.cs`

| Property | Default | Purpose |
|----------|---------|---------|
| `TooltipsEnabled` | `true` | Separate flag for tooltip rendering |
| `HoeFeedbackEnabled` | `true` | Separate flag for hoe feedback effects |
| `OverlaysEnabled` | `true` | Master toggle for overlays |

**Existing check patterns in `VisualizationService.cs`:**
- Line 137: `if (!config.TooltipsEnabled)` — skips tooltip rendering
- Line 184: `if (!config.HoeFeedbackEnabled)` — skips hoe feedback rendering

Both flags are already wired into the render pipeline. Enabling/disabling in config will work correctly once the draw calls are uncommented.

---

## Implementation Checklist

| # | Change | File | Lines | Effort |
|---|--------|------|-------|--------|
| 1 | Uncomment `DrawString` in `DrawTooltip` | `VisualizationService.cs` | 339 | Trivial |
| 2 | Add `DrawString` for floating text in `DrawHoeFeedback` | `VisualizationService.cs` | After 381 | Trivial |
| 3 | Add null guard for `_whiteTexture` in `DrawTooltip` | `VisualizationService.cs` | Before 325 | Trivial |
| 4 | Add null guard for `_whiteTexture` in `DrawHoeFeedback` | `VisualizationService.cs` | Before 355 | Trivial |
| 5 | Replace hardcoded viewport with `Game1.viewport` | `ModController.cs` | 727 | Trivial |
| 6 | Replace hardcoded cursor with `_helper.Input.GetCursorPosition()` | `ModController.cs` | 730 | Trivial |
| 7 | Set `VisualizationTextures.SmallFont = Game1.smallFont` | `ModController.cs` | After 724 | Trivial |

---

## References

- `LivingRoots/Services/Visualization/VisualizationService.cs` — Main render orchestrator
- `LivingRoots/Controllers/ModController.cs` — Event wiring, `OnRenderedWorld` handler
- `LivingRoots/Services/Visualization/VisualizationTextures.cs` — Texture/font storage
- `LivingRoots/Services/Visualization/ColorInterpolationService.cs` → `GetColorForHealth()`
- `LivingRoots/Domain/Visualization/VisualizationConfiguration.cs` — Toggle properties
- `LivingRoots/Domain/Visualization/HealthCategory.cs` — Category enum + `FromHealthValue()`
- `LivingRoots/Services/Visualization/HoeFeedbackRenderer.cs` — Reference for working `DrawString` pattern
