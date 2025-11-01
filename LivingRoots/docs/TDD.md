# TDD Implementation for Living Roots Mod

## Overview
This document outlines the Test-Driven Development (TDD) approach for the Living Roots mod, following the principles established in our architecture and the red-green-refactor cycle.

## Core Principles
- Write tests before implementing functionality
- Follow the "Red-Green-Refactor" cycle
- Ensure high test coverage for business logic
- Maintain fast and reliable test execution

## TDD Cycle Applied

### 1. Red Phase: Write Failing Tests First
- Created `LivingRoots.Tests` project with xUnit
- Wrote tests for the `ModEntry`, `ModController`, and `ModDataService` classes before implementing functionality
- Ensured tests fail initially to validate test correctness

### 2. Green Phase: Implement Minimal Code to Pass Tests
- Refactored `ModEntry.cs` to follow dependency injection principles
- Created `ModController` to handle game events separately from the main entry point
- Implemented `ModDataService` for handling mod-specific data operations
- Made sure all tests pass with minimal implementation

### 3. Refactor Phase: Improve Code Quality While Maintaining Tests
- Applied SOLID principles (Separation of Concerns, Dependency Inversion)
- Implemented Domain-Driven Design patterns as outlined in ARCHITECTURE.md
- Ensured code follows DRY, KISS, and YAGNI principles
- Maintained test coverage during refactoring

## Project Structure for Tests

```
Stardew-LivingRoots.sln
├── LivingRoots/
│   ├── LivingRoots.csproj
│   ├── ModEntry.cs
│   ├── manifest.json
│   ├── bin/
│   ├── Controllers/
│   │   ├── ModController.cs
│   │   └── ...
│   ├── Domain/
│   │   ├── IModLogic.cs
│   │   └── ...
│   ├── Services/
│   │   ├── IModDataService.cs
│   │   ├── ModDataService.cs
│   │   └── ...
│   └── docs/
│       └── ...
└── LivingRoots.Tests/
    ├── LivingRoots.Tests.csproj
    ├── Usings.cs
    ├── ModControllerTests.cs
    ├── ModDataServiceTests.cs
    └── ...
```

## Testing Strategy

### 1. Unit Tests (Domain Logic)
- **Focus**: Pure business logic in the Domain layer
- **Examples**: 
  - SoilHealthLogic: Test degradation calculations, compost effects
  - CropRotationLogic: Test rotation benefits, compatibility checks
- **Approach**: Test each method in isolation with known inputs and expected outputs

### 2. Integration Tests
- **Focus**: Interaction between domain logic and services
- **Examples**:
  - Test soil health persistence and retrieval
  - Verify controller interactions with services

### 3. Mocking Strategy
- Use mocking frameworks (e.g., Moq) to isolate components
- Mock SMAPI interfaces and game objects for unit tests
- Create test doubles for IModHelper, IMonitor, IDataHelper, and other SMAPI services
- Removed unnecessary wrapper interfaces like IMonitorWrapper to reduce complexity

## Sample Test Implementation

### Example: Soil Health Logic Test
```csharp
[Fact]
public void ApplyDailyDegradation_WithHealthySoil_ReturnsExpectedValue()
{
    // Arrange
    var logic = new SoilHealthLogic();
    const int initialHealth = 100;
    const int expectedHealth = 99; // Assuming 1 point degradation per day

    // Act
    var result = logic.ApplyDailyDegradation(initialHealth);

    // Assert
    Assert.Equal(expectedHealth, result);
}
```

### Example: Controller Test
```csharp
// This is a conceptual example for a future controller that handles game-day events.
[Fact]
public void OnDayStarted_WhenEventFires_AppliesSoilHealthChanges()
{
    // Arrange
    var mockHelper = new Mock<IModHelper>();
    // Assume ISoilHealthService and SoilHealthLogic exist as per the architecture.
    var mockService = new Mock<ISoilHealthService>();
    var logic = new SoilHealthLogic();
    var controller = new GameEventsController(mockHelper.Object, mockService.Object, logic);
    
    // Act
    // Simulate the DayStarted event being fired by SMAPI.
    // controller.OnDayStarted(null, new DayStartedEventArgs());
    
    // Assert
    // Verify that the soil health logic was triggered via the service.
    mockService.Verify(s => s.UpdateAllSoilHealth(), Times.Once);
}
```

## Test Coverage

### Unit Tests
- `ModEntryTests`: Tests for the main mod entry point
- `ModControllerTests`: Tests for event handling logic
- `ModDataServiceTests`: Tests for data service functionality

### Testing Strategy
- Pure business logic is separated from SMAPI/game dependencies
- Mocking used for SMAPI interfaces to isolate units of code
- Event registration and multiple registration prevention tested
- Console command registration verified

## Continuous Integration
- Run tests on every commit via GitHub Actions
- Maintain test coverage metrics
- Fail builds if tests fail or coverage drops below threshold

## Test-First Workflow
1. Identify a small piece of functionality to implement
2. Write a failing test that describes the desired behavior
3. Implement the minimum code to make the test pass
4. Refactor for code quality while keeping tests passing
5. Repeat

## Naming Conventions
- Test classes: `[ClassName]Tests`
- Test methods: `[MethodUnderTest]_[Scenario]_[ExpectedResult]`
- Example: `SoilHealthLogicTests.ApplyDailyDegradation_WithHealthySoil_ReturnsExpectedValue`

## Performance Considerations
- Keep unit tests fast (under 10ms each)
- Separate slow integration tests if needed
- Use in-memory data stores for testing when possible

## Benefits Achieved
- Code is more modular and testable
- Clear separation of concerns
- Business logic is isolated from game-specific APIs
- Confidence in code changes through automated testing
- Easier maintenance and extension of functionality

## Running Tests

To run the tests:
```bash
dotnet test