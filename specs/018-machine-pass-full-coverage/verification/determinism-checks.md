# T035 — Determinism checks (2× per scenario)

**Date:** 2026-11-08
**Scope:** 004 `compost-growth-hardening` (3-signal) and 013 `mulch-7day-expiry` (v4, 2-signal), same seed, same milestone save, same deployed baseline binaries, back-to-back runs. This is **2 of 19** scenario files, not an exhaustive sweep.
**Definition used:** determinism means the **semantic evidence** (measured values, pre/post artifacts, predicates, verdict) is identical across reruns. Raw SMAPI logs are compared after stripping timestamps and timing values; residual differences are recorded explicitly and are not treated as bit-identical logs.

## Runs

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 004 `compost-growth-hardening` | `evidence-bundle-run-20261108T141912Z-6948` | `evidence-bundle-run-20261108T142058Z-4b8d` | pass / pass |
| 013 `mulch-7day-expiry` | `evidence-bundle-run-20261108T142252Z-6371` | `evidence-bundle-run-20261108T142501Z-266f` | pass / pass |

Command shape (both runs, both scenarios):

```bash
./scripts/verify-scenario.sh --scenario <scenario.json> --no-acquire \
  --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" \
  --skip-build --timeout 420
```

## Semantic comparison

For each pair, the following files were parsed as JSON and compared after dropping only non-semantic fields (`capturedAtUtc`, `elapsedMilliseconds`):

- `artifacts/soil-health-before-after.json`
- `artifacts/soil-health-before.json`
- `artifacts/crop-phase-before.json`
- `artifacts/crop-phase-dump.json`
- `evaluation/actual.json`
- `evaluation/pre.json`
- `verdict.json`

Result: **all compared files identical** for both scenarios.

| Scenario | `actual` values (both runs) | Threshold outcomes (both runs) |
|---|---|---|
| 004 | `soilHealth` 10, tier `poor`, `phaseDays` 5, `currentPhase` 0, `dayOfCurrentPhase` 1 | predicate-0 (×1.15) pass; predicate-1 (poor band) pass |
| 013 | `soilHealth` 49, tier `moderate` | predicate-0 (×0.98) pass; predicate-1 (`isMainPlayer`) pass |

`evaluation/thresholds.jsonl` is identical except for the absolute bundle path embedded in the `predicate` field (`.../evidence-bundle-run-<id>/evaluation/predicates/...`), which necessarily differs per run and carries no semantic content.

## SMAPI log comparison (honest residual variance)

Logs were filtered to mod-relevant lines (`Living Roots`, `verification:`, `Mulch`), then timestamps and `elapsed: N ms` values were stripped.

| Scenario | Filtered lines run 1 / run 2 | Identical after normalization? | Residual difference |
|---|---|---|---|
| 004 | 82 / 83 | No | Run 2 contains one extra `Pruned orphan bin record at Farm:60,15 (no matching world object).` line. The line `Bin at (60, 15): Empty → Processing, item: (O)24` appears in a different position relative to two `Saved data for key 'visualization_config_modfolder'.` lines. |
| 013 | 106 / 105 | No | Run 1 contains one extra `Pruned orphan bin record at Farm:60,15 (no matching world object).` line. |

Both residual differences are **startup/load-order and save-pruning timing artifacts**, not signal or verdict changes. The measured soil/crop values and predicate outcomes are identical. The logs are therefore **not byte-identical**, and this record does not claim they are.

## Conclusion

- Semantic determinism: **PASS** for both scenarios (identical signals, values, predicates, verdicts).
- Log-level determinism: **not claimed**; residual variance is documented above.
- Both scenario pairs pass `./scripts/verify-scenario.sh --verify-bundle` with exit 0 (see `mutation-checks.md` integrity list).
- No claim is promoted above `machine-pass`; this is host-only evidence and says nothing about farmhand/D1, SC-004, human review, render legibility, or balance.

## Pass 13 addition (2026-11-08) — 005 and 017 (coverage now 4 of 19)

Command shape:

```bash
./scripts/verify-scenario.sh --scenario <scenario.json> --no-acquire \
  --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" \
  --skip-build --timeout 420
```

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 005 `soil-health-growth` (healthy 80, seed 987654321) | `evidence-bundle-run-20261108T172115Z-060e` | `evidence-bundle-run-20261108T172354Z-5b8c` | pass / pass |
| 017 `agrochemical-tradeoff` (seed 123456789) | `evidence-bundle-run-20261108T172717Z-31c7` | `evidence-bundle-run-20261108T172927Z-5f08` | pass / pass |

Semantic comparison (same method as pass 11): all seven compared JSON files identical per pair (`soil-health-before-after`, `soil-health-before`, `crop-phase-before`, `crop-phase-dump`, `evaluation/actual`, `evaluation/pre`, `verdict`), and `evaluation/thresholds.jsonl` differs only by the absolute bundle path inside the predicate field.

| Scenario | `actual` values (both runs) | Threshold outcomes (both runs) | SMAPI log (normalized) |
|---|---|---|---|
| 005 | `soilHealth` 80, `healthTier` healthy, `phaseDays` 3 | predicate-0 (×0.85) pass; predicate-1 (healthy band) pass; predicate-2-adjacent smapiLog isMainPlayer pass | filtered 80/80 lines, **0 differences** after timestamp/`elapsed ms` stripping |
| 017 | `soilHealth` 40, `healthTier` moderate, `phaseDays` 4 | predicate-0 (×0.8 soil) pass; predicate-1 (×1.0 phaseDays) pass; predicate-2 (isMainPlayer) pass | filtered 37/37 lines; only one identical `Saved data for key 'visualization_config_modfolder'` TRACE line swapped position (load-order artifact), no content change |

Post-restore runs corroborate the baseline (bundled with the mutation ledger): 005 `20261108T173619Z-070e` and 017 `20261108T173759Z-7c21` restate the same actual values.

Coverage: **4 of 19** scenario files now carry 2× determinism evidence (004, 013 from pass 11; 005 and 017 from pass 13). The rest remain residual.

## Pass 15 addition (2026-11-08) — 013 bare-tile and 014 (coverage now 6 of 19)

Command shape:

```bash
./scripts/verify-scenario.sh --scenario <scenario.json> --no-acquire \
  --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build --timeout 420
```

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 013 `mulch-bare-tile-decay-exemption` (v4, 2-signal, seed 123456789) | `evidence-bundle-run-20261108T211743Z-62f5` | `evidence-bundle-run-20261108T211930Z-69c9` | pass / pass |
| 014 `soil-health-harvest-quality` (v3, 12 predicates, seed 1) | `evidence-bundle-run-20261108T212204Z-79e7` | `evidence-bundle-run-20261108T212407Z-6576` | pass / pass |

Semantic comparison (same method as pass 11/13): all compared JSON files identical per pair; `evaluation/thresholds.jsonl` identical.

| Scenario | `actual` values (both runs) | Threshold outcomes (both runs) | SMAPI log (normalized) |
|---|---|---|---|
| 013 | `soilHealth` 50, tier moderate | predicate-0 (×1.0) pass; predicate-1 (`isMainPlayer`) pass | filtered 93/93 lines, **0 differences** after timestamp/ms stripping |
| 014 | `soilHealth` 90, tier healthy; `poorHarvestQuality` 0, `moderateHarvestQuality` 1, `healthyHarvestQuality` 1; `healthyExtraCount` 1 | all 12 predicates pass | filtered 34/34 lines, **0 differences** after stripping |

Post-restore runs corroborate the baseline (bundled with the mutation ledger): 013 `20261108T213615Z-51b4` and 014 `20261108T213820Z-4874` restate the same actual values.

Coverage: **6 of 19** scenario files now carry 2× determinism evidence (004, 013-expiry, 005, 017, 013-bare-tile, 014). The rest remain residual.

## Pass 16 addition (2026-11-08) — 005-poor and 017-clamped (coverage now 8 of 19)

Command shape:

```bash
./scripts/verify-scenario.sh --scenario <scenario.json> --no-acquire \
  --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build --timeout 420
```

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 005 `soil-health-growth-poor-soil` (poor 10, seed 987654321) | `evidence-bundle-run-20261109T025423Z-3865` | `evidence-bundle-run-20261109T025553Z-7ba7` | pass / pass |
| 017 `agrochemical-clamped` (measured 5, seed 123456789) | `evidence-bundle-run-20261109T025730Z-1080` | `evidence-bundle-run-20261109T025841Z-2be9` | pass / pass |

Semantic comparison (same method): all compared JSON files identical per pair; `evaluation/thresholds.jsonl` identical.

| Scenario | `actual` values (both runs) | Threshold outcomes (both runs) | SMAPI log (normalized) |
|---|---|---|---|
| 005-poor | `soilHealth` 10, tier poor, `phaseDays` 5, `currentPhase` 0 | predicate-0 (×1.15) pass; predicate-1 (poor band) pass | filtered 80/80 lines; 2 diff lines: one identical `Overlay renderer cache invalidated.` line swapped position (load-order artifact) |
| 017-clamped | `soilHealth` 0, tier poor, `phaseDays` 5 | predicate-0 (`soil cmp eq 0`) pass; predicate-1 (×1.15) pass; predicate-2 (`isMainPlayer`) pass | filtered 37/37 lines, **0 differences** after stripping |

Post-restore runs corroborate the baseline (bundled with the mutation ledger): 005-poor `20261109T030410Z-4cd0` and 017-clamped `20261109T030553Z-391b` restate the same actual values.

Coverage: **8 of 19** scenario files now carry 2× determinism evidence (004, 013-expiry, 005-healthy, 017-tradeoff, 013-bare-tile, 014, 005-poor, 017-clamped). The rest remain residual.

## Pass 17 addition (2026-11-08) — 015-harmony, 014-zero, 004-healthy (coverage now 11 of 19)

Command shape:

```bash
./scripts/verify-scenario.sh --scenario <scenario.json> --no-acquire \
  --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build --timeout 420
```

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 015 `harmony-harvest-quality-host` (v3, 12 predicates, seed 1) | `evidence-bundle-run-20261109T031507Z-702d` | `evidence-bundle-run-20261109T031644Z-755d` | pass / pass |
| 014 `soil-health-harvest-measured-zero` (seed 123456789) | `evidence-bundle-run-20261109T031831Z-3838` | `evidence-bundle-run-20261109T032001Z-77c0` | pass / pass |
| 004 `compost-growth-healthy` (healthy 80, seed 123456789) | `evidence-bundle-run-20261109T032111Z-4e06` | `evidence-bundle-run-20261109T032235Z-3b83` | pass / pass |

Semantic comparison (same method): all compared JSON files identical per pair; `evaluation/thresholds.jsonl` identical.

| Scenario | `actual` values (both runs) | Threshold outcomes | SMAPI log (normalized) |
|---|---|---|---|
| 015 | `soilHealth` 90, tier healthy; `poorHarvestQuality` 0, `moderateHarvestQuality` 1, `healthyHarvestQuality` 1; `healthyExtraCount` 1 | all 12 predicates pass | filtered 34/34 lines, **0 differences** |
| 014-zero | `soilHealth` 0, tier poor; `zeroHealthTier` poor, `zeroVanillaQuality` 1, `zeroHarvestQuality` 0 | predicates 0–3 pass | filtered 36/36 lines; 4 diff lines (identical `Saved data …` / `Overlay renderer cache invalidated.` lines swapped position — load-order artifact), no content change |
| 004-healthy | `soilHealth` 80, tier healthy, `phaseDays` 3 | predicate-0 (×0.85) pass; predicate-1 (healthy band) pass | filtered 82/83 lines; 1 extra identical `Pruned orphan bin record at Farm:60,15` line in run 2 |

Post-restore runs corroborate the baselines (bundled with the mutation ledger): 015 `20261109T032903Z-29cf`, 014-zero `20261109T033015Z-0e1b`, 004-healthy `20261109T033119Z-6fda`.

Coverage: **11 of 19** scenario files now carry 2× determinism evidence (004/013-expiry/005/017/013-bare-tile/014/005-poor/017-clamped + 015/014-zero/004-healthy). The rest remain residual.

## Pass 18 addition (2026-11-08) — 004-moderate and 005-moderate (coverage now 13 of 19)

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 004 `compost-growth-moderate` (moderate 50, seed 123456789) | `evidence-bundle-run-20261109T033419Z-5316` | `evidence-bundle-run-20261109T033540Z-40ca` | pass / pass |
| 005 `soil-health-growth-moderate` (moderate 50, seed 987654321) | `evidence-bundle-run-20261109T033704Z-21fe` | `evidence-bundle-run-20261109T033828Z-11c4` | pass / pass |

Semantic comparison (same method): all compared JSON files identical per pair. Threshold outcomes identical (`phaseDays` 4, predicate-0 ×1.0 pass; predicate-1 moderate band `[31,71)` pass).

| Scenario | `actual` values (both runs) | SMAPI log (normalized) |
|---|---|---|
| 004-moderate | `soilHealth` 50, tier moderate, `phaseDays` 4 | filtered 83/83 lines, **0 differences** after stripping |
| 005-moderate | `soilHealth` 50, tier moderate, `phaseDays` 4 | filtered 81/80 lines; 1 extra identical `Pruned orphan bin record at Farm:60,15` in run 1 |

Post-restore runs corroborate the baselines (bundled with the mutation ledger): 004-moderate `20261109T034318Z-7374`, 005-moderate `20261109T034443Z-4cc1`.

Coverage: **13 of 19** scenario files now carry 2× determinism evidence (004/013-expiry/005/017/013-bare-tile/014/005-poor/017-clamped/015/014-zero/004-healthy + 004-moderate/005-moderate). The rest remain residual.

## Pass 19 addition (2026-11-08) — 017-unknown-baseline (coverage now 14 of 19) + 017-refused-at-minimum (deterministic fail)

### 017-unknown-baseline (coverage +1)

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 017 `agrochemical-unknown-baseline` (NaN → 100 → one apply → 90, seed 123456789) | `evidence-bundle-run-20261109T034720Z-49c5` | `evidence-bundle-run-20261109T034828Z-12e6` | pass / pass |

Semantic comparison (same method): all compared JSON files identical per pair; `evaluation/thresholds.jsonl` identical.

| Scenario | `actual` values (both runs) | SMAPI log (normalized) |
|---|---|---|
| 017-unknown-baseline | `soilHealth` 90 healthy, `phaseDays` 3 | filtered 37/37 lines, **0 differences** after stripping |

### 017-refused-at-minimum — deterministic failure (documented, not coverage)

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 017 `agrochemical-refused-at-minimum` (soil 0, seed 123456789) | `run-20261109T034940Z-036e` | `run-20261109T035049Z-5ac1` | **false / false** (both) |

Semantic evidence identical per pair (both failed the same predicate). Clean deterministic failure: `soilHealth` 0 unchanged (predicate-0 `soilHealth multiplier 1.0` → pass), `cropPhaseDelta` pre `phaseDays` 4 → actual **5** (predicate-1 ×1.0 → **fail**) — soil 0 is poor, so `SoilGrowthPoorMultiplier 1.15` applies to the displayed phaseDays; `isMainPlayer` passes. The machine-pass promoted once as `run-20261107T135531Z-7c4e` (post dump captured before the DayEnding growth modifier applied — capture-order flake); the environment on 2026-11-09 consistently captures after it. `--verify-bundle` exit 0 (schema is fine; the assertion is what fails).

This pair is **deliberately not added as green coverage**; it is recorded as a deterministic failure against a stale assertion. The 8 remaining residual scenarios are unchanged in scope, plus refused-at-minimum is flagged `partial/broken-assertion`, not green.

Coverage: **14 of 19** scenario files carry 2× green determinism evidence. The rest: 4 residual green-eligible files, plus refused-at-minimum (deterministic fail at a stale assertion).

## Pass 20 addition (2026-11-08) — 014-unknown, 013-decay-exemption, 013-refused-on-ineligible, 017-refused-contention (coverage now 18 of 19)

Command shape:

```bash
./scripts/verify-scenario.sh --scenario <scenario.json> --no-acquire \
  --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build --timeout 420
```

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 014 `soil-health-harvest-unknown` (NaN, seed 123456789) | `evidence-bundle-run-20261109T035748Z-1fc9` | `evidence-bundle-run-20261109T035851Z-759d` | pass / pass |
| 013 `mulch-decay-exemption` (cropped, seed 123456789) | `evidence-bundle-run-20261109T035954Z-4aa8` | `evidence-bundle-run-20261109T040102Z-50fe` | pass / pass |
| 013 `mulch-refused-on-ineligible` (already mulched, seed 123456789) | `evidence-bundle-run-20261109T040219Z-5080` | `evidence-bundle-run-20261109T040327Z-1ea4` | pass / pass |
| 017 `agrochemical-refused-contention` (lock seam, seed 123456789) | `evidence-bundle-run-20261109T040437Z-7022` | `evidence-bundle-run-20261109T040546Z-2fb6` | pass / pass |

Semantic comparison (same method): all compared JSON files identical per pair; `evaluation/thresholds.jsonl` identical.

| Scenario | `actual` values (both runs) | Threshold outcomes | SMAPI log (normalized) |
|---|---|---|---|
| 014-unknown | tier unknown; `unknownVanillaQuality` 1; `unknownHarvestQuality` 1 | predicates 0–3 pass | filtered 36/36 lines, **0 differences** |
| 013-decay-exemption | `soilHealth` 50, tier moderate, `phaseDays` 4 | predicate-0 ×1.0 pass; predicate-2 isMainPlayer pass | filtered 38/38 lines, **0 differences** |
| 013-refused-on-ineligible | `soilHealth` 50, tier moderate, `phaseDays` 4 | predicates pass | filtered 40/40 lines, **0 differences** |
| 017-refused-contention | `soilHealth` 50 (lock blocked mutation), tier moderate, `phaseDays` 4 | predicates pass | filtered 37/37 lines, **0 differences** |

Post-restore runs corroborate the baselines (bundled with the mutation ledger): 014-unknown `20261109T041048Z-088b`.

Coverage: **18 of 19** scenario files now carry 2× determinism evidence (004, 005, 013, 014, 015, 017 families). The one scenario not covered remains `017-refused-at-minimum` (deterministic fail at a stale crop-phase-predicate, documented).

## Pass 22 addition (2026-11-09) — 017-refused-at-minimum (restored green)

| Scenario | Run 1 | Run 2 | Verdicts |
|---|---|---|---|
| 017 `agrochemical-refused-at-minimum` (v4, 2-signal; soil at minimum, seed 123456789) | `evidence-bundle-run-20261109T041743Z-329c` | `evidence-bundle-run-20261109T041859Z-6938` | pass / pass |

Semantic evidence identical per pair; `evaluation/thresholds.jsonl` identical. Filtered SMAPI logs after stripping: **37/37 lines, 0 differences**. `soilHealth` 0 (poor), `isMainPlayer` true.

Previously recorded as a deterministic fail at a stale predicate-1 — now resolved by an honest v4 `not-applicable` rewrite of `cropPhaseDelta` (identical pattern to the 013 bare-tile v4 scenario), with an explicit `signalPolicy` reason. **Determinism coverage is now 19 of 19.**
