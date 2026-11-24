# Host-Only Gate 3 — Real Run Record — Spec 017 (Joja / Agrochemical MVP)

**Claim class:** `host-only-machine-pass`  
**Status:** `complete` (host-only machine evidence only)  
**Recorded:** 2026-11-06; run timestamp 2026-11-07T02:00:19Z  
**Runner:** 016-hardened `scripts/verify-scenario.sh` (spec 010 base) against the local Stardew 1.6.14 / SMAPI 4.4.0 install

> **This is not human verification, not D1, and not SC-004.** It is machine evidence that a real
> isolated game session, driven through the hardened runner, applied the 017 MVP once on an
> eligible tile and produced three agreeing signals. It does not prove balance, in-game
> rendering, manual playability, or comprehension.

## Result

| Measure | Value |
|---|---|
| Exit code | 0 |
| Verdict | `pass` (`actualVerdict` `pass`, `expectedVerdict` `pass`) |
| Signals complete | `true` |
| Signals agree (dump vs save) | `true` |
| Thresholds | 3/3 `passed` |
| `farmhandBreach` | `false` (host/main-player only) |
| `evaluatorMissing` | `false` |
| test.md status | promoted to `machine-pass` (honesty level R, not E) |
| Offline verifier | `./scripts/verify-evidence-bundle.sh evidence-bundle-run-20261107T015936Z-7f35` → exit 0 |

## Run

```bash
./scripts/verify-scenario.sh \
  --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json \
  --no-acquire \
  --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" \
  --skip-build
```

Scenario details:
- `playerRole`: host, `expectedVerdict`: pass, `scenarioKind`: positive.
- `initCommands`: `lr_plant_crop 60 15 LivingRoots.Verification.LongPhaseCrop`,
  `lr_set_soil_health 60 15 50`.
- `steps`: `runMacro` → `lr_give_agrochemical 1` + `lr_apply_agrochemical 60 15 Farm`;
  then `sleep 1` so DayEnding persists the soil change; then `dumpCropPhase`/`observe`.
- Thresholds: `soilHealthStateDelta` = `multiplier`, `field` `soilHealth`,
  `base` `pre.soilHealth`, `factor` `0.8` (measured 50 → 40 after one provisional `P=10`);
  `cropPhaseDelta` = `multiplier`, `field` `phaseDays`, `base` `pre.phaseDays`, `factor` `1.0`
  (014/015 growth unchanged); `smapiLog` = `cmp`, `left` `isMainPlayer`, `operator` `eq`,
  `right` `true`, `tolerance` `0` (host only).
- Source: `specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json` and
  `specs/017-joja-agrochemical-tradeoff/scenarios/macros/agrochemical-apply.txt`.

## Evidence artifacts

- Bundle: `evidence-bundle-run-20261107T015936Z-7f35/` (manifest.json + manifest.sha256 + claims.json +
  retention-receipt.json + verdict.json + artifacts/ + evaluation/).
- `test.md` status: `machine-pass`, claim `machine-pass by automated headless run + multi-signal evidence`,
  honesty level `R`.
- Observed result: `soil-health-before-after.json` shows Farm `60,15` `before 50`, `after 40`, `delta -10`;
  the fixture crop's `phaseDays` were unchanged; `crop-phase-dump.json` records
  `soilHealthState: measured`, `soilHealth: 40`, `isMainPlayer: true`.

## Failed attempts (provenance, not evidence)

Before the passing run, three earlier attempts in the same session exited 19:
- `run-20261107T014748Z-3dcf` and `run-20261107T015155Z-7fcc`: the deployed verification mod was stale
  (missing the new queue commands), so no signals were captured. `verdict=partial`.
- `run-20261107T015743Z-39cd`: signals captured and thresholds passed, but the crop-phase dump vs the
  persisted save diverged by save-write timing (`dump 40` vs `save 10.0` before the DayEnding
  save), so `signalsAgree=false`. `verdict=partial`.

These attempts are kept on disk as untracked bundles and are not represented in `test.md`, claim records,
or this record as passing evidence. The passing run above is the only promoted run and is the only
run cited for the `host-only-machine-pass` claim.

## Honesty boundaries

- Host-only machine evidence only; it is **not** `human-verified`, **not** `d1-farmhand-convergence`, and
  **not** SC-004.
- The scenario exercises a **measured** 50 → 40 baseline (provisional `P=10`). The C2 unknown/`NaN`
  baseline path is covered by automated tests (`AgrochemicalPersistenceTests`,
  `AgrochemicalApplicationServiceTests`), not by this run.
- `P=10` remains provisional/tunable; no final balance claim.
- The 017 gate is not cumulative across runs: a flaky or non-host pass would remain `partial`.
- D1 requires a real farmhand session; SC-004 requires the recorded `sc004-v1` protocol run.
