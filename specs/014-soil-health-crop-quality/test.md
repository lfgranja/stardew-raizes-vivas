# Feature Test Record: 014-soil-health-crop-quality

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261106T014750Z-14b6
- **Commit**: 3f5235cda6798206956e2196a99d0c99833194ad
- **Date**: 2026-11-06T01:48:37Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261106T014750Z-14b6/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261106T014750Z-14b6/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261106T014750Z-14b6/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261106T014750Z-14b6/`
- **Honesty level**: R
- **Claim**: machine-pass (host, runner-promoted, 3 signals). Primary matrix (poor/moderate/healthy probes): run-20261106T014750Z-14b6; regression post-seam still pass run-20261107T122915Z-2564. Additional 014 A7 applicable host paths (sequential, 2026-11-07, verification seam 2451c8e): NaN/unknown→vanilla (no forced quality, no boost) pass run-20261107T123141Z-3531 (first attempt run-20261107T122437Z-2f19 partial/no-dump, rerun clean); measured-zero→Poor/force quality 0 pass run-20261107T123257Z-08c9. Bundles gitignored.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->
