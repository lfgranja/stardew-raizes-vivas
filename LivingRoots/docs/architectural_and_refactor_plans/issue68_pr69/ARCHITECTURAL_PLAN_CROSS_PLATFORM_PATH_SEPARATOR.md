# Cross-Platform Path Separator Fix - Architectural Plan

## Problem Statement
The `ModDataServiceTests.cs` contains test assertions that fail on Windows because they expect forward slashes (`/`) in file paths but get platform-specific path separators (backslashes `\` on Windows) due to the use of `Path.Combine` in the `GetFilePath` method.

## Root Cause Analysis
1. **Issue Location**: `ModDataService.cs` line 399 in the `GetFilePath` method
2. **Problem**: `Path.Combine("data", $"{key}.json")` uses platform-specific path separators
3. **Impact**: Tests expecting consistent forward slash format fail on Windows
4. **Current Behavior**: 
   - Linux/Mac: `data/test_key.json` (forward slashes)
   - Windows: `data\test_key.json` (backslashes)

## Solution Architecture

### 1. Update GetFilePath Method to Use Consistent Path Separators
**Approach**: Replace `Path.Combine` with manual path construction using forward slashes for cross-platform compatibility.

**Implementation**:
```csharp
private string GetFilePath(string key)
{
    // The key should already be sanitized at this point, so we just return path
    // The validation already happened when SanitizeFileName was called in public methods

    // Return final path with .json extension using forward slash for cross-platform consistency
    return $"data/{key}.json";
}
```

### 2. Maintain Test Accuracy and Cross-Platform Compatibility
**Strategy**: 
- Use forward slashes consistently across all platforms since they work on both Unix-like systems and Windows
- Forward slashes are supported by .NET file APIs and SMAPI framework
- Maintain the same logical path structure while ensuring consistent string representation

### 3. SOLID Principles Compliance
- **Single Responsibility Principle**: The `GetFilePath` method remains focused on path construction only
- **Open/Closed Principle**: The change is closed for modification but open for extension if needed
- **Liskov Substitution Principle**: The behavior remains functionally equivalent
- **Interface Segregation Principle**: Not applicable to private method
- **Dependency Inversion Principle**: Already implemented with domain abstractions

### 4. DRY (Don't Repeat Yourself) Compliance
- The path construction logic is centralized in one method
- No duplicate path construction logic elsewhere in the codebase
- Consistent approach with `SanitizePathSegments` which already uses forward slashes

### 5. KISS (Keep It Simple, Stupid) Principle
- Simple string concatenation instead of platform-specific path combining
- Forward slashes are universally supported by .NET runtime
- Minimal code change with maximum impact

### 6. YAGNI (You Aren't Gonna Need It) Principle
- Avoid complex cross-platform path handling libraries
- Don't over-engineer with conditional logic based on platform detection
- Use the simplest solution that works across all platforms

### 7. DDD (Domain-Driven Design) Compliance
- Maintain clear domain language around data paths
- Keep domain logic in `ModLogic` while infrastructure concerns in `ModDataService`
- Preserve the semantic meaning of data paths

## Implementation Details

### Before (Problematic):
```csharp
private string GetFilePath(string key)
{
    // Return final path with .json extension
    return Path.Combine("data", $"{key}.json");
}
```

### After (Fixed):
```csharp
private string GetFilePath(string key)
{
    // The key should already be sanitized at this point, so we just return path
    // The validation already happened when SanitizeFileName was called in public methods

    // Return final path with .json extension using forward slash for cross-platform consistency
    return $"data/{key}.json";
}
```

## Impact Analysis

### Positive Impacts:
1. **Cross-Platform Compatibility**: Tests will pass consistently on Windows, Linux, and Mac
2. **Maintainability**: Simpler, more predictable path handling
3. **Test Reliability**: Eliminates platform-specific test failures
4. **Consistency**: Aligns with `SanitizePathSegments` method approach

### No Negative Impacts:
1. **Functionality**: .NET and SMAPI handle forward slashes correctly on all platforms
2. **Security**: No security implications as path validation still occurs
3. **Performance**: No performance degradation

## Verification Strategy

### Unit Tests:
1. All existing tests should pass on all platforms
2. Path construction tests should validate consistent forward slash format
3. Cross-platform compatibility tests should verify behavior on different OS

### Integration Tests:
1. Verify actual file operations work correctly with forward slash paths
2. Confirm SMAPI framework handles forward slash paths appropriately
3. Test file creation, reading, and deletion with the new path format

## Backwards Compatibility
- **Fully Backwards Compatible**: The change only affects the string representation of paths
- **No API Changes**: Public interface remains identical
- **No Breaking Changes**: All existing functionality preserved

## Risk Assessment
- **Risk Level**: Low
- **Failure Impact**: Minimal - worst case is file operations fail, but forward slashes are well-supported
- **Mitigation**: Thorough testing on multiple platforms before deployment

## Alternative Approaches Considered

### Alternative 1: Normalize paths in tests
- **Approach**: Update test assertions to normalize expected paths
- **Drawback**: Would require many test changes and doesn't fix the root cause
- **Verdict**: Rejected in favor of fixing the source

### Alternative 2: Platform detection and conditional logic
- **Approach**: Detect platform and adjust expectations accordingly
- **Drawback**: Increases complexity and maintenance burden
- **Verdict**: Rejected as it violates KISS principle

### Alternative 3: Use Path.AltDirectorySeparatorChar
- **Approach**: Replace backslashes with forward slashes after Path.Combine
- **Drawback**: Unnecessary complexity when simple string concatenation works
- **Verdict**: Rejected in favor of simpler solution

## Quality Assurance
- **Test Coverage**: All existing tests must continue to pass
- **Cross-Platform Testing**: Verify on Windows, Linux, and Mac environments
- **Regression Testing**: Ensure no existing functionality is broken
- **Performance Testing**: Confirm no performance degradation

## Deployment Strategy
1. **Local Testing**: Test changes on development machine
2. **CI/CD Integration**: Ensure changes pass all automated tests
3. **Gradual Rollout**: If applicable, deploy to staging first
4. **Monitoring**: Watch for any unexpected issues post-deployment

## Success Criteria
1. ✅ All tests pass consistently across all platforms
2. ✅ File operations continue to work correctly
3. ✅ No functional changes to the mod behavior
4. ✅ Cross-platform compatibility achieved
5. ✅ Code maintains existing quality standards
6. ✅ No performance degradation
7. ✅ Follows all specified principles (SOLID, DRY, KISS, YAGNI, DDD)

## Maintenance Considerations
- **Documentation**: Update any relevant documentation about path handling
- **Future Changes**: Any path-related changes should maintain cross-platform consistency
- **Monitoring**: Watch for any platform-specific issues in future development