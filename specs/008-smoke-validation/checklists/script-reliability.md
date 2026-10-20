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
- [x] 1.7 Background process PIDs are recorded and killed in cleanup — the state model carries `game_pid`/`game_pgid` and `xvfb_pid`/`xvfb_pgid` (`data-model.md` → Process State, the two `pgid` fields added in round 8), and FR-010 signals **both** process groups
- [x] 1.8 `wait` is called after killing children to prevent zombie processes
- [x] 1.9 If script uses subshells, EXIT traps are registered in each subshell (resolved: N/A by bash semantics — bash does **not** propagate an inherited `EXIT` trap into subshells, so the parent trap is the single cleanup path and a subshell registering its own `EXIT` trap would double-clean; verified empirically on bash 5.3.9 across command substitution, parenthesised subshell, background job, pipeline stage and a function called inside a subshell — only the parent fired, once; only `set -E` propagates the `ERR` trap. spec.md FR-005 now states the rule: the `EXIT` trap is registered exactly once in the top-level shell, and a subshell owning a child process MUST clean that child up itself. This replaces the earlier, unsupported "no subshells per plan" rationale — a single-file script still uses command substitution, pipelines and background jobs)
- [x] 1.10 Cleanup is idempotent and time-bounded, not signal-deaf: an `in_cleanup` guard prevents re-entry, and the escalation window is bounded — `trap '' INT TERM` is **prohibited** (SIG_IGN is inherited across `execve`, so it would make every child spawned afterwards un-interruptible, and the script itself un-killable by SIGTERM; bash already does not re-enter a handler for the same signal). FR-005 "Trap design (MUST)"
- [x] 1.11 Lock files are removed in EXIT trap, not just on normal exit (resolved: the flock file is NOT removed — the kernel releases the lock on process death and removing the file re-opens a race window; FR-010 cleanup item (5) "the flock is auto-released by the kernel on process death", FR-002 "Lock file (MUST)" "The lock file MUST **not** be removed at exit", and quickstart.md R-4 all state this. FR-010 item (3) removes only the display **socket** — the `/tmp/.X{n}-lock` policy was retired in round 8 (the server writes no lock file at all under `-displayfd`), so the spec now keeps the flock distinct from every X11 file)
- [x] 1.12 Xvfb process PID is recorded and killed in cleanup (not left running)

---

## 2. Process Lifecycle Management

- [x] 2.1 Script records child PID immediately after launch: `game_pid=$!`
- [x] 2.2 Signal handlers forward the signal to the **child process groups** (game *and* Xvfb), not to a single PID: `kill -TERM -- "-$game_pid"` — SMAPI runs the game in-process, so signalling only the apphost PID is the wrong granularity under the process-group design. FR-005
- [x] 2.3 After forwarding TERM, script calls `wait "$game_pid"` to reap the child
- [x] 2.4 Escalation pattern: SIGTERM → wait `$GRACE_PERIOD` (15 s default, `--grace-period`) → `kill -9` if still running. The SIGTERM→SIGKILL window is the `--grace-period` value; it is **not** a separate fixed 30 s (see item 5.10). FR-005
- [x] 2.5 Process tree termination uses the process group (`kill -- -$PGID`) — the mechanism is pinned: bash job control (`set -m`) is enabled around the launch only, so the game's process-group ID equals its PID and `kill -- -$!` signals exactly that group. A mandatory assertion precedes every group signal: the group ID to be signalled MUST differ from the script's own group ID and from `1` (measured on bash 5.3.9). `setsid` is **not** used — `plan.md` introduces no such dependency; the earlier rationale ("`setsid` may fork, leaving the captured PID pointing at the wrapper rather than the game") is **wrong** and was corrected in round 8: measured on this host, `setsid sleep 30 &` does **not** fork (the captured PID is the `sleep` itself, and it becomes a session leader), so the exclusion rests on the dependency argument alone. FR-005, plan.md "Process-group mechanism"
- [x] 2.6 `kill -0 "$pid"` is used to check if process is still alive before escalating
- [x] 2.5b The failure behaviour of the mandatory PGID assertion is specified: a failed assertion aborts the run immediately with exit code `13` and a message naming both process-group IDs — it MUST NOT fall back to signalling a single PID (FR-005 "Trap design (MUST)", FR-007).
- [x] 2.7 Xvfb is launched with `&` and its PID recorded for cleanup — **one** Xvfb serves the whole run: the pre-flight starts it with `-displayfd`, the software-OpenGL probe runs against the display it reports, and the same process is reused for the game launch and killed in cleanup (FR-012 items 0, 4 and 5; `data-model.md`'s state diagram).
- [x] 2.8 Script uses `wait` (not `sleep`) to block on child processes, enabling signal-responsive shutdown — specifically `wait -f "$pid"`: under `set -m` `wait` returns on a *state change* rather than on termination (measured: with job control on, `wait` returned in 6 ms when a job was stopped), and bash defers a trapped signal until the *foreground* command completes (measured: a SIGINT raised during a foreground `sleep 3` ran the handler only after the sleep ended, while the same signal raised during `wait "$c"` ran it within ~20 ms). FR-005 notes that the `-f` modifier is only meaningful while job control is enabled and is equivalent to plain `wait` otherwise, so the substantive reason to use `wait` rather than `sleep` is the deferred-trap behaviour
- [x] 2.9 Exit code convention: 128 + signal number (143 for TERM, 130 for INT)
- [x] 2.10 If script is PID 1 (container), it explicitly handles SIGTERM (default action ignored for PID 1) (resolved: out of scope — plan.md "Constraints" records "Not a container entrypoint — PID 1 signal semantics are out of scope" and spec.md Clarifications (round 3) records "Is container / PID 1 execution in scope? → A: No. The script is a local-only maintainer tool; no Dockerfile or entrypoint exists. PID-1 signal handling is N/A (script-reliability 2.10)"; consistent with FR-013 (local-first, no CI integration) and the Assumptions section; the repo has no Dockerfile/docker-compose and .github/workflows/*.yml run on ubuntu-latest with no container or service)
- [x] 2.11 `timeout` wraps **only the log poll**, never the game launch — the game is a direct background child of the script, so `$!` is simultaneously the game's PID and its process-group ID. The timeout itself is enforced by a deadline poll loop (FR-009, FR-005).
- [x] 2.12 All process lifecycle events are logged with timestamps (FR-014)
---

## 3. Concurrency Control

- [x] 3.1 Script uses `flock` (not PID file existence check) for mutual exclusion
- [x] 3.2 Lock is acquired non-blocking: `flock -n 9` (fails immediately if held)
- [x] 3.3 Lock file descriptor is held for script lifetime: `exec 9<>"$lockfile"` — read-write, create-if-absent. The `>` form is **forbidden**: it truncates and follows symlinks, whereas `<>` neither truncates nor destroys a symlink target. FR-002 "Lock file (MUST)"
- [x] 3.4 Lock is released automatically on process exit (kernel closes fd) — measured: a holder killed with SIGKILL releases the lock and the file stays on disk (FR-002 "Lock file (MUST)"); note that the game and Xvfb inherit the descriptor, so the lock is only free once the cleanup has killed them, which FR-010's reverse-of-allocation order guarantees
- [x] 3.5 Lock file is at a **stable, per-user** path — `${XDG_RUNTIME_DIR}/livingroots-smoke.lock`, falling back to `${XDG_STATE_HOME:-$HOME/.local/state}/livingroots-smoke/livingroots-smoke.lock` — inside a `0700` directory, and on a local filesystem (`flock` is unreliable on NFS/CIFS). It is **not** inside a per-run `mktemp -d` directory (a unique directory per run means two concurrent instances lock different inodes and mutual exclusion is zero) and **not** in bare `/tmp` (world-writable, predictable, symlink-pre-creatable). FR-002 "Lock file (MUST)"
- [x] 3.6 Script exits with clear error message when lock cannot be acquired
- [x] 3.7 NFS warning: `flock` may not work reliably on NFS/CIFS; the lock path MUST be on a local filesystem (FR-002) — the pinned paths are `${XDG_RUNTIME_DIR}` and the `${XDG_STATE_HOME}` fallback, both local by construction; bare `/tmp` is not used
- [ ] 3.8 Xvfb display lock: ensured by Xvfb's built-in /tmp/.X{n}-lock + script flock + pre-flight cleanup — **the display lock is retired**: under `Xvfb -displayfd` the server sets `nolock = TRUE` and `LockServer()` returns without writing any `/tmp/.X{n}-lock` file, so there is no built-in display lock to lean on; FR-012 item 5 forbids the script from probing, creating or removing lock files, and FR-010 item 3 removes only the **socket** of the display the script allocated. The server binds the first free display atomically, so conflicts are impossible by construction; mutual exclusion between script runs comes from the `flock` (FR-002), and the pre-flight only *detects and reports* foreign Xvfb instances (FR-012 item 10). The item as written cannot hold — left unchecked pending the round-8 retirement of the X11-lock policy
- [x] 3.9 Lock is tested under SIGKILL crash scenarios (kernel releases on process death)

---

## 4. Idempotency

- [x] 4.1 All directory creation uses `mkdir -p` (succeeds if exists)
- [x] 4.2 All file removal uses `rm -f` (succeeds if missing)
- [x] 4.3 Symlinks are replaced atomically: prefer `ln -sfn` (idempotent) or `ln -s … && mv -Tf …` (atomic rename); `rm -f && ln -s` is the documented fallback — retained as a general idempotency idiom: the script creates no symlink of its own, and the one path it removes wholesale (the deploy target) uses `rm -rf -- "$target"` **without** a trailing slash, so a symlinked final component is unlinked rather than followed (FR-016 (b))
- [x] 4.4 File appends are guarded: `grep -qxF "$line" "$file" || echo "$line" >> "$file"` (whole-line match) — applies only to a shared append-only file; the script's own run log is a **per-run** file (FR-014, item 7.10), so this idiom has no target in the current design and is retained as a general idempotency idiom
- [x] 4.5 Output files are written atomically: write to `mktemp` in same dir, then `mv -f` to final path — retained as a general idempotency idiom: the script's only durable output is the per-run log, which FR-014 requires to carry every line as it is emitted, so it is appended in place and cannot be written atomically at the end
- [ ] 4.6 Script detects and cleans up resources from previous crashed runs (orphaned Xvfb, stale locks) — **the cleanup half is retired**: FR-012 item 10 now requires the pre-flight to **detect and report** orphaned Xvfb instances and explicitly forbids signalling them (under `-displayfd` the server allocates the display atomically, so an orphan blocks nothing, and no ownership token exists to prove it is this script's own orphan); the flock needs no cleanup either, because the kernel releases it when the descriptor closes or the process dies (FR-002, FR-010 item 5). The "cleans up" clause as written is now forbidden — left unchecked pending the round-8 retirement of the orphan-cleanup policy
- [x] 4.7 Script can be run 10+ times in succession without error or state corruption — FR-008 makes the repeat property a requirement and SC-015 fixes the comparison at two consecutive runs with the same inputs (same exit code, same verdict); the design accumulates no state across runs (per-run run log, per-run temp directory removed on exit, deploy target deleted and recreated, display allocated atomically by the server, flock released by the kernel), and the quickstart's "Idempotency Check" section documents the two-run procedure
- [x] 4.8 No duplicate entries in logs, no accumulating temp files
- [x] 4.9 Game save data is backed up before test and restored after (or uses isolated save) (resolved: N/A — the script never loads a save, so no game save data can be read or mutated and there is nothing to back up; FR-004 "the save check is always skipped (with a SKIPPED: warning) … The title screen is the expected state", User Story 1 "Independent Test" and User Story 2 (P2, Deferred), SC-004/SC-005 (deferred), plan.md "Constraints" ("No save file loading (title screen is expected state)"), quickstart.md TC-1 ("no save loaded"), data-model.md "Expected State When No Save Loaded" ("Zero data files. This is valid and expected in the automated script"); the mod writes no save-scoped data until SaveLoaded (ModController.cs:946-1027) and never writes into the Stardew save file (FR-004))
- [x] 4.10 Xvfb display is released and can be re-acquired on re-run
- [x] 4.11 Script validates preconditions (game files exist, SMAPI installed) before making changes
- [x] 4.12 A failed `dotnet build` aborts the run before the game is launched with exit code `12` — a failed build that leaves the previous DLL in place would otherwise produce a false pass (FR-016 (b)).
- [x] 4.13 The deployed assembly's SHA-256 digest is compared with the freshly built assembly's, and a mismatch — or a missing or older deployed file — aborts the run before launch with exit `12`, naming both digests (FR-016 (d)). The comparison is **within the run only**: a digest is never compared across runs, because the .NET compiler's deterministic content hash (and therefore the PE timestamp and MVID) is a function of the absolute source path. The build's deploy target is pinned to the resolved `--mods-path` via `-p:GameModsPath`, so the comparison is against the directory SMAPI will load from.
- [x] 4.14 The deploy target directory `<mods-path>/<ModFolderName>/` is removed before the rebuild, so a stale artifact cannot survive the build — `ModBuildConfig`'s deploy task copies but never deletes, so the removal is genuinely required (FR-016 (b)). The target is now unambiguous: it is the resolved mods path, which the build is also told to deploy into. The removal form is pinned: `rm -rf -- "$target"` with **no trailing slash** — a trailing slash makes `rm` follow a symlinked final component and delete the target's *contents* rather than the link (measured), and the target's parent MUST be re-canonicalized immediately before the removal (FR-016 (b)).
- [x] 4.15 The run log is written outside the temporary directory (XDG state dir), so it survives the EXIT trap's temp cleanup and remains available for post-mortem review (FR-014)

---

## 5. Timeout & Readiness Polling

- [x] 5.1 Readiness is determined by polling a real signal (the current run's `Log started at {UTC} UTC` sentinel), not fixed sleep
- [x] 5.2 Every polling loop has a deadline: `deadline=$(( $(date +%s) + timeout ))`
- [x] 5.3 Polling loop checks deadline before each iteration and exits with error on timeout
- [x] 5.4 Process liveness is checked during polling: `kill -0 "$pid" 2>/dev/null || fail`
- [x] 5.5 Readiness is determined by a **deadline poll loop** over the SMAPI log — the normative path (FR-009). `tail -F -n +1` is an optional alternative, not the contract; the loop checks the deadline before each iteration and exits `4` when it expires.
- [x] 5.6 Xvfb readiness: the display is proven usable by the layered probe against the display the server reports — `xdpyinfo` exits 0 and lists `GLX`/`RANDR`, `glxinfo -B` exits 0 under `LIBGL_ALWAYS_SOFTWARE=1`, and the renderer is `llvmpipe`/`softpipe` (FR-012 item 4); the socket path `/tmp/.X11-unix/X{n}` is the one the cleanup removes (FR-010 item 3), and the display number comes from the `-displayfd` descriptor, never from a number the script chose (FR-012 item 5)
- [x] 5.7 Game readiness: the log contains the current run's `Log started at {UTC} UTC` sentinel. The mod-resolution signals (`Loaded N mods:` / `Skipped mods`) are the **verdict**, evaluated immediately after the sentinel — they are deliberately *not* part of the readiness condition, because waiting for the mod to be listed would turn a mod-load failure into exit `4` (timeout) instead of exit `5` (mod load failure), contradicting FR-006 and TC-2/TC-10/TC-11
- [x] 5.8 Polling interval is appropriate: 0.5–1s for local processes
- [x] 5.9 On timeout, diagnostic info is dumped: when no failure record matched, the context is the trailing **records** of the normalised stream, capped at 30 (FR-007 "Log context (MUST)"), and the run log carries the lifecycle events and the per-phase timings that made the overrun visible (FR-014)
- [x] 5.10 The SIGTERM→SIGKILL window is `--grace-period` (15 s default), the same value as item 2.4 — not a separate fixed 30 s. The `--kill-after=30` form belongs to GNU `timeout` (item 2.11) and is a different mechanism; it must not be conflated with the script's own escalation. FR-005
- [x] 5.11 Script does not use `sleep N` as a substitute for readiness checking
- [x] 5.12 All signals are evaluated only against log lines **after** the current run's sentinel — a stale log from an earlier successful run cannot produce a false pass (FR-002, SC-011)
- [x] 5.13 The log file's inode is recorded **before** the stale log is removed, and verified to have changed once the log is recreated (belt-and-braces freshness assertion). The order is normative: recording after the removal would make the baseline "absent" and the assertion vacuous. FR-002 "Log freshness (MUST)"
- [x] 5.14 A game process that exits with status `0` but produced no log is treated as failure, not success (SMAPI's post-update dependency re-assertion exits `0`)

---

## 6. Error Handling

- [x] 6.1 Script starts with `set -Eeuo pipefail` — the escape hatch is **withdrawn**: dropping `-e` would not disable the ERR trap (`set +e` still fires it) but nothing would then abort the script, so the trap's `13` contract and the cleanup function's `exit "$rc"` semantics would lose their trigger (FR-014 "Shell options (MUST)"). `set -Eeuo pipefail` and `set -m` coexist cleanly (measured on bash 5.3.9: a `set -m`-launched child still gets its own process group, and the two option sets run together without conflict)
- [x] 6.2 `set -E` is included so ERR trap fires inside functions and subshells
- [x] 6.3 ERR trap prints line number, function name, and exit code: `BASH_LINENO`, `BASH_SOURCE`, `FUNCNAME`
- [x] 6.4 All error messages go to stderr: `echo "ERROR: ..." >&2`
- [x] 6.5 Error messages include script name and line number for debuggability (FR-014)
- [x] 6.6 Exit codes follow conventions: one distinct code per failure category, `4`=timeout, `128+N`=signal, and every child status is translated before the script exits — FR-007 now carries the complete translation table (`124`→`4`, `125`→`13`, `126`→`9`, `127`→`9`, `131`→`2`, `134`→`2`, `137`→`5`/`2`/`13`, `154`→`2`, `128+N` unchanged, any other non-zero→`13`; the apphost's `135` falls under that catch-all and is listed explicitly in `data-model.md`), including the **measured** statuses of a relocated or broken SMAPI apphost (the measured set is {154, 131, 134, 135}).
- [x] 6.7 `local var; var=$(cmd)` pattern used (not `local var=$(cmd)`) to preserve exit code
- [x] 6.8 Commands that may fail under `set -e` are explicitly handled: `cmd || true` or `if cmd; then`
- [x] 6.9 `bash -n script.sh` is run as a syntax check — wired into `lefthook.yml`'s pre-commit hook over `scripts/*.sh` (round-7 decision), and round 8 fixed the gate: `bash -n {staged_files}` parses only the **first** file (measured: `bash -n good.sh bad.sh` exits 0 while `bash -n bad.sh` exits 2), so the hook now runs `bash -n` once per staged file. It is deliberately *not* inside the script, where it would be circular: bash has already parsed the whole file to run it.
- [x] 6.10 `shellcheck` is run on the script — wired into `lefthook.yml`'s pre-commit hook over `scripts/*.sh` (round-7 decision), and round 8 made the step **skip with a warning when the binary is absent** (shellcheck is not installed on the maintainer's host, so the hook would otherwise abort every commit that touches `scripts/*.sh`). No CI job was added and no `required_status_checks` entry was created, so the repo's own warning about promoting a dormant check is respected; shellcheck needs no game installation, unlike the `Build and Test` workflow.
- [x] 6.11 Script validates all external dependencies exist before use (`command -v X`)
- [x] 6.12 No raw exception messages or stack traces exposed to users (security) — scoped to the script's **own** diagnostics: the script reports its own failures as a script name, failing line, enclosing function and exit code (FR-014) and must not leak system details (plan.md, Security-First). FR-007's "Log context (MUST)" is the complement, not a contradiction: the mod's exception summary, its stack frames and the mod's Trace records are the *required* evidence, because the exception type and the HResult appear only there (FR-003 "Trace diagnostics (MUST)")
- [x] 6.13 `trap - EXIT` is called inside cleanup to prevent recursive trap execution — FR-005 permits it as belt-and-braces (POSIX leaves "a new `EXIT` trap set during the `EXIT` trap" unspecified); measured, `trap - EXIT` plus `exit "$rc"` inside the trap preserves the original status and does not recurse
- [x] 6.14 Empty array expansion `"${arr[@]}"` is guarded for Bash < 4.4 compatibility

---

## 7. Spec-Specific Reliability Requirements

- [ ] 7.1 Pre-flight failures produce distinct exit codes per category (1=game, 2=SMAPI, 3=graphics-unusable, 4=timeout, 5=mod failure, 6=lock, 7=process conflict, 9=missing dependency, 10=display allocation failure, 11=Steam prerequisite, 12=build/artifact failure, 13=internal error, 14=usage error, 15=no log produced, 16=mods path/manifest invalid) — the **taxonomy is not complete**: FR-012 item 7 (path validation) assigns no exit code to a rejected path, and the "SMAPI aborted before mod resolution" mode (the leftover crash marker, FR-002 "Pre-launch cleanup (MUST)") has no code of its own. FR-012 item 9 makes `11` reachable (it checks `steam_appid.txt`) and codes `14`, `15` and `16` were added for the three uncovered modes, but `16` is reachable only through FR-017's post-build mod-folder check. The categories that *are* allocated remain disjoint by condition (`9` = a required host tool is **absent**; `3` = the graphics stack is **present but unusable**). Left unchecked pending the round-8 open decision on the exit-code gaps
- [x] 7.2 Game crash during launch is detected via non-zero exit or `SMAPI-crash.txt` presence
- [x] 7.3 The mod's absence from SMAPI's `Loaded N mods:` block (or its presence in the `   Skipped mods` section) is detected as a mod-not-loaded failure (`5`) — the manifest name is matched only inside that block, never across the whole log, because an unscoped search would let a *skipped* mod satisfy the loaded-mods leg (FR-003 "The success gate (MUST)")
- [x] 7.4 `--force` flag kills existing instances with SIGTERM → SIGKILL escalation before launching
- [ ] 7.5 Interruption (Ctrl+C) triggers EXIT trap: game killed, Xvfb killed, X11 locks removed, temp files cleaned — **the "X11 locks removed" clause is retired**: FR-010 item 3 removes only the **socket** of the display the script allocated, and FR-012 item 5 forbids the script from removing `/tmp/.X{n}-lock` files at all, because under `-displayfd` the server writes none. The remaining clauses hold: FR-005 "Trap design (MUST)" requires the `INT`/`TERM`/`HUP` handlers to forward to the child process groups and then reset and re-raise, so the script dies *from* the signal and the caller observes `128+N`; the `EXIT` trap is the single cleanup path and the temp directory is removed on every exit path (FR-010 item 4). The item as written cannot hold — left unchecked pending the round-8 retirement of the X11-lock policy
- [x] 7.6 Each cleanup step is isolated with `|| true` so one failure does not prevent others
- [x] 7.7 Cleanup order is reverse of allocation: game first, then Xvfb, then the display socket and the temp files (FR-010)
- [x] 7.8 Failure reports include the matched failure record together with its continuation lines and every mod Trace record that follows it (those carry the exception type and HResult) — FR-007 "Log context (MUST)" forbids a fixed `tail -N` window, because a .NET stack trace is routinely 20–40 lines long and such a window can consist entirely of stack frames while omitting the record that names the failure; only when no record matched does the context fall back to the trailing records of the normalised stream, capped at 30
- [x] 7.9 A stale `SMAPI-latest.txt`, `SMAPI-crash.txt` **and both of SMAPI's marker files** (`smapi-internal/StardewModdingAPI.crash.marker` and `smapi-internal/StardewModdingAPI.update.marker`) are removed before launch; the **crash marker** is the load-bearing one — `LogManager.HandleMarkerFiles()` gates its keypress read on the marker's presence, not on the crash log (which is only a copy of the log). Under the `/dev/null` stdin FR-005 mandates the read does not hang but **aborts** SMAPI (status `1`) before mod resolution, and the marker survives that abort, so every later run would fail identically until it is removed (FR-002 "Pre-launch cleanup (MUST)")
- [x] 7.10 The script writes one run log per run (never a shared append-only file), so concurrent runs cannot interleave their output
- [x] 7.11 The run log carries the run ID on **every line** and the script `VERSION` in the **run-log header**, so a failed run can be reconstructed from that file alone (FR-014 "Run ID and version (MUST)", SC-014) — the earlier claim that `VERSION` is stamped on every line was overstated: FR-014 requires the version in the run-log header, the stdout summary and `--help`, and the run ID on every line
- [x] 7.12 Exit codes `9` (missing dependency), `10` (the virtual display could not be allocated or started), and `11` (Steam prerequisite) are distinguished from the existing categories — `10` was renamed from "display conflict" in rounds 7–8, because the conflict policy was dropped with the fixed `:99` display and the server now allocates the display atomically (FR-012 item 5)

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

## Round-8 Verification Note

- **Result**: 89 items — **85 `[x]`**, **4 `[ ]`** (3.8, 4.6, 7.1, 7.5). 7.1 depends on the open exit-code decision (FR-012 item 7 assigns no code to a rejected path; the "SMAPI aborted before mod resolution" mode has none; `16` is reachable only through FR-017's post-build check). 3.8, 4.6 and 7.5 are items whose requirement the current spec **contradicts**: the X11-lock policy and the orphan-cleanup policy were retired in round 8 — under `Xvfb -displayfd` the server writes no lock file at all, the script MUST NOT create or remove one, and the pre-flight only **detects and reports** foreign Xvfb instances (FR-012 items 5 and 10, FR-010 item 3). Item 1.11 carries the same inverted-claim shape (the flock file is deliberately **not** removed), but its resolution was already recorded in round 7 and the round-8 change did not alter the flock file's lifecycle, so it stays `[x]` with a corrected tail.
- **Empirical checks run on this host** (bash 5.3.9, isolated in `/tmp/opencode`): `set -m` around a launch gives the child a process group whose ID equals its PID, and without it the child shares the script's own group; with `set -m` left on for the whole script a terminal SIGINT reaches the poll loop's foreground `sleep` and the `INT` handler never runs (reproduced 3/3 under a pty, the script exiting `130` as a plain exit), which is exactly why FR-005 scopes `set -m` to each launch; `trap - INT; kill -INT $$` dies **from** the signal (`WIFSIGNALED`, `WTERMSIG=2`) while `exit 130` is a plain exit (`WIFEXITED`), so the two are observably different; a trapped SIGINT raised during a foreground `sleep 3` runs the handler only after the sleep ends, while the same signal during `wait "$c"` runs it within ~20 ms; `exec 9<>` neither truncates nor destroys a symlink target while `exec 9>` truncates it; `chmod 0600 /proc/self/fd/9` enforces the mode on the open descriptor; `rm -rf -- "link/"` deletes the **target's contents** while `rm -rf -- "link"` unlinks the link; `case "$real" in "$base"/*)` is a correct component-boundary prefix test, and a positive `case` character class with the spec's allowlist matches **every** path; bare `realpath` (default `-E`) exits 0 for a missing final component while `realpath -e` exits 1; `umask 077` does not change an existing file's mode; `timeout` yields 124/125/126/127 as FR-007's table assumes; GNU `grep` does not strip the CR of a CRLF line, so a pattern ending in `$` never matches until `sed 's/\r$//'`; `bash -n good.sh bad.sh` exits 0 while `bash -n bad.sh` exits 2, so the lint gate must parse each file; `trap '' INT TERM` leaves `SigIgn=0x4002` in every child, i.e. SIG_IGN survives `execve`; an inherited `EXIT` trap does **not** fire in a subshell (command substitution, parenthesised subshell, background job, pipeline stage, function in a subshell — only the parent fired, once), and only `set -E` propagates the `ERR` trap into functions; `setsid sleep 30 &` does **not** fork on this host, so the round-8 correction to the `setsid` rationale is right.
- **Minor inconsistencies noted (outside this file)**: `data-model.md` still describes the log level as "padded to 5 characters" and Validation Rule 5 still says the level is matched "as a **column**", and the spec's Key Entities entry repeats the 5-character claim — all three predate FR-003's round-8 correction that the level token is variable-width and must be bound **by label**; `quickstart.md`'s exit-code index (code `5`) and Escalation bundle item 3 still say the failure report carries "the last 20–30 log lines", where FR-007 specifies the matched record plus its continuation lines and, only when nothing matched, the trailing records capped at 30.

---

## Sources

- Bash Beginners Guide §12.2 — Traps: https://tldp.org/LDP/Bash-Beginners-Guide/html/sect_12_02.html
- BashFAQ/045 — flock: https://mywiki.wooledge.org/BashFAQ/045
- SysTutorials — Process termination: https://www.systutorials.com/in-linux-how-to-kill-a-process-and-all-its-child-processes/
- vishnubob/wait-for-it: https://github.com/vishnubob/wait-for-it
- Google Shell Style Guide: https://google.github.io/styleguide/shellguide.html
- Fatih Arslan — Idempotent bash: https://arslan.io/2019/07/03/how-to-write-idempotent-bash-scripts/
- Paperless — Faux idempotence: https://paperless.blog/faux-idempotency
