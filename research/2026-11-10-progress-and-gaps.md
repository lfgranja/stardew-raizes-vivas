# Living Roots — quanto percorremos e o que falta (2026-11-10)

> **Escopo:** quanto do caminho já foi percorrido, com que tipo de evidência, e o que ainda falta — combinando re-execução local fresca, artefatos commitados e refresh web multilíngue (mercado, 2026-11-10; ecossistema/D1, baseline 2026-11-09).
> **Regra de honestidade:** `machine-pass` ≠ `verified`. Nenhuma claim neste arquivo promove `machine-pass` a atestação humana, D1 ou SC-004.
> **Snapshot local:** `dev` = `origin/dev` = `c07fbab`; árvore limpa; 0 commits à frente; **0 tags; 0 releases públicas**; 31 briefings em `specs/future-roadmap/briefings/`.

---

## 1. Resumo executivo

- **O núcleo jogável existe e está verde em código/testes.** Solo 0–100 por tile, compostagem, mulch, crescimento dirigido por saúde, qualidade/extra de colheita, tradeoff agroquímico Joja e sementes crioulas estão implementados; a suíte fresca é **1997 passed / 0 failed / 4 skipped (2001)**.
- **A trilha ativa está 98% fechada:** 968/988 tarefas (98,0%). O número menor, **1198/1482 = 80,8%**, inclui as trilhas legadas (001, 003, 010, 011) responsáveis pela quase totalidade do aberto restante.
- **A evidência end-to-end é forte, mas estreita:** 7 specs têm Gate 3 `machine-pass` host-only (004, 005, 013, 014, 015, 017, 020). **Nenhum** tem atestação humana/E; **nenhum** tem D1 (farmhand real); **nenhum** tem SC-004.
- **O projeto está pronto para liberação técnica, não para adoção pública:** release 0.1.0 existe só no repositório, sem tag e sem canal público. Proveniência em aberto: o HEAD contém a spec 020, mas o manifest ainda diz 0.1.0 e o zip `LivingRoots 0.1.0.zip` já contém assets 020.
- **O refresh externo de mercado (2026-11-10) rebaixa a confiança do concorrente direto e confirma que o nicho de soil health continua sem líder standalone e sem traduções não-inglesas:** o Nexus bloqueou o fetch direto (HTTP 403) e não há resultados web/fórum para "ReGen Valley"; a atenção da categoria foi para **Wildroot Chronicles** (breeding) e **Harvest Ledger** (economia + rotação, **traduzido PT-BR**, 15 endorsements). A wiki oficial retornou 403 em 4 tentativas; SMAPI 4.5.2 e SDV 1.6.15 permanecem a baseline técnica; 1.7 anunciada em 30/08/2025 sem data/RC público; lista de compatibilidade smapi.io em **sunset confirmado** — **o update check NÃO foi descontinuado**. **Nenhuma mudança em SDV/SMAPI** que afete o Living Roots — guia oficial de migração 1.7 não publicado/verificado; 1.6.16 sem RC.
- **O 80/20 continua sendo "converter evidência em decisão e publicar", não "escrever mais uma feature":** tag/release 0.2.0 com notas honestas, atestação humana dos cenários já cobertos e D1; a feature certa de seguida é rotação/policultura (0.3.0).

---

## 2. Evidência local fresca (executado nesta sessão, 2026-11-10T02:21–02:40Z)

| Verificação | Resultado |
|---|---|
| `./scripts/release-gate.sh` (`GamePath=.../Stardew Valley`) | **`ALL STEPS PASSED`** — commit `c07fbab5830055701a72f609413a5935abb7a8d1` |
| Step 1: `dotnet format --verify-no-changes` | OK |
| Step 2: `dotnet restore --locked-mode` | OK |
| Step 3: build Release (`-m:1 --disable-build-servers`) | OK |
| Step 4: `dotnet test` | **1997 passed / 0 failed / 4 skipped (2001)**; duração 39 s |
| Step 5: smoke launch-free | **51 arquivos: 50 pass, 0 failed, 1 skip** |
| Step 6: docs markers (`--check-docs`) | OK |
| Step 7: protected paths / secrets | OK |
| Git | `dev` = `origin/dev`; `git rev-list --count origin/dev..dev` = **0**; árvore limpa; `git tag --list` = **0** |
| Proveniência | HEAD contém spec 020 (35/35); `LivingRoots/manifest.json` = `0.1.0`; `LivingRoots/bin/LivingRoots 0.1.0.zip` contém `Assets/Crops/heirloom_crops.png` e `heirloom_*.png` com manifest interno `0.1.0`; `.specify/releases/` só tem o registro 017 |

### 2.1 Promoções de evidência registradas (artefatos)

- `specs/*/tasks.md` — contagens por spec (abaixo).
- `specs/*/test.md` e `specs/*/verification/host-only-gate3.md` — Gate 3 `machine-pass` host-only para **004 `7374`**, **005 `4cc1`**, **013 `1ea4`**, **014 `088b`**, **015 `29cf`**, **017 `6938`**, **020 `6021`** (todos `verdict: pass`, honesty R).
- `specs/020-heirloom-seeds-pipeline/scenarios/heirloom-plant-bonus.scenario.json` — único cenário 020; cobre **só o plantio** (50→52); colheita, semente salva, aquisição natural e D1 sem Gate 3.
- `docs/adr/001-harmony-for-harvest-interception.md` — a única ADR do projeto.
- `.specify/validations/017-joja-agrochemical-tradeoff/validate.md` e `.specify/validations/020-heirloom-seeds-pipeline/validate.md` — as duas únicas validações humanas de spec (aprovação de release, não verificação de gameplay).

### 2.2 Formato (as contagens)

| Métrica | Valor | % |
|---|---:|---:|
| Tarefas abertas | 284 | |
| Tarefas totais | **1198/1482** | **80,8%** |
| Trilha ativa (sem 001/003/010/011) | **968/988** | **98,0%** |
| Specs fechadas | **8/20** (002, 005, 008, 012, 013, 016, 017, 020) | 40,0% |
| Gate 3 `machine-pass` | **7/20** | 35,0% |
| Gate 3 `human-verified` | **0/20** | **0%** |
| D1 (farmhand real) | **0/1** | **0%** |
| SC-004 (017) | **0/1** | **0%** |
| Bugs A+F+T | **30/40** | 75,0% |
| Bugs com resultado positivo | **19/40** | 47,5% (17 `verified` + 1 `verified (bug-specific)` + 1 `pass`) |
| Bugs partial | 10 | 25,0% |
| Bug vazios / obsoletos | 2 vazios + 1 obsoleto (`visualization-cache-invalidation-failures`, failed/not-applied) | |
| Bugs assessment-only / sem teste | 5 (`codeql-code-scanning-failure`, `composting-*-complexity`, `gate3-capture-path`) / 2 AF (`compostingbin-s2583-unreachable`, `sonarcloud-quality-gate-failure`) | |

---

## 3. Quanto do caminho já percorremos

### 3.1 Produto (implementado e coberto por teste)

| Feature | Spec | Estado |
|---|---|---|
| Núcleo de solo 0–100/tile, decay sazonal, overlay/tooltip/hoe | 002 | ✅ 38/38; healthy |
| Compost loop (craie→placement→carga→2 dias→collect→+15) | 004 | ✅ 65/67 (T034 D1, T037 playtest abertos); machine-pass `7374` |
| Mulch (reduz/evita decay) | 013 | ✅ 46/46; machine-pass `1ea4` |
| Crescimento dirigido por saúde (1.15x/1.00x/0.85x) | 005 | ✅ 146/146; machine-pass `4cc1` |
| Qualidade/extra de colheita em tiers 31/71 | 014 | ✅ 131/133 (T109 D1, T110 deferido); machine-pass `088b` |
| Harmony harvest seam (qualidade + feedback procedural) | 015 | ✅ 58/65 (D1 + UX pendentes); machine-pass `29cf` |
| Joja/agrochemical tradeoff (penalidade bounded, recusas, feedback) | 017 | ✅ 127/127; machine-pass `6938`; release 0.1.0 interna; validate aprovado |
| Heirloom seeds pipeline (plantio +2, semente salva, registro) | 020 | ✅ 35/35; machine-pass `6021` (**só plantio**); validate aprovado |
| Quality gate local (Sonar→OSS local) | 011/012 | 🟡 42/88 + 11/11; `011-…-baseline-readiness/` vazio |
| Headless verification runner hardening | 010/016 | 🟡 150/203 + 43/43; 53 abertas de escopo |
| End-to-end verification manual | 009 | 🟡 55/58 (T039/T040/T052 manuais) |
| CI/release gate | 019 | 🟡 5/7 (T006/T007 ação do owner) |

### 3.2 Código × verificação (o que está realmente comprovado)

- **Determinístico**: build Release 0 erros, testes 1997/0/4, format exit 0, smoke 50/51, gate ALL STEPS PASSED no HEAD.
- **Real-game headless**: 7 cenários `machine-pass` host-only (evidência, não prova humana; honesty R).
- **Humano**: nenhum cenário `verified`. **SC-004**: não executado. **D1**: nunca houve um farmhand real conectado.

### 3.3 Produto vs. visão

- **Early/mid/end: 2/7 · 0/6 · 0/6.** Compost e mulch entregues; crescimento/qualidade fechados; o restante da progressão está por fazer.
- **31 briefings** em `specs/future-roadmap/briefings/` — apenas heirloom (020) e o MVP Joja (017) cruzaram o funil assess→specify→implement; o resto continua briefing.

---

## 4. Refresh multilíngue 2026-11-10 (delta vs. 2026-11-09)

### 4.1 Mercado/competitivo (confiança H/M/L; M = snippets, Nexus 403)

| Item | Achado | Confiança |
|---|---|---|
| ReGen Valley (#39740) | **Não verificável**: 403 em todas as páginas e zero resultados web/fórum/ModDrop/CurseForge/GitHub para o mod; v0.51/3 endorsements do baseline não confirmado independentemente | L |
| Wildroot Chronicles (#48079, ex-Cropgenics) | 1.3.4, ~ago/2026, **78→85 endorsements**, 354 posts, SpaceCore/MP, nativo 简体中文 — ocupa a atenção da categoria (breeding 1.4 preview ~out/2026) | H/M |
| Harvest Ledger (#48951) | 0.5.0 (2026-07-10), update 2026-08-11, **15 endorsements**, economia + **rotação host-authoritative**, **tradução PT-BR confirmada** (Granola Boboca); nenhum update set–nov/2026 | M |
| Joja Seeds (#8746) | **Segue "Broken in 1.6"**; 621 endorsements; tradução Mandarim; heirloom carrots/corn/potatoes — demanda latente sem solução oficial | M |
| Joja Seeds Redux (#50846) | v2.0 (2026-08-18), rebuild permissão CopperSun, **0 endorsements** | M |
| Cheap Joja Seeds (#36893) | v1.1.0, 33 endorsements — alternativa estável | M |
| Mix All The Seeds (#44365) | 0.5.2 (2026-09-09), 91 endorsements, **PTBR + ES**; adjacente | M |
| Tilled Soil Decay (#2738) | 7.3, ~1.198 endorsements; dores de decay na virada de estação confirmadas em 2026 | H/M |
| **Soil health standalone** | **Nenhum concorrente novo** set–nov/2026; **nenhum mod de solo/agroecologia tem tradução não-inglesa ou hype detectado em 2026** | M (ausência) |
| PT/BR | Nenhum post/vídeo 2026 sobre solo/rotação; traduções BR confirmadas só para economia/seeds/idiomas gerais (Harvest Ledger PT-BR; Mix All The Seeds PTBR) | L/M |
| JA/KO/ZH/RU/FR/DE | Wildroot nativo ZH, HQ; FR/KO translations de Wildroot existem; KO/RU/FR/DE sem sinal de solo; JA guias só instalação | M |
| Nico impact técnico | **Não encontrado** (nenhum handle/modder BR chamado "NicoImpact") | L |

**Conclusão de mercado:** o nicho **soil health + agroecologia continua sem líder standalone e sem traduções não-inglesas**; a atenção da comunidade 2026 foi para *breeding* (Wildroot) e *economia* (Harvest Ledger, PT-BR). A janela para um mod SMAPI de solo/agroecologia **PT-BR/EN com rotação e heirloom** segue aberta. Confiança: **M** (Nexus 403; downloads não exibidos).

### 4.2 Ecossistema SDV/SMAPI (refresh 2026-11-10; Wiki oficial 403 em 4 tentativas)

- **Último patch com post no blog: 1.6.15.1** (Xbox/PlayStation 12/02/2025; Switch 26/02/2025) — PC 1.6.15: 20/12/2024 (snippet; página 403 — confiança M). Nada publicado desde então exceto o 10º aniversário (26/02/2026).
- **1.7: sem data, sem estimativa, sem beta/RC público encontrado.** Anunciada 30/08/2025 (Symphony of Seasons) no fórum oficial / X; no 10º aniversário (26/02/2026), CA: “a new update in the works which I think will make the game even better”; conteúdo confirmado por CA: **2 novos candidatos a casamento** + “children a little more interesting” — nomes não revelados. 1.6.16: **não verificado como RC/beta** (só citações de jogadores). Guia de migração 1.7: não publicado/verificado; o índice “Migrate_to_Stardew_Valley_1.6.16” existe mas **conteúdo e data NÃO VERIFICADOS** (403).
- **SMAPI 4.5.2** estável no GitHub e smapi.io (requires SDV 1.6.14+; data divergente: release-notes 14/03/2026 vs releases page “22 Mar” — versão H, data exata L). **4.6/alphas não existem**; `develop` traz “upcoming release” com métricas de SO, stats page e **deprecação da lista de compatibilidade** — provável próximo tag **4.5.3** (inferência). **Lista de compatibilidade (smapi.io/mods): SUNSET CONFIRMADO** (banner na página + anúncio Patreon ~01/07/2026 + release-notes “Deprecated the mod compatibility list”). **smapi.io/stats: nova, não sunset** (anúncio ~24/06/2026; dataset StardewModDataset MIT/CC-BY-SA; fetch ainda não rodado). **O update check NÃO foi descontinuado** — qualquer alegação de sunset do update check: **não verificada.**
- Nenhuma mudança documentada em `Data/Crops`, `HoeDirt.plant`, `DayEnding`/`DayStarted` em 2026-10/11. Harmony: SMAPI não reescreve mods Harmony 1.x desde 4.0.0; compile contra Harmony 2.x. Sem mudanças novas.

### 4.3 Distribuição/D1 (refresh 2026-11-10 — deltas verificados; Wiki 403)

- **D1**: split-screen **existe desde SDV 1.5 e SMAPI o suporta, mas não é D1 de rede** (não exercita serialização/mensagens host↔farmhand entre processos). **Duas instâncias na mesma máquina** (2 cópias/contas Steam, LAN) segue sendo o D1 mínimo real (M). **JunimoServer ativo: sdvd-server v1.4.0 (2026-01-17, Latest; v1.3.0 trouxe invite codes/Galaxy auth; v1.4.0 requisitos inalterados; exige conta Steam com SDV; experimental).** Ferramentas públicas de teste automatizado de co-op SDV: **não existem/verificadas** (L).
- **Nexus — política de IA (o delta mais importante):** vigente desde **2026-11-03** (anúncio news/15654; artigo 180 “AI Policy Changes 2026”, atualizado 2026-11-05). Três tags obrigatórias: **AI-Generated Content** / **AI Media** / **AI Assisted** (dev humano com IA limitada — exige página com ferramentas/modelos, o que a IA gerou, evidência de dev humano). **Carência de 60 dias (→ 2027-01-02)** cobre só os requisitos novos (descrição itemizada “AI Assisted” + tags de permissão fork/AI-training, default “Ask Me”); regras básicas de disclosure já valem; **uploads novos já devem preencher os campos imediatamente.** Claims sem evidência → moderação reclassifica; Nexus pode pedir **código-fonte**. SMAPI 4.5.2 é distribuído no Nexus (mods/2400); .NET pode disparar falsos positivos de antivírus. Update key `Nexus:<id>` (id visível ao salvar a 1ª página); `@Subkey` opcional (regex em smapi.io/schemas/manifest.json).
- **GitHub Releases (0.2.0)**: zip com pasta raiz `ModFolderName` (`manifest.json`, DLL, i18n, assets), `%ProjectVersion% = 0.2.0`, release com tag semver, `UpdateKeys: ["GitHub:lfgranja/LivingRoots"]`.
- **CurseForge**: autores passam por revisão automática **e manual** antes de publicar; inglês primeiro; logo 400×400 PNG; changelog por arquivo; Beta/alpha ocultos por padrão; sem links externos de download; IA em imagens showcase exige disclaimer; v1.4.0/SMAPI 4.5.2 presente (2026-03-14).
- **ModDrop**: ainda online e SMAPI faz update check nele; **não há base verificada para proibir** — “evitar” é preferência comunitária (audiência menor), não regra. Acesso: opt-in.
- **`smapi.io/ModUpdateChecker` → 404**; checker embutido usa web API interna `https://smapi.io/api/v4.x/mods` (não versionada publicamente, pode mudar); friendliness list migrada para Git repo (4.1.8) e **deprecada** em “Upcoming release” (2026-11) em favor de **smapi.io/stats**.
- **UniqueID `lfgranja.LivingRoots`**: válido conforme regex `^[a-zA-Z0-9_.-]+$`, formato recomendado `Username.ModName`; unicidade global — **verificar colisão antes de publicar**.
- **`release.yml`** só roda com `BUILDDEPS_ENABLED=true` + token privado; sem esses knobs o push da tag **não** gera release — usar `gh release create` manual.
- Nota de frescor: Modder Guide/Release e Multiplayer 403 ao coletor — conteúdo via snippet (M). Nenhum item 1.6.16/1.7 publicado; wiki 403.
- **Checklist-delta 0.2.0**: [ ] Nexus: disclosure IA + permissões na 1ª leva (carência até 2027-01-02); evidência de dev humano na página se “AI Assisted”; [ ] zip ModBuildConfig com pasta raiz; [ ] UpdateKeys Nexus+GitHub; [ ] CurseForge Beta + changelog; [ ] D1: duas instâncias reais (ou JunimoServer v1.4.0); split-screen só como “fumaça” extra, nunca como evidência MP; [ ] colisão de UniqueID; [ ] ModDrop: omitir update key por decisão, não por proibição.

---

## 5. O que ainda falta (por camada)

### 5.1 Evidência (o maior gap)

- ❌ Atestação humana/E dos 7 cenários `machine-pass` (ordem sugerida: 004 → 005/013 → 014 → 015 → 017 → 020).
- ❌ **D1 real** (014 T109, 015 T049/T057, 018 T013/T019/T022/T028) — nunca executado; o runner headless é host-only por construção.
- ❌ **SC-004** (017) — protocolo escrito, nenhuma execução.
- ❌ **004 T034** (D1) e **T037** (playtest discoverability) — abertos e não automatizáveis.
- ❌ **020 Gate 3 de colheita/semente salva/aquisição** — só plantio coberto.
- ❌ **014 T110** (34/67→31/71 contrato histórico) — deferido deliberadamente.

### 5.2 Distribuição (segundo gap)

- ❌ Decisão do mantenedor: tag/release ou registro explícito de “não publicar” (State.md #1).
- ❌ 0 tags/0 releases públicas; Nexus/CurseForge ausentes; ícone/screenshots/licença de arte não prontos (briefing `mod-presentation-release-assets.md`, M).
- ❌ CI remoto dormente (`Build and Test` com token privado); `Verify Scenario` continue-on-error.

### 5.3 Gameplay core

- ❌ **Rotação/policultura/famílias botânicas** — melhor candidato verificado (mercado + agronomia); M (1–2 semanas MVP); briefing pronto.
- ❌ **Pest system MVP** — L; bloqueado por decisão de design (G-01); demanda negativa na comunidade; não liderar.
- ❌ **Joja/monocultura completa** — L; 017 excluiu por escopo.
- ❌ Green manure, Three Sisters, CSA, cozinha coletiva, soberania alimentar — briefings, 0 especificações.

### 5.4 Conteúdo/progressão

- ❌ Early/mid/end: **2/7 · 0/6 · 0/6**. Elena, Welcome Kit, farmácia viva, cisterna, Adobe Shed, ICLF, greywater, feira, cozinha coletiva — nada disso existe.

### 5.5 Infra/higiene

- ⚠️ Spec 010 **53 abertas** (dedupe manual 53→~36; T138/T145/T148 bloqueados por ambiente/humano).
- ⚠️ Trilhas legadas **001 (76), 003 (89), 011 (46)** = 211/284 tarefas abertas.
- ⚠️ **`Quality Gate / static` vermelho em `dev`**: 148 linhas duplicadas / 0,83% vs baseline 111 / 0,66% (jscpd reproducido) — não é regressão de PR; `.jscpd-baseline.json` não será relaxado.
- ⚠️ **Zero medido (`0f`) não sobrevive ao save/load** (limitação conhecida, registrada).
- ⚠️ `soil-health-unknown-tooltip-flashing`: fix aplicado, falta Gate 3 in-game.
- ⚠️ 2 bug dirs vazios (`composting-bin-complexity-203`, `compostingbin-s2583-unreachable-2`).

---

## 6. Próximo 80/20 (ordem recomendada, inalterada pelo refresh)

1. **Decidir e publicar `0.2.0` beta pública** (S) — GitHub Release primeiro; Nexus depois dos assets + declaração de IA. Custo: 0,5 dia + gate. Destrava: distribuição, feedback real, proveniência, e a decisão #1 do STATE.md.
2. **D1 real (M, 3–4 h)** — duas instâncias na mesma máquina; destrava 004/015/018; combine com atestação humana numa mesma sessão.
3. **Atestação humana E (M, 1 sessão)** — 0→N `verified`, começando por 004/005/013/014/017/020.
4. **Rotação/policultura 0.3.0 (M)** — `/speckit.specify` a partir de `rotation-journal.md`; mercado e agronomia convergem (Bowles 2020 H).
5. **Gate 3 colheita/seed-save heirloom (S, 1–3 dias)**; **subset jscpd (S–M)**; **dedupe 010 (S)**; **SC-004 (017, S)**.
6. **Pest MVP e Joja completa (L)** — só em ciclos dedicados, com decisão G-01 e spec própria.

Ordem de riscos desta sequência: **publicar antes de D1** é legítimo com disclosure; **rotação antes de publicar** repete o ciclo “trabalho invisível”; **pest como primeira feature** maximiza rejeição; **ilusão de `machine-pass` como verificado** é o erro mais caro disponível hoje.

---

## 7. Confiança e limites

- **H**: frescor do gate local; build/testes; contagens (recomputado); Proveniência (zip inspecionado); SMAPI/SDV releases oficiais; guia 1.7 **não publicado/verificado**; **lista de compatibilidade sunset confirmada**; **update check NÃO descontinuado**; ecossistema 2026-11-10 (blog CA, GitHub SMAPI, smapi.io, Patreon snippet — Wiki 403); **Nexus AI policy** (artigo 180, anúncio news/15654, vigente 2026-11-03); **GitHub/CurseForge/CF authoring** (SMAPI mod-package.md; moderação 2026-10-22); sdvd-server releases (v1.4.0, 2026-01-17); UniqueID regex válido.
- **M**: mercado 2026-11-10 (snippets; Nexus 403); Wildroot 78→85 endorsements; Harvest Ledger PT-BR; Joja broken; ReGen Valley **não verificável**; JunimoServer funcionalidade/requisitos; CurseForge Beta/alpha ocultos; `@Subkey` opcional; split-screen não é D1 de rede.
- **L**: impacto de adoção; tráfego não-inglês; data da 1.7 (“2027” é palco, não anúncio); ferramenta pública de teste automatizado de co-op SDV (não encontrada); permissão de fork default “Ask Me” sobre forks pré-existentes (não avaliado); estabilidade de `api/v4.x` entre releases (doc diz que pode mudar).
- **Limites estruturais**: nenhum número externo é evidência de que o mod funciona; Nexus não exibe downloads; YouTube/Twitch não coletado; JA/ZH/RU/KO cobertura de nicho ausente (achado, não prova de ausência).
- **`machine-pass` ≠ `verified`** se mantém em todo o documento. Nenhuma claim além de host-only é promovida.

---

## 8. Deltas de mercado 2026-11-10 (registrados verbatim, resumidos)

- ReGen Valley: **403 + zero resultados web** — v0.51/3 endorsements permanecem não confirmados.
- Wildroot Chronicles (ex-Cropgenics): 1.3.4, breeding de culturas, MP/SpaceCore, 简体中文 nativo, 78→85 endorsements, ~354 posts; r/StardewValley “crop-breeding mod got major updates” (~out/2026).
- Harvest Ledger: 0.5.0 (2026-07-10), update 2026-08-11, 15 endorsements, economia + rotação host-authoritative, **PT-BR confirmado**.
- Joja: #8746 broken 1.6 (621 endorsements, ZH); Redux v2.0 (2026-08-18) 0 endorsements; Cheap Joja Seeds #36893 33 endorsements estável.
- Sem mod standalone de soil health/rotação em set–nov/2026; nenhuma tradução não-inglesa de mod de agroecologia detectada em 2026; NicoImpact não encontrado; PT-BR demand latente, não articulada; concorrer **com** Harvest Ledger (economia) e Wildroot (breeding), não contra.

**Fontes (acesso 2026-11-10):** nexusmods.com/stardewvalley/mods/{39740(403),48079,48951,50348,50660,50708,50846,8746,36893,44365,43390,41660,2738,52953,12299}; curseforge.com (Farm Plots 0.2.3, 14,2K DL; Crop Growth Info 0.2.0, 22,2K DL; SMAPI 4.5.2 presença; moderation policies 2026-10-22); reddit.com/r/StardewValleyMods (Wildroot, ~out/2026); gameroamer.net (JA guias, 2026-09/10); stardewvalley.net e forums.stardewvalley.net/whats-new (out/2026 — vazio); GitHub Pathoschild/SMAPI (mod-package.md, web.md, release-notes.md; `api/v4.x/mods` interna); smapi.io/schemas/manifest.json; help.nexusmods.com articles 180 (AI Policy 2026, notícia 15654, vigente 2026-11-03; carência até 2027-01-02), 27 (moderation), 28 (file submission, 2026-11-04); github.com/stardew-valley-dedicated-server/server (sdvd-server v1.4.0, 2026-01-17); stardewvalleywiki.com (Modder Guide/Release; Migrate_1.6.16 — **HTTP 403; conteúdo via snippet, confiança M**); baseline SDV/SMAPI do refresh 2026-11-09 (`research/2026-11-09-web-research-ecosystem-status.md`, `research/2026-11-09-ecosystem-market.md`).

---

*Arquivo append-only. Se um delta o invalidar, adicionar nova anotação datada por cima — nunca reescrever.*
