# Bug Assessment: Test harness hangs (T189–T191) — Revision 2

- **Slug**: test-harness-hangs
- **Created**: 2026-11-03 (revised after test.md failure evidence)
- **Source**: pasted text ("Investigate T189–T191 hangs …") + test.md failure evidence + deep web research (GNU Bash manual §Signals, coreutils `timeout`, StackOverflow/Unix.SE trap propagation, veithen sigterm-propagation, Red Hat KB BZ#978840, DEV Community pipe-drain, forcola process-group reference, Raxol, session-orchestrator ADR-0015)
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

Revision 2 incorporates the evidence from `test.md` (2026-11-03): the partial fix (bounded drain in `_run_script_impl` + 300s per-suite cap in `run-all.sh`) did **not** resolve the hang. `test-preflight.sh` still stalls inside the FR-012 item 8 section, where `run_preflight_env` invokes `smoke-validation.sh` under a restricted PATH (Xvfb missing). The `timeout --kill-after=5 120` wrapper does not reap the child within the deadline (observed 2m35s). Direct invocation of `smoke-validation.sh` under the same restricted PATH exits 9 immediately, so the hang is specific to the harness's invocation path (environment + process-group signaling), not to the script's own logic.

## Symptom

`test-preflight.sh` prints TC-24 assertions, then enters the FR-012 item 8 block (`run_preflight_env "PATH=…block:…bin" -- --game-path …`) and never returns. The harness's `_run_script_impl` launches `env … timeout --kill-after=5 120 smoke-validation.sh …`, but neither the direct child nor its descendants are reaped within the deadline. The 300s cap in `run-all.sh` mitigates aggregate stalls but the individual suite still hangs.

## Reproduction

1. `cd` to repository root.
2. `bash scripts/tests/test-preflight.sh` — hangs after TC-24; kill manually.
3. Narrowed: hang occurs in the FR-012 item 8 `run_preflight_env` call with a restricted PATH (Xvfb withheld). Direct `smoke-validation.sh` invocation under the same PATH exits 9 in <1s.

## Suspected Code Paths

- `scripts/tests/lib/harness.sh:492` `_run_script_impl()` — `timeout --kill-after=5 "$RUN_DEADLINE_SECS" "$HARNESS_SCRIPT" …`. GNU `timeout` calls `setpgid(0,0)` so the child becomes a process-group leader and signals the whole group. **But per the Bash manual §Signals: "If Bash is waiting for a command to complete and receives a signal for which a trap has been set, it will not execute the trap until the command completes."** `smoke-validation.sh` installs `forward_signal` traps for INT/TERM/HUP and an EXIT trap `on_exit`. If the script is blocked in a foreground command (e.g. a `command -v Xvfb` lookup that triggers a PATH walk, or a build step), the SIGTERM from `timeout` is deferred until that command completes — potentially indefinitely. `--kill-after=5` should then SIGKILL, but if the child is in an uninterruptible wait or the signal is blocked, it survives.
- `scripts/smoke-validation.sh:1729-1737` — `trap 'forward_signal INT' INT`, `trap 'forward_signal TERM' TERM`, `trap 'forward_signal HUP' HUP`, `trap 'on_exit "$?"' EXIT`. `forward_signal` must kill the child group and `wait`; if it instead calls `wait` on a foreground command that never returns, the trap blocks and SIGKILL never arrives in time (or the process is in D state).
- `scripts/tests/lib/harness.sh` bounded drain (already applied) — addresses pipe-EOF retention but not trap deferral or group escape.
- `scripts/tests/run-all.sh` (already capped at 300s) — aggregate mitigation only.

## Root Cause Hypothesis

Confidence: high. The primary mechanism is **trap deferral + foreground-command blocking** (Bash manual §Signals, Red Hat KB BZ#978840): when `timeout` sends SIGTERM to the process group, `smoke-validation.sh`'s TERM trap (`forward_signal`) is not executed until the foreground command it is currently waiting on completes. In the FR-012 item 8 case the restricted PATH triggers a code path (host-tool sweep / PATH lookup) that blocks in a foreground command (possibly a subshell or `command -v` loop, or a `wait` on a child), so the trap never fires, SIGTERM is ignored, and `--kill-after=5`'s SIGKILL either does not reach the process (D state / blocked in kernel) or the harness's `wait` on the background subshell never observes the exit because a grandchild holds the output pipe open (mechanism 2, pipe retention).

Secondary: **process-group escape** — any descendant that calls `setsid()` (Xvfb, dotnet under some configs) leaves the group and survives the group kill, holding descriptors.

The bounded drain (Revision 1 fix) addressed mechanism 2 but not mechanism 1, which is why the hang persists.

## Proposed Remediation (revised)

**Preferred**: Make signal handling and reaping explicit and non-blocking in the script under test, and make the harness's invocation use `setsid` + negative-pgid kill with a bounded wait:

1. **`smoke-validation.sh` — non-blocking trap forwarding.** Change `forward_signal` so it does not call `wait` on a foreground command. Pattern (veithen sigterm-propagation, Unix.SE #146756): background the foreground work (`cmd & child=$!; wait "$child"`), so that when SIGTERM arrives, `wait` returns immediately (>128) and the trap runs, forwards SIGTERM to the child group (`kill -- -$$`), then `wait` with a bounded timeout, then exits. This converts trap deferral into immediate trap execution. Concretely:
   - Wrap the main launch in a background job and `wait "$child"` instead of running it in the foreground.
   - In `forward_signal`, do `kill -- -$$` (group) then a bounded `wait` loop (poll up to 5s), then `exit`.
   - De-register the trap before re-signaling to avoid the signal-loop (`trap - TERM INT` then `kill`).

2. **`_run_script_impl` (harness) — explicit process-group reaping.** Launch the child with `setsid` so it is a session/process-group leader, capture its PGID, and after `timeout` returns (or its deadline fires), explicitly `kill -- -"$pgid"` (SIGTERM), wait up to 5s (polling `kill -0`), then `kill -9 -- -"$pgid"`. This mirrors the forcola/Raxol pattern (resolve group → SIGTERM → grace → SIGKILL → confirm reaped) and handles descendants that `timeout`'s group signal missed. Also apply the bounded drain (already in place) as a backstop for pipe retention.

3. **`run-all.sh` — per-suite hard cap** (already applied, 300s). Keep; also report `HANG` distinctly from `FAIL` for diagnostics.

4. **Affected suites** — add `trap 'finish_tests' EXIT` so finalization runs even on signal; replace unbounded `wait` with bounded poll (already mostly bounded via deadlines).

**Alternatives**:
- Use `timeout --signal=KILL` (skip SIGTERM) — simpler but removes cleanup opportunity; may leave orphaned children holding resources.
- cgroup-based kill (`cgroup.kill`, Linux 5.14+) for escapees — heavier, opt-in.
- Run each suite under a separate `systemd-run` / container (heaviest; deferred per Regra dos Três).

**Files likely to change**:
- `scripts/smoke-validation.sh` (`forward_signal`, main launch to use background+wait, trap de-registration)
- `scripts/tests/lib/harness.sh` (`_run_script_impl`: setsid launch, PGID capture, explicit group reap + bounded wait)
- `scripts/tests/run-all.sh` (HANG vs FAIL distinction — optional)
- `scripts/tests/test-preflight.sh` (EXIT trap on `finish_tests`)

**Tests to add or update**:
- Regression: a script under test that installs a SIGTERM trap and blocks in a foreground command must be killed within `deadline + 5s` and report rc 124 (or the script's own code).
- Regression: restricted-PATH invocation (missing Xvfb) completes within the deadline and reports 9.
- Orphan check: `pgrep` before/after the FR-012 item 8 case shows no surviving grandchildren.

## Risks & Considerations

- Backgrounding the main launch in `smoke-validation.sh` changes its execution flow; must preserve exit codes and the EXIT trap's cleanup. The `wait "$child"` returns the child's status, so `$?` propagation works if captured.
- `setsid` in the harness detaches the child from the controlling terminal; acceptable for non-interactive CI, but may complicate interactive debugging. Mitigate by only using `setsid` when `RUNNER_INTERACTIVE != 1`.
- `kill -- -$$` inside a trap requires the script to be a process-group leader; if not, it falls back to killing only itself (per StackOverflow: "kill syntax for killing by PGID is friendly — falls back to the PID"). Using `setsid` in the harness guarantees leadership.
- No product (C#) code involved.

## Open Questions

- [NEEDS CLARIFICATION: does `smoke-validation.sh` currently run its main launch in the foreground (blocking the trap) or already backgrounded? Need to inspect the main entry point.]
- [NEEDS CLARIFICATION: is the restricted-PATH hang caused by a foreground `command -v` loop or by a `wait` on a child that never exits? The bash -x trace showed the hang at the `timeout` invocation line, suggesting the script itself is blocked before its tool sweep returns.]
- [NEEDS CLARIFICATION: should the harness prefer `setsid` (new session) or `set -m` (job control) for group leadership, given interactive-debugging needs?]