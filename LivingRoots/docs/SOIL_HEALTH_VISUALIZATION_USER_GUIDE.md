# Soil Health Visualization - User Guide

## Overview

The Soil Health Visualization feature provides visual feedback about the health of your farm's soil in the Living Roots mod. This system helps you make informed farming decisions by displaying soil health values through hover tooltips, color-coded tile overlays, and hoe action feedback.

## How It Works

The visualization system reads the persistent soil health values (0-100%) stored by the mod and displays them in three ways:

1. **Hover Tooltips**: Display detailed health information when you hover over tiles
2. **Tile Color Overlays**: Color-code tiles based on their health level
3. **Hoe Feedback**: Show visual feedback when using the hoe tool

## Understanding the Color Coding

Soil health is categorized into three levels, each with a distinct color:

### Poor Soil (0-33%)
- **Color**: Reddish-brown (#8B4513 - SaddleBrown)
- **Meaning**: Soil needs attention and care
- **Action**: Consider applying compost or allowing soil to rest

### Moderate Soil (34-66%)
- **Color**: Yellowish-brown (#DAA520 - GoldenRod)
- **Meaning**: Soil is in average condition
- **Action**: Maintain current practices or apply compost for improvement

### Healthy Soil (67-100%)
- **Color**: Greenish-brown (#556B2F - DarkOliveGreen)
- **Meaning**: Soil is in excellent condition
- **Action**: Continue sustainable practices to maintain health

## Using Hover Tooltips

### How to Use
1. Move your mouse cursor over any farm tile
2. A tooltip will appear showing:
   - Soil health percentage (0-100%)
   - Health status (Poor, Moderate, or Healthy)

### Tooltip Appearance
```
┌─────────────────────┐
│ Soil Health: 85%    │
│ Status: Healthy     │
└─────────────────────┘
```

### Tooltip Behavior
- Tooltips appear when hovering over tillable tiles with soil health data
- Tooltips follow your cursor movement
- Tooltips automatically hide when you move to a different tile or off-screen
- Tooltips are styled with a semi-transparent dark background and colored border

## Understanding Tile Color Overlays

### How Overlays Work
- Tiles are colored based on their soil health category
- Overlays are semi-transparent (default 40% opacity)
- Overlays appear on top of ground tiles but below objects
- Only tiles within your viewport are rendered (performance optimization)

### Visual Example
```
┌─────────────────────────────────┐
│  [Brown]  [Brown]  [Green]   │  ← Poor soil tiles
│  [Yellow] [Yellow] [Yellow]   │  ← Moderate soil tiles
│  [Green]  [Green]  [Green]    │  ← Healthy soil tiles
└─────────────────────────────────┘
```

### Overlay Benefits
- Quickly assess soil health across your farm at a glance
- Identify areas needing attention
- Track soil health improvements over time
- Plan crop placement based on soil conditions

## Using Hoe Feedback

### How It Works
When you use the hoe tool on a tile, you'll receive visual feedback about that tile's soil health:

1. **Visual Flash**: The tile flashes with its health category color for 200ms
2. **Floating Text**: The health value appears above the tile and floats upward

### Feedback Types
- **Flash Effect**: Brief color burst matching the health category
- **Floating Text**: Health percentage displayed above the tile
- **Color Match**: Feedback color matches the health category (Poor/Moderate/Healthy)

### Benefits
- Instant feedback when preparing soil for planting
- Helps prioritize which areas need compost
- Reinforces the connection between tools and soil health

## Configuration Options

The visualization system can be customized through the `config.json` file in your mod folder.

### Configuration File Location
```
Stardew Valley/Mods/LivingRoots/config.json
```

### Configuration Options

#### Master Switch
```json
"ShowTileOverlays": true
```
- **Type**: Boolean
- **Default**: `true`
- **Description**: Enable or disable all soil health visualization features

#### Tile Overlays
```json
"ShowTileOverlays": true
```
- **Type**: Boolean
- **Default**: `true`
- **Description**: Show color-coded overlays on tiles

#### Hover Tooltips
```json
"ShowHoverTooltips": true
```
- **Type**: Boolean
- **Default**: `true`
- **Description**: Show tooltips when hovering over tiles

#### Hoe Feedback
```json
"ShowHoeFeedback": true
```
- **Type**: Boolean
- **Default**: `true`
- **Description**: Show visual feedback when using the hoe tool

#### Overlay Opacity
```json
"OverlayOpacity": 0.3
```
- **Type**: Float
- **Range**: 0.0 to 1.0
- **Default**: `0.3` (30% opacity)
- **Description**: Transparency level of tile overlays
  - Lower values (0.1-0.3): More subtle, less intrusive
  - Higher values (0.5-0.8): More visible, but may obscure ground

#### Custom Colors
```json
"UseCustomColors": false
```
- **Type**: Boolean
- **Default**: `false`
- **Description**: Enable custom color scheme (requires color definitions below)

#### Poor Health Color
```json
"PoorHealthColor": {
  "R": 139,
  "G": 69,
  "B": 19,
  "A": 255
}
```
- **Type**: Color (RGBA)
- **Default**: Reddish-brown (139, 69, 19, 255)
- **Description**: Custom color for poor soil (0-33%)
- **Format**: RGB values (0-255), Alpha (0-255)

#### Moderate Health Color
```json
"ModerateHealthColor": {
  "R": 218,
  "G": 165,
  "B": 32,
  "A": 255
}
```
- **Type**: Color (RGBA)
- **Default**: Yellowish-brown (218, 165, 32, 255)
- **Description**: Custom color for moderate soil (34-66%)
- **Format**: RGB values (0-255), Alpha (0-255)

#### Healthy Health Color
```json
"HealthyHealthColor": {
  "R": 85,
  "G": 107,
  "B": 47,
  "A": 255
}
```
- **Type**: Color (RGBA)
- **Default**: Greenish-brown (85, 107, 47, 255)
- **Description**: Custom color for healthy soil (67-100%)
- **Format**: RGB values (0-255), Alpha (0-255)

### Complete Configuration Example

```json
{
  "$schema": "https://smapi.io/schemas/manifest.json",
  "ShowTileOverlays": true,
  "ShowHoverTooltips": true,
  "ShowHoeFeedback": true,
  "OverlayOpacity": 0.3,
  "UseCustomColors": false,
  "PoorHealthColor": {
    "R": 139,
    "G": 69,
    "B": 19,
    "A": 255
  },
  "ModerateHealthColor": {
    "R": 218,
    "G": 165,
    "B": 32,
    "A": 255
  },
  "HealthyHealthColor": {
    "R": 85,
    "G": 107,
    "B": 47,
    "A": 255
  }
}
```

## How to Enable/Disable Features

### Disabling All Visualization
Set the master switch to `false`:
```json
"ShowTileOverlays": false
```

### Disabling Specific Features
You can disable individual features while keeping others enabled:

**Tooltips only** (no overlays):
```json
"ShowTileOverlays": false,
"ShowHoverTooltips": true,
"ShowHoeFeedback": false
```

**Overlays only** (no tooltips):
```json
"ShowTileOverlays": true,
"ShowHoverTooltips": false,
"ShowHoeFeedback": false
```

**Hoe feedback only**:
```json
"ShowTileOverlays": false,
"ShowHoverTooltips": false,
"ShowHoeFeedback": true
```

### Applying Configuration Changes
1. Edit the `config.json` file
2. Save the file
3. The mod will automatically reload the configuration
4. Changes take effect immediately (no game restart required)

## Troubleshooting

### Issue: No visualization appears on tiles

**Possible Causes:**
1. Visualization is disabled in config
2. Soil health data doesn't exist for the tile
3. Tile is not tillable
4. Mod is not loaded correctly

**Solutions:**
1. Check `config.json` and ensure `ShowTileOverlays` is `true`
2. Verify the tile has soil health data (use hoe to check)
3. Ensure you're hovering over tillable farm tiles
4. Check SMAPI console for error messages

### Issue: Tooltips don't appear

**Possible Causes:**
1. Tooltips are disabled in config
2. Cursor is not over a valid tile
3. Tile has no soil health data

**Solutions:**
1. Check `config.json` and ensure `ShowHoverTooltips` is `true`
2. Move cursor to a different tile
3. Use hoe on the tile to check if it has health data

### Issue: Hoe feedback doesn't appear

**Possible Causes:**
1. Hoe feedback is disabled in config
2. Not using the hoe tool
3. Tile has no soil health data

**Solutions:**
1. Check `config.json` and ensure `ShowHoeFeedback` is `true`
2. Ensure you have the hoe tool selected
3. Try a different tile that has soil health data

### Issue: Colors look wrong or don't match expectations

**Possible Causes:**
1. Custom colors are enabled but not properly configured
2. Opacity is too high or too low
3. Graphics settings are affecting rendering

**Solutions:**
1. Check `UseCustomColors` setting in config
2. Adjust `OverlayOpacity` (try 0.2-0.4 for better visibility)
3. Ensure your graphics drivers are up to date

### Issue: Performance drops with many overlays

**Possible Causes:**
1. Too many tiles visible on screen
2. Overlay opacity is too high
3. System performance limitations

**Solutions:**
1. Reduce `OverlayOpacity` to 0.2 or lower
2. Disable tile overlays and use tooltips only
3. Zoom in to reduce number of visible tiles
4. Close other resource-intensive applications

### Issue: Configuration changes don't take effect

**Possible Causes:**
1. Config file has syntax errors
2. Config file is not saved
3. Mod is not reloading config

**Solutions:**
1. Validate JSON syntax using a JSON validator
2. Ensure the file is saved
3. Check SMAPI console for config loading errors
4. Restart the game if changes still don't apply

## Best Practices

### Using Visualization Effectively

1. **Start with Tooltips Only**: If you're new to the feature, enable tooltips but disable overlays to avoid visual clutter
2. **Gradual Overlay Enablement**: Enable overlays once you're comfortable with the tooltip system
3. **Adjust Opacity**: Find an opacity level that works for your visual preferences (0.2-0.4 recommended)
4. **Regular Monitoring**: Check soil health periodically to track changes over time
5. **Strategic Planning**: Use visualization to plan crop placement and compost application

### Performance Tips

1. **Disable on Large Farms**: If you have a very large farm, consider using tooltips only
2. **Lower Opacity**: Reducing opacity can improve rendering performance
3. **Zoom In**: Zooming in reduces the number of tiles rendered
4. **Selective Enablement**: Disable features you don't use regularly

### Color Customization

1. **Choose Accessible Colors**: Ensure colors are distinguishable for your vision
2. **Test in Different Lighting**: Check visibility in different game lighting conditions
3. **Consider Color Blindness**: Use colors that work for different types of color blindness
4. **Keep Contrast**: Ensure overlays are visible against different ground textures

## FAQ

### Q: Does visualization affect gameplay mechanics?
**A:** No, visualization only displays information. It doesn't change how soil health works or how crops grow.

### Q: Can I use visualization with other mods?
**A:** Yes, the visualization system is designed to work alongside other mods. However, some mods that modify rendering may affect how overlays appear.

### Q: Will visualization save with my game?
**A:** No, visualization settings are in the config file, not your save file. Soil health values are saved, but visualization preferences are not.

### Q: Can I share my configuration with friends?
**A:** Yes, you can copy your `config.json` file and share it. Just make sure they back up their config first.

### Q: What happens if I delete the config file?
**A:** The mod will recreate it with default values the next time you launch the game.

### Q: Can I use different colors for different seasons?
**A:** Currently, colors are global and apply to all seasons. This feature may be added in future updates.

### Q: Does visualization work in all locations?
**A:** Visualization works on tillable tiles in any location where soil health data exists (primarily your farm).

### Q: How accurate is the health percentage?
**A:** The health percentage is calculated from the underlying soil health value stored by the mod. It's accurate to the data stored.

### Q: Can I export soil health data?
**A:** Not directly through the visualization system. However, you can access the data through the mod's save file structure.

### Q: Will visualization affect my FPS?
**A:** It may have a small impact, especially with many overlays. Use the configuration options to optimize performance if needed.

## Getting Help

If you encounter issues not covered in this guide:

1. **Check the SMAPI Console**: Look for error messages or warnings
2. **Review Configuration**: Ensure your `config.json` is valid
3. **Consult Developer Guide**: See [Developer Guide](./SOIL_HEALTH_VISUALIZATION_DEVELOPER_GUIDE.md) for technical details
4. **Report Issues**: Report bugs on the [GitHub Issues](https://github.com/lfgranja/stardew-raizes-vivas/issues) page
5. **Join the Community**: Ask questions in the mod's discussion forums

## Related Documentation

- [Implementation Guide](./IMPLEMENTATION_GUIDE_US-01-02.md) - Technical implementation details
- [Developer Guide](./SOIL_HEALTH_VISUALIZATION_DEVELOPER_GUIDE.md) - For mod developers
- [Feature Summary](./FEATURE_SUMMARY_US-01-02.md) - Feature overview and status
- [Epic and User Stories](./EPICS_USER_STORIES.md) - Project planning documents

## Version History

- **v1.0.0** - Initial release of soil health visualization
  - Hover tooltips
  - Tile color overlays
  - Hoe action feedback
  - Customizable configuration
