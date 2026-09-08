# Specification: Composting State Machine Tests

**Feature**: Composting State Machine Tests
**Branch**: feat/002-04-composting-bin-service
**Author**: Sisyphus
**Date**: 2026-09-07

## Overview

The `feat/002-04-composting-bin-service` branch introduces a composting system with a state machine (Empty → Processing → Ready) and supporting services. Currently, these classes have **zero test coverage**, meaning the core feature of this branch is being built without a safety net.

This specification defines the testing requirements for the composting state machine and its supporting services to ensure correctness, prevent regressions, and enable future refactoring.

## User Stories

### As a developer, I want the composting state machine tested so that I can refactor with confidence

**Acceptance Criteria**:
- All state transitions (Empty → Processing → Ready → Empty) are covered by tests
- Invalid transitions (e.g., collecting from an empty bin, adding waste to a ready bin) are tested
- Edge cases (double-add, early collect, concurrent access) are covered

### As a developer, I want the supporting services tested so that I trust the composting logic

**Acceptance Criteria**:
- `CompostApplicationService` tests verify compost is correctly applied to soil
- `SoilDecayService` tests verify soil health decreases over time according to seasonal rules
- `OrganicWasteValidator` tests verify valid/invalid waste items are correctly categorized
- `SeasonalDecayMultiplier` tests verify seasonal modifiers are applied correctly
- `SaveIdProvider` tests verify save IDs are correctly retrieved

### As a QA engineer, I want the test suite to be meaningful so that it catches real bugs

**Acceptance Criteria**:
- Every test has at least one assertion
- Tests verify behavior, not implementation details
- Tests are independent and can run in any order
- Tests do not use reflection to access private state

## Functional Requirements

### FR-1: CompostingBinService State Machine Tests

**Priority**: Critical

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| FR-1.1 | Test initial state is Empty | New bin starts in Empty state |
| FR-1.2 | Test Empty → Processing transition | Adding valid waste moves bin to Processing state |
| FR-1.3 | Test Processing → Ready transition | After sufficient time passes, bin moves to Ready state |
| FR-1.4 | Test Ready → Empty transition | Collecting compost returns bin to Empty state |
| FR-1.5 | Test invalid: add waste to Processing bin | Adding waste while Processing is silently ignored; bin state remains Processing |
| FR-1.6 | Test invalid: add waste to Ready bin | Adding waste while Ready is silently ignored; bin state remains Ready |
| FR-1.7 | Test invalid: collect from Empty bin | Collecting from Empty returns 0 (no compost produced) |
| FR-1.8 | Test invalid: collect from Processing bin | Collecting from Processing returns 0 (no compost produced) |
| FR-1.9 | Test time progression logic | Bin correctly tracks elapsed days and transitions at threshold (2 full days via `ITimeProvider.TotalDays`) |
| FR-1.10 | Test full waste lifecycle | After collection resets bin to Empty, adding new waste transitions back to Processing (full cycle: add → process → ready → collect → add again) |
| FR-1.11 | Test maturation increment | After 7 consecutive active days, maturation level increases by 1 (capped at 5) |
| FR-1.12 | Test maturation reset | After 14 consecutive idle days (Empty state), maturation level resets to 1 |
| FR-1.13 | Test output count equals maturation level | Collecting compost produces exactly `MaturationLevel` compost items |

### FR-2: CompostApplicationService Tests

**Priority**: High

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| FR-2.1 | Test compost application to soil | Applying compost increases soil health by 15 (`ModConstants.RestorationAmount`) |
| FR-2.2 | Test application respects maximum health | Soil health does not exceed 100 (`ModConstants.MaxSoilHealth`) after application |
| FR-2.3 | Test application to invalid location | Applying compost to invalid location returns `false` |
| FR-2.4 | Test application with no compost available | `TryApplyCompost` returns `false` when player's held item is not compost (null or different item) |

### FR-3: SoilDecayService Tests

**Priority**: High

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| FR-3.1 | Test soil health decreases over time | Soil health decreases by 2.0 × seasonal multiplier per day (`ModConstants.DailyDecayRate = 2f`) |
| FR-3.2 | Test decay respects minimum health | Soil health does not go below minimum |
| FR-3.3 | Test decay stops at zero | Soil health does not become negative |
| FR-3.4 | Test seasonal decay variation | Decay rate changes based on current season (via `ISeasonProvider`) |

### FR-4: OrganicWasteValidator Tests

**Priority**: Medium

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| FR-4.1 | Test valid waste items accepted | Known valid items (e.g., sap, fiber) return true |
| FR-4.2 | Test invalid waste items rejected | Known invalid items (e.g., stone, wood) return false |
| FR-4.3 | Test null/empty input | Null or empty input returns false |

### FR-5: SeasonalDecayMultiplier Tests

**Priority**: Medium

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| FR-5.1 | Test spring multiplier | Spring returns 0.5x multiplier |
| FR-5.2 | Test summer multiplier | Summer returns 1.5x multiplier |
| FR-5.3 | Test fall multiplier | Fall returns 0.5x multiplier |
| FR-5.4 | Test winter multiplier | Winter returns 0.0x multiplier (no decay) |
| FR-5.5 | Test invalid season | Invalid season returns default 0.5x multiplier |

### FR-6: SaveIdProvider Tests

**Priority**: Medium

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| FR-6.1 | Test save ID retrieval | Returns valid save ID when available |
| FR-6.2 | Test null save ID handling | Returns null/empty when save folder unavailable |
| FR-6.3 | Test save ID consistency | Returns same ID across multiple calls in same session |

### FR-7: CompostingBinFactory Tests

**Priority**: Low

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| FR-7.1 | Test bin creation | Factory creates bin in Empty state |
| FR-7.2 | Test bin defaults | Factory creates bin with Empty state, MaturationLevel=1, and correct tile coordinates |

## Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-1 | All tests must execute in under 5 seconds |
| NFR-2 | Tests must not depend on external systems (file system, network) |
| NFR-3 | Tests must be deterministic — same input always produces same output |
| NFR-4 | Tests must not use reflection to access private state |
| NFR-5 | Tests must follow existing project conventions (xUnit, Moq, constructor injection) |

## Success Criteria

1. **Coverage**: All public methods on `CompostingBinService`, `CompostApplicationService`, `SoilDecayService`, `OrganicWasteValidator`, `SeasonalDecayMultiplier`, `SaveIdProvider`, and `CompostingBinFactory` have at least one test.
2. **State transitions**: All valid and invalid state transitions in the composting state machine are tested.
3. **Edge cases**: Boundary conditions (min/max health, empty inputs, null inputs) are covered.
4. **No theater**: Every test has at least one assertion; no tests exist solely to pad coverage numbers.
5. **Independence**: Tests can run in any order without shared mutable state.

## Clarifications

### Session 2026-09-07

- Q: How should time progression be handled in the composting state machine tests? → A: Use the existing `ProcessDayStart` method as the test driver, combined with setting `Game1.timeOfDay` to control time-based transitions. No new abstraction is needed; the code is already testable via game state. Research confirmed: `CompostingBinService` uses `Game1.timeOfDay` (line 52, 121) for timestamps and `ProcessDayStart()` (line 109) as the day-change driver. `SoilDecayService` follows the same pattern. No `ITimeProvider` or real-time dependencies exist.
- Q: `Game1` is a static Stardew Valley class — how should tests control `Game1.Date.TotalDays`, `Game1.timeOfDay`, `Game1.player`, and `Game1.currentSeason`? → A: Use the real `Game1` class directly in tests with a setup/teardown helper that snapshots and restores state between tests. This matches how SMAPI mods actually run (they always have a `Game1` instance) and avoids introducing untested abstraction layers.
- Q: `SoilDecayService` iterates `location.terrainFeatures.Pairs` and `CompostApplicationService` reads `location.terrainFeatures` and `Game1.player.CurrentItem` — how should tests provide these dependencies? → A: Create real `GameLocation` instances, add real `HoeDirt` tiles manually, use a real `Farmer` with inventory. This is the SMAPI mod testing standard (real objects, no mocking framework overhead) and keeps tests fast without requiring the full game engine.
- Q: FR-4.4 says "Validation is case-insensitive where applicable" but `OrganicWasteValidator` only uses `QualifiedItemId` (case-sensitive by design) and `Category` (numeric) — what should the case-insensitivity test verify? → A: Drop FR-4.4 — neither field is applicable. `QualifiedItemId` is case-sensitive by design, and `Category` is an integer. There is no string comparison in the validator where case-insensitivity would matter.
- Q: `SaveIdProvider` reads `Constants.SaveFolderName` from SMAPI — a static value unavailable outside the game. How should tests control this dependency? → A: Set `Constants.SaveFolderName` via reflection or a test-only setter, test the provider's validation logic (null, whitespace, length, consistency). This tests the provider's actual logic without mocking SMAPI internals.
- Q: Maturation level mechanics (increment after 7 active days, reset after 14 idle days, output count equals level) are only implicitly covered. How should these be tested? → A: Add explicit test cases for maturation: verify level increases after 7 consecutive active days, resets to 1 after 14 idle days, and output count equals current maturation level. These are deterministic, fast unit tests that directly verify the maturation logic.
- Q: The spec documents two critical bugs (cross-day elapsed time, `ProcessDayStart` never called). How should the test suite handle these known bugs? → A: Fix both bugs first, then write tests against correct behavior. Tests will verify correct behavior; the fix work is a prerequisite for tests to pass.
- Q: FR-1.10 states "Adding multiple waste items accumulates correctly," but `AddWaste()` ignores calls when bin is not Empty. What should the test verify? → A: Full lifecycle: add waste → process → ready → collect → add again. Tests verify the bin correctly resets to Empty after collection and accepts new waste, rather than testing simultaneous multiple additions to the same bin.
- Q: FR-7.2 states "Factory correctly initializes bin with provided data," but `CompostingBinFactory.CreateBin(int, int)` only accepts tile coordinates. What should the test verify? → A: Factory creates bin with correct defaults (Empty state, MaturationLevel=1) at specified tile coordinates. No "initial data" parameter exists; the factory only sets defaults.
- Q: FR-2.4 states "Applying with empty inventory returns error," but `TryApplyCompost` returns `bool` (false on failure), not an exception or error object. How should the test verify "no compost available"? → A: Test that `TryApplyCompost` returns `false` when player's held item is not compost (null or different item). The method returns bool false on rejection, not an exception or error object.
- Q: FR-1.9 requires a monotonic timestamp for cross-day transitions, but the spec doesn't specify the exact formula. What timestamp should the fix use? → A: `ITimeProvider.TotalDays` only — whole-day granularity. The bin records the day when waste was added, and `ProcessDayStart` compares current day against the recorded day. Replace `ProcessingDurationMinutes = 2880` with `MaturationDays = 2`. Tests control time via `ITimeProvider` stub and call `ProcessDayStart()`. (Superseded by Round 2: `ProcessingDurationMinutes` replaced, not kept.)
- Q: FR-1.5 and FR-1.6 state "Adding waste while Processing/Ready returns error/ignores," but `AddWaste()` returns `void` and silently ignores. What should the test verify? → A: Bin state remains unchanged after attempting to add waste to a non-Empty bin. No exception, no state transition, no log message — the method is a no-op for non-Empty bins.
- Q: `ConsecutiveActiveDays` is tracked at runtime but not persisted in `CompostingBinStateData`. Should this be a prerequisite fix? → A: Yes — add as Fix-3. `ConsecutiveIdleDays` IS persisted, proving this is an oversight. Without persistence, the 7-day maturation counter resets on save/load, making FR-1.11 unreachable in normal gameplay.
- Q: Fix-1 states InputTimestamp changes from `int?` to `int`. The actual current type is `long?`. What is the correct fix? → A: Change from `long?` to `int?` (not `int`). `Game1.Date.TotalDays` returns `int`, and the field must remain nullable because it is set to `null` when the bin is empty.
- Q: The plan claims "existing tests use real Game1 with snapshot/restore helper" but zero existing tests use Game1. How should this be described? → A: Describe as a "proposed pattern" with risk acknowledgment. Game1 is accessible but high-risk: `Game1.player` setter disposes the previous value, `Game1.graphics`/`Game1.smallFont` require an XNA graphics device unavailable in unit tests, and xUnit parallelization conflicts with Game1 static mutable state.
- Q: Research says `GameLocation(string name, string mapName)` but binary analysis shows the actual signature is `GameLocation(string mapPath, string name)`. What should test code use? → A: Update to correct signature `GameLocation(string mapPath, string name)` and use `new GameLocation("Maps\\Farm", "Farm")` in test patterns. The first parameter is the map file path, not the display name.
- Q: The spec describes `OrganicWasteValidator` as "whitelist approach" but the implementation uses three validation lists. What is the correct description? → A: Three-list approach: (1) Categories — accepted category IDs (-74, -75, -79, -80, -81) compatible with vanilla and modded items; (2) Include List — `compostable_item` context tag for mod developer opt-in; (3) Exclude List — `not_compostable` context tag for overriding category defaults.

### Session 2026-09-07, Round 2

- Q: The spec proposes using real `Game1` with a snapshot/restore helper for tests, but acknowledges this is a "proposed pattern" with risks (XNA graphics dependencies, player disposal semantics, parallelization conflicts). How should tests handle `Game1` dependencies that may be unavailable or unsafe in a unit test context? → A: Extract `Game1` dependencies behind thin interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) that services accept via constructor injection. Tests provide stub implementations; production wires to real `Game1`. This eliminates XNA/parallelization risks entirely and follows the project's existing DDD convention (interface in `Domain/Interfaces/`, impl in `Services/`).
- Q: FR-1.9 references `ProcessingDurationMinutes = 2880` (minutes) but the fix switches to whole-day granularity via `ITimeProvider.TotalDays`. Should the constant be replaced with `MaturationDays = 2`? → A: Replace `ProcessingDurationMinutes` with `MaturationDays = 2` — single source of truth, days-based comparison (`currentDay - recordedDay >= MaturationDays`). Eliminates confusion about which constant to use.
- Q: Fix-2 wires `CompostingBinService.ProcessDayStart` to `DayStarted`, but `SoilDecayService.ProcessDayStart` is equally unwired (zero `DayStarted` references in codebase). Should Fix-2 also wire `SoilDecayService`? → A: Extend Fix-2 to wire both `CompostingBinService.ProcessDayStart` AND `SoilDecayService.ProcessDayStart` to `DayStarted`. Both follow the same pattern, both are orphaned, and both are needed for the feature to work.
- Q: `CompostingBinController` exists but is never instantiated in `ModEntry`. Fix-2 says to "ensure `CompostingBinController` is instantiated" — should it be wired as a standalone controller or merged into `ModController`? → A: Merge `CompostingBinController` logic (right-click waste add / compost collect) into `ModController` as a new `OnButtonPressed` event handler. Delete the standalone `CompostingBinController` class. This preserves the single-controller architecture and avoids adding another disposable lifecycle.
- Q: `CompostingBinFactory` exists but is never used — `CompostingBinService.AddWaste` creates bins directly via `new CompostingBinStateModel`. Should the service use the factory? → A: Refactor `CompostingBinService.AddWaste` to use `CompostingBinFactory.CreateBin(tileX, tileY)`. The factory encapsulates default initialization (Empty state, MaturationLevel=1), making FR-7 meaningful (tests verify the factory that production actually uses).

## Assumptions

- The composting state machine follows the pattern: Empty → Processing → Ready → Empty
- `CompostingBinService` uses `Game1.Date.TotalDays` for time-based transitions from Processing → Ready (whole-day granularity)
- **PREREQUISITE FIX**: Cross-day elapsed time bug must be fixed before state transition tests can pass — use `ITimeProvider.TotalDays` (whole-day granularity) for a monotonic timestamp; `InputTimestamp` type changes from `long?` to `int?`; replace `ProcessingDurationMinutes` with `MaturationDays = 2`
- **PREREQUISITE FIX**: Both `CompostingBinService.ProcessDayStart()` and `SoilDecayService.ProcessDayStart()` must be wired to `ModController.DayStarted` event so the state machine and decay system advance; `CompostingBinController` logic merged into `ModController`
- **PREREQUISITE FIX**: `ConsecutiveActiveDays` must be persisted in `CompostingBinStateData` to prevent maturation progress loss on save/load
- After fixes, tests control time via `ITimeProvider` (set to desired `TotalDays`) and call `ProcessDayStart()` directly (whole-day granularity); production wires `ITimeProvider` to `Game1.Date.TotalDays`
- `SoilDecayService` applies daily decay based on season from `ISeasonProvider` (production wires to `Game1.currentSeason`)
- `OrganicWasteValidator` uses a three-list approach: (1) **Categories** — item category IDs (-74 seeds, -75 vegetables, -79 fruits, -80 flowers, -81 forage) that are accepted by default, compatible with both vanilla and modded items using the same categories; (2) **Include List** — mod developers can mark items as compostable via the `compostable_item` context tag; (3) **Exclude List** — items that match a category but should be rejected can be excluded via the `not_compostable` context tag override
- The project uses xUnit and Moq for testing (existing convention)
- **Terminology note**: `CompostingBinStateData` is the per-bin persistence DTO (class inside `CompostingBinData.cs` file). `CompostingBinData` (root class) holds the `LocationBinData` dictionary. Plan/tasks reference `CompostingBinData.cs` (file); spec references `CompostingBinStateData` (class). Both refer to the same file.
- Tests will be placed in `LivingRoots.Tests/` following the one-file-per-subject convention
- Tests using `GameLocation` will use the correct constructor signature `GameLocation(string mapPath, string name)` with valid map paths (e.g., `"Maps\\Farm"`)
- Game1 dependencies are extracted behind thin interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) to eliminate XNA graphics dependencies, player disposal risks, and parallelization conflicts; tests use stub implementations

## Prerequisite Fixes

These bugs must be fixed before the test suite can verify correct behavior. They are in scope for this feature.

### Fix-1: Cross-day elapsed time calculation

**Severity**: Critical

`CompostingBinService.cs:121` calculates elapsed time as:
```csharp
var elapsed = Game1.timeOfDay - bin.InputTimestamp.Value;
if (elapsed >= ModConstants.ProcessingDurationMinutes)
```

`Game1.timeOfDay` resets to 0 at the start of each day in Stardew Valley. If waste is added on day 1 (e.g., `timeOfDay = 1200`), then on day 2 `timeOfDay` resets to 0, making `elapsed = 0 - 1200 = -1200`. The `>= 2880` check would never trigger correctly across day boundaries. Additionally, the `InputTimestamp` field must change from `long?` (timeOfDay) to `int?` (TotalDays) for whole-day tracking, and `ProcessingDurationMinutes` must be replaced with `MaturationDays = 2` for whole-day comparison. `ITimeProvider.TotalDays` returns `int`, and the field must remain nullable because it is set to `null` when the bin is empty.

**Impact**: The Processing→Ready transition will never fire correctly, even after wiring `ProcessDayStart` to the game loop.

**Fix required**: Use `ITimeProvider.TotalDays` as the timestamp — whole-day granularity. Bin records the day when waste was added, and `ProcessDayStart` compares current day against recorded day. Replace `ProcessingDurationMinutes = 2880` with `MaturationDays = 2` for whole-day comparison (`currentDay - recordedDay >= MaturationDays`). Tests control time via `ITimeProvider` stub and call `ProcessDayStart()`.

### Fix-2: ProcessDayStart never called

**Severity**: High

`ProcessDayStart` is defined in both `ICompostingBinService` (implemented in `CompostingBinService`) and `ISoilDecayService` (implemented in `SoilDecayService`), but:
- `ModController.cs` has NO `DayStarted` event subscription
- `CompostingBinController.cs` exists but is orphaned — its right-click logic should be merged into `ModController` and the class deleted
- `ModEntry.cs` does NOT instantiate `CompostingBinController` (and should not — logic goes into `ModController`)

**Impact**: Neither the composting state machine nor soil decay ever advances; bins stay in their initial state forever and soil health never decreases.

**Fix required**: Wire both `CompostingBinService.ProcessDayStart` and `SoilDecayService.ProcessDayStart` to the `DayStarted` event in `ModController`. Merge `CompostingBinController` logic (right-click waste add / compost collect) into `ModController` as a new `OnButtonPressed` handler; delete the standalone `CompostingBinController` class.

### Fix-3: ConsecutiveActiveDays not persisted

**Severity**: High

`CompostingBinStateModel` tracks `ConsecutiveActiveDays` (days of continuous operation) at runtime to determine when maturation level should increment (every 7 active days). However, `CompostingBinStateData` (the persistence DTO) does NOT include this field. `ConsecutiveIdleDays` IS persisted, proving this is an oversight.

**Impact**: After save/load, the 7-day maturation counter resets to 0, making FR-1.11 (maturation increment after 7 consecutive active days) unreachable in normal gameplay. Tests would pass in-memory but fail to catch this latent bug.

**Fix required**: Add `ConsecutiveActiveDays` field to `CompostingBinStateData`, populate it in `SaveData`, and restore it in `LoadData`. This ensures maturation progress survives save/load cycles.

### Fix-4: Extract Game1 dependencies behind testable interfaces

**Severity**: High

`CompostingBinService`, `CompostApplicationService`, and `SoilDecayService` directly access `Game1` static properties (`Game1.timeOfDay`, `Game1.Date.TotalDays`, `Game1.currentSeason`, `Game1.player`). `Game1.graphics` and `Game1.smallFont` require an XNA graphics device unavailable in unit tests, `Game1.player` setter disposes the previous value, and xUnit parallelization conflicts with `Game1` static mutable state.

**Impact**: Tests cannot run without a full game engine, or will flake due to XNA/parallelization issues. The test suite would be unreliable on CI.

**Fix required**: Extract `Game1` dependencies behind thin interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) placed in `Domain/Interfaces/`. Services accept these via constructor injection. Production implementations wire to real `Game1`; tests use stub implementations. This follows the project's existing DDD convention (interface in Domain, impl in Services) and eliminates all XNA/parallelization risks.

### Fix-5: CompostingBinFactory not used in production

**Severity**: Medium

`CompostingBinFactory.CreateBin(int, int)` encapsulates default bin initialization (Empty state, MaturationLevel=1, tile coordinates), but `CompostingBinService.AddWaste` creates bins directly via `new CompostingBinStateModel { ... }` with the same defaults. The factory is dead code.

**Impact**: FR-7 (factory tests) would test a class that production never uses — pure coverage theater. The factory's defaults could diverge from the service's inline initialization without detection.

**Fix required**: Refactor `CompostingBinService.AddWaste` to accept `CompostingBinFactory` via constructor injection and call `_factory.CreateBin(tileX, tileY)` instead of inline `new CompostingBinStateModel`. This makes the factory the single creation point and ensures FR-7 tests verify production behavior.

## Out of Scope

- Integration tests with actual game save files
- UI/UX testing of composting bin visualization
- Performance testing of composting operations
- Testing of the existing `SoilHealthService` (already well-tested)

## Dependencies

- `CompostingBinService` must be testable in isolation (dependencies injectable via `ITimeProvider`, `IPlayerProvider`, `CompostingBinFactory`)
- `CompostApplicationService` must be testable in isolation (dependencies injectable via `IPlayerProvider`)
- `SoilDecayService` must be testable in isolation (dependencies injectable via `ISeasonProvider`)
- New interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) placed in `Domain/Interfaces/` with production implementations in `Services/`
- Test project must reference the main project (already configured via `InternalsVisibleTo`)
