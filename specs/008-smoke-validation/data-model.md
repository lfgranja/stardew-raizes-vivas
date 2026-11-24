# Data Model: Smoke Validation Script

**Date**: 2026-10-19
**Revised**: 2026-10-22 — reconciled with the shipped `scripts/smoke-validation.sh`:
the log's **inode** is recorded, re-stat'ed and **consulted** (a third, independent
freshness control), the **crash context** is derived from the **normalised** record
stream with a record-computed trailing-records fallback, the **per-phase durations**
reach **stdout** and the cleanup's duration is among them, the exit-`9` row names the
**shipped** 31-tool sweep, `built` is `<project-dir>/bin/<EntryDll from the manifest>`
and is compared through the `built_snapshot` copy, the containment helper is
`check_mutating_path_under_base`, and the state-transition diagram and the FR-012 (0)
mapping carry the sweep's real position and the two new pre-flight steps (tasks
**T094**, **T095**, **T096**, **T116**, **T130**).
**Revised**: 2026-10-21 — reconciled again with the shipped `scripts/smoke-validation.sh`:
the runtime-state tables now carry every field the script maintains and no write-only flag,
the phase-timing, cleanup and process-state sections describe the shipped behaviour, and
validation rule 3's crash-context clause now states the **spec's** normative rule (FR-007)
instead of the fixed `tail -n 30` window the script used at the time (tasks **T070**,
**T072**).
**Revised**: 2026-10-20 — reconciled with the shipped `scripts/smoke-validation.sh`: the
runtime-state tables, the phase order, validation rules 3 and 5, and the exit-code table now
describe the code as it ships.
**Context**: `specs/008-smoke-validation` — entities and data structures

---

## Overview

The smoke validation script has no persistent data model of its own — it reads external artifacts (SMAPI log, mod build artifact, mod data files) and produces a single exit code. Its only *retained* output is a per-run diagnostic log (see the Run Log entity); the rest of what it writes is listed exhaustively in `spec.md` → Scope → **Declared side effects** (the build output, the deployed mod tree, the lock file it leaves in place, the X11 socket and temp directory it creates and removes). This document defines the external data structures the script interacts with and the internal state it tracks during execution.

---

## External Data Structures (Read-Only)

### SMAPI Log File

**Location**: `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` (Linux)

**Format**: a log **record** is one physical line beginning with the prefix `[HH:mm:ss LEVEL SOURCE] ` followed by the message, optionally followed by **continuation lines** that carry no prefix. The prefix binds three **labels** — a local `HH:mm:ss` timestamp, a **level token** and a **source** — and the level token is **variable width**: SMAPI pads it with `PadRight(Monitor.MaxLevelLength)`, where that constant is 5 (the longest **public** `LogLevel` name), so the six public levels render as `TRACE`, `DEBUG`, `INFO `, `WARN `, `ERROR`, `ALERT` (five characters each, `INFO`/`WARN` carrying a trailing pad space) — but `PadRight(5)` is a **no-op** on the longer internal level names, and SMAPI core can therefore also render **`CRITICAL` (8 characters)** via `Monitor.LogFatal`, which shifts the source column by three and silently mis-slices a fixed-width parse. The source is the remainder of the bracket group and may contain spaces; in split-screen mode a `screen_<n>` token precedes it. Records are terminated with **CRLF**, while newlines *inside* a message are **bare LF** — so the file is **mixed CRLF/LF**. The level and source MUST be bound **by label**, never by a fixed column offset.

**Normalisation (MUST)**: the script MUST strip the trailing carriage return into a working copy (`sed 's/\r$//'`) before any matching, because GNU `grep` does **not** strip the CR on Linux and therefore **any pattern ending in `$` never matches a CRLF-terminated line**.

**Normalised record stream (internal representation)**: `<level>\t<source>\t<text>`, one line per physical line, with the level and source fields **empty for continuation lines**. The script's gates read this stream, never the raw file.

> **Corrected 2026-10-19 (round 8).** The earlier "padded to 5 characters" was incomplete rather than wrong: it is right for the six public levels but the internal `ConsoleLogLevel` enum also contains `Critical` and `Success`, and `PadRight(5)` is a no-op on them, so `CRITICAL` (8 characters) reaches the log file and shifts the source column. The level must therefore be bound **by label**, not by a 5-character column slice. Verified against upstream `src/SMAPI/Framework/Monitor.cs` (`MaxLevelLength`, `LogStrings`) and the installed 4.5.2 binary. The level still lives **inside** the bracket group — there is no `[ERROR]` token — so Error/Alert must be matched on the level **label**, never as a literal `[ERROR]` substring.

**Levels**: `TRACE`, `DEBUG`, `INFO`, `WARN`, `ERROR`, `ALERT` (the public `LogLevel` enum — the field renders 5 characters wide, with `INFO`/`WARN` carrying a trailing pad space) **plus `CRITICAL`** (8 characters), which SMAPI core emits from `Monitor.LogFatal` (malicious-mod detection, `ExitGameImmediately`). `SUCCESS` exists in SMAPI's internal level enum but is written only to the **installer console**, never to the log file.

**Mod Identification**: The mod's manifest name is `Stardew Valley - Living Roots`. The shipped script takes the name from the **deployed manifest** (`phase_build` reads the `Name` field and falls back to that literal when the manifest has none) and hands it to the gate as the source label the mod-scoped legs compare against.

**Run boundary (MUST)**: Every log begins with the per-run sentinel `Log started at {UTC timestamp} UTC`. Only lines **after** that sentinel belong to the current run; all success/failure signals are evaluated against those lines only. SMAPI **unlinks and recreates** `SMAPI-latest.txt` at startup (new inode): the `SCore` constructor calls `PurgeNormalLogs()` (`SCore.cs:2560`), which deletes every `SMAPI-*` file in the log directory **except** `SMAPI-crash.txt` (`SCore.cs:2573`), and only then does `LogFileManager` create the new file. The sentinel is a **TRACE**-level record with source `SMAPI` (`Monitor.Log` defaults to `LogLevel.Trace`).

**Per-line timestamps**: `HH:mm:ss` in **local** time with no date — they MUST NOT be used to identify a run.

**SMAPI's own mod-resolution output** (the authoritative success gate):

| Signal | Meaning |
|--------|---------|
| `SMAPI <ver> with Stardew Valley <ver> on <os>` | Version banner — the installed SMAPI version. **Written before the run sentinel** (see below) |
| `Mods go here: <path>` | The mods directory SMAPI actually resolved. **Written before the run sentinel**, and the path is **tilde-anonymised** (`$HOME` → `~`) by `PathUtilities.AnonymizePathForDisplay` — a literal comparison against an absolute resolved path fails |
| `Loaded N mods:` + `   <Name> <Version> by <Author>` (optionally ` \| <Description>`) | Mods SMAPI loaded — the mod MUST appear here. With zero mods SMAPI writes `Loaded 0 mods.` (period, not colon), so **both** forms MUST be accepted and the zero-mod form MUST be an immediate mod-load failure, not a missing block |
| `   Skipped mods` + `      - <Name> <Version> because <reason>` | Mods SMAPI skipped — the mod MUST NOT appear here. The header literal carries **three** leading spaces (`"   Skipped mods"`, upstream `LogManager.cs:411`) and its entries **six** (`LogManager.cs:428`); anchoring on the unpadded header makes this leg vacuous and reports a skipped mod as a pass. The section header and its entries are logged by **SMAPI core** at Error level, so they are outside the mod-scoped gate |

**Mod success marker (reported, not gating)**: `The 'Stardew Valley - Living Roots' mod was loaded successfully!` (Info level). Emitted **by the mod**, not by SMAPI, from its `GameLaunched` handler (`ModController.cs:959`) — it therefore appears only after the game reaches the title screen, which requires a working graphics device.

**Failure Markers**: Error and Alert level entries attributable to the mod. Absence of the mod from the loaded-mods block (or its presence in the `   Skipped mods` section) also indicates failure.

**Crash File**: `SMAPI-crash.txt` (if present) is a **copy of the log**, written at the moment of the crash. It **overrides** `SMAPI-latest.txt` and indicates failure. The shipped script **deletes** a leftover crash file (and the two SMAPI marker files) in the pre-flight, and then **re-checks** the path after the readiness wait: a crash file present then belongs to this run and forces exit `5` — see Validation Rule 3.

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

**Deployed location**: `<mods-path>/<ModFolderName>/` — the folder is the project's **effective `ModFolderName` MSBuild property**, **read** (never pinned) by `resolve_mod_folder_name`; for this repository it resolves to `LivingRoots/` (manifest `Name` = `Stardew Valley - Living Roots`, `UniqueID` = `lfgranja.LivingRoots`, `EntryDll` = `LivingRoots.dll`)

**Identity**: The artifact carries **no build identity** — `manifest.json` declares a fixed `Version` (`0.0.1`) and the assembly a fixed `AssemblyVersion` (`1.0.0.0`); no commit identifier is embedded, and `SourceRevisionId` is inert because the project sets `GenerateAssemblyInfo=false`. Freshness is therefore established by the **deploy-effectiveness** assertion: the deployed assembly's SHA-256 digest MUST equal the digest of the assembly produced by the current build, with the build's deploy target pinned to the resolved mods path (`-p:GameModsPath`). The digest is comparable **within the run only**. The digest is taken over the **snapshot** of the built assembly (`built_snapshot`, `cp -p` into the run's private temp directory immediately after the build), not over the shared `<project-dir>/bin/` path, so a concurrent rebuild cannot rewrite the artifact between the build and the comparison.

**Failure mode**: A stale deployed assembly still emits the mod's success marker, so validating it yields a **false pass** for code that is not the current source. Two copies of this artifact may coexist (a repository-local `.gamepath/Mods/` copy and the copy in the game installation); only the copy in the resolved mods path is under test.

---

## Internal Script State (Runtime)

Every field below is a **shell global** of `scripts/smoke-validation.sh` unless it is listed
as *phase-local*. The "initial value" is the value the field carries before the phase that
sets it runs; the "set where" / "read where" columns name the functions that write and read
it.

**No write-only state.** Every field listed in the tables below is read by the code that
maintains it: a flag that is assigned and never consulted is dead state, which the
constitution's simplicity principle forbids maintaining, and no such field is listed here.

### Parsed Configuration (`parse_args()` → `apply_defaults()`)

| Field | Type | Initial value | Set where | Read where |
|-------|------|---------------|-----------|------------|
| `game_path` | string | `""` | `parse_args` (`--game-path`) or `apply_defaults` (default `${HOME}/.steam/steam/steamapps/common/Stardew Valley`) | `preflight_game`, `phase_build` (`-p:GamePath`), `launch_game` (the child's working directory) |
| `mods_path` | string | `""` | `parse_args` (`--mods-path`) or `apply_defaults` (default `${game_path%/}/Mods`; a relative value is resolved against `game_path`, FR-017) | `preflight_mods_dir`, `phase_build` (`-p:GameModsPath`), `launch_game` (`--mods-path`) |
| `mod_project` | string | `""` | `parse_args` (`--mod-project`) or `apply_defaults` (default `${REPO_ROOT}/LivingRoots/LivingRoots.csproj`); `preflight_mod_project` **replaces** it with the canonicalised path | `preflight_mod_project` (the canonicalisation and the repository-root containment test), `phase_build` (the project directory, the built-assembly path and the build itself) |
| `smapi_log` | string | `""` | `parse_args` (`--smapi-log`) or `apply_defaults` (default `${HOME}/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`); `resolve_smapi_log_paths` **replaces** it with the canonicalised path | `resolve_smapi_log_paths`, `preflight_crash_markers`, `preflight_log_baseline`, `check_crash_file`, `log_gate`, `log_has_sentinel`, `log_has_mod_resolution`, `wait_for_sentinel`, `run_log_init` |
| `timeout_secs` | integer (seconds) | `120` | `parse_args` (`--timeout`, normalised by `canonical_decimal`) | `phase_launch` (the deadline), `wait_for_sentinel`, `wait_for_mod_resolution` |
| `build_timeout_secs` | integer (seconds) | `300` | `parse_args` (`--build-timeout`) | `phase_build` (the build deadline) |
| `grace_period_secs` | integer (seconds) | `15` | `parse_args` (`--grace-period`) | `terminate_group`, `terminate_game`, `cleanup` (the reap bound), `preflight_process_conflict` |
| `force` | 0/1 flag | `0` | `parse_args` (`--force`) | `preflight_process_conflict` |
| `mods_path_set` | 0/1 flag | `0` | `parse_args` (set when `--mods-path` is given) | `apply_defaults` (decides whether the default is applied) |

### Run-Log State

| Field | Type | Initial value | Set where | Read where |
|-------|------|---------------|-----------|------------|
| `run_id` | string | `""` | `run_log_init` (`<UTC stamp>-$$`, e.g. `20261019T183302Z-48213`) | `log_event` (stamped on every line), `print_summary` (the stdout summary), `die` (the failure report) |
| `run_log_path` | string | `""` (declared twice: with the parsed configuration and again with the run-log state) | `run_log_init` (`${run_log_dir}/${run_id}.log`) | `die` and `log_event` (both write into it); the **output stampers** (`start_stamped_output`) write the children's stdout and stderr into it, one stamped `INFO` line per child line |
| `run_log_dir` | string | `""` | `run_log_init` (`${state_dir}/runs`) | `run_log_init` (the `mkdir` and the `chmod 0700`) |
| `run_log_started` | 0/1 flag | `0` | `run_log_init` (`1`, after the file has been created) | `die` and `log_event` (neither writes before the log exists — a usage error never creates it), and `print_summary`, `log_phase_timings`, `cleanup` and `on_exit` (each of them writes to the run log only once it exists) |
| `run_started_epoch` | integer (epoch seconds) | `0` — declared twice (with the parsed configuration and again with the run-log state) | `main` (assigned right after the command line is parsed and the host-tool sweep has run, so the wall time covers the whole run including a failure before the run log exists), then `run_log_init` (re-assigned once the log exists) | `print_summary` (the total wall time, printed on stdout and into the run log) |
| `phase_current` | string | `""` | `phase_begin` | `phase_end` (the phase name it records) |
| `phase_current_start` | integer (epoch seconds) | `0` | `phase_begin` | `phase_end` (the elapsed calculation) |
| `phase_names[]`, `phase_elapsed[]` | arrays | empty | `phase_end` (appends one entry per closed phase); `close_open_phase` (appends the entry of the phase that was still open when the run failed); `cleanup` (appends its own entry and its elapsed time) | `print_phase_timings` — called **directly** by `print_summary` (which prints the block on **stdout**) and through `log_phase_timings` (which writes the same block into the **run log**); `print_summary` is called on **both** the success path (`main`) and the failure path (`die`), so the per-phase durations are printed on stdout **and** into the run log on every run |

### Log-Freshness State

| Field | Type | Initial value | Set where | Read where |
|-------|------|---------------|-----------|------------|
| `smapi_log_baseline_inode` | string (inode number) | `""` | `preflight_log_baseline` (`stat -c %i -- "$smapi_log"`, read **before** the stale log is removed; the read is **attempted**, so a host with no stale log leaves the baseline empty) | `check_log_inode_freshness` (the poll-time comparison) and `verify_log_freshness` (the final assertion) — both **consult** it, and an inode that did not change aborts the run with `13` |
| `smapi_log_recreated_inode` | string (inode number) | `""` | `check_log_inode_freshness` and `verify_log_freshness` (the inode observed at the log path once the log was recreated) | `check_log_inode_freshness` and `verify_log_freshness` (the comparison against the baseline) |

The inode is therefore a **consulted** variable and a **third, independent** freshness
control — not a write-only log line. FR-002 mandates three controls and the script
implements all three:

1. the **sentinel timestamp**, compared as UTC against the launch timestamp `launch_t0`
   (`sentinel_is_fresh`) — a sentinel older than `launch_t0` is ignored and the wait
   continues;
2. the **absence** of the stale log, asserted immediately after its removal
   (`preflight_log_baseline`) — a log that survives the removal aborts the run with `13`;
3. the **inode**, which must have **changed** once the log was recreated
   (`check_log_inode_freshness` during the wait, `verify_log_freshness` as the final
   assertion) — an unchanged inode aborts the run with `13`, because it is the only
   evidence that SMAPI fell back to a player-specific log name and left the stale log
   deciding the verdict.

Control 3 is evaluated **independently of the sentinel wait**: `wait_for_sentinel` calls
`check_log_inode_freshness` on **every** poll, before the sentinel is looked for, so the
stale-log case is reported as the stale-log internal error (`13`) as soon as the
evidence is there instead of burning the whole `--timeout` budget and being reported as
a timeout (`4`).

### Lock State

| Field | Type | Initial value | Set where | Read where |
|-------|------|---------------|-----------|------------|
| `lock_path` | string | `""` | `lock_acquire` (`${XDG_RUNTIME_DIR}/livingroots-smoke.lock`, else `${XDG_STATE_HOME:-$HOME/.local/state}/livingroots-smoke/livingroots-smoke.lock`) | `lock_acquire` (the `flock` and the descriptor `chmod`) |

The lock descriptor is the **literal file descriptor 9**, not a variable: the script opens it
with `exec 9<>"$lock_path"` (read-write, create-if-absent — never the truncating `exec 9>`),
takes the lock with `flock -n 9`, and enforces the mode on the **open descriptor**
(`chmod 0600 /proc/self/fd/9`) rather than on the path. Every child closes it with `9<&-`,
so the lock is held until the script *and* its children are gone.

The lock's *state* is the kernel's flock on the descriptor, not a shell flag: the script
carries **no** "lock acquired" variable, because the descriptor's own state is the single
source of truth and a flag mirroring it would be write-only state.

### Process State

| Field | Type | Initial value | Set where | Read where |
|-------|------|---------------|-----------|------------|
| `game_pid` | string (PID) | `""` | `launch_game` (`$!` under `set -m`) | `wait_for_sentinel`, `wait_for_mod_resolution` (`kill -0`), `terminate_game` (`reap_child` → `wait -f "$pid"`) |
| `game_pgid` | string (PGID) | `""` | `launch_game` (`child_pgid "$game_pid"`; equals the PID because the launch runs under `set -m`, FR-005) | `terminate_game`, `cleanup`, `forward_signal`, `assert_signallable_group` |
| `game_exit_status` | integer (exit status) | `0` | `terminate_game` (the status `reap_child` returns from `wait -f`) | `phase_launch` — `translate_child_status "$game_exit_status" game`, with a `0` remapped to `EX_NO_LOG_PRODUCED` |
| `xvfb_pid` | string (PID) | `""` | `display_start` (`$!`) | `display_start` (`kill -0` while waiting for the display number); `cleanup` (`reap_child` → `wait -f "$pid"`, **after** the Xvfb group was signalled) — the child **is** waited on |
| `xvfb_pgid` | string (PGID) | `""` | `display_start` (`child_pgid "$xvfb_pid"`) | `cleanup`, `forward_signal`, `assert_signallable_group` |
| `build_pid` | string (PID) | `""` | `phase_build` (`$!` of the `dotnet build` subshell, launched under `set -m`) | `phase_build` (the timeout poll `kill -0` and the reaping), `cleanup` (`terminate_group`'s PID argument), `forward_signal` (the forwarded-signal log line) |
| `build_pgid` | string (PGID) | `""` | `phase_build` (`child_pgid "$build_pid"`; published **before** `assert_child_group_is_own`) — a **script-global**, not a phase-local, so the cleanup and the signal forwarder can stop an interrupted build; it is **withdrawn** (`""`) once the group is gone, because a PGID can be recycled by an unrelated process | `phase_build` (the timeout escalation), `cleanup` (`terminate_group`), `forward_signal` (`assert_signallable_group` and the forwarded signal) |
| `display_num` | string | `""` | `display_start` — the number the **server** writes to the `-displayfd` file (the script never chooses it) | `cleanup` (removes `/tmp/.X11-unix/X${display_num}`); `DISPLAY` is exported from it |
| `temp_dir` | string | `""` | `phase_display` (`mktemp -d "${TMPDIR:-/tmp}/livingroots-smoke.XXXXXX"`, mode `0700`) | `display_start` (the display-number file and the Xvfb output FIFO), `launch_game` (the game output FIFO), `phase_build` (the build log and the built-assembly snapshot), `log_gate` (the normalised log and the gate output), `check_crash_file` (the normalised crash file), `cleanup` (`rm -rf`) |
| `launch_t0` | integer (epoch seconds) | `0` | `phase_launch` (`date +%s`, taken immediately before the launch) | `phase_launch` (the readiness deadline), `wait_for_sentinel` (the freshness comparison against the sentinel) |
| `game_stamper_pid` | string (PID) | `""` | `launch_game` (`start_stamped_output` writes the stampner's PID into it) | `cleanup` (`kill -TERM`, then `reap_child` → `wait -f`) — the stamper that routes the game's stdout and stderr into the run log |
| `xvfb_stamper_pid` | string (PID) | `""` | `display_start` (`start_stamped_output`) | `cleanup` (`kill -TERM`, then `reap_child` → `wait -f`) — the stamper that routes the Xvfb output into the run log |
| `in_cleanup` | 0/1 flag | `0` | `cleanup` | `log_event` (suppresses stderr during the cleanup so the failure report stays the last thing on stderr) |
| `cleanup_done` | 0/1 flag | `0` | `cleanup` | `cleanup` (the idempotence guard) |
| `cleanup_pgid_assertion_failed` | 0/1 flag | `0` | `terminate_group` (set to `1` when the cleanup path's PGID assertion fails and the group is left unsignalled) | `die`, `on_exit` (the EXIT trap) **and** `main` — each of them turns the run's exit code into `13`, so a run that could not signal its own child group never claims success |

### Pre-Flight Flags

_None._ The pre-flight helpers `die` on failure instead of setting a flag, so
there is no pre-flight state to carry. (The declarations `smapi_ok`,
`mods_dir_ok` and `game_pgid_candidate` were removed as dead state.)

### Log-Gate State

| Field | Type | Initial value | Set where | Read where |
|-------|------|---------------|-----------|------------|
| `mod_name` | string | `"Stardew Valley - Living Roots"` | `phase_build` (the deployed manifest's `Name`; the literal is the fallback when the manifest has none) | `log_gate` — handed to the gate as `awk -v mod="$mod_name"`, so it is the source label the mod-scoped legs compare against |
| `mod_folder_name` | string | `""` | `phase_build` → `resolve_mod_folder_name` — **read**, never pinned: the project's **effective `ModFolderName` MSBuild property** (`dotnet msbuild -getProperty:ModFolderName`, bounded by a 10 s timeout), else the project's own **unconditional** `<ModFolderName>` declaration, else the `ModBuildConfig` default (the project file's base name without `.csproj`) | `phase_build` (the deploy target it composes under the mods path) |
| `mod_folder_name_source` | string | `""` | `resolve_mod_folder_name` (which of the three steps answered) | the run log — the `mod identity derived: folder='…' (…)` event |
| `mod_success_marker` | string | `""` | `log_gate` (the third line of the gate output) | `print_summary` (called from `main` on success and from `die` on failure — the marker line is printed when it was seen) — **reported, not gating** (FR-006) |
| `log_context` | string | `""` | `log_gate` (the fourth-and-later lines of the gate output: the matched record's continuation lines plus the mod's own `TRACE` records, or — on any other non-zero verdict — the trailing 30 records) | `log_gate` (the `Context:` argument of the failure report) |

### Phase-Scoped (Local) State

| Field | Type | Set where | Read where |
|-------|------|-----------|------------|
| `built` | string | `phase_build` — `<project-dir>/bin/<EntryDll read out of the mod project's manifest.json>` (FR-016 (d)); canonicalised with `realpath -m` and prefix-tested against the canonicalised project directory | the source of the **snapshot** (`cp -p -- "$built" "$built_snapshot"`); the digest comparison and the "strictly older" test consume the **snapshot**, not this path |
| `built_snapshot` | string | `phase_build` — `${temp_dir}/built-assembly.dll`, a `cp -p` copy of `built` taken **immediately after** the build, so a concurrent rebuild cannot rewrite the shared `<project-dir>/bin/` between the build and the comparison; `cp -p` preserves the mtime, so the "strictly older" rule still compares the deployed file against the mtime of the artifact this run built | **the digest comparison** (`sha256sum -- "$built_snapshot"`) and **the "strictly older" test** (`[[ "$deployed" -ot "$built_snapshot" ]]`) — both consume the snapshot; the snapshot's absence is the "the rebuild produced no assembly" branch (`12`) |
| `deployed` | string | `phase_build` — `<deploy-target>/<EntryDll read from the mod project's manifest>` (today `<mods-path>/LivingRoots/LivingRoots.dll`); canonicalised and prefix-tested against the canonicalised mods path | the digest comparison and the "strictly older" test |
| `target`, `manifest`, `unique_id` | string | `phase_build` | the deploy-target removal and the post-build manifest check |
| `build_log` | string | `phase_build` — `${temp_dir}/build-output.log` | the build output replayed into the run log |
| `marker_content` | string | `preflight_crash_markers` (read from a marker file **before** it is removed) | the run log — the marker's content, so nothing is silently discarded |
| `crash_context` | string | `check_crash_file` — the context derived from the **normalised** crash file (the trailing CR stripped): the **matched failure record together with its continuation lines**, or — when no failure record matched — the trailing **records** of the normalised stream, capped at 30 (see validation rule 3) | the `Context:` argument of the failure report |

> **Corrections to the earlier draft of this table.** The earlier version listed `lock_fd`,
> `build_output` and `log_baseline_inode` as runtime fields. The shipped script has **no**
> `lock_fd` variable (the descriptor is the literal `9`), and `build_output` is the
> phase-local `built`/`built_snapshot`/`deployed` triple. `log_baseline_inode` **is** a
> runtime field — the script-global `smapi_log_baseline_inode` — and it **is** consulted:
> see the Log-Freshness State table above. `run_log` is the shipped field's real name
> `run_log_path`.

### Exit Codes (one code per failure category — bespoke, one code per failure class)

| Code | Constant | Meaning |
|------|----------|---------|
| 0 | `EX_OK` | Success — mod loaded without errors |
| 1 | `EX_GAME_NOT_FOUND` | Game installation not found at expected path |
| 2 | `EX_SMAPI_NOT_FOUND` | SMAPI not installed or not functional — a SMAPI marker file is missing, resolves **outside** the game directory, the launcher is not executable, the assembly's version cannot be parsed, or the apphost died with `131` / `134` / `154` |
| 3 | `EX_GRAPHICS_UNUSABLE` | Graphics environment **present but unusable** — no `GLX`/`RANDR`, no software renderer, no framebuffer-object support — **or** the log records a graphics-device failure (`Failed to create graphics device`, `GPU: Could not detect`) |
| 4 | `EX_TIMEOUT` | The run sentinel or the mod-resolution header did not appear within the `--timeout` window measured from the launch |
| 5 | `EX_MOD_LOAD_FAILURE` | Mod failed to load — a mod-scoped `ERROR`/`ALERT` record, absent from the loaded-mods block, present in the `   Skipped mods` section, **or the game crashed during the run** (a crash file appeared after the readiness wait) — or the game process was SIGKILLed (status `137`, role `game`) |
| 6 | `EX_LOCK_CONTENTION` | Another smoke test instance is running (flock failed) |
| 7 | `EX_PROCESS_CONFLICT` | Existing Stardew Valley/SMAPI process detected |
| 9 | `EX_MISSING_DEPENDENCY` | A required host tool is absent. The **host-tool sweep** verifies **31** tools with `command -v` — `Xvfb`, `xdpyinfo`, `glxinfo`, `flock`, `timeout`, `mktemp`, `stat`, `realpath`, `sha256sum`, `date`, `grep`, `dotnet`, `ps`, `readlink`, `sed`, `awk`, `cut`, `head`, `tr`, `cp`, `cat`, `sort`, `touch`, `mkdir`, `chmod`, `rm`, `dirname`, `basename`, `tail`, `sleep`, `mkfifo` — plus **one either/or rule**: **`pgrep` or `pidof`**, so **either** one satisfies the sweep and only the absence of **both** fails it. The sweep names the **package** that provides each missing tool (`tool_package_for`). `bash` 4+ is a **separate** check in the pre-flight (`preflight_tools`), and an `Xvfb` that is present but **cannot be executed** also exits `9` |
| 10 | `EX_DISPLAY_UNAVAILABLE` | The virtual display could not be allocated or started — Xvfb failed, or reported no display number. (Renamed from `EX_DISPLAY_CONFLICT` in round 8: the conflict policy was dropped with the fixed `:99` display, and the server now allocates the display atomically) |
| 11 | `EX_STEAM_PREREQ_MISSING` | `steam_appid.txt` absent or not `413150` |
| 12 | `EX_BUILD_ARTIFACT_FAILURE` | The mod failed to build, the **deploy target could not be removed**, or the deployed assembly is missing, older, or not byte-identical to the fresh build |
| 13 | `EX_INTERNAL_ERROR` | The script itself broke — an unhandled error caught by the ERR trap, a failed PGID assertion, a failed pre-launch assertion (the stale log survived the removal, the log directory is not writable, the temp directory could not be created, the lock file could not be opened), an unknown verdict from the log gate, or an unmapped child status |
| 14 | `EX_USAGE_ERROR` | Usage or CLI-argument error — unknown option, missing required argument, malformed value |
| 15 | `EX_NO_LOG_PRODUCED` | The game exited (including status `0`) but produced no SMAPI log |
| 16 | `EX_MOD_MANIFEST_INVALID` | The resolved mods path does not exist, is not a directory or is not writable (pre-flight, FR-017), **or** the deployed mod folder / `manifest.json` / its `UniqueID` is missing or invalid (post-build, FR-017) |
| 17 | `EX_MOD_RESOLUTION_ABORTED` | SMAPI aborted before mod resolution — the run sentinel was observed but the mod-resolution header never appeared, and the process exited. Classified from the **log**, not from the status, because the statuses such an abort produces are already claimed by other categories |
| 129 | `EX_SIGHUP` | Script terminated by SIGHUP (POSIX 128+1) |
| 130 | `EX_SIGINT` | Script interrupted by SIGINT (POSIX 128+2) |
| 143 | `EX_SIGTERM` | Script terminated by SIGTERM (POSIX 128+15) |

**Coverage check against the script.** The script's `readonly EX_*` constants are exactly
`EX_OK`=0, `EX_GAME_NOT_FOUND`=1, `EX_SMAPI_NOT_FOUND`=2, `EX_GRAPHICS_UNUSABLE`=3,
`EX_TIMEOUT`=4, `EX_MOD_LOAD_FAILURE`=5, `EX_LOCK_CONTENTION`=6, `EX_PROCESS_CONFLICT`=7,
`EX_MISSING_DEPENDENCY`=9, `EX_DISPLAY_UNAVAILABLE`=10, `EX_STEAM_PREREQ_MISSING`=11,
`EX_BUILD_ARTIFACT_FAILURE`=12, `EX_INTERNAL_ERROR`=13, `EX_USAGE_ERROR`=14,
`EX_NO_LOG_PRODUCED`=15, `EX_MOD_MANIFEST_INVALID`=16, `EX_MOD_RESOLUTION_ABORTED`=17,
`EX_SIGHUP`=129, `EX_SIGINT`=130, `EX_SIGTERM`=143 — **twenty** constants, of which the
last three are the POSIX `128+N` interruption codes. This table covers **exactly** those
twenty: no code is listed here that the script does not define, and none the script defines
is missing here. Code `8` is **not** defined in the script — it is retired and is listed
only for the record (see the retired-codes table below).

**The host-tool sweep (`preflight_tool_sweep`).** The sweep is the FR-012 item 8
implementation, and it runs in `main()` **after** `parse_args` and **before** the run log
is created — so `--help`, `--version` and the exit-`14` usage error stay reachable on a
host that is missing a tool, and a missing tool is reported as a missing host tool (`9`)
rather than as the shell's own `127` or as an internal error (`13`). Its two tables are
**one table**:

- `REQUIRED_TOOLS` is the array of the **31** tools the script actually invokes;
- `tool_package_for()` is a **`case` function** — not an associative array, because
  `declare -A` is a runtime failure on bash 3 and the file must stay parseable there —
  whose arms name the package that provides each tool on Fedora (`xorg-x11-server-Xvfb`,
  `mesa-utils`, `util-linux`, `procps-ng`, `coreutils`, `grep`, `sed`, `gawk`, `dotnet-sdk`).

The two are a **bijection**: every entry of `REQUIRED_TOOLS` has a `case` arm, and every
arm names a tool in the array. A row without an arm reports `unknown-package`, and an arm
with the wrong package sends the operator after a package that does not ship the tool.
The `pgrep`-or-`pidof` requirement is **not** an entry of the array: it is the separate
either/or rule checked immediately after the loop.

**Correction — the `135` status.** The earlier version of the `EX_SMAPI_NOT_FOUND` row
lumped `135` in with `154` / `131` / `134` as "SMAPI not functional". That was wrong, and it
contradicted this document's own translation table: `translate_child_status()` maps **`135`
(a partial runtime — "Could not resolve CoreCLR path") to `13`**, not to `2`. FR-001 states
the same ("`134` → `2`, `135` → `13`, `131` → `2`, `154` → `2`"). The row above is corrected
accordingly.

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
| `135` (a partial runtime — "Could not resolve CoreCLR path") | `13` |
| `137` (SIGKILL) | `5` when it is the game process, `2` when it is the SMAPI apphost, `13` otherwise |
| `154` (the SMAPI apphost cannot find its own assembly) | `2` |
| `128+N` for SIGINT/SIGTERM/SIGHUP on the script itself | `128+N`, unchanged |
| any other non-zero | `13` |

A pre-flight probe that fails is translated by the **category of the probe**, not by the raw status the probe happened to return.

The translation is performed by `translate_child_status <status> <role>`; the `role` argument
(`game` / `apphost` / `other`) is what disambiguates `137`. **Log-based classification takes
precedence over the status translation** (FR-007): a run whose log shows a graphics-device
failure is `3` and a run whose log carries the sentinel but no mod-resolution header is `17`,
whatever status the child returned. The only place the translation is actually applied is
the "the game exited without producing a log" branch of `phase_launch`.

**Interruption note**: interrupted runs use the POSIX `128+N` convention (129 / 130 / 143). The earlier `8 = EX_INTERRUPTED` value was superseded and is not reused.

### Log Check Result

The gate is a single `awk` program (`AWK_LOG_GATE`) run over the normalised stream. Its
state is **awk-local**; only four values reach the shell, and they are read back from the
gate's output file (`${temp_dir}/gate.out`) **by line number**.

| Gate variable (awk-local) | Type | Meaning |
|---------------------------|------|---------|
| `seen_sentinel` | boolean | The current run's `Log started at {UTC} UTC` sentinel was seen; every record before it is discarded |
| `rec_text[]`, `rec_lvl[]`, `rec_src[]`, `rec_count` | arrays / integer | The normalised record stream the gate itself builds — every physical line after the sentinel, with the level and source **empty for continuation lines** |
| `loaded_header`, `in_loaded` | boolean | The `Loaded <N> mods:` (or `Loaded 0 mods.`) header was seen / the gate is inside the loaded-mods block |
| `loaded_count` | integer | The mod count parsed out of the header |
| `mod_listed` | boolean | The mod name appears in the loaded-mods block, followed by a space or the end of the line |
| `skipped_header`, `in_skipped` | boolean | The `   Skipped mods` header was seen / the gate is inside the skipped-mods section |
| `mod_skipped` | boolean | The mod appears in the `   Skipped mods` section, in an entry of the form `      - <Name> … because <reason>` |
| `skip_reason` | string | The text after ` because `, reported verbatim |
| `mod_error` | string | The **first** mod-scoped `ERROR`/`ALERT` record's text |
| `mod_error_rec` | integer | The record index of that failure record — the anchor the context is built from |
| `graphics_fail` | string | The first `Failed to create graphics device` / `GPU: Could not detect` text (matched **level-independently**) |
| `mod_success_marker` | string | The mod's own success marker (exact, case-sensitive match) |
| `code`, `reason` | integer / string | The verdict the ladder produced and its one-line reason |

| Shell-side value | Source | Type | Meaning |
|------------------|--------|------|---------|
| verdict code | line 1 of `gate.out` | integer | `0` (pass), `3` (graphics), `5` (mod load failure), `13` (foreign log) or `17` (aborted before mod resolution) |
| `reason` | line 2 | string | The one-line reason for a non-zero verdict |
| `mod_success_marker` | line 3 | string | The mod's marker, empty when it was not seen — **reported, not gating** |
| `log_context` | line 4 onwards | string | The failure context. On a mod-error verdict: the matched record's **continuation lines**, followed by the mod's own `TRACE` records (they carry the exception type and the `HResult`). On any **other** non-zero verdict: the **trailing 30 records** of the normalised stream. Empty on a pass |

**The gate is a decision ladder, not a boolean conjunction.** Its order is: graphics failure
(`3`) → mod-scoped `ERROR`/`ALERT` (`5`) → mod skipped (`5`) → header present but the mod not
listed (`5`) → no header at all (`17` when the sentinel was seen, `13` when it was not) →
otherwise `0`. The earlier draft expressed the pass condition as the AND conjunction
`log_fresh && mod_listed && !mod_skipped && !error_entries_found`; that formulation names the
same three legs but hides the precedence the code actually implements, so the ladder above
is the accurate description.

---

## Entity Relationships

```
Script (no persistent data model; retains only a diagnostic run log — see spec.md → Scope → Declared side effects for the full write set)
  ├── reads  → SMAPI Log File (~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt)
  ├── reads  → Mod Data Files (~/.config/StardewValley/.smapi/mod-data/lfgranja.LivingRoots/*.json) [deferred — not implemented]
  ├── builds & verifies → Mod Build Artifact (<mods-path>/<ModFolderName>/) via SHA-256 digest comparison
  ├── manages → Game Process (Stardew Valley via SMAPI apphost)
  ├── manages → Xvfb Process (virtual display)
  ├── manages → Lock File (flock)
  └── writes  → Run Log (${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log)
```

---

## Validation Rules

1. **Log Check Pass Condition**: the gate is a **decision ladder** over three scoped legs —
   the mod is listed in SMAPI's loaded-mods block, the mod is absent from SMAPI's
   skipped-mods section, and no mod-scoped `ERROR`/`ALERT` record exists. The ladder's
   precedence is: graphics failure (`3`) → mod-scoped error (`5`) → mod skipped (`5`) →
   header present but the mod not listed (`5`) → no header at all (`17` with a sentinel,
   `13` without) → otherwise `0`. The three legs are evaluated **only** against records
   that follow the current run's sentinel, and each leg only against **its own section's**
   records — the manifest name is never searched across the whole log.
2. **Freshness**: all gating signals are evaluated only against log lines after the current run's `Log started at {UTC} UTC` sentinel. The **header region** — the version banner and the `Mods go here:` line, both of which precede the sentinel — is exempt and is context only; it never gates the verdict. The `Mods go here:` path is `~`-anonymised, so any comparison uses the home-relative form. The sentinel's timestamp is parsed as **UTC** and compared against the launch timestamp `launch_t0`; a sentinel older than `launch_t0` is logged as stale and ignored, and the wait continues. The stale log is removed in the **log-baseline** phase immediately before the launch and its **absence is asserted** (see rule 3). The **inode** of the removed log is **recorded before the removal** (`smapi_log_baseline_inode`), the recreated log is **re-stat'ed**, and an inode that did **not** change aborts the run with `13` — the inode is a **consulted** variable and a **third, independent** freshness control, evaluated on every poll of the readiness wait **independently of** the sentinel wait (see the Log-Freshness State table).
3. **Crash Override and Pre-Launch Artifact Removal** *(replaces the earlier "Crash Override" rule, which asserted an override the code did not implement)*: the crash file and the stale log are handled in **two** places, and the rule is a three-part contract:
   - **`preflight_crash_markers` (in the pre-flight)** deletes a leftover `SMAPI-crash.txt` and **both** of SMAPI's marker files (`<game-path>/smapi-internal/StardewModdingAPI.crash.marker` and `…/StardewModdingAPI.update.marker`), reading each marker's content **before** deleting it and logging that content. The marker — not the crash log — is what gates SMAPI's blocking keypress read.
   - **`preflight_log_baseline` (the log-baseline phase, after the flock and the process-conflict check, immediately before the launch)** deletes the stale `SMAPI-latest.txt` and then **asserts that it is absent** — a log that survives the removal aborts the run with `13`, because a stale log left in place could decide the verdict. The log directory is created if missing and probed for writability (failure → `13`).
   - **`check_crash_file` (after the readiness wait, before the verdict)** re-checks `<dirname $smapi_log>/SMAPI-crash.txt`. Because the pre-flight deleted any leftover, a crash file present at this point belongs to **this run**: the script logs an `ERROR` event, derives the failure context from the crash file, and exits with **`5` (`EX_MOD_LOAD_FAILURE`)** — "the game crashed during the run". The check runs **before** the log gate, so a crash after the mod-resolution header is never reported as a success, and `main()` re-checks the path once more immediately before the verdict.

     **The crash context (FR-007, normative).** The crash file is normalised exactly like the main log — the trailing carriage return is stripped — and the context is emitted from that **normalised record stream**, so the CR is never rendered as control noise. The **matched failure record** is identified within the normalised stream and **its continuation lines** are the context: the record's message plus every continuation line that follows it, up to (but excluding) the next record that carries a prefix. A fixed `tail -N` window MUST **not** be used — a .NET stack trace is routinely 20–40 lines long, so such a window can consist entirely of stack frames while omitting the record that names the failure. When **no** failure record matched, the context is the trailing **records** of the normalised stream, **capped at 30 records**.

     > **Status of this clause — implemented.** The rule above is the **spec's** normative
     > rule (FR-007) and it is now the **shipped** behaviour. `check_crash_file` writes the
     > normalised crash file to `${temp_dir}/smapi-crash.normalized` (`sed 's/\r$//'`),
     > derives the context from that **normalised record stream** with `AWK_CRASH_CONTEXT`
     > — the first mod-scoped `ERROR`/`ALERT` record **together with** its continuation
     > lines — and falls back **only when no failure record matched** to the trailing
     > **records** of the normalised stream, capped at 30.
     >
     > The fallback's window is computed on the **record stream**, not as a physical-line
     > window: `AWK_CRASH_WINDOW` counts the **prefixed records** of the normalised file
     > and prints the **line number of the first line of the 30th record from the end**
     > (`start = rec_total - 29`, clamped at 1), and the context is then read from that
     > line on, with the trailing CR stripped. The raw `tail -n 30` over the CRLF file —
     > the fixed physical-line window FR-007 forbids — is **gone**.
4. **Mod-Specific Filtering**: only records whose **source label** equals the mod's manifest name (`Stardew Valley - Living Roots`, taken from the deployed manifest) are considered for the mod-scoped legs. SMAPI's own records — the skipped-mods section, the mod-update list, the Steam-achievements notice — are written at `ERROR`/`ALERT` level from source `SMAPI` and are therefore outside the mod-scoped gate. The source is bound **by label**, after a leading `screen_<n>` token is stripped.
5. **Level and Source Bound by Label** *(replaces the earlier "Level Filtering" rule — the code never slices a fixed column)*: the level is extracted from the bracket group with a leading-letter match and compared as a **label**. Only `ERROR` and `ALERT` records whose source is the mod are failure markers. `WARN` never fails; `TRACE` and `DEBUG` are diagnostic — with one exception: a mod-scoped `TRACE` record that **follows** a failure record is part of the failure context, because it carries the exception type and the `HResult`. There is no `[ERROR]` token in the log, and the internal `CRITICAL` level renders 8 characters and shifts the source column, so a column slice silently mis-binds. The earlier clause "the level is matched as a **column** inside the bracket group" was wrong and is withdrawn.
6. **Mod Marker Is Non-Gating**: the mod's own success marker is reported but does not participate in the pass condition
7. **Deploy Effectiveness**: the deployed assembly's SHA-256 digest MUST equal the freshly built assembly's digest, with the deploy target pinned to the resolved mods path. The digest is compared **within the run only** — never across runs, because the compiler's content hash (and therefore the PE timestamp and MVID) is a function of the absolute source path
8. **Idempotency**: Running the script multiple times in sequence produces the same result (no state accumulation)

### Shipped Safety Rules (no counterpart in the earlier draft)

These rules are implemented by the shipped script but were absent from this document. Every
one of them is **enforced** by the shipped script — the helper exists *and* the call is made.

9. **Graphics-Failure Classification**: a record whose text matches `Failed to create graphics
   device` or `GPU: Could not detect` is classified as a graphics failure (`3`). The match is
   **level-independent** — the failure is not necessarily logged at `ERROR` — and this leg
   takes **precedence** over every other leg of the ladder, because SMAPI creates the
   graphics device *before* it resolves any mod, so a graphics failure means the
   `Loaded N mods:` block is never written.
10. **Process-Group Safety Guard**: before any group signal, `assert_signallable_group()`
    checks that the target PGID is non-empty, differs from the script's own PGID and is not
    `1`. A failed assertion is a hard failure (`13`) with **no fallback to a single-PID
    signal**. The guard is applied to the **build**, game and Xvfb groups in
    `forward_signal()`, again to the game group in `terminate_game()`, and again to the
    **build** group in `phase_build`'s timeout path. In the **cleanup** path the assertion
    cannot `die()` — the cleanup runs inside the EXIT trap — so `terminate_group()` **logs**
    the refusal, **leaves the group unsignalled** and sets `cleanup_pgid_assertion_failed`;
    that flag is read by `die`, by the EXIT trap (`on_exit`) **and** by `main`, and each of
    them **aborts the run with `13`** instead of letting it claim success.
11. **Component-Boundary Path Containment**: `path_under_base()` is the only containment
    test — a canonical candidate is under a canonical base iff it equals `"$base"/*`, so the
    base must already be canonical and must **not** carry a trailing slash (a trailing slash
    yields `".../Mods//*"`, which never matches). It is applied to (a) the SMAPI installation
    markers, which must resolve **inside** the game directory (otherwise `2`), and (b) the
    conflicting-process detection, where a PID qualifies only when `/proc/<pid>/exe`
    resolves under the **canonicalised** game directory — never by process-name matching,
    because the kernel's `comm` is truncated to 15 characters and the apphost's name is 17.
12. **Path Canonicalisation Modes**: `canonicalize_existing()` is `realpath -e` (every
    component must exist) and is used for the game directory, the SMAPI markers, the mods
    directory, the mod project and the conflicting-process comparison.
    `canonicalize_maybe_missing()` is `realpath -m` (lexical canonicalisation, for a path
    that may not exist) and **is called** — on the mutating paths it was written for,
    through **`check_mutating_path_under_base()`**, which canonicalises the candidate with
    `realpath -m`, prefix-tests it against its base with the component-boundary test of
    rule 11, and returns the canonical spelling in `mutating_path_real`. The helper
    **never calls `die`** — it is called from a command substitution, where an `exit`
    would end only the subshell and the ERR trap would then emit a second, contradictory
    report — so the **caller** reports the failure with `die` in its own shell. The paths
    that go through it are: the deploy target and the **deployed** assembly (base: the
    canonicalised mods path), the **built** assembly (base: the canonicalised project
    directory), the two SMAPI markers (base: the canonicalised game directory), and the
    stale crash file, the stale SMAPI log and the write probe (base: the canonicalised
    SMAPI log directory). The deploy target's **parent** directory is re-canonicalised
    immediately before the removal.
13. **Literal Allowlist for the Script's Own Names Only**: `own_literal_ok()` accepts only
    `[A-Za-z0-9._-]`, and it is the **only** character test in the script. It applies
    **only** to the script's own authored names: the user-supplied roots (`--game-path`,
    `--mods-path`, `--smapi-log`, `--mod-project`) are unrestricted and may contain spaces
    and shell metacharacters. The helper **is called**: `validate_own_literals()` runs in
    the **pre-flight** (`phase_preflight`) and checks the script's authored list — `Mods`,
    `LivingRoots`, `smapi-internal`, `ErrorLogs`, `steam_appid.txt`, `StardewModdingAPI`,
    `StardewModdingAPI.dll`, `SMAPI.Toolkit.dll`, `SMAPI.Toolkit.CoreInterfaces.dll`,
    `SMAPI-latest.txt`, `SMAPI-crash.txt`, the two marker files, `manifest.json` and `bin` —
    through `assert_own_literal()`, which aborts with `13` on the first offending literal.
14. **Lock Opened Without Truncation, and the Open Does Not Gate**: the lock file is opened
    with `exec 9<>"$lock_path"` — read-write, create-if-absent — and never with the
    truncating `exec 9>`, which would destroy the inode the kernel is locking and re-open
    the race window the lock exists to close. The **open never gates**: only `flock -n 9`
    does, and a failure to take the lock exits `6` immediately. The lock file is never
    removed, and its mode is enforced on the **open descriptor** (`chmod 0600
    /proc/self/fd/9`), not on the path. Every child closes the descriptor with `9<&-`.
15. **Child-Group Ownership Assertion**: `child_pgid()` resolves a child's process group
    and **waits for the invariant to settle** — under `set -m` the child's group id
    **equals** its pid, so the very first observation can still be the parent's group.
    `assert_child_group_is_own()` then refuses the group when it is **not** the child's
    own: a child that landed in another group — the script's own group, for instance —
    must not be signalled by group, and the run aborts with `13` instead of recording a
    group the script is forbidden to signal. The assertion is made for **all three**
    children — the **build** (`phase_build`), the **game** (`launch_game`) and the **Xvfb**
    (`display_start`) — and in each case the group is **published before** the assertion,
    so the assertion's own failure path can still stop the child: a group published after
    the assertion leaves the child un-signalled, which is exactly the orphan the cleanup
    exists to prevent.
16. **`--mod-project` Containment**: `preflight_mod_project()` canonicalises the
    user-supplied project path with `realpath -e` and **prefix-tests it against the
    canonicalised repository root** with the component-boundary test of rule 11. A project
    that does not exist exits `12`, and a project **outside** the repository exits `12` as
    well. The canonical form **replaces** the user-supplied spelling, so the build and
    every path derived from it refer to the same file. This is the one user-supplied root
    that is prefix-tested against a **foreign** base — the repository root — because the
    other user-supplied roots (`--game-path`, `--mods-path`, `--smapi-log`) are their own
    bases.

---

## State Transitions

The sequence below is the **shipped** sequence, read off `main()` and the `phase_*`
functions of `scripts/smoke-validation.sh`. The `phase_begin`/`phase_end` names the script
emits are `pre-flight`, `display`, `rebuild-and-verify`, `lock`, `process-conflict-check`,
`log-baseline`, `launch`, `readiness wait`, `verdict` and `termination`; the `cleanup`
phase is appended by the cleanup, which times itself and records its own duration.

```
[Start: parse_args (the command line is parsed FIRST, so --help, --version and the
 usage error stay reachable on a host that is missing a tool) → host-tool sweep
 (31 tools + the pgrep-or-pidof rule) → HOME check → apply_defaults → run_log_init
 (the run log is created only after the command line was accepted, so a usage error
 never creates it)]
                                 ──fail──→ [Error Exit (9 = missing host tool)]
  │
  ▼
[Pre-Flight: bash 4+ and the Xvfb runnability probe → own-literal allowlist →
 game/SMAPI markers → mods dir → Steam prereq → --mod-project containment →
 crash-marker removal (the --smapi-log root is canonicalised first) →
 orphan-Xvfb report]
   ──fail──→ [Error Exit (9 = tool, 1 = game, 2 = SMAPI, 16 = mods dir, 11 = Steam)]
  │
  ▼
[Start Virtual Display (Xvfb -displayfd) + layered software-OpenGL probe]
   ──fail──→ [Error Exit (10 = display, 3 = graphics)]
  │
  ▼
[Build & Verify Artifact (deploy target pinned to --mods-path) + post-build manifest check]
   ──fail──→ [Error Exit (12 = build/artifact, 16 = manifest)]
  │
  ▼
[Acquire Flock (exec 9<> + flock -n 9)] ──fail──→ [Error Exit (6)]
  │
  ▼
[Detect Running Process] ──found──→ [Error Exit (7, or --force kills the identified PID)]
  │
  ▼
[Log Baseline: record the stale log's inode → remove the stale SMAPI log → assert it is
 gone → probe the log dir]
   ──fail──→ [Error Exit (13)]
  │
  ▼
[Launch Game (set -m; ./StardewModdingAPI --no-terminal --mods-path <mods-path>,
 cwd = <game-path>, stdin from /dev/null)]
  │
  ▼
[Readiness wait, phase 1: on every poll, check that the log's inode changed → poll for the
 run sentinel, freshness-checked against launch_t0]
   ──stale inode──→ [Error Exit (13)]
   ──game exited──→ [Error Exit (15 = no log, else the translated status)]
   ──timeout──→ [Error Exit (4)]
  │
  ▼
[Readiness wait, phase 2: poll for the mod-resolution header]
   ──timeout──→ [Error Exit (4)]
  │
  ▼
[Crash check: a crash file present now belongs to this run] ──present──→ [Error Exit (5)]
  │
  ▼
[Verdict: evaluate the log gate] ──fail──→ [Error Exit (5, 3, 17, 13)]
  │
  ▼
[Termination: SIGTERM the game group → grace period → SIGKILL, then wait]
   │
   ▼
[Cleanup: terminate the build, game and Xvfb groups → reap the children with wait -f →
 remove the display socket and the temp directory → record its own duration]
   │
   ▼
[Summary: the verdict, the mod-success marker, the run ID, the version, the run-log path,
 the total wall time and the phase durations (the cleanup's duration among them)]
   │
   ▼
[Success Exit (0)]
```

The **cleanup** runs **before** the summary on **both** paths — `main()` calls it
explicitly after the termination phase, and `die()` calls it before it prints the failure
report and the summary — so the cleanup's duration **is** part of the rendered
`phase durations:` block, and the phase the run **failed** in is closed before the
cleanup appends its own entry. The cleanup terminates the **build**, game and Xvfb
process groups, reaps the children and the two output stampers with `wait -f`, removes
the socket of the display this run allocated and the run's temp directory, and records
its own duration. The EXIT trap's own later call is a **no-op** — `cleanup` is idempotent
on the `cleanup_done` guard — so the cleanup is timed exactly **once** per run. A failed
PGID assertion in that path turns the run's exit code into **`13`**.

### Relationship to FR-012 (0)

FR-012 (0) pins the pre-flight order as (a) tool sweep, (b) game/SMAPI markers, (c) Steam
prerequisite, (d) crash-marker removal, (e) virtual display + software-OpenGL probe — and
then states: *"The build-and-verify step (FR-016) then runs, **then the flock, then the
existing-process check, then the log baseline, then the game launch**."*

The shipped `main()` now implements exactly that order, one `phase_*` function per step:

| FR-012 (0) step | Shipped phase / function | Order |
|------------------|---------------------------|-------|
| (a) required-host-tool presence | `preflight_tool_sweep` — called from `main()` **after** `parse_args` and **before** the run log is created | 0 |
| (b)–(d) pre-flight | `phase_preflight` → `preflight_tools` (the bash 4+ check and the Xvfb runnability probe), `validate_own_literals`, `preflight_game`, `preflight_mods_dir`, `preflight_steam`, `preflight_mod_project`, `preflight_crash_markers` (which canonicalises the `--smapi-log` root first), `preflight_orphan_displays` | 1 |
| (e) display + OpenGL probe | `phase_display` → `display_start`, `display_probe_graphics` | 2 |
| build-and-verify (FR-016) | `phase_build` (the FR-017 manifest check is its tail) | 3 |
| flock | `phase_lock` → `lock_acquire` | 4 |
| existing-process check | `phase_conflict_check` → `preflight_process_conflict` | 5 |
| log baseline | `phase_log_baseline` → `preflight_log_baseline` | 6 |
| game launch | `phase_launch` | 7 |

**Where the tool sweep sits.** FR-012 (0) (a) is "required-host-tool presence", and the
shipped script satisfies it with `preflight_tool_sweep` — but the sweep is **not** inside
the `pre-flight` phase: it is called by `main()` **after** `parse_args` and **before**
`run_log_init`. The command line is therefore parsed **before** the sweep, so `--help`,
`--version` and the exit-`14` usage error are still reachable on a host that is missing
one of the tools. The sweep still runs before the run log is created, because the run
log's own creation uses several of the tools (`date`, `mkdir`, `chmod`) — a sweep that
ran afterwards would report a missing `date` as the shell's own `127` and a missing
`mkdir` as an internal error (`13`) instead of as a missing host tool (`9`).

**The two pre-flight steps FR-012 (0) does not name.** The pre-flight carries two
containment steps that the (0) list does not enumerate, because they are consequences of
FR-012 item 7 rather than separate checks:

- **`preflight_mod_project`** — the `--mod-project` path is canonicalised with
  `realpath -e` and **prefix-tested against the canonicalised repository root**; a
  project outside the repository is refused with `12`. This is the one user-supplied
  root that is prefix-tested against a **foreign** base (the repository root) — the
  other user-supplied roots are their own bases.
- **`resolve_smapi_log_paths`** — the `--smapi-log` root is canonicalised (`realpath -m`
  for the file, `realpath -m` for its directory), and the paths the script constructs
  **inside** that directory — the stale crash file, the stale log and the write probe —
  are built from the canonical spelling and prefix-tested against the canonical
  directory. It runs at the head of `preflight_crash_markers` and is re-run by
  `preflight_log_baseline` if it has not run yet.

> **History of this section (recorded, not hidden).** An earlier revision of this document
> recorded a **discrepancy**: the shipped `main()` acquired the flock *before* the
> pre-flight and ran the existing-process check and the log baseline *inside* the
> pre-flight, so all three ran ahead of the build-and-verify step that FR-012 (0) places
> before them. That was the state of the code at the time, and it was documented here
> rather than papered over. The script has since been reconciled (the dedicated
> `phase_lock`, `phase_conflict_check` and `phase_log_baseline` functions, and the split of
> the old `preflight_stale_artifacts` into `preflight_crash_markers` and
> `preflight_log_baseline`), so the discrepancy **no longer exists** in the shipped
> script. The reconciliation it called for has been carried out.

### Log baseline

The log baseline is a **phase of its own** (`phase_log_baseline`), running after the flock
and the existing-process check and immediately before the launch:

- `preflight_log_baseline` **records the stale log's inode** (`smapi_log_baseline_inode`,
  read with `stat -c %i` before the removal and written to the run log), **removes** the
  stale `SMAPI-latest.txt`, and then **asserts that the log is absent** — a log that
  survives the removal aborts the run with `13`, because a stale log left in place could
  decide the verdict. It creates the log directory if it is missing and probes it for
  writability (failure → `13`).
- The **inode is a consulted variable**, not a write-only log line: `check_log_inode_freshness`
  re-stats the recreated log on **every poll** of the readiness wait and aborts the run
  with `13` when the inode is **unchanged**, and `verify_log_freshness` re-checks it as
  the final assertion once this run's sentinel has been seen. The comparison is strict —
  a changed modification time never excuses an unchanged inode. Both calls are made
  **independently of the sentinel wait**, so the stale-log case FR-002 designs the guard
  for is reported as the stale-log internal error (`13`) instead of burning the whole
  `--timeout` budget and being reported as a timeout (`4`).
- The **crash log and the two SMAPI marker files** are removed earlier, in the pre-flight
  (`preflight_crash_markers`); the marker's content is read before the removal and logged.
- `launch_t0` is recorded in `phase_launch` as the epoch second of the launch and is
  compared against the sentinel's timestamp (parsed as UTC) by `wait_for_sentinel`; a
  sentinel older than `launch_t0` is reported as stale and ignored, and the wait continues.
  The freshness control is therefore **three-fold** — the sentinel timestamp, the
  asserted absence of the stale log, and the inode that must have changed — and the
  inode is one of the three.

**Build & Verify Artifact** rebuilds the mod, removes the deploy target first
(`rm -rf -- "$target"`, no trailing slash) — the removal is **attempted** and its **result
is the evidence**, so a deploy target that could not be removed is **fatal** with its
own diagnostic (`12`, "the deploy target could not be removed", carrying `rm`'s own
error) rather than being silently skipped — and compares SHA-256 digests over the
`built_snapshot` copy; a failure exits before the game is ever launched. The manifest
check that FR-017 places after the build is the **tail of `phase_build`**, not a phase of
its own.

### Phase-timing bookkeeping

The shipped `phase_begin`/`phase_end` pair now yields a **complete** phase list: the `launch`
phase is **closed** before `readiness wait` opens (`phase_launch` calls `phase_end` right
after `launch_game`), the phase the run **failed** in is **closed** before the cleanup
appends its own entry (`close_open_phase`, called from `die` and from the EXIT trap), and
the `cleanup` phase **is** timed — the cleanup appends its own entry to
`phase_names`/`phase_elapsed` and logs `phase cleanup finished in Ns`.

The durations are **rendered** by `print_phase_timings()`. `print_summary()` calls it
**twice**: once **directly**, so the block reaches **stdout**, and once through
`log_phase_timings()`, so the same block reaches the **run log** as `INFO` events under
the `phase durations:` heading. `print_summary()` is called on **both** paths — from
`main()` on success and from `die()` on failure — so the per-phase durations are printed
on **stdout** *and* into the run log on every run.

**The cleanup's duration IS part of the rendered block.** The cleanup runs **before** the
summary on **both** paths — `main()` calls `cleanup` and then `print_summary`; `die()`
calls `cleanup` and then `print_summary` — so the `cleanup` entry is appended to
`phase_names`/`phase_elapsed` **before** the block is rendered. The same ordering closes
the phase the run **failed** in (`close_open_phase` runs before `cleanup`, in both `die`
and the EXIT trap), so the failed phase is in the block too, with its end timestamp and
its elapsed seconds. The EXIT trap's later call is a no-op: `cleanup` is idempotent on
the `cleanup_done` guard, so the cleanup is timed exactly **once** per run.

stdout therefore carries the verdict, the mod-success marker (when it was seen), the run
ID, the version, the run-log path, the total wall time **and** the `phase durations:`
block — the same block the run log carries.

The complete phase list the script records is therefore `pre-flight`, `display`,
`rebuild-and-verify`, `lock`, `process-conflict-check`, `log-baseline`, `launch`,
`readiness wait`, `verdict`, `termination` and `cleanup`.

