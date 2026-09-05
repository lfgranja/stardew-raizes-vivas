# Coordinate Mapping Research: Screen ↔ Tile Conversion in Stardew Valley

**Date:** 2026-01-28  
**Purpose:** Document how Stardew Valley's API handles coordinate conversion between screen pixels and tile (grid) coordinates, for implementing cursor-to-tile tooltip rendering and viewport edge rendering with 1-tile margin.

---

## 1. The Three Coordinate Systems

Stardew Valley uses three related coordinate systems:[^1][^2]

| Coordinate System | Relative To | Unit | Usage |
|---|---|---|---|
| **Tile position** | Top-left corner of the map | Tiles | Placing things on the map (e.g., `location.Objects` uses tile positions) |
| **Absolute position** | Top-left corner of the map | Pixels | Granular measurements (e.g., NPC movement, player position) |
| **Screen position** | Top-left corner of the visible screen | Pixels | Drawing to the screen |

**Key facts:**
- `(0, 0)` is the **top-left** tile of the map
- X increases to the **right**, Y increases **downward**[^1][^3]
- Each tile is **64×64 pixels** (`Game1.tileSize = 64`)[^2][^4]
- Map dimensions: `location.Map.DisplayWidth` × `location.Map.DisplayHeight` in pixels[^2]

---

## 2. Conversion Formulas

The canonical conversion formulas between coordinate systems:[^1][^2]

```
Absolute → Screen:   x - Game1.viewport.X, y - Game1.viewport.Y
Absolute → Tile:     x / Game1.tileSize, y / Game1.tileSize
Screen → Absolute:   x + Game1.viewport.X, y + Game1.viewport.Y
Screen → Tile:       (x + Game1.viewport.X) / Game1.tileSize, (y + Game1.viewport.Y) / Game1.tileSize
Tile → Absolute:     x * Game1.tileSize, y * Game1.tileSize
Tile → Screen:       (x * Game1.tileSize) - Game1.viewport.X, (y * Game1.tileSize) - Game1.viewport.Y
```

**Important implementation notes:**
- Tile coordinates use **integer division with floor** (not truncation): `Math.Floor(position.X / Game1.tileSize)`[^2]
- The `Utility` class contains helper methods for some of these conversions[^1]
- `Game1.GlobalToLocal(absolutePosition)` converts absolute world position to screen position (accounts for viewport scroll and zoom)[^5]

---

## 3. SMAPI Cursor Position API

SMAPI exposes cursor position through `ICursorPosition`, which provides **four coordinate systems** simultaneously:[^6]

```csharp
ICursorPosition cursorPos = this.Helper.Input.GetCursorPosition();
```

| Property | Description |
|---|---|
| `AbsolutePixels` | Pixel position relative to top-left of the in-game map, adjusted for zoom but **not** UI scaling |
| `ScreenPixels` | Pixel position relative to top-left of the visible screen, adjusted for zoom but **not** UI scaling |
| `Tile` | Tile position under the cursor |
| `GrabTile` | Tile position the game considers under the cursor for clicking actions (accounts for controller mode; may differ from `Tile` if too far from player) |

**UI scaling note:** Pixel positions are **not** adjusted for UI scaling. Use `cursorPos.GetScaledAbsolutePixels()` or `cursorPos.GetScaledScreenPixels()` to adjust for current mode, or `Utility.ModifyCoordinatesForUIScale` for UI-mode coordinates.[^6]

### Practical Example: Cursor to Tile

```csharp
// Simplest approach — SMAPI already computes tile coordinates:
Vector2 tilePos = this.Helper.Input.GetCursorPosition().Tile;

// Manual approach from absolute pixels:
Vector2 absolute = this.Helper.Input.GetCursorPosition().AbsolutePixels;
int tileX = (int)Math.Floor(absolute.X / Game1.tileSize);
int tileY = (int)Math.Floor(absolute.Y / Game1.tileSize);

// Manual approach from screen pixels:
Vector2 screen = this.Helper.Input.GetCursorPosition().ScreenPixels;
int tileX = (int)Math.Floor((screen.X + Game1.viewport.X) / Game1.tileSize);
int tileY = (int)Math.Floor((screen.Y + Game1.viewport.Y) / Game1.tileSize);
```

---

## 4. Viewport and Scrolling

### 4.1 Viewport Properties

The viewport represents the visible area on screen:[^2][^7]

| Property | Description |
|---|---|
| `Game1.viewport.Width` | Viewport width in **pixels** (equals screen resolution width) |
| `Game1.viewport.Height` | Viewport height in **pixels** (equals screen resolution height) |
| `Game1.viewport.X` | Absolute X position of viewport's top-left corner (in pixels) |
| `Game1.viewport.Y` | Absolute Y position of viewport's top-left corner (in pixels) |

### 4.2 Visible Tile Range

To calculate which tiles are currently visible:[^2]

```csharp
int visibleTilesX = Game1.viewport.Width / Game1.tileSize;   // typically ~20-22 at 1080p
int visibleTilesY = Game1.viewport.Height / Game1.tileSize;  // typically ~12-14 at 1080p

int leftTile = Game1.viewport.X / Game1.tileSize;
int topTile = Game1.viewport.Y / Game1.tileSize;
int rightTile = leftTile + visibleTilesX;
int bottomTile = topTile + visibleTilesY;
```

### 4.3 Viewport Bounds Are in Pixels

**Viewport bounds are measured in pixels, not tiles.** The viewport dimensions equal the game's screen resolution.[^2] To work in tile space, you must divide by `Game1.tileSize`.

### 4.4 Viewport Scrolling Effect

As the player moves, `Game1.viewport.X` and `Game1.viewport.Y` change to track the camera. This directly affects screen-to-tile conversion because the offset must be added to screen positions before dividing by tile size.[^1][^2]

### 4.5 UI Mode vs Non-UI Mode

In UI mode (when drawing UI elements), replace `Game1.viewport` with `Game1.uiViewport`.[^1][^8] Check `Game1.uiMode` to determine the active mode. Use `Utility.ModifyCoordinatesForUIScale` and `Utility.ModifyCoordinatesFromUIScale` to convert between modes.[^1]

---

## 5. Tile Lookup APIs (GameLocation)

### 5.1 Key Tile-Indexed Collections

| Collection | Type | Key | Contents |
|---|---|---|---|
| `GameLocation.Objects` | `OverlaidDictionary` (essentially `NetVector2Dictionary`) | `Vector2` tile position | Placed objects: fences, machines, etc. |
| `GameLocation.terrainFeatures` | `NetVector2Dictionary<TerrainFeature>` | `Vector2` tile position | Trees, grass, tilled dirt (with crops), flooring |
| `GameLocation.waterTiles` | `bool[,]` | `[tileX, tileY]` | Water tile grid |

### 5.2 Tile Lookup Methods

```csharp
// Check if an object exists at a tile position:
Vector2 tile = new Vector2(tileX, tileY);
if (Game1.currentLocation.Objects.TryGetValue(tile, out Object obj)) { ... }

// Check terrain features (tilled dirt, trees, etc.):
if (Game1.currentLocation.terrainFeatures.TryGetValue(tile, out TerrainFeature feature)) { ... }
// For HoeDirt specifically:
if (feature is HoeDirt dirt) { /* check dirt.crop, dirt.state, etc. */ }

// Check water tiles:
if (Game1.currentLocation.waterTiles[tileX, tileY]) { ... }

// Get tile property (e.g., "Diggable", "Passable"):
string value = Game1.currentLocation.doesTileHaveProperty(tileX, tileY, "Diggable", "Back");
```

### 5.3 Character Position APIs

For NPCs and the player:[^2]

```csharp
// Absolute position (pixels):
Game1.player.Position.X  // float, in pixels
Game1.player.Position.Y  // float, in pixels

// Tile position:
Game1.player.getTileX()  // int, in tiles
Game1.player.getTileY()  // int, in tiles

// Position relative to viewport (screen pixels):
float screenX = Game1.player.Position.X - Game1.viewport.X;
float screenY = Game1.player.Position.Y - Game1.viewport.Y;
```

---

## 6. Zoom and UI Scaling Considerations

### 6.1 Zoom Level

- Range: 75% to 200% (`Game1.options.zoomLevel`)[^1]
- Coordinates are generally adjusted for zoom automatically
- To convert unadjusted: `position * (1f / Game1.options.zoomLevel)`[^1]

### 6.2 UI Scaling

- Range: 75% to 150% (separate from zoom)[^1]
- Two scaling modes: **UI mode** and **non-UI mode**
- Pixel positions from `ICursorPosition` are in **non-UI mode** by default
- Use `GetScaledAbsolutePixels()` / `GetScaledScreenPixels()` for automatic adjustment[^6]

### 6.3 When to Use UI Mode

| Context | Mode |
|---|---|
| Clickable menus | UI mode |
| HUD elements | UI mode |
| `RenderingActiveMenu` / `RenderedActiveMenu` events | UI mode |
| World object `draw` method | Non-UI mode |
| Tile coordinates | Not affected by UI scaling |

---

## 7. Pattern: 1-Tile Margin Beyond Visible Bounds

For rendering overlays with a 1-tile margin beyond the visible viewport (to avoid pop-in at edges):

```csharp
// Calculate visible tile range with 1-tile margin
int margin = 1;
int startTileX = (int)Math.Floor(Game1.viewport.X / (float)Game1.tileSize) - margin;
int startTileY = (int)Math.Floor(Game1.viewport.Y / (float)Game1.tileSize) - margin;
int endTileX = (int)Math.Floor((Game1.viewport.X + Game1.viewport.Width) / (float)Game1.tileSize) + margin;
int endTileY = (int)Math.Floor((Game1.viewport.Y + Game1.viewport.Height) / (float)Game1.tileSize) + margin;

// Clamp to map bounds
int mapTilesX = (int)Math.Floor(Game1.currentLocation.Map.DisplayWidth / (float)Game1.tileSize);
int mapTilesY = (int)Math.Floor(Game1.currentLocation.Map.DisplayHeight / (float)Game1.tileSize);
startTileX = Math.Max(0, startTileX);
startTileY = Math.Max(0, startTileY);
endTileX = Math.Min(mapTilesX - 1, endTileX);
endTileY = Math.Min(mapTilesY - 1, endTileY);

// Iterate and render
for (int x = startTileX; x <= endTileX; x++)
{
    for (int y = startTileY; y <= endTileY; y++)
    {
        Vector2 tilePos = new Vector2(x, y);
        // Convert tile to screen position for rendering:
        Vector2 screenPos = TileToScreen(x, y);
        // ... render overlay at screenPos
    }
}

// Helper: Tile → Screen conversion
Vector2 TileToScreen(int tileX, int tileY)
{
    float x = (tileX * Game1.tileSize) - Game1.viewport.X;
    float y = (tileY * Game1.tileSize) - Game1.viewport.Y;
    return new Vector2(x, y);
}
```

---

## 8. Pattern: Cursor-to-Tile for Tooltip Rendering

```csharp
// In RenderedWorld or RenderedActiveMenu event:
private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
{
    ICursorPosition cursor = this.Helper.Input.GetCursorPosition();
    
    // Option A: Use SMAPI's built-in tile conversion (recommended)
    Vector2 tilePos = cursor.Tile;
    
    // Option B: Manual from absolute pixels (if you need sub-tile precision)
    Vector2 absolute = cursor.AbsolutePixels;
    int tileX = (int)Math.Floor(absolute.X / Game1.tileSize);
    int tileY = (int)Math.Floor(absolute.Y / Game1.tileSize);
    
    // Check if hovering over a tilled soil tile:
    Vector2 tileKey = new Vector2(tileX, tileY);
    if (Game1.currentLocation.terrainFeatures.TryGetValue(tileKey, out TerrainFeature feature) 
        && feature is HoeDirt dirt)
    {
        // Get soil health data and render tooltip
        float health = soilHealthService.GetSoilHealth(Game1.currentLocation.Name, tileKey);
        Vector2 screenPos = TileToScreen(tileX, tileY);
        // Draw tooltip at screenPos...
    }
}
```

---

## 9. Key Constants and Formulas Summary

| Constant/Value | Description |
|---|---|
| `Game1.tileSize` | 64 (pixels per tile) |
| `Game1.viewport.Width` | Screen width in pixels |
| `Game1.viewport.Height` | Screen height in pixels |
| `Game1.viewport.X` | Viewport left edge in absolute pixels |
| `Game1.viewport.Y` | Viewport top edge in absolute pixels |
| `Game1.options.zoomLevel` | Zoom factor (0.75–2.0) |
| `Game1.uiMode` | True when in UI scaling mode |
| `Game1.uiViewport` | Use instead of `viewport` in UI mode |

**Core conversion chain:**
```
Screen Pixels → (+ viewport offset) → Absolute Pixels → (/ tileSize) → Tile Coordinates
Tile Coordinates → (* tileSize) → Absolute Pixels → (- viewport offset) → Screen Pixels
```

---

## 10. References

[^1]: [Modding:Modder Guide/Game Fundamentals - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals) — Three coordinate systems, conversion formulas, zoom/UI scaling
[^2]: [Modding:Common tasks - Stardew Valley Wiki](https://wiki.stardewvalley.net/Modding:Common_tasks) — Position relative to map/viewport, tile size, viewport dimensions
[^3]: [Modding:Maps - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Maps) — Tile coordinate system, (0,0) top-left
[^4]: [Modding:Location data - Stardew Valley Wiki](https://wiki.stardewvalley.net/Modding:Location_data) — Location/map terminology
[^5]: [Immersive Sprinklers Rendering Pipeline - DeepWiki](https://deepwiki.com/aedenthorn/StardewValleyMods/5.1.2-rendering-pipeline) — `Game1.GlobalToLocal()` usage, tile→screen position calculation
[^6]: [Modding:Modder Guide/APIs/Input - Stardew Valley Wiki](https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Input) — `ICursorPosition` four coordinate systems, UI scaling
[^7]: [Modding:World map - Stardew Valley Wiki](https://wiki.stardewvalley.net/Modding:World_map) — Viewport and map area positioning
[^8]: [Modding:Migrate to Stardew Valley 1.5 - Stardew Valley Wiki](https://wiki.stardewvalley.net/Modding:Migrate_to_Stardew_Valley_1.5) — UI mode viewport replacement

### Additional Community Resources

- [stardew-teleport by patapq](https://github.com/patapq/stardew-teleport) — Example mod using `e.Cursor.AbsolutePixels` for cursor-to-position mapping
- [Player Position HUD and Logger (Nexus Mods)](https://www.nexusmods.com/stardewvalley/mods/7969) — Logs cursor coordinates to file
- [Ilucie's Location Tracker (Nexus Mods)](https://www.nexusmods.com/stardewvalley/mods/31622) — Displays current tile position
- [Debug Mode (Nexus Mods)](https://www.nexusmods.com/stardewvalley/mods/679) — Shows tile coordinates in-game
- [TileSense (Nexus Mods)](https://www.nexusmods.com/stardewvalley/mods/41612) — Tile property inspector with pixel-perfect alignment
