# Living Roots — balanço de percurso e perspectivas (2026-11-09)

> **Escopo:** quanto do caminho já foi percorrido, com que tipo de evidência, e o que falta — combinando re-execução local fresca, artefatos commitados e refresh web multilíngue.
> **Regra de honestidade:** `machine-pass` ≠ `verified`. Nenhuma claim neste arquivo promove `machine-pass` a atestação humana, D1 ou SC-004.
> **Snapshot local:** `dev` = `origin/dev` = `cd90f2f`; árvore limpa; 0 commits à frente; 0 tags; 0 releases públicas; 20 cenários JSON; 31 briefings em `specs/future-roadmap/briefings/`.

---

## 1. Resumo executivo

- **O núcleo jogável existe e está verde em código/testes.** Solo 0–100 por tile, compostagem, mulch, crescimento dirigido por saúde, qualidade/extra de colheita, tradeoff agroquímico Joja e sementes crioulas estão implementados; a suíte fresca é **1997 passed / 4 skipped / 0 failed (2001)**.
- **A trilha ativa está 98% fechada em tarefas:** 968/988. O número menor, **1198/1482 = 80,8%**, inclui specs legados (001, 003, 010, 011) cujas tarefas antigas respondem por quase todo o aberto restante.
- **A evidência end-to-end é forte, mas estreita:** 7 specs têm Gate 3 `machine-pass` host-only (004, 005, 013, 014, 015, 017, 020). **Nenhum** tem atestação humana/E; **nenhum** tem D1 (farmhand real); **nenhum** tem SC-004.
- **O projeto está pronto para distribuição técnica, não para distribuição pública:** release `0.1.0` existe apenas no repositório/`dev`, sem tag e sem canal público. O `README.md` ainda mandava baixar de uma página de Releases vazia — corrigido nesta atualização.
- **O refresh externo não muda o alvo técnico:** Stardew Valley segue em 1.6.15 no PC; SMAPI estável é 4.5.2; a 1.7 foi anunciada como a maior atualização da história com janela provável em 2027, sem data. Não há pressão para migrar agora.
- **O 80/20 mudou de "escrever feature" para "converter evidência em confiança e publicar":** decisão de tag/release, atestação humana dos cenários já cobertos, D1 e fechamento documental dos resíduos valem mais do que abrir mais uma frente de conteúdo.

---

## 2. Método, fontes e limites

### 2.1 Evidência local (determinística)

Executada nesta sessão, no host, sobre `cd90f2f`:

| Verificação | Resultado |
|---|---|
| `dotnet build Stardew-LivingRoots.sln -c Release -m:1` | 0 erros; 14 warnings pré-existentes (Mono.Cecil/MSB3277, workspace) |
| `dotnet test Stardew-LivingRoots.sln -c Release -m:1` | **1997 passed / 0 failed / 4 skipped (2001)**; 4 skips são testes RED históricos de hooks |
| `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | exit 0 (apenas aviso de workspace) |
| `git status --short --branch` | `dev...origin/dev`, árvore limpa |
| `git rev-list --count origin/dev..dev` | **0** |
| `git tag --list` | 0 |
| `gh pr list` (registro anterior da sessão) | 0 PRs abertos |

### 2.2 Evidência de artefatos

- `specs/*/tasks.md` — contagem de tarefas por spec.
- `specs/*/test.md` / `specs/*/verification/host-only-gate3.md` — blocos GATE3 e Run IDs.
- `.specify/bugs/*/{assessment,fix,test}.md` — maturidade e resultado dos bugs.
- `evidence-bundle-run-*/verdict.json` — veredito e honesty level dos bundles.
- `specs/018-machine-pass-full-coverage/verification/summary.md` — agregação de cobertura machine-pass, mutantes, determinismo e integridade de bundles.
- `specs/future-roadmap/briefings/` — 31 briefings, 0 implementados.

### 2.3 Refresh web (2026-11-09)

Buscas em **EN, PT, ES, FR, DE, JA, ZH, RU, KO**, com prioridade para fontes primárias (site oficial, SMAPI, GitHub, DOI/PMC). Limitações:

- **Nexus Mods bloqueia fetch direto (HTTP 403).** Números de downloads/endorsements de ReGen Valley vêm de snippets de busca e são confiança **medium**.
- A wiki em chinês retornou 403; o conteúdo citado sobre 1.7 vem de resultado de busca, não de leitura direta.
- Buscas localizadas em JA/ZH/RU/KO/ES/FR/DE/PT **não encontraram** cobertura não-inglesa específica de mods de soil health. Isso é um achado de ausência, não prova de ausência de demanda.
- Datas de patch móvel/console têm confiança menor que as de PC; a data de PC 1.6.15 é a mais firme.
- Nenhuma execução de jogo, compra, benchmark ou teste de performance foi feita neste refresh. Fatos externos não são evidência de que o Living Roots funciona.

### 2.4 Convenção de confiança

- **H** — fonte primária lida diretamente nesta sessão (site oficial, GitHub, DOI/PMC).
- **M** — fonte secundária corroborada ou número primário obtido por snippet.
- **L** — não verificado / inferência.

---

## 3. Refresh externo: o que mudou e o que não mudou

| Tema | Achado (2026-11-09) | Fonte | Conf. | Impacto para o Living Roots |
|---|---|---|---|---|
| Stardew Valley 1.7 | ConcernedApe descreveu a 1.7 como “looking to be the largest” update; janela provável **2027**, sem data. Conteúdo confirmado: nova farm type, Clint/Sandy, overhaul de crianças; nada anunciado sobre mudar o núcleo de solo/farming. | GamesRadar (2026-11-03), Denfaminicogamer (2026-11-04), Kotaku | H/M | Não migrar agora. O alvo 1.6.14+ continua válido durante 2026–2027; monitorar apenas o anúncio de data. |
| Patch atual de PC | **1.6.15** (2024-12-20). Console **1.6.15.1** (2025-02-26). iOS 1.6.15.1 por volta de 2026-03-19. Nenhum 1.6.16+ no PC. | stardewvalley.net/blog, wiki, SenseOS | H/M | Sem novo alvo de compatibilidade. |
| SMAPI estável | **4.5.2** (14/03/2026), requer SDV 1.6.14+. Há builds **4.5.3-alpha** em artifacts do GitHub (ex.: `4.5.3-alpha.20260928-1935`), mas **4.5.3 não é release estável**. | smapi.io, GitHub Releases, GitHub Actions | H | Manter 4.5.2 como baseline; não perseguir alpha. |
| Lista de compatibilidade | smapi.io mantém a lista em **sunset**; distribuição do conhecimento migrou para GitHub (`Pathoschild/StardewModDataset`, `SmapiCompatibilityList`). | smapi.io, GitHub | H | Não depender da lista como canal de descoberta. |
| ReGen Valley | Listado como **“Realistic Dirt to Soil Farming System”**; v0.51; 3 endorsements; MP “early testing”; quality/yield **farm-wide** em médias 75+/90+. | Nexus #39740 (snippet; fetch 403) | M | Concorrente direto segue ativo. Diferenciação continua sendo per-tile determinístico + loop composto + MP host-authoritative + evidência. |
| Servidor dedicado | **JunimoServer** ativo (repo `stardew-valley-dedicated-server/server`, MIT; Docker `sdvd/server`; stable `sdvd-server-v1.4.0`, preview `v1.5.0-preview.140`; JunimoTestClient HTTP :5123). | GitHub, Docker Hub, Survivalservers (2026-08) | H/M | Caminho operacional viável para D1, com custo de contas Steam + jogo + operação; nunca gate de PR. |
| Mercado de mods | Listas “best mods 2026” continuam dominadas por QoL. Não apareceu um segundo mod de agroecologia profunda além de ReGen Valley e adjacentes. | PCGamesN/Technwz/GameSpot, buscas multilíngues | M | O espaço segue aberto, mas a demanda por “soil health” não é massiva; o diferencial é coerência, não a palavra-chave. |
| Agronomia — mulch | Meta-análise (Fan et al., 2023): mulch reduz runoff em **47,4%** e perda de solo em **76,2%** em média. | ScienceDirect S0016706123003208 | H | Sustenta a mecânica de cobertura como proteção, não como buff mágico. |
| Agronomia — Three Sisters | Estudo multi-ano encontrou quedas de **88–95%** em feijão e **64–98%** em abóbora quando plantados no consórcio em relação ao monocultivo. | Plants, People, Planet, DOI 10.1002/ppp3.10576 | H | O consórcio deve ser modelado como tradeoff ecológico, não como buff universal de rendimento. |
| Agronomia — bee hotels | MacIvor & Packer (2015): hotéis de abelhas favorecem **vespas** e abelhas não-nativas mais que abelhas nativas. | PMC4364699 | H | Evitar tratar “hotel de abelhas” como solução automática; o briefing de insetos precisa de nuance. |
| Agronomia — matéria orgânica | Ensaios de longo prazo com esterco/leguminosas seguem mostrando ganho de SOC/N e resiliência, mas os números variam por solo e manejo. | Das et al. 2023 (UNL); Lorenz 2025 (ASA/CSSA/SSSA) | M | Reforça o eixo “composto melhora solo ao longo do tempo”; não usar números únicos como promessa. |

**O que não mudou:** o alvo técnico (SDV 1.6.14+ / SMAPI 4.5.2), a ausência de servidor dedicado oficial, a ausência de um concorrente com a combinação completa do Living Roots e a necessidade de D1/atestação humana para promover claims.

**O que mudou de ênfase:** com a 1.7 provavelmente em 2027, há uma janela de 12–18 meses para consolidar 1.6.x, publicar e validar antes de qualquer migração. Isso favorece “fechar e distribuir o que existe” em vez de “começar outra frente grande”.

---

## 4. Estado local fresco (2026-11-09)

### 4.1 Specs e tarefas

| Spec | Done/Total | Abertas | Estado |
|---|---:|---:|---|
| 001-soil-health-visualization | 10/86 | 76 | legado/parcial |
| 002-soil-decay-compost | 38/38 | 0 | **fechada** |
| 003-composting-tests | 28/117 | 89 | legado/parcial |
| 004-playable-compost-loop | 65/67 | 2 | quase fechada (T034 D1, T037 playtest) |
| 005-soil-health-to-crop-growth | 146/146 | 0 | **fechada** |
| 008-smoke-validation | 184/184 | 0 | **fechada** |
| 009-end-to-end-game-verification | 55/58 | 3 | manual pendente |
| 010-headless-verification-runner | 150/203 | 53 | funcional, escopo aberto |
| 011-local-oss-quality-gate | 42/88 | 46 | parcial |
| 012-sonarcloud-to-local-gate-baseline-readiness | 11/11 | 0 | **fechada** |
| 013-mulch-soil-cover | 46/46 | 0 | **fechada** |
| 014-soil-health-crop-quality | 131/133 | 2 | quase fechada (T109 D1, T110 deferido) |
| 015-harmony-harvest-seam | 58/65 | 7 | parcial (D1 e resíduos) |
| 016-gate3-runner-hardening | 43/43 | 0 | **fechada** |
| 017-joja-agrochemical-tradeoff | 127/127 | 0 | **fechada** |
| 018-machine-pass-full-coverage | 24/28 | 4 | farmhand-guards blocked-feasible |
| 019-ci-release-gate | 5/7 | 2 | ações de owner |
| 020-heirloom-seeds-pipeline | 35/35 | 0 | **fechada** |

**Totais:** 1198 done / 284 open / 1482 (80,8%). **Trilha ativa (002 + 004–020): 968/988 = 98,0%.** Specs fechadas: **8/20 = 40%**.

### 4.2 Bugs

| Grupo | Quantidade | Leitura |
|---|---:|---|
| Assessment + Fix + Test | 30/40 | 75% do fluxo completo |
| A-only | 5 | assessment sem fix/test |
| AF-only | 2 | fix sem test |
| FT-only | 1 | test obsoleto (T072 fechou o bug de cache) |
| Vazios | 2 | `composting-bin-complexity-203`, `compostingbin-s2583-unreachable-2` |
| **Total** | **40** | — |

Resultados em `test.md`: 17 `verified` + 1 `verified (bug-specific)` + 1 `pass` = **19/40 (47,5%) positivos**; 10 `partial`; 1 `failed` obsoleto; 10 sem resultado explícito.

### 4.3 Gate 3 (machine-pass host-only)

| Spec | Run ID | Bundle | Veredito | Honesty | Cobre |
|---|---|---|---|---|---|
| 004 | `run-20261109T034318Z-7374` | `...-7374` | pass | R | composto → crescimento (moderado ×1,00, banda [31,71]) |
| 005 | `run-20261109T034443Z-4cc1` | `...-4cc1` | pass | R | saúde → fase/faseDays |
| 013 | `run-20261109T040327Z-1ea4` | `...-1ea4` | pass | R | borda dos 7 dias do mulch |
| 014 | `run-20261109T041048Z-088b` | `...-088b` | pass | R | qualidade/extra de colheita |
| 015 | `run-20261109T032903Z-29cf` | `...-29cf` | pass | R | seam Harmony |
| 017 | `run-20261109T041859Z-6938` | `...-6938` | pass | R | tradeoff agroquímico (recusa em 0; caminho 50→40 no bundle antigo `7f35`) |
| 020 | `run-20261109T155608Z-6021` | `...-6021` | pass | R | plantio heirloom 50→52 |

**Cobertura machine-pass:** 7/20 specs (35%). **Human-verified:** 0/20. **D1:** 0. **SC-004:** 0.

Limites específicos que não podem ser reinterpretados:

- **020** verifica só o **plantio**. Colheita com bônus, semente salva, D1 e aquisição natural (Traveler, primavera, 35g, estoque 5) **não** têm Gate 3; a aquisição repousa em testes de contrato + `lr_give_heirloom_seed`.
- **017** verifica o item e a penalidade, não o sistema Joja/monocultura completo.
- **004/013** provam o loop sob o runner, não a legibilidade/UX humana.
- **014/015** provam os predicados e o seam, não a convergência ao vivo com farmhand.

### 4.4 Git e distribuição

- `dev` = `origin/dev` = `cd90f2f`; 0 commits à frente; árvore limpa.
- 0 tags; 0 releases públicas; 0 PRs abertos.
- Manifest `0.1.0`; `MinimumApiVersion` 4.4.0; `MinimumGameVersion` 1.6.14.
- `LivingRoots/bin/` contém `LivingRoots 0.1.0.zip`, `manifest.json`, DLL, `i18n/` e `Assets/` (artefatos gitignored).
- `README.md` corrigido para instalação **from source**, porque a página de Releases está vazia.

---

## 5. Matriz de cobertura (percentuais por denominação)

| Dimensão | Numerador / denominador | % | O que mede | Cuidado |
|---|---:|---:|---|---|
| Tarefas da trilha ativa | 968 / 988 | **98,0%** | execução do escopo não-legado | não é “98% do jogo”; mede tarefas |
| Tarefas totais | 1198 / 1482 | **80,8%** | todo o histórico de specs | denominador inclui 001/003/010/011 legados |
| Specs fechadas | 8 / 20 | **40,0%** | specs com 0 tarefas abertas | fechar tarefa ≠ fechar escopo |
| Gate 3 machine-pass | 7 / 20 | **35,0%** | specs com veredito automatizado | host-only, honesty R |
| Gate 3 human-verified | 0 / 20 | **0%** | atestação humana/E | nenhum cenário promovido |
| D1 multiplayer | 0 / 1 | **0%** | convergência host+farmhand | bloqueia 014/015/018 |
| Bugs com A+F+T | 30 / 40 | **75,0%** | fluxo completo de bug | 2 dirs vazios, 1 teste obsoleto |
| Bugs com resultado positivo | 19 / 40 | **47,5%** | `verified`/`pass` | 10 `partial`, 1 `failed` obsoleto |
| Features nomeadas | 6 / 21 | **28,6%** | compostagem, mulch, crescimento, qualidade, Joja MVP, heirloom | heirloom é pipeline, não arco completo |
| Progressão early | 2 / 7 | **28,6%** | compostagem e mulch | item Joja é tradeoff opcional |
| Progressão mid | 0 / 6 | **0%** | rotação/policultura/animais etc. | não iniciado |
| Progressão end | 0 / 6 | **0%** | CSA/soberania/cozinha etc. | não iniciado |

**Por que os números divergem:** a trilha ativa está quase fechada, mas o projeto como um todo não está. “98%” responde “quanto do escopo ativo de engenharia foi executado?”; “40%” responde “quantos specs foram encerrados?”; “0%” responde “quanto foi atestado por humano ou por farmhand real?”. Nenhum desses números deve ser citado sozinho.

---

## 6. O que falta, por camada

### 6.1 Evidência (o gargalo mais barato de resolver)

1. **Atestação humana/E** para os 7 cenários `machine-pass` — roteiro manual de 3 sinais; nenhum foi executado.
2. **D1 real** — sessão host+farmhand; duas instâncias SMAPI sob Xvfb ou JunimoServer.
3. **SC-004 (017)** — protocolo de compreensão escrito, nenhuma execução.
4. **T110 (014)** — decisão documental sobre o contrato histórico 34/67.
5. **004 T034/T037** — D1 e playtest.
6. **009 T039/T040/T052** — sessão SMAPI manual.
7. **T133 (004)** — paridade unidirecional Machine/validator (limitação de API).

### 6.2 Distribuição

- Sem tag git e sem release pública; `0.1.0` é interno.
- `README.md` sem caminho de instalação para usuário final (agora corrigido para build from source).
- Sem canal Nexus/CurseForge; sem pacote de release assinado/atestado.
- CI remoto dormente para build/test (depende de `BUILDDEPS_ENABLED` + token); `Verify Scenario` é best-effort; `Check Formatting` é o único always-on.

### 6.3 Gameplay core

- **Pest system** (Poor = pragas) não existe.
- **Monocultura** não é detectada; só tile nu decai.
- **Rotação, policultura, adubo verde, manejo animal** — quatro promessas “BUILDS” não implementadas.
- **Joja/monocultura completa** — só o MVP agroquímico existe.
- **Living Soil “new interactions”** — deferido.
- **Heirloom** — plantio/semente salva implementados; colheita, aquisição natural, D1 e balanceamento não verificados por Gate 3.

### 6.4 Conteúdo e progressão

- Elena, Welcome Kit, Three Sisters, cisterna, farmácia viva, Adobe Shed/Green Roof, ICLF, greywater, feira semanal, CSA, cozinha coletiva, soberania alimentar, mais heirloom crops.
- Mid-game 0/6; end-game 0/6.
- 31 briefings em `specs/future-roadmap/briefings/`, 0 implementados.

### 6.5 Infra e higiene

- 010: 53 tarefas abertas (runner funcional; escopo de verificação/promoção incompleto).
- 001: 76 abertas; 003: 89; 011: 46 — trilhas antigas que inflam o denominador total.
- 5 bugs A-only, 2 AF-only, 1 FT obsoleto, 2 diretórios vazios.
- Sem migração para 1.7 — desnecessária até o anúncio de data.

---

## 7. O que o refresh externo muda no plano

1. **Reforça “publicar e atestar” em vez de “abrir nova frente”.** A janela até 1.7 (provável 2027) é longa o suficiente para consolidar 1.6.x, mas curta o suficiente para não começar uma reescrita.
2. **Não muda o alvo técnico.** SDV 1.6.15 + SMAPI 4.5.2 segue correto; 4.5.3-alpha não é motivo para bump.
3. **Reforça o diferencial, mas exige nuance agronômica.** Mulch tem base forte; Three Sisters e bee hotels têm evidência que **contraria** versões romantizadas. O design deve modelar tradeoffs, não prometer buffs universais.
4. **D1 tem caminho operacional claro.** JunimoServer ou duas instâncias Xvfb; o custo é operacional, não de pesquisa.
5. **A ausência de cobertura não-inglesa é uma oportunidade, não uma prova.** PT/EN com framing educativo é diferenciado, mas não há dado que estime demanda.
6. **O concorrente direto segue ativo e com MP em “early testing”.** A janela de diferenciação por MP host-authoritative verificado é real, mas só se D1 for atestado.

---

## 8. Prioridades 80/20 (ordem recomendada)

| # | Ação | Custo | Impacto | Pré-requisito | Evidência de saída |
|---|---|---|---|---|---|
| 1 | **Decidir publicação:** manter `0.1.0` interno ou criar tag/release pública | S | Destrava distribuição e install | decisão do maintainer | tag + release notes ou registro explícito de “não publicar” |
| 2 | **Corrigir README/install** para build from source e status honesto | XS | Elimina link morto e expectativa falsa | — | README atualizado (feito nesta atualização) |
| 3 | **Atestação humana/E** de 4 cenários já `machine-pass` (004, 005/013, 014, 017/020) | M | Promove `machine-pass` → `verified` | roteiro 3 sinais | `test.md` com reviewer/data/session |
| 4 | **D1 real** host+farmhand (Xvfb duplo ou JunimoServer) | M–L | Destrava 014/015/018 e o pilar MP | ambiente + contas | bundle/claim D1 |
| 5 | **Fechar/documentar resíduos** 004/014/015/018/019 e decidir escopo de 010 | S–M | Reduz “aberto” sem código novo | — | tasks atualizadas + converge |
| 6 | **Próximo slice de gameplay:** rotação/policultura MVP **ou** Joja/monocultura completa | M | Fecha uma promessa “BUILDS” ou “DESTROYS” | 020 já entregou a base heirloom | spec + Gate 3 |
| 7 | **Preparação 1.7:** monitorar anúncio de data; não migrar | 0 | Evita retrabalho | — | nota de monitoramento |
| 8 | **Limpeza de legado:** 001/003/010/011 e bugs incompletos | M | Melhora o denominador e a higiene | — | tasks/bugs fechados ou arquivados |

**Sequência 80/20:** 1 → 2 → 3 → 4 → 5. O item 6 só depois de 3/4, porque adicionar gameplay sem atestação aumenta a dívida de evidência.

**O que não fazer agora:** Elena/NPC (custo alto), mid/end-game completo (12 features), migração 1.7 (sem data), reescrita de persistência para zero medido (migração cara, impacto pequeno).

---

## 9. Fontes (acesso 2026-11-09)

| Fonte | Uso | Conf. |
|---|---|---|
| https://www.gamesradar.com/games/simulation/stardew-valley-update-1-7-is-looking-to-be-the-largest-yet-concernedape-says-and-itll-probably-come-out-sometime-next-year/ | 1.7 “largest”, janela 2027 | H |
| https://news.denfaminicogamer.jp/news/2611042i | 1.7 “maior de sempre”, 2027 | H |
| https://www.4gamers.com.tw/news/detail/83081/ | 1.7 maior, Clint/Sandy | M |
| https://kotaku.com/stardew-valleys-1-7-update-finally-has-a-release-window-and-its-set-to-be-the-games-largest-update-ever-2000753367 | janela 2027 | M |
| https://www.stardewvalley.net/blog/ | patch notes oficiais; 1.6.15.1 console | H |
| https://stardewvalleywiki.com/Console_Version_History | 1.6.15.1 em 2025-02-26 | H |
| https://smapi.io/ | SMAPI 4.5.2, SDV 1.6.14+ | H |
| https://github.com/Pathoschild/SMAPI/releases/tag/4.5.2 | release estável | H |
| https://github.com/Pathoschild/StardewModDataset | toolkit/SMAPI 4.5.3 em desenvolvimento | H |
| https://www.nexusmods.com/stardewvalley/mods/39740 | ReGen Valley (snippet; fetch 403) | M |
| https://github.com/stardew-valley-dedicated-server/server | JunimoServer | H |
| https://hub.docker.com/r/sdvd/server | imagem Docker | H |
| https://www.sciencedirect.com/science/article/pii/S0016706123003208 | mulch: −47,4% runoff, −76,2% solo | H |
| https://nph.onlinelibrary.wiley.com/doi/full/10.1002/ppp3.10576 | Three Sisters: quedas de 88–95%/64–98% | H |
| https://pmc.ncbi.nlm.nih.gov/articles/PMC4364699/ | bee hotels favorecem vespas | H |
| https://digitalcommons.unl.edu/cgi/viewcontent.cgi?article=2609&context=agronomyfacpub | esterco de longo prazo melhora SOC/saúde | M |
| https://acsess.onlinelibrary.wiley.com/doi/am-pdf/10.1002/saj2.70000 | 34 anos de green manure/SOC | M |

---

## 10. Limitações e o que mudaria as conclusões

- **Contagens de tarefas medem checkboxes, não valor entregue.** Specs legados podem ter tarefas abertas que já não importam; specs fechados podem ter escopo incompleto.
- **`machine-pass` é forte para os sinais declarados, não para o jogo inteiro.** Ele não vê UX, balanceamento, aquisição natural, colheita heirloom ou convergência MP.
- **Bundles são host-only e gitignored.** A reprodutibilidade depende do runner e do ambiente; não são attestation externa.
- **Nexus 403** impede confirmar downloads/endorsements atuais de ReGen Valley; os números são de snippet.
- **Buscas multilíngues** não são exaustivas; a ausência de cobertura localizada não prova ausência de mercado.
- **1.7 sem data** pode escorregar para 2028; a recomendação de “não migrar agora” continua válida em qualquer cenário até o anúncio de data.
- **A conclusão mudaria** se: (a) a 1.7 ganhar data próxima e quebrar APIs de solo/farming; (b) D1 se mostrar inviável no host; (c) um concorrente lançar per-tile + MP host-authoritative + verificação; (d) o maintainer decidir não publicar — nesse caso o 80/20 vira fechamento documental, não distribuição.

---

*Última atualização: 2026-11-09. Evidência local fresca: build 0 erros, 1997/4/0, format exit 0, `dev` = `origin/dev`. Evidência end-to-end: 7 specs `machine-pass` host-only, 0 human-verified, 0 D1. Nenhuma claim foi promovida.*
