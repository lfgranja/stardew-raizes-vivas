# LivingRoots Architecture Refactor Plan

## Overview
This document outlines the comprehensive plan to address all issues identified in the code review for PR 67 - Rodada 21+. The plan follows TDD principles and implements SOLID, DRY, KISS, YAGNI, and DDD engineering practices.

## Issues Summary

### 1. FileNameSanitizationService.cs Issues
- **Duplicated hidden-file prefixing logic** (lines 182-195): Duplicate code blocks for handling hidden files
- **Invalid extension detection bug**: FindExtensionStartIndex method incorrectly identifying extensions
- **Inconsistent validation for dangerous extensions**: Inconsistent handling of blocked extensions

### 2. PathValidationService.cs Issues
- **Security vulnerability**: Checks performed before Unicode normalization
- **Conflicting validation logic**: Mixed depth-based and legacy validation approaches

### 3. ModDataService.cs Issues
- **Missing null checks in DataExists method**: Missing checks for `_helper` and `_helper.Data`
- **Redundant path traversal check**: Duplicate check in SanitizePathSegments
- **Dead null check in GetFilePath method**: Unnecessary null check after validation

### 4. ModController.cs Issues
- **Potential deadlock in event handler**: Lock acquisition within event handler
- **Race condition in Dispose**: Improper disposal order

## Implementation Plan

### Phase 1: Critical Security Issues (High Priority)

#### 1.1 Fix Security Vulnerability in PathValidationService.cs
**Issue**: Unicode normalization occurs after some validation checks, allowing homoglyph attacks to bypass security.

**Solution**:
- Move Unicode normalization to the very beginning of the Validate method
- Ensure all security checks operate on normalized input
- Maintain the depth-based analysis for legitimate path traversal

**TDD Approach**:
1. Create tests for homoglyph attacks before fix
2. Implement the fix
3. Verify tests pass

```csharp
// Current (vulnerable):
string normalizedPath = _unicodeNormalizationService.Normalize(path);
// ... some checks on original path ...
ValidatePathTraversalDepth(normalizedPath);

// Fixed (secure):
string normalizedPath = _unicodeNormalizationService.Normalize(path);
// ... all checks on normalizedPath ...
```

#### 1.2 Remove Duplicated Hidden-File Logic in FileNameSanitizationService.cs
**Issue**: Lines 182-195 contain duplicate code that's already handled in lines 183-190.

**Solution**:
- Remove the duplicate block (lines 191-195)
- Verify the existing logic handles all cases properly

**TDD Approach**:
1. Create test to verify hidden file logic still works after removal
2. Remove duplicate code
3. Run tests to ensure functionality remains

### Phase 2: Path Validation Issues (High Priority)

#### 2.1 Fix Conflicting Validation Logic in PathValidationService.cs
**Issue**: Conditional validation logic creates inconsistent behavior for paths ending with "..".

**Solution**:
- Remove the conditional logic that delegates to PathTraversalValidator
- Consolidate validation logic to use only the depth-based approach
- Ensure legitimate paths like "folder/.." are allowed while malicious ones are blocked

**TDD Approach**:
1. Create tests for both legitimate and malicious path traversal scenarios
2. Refactor validation logic
3. Verify all tests pass

#### 2.2 Fix Invalid Extension Detection in FileNameSanitizationService.cs
**Issue**: FindExtensionStartIndex method incorrectly identifies extensions, especially for dotfiles.

**Solution**:
- Improve the extension detection logic to properly handle dotfiles vs. extensions
- Ensure extensions require alphanumeric characters to be valid
- Fix the logic that determines what constitutes a real extension vs. a dotfile

**TDD Approach**:
1. Create comprehensive tests for various file name scenarios
2. Refactor the extension detection logic
3. Verify all tests pass

### Phase 3: Data Service Issues (Medium Priority)

#### 3.1 Add Missing Null Checks in DataExists Method
**Issue**: Missing defensive null checks for `_helper` and `_helper.Data`.

**Solution**:
- Add null checks similar to LoadData method
- Maintain consistent error handling across methods

**TDD Approach**:
1. Create tests that simulate null helper scenarios
2. Add the null checks
3. Verify tests pass

#### 3.2 Remove Redundant Path Traversal Check
**Issue**: Duplicate path traversal check in SanitizePathSegments method.

**Solution**:
- Remove the redundant check since validation is already performed at a higher level
- Follow Single Responsibility Principle

**TDD Approach**:
1. Verify validation still occurs at the appropriate level
2. Remove redundant check
3. Ensure functionality remains intact

#### 3.3 Remove Dead Null Check in GetFilePath Method
**Issue**: Unnecessary null check after validation already ensures non-null input.

**Solution**:
- Remove the redundant null check on line 318

**TDD Approach**:
1. Verify the validation flow ensures non-null input
2. Remove the dead code
3. Run tests to ensure no regressions

### Phase 4: Controller Issues (Medium Priority)

#### 4.1 Fix Potential Deadlock in ModController.cs
**Issue**: Lock acquisition within OnGameLaunched event handler can cause deadlocks.

**Solution**:
- Replace blocking lock with Monitor.TryEnter to avoid deadlocks
- Use non-blocking approach in event handlers

**TDD Approach**:
1. Create tests that simulate concurrent access scenarios
2. Implement the non-blocking lock approach
3. Verify thread safety and performance

#### 4.2 Fix Race Condition in Dispose Method
**Issue**: Improper disposal order can cause race conditions.

**Solution**:
- Set the `_disposed` flag before calling UnregisterEvents
- Ensure cleanup occurs deterministically

**TDD Approach**:
1. Create tests for disposal scenarios
2. Implement the fix
3. Verify thread safety

### Phase 5: Additional Code Quality Issues (Low Priority)

#### 5.1 Fix Character Equality Checks
**Issue**: Using `.Equals()` for char comparisons instead of `!=` operator.

**Solution**:
- Replace `.Equals()` calls with `!=` operator for better performance and consistency

#### 5.2 Improve Error Handling in RemoveData Method
**Issue**: Missing null check and inconsistent error handling.

**Solution**:
- Add explicit null check for key parameter
- Align error handling with other methods

## Implementation Order

1. **Security fixes first**: Address PathValidationService security vulnerability
2. **Code duplication**: Remove duplicated hidden-file logic
3. **Validation logic**: Fix conflicting path validation
4. **Extension detection**: Improve extension detection logic
5. **Null checks**: Add missing null checks
6. **Redundant checks**: Remove duplicate validations
7. **Thread safety**: Fix deadlock and race condition issues
8. **Code quality**: Address remaining issues

## Testing Strategy

### Unit Tests
- Create specific tests for each identified issue
- Verify fixes don't break existing functionality
- Test edge cases and security scenarios

### Integration Tests
- Verify components work together correctly
- Test security boundaries
- Validate error handling scenarios

### Security Tests
- Test homoglyph attacks
- Verify path traversal prevention
- Test extension blocking

## Quality Assurance

### SOLID Principles
- **Single Responsibility**: Each method should have one clear purpose
- **Open/Closed**: Design for extension without modification
- **Liskov Substitution**: Maintain interface contracts
- **Interface Segregation**: Keep interfaces focused
- **Dependency Inversion**: Depend on abstractions, not implementations

### DRY (Don't Repeat Yourself)
- Eliminate duplicate code blocks
- Consolidate similar logic
- Use helper methods where appropriate

### KISS (Keep It Simple, Stupid)
- Avoid over-engineering
- Choose simple solutions when possible
- Maintain readability

### YAGNI (You Aren't Gonna Need It)
- Don't implement features not currently needed
- Focus on solving current problems
- Avoid speculative generality

### DDD (Domain-Driven Design)
- Maintain clear domain boundaries
- Keep domain logic separate from infrastructure
- Use domain language consistently

## Risk Mitigation

### Testing Risks
- **Solution**: Comprehensive test coverage before and after changes
- **Verification**: Run all existing tests to ensure no regressions

### Security Risks
- **Solution**: Focus on security fixes first
- **Verification**: Test with various attack vectors

### Performance Risks
- **Solution**: Maintain performance characteristics
- **Verification**: Profile critical paths after changes

## Success Criteria

- All identified issues are resolved
- All existing tests pass
- New tests cover fixed scenarios
- Security vulnerabilities are addressed
- Code quality is improved
- No performance regressions
- Thread safety is maintained