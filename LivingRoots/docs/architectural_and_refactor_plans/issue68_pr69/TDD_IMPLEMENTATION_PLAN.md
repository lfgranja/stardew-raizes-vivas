# TDD Implementation Plan for Security Fixes

## Overview
This document outlines the Test-Driven Development approach for implementing security fixes identified in the code review for PR 67 - Rodada 19+.

## Issue 1: Security concerns with filename/path sanitization - Extension handling

### Problem Statement
The `FileNameSanitizationService` has issues with extension handling that may not be consistent with tests expecting ".blocked" extension for dangerous files. The `FindExtensionStartIndex` method needs improvement to correctly identify the last valid extension.

### Test-First Approach

#### Step 1: Write failing tests for current extension handling issues
```csharp
// Test cases to write:
[Test]
public void FindExtensionStartIndex_WithMultipleDotsBeforeExtension_ReturnsCorrectIndex()
{
    // Example: "file...exe" - should not treat "..." as extension
    var result = FindExtensionStartIndex("file...exe");
    Assert.Equal(-1, result); // No valid extension found
}

[Test]
public void FindExtensionStartIndex_WithValidExtensionAfterMultipleDots_ReturnsCorrectIndex()
{
    // Example: "file.txt.exe" - should return index of last extension (.exe)
    var result = FindExtensionStartIndex("file.txt.exe");
    Assert.Equal(8, result); // Index of ".exe"
}

[Test]
public void FindExtensionStartIndex_WithDotAtBeginningAndExtension_ReturnsCorrectIndex()
{
    // Example: ".exe" - should be treated as extension for security purposes
    var result = FindExtensionStartIndex(".exe");
    Assert.Equal(0, result); // Index of ".exe"
}

[Test]
public void Sanitize_WithDangerousExtension_RemovesExtensionAndAddsBlocked()
{
    // Example: "malicious.exe" should become "malicious.blocked"
    var result = service.Sanitize("malicious.exe");
    Assert.Equal("malicious.blocked", result);
}
```

#### Step 2: Implement fix for FindExtensionStartIndex method
- Improve logic to correctly identify valid extensions
- Handle cases with multiple consecutive dots
- Ensure security-focused handling of extensions at the beginning of filenames

#### Step 3: Refactor extension handling in Sanitize method
- Ensure dangerous extensions are properly replaced with ".blocked"
- Maintain consistency with test expectations

### Implementation Details

```csharp
/// <summary>
/// Helper method to find the start index of a valid file extension.
/// </summary>
/// <param name="filename">The filename to check.</param>
/// <returns>The start index of the extension (including the dot) if valid, or -1 if no valid extension found.</returns>
private static int FindExtensionStartIndex(string filename)
{
    // Find the last dot in the filename
    int lastDotIndex = filename.LastIndexOf('.');
    
    if (lastDotIndex >= 0 && lastDotIndex < filename.Length - 1) // Ensure dot is not at the end
    {
        // Check that the last dot is not part of a directory path segment
        // Look for directory separators after the last dot to ensure it's really an extension
        string potentialExtension = filename.Substring(lastDotIndex);
        
        // Check if the extension portion contains directory separators
        if (potentialExtension.Contains('/', StringComparison.Ordinal) || potentialExtension.Contains('\\', StringComparison.Ordinal))
        {
            return -1; // Not a valid extension if it contains path separators
        }
        
        // Extract the part before the last dot (name part)
        string namePartBeforeExtension = filename.Substring(0, lastDotIndex);
        
        // Check if this is a simple dotfile (e.g., ".profile") where the dot is at the beginning
        // This is not considered an extension, but if it's a dangerous extension like ".exe", it should be treated as one
        bool isDotAtBeginning = lastDotIndex == 0 && namePartBeforeExtension.Length == 0;
        bool isDangerousExtension = IsBlockedExtension(potentialExtension);
        
        if (isDotAtBeginning && isDangerousExtension)
        {
            // For security purposes, treat dangerous extensions at the beginning as having an extension
            return lastDotIndex;
        }
        else if (isDotAtBeginning && !isDangerousExtension)
        {
            // For non-dangerous extensions at the beginning (like .profile), don't treat as extension
            return -1;
        }
        
        // Check if all characters before the last dot are dots (like "file....ext")
        // This is likely not a valid extension pattern
        string nameBeforeLastDot = namePartBeforeExtension;
        bool allCharsBeforeAreDots = nameBeforeLastDot.All(ch => ch == '.');
        
        if (allCharsBeforeAreDots && !isDangerousExtension)
        {
            return -1; // Not a valid extension if all chars before are dots
        }
        
        // Extract the part after the dot (excluding the dot itself)
        string extensionPart = potentialExtension.Substring(1);
        
        // Stricter extension qualification: require at least one alphanumeric character in the extension
        bool hasAlphanumeric = extensionPart.Any(c => char.IsLetterOrDigit(c));
        
        if (!hasAlphanumeric)
        {
            return -1; // Not a valid extension if it doesn't contain at least one alphanumeric character
        }
        
        // Make sure the part after the last dot looks like an extension (not part of a directory path)
        if (potentialExtension.IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
        {
            return lastDotIndex;
        }
    }
    
    return -1;
}
```

## Issue 2: Redundant logic in PathValidationService

### Problem Statement
- minDepth tracking and final check are redundant since depth < 0 check already handles traversal
- "ends with .." check is overly restrictive

### Test-First Approach

#### Step 1: Write tests for simplified path validation
```csharp
[Test]
public void ValidatePathTraversalDepth_WithValidPathThatEndsWithDotDot_DoesNotThrow()
{
    // A path like "folder/subfolder/.." is valid if it doesn't go above the root
    // Should not throw after simplification
    var service = new PathValidationService(mockUnicodeService.Object, mockPathValidator.Object);
    // Use reflection to test private method
    var method = typeof(PathValidationService).GetMethod("ValidatePathTraversalDepth", 
        BindingFlags.NonPublic | BindingFlags.Instance);
    
    // This should not throw after the fix
    method.Invoke(service, new object[] { "folder/subfolder/.." });
}
```

#### Step 2: Implement simplified ValidatePathTraversalDepth method
- Remove redundant minDepth tracking
- Remove overly restrictive "ends with .." check
- Keep core traversal logic intact

### Implementation Details

```csharp
/// <summary>
/// Validates path traversal using depth-based analysis to distinguish between
/// legitimate uses of ".." and malicious path traversal attempts.
/// </summary>
/// <param name="path">The normalized path to validate.</param>
/// <exception cref="ArgumentException">Thrown when path traversal is detected.</exception>
private void ValidatePathTraversalDepth(string path)
{
    // Split the path into segments
    string[] segments = path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
    
    int depth = 0;
    
    foreach (string segment in segments)
    {
        if (segment == "..")
        {
            depth--;
            // If depth goes negative, it means we're trying to go above the intended root
            if (depth < 0)
            {
                throw new ArgumentException("Path cannot contain path traversal patterns", nameof(path));
            }
        }
        else if (segment != ".")
        {
            // Regular directory/file names increase the depth
            depth++;
        }
        // If segment is ".", we don't change the depth
    }
}
```

## Issue 3: DataExists method issues

### Problem Statement
- Uses File.Exists instead of SMAPI's ReadJsonFile API
- Has misleading exception handlers for exceptions that won't be thrown
- Inconsistent with LoadData implementation

### Test-First Approach

#### Step 1: Write tests for refactored DataExists
```csharp
[Test]
public void DataExists_WithValidExistingFile_ReturnsTrue()
{
    // Arrange: Setup SMAPI Data helper to return a valid object
    var mockDataHelper = new Mock<IDataHelper>();
    var mockHelper = new Mock<IModHelper>();
    var mockMonitor = new Mock<IMonitor>();
    var mockModLogic = new Mock<IModLogic>();
    
    mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
    mockModLogic.Setup(x => x.SanitizeFileName(It.IsAny<string>())).Returns<string>(s => s);
    mockModLogic.Setup(x => x.ValidatePath(It.IsAny<string>())).Verifiable();
    
    // Return a non-null object to indicate file exists
    mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns(new object());
    
    var service = new ModDataService(mockHelper.Object, mockMonitor.Object, mockModLogic.Object);
    
    // Act
    var result = service.DataExists("test_key");
    
    // Assert
    Assert.True(result);
}

[Test]
public void DataExists_WithNonExistentFile_ReturnsFalse()
{
    // Arrange: Setup SMAPI Data helper to return null for non-existent file
    var mockDataHelper = new Mock<IDataHelper>();
    var mockHelper = new Mock<IModHelper>();
    var mockMonitor = new Mock<IMonitor>();
    var mockModLogic = new Mock<IModLogic>();
    
    mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
    mockModLogic.Setup(x => x.SanitizeFileName(It.IsAny<string>())).Returns<string>(s => s);
    mockModLogic.Setup(x => x.ValidatePath(It.IsAny<string>())).Verifiable();
    
    // Return null to indicate file doesn't exist
    mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns((object)null);
    
    var service = new ModDataService(mockHelper.Object, mockMonitor.Object, mockModLogic.Object);
    
    // Act
    var result = service.DataExists("test_key");
    
    // Assert
    Assert.False(result);
}
```

#### Step 2: Refactor DataExists method
- Use SMAPI's ReadJsonFile API instead of File.Exists
- Remove misleading exception handlers
- Make consistent with LoadData implementation

### Implementation Details

```csharp
/// <summary>
/// Check if data exists for a given key
/// </summary>
/// <param name="key">Key to check</param>
/// <returns>True if data exists, false otherwise</returns>
public bool DataExists(string key)
{
    string sanitizedKey;
    try
    {
        sanitizedKey = GetValidatedAndSanitizedKey(key);
    }
    catch (ArgumentException ex)
    {
        _monitor.Log($"Invalid key provided to DataExists: {ex.Message}", LogLevel.Warn);
        return false;
    }

    try
    {
        string relativePath = GetFilePath(sanitizedKey);
        
        // Use SMAPI's ReadJsonFile API directly instead of File.Exists to be consistent with LoadData
        // This avoids TOCTOU race conditions and maintains consistency
        var result = _helper.Data.ReadJsonFile<object>(relativePath);
        
        // If result is null, the file doesn't exist or contains no valid data
        return result != null;
    }
    catch (System.IO.FileNotFoundException)
    {
        // File not found, so data doesn't exist
        return false;
    }
    catch (System.IO.DirectoryNotFoundException)
    {
        // Directory not found, so data doesn't exist
        return false;
    }
    catch (System.UnauthorizedAccessException)
    {
        // Access denied, but file might exist - return false to be safe
        return false;
    }
    catch (System.IO.IOException)
    {
        // IO error - return false to be safe
        return false;
    }
}
```

## Issue 4: Error handling inconsistencies

### Problem Statement
- RemoveData handles invalid keys differently than SaveData
- Missing null guard checks in ModDataService

### Test-First Approach

#### Step 1: Write tests for consistent error handling
```csharp
[Test]
public void RemoveData_WithInvalidKey_ThrowsArgumentExceptionLikeSaveData()
{
    // Arrange
    var mockHelper = new Mock<IModHelper>();
    var mockMonitor = new Mock<IMonitor>();
    var mockModLogic = new Mock<IModLogic>();
    
    // Setup to throw for invalid path
    mockModLogic.Setup(x => x.ValidatePath(It.IsAny<string>())).Throws<ArgumentException>();
    
    var service = new ModDataService(mockHelper.Object, mockMonitor.Object, mockModLogic.Object);
    
    // Act & Assert
    Assert.Throws<ArgumentException>(() => service.RemoveData("invalid_path"));
}

[Test]
public void RemoveData_WithNullKey_ThrowsArgumentException()
{
    // Arrange
    var mockHelper = new Mock<IModHelper>();
    var mockMonitor = new Mock<IMonitor>();
    var mockModLogic = new Mock<IModLogic>();
    
    var service = new ModDataService(mockHelper.Object, mockMonitor.Object, mockModLogic.Object);
    
    // Act & Assert
    Assert.Throws<ArgumentException>(() => service.RemoveData(null!));
}
```

#### Step 2: Refactor RemoveData method
- Make error handling consistent with SaveData
- Add proper null checks

### Implementation Details

```csharp
/// <summary>
/// Remove data for a given key
/// </summary>
/// <param name="key">Key to remove</param>
public void RemoveData(string key)
{
    if (key == null)
        throw new ArgumentNullException(nameof(key), "Key cannot be null");

    string sanitizedKey = GetValidatedAndSanitizedKey(key);
    
    // Delete data file to properly remove stored data
    var filePath = GetFilePath(sanitizedKey);
    try
    {
        // Use SMAPI's API to write null, which effectively removes the data
        _helper.Data.WriteJsonFile<object>(filePath, null);
        _monitor.Log($"Removed data for key '{sanitizedKey}' by writing null.", LogLevel.Trace);
    }
    catch (System.IO.IOException ex)
    {
        _monitor.Log($"IOException while removing data for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
        throw;
    }
    catch (Exception ex)
    {
        _monitor.Log($"Unexpected error while removing data for key '{sanitizedKey}': {ex.Message}", LogLevel.Error);
        throw;
    }
}
```

## Issue 5: Test robustness concerns

### Problem Statement
- Many tests rely on private method invocation via reflection
- Could create brittle tests tied to implementation details

### Test-First Approach

#### Step 1: Refactor tests to use public API
- Create tests that verify behavior through public methods
- Remove or minimize use of reflection for private methods
- Focus on testing the public contract rather than implementation details

#### Step 2: Add defensive null checks
- Add null checks to public methods in ModDataService

## Implementation Timeline

1. **Week 1**: Implement extension handling fixes with TDD approach
2. **Week 2**: Simplify PathValidationService logic
3. **Week 3**: Refactor DataExists method and make consistent with LoadData
4. **Week 4**: Fix error handling consistency and add null checks
5. **Week 5**: Refactor tests to reduce reliance on reflection
6. **Week 6**: Final testing and validation

## Success Criteria

- All existing tests continue to pass
- New security-focused tests pass
- Extension handling is consistent and secure
- Path validation is simplified without reducing security
- DataExists method is consistent with LoadData
- Error handling is consistent across methods
- Tests are more robust and less dependent on implementation details