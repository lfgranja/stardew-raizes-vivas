# Security Validation Maintenance Plan: Reserved Name Handling with System.Uri

## 1. Overview

This document outlines how the refactored UNC path handling implementation using `System.Uri` maintains all existing security validations for reserved names. The core security logic remains unchanged while only the path parsing mechanism is updated.

## 2. Core Security Components Preservation

### 2.1. Reserved Windows File Names Set
The static `HashSet<string>` containing reserved Windows filenames remains unchanged:

```csharp
private static readonly HashSet<string> ReservedWindowsFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "CON", "PRN", "AUX", "NUL",
    "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
    "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
};
```

This ensures:
- All 22 reserved Windows filenames continue to be detected
- Case-insensitive comparison using `StringComparer.OrdinalIgnoreCase`
- No changes to the fundamental security validation rules

### 2.2. ProcessFileName Method Integrity
The `ProcessFileName` method, which contains the core security validation logic, remains completely unchanged:

```csharp
private string ProcessFileName(string filename)
{
    // All security validations remain exactly as implemented:
    // - Insignificant character handling
    // - Name and extension extraction
    // - Reserved name detection (full match and prefix)
    // - Homoglyph detection through Unicode normalization
    // - Combining marks removal
    // - Reserved name result construction
}
```

## 3. Security Validation Preservation by Component

### 3.1. Insignificant Character Handling
The existing logic for handling dots, spaces, and tabs is preserved:

```csharp
// Check if the entire filename consists entirely of insignificant characters
string trimmedAll = filename.Trim('.', ' ', '\t');
if (string.IsNullOrEmpty(trimmedAll))
{
    // Replace fully insignificant names with a safe placeholder
    return "_";
}
```

This ensures:
- Filenames consisting only of dots, spaces, and tabs are replaced with safe placeholders
- Leading and trailing insignificant characters are properly handled
- The security measure against malformed filenames remains intact

### 3.2. Name and Extension Processing
The logic for separating names from extensions remains unchanged:

```csharp
private static (string namePart, string extensionPart) ExtractNameAndExtension(string filename)
{
    // Properly separates name and extension components
    // Handles multiple extensions correctly (e.g., file.tar.gz)
    // Maintains extension integrity during reserved name processing
}
```

This ensures:
- File extensions are preserved during processing
- Multiple extensions are handled correctly
- The security logic only affects the name part, not extensions

### 3.3. Reserved Name Detection
Three layers of reserved name detection remain intact:

#### 3.3.1. Full and Prefix Match Detection
```csharp
private static (string actualCoreName, bool isReserved) CheckForReservedNameStart(string coreName)
{
    // Detects reserved names at the beginning of longer names (e.g., COM123)
    // Validates that reserved names are followed by non-alphanumeric characters
}
```

#### 3.3.2. Exact Match Detection
```csharp
private static (string actualCoreName, bool isReserved) CheckForReservedNameFullMatch(string coreName, string actualCoreName, bool isReserved)
{
    // Detects exact matches of reserved names
    // Case-insensitive comparison using StringComparer.OrdinalIgnoreCase
}
```

#### 3.3.3. Normalized Detection
```csharp
private (string actualCoreName, bool isReserved) CheckForNormalizedReservedNames(string coreName, string actualCoreName, bool isReserved)
{
    // Uses IUnicodeNormalizationService to detect homoglyphs
    // Prevents spoofing attempts using Unicode characters
}
```

### 3.4. Homoglyph and Diacritic Protection
The Unicode normalization service integration remains unchanged:

```csharp
string? normalizedCore = _unicodeNormalizationService.Normalize(coreName);
// Checks normalized form against reserved names
// Prevents spoofing with visually similar characters
```

This ensures:
- Protection against homoglyph attacks (e.g., using Cyrillic 'О' instead of Latin 'O')
- Proper detection of reserved names even with combining marks
- Security against Unicode spoofing attempts

### 3.5. Combining Marks Removal
The diacritic removal logic remains intact:

```csharp
private static string RemoveCombiningMarks(string input)
{
    // Removes combining marks to detect attempts to bypass using diacritics
    // Prevents attacks using characters with diacritical marks
}
```

## 4. UNC Path-Specific Security Considerations

### 4.1. Filename Component Isolation
When processing UNC paths with `System.Uri`, only the filename component is passed to security validation:

```csharp
var fileNameComponent = segments[segments.Length - 1];
string? processedFileName = ProcessFileName(fileNameComponent);
```

This ensures:
- Directory path components are not subject to reserved name validation
- Only the actual filename is checked for reserved names
- Security validation is applied precisely where needed

### 4.2. Path Reconstruction Security
After filename processing, the path is reconstructed without modifying security-relevant components:

```csharp
// Only the filename segment is replaced with the processed version
// All directory components remain unchanged
```

This ensures:
- No security bypass through path manipulation
- Directory traversal protections remain intact
- The overall path structure is preserved

## 5. Security Validation Flow

### 5.1. UNC Path Processing Flow
```
Input UNC Path (e.g., \\server\share\CON.txt)
    ↓
System.Uri Parsing
    ↓
Extract Last Segment (CON.txt)
    ↓
ProcessFileName Method (with all security validations)
    ↓
Reserved Name Detection (CON → CON_)
    ↓
Reconstruct Path (\\server\share\CON_.txt)
```

### 5.2. Non-UNC Path Processing Flow
```
Input Path (e.g., C:\path\to\CON.txt)
    ↓
Traditional Path.GetFileName (CON.txt)
    ↓
ProcessFileName Method (with all security validations)
    ↓
Reserved Name Detection (CON → CON_)
    ↓
Reconstruct Path (C:\path\to\CON_.txt)
```

## 6. Security Testing Requirements

### 6.1. Reserved Name Detection Tests
- All existing reserved name test cases must continue to pass
- Tests for each of the 22 reserved Windows filenames
- Case variation tests (CON, con, CoN, etc.)

### 6.2. Homoglyph Protection Tests
- Tests with Unicode homoglyphs (CОN with Cyrillic 'О')
- Tests with combining marks
- Tests with diacritical marks

### 6.3. Edge Case Security Tests
- Tests with insignificant characters
- Tests with multiple extensions
- Tests with embedded reserved names (COM123)

### 6.4. UNC-Specific Security Tests
- UNC paths with reserved names
- UNC paths with homoglyphs
- UNC paths with insignificant characters in filenames

## 7. Risk Mitigation

### 7.1. Validation Layer Isolation
The security validation logic is completely isolated from the path parsing logic, ensuring that changes to one do not affect the other.

### 7.2. Comprehensive Testing
All security validations must be verified through comprehensive test coverage before deployment.

### 7.3. Gradual Rollout
The implementation should be tested in isolated environments before full deployment.

## 8. Verification Checklist

### 8.1. Pre-Implementation Verification
- [ ] All existing security tests pass with new implementation
- [ ] Reserved name detection works for all 22 Windows reserved names
- [ ] Homoglyph detection continues to function
- [ ] Insignificant character handling remains intact
- [ ] Extension preservation works correctly

### 8.2. Post-Implementation Verification
- [ ] UNC paths with reserved names are properly handled
- [ ] Security validation performance is acceptable
- [ ] No new security vulnerabilities are introduced
- [ ] All edge cases are handled correctly

## 9. Conclusion

The refactored implementation using `System.Uri` maintains all existing security validations by preserving the core `ProcessFileName` method and its associated security logic. The change only affects the path parsing mechanism, not the security validation itself. This approach ensures that all security measures remain intact while improving the robustness and maintainability of UNC path handling.