# Data Model: Soil Health Visualization

**Feature**: Soil Health Visualization
**Date**: 2026-09-05

## Domain Entities

### VisualizationConfiguration

Stores user-configurable visualization settings. Persisted as JSON per-save.

| Field | Type | Default | Validation | Description |
|-------|------|---------|------------|-------------|
| OverlaysEnabled | bool | true | N/A | Master toggle for overlay rendering |
| TooltipsEnabled | bool | true | N/A | Master toggle for hover tooltips |
| HoeFeedbackEnabled | bool | true | N/A | Master toggle for hoe action feedback |
| Opacity | float | 0.5 | 0.0 - 1.0 | Overlay transparency level |
| PoorColor | ColorDTO | #FF0000 | Valid hex | Color for Poor category (0-33) |
| ModerateColor | ColorDTO | #FFFF00 | Valid hex | Color for Moderate category (34-66) |
| HealthyColor | ColorDTO | #00FF00 | Valid hex | Color for Healthy category (67-100) |
| UnknownColor | ColorDTO | #808080 | Valid hex | Color for unknown/unavailable data |
| ShowPatterns | bool | true | N/A | Enable pattern overlays for accessibility |

**State Transitions**: Configuration is immutable once loaded. Changes require save/reload cycle or hot-reload via configuration service.

### SoilHealthTile

Represents a tilled soil tile with its associated health value. The source data for visualization.

| Field | Type | Validation | Description |
|-------|------|------------|-------------|
| TilePosition | Point | Valid tile coordinates | Tile coordinates (X, Y) in game world |
| HealthValue | float | 0-100 (clamped) | Current soil health value |
| Category | HealthCategory | Derived from HealthValue | Resolved health category |
| IsTilled | bool | N/A | Whether the tile is currently tilled |

**Validation Rules**:
- HealthValue clamped to [0, 100] per ModConstants.MinSoilHealth/MaxSoilHealth
- NaN/Infinity values default to Unknown category with HealthValue = 0
- TilePosition must be within valid game map bounds

### ColorDTO

Serializable color representation for JSON persistence.

| Field | Type | Validation | Description |
|-------|------|------------|-------------|
| R | byte | 0-255 | Red channel |
| G | byte | 0-255 | Green channel |
| B | byte | 0-255 | Blue channel |
| A | byte | 0-255 | Alpha channel |

### HealthCategory

Enumeration of soil health categories with threshold boundaries.

| Value | Name | Range | Default Color | Pattern |
|-------|------|-------|---------------|---------|
| 0 | Poor | [0, 34) | Red (#FF0000) | Stripes |
| 1 | Moderate | [34, 67) | Yellow (#FFFF00) | Dots |
| 2 | Healthy | [67, 100] | Green (#00FF00) | Solid |
| 3 | Unknown | N/A | Gray (#808080) | None |

**Boundary Handling**: Half-open interval rules apply: 33 → Poor, 34 → Moderate, 66 → Moderate, 67 → Healthy. Interpolation occurs between adjacent category colors within each range, reaching the next category color at the upper boundary.

### ColorMapping

Represents the translation from a numeric health value to a rendered color.

| Field | Type | Description |
|-------|------|-------------|
| Category | HealthCategory | Resolved category for the health value |
| BaseColor | Color | Color at category boundary |
| InterpolatedColor | Color | Final color after interpolation |
| HealthValue | float | Original health value (0-100) |

**Validation Rules**:
- HealthValue clamped to [0, 100] per ModConstants.MinSoilHealth/MaxSoilHealth
- NaN/Infinity values default to Unknown category

### TileOverlay

Render data for a single tile overlay.

| Field | Type | Description |
|-------|------|-------------|
| TilePosition | Point | Tile coordinates (X, Y) |
| Color | Color | Computed overlay color with opacity |
| PatternType | PatternType | Accessibility pattern to render |
| HealthValue | float | Soil health value for this tile |
| Category | HealthCategory | Resolved health category |

### PatternType

Enumeration of accessibility patterns.

| Value | Name | Description |
|-------|------|-------------|
| 0 | None | No pattern (solid fill) |
| 1 | Stripes | Diagonal stripes for Poor category |
| 2 | Dots | Dot pattern for Moderate category |
| 3 | Solid | Solid fill for Healthy category |

### TooltipData

Data for hover tooltip rendering.

| Field | Type | Description |
|-------|------|-------------|
| Text | string | Formatted tooltip text |
| Position | Vector2 | Screen position for tooltip |
| BackgroundColor | Color | Background rectangle color |
| TextColor | Color | Text color |

**Format**: `"Soil Health: {percentage}% ({category})"` where percentage is rounded to nearest integer, category is localized category name.

### HoeFeedback

Transient state for hoe action visual feedback.

| Field | Type | Description |
|-------|------|-------------|
| TilePosition | Point | Targeted tile coordinates |
| StartTime | DateTime | When feedback was triggered |
| FlashDuration | TimeSpan | 300ms flash effect duration |
| TextDuration | TimeSpan | 1000ms floating text duration |
| HealthValue | float | Health value at time of action |
| Category | HealthCategory | Category at time of action |
| HealthText | string | Formatted text for floating display |

**State Transitions**:
- Created: When hoe action detected on tilled tile
- Active: While elapsed time < max(FlashDuration, TextDuration)
- Expired: When elapsed time exceeds both durations

## Relationships

```
VisualizationConfiguration (1) ---> (many) TileOverlay
    |
    |-- Opacity applied to all overlays
    |-- Colors define category base colors
    |-- Feature toggles enable/disable rendering

SoilHealthService (1) ---> (many) TileOverlay
    |
    |-- Provides health values for tiles
    |-- Cache invalidation triggers overlay recalculation

TileOverlay (1) ---> (0..1) TooltipData
    |
    |-- Tooltip generated only when cursor hovers tile

HoeFeedback (0..1 per action) ---> (1) TileOverlay
    |
    |-- References tile being targeted
    |-- Transient, expires after duration
```

## Persistence

### Configuration Storage
- **Key**: `"visualization_config"` (via IModDataService)
- **Format**: JSON serialization of VisualizationConfiguration
- **Location**: Per-save SMAPI data storage
- **Lifecycle**: Loaded on SaveLoaded event, saved on Saving event

### Validation on Load
1. Deserialize JSON to VisualizationConfiguration
2. For each field, validate against constraints
3. Invalid fields replaced with defaults (per FR-006)
4. Missing file generates complete defaults (per FR-006)

## Cache Strategy

### Health Value Cache
- **Source**: SoilHealthService runtime cache
- **Invalidation**: On SaveLoaded, tile modification events
- **Key**: Location name + tile coordinates

### Color Computation Cache
- **Key**: Health value (rounded to reduce variations)
- **Invalidation**: On configuration change (colors/opacity)
- **Value**: Computed Color with interpolation applied

### Overlay List Cache
- **Key**: Viewport bounds
- **Invalidation**: On viewport change, health data change
- **Value**: List of TileOverlay for visible tiles
