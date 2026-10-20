# Checklist: SMAPI Integration & Log Analysis

**Purpose**: Validate that the script correctly interacts with SMAPI, accurately parses logs, and properly detects mod loading outcomes
**Created**: 2026-10-19
**Feature**: [spec.md](../spec.md) · [research.md](../research.md)

---

## 1. SMAPI Automation Environment Variables

- [x] 1.1 Set `SMAPI_NO_TERMINAL=true` to prevent terminal launch in headless environment
- [ ] 1.2 Set `SMAPI_USE_CURRENT_SHELL=true` to capture all output from the launching shell — **INERT**: the variable is read **only** by SMAPI's installer wrapper (`src/SMAPI.Installer/assets/unix-launcher.sh`), never by `StardewModdingAPI.dll`. Verified against the installed SMAPI 4.5.2 binary: the only `SMAPI_*` string literals are `SMAPI_NO_TERMINAL`, `SMAPI_MODS_PATH` and `SMAPI_DEVELOPER_MODE`. FR-001 launches the apphost directly, bypassing the wrapper, so the variable is a no-op; and its stated rationale ("capture all output from the launching shell") is unachievable anyway, since `--no-terminal`/`SMAPI_NO_TERMINAL` already disable console output. Pending the round-7 decision on whether FR-001/FR-015 keep listing it.
- [x] 1.3 Set `SMAPI_DEVELOPER_MODE=true` to enable TRACE-level logging for diagnostics — it gates **console** verbosity only; SMAPI writes TRACE-level entries to the log file unconditionally, so the log-file diagnostics do not depend on it (FR-001; spec.md:227)
- [x] 1.4 All three environment variables are exported before game launch
- [x] 1.5 `DISPLAY` environment variable is set to the Xvfb display before launch
- [x] 1.6 Environment variables are documented in `--help` output (FR-015 — "Environment (set automatically)" section)

---

## 2. SMAPI Log File Detection

- [x] 2.1 Script reads log from `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` (Linux convention)
- [x] 2.2 Script checks for `SMAPI-crash.txt` first (presence indicates crash → immediate failure)
- [x] 2.3 If `SMAPI-crash.txt` is present, it overrides `SMAPI-latest.txt` as the failure source
- [x] 2.4 Script waits for the current run's `Log started at {UTC} UTC` sentinel before polling, and evaluates every signal only against log lines that follow it (SMAPI unlinks and recreates the log at launch, so the sentinel — not the file's existence — marks the run boundary; FR-002). The sentinel's exact form is `Log started at {DateTime.UtcNow:s} UTC` → `Log started at 2026-10-19T21:01:38 UTC` — it carries **no offset**, so the comparison against `T0` MUST treat it as UTC explicitly (append `Z` when parsing) rather than parsing it as local time
- [x] 2.5 Log file path is configurable (not hardcoded) for non-standard installations
- [x] 2.6 Script handles the case where the log file never appears (timeout → failure) — and the case where the game exits with status `0` but produces no log (SMAPI's post-update dependency re-assertion), which is also a failure (FR-002)
- [x] 2.7 Pre-launch cleanup removes a stale `SMAPI-latest.txt`, `SMAPI-crash.txt` **and the leftover crash marker** (`smapi-internal/StardewModdingAPI.crash.marker`) — the **marker** is what gates SMAPI's blocking `Console.ReadKey()` (`LogManager.HandleMarkerFiles()` tests for the marker's presence), so removing only the crash log leaves the marker in place and the headless launch still hangs. FR-002 "Pre-launch cleanup (MUST)"
- [x] 2.8 The log's inode is recorded before launch and verified to have changed once the log is recreated (belt-and-braces freshness assertion; FR-002)

---

## 3. Log Format Parsing

- [x] 3.1 Log entries are parsed using the `[HH:mm:ss LEVEL SOURCE] message` format — the level field is padded to **5** characters (SMAPI renders it with `PadRight(Monitor.MaxLevelLength)`, and `MaxLevelLength` is the length of the longest public `LogLevel` name: `Trace`/`Debug`/`Error`/`Alert` = 5) followed by a **single** separator space; lines are CRLF-terminated, so matching is substring-based with the trailing `\r` stripped. A real mod-scoped line reads `[14:32:02 ERROR Stardew Valley - Living Roots] …` — the level lives **inside** the bracket group, so there is no `[ERROR]` token and the level must be matched as a column. FR-003
- [x] 3.2 Mod-specific entries are filtered by manifest name `Stardew Valley - Living Roots` in the bracket column
- [x] 3.3 All 6 log levels are recognized: TRACE, DEBUG, INFO, WARN, ERROR, ALERT
- [x] 3.4 Only ERROR and ALERT level entries are treated as failure markers
- [x] 3.5 WARN-level entries are logged but do NOT fail the smoke test
- [x] 3.6 TRACE-level entries are captured for diagnostics only and do NOT fail the test
- [x] 3.7 Parsing handles multi-line entries (stack traces following Error lines)
- [x] 3.8 Parsing handles entries with special characters in messages (paths, exception names)

---

## 4. Success Marker Detection

- [x] 4.1 The mod's marker string is: `The 'Stardew Valley - Living Roots' mod was loaded successfully!`
- [x] 4.2 The mod's marker is emitted by the **mod**, at `ModController.cs:959`, on SMAPI's `GameLaunched` event — it appears only after the game reaches the title screen, which requires a working graphics device
- [x] 4.3 The mod's marker is detected and **reported**, but does **not** gate success (FR-003, FR-006) — gating on it would couple the test to a working graphics device and misreport a graphics failure as a timeout
- [x] 4.4 The mod's marker match is case-sensitive (exact match) and CRLF-tolerant (substring match, trailing `\r` stripped)
- [x] 4.5 The mod's marker is searched only in mod-specific entries (not SMAPI core messages)

---

## 4b. Mod-Resolution Gate (authoritative success signal)

- [x] 4b.1 The script requires a `Loaded N mods:` block in the log — note that SMAPI writes `Loaded 0 mods.` (**period**, no colon) when zero mods load, so the block must be matched as `Loaded [0-9]+ mods[.:]` rather than the colon form alone, or a "0 mods loaded" state is indistinguishable from "no block at all"
- [x] 4b.2 The mod's full manifest name (`Stardew Valley - Living Roots`) appears within one of that block's per-mod entry lines, matched as a **full-name substring** — SMAPI renders entries as `   <Name> <Version> by <Author>` and the name contains spaces, so a first-token comparison would silently fail
- [x] 4b.3 The mod does **not** appear in SMAPI's `Skipped mods` section; when it does, SMAPI's stated reason (e.g. "because it needs SMAPI X or later") is surfaced in the failure report
- [ ] 4b.4 The log's `Mods go here: <path>` line is used to confirm which mods directory SMAPI actually resolved — **UNRESOLVED**: the line is `$"Mods go here: {PathUtilities.AnonymizePathForDisplay(modsPath)}"`, and `AnonymizePathForDisplay` replaces the `$HOME` prefix with `~` — so the log reads `Mods go here: ~/.steam/steam/steamapps/common/Stardew Valley/Mods`, **not** the absolute path the script resolved, and a literal comparison fails. The line is also written **before** the `Log started at` sentinel, so FR-002's "evaluate every signal only after the sentinel" rule forbids reading it at all. Two fixes are needed together: `~`-expansion of the expected path, and an explicit exemption of the header region from the post-sentinel rule. Pending the round-7 decision.
- [ ] 4b.5 The log's `SMAPI <version> with Stardew Valley <version> on <os>` banner is parsed to report the installed SMAPI version — **UNRESOLVED**: the banner is real and its format is correct, but SMAPI's `LogManager.LogIntro` writes it **first**, then `Mods go here:`, and only then the `Log started at` sentinel — so FR-002's "evaluate every signal only after the sentinel" rule forbids reading it. The banner must be explicitly exempted from the post-sentinel rule (or the sentinel rule restated as "after the sentinel, except the header region"). Pending the round-7 decision.

---

## 5. Pass/Fail Logic (AND Conjunction)

- [x] 5.1 Pass requires ALL: the mod is listed in `Loaded N mods:`, is absent from `Skipped mods`, AND no Error/Alert entries from the mod exist
- [x] 5.2 The mod absent from `Loaded N mods:` (or present in `Skipped mods`) → failure (detected load error or skip)
- [x] 5.3 Mod listed, not skipped, no Error/Alert entries → pass (clean load)
- [x] 5.4 Mod listed but Error/Alert entries also present → failure (errors override the load signal)
- [x] 5.5 No `Loaded N mods:` block at all → failure (mod never loaded)
- [x] 5.6 The AND conjunction eliminates false positives in both directions
- [x] 5.7 Save check is deferred (FR-004) and does NOT gate success in current implementation
- [x] 5.8 All signals are evaluated only against log lines after the current run's `Log started at` sentinel, so a stale log from an earlier successful run cannot produce a false pass (FR-002, SC-011)

---

## 6. SMAPI Mod Loading Lifecycle

- [x] 6.1 Script allows sufficient time for SMAPI to load all mods (polls, doesn't assume instant readiness)
- [x] 6.2 Script detects when SMAPI has finished loading mods (the `Loaded N mods:` block appears)
- [x] 6.3 Script distinguishes between "mod failed to load" (Error entries) and "mod not found" (absent from the `Loaded N mods:` block)
- [x] 6.4 Script handles SMAPI's automatic error recovery (mod marked failed but game still launches)
- [x] 6.5 Script does not confuse SMAPI core messages with mod-specific messages — SMAPI core's "Oops! Steam achievements won't work because Steam isn't loaded." is an Error-level entry from SMAPI, outside the mod-scoped gate
- [x] 6.6 Script distinguishes "mod skipped by SMAPI" from "mod crashed" via the `Skipped mods` section and its stated reason
- [ ] 6.7 Script launches SMAPI's native apphost (`./StardewModdingAPI --no-terminal`) with the game directory as the working directory — a relocated launcher copy does **not** abort with SIGABRT (exit 134): measured against the real 4.5.2 apphost, a copy without its `.dll` exits **154** ("The application to execute does not exist"), with the `.dll` but no runtime it exits **131** ("You must install .NET to run this application"), and with the runtime but no game files it exits **137** ("Failed to create CoreCLR"). None of these is in FR-007's translation table, so a broken SMAPI install is reported as `13` ("the script itself broke") instead of `2` ("SMAPI not functional"). FR-001's supporting rationale is also wrong: the apphost resolves its runtime relative to its **own path**, `Constants.GamePath` is the directory of `StardewModdingAPI.dll`, and the log directory is `$XDG_CONFIG_HOME`/`~/.config` + `/StardewValley/ErrorLogs` — not relative to the game directory. The *requirement* (run in place, from the game directory) is harmless and matches SMAPI's own launcher. Pending the round-7 exit-code decision.

---

## 7. Headless Game Testing via Xvfb

- [ ] 7.1 Xvfb is started on a specific display (e.g., `:99`) before game launch — **UNRESOLVED**: FR-001 says only "a non-default display (e.g., `:99`)" — "e.g." is not a policy. No artifact specifies how the number is chosen, what happens on a collision, or whether the script may retry another display; there is no `--display` option anywhere in the feature; and `quickstart.md`'s troubleshooting table tells the user to "Choose another display", which the CLI cannot do. Pending the round-7 decision (the evidence-backed direction is `Xvfb -displayfd`, which lets the server pick a free display, or a bounded upward scan from `:99`).
- [x] 7.2 `DISPLAY` environment variable is exported to the game process
- [x] 7.3 Xvfb readiness is confirmed before game launch (socket exists or `xdpyinfo` succeeds)
- [x] 7.4 Software OpenGL rendering is genuinely usable — `xdpyinfo` reports the `GLX` and `RANDR` extensions, and `glxinfo -B` under `LIBGL_ALWAYS_SOFTWARE=1` exits successfully reporting a software renderer (`llvmpipe`/`softpipe`); the presence of `libGL.so` alone does NOT satisfy this check (FR-012 item 4). **The probe is layered and must assert the game-specific capability too**: the maintainer's actual failure is MonoGame's `PlatformNotSupportedException: MonoGame requires either ARB_framebuffer_object or EXT_framebuffer_object` — a context *was* created, it just lacked FBOs — and `glxinfo -B` prints **no extension list**, so it cannot detect that class. Full `glxinfo` (or `glxinfo -s`) MUST additionally report `GL_ARB_framebuffer_object` or `GL_EXT_framebuffer_object` (MonoGame's `GraphicsCapabilities.OpenGL.cs` accepts GL ≥ 3.0 **or** either extension). Note also that `glxinfo` exits **255** on failure (not 1) and that `glxinfo -create` does not exist — it prints "Unknown option" to stderr and **exits 0**, i.e. it would always pass
- [x] 7.5 Game launch to log check typically completes in 15-30 seconds on modern hardware — **unverified on the maintainer's machine**; the only prior launch attempt failed with "Failed to create graphics device! GPU: Could not detect." (recorded in spec.md Assumptions). The 15-30 s figure appears **only in this checklist** — `plan.md` says only "within 120 seconds" — and it is probably conservative: the gate is SMAPI's `Loaded N mods:` block, which is written during *mod loading*, i.e. before the title screen, so it should be reached in a few seconds
- [x] 7.6 Xvfb process is terminated during cleanup (same SIGTERM → SIGKILL pattern as game)
- [x] 7.7 X11 lock files (`/tmp/.X{n}-lock` and `/tmp/.X11-unix/X{n}`) are removed during cleanup
- [x] 7.8 `SDL_VIDEODRIVER=x11` is exported (never `wayland`) — the game bundles `libSDL2` and Wayland cannot be used against an Xvfb display
- [x] 7.9 `LIBGL_ALWAYS_SOFTWARE=1` and `GALLIUM_DRIVER=llvmpipe` are exported to force Mesa software rendering
- [x] 7.10 An X11 lock file owned by a live process aborts the run with exit code `10` and is **not** removed; only a lock whose owning PID is dead is treated as stale and reclaimed

---

## 8. Termination Strategy

- [x] 8.1 Game is terminated immediately after log check passes
- [x] 8.2 Script does NOT wait for the `Saved` event (no observable log marker exists — `ModController.cs:1089-1102`)
- [x] 8.3 SIGTERM is sent first, followed by a brief grace period for graceful exit
- [x] 8.4 SIGKILL is used only if the process is unresponsive after the grace period
- [x] 8.5 Termination happens after the log check, not before (log check is the validation)

---

## 9. Mod-Specific Error Detection

- [x] 9.1 Script greps log file for mod name `Stardew Valley - Living Roots` (case-sensitive exact match, per clarification)
- [x] 9.2 Script searches for entries whose **level column** is `ERROR` or `ALERT` and whose source column is the mod — the log has **no `[ERROR]` token** (the level lives inside the bracket group, `[14:32:02 ERROR Stardew Valley - Living Roots]`), so a literal `grep -F '[ERROR]'` matches nothing; the level must be matched as a column, e.g. `^\[\d\d[:.]\d\d[:.]\d\d\] (ERROR|ALERT) `
- [x] 9.3 Script checks for the mod being skipped or erroring — the real signals are SMAPI's `Skipped mods` section (`      - <Name> <Version> because <reason>`, e.g. "because it needs SMAPI X or later") and mod-scoped Error entries (e.g. `Mod failed handling that command:`, `Failed loading the per-mod API instance from …`). SMAPI emits **no** literal `"failed to load"` string, so that wording is corrected here and in the *Common Failure Modes* table below
- [x] 9.4 Script looks for exception stack traces mentioning mod types
- [x] 9.5 Failure reports include relevant Trace lines following any Error entry (20-30 lines context)
- [x] 9.6 Script distinguishes mod errors from SMAPI core errors and other mods' errors

---

## 10. Crash Detection

- [x] 10.1 `SMAPI-crash.txt` existence is checked before parsing `SMAPI-latest.txt`
- [x] 10.2 If `SMAPI-crash.txt` is present, script parses it for exception details and stack trace
- [x] 10.3 Game crash during launch is detected via non-zero process exit code
- [x] 10.4 Crash time is compared with test run time to confirm relevance (N/A — the timestamp comparison is explicitly ruled out: spec.md Clarifications → Session 2026-10-19 (checklist review — round 3, final), spec.md:211 — "Should `SMAPI-crash.txt` timestamps be compared against the run window? → A: No. Stale-crash cleanup before launch (FR-012 item 6) already guarantees relevance; the timestamp comparison is N/A (smapi-integration 10.4)". The cleanup is unconditional and pre-launch (FR-002 "Pre-launch cleanup (MUST)", spec.md:76; FR-012 item 6, spec.md:89), so any `SMAPI-crash.txt` present after launch was written by the current run — a crash during the run is still detected by items 10.1/10.5 plus the non-zero exit code of item 10.3, leaving no gap in which a real crash is missed)
- [x] 10.5 Any `SMAPI-crash.txt` is treated as a test failure (overrides log check)
- [x] 10.6 Old `SMAPI-crash.txt` is removed before test run to avoid false positives
- [x] 10.7 Removal of the stale crash **marker** (`smapi-internal/StardewModdingAPI.crash.marker`) prevents SMAPI's blocking `Console.ReadKey()` from hanging the headless launch — the marker, not the crash log, gates the read. FR-002 "Pre-launch cleanup (MUST)"

---

## 11. Artifact Identity & Mods Path

- [ ] 11.1 The script rebuilds the mod with `dotnet build --configuration Release --no-incremental` after removing the deploy target directory (FR-016) — **UNRESOLVED**: the command is pinned, but three things are not: (a) the **freshly built assembly's path** — measured, the output is `LivingRoots/bin/LivingRoots.dll` (the csproj sets `OutputPath=bin/` + `AppendTargetFrameworkToOutputPath=false`, so *not* `bin/Release/net6.0/`); (b) **which "deploy target directory"** is removed — `Pathoschild.Stardew.ModBuildConfig` 4.4.0 deploys to `$(GameModsPath)/$(ModFolderName)`, where `GameModsPath` defaults to `$(GamePath)/Mods` and `GamePath` is **auto-detected** from ~15 candidate locations, so the build deploys somewhere the script may not be able to name; (c) the `GamePath` MSBuild property/env var, which the spec never mentions. Pending the round-7 decision.
- [ ] 11.2 A build failure aborts the script with a non-zero exit **before** the game is launched — a failed build that leaves the old DLL in place would otherwise produce a false pass. **VACUOUS as currently specified**: FR-016 also removes the deploy target and then rebuilds, and the build *repopulates* that target, so the subsequent digest comparison (11.3) compares the fresh build against itself and can never fail. Verified: after a build the game's `Mods/LivingRoots/LivingRoots.dll` digest equals `LivingRoots/bin/LivingRoots.dll`. SC-012 and TC-9 are unreachable as written. Pending the round-7 decision.
- [ ] 11.3 The deployed assembly's SHA-256 digest is compared with the freshly built assembly's; a mismatch aborts the run before launch and names both digests (FR-016, SC-012) — **UNRESOLVED**: *what* is hashed is consistent across the artifacts (the DLL only, manifest excluded), but (a) *which* "deployed" copy — FR-017's resolved `--mods-path` or the build's auto-detected `GameModsDir`? If they differ, the digests can never match and the script exits `12` on **every** run; (b) the reference point — FR-016 removes the deploy target *then* rebuilds, so the post-build comparison is a tautology. The evidence-backed mechanism is a **within-run** comparison with the deploy target pinned to the resolved `--mods-path` (`-p:GameModsPath=<resolved>`), i.e. the SLSA-subject-digest / file-integrity-monitoring pattern. Note that `dotnet build` output *is* byte-reproducible at a fixed absolute source path (the compiler's content hash drives the PE timestamp and MVID), so a digest may also be compared across runs at the same path — but not across different paths without `PathMap`. Pending the round-7 decision.
- [x] 11.4 The manifest `Version` is **not** used as the freshness assertion — it is fixed at `0.0.1` and invariant across builds
- [x] 11.5 The mods directory is resolved explicitly and passed to SMAPI via `--mods-path` / `SMAPI_MODS_PATH`, defaulting to `<game-path>/Mods` (FR-017)
- [x] 11.6 The pre-flight verifies the resolved mods directory exists and contains the mod's `manifest.json` with a matching `UniqueID`
- [x] 11.7 A relative `--mods-path` is resolved against the game directory, matching SMAPI's own semantics

---

## Common Failure Modes Detected

| Failure Mode | Detection Method | Log Indicator |
|--------------|------------------|---------------|
| Manifest validation error | Check mod list | `Skipped mods` entry (`      - <Name> <Version> because <reason>`) + a mod-scoped Error entry |
| Missing dependency | Check mod warnings | Warning about missing mod |
| Incompatible version | Version check | Compatibility warning |
| Assembly loading error | Load status | Assembly load failure message |
| Mod crash during entry | Exception trace | Exception in mod entry point |
| Game version mismatch | Version check | Version compatibility warning |
| SMAPI version mismatch | Version check | SMAPI version warning |
| Corrupted save data | Save load errors | Save-related error messages |
| Mod skipped by SMAPI | `Skipped mods` section | "because it needs SMAPI X or later" |
| Stale deployed artifact | SHA-256 digest comparison (pre-launch) | n/a — detected before the game starts |
| Stale log from a previous run | `Log started at {UTC} UTC` sentinel | sentinel timestamp older than the launch time |
| Graphics device failure | `glxinfo -B` pre-flight probe | n/a — detected before the game starts |
| Clean exit with no log | process exited, no log produced | n/a — treated as failure |

---

## Sources

- SMAPI Technical Documentation: https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/smapi.md
- SMAPI Log Parser: https://smapi.io/log
- SMAPI README: https://github.com/Pathoschild/SMAPI/blob/develop/docs/README.md
- SMAPI Release Notes: https://github.com/Pathoschild/SMAPI/blob/develop/docs/release-notes.md
- Stardew Valley Wiki (Modding): https://stardewvalleywiki.com/Modding:Index
- SMAPI source — `src/SMAPI/Framework/Logging/LogManager.cs` (`LogIntro`, `Log started at {UTC} UTC` sentinel), `src/SMAPI/Framework/SCore.cs` (`PurgeNormalLogs`, `GetLogPath`), `src/SMAPI/Framework/Monitor.cs` (`GenerateMessagePrefix`), `src/SMAPI/Program.cs` (`AssertSmapiVersions`, `AssertDepsJson`), `src/SMAPI/Constants.cs` (`LogDir`), `src/SMAPI.Web/Framework/LogParsing/LogParser.cs` (canonical log regexes), `src/SMAPI/Framework/ModLoading/ModResolver.cs` (skip reasons)
- Mesa 3D — Environment Variables: https://docs.mesa3d.org/envvars.html · LLVMpipe: https://docs.mesa3d.org/drivers/llvmpipe.html
- SLSA Build Track v1.2: https://slsa.dev/spec/v1.2/build-track-basics · Reproducible Builds: https://reproducible-builds.org/docs/definition/
