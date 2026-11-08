# Feature Test Record: 005-soil-health-to-crop-growth

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261108T173619Z-070e
- **Commit**: 98bb69716fd5052e2b07485fd85e9871f322f5aa
- **Date**: 2026-11-08T17:37:24Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261108T173619Z-070e/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261108T173619Z-070e/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261108T173619Z-070e/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261108T173619Z-070e/`
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
