# ROADMAP.md - Stardew Valley: Living Roots

This document outlines the development roadmap for the "Living Roots" mod, detailing the progression of features from early to late game, based on agroecological principles.

> **STATUS ANNOTATION — 2026-11-07 (later same day — spec 017 Joja/Agrochemical MVP — Phase 11 CLOSED; Phase 12 CLOSED)**
> Spec 017 Phase 11 (T105–T118) is **closed and checked off** (`c6c4e7a`; the last sub-item, T117(b) reserved-enum remarks, landed as `d69168f`); Phase 12 (T119–T124) evidence-hygiene tasks are closed as well (`4764aaa`–`2fa1a19`, `1ff107b`), with T123 executed as this annotation. Counts cited below (targeted **123/123**, full-suite **1875/0/4**) come from the committed Phase-11 re-run logs `verification/convergence-phase11-targeted.log` / `-full.log` (T119/T124), **not** from `validate.md` (which attests 119/119 and 1871/0/4 on the c9ff596 tree, 2026-11-07). "Phase 11 CLOSED" refers to artifact-backed task closure (all T105–T118 `[x]`) and the committed T124 verification, not to a committed converge artifact; post-Phase-11 converge passes are in-session records, not artifacts. **Gate 3 (017 scenario) stays machine-pass host-only** — runner-promoted markers untouched; bundle `evidence-bundle-run-20261107T040235Z-589a/`, honesty `R`, commit `c9ff596`; **no Gate-3 re-run over the 0.1.0 build** (disclosure in `release.md` stands). **Machine-pass ≠ verified**: D1 (real farmhand), `human-verified`, SC-004 remain `pending`. The 2026-11-07 annotation below (Phase 11 in progress) and the 2026-11-06 annotation (014-specific) are retained verbatim; this block supersedes the "in progress" clause only. No claim is promoted by this annotation; the two `blocked` claim rows remain `blocked`.
> **STATUS ANNOTATION — 2026-11-07 (spec 017 Joja/Agrochemical MVP — release 0.1.0; Phase 11 converge in progress)**
> Spec 017 has shipped to the repo at release **0.1.0** (`d3ada42`; manifest `0.0.1`→`0.1.0`; MDMV release gate; repo-scoped on `dev` — **no public channel, no tag**; human attestation ausente por desenho). **validate.md: aprovado** (2026-11-07, commit `c9ff596`; suíte 1871 passed / 0 failed / 4 skipped, targeted 119/119, format exit 0; claims validator PASS) recorded in `.specify/validations/017-joja-agrochemical-tradeoff/validate.md`. **Gate 3 (017 scenario): machine-pass host-only** — runner-promoted markers untouched; bundle `evidence-bundle-run-20261107T040235Z-589a/`, honesty `R`, commit `c9ff596`. **Machine-pass ≠ verified**: D1 (real farmhand), `human-verified` and SC-004 remain `pending`. **Phase 11 converge is in progress** (T105–T118); the next converge pass is expected to report converged or near-converged, with remaining items being human attestations/SC-004 follow-ups. The 0.1.0 build was **not Gate-3-run in isolation** — the delta vs. the attested `c9ff596` tree is the manifest `Version` string only (disclosed in `.specify/releases/017-joja-agrochemical-tradeoff/release.md`). The 2026-11-06 annotation below remains the 014-specific attestation; this block adds 017. No claim is promoted by this annotation.

> **STATUS ANNOTATION — verified 2026-11-06 (spec 014 T131–T133, machine-pass host-only, tests 1747/0/4)**
> Previous annotations (2026-11-05, 2026-11-04, 2026-11-03, 2026-11-02) are preserved below; newest verified state is this block. Legend: ✅ implemented & verified (build + tests green) · 🟡 partial/inert · ❌ not implemented · ⚠️ has caveats / not verified in-game-human. Original design text preserved; annotations added.
> **Verified now:** build Release 0 errors (`GamePath=/home/luis/stardew-assemblies`); tests **1747 passed / 0 failed / 4 skipped** (1751); band-scale 31/71 gate PASS. Spec 014 **131/133** (`tasks.md`) — T131–T133 complete; FR-009/A8 soil-health sync + procedural harvest feedback wired/tested; `/speckit.converge` found no new actionable findings (no Phase 11). Gate 3 spec 014 scenario **machine-pass** (host-only), `verdict: pass`, `signalsComplete/Agree/thresholdsOk: true`, verifier exit 0 — bundles `evidence-bundle-run-20261106T014215Z-759d`, `evidence-bundle-run-20261106T014750Z-14b6` (`specs/014-soil-health-crop-quality/gate3-evidence.md`). **Machine-pass ≠ verified** (no human E attestation). **T109 D1 blocked** (real farmhand). **T110 deferred** (historical 34/67 contract migration). Specs: 010 **150/203**, 016 **43/43**.
> **Git snapshot (2026-11-05 refresh):** PRs #701–#705 mesclados em `dev`; higiene concluída (local/remoto apenas `dev`/`main`). `origin/dev` em `0260fed`; `dev` local pode estar alguns commits à frente apenas com docs desta atualização — publicar quando apropriado (sem reescrever histórico).
> **Sources (2026, high reliability, refreshed 2026-11-05):** SMAPI **4.5.2** (atual) / SDV **1.6.14+** (https://smapi.io/ ; https://github.com/Pathoschild/SMAPI/releases/tag/4.5.2); smapi.io mod-compat list com caveat de “sunset” (https://smapi.io/mods); JunimoServer headless/E2E reference (https://docs.junimoserver.com/, latest release marcado instável no site — **contexto, não evidência Living Roots**); Nexus #39740 acesso direto bloqueado (403) nesta sessão — contexto já registrado no repo (`research/2026-11-05-web-research-ecosystem-status.md`).

> **2026-11-05 (preserved):** spec 010 149/203, spec 016 converged, spec 004 machine-pass `535d`, suite 1630/0/4. T138/T145 blocked by environment.
>
> > **2026-11-05 (refresh, pesquisa web + hygiene):** fatos externos revalidados via fontes primárias (SMAPI 4.5.2/SDV 1.6.14+, caveat compat list, JunimoServer como referência). Nexus #39740 não revalidado ao vivo (403). Estado técnico do produto **sem regressão conhecida** vs. anotação 2026-11-06: spec 014 131/133 machine-pass host-only 759d/14b6, 1747/0/4, D1/T110/humano E pendentes. Ver `research/2026-11-05-web-research-ecosystem-status.md`.

---

## Progresso consolidado — 2026-11-06 (spec 014 harvest-quality machine-pass host-only + pesquisa atualizada)

### Quanto do caminho já percorremos (verificado)

**Em código/testes (verificado nesta sessão):**
- ✅ Núcleo persistente de solo (`SoilHealthService`, `ModDataService`, sanitização, cache esparso), decay sazonal, overlay/tooltip/hoe feedback.
- ✅ Composting loop (spec 004, machine-pass histórico) + mulch (spec 013) + growth modifier (spec 005).
- ✅ **Spec 014 (131/133):** tiers Cornell CSHA 31/71, classificação/decisão de qualidade e extra-colheita com guards, composição com matemática vanilla, feedback procedural headless-safe (`HarvestFeedbackTextures`), **sync FR-009/A8** host→cliente (`DayStarted` + após `TryApplyCompostWithLock` bem-sucedido). T131–T133 completos; converge sem achados acionáveis novos.
- ✅ Runner headless (specs 010/016) produz bundles verificáveis; **machine-pass host-only** para cenário `soil-health-harvest-quality` (`759d`, `14b6`, verifier exit 0). **Não `verified` humano.**
- ✅ Suíte: **1747 passed / 0 failed / 4 skipped** (1751), build 0 erros, format/band-scale PASS.

**Pendências críticas de evidência:**
- ⚠️ **T109 D1 bloqueado:** convergência ao vivo com farmhand real não atestada (FR-009/A8 testada por unidade/regressão).
- ⚠️ **T110 deferido:** contrato histórico 34/67.
- ⚠️ Gate 3 humano/E pendente.

**Não implementado (roadmap):** Joja Fertilizer/monocultura completa, heirloom pipeline, rotação/policultura/adubo verde/animais, Elena, mid/end-game (maioria), D1 ao vivo.

### O que ainda falta (prioridade prática)

1. **Reconciliação 014↔015 (80/20, baixo custo):** `/speckit.analyze`/convergência baseada em evidência — mapear tarefas spec 015 já cobertas por 014, duplicidades (Junimo) e bloqueios reais (D1/humano/pest); sem reescrever histórico.
2. **D1 real (T109):** ambiente com segundo jogador + evidência honesta (FR-009/A8 testada por unidade, **não** atestada ao vivo).
3. **Publicar docs desta atualização:** `dev` local pode estar à frente de `origin/dev` por commits de documentação; push/PR conforme decisão humana.
4. **Pendências de specs:** 004 T034/T037 (manual/D1), 009 manual, 010 restantes, 015 abertas (validar contra 014), 011/013 residuais — priorizar por valor.
5. **Próximo slice de gameplay (após reconciliação):** candidato 80/20 = tradeoff destrutivo/decisão (ex. Joja/monocultura, escopo controlado) com TDD + Gate 3 honesto.

## Progresso consolidado — 2026-11-04 (histórico, pós-merge PRs #655/#656/#657 + pesquisa web)

### Quanto do caminho já percorremos (verificado em 2026-11-04)

**Em código/testes (verificado por build + testes nesta sessão):**
- ✅ Núcleo persistente de solo (`SoilHealthService`, `ModDataService`, sanitização, cache esparso).
- ✅ Composting loop (bin → 2 dias → composto → +15, clamped) — spec 004 (machine-pass).
- ✅ Mulch (7 dias, decay skip, overlay tint, host-authoritative) — spec 013 (46/48).
- ✅ Growth modifier (health → growth speed) — spec 005 (143/146).
- ✅ Soil decay sazonal (greenhouse exempt).
- ✅ Seam de qualidade (`HarvestQualityService`, 17 testes) + patch Harmony + wiring — specs 014/015 (infra apenas; patch **não executado em jogo**).
- ✅ DDD, segurança, concorrência, quality gate local, smoke launch (mod carrega no SMAPI).
- ✅ UI/visualização reativa (overlay/tooltip/hoe feedback).

**Verificado in-game (Gate 3):** ⚠️ machine-pass headless (spec 004, 2026-11-04) — **não** Gate 3 manual 3-sinais (E humano) pendente.

**Roadmap items shipped:** 3 de 21 features nomeadas (composting, mulch, soil-health→crop-growth) + 2 promessas de core (soil decay, growth driver) + seam de harvest-quality (infra apenas).

**Não implementado:** 18 de 21 features nomeadas + 7 de 13 promessas do core (qualidade de colheita real, Joja/monocultura, heirloom, mid/end-game, Elena, multiplayer ao vivo).

### O que ainda falta (resumo executivo, ordem de prioridade)

1. **Completar spec 010 (headless verification runner) e gerar `machine-pass`** — tarefas T130–T150 (verificação e promoção) estão pendentes. O runner está em 137/203 concluídas. **Este é o caminho obrigatório para `machine-pass` antes de qualquer verificação manual in-game.**
2. **Rodar Gate 3 via spec 010** (3 sinais: SoilHealthState delta, Crop phase delta, SMAPI log) — captura `machine-pass` na feature 004 sem abrir o jogo manualmente.
3. **Completar integração harvest-quality** (feedback sprites/SFX, extra-harvest gated em SC-008) — specs 014/015. **80/20 principal de gameplay.** Só após o spec 010 estar verde.
4. **Reconciliar band scale** (3 doc-only tasks T117–T119, spec 005).
5. **Fechar bug do test harness** (hang em test-preflight/integration/logparse).
6. **Joja Fertilizer** (fecha loop "destrói") — próximo após harvest-quality.
7. **Heirloom pipeline** → Three Sisters, Green Manure, Rotation Journal.
8. **Multiplayer ao vivo (D1)** — owed desde spec 004; Constitution Principle VI.
9. **Mid-game (6) e End-game (6)** — 0 de 12 iniciados.
10. **Specs 001 (visualização), 011 (quality gate CI)** — em andamento/parcial.

### Próximo passo — 80/20 (maior avanço, menor esforço)

**Recomendação: completar spec 010 (headless verification runner) e gerar `machine-pass` na feature 004, APENAS ENTÃO `/speckit.implement` continuando specs 014/015 (harvest-quality) + `/speckit.converge`.**

Justificativa (com pesquisa de mercado 2026):
- O spec 010 é o único caminho para `machine-pass` sem abrir o jogo manualmente. Todas as 137 tarefas de implementação já estão feitas; faltam as tarefas de verificação e promoção (T130–T150).
- Após o spec 010 gerar `machine-pass`, specs 014/015 (harvest-quality) é o único item de gameplay que faz Living Soil (71–100%) **pagar na colheita** (qualidade silver/gold/iridium + colheita extra).
- Infraestrutura cara (Harmony, seam, testes, wiring) já mergeada — custo marginal é integração + verificação.
- Mercado valida "qualidade dirigida por skill/solo" (Crop Quality, Simple Skills Extended); ninguém entrega per-tile com boost determinístico + feedback.
- ReGen Valley entrega farm-wide quality a 75+/90+ avg, mas não per-tile com boost determinístico e feedback transient — nosso seam é único.

**Pré-requisitos (em ordem):**
1. **Completar spec 010 tarefas T130–T150** (verificação e promoção) — `dotnet build`, `dotnet test`, `dotnet format`, shell, schema, launch-free e2e, real-host e2e, second-host 3/3, timed dry run, audit/deletion drill, bundle negatives, check-docs negatives, acquisition negatives, suite negatives, farmhand path.
2. **Rodar Gate 3 via spec 010** — capturar `machine-pass` na feature 004 (compost-growth-hardening.scenario.json) com três sinais.
3. **Reconciliar band scale** (T117–T119, doc-only, spec 005).
4. **Fechar hang do harness** (bug assess/fix, spec 008).

**Somente após o spec 010 estar verde e o `machine-pass` gerado:**
- `/speckit.implement` continuando specs 014/015 (harvest-quality integração) → `/speckit.converge`.
- Verificação manual in-game (opcional, confirmação humana E attestation) — nunca substitui o runner.

**Alternativa (se quiser fechar loop destruição primeiro):** `/speckit.specify` para "Joja Fertilizer" (boost agora, −5% saúde) — fecha o "o que destrói", custo baixo, mas não faz Living Soil pagar.

**Não recomendado agora:** Elena NPC (alto custo/conteúdo), Heirloom pipeline (bloqueia 3 features, alto custo), Mid/End-game (0 de 12).

### Custo vs. impacto (ranking 80/20)

| Item | Custo | Impacto | Veredito |
|------|-------|---------|----------|
| Completar harvest-quality (014/015) | Médio | Living Soil paga na colheita | **80/20 principal** |
| Rodar Gate 3 (in-game) | Baixo | Desbloqueia "verificado" | **Fazer já** |
| Reconciliar band scale (T117–T119) | Baixo (doc-only) | Convergência spec 005 | **Fazer já** |
| Fechar hang do harness | Médio | Destrava CI local | **Fazer já** |
| Joja Fertilizer | Baixo | Fecha loop "destrói" | **Próximo após harvest-quality** |
| Heirloom pipeline | Alto | Desbloqueia 3 features | **Depois** |
| Elena + Welcome Kit | Muito alto | Conteúdo/NPC | **Por último** |
| Mid/End-game (12 features) | Muito alto | Conteúdo | **Por último** |

---

## Where we actually are (verified, not inferred) — atualizado 2026-11-06

| Metric | Value | How verified |
|---|---|---|
| Build | **0 erros** (Release) | `dotnet build Stardew-LivingRoots.sln -c Release` (`GamePath=/home/luis/stardew-assemblies`) |
| Test suite | **1747 passed / 0 failed / 4 skipped** (1751) | `dotnet test Stardew-LivingRoots.sln --no-build -c Release` |
| Format / band-scale | clean / PASS | `dotnet format --verify-no-changes --no-restore`; `scripts/tests/test-band-scale-31-71.sh` |
| Spec 014 | **131/133**; T131–T133 done; **T109 D1 blocked**, **T110 deferred** | `specs/014-soil-health-crop-quality/tasks.md`, `gate3-evidence.md` |
| Spec 010 / 016 | **150/203** / **43/43** | `specs/*/tasks.md` |
| Gate 3 (spec 014 scenario) | **machine-pass host-only** (759d, 14b6), verifier exit 0, 3-signal predicates pass — **NOT verified (human E)** | `gate3-evidence.md`, `evidence-bundle-run-20261106T014750Z-14b6/` |
| Roadmap/realidade | soil measure/display/change/decay/compost/mulch/growth + harvest-quality tier logic/wiring/feedback/sync implemented in code with automated evidence; MP convergence not human-verified; D1 not attested | source + tests + gate3-evidence |
| Git (refresh 2026-11-05) | PRs #701–#705 merged; hygiene done (só `dev`/`main` local/remoto). `origin/dev`=`0260fed`; `dev` local pode estar à frente apenas por docs desta sessão | `git status`, `gh pr list`, `git branch -vv/-r`, `git ls-remote --heads origin` |

**One-sentence truth (2026-11-06):** Living Roots mede, persiste e muda soil health; solo afeta crescimento e, no código atual (spec 014, 131/133), também direciona qualidade/extra de colheita com guards, feedback procedural e sync FR-009/A8 — com **machine-pass headless host-only** (759d/14b6) e suíte 1747/0/4 — mas **sem atesto humano `verified`** e com **D1/T110** ainda pendentes.

## Where we actually are (histórico) — 2026-11-04

### Progress snapshot — 2026-11-04 (post-014/015 merge + pesquisa de mercado)

**How far we've come (verified):**
- ✅ Soil health per-tile, persisted, displayed (overlay/tooltip/hoe), decay (seasonal, greenhouse-exempt).
- ✅ Composting loop end-to-end in code (bin → 2 days → compost → +15 health, clamped).
- ✅ Mulch (7-day cover, decay skip, overlay tint, host-authoritative).
- ✅ Growth modifier (health → growth speed).
- ✅ Pure, testable `HarvestQualityService` seam (17 tests) + Harmony patch for `Crop.harvest` (non-suppressing two-phase, try/catch fallback) + defensive `ModEntry` wiring.
- ✅ DDD architecture, security sanitization, async-only, multiplayer-aware patterns, quality gate (Semgrep/Trivy/jscpd), 1621+ tests.
- ✅ Smoke validation (spec 008) implementado e verificado: real-game run exit 0 com success marker.

**What still doesn't work / isn't verified:**
- ✅ **Build verde** (0 erros, 0 warnings).
- ✅ **Testes verdes** (1621 pass / 1 fail pré-existente / 4 skip).
- ❌ **No live SMAPI session ever run** (Gate 3 manual 3-sinais). Every "works" claim above is code/tests, not in-game.
- ⚠️ Harvest-quality patch not executed in game; integration (feedback sprites/SFX, extra-harvest activation) incomplete; SC-008 rollback verification still pending.
- ✅ Band scale reconciled to Cornell CSHA 31/71 (`[0,31)/[31,71)/[71,100]`) by spec 014 FR-013/T127 (supersedes prior 34/67/0-30-70 mismatches).
- ❌ Multiplayer live verification (D1) not proven.

**What's still not implemented (roadmap):**
- 18 of 21 named features (Elena NPC, heirloom pipeline, Three Sisters, green manure, rotation journal, Joja Fertilizer, monoculture penalty, integrated animals, insect hotels, living pharmacy, bio-construction, ICLF, greywater, weekly fair, CSA, collective kitchen, food sovereignty, cistern, rainwater harvesting).
- Mid-game (6) and end-game (6) features: 0 of 12 started.
- Specs 010 (headless runner): not started (0/161).

---

## Pesquisa de mercado e ecossistema (verificada 2026-11-03)

Fontes primárias: Nexus Mods, CurseForge, PCGamesN/Technwz (listas de melhores mods 2026), SMAPI release notes (Pathoschild), Stardew Valley Wiki, mod docs.

### Ecossistema (2026)

- **SMAPI 4.5.2** (lançado 14/03/2026) para Stardew Valley 1.6.14+; 4.4.0+ (jan/2026) adicionou UI de config in-game via GMCM e builds atestados. Mods devem mirar 4.4.0+ / 1.6.14+.
- **Stardew Valley** está em 1.6.15 (referenciado por mods recentes); sem Steam Workshop oficial — distribuição via Nexus/CurseForge + SMAPI.
- **Nexus Mods** concentra dezenas de milhares de mods; listas de "best mods 2026" focam em QoL (Chests Anywhere, Stamina Regen, Crops Anytime Anywhere, Simple Crop Label) — **não há mod de agroecologia profunda** além de ReGen Valley e afins.
- **Multiplayer/headless**: existem mods de servidor dedicado (always-on-server, SMAPIDedicatedServerMod) para co-op; relevante para o pilar "co-op desde o dia 1" (Principle VI).

### Concorrentes diretos (landscape 2026)

| Mod | Foco | Downloads (aprox.) | Sobreposição com Living Roots |
|-----|------|-------------------|-------------------------------|
| **ReGen Valley** (Nexus, 2025) | Soil health farm-wide (75+/90+ avg) | — | Concorrente mais próximo; **não** é per-tile com boost determinístico + feedback transient |
| **Crop Quality** (aedenthorn, 0.2.0) | Exibe qualidade da colheita; tweaks de chances | 9.2K (CurseForge) | Complementar — exibição, não mecânica de solo |
| **Simple Skills Extended** (Xsyy, 1.18, out/2026) | Skills com bônus de qualidade/extras, por-farmer, integra Cornucopia/SpaceCore | — | Concorrente em "qualidade dirigida por skill"; não per-tile por saúde do solo |
| **Spore Galore** (HomesteadyEddy) | Cultivo de cogumelos + compostagem (substrato gasto → composto em 2 dias) | — | Sobrepõe compostagem; fecha loop "waste → compost → fertilizer" |
| **Harvest Ledger** (Tigrevurmud666, 0.5.0) | Economia dinâmica com streak de rotação de culturas (+3%/+5% venda) | — | Sobrepõe rotação; valida demanda por mecânica de rotação |
| **Tilled Soil Decay Options** (CaptainSully, 2026) | Configura decay de solo arado (atravessa estações) | — | Sobrepõe decay de solo; mostra demanda por controle de decay |

### Implicações para o roadmap

1. **Diferencial único confirmado**: per-tile soil health com boost determinístico de qualidade + colheita extra + feedback transient **não existe** no ecossistema. Simple Skills Extended dá bônus por skill (farm-wide), ReGen Valley dá por média da fazenda. Living Roots é o único per-tile host-authoritative com feedback. → priorizar specs 014/015 (harvest-quality) + Gate 3.
2. **Compostagem não é diferencial** (Spore Galore já fecha o loop waste→compost). O diferencial de Living Roots é a **coerência agroecológica** (composto + mulch + rotação + policultura + adubo verde) e o **framing educativo**, não a mecânica isolada.
3. **Rotação de culturas tem demanda validada** (Harvest Ledger streaks). Vale investir em Three Sisters / Rotation Journal / Green Manure (mid-game) após harvest-quality.
4. **Multiplayer é diferencial real** se verificado ao vivo (D1): a maioria dos mods de solo não é host-authoritative per-tile com first-wins. Verificar D1 cedo aumenta o valor percebido.
5. **Ecossistema está em 1.6.15 + SMAPI 4.5.x**: manter target 1.6.14+ / SMAPI 4.4.0+ (já adotado) é correto; não há pressão para migrar de framework.

### Onde estamos (atualizado 2026-11-04, com pesquisa)

- **Implementado e verificado por build/testes**: núcleo de solo persistente, compostagem, mulch, decay sazonal (greenhouse exempt), growth modifier, seam de harvest-quality (infra), smoke de carregamento SMAPI.
- **Implementado mas NÃO executado in-game**: patch Harmony de `Crop.harvest`, feedback sprites/SFX, colheita extra.
- **Não implementado**: Joja Fertilizer, Heirloom pipeline (Three Sisters, Green Manure, Rotation Journal), mid/end-game (12 features), Elena NPC, Welcome Kit, headless runner (010).
- **Verificação real (Gate 3)**: 0 runs completos — maior gap.

### O que o mercado NÃO faz (nosso espaço)

- Nenhum mod combina **solo por tile + compostagem composta + mulch + rotação + policultura + manejo animal + soberania alimentar + framing educativo + multiplayer correto** num único loop coerente.
- Nenhum mod tem **verificação formal de Gate 3 (in-game, multi-sinal)** nem **quality gate local (Semgrep/Trivy/jscpd)**.
- Nenhum mod tem **testes property-based (FsCheck)** nem **1400+ testes**.

---

## Core Concept: The Soil Health Bar

All mod mechanics will revolve around "Soil Health," a new property for each tillable tile on the farm (visible when using a hoe or hovering the mouse, ranging from 0% to 100%).

*   **Poor Soil (0-30%):** Slow growth, high chance of pests, lower quality produce (no stars).
*   **Medium Soil (31-70%):** Similar to the base game.
*   **Living Soil (71-100%):** Faster growth, chance of extra harvests, higher quality (silver/gold/iridium), and enables additional agroecological practices (see separate spec — "new interactions" deferred).

**What DESTROYS Soil Health:**

*   Leaving soil tilled and bare (without cover or plants).
*   Monoculture (planting the same crop repeatedly).
*   Using chemical fertilizers (new "Joja Fertilizer" items, which provide an initial boost but cost 5% soil health).

**What BUILDS Soil Health:**

*   Composting and organic fertilizer application.
*   Crop Rotation.
*   Polyculture / Companion Planting.
*   Green Manure.
*   Soil Cover (Mulch).
*   Animal Management.

**Status of the core concept:**

| Promise | Status | Reality |
|---------|--------|---------|
| Value per tile, 0-100, persisted | ✅ | `SoilHealthService`, sparse save, DoS-bounded |
| Visible via hoe / hover | ✅ | overlay + tooltip + hoe feedback (spec 001) |
| Poor = slow growth | ❌ | **no code reads soil health at growth time** — verified: zero references to growth/quality/harvest in `LivingRoots/` |
| Poor = pests | ❌ | pest system does not exist (`briefings/pest-system-mvp.md`) |
| Poor = no stars | ❌ | quality roll is 100% vanilla |
| Medium = vanilla | ✅ | true today *because nothing is hooked at all* |
| Living = faster growth | ✅ | spec 005 — `ModController` growth modifier hook on `Crop.newDay` (verified in code) |
| Living = extra harvest | 🟡 | seam merged (`HarvestQualityService` + Harmony patch); integration + Gate 3 pending (SC-008 gates activation) |
| Living = better quality | 🟡 | seam merged (`HarvestQualityService` + Harmony patch); integration + Gate 3 pending |
| Living = new interactions | ❌ | deferred to separate spec (accepted 2026-11-01) |
| DESTROYS: bare tilled soil | ✅ | `SoilDecayService`, seasonal multiplier, greenhouse exempt |
| DESTROYS: monoculture | ❌ | only *bare* tiles decay; a monoculture never triggers it |
| DESTROYS: Joja Fertilizer | ❌ | item does not exist (`briefings/joja-fertilizer.md`) |
| BUILDS: compost | ✅ | bin → compost → +15/application, clamped (spec 004) |
| BUILDS: rotation / polyculture / green manure / animals | ❌ | four unimplemented |
| BUILDS: mulch (soil cover) | ✅ | spec 013 — 1 object + 1 recipe + decay hook, 7-day duration, 0f decay multiplier |

> **Banding reconciled (spec 014 FR-013/T127):** governing scale is Cornell CSHA `[0,31) / [31,71) / [71,100]` in `LivingRoots/Domain/Visualization/HealthCategory.cs` and `Constants.cs`. Historical 34/67 and 0-30/31-70/71-100 notes are migration history.

---

## Game Progression

### 🎨 Early Game (Year 1 - Learning the Fundamentals)

The focus is on survival and learning the new soil mechanics, being introduced to the basic concepts of agroecology.

*   **New NPC: Elena:** An agronomist and activist who serves as the player's mentor, introducing new mechanics and quests.
    ❌ **Not implemented.** No NPC, no dialogue, no schedule anywhere in the repo. Briefing exists: `specs/future-roadmap/briefings/elena-npc.md`. Note: an NPC is the single most content-expensive item on this roadmap (dialogue, schedules, portraits, festivals, hearts) and is *not* on the cheapest path — see the 80/20 order below.
*   **Agroecological Welcome Kit:** Replaces the initial parsnip seeds with a kit that includes a Composter and heirloom seeds.
    ❌ **Not implemented** (`briefings/welcome-kit.md`). Blocked by Elena (who gives the kit) and heirloom seeds (which the kit contains).
*   **Composting:** Unlocks the Composter (new machine) to transform organic waste into "Compost" (basic fertilizer).
    ✅ **Implemented and verified.** Crafting recipe, `Data/BigCraftables` bin, `Data/Machines` rule (`Triggers` + `DaysUntilReady = 2`), compost item with its own icon, pickup that preserves progress in `Item.modData` (FR-005c), tolerant save reader, refusal messages via `Game1.showRedMessage` in EN/PT. This is the only fully delivered gameplay loop in the mod.
    ⚠️ **But it has never been executed inside the game** — see "Where we actually are".
*   **Soil Cover (Mulch):** Ability to craft and apply mulch to prevent weeds and (most importantly) maintain Soil Health.
    ✅ **Implemented and verified** (spec 013, 46/46 tasks). Crafting recipe (`LivingRoots_MulchRecipe`, fiber ×10), `Data/Objects` entry `LivingRoots_Mulch`, 16×16 sprite, 7-day duration, decay hook in `SoilDecayService` (mulched tiles skip decay), overlay tint (`MulchColor` + Dots pattern), host-authoritative lock, i18n EN/PT. This is the first *defensive* tool the player gets and the second complete gameplay loop after composting.
    ⚠️ **Has never been executed inside the game** — Gate 3 evidence outstanding (same as composting).
*   **Simple Polyculture (Companion Planting):** Introduction of "The Three Sisters" concept (Corn, Snap Beans, and Pumpkin) for growth bonuses and soil improvement.
    ❌ **Not implemented** (`briefings/three-sisters-polyculture.md`). The briefing explicitly rejects reusing the three vanilla crops: Green Bean is spring-only and trellised (`IsRaised`), Pumpkin fall-only, so a complete trio **never coexists** in an open field. Requires 3 new crops + 3 seeds + a crop sheet — real asset work.
*   **Heirloom Seeds:** Elena starts selling heirloom seeds, which are more sustainable and beneficial for the soil, in contrast to Pierre's hybrid seeds.
    ❌ **Not implemented** (`briefings/heirloom-seeds.md`). It is the *pipeline* brief (seed → crop → produce) that Three Sisters, Green Manure and Rotation Journal all consume.
*   **Rainwater Harvesting:** New crafting item: Small Cistern, to collect water on rainy days and reduce reliance on external sources.
    ❌ **Not implemented** (`briefings/small-cistern.md`).

**Early-game subtotal: 2 of 7 done** (composting + mulch). The player can now *build* soil (compost) and *protect* it (mulch); the other five are unreachable.

---

### 🎯 Mid Game (Year 2-3 - Building the System)

The focus is on integrating systems and creating a self-sustaining cycle on the farm, deepening agroecological practices.

*   **Crop Rotation:** Introduction of a "Rotation Journal" and classification of plants by families (e.g., Brassicas - Cauliflower, Cabbage; Solanaceae - Tomato, Pepper; Legumes - Beans, Peas). Proper rotation offers quality bonuses and improves Soil Health.
    ❌ **Not implemented** (`briefings/rotation-journal.md`). Needs per-tile crop history — the mod already persists per-tile state, so the persistence cost is low; the family registry and UI are the cost.
*   **Green Manure:** Elena sells "Green Manure" seeds (like clover or oats) to incorporate nutrients into the soil and give a significant boost to Soil Health.
    ❌ **Not implemented** (`briefings/green-manure.md`). Depends on the heirloom pipeline.
*   **Integrated Animal Management:** Animals left outside in un-fenced areas now eat weeds and insects (Pest Control) and leave manure that increases Soil Health (if mulched) or compacts it (if bare).
    ❌ **Not implemented** (`briefings/pest-system-mvp.md`, `briefings/mobile-coop.md`).
*   **Beneficial Insects and Beekeeping:** Beehives now have a second function: in addition to honey, they create an aura of "Beneficial Insects." "Insect Hotels" (new crafting) also attract beneficial insects for natural pest control.
    ❌ **Not implemented** (`briefings/insect-hotels-beehive-aura.md`). ⚠️ Caution: the entomological evidence for insect hotels is weak-to-negative — see `research/2026-10-roadmap-feasibility-and-agronomy.md` §C.3. Do not present the pest-control benefit as established agronomy.
*   **Living Pharmacy:** Learn from Elena or Linus to grow medicinal herbs (Rue, Boldo, Chamomile). These can be used to create tonics (energy buffs) or "Natural Sprays" (Pest Control without soil damage).
    ❌ **Not implemented** (`briefings/living-pharmacy-tonics-sprays.md`). Needs 3 new crops + crafting + buffs.
*   **Bio-construction:** Robin learns from Elena to offer new construction upgrades: Adobe Shed, Green Roof.
    ❌ **Not implemented** (`briefings/adobe-shed-green-roof.md`).

**Mid-game subtotal: 0 of 6 done.**

---

### 🏆 End Game (Year 4+ - Sovereignty and Community)

The focus is on expanding the agroecological philosophy throughout the Valley, replacing the Joja system and achieving "Food Sovereignty."

*   **ICLF (Integrated Crop-Livestock-Forestry):** new trees (e.g., Acacia, Gliricidia) that fix nitrogen, allowing cultivation between them for drought immunity and faster growth.
    ❌ **Not implemented** (`briefings/iclf-trees.md`).
*   **Greywater Recycling:** House upgrade that collects greywater into tanks as a passive water source.
    ❌ **Not implemented** (`briefings/greywater-recycling.md`).
*   **Short Supply Chains (The Stardew Fair):** a Weekly Fair (every Saturday) in the town square; residents want variety, +25% profit and friendship.
    ❌ **Not implemented** (`briefings/weekly-fair.md`).
*   **Community Supported Agriculture (CSA):** residents subscribe to your CSA Box; deliver a varied basket weekly.
    ❌ **Not implemented** (`briefings/csa-box.md`).
*   **Collective Kitchen (The New Community Center Quest):** weekly event, cooking minigame, Community Meals buffs.
    ❌ **Not implemented** (`briefings/collective-kitchen-meals.md`).
*   **Food Sovereignty (The Final 'Victory'):** journal metric requiring >80% average Soil Health, 10 heirloom seed types, Collective Kitchen operational, 5 CSA families; reward: Pierre stops selling Joja hybrids, Morris leaves, Elena gives the "Guardian of Living Roots" trophy.
    ❌ **Not implemented** (`briefings/food-sovereignty-guardian-trophy.md`, `end-game-year4-plus.md`). Note: three of its four conditions (heirloom seeds, kitchen, CSA) are themselves unimplemented — the victory condition is the *last* thing that can ship, not the first.

**End-game subtotal: 0 of 6 done.**

---

## Progress summary

| Phase | Items | Shipped | Verdict |
|-------|-------|---------|---------|
| Core concept (soil health bar) | 13 promises | 6 (measured/displayed/destroyed + growth driver + mulch + harvest seam infra) | **growth + protection + harvest seam (infra) shipped; quality/harvest *verified* still missing** |
| Early Game (Y1) | 7 | 2 (composting, mulch) | tutorial tier less thin |
| Mid Game (Y2-3) | 6 | 0 | not started |
| End Game (Y4+) | 6 | 0 | not started |
| **Total** | **21 named features + 13 core promises** | **2 features + 6 of 13 promises (1 infra-only)** | |

All 31 design briefings for the unbuilt items already exist under `specs/future-roadmap/briefings/`. What is missing is not design — it is **implementation**, and the first implementation that makes soil health *matter*.

---

## Roadmap order — the 80/20

Ordering by *felt player impact per unit of implementation cost*, not by in-game year. Rationale and cost analysis in the answer that produced this file.

1. ⚠️ **Prerequisite, not a feature:** launch the mod once and confirm the compost loop runs (`T034`/`T037` of spec 004 are blocked on exactly this). It has never been executed in a game. ~30 minutes, no code. **Still pending — Gate 3 evidence outstanding.** This is now the single biggest unverified assumption in the project and blocks "verified" status for every shipped feature, including the new harvest-quality seam.
2. ✅ **`/speckit.specify` — Soil Health → Crop Growth (spec 005).** Done: `ModController` growth modifier hook on `Crop.newDay`, per-tile persistence and overlay reused. This was the 80/20 and it is shipped (143/146 tasks; 3 remaining are doc-only band-scale updates).
3. ✅ **Mulch / Soil Cover (spec 013).** Shipped: 1 object + 1 recipe + decay hook, 7-day duration, 0f decay multiplier, overlay tint, host-authoritative lock, i18n EN/PT (46/46 tasks). The defensive side now exists and has a reason to exist because soil health drives growth.
4. 🟡 **Spec 014 harvest-quality — implementado em código, machine-pass host-only (não `verified`).** Tiers 31/71, qualidade/extra com guards, feedback procedural e sync FR-009/A8 estão implementados/testados (spec 014 **131/133**; T131–T133 completos). Gate 3 cenário harvest-quality: **machine-pass** (bundles `759d`, `14b6`, verifier exit 0). **Falta:** **T109 D1** (farmhand real, bloqueado), **T110** (contrato histórico 34/67, deferido), e **atesto humano/E** para `verified`. Alinhar/fechar tarefas abertas de spec 015 conforme aplicável sem reimplementar o que 014 já entregou.
   Entry point: planejar D1 quando houver ambiente; reconciliar 014/015 tasks com evidência real; depois próximo slice de gameplay com TDD + Gate 3 honesto.
5. **Heirloom pipeline** → then Three Sisters, Green Manure, Rotation Journal (all consume it).
6. **Joja Fertilizer** — closes the "what DESTROYS" loop on the player's side.
7. **Elena + Welcome Kit + Cistern** — the content-heavy NPC epic; do it once there is a game worth tutoring.
8. Mid-game and end-game items, as designed.
9. **Multiplayer live verification (D1)** — owed since spec 004; Constitution Principle VI forbids declaring multiplayer complete without it.

---

## Reference point (external)

`ReGen Valley` (Nexus, Nov 2025) already ships this premise end to end — per-tile soil health, cover crops, grazing, faster growth at 70+/90+ ("an extra day of growth per night") and farm-wide quality/yield at 75+/90+ average. Our differentiation is therefore **not** the soil-health mechanic; it is the agroecological grounding, the compounding compost loop, the educational framing and multiplayer correctness. Design steps 2-4 to beat it on *coherence*, not on novelty. See `research/2026-10-roadmap-feasibility-and-agronomy.md` §B.

---

## Agronomic grounding (what the evidence actually supports)

Before building any of the above, the roadmap's promises were checked against the literature (`research/2026-10-roadmap-feasibility-and-agronomy.md` §C). The result changes the priority order:

| Mechanic | Evidence | Consequence for the mod |
|----------|----------|-------------------------|
| Compost / manure | ✅ strong — long-term trials: manure raises organic C +13%, available N +19%, available P +47% | Build as promised. Keep +15 as an explicit game abstraction, not a simulation |
| **Mulch / soil cover** | ✅ **strongest** — global meta-analysis (90 studies): runoff **−47.4%**, soil loss **−76.2%** | Best-supported claim in the whole roadmap. Teach real agronomy |
| Green manure | ✅ strong — 34-yr trial: grain yield **+45%/+46%**, organic matter **+40.6%**, N total **+23%** | Build as promised |
| Crop rotation | ✅ strong but **21–52 year** timescale — soil health +32–49% over decades | Honest framing impossible at 28-day seasons: model as abstraction, say so |
| **Three Sisters** | 🟡 **overstated** — land-equivalent yield advantage is real, but bean yield was ~10× *lower* and squash ~20× *lower* in polyculture; one trial found **no significant difference in soil N** | Do not teach "sisters improve each other's soil". Model as an area-efficiency trade-off |
| **Insect hotels** | ❌ **not supported** — "bee hotels differentially augment wasps rather than native bees"; authors coined *"bee-washing"*; NC State: no strong evidence either way | **Do not ship as pest control.** If built, make the doubt the lesson |

**Ressalva honesta do mulch:** a revisão *Water* 2022 registra que palha pode **aumentar a abundância de insetos** — mulch tem trade-off real de pragas. Ótimo material de design de jogo (decisão, não bônus grátis).

---

## Recommended next action (80/20, refreshed 2026-11-05)

**1) Reconciliar 014↔015 (maior clareza/menor retrabalho):** `/speckit.analyze` (read-only) focado em 014+015 → mapear tarefas de 015 satisfeitas por implementação/evidência de 014, resolver duplicidades (ex.: Junimo T056/T059) e separar bloqueios reais (D1/humano/pest). Se acionável, `/speckit.converge` **append-only**. Baseado em `research/2026-11-06-next-step-80-20.md` e estado atual.

**2) D1 real (T109, quando viável):** sessão host+farmhand para convergência FR-009/A8; evidência honesta. JunimoServer/`xvfb`+dedicated patterns são **referências** para investigação futura, não prova.

**3) Publicar docs desta atualização:** commits locais de documentação à frente de `origin/dev` (`0260fed`) — push/PR conforme decisão humana, sem reescrever histórico.

**4) Próximo slice de gameplay (após 1):** `/speckit.specify` para tradeoff destrutivo/decisão de alto valor (candidato: Joja/monocultura, escopo controlado), com TDD + Gate 3 + honesty.

**Pesquisa/mercado (honesta, 2026-11-05 refresh):** SMAPI **4.5.2** / SDV **1.6.14+** confirmados (smapi.io/releases). smapi.io mod-compat tem caveat de sunset. JunimoServer = referência headless/E2E. Nexus #39740 não revalidado ao vivo (403 nesta sessão). Detalhes em `research/2026-11-05-web-research-ecosystem-status.md`. `machine-pass` ≠ `verified`; D1/humano pendentes.

---

## Blockers that are not roadmap items (verificar antes de agir)

* **Push/publicação de docs pendentes:** após hygiene/merges (#701–#705), `origin/dev` está em `0260fed`; novos commits locais desta atualização de documentação podem precisar de push/PR conforme decisão humana (sem reescrever histórico).
* **Evidência ≠ verificação humana:** machine-pass não é `verified`; D1/T110/humano E pendentes.
* **Licenciamento/assets:** manter vigilância — `.gitignore` protege game assemblies; verificar periodicamente `git ls-files` para DLLs proprietárias/assets indevidos.
* ~~`COMPLETION_SUMMARY.md` stale 26 failing / `AGENTS.md` ModConstants path~~ — corrigidos em rodadas anteriores (verificar se persistem em outras cópias/branches).

---

## Gate 3 Verification (atualizado 2026-11-07 — máquina via spec 010 antes de manual)

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261108T143604Z-0f8f
- **Commit**: 37032ac3c03e5b4ca66d8c45415cd0f906d988ec
- **Date**: 2026-11-08T14:37:16Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261108T143604Z-0f8f/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261108T143604Z-0f8f/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261108T143604Z-0f8f/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261108T143604Z-0f8f/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->

---

## Gate 3 Verification (histórico 2026-11-02 — smoke only)

<!-- GATE3-HISTORY:BEGIN -->
- **Status**: partial (automated smoke only)
- **Verdict**: mod loads + Harmony patch registered + no crash during launch
- **Flaky**: false
- **Run ID**: smoke-20261102
- **Commit**: c594fae
- **Date**: 2026-11-02
- **Signals**:
  - SoilHealthState delta: not captured (requires manual play: till → compost → harvest → save → reload)
  - Crop-phase delta: not captured (requires manual play)
  - SMAPI log: captured — "Patched game code" + "Stardew Valley - Living Roots 0.0.1 ... loaded" + "Mods loaded and ready!"; pre-existing "Error occurred while registering game events" (NullReferenceException in headless, documented)
- **Bundle**: verification-evidence/gate3-smoke-*.log
- **Honesty level**: smoke-level (launch only, not full 3-signal Gate 3)
- **Claim**: The mod loads under SMAPI 4.5.2 + Stardew 1.6.15 with Harmony patch registered (warning "Patched game code"). Full 3-signal Gate 3 (observation + save diff + SMAPI log) and multiplayer verification (D1) still require manual play or the headless runner (spec 010, not started). No "verified" claim is made for harvest-quality effects.
<!-- GATE3-HISTORY:END -->

---

## Competitive landscape (verified via web research, 2026-11-03)

### Direct competitors (soil-health / regenerative farming)

| Mod | Nexus DL | Approach | Differentiation vs. Living Roots |
|-----|----------|----------|----------------------------------|
| **ReGen Valley** (robertsvalley, v0.51, Nov 2025) | -- (new) | Per-tile soil health (baseline 25), cover crops (rye/clover/sorghum/buckwheat), rotational grazing (cows + pens + beacons), roller/no-till planter, water retention at >80, faster growth at 70+/90+, **farm-wide** yield/quality at avg 75+/90+ | Heavy on animal integration & machinery; no composting loop, no educational framing, no per-tile harvest quality, no multiplayer host-authoritative design documented. Our edge: coherence + compost loop + educational framing + multiplayer correctness. |
| **Permaculture Farming** (wiki) | — | Mulch crop (2-day grow) prevents tilled soil from disappearing at season change | Narrow: only soil disappearance prevention. |
| **Spoilage** (Nexus) | — | Realistic food rot + composting bin (perishables → compost) | Composting-only; no soil health, no growth/quality linkage. |
| **Spore Galore** (Nexus) | — | Mushroom cultivation; composting bin (spent substrate → mushroom compost) | Niche (mushrooms); no soil-health system. |
| **Crop Quality** (CurseForge) | — | Replaces days-played quality roll with a random number so same crop = same quality | Orthogonal: quality determinism, no soil linkage. |

### Adjacent (quality/yield boosters, no soil)

- **Quality Giant Crops** — quality roll on giant crops (vanilla formula).
- **Farming level yield mods** — yield scales with Farming skill.
- **Quality Sprinklers / Iridium Sprinklers** — layout optimization, no soil.

### Our positioning (verified gap)

No mod combines **per-tile soil health + composting loop + mulch + rotation + polyculture + animal management + educational framing + multiplayer correctness** in one coherent loop. ReGen Valley is the closest on soil mechanics but ships farm-wide (not per-tile) quality/yield and leans on machinery/animals rather than compost. Living Roots' differentiator remains coherence, the compounding compost loop, educational framing, and multiplayer correctness from day 1.

### Agronomic grounding (from `research/2026-10-roadmap-feasibility-and-agronomy.md`)

| Mechanic | Evidence | Consequence |
|----------|----------|-------------|
| Compost/manure | ✅ strong (long-term trials: +13% organic C, +19% N, +47% P) | Build as promised; +15 is a game abstraction |
| Mulch/soil cover | ✅ strongest (meta-analysis 90 studies: −47% runoff, −76% soil loss) | Best-supported claim in the roadmap; teach real agronomy |
| Crop rotation | ✅ strong (yield +20–25% in long trials) | Core mid-game mechanic |
| Polyculture/intercropping | 🟡 medium (yield +20–30% in tropical systems) | Scope carefully; "Three Sisters" needs new crops |
| Green manure | ✅ strong (N fixation) | Mid-game item |
| Pest control via beneficial insects | ⚠️ weak-to-negative (entomology) | Do NOT present as established agronomy |
| Beehive "beneficial insect aura" | ⚠️ weak | Treat as flavor, not science |

### Market validation

- Soil-health mods are a proven niche (ReGen Valley, Permaculture Farming, Spoilage, Spore Galore all ship and are downloaded).
- "Quality driven by soil" is an open window: ReGen delivers farm-wide at 75+/90+ avg; per-tile deterministic boost + transient feedback (our seam, specs 014/015) has no equivalent competitor with in-game verification.
- Multiplayer is mainstream (co-op is a vanilla path); Principle VI (host-authoritative, first-wins) is a real differentiator.
- Agronomic honesty (mulch = strongest evidence) is a unique educational differentiator.

### Implications for roadmap priority

1. **Harvest-quality loop (specs 014/015)** remains the 80/20: only item that makes Living Soil (71–100%) visibly pay off at harvest; infrastructure already merged.
2. **Mulch** is our strongest agronomic claim — lean on it in marketing/education.
3. **Pest control via insects** should not be presented as science; reframe or drop.

---
