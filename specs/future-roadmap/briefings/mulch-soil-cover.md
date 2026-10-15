# Briefing 1:1 — Mulch / Soil Cover (Epic 4)

> Status: briefing (não implementado). Agrupado de origem: `specs/future-roadmap/briefings/early-game-year1.md` Cap. 3 (preservado, não apagar).
> Escopo interno: `ROADMAP.md:37` + `AGROECOLOGY_CONCEPTS.md:39` (cobertura morta anti-erosão; Primavesi) · Epic 4 — US-04-01 #30 (item) / US-04-02 #31 (efeitos) em `LivingRoots/docs/EPICS_USER_STORIES.md:27-31`.
> Não promover para `specs/` sem `/speckit.specify`.

## 1. Conceito

Cobertura morta aplicada em tile arado (gesto como fertilizante vanilla, via novo `IMulchApplicationService` espelhado em `ICompostApplicationService` — contrato em `Domain/Interfaces/`, impl em `Services/`, eventos em `Controllers/`). Efeito: **zera ou reduz o `DailyDecayRate = 2/dia`** (`Constants.cs:57`) no tile coberto, com sinergia ao `SeasonalDecayMultiplier` (Spring 0.5×, Summer 1.5×). Duração: consome 1 carga a cada N dias (nova constante `MulchDurationDays = 7`, análoga a `MaturationIncrementDays`); chuva não remove. Também previne ervas (spec detalha). Primeiro da ordem 80/20 — menor esforço (1 objeto + 1 receita + hook no decay existente).

## 2. Sprite specs

| Asset | Spec |
|---|---|
| Item | `LivingRoots/Assets/mulch.png` **16×16** (wiki `Modding:Objects`: sprite 16×16, `Texture` default `Maps/springobjects`), base marrom `ModConstants.MulchColor #92400E (146,64,14)` + highlights `#A16207/#78716C` da paleta compost |
| Tile overlay | tint `MulchColor` opacidade 0.5 (`DefaultOpacity`, `Constants.cs:37`) + pattern `Dots` (`PatternType`) para daltônicos; tile com mulch+planta = borda pontilhada; alimentar `IVisualizationService` (nunca hardcodar em controller) |
| Partícula aplicação | reutilizar partícula do compost (marrom) — sem asset novo |

Categorias de saúde afetadas: Poor [0,34) / Moderate [34,67) / Healthy [67,100] (`Domain/Visualization`, pattern mín. 0.7).

## 3. Data/* — JSON/C# (registro via `ModEntry.OnAssetRequested`, padrão `ModEntry.cs:116-151`)

```csharp
// Data/Objects — chave = ID não-qualificado (wiki Modding:Objects)
data["LivingRoots_Mulch"] = new ObjectData
{
    Name = "Mulch",
    DisplayName = "{{i18n:mulch.name}}",
    Description = "{{i18n:mulch.description}}",
    Type = "Crafting", Category = -26, // mesmo do compost (ModConstants.CompostCategoryId)
    Price = 5,
    Texture = "Mods/LivingRoots/Mulch", SpriteIndex = 0,
    ContextTags = new[] { "mulch_item", "compostable_item" },
};
// Data/CraftingRecipes — string legada "<ingredientes>/<aba>/<item qtd>/<craftable?>/<condição>/<nome>"
// Receita barata Y1 (fibra = capina, acessível dia 1). Ex. vanilla: "388 45 390 45/Home/16/true/s Farming 6/"
data["LivingRoots_MulchRecipe"] = "771 10/Home/LivingRoots.Mulch 1/false/null/Mulch";
// 771 Fiber x10 — IDs finais na spec (placeholder acima; verificar 259/309 antes de travar)
```

Efeito solo (C#, não data): `ISoilDecayService` checa `tile.HasMulch` → `decay *= MulchDecayMultiplier (0f)` (ou 0.25 se a spec preferir balanceamento parcial). Estado `HasMulch + dias restantes` persistido via `ModDataService` com chaves `*_KeyPrefix`, cache esparso (zeros omitidos, como `soil_health_data_<saveId>`; respeitar `MaxTilesPerLocation`/`MaxTilesPerSave`).

### Loja

Receita à venda na Elena shop dia 1 (`Price: 100g`, `AvailableStockLimit: Player`); item é craftável, não precisa vender o item. Pierre **não** vende (diferenciação agroecologia × convencional).

## 4. Sons

Aplicação: `hoeHit` ou `sandyStep` vanilla (passos em palha). Craft: `axe` (segue `ModConstants.CompostCraftingSound = "axe"`). Sem `.ogg` custom Y1 (precedente: `Assets/Audio/composting_bin_{processing,ready}.ogg` só para a bin).

## 5. i18n (adicionar a `en.json` + `default.json`)

```json
{ "mulch.name": "Mulch",
  "mulch.description": "Dry cover that keeps tilled soil alive. Blocks decay and weeds.",
  "message.mulch.invalid-tile": "Mulch can only be applied to tilled soil.",
  "message.mulch.already": "This tile is already mulched." }
```

## 6. Arquitetura / multiplayer (Principle VI)

- Novo `IMulchApplicationService` em `Domain/Interfaces/` + impl em `Services/` (DDD; `new` só em `ModEntry.cs`); hook em `ModController.OnButtonPressed` como o compost (`ModController.cs:845`); `ExecuteWithConcurrencyGuard` para reentrância.
- Estado farm-keyed (`SaveIdProvider`), writes host-autoritativos, guards `Context.IsMainPlayer`; sem `.Result`/`.Wait()`, sem lock através de I/O; logs sem mensagens cruas (`ex.GetType().FullName` + `HResult`, `Trace` apenas); `CheckForOverflowUnderflow=true`.

## 7. Testes / gates

- `dotnet format --verify-no-changes` → build com `GamePath=` → `dotnet test` (baseline 816).
- Novos testes: `Data/Objects` contém `LivingRoots_Mulch` (Category −26, tags); receita parseável no formato legado; `decay *= 0` em tile com mulch (incl. sinergia Spring 0.5×/Summer 1.5×); duração 7 dias expira; chuva não remove; aplicar em tile não-arado → `message.mulch.invalid-tile`; reaplicar → `message.mulch.already`; overlay usa `MulchColor` via `IVisualizationService`.
- Critérios de aceite: craft dia 1 só com fibra; tile com mulch mantém saúde por 7 dias; receita comprável na Elena dia 1; `dotnet format` limpo.

## 8. Constantes novas (`LivingRoots/Constants.cs`, classe `ModConstants`)

```csharp
public const string MulchItemId = "LivingRoots.Mulch";
public const string QualifiedMulchItemId = "(O)LivingRoots.Mulch";
public const int MulchDurationDays = 7;
public const float MulchDecayMultiplier = 0f;
public const string MulchTag = "mulch_item";
```

## 9. Dependências / ordem

Primeiro na ordem 80/20. Sem dependências (só do decay existente). Desbloqueia `welcome-kit.md` (passo 2 da quest) e alimenta overlay de `elena-npc.md`.
