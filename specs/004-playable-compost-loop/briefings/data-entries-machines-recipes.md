# Briefing — Data entries: `Data/Objects` + `Data/BigCraftables` + `Data/Machines` + `Data/CraftingRecipes` (Compost Loop)

**Data:** 2026-10-05 · **Feature:** `specs/004-playable-compost-loop/` (US1+US2, T013/T022) · **Tipo:** wiring C# via `AssetRequested`, sem Content Patcher
**Status:** pesquisa completa (repo + wiki revisada) · **Ação:** implementar em `LivingRoots/ModEntry.cs:114-177`
**Fontes primárias:** [Modding:Objects](https://stardewvalleywiki.com/Modding:Objects) (rev. 2026-08-08), [Modding:Big craftables](https://stardewvalleywiki.com/Modding:Big_craftables) (rev. 2026-01-12), [Modding:Machines](https://stardewvalleywiki.com/Modding:Machines) (rev. 2026-02-27), [Modding:Recipe data](https://stardewvalleywiki.com/Modding:Recipe_data) (rev. 2026-05-20), [Modding:Items](https://stardewvalleywiki.com/Modding:Items) (rev. 2026-08-04, tabela Categories), [Modding:Context tags](https://stardewvalleywiki.com/Modding:Context_tags) (rev. 2026-06-13).
**Fontes internas:** `ModEntry.cs:114-177`, `Constants.cs:56-79`, `AssetRegistrationTests.cs`, specs/002 `spec.md` (FR-008/FR-018/FR-019 + clarificação hybrid/tags), specs/002 `research.md:127-179` (§7–§8), specs/004 `spec.md` (FR-001/FR-001a/FR-001b/FR-003/FR-005a/FR-006/FR-018/FR-019), `tasks.md:61,86` (T013/T022), `ROADMAP.md:97-110` (D3), briefings irmãos (`i18n-localization.md`, `composting-bin-*.md`, `compost-icon.png.md`).

---

## 1. Estado atual (verificado no repo) e gaps

### 1.1 O que existe hoje em `ModEntry.cs:114-177`

| Asset | Chave registrada | Conteúdo | Veredito |
|---|---|---|---|
| `Data/CraftingRecipes` | `LivingRoots.CompostingBinRecipe` | `388 50 390 25 771 15/Home/LivingRoots.CompostingBin 1/false/null/Composting Bin` | ⚠️ 2 campos errados (ver §4) |
| `Data/CraftingRecipes` | `LivingRoots.Compost` | `771 15/Home/LivingRoots.Compost 1/false/null/Compost` (fibra → compost direto) | ❌ remover — viola FR-006 |
| `Data/Objects` | `LivingRoots.CompostingBin` | `ObjectData` sem `Texture`/`SpriteIndex`, `Type="Crafting"`, `Category=-26` | ❌ asset errado + sem sprite |
| `Data/Objects` | `LivingRoots.Compost` | `ObjectData` sem `Texture`/`SpriteIndex`, `Type="Basic"`, `Category=-26`, `Price=10` | ⚠️ falta sprite; resto OK |
| `Data/Machines` | `(BC)LivingRoots.CompostingBin` | 1 `OutputRule` só com `OutputItem.ItemId`, sem `Triggers`, sem tempo | ❌ produz instantâneo; sem input |
| `Data/BigCraftables` | — | **nenhum handler** | ❌ P0 — máquina `(BC)` sem item `(BC)` |
| Texturas (`Assets/*.png`) | — | 4 PNGs corretos (16×16 compost, 16×32 bins), copiados mas nunca carregados | ❌ unwired (cf. briefing `composting-bin-ready.md` §1.1) |

### 1.2 Gap table (severidade → consequência em jogo)

| # | Gap | Severidade | Consequência se não corrigido |
|---|---|---|---|
| G1 | Sem entrada em `Data/BigCraftables`, mas `Data/Machines` usa chave `(BC)…` | **P0 bloqueante** | `ItemRegistry.Create("(BC)…")` / receita com yield big-craftable resolve para **Error Item** (wiki Items §Error items). Bin não existe como máquina. |
| G2 | Bin registrado em `Data/Objects` como `(O)` | **P0 bloqueante** | Duplo registro `(O)` + `(BC)` do mesmo conceito = dois itens distintos; receita/maquina apontam para tipos diferentes. Mover o bin para `Data/BigCraftables`. |
| G3 | `MachineData` sem `Triggers` e sem `DaysUntilReady`/`MinutesUntilReady` | **P0 bloqueante** | Wiki Machines: "If none are specified, the item will be ready instantly" — compost sai na hora, violando FR-001b/MaturationDays=2. E sem trigger o jogo não sabe quando iniciar a regra. |
| G4 | Receita com flag big-craftable `false` + unlock `null` | **P0 bloqueante** | `false` = yield tratado como `(O)` (Error Item); `null`/`none` = "must be unlocked in some other way, such as an event" (wiki Recipe data §Unlock) — receita **nunca aparece**. FR-018 exige `default` (day-one, cf. spec 002 FR-018 "default unlock condition"). |
| G5 | Receita direta fibra→compost sob a chave `LivingRoots.Compost` | **P1 regra** | Viola FR-006 ("MUST NOT exist through any path that bypasses the bin"). Remover a entrada. |
| G6 | `ObjectData`/`BigCraftableData` sem `Texture`/`SpriteIndex` | **P1 visual** | Default = `Maps/springobjects` índice 0 para `(O)` / `TileSheets/Craftables` índice 0 para `(BC)` — sprite vanilla errado (weeds/stone-like). Apontar para spritesheet do mod via `e.LoadFromModFile`. |
| G7 | `DisplayName`/`Description` hardcoded em inglês | **P2** | Cobrado pelo briefing `i18n-localization.md` §2.1 — usar `helper.Translation.Get(...).Default(...)`; `ModEntry` precisa reter `helper` em campo. |
| G8 | `Category = -26` no compost | **P2 decisão** | -26 = `category_artisan_goods` (wiki Items §Categories), **não** fertilizer (-19). Efeito colateral: bônus da profissão Artisan (+40% no preço de venda, 10→14g) e contagem como artisan good. Ver §7 para decisão recomendada. |

---

## 2. Decisões de design (travadas por spec/wiki)

1. **Quatro assets, quatro papéis.** `(O)LivingRoots.Compost` vive em `Data/Objects`; `(BC)LivingRoots.CompostingBin` vive em `Data/BigCraftables`; o comportamento vive em `Data/Machines` sob a chave **qualificada** `(BC)LivingRoots.CompostingBin` (wiki Machines §Data format: "The key is the qualified item ID"); a obtenção vive em `Data/CraftingRecipes` sob a chave `LivingRoots.CompostingBinRecipe`. Nada de `Data/ObjectInformation` (formato pré-1.6 citado no research 002 §7 — obsoleto).
2. **Bin é big craftable, não object.** Máquinas vanilla (keg, worm bin, crystalarium) são todas `(BC)`. `FR-001a` (máquina nativa) exige o tipo `(BC)`.
3. **Receita: 6 segmentos, flag `true`, unlock `default`.** Formato crafting (wiki Recipe data §Format): `ingredientes / aba(HOME) / yield / bigCraftable? / unlock / displayName`. Comparar com vanilla worm bin: `709 15 336 1 335 1 771 50/Home/154/true/s Fishing 4/`.
4. **Máquina filtra input por `RequiredTags`, não por categoria.** `RequiredTags` casa contra **context tags** do item (wiki Machines §Item processing rules: "An item must have all of the listed tags"). Como é AND dentro de um trigger e OR entre triggers ("The output will apply if any of them match"), o conjunto herdado da spec 002 (FR-003: categorias -74/-75/-79/-80/-81 OU tag `compostable_item`, menos `not_compostable`) vira **6 triggers**, cada um com sua negação `!not_compostable`.
5. **Sem `InvalidItemMessage`.** A clarificação 004 (2026-10-04, FR-008a) determina rejeição nativa silenciosa (shake/cancel cue, sem texto) para drop-in inválido. `InvalidItemMessage` geraria toast — não configurar.
6. **`AllowFairyDust: false`, `DaysUntilReady: 2`, output único `(O)LivingRoots.Compost`.** FR-005a (one in, one out) + clarificação MaturationDays=2. Fairy dust aceleraria a máquina por fora do `CompostingBinService` — desabilitar para manter autoridade única do tempo.
7. **Data entries são multiplayer-neutras por construção** (FR-018): nenhum campo carrega estado de jogador; concorrência (FR-019) vive no lock atômico do controller/service, não nos dados.

---

## 3. `Data/Objects` — compost `(O)LivingRoots.Compost`

Chave do dicionário: **não-qualificada** `LivingRoots.CompostingBin`→ não; para Objects a chave é o ID não-qualificado (wiki Objects §Data format: "The key is the unqualified item ID"): `LivingRoots.Compost`.

| Campo `ObjectData` | Valor | Origem / porquê |
|---|---|---|
| `Name` | `"Compost"` | Nome interno; convenção wiki Items: igualar ao ID quando possível (o sufixo após o ponto). |
| `DisplayName` | tradução `compost.name`, fallback `"Compost"` | Player-visível (FR-005). Tokenizable string; wiring i18n no §6. |
| `Description` | tradução `compost.description`, fallback EN atual | Contém "15"/"Restores 15 soil health" — consistente com `RestorationAmount=15`; ver dívida em `i18n-localization.md` §2.1. |
| `Type` | `"Basic"` | Tipo genérico; evita efeitos colaterais de tipos especiais (cf. wiki: `Arch` quebra artifact spots — nunca usar). |
| `Category` | `ModConstants.CompostCategoryId` (`-26`) | Decisão §7; mantém valor atual até deliberação. |
| `Price` | `10` | Preço de venda; compra não se aplica (Price ≠ shop price). |
| `Texture` | `"LivingRoots/Objects"` | Spritesheet do mod servido via `e.LoadFromModFile` (§6). Nome sob prefixo do mod evita colisão. |
| `SpriteIndex` | `0` | Frame 0 = `Assets/compost.png` (16×16, verificado no briefing `compost-icon.png.md`). |
| `CanBeGivenAsGift` | default `true` | Compostável-presenteável não fere spec; manter default (YAGNI). |
| `CanBeTrashed` | default `true` | Idem. |
| `ContextTags` | `["compost_item"]` (opcional, aditivo) | Marca o **output** para futuras queries (ex.: gift tastes, special orders) sem interferir no filtro de input (que usa `compostable_item`, distinto de propósito). |

C# (dentro do `e.Edit` existente de `Data/Objects` — remover o bloco do bin daqui):

```csharp
data[ModConstants.CompostItemId] = new StardewValley.GameData.Objects.ObjectData
{
    Name = "Compost",
    DisplayName = translation.Get("compost.name").Default("Compost"),
    Description = translation.Get("compost.description")
        .Default("Organic compost rich in microbial life. Restores 15 soil health to tilled tiles."),
    Type = "Basic",
    Category = ModConstants.CompostCategoryId, // -26; ver decisão §7
    Price = 10,
    Texture = "LivingRoots/Objects",
    SpriteIndex = 0,
    ContextTags = new List<string> { "compost_item" },
};
```

---

## 4. `Data/BigCraftables` — bin `(BC)LivingRoots.CompostingBin` (handler novo)

Chave: ID não-qualificado `LivingRoots.CompostingBin` (wiki Big craftables §Data format: "The key is the unqualified item ID"). Tipo C#: `StardewValley.GameData.BigCraftables.BigCraftableData`.

| Campo `BigCraftableData` | Valor | Origem / porquê |
|---|---|---|
| `Name` | `"CompostingBin"` | Sem espaço (interno); display vai em `DisplayName`. |
| `DisplayName` | tradução `composting-bin.name`, fallback `"Composting Bin"` | Player-visível (FR-001). |
| `Description` | tradução `composting-bin.description`, fallback EN atual | Menciona "over 2 days" = `MaturationDays=2` (frágil se a constante mudar — dívida registrada, cf. i18n §2.1). |
| `Price` | `0` | Máquina não vendável por padrão; evita exploração econômica. |
| `Fragility` | `0` | "pick up with any tool" — exigido por FR-005c (pickup/disloge com qualquer ferramenta, sem destruir conteúdo). `1` destruiria com machado; `2` impediria remoção. |
| `CanBePlacedIndoors` | `true` | Greenhouse conta como interior; FR-002 + US1 exigem farm **e** Greenhouse. |
| `CanBePlacedOutdoors` | `true` | Farm. |
| `Texture` | `"LivingRoots/BigCraftables"` | Spritesheet do mod (§6). |
| `SpriteIndex` | `0` | Frame base (empty); `ShowNextIndexWhileWorking/WhenReady` (§5) avançam para frames 1 (processing) e 2 (ready) — exige spritesheet vertical 16×32×3, montado dos 3 PNGs existentes. |
| `IsLamp` | `false` | Sem luz (default; explícito por clareza). |

C# (novo ramo em `OnAssetRequested`):

```csharp
else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, StardewValley.GameData.BigCraftables.BigCraftableData>().Data;
        data[ModConstants.CompostingBinItemId] = new StardewValley.GameData.BigCraftables.BigCraftableData
        {
            Name = "CompostingBin",
            DisplayName = translation.Get("composting-bin.name").Default("Composting Bin"),
            Description = translation.Get("composting-bin.description")
                .Default("Converts organic waste into rich compost over 2 days. Output increases with continuous use."),
            Price = 0,
            Fragility = 0,
            CanBePlacedIndoors = true,
            CanBePlacedOutdoors = true,
            Texture = "LivingRoots/BigCraftables",
            SpriteIndex = 0,
        };
    });
}
```

---

## 5. `Data/CraftingRecipes` — receita do bin (corrigir) + remover receita de compost

### 5.1 Anatomia do formato (wiki Recipe data §Format — crafting)

`segmento0 / segmento1 / segmento2 / segmento3 / segmento4 / segmento5`, split por `/`:

| Índice | Campo | Valor para o bin | Notas |
|---|---|---|---|
| 0 | Ingredients | `388 50 390 25 771 15` | pares `id qtd`: 388 wood×50, 390 stone×25, 771 fiber×15. IDs numéricos vanilla (nunca qualificados aqui). |
| 1 | (Unused) aba | `Home` | "The values in this field don't affect anything, but the field must be present". `Home` = aba de crafting (FR-018 "Home crafting tab"). |
| 2 | Yield | `LivingRoots.CompostingBin` | ID do produto. Com flag `true`, resolvido como `(BC)`. Vanilla usa índice numérico (`154`); mod usa string ID (válido em 1.6). Quantidade omitida = 1. |
| 3 | Big craftable? | **`true`** | Hoje `false` ❌. `true` = yield é big craftable. |
| 4 | Unlock | **`default`** | Hoje `null` ❌ (= `none`: "must be unlocked in some other way"). `default` = aprendida automaticamente; "Any missing default recipes will be learned on day start". Exige FR-018 day-one. Alternativa `s Farming <nível>` trancaria atrás de skill — rejeitada (FR-018 diz "available from day one"). |
| 5 | Display name | `Composting Bin` | Tokenizable; default = display name do produto se omitido. Manter explícito + traduzido via `translation` (nota §6). |

String final:

```text
388 50 390 25 771 15/Home/LivingRoots.CompostingBin/true/default/Composting Bin
```

Custo em constantes (já existem — `Constants.cs:68-71`): `CompostingBinWoodCost=50`, `CompostingBinStoneCost=25`, `CompostingBinFiberCost=15`. Montar via interpolação como hoje, trocando `false/null` → `true/default` e o yield `LivingRoots.CompostingBin 1` → `LivingRoots.CompostingBin` (quantidade 1 é default; manter `1` explícito também é válido — escolher uma forma e travar no teste).

```csharp
data[ModConstants.CompostingBinRecipeId] =
    $"388 {ModConstants.CompostingBinWoodCost} " +
    $"390 {ModConstants.CompostingBinStoneCost} " +
    $"771 {ModConstants.CompostingBinFiberCost}" +
    $"/Home/{ModConstants.CompostingBinItemId}/true/default/" +
    translation.Get("composting-bin.name").Default("Composting Bin");
```

### 5.2 Remover `data[ModConstants.CompostItemId]` de `Data/CraftingRecipes`

A segunda entrada atual (`771 15 → compost`) permite fabricar compost com fibra, sem bin — violação direta de FR-006 ("The compost item MUST be obtainable by the player through the bin and MUST NOT exist through any path that bypasses the bin's processing rules"). **Apagar a linha** `ModEntry.cs:122`. (T022 então passa a cobrir só o registro do objeto compost em `Data/Objects`, não uma receita.)

---

## 6. `Data/Machines` — regra completa do bin

Chave: **qualificada** `(BC)LivingRoots.CompostingBin` (código atual já faz isso — manter). Modelo: `MachineData` → `OutputRules: List<MachineOutputRule>` → `Triggers: List<MachineOutputTrigger>` + `OutputItem: List<MachineItemOutput>`.

### 6.1 Tabela de campos

| Nível | Campo | Valor | Fonte / porquê |
|---|---|---|---|
| `MachineData` | `AllowFairyDust` | `false` | Default vanilla é `true`; fairy dust pularia os 2 dias por fora do service (FR-001b). |
| `MachineData` | `ShowNextIndexWhileWorking` | `true` | FR-019: sprite processing enquanto processa (frame `SpriteIndex+1`). Exige spritesheet de 3 frames (§4). |
| `MachineData` | `ShowNextIndexWhenReady` | `true` | FR-019: sprite ready quando coletável (frame `SpriteIndex+2`). |
| `MachineData` | `WobbleWhileWorking` | `true` (opcional) | Pulsação vanilla durante processamento; cosmético, sem custo lógico. |
| `MachineData` | `InvalidItemMessage` | **omitir** | FR-008a/clarificação: drop-in inválido = shake/cancel cue, sem texto. |
| `OutputRule` | `Id` | `"Default"` | ID único dentro da máquina (manter o atual). |
| `Trigger` ×6 | `Trigger` | `"ItemPlacedInMachine"` (default; pode omitir o campo) | Inicia ao colocar o item (padrão keg/jar). Outros triggers (`MachinePutDown`, `DayUpdate`, `OutputCollected`) são para recarga automática — não se aplicam (input é manual). |
| `Trigger` | `RequiredTags` | ver §6.2 | Filtro herdado spec 002 (FR-003). |
| `Trigger` | `RequiredCount` | omitir (=1) | FR-008/002-spec: exatamente 1 item por interação. |
| `OutputItem` (único) | `ItemId` | `"(O)LivingRoots.Compost"` | Qualificado (já correto no código atual). |
| `OutputItem` | `DaysUntilReady` | `2` (= `ModConstants.MaturationDays`) | Duração nativa; sem este campo o output é instantâneo. Usar `DaysUntilReady`, não `MinutesUntilReady` ("If both days and minutes are specified, days are used"). |
| `OutputItem` | `MinStack`/`MaxStack` | omitir (=1) | FR-005a: exatamente 1. Não configurar sorteio. |

### 6.2 Os 6 triggers `RequiredTags` (coração do FR-003)

Semântica wiki: **AND dentro do trigger, OR entre triggers**; `!` nega. Tags de categoria são automáticas: `category_<nome>` (wiki Context tags §Added automatically + tabela Items §Categories). Mapeamento exato (atenção ao singular/plural da tabela oficial):

| # | `RequiredTags` | Casa com (spec 002 FR-008) |
|---|---|---|
| 1 | `["category_seeds", "!not_compostable"]` | -74 Seeds |
| 2 | `["category_vegetable", "!not_compostable"]` | -75 Vegetables (singular!) |
| 3 | `["category_fruits", "!not_compostable"]` | -79 Fruits |
| 4 | `["category_flowers", "!not_compostable"]` | -80 Flowers |
| 5 | `["category_greens", "!not_compostable"]` | -81 Forage/Greens |
| 6 | `["compostable_item", "!not_compostable"]` | tag custom p/ compat. com mods (research 002 §8) |

Notas de fidelidade:
- `OrganicWasteValidator.cs:27-49` implementa a **mesma** política em C# (exclusão primeiro, `compostable_item`, switch de categoria). A regra nativa e o validador devem concordar item a item; divergência = bug de UX (máquina aceita e service rejeita ou vice-versa). Cobrir com teste de paridade (ver §8, T-NEW).
- `!not_compostable` em **todos** os 6 triggers: a tag de exclusão "MUST be rejected even if they match a category" (spec 002 FR-008). Sem a negação, Ancient Fruit e itens caros seriam aceitos pela máquina.
- Exclusão de artisan goods ("Artisan goods are excluded", spec 002 FR-008): **não** replicada nas tags — artisan goods (categoria -26) simplesmente não casam nenhum trigger positivo, então já são rejeitados por ausência. Nenhum trigger negativo extra necessário (YAGNI). Exceção conhecida: um artisan good que carregue `compostable_item` seria aceito — comportamento correto (tag explícita do autor do mod vence).
- Case: tags custom em minúsculas por convenção wiki; `HasContextTag` é case-insensitive na maioria dos caminhos, mas manter minúsculo nos dois lados.

### 6.3 C# pronto (substitui `ModEntry.cs:152-176`)

```csharp
else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, StardewValley.GameData.Machines.MachineData>().Data;
        var binId = $"(BC){ModConstants.MachineCompostingBinId}";
        data[binId] = new StardewValley.GameData.Machines.MachineData
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
                    Triggers = new List<StardewValley.GameData.Machines.MachineOutputTrigger>
                    {
                        new() { Id = "Seeds", RequiredTags = new List<string> { "category_seeds", "!not_compostable" } },
                        new() { Id = "Vegetables", RequiredTags = new List<string> { "category_vegetable", "!not_compostable" } },
                        new() { Id = "Fruits", RequiredTags = new List<string> { "category_fruits", "!not_compostable" } },
                        new() { Id = "Flowers", RequiredTags = new List<string> { "category_flowers", "!not_compostable" } },
                        new() { Id = "Greens", RequiredTags = new List<string> { "category_greens", "!not_compostable" } },
                        new() { Id = "Modded", RequiredTags = new List<string> { "compostable_item", "!not_compostable" } },
                    },
                    OutputItem = new List<StardewValley.GameData.Machines.MachineItemOutput>
                    {
                        new StardewValley.GameData.Machines.MachineItemOutput
                        {
                            ItemId = ModConstants.QualifiedCompostItemId, // "(O)LivingRoots.Compost"
                            DaysUntilReady = ModConstants.MaturationDays, // 2
                        },
                    },
                },
            },
        };
    });
}
```

> ⚠️ Verificação de compilação obrigatória: os nomes exatos das propriedades (`Triggers`, `RequiredTags`, `DaysUntilReady`, `ShowNextIndexWhileWorking/WhenReady`, `WobbleWhileWorking`, `AllowFairyDust`) seguem a wiki Machines (rev. 2026-02-27, pós-1.6.15). Confirmar contra os assemblies do jogo (`GamePath`, `StardewValley.GameData.Machines` namespace — já importado nos testes) no build; se algum membro mudou de nível (regra vs. output), o compilador acusa e o ajuste é mecânico. `DaysUntilReady`/`UseFirstValidOutput`/`RecalculateOnCollect` documentados sob `OutputItem` — manter nesse nível.

---

## 7. Texturas: `e.LoadFromModFile` (fecha G6)

Padrão SMAPI 4 para spritesheet próprio (espelha o exemplo `Mods/{{ModId}}/Objects` da wiki Items §Define a custom item, traduzido para C#):

```csharp
// Em OnAssetRequested, antes dos ramos e.Edit:
if (e.NameWithoutLocale.IsEquivalentTo("LivingRoots/Objects"))
{
    e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>(
        "Assets/compost.png", StardewModdingAPI.Events.AssetLoadPriority.Medium);
}
else if (e.NameWithoutLocale.IsEquivalentTo("LivingRoots/BigCraftables"))
{
    e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>(
        "Assets/composting_bin_spritesheet.png", StardewModdingAPI.Events.AssetLoadPriority.Medium);
}
```

Implicações de asset (coordenar com briefings de sprite):
- `Assets/compost.png` já é 16×16 frame único → `SpriteIndex: 0` direto, sem trabalho.
- Os 3 PNGs do bin são 16×32 cada; `ShowNextIndex*` exige **frames empilhados verticalmente numa única textura** (`SpriteIndex`, `+1`, `+2`). Gerar `Assets/composting_bin_spritesheet.png` (16×96: empty/processando/ready nessa ordem) a partir dos PNGs existentes — tarefa de montagem, sem nova arte (FR-008a "MUST NOT require new artwork" vale para recusas; aqui é rearranjo dos placeholders já aprovados).
- `helper` precisa estar acessível no handler: hoje `Entry(IModHelper helper)` não o retém. Guardar em campo privado (`_helper`/`_translation`) no composition root — mesma mudança pedida por `i18n-localization.md` §2.1. `e.LoadFromModFile` não precisa do helper, mas `translation.Get` sim.

---

## 8. i18n (resumo — dono é `i18n-localization.md`)

`DisplayName`/`Description` (Objects + BigCraftables) e o segmento 5 da receita usam `translation.Get(chave).Default(fallbackEN)`. Chaves: `compost.name`, `compost.description`, `composting-bin.name`, `composting-bin.description` (`i18n/default.json`, já existem). `Data/Objects` e `Data/BigCraftables` são carregados **por locale** (`e.NameWithoutLocale` já ignora o sufixo) — o `e.Edit` re-executa por idioma e `Translation.Get` resolve no locale corrente, desde que o C# não cacheie a string. Se troca de idioma em runtime não refletir: `helper.GameContent.InvalidateCache("Data/Objects")` (+ `"Data/BigCraftables"`) em `GameLoop.LocaleChanged` — verificar em jogo (§9).

---

## 9. Constantes (`Constants.cs`) — adicionar / manter

| Constante | Valor | Status |
|---|---|---|
| `CompostItemId = "LivingRoots.Compost"` | — | manter (`:63`) |
| `QualifiedCompostItemId = "(O)LivingRoots.Compost"` | — | manter (`:64`); ambas as formas devem ser comparadas em `heldItem.QualifiedItemId` (AGENTS.md raiz). |
| `CompostingBinItemId = "LivingRoots.CompostingBin"` | — | manter (`:65`); passa a ser chave de `Data/BigCraftables` (não-qualificada) e base da chave `(BC)` de Machines. |
| `MachineCompostingBinId` | = `CompostingBinItemId` | manter (`:66`); duplicada hoje — candidata a alias único futuro, fora do escopo. |
| `CompostingBinRecipeId` | — | manter (`:67`). |
| `CompostCraftingTab = "Home"` | — | manter (`:68`); é o segmento 1 (unused/tab). |
| `CompostingBinWoodCost/StoneCost/FiberCost = 50/25/15` | — | manter (`:69-71`); FR-018. |
| `MaturationDays = 2` | — | manter (`:59`); agora referenciado em `DaysUntilReady` (fonte única). |
| `CompostCategoryId = -26` | — | **manter com ressalva** (decisão §7). |
| ` CompostStackMax = 999` | — | informar em doc/teste (stack vanilla default já é 999; sem campo explícito em `ObjectData`). |
| NOVO `CompostBinTexturePath`, `CompostTexturePath` etc. | `"LivingRoots/Objects"`, `"LivingRoots/BigCraftables"` | **adicionar** — zera magic strings de asset em `ModEntry` (convenção "magic numbers em Constants"). |
| NOVO `RequiredTags` por trigger? | 6 listas | **não** adicionar como constantes compostas — `List<string>` mutável como `const` é impossível e `static readonly` vaza mutabilidade; os literais de tag vivem no `OnAssetRequested` com teste travando o conteúdo (§10). Exceção: `"compostable_item"`/`"not_compostable"` já são literais em `OrganicWasteValidator.cs:27,34` — se centralizar, centralizar nos dois lugares juntos (fora do escopo mínimo). |

### Decisão §7 — categoria do compost (-26 vs -19), recomendação explícita

- Fato wiki: `-19` = fertilizer (`category_fertilizer`, "Is Placeable, is always passable"); `-26` = artisan goods (`category_artisan_goods`, profissão Artisan). O research 002 §6 chamou -26 de "fertilizer-like" — **incorreto** contra a tabela oficial atual.
- Porém: categoria -19 (`Is Placeable`) pode fazer o jogo tentar **plantar** o compost como fertilizante vanilla ao clicar no solo, competindo com o `OnButtonPressed` custom (ModController) — risco de duplo-consumo/dupla-aplicação. E a spec 002 (dona do conjunto, FR-003 herdado) fixou -26; trocar é "change to that specification, not to this one" (spec 004 FR-003).
- **Recomendação: manter `-26`** (spec-locked; validador de input não é afetado — ele lê a categoria dos *inputs*, não do compost). Registrar o side-effect Artisan (+40% venda: 10→14g) como conhecido/aceito; revisitar só via emenda à spec 002. Teste trava `Category == CompostCategoryId` (já existe em `AssetRegistrationTests.cs:80`).

---

## 10. Testes (`LivingRoots.Tests/AssetRegistrationTests.cs` — T010/T022)

Infra atual reutilizável: `CreateAssetRequestedEventArgs`, `GetEditOperations`, `ApplyEditOperation<TKey,TValue>` (mock de `IAssetData`). Estender, não reescrever.

| # | Teste | Arquivo | Asserts (contrato travado) |
|---|---|---|---|
| T010a | `CraftingRecipes_RegistersBinRecipeContract` (atualizar) | `AssetRegistrationTests.cs:27-45` | Nova `ExpectedCraftingRecipe = "388 50 390 25 771 15/Home/LivingRoots.CompostingBin/true/default/Composting Bin"` (ou com ` 1` no yield — travar a forma escolhida em §5.1). Split por `/` tem 6 segmentos; `[3]=="true"`; `[4]=="default"`. |
| T010b | `CraftingRecipes_DoesNotRegisterDirectCompostRecipe` (novo, FR-006) | idem | `!recipeDict.ContainsKey(ModConstants.CompostItemId)` após aplicar o edit. |
| T010c | `BigCraftables_RegistersBinDataContract` (novo, G1/G2) | idem | Contém `CompostingBinItemId`; `Name/DisplayName/Description` não-vazios; `Fragility==0`; `CanBePlacedIndoors && CanBePlacedOutdoors`; `Texture=="LivingRoots/BigCraftables"`; `SpriteIndex==0`; `Price>=0`. Padrão segue o teste de Objects (`:47-82`). |
| T010d | `Objects_DoesNotContainBin` (novo, G2) | idem | `!objectsDict.ContainsKey(ModConstants.CompostingBinItemId)` — impede regressão do duplo-registro. |
| T010e | `Objects_CompostHasSpriteWiring` (estender `:73-81`) | idem | `Texture` não-vazia + `SpriteIndex>=0`. |
| T022a | `Machines_FullRuleContract` (estender `:84-111`) | idem | Chave `(BC)+MachineCompostingBinId`; `AllowFairyDust==false`; `OutputRules` 1 regra `Id=="Default"`; `Triggers.Count==6`; cada trigger tem `RequiredTags` contendo `"!not_compostable"`; união dos positivos == `{category_seeds, category_vegetable, category_fruits, category_flowers, category_greens, compostable_item}`; `OutputItem` único com `ItemId==QualifiedCompostItemId` e `DaysUntilReady==MaturationDays (2)`; `ShowNextIndexWhileWorking && ShowNextIndexWhenReady`. |
| T-NEW | `MachineTagsAgreeWithValidator` (novo, paridade) | `AssetRegistrationTests.cs` ou `OrganicWasteValidatorTests` | Para amostra de IDs vanilla por categoria (ex.: 24 Parsnip=cultivo→vegetable? escolher fixtures reais do repo `Fixtures/`) + item com `not_compostable` + item com `compostable_item`: `validator.IsValidOrganicWaste(item) == tagsCasam(item)` (reimplementar o OR-dos-6-triggers no teste via `ItemContextTagManager.DoAllTagsMatch`-like ou string match). Trava G-divergência §6.2. |
| T-IDEM | `AssetEdits_AreIdempotent` (novo) | idem | Aplicar cada edit 2× no mesmo dict: sem throw, contagem de chaves estável (SMAPI pode re-executar edits por locale/reload). |
| Existente | `UnrelatedAsset_DoesNotRegisterAnyEdits` (`:113-126`) | idem | Manter; adicionar `Data/BigCraftables`-typo (`"Data/BigCraftable"`) à parametrização se virar `[Theory]`. |

Notas de implementação dos testes:
- `ApplyEditOperation` é genérico — para BigCraftables instanciar `Dictionary<string, BigCraftableData>` (`StardewValley.GameData.BigCraftables`).
- Teste de `e.LoadFromModFile` (texturas §7): `AssetRequestedEventArgs` expõe load operations separadas de edits — verificar no tipo SMAPI referenciado como `EditOperations` foi verificado (`:173-177`); se `LoadFromModFile` enfileirar em coleção distinta, espelhar o helper `GetEditOperations` para ela. Se a API não for mock-friendly, cobrir com assert de integração mínima (chamar `OnAssetRequested` com nome `LivingRoots/Objects` e assert que não-throw + operação enfileirada).
- `InternalsVisibleTo("LivingRoots.Tests")` já expõe `OnAssetRequested` (internal) — sem mudança de visibilidade.

---

## 11. Verificação in-game (gate comportamental — `dotnet test` não cobre data)

1. `list_items LivingRoots` no console SMAPI → `(O)LivingRoots.Compost` e `(BC)LivingRoots.CompostingBin` existem (não-Error Item); `debug listtags` com compost na mão mostra `compost_item` + `category_artisan_goods` (documenta §7).
2. Crafting menu dia 1 → receita da composteira visível (unlock `default`); craft consome 50/25/15 exatos; inventário recebe `(BC)` colocável.
3. Place na farm + Greenhouse (indoors) OK; pickup com qualquer ferramenta preserva item (Fragility 0).
4. Drop-in: item -74/-75/-79/-80/-81 entra e inicia (som vanilla de load); item inválido (ex.: 388 wood, artisan good sem tag) = shake + `cancel` sem texto; item com `not_compostable` rejeitado mesmo se da categoria.
5. Dormir 2 dias → estado ready (sprite frame +2, wobble cessa); coleta entrega exatamente 1 compost; inventário cheio = compost permanece (FR-005b, lógica do service — fora deste briefing).
6. Troca de idioma EN↔PT (com `pt.json` do briefing i18n) → nome/descrição do bin e compost acompanham; se não: aplicar `InvalidateCache` (§8).
7. Save/reload com bin em cada estado → sem Error Item, sem duplicata (tolerant reader FR-012 é do service; aqui só garantir que IDs são estáveis entre loads).

---

## 12. Riscos e armadilhas (não reintroduzir)

1. **Chave `(BC)` sem item `(BC)`** = Error Item silencioso (G1). Todo rename de `CompostingBinItemId` tem que tocar `Data/BigCraftables` + `Data/Machines` + receita juntos — grep pelos 3 antes de commitar.
2. **`null` no unlock** = receita fantasma (G4). Revisores: qualquer string de receita terminada em `/null/` ou `/none/` sem evento que a destrave é bug salvo prova em contrário.
3. **Tempo instantâneo**: `MachineData` sem `DaysUntilReady`/`MinutesUntilReady` produz na hora (wiki). Revisores: regra sem tempo = suspeita imediata.
4. **`category_vegetable` é singular**; os demais seguem a tabela oficial (§6.2). Copiar da wiki, não da intuição.
5. **Receita direta de compost** (G5/FR-006): nenhum caminho alternativo de obtenção pode nascer em `Data/CraftingRecipes`, `Data/CookingRecipes` ou shops sem emenda à spec.
6. **Sprites**: `ShowNextIndex*` com spritesheet de 1 frame lê lixo do atlas. Só habilitar §6.1 junto da entrega do spritesheet 16×96 (§7).
7. **Locale**: nunca cachear `Translation.Get` em campo estático (i18n §2.1); `dotnet format` não valida JSON nem data strings — o gate é T010/T022 + §11.

---

## 13. Out of scope (explicitamente NÃO neste briefing)

- Lógica de runtime (service/controller, modData, tolerant reader, locks FR-019) — briefings de US2/US3 e tasks T023-T026/T037.
- Sons de máquina (`LoadEffects`/`WorkingEffects`, `WorkingEffectChance`) — briefing `audio-processing.md`/`audio-ready.md` é dono; `WobbleWhileWorking` aqui é o mínimo visual.
- `ExperienceGainOnHarvest` (ex.: `"Farming 5"`), `StatsToIncrementWhen*` — YAGNI até a spec pedir.
- Gift tastes, shipping collection, museum — só via `ContextTags` aditivas futuras; nada agora além de `compost_item`.
- Classe tipada de tradução / paridade JSON em CI — dono `i18n-localization.md` §3.4/§6.

---

*Última verificação: código em `ModEntry.cs:114-177`, `Constants.cs:56-79`, `AssetRegistrationTests.cs:1-200`; specs 002 (`spec.md` FR-008/018/019, `research.md` §7–§8) e 004 (`spec.md` FR-001–008a/018/019, `tasks.md` T013/T022); ROADMAP D3; wiki revs. 2026-01 a 2026-08 citadas no cabeçalho. Verificação em jogo (§11) e confirmação dos membros `GameData.Machines/*` no build com `GamePath` pendentes — marcadas como verificação, não assumidas.*
