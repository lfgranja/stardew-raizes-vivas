# Gate 3 Evidence — Living Roots (feature 004)

- **Session date**: 2026-11-05
- **Bundle pointer**: `evidence-bundle-run-20261105T024756Z-07ba/`
- **Run ID**: `run-20261105T024756Z-07ba`
- **Commit**: (see bundle manifest `commit` field)
- **Verdict**: `pass` (`signalsComplete=true`, `signalsAgree=true`, `thresholdsOk=true`)
- **Honesty level**: R (runtime evidence; `verified` remains human-only, T148)
- **Offline verifier**: `scripts/verify-evidence-bundle.sh` → 0

## Observation notes

The real-host run completed end-to-end under Xvfb with no human input. The milestone save `farm-year1-day5` loaded successfully after the save-folder/inner-file name alignment fix. All three Gate 3 signals were captured from the real game process:

1. Soil-health JSON delta (`artifacts/soil-health-before-after.json`)
2. Crop phase / `phaseDays` delta (`artifacts/crop-phase-dump.json`, plus `crop-phase-before.json` and `crop-phase-cross-check.json`)
3. SMAPI log (`artifacts/smapi-log.txt`)

No anomaly report was written, i.e. no in-process dump vs. save-file divergence, no missing signal, and no signal mismatch was recorded.

## Artifact comparison (before / after)

- `crop-phase-before.json` — crop state before the scenario's mutating steps.
- `crop-phase-dump.json` — crop state after; `phaseDays` changed per the compost growth multiplier.
- `soil-health-before-after.json` — soil health delta for the target tile.
- `crop-phase-cross-check.json` — confirms dump agrees with the save file (`agree: true`).

## Promotion

The runner promoted the GATE3 block in `specs/004-playable-compost-loop/test.md`, `ROADMAP.md`, and `COMPLETION_SUMMARY.md` from `partial` to `machine-pass` via the single owner `scripts/lib/promotion.sh`. Three transitions were appended to `promotion-triggers.json`.

## T148 (human E attestation)

Not performed. `verified` requires a maintainer's honesty-level E review of this bundle, the (blocked) second-host 3/3 result, and this evidence directory. T138 (second host) and T145 (farmhand path) remain BLOCKED in this environment.