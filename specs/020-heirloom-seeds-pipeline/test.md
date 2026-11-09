# Spec 020 — Test & Gate 3 Evidence

- **Feature**: 020-heirloom-seeds-pipeline (Sementes Crioulas — Crioulo Parsnip)
- **Runner**: spec 010 headless runner (`scripts/verify-scenario.sh`)
- **Honesty rule**: `machine-pass` ≠ `verified`. D1 (farmhand), `human-verified` and any playtest acknowledgement remain `pending` — none of them is claimed anywhere in this file.

## GATE3

<!-- GATE3:BEGIN -->
- **Status**: pending (first run not yet executed at time of writing)
- **Verdict**: —
- **Flaky**: —
- **Run ID**: —
- **Commit**: —
- **Date**: —
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-<id>/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-<id>/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-<id>/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-<id>/`
- **Honesty level**: —
- **Claim**: —
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->

## Known boundaries (do not reinterpret)

- Headless run **cannot** observe a natural cart acquisition (`Seller: Traveler` shows `SEASON Spring` stock only by chance) — acquisition correctness rests on the registration contract tests + `lr_give_heirloom_seed` verification grant.
- Headless run **cannot** attest multiplayer convergence (D1) — farmhand paths are covered by unit tests of the request/apply seam only; live convergence remains `pending`.
- Headless run **cannot** attest comprehension (SC-004-class protocol) — not Run this cycle.
- The `1.04` factor asserts only the flat `+HeirloomPlantBonus` write (2 points on a measured 50 baseline). It does **not** certify balance, UX clarity, or agronomic fit.
