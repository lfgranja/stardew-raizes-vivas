# Briefing — Early Game Year 1 (ROADMAP:30-40)

> Status: briefing (não implementado). Escopo: 6 assets futuros — Elena NPC, Welcome Kit, Mulch, Heirloom Seeds, Joja Fertilizer, Small Cistern.
> Convenção de IDs: `LivingRoots.<Nome>` (objeto/semente), `(O)LivingRoots.<Nome>` qualificado 1.6, `(BC)LivingRoots.<Nome>` para big-craftable/máquina. Segue o padrão já usado em `LivingRoots/Constants.cs:63-66` (`CompostItemId`, `QualifiedCompostItemId`, `MachineCompostingBinId`).

## Referências internas (verificadas no repo)

| Fonte | O que diz |
|---|---|
| `ROADMAP.md:30-40` | Early Game = sobrevivência + aprendizado; lista os 6 itens deste briefing (Elena mentora, Welcome Kit no lugar das parsnips, Composter, Mulch, Three Sisters, heirlooms vs. híbridas Pierre, Small Cistern chuva) |
| `LivingRoots/docs/EPICS_USER_STORIES.md:5-50` | Epics 1–7 = Year 1: Epic 4 Mulch (#30/#31), Epic 5 Elena (#32/#33), Epic 6 Heirloom (#34/#35), Epic 7 Cistern (#36/#37); Epic 3 = Three Sisters (#28/#29) |
| `LivingRoots/docs/AGROECOLOGY_CONCEPTS.md:32-40` | Base conceitual: policultura/companion, heirloom vs. híbridas, green manure, compostagem, cobertura morta/mulch anti-erosão; Primavesi (vida do solo) como âncora |
| `LivingRoots/Constants.cs:53` | `MulchColor = #92400E (146,64,14)` — cor canônica do mulch no overlay; reutilizar para tile tint/partícula |
| `LivingRoots/Domain/Visualization/*` + `Domain/AGENTS.md` | `HealthCategory`: Poor [0,34) / Moderate [34,67) / Healthy [67,100]; opacidade default 0.5, pattern mín. 0.7; DTOs sem SMAPI/XNA |
| `.specify/assessments/next-feature-80-20/{intake,research,decision}.md` | D3 = sem superfície jogável (sem `Data/Objects` handler na época; `Data/Machines` vazio); decisão = Option A "Make It Playable First" + rider C (corrigir specs/README); Principle VI = nada novo single-player-only, host-autoritativo |
| `LivingRoots/ModEntry.cs:114-177` | Padrão de registro via `AssetRequested`: `Data/CraftingRecipes` (string `ingrediente qtd/.../aba/item qtd/...`), `Data/Objects` (`ObjectData`), `Data/Machines` (chave `(BC)id` + `OutputRules`) |
| `LivingRoots/i18n/en.json` + `default.json` | Chaves existentes: `compost.name`, `composting-bin.*`, `floating-text`, `message.compost.*`; padrão `snake.case` com prefixo de asset |
| `LivingRoots/Assets/` | Precedente: `compost.png` (item), `composting_bin_{empty,processing,ready}.png`, `Audio/composting_bin_{processing,ready}.ogg` — copiados ao build, carregamento futuro via `ModContent.Load` / `Texture` em data JSON |

## Referências externas (wiki 1.6, verificadas nesta sessão)

- **NPC**: `Modding:NPC_data` — `Data/Characters` (DisplayName tokenizável, Gender/Age/Manner/SocialAnxiety/Optimism, BirthSeason/Day, HomeRegion=Town p/ introductions quest + Winter Star + Luau, `Home:[{Id,Location,Tile{X,Y},Direction}]`); sprite overworld `Characters/<Nome>` **16×32 por frame** (16 primeiros = movimento 4/dir; 40–47 dança Flower Dance; kiss frame default 28); retrato `Portraits/<Nome>` **64×64 por emoção**; `Data/NPCGiftTastes` (5 pares diálogo+IDs: Love/Like/Dislike/Hate/Neutral); schedule em `Characters/schedules/<ID>` (`HHMM Local X Y facing[/...]`); falas em `Characters/Dialogue/<ID>` + `Strings/schedules/<ID>`; sono via `Data/animationDescriptions` (`<nome>_sleep`).
- **Objetos**: `Modding:Objects` — `Data/Objects` chave = ID não-qualificado; campos `Name, DisplayName, Description (tokenizáveis), Type (Basic/Crafting/Seeds/...), Category, Price, Texture (default Maps/springobjects), SpriteIndex`; sprite **16×16**; `ContextTags`, `Edibility`, `Buffs`.
- **Culturas**: `Modding:Crop_data` — `Data/Crops` chave = ID da **semente**; `Seasons[], DaysInPhase[], RegrowDays, IsRaised, HarvestItemId, HarvestMethod (Grab/Scythe), HarvestMin/MaxStack, ExtraHarvestChance, Texture (default TileSheets\crops), SpriteIndex (1 linha por cultura), CountForMonoculture/CountForPolyculture`.
- **Receitas**: `Modding:Recipe_data` — `Data/CraftingRecipes` string `"<ingredienteId> <qtd>.../<aba>/<itemId> <qtd>/<falso|verdadeiro>/<condição>/<nome>"`; exemplo vanilla: `"388 45 390 45/Home/16/true/s Farming 6/"`.
- **Big-craftable**: `Modding:Big_craftables` — `Data/BigCraftables`, tipo `(BC)`, `Fragility, CanBePlacedIndoors/Outdoors, IsLamp, Texture (default TileSheets/Craftables), SpriteIndex`; sprite **16×32** (2 tiles de altura).
- **Máquinas**: `Modding:Machines` — `Data/Machines` chave = ID **qualificado** (`(BC)id`); `OutputRules[{Id, Triggers[{Trigger: ItemPlacedInMachine|OutputCollected|MachinePutDown|DayUpdate, RequiredItemId/RequiredTags/RequiredCount, Condition}], OutputItem[{ItemId,...}], MinutesUntilReady/DaysUntilReady, UseFirstValidOutput}]`; `PreventTimePass: [Sun|Rain|...]`, `OnlyCompleteOvernight`, `WorkingEffects/LoadEffects (Sounds/Frames/ShakeDuration)`, `InvalidItemMessage`, `ExperienceGainOnHarvest`, `CustomFields`.
- **Lojas**: `Modding:Shops` — `Data/Shops` (`SeedShop` = Pierre, `Joja` = Joja Mart); item `{Id, ItemId:"(O)...", Price, AvailableStock, AvailableStockLimit: Global|Player|None, Condition: "SEASON ..."}`; atalho `TargetField: [ShopId, Items]` p/ append.
- **Content Patcher**: `Modding:Content_Patcher` — `Load` (imagem/diálogo/schedule novo) vs `EditData`/`EditImage` (patch); exemplo canônico Dobson no `Modding:NPC_data#Full_NPC`.
- **Áudio**: cues vanilla via nome (`axe` já usado em `CompostCraftingSound`, `dwop` = abrir loja, `purchaseClick`, `wateringCan`, `compost` inexistente → usar custom `.ogg` como já feito p/ bin).
- **i18n**: `DisplayName`/`Description`/diálogos aceitam `{{i18n:chave}}` (padrão Dobson: `"DisplayName": "Dobson" // this can use {{i18n:}}`).

## Decisões transversais (valem p/ os 6 capítulos)

1. **Registro em C# via `OnAssetRequested`** (padrão `ModEntry.cs`), espelhável em Content Patcher depois. D3/assessment: primeiro task de spec deve ser spike CP-vs-C# — briefing assume C# como default, com bloco CP pronto.
2. **IDs prefixados `LivingRoots.`** (1.6 exige string IDs únicas; qualificar com `(O)`/`(BC)` onde o asset pede qualificado).
3. **Nada single-player-only** (Principle VI): estado = propriedade da fazenda (farm-keyed, `SaveIdProvider`), writes host-autoritativos, `Context.IsMainPlayer` guards onde houver mutação diária; `AvailableStockLimit: Player` nas lojas p/ não punir farmhands.
4. **`ModConstants.cs` (nome do arquivo é `Constants.cs`, classe `ModConstants`)** recebe toda constante nova — sem magic numbers; sons seguem `CompostCraftingSound = "axe"`.
5. **i18n desde o dia 1**: `en.json` + `default.json` (PT-BR pode vir depois; chaves idênticas).
6. **Overlay**: mulch/cistern/heirloom devem alimentar `IVisualizationService` (tile tint `MulchColor` p/ solo coberto; ícone gota p/ cistern; borda verde p/ heirloom) em vez de hardcodar em controller.

---

## Cap. 1 — Elena NPC (Epic 5 · US-05-01 #32 / US-05-02 #33)

**Papel no jogo** (ROADMAP:34): agrônoma + ativista, mentora Year 1. Dá Welcome Kit (Cap. 2), vende heirlooms (Cap. 4), ensina mulch/policultura/green manure via quests e diálogos. `HomeRegion: Town` para entrar na introductions quest, Winter Star e Luau.

### Sprites
| Asset | Spec | Arquivo sugerido |
|---|---|---|
| Overworld | `Characters/LivingRoots_Elena`, **16×32/frame**, mín. 16 frames movimento (4/dir: down/up/left/right); + frames 40–47 dança (só se `FlowerDanceCanDance=true` — sugerido `false` Y1); kiss frame omitido (não-romanceável Y1) | `Assets/NPC/elena_sprites.png` |
| Retrato | `Portraits/LivingRoots_Elena`, **64×64/emoção**, mín. 6 (neutro, feliz `$h`, triste `$s`, bravo `$a`, surpreso, especial festa) | `Assets/NPC/elena_portraits.png` |
| Sono | `Data/animationDescriptions: "elena_sleep"` | — (data only) |

Paleta: camisa verde-caqui agroecologia, calça marrom, lenço terracota; legível em 16×32 (evitar detalhe <2px).

### Data JSON (C# `AssetRequested` — espelho CP entre parênteses)
```csharp
// Data/Characters — Load: Characters/LivingRoots_Elena, Portraits/LivingRoots_Elena,
//   Characters/Dialogue/LivingRoots_Elena, Characters/schedules/LivingRoots_Elena
data["LivingRoots_Elena"] = new CharacterData {
  DisplayName = "{{i18n:elena.name}}", Gender = "Female", Age = "Adult",
  Manner = "Polite", SocialAnxiety = "Outgoing", Optimism = "Positive",
  BirthSeason = "spring", BirthDay = 18, HomeRegion = "Town",
  CanBeRomanced = false, // Y1; revisitar pós-Year-3
  Home = new[] { new HomeEntry { Id = "Default", Location = "SeedShop", Tile = (X:2,Y:8), Direction = "down" } },
  // TODO specs: quarto próprio (SeedShop backroom) vs. casa nova — placeholder SeedShop aceitável Y1
};
// Data/NPCGiftTastes — formato 5 pares: Love/Like/Dislike/Hate/Neutral
data["LivingRoots_Elena"] =
  "{{i18n:elena.gift.love}}/LivingRoots_Compost LivingRoots_HeirloomParsnip/.../" +
  "{{i18n:elena.gift.like}}/.../.../" +
  "{{i18n:elena.gift.dislike}}/.../.../" +
  "{{i18n:elena.gift.hate}}/LivingRoots_JojaFertilizer/.../" +  // odeia fertilizante Joja (lore!)
  "{{i18n:elena.gift.neutral}}/...";
// Data/animationDescriptions
data["elena_sleep"] = "50/50/50";
```
Schedule Y1 mínimo (`Characters/schedules/LivingRoots_Elena`):
```json
{ "spring": "800 SeedShop 2 8 0/1200 Town 47 87 0/1700 SeedShop 2 8 2",
  "summer": "800 SeedShop 2 8 0/1200 Beach 20 10 0/1700 SeedShop 2 8 2",
  "fall":   "800 SeedShop 2 8 0/1200 Town 47 87 0/1700 SeedShop 2 8 2",
  "winter": "900 SeedShop 2 8 0/1500 Saloon 10 20 2/2000 SeedShop 2 8 2",
  "rain":   "800 SeedShop 2 8 0/1800 SeedShop 2 8 2" }
```
Diálogo (`Characters/Dialogue/LivingRoots_Elena`): `Introduction`, `Mon`–`Sun` rotativo ensinando 1 conceito/semana (composto → mulch → policultura → heirloom → cisterna), `AcceptGift_*`, `FlowerDance_Decline` (se não dança). Quest-gating via `Data/Quests` (entregar 5 compost → receita mulch; plantar Three Sisters → heirloom discount).

### Sons
Nenhum custom Y1 — reutilizar cues vanilla de diálogo (`talkToNPC`). Futuro: greeting `.ogg` próprio em `Assets/Audio/elena_greeting.ogg`.

### i18n (adicionar a `en.json` + `default.json`)
```json
{ "elena.name": "Elena", "elena.title": "Agroecologist",
  "elena.introduction": "You must be the new farmer! I'm Elena — I study living soil...",
  "elena.dialogue.monday": "Bare tilled soil loses life every day. Cover it with mulch!#$e#",
  "elena.gift.love": "For my compost trials? Wonderful!",
  "elena.gift.hate": "Joja fertilizer? Keep that poison away from my plots.",
  "elena.shop.title": "Have a look at what really feeds the soil.",
  "message.elena.welcome-kit": "Elena gave you an Agroecological Welcome Kit!" }
```

### Loja
Elena vende de balcão próprio: nova shop `LivingRoots_ElenaShop`, aberta via `Utility.TryOpenShopMenu` em diálogo ou tile `Action OpenShop` no SeedShop. `Owners:[{Name:"LivingRoots_Elena", Dialogues:[...]}]`. Itens Y1: heirlooms (Cap. 4), receita mulch (Cap. 3), small cistern (Cap. 6). Ver Cap. 4 p/ condicionar desconto a amizade (`Condition: "PLAYER_HEARTS LivingRoots_Elena 4"`).

---

## Cap. 2 — Agroecological Welcome Kit (ROADMAP:35)

**Papel**: substitui as 15 parsnip seeds iniciais. Entregue pela Elena via evento de intro (dia 1–2, mail + cutscene curta) — contém: 1 Composter (já existe `CompostingBinItemId`), 5 heirloom parsnip seeds (Cap. 4), 3 mulch (Cap. 3), 1 carta tutorial.

### Sprites
Nenhum sprite novo obrigatório Y1: kit = `Data/Mail` + itens existentes. Opcional: `Assets/m welcome_kit.png` **16×16** como token visual do mail (reutilizável como `Data/Objects LivingRoots_WelcomeKit` Type=Quest, Price=0, unobtainable fora do evento).

### Data JSON
```csharp
// Data/Mail — carta da Elena com attachments via event script
data["LivingRoots_WelcomeKit"] =
  "{{i18n:mail.welcome-kit.text}}";
// Evento: dar via `addItem (O)LivingRoots_CompostingBin 1` etc. — usar trigger actions
// Data/Quests (tutorial em 3 passos)
//   1. "Craft or place your Composter"  2. "Apply mulch to 4 tiles"  3. "Harvest an heirloom crop"
// Data/CraftingRecipes — NADA novo aqui (kit usa itens dos Caps. 3/4 + bin existente)
```
Condição de elegibilidade: `Condition: "PLAYER_HAS_FLAG LivingRoots_IntroSeen"` falsa → envia mail dia 2 se evento pulado (fallback anti-softlock).

### Sons
`questStarted` vanilla ao receber; `gift` ao abrir. Sem custom.

### i18n
```json
{ "mail.welcome-kit.title": "Welcome to the valley — Elena",
  "mail.welcome-kit.text": "Forget Joja hybrids for a season. Try these heirlooms, keep your soil covered, and come talk to me at Pierre's.^ ...",
  "quest.welcome-kit.1": "Place your Composter",
  "quest.welcome-kit.2": "Mulch 4 tiles of tilled soil",
  "quest.welcome-kit.3": "Harvest your first heirloom crop" }
```

### Loja
Não vendável (`ExcludeFromRandomSale: true`, sem entrada em `Data/Shops`). Preço 0, `CanBeTrashed: false` se virar item.

---

## Cap. 3 — Mulch / Soil Cover (Epic 4 · US-04-01 #30 / US-04-02 #31)

**Papel** (ROADMAP:37 + AGROECOLOGY:39): cobertura morta — previne ervas, **segura Soil Health** (zera/reduz `DailyDecayRate=2/dia` de `Constants.cs:58` no tile coberto; sinergia com `SeasonalDecayMultiplier`: Spring 0.5×, Summer 1.5×). Mecânica: aplicar em tile arado (como fertilizante vanilla, mas via `ICompostApplicationService`-like → novo `IMulchApplicationService`, DDD: contrato em `Domain/Interfaces/`, impl em `Services/`).

### Sprites
| Asset | Spec |
|---|---|
| Item | `Assets/mulch.png` **16×16**, marrom `#92400E` (usar `ModConstants.MulchColor` como base + highlights `#A16207/#78716C` da paleta compost) |
| Tile overlay | tint `MulchColor` opacidade 0.5 (`DefaultOpacity`) + pattern `Dots` (`PatternType`) p/ daltônicos; tile com mulch+planta = borda pontilhada |
| Partícula aplicação | reutilizar partícula do compost (marrom) — sem asset novo |

### Data JSON
```csharp
// Data/Objects
data["LivingRoots_Mulch"] = new ObjectData {
  Name = "Mulch", DisplayName = "{{i18n:mulch.name}}", Description = "{{i18n:mulch.description}}",
  Type = "Crafting", Category = -26 /* mesmo do compost */,
  Price = 5, Texture = "Mods/LivingRoots/Mulch", SpriteIndex = 0,
  ContextTags = new[] { "mulch_item", "compostable_item" },
};
// Data/CraftingRecipes — receita barata Y1 (fibra = capina, acessível dia 1)
data["LivingRoots_MulchRecipe"] = $"771 10 259 1/Home/{CompostItemId→Mulch} 1/false/null/Mulch";
// 771 fiber x10 + 259? usar IDs vanilla: 771 Fiber, 309 Acorn? — definir na spec; placeholder: "771 10/Home/LivingRoots.Mulch 1/false/null/Mulch"
```
Efeito (spec, não data): `ISoilDecayService` checa `tile.HasMulch` → `decay *= 0` (ou 0.25 p/ balancear); mulch consome 1 carga a cada N dias (sugerido 7, `MaturationIncrementDays`-like constante nova `MulchDurationDays`); chuva não remove.

### Sons
Aplicação: `hoeHit` ou `sandyStep` vanilla (passos em palha). Craft: `axe` (segue `CompostCraftingSound`). Sem `.ogg` custom Y1.

### i18n
```json
{ "mulch.name": "Mulch", "mulch.description": "Dry cover that keeps tilled soil alive. Blocks decay and weeds.",
  "message.mulch.invalid-tile": "Mulch can only be applied to tilled soil.",
  "message.mulch.already": "This tile is already mulched." }
```

### Loja
Receita à venda na Elena shop dia 1 (`Price: 100g`, `AvailableStockLimit: Player`); item craftável, não precisa vender o item. Pierre **não** vende (diferenciação agroecologia vs. convencional).

---

## Cap. 4 — Heirloom Seeds (Epic 6 · US-06-01 #34 / US-06-02 #35)

**Papel** (ROADMAP:39 + AGROECOLOGY:35): sementes crioulas da Elena vs. híbridas do Pierre — +Soil Health no plantio/colheita, growth levemente mais lento, qualidade maior em solo vivo (Healthy 67–100). Escopo Y1: **3 culturas** (parsnip heirloom primavera + 1 verão + 1 outono; Three Sisters — milho/feijão/abóbora — ficam no Epic 3, reusam este pipeline).

### Sprites
| Asset | Spec | Qtd |
|---|---|---|
| Semente (item) | **16×16** cada, envelope de papel com Broto verde (vs. sachê azul Pierre) | 3 |
| Produto | **16×16** cada (variação sutil: parsnip mais torta/manchada = charme heirloom) | 3 |
| Cultura (crop sheet) | 1 linha por cultura em `Assets/Crops/heirloom_crops.png`, fases = `DaysInPhase.Count` sprites lado a lado (padrão `TileSheets/crops`: cada fase ~16×32) | 3 linhas |

Arquivos: `Assets/heirloom_seeds.png` (3 ícones), `Assets/heirloom_produce.png` (3 ícones), `Assets/Crops/heirloom_crops.png`.

### Data JSON (por cultura; exemplo parsnip)
```csharp
// Data/Objects — semente + produto
data["LivingRoots_HeirloomParsnipSeeds"] = new ObjectData {
  Name = "Heirloom Parsnip Seeds", DisplayName = "{{i18n:heirloom.parsnip.seeds.name}}",
  Description = "{{i18n:heirloom.parsnip.seeds.description}}",
  Type = "Seeds", Category = -74 /* seed category vanilla */, Price = 25,
  Texture = "Mods/LivingRoots/HeirloomSeeds", SpriteIndex = 0,
  ContextTags = new[] { "heirloom_seed", "seed_item" },
};
data["LivingRoots_HeirloomParsnip"] = new ObjectData {
  Name = "Heirloom Parsnip", Type = "Basic", Category = -75 /* vegetable */,
  Price = 45 /* > parsnip vanilla 35: prêmio qualidade/solo */,
  Texture = "Mods/LivingRoots/HeirloomProduce", SpriteIndex = 0,
  ContextTags = new[] { "heirloom_produce", "compostable_item" },
};
// Data/Crops — chave = ID da SEMENTE
data["LivingRoots_HeirloomParsnipSeeds"] = new CropData {
  Seasons = new[] { "spring" }, DaysInPhase = new[] { 1, 2, 2, 2 }, // 7 dias (vanilla parsnip 4 — tradeoff heirloom)
  HarvestItemId = "LivingRoots_HeirloomParsnip",
  HarvestMethod = "Grab", ExtraHarvestChance = 0.1, // bônus solo vivo aplicado via C# (não data)
  Texture = "Mods/LivingRoots/HeirloomCrops", SpriteIndex = 0,
  CountForMonoculture = true, CountForPolyculture = true,
};
```
Efeito solo (C#): plantio heirloom `+2` soil health; colheita em solo Healthy `+1` e chance extra (stack com `ExtraHarvestChance`); híbridas Pierre: neutras ou `-1` (spec decide; ROADMAP só diz "mais sustentáveis").

### Sons
Colheita: `harvest` vanilla. Sem custom.

### i18n
```json
{ "heirloom.parsnip.seeds.name": "Heirloom Parsnip Seeds",
  "heirloom.parsnip.seeds.description": "Old variety, slow but kind to the soil. Sold by Elena.",
  "heirloom.parsnip.name": "Heirloom Parsnip",
  "message.heirloom.soil-bonus": "The living soil thanks you! (+1)" }
```

### Loja
Elena shop: sementes com `AvailableStock` ilimitado Y1, preço ~1.25× Pierre (qualidade justifica); desconto amizade via `PriceModifiers` + `Condition PLAYER_HEARTS`. Pierre continua vendendo híbridas (contraste narrativo; Joja Mart vende Joja Fertilizer Cap. 5 — triângulo ideológico completo).

---

## Cap. 5 — Joja Fertilizer (antagonista · ROADMAP:17 "custa 5% soil health")

**Papel**: espelho sombrio do compost — boost imediato (growth +25% vanilla `Speed-Gro` behavior via `Data/Objects` + `CustomFields` de fertilizante) ao custo de **−5 soil health** por aplicação (`Constants.cs: novo `JojaFertilizerSoilCost = 5f``). Vendido no Joja Mart, odiado pela Elena (gift hate Cap. 1). Ensina o tradeoff central do mod.

### Sprites
Item **16×16** `Assets/joja_fertilizer.png`: saco azul-Joja com logo "$" — deliberadamente plástico/industrial vs. estética orgânica do compost.

### Data JSON
```csharp
data["LivingRoots_JojaFertilizer"] = new ObjectData {
  Name = "Joja Fertilizer", DisplayName = "{{i18n:joja-fertilizer.name}}",
  Description = "{{i18n:joja-fertilizer.description}}",
  Type = "Basic", Category = -19 /* fertilizer vanilla */, Price = 80,
  Texture = "Mods/LivingRoots/JojaFertilizer", SpriteIndex = 0,
  ContextTags = new[] { "joja_fertilizer", "!compostable_item" }, // NUNCA compostável
};
// Data/Shops — append no Joja (TargetField ["Joja","Items"])
{ "Id": "LivingRoots_JojaFertilizer", "ItemId": "(O)LivingRoots_JojaFertilizer",
  "Price": 120, "AvailableStockLimit": "Player" }
```
Mecânica fertilizante vanilla: checar como Speed-Gro registra crescimento (tile `fertilizer` index) — spec deve mapear `HoeDirt.fertilizer` compat; efeito solo via `ICompostApplicationService`-inverso no `ModController.OnButtonPressed`.

### Sons
Aplicação: `fertilizer`? vanilla usa `dirtyHit` — confirmar na spec; compra: `purchaseClick`. Sem custom.

### i18n
```json
{ "joja-fertilizer.name": "Joja Fertilizer",
  "joja-fertilizer.description": "Fast growth now, dead soil later. (-5 soil health)",
  "message.joja-fertilizer.warning": "The soil hisses... (-5 health)" }
```

### Loja
**Só Joja Mart** (`Joja` shop). Pierre não vende (ele tem híbridas, mas não veneno — nuance). Preço alto p/ não dominar Y1.

---

## Cap. 6 — Small Cistern (Epic 7 · US-07-01 #36 / US-07-02 #37)

**Papel** (ROADMAP:40): captação de chuva — coleta água em dias chuvosos, reduz idas ao lago; alternativa ecológica ao poço. Big-craftable 1×2 + `Data/Machines` com trigger climático.

### Sprites
Big-craftable **16×32** `Assets/small_cistern.png`: barril de madeira com calha + nível d'água em 3 estágios (usar `ShowNextIndexWhenReady` / `IncrementMachineParentSheetIndex` ou 3 sprites: vazio/parcial/cheio — spec decide; precedente `composting_bin_{empty,processing,ready}.png` sugere 3 arquivos).

### Data JSON
```csharp
// Data/BigCraftables
data["LivingRoots_SmallCistern"] = new BigCraftableData {
  Name = "Small Cistern", DisplayName = "{{i18n:cistern.name}}", Description = "{{i18n:cistern.description}}",
  Price = 0, Fragility = 0, CanBePlacedOutdoors = true, CanBePlacedIndoors = false,
  Texture = "Mods/LivingRoots/SmallCistern", SpriteIndex = 0,
};
// Data/Machines — chave QUALIFICADA com (BC)
data["(BC)LivingRoots_SmallCistern"] = new MachineData {
  OutputRules = new[] {
    new MachineOutputRule { // enche sozinha na chuva — sem input
      Id = "LivingRoots_RainCollect",
      Triggers = new[] { new Trigger { Trigger = "DayUpdate",
        Condition = "WEATHER Here Rain" } },   // cobre Rain + Storm? spec verifica GSQ exato
      OutputItem = new[] { new MachineItemOutput { ItemId = "(O)LivingRoots_Rainwater", ... } },
      DaysUntilReady = 0, // pronto no dia seguinte
    },
  },
  PreventTimePass = new[] { "Inside" }, // só funciona outdoor
  // TODO spec: capacidade (sugestão 5 unidades), esvaziar overnight se destruída, inverno congela?
};
// Data/CraftingRecipes — barato Y1
data["LivingRoots_SmallCisternRecipe"] = "388 30 390 20/Home/(BC)LivingRoots_SmallCistern 1/false/null/Small Cistern";
// Data/Objects — a água coletada (item consumível p/ regador? ou auto-irrigação aura?)
data["LivingRoots_Rainwater"] = new ObjectData {
  Name = "Rainwater", Type = "Basic", Category = -26, Price = 0,
  Texture = "Mods/LivingRoots/Rainwater", SpriteIndex = 0,
  ContextTags = new[] { "rainwater", "!compostable_item" },
};
```
Questão aberta p/ spec (registrar, não resolver aqui): água vai p/ inventário (recarrega regador via C#) ou cisterna irriga tiles vizinhos passivamente (aura 3×3)? Segunda é mais "Year 1 mágico", primeira é mais simples (80/20 → recomendo inventário primeiro).

### Sons
Chuva coletada: `wateringCan` / `rain` ambiente vanilla; transbordo: `glug`? confirmar cue IDs na spec. Pronto: reutilizar padrão `composting_bin_ready.ogg` → `Assets/Audio/cistern_ready.ogg` (gotejo).

### i18n
```json
{ "cistern.name": "Small Cistern", "cistern.description": "Collects rainwater overnight when it rains.",
  "rainwater.name": "Rainwater",
  "message.cistern.collected": "Your cistern collected {0}x rainwater!",
  "message.cistern.frozen": "The cistern is frozen solid." }
```

### Loja
Receita na Elena shop (200g) + Robin Carpenter? Sugestão: Elena ensina (receita), Robin constrói upgrade grande depois (ponte p/ Mid Game greywater Epic 15). `AvailableStockLimit: Player`.

---

## Apêndice A — Matriz loja (quem vende o quê Y1)

| Item | Elena (`LivingRoots_ElenaShop`) | Pierre (`SeedShop`) | Joja (`Joja`) | Craft |
|---|---|---|---|---|
| Heirloom seeds | ✅ ilimitado | ❌ (só híbridas) | ❌ | ❌ |
| Mulch receita | ✅ 100g | ❌ | ❌ | ✅ fibra |
| Mulch item | ❌ | ❌ | ❌ | ✅ |
| Small Cistern receita | ✅ 200g | ❌ | ❌ | ✅ 30 wood 20 stone |
| Compost / bin | ✅ tutorial | ❌ | ❌ | ✅ (existente) |
| Joja Fertilizer | ❌ (odeia) | ❌ | ✅ 120g | ❌ |

## Apêndice B — Constantes novas propostas (`Constants.cs`)

```csharp
// Mulch
public const string MulchItemId = "LivingRoots.Mulch";
public const string QualifiedMulchItemId = "(O)LivingRoots.Mulch";
public const int MulchDurationDays = 7;
public const float MulchDecayMultiplier = 0f; // decay *= 0 em tile com mulch
// Heirloom
public const string HeirloomSeedTag = "heirloom_seed";
public const float HeirloomPlantBonus = 2f;
public const float HeirloomHarvestBonus = 1f;
// Joja
public const string JojaFertilizerItemId = "LivingRoots.JojaFertilizer";
public const float JojaFertilizerSoilCost = 5f;
// Cistern
public const string SmallCisternId = "LivingRoots.SmallCistern";
public const string MachineSmallCisternId = "LivingRoots.SmallCistern";
public const int CisternCapacity = 5;
// Elena
public const string ElenaNpcId = "LivingRoots_Elena";
public const string ElenaShopId = "LivingRoots_ElenaShop";
```

## Apêndice C — Ordem de implementação sugerida (80/20)

1. **Mulch** (menor esforço: 1 objeto + 1 receita + hook no decay existente) → 2. **Joja Fertilizer** (1 objeto + shop append + hook inverso) → 3. **Heirloom ×3** (pipeline semente→crop→produto, template p/ Three Sisters) → 4. **Small Cistern** (big-craftable + machine climática, maior risco técnico GSQ) → 5. **Welcome Kit** (mail+quest, depende de 1–3) → 6. **Elena NPC** (maior asset: sprites 16×32+64×64, schedule, diálogos, loja — coroa o arco).
