# OBJETIVO.md — Living Roots / Raízes Vivas

> **Fontes:** `docs/GDD-living-roots.md` (v1.1), `ROADMAP.md` (atualizado 2026-11-09), `.specify/memory/constitution.md` v2.0.0, `README.md`, `AGENTS.md`, `VERIFICATION_PROTOCOL.md`, assessments/specs/bugs, `research/2026-10-roadmap-feasibility-and-agronomy.md`, `research/2026-11-09-journey-balance-and-outlook.md`, `research/2026-11-09-web-research-ecosystem-status.md`, `specs/014-soil-health-crop-quality/gate3-evidence.md`, `specs/020-heirloom-seeds-pipeline/test.md`.
> **Status do projeto:** **v0.1.0** (manifest; release de repositório em `dev` desde 2026-11-07 — sem canal público, sem tag git, sem attestation humana por desenho) · .NET 6 / SMAPI 4.5.2 · Stardew Valley 1.6.14+ · branch `dev` = `origin/dev` com **0 commits à frente**, working tree limpo em 2026-11-09 · sem PRs abertos · suíte **1997 passed / 4 skipped / 0 failed** (2001).
> **Owner:** maintainer (lfgranja) · **Audiência primária:** jogadores cozy 25–45 + casais em co-op + educadores/ativistas agroecológicos.

---

## 1. Objetivo do projeto (o "por quê")

Construir um **mod SMAPI para Stardew Valley 1.6** que transforme o solo de "sujeira" em **ser vivo** — um valor persistente 0–100 por tile, farm-keyed, host-authoritative — e faça disso o eixo de toda a progressão: compostagem, cobertura, rotação, policultura, adubo verde, manejo animal e soberania alimentar, culminando na saída da Joja do vale. **Gratuito (MIT), sem monetização, sem gacha, sem dual-currency.**

Diferencial vs. concorrentes próximos (ex.: ReGen Valley, Nexus #39740): não é só “ter soil health”, mas a **coerência agroecológica**, o **loop composto de compostagem/proteção/decisão**, o **framing educativo honesto** e a **correção multiplayer desde o dia 1** (Constitution Principle VI). O espaço-alvo permanece: per-tile soil health + compostagem + mulch + rotação/policultura + manejo animal + soberania alimentar + framing educativo + multiplayer correto num único loop coerente.

---

## 2. Objetivo do produto (o "o quê" jogável)

**One-line pitch:** *Stardew Valley com solo vivo — composte, cubra e rotacione para libertar o vale da Joja.*

**Verbos (7, teto duro):** arar/capinar · compostar · coletar · aplicar · plantar/rotacionar · observar · partilhar.

**Loops implementados em código e cobertos por evidência automatizada (machine-pass headless, host-only):**
- **Compost loop:** craftar bin → posicionar → carregar resíduo → avançar 2 dias → coletar composto → aplicar em `HoeDirt` (+15, com limites/validação).
- **Proteção:** aplicar mulch/cobertura (spec 013, 46/46) para reduzir/evitar decaimento conforme regras implementadas.
- **Crescimento:** soil health influencia crescimento (spec 005, 146/146).
- **Colheita (spec 014, 131/133):** tiers Cornell CSHA `[0,31)/[31,71)/[71,100]` (Poor/Moderate/Healthy) afetam qualidade/extra de colheita com guards de escopo, composição com matemática vanilla, feedback procedural headless-safe; sync MP FR-009/A8 host→cliente no `DayStarted` e após aplicação bem-sucedida de composto.
- **Joja / tradeoff agroquímico (spec 017, 127/127 — release 0.1.0):** item `LivingRoots.JojaAgrochemical`, penalidade bounded (P=10, recuperável), recusa explícita em baseline 0, telegrafia do custo, sem buff duplo com vanilla. **Não é o sistema Joja/monocultura completo** — um tradeoff controlado.
- **Heirloom seeds (spec 020, 35/35 — pipeline inicial):** Crioulo Parsnip, +2 de saúde ao plantar, +1 na colheita saudável, 1 semente salva por colheita; registro em `Data/Objects`/`Data/Crops`/`Data/Shops`; venda pelo Traveler na primavera (35g, estoque 5). O Gate 3 cobre **apenas o plantio**; colheita, semente salva, aquisição natural e D1 seguem sem Gate 3.

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

## 4. Estado atual verificado (snapshot 2026-11-09)

| Métrica | Valor | Como verificado |
|---|---|---|
| Build | **0 erros** (14 warnings pré-existentes; `-m:1` neste host — mitigação registrada de `msbuild-release-build-sigabrt`) | `GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln -c Release -m:1` — re-verificado 2026-11-09 |
| Testes | **1997 passed / 0 failed / 4 skipped** (2001) | `dotnet test Stardew-LivingRoots.sln -c Release -m:1` — re-verificado 2026-11-09 (fresca) |
| Band scale | gate 31/71 passou | `scripts/tests/test-band-scale-31-71.sh` |
| Spec 014 | **131/133** tarefas (`tasks.md`); T131–T133 completos; T109 D1 e T110 pendentes | `specs/014-soil-health-crop-quality/tasks.md` |
| Spec 017 Joja (MVP) | **127/127** tarefas; release **0.1.0** no repo (`d3ada42`); validate.md **aprovado** (1871/0/4 no c9ff596) | `.specify/validations/017-joja-agrochemical-tradeoff/validate.md`, `.specify/releases/017-joja-agrochemical-tradeoff/release.md` |
| Spec 020 heirloom | **35/35** tarefas; Gate 3 só do plantio | `specs/020-heirloom-seeds-pipeline/tasks.md`, `test.md` |
| Specs fechadas (0 abertas) | **002 (38), 005 (146), 008 (184), 012 (11), 013 (46), 016 (43), 017 (127), 020 (35)** | `specs/*/tasks.md` |
| Specs parciais/legadas | 004 (65/67), 014 (131/133), 015 (58/65), 018 (24/28), 019 (5/7), 009 (55/58), 010 (150/203), 001 (10/86), 003 (28/117), 011 (42/88) | `specs/*/tasks.md` — totais **1198 done / 284 open (1482)**; trilha ativa **968/988 = 98,0%** |
| Gate 3 (7 specs) | **machine-pass host-only**: 004 `7374`, 005 `4cc1`, 013 `1ea4`, 014 `088b`, 015 `29cf`, 017 `6938`, 020 `6021`; todos `verdict: pass`, honesty R | `specs/*/test.md`, `specs/*/verification/host-only-gate3.md`; bundles gitignored |
| Qualidade local | gate v4 "ALL STEPS PASSED" (format/restore/build+tests 1997/4/0/smoke 49-0/1-skip) | `.specify/bugs/msbuild-release-build-sigabrt/fix.md`, commit `b945f81` |
| Repo/git | `dev` = `origin/dev`; **0 commits à frente**; árvore limpa; sem tag; release só no repo; sem PRs abertos; 0 releases públicas | `git status -sb`, `git rev-list --count origin/dev..dev`, `git tag --list` (2026-11-09) |

> **Honestidade de verificação:** `machine-pass` ≠ `verified`. **Não existe atestado humano/E** e **não existe confirmação multiplayer real (D1)** neste snapshot. Evidência automatizada headless é forte para os sinais cobertos, mas não substitui confirmação humana nem multiplayer real. **Nenhuma claim é promovida além de host-only machine-pass.**

### 4.1 Bloqueios/deferimentos explícitos
- **T109 D1 (spec 014):** bloqueado — teste ao vivo com **segundo jogador/farmhand real** não executado pelo runner headless atual. FR-009/A8 foi implementada e testada por unidade/regressão; **convergência ao vivo não atestada**. Mesmo bloqueio aplica-se a spec 015 (T049/T057) e aos 4 cenários farmhand-guard de spec 018 (`blocked-feasible`, T002).
- **Spec 020 (heirloom):** o Gate 3 cobre **apenas o plantio** (50→52). Colheita (+1), semente salva, aquisição natural (Traveler, primavera, 35g, estoque 5) e convergência D1 **não** têm Gate 3; a aquisição repousa em testes de contrato + `lr_give_heirloom_seed`.
- **T110:** deferido — migração histórica de contrato 34/67→31/71 para bundle/spec 004/contrato histórico spec 010. Código vivo/spec 014 alinhados a 31/71; desvio registrado.
- **SC-004 (spec 017):** protocolo de compreensão escrito; **nenhuma execução registrada** — `pending`.
- **Gate 3 humano/E:** nenhum spec tem attestation humana; `.specify/validations/017/` é a única validação humana de spec (aprovação de release, não verificação de gameplay).
- **Spec 009 T039/T040/T052:** sessão SMAPI manual nunca executada.
- **Spec 019 T006/T007:** ações de owner fora do repo (secrets, branch protection, auto-merge, tag) — nunca executadas autonomamente; `local-only` por desenho.
- **CI remoto dormente:** `Build and Test` só roda com `BUILDDEPS_ENABLED=true` + `BUILDDEPS_TOKEN`; `Verify Scenario` é `continue-on-error`; `Check Formatting` é o único check sempre-ativo e só atesta formatação — não pode ser promovido a required check sozinho.
- **Persistência de zero medido:** um resultado exato `0f` sobrevive em memória na sessão (T166/T182), mas é omitido no save/load e recarrega como Unknown/`NaN`; sub-1e-4 não-zero também não persiste (sparse cache; limitação pré-existente, `specs/017-joja-agrochemical-tradeoff/verification/measured-zero-persistence.md`).

### 4.2 Contexto competitivo/ecossistema (fontes primárias; refresh 2026-11-09)

> **Refresh 2026-11-09:** ecossistema/técnica re-verificados — ver `research/2026-11-09-web-research-ecosystem-status.md` (síntese), `research/2026-11-09-ecosystem-market.md` e `research/2026-11-09-technical-verification.md` (dados brutos, fontes e limitações).

- **ReGen Valley** (#39740, robertsvalley): v0.51, tag 1.6-compatible, MP "early testing", **3 endorsements** (DLs ocultos; Nexus 403 nesta sessão — números via snippet de busca, confiabilidade medium). Overhaul regenerador com soil health per-tile + cover crops + grazing; quality/yield **farm-wide** (75+/90+ avg). Distinção mecânica vs Living Roots: per-tile deterministic + compost loop + educational framing + host-authoritative documentado + verificação automatizada. Fonte: https://www.nexusmods.com/stardewvalley/mods/39740 (acesso direto 403; ver refresh 2026-11-09).
- **Ecossistema de qualidade/colheita (adjacente, não idêntico):** *Crop Quality* #47141 (jun/2026, ~0,9k DLs), *Abundant Harvest Fertilizers* #51999 (mit; ~0,1k DLs), *Quality Multi-Harvest* #54375 (out/2026, ~0,1k DLs), *Ultimate Fertilizer* #21318 (**862 endorsements**). Demanda modesta porém real no eixo "qualidade dirigida"; não evidência de paridade com o design Living Roots.
- **Base técnica (atual):** SDV PC **1.6.15** (2024-12-20) / console 1.6.15.1; **1.7 anunciada, sem data** (Clint/Sandy, children overhaul, new farm type; provável 2027); SMAPI **4.5.2** (2026-03; requer SDV 1.6.14+; ~350k downloads no asset GitHub); Harmony **2.2.2** empacotado; compat list em **sunset** — distribuição = GitHub Releases + Nexus + `Pathoschild/SmapiCompatibilityList` (PRs).
- **Dedicated/headless (D1):** sem dedicated server oficial; **JunimoServer** (repo movido para `stardew-valley-dedicated-server/server`, MIT 383★; stable `sdvd-server-v1.4.0` 2026-01-17; docs recomendam preview `v1.5.0-preview.140`) roda cliente automatizado (JunimoTestClient, HTTP :5123; Testcontainers+xUnit; manual/VPS, nunca PR-gate; contas Steam + jogo). Fallback local: duas instâncias SMAPI sob Xvfb. SMAPIDedicatedServerMod **morto** (2024); ValleyServer 2.x maintenance / 3.x experimental.
- **Norm de CI no ecossistema:** Pathoschild/StardewMods = `StardewModders/mod-reference-assemblies` + `dotnet test` (NUnit, jogo nunca lançado); SVE e GMCM sem CI. Living Roots' `Xvfb` runner já supera a norma.

**Implicação:** o diferencial defensável continua sendo a combinação coerente (solo por tile + compost/mulch/crescimento/qualidade + framing educativo + MP host-authoritative/FR-009/A8 + evidência automatizada reproduzível) — comunicado com honestidade: `machine-pass` ≠ `verified`; D1/humano pendentes.

---

## 5. O que FUNCIONA (evidência forte)

- **Núcleo:** `SoilHealthService` (0–100, cache esparso), `CompostingBinService`, `SoilDecayService` (incl. estufa conforme regras), `MulchApplicationService` (spec 013), visualização/overlay/feedback.
- **Spec 005:** soil health → crescimento (implementado/testado).
- **Spec 014:** classificação 31/71, decisão de qualidade/extra com guards, composição com vanilla, feedback procedural (`HarvestFeedbackTextures`), sync FR-009/A8 (host snapshot + apply de cliente), testes RED/regressão/propriedades; cenário harvest-quality com **machine-pass** host-only.
- **Spec 017 (MVP Joja):** item + telegrafia + penalidade bounded + recusas explícitas + broadcast host-authoritative; **release 0.1.0** no repo; Gate 3 host-only machine-pass; `validate.md` aprovado.
- **Spec 020 (heirloom):** identidade/registro (Objects/Crops/Shops), plantio +2, colheita +1 em solo saudável, semente salva determinística, i18n default/en/pt, comandos de verificação; Gate 3 host-only do plantio.
- **Specs 002/005/008/012/013/016/017/020 fechadas** (0 tarefas abertas, artefatos commitados).
- **Persistência/segurança:** `ModDataService` + sanitização/validação; sem logar exceções cruas; limites 0–100.
- **Concorrência/MP (design):** host-authoritative, locks/estado atômico onde aplicável, snapshot fora de lock + I/O.
- **Qualidade local:** build (0 erros), suíte **1997/0/4**, format exit 0, band-scale executáveis e passando em 2026-11-09.
- **Harness de verificação:** runner headless (specs 010/016) produz bundles verificáveis (SHA-256 anchor); smoke suite 49/0/1-skip (`b945f81`); 7 specs com Gate 3 machine-pass host-only.

---

## 6. O que FALTA / gaps

1. **Confirmação humana/E (Gate 3):** promover evidência de machine-pass para `verified` apenas com atesto humano conforme protocolo.
2. **Multiplayer real (T109 D1):** sessão ao vivo host+farmhand, 3 sinais/convergência observável. Também destrava spec 015 T049/T057 e os 4 farmhand-guards de spec 018.
3. **T110:** migração/decisão sobre contrato histórico 34/67 (deferido).
4. **Specs/roadmap aberto:** 010 (53 abertas), 015 (7), 004 (2), 009 (3), 018 (4), 019 (2) e trilhas antigas 001/003/011 majoritariamente abertas (totais em 2026-11-09: **1198 tarefas done / 284 open = 80,8%**; trilha ativa **968/988 = 98,0%**).
5. **Higiene de publicação:** `dev` já está sincronizado com `origin/dev` (0 à frente), mas **não há tag nem release pública** — decidir tag `0.1.0`/canal público ou registrar explicitamente “não publicar”; sem force-push/rewrite.
6. **SC-004 (spec 017):** protocolo de compreensão sem execução registrada.
7. **Conteúdo/visão longa:** Joja Fertilizer/monocultura completa, heirloom para outras culturas (020 entregou só o Crioulo Parsnip), rotação/policultura/adubo verde/animais, Elena/NPCs, mid/end-game, soberania alimentar — majoritariamente não implementados (ver `specs/future-roadmap/briefings/`).
8. **Zero medido não persiste** (sparse cache) — limitação registrada, não corrigida.
9. **T133 (spec 004):** paridade unidirecional Machine/validator — limitação de API do jogo.

---

## 7. O que o projeto NÃO faz (escopo explícito)

- Não é jogo standalone; depende de **Stardew Valley + SMAPI**.
- Sem DB externo/serviço de rede/fonte de verdade dupla; estado farm-scoped/host-authoritative.
- Sem monetização (MIT, sem gacha/dual-currency/live-ops).
- Sem quebra intencional de saves single-player antigos (migração aditiva).
- Sem localização além PT/EN no escopo atual.
- Não declara `verified` humano sem evidência humana; não trata machine-pass como E.

---

## 8. Próximo passo 80/20 (recomendado, refresh 2026-11-09)

**Contexto:** desde o snapshot anterior (2026-11-05), spec 017 (Joja/Agrochemical MVP) foi **implementada, verificada (host-only machine-pass) e lançada no repo como 0.1.0** (`d3ada42`; sem canal público/tag), e spec 020 (heirloom seeds) foi implementada e fechada (35/35; Gate 3 do plantio `6021`). Suite atual: **1997/0/4**; build 0 erros; format exit 0; `dev` = `origin/dev` (0 à frente).

**O que falta não é código novo — é evidência, decisão de publicação e fechamento documental:**

1. **Decisão de publicação (maintainer):** `dev` já está em `origin/dev`; decidir tag `0.1.0` + release notes e eventual canal público (Nexus/CurseForge) **ou** registrar explicitamente “não publicar”. Sem reescrita de histórico. Bloqueador: decisão humana, não técnica.
2. **Atestado humano/E:** roteiro manual 3-sinais (observation + save diff + SMAPI log) para promover `machine-pass` → `verified` nos 7 cenários cobertos (004, 005, 013, 014, 015, 017, 020). Nenhum foi executado.
3. **D1 real (T109/015 T049/T057):** sessão host+farmhand com evidência honesta — caminho concreto (pesquisa 2026-11-09): **duas instâncias SMAPI sob Xvfb** (host+farmhand, localhost join) ou **JunimoServer** (server+client containers + JunimoTestClient :5123; manual/VPS; contas Steam + jogo; nunca PR-gate). Destrava 014/015/018.
4. **SC-004 (spec 017):** executar o protocolo de compreensão registrado em `specs/017-joja-agrochemical-tradeoff/verification/sc004-comprehension-protocol.md`.
5. **Fechar/documentar resíduos:** 004 T034/T037, 014 T110, 015 abertas, 018 farmhand-guards, 019 owner actions; decidir escopo de 010 (53 abertas) via `/speckit.converge` append-only.
6. **Próximo slice de gameplay:** **rotação/policultura MVP** (fecha “BUILDS”) **ou** **Joja/monocultura completa** (fecha “DESTROYS”). O pipeline heirloom (020) já entregou a base para Three Sisters/Green Manure/Rotation Journal/CSA. Só depois de 2/3.
7. **Preparação 1.7:** não migrar agora; monitorar o anúncio de data (janela provável 2027). Custo zero.
8. **Reconciliação documental contínua:** manter `OBJETIVO.md`/`ROADMAP.md`/`research/2026-11-09-journey-balance-and-outlook.md`/evidências alinhados.

**Append (2026-11-09, Cycle 020):** o slice heirloom foi **implementado e verificado em machine-pass host-only** (spec 020, run `20261109T155608Z-6021`, bundle `evidence-bundle-run-20261109T155608Z-6021/`, honesty R, suite 1997/0/4, format clean). `machine-pass` ≠ `verified`; D1/humano E/SC-comprehension permanecem `pending`. Nenhum bump de manifest/tag/publish foi feito (decisão do maintainer).

---

## 9. Referências-chave

- Pesquisa ecossistema/mercado (2026-11-05): `research/2026-11-05-web-research-ecosystem-status.md`
- Pesquisa ecossistema/mercado (refresh 2026-11-09): `research/2026-11-09-journey-balance-and-outlook.md` (síntese/balanço) + `research/2026-11-09-web-research-ecosystem-status.md` (+ `2026-11-09-ecosystem-market.md`, `2026-11-09-technical-verification.md`)
- Pesquisa agronômica: `research/2026-10-roadmap-feasibility-and-agronomy.md`
- 80/20 cycle (2026-11-07): `research/2026-11-07-mdmv-80-20-cycle-ecosystem-and-reconciliation.md`
- Ver `ROADMAP.md` (snapshot verificado 2026-11-09) e `.specify/memory/constitution.md` v2.0.0.
- Verification: `VERIFICATION_PROTOCOL.md`, `specs/*/test.md`, `specs/*/gate3-evidence.md`; heirloom: `specs/020-heirloom-seeds-pipeline/test.md`.
- Release: `.specify/releases/017-joja-agrochemical-tradeoff/release.md`; validação: `.specify/validations/017-joja-agrochemical-tradeoff/validate.md`.

- https://smapi.io/ · https://github.com/Pathoschild/SMAPI/releases/tag/4.5.2 · https://smapi.io/mods · https://docs.junimoserver.com/ · https://www.nexusmods.com/stardewvalley/mods/39740

*Última atualização: 2026-11-09 (verificado: build 0 erros, testes 1997/0/4, release 0.1.0 no repo, `dev` = `origin/dev` com 0 commits à frente; specs 005/013/014/016/017/020 entregues; 7 specs machine-pass host-only; D1/humano E/SC-004 **pendentes**; sem tag/release pública).*
