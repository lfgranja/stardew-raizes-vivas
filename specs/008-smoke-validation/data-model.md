# Data Model: Smoke Validation Script

**Date**: 2026-10-19
**Context**: `specs/008-smoke-validation` — entities and data structures

---

## Overview

The smoke validation script has no persistent data model of its own — it reads external artifacts (SMAPI log, mod build artifact, mod data files) and produces a single exit code. The one thing it writes is a per-run diagnostic log (see the Run Log entity). This document defines the external data structures the script interacts with and the internal state it tracks during execution.

---

## External Data Structures (Read-Only)

### SMAPI Log File

**Location**: `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` (Linux)

**Format**: `[HH:mm:ss LEVEL SOURCE] message` — the level field is padded to **5** characters (SMAPI renders it with `PadRight(Monitor.MaxLevelLength)`; `MaxLevelLength` is the length of the longest public `LogLevel` name — `Trace`/`Debug`/`Error`/`Alert` = 5) followed by a single separator space, the source column carries the mod's manifest name for mod-originated entries, and lines are **CRLF-terminated**. Matching MUST be substring-based (`grep -F`), with any trailing carriage return stripped before an exact comparison.

> **Corrected 2026-10-19 (round 7).** The earlier "padded to 8 characters" was factually wrong and contradicted the worked examples below. The level lives **inside** the bracket group — there is no `[ERROR]` token — so Error/Alert entries MUST be matched on the **level column**, never as a literal `[ERROR]` substring. Verified against the installed SMAPI 4.5.2 binary and upstream `src/SMAPI/Framework/Monitor.cs`.

**Levels** (6): `TRACE`, `DEBUG`, `INFO`, `WARN`, `ERROR`, `ALERT`

**Mod Identification**: The mod's manifest name is `Stardew Valley - Living Roots`. The script filters log entries by this name in the source column to isolate mod-specific messages.

**Run boundary (MUST)**: Every log begins with the per-run sentinel `Log started at {UTC timestamp} UTC`. Only lines **after** that sentinel belong to the current run; all success/failure signals are evaluated against those lines only. SMAPI unlinks and recreates `SMAPI-latest.txt` at startup (new inode) rather than truncating it.

**Per-line timestamps**: `HH:mm:ss` in **local** time with no date — they MUST NOT be used to identify a run.

**SMAPI's own mod-resolution output** (the authoritative success gate):

| Signal | Meaning |
|--------|---------|
| `SMAPI <ver> with Stardew Valley <ver> on <os>` | Version banner — the installed SMAPI version. **Written before the run sentinel** (see below) |
| `Mods go here: <path>` | The mods directory SMAPI actually resolved. **Written before the run sentinel**, and the path is **tilde-anonymised** (`$HOME` → `~`) by `PathUtilities.AnonymizePathForDisplay` — a literal comparison against an absolute resolved path fails |
| `Loaded N mods:` + `   <Name> <Version> by <Author>` (optionally ` \| <Description>`) | Mods SMAPI loaded — the mod MUST appear here. With zero mods SMAPI writes `Loaded 0 mods.` (period, not colon) |
| `Skipped mods` + `      - <Name> <Version> because <reason>` | Mods SMAPI skipped — the mod MUST NOT appear here. The section header and its entries are logged by **SMAPI core** at Error level, so they are outside the mod-scoped gate |

**Mod success marker (reported, not gating)**: `The 'Stardew Valley - Living Roots' mod was loaded successfully!` (Info level). Emitted **by the mod**, not by SMAPI, from its `GameLaunched` handler (`ModController.cs:959`) — it therefore appears only after the game reaches the title screen, which requires a working graphics device.

**Failure Markers**: Error and Alert level entries attributable to the mod. Absence of the mod from the `Loaded N mods:` block (or its presence in `Skipped mods`) also indicates failure.

**Crash Override**: `SMAPI-crash.txt` (if present) overrides `SMAPI-latest.txt` and indicates failure. It is a **copy of the log**, written at the moment of the crash.

> **Corrected 2026-10-19 (round 7) — the blocking-read trigger is the MARKER, not the crash log.** SMAPI's `LogManager.HandleMarkerFiles()` gates its blocking `Console.ReadKey()` on `smapi-internal/StardewModdingAPI.crash.marker` (and, for beta/update flows, `smapi-internal/StardewModdingAPI.update.marker`). `Constants.FatalCrashLog` (`SMAPI-crash.txt`) is only a copy of the log file. Verified against the installed SMAPI 4.5.2 binary — the three literals `SMAPI-crash.txt`, `StardewModdingAPI.crash.marker` and `StardewModdingAPI.update.marker` are all present. Removing **only** `SMAPI-crash.txt` leaves the marker in place, so the headless launch still blocks on the keypress read. See the round-7 clarification for which files the pre-flight must remove.

**Example Entry**:
```
[14:32:01 INFO  Stardew Valley - Living Roots] The 'Stardew Valley - Living Roots' mod was loaded successfully!
[14:32:02 ERROR Stardew Valley - Living Roots] NullReferenceException: Object reference not set...
```

### Mod Data Files

**Location**: `~/.config/StardewValley/.smapi/mod-data/lfgranja.LivingRoots/` — SMAPI's mod-data directory, resolved by `IModHelper.Data.WriteJsonFile` from a relative name. The mod does **not** write into a `data/` folder inside its own mod folder.

**Naming Convention**: `<prefix><sanitizedSaveId>.json`

| Prefix | Source | Content |
|--------|--------|---------|
| `soil_health_data_` | `ModConstants.KeyPrefix` | Soil health per tile |
| `composting_bins_` | `ModConstants.CompostingBinKeyPrefix` | Composting bin state |
| `visualization_config_` | Visualization configuration | Overlay/tooltip settings |

**SaveId Sanitization**: Unicode normalization (FormC) + filename sanitization via `ModDataService.SanitizePathSegments()`. Max length: `ModConstants.MaxSaveIdLength`.

**Expected State When No Save Loaded** (title screen): Zero data files. This is valid and expected in the automated script.

---

## External Build Artifact (Read/Verified)

### Mod Build Artifact

**Deployed location**: `<mods-path>/LivingRoots/` (manifest `Name` = `Stardew Valley - Living Roots`, `UniqueID` = `lfgranja.LivingRoots`, `EntryDll` = `LivingRoots.dll`)

**Identity**: The artifact carries **no build identity** — `manifest.json` declares a fixed `Version` (`0.0.1`) and the assembly a fixed `AssemblyVersion` (`1.0.0.0`); no commit identifier is embedded, and `SourceRevisionId` is inert because the project sets `GenerateAssemblyInfo=false`. Freshness is therefore established by the **deploy-effectiveness** assertion: the deployed assembly's SHA-256 digest MUST equal the digest of the assembly produced by the current build, with the build's deploy target pinned to the resolved mods path (`-p:GameModsPath`). The digest is comparable **within the run only**.

**Failure mode**: A stale deployed assembly still emits the mod's success marker, so validating it yields a **false pass** for code that is not the current source. Two copies of this artifact may coexist (a repository-local `.gamepath/Mods/` copy and the copy in the game installation); only the copy in the resolved mods path is under test.

---

## Internal Script State (Runtime)

### Process State

| Field | Type | Description |
|-------|------|-------------|
| `game_pid` | integer | PID of the launched Stardew Valley process |
| `xvfb_pid` | integer | PID of the Xvfb process |
| `display_num` | string | X11 display number **reported by the server** via `Xvfb -displayfd` (not chosen by the script) |
| `lock_fd` | integer | File descriptor for flock lock |
| `lock_path` | string | Stable per-user lock file path (`${XDG_RUNTIME_DIR}/livingroots-smoke.lock`, or the `${XDG_STATE_HOME}` fallback) |
| `temp_dir` | string | Temporary directory path for script resources |
| `game_path` | string | Resolved game installation directory (working directory of the launch) |
| `mods_path` | string | Resolved mods directory passed to SMAPI via `--mods-path` **and** to the build via `-p:GameModsPath` |
| `mod_project` | string | Resolved path of the mod project file (`<repo-root>/LivingRoots/LivingRoots.csproj` by default, via `--mod-project`) |
| `build_output` | string | Path of the freshly built assembly (`<mod-project-dir>/bin/<EntryDll>`) |
| `run_id` | string | `<UTC timestamp>-<PID>` correlation key for this run |
| `run_log` | string | Path to this run's diagnostic log file |
| `launch_t0` | epoch/ISO | UTC launch timestamp, used to validate the log sentinel |
| `log_baseline_inode` | integer | Inode of `SMAPI-latest.txt` before launch (absent ⇒ recorded as none) |

### Exit Codes (one code per failure category — bespoke, one code per failure class)

| Code | Constant | Meaning |
|------|----------|---------|
| 0 | `EX_OK` | Success — mod loaded without errors |
| 1 | `EX_GAME_NOT_FOUND` | Game installation not found at expected path |
| 2 | `EX_SMAPI_NOT_FOUND` | SMAPI not installed or not functional (includes a relocated/broken launcher — the apphost exits 154 / 131 / 137) |
| 3 | `EX_GRAPHICS_UNUSABLE` | Graphics environment **present but unusable** — no `GLX`/`RANDR`, no software renderer, or no framebuffer-object support |
| 4 | `EX_TIMEOUT` | Game did not produce log within timeout window |
| 5 | `EX_MOD_LOAD_FAILURE` | Mod failed to load (Error/Alert in log, absent from `Loaded N mods:`, or present in `Skipped mods`) |
| 6 | `EX_LOCK_CONTENTION` | Another smoke test instance is running (flock failed) |
| 7 | `EX_PROCESS_CONFLICT` | Existing Stardew Valley/SMAPI process detected |
| 9 | `EX_MISSING_DEPENDENCY` | A required host tool is absent (`Xvfb`, `xdpyinfo`, `glxinfo`, `flock`, `timeout`, `mktemp`, `stat`, `realpath`, `pgrep`/`pidof`, `sha256sum`, `date`, `grep`, `dotnet`, `bash` 4+) |
| 10 | `EX_DISPLAY_CONFLICT` | The virtual display could not be allocated or started — Xvfb failed, or reported no display number |
| 11 | `EX_STEAM_PREREQ_MISSING` | `steam_appid.txt` absent or not `413150` |
| 12 | `EX_BUILD_ARTIFACT_FAILURE` | The mod failed to build, or the deployed assembly is missing, older, or not byte-identical to the fresh build |
| 13 | `EX_INTERNAL_ERROR` | The script itself broke — an unhandled error caught by the ERR trap, a failed PGID assertion, or an unmapped child status |
| 14 | `EX_USAGE_ERROR` | Usage or CLI-argument error — unknown option, missing required argument, malformed value |
| 15 | `EX_NO_LOG_PRODUCED` | The game exited (including status `0`) but produced no SMAPI log |
| 16 | `EX_MOD_MANIFEST_INVALID` | The resolved mods directory, the mod folder, `manifest.json`, or its `UniqueID` is missing or invalid |
| 129 | `EX_SIGHUP` | Script terminated by SIGHUP (POSIX 128+1) |
| 130 | `EX_SIGINT` | Script interrupted by SIGINT (POSIX 128+2) |
| 143 | `EX_SIGTERM` | Script terminated by SIGTERM (POSIX 128+15) |

**Numbering note**: this is a bespoke, **one code per failure class** taxonomy. It is **not** the `sysexits.h` numbering, which begins at `EX__BASE` = 64 and has been deprecated by FreeBSD (since 2008) and by OpenBSD ("Do not use them"); the phrase "sysexits.h convention" is withdrawn, and so is "sysexits-*style*", which still implies the 64–78 range.

**Allocation rule**: codes are allocated **additively** and permanently — a **published code MUST NOT be re-assigned** to a different failure class, and a **retired code MUST NOT be reused**. A new category takes the next value **above the highest published code**, not the next numerically unused value.

**Retired and reserved codes**:

| Code | Status | Note |
|------|--------|------|
| 8 | **Retired** (round 2) | Superseded by the POSIX `128+N` interruption codes. Never reused — and never handed out again, because new codes are allocated above the highest published code rather than into holes |

**Reserved ranges the script MUST NOT emit**: `126`/`127` (POSIX reserves them for the shell's own exec failures), `128`–`159` except by genuinely dying on a signal, `64`–`78` (the deprecated BSD `sysexits` band), `100`–`149` (LSB distribution use), `150`–`199` (LSB application use), `200`–`254` (reserved / systemd-private), `255` (de-facto reserved as `EXIT_EXCEPTION`).

**Translation note (FR-007)**: no subprocess-specific status may ever become the script's final code. Every child status is translated before the script exits:

| Child status | Script exit code |
|--------------|------------------|
| `0` | `0` |
| `124` (`timeout` expired) | `4` |
| `125` (`timeout` itself failed) | `13` |
| `126` (found but not executable) | `9` |
| `127` (not found) | `9` |
| `131` (the .NET runtime could not be started by the SMAPI apphost) | `2` |
| `134` (SIGABRT) | `2` |
| `137` (SIGKILL) | `5` when it is the game process, `2` when it is the SMAPI apphost, `13` otherwise |
| `154` (the SMAPI apphost cannot find its own assembly) | `2` |
| `128+N` for SIGINT/SIGTERM/SIGHUP on the script itself | `128+N`, unchanged |
| any other non-zero | `13` |

A pre-flight probe that fails is translated by the **category of the probe**, not by the raw status the probe happened to return.

**Interruption note**: interrupted runs use the POSIX `128+N` convention (129 / 130 / 143). The earlier `8 = EX_INTERRUPTED` value was superseded and is not reused.

### Log Check Result

| Field | Type | Description |
|-------|------|-------------|
| `log_fresh` | boolean | Current run's `Log started at` sentinel seen at or after `launch_t0` |
| `mod_listed` | boolean | Mod appears in SMAPI's `Loaded N mods:` block |
| `mod_skipped` | boolean | Mod appears in SMAPI's `Skipped mods` section |
| `mod_marker_present` | boolean | The mod's own success marker line was seen (reported, **not** gating) |
| `error_entries_found` | boolean | Error/Alert entries from mod found |
| `pass` | boolean | AND conjunction: `log_fresh && mod_listed && !mod_skipped && !error_entries_found` |

---

## Entity Relationships

```
Script (no persistent data model; writes only a diagnostic run log)
  ├── reads  → SMAPI Log File (~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt)
  ├── reads  → Mod Data Files (~/.config/StardewValley/.smapi/mod-data/lfgranja.LivingRoots/*.json) [deferred — not implemented]
  ├── builds & verifies → Mod Build Artifact (<mods-path>/LivingRoots/) via SHA-256 digest comparison
  ├── manages → Game Process (Stardew Valley via SMAPI apphost)
  ├── manages → Xvfb Process (virtual display)
  ├── manages → Lock File (flock)
  └── writes  → Run Log (${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log)
```

---

## Validation Rules

1. **Log Check Pass Condition**: `log_fresh AND mod_listed AND NOT mod_skipped AND NOT error_entries_found`
2. **Freshness**: all gating signals are evaluated only against log lines after the current run's `Log started at {UTC} UTC` sentinel. The **header region** — the version banner and the `Mods go here:` line, both of which precede the sentinel — is exempt and is context only; it never gates the verdict. The `Mods go here:` path is `~`-anonymised, so any comparison uses the home-relative form
3. **Crash Override**: If `SMAPI-crash.txt` exists → immediate failure (regardless of log content)
4. **Mod-Specific Filtering**: Only log entries with mod name `Stardew Valley - Living Roots` in the source column are considered
5. **Level Filtering**: Only `ERROR` and `ALERT` levels are failure markers; `WARN` is logged but never fails; `TRACE` is diagnostic-only. The level is matched as a **column** inside the bracket group — the log contains no `[ERROR]` token
6. **Mod Marker Is Non-Gating**: the mod's own success marker is reported but does not participate in the pass condition
7. **Deploy Effectiveness**: the deployed assembly's SHA-256 digest MUST equal the freshly built assembly's digest, with the deploy target pinned to the resolved mods path. The digest is compared **within the run only** — never across runs, because the compiler's content hash (and therefore the PE timestamp and MVID) is a function of the absolute source path
8. **Idempotency**: Running the script multiple times in sequence produces the same result (no state accumulation)

---

## State Transitions

```
[Start]
  │
  ▼
[Pre-Flight: tool sweep → game/SMAPI paths → Steam prereq → crash-marker removal] ──fail──→ [Error Exit]
  │
  ▼
[Start Virtual Display (Xvfb -displayfd) + software-OpenGL probe] ──fail──→ [Error Exit (10 = display, 3 = graphics)]
  │
  ▼
[Build & Verify Artifact (deploy target pinned to --mods-path)] ──fail──→ [Error Exit (12)]
  │
  ▼
[Verify Mod Manifest in the resolved mods path] ──fail──→ [Error Exit (16)]
  │
  ▼
[Acquire Flock] ──fail──→ [Error Exit (6)]
  │
  ▼
[Detect Running Process] ──found──→ [Error Exit (7, or --force kills)]
  │
  ▼
[Capture Log Baseline (inode → then remove stale log + crash marker)] ──fail──→ [Error Exit]
  │
  ▼
[Launch Game] ──fail──→ [Error Exit]
  │
  ▼
[Poll Log until the sentinel] ──timeout──→ [Error Exit (4)]
  │
  ▼
[Check Log (verdict)] ──fail──→ [Error Exit (5)]
  │
  ▼
[Terminate Game] ──→ [Success Exit (0)]
```

**Capture Log Baseline** records the UTC launch timestamp `T0` and the inode of `SMAPI-latest.txt` before the game starts, and removes any pre-existing `SMAPI-latest.txt` and `SMAPI-crash.txt`. **Build & Verify Artifact** rebuilds the mod, removes the deploy target first, and compares SHA-256 digests; a failure exits before the game is ever launched.
