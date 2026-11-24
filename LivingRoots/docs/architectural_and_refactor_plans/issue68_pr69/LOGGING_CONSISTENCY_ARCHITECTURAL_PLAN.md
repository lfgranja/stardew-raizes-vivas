# Architectural Plan: Logging Consistency in ModDataService

This document outlines the architectural plan to ensure logging consistency for `DirectoryNotFoundException` in the `DataExists` method of `ModDataService.cs`.


## 1. Analysis of `DirectoryNotFoundException` Handling

**Objective:** Verify and ensure the logging of `DirectoryNotFoundException` within the `DataExists` method uses `LogLevel.Trace` for consistency with other file-not-found scenarios.

**Analysis of Current State:**
A review of the `ModDataService.cs` file shows that the `DataExists` method already handles `DirectoryNotFoundException` by logging at the `LogLevel.Trace` level.

**Relevant Code Snippet (`DataExists` method):**
```csharp
catch (System.IO.DirectoryNotFoundException)
{
    // Directory does not exist - log as trace to reduce noise and for consistency
    _monitor?.Log($"No valid data found for key '{sanitizedKey}'", LogLevel.Trace);
    return false;
}
```

**Conclusion:** The current implementation is already consistent with the desired logging behavior for non-critical exceptions like `DirectoryNotFoundException` and `FileNotFoundException`. Therefore, **no code changes are required** to meet this part of the requirement. The logging is correctly configured to avoid excessive noise for expected conditions.


## 2. Security Best Practices

**Objective:** Ensure that the logging for `DirectoryNotFoundException` does not reveal sensitive information about the file system.

**Analysis of Current State:**
The current implementation adheres to security best practices by:

- **Avoiding Information Disclosure:** The log message for `DirectoryNotFoundException` is generic (`No valid data found for key '{sanitizedKey}'`) and does not include the full file path or raw exception details. This prevents attackers from discovering the directory structure of the system.
- **Using `LogLevel.Trace`:** Logging this exception at the `Trace` level ensures that it is only recorded when verbose logging is explicitly enabled, reducing the risk of accidental information leakage in production environments.
- **Sanitizing Input:** The `sanitizedKey` is used in the log message, which means any potentially malicious input from the original `key` has been cleaned, preventing log injection attacks.

**Conclusion:** The current exception handling and logging strategy for `DirectoryNotFoundException` is secure and does not require any changes. It effectively balances the need for debugging information with the principle of least information disclosure.


## 3. Alignment with Design Principles

**Objective:** Verify that the current implementation aligns with key software design principles.

**Analysis of Current State:**

- **SOLID Principles:**
 - **Single Responsibility Principle (SRP):** The `ModDataService` has a clear, single responsibility: managing mod data persistence. The logging logic is encapsulated within the error handling blocks of each method, which is appropriate.
  - **Open/Closed Principle (OCP):** The service is closed for modification regarding its core logic but open for extension through its dependencies (e.g., `IModHelper`, `IMonitor`, `IModLogic`).
  - **Liskov Substitution Principle (LSP):** The service correctly implements the `IModDataService` interface, and any derived classes would be substitutable.
  - **Interface Segregation Principle (ISP):** The `IModDataService` interface is focused and does not force implementers to depend on methods they don't use.
  - **Dependency Inversion Principle (DIP):** The service depends on abstractions (`IModHelper`, `IMonitor`, `IModLogic`) rather than concrete implementations, promoting loose coupling.

- **DRY (Don't Repeat Yourself):** The service reuses the `GetValidatedAndSanitizedKey` method across `SaveData`, `LoadData`, `DataExists`, and `RemoveData`, preventing code duplication for key validation and sanitization.

- **KISS (Keep It Simple, Stupid):** The implementation is straightforward and avoids unnecessary complexity. Each method has a clear, single purpose.

- **YAGNI (You Aren't Gonna Need It):** The service only implements the required methods (`SaveData`, `LoadData`, `DataExists`, `RemoveData`) and does not include speculative features.

- **DDD (Domain-Driven Design):** The service correctly depends on domain abstractions (`IModLogic`) rather than infrastructure concerns, aligning with DDD principles. The separation of concerns between the service layer and domain logic is maintained.

**Conclusion:** The current implementation adheres well to these design principles. No refactoring is necessary to improve its alignment with these principles in the context of this specific logging consistency task.


## 4. Maintaining Existing Functionality

**Objective:** Ensure that all existing functionality remains intact.

**Analysis of Current State:**
Since no code changes are being made to address the logging consistency issue (as it was already consistent), all existing functionality is inherently preserved. The `DataExists` method continues to:

- Validate and sanitize the input key.
- Attempt to read the data file using SMAPI's API.
- Return `true` if data exists and is valid, `false` otherwise.
- Handle various exceptions (e.g., `FileNotFoundException`, `DirectoryNotFoundException`, `UnauthorizedAccessException`, `IOException`, `JsonException`) appropriately by returning `false` or handling them as per the existing logic.
- Log exceptions according to the established patterns.

**Conclusion:** No changes are required to maintain existing functionality, as no changes are being made to the code. The existing behavior and contract of the `DataExists` method remain unchanged.


## 5. Test Updates

**Objective:** Ensure that existing tests cover the `DirectoryNotFoundException` scenario in `DataExists` and that no test updates are required.

**Analysis of Current State:**
Reviewing the `ModDataServiceTests.cs` file, I have identified the following relevant test:

- `DataExists_WithDirectoryNotFoundException_ReturnsFalse` (lines 250-263 in the provided file content): This test correctly verifies that `DataExists` returns `false` when a `DirectoryNotFoundException` is thrown by the underlying SMAPI API. It sets up the mock to throw a `DirectoryNotFoundException` and asserts that the method returns `false`.

This test correctly validates the behavior of the `DataExists` method for this specific exception. Since the logging level for this exception is already `LogLevel.Trace` (as confirmed in section 1), and the test verifies the functional outcome (returning `false`), **no test updates are required**.

**Conclusion:** The existing test coverage for `DirectoryNotFoundException` in `DataExists` is adequate. No new tests need to be added or existing tests modified.


## 6. Verification Strategy

**Objective:** Define how to verify that the current implementation meets the requirements without needing code changes.

**Verification Steps:**

1.  **Code Review:**
    - Confirm that the `catch (System.IO.DirectoryNotFoundException)` block in the `DataExists` method logs with `LogLevel.Trace`.
    - Verify that the log message is generic and does not expose file system details (e.g., uses `No valid data found for key '{sanitizedKey}'`).
    - Ensure the method returns `false` after logging the exception.

2.  **Test Execution:**
    - Run the existing unit tests in `ModDataServiceTests.cs`, specifically `DataExists_WithDirectoryNotFoundException_ReturnsFalse`.
    - Confirm that all tests pass, verifying the functional behavior of the method under this exception condition.

3.  **Static Analysis:**
    - Use a static analysis tool (if available) to confirm that the log level used for `DirectoryNotFoundException` in `DataExists` matches that used in `LoadData` for similar exceptions (e.g., `FileNotFoundException`, `DirectoryNotFoundException`).

4.  **Security Verification:**
    - Manually inspect the log messages generated by the `DataExists` method when a `DirectoryNotFoundException` occurs to ensure they do not contain sensitive file system paths or raw exception details.

**Conclusion:** The verification strategy confirms that the current implementation already satisfies the requirements for logging consistency, security, and functionality. No code modifications are necessary.


## 7. Conclusion and Next Steps

This architectural plan confirms that the `DataExists` method in `ModDataService.cs` already exhibits the desired logging behavior for `DirectoryNotFoundException`, using `LogLevel.Trace`. The current implementation is consistent with other methods, maintains security best practices, aligns with software design principles, preserves all existing functionality, and is covered by existing tests.

### Summary of Findings:
- **Logging Consistency:** Already consistent.
- **Security:** Adequately protected against information disclosure.
- **Design Principles:** Well-aligned.
- **Functionality:** Preserved.
- **Testing:** Adequately covered.

### Recommended Action:
No code changes are required. The task can be considered complete based on the verification that the current state already meets all requirements.

### Optional Next Step:
If desired, a developer could enter implementation mode to run the verification steps outlined in Section 6 to confirm the findings of this plan.
