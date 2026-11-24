# Stardew Valley Mod UI Overlays, Tooltips & Visual Feedback — Research Report

**Date:** 2026-09-09
**Scope:** Analysis of established Stardew Valley mods (UI Info Suite 2, CJB Item Spawner, Data Layers, and others) to inform the Living Roots design system for overlay rendering, tooltip display, visual feedback, configuration, console commands, and accessibility.

---

## Table of Contents

1. [UI Info Suite 2 — Rendering & Tooltips](#1-ui-info-suite-2)
2. [CJB Item Spawner — UI Patterns](#2-cjb-item-spawner)
3. [Data Layers — Tile Overlay System](#3-data-layers)
4. [Tooltip Patterns Across Mods](#4-tooltip-patterns)
5. [Configuration Patterns](#5-configuration-patterns)
6. [Console Command Patterns](#6-console-command-patterns)
7. [Accessibility in Stardew Mods](#7-accessibility)
8. [Recommendations for Living Roots](#8-recommendations)
9. [Sources](#9-sources)

---

## 1. UI Info Suite 2

**Repository:** https://github.com/Annosz/UIInfoSuite2
**Nexus:** https://www.nexusmods.com/stardewvalley/mods/7098

### 1.1 Rendering Approach

UI Info Suite 2 uses SMAPI's **`Events.Display.RenderingHud`** event to render overlays directly onto the game's HUD layer. This is the most common approach for tile-level visual feedback.

#### Key Pattern: `OnRenderingHud` Event Handler

From `ShowItemEffectRanges.cs` (full source inspected):

```csharp
private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
{
    if (_mutex.WaitOne(0))
    {
        try
        {
            foreach (Point point in _effectiveAreaOther.Value)
            {
                var position = new Vector2(
                    point.X * Utility.ModifyCoordinateFromUIScale(Game1.tileSize),
                    point.Y * Utility.ModifyCoordinateFromUIScale(Game1.tileSize)
                );
                e.SpriteBatch.Draw(
                    Game1.mouseCursors,
                    Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(Utility.ModifyCoordinatesForUIScale(position))),
                    new Rectangle(194, 388, 16, 16),
                    Color.White * 0.7f,  // <-- 70% opacity white highlight
                    0.0f,
                    Vector2.Zero,
                    Utility.ModifyCoordinateForUIScale(Game1.pixelZoom),
                    SpriteEffects.None,
                    0.01f
                );
            }

            // Overlapping areas drawn in RED
            foreach (Point point in _effectiveAreaIntersection.Value)
            {
                e.SpriteBatch.Draw(
                    Game1.mouseCursors,
                    /* ... */ position,
                    new Rectangle(194, 388, 16, 16),
                    Color.Red * 0.7f,  // <-- Red highlight for overlaps
                    /* ... */
                );
            }
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }
}
```

**Key observations:**
- Uses `Game1.mouseCursors` as the texture source (a built-in sprite sheet)
- Draws 16×16 pixel sprites scaled to tile size
- Uses **alpha blending** (`Color.White * 0.7f`) for semi-transparent overlays
- Supports **multiple colors** (white for coverage, red for overlaps)
- Uses coordinate transformation: tile → pixel → local/screen space via `Game1.GlobalToLocal()`
- Accounts for UI scale via `Utility.ModifyCoordinatesForUIScale()`

#### Performance Handling

The mod uses several performance optimization strategies:

1. **Throttled updates:** Only recalculates every 4 game ticks (`e.IsMultipleOf(4)`)
2. **Mutex for thread safety:** Uses `Mutex` to prevent concurrent modification of shared tile data
3. **Per-screen data:** Uses `PerScreen<T>` wrappers so data is per-player in split-screen
4. **Conditional rendering:** Only draws when `Game1.activeClickableMenu == null` (no menu open) and `UIElementUtils.IsRenderingNormally()`

```csharp
private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
{
    if (!e.IsMultipleOf(4))  // Only every 4 ticks
        return;
    
    if (Game1.currentLocation is null)
        return;

    if (_mutex.WaitOne())
    {
        try {
            _effectiveAreaCurrent.Value.Clear();
            _effectiveAreaOther.Value.Clear();
            _effectiveAreaIntersection.Value.Clear();
        }
        finally { _mutex.ReleaseMutex(); }
    }

    if (Game1.activeClickableMenu == null && UIElementUtils.IsRenderingNormally())
    {
        UpdateEffectiveArea();
        GetOverlapValue();
    }
}
```

### 1.2 Tile Indicator System

The mod maintains three separate tile collections:
- `_effectiveAreaCurrent` — tiles for the currently-held item
- `_effectiveAreaOther` — tiles for similar items already placed
- `_effectiveAreaIntersection` — overlapping tiles between the two

This allows color-coding: white for current item coverage, white (different set) for existing items, red for overlaps.

### 1.3 Tooltip Rendering

For crop/machine time tooltips, UI Info Suite 2 uses the game's **native tooltip method**:

From `ShowCropAndBarrelTime.cs`:

```csharp
private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
{
    if (Game1.activeClickableMenu != null)
        return;

    List<string> lines = new();
    // ... populate lines with crop/machine data ...

    if (lines.Count <= 0)
        return;

    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
    {
        overrideX = (int)(tile.X + Utility.ModifyCoordinateFromUIScale(32));
        overrideY = (int)(tile.Y + Utility.ModifyCoordinateFromUIScale(32));
    }

    IClickableMenu.drawHoverText(
        Game1.spriteBatch,
        string.Join('\n', lines),
        Game1.smallFont,
        overrideX: overrideX,
        overrideY: overrideY
    );
}
```

**Key observations:**
- Uses `IClickableMenu.drawHoverText()` — the game's built-in tooltip renderer
- This produces the **native Stardew Valley tooltip style** (dark background, white text, familiar font)
- Uses `Game1.smallFont` for text
- Supports gamepad mode with position overrides
- Multiple lines joined with `\n`

### 1.4 Feature Architecture

Each feature is a separate class implementing `IDisposable`:
- `ShowItemEffectRanges` — sprinkler/scarecrow/junimo ranges
- `ShowCropAndBarrelTime` — crop harvest times, machine processing
- `ShowWhenAnimalNeedsPet` — animal pet indicators
- `LuckOfDay` — daily luck icon
- `ExperienceBar` — dynamic XP bar
- `LocationOfTownsfolk` — NPC map locations

Each follows the same lifecycle pattern:
```csharp
public void ToggleOption(bool show)
{
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
    if (show)
    {
        _helper.Events.Display.RenderingHud += OnRenderingHud;
        _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
}
```

---

## 2. CJB Item Spawner

**Repository:** https://github.com/CJBok/SDV-Mods (CJBItemSpawner folder)
**Nexus:** https://www.nexusmods.com/stardewvalley/mods/93

### 2.1 UI Architecture

CJB Item Spawner creates a **full custom menu** by extending `ItemGrabMenu` (a built-in `IClickableMenu` subclass):

```csharp
internal class ItemMenu : ItemGrabMenu
{
    // Custom UI components
    private ClickableComponent SortButton;
    private ClickableComponent QualityButton;
    private Dropdown<string> CategoryDropdown;
    private ClickableTextureComponent SortIcon;
    private ClickableTextureComponent UpArrow;
    private ClickableTextureComponent DownArrow;
    private TextBox SearchBox;
    private ClickableComponent SearchBoxArea;
    private ClickableTextureComponent SearchIcon;
    private readonly List<ClickableComponent> ChildComponents = [];
}
```

### 2.2 Rendering Approach

The menu overrides `draw(SpriteBatch)` to render custom UI elements on top of the base game menu:

```csharp
public override void draw(SpriteBatch spriteBatch)
{
    // Draw background overlay
    if (!Game1.options.showMenuBackground && !Game1.options.showClearBackgrounds)
        spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * .4f);

    // Draw scroll arrows
    if (this.CanScrollUp)
        this.UpArrow.draw(spriteBatch);
    if (this.CanScrollDown)
        this.DownArrow.draw(spriteBatch);
    
    // Draw base menu (via reflection workaround)
    this.BaseDraw(spriteBatch);

    // Draw custom UI elements
    CommonHelper.DrawTab(/* search box background */);
    this.SearchBox.Draw(spriteBatch);
    spriteBatch.Draw(this.SearchIcon.texture, /* ... */);
    
    CommonHelper.DrawTab(/* quality button */);
    CommonHelper.DrawTab(/* sort button */);
    this.SortIcon.draw(spriteBatch);
    
    // Draw quality icon
    this.GetQualityIcon(out Texture2D texture, out Rectangle sourceRect, out Color color);
    spriteBatch.Draw(texture, new Vector2(qualityIconPos.X, qualityIconPos.Y), sourceRect, color);
    
    // Draw category dropdown
    this.CategoryDropdown.Draw(spriteBatch);

    // Redraw cursor over new UI
    this.drawMouse(spriteBatch);
}
```

**Key observations:**
- Uses `CommonHelper.DrawTab()` for consistent UI element backgrounds
- Draws a semi-transparent black overlay (`Color.Black * .4f`) behind the menu
- Uses custom `Dropdown<T>` component for category selection
- Uses `TextBox` from Stardew Valley's built-in UI for search
- Redraws the mouse cursor last to ensure it's on top
- Uses `Game1.mouseCursors` texture for icons
- Supports custom textures loaded via content pipeline (`content.Load<Texture2D>("assets/empty-quality.png")`)

### 2.3 Controller Support

The mod implements full controller/gamepad support with navigation flow:

```csharp
private void InitializeControllerFlow()
{
    this.ChildComponents.Clear();
    this.ChildComponents.AddRange([this.QualityButton, this.SortButton, this.UpArrow, this.DownArrow, this.SearchBoxArea, this.CategoryDropdown]);
    
    // Set IDs
    int curId = 1_000_000;
    foreach (ClickableComponent component in this.ChildComponents)
        component.myId = curId++;
    
    // Set navigation neighbors
    this.QualityButton.rightNeighborID = this.SortButton.myId;
    this.SortButton.rightNeighborID = this.CategoryDropdown.myId;
    // ... etc
}
```

### 2.4 UI Patterns Summary

| Pattern | Implementation |
|---------|---------------|
| Menu type | Custom `IClickableMenu` via `ItemGrabMenu` subclass |
| Background | Semi-transparent black overlay |
| Buttons | `ClickableComponent` with `CommonHelper.DrawTab()` backgrounds |
| Dropdowns | Custom `Dropdown<T>` component |
| Search | Built-in `TextBox` with hover-based selection |
| Scroll | ClickableTextureComponent arrows + scroll wheel |
| Icons | SpriteBatch.Draw from mouseCursors or custom textures |
| Controller | Neighbor-ID-based navigation flow |
| Pagination | TopRowIndex with ItemsPerView/ItemsPerRow |

---

## 3. Data Layers

**Repository:** https://github.com/Pathoschild/StardewMods (DataLayers folder, develop branch)
**Nexus:** https://www.nexusmods.com/stardewvalley/mods/1691

### 3.1 Architecture Overview

Data Layers is the most sophisticated tile overlay mod, implementing a **layer-based rendering system**:

```
DataLayers/
├── Layers/
│   ├── BaseLayer.cs          — Abstract base for all layers
│   ├── AccessibleLayer.cs    — Walkable/door/warp overlay
│   ├── Coverage/             — Sprinkler/scarecrow/junimo coverage
│   ├── Crops/                — Crop state overlay
│   ├── GridLayer.cs          — Tile grid
│   ├── MachineLayer.cs       — Machine state
│   ├── TillableLayer.cs      — Tillable soil
│   ├── FishingDepthLayer.cs  — Fishing depth
│   ├── BuildableLayer.cs     — Buildable tiles
│   └── ModLayer.cs           — Extensible layer API
├── Framework/
│   ├── DataLayerOverlay.cs   — Main rendering orchestrator
│   ├── TileGroup.cs          — Group of tiles with shared properties
│   ├── TileData.cs           — Per-tile data (color, offset)
│   ├── TileDrawData.cs       — Aggregated draw data
│   ├── TileEdge.cs           — Edge flags for borders
│   ├── ColorScheme.cs        — Color configuration
│   ├── ColorRegistry.cs      — Multiple color schemes
│   ├── LegendEntry.cs        — Legend item definition
│   └── LegendComponent.cs    — UI for layer legend
```

### 3.2 Rendering Pipeline

The `DataLayerOverlay` class extends `BaseOverlay` and implements a two-phase rendering pipeline:

#### Phase 1: World Rendering (`DrawWorld`)

```csharp
protected override void DrawWorld(SpriteBatch spriteBatch)
{
    if (!this.DrawOverlay())
        return;

    int tileSize = Game1.tileSize;
    const int borderSize = 4;
    IDictionary<Vector2, TileDrawData> tiles = this.AggregateTileData(this.TileGroups, this.CombineOverlappingBorders);

    foreach (Vector2 tilePos in this.VisibleTiles)
    {
        Vector2 pixelPosition = tilePos * tileSize - new Vector2(Game1.viewport.X, Game1.viewport.Y);

        if (tiles.TryGetValue(tilePos, out TileDrawData? tile))
        {
            // Draw colored overlay (30% opacity)
            foreach (Color color in tile.Colors)
                spriteBatch.Draw(CommonHelper.Pixel, new Rectangle(
                    (int)pixelDrawPosition.X, (int)pixelDrawPosition.Y, 
                    tileSize, tileSize), 
                    color * .3f);

            // Draw group borders (solid, 4px wide)
            foreach (Color color in tile.BorderColors.Keys)
            {
                TileEdge edges = tile.BorderColors[color];
                this.DrawBorder(spriteBatch, pixelDrawPosition, TileEdge.Left, color, 
                    edges.HasFlag(TileEdge.Left) ? borderSize : gridSize);
                // ... right, top, bottom
            }
        }

        // Draw grid lines (1px, 50% opacity)
        if (gridSize > 0)
        {
            Color color = (tile?.Colors.First() ?? this.GridColor) * 0.5f;
            this.DrawBorder(spriteBatch, pixelPosition, TileEdge.Left, color, width);
            // ... etc
        }
    }
}
```

**Key observations:**
- Uses `CommonHelper.Pixel` (a 1×1 white texture) for filled rectangles — this is the standard approach
- Renders in `DrawWorld` (before UI) rather than `RenderingHud`
- Supports **multiple colors per tile** (layered overlays)
- Draws **borders** around groups of tiles (4px solid lines)
- Draws optional **grid lines** (1px, semi-transparent)
- 30% opacity for fill colors, 50% for grid lines
- Uses `AggregateTileData()` to merge overlapping tile groups efficiently

#### Phase 2: UI Rendering (`DrawUi`)

```csharp
protected override void DrawUi(SpriteBatch batch)
{
    if (this.DrawOverlay() && Game1.displayHUD)
    {
        this.Legend.Draw(batch);
        this.PrevButton.draw(batch);
        this.NextButton.draw(batch);
    }
}
```

### 3.3 Color Scheme System

Data Layers implements a configurable color scheme system:

```csharp
public class ColorScheme
{
    public string Name { get; }
    public Dictionary<string, Color> Colors { get; }
    // ...
}
```

The mod ships with multiple built-in color schemes (Default, Colorblind-friendly, etc.) and supports custom schemes via JSON files. Colors are defined per-category (e.g., "sprinkler", "scarecrow", "junimo") and applied to legend entries.

### 3.4 Layer Update System

Each layer implements `ILayer` with configurable update rates:

```csharp
public interface ILayer
{
    string Id { get; }
    string Name { get; }
    int UpdateTickRate { get; }           // How often to recalculate
    bool UpdateWhenVisibleTilesChange { get; }  // Recalculate on viewport move
    KeybindList ShortcutKey { get; }
    LegendEntry[] Legend { get; }
    bool AlwaysShowGrid { get; }
    
    bool UpdateMetadata();
    IReadOnlyCollection<TileGroup> Update(
        ref readonly GameLocation location,
        ref readonly Rectangle visibleArea,
        ref readonly IReadOnlySet<Vector2> visibleTiles,
        ref readonly Vector2 cursorTile);
}
```

The `BaseLayer` constructor converts updates-per-second to tick rate:
```csharp
this.UpdateTickRate = (int)(60 / config.UpdatesPerSecond);
```

### 3.5 Performance Optimizations

- **Visible tile culling:** Only processes tiles within the visible viewport (`GetVisibleRadiusArea`)
- **Configurable update rate:** Each layer defines its own tick rate
- **View-change detection:** Only recalculates when visible area changes (if configured)
- **Tile grouping:** Adjacent tiles with same color are grouped for efficient rendering
- **Border merging:** Optional `CombineOverlappingBorders` to reduce draw calls
- **Early exit:** `DrawOverlay()` check before any rendering

---

## 4. Tooltip Patterns Across Mods

### 4.1 Native Tooltip Style (`IClickableMenu.drawHoverText`)

Used by: **UI Info Suite 2**, **Show Item Sell Price**, and most tooltip mods.

```csharp
IClickableMenu.drawHoverText(
    Game1.spriteBatch,
    string.Join('\n', lines),
    Game1.smallFont,
    overrideX: overrideX,
    overrideY: overrideY
);
```

**Characteristics:**
- Dark semi-transparent background
- White text in `Game1.smallFont`
- Automatic text wrapping
- Standard Stardew Valley look-and-feel
- Supports multi-line text via `\n`
- Optional position override (used for gamepad mode)

### 4.2 Custom SpriteBatch Tooltips

Used by: mods that need custom styling or positioning.

```csharp
spriteBatch.Draw(Game1.mouseCursors, position, sourceRect, Color.White * 0.7f);
```

**Characteristics:**
- Uses `Game1.mouseCursors` as texture atlas
- Manual positioning via `Game1.GlobalToLocal()`
- Alpha blending for transparency
- Requires manual viewport offset calculation

### 4.3 Positioning Patterns

| Context | Position Strategy |
|---------|-------------------|
| HUD icons (luck, weather) | Fixed screen position, offset from edges |
| Tile tooltips (crops, machines) | `Game1.GlobalToLocal(tile * Game1.tileSize)` |
| Gamepad mode | Tile position + 32px offset |
| Range overlays | Iterate tile collection, draw at each tile's pixel position |
| Legend (Data Layers) | Fixed top-left with configurable margins |

---

## 5. Configuration Patterns

### 5.1 Standard `config.json` Pattern

The standard SMAPI pattern uses `helper.ReadConfig<T>()` and `helper.WriteConfig()`:

From the [Stardew Valley Wiki — Modding:Modder Guide/APIs/Config](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Config):

```csharp
// In ModEntry.cs or feature constructor:
this.Config = helper.ReadConfig<ModConfig>();

// To save changes:
helper.WriteConfig(this.Config);
```

SMAPI automatically:
- Creates `config.json` next to the mod's DLL on first run
- Handles deserialization/validation
- Manages file I/O

### 5.2 Per-Save Settings

For per-save configuration, mods use one of these patterns:

**Pattern A: Save-specific data files**
```csharp
// Save data is stored in the save folder via helper.Data
helper.Data.SaveData(writer, "soil-health-data");
```

**Pattern B: `PerScreen<T>` for runtime state**
```csharp
private readonly PerScreen<SoilHealthState> _state = new(() => new());
```

**Pattern C: Custom JSON in save folder**
```csharp
string savePath = Path.Combine(Constants.SavePath, "LivingRoots");
helper.Data.WriteSaveData("per-save-config.json", config);
```

### 5.3 Generic Mod Config Menu (GMCM) Integration

Many mods integrate with [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) for in-game configuration:

From DataLayers:
```csharp
this.AddGenericModConfigMenu(
    new GenericModConfigMenuIntegrationForDataLayers(this.LayerRegistry, this.ColorRegistry),
    get: () => this.Config,
    set: config => this.Config = config,
    onSaved: this.ReapplyConfig
);
```

From CJB Item Spawner:
```csharp
// In ModEntry.cs:
helper.Events.GameLoop.GameLaunched += (s, e) =>
{
    var gmcm = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
    if (gmcm != null)
    {
        gmcm.Register(mod: manifest, reset: () => config = new(), save: () => helper.WriteConfig(config));
    }
};
```

### 5.4 Configuration Data Structure (from DataLayers)

```csharp
public class ModConfig
{
    public LayerConfig Controls { get; set; } = new();
    public Dictionary<string, LayerConfig> Layers { get; set; } = new();
    public string ColorScheme { get; set; } = "Default";
    public bool CombineOverlappingBorders { get; set; } = true;
    public bool ShowGrid { get; set; } = false;
    public float LegendAlphaOnHover { get; set; } = 0.5f;
    public int UpdatesPerSecond { get; set; } = 60;
}

public class LayerConfig
{
    public bool Enabled { get; set; }
    public KeybindList ShortcutKey { get; set; }
    public bool UpdateWhenViewChange { get; set; }
}
```

---

## 6. Console Command Patterns

### 6.1 Standard SMAPI Console Command Registration

From the [Stardew Valley Wiki — Modding:Console commands](https://stardewvalleywiki.com/Modding:Console_commands):

Mods can register custom console commands via `helper.ConsoleCommands.Add()`:

```csharp
helper.ConsoleCommands.Add("command_name", "Help text describing the command", (command, args) =>
{
    // Command logic here
});
```

### 6.2 Command Handler Pattern (from DataLayers)

DataLayers uses a more structured command handler pattern:

```csharp
// In ModEntry.cs:
var commandHandler = new CommandHandler(this.Monitor, () => this.CurrentOverlay.Value?.CurrentLayer);
commandHandler.RegisterWith(helper.ConsoleCommands);

// CommandHandler.cs:
internal class CommandHandler
{
    private readonly IMonitor Monitor;
    private readonly Func<ILayer?> GetCurrentLayer;

    public CommandHandler(IMonitor monitor, Func<ILayer?> getCurrentLayer)
    {
        this.Monitor = monitor;
        this.GetCurrentLayer = getCurrentLayer;
    }

    public void RegisterWith(IConsoleCommands consoleCommands)
    {
        consoleCommands.Add("datalayers", "Toggle or manage data layers.", this.HandleCommand);
        consoleCommands.Add("datalayers_list", "List available data layers.", this.HandleListCommand);
        // ... more commands
    }

    private void HandleCommand(string command, string[] args)
    {
        switch (args.FirstOrDefault())
        {
            case "toggle":
                // Toggle overlay
                break;
            case "next":
                // Switch to next layer
                break;
            // ... etc
        }
    }
}
```

### 6.3 Living Roots Console Command Pattern

Based on the project's AGENTS.md, console commands follow this pattern:

```csharp
// In ModController.RegisterConsoleCommand():
helper.ConsoleCommands.Add("livingroots", "Living Roots mod commands.", (cmd, args) =>
{
    // Use lock + flag pattern for thread safety
    lock (_consoleLock)
    {
        switch (args[0])
        {
            case "status":
                // Show soil health status
                break;
            case "reset":
                // Reset data
                break;
        }
    }
});
```

---

## 7. Accessibility in Stardew Mods

### 7.1 Colorblind Mod

There is an established [Colorblind Accessibility Options](http://forums.stardewvalley.net/threads/colorblind-accessibility-options.10687/) discussion and mod for Stardew Valley. The mod addresses:

- **Red/green colorblindness** — the most common form affecting Stardew Valley
- Crop maturity indicators (red vs green)
- Fishing bar color differences
- Various UI elements that rely solely on color

### 7.2 Data Layers Color Scheme System

Data Layers implements the most comprehensive accessibility approach:

1. **Multiple color schemes** — Default, Colorblind-friendly, Monochrome
2. **Legend component** — Shows what each color means (not just color-coded)
3. **Border rendering** — Tiles have borders, not just fill colors
4. **Configurable opacity** — Legend alpha on hover can be adjusted
5. **Grid overlay** — Optional tile grid for spatial reference

### 7.3 Accessibility Best Practices (from mods)

| Feature | Implementation |
|---------|---------------|
| Colorblind modes | Multiple color schemes with non-red/green palettes |
| Legend display | Text labels alongside colors |
| Borders/patterns | Not relying solely on color |
| Configurable opacity | Adjustable transparency for overlays |
| Toggle per feature | Enable/disable individual layers |
| Gamepad support | Navigation without precise mouse positioning |
| Tooltip text | Information available via hover, not just visual |

### 7.4 Niche Accessibility Mods

From [Modding Stardew Valley for accessibility](https://nicchan.me/blog/modding-stardew-valley-for-accessibility/):
- The SDV Colorblind Mod offers red/green alternatives
- Cognitive accessibility features (clear text labels, consistent UI)
- Motor accessibility (gamepad support, large click targets)

---

## 8. Recommendations for Living Roots

Based on this research, here are concrete recommendations for the Living Roots mod's visualization system:

### 8.1 Rendering Approach

**Recommended: `Events.Display.RenderingHud` with `e.SpriteBatch.Draw()`**

This is the standard pattern used by UI Info Suite 2 and most overlay mods. It:
- Renders after the world but before the HUD
- Provides access to `SpriteBatch` for custom drawing
- Is automatically called each frame
- Can be toggled per-feature

```csharp
// Pattern to follow:
helper.Events.Display.RenderingHud += OnRenderingHud;

private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
{
    if (!ShouldRender())
        return;
    
    foreach (var tile in GetTilesToHighlight())
    {
        Vector2 screenPos = Game1.GlobalToLocal(tile * Game1.tileSize);
        e.SpriteBatch.Draw(
            Game1.mouseCursors,  // or custom texture
            screenPos,
            sourceRect,
            Color.Lerp(Color.Green, Color.Red, healthValue) * 0.5f,  // alpha blending
            0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.01f
        );
    }
}
```

### 8.2 Colored Tile Overlays

**Multiple mods render colored tile overlays. The standard approach:**

1. **Use `CommonHelper.Pixel`** (1×1 white texture) for filled rectangles — this is what Data Layers uses
2. **Or use `Game1.mouseCursors`** with a specific source rectangle — what UI Info Suite 2 uses
3. **Apply alpha blending** — multiply color by 0.3-0.7f for transparency
4. **Use `Game1.GlobalToLocal()`** for coordinate transformation
5. **Account for viewport offset** — subtract `Game1.viewport.X/Y`

**Color coding pattern (from Data Layers):**
- Green = healthy soil
- Yellow/Orange = moderate
- Red = poor health
- Blue = recently amended
- Each color should be configurable

### 8.3 Tooltip Pattern

**Recommended: `IClickableMenu.drawHoverText()` for native style**

```csharp
List<string> lines = new();
lines.Add($"Soil Health: {healthValue}/100");
lines.Add($"Nitrogen: {nitrogen}");
lines.Add($"Phosphorus: {phosphorus}");
lines.Add($"Potassium: {potassium}");

IClickableMenu.drawHoverText(
    Game1.spriteBatch,
    string.Join('\n', lines),
    Game1.smallFont,
    overrideX: -1,
    overrideY: -1
);
```

### 8.4 Configuration Pattern

**Recommended: `helper.ReadConfig<T>()` + GMCM integration**

```csharp
public class ModConfig
{
    public bool ShowSoilHealthOverlay { get; set; } = true;
    public bool ShowCompostBinOverlay { get; set; } = true;
    public float OverlayOpacity { get; set; } = 0.5f;
    public string ColorScheme { get; set; } = "Default";
    public KeybindList ToggleOverlayKey { get; set; } = KeybindList.Parse("F7");
    public int UpdatesPerSecond { get; set; } = 30;
}

// In ModEntry:
this.Config = helper.ReadConfig<ModConfig>();
```

### 8.5 Performance Pattern

**Recommended: Throttle updates + PerScreen + Mutex**

```csharp
private readonly PerScreen<Dictionary<Vector2, SoilHealthState>> _cache = new(() => new());
private readonly object _lock = new();

private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
{
    if (!e.IsMultipleOf(4))  // Update every 4 ticks (15 times/sec)
        return;
    
    lock (_lock)
    {
        // Recalculate visible tiles
    }
}
```

### 8.6 Accessibility

**Recommended: Follow Data Layers' color scheme pattern**

1. Provide a "Default" and "Colorblind" color scheme
2. Use borders in addition to fill colors
3. Show text tooltips on hover (not just color)
4. Make overlay opacity configurable
5. Allow toggling individual overlay layers
6. Include a legend component explaining what colors mean

---

## 9. Sources

### Source Code Repositories

- **UI Info Suite 2 GitHub:** https://github.com/Annosz/UIInfoSuite2
  - `ShowItemEffectRanges.cs` — Range overlay rendering with colored tile highlights
  - `ShowCropAndBarrelTime.cs` — Crop/machine tooltips via `IClickableMenu.drawHoverText()`
  - `ExperienceBar.cs` — HUD icon rendering
  - `UIElements/` directory — All feature implementations
- **CJB Item Spawner GitHub:** https://github.com/CJBok/SDV-Mods (CJBItemSpawner folder)
  - `Framework/ItemMenu.cs` — Custom IClickableMenu with full UI rendering
  - `Framework/GenericModConfigMenuIntegrationForItemSpawner.cs` — GMCM integration
- **Data Layers GitHub:** https://github.com/Pathoschild/StardewMods (DataLayers folder)
  - `Framework/DataLayerOverlay.cs` — Layer rendering orchestrator with colored tile fills + borders
  - `Layers/BaseLayer.cs` — Abstract layer base
  - `Layers/AccessibleLayer.cs` — Accessibility overlay
  - `Framework/ColorScheme.cs` — Color scheme system
  - `Framework/LegendEntry.cs` — Legend UI component

### Nexus Mods Pages

- **UI Info Suite 2:** https://www.nexusmods.com/stardewvalley/mods/7098
- **CJB Item Spawner:** https://www.nexusmods.com/stardewvalley/mods/93
- **Data Layers:** https://www.nexusmods.com/stardewvalley/mods/1691
- **Generic Mod Config Menu:** https://www.nexusmods.com/stardewvalley/mods/5098

### Stardew Valley Wiki

- **Modding:Console commands:** https://stardewvalleywiki.com/Modding:Console_commands
- **Modding:Modder Guide/APIs/Config:** https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Config
- **Modding:Maps:** https://stardewvalleywiki.com/Modding:Maps

### Accessibility Resources

- **Colorblind Accessibility Options (Forum):** http://forums.stardewvalley.net/threads/colorblind-accessibility-options.10687/
- **Modding Stardew Valley for accessibility (Blog):** https://nicchan.me/blog/modding-stardew-valley-for-accessibility/
- **Reddit Colorblind Mode Discussion:** https://www.reddit.com/r/StardewValley/comments/1ble8gw/my_16_plea_colorblindness_mode_or_accessibility/

### Additional References

- **Pathoschild StardewMods (GitHub):** https://github.com/pathoschild/stardewmods
- **Stardew Valley Forums — UI Info Suite 2 Overlay Problems:** https://forums.stardewvalley.net/threads/ui-info-suite-2-overlay-problems.14045/
- **UI Info Suite 2 Alternative Fork:** https://www.nexusmods.com/stardewvalley/mods/43127

---

## Appendix A: Rendering Event Comparison

| Event | When Fired | Use Case | Mod Example |
|-------|-----------|----------|-------------|
| `RenderingHud` | Before HUD draw | Tile overlays, icons, tooltips | UI Info Suite 2 |
| `RenderedHud` | After HUD draw | Custom UI on top of everything | Rare |
| `RenderingWorld` | Before world draw | Background modifications | Rare |
| `RenderedWorld` | After world draw | Post-processing effects | Data Layers (DrawWorld) |
| `UpdateTicked` | ~60/sec | Data recalculation | All overlay mods |

## Appendix B: Texture Sources

| Source | Usage |
|--------|-------|
| `Game1.mouseCursors` | Built-in cursor/sprite atlas (UI Info Suite 2, CJB Item Spawner) |
| `CommonHelper.Pixel` | 1×1 white texture for filled rectangles (Data Layers) |
| Custom content assets | Mod-specific sprites via `helper.ModContent.Load<Texture2D>()` |
| `Game1.fadeToBlackRect` | Full-screen overlay texture (CJB Item Spawner) |

## Appendix C: Thread Safety Patterns

| Pattern | Usage | Mod Example |
|---------|-------|-------------|
| `PerScreen<T>` | Per-player/split-screen data | UI Info Suite 2 |
| `Mutex` | Cross-thread tile data protection | UI Info Suite 2 |
| `lock` statement | Console command safety | Living Roots (AGENTS.md) |
| `Interlocked` | Atomic flag operations | Living Roots (AGENTS.md) |
| Update throttling | `e.IsMultipleOf(N)` | UI Info Suite 2, Data Layers |

---

*Report compiled for the Living Roots mod design system evaluation.*
