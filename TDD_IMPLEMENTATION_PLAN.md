# TDD Implementation Plan for Living Roots Mod Refactor

## Overview
This document outlines the Test Driven Development (TDD) approach for implementing the architecture changes based on PR 67 review comments. Each change will follow the Red-Green-Refactor cycle.

## TDD Process for Each Change

### 1. Fix base.Dispose(disposing) redundancy in ModEntry.cs

**Red Phase (Write failing test):**
- Create a test that verifies the current behavior where base.Dispose(disposing) is called redundantly
- This might be an existing test that demonstrates the issue

**Green Phase (Make test pass):**
- Remove the redundant call to `base.Dispose(disposing)` in the if statement when `_disposed` is true
- The code should simply return without calling base.Dispose again

**Refactor Phase:**
- Verify no other issues are introduced
- Ensure the disposal pattern remains correct

### 2. Refactor path validation logic in PathValidationService.cs

**Red Phase (Write failing test):**
- Write tests that verify valid paths like `a/b/../../c` and `a/b/..` are accepted
- Write tests that verify invalid traversal paths like `../../../etc/passwd` are still rejected
- Current implementation should fail these new tests

**Green Phase (Make test pass):**
- Simplify the `ValidatePathDepth` method to only check for negative depth when processing `..` segments
- Remove the checks for consecutive `..` segments and paths ending with `..`
- Ensure the depth calculation is accurate

**Refactor Phase:**
- Clean up any duplicated logic
- Ensure the method remains readable and maintainable

### 3. Address homoglyph spoofing vulnerability in ReservedNameHandlerTests.cs

**Red Phase (Write failing test):**
- Create a test method `Handle_WithUnicodeHomoglyphOfReservedName_AddsUnderscore_Safely` that verifies the returned filename is the normalized, safe version
- The test should expect "CON_" instead of "CОN_" when handling a Unicode homoglyph

**Green Phase (Make test pass):**
- Update the ReservedNameHandler implementation to ensure it returns the normalized, safe version
- Ensure the returned value is based on the normalized form, not the original homoglyph

**Refactor Phase:**
- Ensure the implementation handles all homoglyph cases properly
- Verify no regression in other functionality

### 4. Add security check for hidden-name dot prefixing in FileNameSanitizationService.cs

**Red Phase (Write failing test):**
- Create tests that verify adding a leading dot doesn't create invalid path components like `.` or `..`
- Test cases should include inputs that could result in `.` or `..` after adding the dot

**Green Phase (Make test pass):**
- Add a security check when prefixing a filename with a dot for hidden files
- Ensure the resulting filename does not become an invalid path component like `.` or `..`

**Refactor Phase:**
- Clean up the implementation to be efficient and clear
- Ensure all edge cases are handled properly

### 5. Make URL detection case-insensitive in tests

**Red Phase (Write failing test):**
- Create tests that use uppercase URL schemes like `HTTP://` and `HTTPS://`
- Current implementation should fail to detect these as invalid paths

**Green Phase (Make test pass):**
- Update the URL detection in tests to use case-insensitive comparisons
- Use `StringComparison.OrdinalIgnoreCase` or similar for URL scheme detection

**Refactor Phase:**
- Ensure the implementation is consistent across all URL detection
- Verify performance is not negatively impacted

### 6. Improve logging privacy test robustness in ModDataServiceTests.cs

**Red Phase (Write failing test):**
- Create a test that verifies logging behavior without coupling it to exception throwing
- The current test might fail when exception behavior changes

**Green Phase (Make test pass):**
- Decouple the logging verification from exception propagation
- Use try-catch blocks to ensure logging verification happens regardless of exceptions

**Refactor Phase:**
- Ensure the test remains focused and clear
- Verify the test accurately reflects the intended behavior

### 7. Refactor DataExists method to use File.Exists instead of reading JSON

**Red Phase (Write failing test):**
- Create performance tests that show the difference between reading JSON and checking file existence
- Create tests that verify behavior with corrupt JSON files is handled appropriately

**Green Phase (Make test pass):**
- Replace the JSON reading approach with File.Exists
- Maintain the same public interface and behavior
- Ensure error handling remains appropriate

**Refactor Phase:**
- Clean up any duplicated code
- Ensure the implementation is efficient and clear

### 8. Make command registration atomic in ModController.cs

**Red Phase (Write failing test):**
- Create tests that simulate concurrent access to the command registration
- These tests should reveal potential race conditions

**Green Phase (Make test pass):**
- Use the existing `_registrationLock` to ensure atomic registration
- Ensure both command registration and event unsubscription are atomic

**Refactor Phase:**
- Verify the locking mechanism is efficient and doesn't create unnecessary contention
- Ensure the implementation is thread-safe under all conditions

### 9. Differentiate missing vs corrupt files on load in LoadData method

**Red Phase (Write failing test):**
- Create tests that distinguish between missing files and corrupt JSON files
- Verify that log messages are appropriate for each case

**Green Phase (Make test pass):**
- Check for file existence before attempting to read JSON
- Update log messages to reflect whether the file is missing or corrupt
- Adjust JsonException log level to Warn for consistency

**Refactor Phase:**
- Ensure the implementation is efficient and doesn't double-check file existence
- Verify all error handling paths are properly covered

## Overall TDD Workflow

1. **Select one change** from the list above to implement
2. **Write a failing test** that demonstrates the current incorrect behavior or missing functionality
3. **Run the test** to confirm it fails as expected
4. **Make the minimal change** to make the test pass
5. **Run all existing tests** to ensure no regression
6. **Refactor** the code if needed to improve quality
7. **Repeat** for the next change

## Quality Checks

### Before Each Change
- Run all existing tests to ensure baseline is stable
- Understand the current behavior and expected behavior

### During Each Change
- Follow Red-Green-Refactor cycle strictly
- Keep changes minimal and focused
- Verify no unintended side effects

### After Each Change
- Run all tests to ensure no regression
- Verify the specific issue is resolved
- Ensure code quality is maintained

## Testing Priority

1. Security-related changes (homoglyph spoofing, hidden-name dot prefixing, URL detection)
2. Performance-related changes (DataExists method)
3. Correctness-related changes (path validation, missing vs corrupt files)
4. Reliability-related changes (command registration, dispose redundancy)

This TDD approach will ensure each change is properly validated and the codebase remains stable throughout the refactoring process.