# 018 T002 — farmhand-host-guard headless attempt (evidence)

**Date (UTC run):** 2026-11-07T11:05:33Z (`run-20261107T110533Z-3e46`)  
**Scenario:** `specs/010-headless-verification-runner/fixtures/valid/farmhand-host-guard.scenario.json` (validates `v=0` via `scenario-validate.sh`)  
**Command:**
```bash
./scripts/verify-scenario.sh --scenario specs/010-headless-verification-runner/fixtures/valid/farmhand-host-guard.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" \
  --skip-build --timeout 300
```

## What happened (observed)

- Real SMAPI/Stardew process launched under runner; SMAPI log shows mods loaded including `Stardew Valley - Living Roots (Verification) 0.0.1 by lfgranja` and `verification: Farm map 80x65`.
- Runner trace shows `injection_run` returned **`inj_rc=19`**; overall runner exited **19**.
- Local evidence bundle created (gitignored): `evidence-bundle-run-20261107T110533Z-3e46`.
- `verdict.json` (from bundle): `{"verdict":"partial","actualVerdict":"partial","signalsComplete":false,"signalsAgree":false,"thresholdsOk":true,"expectedVerdict":"pass","farmhandBreach":false,"evaluatorMissing":false}`
- `claims.json`: `honestyLevel=W`, `claims=[]`.
- `artifacts/anomaly-report-soilHealthStateDelta.json`: missing `rootCause: "missing before/after for soil health delta"`.

## Root cause (code-evidenced)

- The fixture init macro `macros/split-screen.txt` (created in `bc889c1`, format-valid) contains `/debug split` + `/sleep`.
- `scripts/lib/injection.sh` `injection_build_queue` flattens init macros correctly (direct check: init queue = `debug split`; steps = `lr_advance_days 2`, both RC=0).
- `LivingRoots.Verification/Queue/QueueCommandParser` accepts **only `lr_*` commands**; `debug split` is rejected as `unknown-command` (parser returns false / `unknown-command`).
- `LivingRoots.Verification.ModEntry` consumes the queue and writes ack/error on parse/command failures; `scripts/lib/injection.sh` `injection_wait_ack` returns **19** when ack indicates `ok != true` or parse fails.
- Therefore the failure is **queue architecture incompatibility** (non-`lr_` command in verification queue), not a proven failure of SMAPI `debug split` in headless mode.

## 018 T002 status

**Blocked by mechanism (current evidence).** Do **not** promote to `machine-pass`/`verified`. Do **not** claim `debug split` headless feasibility from this run.

- `debug split` headless feasibility: **still untested** via a compatible path.
- Farmhand guard via verification queue: **blocked** until one of: (a) verification mod whitelists specific debug commands (strict allowlist, TDD, security review), (b) runner injects console commands outside the `lr_*` queue, or (c) alternative farmhand simulation strategy is designed.
- Honesty classification for this attempt: **attempted, runner exited 19, partial/incomplete signals, no farmhand breach observed, not a pass**.

## Next actions (MDMV-aligned)

1. Design/code (if chosen): extend verification queue/parser with an explicit, minimal allowlist for `debug split` (or a dedicated `lr_debug_split` wrapper) under TDD; document trust boundary.
2. Re-run T002 end-to-end only after mechanism is fixed; record `partial` (farmhand runs never `machine-pass` per 010/GOVERNANCE-ANNEX) if clean, with full signal evidence.
3. If mechanism remains infeasible, mark 018 farmhand-guard scenarios `blocked` with reason and preserve D1 transport as `blocked/pending`.
