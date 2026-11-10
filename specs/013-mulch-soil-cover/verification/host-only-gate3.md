# Host-Only Gate 3 — Run Record — Spec 013 (Mulch Soil Cover)

**Claim class:** `host-only-machine-pass`
**Status:** `complete` for the listed host mulch paths; farmhand/D1, visual SC-004, and human layers remain `blocked`/`pending`
**Derived from:** `specs/013-mulch-soil-cover/test.md` GATE3 block + append-only notes and `specs/013-mulch-soil-cover/verification/claim-records.md` (018 pass 7)
**Record created:** 2026-11-08 (018 pass 10, conservative; no hand-edit of `test.md` GATE3 and no claim promotion)

> This record is **not** human verification, **not** D1, and **not** SC-004 visual confirmation. It restates
> runner-promoted machine evidence for the listed paths, including the v4 2-signal bare-tile policy.

## Runs attested

| Path | Run ID | Result | Notes |
|---|---|---|---|
| 7-day expiry (SC-005) | `run-20261107T205338Z-08f8` | `machine-pass` (runner-promoted GATE3) | Soil `50 → 49` (0.98×) at the expiry tick; `cropPhaseDelta` not-applicable + reasoned (v4 2-signal bare-tile policy). |
| Bare-tile decay exemption (SC-004 implementation path) | `run-20261107T205141Z-0508` | `pass` | 6 spring ticks; soil `50 → 50` (1.0×); v4 2-signal not-applicable + reasoned. `205010Z-1fed` also cited pass. |
| Apply on cropped tilled tile — no mutation + stable capture | `run-20261107T141941Z-18e6` | `pass` | 3-signal; soil 1.0×; LongPhaseCrop `phaseDays` stable (1-day). Control `run-20261107T142212Z-10d5` pass. Does not isolate bare-tile decay delta (honest caveat in `test.md`). |
| Refused on ineligible (already mulched, 2nd apply) | `run-20261107T142415Z-4e29` | `pass` | Refusal attestable; no mutation. |

All cited bundles were present locally and passed `./scripts/verify-scenario.sh --verify-bundle` (exit 0) in the pass 10 audit (2026-11-08).

## Honesty boundaries

- The v4 bare-tile runs declare only `soilHealthStateDelta` + `smapiLog`; `cropPhaseDelta` is explicitly `not-applicable` with a reason (bare HoeDirt has no crop). This is not fabricated: the validator/schema allows it only for that policy.
- `machine-pass` ≠ human-verified; render legibility, balance, and SC-004 visual comprehension remain manual.
- Farmhand guard (T028) and D1 transport: `blocked` (T002), never `machine-pass`.
