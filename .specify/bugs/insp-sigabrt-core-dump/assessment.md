# Bug Assessment: insp SIGABRT core dump

- **Slug**: insp-sigabrt-core-dump
- **Created**: 2026-11-02T01:40:11-04:00
- **Source**: pasted text (systemd-coredump)
- **Verdict**: likely valid, needs reproduction
- **Severity**: low

## Report (verbatim or summarized)

```
PID: 1820007 (insp)
           UID: 1000 (luis)
           GID: 1000 (luis)
        Signal: 6 (ABRT)
     Timestamp: Mon 2026-11-02 01:40:11 -04 (17s ago)
  Command Line: /tmp/opencode/insp/bin/Debug/net6.0/insp
    Executable: /tmp/opencode/insp/bin/Debug/net6.0/insp
 Control Group: /user.slice/user-1000.slice/user@1000.service/app.slice/kitty-3536-0.scope
          Unit: user@1000.service
     User Unit: kitty-3536-0.scope
         Slice: user.slice
     Owner UID: 1000 (luis)
       Boot ID: dc4df930947646b88fe5af7f355899f7
    Machine ID: c5d5a92fe6f54016b8c383400249c18b
      Hostname: fedora
       Storage: /var/lib/systemd/coredump/core.insp.1000.dc4df930947646b88fe5af7f355899f7.1820007.1793598011000000.zst (present)
  Size on Disk: 2.6M
       Message: Process 1820007 (insp) of user 1000 dumped core.

                Module libicui18n.so.77 from rpm icu-77.1-3.fc44.x86_64
                Module libicudata.so.77 from rpm icu-77.1-3.fc44.x86_64
                Module libicuuc.so.77 from rpm icu-77.1-3.fc44.x86_64
                Stack trace of thread 1820007:
                #0  0x00007f93aaa7bd0c __pthread_kill_implementation (libc.so.6 + 0x74d0c)
                #1  0x00007f93aaa20e8e raise (libc.so.6 + 0x19e8e)
                #2  0x00007f93aaa087b3 abort (libc.so.6 + 0x17b3)
                #3  0x00007f93aa712cc2 n/a (libcoreclr.so + 0x512cc2)
                #4  0x00007f93aa712be2 n/a (libcoreclr.so + 0x512be2)
                #5  0x00007f93aa4da6f5 n/a (libcoreclr.so + 0x2da6f5)
                #6  0x00007f93aa4da7d6 n/a (libcoreclr.so + 0x2da7d6)
                #7  0x00007f93aa48dfc7 n/a (libcoreclr.so + 0x28dfc7)
                #8  0x00007f933096468c n/a (n/a + 0x0)
                #9  0x00007f9330964e97 n/a (n/a + 0x0)
                #10 0x00007f93aa583657 n/a (libcoreclr.so + 0x383657)
                #11 0x00007f93aa3b984b n/a (libcoreclr.so + 0x1b984b)
                #12 0x00007f93aa28efea n/a (libcoreclr.so + 0x8efea)
                #13 0x00007f93aa28f341 n/a (libcoreclr.so + 0x8f341)
                #14 0x00007f93aa2c3dbd n/a (libcoreclr.so + 0xc3dbd)
                #15 0x00007f93aa27720a coreclr_execute_assembly (libcoreclr.so + 0x1b984b)
                #16 0x00007f93aa96a5b1 n/a (libhostpolicy.so + 0x185b1)
                #17 0x00007f93aa96aa41 n/a (libhostpolicy.so + 0x18a41)
                #18 0x00007f93aa96b46c corehost_main (libhostpolicy.so + 0x1946c)
                #19 0x00007f93aa9d23bb n/a (libhostfxr.so + 0x213bb)
                #20 0x00007f93aa9d0f13 n/a (libhostfxr.so + 0x1ff13)
                #21 0x00007f93aa9cb867 hostfxr_main_startupinfo (libhostfxr.so + 0x1a867)
                #22 0x000055a932a458f5 n/a (/tmp/opencode/insp/bin/Debug/net6.0/insp + 0x108f5)
                #23 0x000055a932a45dc0 n/a (/tmp/opencode/insp/bin/Debug/net6.0/insp + 0x10dc0)
                #24 0x00007f93aaa0a681 __libc_start_call_main (libc.so.6 + 0x3681)
                #25 0x00007f93aaa0a798 __libc_start_main@@GLIBC_2.34 (libc.so.6 + 0x3798)
                #26 0x000055a932a39d4a n/a (/tmp/opencode/insp/bin/Debug/net6.0/insp + 0x4d4a)
```

Core dump: `/var/lib/systemd/coredump/core.insp.1000.dc4df930947646b88fe5af7f355899f7.1820007.1793598011000000.zst` (2.6M). Managed stack frames #8/#9 are JIT-compiled (no symbols); no SOS/debugger extension available to decode them. Binary is `/tmp/opencode/insp/bin/Debug/net6.0/insp` (an ad-hoc diagnostic tool, not part of this repository).

## Symptom

Process `insp` (PID 1820007) crashed with SIGABRT under .NET CoreCLR 6.0.36 on Fedora 44 (x86_64) at 2026-11-02 01:40:11 -04. The native stack shows `abort()` invoked from `libcoreclr.so` after JIT-compiled managed frames (#8/#9), consistent with an unhandled managed exception escalating to `Environment.FailFast` (which calls `abort()`), or a runtime assertion. ICU modules (`libicui18n`/`libicudata`/`libicuuc`) are loaded, suggesting globalization-related code may be on the path. The binary is an ad-hoc diagnostic tool living in `/tmp/opencode/insp/`, not part of the `stardew-raizes-vivas` codebase. The tool ran successfully when re-invoked under `DOTNET_ROOT` (exit 0), so the crash is not deterministically reproducible in the current environment.

## Reproduction

1. Run `/tmp/opencode/insp/bin/Debug/net6.0/insp` on Fedora 44 (x86_64) with .NET 6 runtime 6.0.36 [NEEDS CLARIFICATION: exact invocation/environment at crash time — the apphost requires `DOTNET_ROOT` to be set to find the runtime, and the crash may depend on its absence or on a specific env state].
2. Observe crash [NEEDS CLARIFICATION: the specific managed exception/condition that triggered the abort — frames #8/#9 are JIT-compiled and could not be decoded without SOS].
3. Collect core dump as shown.

## Suspected Code Paths

- `/tmp/opencode/insp/Program.cs` (external, ephemeral) — the only managed code in the crashing process. At crash time it was a reflection inspector over `StardewModdingAPI.Mod` (per the source captured in the bug report); the current on-disk version has since been rewritten to load `LivingRoots.dll` and print its SMAPI references, so the exact crashing source is no longer available [NEEDS CLARIFICATION].
- `libcoreclr.so` internals — runtime abort (`Environment.FailFast`/assertion) [NEEDS CLARIFICATION: managed exception type].
- ICU libraries present — possible globalization/encoding issue [NEEDS CLARIFICATION].
- Not in this repository: no file under `LivingRoots/` or `LivingRoots.Tests/` is on the crashing process's execution path. `LivingRoots.dll` is only referenced by the tool, not loaded/exercised during the crash (the tool never instantiates mod types).

## Root Cause Hypothesis

**Confidence: low.** The native stack is consistent with an unhandled managed exception in the `insp` tool's `Main` escalating to `Environment.FailFast` → `abort()` → SIGABRT, or with a runtime assertion. The most plausible managed trigger is `Type.GetType("StardewModdingAPI.Mod, StardewModdingAPI")` returning `null` (assembly load failure / type not found in the reflection-inspector version of the tool), followed by dereferencing `t.FullName` → `NullReferenceException`, unhandled on the main thread. ICU modules on the stack suggest globalization may be involved but is not conclusive. The tool is external to `stardew-raizes-vivas`, has since been rewritten, and now runs cleanly (exit 0), so the crash cannot be reproduced in the current environment. No code in this repository is implicated.

## Proposed Remediation

**Preferred**: No change to `stardew-raizes-vivas`. The crashing binary is an external, ephemeral diagnostic tool (`/tmp/opencode/insp/`) that has already been rewritten and now exits successfully. If the crash recurs, capture managed diagnostics: run with `DOTNET_DbgEnableMiniDump=1` / `DOTNET_EnableDiagnostics=1`, or install `dotnet-dump` and run `dotnet dump analyze <core>` with the SOS extension, to decode JIT frames #8/#9 and confirm the managed exception type. Keep any future diagnostic tool's `Type.GetType`/reflection calls null-checked.

**Alternatives**:
- Add null-guards and try/catch around reflection in the diagnostic tool (defensive; not a repo change).
- Run the tool via `dotnet run` (SDK-provided runtime) instead of the apphost to avoid runtime-resolution failures.

**Files likely to change**:
- None in this repository.

**Tests to add or update**:
- None applicable.

## Risks & Considerations

- Core dump is 2.6M but managed frames could not be decoded (no SOS/dotnet-dump available); managed exception type unknown.
- The crashing source version of `insp` no longer exists on disk (rewritten), limiting reproducibility.
- Not related to the `stardew-raizes-vivas` codebase; no production/mod code is affected.
- A second core dump exists (PID 1821087) from the same binary; not analyzed here.

## Open Questions

- [NEEDS CLARIFICATION: What was the exact managed exception (decode JIT frames #8/#9 via SOS/dotnet-dump)?]
- [NEEDS CLARIFICATION: Exact invocation/environment at crash time (was `DOTNET_ROOT` set)?]
- [NEEDS CLARIFICATION: Was the crash triggered by a null `Type.GetType` result, or by ICU/globalization code?]
- [NEEDS CLARIFICATION: Is the second core dump (PID 1821087) a separate instance of the same failure?]

Slug: insp-sigabrt-core-dump
Path: .specify/bugs/insp-sigabrt-core-dump/assessment.md
Verdict: likely valid, needs reproduction
Severity: low
Next suggested step: `/speckit.bug.fix slug=insp-sigabrt-core-dump`