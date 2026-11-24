# Claim Records — Spec 020 (Heirloom Seeds / Sementes Crioulas Pipeline — Crioulo Parsnip)

- **Slug**: 020-heirloom-seeds-pipeline
- **Status note**: machine-pass ≠ verified. No claim is promoted beyond `host-only-machine-pass` by this file.

## Claim audit table

| Claim | Evidence class | Source artifact | Status | Scope note |
|---|---|---|---|---|
| Crioulo parsnip seed/produce/crop registered via `Data/Objects` + `Data/Crops` + `Data/Shops` Traveler entry | automated | `LivingRoots.Tests/Controllers/HeirloomRegistrationTests.cs` (16/16 green) | complete | registration contract pinned by test; unit-level |
| Crioulo copy carries no nitrogen-fixation vocabulary and no Pierre vilification | automated | `LivingRoots.Tests/HeirloomI18nTests.cs` (16/16 green) + `scripts/tests/test-020-heirloom-copy.sh` (PASS) | complete | lexical proxy only; manual review still required for tone |
| Crioulo plant bonus math + bounds + NaN→100 baseline | automated | `LivingRoots.Tests/HeirloomSoilEffectsTests.cs` (44/44 green) | complete | pure-domain unit tests; seams are thin glue |
| Crioulo harvest arm reuses the 014 seam without double-counting | automated | `LivingRoots.Tests/HeirloomHarvestQualityParityTests.cs` (6/6 green) | complete | composition-by-construction at the service seam |
| Deterministic seed save (exactly one, Junimo exemption) | automated | `LivingRoots.Tests/HeirloomSeedSaveTests.cs` (5/5 green) | complete | service-level; game-side grant not separately attested |
| Host-only Gate 3 plant-bonus path | `host-only-machine-pass` | bundle `evidence-bundle-run-20261109T155608Z-6021/`, verdict `pass`, honesty R, commit `3bd84952b4a86e9a1ccd1330af934620b1c932e0` | complete | signals: soilHealthStateDelta (50→52, ×1.04), cropPhaseDelta (allOf: phaseDays=1, currentPhase=0, fullyGrown=false), smapiLog (isMainPlayer true). **No** human-verified, **no** d1, **no** comprehension claim. |
| Natural cart acquisition (Traveler special stock) | qualitative-follow-up | registration contract tests + bundle scenario | pending | cart odds are non-deterministic; headless runner cannot attest obtainability. P2 playtest follow-up. |
| Farmhand plant-bonus convergence (D1) | `d1-farmhand-convergence` | unit seam tests only | pending | environment-blocked (needs host+farmhand session; JunimoServer available as reference but never PR-gate). |
| Human-verified gameplay (soil +15 → visual feedback, seed save, harvest premium) | `human-verified` | none | pending | no human playtest performed this cycle. |
| SC-004-class comprehension (crioulo choice understood) | `qualitative-follow-up` | not run | pending | optional protocol; not exercised; not claimed complete. |
| Heirloom copy tone/accuracy (agronomy audit) | `qualitative-follow-up` | `scripts/tests/test-020-heirloom-copy.sh` (PASS) + manual review recorded | partial | lexical proxies green; manual tone review recorded as AI-assisted (not human attestation). |
| Seed Maker interop (crioulo produce → seeds) | `planned-run-not-executed` | none | pending | optional check; never claimed. |
| Multiplayer farmhand request correlation idempotency | `planned-run-not-executed` | unit seam tests only | pending | not separately attested live. |

## Decision hygiene

- No claim was promoted beyond `host-only-machine-pass` by this cycle.
- `machine-pass` ≠ `verified` (Constitution Principle VI; project verification protocol).
- Bundle `evidence-bundle-run-20261109T155608Z-6021` is the sole Gate 3 evidence for this slice; earlier bundles (`153455Z-255c`, `142439Z-1fdc`) are diagnostic only and not cited for claims.
