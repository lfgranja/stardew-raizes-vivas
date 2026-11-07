# Feature Test Record: 005-soil-health-to-crop-growth

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261107T112602Z-5b08
- **Commit**: a207952621ed9877cae68f01af82c37f43459f37
- **Date**: 2026-11-07T11:26:53Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261107T112602Z-5b08/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261107T112602Z-5b08/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261107T112602Z-5b08/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261107T112602Z-5b08/`
- **Honesty level**: R
- **Claim**: machine-pass (host, runner-promoted, 3 signals). All applicable host growth paths (2026-11-07, sequential): healthy 80/0.85x pass run-20261107T112602Z-5b08, poor 10/1.15x pass run-20261107T114448Z-4b61, moderate 50/1.0x parity pass run-20261107T114623Z-3365. Scenarios on-map 60,15 with LivingRoots.Verification.LongPhaseCrop + 5-day sleep. Bundles gitignored.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->