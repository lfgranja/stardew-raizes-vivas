# Bug Assessment: Gate 3 launches but cannot capture its three signals

- **Slug**: gate3-capture-path
- **Created**: 2026-11-04
- **Source**: Gate 3 run against the real install (spec 010 runner, scenario 004)
- **Verdict**: valid — Gate 3 remains BLOCKED; no `machine-pass` and no `verified`
- **Severity**: high

## Outcome

Gate 3 was run end-to-end against the real install. The game now launches and
reaches the title screen under Xvfb, but the runner has no capture path, so the
three mandatory signals are absent. The run exits `19` (`partial`) and writes
three `missing` anomaly reports. `specs/004-playable-compost-loop/test.md`
correctly stays `partial`; it was not promoted.

This is the honest result. It is not a Gate 3 pass.

## Evidence of the launch (the part that works)

Command:

```
bash scripts/verify-scenario.sh \
  --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json \
  --game-path "/home/luis/.steam/steam/steamapps/common/Stardew Valley" \
  --skip-build --no-acquire --timeout 120
```

Result: `RUNNER_EXIT=19`, bundle `evidence-bundle-run-20261104T061732Z-0381`.

SMAPI 4.5.2 / Stardew Valley 1.6.15 build 24356 log:

```
[... INFO  SMAPI] Loaded 4 mods:
[... DEBUG SMAPI] Mods loaded and ready!
[... INFO  Stardew Valley - Living Roots] The 'Stardew Valley - Living Roots' mod was loaded successfully!
```

Bundle contents:

```
artifacts/anomaly-report.json                    -> soilHealthStateDelta missing
artifacts/anomaly-report-cropPhaseDelta.json     -> cropPhaseDelta missing
artifacts/anomaly-report-smapiLog.json           -> smapiLog missing
claims.json                                      -> honestyLevel W, claims []
manifest.json / manifest.sha256
```

`claims.json` asserts `W` (launch/connect) and an empty claim list, which is
accurate: nothing was captured, so nothing is claimed.

## Defects found and fixed on the way

Each fix is committed with its own regression test.

1. **False pass on launch failure.** `if ! launch_wait_ready ...; then rc=$?`
   captured the negated status (always 0), so a failed launch exited 0 and left
   an empty bundle. Fixed with the `|| rc=$?` idiom so the real code survives
   `set -e` and `display_stop` still runs. Commit `ef71e7e`.
2. **Same swallowing at the version gate.** `if ! version_gate_classify ...`
   never saw code 19. Fixed the same way. Commit `ef71e7e`.
3. **Phase 2 of readiness never polled.** The mod-resolution header lands ~20 s
   after the sentinel on a real install, but the code grepped once and returned
   `17` on every healthy launch. Now polls to the phase bound per contract
   C-LCH-01. Commit `fbfb7b1`.
4. **Exit 0 with an empty signal set.** The pipeline skipped `injection_run`,
   `signals_capture` and `quit_run` (none exist) with `|| true`, called
   `verdict_evaluate` with 2 args where it requires 5, and fell off the end with
   a success status. Now `signals_require_expected_signals` gates the verdict:
   missing signals => `partial`/`19`, promotion skipped, no `--ignore-exit-code`
   demotion. Commit `0d90abc`.
5. **Missing-signal anomalies overwrote each other.** All three wrote the same
   `anomaly-report.json`, so the bundle named only one. Now one file per signal.
   Commit `0d90abc`.
6. **Claims file was schema-invalid and overclaimed.** It omitted the required
   `claims` array and asserted `R` on a launch-only run. Now writes `claims: []`
   and `W` when the signal set is incomplete. Commit `0d90abc`.
7. **Bundle verifier counted evidence of absence as evidence of presence.**
   Three anomaly reports satisfied its `count >= 3` check. The presence check is
   now honesty-aware and ignores `anomaly-report*`; a `W` bundle claiming
   nothing stays valid. Commit `0d90abc`.

## What is still missing (exact blockers)

`scripts/verify-scenario.sh` guards every capture step with
`declare -F <fn>`, and these functions are defined nowhere:

| Step | Function | State |
|------|----------|-------|
| save isolation / milestone load | `save_prepare` | not implemented |
| scenario injection | `injection_run` | not implemented (`injection.sh` has validation primitives only) |
| signal capture | `signals_capture` | not implemented (`signals.sh` has primitives only) |
| clean quit | `quit_run` | not implemented (`quit.sh` has `quit_request`/`quit_wait_clean` only) |
| verdict | `verdict_evaluate` | exists but is called with the wrong arity; thresholds never evaluated |
| attempts | `evidence_record_attempt` | exists but never called, so `attempts/` stays empty |

Supporting gaps:

- `LivingRoots.Verification/ModEntry.cs` is an empty stub. `CropPhaseDumpWriter`
  exists but is unused, so no crop-phase dump is produced.
- The mod registers only `lr_version`. The scenario's init command
  `lr_set_soil_health -10 5 10` does not exist, so poor soil cannot be seeded.
- The scenario's `insertWaste`, `sleep 5 days` and `dumpCropPhase` steps have no
  executor.
- The milestone save `farm-year1-day5` does not exist on this host. The saves
  present are `Agroeco_421179919`, `Alch_421181235`, `MDMV_423846455`,
  `MDMV_424795694`, `Mocochinchi_435810789`, `Mocochinchi_435811461`,
  `Paozinho_420340450`. Without it there is nothing to load and no farm state to
  mutate.
- `specs/010-headless-verification-runner/gamepath.lock.json` is still a
  fail-closed placeholder, so runs must pass `--no-acquire`.

## What would unblock Gate 3

1. Implement `save_prepare` and provide the `farm-year1-day5` milestone save.
2. Implement the verification mod: `lr_set_soil_health`, waste insertion, day
   advance, and a crop-phase dump on demand.
3. Implement `injection_run` and `signals_capture` on the existing primitives,
   writing a soil-health before/after pair, the crop-phase dump, and a SMAPI log
   excerpt into `artifacts/`.
4. Wire `verdict_evaluate` with its real 5-argument signature and record
   per-attempt artifacts.
5. Re-run this command and confirm three real signals plus exit 0, then obtain
   the human attestation (T148). Only a human may write `verified`.

## Verification status of these fixes

- `scripts/tests/test-display-launch.sh`: 18/18 (includes the exit-15
  propagation, the display-stop-on-failure check, the phase-2 poll, and the
  ready-launch-with-no-signals => 19 case).
- `scripts/tests/test-signals.sh`: 24/24.
- `scripts/tests/test-evidence.sh`: 17/17.
- `scripts/tests/test-bundle-negative.sh`: 8/8.
- `scripts/tests/test-verify-cli.sh`: 19/19.
- `scripts/tests/test-version-gate.sh`: 10/10.
- Also green: `test-promotion`, `test-suite`, `test-zero-core-change`,
  `test-contract-registries`, `test-docs`, `test-doc-links`,
  `test-schema-contract`, `test-verdict`, `test-retention`, `test-flaky`.
- Pre-existing failures unrelated to these changes: `test-cli.sh`,
  `test-logparse.sh` (spec 008 smoke validator), `test-preflight.sh` (exit 13,
  recorded earlier), and `test-injection.sh` (`build_queue`, reproduced at
  `HEAD~2`).

These fixes are covered by launch-free suites plus a real launch observation.
They are **not** Gate 3 verified: the three-signal evidence still does not exist.
