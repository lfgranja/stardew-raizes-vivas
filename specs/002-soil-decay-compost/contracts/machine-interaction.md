# Machine Interaction Contract: Composting Bin

**Feature Branch**: `002-soil-decay-compost` | **Date**: 2026-09-06

## Overview

This contract defines the interaction protocol between the player and the Composting Bin machine. It follows Stardew Valley's standard machine interaction conventions (keg/preserves jar pattern).

## Input: Adding Organic Waste

### Preconditions
- Composting bin is in `Empty` state
- Player is holding an item classified as valid organic waste
- Player is within interaction range of the bin

### Valid Organic Waste Categories
| Category ID | Category Name |
|-------------|---------------|
| -74 | Seeds |
| -75 | Vegetables |
| -79 | Fruits |
| -80 | Flowers |
| -81 | Forage/Greens |

**OR** item has `compostable_item` context tag

**AND** item does NOT have `not_compostable` context tag

### Interaction Sequence
1. Player equips organic waste item in active slot
2. Player right-clicks on composting bin
3. System validates:
   - Bin is empty
   - Item is valid organic waste
   - Player is on farm or Greenhouse
4. On success:
   - Input item consumed from inventory
   - Bin state → `Processing`
   - `InputTimestamp` set to current game time (minutes)
   - `InputItemId` recorded for drop-on-break
   - Subtle "add" sound played

### Failure Modes
| Condition | Response |
|-----------|----------|
| Bin not empty | No action, no sound |
| Invalid item | No action, no sound |
| Player not on farm/Greenhouse | No action, no sound |
| Bin in `Processing` state | No action, no sound |
| Bin in `Ready` state | No action, no sound |

---

## Output: Collecting Compost

### Preconditions
- Composting bin is in `Ready` state
- Player is within interaction range of the bin
- Player has inventory space for output

### Interaction Sequence
1. Player right-clicks on composting bin (with empty hands or any item)
2. System validates bin is in `Ready` state
3. On success:
   - Output quantity calculated: `1 × MaturationLevel` compost items
   - Compost items added to player inventory
   - Bin state → `Empty`
   - `InputTimestamp` cleared
   - `InputItemId` cleared
   - `ConsecutiveIdleDays` reset to 0
   - Distinct "ready" chime played
   - Maturation level incremented (if below 5 and continuous operation)

### Failure Modes
| Condition | Response |
|-----------|----------|
| Bin in `Empty` state | No action |
| Bin in `Processing` state | No action, subtle "not ready" feedback |
| Inventory full | No action, compost remains in bin |

---

## Processing State

### Duration
- 2 full in-game days (2880 minutes)
- Calculated from `InputTimestamp` to current game time

### Visual Feedback
- Machine shows input sprite while processing
- Intermittent ambient sound every 2-3 in-game hours (subtle bubbling)
- No hover tooltip change (still shows "Maturity: Xx")

### Mid-Processing Interactions
- Right-click with waste: Rejected (bin not empty)
- Right-click with empty hands: No action (not ready yet)
- Break bin: Drops input item on ground, maturation lost

---

## Ready State

### Visual Feedback
- Machine switches to "ready" visual (bubbling animation or output sprite)
- Distinct "ready" chime plays once when state transitions to Ready
- Hover tooltip shows "Maturity: Xx" (standard machine hover)

### Collection
- Right-click with empty hands collects compost
- Compost quantity = current maturation level (1-5)

---

## Breaking/Removing the Bin

### Interaction
- Hit with pickaxe or axe to break
- Or use "Pick Up" action if supported

### Result
- If in `Processing` state: Input item dropped on ground
- If in `Ready` state: Output compost items dropped on ground
- Maturation level lost (machine is gone)
- No sound (standard machine break sound)

---

## Maturation System

### Progression
| Week of Continuous Operation | Maturation Level | Output Multiplier |
|-------------------------------|------------------|-------------------|
| Week 1 | 1 | 1:1 |
| Week 2 | 2 | 2:1 |
| Week 3 | 3 | 3:1 |
| Week 4 | 4 | 4:1 |
| Week 5+ | 5 | 5:1 (max) |

### Reset Condition
- 14 consecutive days of being empty (no input, no output)
- Resets to Level 1

### Tracking
- `ConsecutiveIdleDays` incremented each day if bin is empty
- Reset to 0 when waste is added or compost is collected
- Persisted through save/load

---

## Save/Load Contract

### Save Data
```json
{
  "State": "Processing|Ready|Empty",
  "InputItemId": "QualifiedItemId or null",
  "InputTimestamp": 1234,
  "MaturationLevel": 3,
  "ConsecutiveIdleDays": 0
}
```

### Load Behavior
- Deserialize state from JSON
- If `State == Processing`:
  - Calculate elapsed time: `currentTime - InputTimestamp`
  - If elapsed ≥ 2880 minutes: Transition to `Ready` (play chime)
  - Else: Continue processing (no chime)
- If `State == Ready`: Remain ready (no time check needed)
- If `State == Empty`: Remain empty

---

## Error Handling

| Error | Response |
|-------|----------|
| Invalid save data | Reset bin to Empty state |
| Missing fields | Use defaults (Empty, Level 1, 0 idle days) |
| Corrupted timestamp | Reset to Empty state |
| Negative maturation | Clamp to 1 |
| Excessive maturation | Clamp to 5 |
