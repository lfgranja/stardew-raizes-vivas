# Architecture Refactor Plan for Security Fixes

## Overview
This document outlines the architectural approach for implementing security fixes identified in the code review for PR 67 - Rodada 19+. The changes follow SOLID principles and TDD methodology.

## Current Architecture Analysis

### Domain Layer
- `FileNameSanitizationService`: Handles filename sanitization with security considerations
- `PathValidationService`: Validates paths to prevent traversal attacks
- `ModLogic`: Orchestrates domain operations

### Services Layer
- `ModDataService`: Handles data persistence using SMAPI APIs
- `FileNameSanitizer`: Implements filename sanitization logic
- `PathTraversalValidator`: Validates path traversal patterns

### Security Issues Identified

#### 1. Extension Handling Vulnerability
**Location**: `FileNameSanitizationService.FindExtensionStartIndex`

**Current Implementation Issues**:
- Complex logic that may not correctly identify valid extensions
- Doesn't properly handle multiple consecutive dots
- May not consistently apply ".blocked" extension for dangerous files

**Security Impact**: Could allow dangerous file extensions to bypass sanitization

**Refactor Strategy**:
```csharp
// Current problematic logic
private static int FindExtensionStartIndex(string filename)
{
    // Complex logic with multiple edge cases
    int lastDotIndex = filename.LastIndexOf('.');
    if (lastDotIndex >= 0 && lastDotIndex < filename.Length - 1)
    {
        // Multiple checks that may not be comprehensive
        string potentialExtension = filename.Substring(lastDotIndex);
        // ...
    }
    return -1;
}

// Improved logic
private static int FindExtensionStartIndex(string filename)
{
    // Simplified, more robust approach
    int lastDotIndex = filename.LastIndexOf('.');
    
    if (lastDotIndex < 0 || lastDotIndex >= filename.Length - 1)
        return -1; // No valid extension if no dot or dot at end

    string potentialExtension = filename.Substring(lastDotIndex);
    
    // Check for path separators in extension (security check)
    if (potentialExtension.Contains('/', StringComparison.Ordinal) || 
        potentialExtension.Contains('\\', StringComparison.Ordinal))
        return -1;

    // Security: Handle dangerous extensions at beginning (e.g., ".exe")
    if (lastDotIndex == 0)
    {
        if (IsBlockedExtension(potentialExtension))
            return 0; // Treat as extension for security
        else
            return -1; // Not a real extension for non-dangerous cases
    }

    // Extract extension part (without the dot)
    string extensionPart = potentialExtension.Substring(1);
    
    // Must contain at least one alphanumeric character
    if (!extensionPart.Any(c => char.IsLetterOrDigit(c)))
        return -1;

    // Validate against invalid filename characters
    if (potentialExtension.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
        return -1;

    return lastDotIndex;
}
```

#### 2. Path Validation Redundancy
**Location**: `PathValidationService.ValidatePathTraversalDepth`

**Current Implementation Issues**:
- Redundant `minDepth` tracking alongside `depth < 0` check
- Overly restrictive "ends with .." check that blocks valid paths

**Security Impact**: May block legitimate paths while not improving security

**Refactor Strategy**:
```csharp
// Current redundant logic
private void ValidatePathTraversalDepth(string path)
{
    string[] segments = path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
    
    int depth = 0;
    int minDepth = 0; // Redundant tracking
    
    foreach (string segment in segments)
    {
        if (segment == "..")
        {
            depth--;
            if (depth < minDepth) // This check is redundant with depth < 0
            {
                minDepth = depth;
            }
            if (depth < 0) // Primary check
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }
        // ...
    }
    
    // Redundant final check
    if (minDepth < 0)
    {
        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
    }
    
    // Overly restrictive check
    if (segments.Length > 0 && segments[segments.Length - 1] == "..")
    {
        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
    }
}

// Improved logic
private void ValidatePathTraversalDepth(string path)
{
    string[] segments = path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
    
    int depth = 0;
    
    foreach (string segment in segments)
    {
        if (segment == "..")
        {
            depth--;
            // Primary security check: prevent going above root
            if (depth < 0)
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }
        else if (segment != ".")
        {
            depth++; // Regular segments increase depth
        }
        // "." segments don't change depth
    }
    // No redundant checks needed
}
```

#### 3. Inconsistent Data Access
**Location**: `ModDataService.DataExists`

**Current Implementation Issues**:
- Uses `File.Exists` instead of SMAPI's `ReadJsonFile` API
- Inconsistent with `LoadData` implementation
- Contains misleading exception handlers

**Security Impact**: Potential TOCTOU (Time-of-Check-Time-of-Use) race conditions

**Refactor Strategy**:
```csharp
// Current problematic implementation
public bool DataExists(string key)
{
    string sanitizedKey;
    try
    {
        sanitizedKey = GetValidatedAndSanitizedKey(key);
    }
    catch (ArgumentException ex)
    {
        _monitor.Log($"Invalid key provided to DataExists: {ex.Message}", LogLevel.Warn);
        return false;
    }

    try
    {
        string relativePath = GetFilePath(sanitizedKey);
        string absolutePath = Path.Combine(_helper.DirectoryPath, relativePath);
        return File.Exists(absolutePath); // Inconsistent with LoadData
    }
    // Multiple exception handlers that may not be triggered
    catch (System.IO.DirectoryNotFoundException ex)
    {
        _monitor.Log($"Directory not found while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
        return false;
    }
    // ... more exception handlers
}

// Improved consistent implementation
public bool DataExists(string key)
{
    string sanitizedKey;
    try
    {
        sanitizedKey = GetValidatedAndSanitizedKey(key);
    }
    catch (ArgumentException ex)
    {
        _monitor.Log($"Invalid key provided to DataExists: {ex.Message}", LogLevel.Warn);
        return false;
    }

    try
    {
        string relativePath = GetFilePath(sanitizedKey);
        
        // Consistent with LoadData - use SMAPI's API directly
        var result = _helper.Data.ReadJsonFile<object>(relativePath);
        
        // If result is not null, data exists
        return result != null;
    }
    // Minimal, necessary exception handling only
    catch (System.IO.FileNotFoundException)
    {
        return false; // File not found, so data doesn't exist
    }
    catch (System.UnauthorizedAccessException)
    {
        return false; // Access denied, assume data doesn't exist
    }
}
```

## SOLID Principles Application

### Single Responsibility Principle
- Each method should have one clear purpose
- Separate concerns: validation, sanitization, and data access

### Open/Closed Principle
- Design for extension without modification
- Use dependency injection for testability

### Liskov Substitution Principle
- Derived classes should be substitutable for base classes
- Maintain consistent behavior across implementations

### Interface Segregation Principle
- Create focused interfaces
- Avoid "fat" interfaces that clients don't need

### Dependency Inversion Principle
- Depend on abstractions, not concretions
- Already implemented in current architecture

## TDD Implementation Strategy

### Phase 1: Extension Handling Security
1. Write failing tests for extension handling edge cases
2. Implement improved `FindExtensionStartIndex` method
3. Refactor `Sanitize` method to use improved extension logic
4. Verify all tests pass

### Phase 2: Path Validation Simplification
1. Write tests for valid paths that should not be blocked
2. Simplify `ValidatePathTraversalDepth` method
3. Remove redundant logic
4. Verify security is maintained while reducing false positives

### Phase 3: Data Access Consistency
1. Write tests for `DataExists` behavior consistency
2. Refactor to use SMAPI's API consistently
3. Remove misleading exception handlers
4. Ensure all data access methods follow the same pattern

### Phase 4: Error Handling Consistency
1. Write tests for consistent error handling
2. Standardize error handling patterns across methods
3. Add defensive null checks
4. Verify all edge cases are handled properly

## Security Considerations

### Defense in Depth
- Multiple layers of validation
- Input sanitization at boundaries
- Output encoding where appropriate

### Principle of Least Privilege
- Use SMAPI's secure file access methods
- Validate inputs before processing
- Sanitize outputs before logging

### Fail-Safe Defaults
- Return false for uncertain cases in `DataExists`
- Throw exceptions for invalid inputs
- Log security-relevant events

## Testing Strategy

### Unit Tests
- Test each method in isolation
- Verify security edge cases
- Test error handling paths

### Integration Tests
- Test interactions between components
- Verify end-to-end functionality
- Test security scenarios

### Security Tests
- Test for path traversal vulnerabilities
- Test extension bypass attempts
- Test TOCTOU race conditions

## Implementation Order

1. **Security Critical**: Extension handling fixes
2. **Simplification**: Path validation logic
3. **Consistency**: Data access methods
4. **Robustness**: Error handling
5. **Test Improvements**: Reduce reflection usage

## Success Metrics

- All existing functionality preserved
- Security vulnerabilities addressed
- Performance maintained or improved
- Test coverage maintained or increased
- Code complexity reduced where possible
- Consistent behavior across methods