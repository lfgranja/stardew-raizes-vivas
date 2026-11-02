# Bug Fix: insp SIGABRT core dump

- **Slug**: insp-sigabrt-core-dump
- **Fixed**: 2026-11-02T01:55:00-04:00
- **Assessment**: ./assessment.md
- **Status**: not-applied

## Summary

No changes applied in this repository. The crashing binary (`/tmp/opencode/insp/bin/Debug/net6.0/insp`) is an external, ephemeral diagnostic tool, not part of `stardew-raizes-vivas`. Per the assessment's preferred remediation, there is nothing to fix in this codebase; the tool has since been rewritten and now exits successfully (exit 0). The unresolved `[NEEDS CLARIFICATION]` items (managed exception type in JIT frames #8/#9, exact invocation environment, whether a null `Type.GetType` result triggered the abort) are environment/diagnostic gaps, not code defects in this repo. The second core dump (PID 1821087) has since been analyzed via `coredumpctl` + `gdb` and confirmed to be the same failure mode as PID 1820007 (identical native stack offsets); it is not a separate bug. The user chose to proceed and record the fix as `not-applied`.

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
- Manual checks: Re-invoked the current `/tmp/opencode/insp` tool under `DOTNET_ROOT` → exit 0, confirming the diagnostic tool is not currently crashing. Confirmed no file under `LivingRoots/` or `LivingRoots.Tests/` is on the crashing process's execution path. Analyzed second core dump (PID 1821087) via `coredumpctl` + `gdb` (thread 1 backtrace) → same native stack offsets as PID 1820007, confirming a single recurring failure mode. Core extracted to `/tmp/opencode/insp.core`.

## Deviations from Assessment

None. Followed assessment: preferred remediation is "No change to stardew-raizes-vivas"; no repo files to change. Flagged the unresolved `[NEEDS CLARIFICATION]` items to the user before proceeding, as required for a `likely valid, needs reproduction` verdict.

## Follow-ups

- [NEEDS CLARIFICATION: Decode JIT frames #8/#9 via SOS/dotnet-dump to confirm the managed exception type.]
- [NEEDS CLARIFICATION: Confirm exact invocation environment at crash time (was `DOTNET_ROOT` set?).]
- [NEEDS CLARIFICATION: Confirm whether the crash was triggered by a null `Type.GetType` result or by ICU/globalization code.]
- [RESOLVED: Second core dump (PID 1821087) analyzed via gdb — same failure mode as PID 1820007; not a separate bug.]
- If a future diagnostic tool is added to this repo, keep reflection calls (`Type.GetType`, property access) null-checked and wrapped in try/catch to avoid unhandled exceptions escalating to `Environment.FailFast`.