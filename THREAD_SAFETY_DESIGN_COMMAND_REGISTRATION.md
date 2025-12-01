# Thread-Safe Command Registration Design

## Core Atomic Operation Design

The solution relies on a compare-and-swap (CAS) pattern using `Interlocked.CompareExchange` to ensure atomic command registration:

### 1. Two-Phase Registration Process

**Phase 1: Atomic State Reservation**
```csharp
private bool TryReserveCommandRegistration()
{
    int currentState, newState;
    bool reservationSuccessful = false;
    
    do
    {
        currentState = Volatile.Read(ref _state);
        
        // Check if command is already registered or if disposed
        if ((currentState & CommandRegisteredFlag) != 0 || 
            (currentState & DisposedFlag) != 0)
            return false;
        
        // Calculate new state with command registration flag set
        newState = currentState | CommandRegisteredFlag;
        
        // Atomically attempt to set the flag
        reservationSuccessful = 
            Interlocked.CompareExchange(ref _state, newState, currentState) == currentState;
    }
    while (!reservationSuccessful); // Retry if another thread modified state concurrently
    
    return true; // Reservation successful - this thread can proceed with registration
}
```

**Phase 2: Command Registration Execution**
```csharp
private void ExecuteCommandRegistration(IModHelper helper, IMonitor monitor)
{
    var commands = helper?.ConsoleCommands;
    if (commands != null)
    {
        commands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
        monitor.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
    }
}
```

### 2. Complete Thread-Safe Registration Method

```csharp
public bool TryRegisterCommand(IModHelper helper, IMonitor monitor)
{
    // Early exit if disposed
    if (IsDisposed())
    {
        monitor.Log("Attempted to register command after disposal. Operation skipped.", LogLevel.Trace);
        return false;
    }
    
    // Attempt to reserve command registration atomically
    bool canRegister = TryReserveCommandRegistration();
    
    if (canRegister)
    {
        // Only the thread that successfully reserved can execute registration
        ExecuteCommandRegistration(helper, monitor);
        return true; // Successfully registered
    }
    else
    {
        // Another thread already registered the command
        monitor.Log("Command 'lr_version' already registered by another thread.", LogLevel.Trace);
        return false; // Registration was not needed
    }
}
```

## Thread Safety Mechanisms

### 1. Compare-and-Swap (CAS) Operation

The core of the solution uses `Interlocked.CompareExchange` which performs three atomic operations:
1. Reads the current value of the state variable
2. Compares it with an expected value
3. If equal, replaces it with a new value; otherwise, returns the actual current value

This ensures that only one thread can successfully modify the state at a time.

### 2. Volatile Memory Access

`Volatile.Read` ensures that:
- The read operation is not reordered with other memory operations
- The value is read from main memory, not from CPU cache
- All threads see a consistent view of the state

### 3. Retry Loop Pattern

The do-while loop ensures that if a thread fails to update the state (because another thread modified it concurrently), it retries the operation with the latest state value.

## Race Condition Elimination

### Before the Fix:
```
Thread A: Check IsCommandRegistered() -> false
Thread B: Check IsCommandRegistered() -> false
Thread A: Register command
Thread B: Register command (DUPLICATE!)
```

### After the Fix:
```
Thread A: CAS operation to set flag -> SUCCESS, proceeds with registration
Thread B: CAS operation to set flag -> FAILS (flag already set), skips registration
```

## State Transition Matrix

| Current State | Operation | Result State | Success |
|---------------|-----------|--------------|---------|
| Not Registered | TrySetStateOnce(CommandRegisteredFlag) | Command Registered | Yes |
| Command Registered | TrySetStateOnce(CommandRegisteredFlag) | Command Registered | No (already set) |
| Disposed | TrySetStateOnce(CommandRegisteredFlag) | Disposed | No (disposed) |
| Events Registered | TrySetStateOnce(CommandRegisteredFlag) | Events+Command Registered | Yes |

## Error Handling and Recovery

### 1. Registration Failure After Atomic Reservation

If the actual command registration fails after successfully reserving the registration:

```csharp
private bool TryRegisterCommandWithRecovery(IModHelper helper, IMonitor monitor)
{
    bool canRegister = TryReserveCommandRegistration();
    
    if (canRegister)
    {
        try
        {
            ExecuteCommandRegistration(helper, monitor);
            return true;
        }
        catch (Exception ex)
        {
            // Log the error
            monitor.Log($"Command registration failed: {ex.Message}", LogLevel.Error);
            
            // Reset the flag to allow another attempt
            Interlocked.And(ref _state, ~(CommandRegisteredFlag));
            
            return false;
        }
    }
    
    return false; // Another thread already registered
}
```

### 2. Dependency Snapshotting

To prevent `NullReferenceException` if the controller is disposed during operation:

```csharp
public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    // Early exit if disposed
    if (IsDisposed())
    {
        _monitor.Log("OnGameLaunched called after disposal. Operation skipped.", LogLevel.Trace);
        return;
    }

    // Create snapshots of dependencies to avoid errors if disposed mid-execution
    var monitor = _monitor;
    var helper = _helper;
    
    // Use atomic operation to ensure only one thread registers the command
    bool wasCommandRegistered = TrySetStateOnce(CommandRegisteredFlag);
    
    if (wasCommandRegistered)
    {
        // Only this thread proceeds with actual registration
        var commands = helper?.ConsoleCommands;
        if (commands != null)
        {
            commands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
            monitor.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
        }
    }
}
```

## Thread Safety Verification

### 1. Linearizability
Each atomic operation appears to occur instantaneously at some point between its invocation and response, ensuring consistent ordering across all threads.

### 2. Atomicity
The state change is indivisible - other threads never observe a partial state change.

### 3. Isolation
Concurrent operations do not interfere with each other, maintaining data integrity.

## Performance Characteristics

### 1. Time Complexity
- Best case: O(1) - single atomic operation succeeds
- Worst case: O(k) where k is the number of concurrent attempts
- Average case: O(1) under normal contention

### 2. Memory Usage
- Single integer field for state management
- No additional memory allocation during operations
- Minimal memory footprint

### 3. Scalability
- Scales well with increasing thread count
- No blocking operations that cause thread contention
- Efficient under high concurrency

## Implementation Safety Guarantees

### 1. Mutual Exclusion
Only one thread can successfully register the command at a time.

### 2. Progress Guarantee
Eventually, one thread will succeed in registering the command.

### 3. Bounded Wait Time
No thread waits indefinitely; all threads either succeed or detect that another thread succeeded.

This design ensures that command registration is completely thread-safe while maintaining all existing functionality and performance characteristics.