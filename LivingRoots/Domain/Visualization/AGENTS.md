# Visualization Domain

Plain C# DTOs/models for soil health tile rendering — no SMAPI or XNA dependencies.

## Files

- **ColorDTO.cs** — Color data transfer object
- **ColorMapping.cs** — Maps health percentage ranges to display colors
- **HealthCategory.cs** — Enum: `Poor`, `Moderate`, `Healthy`, `Unknown`
- **PatternType.cs** — Enum: `None`, `Stripes`, `Dots`, `Solid`
- **TileOverlay.cs** — Tile position, color, and pattern type
- **TooltipData.cs** — Tooltip display model
- **HoeFeedback.cs** — Hoe feedback effect (flash + floating text)
- **VisualizationConfiguration.cs** — Toggles for overlays, tooltips, hoe feedback; opacity settings

## Health Categories

| Category | Range |
|----------|-------|
| Poor | 0–33% |
| Moderate | 34–66% |
| Healthy | 67–100% |
| Unknown | N/A |

## Colors (from ModConstants)

- Poor → Red
- Moderate → Yellow
- Healthy → Green
- Unknown → Gray

## Rules

- Minimum pattern opacity: **0.7** (accessibility)
- Default opacity: **0.5** (`ModConstants.DefaultOpacity`)
- Reference `ModConstants` for all color and opacity values — never hardcode
- Keep all types SMAPI/XNA-free
