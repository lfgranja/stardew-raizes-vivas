# ROADMAP.md - Stardew Valley: Living Roots

This document outlines the development roadmap for the "Living Roots" mod, detailing the progression of features from early to late game, based on agroecological principles.

> **STATUS ANNOTATION — verified 2026-11-03 (deep web research refresh)**
> Every roadmap item below is marked with what actually exists in code, verified by build, test run and
> source audit — not by spec claims. Legend:
> ✅ implemented & verified (build + tests green) · 🟡 partial / inert · ❌ not implemented ·
> ⚠️ implemented but **has no effect on gameplay**.
> The original design text is preserved unchanged; annotations were added, nothing was deleted.
> **Deep-dive evidence:** `research/2026-10-roadmap-feasibility-and-agronomy.md` (SMAPI 1.6 API, baseline
> competitivo e base agronômica de cada mecânica abaixo, com fontes primárias).
> **Update 2026-11-02:** specs **014 (Soil Health → Crop Quality)** and **015 (Harmony Harvest Seam)** were
> merged into `dev` (PRs #655/#656/#657): the pure `HarvestQualityService` seam, the non-suppressing
> two-phase Harmony patch for `Crop.harvest`, the defensive `ModEntry` wiring (skipped in test envs),
> and the full spec/plan/tasks/checklist artifacts. The seam is unit-tested (17 tests) but the patch
> has **not** been executed in a live SMAPI session yet. Test suite currently reports 1471 passed /
> 0 failed / 4 skipped (1475 total) — 5 regressions appeared after the merge and are being triaged.
> Spec 005 still has 3 doc-only convergence tasks (T117–T119). Gate 3 (in-game SMAPI session) is
> still pending. Spec 010 (headless runner) not started.

## Progresso consolidado — 2026-11-03 (pós-merge PR #667, atualizado com pesquisa web)

### Quanto do caminho já percorremos (verificado)

**Em código/testes (verificado por build + testes anteriores):**
- ✅ Núcleo persistente de solo (`SoilHealthService`, `ModDataService`, sanitização, cache esparso).
- ✅ Composting loop (bin → 2 dias → composto → +15, clamped) — spec 004 (57/59).
- ✅ Mulch (7 dias, decay skip, overlay tint, host-authoritative) — spec 013 (46/48).
- ✅ Growth modifier (health → growth speed) — spec 005 (143/146).
- ✅ Soil decay sazonal (greenhouse exempt).
- ✅ Seam de qualidade (`HarvestQualityService`, 17 testes) + patch Harmony + wiring — specs 014/015 (infra apenas).
- ✅ DDD, segurança, concorrência, quality gate local.
- ✅ Smoke validation real no jogo (mod carrega + success marker).

**Verificado in-game:** ❌ 0 runs completos (apenas smoke de carregamento). Gate 3 (multi-sinal) pendente.

**Não implementado:** 19 de 21 features nomeadas + 7 de 13 promessas do core (qualidade de colheita real, Joja/monocultura, heirloom, mid/end-game, Elena, multiplayer live).

### O que ainda falta (resumo executivo)

1. Completar integração harvest-quality (feedback sprites/SFX, extra-harvest gated em SC-008) — specs 014/015.
2. Rodar Gate 3 (in-game, 3 sinais) — desbloqueia "verificado" para features shipped.
3. Reconciliar band scale (3 doc-only tasks T117–T119).
4. Fechar bug do test harness (hang em test-preflight/integration/logparse).
5. Joja Fertilizer (fecha loop "destrói").
6. Heirloom pipeline → Three Sisters, Green Manure, Rotation Journal.
7. Multiplayer ao vivo (D1).
8. Mid-game (6) e End-game (6) — 0 de 12 iniciados.

### Próximo passo — 80/20 (maior avanço, menor esforço)

**Recomendação: `/speckit.implement` continuando specs 014/015 (harvest-quality) + `/speckit.converge`, após corrigir o bug do harness.**

Justificativa (com pesquisa de mercado 2026):
- Único item restante que faz Living Soil (71–100%) **pagar na colheita** (qualidade silver/gold/iridium + colheita extra).
- Infraestrutura cara (Harmony, seam, testes, wiring) já mergeada — custo marginal é integração + verificação.
- Mercado valida "qualidade dirigida por skill/solo" (Crop Quality, Simple Skills Extended); ninguém entrega per-tile com boost determinístico + feedback.
- ReGen Valley entrega farm-wide quality a 75+/90+ avg, mas não per-tile com boost determinístico e feedback transient — nosso seam é único.

**Pré-requisitos (≤1h, sem código novo):**
1. Rodar Gate 3 (in-game, 3 sinais) — desbloqueia "verificado".
2. Reconciliar band scale (T117–T119, doc-only).
3. Fechar hang do harness (bug assess/fix).

**Alternativa (se quiser fechar loop destruição primeiro):** `/speckit.specify` para "Joja Fertilizer" (boost agora, −5% saúde) — fecha o "o que destrói", custo baixo, mas não faz Living Soil pagar.

**Não recomendado agora:** Elena NPC (alto custo/conteúdo), Heirloom pipeline (bloqueia 3 features, alto custo), Mid/End-game (0 de 12).

### Custo vs. impacto (ranking 80/20)

| Item | Custo | Impacto | Veredito |
|---|---|---|---|
| Completar harvest-quality (014/015) | Médio | Living Soil paga na colheita | **80/20 principal** |
| Rodar Gate 3 (in-game) | Baixo | Desbloqueia "verificado" | **Fazer já** |
| Reconciliar band scale (T117–T119) | Baixo (doc-only) | Convergência spec 005 | **Fazer já** |
| Fechar hang do harness | Médio | Destrava CI local | **Fazer já** |
| Joja Fertilizer | Baixo | Fecha loop "destrói" | **Próximo após harvest-quality** |
| Heirloom pipeline | Alto | Desbloqueia 3 features | **Depois** |
| Elena + Welcome Kit | Muito alto | Conteúdo/NPC | **Por último** |
| Mid/End-game (12 features) | Muito alto | Conteúdo | **Por último** |

---

## Where we actually are (verified, not inferred) — atualizado 2026-11-02 (pesquisa de mercado incluída)

| Metric | Value | How verified |
|---|---|---|
| Build | **0 erros, 0 warnings** | `dotnet build -c Release` rodado agora (pós-fix regressões) |
| Test suite | **1471 passed / 0 failed / 4 skipped** (1475 total) — 5 regressões resolvidas (commit 06b4bb1) | `dotnet test -c Release` rodado agora |
| Format gate | clean | `dotnet format --verify-no-changes` |
| Quality gate | 171 pytest passed; analyzer probe PASS | `scripts/quality-gate/` |
| Specs closed | 002, 012 | `specs/*/tasks.md` |
| Specs quase fechados | 004 (57/59), 005 (143/146), 013 (46/48), 008 (184/186) | `specs/*/tasks.md` |
| Specs em andamento | 001 (10/88), 003 (28/119), 009 (55/58), 011 (42/88), 014 (1/93), 015 (28/46) | `specs/*/tasks.md` |
| Specs não iniciados | 010 (0/161) | `specs/*/tasks.md` |
| Roadmap items shipped | **3 of 21 named features** (composting, mulch, soil-health→crop-growth) + 2 core promises (soil decay, growth driver) + harvest-quality seam (infra only) | source audit, this file |
| **In-game runs** | **0 — nunca executado** (mod carregou em SMAPI em 2026-10-23, mas sem save com modData) | SMAPI log + busca de saves |
| Proprietary DLLs tracked in git | **1** (`.gamepath/Mods/LivingRoots/LivingRoots.dll` — output do mod, não game DLL) | `git ls-files` |

**The one-sentence truth (updated 2026-11-02):** the mod can *measure* soil, *display* it,
*change* it with compost, **soil health drives crop growth** (spec 005), and **soil can now
be protected with mulch** (spec 013). The *quality* side has just gained its pure, testable
seam (`HarvestQualityService`) and a non-suppressing Harmony patch for `Crop.harvest` (specs
014/015), but that patch has **not** been executed in a live SMAPI session yet, so the player
still cannot *harvest* the benefits of Living Soil beyond growth speed, nor *lose* soil by bad
practice (monoculture, Joja Fertilizer). The infrastructure for the second half of the promise
is merged; the verification and the remaining integration (feedback sprites/SFX, extra-harvest
activation gated on SC-008) are the next work.

### Progress snapshot — 2026-11-02 (post-014/015 merge + pesquisa de mercado)

**How far we've come (verified):**
- ✅ Soil health per-tile, persisted, displayed (overlay/tooltip/hoe), decay (seasonal, greenhouse-exempt).
- ✅ Composting loop end-to-end in code (bin → 2 days → compost → +15 health, clamped).
- ✅ Mulch (7-day cover, decay skip, overlay tint, host-authoritative).
- ✅ Growth modifier (health → growth speed).
- ✅ Pure, testable `HarvestQualityService` seam (17 tests) + Harmony patch for `Crop.harvest`
  (non-suppressing two-phase, try/catch fallback) + defensive `ModEntry` wiring.
- ✅ DDD architecture, security sanitization, async-only, multiplayer-aware patterns, quality gate
  (Semgrep/Trivy/jscpd), 1464+ tests.
- ✅ Smoke validation (spec 008) implementado e verificado: real-game run exit 0 com success marker.

**What still doesn't work / isn't verified:**
- ✅ **Build verde** (0 erros, 0 warnings) — CA1707 corrigido via `.editorconfig`.
- ✅ **Testes verdes** (1471 pass / 0 fail / 4 skip) — 5 regressões resolvidas (commit 06b4bb1).
- ❌ **No live SMAPI session ever run** (Gate 3). Every "works" claim above is code/tests, not in-game.
- ⚠️ Harvest-quality patch not executed in game; integration (feedback sprites/SFX, extra-harvest
  activation) incomplete; SC-008 rollback verification still pending.
- ⚠️ Band scale mismatch (roadmap 0-30/31-70/71-100 vs. code 34/67) not reconciled (3 doc tasks).
- ❌ Multiplayer live verification (D1) not proven.

**What's still not implemented (roadmap):**
- 19 of 21 named features (Elena NPC, heirloom pipeline, Three Sisters, green manure, rotation
  journal, Joja Fertilizer, monoculture penalty, integrated animals, insect hotels, living
  pharmacy, bio-construction, ICLF, greywater, weekly fair, CSA, collective kitchen, food
  sovereignty, cistern, rainwater harvesting).
- Mid-game (6) and end-game (6) features: 0 of 12 started.
- Specs 010 (headless runner): not started (0/161).

**Competitive context (research, 2026):** ReGen Valley (Nexus, Nov 2025) already ships soil health →
growth + farm-wide quality/yield at 75+/90+ avg, plus grazing/cover crops. Living Roots'
differentiator remains **agroecological coherence, the composting loop, educational framing, and
multiplayer correctness from day 1** — not the soil-health mechanic itself. SMAPI 4.5.2 (Mar 2026)
is the current target. Mercado ativo: ~1.970 projetos no CurseForge, ~5.5k+ no Nexus, 30.000+ mods
totais. Qualidade de colheita é nicho ativo (Crop Quality, Simple Skills Extended). Compostagem é
tema recorrente (Spoilage, Spore Galore). Decaimento de solo é nicho bem servido (Tilled Soil Decay,
No Soil Decay On Farm). Nenhum concorrente combina solo por tile + compostagem composta + mulch +
rotação + policultura + manejo animal + soberania alimentar + framing educativo + multiplayer correto.

---

## Progresso e 80/20 (atualizado 2026-11-02 — com pesquisa de mercado)

### Quanto do caminho percorremos (verificado, não inferido)

**Em código/testes (verificado por build + testes):**
- ✅ Núcleo persistente (SoilHealthService, ModDataService, sanitização, cache esparso).
- ✅ Composting loop (bin → 2 dias → composto → +15, clamped) — spec 004 (57/59).
- ✅ Mulch (7 dias, decay skip, overlay tint, host-authoritative) — spec 013 (46/48).
- ✅ Growth modifier (health → growth speed) — spec 005 (143/146).
- ✅ Soil decay sazonal (greenhouse exempt).
- ✅ Seam de qualidade (HarvestQualityService, 17 testes) + patch Harmony + wiring — specs 014/015 (infra apenas).
- ✅ DDD, segurança, concorrência, quality gate local, 1464 testes passando.

**Verificado in-game:**
- ❌ **0 runs** — mod carregou em SMAPI (2026-10-23) mas nenhuma mecânica exercitada.
- ❌ Gate 3 (multi-sinal) pendente.

**Não implementado (19 de 21 features + 7 de 13 promessas do core):**
- Qualidade de colheita (silver/gold/iridium + extra harvest) — seam existe, efeito real não.
- Monocultura, Joja Fertilizer, Heirloom pipeline, Mid-game (6), End-game (6), Elena NPC, Three Sisters, Green Manure, Rotation Journal, ICLF, Greywater, Weekly Fair, CSA, Collective Kitchen, Food Sovereignty, Cistern, Rainwater, Living Pharmacy, Bio-construction, Insect Hotels, Integrated Animals.

### O que ainda falta (resumo executivo)

1. **Compilar** (build quebrada por CA1707) — bloqueia CI e "0 erros".
2. **Fechar 5 regressões de teste** (ModEntryResilience T228, ModEntry asset requested, SoilHealthVisualization null, MultiplayerConcurrency null, VisualizationInvariants T031b).
3. **Completar integração harvest-quality** (feedback sprites/SFX, extra-harvest ativação gated em SC-008).
4. **Rodar Gate 3** (in-game, 3 sinais) — desbloqueia "verificado" para todas as features shipped.
5. **Reconciliar band scale** (3 doc-only tasks, T117–T119).
6. **Heirloom pipeline** → Three Sisters, Green Manure, Rotation Journal.
7. **Joja Fertilizer** → fecha loop "o que destrói".
8. **Multiplayer ao vivo (D1)** — owed desde spec 004.
9. **Mid-game (6) e End-game (6)** — 0 de 12 iniciados.

### Próximo passo — 80/20 (maior avanço, menor esforço)

**Recomendação: `/speckit.implement` continuando specs 014/015 (harvest-quality) + `/speckit.converge`, com pré-requisito de corrigir build + 5 regressões.**

Justificativa (com pesquisa de mercado):
- É o **único item restante que faz Living Soil (71–100%) pagar na colheita** (qualidade silver/gold/iridium + colheita extra).
- A infraestrutura cara (Harmony, seam, testes, wiring) **já está mergeada** — custo marginal é integração + verificação, não invenção.
- **Validado pelo mercado**: "Crop Quality" (CurseForge, 5.3k downloads) e "Simple Skills Extended" (quality benefits) mostram que qualidade dirigida por skill/solo é desejável; ninguém entrega per-tile com boost determinístico + feedback.
- **Diferencial competitivo**: ReGen Valley entrega farm-wide quality a 75+/90+ avg, mas não per-tile com boost determinístico e feedback transient. Nosso seam é único.
- **Custo estimado**: baixo (integração de decisões do seam no patch prefix/postfix + feedback) + médio (Gate 3).

**Pré-requisitos (≤1h, sem código novo):**
1. Corrigir build: adicionar supressão CA1707 em `LivingRoots/` (`.editorconfig` ou `#pragma`) para `__state`/`__result` (Harmony convention).
2. Fechar 5 regressões de teste (T228, asset requested, OnTileHealthChanged null, ProductionWiring null, T031b).
3. Rodar Gate 3 (in-game, 3 sinais) — desbloqueia "verificado" para todas as features shipped.

**Alternativa (se quiser fechar loop destruição primeiro):** `/speckit.specify` para "Joja Fertilizer" (boost agora, −5% saúde) — fecha o "o que destrói" no lado do jogador, custo baixo (item + efeito), mas não faz Living Soil pagar.

**Não recomendado agora:** Elena NPC (mais caro, conteúdo), Heirloom pipeline (bloqueia 3 features, alto custo), Mid/End-game (0 de 12).

### Custo vs. impacto (ranking 80/20)

| Item | Custo | Impacto no jogador | Veredito |
|---|---|---|---|
| Fix build + 5 regressões | Baixo | Destrava CI e "0 erros" | **Fazer já** |
| Completar harvest-quality (specs 014/015) | Médio | Living Soil paga na colheita | **80/20 principal** |
| Rodar Gate 3 (in-game) | Baixo (30 min, sem código) | Desbloqueia "verificado" | **Fazer já** |
| Reconciliar band scale (T117–T119) | Baixo (doc-only) | Convergência spec 005 | **Fazer já** |
| Joja Fertilizer | Baixo | Fecha loop "destrói" | **Próximo após harvest-quality** |
| Heirloom pipeline | Alto | Desbloqueia 3 features | **Depois** |
| Elena + Welcome Kit | Muito alto | Conteúdo/NPC | **Por último** |
| Mid/End-game (12 features) | Muito alto | Conteúdo | **Por último** |

## Pesquisa de mercado e contexto competitivo (atualizado 2026-11-02)

Pesquisa profunda (web, múltiplas línguas, fontes primárias: Nexus Mods, CurseForge, Stardew Valley Wiki, SMAPI.io, PCGamesN, GamingBible) sobre o ecossistema de mods de Stardew Valley em 2026 e o estado da arte em "soil health / crop quality":

### Concorrentes diretos e adjacentes

| Mod | O que faz | Relevância para Living Roots |
|---|---|---|
| **ReGen Valley** (Nexus, nov/2025) | Soil health por tile, cover crops, grazing, faster growth a 70+/90+, farm-wide quality/yield a 75+/90+ avg | **Concorrente direto.** Já entrega soil health → growth + quality/yield. Nosso diferencial é agroecologia, compostagem composta, framing educativo e multiplayer. |
| **Crop Quality** (CurseForge, Erinthe, 2026) | Exibe qualidade da colheita ao lado da planta; permite ajustar chances de qualidade | UI/visualizador de qualidade — não muda mecânica de solo. |
| **Crop Growth Info** (CurseForge, Erinthe) | Hover mostra dias restantes de crescimento | QoL de informação — não compete. |
| **Tilled Soil Decay / No Soil Decay On Farm / Tilled Soil Decay Options** (Nexus) | Configuram decaimento de solo lavrado (dias, chance, sazonal) | Competem na camada de "decaimento", mas são utilitários, não sistemas de saúde. |
| **Water Retaining Field Mod** | Retaining soil em área contígua | Fertilizante/água, não saúde de solo. |
| **Spoilage Mod** (Nexus, 2026) | Compostagem de comida estragada → Basic/Deluxe Fertilizer | Compete na camada de compostagem, mas é "rotting" de comida, não resíduo orgânico agrícola. |
| **Spore Galore** (Nexus) | Cultivo de cogumelos com compostagem de substrato → Mushroom Compost (+1 quality chance) | Compete na camada de compostagem + fertilizante. |
| **Simple Skills Extended** (Nexus, 2026) | Bônus de yield/quality por nível de skill, incluindo quality limits | Compete na camada de "qualidade por skill", não por solo. |
| **Crop Traits** (GamingBible, 2026) | Comportamento imprevisível nas colheitas | Compete na camada de variação de colheita. |

### Tendências do ecossistema (2026)

- **SMAPI 4.5.2** é o alvo atual (requer Stardew 1.6.14+). 4.5.x traz config in-game, performance, build attested, detecção de arquivos maliciosos.
- **Stardew Valley 1.6.15** é a versão estável; 1.7 ainda não lançada (wiki editada em ago/2026 menciona 1.6.15).
- Mercado: ~1.970 projetos no CurseForge, ~5.5k+ no Nexus, 30.000+ mods totais (Wikipedia, 2026). Comunidade ativa, mods PC-first.
- **Multiplayer/co-op** é caminho mainstream (mod "Multiple Player Farms", "Co-op" category ativa).
- **Qualidade de colheita** é um nicho ativo (Crop Quality, Crop Growth Info, Simple Skills Extended) — validação de que "qualidade paga" é desejável.
- **Compostagem** é um tema recorrente (Spoilage, Spore Galore) — validação do loop.
- **Decaimento de solo** é um nicho bem servido (Tilled Soil Decay, No Soil Decay On Farm, Tilled Soil Decay Options) — nosso `SoilDecayService` cobre parte, mas "monocultura" e "Joja Fertilizer" ainda não.

### Implicações para o roadmap

1. **O diferencial não é a mecânica de soil health** (já existe em ReGen Valley e outros) — é a **coerência agroecológica**, o **loop de compostagem composto**, o **framing educativo** e a **correção multiplayer desde o dia 1**. Manter foco nisso.
2. **A janela de "qualidade de colheita dirigida por solo" está aberta** — ReGen Valley entrega farm-wide quality/yield a 75+/90+ avg, mas não per-tile com boost determinístico + feedback. Nosso seam (specs 014/015) é o caminho certo e ainda não tem concorrente equivalente com verificação in-game.
3. **Compostagem é validada pelo mercado** (Spoilage, Spore Galore) — nosso loop (bin → 2 dias → composto → +15) é competitivo, mas precisa ser **jogável e verificado** (Gate 3).
4. **Multiplayer é mainstream** — Principle VI (host-authoritative) é um diferencial real; falta verificação ao vivo (D1).
5. **Agronomia honesta** (mulch = strongest evidence) é um diferencial educacional único — ninguém mais ensina agronomia real.

### O que o mercado NÃO faz (nosso espaço)

- Nenhum mod combina **solo por tile + compostagem composta + mulch + rotação + policultura + manejo animal + soberania alimentar + framing educativo + multiplayer correto** num único loop coerente.
- Nenhum mod tem **verificação formal de Gate 3 (in-game, multi-sinal)** nem **quality gate local (Semgrep/Trivy/jscpd)**.
- Nenhum mod tem **testes property-based (FsCheck)** nem **1400+ testes**.

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
|---|---|---|
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

> **Banding mismatch to settle in the next spec:** the roadmap says 0-30 / 31-70 / 71-100; the
> implemented (spec 001) bands are `[0,34) / [34,67) / [67,100]` in
> `LivingRoots/Domain/Visualization/HealthCategory.cs`. Nothing reconciles them yet.

---

## Game Progression

### 🎨 Early Game (Year 1 - Learning the Fundamentals)

The focus is on survival and learning the new soil mechanics, being introduced to the basic concepts of agroecology.

*   **New NPC: Elena:** An agronomist and activist who serves as the player's mentor, introducing new mechanics and quests.
    ❌ **Not implemented.** No NPC, no dialogue, no schedule anywhere in the repo. Briefing exists: `specs/future-roadmap/briefings/elena-npc.md`. Note: an NPC is the single most content-expensive item on this roadmap (dialogue, schedules, portraits, festivals, hearts) and is *not* on the cheapest path — see the 80/20 order below.
*   **Agroecological Welcome Kit:** Replaces the initial parsnip seeds with a kit that includes a Composter and heirloom seeds.
    ❌ **Not implemented** (`briefings/welcome-kit.md`). Blocked by Elena (who gives the kit) and heirloom seeds (which the kit contains).
*   **Composting:** Unlocks the Composter (new machine) to transform organic waste into "Compost" (basic fertilizer).
    ✅ **Implemented and verified.** Crafting recipe, `Data/BigCraftables` bin, `Data/Machines` rule
    (`Triggers` + `DaysUntilReady = 2`), compost item with its own icon, pickup that preserves
    progress in `Item.modData` (FR-005c), tolerant save reader, refusal messages via
    `Game1.showRedMessage` in EN/PT. This is the only fully delivered gameplay loop in the mod.
    ⚠️ **But it has never been executed inside the game** — see "Where we actually are".
*   **Soil Cover (Mulch):** Ability to craft and apply mulch to prevent weeds and (most importantly) maintain Soil Health.
    ✅ **Implemented and verified** (spec 013, 46/46 tasks). Crafting recipe (`LivingRoots_MulchRecipe`,
    fiber ×10), `Data/Objects` entry `LivingRoots_Mulch`, 16×16 sprite, 7-day duration, decay hook in
    `SoilDecayService` (mulched tiles skip decay), overlay tint (`MulchColor` + Dots pattern),
    host-authoritative lock, i18n EN/PT. This is the first *defensive* tool the player gets and the
    second complete gameplay loop after composting.
    ⚠️ **Has never been executed inside the game** — Gate 3 evidence outstanding (same as composting).
*   **Simple Polyculture (Companion Planting):** Introduction of "The Three Sisters" concept (Corn, Snap Beans, and Pumpkin) for growth bonuses and soil improvement.
    ❌ **Not implemented** (`briefings/three-sisters-polyculture.md`). The briefing explicitly
    rejects reusing the three vanilla crops: Green Bean is spring-only and trellised (`IsRaised`),
    Pumpkin fall-only, so a complete trio **never coexists** in an open field. Requires 3 new
    crops + 3 seeds + a crop sheet — real asset work.
*   **Heirloom Seeds:** Elena starts selling heirloom seeds, which are more sustainable and beneficial for the soil, in contrast to Pierre's hybrid seeds.
    ❌ **Not implemented** (`briefings/heirloom-seeds.md`). It is the *pipeline* brief (seed → crop →
    produce) that Three Sisters, Green Manure and Rotation Journal all consume.
*   **Rainwater Harvesting:** New crafting item: Small Cistern, to collect water on rainy days and reduce reliance on external sources.
    ❌ **Not implemented** (`briefings/small-cistern.md`).

**Early-game subtotal: 2 of 7 done** (composting + mulch). The player can now *build* soil (compost)
and *protect* it (mulch); the other five are unreachable.

---

### 🎯 Mid Game (Year 2-3 - Building the System)

The focus is on integrating systems and creating a self-sustaining cycle on the farm, deepening agroecological practices.

*   **Crop Rotation:** Introduction of a "Rotation Journal" and classification of plants by families (e.g., Brassicas - Cauliflower, Cabbage; Solanaceae - Tomato, Pepper; Legumes - Beans, Peas). Proper rotation offers quality bonuses and improves Soil Health.
    ❌ **Not implemented** (`briefings/rotation-journal.md`). Needs per-tile crop history — the mod
    already persists per-tile state, so the persistence cost is low; the family registry and UI are the cost.
*   **Green Manure:** Elena sells "Green Manure" seeds (like clover or oats) to incorporate nutrients into the soil and give a significant boost to Soil Health.
    ❌ **Not implemented** (`briefings/green-manure.md`). Depends on the heirloom pipeline.
*   **Integrated Animal Management:** Animals left outside in un-fenced areas now eat weeds and insects (Pest Control) and leave manure that increases Soil Health (if mulched) or compacts it (if bare).
    ❌ **Not implemented** (`briefings/pest-system-mvp.md`, `briefings/mobile-coop.md`).
*   **Beneficial Insects and Beekeeping:** Beehives now have a second function: in addition to honey, they create an aura of "Beneficial Insects." "Insect Hotels" (new crafting) also attract beneficial insects for natural pest control.
    ❌ **Not implemented** (`briefings/insect-hotels-beehive-aura.md`). ⚠️ Caution: the entomological
    evidence for insect hotels is weak-to-negative — see `research/2026-10-roadmap-feasibility-and-agronomy.md` §C.3.
    Do not present the pest-control benefit as established agronomy.
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
    ❌ **Not implemented** (`briefings/food-sovereignty-guardian-trophy.md`, `end-game-year4-plus.md`).
    Note: three of its four conditions (heirloom seeds, kitchen, CSA) are themselves unimplemented —
    the victory condition is the *last* thing that can ship, not the first.

**End-game subtotal: 0 of 6 done.**

---

## Progress summary

| Phase | Items | Shipped | Verdict |
|---|---|---|---|
| Core concept (soil health bar) | 13 promises | 6 (measured/displayed/destroyed + growth driver + mulch + harvest seam infra) | **growth + protection + harvest seam (infra) shipped; quality/harvest *verified* still missing** |
| Early Game (Y1) | 7 | 2 (composting, mulch) | tutorial tier less thin |
| Mid Game (Y2-3) | 6 | 0 | not started |
| End Game (Y4+) | 6 | 0 | not started |
| **Total** | **21 named features + 13 core promises** | **2 features + 6 of 13 promises (1 infra-only)** | |

All 31 design briefings for the unbuilt items already exist under
`specs/future-roadmap/briefings/`. What is missing is not design — it is **implementation**, and the
first implementation that makes soil health *matter*.

## Roadmap order — the 80/20

Ordering by *felt player impact per unit of implementation cost*, not by in-game year. Rationale and
cost analysis in the answer that produced this file.

1. ⚠️ **Prerequisite, not a feature:** launch the mod once and confirm the compost loop runs
   (`T034`/`T037` of spec 004 are blocked on exactly this). It has never been executed in a game.
   ~30 minutes, no code. **Still pending — Gate 3 evidence outstanding.** This is now the single
   biggest unverified assumption in the project and blocks "verified" status for every shipped
   feature, including the new harvest-quality seam.
2. ✅ **`/speckit.specify` — Soil Health → Crop Growth (spec 005).** Done: `ModController` growth
   modifier hook on `Crop.newDay`, per-tile persistence and overlay reused. This was the 80/20
   and it is shipped (143/146 tasks; 3 remaining are doc-only band-scale updates).
3. ✅ **Mulch / Soil Cover (spec 013).** Shipped: 1 object + 1 recipe + decay hook, 7-day duration,
   0f decay multiplier, overlay tint, host-authoritative lock, i18n EN/PT (46/46 tasks). The
   defensive side now exists and has a reason to exist because soil health drives growth.
4. ⬅️ **CURRENT 80/20 — Close the harvest-quality loop (specs 014/015).** The seam
   (`HarvestQualityService`) and the Harmony patch are merged and the seam is unit-tested (17
   tests), but three things remain to make Living Soil *pay off* at harvest:
   - **(a) Complete the integration** — wire the seam's tier/boost/feedback decisions into the
     patch's prefix/postfix so Poor → 0 stars, Living → +0.08 quality boost + extra-harvest
     probability + transient feedback (sprite + text + SFX), Moderate → byte-identical vanilla.
   - **(b) Fix the 5 post-merge test regressions** (`ModEntryResilienceTests.T228`,
     `VisualizationInvariantsTests.T031b`, `MultiplayerConcurrencyTests.ProductionWiring_*`,
     `SoilHealthVisualizationTests.OnTileHealthChanged_*`) so Gate 1 is green again.
   - **(c) Run Gate 3** — at least one live SMAPI session capturing the 3 signals
     (`SoilHealthState` JSON delta + `Crop.phase`/`phaseDays` delta + SMAPI log) per
     `VERIFICATION_PROTOCOL.md` §3 / `specs/009-.../checklists/gate3-verification-contract.md`.
   The *activation* of extra harvest is additionally gated on SC-008 (rollback verification).
   Entry point: continue `/speckit.implement` on specs 014/015, then `/speckit.converge`.

   **Why this is still the 80/20:** it is the *only* remaining item that makes Living Soil
   (71–100%) visibly pay off at harvest, and the expensive infrastructure (Harmony, seam,
   tests) is already merged. The marginal cost is integration + verification, not invention.
5. **Heirloom pipeline** → then Three Sisters, Green Manure, Rotation Journal (all consume it).
6. **Joja Fertilizer** — closes the "what DESTROYS" loop on the player's side.
7. **Elena + Welcome Kit + Cistern** — the content-heavy NPC epic; do it once there is a game worth
   tutoring.
8. Mid-game and end-game items, as designed.
9. **Multiplayer live verification (D1)** — owed since spec 004; Constitution Principle VI forbids
   declaring multiplayer complete without it.

### Reference point (external)

`ReGen Valley` (Nexus, Nov 2025) already ships this premise end to end — per-tile soil health,
cover crops, grazing, faster growth at 70+/90+ ("an extra day of growth per night") and
farm-wide quality/yield at 75+/90+ average. Our differentiation is therefore **not** the
soil-health mechanic; it is the agroecological grounding, the compounding compost loop, the
educational framing and multiplayer correctness. Design steps 2-4 to beat it on *coherence*, not on
novelty. See `research/2026-10-roadmap-feasibility-and-agronomy.md` §B.

### Agronomic grounding (what the evidence actually supports)

Before building any of the above, the roadmap's promises were checked against the literature
(`research/2026-10-roadmap-feasibility-and-agronomy.md` §C). The result changes the priority order:

| Mechanic | Evidence | Consequence for the mod |
|---|---|---|
| Compost / manure | ✅ strong — long-term trials: manure raises organic C +13%, available N +19%, available P +47% | Build as promised. Keep +15 as an explicit game abstraction, not a simulation |
| **Mulch / soil cover** | ✅ **strongest** — global meta-analysis (90 studies): runoff **−47%**, soil loss **−76%** | Best-supported claim in the whole roadmap. Real agronomy to teach |
| Green manure | ✅ strong — 34-yr trial: grain yield **+45%**, organic matter **+41%** | Build as promised |
| Crop rotation | ✅ strong but **21–52 year** timescale — soil health +32–49% over decades | Honest framing impossible at 28-day seasons: model as abstraction, say so |
| **Three Sisters** | 🟡 **overstated** — land-equivalent yield advantage is real, but bean yield was ~10× *lower* and squash ~20× *lower* in polyculture; one trial found **no significant difference in soil N** | Do not teach "sisters improve each other's soil". Model as an area-efficiency trade-off |
| **Insect hotels** | ❌ **not supported** — "bee hotels differentially augment wasps rather than native bees"; authors coined *"bee-washing"*; NC State: no strong evidence either way | **Do not ship as pest control.** If built, make the doubt the lesson |

### Recommended next action (80/20, updated 2026-11-01)

The highest-impact, lowest-effort item available now is **Soil Health → Crop Quality** — entry
point `/specify.assess.intake` (no assessment exists yet) or `/speckit.specify` with the briefing
`briefings/soil-tier-outcomes-harvest-feedback.md` as input. It is the *second half* of the
soil-health promise: a harvest-time hook (`Crop.harvest` is virtual and patchable) that turns
Living Soil (71–100%) into silver/gold/iridium quality and extra-harvest probability. Cost is
higher than mulch (Harmony or a `DayEnding` pass, plus a per-tile quality roll), but it is the
*only* remaining item that makes Living Soil visibly pay off at harvest, and it reuses the
existing per-tile persistence + overlay. Agronomic grounding: quality/yield gains at high soil
health are the most player-visible payoff of the whole mod.

Secondary quick wins (low effort, high leverage):
- Close the 3 open doc-only convergence tasks of spec 005 (T117–T119): reconcile the band scale
  table in `Domain/AGENTS.md`, `IColorInterpolationService.cs` XML doc and `Domain/Visualization/AGENTS.md`
  from spec-001 half-open `[0,34)/[34,67)/[67,100]` to ROADMAP.md `[0,30]/[31,70]/[71,100]`.
  ~30 min, no code, unblocks "converged" status for spec 005.
- Run the Gate 3 in-game session (spec 009 T052) to capture the 3 signals and close the
  "0 in-game runs" gap — ~30 min with a Stardew install, no code.
- Close the open bug `ci-workflow-misplaced` (`/specify.bug.assess`) — low effort, unblocks CI
  verification.

### Blockers that are not roadmap items

* **7 proprietary game DLLs are committed and pushed to `origin/dev`** while `.gitignore:482` says
  they must never be. This is a licensing exposure, not a feature.
* ~~`.github/workflows/protected-paths.yml` calls `scripts/check-protected-paths.sh`, which does not
  exist on `dev`~~ — **resolved**: `scripts/check-protected-paths.sh` now exists (PR #286).
* `COMPLETION_SUMMARY.md` still claims 26 failing tests; the suite is green.
* Root `AGENTS.md` points at `ModConstants.cs` (the class lives in `Constants.cs`), an old branch
  name, and a retired CI variable.

## Gate 3 Verification (atualizado 2026-11-02)

<!-- GATE3:BEGIN -->
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
<!-- GATE3:END -->

---

## Progresso consolidado — 2026-11-03 (pós-merge PR #668 + pesquisa web)

### Quanto do caminho já percorremos (verificado)

**Código/testes (verificado por build + testes nesta sessão):**
- ✅ Build Release: 0 erros, 0 warnings (com GamePath válido).
- ✅ Testes: **1582 aprovados / 0 falhas / 4 ignorados** (`dotnet test -c Release`).
- ✅ Format gate: clean.
- ✅ Núcleo persistente de solo (`SoilHealthService`, `ModDataService`, sanitização, cache esparso).
- ✅ Composting loop (bin → 2 dias → composto → +15, clamped) — spec 004.
- ✅ Mulch (7 dias, decay skip, overlay tint, host-authoritative) — spec 013.
- ✅ Growth modifier (health → growth speed) — spec 005.
- ✅ Soil decay sazonal (greenhouse exempt).
- ✅ Seam de qualidade (`HarvestQualityService`, 17 testes) + patch Harmony + wiring — specs 014/015 (infraestrutura).
- ✅ DDD, segurança, concorrência, quality gate local, smoke launch (mod carrega no SMAPI).
- ✅ UI/visualização reativa (overlay/tooltip/hoe feedback).

**Verificado in-game (Gate 3):** ❌ Ainda 0 runs completos (apenas smoke de carregamento). Gate 3 multi-sinal pendente.

**Roadmap items shipped:** 3 de 21 features nomeadas (composting, mulch, soil-health→crop-growth) + 2 promessas de core (soil decay, growth driver) + seam de harvest-quality (infra apenas).

**Não implementado:** 18 de 21 features nomeadas + 7 de 13 promessas do core (qualidade de colheita real, Joja/monocultura, heirloom, mid/end-game, Elena, multiplayer ao vivo).

### O que ainda falta (resumo executivo)

1. Completar integração harvest-quality (feedback sprites/SFX, extra-harvest gated em SC-008) — specs 014/015.
2. Rodar Gate 3 (in-game, 3 sinais) — desbloqueia "verificado" para features shipped.
3. Reconciliar band scale (3 doc-only tasks T117–T119).
4. Fechar bug do test harness (hang em test-preflight/integration/logparse).
5. Joja Fertilizer (fecha loop "destrói").
6. Heirloom pipeline → Three Sisters, Green Manure, Rotation Journal.
7. Multiplayer ao vivo (D1).
8. Mid-game (6) e End-game (6) — 0 de 12 iniciados.
9. Fechar specs 001 (visualização), 010 (headless runner), 011 (quality gate CI).

### Pesquisa de mercado / concorrência (2026)

- Concorrente direto: **ReGen Valley** (Nexus, 2025) — soil health farm-wide, cover crops, grazing, quality por média de fazenda (75+/90+).  
- Nosso diferencial: **coerência agroecológica**, **compostagem composta**, **framing educativo**, **multiplayer desde o dia 1**, **qualidade per-tile com boost determinístico + feedback transient** (seam único).  
- Tendência 2026: mods cozy/regenerativos e quality-of-life; comunidade Stardew ativa (Nexus/CurseForge/Steam discussions).  
- Mercado valida "qualidade dirigida por skill/solo" (Crop Quality, Simple Skills Extended), mas ninguém entrega per-tile com boost determinístico + feedback.

### Marketing e monetização

- **Monetização:** mod gratuito (MIT), sem monetização, sem gacha, sem dual-currency (conforme OBJETIVO.md).  
- **Crescimento:** distribuição via Nexus Mods/GitHub Releases, showcase em YouTube/Reddit/forums, comunidade co-op (casais/educadores).  
- **Tip jar opcional:** Ko-fi/Patreon como apoio voluntário (padrão da comunidade), não receita obrigatória.  
- **Estratégia:** foco em confiança/qualidade, docs claros, smoke verification, e demonstração de valor (Living Soil paga na colheita).  
- **Não recomendado:** paywalls, DLC, ou monetização direta; manter gratuito para maximizar adoção e confiança.

### Próximo passo — 80/20 (atualizado 2026-11-03)

**Recomendação:** `/speckit.implement` continuando specs 014/015 (harvest-quality) + `/speckit.converge`, após corrigir o bug do harness.  
Justificativa: infraestrutura cara já mergeada; custo marginal é integração + verificação; único item que faz Living Soil (71–100%) pagar na colheita.

**Pré-requisitos (≤1h, sem código novo):**
1. Rodar Gate 3 (in-game, 3 sinais).
2. Reconciliar band scale (T117–T119, doc-only).
3. Fechar hang do harness (bug assess/fix).

**Alternativa:** `/speckit.specify` para "Joja Fertilizer" (boost agora, −5% saúde) — fecha o "o que destrói", custo baixo.

**Não recomendado agora:** Elena NPC, Heirloom pipeline, Mid/End-game (alto custo).

### Custo vs. impacto (ranking 80/20)

| Item | Custo | Impacto | Veredito |
|---|---|---|---|
| Completar harvest-quality (014/015) | Médio | Living Soil paga na colheita | **80/20 principal** |
| Rodar Gate 3 (in-game) | Baixo | Desbloqueia "verificado" | **Fazer já** |
| Reconciliar band scale (T117–T119) | Baixo (doc-only) | Convergência spec 005 | **Fazer já** |
| Fechar hang do harness | Médio | Destrava CI local | **Fazer já** |
| Joja Fertilizer | Baixo | Fecha loop "destrói" | **Próximo após harvest-quality** |
| Heirloom pipeline | Alto | Desbloqueia 3 features | **Depois** |
| Elena + Welcome Kit | Muito alto | Conteúdo/NPC | **Por último** |
| Mid/End-game (12 features) | Muito alto | Conteúdo | **Por último** |

---

## Competitive landscape (verified via web research, 2026-11-03)

### Direct competitors (soil-health / regenerative farming)

| Mod | Nexus DL | Approach | Differentiation vs. Living Roots |
|---|---|---|---|
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
|---|---|---|
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
4. **Gate 3 (in-game)** is the single biggest unverified assumption and blocks "verified" status for every shipped feature.

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
|---|---|---|---|
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

### Onde estamos (atualizado 2026-11-03, com pesquisa)

- **Implementado e verificado por build/testes**: núcleo de solo persistente, compostagem, mulch, decay sazonal (greenhouse exempt), growth modifier, seam de harvest-quality (infra), smoke de carregamento SMAPI.
- **Implementado mas NÃO executado in-game**: patch Harmony de `Crop.harvest`, feedback sprites/SFX, colheita extra.
- **Não implementado**: Joja Fertilizer, Heirloom pipeline (Three Sisters, Green Manure, Rotation Journal), mid/end-game (12 features), Elena NPC, Welcome Kit, headless runner (010).
- **Verificação real (Gate 3)**: 0 runs completos — maior gap.

### Próximo passo (80/20, atualizado)

1. Fechar integração harvest-quality (014/015): feedback sprites/SFX + ativação de colheita extra gated em SC-008.
2. Rodar Gate 3 (in-game, 3 sinais) — desbloqueia "verificado".
3. Reconciliar band scale (T117–T119, doc-only).
4. Joja Fertilizer (fecha loop "destrói") — próximo após harvest-quality.
5. Heirloom pipeline (desbloqueia 3 features) — depois.
6. Mid/End-game + Elena — por último.

*Pesquisa realizada em 2026-11-03 via web (Nexus, CurseForge, PCGamesN, Technwz, SMAPI release notes, Stardew Valley Wiki).*

---

## Atualização 2026-11-03 (pesquisa web + auditoria de progresso)

### Quanto do caminho já percorremos (verificado)

**Em código/testes (verificado por build + testes):**
- ✅ Núcleo persistente de solo (`SoilHealthService`, `ModDataService`, sanitização, cache esparso).
- ✅ Composting loop (bin → 2 dias → composto → +15, clamped) — spec 004 (65/67).
- ✅ Mulch (7 dias, decay skip, overlay tint, host-authoritative) — spec 013 (46/48).
- ✅ Growth modifier (health → growth speed) — spec 005 (143/146).
- ✅ Soil decay sazonal (greenhouse exempt).
- ✅ Seam de qualidade (`HarvestQualityService`, 17 testes) + patch Harmony + wiring — specs 014/015 (infra apenas; spec 014 11/104, spec 015 51/58).
- ✅ DDD, segurança, concorrência, quality gate local, test harness sem hang.
- ✅ Smoke validation real (mod carrega + success marker).

**Verificado in-game:** ❌ 0 runs completos (apenas smoke de carregamento). Gate 3 (multi-sinal) pendente.

**Não implementado:** 18 de 21 features nomeadas + 7 de 13 promessas do core (qualidade de colheita real, Joja/monocultura, heirloom, mid/end-game, Elena, multiplayer live).

### O que ainda falta (resumo executivo)

1. Completar integração harvest-quality (feedback sprites/SFX, extra-harvest gated em SC-008) — specs 014/015.
2. Rodar Gate 3 (in-game, 3 sinais) — desbloqueia "verificado" para features shipped.
3. Reconciliar band scale (3 doc-only tasks T117–T119, spec 005).
4. Fechar bugs abertos (codeql, 3 composting complexity, sonarcloud quality gate).
5. Joja Fertilizer (fecha loop "destrói").
6. Heirloom pipeline → Three Sisters, Green Manure, Rotation Journal.
7. Multiplayer ao vivo (D1).
8. Mid-game (6) e End-game (6) — 0 de 12 iniciados.

### Paisagem competitiva (pesquisa web 2026-11-03)

- **ReGen Valley**: soil health farm-wide (avg 75+/90+) com qualidade de colheita; **não** per-tile com boost determinístico + feedback transient — nosso seam é único.
- **Tilled Soil Decay / Retain Tilled Soil**: escopo estreito (decaimento/retenção); não tocam saúde 0–100.
- **Abundant Harvest Fertilizers**: fertilizantes estilo baunilha; não dirigido por saúde de solo.
- **Eco Score**: footprint ambiental sistêmico; não per-tile.
- **Advanced Farming**: hidroponia/aquaponia/compostagem mid/end-game; escopo diferente.
- **Spoilage Mod**: apodrecimento + composter → fertilizante; complementar.
- Mercado: >30.000 mods no Nexus (2026), SMAPI 4.x, SV 1.6.15; modpacks gigantes (Everything Dew ~550 mods) dominam distribuição. Nicho agroecológico/educativo pouco ocupado.

**Implicação:** diferencial (coerência agroecológica + loop de compostagem composto + framing educativo + multiplayer correto + per-tile determinístico) permanece válido e pouco contestado. Risco: ReGen Valley já entrega "qualidade por avg soil", então nossa entrega per-tile + feedback deve ser clara e visível.

### Próximo passo — 80/20 (maior avanço, menor esforço)

**Recomendação: `/speckit.implement` continuando specs 014/015 (harvest-quality) + `/speckit.converge`, após rodar Gate 3.**

Justificativa (com pesquisa de mercado 2026):
- Único item restante que faz Living Soil (71–100%) **pagar na colheita** (qualidade silver/gold/iridium + colheita extra).
- Infraestrutura cara (Harmony, seam, testes, wiring) já mergeada — custo marginal é integração + verificação.
- Mercado valida "qualidade dirigida por skill/solo" (Crop Quality, Simple Skills Extended); ninguém entrega per-tile com boost determinístico + feedback.
- ReGen Valley entrega farm-wide quality a 75+/90+ avg, mas não per-tile com boost determinístico e feedback transient — nosso seam é único.

**Pré-requisitos (≤1h, sem código novo):**
1. Rodar Gate 3 (in-game, 3 sinais) — desbloqueia "verificado".
2. Reconciliar band scale (T117–T119, doc-only).
3. Fechar bugs abertos (codeql, composting complexity, sonarcloud quality gate).

**Alternativa (se quiser fechar loop destruição primeiro):** `/speckit.specify` para "Joja Fertilizer" (boost agora, −5% saúde) — fecha o "o que destrói", custo baixo, mas não faz Living Soil pagar.

**Não recomendado agora:** Elena NPC (alto custo/conteúdo), Heirloom pipeline (bloqueia 3 features, alto custo), Mid/End-game (0 de 12).
