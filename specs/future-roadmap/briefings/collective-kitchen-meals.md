# Briefing — Collective Kitchen / Community Meals (Epic 18 · US-18-01 #60 / US-18-02 #61 / US-18-03 #62)

> **Escopo:** `ROADMAP.md:63` — "Abandoned JojaMart (if Joja path) does NOT become a movie theater. Becomes 'Living Roots Collective Kitchen'. Town-wide construction project. Sunday evening 'Kitchen Work Party'. Cooking minigame (Luau-like). Result: 'Community Meals' (powerful buffs for everyone) + Food Sovereignty".
> **Epics:** Epic 18 (Issue #20): US-18-01 (#60) transformação, US-18-02 (#61) work party + minigame, US-18-03 (#62) refeições/buffs.
> **Status:** sem spec/plan/tasks. **Maior bloco do End Game (GG)** — fatiar em 3 specs (uma por US), não uma.
> **Leitura interna:** agrupado §7, `EPICS_USER_STORIES.md:117-122`.
> **Maior risco de compat/save do End Game:** gating cinema-vs-cozinha + migração de save existente.

---

## 1. Conceito de jogo

Se rota Joja, o JojaMart abandonado vira **Cozinha Coletiva** (projeto de construção da cidade via doações), não cinema. **Domingo à noite: Work Party** — jogador + moradores trazem ingredientes; **minigame culinário** (espírito do Luau: qualidade da contribuição); resultado em tiers: **Community Meals** (buffs poderosos para quem comeu + amizade cidade + progresso soberania). "Buffs para todos" = buff mecânico **só para quem comeu** (NPCs não têm stats de buff — recebem friendship/diálogo; especificar para não prometer impossível).

Fatiamento obrigatório: US-18-01 (gating + projeto + mapa) / US-18-02 (evento semanal + minigame) / US-18-03 (receitas + itens + buffs).

---

## 2. Sprites

| Asset | Spec | Arquivo sugerido |
|---|---|---|
| Interior cozinha (tiles) | `.tmx` tiles bancadas/fogões/mesas | tileset `z_kitchen.png` — bancada madeira, fogão ferro, panelas |
| Fogão coletivo (máquina decorativa/funcional) | **16×32** big-craftable | `Assets/collective_stove.png` (3 estágios: apagado/aceso/pronto) |
| Community Meals (3–5 pratos) | **16×16** cada | `Assets/meals.png` (spritesheet 5): Moqueca Coletiva, Pirão Soberano, Salada Policultura, Caldo Farmácia Viva, Pão Forno Adobe |
| Avental/boné cozinha (cosmético, opcional) | hats/shirts | cortar Y1 (extensão não-prometida) |
| Pôster cozinha (town) | mail/notice sprite | reutilizar vanilla |

Reaproveitar interior JojaMart via `EditMap` (não `Load` integral): bancadas como máquinas `Data/Machines` decorativas/funcionais, `Action` tiles cozinhar, `ChairTiles` banquete.

---

## 3. Data JSON

```csharp
// ---- US-18-01: projeto coletivo (SpecialOrders de doação — fantasia coletiva) ----
data["LivingRoots.KitchenBuild_Wood"] = new SpecialOrderData {
    Name = "{{i18n:kitchen.build.wood.name}}", Requester = "Robin",
    Duration = "Month", Repeatable = false,
    RequiredTags = "mail_livingroots_kitchen_unlocked",
    OrderType = "", // board town vanilla (visível p/ cidade toda)
    Objectives = new[] {
        new { Type = "Donate", RequiredCount = 200,
              Data = new { DropBox = "KitchenSite", DropBoxGameLocation = "Town",
                           DropBoxIndicatorLocation = "<joja-door>",
                           AcceptedContextTags = "item_wood" } },
    },
    Rewards = new[] {
        new { Type = "Mail", Data = new { MailReceived = "livingroots_kitchen_wood_done", NoLetter = "true" } },
    },
};
// + ordens pedra (100), ouro (10000 via Money? — doação ouro não é Donate; usar Ship? spec decide:
//   alternativa: Marshal via trigger AddMoney em evento; registrar como decisão)

// ---- US-18-03a: receitas ----
data["LivingRoots.MoquecaColetiva"] = new CookingRecipeData {
    // Ingredientes = pares itemID qty; yield 3; unlock por evento (adicionado via AddCookingRecipe trigger)
    // ex.: "(O)LivingRoots.HeirloomTomato 3 (O)198 2 (O)767 1" → yield "(O)LivingRoots.MoquecaColetiva 3"
    // Unlock: "event LivingRoots_KitchenOpening" (ou f <NPC> <hearts> carta)
};

// ---- US-18-03b: itens ----
data["LivingRoots.MoquecaColetiva"] = new ObjectData {
    Name = "Moqueca Coletiva", DisplayName = "{{i18n:meal.moqueca.name}}",
    Description = "{{i18n:meal.moqueca.description}}",
    Type = "Cooking", Category = -7, // OBRIGATÓRIO p/ Gourmet Chef/Perfection!
    Price = 450, Edibility = 120, IsDrink = false,
    Texture = "Mods/LivingRoots/Meals", SpriteIndex = 0,
    Buffs = new[] {
        new { Id = "MealBuff", BuffId = "LivingRoots.WellFedCommunity",
              Duration = 300 /* minutos in-game; -2 = resto do dia */ },
    },
    ContextTags = new[] { "community_meal", "cooked_item" },
};

// ---- US-18-03c: buffs ----
data["LivingRoots.WellFedCommunity"] = new BuffData {
    DisplayName = "{{i18n:buff.community.name}}",
    Description = "{{i18n:buff.community.description}}",
    Duration = -2, // resto do dia (ou 3600000ms explícito — spec trava)
    IconTexture = "Mods/LivingRoots/BuffIcons", IconSpriteIndex = 0,
    // CustomAttributes (spec calibra "poderoso sem quebrar mina"):
    //   Tier S (contribuição iridium): FarmingLevel +2, MaxStamina +50
    //   Tier A: FarmingLevel +1, Speed +1
    //   Tier B: MaxStamina +25
    // GlowColor opcional; IsDebuff false
    Effects = new { FarmingLevel = 2, MaxStamina = 50 },
};

// ---- SHOPS n/a (refeições não vendidas; ingredientes via Fair/CSA) ----
```

Gating cinema (decisão crítica — spec dedicada US-18-01): vanilla vira **Movie Theater** (`Movie_Theater`, 9h–21h, 1 filme/semana) após Community Center (ou upgrades Joja) + cutscene rainy-day "Missing Bundle". Divergência LivingRoots: **condicional** (só se rota comunitária + cozinha construída?) ou total? E save com cinema existente? Migração/gating por GSQ (`CommunityCenterOrWarehouseDone`? mail flags? bundle completo?) **obrigatória**. Recomendado: cozinha = caminho alternativo (se cinema existe, cozinha vai para outro prédio? — não; spec decide com migração explícita, nunca overwrite silencioso).

---

## 4. Mapa / Evento

- **Mapa:** interior JojaMart reaproveitado via `EditMap` (`.tmx` > `.tbin`, prefixo `z_`): bancadas, fogões (máquinas), `Action` tiles cozinhar (`Action Cook`), `ChairTiles` banquete, warps preservados. `Data/Locations` entry se virar location própria (`LivingRoots_Kitchen`: `DisplayName`, `Music`, `AmbientLight`); senão mantém `JojaMart` com tiles trocados + `HasIndoorsName`? (spec verifica).
- **Construção:** sequência SpecialOrders doação (cap diário) + `Mail` progresso + evento(s) inauguração (`Data/Events/JojaMart`: preconditions `HostMail livingroots_kitchen_funded`, `SawEvent`?, clima qualquer).
- **Work Party dominical (US-18-02):** evento semanal `Time 1700 2100`, `DayOfWeek Sunday`, `!FestivalDay`, todo ano. Estrutura `Data/Events/Town|JojaMart`: `music`, câmera, posições, `speak/move/faceDirection/emote/pause`, `question fork` (minigame: quiz ingredientes + contribuição-por-qualidade — YAGNI: começar simples, menu C# barra-tempo só se playtest pedir), `farmerEat`, `addItem`, `friendship`, `mail`, **`setSkipActions` OBRIGATÓRIO** (recompensa parcial se pular — evento repetível!), `skippable`, `broadcastEvent` (multiplayer).
- **Minigame (MVP):** cada jogador deposita 1–3 ingredientes (tags sazonais, qualidade conta); tier = média qualidade × variedade (reutiliza cotas Fair!); `quickQuestion` para quiz ("qual combinação alimenta mais solo?"). Sem física nova.
- **Layers exemplo:** Back (piso) / Buildings (bancadas, parede salvo Passable) / Paths (tapetes) / Front (prateleiras) / AlwaysFront (vigas).

---

## 5. UI / Interação / Sons

- **UI:** sem menu novo no MVP — quiz via `question`/`quickQuestion` scripts (`(break)`); depósito via dropbox/chest UI vanilla; resultado via diálogo + `farmerEat` + carta. Menu C# custom (barra tempo/combinação) = extensão não-prometida.
- **Limite vanilla a respeitar:** 1 comida + 1 bebida simultâneas — Community Meal como comida "dominante" (trade-off vs. buffs mina/pesca = design interessante, documentar no tutorial).
- **Duração buff:** minutos in-game (42s reais = 1h jogo; Skull 54s); `-2` = resto do dia.
- **Sons:** `cooking`/`eat`/`gulp`/`gift`/`festivalScore` (confirmar cues); ambiente cozinha `bubbles`? Sem `.ogg` custom MVP.

---

## 6. i18n

```json
{
  "kitchen.title": "Living Roots Collective Kitchen",
  "kitchen.build.wood.name": "Beams for the Kitchen",
  "kitchen.party.invite": "Sunday Work Party at the Kitchen — bring something fresh!",
  "kitchen.quiz.q1": "What feeds the soil fastest?$#Compost$#Joja Fertilizer$#Nothing, soil feeds itself#(break)",
  "meal.moqueca.name": "Moqueca Coletiva",
  "meal.moqueca.description": "The valley in a pot. Feeds everyone who helped.",
  "buff.community.name": "Well-Fed Community",
  "buff.community.description": "+2 Farming until rest. Cooked together, stronger together.",
  "message.kitchen.tier-s": "Legendary feast! The whole town glows.",
  "message.kitchen.skipped": "You skipped the party — Elena saved you a bowl. (Weak buff)"
}
```

---

## 7. Gates / desbloqueio / balanceamento

| Gate | Proposta |
|---|---|
| Desbloqueio projeto | Rota Joja + Year 4 + Fair aberta + CSA ≥2 assinantes (cozinha coroa o arco, não abre cedo) |
| Custo coletivo | 200 wood + 100 stone + 10000g (via evento) + 1 ordem ouro; ritmo ~1 estação; cap diário doação anti-whale |
| Contribuição semanal | 1–3 ingredientes, tags sazonais, qualidade mínima normal; sem contribuição = sem meal (ou bowl fraco via skip) |
| Tiers buff | S (média iridium + ≥3 categorias): +2 skill resto do dia; A: +1 skill +1 speed; B: +25 stamina; nunca +3 skill (quebrado mina/caverna) |
| Multiplayer | Contribuição por jogador; buff quem comeu; `broadcastEvent` força warp; ouro/amizade por jogador |
| Cinema existente | Migração explícita (GSQ + mail + diálogo Lewis; nunca apaga cinema sem consentimento em evento com fork) |
| Segurança | `checked` doações/contadores; Trace-only exceções; cap tiles/evento |

---

## 8. Constantes novas

```csharp
public const string KitchenOrderType = "LivingRoots_Kitchen";
public const string KitchenDropBoxId = "KitchenSite";
public const string KitchenOpenMail = "livingroots_kitchen_open";
public const string KitchenFundedMail = "livingroots_kitchen_funded";
public const int KitchenWoodNeed = 200;
public const int KitchenStoneNeed = 100;
public const int KitchenGoldNeed = 10000;
public const string CommunityBuffId = "LivingRoots.WellFedCommunity";
```

Refeições (IDs): `LivingRoots.MoquecaColetiva`, `LivingRoots.PiraoSoberano`, `LivingRoots.SaladaPolicultura`, `LivingRoots.CaldoFarmacia`, `LivingRoots.PaoAdobe` (+ `(O)` qualified).

## 9. Riscos e teste

- **Riscos:** GG = mapa + evento semanal + minigame + buffs + gating cinema (fatiar!); `Category -7` errada quebra Gourmet Chef; buff "para todos" impossível em NPCs (escopo: só jogadores); `Duration -2` vs. ms (travar unidade!); evento semanal sem `setSkipActions` = softlock pular.
- **Teste:** unit (tiers por qualidade/variedade, buff attributes, doação caps); integração (orders completam → flag → evento inauguração; party dispara domingo/não-festival; skip dá bowl fraco; cinema gating por GSQ); manual (construir, 3 parties S/A/B/skip, comer + checar buff + limite 1 comida, save com cinema existente).

## Fontes

- Internas: `ROADMAP.md:63`, Epic 18, agrupado §7.
- Externas: `Modding:Special_orders` (Donate/DropBox/Repeatable); `Modding:Event_data` (preconditions Time/DayOfWeek/Friendship/SawEvent/HostMail/Year/!/music/speak/move/faceDirection/emote/pause/question fork/quickQuestion/farmerEat/addItem/friendship/mail/setSkipActions/skippable/broadcastEvent); `Modding:Recipe_data` (`Data/CookingRecipes` ingredientes/yield/unlock `f`/`s`/evento); `Modding:Objects` (Type Cooking/**Category -7**/Edibility ×2.5 energia ×1.125 vida/IsDrink/Buffs[] BuffId); `Modding:Buffs` (`Data/Buffs`: Duration ms, `-2` resto do dia, CustomAttributes FarmingLevel/Speed/MaxStamina/MagneticRadius/Defense, GlowColor, IsDebuff, IconTexture 16×16); `Modding:Maps` (.tmx/EditMap/`z_`); Movie Theater (pós-Missing Bundle, 9h–21h).
