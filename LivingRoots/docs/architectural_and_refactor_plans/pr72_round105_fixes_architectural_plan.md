# PR #72 Round 105 Fixes - Architectural Plan

## Executive Summary

This architectural plan addresses code review feedback from PR #72 Round 105, focusing on improving the robustness, security, and maintainability of the Living Roots mod. The plan addresses five distinct issues of varying severity levels in the SoilHealthService and ModControllerTests components.

## Issues Analysis

### HIGH SEVERITY: UpdateHealth Method NaN/Infinity Validation
- **File**: `LivingRoots/Services/SoilHealthService.cs`
- **Issue**: The `UpdateHealth` method does not validate the `delta` parameter for NaN or Infinity values
- **Impact**: Invalid delta values cause `newHealth` to become NaN, which is clamped to 0f by `SetHealthInternal`, resulting in unintentional data loss
- **Root Cause**: Missing input validation for floating-point arithmetic operations

### MEDIUM SEVERITY: Defensive Check for Null/Whitespace Location Keys
- **File**: `LivingRoots/Services/SoilHealthService.cs`
- **Issue**: No check for null or whitespace location keys before saving in the SaveData method
- **Impact**: Potential runtime exceptions or invalid data being saved to persistent storage
- **Root Cause**: Missing defensive programming practices in data persistence operations

### LOW SEVERITY: FormatterServices.GetUninitializedObject Removal
- **File**: `LivingRoots.Tests/ModControllerTests.cs`
- **Issue**: Using `FormatterServices.GetUninitializedObject` bypasses constructors, leading to flaky tests
- **Impact**: Unreliable test results due to uninitialized object state
- **Root Cause**: Using unsafe reflection practices in test helper methods

### LOW SEVERITY: Tile Key Parsing Trim
- **File**: `LivingRoots/Services/SoilHealthService.cs`
- **Issue**: No trimming of whitespace from coordinate strings before parsing in LoadData
- **Impact**: Potential parsing failures when coordinate strings contain leading/trailing whitespace
- **Root Cause**: Missing input sanitization in data loading operations

### LOW SEVERITY: TargetInvocationException Unwrapping
- **File**: `LivingRoots.Tests/ModControllerTests.cs`
- **Issue**: Exceptions during reflection are wrapped in TargetInvocationException, making test failures harder to debug
- **Impact**: Poor debugging experience when reflection-based tests fail
- **Root Cause**: Not properly handling exception wrapping in reflection operations

## Proposed Solutions

### Solution 1: UpdateHealth Parameter Validation (HIGH)
Add validation check at the beginning of the `UpdateHealth` method to prevent NaN/Infinity delta values:

```csharp
public void UpdateHealth(string locationName, Vector2 tile, float delta)
{
    // Validate delta parameter for NaN or Infinity
    if (float.IsNaN(delta) || float.IsInfinity(delta))
    {
        return; // Ignore invalid delta values.
    }
    
    // Validate the tile using the validation helper
    if (!IsValidTile(locationName, tile, out Point tilePoint))
    {
        return; // Skip if location or tile is invalid
    }

    // Variables to track if we need to log warnings (set inside the lock, used outside)
    bool logLocationLimitExceeded = false;
    bool logTileLimitExceeded = false;
    string truncatedLocationNameForTileLog = "";

    lock (_lock)
    {
        // Read the current health value from the cache
        float currentHealth = 0f;
        if (_runtimeCache.TryGetValue(locationName, out var tiles))
        {
            if (tiles.TryGetValue(tilePoint, out float health))
            {
                currentHealth = health;
            }
        }

        // Calculate the new health value
        float newHealth = currentHealth + delta;

        // Use the internal helper method to set the health value
        SetHealthInternal(locationName, tilePoint, newHealth, ref logLocationLimitExceeded,
            ref logTileLimitExceeded, ref truncatedLocationNameForTileLog);
    }
    
    // Log messages outside the lock to avoid blocking other threads
    if (logLocationLimitExceeded)
    {
        _monitor.Log($"Location count limit ({ModConstants.MaxLocationsPerSave}) reached in runtime cache; refusing to add new location to prevent memory growth.", LogLevel.Warn);
    }
    else if (logTileLimitExceeded)
    {
        _monitor.Log($"Tile count limit ({ModConstants.MaxTilesPerLocation}) exceeded for location '{truncatedLocationNameForTileLog}'; refusing to add new tile to prevent memory growth.", LogLevel.Warn);
    }
}
```

### Solution 2: Defensive Location Key Check (MEDIUM)
Add null/whitespace check in the SaveData method loop:

```csharp
lock (_lock)
{
    foreach (var location in _runtimeCache)
    {
        // ADD DEFENSIVE CHECK: Skip if location key is null or whitespace
        if (string.IsNullOrWhiteSpace(location.Key))
        {
            continue;
        }
        
        // ADD LOCATION NAME LENGTH CHECK: Check location name length during save to ensure consistency with load logic
        if (location.Key.Length > ModConstants.MaxLocationNameLength)
        {
            continue; // Skip locations with names that are too long
        }
        
        var tileDict = new Dictionary<string, float>();
        foreach (var tile in location.Value)
        {
            // Convert Point back to "X,Y" string format using invariant culture for consistency
            string tileKey = $"{tile.Key.X.ToString(CultureInfo.InvariantCulture)},{tile.Key.Y.ToString(CultureInfo.InvariantCulture)}";
            
            // Clamp value to valid range [0, 100] before saving - ClampHealthValue handles NaN/Infinity
            float clampedValue = ClampHealthValue(tile.Value);
            
            // Only save non-zero values to prevent bloating the save file with default values
            if (clampedValue != 0f)
            {
                tileDict[tileKey] = clampedValue;
            }
        }

        // Only add location if it has valid tiles
        if (tileDict.Count > 0)
        {
            snapshotState[location.Key] = tileDict;
        }
    }
}
```

### Solution 3: Remove FormatterServices Fallback (LOW)
Modify the `CreateInstanceWithFallback` method to remove the third fallback attempt:

```csharp
private static T CreateInstanceWithFallback<T>() where T : class
{
    Exception? lastError = null;

    // First attempt: Try Activator.CreateInstance with nonPublic: true
    try
    {
        var instance = Activator.CreateInstance(typeof(T), nonPublic: true) as T;
        if (instance != null)
        {
            return instance;
        }
    }
    catch (Exception ex)
    {
        lastError = ex;
        // Fall through to reflection-based approach
    }

    // Second attempt: Use reflection to find and invoke constructors with default parameter values
    var constructors = typeof(T).GetConstructors(
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    // Sort constructors by parameter length to prioritize simpler constructors
    foreach (var constructor in constructors.OrderBy(c => c.GetParameters().Length))
    {
        try
        {
            var parameters = constructor.GetParameters();

            // Skip constructors that have parameters without default values, optional markers, or value types
            // This ensures we only use constructors that can be safely satisfied by defaults
            if (parameters.Any(p => !p.HasDefaultValue && !p.IsOptional && !p.ParameterType.IsValueType))
                continue;

            // Create arguments using LINQ with proper default values
            var args = parameters.Select(p =>
            {
                // Check for optional parameters first to ensure constructor's actual default values are used
                if (p.IsOptional)
                    return Type.Missing;
                if (p.HasDefaultValue)
                    return p.DefaultValue;
                if (p.ParameterType.IsValueType)
                    return Activator.CreateInstance(p.ParameterType);
                return Type.Missing;
            }).ToArray();

            // Try to invoke the constructor with the default arguments
            var instance = constructor.Invoke(args) as T;
            if (instance != null)
            {
                return instance;
            }
        }
        catch (Exception ex)
        {
            lastError = ex;
            // Try the next constructor
            continue;
        }
    }

    // If all attempts fail, throw an informative exception with the last error as InnerException
    throw new InvalidOperationException(
        $"Failed to create instance of type {typeof(T)} for tests. " +
        $"Tried Activator.CreateInstance and all constructors with default/optional parameters via reflection. " +
        $"Ensure the type has an accessible constructor with default/optional parameters or provide a test-specific factory method.",
        lastError);
}
```

### Solution 4: Add Trim to Tile Key Parsing (LOW)
Update the tile key parsing logic in the `ProcessTileEntry` method:

```csharp
// Parse "X,Y" string back to Point (using integers for tile coordinates)
// Use ReadOnlySpan<char> to avoid string.Split allocation for better performance
ReadOnlySpan<char> keySpan = tileEntry.Key.Trim(); // ADD TRIM TO REMOVE WHITESPACE
int commaIndex = keySpan.IndexOf(',');
if (commaIndex > 0 && commaIndex < keySpan.Length - 1 &&
    int.TryParse(keySpan.Slice(0, commaIndex).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) &&
    int.TryParse(keySpan.Slice(commaIndex + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
{
    // ADDITION: Check for extreme coordinates to prevent potential issues with malicious save files
    // Using direct boundary checks instead of Math.Abs to prevent overflow when dealing with int.MinValue
    if (x < -ModConstants.MaxAbsoluteTileCoordinate || x > ModConstants.MaxAbsoluteTileCoordinate || 
        y < -ModConstants.MaxAbsoluteTileCoordinate || y > ModConstants.MaxAbsoluteTileCoordinate)
    {
        if (!warnedForMalformedKey)
        {
            // Use the helper method to truncate the location name for logging
            string truncatedLocationName = TruncateForLogging(locationName);
            _monitor.Log($"Extreme tile coordinates found in save data for location '{truncatedLocationName}'; skipping entry.", LogLevel.Warn);
            warnedForMalformedKey = true;
        }
        return false; // Skip this entry
    }

    // Simplified validation logic: check for NaN/Infinity first, then range
    float validatedValue = tileEntry.Value;
    
    // Check for NaN or Infinity values and convert to 0 instead of skipping
    if (float.IsNaN(validatedValue) || float.IsInfinity(validatedValue))
    {
        // Only warn once per location for invalid values to prevent log spam
        if (!warnedForInvalidValue)
        {
            // Use the helper method to truncate the location name for logging
            string truncatedLocationName = TruncateForLogging(locationName);
            _monitor.Log($"Invalid health value (NaN/Infinity) found in save data for location '{truncatedLocationName}'; converting to 0.", LogLevel.Warn);
            warnedForInvalidValue = true;
        }
        validatedValue = 0f; // Convert to 0 instead of skipping
    }
    else if (validatedValue < ModConstants.MinSoilHealth || validatedValue > ModConstants.MaxSoilHealth)
    {
        // Only warn once per location for out-of-range values to prevent log spam
        if (!warnedForInvalidValue)
        {
            // Use the helper method to truncate the location name for logging
            string truncatedLocationName = TruncateForLogging(locationName);
            _monitor.Log($"Invalid health value found in save data for location '{truncatedLocationName}'; clamping to valid range [{ModConstants.MinSoilHealth}, {ModConstants.MaxSoilHealth}].", LogLevel.Warn);
            warnedForInvalidValue = true;
        }
        validatedValue = ClampHealthValue(validatedValue);
    }

    tilePoint = new Point(x, y);
    value = validatedValue;
    return true; // Successfully processed
}
```

### Solution 5: Unwrap TargetInvocationException (LOW)
Update reflection-based test methods to properly handle TargetInvocationException:

```csharp
// WRAP REFLECTION INVOKE CALL WITH TRY-CATCH TO UNWRAP TARGETINVOCATIONEXCEPTION
try
{
    onGameLaunchedMethod.Invoke(controller, new object[] { controller, gameLaunchedEventArgs });
}
catch (TargetInvocationException tie) when (tie.InnerException != null)
{
    throw tie.InnerException; // Rethrow the actual exception that occurred in the invoked method
}
```

## Implementation Strategy (TDD Approach)

### Phase 1: Unit Tests for New Validation Logic
1. Create unit tests for the `UpdateHealth` method with NaN and Infinity delta values
2. Create unit tests for the SaveData method with null/whitespace location keys
3. Update existing tests to handle the new validation behavior

### Phase 2: Implementation of Core Fixes
1. Implement the UpdateHealth validation fix
2. Add the defensive location key check in SaveData
3. Add trim operations to tile key parsing

### Phase 3: Test Infrastructure Improvements
1. Remove the FormatterServices fallback from CreateInstanceWithFallback
2. Update reflection-based test methods to properly unwrap TargetInvocationException

### Phase 4: Integration Testing
1. Run all existing unit tests to ensure no regressions
2. Create additional integration tests for the fixed functionality
3. Verify all fixes work correctly in combination

## Risk Assessment and Mitigation

### Risks
1. **Performance Impact**: Additional validation checks might slightly impact performance
2. **Behavioral Changes**: New validation might change existing behavior for edge cases
3. **Test Reliability**: Changes to CreateInstanceWithFallback might affect test reliability

### Mitigation Strategies
1. **Performance**: All validation operations are lightweight and should have minimal impact
2. **Behavior**: New validation prevents invalid data processing, which improves overall system stability
3. **Tests**: Thorough unit testing of CreateInstanceWithFallback to ensure it still works for valid cases

## Testing Strategy

### Unit Tests for UpdateHealth Validation
- Test with NaN delta value
- Test with Infinity delta value
- Test with -Infinity delta value
- Test with valid delta values to ensure normal functionality is preserved

### Unit Tests for SaveData Defensive Checks
- Test with null location keys
- Test with whitespace-only location keys
- Test with valid location keys to ensure normal functionality is preserved

### Unit Tests for Tile Key Parsing
- Test with leading/trailing whitespace in coordinate strings
- Test with valid coordinate strings to ensure normal functionality is preserved

### Unit Tests for CreateInstanceWithFallback
- Test with types that have parameterless constructors
- Test with types that have constructors with optional parameters
- Verify that the method still works for valid cases after removing FormatterServices fallback

## SOLID, DRY, KISS, YAGNI, DDD Compliance

### SOLID Principles
- **Single Responsibility**: Each fix addresses a specific, well-defined issue
- **Open/Closed**: Fixes extend functionality without breaking existing code
- **Liskov Substitution**: No changes to interface contracts
- **Interface Segregation**: No interface changes
- **Dependency Inversion**: No dependency changes

### DRY (Don't Repeat Yourself)
- The validation logic follows existing patterns in the codebase
- Reuses existing helper methods like `ClampHealthValue` and `IsValidTile`

### KISS (Keep It Simple, Stupid)
- Simple validation checks using built-in .NET methods
- Minimal code changes to fix each issue
- Clear, straightforward implementation

### YAGNI (You Aren't Gonna Need It)
- Only implementing fixes for the specific issues identified
- No speculative future-proofing beyond the current requirements

### DDD (Domain-Driven Design)
- Maintains consistency with existing domain logic
- Preserves the soil health domain rules and constraints

## Dependencies and Implementation Order

### Optimal Implementation Order
1. **CreateInstanceWithFallback modification** - This is the safest to change first as it affects only test code
2. **TargetInvocationException unwrapping** - Update test methods to properly handle exceptions
3. **Tile key parsing trim** - This is a low-risk change that improves robustness
4. **Location key defensive check** - This adds safety to the save operation
5. **UpdateHealth validation** - This is the highest priority fix as it addresses data integrity concerns

### Dependencies
- The CreateInstanceWithFallback fix is independent
- The TargetInvocationException fix is independent
- The tile key parsing fix is independent
- The location key check is independent
- The UpdateHealth validation is independent

Each fix can be implemented and tested separately, reducing risk and enabling easier debugging if issues arise.

## Quality Assurance

### Code Review Checklist
- [ ] All validation checks use appropriate .NET methods (float.IsNaN, float.IsInfinity)
- [ ] Defensive programming practices are consistently applied
- [ ] Error messages are clear and informative
- [ ] Performance impact is minimal
- [ ] No breaking changes to public APIs
- [ ] All changes follow existing code style and patterns

### Testing Checklist
- [ ] Unit tests cover all new validation paths
- [ ] Existing tests continue to pass
- [ ] Edge cases are properly handled
- [ ] Error conditions are tested
- [ ] Integration tests verify the complete functionality
- [ ] Performance is not significantly impacted

## Conclusion

This architectural plan provides a comprehensive approach to addressing the code review feedback from PR #72 Round 105. The plan prioritizes the most critical fixes (HIGH severity) while also addressing MEDIUM and LOW severity issues to improve overall code quality. The implementation strategy follows TDD principles, ensuring that all changes are properly tested and validated before deployment. The solutions maintain compliance with SOLID, DRY, KISS, YAGNI, and DDD principles while improving the robustness and maintainability of the codebase.