# Phase 0 Research: Soil Health Visualization

**Feature**: Soil Health Visualization
**Date**: 2026-09-05

## Research Questions & Findings

### 1. MonoGame SpriteBatch Rendering for Tile Overlays

**Question**: How to efficiently render color-coded overlays on tiles using SpriteBatch?

**Decision**: Use SpriteBatch with a 1x1 white texture tinted via Color property for overlay rectangles.

**Rationale**:
- SpriteBatch is the standard MonoGame rendering approach and is already available in the Stardew Valley modding context
- A 1x1 white texture can be tinted to any color and scaled to tile dimensions, minimizing memory usage
- SpriteBatch.Begin() with SpriteSortMode.Deferred batches draw calls efficiently
- Alpha blending via Color.A channel supports configurable opacity

**Alternatives Considered**:
- Direct texture manipulation: More complex, requires per-pixel operations
- Custom shader: Overkill for simple color overlays, adds shader compilation complexity

---

### 2. Color Interpolation in RGB Space

**Question**: How to implement linear RGB interpolation between category colors?

**Decision**: Use Microsoft.Xna.Framework.Color with Lerp() for linear interpolation between adjacent category colors.

**Rationale**:
- Color.Lerp() performs linear interpolation on R, G, B, A channels independently
- Spec requires linear RGB interpolation between adjacent category colors
- Interpolation formula: `result = colorA + (colorB - colorA) * t` where t is normalized position within range
- For Poor (0-33) to Moderate (34-66): interpolate between red and yellow based on position in 0-66 range
- For Moderate (34-66) to Healthy (67-100): interpolate between yellow and green based on position in 34-100 range

**Alternatives Considered**:
- HSL interpolation: Spec explicitly requires RGB space
- Custom interpolation math: Redundant when Color.Lerp exists

---

### 3. Accessibility Patterns for Colorblind Players

**Question**: How to provide pattern overlays for colorblind accessibility?

**Question**: How to provide pattern overlays for colorblind accessibility?

**Decision**: Use distinct pattern textures (stripes, dots, solid) combined with colors for dual-coding.

**Rationale**:
- Spec defines: stripes for poor, dots for moderate, solid for healthy
- Patterns provide redundant encoding beyond color alone
- Simple 8x8 or 16x16 tileable textures can be created programmatically
- Patterns overlay on top of color with higher opacity to remain visible
- Implementation: separate SpriteBatch draw call with pattern texture per visible tile

**Alternatives Considered**:
- Text overlays: Too cluttered with many tiles
- Shape borders: Less distinguishable at small tile sizes

---

### 4. SMAPI Events for Rendering

**Question**: Which SMAPI events should trigger overlay rendering and tooltip display?

**Decision**: Subscribe to GameLoop.RenderedActiveMenu (for menu overlays) and GameLoop.RenderedHud (for in-game rendering), Input.ButtonPressed/Released (for hoe detection), CursorMoved (for tooltips).

**Rationale**:
- RenderedActiveMenu fires after all menu rendering completes, allowing overlay drawing on top
- RenderedHud fires after HUD rendering, appropriate for in-game tile overlays
- ButtonPressed/Released events detect hoe usage (tool type Hoe with action button)
- Input.CursorMoved provides cursor position for tile hover detection
- These events align with existing ModController event registration pattern

**Alternatives Considered**:
- UpdateTick for rendering: Wrong phase, rendering must happen after game draws
- Direct game loop hooks: Bypasses SMAPI abstraction, violates project conventions

---

### 5. Viewport Culling for Performance

**Question**: How to implement viewport culling to only render visible tiles?

**Decision**: Calculate visible tile range from viewport bounds and only process tiles within range.

**Rationale**:
- Stardew Valley viewport provides current screen bounds in tile coordinates
- Calculate min/max visible tiles: `viewport.X - margin` to `viewport.X + viewport.Width + margin`
- Filter soil health cache entries to only visible range before rendering
- Reduces rendering from potentially thousands of tiles to only visible subset
- Critical for maintaining 60 FPS with 1,000+ tilled tiles

**Alternatives Considered**:
- Frustum culling: Overkill for 2D tile grid
- Quadtree: Adds complexity, viewport culling sufficient for tile grid

---

### 6. Tooltip Rendering Approach

**Question**: How to render hover tooltips showing soil health percentage and category?

**Decision**: Use SpriteBatch.DrawString with SpriteFont for text rendering, positioned at cursor offset.

**Rationale**:
- SpriteBatch.DrawString is the standard MonoGame text rendering approach
- Format: "Soil Health: {percentage}% ({category})"
- Position: cursor position + small offset (e.g., 16, 16) to avoid cursor overlap
- Background rectangle behind text for readability
- Only render when cursor is over a tilled soil tile with valid data

**Alternatives Considered**:
- Custom UI framework: Overkill for simple tooltip
- Game dialog system: Too heavy for hover tooltip

---

### 7. Hoe Action Feedback Implementation

**Question**: How to implement flash effect and floating text feedback on hoe usage?

**Decision**: Track feedback state with timestamps, render flash as full-tile overlay with decreasing alpha, render floating text above tile with vertical offset.

**Rationale**:
- Flash effect: 300ms duration, render as colored overlay with alpha decreasing from 1.0 to 0.0
- Floating text: 1000ms duration, render text 32px above tile center, alpha decreasing
- Store feedback state: target tile, start time, health value at time of action
- Check elapsed time each frame, remove expired feedback
- Only render feedback for targeted tile (verify cursor position matches tile)

**Alternatives Considered**:
- Particle system: Overkill for simple flash
- Animation clips: More complex, no benefit for simple fade

---

### 8. Configuration Persistence Strategy

**Question**: How to persist visualization configuration using existing IModDataService?

**Decision**: Extend IModDataService pattern with a separate configuration key for visualization settings.

**Rationale**:
- IModDataService already provides SaveData<T> and LoadData<T> methods
- Use key "visualization_config" for configuration storage
- Configuration is per-save (loaded on SaveLoaded, saved on Saving)
- Invalid values fall back to defaults per FR-006
- Missing file generates complete defaults per FR-006

**Alternatives Considered**:
- Separate JSON file: Violates project pattern of using SMAPI data service
- Global config: Spec implies per-save configuration

---

### 9. Frame Rate Optimization for 1,000 Tiles

**Question**: How to maintain 60 FPS with up to 1,000 visible tiles?

**Decision**: Implement viewport culling, batch SpriteBatch calls, cache computed colors, and avoid per-frame allocations.

**Rationale**:
- Viewport culling reduces visible tiles to typically 50-200 (screen-sized area)
- Single SpriteBatch.Begin/End pair for all overlays minimizes draw calls
- Cache health-to-color mappings, invalidate only on data changes
- Pre-allocate 1x1 texture once, reuse for all overlay tiles
- Avoid LINQ and allocations in render loop
- Target: <16.67ms total frame time for visualization system

**Alternatives Considered**:
- Parallel rendering: SpriteBatch is not thread-safe
- GPU instancing: Not available in MonoGame standard profile

---

## Research Summary

All technical unknowns resolved. Key decisions:
1. SpriteBatch with tinted 1x1 texture for overlays
2. Color.Lerp for RGB interpolation
3. Pattern textures for accessibility
4. RenderedHud event for rendering, ButtonPressed for hoe detection
5. Viewport culling from viewport bounds
6. DrawString for tooltips
7. Timestamp-based feedback state for hoe effects
8. IModDataService for config persistence
9. Batching and caching for performance

No NEEDS CLARIFICATION items remain.
