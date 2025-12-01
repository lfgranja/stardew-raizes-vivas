# Failure Handling Design: Registration Failures After Atomic Operations

## Overview

This document outlines the design for handling registration failures that occur after atomic operations have been successfully performed. The design ensures that the system can recover gracefully from failures while maintaining data integrity and preventing inconsistent states.

## Failure Scenarios

### 1. Command Registration Failure After Atomic Flag Set
- The atomic operation to set the `CommandRegisteredFlag` succeeds
- The actual command registration (`commands.Add()`) fails due to SMAPI issues
- The flag is set but the command wasn't actually registered

### 2. Event Registration Failure After Atomic Flag Set
- The atomic operation to set the `EventsRegisteredFlag` succeeds
- The event subscription fails due to null references or other issues
- The flag is set but events weren't actually registered

### 3. External Dependency Failures
- SMAPI command system is temporarily unavailable
- Network or resource issues during registration
- Third-party system failures affecting registration

## Recovery Strategy

### 1. Immediate State Recovery

When a registration failure occurs after an atomic operation succeeds, the system must:

1. **Log the failure** without exposing internal details
2. **Reset the atomic flag** to allow retry by another thread or at a later time
3. **Clean up any partial state** that may have been created
4. **Continue execution** without disrupting other operations

### 2. State Recovery Implementation

```csharp
private bool TryRegisterCommandWithRecovery(IModHelper helper, IMonitor monitor)
{
    // Attempt to atomically reserve command registration
    bool canRegister = TrySetStateOnce(CommandRegisteredFlag);
    
    if (canRegister)
    {
        try
        {
            // Attempt the actual command registration
            var commands = helper?.ConsoleCommands;
            if (commands != null)
            {
                commands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                monitor.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
                return true; // Success: flag set and registration completed
            }
            else
            {
                // SMAPI command system is null - reset the flag
                Interlocked.And(ref _state, ~(CommandRegisteredFlag));
                monitor.Log("Command registration failed: ConsoleCommands is null.", LogLevel.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            // Registration failed after atomic reservation - reset the flag
            Interlocked.And(ref _state, ~(CommandRegisteredFlag));
            
            // Log the error without exposing stack trace
            monitor.Log($"Command registration failed: {ex.Message}", LogLevel.Error);
            
            return false; // Registration failed despite atomic reservation success
        }
    }
    
    // Another thread already registered the command
    return false;
}
```

### 3. Event Registration Recovery

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
            
            // Reset flag since registration failed - recover from partial state
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
        // Log error and reset the flag if registration failed - recovery from partial state
        monitor.Log($"Error registering events: {ex.Message}", LogLevel.Error);
        _onGameLaunchedHandler = null;
        
        // Reset the events registered flag to allow retry
        Interlocked.And(ref _state, ~(EventsRegisteredFlag));
    }
}
```

## Failure Handling Patterns

### 1. Try-Reset Pattern

The core pattern for handling failures after atomic operations:

```csharp
bool operationSucceeded = TrySetStateOnce(flag);
if (operationSucceeded)
{
    try
    {
        // Perform the actual operation that might fail
        PerformOperation();
    }
    catch (Exception ex)
    {
        // Reset the flag to allow retry
        Interlocked.And(ref _state, ~flag);
        LogError(ex);
    }
}
```

### 2. Snapshot-and-Validate Pattern

Take snapshots of dependencies and validate them before operations:

```csharp
var helper = _helper;  // Snapshot to prevent disposal during operation
var monitor = _monitor;

if (helper?.ConsoleCommands == null)
{
    // Reset flag without attempting registration
    Interlocked.And(ref _state, ~CommandRegisteredFlag);
    monitor.Log("Cannot register command: dependencies are unavailable.", LogLevel.Error);
    return;
}
```

### 3. Atomic-First Pattern

Always perform the atomic operation first, then the risky operation:

```csharp
// 1. Atomically reserve the operation
bool canProceed = TrySetStateOnce(flag);

if (canProceed)
{
    // 2. Perform the operation that might fail
    // 3. Handle any failures by resetting the atomic state
}
```

## Error Recovery Considerations

### 1. Non-Blocking Recovery

Recovery operations should not block other threads:

```csharp
// Use non-blocking atomic operations for recovery
Interlocked.And(ref _state, ~flag);  // Non-blocking operation
```

### 2. Idempotent Recovery

Recovery operations should be safe to perform multiple times:

```csharp
// Clearing a flag that's already clear is safe
Interlocked.And(ref _state, ~flag);  // Safe even if flag is already clear
```

### 3. Cascade Failure Prevention

Prevent one failure from causing system-wide issues:

```csharp
try
{
    RegisterCommand();
}
catch (Exception ex)
{
    // Handle locally - don't let failure affect other operations
    Interlocked.And(ref _state, ~CommandRegisteredFlag);
    LogError(ex);
    // Continue with other operations
}
```

## State Consistency Guarantees

### 1. Atomic State Consistency

After any failure, the atomic state must remain consistent:

- If registration fails, the corresponding flag must be reset
- No partial or inconsistent states should exist
- All state flags should accurately reflect actual system state

### 2. Dependency Consistency

Ensure dependencies remain in a consistent state:

```csharp
private void HandleEventRegistrationFailure()
{
    // Reset state flag
    Interlocked.And(ref _state, ~EventsRegisteredFlag);
    
    // Clean up any partial event subscriptions
    _onGameLaunchedHandler = null;
    
    // Ensure no dangling references
    // Log the issue for diagnostics
}
```

## Logging Strategy for Failures

### 1. Secure Error Logging

Log errors without exposing sensitive information:

```csharp
monitor.Log($"Command registration failed: {ex.Message}", LogLevel.Error);
// DO NOT log: ex.ToString() or stack traces
```

### 2. Diagnostic Information

Include sufficient information for debugging while maintaining security:

```csharp
monitor.Log($"Command registration failed at step X: {ex.Message}", LogLevel.Error);
// Include context but not internal details
```

## Retry Handling

### 1. Automatic Retry Prevention

The atomic flag system naturally prevents unwanted retries:

```csharp
// Once TrySetStateOnce succeeds for a specific flag, other threads will fail
// This prevents multiple retries of the same operation
```

### 2. Manual Retry Capability

Allow for manual retry when appropriate:

```csharp
// If external conditions improve, the system can reset all flags and retry
// This would typically happen during a full restart or reinitialization
```

## Thread Safety During Recovery

### 1. Concurrent Recovery Safety

Ensure recovery operations are thread-safe:

```csharp
// Recovery uses the same atomic operations as the original operations
Interlocked.And(ref _state, ~flag);  // Thread-safe recovery
```

### 2. No Race Conditions in Recovery

Recovery operations should not introduce new race conditions:

```csharp
// Recovery is a simple atomic operation that can't fail
// Multiple threads attempting recovery simultaneously is safe
```

## Performance Considerations

### 1. Minimal Performance Impact

Recovery operations should be fast:

```csharp
// Atomic operations are very fast
Interlocked.And(ref _state, ~flag);  // Minimal performance impact
```

### 2. No Blocking Operations

Recovery should not block other threads:

```csharp
// All recovery operations are non-blocking atomic operations
// No locks or blocking synchronization primitives used
```

## Validation of Recovery

### 1. Recovery Verification

Ensure recovery operations work correctly:

```csharp
// After recovery, the system should be in a state where
// another thread can successfully perform the same operation
```

### 2. State Verification

Verify that state flags accurately reflect system reality:

```csharp
// After command registration failure recovery:
// IsCommandRegistered() should return false
// The actual command should not be registered in SMAPI
```

This failure handling design ensures that the system can gracefully recover from registration failures while maintaining thread safety and state consistency.