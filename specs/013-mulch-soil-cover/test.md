# Feature Test Record: 013-mulch-soil-cover

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261108T213615Z-51b4
- **Commit**: b945f81d31f3b498be263d6a0e78234f8e6d8654
- **Date**: 2026-11-08T21:37:44Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261108T213615Z-51b4/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261108T213615Z-51b4/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261108T213615Z-51b4/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261108T213615Z-51b4/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->

## Append-only run notes (018 W4, 2026-11-07)

- `mulch-decay-exemption` (host, positive): **machine-pass** `evidence-bundle-run-20261107T141941Z-18e6/` (commit `aadcfed...` context; rerun control `evidence-bundle-run-20261107T142212Z-10d5/` pass). Signals complete/agree/thresholds ok. Attests mulch apply on cropped tilled tile with **no soil mutation** (1.0x) and stable LongPhaseCrop `phaseDays` (1.0x, 1-day capture). **Honesty**: FR-009/SC-004 decay exemption is implemented (`MulchDecayMultiplier=0`, mulch ticked before winter early-return); `SoilDecayService` applies decay only to **bare** HoeDirt (`crop == null`), so this cropped-tile setup does **not** isolate a bare-tile decay delta. Bare-tile decay-exemption remains implementation-verified; a dedicated bare-tilled verification command/scenario would strengthen it.
- `mulch-refused-on-ineligible` (host, positive, already-mulched 2nd apply): **machine-pass** `evidence-bundle-run-20261107T142415Z-4e29/` (commit `1da30a8`). Refusal is attestable (no generation failure; `lr_apply_*` refusal logged, signals prove no mutation).
- Farmhand guard (T028): **blocked** (T002 — queue rejects non-`lr_*` `/debug split`, ack 19; split-screen never `machine-pass`). Not run/fabricated.
- 013 verification seam (018 W4): `lr_give_mulch`/`lr_apply_mulch` parser + `LivingRoots.Verification` handlers + `ILivingRootsModApi.ApplyMulch`/`BindMulchApplication` (delegate to `IMulchApplicationService.TryApplyMulch`). Refusal semantic: apply refusals do not fail generation (attestable no-mutation); `lr_apply_agrochemical` aligned.
- `lr_till` dev/CI seam added (`833dbb6`) for bare HoeDirt setup. Bare-tile SC-004 decay-exemption scenario authored but **schema-blocked** (`scenario-validate` exit 14 `/expectedSignals`: runner requires 3 signals incl. `cropPhaseDelta`; bare tile has no crop → cropPhaseDelta not honestly attestable on same tile). Scenario removed (`15b8542`) to avoid a false/incomplete claim. SC-004 on the actual bare-tile decay path remains **implementation-verified**; future schema-compatible attestation possible (e.g., secondary-crop signal design with defensible thresholds, or validator/signal evolution) — not done here to avoid guessing thresholds.
- **Applicable host 013 paths with current harness**: 2/2 machine-pass (`mulch-decay-exemption` apply+no-mutation/stable-capture, `mulch-refused-on-ineligible` already-mulched refusal). Not 100% of every 013 claim (SC-004 bare-tile delta unisolated by machine-pass; 7-day expiry not run). No fabrication.
- **013 7-day expiry (SC-005) / SC-004 bare-tile delta (2026-11-07, defensible analysis, no guessing):** _superseded by v4 runner machine-pass below (kept for honesty/history)._ Previously blocked for runner `machine-pass` under the 3-signal schema because bare HoeDirt has no crop and `cropPhaseDelta` was required; defensible timing was implementation-verified only (no fabrication).
- **v4 harness/schema evolution (approved, `139f2e3` + fixes this session):** `scenario-v4`/manifest allow 2 declared signals (`soilHealthStateDelta`,`smapiLog`) **only** with explicit `signalPolicy.cropPhaseDelta=not-applicable` + required reason (bare HoeDirt/no-crop decay-path); crop thresholds rejected when crop not declared; declared-signal completeness/agreement; v2/v3 preserved. 010 targeted tests green: schema-contract 52/52, signals 33/33, verdict 45/45, evaluation 10, evidence 22, manifest-schema 7; syntax OK. Fixed manifest finalization (`evidence-bundle.schema.json` minItems/cross-field) and bare-tile verdict pre/actual soil fallback (`before` vs `after`).
- **013 SC-004 bare-tile decay exemption (defensible, runner machine-pass):** `mulch-bare-tile-decay-exemption` (v4, tile 61,15 bare HoeDirt, `lr_till`+soil 50, mulch, **6 spring day-start ticks**, 2-signal not-applicable). **`run-20261107T205141Z-0508` verdict=`pass` actual=`pass` signalsComplete/Agree thresholdsOk=true evaluatorMissing=false honesty R anchor present** (also `205010Z-1fed` pass). `evidence-bundle-run-20261107T205141Z-0508/artifacts/soil-health-before-after.json`: 61,15 before 50, after 50, delta 0. Evaluation pre/actual soil 50.0→50.0 (1.0x). Defensible: `MulchDurationDays=7`, TickMulch decrements then removes at `<=0` before decay; ticks 7→6..2→1 keep mulch active during decay checks (`MulchDecayMultiplier=0`), so no unprotected spring decay in 6 ticks.
- **013 SC-005 7-day expiry (defensible, runner machine-pass):** `mulch-7day-expiry` (v4, tile 61,15, mulch, **7 spring day-start ticks**, 2-signal not-applicable). **`run-20261107T205338Z-08f8` verdict=`pass` actual=`pass` signalsComplete/Agree thresholdsOk=true evaluatorMissing=false honesty R anchor present** (runner-promoted GATE3). `artifacts/soil-health-before-after.json`: 61,15 before 50, after 49, delta -1. Evaluation pre 50.0, actual 49.0 (0.98). Defensible: tick 7 does 1→0 and removes mulch before decay check, so one unprotected spring decay applies (`DailyDecayRate*seasonal*multiplier` = 2*0.5*1 = 1; 50→49).
- **Honest caveats:** 013 now has runner machine-pass evidence for prior cropped-tile apply/no-mutation+stable-capture (`141941Z`/control), refused-on-ineligible (`142415Z`), **and** defensible bare-tile SC-004/SC-005 via v4 2-signal (`205141Z`/`205338Z`). These are host-only paths with declared signals; `cropPhaseDelta` explicitly not-applicable+reasoned for the bare-tile proofs (not fabricated). `machine-pass` ≠ human-verified/D1 render/balance/SC-004 visual in all interpretations. Farmhand split-screen (T028) remains **blocked/partial, never machine-pass** (T002). GATE3 block was runner-promoted for `205338Z` (not hand-edited).
- **Pass 12 (2026-11-08) — T006/T007 closed + real revalidation:** 33 new unit cases (13 `MulchApplicationService`, 14 pure `MulchVerificationRules`, 6 `ILivingRootsModApi.ApplyMulch`) with demonstrated RED (`CS0103` before the rule existed) and targeted 37/37 GREEN; full suite 1937 passed / 4 skipped / 0 failed; `dotnet format --verify-no-changes` exit 0. Real Gate 3 reruns on the `609cef9` build (tree now `10a9202` after a message-only amend; identical content): `mulch-7day-expiry` **pass** `run-20261108T150706Z-5735` (soil 50→49, 0.98×) and `mulch-refused-on-ineligible` **pass** `run-20261108T151007Z-0abd` (no mutation, 1.0×). Both `--verify-bundle` exit 0. The runner promoted both, so the GATE3 block above points to the last run (`0abd`, refusal); the expiry run remains evidence via its bundle and this note. No hand-edit of the block. `GrantMulch`'s item-creation success path is attested by these Gate 3 runs (the expiry scenario requires a successful `lr_give_mulch`), not by a unit test; farmhand/D1 remains `blocked/partial`.

## Append-only run notes (pass 15, 2026-11-08)

- `mulch-bare-tile-decay-exemption` (host, SC-004) rerun twice on seed 123456789: `run-20261108T211743Z-62f5` / `run-20261108T211930Z-69c9` — both **pass**; bundles verify exit 0; semantic evidence identical (`soilHealth` 50, tier moderate, ×1.0). Logs: 93/93 filtered mod-relevant lines identical after timestamp/ms stripping.
- Mutation negative control: `MulchDecayMultiplier 0f → 1f` → `run-20261108T212701Z-0ce2`, soil 50→44, predicate-0 (×1.0) **false**; exit 19; source restored (`git checkout`), post-restore `run-20261108T213615Z-51b4` **pass**. See `verification/mutation-checks.md`.
- GATE3 block is runner-promoted to the post-restore run `run-20261108T213615Z-51b4` (commit `b945f81`); not hand-edited.
