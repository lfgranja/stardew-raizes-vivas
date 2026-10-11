# Briefing 1:1 — Heirloom Seeds (Epic 6)

> Status: briefing (não implementado). Agrupado de origem: `specs/future-roadmap/briefings/early-game-year1.md` Cap. 4 (preservado, não apagar).
> Escopo interno: `ROADMAP.md:39` + `AGROECOLOGY_CONCEPTS.md:35` (sementes crioulas × híbridas; agrobiodiversidade) · Epic 6 — US-06-01 #34 (itens) / US-06-02 #35 (diferenciação) em `LivingRoots/docs/EPICS_USER_STORIES.md:39-43`.
> Não promover para `specs/` sem `/speckit.specify`.

## 1. Conceito

Sementes crioulas vendidas pela Elena × híbridas do Pierre — contraste narrativo central do Y1 (triângulo completo com `joja-fertilizer.md`). Mecânica: **+Soil Health no plantio/colheita**, crescimento levemente mais lento (tradeoff), qualidade maior em solo vivo (Healthy 67–100). Escopo Y1: **3 culturas** — 1 parsnip heirloom (primavera, vai no `welcome-kit.md`) + 1 verão + 1 outono. Three Sisters (milho/feijão/abóbora, Epic 3 #28/#29) reusam este pipeline depois. Regra proposta (spec trava): plantio heirloom `+2` soil health; colheita em solo Healthy `+1` + chance extra (stack com `ExtraHarvestChance`); híbridas Pierre neutras ou `−1`.

## 2. Sprite specs (wiki `Modding:Crop_data` + `Modding:Objects`, verificadas 2026-10-05)

| Asset | Spec | Qtd |
|---|---|---|
| Semente (item) | **16×16** cada; envelope de papel com broto verde (vs. sachê azul Pierre) | 3 |
| Produto | **16×16** cada; variação sutil (parsnip mais torta/manchada = charme heirloom) | 3 |
| Cultura (crop sheet) | 1 linha por cultura em `Assets/Crops/heirloom_crops.png`; fases = `DaysInPhase.Count` sprites lado a lado (padrão `TileSheets\crops`) | 3 linhas |

Arquivos: `LivingRoots/Assets/heirloom_seeds.png` (3 ícones), `LivingRoots/Assets/heirloom_produce.png` (3 ícones), `LivingRoots/Assets/Crops/heirloom_crops.png` (`Texture` custom por cultura, `SpriteIndex` = linha).

## 3. Data/* — JSON/C# (registro via `ModEntry.OnAssetRequested`; `Data/Crops` nunca tocado hoje — primeiro uso)

```csharp
// Data/Objects — semente + produto (exemplo parsnip; replicar ×3)
data["LivingRoots_HeirloomParsnipSeeds"] = new ObjectData
{
    Name = "Heirloom Parsnip Seeds",
    DisplayName = "{{i18n:heirloom.parsnip.seeds.name}}",
    Description = "{{i18n:heirloom.parsnip.seeds.description}}",
    Type = "Seeds", Category = -74, // seed category vanilla
    Price = 25,
    Texture = "Mods/LivingRoots/HeirloomSeeds", SpriteIndex = 0,
    ContextTags = new[] { "heirloom_seed", "seed_item" },
};
data["LivingRoots_HeirloomParsnip"] = new ObjectData
{
    Name = "Heirloom Parsnip",
    DisplayName = "{{i18n:heirloom.parsnip.name}}",
    Description = "{{i18n:heirloom.parsnip.description}}",
    Type = "Basic", Category = -75, // vegetable
    Price = 45, // > parsnip vanilla 35: prêmio qualidade/solo
    Texture = "Mods/LivingRoots/HeirloomProduce", SpriteIndex = 0,
    ContextTags = new[] { "heirloom_produce", "compostable_item" }, // colheita compostável!
};
// Data/Crops — chave = ID da SEMENTE (wiki Modding:Crop_data)
data["LivingRoots_HeirloomParsnipSeeds"] = new CropData
{
    Seasons = new[] { "spring" },
    DaysInPhase = new[] { 1, 2, 2, 2 }, // 7 dias (vanilla parsnip 4 — tradeoff heirloom)
    RegrowDays = -1,
    HarvestItemId = "LivingRoots_HeirloomParsnip",
    HarvestMethod = "Grab", HarvestMinStack = 1, HarvestMaxStack = 1,
    ExtraHarvestChance = 0.1, // geométrica: E = 1/(1-p)−1; bônus solo vivo via C# (não data)
    Texture = "Mods/LivingRoots/HeirloomCrops", SpriteIndex = 0,
    CountForMonoculture = true, CountForPolyculture = true,
};
```

Efeito solo (C#): hook no plantio/colheita via `ModController`; bônus `+2`/`+1` como constantes (abaixo). Overlay: borda verde em cultura heirloom via `IVisualizationService` (decisão transversal do agrupado).

### Loja (`Data/Shops`)

Elena shop (`LivingRoots_ElenaShop`): sementes com `AvailableStock` ilimitado Y1, preço ~1.25× Pierre (qualidade justifica); desconto por amizade via `PriceModifiers` + `Condition: "PLAYER_HEARTS LivingRoots_Elena 4"`; `AvailableStockLimit: Player`. Pierre continua vendendo híbridas (contraste, não substituição — substituição total é recompensa Food Sovereignty no End Game).

## 4. Sons

Colheita: `harvest` vanilla. Sem custom.

## 5. i18n (×3 culturas; exemplo parsnip; adicionar a `en.json` + `default.json`)

```json
{ "heirloom.parsnip.seeds.name": "Heirloom Parsnip Seeds",
  "heirloom.parsnip.seeds.description": "Old variety, slow but kind to the soil. Sold by Elena.",
  "heirloom.parsnip.name": "Heirloom Parsnip",
  "heirloom.parsnip.description": "Gnarled, fragrant, alive. The living soil thanks you.",
  "message.heirloom.soil-bonus": "The living soil thanks you! (+1)" }
```

## 6. Arquitetura / multiplayer (Principle VI)

- Pipeline semente→crop→produto é o template que Epic 3 (Three Sisters) e Mid Game (Green Manure) reusam — manter genérico (`HeirloomSeedTag`, bônus parametrizados).
- `OrganicWasteValidator` (`Domain/Services/`): colheita heirloom deve ser `compostable_item` (categorias aceitas hoje: −74/−75/−79/−80/−81; rejeita compost −2 anti-loop — ver `specs/003/.../compost-item.md §2.3`); sementes (`-74`) seguem regra do validador.
- Estado farm-keyed, host-autoritativo, `Context.IsMainPlayer` guards; DDD (`Domain/Interfaces/` → `Services/` → `Controllers/`; `new` só em `ModEntry.cs`).

## 7. Testes / gates

- `dotnet format --verify-no-changes` → build com `GamePath=` → `dotnet test` (baseline 816).
- Novos testes: 3 sementes + 3 produtos + 3 entradas `Data/Crops` (chave = ID da semente); `DaysInPhase` soma = dias anunciados; `HarvestItemId` resolve para produto existente; plantio `+2`, colheita Healthy `+1`; loja Elena lista as 3 com `AvailableStockLimit: Player`; chaves i18n ×3 em ambos JSONs.
- Critérios de aceite: ciclo completo plantar→colher→compostar fecha para as 3; tradeoff de crescimento perceptível vs. vanilla; desconto amizade aplica; `dotnet format` limpo.

## 8. Constantes novas (`LivingRoots/Constants.cs`, classe `ModConstants`)

```csharp
public const string HeirloomSeedTag = "heirloom_seed";
public const string HeirloomProduceTag = "heirloom_produce";
public const float HeirloomPlantBonus = 2f;
public const float HeirloomHarvestBonus = 1f;
// + 3 pares de IDs (semente/produto/qualificados) e preços por cultura
```

## 9. Dependências / ordem

Terceiro na ordem 80/20 (após mulch e joja; pipeline template para Three Sisters). Alimenta `welcome-kit.md` (5 sementes no kit) e `elena-npc.md` (itens da loja + quest de desconto).
