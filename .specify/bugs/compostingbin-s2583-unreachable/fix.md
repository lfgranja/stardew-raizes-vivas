# Bug Fix: SonarCloud S2583 — unreachable condition in CollectCompost

- **Slug**: compostingbin-s2583-unreachable
- **Fixed**: 2026-11-01T04:15:00Z
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Removed the redundant `harvested` flag and the always-true `if (harvested)` guard in `CollectCompost`, replacing them with the unconditional success path. This eliminates the unreachable code flagged by SonarCloud rule S2583.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Services/CompostingBinService.cs` | modified | Removed `harvested` variable, its assignment, and the redundant guard |

## Local Verification

- `dotnet build LivingRoots/LivingRoots.csproj -c Release` → 0 errors.
- `dotnet build LivingRoots.Tests/LivingRoots.Tests.csproj -c Release` → 0 errors.

## Deviations from Assessment

None.

## Follow-ups

- Add tests covering CollectCompost success and inventory-full paths.
