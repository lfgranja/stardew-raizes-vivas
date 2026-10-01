# Research: Stardew Valley Rendering Pipeline for SMAPI Tile Overlays

**Date:** 2026-09-09
**Feature:** FR-009 (Degradation HUD Indicator) & Soil Health Visualization
**Scope:** layerDepth, draw order, Display events, and tile overlay rendering

---

## 1. Display.RenderedWorld Event

### What it is

`Display.RenderedWorld` is a SMAPI event raised **after the game world is drawn to the sprite batch, before it's rendered to the screen** [^1].

### Draw order

The full draw order in a frame is:

```
1. Display.Rendering           ← BEFORE anything is drawn (sprite batch just opened)
2.  [Game draws world: Back layer → Buildings → Front layer → Characters → Always Front]
3. Display.RenderedWorld        ← AFTER world is drawn, before screen render
4. Display.RenderingActiveMenu  ← BEFORE menu (if open)
5. Display.RenderedActiveMenu   ← AFTER menu
6. Display.RenderingHud         ← BEFORE HUD
7. Display.RenderedHud          ← AFTER HUD
8. Display.Rendered             ← AFTER everything (including HUD), before screen render
```

### Critical implication for layerDepth

Content drawn in `Display.RenderedWorld` is drawn **over the world (including characters)** but **under any active menu, HUD elements, or cursor** [^1].

**This means:** If you draw a tile overlay in `RenderedWorld`, it will appear **above characters**. The player and NPCs will be hidden behind your overlay.

### Appropriate layerDepth values

When using `Display.RenderedWorld`, the game's `SpriteBatch` has already been opened (typically with `SpriteSortMode.Deferred`). To use layerDepth effectively, you must either:

- **Option A:** Begin a new `SpriteBatch` section with `SpriteSortMode.FrontToBack` or `BackToFront` and use layerDepth values in the `[0, 1]` range.
- **Option B:** Draw without layerDepth (using Deferred mode) — your draws will be on top of everything drawn so far in the batch.

For rendering **above tiles but below characters**, `RenderedWorld` is the **wrong event** — characters are already drawn. You need `RenderingStep` with a specific step (see §3).

---

## 2. layerDepth in SpriteBatch.Draw

### How it works

The `layerDepth` parameter in `SpriteBatch.Draw` sets the **Z value** for the quad vertices [^2]. Key facts:

| Property | Value |
|----------|-------|
| **Type** | `float` |
| **Effective range (default SpriteEffect)** | `[0.0, 1.0]` |
| **0.0** | Front (closest to camera) — drawn last (on top) in FrontToBack |
| **1.0** | Back (farthest from camera) — drawn first (behind) in FrontToBack |
| **When ignored** | `SpriteSortMode.Deferred` (draw order = call order) |
| **When active** | `SpriteSortMode.FrontToBack` or `SpriteSortMode.BackToFront` |

### The projection matrix constraint

The default `SpriteEffect` used by MonoGame sets up an orthographic projection with:
- Near plane: `0.0`
- Far plane: `-1.0`

This means Z values outside `[0, 1]` are **clipped** and the sprite won't render [^2]. Technically, `layerDepth` can be any float, but with the default shader, only `[0, 1]` is visible.

### SpriteSortMode behavior

| Sort Mode | layerDepth effect | Draw order |
|-----------|-------------------|------------|
| `Deferred` | **Ignored** | Call order (first drawn = behind) |
| `FrontToBack` | Active | Higher layerDepth drawn first (behind) |
| `BackToFront` | Active | Lower layerDepth drawn first (behind) |
| `Texture` | Active | Grouped by texture for batching |

### Typical layerDepth values in Stardew Valley

From decompiled code analysis and modding community patterns [^3]:

| Layer | Typical layerDepth |
|-------|-------------------|
| Back tile layer (ground) | ~0.667 - 0.8 |
| Front tile layer (flooring, paths) | ~0.333 - 0.5 |
| Characters / NPCs | ~0.0 - 0.333 (Y-sorted) |
| Always Front tile layer | ~0.0 - 0.1 |
| HUD elements | ~0.0 (drawn last) |

**For tile overlays that should appear above tiles but below characters:**
- Use a layerDepth of approximately **0.333 - 0.45** (between Front layer and characters).
- Characters use Y-sorting with depth values typically in `[0, 0.333]` based on their Y position.

---

## 3. Display.RenderedWorld vs Display.RenderingStep vs Display.Rendered

### Comparison table

| Event | Timing | Draws over | Draws under | Best for |
|-------|--------|------------|-------------|----------|
| `Display.Rendering` | Before anything | Nothing | Everything | ❌ Not useful (game draws over) |
| `Display.Rendered` | After everything | Everything (incl. HUD, cursor) | Nothing | ✅ Tooltips, cursor overlays |
| `Display.RenderingWorld` | Before world | Nothing | World+ | ❌ Not useful (game draws over) |
| `Display.RenderedWorld` | After world | World (incl. characters) | Menus, HUD, cursor | ✅ World overlays above characters |
| `Display.RenderingStep` | Before specific step | Steps before it | Steps after it | ✅ Granular control |
| `Display.RenderedStep` | After specific step | That step | Later steps | ✅ Granular control |
| `Display.RenderingHud` | Before HUD | World + HUD | Nothing (under HUD) | ❌ Rarely needed |
| `Display.RenderedHud` | After HUD | Everything | Nothing | ✅ Above-HUD indicators |

### Display.RenderingStep / RenderedStep

These specialized events fire for each `RenderSteps` enum value in the draw cycle [^1]:

```
RenderSteps.World      →  The game world (tiles, characters, objects)
RenderSteps.Hud        →  The HUD (toolbar, clock, etc.)
RenderSteps.Mouse      →  Cursor and held item
RenderSteps.Tooltip    →  Tooltips and hover text
RenderSteps.Overlay    →  Special overlays
```

**⚠️ Warning:** These are more vulnerable to changes in game updates. The wiki recommends using the other render events if possible [^1].

### Which event for tile overlays?

| Goal | Recommended Event |
|------|-------------------|
| Overlay above tiles AND characters (e.g., full tile highlight) | `Display.RenderedWorld` |
| Overlay above tiles but BELOW characters (e.g., soil health tint) | `Display.RenderedStep` with `RenderSteps.World` or custom layerDepth |
| Overlay above everything including HUD (e.g., degradation HUD indicator) | `Display.Rendered` |
| Overlay above HUD but below cursor | `Display.RenderedHud` |

---

## 4. Existing Mod Examples

### Data Layers (by Pathoschild)

**Source:** [DataLayerOverlay.cs](https://github.com/Pathoschild/StardewMods/blob/develop/DataLayers/Framework/DataLayerOverlay.cs) [^4]

**How it renders:**
- Extends `BaseOverlay` which hooks into `Display.RenderedWorld`
- Draws in the `DrawWorld(SpriteBatch spriteBatch)` override
- Uses `spriteBatch.Draw(CommonHelper.Pixel, rectangle, color)` — **no explicit layerDepth**
- This means it uses `SpriteSortMode.Deferred` (the game's current mode) and draws **on top of everything in the batch**

**Key insight:** Data Layers draws tile overlays that appear **above characters** because it renders in `RenderedWorld` with Deferred mode. The overlay colors use `color * 0.3f` for semi-transparency.

**Legend/UI:** Drawn in `DrawUi(SpriteBatch batch)` which appears above the world overlay.

### UI Info Suite 2

**Source:** [GitHub](https://github.com/Annosz/UIInfoSuite2) [^5]

Renders in `Display.RenderedWorld` for world-based indicators (like scarecrow coverage). Uses similar pattern — draws directly to the sprite batch without layerDepth, appearing above the world layer.

### Tile-highlighting mods (e.g., Better Planting, sprinkler coverage)

Most tile-highlighting mods use `Display.RenderedWorld` and draw with Deferred mode, accepting that the overlay appears above characters. This is generally acceptable for coverage indicators.

---

## 5. Characters and Y-Sorting

### How Y-sorting works in Stardew Valley

Stardew Valley uses a **painter's algorithm** with Y-sorting for characters and objects within the world layer [^3]. The depth value is calculated based on the sprite's "feet position":

```
depth = (position.Y + Dimension.Y) * cons_depth_y + position.X * cons_depth_x
```

Where:
- `cons_depth_y = 1.0 / MapSize.Y` (primary sort by Y)
- `cons_depth_x = 0.00001 / MapSize.X` (tiny X adjustment for edge cases)

This means:
- Objects lower on screen (higher Y) are drawn **in front** of objects higher on screen
- A character standing at the bottom of the screen has the highest priority

### Interaction with tile overlays

**If a character stands on a tile with an overlay:**

| Overlay event | Result |
|---------------|--------|
| `RenderedWorld` (Deferred) | Overlay draws **above** character ❌ |
| `RenderedStep(World)` with layerDepth ~0.33 | Overlay draws **below** character ✅ |
| `RenderingStep(World)` with layerDepth ~0.4 | Overlay draws **below** character ✅ |

**Recommendation for soil health visualization:**
- If you want the soil tint to be visible but not hide the player, use `Display.RenderedStep` with `RenderSteps.World` and a layerDepth that places it between the Front tile layer and characters.
- If a full opaque overlay is acceptable (e.g., for a "degraded" warning), `RenderedWorld` is simpler and works fine.

---

## 6. HUD Rendering (FR-009)

### For degradation HUD indicators

**Use `Display.Rendered`** (not `RenderedWorld`) if you want the indicator to appear above the HUD [^1].

| Requirement | Event |
|-------------|-------|
| Above HUD, above cursor | `Display.Rendered` |
| Above HUD, below cursor | `Display.RenderedHud` |
| Below HUD, above world | `Display.RenderedWorld` |

### Why Display.Rendered?

From the Stardew Valley wiki [^1]:

> **Display.Rendered:** Raised after the game draws to the sprite batch in a draw tick, just before the final sprite batch is rendered to the screen. Content drawn to the sprite batch at this point will be drawn over all vanilla content (including menus, HUD, and cursor).

This is the correct event for a degradation HUD indicator that should always be visible regardless of what's on screen.

### Implementation pattern

```csharp
// In your mod's event registration:
helper.Events.Display.Rendered += this.OnRendered;

private void OnRendered(object? sender, RenderedEventArgs e)
{
    // Draw your HUD indicator using e.SpriteBatch
    // This will appear above everything including the game HUD
    e.SpriteBatch.Draw(
        texture,
        position,
        sourceRect,
        color,
        rotation,
        origin,
        scale,
        effects,
        layerDepth: 0.0f  // layerDepth is ignored in Deferred mode
    );
}
```

---

## 7. Practical Recommendations for Living Roots

### For soil health tile overlays (FR-008/FR-009)

| Visualization type | Event | layerDepth | Notes |
|--------------------|-------|------------|-------|
| Semi-transparent soil tint | `RenderedWorld` | N/A (Deferred) | Simple, draws above characters |
| Soil tint below characters | `RenderedStep(World)` | ~0.33-0.45 | Complex, needs layerDepth |
| Degradation HUD indicator | `Rendered` | N/A (Deferred) | Above everything |
| Hover tooltip | `Rendered` or `RenderedHud` | N/A (Deferred) | Above HUD |

### Recommended approach

1. **Start with `Display.RenderedWorld`** for tile overlays — it's the simplest and most compatible event.
2. Use `SpriteBatch.Draw` without layerDepth (Deferred mode) — your overlay will appear above the world.
3. Apply alpha transparency (e.g., `color * 0.3f`) so characters/objects are still visible through the overlay.
4. For the degradation HUD indicator (FR-009), use `Display.Rendered` to ensure it's always visible above the game HUD.

### Code structure

```csharp
// Tile overlay (above world, above characters)
helper.Events.Display.RenderedWorld += this.OnRenderedWorld;

// HUD indicator (above everything)
helper.Events.Display.Rendered += this.OnRendered;

private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
{
    if (!Context.IsWorldReady)
        return;

    SpriteBatch spriteBatch = e.SpriteBatch;
    
    foreach (var tile in tilesToOverlay)
    {
        Vector2 screenPos = tile.WorldPosition - new Vector2(Game1.viewport.X, Game1.viewport.Y);
        spriteBatch.Draw(
            this.overlayTexture,
            screenPos,
            null,
            tile.Color * 0.3f,  // 30% opacity
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            0f  // layerDepth ignored in Deferred mode
        );
    }
}

private void OnRendered(object? sender, RenderedEventArgs e)
{
    if (!Context.IsWorldReady)
        return;

    // Draw degradation HUD indicator above everything
    e.SpriteBatch.Draw(
        this.hudTexture,
        this.hudPosition,
        null,
        Color.White,
        0f,
        Vector2.Zero,
        1f,
        SpriteEffects.None,
        0f
    );
}
```

---

## Sources

[^1]: [Modding:Modder Guide/APIs/Events - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events) — Official documentation for Display events (Rendering, Rendered, RenderingWorld, RenderedWorld, RenderingStep, RenderedStep, RenderingHud, RenderedHud)

[^2]: [MonoGame Issue #8074](https://github.com/MonoGame/MonoGame/issues/8074) — layerDepth must be in [0, 1] for default SpriteEffect; Z clipping behavior; projection matrix near/far planes

[^3]: [MonoGame University - Working with Textures](https://docs.monogame.net/articles/tutorials/building_2d_games/06_working_with_textures/index.html) — Official MonoGame documentation on SpriteBatch.Draw parameters including layerDepth, SpriteSortMode

[^4]: [DataLayerOverlay.cs - Pathoschild/StardewMods](https://github.com/Pathoschild/StardewMods/blob/develop/DataLayers/Framework/DataLayerOverlay.cs) — Reference implementation of tile overlay rendering using Display.RenderedWorld

[^5]: [UIInfoSuite2 - GitHub](https://github.com/Annosz/UIInfoSuite2) — Another reference mod for world overlay rendering

[^6]: [MonoGame Community - Order when draw the elements in the world](https://community.monogame.net/t/order-when-draw-the-elements-in-the-world/10268) — Y-sorting algorithm discussion for Stardew Valley-like games

[^7]: [SMAPI Issue #151 - Graphics events and render targets](https://github.com/Pathoschild/SMAPI/issues/151) — Historical context on SMAPI graphics event timing and zoom-level issues

[^8]: [GameDev StackExchange - XNA Spritebatch sorting](https://gamedev.stackexchange.com/questions/30998/xna-spritebatch-sorting-by-texture-vs-depth) — SpriteBatch sorting modes and performance considerations
