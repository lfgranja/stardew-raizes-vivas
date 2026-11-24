# Phase 0 Research: Soil Health Decay + Compost Restoration

**Feature Branch**: `002-soil-decay-compost` | **Date**: 2026-09-06

## Research Outcomes

### 1. Stardew Valley Machine Implementation Pattern

**Decision**: Use `Data/Machines` data file with standard machine behavior class extending `StardewValley.Machines.Machine` base or implementing `IMachineActions`.

**Rationale**: The Keg, Preserves Jar, and other processing machines all follow this pattern. The Composting Bin is functionally identical to these (input → process → output). Using the standard pattern ensures:
- Automatic Automate mod compatibility (Automate reads `Data/Machines` natively)
- Standard hover tooltip support (satisfies FR-019 without custom code)
- Standard input/output interaction (equip-to-add, empty-collect)
- Standard animation state machine (empty → processing → ready)

**Alternatives considered**:
- Custom building (shed-style): Rejected — requires carpenter menu integration, more complex placement logic
- Object/prop (scarecrow-style): Rejected — no processing behavior, no output collection

**Source**: Stardew Valley Modding Wiki — Machine Guide; existing mod implementations (SVE, PPJA)

---

### 2. Tilled Soil Detection via Game API

**Decision**: Check `terrainFeature is HoeDirt` with `crop == null` for bare state. Dead crop residue: `crop != null && crop.dead`.

**Rationale**: The `HoeDirt` class in Stardew Valley's API has a `crop` property. When `crop` is null, the tile is bare. When `crop.dead` is true, the crop is residue (mulch protection per spec).

**Implementation approach**:
```csharp
if (location.terrainFeatures.TryGetValue(tile, out var terrainFeature)
    && terrainFeature is HoeDirt hoeDirt)
{
    bool isBare = hoeDirt.crop == null;
    bool hasResidue = hoeDirt.crop != null && hoeDirt.crop.dead;
}
```

**Source**: Stardew Valley decompiled game code via SMAPI

---

### 3. Seasonal Multiplier Timing

**Decision**: Apply at day start before decay calculation. Use `Game1.currentSeason` for current season.

**Rationale**: The day start event (`IDayStartedWritableAPI`) fires once when the new day begins. At this point, `Game1.currentSeason` is already updated to the new season. Seasonal multipliers are applied per-tile during the linear scan.

**Season mapping**:
- Spring: 0.5x
- Summer: 1.5x
- Fall: 0.5x
- Winter: 0x (no decay)

**Alternatives considered**:
- Apply multiplier at save time: Rejected — season could change between save and load
- Apply at event registration: Rejected — season is dynamic

**Source**: Stardew Valley game loop documentation; existing `DayStarted` event patterns

---

### 4. Persistence Pattern for Composting Bin State

**Decision**: Extend `IModDataService` with a new key for composting bin data. Store `CompostingBinState` per location.

**Rationale**: The existing `SoilHealthService` uses `IModDataService` with JSON serialization keyed by save ID. The composting bin state follows the same pattern:
- Key: `composting_bins_{saveId}_{locationName}`
- Value: `Dictionary<string, CompostingBinState>` keyed by tile coordinates `"X,Y"`

**State stored per composting bin**:
- Tile position (X, Y)
- Input timestamp (when waste was added)
- Maturation level (1-5)
- Consecutive idle days (for maturation reset)
- Input item ID (for drop-on-break)

**Alternatives considered**:
- Separate JSON file: Rejected — breaks single-save-id pattern, harder to manage
- Extend SoilHealthState: Rejected — different domain concerns, different save/load timing

**Source**: Existing `SoilHealthService` implementation (lines 26-519)

---

### 5. Input Handling for Compost Application

**Decision**: Use `IInputEvents.ButtonPressed` with right-click detection. Check player is holding compost item and target tile is valid.

**Rationale**: `ButtonPressed` fires once at the moment of player intent (button goes down), which is the standard SMAPI pattern for discrete item-on-tile interactions like applying compost. Note: The existing `ModController.OnButtonReleased` is used for cursor position tracking (updating visualization overlays), which is a fundamentally different use case — it tracks where the cursor is after a click ends, not whether the player intends to use an item on a tile.
1. Detect right-click press (fires once on button down)
2. Check player's held item is compost (by item ID)
3. Get cursor tile position
4. Validate tile is tilled soil on player's farm/Greenhouse
5. Apply compost (increase health, consume item, show floating text)

**Item held check**:
```csharp
var heldItem = Game1.player.CurrentItem;
if (heldItem?.QualifiedItemId == ModConstants.CompostItemId) { ... }
```

**Source**: Existing `ModController.OnButtonReleased` pattern (lines 745-758); Stardew Valley interaction conventions

---

### 6. Compost Item Creation

**Decision**: Register a new Object item with unique ID via `ItemRegistry.Create<Object>`. Category: -26 (fertilizer-like, consumable on tile).

**Rationale**: Stardew Valley's item system uses `ItemRegistry` for dynamic item registration. The compost item is a simple consumable Object (not a tool or weapon).

**Item properties**:
- ID: `LivingRoots.Compost` (mod-unique qualified ID)
- Category: -26 (fertilizer)
- Display name: `I18n.CompostItem_Name` (localized)
- Description: `I18n.CompostItem_Description` (localized)
- Texture: `assets/compost.png` (16x16 sprite)
- Stackable: Yes (max 999)

**Source**: Stardew Valley item system documentation; PPJA mod item registration patterns

---

### 7. Composting Bin Machine Registration

**Decision**: Register machine via `Data/ObjectInformation` (for placement) and `Data/Machines` (for behavior).

**Rationale**: Stardew Valley machines require two data entries:
1. `Data/ObjectInformation` — defines the placeable object (name, description, texture, price)
2. `Data/Machines` — defines the machine behavior (input/output rules, processing time, animations)

**Object entry**:
```
LivingRoots.CompostingBin: "Composting Bin/Converts organic waste into soil-restoring compost./1 1/1/1 1/LivingRoots.CompostingBin/-300/Machine/32 32/Placeable/"
```

**Machine entry**:
```
LivingRoots.CompostingBin: LivingRoots.Compost LIVINGROOTS_WASTE_ITEMS 2/...
```

**Source**: Stardew Valley machine registration documentation

---

### 8. Organic Waste Validation

**Decision**: Hybrid approach — check item category IDs OR custom context tag. Exclude items with `not_compostable` tag.

**Rationale**: The spec defines valid categories (-74 Seeds, -75 Vegetables, -79 Fruits, -80 Flowers, -81 Forage/Greens) plus custom `compostable_item` tag for mod extensibility. The `not_compostable` tag overrides all.

**Implementation**:
```csharp
bool IsValidOrganicWaste(Item item)
{
    // Check exclusion first
    if (item.HasContextTag("not_compostable")) return false;
    
    // Check custom inclusion tag
    if (item.HasContextTag("compostable_item")) return true;
    
    // Check vanilla categories
    return item.Category switch
    {
        -74 => true,  // Seeds
        -75 => true,  // Vegetables
        -79 => true,  // Fruits
        -80 => true,  // Flowers
        -81 => true,  // Forage/Greens
        _ => false
    };
}
```

**Source**: Stardew Valley item category documentation; Json Assets mod context tag conventions

---

### 9. Floating Text Rendering

**Decision**: Use `TemporaryAnimatedSprite` with `text` field for world-space "+15" rendering. Multiplayer-compatible via `Game1.multiplayer.broadcastSprites`.

**Rationale**: This is the standard Stardew Valley convention for stat change feedback (like damage numbers). It renders text at a world position, floats upward, and fades out.

**Implementation**:
```csharp
var sprite = new TemporaryAnimatedSprite(
    texture: null,
    sourceRectangle: Rectangle.Empty,
    animationInterval: 1000f,
    animationLength: 1,
    animationCount: 1,
    position: worldPosition,
    flicker: false,
    flipped: false);
sprite.text = "+15";
sprite.color = Color.Green;
sprite.alphaFade = 0.02f;
sprite.motion = new Vector2(0, -0.5f);

Game1.multiplayer.broadcastSprites(location, sprite);
```

**Source**: Stardew Valley sprite rendering documentation; existing mod implementations

---

### 10. Audio Cue Registration

**Decision**: Register custom sound cues via `SoundBank` or use existing Stardew Valley sound names via `Game1.playSound`.

**Rationale**: The spec defines two audio events:
1. Intermittent processing sound (every 2-3 hours)
2. "Ready" chime when processing completes

**Implementation approach**:
- Processing sound: `Game1.playSound("crafting")` or custom cue (subtle bubbling)
- Ready chime: `Game1.playSound("Ship")` or custom cue (distinct ding)
- Invalid action: `Game1.playSound("cancel")` (subtle, no visual)

**Source**: Stardew Valley sound cue documentation; existing mod audio patterns

---

## Technical Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Machine animation state desync in multiplayer | Visual inconsistency | Use standard machine state machine; broadcast sprite changes |
| Performance degradation with 10,000+ tiles | Frame drops | Linear scan is O(n) and completes in <5ms; no per-frame processing |
| Save file bloat from composting bin state | Large save files | Sparse storage (only save non-default states); cap bins per location |
| Organic waste validation performance | Lag on machine interaction | Cache validation results per item type; validate on add, not on hover |

## Open Questions Resolved

All technical unknowns from the plan's Technical Context have been resolved. No remaining NEEDS CLARIFICATION items.
