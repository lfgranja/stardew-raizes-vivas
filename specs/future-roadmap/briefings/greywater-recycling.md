# Briefing — Greywater Recycling (Epic 15 · US-15-01 #54 / US-15-02 #55)

> **Escopo:** `ROADMAP.md:60` — "House upgrade that installs a system to collect water from sink/shower (greywater) and store it in tanks, passive irrigation (like the Well, but fills faster)".
> **Epics:** Epic 15 (Issue #17): US-15-01 (#54) coleta/upgrade, US-15-02 (#55) armazenamento/reuso.
> **Status:** sem spec/plan/tasks. Insumo para `speckit.specify`.
> **Leitura interna:** agrupado §4, `AGROECOLOGY_CONCEPTS.md` (autonomia/fechamento de ciclos), Year 1 Small Cistern (Epic 7 — greywater é a evolução casa-nível), `LivingRoots/Constants.cs`, `LivingRoots/ModEntry.cs:151-175` (padrão `Data/Machines`).
> **Independência:** largamente paralelizável com ICLF após fundação `Data/Objects`+`Data/Machines` dominada.

---

## 1. Conceito de jogo

Upgrade de casa que instala coleta de pia/chuveiro em tanques — **fonte passiva de irrigação** (Well vanilla, mas enche mais rápido). Fecha o ciclo água junto com a Cisterna de chuva (Year 1): chuva = exterior sazonal, greywater = interior constante. Autonomia hídrica temática, sem mágica: taxa fixa/dia, cap finito, inverno congela (exterior) ou reduz (interior — spec decide, coerência com `GrowsInWinter` das ICLF).

Atenção técnica central: o vanilla **não tem "item água"** — Well/Sprinkler são lógicas hardcoded, não máquinas com output. O briefing fixa duas rotas e recomenda uma (ver §3).

---

## 2. Sprites

| Asset | Spec | Arquivo sugerido |
|---|---|---|
| Tanque greywater (big-craftable) | **16×32** (2 tiles altura, padrão big-craftable), 3 estágios (vazio/parcial/cheio) via `ShowNextIndexWhileWorking/WhenReady` ou 3 sprites | `Assets/greywater_tank_empty.png`, `..._partial.png`, `..._full.png` — tambor cinza-esverdeado com cano + nível d'água |
| Upgrade casa (exterior/interior) | tiles `.tmx` (canos, caixa de inspeção) | tileset `z_greywater.png` (prefixo `z_` p/ não deslocar índices vanilla) |
| Carga greywater (item, se rota-a) | **16×16** | `Assets/greywater_charge.png` — balde cinza |
| Efeito rega | vanilla `wateringCan` splash | sem asset novo |

---

## 3. Data JSON

IDs: `LivingRoots.GreywaterTank` (big-craftable), máquina qualificada `(BC)LivingRoots.GreywaterTank`, carga `(O)LivingRoots.GreywaterCharge` (só rota-a), upgrade `LivingRoots.GreywaterUpgrade`.

```csharp
// Data/BigCraftables
data["LivingRoots.GreywaterTank"] = new BigCraftableData {
    Name = "Greywater Tank", DisplayName = "{{i18n:greywater.tank.name}}",
    Description = "{{i18n:greywater.tank.description}}",
    Price = 0, Fragility = 0, CanBePlacedOutdoors = true, CanBePlacedIndoors = false,
    Texture = "Mods/LivingRoots/GreywaterTank", SpriteIndex = 0,
    ContextTags = new[] { "greywater_tank" },
};

// Data/Machines — ROTA (a) RECOMENDADA: máquina passiva DayUpdate + OutputMethod C# condicional
data["(BC)LivingRoots.GreywaterTank"] = new MachineData {
    OutputRules = new[] {
        new MachineOutputRule {
            Id = "LivingRoots_GreywaterDaily",
            Triggers = new[] {
                new Trigger { Trigger = "DayUpdate",
                    Condition = "SEASON Spring Summer Fall" } // inverno pausa exterior (spec confirma)
            },
            OutputItem = new[] {
                new MachineItemOutput {
                    ItemId = "(O)LivingRoots.GreywaterCharge",
                    Condition = "PLAYER_HAS_MAIL LivingRoots_GreywaterInstalled",
                    MinStack = 2, MaxStack = 4, // "enche mais rápido que Well" = número explícito
                }
            },
            MinutesUntilReady = 0, RecalculateOnCollect = true,
            OutputMethod = "LivingRoots.Services.GreywaterService, LivingRoots: GetDailyOutput",
            // C# decide: nível upgrade × nº moradores × estação (lógica além de taxa fixa)
        },
    },
    PreventTimePass = new[] { "Inside" }, // só outdoor conta dia? (tanque exterior)
    OnlyCompleteOvernight = true,
    AllowFairyDust = false,
    InvalidItemMessage = "{{i18n:greywater.invalid}}",
};

// ROTA (b) alternativa data-only: Data/Buildings Chests + ItemConversions
// building com Chests[Input/Output] + ItemConversions (SourceChest/DestinationChest,
// MaxDailyConversions, RequiredTags vazio = gera do nada). Mais barato, menos condicional.
// Spec escolhe (a) se taxa varia por upgrade/morador, (b) se taxa fixa.

// Data/Objects — carga (rota-a)
data["LivingRoots.GreywaterCharge"] = new ObjectData {
    Name = "Greywater Charge", DisplayName = "{{i18n:greywater.charge.name}}",
    Description = "{{i18n:greywater.charge.description}}",
    Type = "Basic", Category = -26, Price = 0,
    Texture = "Mods/LivingRoots/GreywaterCharge", SpriteIndex = 0,
    ContextTags = new[] { "greywater_charge", "!compostable_item" },
};

// Data/Buildings — upgrade Robin formal (recomendado sobre anexo mailbox-like)
data["LivingRoots.GreywaterUpgrade"] = new BuildingData {
    // BuildingToUpgrade: "FarmHouse" (ou nível específico), Builder: "Robin",
    // BuildCost, BuildMaterials (ex.: 50 stone + 30 hardwood + 5000g), BuildDays = 3,
    // ActionTiles: abrir UI tanque; Chests se rota-b; IndoorMap preservado;
    // Condition de compra: "YEAR 4, PLAYER_HAS_MAIL livingroots_year4"
};

// Data/CraftingRecipes — tanque craftável pós-upgrade? (alternativa barata ao upgrade formal)
data["LivingRoots.GreywaterTankRecipe"] = "388 60 390 40 335 5/Home/(BC)LivingRoots.GreywaterTank 1/false/.../Greywater Tank";
```

Consumo (spec trava): 1 carga = N tiles regados (sugerido 8) via regador recarregado por proximidade (C# `HoeDirt` water) **ou** aura 3×3 passiva do tanque. Recomendado 80/20: recarga de regador por proximidade primeiro (simples, testável).

Persistência: cargas em modData do building/save, com caps anti-DoS (`MaxDataValueSizeBytes`, cap Y cargas, ex.: 12).

---

## 4. Mapa / Evento

- **Formato:** `.tmx` (Tiled) > `.tbin`; `EditMap` (nunca `Load` integral); tilesets custom prefixo `z_`.
- **Exterior:** tanque ao lado da casa — `SourceRect`, `DrawLayers`, `CollisionMap` (`XXXX/XOOX` pattern se 2×2 com frente passável), `ActionTiles` (abrir coleta), `NoSpawn`/`NoFurniture` onde couber. Layers: Back (terreno) / Buildings (tanque bloqueia salvo `Passable T`) / Paths (canos) / Front (telhado cano).
- **Interior (upgrade):** `Maps/FarmHouse` edit — canos pia→parede (`Buildings` layer), `IndoorMap` preservado, warps intactos. `Data/Buildings` formal com `HasIndoorsName("FarmHouse")` e saves.
- **Evento:** compra via Robin (carpenter menu vanilla — sem evento custom se upgrade formal). Se anexo: evento curto de instalação (`Data/Events/FarmHouse`: Robin 1 dia, `Time 900 1200`, `skippable`, `setSkipActions` dá flag mesmo se pular, `broadcastEvent`).
- **Action tile:** `Action OpenShop`? Não — `Action CollectGreywater` (C# intercepta) ou chest `Collect` vanilla se rota-b.

---

## 5. UI / Interação / Sons

- **UI:** nenhuma janela custom no MVP — coleta via interação máquina/chest vanilla (bubble "item ready" `DisplayTile`, `ShowNextIndexWhenReady`, luz `LightWhileWorking` opcional). Contador de cargas no tooltip (`maturity-tooltip` pattern: `"Greywater: {0}/{1}"`). Evolução (não-prometida): medidor no HUD fazenda.
- **Overlay visualization:** gota sobreTiles irrigáveis pelo tanque (tint `WaterColor #2563EB`, opacidade 0.5) — reutilizar `IVisualizationService`.
- **Sons:** `wateringCan` (coleta), `glug`/`waterDrop` transbordo (confirmar cue IDs na spec), construção `hammer`. Pronto: `Assets/Audio/greywater_ready.ogg` (gotejo, padrão `composting_bin_ready.ogg`) — placeholder até wirado.

---

## 6. i18n

```json
{
  "greywater.tank.name": "Greywater Tank",
  "greywater.tank.description": "Slowly collects sink/shower water. Passive irrigation, faster than a well.",
  "greywater.charge.name": "Greywater Charge",
  "greywater.charge.description": "One bucket of second-hand water. Good for 8 tiles.",
  "greywater.invalid": "This tank only collects overnight.",
  "greywater.tooltip": "Greywater: {0}/{1}",
  "message.greywater.collected": "Collected {0}x greywater.",
  "message.greywater.frozen": "The tank is frozen solid.",
  "message.greywater.upgraded": "Robin installed your greywater system!"
}
```

---

## 7. Gates / desbloqueio / balanceamento

| Gate | Proposta |
|---|---|
| Desbloqueio | Upgrade Robin Year 4+ (`YEAR 4` + mail `livingroots_year4`); custo 5000g + 50 stone + 5 iron bar + 3 dias (ponte Small Cistern: quem tem cisterna ganha 10% desconto? flavor — spec decide) |
| Taxa | 2–4 cargas/dia (primavera-verão-outono), 0–1 inverno exterior congelado; cap 12; "mais rápido que Well" = Well ~1/dia equivalente → 3× |
| Consumo | 1 carga = 8 tiles (rega por proximidade) ou aura 3×3/dia; chuva não acumula (anti-double-dip com cisterna) |
| Multiplayer | Cargas farm-keyed, coleta por jogador; upgrade comprado pelo host; `AvailableStockLimit: Player` na receita |
| Saves | Flag `LivingRoots_GreywaterInstalled` em modData; tanque sem flag = decorativo (sem output) |
| Segurança | Clamp cargas, `checked`, Trace-only tipo+HResult; `MaxTilesPerSave`-style cap no loop de rega |

---

## 8. Constantes novas

```csharp
public const string GreywaterTankId = "LivingRoots.GreywaterTank";
public const string MachineGreywaterTankId = "(BC)LivingRoots.GreywaterTank";
public const string GreywaterChargeId = "LivingRoots.GreywaterCharge";
public const string QualifiedGreywaterChargeId = "(O)LivingRoots.GreywaterCharge";
public const int GreywaterDailyMin = 2;
public const int GreywaterDailyMax = 4;
public const int GreywaterCapacity = 12;
public const int GreywaterTilesPerCharge = 8;
public const string GreywaterInstalledMail = "LivingRoots_GreywaterInstalled";
```

## 9. Riscos e teste

- **Riscos:** "item água" não existe — rota errada gera item morto; inverno inconsistente com ICLF; `OutputMethod` C# com assinatura `MachineOutputDelegate` errada quebra silenciosamente (validar assinatura `GetOutput(Object, Item, bool, MachineItemOutput, Farmer, out int?)`); upgrade formal vs. anexo (migração de save se trocar depois).
- **Teste:** unit (taxa por estação/upgrade/morador, cap, consumo tiles); integração (registro BigCraftable+Machine+Building, `DayUpdate` gera carga, coleta esvazia, inverno pausa); manual (instalar, dormir 3 dias cada estação, regar 8 tiles/carga, demolir tanque).

## Fontes

- Internas: `ROADMAP.md:60`, Epic 15, agrupado §4, Small Cistern (Epic 7) como precedente.
- Externas: `Modding:Machines` (OutputRules/Triggers DayUpdate/OutputMethod C# `MachineOutputDelegate`/PreventTimePass/OnlyCompleteOvernight/AllowFairyDust/WorkingEffects/InvalidItemMessage); `Modding:Big_craftables` (Fragility/CanBePlacedIndoors-Outdoors/Texture 16×32/SpriteIndex); `Modding:Buildings` (Builder/BuildCost/BuildMaterials/BuildDays/Upgrades/Chests+ItemConversions/ActionTiles/IndoorMap/Size/CollisionMap); `Modding:Maps` (.tmx/EditMap/`z_`); `Modding:Recipe_data`.
