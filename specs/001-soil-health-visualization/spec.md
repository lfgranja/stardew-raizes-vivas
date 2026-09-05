# Feature Specification: Soil Health Visualization

**Feature Branch**: `001-soil-health-visualization`

**Created**: 2026-09-05

**Status**: Draft

**Input**: User description: "@docs/pr-73-research-report.md"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Soil Health Overlay on Tilled Tiles (Priority: P1)

As a player, I want to see color-coded overlays on tilled soil tiles so I can quickly identify which areas need attention without checking individual tile values.

**Why this priority**: This is the core value proposition of the visualization system. Without overlays, the feature delivers no user value.

**Independent Test**: Can be fully tested by tilling multiple soil tiles with different health values (poor, moderate, healthy) and verifying the correct colors render on each tile.

**Acceptance Scenarios**:

1. **Given** a tilled soil tile with health value below the "poor" threshold, **When** the player views the farm, **Then** a red overlay appears on that tile
2. **Given** a tilled soil tile with health value in the "moderate" range, **When** the player views the farm, **Then** a yellow overlay appears on that tile
3. **Given** a tilled soil tile with health value above the "healthy" threshold, **When** the player views the farm, **Then** a green overlay appears on that tile
4. **Given** a tile with health value exactly at a category boundary, **When** the overlay renders, **Then** the color matches the defined category for that boundary
5. **Given** the player has disabled overlays in configuration, **When** the player views the farm, **Then** no tile overlays are rendered

---

### User Story 2 - Inspect Soil Health via Hover Tooltip (Priority: P2)

As a player, I want to hover over a tilled soil tile and see its exact health percentage and status text so I can make informed decisions about soil management.

**Why this priority**: Tooltips provide detailed information that complements the color overlay, enabling precision gameplay.

**Independent Test**: Can be fully tested by hovering over tiles with known health values and verifying the tooltip displays correct percentage and status text.

**Acceptance Scenarios**:

1. **Given** a tilled soil tile with known health value, **When** the player hovers over it, **Then** a tooltip displays the health percentage and status category
2. **Given** the player moves the cursor away from a tile, **When** the cursor leaves the tile bounds, **Then** the tooltip disappears
3. **Given** the player has disabled tooltips in configuration, **When** the player hovers over a tile, **Then** no tooltip appears

---

### User Story 3 - Receive Hoe Action Feedback (Priority: P2)

As a player, I want visual feedback when I use a hoe on tilled soil so I can confirm my action registered and see the current health of the targeted tile.

**Why this priority**: Action feedback improves game feel and confirms player interactions are working correctly.

**Independent Test**: Can be fully tested by using a hoe on a tilled tile and verifying the flash effect and floating text appear on the targeted tile only.

**Acceptance Scenarios**:

1. **Given** the player uses a hoe on a tilled soil tile, **When** the action completes, **Then** a brief flash effect appears on that tile
2. **Given** the player uses a hoe on a tilled soil tile, **When** the action completes, **Then** floating text displays the tile's health status
3. **Given** the player uses a hoe on a non-tilled tile, **When** the action completes, **Then** no visualization feedback appears
4. **Given** multiple tilled tiles exist nearby, **When** the player uses a hoe on one tile, **Then** feedback appears only on the targeted tile

---

### User Story 4 - Configure Visualization Settings (Priority: P3)

As a player, I want to customize visualization behavior through configuration so I can tailor the feature to my preferences.

**Why this priority**: Configuration enhances accessibility and player agency but is not required for core functionality.

**Independent Test**: Can be fully tested by modifying configuration values and verifying the visualization behavior changes accordingly.

**Acceptance Scenarios**:

1. **Given** the player sets custom overlay colors in configuration, **When** overlays render, **Then** they use the custom colors instead of defaults
2. **Given** the player adjusts opacity in configuration, **When** overlays render, **Then** they appear at the configured opacity level
3. **Given** individual features are toggled off in configuration, **When** the player triggers those features, **Then** the corresponding visualizations do not appear
4. **Given** configuration contains invalid values, **When** the configuration loads, **Then** the system falls back to default values and logs a warning
5. **Given** no configuration file exists, **When** the mod initializes, **Then** default configuration is created and used

---

### Edge Cases

- What happens when a tile's health value changes while the overlay is mid-render?
- How does the system handle rapid mouse movement across many tiles (tooltip flickering)?
- What happens when the player's viewport changes size (window resize)?
- How does the system handle tiles at the edge of the viewport?
- What happens when configuration is saved while the game is actively rendering overlays?
- How does the system handle concurrent save/load operations with visualization state?
- What happens when soil health values are at exact category boundaries?

## Clarifications

### Session 2026-09-05

- Q: What are the exact health value thresholds for Poor, Moderate, and Healthy categories? → A: Poor: 0-33 (value < 34), Moderate: 34-66 (34 ≤ value < 67), Healthy: 67-100 (value ≥ 67). Integer ranges for display; half-open intervals for computation.
- Q: How long should the hoe feedback flash effect remain visible? → A: 300 milliseconds
- Q: Should the visualization provide an alternative to color-coding for colorblind players? → A: Patterns or symbols overlaid on colors (stripes for poor, dots for moderate, solid for healthy)
- Q: What exact information should the hover tooltip display? → A: "Soil Health: {percentage}% ({category})" format (e.g., "Soil Health: 75% (Healthy)")
- Q: When configuration contains invalid values, what should happen to the existing configuration? → A: Replace only invalid values with defaults, preserve valid ones
- Q: When should the cached health values be cleared or refreshed? → A: Cache invalidates on game state changes (tile modification events, save load)
- Q: How long should the floating text from hoe feedback remain visible? → A: 1000 milliseconds
- Q: What specific frame rate should overlay rendering maintain with 1,000 visible tiles? → A: 60 FPS (16.67ms per frame)
- Q: What should the overlay display when soil health data is unavailable or hasn't loaded yet? → A: Render a neutral gray overlay indicating "unknown" status
- Q: How should colors be interpolated between category boundaries? → A: Linear interpolation in RGB space between adjacent category colors
- Q: What should happen to existing user configuration files when the mod is updated and the config schema changes? → A: Forward-compatible deserialization — missing fields get defaults, unknown fields ignored, old configs keep working
- Q: Which specific concurrent scenarios must the visualization system handle safely? → A: Three specific scenarios already defined in the spec: (1) health value changing mid-render (FR-020), (2) config hot-reload during active rendering (FR-019), (3) save/load pausing rendering (FR-017)
- Q: What is the maximum acceptable delay for configuration changes to take effect (SC-003)? → A: Within the next render frame (≤16.67ms at 60 FPS)
- Q: How should technical FRs (FR-009 through FR-021) be verified without full acceptance scenarios? → A: Brief verification note on each technical FR describing the observable test outcome
- Q: How is a soil health tile uniquely identified within the visualization system? → A: Grid position (X, Y) alone — consistent with existing SoilHealthService and single-player scope

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST render color-coded overlays on tilled soil tiles based on health values (Poor, Moderate, Healthy categories), with pattern overlays for accessibility (stripes for poor, dots for moderate, solid for healthy). Pattern opacity MUST remain at minimum 0.7 regardless of overlay opacity setting to maintain accessibility compliance.
- **FR-002**: System MUST display hover tooltips showing soil health percentage and status text for tilled tiles. Cursor-to-tile mapping MUST use SMAPI's `ICursorPosition.Tile` property (from `Helper.Input.GetCursorPosition().Tile`) which provides tile coordinates directly, avoiding manual screen-to-tile conversion.
- **FR-003**: System MUST show flash effect (white overlay flash at the tile's health category color) and floating text when a hoe is used on tilled soil tiles
- **FR-004**: System MUST persist visualization configuration (feature toggles, custom colors, opacity) via JSON file
- **FR-005**: System MUST load configuration on mod initialization and apply settings to all visualization features. System MUST support hot-reload: detecting configuration file changes and applying them immediately (within the next frame) without requiring game restart
- **FR-006**: System MUST fall back to default values only for invalid configuration entries while preserving valid ones; missing files generate complete defaults. Configuration deserialization MUST be forward-compatible: missing fields receive defaults, unknown fields are ignored, and configs from older mod versions continue to load without data loss
- **FR-007**: System MUST support enabling/disabling overlay rendering, tooltip rendering, and hoe feedback independently from each other
- **FR-008**: System MUST interpolate colors using linear RGB interpolation for health values between category boundaries
- **FR-009**: System MUST perform viewport culling to only render overlays for visible tiles, with graceful degradation when visible tile count exceeds 1,000 to maintain 60 FPS. Graceful degradation MUST disable accessibility pattern rendering (stripes, dots) for all tiles, rendering only solid color overlays. Color interpolation, opacity, and viewport culling MUST remain active during degradation. The system MUST provide an `accessibilityDegradation` configuration option with values `auto` (default: patterns disable when >1,000 tiles), `never` (patterns always render, FPS may drop below 60), and `notify` (patterns always render with performance warning). When degradation activates in `auto` mode, the system MUST display a one-time toast notification to the player noting that accessibility patterns were disabled for performance, with a hint to re-enable via config (per WCAG 1.4.1 best practice: notify users when their accessibility aids are reduced; per Game Accessibility Guidelines: player agency over accessibility settings). *Verification: Render 1,001+ visible tiles and confirm frame time stays ≤16.67ms with patterns disabled but colors and opacity unchanged. Verify toast notification shown exactly once per degradation event. Verify `never` config keeps patterns at >1,000 tiles.*
- **FR-010**: System MUST cache computed health values to avoid redundant calculations, invalidating the cache on game state changes (tile modification events, save load). *Verification: Modify a tile health value and confirm overlay color updates within one frame without manual refresh.*
- **FR-011**: System MUST provide hoe action feedback only on the tile targeted by the player's cursor. *Verification: Use hoe on one tile among many and confirm flash/floating text appears only on targeted tile.*
- **FR-012**: System MUST register all event handlers on mod initialization and unregister all event handlers on mod disposal to prevent memory leaks. *Verification: Dispose mod and confirm all SMAPI event handlers are unsubscribed via framework inspection.*
- **FR-013**: System MUST handle the following concurrent scenarios safely without deadlocks or race conditions: (1) health value changing mid-render (per FR-020 frame consistency), (2) config hot-reload during active rendering (per FR-019 atomic application), (3) save/load pausing rendering (per FR-017 pause/resume).

  **Concurrency rules by operation type:**
  - **I/O-bound operations** (config file load, save/load data persistence): MUST use async/await patterns per Constitution Principle III — no `.Result` or `.Wait()` blocking.
  - **Game-loop-thread coordination** (render state snapshots, atomic config swaps, pause/resume flags): MUST use lock-free or atomic synchronization (`Interlocked`, `Volatile.Read`, atomic flag-based state machines) on the game loop thread. Async/await is not applicable to same-thread frame coordination.

  *Verification: Trigger each concurrent scenario 100 times in tests. For I/O scenarios: confirm no `.Result`/`.Wait()` calls via code inspection and no deadlocks in test output. For game-loop coordination scenarios: confirm no mixed-state frames, no UI freezes, and no state corruption via frame-level assertions.*
- **FR-014**: System MUST render a neutral gray overlay for tiles where soil health data is unavailable or hasn't loaded yet. *Verification: Load farm before soil health data is available and confirm gray (#808080) overlay renders.*
- **FR-015**: System MUST throttle tooltip updates to a minimum 50ms interval during cursor movement, suppressing successive tooltip redraws that occur within the throttle window to prevent tooltip flickering. Tooltip MUST update immediately when the cursor first enters a new tile (leading-edge), then suppress further updates until 50ms has elapsed since the last rendered update. [Resolves: CHK036 tooltip flickering]. *Verification: Sweep cursor across 20 tiles in <100ms and confirm tooltip renders at most 2 times (initial entry + one update after 50ms window). Verify that a cursor held stationary over a new tile for >50ms updates tooltip within the next render frame.*
- **FR-016**: System MUST recalculate visible tile overlays when the viewport resizes, updating the overlay list cache within a single frame [Resolves: CHK037 viewport resize]. *Verification: Resize game window and confirm overlay positions update within one frame without visible lag.*
- **FR-017**: System MUST pause overlay rendering during save/load operations and resume with refreshed data once the operation completes, ensuring visualization state consistency [Resolves: CHK038 concurrent save/load]. *Verification: Trigger save during active overlay rendering and confirm no overlay frames render during save, with correct data after load.*
- **FR-018**: System MUST render partial overlays for tiles at viewport edges using clipping bounds, with a 1-tile margin (in tile units) beyond visible viewport bounds to prevent pop-in. Visible tile range MUST be calculated from `Game1.viewport` pixel bounds divided by `Game1.tileSize` (64), then extended by ±1 tile and clamped to map bounds (`Map.DisplayWidth/Height / tileSize`) [Resolves: CHK039 viewport edge tiles]. *Verification: Pan viewport to edge tiles and confirm partial overlays render smoothly without pop-in artifacts.*
- **FR-019**: System MUST apply configuration changes atomically during the next render frame, avoiding partial-state rendering where some tiles use old config and others use new config [Resolves: CHK040 config during render, CHK064 hot-reload]. *Verification: Change config mid-render and confirm all tiles use same config within one frame (no mixed-state frames).*
- **FR-020**: System MUST maintain frame consistency during mid-render health value changes by completing the current frame with the previous state and applying changes on the next frame [Resolves: CHK046 mid-render changes]. *Verification: Update health value during overlay render and confirm current frame completes with old value, next frame shows new value.*
- **FR-021**: System MUST handle the zero-tilled-tiles case as a no-op with minimal resource usage (no overlay draw calls, no cache allocation) and no errors thrown [Resolves: CHK047 empty farm]. *Verification: Load farm with zero tilled tiles and confirm no exceptions, zero overlay draw calls, and no cache allocation in logs.*

### Key Entities

- **Visualization Configuration**: Stores feature toggles (overlays enabled, tooltips enabled, hoe feedback enabled), custom color values for each health category, opacity level, and accessibility degradation behavior (`auto`/`never`/`notify` per FR-009). Persisted as JSON.
- **Soil Health Tile**: A tilled soil position with an associated health value (0-100) that determines overlay color and tooltip content. Uniquely identified by tile position (X, Y) in grid coordinates — consistent with existing SoilHealthService tile key scheme and single-player scope. Cursor-to-tile mapping converts screen pixel coordinates to tile position (X, Y) using Stardew Valley's tile coordinate system.
- **Color Mapping**: The translation from a numeric health value to a visual color, using thresholds: Poor (0-33), Moderate (34-66), Healthy (67-100). Uses linear RGB interpolation between adjacent category colors. Boundaries use half-open intervals: [0, 34), [34, 67), [67, 100].
- **Tile Overlay**: A visual layer rendered on top of a soil tile indicating its health status through color.
- **Tooltip Data**: Formatted string displayed when hovering over a tile: "Soil Health: {percentage}% ({category})" (e.g., "Soil Health: 75% (Healthy)").
- **Hoe Feedback**: Transient visual effects (flash lasting 300ms + floating text lasting 1000ms) triggered by hoe usage on tilled soil.
- **PatternType**: Accessibility pattern overlaid on tile colors for colorblind players. Values: `Stripes` (Poor), `Dots` (Moderate), `Solid` (Healthy), `None` (Unknown or degraded).
- **ColorDTO**: Serializable RGBA color struct (R, G, B, A byte properties) used for JSON configuration persistence. Enables round-trip serialization of custom color values.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Players can identify soil health status of any tilled tile within 1 second of viewing their farm
- **SC-002**: Overlay rendering maintains 60 FPS (16.67ms per frame) with up to 1,000 visible tilled tiles
- **SC-003**: Configuration changes take effect within the next render frame (≤16.67ms at 60 FPS) without requiring game restart
- **SC-004**: Tooltips appear within 100ms of hovering over a soil tile. Note: FR-015's 50ms throttle applies to *successive* tooltip redraws on the same tile. The leading-edge update (cursor entering a new tile) is immediate and counts against the 100ms budget.
- **SC-005**: Hoe feedback appears on the correct tile (not adjacent tiles) in 100% of interactions
- **SC-006**: Zero event handler leaks after mod disposal (verified through disposal tests)
- **SC-007**: All soil health values remain within valid range (0-100) during visualization

## Assumptions

Each assumption includes a validation mechanism to confirm correctness before and during implementation.

- Soil health values are already persisted and accessible from the existing save system (implemented in PR #72 / Issue #22). **Validation**: Verify by reading SoilHealthService implementation and running existing save/load tests.
- Tilled soil tiles can be identified through existing game state queries. **Validation**: Confirm via Stardew Valley API documentation for tile state queries during Phase 0 research.
- Players understand color-coding conventions (red/yellow/green) without in-game legend. **Validation**: This is a UX risk; if player feedback indicates confusion, an in-game legend may be required (currently Out of Scope).
- The existing `ModConstants` structure will be extended with visualization default values. **Validation**: Confirmed by code review — ModConstants is a static class designed for extension.
- MonoGame/XNA `SpriteBatch` rendering is available for overlay drawing. **Validation**: Confirmed — Stardew Valley uses MonoGame and SpriteBatch is available in all SMAPI mods.
- The existing `IModHelper.Events` system provides the necessary game loop events (update, cursor change, input). **Validation**: Confirmed via SMAPI documentation and existing ModController event subscriptions.
- Configuration file lives in the mod's directory alongside existing mod files. **Validation**: Confirmed — IModDataService uses per-save SMAPI data storage in mod directory.
- Single-player game context only (no multiplayer sync required for visualization state). **Validation**: Confirmed by design — visualization is local-only; multiplayer sync is Out of Scope.

## Dependencies

- Requires soil health save/load system (US-01-01, Issue #22) to be functional
- Requires existing `ModController` event registration infrastructure
- Requires existing `ModEntry` composition root pattern for DI wiring
- Relies on `IModDataService` for JSON configuration persistence

## Out of Scope

- Soil health value modification through visualization (visualization is read-only)
- Multiplayer synchronization of visualization settings
- In-game legend or tutorial explaining the color coding
- Historical trend visualization (only current values shown)
- Integration with other mods' soil systems
