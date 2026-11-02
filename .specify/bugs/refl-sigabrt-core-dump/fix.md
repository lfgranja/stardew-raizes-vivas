# Bug Fix: refl SIGABRT core dump

- **Slug**: refl-sigabrt-core-dump
- **Fixed**: 2025-11-01T19:16:00-04:00
- **Assessment**: ./assessment.md
- **Status**: not-applied

## Summary

No changes applied in this repository. The crashing binary (`/tmp/refl/bin/Debug/net6.0/refl`) is external to `stardew-raizes-vivas`, and the assessment indicates reproduction/diagnostics are required before a concrete fix can be proposed.

## Changes

| File | Change | Notes |
|------|--------|-------|
| — | none | No files modified in workspace |

## Diff Highlights (optional)

N/A

## Tests Added or Updated

- None

## Local Verification

- Commands run: none (no repo changes)
- Manual checks: Reviewed systemd-coredump report and confirmed binary is outside repo

## Deviations from Assessment

None. Followed assessment: no repo files to change; flagged need for reproduction/clarification.

## Follow-ups

- [NEEDS CLARIFICATION: What does `refl` do / what args were passed?]
- [NEEDS CLARIFICATION: Is source code available for refl?]
- [NEEDS CLARIFICATION: Can we reproduce locally?]
- [NEEDS CLARIFICATION: Runtime versions (`dotnet --info`) at time of crash?]
- Capture managed diagnostics (dotnet-dump/trace) from core or reproduction
- If source becomes available, re-run `/speckit.bug.assess` with details
