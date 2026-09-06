# Feature Specification: Soil Health Decay + Compost Restoration

**Feature Branch**: `002-soil-decay-compost`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "Soil Health Decay + Compost Restoration - This creates the core gameplay loop that makes everything else meaningful. The decay mechanic (the 'problem'): Tilled soil with no crop/plant overnight loses a small amount of health each day. Tracked via OnDayStarted event on existing tilled tiles. One rule: if (tile is tilled && bare) health -= decayRate. Makes the visualization actually dynamic instead of static. The compost restoration (the 'solution'): Composter machine (converts organic waste → compost item). Right-clicking compost on tilled soil restores health. This is the first 'builder' in the early game progression."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Soil Decays When Left Bare (Priority: P1)

As a player, I want tilled soil to lose health each day when left bare so that I must actively manage my farm and cannot ignore soil health.

**Why this priority**: This is the core "problem" that drives the entire gameplay loop. Without decay, soil health is static and the visualization has no meaning.

**Independent Test**: Can be fully tested by tilling soil, leaving it bare overnight, and verifying health decreases by the configured decay rate.

**Acceptance Scenarios**:

1. **Given** a tilled soil tile with health at 80, **When** a new day starts and the tile has no crop or plant, **Then** the tile's health decreases by the configured decay rate
2. **Given** a tilled soil tile with health at 5 and decay rate of 3, **When** a new day starts and the tile is bare, **Then** the tile's health becomes 0 (not -2)
3. **Given** a tilled soil tile with a growing crop, **When** a new day starts, **Then** the tile's health does not decrease
4. **Given** a non-tilled grass tile, **When** a new day starts, **Then** no health change occurs
5. **Given** multiple bare tilled tiles with different health values, **When** a new day starts, **Then** all bare tilled tiles decay by the same rate

---

### User Story 2 - Restore Health with Compost (Priority: P1)

As a player, I want to apply compost to tilled soil to restore its health so that I can recover from decay and maintain productive fields.

**Why this priority**: This is the core "solution" that completes the gameplay loop. Without restoration, decay would only punish with no counterplay.

**Independent Test**: Can be fully tested by applying compost to a tilled tile with reduced health and verifying health increases by the configured restoration amount.

**Acceptance Scenarios**:

1. **Given** a tilled soil tile with health at 50 and the player has compost in inventory, **When** the player right-click applies compost to the tile, **Then** the tile's health increases by the configured restoration amount and one compost is consumed
2. **Given** a tilled soil tile with health at 98 and restoration amount of 10, **When** the player applies compost, **Then** the tile's health becomes 100 (not 108)
3. **Given** a non-tilled grass tile, **When** the player attempts to apply compost, **Then** no health change occurs and the compost is not consumed
4. **Given** the player has no compost in inventory, **When** the player right-clicks a tilled tile, **Then** no health change occurs
5. **Given** a tilled tile at health 100, **When** the player applies compost, **Then** no health change occurs and the compost is not consumed

---

### User Story 3 - Produce Compost via Composting Bin (Priority: P2)

As a player, I want to place organic waste into a composting bin machine to produce compost so that I have a renewable source of soil restoration.

**Why this priority**: This creates the early-game progression loop (collect waste → produce compost → restore soil) but is not required for the core decay/restoration mechanic to function.

**Independent Test**: Can be fully tested by adding organic waste to a composting bin, waiting the configured processing time, and collecting the resulting compost item.

**Acceptance Scenarios**:

1. **Given** an empty composting bin machine, **When** the player adds organic waste, **Then** the composting bin begins processing and the waste is consumed
2. **Given** a composting bin that has been processing for the required duration, **When** the player interacts with it, **Then** the player receives compost items and the composting bin becomes empty
3. **Given** a composting bin that is still processing (time not yet elapsed), **When** the player interacts with it, **Then** no compost is produced and the player receives feedback that processing is incomplete
4. **Given** a composting bin is full (has uncollected compost), **When** the player attempts to add more waste, **Then** no waste is accepted until the compost is collected
5. **Given** the player has no organic waste in inventory, **When** the player interacts with an empty composting bin, **Then** no action occurs

---

### Edge Cases

- What happens when soil health is already at 0? (No further decay below 0)
- What happens when soil health is at 100 and compost is applied? (No increase above 100, compost not consumed)
- How does the system handle a composting bin mid-processing when the player reloads a save? (Resolved: input timestamp is persisted; elapsed time calculated on load)
- What happens when the player applies compost to a tile they do not own (NPC's garden)? (Resolved: compost rejected without consumption; only player's farm + Greenhouse allowed)
- How does decay interact with tiles that transition from cropped to bare within the same day? (Decay applies based on tile state at day start; mid-day transitions don't trigger until next day)
- What happens when multiple composting bins exist on the farm simultaneously? (Resolved: each composting bin operates independently with its own maturation level and processing state)
- What happens when a composting bin is broken/removed? (Resolved: unfinished waste and ready compost drop as items on the ground; maturation is lost)
- How does the system determine if a tile is "bare" for decay purposes? (Resolved: bare = no living crop AND no dead crop residue; residue acts as mulch protecting soil)

## Clarifications

### Session 2026-09-06 (current)

- Q: Should the mod's player-facing strings (maturity tooltip "Maturity: 3x", floating restoration text "+15", audio cue names) use Stardew Valley's localization system (`I18n`) to support multiple languages, or are hardcoded English strings acceptable for v1? → A: Full `I18n` localization from the start — author all strings in `.json` files, access via `I18n.Key`. Supports all languages immediately.
- Q: Should the decay and composting systems include debug-level logging (via `IMonitor`) for operations like tile decay counts, composting bin state changes, and compost applications? → A: Debug-level logging for state-changing operations only — decay summaries (tiles processed, total health lost), bin transitions (empty→processing→ready), compost applications. Uses `IMonitor` with `LogLevel.Trace`/`Debug` so it doesn't spam production logs.
- Q: When the player successfully applies compost and "+15" floating text should appear, which rendering approach should the system use? → A: `TemporaryAnimatedSprite` with the `text` field — renders "+15" directly above the tile in world space. Standard Stardew Valley convention for in-world stat changes. Well-supported in multiplayer via `Game1.multiplayer.broadcastSprites`.
- Q: Should the composting bin's maturation tooltip ("Maturity: 3x") be displayed using Stardew Valley's standard hover tooltip pattern or via a custom `ICursorTooltip` event registration? → A: Standard machine hover tooltip — uses `Data/Machines` built-in hover text support. No custom code, automatic vanilla consistency.
- Q: When the composting bin is processing, should the ambient sound loop continuously for the entire processing duration, or only play intermittently? → A: Intermittent playback — brief bubbling sound every 2-3 in-game hours during processing. Reduces noise pollution while maintaining feedback. The "ready" chime remains a single distinct event when processing completes.

### Session 2026-09-09

- Q: What feedback should the player receive when attempting to apply compost to an invalid target (non-tilled tile, max health, NPC garden)? → A: Subtle "cancel" audio cue (no visual). Matches Stardew Valley's convention for "action cannot be completed" and aligns with how vanilla machines silently reject invalid inputs. The mod already provides rich visual feedback for valid actions; adding visual noise for invalid ones would deviate from the game's minimalist feedback style.
- Q: What is the crafting recipe for the composting bin machine? → A: Preserves Jar tier: 50 Wood + 25 Stone + 15 Fiber. Available from day 1, matches comparable processing machine tier. Future evolutions (not implemented now) will use more materials.
- Q: What is the canonical name for this machine? → A: "Composting Bin" (not "Composter"). Early game item with planned future evolutions/upgrades (deferred to future roadmap).
- Q: How should the system identify which items are valid "organic waste" inputs for the composting bin? → A: Hybrid approach using Stardew Valley's built-in category IDs (-74 Seeds, -75 Vegetables, -79 Fruits, -80 Flowers, -81 Forage/Greens) combined with a custom `compostable_item` context tag for mod extensibility. Machine `RequiredTags` accept any item matching these categories OR the custom tag. Vanilla items work automatically; mod authors add `compostable_item` to make their items compatible. Additionally, a `not_compostable` exclusion tag prevents specific items from being composted (e.g., valuable items like Ancient Fruit, quest items, or modded items that shouldn't be processed). Items with `not_compostable` tag are rejected even if they match a category. Future planned feature (not now): "Cooking Waste" byproduct item from cooking actions that also feeds the composting system.
- Q: How should the composting bin communicate its current state (processing progress, maturation level) to the player? → A: Standard Stardew Valley machine conventions: machine shows input sprite while processing, switches to "ready" visual (bubbling animation or output sprite) when complete. For maturation, tooltip on hover shows current output multiplier (e.g., "Maturity: 3x"). Matches how players already understand machines from vanilla gameplay.
- Q: Should the soil health decay system apply to tilled soil in the Greenhouse, or should the Greenhouse be exempt? → A: Greenhouse is exempt from decay. Matches vanilla Stardew Valley's "controlled environment" expectation where soil never untills or loses fertility in the Greenhouse. Players expect it to be a permanent safe zone. Compost application is still allowed in the Greenhouse (for restoring health after manual tilling), but no daily decay occurs.
- Q: Should the composting bin be compatible with the Automate mod? → A: Implement using standard `Data/Machines` patterns — Automate compatibility is automatic since Automate reads `Data/Machines` natively. No custom integration code needed. Follows YAGNI principle.
- Q: Should the composting bin produce any sound effects during processing or when ready for collection? → A: Subtle ambient sound while processing (low bubbling/composting hum) and a distinct "ready" chime when compost is finished. Matches Stardew Valley's convention where machines like furnace and keg produce ambient sounds. The "ready" audio cue helps players know when to collect without visually checking every machine.

### Session 2026-09-06

- Q: What is the exact daily decay rate for bare tilled soil? → A: 2 health points per day. Small enough to not punish casual players, large enough to matter over a season (≈28 points/month).
- Q: How much health does one application of compost restore? → A: 15 health points per compost application. Meaningful restoration that rewards the composting effort.
- Q: What is the composting bin processing time? → A: 2 full days from input to output. Creates a planning horizon for players.
- Q: What items qualify as "organic waste" for the composting bin? → A: Stardew Valley's existing "Seeds" category items (wild seeds, tree seeds) and "Vegetable" category items that are not artisan goods. Uses existing game categorization.
- Q: Should the composting bin preserve processing progress through save/load? → A: Persist the input timestamp; on load, calculate elapsed time from saved timestamp to current time. Standard Stardew Valley pattern (like preserves boxes and kegs).
- Q: What feedback should the player see when compost is applied? → A: Floating text showing the restoration amount (e.g., "+15") in green above the targeted tile. Matches Stardew Valley's convention for stat changes.
- Q: Should decay vary by season? → A: Seasonal multipliers apply: 1.5x in summer (heat stress), 0.5x in spring/fall (mild weather), 0 in winter (soil frozen). Adds strategic depth to the farming calendar.
- Q: Should the composting bin have a maturation mechanic? → A: Yes. Starts at 1:1 output (1 waste → 1 compost). Each week of continuous operation increases output by +1 (week 2 = 2 compost, week 3 = 3 compost), up to a maximum of 5x.
- Q: When should composting bin maturation reset? → A: Reset to 1x after 14 consecutive days of being empty (no input, no output). Forgiving but rewards consistent use.

### Session 2026-09-08

- Q: Roughly how many tilled soil tiles should the decay system handle on a single farm before performance becomes a concern? → A: Up to 10,000 tiles (extreme modded scenarios). Research confirms even 40,000 tiles scan in ~2-4ms with a simple linear iteration, so this target is easily achievable without spatial partitioning.

### Session 2026-09-07

- Q: Should a composting bin process only one waste item at a time, or accept multiple wastes that queue up? → A: Single waste processed at a time; player must wait for completion before adding next. Matches Stardew Valley's keg/preserves jar pattern and keeps the data model simple (single timestamp per composting bin).
- Q: When multiple composting bins exist, is maturation tracked independently or shared globally? → A: Each composting bin tracks its own maturation level independently. Creates interesting player decisions and matches Stardew Valley's per-machine treatment.
- Q: Can players apply compost to any tilled soil (including NPC gardens) or only the player's farm? → A: Only tiles on the player's farm and Greenhouse (locations where IsFarm == true OR Name == "Greenhouse"). Prevents edge case bugs and matches base game convention that soil mechanics are farm-only systems.

### Session 2026-09-06 (continued)

- Q: Should the composting bin be implemented as a crafting machine (keg-style), farm building (shed-style), or object/prop (scarecrow-style)? → A: Crafting machine (keg-style). Player crafts it and places it like a keg on any farm tile. Future NPC blueprint gating is planned but for now it is available from day one.
- Q: How should decay determine if a tile is “bare” — should dead crop residue, fertilizer, or weeds affect the determination? → A: No crop AND no residue = bare. Tilled tiles with dead/dried crops are protected (mulch effect, matching real agroecology where crop residue shields soil). Fertilizer alone does not exempt decay. More realistic and adds strategic depth (leaving residue protects soil but occupies the tile).
- Q: When the player right-clicks with compost, how should the system find the target tile? → A: Use the tile under the mouse cursor at the moment of right-click. Standard Stardew Valley interaction pattern (same as hoe, watering can).
- Q: How should the player interact with the composting bin machine to add waste and collect compost? → A: Equip-to-add, empty-collect pattern (standard Stardew machine). Hold organic waste + right-click composting bin → adds waste (if empty and item is valid organic waste). Right-click with empty hands → collects finished compost (if ready). Matches Furnace/Keg/Preserves Jar interaction model.
- Q: What happens when a player breaks/removes a composting bin machine? → A: Breaking the composting bin drops the unfinished waste AND any ready compost as items on the ground. Maturation resets (machine is gone). Matches how breaking a Keg with something inside works in Stardew Valley.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST decrease soil health by the configured decay rate multiplied by the seasonal multiplier for all tilled tiles that have no living crop AND no dead crop residue at the start of each new day. Greenhouse tiles are exempt from decay (controlled environment).
- **FR-002**: System MUST NOT decrease soil health below 0 (hard floor)
- **FR-003**: System MUST NOT decay tiles that have a living crop, dead crop residue (mulch protection), fruit tree, or any plant present. Fertilizer alone does NOT exempt a tile from decay
- **FR-004**: System MUST increase soil health by the configured restoration amount when compost is applied to a tilled tile
- **FR-005**: System MUST NOT increase soil health above 100 (hard ceiling)
- **FR-006**: System MUST consume one compost item per successful application to a tilled tile
- **FR-007**: System MUST NOT consume compost when applied to non-tilled tiles or tiles already at maximum health
- **FR-008**: System MUST process organic waste in a composting bin machine over a configured duration to produce compost items, with output quantity determined by the composting bin's maturation level (see FR-013). Each composting bin MUST accept only one single waste item at a time (consuming exactly 1 item from the player's inventory per interaction); additional waste cannot be added until current processing completes. Interaction MUST follow standard Stardew Valley machine pattern: hold organic waste + right-click to add (if empty), right-click with empty hands to collect finished compost (if ready). Valid organic waste items are identified by Stardew Valley's built-in category IDs: -74 (Seeds), -75 (Vegetables), -79 (Fruits), -80 (Flowers), -81 (Forage/Greens), OR by the custom `compostable_item` context tag. Items with the `not_compostable` tag MUST be rejected even if they match a category.
- **FR-009**: System MUST persist composting bin state through save/load cycles by storing the input timestamp; on load, elapsed time is calculated from the saved timestamp to the current game time (standard Stardew Valley machine pattern)
- **FR-010**: System MUST persist soil health values through save/load cycles (existing behavior, verified by this feature)
- **FR-011**: System MUST trigger decay calculation once per day at day start, not per frame or per tick
- **FR-012**: System MUST display floating text showing the restoration amount (e.g., "+15") in green above the targeted tile when compost is successfully applied. Uses `TemporaryAnimatedSprite` with the `text` field to render in world space. Text MUST remain visible for `ModConstants.TextDurationMs` (1000ms) with a linear fade-out (`alphaFade = 0.0167f`). Multiplayer-compatible via `Game1.multiplayer.broadcastSprites`
- **FR-013**: System MUST track composting bin maturation independently per machine (weeks of continuous operation) and increase output accordingly, up to a maximum of 5x. Maturation resets to 1x after 14 consecutive days of being empty.
- **FR-014**: System MUST persist composting bin maturation level through save/load cycles alongside the input timestamp
- **FR-015**: System MUST restrict compost application to tiles on the player's farm or Greenhouse (locations where `IsFarm == true` OR `Name == "Greenhouse"`). Compost applied elsewhere MUST be rejected without consumption.
- **FR-016**: When a composting bin machine is broken/removed, System MUST drop any unfinished waste input AND any ready compost output as items on the ground at the bin's tile position. Maturation level is lost (machine is gone). Matches Stardew Valley's standard machine break behavior.
- **FR-017**: System MUST play a subtle "cancel" audio cue when the player attempts to apply compost to an invalid target (non-tilled tile, tile at max health, or NPC garden). No visual feedback is shown for invalid targets — matches Stardew Valley's minimalist feedback convention
- **FR-018**: System MUST register a crafting recipe for the Composting Bin requiring 50 Wood + 25 Stone + 15 Fiber, available from day one (default unlock condition, Home crafting tab)
- **FR-019**: System MUST display the composting bin's current state using standard Stardew Valley machine visuals: show input sprite while processing, switch to "ready" visual (output sprite) when complete. On hover, a tooltip MUST show the current maturation multiplier (e.g., "Maturity: 3x"). Uses standard `Data/Machines` built-in hover text support — no custom `ICursorTooltip` event registration needed.
- **FR-020**: System MUST play a subtle ambient sound (e.g., low bubbling/composting hum) intermittently (every 2-3 in-game hours) while the composting bin is processing, and a distinct "ready" chime when compost is finished and ready for collection. Intermittent playback prevents noise pollution with multiple bins.
- **FR-021**: System MUST author all player-facing strings (maturity tooltip, floating restoration text, audio cue names, machine display names) using Stardew Valley's `I18n` localization system. All strings live in `.json` files and are accessed via `I18n.Key` calls. Supports multiple languages from v1.
- **FR-022**: System MUST log state-changing operations at `Trace`/`Debug` level via `IMonitor`: daily decay summaries (tiles processed, total health lost), composting bin state transitions (empty→processing→ready), and compost applications (tile position, amount restored). Uses `LogLevel.Trace` or `Debug` to avoid production log spam.

### Key Entities

- **Soil Health Tile**: A tilled soil position with an associated health value (0-100). Decays when bare, restorable with compost. Uniquely identified by tile position (X, Y).
- **Compost Item**: A consumable item that restores soil health when applied to tilled soil. Produced by the composting bin machine.
- **Composting Bin**: A farm building that accepts one organic waste item at a time and produces compost after a configured processing duration. Has states: Empty, Processing, Ready. Has a maturation level that increases output over time (1:1 initially, +1 per week of continuous operation, up to 5x max). Maturation resets after 14 days idle. Each composting bin tracks maturation independently.
- **Organic Waste**: Input items accepted by the composting bin machine for conversion to compost. Identified by Stardew Valley category IDs: -74 (Seeds), -75 (Vegetables), -79 (Fruits), -80 (Flowers), -81 (Forage/Greens), OR by the custom `compostable_item` context tag (for mod compatibility). Items with the `not_compostable` tag are excluded regardless of category. Artisan goods are excluded.
- **Decay Rate**: Configurable constant defining daily health loss for bare tilled tiles (default: 2). Modified by seasonal multiplier.
- **Seasonal Decay Multiplier**: Configurable multipliers per season: Summer 1.5x, Spring 0.5x, Fall 0.5x, Winter 0x (no decay).
- **Restoration Amount**: Configurable constant defining health gained per compost application (default: 15).

## Technical Constraints

### Technology Stack
- **Platform**: .NET 6 with C# latest language version, targeting Stardew Valley via SMAPI
- **Game Integration**: SMAPI's `IModHelper.Events` system for day start events and input handling
- **Persistence**: `IModDataService` for JSON save data (soil health, composting bin state)
- **Item System**: Stardew Valley's item system for compost creation and inventory management
- **Localization**: Stardew Valley's `I18n` system for all player-facing strings (`.json` files in `i18n/` directory, accessed via `I18n.Key`). Supports multiple languages from v1.

### Performance & Scale
- **Tile Count**: System is designed to handle up to 10,000 tilled tiles per farm with a simple linear scan at day start. Even extreme modded scenarios (40,000+ tiles) complete in under 5ms, well within a single frame budget. No spatial partitioning or caching required.

### Game Integration
- **Day Start Event**: Use `IDayStartedWritableAPI` event to trigger decay calculation
- **Input Handling**: Use `IInputEvents.ButtonPressed` to detect right-click compost application
- **Machine Placement**: Composting Bin is a crafted machine (keg-style) placed on farm tiles, not a carpenter-menu building. Available from day one (future NPC blueprint gating deferred).
- **Item Factory**: Use `ItemRegistry` to create compost items with a unique mod item ID
- **Automate Compatibility**: Composting Bin uses standard `Data/Machines` patterns, making it automatically compatible with the Automate mod. No custom integration code required.

### Crafting Recipe
- **Recipe**: `50 Wood + 25 Stone + 15 Fiber` (Preserves Jar tier — comparable processing machine)
- **Format**: `"LivingRoots_CompostingBin": "388 50 390 25 771 15/Home/CompostingBin/true/default/"`
- **Unlock Condition**: `default` (known from day one, no skill or friendship requirements)
- **Category**: `Home` crafting tab
- **Future Evolutions**: Planned upgrades (not implemented now) will use more materials and higher-tier components

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Bare tilled soil loses exactly the configured decay rate (default 2) multiplied by the seasonal multiplier, verified across all four seasons
- **SC-002**: Compost application restores exactly the configured amount (default 15) health points per use, verified across 10 applications
- **SC-003**: Soil health never exceeds the 0-100 bounds under any combination of decay and restoration
- **SC-004**: Composting Bin output increases with maturation: 1:1 at week 1, 2:1 at week 2, up to 5:1 max after 5 weeks of continuous operation
- **SC-005**: Save/load cycles preserve all soil health values and composting bin states with zero data loss
- **SC-006**: Day start decay processes all tilled tiles within a single frame (no visible lag or staggered updates)
- **SC-007**: Players can identify soil health changes within 1 day of gameplay (decay visible, compost effect immediate)

## Assumptions

Each assumption includes a validation mechanism to confirm correctness before and during implementation.

- Soil health values are already persisted and accessible from the existing save system. **Validation**: Verify by reading SoilHealthService implementation and running existing save/load tests.
- Tilled soil tiles can be identified through existing game state queries. **Validation**: Confirm via Stardew Valley API for tile state queries.
- The existing events system provides day start and input button events. **Validation**: Confirmed via SMAPI documentation and existing ModController event subscriptions.
- The existing constants structure will be extended with decay/compost default values. **Validation**: Confirmed by code review — constants class is designed for extension.
- Organic waste categorization maps to existing Stardew Valley item category flags. **Validation**: Verify by inspecting item data definitions for seeds and vegetables.
- Composting Bin can be implemented as a custom building using existing building placement patterns. **Validation**: Research Stardew Valley building implementation during Phase 0.
- Single-player game context only (no multiplayer sync required). **Validation**: Confirmed by design — all state is local per save file.

## Dependencies

- Requires soil health save/load system to be functional (existing)
- Requires existing `ModController` event registration infrastructure
- Requires existing `ModEntry` composition root pattern for DI wiring
- Requires visualization system (PR #73) for health value display (decay effect visible to player)
- Relies on data service for persistence

## Out of Scope

- Compost quality tiers (basic compost only in v1)
- Composting Bin evolutions/upgrades (planned for future roadmap, not implemented now)
- Automated compost application (must be manual player action)
- Compost trading or selling (player use only)
- Soil health effects on crop yield (future feature)
- Fertilizer items distinct from compost (future feature)
- Composting Bin placement restrictions (can be placed anywhere on farm)
