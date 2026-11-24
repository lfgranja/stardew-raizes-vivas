---

description: "Task list for feature 008 — Smoke Validation Script"
---

# Tasks: Smoke Validation Script (008)

**Input**: Design documents from `/specs/008-smoke-validation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Tests are REQUIRED for this feature (TDD). The test suite is a **bash harness** that automates the 26 test cases of `quickstart.md`'s TC table. Because bash has no property-testing framework, the analogue is used: **table-driven property tests** that assert an invariant over a whole table of inputs (the 12-row FR-007 exit-code translation table of `data-model.md`, of which the shipped TC-17 suite asserts 9 rows; the 11-row option surface; a matrix of path fixtures; a matrix of synthetic SMAPI log fixtures). Each property test iterates its table and asserts the same invariant for every row.

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

> **Progress (2026-10-21) — the feature was COMPLETE through Phase 6.** The build-out landed in waves: first the shared harness, the script skeleton with the normative option parser, the test runner and the four property suites (T001–T003, T009–T012, T018); then the foundational contracts (T004–T008) and the pipeline (T013–T017); then the four verification tasks (T021–T024); then the Phase 6 convergence backlog (T025–T059), which closed every gap the round-8 audit had found. **All 59 tasks of Phases 1–6 were complete at that point**, including T019 and T053 — the quickstart's 26 TC rows now carry their executed output and verdict. *This paragraph records that checkpoint, not the present state: the three convergence rounds (Phases 7, 8 and 9) were appended after it and their work is tracked in the Convergence sections below.*

> **State at the Phase-7 checkpoint (measured 2026-10-21).** At that point this file held **90** tasks and all 90 of them were complete. The four property suites passed **784 assertions** — CLI 233, pre-flight 167, log gate 79, integration 305. They were re-measured on the final tree with
>
> ```bash
> scripts/tests/run-all.sh test-cli.sh test-preflight.sh test-logparse.sh test-integration.sh
> ```
>
> → `4 files, 4 passed, 0 failed`, with the per-file totals `# tests: 233, passed: 233, failed: 0` (CLI), `# tests: 167, passed: 167, failed: 0` (pre-flight), `# tests: 79, passed: 79, failed: 0` (log gate) and `# tests: 305, passed: 305, failed: 0` (integration).
>
> The integration suite is the **Gate-3 evidence**: TC-1 ran the real installation and observed exit `0` with `SUCCESS: Mod loaded without errors.` A standalone run on the same day measured a total wall time of 31 s (`pre-flight 0s · display 2s · rebuild-and-verify 5s · lock 0s · process-conflict-check 2s · log-baseline 0s · launch 0s · readiness wait 21s · verdict 0s · termination 0s · cleanup 1s`).
>
> **Phase 7 (T060–T090) closed 31 convergence findings** — 2 `missing`, 26 `partial`, 2 `contradicts`, 3 `unrequested`: 0 CRITICAL, 3 HIGH, 11 MEDIUM, 17 LOW. The three HIGH items were the `--mod-project` path validation, the third log-freshness guard's verification half and the process-group invariant. Every task was closed by a test written first: the red-phase cases are in the four suites (TC-27…TC-33 are new numbered integration cases).
>
> **These counts are a moving target.** The convergence phases (T025–T059, T060–T090) edit `scripts/smoke-validation.sh` in place, so re-measure after every change to it

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Test harness scaffolding and the script's skeleton

- [X] T001 Create `scripts/tests/lib/harness.sh` — the shared test library. It MUST provide: `assert_eq <expected> <actual> <label>`, `assert_contains <haystack> <needle> <label>`, `assert_not_contains`, `assert_exit <expected_code> <label>`, `assert_file_mode <path> <expected_octal> <label>`, `assert_file_absent`, `run_script <args...>` (invokes `scripts/smoke-validation.sh`, capturing stdout, stderr and the exit code into globals `RUN_STDOUT`, `RUN_STDERR`, `RUN_CODE`), `pass`/`fail`/`summary` counters, and a non-zero exit at the end when any assertion failed. It MUST also provide the **fixture builders**: `make_fake_game_dir <path>` (creates a game dir containing `Stardew Valley.dll`, an executable `StardewModdingAPI`, `StardewModdingAPI.dll`, `smapi-internal/SMAPI.Toolkit.dll`, `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll` and a `steam_appid.txt` containing `413150`), `make_broken_smapi_dir <path>` (the same minus `SMAPI.Toolkit.CoreInterfaces.dll`), `make_bad_steam_dir <path>` (the same but with a wrong app id in `steam_appid.txt`), `make_stub_xvfb <path>` (an executable stub that exits non-zero without printing a display number), `make_log_fixture <path> <kind>` (writes a synthetic SMAPI log — kinds: `mod_loaded`, `mod_skipped`, `mod_absent`, `zero_mods`, `critical_level`, `continuation_lines`, `crlf_only`, `stale_marker`). All fixtures MUST be created under a per-test `mktemp -d` and removed on exit.
- [X] T002 [P] Create `scripts/smoke-validation.sh` with its skeleton: the shebang, `set -Eeuo pipefail`, `umask 077`, the `VERSION` constant (`0.1.0`), `SCRIPT_DIR` resolution (`SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"`), and the **complete option parser** — every option of the spec's normative table (`--game-path`, `--mods-path`, `--mod-project`, `--timeout`, `--build-timeout`, `--grace-period`, `--smapi-log`, `--force`, `--help`, `-h`, `--version`) with the table's exact placeholder and default, long-form-only parsing except `-h`, `--opt VALUE` and `--opt=VALUE` both accepted, **no abbreviation** (`--mod` and `--mods` MUST be rejected as ambiguous/unknown), `--` terminating option processing, **no positional arguments**, and unknown option / missing argument / malformed value exiting `14` with a message naming the offending option. `--help` MUST print a Usage line, an Options section with **exactly one entry per table row** (same spelling, same placeholder, same default), the "Environment (set automatically)" section listing `SMAPI_NO_TERMINAL=true`, `SMAPI_DEVELOPER_MODE=true`, `LIBGL_ALWAYS_SOFTWARE=1`, `GALLIUM_DRIVER=llvmpipe`, `SDL_VIDEODRIVER=x11`, `SDL_AUDIODRIVER=dummy`, `ALSOFT_DRIVERS=null`, `DISPLAY`, the run-log location, the run-ID format, the `VERSION` and **every** exit code; `--version` MUST print `<name> <VERSION>` and exit `0`. Every path argument MUST be expanded as `"$var"` and passed after `--`.
- [X] T003 [P] Create `scripts/tests/run-all.sh` — the runner. It MUST discover every `scripts/tests/test-*.sh`, execute each in a subshell with `TMPDIR` pointing at a fresh `mktemp -d`, print a per-file summary and an overall total, and exit non-zero if any file reports a failure. It MUST accept file names as arguments to run a subset.

**Checkpoint**: the skeleton parses options correctly and the runner can execute a (still empty) test file.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The contracts every user story depends on — the exit-code taxonomy, path handling, the run log, the lock file and the trap design.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Implement the exit-code taxonomy and translation table in `scripts/smoke-validation.sh`. Define every code of the spec's FR-007: `0` success; `1` game installation not found; `2` SMAPI not found or not functional; `3` graphics environment present but unusable; `4` timeout; `5` mod load failure; `6` lock contention; `7` process conflict; `9` missing host tool or runtime dependency; `10` virtual display could not be allocated or started; `11` Steam API prerequisite missing; `12` build or artifact-identity failure; `13` internal script error; `14` usage or CLI-argument error; `15` the game produced no SMAPI log; `16` mods path or mod manifest invalid; `17` SMAPI aborted before mod resolution; plus the POSIX `128+N` statuses `129` (SIGHUP), `130` (SIGINT), `143` (SIGTERM). `8` is **retired and never reused**. Implement the FR-007 translation table exactly: `0`→`0`; `124`→`4`; `125`→`13`; `126`→`9`; `127`→`9`; `131`→`2`; `134`→`2`; `135`→`13`; `137`→`5` when it is the game process, `2` when it is the SMAPI apphost, `13` otherwise; `154`→`2`; `128+N` unchanged; **any other non-zero → `13`**. Implement the **log-based classification precedence**: a run in which the sentinel was observed but the mod-resolution header never appeared and the process exited is `17` whatever the status was, and a log showing a graphics-device failure is `3` — the log-based rule MUST be applied before the status translation. The script MUST never let a subprocess-specific status become its own final code.
- [X] T005 Implement path handling in `scripts/smoke-validation.sh`. Provide `canonicalize_existing <path>` (`realpath -e`) and `canonicalize_maybe_missing <path>` (`realpath -m`) — the default `-E` MUST NOT be used, because it fails on exactly the paths the script mutates. Provide `path_under_base <real_path> <real_base>` implementing the **component-boundary** prefix test (`case "$real" in "$base"/*)`) where the base is the canonicalized base **with no trailing slash** (a base that keeps one yields `…/Stardew Valley//*`, which never matches). **No character allowlist may be applied to environment-derived names** — the user-supplied roots (`--game-path`, `--mods-path`, `--smapi-log`, `--mod-project`), the game's own file names and everything inherited from them are unrestricted. Only the script's **own authored literals** (`Mods`, `LivingRoots`, `smapi-internal`, `ErrorLogs`, `steam_appid.txt`, `StardewModdingAPI`, `StardewModdingAPI.dll`, `SMAPI.Toolkit.dll`, `SMAPI.Toolkit.CoreInterfaces.dll`, `SMAPI-latest.txt`, `SMAPI-crash.txt`, `StardewModdingAPI.crash.marker`, `StardewModdingAPI.update.marker`, `manifest.json`, `bin`) are constrained to `[A-Za-z0-9._-]+`. No Unicode normalization is performed. Every path MUST be expanded as `"$var"` and passed after `--` to external commands.
- [X] T006 Implement the run log in `scripts/smoke-validation.sh`. The run log lives at `${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log`; the run ID has the form `<UTC timestamp>-<PID>` (e.g. `20261019T183302Z-48213`); the script MUST define `VERSION` and stamp it in the run-log header and the stdout summary. The run-log **file** MUST have mode `0600` and **every** directory level below the XDG base (`living-roots-smoke/` and `runs/`) MUST have mode `0700` — a directory's mode is NOT inherited by the files inside it, and `mkdir -p -m 0700` applies the mode to the **final path component only**, so the mode MUST be enforced with an explicit `chmod 0700` per level. The run log MUST contain every line the script emits at any level. The stdout summary MUST carry the verdict, the run ID, the `VERSION` and the run-log path; all level-prefixed diagnostics go to **stderr** and the verdict to **stdout**. Per-phase timings (pre-flight, rebuild-and-verify, launch, readiness wait, verdict, termination, cleanup) MUST be recorded with start, end and elapsed seconds, and the summary MUST print the total wall time.
- [X] T007 Implement the lock file in `scripts/smoke-validation.sh`. The path MUST be `${XDG_RUNTIME_DIR}/livingroots-smoke.lock`, falling back to `${XDG_STATE_HOME:-$HOME/.local/state}/livingroots-smoke/livingroots-smoke.lock` when `XDG_RUNTIME_DIR` is unset, inside a directory of mode `0700` owned by the current user. It MUST NOT be inside a per-run `mktemp -d` directory. Open it with `exec 9<>"$lockfile"` — **not** `exec 9>"$lockfile"`, which truncates and follows symlinks. The mode MUST be `0600`, enforced with `chmod 0600 /proc/self/fd/9` (the **descriptor**, because a path-based `chmod` follows symlinks and could act on an inode other than the one `flock` holds). Acquire with `flock -n 9`; on failure exit `6` with a descriptive error. The lock file MUST **never** be removed — the kernel releases the lock when the descriptor closes, and removing the name detaches it from the locked inode.
- [X] T008 Implement the trap design in `scripts/smoke-validation.sh`. Register the `EXIT` trap **exactly once**, at the top level; it is the single cleanup path. The cleanup function MUST capture `$?` as its very first statement and MUST end with `exit "$rc"`. It MUST be idempotent (guarded by an `in_cleanup` flag) and time-bounded. The `INT`, `TERM` and `HUP` handlers MUST forward the signal to the child process groups and then **reset their own trap and re-raise** (`trap - SIG; kill - SIG $$`) so the script dies *from* the signal and the caller observes a genuine `128+N`. `trap '' INT TERM` MUST NOT be used. Before any group signal the script MUST assert that the target PGID differs from the script's own PGID and from `1`; a failed assertion MUST abort with `13` and MUST NOT fall back to a single-PID signal. Children MUST be launched with `&`, with stdin from `/dev/null`, stdout/stderr to the run log, and waited on with `wait -f "$pid"`.

**Checkpoint**: Foundation ready — the exit codes, paths, run log, lock and traps exist and are individually testable. **All Phase 2 tasks except T004 touch `scripts/smoke-validation.sh` and are therefore SEQUENTIAL** — T004 (the exit-code taxonomy) is marked `[P]` and is independent of T005…T008.

---

## Phase 3: User Story 1 - Automated smoke validation of mod loading (Priority: P1) 🎯 MVP

**Goal**: A maintainer-run script that launches Stardew Valley via SMAPI under a headless Xvfb display and reports whether the Living Roots mod loaded without errors.

**Independent Test**: Run the script with the mod deployed; it exits `0` and prints `SUCCESS: Mod loaded without errors.` — or exits with the category-specific code and a "What → Where → How to fix" report.

### Tests for User Story 1 (write FIRST, ensure they FAIL before implementation) ⚠️

> **NOTE**: These MUST be written and observed to FAIL (the script does not exist / does not yet implement the behaviour) before T013…T017 begin.

- [X] T009 [P] [US1] Write `scripts/tests/test-cli.sh` — **TC-18** (`--version` prints exactly `<name> <VERSION>` on stdout, exits `0`, writes nothing to stderr; `-h` produces byte-identical output to `--help`) and **TC-19** (an unknown option and a malformed value both exit `14`, name the offending option, and launch nothing — no SMAPI log touched, no game process, no display started, no build run). PLUS the **option-surface property**: iterate every row of the spec's normative Command-Line Options table and assert that `--help` contains that exact option spelling, its argument placeholder and its default; and assert that `--mod` and `--mods` are both **rejected** with `14` (no abbreviation).
- [X] T010 [P] [US1] Write `scripts/tests/test-preflight.sh` — **TC-20** (a `--mods-path` that does not exist exits `16` before the build and before any launch, and the deploy target is untouched), **TC-21** (a game dir missing `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll` exits `2`, naming SMAPI and the missing file), **TC-22** (a wrong `steam_appid.txt` app id exits `11`, naming the file, the expected `413150` and the observed value), **TC-23** (a stub `Xvfb` on `PATH` that fails without printing a display number exits `10` — **not** `9` and **not** `3` — and the graphics probe is not reached), **TC-24** (an unwritable `XDG_STATE_HOME` exits `13` via the ERR trap, which reports the script name, failing line, enclosing function and exit code, and the code is **not** the raw status of the failing command). PLUS the **path-property table**: for a matrix of fixtures assert the component-boundary prefix test accepts/rejects correctly — a genuine child of the base (accept), a sibling with a shared prefix such as `/base-evil` (reject), a path with a trailing slash on the base (accept, and the base must be canonicalized first), a `..` segment (canonicalize then accept), a symlink escaping the base (reject), and a path containing a space (accept, because environment-derived names are unrestricted).
- [X] T011 [P] [US1] Write `scripts/tests/test-logparse.sh` — the **FR-003 record-shape and gate-leg property table**. For each synthetic SMAPI log fixture assert the exact expected exit code: `mod_loaded` → `0`; `mod_skipped` (the mod in the `   Skipped mods` section) → `5`, with SMAPI's stated reason in the report; `mod_absent` (the header present, the mod not listed) → `5`; `zero_mods` (the `Loaded 0 mods.` form) → `5`, **not** `13`; a missing `Loaded` header → `17` when the log carries **this run's sentinel** (SMAPI started but aborted before mod resolution), and `13` only when the log carries **no run sentinel at all** (a foreign or truncated log that does not belong to this run) — the sentinel, not the header, tells the two apart, and the `no_header` fixture carries the sentinel, so the table asserts `17`; `critical_level` (a `CRITICAL` 8-character level line) → the level MUST still be bound by label and MUST NOT shift the source column; `continuation_lines` (an unprefixed stack-trace continuation) → the continuation MUST attach to the preceding record and MUST NOT become a new record; `crlf_only` → the CR MUST be stripped before matching and no `$`-anchored pattern may be used on the raw file; `stale_marker` (content before the sentinel) → MUST be ignored. PLUS: the mod's name MUST NOT be searched across the whole log — a log whose only occurrence of the name is inside the skipped-mods section MUST yield `5`, never `0`.
- [X] T012 [P] [US1] Write `scripts/tests/test-integration.sh` — the full-lifecycle cases from `quickstart.md`: **TC-1** (mod loads → exit `0`, `SUCCESS: Mod loaded without errors.`, the run log records the launch contract — the apphost invoked as `./StardewModdingAPI --no-terminal` with the game dir as the working directory, the seven exported environment variables, Xvfb started with `-nolisten tcp -displayfd`, and `DISPLAY` set from the reported number), **TC-2** (mod-scoped Error → exit `5`), **TC-3** (missing game → `1`), **TC-4** (game already running → `7`), **TC-5** (lock contention → `6`), **TC-6** (timeout → `4`), **TC-7** (Ctrl+C → `130` with cleanup and a **genuine** signal death — `WIFSIGNALED`, not a plain exit `130`), **TC-9** (deploy ineffective → `12`, naming both digests, before the launch), **TC-10** (a stale log cannot produce a false pass), **TC-11** (a mod skipped by SMAPI → `5` with the reason), **TC-12** (SMAPI exits without a log → `15`), **TC-14** (the resolved mods directory is the one SMAPI uses, compared on **canonicalized** paths), **TC-15** (the run log exists with the run ID, `VERSION` and the launch timestamp; the verdict on stdout, the diagnostics on stderr), **TC-16** (`--help` completeness), **TC-17** (the FR-007 translation table — every row), **TC-25** (idempotency — two consecutive runs produce the same exit code and verdict), **TC-26** (build timeout → `12`). This file MUST be skippable (`SMOKE_SKIP_INTEGRATION=1`) so the launch-free suite can run on a host without the game.

### Implementation for User Story 1

- [X] T013 [US1] Implement the pre-flight phase in `scripts/smoke-validation.sh`, in the pinned order: (a) required host tools — `bash` 4+, `Xvfb`, `xdpyinfo`, `glxinfo`, `flock`, `timeout`, `mktemp`, `stat`, `realpath`, `pgrep`/`pidof`, `sha256sum`, `date`, `grep`, `dotnet` — each verified with `command -v`, a missing one failing closed with `9` **naming the tool** *(this is the **initial** set; the tool-sweep task T069 later extended the sweep to every external the script actually invokes, and T163 added the `sleep` it calls in ten places — the list in this task text is the one the pre-flight was first specified with)*; (b) the game installation and the SMAPI markers (`Stardew Valley.dll`, the executable `StardewModdingAPI`, `StardewModdingAPI.dll`, `smapi-internal/SMAPI.Toolkit.dll`, `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll` — never `smapi-internal/SMAPI.dll`, which no release ships) failing with `1` and `2`; (c) the Steam prerequisite — `<game-path>/steam_appid.txt` must exist and contain `413150`, failing with `11`; (d) removal of the stale `SMAPI-latest.txt` and `SMAPI-crash.txt` from the log directory **and of both marker files** `<game-path>/smapi-internal/StardewModdingAPI.crash.marker` and `…update.marker`, logging each removal with the update marker's content; the log directory MUST be verified writable; (e) the virtual display and the graphics probe. The pre-flight MUST also **detect and report** foreign Xvfb instances (PID, `/proc/$PID/exe`, `/proc/$PID/stat` start time, owning UID) and MUST **not** signal them. The resolved `--mods-path` MUST be verified as an existing, writable directory before the build, failing with `16`.
- [X] T014 [US1] Implement the virtual display and the graphics probe in `scripts/smoke-validation.sh`. Start `Xvfb -nolisten tcp -displayfd` so the **server** allocates the display; read the number back from the descriptor and record it. The script MUST NOT choose a display number and MUST NOT create, probe or remove `/tmp/.X{n}-lock` files. If Xvfb fails to start or reports no number, exit `10`. Then run the **layered** software-OpenGL probe: `xdpyinfo` exits `0` and reports `GLX` and `RANDR`; `glxinfo -B` under `LIBGL_ALWAYS_SOFTWARE=1` exits **`0`** (it exits **255** on failure — branch on "exit 0", never on a specific status); the `OpenGL renderer string` is `llvmpipe` or `softpipe`; and **full** `glxinfo` (or `glxinfo -s`) reports `GL_ARB_framebuffer_object` or `GL_EXT_framebuffer_object`, or the `OpenGL version string` is ≥ 3.0 — that last layer is load-bearing, because the game's MonoGame backend throws `PlatformNotSupportedException` without FBO support and `glxinfo -B` prints no extension list. A probe failure **with all three tools present** is a graphics stack that is present but unusable → exit `3`. On cleanup, remove only the **socket** `/tmp/.X11-unix/X{n}` of the display this script allocated.
- [X] T015 [US1] Implement the build-and-verify phase in `scripts/smoke-validation.sh`. Remove the deploy target `<mods-path>/<ModFolderName>/` with `rm -rf -- "$target"` and **no trailing slash** (a trailing slash makes `rm` follow a symlinked final component and delete the target's *contents*), re-canonicalizing the parent immediately before the removal. Run `dotnet build --configuration Release --no-incremental` **in the project directory**, pinned with `-p:GameModsPath="<resolved mods path>"` so the deploy lands exactly where SMAPI loads from, and bounded by `--build-timeout` (default 300 s) — expiry exits `12` naming the elapsed time, and the build timeout is independent of `--timeout`. Then compute the SHA-256 digest of `<mod-project-dir>/bin/<EntryDll>` and of the deployed `<mods-path>/<ModFolderName>/<EntryDll>` and exit `12` **naming both digests** if they differ, if either is missing, or if the deployed file is **strictly** older than the build (MSBuild preserves the source mtime, so the timestamps are identical in the happy path). The build MUST be run from the resolved `--mod-project` directory, and the deploy target MUST be `<mods-path>/LivingRoots/`.
- [X] T016 [US1] Implement the log gate and the verdict in `scripts/smoke-validation.sh`. Normalise the SMAPI log by stripping the trailing CR (`sed 's/\r$//'`) into a working copy, then split it into a record stream of `<level>\t<source>\t<text>` where the level and source are **bound by label, never by a fixed column**, and continuation lines (lines with no record prefix) get **empty** level and source. The gate has three scoped legs, each evaluated only against its own section's records: (1) the mod is listed in the loaded-mods block (the header is `Loaded <N> mods:` or `Loaded 0 mods.`; the entries are the following records whose message begins with three spaces; the manifest name MUST be matched as a substring followed by a space or end-of-line); (2) the mod is **not** in the skipped-mods section (the header message is exactly `   Skipped mods` — three leading spaces, which live *inside* the message, after the `[ts LEVEL SOURCE] ` prefix; the entries begin with six spaces and MUST carry a ` because ` suffix to be distinguished from the identically-shaped mod-warning entries); and (3) no record whose level label is `ERROR` or `ALERT` **and** whose source label equals the mod's manifest name. The mod's own marker MUST be reported but MUST NOT gate. The manifest name MUST NOT be searched across the whole log. Success MUST print exactly `SUCCESS: Mod loaded without errors.` and exit `0`. A failure MUST produce the "What → Where → How to fix" report with the **matched failure record plus its continuation lines** (never a fixed `tail -N` window) and a trailing `Fix:` line.
- [X] T017 [US1] Implement the launch, the two-phase wait and the termination in `scripts/smoke-validation.sh`. Detect an existing Stardew Valley/SMAPI process by `/proc/$PID/exe` under the **canonicalized** game directory (bash's `$PWD` is logical while `/proc` is physical, and `~/.steam/steam` is a symlink); abort with `7`, or with `--force` terminate the **identified PID only** (never a group the script did not create) via SIGTERM → `--grace-period` → SIGKILL. Build with the deploy target pinned, then launch `./StardewModdingAPI --no-terminal` from the game directory with `set -m` enabled **around the launch only** (so the game's PGID equals its PID) and disabled again immediately after (leaving it on would move the script out of the terminal's foreground group, so a terminal SIGINT would reach the poll loop's `sleep` instead of the script and the `INT` handler would never run — measured under a pty). Export `SMAPI_NO_TERMINAL=true`, `SMAPI_DEVELOPER_MODE=true`, `LIBGL_ALWAYS_SOFTWARE=1`, `GALLIUM_DRIVER=llvmpipe`, `SDL_VIDEODRIVER=x11`, `SDL_AUDIODRIVER=dummy`, `ALSOFT_DRIVERS=null` (the game must be **muted** — Xvfb virtualizes the display, not the audio device) and `DISPLAY` from the reported display number. Then the **two-phase wait**, both phases inside the same `--timeout` deadline measured **from the instant the game was launched**: phase 1 waits for the run sentinel `Log started at <UTC> UTC` (a **TRACE**-level record — a parser reading only `INFO`+ would never find it); phase 2 waits for the mod-resolution header `Loaded <N> mods:` / `Loaded 0 mods.`. If the header appears, evaluate the gate (a missing mod → `5`); if it never appears, exit `4`; if the process exits with the sentinel seen but no header, exit `17`. On success, terminate the game's **process group** (SIGTERM → `--grace-period` → SIGKILL, with the PGID assertion) and exit `0`.

**Checkpoint**: User Story 1 is fully functional and testable independently. **All Phase 3 implementation tasks touch `scripts/smoke-validation.sh` and are therefore SEQUENTIAL.**

---

## Phase 4: User Story 2 - Mod data persistence verification (Priority: P2, DEFERRED)

**Goal**: Verify the mod's JSON sidecars exist under SMAPI's mod-data directory after a run with a save loaded.

**Independent Test**: Not applicable in this release.

**Status**: **DEFERRED by FR-004.** The automated script cannot load a save — SMAPI has no command-line save selection and the mod is purely reactive — so the title screen is the expected state, and the save check is skipped with a `SKIPPED:` warning that does not gate success. SC-004 and SC-005 state in their own text that they are **not evaluated** in this release. **This phase contains no implementation tasks in this release**; it is recorded so the deferral is explicit rather than an omission.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Gate wiring, documentation, and the final verification

- [X] T018 [P] Wire the test suite into `lefthook.yml` — add a `scripts/tests/**/*.sh` entry to the existing `bash -n` gate (it must keep parsing **every** file individually, never `bash -n {staged_files}`, which parses only the first) and keep the `shellcheck` step skipping gracefully when the binary is absent.
- [X] T019 [P] Update `specs/008-smoke-validation/quickstart.md` — fill in the **Actual output** and **Pass/Fail** columns of the TC table with the executed results, and record the execution date. Update the TC-18…TC-26 rows' expectations if the implementation revealed a discrepancy (and, if it did, correct the spec rather than weakening the test).
- [X] T020 [P] Update `.specify/memory/STATE.md` — record the implementation completion, the executed TC results, and the remaining Gate-3 status.
- [X] T021 **VERIFICATION** — run the launch-free suite: `scripts/tests/run-all.sh` excluding `scripts/tests/test-integration.sh`. It MUST exit `0` and every assertion MUST pass. Record the exact command and its output.
- [X] T022 **VERIFICATION** — run the integration suite: `scripts/tests/test-integration.sh` against the real game installation and the real mod. Record every TC's exit code and the actual output. **This is the Gate-3 evidence** — it MUST be executed, not inferred.
- [X] T023 **VERIFICATION** — audit `scripts/smoke-validation.sh` against `specs/008-smoke-validation/spec.md` **requirement by requirement**: for each of FR-001…FR-017, quote the clause and point at the implementing line(s) in the script; report any clause with no implementation, and any implemented behaviour with no clause. The two-phase wait, the three gate legs, the `Loaded 0 mods.` handling, the `17` classification, the `set -m` scoping, the `umask`/`chmod` modes, the quoting discipline, the `exec 9<>` lock, the `exec 9>` prohibition, the slash-free `rm -rf`, the marker removals, the mute, and the `set -Eeuo pipefail` line MUST each be confirmed present.
- [X] T024 **VERIFICATION** — verify the exit-code contract **exhaustively**: for every row of the FR-007 translation table, assert the script maps that raw status to the documented code; assert that no subprocess-specific status can escape as the script's own final code; assert that `8` is never emitted; assert that every documented code appears in `--help`; and assert the exit code of every TC in the quickstart table matches its expected output.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately. T001 must precede T009…T012 (they use the harness); T002 and T003 are independent of each other and of T001.
- **Foundational (Phase 2)**: Depends on Setup completion — **BLOCKS all user stories**. T004 is independent of T005…T008; T005…T008 are sequential (same file).
- **User Stories (Phase 3+)**: Depends on the Foundational phase.
- **Polish (Phase 5)**: Depends on User Story 1 being complete.
- **Convergence (Phases 6–9)**: Depends on the shipped implementation (Phase 5). T025–T059 (Phase 6) and T060–T090 (Phase 7) are convergence blocks whose tasks are ordered by severity within the phase (CRITICAL → HIGH → MEDIUM → LOW). **Phase 8 (T091–T134)** and **Phase 9 (T135–T183)** depend on the shipped implementation (Phase 5) as well, and no task in either block is marked `[P]`, so the tasks that edit `scripts/smoke-validation.sh` are **sequential** within their phase — they all touch the same file. Phase 9 additionally carries a `### TDD` section that is **blocking**: its test tasks (T135–T154) MUST all be written before the implementation tasks below them (T155–T173) are started.

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

### Deferred Phase and Convergence Phases (after the MVP)

5. **Phase 4 — User Story 2 (P2, DEFERRED)**: no implementation tasks in this release; the phase is recorded so the deferral is explicit rather than an omission. It is **not** scheduled — nothing in it may be started.
6. **Phase 6 (T025–T059) and Phase 7 (T060–T090)**: the first two convergence rounds — implemented and verified.
7. **Phase 8 (T091–T134)** — the second round's backlog: implement the 44 tasks in severity order — the HIGH band (`T091`–`T097`) is the first wave, then the MEDIUM band, then the LOW band. Every task that edits `scripts/smoke-validation.sh` is **sequential**; the document tasks touch disjoint files and may run in parallel with the code work.
8. **Phase 9 (T135–T183)** — the third round, **test-first**: the `### TDD` section (`T135`–`T154`) is written first and is **blocking** — no implementation task below it may start until every test in that section exists. Then the implementation tasks (`T155`–`T173`), then the documentation tasks (`T174`–`T181`), and finally the verification tasks (`T182`–`T183`), which run **last and alone**.

### Parallel Team Strategy

- Wave A: one agent on `scripts/tests/lib/harness.sh`
- Wave B: four agents, one per test file (disjoint)
- Wave C: one agent on `scripts/smoke-validation.sh` (sequential, single file — reuse the same agent for T013…T017 so the context is not lost)
- Wave D: four agents on the verification tasks (disjoint concerns)

**Convergence waves (Phases 8–9):**

- Wave E: one agent per test file — `test-logparse.sh`, `test-preflight.sh`, `test-cli.sh`, `test-integration.sh` and `run-all.sh` + `lib/harness.sh` (disjoint files; the last two share the harness's context and stay with one agent). The suite runs are **serialised** — the integration cases rebuild the repository tree and contend on `LivingRoots/obj/`.
- Wave F: one agent on `scripts/smoke-validation.sh` (sequential, single file — the Phase 8 code tasks, then the Phase 9 implementation tasks that follow the blocking `### TDD` section).
- Wave G: the document agents — `spec.md` / `plan.md` / `data-model.md` / `quickstart.md` / `research*.md`, `STATE.md` and `tasks.md` (disjoint files).
- Wave H: the verification tasks — last and alone (`T182`–`T183`), with the Phase 8 verification work folded in.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps a task to a specific user story for traceability
- Verify tests fail before implementing
- Commit after each task or logical group (the `atomic-commit-hook` is mandatory after every file edit)
- The script is the feature; `scripts/tests/` is validation infrastructure and is NOT part of the shipped artifact
- Avoid: vague tasks, same-file conflicts, cross-story dependencies that break independence

---

## Phase 6: Convergence

**Origin**: `/speckit.converge` (2026-10-20). Four parallel assessment agents audited the shipped
code against `spec.md`, `plan.md`, `tasks.md` and the constitution; the findings below are the
gaps that audit found. Everything is ordered CRITICAL → HIGH → MEDIUM → LOW, and the task IDs
continue from the highest existing ID (T024).

**Constitution scope note**: principles I (DDD), III (Async-Only) and VI (Multiplayer) do not
bind this feature — `scripts/smoke-validation.sh` is a standalone maintainer CLI that touches no
`Domain/`/`Services/`/`Controllers` layer, is not .NET async code, and validates single-player
mod loading. Principle II (Security-First) binds only through its path-sanitisation clause,
principle IV (TDD) through its first sentence, and principle V (YAGNI) binds. **No CRITICAL
constitution MUST violation was found.**

- [X] T025 Make an external SIGINT terminate the run with exit `130` — a non-interactive bash ignores an externally delivered SIGINT, so the `INT` trap never runs and the script survives the interrupt and later exits `4`; the run must die from the signal with `WIFSIGNALED` set and the caller observing `128+N` per FR-005 (contradicts)
- [X] T026 Make `--build-timeout` actually stop the build — `$!` is the PID of the log-pump loop, not of `dotnet build`, so the kill targets the wrong process and the build survives the script's exit and keeps writing into the shared build output and the deploy target per FR-016 (b) (contradicts)
- [X] T027 Implement the three log-freshness guards FR-002 mandates — compare the run sentinel's timestamp against the launch timestamp `T0` as UTC, record the log file's inode before the stale log is removed and verify it changed, and assert the stale log is absent immediately afterwards (a failed removal MUST abort the run); today a stale log can still decide the verdict and produce a false pass per FR-002 and SC-011 (missing)
- [X] T028 Fix the shell-lint gate so it actually runs — the block-form `glob:` in `lefthook.yml` is ignored by lefthook v2.1.10, so both `shell-syntax` and `shellcheck` report "no files for inspection" and never inspect anything; the gate must cover `scripts/*.sh` and `scripts/tests/**/*.sh` per plan Gate 1 (contradicts)
- [X] T029 Resolve a relative `--mods-path` against the game directory — it is currently kept relative, so it resolves against the process working directory and the run aborts with `16` even when `<game-path>/Mods` exists per FR-017 (missing)
- [X] T030 Apply the mandatory PGID assertion to the signal-forwarding group signals — `forward_signal` sends six group signals with no assertion, so an interruption could signal the script's own group per FR-005 (contradicts)
- [X] T031 Detect and report the mod's own success marker as a secondary, non-gating signal — it is never detected, so the mandated secondary signal is absent from the result output per FR-003, FR-006 and T016 (missing)
- [X] T032 Carry the mod's own Trace records into the failure context — the context stops at the first following prefixed record, so the exception type and HResult never reach the report, and the normalised record stream carries no source field per FR-003 and FR-007 (missing)
- [X] T033 Reconcile the phase order with FR-012 (0) and the plan's lifecycle — the flock, the existing-process check and the log baseline all run before the build-and-verify step, and the spec pins them after it per FR-012 and plan (contradicts)
- [X] T034 Consult `SMAPI-crash.txt` after the launch so a crash is reported as a failure — the file is deleted in the pre-flight and never inspected again, so a crash after the mod-resolution header is currently reported as a success per FR-006, the data-model's crash-override rule and the edge case (partial)
- [X] T035 Isolate the deploy-effectiveness digest from a concurrent rebuild — the built digest is read from the shared `<project>/bin/`, which another build can rewrite between the build and the comparison, so the run aborts `12` instead of applying the rule that governs it per FR-016 (d) and SC-012 (partial)
- [X] T036 Name both SHA-256 digests when either file is missing — the "no assembly" and "not deployed" branches carry only a path, while the mismatch and older branches carry both per FR-016 (d) and SC-012 (partial)
- [X] T037 Emit the trailing-records context when no failure record matched — the report currently carries only a one-line reason, where FR-007 requires the trailing records of the normalised stream capped at 30 per FR-007 (missing)
- [X] T038 Make the `Fix:` line the trailing line of the failure report — the `Context:` block is printed after it per FR-007 (partial)
- [X] T039 Record every phase duration and print the per-phase durations — the `launch` phase is superseded without being closed, `cleanup` is never timed, and the helper that renders the durations is never called per FR-014 and SC-001 (partial)
- [X] T040 Stamp the failure report with a timestamp, level and run ID and write the stdout summary into the run log — both bypass `log_event`, and the summary is only produced on the success path per FR-014 (partial)
- [X] T041 Wait for the children with `wait -f "$pid"` and give Xvfb the mandated stdin and streams — the Xvfb child is never waited on and its output is discarded instead of going to the run log per FR-005 and T008 (contradicts)
- [X] T042 Abort with `13` when the cleanup path's PGID assertion fails — it is logged and skipped, leaving the group unsignalled, where FR-010 and the plan require the run to abort per FR-010 and plan (partial)
- [X] T043 Record the victim's executable path and start time before the `--force` signal — only the PID is written, so the evidence needed to reconstruct which process was terminated is lost per FR-011 and the Scope's declared side effects (partial)
- [X] T044 Log the update marker's content when removing it — only its path is recorded, so information is silently discarded per FR-002 and FR-012 item 6 (partial)
- [X] T045 Enforce the literal allowlist on the script's own authored names and canonicalise plus prefix-test the mutating constructed paths — the deploy target and the markers get neither `realpath -m` nor the component-boundary test, and the literal allowlist helper is never called per FR-012 item 7 and the plan's path-validation decision (partial)
- [X] T046 Verify every tool the script depends on in the pre-flight sweep — `ps`, `readlink`, `sed`, `awk`, `cut`, `head`, `tr` and `env` are used but not verified, so FR-011's instance detection silently never matches without `readlink` and the PGID assertion degrades to a no-op without `ps` per FR-012 item 8 and FR-012's preamble (partial)
- [X] T047 Extend the troubleshooting ladder to every published failure code — only 9 of the 17 failure codes have a step, and the most likely class, `5`, has none per SC-006 (partial)
- [X] T048 Make the quickstart's option table a verbatim copy of the spec's normative table — it drops the "Introduced by" column, paraphrases the behaviour column and renders `--mod-project`'s default differently, so the two copies are not diffable per SC-006 (partial)
- [X] T049 Update `plan.md` to match the shipped implementation — Gate 1's and Gate 3's statuses are stale, the Project Structure omits the test harness, the lifecycle order and the launch invocation predate the `--mods-path` flag, and the "TDD: Not applicable" line contradicts the shipped harness per plan (contradicts)
- [X] T050 Update `data-model.md` to match the shipped implementation — five runtime-state fields have no counterpart, the state machine's phase order does not match the code, and validation rules 3 and 5 are stale per data-model (contradicts)
- [X] T051 Correct `research.md`'s exit-code list and `research-bash-security.md`'s supersession banner — the list omits code `17` while claiming to be complete, and the banner's path-allowlist row is missing its leading pipe so it does not render as a table row per research (contradicts)
- [X] T052 Correct the quickstart's TC-19, the coverage table's attribution of exit `17`, and the TC-1 and TC-26 expected outputs — TC-19 expects a run log a usage error never creates, the coverage table credits `17` to two cases that produce `12` and `14`, and TC-1 and TC-26 expect strings the code does not emit per quickstart (contradicts)
- [X] T053 Record the executed results in the quickstart's test-case table — all 26 rows still carry their `_(record)_` and `☐ Pass ☐ Fail` placeholders, so no result and no execution date is recorded per T019 and SC-006 (missing)
- [X] T054 Correct `tasks.md`'s recorded verification claims and T011's wording — the note claims 507/507 assertions and that all 24 tasks are done, and T011 still says a missing `Loaded` header yields `13` where the code and the property table yield `17` per T011, T021 and T024 (contradicts)
- [X] T055 Produce exit `129` for SIGHUP end to end — the code path works only when the signal lands in the readiness poll, so the suite observed `4` per FR-007 (partial)
- [X] T056 Complete the host-tool sweep details — add the Xvfb version probe, check the "bash 4+" requirement, accept `pidof` as the spec allows instead of hard-requiring `pgrep`, and name the providing package per tool instead of a generic list per FR-012 item 8 and the edge case (partial)
- [X] T057 Log the signal and the grace period on the game termination path and print a summary on the failure path — the game path logs neither, and a failing run prints no stdout summary per FR-014 (partial)
- [X] T058 Update the script's header banner and re-canonicalise the deploy target's parent before the removal — the banner still declares the file a skeleton, and the parent was canonicalised about 30 seconds earlier per T002 and T015 (contradicts)
- [X] T059 Remove the dead helpers and the unrequested harness switches, and derive the mod folder name and UniqueID from the deployed manifest — six helpers ship uncalled, the harness carries two switches and the runner two flags no requirement asks for, and the folder name and UniqueID are hardcoded rather than derived per Constitution V and the constitution's bounds clause (unrequested)

---

## Phase 7: Convergence

> **Convergence (2026-10-21).** A fresh assessment of the **present state** of the code against `spec.md`, `plan.md`, `data-model.md` and the constitution — not a diff against any branch. Four read-only assessment passes covered FR-001…FR-017, SC-001…SC-015, both user stories, the edge cases, the declared side effects, the 11 plan decisions, the 20 data-model rules, all 59 existing tasks and the constitution's six principles and three gates; **115 intent items** were checked. **31 findings** remain: 2 `missing`, 26 `partial`, 2 `contradicts`, 3 `unrequested` — 0 CRITICAL, 3 HIGH, 11 MEDIUM, 17 LOW.
>
> Two claims were **refuted by experiment** and are not tasks: the `lefthook` `glob:` does select the test-suite files (a deliberately broken `scripts/tests/*.sh` made the hook exit `1`), and the `bash -n`/`shellcheck` steps are therefore live — only the plan's text about them is stale (T073).

### HIGH

- [X] T060 Validate the `--mod-project` path the way FR-016 (a) and FR-012 item 7 require — the path is currently used raw, so a project outside the repository is neither canonicalised nor prefix-tested, and it reaches `dotnet build` as a bare positional argument instead of after `--` (missing)
- [X] T061 Complete the third log-freshness guard of FR-002 — the stale log's inode is recorded before its removal but never compared, so the "verify it changed once the log is recreated" half is missing and SMAPI's `player-N` fallback can still leave a stale log deciding the verdict (partial)
- [X] T062 Assert the process-group invariant `child_pgid` relies on — the helper accepts whatever group `ps` reports, and a retained run log shows the Xvfb landing in the script's own group, which the cleanup then refuses to signal, ending the run with `13` after the success verdict was printed (partial)

### MEDIUM

- [X] T063 Label the deferred save-data check as `SKIPPED:` — FR-004 and the operational-readiness checklist require the skipped check to be labelled and counted separately from passes, and the label appears nowhere in the script (missing)
- [X] T064 Reconcile the lock-descriptor inheritance clause with the shipped implementation — FR-002 and FR-005 state that the children inherit the descriptor, but every launch closes it with `9<&-` because MSBuild's node reuse otherwise held the lock across runs, so the spec's rationale and the code must be brought into agreement (contradicts)
- [X] T065 Print each phase's duration in the stdout summary — FR-014 requires the summary to carry the total wall time **and** each phase's duration, and the durations currently reach the run log only (partial)
- [X] T066 Write the cleanup's failure report into the run log — FR-014 requires the run log to contain every line the script emits, and the report the EXIT trap prints for a failed cleanup reaches stderr only (partial)
- [X] T067 Log every signal with the PID and the signal name, the grace period that was applied and each SIGKILL escalation — FR-014 requires all of them, and the game and build paths log none of the signal names (partial)
- [X] T068 Apply the relative `--mods-path` rule only to a user-supplied value — the rule currently also rewrites the derived default, so a relative `--game-path` yields a doubled path such as `././Mods` (partial)
- [X] T069 Extend the host-tool sweep to every external the script invokes — `cp`, `cat`, `sort`, `tail`, `touch`, `mkdir`, `chmod`, `rm`, `dirname` and `basename` are used but unverified, so a missing one silently misclassifies the verdict (e.g. no `sort` reports a healthy installation as `2`) (partial)
- [X] T070 Emit the crash-file context from the normalised record stream — FR-007 forbids a fixed `tail -N` window and requires the context to come from the normalised stream, but the crash path still uses a raw `tail -n 30` over the CRLF file, and the data-model's rule 3 mandates that same window (partial)
- [X] T071 Decide the failure verdict only after the cleanup has run — the verdict is printed before the EXIT trap's cleanup, so a cleanup failure changes the exit code after `SUCCESS:` was already on stdout (partial)
- [X] T072 Update `data-model.md` to describe the shipped implementation — the document still asserts that `print_phase_timings`, `own_literal_ok` and `canonicalize_maybe_missing` are never called, that the launch phase is superseded without being closed and that `xvfb_pid` is never waited on, all of which the code no longer does (partial)
- [X] T073 Correct `plan.md`'s gate rows to the measured reality — Gate 1 still claims the hook's file selection is unproven and blames lefthook v2.1.10 (the installed version is 2.2.2 and the gate demonstrably inspects and fails on a broken test file), and Gate 3 still claims no run-log artifact exists although five retained logs record the post-reconciliation lifecycle (partial)

### LOW

- [X] T074 Include the matched failure record's own message in the reported context — FR-007 requires the matched record together with its continuation lines, and the context currently starts after it (partial)
- [X] T075 Stamp every line of the failure report written to the run log — FR-014 requires the run ID on every run-log line, and only the report's first line carries it (partial)
- [X] T076 Derive the built assembly's path from the manifest's `EntryDll` — FR-016 (d) specifies `<mod-project-dir>/bin/<EntryDll>`, while the code uses the project file's name, which coincides today but is not the specified rule (partial)
- [X] T077 Render the `--help` examples with the `--opt=VALUE` form — the normative option-syntax rules name that form as the one to use in examples and in the `--help` text (partial)
- [X] T078 Pass the path arguments of the `awk`, `head` and `sed` reads of the run's temporary files after `--` — T005 and FR-012 item 7 require every path argument to be passed after the terminator (partial)
- [X] T079 Report a containment failure from `assert_mutating_path_under_base` without calling `die` inside a command substitution — the script's own contract forbids it, and the ERR trap then emits a second, contradictory report for the same condition (partial)
- [X] T080 Attempt the manifest read and branch on its result — FR-012 item 7 forbids an existence test deciding whether the read is attempted, and `read_manifest_field` still gates its `sed` on `[[ -f ]]` (partial)
- [X] T081 Make the orphan-Xvfb detection work when only `pidof` is available — the tool sweep accepts either tool, but the scan uses `pgrep` alone, so on a `pidof`-only host the detection silently reports nothing (partial)
- [X] T082 Isolate the cleanup's two `terminate_group` calls with `|| true` — FR-010 requires every cleanup step to be isolated so one failure cannot prevent the others, and these two are the only unguarded ones (partial)
- [X] T083 Record exit code `129` as covered by TC-17 in the quickstart's requirement-coverage table — the table still records it as **Waived** although the suite now asserts a genuine SIGHUP death mapping to `129` (partial)
- [X] T084 Reword the Phase-2 checkpoint so it no longer contradicts T004's `[P]` marker — the checkpoint says every Phase 2 task touches the script and is therefore sequential, while T004 is marked parallel (contradicts)
- [X] T085 List the transient probe files and the directory creations in the declared side effects — the list is declared exhaustive and none of the four mutations appears in it, so either they are listed or the throw-away probes are removed (unrequested)
- [X] T086 Remove `env` from the required host-tool set — the script never invokes it, so the sweep fails closed with `9` on a host that has every tool the script actually needs (unrequested)
- [X] T087 Remove the `SKIP_INTEGRATION` alias from the test harness — only `SMOKE_SKIP_INTEGRATION` is named by any requirement, and the runner's documented environment lists only that one (unrequested)
- [X] T088 Remove the three write-only variables — `lock_acquired` in the script and `LAST_FIXTURE` and `REAL_MOD_PROJECT` in the test suite are assigned and never read, which the constitution's simplicity principle forbids maintaining (unrequested)
- [X] T089 Use or remove `internal_real` — the canonicalised `smapi-internal` path is assigned in the crash-marker phase and **is** read: the marker paths are built from the canonicalised form, so the variable is used and the finding no longer holds against the shipped code (unrequested)
- [X] T090 Make the wall-clock performance budget test load-tolerant — the constitution's Gate 1 requires `dotnet test` to succeed, and `GrowthPerformanceTests.LinearScan_40000Tiles_CompletesWithinBudget` asserts a 5 ms budget that a loaded host exceeds, so the suite is not reliably green (partial)

---

## Phase 8: Convergence

> **Convergence (2026-10-21, round 2).** A second read-only assessment of the **present state** of the code — after Phase 7 was implemented — against the same intent sources. Four parallel passes covered FR-002…FR-012 with the edge cases and the declared side effects; FR-001/004/006/008/009/011/013…FR-017 with SC-001…SC-015 and the user stories; the seven feature documents' fidelity to the code; and the constitution plus the test suite as an artifact. **The test suite was assessed for the first time as a deliverable** — which is why most of the new findings are uncovered MUST sub-clauses rather than broken code. Every FR, every SC, the edge cases, the declared side effects, the plan decisions, the data-model rules, all 90 tasks, the six constitution principles and the three gates were checked — over 120 intent checks in total. Duplicates two passes reported independently (the crash-context rule, the inode rows, the `SKIPPED:` label) are merged into one task each; the **process-group publication is not merged** — it stays split across T091 and T103, which now carry one clause each (T091: the group is published only after the `die` that can preempt it; T103: the build's group is phase-local). **Findings: 0 CRITICAL · 7 HIGH · 22 MEDIUM · 15 LOW** (44 in total) — no constitution MUST is violated and the P1 user story's baseline is intact.
>
> **Status: OPEN — not implemented.** Phase 8 is the second convergence round's backlog; `T091`–`T134` are the work `/speckit.implement` must complete. The seven HIGH tasks (`T091`–`T097`) are the first wave. Phases 1–7 remain implemented and verified (784 assertions green; the constitution's three gates green; a real-game run exiting `0` with `SUCCESS: Mod loaded without errors.`).
>
> **Two claims were refuted by experiment and are not tasks:** the `lefthook` `glob:` DOES select the test-suite files (under gobwas the greedy `scripts/*.sh` pattern already covers them, and a deliberately broken `scripts/tests/*.sh` fails the hook), and the `bash -n` gate parses every staged file individually.
>
> **Severity tags (T179).** Every task in this block carries a `[HIGH]` / `[MEDIUM]` / `[LOW]` tag, so the tally above is checkable by counting the tags: **0 CRITICAL · 7 HIGH · 22 MEDIUM · 15 LOW** = 44. The tags were derived from each task's own description and its gap type: `CRITICAL` — a constitution MUST violation, or a `missing`/`contradicts` gap that blocks the P1 user story's baseline functionality (none in this block); `HIGH` — the seven tasks the note names (`T091`–`T097`: the process-group publication, the trace-context regression guard, the timeout anchor, and the four document misstatements); `MEDIUM` — a gap in the shipped behaviour of a core FR/SC (a report payload, a CLI surface, a diagnostic, a path-safety rule), a missing regression assertion for one, or a document passage that misstates the shipped implementation in a way a reader would act on; `LOW` — a minor document correction, a polish-level logging addition or a low-risk suite hardening, with no behavioural consequence. The tags record severity only: the IDs stay in ascending order, and the first wave remains `T091`–`T097`.

- [X] T091 [HIGH] Record each child's process group before any `die` on that path, so a child is never orphaned by the very guard that exists to prevent it — the Xvfb's group is published only after the display-number wait, so the exit-10 path cannot signal it, and the process-group assertion fires before the group is published per FR-010 (partial)
- [X] T092 [HIGH] Add a log fixture that carries a mod-scoped TRACE record after a mod-scoped ERROR record and assert the failure report's Context section contains it, plus a fixture with a mod-scoped TRACE record and no ERROR that must still exit 0 — the implementation adds the mod's Trace records to the context so they carry the exception type and HResult, but no assertion would fail if that regressed per Constitution IV, FR-003, FR-007 (missing)
- [X] T093 [HIGH] Assert in TC-6 that the game was actually launched before the timeout fired, and add a case whose `--timeout` is shorter than the rebuild, asserting exit 4 and a launched game — the timeout must be measured from the launch instant, and an implementation that anchored the deadline at the script's start would still pass every case today per FR-009, SC-001 (partial)
- [X] T094 [HIGH] Correct `data-model.md`, which states that the stale log's inode is written to the run log but never consulted and that the freshness control is the sentinel rather than the inode — the shipped code records the baseline inode, re-stats the recreated log and aborts with 13 when the inode is unchanged, so the inode IS a consulted variable and a third, independent freshness control per data-model: validation rules 2, the log baseline, the process state (contradicts)
- [X] T095 [HIGH] Correct `data-model.md`'s crash-context rule, which still says the shipped script derives the crash context from a raw `tail -n 30` over the CRLF file — the shipped path normalises the crash file and derives the context from the normalised record stream, with the trailing-30 fallback used only when no failure record matched per data-model: validation rule 3 (contradicts)
- [X] T096 [HIGH] Correct `data-model.md`, which states that the per-phase durations are not printed on stdout and that the cleanup's duration is absent from the rendered block — the stdout summary now prints the phase durations and the cleanup runs before the summary on both paths, so the cleanup's duration is included per data-model: phase-timing bookkeeping (contradicts)
- [X] T097 [HIGH] Correct the quickstart's TC-1 expected stdout order, which places the per-phase durations before the total wall time — the shipped summary prints `total wall time` first and the `phase durations:` block after it per quickstart: TC-1 expected output (contradicts)
- [X] T098 [MEDIUM] Pass the gate's context to the failure report for the graphics, no-sentinel and no-mod-resolution verdicts, which currently discard it — when no record matched, the report must carry the trailing records of the normalised stream, capped at 30 per FR-007 (partial)
- [X] T099 [MEDIUM] Derive the crash-file fallback from the trailing RECORDS of the normalised crash stream instead of a fixed `tail -n 30` over the raw file — a .NET stack trace is routinely 20–40 lines long, so a physical-line window can consist entirely of stack frames while omitting the record that names the failure per FR-007 (partial)
- [X] T100 [MEDIUM] Attempt the crash-file and marker removals unconditionally and branch on the removal's result, instead of gating them on an existence test and discarding the outcome — a marker that could not be deleted must be reported, not silently skipped into a misleading verdict per FR-012 item 7 (partial)
- [X] T101 [MEDIUM] Let the command line be parsed before the host-tool sweep, or exempt `--help`, `--version` and the usage-error path from it — the sweep currently preempts them, so on a host missing one tool the usage reference and the exit-14 usage error are unreachable per FR-012 item 8, FR-015 (partial)
- [X] T102 [MEDIUM] Dump the build output into the run log before the build-timeout report, so the promised build output survives the timeout — the report names the run log as the place to find it, but the cleanup deletes the file first per FR-014, FR-016 (b) (partial)
- [X] T103 [MEDIUM] Store the rebuild's process group in a script-global and signal it from the cleanup and the signal forwarder, so an interrupted build cannot outlive the run — the group is currently phase-local to `phase_build`, so an interrupted build keeps writing into the shared build output and the deploy target per FR-016 (b), FR-010 (partial)
- [X] T104 [MEDIUM] Add log fixture rows for a mod-scoped ALERT record (must fail with 5) and a mod-scoped WARN record (must not fail), so the failure-marker rule's level matrix is asserted rather than only implemented per FR-003 (missing)
- [X] T105 [MEDIUM] Add a log fixture carrying the mod's own success marker after the sentinel and assert the run exits 0 AND that stdout carries a `mod-success-marker:` line — only the negative case (a marker before the sentinel) is asserted today per FR-006 (missing)
- [X] T106 [MEDIUM] Assert in the `mod_absent` row that the report carries a Context section holding the trailing records of the fixture's normalised stream and no more than 30 of them — the implementation emits them, but no assertion would fail if it stopped per FR-007 (missing)
- [X] T107 [MEDIUM] Assert after a fixture run that the allocated display's socket is gone and that the run's temporary directory does not survive, so the cleanup's socket and temp-dir removals are observed rather than merely implemented per FR-010, SC-010 (missing)
- [X] T108 [MEDIUM] Assert after a fixture run that the lock file exists with mode 0600, that its directory has mode 0700, and that the lock file survives the run — the lock is deliberately left in place and no test observes either property per FR-002 (missing)
- [X] T109 [MEDIUM] Replace the quickstart's blanket "Covered" coverage row with a per-clause status naming the MUST sub-clauses no assertion covers, and change FR-013's row to reflect that the option-surface property guards it indirectly — the table currently overstates the coverage and SC-006's quickstart contract has no assertion at all per SC-006, quickstart: requirement-coverage table (contradicts)
- [X] T110 [MEDIUM] Correct the plan's Summary lifecycle, which places the host-tool sweep inside the pre-flight after the run-log creation, and add the `--mod-project` resolution to the pre-flight enumeration — the shipped script sweeps first, before the command line is parsed per plan: Summary lifecycle, the pre-flight enumeration (contradicts)
- [X] T111 [LOW] Replace the plan's enumerated host-tool set with the shipped one — the plan still repeats the pre-T069 13-tool set while the sweep now covers the 29 tools in `REQUIRED_TOOLS` plus the `pgrep`-or-`pidof` alternative check per plan: Technical Context (partial)
- [X] T112 [LOW] State in the plan that the rebuild is a third group-owning child launched under `set -m` and that the PGID assertion covers it, since the plan still describes only the game and the X server per plan: Technical Context, design decisions (partial)
- [X] T113 [LOW] Record in the plan's path-validation decision that `--mod-project` is the one user-supplied root prefix-tested against a foreign base — the repository root — because the plan states that every user-supplied root is its own base per plan: path-validation decision (partial)
- [X] T114 [LOW] Mark the plan's storage enumeration as a summary of the exhaustive list in `spec.md`, or extend it — the plan presents a shorter set as exhaustive per plan: Technical Context storage (partial)
- [X] T115 [LOW] Extend the plan's TDD row to TC-1…TC-33 and name the seven convergence cases, which the plan does not mention per plan: Constitution Check TDD (partial)
- [X] T116 [MEDIUM] Correct `data-model.md`: replace the exit-9 row's tool list with the shipped set, state that `built` is `<project-dir>/bin/<EntryDll from the manifest>`, add the `built_snapshot` field that the digest comparison and the strictly-older test actually consume, and correct the state-transition diagram and the FR-012 (0) mapping for the sweep's position and the two new pre-flight steps per data-model: Exit Codes row 9, the built path, the process state (partial)
- [X] T117 [MEDIUM] Correct the quickstart: the Scenario 1 text must say the phase timings go to stdout as well as the run log, the Prerequisites and D-16 tool lists must match the shipped set, TC-5 must say the flock is taken after the rebuild rather than "exits immediately", the Test Oracle must record the current 784 assertions, and the coverage table must list TC-27…TC-33 per quickstart: Scenario 1, Prerequisites, D-16, TC-5, the Test Oracle, the coverage table (contradicts)
- [X] T118 [MEDIUM] Stamp the EXIT trap's cleanup-failure report line by line and emit it for a non-zero exit code too, or record in T075 that it is the one remaining exception — three of its four run-log lines carry no stamp, and an interrupted run whose cleanup assertion fails exits 13 with no stderr diagnostic at all per tasks.md: Phase 7 T075 (partial)
- [X] T119 [LOW] Record the six contradictions between the reviewer-owned checklists and the shipped implementation for the reviewer — the checklists still say the script does not exist, that Xvfb is absent, that the tool set is consistent across the four artifacts and that the pre-flight owns the display and the tool sweep; the six contradictions are enumerated in the **"Reviewer note (T119)"** section at the end of this phase, and the checklists themselves are reviewer-owned and were not edited per checklists: the six recorded contradictions (partial)
- [X] T120 [MEDIUM] Canonicalise the `--smapi-log` root and prefix-test the constructed log-directory paths against it, exactly as the deploy target and the SMAPI markers are tested — the log directory, the crash-file removal, the stale-log removal and the write probe are built from the un-canonicalised string per FR-012 item 7 (partial)
- [X] T121 [LOW] Log the signal the script forwards to each child group before sending it, so an interrupted run's log records the forwarded signal and not only the cleanup's SIGTERM to the groups per FR-005, FR-014 (partial)
- [X] T122 [MEDIUM] Evaluate the inode freshness guard independently of the sentinel wait, so the stale-log case FR-002 designs it for is reported as the stale-log internal error instead of burning the whole timeout and reporting a timeout per FR-002 (partial)
- [X] T123 [MEDIUM] Pass the canonicalised game path to `-p:GamePath=` — the build receives the raw `--game-path` value while its sibling `-p:GameModsPath=` receives the canonicalised one per FR-016 (c) (partial)
- [X] T124 [MEDIUM] Emit the `SKIPPED:` save-check line before the verdict is decided, so a failing run's log also records that the save check was skipped — today only the success path carries the label per FR-004 (partial)
- [X] T125 [LOW] Name the target PID in the cleanup's group signals as well as the group — FR-014 requires every signal to be logged with the PID and the signal per FR-014 (partial)
- [X] T126 [MEDIUM] Validate the manifest-derived `EntryDll` paths — the built and deployed assembly paths embed a name read out of the project's manifest and are neither canonicalised nor prefix-tested, unlike every other constructed path per FR-012 item 7, FR-016 (d) (partial)
- [X] T127 [MEDIUM] Add a fixture-driven case that asserts the run log's mode, its directory modes, the run-id form, the version, the per-line stamping and the stdout/stderr split, and a fixture-driven idempotency case that runs the script twice against the same fixture — today those properties are only observed on a real-installation run and are skipped elsewhere per Constitution IV, FR-001, FR-014, SC-014, SC-015 (partial)
- [X] T128 [LOW] Give the CLI and pre-flight suites a hard deadline, so a hung script under test cannot outlive the suite or leak a child (and an X server) — neither suite uses a bounded invocation today per suite (partial)
- [X] T129 [LOW] Re-count the retained run logs and correct the recorded Xvfb version in the plan — the plan names five logs where seven exist and records 21.1.24 where 21.1.25 is installed per plan: Constitution Check Gate 3 (partial)
- [X] T130 [LOW] Correct `data-model.md`: rename the containment helper to the shipped `check_mutating_path_under_base`, extend the safety rules with the build-group assertion, the child-group-ownership assertion and the `--mod-project` containment, and add `die` and `main` to the read sites of the cleanup-assertion flag and `check_crash_file` to the read sites of the temp directory per data-model: validation rule 12, the safety rules, the process state (partial)
- [X] T131 [LOW] Correct the quickstart's remaining stale text: TC-12's expected output must match the report the script actually produces, the scenario sample reports must show the shipped stamped shape on stderr, the recorded Xvfb version must be 21.1.25, and the log-baseline item must not claim the launch timestamp is recorded there per quickstart: TC-12, the scenario samples, the Prerequisites, the log-baseline item (partial)
- [X] T132 [LOW] Correct `research.md`: note that the cleanup now runs on the failure path and before the verdict as well as from the EXIT trap, and state that the level is a variable-width label bound by label rather than a five-character field per research.md: signal handling and log parsing (partial)
- [X] T133 [LOW] Correct `tasks.md`: reword the earlier progress paragraph so it does not read as the current state, extend the Implementation Strategy with the deferred phase and the convergence phases (Phase 6, Phase 7, Phase 8 and Phase 9), and annotate T013's tool list as the initial set later extended by the tool-sweep task per tasks.md: the progress note, the Implementation Strategy, T013 (partial)
- [X] T134 [LOW] Correct the session state's path-property matrix count — the pre-flight suite has seven rows, not six per STATE.md: the test suite table (partial)

### Reviewer note (T119) — the six contradictions with the reviewer-owned checklists

The checklists under `specs/008-smoke-validation/checklists/` are **reviewer-owned artifacts** and were **not** edited. The six contradictions between them and the shipped implementation are recorded here so the reviewer can act on them:

1. **`operational-readiness.md` → *6. Maintainability* → 6.1** — "The script does not exist yet, so neither gate has anything to lint" — the script exists (3150 lines) and `lefthook.yml` lints it: the `bash -n` and `shellcheck` steps both match `scripts/*.sh`, which covers `scripts/smoke-validation.sh`.
2. **`operational-readiness.md` → *6. Maintainability* → 6.17** — "the file itself does not exist yet" — same: the file exists, so the CRLF check has a file to inspect.
3. **`security-safety.md` → *10. Script Execution Safety* → 10.5** — "the script does not exist yet **and the host lacks `Xvfb`**, so the pre-flight fails closed with `9`" — the script exists and `Xvfb` **is** installed on this host (`xorg-x11-server-Xvfb` 21.1.25), so the pre-flight no longer fails closed with `9` for that reason.
4. **`requirements.md` → *Requirement Completeness* → "Dependencies and assumptions identified"** — "the host-tool set is consistent across all four artifacts: FR-012 item 8 enumerates … and `plan.md`, `quickstart.md` and `data-model.md` … repeat exactly that set" — the shipped sweep is a **29-entry superset** of that 14-item list, which the four artifacts still repeat; the same item also records the stale host state ("`Xvfb` and `shellcheck` are absent").
5. **`operational-readiness.md` → *1. Pre-Flight Validation* → 1.1 and 1.5** — FR-012 item 0 "pins its order (tool sweep → game/SMAPI paths → Steam prerequisite → marker removal → **display start + graphics probe**)", i.e. the pre-flight owns both the display and the tool sweep — the tool sweep actually runs in `main()` **before** `parse_args` (it must, so a missing tool is reported as `9` and not as the shell's own `127`), and the display is a **separate** `phase_display` that runs **after** `phase_preflight`.
6. **`script-reliability.md` → *2. Process Lifecycle Management* → 2.7** — "the **pre-flight** starts it [`Xvfb`] with `-displayfd`" — it is `phase_display` that starts it, and `phase_display` runs after `phase_preflight`.

---

## Phase 9: Convergence

> **Convergence (2026-10-21, round 3).** A third read-only assessment of the **present state** of the code — after Phases 1–7 were implemented — against `spec.md`, `plan.md`, `data-model.md`, `quickstart.md`, `research.md`, `research-bash-security.md`, `tasks.md`, `STATE.md`, the five reviewer-owned checklists and the constitution. Five parallel assessment passes covered the CLI parsing and the ERR trap; the build, display, run-log, stamper and `--force` machinery; the log gate, the verdict ladder and the report; the seven feature documents and the fidelity of the task list; and the test suite as a deliverable together with the constitution's six principles and three gates. **Every FR, every SC, both user stories, the edge cases, the declared side effects, the 11 plan decisions, the 20 data-model codes and rules, all 134 existing tasks and the constitution were checked.** **Findings: 0 CRITICAL · 3 HIGH · 16 MEDIUM · 16 LOW** (35 in total) — 5 `missing`, 26 `partial`, 4 `contradicts`, 0 `unrequested`; no constitution MUST is violated and the P1 user story's baseline is intact.
>
> **Nothing here duplicates Phase 8.** All 44 tasks of Phase 8 (T091–T134) were re-read first and re-verified as still open; the findings below are the ones Phase 8 does **not** cover. **26** of the Phase 8 items were re-verified as still open by direct inspection of the present code, and **41 of the 44** Phase 8 task descriptions were re-verified as accurate (the three defective ones are T091/T103, T111 and T133 — corrected by T177, T178 and T180).
>
> **TDD discipline (this phase is test-first).** The `### TDD` section below holds **every** test that must be written before any implementation in this phase, and each implementation task is blocked until it is complete. The tests are grouped by the file they touch so that the waves can run in parallel without two agents editing the same file: `test-logparse.sh`, `test-preflight.sh`, `test-cli.sh`, `test-integration.sh`, and `run-all.sh` + `lib/harness.sh` (these two share the harness's context and stay with one agent). **The suite runs are serialised** — the integration cases rebuild the repository tree and contend on `LivingRoots/obj/`, and at most one suite that rebuilds may run at a time. Use the **default `TMPDIR`**; a `TMPDIR` inside `$HOME` makes SMAPI `~`-anonymise the mods path and fails TC-14 on a correct run. A full `/tmp` (a 2 GB tmpfs other sessions fill) makes the X server fail with exit `10` and the build hang — that is an environment symptom, never a verdict.
>
> **Two claims were refuted and are not tasks:** the lock's containing directory is **not** a gap — FR-002 states that when the directory is `$XDG_RUNTIME_DIR` the script **MUST NOT** change its permissions (the XDG specification already guarantees mode `0700` and sole ownership) and the `umask 077` creation already yields `0700`, while the fallback branch's `chmod 0700` is the behaviour FR-002 mandates; and the spec's `### Declared side effects` list and `### Command-Line Options (normative)` table were both verified **exhaustive and accurate** against the code.
>
> **Two premises were refined by measurement during implementation.** T154's description says the old word-splitting "dropped" a pre-existing trap's argument — it did **not**: `trap -p` single-quotes any action containing a space or metacharacter, so the whole action lands in the first word. Nine action shapes (quoted and unquoted arguments, two arguments, `;`, `&&`, command substitution, `exit`, an embedded space, no arguments) were measured against the old and the new harness with **identical output in every case**. The real gains are the mechanism (no word-splitting at all) and that the replayed action now observes the shell's exit status instead of `0`; the task is satisfied, but not for the reason it stated. Separately, T144's property test — which derives the tool set from the script's own source rather than from a hand-listed set — found that the sweep is missing **both `sleep` and `mkfifo`**, where the original finding named only `sleep`.

### TDD — test tasks (BLOCKING: no implementation task below may start until this section is complete)

#### HIGH

- [X] T135 Add a log-gate fixture whose graphics-device failure spans continuation lines — SMAPI 4.5.2 emits ONE ERROR record whose message ends at `Technical details:` and whose `Failed to create graphics device!` text lands in the continuation lines — and assert the run exits `3` and not `17`, so a record's message is matched together with its continuation lines per FR-007, the graphics-device edge case, data-model: validation rule 9 (missing)
- [X] T136 Add a fixture project whose `.csproj` sets `<ModFolderName>` to a name that differs from the project file's base name, and assert the script removes, digest-compares and reports the deploy effectiveness against THAT folder, so the folder ModBuildConfig actually deploys into is the one the assertion covers per FR-016 (b) (d), SC-012 (missing)
- [X] T137 Assert the remediation package for every row of the host-tool sweep table — `Xvfb` → `xorg-x11-server-Xvfb`, `glxinfo`/`xdpyinfo` → `mesa-utils`, `flock` → `util-linux` — and add the package clause to TC-8, so a wrong or `unknown-package` entry in `TOOL_PACKAGE` can no longer ship unnoticed per FR-012 item 8, quickstart: TC-8 (missing)

#### MEDIUM

- [X] T138 Add a case in which the sentinel's timestamp cannot be parsed from the log — the grep matches nothing and the pipeline's exit status is 1 under `set -o pipefail` — and assert that exactly ONE failure report reaches stderr and that the run's exit code is that report's own, so the ERR trap cannot fire a second, contradictory report from inside the command substitution per FR-007, FR-014, the unexpected-error edge case (missing)
- [X] T139 Assert that the phase in which the run failed appears in the stdout `phase durations:` block and in the run log with its end timestamp and elapsed seconds — the readiness-wait timeout and the verdict phase are the cases that matter most per FR-014, SC-001 (missing)
- [X] T140 Add a case in which the deploy target cannot be removed — a foreign-owned subtree or a mount point under `<mods-path>/<ModFolderName>/` — and assert the run aborts with exit `12` naming the target and the removal's error, so a stale file from a previous build can no longer survive a run that reports success per FR-016 (b), FR-012 item 7 (missing)
- [X] T141 Assert that the run log carries the launch epoch the sentinel-freshness comparison actually used — the value taken immediately before the game is launched — and that the header field labelled `launch timestamp` is not the run-start instant, so a failed run can be reconstructed from the run log alone per FR-002, FR-014, SC-014 (missing)
- [X] T142 Assert that the mod's own success marker, written on `GameLaunched` after the title screen, is detected and reported on stdout as `mod-success-marker:` when it appears within the window between the mod-resolution header and the termination, so the mandated secondary signal is reachable in a real run per FR-003, FR-006, SC-002 (missing)
- [X] T143 Add a case that withholds BOTH `pgrep` and `pidof` from the tool sweep and asserts the run fails closed with exit `9` naming the missing capability, since the spec names `pgrep (or pidof)` explicitly and only the positive half is asserted today per FR-012 item 8 (missing)
- [X] T144 Extend the host-tool sweep table so it covers every external the script actually invokes — `sleep` is called in ten places and is in neither `REQUIRED_TOOLS` nor the table — and so the `pgrep`-or-`pidof` alternative check is asserted as the rule it is, so the table matches the section header's claim per FR-012 item 8 (missing)
- [X] T145 Register INT and TERM traps in `scripts/tests/run-all.sh` and create each test file's temporary directory inside `RUNNER_TMP` rather than as its sibling, so an interrupted run cannot leak the per-file directory and cannot leave the summary unprinted per suite: run-all.sh (missing)
- [X] T146 Track skipped cases in a counter of their own in the harness and give a file whose cases were all skipped a verdict that is distinct from PASS, so "nothing was tested" can never be reported as "everything passed" per suite: run-all.sh, lib/harness.sh (missing)
- [X] T147 Make the integration suite's `run_script_bounded` delegate to the harness's invocation path — or carry the same `NUGET_PACKAGES` and `DOTNET_*` assignments the harness pins — so the fixture cases that reach the rebuild restore from the pinned cache instead of nuget.org per suite: test-integration.sh (partial)
- [X] T148 Identify each run's own log by the run id the script reports on stdout (or by the run id inside the log) and fail the case when the current run wrote no log, instead of falling back to the newest log present in the sandbox, so TC-27…TC-31 and TC-33 can no longer assert against an earlier case's log per suite: test-integration.sh (partial)

#### LOW

- [X] T149 Assert that the ERR-trap path always exits `13` — even when a command inside the handler fails — so a failure in the cleanup, the report pipeline or the summary cannot replace the mandated exit code with that command's raw status per FR-007, the unexpected-error edge case (missing)
- [X] T150 Assert that a child's last line reaches the run log even when it ends without a trailing newline — a child killed mid-write — and that the same holds for the build-output dump loop per FR-005, FR-014 (missing)
- [X] T151 Assert that a stale `Log started at` line and a stale `Loaded N mods:` header left before the sentinel can neither satisfy phase 1 of the readiness wait nor end phase 2 early, so only post-sentinel lines decide the wait per FR-002 (missing)
- [X] T152 Point the "relative game dir" case at a relative path under the harness sandbox instead of at the process's current working directory — through the runner that is the repository root per suite: test-cli.sh (partial)
- [X] T153 Replace the assertion that the fixture's `Mods` directory is a child of the base with one that reads a value the script produced, since the fixture itself creates the directory the assertion checks and the row can never fail per suite: test-preflight.sh (partial)
- [X] T154 Replay a captured trap definition verbatim in the harness's `trap -p EXIT` recovery instead of splitting it into words and keeping only the first, so a pre-existing trap's arguments are not dropped per suite: lib/harness.sh (partial)

### Implementation — HIGH

- [X] T155 Match the graphics-device markers against a record's message together with its continuation lines — the current gate skips every continuation line before the legs run, so the `Failed to create graphics device!` text, which SMAPI 4.5.2 places in the `Technical details:` block, is never inspected — and replace the non-existent `GPU: Could not detect` marker with one SMAPI actually emits, so a real graphics failure is reported as `3` and not as `17` per FR-007, the graphics-device edge case, data-model: validation rule 9 (partial)
- [X] T156 Derive the deploy folder from the project's effective `ModFolderName` MSBuild property — defaulting to `$(MSBuildProjectName)` — for BOTH the removal target and the deployed-assembly path, instead of the project file's base name, and correct the inline comment that claims the project's *directory* name is the deploy folder while the code uses the file name, so the folder SMAPI actually loads is cleaned and digest-verified per FR-016 (b) (d), SC-012 (partial)
- [X] T157 Correct every `TOOL_PACKAGE` entry the new package assertion exposes, so no row can report `unknown-package` and no tool's remediation names the wrong package per FR-012 item 8, the missing-prerequisite edge case (partial)

### Implementation — MEDIUM

- [X] T158 Make the ERR handler subshell-safe — have `on_error` return the status instead of calling `die` when `BASH_SUBSHELL` is greater than zero, and guard the pipelines evaluated inside command substitutions with `|| true`, starting with the sentinel's timestamp extraction — so only the outer shell reports and a failing run emits exactly one failure report per FR-007, FR-014 (partial)
- [X] T159 Close the phase that is open when the run fails — record its end timestamp and elapsed seconds in `die` and in the EXIT trap before the cleanup appends its own entry — so the failed phase appears in the phase-duration block and in the run log, as FR-014 requires for every phase per FR-014, SC-001 (partial)
- [X] T160 Capture the deploy-target removal's status and stderr and abort with exit `12`, naming the target and the removal's error, instead of discarding the result with `|| true` and `2>/dev/null`, so a stale tree that FR-016 (b) exists to remove can no longer survive a run that reports success per FR-016 (b), FR-012 item 7 (partial)
- [X] T161 Log the launch epoch the sentinel-freshness comparison actually used — the value taken immediately before the game is launched — and relabel the run log's header field that is written at run start and called the launch timestamp, so the freshness threshold is reconstructable from the run log per FR-002, FR-014, SC-014 (partial)
- [X] T162 Poll the SMAPI log for the mod's `GameLaunched` success marker for a bounded window after the mod-resolution header and before the game is terminated, so the `mod-success-marker:` line is reachable in a real run, and reconcile FR-005's "immediately" with that window per FR-003, FR-005, FR-006, SC-002 (partial)
- [X] T163 Add `sleep` — invoked in ten places and in neither `REQUIRED_TOOLS` nor the sweep table — to the script's host-tool sweep with its package mapping, so the sweep covers every external the script actually calls per FR-012 item 8 (partial)
- [X] T164 Run `shellcheck` over the seven shell files, fix or explicitly suppress every finding, and restate the plan's Gate 1 status on the strength of an executed lint rather than of a self-skipped one, since the cell currently claims "Satisfied" while stating in the same cell that the lint has never run per Constitution Gate 1, plan: Gate 1 row (contradicts)
- [X] T165 Add a pre-push step that runs `scripts/tests/run-all.sh` — with `SMOKE_SKIP_INTEGRATION=1` for the launch-free suites — so the feature's own verification vehicle is part of the gate it was written to satisfy, since nothing in `lefthook.yml` or in CI runs the bash suite today per Constitution Gate 3 (partial)
- [X] T166 State the repository mutation the integration suite's fixture cases cause — the rebuild writes `LivingRoots/obj/` and `LivingRoots/bin/` — in that suite's SAFETY header, or point the fixture cases at a throwaway project, so the two suites apply one documented policy about writing into the repository tree per suite: test-integration.sh (partial)
### Implementation — LOW

- [X] T167 Change the exit-`9` message that reports neither `pgrep` nor `pidof` as available so that it names the orphan-Xvfb detection those tools are actually used for — the game itself is detected through a `/proc/<pid>/exe` scan — so the remediation sends the operator to the right check per FR-012 item 8, FR-012 item 10 (partial)

- [X] T168 Run the ERR-trap path with `set +e` — `on_error` ends with an explicit `exit "$code"`, so the script still aborts — or guard every command `die` and `cleanup` execute, so a failure inside the handler cannot replace the mandated exit `13` with that command's raw status per FR-007, the unexpected-error edge case (partial)
- [X] T169 Move the bash-4 requirement check behind the command-line parsing, or exempt `--help`, `--version` and `-h` from it, so the usage reference and the version stay printable on a host that lacks bash 4 — the check currently runs above the function definitions and preempts the parser per FR-015, FR-012 (0) (a) (partial)
- [X] T170 Emit the residual partial line after the read loop exits — `read` returns non-zero at EOF with a partial buffer, so the loop body never runs for it — in `start_stamped_output` and in the build-output dump loop, so a child killed mid-write does not lose its last line silently per FR-005, FR-014 (partial)
- [X] T171 Read the display-number file once more after the deadline loop exits, before the emptiness test, so a number written in the last 100 ms is not missed and a live X server is not reported as "could not be allocated" per FR-012 item 5 (partial)
- [X] T172 Compute and name the elapsed seconds at the build-timeout expiry point — the message currently names the configured bound, not the time the run actually consumed — per FR-016 (b) (partial)
- [X] T173 Scope the two readiness probes to the post-sentinel region — require the `Log started at` match to be a prefixed record and the `Loaded N mods:` match to occur after it — so a stale pre-sentinel line can neither satisfy phase 1 nor end phase 2 early per FR-002 (partial)

### Documentation

- [X] T174 Correct the plan's "Process identification" decision row and research.md § 4.2 to state that the game is discovered by a `/proc/<pid>/exe` scan, with `pgrep`/`pidof` consulted only for the orphan-Xvfb scan, keeping the "never `pgrep -x` on the 17-character apphost" rationale per plan: design decisions, research.md § 4.2 (contradicts)
- [X] T175 Amend research.md's "Log Levels" line to name the internal `CRITICAL` level alongside the six public ones, so the closed six-level set the line asserts no longer contradicts the level bound by label per research.md § 3 (partial)
- [X] T176 Amend research-bash-security.md's banner so it no longer lists "ownership checks" among the recommendations that stand — no ownership verification is implemented — and record that the lock directory's ownership is satisfied by construction rather than by check per research-bash-security.md § banner (contradicts)
- [X] T177 Reconcile the task list's own stale text — fold the overlapping build-group clauses of T091 and T103 so each carries one clause, reword the Phase 8 note's dedup sentence that claims the process-group publication was merged into one task while T091 and T103 both carry it, and correct Phase 7's T089, whose claim that `internal_real` is assigned and never read no longer matches the code per tasks.md: Phase 8 note, T091, T103, T089 (contradicts)
- [X] T178 Reword T111 to say "the 29 tools in `REQUIRED_TOOLS` plus the `pgrep`-or-`pidof` alternative check" rather than "30 externals", so the plan's rewritten tool list does not enumerate 30 mandatory tools per tasks.md: T111 (partial)
- [X] T179 Add the severity to each Phase 8 task — as a `### HIGH` / `### MEDIUM` / `### LOW` grouping, as Phases 6 and 7 do, or as a tag on each line — so the note's "0 CRITICAL · 7 HIGH · 22 MEDIUM · 15 LOW" tally is checkable and the first-wave ordering can be applied to the remainder per tasks.md: Phase 8 (missing)
- [X] T180 Extend T133 to name the third convergence phase as well, and add a Phase 8 row to the Dependencies list, so the Implementation Strategy and the dependency graph cover T091–T134 per tasks.md: T133, the Dependencies section (partial)
- [X] T181 Update the three suite headers so the TDD-state paragraph and the property counts describe the suites as they are — the log-gate suite no longer expects to fail, and it asserts six more properties outside the table than its header says, while the CLI suite asserts five where its header says two per suite: test-logparse.sh, test-cli.sh (partial)

### Verification (attests completeness — run last, alone)

- [X] T182 Run the full suite — the CLI, pre-flight, log-gate and integration files — and the constitution's three gates, and record the assertion count and the gate verdicts in the quickstart's Test Oracle, so the completeness of this phase is attested by an executed run and not by the implementing agent per Constitution Gate 1, Gate 2, Gate 3, SC-014 (missing)
- [X] T183 Run the script against the installed game and SMAPI and record the exit code, the verdict and the run-log path, confirming that the mod's success marker is now reported — it was unreachable in every retained run because the game was terminated before the title screen — so the real-game path is attested by an executed run per SC-002, SC-014, SC-015 (missing)

### Follow-up — found during implementation (2026-10-22)

- [X] T184 Re-point TC-9's fixture at a deploy that is ineffective **without** making the removal fail — T160 made the deploy-target removal's failure **fatal**, so TC-9's unwritable target now aborts at the removal (exit `12`) before the build, and its two digest assertions never see the report naming both digests; the fixture must let the removal succeed and make the build or the deploy ineffective instead, so FR-016 (d)'s "naming **both** digests" path is still exercised per FR-016 (d), SC-012 (partial)
