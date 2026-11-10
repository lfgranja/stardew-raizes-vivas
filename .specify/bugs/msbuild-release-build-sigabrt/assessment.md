# Bug Assessment: SIGABRT core dump in `dotnet build` (MSBuild solution build)

- **Slug**: msbuild-release-build-sigabrt
- **Created**: 2026-11-08T18:52:00Z
- **Source**: pasted text (systemd-coredump report)
- **Verdict**: valid — reproduced on this host (failure reproduced and mitigated; root cause environmental)
- **Severity**: high

## Report (verbatim or summarized)

```
PID: 2621637 (dotnet)
UID/GID: 1000 (luis)
Signal: 6 (ABRT)
Timestamp: Sun 2026-11-08 14:25:36 -04 (17s ago)
Command Line: dotnet build Stardew-LivingRoots.sln --configuration Release --no-restore
Executable: /home/luis/.local/share/mise/dotnet-root/dotnet
Storage: /var/lib/systemd/coredump/core.dotnet.1000.d37ee39f69934ed689ff7c1817db459f.2621637.1794162336000000.zst (present)
Size on Disk: 18.5M
```

Native stack: `abort()` in libc called from `libcoreclr.so` after JIT frames (managed code); module list includes libnss_*/libz/libssl/libicu. No assembly/object of this repository appears in the stack; the process is the .NET SDK `dotnet` host running MSBuild (SDK 10.0.400). The command line matches the exact step-3 command of `scripts/release-gate.sh` (pass 14, v1 attempt).

## Symptom

`dotnet build Stardew-LivingRoots.sln --configuration Release --no-restore` crashed with SIGABRT (PID 2621637) on Fedora 44 (x86_64, hostname `fedora`), .NET SDK 10.0.400. Expected: the Release build either succeeds, or fails with compiler/package diagnostics. It dumped core instead. The observed MSBuild companion error for this family of failure (pass 14, v1) was `MSBUILD : error MSB4166: O nó filho "4" foi encerrado prematuramente. Desligando.` — MSBuild aborted because a child node died prematurely; this crash is that child dying via ABRT mid-build (memory-host-related).

## Reproduction

1. On this host (Fedora 44, 13.9 GB RAM, swap fully used, `/tmp` tmpfs at 85%+ before the incident), run `dotnet build Stardew-LivingRoots.sln --configuration Release --no-restore` when the host is memory-constrained — reproduced: build stops with MSB4166 and the ABRT (same PID/timestamp family in pass 14 v1).
2. Run the identical command with `-m:1 --disable-build-servers` — build passes (`gate_v4_exit=0`, ALL STEPS PASSED, commit `4860cf2`).
3. `[NEEDS CLARIFICATION: exact internal managed exception/assertion — core dump is `root`-owned (0600), not readable by luis; MSBuild failure files `/tmp/MSBuildTempnjuevI/MSBuild_pid-2621637_*.failure.txt` are 0-byte empty.]`

## Suspected Code Paths

- `scripts/release-gate.sh` (in repo, step 3, the invocation) — not the cause of the crash, but the surface that exposed it; informed the `-m:1 --disable-build-servers` fix.
- `Stardew-LivingRoots.sln` / `LivingRoots.csproj` / `LivingRoots.Tests.csproj` — build inputs; nothing in the code is known-bad (the same source built green under `-m:1`).
- `libcoreclr.so` (SDK 10.0.400, `10.0.400/Roslyn`) — the stack's abort site; failing inside the .NET runtime, e.g., fatal failure under resource pressure [NEEDS CLARIFICATION].
- Host memory subsystem — swap exhausted (~8.2 GB in use), `/tmp` tmpfs ~86% full at the time; MSBuild spawns ~4 parallel Roslyn nodes by default; host context identical to this crash.
- NOT in the repository's game/verification content: no Living Roots domain code path executes during build.

## Root Cause Hypothesis

**Confidence: medium-high.** MSBuild solution builds with 4 parallel nodes; on a swap-exhausted, tmpfs-heavy host, a child MSBuild node fails irrecoverably, and the SDK host escalates the failure to an `abort()` from `libcoreclr.so` (this SIGABRT). MSBuild's own message for this failure family is `MSB4166` (child node terminated early). Confirmation: the identical solution builds clean with a single fenced-off node (`-m:1 --disable-build-servers`) and passes Gate 3's local gate; the pre-fix crash timeline (14:24–14:25 -04) aligns with peak memory consumers on this host (Java/Gradle daemon, browser + user shells, `omp`/agent processes). Not an assembly corruption (identical inputs build green).

## Proposed Remediation

**Preferred** (local-only, already in tree):
- Build the solution with `-m:1` (and `--disable-build-servers`) to keep MSBuild node count at 1 — applied in `scripts/release-gate.sh` step 3; verified `gate_v4_exit=0` including full smoke/tests (commit `4860cf2`; recorded in `specs/019-ci-release-gate/verification/verification.md`). Keep this as the documented deterministic build invocation; CI workflows pin equivalent behavior.
- For diagnosing future instances: `MSBUILDDEBUGCONSOLE=1` / `dotnet build -v:diag` to force the child's own error to the console; `DOTNET_DbgEnableMiniDump=1` / `DOTNET_EnableDiagnostics=1` to capture a managed minidump if it recurs; never retry the identical parallel build under memory pressure.
- Avoid running full-solution parallel builds while the host is swap-exhausted (agent and game processes dominate); reclaim tmpfs-only before heavy builds.

**Alternatives**:
- `maxcpucount=1` via `/p:BuildInParallel=false` (equivalent effect; the explicit `-m:1` CLI flag is simpler to see in logs).
- Split the solution build into per-project projects with `--no-dependencies` (not recommended; delays, no benefit over `-m:1`).
- Acquire a bigger `DOTNET_GCHeapHardLimit` or GC gen0 budget (symptom-specific; unnecessary if `-m:1` holds).

**Files likely to change**:
- None required; `scripts/release-gate.sh` already carries `-m:1 --disable-build-servers` (commit `4860cf2`, pass 14). Any future stability fix belongs to the same gate record, not production code.

**Tests to add or update**:
- None — this is a crash-signature diagnosis of a build orchestration issue, not a testable application/domain change. The gate itself (v4 green) is the regression record. If recurrence continues after `-m:1`: capture `DOTNET_DbgEnableMiniDump=1` minidump + `dotnet-dump analyze` with SOS to read the managed frames; revisit severity.

## Risks & Considerations

- High swap-exhausted hosts (this machine) will continue to flake on parallel MSBuild; the safe local fallback exists, but a **host-level fix** (more RAM, reclaiming swap/tmp, not running agent jobs in parallel with heavy builds) is outside this repository's control.
- The failure files `/tmp/MSBuildTempnjuevI/MSBuild_pid-2621637_*.failure.txt` are 0 bytes — nothing survives of the child's reason; don't rely on them for post-mortem.
- The core dump is root-owned; permission meaning unavailable for deep read.
- Recurrence under `-m:1` would change the verdict (then a runtime-defect hypothesis stands and a `dotnet` SDK downgrade is worth consideration).

## Open Questions

- [NEEDS CLARIFICATION: trigger managed exception/assertion (core dump is root-owned; failure files are empty).]
- [NEEDS CLARIFICATION: whether the failure reproduces deterministically on a quieted host with ≥2 GB free RAM (not testable without a quieter host window today).]
