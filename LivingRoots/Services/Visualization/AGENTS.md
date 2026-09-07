# Visualization Services

XNA rendering services for soil health overlays, tooltips, and hoe feedback.

## Files

| File | Role |
|------|------|
| `VisualizationService` | Orchestrates all rendering. Lock + snapshot pattern for thread safety. |
| `OverlayRenderer` | Computes visible overlays with viewport culling + cache. |
| `HoeFeedbackRenderer` | Flash (300ms) + floating text (1000ms) on hoe use. |
| `TooltipRenderer` | Cursor-following tooltip with 50ms debounce (FR-015). |
| `ColorInterpolationService` | Health→color interpolation (Poor/Moderate/Healthy/Unknown). |
| `VisualizationConfigurationService` | Per-save config load/save via `IModDataService`. |
| `VisualizationTextures` | White texture singleton + `SpriteFont` for rectangle drawing. |

## Constants

| Value | Source |
|-------|--------|
| Tile size | `VisualizationService.TileSize` = 64 |
| Pattern opacity min | `ModConstants.PatternMinOpacity` = 0.7 (accessibility) |
| Tooltip debounce | `ModConstants.TooltipDebounceMs` = 50 |
| Flash duration | 300ms |
| Floating text duration | 1000ms |
| Frame budget | 16.67ms (single frame) |
| Graceful degradation | >1000 visible tiles → disable patterns |

## Rendering Rules

- All drawing through `SpriteBatch` — no direct GL calls.
- Hold lock for snapshot only, never during `spriteBatch.Draw`.
- Skip all rendering when `_isPaused` is true (save/load).
- Pattern opacity clamped to ≥0.7 per FR-001; never below.
- Color constants live in `ModConstants` — `PoorColor`, `ModerateColor`, `HealthyColor`, `UnknownColor`. Hardcoding colors is forbidden.
- Cache overlay list; invalidate via `InvalidateCache()` on data change.
