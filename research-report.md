# Stardew Valley Modding Technical Research Report

**Date:** 2026-09-07
**Purpose:** Verify technical claims in the Living Roots design system document against actual Stardew Valley / SMAPI / MonoGame behavior.

---

## 1. Game Loop & Threading

### Claim to verify
The design system claims separate "Game Thread" and "Render Thread" with locking between them.

### Finding: **INACCURATE — MonoGame uses a single-threaded game loop**

MonoGame (and XNA before it) uses a **single-threaded game loop** where `Update()` and `Draw()` run **sequentially on the same thread**, never in parallel.

The MonoGame documentation explicitly states:

> "MonoGame is executing the **Update** method and then the **Draw** method 60 times per second."
> — [MonoGame docs: Chapter 03 - The Game1 File](https://docs.monogame.net/articles/tutorials/building_2d_games/03_the_game1_file/index.html)

The MonoGame game loop is:
1. `Update(gameTime)` — runs game logic
2. `Draw(gameTime)` — renders the frame
3. Repeat

These never execute simultaneously. As confirmed on the MonoGame community forum:

> "Engine logic and game logic are completely separated. everything is controlled from the XNA Update and Draw deep down in the code."
> — [Game logic on separate thread? - MonoGame Community](https://community.monogame.net/t/game-logic-on-separate-thread/14171)

And from Reddit r/monogame:

> "Update is executed, then draw, for every single frame (60 frames per second). MonoGame isn't 'parallel'..."
> — [Is MonoGame parallel or serial? - Reddit](https://www.reddit.com/r/monogame/comments/wc88ax/is_monogame_parallel_or_serial/)

SMAPI events also run **synchronously** on this same thread:

> "Your event handlers are run **synchronously**: the game is paused and no other mod's code will run simultaneously, so there's no risk of conflicting changes."
> — [SMAPI Events - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events)

### Implication for the design system
The "Game Thread" / "Render Thread" separation with locking is a **false mental model** for Stardew Valley. There is only one thread for game logic and rendering. Mod code does not need to worry about cross-thread synchronization with a render thread. However, SMAPI's `UnvalidatedUpdateTicked` event is an exception — it is explicitly **not thread-safe** and can run asynchronously.

---

## 2. Tile Size

### Claim to verify
The design system says tiles are 64×64 pixels matching `Game1.tileSize`.

### Finding: **PARTIALLY ACCURATE — `Game1.tileSize` = 64 at default zoom, but source sprites are 16×16**

The Stardew Valley Wiki confirms `Game1.tileSize` is the key constant for coordinate conversion:

> "tile→absolute: `x * Game1.tileSize, y * Game1.tileSize`"
> "absolute→tile: `x / Game1.tileSize, y / Game1.tileSize`"
> — [Modding:Modder Guide/Game Fundamentals - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals)

At the default 100% zoom level, `Game1.tileSize` = **64 pixels**. This is because:
- Source sprites are **16×16 pixels** (most tiles)
- The game applies a **4× scale factor** (16 × 4 = 64)

As confirmed by the community:

> "Stardew sprites are (mostly) 16x16 yes, with things like an NPC overworld sprite being 16x32, or individual portrait frames being 64x64."
> — [Sprite size (First time modding SV) - Stardew Valley Forums](https://forums.stardewvalley.net/threads/sprite-size-first-time-modding-sv.7128/)

**Important caveat:** `Game1.tileSize` is affected by zoom level. The player can set zoom between 75% and 200%, which changes the effective pixel size. The wiki notes:

> "The player can set an in-game zoom level between 75% and 200%, which adjusted the size of all pixels shown on the screen."
> — [Modding:Modder Guide/Game Fundamentals - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals)

### Implication for the design system
The 64×64 tile size is correct **at 100% zoom**. However, the design system should account for zoom level if doing pixel-precise calculations. The source sprite resolution is 16×16, which matters when creating custom textures or doing pixel-level work.

---

## 3. SpriteFont Sizes

### Claim to verify
The design system claims `Game1.smallFont` is ~14px and `Game1.tinyFont` is ~10px.

### Finding: **CANNOT CONFIRM EXACT VALUES — Fonts are small pixel fonts; dialogue font glyphs are ~8px wide**

The exact pixel heights of `Game1.smallFont` and `Game1.tinyFont` are **not definitively documented** in the sources found. However, we have some data:

The dialogue font (which `Game1.dialogueFont` references) uses:

> "8px [mostly] uniform glyph width with a max 1px descender"
> — [Converting dialog font to a SpriteFont - Stardew Valley Forums](http://forums.stardewvalley.net/threads/converting-dialog-font-to-a-spritefont.20446/)

The fonts are named in game files as `SpriteFont1`, `SmallFont`, or `TinyFont` depending on height:

> "In the game files it's called SpriteFont1, Smallfont or Tinyfont depending on the height."
> — [What Font is used for SpriteFont, SmallFont and TinyFont? - Reddit r/SMAPI](https://www.reddit.com/r/SMAPI/comments/1il1ang/what_font_is_used_for_spritefont_smallfont_and/)

The fonts are custom pixel fonts (not standard TTF), distributed as `.xnb` files in the game's `Content/Fonts` directory. `Game1.smallFont` and `Game1.tinyFont` are smaller variants of the dialogue font.

### Implication for the design system
The ~14px and ~10px claims are **plausible but unverified**. The design system should not hardcode these values without measuring them at runtime via `Game1.smallFont.LineSpacing` and `Game1.smallFont.MeasureString()`. The `LineSpacing` property of `SpriteFont` gives the vertical advance in pixels, which is the correct way to measure font height.

---

## 4. SpriteBatch Rendering Settings

### Claim to verify
The design system specifies `SpriteSortMode.Deferred`, `BlendState.AlphaBlend`, `SamplerState.PointClamp`.

### Finding: **ACCURATE — These are the standard settings for Stardew Valley mod rendering**

The standard MonoGame pattern for 2D rendering is:

```csharp
spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
```

This is confirmed in the MonoGame community:

> "This is my code to start the draw routine. spriteBatch.Begin( SpriteSortMode.Deferred, BlendState.AlphaBlend );"
> — [C# graphics bleeding spritebatch - MonoGame Community](https://community.monogame.net/t/c-graphics-bleeding-spritebatch/518)

`SpriteSortMode.Deferred` is the default and most common — it batches all sprites and draws them all at once when `End()` is called, which is efficient for typical mod rendering.

`BlendState.AlphaBlend` is the standard for alpha transparency blending.

`SamplerState.PointClamp` is the correct choice for pixel-art games like Stardew Valley. It prevents texture bleeding (interpolation artifacts) when drawing sprites at non-integer positions or scaled sizes. The MonoGame community confirms:

> "I draw on two render targets and a spritebatch like this: `_graphicsDevice.SamplerStates[0] = SamplerState.PointClamp; _spritebatch.Begin(...)`"
> — [Sprite batch refusing to set sample state to clamp - MonoGame Community](https://community.monogame.net/t/sprite-batch-refusing-to-set-sample-state-to-clamp/19898)

**Note:** Some sources use `SamplerState.LinearClamp` for UI elements. The choice between `PointClamp` and `LinearClamp` depends on whether you want pixel-perfect (nearest-neighbor) or smoothed sampling.

### Implication for the design system
The specified settings are correct and represent best practices for Stardew Valley mod rendering. `PointClamp` is especially important for pixel-art sprites to avoid bleeding artifacts.

---

## 5. Game1 Class Properties

### Claim to verify
The design system references `Game1.smallFont`, `Game1.tinyFont`, `Game1.dialogueFont`, `Game1.tileSize`, etc.

### Finding: **ACCURATE — These are real and commonly used properties**

The Stardew Valley Wiki documents the `Game1` class as the game's core logic:

> "Game1 is the game's core logic. Most of the game state is tracked through this class."
> — [Modding:Modder Guide/Game Fundamentals - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals)

Documented commonly-used `Game1` fields:

| Field | Type | Purpose |
|-------|------|---------|
| `Game1.player` | `Farmer` | The current player |
| `Game1.currentLocation` | `GameLocation` | The game location containing the current player |
| `Game1.locations` | `IList<GameLocation>` | All locations in the game |
| `Game1.timeOfDay` | `int` | Current time (24-hour format, 10-min intervals) |
| `Game1.dayOfMonth` | `int` | Current day of month |
| `Game1.currentSeason` | `string` | Current season |
| `Game1.year` | `int` | Current year |
| `Game1.activeClickableMenu` | `IClickableMenu` | Modal menu being displayed |
| `Game1.tileSize` | `int` | Tile size in pixels (64 at default zoom) |
| `Game1.viewport` | `Rectangle` | The visible screen area in absolute coordinates |
| `Game1.options.zoomLevel` | `float` | Current zoom level (0.75–2.0) |
| `Game1.uiMode` | `bool` | Whether the game is in UI scaling mode |

The font properties (`Game1.smallFont`, `Game1.tinyFont`, `Game1.dialogueFont`, `Game1.bigFont`) are all `SpriteFont` instances and are widely used in mod code. They are loaded from the game's `Content/Fonts` directory.

### Implication for the design system
All referenced `Game1` properties are real and accessible. The design system is correct to use them.

---

## 6. Existing Mods for Reference

### Claim to verify
The design system references UI Info Suite 2, CJB Item Spawner, and similar mods for overlay/tooltip rendering patterns.

### Finding: **ACCURATE — These are real, popular mods that use SMAPI render events**

**UI Info Suite 2** ([GitHub](https://github.com/Annosz/UIInfoSuite2)) is the maintained successor to the original UI Info Suite. It uses SMAPI's `RenderedWorld` and `RenderedHud` events to draw overlays (e.g., crop readiness, birthday reminders, experience bars). The mod's release notes mention:

> "Fix tooltips being drawn behind active menu."
> — [Releases · Annosz/UIInfoSuite2](https://github.com/Annosz/UIInfoSuite2/releases)

This confirms the mod draws tooltips and overlays using the render event pipeline.

**CJB Item Spawner** ([GitHub](https://github.com/CJBok/SDV-Mods)) is a popular mod that opens a menu for spawning items. It demonstrates the `IClickableMenu` pattern for custom UI.

The standard pattern for overlay rendering in these mods is:
1. Subscribe to `Helper.Events.Display.RenderedWorld` or `RenderedHud`
2. In the handler, use `e.SpriteBatch` to draw text/sprites
3. Use `Game1.smallFont` / `Game1.tinyFont` for text via `DrawString`

As documented on the wiki:

> "RenderedWorld: Raised after the game world is drawn to the sprite batch, before it's rendered to the screen. Content drawn to the sprite batch at this point will be drawn over the world, but under any active menu, HUD elements, or cursor."
> — [SMAPI Events - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events)

### Implication for the design system
These are appropriate reference mods. The rendering pattern (subscribe to `RenderedWorld`/`RenderedHud`, draw to `e.SpriteBatch`) is the standard approach.

---

## 7. SMAPI Events for Rendering

### Claim to verify
The design system references `RenderedWorld`, `RenderedHud`, etc.

### Finding: **ACCURATE — These are real SMAPI Display events**

The full list of SMAPI Display render events, all providing a `SpriteBatch` parameter:

| Event | When | Draw Layer |
|-------|------|------------|
| `Display.Rendering` | Before game draws anything (draw tick) | Under everything (game draws over it) |
| `Display.Rendered` | After game draws to sprite batch | Over all vanilla content (menus, HUD, cursor) |
| `Display.RenderingWorld` | Before game world is drawn | Under world (world draws over it) |
| `Display.RenderedWorld` | After game world is drawn | Over world, under menus/HUD/cursor |
| `Display.RenderingActiveMenu` | Before active menu is drawn | Under menu |
| `Display.RenderedActiveMenu` | After active menu is drawn | Over menu and menu cursor |
| `Display.RenderingHud` | Before HUD is drawn | Under HUD |
| `Display.RenderedHud` | After HUD is drawn | Over HUD |
| `Display.RenderingStep` | Before a specific render step | Granular control |
| `Display.RenderedStep` | After a specific render step | Granular control |

Source: [SMAPI Events - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events)

**Key detail:** The game may open/close the sprite batch multiple times in a draw tick. The `Rendered` event's sprite batch may not contain everything being drawn.

> "Since the game may open/close the sprite batch multiple times in a draw tick, the sprite batch may not contain everything being drawn and some things may already be rendered to the screen."
> — [SMAPI Events - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events)

### Implication for the design system
The events are real and correctly named. For world overlays (like soil health indicators), `RenderedWorld` is the correct event. For HUD elements, `RenderedHud` is correct.

---

## 8. Color Type

### Claim to verify
The design system uses `Microsoft.Xna.Framework.Color`.

### Finding: **ACCURATE — Stardew Valley uses `Microsoft.Xna.Framework.Color`**

The MonoGame `Color` struct ([docs](https://docs.monogame.net/api/Microsoft.Xna.Framework.Color.html)) is in the `Microsoft.Xna.Framework` namespace. Constructor signatures:

```csharp
// Integer constructors (0–255 per channel)
public Color(int r, int g, int b)              // Alpha = 255 (opaque)
public Color(int r, int g, int b, int alpha)   // All 0–255

// Byte constructors (direct, no clamping — faster)
public Color(byte r, byte g, byte b, byte alpha)

// Float constructors (0.0f–1.0f per channel)
public Color(float r, float g, float b)              // Alpha = 1.0f
public Color(float r, float g, float b, float alpha)

// Vector constructors
public Color(Vector3 color)   // XYZ = RGB, Alpha = 1.0f
public Color(Vector4 color)   // XYZW = RGBA

// Packed value
public Color(uint packedValue)  // 32-bit RGBA packed
```

Properties: `R`, `G`, `B`, `A` (all `byte`), `PackedValue` (`uint`).

Also has many named static properties: `Color.White`, `Color.Black`, `Color.Transparent`, `Color.Red`, etc.

Source: [Struct Color - MonoGame Documentation](https://docs.monogame.net/api/Microsoft.Xna.Framework.Color.html)

### Implication for the design system
The `Microsoft.Xna.Framework.Color` type is correct. The most common constructor for modding is `new Color(int r, int g, int b)` or `new Color(int r, int g, int b, int alpha)` with values 0–255.

---

## Summary of Findings

| # | Claim | Verdict | Notes |
|---|-------|---------|-------|
| 1 | Separate Game/Render threads with locking | ❌ **INACCURATE** | Single-threaded loop; Update→Draw sequentially on same thread |
| 2 | Tiles are 64×64 (`Game1.tileSize`) | ⚠️ **PARTIALLY ACCURATE** | 64px at 100% zoom; source sprites are 16×16; zoom affects size |
| 3 | `smallFont` ~14px, `tinyFont` ~10px | ⚠️ **UNVERIFIED** | Plausible but not confirmed; use `LineSpacing` at runtime |
| 4 | `SpriteSortMode.Deferred`, `BlendState.AlphaBlend`, `SamplerState.PointClamp` | ✅ **ACCURATE** | Standard settings for pixel-art mod rendering |
| 5 | `Game1.smallFont`, `Game1.tinyFont`, `Game1.dialogueFont`, `Game1.tileSize` | ✅ **ACCURATE** | All real, documented properties |
| 6 | UI Info Suite 2, CJB Item Spawner as references | ✅ **ACCURATE** | Real, popular mods using standard render patterns |
| 7 | `RenderedWorld`, `RenderedHud` events | ✅ **ACCURATE** | Real SMAPI Display events with `SpriteBatch` parameter |
| 8 | `Microsoft.Xna.Framework.Color` | ✅ **ACCURATE** | Correct type with multiple constructor signatures |

---

## Recommendations for the Design System

1. **Remove the dual-thread model.** Replace with a single-threaded game loop description. The "Game Thread" and "Render Thread" should be collapsed into one "Game Loop Thread" with sequential Update→Draw phases.

2. **Clarify tile size.** State that `Game1.tileSize` = 64px at 100% zoom, derived from 16×16 source sprites at 4× scale. Note that zoom level affects this value.

3. **Don't hardcode font sizes.** Use `Game1.smallFont.LineSpacing` and `Game1.smallFont.MeasureString()` at runtime instead of hardcoded pixel values.

4. **Keep the SpriteBatch settings.** `SpriteSortMode.Deferred`, `BlendState.AlphaBlend`, `SamplerState.PointClamp` are correct and best-practice.

5. **Use `RenderedWorld` for world overlays.** This is the correct event for drawing over the world but under menus/HUD.
