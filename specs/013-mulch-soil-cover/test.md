# Feature Test Record: 013-mulch-soil-cover

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261107T142415Z-4e29
- **Commit**: 1da30a8b3ba0b883c33dbba6f2f6cd70818e10de
- **Date**: 2026-11-07T14:24:47Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261107T142415Z-4e29/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261107T142415Z-4e29/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261107T142415Z-4e29/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261107T142415Z-4e29/`
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
