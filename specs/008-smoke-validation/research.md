# Research: Smoke Validation Script

**Date**: 2026-10-19
**Context**: `specs/008-smoke-validation` — automated mod loading validation

---

## 1. SMAPI Automation Environment Variables

**Decision**: Use `SMAPI_NO_TERMINAL=true`, `SMAPI_USE_CURRENT_SHELL=true`, `SMAPI_DEVELOPER_MODE=true`

**Rationale**: SMAPI's official documentation defines three environment variables for headless/automated operation. `SMAPI_NO_TERMINAL=true` prevents SMAPI from allocating a terminal (essential for headless). `SMAPI_USE_CURRENT_SHELL=true` prevents SMAPI from spawning a new shell (ensures the game process is a direct child of the script). `SMAPI_DEVELOPER_MODE=true` enables TRACE-level logging, which captures DI wiring failures and exception details that are otherwise invisible.

**Alternatives Considered**:
- Running without env vars → SMAPI hangs waiting for terminal input
- Using `xvfb-run` wrapper → 26-40% failure rate per Debian bug #1095028; manual Xvfb lifecycle is more reliable
- Using `SMAPI_NO_TERMINAL` only → misses TRACE-level diagnostics needed for failure analysis

**Source**: SMAPI official documentation (smapi.io/smodder-guide); Debian bug #1095028 (xvfb-run flaky behavior)

---

## 2. Headless Game Testing via Xvfb

**Decision**: Direct Xvfb lifecycle management (launch → use → kill) within the script

**Rationale**: Direct management gives the script full control over the display resource, including cleanup on failure. The script starts Xvfb on a specific display number (`:99`), waits for it to be ready, launches the game with `DISPLAY=:99`, and kills Xvfb during cleanup. This is more reliable than `xvfb-run` which has documented race conditions (26-40% failure rate in CI environments).

**Alternatives Considered**:
- `xvfb-run` wrapper → unreliable per Debian bug #1095028; doesn't allow custom display number
- `Xvfb &` without readiness check → race condition where game launches before X server is ready
- Persistent Xvfb service → requires systemd/root; not portable to maintainer machines

**Source**: Debian bug #1095028 (xvfb-run 26-40% failure rate in cc-ci); BashFAQ/062 (resource cleanup patterns); Politowski et al. 2022 (mc-runtime-test ephemeral pattern)

---

## 3. SMAPI Log Parsing Conventions

**Decision**: Parse `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` for mod-specific entries

**Rationale**: SMAPI writes structured log entries in `[HH:mm:ss LEVEL     SOURCE] message` format — the level field is padded to 8 characters and lines are CRLF-terminated, so matching must be substring-based with the trailing `\r` stripped. The mod's manifest name is "Stardew Valley - Living Roots" (from `manifest.json`). The script filters entries by this manifest name to distinguish mod-specific messages from SMAPI core or other mods.

The **authoritative success signal is SMAPI's own mod-resolution output**, not the mod's self-reported marker: the mod must appear in the `Loaded N mods:` block and must not appear in the `Skipped mods` section. The mod's own marker — "The 'Stardew Valley - Living Roots' mod was loaded successfully!" (Info level) — is emitted by the **mod** at `ModController.cs:959`, on SMAPI's `GameLaunched` event, not by SMAPI after `Entry()`. It therefore appears only once the game reaches the title screen, which requires a working graphics device; it is detected and reported but does not gate the verdict. *(Corrected 2026-10-19 against SMAPI 4.5.2 — the earlier attribution to SMAPI was incorrect.)*

**Log Levels** (6 total): TRACE, DEBUG, INFO, WARN, ERROR, ALERT. Only Error and Alert are failure markers (per FR-003). WARN is logged but never fails. Trace is diagnostic-only — SMAPI writes Trace-level entries to the log file unconditionally, so `SMAPI_DEVELOPER_MODE` (which gates console verbosity only) is not required to capture them.

**Run Boundary**: every log begins with `Log started at {UTC timestamp} UTC`. Only lines after that sentinel belong to the current run, so a stale log left by an earlier successful run cannot produce a false pass. SMAPI unlinks and recreates `SMAPI-latest.txt` at launch (new inode) rather than truncating it.

**Alternatives Considered**:
- Parsing stdout/stderr → SMAPI redirects output to log file; stdout is unreliable
- Using `SMAPI-crash.txt` as primary → only present on crash; `SMAPI-latest.txt` is always written
- Parsing all log entries without mod filter → false positives from other mods or SMAPI core
- Gating on the mod's own success marker → rejected: it requires a full graphics-initialised boot to the title screen and would report a graphics failure as a timeout

**Source**: SMAPI source code (`Constants.cs`, `SCore.cs`, `Monitor.cs`, `LogManager.cs`, `LogParser.cs`); SMAPI log format documentation; verified against the installed SMAPI 4.5.2

---

## 4. Bash Script Lifecycle Management

### 4.1 Concurrent Execution Prevention

**Decision**: `flock` on dedicated lock file (non-blocking)

**Rationale**: `flock` is kernel-managed and automatically released on process death (even SIGKILL). This eliminates stale-lock risk entirely. A dedicated lock file (not the script itself) is used to avoid blocking on self-read.

**Alternatives Considered**:
- PID file + liveness check → racy; requires stale PID detection
- `mkdir` lock → not atomic on all filesystems
- No locking → concurrent runs corrupt data files and truncate SMAPI log

**Source**: flock(1) man page; cc-ci production pattern

### 4.2 Process Detection

**Decision**: `pgrep` for Stardew Valley/SMAPI processes before launch

**Rationale**: The mod has no cross-process data file locking (all locks are process-local). Concurrent instances would corrupt data files (non-atomic JSON writes) and truncate the SMAPI log. The script detects existing processes and aborts by default, with `--force` to kill them (SIGTERM → SIGKILL escalation).

**Alternatives Considered**:
- No detection → data corruption risk
- Lock file only → doesn't detect processes from other script invocations
- `pgrep -f` pattern matching → may catch unintended processes; use exact process name matching

**Source**: Politowski et al. 2022 (mc-runtime-test ephemeral pattern)

### 4.3 Signal Handling & Cleanup

**Decision**: Trap SIGINT/SIGTERM + EXIT trap for universal cleanup

**Rationale**: Bash's EXIT trap fires on all exit paths (normal, error, signal). The script traps SIGINT/SIGTERM to forward signals to child processes, then the EXIT trap runs full cleanup: kill game (SIGTERM → SIGKILL), kill Xvfb, remove X11 locks, remove temp files. Each cleanup step is guarded with `|| true` so one failure doesn't prevent others. Cleanup order is reverse of allocation.

**Alternatives Considered**:
- No signal handling → orphaned game processes and X servers
- Single trap without EXIT → misses cleanup on unexpected exit
- Cleanup without `|| true` guards → one failure aborts remaining cleanup

**Source**: BashFAQ/062; Mironsoft four-layer cleanup model; systemd service cleanup pattern; Rasheed et al. 2022 (651 articles confirming resource leakage as top flakiness cause)

---

## 5. Exit Code Conventions

**Decision**: Follow sysexits.h convention with distinct codes per failure category

**Rationale**: sysexits.h provides standard exit codes that are self-documenting and CI-friendly. The script uses: 0 (success), 1 (game not found), 2 (SMAPI not found), 3 (Xvfb not available), 4 (timeout), 5 (mod load failure), 6 (lock contention), 7 (process conflict), 8 (interrupted). This allows callers (including future CI) to distinguish failure modes programmatically.

**Alternatives Considered**:
- Binary 0/1 → insufficient for actionable diagnostics
- Custom arbitrary codes → not standard; harder to remember
- sysexits.h `EX_*` constants → standard, widely recognized

**Source**: sysexits.h (BSD convention); IEEE 829-2008 (test exit codes)

---

## 6. Mod Data File Naming (from codebase)

**Decision**: Data files follow `<prefix><sanitizedSaveId>.json` pattern, written under SMAPI's mod-data directory — `~/.config/StardewValley/.smapi/mod-data/lfgranja.LivingRoots/`

**Rationale**: From `ModConstants.cs`:
- `KeyPrefix = "soil_health_data_"` → soil health data files
- `CompostingBinKeyPrefix = "composting_bins_"` → composting bin data files
- Visualization config uses `visualization_config_` prefix

The saveId is sanitized via Unicode normalization (FormC) and filename sanitization (`SanitizePathSegments` in `ModDataService.cs`). Files are written to the mod's own data folder (not the save file). When no save is loaded (title screen), zero data files is the expected valid state.

**Alternatives Considered**: N/A — this is existing mod behavior, not a script decision.

**Source**: `LivingRoots/ModConstants.cs:9` (KeyPrefix), `LivingRoots/Services/ModDataService.cs` (WriteJsonFile, SanitizePathSegments)

---

## 7. Termination Strategy

**Decision**: Terminate game immediately after log check passes (success marker found)

**Rationale**: The Info-level success marker confirms all mod `Entry()` methods completed without throwing. The mod emits no observable log marker on `Saved` (per `ModController.cs:1089-1102`), making it impossible to detect externally. Waiting for `Saved` would require automation of title screen UI interaction (clicking "Load" → selecting save → waiting for load), which adds significant complexity and fragility for a P2 feature (save check). The script sends SIGTERM first, waits a brief grace period for graceful exit, and escalates to SIGKILL only if unresponsive.

**Alternatives Considered**:
- Wait for `Saved` event → impossible to detect externally (no log marker)
- Automate title screen save loading → complex, fragile, P2 feature
- Never terminate → orphaned processes
- SIGKILL immediately → risk of data corruption if mod is mid-write

**Source**: `LivingRoots/Controllers/ModController.cs:1089-1102` (Saved event handler emits no log); spec 008 clarifications session 1

---

## 8. Pre-Flight Validation

**Decision**: Validate game, SMAPI, and Xvfb before launching

**Rationale**: Pre-flight checks provide actionable error messages before attempting launch. Each check follows the "What → Where → How to fix" pattern with distinct exit codes. This mirrors the existing CI preflight pattern in `.github/workflows/build_and_test.yml` and `scripts/check-protected-paths.sh` conventions (set -euo pipefail, table-driven checks, aggregated failure count).

**Alternatives Considered**:
- No pre-flight → cryptic errors when game fails to launch
- Post-launch validation → too late; wasted time on doomed launch

**Source**: `.github/workflows/build_and_test.yml:87-109`; `scripts/check-protected-paths.sh`; IEEE 829-2008; xUnit fixture patterns

---

## Summary of Decisions

| # | Decision | Confidence | Basis |
|---|----------|------------|-------|
| 1 | SMAPI env vars: NO_TERMINAL, USE_CURRENT_SHELL, DEVELOPER_MODE | High | SMAPI official docs |
| 2 | Direct Xvfb lifecycle management | High | Debian bug #1095028 |
| 3 | Parse SMAPI-latest.txt with mod name filter | High | SMAPI source + mod manifest |
| 4 | flock for concurrency prevention | High | Kernel-managed atomicity |
| 5 | pgrep for process detection | High | Politowski et al. 2022 |
| 6 | EXIT trap + signal forwarding | High | BashFAQ/062, Rasheed et al. 2022 |
| 7 | sysexits.h exit codes | High | BSD standard, IEEE 829-2008 |
| 8 | Data file naming from ModConstants | High | Codebase (ModConstants.cs) |
| 9 | Terminate after success marker | High | Codebase (ModController.cs) |
| 10 | Pre-flight validation | High | Existing CI patterns |
