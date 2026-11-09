# Spec 020 — Test & Gate 3 Evidence

- **Feature**: 020-heirloom-seeds-pipeline (Sementes Crioulas — Crioulo Parsnip)
- **Runner**: spec 010 headless runner (`scripts/verify-scenario.sh`)
- **Honesty rule**: `machine-pass` ≠ `verified`. D1 (farmhand), `human-verified` and any playtest acknowledgement remain `pending` — none of them is claimed anywhere in this file.

## GATE3

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261109T155608Z-6021
- **Commit**: 3bd84952b4a86e9a1ccd1330af934620b1c932e0
- **Date**: 2026-11-09T15:56:51Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261109T155608Z-6021/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261109T155608Z-6021/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261109T155608Z-6021/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261109T155608Z-6021/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->

## Known boundaries (do not reinterpret)

- Headless run **cannot** observe a natural cart acquisition (`Seller: Traveler` shows `SEASON Spring` stock only by chance) — acquisition correctness rests on the registration contract tests + `lr_give_heirloom_seed` verification grant.
- Headless run **cannot** attest multiplayer convergence (D1) — farmhand paths are covered by unit tests of the request/apply seam only; live convergence remains `pending`.
- Headless run **cannot** attest comprehension (SC-004-class protocol) — not Run this cycle.
- The `1.04` factor asserts only the flat `+HeirloomPlantBonus` write (2 points on a measured 50 baseline). It does **not** certify balance, UX clarity, or agronomic fit.
