# Implementation Guidelines: Thread-Safe Command Registration

## Overview

These guidelines provide step-by-step instructions for implementing the thread-safe command registration solution in ModController. The implementation ensures atomic operations prevent race conditions while maintaining all existing functionality.

## Prerequisites

Before implementing the thread-safe solution, ensure:

1. **Understanding of Current Implementation**: Familiarize yourself with the existing ModController code
2. **Testing Environment**: Have a working test suite to verify changes
3. **Backup**: Create a backup of the current implementation
4. **Dependencies**: Confirm all dependencies (SMAPI, .NET framework) are properly configured

## Implementation Steps

### Step 1: Atomic State Management Setup

#### 1.1 Verify State Management Structure

Ensure the following state management structure is in place:

```csharp
// State variable and flags should already exist
private int _state = 0;

private const int EventsRegisteredFlag = 0x01;
private const int CommandRegisteredFlag = 0x02;
private const int DisposedFlag = 0x04;
```

#### 1.2 Verify Atomic Operation Methods

Ensure the `TrySetStateOnce` method exists with the following implementation:

```csharp
/// <summary>
/// Attempts to set a specific state flag only once, ensuring thread safety.
/// This method uses atomic operations to ensure that the flag is only set once.
/// </summary>
/// <param name="flag">The flag to set</param>
/// <returns>True if the flag was set (meaning this was the first thread to set it), false otherwise</returns>
private bool TrySetStateOnce(int flag)
{
    int currentState, newState;
    bool wasSet = false;
    
    do
    {
        currentState = Volatile.Read(ref _state);
        
        // If flag is already set, return false
        if ((currentState & flag) != 0)
            return false;
        
        // If disposed, return false
        if ((currentState & DisposedFlag) != 0)
            return false;
        
        newState = currentState | flag;
        wasSet = Interlocked.CompareExchange(ref _state, newState, currentState) == currentState;
    }
    while (!wasSet);
    
    return wasSet;
}
```

### Step 2: Command Registration Implementation

#### 2.1 Update OnGameLaunched Method

Modify the `OnGameLaunched` method to use atomic operations for command registration:

```csharp
private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    // Early exit if disposed - single atomic check at the beginning using volatile read
    if (IsDisposed())
    {
        _monitor.Log("OnGameLaunched called after disposal. Operation skipped.", LogLevel.Trace);
        return;
    }

    // Create snapshots of dependencies to avoid errors if disposed mid-execution
    var monitor = _monitor;
    var helper = _helper;
    
    try
    {
        monitor.Log("The 'Living Roots' mod was loaded successfully!", LogLevel.Info);
        
        // Use TrySetStateOnce to ensure the command is only registered once
        bool wasCommandRegistered = TrySetStateOnce(CommandRegisteredFlag);
        
        // Only register the command if we successfully set the flag (meaning were the first thread to do so)
        if (wasCommandRegistered)
        {
            var commands = helper?.ConsoleCommands;
            if (commands != null)
            {
                commands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                monitor.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
            }
            else
            {
                // Command system unavailable - reset flag to allow retry
                Interlocked.And(ref _state, ~(CommandRegisteredFlag));
                monitor.Log("Command registration failed: ConsoleCommands is null.", LogLevel.Error);
            }
        else
        {
            monitor.Log("Command 'lr_version' already registered by another thread.", LogLevel.Trace);
        }
        
        // Use Interlocked.Exchange to safely get and clear the handler to avoid race condition
        var handler = Interlocked.Exchange(ref _onGameLaunchedHandler, null);
        if (handler != null && helper?.Events?.GameLoop != null)
        {
            helper.Events.GameLoop.GameLaunched -= handler;
            monitor.Log("GameLaunched event handler unsubscribed after first execution.", LogLevel.Trace);
        }
    }
    catch (Exception ex)
    {
        _monitor.Log($"Error in OnGameLaunched: {ex.Message}", LogLevel.Error);
    }
}
```

### Step 3: Error Handling and Recovery

#### 3.1 Implement Failure Recovery

Ensure that registration failures properly reset atomic flags:

```csharp
// In any method where registration might fail after atomic operation:
try
{
    // Perform the operation that might fail
    PerformRegistration();
}
catch (Exception ex)
{
    // Reset the flag to allow retry by another thread
    Interlocked.And(ref _state, ~flagThatWasSet);
    
    // Log the error without exposing internal details
    _monitor.Log($"Operation failed: {ex.Message}", LogLevel.Error);
    
    // Continue with appropriate error handling
}
```

#### 3.2 Verify Event Registration Error Handling

Ensure the `RegisterEvents` method properly handles failures:

```csharp
public void RegisterEvents()
{
    // Early exit if already disposed
    if (IsDisposed())
    {
        _monitor.Log("Attempted to register events after disposal. Operation skipped.", LogLevel.Trace);
        return;
    }
    
    // Use TrySetStateOnce to ensure events are only registered once
    if (!TrySetStateOnce(EventsRegisteredFlag))
    {
        _monitor.Log("Events are already registered, skipping registration.", LogLevel.Trace);
        return;
    }
    
    // Create snapshots of dependencies to avoid errors if disposed mid-execution
    var monitor = _monitor;
    var helper = _helper;
    
    try
    {
        var gameLoop = helper?.Events?.GameLoop;
        if (gameLoop == null)
        {
            monitor.Log("Helper or Events or GameLoop is null, cannot register events.", LogLevel.Error);
            // Reset flag since registration failed - ensure disposed flag is preserved
            Interlocked.And(ref _state, ~(EventsRegisteredFlag));
            return;
        }

        // Initialize the handler once
        _onGameLaunchedHandler ??= OnGameLaunched;
        
        // Subscribe to events
        gameLoop.GameLaunched += _onGameLaunchedHandler;
        
        monitor.Log("Events registered successfully.", LogLevel.Trace);
    }
    catch (Exception ex)
    {
        // Log error and reset the flag if registration failed - ensure disposed flag is preserved
        monitor.Log($"Error registering events: {ex.Message}", LogLevel.Error);
        _onGameLaunchedHandler = null;
        
        Interlocked.And(ref _state, ~(EventsRegisteredFlag));
    }
}
```

### Step 4: State Checking Methods

#### 4.1 Verify State Checking Implementation

Ensure all state checking methods use `Volatile.Read`:

```csharp
private bool IsDisposed()
{
    return (Volatile.Read(ref _state) & DisposedFlag) != 0;
}

private bool IsEventsRegistered()
{
    return (Volatile.Read(ref _state) & EventsRegisteredFlag) != 0;
}

private bool IsCommandRegistered()
{
    return (Volatile.Read(ref _state) & CommandRegisteredFlag) != 0;
}
```

### Step 5: Disposal Implementation

#### 5.1 Verify Thread-Safe Disposal

Ensure the `Dispose` method is thread-safe:

```csharp
public void Dispose()
{
    // Use TrySetStateOnce to ensure disposal flag is only set once
    if (!TrySetStateOnce(DisposedFlag))
    {
        _monitor.Log("Controller is already disposed.", LogLevel.Trace);
        return; // Already disposed
    }

    // Perform cleanup in a thread-safe manner
    PerformCleanup();
}
```

### Step 6: Testing Implementation

#### 6.1 Add Concurrency Tests

Add tests to verify the race condition is fixed:

```csharp
[Fact]
public async Task OnGameLaunched_CommandRegistration_IsThreadSafe()
{
    // Arrange
    var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
    
    var mockCommandHelper = new Mock<ICommandHelper>(MockBehavior.Strict);
    int registrationCount = 0;
    
    mockCommandHelper
        .Setup(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()))
        .Callback(() => Interlocked.Increment(ref registrationCount))
        .Verifiable();
    
    _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);
    
    var mockModDataService = new Mock<IModDataService>();
    var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
    
    // Act - Simulate concurrent calls to OnGameLaunched to test command registration race condition
    var tasks = new Task[10];
    for (int i = 0; i < 10; i++)
    {
        tasks[i] = Task.Run(() =>
        {
            var args = new GameLaunchedEventArgs();
            controller.GetType().GetMethod("OnGameLaunched", 
                BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(controller, new object[] { null, args });
        });
    }
    
    // Wait for all tasks to complete
    await Task.WhenAll(tasks);
    
    // Assert - Command should only be registered once despite multiple concurrent calls
    mockCommandHelper.Verify(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()), Times.Once);
    Assert.Equal(1, registrationCount); // Verify actual registration count
    _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
}
```

## Implementation Verification

### Step 7: Verification Checklist

Before deploying the implementation, verify:

#### 7.1 Atomic Operation Verification
- [ ] `TrySetStateOnce` method uses proper compare-and-swap pattern
- [ ] All state reads use `Volatile.Read`
- [ ] All state modifications use `Interlocked` operations
- [ ] Retry loops are properly implemented

#### 7.2 Thread Safety Verification
- [ ] Only one thread can successfully register the command
- [ ] No race conditions exist in state management
- [ ] Disposal is thread-safe
- [ ] Concurrent operations don't interfere with each other

#### 7.3 Error Handling Verification
- [ ] Registration failures properly reset atomic flags
- [ ] No partial states remain after failures
- [ ] Error logging doesn't expose internal details
- [ ] Recovery operations are thread-safe

#### 7.4 Functionality Verification
- [ ] All existing functionality remains intact
- [ ] Public interfaces unchanged
- [ ] Event registration behavior preserved
- [ ] Command registration behavior preserved
- [ ] Disposal behavior preserved

## Performance Considerations

### Step 8: Performance Validation

#### 8.1 Atomic Operation Performance
- Verify that atomic operations don't introduce significant overhead
- Ensure compare-and-swap retry loops don't cause excessive spinning
- Monitor for any performance degradation under high concurrency

#### 8.2 Memory Usage
- Confirm that memory usage remains minimal
- Verify no unnecessary object allocations during atomic operations
- Ensure state management doesn't increase memory footprint

## Common Implementation Pitfalls

### Step 9: Avoid These Mistakes

#### 9.1 Race Condition Pitfalls
- ❌ **Don't** directly modify the `_state` variable without atomic operations
- ❌ **Don't** use regular reads instead of `Volatile.Read` for state checking
- ❌ **Don't** forget to reset flags when operations fail after atomic reservation
- ❌ **Don't** use locks when atomic operations are sufficient

#### 9.2 Error Handling Pitfalls
- ❌ **Don't** forget to reset atomic flags when registration fails
- ❌ **Don't** expose stack traces in error logs
- ❌ **Don't** allow partial states to persist after failures
- ❌ **Don't** ignore exceptions in atomic operations

## Deployment Guidelines

### Step 10: Safe Deployment

#### 10.1 Pre-Deployment
- Run complete test suite including concurrency tests
- Verify all existing functionality tests pass
- Perform performance benchmarking
- Review code changes for any missed race conditions

#### 10.2 Post-Deployment Monitoring
- Monitor for any new error patterns
- Track command registration success rates
- Watch for performance regressions
- Verify thread safety in production environment

## Maintenance Guidelines

### Step 11: Ongoing Maintenance

#### 11.1 Code Changes
- Any changes to state management must use atomic operations
- New state flags must follow the same atomic patterns
- Never bypass the atomic operation system for state changes

#### 11.2 Testing Updates
- Add new concurrency tests when adding functionality
- Update existing tests if state management changes
- Maintain comprehensive test coverage for atomic operations

These implementation guidelines ensure that the thread-safe command registration solution is properly implemented, thoroughly tested, and maintains all existing functionality while eliminating the race condition.