# Briefing 1:1 — Small Cistern (Epic 7 · captação de chuva)

> Status: briefing (não implementado). Agrupado de origem: `specs/future-roadmap/briefings/early-game-year1.md` Cap. 6 (preservado, não apagar).
> Escopo interno: `ROADMAP.md:40` (Small Cistern, coleta em dias chuvosos) · Epic 7 — US-07-01 #36 (receita) / US-07-02 #37 (coleta) em `LivingRoots/docs/EPICS_USER_STORIES.md:45-49`. Ponte para Mid Game greywater (Epic 15).
> Não promover para `specs/` sem `/speckit.specify`.

## 1. Conceito

Captação de chuva: big-craftable 1×2 (sprite **16×32**) + `Data/Machines` com trigger climático (`DayUpdate` + GSQ de clima). Coleta água em dias chuvosos, reduz idas ao lago; alternativa ecológica ao poço. Maior risco técnico do arco Y1 (GSQ exata de clima + decisão de design da água) — por isso é quarto na ordem 80/20, após os itens simples.

Questão aberta para a spec (**registrar, não resolver aqui**): a água vai (A) para o inventário como item `Rainwater` que recarrega o regador via C# (mais simples — recomendado 80/20 primeiro), ou (B) a cisterna irriga tiles vizinhos passivamente (aura 3×3, mais "mágica" Y1)? Este briefing especifica (A) com ganchos para (B).

## 2. Sprite specs (wiki `Modding:Big_craftables` + `Modding:Machines`, verificadas 2026-10-05)

Big-craftable **16×32** (2 tiles de altura) `LivingRoots/Assets/small_cistern.png`: barril de madeira com calha + nível d'água em 3 estágios. Usar `ShowNextIndexWhenReady` / `IncrementMachineParentSheetIndex` ou 3 arquivos (precedente `composting_bin_{empty,processing,ready}.png` sugere 3 arquivos: `small_cistern_{empty,partial,full}.png` — spec decide). `Fragility: 0`, `CanBePlacedOutdoors: true`, `CanBePlacedIndoors: false`. Overlay: ícone gota via `IVisualizationService` (decisão transversal do agrupado).

## 3. Data/* — JSON/C# (registro via `ModEntry.OnAssetRequested`; estende o padrão `Data/Machines` de `ModEntry.cs:151-176`, cuja chave é o ID **qualificado** `(BC)id`)

```csharp
// Data/BigCraftables — chave NÃO-qualificada (wiki Modding:Big_craftables)
data["LivingRoots_SmallCistern"] = new BigCraftableData
{
    Name = "Small Cistern",
    DisplayName = "{{i18n:cistern.name}}",
    Description = "{{i18n:cistern.description}}",
    Price = 0, Fragility = 0,
    CanBePlacedOutdoors = true, CanBePlacedIndoors = false,
    Texture = "Mods/LivingRoots/SmallCistern", SpriteIndex = 0,
};
// Data/Machines — chave QUALIFICADA com (BC) (wiki Modding:Machines)
data["(BC)LivingRoots_SmallCistern"] = new MachineData
{
    OutputRules = new[]
    {
        new MachineOutputRule
        { // enche sozinha na chuva — sem input (precedente: solar panel / worm bin via MachinePutDown)
            Id = "LivingRoots_RainCollect",
            Triggers = new[] { new Trigger { Trigger = "DayUpdate",
                Condition = "WEATHER Here Rain" } }, // cobre Rain + Storm? spec verifica GSQ exato
            OutputItem = new[] { new MachineItemOutput { ItemId = "(O)LivingRoots_Rainwater" } },
            DaysUntilReady = 0, // pronto no dia seguinte
        },
    },
    PreventTimePass = new[] { "Inside" }, // só funciona outdoor
    ExperienceGainOnHarvest = "Farming 2",
    // TODO spec: capacidade (sugestão CisternCapacity = 5), esvaziar se destruída, inverno congela?
};
// Data/CraftingRecipes — barata Y1 (IDs vanilla: 388 Wood, 390 Stone)
data["LivingRoots_SmallCisternRecipe"] = "388 30 390 20/Home/(BC)LivingRoots_SmallCistern 1/false/null/Small Cistern";
// Data/Objects — a água coletada (opção A: item p/ recarregar regador via C#)
data["LivingRoots_Rainwater"] = new ObjectData
{
    Name = "Rainwater",
    DisplayName = "{{i18n:rainwater.name}}",
    Description = "{{i18n:rainwater.description}}",
    Type = "Basic", Category = -26, Price = 0,
    Texture = "Mods/LivingRoots/Rainwater", SpriteIndex = 0,
    ContextTags = new[] { "rainwater", "!compostable_item" },
};
```

Notas wiki verificadas: trigger `DayUpdate` sem input é o padrão solar-panel/soda-machine; `PreventTimePass: [Sun|Rain|...]` pausa o timer (aqui `Inside` garante outdoor-only); `ShowNextIndexWhileWorking/WhenReady` + `WobbleWhileWorking` + `WorkingEffects` (sons/frames/shake) dão o feedback visual/sonoro; `InvalidItemMessage` para input rejeitado.

### Loja

Receita na Elena shop (200g, `AvailableStockLimit: Player`) + ponte narrativa: Elena ensina (receita), Robin constrói o upgrade grande depois (Mid Game greywater, Epic 15). Carpenter shop vanilla (`Carpenter`) fica para a spec do Mid Game.

## 4. Sons

Chuva coletada: `wateringCan` / ambiente `rain` vanilla; transbordo: `glug` (confirmar cue IDs na spec). Pronto: reutilizar padrão `composting_bin_ready.ogg` → `LivingRoots/Assets/Audio/cistern_ready.ogg` (gotejo) — primeiro `.ogg` novo desde a bin.

## 5. i18n (adicionar a `en.json` + `default.json`)

```json
{ "cistern.name": "Small Cistern",
  "cistern.description": "Collects rainwater overnight when it rains.",
  "rainwater.name": "Rainwater",
  "rainwater.description": "Sky-gifted water. Refills your watering can.",
  "message.cistern.collected": "Your cistern collected {0}x rainwater!",
  "message.cistern.frozen": "The cistern is frozen solid." }
```

## 6. Arquitetura / multiplayer (Principle VI)

- `Data/Machines` vazio hoje é o gap D3 (`ROADMAP.md:97-110`): este é o primeiro machine climático — o spike CP-vs-C# e a GSQ exata (`WEATHER Here Rain` vs. Storm/green rain) são tasks 1–2 da spec.
- Capacidade/esvaziamento/inverno via `ModDataService` farm-keyed; writes host-autoritativos, `Context.IsMainPlayer` guards; DDD + `new` só em `ModEntry.cs`; overlay via `IVisualizationService`; `CheckForOverflowUnderflow=true`.

## 7. Testes / gates

- `dotnet format --verify-no-changes` → build com `GamePath=` → `dotnet test` (baseline 816).
- Novos testes: `Data/BigCraftables` (outdoor-only, `Fragility 0`); `Data/Machines` chave `(BC)…` com regra `DayUpdate` + condição clima; noite chuvosa produz `Rainwater`, noite seca não produz; indoor não produz (`PreventTimePass`); capacidade 5 respeitada; inverno → `message.cistern.frozen`; receita craftável só com wood/stone; chaves i18n em ambos JSONs.
- Critérios de aceite: colocar → chover → coletar água → recarregar regador (opção A) funciona ponta a ponta; `dotnet format` limpo. Risco: GSQ de clima — travar a string exata em teste de integração contra build real.
- Armadilhas: trailing newline nos `.md`; não tocar nos `"(Planned)"` do `README.md`.

## 8. Constantes novas (`LivingRoots/Constants.cs`, classe `ModConstants`)

```csharp
public const string SmallCisternId = "LivingRoots.SmallCistern";
public const string MachineSmallCisternId = "(BC)LivingRoots.SmallCistern";
public const string SmallCisternRecipeId = "LivingRoots.SmallCisternRecipe";
public const string RainwaterItemId = "LivingRoots.Rainwater";
public const int CisternCapacity = 5;
```

## 9. Dependências / ordem

Quarto na ordem 80/20 (maior risco técnico GSQ). Sem dependências de itens; é pré-requisito narrativo do greywater (Epic 15, End Game). Receita vendida pela Elena (`elena-npc.md`).
