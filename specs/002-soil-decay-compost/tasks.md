# Tasks: Soil Health Decay + Compost Restoration

**Input**: Design documents from `/specs/002-soil-decay-compost/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `constitution.md`

**Tests**: TDD is MANDATORY per Constitution Principle IV. Test tasks are written first and BLOCK implementation.

**Organization**: Tasks are grouped by phase and user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Subagent Driven Development Execution Waves

- **Wave 1 (Parallel Setup & TDD Contracts)**: Interfaces, domain constants, and unit test suites [P] (blocking implementation).
- **Wave 2 (Domain Services & Pure Models)**: Implementations of validators, decay calculators, state models, fixtures [P].
- **Wave 3 (Application Services & Controller Integration)**: Integration of services (`SoilDecayService`, `CompostApplicationService`, `CompostingBinService`), ModController wiring, persistence, i18n, assets.
- **Wave 4 (Verification Gates)**: Automated verification gates (Gate 1, Gate 2, Gate 3) — ONLY these gates attest completion.

## Path Conventions

- **Source**: `LivingRoots/` (Domain/, Services/, Controllers/)
- **Tests**: `LivingRoots.Tests/`
- **Assets**: `LivingRoots/Assets/`
- **i18n**: `LivingRoots/i18n/`

---

## Phase 1: Setup & TDD Prerequisites (Wave 1)

**Purpose**: Constants, test abstractions, and domain interfaces

- [x] T001 Add decay and compost constants to `LivingRoots/Constants.cs` (`DailyDecayRate = 2f`, `RestorationAmount = 15f`, `MaturationDays = 2`, `MaturationMaxLevel = 5`, `MaturationIdleResetDays = 14`, `CompostItemId = "LivingRoots.Compost"`, `CompostingBinItemId = "LivingRoots.CompostingBin"`, `CompostingBinRecipeId = "LivingRoots.CompostingBinRecipe"`, `CompostingBinKeyPrefix = "composting_bins_"`)
- [x] T002 [P] Create `ITimeProvider` interface in `LivingRoots/Domain/ITimeProvider.cs` (`int TotalDays { get; }`) and `TimeProviderStub` in `LivingRoots.Tests/Stubs/TimeProviderStub.cs`
- [x] T003 [P] Create `ISeasonProvider` interface in `LivingRoots/Domain/ISeasonProvider.cs` (`string CurrentSeason { get; }`) and `SeasonProviderStub` in `LivingRoots.Tests/Stubs/SeasonProviderStub.cs`
- [x] T004 [P] Create `IPlayerProvider` interface in `LivingRoots/Domain/IPlayerProvider.cs` (`Farmer CurrentPlayer { get; }`, `Item? CurrentItem { get; }`) and `PlayerProviderStub` in `LivingRoots.Tests/Stubs/PlayerProviderStub.cs`
- [x] T005 [P] Create `ILocationProvider` interface in `LivingRoots/Domain/ILocationProvider.cs` (`GameLocation? GetLocationByName(string name)`)
- [x] T006 [P] Create `IPlayerInventory` interface in `LivingRoots/Domain/IPlayerInventory.cs` (`bool TryAddItem(Item item)`)
- [x] T007 [P] Create `ISoilDecayService` interface in `LivingRoots/Domain/ISoilDecayService.cs` (`void ProcessDayStart(string locationName)`)
- [x] T008 [P] Create `ICompostingBinService` interface in `LivingRoots/Domain/ICompostingBinService.cs` (`void AddWaste`, `int CollectCompost`, `CompostingBinState GetBinState`, `void ProcessDayStart`)
- [x] T009 [P] Create `ICompostApplicationService` interface in `LivingRoots/Domain/ICompostApplicationService.cs` (`bool TryApplyCompost(GameLocation location, Vector2 tile)`)
- [x] T010 [P] Create `IOrganicWasteValidator` interface in `LivingRoots/Domain/IOrganicWasteValidator.cs` (`bool IsValidOrganicWaste(Item item)`)

---

## Phase 2: Foundational Domain Models & Unit Tests (Wave 1 & Wave 2)

**Purpose**: Core domain entities and failing unit test suites that BLOCK implementation per Constitution IV

**⚠️ CRITICAL**: No implementation service can proceed until these test tasks are defined and run failing

### TDD Unit Tests (Wave 1 - Blocking)

- [x] T011 [P] Create `OrganicWasteValidatorTests` in `LivingRoots.Tests/OrganicWasteValidatorTests.cs` (assert categories -74, -75, -79, -80, -81 and `compostable_item` tag return true; assert `not_compostable` tag returns false; assert null input returns false)
- [x] T012 [P] Create `SeasonalDecayMultiplierTests` in `LivingRoots.Tests/SeasonalDecayMultiplierTests.cs` (assert spring=0.5f, summer=1.5f, fall=0.5f, winter=0.0f, unknown defaults to 0.5f)
- [x] T013 [P] Create `SoilDecayServiceTests` in `LivingRoots.Tests/SoilDecayServiceTests.cs` (assert bare tilled tile loses `DailyDecayRate * multiplier`; assert health floor at 0; assert crop and dead crop mulch prevent decay; assert Greenhouse exempt)
- [x] T014 [P] Create `CompostApplicationServiceTests` in `LivingRoots.Tests/CompostApplicationServiceTests.cs` (assert +15 restoration; assert health ceiling at 100; assert 1 compost consumed; assert invalid target or full health rejected with cancel sound and no consumption)
- [x] T015 [P] Create `CompostingBinServiceTests` in `LivingRoots.Tests/CompostingBinServiceTests.cs` (assert state machine Empty→Processing→Ready→Empty; assert 2-day maturation; assert output multiplier 1x to 5x; assert 14-day idle reset)

### Domain Entities & Providers (Wave 2)

- [x] T016 [P] Create `CompostingBinState` enum in `LivingRoots/Domain/CompostingBinState.cs` (`Empty`, `Processing`, `Ready`)
- [x] T017 [P] Create `CompostingBinStateModel` in `LivingRoots/Domain/Models/CompostingBinStateModel.cs` and `CompostingBinStateData` persistence DTO in `LivingRoots/Domain/Models/CompostingBinStateData.cs` (`TileX`, `TileY`, `State`, `InputItemId`, `InputTimestamp`, `MaturationLevel`, `ConsecutiveIdleDays`, `ConsecutiveActiveDays`)
- [x] T018 [P] Implement `OrganicWasteValidator` in `LivingRoots/Domain/Services/OrganicWasteValidator.cs` satisfying T011
- [x] T019 [P] Implement `SeasonalDecayMultiplier` in `LivingRoots/Domain/Services/SeasonalDecayMultiplier.cs` satisfying T012
- [x] T020 [P] Implement `CompostingBinFactory` in `LivingRoots/Services/CompostingBinFactory.cs` (creates default bin state model at specified tile coordinates)
- [x] T021 [P] Implement `TimeProvider`, `SeasonProvider`, `PlayerProvider`, `LocationProvider`, and `PlayerInventory` wrappers in `LivingRoots/Services/`

**Checkpoint**: Foundation and unit test harnesses ready. Implementation of user stories can proceed.

---

## Phase 3: User Story 1 - Soil Decays When Left Bare (Priority: P1) 🎯 MVP

**Goal**: Tilled soil loses health each day when left bare, modified by seasonal multiplier. Greenhouse is exempt.

**Independent Test**: Verify via `SoilDecayServiceTests` that bare tiles decay, crops/residue protect soil, and health never drops below 0.

### Implementation for User Story 1 (Wave 2 & Wave 3)

- [x] T022 [US1] Implement `SoilDecayService` in `LivingRoots/Services/SoilDecayService.cs` (iterates `location.terrainFeatures`, checks `HoeDirt.crop == null` for bare, `crop.dead` for residue, applies seasonal multiplier, clamps floor to 0, logs trace summary per FR-001, FR-002, FR-003, FR-022)
- [x] T023 [US1] Wire `SoilDecayService.ProcessDayStart` in `LivingRoots/Controllers/ModController.cs` on `DayStarted` event under concurrency guard

**Checkpoint**: User Story 1 is functional and verifiable via unit tests.

---

## Phase 4: User Story 2 - Restore Health with Compost (Priority: P1)

**Goal**: Player applies compost to tilled soil to restore health (+15). Compost is consumed on success, rejected on invalid targets.

**Independent Test**: Verify via `CompostApplicationServiceTests` that compost restores health, clamps to 100, consumes 1 item, and triggers cancel feedback when invalid.

### Implementation for User Story 2 (Wave 3)

- [x] T024 [US2] Implement `CompostApplicationService` in `LivingRoots/Services/CompostApplicationService.cs` (validates farm/Greenhouse location, `HoeDirt` presence, health < 100, player holding `LivingRoots.Compost`; updates health via `ISoilHealthService`, decrements stack, spawns `TemporaryAnimatedSprite` with text "+15", plays "cancel" on failure per FR-004 to FR-007, FR-012, FR-015, FR-017)
- [x] T025 [US2] Inject `ICompostApplicationService` into `LivingRoots/Controllers/ModController.cs` and wire `TryApplyCompost` in `OnButtonPressed` on right-click when holding compost
- [x] T026 [US2] Wire `ICompostApplicationService` in `LivingRoots/ModEntry.cs` composition root

**Checkpoint**: User Stories 1 and 2 complete the soil decay and restoration loop.

---

## Phase 5: User Story 3 - Produce Compost via Composting Bin (Priority: P2)

**Goal**: Composting bin machine converts organic waste to compost over 2 days with maturation scaling (1x to 5x).

**Independent Test**: Verify via `CompostingBinServiceTests` that waste is consumed, maturation increments, compost is yielded, and save/load preserves state.

### Implementation for User Story 3 (Wave 3)

- [x] T027 [US3] Implement `CompostingBinService` in `LivingRoots/Services/CompostingBinService.cs` (`AddWaste`, `CollectCompost`, `GetBinState`, `ProcessDayStart`, thread-safe cache, save/load persistence via `IModDataService` with key `composting_bins_{saveId}_{locationName}` per FR-008, FR-009, FR-013, FR-014)
- [x] T028 [US3] Wire `CompostingBinService` right-click interactions (`AddWaste` and `CollectCompost`) in `LivingRoots/Controllers/ModController.cs` under `OnButtonPressed`
- [x] T029 [US3] Wire `CompostingBinService.ProcessDayStart` in `LivingRoots/Controllers/ModController.cs` under `OnDayStarted`
- [x] T030 [US3] Register Composting Bin crafting recipe (50 Wood, 25 Stone, 15 Fiber) in `LivingRoots/ModEntry.cs` via SMAPI `AssetRequested` event (`Data/CraftingRecipes`) per FR-018
- [x] T031 [US3] Register Composting Bin machine data and hover tooltip in `LivingRoots/ModEntry.cs` via SMAPI `AssetRequested` event (`Data/Machines`) per FR-019

**Checkpoint**: All user stories implemented.

---

## Phase 6: Polish, Assets & Localization (Wave 3)

**Purpose**: Cross-cutting resources, localization, and assets

- [x] T032 [P] Create i18n localization file `LivingRoots/i18n/default.json` and `LivingRoots/i18n/en.json` (compost name/description, bin name/description, maturity tooltip, restoration text per FR-021)
- [x] T033 [P] Add placeholder textures in `LivingRoots/Assets/` (`compost.png`, `composting_bin_empty.png`, `composting_bin_processing.png`, `composting_bin_ready.png`) and register via SMAPI `AssetRequested` per FR-019
- [x] T034 [P] Add placeholder audio cues in `LivingRoots/Assets/Audio/` (`composting_bin_processing.ogg`, `composting_bin_ready.ogg`) and register via SMAPI `AssetRequested` per FR-020

---

## Phase 7: Verification Gates (Wave 4)

**Purpose**: Strict non-negotiable verification gates. ONLY these verification commands attest completion, NEVER agent assertion.

- [x] T035 Run Gate 1 (Code Formatting): `dotnet format Stardew-LivingRoots.sln --verify-no-changes` — must produce zero changes
- [x] T036 Run Gate 2 (Build & Architecture): `dotnet build Stardew-LivingRoots.sln --configuration Release` — must compile with 0 errors and 0 warnings
- [x] T037 Run Gate 3 (TDD & Regressions): `dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal` — all unit and integration tests must pass (816 passed, 0 failed in GitHub Actions run 37137502126)
- [x] T038 Validate Quickstart End-to-End Scenarios: Validate all 12 scenarios in `specs/002-soil-decay-compost/quickstart.md` (decay rates, zero clamping, 100 ceiling, maturation scaling, save/load)

---

## Dependencies & Execution Order

### Wave Breakdown

1. **Wave 1 (Setup & TDD Contracts)**: T001–T015. Tasks marked `[P]` execute in parallel via specialized subagents.
2. **Wave 2 (Domain Services & Models)**: T016–T023. Can proceed as soon as Wave 1 test definitions are in place.
3. **Wave 3 (Application Services & Controllers)**: T024–T034. Integrates domain services into `ModController` and SMAPI events.
4. **Wave 4 (Verification Gates)**: T035–T038. Must run strictly at the end. Only these gates declare the feature complete.

### Parallel Opportunities

- In Wave 1: T002–T010 (interfaces and stubs) and T011–T015 (test files) run in parallel across subagents.
- In Wave 2: T016–T021 (models, entities, domain services) run in parallel.
- In Wave 3: T032–T034 (localization and assets) run in parallel with controller wiring.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Wave 1 Setup & TDD prerequisites (T001–T015).
2. Complete User Story 1 (T022–T023).
3. Validate decay mechanics via `SoilDecayServiceTests`.

### Incremental Delivery

1. Foundation + TDD ready (Wave 1).
2. Soil Decay active (Wave 2 / US1).
3. Compost Application active (Wave 3 / US2).
4. Composting Machine active (Wave 3 / US3).
5. Pass Verification Gates (Wave 4).
