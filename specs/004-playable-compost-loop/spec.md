# Feature Specification: Playable Compost Loop

**Feature Branch**: `004-playable-compost-loop`

**Created**: 2026-10-03

**Status**: Draft

**Input**: Handoff from `.specify/assessments/next-feature-80-20/decision.md` (verdict: `go`, Option A
— Make It Playable First), with the maintainer's clarification that Principle VI requires only that no
new feature be designed single-player-only.

## Problem

The mod reports success while being unplayable. Its automated suite passes completely — 816 tests,
0 failures — yet a player cannot obtain compost, cannot use a composting bin, and cannot see soil
health change, because the compost and bin items are never actually introduced into the game. The
registered crafting recipe points at an item that does not exist, and the soil health display never
receives data to draw. No prioritization decision can be trusted while the project's primary signals
describe a product that is not there.

## Clarifications

### Session 2026-10-03

- Q: Now that the bin must be a native game machine, how should the existing rule that a matured bin
  yields between one and five compost items be handled? → A: One in, one out; retire the 1–5 multiplier.
  A native machine performs the whole batch, so the mod never resizes or manufactures the output.
- Q: When a player cannot take compost — because their inventory is full, or because they pick up a bin
  that is still processing — should the game hold on to the goods until the player can collect them, or
  let them be lost? → A: The game keeps the goods until the player collects. Nothing is destroyed; a
  blocked collection is retried later, and a moved bin carries its contents and progress with it.
- Q: Which items should count as compostable organic waste, and should this spec write that list down
  itself instead of leaving it to the earlier compost specification? → A: Inherit by reference. The
  qualifying set stays owned by the soil decay and compost specification; this feature neither restates
  nor extends it.
- Q: When the mod has to tell the player why something was refused — compost on an unsuitable tile, or a
  collection that could not complete — how should that message actually reach them? → A: Through the
  game's own on-screen messages, in the game's font and the player's own language. This feature builds
  no user interface of its own.
- Q: When two players on the same farm act on the same composting bin or the same soil tile at the same
  time, which action should take effect? → A: The first action to take effect wins. The second player's
  attempt is refused and explained, never silently overwritten.

### Session 2026-10-04

- Q: Can compost be applied to tilled tiles that contain an active growing crop? → A: Yes, compost top-dressing/side-dressing on active crops is permitted and nourishes the living roots without harming or removing the crop.

### Session 2026-10-13

- Q: How should baseline (health 0) soil appear visually? → A: Neutral vanilla dirt appearance without color overlay to avoid cognitive visual clutter and alert fatigue.
- Q: What exact incremental value is added to soil health per compost application? → A: Exactly 15 points per application (from ModConstants.RestorationAmount), clamped to 100.
- Q: What are the exact 8 stale specification and plan documents to be reconciled under FR-017? → A: specs/001-soil-health-visualization/{spec,plan}.md, specs/002-soil-decay-compost/{spec,plan}.md, specs/003-composting-tests/{spec,plan}.md, docs/design-system/REALITY-CHECK-EVALUATION.md, and docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md.
- Q: Which document and label taxonomy must be used for unobservable feature claims? → A: README.md under ## Features, with unbuilt capabilities explicitly labelled with the suffix `(Planned)`.
- Q: Should composting bin processing duration be 1 in-game day or 2 in-game days? → A: 2 in-game days (MaturationDays = 2). Harmonizes User Story 2 with FR-001b, Constants.cs, and earlier specs.
- Q: How should an invalid item drop-in attempt on a composting bin be signalled? → A: Vanilla machine rejection (audio cancel cue / machine shake). FR-008a HUD text messages are scoped to tile compost application, full inventory on harvest, and concurrency collisions.
- Q: How should a bin with contents/progress behave when picked up into inventory? → A: Progress is serialized into Item.modData (e.g. LivingRoots.InputId, LivingRoots.DaysRemaining) with non-stackable enforcement (max stack 1 while carrying progress). Processing is paused while unplaced.
- Q: How should multiplayer concurrency criteria (SC-005, SC-010) be verified in Feature 004? → A: Automated multi-threaded test stubs and event simulations in LivingRoots.Tests. Physical two-player live verification remains linked to Roadmap item D1.
- Q: How should refusal text and localization be delivered under FR-008a? → A: Wire SMAPI ITranslationHelper with translation keys in i18n/default.json displayed via Game1.showRedMessage(...).
- Q: How is soil health visually differentiated across the 0–100 scale under weather and lighting? → A: Inherits Feature 001's 3-tier continuous RGB interpolation (Poor: 1–33, Moderate: 34–66, Healthy: 67–100) rendered under ambient lighting, with health 0 remaining unrendered neutral dirt. Overlays blend under ambient light without clipping or washing out during night, rain, and winter.
- Q: How much compost is consumed per application action? → A: Exactly one compost item is decremented from the player's active inventory stack upon successful application.
- Q: What are the spike exit criteria and fallbacks for item and machine registration? → A: Programmatically register compost and bin via SMAPI AssetRequested in C# against Data/Objects, Data/CraftingRecipes, and Data/Machines. If native Data/Machines cannot preserve Item.modData progress across pickup/placement, fallback to SMAPI event-interception (ModController) with vanilla data declarations.
- Q: How are concurrent player actions on a tile or bin resolved? → A: Host-authoritative atomic locking per tile coordinate/machine instance. The first action mutates state; the second action executes against the updated state, is refused if invalid (e.g. soil already at 100), and preserves inventory items.
- Q: How should corrupted, obsolete, or disconnected bin save entries from previous versions be quarantined without crashing loads or duplicating machines? → A: Tolerant reader with per-entry fault isolation: try-catch per record, ignore obsolete legacy fields (MaturationLevel, ConsecutiveActiveDays, ConsecutiveIdleDays), skip/quarantine corrupt records with sanitized logs (FR-015), and prune save entries where no physical bin exists in the world (FR-002) to prevent machine duplication.
- Q: How should farmhand visual overlay synchronization be handled without polling lag or desync? → A: Reactive event-driven cache invalidation: ISoilHealthService exposes OnTileHealthChanged, which VisualizationService subscribes to for immediate (≤16.67ms) overlay invalidation with 0 polling latency. Multi-client verification in Feature 004 is bounded to automated event-listener stubs in LivingRoots.Tests; live network packet transport is scheduled for Roadmap D1.
- Q: What happens to a placed bin containing progress if dislodged by explosions or tools with full inventory? → A: The bin is dislodged as a single unstackable item carrying Item.modData progress. If inventory is full, it drops as a floating world debris item retaining its modData.
- Q: What semantic translation keys are used for refusal messages? → A: Standard keys: message.compost.invalid-tile, message.compost.max-health, message.compost.action-conflict, and message.harvest.inventory-full.
- Q: Is the item-registration route (native SMAPI AssetRequested vs ContentPatcher) still unsettled for this feature? → A: Settled — native SMAPI AssetRequested (current ModEntry.OnAssetRequested, T053 completed, no ContentPatcher dependency). FR-001a native-machine requirement forces this; the earlier assumption that the route was unsettled is stale documentation, not open design.
- Q: How should multiplayer concurrency verification (SC-010 / FR-019 / Constitution VI) be recorded as a completion boundary? → A: Formal "stub-bounded + live D1 required" boundary. 004 verifies concurrency logic via automated multi-threaded stubs / event simulations; live 2-player verification remains owed to roadmap D1; 004 MUST NOT be declared complete for multi Gate 3 until D1 runs (spec Assumptions — multiplayer verification boundary; Constitution Principle VI non-negotiable). CHESS/CTrigger/Jepsen evidence confirms stubs systematically miss interleavings/network semantics stubs cannot reproduce.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Obtain and place a composting bin (Priority: P1)

As a player starting the mod, I craft a composting bin from the recipe I can see in my crafting menu
and place it on my farm, so that I have somewhere to put organic waste.

**Why this priority**: nothing else in this feature can be attempted until the bin exists in the game.
Today the recipe is listed but the item it produces is absent, so the very first step of the loop
fails. This is the single blocking defect.

**Independent Test**: launch the mod, open the crafting menu, craft the composting bin, place it on
farmable land. Verifiable by one player in under two minutes with no other feature present.

**Acceptance Scenarios**:

1. **Given** a save with the mod active, **When** the player opens the crafting menu, **Then** the
   composting bin recipe is listed and craftable.
2. **Given** the player has the crafted bin in inventory, **When** they place it on valid farm land,
   **Then** it appears in the world and can be picked back up.
3. **Given** the player attempts to place the bin on land that cannot hold it, **When** placement
   fails, **Then** the game refuses without leaving a stray object.

---

### User Story 2 - Turn organic waste into compost (Priority: P1)

As a player, I load my composting bin with organic material and return after two in-game days to collect compost,
so that I have a usable fertilizer.

**Why this priority**: this is the feature's reason to exist — it is the entire name of the mod. It
also cannot be tested until User Story 1 delivers a bin, and the compost it yields must be a real item
in its own right.

**Independent Test**: place a bin, add organic material, advance two in-game days, collect the result.
The collected item must exist in the player's inventory and be identifiable as compost.

**Acceptance Scenarios**:

1. **Given** an empty placed bin, **When** the player adds qualifying organic material, **Then** the
   bin accepts it and moves out of its empty state.
2. **Given** a bin that has processed for two days, **When** the player collects from it, **Then** compost is added
   to their inventory and the bin returns to its empty state.
3. **Given** a bin that has not finished processing (e.g. after one day), **When** the player tries to collect, **Then**
   nothing is produced and the bin is left untouched.
4. **Given** a bin holding organic material, **When** the in-game day advances, **Then** the bin's
   processing advances exactly once for that day.

---

### User Story 3 - See soil health change on the ground (Priority: P2)

As a player who has applied compost, I can see that the soil I treated is richer than the soil around
it, so that the effect of my work is visible without reading numbers.

**Why this priority**: applying compost already adjusts soil health internally, but the player has no
way to observe it. This is the difference between a mechanic that exists and a mechanic that is
experienced. Priority is below the bin and compost because it delivers no new capability — it makes an
existing one legible.

**Independent Test**: apply compost to a tilled tile, then look at that tile in the world and confirm
its soil state is visibly distinguishable from an untreated neighbour.

**Acceptance Scenarios**:

1. **Given** a tilled tile with room to improve, **When** the player applies compost to it, **Then** the
   tile's soil health increases and the increase is visible on that tile.
2. **Given** a tile whose soil cannot be improved further, **When** the player applies compost, **Then**
   the value does not exceed its maximum and the player is told why nothing changed.
3. **Given** a tile that is not suitable for compost, **When** the player attempts to apply it, **Then**
   the attempt is refused with a visible reason and no compost is consumed.
4. **Given** the soil health display is turned off in settings, **When** the player applies compost,
   **Then** the soil still improves and only the visual indication is absent.

---

### User Story 4 - Trust what the project says about itself (Priority: P3)

As the person maintaining this mod, I read documentation that tells me what actually works, so that I
can decide what to build next without being misled by green test counts.

**Why this priority**: this is the problem the whole feature exists to solve — untrustworthy signals.
It is last because it is cheapest and least risky, and it can only be written accurately after
Stories 1-3 have actually been played.

**Independent Test**: read the project's user-facing feature list and the specifications, and confirm
every stated capability either works or is explicitly labelled as not yet working.

**Acceptance Scenarios**:

1. **Given** a reader opens the project's feature list, **When** they read a capability claim, **Then**
   that claim corresponds to behaviour they can observe in the game.
2. **Given** a reader opens a specification marked complete, **When** they check its capability in the
   game, **Then** it is reachable.
3. **Given** a reader checks the project's multiplayer scope, **When** they compare it against the
   project's constitution, **Then** they find no contradiction.

---

### Edge Cases

- Full-inventory harvest: player tries to collect ready compost with full inventory; compost must remain in bin (FR-005b); refusal message shown; no partial loss.
- Dislodgement mid-batch with full inventory: bin removed (axe/pickaxe/hoe/explosion); progress + input preserved in Item.modData (FR-005c); if no inventory space, floats as debris with same modData; placed back restores progress.
- Emptying a ready bin: must yield exactly 1 compost; no leftover progress; bin returns to Empty.
- Concurrent action on same bin/tile: first wins; second refused with message; no state change (FR-019).

- The player's inventory is full when collecting compost, or when a crafted bin would be added.
- Compost is applied to untilled soil, to a growing crop, to a building, or to indoor tiles.
- A tile's soil health is already at its maximum when compost is applied.
- The bin is emptied, moved, or picked up while it is mid-processing, or dislodged via bomb/explosion.
- The bin is dislodged with a tool or explosion while the player's inventory is completely full (drops as floating world debris retaining `Item.modData`).
- The day advances while the bin is processing and the player is not on the farm.
- The game is saved and reloaded at every stage of the bin's lifecycle.
- The day advances on a multiplayer farm where more than one player is connected, or when a farmhand disconnects/reconnects mid-day (host-authoritative state remains intact).
- Two players independently or simultaneously act on the same tile or the same bin (serialized by atomic lock; first wins, second evaluates updated state and refuses if invalid).
- The soil health display is toggled on for the first time after items already exist, or toggled during adverse weather/nighttime conditions.
- A tile's soil health returns to its default value (0) and must display as neutral vanilla dirt without visual overlay.
- A save file contains corrupt, malformed, or legacy bin entries from earlier versions (tolerantly quarantined/skipped per-entry; orphaned entries without matching world objects are pruned to prevent duplicates).
- A farmhand client observes a tile when soil health is mutated by another player (overlay cache invalidates immediately on reactive notification and re-renders on the next frame without polling lag).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The composting bin MUST be obtainable through the crafting recipe the mod already
  registers, and MUST exist as a real, placeable object in the game world.
- **FR-001a**: The composting bin MUST behave as a **native game machine**, using the game's own
  machine interface, progress display, and processing rules, rather than a bespoke interface supplied
  by this mod. A player who has used another game's machine must recognise this one without instruction.
  Asset-registration details (`Triggers`, `DaysUntilReady = 2`, output quantity `1`) are owned by
  `contracts/contracts.md` §1.3 — authoritative asset-registration spec; spec/plan reference it only,
  never redefine. Timing is governed by FR-001b; output quantity by FR-005a.
- **FR-001b**: The bin's processing MUST follow the game's own machine timing (2 days, `MaturationDays`), and MUST NOT be accelerated, shortened, or completed by this mod. Implementation preferred: native `Data/Machines` rules (`Triggers` + `DaysUntilReady`). **Fallback permitted** (Assumptions): event-interception via `OnDayStarted` with an exactly-once guard (`Interlocked.CompareExchange` on a dedicated decrement flag; guard must pass before decrement), without changing duration or quantity. The fallback is accepted behaviour, not a violation. Exactly-once guard implemented via Interlocked.CompareExchange on a dedicated decrement flag (separate from the concurrent-action first-wins lock flag). The guard passes first; only then is DaysRemaining decremented. If the guard fails, no decrement occurs. Fallback event-interception via OnDayStarted with guard check.
- NOTE: FR-001a defines interface; FR-001b defines timing/fallback. Consolidated: native machine preferred (FR-001a); fallback event-interception (FR-001b) only if native Data/Machines fails. No redesign of either.
- **FR-002**: The composting bin MUST be placeable on farm land, **the Greenhouse, and Ginger Island farm** (invalid terrain: water, buildings, non-farm interiors), removable, and MUST NOT leave
  orphaned state in the save data when removed. Rejected placement MUST NOT leave a ghost object.
- **FR-003**: The bin MUST accept qualifying organic material by player action and MUST reject
  non-qualifying material. **The qualifying set is owned by the soil decay and compost specification and
  is inherited from it unchanged. VERIFICATION: T017 confirms qualifying set matches spec 002 (artisan goods excluded unless 002 updated); property-based invariant ForAll(item => IsCompostable(item) == ExpectedFromSpec002(item)) enforced in LivingRoots.Tests/CompostingBinServiceTests.cs.** This feature MUST NOT restate, extend, narrow, or contradict that
  set; changing it is a change to that specification, not to this one.
- **FR-004**: Advancing one in-game day MUST advance bin processing exactly once, regardless of how many
  players are connected to the farm.
- **FR-005**: A bin that has finished processing MUST yield compost that exists as a real in-game item
  with a player-visible name, icon, and stack behaviour.
- **FR-005a**: One completed batch MUST yield **exactly one** compost item. The maturation-level
  multiplier settled in the soil decay and compost specification — which yielded between one and five
  compost items according to how long a bin had been kept busy — is **superseded by this feature and
  MUST NOT be implemented**. The quantity of a batch's output is never adjusted, scaled, or
  accumulated by this mod.
  COMPOST ITEM STACK: `LivingRoots.Compost` defines `Stack = 999` (standard item stack) per contracts/contracts.md §1.1; bin output always produces 1 item regardless of stack; no accumulation. Verified by T021 (`Data/Machines` declaration) and T012/T013 (asset registration).
- **FR-005b**: Finished compost MUST NOT be destroyed, discarded, or silently dropped. When the player
  cannot receive a completed batch — a full inventory being the ordinary cause — the compost MUST remain
  in the bin, MUST be delivered in full on a later player-initiated harvest attempt (OnButtonPressed / right-click), and MUST NOT be partially lost, duplicated,
  or credited twice. The player MUST be given a visible reason at the moment collection does not
  complete.
- **FR-005c**: A bin that is emptied, moved, or picked up while it holds organic material or ready compost MUST retain
  that material and its processing progress. The bin object in inventory MUST serialize remaining progress and input data into its `Item.modData` (e.g. `LivingRoots.InputId`, `LivingRoots.DaysRemaining`), MUST enforce a maximum stack size of 1 while carrying progress or contents, and MUST pause processing while unplaced. Removing a bin via tools (axe, pickaxe, hoe) or environmental explosions MUST destroy neither its contents nor its progress, and MUST leave no orphaned state behind (FR-002). If inventory space is unavailable upon dislodgement, the bin MUST drop as a floating world debris item retaining its `Item.modData`; persistence rule: debris is written to `GameLocation.Objects` with the same `Item.modData` key and pruned on save-load only when no matching placed bin exists (FR-002 / FR-012). Placing a bin back down MUST restore the progress it had, so that a bin cannot be used to reset or shortcut processing.
- **FR-006**: The compost item MUST be obtainable by the player through the bin and MUST NOT exist
  through any path that bypasses the bin's processing rules.
- **FR-007**: Applying compost to a valid, improvable tile MUST consume exactly one compost item from the player's active inventory stack and MUST increase that tile's soil health by exactly 15 points (clamped to 100). A valid, improvable tile is a tilled dirt tile (`HoeDirt`) on farm land, in the Greenhouse, or on Ginger Island farm whose current soil health is under 100, regardless of whether a living crop is planted on it. Compost application on actively growing crops acts as side-dressing/top-dressing and MUST NOT damage or remove the crop.
- **FR-008**: Applying compost MUST be refused, with a player-visible reason and without consuming the
  compost, when the target is not a valid improvable tile (untilled tile, non-farm/greenhouse map, or soil health already at 100).
- **FR-008a**: Every refusal reason this feature owes the player for compost application failures, full inventory on harvest, and multiplayer action conflicts MUST be delivered through the game's own
  on-screen message system (`Game1.showRedMessage`), in the game's own font and in the player's own language via SMAPI's `ITranslationHelper` with standard translation keys registered in `i18n/default.json` (`message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`, and `message.harvest.inventory-full`). Attempting to load an invalid, non-compostable item into a composting bin follows native game machine behavior (vanilla audio cancel cue and machine shake, without text overlay). This feature MUST
  NOT introduce its own user interface, text-drawing component, or on-screen notice, and MUST NOT require
  new artwork in order to explain a refusal.
- KEY UNIFICATION: inventory-full refusal (FR-005b) and harvest-full refusal share `message.harvest.inventory-full`; no duplicate keys. Confirmed in i18n/default.json. All refusal keys (`message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`, `message.harvest.inventory-full`) use same `ITranslationHelper` path (SMAPI `helper.Translation.Get`).
- **FR-009**: Every soil health value the player can observe MUST remain within the defined bounds of
  0 to 100 inclusive. Baseline unaugmented soil (health value 0) MUST display with neutral vanilla dirt appearance without visual overlay to prevent visual noise.
- **FR-010**: The soil health of a tile MUST be visually distinguishable from a neighbouring untreated
  tile after compost is applied. The soil health display is **in scope for this feature** — seeing the
  soil change is part of the promise this feature delivers, not a later enhancement. Visual differentiation across the 0–100 scale inherits Feature 001's 3-tier continuous RGB interpolation (Poor: 1–33, Moderate: 34–66, Healthy: 67–100) rendered under ambient lighting, with health 0 remaining unrendered neutral dirt. Overlays MUST blend under ambient lighting passes without clipping or washing out during night, rain, and winter. Toggling the display setting or updating tile health takes effect on the next frame (≤16.67ms) without scene reload or polling lag via reactive event notification (`ISoilHealthService.OnTileHealthChanged`). Blend alpha ≥ 0.65 under ambient lighting; RGB shift < 5% vs untreated neighbouring tile at same light level. MEASUREMENT POINT: blend alpha measured at tile center pixel (not boundary); RGB shift computed over full tile quad versus adjacent untreated quad under identical ambient pass. Fixture required in LivingRoots.Tests/Visualization/OverlayMeasurementFixture.cs (new).
- **FR-011**: The soil health display MUST operate without silently failing, and MUST NOT report a
  value it is not actually able to draw. SILENT FAILURE DEFINED: a silent failure is any condition
  where the overlay renders without error but displays no data (e.g., missing cache, null health
  map, zero-initialized tile). The display MUST either render the correct tier or render nothing
  (neutral vanilla dirt); it MUST NOT render a stale or zero-value overlay as if it were valid data.
- **FR-012**: Bin state, processing progress, and soil health MUST survive a save and reload unchanged,
  with neither lost values nor invented values. Visual overlays MUST be invalidated and re-rendered from the reloaded soil health cache on the subsequent frame. Save loading MUST employ a tolerant reader with per-entry fault isolation: individual corrupted or malformed bin entries MUST be quarantined (skipped from runtime registration) with sanitized diagnostic logging (FR-015) without aborting the save load or corrupting valid bins. Obsolete schema fields from prior mod versions (`MaturationLevel`, `ConsecutiveActiveDays`, `ConsecutiveIdleDays`) MUST be ignored without throwing errors, defaulting to single-item batch yield (FR-005a). Disconnected save entries where no physical bin exists in the world (`GameLocation.Objects`) MUST be pruned as orphaned state (FR-002) to guarantee zero phantom machine duplication.
- **FR-013**: Advancing one in-game day MUST apply soil decay exactly once, and MUST apply the same
  seasonal rules regardless of which player is connected.
- **FR-014**: Failure of any part of this feature MUST NOT prevent the mod from loading or the game from
  being played. Corrupted or unparseable persistence records are quarantined safely at entity boundaries rather than propagating unhandled exceptions.
- **FR-015**: The mod MUST NOT write raw exception detail into its output; failures MUST be reported in
  a form that does not expose internal content.
- **FR-016**: The project's user-facing feature list in `README.md` (under `## Features`, which defines the complete public feature audit boundary) MUST NOT claim a capability that the player cannot
  observe in the game. Every capability claim in it must either correspond to behaviour the player can
  verify in-game, or be explicitly labelled with the `(Planned)` suffix. Internal engineering documentation and historical spec archives (`specs/001`-`003`) remain governed by FR-017 rather than player-facing feature markers.
- **FR-017**: No specification or plan document may declare multiplayer support out of scope where that
  contradicts the project's constitution. This covers all eight currently stale specification and plan
  documents (`specs/001-soil-health-visualization/spec.md`, `specs/001-soil-health-visualization/plan.md`, `specs/002-soil-decay-compost/spec.md`, `specs/002-soil-decay-compost/plan.md`, `specs/003-composting-tests/spec.md`, `specs/003-composting-tests/plan.md`, `docs/design-system/REALITY-CHECK-EVALUATION.md`, and `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md`), whose scope statements must be reconciled against the constitution. Correction is limited to
  scope wording and out-of-scope statements — it MUST NOT redesign or reopen the settled requirements of
  those specifications.
- **FR-018**: Nothing delivered in this feature MAY be designed such that it behaves differently on a
  multiplayer farm than on a single-player farm.
- **FR-019**: Where two players on the same farm act on the same composting bin or the same soil tile,
  **the first action to take effect wins**. State mutation is protected by host-authoritative atomic locking per tile coordinate and machine instance. The later action MUST be evaluated against the updated state, MUST be refused if invalid (with zero items deducted from the player's inventory), MUST leave the farm state unchanged, and MUST be explained to the player who made it through the game's own on-screen messages
  (FR-008a). This feature MUST NOT implement last-write-wins on farm tiles, and MUST NOT rely on the
  player whose action was refused noticing the change on their own.

### Key Entities

- **Composting Bin**: A placeable object on a farm. Has a location, a lifecycle state, and — while a
  batch is loaded — that batch's processing progress. Holds at most one batch at a time, and one
  completed batch yields exactly one compost item (FR-005a). Belongs to the farm, not to an individual
  player.
- **Compost**: An item produced by a completed batch in a bin. The only legitimate source is the bin.
  One batch produces one compost item (FR-005a).
- **Soil Health**: A value from 0 to 100 describing a single tilled tile's condition on one farm
  location. Belongs to the farm, not to an individual player.
- **Organic Waste**: An input item that qualifies for composting. Must be distinguishable from items
  that do not.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A player starting from a fresh save can complete the entire loop — craft the bin, place
  it, load it with waste, advance two days, collect compost, apply it to a tilled tile, and observe the
  change — in a single sitting without external instruction.
- **SC-002**: Every step of that loop produces a visible in-game result. Zero steps complete silently
  with no observable change.
- **SC-003**: 100% of observed soil health values fall within 0 to 100 inclusive.
- **SC-004**: A save and reload round-trip preserves every bin state, every batch's processing progress,
  and every soil health value exactly, verified across all bin lifecycle states.
- **SC-005**: Advancing one in-game day changes bin processing exactly once when two players are
  connected, not twice.
- **SC-006**: Zero user-facing feature claims in the project are false or unlabelled.
- **SC-007**: Zero specification or plan documents declare multiplayer support out of scope in
  contradiction of the project's constitution.
- **SC-008**: A player starting from a fresh save can discover the composting loop by opening the
  crafting menu and experimenting, without reading external documentation. DISCOVERABILITY MARKER:
  verified by manual playtest — the composting recipe appears in the crafting menu unconditionally,
  the bin is placeable on first attempt, and organic waste is accepted on first insert. No automated
  task covers this criterion; it is validated during Gate 2/3 manual playthrough only.
- **SC-009**: Zero instances of player-provided organic material or finished compost are destroyed by
  this feature — verified by collecting from a full inventory repeatedly, and by picking a bin up and
  putting it back down mid-batch.
- **SC-010**: Two concurrent actions on one bin or one tile result in
  exactly **one** applied change, and the player whose action was refused is shown a reason. In addition, when soil health updates, visual overlays reflect the updated tier on the next render frame (≤16.67ms) with zero polling lag. Verified in Feature 004 by automated multi-threaded test stubs and event simulations in `LivingRoots.Tests` (including simulated client listener reception of `OnTileHealthChanged` triggering immediate cache invalidation); end-to-end live multi-client network packet transport testing remains owed to the multiplayer work item on the roadmap. Overlay pixel coverage under ambient lighting MUST be ≥95% (clipping defined as any pixel falling below blend-alpha 0.65 at the tile boundary); RGB shift vs untreated neighbour <5% at same light level verified per frame.

## Assumptions

- **Registration route settled (not unsettled).** The spike (T053, briefings/spike-decision.md) completed: `ModEntry.OnAssetRequested` edits `Data/Objects` (compost), `Data/BigCraftables` (bin), `Data/Machines` (Triggers + DaysUntilReady), and `Data/CraftingRecipes`; `ContentPatcher` is out of scope (manifest.json; `ModEntry.cs:297-301`). The earlier assumption that the route was unsettled is stale documentation.
- **Multiplayer verification boundary (formal, not implicit).** 004 verifies concurrency rules (FR-019 first-wins atomic lock; SC-010 exactly-one-change + refusal + ≤16.67ms overlay) via automated multi-threaded stubs / `LivingRoots.Tests` event simulations. Live 2-player verification (real SMAPI message ordering, disconnect/reconnect) remains owed to roadmap D1; 004 MUST NOT be declared complete for multi Gate 3 until D1 runs (Constitution Principle VI; spec Assumptions (multiplayer verification boundary)). Stress/stub verification is bounded (CHESS 2007 / CTrigger 2009 / Jepsen — misses interleavings/network semantics); fallback is systematic interleaving exploration / DST with simulated network faults, never more stubs.
- **The game's native machine interface is assumed to expose everything this feature needs** — an
  input, a progress display, an output, and per-day processing. If Stardew 1.6's `Data/Machines` cannot natively handle custom progress serialization via `Item.modData` (FR-005c) or custom day processing hooks, the fallback is to use SMAPI event-intercepted machine handling (e.g., `ModController.OnDayStarted` / `OnButtonPressed` event handling already present in LivingRoots architecture) while presenting vanilla machine data definitions, progress sprites, and audio cues.
- **The game's item and machine definition formats will be verified during implementation** rather than
  assumed. They are the most likely source of a blocked first attempt.
- **The game can be launched and played manually on the maintainer's machine.** The project's quality
  gates require an end-to-end journey performed as a real player, and no automated check can substitute
  for this.
- **Live two-player verification is deferred to D1 with recorded debt.** FR-004, FR-013,
  FR-018 and FR-019 encode multiplayer-correct behaviour, verified in 004 **only** via automated stubs in `LivingRoots.Tests` (SC-005/SC-010 stub-bounded). The Constitution Gate 3 live check (host + farmhand observing consistent values) **is not satisfied by 004** and remains debt linked to roadmap item D1. 004 **MUST NOT be declared complete** for multi Gate 3 purposes until D1 runs the live session.
- **Refusal messages and localization use SMAPI's translation helper.** This feature wires `ITranslationHelper` with standard translation keys in `LivingRoots/i18n/default.json` and renders messages via the game's native on-screen red message banner (`Game1.showRedMessage`), satisfying player localization without bespoke UI components (FR-008a). Artwork and sound remain default/native placeholders.
- **Existing persistence behaviour is unchanged.** State is stored farm-scoped and host-local today.
  Changing that transport belongs to the multiplayer work item and is deliberately not touched here.
- **The soil health decay rules, the seasonal rules, and the processing duration of a bin already
  implemented remain as specified** in the earlier soil decay and compost specifications. This feature
  makes them reachable and observable; it does not redesign them. Decay logic (rate, seasonal
  multiplier, greenhouse exemption, once-per-day application) is inherited unchanged from
  `specs/002-soil-decay-compost/`; FR-013 does not restate those rules but references them by
  inheritance, with property-based invariant `ForAll` enforcing clamped 0–100 after decay. There are exactly two deliberate
  exceptions, both settled by the maintainer in this feature's clarification session, and both of which
  override the earlier specification:
  1. **Compost quantity per batch.** The maturation multiplier is retired; one completed batch yields
     exactly one compost item (FR-005a).
  2. **A removed bin's contents.** The earlier specification has a broken or removed bin drop its
     unfinished waste and any ready compost on the ground, and lose its progress. That is superseded:
     a removed bin retains its contents and its progress (FR-005c). The drop-on-ground behaviour MUST
     NOT be reintroduced.
- **Prior specifications marked complete are believed correct in intent** even where their scope
  statements are now stale. Reconciliation is a wording correction, not a redesign.
- **One known divergence is carried forward unresolved, not fixed here.** The soil decay and compost
  specification states that artisan goods are excluded from composting, and no code enforces that
  exclusion today. Because the qualifying set is inherited (FR-003), this feature neither corrects nor
  codifies that behaviour; closing the gap is a change to that specification and is out of scope here.
