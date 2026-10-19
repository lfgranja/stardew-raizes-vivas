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

## Requirements

### Functional Requirements

- **FR-001**: The script MUST launch the Stardew Valley game via SMAPI with the mod installed, using a virtual display (Xvfb) for headless operation
- **FR-002**: The script MUST wait for the game to initialize and produce a SMAPI log file before performing checks
- **FR-003**: The script MUST parse the SMAPI log file for errors attributable to the mod (LivingRoots). Only Error and Alert level entries are failure markers. WARN-level entries (if ever added to the mod) are logged but do not fail the smoke test.
- **FR-004**: The script MUST verify that the mod's data files are present in the mod's data folder (`<mod folder>/data/`) after the game has run with a save loaded — checking for `soil_health_data_*.json` and `composting_bins_*.json`. In the current implementation, the save check is always skipped because the automated script cannot load a save (SMAPI has no command-line save selection, and the mod is purely reactive — it only responds to SMAPI events after a save is loaded via the title screen UI). The title screen is the expected state. The save check is deferred until the basic smoke test works and the team has real usage data.
- **FR-005**: The script MUST terminate the game process immediately after the log check passes (success marker found), without leaving orphaned processes. The script sends SIGTERM first, waits a brief grace period for graceful exit, and escalates to SIGKILL only if the process is unresponsive. The script does NOT wait for the `Saved` event — the mod emits no observable log marker on `Saved`, making it impossible to detect externally.
- **FR-006**: The script MUST report success (exit code 0) when the log check passes. The log check uses an AND conjunction: success requires the success marker present AND no Error/Alert entries from the mod. The save check is deferred (see FR-004) and does not gate success in the current implementation.
- **FR-007**: The script MUST report failure (non-zero exit code) with a descriptive message when any check fails
- **FR-008**: The script MUST be idempotent — running it multiple times in sequence produces the same result
- **FR-009**: The script MUST accept a configurable timeout for game launch and initialization via command-line argument, with a 120-second default. The script polls the log continuously (no wasted time), and the timeout only matters for failure cases.
- **FR-010**: The script MUST clean up temporary resources (Xvfb process, lock files) on exit, including on failure

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
- **SC-004**: When the mod loads but does not persist data files to the mod's data folder, the script exits with a non-zero code and reports the missing data files. (Deferred — see FR-004. The save check is not implemented in the current version.)
- **SC-005**: The gate SC-008 (from spec 004) can be evaluated based on the script's result — a successful script run satisfies the "mod loads and persists data" portion of SC-008
- **SC-006**: The script is documented with clear instructions for local execution and troubleshooting

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

- Q: What is the exact pass/fail logic — must BOTH the success marker be present AND no Error/Alert entries exist, or is either condition alone sufficient? → A: AND conjunction. Success requires the success marker present AND no Error/Alert entries from the mod. The two signals are independent (SMAPI emits the marker; the mod's catch block emits errors on failure). Research supports AND for necessary conditions in critical gates (Baylor et al. 2012, ISTQB, Google, Microsoft). This eliminates false positives in both directions: marker-without-errors confirms clean load; errors-without-marker confirms detected failure.

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
