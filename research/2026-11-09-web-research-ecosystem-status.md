# Web research — ecossistema e estado externo (2026-11-09)

> **Escopo:** refresh atualizado (nov/2026) de `research/2026-11-05-web-research-ecosystem-status.md` — SDV/SMAPI baseline, ecossistema de mods, concorrência, distribuição e **caminho para verificação multiplayer (D1)**.
> **Método:** 2 agentes de pesquisa em background (EN + tentativas PT-BR), `websearch`/`webfetch`, chamadas à API pública do GitHub/NuGet/Docker Hub; fontes primárias sempre que possível. **Toda a informação não verificada ao vivo é marcada como tal; fontes bloqueadas (Nexus/wiki/Patreon/CurseForge/Cloudflare) são registradas, não presumidas.**
> **Arquivos brutos:** `research/2026-11-09-ecosystem-market.md` (mercado/ecossistema) e `research/2026-11-09-technical-verification.md` (técnico/verificação). Este arquivo é a síntese honesta para o projeto.
> **Convenção de confiança:** high = fonte primária acessada hoje; medium = secundária/indireta; low = teoria comunitária.

## 1. Fatos verificados (high/medium, acesso 2026-11-09)

### Base técnica
| Tema | Fato | Fonte | Conf |
|---|---|---|---|
| SDV PC atual | **1.6.15** (2024-12-20); mobile 1.6.15; console 1.6.15.1 (fev/2025). Sem 1.6.16+ | stardewvalley.net; TheGamer; Wikipedia | high |
| SDV — 10º aniversário | 2026-02-26; ConcernedApe: **~50M cópias**; jogo recebe updates gratuitos | stardewvalley.net (anniversary), Wikipedia | high |
| SDV — 1.7 | **Anunciada, em desenvolvimento, sem data** (Clint/Sandy marriage candidates, children overhaul, new farm type; anúncios fev/2026; riddle set/2026; reportagem nov/2026: "provavelmente 2027"). Nada anunciado de soil/farm core | Game Informer; dotEsports; GamingBible; GamesRadar+ via n4g.com; stardewvalley.net | medium-high (existência/conteúdo) / low (data) |
| SDV — 1.6 endgame | Golden Joja Parrot + Desert Festival; **nenhuma mecânica de soil health** oficial | Stardew Wiki (via busca); stardewvalleywiki | high (conteúdo) |
| SMAPI atual | **4.5.2** (criado 2026-03-14, publicado 2026-03-22; release-notes dizem 14 Mar) — requer **SDV 1.6.14+**; GitHub asset com **349.857 downloads** | GitHub releases/API; smapi.io | high |
| SMAPI 4.5 | Config in-game via GMCM; builds atestados; "SMAPI for developers" **aposentado**; detecção de arquivos maliciosos; performance | GitHub release notes 4.5.0 | high |
| Harmony | SMAPI 4.5.2 empacota **Lib.Harmony 2.2.2** (`0Harmony.dll`, 2.2.2.0); `EnableHarmony=true` liga a ele (não a NuGet) | raw 4.5.2 tree; smapi.targets; NuGet | high |
| Mod build practice | net6.0 + ModBuildConfig **4.4.0** (atual, 2025-04-13) — usado pelo próprio JunimoTestClient | repo csproj; NuGet | high |
| Dedicated server | **Não existe oficial**; jogo é host-based P2P; "dedicated" = processo host headless | hosts comerciais; Wikipedia | high/medium |

### Ecossistema & mercado
| Tema | Fato | Fonte | Conf |
|---|---|---|---|
| Mods criados | **>30.000** (2026) | Wikipedia | medium |
| SVE | **Oficialmente completo em 2026**; >4M downloads (multi-plataforma) | Technwz (2026-06-04); Wikipedia | medium |
| SteamCharts | ~50k avg concurrent; pico all-time 236.614 (1.6 launch, mar/2024) | steamcharts.com | high |
| Compat list | **Sunset** — "no longer comprehensively updated… future in doubt"; comunidade migra para Nexus/GitHub/`Pathoschild/SmapiCompatibilityList` (PRs) | smapi.io/mods | high (fato) |
| Co-op PC | 1.6: **8 jogadores**; consoles 4 (Switch 2: 8); mobile MP experimental | stardewvalley.net; Wikipedia | high |
| JunimoServer | Repo **movido** para `github.com/stardew-valley-dedicated-server/server` (MIT, 383★, push 2026-11-08, ativo); stable mais recente **`sdvd-server-v1.4.0`** (2026-01-17); docs avisam *"latest release is unstable — use preview builds"*; preview atual `v1.5.0-preview.140`; Docker Hub `latest` stale (2026-01-07) | GitHub API/docs/hub.docker.com | high |
| JunimoTestClient | Mod in-repo (MIT, net6.0, ModBuildConfig 4.4.0, EnableHarmony) — HTTP API :5123 (navegação de menu, personagem/movimento, chat, coop join/auth, screenshots, god-tools) | raw tree; manifest | high |
| D1 viável hoje | JunimoServer executa **cliente automatizado** (Testcontainers + xUnit, long-poll REST, `/diagnostics/state`, screenshots/vídeo, CTRF) — mas **manual/VPS, nunca PR-gate**, exige Steam accounts + jogo próprio | docs/e2e-testing | high |
| SMAPIDedicatedServerMod | **Morto** — v1.0.1 (2024-02-07), net5.0/ModBuildConfig 4.1.0, sem releases, issues abertas em 2026; compat SMAPI 4.5/SV 1.6 **não verificada** | GitHub API | high |
| ValleyServer | Lixeer (Apache-2.0, 580★); 2.x maintenance + MOD/Docker; 3.x "experimental/not for production"; v3.0.3 (2026-09-20) | GitHub | high |
| ALOS/Always On Server | Linha legada mantida por forks (ALOS, StardewUnattendedServer, puppy-stardew-server + Docker/Xvfb); sem API de automação; modo "freeze quando vazio" | Nexus via busca; forks GitHub | medium |
| Norm de CI na comunidade | **Pathoschild/StardewMods**: CI com `StardewModders/mod-reference-assemblies` + `dotnet test` (NUnit), **jogo nunca lançado**; SVE e GMCM: **sem workflows** | .github/workflows; API | high |
| ReGen Valley (#39740) | v0.51, tag 1.6-compatible, MP "early testing", **3 endorsements**, DLs ocultos; per-tile soil health + cover crops + grazing; upload ~nov/2025 | Nexus via busca (403 direto) | medium |
| Adjacentes | Crop Quality #47141 (jun/2026, ~0,9k DLs); Abundant Harvest Fertilizers #51999 (MIT; ~0,1k DLs); Quality Multi-Harvest #54375 (out/2026; ~0,1k DLs); **Ultimate Fertilizer #21318 (862 endorsements)**; Compost Pests and Cultivation #4446 (**morto desde 2019**) | Nexus via busca (403 direto) | medium |
| Wiki/Nexus | stardewvalleywiki.com **403 (Cloudflare)**; Nexus **403**; Patreon sunset post **403**; CurseForge **403**; PT-BR news **0 resultados** | fetches ao vivo | high (fato do bloqueio) |

## 2. O que isso significa para o Living Roots (honesto)

1. **Baseline segue correto:** SDV 1.6.14+ / SMAPI 4.5.2 é exatamente o publicado e estável. O próximo evento é o **1.7 (provável 2027)** — qualquer 1.7 preview muda SMAPI/Harmony/assemblies; **reteste pós-1.7 é obrigatório** antes de declarar "compatível".
2. **D1 (T109) tem caminho técnico concreto e viável:** ou (a) **JunimoServer** (server + client containers + JunimoTestClient; REST diagnostics; pin em `v1.5.0-preview.x`; Steam accounts + jogo + VPS; manual/VPS, não PR CI), ou (b) **fallback sem infra nova**: duas instâncias SMAPI locais (host + farmhand) sob Xvfb na mesma máquina — o padrão X11/Openbox que o JunimoServer usa em produção e que o nosso runner (spec 010) já replica. Não depende de 1.7 nem de dedicated server oficial.
3. **CI pode parar de depender de assemblies proprietárias:** `StardewModders/mod-reference-assemblies` (Pathoschild) permite `dotnet test` em CI sem o jogo — padrão do ecossistema; hoje nosso CI `Build and Test` está dormente por falta de builddeps; essa é a alternativa legítima (decidido pelo maintainer).
4. **Distribuição mudou de forma:** compat list em sunset ⇒ reputação via **GitHub Releases + página Nexus com requisitos explícitos + PR no `SmapiCompatibilityList`**. O asset GitHub do SMAPI 4.5.2 com ~350k downloads mostra demanda contínua de instalação.
5. **Gap competitivo persiste, nicho pequeno:** ReGen Valley (3 endorsements, MP early-testing) valida o conceito sem tração de mercado. Não há concorrente vivo com per-tile soil → growth/harvest/quality + compost loop + MP host-authoritative + verificação automatizada. Expectativa honesta de adoção: **centenas a poucos milhares de downloads**.
6. **Joja/CC route:** a comunidade deseja extensões da rota Joja (teoria do riddle 1.7; sentimento pró-Joja). O arco "libertar o vale via solo" **alinha com desejo declarado** — usar como framing de lançamento, sem prometer o que o 1.7 pode ou não fazer.
7. **Honestidade permanece:** machine-pass ≠ verified; D1 nunca executado; 1.7 timing desconhecido; nenhum número de download do Nexus verificável ao vivo.

## 3. Limitações desta pesquisa (registradas, não presumidas)

- **Nexus/CurseForge/wiki/Patreon:** 403 em todas as tentativas; números de mods via **snippets de busca** — trate como ordem de grandeza, não decimais.
- **ReGen Valley:** data de atualização/DLs não revalidadas ao vivo; "v0.51, ~nov/2025, 3 endorsements" = snippet + registro do repo.
- **PT-BR:** zero resultados relevantes — cobertura de 1.7/ecossistema é EN-first; lacuna registrada.
- **SMAPI 4.5.2 date drift:** GitHub tag (2026-03-22) vs release-notes ("14 March 2026"); substância concorda.
- **JunimoServer:** docs recomendam preview (stable "unstable"); Docker `latest` stale; Steam tokens expiram; CI próprio é manual/maintainer-gated.
- **1.7:** existência e conteúdo = high; **data = unknown** — sem sinal para 1H2027.
- **GitHub API** rate-limited ao fim da sessão (não autenticada); code search 401.

## 4. Implications → ações (ver `OBJETIVO.md` §8 e `ROADMAP.md` "Recommended next action")

1. Publicar 0.1.0 (decisão maintainer) + GitHub Release com requisitos + `SmapiCompatibilityList` PR.
2. D1: manter JunimoServer (ou fallback two-Xvfb + JunimoTestClient standalone) como **próximo passo de verificação** — environment/Steam required; não é trabalho de CI.
3. Considerar **mod-reference-assemblies** para destravar CI build/test (substitui/complementa builddeps dormente) — decisão de maintainer.
4. Atualizar compat/CI claims sempre que SMAPI/1.7 mudar; Harmony 2.2.2 é o alvo atual, não NuGet 3.x RCs.
5. Re-executar esta pesquisa quando: 1.7 preview sair; ReGen Valley atualizar para v0.6+; ou a compat list sunset for resolvida/substituída.

## 5. Referências

- SDV/SMAPI: https://smapi.io/ · https://smapi.io/mods · https://github.com/Pathoschild/SMAPI/releases/tag/4.5.2 · https://stardewvalley.net/ · https://en.wikipedia.org/wiki/Stardew_Valley · https://steamcharts.com/app/413150
- JunimoServer: https://github.com/stardew-valley-dedicated-server/server · https://docs.junimoserver.com/ · https://hub.docker.com/r/sdvd/server
- Community/dedicated: https://github.com/ObjectManagerManager/SMAPIDedicatedServerMod · https://github.com/Lixeer/ValleyServer · https://github.com/Pathoschild/SmapiCompatibilityList · https://github.com/StardewModders/mod-reference-assemblies
- Competitive: https://www.nexusmods.com/stardewvalley/mods/39740 · https://www.nexusmods.com/stardewvalley/mods/21318 · https://www.nexusmods.com/stardewvalley/mods/47141 · https://www.nexusmods.com/stardewvalley/mods/51999 · https://www.nexusmods.com/stardewvalley/mods/54375 · https://stardewmodding.wiki.gg/wiki/Permaculture_Farming
- 1.7: https://www.gamingbible.com/gaming/stardew-valley-1-7/ · https://dotesports.com/stardew-valley/news/stardew-valley-1-7-marriage-candidates-clint-sandy-update-patch-note-concerns/ · https://www.stardewvalley.net/stardew-valley-10-year-anniversary
- Local: `research/2026-11-09-ecosystem-market.md`, `research/2026-11-09-technical-verification.md`, `research/2026-11-05-web-research-ecosystem-status.md`, `research/2026-10-roadmap-feasibility-and-agronomy.md`

*Atualizado: 2026-11-09 (acesso às fontes nesta data; limitações em §3).*
