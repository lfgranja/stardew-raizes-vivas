# Data Model: Soil Health Decay + Compost Restoration

**Feature Branch**: `002-soil-decay-compost` | **Date**: 2026-09-06

## Entity Definitions

### 1. SoilHealthTile

A tilled soil position with an associated health value.

| Field | Type | Validation | Description |
|-------|------|------------|-------------|
| LocationName | `string` | Required, max 100 chars, sanitized | Game location containing the tile |
| TileX | `int` | -10000 to 10000 | X coordinate of the tile |
| TileY | `int` | -10000 to 10000 | Y coordinate of the tile |
| Health | `float` | 0.0 to 100.0 (clamped) | Current soil health value |

**Identity**: Uniquely identified by `(LocationName, TileX, TileY)` composite key.

**State transitions**:
- Created: When soil is first tilled and health is set
- Modified: When health changes (decay or restoration)
- Persisted: On save, serialized to JSON
- Loaded: On load, deserialized from JSON

**Persistence**: Uses existing `SoilHealthService` save/load via `IModDataService`. Key: `soil_health_data_{saveId}`.

---

### 2. CompostingBinState

Represents the current state of a single composting bin machine.

| Field | Type | Validation | Description |
|-------|------|------------|-------------|
| TileX | `int` | -10000 to 10000 | X coordinate of the bin |
| TileY | `int` | -10000 to 10000 | Y coordinate of the bin |
| State | `CompostingBinStateEnum` | Required | Current state (Empty, Processing, Ready) |
| InputItemId | `string?` | Null if empty | Qualified item ID of input waste |
| InputTimestamp | `long?` | Null if empty | Game time when waste was added (minutes) |
| MaturationLevel | `int` | 1 to 5 | Current output multiplier |
| ConsecutiveIdleDays | `int` | 0 to 14 | Days since last input/output activity |

**Identity**: Uniquely identified by `(LocationName, TileX, TileY)` composite key.

**State transitions**:
```
Empty → Processing (when waste added)
Processing → Ready (when time elapsed)
Ready → Empty (when compost collected)
Processing → Empty (when bin broken, items dropped)
Ready → Empty (when bin broken, items dropped)
```

**Persistence**: New save key `composting_bins_{saveId}_{locationName}`. Serialized to JSON via `IModDataService`.

---

### 3. CompostingBinMaturation

Tracks maturation progress for output multiplier (embedded in CompostingBinState).

| Field | Type | Validation | Description |
|-------|------|------------|-------------|
| Level | `int` | 1 to 5 | Current maturation level |
| ConsecutiveIdleDays | `int` | 0 to 14 | Days without input/output activity |

**Maturation rules**:
- Level 1: Initial state (1:1 output)
- Level 2: After 7 consecutive days of operation (2:1 output)
- Level 3: After 14 consecutive days (3:1 output)
- Level 4: After 21 consecutive days (4:1 output)
- Level 5: After 28 consecutive days (5:1 output, maximum)
- Reset: After 14 consecutive idle days → Level 1

**Identity**: Part of `CompostingBinState` (no separate entity).

---

### 4. CompostItem

A consumable item that restores soil health when applied to tilled soil.

| Field | Type | Validation | Description |
|-------|------|------------|-------------|
| QualifiedItemId | `string` | Fixed: `LivingRoots.Compost` | Unique item identifier |
| DisplayName | `string` | Localized via I18n | Player-facing name |
| Description | `string` | Localized via I18n | Item description |
| Category | `int` | Fixed: -26 | Fertilizer category |
| StackMax | `int` | Fixed: 999 | Maximum stack size |
| Texture | `AssetPath` | `assets/compost.png` | 16x16 sprite |

**Identity**: Single static definition (not per-instance).

**Persistence**: Not persisted individually — tracked as part of player inventory via Stardew Valley's built-in inventory system.

---

### 5. CompostingBinObject

The placeable machine object (crafting machine, keg-style).

| Field | Type | Validation | Description |
|-------|------|------------|-------------|
| QualifiedItemId | `string` | Fixed: `LivingRoots.CompostingBin` | Unique object identifier |
| DisplayName | `string` | Localized via I18n | Player-facing name |
| Description | `string` | Localized via I18n | Object description |
| Price | `int` | 0 (craftable, not purchasable) | Gold price |
| Texture | `AssetPath` | `assets/composting_bin.png` | 16x32 sprite (machine) |
| PlacementSound | `string` | `axe` | Sound on placement |

**Identity**: Single static definition (not per-instance).

**Persistence**: Placed objects tracked by Stardew Valley's object layer; state persisted separately via `CompostingBinState`.

---

### 6. OrganicWasteDefinition

Defines which items qualify as valid composting inputs.

| Field | Type | Validation | Description |
|-------|------|------------|-------------|
| ValidCategories | `int[]` | [-74, -75, -79, -80, -81] | Valid vanilla category IDs |
| InclusionTag | `string` | `compostable_item` | Custom context tag for mod items |
| ExclusionTag | `string` | `not_compostable` | Override tag to reject items |

**Identity**: Single static configuration.

---

## Entity Relationships

```
SoilHealthTile (1) ←applies→ (1) CompostItem
    │
    │ restores health
    │
CompostingBinState (1) ←produces→ (N) CompostItem
    │
    │ consumes
    │
OrganicWasteDefinition (1) ←validates→ (N) Items
```

- **SoilHealthTile** is restored by **CompostItem** (one application = +15 health)
- **CompostingBinState** produces **CompostItem** (after 2 days, quantity = maturation level)
- **CompostingBinState** consumes items validated by **OrganicWasteDefinition**
- **CompostingBinState** tracks **CompostingBinMaturation** (embedded)

---

## Save Data Schema

### Soil Health Data (existing)

```json
{
  "LocationHealthData": {
    "Farm": {
      "10,20": 85.0,
      "10,21": 72.5
    },
    "Greenhouse": {
      "5,5": 100.0
    }
  }
}
```

### Composting Bin Data (new)

```json
{
  "LocationBinData": {
    "Farm": {
      "15,25": {
        "State": "Processing",
        "InputItemId": "Object_(o)178",
        "InputTimestamp": 1200,
        "MaturationLevel": 3,
        "ConsecutiveIdleDays": 0
      }
    }
  }
}
```

---

## Validation Rules

### Soil Health Bounds
- Minimum: 0.0 (hard floor)
- Maximum: 100.0 (hard ceiling)
- NaN/Infinity: Clamped to 0.0

### Composting Bin State
- Maturation level: 1 to 5 (clamped)
- Consecutive idle days: 0 to 14 (reset at 14)
- Processing time: 2880 minutes (2 full days × 1440 min/day)

### Item Validation
- Compost item: Must have `QualifiedItemId == "LivingRoots.Compost"`
- Organic waste: Must match valid category OR `compostable_item` tag, AND NOT have `not_compostable` tag

### Tile Ownership
- Compost application allowed: `location.IsFarm == true` OR `location.Name == "Greenhouse"`
- All other locations: Reject without consumption

---

## State Machine: Composting Bin

```
┌─────────┐    add waste     ┌─────────────┐   time elapsed   ┌─────────┐
│  EMPTY   │ ──────────────→ │  PROCESSING  │ ──────────────→ │  READY   │
└─────────┘                  └─────────────┘                  └─────────┘
     ↑                             │                               │
     │                             │ break                         │ break
     │                             ↓                               ↓
     │                        items drop                        items drop
     │                             │                               │
     └─────────────────────────────┴─────────────────────────────┘
                              (bin removed)
```

**Transitions**:
- Empty → Processing: Valid waste added, state set, timestamp recorded
- Processing → Ready: Current time ≥ input timestamp + 2880 minutes
- Ready → Empty: Player collects compost, state reset
- Processing/Ready → (gone): Bin broken, items dropped on ground
