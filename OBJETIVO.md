# OBJETIVO.md — Living Roots / Raízes Vivas

> **Fontes:** `docs/GDD-living-roots.md` (v1.1), `ROADMAP.md` (atualizado 2026-11-06), `.specify/memory/constitution.md` v2.0.0, `README.md`, `AGENTS.md`, `VERIFICATION_PROTOCOL.md`, assessments/specs/bugs, `research/2026-10-roadmap-feasibility-and-agronomy.md`, `specs/014-soil-health-crop-quality/gate3-evidence.md`.
> **Status do projeto:** v0.0.1 · .NET 6 / SMAPI 4.5.2 · Stardew Valley 1.6.14+ · branch local `dev` (`a5293d7`) — **98 commits à frente de `origin/dev`** no snapshot 2026-11-06 (push/PR hygiene pendente; sem PRs abertos).
> **Owner:** maintainer (lfgranja) · **Audiência primária:** jogadores cozy 25–45 + casais em co-op + educadores/ativistas agroecológicos.

---

## 1. Objetivo do projeto (o "por quê")

Construir um **mod SMAPI para Stardew Valley 1.6** que transforme o solo de "sujeira" em **ser vivo** — um valor persistente 0–100 por tile, farm-keyed, host-authoritative — e faça disso o eixo de toda a progressão: compostagem, cobertura, rotação, policultura, adubo verde, manejo animal e soberania alimentar, culminando na saída da Joja do vale. **Gratuito (MIT), sem monetização, sem gacha, sem dual-currency.**

Diferencial vs. concorrentes próximos (ex.: ReGen Valley, Nexus #39740): não é só “ter soil health”, mas a **coerência agroecológica**, o **loop composto de compostagem/proteção/decisão**, o **framing educativo honesto** e a **correção multiplayer desde o dia 1** (Constitution Principle VI). O espaço-alvo permanece: per-tile soil health + compostagem + mulch + rotação/policultura + manejo animal + soberania alimentar + framing educativo + multiplayer correto num único loop coerente.

---

## 2. Objetivo do produto (o "o quê" jogável)

**One-line pitch:** *Stardew Valley com solo vivo — composte, cubra e rotacione para libertar o vale da Joja.*

**Verbos (7, teto duro):** arar/capinar · compostar · coletar · aplicar · plantar/rotacionar · observar · partilhar.

**Loops implementados em código (verificados por testes; machine-pass headless em cenários específicos):**
- **Compost loop:** craftar bin → posicionar → carregar resíduo → avançar 2 dias → coletar composto → aplicar em `HoeDirt` (+15, com limites/validação).
- **Proteção:** aplicar mulch/cobertura (spec 013) para reduzir/evitar decaimento conforme regras implementadas.
- **Crescimento:** soil health influencia crescimento (spec 005).
- **Colheita (spec 014, implementado em código):** tiers Cornell CSHA `[0,31)/[31,71)/[71,100]` (Poor/Moderate/Healthy) afetam qualidade/extra de colheita com guards de escopo, composição com matemática vanilla, feedback procedural headless-safe; sync MP FR-009/A8 host→cliente no `DayStarted` e após aplicação bem-sucedida de composto.

**Win final (Food Sovereignty, não implementado):** média da fazenda >80% saúde + ≥10 heirlooms + Cozinha Coletiva + CSA ≥5 famílias → Pierre troca híbrido por heirloom, Morris sai, troféu "Guardiã das Raízes Vivas". Transformação do vale, não tela de vitória.

**Loss soft:** solo pobre penaliza resultados (dentro dos limites implementados/testados); nunca trava save — sempre recuperável.

---

## 3. Pilares testáveis (filtro anti-feature-creep)

1. **Solo é ser vivo, não barra** — toda feature toca 0–100 ou é cortada.
2. **Recompensar, nunca pregar** — Elena explica, regra convence; Joja nunca bloqueada, só pior no longo prazo.
3. **Co-op desde o dia 1** (Principle VI) — farm-keyed, host-authoritative, first-wins/recusa explicada; single-player é caso degenerado.
4. **Verbo novo por arco, não +10%** — Y1 compostar, Y2-3 rotacionar/incorporar/pastejar, Y4+ assinar/trocar/cozinhar.
5. **Aconchego seguro** (Safety/Abundance/Softness) — sem fail punitivo, teto em toda penalidade.

---

## 4. Estado atual verificado (snapshot 2026-11-06)

| Métrica | Valor | Como verificado |
|---|---|---|
| Build | 0 erros (warnings preexistentes podem aparecer conforme ambiente) | `dotnet build Stardew-LivingRoots.sln -c Release` (`GamePath=/home/luis/stardew-assemblies`) |
| Testes | **1747 passed / 0 failed / 4 skipped** (1751) | `dotnet test Stardew-LivingRoots.sln --no-build -c Release` |
| Formato | limpo nos projetos verificados na sessão | `dotnet format` `--verify-no-changes --no-restore` |
| Band scale | gate 31/71 passou | `scripts/tests/test-band-scale-31-71.sh` |
| Spec 014 | **131/133** tarefas (`tasks.md`) | `specs/014-soil-health-crop-quality/tasks.md` |
| Spec 014 T131–T133 | completos; convergence sem achados acionáveis novos | `tasks.md` + `gate3-evidence.md` |
| Spec 010 runner | **150/203** tarefas | `specs/010-headless-verification-runner/tasks.md` |
| Spec 016 | **43/43** tarefas | `specs/016-gate3-runner-hardening/tasks.md` |
| Gate 3 spec 014 | **machine-pass** (host-only), `verdict: pass`, `signalsComplete/Agree/thresholdsOk: true`, bundle verifier exit 0 | bundles `evidence-bundle-run-20261106T014215Z-759d`, `evidence-bundle-run-20261106T014750Z-14b6`; `specs/014-soil-health-crop-quality/gate3-evidence.md` |
| Repo/git | `dev` `a5293d7` local, **98 à frente de `origin/dev`**; sem PRs abertos; branches `dev`/`main` | `git status`, `gh pr list --state open`, `git branch -vv/-r` |

> **Honestidade de verificação:** `machine-pass` ≠ `verified`. **Não existe atestado humano/E** para Gate 3 neste snapshot. Evidência automatizada headless é forte para os sinais cobertos, mas não substitui confirmação humana nem multiplayer real.

### 4.1 Bloqueios/deferimentos explícitos
- **T109 D1 (spec 014):** bloqueado — teste ao vivo com **segundo jogador/farmhand real** não executado pelo runner headless atual. FR-009/A8 foi implementada e testada por unidade/regressão; **convergência ao vivo não atestada**.
- **T110:** deferido — migração histórica de contrato 34/67→31/71 para bundle/spec 004/contrato histórico spec 010. Código vivo/spec 014 alinhados a 31/71; desvio registrado.

### 4.2 Contexto competitivo/ecossistema (fontes primárias, 2026)
- **SMAPI 4.5.2** para Stardew Valley **1.6.14+** (Linux/macOS/Windows); loader oficial/modding API. Fonte: https://smapi.io/
- **Padrões de teste headless/E2E:** JunimoServer documenta E2E com containers/Testcontainers, X11/VNC/noVNC e automação cliente+servidor — útil como referência de arquitetura de verificação automatizada, não como prova do nosso mod. Fonte: https://docs.junimoserver.com/developers/testing/e2e-testing.html. Padrão comunitário relacionado: SMAPI dedicated server + Xvfb (ex.: `SMAPIDedicatedServerMod`).
- **ReGen Valley** (Nexus #39740, robertsvalley, v0.51, upload 2025-11-28; consultado 2026): overhaul regenerativo com soil health/cover crops/grazing/ferramentas e thresholds de crescimento/colheita ligados à saúde média do solo da fazenda (mecânica distinta do nosso foco per-tile determinístico + feedback/cenários). Fonte: https://www.nexusmods.com/stardewvalley/mods/39740
- **Ecossistema de qualidade/colheita (adjacente, não idêntico):** ex. *Crop Quality* (Nexus #47141), *Abundant Harvest Fertilizers* (Nexus #51999), *Quality Multi-Harvest* (Nexus #54375). Usar como referência de demanda/soluções parciais, não como evidência de paridade com o design Living Roots.

**Implicação:** o diferencial defensável continua sendo a combinação coerente (solo por tile + compost/mulch/crescimento/qualidade + framing educativo + MP host-authoritative/FR-009/A8 + evidência automatizada reproduzível) — desde que comunicado com honestidade sobre o que é machine-pass vs. verificado humano.

---

## 5. O que FUNCIONA (evidência forte)

- **Núcleo:** `SoilHealthService` (0–100, cache esparso), `CompostingBinService`, `SoilDecayService` (incl. estufa conforme regras), `MulchApplicationService` (spec 013), visualização/overlay/feedback.
- **Spec 005:** soil health → crescimento (implementado/testado).
- **Spec 014:** classificação 31/71, decisão de qualidade/extra com guards, composição com vanilla, feedback procedural (`HarvestFeedbackTextures`), sync FR-009/A8 (host snapshot + apply de cliente), testes RED/regressão/propriedades; cenário harvest-quality com **machine-pass** host-only.
- **Persistência/segurança:** `ModDataService` + sanitização/validação; sem logar exceções cruas; limites 0–100.
- **Concorrência/MP (design):** host-authoritative, locks/estado atômico onde aplicável, snapshot fora de lock + I/O.
- **Qualidade local:** build/tests/format/band-scale executáveis; suíte **1747/0/4** no snapshot.

---

## 6. O que FALTA / gaps

1. **Confirmação humana/E (Gate 3):** promover evidência de machine-pass para `verified` apenas com atesto humano conforme protocolo.
2. **Multiplayer real (T109 D1):** sessão ao vivo host+farmhand, 3 sinais/convergência observável.
3. **T110:** migração/decisão sobre contrato histórico 34/67 (deferido).
4. **Specs/roadmap aberto:** pendências em 004/009/010/015 e demais specs (ver `specs/*/tasks.md`, `ROADMAP.md`).
5. **Higiene de publicação:** 98 commits locais à frente de `origin/dev`; sem PRs abertos — precisa push/estratégia de PRs revisáveis quando confirmado.
6. **Conteúdo/visão longa:** Joja Fertilizer/monocultura completa, heirloom pipeline, rotação/policultura/adubo verde/animais, Elena/NPCs, mid/end-game, soberania alimentar — majoritariamente não implementados.

---

## 7. O que o projeto NÃO faz (escopo explícito)

- Não é jogo standalone; depende de **Stardew Valley + SMAPI**.
- Sem DB externo/serviço de rede/fonte de verdade dupla; estado farm-scoped/host-authoritative.
- Sem monetização (MIT, sem gacha/dual-currency/live-ops).
- Sem quebra intencional de saves single-player antigos (migração aditiva).
- Sem localização além PT/EN no escopo atual.
- Não declara `verified` humano sem evidência humana; não trata machine-pass como E.

---

## 8. Próximo passo 80/20 (recomendado)

1. **Fechar decisão operacional de git/PRs:** confirmar commit/push dos 4 arquivos pendentes (ou tratá-los) e definir PRs revisáveis contra `origin/dev` — sem reescrever histórico.
2. **Planejar/viabilizar D1 real (T109)** quando houver ambiente com farmhand; manter FR-009/A8 e registrar evidência honesta.
3. **Ataque de produto após verificação MP:** escolher próximo slice de maior impacto (ex.: integração/fechamento de pendências de harvest/spec 015 onde aplicável, ou mecânica destrutiva/loop de decisão como Joja/monocultura) com TDD + Gate 3 + honesty.
4. **Reconciliação documental contínua:** manter `ROADMAP.md`/`OBJETIVO.md`/evidências alinhados ao estado verificado (esta atualização).

---

## 9. Referências-chave

- `docs/GDD-living-roots.md` (v1.1)
- `ROADMAP.md`
- `.specify/memory/constitution.md` (v2.0.0)
- `VERIFICATION_PROTOCOL.md`
- `research/2026-10-roadmap-feasibility-and-agronomy.md`
- `specs/014-soil-health-crop-quality/gate3-evidence.md`
- https://smapi.io/ · https://docs.junimoserver.com/developers/testing/e2e-testing.html · https://www.nexusmods.com/stardewvalley/mods/39740

*Última atualização: 2026-11-06 (spec 014 T131–T133 machine-pass host-only 759d/14b6; testes 1747/0/4; D1/T110 pendentes; snapshot git local).*
