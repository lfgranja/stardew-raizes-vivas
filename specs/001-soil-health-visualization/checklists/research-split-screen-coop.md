# Research: Stardew Valley Split-Screen Local Co-op for SMAPI Mod Development

**Date:** 2026-09-09
**Purpose:** Technical reference for implementing per-player tile overlays (soil health visualization) in split-screen local co-op mode.

---

## 1. Split-Screen Viewport Behavior

### How It Works

In Stardew Valley's local co-op (split-screen), the game runs **multiple game instances** on a single machine. Each player gets their own independent viewport/camera that renders to a portion of the screen.

**Key technical details:**

- The game uses `GameRunner.instance.gameInstances` to manage multiple game instances — one per local player. [\[source\]](https://github.com/alanperrow/StardewModding/blob/master/SplitscreenImproved/Game1Patches.cs)
- Each instance has an `instanceIndex` that identifies which player/screen it corresponds to.
- The method `Game1.SetWindowSize()` reads a `screen_splits` array indexed by `Game1.game1.instanceIndex` to determine each player's viewport rectangle (a `Vector4` representing the sub-region of the screen). [\[source\]](https://github.com/alanperrow/StardewModding/blob/master/SplitscreenImproved/Game1Patches.cs)
- The game **swaps the entire game state** between each player. With four local players, `UpdateTicked` fires four times per tick. [\[source\]](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer)

### Viewport Per Player

Each player's viewport is independent:
- `Game1.viewport` is the **current player's** viewport (the one being rendered this frame).
- The viewport position (`Game1.viewport.X`, `Game1.viewport.Y`) determines which part of the world is visible to the current player.
- In split-screen, `Game1.viewport` changes for each player as the game renders each sub-region.

### Split Layouts

The vanilla game has hard-coded split layouts (side-by-side for 2 players, quad for 4 players). The **Split Screen Manager** mod by RomenH was the first to make these customizable by patching `Game1.SetWindowSize`. [\[source\]](https://www.nexusmods.com/stardewvalley/mods/12063) The **Splitscreen Improved** mod by gaussfire/alanperrow continues this approach with a transpiler patch. [\[source\]](https://github.com/alanperrow/StardewModding/tree/master/SplitscreenImproved)

---

## 2. Cursor Handling in Split-Screen

### Per-Player Cursor

Each local player has their own cursor position. The game tracks this through:

- `Game1.currentCursorTile` — the tile the cursor is hovering over **for the current player/screen**.
- `Game1.player` — the **current player** (the one whose screen is being rendered).

**Important:** In split-screen, `Game1.player` and `Game1.currentCursorTile` are swapped as the game renders each player's viewport. They always reflect the **currently active** player instance.

### SMAPI Cursor APIs

SMAPI provides cursor position through:

- **`Input.CursorMoved` event** — raised after the player moves the in-game cursor. Event args provide `e.OldPosition` and `e.NewPosition` as `ICursorPosition`. [\[source\]](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Input.CursorMoved)
- **`ICursorPosition`** — contains both screen-space and tile-space coordinates, plus the "grab tile" (the tile the player is actively interacting with). [\[source\]](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input#Check_cursor_position)

### Known Issue: Cursor Restricted in Split-Screen

There is a known issue where the cursor is restricted in movement within each split-screen window and cannot reach the toolbar or menu buttons. This was reported with the Universal Split Screen mod. [\[source\]](https://www.reddit.com/r/StardewValley/comments/f1b182/universal_split_screen_cursor_problem/) This is a game-level input routing issue, not something mods can easily fix.

---

## 3. Rendering Layers

### Map Layers (TMX/TBIN)

Stardew Valley maps have four primary layers, drawn in this order: [\[source\]](https://stardewvalleywiki.com/Modding:Maps)

| Layer | Typical Contents | Draw Behavior |
|-------|-----------------|---------------|
| **Back** | Terrain, water, basic features (permanent paths) | Drawn first (bottom) |
| **Buildings** | Building placeholders, walls | Acts as collision walls unless tile has "Passable" property |
| **Front** | Trees, objects drawn on top of things behind them | Drawn on top of player if player is **north** of them; behind player if player is **south** |
| **AlwaysFront** | Foreground effects (foliage cover) | Always drawn on top of everything including the player |

**Custom layers** can be added by suffixing a vanilla layer name with an offset (e.g., `Back-1`, `Back2`). This only affects rendering; original layers must be used for tile properties and collisions. [\[source\]](https://stardewvalleywiki.com/Modding:Maps)

### Sprite Batch Draw Order (Game Code)

The game's draw order within a single frame (from back to front) is approximately:

1. **Background** (sky, weather)
2. **World layers** (Back → Buildings → Front → AlwaysFront) — drawn via `GameLocation.draw()` with sprite batch layers
3. **Characters/NPCs** (sorted by Y-position for depth)
4. **Player(s)** (sorted by Y-position relative to objects)
5. **HUD** (toolbar, clock, etc.)
6. **Cursor**
7. **Active menus**

### Where to Render a Tile Overlay

For a soil health overlay that should appear **above the tile but below characters/buildings**:

- **Option A: Use `Display.RenderedWorld` event** — draw after the world but before HUD/menus. This renders in screen space and works with the current player's viewport. [\[source\]](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Display.RenderedWorld)
- **Option B: Use `Display.RenderingStep` with specific render steps** — more granular but more fragile across game updates. [\[source\]](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Display.RenderingStep)
- **Option C: Custom map layer** — add a `Back+1` or `Front-1` layer via Content Patcher for static overlays (less flexible for dynamic data).

**Recommended approach:** Use `Display.RenderedWorld` with `PerScreen<T>` state to render per-player overlays. Draw using the current `Game1.viewport` to convert tile coordinates to screen coordinates.

---

## 4. SMAPI / Game1 APIs

### Detecting Split-Screen Mode

```csharp
// Check if the current player is in split-screen mode
bool isSplitScreen = Context.IsMultiplayer && Game1.player.IsSplitScreen();
```

From `IMultiplayerPeer`: [\[source\]](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer)

| Field | Type | Description |
|-------|------|-------------|
| `IsSplitScreen` | `bool` | True if the player is running on the same computer in split-screen mode |
| `ScreenID` | `int?` | The player's screen ID in split-screen mode, if `IsSplitScreen` is true |

### Getting Each Player's Viewport

```csharp
// The current player's viewport (changes per-player in split-screen)
Viewport viewport = Game1.viewport;
int viewportX = viewport.X;
int viewportY = viewport.Y;

// Convert tile to screen position for the current player
int screenX = (tileX * Game1.tileSize) - viewport.X;
int screenY = (tileY * Game1.tileSize) - viewport.Y;
```

Coordinate conversion formulas: [\[source\]](https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals#Positions)

| Conversion | Formula |
|-----------|---------|
| absolute → screen | `x - Game1.viewport.X, y - Game1.viewport.Y` |
| screen → absolute | `x + Game1.viewport.X, y + Game1.viewport.Y` |
| screen → tile | `(x + Game1.viewport.X) / Game1.tileSize, (y + Game1.viewport.Y) / Game1.tileSize` |
| tile → absolute | `x * Game1.tileSize, y * Game1.tileSize` |
| tile → screen | `(x * Game1.tileSize) - Game1.viewport.X, (y * Game1.tileSize) - Game1.viewport.Y` |

### Getting Each Player's Cursor Position

```csharp
// Current player's cursor tile (changes per-player in split-screen)
Vector2 cursorTile = Game1.currentCursorTile;

// Via SMAPI event
helper.Events.Input.CursorMoved += (sender, e) =>
{
    Vector2 tile = e.NewPosition.Tile;
    Vector2 screenPixel = e.NewPosition.ScreenPixels;
};
```

### Per-Screen State Management

SMAPI provides `PerScreen<T>` to store per-player state automatically: [\[source\]](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer#Per-screen_data)

```csharp
private readonly PerScreen<Color[]> OverlayColors = new PerScreen<Color[]>();
private readonly PerScreen<bool> ShowOverlay = new PerScreen<bool>(createNewState: () => true);

// Access current player's value
bool shouldShow = ShowOverlay.Value;

// Access specific player's value
bool player2Show = ShowOverlay.GetValueForScreen(1);

// Iterate all active screens
foreach (var (screenId, value) in ShowOverlay.GetActiveValues())
{
    // screenId is the player's screen index
}
```

### Key Game1 Fields for Split-Screen

| Field | Type | Purpose |
|-------|------|---------|
| `Game1.player` | `Farmer` | The **current** player (swapped per-screen in split-screen) |
| `Game1.currentLocation` | `GameLocation` | The current player's location |
| `Game1.viewport` | `Viewport` | The current player's viewport |
| `Game1.currentCursorTile` | `Vector2` | The current player's cursor tile |
| `Game1.getOnlineFarmers()` | `IEnumerable<Farmer>` | All connected players |
| `Game1.game1.instanceIndex` | `int` | Current game instance index (0 for main player) |

---

## 5. Existing Mod Examples

### Split Screen Manager (RomenH)

- **Nexus:** https://www.nexusmods.com/stardewvalley/mods/12063
- **Approach:** Replaces the game's hard-coded split-screen viewports with custom ones from a config file.
- **Compatibility:** Supports 2-4 players with customizable layouts.
- **Note:** No longer supported; superseded by Splitscreen Improved.

### Splitscreen Improved (gaussfire / alanperrow)

- **Nexus:** https://www.nexusmods.com/stardewvalley/mods/24507
- **GitHub:** https://github.com/alanperrow/StardewModding/tree/master/SplitscreenImproved
- **Approach:** Harmony transpiler patch on `Game1.SetWindowSize` to replace the `screen_splits[instanceIndex]` lookup with a custom layout system.
- **Key implementation detail:** The transpiler finds the IL instruction `screen_splits[Game1.game1.instanceIndex]` and replaces it with a call to `GetScreenSplit()` which returns a custom `Vector4` from the mod's layout config. [\[source\]](https://github.com/alanperrow/StardewModding/blob/master/SplitscreenImproved/Game1Patches.cs)
- **Features:** Custom layouts, music fix, HUD tweaks (chat/buff offset), player name in summary menus.
- **Relevance to tile overlays:** This mod demonstrates how to intercept the per-player viewport system but does NOT render per-player overlays itself.

### Data Layers (Nexus Mod 1691)

- **Nexus:** https://www.nexusmods.com/stardewvalley/mods/1691
- Shows coverage areas for bee houses and sprinklers using tile overlays.
- Does not specifically address split-screen rendering.

### UI Info Suite 2

- **Nexus:** https://www.nexusmods.com/stardewvalley/mods/7098
- Provides various HUD overlays and tile information.
- Does not specifically handle split-screen per-player rendering.

### Per-Player Overlay Pattern (Recommended)

No existing mod was found that renders per-player tile overlays in split-screen. The recommended pattern based on research:

```csharp
// In ModEntry.cs
private readonly PerScreen<SoilOverlayState> OverlayState = new PerScreen<SoilOverlayState>();

public override void Entry(IModHelper helper)
{
    helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
}

private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
{
    // This event fires once per player per frame in split-screen
    // Game1.viewport, Game1.player, Game1.currentCursorTile are all
    // set to the CURRENT player being rendered
    
    var state = this.OverlayState.Value;
    var spriteBatch = e.SpriteBatch;
    var viewport = Game1.viewport;
    
    // Render overlay tiles using current player's viewport
    foreach (var tile in state.GetVisibleTiles(viewport))
    {
        int screenX = (tile.X * Game1.tileSize) - viewport.X;
        int screenY = (tile.Y * Game1.tileSize) - viewport.Y;
        
        spriteBatch.Draw(
            texture: state.OverlayTexture,
            position: new Vector2(screenX, screenY),
            sourceRectangle: null,
            color: state.GetColor(tile),
            rotation: 0f,
            origin: Vector2.Zero,
            scale: 1f,
            effects: SpriteEffects.None,
            layerDepth: 0.0001f  // Just above the tile layer
        );
    }
}
```

---

## 6. Key Takeaways for Soil Health Visualization

1. **Each player gets their own viewport** — `Game1.viewport` is per-player in split-screen. Use it to determine which tiles are visible.

2. **Use `PerScreen<T>` for per-player state** — SMAPI automatically swaps this per-player. Store overlay visibility, cached colors, etc. here.

3. **Render in `Display.RenderedWorld`** — This fires once per player per frame. Draw using the current `Game1.viewport` for correct positioning.

4. **Coordinate conversion is critical** — Always convert tile → screen using the current `Game1.viewport`. The formula is: `screenPos = (tilePos * tileSize) - viewport`.

5. **No existing mod does per-player tile overlays** — This is novel territory. The pattern above is the recommended approach based on how the game's split-screen rendering works.

6. **Cursor position is per-player** — `Game1.currentCursorTile` reflects the current player. Use `Input.CursorMoved` event for reactive updates.

7. **Layer depth** — For tile overlays, use a `layerDepth` close to 0 (but above 0) in the `RenderedWorld` event to render above tiles but below characters. Alternatively, use `Display.Rendered` to render above everything including HUD.

---

## Sources

- [SMAPI Multiplayer API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer)
- [SMAPI Events API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events)
- [Game Fundamentals (Positions/Coordinates)](https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals)
- [Modding:Maps (Layers)](https://stardewvalleywiki.com/Modding:Maps)
- [Split Screen Manager (Nexus)](https://www.nexusmods.com/stardewvalley/mods/12063)
- [Splitscreen Improved (Nexus)](https://www.nexusmods.com/stardewvalley/mods/24507)
- [Splitscreen Improved (GitHub)](https://github.com/alanperrow/StardewModding/tree/master/SplitscreenImproved)
- [Splitscreen Improved Game1Patches.cs](https://github.com/alanperrow/StardewModding/blob/master/SplitscreenImproved/Game1Patches.cs)
- [Stardew Valley Decompiled Source (GitHub)](https://github.com/WeDias/StardewValley)
- [Universal Split Screen Cursor Issue (Reddit)](https://www.reddit.com/r/StardewValley/comments/f1b182/universal_split_screen_cursor_problem/)
- [Stardew Valley Multiplayer (Wiki)](https://stardewvalleywiki.com/Multiplayer)
