# Bug Assessment: Test harness "hangs" (T189–T191) — revised 2026-11-04

- **Slug**: test-harness-hangs
- **Created**: 2026-11-03
- **Revised**: 2026-11-04 (after direct measurement; supersedes the trap-deferral-only diagnosis)
- **Source**: pasted reports + local reproduction + `test-cli.sh` / `test-preflight.sh` / `test-logparse.sh` output
- **Verdict**: valid
- **Severity**: medium — test infrastructure only, but it made the pre-push gate red and hid ~60 legitimate failures behind exit `13`

## Report

Three suites were reported as hanging:

- `test-preflight.sh` — allegedly stalls in the FR-012 item 8 block (restricted PATH, Xvfb withheld).
- `test-integration.sh` — allegedly hangs in signal/deadline paths.
- `test-logparse.sh` — allegedly hangs during teardown.

A first fix (2026-11-03) backgrounded `smoke-validation.sh`'s `main`, added `setsid` + explicit
group reaping in the harness, and added a bounded drain. That fix removed the original trap-deferral
hang, but the suites still failed and still looked slow. The residual was recorded as
"environment-related". That was wrong.

## Current finding: two separate defects, neither a deadlock

### 1. The background-main fix broke exit-code propagation (functional regression)

`scripts/smoke-validation.sh` ended with:

```bash
main "$@" &
main_pid=$!
wait "$main_pid"
exit $?
```

Two mechanisms then replaced documented exit codes with `13` or `1`:

- A bare `wait` on a failing child is itself a failing command. Under `set -e` it triggers the
  ERR trap (`on_error`), which exits `13`. Every documented non-zero code — `14` for a usage
  error, `5` for a mod-load failure, `12` for a build failure — was therefore reported as `13`.
- A background subshell does **not** inherit the ERR trap, even with `set -E`. When the failure
  happened inside `main` (the background job), no handler ran at all: the run exited with the
  raw status (`1`) and printed no failure report.

`on_error` also had an explicit `BASH_SUBSHELL > 0` guard, added to prevent double reports from
command substitutions. Once `main` itself ran in a background subshell, that guard silenced the
only handler that could report the failure.

This is the source of the "47 preflight failures" and "13 CLI failures" attributed to the
environment. They were not environment failures.

**Direct reproduction** (failing `date` stub, the T138 mechanism):

```
before: EXIT=1, stderr only "date stub: failing on purpose", 0 failure reports
after:  EXIT=13, one "ERROR: smoke validation failed (exit 13)" report
```

### 2. `run-all.sh`'s 300 s per-file deadline kills legitimate suites (budget defect)

`test-logparse.sh` runs the real pipeline once per log-gate row. Each run does a deliberate
`dotnet build --configuration Release --no-incremental` — measured at **20.99 s** — plus launch,
log gate and cleanup. The suite prints `#   [mod_loaded] expected 0, took 44s` per row and has
16 rows, so it needs roughly 12–18 minutes. `run-all.sh` killed every file at 300 s.

Because a file's output is captured and printed only after it exits, a killed suite produced no
partial output, which read exactly like a hang. `timeout`'s `124` was also reported as a plain
`FAIL`, so nothing distinguished "deadline fired" from "test failed".

`test-cli.sh` completes in ~180–195 s (255 cases) and `test-preflight.sh` completes in minutes;
neither deadlocks. The only genuine hang was the one the 2026-11-03 fix addressed.

## Evidence

| Check | Result |
|-------|--------|
| `dotnet build --no-incremental` timing | 20.99 s (one build) |
| `test-logparse.sh` per-row timing | 44 s/row; 16 table rows |
| `test-cli.sh` before the exit-code fix | 242/255 passed, 13 failed (`expected 14/5, got 13`) |
| `test-cli.sh` after the exit-code fix | 254/255 passed; the one failure is a 1 s timing bound (below) |
| T138 reproduction | exit `1` / 0 reports before; exit `13` / 1 report after |
| `run-all.sh` cap before | 300 s per file |
| Stray processes | none; no Xvfb/SMAPI/dotnet build survived the runs |

## Root Cause

1. **Exit-code masking** — background-job ERR-trap/inheritance semantics plus a bare `wait`
   under `set -e`. Fixed in `scripts/smoke-validation.sh`.
2. **Mis-sized deadline** — 300 s cannot cover a suite that intentionally rebuilds once per
   case. Fixed in `scripts/tests/run-all.sh`.

The 2026-11-03 trap-deferral diagnosis was correct for the original hang and its fixes
(background `main`, `setsid`, bounded drain) are retained. It was incomplete: backgrounding
`main` introduced the exit-code regression, and the deadline was never revisited.

## Proposed / Applied Remediation

### Applied in `scripts/smoke-validation.sh`

- Capture the background job's status without tripping errexit:
  `wait "$main_pid" || main_status=$?`, then `exit "$main_status"`.
- Re-install the ERR trap inside the background job before `main` runs.
- Make `on_error`'s subshell guard depth-aware: the owning shell records its
  `BASH_SUBSHELL` depth, and only deeper subshells (command substitutions) stay silent.
  This keeps the single-report property (T138) while restoring the exit-13 contract (T149).

### Applied in `scripts/tests/run-all.sh`

- Per-file deadline is now `RUNNER_FILE_TIMEOUT_SECS`, default **2700 s**, overridable with
  `RUN_ALL_FILE_TIMEOUT_SECS` for slower hosts.
- A deadline that fires is reported as `HANG <file>` with `[deadline fired after Ns]`, while
  still counting as a failure. Exit codes `124`/`137` are the deadline's, not a verdict.

### Rejected alternative

Adding a test-only build-reuse switch to `smoke-validation.sh` would cut the 21 s rebuild per
row, but spec 008 FR-015 and `plan.md` explicitly say the script has **no** environment-variable
mechanism and that its automation variables are not user-configurable. Adding one would be
contract drift. The harness must live with the rebuild cost; the deadline accommodates it.

## Files Changed

- `scripts/smoke-validation.sh` — status capture, ERR-trap re-installation, depth-aware guard.
- `scripts/tests/run-all.sh` — configurable per-file deadline, `HANG` classification.

## Tests

- `test-cli.sh` T138/T149 exercise both ERR-trap paths: exactly one report, exit `13`, report
  code equals process code. They went from failing to passing.
- `test-cli.sh` as a whole: 13 failures → 1 (timing bound only).
- `test-preflight.sh`: re-run after the fix; the expected `9`/`10`/`14` codes are restored.
- `run-all.sh test-save-preservation.sh`: still passes; syntax clean.

## Residual / Follow-ups

- **T128 timing bound**: the hung-display case now completes in 15 s against a `<= 14` assertion.
  The exit code is still `124`/`137` (the deadline fired), so the functional property holds; the
  wall-clock bound needs either a shorter interrupt grace or a re-derived bound. Track separately
  from the hang (no process survives).
- **`test-integration.sh`** was not re-run in this session; it is opt-out
  (`SMOKE_SKIP_INTEGRATION=1`) and remains the slowest suite.
- The prior `fix.md`/`test.md` statements that the residual failures were "environment-related"
  are superseded by this revision.
