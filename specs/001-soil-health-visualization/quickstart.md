# Quickstart Validation: Soil Health Visualization

**Feature**: Soil Health Visualization
**Date**: 2026-09-05

## Prerequisites

1. Living Roots mod built and installed in Stardew ValleyMods folder
2. SMAPI installed and configured
3. Existing save game with tilled soil tiles
4. Soil health data present (from US-01-01 implementation)

## Setup

1. Build the mod: `dotnet build Stardew-LivingRoots.sln`
2. Run tests: `dotnet test`
3. Launch Stardew Valley via SMAPI
4. Load a save with tilled soil tiles

## Validation Scenarios

### Scenario 1: Overlay Rendering on Tilled Tiles

**Purpose**: Verify color-coded overlays render correctly for different health values

**Steps**:
1. Till multiple soil tiles in a row
2. Set soil health values: tile1 = 15 (Poor), tile2 = 50 (Moderate), tile3 = 85 (Healthy)
3. View the farm area with the tiles visible
4. Observe overlay colors

**Expected Results**:
- tile1 (15): Red overlay with stripes pattern
- tile2 (50): Yellow overlay with dots pattern
- tile3 (85): Green overlay with solid fill pattern

**Verification**: Overlays match health categories per spec acceptance criteria

---

### Scenario 2: Hover Tooltip Display

**Purpose**: Verify tooltip shows correct health percentage and category

**Steps**:
1. Set a tile's health to 75 (Healthy)
2. Move cursor over the tile
3. Observe tooltip text
4. Move cursor away
5. Verify tooltip disappears

**Expected Results**:
- Tooltip displays: "Soil Health: 75% (Healthy)"
- Tooltip appears within 100ms of hover
- Tooltip disappears when cursor leaves tile

**Verification**: Text format matches spec, timing meets SC-004

---

### Scenario 3: Hoe Action Feedback

**Purpose**: Verify flash effect and floating text on hoe usage

**Steps**:
1. Set a tile's health to 42 (Moderate)
2. Equip hoe tool
3. Use hoe on the tile (action button)
4. Observe visual feedback

**Expected Results**:
- Flash effect appears on targeted tile for 300ms
- Floating text displays health status for 1000ms
- No feedback on adjacent tiles
- Feedback appears only on targeted tile

**Verification**: Durations match spec, tile targeting accurate per SC-005

---

### Scenario 4: Configuration Persistence

**Purpose**: Verify configuration loads, saves, and falls back to defaults

**Steps**:
1. Exit game
2. Locate visualization_config in save data
3. Modify opacity to 0.75
4. Relaunch game and load save
5. Verify overlay opacity changed
6. Exit and corrupt a color value
7. Relaunch and verify fallback to default

**Expected Results**:
- Valid changes persist across save/load
- Invalid values replaced with defaults (per FR-006)
- Valid values preserved when others are invalid

**Verification**: Configuration validation works per spec acceptance criteria

---

### Scenario 5: Feature Toggles

**Purpose**: Verify independent enabling/disabling of overlay, tooltip, and feedback

**Steps**:
1. Disable overlays in configuration
2. Verify no overlays render
3. Enable overlays, disable tooltips
4. Hover over tile, verify no tooltip
5. Disable hoe feedback
6. Use hoe, verify no feedback

**Expected Results**:
- Each feature toggles independently
- No side effects between features
- Configuration takes effect immediately

**Verification**: Feature toggles work per FR-007

---

### Scenario 6: Performance with Many Tiles

**Purpose**: Verify 60 FPS maintained with 1,000 visible tiles

**Steps**:
1. Create 1,000 tilled soil tiles (or use existing large farm)
2. Enable all visualization features
3. Move camera to view all tiles
4. Monitor frame rate (use SMAPI's built-in FPS counter or external tool)

**Expected Results**:
- Frame rate remains at 60 FPS (16.67ms per frame)
- No visible stuttering or lag
- All tiles render correct overlays

**Verification**: Performance meets SC-002

---

### Scenario 7: Color Interpolation

**Purpose**: Verify smooth color transitions between categories

**Steps**:
1. Create tiles with health values: 16, 33, 34, 50, 66, 67, 83
2. Observe overlay colors
3. Verify smooth transitions at boundaries

**Expected Results**:
- Values within category show interpolated colors
- Boundary values (33, 34, 66, 67) show correct category colors
- No abrupt color jumps at boundaries

**Verification**: Interpolation follows FR-008 linear RGB specification

---

### Scenario 8: Unknown Data Handling

**Purpose**: Verify neutral gray overlay for unavailable data

**Steps**:
1. Find a tilled tile with no soil health data (newly tilled, never modified)
2. Observe overlay color

**Expected Results**:
- Tile renders neutral gray (#808080) overlay
- Indicates "unknown" status per FR-004

**Verification**: Unknown data handled per FR-014

---

### Scenario 9: Event Handler Cleanup

**Purpose**: Verify no memory leaks after mod disposal

**Steps**:
1. Load save with visualization active
2. Exit game (triggers mod disposal)
3. Check SMAPI log for disposal messages
4. Reload game (triggers mod reinitialization)

**Expected Results**:
- No errors in SMAPI log
- Event handlers properly unregistered
- No duplicate handlers after reload

**Verification**: Zero event handler leaks per SC-006

---

### Scenario 10: Configuration Defaults

**Purpose**: Verify default configuration created when none exists

**Steps**:
1. Delete or rename visualization_config from save data
2. Launch game and load save
3. Verify overlays render with default settings

**Expected Results**:
- Default configuration created automatically
- Overlays render with default colors (red/yellow/green)
- Default opacity (0.5) applied
- All features enabled by default

**Verification**: Default configuration works per acceptance scenario 5 in US-4

---

## Test Commands

```bash
# Run all visualization tests
dotnet test --filter "FullyQualifiedName~Visualization"

# Run specific test class
dotnet test --filter "FullyQualifiedName~VisualizationServiceTests"

# Run with performance timing
dotnet test --logger "console;verbosity=detailed"
```

## Troubleshooting

| Issue | Possible Cause | Solution |
|-------|----------------|----------|
| No overlays visible | Overlays disabled in config | Check OverlaysEnabled setting |
| Wrong colors | Configuration not reloaded | Exit and reload game |
| Tooltips not showing | Cursor not over tilled tile | Ensure tile is tilled and has health data |
| Performance issues | Too many tiles visible | Verify viewport culling is working |
| Config not saving | Invalid saveId | Check SMAPI log for warnings |
