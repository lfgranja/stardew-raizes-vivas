# Data Model: Smoke Validation Script

**Date**: 2026-10-19
**Context**: `specs/008-smoke-validation` — entities and data structures

---

## Overview

The smoke validation script is a stateless bash tool. It has no persistent data model of its own — it reads external artifacts (SMAPI log, mod data files) and produces a single exit code. This document defines the external data structures the script interacts with and the internal state it tracks during execution.

---

## External Data Structures (Read-Only)

### SMAPI Log File

**Location**: `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` (Linux)

**Format**: `[HH:MM:SS LEVEL ModName] message`

**Levels** (6): `TRACE`, `DEBUG`, `INFO`, `WARN`, `ERROR`, `ALERT`

**Mod Identification**: The mod's manifest name is `Stardew Valley - Living Roots`. The script filters log entries by this name in the bracket column to isolate mod-specific messages.

**Success Marker**: `The 'Stardew Valley - Living Roots' mod was loaded successfully!` (Info level, emitted by SMAPI after all mod Entry() methods complete)

**Failure Markers**: Error and Alert level entries attributable to the mod. Absence of success marker also indicates failure.

**Crash Override**: `SMAPI-crash.txt` (if present) overrides `SMAPI-latest.txt` and indicates failure.

**Example Entry**:
```
[14:32:01 INFO  Stardew Valley - Living Roots] The 'Stardew Valley - Living Roots' mod was loaded successfully!
[14:32:02 ERROR Stardew Valley - Living Roots] NullReferenceException: Object reference not set...
```

### Mod Data Files

**Location**: `<mod folder>/data/` (mod's own folder, NOT the save file)

**Naming Convention**: `<prefix><sanitizedSaveId>.json`

| Prefix | Source | Content |
|--------|--------|---------|
| `soil_health_data_` | `ModConstants.KeyPrefix` | Soil health per tile |
| `composting_bins_` | `ModConstants.CompostingBinKeyPrefix` | Composting bin state |
| `visualization_config_` | Visualization configuration | Overlay/tooltip settings |

**SaveId Sanitization**: Unicode normalization (FormC) + filename sanitization via `ModDataService.SanitizePathSegments()`. Max length: `ModConstants.MaxSaveIdLength`.

**Expected State When No Save Loaded** (title screen): Zero data files. This is valid and expected in the automated script.

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

### Exit Codes (sysexits.h convention)

| Code | Constant | Meaning |
|------|----------|---------|
| 0 | `EX_OK` | Success — mod loaded without errors |
| 1 | `EX_GAME_NOT_FOUND` | Game installation not found at expected path |
| 2 | `EX_SMAPI_NOT_FOUND` | SMAPI not installed or not functional |
| 3 | `EX_XVFB_NOT_AVAILABLE` | Xvfb not available on system |
| 4 | `EX_TIMEOUT` | Game did not produce log within timeout window |
| 5 | `EX_MOD_LOAD_FAILURE` | Mod failed to load (Error/Alert in log or success marker absent) |
| 6 | `EX_LOCK_CONTENTION` | Another smoke test instance is running (flock failed) |
| 7 | `EX_PROCESS_CONFLICT` | Existing Stardew Valley/SMAPI process detected |
| 130 | `EX_SIGINT` | Script interrupted by SIGINT (POSIX 128+2) |
| 143 | `EX_SIGTERM` | Script terminated by SIGTERM (POSIX 128+15) |

**Timeout code note**: `4` (`EX_TIMEOUT`) is canonical. The `timeout` utility's native status `124` is translated to `4` at the script boundary, so no subprocess-specific status ever becomes the script's final code (FR-007).

**Interruption note**: interrupted runs use the POSIX `128+N` convention (130 / 143). The earlier `8 = EX_INTERRUPTED` value was superseded — `sysexits.h` defines no value below 64, so it would be neither a sysexits value nor POSIX-compliant.

### Log Check Result

| Field | Type | Description |
|-------|------|-------------|
| `success_marker_found` | boolean | Success marker present in SMAPI log |
| `error_entries_found` | boolean | Error/Alert entries from mod found |
| `pass` | boolean | AND conjunction: `success_marker_found && !error_entries_found` |

---

## Entity Relationships

```
Script (stateless)
  ├── reads → SMAPI Log File (~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt)
  ├── reads → Mod Data Files (<mod folder>/data/*.json) [deferred — not implemented]
  ├── manages → Game Process (Stardew Valley via SMAPI)
  ├── manages → Xvfb Process (virtual display)
  └── manages → Lock File (flock)
```

---

## Validation Rules

1. **Log Check Pass Condition**: `success_marker_found AND NOT error_entries_found`
2. **Crash Override**: If `SMAPI-crash.txt` exists → immediate failure (regardless of log content)
3. **Mod-Specific Filtering**: Only log entries with mod name `Stardew Valley - Living Roots` in the bracket column are considered
4. **Level Filtering**: Only `ERROR` and `ALERT` levels are failure markers; `WARN` is logged but never fails; `TRACE` is diagnostic-only
5. **Idempotency**: Running the script multiple times in sequence produces the same result (no state accumulation)

---

## State Transitions

```
[Start]
  │
  ▼
[Pre-Flight] ──fail──→ [Error Exit]
  │
  ▼
[Acquire Flock] ──fail──→ [Error Exit]
  │
  ▼
[Detect Running Process] ──found──→ [Error Exit (or --force kills)]
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
