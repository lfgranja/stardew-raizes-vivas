# OBJETIVO.md — Living Roots / Raízes Vivas

> **Fontes:** `docs/GDD-living-roots.md` (v1.1), `ROADMAP.md` (verificado 2026-11-01), `.specify/memory/constitution.md` v2.0.0, `README.md`, assessments/specs/bugs.  
> **Status do projeto:** v0.0.1 · .NET 6 / SMAPI 4.5.2 · Stardew 1.6.14+ · branch `dev`  
> **Owner:** maintainer (lfgranja) · **Audiência primária:** jogadores cozy 25–45 + casais em co-op (caso fundador: maintainer + esposa) + educadores/ativistas agroecológicos

---

## 1. Objetivo do projeto (o "por quê")

Construir um **mod SMAPI para Stardew Valley 1.6** que transforme o solo de "sujeira" em **ser vivo** — um valor persistente 0–100 por tile, farm-keyed, host-authoritative — e faça disso o eixo de toda a progressão: compostagem, cobertura, rotação, policultura, adubo verde, manejo animal e soberania alimentar, culminando na saída da Joja do vale. **Gratuito (MIT), sem monetização, sem gacha, sem dual-currency.**

Diferencial vs. ReGen Valley (Nexus, 2025): não é a mecânica de soil health (já existe), mas a **coerência agroecológica**, o **loop de compostagem composto**, o **framing educativo** e a **correção multiplayer desde o dia 1**.

## 2. Objetivo do produto (o "o quê" jogável)

**One-line pitch:** *Stardew Valley com solo vivo — composte, cubra e rotacione para libertar o vale da Joja.*

**Verbs (7, teto duro):** arar/capinar · compostar · coletar · aplicar · plantar/rotacionar · observar · partilhar.

**Loop vertical slice (jogável hoje):** craftar bin → posicionar → carregar resíduo → avançar 2 dias → coletar 1 composto → aplicar em `HoeDirt` (+15) → ver tier mudar. Critério: fresh save completa numa sentada sem instrução externa (SC-001/SC-008).

**Win final (Food Sovereignty):** média da fazenda >80% saúde + ≥10 heirlooms + Cozinha Coletiva + CSA ≥5 famílias → Pierre troca híbrido por heirloom, Morris sai, troféu "Guardiã das Raízes Vivas". É transformação do vale, não tela de vitória.

**Loss soft:** solo pobre (0–30%) = crescimento lento, pragas, sem estrela. Nunca trava save — sempre recuperável.

## 3. Pilares testáveis (filtro anti-feature-creep)

1. **Solo é ser vivo, não barra** — toda feature toca 0–100 ou é cortada.
2. **Recompensar, nunca pregar** — Elena explica, regra convence; Joja nunca bloqueada, só pior no longo prazo.
3. **Co-op desde o dia 1** (Principle VI) — farm-keyed, host-authoritative, first-wins + recusa explicada.
4. **Verbo novo por arco, não +10%** — Y1 compostar, Y2-3 rotacionar/incorporar/pastejar, Y4+ assinar/trocar/cozinhar.
5. **Aconchego seguro** (Safety/Abundance/Softness) — sem fail punitivo, teto em toda penalidade.

## 4. Estado atual (verificado, 2026-11-01)

| Métrica | Valor |
|---|---|
| Build | 0 erros, 0 warnings |
| Testes | 1436 passed / 0 failed / 4 skipped |
| Format gate | clean |
| Quality gate | 171 pytest passed; analyzer probe PASS |
| Specs fechadas | 002, 004, 008, 009 (local), 011 (local), 012 (converged), 013 (shipped) |
| Features nomeadas shipped | 2 de 21 (composting, mulch) + 5 de 13 promessas de core |
| In-game runs | **0 — nunca executado** |
| DLLs proprietárias no git | 0 (apenas assets em `.gamepath/Mods/`) |

**Verdade em uma frase:** o mod pode *medir* solo, *exibir*, *mudar* com composto, **solo dirige crescimento** (spec 005) e **solo pode ser protegido com mulch** (spec 013). Falta a *qualidade* (colheita) e a *destruição* (Joja, monocultura) — o jogador cresce mais rápido e protege, mas não *colhe* benefícios além de velocidade nem *perde* solo por má prática.

## 5. O que FUNCIONA (verificado)

- Núcleo: `SoilHealthService` (cache esparso, 0–100), `CompostingBinService` (state machine Empty→Processing→Ready, 1-in-1-out, 2 dias), `SoilDecayService` (decaimento sazonal, greenhouse exempt), `MulchApplicationService` (7 dias, decay skip), visualização/overlay reativo, feedback com enxada.
- Persistência segura: `ModDataService` (sanitização path traversal/homoglyph/Unicode), valores validados, save/load tolerante.
- Concorrência: locks host-authoritativos compartilhados, flags atômicos, snapshot fora do lock, rollback em unsubscribe.
- Quality gate local: scripts Python, Semgrep/Trivy, baselines (76.9% linhas, 0.49% duplicação), hooks lefthook.
- Testes: xUnit + Moq + FsCheck (property-based), 1436 passando.

## 6. O que FALTA / gaps (não verificado ou não implementado)

- **Qualidade de colheita** (silver/gold/iridium + colheita extra) — ❌ não implementado. **Próximo 80/20.**
- **Monocultura** (penalidade por repetição) — ❌ só solo nu decai.
- **Joja Fertilizer** (boost agora, −5% saúde) — ❌ item não existe.
- **Heirloom pipeline** — ❌ bloqueia Three Sisters, Green Manure, Rotation Journal.
- **Mid-game (6) e End-game (6)** — ❌ não iniciado.
- **Multiplayer ao vivo (D1)** — ❌ verificação multi-cliente não provada (apenas stubs).
- **Gate 3 (in-game)** — ❌ 0 runs executados.
- **Spec 001** (visualização) — 10/86 (12%).
- **Spec 003** (testes state machine) — 28/117 (24%).
- **Spec 010** (headless runner) — 0/161 (não iniciado).
- **Spec 011** (quality gate) — 42/88 (verificação CI pendente).
- **Assessment soil-health-to-crop-growth** — needs-clarification (5 perguntas abertas).
- **Bugs hold** — ~8 bugs dependem de re-verificação SonarCloud/CI.

## 7. O que o projeto NÃO faz (escopo explícito)

- Sem jogo standalone/engine própria.
- Sem multiplayer com DB externo/serviço rede/fonte-verdade dupla — só save Stardew, host-authoritative (Principle VI).
- Sem quebrar saves single-player antigos — migração aditiva.
- Sem UI custom/text-drawing/arte obrigatória para recusas no slice.
- Sem reestatar set compostável em 004 (herdado de 002).
- Sem romance Elena, sem localização além PT/EN, sem console/mobile, sem live-ops no v1.
- Sem balance final travado antes de playtest Gate 3.
- Sem monetização: mod gratuito MIT.

## 8. Próximo passo (80/20 — maior avanço, menor esforço)

**`/specify.assess.intake` (ou `/speckit.specify` com o briefing) para "Soil Health → Crop Quality"**  
(`briefings/soil-tier-outcomes-harvest-feedback.md`).

- É a *segunda metade* da promessa central: Living Soil (71–100%) finalmente paga na colheita (qualidade silver/gold/iridium + colheita extra).
- Reusa persistência por tile + overlay existentes; precisa de hook em `Crop.harvest` (virtual, patchável).
- Único item restante que faz Living Soil visivelmente valer a pena; fecha o loop build → protect → harvest.

**Quick wins paralelos (≤30 min, sem código):**
1. Fechar T117–T119 (spec 005): reconciliar escala de bandas nos docs.
2. Rodar Gate 3 in-game (spec 009 T052) para capturar os 3 sinais.
3. Fechar bug `ci-workflow-misplaced` via `/specify.bug.assess`.

## 9. Referências-chave

- `docs/GDD-living-roots.md` (v1.1) — bíblia de implementação + visão
- `ROADMAP.md` — progressão e 80/20
- `.specify/memory/constitution.md` (v2.0.0) — princípios não-negociáveis
- `research/2026-10-roadmap-feasibility-and-agronomy.md` — viabilidade SMAPI + agronomia
- `specs/` — specs 001–013 + future-roadmap briefings (31 briefings)
- `AGENTS.md` — comandos, convenções, CI

---

*Última atualização: 2026-11-01 (criado a partir do GDD + ROADMAP + assessments/specs/bugs).*