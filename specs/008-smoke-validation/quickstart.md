# Quickstart: Smoke Validation Script

**Date**: 2026-10-19
**Context**: `specs/008-smoke-validation` — validation and run guide

---

## Prerequisites

Before running the smoke validation script, ensure:

1. **Stardew Valley** is installed via Steam on Linux at `~/.steam/steam/steamapps/common/Stardew Valley/`
2. **SMAPI** is installed and functional in the game directory (`smapi-internal/SMAPI.dll` present)
3. **Xvfb** is available on the system (`apt install xvfb` if missing)
4. **The Living Roots mod** is deployed to `.gamepath/Mods/LivingRoots/`
5. **Bash 4+** is available (standard on Linux)
6. **A Unicode normalizer** is available — `python3` (preferred) or `uconv` (fallback). The pre-flight phase fails closed with exit code `3` if neither is present, because path normalization (NFC) and symlink-safe file opens depend on it (FR-012 item 7)

---

## Running the Script

### Basic Execution

```bash
./scripts/smoke-validation.sh
```

**Expected outcome (success)**: Exit code `0`, message `SUCCESS: Mod loaded without errors.`

**Expected outcome (failure)**: Non-zero exit code with descriptive message following "What → Where → How to fix" pattern.

### Configurable Timeout

```bash
# Default 120-second timeout
./scripts/smoke-validation.sh

# Custom 180-second timeout
./scripts/smoke-validation.sh --timeout 180
```

### Force Mode (Kill Existing Game)

```bash
# If a Stardew Valley/SMAPI process is already running, kill it first
./scripts/smoke-validation.sh --force
```

---

## Validation Scenarios

### Test Case Documentation (IEEE 829-style)

Each scenario below is a test case with a fixed input, a fixed execution condition, and a defined expected output. **Actual output** and **Pass/Fail** are recorded at run time by the person executing the script (fill them in when the run happens, and keep them with the escalation evidence bundle).

| # | Test item | Input | Execution condition | Expected output | Actual output | Pass/Fail |
|---|-----------|-------|---------------------|-----------------|---------------|-----------|
| TC-1 | Mod loads successfully | Default install paths, mod deployed, no save loaded | Script run with no arguments; game launch to log check within 120s | Exit `0`; `SUCCESS: Mod loaded without errors.`; game and Xvfb terminated; no orphaned processes | _(record)_ | ☐ Pass ☐ Fail |
| TC-2 | Mod load-time error | Mod build with a failing `ModEntry.Entry()` | Script run with no arguments | Exit `5`; failure message with "What → Where → How to fix" and SMAPI log context | _(record)_ | ☐ Pass ☐ Fail |
| TC-3 | Game not installed | `--game-path` pointing at a non-existent directory | Script run with `--game-path <missing>` | Exit `1`; actionable pre-flight error naming the missing path | _(record)_ | ☐ Pass ☐ Fail |
| TC-4 | Game already running | An existing Stardew Valley/SMAPI process | Script run **without** `--force` | Exit `7`; error listing the conflicting PIDs | _(record)_ | ☐ Pass ☐ Fail |
| TC-5 | Concurrent execution prevention | A second instance already holds the lock | Second script instance started while the first is running | Exit `6`; lock contention error; second instance exits immediately | _(record)_ | ☐ Pass ☐ Fail |
| TC-6 | Timeout | Game that does not produce the SMAPI log | Script run with `--timeout 30`; no log produced within 30s | Exit `4`; timeout error with the log path and the `--timeout` hint | _(record)_ | ☐ Pass ☐ Fail |
| TC-7 | Interruption | Script in its wait/poll phase | User presses Ctrl+C (SIGINT) | Exit `130`; cleanup ran (game killed, Xvfb killed, X11 locks removed, temp files cleaned) | _(record)_ | ☐ Pass ☐ Fail |
| TC-8 | Pre-flight dependency missing | Environment without `python3`/`uconv` | Script run with the normalizer removed from `PATH` | Exit `3`; pre-flight error naming the missing normalizer and the fix | _(record)_ | ☐ Pass ☐ Fail |

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
  Where: /tmp/smoke-validation.lock
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
**Then**: EXIT trap runs cleanup (game killed, Xvfb killed, locks removed), exit code `130`

```bash
$ ./scripts/smoke-validation.sh
^C
INTERRUPTED: Cleaning up...
$ echo $?
130
```

---

## Troubleshooting

| Symptom | Likely Cause | Remediation |
|---------|-------------|-------------|
| Exit code 1 | Game not installed | Install Stardew Valley via Steam |
| Exit code 2 | SMAPI not installed | Run SMAPI installer in game directory |
| Exit code 3 | Xvfb not available | `sudo apt install xvfb` |
| Exit code 4 | Game slow to launch | Increase timeout: `--timeout 180` |
| Exit code 5 | Mod has load error | Check SMAPI log for error details |
| Exit code 6 | Another instance running | Wait for other instance to finish |
| Exit code 7 | Game already running | Close game or use `--force` |
| Exit code 130 | Script interrupted (Ctrl+C) | Normal — cleanup was run |
| Exit code 143 | Script terminated (SIGTERM) | Normal — cleanup was run |

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
2. **Exit code** — numeric code and the documented category (1=game, 2=SMAPI, 3=Xvfb, 4=timeout, 5=mod failure, 6=lock, 7=process conflict).
3. **Error text** — exact error message emitted by the script plus the last 20–30 lines of `SMAPI-latest.txt` (including Trace entries following any Error).
4. **Repro steps** — exact command invoked (with flags), environment (game path, SMAPI version, mod version/git commit), and a clean rerun result.
5. **System context** — conflicting PIDs (if any), lock file path, Xvfb display number, temp directory, OS/kernel.

### Evidence Template

```
Run ID:        <script run id / correlation id>
Started:       <ISO8601 UTC>
Failed at:     <ISO8601 UTC>
Exit code:     <n> (<category>)
Version:       <script version> / mod <git sha>
Command:       ./scripts/smoke-validation.sh <flags>
Game path:     <path>
SMAPI version: <version>
Last log line: <timestamp>
Error text:    <exact script error>
SMAPI excerpt: <last 20-30 lines>
Repro:         <steps>
```

### Escalation Rules

- Time-box each tier: 15 min at L1 before pulling in the mod developer; 30 min before involving SMAPI/runtime support.
- Isolate the fault boundary first (inside vs. outside the stack): check the vendor status page; if an external dependency is implicated, engage it in parallel.
- Never escalate without the evidence bundle above; incomplete escalations are returned to the sender.

---

## Resumption Requirements

A run that ended in failure (or was interrupted) can be re-run without manual repair **once all of the following are true**. If any precondition is false, the re-run will fail in the same way and is not worth attempting until it is fixed.

| # | Precondition | How to verify | Satisfied when |
|---|--------------|---------------|----------------|
| R-1 | No Stardew Valley/SMAPI process is still running | `pgrep -a StardewValley; pgrep -a SMAPI` | Both commands return nothing (or `--force` is used) |
| R-2 | No orphaned Xvfb holds the display | `pgrep -a Xvfb` | Returns nothing (the pre-flight also cleans orphaned instances with logging) |
| R-3 | No stale X11 lock files remain | `ls /tmp/.X*-lock` | Empty (the pre-flight removes stale locks belonging to dead PIDs) |
| R-4 | The smoke-test lock is free | The previous run has exited (the kernel releases the `flock` on process death, even after SIGKILL) | The lock is acquirable — a second run no longer exits with `6` |
| R-5 | No stale crash file can mask the result | The pre-flight deletes `SMAPI-crash.txt` before launch | The pre-flight reports the removal, or the file was already absent |
| R-6 | No leftover temp directories | `ls -d /tmp/livingroots-smoke.* 2>/dev/null` | Empty — the EXIT trap removed them, or they were removed manually after a SIGKILL |
| R-7 | The mod build under test is the intended one | The mod folder contains the build you meant to test | The SMAPI log shows the expected mod version |

**Rule:** the script is idempotent (FR-008) — a re-run under the same conditions produces the same verdict. If a re-run produces a *different* verdict than the previous run with identical inputs, one of the preconditions above was violated.

---

## Post-Run Verification

After a successful run, verify:

1. **No orphaned processes**: `pgrep -a StardewValley` should return nothing
2. **No orphaned Xvfb**: `pgrep -a Xvfb` should return nothing
3. **No X11 locks**: `ls /tmp/.X*-lock` should be empty
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
