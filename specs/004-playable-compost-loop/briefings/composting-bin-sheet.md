# Briefing: `BigCraftables composting bin sheet` (empty / processing / ready — folha consolidada)

**Feature:** `specs/004-playable-compost-loop` (playable compost loop, máquina nativa, maturação 2 dias)
**Assets-fonte:** `LivingRoots/Assets/composting_bin_empty.png`, `composting_bin_processing.png`, `composting_bin_ready.png` (16×32 RGBA cada, verificados via `file` em 2026-10-05)
**Asset-alvo:** `LivingRoots/Assets/composting_bin_sheet.png` (a criar — 16×96 vertical **ou** 48×32 horizontal; 96×32 animado como extensão)
**Status:** 3 PNGs existem, tamanho correto, **não wired** — nenhum C# os carrega; `Data/Machines` é esqueleto; `Data/BigCraftables` não existe
**Consolida e supersede em escopo de sheet:** `composting-bin-empty.md`, `composting-bin-processing.md`, `composting-bin-ready.md` (os 3 permanecem como referência de arte por estado; este arquivo é o contrato único de folha + wiring)
**Data:** 2026-10-05 | **Game:** Stardew 1.6 | **API:** SMAPI 4.0 (`MinimumApiVersion 4.0.0` em `manifest.json`) | **TFM:** net6.0
**Decisões vigentes da 004:** máquina nativa sem UI própria (FR-001a), 2 dias fixos sem acelerador (FR-001b), um-dentro-um-fora exatamente 1 compost (FR-005a), progresso em `Item.modData` sobrevive a pickup (FR-005c), rejeição vanilla sem texto próprio (FR-008a)

---

## 1. Estado atual (verificado no repo — nada presumido)

### 1.1 Arquivos: existem, tamanho certo, unwired

| Arquivo | `file` real | Padrão vanilla | Situação |
|---|---|---|---|
| `LivingRoots/Assets/composting_bin_empty.png` | 16×32 RGBA 8-bit | BigCraftable = 16×32 | OK de tamanho, **unwired** |
| `LivingRoots/Assets/composting_bin_processing.png` | 16×32 RGBA 8-bit | BigCraftable = 16×32 | OK de tamanho, **unwired** |
| `LivingRoots/Assets/composting_bin_ready.png` | 16×32 RGBA 8-bit | BigCraftable = 16×32 | OK de tamanho, **unwired** |
| `LivingRoots/Assets/compost.png` | 16×16 RGBA 8-bit | Object `(O)` = 16×16 | OK, fora do escopo (briefing `compost-icon.png.md`) |
| `LivingRoots/Assets/composting_bin_sheet.png` | **não existe** | 16×96 ou 48×32 | **a criar a partir dos 3** |

- `.csproj:26-28` copia `Assets/**/*.*` para o output (`PreserveNewest`) — copiar ≠ wirar.
- Grep por `Texture2D.*Assets|LoadFromModFile|ModContent.Load|PatchImage|AsImage|SpriteIndex` em `LivingRoots/` (fora renderer de overlay de solo, que cria textura branca 1×1 procedural) retorna **zero** para esses PNGs. Confirmado nos 3 briefings irmãos e revalidado em 2026-10-05.
- `manifest.json`: `UniqueID = lfgranja.LivingRoots`, sem dependência de Content Patcher nem Alternative Textures. Spike da 004 manda registro 100% programático via `AssetRequested` em C#.

### 1.2 Anatomia pixel a pixel (medição real, briefing processing §1.2 — reutilizada aqui como contrato)

Leitura Pillow dos 3 PNGs (16 col × 32 lin; `.` = transparente):

- **Base comum, linhas 08–30, BIT-IDÊNTICA nos 3 arquivos:** caixa de madeira com ripas — moldura externa escura `rgb(43,29,14)`, corpo `rgb(133,87,44)`, destaques `rgb(168,114,62)` / `rgb(92,58,30)`, tampa abaulada nas linhas 08–12. O artista reutilizou a base — **é exatamente o invariante que a folha precisa preservar** (§4.3).
- **Empty (270 px opacos, 6 cores):** interior das ripas vazio/escuro, sem conteúdo visível.
- **Processing (273 px opacos, 12 cores — o mais rico):** montinho verde visível através das frestas (`rgb(85,139,47)` ×17 px + `rgb(124,179,66)` ×7 px) nas linhas 14–18 e 23–26, **mais 3 pontos de vapor** acima da tampa (linhas 04–06, coluna ~8). Único com algo acima da silhueta — boa intuição de calor/vapor, mas em frame único estático.
- **Ready (274 px opacos, 10 cores):** conteúdo interno mais escuro e "curtido" (`rgb(78,52,30)` / `rgb(55,36,20)`), **mais 4 px dourados de faísca** (`rgb(255,214,0)` ×4 + `rgb(255,235,59)` ×2) nas linhas 04–06.
- **Margens:** linha 31 e linhas 00–03 (exceto vapor/faísca em 04–06) totalmente transparentes — correto, evita sangramento no tile vizinho quando desenhado a 4× (64×128 px na tela).

Limitações reais herdadas (não regredir na folha):

1. Frame único por estado — qualquer `WorkingEffects.Frames` com >1 offset exige a folha única (§3).
2. Vapor de 3 px não anima sozinho — o motor só anima com `Frames` (troca de sprite) ou `TemporarySprites` (partículas sobrepostas).
3. Verde do heap (`rgb(85,139,47)`) destoa da paleta declarada `CompostProcessingColor #A16207` (ocre) em `Constants.cs:49` — pertenceria à família `CompostReadyColor #166534`. Não é erro funcional (sprites ≠ overlays), mas a direção de arte do §5 alinha.

### 1.3 Registro atual (`ModEntry.cs:115-176`) — esqueleto mínimo com 2 bugs bloqueantes

```csharp
// Data/CraftingRecipes (ModEntry.cs:121) — BUG 1: flag big-craftable
data[CompostingBinRecipeId] = $"388 50 390 25 771 15/Home/LivingRoots.CompostingBin 1/false/null/Composting Bin";
//                                                                                  ^^^^^ deve ser true (§2.2)

// Data/Objects (ModEntry.cs:126-150) — BUG 2: catálogo errado
data[CompostingBinItemId] = new ObjectData { Name="Composting Bin", Type="Crafting", Category=-26, Price=0 };
// bin placeable 2-tiles NÃO vive em Data/Objects (§2.1)

// Data/Machines (ModEntry.cs:152-174) — esqueleto: só OutputRules → OutputItem, sem timing/triggers/sprites
data["(BC)LivingRoots.CompostingBin"] = new MachineData
{
    OutputRules = [ new() { Id="Default",
        OutputItem = [ new() { ItemId="(O)LivingRoots.Compost" } ] } ]
};
```

Faltando (todos opcionais no schema, obrigatórios para o loop jogável): `Triggers` (`ItemPlacedInMachine` + `RequiredTags` herdadas da spec 002 FR-003), `DaysUntilReady = 2` (`Constants.cs:59` `MaturationDays`; dias vencem minutos se coexistirem), `ShowNextIndexWhileWorking/WhenReady`, `IncrementMachineParentSheetIndex`, `WorkingEffects`/`LoadEffects`/`WorkingEffectChance`, `InvalidItemMessage`, `AllowFairyDust = false` explícito (default `true` quebraria FR-001b), `WobbleWhileWorking` (decisão pós-playtest).

### 1.4 Constantes e modelo de estados (base pronta)

- `Constants.cs:56` `CompostingBinKeyPrefix`, `:59` `MaturationDays = 2`, `:63-66` `CompostItemId / QualifiedCompostItemId = (O)LivingRoots.Compost / CompostingBinItemId / MachineCompostingBinId = LivingRoots.CompostingBin`, `:67-71` receita (50 wood / 25 stone / 15 fiber, tab `Home`), `:72-74` som `axe`, categoria `-26`, stack 999, `:77-78` chaves modData `LivingRoots.InputId / LivingRoots.DaysRemaining`.
- `CompostingBinState`: `Empty=0, Processing=1, Ready=2` (3 estados lógicos ↔ 3 frames da folha).
- `CompostingBinStateModel.PopulateFromModData / WriteToModData` + `CompostingBinService` (cache, stack-1 com progresso, dislodge com `createItemDebris`) — persistência existe; falta a camada conteúdo→motor.
- `specs/002-soil-decay-compost/data-model.md:99-115` (`CompostingBinObject`: `assets/composting_bin.png`, 16×32, som `axe`) e `:118-129` (`OrganicWasteDefinition`: categorias `[-74,-75,-79,-80,-81]`, tags `compostable_item` / `not_compostable`) — **referenciar, nunca recopiar listas** (dono: spec 002, FR-003). Nota: o modelo 002 ainda descreve multiplicador de maturação 1–5 (`data-model.md:59-77`); a 004 o aposentou (FR-005a one-in-one-out) mantendo só leitura tolerante de saves legados.
- `ROADMAP.md D3`: o mod registra receita mas sem `Data/Objects` handler na época da redação — hoje o handler existe mas no catálogo errado; fechar este sheet + `Data/BigCraftables` + `Data/Machines` completo **é** fechar D3 na prática.

---

## 2. Fundamentos externos (fontes primárias, fetch 2026-10-05/06)

### 2.1 BigCraftables — o bin precisa ser `(BC)`, não `(O)`

- [`Modding:Big_craftables`](https://wiki.stardewvalley.net/Modding:Big_craftables): big-craftables são itens placeáveis de **2 tiles de altura** (vs. 1 tile de objects), tipo `(BC)`, dados em **`Data/BigCraftables`**, sprites default em `TileSheets/Craftables`, código em `StardewValley.Object` via campo `bigCraftable`. Campos: `Name`, `DisplayName`/`Description` (tokenizable), `Price` (default 0), `Fragility` 0/1/2 (default 0 = recolhe com qualquer ferramenta), `CanBePlacedIndoors/Outdoors` (default true), **`Texture`** (default `TileSheets/Craftables`), **`SpriteIndex`** (0 = top-left), `IsLamp` (default false), `ContextTags`, `CustomFields`.
- Consequência: **mover o registro do bin de `Data/Objects` para `Data/BigCraftables`** (`BigCraftableData`), manter **só** o compost `(O)LivingRoots.Compost` em `Data/Objects` (`Type = Basic`, ícone 16×16).
- Chave `Data/Machines` = qualified ID da máquina — `(BC)LivingRoots.CompostingBin` — **idêntica** à chave `Data/BigCraftables` sem o prefixo. Tutorial oficial: *"The key of Data/Machines must be identical to Data/BigCraftables, but with the (BC) identifier added"*.

### 2.2 Receita — flag big-craftable `true`

- [`Modding:Recipe_data`](https://wiki.stardewvalley.net/Modding:Recipe_data): formato `ingredientes/tab/yield/isBigCraftable/unlock/displayName`. Vanilla: `"Worm Bin":"709 15 336 1 335 1 771 50/Home/154/true/s Fishing 4/"`, `"Chest":"388 50/Home/130/true/default/"`. O `true` na 4ª posição é obrigatório para produto big-craftable; yield usa ID **não-qualificado** (`LivingRoots.CompostingBin`, quantidade 1 pode ser omitida). A string atual (`.../LivingRoots.CompostingBin 1/false/...`) produz um `(O)` que nunca casa com a chave `(BC)`.

### 2.3 Machines — tempo, triggers, sprites, efeitos, luz (schema completo)

Fonte: [`Modding:Machines`](https://wiki.stardewvalley.net/Modding:Machines) (rev. 2026-02-27):

| Campo | Semântica relevante ao bin |
|---|---|
| `OutputRules[].Triggers[]` | qualquer trigger casando seleciona a regra; **primeira regra disponível de cima para baixo vence**. `ItemPlacedInMachine` (default, nosso caso) / `OutputCollected` (crystalarium recarrega; fora de escopo — output terminal) / `MachinePutDown` (worm bin auto-inicia; candidato a fallback) / `DayUpdate` (soda machine; alternativa passiva que removeria o gesto de alimentar — contraria US2). `RequiredTags` (todas devem casar; `!` nega), `RequiredItemId`, `RequiredCount` (default 1), `Condition` (game-state query) |
| `OutputItem[]` | `ItemId` (+ `MinStack/MaxStack`), tokens `DROP_IN_ID`, `FLAVORED_ITEM Wine DROP_IN_ID`; **fora de escopo aqui** (output fixo 1×, sem `CopyColor/Price/Quality`, sem `PreserveId: DROP_IN` — briefing processing §8). **`IncrementMachineParentSheetIndex`**: soma N ao índice **durante processamento E quando pronto**; acumula com `ShowNextIndex*` (default 0) |
| `MinutesUntilReady / DaysUntilReady` | **se ambos, dias vencem**; se nenhum, output **instantâneo** (caso atual do mod). Alvo: `"DaysUntilReady": 2` |
| `AllowFairyDust` (default true) | setar **`false`** explícito — fairy dust encurtaria os 2 dias fixos (FR-001b). `ReadyTimeModifiers` ausente; `RecalculateOnCollect` ausente/`false` (output determinístico) |
| `AllowLoadWhenFull` (default false) | manter `false` = no máximo um batch (spec 004) |
| `OnlyCompleteOvernight` (default false) | manter; `PreventTimePass` ausente (bin conta dentro/fora, toda estação) |
| `ShowNextIndexWhileWorking / ShowNextIndexWhenReady` (top-level, default false) | exibe **`SpriteIndex + 1`** enquanto processa / quando pronto |
| `LoadEffects / WorkingEffects` | `{ Condition, Sounds[] {Id cue + Delay ms}, ShakeDuration ms, Frames[] (offsets relativos à base), Interval ms (default 100), TemporarySprites[] {Id, Texture, SourceRect{X,Y,W,H}, PositionOffset{X,Y}, Color, Scale, Interval, Loops, Flicker…} }`. `WorkingEffectChance` default **0.33** (probabilidade de re-disparar a cada tick/amanhecer) — fixar `1` para animação contínua |
| `LightWhileWorking {Radius, Color}` | omitir — compostagem não brilha; luz quente leria "fornalha". **Não há `LightWhenReady` no schema** — glow de ready, se algum, é pintado no sprite ou `TemporarySprites` no collect |
| `WobbleWhileWorking` (default **false** — corrigindo o briefing ready §3.1 que dizia "default") | pulsar ligado/desligado; decidir no playtest |
| `InvalidItemMessage (+Condition)` / `InvalidCountMessage (+[ItemCount])` | toast vanilla; canal correto para "não compostável" (FR-008a: sem `showRedMessage` aqui — HUD vermelha reserva-se a aplicação em tile, inventário cheio, colisão multiplayer) |
| `OutputMethod / InteractMethod` (delegates C#) | **só** se o spike provar que `DaysUntilReady: 2` + modData não coexistem. `probe=true` nunca muta nem toca som/animação |
| Leitura C# | `Object.readyForHarvest`, `heldObject`, `minutesUntilReady`, `showNextIndex`, `lastOutputRuleId`, `lastInputItem`, `IsMachineWorking()`, `PlaceInMachine()`, `OutputMachine()`, `GetMachineData()`, classe `MachineDataUtility` (probe sem mutar — ideal para validação antecipada) |

### 2.4 Tutorial oficial — a receita canônica de 3 sprites (fonte normativa deste sheet)

[`Tutorial: Adding New Machines`](https://stardewmodding.wiki.gg/wiki/Tutorial:_Adding_New_Machines_(Or_New_Rules_To_Existing_Machines)) (últ. ed. 2026-06-21), §§ *Load its sprite*, *Show next sprite when processing or ready*, *Show different sprites for idle, processing and ready*, *Animate a machine while working*:

- BC = **16×32** (1×2 tiles de 16 px); múltiplas máquinas na mesma folha em retângulos 1×2, **índice 0 = primeiro sprite**, depois 1, 2… Asset custom segue convenção `Mods/<UniqueID>/<Nome>` servido por patch `Load` + entrada `Data/BigCraftables` com `Texture` + `SpriteIndex`.
- Chave `Data/Machines` = chave `Data/BigCraftables` + prefixo `(BC)`.
- **3 sprites distintos (idle/working/ready) = `IncrementMachineParentSheetIndex: 1` no output (0→1 ao processar E ao aprontar) + `ShowNextIndexWhenReady: true` (1→2 só quando pronto).** Exemplo StoneMill do tutorial é o modelo literal (§6.2).
- Animação contínua = `WorkingEffects.Frames` (offsets relativos) + `Interval` + **`WorkingEffectChance: 1`** (default 0.33 = intermitente). Exemplo: `Frames [2,3,4,5]`, `Interval 80`; som recomenda-se só em `LoadEffects` (som em working com chance 1 toca a cada tick — irritante).
- Ordem de regras importa (primeira que casa vence); `TargetField`/`MoveEntries` para edits cirúrgicos (relevante se um dia estendermos máquinas vanilla — não agora).

### 2.5 Content API SMAPI — wiring sem Content Patcher

[`Modding:Modder Guide/APIs/Content`](https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Content): o equivalente exato do `Load/FromFile` do CP em C# é **`e.LoadFromModFile<T>` + `e.Edit(...)`** dentro de `AssetRequested` (spike 004: SMAPI puro; CP só como fallback). Regras: wiring é declarativo (evento dispara a cada load), `e.NameWithoutLocale` para `Data/*` não-localizados (padrão já usado no `ModEntry`), nunca `ModContent.Load` dentro de draw (custo GPU — carregar uma vez e reutilizar), sem `InvalidateCache` para asset estático, path **case-sensitive no Linux** (`Assets/...`).

### 2.6 Dia/noite sem swap nativo + IsLamp

- Não existe troca sazonal/diurna de sprite para `Data/Machines` (seasonal switching `fall_`/`spring_` é de tilesheets de mapa; variantes sazonais do bin estão out-of-scope nos 3 briefings).
- Sprites de mundo passam por passe de luz ambiente (noite/chuva/inverno escurecem) — cores muito escuras somem; é para isso que `LightWhileWorking` existe. Para a composteira: **omitir luz**; se o playtest noturno pedir legibilidade, preferir `TemporarySprites` de brilho suave a luz real (luz altera gameplay — atrai atenção como poste). `IsLamp` em `Data/BigCraftables` default `false` — manter `false`.
- Nunca diferenciar estados só por matiz (daltonismo ~8% homens; noite dessatura tudo). Gramática do trio atual já acerta: vazio = ausência, processando = textura orgânica + vapor, pronto = pontos luminosos + forma (tampa/montinho/sparkle). Manter.

---

## 3. Spec técnica da folha (o contrato único)

### 3.1 Formato e layouts aceitos

- **Célula: 16×32 px, PNG RGBA 8-bit, fundo 100% transparente fora da silhueta.** Nada de JPG, nada de sombra de chão assada (máquinas têm sombra própria; sombra pintada duplica/escurece), nada de interlace esquisito.
- **Ordem de índices: 0 = empty (idle/base), 1 = processing (working), 2 = ready.** `SpriteIndex` conta da esquerda para a direita, de cima para baixo, a partir de 0 no top-left da `Texture`.
- **Layout A — vertical 16×96 (RECOMENDADO):** empty no topo, processing no meio, ready embaixo. Vantagem: mesma largura de 1 célula (edição/diff por linhas, `PatchImage` parcial simples, leitura visual "empilha estados").
- **Layout B — horizontal 48×32 (ACEITO):** mesma ordem 0/1/2 da esquerda para a direita. Equivalente funcional; preferir A salvo ferramenta que exija faixa horizontal.
- **Layout C — animado 96×32 (EXTENSÃO, só após A validado):** `| 0 empty | 1 proc-A (vapor esq) | 2 ready | 3 proc-B (vapor centro) | 4 proc-C (vapor dir) | 5 proc-A' (dissipado) |` com `WorkingEffects.Frames: [1,3,4,5]`, `Interval: 400–600` (vapor lento — processo lento → animação lenta; 80–100 ms é para moinho, agitado demais para compostagem). Por que 3–4 frames e não 2 ou 8: 2 = pisca binário (parece bug); 8+ = custo de arte sem ganho a 4×. `ready` permanece célula única (estado de espera; o motor já adiciona bolha/partículas).
- PixelZoom: o jogo desenha a ~4× (64×128 px na tela) — pintar com zoom 800%, revisar a 400%; detalhes < 2 px de sprite somem.

### 3.2 Como os 3 estados mapeiam para os flags nativos (tabela normativa)

| Estado lógico | `Object` nativo | Índice exibido (folha A/B) | Coleta / interação |
|---|---|---|---|
| Empty = 0 | `heldObject == null`, `readyForHarvest = false` | **0** (base `SpriteIndex = 0`) | `PlaceInMachine` aceita waste; inválido → `cancel` + shake, nada consumido |
| Processing = 1 | `heldObject` = derivado do waste, timer > 0, `showNextIndex` | **0 + 1 (`IncrementMachineParentSheetIndex`) = 1** | interagir → nada (default); `WorkingEffects` ocasionais/contínuos |
| Ready = 2 | `heldObject` = compost, **`readyForHarvest = true`** | **1 + 1 (`ShowNextIndexWhenReady`) = 2** + **bolha do compost quicando acima** (canal vanilla mais forte, vem de graça) | right-click → 1 compost, `heldObject = null`, volta a 0 |

Fórmula: `IncrementMachineParentSheetIndex = 1` no `OutputItem` (0→1 ao processar **e** ao aprontar) + `ShowNextIndexWhenReady = true` no `MachineData` (1→2 só quando pronto). **Não** setar `ShowNextIndexWhileWorking` junto (redundante com o incremento — causaria 0→2 durante working e pularia o frame processing; se algum dia a folha C for usada com `Frames`, os offsets já cobrem o working).
**Spike explícito (herdado dos 3 briefings):** confirmar em jogo que a combinação produz 0/1/2; se o motor divergir, registrar o comportamento real e ajustar este briefing — não afirmar como garantido até o teste §7 passar.

### 3.3 `Data/BigCraftables` — a entrada que falta (wiring `Mods/lfgranja.LivingRoots/BigCraftables`)

```csharp
// 1. Servir a textura (uma vez por asset; declarativo — SMAPI chama o evento a cada load)
if (e.NameWithoutLocale.IsEquivalentTo("Mods/lfgranja.LivingRoots/BigCraftables"))
{
    e.LoadFromModFile<Texture2D>("Assets/composting_bin_sheet.png", AssetLoadPriority.Medium);
}

// 2. Registrar o BigCraftable (mover do Data/Objects — remover a entrada de lá)
else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, StardewValley.GameData.BigCraftables.BigCraftableData>().Data;
        data[ModConstants.MachineCompostingBinId] = new StardewValley.GameData.BigCraftables.BigCraftableData
        {
            Name = "Composting Bin",
            DisplayName = "Composting Bin",   // depois: token i18n (seguir briefing i18n-localization.md)
            Description = "Turns organic waste into rich compost in 2 days. 1 waste in, 1 compost out.",
            Price = 0,
            Fragility = 0,                    // recolhível com qualquer ferramenta (FR-005c exige pickup)
            CanBePlacedIndoors = true,
            CanBePlacedOutdoors = true,
            IsLamp = false,                   // sem luz — compostagem não brilha (§2.6)
            Texture = "Mods/lfgranja.LivingRoots/BigCraftables",
            SpriteIndex = 0,                  // 0=empty; 1/2 via §3.2
        };
    });
}
```

Notas: `UniqueID` real `lfgranja.LivingRoots` (de `manifest.json`); manter os 3 PNGs individuais como **fonte** (`Assets/` raiz) e gerar `composting_bin_sheet.png` a partir deles (script ou camada do editor) — nunca editar a folha final à mão sem atualizar as fontes; nomes de arquivo case-sensitive no Linux.

### 3.4 `Data/Machines` — regra completa (substitui o esqueleto `ModEntry.cs:152-174`)

```csharp
else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, StardewValley.GameData.Machines.MachineData>().Data;
        var binId = $"(BC){ModConstants.MachineCompostingBinId}";
        data[binId] = new StardewValley.GameData.Machines.MachineData
        {
            OutputRules = new List<MachineOutputRule>
            {
                new MachineOutputRule
                {
                    Id = "LivingRoots_CompostBatch",
                    Triggers = new List<MachineOutputTrigger>
                    {
                        new MachineOutputTrigger
                        {
                            Id = "LivingRoots_OrganicInput",
                            Trigger = MachineOutputTriggerRule.ItemPlacedInMachine,
                            RequiredTags = new List<string> { /* herdado spec 002 FR-003 — NÃO restatar aqui */ },
                            RequiredCount = 1,
                        },
                    },
                    OutputItem = new List<MachineItemOutput>
                    {
                        new MachineItemOutput
                        {
                            Id = "LivingRoots_CompostOutput",
                            ItemId = ModConstants.QualifiedCompostItemId, // "(O)LivingRoots.Compost"
                            MinStack = 1,
                            MaxStack = 1,
                            IncrementMachineParentSheetIndex = 1,          // 0→1 processando e pronto
                        },
                    },
                    DaysUntilReady = ModConstants.MaturationDays,          // 2 (FR-001b; dias vencem minutos)
                },
            },
            ShowNextIndexWhenReady = true,                                 // 1→2 só quando pronto (§3.2)
            WorkingEffectChance = 1.0f,                                    // vapor contínuo (default 0.33 = intermitente)
            WorkingEffects = new List<MachineEffects>
            {
                new MachineEffects
                {
                    // Folha A/B (3 células): Frames = [1] ou omitir + TemporarySprites vanilla de fumaça.
                    // Folha C (6 células): Frames = [1, 3, 4, 5], Interval = 500.
                    Frames = new List<int> { 1 },
                    Interval = 500,
                    // Sounds: AUSENTE de propósito — som só no LoadEffects
                },
            },
            LoadEffects = new List<MachineEffects>
            {
                new MachineEffects
                {
                    Sounds = new List<MachineSound> { new MachineSound { Id = "<cue vanilla curta>" } }, // validar em Data/Audio; NÃO referenciar Assets/Audio/*.ogg (placeholders sem pipeline)
                    ShakeDuration = 300,
                },
            },
            AllowFairyDust = false,        // 2 dias são regra (FR-001b)
            AllowLoadWhenFull = false,     // um batch por vez (default; explícito por legibilidade)
            InvalidItemMessage = "...",    // tokenizable/i18n; toast vanilla (FR-008a)
            WobbleWhileWorking = false,    // default off neste mod; reavaliar no playtest
            // Ausentes de propósito: MinutesUntilReady, OnlyCompleteOvernight, PreventTimePass,
            // AdditionalConsumedItems, RecalculateOnCollect, ReadyTimeModifiers, OutputMethod,
            // InteractMethod, LightWhileWorking, CopyColor/Price/Quality, PreserveId.
        };
    });
}
```

Variante folha-de-3 sem arte nova: manter `Frames = [1]` ou omitir `WorkingEffects` e obter vapor via `TemporarySprites` com textura vanilla (`TileSheets/animations`, ex. fumaça do Fish Smoker — inspecionar dados desempacotados como modelo). **Recomendação YAGNI: implementar primeiro folha A + TemporarySprites vanilla; pintar frames 3–5 (folha C) só se o playtest julgar o vapor vanilla deslocado.**

### 3.5 `Item.modData` (já existe; ligar a colocação/retirada — driver do timer, não do desenho)

O motor vanilla não persiste chaves custom de `modData` no timer: ao recolher um `(BC)` em processamento, zera `minutesUntilReady`. Contrato FR-005c exige sobrevivência do progresso: ao recolher/dislodgar, `WriteToModData` (`InputId`, `DaysRemaining`) + stack 1 + pausa; ao colocar, `PopulateFromModData` e re-hidratar (`PlaceInMachine` com input restaurado e timer proporcional, ou `OutputMethod` com `overrideMinutesUntilReady`). Se o spike concluir que nenhum re-hidrata de forma save-compatível, fallback especificado: interceptação SMAPI (`ModController.OnDayStarted/OnButtonPressed`) com dados vanilla — **a folha e este briefing não mudam**, só o driver do timer. Regra de ouro: fonte de verdade de `DaysRemaining` = `CompostingBinStateModel`; timer vanilla = espelho. **C# nunca desenha sprite** (regra de decisão conteúdo-vs-C# em processing §5.5).

---

## 4. Direção de arte consolidada (os 3 estados como 1 história de matéria)

Princípios Stardew-look (Sundrop Art Guide, Pixel Art Infographics, Pixeljoint *Things to Avoid*, Tutorial pixel-art wiki.gg): silhueta primeiro (reconhecível a 3 tiles, ripas ≥ 2 px de sprite); paleta terrosa dessaturada, outline 1 px **não-preto-puro** (tom mais escuro do fill deslocado no matiz — marrom-arroxeado para madeira); luz de **cima-direita levemente frontal** (highlight topo/borda direita, sombra esquerda/embaixo); hue shifting (sombras para o frio, highlights para o quente); frontal com leve vista de cima (vê-se a borda interna da abertura); sem dithering pesado, sem ruído aleatório, sem simetria espelhada perfeita, sem banding de highlight em tudo; sem sombra de chão embutida.

### 4.1 Frame 0 — Empty (vazio legível à distância)

Tampa **aberta/encostada** ou abertura frontal visível mostrando **interior oco escuro** (marrom quase-preto 1–2 tons, ex. `#3A2E22` — **não** usar `CompostEmptyColor #78716C` como fill: é cinza de overlay, no sprite vira "cimento"). Madeira **dessaturada**, sem conteúdo, sem brilho, sem vapor, sem estrela. Silhueta: caixa de ripas horizontais estilo Worm Bin (primo visual mais próximo), ~14 dos 16 px de largura, base assentada nos últimos 2 px.

### 4.2 Frame 1 — Processing (conteúdo parcial + calor/vapor)

Tampa **fechada** com **fresta âmbar fina de 1–2 px** (eco de `CompostProcessingColor #A16207`, faixa — não banho de cor); heap visível nas frestas migrado do verde-vivo atual para a **família ocre** (`rgb(161,98,7)` + destaques `rgb(194,150,80)` já presentes no PNG — a ponte que o artista deixou): verde-vivo sugere "planta saudável", leitura oposta a "matéria em decomposição"; ocre/marrom = terra/húmus (Living Roots). Preenchimento parcial ~30–40% das frestas (batch único FR-005a não enche a caixa). Vapor **branco-acinzentado semi-opaco frio** (nunca amarelo/laranja — quente + madeira = "pegando fogo"), 2–4 px por frame dentro das **linhas 00–07** (folha C: 3 posições esq/centro/dir + 1 dissipação). Laterais com 1 px de sombra quente.

### 4.3 Frame 2 — Ready (o herói: full + glow/steam sutil + marcador de forma)

1. *Full (legível sem efeitos):* tampa **entreaberta** (2–3 px mostrando massa de compost escura quase-preta esverdeada `#1E2B1F` com 3–5 px de highlight `#3E5A34` — "terra rica") + **montinho transbordando pela borda ou pá dourada inclinada para fora** (forma distinta do frame 1 mesmo em grayscale). Verdes sempre dessaturados escuros (`#2D4A22`–`#3E5A34`) — **nunca** `CompostReadyColor #166534` puro como fill (vira "lixeira tóxica" a 4×, some à noite, colide com plantações).
2. *Glow sutil:* 1 px de rim-light verde-amarelado (`#9ACD32`-ish, alfa ~40%) **só na borda da abertura** — nunca contorno inteiro, nunca vazando para tiles vizinhos.
3. *Steam frio:* 2 px ascendentes branco-azulados em alfa baixo (contrastar com o vapor âmbar-quente do processing).
4. *Marcador de forma (acessibilidade):* **estrela/sparkle 3–4 px** em `SunColor #FACC15` (`Constants.cs:52`) no canto superior direito **com contorno escuro 1 px** + montinho transbordando. Sobrevive a qualquer daltonismo e a screenshot P&B. Vetados: `!` gigante (conflita com quests/NPC), mudança de cor do corpo inteiro, animação > 2 frames no sprite estático, `Flicker` alta frequência (fotossensibilidade — respeitar *A Less Flashy Stardew*).

### 4.4 Invariante de montagem (bit-identidade da base)

**Base da caixa (linhas 08–30) BIT-IDÊNTICA entre as 3 células** — diff binário fora das áreas de conteúdo/vapor/faísca deve dar zero, garantindo que `ShowNextIndex*`/`Increment*` não "pula" a caixa na troca de estado. Mesma paleta/largura de contorno nos 3, fundo transparente, export PNG-8/32 com alfa. Teste de redução: encolher a folha a 50% — 3 estados ainda distinguíveis? Se não, voltar para **forma** (tampa/montinho), não para saturação. Revisar a 100% (arquivo), 400% (jogo) e com filtro deuteranopia/protanopia; testar sobre grama clara, terra, neve, à noite/chuva/inverno.

---

## 5. Mapa sonoro mínimo (100% vanilla, sem `Assets/Audio`)

| Evento | Som | Via |
|---|---|---|
| Waste aceito | `bubbles` (+ `Ship` opcional se o playtest pedir peso) | `LoadEffects.Sounds` — motor toca, sem código |
| Item inválido | `cancel` + shake ~300–500 ms | `Data/Machines` / fallback `location.playSound("cancel")` + shake no controller; **sem texto** (FR-008a) |
| Lote pronto (overnight) | *(silêncio)* | descoberta visual na manhã seguinte (sprite 2 + bolha), como vanilla |
| Coleta OK | `Ship` (manter provisório de `CompostingBinService:93`) ou `harvest` | `CollectCompost`; 1 toque |
| Coleta inventário cheio | `cancel` + `showRedMessage(message.harvest.inventory-full)` | controller/service (FR-005b); nada consumido/duplicado |
| `ProcessDayStart` | **remover** o `playSound("Ship")` atual (`CompostingBinService:128`) | toca sem jogador por perto, 1×/bin, semântica errada (`Ship` = depósito) |

Cues vanilla respeitam o slider de efeitos. **Nunca referenciar `Assets/Audio/*.ogg`** (placeholders sem pipeline — nota do repo). Descrições a corrigir: `ModEntry.cs:133` e `i18n` dizem "Output increases with continuous use" (resquício do multiplicador 1–5 aposentado pela FR-005a) → "1 waste → 1 compost in 2 days".

---

## 6. Verificação (manual obrigatória + automatizada)

### 6.1 Manual in-game (~25 min; template de PR exige)

1. Build Release com `GamePath`, `dotnet format --verify-no-changes` verde.
2. SMAPI: `list_items LivingRoots` → `(BC)LivingRoots.CompostingBin` e `(O)LivingRoots.Compost`, sem Error Item.
3. Craft (50 wood / 25 stone / 15 fiber, tab Home) → `(BC)` no inventário; place → sprite **frame 0**, sem bolha/som residual; comparar lado a lado com Worm Bin (mesma luz/estação — sem destoar em saturação/escala).
4. Item inválido → **`cancel` + shake**, nada consumido, sprite inalterado.
5. Resíduo válido → tremor + som curto, sprite vira **frame 1** (sem reload de cena).
6. Dormir 1 noite → ainda processing, sem som noturno, nada coletável.
7. Dormir 2ª noite → manhã: sprite **frame 2** + **bolha do compost quicando acima**, sem som ao acordar.
8. Mão vazia + right-click → 1 compost no inventário, bin volta a 0, bolha some (FR-005a).
9. Inventário cheio + coleta → `cancel` + `showRedMessage`, compost permanece no bin.
10. Pickup mid-processing → item stack 1 com modData; recolocar retoma (FR-005c); machadada com inventário cheio → debris flutuante com modData.
11. Save/reload em cada estado (empty, processing d1/d2, ready) → idênticos (FR-012/SC-004); dia avança com jogador fora da fazenda → exatamente 1× (FR-004).
12. Screenshots 0/1/2 em meio-dia, noite, chuva, inverno + grayscale test (forma carrega o significado; cor confirma); 3 bins lado a lado 10 min: "qual está pronto?" respondido em ≤ 3 s de olhar.
13. **Spike:** confirmar 0/1/2 via `IncrementMachineParentSheetIndex + ShowNextIndexWhenReady`; registrar comportamento real.

### 6.2 Automatizada (porta: `tasks.md` 004)

- `AssetRegistrationTests`: `Data/BigCraftables` com `Texture`/`SpriteIndex` corretos; `Data/Machines[(BC)…]` com 1 regra, `Trigger == ItemPlacedInMachine`, `DaysUntilReady == 2`, `ShowNextIndexWhenReady == true`, `IncrementMachineParentSheetIndex == 1`, `AllowFairyDust == false`.
- `CompostingMachineInvariantsTests` (FsCheck): Empty + waste → Processing; +2 day-updates → Ready; output == 1 para qualquer input válido.
- `BinPickupTests`: pickup serializa modData, stack 1, debris com modData se cheio.
- Gates: format → build → suite 100%.

---

## 7. Riscos e decisões abertas

| # | Risco / pergunta | Mitigação / dono |
|---|---|---|
| 1 | Um único N+1 comporta 3 estados? | Folha A/B + §3.2 resolve working/ready por contexto; validar em jogo antes de fechar arte |
| 2 | `modData` × timer nativo (re-hidratação save-compatível?) | `PlaceInMachine`/`OutputMethod` ou fallback SMAPI; folha inalterada em qualquer caso |
| 3 | Save existente com bin `(O)` quebra ao migrar para `(BC)`? | tolerant reader + nota de migração; testar upgrade no roteiro §6.1 |
| 4 | `Ship` vs `harvest`/`coin` no collect; `WobbleWhileWorking`/`LightWhileWorking` = ruído? | playtest cego A/B; defaults off; orçamento ≤ 16.67 ms (FR-010) |
| 5 | `RequiredTags` divergem da spec dona (incl. gap artisan goods, spec 004) | referenciar, nunca copiar; resolve-se na spec 002 |
| 6 | Automate puxa output antes do jogador ver a bolha | aceitável (vanilla-equivalente); não quebrar com `InteractMethod` que assuma jogador humano |
| 7 | Save-compat de `MaturationLevel`/campos legados 002 | manter reader tolerante, ignorar no yield (FR-005a) |

---

## 8. Out-of-scope (explicitamente NÃO deste sheet)

Arte de `compost.png` (briefing próprio); variantes sazonais/diurnas do bin; `IsLamp`/luz noturna além do declarado; sons `.ogg` custom; outputs aromatizados (`PreserveId: DROP_IN`, `CopyColor/Price`); combustíveis (`AdditionalConsumedItems`, Extra Machine Config); `RecalculateOnCollect`, `AllowLoadWhenFull: true`, múltiplas regras; sprites HD / Alternative Textures; UI própria ou artwork de recusa (FR-008a); mudança no conjunto compostável, `MaturationDays` ou `+15`/`100`; drop-on-ground com perda de progresso (superseded por FR-005c); verificação live 2-jogadores (roadmap D1); dependência Content Patcher (fallback do spike, não caminho primário); correção do código em si (tarefas T013/T022 — este briefing registra o requisito, não o implementa).

---

## Fontes

Internas (estado em 2026-10-05, revalidado 2026-10-06): `LivingRoots/Assets/` (4 PNGs, `file` 16×32/16×16), `LivingRoots/ModEntry.cs:115-176`, `LivingRoots/Constants.cs:47-79`, `LivingRoots/LivingRoots.csproj:26-28`, `LivingRoots/manifest.json` (`lfgranja.LivingRoots`, SMAPI 4.0), `specs/004-playable-compost-loop/briefings/{composting-bin-empty,composting-bin-processing,composting-bin-ready}.md`, `specs/002-soil-decay-compost/data-model.md:99-129` (bin object + waste definition), `ROADMAP.md D3`, `CompostingBinStateModel`, `CompostingBinService`, `ModController` (interação right-click).

Externas (todas com fetch verificado): `Modding:Machines` (schema completo) — https://wiki.stardewvalley.net/Modding:Machines · `Modding:Big craftables` (`Texture`, `SpriteIndex`, `Fragility`, `IsLamp`) — https://wiki.stardewvalley.net/Modding:Big_craftables · `Tutorial: Adding New Machines` (BC 16×32, índices sequenciais, receita 3-sprites `IncrementMachineParentSheetIndex + ShowNextIndexWhenReady`, `Frames`+`Interval`+`WorkingEffectChance: 1`) — https://stardewmodding.wiki.gg/wiki/Tutorial:_Adding_New_Machines_(Or_New_Rules_To_Existing_Machines) · `Modding:Recipe data` (flag big-craftable) — https://wiki.stardewvalley.net/Modding:Recipe_data · `Modding:Items` (sprites, `Texture`/`SpriteIndex`) — https://wiki.stardewvalley.net/Modding:Items · Content API (`AssetRequested`, `LoadFromModFile`, `AsDictionary`, `PatchImage`) — https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Content · `BigCraftableData.cs` decompilado — https://github.com/Dannode36/StardewValleyDecompiled · pixel-art (Sundrop Art Guide via Wayback, Pixel Art Infographics, Tutorial pixel-art wiki.gg, Pixeljoint *Things to Avoid*).

*Notas de honestidade: dimensões via `file`; ausência de wiring via grep no C#; semânticas de `Data/Machines` citadas da wiki vigente (rev. 2026-02-27) e do tutorial (ed. 2026-06-21). Comportamentos de motor sem teste local (empilhamento exato N+1, `WorkingEffectChance` 0.33, default `WobbleWhileWorking false`) marcados como "validar em jogo" no §6 — exceto o default de `WobbleWhileWorking`, corrigido para `false` conforme a wiki atual contra o briefing ready §3.1.*
