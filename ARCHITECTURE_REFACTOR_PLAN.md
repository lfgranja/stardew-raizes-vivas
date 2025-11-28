# Architecture Refactor Plan: ModDataService Consistency Improvements

## Overview
This document outlines the architectural plan to improve ModDataService consistency, focusing on two key areas:
1. Using consistent path separators (forward slashes instead of platform-specific DirectorySeparatorChar)
2. Improving exception handling patterns in LoadData and DataExists methods

## Current Issues Analysis

### Path Separator Inconsistency
**Location**: [`LivingRoots/Services/ModDataService.cs`](LivingRoots/Services/ModDataService.cs:396) - `SanitizePathSegments` method

**Issue**: The method uses `Path.DirectorySeparatorChar` which is platform-specific:
```csharp
return string.Join(Path.DirectorySeparatorChar.ToString(), validSegments);
```

**Problem**: This creates inconsistent keys across platforms. SMAPI's data system expects consistent keys regardless of the underlying platform's directory separator.

### Exception Handling Inconsistencies
**Locations**: 
- [`LoadData<T>(string key)`](LivingRoots/Services/ModDataService.cs:79) method
- [`DataExists(string key)`](LivingRoots/Services/ModDataService.cs:168) method

**Issues**:
1. Different exception handling patterns between methods
2. Some exceptions are caught and logged generically, others re-thrown
3. Inconsistent logging levels for similar issues

## Architectural Solution

### 1. Consistent Path Separator Implementation

#### Current Implementation (Problematic):
```csharp
private string SanitizePathSegments(string path)
{
    // ... validation logic ...
    
    // Join sanitized segments back together using system's directory separator
    return string.Join(Path.DirectorySeparatorChar.ToString(), validSegments);
}
```

#### Proposed Implementation (Solution):
```csharp
private string SanitizePathSegments(string path)
{
    // ... validation logic ...
    
    // Join sanitized segments back together using forward slashes for consistency
    // This ensures keys are consistent across all platforms for SMAPI's data system
    return string.Join("/", validSegments);
}
```

#### Rationale:
- SMAPI's data system uses forward slashes consistently
- Keys should be platform-agnostic for reliable data access
- Forward slashes work correctly on all platforms with SMAPI

### 2. Improved Exception Handling Patterns

#### LoadData Method Exception Handling:
- Maintain current behavior: return null for expected exceptions (file not found, etc.)
- Standardize exception logging with consistent messages
- Keep re-throwing for unexpected critical errors

#### DataExists Method Exception Handling:
- Maintain current behavior: return false for expected exceptions
- Align exception handling patterns with LoadData for consistency
- Standardize logging patterns

### 3. Maintaining Functionality

#### Key Requirements to Preserve:
1. Path validation and sanitization for security
2. Proper null handling and argument validation
3. Security measures (no information disclosure)
4. Cross-platform compatibility
5. Existing API contract (interface remains unchanged)

#### Implementation Strategy:
- Use forward slashes only for keys passed to SMAPI
- Keep internal file system operations using appropriate separators when needed
- Maintain all security validations and sanitization logic

## SOLID, DRY, KISS, YAGNI, and DDD Principles Application

### SOLID Principles:
- **Single Responsibility**: Each method maintains its specific responsibility while improving consistency
- **Open/Closed**: Extend functionality through improved consistency without modifying existing behavior
- **Liskov Substitution**: Interface contract remains unchanged
- **Interface Segregation**: No changes to interface
- **Dependency Inversion**: Dependencies remain the same

### DRY (Don't Repeat Yourself):
- Standardize exception handling patterns between LoadData and DataExists
- Create consistent logging approach across methods

### KISS (Keep It Simple, Stupid):
- Use simple forward slash separator consistently
- Maintain existing security and validation logic
- Avoid complex cross-platform path handling

### YAGNI (You Aren't Gonna Need It):
- Focus only on path separator consistency and exception handling
- Don't add unnecessary abstractions or complexity
- Keep changes minimal and targeted

### DDD (Domain-Driven Design):
- Maintain domain logic in ModLogic class unchanged
- Keep business rules and validation intact
- Preserve ubiquitous language in method names and contracts

## Security Measures Preservation

### Current Security Measures to Maintain:
1. Path traversal validation via `_modLogic.ValidatePath()`
2. Filename sanitization via `_modLogic.SanitizeFileName()`
3. Generic error messages to prevent information disclosure
4. Proper null checks for dependencies
5. Input validation for all public methods

### Security Impact of Changes:
- Path separator change does not affect security validation
- All validation and sanitization logic remains intact
- Security logging patterns remain the same

## Cross-Platform Compatibility

### Current Cross-Platform Considerations:
- SMAPI handles cross-platform file operations
- ModDataService should provide consistent keys regardless of platform
- Forward slashes work consistently across Windows, macOS, and Linux in SMAPI context

### Compatibility Strategy:
- Use forward slashes for SMAPI keys (consistent with SMAPI's approach)
- Maintain platform-agnostic approach to data keys
- Ensure all existing functionality works identically across platforms

## Detailed Implementation Plan

### Phase 1: Path Separator Consistency
1. Update `SanitizePathSegments` method to use forward slashes
2. Update related tests to expect forward slash separators
3. Verify that all existing functionality remains intact

### Phase 2: Exception Handling Consistency
1. Review and standardize exception handling between LoadData and DataExists
2. Ensure consistent logging patterns
3. Maintain current behavior (return null/false vs. re-throwing)

### Phase 3: Testing
1. Update unit tests to reflect new path separator behavior
2. Verify all existing tests still pass
3. Add new tests for cross-platform path consistency

## Before/After Examples

### Before (Inconsistent Path Separators):
```csharp
// On Windows: "folder\\file" -> "folder\file"
// On Unix: "folder/file" -> "folder/file"
return string.Join(Path.DirectorySeparatorChar.ToString(), validSegments);
```

### After (Consistent Path Separators):
```csharp
// On all platforms: "folder/file" -> "folder/file" 
return string.Join("/", validSegments);
```

### Before (Inconsistent Exception Handling):
```csharp
// LoadData and DataExists had slightly different exception handling patterns
```

### After (Consistent Exception Handling):
```csharp
// Both methods follow the same pattern for similar exceptions
```

## Risk Assessment

### Low Risk Areas:
- Path separator change affects only internal key formatting
- No changes to security validation logic
- Interface contract remains unchanged

### Medium Risk Areas:
- Existing saved data with old path format may need migration
- Tests expecting specific path separator behavior need updates

### Mitigation Strategies:
- Maintain backward compatibility where possible
- Update tests to match new behavior
- Document the change for other developers

## Testing Strategy

### Unit Tests to Update:
- Update tests in [`LivingRoots.Tests/ModDataServiceTests.cs`](LivingRoots.Tests/ModDataServiceTests.cs) that expect specific path separator behavior
- Verify path separator consistency across different scenarios
- Ensure exception handling patterns are consistent

### Integration Tests:
- Verify that data saving/loading works correctly with new path separators
- Test cross-platform compatibility with SMAPI
- Ensure existing saved data remains accessible

## Implementation Timeline

### Phase 1: Path Separator Changes (1-2 days)
- Update `SanitizePathSegments` method
- Update related unit tests
- Verify functionality

### Phase 2: Exception Handling Consistency (1 day)
- Standardize exception handling patterns
- Update tests for consistency
- Verify behavior

### Phase 3: Testing and Validation (1-2 days)
- Run full test suite
- Verify cross-platform compatibility
- Document changes

## Expected Outcomes

### Functional Improvements:
1. Consistent path separators across all platforms
2. Improved code maintainability through consistent exception handling
3. Better cross-platform compatibility
4. No breaking changes to existing API

### Quality Improvements:
1. More predictable behavior across different operating systems
2. Easier debugging due to consistent path format
3. Better adherence to clean architecture principles
4. Improved testability with consistent patterns

## Success Metrics

### Code Quality:
- Consistent path separator usage throughout the service
- Unified exception handling patterns between methods
- Maintained security and validation logic

### Functional:
- All existing tests pass
- Cross-platform compatibility maintained
- No breaking changes to public API
- Existing saved data remains accessible