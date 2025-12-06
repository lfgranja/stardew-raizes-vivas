### Architectural Plan: Refactoring UNC Path Handling in `ReservedNameHandler.cs`

#### 1. Introduction

This document outlines the architectural plan to refactor the UNC path handling logic within the `ReservedNameHandler.cs` class. The current implementation relies on manual string manipulation to detect and handle UNC paths, which is complex, error-prone, and not guaranteed to be cross-platform. The proposed solution is to leverage the `System.Uri` class, which provides a robust and cross-platform way to parse and handle UNC paths.

#### 2. Problem Statement

The existing `IsUncPath` and `HandleUncPath` methods in `ReservedNameHandler.cs` manually check for `//` or `\\` prefixes to identify UNC paths. This approach has several drawbacks:

*   **Complexity**: The manual parsing logic is verbose and difficult to maintain.
*   **Lack of Robustness**: It may not correctly handle all valid UNC path formats.
*   **Platform Dependency**: It assumes a Windows-style path format, which may not be reliable across different operating systems.

#### 3. Proposed Solution

The core of the proposed solution is to replace the manual UNC path handling with the `System.Uri` class. This class is part of the .NET standard library and is designed to handle various URI schemes, including `file://`, which is used for local and UNC paths.

The new implementation will follow these steps:

1.  **Path Parsing with `System.Uri`**: The `Handle` method will use `Uri.TryCreate` to attempt to create a `Uri` object from the input path. This method is safe and will not throw an exception for invalid paths.
2.  **UNC Path Detection**: If a `Uri` object is successfully created, the `IsUnc` property will be used to determine if the path is a UNC path.
3.  **Component Extraction**: For UNC paths, the `Segments` property of the `Uri` object will be used to extract the individual components of the path (server, share, and subsequent directories/files).
4.  **Reserved Name Handling**: The last segment (the filename) will be processed by the existing `ProcessFileName` method to handle reserved names.
5.  **Path Recomposition**: The modified filename will be combined with the other path segments to reconstruct the full UNC path.

#### 4. Design Principles Compliance

This refactoring will adhere to the following design principles:

*   **SOLID**:
    *   **Single Responsibility Principle (SRP)**: The `ReservedNameHandler` will remain focused on handling reserved names, while the `System.Uri` class will be responsible for path parsing.
    *   **Dependency Inversion Principle (DIP)**: The `ReservedNameHandler` will continue to depend on the `IUnicodeNormalizationService` abstraction.
*   **DRY (Don't Repeat Yourself)**: The manual and redundant UNC path logic will be eliminated.
*   **KISS (Keep It Simple, Stupid)**: The new implementation will be significantly simpler and easier to understand.
*   **YAGNI (You Ain't Gonna Need It)**: The solution focuses only on the required functionality without adding unnecessary features.
*   **DDD (Domain-Driven Design)**: The `ReservedNameHandler` remains a clear component of the domain logic.

#### 5. Implementation Details

The following changes will be made to `ReservedNameHandler.cs`:

*   The `IsUncPath` and `HandleUncPath` methods will be removed.
*   The `Handle` method will be updated to use `System.Uri` as described above.

Here is a conceptual example of the new `Handle` method:

```csharp
public string? Handle(string? filename)
{
    if (string.IsNullOrEmpty(filename)) return filename;

    if (Uri.TryCreate(filename, UriKind.Absolute, out Uri? uri) && uri.IsUnc)
    {
        var segments = uri.Segments;
        if (segments.Length > 0)
        {
            var lastSegment = segments[segments.Length - 1];
            var processedSegment = ProcessFileName(lastSegment);

            if (processedSegment != lastSegment)
            {
                // Reconstruct the UNC path with the modified filename
                var newPath = string.Concat(uri.Scheme, "://", uri.Host, string.Concat(segments.Take(segments.Length - 1)), processedSegment);
                return newPath;
            }
        }
    }
    else
    {
        // Existing logic for non-UNC paths
        string directoryPath = Path.GetDirectoryName(filename) ?? string.Empty;
        string fileName = Path.GetFileName(filename);

        if (string.IsNullOrEmpty(fileName)) return filename;

        string? processedFileName = ProcessFileName(fileName);

        if (processedFileName == null || processedFileName == fileName)
            return filename;

        if (!string.IsNullOrEmpty(directoryPath))
        {
            return Path.Combine(directoryPath, processedFileName);
        }

        return processedFileName;
    }

    return filename;
}
```

#### 6. Verification and Testing

The existing unit tests in `ReservedNameHandlerTests.cs` will be updated to reflect the new implementation. New tests will be added to cover the following scenarios:

*   Valid UNC paths with and without reserved names.
*   UNC paths with various numbers of segments.
*   Non-UNC paths (to ensure existing functionality is not broken).
*   Invalid paths that should not be processed as UNC paths.

This comprehensive testing strategy will ensure that the refactoring improves maintainability without compromising security or functionality.