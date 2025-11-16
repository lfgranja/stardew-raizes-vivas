# Architecture Refactor Plan for Living Roots Mod

## Overview
This document outlines the planned architecture changes based on the PR 67 review comments. The changes aim to improve security, maintainability, and follow SOLID principles with Test Driven Development (TDD).

## Changes Required

### 1. Fix base.Dispose(disposing) redundancy in ModEntry.cs
- **Issue**: Redundant call to `base.Dispose(disposing)` when object is already disposed
- **Solution**: Remove the redundant call to `base.Dispose(disposing)` in the if statement
- **Location**: `LivingRoots/ModEntry.cs` line 57

### 2. Refactor path validation logic in PathValidationService.cs
- **Issue**: Path validation logic is too restrictive and can be simplified
- **Current problems**:
  - Blocks valid paths like `a/b/../../c` which should resolve to `c`
  - Blocks valid directory paths like `a/b/..` which should resolve to directory `a`
  - The depth < 0 check is sufficient to prevent traversal attacks
- **Solution**: Simplify the validation to rely solely on the depth check for `..` segments
- **Location**: `LivingRoots/Domain/PathValidationService.cs` method `ValidatePathDepth`

### 3. Address homoglyph spoofing vulnerability in ReservedNameHandlerTests.cs
- **Issue**: Test asserts the unsafe original string is returned instead of the normalized, safe version
- **Solution**: Update the test to assert that the returned filename is the normalized, safe version (CON_) rather than the original unsafe input (CОN_)
- **Location**: `LivingRoots.Tests/ReservedNameHandlerTests.cs` method `Handle_WithUnicodeHomoglyphOfReservedName_AddsUnderscore`

### 4. Add security check for hidden-name dot prefixing in FileNameSanitizationService.cs
- **Issue**: Adding a leading dot could create an invalid path component like `.` or `..`
- **Solution**: Add a security check when prefixing a filename with a dot for hidden files to ensure the resulting filename does not become an invalid path component
- **Location**: `LivingRoots/Domain/FileNameSanitizationService.cs` around line 120

### 5. Make URL detection case-insensitive in tests
- **Issue**: URL detection in tests is case-sensitive
- **Solution**: Update the test's mock setup for ValidatePath by using a case-insensitive check for URL schemes like "http://" and "https://"
- **Location**: `LivingRoots.Tests/ModDataServiceTests.cs` line 63

### 6. Improve logging privacy test robustness in ModDataServiceTests.cs
- **Issue**: Coupling exception-throwing with logging verification makes the test brittle
- **Solution**: Decouple the logging verification from exception propagation by using a try-catch block
- **Location**: `LivingRoots.Tests/ModDataServiceTests.cs` method `SaveData_WithDangerousPathKey_DoesNotLogFullFilePath`

### 7. Refactor DataExists method to use File.Exists instead of reading JSON
- **Issue**: Using ReadJsonFile to check for file existence is inefficient and leads to inconsistent behavior with invalid JSON
- **Solution**: Use File.Exists to check for file existence without forcing JSON parsing
- **Location**: `LivingRoots/Services/ModDataService.cs` method `DataExists`

### 8. Make command registration atomic in ModController.cs
- **Issue**: Potential race condition in the OnGameLaunched method
- **Solution**: Use the existing `_registrationLock` to ensure the console command registration and event unsubscription are atomic and thread-safe
- **Location**: `LivingRoots/Controllers/ModController.cs` method `OnGameLaunched`

### 9. Differentiate missing vs corrupt files on load in LoadData method
- **Issue**: The LoadData method doesn't differentiate between missing and corrupt files
- **Solution**: Check for file existence before reading to provide more accurate log messages, and adjust the JsonException log level to Warn for consistency
- **Location**: `LivingRoots/Services/ModDataService.cs` method `LoadData`

## Implementation Strategy (TDD Approach)

### Phase 1: Write failing tests
- For each change, write a test that verifies the current behavior and fails with the desired behavior
- This will ensure we understand the issue and have a target to reach

### Phase 2: Implement fixes
- Make the minimal changes required to make each test pass
- Follow SOLID principles and ensure code remains maintainable

### Phase 3: Refactor and optimize
- Clean up any duplicated code
- Ensure consistent error handling and logging
- Verify that all existing tests still pass

### Phase 4: Verify security improvements
- Ensure all security vulnerabilities are properly addressed
- Run security-focused tests to verify improvements

## Testing Strategy

### Unit Tests
- Each change should have corresponding unit tests that verify the fix
- Tests should cover both positive and negative cases
- Ensure edge cases are properly handled

### Integration Tests
- Verify that the changes work properly in the context of the full system
- Ensure that error handling works as expected across service boundaries

### Security Tests
- Verify that all path traversal protections remain effective
- Ensure that no new security vulnerabilities are introduced
- Test with various malicious inputs to ensure robustness

## Quality Assurance

### Code Review Checklist
- [ ] All changes follow SOLID principles
- [ ] Security vulnerabilities are properly addressed
- [ ] Error handling is consistent and appropriate
- [ ] Logging is privacy-preserving and informative
- [ ] Performance is not negatively impacted
- [ ] All tests pass (existing and new)

### Deployment Considerations
- [ ] Changes are backward compatible
- [ ] No breaking changes to public APIs
- [ ] Migration path for existing data is provided if needed
- [ ] Documentation is updated if necessary

## Timeline
1. Write failing tests: 1 day
2. Implement fixes: 2-3 days
3. Refactor and optimize: 1 day
4. Security verification: 1 day
5. Final testing and validation: 1 day

Total estimated time: 5-7 days