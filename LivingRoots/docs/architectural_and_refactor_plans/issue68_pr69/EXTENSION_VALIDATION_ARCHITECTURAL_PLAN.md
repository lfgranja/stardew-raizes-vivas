# Extension Validation Architectural Plan

## Overview
This document outlines the architectural plan for updating extension validation in `FileNameSanitizationService.cs` to properly support extensions containing hyphens and underscores (e.g., `.my-save`) while maintaining security against dangerous file extensions.

## Current State Analysis

### Extension Validation Logic
The current extension validation is implemented in the `FindExtensionStartIndex` method within `FileNameSanitizationService.cs`. The key validation logic is on line 727:

```csharp
if (!extensionPart.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || char.IsDigit(c) || c == '-' || c == '_'))
    return -1;
```

### Current Validation Rules
1. Extension must start with a dot (.)
2. Extension part (after dot) can contain ASCII letters (a-z, A-Z)
3. Extension part can contain digits (0-9)
4. Extension part can contain hyphens (-) and underscores (_)
5. Extension must contain at least one alphanumeric character
6. Extension cannot contain control/bidi characters
7. Extension cannot contain path separators
8. Extension cannot contain invalid filename characters

## Identified Issues

### Primary Issue
Despite the validation allowing hyphens and underscores, there may be edge cases where extensions like `.my-save` are not properly recognized due to other validation constraints or logic flows.

### Potential Secondary Issues
1. Extension detection might not work correctly with certain patterns
2. The blocked extensions list might need to be handled differently for extensions with special characters
3. Trimming and processing logic might affect extensions with hyphens/underscores

## Solution Architecture

### Goal
Update the extension validation to robustly support extensions containing hyphens and underscores while maintaining all security measures.

### Design Principles Applied
- **SOLID**: Follow Single Responsibility Principle by keeping extension validation logic focused
- **DRY**: Reuse validation logic where appropriate
- **KISS**: Keep validation rules simple and clear
- **YAGNI**: Only implement necessary validation, avoid over-engineering
- **DDD**: Use domain-specific language for file extension concepts

## Implementation Plan

### 1. Enhanced Extension Validation Logic

#### Updated FindExtensionStartIndex Method
```csharp
/// <summary>
/// Helper method to find the start index of a valid file extension.
/// Enhanced to properly support extensions with hyphens and underscores.
/// </summary>
/// <param name="filename">The filename to check.</param>
/// <returns>The start index of the extension (including the dot) if valid, or -1 if no valid extension found.</returns>
private static int FindExtensionStartIndex(string filename)
{
    // Special handling for "." and ".." - these are special path components, not filenames with extensions
    if (filename == "." || filename == "..")
        return -1;
    
    // Find the last dot in the original filename string
    int lastDotIndex = filename.LastIndexOf('.');

    // No dot found or dot is at the end
    if (lastDotIndex < 0 || lastDotIndex >= filename.Length - 1)
        return -1;

    // Extract potential extension from the original string (including the dot)
    string potentialExtension = filename.Substring(lastDotIndex);

    // Normalize the potential extension for validation purposes only
    var extNormalized = potentialExtension.Normalize(NormalizationForm.FormC);

    // Reject if extension ends with whitespace or an extra dot
    if (char.IsWhiteSpace(extNormalized[extNormalized.Length - 1]) || extNormalized[extNormalized.Length - 1] == '.')
        return -1;

    string extensionPart = extNormalized.Substring(1);

    // If the extension part trimmed of fillers has no alphanumerics, it's not a real extension
    var trimmedPart = extensionPart.Trim('_', ' ', '.');
    if (trimmedPart.Length == 0 || !trimmedPart.Any(c => char.IsLetterOrDigit(c)))
        return -1;

    // Reject if extension contains any bidi/control characters that could obfuscate
    // U+202A..U+202E (bidi overrides), U+2066..U+2069 (isolate), and general control chars
    if (extensionPart.Any(ch => char.IsControl(ch) ||
                               (ch >= '\u202A' && ch <= '\u202E') ||
                               (ch >= '\u2066' && ch <= '\u2069')))
        return -1;

    // Allow ASCII letters/digits/hyphens/underscores for core of extension
    // ENHANCEMENT: This validation properly supports extensions with hyphens and underscores
    if (!extensionPart.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || char.IsDigit(c) || c == '-' || c == '_'))
        return -1;

    // Check if extension contains path separators
    if (extNormalized.Contains('/') || extNormalized.Contains('\\'))
        return -1;

    // For security: if the filename starts with a dot followed by a dangerous extension, still detect it
    if (lastDotIndex == 0 && IsBlockedExtension(extNormalized))
        return 0; // Return 0 for simple dotfiles with dangerous extensions like ".exe"

    // For simple dotfiles (e.g., ".profile"), check if it's not a dangerous extension
    if (lastDotIndex == 0 && !IsBlockedExtension(extNormalized))
        return -1; // Don't treat simple dotfiles as having extensions unless they're dangerous

    // Check if the extension contains invalid filename characters
    if (extNormalized.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
        return -1;

    // If all checks pass, return the index from the original string
    return lastDotIndex;
}
```

### 2. Security Measures

#### Maintained Security Features
1. **Blocked Extensions**: Dangerous extensions (executables, scripts) remain blocked
2. **Control Character Prevention**: No control or bidi characters allowed
3. **Path Traversal Prevention**: No path separators allowed in extensions
4. **Invalid Character Prevention**: System invalid filename characters blocked

#### Enhanced Security for Special Characters
1. **Proper Hyphen/Underscore Handling**: These characters are allowed but validated
2. **Alphanumeric Requirement**: Extensions must still contain at least one alphanumeric character
3. **Length Validation**: No excessive length extensions allowed

### 3. SOLID, DRY, KISS, YAGNI, and DDD Compliance

#### SOLID Principles
- **SRP**: Extension validation is kept in a focused method
- **OCP**: New validation rules can be added without modifying existing code
- **LSP**: Derived classes can override validation while maintaining contract
- **ISP**: Interface remains minimal and focused
- **DIP**: Dependencies on abstractions, not concrete implementations

#### DRY Principle
- Reuse existing validation logic where appropriate
- Avoid duplication of character validation logic

#### KISS Principle
- Keep validation rules simple and readable
- Avoid complex regex or convoluted logic

#### YAGNI Principle
- Only implement validation needed for current requirements
- Avoid speculative validation for hypothetical scenarios

#### DDD Principles
- Use domain language: "extension", "filename", "blocked extension"
- Clear boundaries between validation and sanitization concerns

### 4. Existing Functionality Preservation

#### Maintained Features
1. **Blocked Extension Handling**: Dangerous extensions still converted to `.blocked`
2. **Hidden File Support**: Files starting with dot still handled properly
3. **Length Limitations**: Maximum filename length still enforced
4. **Reserved Name Handling**: Windows reserved names still handled
5. **Unicode Support**: Proper Unicode normalization maintained
6. **Surrogate Pair Handling**: Emoji and special characters still supported

### 5. Test Updates

#### New Test Cases Required
1. Extensions with hyphens: `.my-extension`, `.config-dev`
2. Extensions with underscores: `.my_extension`, `.config_dev`
3. Extensions with mixed characters: `.my_config-dev`
4. Multiple consecutive special characters: `.my--extension`, `.my__extension`
5. Edge cases: `.my-.extension`, `.-my.extension`, `._my.extension`

#### Updated Test Cases
1. Ensure blocked extensions with special characters are still blocked
2. Verify that extensions with hyphens/underscores are preserved
3. Test that security validation still works with special character extensions

## Verification Approach

### Security Verification
1. **Penetration Testing**: Test various malicious extension patterns
2. **Fuzz Testing**: Test random combinations of allowed characters
3. **Edge Case Testing**: Test boundary conditions and unusual patterns

### Usability Verification
1. **Real-world Extensions**: Test with actual extensions that use hyphens/underscores
2. **Cross-platform Compatibility**: Ensure extensions work across different OS
3. **Integration Testing**: Verify with the full file sanitization workflow

### Performance Verification
1. **Performance Testing**: Ensure validation doesn't significantly impact performance
2. **Memory Usage**: Verify no memory leaks in validation logic
3. **Scalability**: Test with large numbers of files containing special extensions

## Implementation Steps

### Phase 1: Code Changes
1. Update `FindExtensionStartIndex` method with enhanced validation
2. Ensure all character validation is properly implemented
3. Add comprehensive comments explaining the validation logic

### Phase 2: Testing
1. Update existing tests to verify new functionality
2. Add new test cases for extensions with hyphens and underscores
3. Run security-focused tests to ensure no vulnerabilities introduced

### Phase 3: Verification
1. Run full test suite to ensure no regressions
2. Perform manual verification of new extension patterns
3. Verify security measures remain effective

## Risk Mitigation

### Security Risks
- **Risk**: Allowing hyphens/underscores might enable new attack vectors
- **Mitigation**: Maintain all existing security validations and add specific tests

### Compatibility Risks
- **Risk**: Changes might affect existing functionality
- **Mitigation**: Comprehensive regression testing and backward compatibility checks

### Performance Risks
- **Risk**: Additional validation might slow down processing
- **Mitigation**: Performance testing and optimization where necessary

## Success Criteria

### Functional Requirements
1. Extensions with hyphens are properly recognized and preserved (e.g., `.my-save`)
2. Extensions with underscores are properly recognized and preserved (e.g., `.my_save`)
3. Extensions with mixed hyphens and underscores work correctly (e.g., `.my-config_dev`)
4. All security measures remain intact
5. Existing functionality is preserved

### Non-functional Requirements
1. Performance remains within acceptable bounds
2. Code maintainability is improved or maintained
3. Security posture is not compromised
4. All tests pass successfully

## Conclusion

This architectural plan provides a comprehensive approach to updating extension validation in `FileNameSanitizationService.cs` to support extensions containing hyphens and underscores while maintaining security. The solution follows established software engineering principles and ensures all existing functionality is preserved while adding the required new capabilities.