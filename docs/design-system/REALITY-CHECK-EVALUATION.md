# Living Roots Design System — Reality Check Evaluation

> **Date:** 2026-09-09
> **Evaluator:** AI-assisted analysis against Stardew Valley modding reality
> **Scope:** `LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` v1.0.0 vs actual SMAPI/XNA/MonoGame constraints, existing mod patterns, and game accessibility standards
> **Status:** 17 findings — 6 Critical, 7 Major, 4 Minor

---

## Executive Summary

The Living Roots UI-UX Design System is a well-structured document that demonstrates strong design thinking, but it was written with assumptions more typical of **web application development** than **Stardew Valley mod development**. Several fundamental technical claims about the game's rendering pipeline, threading model, coordinate systems, and accessibility standards are incorrect or misaligned with reality. The document also references a `ModConstants.cs` file that does not exist, and specifies color values that conflict with the actual codebase defaults.

**Bottom line:** The design philosophy (progressive disclosure, pixel fidelity, accessibility-first) is sound and well-aligned with the mod's goals. The implementation details need significant correction before they can guide development.

---

## Critical Findings

### C1. `ModConstants.cs` Does Not Exist

**Design system claims:** All color values, opacity limits, debounce times, and other constants live in `ModConstants.cs` — "never hardcode colors in rendering code."

**Reality:** The file `ModConstants.cs` does not exist anywhere in the codebase. A glob search (`**/ModConstants.cs`) returns zero results. The code references `ModConstants.PoorColor`, `ModConstants.PatternMinOpacity`, `ModConstants.TooltipDebounceMs`, `ModConstants.UnknownColor`, and `ModConstants.DefaultOpacity` — but these symbols are undefined. **The project will not compile.**

**Impact:** Every file that references `ModConstants` is currently broken. This includes `VisualizationService.cs`, `OverlayRenderer.cs`, `ColorInterpolationService.cs`, and others.

**Recommendation:** Create `ModConstants.cs` as a static class with all constants defined. This is the single highest-priority fix.

---

### C2. Threading Model Is Incorrect — No Separate Render Thread

**Design system claims:** The architecture uses separate "Game Thread" and "Render Thread" with `lock(_dataLock)` synchronization between them. Section 5.4 shows a diagram with concurrent game and render threads.

**Reality:** Stardew Valley uses MonoGame, which has a **single-threaded game loop**. The SMAPI wiki explicitly states:

> "Your event handlers are run **synchronously**: the game is paused and no other mod's code will run simultaneously, so there's no risk of conflicting changes." [^1]

MonoGame's `Game.Update()` and `Game.Draw()` run sequentially on the same thread. There is no separate render thread. The elaborate lock-based thread safety between "game thread" and "render thread" is unnecessary for single-player.

**Caveat:** Some thread safety IS needed for:
- SMAPI's `UnvalidatedUpdateTicked` event (explicitly documented as not thread-safe)
- Multiplayer scenarios where farmhand data syncs asynchronously
- The `Mutex` used by UI Info Suite 2's `ShowItemEffectRanges` for cross-thread tile data access

**Impact:** The lock-based approach adds unnecessary complexity for the common case. The `lock(_dataLock)` pattern is harmless but misleading — it suggests a threading model that doesn't exist.

**Recommendation:** Simplify the threading story. Use `lock` only where genuinely needed (multiplayer sync, `PerScreen<T>` state). Document that Stardew Valley is single-threaded and SMAPI events are synchronous.

---

### C3. Tile Size Is Not Fixed at 64px — Zoom and UI Scale Break This

**Design system claims:** "Tile size: 64×64 pixels (matches `Game1.tileSize`)" and `public const int TileSize = 64`.

**Reality:** `Game1.tileSize` is 64 **only at 100% zoom**. The game supports:
- **Zoom level:** 75% to 200% (`Game1.options.zoomLevel`) [^2]
- **UI scale:** 75% to 150% (separate from zoom) [^2]

The actual rendered tile size varies. UI Info Suite 2 handles this correctly using:
```csharp
Utility.ModifyCoordinateFromUIScale(Game1.tileSize)  // accounts for UI scale
Utility.ModifyCoordinateForUIScale(Game1.pixelZoom)   // accounts for zoom
Game1.GlobalToLocal(position)                          // accounts for viewport
```

The design system's hardcoded `TileSize = 64` and manual viewport subtraction `(overlay.TilePosition.X - viewport.X) * TileSize` will produce **incorrect positions** at any zoom level other than 100%.

**Impact:** Overlays will be misaligned when players use non-default zoom levels (which is common — many players use 75% zoom for a wider view, or 200% on 4K displays).

**Recommendation:** Use `Game1.tileSize` dynamically, apply `Utility.ModifyCoordinatesForUIScale()` for coordinate conversion, and use `Game1.GlobalToLocal()` for viewport handling. Never hardcode 64.

---

### C4. Color Defaults in Code Conflict with Design System

**Design system claims:** Carefully chosen earth tones:
| Category | Hex | RGB |
|----------|-----|-----|
| Poor | `#B91C1C` | `(185, 28, 28)` |
| Moderate | `#D97706` | `(217, 119, 6)` |
| Healthy | `#15803D` | `(21, 128, 61)` |
| Unknown | `#6B7280` | `(107, 114, 128)` |

**Reality:** `VisualizationConfiguration.cs` defines defaults as pure RGB:
```csharp
public ColorDTO PoorColor { get; set; } = new ColorDTO { R = 255, G = 0, B = 0, A = 255 };      // Pure red
public ColorDTO ModerateColor { get; set; } = new ColorDTO { R = 255, G = 255, B = 0, A = 255 }; // Pure yellow
public ColorDTO HealthyColor { get; set; } = new ColorDTO { R = 0, G = 255, B = 0, A = 255 };    // Pure green
public ColorDTO UnknownColor { get; set; } = new ColorDTO { R = 128, G = 128, B = 128, A = 255 }; // Gray
```

These are **not** the design system's earth tones. Pure red/yellow/green are visually aggressive and break the "pixel fidelity" and "ecological authenticity" design pillars.

**Impact:** If the config file is missing or colors are reset to defaults, the mod will display pure RGB colors that clash with Stardew Valley's aesthetic.

**Recommendation:** Update `VisualizationConfiguration.cs` defaults to match the design system's earth tones. Also ensure `ModConstants.cs` (once created) uses the same values.

---

### C5. WCAG 2.1 AA Is the Wrong Standard for a Game Mod

**Design system claims:** "Living Roots visualization targets WCAG 2.1 Level AA compliance" and lists WCAG criteria (1.4.1, 1.4.3, 1.4.11, 2.1.1, 2.4.7).

**Reality:** WCAG is a **web content** accessibility standard. The relevant standard for games is the [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com) [^3], which states:

> "Ensure no essential information is conveyed by a fixed colour alone" [^4]

The game accessibility guidelines recommend:
- Patterns, icons, labels, text as redundant cues (aligns with design system)
- Allowing color customization (design system has this)
- Orange vs blue as a colorblind-friendly default pair (design system uses red/amber/green)
- Testing with CVD simulators (design system mentions this)

**Impact:** While the design system's accessibility intentions are correct, citing WCAG creates a false impression of compliance with a standard that doesn't apply. A player or reviewer familiar with game accessibility will notice the mismatch.

**Recommendation:** Replace WCAG references with Game Accessibility Guidelines. Keep the same technical requirements (redundant encoding, contrast, patterns) but cite the correct standard.

---

### C6. `prefers-reduced-motion` Does Not Exist in Stardew Valley

**Design system claims:** "All animations respect `prefers-reduced-motion` (future-proofing for Stardew Valley updates)."

**Reality:** `prefers-reduced-motion` is a **CSS media query** (`@media (prefers-reduced-motion: reduce)`) for web browsers. It does not exist in MonoGame, XNA, or Stardew Valley. There is no mechanism for the game to signal reduced-motion preferences to mods.

**Impact:** This is dead documentation that describes a feature that cannot be implemented. It also suggests the design system author may be thinking in web terms rather than game terms.

**Recommendation:** Replace with a simple config option `ReduceMotion` (bool) that players can toggle. This is the standard approach for games — let the player choose, don't try to detect a system setting that doesn't exist.

---

## Major Findings

### M1. Coordinate System Handling Is Incomplete

**Design system claims:** Overlays are positioned using `(overlay.TilePosition.X - viewport.X) * TileSize`.

**Reality:** Stardew Valley has three coordinate systems (tile, absolute, screen) with viewport-based conversions [^2]. The correct approach is:
```csharp
// UI Info Suite 2's approach:
Vector2 screenPos = Utility.ModifyCoordinatesForUIScale(
    Game1.GlobalToLocal(tilePosition * Game1.tileSize)
);
```

The design system's manual viewport subtraction doesn't account for:
- Zoom level scaling
- UI scale scaling
- `Game1.GlobalToLocal()` which handles viewport + zoom automatically

**Impact:** Overlays will be misaligned at non-default zoom levels.

---

### M2. Color Interpolation Description Doesn't Match Code

**Design system claims:** "Linear RGB interpolation within each band" — Poor band interpolates from `#7F1D1C` → `#B91C1C`, Moderate from `#D97706` → `#F59E0B`, Healthy from `#15803D` → `#22C55E`.

**Reality:** The actual `ColorInterpolationService.cs` interpolates **between category colors**, not within bands:
- Poor [0,34): Interpolates from `_poorColor` → `_moderateColor`
- Moderate [34,67): Interpolates from `_moderateColor` → `_healthyColor`
- Healthy [67,100]: Interpolates from `_moderateColor` → `_healthyColor` (note: this looks like a bug — it interpolates from Moderate to Healthy, not within the Healthy band)

Additionally, linear RGB interpolation between red (`#B91C1C`) and green (`#15803D`) passes through desaturated brown/gray in the middle, which looks muddy in pixel art.

**Impact:** The visual result is different from what the design system describes. The "gradient effect within each band" doesn't exist — instead there's a gradient across the entire 0-100 range.

---

### M3. Tooltip Rendering Is Incomplete

**Design system claims:** Tooltips use `Game1.smallFont`, black background with alpha 200, white text, 8px/4px padding, 16px cursor offset.

**Reality:** The actual `VisualizationService.DrawTooltip()` method has the `spriteBatch.DrawString()` call **commented out**:
```csharp
// Note: Text rendering would use Game1.smallFont or similar in a full implementation
// spriteBatch.DrawString(Game1.smallFont, text, new Vector2(tooltipX + 8, tooltipY + 4), textColor);
_monitor.Log($"Tooltip rendered: {text}", LogLevel.Trace);
```

Meanwhile, UI Info Suite 2 uses the game's built-in tooltip renderer:
```csharp
IClickableMenu.drawHoverText(Game1.spriteBatch, text, Game1.smallFont, overrideX, overrideY);
```

**Impact:** Tooltips don't actually render text in the current implementation.

---

### M4. TileOverlay Uses XNA Color in Domain Layer — Architecture Violation

**Design system claims:** Domain layer is framework-agnostic.

**Reality:** `TileOverlay.cs` (in `Domain/Visualization/`) imports and uses `Microsoft.Xna.Framework.Color` directly. The `Domain/Visualization/AGENTS.md` explicitly states: "Keep all types SMAPI/XNA-free."

**Impact:** This violates the DDD layering principle. The domain layer should not depend on XNA/MonoGame types.

**Recommendation:** Use `ColorDTO` (which already exists) in `TileOverlay` instead of `Color`. Convert to XNA `Color` at the service layer boundary.

---

### M5. Pattern Rendering Doesn't Scale with Zoom

**Design system claims:** Stripes are 8px wide, dots are 8×8px with 16px spacing.

**Reality:** These are fixed pixel sizes. At 200% zoom, tiles are 128px but patterns would still be 8px — appearing too small. At 75% zoom, tiles are 48px but patterns would be 8px — appearing too large.

UI Info Suite 2 scales sprites using `Game1.pixelZoom`:
```csharp
spriteBatch.Draw(texture, position, sourceRect, color, 0f, Vector2.Zero,
    Utility.ModifyCoordinateForUIScale(Game1.pixelZoom), SpriteEffects.None, 0.01f);
```

**Impact:** Patterns will look inconsistent across zoom levels.

---

### M6. HoeFeedbackRenderer Duplicates TileSize Constant

**Design system claims:** "No hardcoded values — everything in `ModConstants`."

**Reality:** `HoeFeedbackRenderer.cs` defines `private const int TileSize = 64;` — a separate copy of the same constant in `VisualizationService.TileSize`. This creates a maintenance hazard.

**Impact:** If tile size ever changes (e.g., to support zoom), both constants must be updated manually.

---

### M7. ColorInterpolationService Cache Key Is Fragile

**Design system claims:** Color interpolation uses caching for performance.

**Reality:** The cache key uses bitwise OR: `((int)(clamped * 100)) | ((int)(opacity * 100) << 17)`. This is fragile — it assumes opacity fits in 17 bits and doesn't collide with the health value bits. A `ValueTuple<int, int>` key would be safer and more readable.

**Impact:** Potential cache collisions causing incorrect colors, though unlikely in practice.

---

## Minor Findings

### m1. Non-Diegetic UI Reference Is Appropriate

Research confirmed that non-diegetic UI is a valid and well-applied concept for Stardew Valley. The Medium article by Nicolas Kraj (UX Director at Ubisoft) is a credible source. Stardew Valley's existing UI (health bar, stamina, toolbar, money, time) is classic non-diegetic UI. **No change needed.**

---

### m2. Font Size Claims Are Approximate

The design system says `Game1.smallFont` is "~14px" and `Game1.tinyFont` is "~10px". These are approximations. The actual sizes depend on the game's font files and zoom level. The `Game1.smallFont` is actually a `SpriteFont` with a specific line spacing that varies by language (the game supports localization with different font metrics). **Recommendation: use `Game1.smallFont.LineSpacing` and `MeasureString()` at runtime instead of hardcoded sizes.**

---

### m3. Configuration Persistence Path Mismatch

The design system says config is saved to `data/{saveId}/visualization-config.json`. The actual code uses `viz_config_{saveId}` as the SMAPI data key. The design system also doesn't mention the debounced save pattern (500ms debounce in Section 10.3).

---

### m4. Performance Budgets May Be Over-Optimistic

The design system budgets <4ms for all visualization including up to 400 draw calls. The pattern rendering alone (8 draw calls per stripe tile × 100 tiles = 800 draw calls) could exceed this budget. UI Info Suite 2 uses a `Mutex` for thread safety, which is heavier than the `lock` approach but more appropriate for cross-thread access.

---

### m5. Linear RGB Interpolation Produces Muddy Colors

Research confirms that linear RGB interpolation (which the design system specifies) is **not perceptually uniform**. Interpolating between red (`#B91C1C`) and green (`#15803D`) passes through desaturated brown-gray in the middle — the "gray zone" problem. For pixel art, this looks muddy and unattractive. **Recommendation: use HSL hue-interpolation or CIELAB for wide hue ranges; or use `Color.Lerp` only for similar hues within bands.**

---

### m6. Pattern Textures Should Be White + Tint

The design system describes drawing patterns with the category color directly. Research shows the more performant approach is to create small white-on-transparent pattern textures once, then tint at draw time: `spriteBatch.Draw(texture, rect, color * opacity)`. This allows a single texture to serve all categories and supports `SamplerState.PointWrap` for seamless tiling.

---

## What the Design System Gets Right

Despite the issues above, several aspects of the design system are well-aligned with reality:

1. **Progressive disclosure** (Ambient → Focused → Active → Detailed) is an excellent pattern for game UI and matches how UI Info Suite 2 works.

2. **Pixel fidelity** emphasis is correct — `SamplerState.PointClamp` and integer positioning are the right approach for pixel art.

3. **Redundant encoding** (color + pattern + text) is the gold standard for game accessibility, endorsed by gameaccessibilityguidelines.com.

4. **Tooltip debounce** (50ms) is a reasonable throttle that prevents excessive updates during fast cursor movement.

5. **Graceful degradation** (>1000 tiles → disable patterns) is a sensible performance strategy.

6. **Console commands** for configuration follow SMAPI conventions and match the project's existing patterns.

7. **The color philosophy** (earth tones, avoiding visual aggression) is well-reasoned and appropriate for Stardew Valley's aesthetic.

8. **SpriteBatch usage** (`BlendState.AlphaBlend`, `SamplerState.PointClamp`) is technically correct for XNA/MonoGame.

---

## Recommendations Summary

| Priority | Finding | Action |
|----------|---------|--------|
| **P0** | C1: `ModConstants.cs` missing | Create the file with all constants |
| **P0** | C4: Color defaults mismatch | Update `VisualizationConfiguration.cs` defaults |
| **P1** | C2: Threading model wrong | Simplify to single-threaded story |
| **P1** | C3: Tile size not fixed | Use `Game1.tileSize` + zoom/UI scale |
| **P1** | C5: WCAG is wrong standard | Cite Game Accessibility Guidelines |
| **P1** | C6: `prefers-reduced-motion` invalid | Replace with config toggle |
| **P2** | M1: Coordinate system incomplete | Use `Game1.GlobalToLocal()` + UI scale |
| **P2** | M2: Interpolation description wrong | Fix description or fix code |
| **P2** | M3: Tooltip rendering incomplete | Implement actual text rendering |
| **P2** | M4: XNA Color in domain layer | Use `ColorDTO` in domain |
| **P2** | M5: Patterns don't scale | Use `Game1.pixelZoom` for patterns |
| **P3** | M6: Duplicate TileSize constant | Reference `ModConstants.TileSize` |
| **P3** | M7: Fragile cache key | Use `ValueTuple` key |

---

## Sources

[^1]: Stardew Valley Wiki. "Modding:Modder Guide/APIs/Events." https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events — Confirms SMAPI events are synchronous: "the game is paused and no other mod's code will run simultaneously."

[^2]: Stardew Valley Wiki. "Modding:Modder Guide/Game Fundamentals." https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals — Documents zoom level (75%-200%), UI scale (75%-150%), coordinate systems, and `Utility.ModifyCoordinatesForUIScale()`.

[^3]: Game Accessibility Guidelines. https://gameaccessibilityguidelines.com — The standard reference for game accessibility, not WCAG.

[^4]: Game Accessibility Guidelines. "Ensure no essential information is conveyed by a fixed colour alone." https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/ — Recommends redundant encoding (patterns, icons, labels).

[^5]: UI Info Suite 2. https://github.com/Annosz/UIInfoSuite2 — Reference implementation showing how a production Stardew Valley mod handles rendering, tooltips, coordinate conversion, and zoom scaling.

[^6]: MonoGame Documentation. https://docs.monogame.net/ — XNA/MonoGame rendering pipeline, `SpriteBatch`, `BlendState`, `SamplerState`.

---

## Appendix: How UI Info Suite 2 Actually Handles Rendering

For reference, here's how the most popular Stardew Valley UI mod handles the things the design system gets wrong:

1. **Event:** Uses `RenderingHud` (not `RenderedWorld`) for overlay drawing
2. **Coordinates:** Uses `Game1.GlobalToLocal()` + `Utility.ModifyCoordinatesForUIScale()` — never manual viewport math
3. **Zoom:** Uses `Game1.pixelZoom` and `Utility.ModifyCoordinateForUIScale(Game1.tileSize)` — never hardcoded 64
4. **Tooltips:** Uses `IClickableMenu.drawHoverText()` — the game's built-in tooltip renderer
5. **Textures:** Uses `Game1.mouseCursors` — existing game textures, not custom white textures
6. **Thread safety:** Uses `System.Threading.Mutex` for cross-thread access (not `lock` for game/render thread)
7. **Transparency:** Uses `Color.White * 0.7f` — XNA color multiplication for alpha

The design system should align with these proven patterns.

---

## Supplementary Research Reports

This evaluation was informed by four parallel research agents that produced detailed reports:

| Report | Location | Scope |
|--------|----------|-------|
| Technical Realities | `research-report.md` | Game loop, threading, tile size, fonts, SpriteBatch, SMAPI events |
| Accessibility Standards | `research-wcag-gaming.md` | WCAG vs Game Accessibility Guidelines, CVD, `prefers-reduced-motion`, existing mods |
| Color Rendering | `research/xna-monogame-color-rendering-report.md` | XNA Color, interpolation, alpha blending, textures, pixel-perfect rendering |
| Existing Mod Patterns | (incorporated above) | UI Info Suite 2 source code analysis |

These reports contain additional detail, code examples, and source citations that complement this evaluation.
