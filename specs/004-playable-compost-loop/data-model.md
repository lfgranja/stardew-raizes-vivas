# Data Model — Playable Compost Loop (004)

**Source**: `spec.md` (Key Entities, FR-001–FR-019) | **Branch**: `004-playable-compost-loop`

---

## Entities

### Composting Bin (Machine State Machine)

| Field | Type | Source / Constraint |
|---|---|---|
| `Location` | `GameLocation` + tile coord | Placed on farm / Greenhouse; invalid: water, buildings, non-farm interiors (FR-002) |
| `State` | Enum: `Empty` → `Processing` → `Ready` | Native `Data/Machines` `Triggers` + `DaysUntilReady` (FR-001a / FR-001b) |
| `InputId` | `string` (item qualified-ID) | Serializes into `Item.modData` (`LivingRoots.InputId`) when in inventory (FR-005c) |
| `DaysRemaining` | `int` | Serializes into `Item.modData`; decrements exactly once per in-game day (FR-004) |
| `ProgressPaused` | `bool` | `true` when unplaced (inventory); `false` when placed (FR-005c) |
| `MaxStack` | `int` | `1` when carrying `InputId` or `Ready`; else default (FR-005c) |
| `BatchYield` | `int` | Fixed `1` (FR-005a); never scaled |

**State Transitions** (host-authoritative atomic):
- `Empty + valid waste inserted` → `Processing` (set `InputId`, `DaysRemaining = MaturationDays = 2`)
- `Processing + 1 in-game day` → either `Processing` (decrement `DaysRemaining`) or `Ready` (if `DaysRemaining == 0`)
- `Ready + collect` → yield 1 `Compost`; reset to `Empty`; if full inventory → retry later, nothing destroyed (FR-005b / FR-009 equivalent for harvest)
- `Empty / Processing / Ready + remove` → `Item.modData` preserved; processing pauses; restore on placement
- `Ready + collect when inventory full` → stay `Ready`; deliver full batch later (FR-005b)

**Validation Rules**:
- Placeable only on farm land or `Greenhouse` (FR-002)
- Non-qualifying input rejected with native machine shake / audio cancel (FR-003 / FR-008a clarification)
- Concurrent mutation: first wins; second evaluated against new state; refused with `message.compost.action-conflict` if invalid (FR-019)

**Persistence (tolerant)**:
- Save key: `modData` / `IModDataService` per `saveId`; per-entry `try/catch`; obsolete fields ignored; missing-world-object entries pruned (FR-012 / FR-015)

---

### Compost (Item)

| Field | Type | Source / Constraint |
|---|---|---|
| `QualifiedItemId` | `string` | Registered via `Data/Objects` (`LivingRoots.Compost`) |
| `DisplayName` / `Description` | `string` (localized) | Game-native; no custom UI |
| `Stackable` | `bool` | Standard item stack behavior |
| `Source` | Constraint | ONLY from completed bin batch (FR-006) — no crafting bypass |

**Validation / Rules**:
- Consumption per application = exactly `1` from active inventory stack (FR-007 clarification)
- No destruction / partial loss on blocked collection (FR-005b, FR-009)

---

### Soil Health (Per Tile, Farm-Scoped)

| Field | Type | Source / Constraint |
|---|---|---|
| `LocationName` | `string` (map key) | Farm-scoped |
| `TileKey` | `string` (coordinate key) | Parsed / sanitized via `ModDataService.SanitizePathSegments()` |
| `HealthValue` | `int` (0–100 inclusive) | `ModConstants.MinSoilHealth` / `MaxSoilHealth`; clamped (FR-009 / FR-007) |
| `Baseline` | Implicit | `0` = neutral vanilla dirt, no overlay (FR-009, clarification 2026-10-04) |

**State Transitions**:
- `Health < 100 + valid tilled tile (HoeDirt) + compost applied` → `Health = min(100, Health + 15)` (FR-007 clarification: +15 points, clamped)
- Active crop on tile permitted — side-dressing does not harm / remove crop (FR-007 clarification 2026-10-04)
- Stop at `100`; refusal with `message.compost.max-health`; no consumption (FR-008)

**Relationships**:
- `Compost` (item) → applied to `SoilHealth` (tile) via `CompostApplicationService`
- `VisualizationService` renders from `SoilHealthService` cache via reactive `OnTileHealthChanged`

**Persistence (sparse)**:
- Do not store `0` health (default / unmodified) — keeps save files small (project convention)
- Save key: `soil_health_data_{saveId}` via `IModDataService`
- Load: tolerate corrupt per-tile; skip / log sanitized (FR-012 / FR-015)

---

### Organic Waste (Inherited Input Qualify Set)

| Field | Type | Source / Constraint |
|---|---|---|
| `QualifiedItemId` | Set of `string` | Owned by soil-decay / compost specification (`FR-003`); this feature inherits by reference — neither restates nor extends |
| `ValidForBin` | `bool` | Checked by `CompostApplicationService` / bin input validator; non-qualifying rejected natively |

**Note**: The qualifying set is NOT restated in this spec; any change belongs to the prior specification.

---

## Invariants (Property-Based / Stubs)

- `SoilHealth` always in `[0, 100]` (FR-009 / `ModConstants` bounds)
- `CompostingBin` batch yield always `1` (FR-005a)
- `DaysRemaining` advances exactly once per in-game day regardless of player count (FR-004 / FR-013)
- Concurrent mutation on same tile / bin = exactly `1` applied; second refused with visible reason (FR-019 / SC-010)
- Visual overlay invalidation ≤ 16.67ms (FR-010 / SC-10) — verified by stub, not live packet transport (debt D1)
