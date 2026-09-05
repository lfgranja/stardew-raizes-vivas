# Remediation: TDD Test Tasks for Soil Health Visualization

**Feature**: Soil Health Visualization  
**Branch**: `001-soil-health-visualization`  
**Date**: 2026-09-05  
**Purpose**: Add test tasks required by Constitution Principle IV (TDD) — "Tests are written before production code. The Red-Green-Refactor cycle is enforced."

---

## Summary of Findings

### Current State
The existing `tasks.md` states: *"Tests: No test tasks included (not explicitly requested in spec.md). Add test tasks if TDD approach desired."*

This violates Constitution Principle IV, which makes TDD **non-negotiable**. The plan.md already acknowledges this: *"Tests will be written before implementation. xUnit + Moq for unit tests. ThreadSafeGameLoopEventsStub for event testing."*

### Project Test Conventions (from analysis)

| Convention | Source | Detail |
|------------|--------|--------|
| **Test framework** | `Usings.cs`, all test files | xUnit with `global using Xunit;` |
| **Mocking** | All service test files | Moq (`Mock<T>`, `.Setup()`, `.Verify()`) |
| **File naming** | AGENTS.md, existing files | `{Subject}Tests.cs` — one per subject |
| **File location** | plan.md, AGENTS.md | `LivingRoots.Tests/Visualization/` for this feature |
| **Namespace** | Existing test files | `LivingRoots.Tests` (or `LivingRoots.Tests.Visualization` for subdirectory) |
| **Constructor tests** | `SoilHealthServiceTests.cs`, `ModControllerTests.cs` | `Constructor_WithNull[Dep]_ThrowsArgumentNullException` |
| **Theory tests** | `SoilHealthServiceTests.cs` | `[Theory]` + `[InlineData]` for parameterized cases |
| **Thread safety** | `ThreadSafeGameLoopEventsStub.cs`, `ModControllerTests.cs` | Use stub for concurrent event handler tests |
| **Integration tests** | `ModControllerTests.cs` | `[Trait("Category", "Integration")]` with `#region` blocks |
| **Assertions** | All test files | `Assert.Equal`, `Assert.True`, `Assert.Throws`, `Assert.InRange`, `Record.Exception` |
| **Mock patterns** | `SoilHealthServiceTests.cs` | `Mock<IModDataService>`, `Mock<IMonitor>`, `Mock<IFileNameSanitizationService>` |

### Test File Structure (from plan.md)

```
LivingRoots.Tests/
├── Visualization/
│   ├── VisualizationServiceTests.cs
│   ├── VisualizationConfigurationServiceTests.cs
│   ├── ColorInterpolationServiceTests.cs
│   └── ModControllerVisualizationTests.cs
```

---

## Proposed Test Tasks

These tasks follow TDD Red-Green-Refactor: each test task creates a **failing test first** (Red), then the corresponding implementation task makes it pass (Green). Test tasks are inserted **before** their implementation counterparts.

Format matches existing tasks.md: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup (Shared Infrastructure)

### Existing Task
- [ ] T001 Add visualization default constants to LivingRoots/Constants.cs

### Test Task (NEW)
- [ ] T001.1 [P] Create VisualizationConstantsTests in LivingRoots.Tests/Visualization/VisualizationConstantsTests.cs (verify PoorColor=#FF0000, ModerateColor=#FFFF00, HealthyColor=#00FF00, UnknownColor=#808080, DefaultOpacity=0.5f, PatternMinOpacity=0.7f, FlashDurationMs=300, TextDurationMs=1000, MaxRenderTimeMs=16.67, TooltipDebounceMs=50, OverlaysEnabledDefault=true, TooltipsEnabledDefault=true, HoeFeedbackEnabledDefault=true — assert exact values from ModConstants)

---

## Phase 2: Foundational (Blocking Prerequisites)

### Domain Models (Parallel)

**Before T002 (HealthCategory enum):**
- [ ] T002.1 [P] Create HealthCategoryTests in LivingRoots.Tests/Visualization/HealthCategoryTests.cs (verify Poor=0, Moderate=1, Healthy=2, Unknown=3 enum values; verify Parse/ToString round-trip; verify that 33 maps to Poor, 34 maps to Moderate, 66 maps to Moderate, 67 maps to Healthy per boundary handling rule)

**Before T003 (PatternType enum):**
- [ ] T003.1 [P] Create PatternTypeTests in LivingRoots.Tests/Visualization/PatternTypeTests.cs (verify None=0, Stripes=1, Dots=2, Solid=3 enum values; verify accessibility mapping: Poor→Stripes, Moderate→Dots, Healthy→Solid, Unknown→None)

**Before T004 (ColorDTO struct):**
- [ ] T004.1 [P] Create ColorDTOTests in LivingRoots.Tests/Visualization/ColorDTOTests.cs (verify R/G/B/A byte properties; verify JSON serialization round-trip; verify default value is (0,0,0,0); verify equality semantics for struct)

**Before T005 (VisualizationConfiguration class):**
- [ ] T005.1 [P] Create VisualizationConfigurationTests in LivingRoots.Tests/Visualization/VisualizationConfigurationTests.cs (verify default values: OverlaysEnabled=true, TooltipsEnabled=true, HoeFeedbackEnabled=true, Opacity=0.5f, ShowPatterns=true; verify colors default to ModConstants values; verify property setters work correctly)

**Before T006 (ColorMapping class):**
- [ ] T006.1 [P] Create ColorMappingTests in LivingRoots.Tests/Visualization/ColorMappingTests.cs (verify Category property set correctly; verify BaseColor and InterpolatedColor are Color type; verify HealthValue stored correctly; verify default state)

**Before T007 (TileOverlay class):**
- [ ] T007.1 [P] Create TileOverlayTests in LivingRoots.Tests/Visualization/TileOverlayTests.cs (verify TilePosition, Color, PatternType, HealthValue, Category properties; verify constructor sets all values; verify default state)

**Before T008 (TooltipData class):**
- [ ] T008.1 [P] Create TooltipDataTests in LivingRoots.Tests/Visualization/TooltipDataTests.cs (verify Text, Position, BackgroundColor, TextColor properties; verify format string "Soil Health: {percentage}% ({category})" renders correctly for each category)

**Before T009 (HoeFeedback class):**
- [ ] T009.1 [P] Create HoeFeedbackTests in LivingRoots.Tests/Visualization/HoeFeedbackTests.cs (verify TilePosition, StartTime, FlashDuration=300ms, TextDuration=1000ms, HealthValue, Category, HealthText properties; verify IsActive returns true when elapsed < max duration; verify IsExpired returns true when elapsed exceeds both durations)

### Domain Interfaces (Parallel)

> Note: Interfaces (T010-T012) define contracts and have no testable logic. Their validation comes from the service tests below. No separate test tasks needed.

### Core Services (Blocking)

**Before T013 (ColorInterpolationService):**
- [ ] T013.1 [P] Create ColorInterpolationServiceTests in LivingRoots.Tests/Visualization/ColorInterpolationServiceTests.cs (verify linear RGB interpolation per FR-008: health=0 returns PoorColor, health=100 returns HealthyColor, health=50 returns midpoint; verify GetCategoryForHealth returns correct category for boundary values 0/33/34/66/67/100; verify GetColorForHealth caches results; verify InvalidateCache clears cache; verify health values clamped to [0,100] per ModConstants; verify NaN/Infinity handled gracefully)

**Before T014 (VisualizationConfigurationService):**
- [ ] T014.1 [P] Create VisualizationConfigurationServiceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationServiceTests.cs (verify GetConfiguration never null; verify LoadConfiguration with valid JSON loads correctly; verify LoadConfiguration with invalid fields replaces only invalid values per FR-006; verify LoadConfiguration with missing file generates defaults per FR-006; verify SaveConfiguration persists via IModDataService; verify ResetToDefaults restores ModConstants values; verify UpdateConfiguration with null throws ArgumentNullException; verify thread-safe access with concurrent reads; use Mock<IModDataService>, Mock<IMonitor>, Mock<IFileNameSanitizationService>)

---

## Phase 3: User Story 1 - View Soil Health Overlay on Tilled Tiles (Priority: P1) 🎯 MVP

**Independent Test**: Till multiple soil tiles with different health values and verify the correct colors render

### Test Tasks (BEFORE implementation tasks)

**Before T015 (OverlayRenderer):**
- [ ] T015.1 [P] [US1] Create OverlayRendererTests in LivingRoots.Tests/Visualization/OverlayRendererTests.cs (verify viewport culling per FR-009: tiles outside viewport excluded; verify cache returns same list on repeated calls; verify cache invalidation per FR-010 returns fresh list; verify graceful degradation >1000 tiles per FR-009: patterns disabled, solid colors only; verify zero-tilled-tiles returns empty list per FR-021; use Mock<IColorInterpolationService>, Mock<IVisualizationConfigurationService>)

**Before T016 (RenderOverlays):**
- [ ] T016.1 [P] [US1] Create VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify RenderOverlays calls OverlayRenderer with correct viewport; verify Unknown color rendered when health data unavailable per FR-014; verify config opacity applied to all overlays; verify pattern overlays rendered per FR-001 when ShowPatterns=true; verify no draw calls when OverlaysEnabled=false; verify no exceptions with null SpriteBatch — ArgumentNullException at construction only)

**Before T017 (zero-tilled-tiles no-op):**
- [ ] T017.1 [US1] Add test to VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify zero tilled tiles: no OverlayRenderer invocation, no cache allocation, no exceptions per FR-021; verify performance: render time < 1ms for empty farm)

**Before T018 (viewport resize):**
- [ ] T018.1 [US1] Add test to VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify viewport change triggers cache recalculation per FR-016; verify 1-tile margin beyond visible bounds per FR-018; verify overlay positions updated within single frame)

**Before T019 (frame consistency):**
- [ ] T019.1 [US1] Add test to VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify mid-render health change completes current frame with old state per FR-020; verify next frame reflects new value; use ThreadSafeGameLoopEventsStub to simulate concurrent game events)

---

## Phase 4: User Story 2 - Inspect Soil Health via Hover Tooltip (Priority: P2)

**Independent Test**: Hover over tiles with known health values and verify tooltip displays correct percentage and status text

### Test Tasks (BEFORE implementation tasks)

**Before T020 (TooltipRenderer):**
- [ ] T020.1 [P] [US2] Create TooltipRendererTests in LivingRoots.Tests/Visualization/TooltipRendererTests.cs (verify cursor-to-tile mapping: screen coordinates map to correct tile; verify tooltip format "Soil Health: {percentage}% ({category})" per spec — test with 0/33/34/66/67/100 values; verify debounce logic per FR-015: rapid cursor movement suppresses updates, minimum 50ms between tooltip updates; verify cursor outside tile bounds returns null/no tooltip; use Mock<IVisualizationConfigurationService>)

**Before T021 (RenderTooltip):**
- [ ] T021.1 [P] [US2] Create VisualizationServiceTooltipTests in LivingRoots.Tests/Visualization/VisualizationServiceTooltipTests.cs (verify RenderTooltip calls TooltipRenderer when cursor over tilled tile; verify no tooltip rendered when TooltipsEnabled=false per acceptance scenario 3; verify tooltip disappears on cursor leave per acceptance scenario 2; verify tooltip position matches cursor screen coordinates)

**Before T022 (cursor tracking integration):**
- [ ] T022.1 [US2] Create ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify Input.ButtonReleased updates cursor position; verify cursor position passed to IVisualizationService.RenderTooltip; use Mock<IVisualizationService>, Mock<IVisualizationConfigurationService>, Mock<IColorInterpolationService>; use ThreadSafeGameLoopEventsStub for event simulation)

---

## Phase 5: User Story 3 - Receive Hoe Action Feedback (Priority: P2)

**Independent Test**: Use a hoe on a tilled tile and verify flash effect and floating text appear on the targeted tile only

### Test Tasks (BEFORE implementation tasks)

**Before T023 (HoeFeedbackRenderer):**
- [ ] T023.1 [P] [US3] Create HoeFeedbackRendererTests in LivingRoots.Tests/Visualization/HoeFeedbackRendererTests.cs (verify flash effect renders for 300ms duration; verify floating text renders for 1000ms duration; verify fade-out animation progresses correctly; verify HoeFeedback.IsActive returns true during duration; verify HoeFeedback.IsExpired returns true after duration; verify HealthText format matches "Soil Health: {percentage}% ({category})")

**Before T024 (TriggerHoeFeedback):**
- [ ] T024.1 [P] [US3] Create VisualizationServiceHoeFeedbackTests in LivingRoots.Tests/Visualization/VisualizationServiceHoeFeedbackTests.cs (verify TriggerHoeFeedback creates HoeFeedback state with correct tile position; verify TriggerHoeFeedback validates tile is tilled per FR-011; verify TriggerHoeFeedback on non-tilled tile does nothing per acceptance scenario 3; verify only targeted tile receives feedback per acceptance scenario 4)

**Before T025 (RenderHoeFeedback):**
- [ ] T025.1 [US3] Add test to VisualizationServiceHoeFeedbackTests in LivingRoots.Tests/Visualization/VisualizationServiceHoeFeedbackTests.cs (verify RenderHoeFeedback calls HoeFeedbackRenderer with active feedback; verify expired feedback is not rendered; verify no rendering when HoeFeedbackEnabled=false)

**Before T026 (hoe action detection):**
- [ ] T026.1 [US3] Add test to ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify hoe tool usage on tilled tile triggers IVisualizationService.TriggerHoeFeedback; verify hoe on non-tilled tile does not trigger feedback; verify correct tile position passed to TriggerHoeFeedback; use ThreadSafeGameLoopEventsStub for event simulation)

---

## Phase 6: User Story 4 - Configure Visualization Settings (Priority: P3)

**Independent Test**: Modify configuration values and verify visualization behavior changes accordingly

### Test Tasks (BEFORE implementation tasks)

**Before T027 (configuration persistence):**
- [ ] T027.1 [P] [US4] Create VisualizationConfigurationPersistenceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationPersistenceTests.cs (verify SaveConfiguration called on Saving event; verify LoadConfiguration called on SaveLoaded event; verify JSON serialization round-trip preserves all fields; verify IModDataService.SaveData called with correct key "visualization_config"; use Mock<IModDataService>, Mock<IMonitor>)

**Before T028 (configuration validation):**
- [ ] T028.1 [US4] Add test to VisualizationConfigurationPersistenceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationPersistenceTests.cs (verify invalid Opacity values (>1.0 or <0.0) replaced with defaults per FR-006; verify invalid color values replaced with defaults; verify valid fields preserved when others invalid per FR-006; verify warning logged for each invalid field)

**Before T029 (hot-reload):**
- [ ] T029.1 [US4] Add test to VisualizationConfigurationPersistenceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationPersistenceTests.cs (verify file change detection triggers reload; verify changes applied within next frame per FR-005/SC-003 (≤16.67ms); verify atomic application per FR-019: no mixed-state frames; use ThreadSafeGameLoopEventsStub to simulate concurrent rendering)

**Before T030 (default configuration creation):**
- [ ] T030.1 [US4] Add test to VisualizationConfigurationPersistenceTests in LivingRoots.Tests/Visualization/VisualizationConfigurationPersistenceTests.cs (verify missing file generates complete defaults per FR-006; verify defaults match ModConstants values; verify no exception when file not found)

**Before T031 (configuration event handlers):**
- [ ] T031.1 [US4] Add test to ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify SaveLoaded event triggers LoadConfiguration; verify Saving event triggers SaveConfiguration; verify config change triggers IColorInterpolationService.SetCategoryColors; use ThreadSafeGameLoopEventsStub for event simulation)

---

## Phase 7: Integration & Event Handling

### Test Tasks (BEFORE implementation tasks)

**Before T032 (DI registration):**
- [ ] T032.1 Create VisualizationServiceRegistrationTests in LivingRoots.Tests/Visualization/VisualizationServiceRegistrationTests.cs (verify IVisualizationService, IVisualizationConfigurationService, IColorInterpolationService registered in DI container; verify services resolve correctly from composition root; verify singleton lifetime matches existing service patterns)

**Before T033 (event handler wiring):**
- [ ] T033.1 Add test to ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify RenderedWorld event subscribed for overlays; verify Input.ButtonReleased subscribed for tooltips/hoe; verify SaveLoaded/Saving subscribed for config; use ThreadSafeGameLoopEventsStub to verify subscription counts)

**Before T034 (visualization disposal):**
- [ ] T034.1 Add test to ModControllerVisualizationTests in LivingRoots.Tests/Visualization/ModControllerVisualizationTests.cs (verify all visualization event handlers unsubscribed on Dispose per FR-012; verify lock + _disposed pattern used correctly; verify no memory leaks: zero subscriptions after disposal; use ThreadSafeGameLoopEventsStub to verify unsubscription counts)

**Before T035 (save/load rendering pause):**
- [ ] T035.1 Add test to VisualizationServiceOverlayTests in LivingRoots.Tests/Visualization/VisualizationServiceOverlayTests.cs (verify overlay rendering paused during save per FR-017; verify rendering resumed with refreshed data after load; verify no overlay frames render during save operation; use ThreadSafeGameLoopEventsStub to simulate save/load events)

---

## Phase 8: Polish & Cross-Cutting Concerns

### Test Tasks (BEFORE implementation tasks)

**Before T037 (performance optimization):**
- [ ] T037.1 [P] Create VisualizationPerformanceTests in LivingRoots.Tests/Visualization/VisualizationPerformanceTests.cs (verify 60 FPS with 1000 tiles per SC-002: render time ≤16.67ms; verify viewport culling reduces tile count correctly; verify cache hit rate >90% for static scenes; verify zero allocations in render methods (no GC pressure); verify graceful degradation activates at >1000 tiles)

**Before T039 (thread safety verification):**
- [ ] T039.1 [P] Create VisualizationThreadSafetyTests in LivingRoots.Tests/Visualization/VisualizationThreadSafetyTests.cs (verify concurrent scenario 1: health value changing mid-render per FR-020 — trigger 100 times, confirm no deadlocks; verify concurrent scenario 2: config hot-reload during active rendering per FR-019 — trigger 100 times, confirm no mixed-state frames; verify concurrent scenario 3: save/load pausing rendering per FR-017 — trigger 100 times, confirm no state corruption; use ThreadSafeGameLoopEventsStub for all scenarios; verify async-only per constitution — no .Result or .Wait() calls)

---

## Integration Test Region

All test files should include an `#region End-to-End Integration Tests` block (matching `ModControllerTests.cs` pattern) with `[Trait("Category", "Integration")]` attributed tests:

- [ ] T040 Create VisualizationIntegrationTests in LivingRoots.Tests/Visualization/VisualizationIntegrationTests.cs (end-to-end: till tile → set health → render overlay → verify color; hover tile → verify tooltip text; use hoe → verify flash + floating text; modify config → verify behavior changes; full save/load round-trip → verify visualization state preserved; use Mock<IModDataService> for persistence, ThreadSafeGameLoopEventsStub for events)

---

## Test Task Summary Table

| Phase | Test Task IDs | Implementation Task IDs | Test Files Created |
|-------|---------------|------------------------|-------------------|
| Phase 1: Setup | T001.1 | T001 | VisualizationConstantsTests.cs |
| Phase 2: Foundational | T002.1–T009.1, T013.1, T014.1 | T002–T014 | HealthCategoryTests.cs, PatternTypeTests.cs, ColorDTOTests.cs, VisualizationConfigurationTests.cs, ColorMappingTests.cs, TileOverlayTests.cs, TooltipDataTests.cs, HoeFeedbackTests.cs, ColorInterpolationServiceTests.cs, VisualizationConfigurationServiceTests.cs |
| Phase 3: US1 | T015.1–T019.1 | T015–T019 | OverlayRendererTests.cs, VisualizationServiceOverlayTests.cs |
| Phase 4: US2 | T020.1–T022.1 | T020–T022 | TooltipRendererTests.cs, VisualizationServiceTooltipTests.cs, ModControllerVisualizationTests.cs |
| Phase 5: US3 | T023.1–T026.1 | T023–T026 | HoeFeedbackRendererTests.cs, VisualizationServiceHoeFeedbackTests.cs |
| Phase 6: US4 | T027.1–T031.1 | T027–T031 | VisualizationConfigurationPersistenceTests.cs |
| Phase 7: Integration | T032.1–T035.1 | T032–T035 | VisualizationServiceRegistrationTests.cs |
| Phase 8: Polish | T037.1, T039.1 | T037, T039 | VisualizationPerformanceTests.cs, VisualizationThreadSafetyTests.cs |
| Integration | T040 | — | VisualizationIntegrationTests.cs |

**Total new test tasks**: 32  
**Total new test files**: 18  
**Test-to-implementation ratio**: ~2.3:1 (each implementation task has 1-4 corresponding test tasks)

---

## TDD Enforcement Notes

1. **Red-Green-Refactor**: Each test task MUST produce a failing test before its implementation task begins. The test file is created with the correct assertions, then the implementation is written to make it pass.

2. **Test Independence**: Each test creates its own mocks and stubs. No shared mutable state between tests (per AGENTS.md anti-patterns).

3. **Parallel Execution**: Tasks marked [P] can run simultaneously — they target different test files with no dependencies.

4. **Constitution Compliance**: These tasks satisfy Principle IV (TDD) by ensuring:
   - Tests written before production code ✓
   - Red-Green-Refactor cycle enforced ✓
   - Integration tests cover save/load round-trips, tile key parsing, security validation ✓
   - ThreadSafeGameLoopEventsStub used for concurrent game events ✓
   - One test file per subject: `{Subject}Tests.cs` ✓

5. **Quality Gate Alignment**: All tests must pass before Gate 1 (`dotnet test --nologo`) is satisfied.

---

## Insertion Instructions

To integrate these tasks into `tasks.md`:

1. Replace line 7: `"Tests: No test tasks included..."` with `"Tests: TDD test tasks included below (required by Constitution Principle IV)"`
2. Insert each test task **immediately before** its corresponding implementation task
3. Renumber existing tasks if needed (or use decimal IDs as shown)
4. Update Phase 8 to include T037.1 and T039.1 before T037 and T039
