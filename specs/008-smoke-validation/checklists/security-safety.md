# Checklist: Security & Safety

**Purpose**: Validate that the script handles paths, processes, files, and injection vectors safely
**Created**: 2026-10-19
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [data-model.md](../data-model.md)

---

## 1. Path Traversal Prevention

- [x] 1.1 All filesystem paths constructed from external input are canonicalized with `realpath()` before use
- [x] 1.2 Canonicalized paths are verified to start with the expected base directory prefix (game install, mod dir, or save dir)
- [x] 1.3 Paths containing `../` sequences (literal or percent-encoded) after canonicalization are rejected
- [x] 1.4 Paths containing NUL bytes (`\0`, `%00`) are rejected — these truncate validation in C-based tools
- [x] 1.5 Unicode-normalize all path components (NFC or NFKC) before comparison to prevent homoglyph bypasses
- [x] 1.6 Absolute paths (starting with `/`) are rejected when a relative path within a known base is expected
- [x] 1.7 `O_NOFOLLOW` or equivalent is used when opening files to prevent symlink attacks on lock/temp files
- [x] 1.8 Game save directory paths are validated to resolve to the expected Stardew Valley save location
- [x] 1.9 Path sanitization failures are logged (without exposing the raw malicious input) for audit purposes
- [x] 1.10 The same sanitization rigor from `ModDataService.SanitizePathSegments()` is applied in the script

---

## 2. Process Management Security

- [x] 2.1 Process detection uses `pgrep -x` (exact match) or `pidof` — never `ps | grep` piping
- [x] 2.2 When killing by pattern, `pgrep -f` uses the most specific pattern possible (full binary path)
- [x] 2.3 After spawning the game, the actual PID is captured from `$!` and stored
- [x] 2.4 Before sending any signal to a stored PID, process identity is verified via `/proc/$PID/exe` or `/proc/$PID/comm`
- [x] 2.5 SIGTERM → grace period (15-30s) → SIGKILL escalation is implemented with configurable timeout
- [x] 2.6 SIGTERM/SIGINT traps forward the signal to child processes before exiting
- [x] 2.7 Process exit is verified using `kill -0 "$PID"` or `wait` after SIGKILL
- [x] 2.8 The entire process group is killed, not just the parent PID (prevents orphaned children)
- [x] 2.9 All process lifecycle events (spawn, signal, exit) are logged with timestamps
- [x] 2.10 `kill -9` is never used as the first signal — graceful SIGTERM is always attempted first

---

## 3. File Lock Security

- [x] 3.1 `flock` (kernel advisory lock) is used rather than PID files — eliminates stale lock problem
- [x] 3.2 `flock -n` (non-blocking) is used to fail fast if another instance holds the lock
- [x] 3.3 Lock file is stored in a directory with restrictive permissions (`chmod 0700`)
- [x] 3.4 Lock file is opened with a dedicated file descriptor (`exec 9>"$lockfile"`) held for script lifetime
- [x] 3.5 If PID files are used, stale lock detection verifies PID is alive AND `/proc/$PID/exe` matches expected binary
- [x] 3.6 If PID files are used, they are created with `O_CREAT|O_EXCL` (or `set -o noclobber`) to prevent symlink following
- [x] 3.7 Lock is tested under SIGKILL crash scenarios (kernel releases on process death)
- [x] 3.8 `rm` of lock files after `flock` is avoided — kernel releases lock on process exit; removal creates race window

---

## 4. Temporary File Security

- [x] 4.1 `mktemp` (or `mktemp -d` for directories) is used for all temporary files — never hardcoded paths
- [x] 4.2 `mktemp` uses a template with identifiable prefix: `mktemp "${TMPDIR:-/tmp}/livingroots-smoke.XXXXXX"`
- [x] 4.3 Temp file/directory creation success is verified: `[ -f "$tmpfile" ] || exit 1`
- [x] 4.4 `umask 077` is set at script start to ensure all created files are owner-only by default
- [x] 4.5 `trap cleanup EXIT` is defined BEFORE creating any temp files
- [x] 4.6 Cleanup trap includes SIGTERM and SIGINT: `trap 'rm -rf "$tmpdir"' EXIT INT TERM`
- [x] 4.7 Temp files containing sensitive data are in `mktemp -d` directories with `chmod 0700`
- [x] 4.8 Fixed filenames in `/tmp` are never used (vulnerable to symlink attacks)
- [x] 4.9 Temp file paths passed between processes are validated to be within the expected temp directory

---

## 5. Injection Prevention

- [x] 5.1 `set -euo pipefail` is at the top of the script
- [x] 5.2 Every variable expansion is quoted: `"$var"` — never bare `$var` or `${var}` in command position
- [x] 5.3 `--` is used before any variable representing a filename or path: `rm -- "$file"`, `cd -- "$dir"`
- [x] 5.4 All external input is validated against an allowlist (e.g., save directory names match `^[a-zA-Z0-9_-]+$`)
- [x] 5.5 `eval` is never used with any input originating from outside the script
- [x] 5.6 Shell metacharacters are rejected in untrusted input: `[[ "$data" =~ [;&|$\(\)\<\>] ]] && reject`
- [x] 5.7 Command strings are not constructed by concatenation — use arrays (`cmd=(binary "${args[@]}")`)
- [x] 5.8 `printf '%q'` is used to safely escape strings when passing through other shell-invoking commands
- [x] 5.9 Game-generated data passed to other commands is validated for shell metacharacters

---

## 6. Data Integrity During Termination

- [x] 6.1 SIGTERM (15) is always sent first — never SIGKILL (9) as the initial signal
- [x] 6.2 A grace period (15-30 seconds) is implemented after SIGTERM before escalating to SIGKILL
- [x] 6.3 After SIGTERM, process exit is polled using `kill -0 "$PID"` in a loop with sleep intervals
- [x] 6.4 Process exit is verified (not just that the signal was sent)
- [x] 6.5 After process exit, expected output files (SMAPI log) are verified to exist and have non-zero size
- [x] 6.6 A timeout prevents the script from hanging indefinitely if the game becomes unresponsive
- [x] 6.7 SIGKILL usage is logged (indicates graceful shutdown failed — potential data corruption risk)
- [x] 6.8 `timeout --signal=TERM --kill-after=30s` is used as a safety net for unresponsive processes
- [x] 6.9 Mod-specific save triggers that must be invoked before termination are documented

---

## 7. Xvfb Security

- [x] 7.1 Xvfb is started with `-nolisten tcp` to prevent remote TCP connections to the virtual display
- [x] 7.2 xauth-based access control (MIT-MAGIC-COOKIE-1) is used — never `xhost +` or `-ac`
- [x] 7.3 A unique, random cookie is generated for the Xvfb display (not hardcoded)
- [x] 7.4 Xauthority file is stored in a directory with `0700` permissions (`mktemp -d` + `chmod 0700`)
- [x] 7.5 `XAUTHORITY` environment variable points to the isolated auth file — does not rely on `~/.Xauthority`
- [x] 7.6 A non-default display number is used (e.g., `:99` or higher) to avoid conflicts
- [x] 7.7 Xvfb process receives the same SIGTERM → SIGKILL treatment as the game process
- [x] 7.8 Display `DISPLAY` value is not shared in world-readable logs or output

---

## 8. POSIX Filesystem Security

- [x] 8.1 `O_CREAT|O_EXCL` semantics (via `mktemp`, `set -o noclobber`) are used for all file creation in shared directories
- [x] 8.2 `access()` or `stat()` is never used to check before `open()` — this creates a TOCTOU window
- [x] 8.3 `lstat()` is used to verify a file is not a symlink before operating on it
- [x] 8.4 Directories used for lock/temp files have `0700` permissions
- [x] 8.5 On shared systems, `mktemp -d` creates a private working directory rather than writing to `/tmp` directly
- [x] 8.6 Symlinks in the game directory and mod directory are checked before reading/writing
- [x] 8.7 File ownership and permissions are verified before reading sensitive files

---

## 9. Secrets & Sensitive Data

- [x] 9.1 API keys, tokens, and passwords never appear in logs, error messages, or script output
- [x] 9.2 SMAPI log excerpts in failure reports are truncated to relevant lines only (no full path exposure)
- [x] 9.3 Error messages do not expose sensitive system details (constitution: "DO NOT expose raw exception messages to logs")
- [x] 9.4 Xauthority cookie values are never logged or displayed

---

## 10. Script Execution Safety

- [x] 10.1 Script runs as non-root user where possible (least privilege)
- [x] 10.2 Script is not SUID/SGID
- [x] 10.3 All error paths log meaningful messages without exposing secrets
- [x] 10.4 Script exits with appropriate exit codes (0 = success, non-zero = failure)
- [x] 10.5 Script is tested under concurrent execution attempts (lock verification)

---

## Anti-Patterns Verified Absent

| # | Anti-Pattern | Risk | Fix |
|---|-------------|------|-----|
| 1 | `eval "$user_input"` | Arbitrary code execution | Never use `eval` with external input |
| 2 | `rm /tmp/$filename` (fixed) | Symlink attack | `rm -- "$tmpfile"` from `mktemp` |
| 3 | `curl $url` (unquoted) | Argument injection | `curl -- "$url"` with validation |
| 4 | `ps aux \| grep "game"` | Fragile matching | `pgrep -x` or `pidof` |
| 5 | `kill -9 $pid` immediately | Data corruption | SIGTERM first, then SIGKILL |
| 6 | `echo $var` (unquoted) | Word splitting, globbing | `echo "$var"` (always quote) |
| 7 | `cd $dir` without validation | Path traversal | `cd -- "$dir"` + prefix check |
| 8 | PID file without verification | PID reuse kills wrong process | Verify `/proc/$PID/exe` before signaling |
| 9 | `xhost +` for X11 | Unauthorized display access | Use xauth/MIT-MAGIC-COOKIE-1 |
| 10 | Fixed temp filename | Predictable, symlink-vulnerable | `mktemp` with random suffix |
| 11 | `trap` defined after temp creation | Temp files left on crash | Define `trap` BEFORE `mktemp` |
| 12 | Script running as root | Privilege escalation | Run as least-privilege user |

---

## Sources

- OWASP OS Command Injection Defense Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/OS_Command_Injection_Defense_Cheat_Sheet.html
- CWE-22 (Path Traversal): https://cwe.mitre.org/data/definitions/22.html
- CWE-78 (OS Command Injection): https://cwe.mitre.org/data/definitions/78.html
- CWE-367 (TOCTOU): https://cwe.mitre.org/data/definitions/367.html
- CWE-377 (Insecure Temp File): https://cwe.mitre.org/data/definitions/377.html
- Wheeler's Secure Programs HOWTO: https://dwheeler.com/secure-programs/Secure-Programs-HOWTO/avoid-race.html
- mktemp security: https://www.netmeister.org/blog/mktemp.html
- X11 Security: https://www.x.org/Development/Documentation/Security
- tox-dev/filelock TOCTOU advisory: https://github.com/tox-dev/filelock/security/advisories/GHSA-w853-jp5j-5j7f
