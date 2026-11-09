# Phase 13 Completion Summary

> **Atualização 2026-11-07 (tarde — spec 017 Joja/Agrochemical MVP — Phase 11 FECHADA; Phase 12 fechada):** Phase 11 (T105–T118) está **fechada e marcada como [x]** (`c6c4e7a`; último sub-item T117(b) em `d69168f`); Phase 12 (T119–T124) fechada (`4764aaa`–`2fa1a19`, `1ff107b`), com T123 executada por esta anotação. Os números citados abaixo (targeted **123/123**, suíte completa **1875/0/4**) vêm dos logs commitados da re-run de Phase 11 `verification/convergence-phase11-targeted.log` / `-full.log` (T119/T124), **não** do `validate.md` (que atesta 119/119 e 1871/0/4 na árvore c9ff596, 2026-11-07). "Phase 11 FECHADA" refere-se ao fechamento de tarefas com artefatos (todos T105–T118 `[x]`) e à verificação final T124 commitada, não a um artefato de converge commitado; passes de converge pós-Phase-11 são registros de sessão, não artefatos. **Gate 3 permanece machine-pass host-only** — markers promovidos pelo runner intocados; bundle `evidence-bundle-run-20261107T040235Z-589a/`, honesty `R`, commit `c9ff596`; **sem re-run de Gate 3 sobre o build 0.1.0** (disclosure em `release.md` permanece válida). **Machine-pass ≠ verified**: D1 (farmhand real), `human-verified` e SC-004 permanecem `pending`. O bloco 2026-11-07 abaixo (em andamento) e o bloco 2026-11-06 (014-específico) são preservados; este bloco supersede apenas a cláusula "em andamento". Nenhuma claim é promovida por esta anotação; as duas rows `blocked` permanecem `blocked`.
> **Atualização 2026-11-07 (spec 017 Joja/Agrochemical MVP — release 0.1.0; Phase 11 converge em andamento):** 017 foi **released** no repositório em **0.1.0** (`d3ada42`; manifest `0.0.1`→`0.1.0`; escopo repo/`dev`; **sem canal público, sem tag**; attestation humana ausente por desenho). **validate.md: aprovado** (2026-11-07, commit `c9ff596`; suíte 1871 passed / 0 failed / 4 skipped; targeted 119/119; format exit 0; claims validator PASS) em `.specify/validations/017-joja-agrochemical-tradeoff/validate.md`. **Gate 3 do cenário 017: machine-pass host-only** — markers promovidos pelo runner, intocados; bundle `evidence-bundle-run-20261107T040235Z-589a/`, honesty `R`, commit `c9ff596`. **Machine-pass ≠ verified**: D1 (farmhand real), `human-verified` e SC-004 permanecem `pending`. **Phase 11 converge em andamento** (T105–T118). O build 0.1.0 **não** foi Gate-3-run isoladamente — o delta vs. a árvore atestada `c9ff596` é apenas a string `Version` do manifest (disclosure em `.specify/releases/017-joja-agrochemical-tradeoff/release.md`). O bloco 2026-11-06 abaixo permanece a attestation 014-específica; este bloco adiciona 017. Nenhuma claim é promovida por esta anotação.
>
> **Atualização 2026-11-06 (verificada):** build Release **0 erros** (`GamePath=/home/luis/stardew-assemblies`), testes **1747 passed / 0 failed / 4 skipped** (1751), format limpo nos projetos verificados, band-scale 31/71 passou. Gate 3 atual (marker abaixo): **machine-pass** host-only, bundle `evidence-bundle-run-20261106T014750Z-14b6` (honesty R, verifier exit 0) — **NÃO `verified` humano/E**. Spec 014 está **131/133** (`tasks.md`): T131–T133 completos; **T109 D1** (farmhand real) bloqueado; **T110** (contrato histórico 34/67) deferido. Estado git (snapshot): `dev` `50cc892` local, 99 commits à frente de `origin/dev` no momento desta nota; sem PRs abertos.
>
> **Atualização 2026-11-04 (histórica):** 1621/1/4 (1626); Gate 3 spec 004 machine-pass `evidence-bundle-run-20261104T082444Z-4acd/`. As tabelas abaixo preservam contexto histórico da onda T114–T145; T133 permanece limitação documentada de API.

## Objective
Complete remaining Phase 13 implementation tasks (T114-T145) for feature 004-playable-compost-loop using wave-based subagent delegation with TDD red-first approach.

## ✅ Completed: Implementation Tasks T127-T141

All implementation tasks T127-T141 have code changes verified in git history from prior waves:

| Task | ID | Description | Status |
|------|-----|-------------|--------|
| T127 | [US1] | Preserve batch modData on tool/explosion removal | ✅ Done |
| T128 | [US1/US2] | Fail closed on absent lock in TryInteractWithBin | ✅ Done |
| T129 | [US1] | **Refuse bin placement on building-footprint tiles** | ✅ **Just completed** - Added check of `location.buildings` with `tileX`, `tileY`, `tilesWide`, `tilesHigh` fields |
| T130 | [US1] | Enforce max stack 1 via Data/BigCraftables | ✅ Done |
| T131 | [US1] | Route dislodged bins into inventory when space available | ✅ Done |
| T132 | [US2] | Reconcile native Data/Machines output with interception | ✅ Done |
| T133 | [US2] | Derive Data/Machines RequiredTags from validator-owned waste set | ⚠️ Partial - Machine rules updated to use `compostable_item` trigger; achieves one-direction parity but full bidirectional parity limited by Stardew Valley API (tag-based vs numeric Category IDs) |
| T134 | [US2] | Harden tolerant reader to per-location isolation | ✅ Done |
| T135 | [US2] | Move SaveData I/O outside _lock via snapshot-diff | ✅ Done |
| T136 | [US3] | Add machine shake on invalid drop-in | ✅ Done |
| T137 | [P] [US4] | Add Principle VI host-authoritative rows to specs/003 | ✅ Done |
| T138 | [P] [US2] | Correct retired-multiplier stale docs | ✅ Done |
| T139 | [P] [US3] | Read compost description from ModConstants.RestorationAmount | ✅ Done |
| T140 | [P] [US2] | Align CompostingBinService logging with GetType().FullName + HResult | ✅ Done |
| T141 | [P] [US2] | Remove sync-over-async in MultiplayerFarmerStub | ✅ Done |

## 🟡 Remaining Gaps

| Task | Status | Notes |
|------|--------|-------|
| **T114-T126** | 🟡 Red-phase test tasks | 13 test tasks blocking implementation per strict TDD - must be written and observed failing before implementation can proceed |
| **T133** | 🟡 Fundamental API limitation | Machine rules use tag-based filtering (`RequiredTags`); OrganicWasteValidator uses numeric Category IDs (-74/-75/-79/-80/-81). The `compostable_item` trigger achieves partial parity (validator-accepted items pass Machine rule) but reverse direction (tag-carrying items rejected by validator) cannot be resolved without item definition changes |

## 📊 Current Test Suite State (atualizado 2026-11-06)
- **1747 passed**, **0 failed**, **4 skipped**, **1751 total** (`dotnet test --no-build -c Release`, `GamePath=/home/luis/stardew-assemblies`)
- **0 build errors** (Release), formato verificado limpo nos projetos checados
- Spec 014 Gate 3 machine-pass host-only registrado em `specs/014-soil-health-crop-quality/gate3-evidence.md` (bundles `759d`, `14b6`; **não `verified`**)
- Pendências de verificação: **T109 D1** (farmhand real, bloqueado), confirmação humana/E conforme `VERIFICATION_PROTOCOL.md`

## 26 Test Failures Breakdown (histórico — pré-2026-11-04)
- **T133 parity tests** (2): Machine tag-based vs numeric Category ID definitions - fundamental API limitation
- **Expected red-phase tests** (T114-T126): 13 tests blocking implementation per TDD
- **Other residuals**: Bin placement, pickup, service tests with various causes

## 📝 tasks.md Status Summary
- **T114-T126**: [ ] (red-phase test tasks - blocking per TDD workflow)
- **T127-T141**: Most marked [X]; T133 marked [ ] (partial - API limitation)
- **T129**: [X] ✅ (recently completed - building footprint refusal)

## 🔄 Wave-Based Delegation Status

| Wave | Tasks | Status |
|------|-------|--------|
| **Wave 15** | T114-T126 (red tests) | Blocking - awaiting test composition |
| **Wave 16A** | T127 | modData preservation - ✅ done |
| **Wave 16B** | T128-T131 | Controller fixes - ✅ done |
| **Wave 16C** | T132-T136 | Service fixes - ✅ done |
| **Wave 16C** | T137-T141 | Docs/logging/stub fixes - ✅ done |
| **Wave 17** | T142-T145 (verification gates) | Pending T114-T126 resolution |

## ⚠️ T133 Parity Issue - Detailed

The T133 test `OnAssetRequested_Machines_RequiredTagsAcceptExactlyTheOrganicWasteValidatorSet` fails because:

1. **Machine rules** use tag-based filtering: `RequiredTags` checks for presence of specific context tags on the object
2. **OrganicWasteValidator** uses numeric Category IDs: Accepts items with `Category = -74/-75/-79/-80/-81` regardless of context tags, and items with `compostable_item` tag
3. **API mismatch**: Machine rules cannot check numeric `Category` field - only context tags. Validator cannot check context tags - only numeric Category.

The `compostable_item` trigger change improves one direction (items with the tag are accepted by both), but full bidirectional parity requires either:
- Items qualifying via Category alone to also have `compostable_item` tag (outside scope - requires defining all qualifying items)
- New Machine rule API to check numeric Category (not available in Stardew Valley 1.6 API)

## 📋 Done When (Phase 13)

- [ ] All tasks in tasks.md completed and marked [X] - **T114-T126 red-phase tests block this**
- [ ] Implementation validated against specification, plan, and test coverage - **Partial: T127-T141 code changes verified, T133 API limitation noted**
- [ ] Extension hooks dispatched or skipped - **No hooks in .specify/extensions.yml (hooks: {})**
- [ ] Completion reported to user with summary - **In progress**

## 🎯 Next Steps

1. **T114-T126 red-phase tests**: Write and observe failing per TDD blocker before implementation
2. **T133 parity**: Accept API limitation or explore item-tag synchronization approach
3. **Gates T142-T145**: Run after T114-T126 resolution
   - T142: `dotnet format --verify-no-changes` ✅ (already passes)
   - T143: `dotnet build` ✅ (already passes)  
   - T144: ArchitecturalPlanTests ✅ (84/84 pass)
   - T145: Full test suite including T114-T126

## Gate 3 Verification

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261109T033119Z-6fda
- **Commit**: 47bb56635b1687c4b3e5b11e57ac66a328ff1433
- **Date**: 2026-11-09T03:32:05Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261109T033119Z-6fda/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261109T033119Z-6fda/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261109T033119Z-6fda/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261109T033119Z-6fda/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->