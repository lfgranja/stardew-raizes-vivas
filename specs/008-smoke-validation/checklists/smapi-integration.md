# Checklist: SMAPI Integration & Log Analysis

**Purpose**: Validate that the script correctly interacts with SMAPI, accurately parses logs, and properly detects mod loading outcomes
**Created**: 2026-10-19
**Feature**: [spec.md](../spec.md) · [research.md](../research.md)

---

## 1. SMAPI Automation Environment Variables

- [x] 1.1 Set `SMAPI_NO_TERMINAL=true` to prevent terminal launch in headless environment
- [x] 1.2 Set `SMAPI_USE_CURRENT_SHELL=true` to capture all output from the launching shell
- [x] 1.3 Set `SMAPI_DEVELOPER_MODE=true` to enable TRACE-level logging for diagnostics
- [x] 1.4 All three environment variables are exported before game launch
- [x] 1.5 `DISPLAY` environment variable is set to the Xvfb display before launch
- [ ] 1.6 Environment variables are documented in `--help` output

---

## 2. SMAPI Log File Detection

- [x] 2.1 Script reads log from `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` (Linux convention)
- [x] 2.2 Script checks for `SMAPI-crash.txt` first (presence indicates crash → immediate failure)
- [x] 2.3 If `SMAPI-crash.txt` is present, it overrides `SMAPI-latest.txt` as the failure source
- [x] 2.4 Script waits for log file to be created/recreated before polling (SMAPI truncates at launch)
- [x] 2.5 Log file path is configurable (not hardcoded) for non-standard installations
- [x] 2.6 Script handles the case where log file never appears (timeout → failure)

---

## 3. Log Format Parsing

- [x] 3.1 Log entries are parsed using the `[HH:MM:SS LEVEL ModName] message` format
- [x] 3.2 Mod-specific entries are filtered by manifest name `Stardew Valley - Living Roots` in the bracket column
- [x] 3.3 All 6 log levels are recognized: TRACE, DEBUG, INFO, WARN, ERROR, ALERT
- [x] 3.4 Only ERROR and ALERT level entries are treated as failure markers
- [x] 3.5 WARN-level entries are logged but do NOT fail the smoke test
- [x] 3.6 TRACE-level entries are captured for diagnostics only and do NOT fail the test
- [x] 3.7 Parsing handles multi-line entries (stack traces following Error lines)
- [x] 3.8 Parsing handles entries with special characters in messages (paths, exception names)

---

## 4. Success Marker Detection

- [x] 4.1 Success marker string is: `The 'Stardew Valley - Living Roots' mod was loaded successfully!`
- [x] 4.2 Success marker is searched at INFO level (not other levels)
- [x] 4.3 Absence of success marker is treated as a failure condition
- [x] 4.4 Success marker detection is case-sensitive (exact match)
- [x] 4.5 Success marker is searched only in mod-specific entries (not SMAPI core messages)

---

## 5. Pass/Fail Logic (AND Conjunction)

- [x] 5.1 Pass requires BOTH: success marker present AND no Error/Alert entries from the mod
- [x] 5.2 Error/Alert entries without success marker → failure (detected load error)
- [x] 5.3 Success marker without Error/Alert entries → pass (clean load)
- [x] 5.4 Success marker WITH Error/Alert entries → failure (errors override marker)
- [x] 5.5 No success marker AND no Error/Alert entries → failure (mod never loaded)
- [x] 5.6 The AND conjunction eliminates false positives in both directions
- [x] 5.7 Save check is deferred (FR-004) and does NOT gate success in current implementation

---

## 6. SMAPI Mod Loading Lifecycle

- [x] 6.1 Script allows sufficient time for SMAPI to load all mods (polls, doesn't assume instant readiness)
- [x] 6.2 Script detects when SMAPI has finished loading mods (success marker appears after all Entry() methods)
- [x] 6.3 Script distinguishes between "mod failed to load" (Error entries) and "mod not found" (absent from log)
- [x] 6.4 Script handles SMAPI's automatic error recovery (mod marked failed but game still launches)
- [x] 6.5 Script does not confuse SMAPI core messages with mod-specific messages

---

## 7. Headless Game Testing via Xvfb

- [x] 7.1 Xvfb is started on a specific display (e.g., `:99`) before game launch
- [x] 7.2 `DISPLAY` environment variable is exported to the game process
- [x] 7.3 Xvfb readiness is confirmed before game launch (socket exists or `xdpyinfo` succeeds)
- [x] 7.4 Mesa/OpenGL libraries are available for software rendering (pre-flight check)
- [x] 7.5 Game launch to log check typically completes in 15-30 seconds on modern hardware
- [x] 7.6 Xvfb process is terminated during cleanup (same SIGTERM → SIGKILL pattern as game)
- [x] 7.7 X11 lock files (`/tmp/.X{n}-lock` and `/tmp/.X11-unix/X{n}`) are removed during cleanup

---

## 8. Termination Strategy

- [x] 8.1 Game is terminated immediately after log check passes (success marker found)
- [x] 8.2 Script does NOT wait for the `Saved` event (no observable log marker exists — `ModController.cs:1089-1102`)
- [x] 8.3 SIGTERM is sent first, followed by a brief grace period for graceful exit
- [x] 8.4 SIGKILL is used only if the process is unresponsive after the grace period
- [x] 8.5 Termination happens after the log check, not before (log check is the validation)

---

## 9. Mod-Specific Error Detection

- [ ] 9.1 Script greps log file for mod name `Stardew Valley - Living Roots` (case-insensitive option)
- [x] 9.2 Script searches for `[ERROR]` and `[ALERT]` entries associated with the mod
- [x] 9.3 Script checks for "failed to load" status for the mod
- [x] 9.4 Script looks for exception stack traces mentioning mod types
- [x] 9.5 Failure reports include relevant Trace lines following any Error entry (20-30 lines context)
- [x] 9.6 Script distinguishes mod errors from SMAPI core errors and other mods' errors

---

## 10. Crash Detection

- [x] 10.1 `SMAPI-crash.txt` existence is checked before parsing `SMAPI-latest.txt`
- [x] 10.2 If `SMAPI-crash.txt` is present, script parses it for exception details and stack trace
- [x] 10.3 Game crash during launch is detected via non-zero process exit code
- [ ] 10.4 Crash time is compared with test run time to confirm relevance
- [x] 10.5 Any `SMAPI-crash.txt` is treated as a test failure (overrides log check)
- [x] 10.6 Old `SMAPI-crash.txt` is removed before test run to avoid false positives

---

## Common Failure Modes Detected

| Failure Mode | Detection Method | Log Indicator |
|--------------|------------------|---------------|
| Manifest validation error | Check mod list | "failed to load" + manifest error |
| Missing dependency | Check mod warnings | Warning about missing mod |
| Incompatible version | Version check | Compatibility warning |
| Assembly loading error | Load status | Assembly load failure message |
| Mod crash during entry | Exception trace | Exception in mod entry point |
| Game version mismatch | Version check | Version compatibility warning |
| SMAPI version mismatch | Version check | SMAPI version warning |
| Corrupted save data | Save load errors | Save-related error messages |

---

## Sources

- SMAPI Technical Documentation: https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/smapi.md
- SMAPI Log Parser: https://smapi.io/log
- SMAPI README: https://github.com/Pathoschild/SMAPI/blob/develop/docs/README.md
- SMAPI Release Notes: https://github.com/Pathoschild/SMAPI/blob/develop/docs/release-notes.md
- Stardew Valley Wiki (Modding): https://stardewvalleywiki.com/Modding:Index
