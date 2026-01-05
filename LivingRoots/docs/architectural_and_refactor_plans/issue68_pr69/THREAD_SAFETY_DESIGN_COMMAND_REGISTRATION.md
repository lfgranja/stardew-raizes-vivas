# Thread Safety Design: Command Registration Implementation

## Design Overview

This document outlines the detailed design for fixing the race condition in command registration in `ModController.cs`. The solution implements an atomic registration pattern with proper error recovery.

## Core Design Elements

### 1. Atomic Registration Method

Introduce a new method `TryRegisterCommandAtomically` that performs the complete registration operation atomically:

```csharp
private bool TryRegisterCommandAtomically(IModHelper helper, IMonitor monitor)
{
    // Use a loop to handle potential race conditions during the multi-step process
    int currentState, newState;
    bool registrationAttempted = false;
    bool commandAvailable = false;

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

        // Set the flag and proceed with registration
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
                commandAvailable = true;
            }
            catch (Exception ex)
            {
                // If registration failed after setting the flag, reset the flag to maintain consistency
                monitor?.Log($"Command registration failed after flag was set: {ex.Message}", LogLevel.Error);
                
                // Reset the command registered flag to maintain state consistency
                Interlocked.And(ref _state, ~CommandRegisteredFlag);
                return false;
            }
        }
    }
    while (!registrationAttempted);

    return commandAvailable;
}
```

### 2. Updated OnGameLaunched Method

Modify the `OnGameLaunched` method to use the new atomic registration method:

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

## Winner Takes All Pattern Implementation

### 1. Compare-and-Swap Logic

The design uses `Interlocked.CompareExchange` to implement the Winner Takes All pattern:
- Only the first thread to successfully update the state flag wins
- Other threads will detect that the flag is already set and exit gracefully
- No thread can register the command if another has already done so

### 2. Prerequisite Verification

Before setting the flag, the method verifies that `ConsoleCommands` is available, ensuring that the winning thread can actually complete the registration.

## Recovery Pattern

### 1. State Consistency Recovery

If command registration fails after the flag has been set, the system automatically resets the flag to maintain state consistency:

```csharp
catch (Exception ex)
{
    // If registration failed after setting the flag, reset the flag to maintain consistency
    monitor?.Log($"Command registration failed after flag was set: {ex.Message}", LogLevel.Error);
    
    // Reset the command registered flag to maintain state consistency
    Interlocked.And(ref _state, ~CommandRegisteredFlag);
    return false;
}
```

### 2. Error Handling

Comprehensive error handling ensures that any failure during registration results in proper state cleanup.

## Thread Safety Mechanisms

### 1. Atomic Operations

- Uses `Interlocked.CompareExchange` for state flag updates
- Uses `Volatile.Read` for state checks
- Uses `Interlocked.And` for flag resets

### 2. Dependency Snapshots

- Creates local snapshots of dependencies to prevent null reference exceptions during concurrent disposal

### 3. Loop-Based Retry

- Uses a loop with compare-and-swap to handle potential race conditions during the multi-step registration process

## Integration with Existing Architecture

### 1. State Management

- Maintains compatibility with existing bit flag state system
- Preserves the `EventsRegisteredFlag`, `CommandRegisteredFlag`, and `DisposedFlag` structure
- Ensures disposed flag is never cleared accidentally

### 2. Logging and Monitoring

- Maintains existing logging patterns and message formats
- Adds appropriate error logging for the new failure scenarios
- Preserves log level consistency

### 3. Event Handling

- Preserves existing event handler unsubscription pattern
- Maintains the one-time execution behavior of `OnGameLaunched`

## Performance Characteristics

### 1. Efficiency

- Minimal overhead with efficient atomic operations
- No blocking or locking mechanisms
- Optimistic concurrency approach reduces contention

### 2. Scalability

- Scales well with multiple concurrent threads
- No performance degradation with increased thread count
- Constant time complexity for successful operations
