# Quickstart Validation Guide: Soil Health Decay + Compost Restoration

**Feature Branch**: `002-soil-decay-compost` | **Date**: 2026-09-06

## Overview

This guide provides runnable validation scenarios to prove the feature works end-to-end. Each scenario includes prerequisites, execution steps, and expected outcomes.

---

## Prerequisites

1. Build the mod: `dotnet build Stardew-LivingRoots.sln`
2. Install the mod in Stardew Valley's `Mods/` directory
3. Launch Stardew Valley with SMAPI
4. Load or create a save game
5. Open the SMAPI console for debug output

---

## Scenario 1: Soil Decay on Bare Tilled Soil

**Objective**: Verify that bare tilled soil loses health each day.

### Setup
1. Till a 3x3 area of soil on the farm (use hoe on grass tiles)
2. Ensure no crops are planted in the tilled area
3. Open SMAPI console and note current day

### Execution
1. Sleep/pass the night to advance to the next day
2. Observe the tiled soil tiles (visualization overlay should show health decrease)
3. Check SMAPI console for decay summary log: `[TRACE] Decay summary: 9 tiles processed, -18 total health`

### Expected Outcome
- Each bare tilled tile's health decreases by 2 (default decay rate) × seasonal multiplier
- In Spring/Fall: -1 health per tile (0.5 × 2)
- In Summer: -3 health per tile (1.5 × 2)
- In Winter: No decay (0 × 2)
- Health never goes below 0

---

## Scenario 2: Soil Decay Stops at Zero

**Objective**: Verify health does not go negative.

### Setup
1. Till a single tile
2. Let it decay until health reaches 1 or 0

### Execution
1. Continue passing days until health would go below 0
2. Verify health stays at 0

### Expected Outcome
- Health reaches 0 and stays at 0
- No negative values in save data
- Console log shows decay applied but clamped

---

## Scenario 3: Cropped Soil Does Not Decay

**Objective**: Verify planted crops prevent decay.

### Setup
1. Till two tiles
2. Plant a crop on one tile
3. Leave the other tile bare

### Execution
1. Pass one day
2. Check health of both tiles

### Expected Outcome
- Cropped tile: Health unchanged
- Bare tile: Health decreased by decay rate
- Console log shows "1 tiles processed" (only bare tile)

---

## Scenario 4: Apply Compost to Restore Health

**Objective**: Verify compost application increases soil health.

### Setup
1. Obtain compost (via console command `world_settime` or creative testing)
2. Till a tile with reduced health (e.g., 50)

### Execution
1. Equip compost in active slot
2. Right-click on the tilled tile
3. Observe floating "+15" text in green above the tile
4. Verify inventory decreased by 1 compost

### Expected Outcome
- Tile health increases by 15 (from 50 to 65)
- One compost consumed from inventory
- Green "+15" floating text appears at tile position
- Console log: `[TRACE] Compost applied at (10, 20): +15 health`

---

## Scenario 5: Compost Caps at 100 Health

**Objective**: Verify health does not exceed 100.

### Setup
1. Till a tile with health at 90
2. Obtain compost

### Execution
1. Apply compost to the tile
2. Attempt to apply another compost

### Expected Outcome
- First application: Health increases from 90 to 100 (not 105)
- Second application: No health change, compost not consumed
- Console log: Compost rejected (already at max health)

---

## Scenario 6: Compost Rejected on Non-Tilled Tile

**Objective**: Verify compost is not consumed on invalid targets.

### Setup
1. Obtain compost
2. Find a grass tile (non-tilled)

### Execution
1. Equip compost
2. Right-click on grass tile

### Expected Outcome
- No health change (tile has no health data)
- Compost not consumed
- Subtle "cancel" audio cue plays
- No visual feedback

---

## Scenario 7: Craft Composting Bin

**Objective**: Verify crafting recipe works.

### Setup
1. Have materials: 50 Wood, 25 Stone, 15 Fiber
2. Open crafting menu (Home tab)

### Execution
1. Scroll to "Composting Bin" recipe
2. Click to craft
3. Verify item added to inventory

### Expected Outcome
- Composting Bin item crafted successfully
- Materials consumed from inventory
- Item display name localized: "Composting Bin"

---

## Scenario 8: Add Waste to Composting Bin

**Objective**: Verify organic waste input works.

### Setup
1. Place Composting Bin on farm
2. Obtain valid organic waste (e.g., Wild Seeds, Parsnip)

### Execution
1. Equip organic waste
2. Right-click on composting bin
3. Observe bin state change (visual)

### Expected Outcome
- Waste consumed from inventory
- Bin enters Processing state
- Machine shows input sprite
- Console log: `[TRACE] Composting bin at (15, 25): Empty → Processing`

---

## Scenario 9: Collect Compost After Processing

**Objective**: Verify compost output after 2 days.

### Setup
1. Have a composting bin in Processing state
2. Pass 2 full in-game days

### Execution
1. Right-click bin with empty hands after 2 days
2. Verify compost items added to inventory

### Expected Outcome
- Bin was in Ready state after 2 days
- "Ready" chime played when processing completed
- Compost quantity = maturation level (1 if first week)
- Bin returns to Empty state
- Console log: `[TRACE] Composting bin at (15, 25): Ready → Empty, produced 1 compost`

---

## Scenario 10: Save/Load Preserves State

**Objective**: Verify persistence of all states.

### Setup
1. Have multiple composting bins in different states (Empty, Processing, Ready)
2. Have tilled soil with various health values

### Execution
1. Save and exit the game
2. Reload the save
3. Verify all states match pre-save values

### Expected Outcome
- Soil health values preserved exactly
- Processing bins: Correct remaining time calculated
- Ready bins: Still ready
- Maturation levels preserved
- Console log: `[TRACE] Composting bin data loaded: N bins across M locations`

---

## Scenario 11: Greenhouse Exempt from Decay

**Objective**: Verify Greenhouse tiles do not decay.

### Setup
1. Till soil in Greenhouse
2. Leave tiles bare

### Execution
1. Pass multiple days
2. Check health of Greenhouse tiles

### Expected Outcome
- Greenhouse tiles: No health decrease
- Farm tiles (if any bare): Health decreased normally
- Console log: Excludes Greenhouse from decay count

---

## Scenario 12: NPC Garden Compost Rejection

**Objective**: Verify compost cannot be applied outside farm/Greenhouse.

### Setup
1. Obtain compost
2. Visit a location with tillable soil (e.g., Railroad, Beach)

### Execution
1. Right-click on tilled soil in non-farm location

### Expected Outcome
- Compost not consumed
- No health change
- "Cancel" audio cue plays
- Console log: Compost rejected (invalid location)

---

## Debug Commands

For testing purposes, use SMAPI console commands:

```bash
# Set time to test day transitions
world_settime 0600

# Add compost to inventory
player_add LivingRoots.Compost 10

# Add organic waste
player_add Object_(o)178 10  # Wild Seeds
player_add Object_(o)24 10   # Parsnip

# Check soil health at tile
lr_soilhealth 10 20

# Force decay calculation
lr_decay_now
```

---

## Success Criteria Validation

| Success Criteria | Validated By Scenario |
|------------------|----------------------|
| SC-001: Decay rate × seasonal multiplier | Scenario 1 |
| SC-002: Compost restores 15 health | Scenario 4 |
| SC-003: Health bounds 0-100 | Scenarios 2, 5 |
| SC-004: Maturation increases output | Scenario 9 (extended) |
| SC-005: Save/load preserves state | Scenario 10 |
| SC-006: Day start within frame budget | Scenario 1 (performance) |
| SC-007: Visible feedback within 1 day | Scenarios 1, 4 |
