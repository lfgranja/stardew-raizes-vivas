# Test Coverage Maintenance Plan - Cross-Platform Path Separator Compatibility

## Overview
This document outlines how to maintain all existing test coverage while improving cross-platform compatibility for path separator handling in tests.

## Current Test Coverage Analysis

### Path Validation Coverage
- **Path traversal detection**: Tests for `../`, `..\\`, multiple dots, etc.
- **Absolute path detection**: Tests for Unix `/`, Windows `C:\\`, etc.
- **URI detection**: Tests for `http://`, `https://`, etc.
- **Encoded traversal**: Tests for URL-encoded and Unicode-encoded paths
- **Dot segment handling**: Tests for `./`, `.\\`, `.` at various positions
- **Security validation**: Tests for path traversal bypass attempts

### Current Test Scenarios
1. **Valid paths**: Relative paths with various formats
2. **Invalid paths**: Path traversal attempts with different patterns
3. **Edge cases**: Single dots, multiple dots, mixed formats
4. **Security scenarios**: Encoded traversal, homoglyph attacks
5. **Platform-specific**: Windows and Unix-style paths

## Coverage Maintenance Strategy

### 1. Preserve All Existing Test Scenarios
- **Maintain validation logic coverage**: All current validation scenarios must remain
- **Keep security test coverage**: All security-related tests preserved
- **Retain edge case coverage**: All edge cases continue to be tested
- **Preserve negative test coverage**: All invalid path tests maintained

#### Implementation:
```csharp
// Instead of removing existing tests, enhance them for cross-platform compatibility
[Fact]
public void Validate_WithPathTraversal_ForwardSlash_ThrowsArgumentException()
{
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate("../file.txt"));
    Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
}

[Fact] 
public void Validate_WithPathTraversal_BackSlash_ThrowsArgumentException()
{
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate("..\\file.txt"));
    Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
}

// Or use parameterized test for both
[Theory]
[InlineData("../file.txt")]
[InlineData("..\\file.txt")]
public void Validate_WithPathTraversal_BothFormats_ThrowsArgumentException(string path)
{
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
    Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
}
```

### 2. Expand Coverage for Cross-Platform Scenarios
- **Dual-format testing**: Test both separator formats for each scenario
- **Mixed separator testing**: Test paths with mixed separators
- **Platform-specific edge cases**: Handle UNC paths, drive letters appropriately

#### Implementation:
```csharp
// Ensure comprehensive coverage of both formats
[Theory]
[InlineData("valid/path")]
[InlineData("valid\\path")]
public void Validate_WithValidPath_BothFormats_DoesNotThrow(string path)
{
    var ex = Record.Exception(() => _service.Validate(path));
    Assert.Null(ex);
}

// Test mixed separators which could occur in real scenarios
[Theory]
[InlineData("valid/path\\with/mixed/separators")]
[InlineData("another\\example/with\\mixed")]
public void Validate_WithMixedSeparators_DoesNotThrow(string path)
{
    var ex = Record.Exception(() => _service.Validate(path));
    Assert.Null(ex);
}
```

### 3. Maintain Security Coverage
- **All traversal patterns**: Continue testing all known traversal patterns
- **Encoded patterns**: Maintain coverage for URL and Unicode encoding
- **Homoglyph detection**: Keep Unicode dot homoglyph tests
- **Bypass attempts**: Preserve tests for various bypass techniques

#### Implementation:
```csharp
// Security tests for both formats
[Theory]
[InlineData("../../file.txt")]
[InlineData("..\\..\\file.txt")]
public void Validate_PathTraversalMultipleLevels_BothFormats_Throws(string path)
{
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
    Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
}

// Continue encoded traversal testing
[Fact]
public void Validate_WithEncodedPathTraversal_ThrowsArgumentException()
{
    // This test remains unchanged as it's not separator-dependent
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate("%2e%2e%2f"));
    Assert.Contains("Path cannot contain encoded path traversal patterns", exception.Message);
}
```

## Coverage Mapping Strategy

### 1. Test Scenario Mapping
Create a mapping of current tests to ensure no scenarios are lost:

| Original Test | Cross-Platform Version | Coverage Maintained |
|---------------|------------------------|-------------------|
| `Validate_WithNullPath_ThrowsArgumentException` | Unchanged (no path separators) | ✓ |
| `Validate_WithPathTraversal_DotDot_ThrowsArgumentException` | Dual-format version | ✓ |
| `Validate_WithWindowsAbsolutePath_ThrowsArgumentException` | Keep Windows-specific | ✓ |
| `Validate_WithUnixAbsolutePath_ThrowsArgumentException` | Keep Unix-specific | ✓ |

### 2. Security Test Preservation
- **Path traversal detection**: All patterns maintained
- **Absolute path detection**: Both Windows and Unix formats
- **URI detection**: All protocols and formats
- **Encoded patterns**: All encoding variations

### 3. Edge Case Preservation
- **Single dot validation**: `.` scenarios maintained
- **Dot slash validation**: `./` and `.\` scenarios
- **Dot dot slash validation**: `../` and `..\\` scenarios
- **Complex combinations**: Multi-level traversals

## Implementation Approach

### Phase 1: Inventory Current Coverage
```csharp
// Document all existing test scenarios
public class TestCoverageInventory
{
    // Path validation scenarios
    - Null/empty path validation
    - Path traversal detection (../, ..\\)
    - Absolute path detection (/, C:\\)
    - URI detection (http://, https://)
    - Encoded traversal (%2e%2e%2f)
    - Dot segment handling (./, .\\, .)
    - Mixed scenarios and edge cases
}
```

### Phase 2: Map to Cross-Platform Versions
```csharp
// Create mapping for each test to cross-platform equivalent
public class TestMapping
{
    public Dictionary<string, string[]> CoverageMap = new Dictionary<string, string[]>
    {
        {
            "Validate_WithPathTraversal_DotDot_ThrowsArgumentException",
            new[] { 
                "Validate_WithPathTraversal_ForwardSlash_ThrowsArgumentException",
                "Validate_WithPathTraversal_BackSlash_ThrowsArgumentException" 
            }
        },
        // Additional mappings...
    };
}
```

### Phase 3: Implementation with Coverage Verification
```csharp
// Ensure coverage is maintained during implementation
public class CoverageVerification
{
    public void VerifyAllScenariosCovered()
    {
        // Verify that all original test scenarios are covered
        // in the new cross-platform implementation
    }
}
```

## Quality Assurance for Coverage Maintenance

### 1. Coverage Metrics
- **Statement coverage**: Ensure all code paths remain covered
- **Branch coverage**: Verify all conditional branches tested
- **Path coverage**: Maintain coverage of different execution paths

### 2. Security Coverage Verification
- **All traversal patterns**: Verify each pattern is tested with both separators
- **All encoding methods**: Ensure encoded traversal detection works
- **All homoglyphs**: Verify Unicode dot detection works with both separators

### 3. Regression Testing
- **Full test suite execution**: Run all tests before and after changes
- **Security validation**: Verify all security checks still function
- **Performance impact**: Ensure no performance degradation

## Specific Coverage Maintenance Techniques

### 1. Parameterized Testing for Dual Formats
```csharp
// Maintain coverage while reducing duplication
[Theory]
[MemberData(nameof(PathTraversalTestCases))]
public void Validate_PathTraversal_CrossPlatform_Throws(string path)
{
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
    Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
}

public static TheoryData<string> PathTraversalTestCases()
{
    var data = new TheoryData<string>();
    data.Add("../file.txt");
    data.Add("..\\file.txt");
    data.Add("../../deep/path");
    data.Add("..\\..\\deep\\path");
    return data;
}
```

### 2. Comprehensive Scenario Testing
```csharp
// Ensure all scenarios are covered with both formats
public class ComprehensivePathValidationTests
{
    [Theory]
    [InlineData("valid/path", true)]
    [InlineData("valid\\path", true)]
    [InlineData("../invalid", false)]
    [InlineData("..\\invalid", false)]
    public void Validate_PathWithFormat_ExpectedOutcome(string path, bool shouldPass)
    {
        if (shouldPass)
        {
            var ex = Record.Exception(() => _service.Validate(path));
            Assert.Null(ex);
        }
        else
        {
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
            Assert.Contains("Path cannot", exception.Message);
        }
    }
}
```

### 3. Security-Specific Coverage
```csharp
// Maintain security coverage with cross-platform approach
[Theory]
[InlineData("..%2f..%2f", true)]  // Encoded traversal with forward slash
[InlineData("..%5c..%5c", true)]  // Encoded traversal with backslash encoding
[InlineData("..\\../", true)]     // Mixed encoding
[InlineData("../..\\", true)]     // Mixed encoding
public void Validate_EncodedTraversal_CrossPlatform_Throws(string path, bool shouldThrow)
{
    if (shouldThrow)
    {
        var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
        Assert.Contains("Path cannot contain encoded path traversal patterns", exception.Message);
    }
}
```

## Verification Checklist

### Pre-Implementation
- [ ] Inventory all existing test scenarios
- [ ] Document security test coverage
- [ ] Identify edge cases and special scenarios
- [ ] Create mapping plan for cross-platform versions

### During Implementation
- [ ] Implement dual-format versions of all path-dependent tests
- [ ] Preserve all security validation scenarios
- [ ] Maintain edge case coverage
- [ ] Use helper methods to reduce duplication

### Post-Implementation
- [ ] Verify all original scenarios are covered
- [ ] Run complete test suite to ensure no regressions
- [ ] Confirm security validations still work
- [ ] Validate performance impact is minimal
- [ ] Document any new test scenarios added

## Risk Mitigation

### 1. Coverage Loss Prevention
- **Before/after comparison**: Compare test coverage before and after changes
- **Automated checks**: Use coverage tools to verify no code paths are missed
- **Manual review**: Review all test scenarios to ensure comprehensive coverage

### 2. Security Gap Prevention
- **Security-focused review**: Special attention to security test coverage
- **Penetration testing**: Verify no new attack vectors are introduced
- **Validation verification**: Ensure all security validations remain effective

### 3. Regression Prevention
- **Comprehensive testing**: Run full test suite on multiple platforms
- **Incremental changes**: Implement changes gradually with testing at each step
- **Rollback plan**: Maintain ability to revert if coverage is compromised

This plan ensures that all existing test coverage is maintained while adding cross-platform compatibility, with special attention to preserving security validation and edge case handling.