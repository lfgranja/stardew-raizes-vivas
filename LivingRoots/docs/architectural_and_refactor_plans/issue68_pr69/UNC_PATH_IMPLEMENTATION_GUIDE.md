# Implementation Guide: UNC Path Handling with System.Uri

## 1. Overview

This document provides a detailed guide on implementing UNC path handling using `System.Uri` in the `ReservedNameHandler.cs` class. The focus is on properly extracting and processing the filename component while maintaining all security validations.

## 2. System.Uri for UNC Path Processing

### 2.1. Why System.Uri?
- Provides built-in, cross-platform UNC path detection via the `IsUnc` property
- Properly handles various UNC path formats (`\\server\share\file`, `//server/share/file`, etc.)
- Offers structured access to path components through properties like `Segments`, `Host`, and `PathAndQuery`
- Handles edge cases and malformed paths safely

### 2.2. Key Properties for UNC Processing
- `Uri.IsUnc`: Boolean property that reliably identifies UNC paths
- `Uri.Segments`: Array of path segments that can be processed individually
- `Uri.Host`: Server name in UNC paths
- `Uri.Scheme`: Protocol scheme (typically "file" for UNC paths)

## 3. Detailed Implementation Steps

### 3.1. URI Creation and Validation
```csharp
public string? Handle(string? filename)
{
    if (string.IsNullOrEmpty(filename)) return filename;

    // Safely attempt to create a URI from the input path
    if (Uri.TryCreate(filename, UriKind.Absolute, out Uri? uri) && uri.IsUnc)
    {
        // Process as UNC path
        return ProcessUncPath(uri, filename);
    }
    else
    {
        // Process as non-UNC path using existing logic
        return ProcessNonUncPath(filename);
    }
}
```

### 3.2. UNC Path Processing Method
```csharp
private string? ProcessUncPath(Uri uri, string originalPath)
{
    var segments = uri.Segments;
    
    // Validate that we have at least one segment (the filename)
    if (segments.Length == 0)
    {
        // No segments found, return original path
        return originalPath;
    }
    
    // Extract the filename component (last segment)
    string fileNameComponent = segments[segments.Length - 1];
    
    // Process the filename component for reserved names
    string? processedFileName = ProcessFileName(fileNameComponent);
    
    // If no change was made, return the original path
    if (processedFileName == fileNameComponent)
    {
        return originalPath;
    }
    
    // Reconstruct the path with the processed filename
    return ReconstructUncPath(uri, segments, processedFileName);
}
```

### 3.3. Filename Component Extraction

#### 3.3.1. Accessing the Last Segment
The `Segments` property of a `Uri` instance provides an array of path segments. For UNC paths, the last element in this array represents the filename component:

```csharp
// For path: \\server\share\folder\file.txt
// Segments[0] = "/" (root)
// Segments[1] = "server/"
// Segments[2] = "share/"
// Segments[3] = "folder/"
// Segments[4] = "file.txt" <- This is the filename component
var fileNameComponent = segments[segments.Length - 1];
```

#### 3.3.2. Handling Different UNC Path Formats
The `Segments` property normalizes different UNC path formats, making the extraction logic consistent:

- `\\server\share\file.txt` → Segments contain normalized parts
- `//server/share/file.txt` → Same normalized segments
- `file://server/share/file.txt` → Same normalized segments

### 3.4. Path Reconstruction

#### 3.4.1. Using StringBuilder for Efficient Reconstruction
```csharp
private string ReconstructUncPath(Uri uri, string[] originalSegments, string processedFileName)
{
    var builder = new StringBuilder();
    
    // Start with the scheme and server
    builder.Append(uri.Scheme);
    builder.Append("://");
    builder.Append(uri.Host);
    
    // Add all path segments except the last one (original filename)
    for (int i = 0; i < originalSegments.Length - 1; i++)
    {
        builder.Append(originalSegments[i]);
    }
    
    // Add the processed filename
    builder.Append(processedFileName);
    
    return builder.ToString();
}
```

#### 3.4.2. Preserving Path Format
The reconstruction process maintains the original path format while substituting only the processed filename component.

## 4. Processing the Filename Component

### 4.1. Integration with Existing Logic
The extracted filename component is processed using the existing `ProcessFileName` method, which contains all the security validation logic:

```csharp
private string? ProcessFileName(string filename)
{
    // This method contains all the existing security validations:
    // - Reserved Windows filename detection
    // - Homoglyph detection through Unicode normalization
    // - Insignificant character handling
    // - Extension preservation
    // All security validations remain unchanged
}
```

### 4.2. Security Validation Preservation
The filename component extraction and processing maintains all existing security validations:

1. **Reserved Name Detection**: All Windows reserved names (CON, PRN, AUX, etc.) are detected
2. **Homoglyph Detection**: Unicode normalization detects spoofing attempts
3. **Insignificant Character Handling**: Dots, spaces, and tabs are properly handled
4. **Extension Preservation**: File extensions remain intact during processing

## 5. Edge Case Handling

### 5.1. Server-Only UNC Paths
For paths like `\\server` or `\\server\`, the implementation should handle gracefully:

```csharp
if (segments.Length <= 1)
{
    // Path is server-only or has no meaningful filename
    // Return original path as there's nothing to process
    return originalPath;
}
```

### 5.2. Server-Share-Only UNC Paths
For paths like `\\server\share` or `\\server\share\`, the share name becomes the "filename" component but should be handled appropriately:

```csharp
// The share name will be processed as a filename, but since it's not typically a reserved name,
// it will be returned unchanged by ProcessFileName, preserving the original path
```

### 5.3. Malformed UNC Paths
The `Uri.TryCreate` method safely handles malformed paths by returning `false`, allowing the system to fall back to existing non-UNC processing logic.

## 6. Cross-Platform Considerations

### 6.1. Path Separator Handling
`System.Uri` normalizes path separators internally, so the implementation works consistently across platforms:

- Windows: `\\server\share\file.txt`
- Unix: `//server/share/file.txt`
- Both are handled consistently through the `Segments` property

### 6.2. File URI Schemes
The implementation handles various file URI schemes:
- Traditional UNC: `\\server\share\file`
- File URI: `file://server/share/file`

## 7. Performance Optimization

### 7.1. Efficient String Operations
- Use `StringBuilder` for path reconstruction to minimize string allocations
- Process segments directly without unnecessary string manipulations
- Cache URI parsing results only if processing the same paths repeatedly

### 7.2. Minimal Memory Footprint
- Only extract necessary components (filename segment)
- Reuse existing security validation logic
- Avoid creating unnecessary intermediate strings

## 8. Error Handling

### 8.1. Safe URI Creation
```csharp
if (!Uri.TryCreate(filename, UriKind.Absolute, out Uri? uri))
{
    // Invalid URI format, fall back to non-UNC processing
    return ProcessNonUncPath(filename);
}
```

### 8.2. Null Safety
All URI properties are properly checked for null values, and the implementation gracefully handles edge cases.

## 9. Testing Considerations

### 9.1. Unit Test Scenarios
The implementation should be tested with various UNC path formats:
- Basic UNC: `\\server\share\file.txt`
- Nested paths: `\\server\share\folder\subfolder\file.txt`
- Paths with reserved names: `\\server\share\CON.txt`
- Paths with homoglyphs: `\\server\share\CОN.txt`
- Edge cases: `\\server`, `\\server\share`

### 9.2. Integration Testing
- Verify that security validations work correctly with extracted filename components
- Ensure path reconstruction preserves the original format
- Test cross-platform compatibility

## 10. Migration Strategy

### 10.1. Gradual Implementation
1. Implement the new UNC handling logic alongside existing code
2. Add comprehensive tests for the new implementation
3. Gradually replace the old implementation after verification
4. Remove deprecated code once the new implementation is proven stable

### 10.2. Backward Compatibility
- Maintain the same public API
- Preserve all existing behavior for non-UNC paths
- Ensure all existing tests continue to pass

This implementation approach provides a robust, secure, and maintainable solution for UNC path handling using System.Uri while preserving all existing security validations.
