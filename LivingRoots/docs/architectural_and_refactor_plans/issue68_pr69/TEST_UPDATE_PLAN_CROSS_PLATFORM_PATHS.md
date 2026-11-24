# Test Update Plan - Cross-Platform Path Differences

## Overview
This document outlines the specific approach for updating tests to handle cross-platform path differences properly, ensuring consistent behavior across Windows, Linux, and Mac platforms.

## Core Strategy

### 1. Path Normalization Approach
Implement a consistent path normalization strategy across all tests:

```csharp
public static class PathTestHelper
{
    /// <summary>
    /// Normalizes path separators to forward slashes for consistent cross-platform comparisons
    /// </summary>
    public static string NormalizePathSeparators(string path)
    {
        return path?.Replace('\\', '/') ?? string.Empty;
    }
    
    /// <summary>
    /// Compares two paths for equality after normalizing separators
    /// </summary>
    public static bool PathsEqual(string path1, string path2)
    {
        return NormalizePathSeparators(path1) == NormalizePathSeparators(path2);
    }
    
    /// <summary>
    /// Creates test data with both forward and backslash separator formats
    /// </summary>
    public static string[] GetDualFormatPaths(string basePath)
    {
        return new[]
        {
            basePath,                    // Forward slash format
            basePath.Replace('/', '\\')  // Backslash format
        };
    }
}
```

### 2. Dual-Format Testing Strategy
For validation tests, explicitly test both separator formats:

```csharp
[Fact]
public void Validate_WithPathTraversal_BothSeparators_ThrowsArgumentException()
{
    // Test forward slash format
    var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("../file.txt"));
    Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
    
    // Test backslash format
    var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("..\\file.txt"));
    Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
}
```

## Detailed Update Plans by Test File

### 1. PathValidationServiceTests.cs

#### Current Issues
- 21 instances of hardcoded backslashes
- Mixed approach to path validation testing

#### Update Strategy
1. **Replace hardcoded backslash tests with dual-format tests**:
   ```csharp
   // Instead of:
   var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("..\\file.txt"));
   
   // Use:
   var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("../file.txt"));
   var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("..\\file.txt"));
   ```

2. **Update path comparison assertions**:
   ```csharp
   // Instead of:
   Assert.Equal("folder\\file.txt", result);
   
   // Use:
   Assert.Equal("folder/file.txt", PathTestHelper.NormalizePathSeparators(result));
   ```

3. **Create parameterized tests for separator formats**:
   ```csharp
   [Theory]
   [InlineData("../file.txt")]
   [InlineData("..\\file.txt")]
   public void Validate_WithPathTraversal_EitherSeparator_ThrowsArgumentException(string path)
   {
       var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
       Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
   }
   ```

### 2. PathTraversalValidatorDomainTests.cs

#### Current Issues
- 2 instances of hardcoded backslashes in validation tests

#### Update Strategy
1. **Create dual-format validation tests**:
   ```csharp
   [Fact]
   public void Validate_WithPathTraversal_BothFormats_ThrowsArgumentException()
   {
       // Test forward slash
       var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("../file.txt"));
       Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
       
       // Test backslash
       var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("..\\file.txt"));
       Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
   }
   ```

### 3. DotSegmentTests.cs

#### Current Issues
- 6 instances of hardcoded backslashes
- Path.Combine usage with potential assertion problems

#### Update Strategy
1. **Update Path.Combine assertions**:
   ```csharp
   // Instead of:
   _validator.Validate(Path.Combine("folder", ".hidden"));
   
   // Use:
   string path = Path.Combine("folder", ".hidden");
   // The validation should work regardless of separator format
   _validator.Validate(path);
   ```

2. **Create helper methods for consistent testing**:
   ```csharp
   private void ValidatePathWithBothSeparators(string relativePath)
   {
       string[] formats = PathTestHelper.GetDualFormatPaths(relativePath);
       foreach (string format in formats)
       {
           var ex = Record.Exception(() => _validator.Validate(format));
           Assert.Null(ex);
       }
   }
   ```

### 4. FileNameExtensionHelperTests.cs

#### Current Issues
- Mixed separator formats in tests

#### Update Strategy
1. **Ensure both separator formats work for extension extraction**:
   ```csharp
   [Fact]
   public void GetFileExtension_WithMixedSeparators_ReturnsCorrectExtension()
   {
       // Test forward slash
       Assert.Equal("txt", GetFileExtensionTest(sanitizationService, "file.txt/extra"));
       // Test backslash
       Assert.Equal("txt", GetFileExtensionTest(sanitizationService, "file.txt\\extra"));
   }
   ```

### 5. FileNameSanitizationServiceTests.cs

#### Current Issues
- Mock setups with hardcoded backslashes
- Assertions expecting specific backslash formats

#### Update Strategy
1. **Update mock setups for both formats**:
   ```csharp
   // Instead of single format setup:
   _mockUnicodeNormalizationService
       .Setup(x => x.Normalize("..\\test"))
       .Returns("..\\test");
   
   // Use dual format setup:
   _mockUnicodeNormalizationService
       .Setup(x => x.Normalize(It.IsAny<string>()))
       .Returns<string>(input => 
       {
           if (input == "../test" || input == "..\\test")
               return input;
           return input; // default behavior
       });
   ```

2. **Update assertions to use normalized comparisons**:
   ```csharp
   // Instead of:
   Assert.NotEqual("..\\test", result2);
   
   // Use:
   Assert.NotEqual("../test", PathTestHelper.NormalizePathSeparators(result2));
   ```

### 6. ReservedNameHandlerTests.cs

#### Current Issues
- Path.Combine usage with potential assertion problems
- UNC path tests with hardcoded separators

#### Update Strategy
1. **Normalize Path.Combine results for assertions**:
   ```csharp
   // Instead of:
   Assert.Equal(Path.Combine("C:", "CON_.txt"), result);
   
   // Use:
   string expected = Path.Combine("C:", "CON_.txt");
   Assert.True(PathTestHelper.PathsEqual(expected, result));
   ```

2. **Handle UNC paths specially**:
   ```csharp
   [Fact]
   public void ProcessPath_WithUNCPathWithReservedName_HandlesCorrectly()
   {
       // UNC paths have specific format requirements
       string input = @"\\server\share\PRN.log";
       string result = _handler.ProcessPath(input);
       
       // For UNC paths, normalize but preserve the double backslash at start
       Assert.StartsWith(@"\\", result);
       Assert.EndsWith(@"PRN_.log", result); // The reserved name should be handled
   }
   ```

### 7. ModDataServiceTests.cs and DotSegmentAllowanceTest.cs

#### Current Issues
- Path search logic that may miss patterns on different platforms

#### Update Strategy
1. **Update search logic to handle both separators**:
   ```csharp
   // Instead of:
   s.Contains("..\\") ||
   s.Contains("..\\..\\") ||
   
   // Use normalized search:
   PathTestHelper.NormalizePathSeparators(s).Contains("../") ||
   PathTestHelper.NormalizePathSeparators(s).Contains("../../") ||
   ```

2. **Or update to search for both formats explicitly**:
   ```csharp
   s.Contains("../") || s.Contains("..\\") ||
   s.Contains("../../") || s.Contains("..\\..\\") ||
   ```

## Implementation Phases

### Phase 1: Helper Method Creation (Days 1-2)
- Create PathTestHelper class with normalization methods
- Add dual-format path generation methods
- Document helper usage

### Phase 2: High-Priority Test Updates (Days 3-5)
- Update PathValidationServiceTests.cs
- Update PathTraversalValidatorDomainTests.cs
- Update DotSegmentTests.cs

### Phase 3: Medium-Priority Test Updates (Days 6-7)
- Update FileNameExtensionHelperTests.cs
- Update FileNameSanitizationServiceTests.cs
- Update ReservedNameHandlerTests.cs

### Phase 4: Low-Priority and Special Cases (Days 8-9)
- Update ModDataServiceTests.cs
- Update DotSegmentAllowanceTest.cs
- Handle UNC path special cases

### Phase 5: Verification and Testing (Days 10-11)
- Run tests on multiple platforms
- Verify all security validations still work
- Perform regression testing

## Quality Assurance Measures

### 1. Cross-Platform Testing
- Test on Windows with backslash paths
- Test on Linux/Mac with forward slash paths
- Verify consistent behavior across platforms

### 2. Security Validation
- Ensure path traversal detection works with both separator formats
- Verify no security bypasses are introduced
- Test edge cases with mixed separators

### 3. Performance Considerations
- Path normalization should be efficient
- Minimal impact on test execution time
- No complex path manipulation algorithms

### 4. Regression Prevention
- All existing functionality must remain intact
- Security validations must continue to work
- No breaking changes to the API

## Risk Mitigation

### 1. Gradual Implementation
- Implement changes incrementally
- Test after each file update
- Maintain working state throughout

### 2. Backup Strategy
- Keep original files in version control
- Use feature branch for development
- Easy rollback if issues arise

### 3. Comprehensive Testing
- Unit tests for helper methods
- Integration tests for path validation
- Cross-platform verification

This plan ensures that all tests properly handle cross-platform path differences while maintaining all existing functionality and security validations.
