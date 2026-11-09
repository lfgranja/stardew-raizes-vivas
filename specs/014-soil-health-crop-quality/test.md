# Feature Test Record: 014-soil-health-crop-quality

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261109T033015Z-0e1b
- **Commit**: 47bb56635b1687c4b3e5b11e57ac66a328ff1433
- **Date**: 2026-11-09T03:30:46Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261109T033015Z-0e1b/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261109T033015Z-0e1b/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261109T033015Z-0e1b/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261109T033015Z-0e1b/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->

## Append-only run notes (pass 15, 2026-11-08)

- `soil-health-harvest-quality` (host, 12-predicate Harmony probe) rerun twice on seed 1: `run-20261108T212204Z-79e7` / `run-20261108T212407Z-6576` — both **pass**; bundles verify exit 0; semantic evidence identical. Logs: 34/34 filtered mod-relevant lines identical after stripping.
- Mutation negative control **used**: decision seam `HarvestQualityService` Poor branch `ForceQualityZero true → false` → `run-20261108T213324Z-3925`: poor probe harvestQuality observed 1 (scenario requires 0), predicate-1 **false**, all other 11 predicates pass; exit 19; source restored (`git checkout`), post-restore `run-20261108T213820Z-4874` **pass**.
- Mutation negative control **discarded (honest)**: `SoilQualityHealthyBoost 0.08f → 0f` → `run-20261108T212933Z-7976`: the harvest matrix could not fill its probes from 4188 candidates, so every predicate returned `unmeasured-actual`. This is a *selection/harness* failure, not an assertion kill; the mutant was rejected as SC-MP-6 evidence and replaced by the decision-seam mutant above. Recorded in `verification/mutation-checks.md`.
- GATE3 block is runner-promoted to the post-restore run `run-20261108T213820Z-4874` (commit `b945f81`); not hand-edited.

## Append-only run notes (pass 17, 2026-11-08)

- `soil-health-harvest-measured-zero` (measured 0/Poor) rerun twice on seed 123456789: `run-20261109T031831Z-3838` / `run-20261109T032001Z-77c0` — both **pass**; bundles verify exit 0; semantic evidence identical (`zeroHealthTier` poor, `zeroVanillaQuality` 1, `zeroHarvestQuality` 0). Logs: 36/36 filtered lines; 4 diff lines (identical `Saved data …` / `Overlay renderer cache invalidated.` lines swapped position — load-order artifact).
- Mutation negative control (same decision seam as the 014/015 quality probes): `HarvestQualityService` Poor branch `ForceQualityZero` true→false → `run-20261109T032550Z-1e3b`, `zeroHarvestQuality` 1 (expected 0), predicate-2 **false**; exit 19; source restored (`git checkout`), post-restore `run-20261109T033015Z-0e1b` **pass**. See `verification/mutation-checks.md`.
- GATE3 block is runner-promoted to the post-restore run `run-20261109T033015Z-0e1b` (commit `47bb566`); not hand-edited.
