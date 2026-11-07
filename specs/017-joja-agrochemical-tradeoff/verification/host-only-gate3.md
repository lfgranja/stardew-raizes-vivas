# Host-Only Gate 3 Record — Spec 017 (Joja / Agrochemical MVP)

**Claim class:** `planned-run-not-executed`  
**Status:** `infeasible` — no 017 host-only scenario can be expressed by the current runner  
**Date:** 2026-11-06  
**Recorder:** implementation agent (record only; no run was executed)

**This record is not a pass.** It is not `host-only-machine-pass`, not `human-verified`, and not
`d1-farmhand-convergence`. No log, bundle, or scenario was fabricated.

## Decision

No host-only Gate 3 run was executed for 017. The local game/runner prerequisites exist, but the
current verification runner cannot express the 017 interaction or provision the item. Adding that
support would require runner/schema/harness work outside this MVP, so the planned run is recorded
as `infeasible` rather than silently skipped or claimed as passing.

## Environment facts checked

| Check | Result |
|---|---|
| Stardew Valley game directory at `/home/luis/.local/share/Steam/steamapps/common/Stardew Valley` | present |
| `xvfb-run` | present at `/usr/bin/xvfb-run` |
| `scripts/verify-scenario.sh` | present |
| 017 scenario file under `specs/017-joja-agrochemical-tradeoff/` | **absent** |

The blocker is therefore the missing 017 scenario capability, not a missing local game install or
display wrapper.

## Exact missing capabilities

1. **No scenario step action.** The closed v3 action enum is `sleep`, `warp`, `harvest`,
   `applyCompost`, `insertWaste`, `setSoilHealth`, `observe`, `dumpCropPhase`, `runMacro`
   (`specs/010-headless-verification-runner/contracts/scenario-v3.schema.json`). There is no
   `applyAgrochemical` (or equivalent) action.
2. **No item-grant path.** Scenario init console commands invoke registered mod commands; `debug `
   commands are rejected by the scenario contract. The verification queue parser accepts only the
   existing `lr_*` commands (`lr_set_soil_health`, `lr_plant_crop`, `lr_place_bin`,
   `lr_insert_waste`, `lr_harvest_matrix`, `lr_advance_days`, `lr_dump_crop_phase`). There is no
   `lr_give_agrochemical` command; the intended manual grant is documented but not implemented in
   `verification/provisioning-validation.md`.
3. **No right-click interaction simulation.** The 017 path is triggered by a right-click with a
   held agrochemical; the runner has no action that places the item in the player's hands and
   invokes the controller path.
4. **No 017 scenario/threshold definition.** No scenario file, signal set, or expected verdict
   exists for this feature. The existing scenarios cover compost/growth, not agrochemical
   application.

## Command that would be needed (not executed)

```bash
./scripts/verify-scenario.sh \
  --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json \
  --no-acquire \
  --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" \
  --skip-build
```

Not run: the scenario file does not exist and the current schema cannot express the required
steps. This is **not** an attempted run and produced no evidence.

## What a future run would require

- A schema-compatible scenario action (or a future schema version) that invokes the agrochemical
  controller path with a held item, or a documented macro/console route that does the same.
- A validation-only item grant, for example the scoped `lr_give_agrochemical` command described in
  `verification/provisioning-validation.md`.
- A scenario with explicit thresholds/signals: bounded soil-health delta (C2/C3), item consumption
  only on success, refusal behavior, and SMAPI-log evidence. Existing runner rules for signals and
  evidence bundles apply.
- A real host-only run and a new evidence-bundle record. That record would be classified
  `host-only-machine-pass` only; it still would not establish D1 or human verification.

## Evidence boundary for 017

- Automated service/controller/content-parity evidence: see `verification/automated-tests.md`
  once T048 records it.
- Mocked persistence-seam evidence: see `verification/persistence-roundtrip.md` once T049 records
  it (not real save/load, not Gate 3).
- This record: `planned-run-not-executed` / `infeasible`, not a pass.
- `human-verified`: pending; no human attestation exists.
- `d1-farmhand-convergence`: pending; no real farmhand evidence exists.
- SC-004: pending; the protocol exists but no run was executed.

## Provenance

- No prior verification artifact was rewritten or retroactively promoted.
- No Gate 3 log, bundle, SHA-256 anchor, or machine-pass verdict was created.
- If a future run is performed, it must produce a new record and must not overwrite this one.
