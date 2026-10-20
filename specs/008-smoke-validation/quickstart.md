# Quickstart: Smoke Validation Script

**Date**: 2026-10-19
**Context**: `specs/008-smoke-validation` — validation and run guide

---

## Prerequisites

Before running the smoke validation script, ensure:

1. **Stardew Valley** is installed via Steam on Linux at `~/.steam/steam/steamapps/common/Stardew Valley/`
2. **SMAPI 4.5.2** is installed and functional in the game directory. The correct markers are `StardewModdingAPI` (the ELF launcher), `StardewModdingAPI.dll`, and `smapi-internal/SMAPI.Toolkit.dll` + `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll`. **No SMAPI release ships `smapi-internal/SMAPI.dll`** — do not use that as the check
3. **Xvfb** is available on the system. On Fedora this is the `xorg-x11-server-Xvfb` package (`apt install xvfb` on Debian/Ubuntu). **It is not installed by default**, and the pre-flight will exit `9` without it (a missing binary is a missing host tool). **It is not installed on the maintainer's machine as of 2026-10-19**, so no end-to-end run of the script is currently possible — every TC below is unexecuted until it is installed
4. **Mesa software rendering** is usable, and `mesa-utils` provides `glxinfo` and `xdpyinfo`. The pre-flight starts the virtual display and asserts, in layers: `xdpyinfo` reports the `GLX` and `RANDR` extensions; `glxinfo -B` under `LIBGL_ALWAYS_SOFTWARE=1` exits **0** (it exits **255**, not 1, on failure); the `OpenGL renderer string` is `llvmpipe` or `softpipe`; and **full `glxinfo` reports `GL_ARB_framebuffer_object` or `GL_EXT_framebuffer_object`** (or the GL version is ≥ 3.0). That last layer is load-bearing — the game's MonoGame backend throws `PlatformNotSupportedException: MonoGame requires either ARB_framebuffer_object or EXT_framebuffer_object` when a context lacks FBOs, and `glxinfo -B` prints no extension list, so it cannot detect that failure. A missing `glxinfo`/`xdpyinfo` exits `9`; a stack that is present but cannot provide usable software OpenGL exits `3`
5. **The Living Roots mod** is deployed into the resolved mods directory (default `~/.steam/steam/steamapps/common/Stardew Valley/Mods/LivingRoots/`). The script rebuilds and redeploys it on every run — with the build's deploy target pinned to the resolved mods path — and verifies the deployed assembly by SHA-256 digest, so a manual deploy is not required. **Note:** the script *deletes and recreates* that directory on every run
6. **Bash 4+** is available (standard on Linux)
7. **ASCII host paths** — path components are validated against `[A-Za-z0-9._/-]+`. No Unicode normalization is performed and **no normalizer dependency is required**: NFC is a provable no-op for ASCII paths (UAX #15) and is not a homoglyph defence (that needs UTS #39)
8. **The .NET SDK (`dotnet`) IS required on the host** — the script rebuilds the mod before every run (FR-016). The *game* needs no host .NET runtime: the Steam install ships a self-contained .NET 6 `linux-x64` runtime (`libcoreclr.so`, `libhostfxr.so`, `libhostpolicy.so` in the game folder). Mono is not involved.
9. **The remaining host tools** — `flock`, `timeout`, `mktemp`, `stat`, `realpath`, `pgrep` (or `pidof`), `sha256sum`, `date` and `grep` — must be on `PATH`. The pre-flight verifies each with `command -v` and exits `9` naming the missing one (FR-012 item 8)

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

### Configurable Timeout

```bash
# Default 120-second timeout
./scripts/smoke-validation.sh

# Custom 180-second timeout
./scripts/smoke-validation.sh --timeout 180
```

`--timeout` bounds the **game-launch-to-verdict window** only, and it is measured **from the instant the game process is launched** — not from the script's start. The pre-flight and the rebuild-and-verify phase run before the launch and are **not** charged against it (FR-009). The rebuild has its own bound, `--build-timeout` (default 300 seconds), so a hung NuGet restore cannot hang the run forever (FR-016). The run log records the duration of every phase and the total wall time, so both budgets are auditable after the fact (FR-014).

### Build Timeout

```bash
# Default: 300 seconds
./scripts/smoke-validation.sh

# Give the rebuild 10 minutes
./scripts/smoke-validation.sh --build-timeout 600
```

A build that exceeds the bound aborts the run with exit code `12` and a message naming the elapsed time. The build timeout is independent of `--timeout` and is never consumed from it.

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

1. **Tool sweep** — every required host tool is verified with `command -v`; a missing one exits `9` naming the tool (FR-012 item 8)
2. **Game & SMAPI checks** — the game installation and SMAPI's markers; then the Steam prerequisite `steam_appid.txt` (FR-012 items 1, 2, 9)
3. **Stale-artifact cleanup** — a leftover `SMAPI-crash.txt` and SMAPI's **crash and update markers** (`smapi-internal/StardewModdingAPI.crash.marker` and `…update.marker`) are removed, because a marker is what makes SMAPI perform a keypress read that aborts the headless launch before mod resolution (FR-012 item 6)
4. **Virtual display + graphics probe** — Xvfb is started with `-displayfd` (the server allocates a free display and reports it back), then the layered software-OpenGL probe runs against it (FR-012 items 4, 5)
5. **Build & verify artifact** — `dotnet build --configuration Release --no-incremental` with `-p:GameModsPath="<resolved mods path>"`, after removing the deploy target; the deployed assembly's SHA-256 digest is compared with the freshly built one. **A mismatch aborts the run before the game is launched** (FR-016)
6. **Mod manifest check** — the resolved mods path contains `LivingRoots/manifest.json` with a matching `UniqueID` (FR-017)
7. **Flock** + existing-process detection (FR-002, FR-011)
8. **Log baseline** — records the UTC launch timestamp and the log inode, *then* removes any stale `SMAPI-latest.txt` (the order matters: recording after the removal would make the inode assertion vacuous)

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

The **oracle** for every test case is the FR-007 exit-code contract (`data-model.md` → *Exit Codes*), plus, where the case concerns log content, the current run's `Log started at {UTC} UTC` sentinel. A test case **passes** iff the actual exit code equals the expected exit code **and** the named log predicate holds.

| # | Test item / feature | Objective | Priority | Traceability | Input | Execution condition | Expected output | Actual output | Pass/Fail |
|---|-----------|-----------|----------|---------------|-------|---------------------|-----------------|---------------|-----------|
| TC-1 | Mod loads successfully | Verify the mod resolves and loads in the real game with no errors | Must | FR-006, SC-002 | Default install paths, mod deployed, no save loaded | Script run with no arguments; **launch-to-verdict** within `--timeout` (default 120 s) | Exit `0`; `SUCCESS: Mod loaded without errors.`; the mod appears in SMAPI's loaded-mods block; game and Xvfb terminated; no orphaned processes; the per-phase durations and the total wall time are recorded in the run log | _(record)_ | ☐ Pass ☐ Fail |
| TC-2 | Mod load-time error | Verify a mod-scoped Error entry fails the run and is reported | Must | FR-003, FR-006, SC-003 | Mod build with a failing `ModEntry.Entry()` | Script run with no arguments | Exit `5`; failure message with "What → Where → How to fix" and SMAPI log context | _(record)_ | ☐ Pass ☐ Fail |
| TC-3 | Game not installed | Verify the pre-flight fails closed on a missing game install | Must | FR-007, FR-012, SC-009 | `--game-path` pointing at a non-existent directory | Script run with `--game-path <missing>` | Exit `1`; actionable pre-flight error naming the missing path | _(record)_ | ☐ Pass ☐ Fail |
| TC-4 | Game already running | Verify an existing game instance is detected and not clobbered | Must | FR-011, SC-007 | An existing Stardew Valley/SMAPI process | Script run **without** `--force` | Exit `7`; error listing the conflicting PIDs | _(record)_ | ☐ Pass ☐ Fail |
| TC-5 | Concurrent execution prevention | Verify a second instance cannot run concurrently | Must | FR-002, SC-008 | A second instance already holds the lock | Second script instance started while the first is running | Exit `6`; lock contention error; second instance exits immediately | _(record)_ | ☐ Pass ☐ Fail |
| TC-6 | Timeout | Verify the configurable timeout is honoured | Must | FR-009, SC-001 | Game that does not produce the SMAPI log | Script run with `--timeout 30`; no log produced within 30s **measured from the game launch** | Exit `4`; timeout error with the log path and the `--timeout` hint | _(record)_ | ☐ Pass ☐ Fail |
| TC-7 | Interruption | Verify cleanup runs and the exit status is the POSIX signal status | Must | FR-005, FR-010, SC-010 | Script in its wait/poll phase | User presses Ctrl+C (SIGINT) | Exit `130`; cleanup ran (game killed, Xvfb killed, the socket of the display this script allocated removed, temp files cleaned) | _(record)_ | ☐ Pass ☐ Fail |
| TC-8 | Pre-flight dependency missing | Verify the pre-flight fails closed when a required host tool is absent | Must | FR-012 item 8, SC-009 | Environment without `Xvfb` on `PATH` | Script run with `Xvfb` removed from `PATH` | Exit `9`; pre-flight error naming the missing tool and the package that provides it (`xorg-x11-server-Xvfb`) | _(record)_ | ☐ Pass ☐ Fail |
| TC-9 | Deploy ineffective | Verify the deployed assembly is byte-identical to the one this run built | Must | FR-016, SC-012 | The build cannot replace the deployed assembly in the resolved mods path | Make the resolved mods path read-only (or seed a stale `LivingRoots.dll` and make the deploy a no-op), then run the script | Exit `12` **before** the game launches; the message names both SHA-256 digests | _(record)_ | ☐ Pass ☐ Fail |
| TC-10 | Stale log cannot produce a false pass | Verify a stale success marker cannot make the run pass | Must | FR-002, SC-011 | A previous run left an `SMAPI-latest.txt` containing the mod's success marker | Seed a stale log, then run the script against a mod that fails to load | Exit `5` (not `0`); the stale marker is ignored because it precedes the current run's `Log started at` sentinel | _(record)_ | ☐ Pass ☐ Fail |
| TC-11 | Mod skipped by SMAPI | Verify a SMAPI skip is reported as a mod-load failure with SMAPI's reason | Must | FR-003, FR-006, SC-013 | Installed SMAPI older than the mod's `MinimumApiVersion` | Run the script with an incompatible SMAPI version | Exit `5`; report states SMAPI's skip reason ("needs SMAPI X or later") | _(record)_ | ☐ Pass ☐ Fail |
| TC-12 | SMAPI exits without producing a log | Verify a clean SMAPI exit that produced no log is a failure, not a success | Must | FR-002, FR-007 | A stale `StardewModdingAPI.deps.json` makes SMAPI re-assert its dependency manifest and exit before the game starts | Run the script against a SMAPI whose dependency manifest is stale | Exit `15`; the message explains the relaunch requirement and names the stale dependency manifest | _(record)_ | ☐ Pass ☐ Fail |
| TC-13 | Graphics environment unusable | Verify a present-but-unusable graphics stack is distinguished from a missing one | Must | FR-012 item 4, FR-007 | `glxinfo` reports no software renderer under Xvfb | Run the script with Mesa software rendering disabled | Exit `3`; error names the missing software renderer and the fix | _(record)_ | ☐ Pass ☐ Fail |
| TC-14 | Mods directory override | Verify the resolved mods directory is the one SMAPI uses | Must | FR-017 | A mods directory other than `<game-path>/Mods` | Run the script with `--mods-path <dir>` containing the mod | SMAPI receives the resolved directory (the `Mods go here:` line in the log matches the resolved path **after applying SMAPI's `~`-anonymisation**); the verdict is computed from that directory's mod | _(record)_ | ☐ Pass ☐ Fail |
| TC-15 | Run log, run ID, version and stream split | Verify the run log is produced and the streams are separated | Must | FR-014, SC-014 | Default install paths, mod deployed | Script run with no arguments | A run log exists at `${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log` and contains the run ID, the script `VERSION` and the launch timestamp; the **verdict** is on stdout and the **diagnostics** are on stderr; the run ID appears on every log line | _(record)_ | ☐ Pass ☐ Fail |
| TC-16 | `--help` completeness | Verify `--help` documents every contract the spec requires | Must | FR-015, SC-006 | — | `smoke-validation.sh --help` | Exits `0`; stdout documents the usage, every option (including `--mods-path`, `--mod-project`, `--timeout`, `--grace-period`, `--smapi-log`, `--force`), at least one example, the "Environment (set automatically)" section, the run-log location, the run-ID format, the `VERSION`, and **every** exit code in the FR-007 table | _(record)_ | ☐ Pass ☐ Fail |
| TC-17 | Status translation | Verify no raw subprocess status ever becomes the script's final code | Must | FR-007 | — | Feed each raw child status in the FR-007 translation table to the translator | Each status maps to its documented exit code (`124`→`4`, `125`→`13`, `126`→`9`, `127`→`9`, `131`→`2`, `134`→`2`, `137`→`5`/`2`/`13`, `154`→`2`, `128+N` unchanged, any other non-zero → `13`) | _(record)_ | ☐ Pass ☐ Fail |

**Execution order**: TC-1 → TC-2 → … → TC-17 (29119-3 §8.4.6 *Ordered test cases*). A failure in one case does **not** skip the independent cases that follow; TC-1 is always run first because it is the canary for the environment.

### Scenario 1: Mod Loads Successfully

**Given**: Game installed, SMAPI installed, mod deployed correctly, no save file
**When**: Script runs
**Then**: Exit code `0`, `SUCCESS` message, game terminated, no orphaned processes

```bash
$ ./scripts/smoke-validation.sh
SUCCESS: Mod loaded without errors.
$ echo $?
0
```

### Scenario 2: Mod Has Load-Time Error

**Given**: Game installed, mod has a runtime error during load
**When**: Script runs
**Then**: Exit code `5`, descriptive error message with SMAPI log context

```bash
$ ./scripts/smoke-validation.sh
FAILURE: Mod load error detected.
  What: Mod 'Stardew Valley - Living Roots' failed to load
  Where: ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt
  Error: NullReferenceException at ModEntry.Entry() line 143
  Fix: Check ModEntry.Entry() for null reference in DI wiring
$ echo $?
5
```

### Scenario 8: Deploy Ineffective

**Given**: The build cannot replace the assembly deployed into the resolved mods path
**When**: Script runs
**Then**: Exit `12` **before** the game is launched, naming both digests

```bash
$ ./scripts/smoke-validation.sh
ERROR: Deployed mod artifact does not match the current build.
  What: SHA-256 mismatch between the built and deployed LivingRoots.dll
  Where: ~/.steam/steam/steamapps/common/Stardew Valley/Mods/LivingRoots/LivingRoots.dll
  built:    568164bf32f768f4...
  deployed: 7a2e71c92d7201fb...
  Fix: Remove the mods directory and re-run — the script redeploys on every run
$ echo $?
12
```

### Scenario 3: Game Not Installed

**Given**: Game not found at expected path
**When**: Script runs
**Then**: Exit code `1`, actionable pre-flight error

```bash
$ ./scripts/smoke-validation.sh
ERROR: Game installation not found.
  What: Stardew Valley game directory missing
  Where: ~/.steam/steam/steamapps/common/Stardew Valley/
  Fix: Install Stardew Valley via Steam
$ echo $?
1
```

### Scenario 4: Game Already Running

**Given**: Stardew Valley/SMAPI process already running
**When**: Script runs (without `--force`)
**Then**: Exit code `7`, lists conflicting PIDs

```bash
$ ./scripts/smoke-validation.sh
ERROR: Game already running.
  What: Stardew Valley process detected
  Where: PID 12345 (StardewValley), PID 12346 (SMAPI)
  Fix: Close the game or use --force to kill existing instances
$ echo $?
7
```

### Scenario 5: Concurrent Execution Prevention

**Given**: Another smoke test instance is already running
**When**: Script runs
**Then**: Exit code `6`, lock contention error

```bash
$ ./scripts/smoke-validation.sh
ERROR: Another smoke test instance is running.
  What: Lock file is held by another process
  Where: ${XDG_RUNTIME_DIR}/livingroots-smoke.lock   (or ${XDG_STATE_HOME:-$HOME/.local/state}/livingroots-smoke/livingroots-smoke.lock when XDG_RUNTIME_DIR is unset)
  Fix: Wait for the other instance to complete
$ echo $?
6
```

### Scenario 6: Timeout

**Given**: Game takes longer than timeout to produce SMAPI log
**When**: Script runs with `--timeout 30` and game doesn't produce log in 30s
**Then**: Exit code `4`, timeout error

```bash
$ ./scripts/smoke-validation.sh --timeout 30
ERROR: Game did not produce SMAPI log within 30 seconds.
  What: SMAPI log file not created or not updated
  Where: ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt
  Fix: Increase timeout with --timeout flag or check game installation
$ echo $?
4
```

### Scenario 7: Interruption (Ctrl+C)

**Given**: Script is running and user presses Ctrl+C
**When**: SIGINT received
**Then**: EXIT trap runs cleanup (game killed, Xvfb killed, the socket of the display this script allocated removed), exit code `130`

```bash
$ ./scripts/smoke-validation.sh
^C
INTERRUPTED: Cleaning up...
$ echo $?
130
```

---

## Troubleshooting

### Diagnostic ladder

Ordered by **decreasing likelihood**, with **cost as the tiebreak** (Google SRE Ch. 12: "perform the tests in decreasing order of likelihood"; Cisco *IT Essentials* 4.2: "starting with the quickest and easiest"). Each step states the exact command, its expected result, the branch when it does not hold, the remediation, and what to expect once it is fixed. Start at D-1 and go down; the first step whose expected result does not hold is your diagnosis.

| Step | Check | Expected result if OK | If NOT ok → | Remediation | Expected result once fixed |
|------|-------|----------------------|-------------|-------------|---------------------------|
| D-1 | `command -v Xvfb xdpyinfo glxinfo dotnet flock sha256sum` | every path printed | D-2 | install the named package (`xorg-x11-server-Xvfb`, `mesa-utils`, `dotnet-sdk`, `util-linux`) | the tool is on `PATH`; the run reaches the next step |
| D-2 | `ls -l ~/.steam/steam/steamapps/common/"Stardew Valley"/StardewModdingAPI{,.dll} ~/.steam/steam/steamapps/common/"Stardew Valley"/smapi-internal/SMAPI.Toolkit*.dll` | all four files exist | D-3 | re-run the SMAPI installer in the game directory | the markers exist; the run reaches the next step |
| D-3 | `cat ~/.steam/steam/steamapps/common/"Stardew Valley"/steam_appid.txt` | `413150` | D-4 | restore `steam_appid.txt` from a Steam install | the file contains `413150` |
| D-4 | `pidof StardewValley; pidof StardewModdingAPI` | both return nothing | D-5 | close the game, or re-run with `--force` | no game process remains |
| D-5 | `pgrep -a Xvfb` | no orphan Xvfb holds a display | D-6 | kill the orphan Xvfb manually (the script allocates its own display via `-displayfd`, so a stale lock is harmless and the pre-flight only reports orphans) | no orphan Xvfb remains |
| D-6 | `LIBGL_ALWAYS_SOFTWARE=1 glxinfo -B \| grep -E 'llvmpipe\|softpipe'` | `OpenGL renderer string: llvmpipe` | D-7 | install Mesa's software rasteriser (`mesa-dri-drivers`); confirm `GALLIUM_DRIVER=llvmpipe` is exported | the renderer string reports a software rasteriser |
| D-7 | `glxinfo \| grep -E 'GL_ARB_framebuffer_object\|GL_EXT_framebuffer_object'` | one extension is listed | D-8 | update Mesa — the game's MonoGame backend requires FBO support | the extension is listed |
| D-8 | `tail -30 ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` | the current run's `Loaded N mods:` block is present | D-9 | see D-9…D-12 by the exit code | the mod appears in the block |
| D-9 | exit code `9` | the tool sweep passed, so the missing-tool error names a tool that was found at sweep time but became unavailable (or the run was invoked with a restricted `PATH`) | see the remediation in this row | the message names the missing tool; install it and re-run | the run proceeds past the tool sweep |
| D-10 | exit code `2` | SMAPI is installed and functional | see the remediation in this row | SMAPI is broken or misplaced — re-run the SMAPI installer; a relocated launcher exits `154` (no assembly), `131` (no runtime), `134` (runtime present, game files absent) or `135` (partial runtime) | SMAPI starts |
| D-11 | exit code `15` | SMAPI wrote a log | see the remediation in this row | SMAPI exited without writing a log — check for a stale `StardewModdingAPI.deps.json` and re-run the SMAPI installer | SMAPI writes a log |
| D-12 | exit code `12` | the deployed assembly matches the fresh build | see the remediation in this row | the deploy did not take effect — make sure the resolved mods path is writable and that `dotnet build` succeeds | the digests match and the run proceeds |

### Exit-code index (quick lookup)

| Exit code | Likely cause | Remediation | Expected result once fixed |
|-----------|--------------|-------------|---------------------------|
| 0 | Success — the mod loaded without errors | — | `SUCCESS: Mod loaded without errors.` |
| 1 | Game not installed | Install Stardew Valley via Steam; check `--game-path` | the pre-flight finds the game |
| 2 | SMAPI not installed, broken, or not functional (a relocated launcher exits `154`, `131`, `134` or `135`) | Run the SMAPI installer in the game directory; verify `StardewModdingAPI`, `StardewModdingAPI.dll`, and `smapi-internal/SMAPI.Toolkit*.dll` exist | SMAPI starts and writes a log |
| 3 | Graphics stack present but unusable — no `GLX`/`RANDR`, no software renderer, or no framebuffer-object support | `sudo dnf install mesa-utils mesa-dri-drivers` (or the apt equivalents); verify `LIBGL_ALWAYS_SOFTWARE=1 glxinfo -B` reports `llvmpipe` and that full `glxinfo` lists `GL_ARB_framebuffer_object` | the graphics probe passes |
| 4 | Game slow to launch, or never produced a log | Increase the timeout: `--timeout 180`; check the run log for a `Log started at` sentinel | the sentinel appears within the window |
| 5 | Mod not loaded, skipped by SMAPI, or produced an error | Check the SMAPI log's loaded-mods block and its skipped-mods section; the failure report includes the matched failure record together with its continuation lines | the mod appears in the block with no Error entries |
| 6 | Another instance running | Wait for the other instance to finish (the lock is released by the kernel when that instance exits) | the next run acquires the lock |
| 7 | Game already running | Close the game or use `--force` | no game process remains |
| 9 | A required host tool is absent | Install the missing binary — the error names it (e.g. `Xvfb`, `glxinfo`, `xdpyinfo`, `dotnet`, `flock`, `sha256sum`) | the tool sweep passes |
| 10 | The virtual display could not be allocated or started | Xvfb failed to start — check that `Xvfb` is installed and that the process is not being killed by a memory limit; the script allocates the display itself via `-displayfd` | the display starts and the graphics probe runs |
| 11 | `steam_appid.txt` missing or wrong | Restore `steam_appid.txt` (content `413150`) in the game directory | the Steam prerequisite check passes |
| 12 | The build failed, or the deployed assembly is missing, older, or not byte-identical to the fresh build | Fix the build error; make sure the resolved mods path is writable so the deploy can take effect | the digests match and the run proceeds |
| 13 | The script itself broke (unhandled error, or a failed process-group assertion) | Read the run log — the ERR trap reports the script name, failing line and function. This is a bug in the script, not in your setup | the script no longer errors |
| 14 | Usage or CLI-argument error | Re-read `--help`; the message names the offending option and its expected form | the script accepts the arguments |
| 15 | The game exited without producing a SMAPI log | SMAPI re-asserted its dependency manifest and asked for a relaunch — re-run the SMAPI installer, then re-run the script | SMAPI writes a log |
| 16 | The resolved mods path, the mod folder, `manifest.json`, or its `UniqueID` is invalid | Check `--mods-path`; the mod folder must contain `LivingRoots/manifest.json` with `UniqueID` = `lfgranja.LivingRoots` | the manifest check passes |
| 8 | **Retired** (round 2) — superseded by the POSIX `128+N` interruption codes | — | never reused; a new code is always allocated above the highest published code |
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
2. **Exit code** — numeric code and the documented category (0=success, 1=game, 2=SMAPI, 3=graphics-unusable, 4=timeout, 5=mod failure, 6=lock, 7=process conflict, 9=missing dependency, 10=display allocation failure, 11=Steam prerequisite, 12=build/artifact failure, 13=internal error, 14=usage error, 15=no log produced, 16=mods path/manifest invalid, 129/130/143=interrupted).
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
| R-6 | No leftover temp directories | `ls -d /tmp/livingroots-smoke.* 2>/dev/null` | Empty — the EXIT trap removed them, or they were removed manually after a SIGKILL |
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
