# Idea Intake: Heirloom Seeds / Sementes Crioulas Pipeline (next 80/20 gameplay slice)

- **Slug**: heirloom-seeds-pipeline
- **Created**: 2026-11-09
- **Source**: repo path — `ROADMAP.md` ("Recommended next action (80/20, refreshed 2026-11-09)", item 6), `OBJETIVO.md` (§8 item 6), `specs/future-roadmap/briefings/heirloom-seeds.md` (Epic 6 — US-06-01 #34 / US-06-02 #35), `LivingRoots/docs/EPICS_USER_STORIES.md`
- **Type**: new-capability

## Idea (as captured)

> **Refreshed recommendation, `ROADMAP.md` / `OBJETIVO.md` (2026-11-09), item 6:**
> "Próximo slice de gameplay: `pipeline de **heirloom seeds**` (spec stub; consumido por Three Sisters, Green Manure, Rotation Journal, CSA) — maior impacto/80/20 do conteúdo restante; Joja/monocultura completa permanece fora de escopo controlado até decisão explícita."
>
> **Briefing (`specs/future-roadmap/briefings/heirloom-seeds.md`), concept:** Sementes crioulas vendidas pela Elena × híbridas do Pierre — contraste narrativo central do Y1. Mecânica: **+Soil Health no plantio/colheita**, crescimento levemente mais lento (tradeoff), qualidade maior em solo vivo. Escopo Y1: **3 culturas** — 1 parsnip heirloom (primavera) + 1 verão + 1 outono. Three Sisters (milho/feijão/abóbora, Epic 3 #28/#29) reusam este pipeline depois. Regra proposta (spec trava): plantio heirloom `+2` soil health; colheita em solo Healthy `+1` + chance extra (stack com `ExtraHarvestChance`); híbridas Pierre neutras ou `−1`.
>
> **Briefing, shape:** 3× seed item (16×16), 3× produce item (16×16), 1 crop sheet (`Assets/Crops/heirloom_crops.png`), registro via `ModEntry.OnAssetRequested` (Data/Objects + Data/Crops — primeiro uso de Data/Crops), loja Elena (`LivingRoots_ElenaShop`) com desconto por amizade, i18n EN/PT, constantes em `ModConstants`, colheita heirloom `compostable_item` (`OrganicWasteValidator` categoriais −74/−75/−79/−80/−81).
>
> **Dependências declaradas no briefing:** Elena NPC (`specs/future-roadmap/briefings/elena-npc.md`, não implementado), Welcome Kit, Green Manure, Three Sisters, Rotation Journal, CSA — todos consomem este pipeline.

**Related in-repo context (captured, not endorsed here):**
- O briefing propõe Elena como canal de distribuição ("Elena shop… Pierre continua vendendo híbridas"). **Elena NPC não existe no código** — sem NPC, sem loja. Pergunta de viabilidade central.
- O briefing pede assets novos (3 seeds + 3 produce + 1 crop sheet, sprites 16×16/9-slice). Custo real de conteúdo.
- Regras de solo propostas (`+2` no plantio, `+1` na colheita em Healthy) não existem ainda como hooks; o mod tem hook de `Crop.newDay` (spec 005) e seam Harmony de colheita (spec 014/015), e `HoeDirt.plant` (1.6) é um seam verificado por decompilação nesta sessão (`plant(string itemId, Farmer who, bool isFertilizer)`, instance).
- Constituição Princípio VI: nada pode ser desenhado single-player-only; estado farm-keyed, host-authoritative, first-wins.
- Mercado/ecossistema re-verificado 2026-11-09 (esta sessão): nenhum mod de heirloom/crioulo/agroecologia existe em nenhum idioma; consórcio mais próximo = Harvest Seeds–Continued (Nexus #30400, ~354 endorsements, mantido), Joja Seeds (Nexus #8746, framing anti-orgânico "não pode ir no seed maker"), Joseph's Seed Shop (Nexus #11676, sementes nativas norte-americanas, v2.0.0, sem mecânica de solo), ReGen Valley (Nexus #39740, alpha v0.51, 3 endorsements, soil per-tile + grazing, MP não verificado), Compost Pests and Cultivation (Nexus #4446, abandonado desde SDV 1.4, único com lineage/trait XP, sem heirloom). Contraste mecânico híbrido×crioulo **inexplorado**; audiência brasileira de sementes crioulas (MST/ANA/Embrapa/PNAPO/Lei 10.711/2003) sem nenhum mod atendendo.
- SDV 1.6.15 estável (1.6.16 pré-anunciado, sem data; 1.7 indev sem data); SMAPI 4.5.2 (requer 1.6.14+) — alvo do repo (`MinimumGameVersion` 1.6.14 / `MinimumApiVersion` 4.4.0) correto. `Data/Crops` / `Data/Objects` / `Data/Shops` data-driven (Shop ID `Traveler` = carrinho viajante, verificado por decompilação e wiki).
- SP de shop: Pierre's `SeedShop` e `Traveler` são `Data/Shops` data-driven; qualquer seleção de distribuição pode ser feita sem NPC novo.
- Pipeline é pré-requisito de Three Sisters (Epic 3), Green Manure, Rotation Journal e CSA — fechar este pipeline destrava 4 features nomeadas.

## Restated

Propose an implementing (or specifying, pending decision) slice that adds an heirloom/landrace ("sementes crioulas") seed→crop→produce pipeline to Living Roots — slow-growing, higher-quality, soil-restoring seeds contrasted against Pierre's hybrids — with an in-game distribution path, multiplayer-safe state handling, and honest verification, sized as the next 80/20 step.

## Origin & Context

- **Raised by**: maintainer (`lfgranja`) via `ROADMAP.md`/`OBJETIVO.md` refresh 2026-11-09, item 6 ("Próximo slice de gameplay"); autonomy model per constitution/023 — agent decides autonomously; human only for explicit authorization or incapacity.
- **Trigger**: specs 004/005/013/014/016/017 closed/near-closed; 017 released repo-scoped 0.1.0 (machine-pass host-only, sem D1, sem attestation humana); pendências críticas restantes são attestation humana/publicação (decisão do maintainer), D1 (ambiente), SC-004 (humanos) — nenhum executável autonomamente. Item 5 (spec 010) é decisão documental pequena. Item 6 é o único slice de gameplay de alto valor/80/20 executável no mandato autônomo.
- **Project constraints (from repo guidance/constitution):** .NET 6/SMAPI ≥4.4, SDV ≥1.6.14; DDD layering; `ModEntry.cs` sole composition root; async-only; `ModConstants` single source; multiplayer first-class (Principle VI); TDD; Gate 3 com machine-pass ≠ `verified`; honestidade de evidência; sem promoção de claims; `git add <file>` explícito; `.specify/scripts/atomic-commit-hook.sh` após cada edição.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: qual é o escopo mínimo viável — 3 culturas (briefing) vs. 1 cultura-template vs. infraestrutura genérica com conteúdo exemplo? O que "80/20" significa aqui (impacto/custo)?]
- [NEEDS CLARIFICATION: canal de distribuição sem Elena — carrinho viajante (`Traveler`), Pierre com condição, semente salva (seed-saving puro), craft, ou descope para "sem caminho de aquisição neste slice" (como 017)? Contraste Pierre×crioulo é o ponto narrativo — quem vende o quê?]
- [NEEDS CLARIFICATION: seed-saving embutido (colheita devolve semente com chance) ou apenas "produce compostable"? Qual chance/invariante? Interage com `OrganicWasteValidator` e com sementes híbridas (devem retornar semente também?)?]
- [NEEDS CLARIFICATION: assets novos (sprites 16×16 + crop sheet) são obrigatórios, ou há fallback honesto (tint/vanilla fallback) no escopo? Custo de criação de arte e licença/atribuição?]
- [NEEDS CLARIFICATION: regra de solo `+2` plantio / `+1` colheita — hooks (`HoeDirt.plant` postfix via Harmony? novo hook Crop.newDay? seam de colheita existente?), multiplayer (host-authoritative, farmhand message? reutiliza FR-009/A8 sync?), first-wins/locks?]
- [NEEDS CLARIFICATION: qual a relação com Three Sisters/Green Manure (consumidores futuros) — abstração genérica `Heirloom*` ou hardcode das 3 culturas do briefing?]
- [NEEDS CLARIFICATION: honestidade agronômica — quais números são abstração explícita de jogo vs. claim agronômico (renda/estabilidade/qualidade nutricional)? Beleza/proveniência das "sementes da paixão"/crioulas brasileiras entra no texto de UI?]
- [NEEDS CLARIFICATION: Gate 3 cenário novo para o slice (3 sinais)? E máquina-pass possível host-only? E D1/humano permanecem `pending`?]
- [NEEDS CLARIFICATION: dependência de Elena — aguardar Elena (não implementado) ou descope de distribuição independente dela? Como o briefing foi desenhado para Elena, o que muda?]
- [NEEDS CLARIFICATION: release — SemVer 0.2.0 repo-scoped (precedente 017: manifest bump, sem canal público, sem tag) ou nenhum bump/release neste ciclo?]
