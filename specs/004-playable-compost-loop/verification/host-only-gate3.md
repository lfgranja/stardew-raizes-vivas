# Host-Only Gate 3 — Run Record — Spec 004 (Playable Compost Loop)

**Claim class:** `host-only-machine-pass`
**Status:** `complete` for the three host compost-growth tier paths; farmhand/D1 and human layers remain `blocked`/`pending`
**Derived from:** `specs/004-playable-compost-loop/test.md` GATE3 block + claim line and `specs/004-playable-compost-loop/verification/claim-records.md` (018 pass 7)
**Record created:** 2026-11-08 (018 pass 10, conservative; no hand-edit of `test.md` GATE3 and no claim promotion)

> This record is **not** human verification, **not** D1, and **not** SC-004. It restates runner-promoted
> machine evidence that a real headless host session produced the declared signals for the listed paths.

## Runs attested

| Path | Run ID | Result | Source |
|---|---|---|---|
| Poor soil 10 / 1.15× growth (hardening primary) | `run-20261105T053313Z-535d` | `machine-pass` (runner-promoted GATE3, honesty R) | `test.md` GATE3 block |
| Moderate soil 50 / 1.0× parity | `run-20261107T120435Z-1ea9` | `pass` | `test.md` claim line |
| Healthy soil 80 / 0.85× | `run-20261107T120602Z-69ea` | `pass` | `test.md` claim line |

All cited bundles were present locally and passed `./scripts/verify-scenario.sh --verify-bundle` (exit 0) in the pass 10 audit (2026-11-08), consistent with pass 3/5 records.

## Scope

- Scenarios mirror the hardening structure: bin placement, waste insertion, 5-day sleep, `60,15` LongPhaseCrop.
- Host/main-player only.
- Bundles are gitignored local artifacts; referenced in `test.md`/claim records, not committed.

## Honesty boundaries

- `machine-pass` ≠ `human-verified`; render legibility and balance remain manual.
- Farmhand guard / D1 transport: `blocked` (T002 split-screen `isMainPlayer` flip not demonstrated), never `machine-pass`.
- SC-004 comprehension: not satisfied by these host runs.
