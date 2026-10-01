# Research: SMAPI Rendering Events for Tooltips & Floating Text

**Date:** 2026-09-12
**Feature:** Soil Health Visualization (001-soil-health-visualization)
**Scope:** Determine whether tooltips and floating text should render in `Display.RenderedWorld` or `Display.Rendered`

---

## TL;DR — Recommendation

**Tooltips and floating text MUST be rendered in `Display.Rendered`** (above all HUD elements), NOT in `Display.RenderedWorld`.

This is consistent with:
- The existing spec clarification (Session 2026-09-11, `spec.md` line 101)
- FR-009's degradation HUD indicator already using `Display.Rendered`
- SMAPI documentation: `Display.Rendered` draws over all vanilla content including HUD [^1]
- The principle that interactive UI elements must always be readable and not obscured

Tile overlays remain in `Display.RenderedWorld` per FR-023 — they are decorative world-layer effects, not interactive UI.

---

## 1. Current Codebase State

### 1.1 Event Subscription

In `ModController.cs`, visualization rendering is currently subscribed **only** to `Display.RenderedWorld`:

**`LivingRoots/Controllers/ModController.cs:134-143`**
```csharp
if (_visualizationService != null)
{
    localRenderedWorldHandler = _onRenderedWorldHandler ??= OnRenderedWorld;
    localButtonReleasedHandler = _onButtonReleasedHandler ??= OnButtonReleased;
    _helper.Events.Display.RenderedWorld += localRenderedWorldHandler;  // <-- Only this event
    _helper.Events.Input.ButtonReleased += localButtonReleasedHandler;
    _onUpdateTickedHandler = OnUpdateTicked;
    gameLoop.UpdateTicked += _onUpdateTickedHandler;
}
```

There is **no subscription** to `Display.Rendered` for visualization purposes.

### 1.2 Current Rendering (All in RenderedWorld)

The `OnRenderedWorld` handler at `ModController.cs:771-794` currently renders ALL visualization layers in a single event:

```csharp
private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
{
    if (IsDisposed() || _visualizationService == null) return;
    try
    {
        // ... texture init ...
        _visualizationService.RenderOverlays(e.SpriteBatch, viewport, gameTime);    // overlays
        _visualizationService.RenderTooltip(e.SpriteBatch, new Vector2(0, 0), gameTime);  // tooltips
        _visualizationService.RenderHoeFeedback(e.SpriteBatch, gameTime);            // hoe feedback
    }
    catch (Exception ex) { /* ... */ }
}
```

**Problem:** Tooltips and floating text rendered here appear **under the HUD**, making them potentially obscured by the toolbar, clock, money display, etc.

### 1.3 Existing Spec Decisions

The spec already explicitly answered this question:

**`specs/001-soil-health-visualization/spec.md:101`** (Session 2026-09-11):
> **Q:** In which SMAPI rendering event should tooltips and floating text be rendered — `Display.RenderedWorld` (above characters, below HUD) or `Display.Rendered` (above all HUD elements)?
>
> **A:** Render both tooltips and floating text in `Display.Rendered` (above HUD). Tooltips and floating text are interactive UI elements that must always be visible and readable, and must not be obscured by HUD elements (toolbar, clock, money display). This is consistent with the degradation HUD indicator which already uses `Display.Rendered` per FR-009. Overlays remain in `Display.RenderedWorld` per FR-023 since they are decorative world-layer effects, not interactive UI.

---

## 2. SMAPI Rendering Pipeline

### 2.1 Draw Order

From the Stardew Valley Wiki [^1] and `research-rendering-layers.md` [^2]:

```
1. Display.Rendering           ← BEFORE anything is drawn
2.  [Game draws world layers]
3. Display.RenderedWorld        ← AFTER world drawn, before screen render
4. Display.RenderingActiveMenu  ← BEFORE menu (if open)
5. Display.RenderedActiveMenu   ← AFTER menu
6. Display.RenderingHud         ← BEFORE HUD
7. Display.RenderedHud          ← AFTER HUD
8. Display.Rendered             ← AFTER everything, before screen render
```

### 2.2 Key Distinctions

| Event | Draws Over | Draws Under | Best For |
|-------|-----------|-------------|----------|
| `Display.RenderedWorld` | World (incl. characters) | Menus, HUD, cursor | World overlays above characters |
| `Display.Rendered` | Everything (incl. HUD, cursor, menus) | Nothing | ✅ Tooltips, cursor overlays, HUD indicators |
| `Display.RenderedHud` | Everything up to HUD | Cursor | Above-HUD indicators below cursor |

### 2.3 From SMAPI Documentation [^1]

> **Display.Rendered:** Raised after the game draws to the sprite batch in a draw tick, just before the final sprite batch is rendered to the screen. Content drawn to the sprite batch at this point will be drawn over all vanilla content (including menus, HUD, and cursor).

> **Display.RenderedWorld:** Raised after the game world is drawn to the sprite batch, before it's rendered to the screen. Content drawn to the sprite batch at this point will be drawn over the world, but under any active menu, HUD elements, or cursor.

---

## 3. How Other Mods Handle This

### 3.1 Data Layers (Pathoschild) [^3]

- **Tile overlays:** Rendered in `Display.RenderedWorld` with `SpriteSortMode.Deferred`, alpha ~0.3
- **Pattern:** `spriteBatch.Draw(CommonHelper.Pixel, rectangle, color * 0.3f)` — draws above world and characters
- **Legend/UI:** Drawn separately in a UI layer above the world overlay
- **Key insight:** World overlays and UI elements are rendered in **separate events**

### 3.2 UI Info Suite 2 [^4]

- **World-based indicators** (scarecrow coverage, sprinkler range): Rendered in `Display.RenderedWorld`
- **UI elements** (hover tooltips, item information): Rendered in later display events
- **Pattern:** Same separation — world overlays in `RenderedWorld`, UI in `Rendered` or later

### 3.3 Community Convention

The consistent pattern across tile-highlighting mods is:
1. **World overlays** (tile highlights, coverage areas) → `Display.RenderedWorld`
2. **UI elements** (tooltips, labels, HUD indicators) → `Display.Rendered` or `Display.RenderedHud`

---

## 4. Spec Requirements Analysis

### 4.1 FR-009: Degradation HUD Indicator

> "persistent HUD indicator (small icon with tooltip) rendered via `Display.Rendered` (above all vanilla content including HUD)"

**Already mandates `Display.Rendered`** for a UI element that must always be visible.

### 4.2 FR-023: Tile Overlays

> "System MUST render tile overlays in `Display.RenderedWorld` using `SpriteBatch.Draw` in Deferred mode with alpha transparency (~0.3)."

**Mandates `Display.RenderedWorld`** for decorative world overlays.

### 4.3 Tooltip Requirements

> "Tooltips should always be readable and not obscured by HUD"

This requirement is **impossible to satisfy** in `Display.RenderedWorld` because the HUD is drawn after this event.

### 4.4 Floating Text Requirements

> "Floating text is transient feedback from hoe actions"

Floating text is UI feedback, not a world-layer effect. It must be visible above all game elements to serve its purpose as player feedback.

---

## 5. Recommended Approach

### 5.1 Split Rendering Across Two Events

| Visualization | SMAPI Event | Rationale |
|--------------|-------------|-----------|
| Tile overlays | `Display.RenderedWorld` | Decorative world-layer effect (FR-023) |
| Hover tooltips | `Display.Rendered` | Interactive UI, must not be obscured |
| Hoe feedback floating text | `Display.Rendered` | Transient UI feedback, must be readable |
| Degradation HUD indicator | `Display.Rendered` | Already mandated by FR-009 |

### 5.2 Implementation Plan

1. **Add `Display.Rendered` subscription** in `ModController.RegisterEvents()`
2. **Split `OnRenderedWorld`** into two handlers:
   - `OnRenderedWorld` — calls only `_visualizationService.RenderOverlays()`
   - `OnRendered` — calls `_visualizationService.RenderTooltip()` and `_visualizationService.RenderHoeFeedback()`
3. **Update rollback/unsubscribe** logic to handle the new event handler
4. **Guard `OnRendered`** with same disposal checks as `OnRenderedWorld`

### 5.3 Consistency Argument

Rendering tooltips and floating text in `Display.Rendered` creates a consistent pattern:
- The degradation HUD indicator already uses `Display.Rendered` (FR-009)
- All UI-layer visualization goes in `Display.Rendered`
- All world-layer visualization goes in `Display.RenderedWorld`
- This matches the convention used by Data Layers and UI Info Suite 2

### 5.4 Code Structure

```csharp
// In RegisterEvents():
helper.Events.Display.RenderedWorld += OnRenderedWorld;  // overlays
helper.Events.Display.Rendered += OnRendered;            // tooltips + hoe feedback

// Tile overlays (world layer)
private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
{
    if (IsDisposed() || _visualizationService == null) return;
    _visualizationService.RenderOverlays(e.SpriteBatch, viewport, gameTime);
}

// UI layer (tooltips + floating text)
private void OnRendered(object? sender, RenderedEventArgs e)
{
    if (IsDisposed() || _visualizationService == null) return;
    _visualizationService.RenderTooltip(e.SpriteBatch, cursorPosition, gameTime);
    _visualizationService.RenderHoeFeedback(e.SpriteBatch, gameTime);
}
```

---

## 6. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Tooltip renders over game menus | Visual clutter | Already handled by pause-on-menu (FR-022) — rendering is paused when menus are open |
| Floating text appears above cursor | Minor cosmetic | Floating text appears at tile position, not cursor position; acceptable |
| Extra event subscription overhead | Negligible | `Display.Rendered` fires once per frame anyway; just routing calls differently |
| Split across two events complicates state coordination | Low | `GameTime` is captured in `UpdateTicked` (already at `ModController.cs:796-799`); both events use same `_lastGameTime` |

---

## 7. Conclusion

**Render tooltips and floating text in `Display.Rendered`** (above HUD). This is:

1. ✅ Already mandated by spec clarification (Session 2026-09-11)
2. ✅ Consistent with FR-009's degradation HUD indicator
3. ✅ Required to satisfy "tooltips always readable, not obscured by HUD"
4. ✅ Consistent with community conventions (Data Layers, UI Info Suite 2)
5. ✅ Technically straightforward — split existing `OnRenderedWorld` into two handlers

The current codebase renders everything in `Display.RenderedWorld`, which means tooltips and floating text appear **under the HUD**. This must be corrected by adding a `Display.Rendered` subscription and moving tooltip/floating text rendering there.

---

## Sources

[^1]: [Modding:Modder Guide/APIs/Events - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events) — Official SMAPI documentation for Display events

[^2]: `specs/001-soil-health-visualization/checklists/research-rendering-layers.md` — Existing research document on rendering pipeline (lines 99-133 cover event comparison)

[^3]: [Data Layers - Pathoschild/StardewMods](https://github.com/Pathoschild/StardewMods/blob/develop/DataLayers/Framework/DataLayerOverlay.cs) — Reference implementation of tile overlay rendering in `Display.RenderedWorld`

[^4]: [UIInfoSuite2 - GitHub](https://github.com/Annosz/UIInfoSuite2) — Reference mod for world overlay rendering patterns
