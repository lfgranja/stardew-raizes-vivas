# Bug Verification: SIGABRT core dump in `dotnet build` (Release solution build)

- **Slug**: msbuild-release-build-sigabrt
- **Tested**: 2026-11-08T19:55:00Z
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The reported failure (MSBuild solution build dying via SIGABRT/MSB4166 on this swap-exhausted host) does **not** reproduce under the fix's mitigated build path: a fresh `-m:1 --disable-build-servers` build passed (`exit 0`, `Compilação com êxito`) and the full test suite passed (1937/4/0). The unmodified reported command was **not** re-run — re-running it would re-risk this memory-constrained host and is not needed to validate the fix (it is precisely the pre-fix invocation that failed). The residual risk class (host orchestration under memory pressure) cannot be eliminated by this repository.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix, build mitigated) | `GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln --configuration Release --no-restore -m:1 --disable-build-servers` | pass | exit 0, `Compilação com êxito` (1m34s; build against live binaries already OK) |
| Full regression suite | `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release --no-build --verbosity minimal` | pass | `Aprovado! – Com falha: 0, Aprovado: 1937, Ignorado: 4, Total: 1941 (net6.0), 1m28s` |
| Documented gate run (pre-fix failed, post-fix green) | `GamePath=/home/luis/stardew-assemblies scripts/release-gate.sh` | pass | `release-gate-pass14-v4.log`: build OK 19:16:59Z, tests OK 19:18:17Z, smoke OK 19:42:35Z, `ALL STEPS PASSED`, exit 0 |
| Original reported command (pre-fix, unmitigated) | `dotnet build Stardew-LivingRoots.sln --configuration Release --no-restore` | skipped | would re-trigger the exact `MSB4166`/SIGABRT under memory pressure on this host; not needed to validate the mitigation. Not re-tested. |
| New / updated tests | — | not-run | none added by the fix (fix.md); no application code or test changed |
| Lint / format | `dotnet format --verify-no-changes` (inside v4 gate) | pass | unchanged since v4 (HEAD adds only bug-record docs) |
| Build orchestration guard | `bash -n scripts/release-gate.sh`; grep `-m:1`, `--disable-build-servers`, `--no-build` in HEAD | pass | mitigation present; `--help` prints usage |

## Output Excerpts

```
[release-gate 2026-11-08T19:16:59Z] step 3 OK: build (Release)
[release-gate 2026-11-08T19:18:17Z] step 4 OK: tests
[release-gate 2026-11-08T19:42:35Z] ALL STEPS PASSED
```

```
Aprovado!  – Com falha: 0, Aprovado: 1937, Ignorado: 4, Total: 1941 (net6.0)
```

## Residual Risks

- The raw reported command (`dotnet build ... --no-restore` without `-m:1`) is **not** re-tested and remains capable of the same failure under memory pressure on this host (and on any host that spawns 4 default MSBuild nodes under swap exhaustion).
- The `[NEEDS CLARIFICATION]` from the assessment (exact managed trigger for the ABRT) remains open; core dump is root-owned; `MSBuildTemp…njuevI/MSBuild_pid-2621637_*.failure.txt` are 0 bytes.
- Host state here: swap ~100% used; `/tmp` tmpfs ~86% full; the class of failures that killed node 4 cannot be fixed by this repository; a quieter host window or more RAM is the durable mitigation.
- `test.md` is an in-repo orchestration check; it says nothing about runtime behavior under gameplay (Gate 3 covers a different layer).

## Recommendation

Hold as **partial**: the mitigation path is green and standing in the deterministic gate (`ALL STEPS PASSED`, fresh build + suite green post-fix). Of this bug's class, nothing further can be done in-repo. If the SIGABRT recurs on a quieter host window (or the reported command is re-tested intentionally), re-run `/speckit.bug.assess` with the new evidence captured there (especially `DOTNET_DbgEnableMiniDump=1` minidump + `dotnet-dump analyze`), before any SDK downgrade or runtime-level change.
