# Contracts — Playable Compost Loop (004)

**Project type**: SMAPI desktop mod (`.NET 6` / C#) — external interfaces are SMAPI asset/data contracts and domain service contracts (test / inter-mod visibility via `InternalsVisibleTo`).

---

## 1. Game-Visible Data Contracts (SMAPI `AssetRequested` — `ModEntry.OnAssetRequested`)

These define what the game (and other mods via SMAPI) sees when loading assets.

### 1.1 `Data/Objects` — Compost Item (`LivingRoots.Compost`)

```json
{
  "Name": "LivingRoots.Compost",
  "DisplayName": "Compost",
  "Description": "Rich organic fertilizer.",
  "Type": "Basic",
  "Category": "-9", // Basic category; confirm during implementation
  "Price": 0,
  "Texture": "Mods/LivingRoots/Compost",
  "SpriteSize": [16, 16],
  "CanBeGivenAsGift": true
}
```

**Constraints (FR-005, FR-006)**:
- Only obtainable through completed bin batch (`Data/Machines` output → inventory).
- No alternate crafting / purchase path bypassing bin.

---

### 1.2 `Data/BigCraftables` — Composting Bin (`LivingRoots.CompostingBin`)

```json
{
  "Name": "LivingRoots.CompostingBin",
  "DisplayName": "Composting Bin",
  "Description": "A bin for turning organic waste into compost.",
  "Texture": "Mods/LivingRoots/CompostingBin",
  "SpriteSize": [16, 32],
  "PlacementRestriction": "Farmable",
  "IsFurniture": false,
  "CanBeRemoved": true,
  "ContextTags": []
}
```

**Constraints (FR-001a, FR-002)**:
- Must behave as native `Data/Machines` machine — uses game's machine UI, progress display, input/output rules.
- Placeable on farm / `Greenhouse`; rejected on water / buildings / non-farm interiors.

---

### 1.3 `Data/Machines` — Bin Machine Definition

```json
{
  "Name": "LivingRoots.CompostingBin",
  "DisplayName": "Composting Bin",
  "Input": { "ItemId": "<inherited from spec 002>", "Quantity": 1 }, // Qualifying set owned by spec 002 (FR-003); see CompostApplicationService for validation
  "Output": { "ItemId": "(O)LivingRoots.Compost", "Quantity": 1 },
  "MinutesUntilReady": 0,
  "DaysUntilReady": 2,
  "Triggers": [
    {
      "Trigger": "DropIn",
      "Condition": "...",
      "Result": "Ready",
      "Id": "..."
    }
  ],
  "MachineTileSets": [ ... ],
  "DeluxeOutputChance": 0.0
}
```

**Constraints (FR-001b)**:
- `DaysUntilReady`: exactly `2` (`MaturationDays`). No acceleration / shortening by mod.
- `Output` quantity fixed to `1`; no maturation multiplier.
- Fallback permitted: event-interception (`OnDayStarted` exactly-once guard) if native `Data/Machines` cannot preserve `Item.modData` progress — but data declaration remains native.

---

### 1.4 `Data/CraftingRecipes` — Bin Crafting Recipe

```json
{
  "Name": "LivingRoots.CompostingBinRecipe",
  "Ingredients": { "Wood": 50, "Stone": 20 }, // Placeholder — confirm with maintainer during T014
  "Output": { "ItemId": "(BC)LivingRoots.CompostingBin", "Quantity": 1 },
  "IsKnown": true
}
```

---

## 2. Domain Service Contracts (Internal / Test-Visible via `InternalsVisibleTo`)

These interfaces live in `Domain/Interfaces/` and are implemented in `Services/`. They govern the feature's behavior and are the surface for test stubs (`ThreadSafeGameLoopEventsStub`).

### 2.1 `ISoilHealthService`

```csharp
public interface ISoilHealthService
{
    int GetHealth(GameLocation location, Vector2 tile);
    void ApplyCompost(GameLocation location, Vector2 tile, int amount); // amount = 1 item; +15 pts clamped
    event EventHandler<TileHealthChangedEventArgs> OnTileHealthChanged; // ≤16.67ms invalidation
    // ... persistence / load / save contracts
}
```

**Guarantees (FR-007, FR-009, FR-010)**:
- Health always in `[0, 100]`.
- `OnTileHealthChanged` fires on mutation → `VisualizationService` reacts with no polling.
- Application allowed on active crop tiles (side-dressing); invalid floors refused with visible message.

---

### 2.2 `ICompostingBinService`

```csharp
public interface ICompostingBinService
{
    CompostingBinState GetState(GameLocation location, Vector2 tile);
    bool TryInsertOrganicWaste(GameLocation location, Vector2 tile, Item item, string playerId); // first-wins atomic
    bool TryCollect(GameLocation location, Vector2 tile, Player player); // delivers 1 if Ready; full inventory retries
    IEnumerable<Item> GetInventoryProgressItems(); // items with modData (InputId, DaysRemaining)
    // ... save / load / tolerant reader
}
```

**Guarantees (FR-001–FR-005c, FR-019)**:
- Host-authoritative atomic lock per tile / machine instance; first wins; second refused with explanation.
- Batch yield = `1`; `DaysRemaining` decrements exactly once/day.
- Pickup preserves progress (`modData`); unplaced = processing paused; max stack `1` while progress present.

---

### 2.3 `ICompostApplicationService`

```csharp
public interface ICompostApplicationService
{
    ApplicationResult TryApply(GameLocation location, Vector2 tile, Player player, Item compostItem);
    // Returns: Applied (health +15, item decremented), Refused (reason key), NoChange (already at 100, etc.)
}
```

**Guarantees (FR-007, FR-008, FR-008a)**:
- Consumes exactly `1` compost from active inventory on success; `0` consumption on refusal.
- Refusal delivered via `Game1.showRedMessage` + `ITranslationHelper` (keys: `message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`, `message.harvest.inventory-full`).
- No custom UI / overlay / text-drawing component added.

---

## 3. Localization Contract (`i18n/default.json`)

Keys required by `FR-008a` (registered in `i18n/default.json`, rendered via `Game1.showRedMessage` in game font / player's language):

```json
{
  "message.compost.invalid-tile": "You can't apply compost here.",
  "message.compost.max-health": "This soil is already at maximum health.",
  "message.compost.action-conflict": "Another player changed this first.",
  "message.harvest.inventory-full": "Your inventory is full; come back when you have space."
}
```

**Note**: Translation values are placeholders; live localization follows SMAPI `ITranslationHelper` conventions.

---

## 4. Persistence / Save Contracts

- **Bin save**: `composting_bins_{saveId}` via `IModDataService`; per-entry fault isolation; obsolete fields (`MaturationLevel`, `ConsecutiveActiveDays`, `ConsecutiveIdleDays`) ignored; orphaned entries (no `GameLocation.Objects` match) pruned.
- **Soil save**: `soil_health_data_{saveId}`; default `0` not stored (sparse); corrupt tiles skipped; bounds enforced on load.
- **Item progress**: `Item.modData` (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`) serializes into inventory item; non-stackable enforcement when progress active (`max stack = 1`).
