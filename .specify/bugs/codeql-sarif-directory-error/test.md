# Bug Verification: CodeQL annotations step

- **Slug**: codeql-sarif-directory-error
- **Tested**: 2026-11-01T03:40:00Z
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The CodeQL job now passes on push to `dev` (run `41269515115`): all jobs green including "Quality Gate / codeql".

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| CI reproduction (pre-fix) | run `41269120163` | fail | IsADirectoryError |
| CI reproduction (post-fix) | run `41269515115` | pass | codeql job success |

## Recommendation

Close the bug — verified in CI.
