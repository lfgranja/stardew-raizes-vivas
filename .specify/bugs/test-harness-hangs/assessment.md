# Bug Assessment: Test harness hangs (T189–T191) — Final Revision

- **Slug**: test-harness-hangs
- **Created**: 2026-11-03 (final revision after test.md failure + deep research)
- **Source**: pasted text + test.md failure evidence + deep web research (GNU Bash manual §Signals; coreutils `timeout`; StackOverflow/Unix.SE trap propagation; veithen sigterm-propagation; Red Hat KB BZ#978840; DEV Community pipe-drain; xvfb-run SIGUSR1 race; forcola process-group reference; Raxol; session-orchestrator ADR-0015; Archon #1242; NousResearch #78432)
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

Final revision consolidates all evidence. Three suites hang:

- `test-preflight.sh` — passes 380 assertions, then stalls in the FR-012 item 8 block (restricted PATH, Xvfb withheld). `run_preflight_env` invokes `smoke-validation.sh` under `timeout --kill-after=5 120`; the child is never reaped within the deadline (observed 2m35s). Direct invocation under the same restricted PATH exits 9 in <1s.
- `test-integration.sh` — hangs (signal/deadline paths).
- `test-logparse.sh` — hangs (teardown).

A partial fix (bounded drain in `_run_script_impl` + 300s per-suite cap) was applied and verified insufficient. A setsid watchdog attempt was reverted because it killed the test's own process group.

## Symptom

`timeout --kill-after=5 120 smoke-validation.sh …` does not terminate within 120+5s when invoked through the harness's `_run_script_impl` with a restricted PATH. Direct invocation exits 9 immediately. The hang is in the harness's invocation path, not in `smoke-validation.sh`'s own logic.

## Reproduction

1. `cd` to repository root.
2. `bash scripts/tests/test-preflight.sh` — hangs after the tool-sweep section; kill manually.
3. Narrowed: hang occurs in `run_preflight_env "PATH=…block:…bin" -- --game-path …` (FR-012 item 8). Direct `smoke-validation.sh` under the same PATH exits 9 in <1s.

## Suspected Code Paths

- `scripts/tests/lib/harness.sh:492` `_run_script_impl()` — launches `env … timeout --kill-after=5 "$RUN_DEADLINE_SECS" "$HARNESS_SCRIPT" …` as a foreground command and `wait`s on it. `timeout` creates a process group and signals it, but per the Bash manual §Signals, "If Bash is waiting for a command to complete and receives a signal for which a trap has been set, it will not execute the trap until the command completes." `smoke-validation.sh` installs `forward_signal` traps (INT/TERM/HUP) and an EXIT trap `on_exit`; if it is blocked in a foreground command, the SIGTERM from `timeout` is deferred indefinitely. `--kill-after=5` should SIGKILL, but if the process is in uninterruptible sleep (D state, e.g. blocked on a pipe/Xvfb) or the signal is blocked, it survives. Additionally, `timeout`'s `waitpid` only reaps the direct child; if that child's trap blocks, `timeout` itself blocks.
- `scripts/smoke-validation.sh:1686` `forward_signal()` — asserts/signals process groups (`build_pgid`, `game_pgid`, `xvfb_pgid`), then `trap - "$sig"; kill "-${sig}" "$$"; exit …`. If the script is blocked in a foreground command when SIGTERM arrives, the trap is deferred; when it finally runs, `signal_group` calls `assert_signallable_group` and `kill` on possibly-empty groups, and `exit` may not be reached if a `wait` inside blocks.
- `scripts/smoke-validation.sh:1628` `on_exit()` — calls `cleanup`, which asserts PGIDs and may `wait` on children. If a child group is empty or a process is stuck, `cleanup` can block.
- `scripts/tests/run-all.sh` — per-suite `wait` on the background job; mitigated by the 300s cap (already applied).
- `scripts/smoke-validation.sh:4338` `main()` — runs the full pipeline in the foreground (`preflight_tool_sweep`, `phase_preflight`, `phase_display`, `phase_build`, `phase_lock`, `phase_conflict_check`, `phase_log_baseline`, `phase_launch`, `cleanup`), so any SIGTERM delivered during those phases is deferred until the foreground command completes.

## Root Cause Hypothesis

Confidence: high. Primary mechanism: **Bash trap deferral + foreground-command blocking** (Bash manual §Signals, POSIX §2.11). When `timeout` sends SIGTERM to `smoke-validation.sh`'s process group, the script's TERM trap (`forward_signal`) is not executed until the foreground command it is currently waiting on completes. In the FR-012 item 8 case the restricted PATH triggers a code path (host-tool sweep / `command -v` loop / PATH walk) that blocks in a foreground command or `wait`, so the trap never fires, SIGTERM is ignored, and `--kill-after=5`'s SIGKILL either does not reach the process (D state / blocked in kernel on a pipe or Xvfb) or the harness's `wait` on the background subshell never observes the exit because a grandchild holds the output pipe open (mechanism 2, pipe retention, DEV Community 2026-09-06).

Secondary: **process-group escape** — descendants that call `setsid()` (Xvfb under some configs, dotnet) leave the group and survive the group kill, holding descriptors.

The bounded drain (Revision 1) addressed mechanism 2 but not mechanism 1, which is why the hang persists.

## Proposed Remediation

**Preferred — make signal handling non-blocking and reaping explicit:**

1. **`smoke-validation.sh` — background the main launch so traps fire immediately.** Convert the main entry point to launch its work in a background job and `wait "$child"` (per linuxconfig.org / Unix.SE #146756): when SIGTERM arrives, `wait` returns immediately (>128) and the trap runs, forwards SIGTERM to the child group (`kill -- -$$` after de-registering the trap to avoid the signal loop), then `wait` with a bounded timeout, then exits. This converts trap deferral into immediate trap execution. Concretely:
   - In `main()` (line 4338), capture the launch as `… & main_pid=$!; wait "$main_pid"`.
   - In `forward_signal`, de-register first (`trap - "$sig"`), then `kill -- -$$` (group), then a bounded `wait` loop (poll `kill -0` up to 5s), then `exit`.
   - Guard against the signal-loop: `trap 'trap - SIGTERM && kill -- -$$' SIGTERM`.

2. **`_run_script_impl` (harness) — explicit process-group reaping.** Launch with `setsid` so the child is a session/process-group leader; capture its PGID; after `timeout` returns (or its deadline fires), explicitly `kill -- -"$pgid"` (SIGTERM), poll `kill -0` up to 5s, then `kill -9 -- -"$pgid"`. This mirrors the forcola/Raxol/session-orchestrator pattern and handles descendants that `timeout`'s group signal missed. Use `setsid` only when available; fall back to `timeout`. Apply the bounded drain (already in place) as a backstop.

3. **`run-all.sh` — per-suite hard cap** (already applied, 300s). Keep; report `HANG` distinctly from `FAIL`.

4. **Affected suites** — add `trap 'finish_tests' EXIT` so finalization runs even on signal.

**Alternatives**:
- `timeout --signal=KILL` (skip SIGTERM) — simpler but removes cleanup opportunity.
- cgroup-based kill (`cgroup.kill`, Linux 5.14+) for escapees — heavier, opt-in.
- Container/namespace isolation — heaviest, deferred per Regra dos Três.

**Files likely to change**:
- `scripts/smoke-validation.sh` (`forward_signal`, `on_exit`, `main` launch to use background+wait, trap de-registration)
- `scripts/tests/lib/harness.sh` (`_run_script_impl`: setsid launch, PGID capture, explicit group reap + bounded wait; keep bounded drain)
- `scripts/tests/run-all.sh` (HANG vs FAIL — optional)
- `scripts/tests/test-preflight.sh` (EXIT trap on `finish_tests`)

**Tests to add or update**:
- Regression: a script under test that installs a SIGTERM trap and blocks in a foreground command must be killed within `deadline + 5s` and report rc 124 (or the script's own code).
- Regression: restricted-PATH invocation (missing Xvfb) completes within the deadline and reports 9.
- Orphan check: `pgrep` before/after the FR-012 item 8 case shows no surviving grandchildren.

## Risks & Considerations

- Backgrounding the main launch in `smoke-validation.sh` changes its execution flow; must preserve exit codes and the EXIT trap's cleanup. `wait "$child"` returns the child's status, so `$?` propagation works if captured before the trap.
- `setsid` in the harness detaches from the controlling terminal; acceptable for non-interactive CI. Mitigate by only using `setsid` when `RUNNER_INTERACTIVE != 1`.
- `kill -- -$$` inside a trap requires group leadership; `setsid` in the harness guarantees it for the child. Guard with a `pgid == pid` check (Raxol pattern).
- No product (C#) code involved.

## Open Questions

- [NEEDS CLARIFICATION: does `smoke-validation.sh`'s `main()` currently run its launch in the foreground (blocking the trap) or already backgrounded? Need to inspect the main entry point around line 4338.]
- [NEEDS CLARIFICATION: is the restricted-PATH hang caused by a foreground `command -v` loop or by a `wait` on a child that never exits? The bash -x trace showed the hang at the `timeout` invocation line, suggesting the script itself is blocked before its tool sweep returns — possibly in `cleanup` called by `die` on a prior failure, or in the ERR trap.]
- [NEEDS CLARIFICATION: should the harness prefer `setsid` (new session) or `set -m` (job control) for group leadership, given interactive-debugging needs?]

## Research References

- GNU Bash Manual §Signals: https://www.gnu.org/s/bash/manual/html_node/Signals.html
- `timeout` man page (coreutils): https://www.gnu.org/software/coreutils/manual/html_node/timeout-invocation.html
- StackOverflow #360201 (killing background processes): https://stackoverflow.com/questions/360201/how-do-i-kill-background-processes-jobs-when-my-shell-script-exits
- Unix.SE (trap deferral): https://unix.stackexchange.com/questions/185903/why-does-bash-ignore-sigterm
- Veithen sigterm-propagation: https://github.com/veithen/sigterm-propagation
- Red Hat KB BZ#978840 (trap deferral in foreground commands)
- DEV Community pipe-drain (2026-09-06)
- Forcola/Raxol process-group reference (SIGTERM → grace → SIGKILL → confirm reaped)
- Session-orchestrator ADR-0015 (process group management)
- Archon #1242, NousResearch #78432 (related hang reports)
EOF
echo written