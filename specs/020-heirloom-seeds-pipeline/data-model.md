# Data Model: Heirloom Seeds / Sementes Crioulas Pipeline (Crioulo Parsnip)

**Feature**: `020-heirloom-seeds-pipeline` | **Date**: 2026-11-09

Entities are data-asset content + item/soil rules; no new persisted structures. Identifiers are namespaced `LivingRoots_*`; qualified form `(O)<id>`.

## Entities

### 1. Heirloom Seed (`LivingRoots_HeirloomParsnipSeeds`)

| Field | Value | Notes |
|---|---|---|
| `Data/Objects` key | `LivingRoots_HeirloomParsnipSeeds` | unqualified ID; resolution via ItemRegistry |
| `Type` | `Seeds` | |
| `Category` | `-74` | vanilla seeds category; required for planting |
| `Name` | `LivingRoots_HeirloomParsnipSeeds` (internal) | match ID per best practice |
| `DisplayName` | `i18n: item.heirloom.parsnip-seed.name` | EN: "Crioulo Parsnip Seeds" |
| `Description` | `i18n: item.heirloom.parsnip-seed.description` | honest crioulo copy |
| `Price` | `35` | vs vanilla parsnip 20g; labeled first-pass tunable |
| `Texture` | `Mods/lfgranja.LivingRoots/HeirloomSeeds` | placeholder icon (16×16) |
| `SpriteIndex` | `0` | single icon sheet |
| `ContextTags` | `[ heirloom_seed, seed_item ]` | prefix of `ModConstants.HeirloomSeedTag` family |
| `Edibility` | not set (`-300` default n/a for seeds) | |
| `CanBeGivenAsGift` | `true` (default) | no gift-taste entries this slice (recorded) |
| `ExcludeFromRandomSale` / `ExcludeFromShippingCollection` | not set | no exclusions required |

Lifecycle: acquirable (Traveler cart; Seed Maker produce-recovery) → plantable (Spring-legal via crop entry) → consumed on plant (vanilla semantics) → 1 seed returned per harvest (FR-009).

### 2. Heirloom Produce (`LivingRoots_HeirloomParsnip`)

| Field | Value | Notes |
|---|---|---|
| `Data/Objects` key | `LivingRoots_HeirloomParsnip` | |
| `Type` | `Basic` | |
| `Category` | `-75` | vegetable |
| `Price` | `45` | premium vs vanilla 35g; labeled tunable |
| `Texture` | `Mods/lfgranja.LivingRoots/HeirloomProduce` | placeholder icon (16×16) |
| `SpriteIndex` | `0` | |
| `ContextTags` | `[ heirloom_produce, compostable_item, category_vegetable ]` | **must** pass `OrganicWasteValidator` (FR-010) |
| `Edibility` | `-300` (default non-eatable cooking value; no recipe this slice) | conservative |

### 3. Crioulo Crop Entry (`Data/Crops` keyed by seed ID)

| Field | Value | Notes |
|---|---|---|
| Key | `LivingRoots_HeirloomParsnipSeeds` | **key = seed item ID** (1.6 contract) |
| `Seasons` | `[ Spring ]` | Greenhouse allowed (season override), unchanged |
| `DaysInPhase` | `[ 1, 2, 2, 2 ]` | sum = **7** (asserted by test; FR-002) |
| `RegrowDays` | `-1` | no regrow |
| `IsRaised` | `false` | not trellised |
| `IsPaddyCrop` | `false` | |
| `NeedsWatering` | `true` | |
| `PlantableLocationRules` | omitted | farm/greenhouse default |
| `HarvestItemId` | `LivingRoots_HeirloomParsnip` | resolves via ItemRegistry (tested) |
| `HarvestMinStack` / `HarvestMaxStack` | `1` / `1` | one produce per harvest for crioulo |
| `HarvestMaxIncreasePerFarmingLevel` | not set | crioulo restraint |
| `ExtraHarvestChance` | `0.1` | 014/vanilla-doc'd 1.6 semantics; labeled tunable |
| `HarvestMethod` | `Grab` | |
| `HarvestMinQuality` / `HarvestMaxQuality` | `0` / omitted (vanilla decides) | |
| `TintColors` | `[]` | no tint |
| `Texture` | `Mods/lfgranja.LivingRoots/HeirloomCrops` | placeholder spritesheet row |
| `SpriteIndex` | `0` | row 0 (single crioulo crop this slice) |
| `CountForMonoculture` / `CountForPolyculture` | `true` / `true` | crioulo counts for both achievements |
| `CustomFields` | not set | no mod-internal state on crop |

Invariants (property-tested):
- `sum(DaysInPhase) == 7`
- `HarvestItemId` resolves to a registered `Data/Objects` key
- seed `Category == -74 && Type == "Seeds"`
- produce passes waste validator inclusion (tag `compostable_item`)

### 4. Traveler Cart Stock Entry (`Data/Shops` key `Traveler`)

| Field | Value | Notes |
|---|---|---|
| `Items[].Id` | `LivingRoots_HeirloomParsnipSeeds` | unique; collisions must be tested absent |
| `Items[].ItemId` | `(O)LivingRoots_HeirloomParsnipSeeds` | game/SMAPI qualified |
| `Items[].Price` | `35` (or unset ⇒ `UseObjectDataPrice: true`) | final choice in implement; either is legal |
| `Items[].AvailableStock` | `5` | generous-but-bounded |
| `Items[].AvailableStockLimit` | `Player` | co-op-safe |
| `Items[].Condition` | `SEASON Spring` | if unsupported at runtime → fallback unconditional, disclosed |

### 5. Soil-Coupling Effects (rules, not stored fields)

| Rule | Trigger | Write | Authority |
|---|---|---|---|
| Plant bonus | `HoeDirt.plant` postfix, heirloom seed IDs only | `SetSoilHealth(tile, clamp(measuredOrNaN→100 + HeirloomPlantBonus))` (measured 0–100) | Host; farmhand via request/refusal |
| Harvest bonus | harvest seam, heirloom crop ID + measured ≥ 71 | flat `+HeirloomHarvestBonus` bounded write | Host; farmhand via request/refusal |
| Seed save | any completed heirloom harvest | inventory `+1` heirloom seed (or refusal copy) | harvesting player (local inventory) |
| 014 interaction | every heirloom harvest | **unchanged** 014 tier math | existing `IHarvestQualityService` |

### 6. Messaging (MP)

- `SoilHealthSyncMessage` (existing) — reused for broadcast after host-applied heirloom writes.
- `HeirloomPlantBonusRequest` (new, typed string `"HeirloomPlantBonusRequest"`, same `modIDs` envelope) — farmhand→host request `{ saveId, day, x, y, itemId }`; host validates (host turn, bounds, idempotency by `(tile, day)` or equivalent guard), applies `HeirloomPlantBonus`, broadcasts snapshot. If correlation/validation fails → no mutation; farmhand shows authority-unsupported copy.

### 7. Non-persistence

- No new save files, schema, keys, or migrations. Additive items only; unknown items degrade to vanilla Error Item (documented 1.6 behavior). Sparse-zero persistence limitation carries over (unmeasured 0 never round-trips).

### 8. Directory/file manifest (created by this feature)

- `LivingRoots/Assets/{heirloom_seeds.png, heirloom_produce.png, Crops/heirloom_crops.png}`
- `specs/020-heirloom-seeds-pipeline/scenarios/heirloom-plant-bonus.scenario.json`
