# OBJETIVO.md — Living Roots / Raízes Vivas

> **Fontes:** `docs/GDD-living-roots.md` (v1.1), `ROADMAP.md` (verificado 2026-11-04), `.specify/memory/constitution.md` v2.0.0, `README.md`, `AGENTS.md`, assessments/specs/bugs, `research/2026-10-roadmap-feasibility-and-agronomy.md`.
> **Status do projeto:** v0.0.1 · .NET 6 / SMAPI 4.5.2 · Stardew Valley 1.6.15 · branch `dev`
> **Owner:** maintainer (lfgranja) · **Audiência primária:** jogadores cozy 25–45 + casais em co-op (caso fundador: maintainer + esposa) + educadores/ativistas agroecológicos

---

## 1. Objetivo do projeto (o "por quê")

Construir um **mod SMAPI para Stardew Valley 1.6** que transforme o solo de "sujeira" em **ser vivo** — um valor persistente 0–100 por tile, farm-keyed, host-authoritative — e faça disso o eixo de toda a progressão: compostagem, cobertura, rotação, policultura, adubo verde, manejo animal e soberania alimentar, culminando na saída da Joja do vale. **Gratuito (MIT), sem monetização, sem gacha, sem dual-currency.**

Diferencial vs. ReGen Valley (Nexus, 2025): não é a mecânica de soil health (já existe), mas a **coerência agroecológica**, o **loop de compostagem composto**, o **framing educativo** e a **correção multiplayer desde o dia 1**. Nenhum mod combina per-tile soil health + compostagem composta + mulch + rotação + policultura + manejo animal + soberania alimentar + framing educativo + multiplayer correto num único loop coerente.

---

## 2. Objetivo do produto (o "o quê" jogável)

**One-line pitch:** *Stardew Valley com solo vivo — composte, cubra e rotacione para libertar o vale da Joja.*

**Verbs (7, teto duro):** arar/capinar · compostar · coletar · aplicar · plantar/rotacionar · observar · partilhar.

**Loop vertical slice (jogável hoje em código, não ainda verificado in-game):** craftar bin → posicionar → carregar resíduo → avançar 2 dias → coletar 1 composto → aplicar em `HoeDirt` (+15) → ver tier mudar. Critério: fresh save completa numa sentada sem instrução externa (SC-001/SC-008).

**Win final (Food Sovereignty):** média da fazenda >80% saúde + ≥10 heirlooms + Cozinha Coletiva + CSA ≥5 famílias → Pierre troca híbrido por heirloom, Morris sai, troféu "Guardiã das Raízes Vivas". É transformação do vale, não tela de vitória.

**Loss soft:** solo pobre (0–30%) = crescimento lento, pragas, sem estrela. Nunca trava save — sempre recuperável.

---

## 3. Pilares testáveis (filtro anti-feature-creep)

1. **Solo é ser vivo, não barra** — toda feature toca 0–100 ou é cortada.
2. **Recompensar, nunca pregar** — Elena explica, regra convence; Joja nunca bloqueada, só pior no longo prazo.
3. **Co-op desde o dia 1** (Principle VI) — farm-keyed, host-authoritative, first-wins + recusa explicada.
4. **Verbo novo por arco, não +10%** — Y1 compostar, Y2-3 rotacionar/incorporar/pastejar, Y4+ assinar/trocar/cozinhar.
5. **Aconchego seguro** (Safety/Abundance/Softness) — sem fail punitivo, teto em toda penalidade.

---

## 4. Estado atual (verificado, 2026-11-04)

| Métrica | Valor | Como verificado |
|---------|-------|-----------------|
| Build | 0 erros, 0 warnings | `dotnet build -c Release` (com GamePath válido) |
| Testes | 1621 passed / 1 failed / 4 skipped (1626 total) | `dotnet test -c Release` — 1 falha pré-existente `ProductionSourceGuardTests`, `ModController.cs:399` |
| Format gate | clean | `dotnet format --verify-no-changes` |
| Quality gate | 171 pytest passed; analyzer probe PASS | `scripts/quality-gate/` (Semgrep/Trivy/jscpd) |
| Testes de arquitetura | 84/84 `ArchitecturalPlanTests` pass | `dotnet test` |
| Specs fechadas/convergidas | 002, 008, 012, 013 | `specs/*/tasks.md` |
| Specs quase fechadas | 004 (machine-pass), 005 (143/146), 009 (55/58), 015 (51/56) | `specs/*/tasks.md` |
| Specs em andamento | 001 (10/86), 003 (28/117), 010 (137/203), 011 (42/88), 014 (11/104) | `specs/*/tasks.md` |
| Features nomeadas shipped | 3 de 21 | composting, mulch, soil-health→crop-growth |
| Promessas de core | 6 de 13 (1 infra-only) | soil decay, growth driver, mulch, measured/displayed, compost, harvest seam (infra) |
| In-game runs | machine-pass headless (spec 004, 2026-11-04) | bundle `evidence-bundle-run-20261104T082444Z-4acd/`, honesty **R** |
| DLLs proprietárias no git | 0 (apenas assets em `.gamepath/Mods/`) | `git ls-files` + `.gitignore` |

> **Gate 3 (2026-11-04):** `evidence-bundle-run-20261104T082444Z-4acd/` — verdict `pass`, honesty `R`. Porém: soil-health delta **0** (10→10, tier poor) é sinal neutro; crop `phaseDays` 4→5 (`round(4×1.15)=5`) é o sinal forte; SMAPI log 1,894B com 5 transições de dia. Um run machine-pass ≠ "verified" (E humano pendente, `VERIFICATION_PROTOCOL.md` §3). Divergência aberta: `Constants.cs` usa `PoorModerateBandEdge=31f`, `ModerateHealthyBandEdge=71f` vs. docs `34/67` — reconciliação T117–T119 (spec 005).

### 4.1 Paisagem competitiva (pesquisa web 2026)

| Mod | O que faz de fato | Relevância para Living Roots |
|-----|-------------------|------------------------------|
| **ReGen Valley** (Nexus, nov/2025) | Soil health per-tile, cover crops, rotational grazing, growth 70+/90+, **farm-wide** quality/yield 75+/90+ avg | Concorrente direto mais próximo. **Não** é per-tile com boost determinístico + feedback transient. Nosso seam (specs 014/015) é único. |
| **Crop Quality** (aedenthorn, CurseForge) | Exibe qualidade da colheita; tweaks de chances | 9.2K downloads. Complementar — exibição, não mecânica de solo. |
| **Simple Skills Extended** (Xsyy, 1.18, out/2026) | Bônus de qualidade/extras por skill, por farmer | Concorrente em "qualidade por skill", não per-tile por saúde do solo. |
| **Spore Galore** (HomesteadyEddy) | Cogumelos + compostagem (substrato → composto em 2 dias) | Sobrepõe compostagem; fecha loop waste→compost→fertilizer. |
| **Harvest Ledger** (Tigrevurmud666, 0.5.0) | Economia dinâmica com streak de rotação | Valida demanda por rotação de culturas. |
| **Tilled Soil Decay Options** (CaptainSully, 2026) | Configura decay de solo arado | Sobrepõe decay; mostra demanda por controle. |
| **Spoilage Mod** (Nexus, 2026) | Apodrecimento + composter → fertilizante | Complementar, não competitivo. |

**Ecossistema 2026:** SMAPI 4.5.2 (14/03/2026) para Stardew 1.6.14+; Stardew 1.6.15 estável; ~30.000+ mods no Nexus; modpacks gigantes (Everything Dew ~550 mods) dominam distribuição; nicho agroecológico/educativo pouco ocupado.

**Implicação:** diferencial (coerência agroecológica + loop de compostagem composto + framing educativo + multiplayer correto desde dia 1 + per-tile determinístico) permanece válido e pouco contestado. Risco: ReGen Valley já entrega "qualidade por avg soil", então nossa entrega per-tile + feedback deve ser clara e visível.

**Verdade em uma frase:** o mod pode *medir* solo, *exibir*, *mudar* com composto, **solo dirige crescimento** (spec 005) e **solo pode ser protegido com mulch** (spec 013). Falta a *qualidade* (colheita) e a *destruição* (Joja, monocultura) — o jogador cresce mais rápido e protege, mas não *colhe* benefícios além de velocidade nem *perde* solo por má prática.

---

## 5. O que FUNCIONA (verificado)

- **Núcleo:** `SoilHealthService` (cache esparso, 0–100), `CompostingBinService` (state machine Empty→Processing→Ready, 1-in-1-out, 2 dias), `SoilDecayService` (decaimento sazonal, greenhouse exempt), `MulchApplicationService` (7 dias, decay skip), visualização/overlay reativo, feedback com enxada.
- **Persistência segura:** `ModDataService` (sanitização path traversal/homoglyph/Unicode), valores validados 0–100, save/load tolerante.
- **Concorrência:** locks host-authoritativos compartilhados, flags atômicos (`Interlocked`), snapshot fora do lock, rollback em unsubscribe.
- **Quality gate local:** scripts Python, Semgrep/Trivy, baselines (76.9% linhas, 0.49% duplicação), hooks lefthook.
- **Testes:** xUnit + Moq + FsCheck (property-based), 1621 passando.
- **Smoke validation real:** mod carrega em SMAPI com success marker (spec 008).

---

## 6. O que FALTA / gaps (não verificado ou não implementado)

### Bloqueia "verificado" (prioridade máxima)

- **Gate 3 (in-game, 3 sinais)** — machine-pass headless (spec 004, 2026-11-04) mas **não** Gate 3 manual 3-sinais (observação + artifact diff + SMAPI log, E humano). Pendente. Sem ele, nenhuma feature shipped pode afirmar "verified".
- **Hang do test harness** — `test-preflight`/`test-integration`/`test-logparse` travam se rodarem concorrentemente (`.specify/bugs/test-harness-hangs/`). Precisa de re-verificação.
- **1 teste falhando pré-existente** — `ProductionSourceGuardTests`, `ModController.cs:399`.

### 80/20 principal (faz Living Soil pagar na colheita)

- **Qualidade de colheita** (silver/gold/iridium + colheita extra) — ❌ não implementado em jogo. Seam `HarvestQualityService` + patch Harmony mergados e unit-testados (17 testes), mas patch **não executado em jogo**; feedback sprites/SFX e extra-harvest gated em SC-008 incompletos. **Próximo 80/20.**
- **Bug `CollectCompost`** — rollback parcial: se output aumenta e o 2º item enche inventário, o 1º já foi entregue, bin continua `Ready`, retorno 0 → duplicação. Corrigir antes de qualquer aumento de output.

### Não implementado

- **Monocultura** (penalidade por repetição) — ❌ só solo nu decai.
- **Joja Fertilizer** (boost agora, −5% saúde) — ❌ item não existe.
- **Heirloom pipeline** — ❌ bloqueia Three Sisters, Green Manure, Rotation Journal.
- **Mid-game (6) e End-game (6)** — ❌ não iniciado (0 de 12).
- **Multiplayer ao vivo (D1)** — ❌ verificação multi-cliente não provada (apenas stubs).
- **Specs 001 (visualização), 010 (headless runner), 011 (quality gate CI), 014 (soil-health→crop-quality integração)** — em andamento/parcial.
- **Assessment soil-health-to-crop-growth** — needs-clarification (5 perguntas abertas: hook `Crop.newDay`, band scale 31/71 vs 34/67, `CollectCompost` rollback, play-test gate, interação ReGen Valley).
- **Bugs hold** — ~8 bugs dependem de re-verificação SonarCloud/CI/codeql.

---

## 7. O que o projeto NÃO faz (escopo explícito)

- Sem jogo standalone/engine própria.
- Sem multiplayer com DB externo/serviço rede/fonte-verdade dupla — só save Stardew, host-authoritative (Principle VI).
- Sem quebrar saves single-player antigos — migração aditiva.
- Sem UI custom/text-drawing/arte obrigatória para recusas no slice.
- Sem reestatar set compostável em 004 (herdado de 002).
- Sem romance Elena, sem localização além PT/EN, sem console/mobile, sem live-ops no v1.
- Sem balance final travado antes de playtest Gate 3.
- Sem monetização: mod gratuito MIT, sem gacha, sem dual-currency.
- Não apresenta "insect hotels" como controle de pragas comprovado (ciência fraca/negativa — deve ser questionado no jogo, não ensinado como fato).

---

## 8. Próximo passo (80/20 — maior avanço, menor esforço)

**`/speckit.implement` continuando specs 014/015 (harvest-quality) + `/speckit.converge`, após rodar Gate 3.**

- É a *segunda metade* da promessa central: Living Soil (71–100%) finalmente paga na colheita (qualidade silver/gold/iridium + colheita extra).
- Reusa persistência por tile + overlay existentes; precisa de hook em `Crop.harvest` (virtual, patchável).
- Único item restante que faz Living Soil visivelmente valer a pena; fecha o loop build → protect → harvest.
- Infraestrutura cara (Harmony, seam, testes, wiring) já mergeada — custo marginal é integração + verificação.

**Quick wins paralelos (≤1h, sem código novo):**
1. Fechar T117–T119 (spec 005): reconciliar escala de bandas nos docs (31/71 → 34/67 ou vice-versa).
2. Rodar Gate 3 in-game (spec 009 T052) para capturar os 3 sinais.
3. Fechar bug do test harness via `/speckit.bug.assess`.

---

## 9. Referências-chave

- `docs/GDD-living-roots.md` (v1.1) — bíblia de implementação + visão
- `ROADMAP.md` — progressão e 80/20
- `.specify/memory/constitution.md` (v2.0.0) — princípios não-negociáveis
- `research/2026-10-roadmap-feasibility-and-agronomy.md` — viabilidade SMAPI + agronomia
- `specs/` — specs 001–015 + future-roadmap briefings (31 briefings)
- `AGENTS.md` — comandos, convenções, CI
- `VERIFICATION_PROTOCOL.md` — protocolo de verificação end-to-end (Gate 3)

---

*Última atualização: 2026-11-04 (Gate 3 machine-pass spec 004; testes 1621/1/4; spec 010 137/203). Supersede: 2026-11-01 (criado a partir do GDD + ROADMAP + assessments/specs/bugs).*
