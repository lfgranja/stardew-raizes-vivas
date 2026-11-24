# Briefing — End Game Year 4+: Soberania e Comunidade (ICLF, Greywater, Fair, CSA, Cozinha Coletiva, Food Sovereignty)

> **Escopo:** assets futuros do ROADMAP (End Game, Year 4+) — `ROADMAP.md:55-70`, Epics 14–19 (`LivingRoots/docs/EPICS_USER_STORIES.md:90-129`).
> **Status:** nenhum destes assets tem spec, plan ou tasks. Este briefing é insumo para futuros `speckit.specify` — não é especificação.
> **Leitura interna complementar:** `LivingRoots/docs/AGROECOLOGY_CONCEPTS.md` (ICLF §41, Food Sovereignty §50–56), `LivingRoots/Constants.cs` (padrão de magic numbers), `LivingRoots/ModEntry.cs` (composition root único).

---

## 1. Visão geral e tese de design

### 1.1 O que o End Game precisa provar

Early Game (Year 1) ensina solo e composteira. Mid Game (Year 2–3) fecha o ciclo na porteira (rotação, adubação verde, animais, insetos, farmácia viva, bioconstrução). O End Game (Year 4+) tem tese diferente e **política**: expandir a filosofia agroecológica para o vale inteiro, substituir o sistema Joja e atingir "Food Sovereignty" (soberania alimentar).

Os 7 assets deste briefing formam um arco único:

| # | Asset (ROADMAP) | Epic / US | Papel no arco |
|---|---|---|---|
| 1 | ICLF trees — Acacia / Gliricidia | Epic 14 (Issue #16): US-14-01 (#51), US-14-02 (#52), US-14-03 (#53) | Infraestrutura ecológica: N-fixação + sombra → base produtiva resiliente (seca) |
| 2 | Shaded Milk / Soft Wool | Epic 14, US-14-03 (#53) | Premium animal sob copa: recompensa do manejo integrado |
| 3 | Greywater tanks | Epic 15 (Issue #17): US-15-01 (#54), US-15-02 (#55) | Autonomia hídrica passiva (upgrade de casa) |
| 4 | Weekly Fair (sábado, praça) | Epic 16 (Issue #18): US-16-01 (#56), US-16-02 (#57) | Circuito curto: +25% lucro + amizade, exige variedade (anti-monocultura) |
| 5 | CSA Box | Epic 17 (Issue #19): US-17-01 (#58), US-17-02 (#59) | Assinatura sazonal pré-paga, entrega semanal variada, risco de amizade |
| 6 | Collective Kitchen / Community Meals | Epic 18 (Issue #20): US-18-01 (#60), US-18-02 (#61), US-18-03 (#62) | JojaMart → cozinha coletiva; work party dominical + minigame culinário + buffs cidade |
| 7 | Guardian trophy / Food Sovereignty metric | Epic 19 (Issue #21): US-19-01 (#63), US-19-02 (#64) | "Vitória" final: journal metric + 5 critérios + recompensas (Pierre, Morris, Elena, troféu) |

### 1.2 Fundamento agroecológico (por que estes 7, nesta ordem)

`AGROECOLOGY_CONCEPTS.md` dá o lastro teórico — o briefing o ancora mecanicamente:

- **ICLF (§41):** "combining animal husbandry, crop cultivation, and tree maintenance". Na literatura real (Embrapa ILPF), leguminosas arbóreas (acácia-negra, gliricídia) fixam N via rizóbio, produzem sombra/forragem e quebram vento. O mod traduz isso em: aura de N (solo), imunidade à seca, crescimento acelerado, bem-estar animal.
- **Economia solidária (§55):** "short supply chains (local markets, cooperatives, CSA)". Fair + CSA são a tradução literal — e o design anti-monocultura (moradores recusam 100 Ancient Fruit Wines; querem 2 pães + 1 leite + 3 tomates + 1 geleia) é a mecânica que impede o late-game vanilla de colapsar num monocultivo de alto valor.
- **Food Sovereignty (§50):** "right of peoples to define their own agricultural and food policies". A métrica final do mod não é ouro — é autonomia: solo médio >80%, 10 heirlooms guardadas, cozinha operacional, 5 famílias no CSA. A recompensa (Pierre só vende heirloom, Morris sai da cidade) é a derrota sistêmica da Joja, não apenas um troféu.

### 1.3 Premissas de fundação (o que já existe e será reutilizado)

- **Soil Health 0–100** (`ModConstants.MinSoilHealth/MaxSoilHealth`), cache esparso, persistência por save — a métrica de soberania (>80% média) lê direto deste serviço.
- **Compostagem e bin** (`CompostItemId`, `QualifiedCompostItemId = "(O)LivingRoots.Compost"`) — padrão de registro de item + máquina a replicar para CSA Box, Greywater Tank, Fair stall.
- **Composition root único** (`ModEntry.cs`) + layering Domain → Services → Controllers + `InternalsVisibleTo("LivingRoots.Tests")` — todos os novos serviços seguem o mesmo molde (contrato em `Domain/Interfaces/`, impl em `Services/`).
- **Convenção anti-`.Result`/lock-em-I/O, logs sem mensagens cruas de exceção, `CheckForOverflowUnderflow=true`** — vale para os serviços de economia (Fair, CSA) que farão aritmética de ouro/amizade.

---

## 2. Asset 1 — ICLF Trees: Acacia / Gliricidia (US-14-01, US-14-02)

### 2.1 Conceito de jogo

Duas novas árvores de **integração lavoura-pecuária-floresta**: fixam nitrogênio (buff de solo em aura), permitem cultivo entre elas com **imunidade à seca + crescimento acelerado**, e dão sombra para o gado. São o "motor ecológico" do Year 4 — caras de plantar, lentas de crescer, permanentes de manter.

### 2.2 Referência técnica externa (verificada, 1.6+)

- **Asset canônico: `Data/WildTrees`.** Chave = string ID única (vanilla usa `1` oak, `2` maple, `3` pine, `8` mahogany, `13` mystic). Recomendado: `LivingRoots.Acacia`, `LivingRoots.Gliricidia`.
- **Campos relevantes:**
  - `SeedItemId` (qualified `(O)...`) + `SeedPlantable: true` — sem isso a árvore não é plantável e `SeedOnShakeChance` é ignorado.
  - `GrowthChance` (default 0.2/dia), `FertilizedGrowthChance` (default 1.0), `GrowsInWinter` (default false — decidir: gliricídia tropical **não** cresce no inverno; acácia talvez sim com flag).
  - `SeedSpreadChance` (default 0.15 — **reduzir para ~0.02–0.05**: ICLF não pode virar infestação), `SeedOnShakeChance`, `SeedOnChopChance` (default 0.75).
  - `TapItems` (lista com `DaysUntilReady` + `PreviousItemId`) — oportunidade: gliricídia produz forragem/muda via tapper? Não prometido no ROADMAP; registrar como extensão opcional.
  - `ShakeItems` / `ChopItems` (com `Chance`, `MinSize/MaxSize`, `Season`) — vagens/forragem como shake drop sazonal.
  - `PlantableLocationRules` (Allow/Deny por `Condition` game-state-query + `DeniedMessage`) — restringir à fazenda (Ground, não GardenPot).
  - `CustomFields` — usar para metadados LivingRoots (raio da aura N, taxa/dia) sem poluir o schema vanilla.
- **Spawn:** dois caminhos — (a) seed item via loja/carta (recomendado: Elena vende), (b) tile property `SpawnTree: wild <tree ID> <estágio>` na layer Paths (tile 34 do paths sheet) para plantio inicial em mapa.
- **Spritesheet:** `Textures[]` com suporte a `Season`/`Condition` (game state query, avaliada na criação, novo dia ou reload manual — **não** suporta hora-do-dia sem trigger manual de update pela SMAPI). Folhagem cheia = arquivo duplo/triplo de largura se usar `UseAlternateSpriteWhenSeedReady` / `GrowsMoss`.

### 2.3 Mecânicas LivingRoots a especificar (não existem no vanilla)

1. **Aura de nitrogênio:** a cada `DayStarted`, tiles aráveis num raio R (ex.: 5–8 tiles, constante em `Constants.cs`) ganham +X soil health (ex.: +2–4/dia) até um teto. Implementação: serviço `IIclfService` lendo posições de árvores via `GameLocation.terrainFeatures` (WildTree), cruzando com o sparse cache de soil health. Custo perf: varredura por fazenda/dia, não por tick.
2. **Imunidade à seca + crescimento acelerado entre árvores:** cultivos dentro da aura não perdem água / crescem Y% mais rápido. O vanilla não tem "drought" como sistema — definir: rega pulada por 1 dia não mata/atrasa? Ou imune ao `DirtDecayChance`? Decisão de spec.
3. **Sinergia animal (ponte para Asset 2):** pasto sob copa = gado "sombreado".

### 2.4 Riscos e decisões abertas

- **Balanceamento:** aura permanente + imune à seca pode trivializar aspersores. Contrabalanços: crescimento lento da árvore (GrowthChance baixa), custo alto da muda, raio limitado, teto de bônus.
- **Compatibilidade:** outras mods que editam `Data/WildTrees` — usar CP `EditData` com chave própria evita conflito; nunca sobrescrever o asset inteiro.
- **Multiplayer:** aura calculada host-side, replicada via modData; ver Deferred Intent D1 (multiplayer obrigatório pela Constituição v2).
- **Arte:** 2 spritesheets sazonais (4 estações × estágios) — maior custo de asset do End Game. Placeholder `.png` em `Assets/` segue o padrão atual (não wired) até o pipeline de conteúdo.

---

## 3. Asset 2 — Shaded Milk / Soft Wool (US-14-03)

### 3.1 Conceito de jogo

Animais pastando sob copa ICLF ficam mais felizes e produzem variantes premium: **"Shaded Milk"** e **"Soft Wool"** (qualidade superior). É a recompensa do manejo rotacionado (ponte com o Mid Game: Mobile Coop + pastejo rotacionado).

### 3.2 Referência técnica externa (verificada)

- **Animais: asset `Data/FarmAnimals`** (não `Data/Objects` direto). Cada entrada define produtos (`ProduceItemIds` / deluxe produce), construções válidas, dias até adulto. Produto em si é um `Data/Objects` com `(O)` ID próprio.
- **Objeto premium:** novo `Data/Objects` por produto:
  - `Name` (interno, único), `DisplayName`/`Description` (tokenizable, i18n-ready), `Type` (ex.: `Basic`), `Category` (atenção: categoria errada quebra Perfection/achievements — leite/wool têm categorias vanilla que devem ser replicadas), `Price` (venda; preço de compra vai em `Data/Shops`).
  - `Texture` + `SpriteIndex` (default `Maps/springobjects`; custom `TileSheets/Objects_2`-like ou spritesheet própria).
  - `ContextTags` (ex.: `milk_item`, `wool_item`, cores) — **crítico**: máquinas (Cheese Press, Loom), bundles e Special Orders filtram por tags. Shaded Milk deve carregar `milk_item` para virar queijo; Soft Wool `wool_item` para tear — ou deliberadamente não (decisão de design: premium processável vs. venda direta?).
- **Lógica de produção:** vanilla decide produto por felicidade/aleatoriedade no `DayUpdate` do animal. Opções de implementação:
  - (a) **C# SMAPI pura** (recomendado): interceptar `DayStarted`/`Animal.dayUpdate`, checar sombra (árvore ICLF num raio da posição do animal / barn com pasto sombreado), trocar produto por variante com probabilidade × felicidade.
  - (b) Framework **Extra Animal Configs (EAC)** como dependência — permite animais que ordenham E tosam, mas adiciona dependência externa; YAGNI sugere (a) primeiro.
- **Edibilidade/buffs:** se consumíveis, `Edibility` + `Buffs[]` (ver §7.2 — formato de buff em Objects referencia `Data/Buffs` por `BuffId`).

### 3.3 Decisões abertas

- Gatilho exato de "sob sombra": raio da copa? Dentro da fazenda apenas? Conta estabulação (barn) ou só pasto aberto?
- Probabilidade vs. determinismo: 100% sob copa com felicidade alta, ou chance escalonada? (Determinismo facilita teste; chance dá fantasia.)
- Preço premium: +50%? +100%? Deve ser inferior ao queijo/tecido processado para não matar o loop artesanal.
- Nomes/IDs: `LivingRoots.ShadedMilk` / `LivingRoots.SoftWool` (unqualified) → `(O)LivingRoots.ShadedMilk`.

---

## 4. Asset 3 — Greywater Recycling: tanques + upgrade de casa (US-15-01, US-15-02)

### 4.1 Conceito de jogo

Upgrade de casa que instala coleta de água de pia/chuveiro ("greywater") em tanques — **fonte passiva de irrigação** (como o Well vanilla, mas enche mais rápido). Autonomia hídrica temática: fecha o ciclo água junto com a Cisterna de chuva (Year 1).

### 4.2 Referência técnica externa (verificada)

- **Máquinas: asset `Data/Machines`.** Chave = **qualified** item ID (`(BC)...` para big-craftable). Modelo:
  - `OutputRules[]` com `Triggers[]` (`ItemPlacedInMachine`, `OutputCollected`, `MachinePutDown`, **`DayUpdate`** — este é o do greywater: gera água sem input), `OutputItem` (item spawn fields + `MinutesUntilReady`/`DaysUntilReady`), `RecalculateOnCollect`.
  - `PreventTimePass` (Outside/Inside/season/weather — ex.: pausar no inverno?), `OnlyCompleteOvernight`, `AllowFairyDust` (false para tanque), `ReadyTimeModifiers`.
  - `WobbleWhileWorking` / `ShowNextIndexWhileWorking|WhenReady` / `LightWhileWorking` para feedback visual; `InvalidItemMessage` para interação.
  - Para tanque puramente passivo (sem input): regra `DayUpdate` que produz `Watering Can refill`? **Atenção:** o vanilla não tem "item água" — Well/Sprinkler são lógicas hardcoded, não máquinas com output. Duas rotas:
    - (a) Tanque como **máquina que produz "Greywater Charge"** (item ou contador em modData) consumível para regar (rega via ferramenta custom ou recarga de regador por proximidade — requer C#).
    - (b) Tanque como **building/chest com `ItemConversions`** (`Data/Buildings`: `Chests[]` + `ItemConversions[]` com `SourceChest/DestinationChest`, `MaxDailyConversions`) — converte "nada" em cargas diárias.
  - Recomendação do briefing: (a) com `OutputMethod` C# custom (`MachineOutputDelegate`) se a lógica de carga for condicional (nível de upgrade da casa, nº de moradores, estação); `Data/Machines` puro se for taxa fixa/dia.
- **Big-craftable base: `Data/BigCraftables`** (item colocável, sprite, preço) + **receita `Data/CraftingRecipes`** (ingredientes, `Home`/`Field`, unlock `s <skill> <level>` / `default` / evento).
- **Upgrade de casa:** vanilla usa `Data/Buildings` (`BuildingToUpgrade`, `BuildCost`, `BuildMaterials`, `BuildDays`, `Builder: Robin`?) + mapas `Maps/` + `IndoorMap`. Greywater pode ser: upgrade Robin formal (novo nível) **ou** anexo comprado ( mailbox-like `ActionTiles`). Formal é mais compatível com `HasIndoorsName("FarmHouse")` e saves.
- **Mapa:** edição `.tmx` (recomendado sobre `.tbin`: texto/XML, diffável, suporta flip/rotação) via Content Patcher `EditMap` (recomendado; `Load` integral quebra compatibilidade). Prefixar tilesets custom com `z_` para não deslocar índices vanilla. Tanque exterior: `SourceRect`, `DrawLayers`, `CollisionMap`, `HumanDoor` n/a.

### 4.3 Mecânica a especificar

- Taxa de enchimento (ex.: +X cargas/dia, cap Y; "enche mais rápido que Well" = número explícito).
- Consumo: 1 carga = N tiles regados? Recarga automática de regador por proximidade? Chuva interage?
- Inverno: congela (pausa) ou continua (interior)? Coerência com `GrowsInWinter` das ICLF.
- Persistência: cargas em modData do building/save, com limites anti-DoS seguindo o padrão (`MaxTilesPerSave`-style para cargas).

---

## 5. Asset 4 — Short Supply Chains: Weekly Fair (US-16-01, US-16-02)

### 5.1 Conceito de jogo

Todo **sábado na praça**: vender direto aos moradores = **+25% lucro + amizade**. Contrapeso: moradores querem **variedade** (policultura); recusam pilhas homogêneas (o exemplo canônico: não 100 Ancient Fruit Wines; sim 2 pães + 1 leite + 3 tomates + 1 geleia). É o assassino mecânico da monocultura late-game.

### 5.2 Referência técnica externa (verificada)

- **Lojas: asset `Data/Shops`.** Mapa string→model: `Owners[]`, `Currency`, `PriceModifiers`, itens com `Condition` (game state queries: dia da semana, estação, amizade, flags LivingRoots), estoque rotativo/limitado. Fair = shop temporária `LivingRoots.WeeklyFair` aberta só sábado (Condition `DAY_OF_WEEK Saturday` + localização praça).
- **Vender (jogador→loja):** shops vanilla são compra; venda direta exige C# (menu custom de venda ou `Store` invertido) ou reutilizar **shipping bin** com modificador. Opções:
  - (a) Menu de venda custom SMAPI (controle total: variedade, +25%, amizade por morador) — recomendado.
  - (b) Clones de `Shipping Bin` (`Data/Buildings` pode instanciar) com taxa — menos controle sobre variedade.
- **Festivais vs. evento semanal:** o vanilla tem `Data/Festivals` (dias fixos, mapas especiais, `awardFestivalPrize`) — **não** usar festival real todo sábado (colidiria com Luau/Dance/Fair anual e com `FestivalDay` preconditions). Implementar como **evento recorrente de localização**: NPCs agendados na praça (schedule patch), barracas via `EditMap` temporário ou `temporarySprite`/`addProp`, música/ Accord via `Data/Locations` music override. Precondição `Year >= 4`? ROADMAP diz Year 4+ como fase, mas travar conteúdo por ano é decisão de spec (recomendado: desbloquear por progresso — cozinha/CSA — não por relógio).
- **Amizade:** `friendship <npc> <amount>` (250 pts = 1 coração) via trigger actions/event commands ou C# `Game1.player.changeFriendship`. Cap semanal para evitar farm infinito.
- **Anti-monocultura (o coração do design):** cotas por categoria por morador/semana (ex.: cada NPC compra até N de cada categoria; excedente recusado ou a preço base sem bônus). Categorias = `Category` + `ContextTags` dos `Data/Objects`. Exige tabela de demanda (determinística por seed semanal? rotativa por estação?) — especificar.

### 5.3 Decisões abertas

- Onde exatamente (praça toda ou sub-área)? Conflito com festivais anuais que usam a praça (Fair de outono!) — Fair semanal é suspensa em dia de festival (`!FestivalDay`).
- UI de venda: diálogo por morador (lento, charmoso) vs. menu único (rápido, frio)? Híbrido: banca central + diálogos de entrega para CSA? (ver §6).
- +25% sobre qual base (preço de venda? preço Pierre?). Definir fórmula e arredondamento (overflow-checked).
- Multiplayer: ouro/amizade por jogador; banca compartilhada?

---

## 6. Asset 5 — CSA Box: assinatura e entrega semanal (US-17-01, US-17-02)

### 6.1 Conceito de jogo

Caixa construível na fazenda. No **início de cada estação**, moradores (Evelyn, Gus, Clint, …) **assinam**: pagam **adiantado** (caixa sazonal antecipado = alívio do early-season). Obrigação: **cesta variada toda semana**. Falhar = perde amizade. Sucesso = grande amizade (+ "solo metafórico" — ler como pequeno buff real de soil health? especificar).

### 6.2 Referência técnica externa (verificada)

- **Construção:** `Data/Buildings` (CSA Box como building pequeno ou big-craftable? ROADMAP diz "can be built on the farm" — building com `Size`, `CollisionMap`, `Chests[]`/`ItemConversions[]` para depósito da cesta é o mais expressivo; big-craftable + máquina é mais barato). **Recomendação:** building com `DisplayTile` + bubble "item ready", `IndoorItems` n/a, `ActionTiles` para abrir UI.
- **Assinatura/quest:** dois veículos vanilla:
  - (a) **`Data/SpecialOrders`** (board-based, late-game, flexível): `Duration: Week/TwoWeeks/Month`, `Repeatable: True`, objetivos `Collect`/`Deliver`/`Donate` (com `DropBox` + `DropBoxGameLocation` — a CSA Box como dropbox!), `RequiredTags` (estação, `mail_<ID>`), `RandomizedElements` (cesta variada por estação!), recompensas `Money`/`Friendship`/`Mail`. **É o molde ideal da cesta semanal.**
  - (b) `Data/Quests` clássicas (journal + `addQuest`/`removeQuest` em eventos) — mais simples, menos expressiva (sem dropbox, sem repetição sazonal nativa).
  - Recomendação: SpecialOrders com `OrderType` custom? (vanilla: blank/town board, `Qi`, `DesertFestivalMarlon`; mods podem adicionar boards que filtram por tipo próprio — uma "CSA board" na caixa é elegante).
- **Pagamento adiantado:** `Money` reward na aceitação + `Donate` semanal? Ou evento de assinatura (`Data/Events/<Location>` com preconditions `Season` + `DayOfMonth 1-7` + `Year`?) que cobra/entrega via `action AddMoney` / `addItem`. Mail sazonal (`Data/Mail` + `LocalMail`/`HostMail` preconditions) convidando a assinar.
- **Variedade da cesta:** `RandomizedElements` por estação (PICK_ITEM por tags: vegetal/fruta/laticínio/artesanal) + `AcceptedContextTags` com sintaxe `,` (E) / `/` (OU) / `!` (NÃO) — ex.: `!forage_item, category_vegetable/category_fruit, quality_gold` (padrão Pierre QoS).
- **Falha:** expiração de SpecialOrder (`Duration`) → penalidade de amizade via `Mail` reward que dispara evento/diálogo? Vanilla não tem "punishment reward" — requer C# (`DayEnded` checa ordens expiradas → `changeFriendship` negativo + carta de decepção). Cap de perda para não soft-lockar corações.

### 6.3 Decisões abertas

- Nº de assinantes (meta soberania = 5 famílias) vs. lista de candidatos (quantos oferecem? todos aceitam? rotativo?).
- Valor adiantado (fixo por estação? escala com nº de assinantes? por tamanho da cesta?).
- "Solo metafórico": flavor text ou buff real (+soil health global semanal em sucesso? ex.: +1 em todas as tiles? — cuidado com inflação; recomendo buff pequeno e auditável).
- Interação Fair×CSA: cesta CSA conta para cotas da Fair? (Evitar double-dip: entrega CSA não gera +25% da Fair.)

---

## 7. Asset 6 — Collective Kitchen: JojaMart → cozinha + work party + Community Meals (US-18-01, US-18-02, US-18-03)

### 7.1 Conceito de jogo

Se o jogador seguiu a rota Joja, o JojaMart abandonado **não** vira cinema — vira a **"Living Roots Collective Kitchen"** (projeto de construção da cidade). **Domingo à noite: "Kitchen Work Party"** — jogador + moradores trazem ingredientes; **minigame culinário** (espírito do Luau); resultado: **"Community Meals"** (buffs poderosos para todos) + progresso de soberania.

### 7.2 Referência técnica externa (verificada)

- **JojaMart→cinema no vanilla:** completar o Community Center (ou os community upgrades Joja) + rainy-day cutscene do "Missing Bundle" no JojaMart abandonado → vira **Movie Theater** (`Movie_Theater`, aberto 9h–21h, 1 filme/semana com convidado). O mod **diverge** aqui: rota LivingRoots (comunitária) substitui o cinema pela cozinha. Decisão crítica de spec: a divergência é condicional (só se rota X + cozinha construída?) ou total (mod sempre substitui)? E o que acontece com quem já tem cinema (save existente)? Migração/gating por `GameStateQuery` (bundle completo? `CommunityCenterOrWarehouseDone`? mail flags?) é obrigatória.
- **Projeto de construção comunitária:** modelar como sequência de `SpecialOrders` (doações `Donate` de madeira/pedra/ouro via dropbox no terreno) + `Mail` progress + evento(s) de inauguração (`Data/Events/JojaMart` com preconditions `HostMail`/`SawEvent`/clima). Alternativa: `Data/Buildings` upgrade direto pago a Robin — menos "coletivo". Recomendação: SpecialOrders de doação (fantasia coletiva) com cap diário.
- **Work party dominical:** evento semanal (`Time 1700 2100`, `DayOfWeek Sunday`, `!FestivalDay`, `!Season`? — semanal o ano todo). Estrutura `Data/Events/<Town|JojaMart>`: `music`, câmera, posições iniciais, `speak`/`move`/`faceDirection`/`emote`/`pause`, `question fork` para o minigame, `farmerEat`, `addItem`, `friendship`, `mail`, `setSkipActions` (recompensa parcial se pular — **obrigatório** para evento repetível!). `skippable` + `broadcastEvent` (multiplayer).
- **Minigame culinário:** vanilla oferece `question`/`quickQuestion` (scripts de resposta com `(break)`) — suficiente para quiz de ingredientes? Para algo "tipo Luau" (qualidade da contribuição), considerar menu C# custom (barra de tempo, combinação) — especificar complexidade (YAGNI: começar com contribuição-por-qualidade + quiz, evoluir se playtest pedir).
- **Community Meals (buffs poderosos):** dois níveis:
  - **Receita:** `Data/CookingRecipes` (ingredientes = pares `itemID qty`, yield, unlock: `f <NPC> <hearts>` carta / `s <skill> <level>` / evento). Ingredientes contribuídos no party.
  - **Item:** `Data/Objects` (Type `Cooking`, **Category `-7` obrigatório** para contar em Gourmet Chef/Perfection), `Edibility` (energia = ×2.5, vida = ×1.125), `IsDrink` false, `Price`.
  - **Buff:** `Buffs[]` no Objects referenciando **`Data/Buffs`** por `BuffId` (nome, duração — `-2` = resto do dia —, ícone 16×16, `CustomAttributes`: `FarmingLevel/FishingLevel/...`, `Speed`, `MaxStamina`, `MagneticRadius`, `Defense`, etc., `GlowColor`, `IsDebuff`). Override de duração/ícone por refeição. "Buffs para todos na cidade": buff aplica ao(s) jogador(es) que comeram; NPCs recebem `friendship`/diálogo, não buff mecânico (eles não têm stats de buff) — especificar para não prometer impossível.
  - Duração em **minutos in-game**; notar limite 1 comida + 1 bebida simultâneas (design: Community Meal como comida "dominante" — trade-off interessante vs. buffs de mineração/pesca).
- **Localização/mapa:** interior JojaMart reaproveitado via `EditMap` (cozinha comunitária: bancadas, fogões como máquinas `Data/Machines` decorativas/funcionais, `Action` tiles para cozinhar, `ChairTiles` para banquete?). `Data/Locations` entry se virar location própria; `Music`, `AmbientLight`, warps preservados.

### 7.3 Decisões abertas

- Gating rota: e se o jogador fez rota Community Center vanilla (cinema já existe)? Cozinha é alternativa, adicional ou substituta? (Maior risco de compat/save do End Game — spec dedicada só para US-18-01.)
- Custo do projeto coletivo (lista de doações) e ritmo (1 semana? 1 estação?).
- Contribuição semanal: quantos ingredientes, quais tags, qualidade mínima? Falhar em trazer = sem meal ou meal fraca?
- Poder do buff: "poderoso" sem quebrar mina/caverna (ex.: +2 skill por resto do dia é forte; +3 quebrado). Tabela explícita por tier de contribuição.

---

## 8. Asset 7 — Food Sovereignty: métrica + vitória + recompensas (US-19-01, US-19-02)

### 8.1 Conceito de jogo

Nova **métrica no journal**. Condições de vitória (todas):

1. Fazenda com **>80% soil health médio**;
2. **≥10 tipos de Heirloom Seeds** guardadas;
3. **Collective Kitchen operacional**;
4. **CSA com ≥5 famílias assinantes**.

Recompensas: Pierre para de vender "Joja Hybrid" e só vende heirloom local; **Morris sai da cidade permanentemente**; Elena entrega o troféu **"Guardian of Living Roots"**; vale autossuficiente.

### 8.2 Referência técnica externa (verificada)

- **Journal/UI:** vanilla tem quest log (`addQuest`/`removeQuest`, `Data/Quests`) e **Special Orders board** (pedidos ativos com progresso). "Nova métrica no journal" tem 3 rotas:
  - (a) **Quest persistente** com texto de progresso atualizado por C# (mais nativa; limitada a texto).
  - (b) **Menu custom SMAPI** (StardewUI/IClickableMenu) com barra de soberania + 5 critérios (mais rica; custo UI + i18n + controller/keyboard).
  - (c) **Special Order permanente** como carrier de progresso (hack esperto, mas semântica errada).
  - Recomendação: (a) agora (journal quest "Food Sovereignty" com contadores), (b) como evolução se playtest pedir visual. **Nenhuma** rota é "Data-only" — todas exigem C# para recalcular (>80% média = agregação do sparse cache; heirlooms = scan de inventário/baús; cozinha = flag; CSA = contagem de assinaturas ativas).
- **Game state queries + trigger actions** (a cola): `GameStateQuery` (`Season`, `Year`, `Weather`, `PLAYER_SPECIAL_ORDER_RULE_ACTIVE`, mail flags, amizade, etc.) gating cada critério; `Trigger Actions` (`AddMoney`, `AddItem`, `AddCraftingRecipe`, `AddCookingRecipe`, `ResetEvent`, etc.) executando recompensas. Pré-condições de evento (`Time`, `DayOfWeek`, `Friendship`, `SawEvent`, `HostMail`, `Year`, `!FestivalDay`) para a cerimônia do troféu.
- **Pierre/Morris (o golpe político):**
  - Pierre stock: `Data/Shops` (loja `Pierre`) com `Condition` por item — híbridas Joja com `Condition: !mail_livingroots_sovereignty` (somem após vitória), heirlooms com condição inversa (aparecem). Alternativa C# (`ShopMenu` patch) se precisar de lógica além de queries.
  - Morris sair: NPC schedule/retire — vanilla não tem "NPC sai para sempre" data-only; requer C# (remover schedule, trocar diálogos, evento de despedida `Data/Events`, `mail` flag irreversível). Edge: casamento? (não é casável — ok.) Multiplayer: flag de host.
- **Troféu:** `Data/Objects` (Type `Basic`/`Quest`? — `Quest` não pode ser giftable/trashed por padrão; troféu de exibição sugere `Basic` não-vendável ou furniture). Entrega via evento Elena (`addItem (O)LivingRoots.GuardianTrophy`, `friendship Elena`, `end dialogue`).
- **"Vale autossuficiente" (epílogo):** especificar o que muda de fato (diálogos town-wide via `addConversationTopic`, preços Pierre, estoque Fair, carta final?) — sem isso a vitória é só um popup.

### 8.3 Definições a travar em spec (US-19-01)

- Média de soil health: sobre quais tiles (aráveis? todos da fazenda? inclui estufa/ilhas?) e em que momento (cálculo contínuo vs. snapshot no `DayStarted`?).
- "10 tipos de Heirloom Seeds": tipos distintos (IDs) vs. stacks; sementes plantadas contam? Baús contam? (Recomendado: IDs distintos em inventário+baús da fazenda.)
- "Cozinha operacional": flag de inauguração (não basta construir — precisa do evento?).
- "5 famílias": famílias (counts por household?) vs. indivíduos; rotatividade (assinante que cancela conta? janela: simultâneos?).
- Reversibilidade: pode perder a soberania (solo cai <80%)? Ou vitória é permanente (one-way latch)? Recomendado: latch permanente com epílogo, para não punir experimentação pós-vitória.

---

## 9. Dependências, sequência e fatiamento sugerido

```
Year1 base (solo+compost+mulch+heirloom+cisterna) ─┐
Mid (rotação+adubo verde+animais+abelhas+farmácia+bio) ─┤
                                                    ▼
  ICLF trees ──► Shaded Milk/Wool ──► Greywater ──► Fair ──► CSA ──► Kitchen ──► Sovereignty
   (base)        (produto)            (paralelo*)    (loja)    (orders) (evento)   (métrica)
```

- `*` Greywater é largamente independente (casa+máquina) — pode paralelizar com ICLF após a fundação `Data/Objects`+`Data/Machines` estar dominada.
- Fair antes de CSA (a lógica de variedade/cotas da Fair é reutilizada na cesta CSA).
- Kitchen é o bloco mais arriscado (mapa + evento semanal + minigame + buffs + gating Joja/cinema) — fatiar em 3 specs (US-18-01, -02, -03 separadas), não uma.
- Sovereignty por último (consome flags de todos os anteriores).
- Cada spec deve declarar sua estratégia de teste: unitário (cálculos puros: aura, cotas, cesta, média) + integração SMAPI (registro de assets, evento dispara, order completa) + manual in-game (checklist PR template — o mod exige teste manual em jogo).

## 10. Riscos transversais (todos os 7 assets)

1. **Compatibilidade de assets:** nunca `Load` integral de vanilla; sempre `EditData`/`EditMap` por chave. Chaves com prefixo `LivingRoots.` em tudo (árvores, itens, máquinas, lojas, ordens, eventos, mails, buffs).
2. **Performance:** auras/varreduras só em `DayStarted`/`DayEnded`, nunca por tick; teto de tiles (`MaxTilesPerSave`-style) para loops de solo; `MaxRenderTimeMs`-style budget se houver overlay (ex.: mostrar aura ICLF no hover da enxada — reutilizar `Services/Visualization/`).
3. **Saves existentes:** gating por `Year`/`SawEvent`/mail evita aplicar End Game retroativamente de forma quebrada; cinema-vs-cozinha precisa de migração explícita.
4. **Multiplayer (D1, Constituição v2 — obrigatório):** ouro, amizade, assinatura CSA, contribuição da cozinha e métrica são por-jogador vs. por-time — decidir por asset; eventos com `broadcastEvent`; aura host-side.
5. **i18n:** todo `DisplayName`/`Description`/carta/diálogo como tokenizable string desde o dia 1 (`i18n/*.json` hoje é placeholder — End Game é o momento de wirar de verdade).
6. **Anti-cheat/overflow:** `CheckForOverflowUnderflow=true` global; aritmética de +25%, pagamentos adiantados e contadores de cesta com tipos largos e clamps em `Constants.cs` (seguir o padrão: nada de número mágico fora de `ModConstants`).
7. **Logs sem PII/segredo:** só `ex.GetType().FullName + HResult` em Trace (regra de segurança do repo) — vale para falhas de economia/evento.
8. **Escopo vs. YAGNI:** cada asset tem uma extensão tentadora (tapper de gliricídia, menu de loja 3D, minigame complexo, UI custom de soberania) — o briefing as registra como **extensões explícitas não-prometidas**, a cortar no `speckit.clarify` se estourarem a spec.

## 11. Estimativa relativa (t-shirt, por Epic, para planejar specs)

| Epic | Tamanho | Motivo |
|---|---|---|
| 14 ICLF + premium (US-51–53) | G (grande) | 2 árvores (arte 4 estações) + aura + drought + produto animal C# |
| 15 Greywater (US-54–55) | M | 1 máquina + 1 upgrade + persistência de cargas |
| 16 Fair (US-56–57) | G | menu de venda + cotas/variedade + schedules sábado + amizade |
| 17 CSA (US-58–59) | M–G | building/dropbox + SpecialOrders sazonais + adiantado + penalidade |
| 18 Kitchen (US-60–62) | GG (maior) | gating cinema + projeto coletivo + evento semanal + minigame + refeições/buffs + mapa |
| 19 Sovereignty (US-63–64) | M | agregações + journal + cerimônia + Pierre/Morris/Elena |

## 12. Fontes externas (consultadas, 1.6+)

- `Modding:Wild_trees` — `Data/WildTrees` (SeedItemId, GrowthChance, Tap/Shake/ChopItems, PlantableLocationRules, SpawnTree tile prop).
- `Modding:Objects` — `Data/Objects` (Name/DisplayName/Type/Category/Price/Texture/SpriteIndex/Edibility/IsDrink/Buffs/ContextTags).
- `Modding:Machines` — `Data/Machines` (OutputRules/Triggers incl. DayUpdate, OutputMethod C#, PreventTimePass, WorkingEffects).
- `Modding:Buildings` — `Data/Buildings` (Builder/BuildCost/BuildMaterials/BuildDays, Upgrades, Chests+ItemConversions, ActionTiles, IndoorMap).
- `Modding:Maps` — `.tmx` > `.tbin`, `EditMap` > `Load`, prefixo `z_`, layers Back/Buildings/Paths/Front/AlwaysFront, `Data/Locations` para location nova.
- `Modding:Recipe_data` — `Data/CookingRecipes` (ingredientes/yield/unlock `f`/`s`/`default`) + `Data/CraftingRecipes`.
- `Modding:Event_data` — `Data/Events/<Local>` (preconditions com `!`, comandos speak/move/question fork/farmerEat/addItem/friendship/mail/setSkipActions/skippable/broadcastEvent).
- `Modding:Special_orders` — `Data/SpecialOrders` (Duration/Repeatable/RequiredTags/OrderType/Objectives Collect-Deliver-Donate-Ship/Rewards Money-Friendship-Mail-Object/RandomizedElements/ContextTags `,!/`).
- `Modding:Shops`, `Modding:Buffs` + Tutorial Adding Buffs (`Data/Buffs`: duração em minutos, `-2` resto do dia, CustomAttributes), `Modding:Animal_data` (`Data/FarmAnimals`), Movie Theater (pós-Missing Bundle, 9h–21h), Special Order boards custom por `OrderType`.

---

*Briefing gerado por pesquisa interna (ROADMAP, EPICS_USER_STORIES, AGROECOLOGY_CONCEPTS, Constants) + wiki de modding 1.6+ (WildTrees/Objects/Machines/Buildings/Maps/Recipe/Event/SpecialOrders/Shops/Buffs/Animal). Próximo passo: `speckit.specify` por Epic, começando por ICLF (Epic 14).*
