# Feature Test Record: 004-playable-compost-loop

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261105T053313Z-535d
- **Commit**: 0c52552d2fb3115f48339d5018d28baacbe35311
- **Date**: 2026-11-05T05:36:36Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261105T053313Z-535d/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261105T053313Z-535d/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261105T053313Z-535d/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261105T053313Z-535d/`
- **Honesty level**: R
- **Claim**: machine-pass (host, runner-promoted, 3 signals). All applicable host compost-growth tier paths (sequential, 2026-11-07): poor 10/1.15x primary run-20261105T053313Z-535d, moderate 50/1.0x parity pass run-20261107T120435Z-1ea9, healthy 80/0.85x pass run-20261107T120602Z-69ea. Scenarios mirror hardening structure (bin/place, insertWaste, 5-day sleep, 60,15 LongPhaseCrop). Bundles gitignored.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->