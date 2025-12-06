# Functionality Verification Plan - Cross-Platform Path Separator Tests

## Overview
This document outlines how to ensure tests verify functionality rather than platform-specific implementation details when handling path separators.

## Core Philosophy

### Functionality-First Testing Approach
- **Test behavior, not implementation**: Focus on what the system does, not how it does it on specific platforms
- **Validate outcomes, not paths**: Verify that validation results are correct regardless of separator format
- **Security-first mindset**: Ensure security validations work consistently across platforms

## Test Design Principles

### 1. Behavior-Based Test Assertions
Instead of asserting specific path formats, verify functional outcomes:

```csharp
// BAD: Platform-specific assertion
Assert.Equal("folder\\file.txt", result); // Windows-specific

// GOOD: Functional assertion
Assert.True(PathTestHelper.PathsEqual("folder/file.txt", result));
```

### 2. Semantic Validation Testing
Focus on the meaning and security implications of paths rather than their string representation:

```csharp
// BAD: Testing string format
Assert.Equal("../file.txt", sanitizedPath);

// GOOD: Testing functional outcome
Assert.DoesNotContain("../", sanitizedPath); // Ensures no traversal
```

### 3. Cross-Platform Outcome Verification
Test that the same functional outcomes occur regardless of platform:

```csharp
// GOOD: Verifies security behavior, not format
[Theory]
[InlineData("../dangerous/path")]
[InlineData("..\\dangerous\\path")]
public void Validate_PathTraversal_BothFormats_SecurityOutcome(string path)
{
    // Test that security validation works regardless of separator format
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
    Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
}
```

## Specific Test Patterns

### 1. Security Validation Tests
Focus on security outcomes rather than path formats:

```csharp
[Theory]
[InlineData("../file.txt", true)]   // Should be rejected (security)
[InlineData("..\\file.txt", true)]  // Should be rejected (security)
[InlineData("safe/file.txt", false)] // Should be accepted (security)
[InlineData("safe\\file.txt", false)] // Should be accepted (security)
public void Validate_PathSecurity_OutcomeConsistent(string path, bool shouldReject)
{
    if (shouldReject)
    {
        var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
        Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
    }
    else
    {
        var ex = Record.Exception(() => _service.Validate(path));
        Assert.Null(ex); // No exception means accepted
    }
}
```

### 2. Path Traversal Detection Tests
Test the detection capability, not the specific format:

```csharp
[Theory]
[InlineData("../file.txt")]
[InlineData("..\\file.txt")]
[InlineData("../../deep/path")]
[InlineData("..\\..\\deep\\path")]
public void Validate_PathTraversalDetection_Functionality(string traversalPath)
{
    // Verify that traversal detection works regardless of separator format
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate(traversalPath));
    Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
}
```

### 3. Valid Path Acceptance Tests
Test that valid paths are accepted, regardless of separator format:

```csharp
[Theory]
[InlineData("folder/file.txt")]
[InlineData("folder\\file.txt")]
[InlineData("deep/nested/structure/file.json")]
[InlineData("deep\\nested\\structure\\file.json")]
public void Validate_ValidPath_AcceptanceFunctionality(string validPath)
{
    // Verify that valid paths are accepted regardless of separator format
    var ex = Record.Exception(() => _service.Validate(validPath));
    Assert.Null(ex); // No exception means path was accepted
}
```

### 4. Depth Calculation Tests
Test the functional depth calculation, not the path string format:

```csharp
[Theory]
[InlineData("folder/../file.txt", 1)]    // Goes down 1, up 1, down 1 = depth 1
[InlineData("folder\\..\\file.txt", 1)]  // Same logic, different separators
[InlineData("a/b/../../c", 1)]           // Goes down 2, up 2, down 1 = depth 1
[InlineData("a\\b\\..\\..\\c", 1)]       // Same logic, different separators
public void Validate_PathDepthCalculation_FunctionalOutcome(string path, int expectedDepth)
{
    // This test would require a method to get the calculated depth
    // For now, we verify that valid paths (not going above root) are accepted
    var ex = Record.Exception(() => _service.Validate(path));
    Assert.Null(ex); // Path should be valid (not go above root)
}
```

## Anti-Patterns to Avoid

### 1. Platform-Specific Assertions
```csharp
// AVOID: Platform-specific string comparisons
Assert.Equal("folder\\file.txt", result); // Windows-specific

// PREFER: Platform-agnostic comparisons
Assert.True(PathTestHelper.PathsEqual("folder/file.txt", result));
```

### 2. Separator-Dependent Logic
```csharp
// AVOID: Logic that depends on specific separators
if (path.Contains("\\"))
{
    // Windows-specific handling
}

// PREFER: Format-agnostic logic
var normalizedPath = PathTestHelper.NormalizePathSeparators(path);
// Handle normalized path
```

### 3. Format-Dependent Mock Expectations
```csharp
// AVOID: Mock expecting specific format
_mockService.Verify(x => x.Process("folder\\file.txt"), Times.Once);

// PREFER: Format-agnostic verification
_mockService.Verify(x => x.Process(It.Is<string>(p => 
    PathTestHelper.PathsEqual(p, "folder/file.txt"))), Times.Once);
```

## Test Coverage for Functional Verification

### 1. Security Functionality Coverage
- **Path traversal detection**: Works with both separator formats
- **Absolute path detection**: Identifies absolute paths regardless of format
- **URI detection**: Recognizes URIs regardless of separator format
- **Encoded traversal detection**: Works with encoded separators

### 2. Validation Functionality Coverage
- **Valid path acceptance**: Accepts valid paths with any separator format
- **Invalid path rejection**: Rejects invalid paths regardless of format
- **Edge case handling**: Handles edge cases consistently across platforms
- **Performance**: Maintains performance characteristics across platforms

### 3. Integration Functionality Coverage
- **Service integration**: Verifies that services work together correctly
- **Data flow**: Ensures data flows correctly through the system
- **Error handling**: Validates consistent error handling across platforms
- **Security boundaries**: Ensures security boundaries are maintained

## Verification Techniques

### 1. Property-Based Testing
Test properties that should hold regardless of path format:

```csharp
[Theory]
[InlineData("safe/path")]
[InlineData("safe\\path")]
public void Validate_Property_SafePathAlwaysAccepted(string path)
{
    // Property: All safe paths should be accepted regardless of format
    var ex = Record.Exception(() => _service.Validate(path));
    Assert.Null(ex);
}

[Theory]
[InlineData("../dangerous")]
[InlineData("..\\dangerous")]
public void Validate_Property_DangerousPathAlwaysRejected(string path)
{
    // Property: All dangerous paths should be rejected regardless of format
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
    Assert.Contains("cannot contain path traversal", exception.Message);
}
```

### 2. Equivalence Class Testing
Group paths by functional behavior rather than format:

```csharp
public class PathEquivalenceTests
{
    // Equivalence class: All traversal paths should be rejected
    public static TheoryData<string> TraversalPaths()
    {
        var data = new TheoryData<string>();
        data.Add("../file.txt");        // Forward slash traversal
        data.Add("..\\file.txt");      // Backslash traversal
        data.Add("../../file.txt");     // Forward slash deep traversal
        data.Add("..\\..\\file.txt");  // Backslash deep traversal
        return data;
    }
    
    [Theory]
    [MemberData(nameof(TraversalPaths))]
    public void Validate_TraversalPaths_AllRejected(string path)
    {
        var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
        Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
    }
}
```

### 3. Boundary Value Testing
Test functional boundaries rather than format boundaries:

```csharp
[Theory]
[InlineData("folder/../file.txt", true)]   // Valid: goes to root level but not above
[InlineData("folder\\..\\file.txt", true)] // Valid: same logic, different format
[InlineData("folder/../../file.txt", false)]   // Invalid: goes above root
[InlineData("folder\\..\\..\\file.txt", false)] // Invalid: same logic, different format
public void Validate_PathBoundary_FunctionalOutcome(string path, bool shouldAccept)
{
    if (shouldAccept)
    {
        var ex = Record.Exception(() => _service.Validate(path));
        Assert.Null(ex);
    }
    else
    {
        var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
        Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
    }
}
```

## Quality Assurance for Functional Verification

### 1. Test Review Checklist
- [ ] Tests verify functional outcomes, not implementation details
- [ ] Path format variations are handled consistently
- [ ] Security validations work regardless of separator format
- [ ] No platform-specific assumptions in test logic
- [ ] Assertions are format-agnostic

### 2. Cross-Platform Validation
- [ ] Tests pass on Windows with backslash paths
- [ ] Tests pass on Unix-like systems with forward slash paths
- [ ] Functional outcomes are consistent across platforms
- [ ] Security behavior is identical on all platforms

### 3. Security Verification
- [ ] Path traversal detection works with all separator formats
- [ ] No security bypasses introduced by format handling
- [ ] All validation rules apply consistently
- [ ] Error messages remain appropriate regardless of format

## Implementation Guidelines

### 1. Use Format-Agnostic Assertions
```csharp
// Create helper methods for format-agnostic testing
public static class FunctionalAssertionHelper
{
    public static void PathsAreEquivalent(string expected, string actual)
    {
        Assert.True(PathTestHelper.PathsEqual(expected, actual), 
            $"Expected {expected}, but got {actual}");
    }
    
    public static void ValidateSecurityOutcome(string path, bool shouldReject)
    {
        if (shouldReject)
        {
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
            Assert.Contains("Path cannot", exception.Message);
        }
        else
        {
            var ex = Record.Exception(() => _service.Validate(path));
            Assert.Null(ex);
        }
    }
}
```

### 2. Focus on Business Rules
```csharp
// Test business rules, not technical implementation
[Theory]
[InlineData("../traversal", false)]  // Business rule: traversal not allowed
[InlineData("..\\traversal", false)] // Same business rule, different format
[InlineData("safe/path", true)]      // Business rule: safe paths allowed
[InlineData("safe\\path", true)]     // Same business rule, different format
public void Validate_BusinessRule_PathTraversalNotAllowed(string path, bool shouldAccept)
{
    // Test the business rule, not the implementation detail
    FunctionalAssertionHelper.ValidateSecurityOutcome(path, !shouldAccept);
}
```

### 3. Maintain Security Focus
```csharp
// Always prioritize security functionality over format details
[Theory]
[InlineData("%2e%2e%2f")]  // Encoded traversal forward slash
[InlineData("%2e%2e%5c")]  // Encoded traversal backslash
public void Validate_EncodedTraversal_SecurityFunctionality(string encodedPath)
{
    // Focus on security detection, not encoding format
    var exception = Assert.Throws<ArgumentException>(() => _service.Validate(encodedPath));
    Assert.Contains("encoded path traversal", exception.Message);
}
```

This approach ensures that tests verify the actual functionality and security behavior of the path validation system rather than getting distracted by platform-specific implementation details like path separator formats.