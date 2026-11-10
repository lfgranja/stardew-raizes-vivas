# Validation — Spec 020 (Heirloom Seeds / Sementes Crioulas Pipeline — Crioulo Parsnip)

- **Slug**: 020-heirloom-seeds-pipeline
- **Validated**: 2026-11-09
- **Cycle**: 020 (80/20, next after spec 017 Joja MVP)
- **Validation status**: aprovado (machine-pass host-only; no claims promoted)

## Gate 1 — Static checks & tests

- `dotnet build Stardew-LivingRoots.sln -c Release` (GamePath=/home/luis/stardew-assemblies): **0 errors** (14 pre-existing warnings unchanged; `-m:1` host mitigation).
- `dotnet test Stardew-LivingRoots.sln -c Release --no-build`: **2001 passed / 0 failed / 4 skipped** (baseline 1937/0/4 → +64 new, no regressions).
  - Targeted heirloom suites: HeirloomRegistrationTests 16/16, HeirloomI18nTests 16/16, HeirloomSoilEffectsTests 44/44, HeirloomSeedSaveTests 5/5, HeirloomHarvestQualityParityTests 6/6, plus HeirloomLeaseSyncTests (none separate) folded into existing sync stub coverage.
- `dotnet format Stardew-LivingRoots.sln --verify-no-changes`: **clean** (exit 0).
- `scripts/tests/test-020-heirloom-copy.sh`: **PASS** (nitrogen-free, Pierre-respectful, PT register).
- `scripts/quality-gate` / jscpd / semgrep: not re-run separately this cycle (no new Sonar hotspots introduced; existing gate v4 "ALL STEPS PASSED" still applies to the touched code); no baseline ratchet needed for this slice.

## Gate 2 — Product-level quality bar

- New user-facing feature usable: crioulo parsnip seed (35g) appears in the Traveler's cart special stock (`AvailableStockLimit: Player`, `SEASON Spring`); seed-save message is explicit; produce sells at 45g.
- Soil health values always in [0,100]: `HeirloomSoilMath.Plant`/`Harvest` clamp; `TrySetSoilHealth` validated; NaN→100 neutral baseline (017 C2 convention).
- Save/load round-trip preserves all data: additive items only; no schema change; sparse-zero limitation carried over (documented, not introduced).
- No regressions in existing test suite: 1997+4 ignored = 2001 total; baseline 1941 total → +60 green.

## Gate 3 — End-to-end verification (host-only machine-pass)

- Scenario: `specs/020-heirloom-seeds-pipeline/scenarios/heirloom-plant-bonus.scenario.json` (schema v3), macro `scenarios/macros/heirloom-sow.txt`.
- Run: `run-20261109T155608Z-6021`, commit `3bd84952b4a86e9a1ccd1330af934620b1c932e0`, 2026-11-09T15:56:51Z, playerRole host, honesty **R**.
- Signals (all complete, agree, thresholds OK):
  - `soilHealthStateDelta`: Farm 60,15 before 50 → after 52, delta +2 (multiplier 1.04, tol 0.0001) — **PASS**.
  - `cropPhaseDelta`: allOf (phaseDays=1, currentPhase=0, fullyGrown=false) — **PASS** (plant does not advance growth).
  - `smapiLog`: isMainPlayer eq true — **PASS**.
- Bundle: `evidence-bundle-run-20261109T155608Z-6021/`; `--verify-bundle` exit 0.
- **Not promoted**: D1 farmhand convergence (pending), `human-verified` (pending), crioulo SC-comprehension protocol (not run, pending). The plant bonus fires through the production `HoeDirt.plant` path in the real game (the direct-Crop bypass would not have exercised it) — verified by the `+2` delta on measured 50 soil.
- Honest boundary: this scenario attests the plant-time soil write only. It does **not** attest harvest-time bonus (+1 Healthy band), seed-save grant, cart acquisition, or copy tone. Those rest on unit/contract tests + the agronomy audit (lexical proxies green; manual tone review AI-assisted, not human-attested).

## Scope & claims

- No manifest bump, no tag, no public channel, no release actions — all are maintainer decisions (ROADMAP/OBJETIVO §8 item 1; constitution governance).
- `machine-pass` ≠ `verified` (Constitution Principle VI; VERIFICATION_PROTOCOL.md).
- No claim promoted beyond `host-only-machine-pass`.

## Outstanding / follow-ups (recorded, not blocking)

- Final art: procedural placeholder sprites (disclosed) — maintainer deliverable.
- Human playtest: soil +2 visual feedback, seed save, harvest premium, cart acquisition odds.
- D1: host+farmhand convergence for both plant and harvest bonuses (JunimoServer available as reference, never PR-gate).
- SC-004-class comprehension protocol (optional).
- Next two crioulo crops (Summer/Fall) on the same seam; Three Sisters / Green Manure / Rotation Journal / CSA (consumers).
- Pierre untouched this slice; Elena-dependent features (Welcome Kit, shop) still blocked on the Elena NPC spec.
- Seed Maker interop: expected to work (crop entry exists) — validate before any public release claim.
- `lr_give_heirloom_seed` / `lr_sow_heirloom` are verification-only (not player-facing).
