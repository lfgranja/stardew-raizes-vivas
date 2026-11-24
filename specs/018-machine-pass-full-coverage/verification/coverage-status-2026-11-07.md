# 018 Coverage Status — machine-pass (applicable paths, honest)

**Date:** 2026-11-07 (autonomous 80/20, MDMV dark-factory flow)  
**Rule:** `machine-pass` = runner verdict `pass`, required declared signals complete+agree, thresholds ok, honesty R, anchor present (as applicable). **Never** equals human-verified, D1 render/balance/visual, or farmhand/split-screen `machine-pass`. No fabrication; no GATE3 hand-edits. Bundles gitignored; run IDs/bundle names are evidence refs.

## Harness state (this cycle)
- v4 implemented (approved): `scenario-v4` + manifest allow 2 declared signals (`soilHealthStateDelta`,`smapiLog`) **only** with explicit `signalPolicy.cropPhaseDelta=not-applicable` + required reason (defensible bare-tile/no-crop proofs). v2/v3 preserved. Commit `cbe0b72` (+ fixes in same change). Targeted 010 tests green (schema-contract 52/52, signals 33/33, verdict 45/45, evaluation 10, evidence 22, manifest-schema 7).

## Feature/path status (best-known, conservative)

| Feature | Applicable host path(s) | Status | Evidence (representative) | Blocked/partial (honest) |
|---|---|---|---|---|
| 004 playable-compost-loop | Host compost/moderate/healthy paths (as authored/run historically) | **machine-pass** evidence exists per prior 018/feature records; confirm per `specs/004-*/test.md` | see 004 test.md / prior bundles | farmhand guard blocked-feasible (T002) |
| 005 crop-growth-modifier | Host growth-modifier path | **machine-pass** per prior records; confirm per `specs/005-*/test.md` | see 005 test.md | D1/farmhand visual/transport constraints as recorded |
| 013 mulch-soil-cover | Cropped-tile apply+no-mutation/stable-capture; refused-on-ineligible; **v4 bare-tile SC-004 (6 spring ticks, 1.0x)**; **v4 SC-005 7-day expiry (50→49, 0.98)** | **host machine-pass** (all above) | `141941Z-18e6`(+control), `142415Z-4e29`, `205141Z-0508`/`205010Z` (SC-004 v4), `205338Z-08f8` (SC-005 v4, runner-promoted GATE3) | T028 farmhand/split-screen **blocked/partial, never machine-pass** (T002). cropPhaseDelta N/A reasoned for bare-tile proofs. |
| 014 soil-health-crop-quality | Harvest matrix (poor/moderate/healthy) + NaN/unknown + measured-zero (014 A7) host | **host machine-pass** | `014750Z-14b6` (matrix), `122915Z-2564`, `123141Z-3531` (NaN/unknown), `123257Z-08c9` (measured-zero) — per 014 test.md | D1 multiplayer aspects where specified; visual/balance human out of scope |
| 015 harmony-harvest-seam | Host 3-signal Harmony `Crop.harvest` seam (harvest matrix probes) | **host machine-pass** (3-signal) | `210830Z-67f1` (015 scenario, runner-promoted GATE3) + 014 runtime refs; HarvestQuality 17/17; Harmony registered in smapi logs | **SC-004 D1 multiplayer pending/blocked-feasible (T002)** → full SC-004 incomplete; do not claim D1/verified |
| 017 joja-agrochemical-tradeoff | Measured/unknown/clamped/refused-at-minimum + contention/no-mutation (authorized seam) | **host machine-pass** (recorded paths) | `135531Z-7c4e`, contention `193756Z-6d97` per 017 test.md | farmhand split-screen blocked-feasible (T002) |
| 018 machine-pass-full-coverage | Coverage coordination | **partial/in-progress** | this artifact + 018 plan/research/tasks/verification | R1/assess/decision/spec/waiver gaps if any; not all features re-audited in this cycle |
| Tooltip bug `soil-health-unknown-tooltip-flashing` | Render-path/UI flashing/scope | **partial** | TooltipRenderer/Visualization targeted **11/11**; render-signal constraint (no runner frame capture) | Gate 3 manual/in-game visual required; not runner machine-pass |

## “100% applicable machine-pass” (honest)
- **Not achieved globally.** Improved materially: 013 SC-004/SC-005 defensible bare-tile proofs now runner machine-pass (v4); 015 now has its own host 3-signal runner machine-pass (D1 still pending).
- **Still blocking full claim:** 015 SC-004 D1 multiplayer; T002 farmhand/split-screen (blocked-feasible, never machine-pass); tooltip visual Gate 3; D1/render/balance/human criteria; any 018 R1 artifacts/policy gaps; any host-applicable paths not yet authored/run (needs per-feature audit).
- **No authorization needed/used for new trust-root changes beyond approved v4?** v4 was explicitly approved earlier; this cycle made no new trust-root changes. No new production gameplay behavior changes. No fabrication.
