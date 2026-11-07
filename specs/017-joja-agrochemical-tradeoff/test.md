# Feature Test Record: 017-joja-agrochemical-tradeoff

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261107T040235Z-589a
- **Commit**: c9ff596db6268550b2559fccec894eb8cfcd8d7c
- **Date**: 2026-11-07T04:03:17Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261107T040235Z-589a/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261107T040235Z-589a/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261107T040235Z-589a/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261107T040235Z-589a/`
- **Honesty level**: R
- **Claim**: machine-pass (host, runner-promoted, 3 signals). Primary measured-50: run-20261107T040235Z-589a. Additional applicable host paths (sequential, 2026-11-07): unknown-baseline pass run-20261107T113702Z-240d, clamped pass run-20261107T113959Z-430a. Refused-at-minimum: partial run-20261107T114124Z-3854 (signals incomplete: missing soil-health delta artifact on no-mutation refusal; thresholds pred0/pred2 pass, pred1 mismatch vs observed phaseDays behavior — runner/attestation gap, not promoted). Bundles gitignored.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->
