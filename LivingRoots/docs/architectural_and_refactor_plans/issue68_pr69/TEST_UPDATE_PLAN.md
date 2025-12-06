# Test Update Plan for ReservedNameHandler Refactoring

## 1. Overview

This document outlines the testing strategy and required updates to ensure the ReservedNameHandler refactoring maintains all functionality while using .NET's built-in Path.GetFileName and Path.GetDirectoryName methods. The refactoring should not require changes to existing test expectations since the behavior remains identical.

## 2. Test Impact Assessment

### 2.1. No Expected Behavior Changes
Since the refactoring only changes the internal path parsing mechanism while preserving all external behavior, **no test updates should be necessary**. All existing test cases should continue to pass with identical results.

### 2.2. Test Categories Analysis
- **Reserved name detection tests**: No changes needed (behavior unchanged)
- **Extension handling tests**: No changes needed (behavior unchanged) 
- **UNC path tests**: No changes needed (behavior unchanged)
- **Unicode normalization tests**: No changes needed (behavior unchanged)
- **Edge case tests**: No changes needed (behavior unchanged)
- **Security-focused tests**: No changes needed (behavior unchanged)

## 3. Required Test Updates

### 3.1. Minimal Code Updates
The only potential test update is to remove the dependency on the now-eliminated `IsUncPath` method if any tests directly accessed it (which is unlikely since it's a private method).

### 3.2. Test Coverage Enhancement
Add new tests to specifically validate that the .NET built-in methods are properly handling various path formats:

```csharp
[Fact]
public void Handle_WithVariousPathFormats_UsesNetBuiltInMethodsCorrectly()
{
    // This test validates that the refactored implementation correctly
    // uses Path.GetFileName and Path.GetDirectoryName for all path types
    
    // UNC path
    string uncPath = @"\\server\share\CON.txt";
    string uncExpected = @"\\server\share\CON_.txt";
    Assert.Equal(uncExpected, _reservedNameHandler.Handle(uncPath));
    
    // Unix-style UNC path
    string unixUncPath = @"//server/share/CON.txt";
    string unixUncExpected = @"//server/share/CON_.txt";
    Assert.Equal(unixUncExpected, _reservedNameHandler.Handle(unixUncPath));
    
    // Local path
    string localPath = @"C:\folder\PRN.txt";
    string localExpected = @"C:\folder\PRN_.txt";
    Assert.Equal(localExpected, _reservedNameHandler.Handle(localPath));
    
    // Relative path
    string relativePath = @"folder\AUX.txt";
    string relativeExpected = @"folder\AUX_.txt";
    Assert.Equal(relativeExpected, _reservedNameHandler.Handle(relativePath));
    
    // Path with forward slashes
    string forwardSlashPath = @"folder/AUX.txt";
    string forwardSlashExpected = @"folder/AUX_.txt";
    Assert.Equal(forwardSlashExpected, _reservedNameHandler.Handle(forwardSlashPath));
}

[Fact]
public void Handle_WithComplexUNCPaths_HandlesCorrectly()
{
    // Test complex UNC paths to ensure .NET methods handle them properly
    string complexUncPath = @"\\server\share\folder1\folder2\COM1.tar.gz";
    string complexUncExpected = @"\\server\share\folder1\folder2\COM1_.tar.gz";
    Assert.Equal(complexUncExpected, _reservedNameHandler.Handle(complexUncPath));
}

[Fact]
public void Handle_WithNetworkPaths_HandlesCorrectly()
{
    // Test various network path formats
    string networkPath = @"\\?\C:\folder\NUL.txt";
    string networkExpected = @"\\?\C:\folder\NUL_.txt";
    Assert.Equal(networkExpected, _reservedNameHandler.Handle(networkPath));
}
```

## 4. Test Execution Strategy

### 4.1. Comprehensive Test Execution
Execute all existing tests to verify no regressions:

```bash
dotnet test --filter "FullyQualifiedName~ReservedNameHandlerTests"
```

### 4.2. Specific Test Categories to Run
- All existing ReservedNameHandler test methods
- Integration tests that depend on ReservedNameHandler
- Any tests that might indirectly use path validation functionality
- Security-focused tests that validate reserved name handling

### 4.3. Cross-Platform Test Validation
Since the refactored implementation relies on .NET built-ins, validate on multiple platforms:
- Windows (traditional UNC path format: `\\server\share`)
- Linux/Unix (URL-style UNC: `//server/share` or paths with forward slashes)
- macOS (with various path formats)

## 5. New Test Scenarios

### 5.1. Path Format Diversity Tests
Add tests to ensure various path formats are handled correctly:

```csharp
[Theory]
[InlineData(@"\\server\share\CON.txt", @"\\server\share\CON_.txt")]
[InlineData(@"//server/share/CON.txt", @"//server/share/CON_.txt")]
[InlineData(@"C:\folder\CON.txt", @"C:\folder\CON_.txt")]
[InlineData(@"folder/CON.txt", @"folder/CON_.txt")]
[InlineData(@"/absolute/path/CON.txt", @"/absolute/path/CON_.txt")]
public void Handle_WithDifferentPathFormats_HandlesReservedNames(string input, string expected)
{
    // Verify that all path formats are handled consistently
    Assert.Equal(expected, _reservedNameHandler.Handle(input));
}
```

### 5.2. Directory Path Preservation Tests
Validate that directory paths ending with separators are preserved:

```csharp
[Theory]
[InlineData(@"\\server\share\", @"\\server\share\")]
[InlineData(@"C:\folder\", @"C:\folder\")]
[InlineData(@"folder/", @"folder/")]
[InlineData(@"/absolute/path/", @"/absolute/path/")]
public void Handle_WithDirectoryPaths_ReturnsUnchanged(string input, string expected)
{
    // Verify that paths ending with separators (directories) are not modified
    Assert.Equal(expected, _reservedNameHandler.Handle(input));
}
```

### 5.3. Path Separator Handling Tests
Test that different separator formats are handled properly:

```csharp
[Theory]
[InlineData(@"folder\CON.txt", @"folder\CON_.txt")]
[InlineData(@"folder/CON.txt", @"folder/CON_.txt")]
[InlineData(@"path/to/PRN.log", @"path/to/PRN_.log")]
[InlineData(@"path\to\PRN.log", @"path\to\PRN_.log")]
public void Handle_WithPathSeparators_HandlesConsistently(string input, string expected)
{
    // Verify that both forward and backward slashes are handled consistently
    Assert.Equal(expected, _reservedNameHandler.Handle(input));
}
```

## 6. Performance Tests

### 6.1. Baseline Performance Comparison
Create performance tests to ensure no degradation:

```csharp
[Fact]
public void Handle_Performance_NoDegradation()
{
    // Performance test to ensure .NET built-ins don't cause degradation
    var sw = System.Diagnostics.Stopwatch.StartNew();
    
    for (int i = 0; i < 10000; i++)
    {
        _reservedNameHandler.Handle($"file{i % 100}.txt");
        _reservedNameHandler.Handle($"CON{i % 10}.txt");  // Some reserved names
        _reservedNameHandler.Handle($"\\server\\share\\file{i % 50}.txt"); // UNC paths
    }
    
    sw.Stop();
    
    // The refactored version should not be significantly slower
    // (Set appropriate threshold based on baseline measurements)
    Assert.True(sw.ElapsedMilliseconds < 1000, "Performance should not degrade significantly");
}
```

## 7. Security Tests

### 7.1. Homoglyph Attack Tests
Ensure homoglyph protection continues to work:

```csharp
[Fact]
public void Handle_WithHomoglyphUNCPaths_Protected()
{
    // Test homoglyph attacks with UNC paths
    string input = @"\\server\share\CОN"; // Cyrillic 'О'
    string expected = @"\\server\share\CON_"; // Should normalize to Latin and add underscore
    
    _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(@"\\server\share\CON");
    
    string? result = _reservedNameHandler.Handle(input);
    Assert.Equal(expected, result);
}
```

### 7.2. Multi-Extension Tests
Ensure multi-extension handling continues to work with various path formats:

```csharp
[Fact]
public void Handle_WithMultiExtensionUNCPaths_HandlesCorrectly()
{
    string input = @"\\server\share\COM1.tar.gz";
    string expected = @"\\server\share\COM1_.tar.gz";
    
    Assert.Equal(expected, _reservedNameHandler.Handle(input));
}
```

## 8. Regression Testing Strategy

### 8.1. Full Test Suite Execution
Execute the complete test suite to catch any unexpected impacts:

```bash
dotnet test --logger "trx" --results-directory ./test-results
```

### 8.2. Integration Test Validation
Run all integration tests that might be affected:

```bash
dotnet test --filter "FullyQualifiedName~DomainServiceIntegrationTests"
dotnet test --filter "FullyQualifiedName~PathValidationServiceTests"
```

### 8.3. Test Coverage Validation
Ensure test coverage is maintained:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutput=TestResults/ /p:CoverletOutputFormat=lcov
```

## 9. Test Data Updates

### 9.1. No Test Data Changes Required
Since the external behavior is unchanged, no test data updates are needed. All existing test inputs and expected outputs remain valid.

### 9.2. Additional Test Data for Path Diversity
Consider adding test data to validate the robustness of .NET built-in methods:

```csharp
public static TheoryData<string, string> PathFormatTestData()
{
    var data = new TheoryData<string, string>();
    
    // UNC paths
    data.Add(@"\\server\share\CON.txt", @"\\server\share\CON_.txt");
    data.Add(@"//server/share/CON.txt", @"//server/share/CON_.txt");
    
    // Various local paths
    data.Add(@"C:\folder\CON.txt", @"C:\folder\CON_.txt");
    data.Add(@"folder\CON.txt", @"folder\CON_.txt");
    data.Add(@"folder/CON.txt", @"folder/CON_.txt");
    
    // Paths with multiple segments
    data.Add(@"\\server\share\folder\CON.txt", @"\\server\share\folder\CON_.txt");
    data.Add(@"C:\path\to\folder\CON.txt", @"C:\path\to\folder\CON_.txt");
    
    return data;
}
```

## 10. Test Documentation Updates

### 10.1. Update Test Comments
Update any test comments that reference the old implementation details:

```csharp
// OLD COMMENT: "This test verifies the manual UNC path parsing logic"
// NEW COMMENT: "This test verifies UNC path handling using .NET built-in methods"

[Fact]
public void Handle_WithUNCPathAndReservedName_ProcessesFileNameComponent()
{
    // This test verifies UNC path handling using .NET built-in methods
    // The Path.GetFileName method correctly extracts the filename component
    // from UNC paths, and the reserved name handling logic remains unchanged
    
    string input = @"\\server\share\PRN.log";
    string fileNamePart = "PRN";
    
    _mockUnicodeNormalizationService.Setup(x => x.Normalize(fileNamePart)).Returns(fileNamePart);

    string? result = _reservedNameHandler.Handle(input);
    
    Assert.Equal(@"\\server\share\PRN_.log", result);
}
```

## 11. Test Environment Considerations

### 11.1. Cross-Platform Testing
Ensure tests run correctly on all target platforms since .NET path methods have platform-specific behaviors:

- Windows: UNC paths use `\\server\share` format
- Unix/Linux: UNC paths may use `//server/share` format
- Cross-platform: Forward slashes work on all platforms

### 11.2. .NET Version Compatibility
Verify the refactored implementation works across target .NET versions since Path.GetFileName and Path.GetDirectoryName are core .NET functionality available in all supported versions.

## 12. Test Verification Checklist

### 12.1. Pre-Implementation Verification
- [ ] All existing tests pass with current implementation
- [ ] Test coverage metrics established as baseline
- [ ] Performance benchmarks established
- [ ] Security test results documented

### 12.2. Post-Implementation Verification
- [ ] All existing tests continue to pass
- [ ] New path format tests pass
- [ ] Performance remains within acceptable bounds
- [ ] Security tests continue to pass
- [ ] Cross-platform tests pass
- [ ] Integration tests pass
- [ ] Test coverage maintained or improved

### 12.3. Regression Verification
- [ ] No new test failures introduced
- [ ] No behavior changes in unrelated functionality
- [ ] All security validations still working
- [ ] Performance characteristics maintained

## 13. Rollback Strategy

### 13.1. Test-Based Rollback Validation
If issues are discovered, have tests ready to validate the rollback:

- Maintain a copy of the original test suite
- Ensure rollback returns to the same test results
- Validate that all functionality returns to original state

## 14. Conclusion

The test update strategy for the ReservedNameHandler refactoring is minimal because:

1. **No behavior changes**: The refactored implementation produces identical results
2. **All existing tests remain valid**: No test expectations need modification
3. **Enhanced coverage**: Additional tests validate the robustness of .NET built-ins
4. **Comprehensive validation**: Multiple test layers ensure quality
5. **Safety net**: Full test suite execution catches any regressions

The primary focus is on ensuring all existing tests continue to pass while adding a few new tests to validate the diversity of path formats that .NET built-in methods can handle.