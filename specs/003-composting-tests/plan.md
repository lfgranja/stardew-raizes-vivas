# Implementation Plan: Composting State Machine Tests

**Branch**: `feat/002-04-composting-bin-service` | **Date**: 2026-09-07 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-composting-tests/spec.md`

## Summary

This feature adds test coverage for the composting state machine and supporting services introduced in the `feat/002-04-composting-bin-service` branch. Currently, these classes have **zero test coverage**. The plan includes five prerequisite fixes (cross-day elapsed time bug, `ProcessDayStart` event wiring, `ConsecutiveActiveDays` persistence, Game1 interface extraction, factory usage refactor) and seven test files covering all functional requirements. Tests use thin interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) extracted from `Game1` static dependencies, with stub implementations for testing and production wiring to real `Game1`.

## Technical Context

**Language/Version**: .NET 6, C# latest language version

**Primary Dependencies**: SMAPI (Stardew Modding API), Stardew Valley game API

**Storage**: IModDataService for JSON save data (already used by SoilHealthService)

**Testing**: xUnit with Moq, `ThreadSafeGameLoopEventsStub` for async event testing, stub implementations of `ITimeProvider`/`ISeasonProvider`/`IPlayerProvider` for time/season/player-dependent tests

**Target Platform**: Stardew Valley via SMAPI (Windows/Linux/macOS, single-player)

**Project Type**: Stardew Valley mod (desktop game mod, DLL plugin)

**Performance Goals**: All tests execute in under 5 seconds (NFR-1)

**Constraints**:
- Tests must not depend on external systems (file system, network) — NFR-2
- Tests must be deterministic — NFR-3
- Tests must not use reflection to access private state — NFR-4
- Tests must follow existing project conventions (xUnit, Moq, constructor injection) — NFR-5
- Async-only concurrency (no `.Result` or `.Wait()`)
- All constants from `ModConstants` (no hardcoded values)

**Scale/Scope**: 7 test files, ~50+ test methods covering state machine transitions, supporting services, and edge cases

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-Driven Design | PASS | Test files mirror service structure. New interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) follow DDD convention: interface in `Domain/Interfaces/`, impl in Services. Ubiquitous language used. |
| II. Security-First Data Handling | PASS | No new file paths or user input. Tests use in-memory mocks and stub implementations. |
| III. Async-Only Concurrency | PASS | No blocking calls. `ThreadSafeGameLoopEventsStub` for async event testing. |
| IV. Test-Driven Development | PASS | Tests written before implementation. One test file per subject: `{Subject}Tests.cs`. |
| V. Simplicity and YAGNI | PASS | Uses existing test infrastructure (Moq, xUnit, ThreadSafeGameLoopEventsStub). New interfaces are thin wrappers around existing `Game1` access — no premature abstraction. |

**Gate Result**: PASS — No violations detected.

## Project Structure

### Documentation (this feature)

```text
specs/003-composting-tests/
├── plan.md              # This file ($speckit-plan command output)
├── research.md          # Phase 0 output ($speckit-plan command)
├── data-model.md        # Phase 1 output ($speckit-plan command)
├── quickstart.md        # Phase 1 output ($speckit-plan command)
├── contracts/           # Phase 1 output ($speckit-plan command)
└── tasks.md             # Phase 2 output ($speckit-tasks command - NOT created by $speckit-plan)
```

### Source Code (repository root)

```text
LivingRoots/
├── Controllers/
│   └── ModController.cs         # Fix-2: Add DayStarted event wiring for both services
├── Services/
│   ├── CompostingBinService.cs  # Fix-1: Cross-day elapsed time bug (uses ITimeProvider)
│   ├── CompostApplicationService.cs  # Fix-4: Accepts IPlayerProvider
│   ├── SoilDecayService.cs      # Fix-4: Accepts ISeasonProvider
│   ├── SaveIdProvider.cs
│   ├── CompostingBinFactory.cs  # Fix-5: Used by CompostingBinService.AddWaste
│   ├── TimeProvider.cs          # Fix-4: ITimeProvider impl wrapping Game1.Date.TotalDays
│   ├── SeasonProvider.cs        # Fix-4: ISeasonProvider impl wrapping Game1.currentSeason
│   └── PlayerProvider.cs        # Fix-4: IPlayerProvider impl wrapping Game1.player
├── Domain/
│   ├── Models/
│   │   ├── CompostingBinStateModel.cs
│   │   └── CompostingBinData.cs  # Fix-3: Add ConsecutiveActiveDays field
│   ├── Services/
│   │   ├── OrganicWasteValidator.cs
│   │   └── SeasonalDecayMultiplier.cs
│   └── Interfaces/
│       ├── ICompostingBinService.cs
│       ├── ICompostApplicationService.cs
│       ├── ISoilDecayService.cs
│       ├── IOrganicWasteValidator.cs
│       ├── ITimeProvider.cs      # Fix-4: New interface
│       ├── ISeasonProvider.cs    # Fix-4: New interface
│       └── IPlayerProvider.cs    # Fix-4: New interface
└── Constants.cs                 # ModConstants class (MaturationDays = 2 replaces ProcessingDurationMinutes)

LivingRoots.Tests/
├── CompostingBinServiceTests.cs         # FR-1: State machine tests
├── CompostApplicationServiceTests.cs    # FR-2: Compost application tests
├── SoilDecayServiceTests.cs             # FR-3: Soil decay tests
├── OrganicWasteValidatorTests.cs        # FR-4: Waste validation tests
├── SeasonalDecayMultiplierTests.cs      # FR-5: Seasonal multiplier tests
├── SaveIdProviderTests.cs               # FR-6: Save ID provider tests
├── CompostingBinFactoryTests.cs         # FR-7: Factory tests
└── ThreadSafeGameLoopEventsStub.cs      # Existing stub for async event testing
```

**Structure Decision**: Follows existing DDD layered architecture and one-file-per-subject convention. New interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) follow the project's established pattern (interface in `Domain/Interfaces/`, impl in `Services/`). Test files are placed in `LivingRoots.Tests/` root (matching existing pattern).

## Complexity Tracking

No constitution violations to track.

---

## Phase 0: Research & Unknowns

*See [research.md](./research.md) for detailed findings.*

### Research Outcomes

| Unknown | Resolution | Source |
|---------|------------|--------|
| How to test `Game1` static class dependencies | Extract behind thin interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`). Tests use stub implementations; production wires to real `Game1`. | Spec clarification Round 2 + existing DDD convention |
| How to test time-dependent transitions | Set `ITimeProvider.TotalDays` via stub and call `ProcessDayStart()` directly. | Spec clarification |
| How to test `CompostingBinController` wiring | Merge controller logic into `ModController` as `OnButtonPressed` handler. Delete standalone `CompostingBinController`. | Spec clarification Round 2 |
| How to test `SoilDecayService` location iteration | Create real `GameLocation` instances with `HoeDirt` tiles. Use correct constructor signature `GameLocation(string mapPath, string name)`. | Spec clarification |
| How to test `SaveIdProvider` with `Constants.SaveFolderName` | Set `Constants.SaveFolderName` via reflection or test-only setter. Test validation logic (null, whitespace, length, consistency). | Spec clarification |
| How to test maturation mechanics | Add explicit test cases: level increases after 7 active days, resets after 14 idle days, output count equals level. | Spec clarification |
| How to handle `CompostingBinFactory` dead code | Refactor `CompostingBinService.AddWaste` to use factory via constructor injection. | Spec clarification Round 2 |

## Phase 1: Design & Contracts

### Data Model

*See [data-model.md](./data-model.md) for full entity definitions, fields, relationships, validation rules, and state transitions.*

### Interface Contracts

*See [contracts/](./contracts/) for test fixture contracts and helper patterns.*

### Quickstart Validation

*See [quickstart.md](./quickstart.md) for runnable validation scenarios.*

---

## Phase 0-1 Research Summary

### Key Technical Findings

1. **Game1 Static Class Testing**: `Game1` dependencies are extracted behind thin interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) placed in `Domain/Interfaces/`. Services accept these via constructor injection. Production implementations wire to real `Game1`; tests use stub implementations. This follows the project's existing DDD convention and eliminates XNA graphics dependencies, player disposal risks, and xUnit parallelization conflicts.

2. **Time-Dependent Transitions**: Tests control time via `ITimeProvider` stub (set to desired `TotalDays`) and call `ProcessDayStart()` directly. The constant `MaturationDays = 2` replaces the old `ProcessingDurationMinutes = 2880` for whole-day comparison (`currentDay - recordedDay >= MaturationDays`).

3. **Event Wiring**: Both `CompostingBinService.ProcessDayStart` AND `SoilDecayService.ProcessDayStart` are wired to the `DayStarted` event in `ModController`. The standalone `CompostingBinController` is deleted; its right-click logic merges into `ModController` as a new `OnButtonPressed` handler.

4. **Location Iteration Testing**: `SoilDecayService` iterates `location.terrainFeatures.Pairs` — tests create real `GameLocation` instances using the correct constructor signature `GameLocation(string mapPath, string name)` with `HoeDirt` tiles manually added.

5. **SaveIdProvider Testing**: `Constants.SaveFolderName` is a static SMAPI value. Tests set it via reflection or test-only setter to test validation logic (null, whitespace, length, consistency).

6. **Factory Usage**: `CompostingBinFactory` becomes the single creation point for `CompostingBinStateModel` — `CompostingBinService.AddWaste` accepts the factory via constructor injection and calls `_factory.CreateBin(tileX, tileY)`.

### Constitution Re-Check (Post-Design)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-Driven Design | PASS | New interfaces follow DDD convention. Test files mirror service structure. |
| II. Security-First Data Handling | PASS | No new file paths. Tests use in-memory mocks and stub implementations. |
| III. Async-Only Concurrency | PASS | No blocking calls. ThreadSafeGameLoopEventsStub for async testing. |
| IV. Test-Driven Development | PASS | Test files planned for all services. One file per subject. |
| V. Simplicity and YAGNI | PASS | Thin interfaces around existing `Game1` access. No premature abstractions. |

**Re-evaluation Result**: PASS — Design aligns with all constitutional principles.

---

## Prerequisite Fixes

These bugs must be fixed before the test suite can verify correct behavior. They are in scope for this feature.

### Fix-1: Cross-day elapsed time calculation

**Severity**: Critical

**Location**: `CompostingBinService.cs:121`

**Current code**:
```csharp
var elapsed = Game1.timeOfDay - bin.InputTimestamp.Value;
if (elapsed >= ModConstants.ProcessingDurationMinutes)
```

**Problem**: `Game1.timeOfDay` resets to 0 at the start of each day. If waste is added on day 1 (e.g., `timeOfDay = 1200`), then on day 2 `timeOfDay` resets to 0, making `elapsed = 0 - 1200 = -1200`. The `>= 2880` check never triggers correctly across day boundaries. Additionally, `InputTimestamp` is `long?` (timeOfDay) but should be `int?` (TotalDays) for whole-day tracking.

**Fix required**: Use `ITimeProvider.TotalDays` as the timestamp — whole-day granularity. Bin records the day when waste was added, and `ProcessDayStart` compares current day against recorded day. Replace `ProcessingDurationMinutes = 2880` with `MaturationDays = 2` for whole-day comparison (`currentDay - recordedDay >= MaturationDays`). Change `InputTimestamp` type from `long?` to `int?` (remains nullable because set to `null` when bin is empty).

**Impact**: The Processing→Ready transition will never fire correctly without this fix.

### Fix-2: ProcessDayStart never called

**Severity**: High

**Problem**: `ProcessDayStart` is defined in both `ICompostingBinService` (implemented in `CompostingBinService`) and `ISoilDecayService` (implemented in `SoilDecayService`), but:
- `ModController.cs` has NO `DayStarted` event subscription
- `CompostingBinController.cs` exists but is orphaned — its right-click logic should be merged into `ModController` and the class deleted
- `ModEntry.cs` does NOT instantiate `CompostingBinController` (and should not — logic goes into `ModController`)

**Fix required**: Wire both `CompostingBinService.ProcessDayStart` AND `SoilDecayService.ProcessDayStart` to the `DayStarted` event in `ModController`. Merge `CompostingBinController` logic (right-click waste add / compost collect) into `ModController` as a new `OnButtonPressed` handler; delete the standalone `CompostingBinController` class.

**Impact**: Neither the composting state machine nor soil decay ever advances; bins stay in their initial state forever and soil health never decreases.

### Fix-3: ConsecutiveActiveDays not persisted

**Severity**: High

**Problem**: `CompostingBinStateModel` tracks `ConsecutiveActiveDays` (days of continuous operation) at runtime to determine when maturation level should increment (every 7 active days). However, `CompostingBinStateData` (the persistence DTO) does NOT include this field. `ConsecutiveIdleDays` IS persisted, proving this is an oversight.

**Impact**: After save/load, the 7-day maturation counter resets to 0, making FR-1.11 (maturation increment after 7 consecutive active days) unreachable in normal gameplay. Tests would pass in-memory but fail to catch this latent bug.

**Fix required**: Add `ConsecutiveActiveDays` field to `CompostingBinStateData`, populate it in `SaveData`, and restore it in `LoadData`. This ensures maturation progress survives save/load cycles.

### Fix-4: Extract Game1 dependencies behind testable interfaces

**Severity**: High

**Problem**: `CompostingBinService`, `CompostApplicationService`, and `SoilDecayService` directly access `Game1` static properties (`Game1.timeOfDay`, `Game1.Date.TotalDays`, `Game1.currentSeason`, `Game1.player`). `Game1.graphics` and `Game1.smallFont` require an XNA graphics device unavailable in unit tests, `Game1.player` setter disposes the previous value, and xUnit parallelization conflicts with `Game1` static mutable state.

**Impact**: Tests cannot run without a full game engine, or will flake due to XNA/parallelization issues. The test suite would be unreliable on CI.

**Fix required**: Extract `Game1` dependencies behind thin interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) placed in `Domain/Interfaces/`. Services accept these via constructor injection. Production implementations wire to real `Game1`; tests use stub implementations. This follows the project's existing DDD convention (interface in Domain, impl in Services) and eliminates all XNA/parallelization risks.

### Fix-5: CompostingBinFactory not used in production

**Severity**: Medium

**Problem**: `CompostingBinFactory.CreateBin(int, int)` encapsulates default bin initialization (Empty state, MaturationLevel=1, tile coordinates), but `CompostingBinService.AddWaste` creates bins directly via `new CompostingBinStateModel { ... }` with the same defaults. The factory is dead code.

**Impact**: FR-7 (factory tests) would test a class that production never uses — pure coverage theater. The factory's defaults could diverge from the service's inline initialization without detection.

**Fix required**: Refactor `CompostingBinService.AddWaste` to accept `CompostingBinFactory` via constructor injection and call `_factory.CreateBin(tileX, tileY)` instead of inline `new CompostingBinStateModel`. This makes the factory the single creation point and ensures FR-7 tests verify production behavior.

---

## Test Files Overview

| Test File | FR Coverage | Test Count | Key Patterns |
|-----------|-------------|------------|--------------|
| `CompostingBinServiceTests.cs` | FR-1.1 to FR-1.13 | ~15 | State machine transitions, maturation, time progression via `ITimeProvider` stub |
| `CompostApplicationServiceTests.cs` | FR-2.1 to FR-2.4 | ~6 | Real `GameLocation` with `HoeDirt` tiles, `IPlayerProvider` stub |
| `SoilDecayServiceTests.cs` | FR-3.1 to FR-3.4 | ~6 | Seasonal multipliers via `ISeasonProvider` stub, min/max bounds |
| `OrganicWasteValidatorTests.cs` | FR-4.1 to FR-4.3 | ~10 | Valid/invalid items, null input, context tags |
| `SeasonalDecayMultiplierTests.cs` | FR-5.1 to FR-5.5 | ~5 | All seasons + invalid season |
| `SaveIdProviderTests.cs` | FR-6.1 to FR-6.3 | ~6 | Reflection for `Constants.SaveFolderName` |
| `CompostingBinFactoryTests.cs` | FR-7.1 to FR-7.2 | ~3 | Default state, tile coordinates |

**Total**: ~51 test methods across 7 files

---

## Test Patterns

### Pattern 1: Service with Mock Dependencies

For services with injectable dependencies (`CompostApplicationService`, `SoilDecayService`, `SaveIdProvider`):

```csharp
// Example: CompostApplicationServiceTests
private readonly Mock<ISoilHealthService> _mockSoilHealthService;
private readonly Mock<IPlayerProvider> _mockPlayerProvider;
private readonly CompostApplicationService _service;

public CompostApplicationServiceTests()
{
    _mockSoilHealthService = new Mock<ISoilHealthService>();
    _mockPlayerProvider = new Mock<IPlayerProvider>();
    _service = new CompostApplicationService(_mockSoilHealthService.Object, _mockPlayerProvider.Object, _mockMonitor.Object);
}
```

### Pattern 2: Time-Dependent Tests via ITimeProvider Stub

For time-dependent tests (`CompostingBinServiceTests`):

```csharp
// Example: Time progression test
public class TimeProviderStub : ITimeProvider
{
    public int TotalDays { get; set; } = 1;
}

[Fact]
public void ProcessDayStart_AfterTwoDays_TransitionsToReady()
{
    // Arrange
    var timeStub = new TimeProviderStub { TotalDays = 1 };
    var service = new CompostingBinService(_mockModDataService.Object, _mockSaveIdProvider.Object, _mockWasteValidator.Object, _mockMonitor.Object, timeStub);
    service.AddWaste("Farm", new Vector2(10, 10), validWasteItem);
    
    // Act: Advance 2 days
    timeStub.TotalDays = 3;
    service.ProcessDayStart("Farm");
    
    // Assert
    Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", new Vector2(10, 10)));
}
```

### Pattern 3: Real GameLocation with HoeDirt

For location-iteration tests (`SoilDecayServiceTests`):

```csharp
// Example: Create real location with tilled soil
var location = new GameLocation("Maps\\Farm", "Farm");
var tile = new Vector2(10, 10);
var hoeDirt = new HoeDirt(0, location);
hoeDirt.crop = null; // Bare tile
location.terrainFeatures.Add(tile, hoeDirt);
```

### Pattern 4: ThreadSafeGameLoopEventsStub for Event Testing

For controller wiring tests:

```csharp
// Example: DayStarted event wiring
var stub = new ThreadSafeGameLoopEventsStub();
// ... setup controller with stub ...
stub.RaiseDayStarted(controller);
// Verify ProcessDayStart was called
```

### Pattern 5: Reflection for Static Values

For `SaveIdProviderTests`:

```csharp
// Example: Set Constants.SaveFolderName via reflection
var field = typeof(Constants).GetField("SaveFolderName", BindingFlags.Public | BindingFlags.Static);
field.SetValue(null, "test_save_id");
```

### Pattern 6: Season-Dependent Tests via ISeasonProvider Stub

For season-dependent tests (`SoilDecayServiceTests`):

```csharp
// Example: Seasonal decay test
public class SeasonProviderStub : ISeasonProvider
{
    public string CurrentSeason { get; set; } = "spring";
}

[Fact]
public void ProcessDayStart_SummerDecay_HigherThanSpring()
{
    var springStub = new SeasonProviderStub { CurrentSeason = "spring" };
    var summerStub = new SeasonProviderStub { CurrentSeason = "summer" };
    // ... test both and compare decay amounts
}
```

---

## Success Criteria Mapping

| Success Criterion | Test Coverage |
|-------------------|---------------|
| 1. Coverage: All public methods have at least one test | All 7 test files cover all public methods |
| 2. State transitions: All valid/invalid transitions tested | `CompostingBinServiceTests` covers FR-1.1 to FR-1.10 |
| 3. Edge cases: Boundary conditions covered | Null inputs, min/max health, empty collections |
| 4. No theater: Every test has at least one assertion | All test methods include Assert calls |
| 5. Independence: Tests run in any order | No shared mutable state; each test creates fresh fixtures |

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| `GameLocation` constructor requires game context | Medium | Use correct signature `GameLocation(string mapPath, string name)` with valid map paths |
| `Constants.SaveFolderName` is read-only | Low | Use reflection to set value in test setup |
| `ProcessDayStart` not wired to game loop | High | Fix-2 must be completed before tests can pass |
| Cross-day elapsed time bug | High | Fix-1 must be completed before state transition tests can pass |
| `ConsecutiveActiveDays` not persisted | High | Fix-3 must be completed before maturation tests are meaningful |
| XNA graphics dependencies in tests | High | Fix-4 eliminates all XNA/parallelization risks via interface extraction |
| Factory defaults diverge from service | Medium | Fix-5 makes factory the single creation point |

---

## Next Steps

1. Run `/speckit-tasks` to generate `tasks.md` with dependency-ordered task list
2. Execute prerequisite fixes (Fix-1 through Fix-5)
3. Create test files following patterns in this plan
4. Run test suite and verify all tests pass
