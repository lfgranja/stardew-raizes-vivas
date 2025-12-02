# Implementation Guidelines: Command Registration Race Condition Fix

## Overview

This document provides detailed implementation guidelines for fixing the race condition in command registration in `ModController.cs`. The guidelines ensure proper implementation of the atomic registration pattern with error recovery.

## Implementation Prerequisites

### 1. Required Knowledge
- Understanding of atomic operations and thread safety
- Familiarity with SMAPI (Stardew Modding API) console command system
- Knowledge of C# Interlocked operations
- Understanding of the existing ModController architecture

### 2. Development Environment
- .NET development environment
- Access to StardewModdingAPI references
- Unit testing framework (xUnit)

## Core Implementation Steps

### Step 1: Add the Atomic Registration Method

Implement the new `TryRegisterCommandAtomically` method in `ModController.cs`:

```csharp
private bool TryRegisterCommandAtomically(IModHelper helper, IMonitor monitor)
{
    // Use a loop to handle potential race conditions during the multi-step process
    int currentState, newState;
    bool registrationAttempted = false;
    bool commandSuccessfullyRegistered = false;

    do
    {
        currentState = Volatile.Read(ref _state);

        // If already disposed, exit immediately
        if ((currentState & DisposedFlag) != 0)
            return false;

        // If command is already registered, return false (no work needed)
        if ((currentState & CommandRegisteredFlag) != 0)
            return false;

        // Check if ConsoleCommands is available before proceeding
        var commands = helper?.ConsoleCommands;
        if (commands == null)
        {
            monitor?.Log("ConsoleCommands is not available for command registration.", LogLevel.Error);
            return false;
        }

        // Attempt to set the flag atomically while preserving other flags
        newState = currentState | CommandRegisteredFlag;
        
        // Attempt to set the state atomically
        registrationAttempted = Interlocked.CompareExchange(ref _state, newState, currentState) == currentState;
        
        if (registrationAttempted)
        {
            try
            {
                // Actually register the command
                commands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                monitor?.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
                commandSuccessfullyRegistered = true;
            }
            catch (Exception ex)
            {
                // CRITICAL: Recovery mechanism - reset flag to maintain consistency
                monitor?.Log($"Command registration failed after flag was set: {ex.Message}", LogLevel.Error);
                
                // Reset the command registered flag to maintain state consistency
                Interlocked.And(ref _state, ~CommandRegisteredFlag);
                
                // Return failure status
                return false;
            }
        }
    }
    while (!registrationAttempted);

    return commandSuccessfullyRegistered;
}
```

### Step 2: Update the OnGameLaunched Method

Replace the existing command registration logic in the `OnGameLaunched` method:

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

        // Use the new atomic registration method
        bool commandRegistered = TryRegisterCommandAtomically(helper, monitor);

        if (!commandRegistered)
        {
            monitor.Log("Command registration was not performed (already registered or unavailable).", LogLevel.Trace);
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

## Implementation Best Practices

### 1. Atomic Operation Patterns

#### A. Compare-And-Swap Loop
- Use a do-while loop with `Interlocked.CompareExchange` for multi-step atomic operations
- Always read the current state with `Volatile.Read` before each operation
- Continue looping until the operation succeeds

#### B. State Preservation
- When setting flags, use bitwise OR (`|`) to preserve other flags
- When clearing flags, use bitwise AND with negation (`& ~`) to preserve other flags
- Never clear the `DisposedFlag` accidentally

### 2. Error Recovery Patterns

#### A. Recovery on Exception
- Always reset the state flag if an operation fails after the flag has been set
- Use `Interlocked.And` with bitwise negation to atomically clear flags
- Log the recovery action for debugging purposes

#### B. Recovery on Prerequisites Failure
- Check all prerequisites before setting any flags
- Return false without setting flags if prerequisites are not met
- Log appropriate error messages for different failure scenarios

### 3. Thread Safety Considerations

#### A. Dependency Snapshots
- Create local snapshots of dependencies (`helper`, `monitor`) to prevent null reference exceptions
- Use these snapshots throughout the method to avoid accessing disposed objects

#### B. Volatile Reads
- Always use `Volatile.Read` when checking state flags
- This ensures you get the most current value across all threads

## Code Quality Standards

### 1. Naming Conventions
- Use descriptive method names: `TryRegisterCommandAtomically`
- Follow C# naming conventions (PascalCase for methods, camelCase for parameters)
- Use clear variable names that indicate their purpose

### 2. Documentation Standards
- Add XML documentation comments for all new public and private methods
- Document the atomic nature and thread safety of operations
- Include remarks about error recovery behavior

### 3. Logging Standards
- Use appropriate log levels (Trace for detailed info, Error for problems)
- Include descriptive messages that help with debugging
- Follow existing logging patterns in the codebase

## Testing Implementation Guidelines

### 1. Unit Test Structure
- Test the atomic registration method in isolation
- Verify all code paths including success and failure scenarios
- Test concurrent execution scenarios

### 2. Mock Configuration
- Mock `ICommandHelper` to simulate different ConsoleCommands states
- Test with null ConsoleCommands to verify recovery
- Simulate exceptions during command registration

### 3. Assertion Strategy
- Verify that state flags match actual registration status
- Confirm that error recovery properly resets flags
- Validate that only one registration occurs in concurrent scenarios

## Performance Considerations

### 1. Atomic Operation Efficiency
- Compare-and-swap loops are efficient under low to moderate contention
- No blocking or locking mechanisms are used
- The operation complexity remains constant

### 2. Memory Usage
- No additional memory allocation beyond existing patterns
- Uses the same bit flag system as existing code
- Minimal overhead for atomic operations

## Integration Considerations

### 1. Existing Architecture Compatibility
- Maintain compatibility with existing state management patterns
- Preserve existing disposal and cleanup behavior
- Keep the same logging and monitoring integration

### 2. External API Integration
- Continue to use SMAPI's `ConsoleCommands.Add` method
- Maintain the same command name, description, and handler
- Preserve all existing event handling behavior

## Error Handling Implementation

### 1. Exception Safety
- Wrap command registration in try-catch to handle potential exceptions
- Ensure recovery mechanism executes even if registration throws
- Log exceptions appropriately without exposing internal details

### 2. Null Reference Prevention
- Check for null dependencies before use
- Handle cases where SMAPI services become unavailable
- Provide graceful degradation when services are not available

## Verification Implementation

### 1. State Consistency Checks
- Verify that the flag accurately reflects registration status
- Confirm recovery mechanism works properly
- Test all failure scenarios thoroughly

### 2. Concurrency Verification
- Run tests with multiple concurrent threads
- Verify atomic behavior under high contention
- Test boundary conditions with rapid state changes

## Deployment Guidelines

### 1. Backward Compatibility
- Maintain all existing public interfaces
- Preserve existing behavior for all success scenarios
- Ensure no breaking changes to external dependencies

### 2. Forward Compatibility
- Design for potential future command registration needs
- Keep the atomic registration method generalizable
- Maintain clear separation of concerns

## Code Review Checklist

Before merging the implementation, verify:

- [ ] Atomic registration method properly implements the compare-and-swap pattern
- [ ] Recovery mechanism correctly resets flags on failure
- [ ] All existing functionality remains intact
- [ ] Thread safety is maintained in all scenarios
- [ ] Error handling and logging follow established patterns
- [ ] Unit tests cover all code paths and scenarios
- [ ] Performance characteristics are maintained
- [ ] Code follows established naming and documentation conventions

## Common Implementation Pitfalls to Avoid

### 1. Incorrect Flag Management
- Don't clear the `DisposedFlag` during recovery
- Always preserve other state flags when modifying the state
- Use proper bitwise operations to avoid unintended side effects

### 2. Race Condition Creation
- Don't set flags before checking prerequisites
- Always use atomic operations for state changes
- Don't assume state won't change between read and write operations

### 3. Incomplete Error Recovery
- Always reset flags when operations fail after setting them
- Don't leave the system in an inconsistent state
- Log recovery actions for debugging purposes

Following these implementation guidelines will ensure a robust, thread-safe solution that properly fixes the race condition while maintaining all existing functionality.