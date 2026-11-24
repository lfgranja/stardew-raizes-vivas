# Feature Test Record: 015-harmony-harvest-seam

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261109T032903Z-29cf
- **Commit**: 47bb56635b1687c4b3e5b11e57ac66a328ff1433
- **Date**: 2026-11-09T03:29:42Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261109T032903Z-29cf/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261109T032903Z-29cf/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261109T032903Z-29cf/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261109T032903Z-29cf/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->

## Append-only evidence notes (2026-11-07, MDMV 80/20; no new authorization)

- **SC-001 (HarvestQualityService seam, no game runtime):** `GamePath=/home/luis/stardew-assemblies dotnet test --filter "FullyQualifiedName~HarvestQualityServiceTests" -c Release --no-restore` → **17 passed, 0 failed, 0 skipped** (559 ms, fresh). Real test evidence.
- **SC-002/SC-005 (build/format context):** solution builds 0 errors in this session (`dotnet build Stardew-LivingRoots.sln -c Release --no-restore`); format not re-run in this note (see prior records/pre-push).
- **SC-003 (Harmony registration, real smapiLog signal):** headless bundles show `Living Roots: Harvest Harmony patch registered` with no Harmony/harvest-patch errors in checked logs: `evidence-bundle-run-20261106T014750Z-14b6/artifacts/smapi-log.txt` (014 harvest-matrix machine-pass), `evidence-bundle-run-20261107T193756Z-6d97/artifacts/smapi-log.txt`. Real-signal evidence (log), not GUI/visual.
- **SC-004 (in-game Gate 3, 3 signals, D1 second player):** **not satisfied** by this headless evidence. 014 achieved runner `machine-pass` for harvest-quality/matrix host paths using the 015-enabled seam (`run-20261106T014750Z-14b6` primary, plus 014 test.md records) — strong runtime evidence of the seam, but 015's own SC-004 (explicit Gate 3 + D1 multiplayer verification) remains **partial/pending**. Do not promote 015 to `verified`/`machine-pass` on this basis.
- Honesty: `machine-pass` (014 paths) ≠ 015 SC-004 complete; D1/render/balance/human out of scope here.

## Append-only: 015 host 3-signal runner machine-pass (2026-11-07, autonomous 80/20; no GATE3 hand-edit)
- **Scenario:** `specs/015-harmony-harvest-seam/scenarios/harmony-harvest-quality-host.scenario.json` (v3, host, 014-compatible Harmony harvest matrix probes; `rngSeed`/deterministic; `testMdPath` 015). Runner-promoted GATE3 points to `run-20261107T210830Z-67f1` (not hand-edited).
- **Result:** `evidence-bundle-run-20261107T210830Z-67f1/` verdict=`pass` actual=`pass` signalsComplete=true thresholdsOk=true anchor present. Real 3-signal host attestation of Harmony `Crop.harvest` seam (poor forced quality 0, moderate vanilla untouched, healthy +0.08 quality boost / +0.10 extra with deterministic roll windows) — consistent with 014 matrix passes (`014750Z-14b6` primary). `HarvestQualityServiceTests` 17/17 (fresh earlier) and Harmony registration in smapi logs remain valid.
- **SC-004 status (honest):** SC-004 requires “3 signals… with a second player for multiplayer verification (D1)”. This run satisfies the **host 3-signal** Gate 3 aspect for the Harmony harvest seam; **D1 multiplayer remains pending/blocked-feasible (T002 split-screen/headless)**. Full SC-004 not complete; do not claim D1/verified. `machine-pass` (host scenario) ≠ full SC-004 ≠ human-verified/D1/render/balance.

## Append-only run notes (pass 17, 2026-11-08)

- `harmony-harvest-quality-host` (Harmony seam, seed 1) rerun twice: `run-20261109T031507Z-702d` / `run-20261109T031644Z-755d` — both **pass**; bundles verify exit 0; semantic evidence identical (12/12 predicates pass). Logs: 34/34 filtered lines identical after stripping.
- Mutation negative control: decision seam `HarvestQualityService` Poor branch `ForceQualityZero` true→false → `run-20261109T032436Z-61fb`, `poorHarvestQuality` 1 (expected 0), predicate-1 **false**; exit 19; source restored (`git checkout`), post-restore `run-20261109T032903Z-29cf` **pass**. See `verification/mutation-checks.md`.
- GATE3 block is runner-promoted to the post-restore run `run-20261109T032903Z-29cf` (commit `47bb566`); not hand-edited.
