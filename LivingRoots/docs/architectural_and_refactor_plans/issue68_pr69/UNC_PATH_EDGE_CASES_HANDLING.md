# UNC Path Edge Cases Handling: Server-Only and Server-Share-Only Scenarios

## 1. Overview

This document details how the refactored UNC path handling implementation using `System.Uri` addresses edge cases such as server-only and server-share-only UNC paths. These scenarios require special handling to ensure proper behavior while maintaining security validations.

## 2. Edge Case Categories

### 2.1. Server-Only UNC Paths
- `\\server` - UNC path with only a server component
- `\\server\` - Server component with trailing separator
- `file://server` - URI format with only server component

### 2.2. Server-Share-Only UNC Paths
- `\\server\share` - UNC path with server and share components
- `\\server\share\` - Server-share with trailing separator
- `file://server/share` - URI format with server and share

### 2.3. Other Edge Cases
- Empty path segments
- Invalid or malformed UNC paths
- Paths with special characters

## 3. Current Implementation Behavior Analysis

### 3.1. Server-Only Paths Analysis
In the current manual implementation:
```csharp
// For path "\\server"
int lastSeparatorPos = -1;
for (int i = 2; i < filename.Length; i++) // Start from 2 to skip the UNC prefix (\\)
{
    // No separator found after UNC prefix
    // Would result in fileName = "server" (after removing UNC prefix)
    // Processed as a filename, which is incorrect
}
```

### 3.2. Server-Share-Only Paths Analysis
In the current manual implementation:
```csharp
// For path "\\server\share"
// Would extract "share" as the filename component
// Processed as a filename, which may not be the intended behavior
```

## 4. System.Uri Behavior for Edge Cases

### 4.1. Server-Only Paths with System.Uri
```csharp
var uri = new Uri(@"\\server");
// uri.IsUnc = true
// uri.Segments = ["/", "server/"]
// Last segment would be "server/" - this represents a directory, not a file
```

### 4.2. Server-Share-Only Paths with System.Uri
```csharp
var uri = new Uri(@"\\server\share");
// uri.IsUnc = true
// uri.Segments = ["/", "server/", "share/"]
// Last segment would be "share/" - this represents a directory, not a file
```

## 5. Proper Edge Case Handling Strategy

### 5.1. Detection of Directory vs File Paths
The key insight is to distinguish between:
- Paths that represent directories (ending with separator or where last segment ends with '/')
- Paths that represent files (where last segment doesn't end with '/')

### 5.2. Updated Implementation Approach

```csharp
private string? ProcessUncPath(Uri uri, string originalPath)
{
    var segments = uri.Segments;
    
    if (segments.Length == 0)
    {
        // No segments, return original
        return originalPath;
    }
    
    // Get the last segment
    var lastSegment = segments[segments.Length - 1];
    
    // Check if the last segment represents a directory (ends with '/')
    if (lastSegment.EndsWith("/"))
    {
        // This is a directory path, not a file path
        // Return original as there's no filename to process
        return originalPath;
    }
    
    // This is a file path, process the filename
    string? processedFileName = ProcessFileName(lastSegment);
    
    if (processedFileName == lastSegment)
    {
        // No change needed, return original
        return originalPath;
    }
    
    // Reconstruct with processed filename
    return ReconstructUncPath(uri, segments, processedFileName);
}
```

### 5.3. Enhanced Path Reconstruction

```csharp
private string ReconstructUncPath(Uri uri, string[] originalSegments, string processedFileName)
{
    var builder = new StringBuilder();
    
    // Start with the scheme and server
    builder.Append(uri.Scheme);
    builder.Append("://");
    builder.Append(uri.Host);
    
    // Add all path segments except the last one
    for (int i = 0; i < originalSegments.Length - 1; i++)
    {
        builder.Append(originalSegments[i]);
    }
    
    // Add the processed filename
    builder.Append(processedFileName);
    
    return builder.ToString();
}
```

## 6. Specific Edge Case Handling

### 6.1. Server-Only Paths
```csharp
// Input: "\\server"
// Segments: ["/", "server/"]
// Last segment: "server/" (ends with '/', indicating directory)
// Result: Return original path unchanged
```

### 6.2. Server-Share-Only Paths
```csharp
// Input: "\\server\share"
// Segments: ["/", "server/", "share/"]
// Last segment: "share/" (ends with '/', indicating directory)
// Result: Return original path unchanged
```

### 6.3. Valid File Paths
```csharp
// Input: "\\server\share\file.txt"
// Segments: ["/", "server/", "share/", "file.txt"]
// Last segment: "file.txt" (doesn't end with '/', indicating file)
// Result: Process "file.txt" for reserved names
```

## 7. Additional Edge Case Considerations

### 7.1. Paths Ending with Separators
```csharp
// Input: "\\server\share\"
// Should be treated as directory path, return unchanged
```

### 7.2. Multiple Trailing Separators
```csharp
// Input: "\\server\share\\"
// Should be normalized and treated as directory path
```

### 7.3. Empty Segments
```csharp
// Input: "\\server\\share" (double separator)
// System.Uri should handle this appropriately
```

## 8. Implementation with Comprehensive Edge Case Handling

```csharp
private string? ProcessUncPath(Uri uri, string originalPath)
{
    var segments = uri.Segments;
    
    // Handle edge case: no meaningful segments
    if (segments.Length == 0)
    {
        return originalPath;
    }
    
    // Handle edge case: only root segment (e.g., "\\server\")
    if (segments.Length == 1)
    {
        // This is likely just a server reference, return original
        return originalPath;
    }
    
    // Get the last segment
    var lastSegment = segments[segments.Length - 1];
    
    // Check if the last segment represents a directory
    // This includes segments that end with '/' or are empty
    if (string.IsNullOrEmpty(lastSegment) || lastSegment.EndsWith("/"))
    {
        // This represents a directory path, not a file path
        // Return original as there's no filename to process for reserved names
        return originalPath;
    }
    
    // This is a file path, process the filename
    string? processedFileName = ProcessFileName(lastSegment);
    
    if (processedFileName == lastSegment)
    {
        // No change needed, return original
        return originalPath;
    }
    
    // Reconstruct with processed filename
    return ReconstructUncPath(uri, segments, processedFileName);
}
```

## 9. Validation of Edge Case Handling

### 9.1. Test Cases for Edge Cases

#### 9.1.1. Server-Only Paths
```csharp
// Test: "\\server" -> "\\server" (unchanged)
// Test: "\\server\" -> "\\server\" (unchanged)
// Test: "file://server" -> "file://server" (unchanged)
```

#### 9.1.2. Server-Share-Only Paths
```csharp
// Test: "\\server\share" -> "\\server\share" (unchanged)
// Test: "\\server\share\" -> "\\server\share\" (unchanged)
// Test: "file://server/share" -> "file://server/share" (unchanged)
```

#### 9.1.3. Valid File Paths
```csharp
// Test: "\\server\share\CON" -> "\\server\share\CON_" (processed)
// Test: "\\server\share\file.txt" -> "\\server\share\file.txt" (unchanged)
```

#### 9.1.4. Complex Paths
```csharp
// Test: "\\server\share\folder\CON.txt" -> "\\server\share\folder\CON_.txt" (processed)
// Test: "\\server\share\folder\" -> "\\server\share\folder\" (unchanged)
```

## 10. Error Handling for Invalid Paths

### 10.1. Malformed UNC Paths
```csharp
// If Uri.TryCreate fails, fall back to existing non-UNC logic
if (!Uri.TryCreate(filename, UriKind.Absolute, out Uri? uri))
{
    // Handle as non-UNC path using existing logic
    return ProcessNonUncPath(filename);
}
```

### 10.2. Invalid URI Components
```csharp
// Additional validation can be added if needed
if (string.IsNullOrEmpty(uri.Host))
{
    // Invalid UNC path without server component
    return originalPath;
}
```

## 11. Performance Considerations

### 11.1. Efficient Edge Case Detection
- Directory vs file detection using string operations is efficient
- No complex parsing required for the determination
- Minimal performance impact

### 11.2. Memory Usage
- Efficient string operations using StringBuilder
- No unnecessary object creation
- Proper disposal of URI objects

## 12. Security Implications

### 12.1. Directory Path Security
- Directory paths are not processed for reserved names (which is correct)
- Prevents potential security issues with directory name processing
- Maintains the same security model as file paths

### 12.2. Reserved Name Processing
- Only applies to actual file names, not directory names
- Maintains all existing security validations
- No new security vulnerabilities introduced

## 13. Cross-Platform Considerations

### 13.1. Different URI Formats
- Handles both `\\server\share` and `file://server/share` formats
- Properly identifies directory vs file paths in both formats
- Consistent behavior across platforms

### 13.2. Path Separator Normalization
- System.Uri normalizes different separator styles
- Consistent segment identification regardless of original format

## 14. Verification Checklist

### 14.1. Edge Case Handling
- [ ] Server-only paths handled correctly (returned unchanged)
- [ ] Server-share-only paths handled correctly (returned unchanged)
- [ ] Valid file paths processed for reserved names
- [ ] Paths ending with separators treated as directories
- [ ] Empty segments handled gracefully

### 14.2. Security Validation
- [ ] Directory paths not processed for reserved names
- [ ] File paths still validated for reserved names
- [ ] All security validations preserved

### 14.3. Cross-Platform Compatibility
- [ ] Different UNC formats handled consistently
- [ ] Path separator differences normalized
- [ ] Behavior consistent across platforms

## 15. Conclusion

The edge case handling strategy ensures that:
- Server-only and server-share-only paths are treated as directory paths and returned unchanged
- Only actual file paths have reserved name processing applied
- All security validations are preserved for actual file names
- The implementation is robust and handles various edge cases gracefully
- Cross-platform compatibility is maintained through System.Uri

This approach provides a comprehensive solution that properly handles edge cases while maintaining all security validations and existing functionality.