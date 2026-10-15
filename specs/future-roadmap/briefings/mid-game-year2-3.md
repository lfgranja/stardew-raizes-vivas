# Briefing: Mid Game Year 2-3 — Rotation Journal + Green Manure + Mobile Coop + Insect Hotels/Beehive Aura + Living Pharmacy + Adobe Shed/Green Roof

**Fonte interna:** `ROADMAP.md:42-53` · `LivingRoots/docs/EPICS_USER_STORIES.md` (Epics 8–13, US-08-01…US-13-02) · `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` v2.0.0 · `docs/research/remediation-*` · `research/xna-monogame-color-rendering-report.md` · `LivingRoots/ModEntry.cs` · `LivingRoots/Constants.cs` (`ModConstants`) · `LivingRoots/Assets/` + `LivingRoots/i18n/en.json` · `specs/001/002/003/004`
**Fontes externas (wiki 1.6.x, verificadas em 2026-10-05):** `Modding:Objects` · `Modding:Big_craftables` · `Modding:Recipe_data` (Cooking + Crafting) · `Modding:Crop_data` · `Modding:Machines` · `Modding:Buildings` · `Bee_House` (range/aura) · GMCM vs `IClickableMenu` · tilesheets/construção
**Status:** briefing de assets futuros — nada aqui está implementado. Não promover para `specs/` sem `/speckit.specify`.
**Público:** quem for escrever spec/plan/tasks de cada Epic 8–13.

---

## 0. Leitura cruzada: o que o ROADMAP pede vs. o que a codebase já tem

| ROADMAP Mid Game | Epic / US | Estado atual na codebase | Gap de asset |
|---|---|---|---|
| Rotation Journal + famílias (Brassicas, Solanaceae, Legumes) | Epic 8 — US-08-01 Journal, US-08-02 famílias/bônus | `SoilHealthService` persiste `soil_health_data_<saveId>` esparso (zeros omitidos); `SeasonalDecayMultiplier`, `SoilDecayService`; visualização (overlay/tooltip/hoe feedback) com orçamento <4 ms; **nenhum histórico por tile, nenhuma família** | Cap. 1 |
| Green Manure (clover/oats via Elena) | Epic 9 — US-09-01 sementes, US-09-02 incorporação | `Data/Crops` nunca tocado; `ModEntry.OnAssetRequested` só edita `Data/CraftingRecipes`, `Data/Objects`, `Data/Machines`; sem `CustomFields` de família | Cap. 2 |
| Manejo animal integrado + Mobile Coop | Epic 10 — US-10-01 pasto/pragas, US-10-02 esterco, US-10-03 Mobile Coop | `LocationProvider`, `PlayerProvider`, `TimeProvider`, `SeasonProvider` existem (seams testáveis); `ModController` é orquestrador com `ExecuteWithConcurrencyGuard`; **nenhum `Data/Buildings`, nenhum tracking de galpão móvel** | Cap. 3 |
| Insetos benéficos + Beehive aura + Insect Hotels | Epic 11 — US-11-01 estender colmeia, US-11-02 hotel craftável | Padrão `Data/Machines` dominado (CompostingBin com `OutputRules`/`MachineOutputRule`/`MachineItemOutput`); overlay com culling + cache existe e é o veículo natural da aura | Cap. 4 |
| Living Pharmacy (Rue, Boldo, Chamomile) → tônicos + sprays naturais | Epic 12 — US-12-01 cultivo, US-12-02 receitas | `OrganicWasteValidator` (aceita `-74/-75/-79/-80/-81`, rejeita compost `-2` — anti-loop documentado em `specs/003/.../compost-item.md §2.3`); `ObjectData` com `Edibility`/`Buffs` nunca usado; sem `Data/CookingRecipes` | Cap. 5 |
| Bio-construção: Adobe Shed + Green Roof (Robin aprende com Elena) | Epic 13 — US-13-01 shed, US-13-02 telhado verde | `Data/Buildings` nunca tocado; `manifest.json` sem `Content Patches`/`Dependencies` (todo conteúdo hoje é via `OnAssetRequested` em C#); `.csproj` copia `Assets/**` e `i18n/**/*.json` para output | Cap. 6 |

### 0.1 Decisões de plataforma que valem para TODOS os capítulos

1. **Registro de conteúdo: continuar via `ModEntry.OnAssetRequested` (C#), não `content.json`.**
   O mod hoje registra `Data/CraftingRecipes` (string legada `"388 50 390 25…/Home/ID 1/false/null/…"`, `ModEntry.cs:116-124`), `Data/Objects` (`ObjectData`, `ModEntry.cs:125-151`) e `Data/Machines` (`MachineData`, `ModEntry.cs:152-176`) por `e.Edit()`. Seguir o mesmo padrão para `Data/BigCraftables`, `Data/CookingRecipes`, `Data/Crops`, `Data/Buildings`. Motivo: um único caminho de registro, testável sem Content Patcher, sem nova dependência no `manifest.json`. Se um dia migrar para Content Patcher, fazer de uma vez para todos os assets (não misturar).
2. **IDs: manter a convenção `LivingRoots.<Nome>` (namespace-ponto), NUNCA `UniqueID_Nome`.**
   `ModConstants.CompostItemId = "LivingRoots.Compost"` e a comparação em `CompostApplicationService` é contra `QualifiedItemId` (`(O)LivingRoots.Compost` / `(BC)LivingRoots.…`). A lição do briefing `compost-item.md §3` vale para todos os capítulos: a chave registrada deve ser byte-idêntica à constante.
3. **Magic numbers → `ModConstants` (`LivingRoots/Constants.cs`).** Custos, dias, raios de aura, bônus %, tamanhos de tile, opacidades. Nada hardcoded no renderer ou serviço.
4. **DDD: contrato em `Domain/Interfaces/` (ou `Domain/`), impl em `Services/`, eventos em `Controllers/`, `new` só em `ModEntry.cs`.** Exceção legada conhecida: `Domain/Services/OrganicWasteValidator.cs` usa `IMonitor` — não repetir.
5. **Persistência: `ModDataService` + chaves `*_KeyPrefix`, cache esparso.** Solo omite zeros; históricos de rotação pasto/aura devem seguir o mesmo princípio (só persistir tiles com histórico relevante; capar com `MaxTilesPerLocation`/`MaxTilesPerSave`).
6. **Threading/render:** loop single-thread; `lock` só para snapshot, nunca durante `spriteBatch.Draw()`; `Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp)`; `Game1.tileSize` dinâmico + `Game1.GlobalToLocal()` + `Utility.ModifyCoordinatesForUIScale()`; posições inteiras; texturas-padrão brancas tingidas em draw-time; orçamento <4 ms, culling de viewport +1 tile, degradação >1000 tiles (desliga patterns) — tudo em `LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md §§5/8/11` e `research/xna-monogame-color-rendering-report.md`.
7. **Acessibilidade (Game Accessibility Guidelines, não WCAG):** cor nunca sozinha — padrão + texto + ícone; patterns ≥0.7 opacidade mesmo com overlay em 0.2; `ReduceMotion` mata flash/movimento; fontes só `Game1.smallFont/tinyFont/dialogueFont` com `MeasureString`, sombra 1px; tooltip nativo via `IClickableMenu.drawHoverText`.
8. **Config: GMCM para toggles, `IClickableMenu` custom só para o Journal.** GMCM (Generic Mod Config Menu) edita opções de config — é o padrão da comunidade e o esperado pelos jogadores; menu custom (`IClickableMenu` + paginação + `drawHoverText`) só se justifica para o Rotation Journal (conteúdo tabular navegável, não um formulário). Detalhes no Cap. 1.
9. **Segurança/logging:** sem `.Result`/`.Wait()`, sem lock através de I/O; logs sem mensagens cruas de exceção (`ex.GetType().FullName` + `HResult`, `Trace` apenas); `CheckForOverflowUnderflow=true`; validar coordenadas/nomes (padrão `FileNameSanitizationService`/`PathValidationService`).
10. **Assets/i18n são placeholders hoje:** `Assets/*.png` + `Assets/Audio/` e `i18n/*.json` são copiados pelo `.csproj` mas **nenhum C# carrega textura ou som** (`Content.Load` inexistente) e as chaves i18n atuais (`compost.name`, `message.compost.*`) são literais em `OnAssetRequested` (DisplayName/Description hardcoded). Cada capítulo abaixo especifica sprite + data + i18n juntos para que o wiring aconteça de uma vez.

### 0.2 Mapa de dados vanilla que os capítulos reutilizam

| Asset | Chave | Campos que importam (wiki 1.6) |
|---|---|---|
| `Data/Objects` | ID não-qualificado (`LivingRoots.X`) | `Name`, `DisplayName`/`Description` (tokenizáveis → chaves i18n), `Type` (`Basic/Crafting/Seeds/Cooking`…), `Category` (ex. `-74` Seeds, `-2` Crafting, `-7` Cooking), `Price`, `Texture` (default `Maps/springobjects`), `SpriteIndex`, `Edibility` (−300 = não-comestível; `Edibility×2.5` energia, `×1.125` vida), `IsDrink`, `Buffs[]` (`Id/Duration/BuffId` de `Data/Buffs` ou `CustomAttributes`: `FarmingLevel/LuckLevel/Speed/MaxStamina/MagneticRadius/Attack/Defense`…), `ContextTags[]`, `CanBeGivenAsGift/CanBeTrashed`, `CustomFields` |
| `Data/BigCraftables` | ID não-qualificado | `(BC)` em uso; `Fragility` (0 pega c/ qualquer ferramenta, 1 destrói c/ machado/enxada/picareta, 2 fixo), `CanBePlacedIndoors/Outdoors`, `IsLamp`, `Texture` (default `TileSheets/Craftables`), `SpriteIndex` |
| `Data/CraftingRecipes` (string legada) | nome da receita | `"ingredientes/Home\|Field/yield [qtd]/bigCraftable? true\|false/unlock/displayName"`, ex. Bee House `"388 40 382 8 335 1 724 1/Home/10/true/s Farming 3/"`; unlocks: `default`, `s <Skill> <nível>`, `f <NPC> <corações>`, senão evento/quest |
| `Data/CookingRecipes` (string legada) | nome da receita | `"ingredientes/par1 par2/yield/unlock/"`, ex. Farmer's Lunch `"195 1 24 1/60 1/240/s Farming 3/"`; tônicos = `IsDrink true`; sprays = `Type: Basic`, não-comestíveis |
| `Data/Machines` | ID **qualificado** (`(BC)LivingRoots.X`) | `OutputRules[]` (`Triggers`: `ItemPlacedInMachine/OutputCollected/MachinePutDown/DayUpdate`; `RequiredItemId/RequiredTags/RequiredCount/Condition` (GSQ); `OutputItem` c/ tokens `DROP_IN_ID`, `DROP_IN_PRESERVE`, `NEARBY_FLOWER_ID`; `MinutesUntilReady/DaysUntilReady`; `RecalculateOnCollect` (padrão Bee House); `OutputMethod` C# forma `"<tipo>: <método>"`); `PreventTimePass` (`Outside/Inside/Spring…/Sun/Rain`), `LightWhileWorking/WobbleWhileWorking/ShowNextIndexWhileWorking/WhenReady`, `InvalidItemMessage`, `ExperienceGainOnHarvest` (`"Farming 7"`…) |
| `Data/Crops` | ID da **semente** | `Seasons[]`, `DaysInPhase[]` (1 sprite por fase, 1 crop por linha via `SpriteIndex`), `RegrowDays`, `IsRaised`, `HarvestItemId/MinStack/MaxStack/MaxIncreasePerFarmingLevel/ExtraHarvestChance` (geométrica: `1/(1-p)−1`), `HarvestMethod` (`Grab`/`Scythe`), `HarvestMin/MaxQuality`, `Texture` (default `TileSheets\crops`), `TintColors`, `CountForMonoculture/Polyculture`, `PlantableLocationRules` (Ground/GardenPot/Any + `DeniedMessage`), `CustomFields` (**aqui vai a família botânica**) |
| `Data/Buildings` | string ID única | `Name/Description` (tokenizáveis → `Strings/Buildings`), `Texture` (asset sob `Content`), `Builder` (`Robin`/`Wizard`/null), `BuildCost/BuildMaterials[]` (`ItemId` qualificado + `Amount`), `BuildDays`, `BuildCondition` (GSQ), `Size`, `CollisionMap` (`X/O`), `HumanDoor/AnimalDoor` (+ sons/durações), `SourceRect/SeasonOffset/Skins[]/DrawLayers[]/DrawOffset/FadeWhenBehind`, `IndoorMap/IndoorMapType` (`StardewValley.Shed`, `AnimalHouse`, `DecoratableLocation`…), `BuildingToUpgrade` (cadeia Coop→Big Coop), `HayCapacity/ItemConversions/Chests`, `ActionTiles/DefaultAction/TileProperties`, `Metadata/ModData` |
| Aura vanilla de referência | Bee House | Mel base 100g; flor madura ≤**5 tiles cardeais** (diamante, 60 tiles úteis) vira mel floral (Tulip 160 … Fairy Rose 680); prioridade = flor mais próxima, desempate esquerda-horizontal depois horário; checagem no `DayUpdate`/`RecalculateOnCollect`; **não produz indoor nem no inverno** (`PreventTimePass: Inside, Winter`); colocação fora da fazenda = selvagem + risco de destruição em path de NPC. Espantalho: raio 8 (17 Deluxe) via context tag `crow_scare_radius_<n>`. Mod Range Highlight prova o padrão de overlay de raio reutilizável. |

---

## 1. Rotation Journal + famílias botânicas (Epic 8 · US-08-01 / US-08-02)

**Intenção ROADMAP:** classificar plantas por família (Brassicas — couve-flor, repolho; Solanaceae — tomate, pimenta; Legumes — feijões, ervilhas) e premiar rotação correta com bônus de qualidade + saúde do solo.

### 1.1 Design (o que especificar)

- **Histórico por tile:** anelar `soil_health_data_<saveId>` com `rotation_history_<saveId>`: `Dictionary<location, Dictionary<"X,Y", Queue<familyId>>>` (profundidade 2–3 safras, constante em `ModConstants`: `RotationHistoryDepth`). Esparso: tile sem histórico não entra no save.
- **Famílias:** enum `CropFamily { Brassica, Solanaceae, Legume, Cucurbit, Allium, Root, Grain, Flower, Unknown }` em `Domain/` (+ `Unknown` para flores vanilla/forrageáveis que não afetam mel nem rotação). Tabela `seedId → família` via `CustomFields` em `Data/Crops` para sementes do mod + dicionário C# hardcoded-versionado para sementes vanilla (Cauliflower/Cabbage→Brassica; Tomato/Pepper/Potato/Eggplant→Solanaceae; Green Bean/Pea→Legume; Corn/Pumpkin→Three Sisters, neutras/bônus próprio; resto→Unknown). Regra: **mesma família no mesmo tile em seguida = monocultura** (−saúde, sem estrela); **família diferente = rotação** (+saúde, +chance de qualidade); Legume após qualquer família = bônus extra de N (fixa N — sinergia com Cap. 2).
- **Journal UI (US-08-01):** `IClickableMenu` custom paginado — única exceção à regra "GMCM para config". Página por estação/talhão, cada tile com últimas N famílias (ícone + nome), alerta "repita / rote" e média de saúde do talhão. Navegação: setas + `drawHoverText`; fonte `dialogueFont` no título, `smallFont` no corpo; respeitar zoom/UI scale (`GlobalToLocal`, `ModifyCoordinatesForUIScale`).
- **GMCM (config, não conteúdo):** toggles `RotationBonusesEnabled`, `JournalToastsEnabled`, `RotationStrictness` (leniente/estrito), opacidade do selo de família no tooltip. Conteúdo do journal NUNCA em GMCM (GMCM é formulário, não browser tabular).

### 1.2 Sprite

| Asset | Spec | Notas |
|---|---|---|
| Ícones de família 16×16 (9) | `Assets/rotation/family_<brassica|solanaceae|legume|cucurbit|allium|root|grain|flower|unknown>.png`, pixel-art 16×16, fundo transparente, paleta terrosa do design system (`Mulch #92400E`, `Healthy #15803D`, `Sun #FACC15`) | Um `Texture` custom único `LivingRoots/RotationIcons` + `SpriteIndex` 0–8 é preferível a 9 arquivos (1 bind, sem troca de textura — cf. relatório XNA §4 abordagem B). Fallback: tintar quadrado branco via `ColorDTO` se textura ausente (nunca crashar — `GetTexture()` null-guard, cf. `remediation-visualization-rendering.md` FR-004) |
| Selo de tooltip | reutilizar pattern textures (stripes/dots/solid) — sem asset novo | Brassica=stripes, Solanaceae=dots, Legume=solid+folha, seguindo §8 do design system |
| Capa do Journal | `Assets/ui/journal_cover.png` 64×64 (livro de couro + raiz), `TileSheets`-style | Opcional P1; menu funciona sem |

### 1.3 Data

- `Data/Crops` (sementes do mod, ver Cap. 2/5): `"CustomFields": { "LivingRoots.Family": "Legume" }` etc. Para vanilla, **não editar `Data/Crops`** — lookup C# (`CropFamilyRegistry`, `Domain/Services/`, puro, testável sem jogo).
- `Data/Objects` (o journal como item? **Não.** Journal é menu, não item — abrir via tecla configurável + aba no menu do mod, não via inventário. Evita `SpriteIndex`/receita à toa — YAGNI).
- `Data/CraftingRecipes`: nenhuma (journal é concedido pela Elena no evento de Year 2, via `mail` + GSQ `BuildCondition`-style; registrar flag `LivingRoots.RotationUnlocked` em `ModData`).
- Constantes novas: `RotationHistoryDepth = 3`, `RotationBonusHealth = +8f`, `MonoculturePenaltyHealth = −6f`, `RotationQualityBonusChance = 0.10f`, `RotationModDataKey = "LivingRoots.RotationHistory"`.

### 1.4 UI

- **Custom `RotationJournalMenu : IClickableMenu`**: lista paginada (`< 1/3 >`), cada linha = tile/talhão + sequência de ícones + veredito; hover = `drawHoverText("Tomate (Solanaceae) → Feijão (Legume): rotação ✓ +8 solo")`; tecla de atalho configurável (default `J`); fecha com `Esc/E`.
- **HUD/tooltip**: estender `FormatTooltipText` para `"Soil Health: 72% (Healthy) · Last: Solanaceae → plant Legume"` (respeitar largura máx 200px + wrap, §4 design system).
- **Hoe feedback**: manter flash 300 ms + texto 1000 ms; cor = categoria de saúde (não de família — família vai no texto).
- **GMCM página "Rotation"**: 4 opções (2 bools, 1 enum, 1 slider opacidade 0.2–0.8). Registrar via API GMCM com fallback silencioso se GMCM ausente (nunca dependência dura no `manifest.json`).

### 1.5 i18n (seguir `i18n/en.json` existente, chaves minúsculas com pontos)

```json
{
  "rotation.journal.title": "Rotation Journal",
  "rotation.family.brassica": "Brassicas",
  "rotation.family.solanaceae": "Nightshades",
  "rotation.family.legume": "Legumes",
  "rotation.verdict.rotate": "Rotate ✓ ({0} → {1})",
  "rotation.verdict.repeat": "Same family — soil tires ({0})",
  "rotation.toast.bonus": "Good rotation! +{0} soil health.",
  "rotation.toast.monoculture": "Monoculture… soil loses health.",
  "rotation.config.strictness": "Rotation strictness"
}
```

`DisplayName/Description` de sementes com família usam tokenizável `[LocalizedText …]` ou string i18n; texto de veredito ≥ contraste 4.5:1 sobre fundo `Color(0,0,0,200)`.

### 1.6 Riscos / pré-requisitos

- Mapear IDs vanilla corretamente (HarvestItemId vs seedId — a chave de `Data/Crops` é a **semente**). Teste de contrato: tabela de regressão seed→família.
- Multiplayer: histórico por tile precisa de merge por `PerScreen`/host-autoritário (`HostAtomicLockService` já existe — reutilizar).
- Não prometer detecção automática de "talhão" — agrupar por proximidade é P2; v1 = por tile + média da location.

---

## 2. Green Manure — sementes de cobertura (Epic 9 · US-09-01 / US-09-02)

**Intenção ROADMAP:** Elena vende sementes de adubo verde (trevo, aveia) que se incorporam ao solo com grande boost de saúde.

### 2.1 Design

- Duas culturas de cobertura: **Clover** (Legume, fixa N, boost grande, ciclo curto) e **Oats** (Grain/grass, biomassa, boost médio + supressão de ervas). Plantar → crescer → **incorporar com enxada antes da floração/colheita** (mecânica US-09-02: usar enxada num trevo maduro = "incorporate", some a planta, aplica `+GreenManureHealth` no tile e registra família Legume/Grain no histórico do Cap. 1). Colher em vez de incorporar = feno/fibra pequeno, sem boost (trade-off explícito).
- Venda: Elena (quando existir; antes disso, receita `s Farming 2` + loja da Pierre como fallback? **Decisão para a spec:** vender via Elena por `ShopData`/`Dialogue` quando NPC existir; v1 = receita de crafting de sementes — ver §2.3).

### 2.2 Sprite

| Asset | Spec |
|---|---|
| `Assets/crops/green_manure.png` — tilesheet custom `LivingRoots/GreenManureCrops`, **2 linhas** (row 0 = clover, row 1 = oats), 5–6 estágios por linha, cada estágio 16×16 (padrão `TileSheets\crops`: 1 crop por linha, N fases) | Estágio final do trevo com pontinhos brancos (flor) = sinal visual de "incorpore AGORA"; aveia dourada alta. Reutilizar `TintColors`? Não — sprites dedicados, paleta `Healthy`/`Sun`. |
| Ícones de semente 16×16 | `Assets/seeds/clover_seeds.png`, `oats_seeds.png` OU `Texture` custom + `SpriteIndex` em `Data/Objects` (`Texture: "LivingRoots/Seeds"`) |
| Ícone de item ceifado (fibra/feno) | reutilizar fibra vanilla (`771`) — sem asset novo |

### 2.3 Data

```csharp
// Data/Objects — sementes (Type Seeds, Category -74 para aparecerem como sementes)
data["LivingRoots.CloverSeeds"] = new ObjectData {
  Name = "Clover Seeds", DisplayName = "[LocalizedText … ou i18n: green-manure.clover-seeds.name]",
  Description = "…", Type = "Seeds", Category = -74, Price = 30,
  Texture = "LivingRoots/Seeds", SpriteIndex = 0,
  ContextTags = new() { "seed_item" }, CustomFields = … // família vai no Crop, não no Object
};
// Data/Crops — chaves = IDs das SEMENTES acima
crops["LivingRoots.CloverSeeds"] = new CropData {
  Seasons = ["spring","summer","fall"], DaysInPhase = [1,1,1,2], RegrowDays = -1,
  HarvestItemId = "(O)771", HarvestMinStack = 1, HarvestMaxStack = 2,   // fibra se ceifado
  HarvestMethod = "Scythe", Texture = "LivingRoots/GreenManureCrops", SpriteIndex = 0,
  CountForMonoculture = false, CountForPolyculture = false,             // cobertura não conta p/ conquistas
  CustomFields = new() { ["LivingRoots.Family"] = "Legume", ["LivingRoots.CoverCrop"] = "true" }
};
// Aveia: SpriteIndex = 1, DaysInPhase = [1,2,2,3], HarvestItemId fibra/feno similar, Family = "Grain"
// Data/CraftingRecipes — fallback até Elena existir:
recipes["LivingRoots.Clover Seeds"] = "771 5/Home/LivingRoots.CloverSeeds 4/false/s Farming 2/";
```

- Incorporação: **não é `Data/Machines`** — é interação `ButtonsPressed` + `HoeDirt`/`Crop` no tile (novo `IGreenManureService.TryIncorporate(tile)` chamado do `ModController`, mesmo ponto onde `TryApplyCompost` vive hoje — `ModController.cs:845`). Regra: `crop != null && crop.modData["LivingRoots.CoverCrop"] && mature && tool == Hoe → destroy crop + `soilHealth += CloverIncorporateHealth (+25) / OatsIncorporateHealth (+15)`, clamp 0–100`.
- Constantes: `CloverSeedPrice`, `OatsSeedPrice`, `CloverIncorporateHealth = 25f`, `OatsIncorporateHealth = 15f`, `CoverCropWindowDays` (dias ideais antes de florescer de mais e virar erva — P1: virar weed se ignorado).

### 2.4 UI

- Tooltip no tile com cobertura: `"Clover (Legume cover) — hoe to incorporate +25"`. Hoe feedback reutilizado (`"+25"` flutuante — mesma chave `floating-text` com `{0}`).
- Overlay: tom `Healthy` translúcido + pattern `Solid` quando cobertura viva (é "solo vivo" literalmente).
- Elena (futura): carta/tutorial `"green-manure.elena-letter"` explicando incorporar vs. colher.

### 2.5 i18n

```json
{
  "green-manure.clover-seeds.name": "Clover Seeds",
  "green-manure.clover-seeds.description": "Fast cover crop. Hoe it under before bloom for +25 soil health.",
  "green-manure.oats-seeds.name": "Oats Seeds",
  "green-manure.oats-seeds.description": "Biomass cover crop. Hoe it under for +15 soil health and fewer weeds.",
  "green-manure.incorporated": "Incorporated! +{0} soil health.",
  "green-manure.too-late": "It went to seed… chop it for fiber instead.",
  "message.green-manure.not-cover": "Only cover crops can be incorporated."
}
```

### 2.6 Riscos

- `HarvestMethod: Scythe` vs. enxada-incorpora: os dois gestos precisam coexistir (foice = colhe fibra; enxada = incorpora). Especificar prioridade por ferramenta segurada.
- `PlantableLocationRules`: permitir `GardenPot`? Cobertura em vaso não faz sentido — negar com `DeniedMessage` i18n (detalhe barato, grande polimento).
- Balanço: +25 é enorme (compost dá +15) — justificar: ocupa o tile por ~5 dias + trabalho + janela. Spec deve fixar números ou marcar `TBD-balance`.

---

## 3. Mobile Coop — pastejo rotacionado (Epic 10 · US-10-01 / US-10-02 / US-10-03)

**Intenção ROADMAP:** animais soltos sem cerca comem ervas/insetos (controle de pragas) e deixam esterco que **com mulch melhora o solo, no solo nu compacta**; Mobile Coop viabiliza pastejo rotacionado.

### 3.1 Design

- **US-10-01 (pasto):** cada animal externo/dia: consome N weeds num raio R (remove `Object` erva + `Crop` morto?) e suprime evento de praga no raio (hook no futuro sistema de pragas; v1 = +felicidade animal + −ervas). Sem cerca = comportamento ativo; com cerca = comportamento vanilla (evita quebrar fazendas existentes).
- **US-10-02 (esterco):** cada animal externo/dia deixa 0–1 `manure tile` (estado volátil em `ModData`, expira em 2 dias se ignorado). Interação com enxada/custom: se tile tem mulch → `+ManureMulchedHealth` (+6); se solo nu → `−ManureBarePenalty` (−4 compactação) + mensagem. Com compost/mulch por cima antes do vencimento = bônus; sem manejo = penalidade. É o "mulch matters" do mod em forma jogável.
- **US-10-03 (Mobile Coop):** building `LivingRoots.MobileCoop` — galinheiro pequeno móvel (capacidade 2–4 galinhas, sem incubadora), reposicionável barato (custo de movimento, não de construção), que define o **centro do raio de pastejo**. Mover = rotacionar o pasto (sinergia direta com Caps. 1–2: pasto recém-saído recebe trevo).

### 3.2 Sprite

| Asset | Spec |
|---|---|
| Exterior Mobile Coop | `Assets/buildings/mobile_coop.png` — 4×3 tiles (pequeno vs. Coop vanilla 6×4), madeira + lona listrada, **rodas visíveis** (leitura "móvel"), 4 estações via `SeasonOffset` OU textura única (P1: 1 textura, sem offset — YAGNI) + `DrawShadow: true`, `FadeWhenBehind: true` |
| Interior | reutilizar mapa `Coop` vanilla (`IndoorMap: "Coop"`, `IndoorMapType: StardewValley.AnimalHouse`, `MaxOccupants: 4`) — **zero asset de interior no v1** |
| Esterco (tile) | `Assets/objects/manure.png` 16×16 (monte marrom, 2 variantes via `ColorOverlayFromNextIndex` ou 2 `SpriteIndex`) |
| Pegadas/pasto | reutilizar overlay do Cap. 4 (anel de raio) — sem asset novo |

Construção de tilesheets: textura sob `Content` via asset custom (`Texture: "LivingRoots/MobileCoop"`), `SourceRect` exato, `CollisionMap` (`"XXXX\nXOOX"`-style com porta), `HumanDoor` + `AnimalDoor` (abrir de manhã = animais saem; fechar = recolhem — acoplar ao raio de pasto: porta fechada = sem esterco/pasto).

### 3.3 Data

```csharp
// Data/Buildings
data["LivingRoots.MobileCoop"] = new BuildingData {
  Name = "[i18n coop.name]", Description = "[i18n coop.description]",
  Texture = "LivingRoots/MobileCoop", Builder = "Robin",
  BuildCost = 8000, BuildMaterials = [ { ItemId = "(O)388", Amount = 150 }, { ItemId = "(O)390", Amount = 50 }, { ItemId = "(O)771", Amount = 30 } ],
  BuildDays = 1, Size = new(4, 3), CollisionMap = "XXXX\nXXXX\nXOOX",
  HumanDoor = new(1, 2), AnimalDoor = new …,
  IndoorMap = "Coop", IndoorMapType = "StardewValley.AnimalHouse", MaxOccupants = 4,
  HayCapacity = 0, // sem silo próprio — depende de silo existente (trade-off vs. Coop vanilla)
  MagicalConstruction = false
};
// mover = demolir+reconstruir com desconto? NÃO — especificar MoveCost (ex. 500g, instantâneo, Robin ou menu do próprio building via ActionTiles).
// Data/Objects — esterco como item coletável? NÃO no v1 (é estado de tile, não item). YAGNI.
```

- Pastagem/esterco: serviço `IGrazingService` (`DayStarted`: para cada `FarmAnimal` externo, rolar consumo de weeds + chance de esterco; `ButtonsPressed`: aplicar mulch/interagir). Persistência: `grazing_manure_<saveId>` esparso com expiração (`DayStarted` purga vencidos).
- Constantes: `GrazingRadius = 6`, `WeedsEatenPerAnimal = 3`, `ManureChance = 0.5f`, `ManureMulchedHealth = +6f`, `ManureBarePenalty = −4f`, `ManureExpiryDays = 2`, `MobileCoopCapacity = 4`, `MobileCoopMoveCost = 500`.

### 3.4 UI

- **Anel de pastejo:** overlay de raio (mesmo renderer do Cap. 4 — especificar `IRangeOverlay` compartilhado: círculo/diamante tiled, cor `Sun #FACC15` 0.35, `ReduceMotion`-safe, toggle GMCM). Padrão comprovado: mod Range Highlight.
- **Tile com esterco:** tooltip `"Fresh manure — mulch it (+6) or it compacts (−4 in 2d)"` + mini-ícone; hoe feedback adaptado.
- **Menu do Coop:** `ActionTiles` na porta → diálogo Robin-style "Move coop (500g)?" + seletor de local (reutiliza menu de construção vanilla se possível; custom só se inevitável).
- GMCM: `GrazingEnabled`, `ManureExpiryDays`, `ShowGrazingRadius`.

### 3.5 i18n

```json
{
  "coop.name": "Mobile Coop",
  "coop.description": "A small coop on wheels. Move it to rotate pasture.",
  "coop.move.prompt": "Move the coop here for {0}g?",
  "coop.move.done": "Coop moved. Fresh pasture!",
  "manure.fresh.name": "Fresh Manure",
  "manure.mulched": "Mulched! +{0} soil health.",
  "manure.compacted": "Trampled bare… {0} soil health.",
  "manure.expired": "The manure dried out.",
  "grazing.weeds-eaten": "The hens cleared weeds and bugs."
}
```

### 3.6 Riscos

- `BuildingType`: **não** criar subclasse C# (`BuildingType` custom crasha save/multiplayer — wiki adverte; usar `Building` genérico + lógica por tipo via `building.GetMetadata()`/`ModData`).
- Animais vanilla (`FarmAnimal`) + `AnimalHouse`: `ValidOccupantTypes: ["Coop"]` para aceitar galinhas; gravidez `AllowAnimalPregnancy: false` (v1).
- Performance: varredura diária de animais/weeds é `DayStarted` (fora do frame) — nunca no `RenderedWorld`.
- Esterco como tile-state vs. objeto: se virar item coletável, vira fertilizer loop — manter como estado até spec dizer o contrário.

---

## 4. Insetos benéficos — aura da colmeia + Insect Hotels (Epic 11 · US-11-01 / US-11-02)

**Intenção ROADMAP:** colmeias ganham 2ª função (aura de insetos benéficos além do mel); Insect Hotels craftáveis atraem benéficos = controle natural de pragas, alternativa a pesticidas.

### 4.1 Design

- **Aura (US-11-01):** cada Bee House (vanilla `(BC)10` + futuras do mod) projeta aura **raio 5 cardeais** (mesma geometria do mel — reaproveitar regra que o jogador já conhece) que suprime pragas/doenças nos cultivos do raio e dá `+AuraQualityChance` na colheita. Sem flor no raio = aura fraca (50%); com flor = aura plena (sinergia flor→mel→aura, um só sistema mental). Inverno/indoor = sem aura (igual ao mel — consistência gratuita).
- **Insect Hotel (US-11-02):** BigCraftable que projeta aura menor (**raio 3**) o ano todo (inclusive inverno se sob cobertura? **decisão p/ spec:** sim, mas metade do efeito — hotéis são abrigo, colmeias são atividade). Recarrega com `Fiber + Wood` (trigger `ItemPlacedInMachine` sem output? ou `DayUpdate` puro). Stack: auras sobrepostas **não somam** (max, não stack — evita hotel-spam; documentar).
- **Pragas (dependência!):** hoje **não existe sistema de pragas** no mod (só `DailyDecayRate` + `SeasonalDecayMultiplier`). A spec do Cap. 4 deve definir o MVP de praga (ex.: `crop.chance de "aphids"` por dia em solo pobre, suprimida na aura) OU declarar aura como bônus de qualidade/saúde até pragas existirem. **Não especificar aura sem especificar o que ela suprime.**

### 4.2 Sprite

| Asset | Spec |
|---|---|
| `Assets/machines/insect_hotel.png` — BigCraftable 16×32 (2 tiles verticais, padrão `TileSheets/Craftables`), caixa de madeira com furos/bambu/pinhas, 2 frames (`ShowNextIndexWhileWorking` = com abelhas/joaninhas visíveis) | `Texture: "LivingRoots/InsectHotel"`, `SpriteIndex: 0`. Fragility 0, indoor+outdoor true, `IsLamp: false` (P2: `LightWhileWorking` âmbar sutil) |
| Partículas de aura | **sem textura nova**: reutilizar `VisualizationTextures` (pontinhos brancos tingidos `Sun #FACC15` / `Healthy #15803D`, abordagem B do relatório XNA) flutuando no raio (respeitar `ReduceMotion` — estático se ativo) |
| Anel de raio | mesmo `IRangeOverlay` do Cap. 3 (diamante 5 p/ colmeia, quadrado 3 p/ hotel) |

### 4.3 Data

```csharp
// Data/BigCraftables
data["LivingRoots.InsectHotel"] = new BigCraftableData {
  Name = "Insect Hotel", DisplayName = "[i18n]", Description = "[i18n]",
  Price = 0, Fragility = 0, CanBePlacedIndoors = true, CanBePlacedOutdoors = true,
  Texture = "LivingRoots/InsectHotel", SpriteIndex = 0,
  ContextTags = new() { "LivingRoots.beneficial_aura_3" }  // raio legível por C# sem hardcode reverso
};
// Data/CraftingRecipes
recipes["LivingRoots.Insect Hotel"] = "388 40 771 30 709 10/Home/LivingRoots.InsectHotel 1/true/s Farming 3/";
// Colmeia: NÃO redefinir (BC)10 — estender por C#: detectar bee houses na location (parentSheetIndex 10 / contexto) e aplicar aura via IBeneficialInsectService.
// Data/Machines p/ o hotel (abrigo passivo, sem output):
machines["(BC)LivingRoots.InsectHotel"] = new MachineData {
  OutputRules = [], // sem output — só aura; InteractMethod opcional p/ "refill" que reseta força da aura
  PreventTimePass = [], // funciona o ano todo (decisão Cap. 4.1)
  WobbleWhileWorking = false, ShowNextIndexWhileWorking = true
};
```

- Serviço `IBeneficialInsectService`: `DayStarted` computa cobertura de aura por tile (colmeias r5 + hotéis r3, max-não-stack, indoor/inverno filtrados); expõe `GetPestSuppression(tile)` + `GetQualityBonus(tile)` para colheita/futuro-pragas; `RenderedWorld` desenha anéis só dos emissores visíveis (culling existente).
- Constantes: `BeehiveAuraRadius = 5`, `InsectHotelRadius = 3`, `AuraWeakFactor = 0.5f`, `AuraQualityBonus = 0.08f`, `AuraPestSuppression = 0.75f`, `AuraStacking = Max (não soma)`.

### 4.4 UI

- Anel de aura ao passar cursor sobre colmeia/hotel (hover = mostra raio; fora do hover = só partículas sutis — evita poluição; toggle GMCM `ShowAuraRings: hover|always|never`).
- Tooltip em cultivo protegido: `"Protected by beneficials (Bee House)"` + ícone 🐞 (texto, nunca só cor).
- GMCM: `BeneficialsEnabled`, `ShowAuraRings`, `AuraParticles` (bool, default true, desligado por `ReduceMotion` automaticamente).
- Tutorial Elena: `"insects.elena-tip"` — "flores alimentam as abelhas que guardam a horta".

### 4.5 i18n

```json
{
  "insect-hotel.name": "Insect Hotel",
  "insect-hotel.description": "Shelter for ladybugs and lacewings. Pest control in a {0}-tile radius.",
  "beehive.aura.name": "Beneficial aura",
  "beehive.aura.protected": "Protected by beneficials ({0}).",
  "beehive.aura.weak": "Weak aura — plant flowers nearby.",
  "insects.refill": "Refilled with fiber. The hotel buzzes again.",
  "message.insects.no-stack": "Auras don't stack — spread them out."
}
```

### 4.6 Riscos

- Detectar Bee House vanilla de forma robusta: `parentSheetIndex`/ID `(BC)10` + `GetMachineData()` — não `Name.Contains` (localização quebra).
- `NEARBY_FLOWER_ID` é lógica do mel; a aura lê flores por conta própria (varredura de `Object` flor madura no diamante) — custo O(emissores × 60 tiles) no `DayStarted`, cacheado; nunca por frame.
- Sem sistema de pragas, a aura é "bônus invisível" — a spec deve incluir o MVP de praga ou adiar US-11-01 para depois dele (recomendado: MVP junto).

---

## 5. Living Pharmacy — ervas, tônicos e sprays (Epic 12 · US-12-01 / US-12-02)

**Intenção ROADMAP:** aprender com Elena/Linus a cultivar medicinais (Arruda/Rue, Boldo, Camomila) → tônicos (buffs de energia) e Sprays Naturais (pragas sem dano ao solo).

### 5.1 Design

- **US-12-01 (cultivo):** 3 ervas, 3 perfis: **Chamomile** (calmante — energia/regen; fácil, todas as estações menos inverno); **Rue/Arruda** (amarga — base dos sprays; verão/outono); **Boldo** (digestivo — tônico forte; primavera/verão). Colheita `Grab`, sem `Regrow` (replantar = rotaciona — sinergia Cap. 1; família `Flower`/`Herb` → `Unknown`/neutra na rotação, especificar).
- **US-12-02 (receitas):** 2 tônicos (`Data/CookingRecipes`, `IsDrink`, `Buffs`: energia + `MaxStamina/Speed/MagneticRadius` leve) + 2 sprays (`Data/Objects`, `Type: Basic`, aplicar com clique no cultivo = suprime praga 3 dias **sem tocar na saúde do solo** — o anti-pesticida; contraste explícito com futuro "Joja Fertilizer −5%" do ROADMAP).
- Desbloqueio: corações com Elena/Linus (`f Elena 4` / `f Linus 6` — padrão `Recipe_data`) + carta; fallback `s Foraging 5` se NPC indisponível no v1 (especificar um, não os dois).

### 5.2 Sprite

| Asset | Spec |
|---|---|
| Tilesheet `Assets/crops/pharmacy.png` — 3 linhas × 4–5 estágios (camomila com florzinhas brancas/amarelas; arruda verde-azulada; boldo folha larga) | `Texture: "LivingRoots/PharmacyCrops"`, `SpriteIndex` 0/1/2; 16×16 por estágio; semente 16×16 cada (`Assets/seeds/rue_seeds.png` etc. ou tilesheet única de sementes) |
| Frascos 16×16 | `Assets/objects/tonic_chamomile.png` (xícara fumegante), `tonic_boldo.png` (garrafa verde), `spray_rue.png` (borrifador), `spray_neem.png`? (só os 4 da spec: 2+2) — estilo `springobjects`: objeto centralizado, 1px outline, paleta terrosa + `Water #2563EB` p/ spray |
| Partícula de aplicação | reutilizar ponto branco tingido `Water`/`Healthy` — sem asset novo |

### 5.3 Data

```csharp
// Data/Objects — ervas colhidas (comestíveis leves) + produtos
data["LivingRoots.Chamomile"] = new ObjectData { … Type = "Basic", Category = -81 /*forage-like? decidir: -81 = verduras? usar -26 Crafting p/ não virar compostável por engano — CHECAR OrganicWasteValidator: -74/-75/-79/-80/-81 SÃO compostáveis. Ervas DEVEM ser compostáveis (resto) mas o PRODUTO (tônico/spray) NÃO. Regra: matéria-prima Category -81; tônico/spray Category -7 (Cooking) ou -26. Especificar explicitamente. */,
  Edibility = 10, Buffs = [], Price = 40, Texture = "LivingRoots/Pharmacy", SpriteIndex = 0 };
// Tônico: Edibility alta + Buffs (Duration em minutos in-game; -2 = resto do dia)
data["LivingRoots.ChamomileTea"] = new ObjectData { … Type = "Cooking", Category = -7, Edibility = 60, IsDrink = true,
  Buffs = [ { Id = "LivingRoots.calm", Duration = 180, CustomAttributes = { MaxStamina = 40, Speed = 0 } } ],
  Price = 120, … };
// Spray: NÃO comestível, aplica em cultivo
data["LivingRoots.RueSpray"] = new ObjectData { … Type = "Basic", Category = -26, Edibility = -300, Price = 80,
  ContextTags = new() { "LivingRoots.natural_spray" }, … };
// Data/Crops — 3 entradas (Seeds -74; HarvestMethod Grab; CustomFields Family=Herb; Seasons por erva)
// Data/CookingRecipes — tônicos (via cozinha) + sprays? Sprays são CRAFTING (não vão à panela):
recipes["LivingRoots.Chamomile Tea"] = "LivingRoots.Chamomile 3 423 1/Home?/…"; // cozinhar = formato Cooking "ing/par/out/unlock/"
craft["LivingRoots.Rue Spray"] = "LivingRoots.Rue 2 388 5 724 1/Home/LivingRoots.RueSpray 2/false/f Linus 6/";
```

- Aplicação do spray: `IPharmacyService.TryApplySpray(tile)` no mesmo ponto do compost (`ModController` botão-direito, checar `QualifiedItemId` contra **ambas** as formas `(O)LivingRoots.RueSpray` — regra dos dois IDs do `AGENTS.md` raiz). Efeito: `spray_protection_<saveId>` esparso `{tile: expiryDay}` (3 dias),0948032309 `DayStarted` purga; **não altera `soil_health`** (assert de regressão: solo antes == depois).
- Constantes: `SprayProtectionDays = 3`, `TonicEnergy…` (via `Edibility`, sem constante própria), `RueSeason…`, chaves `ModDataSprayKey`.

### 5.4 UI

- Erva madura: tooltip `"Chamomile (medicinal) — harvest"`; cultivo protegido: selo `"Sprayed (2d left)"` + gota `Water`-tingida (cor+texto, nunca só cor).
- Tônico: buff vanilla (`Data/Buffs` ou `CustomAttributes`) = ícone automático no relógio — sem UI custom.
- GMCM: `PharmacyEnabled`, `SprayDays` (1–5, default 3), `ShowSpraySeals`.

### 5.5 i18n

```json
{
  "herb.chamomile.name": "Chamomile",
  "herb.chamomile.description": "Soothing flowers. Brews a calming tea.",
  "herb.rue.name": "Rue",
  "herb.rue.description": "Bitter leaves. Base for natural pest spray.",
  "herb.boldo.name": "Boldo",
  "herb.boldo.description": "Digestive leaves. Brews a hearty tonic.",
  "tonic.chamomile-tea.name": "Chamomile Tea",
  "tonic.chamomile-tea.description": "Warm and calming. Restores energy.",
  "spray.rue-spray.name": "Rue Spray",
  "spray.rue-spray.description": "Keeps pests off crops for 3 days. Harmless to the soil.",
  "spray.applied": "Sprayed. Protected for {0} days — soil untouched.",
  "message.spray.not-crop": "Spray only works on growing crops."
}
```

### 5.6 Riscos

- `Category` das ervas vs. `OrganicWasteValidator`: decidir tabela explícita (matéria-prima compostável × produto não) e travar com teste — é o mesmo anti-loop do compost (`compost-item.md §2.3`).
- `Buffs` com `Duration: -2` (resto do dia) é forte — balancear `Edibility`/preço ou marcar TBD.
- Nomes BR (Boldo) vs. EN: chave i18n única, `default.json` = EN, `pt.json` futuro — não hardcodar "Boldo" em `DisplayName` (usar tokenizável desde o dia 1, ao contrário do compost v0).

---

## 6. Bio-construção — Adobe Shed + Green Roof (Epic 13 · US-13-01 / US-13-02)

**Intenção ROADMAP:** Robin aprende com Elena: **Adobe Shed** (barato, muito Clay, temperatura ideal → artesanais amadurecem mais rápido) e **Green Roof** (upgrade da casa: plantar ervas do Cap. 5 no telhado).

### 6.1 Design

- **Adobe Shed (US-13-01):** shed alternativo: custo deslocado para Clay (`(O)390`→ na verdade Clay é `(O)330`? **checar ID vanilla na spec** — 390 é Stone, 330 é Clay; ROADMAP diz "usa mais Clay"), `BuildCost` ouro menor que Shed vanilla; interior `Shed` vanilla (`IndoorMapType: StardewValley.Shed`); mecânica: `Cask`-like — todo `MachineData` com `CustomData: { AgingMultiplier }` dentro do shed envelhece X% mais rápido (queijos/vinhos). Implementação provável: `DayStarted`, para cada máquina em location-interior de Adobe Shed, avançar `minutesUntilReady` extra (hook em `Object.DayUpdate` via evento SMAPI, sem Harmony se possível — **a spec deve provar sem-Harmony ou justificar Harmony**).
- **Green Roof (US-13-02):** upgrade da FarmHouse (`BuildingToUpgrade: "Farm House"`? checar ID vanilla — Cabin/FarmHouse) que adiciona plantadores no telhado: 6–8 `GardenPot`-equivalentes só para ervas do Cap. 5 (lista allow `LivingRoots.*` herb seeds; negar resto com `DeniedMessage`). Rega: manual (v1) — especificar.
- Desbloqueio narrativo: quest Elena→Robin (mail + evento), `BuildCondition` GSQ (`HasFlag LivingRoots.AdobeLearned`); sem quest no v1 = `s Farming 6 + Year ≥ 2` (GSQ `PLAYER_YEAR`).

### 6.2 Sprite

| Asset | Spec |
|---|---|
| `Assets/buildings/adobe_shed.png` — exterior 7×5? (Shed vanilla é 7×3 + interior próprio; **espelhar dimensões do Shed vanilla** para reaproveitar `CollisionMap`/portas) em adobe (tons `Mulch #92400E` + palha `Sun`) + telhado vegetal parcial | `Texture: "LivingRoots/AdobeShed"`, `SourceRect` cheio, `DrawLayers[]` p/ trepadeira animada? P2. `SeasonOffset` P2. P1 = 1 textura. |
| Telhado verde (upgrade) | `Assets/buildings/green_roof_overlay.png` — `DrawLayers` sobre FarmHouse vanilla? **Não retexturizar a casa inteira**: `Skins[]` ou `DrawLayers` com canteiros 16×16 × 8 no topo + escada. Requer pesquisar `FarmHouse` texture layout na spec (Tiled/TMX). Fallback P1: canteiros como `IndoorItems`/`OutdoorTiles`? Especificar após spike de mapa. |
| Vasos do telhado | reutilizar Garden Pot vanilla (`(BC)62`) como placeholder até asset próprio — YAGNI explícito |

### 6.3 Data

```csharp
// Data/Buildings — Adobe Shed (novo building)
data["LivingRoots.AdobeShed"] = new BuildingData {
  Name = "[i18n]", Description = "[i18n]", Texture = "LivingRoots/AdobeShed",
  Builder = "Robin", BuildCost = 8000 /* < Shed vanilla 15000 */,
  BuildMaterials = [ Clay 300, Stone 50, Fiber 50 ], // checar IDs: Clay (O)330, Stone (O)390, Fiber (O)771
  BuildDays = 2, BuildCondition = "PLAYER_YEAR >= 2, HasFlag LivingRoots.AdobeLearned",
  Size = new(7, 3), CollisionMap = "XXXXXXX\nXOOOOOX\nXXXXXXX" /* espelhar Shed vanilla — confirmar */,
  HumanDoor = …, IndoorMap = "Shed", IndoorMapType = "StardewValley.Shed",
  Metadata = new() { ["LivingRoots.RipeningBonus"] = "0.25" } // lido por C#, sem subclasse
};
// Data/Buildings — Green Roof (upgrade)
data["LivingRoots.GreenRoof"] = new BuildingData {
  BuildingToUpgrade = "Farm House", /* confirmar ID vanilla */
  Builder = "Robin", BuildCost = 15000, BuildMaterials = [ Wood 200, Fiber 100, Clay 100 ],
  BuildDays = 2, /* sem Size/Collision (usa o da casa) */,
  ModData = new() { ["LivingRoots.HasGreenRoof"] = "true" }
};
// Aceleração: sem Data/Machines novo — C# lê Metadata do building pai da location e aplica multiplicador.
```

- Serviço `IBioConstructionService`: `DayStarted` (ripening bonus nos interiores Adobe) + `ButtonsPressed`/`InventoryChanged` (plantio restrito nos canteiros do telhado). Plantio no telhado: interceptar uso de semente em tile de canteiro (`hoeDirt` no mapa do telhado?) — **a spec precisa de um spike de mapa** (onde ficam os tiles plantáveis: interior dedicado? overlay? `FarmHouse` roof não é location plantável por padrão).
- Constantes: `AdobeRipeningBonus = 0.25f`, `AdobeClayCost = 300`, `GreenRoofPlanterCount = 8`, `GreenRoofHerbOnly = true`.

### 6.4 UI

- Interior Adobe: placa/tooltip `"Adobe coolness: ripening +25%"` (ActionTiles na porta interna ou hover nas máquinas — P1: só carta + GMCM).
- Telhado: tooltip por canteiro `"Roof planter — herbs only"`; negar semente não-erva com toast i18n.
- Menu Robin: entradas novas aparecem sozinhas via `Data/Buildings` (`Builder: Robin` + `BuildCondition`) — sem UI custom. `BuildMenuDrawOffset` para enquadrar sprite no catálogo.
- GMCM: `AdobeBonus` (0–50%, default 25%), `GreenRoofHerbOnly` (bool).

### 6.5 i18n

```json
{
  "adobe-shed.name": "Adobe Shed",
  "adobe-shed.description": "Cool clay walls. Artisan goods ripen 25% faster inside.",
  "green-roof.name": "Green Roof",
  "green-roof.description": "A living roof for your house. Grow pharmacy herbs up top.",
  "green-roof.planter.herbs-only": "Roof planters only take pharmacy herbs.",
  "green-roof.quest.elena-letter": "Robin… clay breathes. Let's build cool. — Elena",
  "adobe.ripening": "Ripening {0}% faster in adobe coolness."
}
```
(`Strings/Buildings` é o padrão vanilla para Name/Description — usar tokenizável apontando p/ i18n do mod desde o dia 1.)

### 6.6 Riscos (os maiores do lote — ler antes de especificar)

1. **Acelerar máquinas sem Harmony:** `minutesUntilReady`/`daysUntilReady` são campos de `Object`; avançá-los de fora no `DayStarted` é viável, mas conflita com `OnlyCompleteOvernight`/`PreventTimePass` e com Automate. Spike obrigatório; fallback: bônus só visual? Não — fallback honesto: Adobe dá `+qualidade de output` em vez de velocidade? Decidir na spec, não aqui.
2. **Telhado plantável:** telhado da FarmHouse não é `GameLocation` com `hoeDirt`. Opções: (a) interior `Greenhouse`-like acessado por escada (simples, robusto); (b) tiles reais no mapa do telhado (frágil, exige TMX). **Recomendação deste briefing: (a)** — um "roof interior" `DecoratableLocation` com 8 vasos, acessado pela porta/escada; parece telhado por narrativa + overlay, sem brigar com o mapa vanilla.
3. **IDs vanilla (Clay/Stone/FarmHouse/Shed/Coop/GardenPot):** confirmar todos contra `Data/Objects` + `Data/Buildings` 1.6.15 antes de escrever a spec (tabela de IDs é o artefato mais barato e o erro mais caro).
4. **Arte de buildings é o asset mais pesado:** exterior multi-tile + sombras + `FadeWhenBehind` + catálogo Robin. Orçar artista (ou aceitar placeholder programático tingido, como o compost fez) explicitamente na spec.

---

## 7. Corte transversal: o que especificar em cada spec futura (checklist)

Para cada Epic 8–13, a spec (`/speckit.specify`) deve conter — nada menos:

- [ ] **Tabela de IDs vanilla** usados (semente vs. harvest, `(O)` vs. `(BC)`, Clay/Stone/Fiber/Wood/Coal/MapleSyrup) com fonte (wiki 1.6.x + `oldid`).
- [ ] **Constantes novas em `ModConstants`** listadas (nome + valor default + unidade).
- [ ] **Chaves i18n** (EN completas; `pt.json` P2) + quais `DisplayName/Description` são tokenizáveis desde o dia 1.
- [ ] **Sprites** (dimensões, tilesheet, `SpriteIndex`, fallback sem textura) — artista ou placeholder tingido, explícito.
- [ ] **`OnAssetRequested` edits** (qual asset, qual chave, payload C#) + teste de contrato (chave == constante; `QualifiedItemId` ambas as formas).
- [ ] **Serviço**: interface em `Domain/Interfaces/`, impl em `Services/`, wiring em `ModEntry.cs`, eventos em `ModController` (com `ExecuteWithConcurrencyGuard` onde houver reentrância).
- [ ] **Persistência**: chave `ModData`, formato, esparsidade, expiração/purga, limites `MaxTiles*`.
- [ ] **UI**: GMCM (toggles) vs. custom (só Journal) + tooltip/overlay/hoe-feedback + `ReduceMotion` + contraste + larguras.
- [ ] **Multiplayer**: host-autoritário ou `PerScreen`, comandos `lr_*` se houver config (padrão `lr_overlays` etc.).
- [ ] **Anti-loops**: compostável × produto (tabela Category), aura sem stack, esterco com expiração.
- [ ] **Verificação**: gate unitário (`dotnet test --filter`) + gate in-game (SMAPI sem vermelho, item visível, efeito mensurável) — seguir o padrão `compost-item.md §4`.
- [ ] **Fora de escopo explícito** (YAGNI): o que fica para P2 (talhões auto-detectados, telhado-TMX real, Harmony, economia balanceada).

## 8. Ordem de implementação sugerida (dependências)

```
1. Pragas MVP (pré-req invisível!) ─┐
2. Rotation Journal + famílias ───────┼─→ base de "o que foi plantado onde"
3. Green Manure ──────────────────────┘         (escreve no histórico)
4. Insect Hotels + aura (+ pragas MVP) ──→ usa histórico? não, mas usa overlay + DayStarted
5. Living Pharmacy (sprays usam pragas; ervas usam famílias/Crops)
6. Mobile Coop (usa weeds/pragas + mulch + histórico p/ rotação de pasto)
7. Adobe Shed + Green Roof (usa ervas do Cap. 5; ripening é o spike mais arriscado — por último)
```

`D3 (No in-game content wiring)` e `D1 (multiplayer)` do ROADMAP §Deferred Intents continuam valendo como pré-requisitos transversais: nada deste briefing é demonstrável sem o loop jogável do `specs/004`, e todo estado novo por tile precisa nascer multiplayer-safe.

---

### Fontes (internas verificadas por leitura direta; externas via wiki em 2026-10-05)

- Internas: `ROADMAP.md:42-53`; `LivingRoots/docs/EPICS_USER_STORIES.md:51-89`; `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` §§3/5/8/10–13; `docs/research/remediation-visualization-rendering.md`; `research/xna-monogame-color-rendering-report.md`; `LivingRoots/ModEntry.cs:114-177`; `LivingRoots/Constants.cs`; `LivingRoots/i18n/en.json`; `LivingRoots/LivingRoots.csproj:19-29`; `specs/003-composting-tests/briefings/compost-item.md`; `specs/004-playable-compost-loop/briefings/`.
- Externas: `Modding:Objects` (oldid 193966); `Modding:Big_craftables` (190701); `Modding:Recipe_data` (193377); `Modding:Crop_data` (186411); `Modding:Machines` (191352); `Modding:Buildings` (191928); `Bee_House` (191201); `Modding:Crop_data` (formato `CustomFields`, `CountForMonoculture/Polyculture`); Range Highlight / `Modding:Common_tasks` (GMCM vs `IClickableMenu`); Alvadea/Ace tilesheets + `Tutorial: Adding a New Farm Building` (construção de tilesheets/branches 1.6).
