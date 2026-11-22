# Matriz de verificação funcional — Living Roots

- **Date**: 2026-11-21
- **Anchor**: `.specify/feature.json` → `specs/030-roteiro-verificacao-funcional`
- **Environment**: `cd /home/luis/development/stardew-raizes-vivas`; `GamePath=/home/luis/stardew-assemblies`; jogo em `$HOME/.local/share/Steam/steamapps/common/Stardew Valley`
- **Honesty rule**: todo resultado abaixo é `machine-pass` / `host-only`. Nenhum item é `verified` humano.

## Camada A — gates determinísticos

| Gate | Comando | Resultado | Registro |
|---|---|---|---|
| SDD/agregado | `bash scripts/ci/aggregate-gate.sh` | **ALL STEPS PASSED** (sdd-gate 0 errors / 29 warnings; policy-drift ALIGNED; protected paths OK; docs markers OK) | 2026-11-21T23:11:14Z |
| Release (7 etapas) | `GamePath=/home/luis/stardew-assemblies bash scripts/release-gate.sh` | **ALL STEPS PASSED** no HEAD `f4eba87` (steps 1–7 OK; suíte 1999/0/4) | 2026-11-22T00:59:31Z |
| i18n | `python3 scripts/ci/i18n-parity-check.py` | **PASS** (key sets identical across default/en/pt; 0 errors, 0 warnings) | 2026-11-21T23:30:12Z |
| Quality-gate pytest | `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q` | **180 passed** (0 falhas) | 2026-11-21T23:30:12Z |

Gates factory-nomeados (`check_production_gate.py`, `hub_docs_gate.py`, 4 scripts SDD) = **N/A — ausentes no alvo, conforme Bloco 0.a**.

## Camada B — xUnit por feature

Comando-genérico: `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln -c Release --no-build --verbosity minimal --filter "<filtro>"`.
Execução: 2026-11-21T23:14:56Z → 23:22:36Z. Log completo: `/tmp/lr-matrix-b-20261121T231456Z.log`.

| # | Feature | Filtro (classes) | Observado | Resultado |
|---|---|---|---|---|
| B1 | Solo 0–100, NaN, faixas 31/71, persistência | `SoilHealthServiceTests`, `SoilHealthServiceDosProtectionTests`, `SoilHealthServiceContractTests`, `SoilHealthBoundsInvariantsTests`, `SoilHealthCategoryInvariantsTests`, `SoilHealthPersistenceInvariantsTests`, `HealthCategoryTests`, `SaveReloadConsistencyTests` | 147 passed / 0 failed / 0 skipped | PASS |
| B2 | Overlay / tooltip / feedback da enxada | `SoilHealthVisualizationTests`, `OverlayRendererTests`, `TooltipRendererTests`, `HoeFeedbackRendererTests`, `VisualizationService*Tests`, `VisualizationIntegrationTests`, `Visualization*InvariantsTests`, `NoHardcodedVisualizationLiteralsTests` | 145 / 0 / 0 | PASS |
| B3 | Composto + bin Empty→Processing→Ready | `CompostApplicationServiceTests`, `CompostingBin*Tests`, `BinPlacementTests`, `BinPickupTests`, `OrganicWasteValidatorTests`, `BinCrafting/CompostingLoop/CompostConservation/CompostingMachineInvariantsTests` | 162 / 0 / 0 | PASS |
| B4 | Mulch + decaimento sazonal | `MulchApplicationServiceTests`, `MulchVerificationRulesTests`, `SoilDecayServiceTests`, `SeasonalDecayMultiplierTests`, `DailyDecayTests` | 53 / 0 / 0 | PASS |
| B5 | Agroquímico Joja −10 | `Agrochemical*Tests`, `AgrochemicalRegistrationTests`, `ModControllerAgrochemicalTests`, `AgrochemicalContentParityTests` | 123 / 0 / 0 | PASS |
| B6 | Sementes crioulas/heirloom | `HeirloomSeedSaveTests`, `HeirloomSoilEffectsTests`, `HeirloomHarvestQualityParityTests`, `HeirloomI18nTests`, `HeirloomRegistrationTests` | 50 / 0 / 0 | PASS |
| B7 | Qualidade de colheita via Harmony | `HarvestPatchesTests`, `HarvestQualityServiceTests`, `HarvestQualityMathTests`, `Poor/Moderate/Healthy/UnknownTierHarvestQualityTests`, `ExtraHarvestTests`, `InventoryFullHarvestTests`, `HarvestQualityContractTests`, `CropScopeHarvestTests`, `HostAuthoritativeRollTests` | 101 / 0 / 0 | PASS |
| B8 | Crescimento por saúde do solo | `FullyQualifiedName~GrowthModifier` (todas as classes) | 23 / 0 / 0 | PASS |
| B9 | Multiplayer / locks host-authoritative | `MultiplayerConcurrencyTests`, `HostAtomicLockServiceTests`, `Multiplayer/SingleMultiplayerParityInvariantsTests`, `ClientResyncConvergenceTests`, `SoilHealthSyncTests`, `FeedbackVisibilityMultiplayerTests`, `ModControllerTests`, race/disposal tests | 93 / 0 / 0 | PASS |
| B10 | Arquitetura / segurança / governança / i18n | `DomainLayeringTests`, `ExceptionSanitizationTests`, `ConstitutionGovernanceTests`, `ProductionSourceGuardTests`, `Path/Sanitize/FileName/Unicode/Homoglyph/DotSegment/SecuritySanitization` tests, `HudNotifierTranslationTests` | 303 / 0 / 0 | PASS |
| B11 | Ferramental de verificação headless | `FullyQualifiedName~LivingRoots.Tests.Verification` | 178 / 0 / 0 | PASS |

Total de execuções nos 11 grupos: **1378** (grupos se sobrepõem; não é a contagem da suíte completa). Suíte completa: ver step 4 do `release-gate.sh`.

## Camada C — Gate 3 (SMAPI real sob Xvfb)

Comando: `./scripts/verify-scenario.sh --scenario <path> --no-acquire --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build`, seguido de `--verify-bundle <dir>`.
Critério: `verdict=pass`, honestidade `R`, 3 sinais (`soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog`) presentes e concordantes; `partial` → exit 19, nunca promovido.

| Spec | Cenários | Evidência registrada | Reexecução neste ciclo |
|---|---|---|---|
| 004 | `compost-growth-{healthy,moderate,hardening}` | `specs/004-playable-compost-loop/test.md` (machine-pass `run-20261121T232308Z-64cf`) | `hardening` (obrigatório, deriva corrigida) |
| 005 | `soil-health-growth{,-poor,-moderate}` | `specs/005-soil-health-to-crop-growth/test.md` (`4cc1`) | não necessária (nenhum artefato alterado) |
| 013 | `mulch-{decay-exemption,bare-tile-decay-exemption,7day-expiry,refused-on-ineligible}` | `specs/013-mulch-soil-cover/test.md` (`1ea4`) | não necessária |
| 014 | `soil-health-harvest-{quality,measured-zero,unknown}` | `specs/014-soil-health-crop-quality/test.md` (`088b`) | não necessária |
| 015 | `harmony-harvest-quality-host` | `specs/015-harmony-harvest-seam/test.md` (`29cf`) | não necessária |
| 017 | `agrochemical-{tradeoff,clamped,unknown-baseline,refused-at-minimum,refused-contention}` | `specs/017-joja-agrochemical-tradeoff/test.md` (`6938`) | não necessária |
| 020 | `heirloom-plant-bonus` | `specs/020-heirloom-seeds-pipeline/test.md` (`6021`) | não necessária |

Sem cenário Gate 3 (lacuna de evidência, não falha): specs 001–003, decaimento sazonal isolado, visualização de overlay/tooltip/hoe, saved-seed de heirloom, sincronização/farmhand.

Resultado da reexecução do cenário `hardening`: ver `specs/004-playable-compost-loop/test.md` (nota append-only com `runId`).

## Camada D — exclusivamente humana `[NV]`

- D1: sessão host + farmhand real com segundo jogador — não executada.
- Atestação humana E de Gate 3 (`verified`) — não emitida; `machine-pass` não é `verified`.
- SC-004: protocolo com participantes 18+, consentimento e first-use real — não executado.
- Tag/release pública `0.2.0`, canais, `RUNNER_AUDIT_STORE` (#523), segredos do Dependabot — owner-gated.
- Aprovação de revisor distinto: não simulada; autorização explícita do prompt cobre o ato de merge, não um `approve` forjado.
