# Test Update Plan for PathValidationService Error Message Changes

## Overview
This document outlines the plan to update tests that depend on the specific error message "Path cannot contain path traversal patterns" in the MaxSegments case, which will be changed to "Path contains too many segments".

## Specific Test That Needs Updating

### PathValidationServiceTests.cs - Validate_WithExcessivePathSegments_ThrowsArgumentException
- **Location**: Lines 457-466 in `LivingRoots.Tests/PathValidationServiceTests.cs`
- **Current assertion**: `Assert.Contains("Path cannot contain path traversal patterns", exception.Message);`
- **Required update**: Change to `Assert.Contains("Path contains too many segments", exception.Message);`

## Test Update Strategy

### 1. Primary Test Update
```csharp
[Fact]
public void Validate_WithExcessivePathSegments_ThrowsArgumentException()
{
    // Create a path with more segments than MaxSegments (1000) to test the hard cap
    string path = string.Join("/", new string[1001].Select((_, i) => $"dir{i}"));
    
    // Act & Assert - This should throw because it exceeds MaxSegments
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
    Assert.Contains("Path contains too many segments", exception.Message); // Updated message
}
```

### 2. All Other Tests Remain Unchanged
All other tests that verify "Path cannot contain path traversal patterns" should remain unchanged as they represent actual security-related path traversal scenarios, not the MaxSegments performance limit scenario.

### 3. Tests That Should NOT Be Updated
The following tests continue to properly use "Path cannot contain path traversal patterns" because they represent actual security violations:

- Validate_WithPathTraversal_DotDot_ThrowsArgumentException
- Validate_WithPathTraversal_DotDotPlatformSpecific_ThrowsArgumentException
- Validate_WithDotSlashAtStart_ThrowsArgumentException
- Validate_WithMultipleDotDot_ThrowsArgumentException
- Validate_WithStandaloneDot_ThrowsArgumentException
- Validate_WithDotSlash_ThrowsArgumentException
- Validate_WithDotDotSlashAtStart_ThrowsArgumentException
- Validate_WithDepthTraversal_GoesNegative_ThrowsArgumentException
- Validate_WithPathTraversalAboveRoot_StillThrowsArgumentException
- Validate_WithPlatformSpecificPathTraversal_ThrowsArgumentException
- Validate_WithUnicodeDotHomoglyphs_PathTraversal_ThrowsArgumentException
- Validate_WithMultipleUnicodeDotHomoglyphs_PathTraversal_ThrowsArgumentException
- Validate_WithMixedPathSeparatorsAndUnicodeDots_PathTraversal_ThrowsArgumentException

## Implementation Steps

### Phase 1: Update the Specific Test
1. Locate the `Validate_WithExcessivePathSegments_ThrowsArgumentException` test in `PathValidationServiceTests.cs`
2. Update the assertion from `"Path cannot contain path traversal patterns"` to `"Path contains too many segments"`

### Phase 2: Add New Test for Boundary Condition
```csharp
[Fact]
public void Validate_WithMaxSegments_DoesNotThrow()
{
    // Create a path with exactly MaxSegments (1000) to test boundary condition
    string path = string.Join("/", new string[1000].Select((_, i) => $"dir{i}"));
    
    // Act & Assert - This should NOT throw since it's exactly at the limit
    _service.Validate(path); // Should not throw
}
```

### Phase 3: Verify Test Updates
1. Run the updated test to ensure it passes with the new error message
2. Run all other tests to ensure they still pass with the original security-related messages
3. Confirm that the MaxSegments functionality still works as expected

## Verification Checklist

- [ ] The `Validate_WithExcessivePathSegments_ThrowsArgumentException` test has been updated to expect the new message
- [ ] All other path traversal tests continue to expect "Path cannot contain path traversal patterns"
- [ ] New boundary test (MaxSegments exact limit) has been added
- [ ] All existing security tests continue to pass
- [ ] The updated test correctly identifies the MaxSegments scenario
- [ ] No other tests are affected by the change

## Risk Mitigation

### 1. Backward Compatibility
- Only one test is being updated, minimizing risk
- All security-related tests remain unchanged
- The exception type remains the same (ArgumentException)

### 2. Security Validation
- All security tests continue to verify the same security messages
- No security functionality is changed, only error message content
- Path traversal detection logic remains unchanged

### 3. Regression Prevention
- Run complete test suite after changes
- Verify that MaxSegments limit is still enforced
- Confirm that legitimate paths still work correctly