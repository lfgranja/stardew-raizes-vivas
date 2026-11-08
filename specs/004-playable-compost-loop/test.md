# Feature Test Record: 004-playable-compost-loop

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261108T143604Z-0f8f
- **Commit**: 37032ac3c03e5b4ca66d8c45415cd0f906d988ec
- **Date**: 2026-11-08T14:37:16Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261108T143604Z-0f8f/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261108T143604Z-0f8f/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261108T143604Z-0f8f/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261108T143604Z-0f8f/`
- **Honesty level**: R
- **Claim**: machine-pass (host, runner-promoted, 3 signals). All applicable host compost-growth tier paths (sequential, 2026-11-07): poor 10/1.15x primary run-20261105T053313Z-535d, moderate 50/1.0x parity pass run-20261107T120435Z-1ea9, healthy 80/0.85x pass run-20261107T120602Z-69ea. Scenarios mirror hardening structure (bin/place, insertWaste, 5-day sleep, 60,15 LongPhaseCrop). Bundles gitignored. Pass 11 determinism 2× (2026-11-08): run-20261108T141912Z-6948 / run-20261108T142058Z-4b8d; mutation negative control run-20261108T142806Z-67d7.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->