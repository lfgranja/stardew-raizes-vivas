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
