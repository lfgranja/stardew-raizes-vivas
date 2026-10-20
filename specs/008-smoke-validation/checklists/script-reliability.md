# Checklist: Script Reliability & Resource Management

**Purpose**: Validate that the smoke validation script handles resources, processes, concurrency, and errors reliably across all execution paths
**Created**: 2026-10-19
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [data-model.md](../data-model.md)

---

## 1. Resource Cleanup (EXIT Trap & Signal Handling)

- [x] 1.1 Script registers `trap cleanup EXIT` as the first cleanup mechanism (not just INT/TERM)
- [x] 1.2 Cleanup function captures `$?` as its very first line (`local rc=$?`) before any other command
- [x] 1.3 Cleanup function ends with `exit "$rc"` to preserve the original exit code
- [x] 1.4 Signal traps (INT, TERM, HUP) reset themselves and re-raise: `trap - TERM; kill -TERM $$`
- [x] 1.5 All temporary files/dirs are created with `mktemp` and cleaned in EXIT trap
- [x] 1.6 `rm -rf` in cleanup is always quoted: `rm -rf -- "$tmpdir"` (prevents `rm -rf /` if var is empty)
- [x] 1.7 Background process PIDs are recorded (`pids+=("$!")`) and killed in cleanup
- [x] 1.8 `wait` is called after killing children to prevent zombie processes
- [x] 1.9 If script uses subshells, EXIT traps are registered in each subshell (resolved: N/A by bash semantics — bash does **not** propagate an inherited `EXIT` trap into subshells, so the parent trap is the single cleanup path and a subshell registering its own `EXIT` trap would double-clean; verified empirically on bash 5.3.9 across command substitution, parenthesised subshell, background job, pipeline stage and a function called inside a subshell — only the parent fired, once; only `set -E` propagates the `ERR` trap. spec.md FR-005 now states the rule: the `EXIT` trap is registered exactly once in the top-level shell, and a subshell owning a child process MUST clean that child up itself. This replaces the earlier, unsupported "no subshells per plan" rationale — a single-file script still uses command substitution, pipelines and background jobs)
- [x] 1.10 Cleanup function ignores further signals during cleanup: `trap '' INT TERM` (prevents interruption)
- [x] 1.11 Lock files are removed in EXIT trap, not just on normal exit (resolved: the flock file is NOT removed — the kernel releases the lock on process death and removing the file re-opens a race window; spec.md:90 FR-010 cleanup item (5) "the flock is auto-released by the kernel on process death", spec.md:162 clarification "The kernel automatically releases the lock on process death (even SIGKILL), eliminating stale-lock risk", quickstart.md:307 R-4, and security-safety.md:48 item 3.8 "rm of lock files after flock is avoided — removal creates race window" with research-bash-security.md:64 and :111 "Do not remove the lock file on exit"; FR-010 item (3) removes only the X11 lock files, which the spec keeps distinct from the flock)
- [x] 1.12 Xvfb process PID is recorded and killed in cleanup (not left running)

---

## 2. Process Lifecycle Management

- [x] 2.1 Script records child PID immediately after launch: `game_pid=$!`
- [x] 2.2 TERM trap forwards signal to child: `kill -TERM "$game_pid" 2>/dev/null || true`
- [x] 2.3 After forwarding TERM, script calls `wait "$game_pid"` to reap the child
- [x] 2.4 Escalation pattern: SIGTERM → sleep N → `kill -9` if still running
- [x] 2.5 Process tree termination uses session ID or process group (`kill -- -$PGID`) (resolved by decision — the mechanism is now specified: bash job control (`set -m`) is enabled around the launch only, so the game's process-group ID equals its PID and `kill -- -$!` signals exactly that group. A mandatory assertion precedes every group signal: the group ID to be signalled MUST differ from the script's own group ID and from `1`, because a plain `cmd &` leaves the child in the script's own group and `kill -- -$PGID` then terminates the script itself. Measured on bash 5.3.9 — FR-005, round-6 clarification)
- [x] 2.6 `kill -0 "$pid"` is used to check if process is still alive before escalating
- [x] 2.7 Xvfb is launched with `&` and its PID recorded for cleanup
- [x] 2.8 Script uses `wait` (not `sleep`) to block on child processes, enabling signal-responsive shutdown
- [x] 2.9 Exit code convention: 128 + signal number (143 for TERM, 130 for INT)
- [x] 2.10 If script is PID 1 (container), it explicitly handles SIGTERM (default action ignored for PID 1) (resolved: out of scope — plan.md:36 constraint "Not a container entrypoint — PID 1 signal semantics are out of scope" and spec.md:204 clarification "Is container / PID 1 execution in scope? → A: No. The script is a local-only maintainer tool; no Dockerfile or entrypoint exists. PID-1 signal handling is N/A (script-reliability 2.10)"; consistent with spec.md:92 FR-013 (local-first, no CI integration) and spec.md:246 assumption; the repo has no Dockerfile/docker-compose and .github/workflows/*.yml run on ubuntu-latest with no container or service)
- [x] 2.11 `timeout` command wraps long-running operations: `timeout --kill-after=30 300 cmd`
- [x] 2.12 All process lifecycle events are logged with timestamps (FR-014)

---

## 3. Concurrency Control

- [x] 3.1 Script uses `flock` (not PID file existence check) for mutual exclusion
- [x] 3.2 Lock is acquired non-blocking: `flock -n 9` (fails immediately if held)
- [x] 3.3 Lock file descriptor is held for script lifetime: `exec 9>"$lockfile"`
- [x] 3.4 Lock is released automatically on process exit (kernel closes fd)
- [x] 3.5 Lock file is in a writable, non-shared location (`/tmp`, `$XDG_RUNTIME_DIR`)
- [x] 3.6 Script exits with clear error message when lock cannot be acquired
- [x] 3.7 NFS warning: `flock` may not work reliably on NFS; use a local filesystem (lock path must be local /tmp or $XDG_RUNTIME_DIR)
- [x] 3.8 Xvfb display lock: ensured by Xvfb's built-in /tmp/.X{n}-lock + script flock + pre-flight cleanup
- [x] 3.9 Lock is tested under SIGKILL crash scenarios (kernel releases on process death)

---

## 4. Idempotency

- [x] 4.1 All directory creation uses `mkdir -p` (succeeds if exists)
- [x] 4.2 All file removal uses `rm -f` (succeeds if missing)
- [x] 4.3 Symlinks are replaced atomically: prefer `ln -sfn` (idempotent) or `ln -s … && mv -Tf …` (atomic rename); `rm -f && ln -s` is the documented fallback
- [x] 4.4 File appends are guarded: `grep -qxF "$line" "$file" || echo "$line" >> "$file"` (whole-line match)
- [x] 4.5 Output files are written atomically: write to `mktemp` in same dir, then `mv -f` to final path
- [x] 4.6 Script detects and cleans up resources from previous crashed runs (orphaned Xvfb, stale locks)
- [x] 4.7 Script can be run 10+ times in succession without error or state corruption
- [x] 4.8 No duplicate entries in logs, no accumulating temp files
- [x] 4.9 Game save data is backed up before test and restored after (or uses isolated save) (resolved: N/A — the script never loads a save, so no game save data can be read or mutated and there is nothing to back up; spec.md:82 FR-004 "the save check is always skipped (with a SKIPPED: warning) … The title screen is the expected state", spec.md:19 and spec.md:32-45 (User Story 2, P2, Deferred), spec.md:120-121 (SC-004/SC-005 deferred), plan.md:33 constraint "No save file loading (title screen is expected state)", quickstart.md:87 TC-1 "no save loaded", data-model.md:65 "Zero data files. This is valid and expected in the automated script"; the mod writes no save-scoped data until SaveLoaded (ModController.cs:946-1027) and never writes into the Stardew save file (spec.md:138))
- [x] 4.10 Xvfb display is released and can be re-acquired on re-run
- [x] 4.11 Script validates preconditions (game files exist, SMAPI installed) before making changes
- [x] 4.12 A failed `dotnet build` aborts the run before the game is launched — a failed build that leaves the previous DLL in place would otherwise produce a false pass (FR-016)
- [x] 4.13 The deployed assembly's SHA-256 digest is compared with the freshly built assembly's; a mismatch aborts the run before launch (FR-016, SC-012)
- [x] 4.14 The deploy target directory is removed before the rebuild, so a stale artifact cannot survive the build (FR-016)
- [x] 4.15 The run log is written outside the temporary directory (XDG state dir), so it survives the EXIT trap's temp cleanup and remains available for post-mortem review (FR-014)

---

## 5. Timeout & Readiness Polling

- [x] 5.1 Readiness is determined by polling a real signal (the current run's `Log started at {UTC} UTC` sentinel), not fixed sleep
- [x] 5.2 Every polling loop has a deadline: `deadline=$(( $(date +%s) + timeout ))`
- [x] 5.3 Polling loop checks deadline before each iteration and exits with error on timeout
- [x] 5.4 Process liveness is checked during polling: `kill -0 "$pid" 2>/dev/null || fail`
- [x] 5.5 Log-based readiness uses `tail -F -n +1` (or an equivalent follow) with a timeout wrapper — `-F` reopens the file by name so it survives SMAPI's unlink-and-recreate; `-n +1` is required because `-F` otherwise starts at EOF
- [x] 5.6 Xvfb readiness: check `/tmp/.X11-unix/X{n}` socket exists
- [x] 5.7 Game readiness: the log contains the current run's `Log started at {UTC} UTC` sentinel, and the mod is listed in `Loaded N mods:` and absent from `Skipped mods`
- [x] 5.8 Polling interval is appropriate: 0.5–1s for local processes
- [x] 5.9 On timeout, diagnostic info is dumped (last N lines of log, process status)
- [x] 5.10 `timeout --kill-after=30` sends SIGKILL 30s after SIGTERM if process refuses to die
- [x] 5.11 Script does not use `sleep N` as a substitute for readiness checking
- [x] 5.12 All signals are evaluated only against log lines **after** the current run's sentinel — a stale log from an earlier successful run cannot produce a false pass (FR-002, SC-011)
- [x] 5.13 The log file's inode is recorded before launch and verified to have changed once the log is recreated (belt-and-braces freshness assertion)
- [x] 5.14 A game process that exits with status `0` but produced no log is treated as failure, not success (SMAPI's post-update dependency re-assertion exits `0`)

---

## 6. Error Handling

- [x] 6.1 Script starts with `set -Eeuo pipefail` (or `set -uo pipefail` if exit codes inspected manually)
- [x] 6.2 `set -E` is included so ERR trap fires inside functions and subshells
- [x] 6.3 ERR trap prints line number, function name, and exit code: `BASH_LINENO`, `BASH_SOURCE`, `FUNCNAME`
- [x] 6.4 All error messages go to stderr: `echo "ERROR: ..." >&2`
- [x] 6.5 Error messages include script name and line number for debuggability (FR-014)
- [x] 6.6 Exit codes follow conventions: one distinct code per failure category, `4`=timeout, `128+N`=signal, and every child status is translated before the script exits (resolved by decision — FR-007 now states the general rule it previously applied only to GNU `timeout`'s `124`: no subprocess-specific status ever becomes the script's final code. Translation table: 124→4, 125→13, 126→9, 127→9, 134→2, 137→5/13, 128+N unchanged, everything else→13. Codes `12` (build/artifact failure) and `13` (internal script error) close the two uncovered categories)
- [x] 6.7 `local var; var=$(cmd)` pattern used (not `local var=$(cmd)`) to preserve exit code
- [x] 6.8 Commands that may fail under `set -e` are explicitly handled: `cmd || true` or `if cmd; then`
- [x] 6.9 `bash -n script.sh` is run as a syntax check before execution
- [x] 6.10 `shellcheck` is run on the script (CI or pre-commit)
- [x] 6.11 Script validates all external dependencies exist before use (`command -v X`)
- [x] 6.12 No raw exception messages or stack traces exposed to users (security)
- [x] 6.13 `trap - EXIT` is called inside cleanup to prevent recursive trap execution
- [x] 6.14 Empty array expansion `"${arr[@]}"` is guarded for Bash < 4.4 compatibility

---

## 7. Spec-Specific Reliability Requirements

- [x] 7.1 Pre-flight failures produce distinct exit codes per category (1=game, 2=SMAPI, 3=graphics-unusable, 4=timeout, 5=mod failure, 6=lock, 7=process conflict, 9=missing dependency, 10=display conflict, 11=Steam prerequisite, 12=build/artifact failure, 13=internal error) (resolved by decision — the categories are now disjoint by condition: `9` means a required host tool is **absent**, `3` means the graphics stack is **present but unusable**; the previously overlapping normalizer/Xvfb case is mapped to `9` and the full required-tool set is enumerated in FR-012 item 8 — FR-007, FR-012, round-6 clarification)
- [x] 7.2 Game crash during launch is detected via non-zero exit or `SMAPI-crash.txt` presence
- [x] 7.3 Mod name absent from SMAPi log is detected as mod-not-loaded failure
- [x] 7.4 `--force` flag kills existing instances with SIGTERM → SIGKILL escalation before launching
- [x] 7.5 Interruption (Ctrl+C) triggers EXIT trap: game killed, Xvfb killed, X11 locks removed, temp files cleaned
- [x] 7.6 Each cleanup step is isolated with `|| true` so one failure does not prevent others
- [x] 7.7 Cleanup order is reverse of allocation: game first, then Xvfb, then locks and temp files
- [x] 7.8 Failure reports include last 20-30 lines of SMAPI log with Trace-level entries following any Error
- [x] 7.9 A stale `SMAPI-latest.txt` and `SMAPI-crash.txt` are removed before launch; the crash file's removal also prevents SMAPI's blocking `Console.ReadKey()` from hanging the headless run
- [x] 7.10 The script writes one run log per run (never a shared append-only file), so concurrent runs cannot interleave their output
- [x] 7.11 The run log carries the run ID and the script `VERSION` on every line, so a failed run can be reconstructed from that file alone (FR-014, SC-014)
- [x] 7.12 Exit codes `9` (missing dependency), `10` (display conflict), and `11` (Steam prerequisite) are distinguished from the existing categories

---

## Common Pitfalls Verified Absent

| Pitfall | Status |
|---------|--------|
| `trap 'rm -rf $tmpdir' EXIT` (unquoted) | Must be quoted |
| Only trapping INT/TERM, not EXIT | EXIT trap required |
| Not capturing `$?` first in cleanup | Must capture first |
| `kill $PID` without `wait` | Zombie risk |
| Using `kill -9` as first resort | SIGTERM first |
| `sleep 5` instead of `wait` | Signal-responsive shutdown |
| Not recording `$!` after `&` | Must record PID |
| `pkill -f "stardew"` (overly broad) | Use exact matching |
| `local var=$(cmd)` masking exit code | Use `local var; var=$(cmd)` |
| Trap defined after resource creation | Define BEFORE creation |

---

## Sources

- Bash Beginners Guide §12.2 — Traps: https://tldp.org/LDP/Bash-Beginners-Guide/html/sect_12_02.html
- BashFAQ/045 — flock: https://mywiki.wooledge.org/BashFAQ/045
- SysTutorials — Process termination: https://www.systutorials.com/in-linux-how-to-kill-a-process-and-all-its-child-processes/
- vishnubob/wait-for-it: https://github.com/vishnubob/wait-for-it
- Google Shell Style Guide: https://google.github.io/styleguide/shellguide.html
- Fatih Arslan — Idempotent bash: https://arslan.io/2019/07/03/how-to-write-idempotent-bash-scripts/
- Paperless — Faux idempotence: https://paperless.blog/faux-idempotency
