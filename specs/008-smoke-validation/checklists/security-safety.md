# Checklist: Security & Safety

**Purpose**: Validate that the script handles paths, processes, files, and injection vectors safely
**Created**: 2026-10-19
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [data-model.md](../data-model.md)

---

## 1. Path Traversal Prevention

- [x] 1.1 All filesystem paths constructed from external input are canonicalized with `realpath()` before use — grounded in `plan.md`'s Constitution Compliance (Security-First: "The script handles filesystem paths … and must sanitize/validate all paths"), which is the only normative hook for this rule; it is not yet an FR
- [ ] 1.2 Canonicalized paths are verified to start with the expected base directory prefix (game install, mod dir, or save dir) — **UNRESOLVED**: there is no single base. `--game-path`, `--mods-path` and `--smapi-log` each accept an absolute path (FR-017 explicitly allows an absolute `--mods-path`, and TC-14 tests one), so a "must be under the game dir" prefix check would reject valid input. The save dir is out of scope (FR-004 deferred). The evidence-backed resolution is that only paths the script *constructs* are prefix-checked, and user-supplied roots are their own bases — but this is pending the round-7 security-posture decision.
- [x] 1.3 Paths containing `../` sequences are handled by canonicalization: `realpath` resolves them away, so the residual check is a **component-boundary prefix test** (`case "$real" in "$base"/*)`), never a reject-on-`..` test — after canonicalization the string *cannot* contain `..`, so such a rule is dead code. The percent-encoded variant is **not applicable**: the CLI surface is `--game-path`, `--mods-path`, `--smapi-log`, `--timeout`, `--grace-period`, `--force` — no URL or percent-decoding exists anywhere in the design
- [x] 1.4 Paths containing NUL bytes (`\0`, `%00`) are rejected — these truncate validation in C-based tools
- [x] 1.5 Unicode-normalize all path components (NFC or NFKC) before comparison to prevent homoglyph bypasses (NFC via python3/uconv, required pre-flight dep — FR-012 item 7; fail closed if absent)
- [ ] 1.6 Absolute paths (starting with `/`) are rejected when a relative path within a known base is expected — **CONTRADICTS the design**: every real input must accept an absolute path — `--game-path` defaults to `~/.steam/steam/steamapps/common/Stardew Valley/` (absolute), `--smapi-log` defaults to `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` (absolute), and FR-017 permits an absolute `--mods-path` (TC-14 exercises one). The only field where a relative path within a known base is expected would be a save identifier — and the save check is deferred (FR-004), exactly as item 1.8 already notes. As written this item would reject the script's own defaults. Pending the round-7 security-posture decision.
- [ ] 1.7 `O_NOFOLLOW` or equivalent is used when opening files to prevent symlink attacks on lock/temp files (python3 helper + 0700 dir + lstat + ownership — FR-012 item 7) — **UNRESOLVED**: FR-012 item 7 names only "symlink-safe file opens (`O_NOFOLLOW`)" and the normalizer; the "0700 dir + lstat + ownership" triple comes from `research-bash-security.md`, and `O_CREAT`/`O_EXCL` appear **nowhere** in the spec. The item does not say *which* opens are protected (lock file, run log, SMAPI log, or all three), and bash exposes no `O_NOFOLLOW`, so the helper's interface (argv, flags, how the fd is returned to bash) is unspecified. Note also the tension with items 8.2/8.3. Pending the round-7 security-posture decision.
- [x] 1.8 Game save directory paths are validated to resolve to the expected Stardew Valley save location (applies when save check is implemented)
- [x] 1.9 Path sanitization failures are logged (without exposing the raw malicious input) for audit purposes
- [ ] 1.10 The same sanitization rigor from `ModDataService.SanitizePathSegments()` is applied in the script (ported as a `sanitize_path_segments()` function inside the single-file script) — **UNRESOLVED (YAGNI)**: the script has **no save-id consumer** — the save check is deferred (FR-004) — so a ported `sanitize_path_segments()` has no call site in the specified design, contradicting `plan.md`'s YAGNI constraint ("The script implements only what the spec requires"). The source research also recommends keeping it in a sourced library, which the single-file decision overrode. Pending the round-7 security-posture decision.

---

## 2. Process Management Security

- [x] 2.1 Process detection identifies the game by executable path, never by `ps | grep` piping (resolved by decision — a candidate PID qualifies only when `/proc/$PID/exe` resolves under the resolved game directory (`StardewModdingAPI` or `StardewValley`). `pgrep -x` MUST NOT be used for the apphost: the kernel's process name is truncated to 15 characters and `StardewModdingAPI` is 17, so an exact-name match can never succeed — verified locally against procps-ng 4.0.6, which refuses the pattern outright. `pidof` matches on the untruncated name and on `/proc/$PID/exe` and is the acceptable alternative — FR-011, round-6 clarification)
- [x] 2.2 When killing by pattern, the most specific identifier possible is used (resolved by decision — identification is by `/proc/$PID/exe` restricted to the resolved game path, not by a command-line pattern. `pgrep -f` over-matches deterministically: it was observed matching the invoking shell whose command line merely contained the target name — FR-011)
- [x] 2.3 After spawning the game, the actual PID is captured from `$!` and stored
- [x] 2.4 Before sending any signal to a PID, process identity is verified via `/proc/$PID/exe` (resolved by decision — the rule now has FR backing and an explicit scope. Any PID **discovered by scanning the process table** MUST be verified before it is signalled; the script's own spawned PID needs no check, because an unreaped child stays a zombie and the kernel does not recycle a zombie PID (documented in `pidfd_open(2)` NOTES; GNU `timeout` relies on the same invariant). `/proc/$PID/comm` is no longer offered as an alternative — it is a 15-character truncated, process-writable name, not an identity token — FR-011)
- [x] 2.5 SIGTERM → grace period (15-30s) → SIGKILL escalation is implemented with configurable timeout — the grace period is `--grace-period`, default **15 s** (FR-005); the "15-30s" range is a description of that default plus headroom, not a second fixed value
- [x] 2.6 SIGTERM/SIGINT traps forward the signal to child processes before exiting
- [x] 2.7 Process exit is verified using `kill -0 "$PID"` or `wait` after SIGKILL
- [x] 2.8 The entire process group is killed, not just the parent PID (prevents orphaned children)
- [x] 2.9 All process lifecycle events (spawn, signal, exit) are logged with timestamps
- [x] 2.10 `kill -9` is never used as the first signal — graceful SIGTERM is always attempted first

---

## 3. File Lock Security

- [x] 3.1 `flock` (kernel advisory lock) is used rather than PID files — eliminates stale lock problem
- [x] 3.2 `flock -n` (non-blocking) is used to fail fast if another instance holds the lock
- [x] 3.3 Lock file is stored in a directory with restrictive permissions (`chmod 0700`) at a **stable, per-user** path — `${XDG_RUNTIME_DIR}/livingroots-smoke.lock`, falling back to `${XDG_STATE_HOME:-$HOME/.local/state}/livingroots-smoke/` — and is **not** inside a per-run `mktemp -d` directory: a unique directory per run means two concurrent instances lock different inodes and mutual exclusion is zero, which would make SC-008, TC-5 and exit `6` unreachable. FR-002 "Lock file (MUST)"
- [x] 3.4 Lock file is opened with a dedicated file descriptor (`exec 9<>"$lockfile"`) held for script lifetime — read-write, create-if-absent; the `>` form is forbidden because it truncates and follows symlinks
- [x] 3.5 If PID files are used, stale lock detection verifies PID is alive AND `/proc/$PID/exe` matches expected binary (N/A — the script uses no PID file: mutual exclusion is `flock` on a dedicated lock file, auto-released by the kernel on process death (spec.md FR-002; spec.md FR-010 item 5; research.md §4.1 lists "PID file + liveness check" as a rejected alternative). The only PID-bearing file the script touches is Xorg's `/tmp/.X{n}-lock`, governed by spec.md FR-012 item 5 — the script never signals that PID (it aborts with exit code `10` or reclaims the stale lock), so PID-reuse identity verification is inapplicable.)
- [x] 3.6 If PID files are used, they are created with `O_CREAT|O_EXCL` (or `set -o noclobber`) to prevent symlink following (N/A — no PID file is created: the only mutual-exclusion lock is `flock` on a dedicated lock file (spec.md FR-002; research.md §4.1), and `/tmp/.X{n}-lock` is created and owned by Xorg, not by the script (spec.md FR-012 item 5). Symlink-safe creation of script-owned files is covered by spec.md FR-012 item 7 (`mktemp` + `O_CREAT|O_EXCL`/`O_NOFOLLOW`) and checklist 8.1.)
- [x] 3.7 Lock is tested under SIGKILL crash scenarios (kernel releases on process death)
- [x] 3.8 `rm` of lock files after `flock` is avoided — kernel releases lock on process exit; removal creates race window

---

## 4. Temporary File Security

- [x] 4.1 `mktemp` (or `mktemp -d` for directories) is used for all temporary files — never hardcoded paths
- [x] 4.2 `mktemp` uses a template with identifiable prefix: `mktemp "${TMPDIR:-/tmp}/livingroots-smoke.XXXXXX"`
- [x] 4.3 Temp file/directory creation success is verified by **`mktemp`'s exit status**, not by testing the resulting path: `tmpfile=$(mktemp …) || { log_error "temp file creation failed"; exit 13; }` — `mktemp` already creates atomically with `O_CREAT|O_EXCL` and mode `0600`/`0700`, so a `[[ -f "$tmpfile" ]]` test adds nothing (and is false for a `mktemp -d` directory). The old `|| exit 1` also collided with FR-007, where `1` means "game installation not found"; a temp-creation failure is an internal script error → `13`
- [x] 4.4 `umask 077` is set at script start to ensure all created files are owner-only by default
- [x] 4.5 `trap cleanup EXIT` is defined BEFORE creating any temp files
- [x] 4.6 Cleanup trap includes SIGTERM and SIGINT: the `EXIT` trap is registered **exactly once** in the top-level shell and is the single cleanup path; the INT/TERM/HUP handlers forward the signal to the child process groups and then **reset their own trap and re-raise** (`trap - SIG; kill -SIG $$`) so the script dies *from* the signal. A combined `trap 'rm -rf "$tmpdir"' EXIT INT TERM` is **forbidden**: its INT/TERM branch would swallow the signal, skip the forwarding, and exit `0` instead of `143`. FR-005 "Trap design (MUST)"
- [x] 4.7 Temp files containing sensitive data are in `mktemp -d` directories with `chmod 0700`
- [x] 4.8 Fixed filenames in `/tmp` are never used for temporary files (vulnerable to symlink attacks) — the **flock file is the single documented exception**: it must live at a stable, predictable path for mutual exclusion to work at all, and it is protected by living in a `0700` per-user directory and by being opened `O_RDWR|O_CREAT` **without** `O_TRUNC` (so a planted symlink cannot destroy its target). FR-002 "Lock file (MUST)"
- [ ] 4.9 Temp file paths passed between processes are validated to be within the expected temp directory — **VACUOUS in the current design**: no design element hands a temp path to another process (the only cross-process handoffs are the X11 socket and the SMAPI log path). It becomes live only if the deferred save check or a python3 helper is added.

---

## 5. Injection Prevention

- [x] 5.1 `set -euo pipefail` is at the top of the script
- [x] 5.2 Every variable expansion is quoted: `"$var"` — never bare `$var` or `${var}` in command position
- [x] 5.3 `--` is used before any variable representing a filename or path: `rm -- "$file"`, `cd -- "$dir"`
- [ ] 5.4 All external input is validated against an allowlist (e.g., save directory names match `^[a-zA-Z0-9_-]+$`) — **VACUOUS as scoped, and would break the CLI**: the only named domain (save directory names) does not exist in the current design (FR-004 deferred). Applied to the real inputs, a `^[a-zA-Z0-9_-]+$` allowlist would reject a legitimate `--game-path` containing a dot or a space. The validation that *is* missing — `--timeout` / `--grace-period` must be positive integers — is not covered by any item. Pending the round-7 security-posture decision.
- [x] 5.5 `eval` is never used with any input originating from outside the script
- [ ] 5.6 Shell metacharacters are rejected in untrusted input: `[[ "$data" =~ [;&|$\(\)\<\>] ]] && reject` — **VACUOUS as a blanket rule**: the maintainer's own `--game-path` may legitimately contain `&`, `(` or `)`. The source research applies this rule *inside the ported C# filename sanitizer*, i.e. to a save-id-derived filename, which the script never produces (FR-004 deferred). The real controls are quoting (5.2/5.3) and the prefix check. Pending the round-7 security-posture decision.
- [x] 5.7 Command strings are not constructed by concatenation — use arrays (`cmd=(binary "${args[@]}")`)
- [ ] 5.8 `printf '%q'` is used to safely escape strings when passing through other shell-invoking commands — **NO CALL SITE**: the design invokes no shell-invoking command — no `bash -c`, no `sh -c`, no `eval`, no `xargs -0 sh`. Values reach child processes through arrays (5.7) and environment variables (FR-001). This is the clearest YAGNI violation against `plan.md`'s YAGNI constraint. Pending the round-7 security-posture decision.
- [x] 5.9 Game-generated data passed to other commands is validated for shell metacharacters

---

## 6. Data Integrity During Termination

- [x] 6.1 SIGTERM (15) is always sent first — never SIGKILL (9) as the initial signal
- [x] 6.2 A grace period (15-30 seconds) is implemented after SIGTERM before escalating to SIGKILL
- [x] 6.3 After SIGTERM, process exit is polled using `kill -0 "$PID"` in a loop with sleep intervals
- [x] 6.4 Process exit is verified (not just that the signal was sent)
- [x] 6.5 After process exit, expected output files (SMAPI log) are verified to exist and have non-zero size — noting that the log is removed pre-launch and recreated by SMAPI, so existence is checked against the current run's file, not a leftover. The **normative** freshness criterion remains FR-002's sentinel + inode assertion; size/non-emptiness is a secondary sanity check that may only be applied *after* the sentinel is seen
- [x] 6.6 A timeout prevents the script from hanging indefinitely if the game becomes unresponsive
- [x] 6.7 SIGKILL usage is logged (indicates graceful shutdown failed — potential data corruption risk)
- [x] 6.8 `timeout --signal=TERM --kill-after=30s` is used as a safety net for unresponsive processes
- [x] 6.9 Mod-specific save triggers that must be invoked before termination are documented
- [x] 6.10 An X11 lock file owned by a **live** process is never removed — the run aborts with exit code `10` instead; only a lock whose owning PID is dead is treated as stale and reclaimed (mirrors xorg-server's `LockServer()` ESRCH/EPERM semantics — verified against xorg-server 21.1.24 `os/utils.c`: `kill(l_pid, 0)` returning `ESRCH` means stale and the lock is unlinked, while `EPERM` or success means live and the server refuses to start leaving the lock intact; FR-012 item 5 now also pins the lock file's fixed 11-byte `"%10lu\n"` format, whose leading-space padding MUST be stripped, and treats a non-11-byte lock as bogus/stale. Resolved by decision: the EXIT-trap cleanup removes X locks **only for the display this script allocated** and, for the lock file, only when its owning PID is dead — FR-010 item 3, round-6 clarification)

---

## 7. Xvfb Security

- [x] 7.1 Xvfb is started with `-nolisten tcp` to prevent remote TCP connections to the virtual display
- [x] 7.2 No xauth-based access control (MIT-MAGIC-COOKIE-1) is configured — the accepted posture is **minimal**: `-nolisten tcp` on a non-default display (e.g., `:99`), no xauth hardening for single-user local use. `xhost +` / `-ac` are never used. (Resolved by decision — spec.md Clarifications, "Session 2026-10-19 (checklist review)": "What Xvfb security posture? → A: Minimal". FR-001 mandates only `-nolisten tcp`, and `plan.md` "Primary Dependencies" list no `xauth` package. Threat model: single-user local maintainer machine — spec.md Assumptions.)
- [x] 7.3 No MIT-MAGIC-COOKIE-1 cookie is generated — there is no Xauthority file under the accepted minimal posture (spec.md Clarifications, "Session 2026-10-19 (checklist review)": "no xauth hardening for single-user local use")
- [x] 7.4 No Xauthority file is created, so there is no Xauthority directory to protect (spec.md Clarifications, "Session 2026-10-19 (checklist review)": "no xauth hardening for single-user local use")
- [x] 7.5 `XAUTHORITY` is not set — no Xauthority file exists to point at, so the script does not rely on `~/.Xauthority` (spec.md Clarifications, "Session 2026-10-19 (checklist review)": "no xauth hardening for single-user local use")
- [x] 7.6 A non-default display number is used (e.g., `:99` or higher) to avoid conflicts
- [x] 7.7 Xvfb process receives the same SIGTERM → SIGKILL treatment as the game process
- [x] 7.8 Display `DISPLAY` value is not shared in world-readable logs or output

---

## 8. POSIX Filesystem Security

- [x] 8.1 `O_CREAT|O_EXCL` semantics are used for file creation: `mktemp` already provides them (verified in the gnulib source — `O_RDWR|O_CREAT|O_EXCL`, mode `0600`; directories via `mkdir(…, 0700)`), and `set -o noclobber` is **not** an `O_EXCL` equivalent (it is a regular-file guard only and `>\|` bypasses it). The "shared directories" clause is vacuous for a single-user tool — the script's own temp dir and its `0700` state dir are private namespaces. FR-002 "Lock file (MUST)"
- [x] 8.2 `access()` or `stat()` is never used to *gate* an `open()` — this creates a TOCTOU window; the implementation opens directly and branches on the syscall result. Existence and readability checks MAY still be performed for **reporting** (a clearer error message), as long as they do not decide whether the open happens. CWE-367: "The most basic advice for TOCTOU vulnerabilities is to not perform a check before the use"
- [x] 8.3 A symlink check (`[[ -L ]]`) MAY be performed immediately before operating on a file, and is used for **diagnosis** — with the residual race accepted (the source research concedes "lstat then open is still racy; mitigate with 0700 dirs + O_NOFOLLOW and by not re-checking"). It is **not** a security control: the atomic protection is the `O_CREAT|O_EXCL` open itself, not the preceding stat. This resolves the apparent conflict with item 8.2 — 8.2 forbids the check as a *gate*, 8.3 permits it as a *diagnostic*
- [x] 8.4 Directories used for lock/temp files have `0700` permissions
- [x] 8.5 On shared systems, `mktemp -d` creates a private working directory rather than writing to `/tmp` directly
- [ ] 8.6 Symlinks in the game directory and mod directory are checked before reading/writing (pre-flight `find -type l`) — **UNGROUNDED**: no FR mandates a symlink sweep; it exists only in `research-bash-security.md`, which itself asks for "a distinct exit code" that FR-007's taxonomy has no slot for. The source research also recommends treating symlink presence as a hard failure, which is aggressive for a Steam install. Pending the round-7 security-posture decision.
- [ ] 8.7 File ownership and permissions are verified before reading sensitive files (owner == current user, no group/world write) — **UNGROUNDED**: this appears in no FR, in no plan entry beyond "must sanitize/validate all paths", and in no other artifact; its only source is `research-bash-security.md`. As written it is also *stricter* than that source, which allows root-owned read-only game files — so a root-owned Steam install would fail it. Pending the round-7 security-posture decision.

---

## 9. Secrets & Sensitive Data

- [x] 9.1 API keys, tokens, and passwords never appear in logs, error messages, or script output
- [x] 9.2 SMAPI log excerpts in failure reports are truncated to relevant lines only — **paths are shown** (the "What → Where → How to fix" format FR-007 mandates requires a `Where:` field, and every worked example prints a full path), but **no credentials, tokens or environment secrets** are included. FR-007, checklist 3.9, 9.6
- [x] 9.3 Error messages do not expose sensitive system details — normatively grounded in `.specify/memory/constitution.md` **Principle II (Security-First Data Handling)** and `plan.md`'s Constitution Compliance ("Exit codes and error messages must not leak sensitive system details"), plus FR-007's "no subprocess-specific status may ever become the script's final code". *(The earlier attribution of the phrase "DO NOT expose raw exception messages to logs" to the constitution was a misattribution — that line lives in the project's `AGENTS.md` anti-pattern list, not in the constitution.)*
- [x] 9.4 No Xauthority/cookie material exists under the accepted minimal posture (spec.md Clarifications, "Session 2026-10-19 (checklist review)"), so there is no cookie to leak; the rule holds trivially if the posture is ever changed
- [x] 9.5 The run log directory is created with mode `0700` and the run log file inherits that restriction — it records paths, PIDs and signals but no secrets (FR-014)
- [x] 9.6 The run log records the run ID and script `VERSION` only as correlation metadata; no credentials or tokens are ever written to it

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
| 3 | `kill -- -$PGID` without the PGID assertion | Terminates the script itself (the child shares the script's group) | Assert the group ID differs from the script's own group ID and from `1` before every group signal (FR-005) |
| 4 | `ps aux \| grep "game"` | Fragile matching | `pidof`, or `/proc/$PID/exe` restricted to the resolved game path — `pgrep -x` cannot match the 17-character apphost name |
| 5 | `kill -9 $pid` immediately | Data corruption | SIGTERM first, then SIGKILL |
| 6 | `echo $var` (unquoted) | Word splitting, globbing | `echo "$var"` (always quote) |
| 7 | `cd $dir` without validation | Path traversal | `cd -- "$dir"` + prefix check |
| 8 | PID file without verification | PID reuse kills wrong process | Verify `/proc/$PID/exe` before signaling |
| 9 | `xhost +` for X11 | Unauthorized display access | `-nolisten tcp` on a non-default display; xauth/MIT-MAGIC-COOKIE-1 is explicitly out of scope for this feature (spec.md Clarifications, "Session 2026-10-19 (checklist review)") |
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
