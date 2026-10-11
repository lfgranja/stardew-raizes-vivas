# Briefing 1:1 — Joja Fertilizer (antagonista)

> Status: briefing (não implementado). Agrupado de origem: `specs/future-roadmap/briefings/early-game-year1.md` Cap. 5 (preservado, não apagar).
> Escopo interno: `ROADMAP.md:17` ("chemical fertilizers… cost 5% soil health") — mecânica de destruição de solo listada no conceito central.
> Não promover para `specs/` sem `/speckit.specify`.

## 1. Conceito

Espelho sombrio do compost: boost imediato de crescimento (comportamento tipo Speed-Gro vanilla via índice `fertilizer` do `HoeDirt`) ao custo de **−5 soil health por aplicação** (nova constante `JojaFertilizerSoilCost = 5f`). Ensina o tradeoff central do mod (input × processo, `AGROECOLOGY_CONCEPTS.md:28` — contra o modelo Green Revolution). Vendido **só no Joja Mart**; odiado pela Elena (gift Hate — ver `elena-npc.md`); Pierre não vende (ele tem híbridas, mas não veneno — nuance proposital). Segundo na ordem 80/20 (1 objeto + shop append + hook inverso ao do compost).

## 2. Sprite specs

Item **16×16** `LivingRoots/Assets/joja_fertilizer.png`: saco azul-Joja com cifrão — deliberadamente plástico/industrial vs. estética orgânica do compost (`Assets/compost.png` como anti-referência). `Texture` custom, `SpriteIndex: 0` (wiki `Modding:Objects`).

## 3. Data/* — JSON/C# (registro via `ModEntry.OnAssetRequested`, padrão `ModEntry.cs:124-151`)

```csharp
data["LivingRoots_JojaFertilizer"] = new ObjectData
{
    Name = "Joja Fertilizer",
    DisplayName = "{{i18n:joja-fertilizer.name}}",
    Description = "{{i18n:joja-fertilizer.description}}",
    Type = "Basic", Category = -19, // fertilizer vanilla
    Price = 80,
    Texture = "Mods/LivingRoots/JojaFertilizer", SpriteIndex = 0,
    ContextTags = new[] { "joja_fertilizer", "!compostable_item" }, // NUNCA compostável
};
// Data/Shops — append no Joja (TargetField ["Joja","Items"], wiki Modding:Shops)
{ "Id": "LivingRoots_JojaFertilizer", "ItemId": "(O)LivingRoots_JojaFertilizer",
  "Price": 120, "AvailableStockLimit": "Player" }
```

Mecânica (C#, spec mapeia): compatibilidade com `HoeDirt.fertilizer` (como Speed-Gro registra crescimento — verificar índice vanilla na spec); efeito solo via ramo inverso de `ICompostApplicationService` em `ModController.OnButtonPressed` (`−JojaFertilizerSoilCost`, clamp em `MinSoilHealth = 0f`). Preço alto (120g vs. compost 10g) para não dominar Y1.

### Loja

Só Joja Mart (shop ID vanilla `Joja`). `AvailableStockLimit: Player` (Principle VI). Pierre não vende; Elena não vende (odeia).

## 4. Sons

Aplicação: `dirtyHit` vanilla (confirmar cue ID na spec; `fertilizer` não existe como cue — verificar); compra: `purchaseClick`. Sem custom.

## 5. i18n (adicionar a `en.json` + `default.json`)

```json
{ "joja-fertilizer.name": "Joja Fertilizer",
  "joja-fertilizer.description": "Fast growth now, dead soil later. (-5 soil health)",
  "message.joja-fertilizer.warning": "The soil hisses... (-5 health)" }
```

## 6. Arquitetura / multiplayer (Principle VI)

- Reutilizar `CompostApplicationService` como modelo (hook + floating text + invalidações), com serviço/ramo próprio para o custo; `HealthCategory` Poor [0,34) deve ser atingível via uso repetido (teste cobre).
- `OrganicWasteValidator`: garantir que `joja_fertilizer` com `!compostable_item` seja **rejeitado** como input da bin (anti-loop industrial → compost orgânico quebraria a tese).
- Farm-keyed, host-autoritativo, guards `Context.IsMainPlayer`; DDD + `new` só em `ModEntry.cs`; sem `.Result`/`.Wait()`; logs sem exceção crua.

## 7. Testes / gates

- `dotnet format --verify-no-changes` → build com `GamePath=` → `dotnet test` (baseline 816).
- Novos testes: `Data/Objects` contém item (Category −19, tag negada); append aparece só na shop `Joja`; aplicação dá boost de crescimento **e** `−5` saúde (clamp ≥ 0); rejeitado pelo `OrganicWasteValidator`; Elena Hate contém o ID; floating text de dano; chaves i18n em ambos JSONs.
- Critérios de aceite: tradeoff jogável e visível (aviso `message.joja-fertilizer.warning`); não compostável; não vendável fora do Joja; `dotnet format` limpo.

## 8. Constantes novas (`LivingRoots/Constants.cs`, classe `ModConstants`)

```csharp
public const string JojaFertilizerItemId = "LivingRoots.JojaFertilizer";
public const string QualifiedJojaFertilizerItemId = "(O)LivingRoots.JojaFertilizer";
public const float JojaFertilizerSoilCost = 5f;
public const string JojaFertilizerTag = "joja_fertilizer";
```

## 9. Dependências / ordem

Segundo na ordem 80/20 (logo após mulch — os dois juntos demonstram o eixo construir×destruir). Sem dependências. Referenciado por `elena-npc.md` (gift hate) e contraposto em `heirloom-seeds.md`.
