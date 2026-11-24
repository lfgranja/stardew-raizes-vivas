# Briefing — ICLF Trees: Acacia / Gliricidia (Epic 14 · US-14-01 #51 / US-14-02 #52)

> **Escopo:** `ROADMAP.md:59` — "ICLF (Integrated Crop-Livestock-Forestry): new trees (Acacia, Gliricidia) that fix nitrogen, cultivation between them for drought immunity and faster growth".
> **Epics:** Epic 14 (Issue #16): US-14-01 (#51) nitrogen-fixing trees, US-14-02 (#52) drought resistance + growth. US-14-03 (produto animal) está em `shaded-milk-soft-wool.md`.
> **Status:** nenhum asset tem spec/plan/tasks. Este briefing é insumo para futuros `speckit.specify` — não é especificação.
> **Leitura interna:** `specs/future-roadmap/briefings/end-game-year4-plus.md` §2, `LivingRoots/docs/AGROECOLOGY_CONCEPTS.md:41` (ICLF), `LivingRoots/docs/EPICS_USER_STORIES.md:92-97`, `LivingRoots/Constants.cs` (padrão magic numbers), `LivingRoots/ModEntry.cs:115-175` (padrão `OnAssetRequested`).
> **Dependências:** Year 1 base (solo 0–100 + compost + mulch + heirloom) e Mid (rotação + adubo verde + Mobile Coop) prontos. ICLF é a base do arco End-Game — bloqueia `shaded-milk-soft-wool.md`, alimenta `food-sovereignty-guardian-trophy.md` (solo >80%).

---

## 1. Conceito de jogo e tese agroecológica

Duas árvores leguminosas de integração lavoura-pecuária-floresta. Na literatura real (Embrapa ILPF), acácia-negra e gliricídia fixam N via rizóbio, produzem sombra/forragem e quebram vento. Tradução no mod:

- **Aura de nitrogênio:** tiles aráveis num raio R ganham +X soil health/dia até teto.
- **Cultivo entre árvores:** imunidade à seca + crescimento acelerado dentro da aura.
- **Sombra para gado:** ponte para `shaded-milk-soft-wool.md`.
- **Custo de design:** caras de plantar, lentas de crescer, permanentes — o "motor ecológico" do Year 4 que não pode trivializar aspersores.

`AGROECOLOGY_CONCEPTS.md:41` ancora: "combining animal husbandry, crop cultivation, and tree maintenance". Princípios atendidos: sinergia, resiliência (seca), autonomia (menos fertilizante externo).

---

## 2. Sprites

| Asset | Spec vanilla | Arquivo sugerido | Notas |
|---|---|---|---|
| Acácia spritesheet | `Data/WildTrees` → `Textures[]`, 5 estágios (Seed/Sprout/Sapling/Bush/Tree) + stump; suporta `Season` por entrada e `Condition` (GSQ, avaliada na criação/novo dia/reload — **não** hora-do-dia sem update manual SMAPI) | `Assets/Trees/acacia.png` (+ `acacia_winter.png` se `GrowsInWinter=false` + `IsStumpDuringWinter`? decidir) | Copa ampla e espinhosa; tronco escuro; folhagem miúda. Se usar `UseAlternateSpriteWhenSeedReady` ou `GrowsMoss`, folha = **duplo/triplo de largura** (alternates à direita). 4 folhas cosméticas à esquerda do stump para `IsLeafy` |
| Gliricídia spritesheet | idem | `Assets/Trees/gliricidia.png` (+ sazonal) | Copa densa tropical, flores rosadas na primavera (variante sazonal via 4 entradas `Textures[]` com `Season`) |
| Semente (item 16×16) | `Data/Objects`, default `Maps/springobjects` | `Assets/acacia_seed.png`, `Assets/gliricidia_seed.png` **16×16** | Envelope-semente com folha; distinguir de oak/maple/pine |
| Shake drop (vagem/forragem 16×16) | `ShakeItems` output | `Assets/acacia_pod.png` | Opcional Y1 do asset; pode reusar sprite da semente |

Paleta: seguir `ModConstants` existentes (marrom `#92400E` tronco, verdes `#166534/#15803D` copa). Evitar detalhe <2px (legibilidade 16×32 por estágio). Placeholders em `Assets/` seguem padrão atual (copiados ao build, não wired até pipeline `ModContent.Load` / `Texture` em JSON).

Registro de imagem (espelho Content Patcher):

```json
{
  "Action": "Load",
  "Target": "Mods/LivingRoots/Acacia, Mods/LivingRoots/Gliricidia, Mods/LivingRoots/IclfSeeds",
  "FromFile": "assets/{{TargetWithoutPath}}.png"
}
```

---

## 3. Data JSON

Convenção de IDs: `LivingRoots.Acacia`, `LivingRoots.Gliricidia` (WildTrees, string IDs únicas — vanilla usa `1` oak, `2` maple, `3` pine, `8` mahogany, `13` mystic); sementes `(O)LivingRoots.AcaciaSeed`, `(O)LivingRoots.GliricidiaSeed`.

```csharp
// Data/WildTrees — via OnAssetRequested (espelho CP: EditData Data/WildTrees)
data["LivingRoots.Acacia"] = new WildTreeData
{
    SeedItemId = "(O)LivingRoots.AcaciaSeed",
    SeedPlantable = true,
    GrowthChance = 0.08,            // lenta: ~12 dias/estágio sem fertilizante (vs. 0.2 default)
    FertilizedGrowthChance = 0.6,
    SeedSpreadChance = 0.03,        // anti-infestação (default 0.15)
    SeedOnShakeChance = 0.05,
    SeedOnChopChance = 0.75,
    DropWoodOnChop = true,
    DropHardwoodOnLumberChop = false,
    IsLeafy = true,
    IsLeafyInWinter = false,
    GrowsInWinter = true,           // acácia rústica: cresce no inverno (decisão travada aqui, spec confirma)
    IsStumpDuringWinter = false,
    GrowsMoss = false,              // YAGNI: sem triplo-width agora
    UseAlternateSpriteWhenSeedReady = false,
    DebrisColor = 12,
    Textures = new[] {
        new { Texture = "Mods/LivingRoots/Acacia", Season = (string?)null, Condition = (string?)null },
    },
    ShakeItems = new[] {
        new { ItemId = "(O)LivingRoots.AcaciaSeed", Chance = 0.5, MinSize = "Tree", MaxSize = "Tree" },
    },
    ChopItems = new[] {
        new { ItemId = "(O)388", MinStack = 8, MaxStack = 14 }, // wood vanilla
    },
    PlantableLocationRules = new[] {
        new { Id = "FarmOnly", Result = "Deny",
              Condition = "LOCATION_IS Farm", // inverter: Allow farm, Deny resto — pseudocódigo, spec escreve GSQ exato
              DeniedMessage = "{{i18n:iclf.denied-message}}" },
    },
    CustomFields = new Dictionary<string,string> {
        ["LivingRoots/AuraRadius"] = "6",
        ["LivingRoots/NitrogenPerDay"] = "3",
    },
};

data["LivingRoots.Gliricidia"] = /* idem, diferenças: */
    // GrowthChance = 0.10, GrowsInWinter = false (tropical — NÃO cresce no inverno),
    // SeedSpreadChance = 0.02, ShakeItems += forragem sazonal,
    // TapItems (extensão opcional, NÃO prometida): forragem via tapper
    // TapItems = [{ ItemId="(O)LivingRoots.GliricidiaForage", DaysUntilReady=3 }]

// Data/Objects — sementes
data["LivingRoots.AcaciaSeed"] = new ObjectData {
    Name = "Acacia Seed", DisplayName = "{{i18n:iclf.acacia-seed.name}}",
    Description = "{{i18n:iclf.acacia-seed.description}}",
    Type = "Seeds", Category = -74, Price = 500,
    Texture = "Mods/LivingRoots/IclfSeeds", SpriteIndex = 0,
    ContextTags = new[] { "iclf_seed", "seed_item" },
};
data["LivingRoots.GliricidiaSeed"] = /* SpriteIndex = 1, Price = 450 */ null;

// Data/Shops — Elena vende (canal agroecológico; Pierre NÃO vende)
{ "Id": "LivingRoots.AcaciaSeed", "ItemId": "(O)LivingRoots.AcaciaSeed",
  "Price": 800, "AvailableStock": 2, "AvailableStockLimit": "Player",
  "Condition": "YEAR 4" }  // ou gate por progresso — ver §7
```

Mecânicas LivingRoots (C#, não existem no vanilla — a spec detalha, o briefing fixa contratos):

1. **Aura N:** `IIclfService` (contrato em `Domain/Interfaces/`, impl em `Services/`). A cada `DayStarted` (nunca por tick): varre `GameLocation.terrainFeatures` (WildTree, filtra IDs `LivingRoots.*`), cruza com sparse cache de soil health, aplica `+NitrogenPerDay` em raio `AuraRadius` até teto. Teto e perf: `MaxTilesPerSave`-style cap, host-side, replicado via modData (ver D1 multiplayer).
2. **Seca:** vanilla não tem "drought" como sistema — definir na spec: cultivo na aura ignora 1 dia sem rega (sem atraso/morte)? Ou imune a `DirtDecayChance`? Sugerido: pular 1 dia sem penalidade + `DaysInPhase` efetivo ×0.85.
3. **Sinergia animal:** pasto sob copa = flag "sombreado" consumida por `shaded-milk-soft-wool.md`.

---

## 4. Mapa / Evento

- **Spawn inicial:** tile property `SpawnTree: wild LivingRoots.Acacia <estágio-criação> <estágio-regrowth>` na layer **Paths** (tile 34 do paths sheet) — ex.: faixa quebra-vento na fazenda. Alternativa player-driven: só via semente Elena (recomendado para não forçar layout).
- **Edição de mapa:** `.tmx` (Tiled, texto/XML, diffável) > `.tbin`; Content Patcher `EditMap` (nunca `Load` integral). Prefixar tilesets custom com `z_`. Layers padrão: Back (terreno) / Buildings (parede salvo `Passable T`) / Paths (spawn, flooring, debris) / Front / AlwaysFront. Suffix `Back-1`/`Back2` para overlays.
- **Evento de desbloqueio (opcional):** `Data/Events/Farm` — Elena visita e planta 1 acácia demonstrativa. Preconditions: `Year 4+ / HostMail livingroots_year4 / !SawEvent <id>`, `Time 800 1200`, `!FestivalDay`. Comandos: `speak/move/faceDirection/emote/pause/addItem (O)LivingRoots.AcaciaSeed/question fork` (aceitar muda?), `mail` flag, `skippable` + `setSkipActions AddItem (O)LivingRoots.AcaciaSeed 1` (anti-softlock se pular), `broadcastEvent` (multiplayer).
- **ActionTiles:** nenhum obrigatório; plantio usa mecânica vanilla de semente de árvore. `PlantableLocationRules.DeniedMessage` cobre feedback.

---

## 5. UI / Interação / Sons

- **Overlay:** estender `IVisualizationService` — aura ICLF no hover da enxada (tint verde `#15803D` opacidade 0.5, pattern `Dots` para daltônicos; borda pontilhada em tile com árvore próxima). Budget `MaxRenderTimeMs = 16.67`, `TooltipDebounceMs = 50`. Sem hardcode em controller.
- **Tooltip:** "Nitrogen aura +3/d — drought immune" ao mirar tile na aura / árvore (reutilizar `maturity-tooltip` pattern com `{0}`).
- **HUD/menu:** nenhum menu novo (plantio/colheita/shake vanilla). Loja Elena usa `Utility.TryOpenShopMenu` existente.
- **Sons:** vanilla (`axechop`, `leafrustle`, `purchaseClick`). Sem `.ogg` custom neste asset. Futuro: `Assets/Audio/iclf_plant.ogg` (placeholder, não wired).

---

## 6. i18n (adicionar a `en.json` + `default.json`, padrão `snake.case`)

```json
{
  "iclf.acacia.name": "Acacia",
  "iclf.acacia-seed.name": "Acacia Seeds",
  "iclf.acacia-seed.description": "Nitrogen-fixing tree. Slow to grow, generous forever. Plant on the farm.",
  "iclf.gliricidia.name": "Gliricidia",
  "iclf.gliricidia-seed.name": "Gliricidia Seeds",
  "iclf.gliricidia-seed.description": "Tropical nitrogen-fixer. Loves warmth, sleeps in winter. Feeds soil and shade.",
  "iclf.denied-message": "ICLF trees only thrive on farm soil.",
  "iclf.aura-tooltip": "Nitrogen aura +{0}/d — drought immune",
  "iclf.event.intro": "Elena: This acacia will feed your soil for decades. Plant it where crops struggle.#$e#",
  "message.iclf.planted": "Planted! The soil will thank you in a few seasons."
}
```

Todo `DisplayName`/`Description`/`DeniedMessage`/diálogo como string tokenizável desde o dia 1 (`{{i18n:chave}}`). PT-BR depois, chaves idênticas.

---

## 7. Gates / desbloqueio / balanceamento

| Gate | Proposta (spec confirma) |
|---|---|
| Desbloqueio | `YEAR 4` **OU** progresso (recomendado: 60% solo médio + cozinha? não — ICLF é base, gate leve): `PLAYER_HAS_MAIL livingroots_year4 OR YEAR 4`. Não travar só por relógio se save antigo Year 8 sem progresso — mail fallback dia 2 do Year 4 |
| Preço/disponibilidade | 800g/450g, estoque 2/dia `AvailableStockLimit: Player` (farmhands não punidos) |
| Crescimento | `GrowthChance` 0.08–0.10 (lenta), fertilizante 0.6; `SeedSpreadChance` ≤0.03 |
| Aura | Raio 6, +3/dia, teto = não passa de 85 via aura (os últimos 15 exigem compost/esterco — anti-trivialização; alimenta meta >80% sem dar de graça) |
| Seca | 1 dia sem rega sem penalidade + crescimento ×0.85 dentro da aura |
| Multiplayer (D1, Constituição v2 obrigatório) | Aura calculada host-side, `Context.IsMainPlayer` guards; estado farm-keyed (`SaveIdProvider`); ouro/amizade por jogador |
| Saves existentes | Gating por mail/Year evita aplicação retroativa quebrada; árvores plantadas antes do mod = ignoradas (IDs desconhecidos) |
| Anti-cheat/overflow | `CheckForOverflowUnderflow=true`; aritmética aura com clamp `MinSoilHealth/MaxSoilHealth`; logs só `ex.GetType().FullName + HResult` em Trace |

---

## 8. Constantes novas propostas (`Constants.cs`, classe `ModConstants`)

```csharp
public const string IclfAcaciaId = "LivingRoots.Acacia";
public const string IclfGliricidiaId = "LivingRoots.Gliricidia";
public const string IclfAcaciaSeedId = "LivingRoots.AcaciaSeed";
public const string IclfGliricidiaSeedId = "LivingRoots.GliricidiaSeed";
public const string QualifiedIclfAcaciaSeedId = "(O)LivingRoots.AcaciaSeed";
public const string QualifiedIclfGliricidiaSeedId = "(O)LivingRoots.GliricidiaSeed";
public const int IclfAuraRadius = 6;
public const float IclfNitrogenPerDay = 3f;
public const float IclfAuraCeiling = 85f;
public const double IclfGrowthChanceAcacia = 0.08;
public const double IclfGrowthChanceGliricidia = 0.10;
public const double IclfSeedSpreadChance = 0.03;
```

---

## 9. Riscos, decisões abertas e teste

- **Riscos:** arte 4 estações × 2 árvores = maior custo de asset do End Game; compat com mods que editam `Data/WildTrees` (usar chave própria + `EditData`, nunca overwrite); aura permanente pode matar aspersores (contrabalanços §7); `Textures[] Condition` não reage a hora-do-dia sem trigger manual.
- **Decisões para `speckit.clarify`:** definição exata de "seca" vanilla; teto da aura; gliricídia dá forragem via tapper? (extensão explícita **não-prometida**); evento de desbloqueio obrigatório ou só loja?
- **Teste:** unitário (aura: raio/teto/cap perf; plantable rules); integração SMAPI (registro `Data/WildTrees` + `Data/Objects` + `Data/Shops`, `SpawnTree` parseia, evento dispara/pula com `setSkipActions`); manual in-game (plantar, dormir 14 dias com/sem fertilizante, checar solo, shake/chop drops) via checklist do PR template.

## Fontes

- Internas: `ROADMAP.md:59`, `EPICS_USER_STORIES.md:92-97`, `AGROECOLOGY_CONCEPTS.md:41`, agrupado §2.
- Externas (wiki 1.6+, verificadas): `Modding:Wild_trees` (SeedItemId/SeedPlantable/GrowthChance/FertilizedGrowthChance/SeedSpreadChance/SeedOnShakeChance/SeedOnChopChance/TapItems/ShakeItems/ChopItems/PlantableLocationRules/CustomFields/Textures Season-Condition/SpawnTree wild/Paths tile 34); `Modding:Maps` (.tmx > .tbin, EditMap > Load, prefixo `z_`, layers); `Modding:Event_data` (preconditions `!`, speak/move/question fork/farmerEat/addItem/friendship/mail/setSkipActions/skippable/broadcastEvent); `Modding:Shops`; `Modding:Objects`.
