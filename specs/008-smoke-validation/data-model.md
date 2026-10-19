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

**Format**: `[HH:mm:ss LEVEL     SOURCE] message` — the level field is padded to 8 characters, the source column carries the mod's manifest name for mod-originated entries, and lines are **CRLF-terminated**. Matching MUST be substring-based (`grep -F`), with any trailing carriage return stripped before an exact comparison.

**Levels** (6): `TRACE`, `DEBUG`, `INFO`, `WARN`, `ERROR`, `ALERT`

**Mod Identification**: The mod's manifest name is `Stardew Valley - Living Roots`. The script filters log entries by this name in the source column to isolate mod-specific messages.

**Run boundary (MUST)**: Every log begins with the per-run sentinel `Log started at {UTC timestamp} UTC`. Only lines **after** that sentinel belong to the current run; all success/failure signals are evaluated against those lines only. SMAPI unlinks and recreates `SMAPI-latest.txt` at startup (new inode) rather than truncating it.

**Per-line timestamps**: `HH:mm:ss` in **local** time with no date — they MUST NOT be used to identify a run.

**SMAPI's own mod-resolution output** (the authoritative success gate):

| Signal | Meaning |
|--------|---------|
| `SMAPI <ver> with Stardew Valley <ver> on <os>` | Version banner — the installed SMAPI version |
| `Mods go here: <path>` | The mods directory SMAPI actually resolved |
| `Loaded N mods:` + `   <Name> <Version> by <Author>` | Mods SMAPI loaded — the mod MUST appear here |
| `Skipped mods` + `   - <Name> <Version> because <reason>` | Mods SMAPI skipped — the mod MUST NOT appear here |

**Mod success marker (reported, not gating)**: `The 'Stardew Valley - Living Roots' mod was loaded successfully!` (Info level). Emitted **by the mod**, not by SMAPI, from its `GameLaunched` handler (`ModController.cs:959`) — it therefore appears only after the game reaches the title screen, which requires a working graphics device.

**Failure Markers**: Error and Alert level entries attributable to the mod. Absence of the mod from the `Loaded N mods:` block (or its presence in `Skipped mods`) also indicates failure.

**Crash Override**: `SMAPI-crash.txt` (if present) overrides `SMAPI-latest.txt` and indicates failure. It is removed pre-launch because SMAPI's handling of a leftover crash marker performs a blocking `Console.ReadKey()` that can hang a headless launch.

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

**Identity**: The artifact carries **no build identity** — `manifest.json` declares a fixed `Version` (`0.0.1`) and the assembly a fixed `AssemblyVersion` (`1.0.0.0`); no commit identifier is embedded. Freshness is therefore established by comparing SHA-256 digests between the freshly built assembly and the deployed assembly.

**Failure mode**: A stale deployed assembly still emits the mod's success marker, so validating it yields a **false pass** for code that is not the current source. Two copies of this artifact may coexist (a repository-local `.gamepath/Mods/` copy and the copy in the game installation); only the copy in the resolved mods path is under test.

---

## Internal Script State (Runtime)

### Process State

| Field | Type | Description |
|-------|------|-------------|
| `game_pid` | integer | PID of the launched Stardew Valley process |
| `xvfb_pid` | integer | PID of the Xvfb process |
| `display_num` | string | X11 display number (e.g., `:99`) |
| `lock_fd` | integer | File descriptor for flock lock |
| `temp_dir` | string | Temporary directory path for script resources |
| `game_path` | string | Resolved game installation directory (working directory of the launch) |
| `mods_path` | string | Resolved mods directory passed to SMAPI via `--mods-path` |
| `run_id` | string | `<UTC timestamp>-<PID>` correlation key for this run |
| `run_log` | string | Path to this run's diagnostic log file |
| `launch_t0` | epoch/ISO | UTC launch timestamp, used to validate the log sentinel |
| `log_baseline_inode` | integer | Inode of `SMAPI-latest.txt` before launch (absent ⇒ recorded as none) |

### Exit Codes (sysexits.h convention)

| Code | Constant | Meaning |
|------|----------|---------|
| 0 | `EX_OK` | Success — mod loaded without errors |
| 1 | `EX_GAME_NOT_FOUND` | Game installation not found at expected path |
| 2 | `EX_SMAPI_NOT_FOUND` | SMAPI not installed or not functional |
| 3 | `EX_XVFB_NOT_AVAILABLE` | Xvfb / graphics environment unavailable (incl. no usable software OpenGL) |
| 4 | `EX_TIMEOUT` | Game did not produce log within timeout window |
| 5 | `EX_MOD_LOAD_FAILURE` | Mod failed to load (Error/Alert in log, absent from `Loaded N mods:`, or present in `Skipped mods`) |
| 6 | `EX_LOCK_CONTENTION` | Another smoke test instance is running (flock failed) |
| 7 | `EX_PROCESS_CONFLICT` | Existing Stardew Valley/SMAPI process detected |
| 9 | `EX_MISSING_DEPENDENCY` | Missing host tool or unresolvable runtime dependency |
| 10 | `EX_DISPLAY_CONFLICT` | X display owned by a live server, or a stale X11 lock could not be reclaimed |
| 11 | `EX_STEAM_PREREQ_MISSING` | `steam_appid.txt` absent or wrong |
| 130 | `EX_SIGINT` | Script interrupted by SIGINT (POSIX 128+2) |
| 143 | `EX_SIGTERM` | Script terminated by SIGTERM (POSIX 128+15) |

**Timeout code note**: `4` (`EX_TIMEOUT`) is canonical. The `timeout` utility's native status `124` is translated to `4` at the script boundary, so no subprocess-specific status ever becomes the script's final code (FR-007).

**Interruption note**: interrupted runs use the POSIX `128+N` convention (130 / 143). The earlier `8 = EX_INTERRUPTED` value was superseded — `sysexits.h` defines no value below 64, so it would be neither a sysexits value nor POSIX-compliant.

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
2. **Freshness**: all signals are evaluated only against log lines after the current run's `Log started at {UTC} UTC` sentinel
3. **Crash Override**: If `SMAPI-crash.txt` exists → immediate failure (regardless of log content)
4. **Mod-Specific Filtering**: Only log entries with mod name `Stardew Valley - Living Roots` in the source column are considered
5. **Level Filtering**: Only `ERROR` and `ALERT` levels are failure markers; `WARN` is logged but never fails; `TRACE` is diagnostic-only
6. **Mod Marker Is Non-Gating**: the mod's own success marker is reported but does not participate in the pass condition
7. **Artifact Identity**: the deployed assembly's SHA-256 digest MUST equal the freshly built assembly's digest
8. **Idempotency**: Running the script multiple times in sequence produces the same result (no state accumulation)

---

## State Transitions

```
[Start]
  │
  ▼
[Pre-Flight] ──fail──→ [Error Exit]
  │
  ▼
[Build & Verify Artifact] ──fail──→ [Error Exit]
  │
  ▼
[Acquire Flock] ──fail──→ [Error Exit]
  │
  ▼
[Detect Running Process] ──found──→ [Error Exit (or --force kills)]
  │
  ▼
[Capture Log Baseline] ──fail──→ [Error Exit]
  │
  ▼
[Launch Xvfb] ──fail──→ [Error Exit]
  │
  ▼
[Launch Game] ──fail──→ [Error Exit]
  │
  ▼
[Poll Log] ──timeout──→ [Error Exit]
  │
  ▼
[Check Log] ──fail──→ [Error Exit]
  │
  ▼
[Terminate Game] ──→ [Success Exit]
```

**Capture Log Baseline** records the UTC launch timestamp `T0` and the inode of `SMAPI-latest.txt` before the game starts, and removes any pre-existing `SMAPI-latest.txt` and `SMAPI-crash.txt`. **Build & Verify Artifact** rebuilds the mod, removes the deploy target first, and compares SHA-256 digests; a failure exits before the game is ever launched.
