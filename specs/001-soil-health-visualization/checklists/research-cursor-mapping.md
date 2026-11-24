# Research: SMAPI Cursor-to-Tile Mapping API

**Date:** 2026-03-18
**Spec:** FR-002 — Cursor-to-tile mapping for soil health visualization
**Researcher:** Subagent (LongCat-2.0)

## Problem Statement

The spec contains an inconsistency:
- **FR-002** says: uses "SMAPI's `ICursorPosition.Tile` property (from `Helper.Input.GetCursorPosition().Tile`)"
- **Data model** says: "Cursor-to-tile mapping converts screen pixel coordinates to tile position (X, Y) using Stardew Valley's tile coordinate system."

These describe two different approaches — one using a direct tile property, the other describing a manual pixel-to-tile conversion. This research resolves which is correct.

---

## Findings

### 1. The Correct SMAPI API: `ICursorPosition`

The canonical SMAPI API for cursor position is **`this.Helper.Input.GetCursorPosition()`**, which returns an `ICursorPosition` object. This is the recommended approach per the official Stardew Valley Wiki modding guide. [\[1\]][wiki-input]

The `ICursorPosition` interface provides **four coordinate systems** [\[1\]][wiki-input]:

| Property | Type | Description |
|----------|------|-------------|
| `AbsolutePixels` | `Vector2` | Pixel position relative to the top-left corner of the in-game map, adjusted for zoom but **not** UI scaling |
| `ScreenPixels` | `Vector2` | Pixel position relative to the top-left corner of the visible screen, adjusted for zoom but **not** UI scaling |
| `Tile` | `Vector2` | The tile position under the cursor |
| `GrabTile` | `Vector2` | The tile position the game considers under the cursor for clicking actions; accounts for controller mode; may differ from `Tile` if the cursor is too far from the player |

### 2. Does the API Return Tile Coordinates Directly?

**Yes, `ICursorPosition.Tile` returns tile coordinates directly.** No manual conversion is needed.

The `Tile` property is a `Vector2` where `X` and `Y` are tile coordinates (not pixel coordinates). This is confirmed by the official wiki [\[1\]][wiki-input]:

> *"Tile is the tile position under the cursor."*

And by real-world mod code (BusLocations mod by comradesean) [\[2\]][buslocations]:

```csharp
private bool IsPlayerAtTicketMachine(Vector2 cursorTile)
{
    // Must be at the bus stop location
    if (!Game1.currentLocation.Name.Contains("BusStop"))
        return false;

    // Check if cursor is on the ticket machine's tile column
    bool isCorrectX = cursorTile.X == TicketMachineTileX;
    bool isCorrectY = cursorTile.Y >= TicketMachineTileYTop && cursorTile.Y <= TicketMachineTileYBottom;

    return isCorrectX && isCorrectY;
}
```

The mod receives tile coordinates directly from `e.Cursor.GrabTile` and compares them against tile constants — no pixel math required.

### 3. `Tile` vs `GrabTile` — Which to Use?

The difference is critical for gameplay correctness [\[1\]][wiki-input]:

- **`Tile`** — The geometric tile under the cursor. This is what you want for **visualization** (e.g., highlighting a tile, showing a tooltip, drawing an overlay).
- **`GrabTile`** — The tile the game actually uses for click actions. In controller mode, this accounts for the player's grab range and may snap to a tile near the player if the cursor is too far away. This is what you want for **interaction handling** (e.g., determining what tile a tool affects).

**Recommendation for FR-002 (soil health visualization):** Use `Tile` for rendering overlays and tooltips, since the goal is to show information about the tile the player is pointing at — not to replicate the game's click behavior.

### 4. `Game1.currentCursorTile` — An Alternative, Not Recommended

`Game1.currentCursorTile` is a Stardew Valley internal field (not a SMAPI API). It returns a `Vector2` of the tile under the cursor. It is used in some mods (e.g., BusLocations uses it as a fallback [\[2\]][buslocations]):

```csharp
private bool IsPlayerAtTicketMachine()
{
    return this.IsPlayerAtTicketMachine(Game1.currentCursorTile);
}
```

However, this approach has drawbacks:
- It's a game internal, not part of the SMAPI API contract
- It doesn't provide pixel coordinates or the `GrabTile` distinction
- It may not be updated in all contexts where SMAPI's `ICursorPosition` is available
- SMAPI's API is the officially documented and supported approach

### 5. Pixel-to-Tile Conversion (When Needed)

If you ever need to convert pixel coordinates to tile coordinates manually (e.g., for custom rendering calculations), the formulas from the Stardew Valley Wiki Game Fundamentals page [\[3\]][game-fundamentals] are:

```
screen→absolute:  x + Game1.viewport.X, y + Game1.viewport.Y
absolute→tile:    x / Game1.tileSize, y / Game1.tileSize
screen→tile:      (x + Game1.viewport.X) / Game1.tileSize, (y + Game1.viewport.Y) / Game1.tileSize
tile→absolute:    x * Game1.tileSize, y * Game1.tileSize
tile→screen:      (x * Game1.tileSize) - Game1.viewport.X, (y * Game1.tileSize) - Game1.viewport.Y
absolute→screen:  x - Game1.viewport.X, y - Game1.viewport.Y
```

Where `Game1.tileSize` is 64 (pixels per tile at 1x zoom) [\[3\]][game-fundamentals].

**Important:** The pixel positions from `ICursorPosition.AbsolutePixels` and `ICursorPosition.ScreenPixels` are **not adjusted for UI scaling**. If you need UI-scaled pixel coordinates, use:
- `cursorPos.GetScaledAbsolutePixels()` or
- `cursorPos.GetScaledScreenPixels()` or
- `Utility.ModifyCoordinatesForUIScale()` [\[1\]][wiki-input]

### 6. Accessing Cursor Position in Events

Cursor position is available in multiple event args [\[4\]][events]:

```csharp
// In ButtonPressed/ButtonReleased events:
helper.Events.Input.ButtonPressed += (sender, e) =>
{
    ICursorPosition cursor = e.Cursor;
    Vector2 tile = cursor.Tile;
    Vector2 grabTile = cursor.GrabTile;
};

// In ButtonsChanged event:
helper.Events.Input.ButtonsChanged += (sender, e) =>
{
    ICursorPosition cursor = e.Cursor;
};

// In CursorMoved event:
helper.Events.Input.CursorMoved += (sender, e) =>
{
    ICursorPosition oldPos = e.OldPosition;
    ICursorPosition newPos = e.NewPosition;
};

// Polling at any time:
ICursorPosition cursorPos = helper.Input.GetCursorPosition();
```

---

## Resolution of the Spec Inconsistency

| Statement | Verdict |
|-----------|---------|
| FR-002: "SMAPI's `ICursorPosition.Tile` property (from `Helper.Input.GetCursorPosition().Tile`)" | **Correct.** This is the canonical SMAPI API. It returns tile coordinates directly. |
| Data model: "converts screen pixel coordinates to tile position (X, Y)" | **Misleading/inaccurate.** While the underlying implementation likely does pixel-to-tile math, the `Tile` property returns tile coordinates directly. The data model description implies manual conversion is needed, which is false. |

### Recommended Spec Correction

The data model should be updated to:

> "Cursor-to-tile mapping uses SMAPI's `ICursorPosition.Tile` property (from `Helper.Input.GetCursorPosition().Tile`), which directly returns the tile coordinates (X, Y) under the cursor in Stardew Valley's tile coordinate system."

If pixel coordinates are needed for custom rendering, use `ICursorPosition.ScreenPixels` or `ICursorPosition.AbsolutePixels` (with optional UI scaling via `GetScaledScreenPixels()` / `GetScaledAbsolutePixels()`).

---

## Summary

| Question | Answer |
|----------|--------|
| What is the correct SMAPI API? | `this.Helper.Input.GetCursorPosition().Tile` (returns `Vector2` of tile coords) |
| Does it return tile coords directly? | **Yes.** `Tile` returns tile coordinates; `AbsolutePixels`/`ScreenPixels` return pixel coords |
| Recommended approach? | Use `ICursorPosition.Tile` for visualization; use `GrabTile` for interaction handling |
| Is `Game1.currentCursorTile` acceptable? | It works but is a game internal; prefer SMAPI's `ICursorPosition` API |
| Manual pixel-to-tile conversion needed? | **No**, unless doing custom rendering math — SMAPI handles it |

---

## Sources

1. [Modding:Modder Guide/APIs/Input — Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input) — Official documentation for `ICursorPosition`, `GetCursorPosition()`, coordinate systems, and UI scaling notes.
2. [BusLocations/ModEntry.cs — comradesean (GitHub)](https://github.com/comradesean/BusLocations/blob/master/ModEntry.cs) — Real-world mod using `e.Cursor.GrabTile` and `Game1.currentCursorTile` for tile-based interaction.
3. [Modding:Modder Guide/Game Fundamentals — Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals) — Coordinate system formulas (tile/absolute/screen), tile size, viewport, zoom, and UI scaling.
4. [Modding:Modder Guide/APIs/Events — Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events) — Input events (`ButtonPressed`, `ButtonsChanged`, `CursorMoved`) and their `Cursor` property.

[wiki-input]: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input
[buslocations]: https://github.com/comradesean/BusLocations/blob/master/ModEntry.cs
[game-fundamentals]: https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals
[events]: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events
