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

## 2026-11-07 authorized mechanism update (explicit user authorization: safe replacement for non-`lr_*` `/debug split`)

- Implemented `lr_split_screen` as a safe `lr_*` queue command (`LivingRoots.Verification` parser + handler, `475caa8`): best-effort reflection lookup of `ICommandHelper.Trigger(string,string[])`. `specs/010-headless-verification-runner/fixtures/valid/macros/split-screen.txt` updated to `/lr_split_screen` (`dad10f1`).
- **Evidence (code/assembly):** `strings` on referenced `/home/luis/stardew-assemblies/StardewModdingAPI.dll` shows `TriggerActionManager`/gamepad triggers but **no console-command `Trigger` on `ICommandHelper`**; handler will log `ICommandHelper.Trigger unavailable in this SMAPI build; debug split not triggered (honest)`. Therefore `isMainPlayer` will not flip via this path in the referenced SMAPI build.
- **No run executed against feature test.md files** to avoid runner promotion corrupting 004/014/017 GATE3 records. The 010 fixture (`farmhand-host-guard.scenario.json`, targets `004-playable-compost-loop/test.md`, expects `expectedVerdict: pass` + `isMainPlayer eq false`) is **unlikely to pass** in this headless/SMAPI environment; running it now would risk an unhelpful promotion.
- **Honest status:** mechanism is **feasible as a safe `lr_*` replacement** (parser blocker removed) but **blocked/partial-feasible by SMAPI console-trigger API availability and/or headless split capability** in this environment. Headless split not demonstrated. Farmhand/split-screen **never `machine-pass`** (010/GOVERNANCE-ANNEX). Class: **partial/blocked-feasible, not promoted**. D1 transport remains blocked/pending.
