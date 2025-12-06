# Affected Tests Analysis - Cross-Platform Path Separator Issues

## Overview
This document provides a comprehensive analysis of all tests that may be affected by platform-specific path separators in the Stardew LivingRoots project.

## Detailed Test Analysis

### 1. PathValidationServiceTests.cs

#### Hardcoded Backslash Tests
- Line 69: `var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("..\\file.txt"));`
- Line 77: `var exception = Assert.Throws<ArgumentException>(() => _service.Validate("C:\\Windows\\file.txt"));`
- Line 112: `_service.Validate(".\\file.txt"); // Should not throw`
- Line 121: `_service.Validate("folder\\.."); // Should not throw`
- Line 131: `var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("..\\..\\file.txt"));`
- Line 141: `_service.Validate("path\\to\\.file.txt"); // Should not throw - test backslash version too`
- Line 166: `var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate(".\\"));`
- Line 177: `var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate(".\\..\\file.txt"));`
- Line 188: `_service.Validate("folder\\subfolder\\..\\..\\file.txt"); // Should not throw - backslash version`
- Line 196: `_service.Validate("folder\\subfolder\\file.txt"); // Should not throw - backslash version`
- Line 211: `_service.Validate("folder1\\folder2\\folder3\\file.txt"); // Should not throw - backslash version`
- Line 223: `_service.Validate("folder\\..\\file.txt"); // Should not throw`
- Line 231: `_service.Validate("folder\\subfolder\\..\\file.txt"); // Should not throw - backslash version`
- Line 239: `_service.Validate("folder\\.\\file.txt"); // Should not throw - backslash version`
- Line 247: `_service.Validate("folder\\..\\subfolder\\file.txt"); // Should not throw - backslash version`
- Line 339: `_service.Validate("a\\b\\..\\..\\c"); // Backslash version`
- Line 352: `_service.Validate("a\\b\\.."); // Should not throw`
- Line 364: `_service.Validate("a\\b\\..\\..\\c"); // Backslash version`
- Line 376: `_service.Validate("folder\\subfolder\\..\\file.txt"); // Backslash version`
- Line 390: `var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("folder\\..\\..\\..\\file.txt"));`
- Line 452: `var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("\u2025\u2025\\file.txt")); // ".." with backslash`

#### Potential Issues
- Tests explicitly checking for backslash behavior may not work on Unix-like systems
- Tests that validate both forward and backslash formats need to be verified

### 2. PathTraversalValidatorDomainTests.cs

#### Hardcoded Backslash Tests
- Line 51: `var exception = Assert.Throws<ArgumentException>(() => _service.Validate("..\\file.txt"));`
- Line 59: `var exception = Assert.Throws<ArgumentException>(() => _service.Validate("C:\\Windows\\file.txt"));`

#### Potential Issues
- Same validation logic should work for both separator formats
- Tests should verify that both formats are handled consistently

### 3. DotSegmentTests.cs

#### Hardcoded Backslash Tests
- Line 56: `var ex2 = Record.Exception(() => _validator.Validate(".\\file"));`
- Line 64: `var ex4 = Record.Exception(() => _validator.Validate("file\\."));`
- Line 73: `_validator.Validate("folder\\.\\file"); // This should not throw`
- Line 74: `_validator.Validate("path\\to\\.\\file.txt");  // This should not throw`
- Line 93: `var ex2 = Record.Exception(() => _validator.Validate(".\\path"));`
- Line 101: `var ex4 = Record.Exception(() => _validator.Validate("path\\."));`

#### Path.Combine Usage
- Line 33: `_validator.Validate(Path.Combine("folder", ".hidden"));`
- Line 34: `_validator.Validate(Path.Combine("folder", ".config"));`
- Line 109: `var ex1 = Record.Exception(() => _validator.Validate(Path.Combine("path", "to", "file.txt")));`
- Line 113: `var ex2 = Record.Exception(() => _validator.Validate(Path.Combine("path", ".", "file.txt")));`

#### Potential Issues
- Path.Combine generates platform-specific separators, but tests may expect specific formats
- Backslash tests need to be validated for cross-platform compatibility

### 4. SanitizePathSegmentsTests.cs

#### Path Normalization
- Line 132: `Assert.Equal("segment1/segment2", result.ToString().Replace('\\', '/'));`
- Line 152: `Assert.Equal("segment1/segment2", result.ToString().Replace('\\', '/'));`

#### Potential Issues
- Already implements path normalization, so this is well-handled
- Good example of proper cross-platform approach

### 5. ModDataServiceTests.cs

#### Path Search Logic
- Line 70: `s.Contains("..\\") ||`
- Line 72: `s.Contains("..\\..\\") ||`

#### Path.Combine Usage with Normalization
- Line 928: `Path.Combine("data", "test", "key", "with_invalid_chars.json").Replace('\\', '/')`

#### Potential Issues
- Search logic may miss patterns on platforms with different separators
- Good that the assertion normalizes the path

### 6. FileNameExtensionHelperTests.cs

#### Mixed Separator Tests
- Line 62: `Assert.Equal("", GetFileExtensionTest(sanitizationService, "file.txt\\extra"));`
- Line 116: `Assert.Equal("file.txt\\extra", RemoveFileExtensionTest(sanitizationService, "file.txt\\extra"));`

#### Potential Issues
- Tests include both forward and backslash separators
- Need to ensure both formats are handled properly

### 7. DotSegmentAllowanceTest.cs

#### Path Search Logic
- Line 65: `s.Contains("..\\") ||`
- Line 67: `s.Contains("..\\..\\") ||`

#### Potential Issues
- Similar to ModDataServiceTests, search logic may miss patterns on different platforms

### 8. FileNameSanitizationServiceTests.cs

#### Hardcoded Backslash Tests
- Line 116: `.Setup(x => x.Normalize("..\\test")).Returns("..\\test");`
- Line 125: `.Setup(x => x.Normalize(It.Is<string>(s => s != "../test" && s != "..\\test" && s != "..test" && s != "test..")))`
- Line 145: `var result2 = _service.Sanitize("..\\test");`
- Line 147: `Assert.NotEqual("..\\test", result2); // Should be transformed`

#### Potential Issues
- Mock setups use hardcoded backslashes
- Assertions expect specific backslash formats

### 9. ReservedNameHandlerTests.cs

#### Path.Combine Usage
- Line 547: `string input = Path.Combine("C:", "CON.txt"); // Rooted path with reserved name`
- Line 557: `Assert.Equal(Path.Combine("C:", "CON_.txt"), result);`
- Line 566: `string input = Path.Combine("C:", "COM1.xml"); // Rooted path with reserved name and extension`
- Line 576: `Assert.Equal(Path.Combine("C:", "COM1_.xml"), result);`
- Line 607: `string input = Path.Combine("C:", "normal_file.txt"); // Rooted path with non-reserved name`
- Line 616: `Assert.Equal(Path.Combine("C:", "normal_file.txt"), result);`
- Line 625: `string input = Path.Combine("C:", "CONSOLE.txt"); // Rooted path with name similar to reserved but not exact`
- Line 634: `Assert.Equal(Path.Combine("C:", "CONSOLE.txt"), result);`
- Line 645: `string input = Path.Combine("C:", "some", "directory") + Path.DirectorySeparatorChar; // Rooted directory path ending with separator`
- Line 660: `string input = Path.Combine("C:", "path", "to", "AUX", "file", "LPT1.dat"); // Rooted path with reserved name in final component`
- Line 670: `Assert.Equal(Path.Combine("C:", "path", "to", "AUX", "file", "LPT1_.dat"), result);`

#### UNC Path Tests
- Line 587: `string input = @"\\server\share\PRN.log"; // UNC path with reserved name`
- Line 598: `Assert.Equal(@"\\server\share\PRN_.log", result);`

#### Potential Issues
- Path.Combine generates platform-specific separators but assertions may expect specific formats
- UNC paths use hardcoded backslashes which may be platform-specific

## Summary of Affected Tests

### High Priority (Require Immediate Attention)
1. **PathValidationServiceTests.cs** - 21 instances of hardcoded backslashes
2. **PathTraversalValidatorDomainTests.cs** - 2 instances of hardcoded backslashes
3. **DotSegmentTests.cs** - 6 instances of hardcoded backslashes + Path.Combine usage
4. **FileNameExtensionHelperTests.cs** - 2 instances of mixed separators
5. **FileNameSanitizationServiceTests.cs** - 4 instances in mock setups and assertions
6. **ReservedNameHandlerTests.cs** - Path.Combine usage with potential assertion issues + UNC paths

### Medium Priority (Need Review)
1. **ModDataServiceTests.cs** - Path search logic with separators
2. **DotSegmentAllowanceTest.cs** - Path search logic with separators

### Low Priority (Already Well-Handled)
1. **SanitizePathSegmentsTests.cs** - Already implements proper path normalization

## Recommended Actions

### Immediate Actions
1. Create path normalization helper methods
2. Update all hardcoded backslash tests to use normalized comparisons
3. Verify Path.Combine usage in assertions
4. Add dual-format testing where appropriate

### Testing Strategy
1. Test both separator formats explicitly in validation tests
2. Use normalized path comparisons for assertions
3. Ensure all security validations work regardless of separator format
4. Verify cross-platform compatibility on Windows, Linux, and Mac

This analysis provides a comprehensive view of all tests affected by path separator issues and guides the implementation of cross-platform compatible tests.