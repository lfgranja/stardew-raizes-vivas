# Tasks: Composting State Machine Tests

**Input**: Design documents from `/specs/003-composting-tests/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: YES — this is a test specification. Test tasks are included for all user stories.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create test stubs and fixtures shared across all test files

- [ ] T001 Create `TimeProviderStub` class in `LivingRoots.Tests/Stubs/TimeProviderStub.cs` implementing `ITimeProvider` with settable `TotalDays` property
- [ ] T002 Create `SeasonProviderStub` class in `LivingRoots.Tests/Stubs/SeasonProviderStub.cs` implementing `ISeasonProvider` with settable `CurrentSeason` property
- [ ] T003 Create `PlayerProviderStub` class in `LivingRoots.Tests/Stubs/PlayerProviderStub.cs` implementing `IPlayerProvider` with settable `CurrentItem` property
- [ ] T004 Create `GameLocationFixture` class in `LivingRoots.Tests/Fixtures/GameLocationFixture.cs` for creating real `GameLocation` instances with `HoeDirt` tiles using correct constructor signature `GameLocation(string mapPath, string name)`
- [ ] T005 Create `ItemFactory` static class in `LivingRoots.Tests/Fixtures/ItemFactory.cs` with methods `CreateValidWasteItem()`, `CreateInvalidWasteItem()`, `CreateItem(string qualifiedItemId, int category)`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Prerequisite fixes that MUST complete before ANY user story tests can pass

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Fix-4: Extract Game1 Dependencies (Interfaces + Production Impls)

- [ ] T006 [P] Create `ITimeProvider` interface in `LivingRoots/Domain/Interfaces/ITimeProvider.cs` with `int TotalDays { get; }`
- [ ] T007 [P] Create `ISeasonProvider` interface in `LivingRoots/Domain/Interfaces/ISeasonProvider.cs` with `string CurrentSeason { get; }`
- [ ] T008 [P] Create `IPlayerProvider` interface in `LivingRoots/Domain/Interfaces/IPlayerProvider.cs` with `Farmer CurrentPlayer { get; }` and `Item? CurrentItem { get; }`
- [ ] T009 [P] Create `TimeProvider` production implementation in `LivingRoots/Services/TimeProvider.cs` wrapping `Game1.Date.TotalDays`
- [ ] T010 [P] Create `SeasonProvider` production implementation in `LivingRoots/Services/SeasonProvider.cs` wrapping `Game1.currentSeason`
- [ ] T011 [P] Create `PlayerProvider` production implementation in `LivingRoots/Services/PlayerProvider.cs` wrapping `Game1.player`

### Fix-1: Cross-Day Elapsed Time + Fix-3: ConsecutiveActiveDays Persistence

- [ ] T012 Add `MaturationDays = 2` constant to `LivingRoots/Constants.cs` (replaces `ProcessingDurationMinutes`)
- [ ] T013 Change `InputTimestamp` type from `long?` to `int?` in `LivingRoots/Domain/Models/CompostingBinStateModel.cs`
- [ ] T014 Add `ConsecutiveActiveDays` field to `LivingRoots/Domain/Models/CompostingBinData.cs` (Fix-3: persistence)
- [ ] T015 Update `CompostingBinService` constructor to accept `ITimeProvider` in `LivingRoots/Services/CompostingBinService.cs`
- [ ] T016 Replace `Game1.timeOfDay` elapsed calculation with `ITimeProvider.TotalDays` comparison in `CompostingBinService.ProcessDayStart()`: `currentDay - recordedDay >= MaturationDays`
- [ ] T017 Update `CompostingBinService.SaveData()` and `LoadData()` to persist `ConsecutiveActiveDays`

### Fix-2: ProcessDayStart Wiring

- [ ] T018 Add `DayStarted` event subscription in `LivingRoots/Controllers/ModController.cs` wiring both `CompostingBinService.ProcessDayStart` and `SoilDecayService.ProcessDayStart`
- [ ] T019 Merge `CompostingBinController` right-click logic into `ModController` as `OnButtonPressed` handler; delete `LivingRoots/Controllers/CompostingBinController.cs`

### Fix-4 (Service Updates): Inject Interfaces into Services

- [ ] T020 Update `CompostApplicationService` constructor to accept `IPlayerProvider` in `LivingRoots/Services/CompostApplicationService.cs`; replace `Game1.player` access with `_playerProvider`
- [ ] T021 Update `SoilDecayService` constructor to accept `ISeasonProvider` in `LivingRoots/Services/SoilDecayService.cs`; replace `Game1.currentSeason` access with `_seasonProvider`

### Fix-5: CompostingBinFactory Usage

- [ ] T022 Update `CompostingBinService` constructor to accept `CompostingBinFactory` in `LivingRoots/Services/CompostingBinService.cs`; replace inline `new CompostingBinStateModel` in `AddWaste` with `_factory.CreateBin(tileX, tileY)`

### Fix-4 (Composition Root): Wire Production Implementations

- [ ] T023 Update `ModEntry.cs` to instantiate `TimeProvider`, `SeasonProvider`, `PlayerProvider` and inject into services

**Checkpoint**: All prerequisite fixes complete — user story test implementation can now begin

---

## Phase 3: User Story 1 - Composting State Machine Tests (Priority: P1) 🎯 MVP

**Goal**: Test all state transitions, invalid transitions, and edge cases for the composting state machine

**Independent Test**: Run `dotnet test --filter "FullyQualifiedName~CompostingBinServiceTests"` — all ~15 tests pass

### Tests for User Story 1

- [ ] T024 [P] [US1] Create `CompostingBinServiceTests.cs` in `LivingRoots.Tests/` with test class constructor creating `Mock<IModDataService>`, `Mock<ISaveIdProvider>`, `Mock<IOrganicWasteValidator>`, `Mock<IMonitor>`, `TimeProviderStub`, and `CompostingBinFactory`
- [ ] T025 [P] [US1] Add test `GetBinState_NewBin_ReturnsEmpty` (FR-1.1): query unknown tile returns `Empty`
- [ ] T026 [P] [US1] Add test `AddWaste_ValidItem_TransitionsToProcessing` (FR-1.2): valid waste moves to `Processing`
- [ ] T027 [P] [US1] Add test `ProcessDayStart_AfterTwoDays_TransitionsToReady` (FR-1.3): advance 2 days via `TimeProviderStub`, call `ProcessDayStart`, verify `Ready`
- [ ] T028 [P] [US1] Add test `CollectCompost_FromReadyBin_ReturnsCompostCount` (FR-1.4): collect returns `MaturationLevel`, state = `Empty`
- [ ] T029 [P] [US1] Add test `AddWaste_ToProcessingBin_IsIgnored` (FR-1.5): add while `Processing` → state unchanged
- [ ] T030 [P] [US1] Add test `AddWaste_ToReadyBin_IsIgnored` (FR-1.6): add while `Ready` → state unchanged
- [ ] T031 [P] [US1] Add test `CollectCompost_FromEmptyBin_ReturnsZero` (FR-1.7): collect from `Empty` → returns 0
- [ ] T032 [P] [US1] Add test `CollectCompost_FromProcessingBin_ReturnsZero` (FR-1.8): collect from `Processing` → returns 0
- [ ] T033 [P] [US1] Add test `ProcessDayStart_AtThreshold_TransitionsCorrectly` (FR-1.9): verify transition at exactly 2 days (not 1, not 3)
- [ ] T034 [P] [US1] Add test `FullLifecycle_AddProcessReadyCollectAddAgain` (FR-1.10): complete cycle twice
- [ ] T035 [P] [US1] Add test `ProcessDayStart_SevenActiveDays_IncrementsMaturation` (FR-1.11): 7 active days → `MaturationLevel += 1`
- [ ] T036 [P] [US1] Add test `ProcessDayStart_FourteenIdleDays_ResetsMaturation` (FR-1.12): 14 idle days → `MaturationLevel = 1`
- [ ] T037 [P] [US1] Add test `CollectCompost_OutputCountEqualsMaturationLevel` (FR-1.13): collect at level 3 → returns 3
- [ ] T038 [P] [US1] Add test `AddWaste_InvalidItem_IsIgnored`: invalid waste → state unchanged (edge case, no FR mapping)
- [ ] T039 [P] [US1] Add test `ProcessDayStart_EmptyLocation_IsNoOp`: process empty location → no exception

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Supporting Services Tests (Priority: P2)

**Goal**: Test all supporting services (CompostApplicationService, SoilDecayService, OrganicWasteValidator, SeasonalDecayMultiplier, SaveIdProvider)

**Independent Test**: Run `dotnet test --filter "FullyQualifiedName~CompostApplicationServiceTests|FullyQualifiedName~SoilDecayServiceTests|FullyQualifiedName~OrganicWasteValidatorTests|FullyQualifiedName~SeasonalDecayMultiplierTests|FullyQualifiedName~SaveIdProviderTests"` — all ~26 tests pass

### Tests for User Story 2

- [ ] T040 [P] [US2] Create `CompostApplicationServiceTests.cs` in `LivingRoots.Tests/` with constructor creating `Mock<ISoilHealthService>`, `Mock<IMonitor>`, `PlayerProviderStub`
- [ ] T041 [P] [US2] Add test `TryApplyCompost_ValidTile_IncreasesHealth` (FR-2.1): health increases by `RestorationAmount`
- [ ] T042 [P] [US2] Add test `TryApplyCompost_AtMaxHealth_ReturnsFalse` (FR-2.2): at max health → returns false
- [ ] T043 [P] [US2] Add test `TryApplyCompost_InvalidLocation_ReturnsFalse` (FR-2.3): non-farm location → returns false
- [ ] T044 [P] [US2] Add test `TryApplyCompost_NoCompostHeld_ReturnsFalse` (FR-2.4): null/non-compost item → returns false
- [ ] T045 [P] [US2] Add test `TryApplyCompost_NonHoeDirtTile_ReturnsFalse`: non-tilled tile → returns false
- [ ] T046 [P] [US2] Add test `TryApplyCompost_HealthCappedAtMax` (FR-2.2): near max health → capped at 100

- [ ] T047 [P] [US2] Create `SoilDecayServiceTests.cs` in `LivingRoots.Tests/` with constructor creating `Mock<ISoilHealthService>`, `Mock<IMonitor>`, `SeasonProviderStub`, `SeasonalDecayMultiplier`
- [ ] T048 [P] [US2] Add test `ProcessDayStart_BareTile_DecaysHealth` (FR-3.1): bare tile health decreases by `DailyDecayRate × multiplier`
- [ ] T049 [P] [US2] Add test `ProcessDayStart_LowHealth_DoesNotGoNegative` (FR-3.2): low health → stays at 0
- [ ] T050 [P] [US2] Add test `ProcessDayStart_AtZeroHealth_StaysAtZero` (FR-3.3): zero health → stays at 0
- [ ] T051 [P] [US2] Add test `ProcessDayStart_SummerDecay_HigherThanSpring` (FR-3.4): summer (1.5x) > spring (0.5x)
- [ ] T052 [P] [US2] Add test `ProcessDayStart_Winter_NoDecay` (FR-3.4): winter (0.0x) → no decay
- [ ] T053 [P] [US2] Add test `ProcessDayStart_CoveredTile_NoDecay`: tile with crop → no decay

- [ ] T054 [P] [US2] Create `OrganicWasteValidatorTests.cs` in `LivingRoots.Tests/` with constructor creating `Mock<IMonitor>`
- [ ] T055 [P] [US2] Add test `IsValidOrganicWaste_Seeds_ReturnsTrue` (FR-4.1): category -74 → true
- [ ] T056 [P] [US2] Add test `IsValidOrganicWaste_Vegetables_ReturnsTrue` (FR-4.1): category -75 → true
- [ ] T057 [P] [US2] Add test `IsValidOrganicWaste_Fruits_ReturnsTrue` (FR-4.1): category -79 → true
- [ ] T058 [P] [US2] Add test `IsValidOrganicWaste_Flowers_ReturnsTrue` (FR-4.1): category -80 → true
- [ ] T059 [P] [US2] Add test `IsValidOrganicWaste_Forage_ReturnsTrue` (FR-4.1): category -81 → true
- [ ] T060 [P] [US2] Add test `IsValidOrganicWaste_Stone_ReturnsFalse` (FR-4.2): category -12 → false
- [ ] T061 [P] [US2] Add test `IsValidOrganicWaste_Wood_ReturnsFalse` (FR-4.2): category -14 → false
- [ ] T062 [P] [US2] Add test `IsValidOrganicWaste_NullItem_ReturnsFalse` (FR-4.3): null → false
- [ ] T063 [P] [US2] Add test `IsValidOrganicWaste_CompostableTag_ReturnsTrue`: `compostable_item` tag → true
- [ ] T064 [P] [US2] Add test `IsValidOrganicWaste_NotCompostableTag_ReturnsFalse`: `not_compostable` tag → false

- [ ] T065 [P] [US2] Create `SeasonalDecayMultiplierTests.cs` in `LivingRoots.Tests/`
- [ ] T066 [P] [US2] Add test `GetMultiplier_Spring_ReturnsHalf` (FR-5.1): "spring" → 0.5f
- [ ] T067 [P] [US2] Add test `GetMultiplier_Summer_ReturnsOneAndHalf` (FR-5.2): "summer" → 1.5f
- [ ] T068 [P] [US2] Add test `GetMultiplier_Fall_ReturnsHalf` (FR-5.3): "fall" → 0.5f
- [ ] T069 [P] [US2] Add test `GetMultiplier_Winter_ReturnsZero` (FR-5.4): "winter" → 0.0f
- [ ] T070 [P] [US2] Add test `GetMultiplier_InvalidSeason_ReturnsDefault` (FR-5.5): "invalid" → 0.5f

- [ ] T071 [P] [US2] Create `SaveIdProviderTests.cs` in `LivingRoots.Tests/` with reflection helper for `Constants.SaveFolderName`
- [ ] T072 [P] [US2] Add test `GetSaveId_WithValidSaveFolder_ReturnsId` (FR-6.1): valid folder → returns ID
- [ ] T073 [P] [US2] Add test `GetSaveId_WithNullSaveFolder_ReturnsNull` (FR-6.2): null → returns null
- [ ] T074 [P] [US2] Add test `GetSaveId_WithEmptySaveFolder_ReturnsNull` (FR-6.2): empty → returns null
- [ ] T075 [P] [US2] Add test `GetSaveId_WithWhitespaceSaveFolder_ReturnsNull` (FR-6.2): whitespace → returns null
- [ ] T076 [P] [US2] Add test `GetSaveId_WithTooLongSaveFolder_ReturnsNull`: > 200 chars → returns null
- [ ] T077 [P] [US2] Add test `GetSaveId_MultipleCalls_ReturnsSameId` (FR-6.3): multiple calls → same ID

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Factory Tests & Quality (Priority: P3)

**Goal**: Test CompostingBinFactory and verify overall test suite quality (meaningful assertions, independence, no reflection)

**Independent Test**: Run `dotnet test --filter "FullyQualifiedName~CompostingBinFactoryTests"` — all ~3 tests pass. Run full suite: all ~51 tests pass with no failures.

### Tests for User Story 3

- [ ] T078 [P] [US3] Create `CompostingBinFactoryTests.cs` in `LivingRoots.Tests/`
- [ ] T079 [P] [US3] Add test `CreateBin_ReturnsEmptyState` (FR-7.1): new bin has `State == Empty`
- [ ] T080 [P] [US3] Add test `CreateBin_HasCorrectDefaults` (FR-7.2): `MaturationLevel == 1`, tile coordinates match input
- [ ] T081 [P] [US3] Add test `CreateBin_DifferentCoordinates` (FR-7.2): different coords → correct TileX/TileY

### Quality Verification

- [ ] T082 [US3] Verify every test has at least one assertion (Success Criterion 4): grep test files for `Assert.` calls, confirm count matches test count
- [ ] T083 [US3] Verify tests do not use reflection to access private state (NFR-4): grep test files for `BindingFlags.NonPublic|GetField|GetMethod` — only allowed in `SaveIdProviderTests` for `Constants.SaveFolderName`
- [ ] T084 [US3] Verify test independence (Success Criterion 5): run `dotnet test` with `--parallel` flag, confirm no ordering dependencies

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and quality gates

- [ ] T085 [P] Run full test suite: `dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal` — all tests pass
- [ ] T086 [P] Verify test execution time under 5 seconds (NFR-1): `dotnet test --logger "console;verbosity=detailed"` and check elapsed time
- [ ] T087 [P] Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` — formatting passes
- [ ] T088 Run quickstart.md validation scenarios to confirm end-to-end correctness
- [ ] T089 Code cleanup: remove any dead code, unused `using` statements, or TODO markers in test files

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - US1, US2, US3 can proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational (Phase 2) — No dependencies on other stories
- **US2 (P2)**: Can start after Foundational (Phase 2) — No dependencies on US1
- **US3 (P3)**: Can start after Foundational (Phase 2) — No dependencies on US1/US2

### Within Each User Story

- All test tasks marked [P] can run in parallel (different test methods, same file)
- Test file creation must precede test method additions within that file
- Models/stubs (Phase 1) must be available before test files reference them

### Parallel Opportunities

- All Setup tasks (T001-T005) can run in parallel — different files
- Fix-4 interface creation tasks (T006-T008) can run in parallel
- Fix-4 production implementation tasks (T009-T011) can run in parallel
- All US1 test tasks (T025-T039) can run in parallel (same file, different methods)
- All US2 test tasks (T041-T077) can run in parallel across different test files
- US1, US2, US3 can be worked on in parallel by different team members after Foundational phase

---

## Parallel Example: User Story 1

```
# Launch all tests for User Story 1 together:
Task: "Add test GetBinState_NewBin_ReturnsEmpty (FR-1.1)"
Task: "Add test AddWaste_ValidItem_TransitionsToProcessing (FR-1.2)"
Task: "Add test ProcessDayStart_AfterTwoDays_TransitionsToReady (FR-1.3)"
Task: "Add test CollectCompost_FromReadyBin_ReturnsCompostCount (FR-1.4)"
Task: "Add test AddWaste_ToProcessingBin_IsIgnored (FR-1.5)"
Task: "Add test AddWaste_ToReadyBin_IsIgnored (FR-1.6)"
Task: "Add test CollectCompost_FromEmptyBin_ReturnsZero (FR-1.7)"
Task: "Add test CollectCompost_FromProcessingBin_ReturnsZero (FR-1.8)"
Task: "Add test ProcessDayStart_AtThreshold_TransitionsCorrectly (FR-1.9)"
Task: "Add test FullLifecycle_AddProcessReadyCollectAddAgain (FR-1.10)"
Task: "Add test ProcessDayStart_SevenActiveDays_IncrementsMaturation (FR-1.11)"
Task: "Add test ProcessDayStart_FourteenIdleDays_ResetsMaturation (FR-1.12)"
Task: "Add test CollectCompost_OutputCountEqualsMaturationLevel (FR-1.13)"
Task: "Add test AddWaste_InvalidItem_IsIgnored (edge case)"
Task: "Add test ProcessDayStart_EmptyLocation_IsNoOp"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T005)
2. Complete Phase 2: Foundational (T006-T023) — CRITICAL
3. Complete Phase 3: User Story 1 (T024-T039)
4. **STOP and VALIDATE**: Run `dotnet test --filter "FullyQualifiedName~CompostingBinServiceTests"`
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Polish → Final validation

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (T024-T039)
   - Developer B: User Story 2 (T040-T077)
   - Developer C: User Story 3 (T078-T084)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence

---

## Phase 7: Convergence

**Purpose**: Prerequisite fixes and test infrastructure not yet implemented — code does not satisfy spec/plan/tasks

**⚠️ CRITICAL**: No test work can begin until prerequisite fixes (F1–F4) are complete

### Prerequisite Fixes (CRITICAL)

- [X] T090 Fix cross-day elapsed time bug per Fix-1 (`missing`): change `InputTimestamp` from `long?` to `int?` in `CompostingBinStateModel.cs` and `CompostingBinStateData.cs`; add `MaturationDays = 2` constant to `Constants.cs`; replace `Game1.timeOfDay` elapsed calculation with `ITimeProvider.TotalDays` comparison in `CompostingBinService.ProcessDayStart()`
- [X] T091 Wire ProcessDayStart to DayStarted event per Fix-2 (`missing`): add `DayStarted` subscription in `ModController.cs` wiring both `CompostingBinService.ProcessDayStart` and `SoilDecayService.ProcessDayStart`; merge `CompostingBinController` right-click logic into `ModController` as `OnButtonPressed` handler; delete standalone `CompostingBinController.cs`
- [X] T092 Add ConsecutiveActiveDays persistence per Fix-3 (`missing`): add `ConsecutiveActiveDays` field to `CompostingBinStateData`; populate in `CompostingBinService.SaveData()`; restore in `CompostingBinService.LoadData()`
- [X] T093 Extract Game1 time dependency behind ITimeProvider per Fix-4 (`missing`): create `ITimeProvider` interface in `Domain/Interfaces/ITimeProvider.cs` with `int TotalDays { get; }`; create `TimeProvider` production impl in `Services/TimeProvider.cs` wrapping `Game1.Date.TotalDays`; update `CompostingBinService` constructor to accept `ITimeProvider`
- [X] T094 Extract Game1 season dependency behind ISeasonProvider per Fix-4 (`missing`): create `ISeasonProvider` interface in `Domain/Interfaces/ISeasonProvider.cs` with `string CurrentSeason { get; }`; create `SeasonProvider` production impl in `Services/SeasonProvider.cs` wrapping `Game1.currentSeason`; update `SoilDecayService` constructor to accept `ISeasonProvider`
- [X] T095 Extract Game1 player dependency behind IPlayerProvider per Fix-4 (`missing`): create `IPlayerProvider` interface in `Domain/Interfaces/IPlayerProvider.cs` with `Farmer CurrentPlayer { get; }` and `Item? CurrentItem { get; }`; create `PlayerProvider` production impl in `Services/PlayerProvider.cs` wrapping `Game1.player`; update `CompostApplicationService` constructor to accept `IPlayerProvider`
- [X] T096 Refactor CompostingBinService to use CompostingBinFactory per Fix-5 (`missing`): update `CompostingBinService` constructor to accept `CompostingBinFactory`; replace inline `new CompostingBinStateModel` in `AddWaste` with `_factory.CreateBin(tileX, tileY)`; update `ModEntry.cs` to wire all new dependencies

### Test Infrastructure Stubs

- [X] T097 Create `TimeProviderStub` in `LivingRoots.Tests/Stubs/TimeProviderStub.cs` per plan: Setup (`missing`): implement `ITimeProvider` with settable `TotalDays` property
- [X] T098 Create `SeasonProviderStub` in `LivingRoots.Tests/Stubs/SeasonProviderStub.cs` per plan: Setup (`missing`): implement `ISeasonProvider` with settable `CurrentSeason` property
- [X] T099 Create `PlayerProviderStub` in `LivingRoots.Tests/Stubs/PlayerProviderStub.cs` per plan: Setup (`missing`): implement `IPlayerProvider` with settable `CurrentPlayer` and `CurrentItem`
- [X] T100 Create `GameLocationFixture` in `LivingRoots.Tests/Fixtures/GameLocationFixture.cs` per plan: Setup (`missing`): helper for creating real `GameLocation` instances with `HoeDirt` tiles using `GameLocation(string mapPath, string name)`
- [X] T101 Create `ItemFactory` in `LivingRoots.Tests/Fixtures/ItemFactory.cs` per plan: Setup (`missing`): static methods `CreateValidWasteItem()`, `CreateInvalidWasteItem()`, `CreateItem(string qualifiedItemId, int category)`

### Test Files

- [X] T102 Create `CompostingBinServiceTests.cs` in `LivingRoots.Tests/` per FR-1 (`missing`): ~15 tests covering FR-1.1 through FR-1.13 — all state transitions, invalid transitions, maturation, time progression via `TimeProviderStub`
- [X] T103 Create `CompostApplicationServiceTests.cs` in `LivingRoots.Tests/` per FR-2 (`missing`): ~6 tests covering FR-2.1 through FR-2.4 — compost application, max health cap, invalid location, no compost held
- [X] T104 Create `SoilDecayServiceTests.cs` in `LivingRoots.Tests/` per FR-3 (`missing`): ~6 tests covering FR-3.1 through FR-3.4 — daily decay, min health, zero health, seasonal variation
- [X] T105 Create `OrganicWasteValidatorTests.cs` in `LivingRoots.Tests/` per FR-4 (`missing`): ~10 tests covering FR-4.1 through FR-4.3 — valid categories (-74,-75,-79,-80,-81), invalid items, null input, context tags
- [X] T106 Create `SeasonalDecayMultiplierTests.cs` in `LivingRoots.Tests/` per FR-5 (`missing`): ~5 tests covering FR-5.1 through FR-5.5 — spring/summer/fall/winter multipliers + invalid season default
- [X] T107 Create `SaveIdProviderTests.cs` in `LivingRoots.Tests/` per FR-6 (`missing`): ~6 tests covering FR-6.1 through FR-6.3 — valid save folder, null/empty/whitespace/long folder, consistency
- [X] T108 Create `CompostingBinFactoryTests.cs` in `LivingRoots.Tests/` per FR-7 (`missing`): ~3 tests covering FR-7.1 through FR-7.2 — default state, MaturationLevel=1, tile coordinates

### Quality Verification

- [X] T109 Verify every test has at least one assertion per Success Criterion 4 (`missing`): grep test files for `Assert.` calls, confirm count matches test count
- [X] T110 Verify tests do not use reflection to access private state per NFR-4 (`missing`): grep test files for `BindingFlags.NonPublic|GetField|GetMethod` — only allowed in `SaveIdProviderTests` for `Constants.SaveFolderName`
- [X] T111 Run full test suite per NFR-1 (`missing`): `dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal` — all tests pass, execution under 5 seconds
