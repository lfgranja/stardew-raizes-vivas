# Tasks: Soil Health Visualization

**Input**: Design documents from `/specs/001-soil-health-visualization/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: TDD test tasks included below (required by Constitution Principle IV — tests before implementation).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `LivingRoots/` (Domain/, Services/, Controllers/)
- **Tests**: `LivingRoots.Tests/` (Visualization/)
- **Constants**: `LivingRoots/Constants.cs`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Extend existing infrastructure with visualization defaults

- [ ] T001.1 [P] Create VisualizationConstantsTests in LivingRoots.Tests/Visualization/VisualizationConstantsTests.cs (verify PoorColor=#FF0000, ModerateColor=#FFFF00, HealthyColor=#00FF00, UnknownColor=#808080, DefaultOpacity=0.5f, PatternMinOpacity=0.7f, FlashDurationMs=300, TextDurationMs=1000, MaxRenderTimeMs=16.67, TooltipDebounceMs=50, OverlaysEnabledDefault=true, TooltipsEnabledDefault=true, HoeFeedbackEnabledDefault=true — assert exact values from ModConstants)
- [ ] T001 Add visualization default constants to LivingRoots/Constants.cs (PoorColor, ModerateColor, HealthyColor, UnknownColor, DefaultOpacity, PatternMinOpacity, FlashDurationMs, TextDurationMs, MaxRenderTimeMs, TooltipDebounceMs, OverlaysEnabledDefault, TooltipsEnabledDefault, HoeFeedbackEnabledDefault)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain models and interfaces that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Domain Models (Parallel)

- [ ] T002.1 [P] Create HealthCategoryTests in LivingRoots.Tests/Visualization/HealthCategoryTests.cs (verify Poor=0, Moderate=1, Healthy=2, Unknown=3 enum values; verify that 33 maps to Poor, 34 maps to Moderate, 66 maps to Moderate, 67 maps to Healthy per half-open interval rule)
- [ ] T002 [P] Create HealthCategory enum in LivingRoots/Domain/Visualization/HealthCategory.cs (Poor, Moderate, Healthy, Unknown)
- [ ] T003.1 [P] Create PatternTypeTests in LivingRoots.Tests/Visualization/PatternTypeTests.cs (verify None=0, Stripes=1, Dots=2, Solid=3 enum values; verify accessibility mapping: Poor→Stripes, Moderate→Dots, Healthy→Solid, Unknown→None)
- [ ] T003 [P] Create PatternType enum in LivingRoots/Domain/Visualization/PatternType.cs (None, Stripes, Dots, Solid)
- [ ] T004.1 [P] Create ColorDTOTests in LivingRoots.Tests/Visualization/ColorDTOTests.cs (verify R/G/B/A byte properties; verify JSON serialization round-trip; verify default value is (0,0,0,0); verify equality semantics for struct)
- [ ] T004 [P] Create ColorDTO struct in LivingRoots/Domain/Visualization/ColorDTO.cs (R, G, B, A properties with JSON serialization)
- [ ] T005.1 [P] Create VisualizationConfigurationTests in LivingRoots.Tests/Visualization/VisualizationConfigurationTests.cs (verify default values: OverlaysEnabled=true, TooltipsEnabled=true, HoeFeedbackEnabled=true, Opacity=0.5f, ShowPatterns=true, AccessibilityDegradation="auto"; verify colors default to ModConstants values; verify property setters work correctly; verify AccessibilityDegradation accepts "auto"/"never"/"notify")
- [ ] T005 [P] Create VisualizationConfiguration class in LivingRoots/Domain/Visualization/VisualizationConfiguration.cs (OverlaysEnabled, TooltipsEnabled, HoeFeedbackEnabled, Opacity, PoorColor, ModerateColor, HealthyColor, UnknownColor, ShowPatterns, AccessibilityDegradation with default values from ModConstants)
- [ ] T006.1 [P] Create ColorMappingTests in LivingRoots.Tests/Visualization/ColorMappingTests.cs (verify Category property set correctly; verify BaseColor and InterpolatedColor are Color type; verify HealthValue stored correctly; verify default state)
- [ ] T006 [P] Create ColorMapping class in LivingRoots/Domain/Visualization/ColorMapping.cs (Category, BaseColor, InterpolatedColor, HealthValue)
- [ ] T007.1 [P] Create TileOverlayTests in LivingRoots.Tests/Visualization/TileOverlayTests.cs (verify TilePosition, Color, PatternType, HealthValue, Category properties; verify constructor sets all values; verify default state)
- [ ] T007 [P] Create TileOverlay class in LivingRoots/Domain/Visualization/TileOverlay.cs (TilePosition, Color, PatternType, HealthValue, Category)
- [ ] T008.1 [P] Create TooltipDataTests in LivingRoots.Tests/Visualization/TooltipDataTests.cs (verify Text, Position, BackgroundColor, TextColor properties; verify format string "Soil Health: {percentage}% ({category})" renders correctly for each category)
- [ ] T008 [P] Create TooltipData class in LivingRoots/Domain/Visualization/TooltipData.cs (Text, Position, BackgroundColor, TextColor)
- [ ] T009.1 [P] Create HoeFeedbackTests in LivingRoots.Tests/Visualization/HoeFeedbackTests.cs (verify TilePosition, StartTime, FlashDuration=300ms, TextDuration=1000ms, HealthValue, Category, HealthText properties; verify IsActive returns true when elapsed < max duration; verify IsExpired returns true when elapsed exceeds both durations)
- [ ] T009 [P] Create HoeFeedback class in LivingRoots/Domain/Visualization/HoeFeedback.cs (TilePosition, StartTime, FlashDuration, TextDuration, HealthValue, Category, HealthText)

### Domain Interfaces (Parallel)

- [ ] T010 [P] Create IColorInterpolationService interface in LivingRoots/Domain/IColorInterpolationService.cs (GetColorForHealth, GetCategoryForHealth, SetCategoryColors, InvalidateCache)
- [ ] T011 [P] Create IVisualizationConfigurationService interface in LivingRoots/Domain/IVisualizationConfigurationService.cs (GetConfiguration, LoadConfiguration, SaveConfiguration, ResetToDefaults, UpdateConfiguration)
- [ ] T012 [P] Create IVisualizationService interface in LivingRoots/Domain/IVisualizationService.cs (RenderOverlays, RenderTooltip, RenderHoeFeedback, TriggerHoeFeedback, InvalidateCache)

### Core Services (Blocking - needed by all stories)

- [ ] T013.1 [P] Create ColorInterpolationServiceTests in LivingRoots.Tests/Visualization/ColorInterpolationServiceTests.cs (verify linear RGB interpolation per FR-008: health=0 returns PoorColor, health=100 returns HealthyColor, health=50 returns midpoint; verify GetCategoryForHealth returns correct category for boundary values 0/33/34/66/67/100 per half-open intervals; verify GetColorForHealth caches results; verify InvalidateCache clears cache; verify health values clamped to [0,100] per ModConstants; verify NaN/Infinity handled gracefully)
- [ ] T013 Create ColorInterpolationService in LivingRoots/Services/Visualization/ColorInterpolationService.cs (implements IColorInterpolationService, linear RGB interpolation per FR-008 with half-open interval thresholds [0,34), [34,67), [67,100], caching, clamping per ModConstants)
- [ ] T014.1 [P] Create VisualizationConfigurationServiceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationServiceTests.cs (verify GetConfiguration never null; verify LoadConfiguration with valid JSON loads correctly; verify LoadConfiguration with invalid fields replaces only invalid values per FR-006; verify LoadConfiguration with missing file generates defaults per FR-006; verify SaveConfiguration persists via IModDataService; verify ResetToDefaults restores ModConstants values; verify UpdateConfiguration with null throws ArgumentNullException; verify thread-safe access with concurrent reads; use Mock<IModDataService>, Mock<IMonitor>, Mock<IFileNameSanitizationService>)
- [ ] T014 Create VisualizationConfigurationService in LivingRoots/Services/Visualization/VisualizationConfigurationService.cs (implements IVisualizationConfigurationService, JSON persistence via IModDataService, forward-compatible deserialization per FR-006, invalid field fallback per FR-006, thread-safe access)

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - View Soil Health Overlay on Tilled Tiles (Priority: P1) 🎯 MVP

**Goal**: Render color-coded overlays on tilled soil tiles based on health values

**Independent Test**: Till multiple soil tiles with different health values (poor, moderate, healthy) and verify the correct colors render on each tile

### Implementation

- [ ] T015.1 [P] [US1] Create OverlayRendererTests in LivingRoots.Tests/Visualization/OverlayRendererTests.cs (verify viewport culling per FR-009: tiles outside viewport excluded; verify cache returns same list on repeated calls; verify cache invalidation per FR-010 returns fresh list; verify graceful degradation >1000 tiles per FR-009: patterns disabled when AccessibilityDegradation="auto"; verify patterns remain when AccessibilityDegradation="never" even at >1000 tiles; verify zero-tilled-tiles returns empty list per FR-021; use Mock<IColorInterpolationService>, Mock<IVisualizationConfigurationService>)
- [ ] T015 [US1] Create OverlayRenderer in LivingRoots/Services/Visualization/OverlayRenderer.cs (viewport culling per FR-009, overlay list cache per FR-010, cache invalidation per FR-010, graceful degradation per FR-009)
- [ ] T016.1 [P] [US1] Create VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify RenderOverlays calls OverlayRenderer with correct viewport; verify Unknown color rendered when health data unavailable per FR-014; verify config opacity applied to all overlays; verify pattern overlays rendered per FR-001 when ShowPatterns=true; verify no draw calls when OverlaysEnabled=false; verify no exceptions with null SpriteBatch — ArgumentNullException at construction only)
- [ ] T016 [US1] Implement RenderOverlays in LivingRoots/Services/Visualization/VisualizationService.cs (orchestrates overlay rendering, applies config opacity, applies pattern overlays per FR-001, renders Unknown color per FR-014)
- [ ] T017.1 [US1] Add test to VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify zero tilled tiles: no OverlayRenderer invocation, no cache allocation, no exceptions per FR-021; verify performance: render time < 1ms for empty farm)
- [ ] T017 [US1] Implement zero-tilled-tiles no-op handling in LivingRoots/Services/Visualization/VisualizationService.cs (per FR-021, no draw calls, no cache allocation)
- [ ] T018.1 [US1] Add test to VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify viewport change triggers cache recalculation per FR-016; verify 1-tile margin beyond visible bounds per FR-018; verify overlay positions updated within single frame)
- [ ] T018 [US1] Implement viewport resize handling in LivingRoots/Services/Visualization/VisualizationService.cs (recalculate overlays per FR-016, 1-tile margin per FR-018)
- [ ] T019.1 [US1] Add test to VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify mid-render health change completes current frame with old state per FR-020; verify next frame reflects new value; use ThreadSafeGameLoopEventsStub to simulate concurrent game events)
- [ ] T019 [US1] Implement frame consistency for mid-render health changes in LivingRoots/Services/Visualization/VisualizationService.cs (per FR-020, complete current frame with previous state)

**Checkpoint**: User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Inspect Soil Health via Hover Tooltip (Priority: P2)

**Goal**: Display hover tooltips showing soil health percentage and status text for tilled tiles

**Independent Test**: Hover over tiles with known health values and verify the tooltip displays correct percentage and status text

### Implementation

- [ ] T020.1 [P] [US2] Create TooltipRendererTests in LivingRoots.Tests/Visualization/TooltipRendererTests.cs (verify cursor-to-tile mapping: screen coordinates map to correct tile; verify tooltip format "Soil Health: {percentage}% ({category})" per spec — test with 0/33/34/66/67/100 values; verify throttle logic per FR-015: rapid cursor movement suppresses updates, minimum 50ms between tooltip updates, leading-edge update on new tile entry; verify cursor outside tile bounds returns null/no tooltip; use Mock<IVisualizationConfigurationService>)
- [ ] T020 [US2] Create TooltipRenderer in LivingRoots/Services/Visualization/TooltipRenderer.cs (cursor-to-tile mapping, tooltip formatting "Soil Health: {percentage}% ({category})" per spec, throttle logic per FR-015 with 50ms minimum interval and leading-edge update)
- [ ] T021.1 [P] [US2] Create VisualizationServiceTooltipTests in LivingRoots.Tests/Visualization/VisualizationServiceTooltipTests.cs (verify RenderTooltip calls TooltipRenderer when cursor over tilled tile; verify no tooltip rendered when TooltipsEnabled=false per acceptance scenario 3; verify tooltip disappears on cursor leave per acceptance scenario 2; verify tooltip position matches cursor screen coordinates)
- [ ] T021 [US2] Implement RenderTooltip in LivingRoots/Services/Visualization/VisualizationService.cs (calls TooltipRenderer, respects TooltipsEnabled config, cursor leave detection per acceptance scenario 2)
- [ ] T022.1 [US2] Create ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify Input.ButtonReleased updates cursor position; verify cursor position passed to IVisualizationService.RenderTooltip; use Mock<IVisualizationService>, Mock<IVisualizationConfigurationService>, Mock<IColorInterpolationService>; use ThreadSafeGameLoopEventsStub for event simulation)
- [ ] T022 [US2] Integrate tooltip cursor tracking in LivingRoots/Controllers/ModController.cs (cursor position update on Input.ButtonReleased events)

**Checkpoint**: User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Receive Hoe Action Feedback (Priority: P2)

**Goal**: Show flash effect and floating text when a hoe is used on tilled soil tiles

**Independent Test**: Use a hoe on a tilled tile and verify the flash effect and floating text appear on the targeted tile only

### Implementation

- [ ] T023.1 [P] [US3] Create HoeFeedbackRendererTests in LivingRoots.Tests/Visualization/HoeFeedbackRendererTests.cs (verify flash effect renders for 300ms duration; verify floating text renders for 1000ms duration; verify fade-out animation progresses correctly; verify HoeFeedback.IsActive returns true during duration; verify HoeFeedback.IsExpired returns true after duration; verify HealthText format matches "Soil Health: {percentage}% ({category})")
- [ ] T023 [US3] Create HoeFeedbackRenderer in LivingRoots/Services/Visualization/HoeFeedbackRenderer.cs (flash effect rendering 300ms duration, floating text rendering 1000ms duration, fade-out animation)
- [ ] T024.1 [P] [US3] Create VisualizationServiceHoeFeedbackTests in LivingRoots.Tests/Visualization/VisualizationServiceHoeFeedbackTests.cs (verify TriggerHoeFeedback creates HoeFeedback state with correct tile position; verify TriggerHoeFeedback validates tile is tilled per FR-011; verify TriggerHoeFeedback on non-tilled tile does nothing per acceptance scenario 3; verify only targeted tile receives feedback per acceptance scenario 4)
- [ ] T024 [US3] Implement TriggerHoeFeedback in LivingRoots/Services/Visualization/VisualizationService.cs (creates HoeFeedback state, validates tile is tilled, per FR-011 targets only the tile under cursor)
- [ ] T025.1 [US3] Add test to VisualizationServiceHoeFeedbackTests in LivingRoots.Tests/Visualization/VisualizationServiceHoeFeedbackTests.cs (verify RenderHoeFeedback calls HoeFeedbackRenderer with active feedback; verify expired feedback is not rendered; verify no rendering when HoeFeedbackEnabled=false)
- [ ] T025 [US3] Implement RenderHoeFeedback in LivingRoots/Services/Visualization/VisualizationService.cs (calls HoeFeedbackRenderer, expires feedback after duration)
- [ ] T026.1 [US3] Add test to ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify hoe tool usage on tilled tile triggers IVisualizationService.TriggerHoeFeedback; verify hoe on non-tilled tile does not trigger feedback; verify correct tile position passed to TriggerHoeFeedback; use ThreadSafeGameLoopEventsStub for event simulation)
- [ ] T026 [US3] Integrate hoe action detection in LivingRoots/Controllers/ModController.cs (detect hoe tool usage on tilled tiles, call TriggerHoeFeedback)

**Checkpoint**: User Stories 1, 2, AND 3 should all work independently

---

## Phase 6: User Story 4 - Configure Visualization Settings (Priority: P3)

**Goal**: Persist and load visualization configuration with hot-reload support

**Independent Test**: Modify configuration values and verify the visualization behavior changes accordingly

### Implementation

- [ ] T027.1 [P] [US4] Create VisualizationConfigurationPersistenceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationPersistenceTests.cs (verify SaveConfiguration called on Saving event; verify LoadConfiguration called on SaveLoaded event; verify JSON serialization round-trip preserves all fields; verify IModDataService.SaveData called with correct key "visualization_config"; use Mock<IModDataService>, Mock<IMonitor>)
- [ ] T027 [US4] Implement configuration persistence in LivingRoots/Services/Visualization/VisualizationConfigurationService.cs (JSON save via IModDataService, SaveConfiguration called on Saving event, LoadConfiguration called on SaveLoaded event)
- [ ] T028.1 [US4] Add test to VisualizationConfigurationPersistenceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationPersistenceTests.cs (verify invalid Opacity values (>1.0 or <0.0) replaced with defaults per FR-006; verify invalid color values replaced with defaults; verify valid fields preserved when others invalid per FR-006; verify warning logged for each invalid field)
- [ ] T028 [US4] Implement configuration validation in LivingRoots/Services/Visualization/VisualizationConfigurationService.cs (invalid fields replaced with defaults per FR-006, valid fields preserved per FR-006)
- [ ] T029.1 [US4] Add test to VisualizationConfigurationPersistenceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationPersistenceTests.cs (verify file change detection triggers reload; verify changes applied within next frame per FR-005/SC-003 (≤16.67ms); verify atomic application per FR-019: no mixed-state frames; use ThreadSafeGameLoopEventsStub to simulate concurrent rendering)
- [ ] T029 [US4] Implement hot-reload in LivingRoots/Services/Visualization/VisualizationConfigurationService.cs (file change detection, apply changes within next frame per FR-005, atomic application per FR-019)
- [ ] T030.1 [US4] Add test to VisualizationConfigurationPersistenceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationPersistenceTests.cs (verify missing file generates complete defaults per FR-006; verify defaults match ModConstants values; verify no exception when file not found)
- [ ] T030 [US4] Implement default configuration creation in LivingRoots/Services/Visualization/VisualizationConfigurationService.cs (missing file generates defaults per FR-006)
- [ ] T031.1 [US4] Add test to ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify SaveLoaded event triggers LoadConfiguration; verify Saving event triggers SaveConfiguration; verify config change triggers IColorInterpolationService.SetCategoryColors; use ThreadSafeGameLoopEventsStub for event simulation)
- [ ] T031 [US4] Register configuration event handlers in LivingRoots/Controllers/ModController.cs (SaveLoaded loads config, Saving saves config, config change triggers ColorInterpolationService.SetCategoryColors)

**Checkpoint**: All user stories should now have full configuration support

---

## Phase 7: Integration & Event Handling

**Purpose**: Wire visualization into existing mod infrastructure

- [ ] T032.1 Create VisualizationServiceRegistrationTests in LivingRoots.Tests/Visualization/VisualizationServiceRegistrationTests.cs (verify IVisualizationService, IVisualizationConfigurationService, IColorInterpolationService registered in DI container; verify services resolve correctly from composition root; verify singleton lifetime matches existing service patterns)
- [ ] T032 Register visualization services in LivingRoots/ModEntry.cs (add IVisualizationService, IVisualizationConfigurationService, IColorInterpolationService to DI container per composition root pattern)
- [ ] T033.1 Add test to ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify RenderedWorld event subscribed for overlays; verify Input.ButtonReleased subscribed for tooltips/hoe; verify SaveLoaded/Saving subscribed for config; use ThreadSafeGameLoopEventsStub to verify subscription counts)
- [ ] T033 Extend LivingRoots/Controllers/ModController.cs with visualization event handlers (subscribe to RenderedWorld for overlays, Input.ButtonReleased for tooltips/hoe, SaveLoaded/Saving for config)
- [ ] T034.1 Add test to ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify all visualization event handlers unsubscribed on Dispose per FR-012; verify lock + _disposed pattern used correctly; verify no memory leaks: zero subscriptions after disposal; use ThreadSafeGameLoopEventsStub to verify unsubscription counts)
- [ ] T034 Implement visualization disposal in LivingRoots/Controllers/ModController.cs (unsubscribe all event handlers per FR-012, use existing lock + _disposed pattern)
- [ ] T035.1 Add test to VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify overlay rendering paused during save per FR-017; verify rendering resumed with refreshed data after load; verify no overlay frames render during save operation; use ThreadSafeGameLoopEventsStub to simulate save/load events)
- [ ] T035 Implement save/load rendering pause in LivingRoots/Services/Visualization/VisualizationService.cs (pause overlay rendering during save per FR-017, resume with refreshed data)

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements and validation

- [ ] T036 [P] Code cleanup and refactor visualization services
- [ ] T037.1 [P] Create VisualizationPerformanceTests in LivingRoots.Tests/Visualization/VisualizationPerformanceTests.cs (verify 60 FPS with 1000 tiles per SC-002: render time ≤16.67ms; verify viewport culling reduces tile count correctly; verify cache hit rate >90% for static scenes; verify zero allocations in render methods (no GC pressure); verify graceful degradation activates at >1000 tiles)
- [ ] T037 [P] Performance optimization for overlay rendering (ensure 60 FPS with 1000 tiles per SC-002)
- [ ] T038 [US1,US2,US3,US4] Run quickstart.md validation scenarios to verify all features work end-to-end
- [ ] T039.1 [P] Create VisualizationThreadSafetyTests in LivingRoots.Tests/Visualization/VisualizationThreadSafetyTests.cs (verify concurrent scenario 1: health value changing mid-render per FR-020 — trigger 100 times, confirm no deadlocks; verify concurrent scenario 2: config hot-reload during active rendering per FR-019 — trigger 100 times, confirm no mixed-state frames; verify concurrent scenario 3: save/load pausing rendering per FR-017 — trigger 100 times, confirm no state corruption; use ThreadSafeGameLoopEventsStub for all scenarios; verify async-only per constitution for I/O operations — no .Result or .Wait() calls; verify lock-free atomic patterns for game-loop coordination)
- [ ] T039 Verify thread safety across all visualization services (no deadlocks per FR-013, async-only per constitution for I/O, lock-free atomic patterns for game-loop coordination)

---

## Integration Tests

- [ ] T040 Create VisualizationIntegrationTests in LivingRoots.Tests/Visualization/VisualizationIntegrationTests.cs (end-to-end: till tile → set health → render overlay → verify color; hover tile → verify tooltip text; use hoe → verify flash + floating text; modify config → verify behavior changes; full save/load round-trip → verify visualization state preserved; use Mock<IModDataService> for persistence, ThreadSafeGameLoopEventsStub for events)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - US1, US2, US3 can proceed in parallel (independent rendering features)
  - US4 (configuration) should complete early as other stories benefit from config support
- **Integration (Phase 7)**: Depends on all user stories being implemented
- **Polish (Phase 8)**: Depends on integration completion

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Renders after US1 overlays
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Renders after US1 overlays
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Other stories benefit from early completion

### Within Each User Story

- Domain models before services
- Services before controller integration
- Core rendering before polish features

### Parallel Opportunities

- Phase 1: T001 (single task)
- Phase 2: T002-T009 (domain models), T010-T012 (interfaces) can all run in parallel
- Phase 3-6: US1, US2, US3, US4 can run in parallel after Phase 2
- Phase 8: T036, T037, T038, T039 can run in parallel

---

## Parallel Example: Foundational Phase

```
# Launch all domain models in parallel:
Task T002: Create HealthCategory enum
Task T003: Create PatternType enum
Task T004: Create ColorDTO struct
Task T005: Create VisualizationConfiguration class
Task T006: Create ColorMapping class
Task T007: Create TileOverlay class
Task T008: Create TooltipData class
Task T009: Create HoeFeedback class

# Launch all interfaces in parallel:
Task T010: Create IColorInterpolationService interface
Task T011: Create IVisualizationConfigurationService interface
Task T012: Create IVisualizationService interface
```

---

## Parallel Example: User Story 1

```
# Overlay rendering components (after foundation):
Task T015: Create OverlayRenderer
Task T016: Implement RenderOverlays
Task T017: Implement zero-tilled-tiles handling
Task T018: Implement viewport resize handling
Task T019: Implement frame consistency
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002-T014)
3. Complete Phase 3: User Story 1 (T015-T019)
4. Complete Phase 7: Integration (T032-T035) - minimal for US1
5. **STOP and VALIDATE**: Test User Story 1 independently via quickstart.md Scenario 1

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → MVP!
3. Add User Story 2 → Test independently
4. Add User Story 3 → Test independently
5. Add User Story 4 → Test independently
6. Complete Integration → All stories wired together
7. Polish → Performance, cleanup, validation

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Overlays)
   - Developer B: User Story 2 (Tooltips)
   - Developer C: User Story 3 (Hoe Feedback)
   - Developer D: User Story 4 (Configuration) - prioritize early
3. Stories complete and integrate in Phase 7

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- **TDD order**: Test tasks (decimal IDs like T001.1) MUST be completed before their corresponding implementation tasks (integer IDs like T001). Red-Green-Refactor: write failing test, then implement to make it pass.
- File naming: PascalCase.cs, interfaces prefixed with I
- All new visualization code goes in LivingRoots/Domain/Visualization/ or LivingRoots/Services/Visualization/
- Configuration key: "visualization_config" via IModDataService
- Follow existing patterns: constructor injection, async/await for I/O, lock + _disposed for cleanup, Interlocked/Volatile.Read for game-loop coordination
- ModConstants extension is single source of truth for visualization defaults
- Test files use xUnit + Moq + ThreadSafeGameLoopEventsStub per project conventions
- Half-open intervals for health category boundaries: [0, 34), [34, 67), [67, 100]
