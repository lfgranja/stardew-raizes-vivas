---

description: "Task list for feature 008 — Smoke Validation Script"
---

# Tasks: Smoke Validation Script (008)

**Input**: Design documents from `/specs/008-smoke-validation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Tests are REQUIRED for this feature (TDD). The test suite is a **bash harness** that automates the 26 test cases of `quickstart.md`'s TC table. Because bash has no property-testing framework, the analogue is used: **table-driven property tests** that assert an invariant over a whole table of inputs (the 17-row exit-code translation table, the 11-row option surface, a matrix of path fixtures, a matrix of synthetic SMAPI log fixtures). Each property test iterates its table and asserts the same invariant for every row.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

**Authoritative sources** (read these before implementing; do not re-derive):
- `specs/008-smoke-validation/spec.md` → `### Functional Requirements` (FR-001…FR-017), `### Command-Line Options (normative)`, `### Success Criteria` (SC-001…SC-015), `### Edge Cases`, `### Declared side effects`
- `specs/008-smoke-validation/data-model.md` → exit-code table + FR-007 translation table, SMAPI Log record shape, Process State
- `specs/008-smoke-validation/quickstart.md` → the TC-1…TC-26 table (the test specification), `### Command-Line Options`, `### Requirement-Coverage Summary`
- `specs/008-smoke-validation/plan.md` → structure decision, constraints, gate definitions
- `specs/008-smoke-validation/research.md`, `research-bash-security.md` → evidence bases (non-normative; §1.5/§1.7 of the latter are **superseded**)

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to
- Include exact file paths in descriptions

## File layout

```text
scripts/
├── smoke-validation.sh        # THE FEATURE — single file (plan.md "Structure Decision")
└── tests/                     # TDD harness (validation infrastructure, not the feature)
    ├── lib/
    │   └── harness.sh         # shared assertions + fixture builders
    ├── test-cli.sh            # TC-18, TC-19 + option-surface property
    ├── test-preflight.sh      # TC-20…TC-24 + path-property table
    ├── test-logparse.sh       # FR-003 record-shape + gate-leg property
    ├── test-integration.sh    # TC-1…TC-17, TC-25, TC-26
    └── run-all.sh             # runner
```

**Every file in `scripts/tests/` MUST be `bash`-runnable, MUST pass `bash -n`, and MUST use `set -Eeuo pipefail`.** The harness must never touch the real game installation except through the script's own CLI (`--game-path`, `--mods-path`, `--mod-project`), so a failing test can never damage the user's install.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Test harness scaffolding and the script's skeleton

- [ ] T001 Create `scripts/tests/lib/harness.sh` — the shared test library. It MUST provide: `assert_eq <expected> <actual> <label>`, `assert_contains <haystack> <needle> <label>`, `assert_not_contains`, `assert_exit <expected_code> <label>`, `assert_file_mode <path> <expected_octal> <label>`, `assert_file_absent`, `run_script <args...>` (invokes `scripts/smoke-validation.sh`, capturing stdout, stderr and the exit code into globals `RUN_STDOUT`, `RUN_STDERR`, `RUN_CODE`), `pass`/`fail`/`summary` counters, and a non-zero exit at the end when any assertion failed. It MUST also provide the **fixture builders**: `make_fake_game_dir <path>` (creates a game dir containing `Stardew Valley.dll`, an executable `StardewModdingAPI`, `StardewModdingAPI.dll`, `smapi-internal/SMAPI.Toolkit.dll`, `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll` and a `steam_appid.txt` containing `413150`), `make_broken_smapi_dir <path>` (the same minus `SMAPI.Toolkit.CoreInterfaces.dll`), `make_bad_steam_dir <path>` (the same but with a wrong app id in `steam_appid.txt`), `make_stub_xvfb <path>` (an executable stub that exits non-zero without printing a display number), `make_log_fixture <path> <kind>` (writes a synthetic SMAPI log — kinds: `mod_loaded`, `mod_skipped`, `mod_absent`, `zero_mods`, `critical_level`, `continuation_lines`, `crlf_only`, `stale_marker`). All fixtures MUST be created under a per-test `mktemp -d` and removed on exit.
- [ ] T002 [P] Create `scripts/smoke-validation.sh` with its skeleton: the shebang, `set -Eeuo pipefail`, `umask 077`, the `VERSION` constant (`0.1.0`), `SCRIPT_DIR` resolution (`SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"`), and the **complete option parser** — every option of the spec's normative table (`--game-path`, `--mods-path`, `--mod-project`, `--timeout`, `--build-timeout`, `--grace-period`, `--smapi-log`, `--force`, `--help`, `-h`, `--version`) with the table's exact placeholder and default, long-form-only parsing except `-h`, `--opt VALUE` and `--opt=VALUE` both accepted, **no abbreviation** (`--mod` and `--mods` MUST be rejected as ambiguous/unknown), `--` terminating option processing, **no positional arguments**, and unknown option / missing argument / malformed value exiting `14` with a message naming the offending option. `--help` MUST print a Usage line, an Options section with **exactly one entry per table row** (same spelling, same placeholder, same default), the "Environment (set automatically)" section listing `SMAPI_NO_TERMINAL=true`, `SMAPI_DEVELOPER_MODE=true`, `LIBGL_ALWAYS_SOFTWARE=1`, `GALLIUM_DRIVER=llvmpipe`, `SDL_VIDEODRIVER=x11`, `SDL_AUDIODRIVER=dummy`, `ALSOFT_DRIVERS=null`, `DISPLAY`, the run-log location, the run-ID format, the `VERSION` and **every** exit code; `--version` MUST print `<name> <VERSION>` and exit `0`. Every path argument MUST be expanded as `"$var"` and passed after `--`.
- [ ] T003 [P] Create `scripts/tests/run-all.sh` — the runner. It MUST discover every `scripts/tests/test-*.sh`, execute each in a subshell with `TMPDIR` pointing at a fresh `mktemp -d`, print a per-file summary and an overall total, and exit non-zero if any file reports a failure. It MUST accept file names as arguments to run a subset.

**Checkpoint**: the skeleton parses options correctly and the runner can execute a (still empty) test file.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The contracts every user story depends on — the exit-code taxonomy, path handling, the run log, the lock file and the trap design.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 [P] Implement the exit-code taxonomy and translation table in `scripts/smoke-validation.sh`. Define every code of the spec's FR-007: `0` success; `1` game installation not found; `2` SMAPI not found or not functional; `3` graphics environment present but unusable; `4` timeout; `5` mod load failure; `6` lock contention; `7` process conflict; `9` missing host tool or runtime dependency; `10` virtual display could not be allocated or started; `11` Steam API prerequisite missing; `12` build or artifact-identity failure; `13` internal script error; `14` usage or CLI-argument error; `15` the game produced no SMAPI log; `16` mods path or mod manifest invalid; `17` SMAPI aborted before mod resolution; plus the POSIX `128+N` statuses `129` (SIGHUP), `130` (SIGINT), `143` (SIGTERM). `8` is **retired and never reused**. Implement the FR-007 translation table exactly: `0`→`0`; `124`→`4`; `125`→`13`; `126`→`9`; `127`→`9`; `131`→`2`; `134`→`2`; `135`→`13`; `137`→`5` when it is the game process, `2` when it is the SMAPI apphost, `13` otherwise; `154`→`2`; `128+N` unchanged; **any other non-zero → `13`**. Implement the **log-based classification precedence**: a run in which the sentinel was observed but the mod-resolution header never appeared and the process exited is `17` whatever the status was, and a log showing a graphics-device failure is `3` — the log-based rule MUST be applied before the status translation. The script MUST never let a subprocess-specific status become its own final code.
- [ ] T005 Implement path handling in `scripts/smoke-validation.sh`. Provide `canonicalize_existing <path>` (`realpath -e`) and `canonicalize_maybe_missing <path>` (`realpath -m`) — the default `-E` MUST NOT be used, because it fails on exactly the paths the script mutates. Provide `path_under_base <real_path> <real_base>` implementing the **component-boundary** prefix test (`case "$real" in "$base"/*)`) where the base is the canonicalized base **with no trailing slash** (a base that keeps one yields `…/Stardew Valley//*`, which never matches). **No character allowlist may be applied to environment-derived names** — the user-supplied roots (`--game-path`, `--mods-path`, `--smapi-log`, `--mod-project`), the game's own file names and everything inherited from them are unrestricted. Only the script's **own authored literals** (`Mods`, `LivingRoots`, `smapi-internal`, `ErrorLogs`, `steam_appid.txt`, `StardewModdingAPI`, `StardewModdingAPI.dll`, `SMAPI.Toolkit.dll`, `SMAPI.Toolkit.CoreInterfaces.dll`, `SMAPI-latest.txt`, `SMAPI-crash.txt`, `StardewModdingAPI.crash.marker`, `StardewModdingAPI.update.marker`, `manifest.json`, `bin`) are constrained to `[A-Za-z0-9._-]+`. No Unicode normalization is performed. Every path MUST be expanded as `"$var"` and passed after `--` to external commands.
- [ ] T006 Implement the run log in `scripts/smoke-validation.sh`. The run log lives at `${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log`; the run ID has the form `<UTC timestamp>-<PID>` (e.g. `20261019T183302Z-48213`); the script MUST define `VERSION` and stamp it in the run-log header and the stdout summary. The run-log **file** MUST have mode `0600` and **every** directory level below the XDG base (`living-roots-smoke/` and `runs/`) MUST have mode `0700` — a directory's mode is NOT inherited by the files inside it, and `mkdir -p -m 0700` applies the mode to the **final path component only**, so the mode MUST be enforced with an explicit `chmod 0700` per level. The run log MUST contain every line the script emits at any level. The stdout summary MUST carry the verdict, the run ID, the `VERSION` and the run-log path; all level-prefixed diagnostics go to **stderr** and the verdict to **stdout**. Per-phase timings (pre-flight, rebuild-and-verify, launch, readiness wait, verdict, termination, cleanup) MUST be recorded with start, end and elapsed seconds, and the summary MUST print the total wall time.
- [ ] T007 Implement the lock file in `scripts/smoke-validation.sh`. The path MUST be `${XDG_RUNTIME_DIR}/livingroots-smoke.lock`, falling back to `${XDG_STATE_HOME:-$HOME/.local/state}/livingroots-smoke/livingroots-smoke.lock` when `XDG_RUNTIME_DIR` is unset, inside a directory of mode `0700` owned by the current user. It MUST NOT be inside a per-run `mktemp -d` directory. Open it with `exec 9<>"$lockfile"` — **not** `exec 9>"$lockfile"`, which truncates and follows symlinks. The mode MUST be `0600`, enforced with `chmod 0600 /proc/self/fd/9` (the **descriptor**, because a path-based `chmod` follows symlinks and could act on an inode other than the one `flock` holds). Acquire with `flock -n 9`; on failure exit `6` with a descriptive error. The lock file MUST **never** be removed — the kernel releases the lock when the descriptor closes, and removing the name detaches it from the locked inode.
- [ ] T008 Implement the trap design in `scripts/smoke-validation.sh`. Register the `EXIT` trap **exactly once**, at the top level; it is the single cleanup path. The cleanup function MUST capture `$?` as its very first statement and MUST end with `exit "$rc"`. It MUST be idempotent (guarded by an `in_cleanup` flag) and time-bounded. The `INT`, `TERM` and `HUP` handlers MUST forward the signal to the child process groups and then **reset their own trap and re-raise** (`trap - SIG; kill - SIG $$`) so the script dies *from* the signal and the caller observes a genuine `128+N`. `trap '' INT TERM` MUST NOT be used. Before any group signal the script MUST assert that the target PGID differs from the script's own PGID and from `1`; a failed assertion MUST abort with `13` and MUST NOT fall back to a single-PID signal. Children MUST be launched with `&`, with stdin from `/dev/null`, stdout/stderr to the run log, and waited on with `wait -f "$pid"`.

**Checkpoint**: Foundation ready — the exit codes, paths, run log, lock and traps exist and are individually testable. **All Phase 2 tasks touch `scripts/smoke-validation.sh` and are therefore SEQUENTIAL.**

---

## Phase 3: User Story 1 - Automated smoke validation of mod loading (Priority: P1) 🎯 MVP

**Goal**: A maintainer-run script that launches Stardew Valley via SMAPI under a headless Xvfb display and reports whether the Living Roots mod loaded without errors.

**Independent Test**: Run the script with the mod deployed; it exits `0` and prints `SUCCESS: Mod loaded without errors.` — or exits with the category-specific code and a "What → Where → How to fix" report.

### Tests for User Story 1 (write FIRST, ensure they FAIL before implementation) ⚠️

> **NOTE**: These MUST be written and observed to FAIL (the script does not exist / does not yet implement the behaviour) before T013…T017 begin.

- [ ] T009 [P] [US1] Write `scripts/tests/test-cli.sh` — **TC-18** (`--version` prints exactly `<name> <VERSION>` on stdout, exits `0`, writes nothing to stderr; `-h` produces byte-identical output to `--help`) and **TC-19** (an unknown option and a malformed value both exit `14`, name the offending option, and launch nothing — no SMAPI log touched, no game process, no display started, no build run). PLUS the **option-surface property**: iterate every row of the spec's normative Command-Line Options table and assert that `--help` contains that exact option spelling, its argument placeholder and its default; and assert that `--mod` and `--mods` are both **rejected** with `14` (no abbreviation).
- [ ] T010 [P] [US1] Write `scripts/tests/test-preflight.sh` — **TC-20** (a `--mods-path` that does not exist exits `16` before the build and before any launch, and the deploy target is untouched), **TC-21** (a game dir missing `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll` exits `2`, naming SMAPI and the missing file), **TC-22** (a wrong `steam_appid.txt` app id exits `11`, naming the file, the expected `413150` and the observed value), **TC-23** (a stub `Xvfb` on `PATH` that fails without printing a display number exits `10` — **not** `9` and **not** `3` — and the graphics probe is not reached), **TC-24** (an unwritable `XDG_STATE_HOME` exits `13` via the ERR trap, which reports the script name, failing line, enclosing function and exit code, and the code is **not** the raw status of the failing command). PLUS the **path-property table**: for a matrix of fixtures assert the component-boundary prefix test accepts/rejects correctly — a genuine child of the base (accept), a sibling with a shared prefix such as `/base-evil` (reject), a path with a trailing slash on the base (accept, and the base must be canonicalized first), a `..` segment (canonicalize then accept), a symlink escaping the base (reject), and a path containing a space (accept, because environment-derived names are unrestricted).
- [ ] T011 [P] [US1] Write `scripts/tests/test-logparse.sh` — the **FR-003 record-shape and gate-leg property table**. For each synthetic SMAPI log fixture assert the exact expected exit code: `mod_loaded` → `0`; `mod_skipped` (the mod in the `   Skipped mods` section) → `5`, with SMAPI's stated reason in the report; `mod_absent` (the header present, the mod not listed) → `5`; `zero_mods` (the `Loaded 0 mods.` form) → `5`, **not** `13`; a missing `Loaded` header entirely → `13`; `critical_level` (a `CRITICAL` 8-character level line) → the level MUST still be bound by label and MUST NOT shift the source column; `continuation_lines` (an unprefixed stack-trace continuation) → the continuation MUST attach to the preceding record and MUST NOT become a new record; `crlf_only` → the CR MUST be stripped before matching and no `$`-anchored pattern may be used on the raw file; `stale_marker` (content before the sentinel) → MUST be ignored. PLUS: the mod's name MUST NOT be searched across the whole log — a log whose only occurrence of the name is inside the skipped-mods section MUST yield `5`, never `0`.
- [ ] T012 [P] [US1] Write `scripts/tests/test-integration.sh` — the full-lifecycle cases from `quickstart.md`: **TC-1** (mod loads → exit `0`, `SUCCESS: Mod loaded without errors.`, the run log records the launch contract — the apphost invoked as `./StardewModdingAPI --no-terminal` with the game dir as the working directory, the seven exported environment variables, Xvfb started with `-nolisten tcp -displayfd`, and `DISPLAY` set from the reported number), **TC-2** (mod-scoped Error → exit `5`), **TC-3** (missing game → `1`), **TC-4** (game already running → `7`), **TC-5** (lock contention → `6`), **TC-6** (timeout → `4`), **TC-7** (Ctrl+C → `130` with cleanup and a **genuine** signal death — `WIFSIGNALED`, not a plain exit `130`), **TC-9** (deploy ineffective → `12`, naming both digests, before the launch), **TC-10** (a stale log cannot produce a false pass), **TC-11** (a mod skipped by SMAPI → `5` with the reason), **TC-12** (SMAPI exits without a log → `15`), **TC-14** (the resolved mods directory is the one SMAPI uses, compared on **canonicalized** paths), **TC-15** (the run log exists with the run ID, `VERSION` and the launch timestamp; the verdict on stdout, the diagnostics on stderr), **TC-16** (`--help` completeness), **TC-17** (the FR-007 translation table — every row), **TC-25** (idempotency — two consecutive runs produce the same exit code and verdict), **TC-26** (build timeout → `12`). This file MUST be skippable (`SMOKE_SKIP_INTEGRATION=1`) so the launch-free suite can run on a host without the game.

### Implementation for User Story 1

- [ ] T013 [US1] Implement the pre-flight phase in `scripts/smoke-validation.sh`, in the pinned order: (a) required host tools — `bash` 4+, `Xvfb`, `xdpyinfo`, `glxinfo`, `flock`, `timeout`, `mktemp`, `stat`, `realpath`, `pgrep`/`pidof`, `sha256sum`, `date`, `grep`, `dotnet` — each verified with `command -v`, a missing one failing closed with `9` **naming the tool**; (b) the game installation and the SMAPI markers (`Stardew Valley.dll`, the executable `StardewModdingAPI`, `StardewModdingAPI.dll`, `smapi-internal/SMAPI.Toolkit.dll`, `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll` — never `smapi-internal/SMAPI.dll`, which no release ships) failing with `1` and `2`; (c) the Steam prerequisite — `<game-path>/steam_appid.txt` must exist and contain `413150`, failing with `11`; (d) removal of the stale `SMAPI-latest.txt` and `SMAPI-crash.txt` from the log directory **and of both marker files** `<game-path>/smapi-internal/StardewModdingAPI.crash.marker` and `…update.marker`, logging each removal with the update marker's content; the log directory MUST be verified writable; (e) the virtual display and the graphics probe. The pre-flight MUST also **detect and report** foreign Xvfb instances (PID, `/proc/$PID/exe`, `/proc/$PID/stat` start time, owning UID) and MUST **not** signal them. The resolved `--mods-path` MUST be verified as an existing, writable directory before the build, failing with `16`.
- [ ] T014 [US1] Implement the virtual display and the graphics probe in `scripts/smoke-validation.sh`. Start `Xvfb -nolisten tcp -displayfd` so the **server** allocates the display; read the number back from the descriptor and record it. The script MUST NOT choose a display number and MUST NOT create, probe or remove `/tmp/.X{n}-lock` files. If Xvfb fails to start or reports no number, exit `10`. Then run the **layered** software-OpenGL probe: `xdpyinfo` exits `0` and reports `GLX` and `RANDR`; `glxinfo -B` under `LIBGL_ALWAYS_SOFTWARE=1` exits **`0`** (it exits **255** on failure — branch on "exit 0", never on a specific status); the `OpenGL renderer string` is `llvmpipe` or `softpipe`; and **full** `glxinfo` (or `glxinfo -s`) reports `GL_ARB_framebuffer_object` or `GL_EXT_framebuffer_object`, or the `OpenGL version string` is ≥ 3.0 — that last layer is load-bearing, because the game's MonoGame backend throws `PlatformNotSupportedException` without FBO support and `glxinfo -B` prints no extension list. A probe failure **with all three tools present** is a graphics stack that is present but unusable → exit `3`. On cleanup, remove only the **socket** `/tmp/.X11-unix/X{n}` of the display this script allocated.
- [ ] T015 [US1] Implement the build-and-verify phase in `scripts/smoke-validation.sh`. Remove the deploy target `<mods-path>/<ModFolderName>/` with `rm -rf -- "$target"` and **no trailing slash** (a trailing slash makes `rm` follow a symlinked final component and delete the target's *contents*), re-canonicalizing the parent immediately before the removal. Run `dotnet build --configuration Release --no-incremental` **in the project directory**, pinned with `-p:GameModsPath="<resolved mods path>"` so the deploy lands exactly where SMAPI loads from, and bounded by `--build-timeout` (default 300 s) — expiry exits `12` naming the elapsed time, and the build timeout is independent of `--timeout`. Then compute the SHA-256 digest of `<mod-project-dir>/bin/<EntryDll>` and of the deployed `<mods-path>/<ModFolderName>/<EntryDll>` and exit `12` **naming both digests** if they differ, if either is missing, or if the deployed file is **strictly** older than the build (MSBuild preserves the source mtime, so the timestamps are identical in the happy path). The build MUST be run from the resolved `--mod-project` directory, and the deploy target MUST be `<mods-path>/LivingRoots/`.
- [ ] T016 [US1] Implement the log gate and the verdict in `scripts/smoke-validation.sh`. Normalise the SMAPI log by stripping the trailing CR (`sed 's/\r$//'`) into a working copy, then split it into a record stream of `<level>\t<source>\t<text>` where the level and source are **bound by label, never by a fixed column**, and continuation lines (lines with no record prefix) get **empty** level and source. The gate has three scoped legs, each evaluated only against its own section's records: (1) the mod is listed in the loaded-mods block (the header is `Loaded <N> mods:` or `Loaded 0 mods.`; the entries are the following records whose message begins with three spaces; the manifest name MUST be matched as a substring followed by a space or end-of-line); (2) the mod is **not** in the skipped-mods section (the header message is exactly `   Skipped mods` — three leading spaces, which live *inside* the message, after the `[ts LEVEL SOURCE] ` prefix; the entries begin with six spaces and MUST carry a ` because ` suffix to be distinguished from the identically-shaped mod-warning entries); and (3) no record whose level label is `ERROR` or `ALERT` **and** whose source label equals the mod's manifest name. The mod's own marker MUST be reported but MUST NOT gate. The manifest name MUST NOT be searched across the whole log. Success MUST print exactly `SUCCESS: Mod loaded without errors.` and exit `0`. A failure MUST produce the "What → Where → How to fix" report with the **matched failure record plus its continuation lines** (never a fixed `tail -N` window) and a trailing `Fix:` line.
- [ ] T017 [US1] Implement the launch, the two-phase wait and the termination in `scripts/smoke-validation.sh`. Detect an existing Stardew Valley/SMAPI process by `/proc/$PID/exe` under the **canonicalized** game directory (bash's `$PWD` is logical while `/proc` is physical, and `~/.steam/steam` is a symlink); abort with `7`, or with `--force` terminate the **identified PID only** (never a group the script did not create) via SIGTERM → `--grace-period` → SIGKILL. Build with the deploy target pinned, then launch `./StardewModdingAPI --no-terminal` from the game directory with `set -m` enabled **around the launch only** (so the game's PGID equals its PID) and disabled again immediately after (leaving it on would move the script out of the terminal's foreground group, so a terminal SIGINT would reach the poll loop's `sleep` instead of the script and the `INT` handler would never run — measured under a pty). Export `SMAPI_NO_TERMINAL=true`, `SMAPI_DEVELOPER_MODE=true`, `LIBGL_ALWAYS_SOFTWARE=1`, `GALLIUM_DRIVER=llvmpipe`, `SDL_VIDEODRIVER=x11`, `SDL_AUDIODRIVER=dummy`, `ALSOFT_DRIVERS=null` (the game must be **muted** — Xvfb virtualizes the display, not the audio device) and `DISPLAY` from the reported display number. Then the **two-phase wait**, both phases inside the same `--timeout` deadline measured **from the instant the game was launched**: phase 1 waits for the run sentinel `Log started at <UTC> UTC` (a **TRACE**-level record — a parser reading only `INFO`+ would never find it); phase 2 waits for the mod-resolution header `Loaded <N> mods:` / `Loaded 0 mods.`. If the header appears, evaluate the gate (a missing mod → `5`); if it never appears, exit `4`; if the process exits with the sentinel seen but no header, exit `17`. On success, terminate the game's **process group** (SIGTERM → `--grace-period` → SIGKILL, with the PGID assertion) and exit `0`.

**Checkpoint**: User Story 1 is fully functional and testable independently. **All Phase 3 implementation tasks touch `scripts/smoke-validation.sh` and are therefore SEQUENTIAL.**

---

## Phase 4: User Story 2 - Mod data persistence verification (Priority: P2, DEFERRED)

**Goal**: Verify the mod's JSON sidecars exist under SMAPI's mod-data directory after a run with a save loaded.

**Independent Test**: Not applicable in this release.

**Status**: **DEFERRED by FR-004.** The automated script cannot load a save — SMAPI has no command-line save selection and the mod is purely reactive — so the title screen is the expected state, and the save check is skipped with a `SKIPPED:` warning that does not gate success. SC-004 and SC-005 state in their own text that they are **not evaluated** in this release. **This phase contains no implementation tasks in this release**; it is recorded so the deferral is explicit rather than an omission.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Gate wiring, documentation, and the final verification

- [ ] T018 [P] Wire the test suite into `lefthook.yml` — add a `scripts/tests/**/*.sh` entry to the existing `bash -n` gate (it must keep parsing **every** file individually, never `bash -n {staged_files}`, which parses only the first) and keep the `shellcheck` step skipping gracefully when the binary is absent.
- [ ] T019 [P] Update `specs/008-smoke-validation/quickstart.md` — fill in the **Actual output** and **Pass/Fail** columns of the TC table with the executed results, and record the execution date. Update the TC-18…TC-26 rows' expectations if the implementation revealed a discrepancy (and, if it did, correct the spec rather than weakening the test).
- [ ] T020 [P] Update `.specify/memory/STATE.md` — record the implementation completion, the executed TC results, and the remaining Gate-3 status.
- [ ] T021 **VERIFICATION** — run the launch-free suite: `scripts/tests/run-all.sh` excluding `scripts/tests/test-integration.sh`. It MUST exit `0` and every assertion MUST pass. Record the exact command and its output.
- [ ] T022 **VERIFICATION** — run the integration suite: `scripts/tests/test-integration.sh` against the real game installation and the real mod. Record every TC's exit code and the actual output. **This is the Gate-3 evidence** — it MUST be executed, not inferred.
- [ ] T023 **VERIFICATION** — audit `scripts/smoke-validation.sh` against `specs/008-smoke-validation/spec.md` **requirement by requirement**: for each of FR-001…FR-017, quote the clause and point at the implementing line(s) in the script; report any clause with no implementation, and any implemented behaviour with no clause. The two-phase wait, the three gate legs, the `Loaded 0 mods.` handling, the `17` classification, the `set -m` scoping, the `umask`/`chmod` modes, the quoting discipline, the `exec 9<>` lock, the `exec 9>` prohibition, the slash-free `rm -rf`, the marker removals, the mute, and the `set -Eeuo pipefail` line MUST each be confirmed present.
- [ ] T024 **VERIFICATION** — verify the exit-code contract **exhaustively**: for every row of the FR-007 translation table, assert the script maps that raw status to the documented code; assert that no subprocess-specific status can escape as the script's own final code; assert that `8` is never emitted; assert that every documented code appears in `--help`; and assert the exit code of every TC in the quickstart table matches its expected output.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately. T001 must precede T009…T012 (they use the harness); T002 and T003 are independent of each other and of T001.
- **Foundational (Phase 2)**: Depends on Setup completion — **BLOCKS all user stories**. T004 is independent of T005…T008; T005…T008 are sequential (same file).
- **User Stories (Phase 3+)**: Depends on the Foundational phase.
- **Polish (Phase 5)**: Depends on User Story 1 being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational — no dependency on other stories.
- **User Story 2 (P2)**: Deferred — no tasks in this release.

### Within Each User Story

- Tests MUST be written and observed to FAIL before implementation.
- The test harness files are disjoint and MUST be written in parallel.
- The implementation tasks all target one file and MUST be sequential.

### Parallel Opportunities

- T002, T003 and (after T001) T009, T010, T011, T012 are all disjoint files → parallel.
- T018, T019, T020 are disjoint files → parallel.
- The four verification tasks T021…T024 are disjoint concerns → parallel, and **must be the last thing that runs**.

---

## Parallel Example: User Story 1 tests

```bash
# Wave A (sequential, shared context):
Task: "T001 Create scripts/tests/lib/harness.sh"

# Wave B (parallel, disjoint files):
Task: "T009 [P] Write scripts/tests/test-cli.sh"
Task: "T010 [P] Write scripts/tests/test-preflight.sh"
Task: "T011 [P] Write scripts/tests/test-logparse.sh"
Task: "T012 [P] Write scripts/tests/test-integration.sh"

# Wave C (sequential, single file):
Task: "T013..T017 Implement scripts/smoke-validation.sh"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 — **STOP and VALIDATE** with T021/T022
4. Phase 5: Polish + verification (T021…T024 attest the result)

### Parallel Team Strategy

- Wave A: one agent on `scripts/tests/lib/harness.sh`
- Wave B: four agents, one per test file (disjoint)
- Wave C: one agent on `scripts/smoke-validation.sh` (sequential, single file — reuse the same agent for T013…T017 so the context is not lost)
- Wave D: four agents on the verification tasks (disjoint concerns)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps a task to a specific user story for traceability
- Verify tests fail before implementing
- Commit after each task or logical group (the `atomic-commit-hook` is mandatory after every file edit)
- The script is the feature; `scripts/tests/` is validation infrastructure and is NOT part of the shipped artifact
- Avoid: vague tasks, same-file conflicts, cross-story dependencies that break independence
