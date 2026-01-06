# In-Game Testing Guide - US01-01 and US01-02

## 1. Introduction

This guide provides complete and detailed instructions for in-game testing of the functionalities implemented in User Stories US01-01 (Soil Health Persistence) and US01-02 (Soil Health Visualization) of the LivingRoots mod for Stardew Valley.

### What is being tested

-   **US01-01 - Soil Health Persistence**: System for storing and retrieving soil health data between game sessions, including automatic loading, automatic saving, in-memory cache, and security protections.

-   **US01-02 - Soil Health Visualization**: System for visual display of soil health through tooltips, colored overlays, visual feedback when using the Hoe, and customizable visualization settings.

### Testing Objective

Ensure that all functionalities work correctly under normal conditions, edge cases, performance situations, and security scenarios, providing a fluid and reliable gaming experience.

---

## 2. Prerequisites

### 2.1. Required Software

-   **Stardew Valley** (version 1.6.0 or higher)
-   **SMAPI** (Stardew Modding API) - version compatible with the mod
-   **LivingRoots Mod** - version with US01-01 and US01-02 implemented
-   **Content Patcher** (if necessary for dependent mods)

### 2.2. Testing Environment

-   A clean or dedicated game save for testing
-   Access to the SMAPI command console (press `F2` by default)
-   Basic game tools: Hoe, watering can, seeds
-   Multiple accessible locations: Farm, Mine, Town, etc.

### 2.3. Test Save Preparation

**Recommendations:**

-   Create a new save specifically for testing
-   Unlock various locations (Farm, Mine, Town, Desert)
-   Have access to basic tools
-   Have some tilled and untilled tiles for comparison

**Preparation Steps:**

1. Start a new game save
2. Play until you unlock the Hoe (available from the beginning)
3. Till some tiles on the Farm (about 20-30 tiles)
4. Explore other locations (Town, Mine, Desert)
5. Save the game
6. Note the date/time of the save for reference

### 2.4. Mod Configuration

Verify that the mod configuration file is properly set up:

```json
{
    "EnableVisualization": true,
    "OverlayOpacity": 0.5,
    "TooltipEnabled": true,
    "EnableHoeFeedback": true
}
```

### 2.5. Console Access

-   Press `F2` to open the SMAPI console
-   Verify that the LivingRoots mod is loaded correctly
-   Test the `lr_version` command to confirm

---

## 3. Available Commands

### 3.1. lr_version

Displays information about the mod version and its UniqueID.

**Syntax:**

```
lr_version
```

**Parameters:**

-   None

**Usage Example:**

```
lr_version
```

**Expected Output:**

```
[LivingRoots] LivingRoots v1.0.0 (UniqueID: LivingRoots.Mod)
```

**How to use for testing:**

-   Verify that the mod is loaded correctly
-   Confirm the version before starting tests
-   Use to document which version was tested

**Quick Test:**

1. Open the console with `F2`
2. Type `lr_version`
3. Verify that the version and UniqueID appear correctly
4. Note the version for bug reports

---

## 4. Persistence Tests (US01-01)

### 4.1. Automatic Data Loading

#### Test 4.1.1: Initial Loading

**Objective:** Verify that soil health data is loaded correctly when starting the game.

**Steps:**

1. Start Stardew Valley with the LivingRoots mod installed
2. Load an existing save that has soil health data
3. Open the console (`F2`) and verify there are no loading errors
4. Navigate to the Farm
5. Hover over tilled tiles

**Expected Result:**

-   The game loads without errors
-   Soil health data is loaded automatically
-   Console shows successful loading messages
-   Tooltips show correct health values

**How to Verify:**

-   Observe the console for messages like `[LivingRoots] Loaded X soil health records`
-   Check tooltips on known tiles
-   Compare with previous values (if documented)

---

#### Test 4.1.2: Loading after External Changes

**Objective:** Test if the mod detects and loads changes made externally to data files.

**Steps:**

1. Load a save and note the health of some specific tiles
2. Save the game and close completely
3. Manually open the mod's data file (located in the save folder)
4. Modify some health values (keeping within the 0-100 range)
5. Save the file
6. Restart Stardew Valley and load the save
7. Check the modified tiles

**Expected Result:**

-   The mod loads the modified values
-   Tooltips reflect the new values
-   Console shows successful loading

**How to Verify:**

-   Compare the values in tooltips with the manually modified ones
-   Verify there are no errors in the console

---

### 4.2. Automatic Saving

#### Test 4.2.1: Saving when Saving Game

**Objective:** Verify that soil health data is saved automatically before the game saves.

**Steps:**

1. Load a test save
2. Use the Hoe on some tiles to modify soil health
3. Note the health values of the modified tiles
4. Save the game (menu or shortcut)
5. Close the game completely
6. Restart and load the save
7. Check the same tiles

**Expected Result:**

-   Health values are preserved after reloading
-   Console shows saving message before the game save
-   No data loss

**How to Verify:**

-   Compare values before and after reloading
-   Check the console for saving messages
-   Confirm that values are identical

---

#### Test 4.2.2: Continuous Automatic Saving

**Objective:** Test if saving occurs correctly in multiple save/load cycles.

**Steps:**

1. Load a save
2. Modify the health of 10 different tiles
3. Save the game
4. Restart and load
5. Modify 10 more tiles
6. Save again
7. Repeat the process 5 times
8. Check all modified tiles

**Expected Result:**

-   All tiles maintain their correct values
-   No data corruption
-   Console shows successful saves in each cycle

**How to Verify:**

-   Document the values of each tile in each cycle
-   Confirm there is no loss or corruption
-   Check console logs

---

### 4.3. In-Memory Cache

#### Test 4.3.1: Performance with Cache

**Objective:** Verify if the in-memory cache improves performance when accessing data frequently.

**Steps:**

1. Load a save with many soil health tiles (500+ tiles)
2. Move quickly between tiles, hovering over them
3. Observe the fluidity of tooltips
4. Repeat the movement 10 times
5. Note if there is lag or noticeable delays

**Expected Result:**

-   Tooltips appear instantly
-   No noticeable lag
-   Data access is fast and fluid

**How to Verify:**

-   Time the tooltip response time
-   Observe if there are frame drops
-   Compare with accesses to uncached tiles (first access)

---

#### Test 4.3.2: Cache Invalidation

**Objective:** Test if the cache is invalidated correctly when data is modified.

**Steps:**

1. Load a save
2. Hover over a tile and note the displayed value
3. Use the Hoe on the tile to modify health
4. Hover over the same tile again
5. Verify if the value updated

**Expected Result:**

-   Value in tooltip updates immediately after modification
-   Cache does not retain obsolete values
-   Modifications are reflected instantly

**How to Verify:**

-   Compare the value before and after modification
-   Confirm that the tooltip shows the new value
-   Verify there is no delay in the update

---

### 4.4. Coordinate and Value Validation

#### Test 4.4.1: Valid Values (0-100)

**Objective:** Verify if the system accepts and stores values correctly within the allowed range.

**Steps:**

1. Load a save
2. Use the Hoe on tiles to generate different health values
3. Observe tooltips to confirm values
4. Save and reload
5. Verify if values remain in the 0-100 range

**Expected Result:**

-   All displayed values are between 0 and 100
-   Values are preserved correctly after save/load
-   No negative values or values above 100

**How to Verify:**

-   Document the values of various tiles
-   Confirm that all are in the correct range
-   Check after reloading

---

#### Test 4.4.2: Valid Coordinates

**Objective:** Test if the system validates and stores coordinates correctly.

**Steps:**

1. Load a save
2. Navigate to different areas of the Farm (extreme corners)
3. Use the Hoe on tiles at extreme coordinates
4. Observe tooltips
5. Save and reload
6. Check the same tiles

**Expected Result:**

-   Coordinates are stored correctly
-   Tooltips show correct values even at extreme positions
-   No coordinate errors

**How to Verify:**

-   Note the approximate coordinates of tested tiles
-   Confirm that values are maintained after reloading
-   Verify there are no errors in the console

---

### 4.5. DoS Attack Protection

#### Test 4.5.1: Tile Limit per Location (MaxTilesPerLocation: 500)

**Objective:** Verify if the system respects the limit of 500 tiles per location.

**Steps:**

1. Load a save
2. Use the Hoe on more than 500 tiles in the same location (Farm)
3. Observe the behavior after reaching the limit
4. Check the console for limit messages

**Expected Result:**

-   System accepts up to 500 tiles per location
-   After reaching 500, new tiles are not registered or replace the oldest ones
-   Console shows warning message about the limit

**How to Verify:**

-   Count how many tiles were registered
-   Check messages in the console
-   Confirm that the system does not crash

---

#### Test 4.5.2: Location Limit per Save (MaxLocationsPerSave: 50)

**Objective:** Test if the system respects the limit of 50 different locations per save.

**Steps:**

1. Load a save with access to many locations
2. Use the Hoe on tiles in more than 50 different locations
3. Observe the behavior after reaching the limit
4. Check the console for messages

**Expected Result:**

-   System accepts up to 50 locations
-   After reaching 50, new locations are not registered
-   Console shows warning message

**How to Verify:**

-   Document how many locations were registered
-   Check messages in the console
-   Confirm that the system does not crash

---

#### Test 4.5.3: Total Tile Limit (MaxTilesPerSave: 30,000)

**Objective:** Verify if the system respects the total limit of 30,000 tiles per save.

**Steps:**

1. Load a save
2. Use the Hoe on tiles until reaching 30,000 total tiles
3. Observe the behavior after reaching the limit
4. Check the console for messages

**Expected Result:**

-   System accepts up to 30,000 total tiles
-   After reaching the limit, new tiles are not registered
-   Console shows warning message

**How to Verify:**

-   Monitor the total number of registered tiles
-   Check messages in the console
-   Confirm that the system does not crash

---

#### Test 4.5.4: Extreme Coordinates (MaxAbsoluteTileCoordinate: ±10,000)

**Objective:** Test if the system rejects coordinates outside the allowed range.

**Steps:**

1. Load a save
2. Navigate to extreme map areas (if accessible)
3. Try to use the Hoe on tiles with coordinates beyond ±10,000
4. Observe the behavior
5. Check the console for error messages

**Expected Result:**

-   System rejects coordinates outside the range
-   Console shows error message
-   Tile is not registered
-   System continues to function normally

**How to Verify:**

-   Check error messages in the console
-   Confirm that tiles with invalid coordinates are not registered
-   Observe if the system does not crash

---

### 4.6. Filename Sanitization

#### Test 4.6.1: Save Names with Special Characters

**Objective:** Verify if the system correctly sanitizes save names with special characters.

**Steps:**

1. Create a new save with name containing special characters (e.g., "Test@#$%")
2. Play and use the Hoe on some tiles
3. Save the game
4. Verify if the mod data file was created with sanitized name
5. Reload the save
6. Verify if data was loaded correctly

**Expected Result:**

-   Data file is created with sanitized name (without dangerous special characters)
-   Data is loaded correctly
-   No file errors

**How to Verify:**

-   Check the save folder in the file system
-   Confirm that the file name is sanitized
-   Verify if reloading works

---

#### Test 4.6.2: Save Names with Spaces and Unicode

**Objective:** Test if the system correctly handles spaces and Unicode characters.

**Steps:**

1. Create a save with name containing spaces and Unicode (e.g., "Fazenda da João Ñ")
2. Play and use the Hoe on tiles
3. Save the game
4. Check the data file
5. Reload the save

**Expected Result:**

-   Data file is created with appropriate name
-   Data is loaded correctly
-   No encoding problems

**How to Verify:**

-   Check the save folder
-   Confirm that the file exists and can be read
-   Verify reloading

---

#### Test 4.6.3: Name Size Limit (MaxSaveIdLength: 200)

**Objective:** Verify if the system handles very long save names.

**Steps:**

1. Create a save with a very long name (more than 200 characters)
2. Play and use the Hoe on tiles
3. Save the game
4. Check the data file
5. Reload the save

**Expected Result:**

-   System truncates or sanitizes the name if necessary
-   Data file is created with valid name
-   Data is loaded correctly

**How to Verify:**

-   Check the name of the created file
-   Confirm that it is within file system limits
-   Verify reloading

---

#### Test 4.6.4: Location Name Size Limit (MaxLocationNameLength: 100)

**Objective:** Test if the system handles very long location names.

**Steps:**

1. Load a save
2. Navigate to a location (if possible with a long name)
3. Use the Hoe on tiles
4. Save the game
5. Check the data file
6. Reload the save

**Expected Result:**

-   System sanitizes or truncates long location names
-   Data is saved and loaded correctly
-   No errors

**How to Verify:**

-   Check the content of the data file
-   Confirm that location names are valid
-   Verify reloading

---

## 5. Visualization Tests (US01-02)

### 5.1. Hover Tooltips

#### Test 5.1.1: Basic Tooltip Display

**Objective:** Verify if tooltips are displayed correctly when hovering over tiles.

**Steps:**

1. Load a save
2. Navigate to the Farm
3. Hover over tiles with soil health
4. Observe the displayed tooltip

**Expected Result:**

-   Tooltip appears when hovering
-   Shows soil health value (0-100)
-   Shows status (Poor/Moderate/Healthy)
-   Tooltip disappears when removing mouse
-   Tooltip position follows the cursor

**How to Verify:**

-   Observe if the tooltip appears instantly
-   Verify if the information is correct
-   Confirm that the tooltip does not obstruct vision
-   Test at different cursor positions

---

#### Test 5.1.2: Tooltip Format

**Objective:** Verify if the tooltip displays information in the correct format.

**Steps:**

1. Load a save
2. Hover over tiles with different health levels
3. Observe the tooltip format for each case

**Expected Result:**

-   Format: "Soil Health: XX (Status)"
-   Examples:
    -   "Soil Health: 25 (Poor)"
    -   "Soil Health: 50 (Moderate)"
    -   "Soil Health: 85 (Healthy)"
-   Numbers are displayed with appropriate precision
-   Status corresponds to the health range

**How to Verify:**

-   Document the displayed format
-   Confirm that it matches the expected format
-   Verify consistency between different tiles

---

#### Test 5.1.3: Tooltip on Tiles without Health

**Objective:** Verify the behavior of the tooltip on tiles that do not have registered soil health.

**Steps:**

1. Load a save
2. Hover over never-tilled tiles
3. Observe if tooltip is displayed

**Expected Result:**

-   Tooltip is not displayed for tiles without health
-   Or tooltip shows "No health data"
-   No errors or crashes

**How to Verify:**

-   Observe if there is tooltip on untilled tiles
-   Verify if there are error messages in the console
-   Confirm that behavior is consistent

---

#### Test 5.1.4: Tooltip Performance

**Objective:** Verify if tooltips do not cause lag when displayed quickly.

**Steps:**

1. Load a save with many health tiles
2. Move the mouse quickly over many tiles
3. Observe if there is lag or delay
4. Repeat the movement 10 times

**Expected Result:**

-   Tooltips appear and disappear smoothly
-   No noticeable lag
-   Frame rate remains stable
-   No delay in update

**How to Verify:**

-   Observe the display fluidity
-   Monitor frame rate (if possible)
-   Verify if there is stuttering

---

### 5.2. Tile Color Overlays

#### Test 5.2.1: Health Colors - Poor Soil (0-33%)

**Objective:** Verify if tiles with poor health (0-33%) are displayed with the correct color.

**Steps:**

1. Load a save
2. Identify tiles with health between 0-33%
3. Observe the overlay color
4. Compare with expected color: Reddish brown (#8B4513)

**Expected Result:**

-   Tiles with health 0-33% have reddish brown overlay
-   Color is clearly distinguishable from other categories
-   Opacity is appropriate (does not completely obscure the tile)

**How to Verify:**

-   Compare the visual color with the expected color
-   Use color capture tools if available
-   Confirm that color is consistent between poor tiles

---

#### Test 5.2.2: Health Colors - Moderate Soil (34-66%)

**Objective:** Verify if tiles with moderate health (34-66%) are displayed with the correct color.

**Steps:**

1. Load a save
2. Identify tiles with health between 34-66%
3. Observe the overlay color
4. Compare with expected color: Yellowish brown (#DAA520)

**Expected Result:**

-   Tiles with health 34-66% have yellowish brown overlay
-   Color is clearly distinguishable from other categories
-   Transition between poor and moderate is clear

**How to Verify:**

-   Compare the visual color with the expected color
-   Confirm that color is consistent
-   Verify visual distinction

---

#### Test 5.2.3: Health Colors - Healthy Soil (67-100%)

**Objective:** Verify if tiles with healthy health (67-100%) are displayed with the correct color.

**Steps:**

1. Load a save
2. Identify tiles with health between 67-100%
3. Observe the overlay color
4. Compare with expected color: Greenish brown (#556B2F)

**Expected Result:**

-   Tiles with health 67-100% have greenish brown overlay
-   Color is clearly distinguishable from other categories
-   Transition between moderate and healthy is clear

**How to Verify:**

-   Compare the visual color with the expected color
-   Confirm that color is consistent
-   Verify visual distinction

---

#### Test 5.2.4: Color Transitions

**Objective:** Verify if transitions between color categories are clear and intuitive.

**Steps:**

1. Load a save
2. Find adjacent tiles with different health categories
3. Observe the visual difference between them
4. Verify if it's easy to distinguish categories

**Expected Result:**

-   Visual difference between categories is clear
-   No confusion between adjacent colors
-   Visual progression (poor → moderate → healthy) is intuitive

**How to Verify:**

-   Compare adjacent tiles of different categories
-   Ask another person to visually identify the categories
-   Confirm that colors are distinguishable

---

#### Test 5.2.5: Overlay Opacity

**Objective:** Verify if the overlay opacity allows seeing the underlying tile.

**Steps:**

1. Load a save
2. Observe tiles with color overlays
3. Verify if it's possible to see the original tile (grass, soil, etc.)
4. Test under different lighting conditions

**Expected Result:**

-   Overlay is transparent enough to see the tile
-   Health color is clearly visible
-   No complete obstruction of the tile

**How to Verify:**

-   Confirm that you can identify the tile type
-   Verify if health color is visible
-   Test in different areas of the map

---

### 5.3. Visual Feedback When Using Hoe

#### Test 5.3.1: Visual Flash

**Objective:** Verify if there is a visual flash when using the Hoe on a tile.

**Steps:**

1. Load a save
2. Use the Hoe on a tile
3. Observe if there is a visual flash
4. Repeat on several tiles

**Expected Result:**

-   Visual flash occurs when using the Hoe
-   Flash is visible but not obstructive
-   Flash color corresponds to the new soil health
-   Flash duration is appropriate

**How to Verify:**

-   Observe if there is flash
-   Check the flash color
-   Confirm that it's not too long or too short

---

#### Test 5.3.2: Floating Text

**Objective:** Verify if floating text appears showing the new soil health.

**Steps:**

1. Load a save
2. Use the Hoe on a tile
3. Observe if floating text appears
4. Read the displayed text

**Expected Result:**

-   Floating text appears above the tile
-   Shows the new health value
-   Text is readable and visible
-   Text disappears after an appropriate time

**How to Verify:**

-   Confirm that the text appears
-   Verify if the value is correct
-   Observe if the text is readable
-   Confirm that it disappears appropriately

---

#### Test 5.3.3: Flash + Text Combination

**Objective:** Verify if flash and floating text work well together.

**Steps:**

1. Load a save
2. Use the Hoe on a tile
3. Observe the flash and text simultaneously
4. Verify if both are visible and do not overlap

**Expected Result:**

-   Flash and text appear simultaneously
-   Both are clearly visible
-   No visual conflict
-   Feedback is clear and informative

**How to Verify:**

-   Observe if both elements appear
-   Verify if there is no problematic overlap
-   Confirm that feedback is clear

---

#### Test 5.3.4: Feedback in Different Conditions

**Objective:** Test visual feedback under different initial health conditions.

**Steps:**

1. Load a save
2. Use the Hoe on tiles with different initial health levels
3. Observe feedback in each case
4. Verify if feedback is consistent

**Expected Result:**

-   Feedback occurs regardless of initial health
-   Flash reflects the new health
-   Text shows the new value
-   Behavior is consistent

**How to Verify:**

-   Test on poor, moderate, and healthy tiles
-   Document feedback in each case
-   Confirm consistency

---

### 5.4. Visualization Configuration

#### Test 5.4.1: Enable/Disable Visualization

**Objective:** Verify if it's possible to enable and disable visualization.

**Steps:**

1. Open the mod configuration file
2. Change `EnableVisualization` to `false`
3. Save the configuration
4. Restart the game
5. Load a save
6. Observe if overlays and tooltips appear
7. Repeat enabling visualization again

**Expected Result:**

-   With `EnableVisualization: false`, there are no overlays or tooltips
-   With `EnableVisualization: true`, visualization works normally
-   Change is applied after restarting the game

**How to Verify:**

-   Confirm absence of visualization when disabled
-   Confirm presence of visualization when enabled
-   Verify if there are no errors

---

#### Test 5.4.2: Opacity Adjustment

**Objective:** Verify if overlay opacity can be adjusted.

**Steps:**

1. Open the configuration file
2. Change `OverlayOpacity` to different values (0.1, 0.5, 1.0)
3. Save the configuration
4. Restart the game
5. Load a save
6. Observe overlay opacity
7. Repeat with other values

**Expected Result:**

-   Opacity changes according to configured value
-   Lower values = more transparent overlays
-   Higher values = more opaque overlays
-   Change is applied after restarting

**How to Verify:**

-   Compare visual opacity with configured value
-   Test multiple values
-   Confirm that change is applied

---

#### Test 5.4.3: Enable/Disable Tooltips

**Objective:** Verify if it's possible to enable and disable tooltips.

**Steps:**

1. Open the configuration file
2. Change `TooltipEnabled` to `false`
3. Save the configuration
4. Restart the game
5. Load a save
6. Hover over tiles
7. Observe if tooltips appear
8. Repeat enabling tooltips

**Expected Result:**

-   With `TooltipEnabled: false`, there are no tooltips
-   With `TooltipEnabled: true`, tooltips work normally
-   Overlays continue to work independently

**How to Verify:**

-   Confirm absence of tooltips when disabled
-   Confirm presence of tooltips when enabled
-   Verify if overlays still work

---

#### Test 5.4.4: Enable/Disable Hoe Feedback

**Objective:** Verify if it's possible to enable and disable feedback when using the Hoe.

**Steps:**

1. Open the configuration file
2. Change `EnableHoeFeedback` to `false`
3. Save the configuration
4. Restart the game
5. Load a save
6. Use the Hoe on tiles
7. Observe if there is flash and text
8. Repeat enabling feedback

**Expected Result:**

-   With `EnableHoeFeedback: false`, there is no flash or text
-   With `EnableHoeFeedback: true`, feedback works normally
-   Other visualizations continue to work

**How to Verify:**

-   Confirm absence of feedback when disabled
-   Confirm presence of feedback when enabled
-   Verify if other visualizations work

---

### 5.5. Performance Optimizations

#### Test 5.5.1: Viewport Culling

**Objective:** Verify if overlays are rendered only for tiles visible on the screen.

**Steps:**

1. Load a save with many health tiles (500+)
2. Navigate to an area with many tiles
3. Observe visible overlays
4. Move to a different area
5. Observe if overlays from the previous area disappear
6. Return to the previous area
7. Verify if overlays reappear

**Expected Result:**

-   Only overlays visible on screen are rendered
-   Overlays outside the screen are not rendered
-   No lag when moving between areas
-   Overlays reappear when returning

**How to Verify:**

-   Observe if there is performance improvement
-   Verify if overlays appear/disappear appropriately
-   Confirm that there is no lag

---

#### Test 5.5.2: Overlay Caching

**Objective:** Verify if overlays are cached to improve performance.

**Steps:**

1. Load a save
2. Navigate to an area with overlays
3. Observe the first rendering
4. Leave and return to the same area
5. Observe if rendering is faster
6. Repeat 5 times

**Expected Result:**

-   First rendering may be slower
-   Subsequent renderings are faster
-   No noticeable lag after cache
-   Overlays appear instantly

**How to Verify:**

-   Compare rendering time
-   Observe if there is improvement after first rendering
-   Confirm that there is no lag

---

#### Test 5.5.3: Update Throttling

**Objective:** Verify if visualization updates are throttled to avoid overload.

**Steps:**

1. Load a save
2. Use the Hoe rapidly on many consecutive tiles
3. Observe if there is lag or delay
4. Verify if feedbacks appear in appropriate order

**Expected Result:**

-   No lag even with rapid actions
-   Feedbacks appear in order
-   System is not overloaded
-   Performance remains stable

**How to Verify:**

-   Observe if there is lag
-   Verify the order of feedbacks
-   Confirm that system remains responsive

---

#### Test 5.5.4: Performance with Many Tiles

**Objective:** Test performance with a large amount of health tiles.

**Steps:**

1. Load a save with many health tiles (ideally 500+)
2. Navigate quickly between areas
3. Hover over many tiles
4. Use the Hoe on several tiles
5. Observe overall performance

**Expected Result:**

-   Frame rate remains stable
-   No noticeable lag
-   Tooltips appear instantly
-   Feedbacks are displayed correctly

**How to Verify:**

-   Monitor frame rate (if possible)
-   Observe if there is lag or stuttering
-   Confirm that all visualizations work

---

## 6. Edge Case Tests

### 6.1. Corrupted Saves

#### Test 6.1.1: Corrupted Data File

**Objective:** Verify how the system handles a corrupted data file.

**Steps:**

1. Load a save and use the Hoe on some tiles
2. Save the game
3. Close the game
4. Manually open the mod's data file
5. Corrupt the file (randomly change some bytes)
6. Save the file
7. Restart the game and load the save
8. Observe the behavior

**Expected Result:**

-   System detects corruption
-   Console shows appropriate error message
-   Mod continues to function (may use default values or ignore corrupted data)
-   Game does not crash

**How to Verify:**

-   Check messages in the console
-   Confirm that the game does not crash
-   Observe if the mod continues to function

---

#### Test 6.1.2: Missing Data File

**Objective:** Verify how the system handles when the data file does not exist.

**Steps:**

1. Load a save and use the Hoe on tiles
2. Save the game
3. Close the game
4. Delete the mod's data file
5. Restart the game and load the save
6. Observe the behavior

**Expected Result:**

-   System detects missing file
-   Console shows informative message
-   Mod creates new file when needed
-   Game functions normally

**How to Verify:**

-   Check messages in the console
-   Confirm that the game does not crash
-   Observe if new file is created

---

#### Test 6.1.3: Save with Inconsistent Data

**Objective:** Test if the system handles inconsistent data in the file.

**Steps:**

1. Load a save
2. Use the Hoe on tiles
3. Save the game
4. Close the game
5. Manually edit the data file to create inconsistencies (e.g., invalid coordinates)
6. Save the file
7. Restart and load the save
8. Observe the behavior

**Expected Result:**

-   System detects inconsistencies
-   Inconsistent data is ignored or corrected
-   Console shows appropriate messages
-   Game does not crash

**How to Verify:**

-   Check messages in the console
-   Confirm that inconsistent data does not cause problems
-   Observe if the mod continues to function

---

### 6.2. Extreme Coordinates

#### Test 6.2.1: Extreme Negative Coordinates

**Objective:** Test behavior with extreme negative coordinates.

**Steps:**

1. Load a save
2. Navigate to areas with negative coordinates (if accessible)
3. Use the Hoe on tiles in these areas
4. Observe the behavior
5. Save and reload
6. Check the same tiles

**Expected Result:**

-   System handles negative coordinates
-   Data is saved and loaded correctly
-   Visualizations work normally
-   No errors

**How to Verify:**

-   Check messages in the console
-   Confirm that data is saved/loaded
-   Observe visualizations

---

#### Test 6.2.2: Extreme Positive Coordinates

**Objective:** Test behavior with extreme positive coordinates.

**Steps:**

1. Load a save
2. Navigate to areas with extreme positive coordinates (if accessible)
3. Use the Hoe on tiles in these areas
4. Observe the behavior
5. Save and reload
6. Check the same tiles

**Expected Result:**

-   System handles extreme positive coordinates
-   Data is saved and loaded correctly
-   Visualizations work normally
-   No errors

**How to Verify:**

-   Check messages in the console
-   Confirm that data is saved/loaded
-   Observe visualizations

---

#### Test 6.2.3: Coordinates Outside Allowed Range

**Objective:** Verify if the system rejects coordinates outside the ±10,000 range.

**Steps:**

1. Load a save
2. If possible, navigate to areas beyond ±10,000 (may require mods or cheats)
3. Try to use the Hoe on tiles in these areas
4. Observe the behavior
5. Check the console

**Expected Result:**

-   System rejects coordinates outside the range
-   Console shows error message
-   Tile is not registered
-   System continues to function

**How to Verify:**

-   Check error messages in the console
-   Confirm that tiles are not registered
-   Observe if the system does not crash

---

### 6.3. Invalid Values

#### Test 6.3.1: Negative Values

**Objective:** Verify how the system handles negative health values.

**Steps:**

1. Load a save
2. Use the Hoe on tiles
3. Save the game
4. Close the game
5. Edit the data file to include negative values
6. Save the file
7. Restart and load the save
8. Observe the behavior

**Expected Result:**

-   System detects negative values
-   Negative values are corrected to 0 or ignored
-   Console shows warning message
-   Game does not crash

**How to Verify:**

-   Check messages in the console
-   Confirm that negative values are not displayed
-   Observe if tooltips show valid values

---

#### Test 6.3.2: Values Above 100

**Objective:** Verify how the system handles values above 100.

**Steps:**

1. Load a save
2. Use the Hoe on tiles
3. Save the game
4. Close the game
5. Edit the data file to include values above 100
6. Save the file
7. Restart and load the save
8. Observe the behavior

**Expected Result:**

-   System detects values above 100
-   Values are corrected to 100 or ignored
-   Console shows warning message
-   Game does not crash

**How to Verify:**

-   Check messages in the console
-   Confirm that values above 100 are not displayed
-   Observe if tooltips show valid values

---

#### Test 6.3.3: Non-Numeric Values

**Objective:** Verify how the system handles non-numeric values.

**Steps:**

1. Load a save
2. Use the Hoe on tiles
3. Save the game
4. Close the game
5. Edit the data file to include non-numeric values (e.g., "abc")
6. Save the file
7. Restart and load the save
8. Observe the behavior

**Expected Result:**

-   System detects non-numeric values
-   Invalid values are ignored
-   Console shows error message
-   Game does not crash

**How to Verify:**

-   Check error messages in the console
-   Confirm that invalid values do not cause problems
-   Observe if the mod continues to function

---

### 6.4. Multiple Saves

#### Test 6.4.1: Alternating between Saves

**Objective:** Verify if the system correctly handles alternating between different saves.

**Steps:**

1. Load Save A
2. Use the Hoe on tiles and note the values
3. Save the game
4. Return to main menu
5. Load Save B
6. Use the Hoe on different tiles
7. Save the game
8. Return to main menu
9. Load Save A again
10. Verify if values are correct

**Expected Result:**

-   Each save maintains its own data
-   No mixing of data between saves
-   Loading works correctly after alternating
-   Console shows no errors

**How to Verify:**

-   Compare values from each save
-   Confirm that there is no data mixing
-   Check messages in the console

---

#### Test 6.4.2: Multiple Saves with Same Name

**Objective:** Verify if the system handles saves with identical names.

**Steps:**

1. Create two saves with the same name (in different folders)
2. Load the first save
3. Use the Hoe on tiles
4. Save the game
5. Return to menu
6. Load the second save
7. Use the Hoe on tiles
8. Save the game
9. Alternate between saves
10. Verify if data is correct

**Expected Result:**

-   Each save maintains its own data
-   No conflict between saves with the same name
-   System uses internal unique identifiers
-   Data is not mixed

**How to Verify:**

-   Compare data from each save
-   Confirm that there is no conflict
-   Check messages in the console

---

### 6.5. Multiple Locations

#### Test 6.5.1: Data in Multiple Locations

**Objective:** Verify if the system handles data in multiple locations simultaneously.

**Steps:**

1. Load a save
2. Navigate to the Farm
3. Use the Hoe on 20 tiles
4. Navigate to Town
5. Use the Hoe on 20 tiles
6. Navigate to the Mine
7. Use the Hoe on 20 tiles
8. Save the game
9. Reload the save
10. Check tiles in all locations

**Expected Result:**

-   Data from all locations is saved
-   Data is loaded correctly for each location
-   No mixing of data between locations
-   Visualizations work in all locations

**How to Verify:**

-   Check tiles in each location
-   Confirm that values are correct
-   Observe visualizations in each location

---

#### Test 6.5.2: Rapid Alternation between Locations

**Objective:** Test behavior when rapidly alternating between locations.

**Steps:**

1. Load a save with data in multiple locations
2. Navigate rapidly between Farm, Town, and Mine
3. Observe visualizations in each location
4. Repeat the alternation 10 times
5. Verify if there is lag or errors

**Expected Result:**

-   Visualizations appear correctly in each location
-   No lag when alternating
-   Data is loaded/unloaded correctly
-   Console shows no errors

**How to Verify:**

-   Observe performance when alternating
-   Check visualizations in each location
-   Confirm that there are no errors

---

### 6.6. Special Game Conditions

#### Test 6.6.1: Testing during Events

**Objective:** Verify if the system works during special game events.

**Steps:**

1. Load a save
2. Participate in an event (festival, wedding, etc.)
3. During the event, observe if there is any unexpected behavior
4. After the event, verify if data is correct

**Expected Result:**

-   System does not interfere with events
-   Data remains correct after events
-   No errors or crashes
-   Visualizations work normally after events

**How to Verify:**

-   Observe behavior during events
-   Check data after events
-   Confirm that there are no errors

---

#### Test 6.6.2: Testing at Different Times of Day

**Objective:** Verify if the system works at different times of day (lighting).

**Steps:**

1. Load a save
2. Test visualizations in the morning (6:00-12:00)
3. Test visualizations in the afternoon (12:00-18:00)
4. Test visualizations in the evening (18:00-24:00)
5. Test visualizations at night (0:00-6:00)
6. Observe if colors are visible at all times

**Expected Result:**

-   Visualizations are visible at all times
-   Colors remain distinguishable
-   No visibility problems
-   Performance is consistent

**How to Verify:**

-   Observe visualizations at each time
-   Confirm that colors are visible
-   Verify if there are contrast problems

---

#### Test 6.6.3: Testing in Different Seasons

**Objective:** Verify if the system works in different game seasons.

**Steps:**

1. Load a save in Spring
2. Use the Hoe on tiles
3. Save the game
4. Advance to Summer and reload
5. Check the tiles
6. Repeat for Fall and Winter
7. Observe if visualizations work in all seasons

**Expected Result:**

-   Data is preserved between seasons
-   Visualizations work in all seasons
-   Colors remain distinguishable
-   No season-specific problems

**How to Verify:**

-   Check data in each season
-   Observe visualizations in each season
-   Confirm that there are no problems

---

## 7. Performance Tests

### 7.1. Performance with Many Tiles

#### Test 7.1.1: 100 Tiles

**Objective:** Test performance with 100 soil health tiles.

**Steps:**

1. Load a save with 100 health tiles
2. Navigate through the area
3. Hover over tiles
4. Use the Hoe on some tiles
5. Observe overall performance

**Expected Result:**

-   Frame rate remains stable (60 FPS ideal)
-   No noticeable lag
-   Tooltips appear instantly
-   Feedbacks are displayed without delay

**How to Verify:**

-   Monitor frame rate (if possible)
-   Observe if there is lag
-   Time tooltip response time

---

#### Test 7.1.2: 500 Tiles

**Objective:** Test performance with 500 soil health tiles.

**Steps:**

1. Load a save with 500 health tiles
2. Navigate through the area
3. Hover over tiles
4. Use the Hoe on some tiles
5. Observe overall performance

**Expected Result:**

-   Frame rate remains stable
-   There may be slight initial lag when loading
-   Tooltips appear quickly
-   Feedbacks are displayed without significant delay

**How to Verify:**

-   Monitor frame rate
-   Observe if there is lag
-   Compare with the 100 tiles test

---

#### Test 7.1.3: 1000 Tiles

**Objective:** Test performance with 1000 soil health tiles.

**Steps:**

1. Load a save with 1000 health tiles
2. Navigate through the area
3. Hover over tiles
4. Use the Hoe on some tiles
5. Observe overall performance

**Expected Result:**

-   Frame rate may decrease slightly
-   There may be initial lag when loading
-   Tooltips appear with slight delay
-   System remains playable

**How to Verify:**

-   Monitor frame rate
-   Observe if there is lag
-   Evaluate if performance is acceptable

---

### 7.2. Save/Load Performance

#### Test 7.2.1: Save Time

**Objective:** Measure the time needed to save soil health data.

**Steps:**

1. Load a save with 500 health tiles
2. Use a stopwatch
3. Save the game
4. Note the total save time
5. Repeat 5 times
6. Calculate the average

**Expected Result:**

-   Saving is fast (less than 1 second ideally)
-   Time is consistent between attempts
-   No freezes during saving
-   Console shows progress

**How to Verify:**

-   Compare save times
-   Verify if there are significant variations
-   Confirm that saving is fast

---

#### Test 7.2.2: Load Time

**Objective:** Measure the time needed to load soil health data.

**Steps:**

1. Load a save with 500 health tiles
2. Use a stopwatch
3. Note the total load time
4. Repeat 5 times
5. Calculate the average

**Expected Result:**

-   Loading is fast (less than 2 seconds ideally)
-   Time is consistent between attempts
-   No freezes during loading
-   Console shows progress

**How to Verify:**

-   Compare load times
-   Verify if there are significant variations
-   Confirm that loading is fast

---

#### Test 7.2.3: Impact on Game Save/Load Time

**Objective:** Verify the mod's impact on game save/load time.

**Steps:**

1. Load a save without mod data (or disable the mod)
2. Save the game and note the time
3. Load the game and note the time
4. Enable the mod and load the same save
5. Use the Hoe on 500 tiles
6. Save the game and note the time
7. Load the game and note the time
8. Compare the times

**Expected Result:**

-   Impact on save time is minimal (less than 0.5 seconds additional)
-   Impact on load time is minimal (less than 1 second additional)
-   Difference is acceptable for the user

**How to Verify:**

-   Compare times with and without the mod
-   Calculate the difference
-   Evaluate if the impact is acceptable

---

### 7.3. Visualization Performance

#### Test 7.3.1: Overlay Rendering

**Objective:** Test overlay rendering performance.

**Steps:**

1. Load a save with 500 health tiles
2. Navigate to an area with many overlays
3. Observe frame rate
4. Move quickly through the area
5. Observe if there are frame drops

**Expected Result:**

-   Frame rate remains stable
-   No significant frame drops
-   Overlays are rendered smoothly
-   Viewport culling works correctly

**How to Verify:**

-   Monitor frame rate
-   Observe if there is stuttering
-   Confirm that rendering is smooth

---

#### Test 7.3.2: Tooltip Display

**Objective:** Test tooltip display performance.

**Steps:**

1. Load a save with 500 health tiles
2. Hover quickly over many tiles
3. Observe if there is lag in tooltips
4. Repeat 10 times
5. Note if there are problems

**Expected Result:**

-   Tooltips appear instantly
-   No lag when moving the mouse
-   Caching works correctly
-   Performance is consistent

**How to Verify:**

-   Observe display speed
-   Verify if there are delays
-   Confirm that caching works

---

#### Test 7.3.3: Hoe Visual Feedback

**Objective:** Test visual feedback performance when using the Hoe.

**Steps:**

1. Load a save
2. Use the Hoe rapidly on 50 consecutive tiles
3. Observe if there is lag or delay in feedbacks
4. Repeat 5 times
5. Note if there are problems

**Expected Result:**

-   Feedbacks appear instantly
-   No lag when using the Hoe rapidly
-   Throttling works correctly
-   Performance is consistent

**How to Verify:**

-   Observe feedback speed
-   Verify if there are delays
-   Confirm that there is no lag

---

### 7.4. Memory Usage

#### Test 7.4.1: Memory Usage with Few Tiles

**Objective:** Verify memory usage with few health tiles.

**Steps:**

1. Load a save with 50 health tiles
2. Use a memory monitor (if available)
3. Note the game's memory usage
4. Navigate the game for 10 minutes
5. Note the memory usage again

**Expected Result:**

-   Memory usage is low
-   No memory leak
-   Memory usage remains stable
-   In-memory cache works correctly

**How to Verify:**

-   Compare initial and final memory usage
-   Verify if there is significant increase
-   Confirm that there is no leak

---

#### Test 7.4.2: Memory Usage with Many Tiles

**Objective:** Verify memory usage with many health tiles.

**Steps:**

1. Load a save with 500 health tiles
2. Use a memory monitor (if available)
3. Note the game's memory usage
4. Navigate the game for 10 minutes
5. Note the memory usage again

**Expected Result:**

-   Memory usage is moderate
-   No memory leak
-   Memory usage remains stable
-   In-memory cache works correctly

**How to Verify:**

-   Compare initial and final memory usage
-   Verify if there is significant increase
-   Confirm that there is no leak

---

#### Test 7.4.3: Memory Usage Over Time

**Objective:** Verify if there is memory leak when playing for a long period.

**Steps:**

1. Load a save with 500 health tiles
2. Use a memory monitor (if available)
3. Note initial memory usage
4. Play for 30 minutes (navigate, use Hoe, save, etc.)
5. Note memory usage every 10 minutes
6. Compare the values

**Expected Result:**

-   Memory usage remains stable
-   No continuous memory increase
-   Cache is managed correctly
-   No memory leak

**How to Verify:**

-   Compare memory values over time
-   Verify if there is an increasing trend
-   Confirm that there is no leak

---

## 8. Security Tests

### 8.1. Input Validation

#### Test 8.1.1: Health Value Validation

**Objective:** Verify if the system correctly validates health values.

**Steps:**

1. Load a save
2. Use the Hoe on tiles
3. Save the game
4. Close the game
5. Edit the data file to include invalid values (negative, above 100, non-numeric)
6. Save the file
7. Restart and load the save
8. Observe the behavior

**Expected Result:**

-   System detects invalid values
-   Invalid values are corrected or ignored
-   Console shows warning messages
-   Game does not crash
-   Valid data is loaded correctly

**How to Verify:**

-   Check messages in the console
-   Confirm that invalid values are not displayed
-   Observe if the game works normally

---

#### Test 8.1.2: Coordinate Validation

**Objective:** Verify if the system correctly validates coordinates.

**Steps:**

1. Load a save
2. Use the Hoe on tiles
3. Save the game
4. Close the game
5. Edit the data file to include invalid coordinates (outside the ±10,000 range)
6. Save the file
7. Restart and load the save
8. Observe the behavior

**Expected Result:**

-   System detects invalid coordinates
-   Invalid coordinates are ignored
-   Console shows error messages
-   Game does not crash
-   Valid data is loaded correctly

**How to Verify:**

-   Check messages in the console
-   Confirm that tiles with invalid coordinates do not appear
-   Observe if the game works normally

---

#### Test 8.1.3: Location Name Validation

**Objective:** Verify if the system correctly validates location names.

**Steps:**

1. Load a save
2. Use the Hoe on tiles
3. Save the game
4. Close the game
5. Edit the data file to include invalid location names (very long, with dangerous characters)
6. Save the file
7. Restart and load the save
8. Observe the behavior

**Expected Result:**

-   System sanitizes location names
-   Invalid names are corrected
-   Console shows warning messages
-   Game does not crash
-   Data is loaded correctly

**How to Verify:**

-   Check messages in the console
-   Confirm that names are sanitized
-   Observe if the game works normally

---

### 8.2. DoS Protection

#### Test 8.2.1: Tile Limit per Location

**Objective:** Verify if the system respects the limit of 500 tiles per location.

**Steps:**

1. Load a save
2. Use the Hoe on more than 500 tiles in the same location
3. Observe the behavior after reaching 500 tiles
4. Check the console for messages
5. Try to use the Hoe on more tiles

**Expected Result:**

-   System accepts up to 500 tiles
-   After reaching 500, new tiles are not registered or replace the oldest ones
-   Console shows warning message
-   System does not crash
-   Performance remains stable

**How to Verify:**

-   Count how many tiles were registered
-   Check messages in the console
-   Confirm that the system does not crash

---

#### Test 8.2.2: Location Limit per Save

**Objective:** Verify if the system respects the limit of 50 locations per save.

**Steps:**

1. Load a save
2. Use the Hoe on tiles in more than 50 different locations
3. Observe the behavior after reaching 50 locations
4. Check the console for messages
5. Try to use the Hoe on new locations

**Expected Result:**

-   System accepts up to 50 locations
-   After reaching 50, new locations are not registered
-   Console shows warning message
-   System does not crash
-   Performance remains stable

**How to Verify:**

-   Count how many locations were registered
-   Check messages in the console
-   Confirm that the system does not crash

---

#### Test 8.2.3: Total Tile Limit

**Objective:** Verify if the system respects the limit of 30,000 tiles per save.

**Steps:**

1. Load a save
2. Use the Hoe on many tiles until reaching 30,000
3. Observe the behavior after reaching the limit
4. Check the console for messages
5. Try to use the Hoe on more tiles

**Expected Result:**

-   System accepts up to 30,000 tiles
-   After reaching the limit, new tiles are not registered
-   Console shows warning message
-   System does not crash
-   Performance remains stable

**How to Verify:**

-   Monitor the total number of registered tiles
-   Check messages in the console
-   Confirm that the system does not crash

---

### 8.3. File Sanitization

#### Test 8.3.1: Filename Sanitization

**Objective:** Verify if the system correctly sanitizes filenames.

**Steps:**

1. Create a save with name containing dangerous special characters (e.g., "../../../malicious")
2. Use the Hoe on tiles
3. Save the game
4. Check the name of the created data file
5. Confirm that the name is sanitized

**Expected Result:**

-   File name is sanitized
-   Dangerous characters are removed or replaced
-   File is created in a safe location
-   No path traversal

**How to Verify:**

-   Check the file name in the file system
-   Confirm that it is sanitized
-   Verify if the file is in the correct location

---

#### Test 8.3.2: Path Traversal Prevention

**Objective:** Verify if the system prevents path traversal attacks.

**Steps:**

1. Try to create a save with name containing path traversal (e.g., "../../etc/passwd")
2. Use the Hoe on tiles
3. Save the game
4. Verify where the data file is created
5. Confirm that there was no path traversal

**Expected Result:**

-   System prevents path traversal
-   File is created in the correct location
-   No access to unauthorized directories
-   Console shows error message if attempt is detected

**How to Verify:**

-   Check the location of the created file
-   Confirm that it is in the save directory
-   Check messages in the console

---

#### Test 8.3.3: Unicode Sanitization

**Objective:** Verify if the system correctly handles Unicode characters.

**Steps:**

1. Create a save with name containing special Unicode characters (e.g., homoglyphs)
2. Use the Hoe on tiles
3. Save the game
4. Check the data file name
5. Confirm that the name is safe

**Expected Result:**

-   System normalizes Unicode
-   Dangerous characters are handled
-   File is created with safe name
-   No encoding problems

**How to Verify:**

-   Check the file name
-   Confirm that it is normalized
-   Verify if there are no encoding problems

---

### 8.4. Data Protection

#### Test 8.4.1: Isolation between Saves

**Objective:** Verify if data from different saves is isolated.

**Steps:**

1. Load Save A
2. Use the Hoe on tiles and note the values
3. Save the game
4. Return to menu
5. Load Save B
6. Use the Hoe on different tiles
7. Save the game
8. Check the data files of each save
9. Confirm that there is no data mixing

**Expected Result:**

-   Each save has its own data file
-   No mixing of data between saves
-   Data is isolated correctly
-   No data leakage between saves

**How to Verify:**

-   Check the data files of each save
-   Confirm that they are separate
-   Check the content of each file

---

#### Test 8.4.2: Protection against External Modification

**Objective:** Verify if the system handles external modifications to data files.

**Steps:**

1. Load a save
2. Use the Hoe on tiles
3. Save the game
4. Close the game
5. Modify the data file externally
6. Restart the game
7. Load the save
8. Observe the behavior

**Expected Result:**

-   System detects external modifications
-   Data is validated when loading
-   Invalid modifications are rejected
-   Console shows appropriate messages
-   Game does not crash

**How to Verify:**

-   Check messages in the console
-   Confirm that invalid modifications are rejected
-   Observe if the game works normally

---

#### Test 8.4.3: Automatic Backup

**Objective:** Verify if the system creates backups of the data.

**Steps:**

1. Load a save
2. Use the Hoe on tiles
3. Save the game several times
4. Verify if there are backups of data files
5. Confirm that backups are created correctly

**Expected Result:**

-   System creates backups of data
-   Backups are created before overwriting
-   Backups are maintained for an appropriate period
-   Backups can be used for recovery

**How to Verify:**

-   Check the save folder for backup files
-   Confirm that backups exist
-   Check the content of backups

---

## 9. Best Practices for Testing Mods

### 9.1. Environment Preparation

#### 9.1.1. Use Dedicated Saves for Testing

**Why it's important:**

-   Avoids corrupting main game saves
-   Allows more aggressive testing
-   Facilitates test repetition
-   Keeps the main game clean

**How to do it:**

1. Create a new save specifically for testing
2. Use a clear name (e.g., "Test_LivingRoots")
3. Keep this save separate from game saves
4. Use this save for all tests

**Tips:**

-   Maintain multiple test saves for different scenarios
-   Document the purpose of each save
-   Regularly backup test saves

---

#### 9.1.2. Keep Console Open

**Why it's important:**

-   Allows seeing error and warning messages
-   Helps identify problems quickly
-   Facilitates issue debugging
-   Provides information for bug reports

**How to do it:**

1. Keep the SMAPI console open (`F2`)
2. Minimize but do not close the console
3. Regularly check the console during tests
4. Note error or warning messages

**Tips:**

-   Use console scroll to see previous messages
-   Copy error messages for reports
-   Check the console after each important action

---

#### 9.1.3. Document Tests

**Why it's important:**

-   Allows reproducing bugs
-   Facilitates communication with developers
-   Helps identify patterns
-   Maintains a record of what was tested

**How to do it:**

1. Create a test document
2. Note each test performed
3. Document expected and obtained results
4. Take screenshots of bugs or unexpected behaviors

**Tips:**

-   Use templates for test documentation
-   Include date/time of each test
-   Note the tested mod version
-   Document the environment (operating system, game version, etc.)

---

### 9.2. Test Execution

#### 9.2.1. Test Systematically

**Why it's important:**

-   Ensures all functionalities are tested
-   Avoids forgetting important tests
-   Allows identifying problems more efficiently
-   Facilitates test repetition

**How to do it:**

1. Follow the test order in this guide
2. Complete each test before advancing
3. Mark tests as completed
4. Go back and repeat tests if necessary

**Tips:**

-   Use a checklist to track progress
-   Do not skip tests even if they seem simple
-   Note failed tests for later review
-   Prioritize critical tests (persistence, security)

---

#### 9.2.2. Test in Different Conditions

**Why it's important:**

-   Identifies problems specific to certain conditions
-   Ensures the mod works in all scenarios
-   Prevents bugs in unusual situations
-   Improves overall mod quality

**How to do it:**

1. Test at different times of day
2. Test in different seasons
3. Test in different locations
4. Test with different amounts of data

**Tips:**

-   Document the conditions of each test
-   Compare results between different conditions
-   Identify problem patterns
-   Report condition-specific problems

---

#### 9.2.3. Test Edge Cases

**Why it's important:**

-   Identifies problems in extreme situations
-   Prevents crashes in unusual scenarios
-   Improves mod robustness
-   Discovers bugs that normal tests don't find

**How to do it:**

1. Test with extreme values (very high, very low)
2. Test with extreme coordinates
3. Test with corrupted saves
4. Test with rapid and repetitive actions

**Tips:**

-   Don't be afraid to test unusual situations
-   Document tested edge cases
-   Report unexpected behaviors even if they don't cause crashes
-   Consider edge cases that users might encounter

---

### 9.3. Bug Reporting

#### 9.3.1. Report Bugs in Detail

**Why it's important:**

-   Facilitates bug reproduction
-   Helps developers identify the cause
-   Increases the chance of the bug being fixed
-   Provides context for the fix

**How to do it:**

1. Describe the bug in detail
2. Include steps to reproduce
3. Provide screenshots or logs
4. Document the environment and versions

**Tips:**

-   Use bug report templates
-   Include console messages
-   Specify the mod and game version
-   Describe expected vs. obtained behavior

---

#### 9.3.2. Provide Adequate Context

**Why it's important:**

-   Helps understand the bug's context
-   Allows identifying the root cause
-   Facilitates fix prioritization
-   Provides information for regression tests

**How to do it:**

1. Describe what you were doing before the bug
2. Include information about the save used
3. List other installed mods
4. Describe the test environment

**Tips:**

-   Be specific about conditions
-   Include information about settings
-   Mention if the bug is reproducible
-   Describe the frequency of the bug

---

#### 9.3.3. Use Appropriate Channels

**Why it's important:**

-   Ensures the report is seen by developers
-   Allows bug tracking
-   Facilitates communication
-   Maintains organization of reports

**How to do it:**

1. Use GitHub issues for bug reports
2. Follow issue templates
3. Include appropriate labels
4. Answer developer questions

**Tips:**

-   Check if the bug has already been reported
-   Use a descriptive title for the issue
-   Include links to related tests
-   Keep the issue updated with new information

---

### 9.4. Continuous Improvement

#### 9.4.1. Learn from Tests

**Why it's important:**

-   Improves the quality of future tests
-   Identifies areas that need more attention
-   Develops testing skills
-   Contributes to mod improvement

**How to do it:**

1. Review the tests performed
2. Identify areas that could be tested better
3. Suggest new tests
4. Share experiences with other testers

**Tips:**

-   Document lessons learned
-   Suggest improvements to the test guide
-   Participate in testing discussions
-   Contribute additional test cases

---

#### 9.4.2. Stay Updated

**Why it's important:**

-   Allows testing new features
-   Ensures tests are up to date
-   Identifies regressions in new versions
-   Contributes to continuous improvement

**How to do it:**

1. Follow mod updates
2. Read release notes
3. Test new versions as soon as available
4. Update the test guide as necessary

**Tips:**

-   Subscribe to release notifications
-   Participate in development discussions
-   Test features in development
-   Report regressions in new versions

---

#### 9.4.3. Collaborate with the Community

**Why it's important:**

-   Improves mod quality for everyone
-   Allows learning from other testers
-   Increases test coverage
-   Strengthens the mod community

**How to do it:**

1. Share test results
2. Help other testers
3. Contribute improvements to the guide
4. Participate in discussions

**Tips:**

-   Be respectful and constructive
-   Share knowledge and experiences
-   Help new testers
-   Contribute to documentation

---

## 10. Final Validation Checklist

### 10.1. Main Features Checklist

#### US01-01 - Soil Health Persistence

-   [ ] **Automatic Loading**

    -   [ ] Data is loaded when starting the game
    -   [ ] Console shows loading messages
    -   [ ] No errors when loading
    -   [ ] Data is loaded correctly after save/load

-   [ ] **Automatic Saving**

    -   [ ] Data is saved before the game saves
    -   [ ] Console shows saving messages
    -   [ ] No errors when saving
    -   [ ] Data is preserved after save/load

-   [ ] **In-Memory Cache**

    -   [ ] Cache improves performance
    -   [ ] Cache is invalidated correctly
    -   [ ] No obsolete values in cache
    -   [ ] Data access is fast

-   [ ] **Coordinate and Value Validation**

    -   [ ] Values in range 0-100 are accepted
    -   [ ] Coordinates are validated correctly
    -   [ ] Invalid values are rejected
    -   [ ] Invalid coordinates are rejected

-   [ ] **DoS Protection**

    -   [ ] Limit of 500 tiles per location is respected
    -   [ ] Limit of 50 locations per save is respected
    -   [ ] Limit of 30,000 tiles per save is respected
    -   [ ] System does not crash when reaching limits

-   [ ] **Filename Sanitization**
    -   [ ] Names with special characters are sanitized
    -   [ ] Names with spaces and Unicode are handled
    -   [ ] Very long names are truncated
    -   [ ] Location names are sanitized

---

#### US01-02 - Soil Health Visualization

-   [ ] **Hover Tooltips**

    -   [ ] Tooltips appear when hovering
    -   [ ] Tooltips show correct value and status
    -   [ ] Tooltips disappear when removing mouse
    -   [ ] Tooltips do not cause lag

-   [ ] **Tile Color Overlays**

    -   [ ] Poor soil (0-33%) has reddish brown color
    -   [ ] Moderate soil (34-66%) has yellowish brown color
    -   [ ] Healthy soil (67-100%) has greenish brown color
    -   [ ] Color transitions are clear
    -   [ ] Opacity is appropriate

-   [ ] **Visual Feedback When Using Hoe**

    -   [ ] Visual flash occurs when using Hoe
    -   [ ] Floating text appears with new value
    -   [ ] Flash and text work well together
    -   [ ] Feedback is consistent in different conditions

-   [ ] **Visualization Configuration**

    -   [ ] Visualization can be enabled/disabled
    -   [ ] Opacity can be adjusted
    -   [ ] Tooltips can be enabled/disabled
    -   [ ] Hoe feedback can be enabled/disabled

-   [ ] **Performance Optimizations**
    -   [ ] Viewport culling works correctly
    -   [ ] Overlays are cached
    -   [ ] Updates are throttled
    -   [ ] Performance is stable with many tiles

---

### 10.2. Edge Cases Checklist

-   [ ] **Corrupted Saves**

    -   [ ] System handles corrupted file
    -   [ ] System handles missing file
    -   [ ] System handles inconsistent data
    -   [ ] Game does not crash with corrupted saves

-   [ ] **Extreme Coordinates**

    -   [ ] System handles extreme negative coordinates
    -   [ ] System handles extreme positive coordinates
    -   [ ] System rejects coordinates outside range
    -   [ ] No errors with extreme coordinates

-   [ ] **Invalid Values**

    -   [ ] System handles negative values
    -   [ ] System handles values above 100
    -   [ ] System handles non-numeric values
    -   [ ] Invalid values are corrected or ignored

-   [ ] **Multiple Saves**

    -   [ ] Data is not mixed between saves
    -   [ ] System handles saves with same name
    -   [ ] Alternating between saves works correctly
    -   [ ] Each save maintains its own data

-   [ ] **Multiple Locations**

    -   [ ] Data works in multiple locations
    -   [ ] Rapid alternation between locations works
    -   [ ] No mixing of data between locations
    -   [ ] Visualizations work in all locations

-   [ ] **Special Game Conditions**
    -   [ ] System works during events
    -   [ ] System works at different times
    -   [ ] System works in different seasons
    -   [ ] No condition-specific problems

---

### 10.3. Performance Checklist

-   [ ] **Performance with Many Tiles**

    -   [ ] 100 tiles do not cause lag
    -   [ ] 500 tiles have acceptable performance
    -   [ ] 1000 tiles are manageable
    -   [ ] Frame rate remains stable

-   [ ] **Save/Load Performance**

    -   [ ] Saving is fast (< 1s)
    -   [ ] Loading is fast (< 2s)
    -   [ ] Impact on game save/load is minimal
    -   [ ] Times are consistent

-   [ ] **Visualization Performance**

    -   [ ] Overlays are rendered smoothly
    -   [ ] Tooltips appear instantly
    -   [ ] Feedbacks are displayed without delay
    -   [ ] No frame drops

-   [ ] **Memory Usage**
    -   [ ] Memory usage is low with few tiles
    -   [ ] Memory usage is moderate with many tiles
    -   [ ] No memory leak
    -   [ ] Memory usage is stable over time

---

### 10.4. Security Checklist

-   [ ] **Input Validation**

    -   [ ] Health values are validated
    -   [ ] Coordinates are validated
    -   [ ] Location names are validated
    -   [ ] Validations work correctly

-   [ ] **DoS Protection**

    -   [ ] Tile limit per location is respected
    -   [ ] Location limit per save is respected
    -   [ ] Total tile limit is respected
    -   [ ] System does not crash when reaching limits

-   [ ] **File Sanitization**

    -   [ ] File names are sanitized
    -   [ ] Path traversal is prevented
    -   [ ] Unicode is handled correctly
    -   [ ] Files are created in safe locations

-   [ ] **Data Protection**
    -   [ ] Data from different saves is isolated
    -   [ ] External modifications are detected
    -   [ ] Backups are created automatically
    -   [ ] Data is adequately protected

---

### 10.5. Compatibility Checklist

-   [ ] **Stardew Valley Compatibility**

    -   [ ] Mod works with current game version
    -   [ ] Mod does not interfere with game functionalities
    -   [ ] Mod works with existing saves
    -   [ ] No conflicts with base game

-   [ ] **SMAPI Compatibility**

    -   [ ] Mod loads correctly with SMAPI
    -   [ ] Mod commands work
    -   [ ] SMAPI console shows appropriate messages
    -   [ ] No mod loading errors

-   [ ] **Other Mods Compatibility**
    -   [ ] Mod works with popular mods
    -   [ ] No known conflicts
    -   [ ] Mod can be disabled without problems
    -   [ ] Loading order does not cause problems

---

### 10.6. User Experience Checklist

-   [ ] **Ease of Use**

    -   [ ] Visualizations are intuitive
    -   [ ] Tooltips are informative
    -   [ ] Colors are distinguishable
    -   [ ] Visual feedback is clear

-   [ ] **Documentation**

    -   [ ] User guide is clear
    -   [ ] Settings are explained
    -   [ ] Examples are provided
    -   [ ] Frequently asked questions are answered

-   [ ] **Accessibility**

    -   [ ] Colors have adequate contrast
    -   [ ] Text is readable
    -   [ ] Visualizations do not obstruct vision
    -   [ ] Configuration options are flexible

-   [ ] **General Satisfaction**
    -   [ ] Mod improves game experience
    -   [ ] Functionalities are useful
    -   [ ] Performance is acceptable
    -   [ ] Mod is stable and reliable

---

### 10.7. Final Validation Checklist

Before considering tests complete, verify:

-   [ ] All main tests have been executed
-   [ ] All edge cases have been tested
-   [ ] All performance tests have been completed
-   [ ] All security tests have been performed
-   [ ] All bugs found have been documented
-   [ ] All critical bugs have been reported
-   [ ] Test documentation is complete
-   [ ] Bug screenshots have been captured
-   [ ] Console logs have been saved
-   [ ] Tested mod version has been documented
-   [ ] Test environment has been documented
-   [ ] Results have been shared with developers

---

## Conclusion

This guide provides a comprehensive and systematic approach to testing the US01-01 (Soil Health Persistence) and US01-02 (Soil Health Visualization) functionalities of the LivingRoots mod for Stardew Valley.

### Key Points

1. **Test Systematically**: Follow the test order and do not skip steps
2. **Document Everything**: Maintain detailed records of all tests
3. **Test Edge Cases**: Don't be afraid to test extreme situations
4. **Report Bugs in Detail**: Provide sufficient context for reproduction
5. **Collaborate with the Community**: Share results and improve the mod

### Next Steps

After completing the tests in this guide:

1. **Review Results**: Analyze results and identify patterns
2. **Report Bugs**: Create GitHub issues for found bugs
3. **Suggest Improvements**: Contribute ideas for improvements
4. **Test New Versions**: Continue testing future mod versions
5. **Help Other Testers**: Share experiences and knowledge

### Additional Resources

-   **GitHub Repository**: [LivingRoots Repository](https://github.com/seu-usuario/LivingRoots)
-   **Developer Documentation**: `LivingRoots/docs/`
-   **User Guide**: `LivingRoots/docs/SOIL_HEALTH_VISUALIZATION_USER_GUIDE.md`
-   **Issues and Discussions**: GitHub Issues and Discussions

### Acknowledgments

Thank you for dedicating your time to test LivingRoots! Your tests help ensure that the mod is stable, secure, and fun for all players.

---

**Guide Version**: 1.0.0
**Last Update**: 2026-01-06
**Tested Mod Version**: 1.0.0
**Authors**: LivingRoots Development Team
