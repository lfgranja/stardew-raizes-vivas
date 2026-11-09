# Feature Test Record: 017-joja-agrochemical-tradeoff

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261109T030553Z-391b
- **Commit**: edaa167af2defae4b4a1fbb7a37f4bf204dc419d
- **Date**: 2026-11-09T03:06:35Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261109T030553Z-391b/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261109T030553Z-391b/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261109T030553Z-391b/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261109T030553Z-391b/`
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

## Append-only run notes (pass 13, 2026-11-08)

- `agrochemical-tradeoff` (measured 50) rerun twice on seed 123456789: `run-20261108T172717Z-31c7` and `run-20261108T172927Z-5f08` — both **pass**; bundles verify exit 0; semantic evidence identical (`soilHealth` 40 moderate, `phaseDays` 4; predicates ×0.8 / ×1.0 / isMainPlayer all pass).
- Determinism artifacts identical; SMAPI log filtered to mod-relevant lines is identical after timestamp/ms stripping (37 lines both runs) except one identical `visualization_config_modfolder` TRACE line swapped position (load-order artifact). See `specs/018-machine-pass-full-coverage/verification/determinism-checks.md`.
- Mutation negative control: `AgrochemicalSoilPenalty 10f → 20f` → `run-20261108T173425Z-7088`, soil 50→30, predicates ×0.8 (false) and ×1.0 (false); exit 19; source restored, post-restore `run-20261108T173759Z-7c21` **pass**. See `verification/mutation-checks.md`.
- GATE3 block above is runner-promoted to the post-restore run `run-20261108T173759Z-7c21` (commit `98bb697`); not hand-edited.

## Append-only run notes (pass 16, 2026-11-08)

- `agrochemical-clamped` (measured 5, factor `soil cmp eq 0`) rerun twice on seed 123456789: `run-20261109T025730Z-1080` / `run-20261109T025841Z-2be9` — both **pass**; bundles verify exit 0; semantic evidence identical (`soilHealth` 0 poor, `phaseDays` 5). Logs: 37/37 filtered lines identical after stripping.
- Mutation negative control: `AgrochemicalSoilPenalty 10f → 0f` → `run-20261109T030225Z-2cb3`, soil stays 5 (clamp never triggers), predicate-0 (`soil cmp eq 0`) **false**; exit 19; source restored, post-restore `run-20261109T030553Z-391b` **pass** (`soilHealth` 0). See `verification/mutation-checks.md`.
- GATE3 block is runner-promoted to the post-restore run `run-20261109T030553Z-391b` (commit `edaa167`); not hand-edited.
