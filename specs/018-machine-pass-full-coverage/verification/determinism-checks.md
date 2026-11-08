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
