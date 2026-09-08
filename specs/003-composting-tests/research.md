# Research: Composting State Machine Tests

**Date**: 2026-09-07
**Feature**: Composting State Machine Tests (`specs/003-composting-tests/`)

## Research Questions

### Q1: How to test `Game1` static class dependencies?

**Decision**: Extract `Game1` dependencies behind thin interfaces (`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`) placed in `Domain/Interfaces/`. Services accept these via constructor injection. Production implementations wire to real `Game1`; tests use stub implementations.

**Rationale**:
- `Game1.graphics` and `Game1.smallFont` require an XNA graphics device unavailable in unit tests
- `Game1.player` setter disposes the previous value
- xUnit parallelization conflicts with `Game1` static mutable state
- The project's existing DDD convention (interface in `Domain/Interfaces/`, impl in `Services/`) supports this approach
- Spec clarification Round 2 confirmed this approach
- Existing project tests (e.g., `ModControllerTests`) use Moq for interface dependencies — this extends the same pattern

**Alternatives Considered**:
- Real `Game1` with snapshot/restore helper — rejected: XNA graphics dependencies, player disposal semantics, parallelization conflicts
- `ITimeProvider` interface with mock implementation — evolved into full interface extraction (Fix-4)
- Isolation framework like `Pose` or `Smocks` — rejected: external dependency, overkill for this use case

---

### Q2: How to test time-dependent state transitions?

**Decision**: Set `ITimeProvider.TotalDays` via stub implementation and call `ProcessDayStart()` directly. The constant `MaturationDays = 2` replaces `ProcessingDurationMinutes = 2880` for whole-day comparison.

**Rationale**:
- `ITimeProvider.TotalDays` returns `int` — whole-day granularity matches the fix for the cross-day elapsed time bug
- Tests control time by setting the stub's `TotalDays` property and calling `ProcessDayStart()`
- Production wires `ITimeProvider` to `Game1.Date.TotalDays`
- `InputTimestamp` type changes from `long?` to `int?` (remains nullable because set to `null` when bin is empty)

**Alternatives Considered**:
- Using `Game1.timeOfDay` — rejected: resets to 0 each day, causes negative elapsed time
- Creating a custom time abstraction — rejected: the thin interface approach is simpler and follows existing DDD convention

---

### Q3: How to test `CompostingBinController` event wiring?

**Decision**: Merge `CompostingBinController` logic (right-click waste add / compost collect) into `ModController` as a new `OnButtonPressed` event handler. Delete the standalone `CompostingBinController` class. Wire both `CompostingBinService.ProcessDayStart` AND `SoilDecayService.ProcessDayStart` to `ModController.DayStarted`.

**Rationale**:
- `CompostingBinController` exists but is orphaned — not instantiated in `ModEntry`
- Single-controller architecture is the project pattern (`ModController` is the sole controller)
- Both services follow the same `ProcessDayStart` pattern and are equally orphaned
- `ThreadSafeGameLoopEventsStub` can raise `DayStarted` to test wiring

**Alternatives Considered**:
- Instantiating `CompostingBinController` separately — rejected: violates single-controller architecture
- Using real SMAPI events — rejected: requires full game context

---

### Q4: How to test `SoilDecayService` location iteration?

**Decision**: Create real `GameLocation` instances using the correct constructor signature `GameLocation(string mapPath, string name)` with `HoeDirt` tiles manually added.

**Rationale**:
- `SoilDecayService` iterates `location.terrainFeatures.Pairs`
- Real `GameLocation` and `HoeDirt` objects are available in the test context
- This is the SMAPI mod testing standard (real objects, no mocking framework overhead)
- Spec clarification confirmed the correct constructor signature: `GameLocation(string mapPath, string name)` — first parameter is the map file path, not the display name

**Alternatives Considered**:
- Mocking `GameLocation` — rejected: complex setup, fragile tests
- Using `Mock<GameLocation>` — rejected: `GameLocation` is not easily mockable

---

### Q5: How to test `SaveIdProvider` with `Constants.SaveFolderName`?

**Decision**: Set `Constants.SaveFolderName` via reflection or test-only setter. Test the provider's validation logic (null, whitespace, length, consistency).

**Rationale**:
- `Constants.SaveFolderName` is a public static field in SMAPI
- Reflection allows setting it in test setup
- Tests verify the provider's actual logic without mocking SMAPI internals

**Alternatives Considered**:
- Using `Mock<IReflection>` — rejected: SMAPI's `Constants` is not mockable
- Creating a wrapper interface — rejected: adds untested abstraction

---

### Q6: How to test maturation mechanics?

**Decision**: Add explicit test cases for maturation: verify level increases after 7 consecutive active days, resets to 1 after 14 idle days, and output count equals current maturation level. These are deterministic, fast unit tests that directly verify the maturation logic.

**Rationale**:
- Maturation mechanics are only implicitly covered in the spec
- `ConsecutiveActiveDays` must be persisted (Fix-3) for maturation to work across save/load
- Tests use `ITimeProvider` stub to advance days and verify maturation counter behavior

**Alternatives Considered**:
- Testing maturation through full lifecycle only — rejected: less precise, harder to debug failures

---

### Q7: How to handle `CompostingBinFactory` dead code?

**Decision**: Refactor `CompostingBinService.AddWaste` to accept `CompostingBinFactory` via constructor injection and call `_factory.CreateBin(tileX, tileY)` instead of inline `new CompostingBinStateModel`.

**Rationale**:
- The factory encapsulates default initialization (Empty state, MaturationLevel=1)
- FR-7 (factory tests) would test a class that production never uses — pure coverage theater
- Making the factory the single creation point prevents divergence between factory defaults and service inline initialization

**Alternatives Considered**:
- Deleting the factory — rejected: the factory provides value as a single creation point
- Testing the factory as-is — rejected: tests a class that production never uses

---

### Q8: What test patterns exist in the codebase?

**Decision**: Follow existing patterns from `ModControllerTests` and `ModDataServiceTests`, extended with stub implementations for the new interfaces.

**Rationale**:
- Constructor injection for test fixtures
- `Mock<T>` for interface dependencies
- `ThreadSafeGameLoopEventsStub` for async event testing
- `[Fact]` attributes for test methods
- `Assert` class for verification
- Stub implementations for `ITimeProvider`, `ISeasonProvider`, `IPlayerProvider`

**Key Patterns Identified**:
1. **Constructor setup**: All mocks created in constructor, stored as fields
2. **Test isolation**: Each test creates fresh service instances
3. **Mock verification**: `mock.Verify(x => x.Method(), Times.Once)` pattern
4. **Exception testing**: `Assert.Throws<T>(() => ...)` pattern
5. **Async testing**: `async Task` test methods with `await`
6. **Stub implementations**: Lightweight classes implementing interfaces for time/season/player control

---

## Technology Findings

### xUnit Patterns in This Project

```csharp
// Standard test class structure
public class ServiceTests
{
    private readonly Mock<IDependency> _mockDep;
    private readonly ServiceUnderTest _service;

    public ServiceTests()
    {
        _mockDep = new Mock<IDependency>();
        _service = new ServiceUnderTest(_mockDep.Object);
    }

    [Fact]
    public void Method_State_ExpectedResult()
    {
        // Arrange
        _mockDep.Setup(x => x.Method()).Returns(value);
        
        // Act
        var result = _service.Method();
        
        // Assert
        Assert.Equal(expected, result);
    }
}
```

### Stub Implementation Pattern (New)

```csharp
// Example: ITimeProvider stub for time-dependent tests
public class TimeProviderStub : ITimeProvider
{
    public int TotalDays { get; set; } = 1;
}

// Example: ISeasonProvider stub for season-dependent tests
public class SeasonProviderStub : ISeasonProvider
{
    public string CurrentSeason { get; set; } = "spring";
}

// Example: IPlayerProvider stub for player-dependent tests
public class PlayerProviderStub : IPlayerProvider
{
    public Item? CurrentItem { get; set; }
}
```

### ThreadSafeGameLoopEventsStub Usage

```csharp
// From ModControllerTests
var threadSafeGameLoopEvents = new ThreadSafeGameLoopEventsStub();
_mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
mockEvents.Setup(x => x.GameLoop).Returns(threadSafeGameLoopEvents);

// Raise events
mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());
```

---

## Dependency Analysis

### Services Under Test

| Service | Dependencies | Mockable? |
|---------|--------------|-----------|
| `CompostingBinService` | `IModDataService`, `ISaveIdProvider`, `IOrganicWasteValidator`, `IMonitor`, `ITimeProvider`, `CompostingBinFactory` | Yes — all interface dependencies |
| `CompostApplicationService` | `ISoilHealthService`, `IMonitor`, `IPlayerProvider` | Yes — all interface dependencies |
| `SoilDecayService` | `ISoilHealthService`, `IMonitor`, `ISeasonProvider`, `SeasonalDecayMultiplier` | Partial — `SeasonalDecayMultiplier` has no interface |
| `OrganicWasteValidator` | `IMonitor` | Yes |
| `SeasonalDecayMultiplier` | None | N/A — no dependencies |
| `SaveIdProvider` | None | N/A — reads `Constants.SaveFolderName` |
| `CompostingBinFactory` | None | N/A — creates `CompostingBinStateModel` |

### Key Insight

`SeasonalDecayMultiplier` has no dependencies and no interface — it's a pure function. Tests can call it directly without mocking.

`SaveIdProvider` has no injectable dependencies — it reads from `Constants.SaveFolderName` static. Tests must use reflection to control this value.

---

## Edge Cases Identified

1. **Null item in `AddWaste`**: `OrganicWasteValidator.IsValidOrganicWaste(null)` returns false
2. **Empty runtime cache**: `ProcessDayStart` on empty location is a no-op
3. **Collect from empty bin**: Returns 0, no state change
4. **Collect from processing bin**: Returns 0, no state change
5. **Add waste to processing bin**: Silently ignored, state unchanged
6. **Add waste to ready bin**: Silently ignored, state unchanged
7. **Maturation level cap**: Capped at `MaturationMaxLevel = 5`
8. **Maturation reset**: After 14 idle days, resets to 1
9. **Soil health bounds**: Clamped to [0, 100]
10. **Invalid season**: Returns default multiplier (0.5x)

---

## Open Questions (Resolved During Research)

| Question | Answer |
|----------|--------|
| Does `Game1.Date.TotalDays` have a public setter? | Yes — but tests no longer use it directly; they use `ITimeProvider` stub |
| Does `GameLocation` have a public constructor? | Yes — `GameLocation(string mapPath, string name)` (first param is map path, not display name) |
| Does `HoeDirt` have a public constructor? | Yes — `HoeDirt(int state, GameLocation location)` |
| Is `ThreadSafeGameLoopEventsStub` in the test project? | Yes — `LivingRoots.Tests/ThreadSafeGameLoopEventsStub.cs` |
| Does `Constants.SaveFolderName` have a public setter? | Yes — can be set via reflection |
| What is the correct `GameLocation` constructor signature? | `GameLocation(string mapPath, string name)` — confirmed via binary analysis |
| Should `CompostingBinController` be a standalone class? | No — merge into `ModController` and delete standalone class |
| Should both services be wired to `DayStarted`? | Yes — both `CompostingBinService` and `SoilDecayService` |

---

## Recommendations

1. **Create stub implementations** for `ITimeProvider`, `ISeasonProvider`, `IPlayerProvider` — lightweight classes with settable properties
2. **Use existing `ThreadSafeGameLoopEventsStub`** for event wiring tests
3. **Use reflection for `Constants.SaveFolderName`** in `SaveIdProviderTests`
4. **Follow existing test patterns** from `ModControllerTests` and `ModDataServiceTests`
5. **Use correct `GameLocation` constructor signature**: `new GameLocation("Maps\\Farm", "Farm")`
6. **Persist `ConsecutiveActiveDays`** in `CompostingBinData` before writing maturation tests
7. **Make `CompostingBinFactory`** the single creation point for `CompostingBinStateModel`
