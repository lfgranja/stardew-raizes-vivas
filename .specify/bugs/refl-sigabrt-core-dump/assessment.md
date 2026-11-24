# Bug Assessment: refl SIGABRT core dump

- **Slug**: refl-sigabrt-core-dump
- **Created**: 2025-11-01T19:14:34-04:00
- **Source**: pasted text (systemd-coredump)
- **Verdict**: likely valid, needs reproduction
- **Severity**: medium

## Report (verbatim or summarized)

```
PID: 1540362 (refl)
           UID: 1000 (luis)
           GID: 1000 (luis)
        Signal: 6 (ABRT)
     Timestamp: Sun 2026-11-01 19:14:34 -04 (9s ago)
  Command Line: /tmp/refl/bin/Debug/net6.0/refl
    Executable: /tmp/refl/bin/Debug/net6.0/refl
 Control Group: /user.slice/user-1000.slice/user@1000.service/app.slice/kitty-3536-0.scope
          Unit: user@1000.service
     User Unit: kitty-3536-0.scope
         Slice: user-1000.slice
     Owner UID: 1000 (luis)
       Boot ID: dc4df930947646b88fe5af7f355899f7
    Machine ID: c5d5a92fe6f54016b8c383400249c18b
      Hostname: fedora
       Storage: /var/lib/systemd/coredump/core.refl.1000.dc4df930947646b88fe5af7f355899f7.1540362.1793574874000000.zst (present)
  Size on Disk: 4.2M
       Message: Process 1540362 (refl) of user 1000 dumped core.
```

Stack trace shows abort in libcoreclr.so (managed runtime) with frames from CoreCLR internals; modules libicui18n/libicudata/libicuuc loaded. Binary is /tmp/refl/bin/Debug/net6.0/refl (not in this repo).

## Symptom

Process `refl` (PID 1540362) crashed with SIGABRT under .NET CoreCLR on Fedora. Expected to run to completion; instead aborted via `abort()` called from CoreCLR internals.

## Reproduction

1. Run `/tmp/refl/bin/Debug/net6.0/refl` on Fedora (x86_64) with .NET 6 runtime [NEEDS CLARIFICATION: exact invocation args]
2. Observe crash [NEEDS CLARIFICATION: conditions triggering abort]
3. Collect core dump as shown

## Suspected Code Paths

- `/tmp/refl/...` — external binary not present in repo [NEEDS CLARIFICATION: source unavailable]
- `libcoreclr.so` internals — runtime abort (assertion failure / managed exception unhandled in certain paths) [NEEDS CLARIFICATION]
- ICU libraries present — possible globalization/encoding issue? [NEEDS CLARIFICATION]

## Root Cause Hypothesis

**Confidence: low.** Core dump indicates managed runtime abort (likely assertion or unhandled exception leading to Environment.FailFast/abort). Without source or reproduction args, cannot pinpoint. Could be reflection-heavy code hitting runtime limits, or environment issue (ICU, runtime version). The binary lives in /tmp (ephemeral) suggesting ad-hoc tool.

## Proposed Remediation

**Preferred**: Gather more context - reproduce with args, get managed stack trace if possible (dotnet-dump, or run with DOTNET_DbgEnableMiniDump/DOTNET_EnableDiagnostics), check runtime version (`dotnet --info`), and see if it's a known issue. If source exists elsewhere, bisect.

**Alternatives**:
- Run with `COMPlus_StackTrace=1` / environment diagnostics to get more info
- Try different runtime version

**Files likely to change**:
- None in this repo (external binary)

**Tests to add or update**:
- None applicable

## Risks & Considerations

- Core dump is large (4.2M) but may contain useful info
- Binary is in /tmp - may be deleted
- Not related to Stardew-LivingRoots codebase

## Open Questions

- [NEEDS CLARIFICATION: What does `refl` do / what args were passed?]
- [NEEDS CLARIFICATION: Is source code available for refl?]
- [NEEDS CLARIFICATION: Can we reproduce locally?]
- [NEEDS CLARIFICATION: Runtime versions (dotnet --info) at time of crash?]

Slug: refl-sigabrt-core-dump  
Path: .specify/bugs/refl-sigabrt-core-dump/assessment.md  
Verdict: likely valid, needs reproduction  
Severity: medium  
Next suggested step: `/speckit.bug.fix slug=refl-sigabrt-core-dump`
