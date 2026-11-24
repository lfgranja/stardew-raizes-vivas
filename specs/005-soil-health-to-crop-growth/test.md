# Feature Test Record: 005-soil-health-to-crop-growth

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261122T041058Z-73a0
- **Commit**: 7f3a4427a028300b22caeff92f03b69675eea34c
- **Date**: 2026-11-22T04:15:17Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261122T041058Z-73a0/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261122T041058Z-73a0/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261122T041058Z-73a0/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261122T041058Z-73a0/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->
## Append-only run notes (pass 13, 2026-11-08)

- `soil-health-growth` (healthy 80) rerun twice on seed 987654321: `run-20261108T172115Z-060e` and `run-20261108T172354Z-5b8c` — both **pass**; bundles verify exit 0; semantic evidence identical (`soilHealth` 80 healthy, `phaseDays` 3).
- Determinism artifacts identical; SMAPI log filtered to mod-relevant lines is identical after timestamp/ms stripping (80 lines both runs). See `specs/018-machine-pass-full-coverage/verification/determinism-checks.md`.
- Mutation negative control: `SoilGrowthHealthyMultiplier 0.85f → 1.00f` → `run-20261108T173204Z-08a0`, predicate-0 (×0.85) **fails**, exit 19; source restored, post-restore `run-20261108T173619Z-070e` **pass**. See `verification/mutation-checks.md`.
- GATE3 block above is runner-promoted to the post-restore run `run-20261108T173619Z-070e` (commit `98bb697`); not hand-edited.

## Append-only run notes (pass 16, 2026-11-08)

- `soil-health-growth-poor` rerun twice on seed 987654321 (poor 10): `run-20261109T025423Z-3865` / `run-20261109T025553Z-7ba7` — both **pass**; bundles verify exit 0; semantic evidence identical (`soilHealth` 10 poor, `phaseDays` 5). Logs: 80/80 filtered mod-relevant lines; 2 diff lines (one identical `Overlay renderer cache invalidated.` line swapped position — load-order artifact).
- Mutation negative control: `SoilGrowthPoorMultiplier 1.15f → 1.00f` → `run-20261109T030033Z-4c21`, `phaseDays` 4 (expected 5), predicate-0 ×1.15 **false**, exit 19; source restored, post-restore `run-20261109T030410Z-4cd0` **pass** (`phaseDays` 5). See `verification/mutation-checks.md`.
- GATE3 block is runner-promoted to the post-restore run `run-20261109T030410Z-4cd0` (commit `edaa167`); not hand-edited.

## Append-only run notes (pass 18, 2026-11-08)

- `soil-health-growth-moderate` (moderate 50) rerun twice on seed 987654321: `run-20261109T033704Z-21fe` / `run-20261109T033828Z-11c4` — both **pass**; bundles verify exit 0; semantic evidence identical (`phaseDays` 4). Logs: 81/80 filtered lines, one extra identical `Pruned orphan bin record at Farm:60,15` in run 1.
- Mutation negative control (shared edge, same as 004-moderate): `ModerateHealthyBandEdge 71f → 0f` → `run-20261109T034143Z-2623`, `phaseDays` 3 (expected 4), predicate-0 ×1.0 **false**, exit 19; post-restore `run-20261109T034443Z-4cc1` **pass**. See `verification/mutation-checks.md`.
- GATE3 block is runner-promoted to the post-restore run `run-20261109T034443Z-4cc1` (commit `e6a3da1`); not hand-edited.

## Append-only run notes (ciclo 032, 2026-11-22)

- `soil-health-growth` reexecutado após alinhamento do threshold Healthy a `ModConstants.ModerateHealthyBandEdge = 71f` (`"min": 67` → `71`, descrição `[71, 100)`): `run-20261122T041058Z-73a0`, `verdict=pass`, `honestyLevel=R`, `signalsComplete=true`, `signalsAgree=true`, `thresholdsOk=true`, `--verify-bundle` exit 0. Bloco de marcadores promovido pelo mecanismo determinístico do runner; nenhuma edição manual. `machine-pass` host-only — nenhuma atestação humana reivindicada.
