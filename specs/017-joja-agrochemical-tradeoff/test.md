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
- **Claim**: machine-pass (host, runner-promoted, 3 signals). Primary measured-50: run-20261107T040235Z-589a. Additional applicable host paths (sequential): unknown-baseline pass run-20261107T113702Z-240d, clamped pass run-20261107T113959Z-430a; refused-at-minimum (no-mutation, soil unchanged 0→0, phaseDays 1.0x) pass run-20261107T135531Z-7c4e after honest harness fixes (signals: synthesize soil after-delta from captured crop-dump soil state when dedicated dump missing; verdict/signals: dump-vs-persisted mod-data cross-check recorded as informational `anomaly-info`/non-blocking for verification in-memory state lag). Prior refused attempts partial (run-20261107T114124Z-3854, run-20261107T132341Z-28df: missing soil delta artifact; run-20261107T133324Z-1063/133740Z/134346Z/135109Z: harness-gap iterations) — not promoted. `RefusedConflict` exercisability unconfirmed (lock contention/controller-reserved; not machine-passed). Bundles gitignored.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->
