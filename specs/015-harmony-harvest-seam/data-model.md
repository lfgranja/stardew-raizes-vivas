# Phase 1 Design: Data Model — Harmony Patch for Crop.harvest (HarvestQualityService Seam)

**Feature**: `015-harmony-harvest-seam` | **Branch**: `015-harmony-harvest-seam`

---

## Entities

### HarvestQualityService (new seam) — pure, stateless

| Field/Member | Type | Constraint | Source |
|---|---|---|---|
| `IHarvestQualityService` | interface | In `LivingRoots.Domain`; 4 methods: `Decide`, `ResolveFeedback`, `BoostQualityProbability`, `BoostExtraHarvestChance` | internal-seam.md §5.4 |
| `HarvestQualityService` | impl | In `LivingRoots.Domain.Services`; pure, no SMAPI/game deps; wired in `ModEntry` as `new HarvestQualityService()` | internal-seam.md §5.1, §2.4 |
| `HarvestTier` | enum | `Unknown=0, Poor=1, Moderate=2, Healthy=3, OutOfScope=4` | internal-seam.md §5.2 |
| `HarvestFeedbackFlags` | `[Flags]` enum | `None=0, Poor=1, LivingQuality=2, ExtraHarvest=4` | internal-seam.md §5.2 |
| `HarvestQualityInputs` | readonly record struct | FarmingLevel (int), FertilizerTier (0–3), IsPaddyCrop (bool), RegrowDays (int), HarvestMaxIncreasePerFarmingLevel (int), IsForageCrop (bool), IsFibreSeed (bool), IsQiFruit (bool), PestQualityPenalty (int?), IsInventoryFull (bool) | internal-seam.md §5.3 |
| `HarvestQualityDecision` | readonly record struct | Tier, IsInScope, ForceQualityZero, QualityBoost (float), ExtraHarvestBoost (float), PestQualityPenalty (int?), MainItemBlocked (bool), ExtraItemDroppedAsDebris (bool) | internal-seam.md §5.3 |

### Validation Rules

- Pure/deterministic given inputs (no RNG, no I/O, no game state) — replicated deterministic roll (A8).
- `Decide` treats `NaN`/`Inf` soil health → `HarvestTier.Unknown` → no modification (A3).
- Measured `0` → `HarvestTier.Poor` → force 0 (A7).
- Finite out-of-range → clamp to boundary category (below 0 → Poor; above 100 → Healthy).
- Forage crops (`IsForageCrop`), fibre seeds, Qi Fruit → `HarvestTier.OutOfScope` → no modification, no feedback (A6).
- Inventory full → `MainItemBlocked = true` (main item vanilla-blocked); `ExtraItemDroppedAsDebris = true` (always, vanilla debris fallback, A1).
- Pest clamp: echoed from `PestQualityPenalty` (null → no clamp, A5).
- Seam references `ModConstants.SoilQualityHealthyBoost` (0.08) and `ModConstants.SoilExtraHarvestHealthyBoost` (0.10) (satisfies `ProductionSourceGuardTests`).
- `BoostQualityProbability` returns vanilla probability unchanged unless decision is Healthy.
- `BoostExtraHarvestChance` returns vanilla chance unchanged unless decision is Healthy.
- `ResolveFeedback` depends on rolled outcome (B1/B2), not just soil health.

### State Transitions

- None — the seam is stateless. The Harmony patch is the stateful Humble Object (game-thread callback).

### Harmony Patch (new, SMAPI-facing)

| Member | Type | Constraint |
|---|---|---|
| `EnableHarmony` | `.csproj` property | `<EnableHarmony>true</EnableHarmony>` in first `<PropertyGroup>` (SMAPI-bundled 2.2.x) |
| Patch class | `static class` | Registered via code API in `ModEntry.Entry`: `new Harmony(ModManifest.UniqueID).Patch(...)` |
| Patch methods | `static` | Wrapped in `try/catch`; log errors via `IMonitor`; default to original game logic on failure |
| Patch style | non-suppressing two-phase | Prefix returns `true` (state capture + crop-scope guard + extra-harvest probability adjustment); Postfix forces `Quality = 0` (Poor), applies Healthy boost as conditional upgrade, drives feedback, handles inventory-full |
| `__result` | `bool` | Success flag (not the item); do not confuse with `Object.Quality` |
| `Object.Quality` | public get/set | Set directly in Postfix (no reflection) |

### Relationships

- `HarvestQualityService` depends on `ModConstants` only (no SMAPI/game deps).
- Harmony patch depends on `IHarvestQualityService`, `ISoilHealthService`, `IVisualizationService`, `IMonitor`, `IPlayerProvider` (or `IPlayerInventory`).
- Spec 014 (soil-health-crop-quality) is the parent feature; this spec adds the Harmony-specific implementation contract for the harvest-hook portion (tasks T044–T054, T007, T017).
