# Gate 3 Evidence — Living Roots (feature 014)

- **Session date**: 2026-11-05
- **Scenario**: `specs/014-soil-health-crop-quality/scenarios/soil-health-harvest-quality.scenario.json`
- **Bundle pointer**: `evidence-bundle-run-20261105T175047Z-04bf/`
- **Run ID**: `run-20261105T175047Z-04bf`
- **Commit**: `d6656499b7c39c44c1816f7311367141817cee90`
- **Verdict**: `pass` (`signalsComplete=true`, `signalsAgree=true`, `thresholdsOk=true`)
- **Honesty level**: R (runtime evidence; `verified` remains human-only, spec 010 T148)
- **Offline verifier**: `scripts/verify-evidence-bundle.sh evidence-bundle-run-20261105T175047Z-04bf` → exit 0

## What the run did

The real-host headless runner launched SMAPI under Xvfb with no human input,
loaded the `farm-year1-day5` milestone save, and drove the verification mod
through the scenario queue. The mod pinned soil health on the anchor tile,
selected four deterministic probes with the whole-map nearest-first scanner,
planted `LongPhaseCrop` on each, forced maturity, set Poor/Moderate/Healthy
soil health per probe, and harvested each crop through the real
Harmony-patched `Crop.harvest` path.

Probe conditions: Farming level temporarily 10, fertilizer tier 0,
`HarvestMinQuality=0`, `HarvestMaxQuality=3`, inventory reduced to one null
slot so the main item and the extra item are unambiguous.

Command:

```bash
./scripts/verify-scenario.sh \
  --scenario specs/014-soil-health-crop-quality/scenarios/soil-health-harvest-quality.scenario.json \
  --no-acquire \
  --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" \
  --skip-build
```

Runner exit: 0.

## Harvest matrix

Recorded in `artifacts/crop-phase-dump.json` under `harvests[]`.

| Probe | Tile | Health | Tier | Vanilla quality | Quality roll | Extra roll | Harvest quality | Extra count | Extra quality |
|---|---|---|---|---|---|---|---|---|---|
| poor | 59,15 | 10 | poor | 1 | 0.4127 | 0.1830 | **0** | 0 | — |
| moderate | 56,10 | 50 | moderate | 1 | 0.2829 | 0.5364 | **1** | 0 | — |
| healthy-quality | 56,15 | 90 | healthy | 0 | 0.2744 | 0.5313 | **1** | 0 | — |
| healthy-extra | 46,15 | 90 | healthy | 0 | 0.7222 | 0.0847 | **0** | **1** | **0** |

Interpretation:

- **Poor**: the tile's vanilla quality would have been 1; the patch forced the
  harvested item to quality 0. No extra harvest.
- **Moderate**: vanilla quality 1 was preserved unchanged. No extra harvest.
- **Healthy quality**: vanilla quality would have stayed 0 because the roll
  0.2744 is above the vanilla probability 0.21. The roll lies inside the
  `+0.08` boosted window `[0.21, 0.29)`, and the harvested item came out at
  quality 1.
- **Healthy extra**: the crop data has `ExtraHarvestChance=0`, so vanilla
  grants no extra item. The extra roll 0.0847 lies inside the `+0.10` window
  `[0, 0.10)`, and the harvest produced exactly one extra item at quality 0.

## Gate 3 signals

1. **Soil-health state delta** — `artifacts/soil-health-before-after.json`
2. **Crop-phase delta / harvest records** — `artifacts/crop-phase-dump.json`
   (plus `crop-phase-before.json` and `crop-phase-cross-check.json`)
3. **SMAPI log** — `artifacts/smapi-log.txt`

Key log lines:

- `299`: `harvest matrix selected 4 probes from 4188 candidates`
- `300`: `harvest probe poor at 59,15 (vanilla=1, qualityRoll=0.4127, extraRoll=0.183)`
- `302`: `Living Roots: Poor soil — harvest quality forced to normal.`
- `303`: `harvest probe moderate at 56,10 (vanilla=1, qualityRoll=0.2829, extraRoll=0.5364)`
- `305`: `harvest probe healthy-quality at 56,15 (vanilla=0, qualityRoll=0.2744, extraRoll=0.5313)`
- `309`: `harvest probe healthy-extra at 46,15 (vanilla=0, qualityRoll=0.7222, extraRoll=0.0847)`
- `311`: `Living Roots: Extra harvest triggered.`
- `312`: `Living Roots: Living soil — quality boost applied.`

All 12 scenario predicates passed (`evaluation/thresholds.jsonl`).

## Promotion

The runner promoted the GATE3 block in
`specs/014-soil-health-crop-quality/test.md`, `ROADMAP.md`, and
`COMPLETION_SUMMARY.md` from `partial` to `machine-pass` through the single
owner `scripts/lib/promotion.sh`. Three transitions were appended to
`specs/010-headless-verification-runner/promotion-triggers.json`. The runner
never wrote `verified`.

## Scope and remaining gaps

- **Host-only.** The scenario declares `playerRole: host`; the second-player
  multiplayer D1 leg was not exercised. `verified` still requires a
  maintainer's honesty-level E review (spec 010 T148), and the multiplayer
  leg remains open as T109 below.
- **Single deterministic sample per tier.** The run proves the outcome for the
  selected tiles, not a quality distribution. `SC-009` has a separate
  unit-level statistical test, but `SC-001` (quality distributions across
  tiers) is not established by this run and remains a documented gap.
- **Growth loop.** This scenario measures harvest quality and extra yield; the
  growth loop is covered separately by the spec 004 machine-pass bundle
  `evidence-bundle-run-20261105T053313Z-535d`.
