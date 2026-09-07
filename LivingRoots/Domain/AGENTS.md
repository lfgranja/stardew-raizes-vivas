# DOMAIN LAYER

Pure business logic — no SMAPI dependencies. Game types (`StardewValley.*`, `Microsoft.Xna.Framework.*`) are permitted; SMAPI using statements are forbidden.

## Structure

| Folder | Contents |
|--------|----------|
| `Interfaces/` | Service contracts — `ICompostingBinService`, `ICompostApplicationService`, `ISoilDecayService`, `IOrganicWasteValidator`, `IVisualizationService` |
| `Models/` | Persistence DTOs — `SoilHealthState`, `CompostingBinData`, `CompostingBinStateModel` |
| `Services/` | Domain services (stateless business rules) — `OrganicWasteValidator`, `SeasonalDecayMultiplier` |
| `Visualization/` | Render DTOs — `ColorDTO`, `ColorMapping`, `HealthCategory`, `TileOverlay`, `TooltipData`, `HoeFeedback`, `VisualizationConfiguration` |

## Interface Roles

- `ICompostingBinService` — State machine for bins (Empty→Processing→Ready); handles waste input, collection, maturation, day-start transitions
- `ICompostApplicationService` — Validates tile state and applies compost to tilled soil
- `ISoilDecayService` — Daily decay for bare tilled tiles; exempts Greenhouse, respects dead-crop mulch
- `IOrganicWasteValidator` — Hybrid: inclusion tag `compostable_item` OR vanilla category IDs (-74, -75, -79, -80, -81); exclusion tag `not_compostable` overrides all

## Domain vs Application Services

- **Domain services** (in `Domain/Services/`): Pure business rules — validation, seasonal multipliers, computations. No side effects.
- **Application services** (in `LivingRoots/Services/`): Implement Domain interfaces. Handle SMAPI, persistence, game event orchestration.

## Key Values

- `CompostingBinState`: `Empty=0`, `Processing=1`, `Ready=2`
- `HealthCategory` half-open intervals: Poor [0,34), Moderate [34,67), Healthy [67,100]
- `SeasonalDecayMultiplier`: Spring 0.5x, Summer 1.5x, Fall 0.5x, Winter 0.0x

## Anti-Patterns

- DO NOT add SMAPI using statements — this layer is framework-agnostic
- DO NOT implement persistence logic here — defer to LivingRoots/Services/
- DO NOT hardcode game values — reference ModConstants
