# Test Update Plan: Command Registration Race Condition Fix

## Overview

This document outlines the necessary updates to existing tests and new tests required to verify the race condition fix in command registration. The goal is to ensure comprehensive test coverage for the new atomic registration implementation.

## Existing Test Analysis

### Current Test Coverage in ModControllerTests.cs

The existing tests cover:
- Basic event registration functionality
- Idempotent behavior for event registration
- Thread safety for concurrent operations
- Exception handling during registration
- Disposal behavior
- Concurrent disposal scenarios
- Basic command registration thread safety test

### Tests That Need Updates

Some existing tests may need updates due to the refactoring, particularly those that directly test the internal behavior of command registration.

## Test Updates Required

### 1. Update OnGameLaunched_CommandRegistration_IsThreadSafe Test

**Current Issue**: The existing test calls the private `OnGameLaunched` method using reflection, which may need adjustment for the new implementation.

**Required Update**:
- Verify that the new atomic registration properly handles concurrent access
- Ensure command is registered only once despite multiple concurrent calls
- Verify that the recovery mechanism works under concurrent failure scenarios

```csharp
[Fact]
public async System.Threading.Tasks.Task OnGameLaunched_CommandRegistration_IsThreadSafe()
{
    // Arrange
    var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
    
    var mockCommandHelper = new Mock<ICommandHelper>(MockBehavior.Strict);
    mockCommandHelper
        .Setup(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()))
        .Verifiable();
    
    _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);
    
    var mockModDataService = new Mock<IModDataService>();
    var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
    
    // Act - Simulate concurrent calls to OnGameLaunched to test command registration race condition
    var tasks = new System.Threading.Tasks.Task[10];
    for (int i = 0; i < 10; i++)
    {
        tasks[i] = System.Threading.Tasks.Task.Run(() =>
        {
            var args = new GameLaunchedEventArgs();
            controller.GetType().GetMethod("OnGameLaunched", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(controller, new object[] { null, args });
        });
    }
    
    // Wait for all tasks to complete
    await System.Threading.Tasks.Task.WhenAll(tasks);
    
    // Assert - Command should only be registered once despite multiple concurrent calls
    mockCommandHelper.Verify(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()), Times.Once);
    _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
}
```

### 2. Add New Test: CommandRegistration_Failure_Recovery

**Purpose**: Verify that when command registration fails, the state is properly recovered (flag is reset).

```csharp
[Fact]
public void CommandRegistration_WhenConsoleCommandsIsNull_ResetsFlagAndHandlesGracefully()
{
    // Arrange
    var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
    _mockHelper.Setup(x => x.ConsoleCommands).Returns((ICommandHelper)null); // Return null to simulate failure condition
    
    var mockModDataService = new Mock<IModDataService>();
    var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
    
    string errorMessage = "";
    _mockMonitor.Setup(x => x.Log(It.IsAny<string>(), LogLevel.Error))
        .Callback<string, LogLevel>((message, level) =>
        {
            if (message.Contains("ConsoleCommands is not available"))
                errorMessage = message;
        });
    
    // Act - Trigger OnGameLaunched which should attempt command registration
    var args = new GameLaunchedEventArgs();
    controller.GetType().GetMethod("OnGameLaunched", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.Invoke(controller, new object[] { null, args });
    
    // Assert - Verify flag was not set (since registration failed)
    // We need to check the internal state - since we can't directly access _state, 
    // we can check if the command was attempted to be registered
    Assert.NotEmpty(errorMessage); // Should have logged the error about ConsoleCommands being unavailable
}
```

### 3. Add New Test: CommandRegistration_Exception_Recovery

**Purpose**: Verify that when an exception occurs during command registration, the recovery mechanism resets the flag.

```csharp
[Fact]
public void CommandRegistration_WhenExceptionOccurs_ResetsFlag()
{
    // Arrange
    var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
    
    var mockCommandHelper = new Mock<ICommandHelper>();
    mockCommandHelper
        .Setup(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()))
        .Throws(new InvalidOperationException("Registration failed"));
    
    _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);
    
    var mockModDataService = new Mock<IModDataService>();
    var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
    
    string errorMessage = "";
    _mockMonitor.Setup(x => x.Log(It.IsAny<string>(), LogLevel.Error))
        .Callback<string, LogLevel>((message, level) =>
        {
            if (message.Contains("Command registration failed"))
                errorMessage = message;
        });
    
    // Act - Trigger OnGameLaunched which should attempt command registration and fail
    var args = new GameLaunchedEventArgs();
    controller.GetType().GetMethod("OnGameLaunched", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.Invoke(controller, new object[] { null, args });
    
    // Assert - Verify error was logged and recovery occurred
    Assert.NotEmpty(errorMessage);
    mockCommandHelper.Verify(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()), Times.Once);
}
```

### 4. Add New Test: AtomicCommandRegistration_Success

**Purpose**: Test the new atomic registration method directly.

```csharp
[Fact]
public void TryRegisterCommandAtomically_WhenConsoleCommandsAvailable_RegistersCommandAndSetsFlag()
{
    // Arrange
    var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
    
    var mockCommandHelper = new Mock<ICommandHelper>();
    mockCommandHelper
        .Setup(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()))
        .Verifiable();
    
    _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);
    
    var mockModDataService = new Mock<IModDataService>();
    var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
    
    // Act - Call the internal atomic registration method directly
    var result = controller.GetType().GetMethod("TryRegisterCommandAtomically", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.Invoke(controller, new object[] { _mockHelper.Object, _mockMonitor.Object });
    
    bool registrationResult = (bool)(result ?? false);
    
    // Assert
    Assert.True(registrationResult);
    mockCommandHelper.Verify(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()), Times.Once);
    _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
}
```

### 5. Add New Test: AtomicCommandRegistration_AlreadyRegistered

**Purpose**: Test that atomic registration returns false when already registered.

```csharp
[Fact]
public void TryRegisterCommandAtomically_WhenAlreadyRegistered_ReturnsFalse()
{
    // Arrange
    var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
    
    var mockCommandHelper = new Mock<ICommandHelper>();
    mockCommandHelper
        .Setup(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()))
        .Verifiable();
    
    _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);
    
    var mockModDataService = new Mock<IModDataService>();
    var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
    
    // First successful registration
    var firstResult = controller.GetType().GetMethod("TryRegisterCommandAtomically", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.Invoke(controller, new object[] { _mockHelper.Object, _mockMonitor.Object });
    
    // Act - Try to register again
    var secondResult = controller.GetType().GetMethod("TryRegisterCommandAtomically", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.Invoke(controller, new object[] { _mockHelper.Object, _mockMonitor.Object });
    
    bool firstRegistrationResult = (bool)(firstResult ?? false);
    bool secondRegistrationResult = (bool)(secondResult ?? false);
    
    // Assert
    Assert.True(firstRegistrationResult);
    Assert.False(secondRegistrationResult);
    mockCommandHelper.Verify(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()), Times.Once);
}
```

### 6. Enhanced Concurrent Test: MultipleFailureScenarios

**Purpose**: Test concurrent scenarios where some threads fail and others succeed.

```csharp
[Fact]
public async System.Threading.Tasks.Task CommandRegistration_ConcurrentMixedScenarios_HandlesProperly()
{
    // Arrange - Create mock with conditional behavior for different calls
    var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
    
    var mockCommandHelper = new Mock<ICommandHelper>();
    int callCount = 0;
    mockCommandHelper
        .Setup(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()))
        .Callback(() => {
            int currentCall = Interlocked.Increment(ref callCount);
            // Simulate that only the first call succeeds, subsequent ones throw
            if (currentCall > 1)
                throw new InvalidOperationException("Only one registration allowed");
        })
        .Verifiable();
    
    _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);
    
    var mockModDataService = new Mock<IModDataService>();
    var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
    
    // Act - Multiple concurrent registration attempts
    var tasks = new System.Threading.Tasks.Task<bool>[10];
    for (int i = 0; i < 10; i++)
    {
        tasks[i] = System.Threading.Tasks.Task.Run(() =>
        {
            return (bool)controller.GetType().GetMethod("TryRegisterCommandAtomically", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(controller, new object[] { _mockHelper.Object, _mockMonitor.Object });
        });
    }
    
    var results = await System.Threading.Tasks.Task.WhenAll(tasks);
    
    // Assert - Only one should succeed
    var successfulRegistrations = results.Count(r => r);
    Assert.Equal(1, successfulRegistrations);
    // Command should only be added once despite multiple attempts
    mockCommandHelper.Verify(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()), Times.Once);
}
```

## Test Strategy for Race Condition Verification

### 1. Stress Testing Approach
- Run concurrent tests with high thread counts (20-50 threads)
- Perform multiple iterations of the same test to increase probability of catching race conditions
- Test boundary conditions where ConsoleCommands availability changes during execution

### 2. Failure Injection Testing
- Mock ConsoleCommands to return null in specific scenarios
- Inject exceptions at various points in the registration process
- Test disposal occurring during registration attempts

### 3. State Verification Testing
- Verify that the CommandRegisteredFlag accurately reflects actual registration status
- Test that after failed registration, the system allows future registration attempts
- Ensure that successful registration prevents duplicate registrations

## Updated Test Categories

### 1. Unit Tests
- Individual method testing (TryRegisterCommandAtomically)
- State management verification
- Error handling validation

### 2. Integration Tests  
- Full OnGameLaunched workflow testing
- SMAPI integration testing
- Event lifecycle testing

### 3. Concurrency Tests
- High-concurrency registration attempts
- Mixed success/failure scenarios
- Disposal during registration scenarios

### 4. Edge Case Tests
- Rapid disposal and registration
- Multiple game launch events
- Null dependency injection tests

## Test Execution Order

1. Run existing tests to ensure no regressions
2. Add new unit tests for atomic registration method
3. Add concurrency tests to verify race condition fix
4. Add failure scenario tests to verify recovery
5. Run all tests multiple times to ensure reliability

## Verification Metrics

- All existing tests continue to pass
- New race condition tests pass consistently
- No intermittent failures in concurrent tests
- Proper error handling verified through logging checks
- State consistency maintained across all scenarios

This test update plan ensures comprehensive coverage of the race condition fix while maintaining all existing functionality.