# Living Roots — refresh ecossistema, mercado e documentação (2026-11-11)

> **Escopo:** pesquisa web atualizada (EN/PT/ES/ZH/JA/KO/RU/FR/DE quando houver sinal verificável), de alta confiabilidade quando possível, para responder **quanto do caminho já percorremos** e **o que ainda falta**, e subsidiar atualização de `OBJETIVO.md` e `ROADMAP.md`.
> **Regra de honestidade:** este documento **não** promove `machine-pass` a `verified`, **não** cria atestação humana/E, **não** atesta D1 nem SC-004. Estado técnico interno do produto não foi re-executado nesta sessão além do que já consta em `research/2026-11-10-progress-and-gaps.md` e artefatos commitados; deltas aqui são majoritariamente **externos/ecossistema/mercado/documentação**.
> **Data da coleta:** 2026-11-11. **Confiança:** H = fonte primária/oficial ou página oficial verificável; M = página de mod/site secundário confiável ou mídia especializada citando fonte primária; L = inferência/ausência de evidência.

---

## 1. Síntese executiva (delta vs. 2026-11-10)

| Eixo | Situação 2026-11-11 | Delta / implicação |
|---|---|---|
| SDV 1.6 | **1.6.15** segue o último patch estável documentado nas notas/fonte oficial conhecidas; **1.6.16** permanece como patch potencial/documentado em guia de migração da wiki, **sem data de lançamento** (H/M). | Sem impacto imediato no mod; monitorar diffs de `HoeDirt`/`Data/Crops`/eventos ao sair RC/patch. |
| SDV 1.7 | **Sem data e sem janela firme**; ConcernedApe reiterou “no release date, no estimate” (2025) e, em aparição pública no concerto *Symphony of Seasons* (Red Rocks, fim de out/início de nov 2026, reportado 2026-11-03/04), disse que 1.7 “provavelmente sairá em algum momento do próximo ano” (2027) e que deve ser o maior update do jogo (M, múltiplas reportagens consistentes; confirmação direta via post em X/blog não capturada aqui como página primária única). Conteúdo confirmado publicamente: **Clint e Sandy** como candidatos a casamento (anúncio de aniversário 2026-02-26), **novo tipo de fazenda**, e “mais conteúdo de personagem/social”/mudanças em filhos entre itens mencionados; rumores/vazamentos negados por ConcernedApe (M). | Mantém estratégia: **não migrar agora**; custo zero de preparação até haver RC/guia. |
| SMAPI | **4.5.2** (2026-03-14) é a release estável mais recente (H). Branch `develop`/“Upcoming release” lista: melhorias de performance/erros/traduções, migração para blacklist de malware revampada, **OS metrics API**, **página de stats** (`smapi.io/stats`), **deprecação da lista de compatibilidade**, e **open mod dataset** (H, `release-notes.md` raw). Sem SMAPI 4.6/5.x estável identificado. | Living Roots já mira SMAPI 4.4.0+/SDV 1.6.14+ (manifest) — alinhado. Preparar para ecossistema sem mod-compat list como fonte central; update checks seguem. |
| Nexus Mods (política/IA) | **Política de IA atualizada em 03/11/2026** (anúncio) com regras detalhadas (help article atualizado 05/11/2026): disclosure obrigatório (AI-Generated Content / AI Media / AI Assisted), exigência de explicitar ferramentas/uso em casos “AI Assisted”, novos controles de permissão/forks/treino, e **60 dias de carência** para mods existentes até **2027-01-02**; **novos uploads devem cumprir já**; moderação pode pedir evidência/código (H, Nexus help/news; cobertura ampla M). | Publicação 0.2.0 no Nexus exige plano explícito de disclosure/licenças/assets de IA desde o primeiro upload. |
| JunimoServer / D1 | **sdvd-server v1.4.0** (2026-01-17) é a última release estável no GitHub; projeto ativo com **preview builds** frequentes (tags `preview` / `X.Y.Z-preview.N`, ex.: `1.5.0-preview.*` observados em fontes secundárias/Docker Hub snippets; docs recomendam preview quando `latest` falha) (H/M). Continua sendo opção prática para D1/headless multiplayer dedicado; requer conta Steam com SDV. | Reforça caminho D1 viável sem “ferramenta mágica” de teste MP; esforço ainda é operacional/humano. |
| Concorrentes (sinais) | **Wildroot Chronicles** (`#48079`): resultados de busca indicam **v1.3.4** e ~**85 endorsements** (atualização set/2026) — consistente com tendência já observada; foco em breeding/narrativa, não soil-health per-tile (M). **Harvest Ledger** (`#48951`): **v0.5.0**, **15 endorsements**, atualização 2026-08-11, inclui **crop rotation** e tradução PT-BR confirmada anteriormente — adjacente (economia/rotação), não substituto do loop de solo (M). **ReGen Valley** (`#39740`): acesso direto/Nexus frequentemente limitado; evidência pública nova robusta não encontrada nesta coleta — manter como concorrente conceitual de baixa verificabilidade (L/M). **Joja Seeds** segue com histórico de quebra em 1.6 segundo ecossistema (M, herdado). | Nicho “soil health per-tile + compost/mulch/crescimento/qualidade + framing agroecológico + MP host-authoritative” segue pouco ocupado como pacote coerente; maior atenção recente em breeding/economia. |
| Multilíngue (PT/ES/ZH/JA/KO/RU/FR/DE) | Sinais fortes de tradução/comunidade em EN; PT-BR aparece em mods adjacentes (ex.: Harvest Ledger, traduções diversas) mas **demanda articulada específica por mod de soil health/agroecologia** continua pouco documentada publicamente (L/M). ZH/KO/ES/FR presentes em traduções de mods de expansão (ex.: Wildroot) — não evidência de concorrência direta no nicho Living Roots (M). | Oportunidade de posicionamento PT-BR/EN com proposta clara permanece; validação requer publicação/feedback real. |

**Conclusão 2026-11-11:** o **gargalo continua sendo evidência/distribuição/validação humana e D1**, não falta de direção técnica. Externamente, o ecossistema segue estável para 1.6.15/SMAPI 4.5.2, com 1.7 sem data e políticas de plataforma (Nexus AI) exigindo preparo documental antes de publicar.

---

## 2. Quanto do caminho já percorremos (estado consolidado)

### 2.1 Produto e engenharia (verificado internamente por artefatos + gate fresco em 2026-11-10)
- **Núcleo entregue em código:** solo 0–100 por tile persistente, decay, visualização/feedback, compostagem (loop), mulch, crescimento influenciado por saúde, qualidade/extra de colheita (tiers 31/71, seam Harmony, sync MP FR-009/A8 implementado/testado), tradeoff Joja MVP (017), pipeline heirloom inicial (020, plantio coberto em Gate 3).
- **Evidência determinística:** `release-gate.sh` **ALL STEPS PASSED** (2026-11-10T02:40Z, `c07fbab`), **1997/0/4**, format/restore/build/smoke/docs/protected-paths OK.
- **Evidência end-to-end automatizada (real game, headless, host-only):** **7** cenários `machine-pass` (004,005,013,014,015,017,020), honesty R — **não** equivalem a verificação humana.
- **Progresso de execução:** trilha ativa ~**98%** de tasks; total ~**80,8%** incluindo legadas (conforme `research/2026-11-10-progress-and-gaps.md`).

### 2.2 Governança/processo (Speckit)
- Constituição, assessments, specs numeradas, validações/releases em `.specify/*` e `specs/*` mostram processo maduro; bug workflow majoritariamente com assess/fix/test (30/40 cadeias completas). Lacunas principais: promoção de evidência (humano/D1/SC-004), publicação, e fechamento de trilhas legadas/escopo 010.

### 2.3 Interpretação honesta
“Percorrido” em **capacidade implementada e coberta por automação** é alto no núcleo; “percorrido” em **confiança de uso real (humano/MP) e adoção pública** é baixo.

---

## 3. O que ainda falta

### 3.1 Evidência (crítico)
1. **Atestação humana/E** dos 7 cenários `machine-pass` (ordem pragmática: 004 → 005/013 → 014 → 015 → 017 → 020).
2. **D1 multiplayer real** (farmhand em sessão real; duas instâncias locais ou JunimoServer dedicado) — bloqueia claims MP vivas (014/015/018 e guards correlatos).
3. **SC-004 (017)** — executar protocolo de compreensão.
4. **020 além do plantio:** Gate 3 para colheita, semente salva, aquisição natural e D1.
5. **Pendências manuais históricas:** 004 T034/T037, 009 T039/T040/T052, 014 T110 (decisão de contrato histórico).

### 3.2 Distribuição e produto público
1. **Decisão e execução de publicação 0.2.0** (GitHub Release primeiro; depois Nexus/CurseForge conforme prontidão).
2. **Assets de release:** ícone, screenshots, notas honestas, licença/atribuição de arte, changelog claro.
3. **Compliance Nexus AI:** disclosure, permissões, evidência de processo humano quando “AI Assisted”, tags corretas desde o upload.
4. **UpdateKeys/identidade:** confirmar ausência de colisão de `UniqueID`, definir `Nexus:<id>`/`GitHub:<owner/repo>` no momento certo.

### 3.3 Gameplay e roadmap
1. **Rotação/policultura MVP** (melhor próximo slice 80/20 para fechar promessa “BUILDS” de forma distinta e defensável).
2. **Sistema de pragas, monocultura/Joja completo, adubo verde, manejo animal integrado, mid/end-game e soberania alimentar** — não implementados.
3. **Conteúdo/narrativa:** Elena, Welcome Kit, progressão Y2–Y4+ — em grande parte briefing.

### 3.4 Infra/higiene
1. **Spec 010** com escopo aberto (53 tasks) — decidir fechar vs. declarar suficiente.
2. **Trilhas legadas 001/003/011** concentram dívida de tarefas.
3. **Quality Gate / static (jscpd)** acima do baseline reportado (não relaxar baseline; tratar como dívida técnica).
4. **CI remoto** condicionado a segredos/builddeps; `Verify Scenario` não é gate duro.

---

## 4. Fontes consultadas (seleção, com tipo/confiança)

### Oficiais / primárias (H quando acesso direto)
- SMAPI releases/notes: https://github.com/Pathoschild/SMAPI/blob/develop/docs/release-notes.md ; https://smapi.io/ ; https://github.com/Pathoschild/SMAPI/releases/tag/4.5.2
- SDV wiki migração (potencial 1.6.16): https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6.16 ; 1.7 (anunciado, sem data): https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.7
- JunimoServer: https://github.com/stardew-valley-dedicated-server/server ; https://github.com/stardew-valley-dedicated-server/server/releases ; https://docs.junimoserver.com/ ; https://hub.docker.com/r/sdvd/server
- Nexus AI policy/help: https://help.nexusmods.com/article/180-ai-policy-changes-2026 ; https://www.nexusmods.com/news/15654

### Ecossistema / mods (M, sujeito a mudança e limitações de acesso)
- Wildroot Chronicles: https://www.nexusmods.com/stardewvalley/mods/48079
- Harvest Ledger: https://www.nexusmods.com/stardewvalley/mods/48951
- ReGen Valley (referência histórica/baixa verificabilidade nesta coleta): https://www.nexusmods.com/stardewvalley/mods/39740

### Cobertura jornalística/segunda mão (M, usada para triangulação de falas públicas)
- 1.7 “provavelmente 2027 / maior update”: GamesRadar+, Kotaku (via agregação), GameLuster, 80.lv, GamingBible, Eurogamer etc. (consultados por busca; tratar como secundários, consistentes entre si).
- Nexus AI: Eurogamer, 80.lv, Automaton (JA), Frontier Now, HowToVideoGame etc.

### Fontes internas do projeto (H para estado do repo)
- `research/2026-11-10-progress-and-gaps.md`, `OBJETIVO.md`, `ROADMAP.md`, `README.md`, `AGENTS.md`, `VERIFICATION_PROTOCOL.md`, `specs/*`, `.specify/*`.

---

## 5. Recomendações práticas (80/20) — atualizadas

1. **Publicar 0.2.0 (beta) com honestidade**: notas explícitas (“machine-pass host-only; sem atestação humana/D1”), zip correto, UpdateKeys, checklist Nexus AI. *Desbloqueia feedback real.*
2. **Executar D1 + atestação humana em sessão combinada**: maximiza valor de evidência por hora.
3. **Fechar lacunas de evidência de alto ROI:** SC-004 (017), Gate 3 colheita/seed-save (020), resíduos 004/014/015/018/019.
4. **Gameplay seguinte:** especificar/implementar **rotação/policultura MVP** aproveitando seams existentes.
5. **Não investir agora em migração 1.7**; manter watchlist (wiki migration 1.6.16/1.7, SMAPI releases, Nexus policy updates).

*Este arquivo é um snapshot de pesquisa datado; novas coletas devem gerar novo arquivo datado em `research/` em vez de reescrever este.*