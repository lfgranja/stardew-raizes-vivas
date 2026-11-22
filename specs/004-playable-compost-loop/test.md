# Feature Test Record: 004-playable-compost-loop

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261122T030046Z-79af
- **Commit**: 469b4c118490e660a9c5968cd91f94707de3c3d1
- **Date**: 2026-11-22T03:01:37Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261122T030046Z-79af/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261122T030046Z-79af/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261122T030046Z-79af/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261122T030046Z-79af/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->
## Append-only run notes (pass 17, 2026-11-08)

- `compost-growth-healthy` (healthy 80) rerun twice on seed 123456789: `run-20261109T032111Z-4e06` / `run-20261109T032235Z-3b83` — both **pass**; bundles verify exit 0; semantic evidence identical (`soilHealth` 80 healthy, `phaseDays` 3). Logs: 82/83 filtered lines, one extra identical `Pruned orphan bin record at Farm:60,15` in run 2.
- Mutation negative control: `SoilGrowthHealthyMultiplier 0.85f → 1.00f` → `run-20261109T032724Z-2b7d`, `phaseDays` 4 (expected 3), predicate-0 ×0.85 **false**, exit 19; source restored, post-restore `run-20261109T033119Z-6fda` **pass**. See `verification/mutation-checks.md`.
- GATE3 block is runner-promoted to the post-restore run `run-20261109T033119Z-6fda` (commit `47bb566`); not hand-edited.

## Append-only run notes (pass 18, 2026-11-08)

- `compost-growth-moderate` (moderate 50) rerun twice on seed 123456789: `run-20261109T033419Z-5316` / `run-20261109T033540Z-40ca` — both **pass**; bundles verify exit 0; semantic evidence identical (`phaseDays` 4). Logs: 83/83 filtered lines, 0 diffs.
- Mutation negative control (shared edge): `ModerateHealthyBandEdge 71f → 0f` → `run-20261109T034019Z-773e`, `phaseDays` 3 (expected 4), predicate-0 ×1.0 **false**, exit 19; post-restore `run-20261109T034318Z-7374` **pass**. See `verification/mutation-checks.md`.
- GATE3 block is runner-promoted to the post-restore run `run-20261109T034318Z-7374` (commit `e6a3da1`); not hand-edited.

## Append-only run notes (ciclo 030, 2026-11-21)

- `compost-growth-hardening` reexecutado após alinhamento do threshold de saúde do solo com `ModConstants.PoorModerateBandEdge = 31f` (`"max": 34` → `31`, `exclusiveMin: false`, `exclusiveMax: true`, descrição "Poor interval [0, 31)"): `run-20261121T232308Z-64cf`, `verdict=pass`, `honestyLevel=R`, `signalsComplete=true`, `signalsAgree=true`, `thresholdsOk=true`, 2 predicates aprovados, `--verify-bundle` exit 0.
- Bloco de marcadores mantido pelo runner promovido para este run; não editado manualmente. `machine-pass` host-only — nenhuma atestação humana reivindicada.
