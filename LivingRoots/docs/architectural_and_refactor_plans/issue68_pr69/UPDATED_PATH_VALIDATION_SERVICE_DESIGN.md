# Updated PathValidationService Design

## Updated ValidatePathTraversalDepth Method

```csharp
/// <summary>
/// Validates path traversal using depth-based analysis to distinguish between
/// legitimate uses of ".." and malicious path traversal attempts.
/// Also enforces a maximum segment count to prevent resource exhaustion attacks.
/// </summary>
/// <param name="path">The normalized path to validate.</param>
/// <exception cref="ArgumentException">
/// Thrown when path traversal is detected ("Path cannot contain path traversal patterns") or
/// when path has too many segments ("Path contains too many segments").
/// </exception>
private void ValidatePathTraversalDepth(string path)
{
    // Check for standalone "." - this should still be blocked as it represents current directory traversal
    if (path.Equals(".", StringComparison.Ordinal))
    {
        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
    }
    
    // Check for standalone "./" - this should be blocked as it represents current directory navigation
    if (path.Equals("./", StringComparison.Ordinal))
    {
        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
    }
    
    // Block any path that starts with "./" as this represents explicit current directory navigation
    if (path.StartsWith("./", StringComparison.Ordinal))
    {
        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
    }
    
    // Check for standalone "..", "../", or "..\"
    if (path.Equals("..", StringComparison.Ordinal) || 
        path.Equals("../", StringComparison.Ordinal) || 
        path.Equals("..\\", StringComparison.Ordinal))
    {
        throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
    }
    
    // Split into segments ignoring empty parts from repeated separators
    string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
    
    // Add a hard cap to prevent excessive processing of pathological inputs
    // Increased from 100 to allow more reasonable paths while still preventing abuse
    const int MaxSegments = 1000;
    if (segments.Length > MaxSegments)
    {
        throw new ArgumentException("Path contains too many segments", nameof(path));
    }
    
    int depth = 0;
    
    foreach (string segment in segments)
    {
        // Check for integer overflow before decrementing
        if (segment.Equals("..", StringComparison.Ordinal))
        {
            // Prevent integer underflow by checking bounds
            if (depth <= int.MinValue + 1)
            {
                throw new ArgumentException("Path contains invalid depth calculation", nameof(path));
            }
            depth--;
            // If depth goes negative, it means we're trying to go above the intended root
            if (depth < 0)
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }
        else if (!segment.Equals(".", StringComparison.Ordinal))
        {
            // Check for integer overflow before incrementing
            if (depth >= int.MaxValue - 1)
            {
                throw new ArgumentException("Path contains invalid depth calculation", nameof(path));
            }
            // Regular directory/file names increase the depth
            depth++;
        }
        // If segment is ".", we don't change the depth since it refers to current directory
    }
    
    // Remove the arbitrary depth cap of 10 that was limiting legitimate use cases
    // The depth < 0 check already prevents traversal above root
    // This allows deeper, legitimate directory structures
}
```

## Key Changes

1. **Line 36**: Updated error message from "Path cannot contain path traversal patterns" to "Path contains too many segments" for the MaxSegments validation
2. **Documentation**: Added clear documentation explaining the dual purpose of the method (security and performance validation)
3. **Exception specification**: Added detailed documentation about which error messages are thrown in which scenarios

## Error Message Classification

| Scenario | Error Message | Type |
|----------|---------------|------|
| Path has too many segments | "Path contains too many segments" | **[UPDATED]** |
| Actual path traversal (depth < 0) | "Path cannot contain path traversal patterns" | [SECURITY - KEEP] |
| Standalone "." | "Path cannot contain path traversal patterns" | [SECURITY - KEEP] |
| Standalone ".." | "Path cannot contain path traversal patterns" | [SECURITY - KEEP] |
| Absolute paths | "Path cannot be an absolute path or URI" | [SECURITY - KEEP] |
| Encoded traversal | "Path cannot contain encoded path traversal patterns" | [SECURITY - KEEP] |

## Security Preservation

All security functionality remains intact:
- Path traversal detection (depth < 0) still throws the same security message
- Integer overflow/underflow protection maintained
- Absolute path detection unchanged
- Encoded traversal pattern detection unchanged
- Unicode homoglyph protection unchanged
- Standalone "." and ".." blocking unchanged
