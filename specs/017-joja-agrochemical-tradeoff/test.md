# Feature Test Record: 017-joja-agrochemical-tradeoff

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261107T193756Z-6d97
- **Commit**: 090364cb7921f52a7d051c0f554c4b45daf0756f
- **Date**: 2026-11-07T19:38:38Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261107T193756Z-6d97/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261107T193756Z-6d97/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261107T193756Z-6d97/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261107T193756Z-6d97/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->

## Append-only run notes (018, 2026-11-07; explicit user authorization for contention/T002 seams)

- `agrochemical-refused-contention` (host, positive, **authorized contention seam**): **machine-pass** `evidence-bundle-run-20261107T193756Z-6d97/` (commit `090364c`). `lr_acquire_host_lock 60 15 999999` held lock as different farmerId; host `lr_apply_agrochemical` hit controller lock-contention refusal (`TryAcquireLock` failed → `AgrochemicalActionConflict`/action-conflict path; `RefusedConflict` enum reserved) **before** service commit/consumption; `lr_release_host_lock`. Signals: soil `1.0x` (50→50), `phaseDays 1.0x`, `isMainPlayer=true`. No mutation. Exercisability now **attested** via deterministic dev/CI seam (not production contention).
- Prior 017 host `machine-pass` runs (still valid, referenced here because GATE3 is runner-promoted/latest): measured-50 `run-20261107T040235Z-589a`, unknown-baseline `run-20261107T113702Z-240d`, clamped `run-20261107T113959Z-430a`, refused-at-minimum (no-mutation 0→0, harness fixes) `run-20261107T135531Z-7c4e` (see git history/pre-promotion claim). Prior refused partials not promoted. Bundles gitignored.
- T002 farmhand mechanism: `lr_split_screen` added (authorized safe `lr_*` replacement; reflection best-effort for SMAPI `ICommandHelper.Trigger` — **unavailable** in referenced SMAPI build; headless split capability not demonstrated). Farmhand/split-screen **never `machine-pass`**; record attempts as `partial`/blocked-feasible with honest evidence.
