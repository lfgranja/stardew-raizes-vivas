# Soil Health Persistence Feature - Security & Optimization Architectural Plan

## Overview

This architectural plan addresses security, optimization, and exception handling issues in the Soil Health Persistence feature. The plan follows SOLID, DRY, KISS, YAGNI, DDD, and TDD principles to enhance the security and performance of the soil health data persistence system.

## Issues Identified

1. **Security Issue**: No tile count limits in `SoilHealthService.LoadData` method, allowing potential DoS attacks from malicious save files
2. **Optimization Issue**: Cache stores all values including default (0f) values, which is inefficient
3. **Security Issue**: Exception logging in `ModController.RegisterEvents` could potentially leak sensitive information
4. **Exception Handling Issue**: Exceptions are being swallowed in `SoilHealthService` save/load operations instead of being properly propagated

## Architectural Changes

### 1. Tile Count Limits in SoilHealthService

#### Problem
The current `LoadData` method does not limit the number of tiles that can be loaded from save files, making it vulnerable to DoS attacks from malicious save files with excessive tile data.

#### Solution
Implement tile count limits with a configurable maximum to prevent DoS attacks while maintaining reasonable limits for legitimate game saves.

#### Implementation Details
- Add a `MaxTilesPerSave` constant to `ModConstants`
- Add tile counting logic in the `LoadData` method
- Implement early termination when tile count exceeds the limit
- Add logging for when limits are exceeded

```csharp
// In ModConstants.cs
public const int MaxTilesPerSave = 100000; // Maximum tiles allowed per save file

// In SoilHealthService.LoadData method
int totalTilesLoaded = 0;
foreach (var locationEntry in locations)
{
    // ... existing location processing ...
    
    foreach (var tileEntry in locationEntry.Value)
    {
        // ... existing tile validation ...
        
        // Check tile count limit before adding to cache
        if (totalTilesLoaded >= ModConstants.MaxTilesPerSave)
        {
            _monitor.Log($"Tile count limit exceeded ({ModConstants.MaxTilesPerSave}) during load; stopping to prevent DoS attack.", LogLevel.Warn);
            break; // Stop processing to prevent DoS
        }
        
        tileDict[new Point(x, y)] = validatedValue;
        totalTilesLoaded++;
    }
    if (totalTilesLoaded >= ModConstants.MaxTilesPerSave)
        break; // Stop processing locations if limit reached
}
```

### 2. Sparse Cache Implementation

#### Problem
The current cache stores all values including default (0f) values, which is inefficient in terms of memory usage and performance.

#### Solution
Implement a sparse cache that only stores non-default values, reducing memory footprint and improving performance.

#### Implementation Details
- Create a new `SparseSoilHealthCache` class that only stores non-zero values
- Modify the cache structure to use a `Dictionary<string, Dictionary<Point, float>>` where empty dictionaries are removed
- Update `GetSoilHealth` to return 0 for missing entries
- Add cleanup logic to remove empty location dictionaries

```csharp
// In SoilHealthService
public float GetSoilHealth(string locationName, Vector2 tile)
{
    // ... existing validation ...
    
    float result;
    lock (_lock)
    {
        if (_runtimeCache.TryGetValue(locationName, out var tiles))
        {
            var key = new Point(ix, iy);
            if (tiles.TryGetValue(key, out float health))
            {
                result = health;
            }
            else
            {
                result = 0f; // Return default (Poor Soil) if no data exists
            }
        }
        else
        {
            result = 0f; // Return default (Poor Soil) if location doesn't exist
        }
        
        // Cleanup: Remove empty location dictionaries to maintain sparse cache
        if (tiles != null && tiles.Count == 0)
        {
            _runtimeCache.Remove(locationName);
        }
    }
    return result;
}

public void SetSoilHealth(string locationName, Vector2 tile, float value)
{
    // ... existing validation ...
    
    lock (_lock)
    {
        // Domain Rule: Clamp between 0 and 100 (not 10 as previously)
        float clampedValue = ClampHealthValue(value);

        // Only store non-default values in the sparse cache
        if (clampedValue != 0f)
        {
            // Use GetOrAddLocationCacheUnsafe to avoid code duplication
            var tiles = GetOrAddLocationCacheUnsafe(locationName);
            var key = new Point(ix, iy);
            tiles[key] = clampedValue;
        }
        else
        {
            // If value is 0, remove the tile from cache if it exists
            if (_runtimeCache.TryGetValue(locationName, out var tiles))
            {
                var key = new Point(ix, iy);
                tiles.Remove(key);
                
                // If location has no tiles left, remove the location entirely
                if (tiles.Count == 0)
                {
                    _runtimeCache.Remove(locationName);
                }
            }
        }
    }
}
```

### 3. Secure Exception Logging in ModController

#### Problem
The current exception logging in `ModController.RegisterEvents` could potentially leak sensitive information through exception messages.

#### Solution
Implement secure logging that avoids exposing raw exception details while still providing sufficient information for debugging.

#### Implementation Details
- Create a secure logging method that only logs exception type and generic message
- Add optional trace-level logging for detailed exception information
- Ensure no sensitive information is exposed through exception messages

```csharp
// In ModController.RegisterEvents
catch (Exception ex)
{
    // Log error without exposing raw exception message for security
    _monitor.Log("Error occurred while registering game events.", LogLevel.Error);
    
    // Add trace-level exception details for debugging without exposing sensitive information
    _monitor.Log($"RegisterEvents exception type: {ex.GetType().Name}", LogLevel.Trace);
    
    // Only log additional details if in debug mode or with user consent
    if (IsDebugMode())
    {
        _monitor.Log($"RegisterEvents exception details: {ex.Message}", LogLevel.Trace);
    }
}
```

### 4. Proper Exception Propagation in SoilHealthService

#### Problem
Exceptions are being swallowed in `SoilHealthService` save/load operations instead of being properly propagated, making error handling difficult.

#### Solution
Implement proper exception propagation with appropriate error handling patterns while maintaining security.

#### Implementation Details
- Create custom exceptions for soil health operations
- Implement proper exception wrapping to maintain security
- Add error handling strategies that allow consumers to handle errors appropriately

```csharp
// New custom exception class
public class SoilHealthDataException : Exception
{
    public SoilHealthDataException(string message, Exception innerException = null) 
        : base(message, innerException) { }
}

// In SoilHealthService.LoadData
try
{
    savedData = _modDataService.LoadData<SoilHealthState>(dataKey);
}
catch (Exception ex)
{
    // Log error but don't expose raw exception message for security
    _monitor.Log("Error loading soil health data.", LogLevel.Error);
    _monitor.Log($"LoadData exception type: {ex.GetType().Name}", LogLevel.Trace);
    
    // Throw a wrapped exception to allow proper error propagation
    throw new SoilHealthDataException("Failed to load soil health data", ex);
}

// In SoilHealthService.SaveData
try
{
    var stateToSave = new SoilHealthState { LocationHealthData = snapshotState };
    _modDataService.SaveData(stateToSave, dataKey);
    _monitor.Log($"Soil health data saved for {saveId}", LogLevel.Trace);
}
catch (Exception ex)
{
    // Log error but don't expose raw exception message for security
    _monitor.Log("Error saving soil health data.", LogLevel.Error);
    _monitor.Log($"SaveData exception type: {ex.GetType().Name}", LogLevel.Trace);
    
    // Throw a wrapped exception to allow proper error propagation
    throw new SoilHealthDataException("Failed to save soil health data", ex);
}
```

## Integration with Existing Architecture

### Domain Layer
- The `ISoilHealthService` interface remains unchanged to maintain backward compatibility
- New constants are added to `ModConstants` to support tile count limits
- Custom exceptions are added to the domain layer for proper error handling

### Service Layer
- `SoilHealthService` is enhanced with security and optimization features
- Sparse caching logic is implemented to reduce memory usage
- Exception handling is improved to follow proper propagation patterns

### Controller Layer
- `ModController` exception logging is made more secure
- Error handling patterns are standardized across the application

## Testing Strategy

### Unit Tests
- Test tile count limit enforcement in `LoadData` method
- Test sparse cache behavior with various scenarios
- Test secure exception logging in `ModController`
- Test proper exception propagation in save/load operations

### Integration Tests
- Test end-to-end save/load scenarios with large data sets
- Test security boundaries with malicious input
- Test performance improvements with sparse caching

### Security Tests
- Test DoS protection with excessive tile counts
- Test exception message sanitization
- Test data integrity during save/load cycles

## Design Principles Compliance

### SOLID Principles
- **Single Responsibility**: Each class has a clear, single purpose
- **Open/Closed**: Extensible through interfaces without modifying existing code
- **Liskov Substitution**: All implementations adhere to interface contracts
- **Interface Segregation**: Interfaces are focused and specific
- **Dependency Inversion**: High-level modules depend on abstractions

### DRY (Don't Repeat Yourself)
- Common validation logic is centralized
- Exception handling patterns are consistent across the codebase
- Configuration values are centralized in `ModConstants`

### KISS (Keep It Simple, Stupid)
- Tile count limits are implemented with simple counters
- Sparse cache logic is straightforward and efficient
- Exception handling follows simple, clear patterns

### YAGNI (You Aren't Gonna Need It)
- Features are implemented only as needed
- No unnecessary complexity is added
- Security measures are targeted to specific vulnerabilities

### DDD (Domain-Driven Design)
- Domain concepts are clearly represented
- Business rules are encapsulated in domain services
- Domain boundaries are maintained

### TDD (Test-Driven Development)
- Tests are written before implementation
- Behavior is verified through tests
- Refactoring is supported by comprehensive test coverage

## Implementation Roadmap

### Phase 1: Security Enhancements
1. Implement tile count limits in `SoilHealthService.LoadData`
2. Add secure exception logging in `ModController`
3. Update unit tests to verify security features

### Phase 2: Optimization Enhancements
1. Implement sparse cache functionality
2. Optimize memory usage patterns
3. Update performance tests

### Phase 3: Error Handling Improvements
1. Implement proper exception propagation
2. Add custom exception types
3. Update integration tests

### Phase 4: Verification and Validation
1. Run comprehensive test suite
2. Perform security validation
3. Verify performance improvements
4. Document changes and update architecture documentation

## Performance Considerations

### Memory Optimization
- Sparse caching reduces memory usage by only storing non-default values
- Tile count limits prevent excessive memory allocation from malicious files
- Cleanup logic removes empty dictionaries to maintain efficiency

### Performance Impact
- Initial load time may increase slightly due to tile counting
- Runtime performance should improve due to reduced memory footprint
- Exception handling overhead is minimal and only occurs on error paths

## Security Considerations

### DoS Protection
- Tile count limits prevent excessive resource consumption
- Early termination prevents processing of malicious data beyond limits
- Memory allocation is bounded by the tile count limit

### Information Disclosure
- Exception messages are sanitized to prevent information leakage
- Only exception types are logged at error level
- Detailed exception information is only available at trace level

### Data Integrity
- Input validation prevents corruption of the cache
- Boundary checks prevent integer overflow
- Value clamping ensures data remains within valid ranges

## Backward Compatibility

### API Compatibility
- All public interfaces remain unchanged
- Method signatures are preserved
- Existing functionality is maintained

### Data Compatibility
- Existing save files continue to work
- New security features don't affect legitimate data
- Migration path is seamless for existing users

## Risk Assessment

### High-Risk Areas
- Tile count limit enforcement could break legitimate large saves
- Sparse cache logic could introduce subtle bugs
- Exception propagation changes could break existing error handling

### Mitigation Strategies
- Conservative tile count limits based on realistic game scenarios
- Comprehensive testing of sparse cache behavior
- Gradual rollout with monitoring of exception propagation

## Monitoring and Observability

### Logging Strategy
- Security events are logged at appropriate levels
- Performance metrics are captured for optimization validation
- Error patterns are monitored for security incidents

### Metrics
- Tile count statistics for DoS detection
- Memory usage patterns for optimization validation
- Exception frequency for error handling verification

## Conclusion

This architectural plan addresses the identified security and optimization issues in the Soil Health Persistence feature while maintaining compatibility with existing code. The implementation follows best practices for security, performance, and maintainability, ensuring the feature remains robust and efficient while protecting against potential threats.