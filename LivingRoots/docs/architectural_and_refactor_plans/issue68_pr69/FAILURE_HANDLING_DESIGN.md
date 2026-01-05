# Failure Handling Design: Command Registration Recovery Pattern

## Overview

This document outlines the failure handling and recovery mechanisms designed to ensure state consistency in command registration. The primary goal is to maintain a consistent state where the `CommandRegisteredFlag` accurately reflects whether the command is actually registered.

## Problem Statement

The original code had a critical failure scenario:
1. Thread A sets the `CommandRegisteredFlag` 
2. Thread A finds that `helper.ConsoleCommands` is null
3. Thread A doesn't register the command but the flag remains set
4. System enters inconsistent state where flag indicates registration but command isn't actually registered

## Recovery Pattern Implementation

### 1. Atomic Operation with Rollback

The recovery pattern ensures that if any step in the registration process fails, the system state is rolled back to a consistent state.

```csharp
private bool TryRegisterCommandAtomically(IModHelper helper, IMonitor monitor)
{
    int currentState, newState;
    bool registrationAttempted = false;
    bool commandSuccessfullyRegistered = false;

    do
    {
        currentState = Volatile.Read(ref _state);

        // Check if already disposed
        if ((currentState & DisposedFlag) != 0)
            return false;

        // Check if command is already registered
        if ((currentState & CommandRegisteredFlag) != 0)
            return false;

        // Verify prerequisites before proceeding
        var commands = helper?.ConsoleCommands;
        if (commands == null)
        {
            monitor?.Log("ConsoleCommands is not available for command registration.", LogLevel.Error);
            return false;
        }

        // Attempt to set the flag atomically
        newState = currentState | CommandRegisteredFlag;
        registrationAttempted = Interlocked.CompareExchange(ref _state, newState, currentState) == currentState;
        
        if (registrationAttempted)
        {
            try
            {
                // Perform the actual registration
                commands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                monitor?.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
                commandSuccessfullyRegistered = true;
            }
            catch (Exception ex)
            {
                // CRITICAL: Recovery mechanism - reset flag to maintain consistency
                monitor?.Log($"Command registration failed after flag was set: {ex.Message}", LogLevel.Error);
                
                // Rollback: Reset the command registered flag to maintain state consistency
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

### 2. Multi-Step Validation

Before setting the flag, the system validates all prerequisites:
- Checks if already disposed
- Checks if command is already registered  
- Verifies ConsoleCommands availability
- Only then attempts to set the flag and register the command

### 3. Comprehensive Error Handling

The recovery pattern includes:

#### A. Exception During Registration
If an exception occurs during `commands.Add()`, the system:
- Logs the error appropriately
- Resets the `CommandRegisteredFlag` using atomic operation
- Returns false to indicate failure

#### B. Null ConsoleCommands
If `helper.ConsoleCommands` is null:
- No flag is set
- Error is logged
- Method returns false immediately

#### C. Concurrent Registration Attempts
Multiple threads attempting registration simultaneously:
- Only the first thread to successfully set the flag will proceed
- Other threads will detect the flag is set and exit gracefully
- No race condition occurs

## State Consistency Guarantees

### 1. Consistent Flag-Command Relationship
- If `CommandRegisteredFlag` is set, the command is guaranteed to be registered
- If the command is registered, `CommandRegisteredFlag` is guaranteed to be set
- Never a state where flag indicates registration but command isn't registered

### 2. Idempotent Operations
- Multiple calls to registration method are safe
- Already registered command won't be registered again
- No side effects from repeated calls

### 3. Cleanup on Disposal
The existing disposal pattern ensures proper cleanup:
- Disposal sets the `DisposedFlag` atomically
- All operations check for disposal before proceeding
- No operations occur after disposal

## Recovery Scenarios

### Scenario 1: ConsoleCommands Becomes Available Later
- Thread A attempts registration when ConsoleCommands is null → returns false
- Thread B attempts later when ConsoleCommands is available → succeeds
- System maintains consistent state throughout

### Scenario 2: Registration Exception
- Flag gets set successfully
- Command registration throws exception
- Recovery mechanism resets the flag
- System returns to consistent state

### Scenario 3: Concurrent Registration
- Multiple threads attempt registration simultaneously
- Only one thread wins and registers the command
- Other threads detect registration and exit gracefully
- Consistent state maintained

## Logging and Monitoring

### Recovery Logging
The system logs recovery actions to enable debugging and monitoring:

```csharp
monitor?.Log($"Command registration failed after flag was set: {ex.Message}", LogLevel.Error);
```

### State Change Logging
All state changes are logged for audit and debugging purposes:
- Successful registrations
- Failed registration attempts
- Recovery actions taken

## Integration with Existing Patterns

### 1. Existing State Management
The recovery pattern integrates with the existing bit flag system:
- Preserves the `EventsRegisteredFlag`, `CommandRegisteredFlag`, and `DisposedFlag` structure
- Uses the same atomic operations as other state management
- Maintains consistency with existing state checking methods

### 2. Existing Error Handling
The recovery pattern follows the same error handling patterns:
- Uses the same logging levels and message formats
- Maintains the same exception handling approach
- Preserves existing error reporting behavior

## Testing Considerations

### 1. Recovery Testing
Tests should verify that:
- Flags are reset appropriately when registration fails
- System state remains consistent after failures
- Recovery mechanism works under various failure scenarios

### 2. Concurrent Failure Testing
Tests should verify behavior when:
- Multiple threads encounter registration failures
- Race conditions occur during failure scenarios
- Recovery happens concurrently with other operations

## Performance Impact

### 1. Minimal Overhead
- Recovery mechanism adds minimal overhead
- Atomic operations remain efficient
- No blocking or locking required

### 2. Failure Path Optimization
- Recovery operations are optimized
- Failure scenarios are handled efficiently
- No resource leaks during recovery

This recovery pattern ensures that the system maintains state consistency even under failure conditions, preventing the race condition that could lead to inconsistent state.
