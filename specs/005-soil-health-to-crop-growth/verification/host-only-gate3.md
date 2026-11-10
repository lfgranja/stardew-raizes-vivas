# Host-Only Gate 3 — Run Record — Spec 005 (Soil Health to Crop Growth)

**Claim class:** `host-only-machine-pass`
**Status:** `complete` for the three host soil-health growth paths; farmhand/D1 and human layers remain `blocked`/`pending`
**Derived from:** `specs/005-soil-health-to-crop-growth/test.md` GATE3 block + claim line and `specs/005-soil-health-to-crop-growth/verification/claim-records.md` (018 pass 7)
**Record created:** 2026-11-08 (018 pass 10, conservative; no hand-edit of `test.md` GATE3 and no claim promotion)

> This record is **not** human verification, **not** D1, and **not** SC-004. It restates runner-promoted
> machine evidence that a real headless host session produced the declared signals for the listed paths.

## Runs attested

| Path | Run ID | Result | Source |
|---|---|---|---|
| Healthy soil 80 / 0.85× growth | `run-20261107T112602Z-5b08` | `machine-pass` (runner-promoted GATE3, honesty R) | `test.md` GATE3 block |
| Poor soil 10 / 1.15× growth | `run-20261107T114448Z-4b61` | `pass` | `test.md` claim line |
| Moderate soil 50 / 1.0× parity | `run-20261107T114623Z-3365` | `pass` | `test.md` claim line |

All cited bundles were present locally and passed `./scripts/verify-scenario.sh --verify-bundle` (exit 0) in the pass 10 audit (2026-11-08), consistent with pass 5/7 records.

## Scope

- Scenarios run on-map at `60,15` with `LivingRoots.Verification.LongPhaseCrop` and a 5-day sleep.
- Host/main-player only.
- Bundles are gitignored local artifacts; referenced in `test.md`/claim records, not committed.

## Honesty boundaries

- `machine-pass` ≠ `human-verified`; render legibility and balance remain manual.
- Farmhand guard / D1 transport: `blocked` (T002), never `machine-pass`.
- SC-004 comprehension: not satisfied by these host runs.
