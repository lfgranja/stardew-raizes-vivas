# Feature Specification: Smoke Validation Script

**Feature Branch**: `008-smoke-validation`

**Created**: 2026-10-19

**Status**: Draft

**Input**: Assessment `automate-ingame-testing` (intake + research + define + shape + decision, verdict: go, Option A)

## User Scenarios & Testing

### User Story 1 - Automated smoke validation of mod loading (Priority: P1)

As the maintainer, I want a script that launches the game with the mod installed and reports whether the mod loaded without errors, so that I can verify the mod works in the real game without manual play-through.

**Why this priority**: This is the immediate blocker — the mod has never been executed in the real game. Without this, no gameplay-affecting feature (spec 005 growth, quality, extra harvest) can be activated with confidence.

**Independent Test**: Can be fully tested by running the script and checking its exit code (0 = mod loaded without errors, non-zero = errors found). Delivers value by providing a repeatable, auditable signal that the mod loads correctly in the game. The save check (User Story 2) is deferred — see FR-004.

**Acceptance Scenarios**:

1. **Given** the game is installed and the mod is deployed in the mods folder, **When** the script runs, **Then** the script launches the game with SMAPI + mod, waits for initialization, checks the SMAPI log for errors, and exits with code 0 if no errors are found. The script terminates the game immediately after the log check passes (see FR-005).
2. **Given** the mod has a runtime error during load, **When** the script runs, **Then** the script detects the error in the SMAPI log and exits with a non-zero code and a descriptive message
3. **Given** the script has run successfully once, **When** the script runs again, **Then** it produces the same result (idempotent)

---

### User Story 2 - Mod data persistence verification (Priority: P2, Deferred)

As the maintainer, I want the script to verify that the mod's data files appear in the mod's data folder after the game loads with a save, so that I can confirm the mod's persistence layer works in the real game.

**Why this priority**: Loading without errors is necessary but not sufficient — the mod must also persist data correctly. This is the second half of the smoke test.

**Status**: Deferred. The automated script cannot load a save (SMAPI has no command-line save selection, and the mod is purely reactive). The title screen is the expected state. This user story will be implemented after the basic smoke test (User Story 1) works and the team has real usage data.

**Independent Test**: Can be fully tested by running the script and checking that the mod's data folder contains the expected JSON files (`soil_health_data_*.json`, `composting_bins_*.json`). Delivers value by confirming the mod's persistence pipeline works end-to-end.

**Acceptance Scenarios**:

1. **Given** the game has a save file loaded with the mod active, **When** the script completes its check, **Then** the mod's data folder contains at least one file matching the mod's data prefix
2. **Given** the mod failed to persist data files, **When** the script completes its check, **Then** the script reports the missing data files and exits with a non-zero code

---

### Edge Cases

- What happens when the game crashes during launch? The script should detect the crash (non-zero process exit or SMAPI-crash.txt) and report failure.
- What happens when the mods path is incorrect? The script should detect that the mod was not loaded (mod name absent from SMAPI log) and report failure.
- What happens when the save file does not exist yet? The save check is deferred (see FR-004). The title screen is the expected state in the automated script.
- What happens when the game takes longer than expected to load? The script should have a configurable timeout and report a timeout failure if the game does not produce a log within the expected window.
- What happens when the display (Xvfb) is not available? The script should detect this and report a clear error message.
- What happens when the game is already running? The script detects the existing process (via `pgrep`) and aborts with a descriptive error by default. An explicit `--force` flag kills the existing instance (SIGTERM then SIGKILL) before launching.
- What happens when another smoke test instance is already running? The script fails to acquire the `flock` and exits immediately with a descriptive error.
- What happens when a prerequisite is missing (game not installed, SMAPI not found, Xvfb not available)? The pre-flight phase detects it and emits an actionable error ("What → Where → How to fix") with a distinct exit code per failure category.
- What happens when the script is interrupted (Ctrl+C) during execution? The EXIT trap runs cleanup: game process killed, Xvfb killed, X11 locks removed, temp files cleaned. The script exits with code 130 (SIGINT) or 143 (SIGTERM).

## Requirements

### Functional Requirements

- **FR-001**: The script MUST launch the Stardew Valley game via SMAPI with the mod installed, using a virtual display (Xvfb) for headless operation. The script uses SMAPI's officially supported automation env vars: `SMAPI_NO_TERMINAL=true`, `SMAPI_USE_CURRENT_SHELL=true`, and `SMAPI_DEVELOPER_MODE=true` (enables TRACE-level logging for diagnostics). Xvfb MUST be started with `-nolisten tcp` on a non-default display (e.g., `:99`).
- **FR-002**: The script MUST wait for the game to initialize and produce a SMAPI log file before performing checks. The SMAPI log path defaults to `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` (Linux convention) and MAY be overridden via `--smapi-log`. Before launching, the script acquires an exclusive `flock` on a dedicated lock file (non-blocking) to prevent concurrent executions. If the lock cannot be acquired, the script exits immediately with a descriptive error.
- **FR-003**: The script MUST parse the SMAPI log file for errors attributable to the mod (LivingRoots). Only Error and Alert level entries are failure markers. WARN-level entries (if ever added to the mod) are logged but do not fail the smoke test. Trace-level entries are captured (via `SMAPI_DEVELOPER_MODE=true`) for diagnostic purposes only — they do not fail the test but provide exception type and HResult information in failure reports. The success marker match is case-sensitive and exact.
- **FR-004**: The script SHOULD verify that the mod's data files are present in the mod's data folder (`<mod folder>/data/`) after the game has run with a save loaded — checking for `soil_health_data_*.json` and `composting_bins_*.json`. In the current implementation the save check is always skipped (with a `SKIPPED:` warning) because the automated script cannot load a save (SMAPI has no command-line save selection, and the mod is purely reactive — it only responds to SMAPI events after a save is loaded via the title screen UI). The title screen is the expected state. The save check is deferred until the basic smoke test works and the team has real usage data; it does not gate success (see FR-006).
- **FR-005**: The script MUST terminate the game process immediately after the log check passes (success marker found), without leaving orphaned processes. The script launches the game in its own process group and sends SIGTERM to the entire group first, waits a configurable grace period (default 15s, via `--grace-period`) for graceful exit, and escalates to SIGKILL only if the process is unresponsive. The script does NOT wait for the `Saved` event — the mod emits no observable log marker on `Saved`, making it impossible to detect externally. The script traps SIGINT and SIGTERM to forward signals to child processes (game + Xvfb) and exits with code 128+N (130 for SIGINT, 143 for SIGTERM). An EXIT trap serves as universal safety net for all termination paths.
- **FR-006**: The script MUST report success (exit code 0) when the log check passes. The log check uses an AND conjunction: success requires the success marker present AND no Error/Alert entries from the mod. The save check is deferred (see FR-004) and does not gate success in the current implementation.
- **FR-007**: The script MUST report failure (non-zero exit code) with a descriptive message when any check fails. Failure messages MUST follow the "What → Where → How to fix" pattern: identify the failed component, the specific error, relevant log context (last 20-30 lines of the SMAPI log including Trace-level entries that follow any Error entry), and an actionable remediation hint. Exit codes MUST follow the sysexits.h convention with distinct codes per failure category (e.g., 1=game not found, 2=SMAPI not found, 3=Xvfb not available, 4=timeout, 5=mod load failure). The timeout category (`4`) is canonical: when the `timeout` utility is used to bound an operation, its native exit status `124` MUST be translated to `4` at the script boundary so that no subprocess-specific status ever becomes the script's final code.
- **FR-008**: The script MUST be idempotent — running it multiple times in sequence produces the same result
- **FR-009**: The script MUST accept a configurable timeout for game launch and initialization via command-line argument, with a 120-second default. The script polls the log continuously (no wasted time), and the timeout only matters for failure cases.
- **FR-011**: The script MUST detect an existing Stardew Valley/SMAPI process before launching. By default, the script aborts with a descriptive error listing the conflicting PIDs. An explicit `--force` flag kills existing instances (SIGTERM → SIGKILL escalation) before launching. This is a correctness requirement: the mod has no cross-process data file locking, so concurrent instances would corrupt data and truncate the SMAPI log.
- **FR-012**: The script MUST include a pre-flight phase before launching that validates: (1) the game installation exists at the expected path (configurable via `--game-path`, default `~/.steam/steam/steamapps/common/Stardew Valley/`), (2) SMAPI is installed and functional (smapi-internal/SMAPI.dll present), (3) Xvfb is available, (4) Mesa/OpenGL software-rendering libraries are available, (5) no orphaned Xvfb on the chosen display or stale X11 lock files remain from a previous crashed run (cleaned up with logging if found), (6) any pre-existing `SMAPI-crash.txt` is removed to avoid false positives, and (7) a Unicode normalizer is available (`python3`, or `uconv` as fallback) for NFC path normalization and symlink-safe file opens (`O_NOFOLLOW`). The pre-flight phase MUST fail closed when the normalizer is absent, with the same "What → Where → How to fix" pattern and the graphics/environment exit code.
- **FR-010**: The script MUST clean up temporary resources on exit, including on failure and interruption. Cleanup includes: (1) kill game process (SIGTERM → SIGKILL escalation), (2) kill Xvfb process, (3) remove X11 lock files (`/tmp/.X{n}-lock` and `/tmp/.X11-unix/X{n}`), (4) remove temp files/directories, (5) the flock is auto-released by the kernel on process death. Each cleanup step MUST be isolated (guarded with `|| true`) so one failure does not prevent others. The cleanup order is reverse of allocation: game first, then Xvfb, then locks and temp files.

- **FR-013**: The script MUST remain local-first: no CI integration and no machine-parseable (`--json`) output mode in the current scope. Proper exit codes (0/non-zero) are retained so future CI integration is straightforward.
- **FR-014**: The script MUST log every process lifecycle event with a timestamp and level prefix — spawn (game, Xvfb), every signal sent (including the PID and the signal), the grace period, the escalation to SIGKILL, and the observed exit — so that a failed run can be reconstructed from the log alone. On any unhandled error, the ERR trap MUST report the script name, the failing line number, the enclosing function name, and the exit code.
- **FR-015**: The script's `--help` output MUST document the automation environment variables it sets (`SMAPI_NO_TERMINAL=true`, `SMAPI_USE_CURRENT_SHELL=true`, `SMAPI_DEVELOPER_MODE=true`, and `DISPLAY`) in a dedicated "Environment (set automatically)" section, stating explicitly that they are managed by the script and are not user-configurable.
### Key Entities

- **SMAPI Log**: The log file produced by SMAPI at `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` (Linux). Entries follow `[HH:MM:SS LEVEL ModName] message` format with 6 levels (TRACE, DEBUG, INFO, WARN, ERROR, ALERT). The script parses this to detect the mod's success marker ("The 'Stardew Valley - Living Roots' mod was loaded successfully!") and any Error/Alert entries attributable to the mod. `SMAPI-crash.txt` (if present) overrides `SMAPI-latest.txt` and indicates failure.
- **Mod Data Files**: The mod persists data as JSON sidecar files in the mod's own folder at `<mod folder>/data/` (NOT inside the game's save file). Files are named `soil_health_data_<sanitizedSaveId>.json`, `composting_bins_<sanitizedSaveId>.json`, and `visualization_config_<sanitizedSaveId>.json`. The saveId is SMAPI's active save folder name (`Constants.SaveFolderName`), sanitized via Unicode normalization and filename sanitization. When no save is loaded (fresh install/title screen), zero data files is the expected valid state.
- **Game Process**: The Stardew Valley process launched via SMAPI. The script manages this process lifecycle (launch, wait, terminate). Termination happens immediately after the log check passes (success marker found). The script sends SIGTERM first, waits a brief grace period for graceful exit, and escalates to SIGKILL only if the process is unresponsive. The script does NOT wait for the `Saved` event — the mod emits no observable log marker on `Saved` (`ModController.cs:1089-1102`), making it impossible to detect externally.
- **Virtual Display**: The Xvfb display buffer that allows the graphical game to run headless. The script manages this resource lifecycle.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The script can be executed from a clean state (no prior game process, no Xvfb) and produces a deterministic pass/fail result within a configurable timeout (default: 120 seconds)
- **SC-002**: When the mod loads without errors, the script exits with code 0 and reports "SUCCESS" — this is the expected baseline after a correct mod build
- **SC-003**: When the mod has a load-time error, the script exits with a non-zero code and reports the specific error message from the SMAPI log
- **SC-004**: *(Deferred — see FR-004.)* When the mod loads but does not persist data files to the mod's data folder, the script exits with a non-zero code and reports the missing data files. Not implemented in the current version; the save check is always skipped with a `SKIPPED:` warning and does not gate success.
- **SC-005**: *(Deferred — see FR-004.)* The gate SC-008 (from spec 004) can be evaluated based on the script's result — a successful script run satisfies the "mod loads and persists data" portion of SC-008. In the current version only the "mod loads" portion is satisfied; the "persists data" portion remains unevaluated until the save check is implemented.
- **SC-006**: The script is documented with clear instructions for local execution and troubleshooting
- **SC-007**: When the game is already running, the script detects it and aborts with a descriptive error (or kills it with `--force`)
- **SC-008**: When another smoke test instance is already running, the script fails to acquire the flock and exits immediately
- **SC-009**: When a prerequisite is missing, the pre-flight phase detects it and emits an actionable error with a distinct exit code
- **SC-010**: When the script is interrupted (Ctrl+C), the EXIT trap runs full cleanup (game, Xvfb, X11 locks, temp files) and exits with code 128+N

## Clarifications

### Session 2026-10-19

- Q: How does the script determine which save file to check, and what happens when no save exists? → A: The script uses SMAPI's active save folder name (from `Constants.SaveFolderName`) to locate data files under the mod's data folder. When no save is loaded (fresh install/title screen), zero data files is the expected valid state — the save check is skipped with a warning, not reported as failure.

- Q: What format does the mod use to persist data, and where are the data files located? → A: The mod does NOT write into the Stardew Valley save file. It uses SMAPI's `IModHelper.Data.WriteJsonFile` to write JSON sidecar files in the mod's own folder at `<mod folder>/data/`. Files are named `soil_health_data_<sanitizedSaveId>.json`, `composting_bins_<sanitizedSaveId>.json`, and `visualization_config_<sanitizedSaveId>.json`.

- Q: What does the SMAPI log format look like and how can mod-specific errors be identified? → A: Log entries follow `[HH:MM:SS LEVEL ModName] message` with 6 levels (TRACE, DEBUG, INFO, WARN, ERROR, ALERT). Mod-specific errors are identified by the manifest name "Stardew Valley - Living Roots" in the bracket column. The success marker is "The 'Stardew Valley - Living Roots' mod was loaded successfully!" at Info level. Failure markers include Error/Alert level entries and absence of the success marker.

- Q: How can an external script detect that the game has launched and is ready for checks? → A: The script polls the SMAPI log file (`~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`) for the success marker line. The log is truncated at each launch, so the script waits for the file to be recreated and then polls its contents. `SMAPI-crash.txt` (if present) overrides `SMAPI-latest.txt` and indicates failure.

- Q: How can the game be terminated safely without corrupting data? → A: The safest termination point is after the `Saved` event fires (all data flushed to disk). The script should send SIGTERM first, wait a grace period (30-60s) for graceful exit, and only escalate to SIGKILL if the process is unresponsive. Never terminate during a save operation (risk of non-atomic JSON write corruption).

- Q: How does the game handle save files and which save should the script check? → A: The mod has no save selection logic — it uses SMAPI's `Constants.SaveFolderName` as the active save identifier. Each save gets its own data files (key includes saveId). When no save is loaded, zero data files is the valid fresh-install state — the save check is skipped with a warning, not reported as failure.

- Q: How should the smoke test trigger the mod's data persistence, given that the mod only persists data when a save is loaded? → A: The save check is conditional — if no save was loaded (title screen), the save check is skipped with a warning. The smoke test validates "mod loads without errors" (log check) unconditionally, and validates "data persistence" (save check) only when a save was loaded. This is the simplest approach and aligns with Option A (smallest thing that could work).

- Q: What is the appropriate timeout value for the game to launch and produce a ready signal? → A: The timeout is configurable via command-line argument with a 120-second default. The script polls the log continuously (no wasted time), and the timeout only matters for failure cases. A configurable timeout allows calibration after the first real run without code changes.

- Q: Is the smoke test script intended for local execution only, or should it be designed for CI integration from the start? → A: Local-first. The script is a simple bash script that runs on the maintainer's machine. CI integration is a separate future effort. The script uses proper exit codes (0/non-zero) so CI integration is straightforward later.

- Q: After the log check passes (mod loaded successfully detected), when should the script terminate the game process? → A: Terminate immediately after the log check passes (success marker found). The Info-level success marker at game launch confirms the mod has loaded. The script sends SIGTERM first, waits a brief grace period for graceful exit, and escalates to SIGKILL only if the process is unresponsive. No waiting for the `Saved` event — the mod emits no observable log marker on `Saved`, making it impossible to detect externally.

- Q: How should the script trigger save loading for the save check, given that SMAPI has no command-line save selection and the mod has no save selection logic? → A: Skip the save check entirely in the automated script. The title screen is the expected state — SMAPI has no `--load-save` argument, the mod is purely reactive, and automating title screen interaction would add significant complexity and fragility for a P2 feature. The save check is deferred until the basic smoke test works and the team has real usage data.

- Q: Should WARN-level log entries from the mod cause the smoke test to fail, or only Error/Alert? → A: Only Error/Alert entries cause failure. WARN-level entries (if ever added to the mod) are logged but do not fail the smoke test. Industry consensus supports binary pass/fail based on errors only (Fowler 2006, Google Testing Blog, Microsoft DevOps, Atlassian). The mod currently emits zero WARN messages; this clarification is future-proofing. The existing spec's "Failure markers include Error/Alert level entries" deliberately excludes WARN.

- Q: Should the script detect and handle an already-running game instance before launching? → A: Yes. The script detects an existing Stardew Valley/SMAPI process and aborts with a descriptive error by default. An explicit `--force` flag kills existing instances (SIGTERM then SIGKILL) before launching. This is a correctness requirement: the mod has no cross-process locking (all locks are process-local), so concurrent instances would corrupt data files (non-atomic JSON writes) and truncate the SMAPI log. (Internal: zero process detection in codebase; Research: Politowski et al. 2022, mc-runtime-test ephemeral pattern, flock gold standard)

- Q: Should the script prevent concurrent executions? → A: Yes. The script uses `flock` on a dedicated lock file to ensure only one instance runs at a time. The kernel automatically releases the lock on process death (even SIGKILL), eliminating stale-lock risk. This aligns with FR-010's "lock files" reference and FR-008's idempotency requirement. (Research: flock kernel-managed atomicity, Debian bug #1095028 xvfb-run 26-40% failure rate, cc-ci production pattern)

- Q: Should the script validate prerequisites before launching the game? → A: Yes. The script includes a pre-flight phase that validates: (1) game installation exists, (2) SMAPI is installed and functional, (3) Xvfb is available. Errors follow the "What → Where → How to fix" pattern with distinct exit codes per failure category (sysexits.h convention). This mirrors the existing CI preflight pattern (build_and_test.yml:87-109, 6-assembly check) and scripts/check-protected-paths.sh conventions (set -euo pipefail, table-driven checks, aggregated failure count). (Internal: mod has zero prerequisite self-validation; Research: IEEE 829-2008, xUnit fixture patterns, Meszaros 2007)

- Q: How should the script handle interruption (Ctrl+C/SIGTERM) during execution? → A: The script traps SIGINT and SIGTERM to forward signals to child processes (game + Xvfb) and exit with code 128+N (130 for SIGINT, 143 for SIGTERM). An EXIT trap serves as universal safety net for all termination paths. Cleanup includes: kill game process (SIGTERM→SIGKILL escalation), kill Xvfb, remove X11 lock files (/tmp/.X{n}-lock, /tmp/.X11-unix/X{n}), remove temp files. This follows the BashFAQ/062 canonical pattern and academic literature (Rasheed et al. 2022, 651 articles) confirming resource leakage as a top flakiness cause. (Internal: mod has zero signal handling; Research: BashFAQ/062, Mironsoft four-layer model, systemd pattern)

- Q: Should the script capture Trace-level SMAPI logs for diagnostics, given that some mod failures only appear at Trace level? → A: Yes. The script sets `SMAPI_DEVELOPER_MODE=true` to enable TRACE-level logging. Trace entries are used for diagnostics only (exception type + HResult) and do not act as failure markers — the pass/fail logic remains Error/Alert-only per FR-003. Failure reports include the relevant Trace lines following any Error entry. (Internal: DI wiring failures in ModEntry.Entry() log at Trace only — ModEntry.cs:143; Research: SMAPI official env vars, layered output pattern, Nguyen et al. 2019, Becker et al. 2019)

- Q: What is the exact pass/fail logic — must BOTH the success marker be present AND no Error/Alert entries exist, or is either condition alone sufficient? → A: AND conjunction. Success requires the success marker present AND no Error/Alert entries from the mod. The two signals are independent (SMAPI emits the marker; the mod's catch block emits errors on failure). Research supports AND for necessary conditions in critical gates (Baylor et al. 2012, ISTQB, Google, Microsoft). This eliminates false positives in both directions: marker-without-errors confirms clean load; errors-without-marker confirms detected failure.

### Session 2026-10-19 (checklist review)

- Q: Should the script keep the spec's sysexits-style distinct exit codes or adopt the generic 1=tool/2=validation convention? → A: Keep the spec's sysexits scheme (1=game, 2=SMAPI, 3=Xvfb, 4=timeout, 5=mod failure, 6=lock, 7=process conflict, 8=interrupted).
- Q: Should the game install path be hardcoded or configurable? → A: Hardcoded default with optional `--game-path` override.
- Q: What grace period after SIGTERM before SIGKILL? → A: Configurable, 15s default (`--grace-period`).
- Q: What Xvfb security posture? → A: Minimal — `-nolisten tcp` on a non-default display (e.g., `:99`); no xauth hardening for single-user local use.
- Q: Should pre-flight verify Mesa/OpenGL? → A: Yes, add the check.
- Q: How handle stale `SMAPI-crash.txt`? → A: Delete it before launch.
- Q: Clean up orphaned resources from prior crashed runs? → A: Yes, pre-flight cleanup with logging.
- Q: Support `--json`/CI mode? → A: No — defer; local-first. Keep proper exit codes.
- Q: Configurable SMAPI log path? → A: Fixed default with optional `--smapi-log` override.
- Q: Kill recorded PID only or process group on termination? → A: Entire process group (launch in own process group).

### Session 2026-10-19 (checklist review — round 2)

- Q: Should interrupted runs exit with a custom sysexits code 8 or follow the POSIX 128+N convention? → A: Follow POSIX 128+N — exit 130 for SIGINT and 143 for SIGTERM. `sysexits.h` defines no value below 64 (values start at EX__BASE=64), so a custom "8=interrupted" is neither a sysexits value nor POSIX-compliant. The shell's own convention (128+signal) is the standard and is what CI/tools expect. Supersedes the earlier "8=interrupted" note.
- Q: Grace period default 15s vs 30–60s? → A: 15s default (`--grace-period`), configurable. Termination happens at the title screen (no save loaded, no save flushing), so a short grace period is safe; 15s matches Docker's Linux default and keeps the script responsive. Configurable for slower machines.
- Q: Should `--json`/CI output be added? → A: No — defer (local-first, YAGNI). Exit codes suffice for future CI integration.
- Q: Single-file script vs sourced library modules? → A: Single-file bash script (plan's stated design). Sourced modules are out of scope for a local smoke test.
- Q: How handle SMAPI "mod marked failed but game still launches"? → A: No special handling — the AND-conjunction (success marker present AND no Error/Alert entries) already fails the run on any mod Error/Alert, including deferred-load failures.
- Q: Mesa/OpenGL pre-flight failure exit code? → A: Reuse exit code 3 (graphics environment unavailable), grouping it with the Xvfb/graphics pre-flight category.
- Q: Mod-name grep case sensitivity? → A: Case-sensitive exact match against the manifest name "Stardew Valley - Living Roots", consistent with the case-sensitive success marker.
- Q: Skipped checks labeling? → A: Skipped checks are labeled `SKIPPED:` and counted separately from passes/failures, per operational-readiness 1.10.
- Q: FR-004 save check wording? → A: Changed from MUST to SHOULD and marked deferred; skipped with a `SKIPPED:` warning when no save is loaded; does not gate success.
- Q: FR-010/FR-013 ordering? → A: Renumbered so FR-010 (cleanup) precedes FR-013 (local-first scope); both on separate lines.

### Session 2026-10-19 (checklist review — round 3, final)

All items below were decided by the user after evidence-backed research.

- Q: Should the spec be a technology-agnostic document or a technical contract? → A: Technical contract. The spec intentionally documents implementation-level contracts (bash, Xvfb, SMAPI, signals, exit codes) because it is a maintainer-facing CLI tool spec. The `requirements.md` checklist criteria "no implementation details", "written for non-technical stakeholders", "success criteria are technology-agnostic", and "no implementation details leak into specification" are waived for this feature, and the checklist Notes were corrected accordingly.
- Q: SC-004/SC-005 vs. the deferred save check? → A: Both are marked deferred in-line, matching FR-004/FR-006. The save check stays out of scope; the "persists data" half of SC-005 remains unevaluated until it is implemented.
- Q: Is container / PID 1 execution in scope? → A: No. The script is a local-only maintainer tool; no Dockerfile or entrypoint exists. PID-1 signal handling is N/A (script-reliability 2.10).
- Q: Unicode normalization (NFC) and `O_NOFOLLOW` need an external helper — add a dependency? → A: Yes. `python3` (with `uconv` as fallback) is a required pre-flight dependency (FR-012 item 7); the pre-flight fails closed when it is missing. NFC is used to match the mod's `FormC` sanitization.
- Q: Do CLI arguments with documented defaults count as "externalized configuration"? → A: Yes. The script is configured via CLI arguments with documented defaults; no config file or env-var mechanism is required. (operational-readiness 6.6 reworded accordingly.)
- Q: Should all process lifecycle events be logged? → A: Yes — new FR-014 requires timestamped logging of spawn/signal/grace/SIGKILL/exit, and requires the ERR trap to report script name, line number, function name, and exit code.
- Q: Should the `--help` output document the SMAPI environment variables? → A: Yes — new FR-015 requires a dedicated "Environment (set automatically)" section.
- Q: Exit code for timeout — canonical `4` or GNU `timeout`'s `124`? → A: `4` is canonical. Any `124` produced by the `timeout` utility is translated to `4` at the script boundary; subprocess-specific statuses never become the script's final code (FR-007).
- Q: Should resumption requirements and a formal test-case format be documented? → A: Yes — both added to `quickstart.md` (Resumption Requirements section and per-scenario test-case tables with input, execution condition, expected output, actual output, pass/fail).
- Q: Should `SMAPI-crash.txt` timestamps be compared against the run window? → A: No. Stale-crash cleanup before launch (FR-012 item 6) already guarantees relevance; the timestamp comparison is N/A (smapi-integration 10.4).

## Assumptions
- The game (Stardew Valley) is installed via Steam on Linux at `~/.steam/steam/steamapps/common/Stardew Valley/`
- SMAPI is installed and functional in the game directory
- Xvfb is available on the system (installable via `apt install xvfb` if missing)
- The mod is deployed to `.gamepath/Mods/LivingRoots/` (the existing GamePath setup)
- The SMAPI log location follows the Linux convention: `~/.config/StardewValley/ErrorLogs/`
- The mod persists data under keys prefixed with `soil_health_data_` and `composting_bins_` (per `ModConstants.KeyPrefix` and `CompostingBinKeyPrefix`), stored as JSON files in the mod's data folder with the save folder name sanitized (Unicode FormC + FileNameSanitizationService) as part of the filename
- The game can be launched headless via Xvfb without graphical issues
- The game process can be terminated via SIGTERM/SIGKILL immediately after the log check passes (success marker found) — the script does not wait for the `Saved` event because the mod emits no observable log marker on `Saved`
- The script is intended for local execution by the maintainer, not CI integration (CI may be added later). The script uses proper exit codes (0/non-zero) so future CI integration is straightforward.
