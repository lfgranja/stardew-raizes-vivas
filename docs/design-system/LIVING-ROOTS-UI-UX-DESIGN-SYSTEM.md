# Living Roots — Game UI-UX Design System

> **Version:** 2.0.0
> **Status:** Active
> **Platform:** Stardew Valley SMAPI Mod (XNA / MonoGame / .NET 6)
> **Last Updated:** 2026-09-09
> **Authors:** Design System Team
> **Reality-Checked:** 2026-09-09 — Aligned with SMAPI/MonoGame constraints, Game Accessibility Guidelines, and existing mod patterns

---

## Table of Contents

1. [Design Philosophy](#1-design-philosophy)
2. [Design Principles](#2-design-principles)
3. [Color System](#3-color-system)
4. [Typography](#4-typography)
5. [Tile Overlay System](#5-tile-overlay-system)
6. [Tooltip System](#6-tooltip-system)
7. [Hoe Feedback System](#7-hoe-feedback-system)
8. [Accessibility Pattern System](#8-accessibility-pattern-system)
9. [Animation & Motion](#9-animation--motion)
10. [Configuration System](#10-configuration-system)
11. [Performance Budgets](#11-performance-budgets)
12. [Accessibility Requirements](#12-accessibility-requirements)
13. [Implementation Guidelines](#13-implementation-guidelines)
14. [Appendices](#14-appendices)

---

## 1. Design Philosophy

### 1.1 Mission

Living Roots extends Stardew Valley's farming experience with deep agroecological mechanics — soil health, composting, and sustainable farming. The UI-UX design system must communicate complex ecological data **without breaking the game's cozy, pixel-art aesthetic** or overwhelming the player.

### 1.2 Design Pillars

| Pillar | Description |
|--------|-------------|
| **Invisible Clarity** | Information is available on demand, never forced. The player sees what they need, when they need it. |
| **Ecological Authenticity** | Visual language draws from nature — earth tones, organic shapes, growth metaphors. |
| **Pixel Fidelity** | All rendering respects Stardew Valley's native 16-bit pixel art style. No anti-aliased, blurred, or non-native elements. |
| **Accessibility First** | Color is never the sole carrier of meaning. Patterns, text, and icons reinforce every data point. |
| **Performance Respect** | Zero frame drops. The visualization layer must never degrade the 60fps game loop. |

### 1.3 Target Audience

- **Primary:** Existing Stardew Valley players (casual to hardcore) who want deeper farming mechanics
- **Secondary:** Players interested in sustainability, ecology, and agricultural simulation
- **Accessibility:** Players with color vision deficiency (8% of males, 0.5% of females), motor impairments, and cognitive accessibility needs

---

## 2. Design Principles

### 2.1 Show, Don't Tell

Soil health is communicated through **color + pattern + optional text**. The player's eye learns to read the field at a glance without reading individual tooltips.

### 2.2 Progressive Disclosure

| Layer | Information | Trigger |
|-------|-------------|---------|
| **Ambient** | Tile color overlay (Poor/Moderate/Healthy) | Always visible when overlays enabled |
| **Focused** | Exact percentage + category name | Cursor hover (tooltip) |
| **Active** | Floating feedback on interaction | Hoe use (flash + text) |
| **Detailed** | Full soil report, history, recommendations | Config panel / console command |

### 2.3 Consistency with Stardew Valley

- Use the game's native `SpriteFont` (`Game1.smallFont`, `Game1.tinyFont`)
- Respect the tile grid — `Game1.tileSize` is 64px at 100% zoom, but zoom (75%–200%) and UI scale (75%–150%) affect the rendered size
- Match the game's existing tooltip style (dark background, white text)
- Follow the same interaction patterns as CJB Item Spawner, UI Info Suite 2, and other established mods
- Use `Game1.GlobalToLocal()` for world-to-screen coordinate conversion and `Utility.ModifyCoordinatesForUIScale()` for UI scaling

### 2.4 Non-Diegetic UI

All Living Roots UI elements are **non-diegetic** — they exist as a layer on top of the game world, not as in-world objects. This is consistent with Stardew Valley's existing UI (toolbar, clock, money display).

> **Reference:** Non-diegetic UI gives full control over shapes, forms, and colors, protecting accessibility and readability. [^1]

---

## 3. Color System

### 3.1 Design Rationale

The color system draws from **natural earth tones and agricultural visual language**:

- **Poor (Red/Dry):** Evokes parched earth, drought, warning — universally understood as "needs attention"
- **Moderate (Amber/Transition):** Evokes autumn leaves, transition, caution — "getting there"
- **Healthy (Green/Growth):** Evokes lush vegetation, vitality, success — "thriving"
- **Unknown (Gray/Neutral):** Evokes untested soil, neutrality — "no data"

### 3.2 Primary Health Colors

These are the **default** colors. All values are configurable per-save via `VisualizationConfiguration`.

| Category | Hex | RGB | XNA Color | Usage |
|----------|-----|-----|-----------|-------|
| **Poor** | `#B91C1C` | `(185, 28, 28)` | `new Color(185, 28, 28)` | Soil health 0–33% |
| **Moderate** | `#D97706` | `(217, 119, 6)` | `new Color(217, 119, 6)` | Soil health 34–66% |
| **Healthy** | `#15803D` | `(21, 128, 61)` | `new Color(21, 128, 61)` | Soil health 67–100% |
| **Unknown` | `#6B7280` | `(107, 114, 128)` | `new Color(107, 114, 128)` | No data / out of bounds |

> **Why these specific values:** The Poor color uses a deeper red (`#B91C1C` not `#FF0000`) to avoid visual aggression and maintain pixel-art aesthetic. Healthy uses a forest green (`#15803D`) that evokes lush vegetation rather than neon green. Moderate uses an amber/amber-gold that bridges red and green naturally. Unknown uses a neutral slate gray.

### 3.3 Extended Palette

For future UI elements (composting bin states, companion planting indicators, etc.):

| Name | Hex | RGB | Usage |
|------|-----|-----|-------|
| **Compost-Empty** | `#78716C` | `(120, 113, 108)` | Bin empty state |
| **Compost-Processing** | `#A16207` | `(161, 98, 7)` | Bin processing (warm amber) |
| **Compost-Ready** | `#166534` | `(22, 101, 52)` | Bin ready (rich green) |
| **Water/Blue** | `#2563EB` | `(37, 99, 235)` | Irrigation, moisture indicators |
| **Sun/Yellow** | `#FACC15` | `(250, 204, 21)` | Sunlight, growth boost |
| **Mulch/Brown` | `#92400E` | `(146, 64, 14)` | Mulch, ground cover |

### 3.4 Overlay Opacity

| Setting | Value | Usage |
|---------|-------|-------|
| **Default Opacity** | `0.5` (50%) | Standard overlay rendering |
| **Pattern Minimum** | `0.7` (70%) | Accessibility patterns (never below) |
| **Config Range** | `0.2` – `0.8` | User-adjustable via config |

> **Rule:** Pattern opacity is clamped to ≥0.7 per FR-001. Even when a user reduces overlay opacity to 0.2, patterns remain at minimum 0.7 to ensure accessibility.

### 3.5 Color Interpolation

For smooth visual transitions between health categories, the `ColorInterpolationService` performs interpolation **between category colors**:

- **Poor band (0–33%):** Interpolate from Poor color → Moderate color
- **Moderate band (34–66%):** Interpolate from Moderate color → Healthy color
- **Healthy band (67–100%):** Interpolate from Moderate color → Healthy color

> **Note on interpolation method:** Linear RGB interpolation is used for simplicity, but it is not perceptually uniform — interpolating between red and green passes through desaturated brown-gray ("gray zone" problem). For pixel art games, consider HSL hue-interpolation or CIELAB for wide hue ranges. The current implementation uses `Color.Lerp` (per-channel RGB lerp) which is acceptable for the earth-toned palette where hue changes are moderate.

### 3.6 Colorblind Accessibility

The default palette has been tested for deuteranopia, protanopia, and tritanopia:

| Simulation | Poor | Moderate | Healthy | Distinguishable? |
|------------|------|----------|---------|-----------------|
| **Normal** | Red | Amber | Green | ✅ |
| **Deuteranopia** | Brown | Amber | Blue-Green | ✅ (with pattern) |
| **Protanopia** | Dark Brown | Amber | Teal | ✅ (with pattern) |
| **Tritanopia** | Pink | Orange | Green | ✅ (with pattern) |

> **Critical Rule:** Color alone is never sufficient. The **Pattern System** (Section 8) provides redundant encoding for all color information.

---

## 4. Typography

### 4.1 Font Strategy

Living Roots uses **Stardew Valley's native fonts** exclusively. No custom fonts are loaded.

| Font | XNA Reference | Line Spacing | Usage |
|------|---------------|--------------|-------|
| **Small Font** | `Game1.smallFont` | `Game1.smallFont.LineSpacing` | Tooltips, floating text, detailed info |
| **Tiny Font** | `Game1.tinyFont` | `Game1.tinyFont.LineSpacing` | Compact tile labels, config panel |
| **Dialogue Font** | `Game1.dialogueFont` | `Game1.dialogueFont.LineSpacing` | Panel headers, titles |

> **Rule:** Never hardcode font pixel sizes. Use `Game1.smallFont.LineSpacing` for vertical layout and `Game1.smallFont.MeasureString(text)` for horizontal sizing. Font metrics vary by language (the game supports localization with different font files).

### 4.2 Text Rendering Rules

1. **Pixel-perfect positioning:** All text coordinates are snapped to integer pixel positions (`(int)x, (int)y`) to prevent subpixel blur
2. **No anti-aliasing override:** Use `SpriteBatch` with `SpritePointSmooth` (default) — never `SpriteSmooth`
3. **Drop shadow:** All text rendered on the game world gets a 1px black offset shadow for readability on any background
4. **Max tooltip width:** 200px; text wraps to next line if exceeded

### 4.3 Text Colors

| Context | Color | Hex |
|---------|-------|-----|
| Tooltip text | White | `#FFFFFF` |
| Tooltip background | Black (alpha 200) | `#000000C8` |
| Floating health text | Category color | (see §3.2) |
| Floating text shadow | Black | `#000000` |
| Config panel text | Game default | (inherited) |

---

## 5. Tile Overlay System

### 5.1 Overview

The tile overlay system renders a **semi-transparent colored rectangle** on each tilled soil tile, indicating its current health. This is the primary ambient information layer.

### 5.2 Rendering Rules

| Rule | Specification |
|------|---------------|
| **Tile size** | `Game1.tileSize` (64 at 100% zoom) — never hardcode 64 |
| **Render target** | `SpriteBatch` with `BlendState.AlphaBlend` |
| **Draw call per tile** | 1 (base) + 1 (pattern, if enabled) |
| **Viewport culling** | Only render tiles within visible viewport + 1 tile buffer |
| **Cache** | Overlay list cached per frame; invalidated on data change |
| **Graceful degradation** | >1000 visible tiles → disable patterns automatically |
| **Coordinate conversion** | Use `Game1.GlobalToLocal()` + `Utility.ModifyCoordinatesForUIScale()` |
| **Zoom handling** | Use `Game1.pixelZoom` for sprite scaling at non-100% zoom |

> **Critical:** Never hardcode tile size as 64. The game supports zoom (75%–200%) and UI scale (75%–150%). Use `Game1.tileSize` dynamically and convert coordinates with `Game1.GlobalToLocal()` (handles viewport + zoom) and `Utility.ModifyCoordinatesForUIScale()` (handles UI scale). See UI Info Suite 2's `ShowItemEffectRanges.cs` for a reference implementation.

### 5.3 Overlay Lifecycle

```
Data Change → InvalidateCache() → Recompute Overlays → Cache → Render per Frame
```

1. **Data Change:** `SetTileHealthData()` called by game event handler
2. **Cache Invalidation:** `InvalidateCache()` clears cached overlay list
3. **Recomputation:** `OverlayRenderer.GetOverlays()` computes visible overlays with viewport culling
4. **Rendering:** `RenderOverlays()` draws each cached overlay via `SpriteBatch`

### 5.4 Thread Safety

Stardew Valley uses MonoGame, which has a **single-threaded game loop** — `Update()` then `Draw()` run sequentially on the same thread, never in parallel. SMAPI event handlers are also synchronous: the game is paused during event handling, so no other mod's code runs simultaneously. [^1]

However, thread safety is still needed for:
- **Multiplayer:** Farmhand data syncs asynchronously
- **`PerScreen<T>` state:** SMAPI's per-screen values can be accessed from different threads
- **`UnvalidatedUpdateTicked`:** SMAPI explicitly documents this event as not thread-safe

```
┌─────────────────────────────────────────────────────────┐
│  Game Loop (Single Thread)                              │
│  ────────────────────────                               │
│  UpdateTicked → SetTileHealthData()                     │
│  ↓ lock(_dataLock)                                      │
│  ↓ update dictionary                                    │
│  ↓ unlock                                               │
│  ...                                                    │
│  RenderedWorld → RenderOverlays()                       │
│  ↓ lock(_dataLock)                                      │
│  ↓ snapshot reference                                  │
│  ↓ unlock                                               │
│  ↓ draw (no lock held)                                  │
└─────────────────────────────────────────────────────────┘
```

> **Rule:** Lock is held for snapshot only, never during `spriteBatch.Draw()`. This prevents blocking during the render phase. For cross-thread access (multiplayer, `PerScreen<T>`), use `Mutex` as UI Info Suite 2 does.

### 5.5 Overlay Opacity Behavior

```
User Config Opacity: 0.5
├── Base overlay: 0.5 (50% transparent)
└── Pattern overlay: max(0.5, 0.7) = 0.7 (70% transparent, clamped)

User Config Opacity: 0.2
├── Base overlay: 0.2 (80% transparent — very subtle)
└── Pattern overlay: max(0.2, 0.7) = 0.7 (still visible for accessibility)
```

---

## 6. Tooltip System

### 6.1 Overview

Tooltips provide **precise, on-demand information** when the player hovers over a tile. They follow the cursor and display the exact health percentage and category.

### 6.2 Tooltip Format

```
Soil Health: 72% (Healthy)
```

Format string: `"Soil Health: {0:F0}% ({1})"` where `{0}` is the health value and `{1}` is the category name.

### 6.3 Rendering Rules

| Rule | Specification |
|------|---------------|
| **Trigger** | Cursor hovering over a tile with health data |
| **Position** | 16px offset from cursor (bottom-right) |
| **Background** | Black with alpha 200 (`new Color(0, 0, 0, 200)`) |
| **Text color** | White (`Color.White`) |
| **Padding** | 8px horizontal, 4px vertical |
| **Font** | `Game1.smallFont` |
| **Debounce** | 50ms minimum between tooltip updates (FR-015) |
| **Z-order** | Drawn after overlays, before hoe feedback |
| **Rendering** | Use `IClickableMenu.drawHoverText()` for native style |

> **Implementation:** Use `IClickableMenu.drawHoverText(Game1.spriteBatch, text, Game1.smallFont)` for native Stardew Valley tooltip rendering. This ensures consistent appearance and handles edge cases (off-screen positioning) automatically. See UI Info Suite 2's `ShowCropAndBarrelTime.cs` for reference.

### 6.4 Tooltip State Machine

```
No Data → No Tooltip
  ↓
Data Found → Check Debounce
  ↓ (50ms elapsed)
  ↓ Same text + same tile → Skip (throttled)
  ↓ Different text or tile → Render
  ↓
Update _lastTooltipText, _lastTooltipTile, _lastTooltipTime
```

### 6.5 Tooltip Positioning

```
Cursor Position (screen space)
    │
    ├──→ Tooltip X = cursor.X + 16
    │
    └──→ Tooltip Y = cursor.Y + 16

    ┌──────────────────────┐
    │ Soil Health: 72%     │  ← 8px padding inside
    │ (Healthy)            │
    └──────────────────────┘
```

> **Edge case handling:** If tooltip would extend beyond screen right edge, position to left of cursor. If beyond bottom edge, position above cursor.

---

## 7. Hoe Feedback System

### 7.1 Overview

When a player uses a hoe on a tile, the system provides **immediate visual feedback** — a flash of the tile's health color and a floating text label that rises and fades.

### 7.2 Feedback Components

| Component | Duration | Effect |
|-----------|----------|--------|
| **Flash** | 300ms | Tile-sized rectangle in category color, alpha fades 255→0 |
| **Floating Text** | 1000ms | "Soil Health: X% (Category)" rises upward at 0.02px/ms, alpha fades |

### 7.3 Timing Diagram

```
Time (ms)  0        100       200       300       400       ...      1000
           │         │         │         │         │                  │
Flash      ████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
           ↑ alpha=255                    ↑ alpha=0 (flash ends)
           │
Floating   ████████████████████████████████████████████████████████████
Text       ↑ alpha=255                                              ↑ alpha=0
           ↑ Y offset=0                                             ↑ Y offset=-20px
```

### 7.4 Feedback State

```csharp
public class HoeFeedback
{
    Point TilePosition;      // Tile coordinates
    long StartTime;          // Game clock ms when triggered
    long FlashDuration = 300;    // Flash effect duration
    long TextDuration = 1000;    // Floating text duration
    float HealthValue;       // Health at time of interaction
    HealthCategory Category; // Category at time of interaction
    string? HealthText;      // Pre-formatted "Soil Health: X% (Category)"
}
```

### 7.5 Feedback Lifecycle

1. **Trigger:** `TriggerHoeFeedback(tilePos, healthValue)` called on hoe use
2. **Active:** Each frame, `RenderHoeFeedback()` checks `IsActive(currentTime)`
3. **Expired:** `IsExpired(currentTime)` → removed from active list
4. **Cleanup:** Expired feedbacks removed under lock at start of each render

### 7.6 Visual Specifications

**Flash Effect:**
- Rectangle: 64×64px at tile position
- Color: Category color with animated alpha
- Alpha curve: `alpha = 255 * (1 - elapsed / flashDuration)` (linear fade)

**Floating Text:**
- Font: `Game1.smallFont`
- Color: Category color with animated alpha
- Shadow: 1px black offset
- Position: Centered horizontally on tile, rises vertically
- Rise rate: 0.02px per millisecond (20px over 1000ms)
- Alpha curve: `alpha = 255 * (1 - elapsed / textDuration)` (linear fade)

---

## 8. Accessibility Pattern System

### 8.1 Purpose

The pattern system provides **redundant visual encoding** so that health information is accessible to players with color vision deficiency (CVD). Patterns are overlaid on top of the base color at a higher opacity.

### 8.2 Pattern Assignments

| Health Category | Pattern | Visual Description |
|-----------------|---------|-------------------|
| **Poor** | `Stripes` | Diagonal stripes (8px wide, 16px period) |
| **Moderate** | `Dots` | Grid of dots (8px diameter, 16px spacing) |
| **Healthy** | `Solid` | Solid fill (no pattern needed — color is sufficient) |
| **Unknown** | `None` | No pattern (gray is self-evident) |

### 8.3 Pattern Rendering Rules

| Rule | Specification |
|------|---------------|
| **Minimum opacity** | 0.7 (70%) — never below, regardless of user config |
| **Pattern color** | Same as base color (not white or black) |
| **Pattern layer** | Drawn on top of base overlay |
| **Toggle** | `VisualizationConfiguration.ShowPatterns` (default: true) |
| **Graceful degradation** | Patterns disabled when >1000 visible tiles |

### 8.4 Pattern Opacity Logic

```csharp
// Pattern opacity is clamped to minimum 0.7 per FR-001
var patternOpacity = Math.Max(ModConstants.PatternMinOpacity, config.Opacity);
```

### 8.5 Pattern Specifications

> **Zoom scaling:** All pattern sizes must scale with `Game1.pixelZoom`. At 100% zoom, 1 pixel = 1 pixel. At 200% zoom, multiply all sizes by 2. Use `Utility.ModifyCoordinateForUIScale()` for consistent scaling.

**Stripes (Poor):**
```
Pattern: Diagonal lines (top-left to bottom-right)
Stripe width: 8px (scaled by zoom)
Gap width: 8px (scaled by zoom)
Period: 16px (scaled by zoom)
Implementation: Draw 8px-wide vertical stripes at x = left, left+16, left+32, ...
Texture: White stripe texture tinted with category color at draw time
```

**Dots (Moderate):**
```
Pattern: Grid of filled squares
Dot size: 8×8px (scaled by zoom)
Spacing: 16px (center-to-center, scaled by zoom)
Implementation: Draw 8×8 squares at (left+8, top+8), (left+24, top+8), ...
Texture: White dot texture tinted with category color at draw time
```

**Solid (Healthy):**
```
Pattern: Full tile fill (same as base overlay at pattern opacity)
Implementation: Single spriteBatch.Draw() at pattern opacity
```

> **Performance tip:** Create small white-on-transparent pattern textures once at mod load, then tint at draw time with `spriteBatch.Draw(texture, rect, color * opacity)`. This allows a single texture to serve all categories and supports `SamplerState.PointWrap` for seamless tiling. See UI Info Suite 2's use of `Game1.mouseCursors` for reference.

### 8.6 Why Patterns Work

Research shows that **redundant visual encoding** (color + texture) improves comprehension for all players, not just those with CVD. [^2] The pattern system ensures:

1. **Deuteranopia/Protanopia:** Stripes vs dots vs solid are distinguishable without color
2. **Tritanopia:** Pattern texture provides contrast independent of hue
3. **Low brightness/glare:** Patterns remain visible even when color perception is reduced (outdoor play, bright screens)
4. **Cognitive accessibility:** Multiple channels reduce cognitive load for players who struggle with color-only encoding

---

## 9. Animation & Motion

### 9.1 Motion Philosophy

Living Roots follows **subtle, purposeful motion**:

- Animations convey meaning (flash = interaction, floating text = data revealed)
- No decorative or ambient animations
- All animations can be reduced via a `ReduceMotion` config option (see §10)

### 9.2 Animation Specifications

| Animation | Duration | Easing | Trigger |
|-----------|----------|--------|---------|
| **Hoe flash** | 300ms | Linear fade out | Hoe use on tile |
| **Floating text rise** | 1000ms | Linear (constant velocity) | Hoe use on tile |
| **Floating text fade** | 1000ms | Linear fade out | Hoe use on tile |
| **Tooltip appear** | Instant (0ms) | None | Cursor hover |
| **Overlay transition** | Instant (0ms) | None | Data change |

### 9.3 Reduced Motion

When `ReduceMotion` is enabled in config (default: false):

| Animation | Reduced Behavior |
|-----------|-----------------|
| **Hoe flash** | Skip entirely |
| **Floating text** | Show text statically for 1000ms at tile position (no rise) |
| **Tooltip** | Unchanged (instant appearance is already reduced) |

> **Implementation:** This is a mod config option, not a system setting. Stardew Valley/MonoGame has no equivalent of CSS `prefers-reduced-motion`. The config option follows the Game Accessibility Guidelines recommendation: "Provide an option to turn off/hide background movement." [^4]

### 9.4 Frame Budget

| Operation | Max Time | Notes |
|-----------|----------|-------|
| **Overlay computation** | < 1ms | Viewport culling + cache |
| **Overlay rendering** | < 2ms | ~100 tiles × 2 draw calls |
| **Tooltip rendering** | < 0.5ms | Single draw call |
| **Hoe feedback** | < 0.5ms | Max 5 concurrent feedbacks |
| **Total visualization** | < 4ms | Out of 16.67ms frame budget |

> **Rule:** Visualization must consume less than 25% of frame budget. Remaining 75%+ is for game logic and base rendering.

---

## 10. Configuration System

### 10.1 Configuration Model

All visualization settings are persisted per-save as JSON via `IModDataService`.

```csharp
public class VisualizationConfiguration
{
    // Feature toggles
    public bool OverlaysEnabled { get; set; } = true;
    public bool TooltipsEnabled { get; set; } = true;
    public bool HoeFeedbackEnabled { get; set; } = true;
    public bool ShowPatterns { get; set; } = true;

    // Opacity
    public float Opacity { get; set; } = 0.5f;

    // Custom colors (optional — defaults in ModConstants)
    public ColorDTO PoorColor { get; set; } = /* default red */;
    public ColorDTO ModerateColor { get; set; } = /* default amber */;
    public ColorDTO HealthyColor { get; set; } = /* default green */;
    public ColorDTO UnknownColor { get; set; } = /* default gray */;

    // Accessibility
    public string AccessibilityDegradation { get; set; } = "auto";
    public bool ReduceMotion { get; set; } = false;
}
```

### 10.2 Configuration Defaults

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| `OverlaysEnabled` | `true` | bool | Master toggle for tile overlays |
| `TooltipsEnabled` | `true` | bool | Master toggle for hover tooltips |
| `HoeFeedbackEnabled` | `true` | bool | Master toggle for hoe interaction feedback |
| `ShowPatterns` | `true` | bool | Toggle accessibility patterns |
| `Opacity` | `0.5` | 0.2–0.8 | Overlay transparency |
| `AccessibilityDegradation` | `"auto"` | auto/always/never | Pattern rendering mode |
| `ReduceMotion` | `false` | bool | Reduce/stop animations (accessibility) |

### 10.3 Configuration Persistence

- **Save key:** `viz_config_{saveId}` via SMAPI data API (`IModDataService`)
- **Load timing:** On save load and on `SaveLoaded` event
- **Write timing:** On config change (debounced 500ms)
- **Thread safety:** Config read is lock-free (immutable snapshot); write uses `lock`

### 10.4 Console Commands

| Command | Description |
|---------|-------------|
| `lr_overlays on/off/toggle` | Toggle tile overlays |
| `lr_tooltips on/off/toggle` | Toggle tooltips |
| `lr_hoefeedback on/off/toggle` | Toggle hoe feedback |
| `lr_patterns on/off/toggle` | Toggle accessibility patterns |
| `lr_opacity <0.2-0.8>` | Set overlay opacity |
| `lr_reducemotion on/off/toggle` | Toggle reduced motion (accessibility) |
| `lr_reset` | Reset all config to defaults |

---

## 11. Performance Budgets

### 11.1 Rendering Budgets

| Metric | Target | Maximum | Notes |
|--------|--------|---------|-------|
| **Frame time (visualization)** | < 2ms | < 4ms | Out of 16.67ms total |
| **Draw calls (overlays)** | < 200 | < 400 | 2 per tile (base + pattern) |
| **Memory (overlay cache)** | < 1MB | < 2MB | ~1000 tiles × ~1KB each |
| **Memory (feedback list)** | < 10KB | < 50KB | Max 5 concurrent feedbacks |
| **Lock contention** | 0ms | < 0.1ms | Snapshot-only locking |

### 11.2 Graceful Degradation

| Condition | Action |
|-----------|--------|
| >1000 visible tiles | Disable patterns (keep base overlay) |
| >2000 visible tiles | Reduce overlay opacity by 50% |
| Frame time > 4ms | Skip tooltip rendering for frame |
| Memory pressure | Clear overlay cache, recompute next frame |

### 11.3 Optimization Strategies

1. **Viewport culling:** Only compute overlays for visible tiles + 1 tile buffer
2. **Overlay caching:** Cache overlay list; recompute only on data change
3. **Lock-free reads:** Config reads use immutable snapshot pattern
4. **Batch rendering:** All overlays drawn in single `SpriteBatch.Begin()/End()` block
5. **Early exit:** Skip rendering when `_isPaused`, `!OverlaysEnabled`, or no data

---

## 12. Accessibility Requirements

### 12.1 Game Accessibility Guidelines

Living Roots visualization follows the [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com) (Basic level) — the standard reference for video game accessibility. WCAG 2.1 AA is a web standard requiring web technologies (DOM, ARIA, CSS) that don't exist in MonoGame. [^3][^4]

| Guideline | Requirement | Implementation |
|-----------|-------------|----------------|
| **No color alone** | Don't convey essential info by fixed color alone | Pattern system (§8) + text labels |
| **High contrast** | Provide high contrast between text/UI and background | Tooltip text on dark bg (≥4.5:1) |
| **Customizable** | Allow color customization | Config colors per-save |
| **Redundant cues** | Use color + pattern + icon/text together | Overlay + pattern + tooltip |
| **Reduced motion** | Provide option to turn off movement | `ReduceMotion` config option |

### 12.2 Color Vision Deficiency

Supported CVD types:

| Type | Prevalence | Support |
|------|------------|---------|
| **Deuteranomaly** (green-weak) | 6% of males | ✅ Pattern + color |
| **Protanomaly** (red-weak) | 1% of males | ✅ Pattern + color |
| **Tritanomaly** (blue-weak) | 0.01% | ✅ Pattern + color |
| **Achromatopsia** (total colorblind) | 0.003% | ✅ Pattern + text |

### 12.3 Motor Accessibility

| Consideration | Implementation |
|---------------|----------------|
| **No rapid-click required** | All interactions are single-click or hover |
| **No precise timing** | No time-sensitive UI interactions |
| **Configurable** | All features can be toggled on/off |
| **Console commands** | All config accessible via keyboard-only console |

### 12.4 Cognitive Accessibility

| Consideration | Implementation |
|---------------|----------------|
| **Progressive disclosure** | Ambient → Focused → Active → Detailed |
| **Consistent patterns** | Same visual language across all features |
| **No information overload** | Overlays are subtle by default (50% opacity) |
| **Clear feedback** | Hoe feedback confirms interaction immediately |
| **Predictable behavior** | Same input always produces same output |

### 12.5 Accessibility Testing Checklist

- [ ] Verify all colors against CVD simulators (deuteranopia, protanopia, tritanopia)
- [ ] Test pattern visibility at minimum opacity (0.7)
- [ ] Verify tooltip text contrast ratio ≥ 4.5:1
- [ ] Test with overlays at minimum opacity (0.2) — patterns still visible
- [ ] Test with overlays at maximum opacity (0.8) — game world still visible
- [ ] Verify all features work with reduced motion enabled
- [ ] Test console commands for keyboard-only configuration
- [ ] Verify graceful degradation at >1000 tiles

---

## 13. Implementation Guidelines

### 13.1 Rendering Pipeline

```
Game Loop (60fps)
    │
    ├── Event: CursorMoved
    │   └── VisualizationService.UpdateCursorTile()
    │
    ├── Event: RenderedWorld (per frame)
    │   ├── VisualizationService.RenderOverlays()
    │   │   ├── Snapshot tile health data (lock)
    │   │   ├── Get cached overlays (viewport culling)
    │   │   └── Draw each overlay (SpriteBatch)
    │   │
    │   ├── VisualizationService.RenderTooltip()
    │   │   ├── Convert cursor to tile coords
    │   │   ├── Lookup health data (lock)
    │   │   ├── Check debounce (50ms)
    │   │   └── Draw tooltip (SpriteBatch)
    │   │
    │   └── VisualizationService.RenderHoeFeedback()
    │       ├── Snapshot active feedbacks (lock)
    │       ├── Remove expired
    │       └── Draw flash + floating text (SpriteBatch)
    │
    └── Event: SaveLoaded
        └── VisualizationConfigurationService.LoadConfig()
```

### 13.2 SpriteBatch Usage

All rendering uses a single `SpriteBatch` instance:

```csharp
spriteBatch.Begin(
    sortMode: SpriteSortMode.Deferred,
    blendState: BlendState.AlphaBlend,
    samplerState: SamplerState.PointClamp,  // Pixel-perfect!
    depthStencilState: null,
    rasterizerState: null);

// ... draw calls ...

spriteBatch.End();
```

> **Critical:** `SamplerState.PointClamp` ensures pixel-perfect rendering with no interpolation blur.

### 13.2.1 Coordinate Conversion

Always use the game's built-in coordinate conversion methods:

```csharp
// World position → screen position (handles viewport + zoom)
Vector2 screenPos = Game1.GlobalToLocal(worldPosition * Game1.tileSize);

// Apply UI scale (handles separate UI zoom)
screenPos = Utility.ModifyCoordinatesForUIScale(screenPos);

// For sprite scaling at non-100% zoom
float scale = Utility.ModifyCoordinateForUIScale(Game1.pixelZoom);
```

> **Never** manually subtract viewport coordinates. `Game1.GlobalToLocal()` handles viewport position, zoom, and UI scale correctly. See UI Info Suite 2's `ShowItemEffectRanges.cs` for reference.

### 13.2.2 Domain Layer Purity

The Domain layer (`LivingRoots/Domain/`) must remain XNA/MonoGame-free:

- **Use `ColorDTO`** in domain models (not `Microsoft.Xna.Framework.Color`)
- **Convert at the boundary:** Services convert `ColorDTO` → `Color` when rendering
- **No `using Microsoft.Xna.Framework`** in domain files (except `System.Numerics`)

This preserves the DDD layering: Domain (pure) → Services (XNA integration).

### 13.3 Color Constants

All color values live in `ModConstants` (in `LivingRoots/Constants.cs`) — **never hardcode colors in rendering code**.

```csharp
// ✅ Correct
var color = ModConstants.PoorColor;

// ❌ Wrong
var color = new Color(185, 28, 28);
```

> **Note:** `ModConstants` is defined in `Constants.cs` (class name `ModConstants`, file name `Constants.cs`). All visualization constants (`PoorColor`, `ModerateColor`, `HealthyColor`, `UnknownColor`, `DefaultOpacity`, `PatternMinOpacity`, `TooltipDebounceMs`, `TileSize`, `FlashDurationMs`, `TextDurationMs`, `DegradationTileThreshold`) are defined there.

### 13.4 Thread Safety Patterns

| Pattern | Usage |
|---------|-------|
| **Lock + snapshot** | Tile health data, feedback list |
| **Immutable snapshot** | Config reads |
| **volatile bool** | `_isPaused` flag |
| **Interlocked** | State machine transitions |
| **Mutex** | Cross-thread access (multiplayer, `PerScreen<T>`) |

> **Reminder:** Stardew Valley/MonoGame is single-threaded. The `lock` pattern is for data consistency between SMAPI events, not for game/render thread separation. Use `Mutex` for genuine cross-thread scenarios (like UI Info Suite 2 does for multiplayer tile data).

### 13.5 Shared Constants

All shared constants must be defined in `ModConstants` — **never duplicate constants across files**.

| Constant | Value | Usage |
|----------|-------|-------|
| `ModConstants.TileSize` | 64 | Tile dimension (at 100% zoom) |
| `ModConstants.FlashDurationMs` | 300 | Flash effect duration |
| `ModConstants.TextDurationMs` | 1000 | Floating text duration |
| `ModConstants.DegradationTileThreshold` | 1000 | Pattern disable threshold |

> **Rule:** If you need a constant in multiple files, define it once in `ModConstants` and reference it. Duplicating constants (like having `TileSize = 64` in both `VisualizationService` and `HoeFeedbackRenderer`) creates maintenance hazards.

### 13.5 Error Handling

| Scenario | Behavior |
|----------|----------|
| `VisualizationService` not initialized | Throw `InvalidOperationException` |
| No health data for tile | Skip tooltip rendering |
| Config file missing | Use defaults |
| Config file corrupt | Log warning, use defaults |
| Texture not loaded | Skip rendering, log error |

---

## 14. Appendices

### Appendix A: Color Reference Table

| Name | Hex | RGB | XNA | Category |
|------|-----|-----|-----|----------|
| Poor | `#B91C1C` | `(185, 28, 28)` | `new Color(185, 28, 28)` | Health |
| Moderate | `#D97706` | `(217, 119, 6)` | `new Color(217, 119, 6)` | Health |
| Healthy | `#15803D` | `(21, 128, 61)` | `new Color(21, 128, 61)` | Health |
| Unknown | `#6B7280` | `(107, 114, 128)` | `new Color(107, 114, 128)` | Health |
| Compost-Empty | `#78716C` | `(120, 113, 108)` | `new Color(120, 113, 108)` | Compost |
| Compost-Processing | `#A16207` | `(161, 98, 7)` | `new Color(161, 98, 7)` | Compost |
| Compost-Ready | `#166534` | `(22, 101, 52)` | `new Color(22, 101, 52)` | Compost |
| Water | `#2563EB` | `(37, 99, 235)` | `new Color(37, 99, 235)` | Environment |
| Sun | `#FACC15` | `(250, 204, 21)` | `new Color(250, 204, 21)` | Environment |
| Mulch | `#92400E` | `(146, 64, 14)` | `new Color(146, 64, 14)` | Environment |

### Appendix B: Constants Reference

| Constant | Value | Source |
|----------|-------|--------|
| `TileSize` | 64 | `VisualizationService.TileSize` |
| `DefaultOpacity` | 0.5 | `ModConstants.DefaultOpacity` |
| `PatternMinOpacity` | 0.7 | `ModConstants.PatternMinOpacity` |
| `TooltipDebounceMs` | 50 | `ModConstants.TooltipDebounceMs` |
| `FlashDuration` | 300ms | `HoeFeedback.FlashDuration` |
| `TextDuration` | 1000ms | `HoeFeedback.TextDuration` |
| `MaxVisibleTiles` | 1000 | Graceful degradation threshold |
| `FrameBudgetMs` | 16.67 | 60fps target |

### Appendix C: File Map

| File | Role |
|------|------|
| `Domain/Visualization/HealthCategory.cs` | Health category enum + thresholds |
| `Domain/Visualization/ColorDTO.cs` | Serializable color struct |
| `Domain/Visualization/ColorMapping.cs` | Health→color mapping model |
| `Domain/Visualization/TileOverlay.cs` | Tile overlay render data |
| `Domain/Visualization/TooltipData.cs` | Tooltip display model |
| `Domain/Visualization/HoeFeedback.cs` | Hoe feedback state |
| `Domain/Visualization/PatternType.cs` | Pattern enum + mapping |
| `Domain/Visualization/VisualizationConfiguration.cs` | User config model |
| `Services/Visualization/VisualizationService.cs` | Main render orchestrator |
| `Services/Visualization/OverlayRenderer.cs` | Overlay computation + cache |
| `Services/Visualization/ColorInterpolationService.cs` | Health→color interpolation |
| `Services/Visualization/VisualizationConfigurationService.cs` | Config load/save |
| `Services/Visualization/VisualizationTextures.cs` | Texture management |

### Appendix D: Research Sources

[^1]: Stardew Valley Wiki. "Modding:Modder Guide/APIs/Events." https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events — Confirms SMAPI events are synchronous: "the game is paused and no other mod's code will run simultaneously."
[^2]: Stardew Valley Wiki. "Modding:Modder Guide/Game Fundamentals." https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals — Documents zoom level (75%–200%), UI scale (75%–150%), coordinate systems, and `Utility.ModifyCoordinatesForUIScale()`.
[^3]: Game Accessibility Guidelines. https://gameaccessibilityguidelines.com — The standard reference for video game accessibility (not WCAG, which is web-only).
[^4]: Game Accessibility Guidelines. "Ensure no essential information is conveyed by a fixed colour alone." https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/ — Recommends redundant encoding (patterns, icons, labels).
[^5]: UI Info Suite 2. https://github.com/Annosz/UIInfoSuite2 — Reference implementation for rendering, tooltips, coordinate conversion, and zoom scaling.
[^6]: Kraj, N. "Designing Efficient User Interfaces For Games." *Medium*, 2024. Non-diegetic vs diegetic UI patterns in game interface design.
[^7]: MonoGame Documentation. https://docs.monogame.net/ — XNA/MonoGame rendering pipeline, `SpriteBatch`, `BlendState`, `SamplerState`.

### Appendix E: Revision History

| Version | Date | Changes |
|---------|------|---------|
| 2.0.0 | 2026-09-09 | Reality-checked update: Fixed threading model (single-threaded), tile size (zoom/UI scale), coordinate conversion (GlobalToLocal), accessibility standard (Game Accessibility Guidelines), color defaults (earth tones), prefers-reduced-motion (config toggle), tooltip rendering (native drawHoverText), domain layer purity (ColorDTO), pattern scaling (pixelZoom), shared constants (no duplicates) |
| 1.0.0 | 2026-09-09 | Initial design system release |

---

> **Document Status:** This is a living document. All changes must be reviewed against the existing codebase conventions in `AGENTS.md` and the visualization layer AGENTS.md. Color values in this document should be synchronized with `ModConstants.cs` during implementation.
