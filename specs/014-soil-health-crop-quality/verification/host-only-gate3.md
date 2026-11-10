# Host-Only Gate 3 — Run Record — Spec 014 (Soil Health Crop Quality)

**Claim class:** `host-only-machine-pass`
**Status:** `complete` for the listed host harvest-quality paths; farmhand/D1 and human layers remain `blocked`/`pending`
**Derived from:** `specs/014-soil-health-crop-quality/test.md` GATE3 block + claim line and `specs/014-soil-health-crop-quality/verification/claim-records.md` (018 pass 7)
**Record created:** 2026-11-08 (018 pass 10, conservative; no hand-edit of `test.md` GATE3 and no claim promotion)

> This record is **not** human verification, **not** D1, and **not** SC-004. It restates runner-promoted
> machine evidence that a real headless host session produced the declared signals for the listed paths.

## Runs attested

| Path | Run ID | Result | Source |
|---|---|---|---|
| Harvest matrix poor/moderate/healthy probes | `run-20261106T014750Z-14b6` | `machine-pass` (runner-promoted GATE3, honesty R) | `test.md` GATE3 block |
| Regression post-seam still pass | `run-20261107T122915Z-2564` | `pass` | `test.md` claim line |
| NaN/unknown baseline → vanilla/no forced quality (A7) | `run-20261107T123141Z-3531` | `pass` (rerun clean; first attempt `122437Z-2f19` partial/no-dump disclosed) | `test.md` claim line |
| Measured-zero → Poor/force quality 0 (A7) | `run-20261107T123257Z-08c9` | `pass` | `test.md` claim line |

All cited bundles were present locally and passed `./scripts/verify-scenario.sh --verify-bundle` (exit 0) in the pass 10 audit (2026-11-08), consistent with pass 5/7 records.

## Honesty boundaries

- `machine-pass` ≠ `human-verified`; render legibility and balance remain manual.
- Farmhand guard / D1 transport: `blocked` (T002), never `machine-pass`.
- SC-004 comprehension: not satisfied by these host runs.
