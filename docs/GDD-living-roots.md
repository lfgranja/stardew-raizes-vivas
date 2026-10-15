# Living Roots / Raízes Vivas — Game Design Document (GDD)

> **Status:** v1.1 Draft — 2026-10-06 | **Tipo:** Híbrido (bíblia de implementação + visão para comunidade)
> **Modelo:** B — Lean Living GDD (wiki, 5–15 pgs, owner por seção) + Modelo A — quatro one-pagers (visão, pilares, loops, fluxo de recursos — Schreiber GDC 2025, Librande GDC 2010). Monolito 100+ pgs é anti-padrão (GameDeveloper 2023, Featurebase 2026, ACM SIGDOC 2019).
> **Owner:** maintainer (lfgranja) | **Changelog:** v1.0 pitch+loops+sistemas → v1.1 pilares testáveis + TDD handoff + QA/cut-list/localização + bibliografia canônica.
> **Escopo:** Mod SMAPI completo para Stardew Valley 1.6 (PC Linux/macOS/Windows), Early → Mid → End game
> **Documentos-mãe:** `ROADMAP.md`, `.specify/memory/constitution.md` v2.0.0, `specs/004-playable-compost-loop/spec.md` + `plan.md`, `README.md##Features`, `docs/design-system/*`
> **Statement de intent confirmado:** Outcome = GDD híbrido do Living Roots; User = maintainer + cônjuge em co-op + colaboradores/jogadores cozy; Success = guiar slice 004 jogável + Gate 3 co-op verificável sem quebrar saves; Constraint = solo vivo 0–100 farm-keyed, host-authoritative, só save Stardew; Out of scope = engine standalone, economia Joja paralela infinita, localização além PT/EN no v1.
> **Como usar:** preencha o que sabe, marque `TBD` honesto no resto, releia a cada 2 semanas nos 3 primeiros meses. GDD é mapa — detalhes vivem nas specs linkadas, não copiados aqui.
> **Pesquisa de fundo:** 3 agentes de research (estrutura GDD 2024–2026, modding Stardew/SMAPI, cozy games agroecológicos + MDA/4 Keys/Bartle) disparados em background em 2026-10-06; digests serão fundidos na v1.1. Base v1.0 = template canônico Ludessy 6 seções (Bondo, 2026-05-15) + wiki SMAPI/Content Patcher + Nexus + GDC/IGDA.

---

## 1. Pitch

### 1.1 One-line pitch (≤15 palavras)

**Stardew Valley com solo vivo — composte, cubra e rotacione para libertar o vale da Joja.**

### 1.2 Gênero e referências

- **Gênero:** Cozy farming sim sistêmico / mod de overhaul agroecológico. Não é jogo standalone — é plugin SMAPI que reescreve a camada de solo do Stardew sem quebrar o loop vanilla.
- **Três referências (vocabulário compartilhado):**
  1. **Stardew Valley vanilla** — verbs, ritmo dia-a-dia, economia shipping bin, festivals. Tudo do mod deve ser legível para quem já jogou 20h de vanilla.
  2. **ReGen Valley / Permaculture Farming (Nexus)** — prova de que soil health, cover crops e composter têm demanda; nosso diferencial é persistência farm-keyed 0–100 + visual reativo ≤16.67ms + co-op host-authoritative desde o dia 1.
  3. **Coral Island / Roots of Pacha / Terra Nil** — cozy games que casam educação ambiental com progressão legível (tier de solo → qualidade → soberania). Tom e pacing vêm daqui, não de sims punitivos.

### 1.3 Plataforma + audiência

- **Plataforma:** Stardew Valley 1.6 + SMAPI 4.x, .NET 6, desktop (Linux/macOS/Windows). Sem console/mobile no v1 — limite do próprio SMAPI.
- **Audiência primária:** jogadores cozy 25–45 que já zeraram 1–2 anos de Stardew e querem profundidade sem punição; casais em co-op (caso fundador: maintainer + esposa); educadores/ativistas agroecológicos que querem exemplo jogável.
- **Audiência secundária:** modders SMAPI que vão ler o código (DDD limpo, TDD, FsCheck) e contribuir.
- **Não-audiência:** speedrunners de lucro máximo Ancient Fruit, jogadores que querem combate/dificuldade souls-like.

### 1.4 Hook (primeiros 30 segundos)

Você ara 3 tiles lado a lado, aplica 1 composto num deles e **vê na hora** aquele tile ficar visivelmente mais rico que os vizinhos — sem abrir menu, sem ler número. O segundo hook: Elena bate na porta no dia 2 com o Kit de Boas-Vindas Agroecológico (composteira + sementes heirloom) e diz: "Pierre vende híbrido que rende hoje e mata amanhã. Quer aprender o outro caminho?". Wishlist/momento DM: screenshot antes/depois do solo + frase "meu solo subiu de 12 para 27".

> **Erro clássico evitado:** hook como tema ("é sobre sustentabilidade") vs hook como momento (ver o solo mudar + Elena te escolher). Ficamos com o segundo.

---

## 2. Core Gameplay

**Pergunta respondida:** o que o jogador faz, segundo a segundo?

### 2.1 Verbs (7, teto duro)

1. **Arar / capinar** — preparar e manter tile.
2. **Compostar** — carregar composteira com resíduo orgânico.
3. **Coletar** — retirar composto pronto (2 dias).
4. **Aplicar** — espalhar composto/mulch/spray no tile (inclui top-dressing sobre cultura ativa, sem dano).
5. **Plantar / rotacionar** — escolher família botânica por tile/estação.
6. **Observar** — ler saúde do solo no hover/enxada (overlay, sem número obrigatório).
7. **Partilhar** — vender na feira/CSA/cozinha coletiva (end game social).

Tudo fora disso é conteúdo, não verbo. "Beekeeping", "greywater" e "telhado verde" são progressões que recombinam esses 7 verbs.

### 2.2 Core loop (diagrama)

```text
      ┌────────────────────────────────────────────┐
      │  1 MINUTO (moment-to-moment)               │
      │  Arar → Observar cor → Aplicar 1 composto  │
      │  (+15, clamp 100, consome 1 do stack)      │
      └──────────────┬─────────────────────────────┘
                     ▼
      ┌────────────────────────────────────────────┐
      │  1 SESSÃO = 1–2 DIAS IN-GAME (~20–30 min)  │
      │  Carregar bins → dormir → coletar (1-in-   │
      │  1-out) → aplicar → plantar fam. certa →   │
      │  cobrir com mulch antes de dormir          │
      └──────────────┬─────────────────────────────┘
                     ▼
      ┌────────────────────────────────────────────┐
      │  MULTI-SESSÃO = ESTAÇÃO → ANO              │
      │  Rotação por família → tiers sobem →       │
      │  qualidade prata/ouro/irídio + colheita    │
      │  extra → heirloom salvas → CSA/feira →    │
      │  % Soberania Alimentar → final Joja cai   │
      └────────────────────────────────────────────┘
```

- **Loop vertical slice 004 (jogável hoje):** craftar bin → posicionar → carregar resíduo → avançar 2 dias → coletar 1 composto → aplicar em `HoeDirt` (+15) → ver tier mudar. Critério SC-001: fresh save completa isso numa sentada sem instrução externa; SC-008: descobrível só abrindo crafting menu.
- **Anti-loop quebrado:** solo nu arado degrada; monocultura repetida degrada; fertilizante Joja dá boost imediato mas −5% saúde (armadilha de curto prazo, decisão interessante).

### 2.3 Win / loss

- **Win final (Food Sovereignty):** média da fazenda >80% saúde + ≥10 heirlooms salvas + Cozinha Coletiva operante + CSA ≥5 famílias → Pierre troca híbrido por heirloom, Morris sai, troféu "Guardiã das Raízes Vivas". Não é tela de vitória — é transformação do vale (receita, catálogo e diálogos mudam).
- **Win intermediário:** cada tier cruzado (0→33→66→100) dá feedback imediato (cor + crescimento + qualidade). Sem grinding invisível — SC-002: zero passo silencioso.
- **Loss soft (sem game over):** solo pobre (0–30%) = crescimento lento, pragas, sem estrela. Nunca trava save — sempre recuperável com composto + cobertura + adubo verde. Falhar entrega CSA numa semana = −amizade, não −save.
- **Fail states de interação:** aplicação inválida / saúde já 100 / inventário cheio / colisão co-op → recusa explicada via `Game1.showRedMessage` + chaves i18n, zero consumo indevido, zero last-write-wins silencioso (first-wins + recusa explicada, FR-019).

### 2.4 Pacing

- **Sessão:** 20–40 min = 1–2 dias in-game. Bin de 2 dias (`MaturationDays = 2`) força ritmo dormir-voltar-colher sem idle forçado de 1 semana.
- **Run completa:** Year 1 aprendizado (kit, composto, mulch, 3 Sisters), Year 2–3 sistema auto-sustentável (rotação, adubo verde, bichos, abelhas, farmácia viva), Year 4+ soberania (ILPF, greywater, feira semanal sábado, CSA, cozinha domingo). Espelha `ROADMAP.md` Early/Mid/End.
- **Curva:** dificuldade não é inimigo mais forte — é complexidade agronômica crescente + variedade exigida (feira/CSA recusam 100 vinhos Ancient Fruit, querem cesta variada). Player verbs constantes, contexto escala.

> **Erro clássico evitado:** 12 verbs "core". Travamos em 7.

---

## 3. Sistemas

**Pergunta respondida:** que maquinaria faz o jogo funcionar? `TBD` honesto onde spec mãe manda.

### 3.1 Solo Vivo (sistema-mãe, NON-NEGOTIABLE)

- **Modelo:** cada tile arável tem `SoilHealth` 0–100, farm-keyed (não player-keyed), persistido no save Stardew (`soil_health_data_{saveId}`), cache esparso (zero omitido para save pequeno). `CheckForOverflowUnderflow` ligado, clamp duro, `ModConstants` única fonte.
- **Tiers (herdados 001, contínuo RGB):**
  - **0 = baseline:** sujeira vanilla neutra, **sem overlay** (anti-clutter, anti-alert-fatigue, decisão 004).
  - **1–33 Pobre:** crescimento lento, pragas altas, sem estrela.
  - **34–66 Médio:** ≈ vanilla.
  - **67–100 Vivo:** crescimento +rápido, chance colheita extra, prata/ouro/irídio, desbloqueia interações.
- **Render:** interpolação RGB contínua por tier sob iluminação ambiente (noite/chuva/inverno sem clip/washout), invalidação reativa via `ISoilHealthService.OnTileHealthChanged` → `VisualizationService` no próximo frame ≤16.67ms, zero polling. Farmhand recebe via mesmo evento (transporte de rede real = D1 roadmap, verificação 004 limitada a stubs multi-thread em `LivingRoots.Tests`).
- **O que DESTROI:** solo arado nu sem cobertura/planta; monocultura (mesma cultura repetida); Joja Fertilizer (+boost agora, −5% saúde).
- **O que CONSTRÓI:** composto orgânico (+15/aplicação, 1 item consumido, top-dressing permitido); rotação por família (Brassicas, Solanaceae, Leguminosas… + `Rotation Journal`); policultura/companion (Three Sisters: milho + feijão + abóbora); adubo verde (trevo/aveia); cobertura/mulch; manejo animal (pastoreio rotativo `Mobile Coop`, esterco sobre mulch +, sobre nu compacta); ILPF com acácia/gliricídia (N-fix, imunidade seca, `Shaded Milk`/`Soft Wool`).

### 3.2 Composteira (vertical slice 004 — especificação travada)

- **Máquina nativa, não UI custom:** registrada via SMAPI `AssetRequested` em `Data/Objects` + `Data/CraftingRecipes` + `Data/Machines` (`LivingRoots.Compost`, `LivingRoots.CompostingBin`). Spike primeiro; fallback = Content Patcher como dependência ou intercept SMAPI mantendo skin vanilla. Comportamento de máquina vanilla (som, shake, progresso) — quem usou outra máquina reconhece sem tutorial.
- **Ciclo:** vazio → carregado (qualquer resíduo do set herdado de `002`, sem restatar/esticar aqui, FR-003) → processa exatamente 1 tick/dia (FR-004, idêntico com 2 players) → pronto em 2 dias → rende **exatamente 1 composto** (1-in-1-out; multiplicador 1–5 aposentado, FR-005a).
- **Nada se perde (FR-005b/c):** inventário cheio = composto fica no bin + `message.harvest.inventory-full`; bin removido/movido/explodido com progresso = item não-empilhável (stack 1) carrega `Item.modData` (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`), pausa fora do chão, restaura ao recolocar, sem reset/shortcut; sem espaço = dropa como debris flutuante com modData. Save/load round-trip exato em todo lifecycle; leitor tolerante com isolamento por registro (ignora `MaturationLevel`/`ConsecutiveActiveDays` legados, poda órfãos sem objeto físico, quarentena corrompido com log sanitizado `GetType().FullName + HResult` em Trace, FR-012/014/015).
- **Aplicação (FR-007/008/008a):** alvo válido = `HoeDirt` arado em fazenda/estufa com saúde <100, com ou sem cultura ativa. Sucesso = −1 composto do stack ativo, +15 clamp 100. Recusa = zero consumo + `Game1.showRedMessage` na língua do jogador via `ITranslationHelper` (`message.compost.invalid-tile`, `.max-health`, `.action-conflict`). Rejeição de input inválido no bin = cue vanilla (som cancel + shake, sem texto). Zero UI/text-drawing/art nova para explicar recusa.
- **Multiplayer (Principle VI, FR-018/019, SC-005/010):** estado é da fazenda, host-authoritative. Lock atômico por coordenada/instância; first-wins; segunda ação reavaliada no estado novo, recusada se inválida com mensagem, inventário intacto, sem last-write-wins silencioso. Verificação 004 = stubs automatizados; live 2-jogadores real = D1.

### 3.3 Economia (fontes precisam de sumidouros)

| Recurso | Fonte | Sumidouro | Cap / nota |
|---|---|---|---|
| Composto | bins (1/2 dias cada, N bins em paralelo) | aplicação +15/tile | sem cap; gargalo é tempo + resíduo, não stack |
| Resíduo orgânico | sap, peixe, restos colheita (set herdado 002) | carga no bin | set fechado; artisan goods excluído em 002 mas não forçado em código — divergência conhecida, fechar em 002 não aqui |
| Saúde do solo | composto, rotação, policultura, mulch, adubo verde, esterco coberto, ILPF | solo nu, monocultura, Joja −5%, pragas | 0–100 clamp; 0 omitido do save (esparso) |
| Ouro | shipping bin (vanilla) vs feira sábado +25% + amizade (exige variedade) vs CSA upfront sazonal | sementes heirloom (Elena), construções (Adobe Shed, Green Roof, cisterna, hotel inseto, cozinha), Joja (armadilha) | inflação controlada pela exigência de variedade — anti-100-Ancient-Fruit-Wine |
| Amizade | feira, CSA semanal, mutirão cozinha domingo (minigame tipo Luau) | falha CSA semanal (−amizade) | porta para soberania (≥5 famílias CSA) |
| Heirloom seeds | Elena (vs híbrido Pierre) | plantio + salvamento (≥10 tipos p/ vitória) | soberania exige banco de sementes, não só ouro |
| Favores/buffs | refeições comunitárias, tônicos farmácia viva (arruda/boldo/camomila), mel + aura inseto benéfico | consumo, sprays naturais | buffs potentes mas locais, não power creep global |

### 3.4 Progressão (conteúdo do ROADMAP, não redesign aqui)

- **Early Y1 — Aprender fundamentos:** NPC Elena (agrônoma mentora) + Kit Boas-Vindas (composteira + heirloom, substitui chirívia inicial) → compostar → mulch craftável → Three Sisters → heirloom Elena vs híbrido Pierre → Mini Cisterna (chuva).
- **Mid Y2–3 — Integrar sistema:** `Rotation Journal` + famílias → adubo verde → animal fora de cerca come erva/praga + esterco (mulch + / nu compacta) + `Mobile Coop` → colmeia dupla-função (mel + aura) + Hotel Inseto → Farmácia Viva (tônicos + spray natural) → Robin: Adobe Shed (adega +rápida, +barata em argila) + Green Roof (erva no telhado).
- **End Y4+ — Soberania e comunidade:** ILPF (acácia/gliricídia) → Greywater (passivo tipo poço, enche +rápido) → Feira semanal sábado (+25% + amizade, exige policultura) → CSA Box (upfront + cesta semanal variada, falha = −amizade) → Cozinha Coletiva (ex-JojaMart, mutirão domingo, refeições = buff cidade) → vitória soberania (checklist §2.3).
- **NPC nova escopo mínimo v1:** Elena vendedora/mentora/quests. Diálogos Pierre/Morris reagem no final (Pierre troca catálogo, Morris sai). Sem romance Elena no v1 (out of scope).

### 3.5 Dificuldade / balanceamento (alvos v1, tuning em playtest)

- Solo começa baixo/heterogêneo; mulch previne decay; +15/aplicação ⇒ tile 0→100 ≈ 7 compostos (≈14 dias de 1 bin — força 2–3 bins em paralelo, decisão econômica).
- Joja = payday loan: +yield agora, −5% permanente até compensar. Heirloom = +solo, −pico imediato.
- Feira/CSA como regulador anti-monocultura: preço +25% só com variedade; 100× mesmo item = recusa/limite.
- Nada escala "tudo junto": escala = complexidade de manejo + exigência de variedade; verbs e UI constantes. Noite/chuva/inverno afetam render e água (cisterna/greywater), não quebram saúde de graça.

> **Erro clássico evitado:** economia sem sumidouro. Tabela acima tem sink em toda linha.

---

## 4. Mundo e Narrativa

### 4.1 Premissa (2 parágrafos)

Vale do Orvalho, Stardew Valley. A fazenda herdada chega exaurida: anos de híbrido Pierre + Joja deixaram o solo cansado. Na primavera do Ano 1, Elena — agrônoma que trocou a universidade pelo vale — bate na porta com uma composteira e um punhado de sementes crioulas: "Solo não é sujeira. É bicho vivo. Trata bem que ele trata de volta."

A Joja não é vilã de bigode — é conveniência: fertilizante que funciona hoje, semente que rende hoje, shipping bin que compra tudo sem perguntar. A campanha é provar, tile por tile, que variedade + cobertura + bicho + vizinho rendem mais e para sempre — até o vale votar com o garfo (feira, CSA, cozinha) e a Joja ir embora sozinha.

### 4.2 Tom (3 adjetivos)

**Acolhedor, teimoso, vivo.** Acolhedor nos tutoriais (Elena nunca pune, só explica); teimoso na fricção Joja vs heirloom (escolha tem custo); vivo no feedback (tudo responde: cor, bicho, abelha, vizinho).

### 4.3 Personagens (1 linha de papel cada; bios em páginas próprias)

- **Elena (nova):** mentora. Apresenta cada sistema, vende heirloom/adubo verde/erva, preside soberania, entrega troféu.
- **Pierre:** espelho. Vende híbrido fácil; no final vira aliado (só heirloom local). Não vilanizar.
- **Morris (Joja):** antagonista-sistema, não boss. Sai quando CSA+cozinha provam o ponto.
- **Linus:** co-mentor farmácia viva (erva/spray). Alternativa quando Elena ausente.
- **Robin:** construtora das upgrades bio (Adobe, Green Roof, cisterna, greywater).
- **Gus/Evelyn/Clint/etc.:** assinantes CSA / frequentadores feira / mutirão cozinha. Amizade como recurso.
- **Abelhas/insetos/animais:** "personagens sistêmicos" — aura, esterco, pragas. Tratamento de personagem no tuning, não só número.

### 4.4 Locais (1 linha de papel cada)

- **Fazenda (tiles):** protagonista. Onde saúde 0–100 é lida a olho.
- **Estufa:** exceção fértil — composto funciona, rotação importa menos. Refúgio inverno.
- **Praça sábado:** feira short-chain. Onde variedade vira ouro+amizade.
- **Ex-JojaMart → Cozinha Coletiva:** dungeon final invertida — construção coletiva, não invasão. Mutirão domingo.
- **Telhados verdes / porões adobe:** upgrades que expandem área plantável/processável sem expandir mapa.

### 4.5 Story beats (esqueleto, 8)

1. Herança exaurida + Kit Elena (dia 2).
2. Primeiro composto aplicado e visto (slice 004) — "o solo responde".
3. Three Sisters + mulch — "cobrir é cuidar".
4. Journal de rotação + adubo verde — "família importa".
5. Bichos/abelhas/farmácia — "ninguém cultiva sozinho".
6. Feira + CSA — "o vale prova e paga".
7. Cozinha coletiva construída — "comer junto é poder".
8. Soberania (>80% + 10 heirlooms + cozinha + 5 CSAs) — Pierre converte, Morris sai, troféu. Pós-jogo = manter vivo, não créditos.

> **Erro clássico evitado:** bios coladas na lista. Aqui só papel; detalhe linka para páginas de NPC/quest.

---

## 5. Arte, Áudio e UI/UX

**Princípio v1: vanilla-first.** Nenhum asset custom bloqueia jogabilidade (FR-008a). `.png`/`.ogg` em `Assets/` e `i18n/*.json` hoje são placeholders copiados ao build mas não carregados — não assumir fiação.

### 5.1 Visual

- **Estilo:** Stardew 1.6 vanilla. Overlay de solo = tinta RGB translúcida sob iluminação ambiente, sem clip. Referências linkadas (manter doc leve): screenshot vanilla arado, mock 3-tier, referência ReGen Valley.
- **Paleta (travar cedo p/ evitar drift):** Pobre `#8A5A3B` (terra cansada), Médio `#6B8E3E` (vanilla-like), Vivo `#3E7A34` (verde profundo) + neutro 0 = sem overlay. Refinar após playtest noturno/chuva/inverno.
- **Regra 0:** saúde 0 = sujeira vanilla pura. Sem overlay = sem fadiga de alerta.

### 5.2 Áudio

- Sem trilha custom v1. Direção: cues vanilla (máquina pronta, cancel, colocar composto). Referências: som composter vanilla, "pop" colheita, sino feira. Mutirão/cozinha podem reutilizar stingers de festival (Luau) — sem compor OST antes do slice estável.

### 5.3 UI/UX (onde o design mora neste mod)

- **Zero UI bespoke para recusa/progresso:** máquina usa interface nativa (progresso, input, output); recusas via `Game1.showRedMessage` + `ITranslationHelper` + `i18n/default.json` (4 chaves padrão). Fonte do jogo, língua do jogador.
- **Leitura sem número:** hover/enxada mostra cor; número exato opcional em tooltip futuro (pesquisa `research-report-ui-overlays-tooltips.md` como base, não escopo v1).
- **Acessibilidade (base `research-wcag-gaming.md`):** tiers distinguíveis sem só-cor (padrão/textura futura + toggle overlay nas settings — aplicar composto com display off ainda cura, só esconde visual, US3); daltonismo e baixa visão testados em noite/chuva; remapeamento herdado vanilla; legendas de recusa via texto, nunca só som.
- **Descoberta:** loop inteiro descobrível sem doc (SC-008). Receita visível no crafting desde save novo; bin rejeita input inválido com shake+som vanilla; tile inválido explica por texto.
- **Multiplayer UX:** recusa de colisão explica ("outro jogador aplicou primeiro"), nunca silêncio. Sem UI de lock visível — só consequência legível.

> **Erro clássico evitado:** pular paleta. Travamos hex v1 acima.

---

## 6. Escopo e Cronograma

**Pergunta respondida:** isso é construível?

### 6.1 Scope (conteúdo concreto, não abstrato)

- **Slice jogável 004 (M1):** item Composto + máquina Bin + receita + ciclo 2 dias 1-in-1-out + aplicação +15 em `HoeDirt` fazenda/estufa + overlay 3-tier reativo + `showRedMessage` 4 chaves PT/EN + save/load tolerante + stubs concorrência + README `(Planned)` + reconciliação 8 docs (FR-017).
- **Pós-slice por fase (conteúdo ROADMAP, cada um vira spec própria):** Early = Elena + kit + mulch + 3 Sisters + heirloom + cisterna; Mid = journal + famílias + adubo verde + `Mobile Coop` + colmeia-aura + hotel + farmácia + Adobe + Green Roof; End = ILPF + greywater + feira + CSA + cozinha + soberania.
- **Conteúdo contado v1 completo:** 1 NPC nova, ~6 famílias botânicas, ~10 heirlooms banco, 4–6 receitas crafting (bin, mulch, cisterna, hotel, CSA box, spray), 2 upgrades Robin, 2 eventos semanais, 1 métrica vitória.

### 6.2 NOT in scope (a seção mais importante — protege de nós mesmos)

- Sem jogo standalone/engine própria.
- Sem multiplayer com DB externo/serviço rede/fonte-verdade dupla — só save Stardew, host-authoritative (Principle VI).
- Sem quebrar saves single-player antigos — migração aditiva sem perda.
- Sem UI custom/text-drawing/arte obrigatória para recusas no slice.
- Sem reestatar set compostável em 004 (herdado 002); sem corrigir exclusão artisan goods aqui.
- Sem romance Elena, sem localização além PT/EN, sem console/mobile, sem daily/seasonal live-ops no v1.
- Sem balance final travado antes de playtest Gate 3 (números §3.5 são alvos, não lei).

### 6.3 Milestones (âncoras ambiciosas-defensáveis)

- **M1 Slice 004 jogável solo + stubs co-op:** spike `AssetRequested` (`Data/Objects`, `CraftingRecipes`, `Machines`) → bin/composto/aplicação/overlay/i18n/save tolerante. Gate 1 (build/test/format) + Gate 2 (usável sem doc, 0–100, round-trip) + Gate 3 solo (arar→modificar→salvar→recarregar→verificar).
- **M2 D3 antes de D1:** fechar superfície jogável real (sem ela, multiplayer seria camada sobre vazio). Recomendação ROADMAP: D3 → D1.
- **M3 D1 spec multiplayer real:** ownership host, visibilidade farmhand, resolução por tile/máquina, migração saves, verificação 2-jogadores viva (host + farmhand valores consistentes).
- **M4 Early game completo:** Elena + kit + mulch + Sisters + heirloom + cisterna, playtest casal 1 estação.
- **M5 Mid + End fatiado vertical:** cada sistema como slice (rotação → bichos → abelhas → farmácia → feira → CSA → cozinha → soberania), cada um com Gate 3.
- **M6 Beta comunidade:** Nexus/ModDrop, PT/EN, guia feira/CSA, cobertura OpenCover.

### 6.4 Riscos (probabilidade × impacto + mitigação)

| Risco | Mitigação |
|---|---|
| Spike `Data/Machines` não preserva `modData` progresso | Fallback travado em spec: intercept SMAPI (`ModController`) mantendo skin vanilla; escopo inalterado, só esforço |
| Overlay lava/clipa à noite/chuva/inverno | Blend sob luz ambiente + 0 sem overlay + playtest clima/noite como critério; toggle sem quebrar cura |
| Economia inflaciona (Ancient Fruit domina) | Feira/CSA exigem variedade + limite por item; tuning pós-playtest, não antes |
| Co-op diverge (fork estado por player) | Farm-keyed + lock atômico + first-wins + recusa explicada; teste dupla viva obrigatório em D1 |
| Teste verde mente (816 passam, nada jogável — D3) | FR-016/FR-017 + Gate 3 jornada real; README `(Planned)` honesto; nunca promover check format-only a required |
| CI sem jogo real (Build and Test nunca verde) | Não pôr em `required_status_checks` até 1 run verde real; manter format + protected-paths como required temporário |
| Escopo romance/localização estoura | NOT in scope acima + capability map por módulo (Specify→Plan→Tasks→Implement por módulo, ordem: solo→bin→UX→multi→Early→Mid→End) |

---

## 7. Apêndices

### A. Glossário ubíquo (DDD — `GLOSSARY.md` não leva detalhe de impl)

`SoilHealth`, `Compost`, `CompostingBin`, `CropRotation`, `Polyculture`, `CompanionPlanting`, `HeirloomSeed`, `GreenManure`, `Mulch`, `FoodSovereignty`, `CSA`, `Farm-Keyed`, `Host-Authoritative`, `First-Wins`. Solo é propriedade da fazenda — por isso persistência é farm-keyed e multiplayer é shared-state, nunca por jogador.

### B. Tuning travado (única fonte `Constants.cs`)

`Min 0 / Max 100`, `RestorationAmount 15`, `MaturationDays 2`, 1-in-1-out, stack 1 com progresso, tiers 0/1–33/34–66/67–100, Joja −5%, chaves `message.compost.invalid-tile/.max-health/.action-conflict`, `message.harvest.inventory-full`.

### C. Verificação (Definition of Done)

Gate 1: `GamePath=$PWD/.gamepath dotnet build -c Release` + `dotnet test --no-build -c Release` (989 testes ~7s) + `dotnet format --verify-no-changes` + `scripts/check-protected-paths.sh`. Gate 2: usável sem doc, 0–100, round-trip, sem path single-only. Gate 3: jornada real arar→modificar→salvar→recarregar→verificar (+ dupla viva quando toca estado compartilhado). Convenções: sem `.Result/.Wait`, sem lock sobre I/O, log sem exceção crua (`FullName+HResult` em Trace), sem expression-bodied, sem hardcoded fora `Constants.cs`, `(O)LivingRoots.Compost` + `Qualified` casados.

### D. Pilares testáveis (filtro anti-feature-creep — God of War/Deus Ex pattern)

1. **Solo é ser vivo, não barra** — toda feature toca 0–100 ou é cortada. Teste: remover a feature muda alguma decisão de manejo? Se não, corta.
2. **Recompensar, nunca pregar** (Coral Island) — Elena explica, regra convence. Joja nunca bloqueada, só pior no longo prazo.
3. **Co-op desde o dia 1** (Principle VI) — farm-keyed, host-authoritative, first-wins + recusa explicada. Single-player é caso degenerado de multi.
4. **Verbo novo por arco, não +10%** (Forager) — Y1 compostar, Y2-3 rotacionar/incorporar/pastejar, Y4+ assinar/trocar/cozinhar.
5. **Aconchego seguro** (Horseshoe: Safety/Abundance/Softness) — sem fail punitivo, teto em toda penalidade, ritual diário, refúgio > exterior.

### E. Referências (fontes primárias)

- Ludessy template completo (estrutura 6 seções, pergunta por seção, erros por seção, exemplo Cinder Crown) — base deste GDD.
- SMAPI docs (`smapi.io`), Stardew modding wiki (Permaculture/Composter), Nexus (ReGen Valley 39740), guia mods + compatibilidade SMAPI.
- **Fiação técnica travada (wiki oficial):** `AssetRequested` + `e.Edit / AsDictionary` com `NameWithoutLocale.IsEquivalentTo` (nunca `==`); `Data/Objects` chave não-qualificada + `Name`=chave; `Data/Machines` chave qualificada `(BC)ModId.Item` com `OutputRules/Triggers (ItemPlacedInMachine, DayUpdate, OutputCollected)`; `Data/CraftingRecipes` formato `ingredientes/Home|Field/rendimento/big?/desbloqueio`; `Item.modData` (strings, chave `ModId/campo`) sincroniza em multi + persiste no save — carrega `DaysRemaining/InputId`, stack 1 com progresso; fallback = Content Patcher `EditData` + `Load` textura; co-op = host-authoritative (`Context.IsMainPlayer`/`IsMasterGame`), farmhand via `SendMessage` + `ModMessageReceived` checando `FromModID/Type`, host persiste, `PerScreen<T>` p/ split-screen. Refs: Modding Content/Machines/Objects/Recipe/Common-data/Multiplayer APIs (stardewvalleywiki.com).
- Canônico GDD 2024–2026: Schell *Art of Game Design* 3e cap.27, Fullerton *Game Design Workshop* 5e (2024), Adams *Practical Game Design*, IGDA Curriculum Framework, Schubert GDC 2007/08, Schreiber GDC 2025 (quatro one-pagers), Librande GDC 2010 (one-page), Adams & Dormans *Game Mechanics* (Machinations), Stenger 2023 (feature briefs), Unity Economy 2026, GDC 2014 Lehdonvirta (sinks), InvestGame LiveOps 2025.
- Cozy/agroecologia: MDA (Hunicke/LeBlanc/Zubek 2004), Lazzaro 4 Keys (GDC 2004), Bartle 1996 (+8 tipos), Horseshoe 2017 (Safety/Abundance/Softness), Sicart FDG 2015 (loops), Jenkins 2004 + Bogost 2007 (narrativa ambiental/retórica procedural), SEGAE (Jouan 2021), Sakuna/Frontiers 2024, Daum & Foureaux 2025 (J. Rural Studies), Holmgren 12 princípios, Coral Island/Roots of Pacha/Wylde Flowers/Terra Nil postmortems.

### F. Cobertura 4 Keys + Bartle + risco (auditoria research v1.2)

- **4 Keys:** Serious (soberania visível, cozinha coletiva) + Easy (experimentar composto/policultura) + People (CSA/feira/farmhands/Elena) + Hard (mastery rotação/planejamento sazonal). Mínimo 3/4 atendido; Serious é o diferencial.
- **Bartle sem PvP:** Achievers → troféus/heirlooms/soberania; Explorers → journal/lore; Socializers → CSA/cozinha/multi; Killers → ranking amigável feira (nunca destrutivo).
- **Checklist anti-Terra-Nil:** sem falha silenciosa (todo loss avisa antes); sem late "chasing numbers" (feira/CSA exigem variedade); sem mecânica cosmética (tese vive nas regras); sem moralismo (recompensa > bloqueio); sem salto Y1→Y2 sem rampa; overlay acessível (cor + padrão, `ReduceMotion`).
- **TDD handoff:** cada feature deste GDD linka 1–2 frases de exigência de engenharia para `specs/004-*/plan.md`; feature sem link = não-construível.
- **Monetização/ética:** mod gratuito MIT — "nunca à venda": saves, solo, sementes. Sem loja, sem gacha, sem dual-currency.
- **Cut list (corta primeiro):** romance Elena, console/mobile, localização além PT/EN, Green Roof jogável, greywater animado — cortam sem matar soberania.

---

*Próximo passo (spec-driven, gated): validar este GDD com humano → `speckit.specify` D3 (fiação item/máquina) → `speckit.plan/tasks/implement` slice 004 → só então `speckit.specify` D1 multiplayer real. Sem código antes do sim humano.*
