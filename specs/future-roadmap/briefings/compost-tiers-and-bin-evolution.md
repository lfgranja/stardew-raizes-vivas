# Briefing 1:1 — Compost Tiers & Composter Evolution (qualidade do compost + upgrades da composteira)

> Status: briefing (não implementado). Sem agrupamento de origem — nenhum outro briefing cobre este ativo. **Nunca aparece no `ROADMAP.md`.**
> Escopo interno: adiado explicitamente pela spec 002 (§Out of Scope). Ativa junto com `004-playable-compost-loop` (contrato de 3 frames + `Data/*` em C#).
> Citações-âncora da spec 002: `specs/002-soil-decay-compost/spec.md:91`, `:92`, `:190`, `:226-227`.
> Sistema existente sobre o qual a escada se apoia: `LivingRoots/Constants.cs:59-62` (`MaturationDays`, `MaturationMaxLevel`, `MaturationIdleResetDays`, `MaturationIncrementDays`).
> Não promover para `specs/` sem `/speckit.specify`.

## 0. Sumário executivo (TL;DR para o orquestrador)

**O quê:** duas escadas que a spec 002 adiou explicitamente (`spec.md:226-227`): **qualidade do compost** (item com mais força) e **evolução da composteira** (máquina melhor). Este briefing resolve a segunda em dados 100% nativos e *adiar* a primeira.

**Decisão central (§3, §6):** **ortogonais, com assimetria.** O *tier* é a **identidade comprada** da máquina (item `(BC)` distinto, `Data/Machines` com `MaxStack` 1/2/3) e **não** a maturidade. A maturidade (`MaturationMaxLevel = 5`) fica como está — e, sendo honesto, **hoje ela não tem efeito nenhum no yield** desde a FR-005a (`CompostingBinService.cs:77` fixa `outputCount = 1`). Uma escada de tiers de compost é **indefensável em dados**: `Data/Objects` não tem campo de tier/quality, e `Data/Machines` só escolhe o output por propriedades do **item de entrada** — jamais pelo estado da máquina. Fá-la exigiria `OutputMethod` (C#), o que contraria a FR-001a (máquina nativa).

**O que trava a implementação (bloqueante, verificado no código):** `CompostingBinService.cs:280` (em `LoadData`) e `:396` (em `PruneOrphanedBins`) comparam `obj.ItemId` com a **constante única** `ModConstants.CompostingBinItemId`. Um bin de tier 2/3 colocado no mundo é **podado como órfão no load do save**, levando a maturação junto. Isso exige trocar as duas comparações por teste de pertinência a um conjunto de tiers **antes** de qualquer arte nova.

**Bônus achado na pesquisa (bug latente, G6):** `CollectCompost` (`CompostingBinService.cs:79-89`) entrega os itens **num laço** e, se o inventário enche no meio, `return 0` mantendo o bin em `Ready` — mas o item já entregue fica no inventário. Com `outputCount == 1` isso é inofensivo; com `MaxStack == 2` ou `3` vira **duplicação repetível de compost**. Tem de ser corrigido *antes* da escada, não junto.

**Recomendação ao orquestrador:** promover **só a evolução da composteira** (3 tiers, `MaxStack` 1/2/3) a `specs/`; a qualidade do compost vira spec própria e separada, com no máximo 2 níveis.

## 1. Gap table

| # | Gap | Evidência | Impacto | Esforço |
|---|-----|-----------|---------|---------|
| G1 | **Maturidade × tier: acoplados ou ortogonais?** | `MaturationLevel` é por-bin (`CompostingBinStateModel.cs:28`), reseta para 1 após `MaturationIdleResetDays` (`CompostingBinService.cs:161-166`) e é clampeado no load (`:291`) — ou seja, é *aluguel*, não patrimônio. Sob a FR-005a ele **não altera o yield** (`:77`, `outputCount = 1` fixo) | **Alto.** Se maturidade virar tier, o jogador perde o tier por 2 semanas sem uso e precisa migrar o *stream de itens* de saves antigos (um único ID de compost em inventários) | M |
| G2 | **Representação do output do compost** | `Data/Objects` (oldid 193966) **não tem** campo de tier nem de quality — a lista completa é Name/DisplayName/Description/Type/Category/Price/Texture/SpriteIndex/ColorOverlayFromNextIndex/Edibility/IsDrink/Buffs/GeodeDrops*/ArtifactSpotChances/ContextTags/CanBeGivenAsGift/CanBeTrashed/ExcludeFrom*Collection/CustomFields. E `Data/Machines` (oldid 191352) avalia `RequiredItemId`/`RequiredTags`/`RequiredCount`/`Condition` contra o **input** ("The Condition field will check the *input* (not output) item") | **Alto.** Não existe rota em dados para "output depende do estado da máquina" → a escada de item vira `OutputMethod` (delegate C#) e contraria a FR-001a | M |
| G3 | **Save-compat de bins colocados de tier superior** | `CompostingBinService.cs:280` e `:396` testam `obj.ItemId != ModConstants.CompostingBinItemId`; logs de poda em `:300` e `:411`. `CreateDislodgedBinItem:220` recria sempre o tier 1 | **Bloqueante.** Bin tier 2/3 é podado como órfão no load → maturação perdida silenciosamente | S |
| G4 | **Custo de arte por tier** | 3 tiers × 3 frames de 16×32 = **9 células**; existem 3 (`composting_bin_{empty,processing,ready}.png`, 16×32 cada) | Médio. 6 células novas, e o frame 0 (vazio) **é** o ícone de inventário de um `(BC)` — os tiers precisam ser distinguíveis no menu de crafting, então o motivo de reforço tem de existir já no frame 0 | M |
| G5 | **Gating de progressão: o jogador nem sabe que a escada existe** | `maturity-tooltip` **não tem consumidor** — só aparece em `i18n/default.json:8` e `i18n/en.json:8`, nenhum C# a lê. E `Data/Machines` (oldid 191352) **não documenta nenhum campo de hover-text** — só `InvalidItemMessage` e `InvalidCountMessage` são tokenizáveis. A spec 002 FR-019 (`spec.md:151`) e o Q&A em `spec.md:85` afirmam "standard `Data/Machines` built-in hover text support" | Médio. A rota de implementação citada pela spec **não existe no schema documentado**; entregar tooltip exige patch de `DrawHover` (Harmony). Escada de progressão invisível = conteúdo morto | M |
| G6 | **Bônus (achado nesta pesquisa, não previsto): yield parcial com inventário cheio** | `CompostingBinService.cs:79-89` — laço de entrega + `return 0` no meio, bin permanece `Ready` | **Alto, pré-existente, ativado pela escada.** Com `MaxStack` 2/3 o jogador recebe 1–2 itens de graça e pode repetir. Inofensivo só porque `outputCount` é 1 | S |

## 2. Fontes (primárias com data de revisão / internas)

### Primárias (externas)

Lidas nesta sessão; `oldid` e data de última edição como exibidos no rodapé de cada página (domínio `stardewvalleywiki.com`).

| Página | `oldid` | Última edição | O que foi extraído |
|---|---|---|---|
| [`Modding:Recipe data`](https://stardewvalleywiki.com/Modding:Recipe_data) | **193377** | **20 May 2026, 12:12** | Formato de crafting em 6 segmentos; valores de unlock (`default`, `s <skill> <n>`, skills válidos `Farming/Mining/Fishing/Foraging/Luck*/Combat`); dump cru de `CraftingRecipes.xnb` da 1.6.15 com as receitas vanilla (§4) |
| [`Modding:Machines`](https://stardewvalleywiki.com/Modding:Machines) | **191352** | **27 February 2026, 12:59** | Chave = ID **qualificado**; `OutputRules`/`Triggers`/`OutputItem`; `MinutesUntilReady`/`DaysUntilReady` (dias vencem; nenhum = instantâneo); `AllowFairyDust` (default `true`); `ShowNextIndexWhileWorking`/`ShowNextIndexWhenReady` (default `false`); `AllowLoadWhenFull` (default `false`); `WobbleWhileWorking` (default `false`); `WorkingEffectChance` (default `0.33`); `IncrementMachineParentSheetIndex` empilha com `ShowNextIndex*`; **sem** campo de hover-text |
| [`Modding:Big craftables`](https://stardewvalleywiki.com/Modding:Big_craftables) | **190701** | **12 January 2026, 21:25** | `Fragility` 0/1/2 (0 = qualquer ferramenta); `CanBePlacedIndoors`/`Outdoors` (default `true`); `IsLamp` (default `false`); `Texture` (default `TileSheets/Craftables`); `SpriteIndex` (0 = top-left); `ContextTags`; `CustomFields`. Chave = ID **não qualificado** |
| [`Modding:Objects`](https://stardewvalleywiki.com/Modding:Objects) | **193966** | **8 August 2026, 10:59** | Lista completa de campos de `ObjectData` — usada para provar a **ausência** de tier/quality (§6, G2) |

**Não lidas** (e portanto não citadas como autoridade): `Modding:Item queries` (fonte dos "item spawn fields" que definiriam `MinStack`/`MaxStack` em `MachineItemOutput` — ver §14 risco 6), `Modding:Context tags`, `Modding:Items`, `Modding:Game state queries`.

**Precedente vanilla central desta escada** (do dump cru de `Recipe data` oldid 193377, dados 1.6.15) — as **três** receitas de evolução do jogo têm a mesma forma: *item base × 1 + um material premium*, destravadas **um nível de skill acima** da receita base.

| Vanilla | String crua | Leitura |
|---|---|---|
| `Deluxe Worm Bin` | `(BC)154 1 Moss 30/Home/DeluxeWormBin/true/s Fishing 8/` | Worm Bin (base) + 30 Moss, `Fishing 8` (base é `Fishing 4`) |
| `Quality Speed-Gro` | `726 1 Moss 5/Field/465 5/false/s Foraging 7/` | Speed-Gro (base) + 5 Moss, `Foraging 7` |
| `Deluxe Speed-Gro` | `725 1 881 3 768 1/Field/466 5/false/s Foraging 8/` | Quality Speed-Gro (base) + 3 do item 881 + 1 do 768, `Foraging 8` |

### Internas

| `file:line` | Fato usado |
|---|---|
| `LivingRoots/Constants.cs:56` | `CompostingBinKeyPrefix = "composting_bins_"` — prefixo da chave de save |
| `LivingRoots/Constants.cs:59-62` | `MaturationDays = 2`, `MaturationMaxLevel = 5`, `MaturationIdleResetDays = 14`, `MaturationIncrementDays = 7` |
| `LivingRoots/Constants.cs:63-74` | IDs (`CompostItemId`, `QualifiedCompostItemId`, `CompostingBinItemId`, `MachineCompostingBinId`, `CompostingBinRecipeId`), `CompostCraftingTab`, custos 50/25/15, `CompostCategoryId = -26`, `CompostStackMax = 999` |
| `LivingRoots/Constants.cs:77-78` | chaves de modData `LivingRoots.InputId` / `LivingRoots.DaysRemaining` |
| `LivingRoots/Constants.cs:60` | `MaturationMaxLevel` lido em apenas **3** sítios de produção: `CompostingBinService.cs:97`, `:151`, `:291` |
| `LivingRoots/Domain/CompostingBinState.cs:9-16` | `Empty = 0, Processing = 1, Ready = 2` — 3 estados lógicos ↔ 3 frames |
| `LivingRoots/Domain/Models/CompostingBinStateModel.cs:28` | `MaturationLevel` (1–5), default 1 — **por bin**, no modData |
| `LivingRoots/Domain/Models/CompostingBinStateModel.cs:31,34` | `ConsecutiveIdleDays` / `ConsecutiveActiveDays` |
| `LivingRoots/Domain/Models/CompostingBinStateModel.cs:44-102` | `PopulateFromModData` / `WriteToModData` — o par que faz o progresso sobreviver a pickup |
| `LivingRoots/Services/CompostingBinService.cs:77` | `int outputCount = 1;` com o comentário "The bin now yields exactly one compost item regardless of maturation level" — **a maturidade está morta no yield** (G1) |
| `LivingRoots/Services/CompostingBinService.cs:79-89` | laço de entrega + `return 0` no inventário cheio → yield parcial (G6) |
| `LivingRoots/Services/CompostingBinService.cs:97-100` | `MaturationLevel++` no collect, limitado a `MaturationMaxLevel` |
| `LivingRoots/Services/CompostingBinService.cs:150-156` | `ConsecutiveActiveDays >= MaturationIncrementDays` → sobe maturidade |
| `LivingRoots/Services/CompostingBinService.cs:161-166` | `ConsecutiveIdleDays >= MaturationIdleResetDays` → `MaturationLevel = 1` (o "aluguel" de G1) |
| `LivingRoots/Services/CompostingBinService.cs:172-184` | `OnObjectRemoved` — **descarta todo o estado do bin** ao remover |
| `LivingRoots/Services/CompostingBinService.cs:186-216` | `OnBinPlaced` — reidrata de modData; cria bin via factory sem ler o tier do mundo |
| `LivingRoots/Services/CompostingBinService.cs:218-238` | `CreateDislodgedBinItem` — **linha 220** recria sempre `ItemId = CompostingBinItemId` (perde o tier) |
| `LivingRoots/Services/CompostingBinService.cs:240-249` | `GetMaxStackSize` — 1 se houver modData de progresso, senão `CompostStackMax` |
| `LivingRoots/Services/CompostingBinService.cs:280` | `LoadData` — poda por `worldObj.ItemId == ModConstants.CompostingBinItemId` **(G3)** |
| `LivingRoots/Services/CompostingBinService.cs:291-292` | clamp de `MaturationLevel` a [1,5] e `ConsecutiveIdleDays` a [0,14] |
| `LivingRoots/Services/CompostingBinService.cs:338-341` | `SaveData` esparso: pula `Empty && MaturationLevel == 1 && ConsecutiveIdleDays == 0` |
| `LivingRoots/Services/CompostingBinService.cs:396` | `PruneOrphanedBins` — **segunda** comparação por constante única **(G3)** |
| `LivingRoots/Services/CompostingBinFactory.cs:9-17` | `CreateBin` — sempre `MaturationLevel = 1`, sem parâmetro de tier |
| `LivingRoots/ModEntry.cs:127` | receita atual: `.../Home/LivingRoots.CompostingBin 1/false/null/Composting Bin` — flag `false` e unlock `null`, ambos errados |
| `LivingRoots/ModEntry.cs:136-144` | bin registrado em `Data/Objects` como `(O)` — precisa sair dali |
| `LivingRoots/ModEntry.cs:157-181` | `Data/Machines` esqueleto: 1 regra, só `OutputItem`, sem `Triggers` nem tempo |
| `LivingRoots/i18n/default.json:8` e `LivingRoots/i18n/en.json:8` | `"maturity-tooltip": "Maturity: {0}x"` — **cópias byte-idênticas**, sem consumidor em C# (G5) |
| `LivingRoots/Assets/` | `compost.png` 16×16; `composting_bin_{empty,processing,ready}.png` 16×32 cada (dimensões via briefing `composting-bin-sheet.md:19-21`) |
| `specs/002-soil-decay-compost/spec.md:91` | "Preserves Jar tier: 50 Wood + 25 Stone + 15 Fiber … **Future evolutions (not implemented now) will use more materials**" |
| `specs/002-soil-decay-compost/spec.md:92` | "Early game item with **planned future evolutions/upgrades** (deferred to future roadmap)" |
| `specs/002-soil-decay-compost/spec.md:186,190` | receita do bin; "**Future Evolutions**: … more materials and higher-tier components" |
| `specs/002-soil-decay-compost/spec.md:226-227` | Out of Scope: "Compost quality tiers (basic compost only in v1)" / "Composting Bin evolutions/upgrades (planned for future roadmap, not implemented now)" |
| `specs/002-soil-decay-compost/spec.md:85,151` | FR-019 afirma "standard `Data/Machines` built-in hover text support" — **não confirmado** no schema (G5) |
| `specs/002-soil-decay-compost/data-model.md:59-77` | maturidade 1:1 → 5:1, +1 a cada 7 dias, reset após 14 dias ociosos (28 dias ativos até o teto) |
| `specs/002-soil-decay-compost/contracts/save-data-schema.md:69-117` | schema de `composting_bins_{saveId}_{locationName}`; `MaturationLevel` `minimum 1 / maximum 5` |
| `specs/002-soil-decay-compost/contracts/save-data-schema.md:180-183` | "Unknown fields ignored (forward compatibility)"; "Missing fields use defaults (backward compatibility)" |
| `specs/004-playable-compost-loop/briefings/composting-bin-sheet.md:149` | fórmula da folha: `IncrementMachineParentSheetIndex = 1` + `ShowNextIndexWhenReady = true` → 0/1/2 — **a escada de tiers compõe com ela, não a substitui** |
| `specs/004-playable-compost-loop/briefings/composting-bin-sheet.md:284` | regra de acessibilidade: nunca diferenciar estados só por matiz |
| `specs/004-playable-compost-loop/briefings/data-entries-machines-recipes.md` | briefing dono do estilo `AssetRequested` em C#; este repo **não** usa Content Patcher |
| `LivingRoots.Tests/CompostingBinServiceTests.cs:97,287,314,339,394` | contratos de coleta: `ReturnsCompostCount`, `OutputCountEqualsOne`, `AddsOneItemToInventory`, `WhenInventoryRefuses_RetainsBinReadyState`, `IncrementsMaturationCappedAt5` |
| `LivingRoots.Tests/FeatureStatusTests.cs:14-21` | lê `README.md` como **recurso embutido** e exige 3 strings literais `"(Planned)"` |
| `LivingRoots.Tests/ArchitecturalPlanTests.cs:48-55` | varre **só** `LivingRoots/docs/architectural_and_refactor_plans/**` — este briefing, em `specs/future-roadmap/briefings/`, **não** é coberto pela asserção de newline final |

## 3. Conceito — duas escadas, uma decisão de ortogonalidade

### 3.1 As duas escadas

**Escada A — evolução da composteira (3 tiers).** O jogador **compra** um `(BC)` melhor. É *identidade de item*: três IDs em `Data/BigCraftables`, três entradas em `Data/Machines`, três receitas em `Data/CraftingRecipes`. O que muda é o **yield por lote** (`MinStack`/`MaxStack` de `MachineItemOutput`): 1 → 2 → 3. Padrão vanilla exato (Mayonnaise Machine → Deluxe Mayonnaise Machine é 1 → 3 ovos).

**Escada B — qualidade do compost (N itens).** O jogador **acumula** maturidade num bin e recebe um item diferente. É *identidade de item do output*, decidida no momento da coleta.

### 3.2 A decisão: **ortogonais, com assimetria**

> **A maturidade NÃO vira o tier.** E o tier da bin **não** é lido da maturidade.

As duas escadas ficam em eixos separados, e cada uma responde a uma pergunta diferente:

| | Escada A (bin) | Escada B (compost) |
|---|---|---|
| O que muda | o `ItemId` do objeto colocado | o `ItemId` do item produzido |
| Quando | no crafting, uma vez, irreversível | a cada coleta, continuamente |
| Persistência | **nativa do jogo** (o save guarda o `ItemId` do objeto) | modData por bin, se a escolha precisar ser adiada |
| Custo de arte | 6 células novas (§5) | N ícones 16×16 novos |
| Ganho | 1× → 3× por lote, visível, óbvio | +15 → +45 solo, invisível até aplicada |

**Por que não fundir maturidade e tier num eixo só:**

1. **Ciclos de vida incompatíveis.** `MaturationLevel` **reseta para 1** após 14 dias ociosos (`CompostingBinService.cs:161-166`) e é clampeado a [1,5] no load (`:291`). É medição *efêmera*. Um tier que o jogador **pagou** para ter não pode desaparecer por esquecimento — isso seria o pior tipo de degressão possível num item de progressão.
2. **A maturidade não controla o tempo.** `MaturationDays = 2` é fixo por lote para qualquer tier. Se tier ⟹ mais rápido, isso é um segundo eixo escondido atrás do mesmo número, e o jogador não consegue prever o ciclo. O `Data/Machines` até dá a ferramenta certa (`ReadyTimeModifiers` / `ReadyTimeModifierMode`), mas usá-la exige um `CustomData` por tier — complexidade para um ganho invisível.
3. **Colapsar os dois produz uma matriz 3×3 de comportamento** cujo único eixo observável (quantidade por lote) é ditado pelo tier *e* pela maturidade ao mesmo tempo. Ilegível no hover da máquina, ilegível no ícone, impossível de balancear em texto.

**A assimetria que fica:** o **tier da bin limita teto de maturidade** — mas só na forma mais barata possível, e *não* porTier no código. Ver §9.

### 3.3 O que a maturidade faz, então?

Sob a FR-005a ela é **puramente informativa hoje** (o multiplicador 1–5 foi aposentado; `CompostingBinService.cs:77` devolve sempre 1). Este briefing **não** ressuscita o multiplicador. A maturidade ganha uma função nova e honesta, alinhada ao `maturity-tooltip` que já existe no i18n: ela é o **indicador de prontidão do lote** — " Maturity: 3x" passa a significar "sinal de maturação", não "multiplicador de saída". Ver §9 para a semântica travada.

## 4. Escada de tiers proposta (custos + gates)

### 4.1 A forma, validada contra o vanilla

As **três** receitas de evolução do jogo (dump 1.6.15, `Recipe data` oldid 193377) têm a mesma gramática:

| Vanilla | Ingredientes | Unlock |
|---|---|---|
| `Deluxe Worm Bin` | `(BC)154 1 Moss 30` — base + material | `s Fishing 8` (base: `Fishing 4`) |
| `Quality Speed-Gro` | `726 1 Moss 5` — base + material | `s Foraging 7` (base: `Foraging 1`) |
| `Deluxe Speed-Gro` | `725 1 881 3 768 1` — base + materiais | `s Foraging 8` |

Padrão: **`produto-base 1` + material complementar**, destravado **um nível de skill acima da receita base**. É exatamente essa forma que a escada LivingRoots deve copiar.

### 4.2 A escada

| Tier | `ItemId` | Ingredientes | Unlock | Yield/lote | Racional |
|---|---|---|---|---|---|
| **1 — Composting Bin** | `LivingRoots.CompostingBin` *(existente)* | `388 50 390 25 771 15` — 50 madeira, 25 pedra, 15 fibra | `default` (dia 1) | **1** | Já é o custo "Preserves Jar tier" travado na spec 002 (`spec.md:186`). `Chest` vanilla também é 50 madeira + `default`; `Preserves Jar` é 50 madeira + 40 pedra + 8 seiva, `Farming 4` — LivingRoots é **mais barato** em pedra e **sem gate**, o que a spec assume de propósito. **Não mexer.** |
| **2 — Reinforced Bin** | `LivingRoots.ReinforcedBin` | `LivingRoots.CompostingBin 1` + `709 25` (25 madeira maciça) + `390 60` (60 pedra) + `771 30` (30 fibra) | `s Farming 4` | **2** | Forma vanilla `base + material`. 25 madeira maciça + 60 pedra escala a partir dos 50/25 do tier 1 sem introduzir metal (o tier 2 é "reforço estrutural", não "indústria"). Gate `Farming 4` = nível do `Preserves Jar` (a referência explícita da spec 002) e 4 acima do `Scarecrow` (`Farming 1`), espelhando o salto `Fishing 4 → 8` do `Deluxe Worm Bin`. |
| **3 — Industrial Composter** | `LivingRoots.IndustrialComposter` | `LivingRoots.ReinforcedBin 1` + `335 15` (15 barras de ferro) + `386 10` (10 quartz) + `384 5` (5 monazita) | `s Mining 9` | **3** | Entra no metal só no tier 3, como o `Worm Bin` vanilla (1 barra de ferro + 1 pedrinha de irídio, `Fishing 4`) e o `Crystalarium` (2 barras + 99 pedra, `Mining 9`). **Gate `Mining 9` é intencional**: é o tier mais alto do jogo e coincide com `Crystalarium`/`Iridium Sprinkler`, então exige de fato ter visto mineração profunda. Funde as duas habilidades (o tier 2 é Farming, o tier 3 é Mining) — a bin vira umsink de progressão de mina, que é o que o jogador espera de "indústria". |

### 4.3 Notas de Project (Farming) — a curva de custo

| Tier | Custo incremental | Multiplicador de yield | Custo por unidade de compost |
|---|---|---|---|
| 1 | 50 madeira + 25 pedra + 15 fibra | 1× | ~base |
| 2 | +25 maciça + 60 pedra + 30 fibra | 2× | **~55% do tier 1** |
| 3 | +15 ferro + 10 quartz + 5 monazita | 3× | **~30% do tier 1** |

O tier 2 é o ponto de quebra de paridade: a partir dele, manter bins antigos é **pior** que fazer o upgrade. É o que faz a escada ser desejada sem ser obrigatória. O tier 3 é deliberadamente *caro em metal* para que não valha a pena converter todos os bins da fazenda — o jogador ganha uma ferramenta, não um efeito de mapa.

> ⚠️ **Verificar no build com `GamePath`:** os IDs numéricos usados como ingredientes (`709` = Madeira Maciça, `386` = Quartz, `384` = Monazita) foram lidos do dump cru de receitas da wiki e **não** foram confirmados contra os dados desempacotados do jogo neste ambiente. A confirmação real é `list_items` no console do SMAPI (§13), não a wiki.

## 5. Inventário de sprites

### 5.1 Regra de composição com o contrato de 3 frames (não-breaking)

O briefing `composting-bin-sheet.md:149` trava a fórmula de exibição:

```
IncrementMachineParentSheetIndex = 1  (no OutputItem)  →  0→1 processando E pronto
ShowNextIndexWhenReady = true        (no MachineData)  →  1→2 só quando pronto
```

Isso produz os índices **0 (empty) / 1 (processing) / 2 (ready)** numa folha com células 16×32 em sequência vertical. **A escada de tiers não altera essa fórmula.** Cada tier é um `BigCraftableData` separado, cada um com `SpriteIndex = 0` e sua **própria** `Texture` de 3 células. A fórmula roda igual em todas.

> **Não é preciso uma folha de 6 (ou 9) frames.** A "folha C" de 96×32 com `Frames [1,3,4,5]` (animação de vapor) continua sendo a **extensão opcional** já especificada em `composting-bin-sheet.md:138` — e se for adotada um dia, ela é adotada **para os três tiers de forma idêntica**, nunca como requisito desta escada.

### 5.2 Inventário por tier

| Tier | Asset (novo) | Células | Dimensões | Origem |
|---|---|---|---|---|
| 1 | `Assets/composting_bin_spritesheet.png` | 3 | **16×96** (3× 16×32, empilhadas) | **Já especificado** no briefing da 004. Arte existente, só reempacotada. |
| 2 | `Assets/reinforced_bin_spritesheet.png` | 3 | **16×96** | **3 células novas** |
| 3 | `Assets/industrial_composter_spritesheet.png` | 3 | **16×96** | **3 células novas** |

Total: **9 células de 16×32**, das quais 3 já existem como PNGs soltos. Uma folha por tier (e não uma folha 16×288 com os 9) porque `SpriteIndex` é **por item** — `Texture` + `SpriteIndex` são campos de `BigCraftableData`, então cada tier precisa apontar para a sua própria textura, ou todos os tiers leriam o mesmo atlas e o `ShowNextIndexWhenReady` mostraria o frame errado.

### 5.3 O que muda entre os tiers (direção de arte)

O **invariante** de `composting-bin-sheet.md:288` (base bit-idêntica entre os 3 estados) vale **dentro** de cada tier. **Entre** os tiers a base muda — é esse o sinal de progressão:

| Tier | Sinal visual (lido no frame 0, sem depender de cor) | Forma | Restrição |
|---|---|---|---|
| 1 | Caixa de ripas simples, madeira crua | Cilíndrica baixa | Estado atual, não mexer |
| 2 | Ripas **duplas** + **2 cantoneiras de ferro** nos cantos inferiores | 2 px mais alta | O ferro é a *forma* da evolução, não um adereço |
| 3 | **Boca de carga larga** (2 px mais larga) + **duto de ventilação** lateral + pé de sustentação | 18 px de largura, base elevada | A boca mais larga justifica visualmente o yield 3× |

**Regras que valem para os 6 frames novos** (herdadas de `composting-bin-sheet.md:269` e `:284`):

- **Silhueta primeiro.** A diferença entre os 3 tiers tem de ser reconhecível a 3 tiles de distância, em grayscale. Sem cor.
- **Frame 0 é o ícone de inventário.** Para `(BC)`, o ícone do inventário vem da mesma textura (`SpriteIndex` da `Texture` do `BigCraftableData`). A diferenciação de tier **precisa** estar no frame 0, não só no processing/ready.
- **Daltonismo / P&B.** A "boca mais larga" e as "cantoneiras" são forma, não matiz. Proibido diferenciar tiers só por cor (`composting-bin-sheet.md:284`).
- **Sem sombra de chão pintada.** Máquina desenhada tem sombra própria; pintar duplica e escurece (`composting-bin-sheet.md:134`).
- **Nada de tier que "pulca" estado.** Trocar de tier é trocar de item, não de estado — nenhum `SpriteIndex` cruza a fronteira de tier.

### 5.4 Custo

| | Hoje (tier 1) | Com 3 tiers |
|---|---|---|
| Arquivos PNG | 3 soltos + 1 folha | 3 folhas |
| Células 16×32 | 3 (existentes) | **9** (3 reaproveitadas + **6 novas**) |
| Bytes de arte nova | 0 | ~6 células ≈ o dobro do que já existe |

Isto é o **teto de arte** desta escada. Qualquer coisa além disso (variantes sazonais, HD, Alternative Textures) está no §15.

## 6. Representação do output de compost (decisão + alternativas rejeitadas)

Três opções foram avaliadas. A verificação que decide o caso é factual: **o que `Data/Objects` e `Data/Machines` realmente suportam** (lidas em oldid 193966 e 191352).

### 6.1 Alternativa (c) — `Data/Objects` com tiers → **REJEITADA, por impossibilidade**

A lista completa de campos de `ObjectData` (oldid 193966) é: `Name`, `DisplayName`, `Description`, `Type`, `Category`, `Price`, `Texture`, `SpriteIndex`, `ColorOverlayFromNextIndex`, `Edibility`, `IsDrink`, `Buffs`, `GeodeDrops`, `GeodeDropsDefaultItems`, `ArtifactSpotChances`, `ContextTags`, `CanBeGivenAsGift`, `CanBeTrashed`, `ExcludeFromRandomSale`, `ExcludeFromFishingCollection`, `ExcludeFromShippingCollection`, `CustomFields`.

**Não existe campo de tier, de quality, nem de potência.** O que mais se aproxima, `CustomFields`, é um dicionário **inerte**: o jogo não lê nada dele. Não há como declarar "este compost é tier 3" em `Data/Objects` de forma que o motor respeite.

A "tierização" via `Data/Objects` só existe em duas formas, e ambas são **(c) renomeada**:
- **N entradas separadas** (`LivingRoots.Compost`, `LivingRoots.CompostRich`, …) — que é exatamente a alternativa (a).
- **`ContextTags` discriminantes** — igualmente inerte para o motor; serve para *consulta*, não para *comportamento*.

### 6.2 Alternativa (b) — um item com `Object.Quality` em runtime → **REJEITADA**

`Quality` é um campo de runtime do `StardewValley.Object` (0/1/2), gravável em C# — e `Data/Machines` de fato o manipula via `CopyQuality` / `PriceModifiers`. É tecnicamente possível. É a escolha errada por três razões:

1. **Semântica trocada.** No jogo, quality é um multiplicador de **preço** (prata/ouro/irídio), não de potência. Compost aqui é um item de **uso interno, não vendido** — `spec.md:229` tira explicitamente "Compost trading or selling". Um tier que se manifesta como "venda por 3× mais" numa jogo onde o objeto não é vendido não comunica nada. A estrela de quality na UI seria o único feedback, e `composting-bin-sheet.md:284` já vetou diferença só por matiz/ícone.
2. **Não é determinístico para o jogador.** A quality é atribuída pela máquina no momento do output; o jogador não tem como antecipar o tier do lote. Uma escada de progressão precisa ser **previsível**, senão é loteria.
3. **Aplica a un tiers de bin errado.** A escada de bins (§3) muda **quantidade** por lote. `Quality` mudaria a **qualidade de uma unidade**. São eixos diferentes; usar `Quality` para expressar "tier de bin" apagaria a distinção entre "produzir mais" e "produzir melhor".

### 6.3 Alternativa (a) — N IDs distintos de item → **REJEITADA *nesta* escada; recomendada para a spec futura**

É a única que o motor respeita de verdade, e é o padrão vanilla (Mayonnaise/Deluxe Mayonnaise; Basic/Quality/Deluxe Speed-Gro). Mas tem dois custos que **não cabem aqui**:

- **Custo de asset**: cada ID novo precisa do seu ícone 16×16. O briefing `compost-icon.png.md` é dono do ícone atual e está fora deste escopo; 2 ícones novos é trabalho de arte, não de código.
- **Custo de gameplay que ainda não existe**: o que um compost "Raw" faz de diferente de um "Rich" se **o único consumidor é `CompostApplicationService`**, que aplica `RestorationAmount` e para? Seria preciso redesenhar o valor de restauração, o floating text (`"+15"`), o tooltip de solo e o balanceamento do ciclo de decay inteiro. Isso é uma spec de balanceamento, não um/item extra.

### 6.4 Decisão

> **Esta escada entrega só a evolução da bin (Escada A, §3.1). O output de compost permanece um único item `(O)LivingRoots.Compost`, quantity-only, governed pelo `MaxStack` da máquina conforme o tier.**

A escada de qualidade do compost (alternativa (a), N IDs) fica **recomendada como spec separada**, a ser especificada com o redesign do `RestorationAmount` como parte do escopo. Ela não é bloqueada por esta — apenas *postergada*. O gate para promovê-la é ter o `compost-icon.png` wired e um valor de restauração por tier decidido.

## 7. Data entry — `Data/BigCraftables` / `Data/Machines` / `Data/CraftingRecipes` / `Data/Objects`

**Estilo deste repo: registro programático em C# via `AssetRequested`. Não há Content Patcher** (ver `data-entries-machines-recipes.md`, e `composting-bin-sheet.md:27`). Todos os trechos abaixo são **parciais e ilustrativos** — a forma final segue o padrão de `ModEntry.cs:120-181` e do briefing dono.

### 7.1 `Data/BigCraftables` — 3 entradas (uma por tier)

| `BigCraftableData` field | Tier 1 | Tier 2 | Tier 3 | Notas |
|---|---|---|---|---|
| `Name` | `"CompostingBin"` | `"ReinforcedBin"` | `"IndustrialComposter"` | Sem espaços (interno) |
| `DisplayName` | i18n `composting-bin.name` | i18n `composting-bin.reinforced.name` | i18n `composting-bin.industrial.name` | Tokenizável |
| `Description` | i18n `composting-bin.description` | i18n `composting-bin.reinforced.description` | i18n `composting-bin.industrial.description` | Tokenizável |
| `Price` | `0` | `0` | `0` | Máquina não vendável |
| `Fragility` | `0` | `0` | `0` | Pickup com qualquer ferramenta — **obrigatório** (FR-005c) |
| `CanBePlacedIndoors` | `true` | `true` | `true` | Greenhouse conta como interior |
| `CanBePlacedOutdoors` | `true` | `true` | `true` | Farm |
| `IsLamp` | `false` | `false` | `false` | Sem luz (default; explícito) |
| `Texture` | `"LivingRoots/BigCraftables"` | `"LivingRoots/ReinforcedBin"` | `"LivingRoots/IndustrialComposter"` | **Uma por tier** (ver §5.2) |
| `SpriteIndex` | `0` | `0` | `0` | Base = frame empty. `+1`/`+2` vêm de `Data/Machines` |

```csharp
// Em OnAssetRequested, novo ramo (o briefing dono adiciona o de tier 1):
else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, StardewValley.GameData.BigCraftables.BigCraftableData>().Data;

        data[ModConstants.CompostingBinItemId] = BuildBinData(
            "CompostingBin",
            translation.Get("composting-bin.name").Default("Composting Bin"),
            translation.Get("composting-bin.description").Default("..."),
            ModConstants.CompostingBinTextureAsset);

        data[ModConstants.ReinforcedBinItemId] = BuildBinData(
            "ReinforcedBin",
            translation.Get("composting-bin.reinforced.name").Default("Reinforced Bin"),
            translation.Get("composting-bin.reinforced.description").Default("..."),
            ModConstants.ReinforcedBinTextureAsset);

        data[ModConstants.IndustrialComposterItemId] = BuildBinData(
            "IndustrialComposter",
            translation.Get("composting-bin.industrial.name").Default("Industrial Composter"),
            translation.Get("composting-bin.industrial.description").Default("..."),
            ModConstants.IndustrialComposterTextureAsset);
    });
}

// Helper privado — os 4 campos que são idênticos nos 3 tiers ficam num lugar só
private static StardewValley.GameData.BigCraftables.BigCraftableData BuildBinData(
    string name, string displayName, string description, string textureAsset)
{
    return new StardewValley.GameData.BigCraftables.BigCraftableData
    {
        Name = name,
        DisplayName = displayName,
        Description = description,
        Price = 0,
        Fragility = 0,                    // FR-005c: pickup com qualquer ferramenta
        CanBePlacedIndoors = true,
        CanBePlacedOutdoors = true,
        IsLamp = false,
        Texture = textureAsset,
        SpriteIndex = 0,                   // empty; +1/+2 vêm de Data/Machines
    };
}
```

### 7.2 `Data/Machines` — 3 entradas, uma por tier

| `MachineData` / `MachineOutputRule` field | Tier 1 | Tier 2 | Tier 3 | Notas |
|---|---|---|---|---|
| chave | `(BC)LivingRoots.CompostingBin` | `(BC)LivingRoots.ReinforcedBin` | `(BC)LivingRoots.IndustrialComposter` | **Qualificada** (oldid 191352) |
| `AllowFairyDust` | `false` | `false` | `false` | Default vanilla é `true`; desabilita (FR-001b) |
| `ShowNextIndexWhileWorking` | `true` | `true` | `true` | → frame 1 |
| `ShowNextIndexWhenReady` | `true` | `true` | `true` | → frame 2 (`composting-bin-sheet.md:149`) |
| `WobbleWhileWorking` | `true` | `true` | `true` | Cosmético |
| `Triggers` | 6× `RequiredTags` | **idênticos** | **idênticos** | Herdado da spec 002 FR-003 — **referenciar, nunca recopiar** |
| `OutputItem.ItemId` | `(O)LivingRoots.Compost` | idem | idem | **Único item de compost em toda a escada** (§6.4) |
| `OutputItem.DaysUntilReady` | `2` | `2` | `2` | `MaturationDays` — fixo (FR-001b) |
| `OutputItem.IncrementMachineParentSheetIndex` | `1` | `1` | `1` | 0→1 processando e pronto |
| **`OutputItem.MaxStack`** | **`1`** | **`2`** | **`3`** | ← **A ÚNICA DIFERENÇA ENTRE TIERS** |

> **Por que `MaxStack` e não `DaysUntilReady`.** `DaysUntilReady` é fixo em 2 pela FR-001b (2 dias é regra de design, não de balanceamento). `MinutesUntilReady` rebaixaria o ciclo a um número que o jogador não consegue prever sem consultar wiki, e quebraria o "2 dias" que aparece em três strings de i18n. `MaxStack` é **a diferença que o jogador vê imediatamente** ao recolher, e é o eixo que o vanilla usa para evoluir máquinas (Mayonnaise 1 → Deluxe Mayonnaise 3).

```csharp
else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, StardewValley.GameData.Machines.MachineData>().Data;

        // Os 3 tiers compartilham a MESMA lista de triggers (spec 002 FR-003).
        var triggers = new List<StardewValley.GameData.Machines.MachineOutputTrigger>
        {
            new() { Id = "Seeds",     RequiredTags = new List<string> { "category_seeds",     "!not_compostable" } },
            new() { Id = "Vegetables",RequiredTags = new List<string> { "category_vegetable", "!not_compostable" } },
            new() { Id = "Fruits",    RequiredTags = new List<string> { "category_fruits",    "!not_compostable" } },
            new() { Id = "Flowers",   RequiredTags = new List<string> { "category_flowers",   "!not_compostable" } },
            new() { Id = "Greens",    RequiredTags = new List<string> { "category_greens",    "!not_compostable" } },
            new() { Id = "Modded",    RequiredTags = new List<string> { "compostable_item",  "!not_compostable" } },
        };

        // $maxStack: 1 | 2 | 3 — o tier, em uma linha
        AddMachine(data, ModConstants.CompostingBinItemId,     triggers, ModConstants.Tier1MaxStack);
        AddMachine(data, ModConstants.ReinforcedBinItemId,     triggers, ModConstants.Tier2MaxStack);
        AddMachine(data, ModConstants.IndustrialComposterItemId, triggers, ModConstants.Tier3MaxStack);
    });
}

private static void AddMachine(
    Dictionary<string, StardewValley.GameData.Machines.MachineData> data,
    string itemId,
    List<StardewValley.GameData.Machines.MachineOutputTrigger> triggers,
    int maxStack)
{
    data[$"(BC){itemId}"] = new StardewValley.GameData.Machines.MachineData
    {
        AllowFairyDust = false,
        ShowNextIndexWhileWorking = true,
        ShowNextIndexWhenReady = true,
        WobbleWhileWorking = true,
        OutputRules = new List<StardewValley.GameData.Machines.MachineOutputRule>
        {
            new StardewValley.GameData.Machines.MachineOutputRule
            {
                Id = "Default",
                Triggers = triggers,
                OutputItem = new List<StardewValley.GameData.Machines.MachineItemOutput>
                {
                    new StardewValley.GameData.Machines.MachineItemOutput
                    {
                        ItemId = ModConstants.QualifiedCompostItemId,      // item ÚNICO (§6.4)
                        DaysUntilReady = ModConstants.MaturationDays,     // 2, fixo
                        IncrementMachineParentSheetIndex = 1,
                        MaxStack = maxStack,                                // ← tier
                    }
                }
            }
        }
    };
}
```

### 7.3 `Data/CraftingRecipes` — 3 receitas (6 segmentos, `default` / `s Farming 4` / `s Mining 9`)

Formato (oldid 193377): `ingredientes / (unused, Home) / yield / bigCraftable? / unlock / displayName`.

| Segmento | Tier 1 | Tier 2 | Tier 3 |
|---|---|---|---|
| 0 `Ingredients` | `388 50 390 25 771 15` | `LivingRoots.CompostingBin 1 709 25 390 60 771 30` | `LivingRoots.ReinforcedBin 1 335 15 386 10 384 5` |
| 1 (unused) | `Home` | `Home` | `Home` |
| 2 `Yield` | `LivingRoots.CompostingBin` | `LivingRoots.ReinforcedBin` | `LivingRoots.IndustrialComposter` |
| 3 `Big craftable?` | `true` | `true` | `true` |
| 4 `Unlock` | `default` | `s Farming 4` | `s Mining 9` |
| 5 `Display name` | (i18n) | (i18n) | (i18n) |

```csharp
if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
{
    e.Edit(editor =>
    {
        var data = editor.AsDictionary<string, string>().Data;

        // Tier 1: receita de base, destravada no dia 1
        data[ModConstants.CompostingBinRecipeId] =
            $"388 {ModConstants.CompostingBinWoodCost} " +
            $"390 {ModConstants.CompostingBinStoneCost} " +
            $"771 {ModConstants.CompostingBinFiberCost}" +
            $"/Home/{ModConstants.CompostingBinItemId}/true/default/Composting Bin";

        // Tier 2: base + materiais. "default" (dia 1), pois é a evolution recipe.
        data[ModConstants.ReinforcedBinRecipeId] =
            $"{ModConstants.CompostingBinItemId} 1 709 25 390 60 771 30" +
            $"/Home/{ModConstants.ReinforcedBinItemId}/true/default/Reinforced Bin";

        // Tier 3: base + metais.
        data[ModConstants.IndustrialComposterRecipeId] =
            $"{ModConstants.ReinforcedBinItemId} 1 335 15 386 10 384 5" +
            $"/Home/{ModConstants.IndustrialComposterItemId}/true/default/Industrial Composter";
    });
}
```

> ⚠️ **Verificar no build com `GamePath`:** (a) o formato exato do segmento 2 quando a receita **não** é a de dia 1 — em algumas versões o yield vanilla é numérico e o mod string só funciona no caminho `default`; (b) a lista exata de campos de `MachineItemOutput` (o wiki oldid 191352 só diz "common fields" e remete a "item spawn fields", que **não** foi lida nesta sessão). Se qualquer um dos dois divergir, o ajuste é mecânico e o briefing dono (`data-entries-machines-recipes.md` §6.3) já marca esse ponto.

### 7.4 `Data/Objects` — **inalterado** nesta escada

Uma única entrada, `(O)LivingRoots.Compost`, exatamente como está. Recordando por quê (§6.1): `Data/Objects` não tem como expressar tier. A entrada atual (`ModEntry.cs:146-154`, sem `Texture`/`SpriteIndex`) precisa do wiring de sprite que o briefing dono já especificó, mas **esta escada não acrescenta nada aqui**.

## 8. i18n

### 8.1 Chaves novas

| Chave | Valor `pt`/EN de fallback | Onde é usada |
|---|---|---|
| `composting-bin.reinforced.name` | `"Reinforced Bin"` | `BigCraftableData.DisplayName` (tier 2) |
| `composting-bin.reinforced.description` | ver abaixo | `BigCraftableData.Description` (tier 2) |
| `composting-bin.industrial.name` | `"Industrial Composter"` | `BigCraftableData.DisplayName` (tier 3) |
| `composting-bin.industrial.description` | ver abaixo | `BigCraftableData.Description` (tier 3) |
| `composting-bin.tier-indicator` | `"Tier {0}"` | eventual rótulo de tier (fora do escopo de wiring agora) |

Descrições propostas — o número **2 dias** e o **yield por tier** são os dois fatos que o jogador precisa:

```json
"composting-bin.reinforced.description":
  "A sturdier bin. Turns organic waste into rich compost in 2 days, and produces 2 compost at once.",
"composting-bin.industrial.description":
  "An industrial composter. Turns organic waste into rich compost in 2 days, and produces 3 compost at once."
```

> ⚠️ `composting-bin.description` (existente) diz *"Output increases with continuous use"* — resquício do multiplicador aposentado pela FR-005a, já sinalizado em `composting-bin-sheet.md:303`. **Esta escada torna a frase ainda mais errada**: com tiers, o output aumenta com *o tier da máquina*, não com uso contínuo. Corrigir a string existente é parte do escopo (é o mesmo item, agora com 3 descrições para manter coerentes).

### 8.2 O que muda com `maturity-tooltip`

A chave **não muda de formato** e **não ganha chaves irmãs**. A decisão de §3.3 é: `maturity-tooltip` continua sendo `"Maturity: {0}x"`, mas o `{0}` passa a ser lido como **indicador de prontidão do lote**, não multiplicador de saída.

| | Antes (spec 002) | Depois (esta escada) |
|---|---|---|
| Formato | `"Maturity: {0}x"` | `"Maturity: {0}x"` — **inalterado** |
| O que `{0}` é | multiplicador de output (1–5) | nível de maturação do lote (1–5), idem escala |
| Quem consome | **ninguém** (§G5) | **ninguém** (ainda) |

**A chave permanece sem consumidor.** Este briefing **não** entrega o tooltip: `Data/Machines` (oldid 191352) **não tem campo de hover-text** — os únicos campos tokenizáveis de interação são `InvalidItemMessage` e `InvalidCountMessage`, ambos para **rejeição de input**, não para estado. A spec 002 FR-019 (`spec.md:151`) e o Q&A em `spec.md:85` afirmam "standard `Data/Machines` built-in hover text support"; **isso não se confirma no schema documentado** e é registrado como gap (G5).

Entregar o tooltip exigiria um patch de draw no `Object` (via Harmony) para injetar texto no hover da máquina — trabalho de C# de renderização, que é **fora do escopo desta escada** (ver §15).

> **Armadilha de locale.** `i18n/default.json` e `i18n/en.json` são hoje **cópias byte-idênticas**. Qualquer chave nova tem de entrar nas **duas**, ou o build falha de forma obscura em um locale e não no outro. (O briefing dono de i18n é quem resolve a duplicação; esta escada só não deve exacerbate.)

## 9. Interação com `maturity-tooltip` e `MaturationMaxLevel = 5`

### 9.1 O que a escada **não** toca

`MaturationMaxLevel = 5` (`Constants.cs:60`) é lido em exatamente **3** sítios de produção, todos em `CompostingBinService.cs`:

| Sítio | O que faz |
|---|---|
| `:97` | `if (bin.MaturationLevel < ModConstants.MaturationMaxLevel) bin.MaturationLevel++;` no collect |
| `:151` | mesmo check no `ProcessDayStart` |
| `:291` | `Math.Clamp(bin.Value.MaturationLevel, 1, ModConstants.MaturationMaxLevel)` no load |

**Esta escada não modifica nenhum dos três.** `MaturationMaxLevel` continua 5, `MaturationIncrementDays` continua 7, `MaturationIdleResetDays` continua 14, `MaturationDays` continua 2. Isso é deliberado: mexer em qualquer um deles é mexer na FR-005a e na spec 002, que são **donas** dessas constantes.

### 9.2 A assimetria tier → teto de maturação

§3.2 definiu que o tier da bin **limita o teto de maturidade**. A implementação mais barata e mais honesta dessa ideia, sem tocar em `MaturationMaxLevel`, é via **`Data/Machines` → não**, porque teto de maturação não é campo de máquina. Ou seja: **o teto de maturidade por tier não é implementável nas data tables** sem um `CustomData` que nada lê.

Duas saídas, e a escolha é explícita:

| Opção | O que faz | Veredito |
|---|---|---|
| (i) Teto por tier em código | `MaturationLevel` clampeado contra um `maxLevel` por tier em `:97`/`:151`/`:291` | ❌ **Rejeitada.** `CompostingBinService` não sabe o tier do bin (o tier vive no `ItemId` do objeto do mundo, não no `CompostingBinStateModel` — precisaria de um campo novo no DTO, nova propriedade, novo teste, e o `PruneOrphanedBins` precisaria populá-lo). Custo alto, e o benefício é um teto que o jogador **nunca vê** (§9.3). |
| (ii) Maturação **inalterada** por tier | `MaturationMaxLevel = 5` para os 3 tiers | ✅ **Escolhida.** YAGNI. O teto de maturação permanece 5 em todos os tiers. |

### 9.3 Por que o teto por tier não compensaria

O teto de maturação só é observável se a maturagem **afetar algo**. Hoje ela **não afeta** nada (§6.4, `outputCount = 1` fixo, FR-005a). Um teto de 3 num tier 1 e 5 num tier 2 é **invisível ao jogador** — não há HUD, não há tooltip (G5), não há diferença no output. Ele só apareceria num teste unitário.

Consequência da escolha (ii): **o único eixo de progressão observável da escada inteira é o `MaxStack` (quantidade por lote).** Isso é bom — é o eixo que o jogador vê ao recolher, e é o eixo que o vanilla usa para evoluir máquinas. Significa também que a escada **não** precisa de nenhuma propriedade de maturação por tier, e que a Complexity que ela adiciona fica confinada às data tables.

## 10. Save-compat e bins já colocados

### 10.1 O que acontece com saves existentes

**Cenário: jogador tem `N` bins de tier 1 colocados na farm, com maturação salva, no momento em que a escada entra.**

| Passo | Comportamento | Risco |
|---|---|---|
| Carregamento do save | `LoadData` (`:251`) lê `composting_bins_{saveId}`. Para cada tile, verifica `worldObj.ItemId == ModConstants.CompostingBinItemId` (`:280`) | ✅ Passa — tier 1 mantém o ID **exato** de hoje. Nenhum registro é podado. |
| `PruneOrphanedBins` (`:373`) | Mesma checagem em `:396` | ✅ Passa, pelos mesmos motivos |
| Player usa o tier 1 | Idêntico a hoje, **exceto** que agora `MaxStack = 1` vem da `Data/Machines` em vez do `outputCount = 1` hardcoded em `:77` | ✅ **Idêntico** — mas o código C# precisa deixar de hardcodar (§12) |
| Player crafta tier 2 | Nova receita; **não** toca em saves | ✅ |
| Player coloca tier 2 | Novo objeto com `ItemId` novo. `OnBinPlaced` cria estado **novo** via factory | ⚠️ Precisa ser **adicionado** ao conjunto aceito — ver §10.2 |

**Nenhuma migração de save é necessária.** Porque o tier vive no `ItemId` do objeto do mundo, e objects do mundo são **persistidos nativamente pelo jogo** (não pelo mod). O modData (`composting_bins_{saveId}`) não tem campo de tier, e **não precisa ter** — o tier é derivado do `ItemId` que o jogo já salvou.

### 10.2 O ponto de dados: a autoridade — `CompostingBinService.cs:280` e `:396`

Este é o **único** lugar no código que decide "isto é um bin válido?". Hoje:

```csharp
// :280 (LoadData)
worldObj.ItemId == ModConstants.CompostingBinItemId

// :396 (PruneOrphanedBins)
if (!location.Objects.TryGetValue(tileVec, out var obj) || obj.ItemId != ModConstants.CompostingBinItemId)
```

Ambas as checagens precisam virar **pertinência a um conjunto de tiers**:

```csharp
// Onde? Um helper estático novo, p.ex. em CompostingBinFactory.cs
// ou como static readonly HashSet<string> em ModConstants.
public static bool IsCompostingBin(string? itemId)
{
    return itemId == ModConstants.CompostingBinItemId
        || itemId == ModConstants.ReinforcedBinItemId
        || itemId == ModConstants.IndustrialComposterItemId;
}
```

**Sem esse helper, o tier 2/3 é podado como órfão no load** (o log de poda em `:300` e `:411` dispararia) e a maturação é perdida. Este é o **risco nº 1** (§14).

### 10.3 O que acontece com o `CreateDislodgedBinItem`

`:220` recria **sempre** o tier 1:

```csharp
var item = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
```

Se o jogador remove um bin de tier 3 com machado, recebe um item de tier 1. O `ItemId` correto está no objeto do mundo (`binItem.ItemId`), não numa constante. **Correção necessária** (mesmo caminho de `§10.2`): ler o `ItemId` do objeto do mundo, não reconstruí-lo. O modData de progresso (`ModDataInputIdKey`/`ModDataDaysRemainingKey`) já é copiado corretamente (`:230-233`) — **só o `ItemId` está errado**.

### 10.4 Origem dos dados

| Dado | Onde vive | Persistido por |
|---|---|---|
| Tier do bin | `ItemId` do objeto do mundo | **Jogo nativamente** (save `objects`) |
| Estado do lote (`State`, `InputItemId`, `InputTimestamp`, `MaturationLevel`, `ConsecutiveIdleDays`, `ConsecutiveActiveDays`) | modData JSON `composting_bins_{saveId}` | **Mod** (`IModDataService`, prefix `Constants.cs:56`) |
| Progresso de um bin **descolgado** (na mão / no debris) | `Item.modData` (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`) | **Jogo nativamente** (save `items`) |

Nenhum destes três precisa de campo novo para a escada de tiers.

## 11. Constantes novas (`LivingRoots/Constants.cs`, classe `ModConstants`)

> Recordatório de convenção (AGENTS.md raiz): a classe `ModConstants` é declarada em **`LivingRoots/Constants.cs`** — nome de arquivo ≠ nome de classe, e **não existe** `ModConstants.cs`. Tudo em `LivingRoots/Constants.cs`, na seção `// Decay & Compost Constants` a partir de `:55`.

### 11.1 IDs (padrão `LivingRoots.<Nome>`, espelhando `:63-67`)

```csharp
// Tier 1 — já existe, não duplicar
public const string CompostingBinItemId = "LivingRoots.CompostingBin";          // :65 (existente)

// Tiers 2-3 — novos
public const string ReinforcedBinItemId = "LivingRoots.ReinforcedBin";
public const string IndustrialComposterItemId = "LivingRoots.IndustrialComposter";

// Qualificados, para Data/Machines — o prefixo (BC) é obrigatório na chave
public const string QualifiedCompostingBinItemId = "(BC)LivingRoots.CompostingBin";
public const string QualifiedReinforcedBinItemId = "(BC)LivingRoots.ReinforcedBin";
public const string QualifiedIndustrialComposterItemId = "(BC)LivingRoots.IndustrialComposter";

// Receitas
public const string ReinforcedBinRecipeId = "LivingRoots.ReinforcedBinRecipe";
public const string IndustrialComposterRecipeId = "LivingRoots.IndustrialComposterRecipe";
```

> **Não** reaproveitar `MachineCompostingBinId` (`:66`) como se fosse um segundo ID — ele é duplicata de `CompostingBinItemId` e o briefing dono já o marcou como "candidato a alias único futuro, fora do escopo mínimo". O padrão `(BC)` + `ItemId` elimina a necessidade dele nos 3 tiers novos; eliminar a duplicata existente é separado.

### 11.2 Custos e yield (§4.2)

```csharp
// Tier 2
public const int ReinforcedBinHardwoodCost = 25;
public const int ReinforcedBinStoneCost = 60;
public const int ReinforcedBinFiberCost = 30;

// Tier 3
public const int IndustrialComposterIronCost = 15;
public const int IndustrialComposterQuartzCost = 10;
public const int IndustrialComposterMonaziteCost = 5;

// Yield por tier — a ÚNICA diferença funcional entre os 3 Data/Machines (§7.2)
public const int Tier1MaxStack = 1;
public const int Tier2MaxStack = 2;
public const int Tier3MaxStack = 3;
```

### 11.3 Assets (§5.2)

```csharp
// Nomes de asset servidos via e.LoadFromModFile<T> — uma por tier (§5.2)
public const string CompostingBinTextureAsset = "LivingRoots/BigCraftables";
public const string ReinforcedBinTextureAsset = "LivingRoots/ReinforcedBin";
public const string IndustrialComposterTextureAsset = "LivingRoots/IndustrialComposter";
```

### 11.4 O que **não** precisa de constante

| Item | Por que não |
|---|---|
| IDs numéricos de ingrediente (`388`, `709`, `386`, `384`, `335`, `390`, `771`) | São índices vanilla, não tunáveis do mod. Hoje `ModEntry.cs:127` já os usa inline. A exeção é os **custos** (50/25/15), que são tuning e têm constante (`:69-71`) — os custos novos seguem o mesmo padrão acima. |
| `DaysUntilReady` por tier | `MaturationDays` (`:59`) já é a fonte única, e é fixo em 2 (§7.2). |
| Teto de maturidade por tier | **Rejeitado** (§9.2 opção ii). |
| Strings de i18n | São chaves de literal, não constantes (§8.1). |

## 12. Plano de testes

### 12.1 Arquivos que mudam

| Arquivo | O que muda | Detalhe |
|---|---|---|
| `LivingRoots.Tests/AssetRegistrationTests.cs` | **3 → 6** entradas `BigCraftableData`; **1 → 3** entradas `Data/Machines` | Assertions novas: (a) cada `BigCraftableData` com `Fragility==0`, `CanBePlacedIndoors && CanBePlacedOutdoors`, `Texture` não-vazia, `SpriteIndex==0`; (b) `Texture` **distinta** por tier (o teste que trava §5.2); (c) cada `MachineData` com `AllowFairyDust==false`, `DaysUntilReady==2`, `IncrementMachineParentSheetIndex==1`, `ShowNextIndexWhileWorking && ShowNextIndexWhenReady`, e **`MaxStack` == 1/2/3 respectivamente**; (d) as 3 receitas com 6 segmentos, `[3]=="true"`, `[4]` ∈ {`default`, `default`, `default`}, yield correto |
| `LivingRoots.Tests/CompostingBinServiceTests.cs` | `CollectCompost` deixa de hardcodar `outputCount = 1` | O teste `CollectCompost_OutputCountEqualsOne` (`:287`) **precisa mudar** — passa a `OutputCountEqualsMaxStack`. Também `CollectCompost_FromReadyBin_ReturnsCompostCount` (`:97`), `CollectCompost_AddsOneItemToInventory` (`:314`) e `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` (`:339`) |
| `LivingRoots.Tests/CompostingBinPersistenceTests.cs` | `PruneOrphanedBins` aceita 3 IDs | Novo teste: **um registro de tier 2/3 NÃO é podado** (este é o teste que trava o risco nº1, §14) |
| `LivingRoots.Tests/BinPickupTests.cs` | `CreateDislodgedBinItem` preserva o `ItemId` | Novo teste: deslog(t)ar um bin de tier 3 devolve item de tier 3, não tier 1 (§10.3) |
| `LivingRoots.Tests/CompostingBinFactoryTests.cs` | Factory pode receber o tier | Se `CompostingBinStateModel` ganhar um campo de tier (§9.2 opção i foi rejeitada, então **provavelmente não** — confirmar antes de mexer) |
| `LivingRoots.Tests/CompostingBinConstantsTests.cs` | Constantes novas | `ReinforcedBinItemId`, `IndustrialComposterItemId` distintos entre si e do tier 1; `Tier1MaxStack < Tier2MaxStack < Tier3MaxStack` |

### 12.2 Testes que **não podem quebrar** (invioláveis)

| Teste / arquivo | Por que é intocável |
|---|---|
| `CollectCompost_IncrementsMaturationCappedAt5` (`:394`) | É o **T049b / FR-005a** — a invariante "maturidade sobe até 5, mas o yield é sempre 1 item" da spec 002. A escada **não** altera maturidade (§9.1). Este teste é a prova executável de que a maturidade e o tier ficaram ortogonais. |
| `FeatureStatusTests.Readme_FeaturesMarkedAsPlanned_ContainsPlannedPrefix` (`:14`) | Lê o `README.md` como **recurso embutido** e exige 3 strings literais `"(Planned)"`. Se a escada vier acompanhada de mudança no README (ex.: listar as 3 bin tiers), o `--no-build` vai ler a cópia **desatualizada** e falhar. Rebuild obrigatório. |
| `ArchitecturalPlanTests` (`:48-55`) | Varre **apenas** `LivingRoots/docs/architectural_and_refactor_plans/**` e exige newline final. **Este briefing está em `specs/future-roadmap/briefings/`, fora dessa árvore** — não é coberto. Mas os docs que a escada vier a gerar, sim. |
| `CompostingBinStateModelTests.cs` | `PopulateFromModData`/`WriteToModData` não mudam (nenhuma chave de modData nova — §10.4). |
| `CompostApplicationServiceTests.cs` | `RestorationAmount` não muda (§6.4 — um único item). |
| `OrganicWasteValidatorTests.cs` | O conjunto compostável não muda. Os 6 `RequiredTags` são **os mesmos** nos 3 tiers (§7.2) — se alguém divergence, é bug de paridade. |
| `MultiplayerConcurrencyTests.cs` | Tiers são 3 máquinas **independentes** com 3 keys separadas. A atomicidade do lock em `CompostingBinService._lock` não muda. **Nenhuma regressão esperada** — e nenhum teste novo é preciso aqui. |
| `SoilHealth*`, `Security*`, `Dot*`, `Path*`, `*Sanitiz*` | Completamente ortogonais. |

### 12.3 Cobertura nova mínima (por-tier, sem explosão combinatória)

Como `MaxStack` é a única diferença funcional, a matriz de testes é **1 tier × 3 asserts**, replicada:

1. Para cada tier `t` ∈ {1,2,3}: dado um bin `t` em `Ready`, quando `CollectCompost`, então retorna `MaxStack(t)` e adiciona `MaxStack(t)` itens ao inventário.
2. Para cada tier: dado um bin `t` em `Ready` com inventário cheio, quando `CollectCompost`, então retorna `0` e o bin **permanece `Ready`** (paridade com `:339`).
3. Para cada tier: dado um registro de save com `ItemId` de tier `t`, quando `LoadData`, então o registro **sobrevive** ao prune.

Total: **9 testes novos**. Nenhum teste de tier 2 × tier 3 combinado é necessário, porque os tiers nunca interagem (são máquinas distintas em tiles distintos, por `§3.2`).

## 13. Verificação em jogo

> O PR template deste repo exige teste manual (`AGENTS.md` raiz, "Commits and PRs"). Esta escada **não é** verificável só por `dotnet test` — `Data/Machines` só é exercitado pelo motor real.

### 13.1 Setup

```bash
GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release
GamePath=$GP dotnet test --no-build --configuration Release
dotnet format Stardew-LivingRoots.sln --verify-no-changes
```

### 13.2 Passos manuais (~35 min)

1. **Dia 1, inventário limpo.** Abrir o menu de craft → aba `Home`. Devem aparecer **3** receitas de bin: "Composting Bin" (visível), "Reinforced Bin" e "Industrial Composter" (as duasevolution recipes, `default`).
2. **Craft tier 1.** Confirma consumo de 50 madeira + 25 pedra + 15 fibra. Item recebido: `(BC)LivingRoots.CompostingBin` (não Error Item).
3. **Colocar na farm + Greenhouse.** Frame 0 (empty) de cada tier. **Lado a lado**, os 3 tiers têm que ser distinguíveis em **grayscale** (§5.3) — este é o teste de arte que importa.
4. **Alimentar os 3 bins** com o mesmo resíduo (ex.: 1 couve). Todos devem aceitar (mesmos 6 `RequiredTags`).
5. **Item inválido** (ex.: 388 madeira) em cada bin → `cancel` + shake, nada consumido, frame continua 0.
6. **Dormir 2 noites.** Os 3 devem virar frame 2 (ready) com a bolha vanilla, **no mesmo dia** (mesmo `DaysUntilReady`).
7. **Coletar de cada um, um de cada vez.** Tier 1 → 1 compost. Tier 2 → 2. Tier 3 → 3. **Este é o teste central da escada.**
8. **Inventário cheio** antes de coletar cada tier → toast vermelha, bin **permanece ready**, e coletar de novo dá o yield completo (não parcial — §14 risco 2).
9. **Pickup de tier 3 com machado** (`:220`) → o item no inventário deve ser **tier 3**, não tier 1. Este é o teste do §10.3.
10. **Save/reload com os 3 bins colocados, cada um num estado diferente** (empty / processing d1 / ready). Nenhum registro pode sumir do log. Procurar por `Pruned orphan` no log do SMAPI — **não deve aparecer** (§14 risco 1).
11. **Maturidade intacta:** um bin que estava em `MaturationLevel 3` antes do reload deve continuar em 3 depois (§10.1). Verificar via hover ou via log de `LoadData`.
12. **Screenshots** dos 3 tiers em meio-dia, noite, chuva, inverno + grayscale (§5.3). Teste de redução a 50%: ainda se distinguem?

### 13.3 Comandos de console SMAPI

| Comando | O que verificar |
|---|---|
| `list_items LivingRoots` | Devem listar `(BC)LivingRoots.CompostingBin`, `(BC)LivingRoots.ReinforcedBin`, `(BC)LivingRoots.IndustrialComposter`, `(O)LivingRoots.Compost`. **Nenhum Error Item.** |
| `player_additem LivingRoots.ReinforcedBin 1` | Confirma que o item existe e é resolúvel (evita depender do gate `default` para o teste) |
| `player_additem LivingRoots.IndustrialComposter 1` | idem para o tier 3 |
| `debug listtags` (com um resíduo na mão) | Confirma as tags `category_*` / `compostable_item` — **o resultado tem que ser o mesmo nos 3 bins** (§12.2) |
| `reload_i18n` ou reiniciar | Confirma que as 3 descrições novas aparecem no hover dos 3 bins differentes |

> ⚠️ **`reload_i18n` não foi confirmado nesta sessão** — verificar o comando exato no `SMAPI.log` ou na doc do console antes de usar. Alternativa confiável: reiniciar o jogo.

## 14. Riscos

| # | Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|---|
| **1** | **`PruneOrphanedBins` poda bins de tier 2/3** no load, porque `:280` e `:396` comparam com a constante única. O jogador perde a maturação do bin, silenciosamente (só um log em `Trace`). | **Certa** — é o estado atual do código | **Alto** — perda de progresso sem aviso | Helper `IsCompostingBin(itemId)` (§10.2) + o teste de persistência do §12.3 item 3. **Fazer primeiro, antes de qualquer arte.** |
| **2** | **Yield parcial** — `CompostingBinService.cs:79-89` entrega os itens **um a um** e, se o inventário encher no meio, `return 0` mantendo o bin em `Ready`. Com `MaxStack = 2` ou `3`, o jogador recebe 1–2 itens de graça e pode repetir. **Já é um bug hoje** (inofensivo só porque `outputCount` é 1); a escada o **ativa**. | **Certa** (em tier ≥ 2) | **Alto** — item grátis repetível | Trocar o laço por: checar capacidade **antes** de entregar, ou entregar em transação com rollback. Note que a spec 002 tem FR-005b ("inventário cheio → nada consumido"); o código atual **viola** isso em tier ≥ 2. |
| 3 | Upgrade de tier **não** preserva o lote em andamento — o jogador tem que estar com o bin vazio para craftar o tier 2, senão o `ModData` do item é perdido na receita | Alta | Médio | Documentar na descrição do tier 2: "upgrade com o bin vazio". Ou aceitar e verificar em jogo se o crafting de receita `default` consome o `modData` (se consume, é o comportamento vanilla para upgrades, tipo `Quality Speed-Gro`) |
| 4 | `Data/Machines` **não tem** campo de hover-text (oldid 191352) — a spec 002 FR-019 e o Q&A em `spec.md:85` **afirmam que existe**. Se alguém construir o tooltip sobre essa premissa, o build falha ou o texto nunca aparece | Média | Médio | Gap G5 registrado. O `maturity-tooltip` **fica sem consumidor** nesta escada (§8.2). Não construir tooltip aqui. |
| 5 | `DaysUntilReady` **é opcional** e "se nenhum é especificado, o item fica pronto instantaneamente" (oldid 191352). Se alguém esquecer de replicar nos 2 novos `Data/Machines`, o compost sai na hora | Média | Alto | Teste de `AssetRegistrationTests` por tier (§12.1c). |
| 6 | `MachineItemOutput.MaxStack` — o wiki (oldid 191352) só diz "common fields" e remete a "item spawn fields" (que **não** foi lida nesta sessão). O campo pode ter outro nome ou nível | Média | Médio | **Verificar no build com `GamePath`.** Se divergir, o compilador acusa e o ajuste é mecânico. Não assumir. |
| 7 | Os IDs numéricos de ingrediente (`386` quartz, `384` monazita) vieram do dump da wiki, **não** foram confirmados contra os dados do jogo | Média | Médio (receita não funcional) | `list_items LivingRoots` + testar o craft de cada tier em jogo (§13.3). Considerar `Moss` (string) no lugar, como faz o `Deluxe Worm Bin` vanilla — mas confirmar que string IDs funcionam em receitas de mod |
| 8 | Os 6 frames novos podem **regressar a direção de arte** (a base de madeira do tier 1 é bit-idêntica nos 3 estados; os tiers novos não têm essa restrição testada) | Méda | Médio | O teste de redução a 50% do §13.2 item 12 é o gate. Não aceitar tier que só se distingue por matiz (§5.3) |

## 15. Fora de escopo

| Fora de escopo | Por quê / onde pertence |
|---|---|
| **Escada de qualidade do compost** (N IDs de item) | §6.4 — spec separada, precisa de `RestorationAmount` por tier + ícones 16×16 (`compost-icon.png.md`) + rebalanceamento do ciclo de decay |
| Mudança em `MaturationMaxLevel`, `MaturationIncrementDays`, `MaturationIdleResetDays` | §9.1 — pertencem à spec 002, que é a dona (FR-005a) |
| Teto de maturação por tier | §9.2 opção (i) — rejeitada por YAGNI; o tier **não** ganha essa propriedade |
| Velocidade por tier (`MinutesUntilReady`, `ReadyTimeModifiers`) | §7.2 — viola a FR-001b (2 dias fixos) |
| Entrega do `maturity-tooltip` (patch de draw / Harmony) | §8.2 — gap G5; `Data/Machines` não tem o campo. C# de renderização |
| `AllowLoadWhenFull: true` (fila de insumos) | Quebraria a FR-005a ("um lote por vez") e o frame 0/1/2 do contrato da 004 |
| Upgrade **no tile** (colocar tier 2 em cima de tier 1) | `Data/Machines` não tem trigger para "substituir objeto colocado". Seria C# de interação — escopo maior |
| Reciclagem / depreciação do tier 1 | Não é problema de gameplay: tier 1 continua útil (Farming baixo, sem metal) |
| Arte de `compost.png` (ícone do output) | Briefing `compost-icon.png.md` é o dono; §6.4 mantém **um** ícone |
| Folha "C" (96×32, `Frames [1,3,4,5]`, animação de vapor) | §5.1 — extensão opcional já especificada em `composting-bin-sheet.md:138`. Se adotada, é **para os 3 tiers de forma idêntica**, nunca como requisito |
| Variantes sazonais / diurnas, HD, Alternative Textures | §5.4 — teto de arte é 6 células novas |
| Venda / trade de compost | `spec.md:229` — explicitamente fora de escopo da spec 002, e por consequência desta |
| Aplicação automatizada de compost | `spec.md:228` |
| Fertilizer items distintos de compost | `spec.md:231` |
| Verificação live 2 jogadores | Roadmap D1 |

---

Última verificação: esqueleto e placeholders `TBD-*` desta mesma sessão. Wiki lidas hoje com `oldid` e data de edição na §2: `Modding:Recipe data` (193377, 20 May 2026), `Modding:Machines` (191352, 27 Feb 2026), `Modding:Big craftables` (190701, 12 Jan 2026), `Modding:Objects` (193966, 8 Aug 2026). Internos em `dev` @ `72f0c85`: `Constants.cs:56-78`, `ModEntry.cs:120-181`, `CompostingBinService.cs` (íntegro), `CompostingBinFactory.cs`, `CompostingBinState.cs`, `CompostingBinStateModel.cs`, `i18n/default.json`, `i18n/en.json`, `specs/002-soil-decay-compost/{spec.md:82,85,91,92,151,186,190,226-227,data-model.md:59-77,contracts/save-data-schema.md}`, `specs/004-playable-compost-loop/briefings/{composting-bin-sheet,data-entries-machines-recipes}.md`, `LivingRoots.Tests/` (lista de arquivos + `:97,287,314,339,394` de `CompostingBinServiceTests.cs`, `:14-21` de `FeatureStatusTests.cs`, `:48-55` de `ArchitecturalPlanTests.cs`). **Pendente de build com `GamePath`:** membros exatos de `MachineItemOutput` (risco 6), IDs numéricos de ingrediente (risco 7), forma do segmento 2 da receita em recipes não-`default` (§7.3). **Pendente de jogo:** §13 inteiro, incluindo a confirmação de que `maturity-tooltip` segue sem consumidor (G5).
