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
- [ ] 1.9 If script uses subshells, EXIT traps are registered in each subshell (N/A: single-file script, no subshells per plan)
- [x] 1.10 Cleanup function ignores further signals during cleanup: `trap '' INT TERM` (prevents interruption)
- [ ] 1.11 Lock files are removed in EXIT trap, not just on normal exit (decided: do NOT remove — race window; kernel releases on exit)
- [x] 1.12 Xvfb process PID is recorded and killed in cleanup (not left running)

---

## 2. Process Lifecycle Management

- [x] 2.1 Script records child PID immediately after launch: `game_pid=$!`
- [x] 2.2 TERM trap forwards signal to child: `kill -TERM "$game_pid" 2>/dev/null || true`
- [x] 2.3 After forwarding TERM, script calls `wait "$game_pid"` to reap the child
- [x] 2.4 Escalation pattern: SIGTERM → sleep N → `kill -9` if still running
- [x] 2.5 Process tree termination uses session ID or process group (`kill -- -$PGID`)
- [x] 2.6 `kill -0 "$pid"` is used to check if process is still alive before escalating
- [x] 2.7 Xvfb is launched with `&` and its PID recorded for cleanup
- [x] 2.8 Script uses `wait` (not `sleep`) to block on child processes, enabling signal-responsive shutdown
- [x] 2.9 Exit code convention: 128 + signal number (143 for TERM, 130 for INT)
- [ ] 2.10 If script is PID 1 (container), it explicitly handles SIGTERM (default action ignored for PID 1) (N/A: container usage out of scope — local-only maintainer tool, no Dockerfile/entrypoint)
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
- [ ] 4.9 Game save data is backed up before test and restored after (or uses isolated save) (N/A: save check deferred, title screen expected)
- [x] 4.10 Xvfb display is released and can be re-acquired on re-run
- [x] 4.11 Script validates preconditions (game files exist, SMAPI installed) before making changes

---

## 5. Timeout & Readiness Polling

- [x] 5.1 Readiness is determined by polling a real signal (log pattern), not fixed sleep
- [x] 5.2 Every polling loop has a deadline: `deadline=$(( $(date +%s) + timeout ))`
- [x] 5.3 Polling loop checks deadline before each iteration and exits with error on timeout
- [x] 5.4 Process liveness is checked during polling: `kill -0 "$pid" 2>/dev/null || fail`
- [x] 5.5 Log-based readiness uses `tail -F` with timeout wrapper
- [x] 5.6 Xvfb readiness: check `/tmp/.X11-unix/X{n}` socket exists
- [x] 5.7 Game readiness: SMAPI log contains the success marker line
- [x] 5.8 Polling interval is appropriate: 0.5–1s for local processes
- [x] 5.9 On timeout, diagnostic info is dumped (last N lines of log, process status)
- [x] 5.10 `timeout --kill-after=30` sends SIGKILL 30s after SIGTERM if process refuses to die
- [x] 5.11 Script does not use `sleep N` as a substitute for readiness checking

---

## 6. Error Handling

- [x] 6.1 Script starts with `set -Eeuo pipefail` (or `set -uo pipefail` if exit codes inspected manually)
- [x] 6.2 `set -E` is included so ERR trap fires inside functions and subshells
- [x] 6.3 ERR trap prints line number, function name, and exit code: `BASH_LINENO`, `BASH_SOURCE`, `FUNCNAME`
- [x] 6.4 All error messages go to stderr: `echo "ERROR: ..." >&2`
- [x] 6.5 Error messages include script name and line number for debuggability (FR-014)
- [x] 6.6 Exit codes follow conventions: canonical 4=timeout (GNU timeout's 124 translated to 4 at boundary), 128+N=signal, plus sysexits 1-7 (research resolved)
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

- [x] 7.1 Pre-flight failures produce distinct exit codes per category (1=game, 2=SMAPI, 3=Xvfb)
- [x] 7.2 Game crash during launch is detected via non-zero exit or `SMAPI-crash.txt` presence
- [x] 7.3 Mod name absent from SMAPi log is detected as mod-not-loaded failure
- [x] 7.4 `--force` flag kills existing instances with SIGTERM → SIGKILL escalation before launching
- [x] 7.5 Interruption (Ctrl+C) triggers EXIT trap: game killed, Xvfb killed, X11 locks removed, temp files cleaned
- [x] 7.6 Each cleanup step is isolated with `|| true` so one failure does not prevent others
- [x] 7.7 Cleanup order is reverse of allocation: game first, then Xvfb, then locks and temp files
- [x] 7.8 Failure reports include last 20-30 lines of SMAPI log with Trace-level entries following any Error

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
