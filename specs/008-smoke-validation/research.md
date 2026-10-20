# Research: Smoke Validation Script

**Date**: 2026-10-19
**Context**: `specs/008-smoke-validation` — automated mod loading validation

---

## 1. SMAPI Automation Environment Variables

**Decision**: Use `SMAPI_NO_TERMINAL=true` and `SMAPI_DEVELOPER_MODE=true`.

**Rationale**: `SMAPI_NO_TERMINAL=true` prevents SMAPI from allocating a terminal (essential for headless) and is equivalent to the `--no-terminal` launch flag. `SMAPI_DEVELOPER_MODE=true` gates **console** verbosity only — SMAPI writes TRACE-level entries to the log file unconditionally, so it is not required to capture them; it is retained because it makes the launch console output usable for debugging.

> **Corrected 2026-10-19 (round 7) — `SMAPI_USE_CURRENT_SHELL` does not exist in SMAPI 4.5.2.** Verified against the installed binary: the only `SMAPI_*` string literals in `StardewModdingAPI.dll` are `SMAPI_NO_TERMINAL`, `SMAPI_MODS_PATH` and `SMAPI_DEVELOPER_MODE`; upstream `src/SMAPI/Program.cs` reads exactly those three. `SMAPI_USE_CURRENT_SHELL` is read **only** by SMAPI's installer wrapper (`src/SMAPI.Installer/assets/unix-launcher.sh`), which this design bypasses by launching the apphost directly — so it is a no-op here. The earlier rationale ("prevents SMAPI from spawning a new shell, ensuring the game process is a direct child of the script") was also wrong: SMAPI 4 runs the game **in-process** (`SCore.RunInteractively()`), so there is no shell to avoid, and the script's `&`-spawned PID *is* the game PID — which is exactly what FR-005's `set -m` process-group design assumes. See the round-7 clarification for whether FR-001/FR-015 should drop the variable.

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

**Source**: Debian bug #1095028 (xvfb-run 26-40% failure rate in cc-ci); BashFAQ/047 (trapping signals in a shell script); Politowski et al. 2022 (mc-runtime-test ephemeral pattern)

---

## 3. SMAPI Log Parsing Conventions

**Decision**: Parse `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` for mod-specific entries

**Rationale**: SMAPI writes structured log entries in `[HH:mm:ss LEVEL SOURCE] message` format — the level field is padded to **5** characters, not 8: SMAPI's `Monitor.MaxLevelLength` is the length of the longest public `LogLevel` name (`Trace`/`Debug`/`Error`/`Alert` = 5), and the level is rendered with `PadRight(5)` followed by a single separator space. Lines are CRLF-terminated, so matching must be substring-based with the trailing `\r` stripped. A real mod-scoped line therefore reads `[14:32:02 ERROR Stardew Valley - Living Roots] …`. The mod's manifest name is "Stardew Valley - Living Roots" (from `manifest.json`). The script filters entries by this manifest name to distinguish mod-specific messages from SMAPI core or other mods.

> **Corrected 2026-10-19 (round 7).** The earlier "level field is padded to 8 characters" was factually wrong and contradicted the worked examples already present in `data-model.md`. Two consequences that the spec text must respect: (1) there is **no `[ERROR]` token** — the level lives *inside* the bracket group, so a literal `grep -F '[ERROR]'` matches nothing and the level must be matched as a column; (2) the canonical anchored form is SMAPI's own, `^\[\d\d[:.]\d\d[:.]\d\d\] (?<level>[A-Z]+)(?: +screen_\d+)? +(?<source>[^\]]+)\] `. Verified against the installed SMAPI 4.5.2 binary and upstream `src/SMAPI/Framework/Monitor.cs`.

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

**Decision**: identify Stardew Valley/SMAPI processes **by executable path** (`/proc/$PID/exe` restricted to the resolved game directory); `pidof` is the acceptable stock discovery tool.

**Rationale**: The mod has no cross-process data file locking (all locks are process-local). Concurrent instances would corrupt data files (non-atomic JSON writes) and truncate the SMAPI log. The script detects existing processes and aborts by default, with `--force` to kill them (SIGTERM → SIGKILL escalation). Identification is by executable path, not by process-name matching: the kernel's process name (`comm`) is truncated to 15 characters and `StardewModdingAPI` is 17, so an exact-name match can never succeed and would silently never match. Any PID discovered by scanning the process table MUST be verified before it is signalled.

> **Corrected 2026-10-19 (round 7).** The earlier decision (`pgrep`, with "use exact process name matching") was **contradicted by FR-011** and by the installed SMAPI 4.5.2 binary: `pgrep -x` refuses the pattern outright (procps-ng truncates `comm` to 15 chars), and `pgrep -f` over-matches deterministically — it was observed matching the invoking shell whose command line merely contained the target name. `pidof` matches on the untruncated name and on `/proc/$PID/exe`.

**Alternatives Considered**:
- No detection → data corruption risk
- Lock file only → doesn't detect processes from other script invocations
- `pgrep -x` on the apphost → non-functional (17-character name vs. a 15-character `comm`)
- `pgrep -f` pattern matching → over-matches; matches unrelated shells whose command line contains the name

**Source**: Politowski et al. 2022 (mc-runtime-test ephemeral pattern); FR-011; `pidfd_open(2)` NOTES (a zombie PID is not recycled)

### 4.3 Signal Handling & Cleanup

**Decision**: Trap SIGINT/SIGTERM + EXIT trap for universal cleanup

**Rationale**: Bash's EXIT trap fires on all exit paths (normal, error, signal). The script traps SIGINT/SIGTERM to forward signals to child processes, then the EXIT trap runs full cleanup: kill game (SIGTERM → SIGKILL), kill Xvfb, remove X11 locks, remove temp files. Each cleanup step is guarded with `|| true` so one failure doesn't prevent others. Cleanup order is reverse of allocation.

**Alternatives Considered**:
- No signal handling → orphaned game processes and X servers
- Single trap without EXIT → misses cleanup on unexpected exit
- Cleanup without `|| true` guards → one failure aborts remaining cleanup

**Source**: BashFAQ/047 (trapping signals in a shell script); Bash Beginners Guide §12.2 (Traps); Mironsoft four-layer cleanup model; systemd service cleanup pattern; Rasheed et al. 2022 (651 articles confirming resource leakage as top flakiness cause)

---

## 5. Exit Code Conventions

**Decision**: One distinct exit code per failure category (a bespoke, sysexits-*style* taxonomy — see the supersession note below)

**Rationale**: A per-category code lets a caller (here: the maintainer) classify the failure without reading the run log, and lets a future CI router branch on the category. The script uses: 0 (success), 1 (game not found), 2 (SMAPI not found or not functional), 3 (graphics present but unusable), 4 (timeout), 5 (mod load failure), 6 (lock contention), 7 (process conflict), 9 (missing host tool or runtime dependency), 10 (display conflict), 11 (Steam API prerequisite missing), 12 (build or artifact-identity failure), 13 (internal script error), plus the POSIX `128+N` interruption codes (130 = SIGINT, 143 = SIGTERM). Every child status is translated into one of these before the script exits — no subprocess-specific status may ever become the script's final code (FR-007's translation table).

> **Superseded 2026-10-19 (round 6)** — this section originally read "Follow sysexits.h convention". `sysexits.h` defines only 64–78 (`EX__BASE` = 64) and two of the three BSDs have deprecated it (FreeBSD: "legacy … use is discouraged"; OpenBSD: "Do not use them"), so codes 1–13 are **not** sysexits values. The phrase "sysexits.h convention" is withdrawn: the scheme is sysexits-*style* (one code per failure class) with bespoke values, allocated additively. Earlier revisions of this section also listed `8 (interrupted)` (retired in round 2 in favour of POSIX `128+N`) and omitted 9/10/11 (added in the environment-verified round). The canonical table is `data-model.md` → "Exit Codes"; FR-007 is the normative source.

**Alternatives Considered**:
- Binary 0/1 → insufficient for actionable diagnostics
- Real `sysexits.h` values (64+) → accurate numbering, but renumbers 11 already-documented codes and the TC-1…TC-14 matrix, and sysexits' own categories (mail/network failure classes) do not fit a game-launch tool
- Raw child-status passthrough → directly violates FR-007; `dotnet build` exits `1`, which collides with "game not found"

**Source**: sysexits.h man page (for the 64–78 range and the "do not use them" position of OpenBSD/FreeBSD); POSIX.1-2024 XCU `exit` (reserved 126/127/128/>128); GNU coreutils `timeout(1)` (124/125/126/127/137); Bazel's published exit-code table (build failure vs test failure vs internal error); pytest's `ExitCode` enum; ABSG Appendix E (user-defined codes should avoid 1–2, 126–165, 255)

---

## 6. Mod Data File Naming (from codebase)

**Decision**: Data files follow `<prefix><sanitizedSaveId>.json` pattern, written under SMAPI's mod-data directory — `~/.config/StardewValley/.smapi/mod-data/lfgranja.LivingRoots/`

**Rationale**: From `ModConstants.cs`:
- `KeyPrefix = "soil_health_data_"` → soil health data files
- `CompostingBinKeyPrefix = "composting_bins_"` → composting bin data files
- Visualization config uses `visualization_config_` prefix

The saveId is sanitized via Unicode normalization (FormC) and filename sanitization (`SanitizePathSegments` in `ModDataService.cs`). Files are written through SMAPI's `IModHelper.Data.WriteJsonFile`, which resolves a relative name against SMAPI's **mod-data directory** (`~/.config/StardewValley/.smapi/mod-data/lfgranja.LivingRoots/`) — *not* the save file and *not* a `data/` folder inside the mod folder. When no save is loaded (title screen), zero data files is the expected valid state.

> **Corrected 2026-10-19 (round 7).** The earlier wording "written to the mod's own data folder" was ambiguous and could be read as a folder inside the mod directory; SMAPI's helper API never writes there.

**Alternatives Considered**: N/A — this is existing mod behavior, not a script decision.

**Source**: `LivingRoots/ModConstants.cs:9` (KeyPrefix), `LivingRoots/Services/ModDataService.cs` (WriteJsonFile, SanitizePathSegments)

---

## 7. Termination Strategy

**Decision**: Terminate game immediately after the log check passes

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
| 1 | SMAPI env vars: NO_TERMINAL, DEVELOPER_MODE (`USE_CURRENT_SHELL` does not exist in SMAPI 4.5.2 — see the round-7 correction below) | High | SMAPI 4.5.2 binary (`strings -e l`) + upstream `Program.cs` |
| 2 | Direct Xvfb lifecycle management | High | Debian bug #1095028 |
| 3 | Parse SMAPI-latest.txt with mod name filter | High | SMAPI source + mod manifest |
| 4 | flock for concurrency prevention | High | Kernel-managed atomicity |
| 5 | Process detection by executable path (`/proc/$PID/exe`), `pidof` as the stock tool | High | FR-011; `comm` is truncated to 15 chars |
| 6 | EXIT trap + signal forwarding | High | BashFAQ/047, Rasheed et al. 2022 |
| 7 | One exit code per failure category | High | POSIX.1-2024 XCU `exit`; GNU `timeout(1)`; Bazel exit-code table |
| 8 | Data file naming from ModConstants | High | Codebase (ModConstants.cs) |
| 9 | Terminate after the **log check** passes (the mod's own marker is reported, not gating) | High | FR-003, FR-006; Codebase (ModController.cs) |
| 10 | Pre-flight validation | High | Existing CI patterns |
