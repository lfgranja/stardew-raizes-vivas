# Feature Test Record: 004-playable-compost-loop

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261109T033119Z-6fda
- **Commit**: 47bb56635b1687c4b3e5b11e57ac66a328ff1433
- **Date**: 2026-11-09T03:32:05Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261109T033119Z-6fda/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261109T033119Z-6fda/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261109T033119Z-6fda/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261109T033119Z-6fda/`
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
