# Research: Smoke Validation Script

**Date**: 2026-10-19
**Context**: `specs/008-smoke-validation` — automated mod loading validation

---

## 1. SMAPI Automation Environment Variables

**Decision**: Use `SMAPI_NO_TERMINAL=true` and `SMAPI_DEVELOPER_MODE=true`.

**Rationale**: `SMAPI_NO_TERMINAL=true` prevents SMAPI from allocating a terminal (essential for headless) and is equivalent to the `--no-terminal` launch flag. `SMAPI_DEVELOPER_MODE=true` gates **console** verbosity only — SMAPI writes TRACE-level entries to the log file unconditionally, so it is not required to capture them; it is retained because it makes the launch console output usable for debugging.

> **Corrected 2026-10-19 (round 7) — `SMAPI_USE_CURRENT_SHELL` does not exist in SMAPI 4.5.2.** Verified against the installed binary: the only `SMAPI_*` string literals in `StardewModdingAPI.dll` are `SMAPI_NO_TERMINAL`, `SMAPI_MODS_PATH` and `SMAPI_DEVELOPER_MODE`; upstream `src/SMAPI/Program.cs` reads exactly those three. `SMAPI_USE_CURRENT_SHELL` is read **only** by SMAPI's installer wrapper (`src/SMAPI.Installer/assets/unix-launcher.sh`), which this design bypasses by launching the apphost directly — so it is a no-op here. The earlier rationale ("prevents SMAPI from spawning a new shell, ensuring the game process is a direct child of the script") was also wrong: SMAPI 4 runs the game **in-process** (`SCore.RunInteractively()`), so there is no shell to avoid, and the script's `&`-spawned PID *is* the game PID — which is exactly what FR-005's `set -m` process-group design assumes. **Round-7 decision: the variable is removed from FR-001, FR-015 and the `--help` contract.**

**Alternatives Considered**:
- Running without env vars → SMAPI hangs waiting for terminal input
- Using `xvfb-run` wrapper → 26-40% failure rate per Debian bug #1095028; manual Xvfb lifecycle is more reliable
- Using `SMAPI_NO_TERMINAL` only → the TRACE-level entries in the **log file** are written unconditionally, so nothing is missed; `SMAPI_DEVELOPER_MODE` only adds console verbosity

**Source**: SMAPI official documentation (smapi.io/smodder-guide); Debian bug #1095028 (xvfb-run flaky behavior)

---

## 2. Headless Game Testing via Xvfb

**Decision**: Direct Xvfb lifecycle management (launch → use → kill) within the script

**Rationale**: Direct management gives the script full control over the display resource, including cleanup on failure. The script starts Xvfb with `-displayfd`, so the **X server itself** binds the first free display and writes the number back; the script reads it and exports `DISPLAY` from it. It then launches the game and kills Xvfb during cleanup. This is more reliable than `xvfb-run` which has documented race conditions (26-40% failure rate in CI environments).

> **Corrected 2026-10-19 (round 8).** This section originally read "The script starts Xvfb on a specific display number (`:99`), waits for it to be ready, launches the game with `DISPLAY=:99`". That is superseded by the round-7 display decision: the fixed `:99` was dropped because the server allocates the display atomically under `-displayfd`, and the server writes **no lock file** at all in that mode (parsing `-displayfd` sets `nolock = TRUE`, so `LockServer()` returns without writing `/tmp/.X{n}-lock`). The script therefore never chooses, probes, creates or removes a display number's lock file (FR-012 item 5, FR-010 item 3).

**Alternatives Considered**:
- `xvfb-run` wrapper → unreliable per Debian bug #1095028; doesn't allow custom display number
- `Xvfb &` without readiness check → race condition where game launches before X server is ready
- Persistent Xvfb service → requires systemd/root; not portable to maintainer machines

**Source**: Debian bug #1095028 (xvfb-run 26-40% failure rate in cc-ci); BashFAQ/047 (trapping signals in a shell script); Politowski et al. 2022 (mc-runtime-test ephemeral pattern)

---

## 3. SMAPI Log Parsing Conventions

**Decision**: Parse `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` for mod-specific entries

**Rationale**: SMAPI writes structured log entries in `[HH:mm:ss LEVEL SOURCE] message` format. The level is a **variable-width label, bound by label and not as a five-character field**: SMAPI pads the level to **5** characters for the six *public* levels — `Monitor.MaxLevelLength` is the length of the longest public `LogLevel` name (`Trace`/`Debug`/`Error`/`Alert` = 5), and the level is rendered with `PadRight(5)` followed by a single separator space — but its **internal** `CRITICAL` level renders **8** characters and shifts the source column by three, so a five-character field is not a safe binding. The gate therefore matches the level as a label of any width and takes the source from what follows it (see the round-7 correction and the "Log Levels" line below). Lines are CRLF-terminated, so matching must be substring-based with the trailing `\r` stripped. A real mod-scoped line therefore reads `[14:32:02 ERROR Stardew Valley - Living Roots] …`. The mod's manifest name is "Stardew Valley - Living Roots" (from `manifest.json`). The script filters entries by this manifest name to distinguish mod-specific messages from SMAPI core or other mods.

> **Corrected 2026-10-19 (round 7).** The earlier "level field is padded to 8 characters" was factually wrong and contradicted the worked examples already present in `data-model.md`. Two consequences that the spec text must respect: (1) there is **no `[ERROR]` token** — the level lives *inside* the bracket group, so a literal `grep -F '[ERROR]'` matches nothing and the level must be matched as a column; (2) the canonical anchored form is SMAPI's own, `^\[(?<time>\d\d[:.]\d\d[:.]\d\d) (?<level>[a-z]+)(?: +screen_(?<screen>\d+))? +(?<modName>[^\]]+)\] ` — the level is **inside** the bracket group and is matched by **label** (`[a-z]+`, any width), because the column is not fixed-width: SMAPI core can also render `CRITICAL` (8 characters), which shifts the source column by three. Verified against the installed SMAPI 4.5.2 binary and upstream `src/SMAPI/Framework/Monitor.cs`.

The **authoritative success signal is SMAPI's own mod-resolution output**, not the mod's self-reported marker: the mod must appear in the `Loaded N mods:` block and must not appear in the `Skipped mods` section. The mod's own marker — "The 'Stardew Valley - Living Roots' mod was loaded successfully!" (Info level) — is emitted by the **mod** at `ModController.cs:959`, on SMAPI's `GameLaunched` event, not by SMAPI after `Entry()`. It therefore appears only once the game reaches the title screen, which requires a working graphics device; it is detected and reported but does not gate the verdict. *(Corrected 2026-10-19 against SMAPI 4.5.2 — the earlier attribution to SMAPI was incorrect.)*

**Log Levels**: SMAPI's **six public** levels are TRACE, DEBUG, INFO, WARN, ERROR and ALERT. SMAPI additionally has an **internal** `CRITICAL` level, which its core can render; `CRITICAL` is **8 characters** against the public maximum of 5, so the level set the gate can encounter is **not closed at six**. The gate therefore binds the level **by label**, not by a fixed column (see the round-7 correction above), and `CRITICAL` is bound like any other level. Only Error and Alert are failure markers (per FR-003) — `CRITICAL` is not one of them. WARN is logged but never fails. Trace is diagnostic-only — SMAPI writes Trace-level entries to the log file unconditionally, so `SMAPI_DEVELOPER_MODE` (which gates console verbosity only) is not required to capture them.

> **Confirmed 2026-10-22 (round 11) — the level is a variable-width label, not a five-character field.** This note does **not** restate the fact anew: the "Log Levels" line above and the round-7 correction already own it, and both are correct. What round 11 fixes is the **rationale** above, which still opened with "the level field is padded to **5** characters, not 8" — a true statement about SMAPI's `PadRight(5)` for the six *public* levels, but phrased so as to present the level as a five-character *field*, which the same section's own `CRITICAL` example contradicts. The binding is **by label, of any width**: SMAPI core can also render the internal `CRITICAL` level, which is **8 characters** and shifts the source column by three, so the gate must not treat the level as a fixed-width field. The shipped script says exactly this in its own comment above the log gate — *"the level and the source are bound BY LABEL, never by a fixed column: SMAPI pads the level to 5 characters for the six public levels, but its internal CRITICAL level renders 8 and shifts the source column by three"* (`scripts/smoke-validation.sh:3094-3098`) — and the gate's matcher takes the level as a leading run of letters (`match(rest, /^[A-Za-z]+/)`, `scripts/smoke-validation.sh:3145`), which is a variable-width match; the source is then whatever follows the label. The same rule is applied to the crash-file copy of the log (`scripts/smoke-validation.sh:3670-3672`). For the avoidance of doubt: the round-7 note's phrase "the level must be matched as a column" means *matched as the level field inside the bracket group* — not *at a fixed column*. The field is bound by its **label**, of any width; the column is not fixed.

**Run Boundary**: every log begins with `Log started at {UTC timestamp} UTC`. Only lines after that sentinel belong to the current run, so a stale log left by an earlier successful run cannot produce a false pass. SMAPI unlinks and recreates `SMAPI-latest.txt` at launch (new inode) rather than truncating it.

**Alternatives Considered**:
- Parsing stdout/stderr → SMAPI redirects output to log file; stdout is unreliable
- Using `SMAPI-crash.txt` as primary → only present on crash; `SMAPI-latest.txt` is always written
- Parsing all log entries without mod filter → false positives from other mods or SMAPI core
- Gating on the mod's own success marker → rejected: it requires a full graphics-initialised boot to the title screen and would report a graphics failure as a timeout

**Source**: SMAPI source code (`Constants.cs`, `SCore.cs`, `Monitor.cs`, `LogManager.cs`, `LogParser.cs`); SMAPI log format documentation; verified against the installed SMAPI 4.5.2; the shipped gate — `scripts/smoke-validation.sh` → log-gate comment (3094-3098), the level matcher `match(rest, /^[A-Za-z]+/)` (3145), the crash-file parser's identical rule (3670-3672)

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

**Decision**: identify Stardew Valley/SMAPI processes **by executable path**: the script discovers the game itself with a **`/proc/[0-9]*/exe` scan** — every PID whose `readlink -f /proc/$PID/exe` lies under the **canonicalized** game directory is a conflict. The game is **never** looked up by process name. `pgrep` and `pidof` are consulted **only by the orphan-Xvfb scan** (FR-012 item 10), and the host-tool sweep accepts **either** of the two.

**Rationale**: The mod has no cross-process data file locking (all locks are process-local). Concurrent instances would corrupt data files (non-atomic JSON writes) and truncate the SMAPI log. The script detects existing processes and aborts by default, with `--force` to kill them (SIGTERM → SIGKILL escalation). Identification is by executable path, not by process-name matching: the kernel's process name (`comm`) is truncated to 15 characters and `StardewModdingAPI` is 17, so an exact-name match can never succeed and would silently never match — **`pgrep -x` on the 17-character apphost is therefore forbidden**, and the shipped implementation honours that prohibition *by construction*: no process-name tool is consulted for the game at all, only the `/proc` scan is. Any PID discovered by scanning the process table MUST be verified before it is signalled.

> **Corrected 2026-10-19 (round 7).** The earlier decision (`pgrep`, with "use exact process name matching") was **contradicted by FR-011** and by the installed SMAPI 4.5.2 binary: `pgrep -x` refuses the pattern outright (procps-ng truncates `comm` to 15 chars), and `pgrep -f` over-matches deterministically — it was observed matching the invoking shell whose command line merely contained the target name. `pidof` matches on the untruncated name and on `/proc/$PID/exe`.

> **Corrected 2026-10-21 (round 10) — the game is found by a `/proc` scan; `pgrep`/`pidof` are orphan-Xvfb-only.** The round-7 note above still left `pidof` in the role of "the acceptable stock discovery tool" for the game, and the decision line repeated it. The shipped script does **not** use `pidof` (nor `pgrep`) to find the game: `find_conflicting_pids()` (`scripts/smoke-validation.sh:1768-1781`) walks `/proc/[0-9]*`, reads `readlink -f "$pid_dir/exe"`, and keeps a PID only when `path_under_base "$exe" "$game_dir"` holds — the canonicalized game directory on both sides, as FR-011 requires. `pgrep`/`pidof` appear in the script in exactly two places, both about Xvfb: `find_xvfb_pids()` (`scripts/smoke-validation.sh:1945-1957`) consults **either** tool and merges their PIDs for `preflight_orphan_displays()` (FR-012 item 10), and the tool sweep (`scripts/smoke-validation.sh:1536`) accepts **either** one and otherwise exits `9`. The `pgrep -x` prohibition is unchanged and is now satisfied by construction rather than by a rule the script could violate.
>
> **Required plan-side correction (T174).** `plan.md` → "Design Decisions (from clarification)" → the **Process identification** row currently reads: *"`/proc/$PID/exe` restricted to the resolved game path; `pidof` for discovery — never `pgrep -x` on the 17-character apphost"*. That row is to be corrected to the same wording as the decision above — the **game is discovered by a `/proc/<pid>/exe` scan** against the canonicalized game directory, **`pgrep`/`pidof` are consulted only for the orphan-Xvfb scan**, and the **"never `pgrep -x` on the 17-character apphost"** rationale is kept. `plan.md` is edited in a later wave by another session, so the plan-side edit is recorded here rather than applied.

**Alternatives Considered**:
- No detection → data corruption risk
- Lock file only → doesn't detect processes from other script invocations
- `pgrep -x` on the apphost → non-functional (17-character name vs. a 15-character `comm`)
- `pgrep -f` pattern matching → over-matches; matches unrelated shells whose command line contains the name
- `pgrep`/`pidof` for the game → **not used**: both are name-matching tools, and the game's identity must come from its executable path; they are consulted only for the orphan-Xvfb scan, where a name match is exactly what is wanted (FR-012 item 10)

**Source**: Politowski et al. 2022 (mc-runtime-test ephemeral pattern); FR-011; FR-012 items 8 and 10; `pidfd_open(2)` NOTES (a zombie PID is not recycled); `scripts/smoke-validation.sh` → `find_conflicting_pids()`, `find_xvfb_pids()`, `preflight_tool_sweep()`

### 4.3 Signal Handling & Cleanup

**Decision**: Trap SIGINT/SIGTERM + EXIT trap for universal cleanup, with the **same idempotent cleanup run on every path**: the failure path (`die`), the success path (before the verdict) and the EXIT trap (`on_exit`)

**Rationale**: The cleanup is **idempotent** and runs on **every** path — it is no longer the EXIT trap's job alone:

- **failure path** — `die()` runs the cleanup **before** it prints the failure report, so the report stays the **last** thing on stderr, and the failing run's summary still carries the cleanup's duration;
- **success path** — the main flow runs the cleanup **before the verdict**, so a cleanup failure can change the exit code without a success verdict already being on stdout;
- **EXIT trap** — `on_exit()` performs a **second, idempotent** cleanup, so an unexpected exit (or a signal) is still cleaned up. The second call is a no-op when the run has already cleaned up, and the trap's own cleanup-failure report is suppressed when the run has already reported a failure of its own, so exactly **one** failure report is emitted.

The cleanup itself: kill the game and Xvfb process groups (SIGTERM → grace period → SIGKILL), reap the children and the output stampers, remove **only the socket** of the display this script allocated, and remove the temp directory. It is **time-bounded** — every wait has an upper bound, and an unresponsive child is left unreaped rather than hanging the script past that bound — and **every step is isolated with `|| true`**, so one failure cannot prevent the others. It **never removes the lock file**: the lock is an advisory `flock` held on an open descriptor and the kernel releases it when the process dies, so deleting the file would be wrong. Bash's EXIT trap fires on all exit paths (normal, error, signal); the script traps SIGINT/SIGTERM (and SIGHUP) to forward the signal to the child process groups, and each handler then resets its own trap and re-raises the signal, so the caller observes a genuine `128+N` rather than a plain exit status.

> **Corrected 2026-10-19 (round 8).** The earlier wording "remove X11 locks" is superseded: under `-displayfd` the server writes no lock file, and FR-010 item 3 forbids the script from creating or removing `/tmp/.X{n}-lock` files. Only the **socket** of the script's own display is removed.

> **Corrected 2026-10-22 (round 11) — the cleanup now runs on the failure path and before the verdict, as well as from the EXIT trap.** This section described the older arrangement, in which the EXIT trap was the single cleanup path and the failure report was printed before any cleanup had run. The shipped script runs the **same idempotent cleanup on three paths**:
> - `die()` (`scripts/smoke-validation.sh:189-283`) calls `cleanup` at line 218, **before** it builds and prints the report at line 261 and the summary at lines 278-280 — so the failure report remains the last line on stderr, and the cleanup's lifecycle events precede the report in the run log;
> - the main flow calls `cleanup` at line 3952, **before** the success verdict at line 3961 — so a cleanup failure can change the exit code without a success verdict already being on stdout;
> - the EXIT trap's `on_exit()` (lines 1560-1610) calls `cleanup` at line 1572 — a **second, idempotent** call, so an unexpected exit or a signal is still cleaned up and the earlier call is a no-op.
>
> `cleanup()` (lines 1488-1557) is guarded by a `cleanup_done` flag (lines 1491-1492) and an `in_cleanup` re-entrancy flag (lines 1493-1494), so it is idempotent and cannot re-enter itself. It is **time-bounded**: each group kill escalates SIGTERM → grace period → SIGKILL (`terminate_group`, lines 1450-1486) and each child wait is polled against an upper bound (`reap_child`, lines 1423-1441), leaving an unresponsive child unreaped rather than hanging the script. Every step is **isolated with `|| true`** (lines 1517-1519, 1523, 1529-1534, 1541, 1545), so one failure cannot prevent the others. It **never removes the lock file**: the only removals are the X11 socket of the script's own display (lines 1540-1542) and the temp directory (lines 1544-1546); the lock is opened on descriptor 9 and held with `flock -n` (lines 1249-1262), and the kernel releases it when the process dies.

**Alternatives Considered**:
- No signal handling → orphaned game processes and X servers
- Single trap without EXIT → misses cleanup on unexpected exit
- Cleanup without `|| true` guards → one failure aborts remaining cleanup
- Cleanup only from the EXIT trap → **superseded** (round 11): the report would then precede the cleanup's own output, and a cleanup failure could not change the exit code before the verdict; the cleanup is idempotent, so running it on the failure path and before the verdict costs nothing
- Removing the lock file in the cleanup → wrong: the lock is an advisory `flock` on an open descriptor, released by the kernel on process death; deleting the file would let a later run create a *different* inode and bypass the lock

**Source**: BashFAQ/047 (trapping signals in a shell script); Bash Beginners Guide §12.2 (Traps); Mironsoft four-layer cleanup model; systemd service cleanup pattern; Rasheed et al. 2022 (651 articles confirming resource leakage as top flakiness cause); `scripts/smoke-validation.sh` → `cleanup()` (1488-1557), `die()` (189-283), `on_exit()` (1560-1610), the main flow's pre-verdict cleanup (3948-3952), `terminate_group()` (1450-1486), `reap_child()` (1423-1441), `forward_signal()` (1620-1666) and the lock acquisition (1249-1262)

---

## 5. Exit Code Conventions

**Decision**: One distinct exit code per failure category (a bespoke one-code-per-failure-class taxonomy — see the supersession note below)

**Rationale**: A per-category code lets a caller (here: the maintainer) classify the failure without reading the run log, and lets a future CI router branch on the category. The script uses **20** exit codes: 0 (success), 1 (game not found), 2 (SMAPI not found or not functional), 3 (graphics present but unusable), 4 (timeout), 5 (mod load failure), 6 (lock contention), 7 (process conflict), 9 (missing host tool or runtime dependency), 10 (the virtual display could not be allocated or started), 11 (Steam API prerequisite missing), 12 (build or artifact-identity failure), 13 (internal script error), 14 (usage or CLI-argument error), 15 (the game produced no SMAPI log), 16 (mods path or mod manifest invalid), 17 (SMAPI aborted before mod resolution), plus the POSIX `128+N` interruption codes (129 = SIGHUP, 130 = SIGINT, 143 = SIGTERM). Code `8` is deliberately absent from that list: it was **retired** in round 2 in favour of the POSIX `128+N` codes and must never be reused — FR-007's allocation rule places every new category *above* the highest published code, never in the hole `8` leaves. Every child status is translated into one of these before the script exits — no subprocess-specific status may ever become the script's final code (FR-007's translation table).

> **Superseded 2026-10-19 (round 6)** — this section originally read "Follow sysexits.h convention". `sysexits.h` defines only 64–78 (`EX__BASE` = 64) and two of the three BSDs have deprecated it (FreeBSD: "legacy … use is discouraged"; OpenBSD: "Do not use them"), so codes 1–13 are **not** sysexits values. The phrase "sysexits.h convention" is withdrawn: the scheme is sysexits-*style* (one code per failure class) with bespoke values, allocated additively. Earlier revisions of this section also listed `8 (interrupted)` (retired in round 2 in favour of POSIX `128+N`) and omitted 9/10/11 (added in the environment-verified round). The canonical table is `data-model.md` → "Exit Codes"; FR-007 is the normative source.
>
> **Corrected 2026-10-19 (round 8)** — this section's own list was still the round-4 list: it omitted `14`, `15`, `16` and `129` (all added in round 7) and still called `10` a "display conflict", a label the round-7 decision replaced with "the virtual display could not be allocated or started" (the constant was renamed to `EX_DISPLAY_UNAVAILABLE` in `data-model.md` in round 8).
>
> **Corrected 2026-10-20 (round 9)** — the round-8 note above was itself premature: the list it declared complete still omitted `17` (`EX_MOD_RESOLUTION_ABORTED`, "SMAPI aborted before mod resolution"), which round 8 allocated **above** the highest published code. `17` is now in the list, and the completeness claim is checkable against three sources that now agree: FR-007's exit-code list, `data-model.md` → "Exit Codes", and the `--help` output of the shipped `scripts/smoke-validation.sh` — 20 codes in all: `0`, `1`–`7`, `9`–`17`, `129`, `130`, `143`, with `8` retired and never reissued. The list above is now genuinely complete.

**Alternatives Considered**:
- Binary 0/1 → insufficient for actionable diagnostics
- Real `sysexits.h` values (64+) → accurate numbering, but renumbers the already-documented codes (11 at the time of the round-6 decision; 20 are published now) and the TC matrix (TC-1…TC-17 then, TC-1…TC-26 today), and sysexits' own categories (mail/network failure classes) do not fit a game-launch tool
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
| 5 | Process detection by a `/proc/$PID/exe` scan against the canonicalized game directory; `pgrep`/`pidof` consulted only for the orphan-Xvfb scan (never `pgrep -x` on the 17-character apphost) | High | FR-011; FR-012 items 8 and 10; `comm` is truncated to 15 chars |
| 6 | Idempotent cleanup on **every** path — the failure path (`die`, before the report), before the verdict, and the EXIT trap — plus signal forwarding | High | BashFAQ/047, Rasheed et al. 2022; `scripts/smoke-validation.sh` → `cleanup()`, `die()`, `on_exit()` |
| 7 | One exit code per failure category | High | POSIX.1-2024 XCU `exit`; GNU `timeout(1)`; Bazel exit-code table |
| 8 | Data file naming from ModConstants | High | Codebase (ModConstants.cs) |
| 9 | Terminate after the **log check** passes (the mod's own marker is reported, not gating) | High | FR-003, FR-006; Codebase (ModController.cs) |
| 10 | Pre-flight validation | High | Existing CI patterns |
