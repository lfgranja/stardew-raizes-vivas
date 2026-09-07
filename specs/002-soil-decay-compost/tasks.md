# Tasks: Soil Health Decay + Compost Restoration

**Input**: Design documents from `/specs/002-soil-decay-compost/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: No test tasks included (not explicitly requested in spec).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `LivingRoots/` (Domain/, Services/, Controllers/)
- **Tests**: `LivingRoots.Tests/`
- **Assets**: `LivingRoots/Assets/`
- **i18n**: `LivingRoots/i18n/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, constants, and domain interfaces

- [ ] T001 Add decay/compost constants to `LivingRoots/Constants.cs` (DecayRate, RestorationAmount, ProcessingDurationMinutes, MaturationMaxLevel, MaturationIdleResetDays, CompostItemId, CompostingBinItemId)
- [ ] T002 [P] Create `CompostingBinState` enum in `LivingRoots/Domain/Models/CompostingBinState.cs` (Empty, Processing, Ready)
- [ ] T003 [P] Create `ISoilDecayService` interface in `LivingRoots/Domain/Interfaces/ISoilDecayService.cs` with method `void ProcessDayStart(string locationName)`
- [ ] T004 [P] Create `ICompostingBinService` interface in `LivingRoots/Domain/Interfaces/ICompostingBinService.cs` with methods for AddWaste, CollectCompost, GetBinState, ProcessDayStart
- [ ] T005 [P] Create `ICompostApplicationService` interface in `LivingRoots/Domain/Interfaces/ICompostApplicationService.cs` with method `bool TryApplyCompost(GameLocation location, Vector2 tile)`
- [ ] T006 [P] Create `IOrganicWasteValidator` interface in `LivingRoots/Domain/Interfaces/IOrganicWasteValidator.cs` with method `bool IsValidOrganicWaste(Item item)`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST complete before ANY user story can implement

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 Implement `OrganicWasteValidator` in `LivingRoots/Domain/Services/OrganicWasteValidator.cs` — validates items against category IDs [-74, -75, -79, -80, -81] OR `compostable_item` tag, rejects `not_compostable` tag
- [ ] T008 [P] Implement `SeasonalDecayMultiplier` in `LivingRoots/Domain/Services/SeasonalDecayMultiplier.cs` — returns multiplier for season (Spring 0.5, Summer 1.5, Fall 0.5, Winter 0.0)
- [ ] T009 Create `CompostingBinState` model in `LivingRoots/Domain/Models/CompostingBinState.cs` — fields: TileX, TileY, State, InputItemId, InputTimestamp, MaturationLevel, ConsecutiveIdleDays
- [ ] T010 Create `CompostingBinData` save model in `LivingRoots/Domain/Models/CompostingBinData.cs` — wrapper for serialization: `Dictionary<string, CompostingBinState>` keyed by "X,Y"
- [ ] T011 Implement `CompostingBinFactory` in `LivingRoots/Services/CompostingBinFactory.cs` — creates bin instances, manages unique IDs per location

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 - Soil Decays When Left Bare (Priority: P1) 🎯 MVP

**Goal**: Tilled soil loses health each day when left bare, with seasonal multipliers. Greenhouse is exempt.

**Independent Test**: Till soil, leave bare overnight, verify health decreases by configured decay rate × seasonal multiplier.

### Implementation for User Story 1

- [ ] T012 [US1] Implement `SoilDecayService` in `LivingRoots/Services/SoilDecayService.cs` — iterates tilled tiles at day start, applies decay to bare tiles using `ISoilHealthService.UpdateHealth()` with seasonal multiplier
- [ ] T013 [US1] Add `ProcessDayStart` event handler in `LivingRoots/Controllers/ModController.cs` — subscribes to `IDayStartedWritableAPI`, calls `SoilDecayService.ProcessDayStart()` for each location
- [ ] T014 [US1] Implement bare tile detection in `SoilDecayService` — checks `HoeDirt.crop == null` for bare, `crop.dead` for residue (mulch protection)
- [ ] T015 [US1] Implement Greenhouse exemption in `SoilDecayService` — skips decay for `location.Name == "Greenhouse"`
- [ ] T016 [US1] Add debug logging for decay operations — log tiles processed, total health lost at `LogLevel.Trace`

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Restore Health with Compost (Priority: P1)

**Goal**: Player applies compost to tilled soil to restore health. Compost consumed on success, rejected on invalid targets.

**Independent Test**: Apply compost to tilled tile with reduced health, verify health increases by 15 and one compost consumed.

### Implementation for User Story 2

- [ ] T017 [P] [US2] Register compost item in `LivingRoots/ModEntry.cs` — create `Object` item with ID `LivingRoots.Compost`, category -26, via `ItemRegistry.Create<Object>`
- [ ] T018 [US2] Implement `CompostApplicationService` in `LivingRoots/Services/CompostApplicationService.cs` — validates tile is tilled, on farm/Greenhouse, not at max health; applies +15 health via `ISoilHealthService.UpdateHealth()`
- [ ] T019 [US2] Add right-click input handler in `LivingRoots/Controllers/ModController.cs` — detects right-click with compost equipped, calls `CompostApplicationService.TryApplyCompost()`
- [ ] T020 [US2] Implement floating text feedback in `CompostApplicationService` — spawns `TemporaryAnimatedSprite` with `text: "+15"` in green at tile position via `Game1.multiplayer.broadcastSprites`
- [ ] T021 [US2] Implement invalid target feedback — play "cancel" sound for non-tilled tile, max health, or NPC garden (no visual)
- [ ] T022 [US2] Add debug logging for compost applications — log tile position, amount restored at `LogLevel.Trace`

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Produce Compost via Composting Bin (Priority: P2)

**Goal**: Composting bin machine accepts organic waste, processes over 2 days, produces compost. Maturation increases output over time.

**Independent Test**: Add organic waste to composting bin, wait 2 days, collect compost items.

### Implementation for User Story 3

- [ ] T023 [P] [US3] Register composting bin object in `LivingRoots/ModEntry.cs` — create placeable machine object with ID `LivingRoots.CompostingBin` via `ItemRegistry.Create<Object>`
- [ ] T024 [P] [US3] Add crafting recipe in `LivingRoots/ModEntry.cs` — register `"LivingRoots_CompostingBin": "388 50 390 25 771 15/Home/CompostingBin/true/default/"` via `CraftingRecipeRegistry`
- [ ] T025 [US3] Implement `CompostingBinService` in `LivingRoots/Services/CompostingBinService.cs` — manages bin state machine (Empty→Processing→Ready→Empty), save/load via `IModDataService`
- [ ] T026 [US3] Implement `AddWaste` method — validates bin is empty and item is valid organic waste via `IOrganicWasteValidator`, sets state to Processing, records timestamp
- [ ] T027 [US3] Implement `CollectCompost` method — validates bin is Ready, calculates output quantity (1 × MaturationLevel), adds compost to inventory, resets state to Empty
- [ ] T028 [US3] Implement `ProcessDayStart` for bins — checks processing completion (elapsed ≥ 2880 min), increments idle days, resets maturation if idle ≥ 14 days
- [ ] T029 [US3] Implement maturation progression — increment level after 7 consecutive days of operation (max 5), reset after 14 idle days
- [ ] T030 [US3] Implement bin break behavior — drop input item (if Processing) or output compost (if Ready) on ground, clear state
- [ ] T031 [US3] Implement save/load persistence — serialize `CompostingBinData` to JSON keyed by `composting_bins_{saveId}_{locationName}`, deserialize on load with elapsed time calculation
- [ ] T032 [US3] Implement machine interaction handler in `LivingRoots/Controllers/CompostingBinController.cs` — handle right-click (add waste if holding organic item, collect if empty hands)
- [ ] T033 [US3] Implement machine hover tooltip — show "Maturity: Xx" via standard `Data/Machines` hover text support
- [ ] T034 [US3] Implement machine visual states — show input sprite while processing, ready sprite when complete via `Data/Machines` animation
- [ ] T035 [US3] Implement ambient audio — play intermittent bubbling sound every 2-3 in-game hours during processing via `Game1.playSound`
- [ ] T036 [US3] Implement ready chime — play distinct sound when processing completes (state transitions to Ready)
- [ ] T037 [US3] Add debug logging for bin operations — log state transitions (empty→processing→ready), maturation changes at `LogLevel.Trace`

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T038 [P] Add i18n localization strings to `LivingRoots/i18n/en.json` — compost item name/description, composting bin name/description, maturity tooltip format, floating text format
- [ ] T039 [P] Add i18n default key fallback in `LivingRoots/i18n/default.json` — ensure all localized strings have English defaults
- [ ] T040 [P] Create asset placeholder textures — `LivingRoots/Assets/compost.png` (16×16), `LivingRoots/Assets/composting_bin_empty.png` (16×32), `LivingRoots/Assets/composting_bin_processing.png` (16×32), `LivingRoots/Assets/composting_bin_ready.png` (16×32)
- [ ] T041 [P] Create audio placeholder files — `LivingRoots/Assets/Audio/composting_bin_processing.ogg`, `LivingRoots/Assets/Audio/composting_bin_ready.ogg`, `LivingRoots/Assets/Audio/compost_apply.ogg`
- [ ] T042 Register content patches in `LivingRoots/ModEntry.cs` — load custom assets via `IAssetEditor`/`IAssetLoader` for textures and audio
- [ ] T043 Run `dotnet build Stardew-LivingRoots.sln` — verify project compiles
- [ ] T044 Run `dotnet test --nologo` — verify existing tests pass (no regressions)
- [ ] T045 Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` — verify code formatting
- [ ] T046 Validate against `specs/002-soil-decay-compost/quickstart.md` — execute all 12 validation scenarios

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User Story 1 (P1): Can start after Foundational — No dependencies on other stories
  - User Story 2 (P1): Can start after Foundational — Independent of US1
  - User Story 3 (P2): Can start after Foundational — Independent of US1/US2
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Phase 2 (Foundational) — No dependencies on other stories
- **User Story 2 (P1)**: Depends on Phase 2 (Foundational) — Uses `ISoilHealthService` (existing), no story dependencies
- **User Story 3 (P2)**: Depends on Phase 2 (Foundational) — Uses `IOrganicWasteValidator`, no story dependencies

### Within Each User Story

- Models before services
- Services before controllers
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T002-T006)
- All Foundational tasks marked [P] can run in parallel (T008)
- Once Foundational phase completes, all user stories can start in parallel
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all implementation tasks for User Story 1 together:
Task: "Implement SoilDecayService in LivingRoots/Services/SoilDecayService.cs"
Task: "Add ProcessDayStart event handler in LivingRoots/Controllers/ModController.cs"
Task: "Implement bare tile detection in SoilDecayService"
Task: "Implement Greenhouse exemption in SoilDecayService"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T006)
2. Complete Phase 2: Foundational (T007-T011) — CRITICAL - blocks all stories
3. Complete Phase 3: User Story 1 (T012-T016)
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (T012-T016)
   - Developer B: User Story 2 (T017-T022)
   - Developer C: User Story 3 (T023-T037)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- All constants must come from `ModConstants` — no hardcoded values
- All player-facing strings use `I18n.Key` — no hardcoded English
- Async-only concurrency — no `.Result` or `.Wait()`
