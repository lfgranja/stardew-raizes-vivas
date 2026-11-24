# Quickstart: Smoke Validation Script

**Date**: 2026-10-19
**Context**: `specs/008-smoke-validation` — validation and run guide

---

## Prerequisites

Before running the smoke validation script, ensure:

1. **Stardew Valley** is installed via Steam on Linux at `~/.steam/steam/steamapps/common/Stardew Valley/`
2. **SMAPI 4.5.2** is installed and functional in the game directory. The correct markers are `StardewModdingAPI` (the ELF launcher), `StardewModdingAPI.dll`, and `smapi-internal/SMAPI.Toolkit.dll` + `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll`. **No SMAPI release ships `smapi-internal/SMAPI.dll`** — do not use that as the check
3. **Xvfb** is available on the system. On Fedora this is the `xorg-x11-server-Xvfb` package (`apt install xvfb` on Debian/Ubuntu). **It is not installed by default**, and the pre-flight will exit `9` without it (a missing binary is a missing host tool). **It was installed on the maintainer's machine on 2026-10-20** (`xorg-x11-server-Xvfb` 21.1.25), so the end-to-end cases are now executable
4. **Mesa software rendering** is usable, and `mesa-utils` provides `glxinfo` and `xdpyinfo`. The pre-flight starts the virtual display and asserts, in layers: `xdpyinfo` reports the `GLX` and `RANDR` extensions; `glxinfo -B` under `LIBGL_ALWAYS_SOFTWARE=1` exits **0** (it exits **255**, not 1, on failure); the `OpenGL renderer string` is `llvmpipe` or `softpipe`; and **full `glxinfo` reports `GL_ARB_framebuffer_object` or `GL_EXT_framebuffer_object`** (or the GL version is ≥ 3.0). That last layer is load-bearing — the game's MonoGame backend throws `PlatformNotSupportedException: MonoGame requires either ARB_framebuffer_object or EXT_framebuffer_object` when a context lacks FBOs, and `glxinfo -B` prints no extension list, so it cannot detect that failure. A missing `glxinfo`/`xdpyinfo` exits `9`; a stack that is present but cannot provide usable software OpenGL exits `3`
5. **The Living Roots mod** is deployed into the resolved mods directory (default `~/.steam/steam/steamapps/common/Stardew Valley/Mods/LivingRoots/`). The script rebuilds and redeploys it on every run — with the build's deploy target pinned to the resolved mods path — and verifies the deployed assembly by SHA-256 digest, so a manual deploy is not required. **Note:** the script *deletes and recreates* that directory on every run
6. **Bash 4+** is available (standard on Linux)
7. **Host paths may contain spaces and non-ASCII characters** — the game directory is `~/.steam/steam/steamapps/common/Stardew Valley/` and the game assembly is `Stardew Valley.dll`. **No character allowlist is applied to environment-derived names** and **no normalizer dependency is required**: path safety rests on `realpath` canonicalization, the component-boundary prefix test, and correct quoting of every path expansion (FR-012 item 7). NFC is a provable no-op for ASCII paths (UAX #15) and is not a homoglyph defence (that needs UTS #39)
8. **The .NET SDK (`dotnet`) IS required on the host** — the script rebuilds the mod before every run (FR-016). The *game* needs no host .NET runtime: the Steam install ships a self-contained .NET 6 `linux-x64` runtime (`libcoreclr.so`, `libhostfxr.so`, `libhostpolicy.so` in the game folder). Mono is not involved.
9. **The remaining host tools** must be on `PATH`. The sweep verifies **31 tools** with `command -v` — `Xvfb`, `xdpyinfo`, `glxinfo`, `flock`, `timeout`, `mktemp`, `stat`, `realpath`, `sha256sum`, `date`, `grep`, `dotnet`, `ps`, `readlink`, `sed`, `awk`, `cut`, `head`, `tr`, `cp`, `cat`, `sort`, `touch`, `mkdir`, `chmod`, `rm`, `dirname`, `basename`, `tail`, `sleep` and `mkfifo` — and **either `pgrep` or `pidof`** must be present as well (the orphan-display detection consults both, so one of the two suffices). A missing tool exits `9` naming the tool **and the package that provides it** (FR-012 item 8)

**Not required**: a running Steam client. The game ships `steam_appid.txt` (`413150`) — the pre-flight verifies it and exits `11` if it is missing or wrong. SMAPI logs "Oops! Steam achievements won't work because Steam isn't loaded." at Error level from **SMAPI core**, which is outside the mod-scoped gate and therefore does not fail the smoke test.

---

## Running the Script

### Basic Execution

```bash
./scripts/smoke-validation.sh
```

**Expected outcome (success)**: Exit code `0`, message `SUCCESS: Mod loaded without errors.`

**Expected outcome (failure)**: Non-zero exit code with descriptive message following "What → Where → How to fix" pattern.

Each run writes a diagnostic log to `${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log`. The path and the run ID are printed in the summary on stdout.

### Command-Line Options

The table below is a **verbatim copy** of the normative option table in `spec.md` → "Command-Line Options (normative)". If the two ever differ, the spec wins.

| Option | Argument | Default | Introduced by | Behaviour |
|--------|----------|---------|---------------|-----------|
| `--game-path` | `DIR` | `~/.steam/steam/steamapps/common/Stardew Valley/` | FR-012 (1) | Root of the game installation. Also the process working directory of the SMAPI launch (FR-001) and the base against which a **relative** `--mods-path` is resolved (FR-017). May contain spaces |
| `--mods-path` | `DIR` | `<game-path>/Mods` | FR-017 | The mods directory handed to SMAPI and pinned as the build's deploy target (FR-016 (c)). May contain spaces |
| `--mod-project` | `FILE` | `<repo-root>/LivingRoots/LivingRoots.csproj` (where `<repo-root>` is `SCRIPT_DIR/..`) | FR-016 (a) | The `.csproj` rebuilt and digest-verified before the launch |
| `--timeout` | `SECONDS` | `120` | FR-009 | The game-launch-to-verdict deadline (the two-phase wait) |
| `--build-timeout` | `SECONDS` | `300` | FR-016 (b) | The bound on the rebuild-and-verify phase; expiry exits `12` |
| `--grace-period` | `SECONDS` | `15` | FR-005 | The SIGTERM → SIGKILL escalation window |
| `--smapi-log` | `FILE` | `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` | FR-002 | The SMAPI log polled for readiness and for the verdict |
| `--force` | *(none — flag)* | off | FR-011 | Terminate an existing game instance (SIGTERM → SIGKILL) instead of aborting. The signal targets the **identified PID only** |
| `--help` | *(none — flag)* | — | FR-015 | Print the usage text to stdout and exit `0` |
| `-h` | *(none — flag)* | — | FR-015 | Alias for `--help`. Never the canonical spelling |
| `--version` | *(none — flag)* | — | FR-015 | Print `<name> <VERSION>` to stdout and exit `0` |

Argument syntax: `--opt VALUE` and `--opt=VALUE` are both accepted and equivalent. `--` terminates option processing. Long-option names are **not** abbreviated — `--mod` is ambiguous between `--mod-project` and `--mods-path` and is rejected, and `--mods` would silently mean `--mods-path`. The script takes no positional arguments; one is a usage error (exit `14`).

### Game Installation Path

```bash
# Default: ~/.steam/steam/steamapps/common/Stardew Valley/
./scripts/smoke-validation.sh

# Point the launch at a different installation
./scripts/smoke-validation.sh --game-path "/path/to/Stardew Valley"
```

A relative `--mods-path` is resolved against the resolved game directory (FR-017). The path may contain spaces; it is passed to every external command behind `--` and is never re-parsed by a shell.

### SMAPI Log Path

```bash
# Default: ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt
./scripts/smoke-validation.sh

# Poll a different SMAPI log
./scripts/smoke-validation.sh --smapi-log /path/to/SMAPI-latest.txt
```

### Grace Period

```bash
# Default: 15 seconds
./scripts/smoke-validation.sh

# Give the game 30 seconds to exit before SIGKILL
./scripts/smoke-validation.sh --grace-period 30
```

### Help and Version

```bash
./scripts/smoke-validation.sh --help     # usage, options, environment, exit codes
./scripts/smoke-validation.sh -h         # alias for --help
./scripts/smoke-validation.sh --version  # "<name> <VERSION>", exit 0
```

### Configurable Timeout

```bash
# Default 120-second timeout
./scripts/smoke-validation.sh

# Custom 180-second timeout
./scripts/smoke-validation.sh --timeout 180
```

`--timeout` bounds the **game-launch-to-verdict window** only, and it is measured **from the instant the game process is launched** — not from the script's start. The pre-flight and the rebuild-and-verify phase run before the launch and are **not** charged against it (FR-009). The rebuild has its own bound, `--build-timeout` (default 300 seconds), so a hung NuGet restore cannot hang the run forever (FR-016). The run log records the duration of every phase and the total wall time, and the stdout summary prints both as well, so both budgets are auditable after the fact (FR-014).

### Build Timeout

```bash
# Default: 300 seconds
./scripts/smoke-validation.sh

# Give the rebuild 10 minutes
./scripts/smoke-validation.sh --build-timeout 600
```

A build that exceeds the bound aborts the run with exit code `12`. The report names **the bound that expired** — `What: the rebuild exceeded its 1s bound` for `--build-timeout 1` — and not the elapsed time. The build timeout is independent of `--timeout` and is never consumed from it.

### Running Quietly

The game runs on a **virtual** X display (Xvfb), so **nothing is ever drawn on your screen** — there is no window to minimize, and the console output is redirected to the run log rather than the terminal. The game is also **muted**: the script exports `SDL_AUDIODRIVER=dummy` and `ALSOFT_DRIVERS=null`, because Xvfb virtualizes the display but *not* the audio device, and without that mute the run would play the title-screen music through your speakers (FR-001).

### Mods Directory

```bash
# Default: <game-path>/Mods
./scripts/smoke-validation.sh

# Point SMAPI at a different mods directory
./scripts/smoke-validation.sh --mods-path /path/to/mods
```

A relative `--mods-path` is resolved against the game directory, matching SMAPI's own semantics; an absolute one is accepted as-is. **The build deploys into the same directory** — the script passes `-p:GameModsPath="<resolved>"` so that the assembly the game loads is the one this run built (FR-016).

### Mod Project

```bash
# Default: <repo-root>/LivingRoots/LivingRoots.csproj  (derived from the script's own location)
./scripts/smoke-validation.sh

# Point the rebuild at a different project
./scripts/smoke-validation.sh --mod-project /path/to/LivingRoots.csproj
```

### Force Mode (Kill Existing Game)

```bash
# If a Stardew Valley/SMAPI process is already running, kill it first
./scripts/smoke-validation.sh --force
```

### What the script does before it launches the game

1. **Command line, then tool sweep** — the command line is parsed **before** the sweep, so `--help`, `--version` and a usage error (exit `14`) are answered even on a host that is missing one of the tools. The sweep then verifies every required host tool with `command -v`; a missing one exits `9` naming the tool **and the package that provides it** (FR-012 item 8, FR-015)
2. **Game & SMAPI checks** — the game installation and SMAPI's markers; then the Steam prerequisite `steam_appid.txt` (FR-012 items 1, 2, 9)
3. **Stale-artifact cleanup** — a leftover `SMAPI-crash.txt` and SMAPI's **crash and update markers** (`smapi-internal/StardewModdingAPI.crash.marker` and `…update.marker`) are removed, because a marker is what makes SMAPI perform a keypress read that aborts the headless launch before mod resolution (FR-012 item 6)
4. **Virtual display + graphics probe** — Xvfb is started with `-displayfd` (the server allocates a free display and reports it back), then the layered software-OpenGL probe runs against it (FR-012 items 4, 5)
5. **Build & verify artifact** — `dotnet build --configuration Release --no-incremental` with `-p:GameModsPath="<resolved mods path>"`, after removing the deploy target; the deployed assembly's SHA-256 digest is compared with the freshly built one. **A mismatch aborts the run before the game is launched** (FR-016)
6. **Mod manifest check** — the resolved mods path contains `LivingRoots/manifest.json` with a matching `UniqueID` (FR-017)
7. **Flock** + existing-process detection — both run **after** the rebuild-and-verify step (FR-012 (0)), so the build is not serialised behind another instance's whole run (FR-002, FR-011)
8. **Log baseline** — records the **inode** of the existing SMAPI log, *then* removes any stale `SMAPI-latest.txt` and asserts that it is gone (the order matters: recording after the removal would make the inode assertion vacuous). The **launch instant is not recorded here** — it is logged later, in the launch phase, as `launch timestamp:` and `launch epoch:`; the run log's header field `run started:` is the **run's** start instant, not the launch's (FR-002, FR-014)

The success verdict is derived from SMAPI's own `Loaded N mods:` / `Skipped mods` output — not from the mod's self-reported marker, which only appears once the game reaches the title screen.

---

## Validation Scenarios

### Test Case Specification (ISO/IEC/IEEE 29119-3-inspired)

> **Conformance statement.** Structured after **ISO/IEC/IEEE 29119-3:2021** (*Software testing — Part 3: Test documentation*), which superseded **IEEE 829-2008** (IEEE lists 829-2008 as a *"Superseded Standard"*). The section naming follows IEEE 829-2008 §9.2, which 29119-3 Annex S maps to 29119-3 §6.2. We claim **tailored** conformance per 29119-3 §4.1.3: the Test Plan (test strategy), Test Case Specification, Test Procedure Specification and Test Execution Log information items are implemented; the remaining Annex A items are out of scope for a single-script smoke test. The justification for that tailoring is recorded in *Test Scope — Features Not Tested* below.

Each scenario below is a test case with a fixed input, a fixed execution condition, and a defined expected output. **Actual output** and **Pass/Fail** are recorded at run time by the person executing the script (fill them in when the run happens, and keep them with the escalation evidence bundle).

#### Test Scope — Features Not Tested

Identifies the features that are **excluded from testing**, and the rationale for each exclusion (29119-3 §6.2.4.3).

| Feature / area excluded | Rationale for exclusion | Where it is covered instead |
|------------------------|------------------------|------------------------------|
| Save-data persistence (the mod's JSON sidecars under SMAPI's mod-data directory) | Deferred — the automated script cannot load a save (SMAPI has no CLI save selection; the mod is purely reactive). The title screen is the expected state (FR-004) | Manual play-through; spec 005 / the deferred US2 scenarios |
| Gameplay behaviour, soil-health simulation, composting bin state | Out of scope for a **load** smoke test — this feature validates that the mod *loads* in the real game without errors, not that its mechanics are correct | Manual play-through (Gate 2/3) |
| Multiplayer | Deferred (roadmap D1) | Not currently planned |
| Non-functional quality attributes (performance, usability, accessibility) | A smoke test does not exercise them | Not currently planned |
| The Steam client itself | Not required — the game ships `steam_appid.txt` (`413150`); Steam only disables achievements, which SMAPI logs from **core** (outside the mod-scoped gate) | n/a — explicitly out of scope |
| CI integration, `--json` machine-parseable output | Deferred by FR-013 (local-first) | n/a — explicitly out of scope |
| Container / PID-1 signal semantics | Not a container entrypoint; no Dockerfile exists (round-2 clarification) | n/a — explicitly out of scope |

#### Test Items and Their Identifiers

The items under test, with their version/revision level (IEEE 829-2008 §9.2.1 → 29119-3 §6.2.4.2).

| Test item | Identifier / version |
|-----------|----------------------|
| Smoke validation script | `scripts/smoke-validation.sh`, `$VERSION` (reported in the run-log header, the stdout summary and `--help`) |
| Living Roots mod | `lfgranja.LivingRoots`, manifest `Version` `0.0.1`, git commit SHA (recorded in the escalation bundle) |
| SMAPI | version parsed from the log banner `SMAPI <ver> with Stardew Valley <ver> on <os>` |
| Stardew Valley | game assembly version (recorded in the same banner) |
| Host graphics stack | `Xvfb` version + the Mesa software renderer reported by `glxinfo -B` |

#### Item Pass/Fail Criteria

The aggregate criterion for the smoke test as a whole, defined **before** the run (IEEE 829-2008 §9.2.6 → 29119-3 §6.2.7.4):

> The smoke test **PASSES** iff the script's exit code is `0` **and** the run log contains no Error/Alert entry attributable to the mod **and** no orphaned game, Xvfb or lock resource remains after cleanup. It **FAILS** otherwise, with the exit code from the FR-007 taxonomy identifying the failure class. An individual run never *suspends* — it fails with a code; suspension is a campaign-level decision (see *Suspension Criteria*).

#### Test Oracle

**Executed on 2026-10-21** against the installed Stardew Valley + SMAPI 4.5.2 (Linux);
the four suites were **re-measured on 2026-10-22**. The four suites are
`scripts/tests/run-all.sh test-cli.sh test-preflight.sh test-logparse.sh
test-integration.sh` and their current sizes are **CLI 255**, **pre-flight 380**,
**log gate 167** and **integration 363**. The CLI and the pre-flight figures were
re-measured here — `bash scripts/tests/test-cli.sh` ends with `# tests: 255,
passed: 255, failed: 0` and `bash scripts/tests/test-preflight.sh` with `# tests:
380, passed: 380, failed: 0` (both are launch-free — the pre-flight suite stubs
`dotnet`, so no build ever runs). The log-gate and the integration figures are
the orchestrator's measurement — those two suites are **not** re-run in this pass
(the integration suite performs a real `dotnet build` and takes about ten
minutes). **The overall assertion total is recorded by the verification task**
(`tasks.md` → T182/T183), which measures all four suites in a single run; it is
not computed here. The **Actual output** column below records what the
2026-10-21 execution observed; the **Pass/Fail** column records the verdict.
TC-13 is the one case no numbered assertion covers (see its row).

**Measured by the verification task (T182) on 2026-10-22 — all four suites in one
run, 0 failures:**

| Suite | Assertions | Result |
|---|---|---|
| `test-cli.sh` | 255 | 255 passed, 0 failed |
| `test-preflight.sh` | 380 | 380 passed, 0 failed |
| `test-logparse.sh` | 167 | 167 passed, 0 failed |
| `test-integration.sh` | 363 | 363 passed, 0 failed |
| **Total** | **1 165** | **1 165 passed, 0 failed** |

> The suites were run with `TMPDIR=/var/tmp`. `/tmp` is a 2 GB tmpfs that other
> sessions' leftovers had filled to 100 %; a full tmpfs makes the X server fail
> with exit `10` and the build hang, which is an **environment symptom, not a
> verdict**. `/var/tmp` is a separate filesystem with free space and is
> **outside `$HOME`**, so SMAPI does not `~`-anonymise the mods path — the only
> `TMPDIR` that breaks TC-14 is one **inside** `$HOME`.

The **oracle** for every test case is the FR-007 exit-code contract (`data-model.md` → *Exit Codes*), plus, where the case concerns log content, the current run's `Log started at {UTC} UTC` sentinel. A test case **passes** iff the actual exit code equals the expected exit code **and** the named log predicate holds.

| # | Test item / feature | Objective | Priority | Traceability | Input | Execution condition | Expected output | Actual output | Pass/Fail |
|---|-----------|-----------|----------|---------------|-------|---------------------|-----------------|---------------|-----------|
| TC-1 | Mod loads successfully | Verify the mod resolves and loads in the real game with no errors | Must | FR-006, SC-002 | Default install paths, mod deployed, no save loaded | Script run with no arguments; **launch-to-verdict** within `--timeout` (default 120 s) | Exit `0`; stdout carries the summary, in this order: `SUCCESS: Mod loaded without errors.`, then — **only when the mod's own success marker was detected in the log** — `mod-success-marker: <marker>`, then `run-id: <run-id>`, `version: <name> <VERSION>`, `run-log: <path>`, then **`total wall time: <n>s`**, and finally the **`phase durations:` block** — a `phase durations:` header line followed by one line per phase, `  <phase> <n>s` (FR-014 requires the stdout summary to carry the total wall time **and** each phase's duration; the shipped summary prints the total wall time **before** the phase durations, not after them); the mod appears in SMAPI's loaded-mods block; game and Xvfb terminated; no orphaned processes; the run log **additionally** records the per-phase durations (start, end and elapsed seconds for each phase) and the total wall time; and the run log records the launch contract — the apphost was invoked as `./StardewModdingAPI --no-terminal` with the game directory as the working directory, `SMAPI_NO_TERMINAL=true`, `SMAPI_DEVELOPER_MODE=true`, `LIBGL_ALWAYS_SOFTWARE=1`, `GALLIUM_DRIVER=llvmpipe`, `SDL_VIDEODRIVER=x11`, `SDL_AUDIODRIVER=dummy` and `ALSOFT_DRIVERS=null` were exported, Xvfb was started with `-nolisten tcp -displayfd`, and `DISPLAY` was set from the reported display number | Exit `0`; stdout carried, in this order, `SUCCESS: Mod loaded without errors.`, the run id, the version, the run-log path, **the total wall time** and then the `phase durations:` block (one line per phase, pre-flight through cleanup) — the total wall time is printed **before** the per-phase durations; the run log recorded the apphost invocation `./StardewModdingAPI --no-terminal`, the game directory as the working directory, all seven exported environment variables, the `-nolisten tcp -displayfd` Xvfb flags, `DISPLAY` from the reported number, every phase duration and the launch timestamp | ☑ Pass |
| TC-2 | Mod load-time error | Verify a mod-scoped Error entry fails the run and is reported | Must | FR-003, FR-006, SC-003 | Mod build with a failing `ModEntry.Entry()` | Script run with no arguments | Exit `5`; failure message with "What → Where → How to fix" and SMAPI log context | Exit `5`; the report named the matched failure record and carried its continuation lines, the SMAPI log path and a trailing `Fix:` line | ☑ Pass |
| TC-3 | Game not installed | Verify the pre-flight fails closed on a missing game install | Must | FR-007, FR-012, SC-009 | `--game-path` pointing at a non-existent directory | Script run with `--game-path <missing>` | Exit `1`; actionable pre-flight error naming the missing path | Exit `1`; the report named the missing game path and ended with an actionable `Fix:` line; the diagnostics were on stderr, not stdout | ☑ Pass |
| TC-4 | Game already running | Verify an existing game instance is detected and not clobbered | Must | FR-011, SC-007 | An existing Stardew Valley/SMAPI process | Script run **without** `--force` | Exit `7`; error listing the conflicting PIDs | Exit `7`; the report listed the conflicting PID and the running game was not clobbered; with `--force` the run proceeded to the launch and the identified process was terminated | ☑ Pass |
| TC-5 | Concurrent execution prevention | Verify a second instance cannot run concurrently | Must | FR-002, SC-008 | A second instance already holds the lock | Second script instance started while the first is running | Exit `6`; lock contention error; the flock is taken **after** the rebuild-and-verify phase (FR-012 (0)), so the second instance runs the tool sweep, the pre-flight, the display allocation and the rebuild **before** it discovers the contention — it does **not** exit immediately | Exit `6`; the report named the lock and ended with an actionable `Fix:` line; the contention was reported only after the rebuild-and-verify phase had completed | ☑ Pass |
| TC-6 | Timeout | Verify the configurable timeout is honoured | Must | FR-009, SC-001 | Game that does not produce the SMAPI log | Script run with `--timeout 30`; no log produced within 30s **measured from the game launch** | Exit `4`; timeout error with the log path and the `--timeout` hint | Exit `4`; the timeout report named the SMAPI log, the timeout and an actionable `Fix:` line | ☑ Pass |
| TC-7 | Interruption | Verify cleanup runs and the exit status is the POSIX signal status | Must | FR-005, FR-010, SC-010 | Script in its wait/poll phase | User presses Ctrl+C (SIGINT) | Exit `130`; cleanup ran (game killed, Xvfb killed, the socket of the display this script allocated removed, temp files cleaned) | Exit `130`; the raw wait status showed `WIFSIGNALED` set with signal `2` (a genuine signal death, not a plain exit), and the cleanup killed the game process | ☑ Pass |
| TC-8 | Pre-flight dependency missing | Verify the pre-flight fails closed when a required host tool is absent | Must | FR-012 item 8, SC-009 | Environment without `Xvfb` on `PATH` | Script run with `Xvfb` removed from `PATH` | Exit `9`; pre-flight error naming the missing tool and the package that provides it (`xorg-x11-server-Xvfb`) | Exit `9`; the report named the missing tool (`Xvfb`) and the package that provides it (`xorg-x11-server-Xvfb`) | ☑ Pass |
| TC-9 | Deploy ineffective | Verify the deployed assembly is byte-identical to the one this run built | Must | FR-016, SC-012 | The build cannot replace the deployed assembly in the resolved mods path | Make the resolved mods path read-only (or seed a stale `LivingRoots.dll` and make the deploy a no-op), then run the script | Exit `12` **before** the game launches; the message names both SHA-256 digests | Exit `12` before the launch; the report named **both** SHA-256 digests (built and deployed) | ☑ Pass |
| TC-10 | Stale log cannot produce a false pass | Verify a stale success marker cannot make the run pass | Must | FR-002, SC-011 | A previous run left an `SMAPI-latest.txt` containing the mod's success marker | Seed a stale log, then run the script against a mod that fails to load | Exit `5` (not `0`); the stale marker is ignored because it precedes the current run's `Log started at` sentinel | Exit `5`, not `0`; the stale marker was ignored because it precedes the current run's sentinel | ☑ Pass |
| TC-11 | Mod skipped by SMAPI | Verify a SMAPI skip is reported as a mod-load failure with SMAPI's reason | Must | FR-003, FR-006, SC-013 | Installed SMAPI older than the mod's `MinimumApiVersion` | Run the script with an incompatible SMAPI version | Exit `5`; report states SMAPI's skip reason ("needs SMAPI X or later") | Exit `5`; the report carried SMAPI's stated reason (`it needs SMAPI 4.6.0 or later. …`) | ☑ Pass |
| TC-12 | SMAPI exits without producing a log | Verify a clean SMAPI exit that produced no log is a failure, not a success | Must | FR-002, FR-007 | A stale `StardewModdingAPI.deps.json` makes SMAPI re-assert its dependency manifest and exit before the game starts | Run the script against a SMAPI whose dependency manifest is stale | Exit `15`; the standard stamped failure report: the `What:` line says the game exited without producing a SMAPI log, the `Where:` line names the **SMAPI log path**, and the trailing `Fix:` line states the relaunch requirement — `re-run the SMAPI installer in the game directory, then re-run`. The stale dependency manifest itself is **not** named | Exit `15`; the report named the SMAPI log and carried an actionable `Fix:` line; a log-less run was never reported as a success | ☑ Pass |
| TC-13 | Graphics environment unusable | Verify a present-but-unusable graphics stack is distinguished from a missing one | Must | FR-012 item 4, FR-007 | `glxinfo` reports no software renderer under Xvfb | Run the script with Mesa software rendering disabled | Exit `3`; error names the missing software renderer and the fix | **Not covered by a numbered case.** The layered software-OpenGL probe is exercised by the pre-flight (TC-23 proves a failing Xvfb is reported as `10` and not as `3`), but no case asserts the `3` verdict itself | ☑ — |
| TC-14 | Mods directory override | Verify the resolved mods directory is the one SMAPI uses | Must | FR-017 | A mods directory other than `<game-path>/Mods` | Run the script with `--mods-path <dir>` containing the mod | SMAPI receives the resolved directory (the `Mods go here:` line in the log matches the resolved path **after applying SMAPI's `~`-anonymisation**); the verdict is computed from that directory's mod | The resolved mods directory is the one SMAPI loads from; the verdict is computed from that directory's mod | ☑ Pass |
| TC-15 | Run log, run ID, version and stream split | Verify the run log is produced and the streams are separated | Must | FR-014, SC-014 | Default install paths, mod deployed | Script run with no arguments | A run log exists at `${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log` and contains the run ID, the script `VERSION` and the launch timestamp; the **verdict** is on stdout and the **diagnostics** are on stderr; the run ID appears on every log line | The run log existed at the FR-014 path with mode `0600` and `0700` directory levels; the run id had the `<UTC timestamp>-<PID>` form; the run log carried the `VERSION` and the launch timestamp; **every** line in the run log carried the run id; the verdict was on stdout and the diagnostics on stderr | ☑ Pass |
| TC-16 | `--help` completeness | Verify `--help` documents every contract the spec requires | Must | FR-015, SC-006 | — | `smoke-validation.sh --help` | Exits `0`; stdout documents the usage, every option (including `--game-path`, `--mods-path`, `--mod-project`, `--timeout`, `--build-timeout`, `--grace-period`, `--smapi-log`, `--force`, `--help`/`-h` and `--version`), at least one example, the "Environment (set automatically)" section, the run-log location, the run-ID format, the `VERSION`, and **every** exit code in the FR-007 table | Exit `0`; stdout documented the usage line, every option of the normative table with its placeholder and default, the Environment section, the run-log location, the run-ID format, the `VERSION` and **every** exit code (`0`–`7`, `9`–`17`, `129`, `130`, `143`) | ☑ Pass |
| TC-17 | Status translation | Verify no raw subprocess status ever becomes the script's final code | Must | FR-007 | — | Feed each raw child status in the FR-007 translation table to the translator | Each status maps to its documented exit code (`124`→`4`, `125`→`13`, `126`→`9`, `127`→`9`, `131`→`2`, `134`→`2`, `137`→`5`/`2`/`13`, `154`→`2`, `128+N` unchanged, any other non-zero → `13`) | Every row of the FR-007 translation table mapped to its documented code (`124`→`4`, `125`→`13`, `126`→`9`, `127`→`9`, `131`→`2`, `134`→`2`, `135`→`13`, `154`→`2`, `137`→`5`/`2`/`13`, `42`→`13`, `0`→`0`); SIGTERM and SIGHUP were genuine signal deaths mapping to `143` and `129` | ☑ Pass |
| TC-18 | `--version` and the `-h` alias | Verify the script prints its version on stdout and exits 0, and that `-h` is accepted as an alias for `--help` | Must | FR-015, SC-006 | — | `smoke-validation.sh --version` and `smoke-validation.sh -h` (no game, no display, no build required) | `--version` exits `0`; stdout is exactly `<name> <VERSION>` (one line, matching the `VERSION` in the run-log header and the stdout summary) and nothing is written to stderr; `-h` exits `0` and produces byte-identical output to `--help` | `--version` exited `0`, printed exactly `smoke-validation.sh 0.1.0` on stdout, wrote nothing to stderr and printed one line; `-h` produced byte-identical output to `--help` | ☑ Pass |
| TC-19 | Usage / CLI-argument error | Verify an unknown option and a malformed option value are rejected by the argument parser with the dedicated usage code | Must | FR-007, FR-015 | — | `smoke-validation.sh --no-such-option` and `smoke-validation.sh --timeout abc` | Both exit `14`; the message names the offending option and its expected form; nothing is launched (no SMAPI log touched, no game process, no display started, no build run); and **no run log is created** — the script creates the run log only after the command line has been accepted, so a usage error never produces one | Both an unknown option and a malformed value exited `14`, named the offending option, wrote nothing to stdout and launched nothing; **no run log was created** | ☑ Pass |
| TC-20 | Invalid `--mods-path` rejected before launch | Verify a mods path that cannot host the deploy target is rejected by the pre-flight with the code the FR-007 taxonomy assigns to that condition | Must | FR-007, FR-012 item 7, FR-017 | A `--mods-path` that does not exist or is not a directory | `smoke-validation.sh --mods-path "$TMP/no-such-mods-dir"` | Exit `16` **before** the build and before any launch; the message names the offending path and says what was expected of it; the build is not run, the deploy target is not touched, and no game process starts | Exit `16` before the build; the report named the offending mods path and ended with an actionable `Fix:` line; the rejected path was not created, the deploy target was not touched and no game was launched | ☑ Pass |
| TC-21 | SMAPI not functional | Verify the FR-012 item 2 marker check fails closed and reports SMAPI as the missing component | Must | FR-002, FR-007, FR-012 item 2 | A game directory that contains `Stardew Valley.dll`, `StardewModdingAPI`, `StardewModdingAPI.dll` and `smapi-internal/SMAPI.Toolkit.dll` but **not** `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll` | `smoke-validation.sh --game-path "$FIXTURE/game-broken-smapi"` | Exit `2`; the message names SMAPI as the failed component, the missing marker file and the SMAPI-reinstall remediation; the game is not launched and the build step is not reached | Exit `2`; the report named SMAPI as the failed component and the missing marker file (`smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll`); the game was not launched | ☑ Pass |
| TC-22 | Steam prerequisite missing | Verify the `steam_appid.txt` check fails closed with the dedicated code | Must | FR-007, FR-012 item 9 | A game directory identical to the real one except `steam_appid.txt` contains a wrong app id | `smoke-validation.sh --game-path "$FIXTURE/game-bad-steam"` | Exit `11`; the message names `steam_appid.txt`, the expected id `413150` and the observed value; the game is not launched | Exit `11`; the report named `steam_appid.txt`, the expected app id `413150` and the observed value; the game was not launched | ☑ Pass |
| TC-23 | Virtual display cannot be allocated | Verify a present-but-failing Xvfb is reported as a display-allocation failure, distinct from a missing tool (`9`) and an unusable graphics stack (`3`) | Must | FR-007, FR-012 item 5 | A stub `Xvfb` earlier on `PATH` that exits non-zero without printing a display number | `PATH="$FIXTURE/stub:$PATH" smoke-validation.sh` with `Xvfb`, `xdpyinfo` and `glxinfo` all present | Exit `10`; the message states that the virtual display could not be allocated or started and names `Xvfb`; the graphics probe is not reached, and the failure is **not** reported as `9` or `3` | Exit `10`; the failure was **not** reported as `9` and **not** as `3`; the report named `Xvfb` and said the virtual display could not be allocated or started; the graphics probe was not reached | ☑ Pass |
| TC-24 | Internal script error | Verify an unhandled error is reported by the ERR trap with the internal-error code and never as a raw child status | Should | FR-007, FR-014 | `XDG_STATE_HOME` pointing at a directory that is not writable | `XDG_STATE_HOME="$TMP/readonly" smoke-validation.sh` | Exit `13`; the ERR trap reports the script name, the failing line number, the enclosing function and the exit code; the exit code is **not** the raw status of the failing command (e.g. not `1`) | Exit `13`; the exit code was not the raw status of the failing command; the ERR trap reported the script name, the failing line number, the enclosing function and the exit code | ☑ Pass |
| TC-25 | Idempotency | Verify two consecutive runs under identical conditions produce the same verdict | Must | FR-008, SC-001, SC-015 | Default install paths, mod deployed, TC-1 already passed in this session | Run `smoke-validation.sh` twice in sequence, unmodified | Both runs exit with the same code and print the same verdict line; the second run finds no state left by the first (no stale log is reused, the deploy is redone, the lock is re-acquirable); the two run logs differ only in run ID and timestamps | Both runs exited with the same code and printed the same verdict; the second run re-acquired the lock | ☑ Pass |
| TC-26 | Build timeout | Verify the rebuild phase is bounded and reports the build failure distinctly | Should | FR-007, FR-016 (b) | A `--build-timeout` small enough to expire during the rebuild | `smoke-validation.sh --build-timeout 1` with a cold build | Exit `12`; stderr carries the "What → Where → How to fix" report — `What: the rebuild exceeded its 1s bound`, `Where: dotnet build <mod-project>`, `Fix: raise --build-timeout, or fix the build (the run log has the build output)` — so the report names **the bound that expired**, not the elapsed time; the game is never launched | Exit `12`; the game was never launched; the report named the expired bound (`1s`) and the elapsed time and ended with an actionable `Fix:` line | ☑ Pass |

#### Requirement-Coverage Summary

ISO/IEC/IEEE 29119-3 §6.2.4.3 requires the test scope to identify excluded features **and the rationale for excluding them**. This table records the deliberate exclusions; everything else is traced in the `Traceability` column of the table above.

**The status is stated per MUST sub-clause, not per requirement.** A requirement counts as *Covered* only when **every** MUST sub-clause it carries has at least one assertion behind it. The sub-clause-by-sub-clause breakdown — with the assertion that covers each one, and the reason for each one that has none — is the table *Per-clause coverage (MUST sub-clauses)* below. Where a requirement is only partially covered, that table names the exact sub-clauses that no assertion covers.

| Requirement / area | Covered by | Status | Rationale / where it is covered instead |
|---|---|---|---|
| FR-001 (launch contract) | TC-1, TC-15 | Covered | Asserted from the run log (FR-014): the apphost invocation and working directory, the exported environment, and the Xvfb flags. No extra run required |
| FR-002 (log freshness, the lock, the pre-launch cleanup, the two-phase wait) | TC-5, TC-10, TC-12, TC-14, TC-34, TC-41; the log-gate suite's freshness, inode-guard and stale-line properties; the pre-flight suite's T108 | **Partially covered** | 6 of its 10 MUST sub-clauses are fully asserted. The removal and logging of SMAPI's **crash/update markers**, the lock-descriptor discipline (`exec 9<>`, `9<&-`) and the descriptor-based `chmod 0600` have **no assertion** — see the per-clause table |
| FR-003 (record shape, failure markers, the success gate) | TC-42; the log-gate suite's 16-row table and its eight properties | **Partially covered** | 6 of its 9 MUST sub-clauses are fully asserted. The `Loaded <N> content packs:` block and the "the manifest name matched as a substring followed by a space or the end of the line" rule have **no assertion** |
| FR-005 (termination, the process groups, the trap design) | TC-7, TC-17, TC-27, TC-30, TC-31, TC-32, TC-35, TC-44 | **Partially covered** | 2 of its 8 MUST sub-clauses are fully asserted. The `set -m` scoping, the `wait -f "$pid"` form, the `9<&-` descriptor close and the trap-design mechanics (the single `EXIT` trap, the `$?` capture, the `in_cleanup` guard, the ban on `trap '' INT TERM`) have **no assertion** |
| FR-006 (the success verdict) | TC-1, TC-2, TC-28, TC-32, TC-42; the log-gate suite's whole table | Covered | Every MUST clause is asserted: the three-leg AND over the post-sentinel stream, the marker as a non-gating secondary signal, the deferred save check and the exact success message |
| FR-007 (the failure report and the exit-code taxonomy) | TC-2, TC-3, TC-4, TC-5, TC-6, TC-9, TC-12, TC-17, TC-20…TC-24, TC-26, TC-33; the log-gate suite's context rows | **Partially covered** | 5 of its 6 MUST clauses are fully asserted. The **allocation rule** — a published code never re-assigned, the retired `8` never reused — has **no assertion** |
| FR-009 (the launch-to-verdict timeout) | TC-6, TC-26, TC-34, TC-39; the CLI suite's option-surface property | **Partially covered** | 3 of its 4 MUST clauses are fully asserted (the default `120`, the launch anchor, the two-phase bound, the "not a whole-script deadline" rule). The source-level form of the deadline poll loop — and the ban on wrapping the game launch in `timeout` — has **no assertion** |
| FR-010 (cleanup) | TC-7, TC-27, TC-31, TC-32, TC-35, TC-37; the pre-flight suite's T108 | **Partially covered** | 2 of its 6 MUST clauses are fully asserted. The per-step isolation (`\|\| true`), the reverse-allocation order and the "MUST NOT create or remove `/tmp/.X{n}-lock` files" half have **no assertion** |
| FR-011 (an already-running game) | TC-4 | **Partially covered** | 2 of its 5 MUST clauses are fully asserted. The victim's executable path and start time are **not** observed in any assertion, and a PID found by scanning the process table is never verified before it is signalled |
| FR-012 (the pre-flight) | TC-3, TC-8, TC-20…TC-24; the pre-flight suite's tool-sweep table, path-property table (P1…P6, P5′), T060, T078, T081, T137, T143, T144 | **Partially covered** | 5 of its 14 MUST clauses are fully asserted. The layered graphics probe's **four failure layers** and its own `3` verdict, the crash/update-marker removal, the open-without-gating rule, the own-literal allowlist and the pinned order as a whole have **no assertion** |
| FR-014 (the run log and the lifecycle events) | TC-1, TC-15, TC-29, TC-30, TC-31, TC-36, TC-39, TC-41, TC-44; the CLI suite's T138 and T149 | **Partially covered** | 6 of its 9 MUST clauses are fully asserted. The `umask 077` and the **order of the stdout summary's own lines** (which is why TC-1's expected order needed correcting) have **no assertion** |
| FR-015 (`--help`, `--version`, `-h`) | TC-16, TC-18; the CLI suite's option-surface, no-abbreviation, `--opt=VALUE` and example-form properties | **Partially covered** | 6 of its 8 MUST clauses are fully asserted: the option rows, the Environment section, the run-log location, the run-ID format, the version, the 20 exit codes, `--version` and the `-h` alias. The **order** of the `--help` sections — and the "the table is the source, the `--help` text is a rendering of it" relation — has **no assertion** |
| FR-016 (deploy effectiveness) | TC-9, TC-26, TC-34, TC-38, TC-40, TC-43 (the rebuild's environment: the pinned NuGet cache and the `DOTNET_*` settings the children inherit); the pre-flight suite's T060 and T078 | **Partially covered** | 3 of its 7 MUST clauses are fully asserted. The exact build command line, the slash-free `rm -rf -- "$target"`, the re-canonicalised parent and the "either file missing" / "strictly older" digest branches have **no assertion** |
| FR-017 (the resolved mods directory) | TC-14, TC-20, TC-38; the CLI suite's relative-`--mods-path` property | **Partially covered** | 4 of its 6 MUST clauses are fully asserted. The "is not a directory" and "is not writable" halves of the pre-flight verification, and the `SMAPI_MODS_PATH` precedence, have **no assertion** |
| FR-004, SC-004, SC-005 | TC-28 (the `SKIPPED:` label only — the check itself is never run) | **Not tested (deferred)** | Deferred by FR-004: the automated script cannot load a save (SMAPI has no CLI save selection; the mod is purely reactive). The title screen is the expected state. TC-28 asserts only that the deferral is **labelled** `SKIPPED:` (exactly once, on stderr and in the run log) and that it does not change the verdict | Manual play-through; spec 005 / the deferred US2 scenarios |
| FR-008, SC-015 | TC-25, TC-37 | Covered | — |
| FR-013 (local-first: no CI integration, no `--json` mode) | the CLI suite's **option-surface property** — *indirectly* | **Guarded indirectly** | FR-013 forbids a machine-parseable output mode, and **no assertion states that `--json` is rejected** (CI integration is out of scope by definition, so there is nothing to assert). The option-surface property guards the clause **indirectly**: it asserts that the `Options:` section of `--help` carries **exactly one entry per row of the normative table and no option that is not in it**, so a `--json` mode would show up as an extra entry and fail that assertion. The clause therefore has **no direct assertion** | n/a — the guard is the option-surface property, in `scripts/tests/test-cli.sh` |
| SC-006 (this guide's own contract) | — | **Not covered** | SC-006 says: "These properties are verifiable by reading `quickstart.md` and are exercised by TC-1…TC-26." The first half is true; the second is not. **No suite reads `quickstart.md`**, so nothing asserts that this guide documents every exit code with its cause and remediation, that it lists every required host tool, that its Command-Line Options table is a verbatim copy of the spec's, or that its troubleshooting ladder maps every symptom to a check with an explicit expected result. The document is verified by reading it, not by an assertion | Read this file and diff it against `spec.md` and `scripts/smoke-validation.sh` |
| Exit code `17` | The log-gate property suite (`scripts/tests/test-logparse.sh`, the `no_header` fixture row) — **no numbered TC** | Partially covered | `17` is the log-based classification for "SMAPI aborted before mod resolution": the log carries the current run's sentinel but the mod-resolution header never appears. **No row of the TC-1…TC-26 table produces it** — TC-19 produces `14` (usage error) and TC-26 produces `12` (build timeout) — so the classification is asserted by the log-gate fixture table rather than by a numbered test case, and the leftover-crash-marker route is exercised by hand | The `no_header` log fixture, or a manual injection of `smapi-internal/StardewModdingAPI.crash.marker` |
| Exit code `129` | TC-17 | Covered | TC-17's **SIGHUP probe** delivers a SIGHUP to the running script and asserts a **genuine signal death** — `WIFSIGNALED` set, the caller observing `129` — the same mechanism TC-7 proves for SIGINT → `130`, with SIGTERM → `143` asserted in the same loop. The convergence work verified the same end to end with a raw-`waitpid` probe. | If a run ends `129` the script died on SIGHUP; the handler is the risk, not the code value — re-run the TC-17 signal probe (`scripts/tests/test-integration.sh`) and confirm the death is a signal death, not a plain exit carrying the same number |

#### Per-clause coverage (MUST sub-clauses)

The table above states the status per **requirement**; this one states it per **MUST sub-clause**, which is the granularity the spec itself uses (the bolded `(MUST)` headings of `spec.md` → *Functional Requirements* and the numbered items of FR-012). Every row names the assertion that covers the sub-clause, or the reason no assertion does.

- **Covered** — at least one assertion in `scripts/tests/` would fail if the sub-clause were violated.
- **Partially covered** — part of the sub-clause is asserted and part is not; the gap is named.
- **Not covered** — no assertion covers any part of it.

| FR | MUST sub-clause | Status | The assertion that covers it, or the reason none does |
|---|---|---|---|
| **FR-001** | The launch contract — the apphost `./StardewModdingAPI --no-terminal` from the game directory as the working directory, the seven exported variables, `DISPLAY` from the reported number, Xvfb with `-nolisten tcp -displayfd` | Covered | TC-1 reads all of it off the run log; the seven variables are also asserted by name in the CLI suite's option-surface property |
| **FR-002** | The log path's default and its `--smapi-log` override | Covered | the CLI suite's option-surface property (`--smapi-log` … `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`); every fixture case drives the script through `--smapi-log`; TC-1 exercises the default |
| | The exclusive, non-blocking `flock` before the launch; a contended lock exits `6` with a descriptive error | Covered | TC-5 |
| | **Lock file (MUST)** — a stable, per-user path outside any per-run `mktemp -d`; opened with `exec 9<>`; acquired with `flock -n 9`; never removed | **Partially covered** | the pre-flight suite's T108 asserts the file at the documented fallback path `${XDG_STATE_HOME}/livingroots-smoke/livingroots-smoke.lock` and that it **survives** the run. The `exec 9<>`-not-`exec 9>` form, the `flock -n 9` call and the `$XDG_RUNTIME_DIR` branch have **no assertion** |
| | **Lock file permissions (MUST)** — `0600` on the file, `0700` on the directory, enforced with `chmod` on the **open descriptor**; the `$XDG_RUNTIME_DIR` directory's mode MUST NOT be changed | **Partially covered** | T108 asserts the file's `0600` and the fallback directory's `0700`. The descriptor-based `chmod 0600 /proc/self/fd/9` (a path-based `chmod` could act on another inode) and the "MUST NOT change `$XDG_RUNTIME_DIR`" clause have **no assertion** |
| | **Every child launch closes the lock descriptor (`9<&-`)** | **Not covered** | nothing observes the descriptor; the suite can only observe the consequence (a later run not reporting `6`), and no case runs a second instance after a build that kept an MSBuild worker alive |
| | **Log freshness (MUST)** — `T0` recorded before the launch; the sentinel compared as **UTC** and not older than `T0`; every signal evaluated only after the sentinel; the stale log removed and its absence asserted | Covered | the log-gate suite's SENTINEL-FRESHNESS table (current → accepted, an hour old → stale, malformed → reported), its STALE-LINE property (a stale pre-sentinel sentinel and header can neither satisfy phase 1 nor end phase 2) and its INODE-GUARD table (the "survived" row → `13`); TC-10; TC-41 (the run log carries the launch epoch the comparison used) |
| | **Header region (MUST)** — the banner and the `Mods go here:` line are context only; the path comparison is `~`-anonymised and canonicalised on both sides | Covered | TC-14 |
| | **Pre-launch cleanup (MUST)** — `SMAPI-latest.txt`, `SMAPI-crash.txt` **and both SMAPI markers** removed, every removal logged with the update marker's content | **Partially covered** | the stale **log**'s removal and its absence are asserted (the inode-guard table). The removal of `SMAPI-crash.txt` and of `StardewModdingAPI.crash.marker` / `…update.marker`, and the logging of the update marker's content, have **no assertion** — no fixture ever creates a marker |
| | **Readiness vs. exit (MUST)** — a game that exits without producing a log, even with status `0`, is `15` | Covered | TC-12 |
| | **Two-phase wait (MUST)** — phase 1 is the sentinel alone; phase 2 is the mod-resolution header within the same deadline; a missing header is `4`; a sentinel without a header is `17` | Covered | TC-6 (`4`), TC-34 (the deadline is anchored at the launch), the log-gate suite's `no_header` row (`17`) and its `mod_loaded` / `zero_mods` rows |
| **FR-003** | **Record shape (MUST)** — one prefixed physical line plus continuation lines; the prefix is applied once to the whole message | Covered | the log-gate suite's INDENTATION property and its `continuation_lines` row |
| | **The level token is variable width (MUST)** — bound by label, never by column; `CRITICAL` shifts the source column | Covered | the `critical_level` row (exit `0`, the level still bound by label and the source column not shifted) |
| | **Source (MUST)** — the remainder of the bracket group, may contain spaces; `SMAPI`/`game` records are outside the mod-scoped gate | Covered | the `critical_level` fixture carries a `CRITICAL` record from source `SMAPI` and the run still exits `0`; the `mod_skipped` and `mod_absent` rows bind the source label |
| | **Continuation lines and line endings (MUST)** — a line without a prefix is a continuation and never a new record; the CR is stripped before matching; no `$`-anchored pattern on the raw file | Covered | the log-gate suite's CRLF property and its `crlf_only` and `continuation_lines` rows |
| | **Normalised record stream (MUST)** — `<level>\t<source>\t<text>`, one line per physical line, empty level and source for continuations | **Partially covered** | the stream's *behaviour* is asserted by every row of the gate table (the `crlf_only`, `continuation_lines` and `critical_level` rows could not pass without it), but the stream's exact tab-separated shape is never read by an assertion |
| | **Failure markers (MUST)** — only `ERROR`/`ALERT` records whose source equals the mod's manifest name; `WARN` never fails; `TRACE`/`DEBUG` are diagnostic | Covered | the `mod_alert` (→ `5`), `mod_warn` (→ `0`) and `mod_trace_only` (→ `0`) rows |
| | **Trace diagnostics (MUST)** — the mod's `TRACE` records are diagnostic only, and the exception type and HResult come from them | **Partially covered** | the `mod_trace_after_error` row (the Trace record is part of the reported context) and the `mod_trace_only` row. The "case-sensitive and exact" marker match and "per-line timestamps carry no date and MUST NOT be used to identify a run" have **no assertion** |
| | **The success gate (MUST)** — leg 1 (the loaded-mods block, `Loaded 0 mods.` → `5`), leg 2 (the skipped-mods section, the ` because ` suffix, the reason verbatim), leg 3 (no mod-scoped `ERROR`/`ALERT`) | **Partially covered** | the `mod_loaded`, `zero_mods` (with `13` as the forbidden code), `mod_skipped`, `mod_absent` and `mod_alert` rows. The `Loaded <N> content packs:` block MUST NOT be confused with the mods block, and the "the manifest name matched as a substring followed by a space or the end of the line" rule, have **no assertion** — no fixture carries either shape |
| | **The manifest name MUST NOT be searched across the whole log** (the false-pass guard) | Covered | the log-gate suite's FALSE-PASS GUARD property (a log whose only occurrence of the name is inside the skipped-mods entry still yields `5`) and the `stale_marker` row |
| **FR-005** | The game is terminated immediately after the log check passes, leaving no orphan | Covered | TC-32 (the verdict is printed only after the cleanup), TC-27, TC-31 |
| | Each child — the game **and** Xvfb — gets its own process group; SIGTERM to the group, the grace period, then SIGKILL | Covered | TC-27 (each child's recorded group equals its PID), TC-31 (the SIGTERM, the grace period and the SIGKILL escalation are logged with their target) |
| | **`set -m` is enabled immediately before each child launch and disabled immediately afterwards** | **Not covered** | a source-level property: no assertion would fail if job control were left on for the whole script |
| | The mandatory PGID assertion before **every** group signal, for **both** children; a failed assertion aborts with `13` and never falls back to a single-PID signal | **Partially covered** | the assertion never fires in any test — the script's own group is never signalled, which every fixture run exercises — but the failure branch has **no assertion** |
| | Both children are launched with `&`, stdin from `/dev/null`, stdout and stderr to the run log, and waited on with `wait -f "$pid"` | **Partially covered** | TC-30 and TC-44 observe the children's output reaching the run log; the `wait -f "$pid"` form is a source-level property with **no assertion** |
| | Every child launch closes the lock descriptor (`9<&-`) | **Not covered** | as under FR-002 |
| | **Trap design (MUST)** — one `EXIT` trap, registered once, as the single cleanup path; the cleanup captures `$?` first and ends with `exit "$rc"`; the `INT`/`TERM`/`HUP` handlers forward to the child groups, reset their own trap and re-raise; `trap '' INT TERM` MUST NOT be used; the cleanup is idempotent and time-bounded | **Partially covered** | TC-7 and TC-17's `SIGTERM`/`SIGHUP` probes prove the reset-and-re-raise path and the genuine `128+N` signal deaths, and TC-32 proves the cleanup runs before the verdict. The single registration, the `$?` capture, the `in_cleanup` guard, the time bound and the ban on `trap '' INT TERM` have **no assertion** |
| | The script does **not** wait for the `Saved` event | **Not covered** | a negative clause: no assertion would fail if the script began waiting for it |
| **FR-006** | Exit `0` when the gate passes — the AND of the three legs, over the normalised stream, over the post-sentinel records only | Covered | the gate table as a whole (a fixture that violates any one leg fails), TC-1 |
| | The mod's own success marker is reported as a secondary signal and does not gate the verdict | Covered | the `mod_success_marker` row (the `mod-success-marker:` line on stdout, exit `0`), the `stale_marker` row (a marker before the sentinel does not gate), TC-42 |
| | The deferred save check does not gate success | Covered | TC-28 (`SKIPPED:` labelled exactly once; the verdict unchanged) |
| | The success message is exactly `SUCCESS: Mod loaded without errors.` | Covered | the `mod_loaded` row, TC-1, TC-18 |
| **FR-007** | The "What → Where → How to fix" pattern with a trailing `Fix:` line | Covered | TC-2, TC-3, TC-4, TC-5, TC-6, TC-9, TC-12, TC-20, TC-21, TC-22, TC-23, TC-26 — every failure case asserts a `Fix:` line |
| | **Log context (MUST)** — the matched failure record with its continuation lines, including the mod's Trace records; no fixed `tail -N`; the trailing records capped at 30 when no record matched; emitted from the normalised stream | Covered | the `continuation_lines` row, the `mod_trace_after_error` row, the `mod_absent` row (the Context section) and the `mod_absent_long_tail` row (the 30-record cap), plus the log-gate suite's CRASH-CONTEXT property |
| | A distinct exit code per failure category — the whole FR-007 taxonomy | Covered | TC-17 (the translation table), the per-case exits (`1`, `2`, `3`, `4`, `5`, `6`, `7`, `9`, `10`, `11`, `12`, `13`, `14`, `15`, `16`, `17`) and the CLI suite's `--help` exit-code table |
| | **Allocation rule (MUST)** — codes are allocated additively and permanently; a published code MUST NOT be re-assigned; the retired `8` MUST NOT be reused | **Not covered** | nothing asserts that the script never emits `8`; the CLI suite's exit-code loop only checks that the 20 published codes are **documented**, and it omits `8` from that list without asserting its absence |
| | No subprocess-specific status may ever become the script's final code; the whole translation table | Covered | TC-17 (every row, `137` in all three contexts, `0` → `0`) |
| | Log-based classification takes precedence over the status translation (`17` for an abort before mod resolution, `3` for a graphics-device failure) | Covered | the log-gate suite's `no_header` row (`17`) and `graphics_device` row (`3`, with `17` as the forbidden code) |
| **FR-009** | A configurable `--timeout` with a 120-second default | Covered | the CLI suite's option-surface property; TC-6, TC-34 |
| | The timeout is measured from the launch instant, bounds the whole two-phase wait, and does not charge the pre-flight or the rebuild | Covered | TC-6, TC-34 (a `--timeout` shorter than the rebuild still reaches the launch and exits `4`) |
| | The timeout is not a whole-script deadline and a phase overrun is not reported as a timeout | Covered | TC-34, TC-39, TC-26 (the build bound yields `12`, not `4`) |
| | The deadline poll loop; the `timeout` utility MUST NOT wrap the game launch | **Partially covered** | the observable behaviour is asserted; the source-level form — `deadline=$(( $(date +%s) + timeout ))`, the check before each iteration, and the launch staying a direct background child — is **not** |
| **FR-010** | The game group and the Xvfb group are killed by group signal, each with the mandatory PGID assertion; a failed assertion aborts with `13` | **Partially covered** | TC-27, TC-31 and TC-35 cover the outcome; the assertion's failure branch has **no assertion** |
| | The allocated display's socket is removed, and only that one; `/tmp/.X{n}-lock` files are never created or removed | **Partially covered** | TC-35 observes the socket's removal; the lock-file prohibition has **no assertion** |
| | The temp files and directories are removed | Covered | TC-35 (the run's temporary directory does not survive) |
| | The lock file is deliberately **not** removed | Covered | the pre-flight suite's T108, TC-5, TC-37 |
| | Each cleanup step is isolated (`\|\| true`) so one failure cannot prevent the others | **Not covered** | a source-level property: no test makes one cleanup step fail and checks that the others still ran |
| | The cleanup order is the reverse of the allocation | **Not covered** | the order is never observed |
| **FR-011** | An existing Stardew Valley/SMAPI process is detected and, by default, aborts the run with a descriptive error listing the conflicting PIDs | Covered | TC-4 (exit `7`, the PID listed, the process untouched) |
| | `--force` kills the existing instance (SIGTERM → SIGKILL) and the run proceeds to the launch | Covered | TC-4 |
| | The signal targets the **identified PID only**, never a group the script did not create | **Partially covered** | by construction — the fixture's process shares the suite's own process group, so a group signal would kill the suite — but no assertion states it |
| | The victim's PID, executable path and start time are recorded in the run log **before** the first signal | **Not covered** | the script records them, but no assertion reads them out of the run log |
| | Identification is by executable path, on the canonicalised game directory; `pgrep -x` MUST NOT be used; every PID found by scanning the process table MUST be verified before it is signalled | **Partially covered** | TC-4's fixture is identified through `/proc/<pid>/exe` (the kernel's `comm` is truncated to 15 characters, so a name match could not have found it). The "every scanned PID MUST be verified before it is signalled" clause has **no assertion** |
| **FR-012** | **(0) Order (MUST)** — the tool sweep → game/SMAPI → Steam → marker removal → display and probe; then the build, the flock, the process check, the log baseline, the launch | **Partially covered** | the path-property accept rows reach the display phase (so the path checks precede it), TC-20 shows the mods-path check precedes the build, and T108's "lock acquired" line shows the lock follows the build. The order **as a whole**, as a single assertion, has **none** |
| | **(1) The game installation** exists and carries `Stardew Valley.dll` and `StardewModdingAPI` | Covered | TC-3 |
| | **(2) SMAPI is installed and functional** — the main assembly and its parseable version, the two dependency assemblies, an executable apphost | **Partially covered** | TC-21 asserts exit `2` and that the missing `SMAPI.Toolkit.CoreInterfaces.dll` is named. The "the apphost is executable for the host architecture" and "the main assembly's version can be parsed" halves have **no assertion** |
| | **(3) Xvfb is available and runnable** — an absent binary is a missing tool (`9`), not `3` | Covered | TC-8 (`9`, the tool and its package named) and TC-23 (a present-but-failing Xvfb is `10`, not `9` and not `3`) |
| | **(4) The layered software-OpenGL probe** (4a `GLX`/`RANDR`, 4b `glxinfo -B` exits `0`, 4c `llvmpipe`/`softpipe`, 4d an FBO extension or GL ≥ 3.0); a failure with all three tools present is `3` | **Partially covered** | the pre-flight suite stubs `xdpyinfo` and `glxinfo` with **passing** output, so only the happy path runs. No fixture drives 4a–4d to fail, and the probe's own `3` verdict is asserted nowhere — TC-13 says so explicitly. The `3` verdict is asserted only on the **log** path, by the log-gate suite's `graphics_device` row |
| | **(5) The display is allocated by the server** (`-displayfd`); the script must not choose a number, and must not probe, create or remove `/tmp/.X{n}-lock` files; the number is read back and recorded; a failure is `10` | **Partially covered** | TC-1 observes the `-displayfd` flag in the run log, TC-23 gives `10`, TC-35 observes the socket of the allocated display being removed. The "MUST NOT choose a display number" and the "no `/tmp/.X{n}-lock` file" halves have **no assertion** |
| | **(6) Crash- and update-marker removal**, each removal logged with the update marker's content; the SMAPI log directory's writability verified | **Not covered** | no fixture creates a marker and observes it removed and logged, and no case makes the log directory unwritable |
| | **(7) Every constructed path is canonicalised** (`realpath -e` / `-m`) and satisfies the component-boundary prefix test; the base is canonicalised and carries no trailing slash; a negated glob class only | **Partially covered** | the pre-flight suite's path-property table (P1…P6 and P5′) asserts the component-boundary decision for a genuine child, a shared-prefix sibling, a trailing-slash base, a `..` segment, a symlink escape, a path with a space and a constructed path that escapes the base; T060 asserts the `--mod-project` containment. The negated-glob-class rule has **no assertion** |
| | **(7) Open-without-gating** — no existence, type or permission test may decide whether an operation on that path is attempted | **Not covered** | a source-level rule with no assertion: no test observes that a removal or a read was attempted regardless of a pre-check |
| | **(7) The script's own literals match `[A-Za-z0-9._-]+`; no character allowlist applies to environment-derived names** | **Partially covered** | the path-property table's P6 row shows a path with a space is accepted, so the "no allowlist on environment-derived names" half holds. The own-literal allowlist has **no assertion** |
| | **(7) Every path is expanded as `"$var"` and passed after `--`; no Unicode normalization is performed** | **Partially covered** | the pre-flight suite's T078 scans the script's own source and asserts the `--` terminator for thirteen call sites. "No Unicode normalization" is a negative clause with **no assertion** |
| | **(8) Every required host tool is present**, verified with `command -v`; a missing one fails closed with `9` and names the tool; a present-but-unusable tool is a different category | Covered | TC-8; the pre-flight suite's sweep table, its T137 package assertion and its T143 `pgrep`-or-`pidof` rule |
| | **(9) The Steam prerequisite** — `steam_appid.txt` exists and holds `413150`; otherwise `11` | Covered | TC-22 (the file, the expected id and the observed value are named) |
| | **(10) Orphaned displays are detected, reported and never signalled** | Covered | the pre-flight suite's T081 (the PID, the executable path, the start time and the owning UID are reported; the double is still alive afterwards) |
| **FR-014** | Every lifecycle event is logged with a timestamp and a level prefix — the spawn, every signal with its PID and its name, the grace period, the SIGKILL escalation, the observed exit | Covered | TC-27 (the spawn lines carry the PID and the group), TC-31 (the SIGTERM, the grace period, the SIGKILL escalation and the observed exit), TC-44 (a child's last line reaches the log) |
| | The ERR trap reports the script name, the failing line number, the enclosing function name and the exit code | Covered | TC-24; the CLI suite's T138 and T149 (exactly one report; the run's exit code is the report's own `13`) |
| | **Shell options (MUST)** — `set -Eeuo pipefail`, with `-E` load-bearing | **Partially covered** | TC-24's "the ERR trap reports the enclosing function" fails if `-E` is dropped, so the load-bearing half is asserted; the rest of the line is a source-level property with **no assertion** |
| | **Umask (MUST)** — `umask 077` before any file or directory is created | **Not covered** | the artifacts' *modes* are asserted (the run log's, the directories', the lock file's), but the umask itself is not |
| | **Run log (MUST)** — the documented path, a `0700` directory, one file per run, and every line the script emits | Covered | TC-15, TC-30, TC-36 |
| | **Run log permissions (MUST)** — `0600` on the file, `0700` on **every** directory level below the XDG base | Covered | TC-36 |
| | **Phase timings (MUST)** — the log records each phase's start, end and elapsed seconds; the stdout summary prints the total wall time **and** each phase's duration | **Partially covered** | TC-29 (every phase's duration is on stdout and agrees with the log's) and TC-39 (the failing phase appears in the block and in the log with its end timestamp and elapsed seconds). The **order of the stdout summary's own lines** is not asserted — the gap that let TC-1's expected order drift from the shipped one |
| | **Run ID and version (MUST)** — the `<UTC timestamp>-<PID>` form, the stamping of every run-log line, the `VERSION` in the summary, the header and `--help` | Covered | TC-15, TC-18, TC-30, TC-36 |
| | **Streams (MUST)** — the verdict and the `--help`/`--version` output on stdout; the diagnostics on stderr | Covered | TC-15, TC-18, TC-32 |
| **FR-015** | The `Usage:` line with every option | Covered | the CLI suite's option-surface property |
| | The `Options:` section — exactly one entry per row of the normative table, the same spelling, placeholder and default, and **no option that is not in that table** | Covered | the CLI suite's option-surface property (the entry count and the "only options from the normative table" loop) |
| | The `Environment (set automatically)` section naming the eight variables | **Partially covered** | the eight names are asserted; "stating explicitly that they are managed by the script and are not user-configurable" has **no assertion** |
| | The run-log location | Covered | the CLI suite's option-surface property |
| | The run-ID format | Covered | the CLI suite's option-surface property (the format is matched as a regex) |
| | The `VERSION` | Covered | the CLI suite's option-surface property; TC-18 |
| | Every exit code, including `17`; `--version`; the `-h` alias | Covered | the CLI suite's exit-code table; TC-18 (the `--version` output and the byte-identical `-h`) |
| | The order of the sections, and the "the table is the source, the `--help` text is a rendering of it" relation | **Not covered** | a divergence between the spec's table and the `--help` text would be detectable only by reading the two; no assertion diffs them |
| **FR-016** | **(a) The project is configurable and defaults to `<repo-root>/LivingRoots/LivingRoots.csproj`**, derived from the script's own location | Covered | the pre-flight suite's T060 ("default" → accepted) |
| | **(a) The path is resolved with `realpath` and prefix-tested against the canonicalised repository root**, expanded as `"$var"` and passed after `--` | Covered | T060 (a project outside the repository is rejected, a `..` path is accepted, a missing project is rejected); T078 (the `--` terminator at the `dotnet`, `dirname` and `basename` call sites) |
| | **(b) `dotnet build --configuration Release --no-incremental`, in the project directory** | **Not covered** | no assertion reads the build command line |
| | **(b) The deploy target is removed before the build**, with `rm -rf -- "$target"` and no trailing slash, and the target's parent is re-canonicalised immediately before the removal | **Partially covered** | TC-40 asserts that a target which cannot be removed aborts with `12` and names the target and the removal's error, and TC-38 that the declared `<ModFolderName>` is the one removed. The slash-free form and the re-canonicalisation are source-level properties with **no assertion** |
| | **(b) The build is bounded by `--build-timeout`** (default 300), expiry is `12`, and the bound is independent of `--timeout` | **Partially covered** | TC-26 (exit `12`, the game never launched) and TC-34 (the independence). "a message naming the elapsed time" is **not** asserted — the report names the bound that expired |
| | **(c) The deploy target is pinned** with `-p:GameModsPath="<resolved mods path>"` | Covered | TC-38 (the deploy lands in the declared folder under the resolved mods path), TC-14 |
| | **(d) The digest comparison** — the built and the deployed SHA-256; a mismatch, a missing file or a strictly older file aborts with `12`, naming both digests | **Partially covered** | TC-9 (the mismatch → `12`, with **both** digests named) and TC-38 (the comparison runs against the declared folder). The "either file is missing" and the "strictly older" branches have **no assertion**, although SC-012 names them |
| **FR-017** | The mods directory is resolved explicitly and never relies on SMAPI's implicit default; the default is `<game-path>/Mods` | Covered | TC-14, TC-20 |
| | The resolved directory is passed to SMAPI; the CLI argument is normative and takes precedence over `SMAPI_MODS_PATH` | **Partially covered** | TC-14 observes the resolved directory in SMAPI's own `Mods go here:` line. The `SMAPI_MODS_PATH` equivalence and the precedence have **no assertion** |
| | A relative `--mods-path` is resolved against the game directory; an absolute one is accepted as-is and is its own base | Covered | the CLI suite's relative-`--mods-path` property (T068); the pre-flight suite's path-property table |
| | A user-supplied `--mods-path` may contain spaces and is never rejected on character grounds | Covered | the path-property table's P6 row |
| | The resolved directory is passed to the build as `-p:GameModsPath` | Covered | TC-38 |
| | **Verification of the mods directory (MUST, pre-flight)** — an existing directory that is writable; otherwise `16`, before the build | **Partially covered** | TC-20 (exit `16` for a path that does not exist, before the build, with the deploy target untouched and nothing launched). The "is not a directory" and the "is not writable" halves have **no assertion** |
| **FR-008** | Two consecutive runs under the same conditions produce the same exit code and the same verdict | Covered | TC-25 (the real installation) and TC-37 (a fixture run) |
| **FR-013** | The script stays local-first: no CI integration and no machine-parseable (`--json`) output mode | **Guarded indirectly** | there is **no direct assertion** that `--json` is rejected, and CI integration is out of scope by definition. The CLI suite's **option-surface property** guards the clause indirectly: it asserts that the `Options:` section of `--help` carries exactly one entry per row of the normative table and no option that is not in it, so a `--json` mode would appear as an extra entry and fail that assertion. The prohibition is therefore enforced by a *side-effect* of another requirement's property, not by a property of its own |
| **SC-006** | The guide documents every exit code with its cause and its remediation, lists every required host tool, carries a verbatim copy of the Command-Line Options table, and maps every symptom to a check with an explicit expected result | **Not covered** | **no suite reads `quickstart.md`** — the guide's own contract is verified by reading it, not by an assertion. The suites assert the *script's* behaviour, never the document's |

**Execution order**: TC-1 → TC-2 → … → TC-44 (29119-3 §8.4.6 *Ordered test cases*). The numbered table above carries TC-1…TC-26; the suite has since grown to **TC-44** — TC-27…TC-33 are the convergence properties (they share one fixture run), TC-34…TC-44 the fixture-project cases — and the coverage table below names the ones that cover each requirement. A failure in one case does **not** skip the independent cases that follow; TC-1 is always run first because it is the canary for the environment. **TC-18…TC-24, TC-26 and TC-33 are launch-free** — they use fixtures and a stubbed `PATH` and need neither the game nor the display, so they can be executed *before* `Xvfb` is installed; TC-25 runs immediately after TC-1.

### Scenario 1: Mod Loads Successfully

**Given**: Game installed, SMAPI installed, mod deployed correctly, no save file
**When**: Script runs
**Then**: Exit code `0`, `SUCCESS` message, game terminated, no orphaned processes

The **verdict, the summary and the phase timings go to stdout**; the run log carries the same phase timings (each phase's start, end and elapsed seconds), the total wall time and the launch contract.

```bash
$ ./scripts/smoke-validation.sh
SUCCESS: Mod loaded without errors.
mod-success-marker: The 'Stardew Valley - Living Roots' mod was loaded successfully!
run-id: 20261020T214512Z-48213
version: smoke-validation.sh 0.1.0
run-log: /home/<user>/.local/state/living-roots-smoke/runs/20261020T214512Z-48213.log
total wall time: 63s
phase durations:
  pre-flight             1s
  display                3s
  rebuild-and-verify     8s
  lock                   0s
  process-conflict-check 3s
  log-baseline           0s
  launch                 0s
  readiness wait         45s
  verdict                0s
  termination            1s
  cleanup                1s
$ echo $?
0
```

> The `mod-success-marker:` line is **conditional**: it is printed only when the mod's own success marker was detected in the SMAPI log. When the marker is absent, that line is simply not printed and the other lines are unchanged.

> The `phase durations:` block is printed on **stdout as well as into the run log** (FR-014). The block covers every phase of the run, **including the phase the run failed in** — a failing run prints the same block, with the failed phase closed and included.

### Scenario 2: Mod Has Load-Time Error

**Given**: Game installed, mod has a runtime error during load
**When**: Script runs
**Then**: Exit code `5`, descriptive error message with SMAPI log context

The failure report is built **once** and emitted **identically** to stderr and
to the run log: every line carries the timestamp, the level and the run ID, and
the `Fix:` line is the **trailing** line (FR-007, FR-014).

```bash
$ ./scripts/smoke-validation.sh
# --- stderr (the same lines are written to the run log) ---
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213] ERROR: smoke validation failed (exit 5).
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   What: the mod did not load cleanly
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Where: /home/<user>/.config/StardewValley/ErrorLogs/SMAPI-latest.txt — Stardew Valley - Living Roots threw an exception: System.NullReferenceException
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Context:
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]     ERROR	Stardew Valley - Living Roots	Stardew Valley - Living Roots threw an exception: System.NullReferenceException
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]        at LivingRoots.ModEntry.Entry() — Object reference not set to an instance of an object.
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]     TRACE	Stardew Valley - Living Roots	HResult: 80004005 (System.NullReferenceException)
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Fix: read the failure context above; the SMAPI log has the detail
# --- stdout ---
FAILURE: the run failed (exit 5).
run-id: 20261020T214512Z-48213
version: smoke-validation.sh 0.1.0
run-log: /home/<user>/.local/state/living-roots-smoke/runs/20261020T214512Z-48213.log
total wall time: 61s
phase durations:
  pre-flight             1s
  display                3s
  rebuild-and-verify     8s
  lock                   0s
  process-conflict-check 3s
  log-baseline           0s
  launch                 0s
  readiness wait         45s
  verdict                0s
  cleanup                0s
$ echo $?
5
```

> The `Context:` block holds the **matched failure record together with its
> continuation lines** — including the mod's own `TRACE` records, which carry
> the exception type and the HResult. The records are tab-separated
> (`<level>\t<source>\t<text>`); a continuation line carries no prefix and is
> printed verbatim.

### Scenario 8: Deploy Ineffective

**Given**: The build cannot replace the assembly deployed into the resolved mods path
**When**: Script runs
**Then**: Exit `12` **before** the game is launched, naming both digests

```bash
$ ./scripts/smoke-validation.sh
# --- stderr (the same lines are written to the run log) ---
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213] ERROR: smoke validation failed (exit 12).
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   What: the deployed assembly is not the one this run built
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Where: built: /home/<user>/development/stardew-raizes-vivas/LivingRoots/bin/LivingRoots.dll (568164bf32f768f4…) / deployed: /home/<user>/.steam/steam/steamapps/common/Stardew Valley/Mods/LivingRoots/LivingRoots.dll (7a2e71c92d7201fb…)
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Fix: the deploy did not take effect — check that the resolved mods path is writable and that the deploy target is pinned to it
# --- stdout ---
FAILURE: the run failed (exit 12).
run-id: 20261020T214512Z-48213
version: smoke-validation.sh 0.1.0
run-log: /home/<user>/.local/state/living-roots-smoke/runs/20261020T214512Z-48213.log
total wall time: 12s
phase durations:
  pre-flight             1s
  display                3s
  rebuild-and-verify     8s
  cleanup                0s
$ echo $?
12
```

### Scenario 3: Game Not Installed

**Given**: Game not found at expected path
**When**: Script runs
**Then**: Exit code `1`, actionable pre-flight error

```bash
$ ./scripts/smoke-validation.sh
# --- stderr (the same lines are written to the run log) ---
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213] ERROR: smoke validation failed (exit 1).
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   What: the game directory does not exist
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Where: /home/<user>/.steam/steam/steamapps/common/Stardew Valley
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Fix: pass --game-path <dir> pointing at the Stardew Valley installation
# --- stdout ---
FAILURE: the run failed (exit 1).
run-id: 20261020T214512Z-48213
version: smoke-validation.sh 0.1.0
run-log: /home/<user>/.local/state/living-roots-smoke/runs/20261020T214512Z-48213.log
total wall time: 0s
phase durations:
  pre-flight             0s
  cleanup                0s
$ echo $?
1
```

### Scenario 4: Game Already Running

**Given**: Stardew Valley/SMAPI process already running
**When**: Script runs (without `--force`)
**Then**: Exit code `7`, lists conflicting PIDs

```bash
$ ./scripts/smoke-validation.sh
# --- stderr (the same lines are written to the run log) ---
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213] ERROR: smoke validation failed (exit 7).
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   What: a Stardew Valley/SMAPI process is already running
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Where: 12345 12346
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Fix: close the running game, or re-run with --force
# --- stdout ---
FAILURE: the run failed (exit 7).
run-id: 20261020T214512Z-48213
version: smoke-validation.sh 0.1.0
run-log: /home/<user>/.local/state/living-roots-smoke/runs/20261020T214512Z-48213.log
total wall time: 14s
phase durations:
  pre-flight             1s
  display                3s
  rebuild-and-verify     8s
  lock                   0s
  process-conflict-check 2s
  cleanup                0s
$ echo $?
7
```

> The `Where:` line lists the conflicting **PIDs** — the run finds them by
> scanning `/proc/<pid>/exe` against the canonicalised game directory, never
> by process name (the kernel truncates `comm` to 15 characters, so an exact
> name match on the 17-character apphost can never succeed).

### Scenario 5: Concurrent Execution Prevention

**Given**: Another smoke test instance is already running
**When**: Script runs
**Then**: Exit code `6`, lock contention error — the flock is taken **after** the rebuild-and-verify phase (FR-012 (0)), so the second instance reaches the contention only after its own rebuild has finished

```bash
$ ./scripts/smoke-validation.sh
# --- stderr (the same lines are written to the run log) ---
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213] ERROR: smoke validation failed (exit 6).
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   What: another smoke-validation instance already holds the lock
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Where: /run/user/1000/livingroots-smoke.lock
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Fix: wait for the other instance to finish (the kernel releases the lock when it exits, even after a SIGKILL)
# --- stdout ---
FAILURE: the run failed (exit 6).
run-id: 20261020T214512Z-48213
version: smoke-validation.sh 0.1.0
run-log: /home/<user>/.local/state/living-roots-smoke/runs/20261020T214512Z-48213.log
total wall time: 12s
phase durations:
  pre-flight             1s
  display                3s
  rebuild-and-verify     8s
  lock                   0s
  cleanup                0s
$ echo $?
6
```

### Scenario 6: Timeout

**Given**: Game takes longer than timeout to produce SMAPI log
**When**: Script runs with `--timeout 30` and game doesn't produce log in 30s
**Then**: Exit code `4`, timeout error

```bash
$ ./scripts/smoke-validation.sh --timeout 30
# --- stderr (the same lines are written to the run log) ---
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213] ERROR: smoke validation failed (exit 4).
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   What: no SMAPI run sentinel appeared within 30s of the launch
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Where: /home/<user>/.config/StardewValley/ErrorLogs/SMAPI-latest.txt
2026-10-20T21:45:12Z [ERROR] [20261020T214512Z-48213]   Fix: raise --timeout, or check the run log for why SMAPI did not start
# --- stdout ---
FAILURE: the run failed (exit 4).
run-id: 20261020T214512Z-48213
version: smoke-validation.sh 0.1.0
run-log: /home/<user>/.local/state/living-roots-smoke/runs/20261020T214512Z-48213.log
total wall time: 47s
phase durations:
  pre-flight             1s
  display                3s
  rebuild-and-verify     8s
  lock                   0s
  process-conflict-check 2s
  log-baseline           0s
  launch                 0s
  readiness wait         30s
  cleanup                0s
$ echo $?
4
```

> The timeout is measured **from the launch instant**, so the pre-flight, the
> display allocation and the rebuild are not charged against it (FR-009).

### Scenario 7: Interruption (Ctrl+C)

**Given**: Script is running and user presses Ctrl+C
**When**: SIGINT received
**Then**: EXIT trap runs cleanup (game killed, Xvfb killed, the socket of the display this script allocated removed), exit code `130`

```bash
$ ./scripts/smoke-validation.sh
^C
$ echo $?
130
```

> An interrupted run emits **no failure report**: the signal is forwarded to the
> child process groups, the handler resets its own trap and re-raises, so the
> script dies **from the signal** and the caller observes the genuine `128+N`
> status (`130` for SIGINT) rather than a plain exit carrying the same number.
> The cleanup and the forwarded signal are recorded in the run log, not on
> stderr.

---

## Troubleshooting

### Diagnostic ladder

Two tiers. **Tier 1 (D-1…D-8)** is the environment sweep — cheap, argument-free checks that settle the setup questions before any code-specific triage. **Tier 2 (D-9…D-25)** is the exit-code ladder: **one step per published failure code** (the three POSIX interruption codes share D-25), **ordered by decreasing likelihood** with **cost as the tiebreak** (Google SRE Ch. 12: "perform the tests in decreasing order of likelihood"; Cisco *IT Essentials* 4.2: "starting with the quickest and easiest"). The most likely class — `5`, a mod load failure — is the first Tier-2 step.

Every step states the **exact command** to run, its **expected result**, the **branch** when it does not hold, the **remediation**, and the **expected result once it is fixed**. Start at D-1 and go down; the first step whose expected result does not hold is your diagnosis.

Paths below are the script's defaults (`~/.steam/steam/steamapps/common/Stardew Valley`, `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`); substitute the values your run actually used — the stdout summary prints the resolved `game-path`, `mods-path`, `smapi-log` and `run-log` paths, and the run log is `${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log` (`<run-id>` is the `run-id:` line of that summary). Where the script produces the remediation itself, the step quotes the script's own `Fix:` line verbatim.

| Step | Check | Expected result if OK | If NOT ok → | Remediation | Expected result once fixed |
|------|-------|----------------------|-------------|-------------|---------------------------|
| D-1 | `command -v Xvfb xdpyinfo glxinfo flock timeout mktemp stat realpath sha256sum date grep dotnet ps readlink sed awk cut head tr cp cat sort touch mkdir chmod rm dirname basename tail sleep mkfifo` | every path printed | D-2 | install the named package (`xorg-x11-server-Xvfb`, `mesa-utils`, `util-linux`, `procps-ng`, `coreutils`, `grep`, `gawk`, `dotnet-sdk`) | the tool is on `PATH`; the run reaches the next step |
| D-2 | `ls -l ~/.steam/steam/steamapps/common/"Stardew Valley"/StardewModdingAPI{,.dll} ~/.steam/steam/steamapps/common/"Stardew Valley"/smapi-internal/SMAPI.Toolkit*.dll` | all four files exist | D-3 | re-run the SMAPI installer in the game directory | the markers exist; the run reaches the next step |
| D-3 | `cat ~/.steam/steam/steamapps/common/"Stardew Valley"/steam_appid.txt` | `413150` | D-4 | restore `steam_appid.txt` from a Steam install | the file contains `413150` |
| D-4 | `pidof StardewValley; pidof StardewModdingAPI` | both return nothing | D-5 | close the game, or re-run with `--force` | no game process remains |
| D-5 | `pgrep -a Xvfb` | no orphan Xvfb holds a display | D-6 | kill the orphan Xvfb manually (the script allocates its own display via `-displayfd`, so a stale lock is harmless and the pre-flight only reports orphans) | no orphan Xvfb remains |
| D-6 | `LIBGL_ALWAYS_SOFTWARE=1 glxinfo -B \| grep -E 'llvmpipe\|softpipe'` | `OpenGL renderer string: llvmpipe` | D-7 | install Mesa's software rasteriser (`mesa-dri-drivers`); confirm `GALLIUM_DRIVER=llvmpipe` is exported | the renderer string reports a software rasteriser |
| D-7 | `glxinfo \| grep -E 'GL_ARB_framebuffer_object\|GL_EXT_framebuffer_object'` | one extension is listed | D-8 | update Mesa — the game's MonoGame backend requires FBO support | the extension is listed |
| D-8 | `tail -30 ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` | the current run's `Loaded N mods:` block is present | D-9…D-25 — see the exit-code ladder | see D-9…D-25 by the exit code | the mod appears in the block |

**Tier 2 — exit-code ladder.** One step per published failure code, ordered by decreasing likelihood with cost as the tiebreak. Start at D-9: the row whose exit code matches the run's verdict is your diagnosis.

| Step | Check | Expected result if OK | If NOT ok → | Remediation | Expected result once fixed |
|------|-------|----------------------|-------------|-------------|---------------------------|
| D-9 | exit code `5` — `sed 's/\r$//' ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt \| grep -nE 'Loaded [0-9]+ mods[.:]\|Skipped mods\|Stardew Valley - Living Roots'` | the `Loaded N mods:` header is present **and** the entry after it names `Stardew Valley - Living Roots`; the mod does **not** appear under `   Skipped mods`; and no `[ERROR … Stardew Valley - Living Roots]` record follows the `Log started at` sentinel | the mod was **skipped** by SMAPI (the report carries the skip reason verbatim); or a mod-scoped `ERROR`/`ALERT` record exists (the exception is in the report's `Context:`); or the header reads `Loaded 0 mods.`; or `~/.config/StardewValley/ErrorLogs/SMAPI-crash.txt` exists — the game crashed during the run, which is also `5` | a skip means SMAPI is older than the mod's `MinimumApiVersion` (`4.0.0`) — install a newer SMAPI; a mod-scoped error means fixing the exception named in the report's `Context:` (the matched record, its continuation lines and the mod's TRACE records); a crash means reading `SMAPI-crash.txt` — the script's own line: "read the crash log above — the crash overrides the mod-resolution output per FR-006" | exit `0`, `SUCCESS: Mod loaded without errors.` on stdout, the mod listed in the loaded-mods block, and — when the game reached the title screen — the `mod-success-marker:` line |
| D-10 | exit code `12` — `grep -E 'build:\|deploy verified' "${XDG_STATE_HOME:-$HOME/.local/state}"/living-roots-smoke/runs/<run-id>.log \| tail -40` | the `build:` lines end with `Build succeeded.` and the log carries `deploy verified: sha256 <digest>` | the rebuild failed (the report names the `dotnet build` exit status), exceeded `--build-timeout`, produced no assembly, did not deploy into the resolved mods path, or the deployed digest differs from the fresh build | fix the build error — the run log has the full build output; raise `--build-timeout` if the bound expired (the report names the bound that expired, not the elapsed time); make the resolved mods path writable so the deploy takes effect. Compare the digests yourself with `sha256sum LivingRoots/bin/LivingRoots.dll "<mods-path>/LivingRoots/LivingRoots.dll"` | the run log carries `deploy verified: sha256 <digest>` and the run proceeds to the lock and the launch |
| D-11 | exit code `4` — `grep -nE 'Log started at \|\] Loaded [0-9]+ mods[.:]' ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` | two lines: the run sentinel `Log started at <UTC> UTC` and the mod-resolution header `] Loaded N mods:` | the sentinel is missing (SMAPI never started) or the sentinel is present but the header is not (SMAPI started and never resolved the mods — a graphics-device failure stops it there, see D-14) | raise `--timeout` (the default 120 s is measured **from the launch**, not from the script's start) or fix whatever kept SMAPI from starting — the script's own line: "raise --timeout, or check the run log for why SMAPI did not start" | the sentinel and the mod-resolution header both appear inside the window and the run reaches the verdict phase |
| D-12 | exit code `15` — `ls -l ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` | the file exists and its mtime is from this run | the game exited before SMAPI wrote a log — a **clean** exit that produced no log is `15`, never `0`: SMAPI's dependency-assertion routine exits with status `0` and the message "Please exit this window and re-launch SMAPI to play" after a game update | SMAPI exited without writing a log — check for a stale `StardewModdingAPI.deps.json` and re-run the SMAPI installer, then re-run the script — the script's own line: "re-run the SMAPI installer in the game directory, then re-run" | SMAPI writes a log at startup and the run gets past the readiness wait |
| D-13 | exit code `17` — `grep -nE 'Log started at \|\] Loaded [0-9]+ mods[.:]\|Failed to create graphics device\|GPU: Could not detect' ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` | the sentinel line **and** a `Loaded N mods:` header line are both present | the sentinel is present but the header is absent — that is the `17` case, classified **from the log**, not from the status. It is told apart from `13` by the sentinel (`13` = no sentinel at all) and from `5` by the header (`5` = the header is present). A leftover `smapi-internal/StardewModdingAPI.crash.marker` or `…update.marker` (or `SMAPI-crash.txt`) in the game directory makes SMAPI's blocking keypress read throw on the `/dev/null` stdin and abort the launch before mod resolution — the pre-flight removes all three, so one present here was re-created mid-run | remove the leftover marker and the crash log and re-run; if the log shows a graphics-device failure, fix the Mesa stack (D-14) — the script's own line: "check for a leftover SMAPI marker in the game directory and for a graphics-device failure in the log" | SMAPI resolves the mods, the `Loaded N mods:` block appears, and the run reaches the verdict phase |
| D-14 | exit code `3` — `grep -nE 'Failed to create graphics device\|GPU: Could not detect' ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` | no match — and D-6 and D-7 above both passed | the log records a graphics-device failure. This leg of the log gate takes **precedence** over every other leg, because SMAPI creates the graphics device *before* it resolves any mod — so the loaded-mods block is never written, and a script that classified on block-absence alone would report `4` instead | install a usable software OpenGL stack (Mesa's llvmpipe) — the script's own line: "install a usable software OpenGL stack (Mesa's llvmpipe); the pre-flight probe should have caught this"; for the pre-flight variants (no `GLX`/`RANDR`, no software renderer, no framebuffer-object support) see D-6 and D-7 | the run log records `graphics probe passed: renderer=llvmpipe fbo=GL_ARB_framebuffer_object …` and the run reaches the mod-resolution header |
| D-15 | exit code `2` — `ls -l ~/.steam/steam/steamapps/common/"Stardew Valley"/StardewModdingAPI{,.dll} ~/.steam/steam/steamapps/common/"Stardew Valley"/smapi-internal/SMAPI.Toolkit*.dll; test -x ~/.steam/steam/steamapps/common/"Stardew Valley"/StardewModdingAPI && echo launcher-executable` | all four markers exist, the launcher is executable, and `StardewModdingAPI.dll` is a readable .NET assembly whose version the script can parse | a marker file is missing, the launcher is not executable, the SMAPI installation resolves **outside** the game directory, or the assembly's version cannot be parsed — and at launch the apphost died: `154` (no assembly beside the launcher), `131` (no runtime) and `134` (runtime present, game files absent) are all reported as `2`, while a **partial runtime exits `135` and is reported as `13`**, not `2` | SMAPI is broken or misplaced — re-run the SMAPI installer in the game directory (`chmod +x` the launcher if only its mode is wrong). The launcher must live **inside** the game directory: the apphost resolves its .NET runtime relative to its own path | SMAPI starts and writes a log |
| D-16 | exit code `9` — `for t in Xvfb xdpyinfo glxinfo flock timeout mktemp stat realpath sha256sum date grep dotnet ps readlink sed awk cut head tr cp cat sort touch mkdir chmod rm dirname basename tail sleep mkfifo; do command -v "$t" >/dev/null \|\| echo "missing: $t"; done; command -v pgrep >/dev/null \|\| command -v pidof` | nothing is printed — all **31** swept tools are on `PATH`, and **either `pgrep` or `pidof`** is present (one of the two suffices) | the tool sweep passed, so the missing-tool error names a tool that was found at sweep time but became unavailable (or the run was invoked with a restricted `PATH`) | the message names the missing tool and the package that provides it — the script's own line: "install the missing package(s): Xvfb(xorg-x11-server-Xvfb) … — on Fedora: xorg-x11-server-Xvfb, mesa-utils, util-linux, procps-ng, coreutils, grep, gawk, dotnet-sdk"; install it and re-run | the run proceeds past the tool sweep |
| D-17 | exit code `14` — `./scripts/smoke-validation.sh --help \| sed -n '/^Options:/,/^Environment/p'` | the option table with the exact spellings — and the command line that failed matches it | an unknown option, a missing argument or a malformed value. Option names are matched **in full**: `--mod` is ambiguous between `--mod-project` and `--mods-path` and is rejected, `--mods` would silently mean `--mods-path`, and `-h` is the only short option | use the full option name and a positive integer — the script's own lines: "run 'smoke-validation.sh --help' for the supported options — long form only ('-h' is the only short option), and option names are never abbreviated" and "pass a positive integer, e.g. '--timeout=120' — see 'smoke-validation.sh --help'" | the script accepts the arguments and a run log appears — a usage error never creates one, because the run log is created only after the command line has been accepted |
| D-18 | exit code `16` — `ls -ld ~/.steam/steam/steamapps/common/"Stardew Valley"/Mods; grep -E '"(UniqueID\|EntryDll)"' ~/.steam/steam/steamapps/common/"Stardew Valley"/Mods/LivingRoots/manifest.json LivingRoots/manifest.json` | the mods directory exists and is writable, and both manifests declare `UniqueID` = `lfgranja.LivingRoots` and `EntryDll` = `LivingRoots.dll` | the resolved mods directory does not exist or is not writable (pre-flight), the mod project's manifest is missing `UniqueID` or `EntryDll`, or the deployed manifest's `UniqueID` does not match the mod project's (post-build) | create the directory or point at the right one — the script's own line: "create it, or pass --mods-path <dir> pointing at the directory SMAPI should load mods from"; and "make the directory writable — the build deploys the mod into it" | the pre-flight's mods-dir check and the post-build manifest check both pass, and the run proceeds to the launch |
| D-19 | exit code `1` — `ls -l ~/.steam/steam/steamapps/common/"Stardew Valley"/"Stardew Valley.dll"` | the game assembly exists | the game directory does not exist, or `Stardew Valley.dll` is missing from it | point the script at the installation — the script's own line: "pass --game-path <dir> pointing at the Stardew Valley installation" | the pre-flight's game check passes and the run proceeds to the SMAPI marker check |
| D-20 | exit code `11` — `cat ~/.steam/steam/steamapps/common/"Stardew Valley"/steam_appid.txt` | the file contains `413150` | the file is missing, or it holds a different app id | restore `steam_appid.txt` (content `413150`) in the game directory — the script's own line: "restore steam_appid.txt (content: 413150) in the game directory" | the Steam prerequisite check passes. A running Steam client is **not** required — the game ships the file, and SMAPI's "Steam achievements won't work" notice comes from SMAPI core, outside the mod-scoped gate |
| D-21 | exit code `6` — `LOCK="${XDG_RUNTIME_DIR:+$XDG_RUNTIME_DIR/livingroots-smoke.lock}"; LOCK="${LOCK:-${XDG_STATE_HOME:-$HOME/.local/state}/livingroots-smoke/livingroots-smoke.lock}"; flock -n "$LOCK" -c true; echo "flock exit=$?"` | `flock exit=0` — the lock is free | `flock exit=1` — another instance holds it | wait for the other instance to finish — the script's own line: "wait for the other instance to finish (the kernel releases the lock when it exits, even after a SIGKILL)" | the next run acquires the lock and proceeds to the process-conflict check |
| D-22 | exit code `7` — `for p in /proc/[0-9]*; do e=$(readlink -f "$p/exe" 2>/dev/null) \|\| continue; case "$e" in "$HOME/.steam/steam/steamapps/common/Stardew Valley"/*) echo "${p#/proc/} $e";; esac; done` | nothing is printed | a Stardew Valley/SMAPI process is running under the game directory. The script matches on `/proc/<pid>/exe`, never on the process **name**: the kernel's `comm` is truncated to 15 characters and the apphost's name is 17, so an exact-name match can never succeed | close the running game, or re-run with `--force` (SIGTERM, then SIGKILL, against the **identified PID only**) | no conflicting process remains and the run proceeds to the log baseline and the launch |
| D-23 | exit code `10` — `Xvfb -version >/dev/null 2>&1; s=$?; [ "$s" -lt 126 ] && echo "Xvfb runs (status $s)" \|\| echo "Xvfb cannot be executed (status $s)"`; `grep -n 'virtual display\|Xvfb did not report' "${XDG_STATE_HOME:-$HOME/.local/state}"/living-roots-smoke/runs/<run-id>.log` | `Xvfb runs (status N)` with `N` below `126`, and the run log carries `virtual display allocated: :N (Xvfb pid …, group …)` | the run log carries `Xvfb did not report a display number (pid N)` — Xvfb started but never wrote the number to the `-displayfd` file within its 15 s window (out of memory, a full `/tmp`, a broken X build). A probe status of `126` or `127` means the binary cannot be executed at all, which the pre-flight reports as a **missing host tool** (`9`), not `10`; a status below `126` is normal — not every Xvfb build accepts `-version`, and the script's own probe only asserts that the binary runs | the script's own line: "check that Xvfb starts on this host and that the graphics stack is usable (mesa-utils provides glxinfo and xdpyinfo)" | the display is allocated, the graphics probe runs, and the run reaches the rebuild-and-verify phase |
| D-24 | exit code `13` — `grep -nE 'unhandled error\|ERROR: smoke validation failed' "${XDG_STATE_HOME:-$HOME/.local/state}"/living-roots-smoke/runs/<run-id>.log` | no match — the failure was not an internal error | the ERR trap fired ("unhandled error (the failing command exited with status N) inside `<function>`", at a named line of `smoke-validation.sh`). The other `13` routes: the stale SMAPI log survived the removal, the log directory is not writable, the temp directory could not be created, the lock file could not be opened or its directory created, `HOME` is unset, the log gate returned an unknown verdict, or the log carries **no run sentinel** (a foreign or truncated log — told apart from `17`, which has the sentinel but no mod-resolution header) | this is a bug in the script, not in your setup — the script's own line: "this is a bug in smoke-validation.sh — re-run with the run log attached and report it" | the script no longer errors and the run reaches its verdict |
| D-25 | exit code `129` / `130` / `143` — `echo $?` immediately after the script returns | `129` (SIGHUP), `130` (SIGINT — Ctrl+C) or `143` (SIGTERM) — the POSIX `128+N` convention | the script was not interrupted; the code is a domain failure and belongs to another row | none — the run was interrupted on purpose. The signal is forwarded to the game and Xvfb process groups, the groups are reaped, the socket of the display this run allocated is removed and the temp directory is deleted | no orphan survives the interruption (`pgrep -a Xvfb` and `pidof StardewValley` return nothing), and the next run is not blocked — the kernel released the lock when the script died |

### Exit-code index (quick lookup)

| Exit code | Likely cause | Remediation | Expected result once fixed |
|-----------|--------------|-------------|---------------------------|
| 0 | Success — the mod loaded without errors | — | `SUCCESS: Mod loaded without errors.` |
| 1 | Game not installed (the directory or `Stardew Valley.dll` is missing) | Install Stardew Valley via Steam; check `--game-path` | the pre-flight finds the game |
| 2 | SMAPI not installed, broken, or not functional (a relocated launcher exits `154` (no assembly), `131` (no runtime) or `134` (runtime present, game files absent); a partial runtime exits `135`, which is reported as `13`, not `2`) | Run the SMAPI installer in the game directory; verify `StardewModdingAPI`, `StardewModdingAPI.dll`, and `smapi-internal/SMAPI.Toolkit*.dll` exist | SMAPI starts and writes a log |
| 3 | Graphics stack present but unusable — no `GLX`/`RANDR`, no software renderer, or no framebuffer-object support; or the log records a graphics-device failure (`Failed to create graphics device`, `GPU: Could not detect`) | `sudo dnf install mesa-utils mesa-dri-drivers` (or the apt equivalents); verify `LIBGL_ALWAYS_SOFTWARE=1 glxinfo -B` reports `llvmpipe` and that full `glxinfo` lists `GL_ARB_framebuffer_object` | the graphics probe passes |
| 4 | Game slow to launch, or the run sentinel / mod-resolution header never appeared within `--timeout` (measured from the launch) | Increase the timeout: `--timeout 180`; check the run log for why SMAPI did not start | the sentinel and the `Loaded N mods:` header appear within the window |
| 5 | Mod not loaded, skipped by SMAPI, or produced an error; or the game crashed during the run | Check the SMAPI log's loaded-mods block and its skipped-mods section; the failure report includes the matched failure record together with its continuation lines. A skip means SMAPI is older than the mod's `MinimumApiVersion` (`4.0.0`) | the mod appears in the block with no Error entries |
| 6 | Another instance running (the `flock` failed) | Wait for the other instance to finish (the lock is released by the kernel when that instance exits) | the next run acquires the lock |
| 7 | Game already running | Close the game or use `--force` | no game process remains |
| 8 | **Retired** (round 2) — superseded by the POSIX `128+N` interruption codes | — | never reused; a new code is always allocated above the highest published code |
| 9 | A required host tool is absent | Install the missing binary — the error names it and the package that provides it (e.g. `Xvfb` → `xorg-x11-server-Xvfb`, `glxinfo`/`xdpyinfo` → `mesa-utils`, `dotnet` → `dotnet-sdk`) | the tool sweep passes |
| 10 | The virtual display could not be allocated or started | Xvfb failed to start — check that `Xvfb` is installed and that the process is not being killed by a memory limit; the script allocates the display itself via `-displayfd` | the display starts and the graphics probe runs |
| 11 | `steam_appid.txt` missing or wrong | Restore `steam_appid.txt` (content `413150`) in the game directory | the Steam prerequisite check passes |
| 12 | The build failed, exceeded `--build-timeout`, or the deployed assembly is missing, older, or not byte-identical to the fresh build | Fix the build error (the run log has the full build output); raise `--build-timeout` if the bound expired; make sure the resolved mods path is writable so the deploy can take effect | the digests match and the run proceeds |
| 13 | The script itself broke (unhandled error, a failed process-group assertion, a failed pre-launch assertion, an unknown gate verdict, or a log with no run sentinel) | Read the run log — the ERR trap reports the script name, failing line and function. This is a bug in the script, not in your setup | the script no longer errors |
| 14 | Usage or CLI-argument error | Re-read `--help`; the message names the offending option and its expected form. Option names are never abbreviated | the script accepts the arguments |
| 15 | The game exited without producing a SMAPI log (including a clean exit with status `0`) | SMAPI re-asserted its dependency manifest and asked for a relaunch — re-run the SMAPI installer, then re-run the script | SMAPI writes a log |
| 16 | The resolved mods path, the mod folder, `manifest.json`, or its `UniqueID` is invalid | Check `--mods-path`; the mod folder must contain `LivingRoots/manifest.json` with `UniqueID` = `lfgranja.LivingRoots` | the manifest check passes |
| 17 | SMAPI aborted before mod resolution — the run sentinel was seen but the mod-resolution header never appeared, and the process exited | Classified from the **log**, not the status. Check for a leftover `smapi-internal/StardewModdingAPI.crash.marker` or `…update.marker` (the pre-flight removes both), and for a graphics-device failure in the log | SMAPI resolves the mods and the block appears |
| 129 / 130 / 143 | Script terminated by SIGHUP / SIGINT / SIGTERM | Normal — cleanup was run | — |

---

## Escalation Path

When the script fails and the troubleshooting table does not resolve it, escalate to the maintainer with a complete evidence bundle. Escalation is a forcing function, not a failure — provide the bundle so the next tier does not repeat diagnostic work.

### Contacts and Channels

| Tier | Owner | Channel | Time-box |
|------|-------|---------|----------|
| L1 | Maintainer (repo owner) | GitHub issue on `stardew-raizes-vivas` | 15 min |
| L2 | Mod developer (ModEntry/DI wiring) | Direct message + issue link | 30 min |
| L3 | SMAPI/game runtime | Steam/SMAPI community + issue | 60 min |

### Required Evidence (must accompany any escalation)

1. **Timestamps** — run start (ISO 8601 UTC), failure detection time, last SMAPI log line time, script `$VERSION`.
2. **Exit code** — numeric code and the documented category (0=success, 1=game, 2=SMAPI, 3=graphics-unusable, 4=timeout, 5=mod failure, 6=lock, 7=process conflict, 9=missing dependency, 10=display allocation failure, 11=Steam prerequisite, 12=build/artifact failure, 13=internal error, 14=usage error, 15=no log produced, 16=mods path/manifest invalid, 17=SMAPI aborted before mod resolution, 129/130/143=interrupted).
3. **Error text** — exact error message emitted by the script plus the **matched failure record together with its continuation lines** from `SMAPI-latest.txt` (including every mod Trace record that follows the failure — those carry the exception type and HResult). A fixed `tail -N` window is **not** used: a .NET stack trace is routinely 20–40 lines long, so such a window can consist entirely of stack frames while omitting the record that names the failure.
4. **Repro steps** — exact command invoked (with flags), environment (game path, SMAPI version, mod version/git commit), and a clean rerun result.
5. **System context** — conflicting PIDs (if any), lock file path, Xvfb display number, temp directory, OS/kernel.
6. **Run log** — the path to the run log for the failed run (`${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log`). The run log contains the run ID, the script `$VERSION`, the launch timestamp, and every lifecycle event, so it is the primary artifact for reconstructing the run.

### Evidence Template

```
Run ID:        <script run id / correlation id>
Started:       <ISO8601 UTC>
Failed at:     <ISO8601 UTC>
Exit code:     <n> (<category>)
Version:       <script version> / mod <git sha>
Run log:       <path to runs/<run-id>.log>
Command:       ./scripts/smoke-validation.sh <flags>
Game path:     <path>
Mods path:     <path>
SMAPI version: <version>
Last log line: <timestamp>
Error text:    <exact script error>
SMAPI excerpt: <the matched failure record and its continuation lines>
Repro:         <steps>
```

### Escalation Rules

- Time-box each tier: 15 min at L1 before pulling in the mod developer; 30 min before involving SMAPI/runtime support.
- Isolate the fault boundary first (inside vs. outside the stack): check the vendor status page; if an external dependency is implicated, engage it in parallel.
- Never escalate without the evidence bundle above; incomplete escalations are returned to the sender.

---

## Suspension Criteria

Suspension is a **campaign-level** decision, not a per-run state: an individual run always terminates with an exit code from the FR-007 taxonomy, and it is the *sequence of runs* that may be suspended (IEEE 829-2008 §9.2.7 → ISO/IEC/IEEE 29119-3 §6.2.7.9 → ISO/IEC/IEEE 29119-2 §8.4.4 NOTE 1).

| # | Suspension criterion | Who may suspend / resume | What is recorded |
|---|----------------------|--------------------------|-----------------|
| S-1 | The same failure class fails **three consecutive times** with an identical exit code | The L1 maintainer (repo owner) | A `SUSPENDED` entry in the escalation bundle: timestamp, the three run IDs, the exit code and its category |
| S-2 | The host environment changed mid-campaign (game patch, SMAPI upgrade, Mesa/`glxinfo` change, .NET SDK change) | The L1 maintainer | The `SUSPENDED` entry above plus what changed |
| S-3 | The run cannot be completed because of an external cause (the game install is mid-update, the disk is full) | The L1 maintainer | The `SUSPENDED` entry above plus the external cause |

**Authority**: only the L1 maintainer may suspend or resume a campaign. An individual run never suspends — it fails with a code. Resumption requires every precondition in *Resumption Requirements* below to hold.

---

## Resumption Requirements

A run that ended in failure (or was interrupted) can be re-run without manual repair **once all of the following are true**. If any precondition is false, the re-run will fail in the same way and is not worth attempting until it is fixed.

| # | Precondition | How to verify | Satisfied when |
|---|--------------|---------------|----------------|
| R-1 | No Stardew Valley/SMAPI process is still running | `pidof StardewValley; pidof StardewModdingAPI` | Both commands return nothing (or `--force` is used). `pidof` is used rather than `pgrep -x` because the kernel's process name is truncated to 15 characters and `StardewModdingAPI` is 17 — an exact-name match can never succeed |
| R-2 | No orphaned Xvfb is left over from a previous crashed run | `pgrep -a Xvfb` | Returns nothing. The pre-flight **detects and reports** orphaned Xvfb instances but MUST NOT signal them — under `Xvfb -displayfd` the server writes no lock file, so there is no way to prove ownership of a foreign X server, and the display number is allocated by the server (FR-012 item 10). Kill them manually |
| R-3 | No orphaned Xvfb holds a display | `pidof Xvfb` | No orphan Xvfb remains. The script allocates its own display with `Xvfb -displayfd`, so it never reads or writes `/tmp/.X{n}-lock` files — the server binds a free display atomically and writes no lock at all |
| R-4 | The smoke-test lock is free | The previous run has exited (the kernel releases the `flock` on process death, even after SIGKILL) | The lock is acquirable — a second run no longer exits with `6` |
| R-5 | No leftover SMAPI marker can abort or mask the run | The pre-flight removes SMAPI's leftover **crash and update markers** (`<game-path>/smapi-internal/StardewModdingAPI.crash.marker` and `…update.marker`) and the crash log (`SMAPI-crash.txt`) before launch | Both markers are absent before launch — with the stdin redirected from `/dev/null`, SMAPI's leftover-marker keypress read throws and aborts the launch with status `1` *before* mod resolution, and the marker is not self-clearing on that path, so every later run would fail identically |
| R-6 | No leftover temp directories | `ls -d "${TMPDIR:-/tmp}"/livingroots-smoke.* 2>/dev/null` | Empty — the EXIT trap removed them, or they were removed manually after a SIGKILL |
| R-7 | The mod build under test is the one this run produced | The script rebuilds with the deploy target pinned to the resolved mods path, and compares SHA-256 digests of the deployed assembly against the fresh build | The digests match — a mismatch, a missing file, or an older file aborts the run with exit `12` before launch |
| R-8 | No stale SMAPI log can be mistaken for this run's | The pre-flight removes `SMAPI-latest.txt`; the script also requires the current run's `Log started at {UTC} UTC` sentinel | The sentinel's timestamp is not older than the run's launch timestamp |

**Rule:** the script is idempotent (FR-008) — a re-run under the same conditions produces the same verdict. If a re-run produces a *different* verdict than the previous run with identical inputs, one of the preconditions above was violated.

### Activities to Be Repeated on Resumption

On resumption the run restarts at the appropriate task (ISO/IEC/IEEE 29119-2 §8.4.4 NOTE 1). Concretely:

1. **TC-1 (mod loads successfully) is always re-run first**, as the canary for the environment.
2. **Any test case whose verdict depended on the condition that caused the suspension MUST be re-run.**
3. **Test cases that passed before the suspension are NOT re-run** — as little as possible of the testing already done should be repeated at resumption, based on a risk assessment (ISO/IEC/IEEE 29119-3 Annex worked example §5.7). A case may be marked "not re-run" only when its expected result is provably independent of the resolved condition.
4. At resumption of a suspended test procedure, **the affected test procedure must be repeated**.

---

## Post-Run Verification

After a successful run, verify:

1. **No orphaned processes**: `pgrep -a StardewValley` should return nothing
2. **No orphaned Xvfb**: `pgrep -a Xvfb` should return nothing
3. **No leftover socket for the script's own display**: `ls /tmp/.X11-unix/` should not contain the display the script allocated. The script removes only that socket, and never touches `/tmp/.X{n}-lock` files — the server writes no lock under `-displayfd`, and any lock it did write is installed atomically by the server
4. **Temp files cleaned**: No smoke-validation temp directories remain

---

## Idempotency Check

Run the script twice in sequence — both should produce the same result:

```bash
$ ./scripts/smoke-validation.sh
SUCCESS: Mod loaded without errors.
$ ./scripts/smoke-validation.sh
SUCCESS: Mod loaded without errors.
```
