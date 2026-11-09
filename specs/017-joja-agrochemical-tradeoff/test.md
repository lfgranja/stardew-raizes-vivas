# Feature Test Record: 017-joja-agrochemical-tradeoff

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261109T040546Z-2fb6
- **Commit**: 156a3676132a0893f00224ba6df7bfb7b8d6650a
- **Date**: 2026-11-09T04:06:22Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261109T040546Z-2fb6/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261109T040546Z-2fb6/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261109T040546Z-2fb6/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261109T040546Z-2fb6/`
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

## Append-only run notes (pass 19, 2026-11-08)

- `agrochemical-unknown-baseline` (NaN → 100 → one apply → 90) rerun twice on seed 123456789: `run-20261109T034720Z-49c5` / `run-20261109T034828Z-12e6` — both **pass**; bundles verify exit 0; semantic evidence identical (`soilHealth` 90 healthy, `phaseDays` 3; predicates `soilHealth eq 90`, ×0.85, isMainPlayer all pass). Logs: 37/37 filtered lines identical after stripping.
- Mutation negative control: `AgrochemicalSoilPenalty 10f → 20f` → `run-20261109T035309Z-4cbc`, soil 80 (expected 90), predicate-0 `soilHealth eq 90` **false**, exit 19; source restored, post-restore `run-20261109T035430Z-76c0` **pass** (`soilHealth` 90). See `verification/mutation-checks.md`.
- **`agrochemical-refused-at-minimum` — regression finding, not green:** both runs of pass 19 failed: `run-20261109T034940Z-036e` and `run-20261109T035049Z-5ac1`, **identical** predicate-1 failure (`farmer_seed` same): `soilHealth` 0 unchanged (predicate-0 pass — the refusal itself works), crop-phase predicate `×1.0` **false** (pre `phaseDays` 4 → actual 5; soil 0 → poor → `SoilGrowthPoorMultiplier` applies). The promoted machine-pass `run-20261107T135531Z-7c4e` read phaseDays 4→4 and was a one-time lucky capture (post-dump captured before the DayEnding growth modifier applied; today the dump runs after it). The earlier `run-20261107T114124Z-3854` also failed at this same predicate. Both of today's bundles verify `--verify-bundle` exit 0, so this is a **scenario-assertion failure, not an infrastructure defect**. Honest status: the scenario as written (refuses apply; `SoilHealth unchanged`; `phaseDays` unchanged) does not hold under the current modifier pipeline — predicates 0 and 2 pass, predicate-1 fails deterministically.
  - **Do not read this as "the feature broke."** The agrochemical refusal at minimum works (soil at 0 is refused, no mutation is applied); only the crop-phase assertion is unsatisfiable for poor soil in this capture path. A silent flake produced a one-time green; two honest repeats expose a stale assertion.
  - **Reviewers to decide:** the scenario predicate (`phaseDays ×1.0`) would have to change for a green today (e.g., only for soil in a non-poor tier), or the capture dump must be ordered before the growth modifier for this scenario. No fix is applied in this pass; the record is append-only and honest.
- GATE3 block above is runner-promoted to the post-restore run `run-20261109T035430Z-76c0` (commit `31b587d`); not hand-edited.

## Append-only run notes (pass 20, 2026-11-08)

- `agrochemical-refused-contention` (host, authorized contention seam) rerun twice on seed 123456789: `run-20261109T040437Z-7022` / `run-20261109T040546Z-2fb6` — both **pass**; bundles verify exit 0; semantic evidence identical (soil 50 unchanged under held lock, phaseDays 4). Logs: 37/37 filtered lines identical after stripping.
- Mutation feasibility: the refusal path (`TryAcquireLock` fails → action-conflict before service commit/consumption) has no decision seam that changes the already-declared signals (soil unchanged in both outcomes). No root-level 014/017 mutation kills this scenario; recorded honestly in `verification/mutation-checks.md` — **no kill claimed**. 2× determinism stands as its evidence.
- GATE3 block is runner-promoted to `run-20261109T040546Z-2fb6` (commit `156a367`); not hand-edited.
