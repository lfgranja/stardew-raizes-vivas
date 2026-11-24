# Bug Fix: CodeQL annotations step handles directory output

- **Slug**: codeql-sarif-directory-error
- **Fixed**: 2026-11-01T03:35:00Z
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Updated the inline Python in the "CodeQL annotations" step to treat `codeql-results.sarif` as a directory of `*.sarif` files (CodeQL v4's output layout) instead of a single file, fixing `IsADirectoryError`.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `.github/workflows/quality-gate.yml` | modified | annotations script now globs `*.sarif` inside the directory |
