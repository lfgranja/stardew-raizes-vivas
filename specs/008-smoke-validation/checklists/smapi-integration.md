# Checklist: SMAPI Integration & Log Analysis

**Purpose**: Validate that the script correctly interacts with SMAPI, accurately parses logs, and properly detects mod loading outcomes
**Created**: 2026-10-19
**Feature**: [spec.md](../spec.md) · [research.md](../research.md)

---

## 1. SMAPI Automation Environment Variables

- [x] 1.1 Set `SMAPI_NO_TERMINAL=true` to prevent terminal launch in headless environment
- [x] 1.2 `SMAPI_USE_CURRENT_SHELL` is **not** set — it does not exist in SMAPI 4.5.2 (only `SMAPI_NO_TERMINAL`, `SMAPI_MODS_PATH` and `SMAPI_DEVELOPER_MODE` are read by `StardewModdingAPI.dll`); it is read only by SMAPI's installer wrapper, which this design bypasses by launching the apphost directly, so it is inert (round-7 decision; FR-001, FR-015).
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
- [x] 4b.4 The log's `Mods go here: <path>` line confirms which mods directory SMAPI resolved — it is **tilde-anonymised** by SMAPI (`PathUtilities.AnonymizePathForDisplay` replaces `$HOME` with `~`), so the comparison is against the home-relative form, and the line is **context only**: it precedes the run sentinel, so it is exempt from the "only after the sentinel" rule and never gates the verdict (FR-002 "Header region (MUST)").
- [x] 4b.5 The log's `SMAPI <version> with Stardew Valley <version> on <os>` banner reports the installed SMAPI version — it too precedes the sentinel, so it is **context only** and never gates the verdict (FR-002 "Header region (MUST)").

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
- [x] 6.7 Script launches SMAPI's native apphost (`./StardewModdingAPI --no-terminal`) with the game directory as the working directory — a relocated launcher copy does **not** abort with SIGABRT: measured against the real 4.5.2 apphost it exits **154** (assembly missing), **131** (.NET runtime missing) or **137** (CoreCLR init failed), and all three are translated to `2` (SMAPI not functional) by FR-007's table. The rationale is corrected too: the apphost resolves its runtime relative to its **own path**, `Constants.GamePath` is the directory of `StardewModdingAPI.dll`, and the log directory is `$XDG_CONFIG_HOME`/`~/.config` + `/StardewValley/ErrorLogs` — not relative to the game directory.

---

## 7. Headless Game Testing via Xvfb

- [x] 7.1 Xvfb is started with `-displayfd`, so the **X server** allocates a free display and reports it back — the script neither chooses a display number nor manages `/tmp/.X{n}-lock` files (the server writes no lock under `-displayfd`). If Xvfb fails to start or reports no number, the run aborts with exit `10` (FR-012 item 5).
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

- [x] 11.1 The script rebuilds the mod with `dotnet build --configuration Release --no-incremental` after removing the deploy target directory `<mods-path>/<ModFolderName>/`, and **pins the deploy target** with `-p:GameModsPath="<resolved mods path>"` so the build deploys into exactly the directory SMAPI loads from (FR-016 (b), (c)). The project is located via `--mod-project`, defaulting to `<repo-root>/LivingRoots/LivingRoots.csproj`.
- [x] 11.2 A build failure aborts the script with exit code `12` **before** the game is launched — a failed build that leaves the old DLL in place would otherwise produce a false pass (FR-016 (b)).
- [x] 11.3 The deployed assembly's SHA-256 digest is compared with the freshly built assembly's (`<mod-project-dir>/bin/<EntryDll>` vs `<mods-path>/<ModFolderName>/<EntryDll>`); a mismatch, a missing file, or a file older than the build aborts with exit `12` and names both digests (FR-016 (d), SC-012). The comparison is within the run only — cross-build digest equality is not asserted, because the compiler's content hash is a function of the absolute source path.
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
