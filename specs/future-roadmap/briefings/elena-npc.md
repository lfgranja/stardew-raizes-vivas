# Briefing 1:1 — Elena NPC (mentora Year 1)

> Status: briefing (não implementado). Agrupado de origem: `specs/future-roadmap/briefings/early-game-year1.md` Cap. 1 (preservado, não apagar).
> Escopo interno: `ROADMAP.md:30-40` (Early Game) · Epic 5 — US-05-01 #32 (criar NPC) / US-05-02 #33 (guidance agroecológica) em `LivingRoots/docs/EPICS_USER_STORIES.md:33-37`.
> Base conceitual: `LivingRoots/docs/AGROECOLOGY_CONCEPTS.md:32-40` (agroecologia como ciência/prática/movimento; Primavesi — vida do solo).
> Não promover para `specs/` sem `/speckit.specify`.

## 1. Conceito

Elena é agrônoma e ativista, mentora do jogador no Year 1 (`ROADMAP.md:34`). Funções:

1. Entregar o Welcome Kit (ver `welcome-kit.md`) via evento/mail dia 1–2;
2. Vender sementes heirloom (ver `heirloom-seeds.md`), receita de mulch (ver `mulch-soil-cover.md`) e receita da cisterna (ver `small-cistern.md`) em loja própria;
3. Ensinar 1 conceito/semana via diálogos rotativos (composto → mulch → policultura → heirloom → cisterna) e quests com gating (entregar 5 compost → receita mulch; plantar Three Sisters → desconto heirloom);
4. Antagonizar o fertilizante Joja — odeia o item como presente (lore do triângulo ideológico Elena × Pierre × Joja; ver `joja-fertilizer.md`).

Decisões travadas: `CanBeRomanced = false` no Y1 (revisitar pós-Year-3); `HomeRegion = Town` para entrar na introductions quest, Winter Star e Luau; `FlowerDanceCanDance = false` no Y1 (sem frames de dança); quarto placeholder no SeedShop (backroom próprio é spec futura).

## 2. Sprite specs

| Asset | Spec (wiki `Modding:NPC_data`, verificada 2026-10-05) | Arquivo sugerido |
|---|---|---|
| Overworld | `Characters/LivingRoots_Elena`, **16×32 por frame**; mín. 16 frames de movimento (4/dir: down/up/left/right, frames 0–15). Kiss frame omitido (não-romanceável). `Size` default (16,32) — nunca maior (quebra spawn/pathfinding/perfection slideshow) | `LivingRoots/Assets/NPC/elena_sprites.png` |
| Retrato | `Portraits/LivingRoots_Elena`, **64×64 por emoção**; mín. 6 (neutro + `$h` feliz + `$s` triste + `$a` bravo + surpreso + especial festa). Primeiro retrato = default | `LivingRoots/Assets/NPC/elena_portraits.png` |
| Sono | `Data/animationDescriptions["elena_sleep"]` (nome lowercase + `_sleep`) | data only |

Paleta: camisa verde-caqui (agroecologia), calça marrom, lenço terracota; legível em 16×32 (evitar detalhe < 2px). `Breather: true` (default). MugShot default por Age=Adult `(0,0,16,24)`.

## 3. Data/* — JSON/C# (registro via `ModEntry.OnAssetRequested`, padrão `ModEntry.cs:114-177`)

```csharp
// Data/Characters — Load: Characters/LivingRoots_Elena, Portraits/LivingRoots_Elena,
//   Characters/Dialogue/LivingRoots_Elena, Characters/schedules/LivingRoots_Elena
data["LivingRoots_Elena"] = new CharacterData
{
    DisplayName = "{{i18n:elena.name}}",
    Gender = "Female", Age = "Adult",
    Manner = "Polite", SocialAnxiety = "Outgoing", Optimism = "Positive",
    BirthSeason = "spring", BirthDay = 18, HomeRegion = "Town",
    CanBeRomanced = false, // Y1; revisitar pós-Year-3
    Calendar = "HiddenUntilMet", SocialTab = "UnknownUntilMet",
    Home = new[] { new HomeEntry { Id = "Default", Location = "SeedShop",
        Tile = new Point(2, 8), Direction = "down" } },
    // TODO specs: quarto próprio (SeedShop backroom) vs. casa nova
};

// Data/NPCGiftTastes — 5 pares diálogo+IDs: Love/Like/Dislike/Hate/Neutral
data["LivingRoots_Elena"] =
    "{{i18n:elena.gift.love}}/LivingRoots_Compost LivingRoots_HeirloomParsnip/.../" +
    "{{i18n:elena.gift.like}}/.../.../" +
    "{{i18n:elena.gift.dislike}}/.../.../" +
    "{{i18n:elena.gift.hate}}/LivingRoots_JojaFertilizer/.../" + // odeia Joja (lore!)
    "{{i18n:elena.gift.neutral}}/...";

// Data/animationDescriptions
data["elena_sleep"] = "50/50/50";
```

Schedule Y1 mínimo (`Characters/schedules/LivingRoots_Elena`, formato `HHMM Local X Y facing`):

```json
{ "spring": "800 SeedShop 2 8 0/1200 Town 47 87 0/1700 SeedShop 2 8 2",
  "summer": "800 SeedShop 2 8 0/1200 Beach 20 10 0/1700 SeedShop 2 8 2",
  "fall":   "800 SeedShop 2 8 0/1200 Town 47 87 0/1700 SeedShop 2 8 2",
  "winter": "900 SeedShop 2 8 0/1500 Saloon 10 20 2/2000 SeedShop 2 8 2",
  "rain":   "800 SeedShop 2 8 0/1800 SeedShop 2 8 2" }
```

Diálogo (`Characters/Dialogue/LivingRoots_Elena`): `Introduction`, `Mon`–`Sun` rotativo (1 conceito/semana), `AcceptGift_*`, `FlowerDance_Decline`. Quest-gating via `Data/Quests`. Espelho Content Patcher: `Load` (sprites/retratos/diálogo/schedule) + `EditData` (`Data/Characters`, `Data/NPCGiftTastes`, `Data/animationDescriptions`) — exemplo canônico Dobson em `Modding:NPC_data#Full_NPC`; default do mod é C# com bloco CP pronto (decisão transversal do agrupado).

### 3.1 Loja própria (`Data/Shops`, wiki `Modding:Shops`)

Nova shop `LivingRoots_ElenaShop`, aberta via `Utility.TryOpenShopMenu` em diálogo ou tile `Action OpenShop` no SeedShop. `Owners: [{ Name: "LivingRoots_Elena", Portrait, Dialogues: [...] }]`. Itens Y1: heirlooms, receita mulch (100g), receita cisterna (200g). Desconto por amizade via `PriceModifiers` + `Condition: "PLAYER_HEARTS LivingRoots_Elena 4"`. `AvailableStockLimit: Player` (Principle VI — não punir farmhands). `OpenSound: dwop`, `PurchaseSound: purchaseClick` (defaults vanilla).

## 4. Sons

Nenhum custom Y1 — reutilizar cues vanilla de diálogo (`talkToNPC`) e loja (`dwop`/`purchaseClick`). Futuro: `LivingRoots/Assets/Audio/elena_greeting.ogg` (padrão já usado: `composting_bin_{processing,ready}.ogg` copiados pelo `.csproj`, carregamento futuro via `ModContent`).

## 5. i18n (adicionar a `en.json` + `default.json`; PT-BR depois, chaves idênticas; padrão `snake.case` com prefixo)

```json
{ "elena.name": "Elena", "elena.title": "Agroecologist",
  "elena.introduction": "You must be the new farmer! I'm Elena — I study living soil...",
  "elena.dialogue.monday": "Bare tilled soil loses life every day. Cover it with mulch!#$e#",
  "elena.gift.love": "For my compost trials? Wonderful!",
  "elena.gift.hate": "Joja fertilizer? Keep that poison away from my plots.",
  "elena.shop.title": "Have a look at what really feeds the soil.",
  "message.elena.welcome-kit": "Elena gave you an Agroecological Welcome Kit!" }
```

`DisplayName`/diálogos aceitam `{{i18n:chave}}` (padrão Dobson).

## 6. Arquitetura / multiplayer (Principle VI, constituição v2.0.0)

- DDD: nenhum serviço novo obrigatório; loja/quests via `ModController` (orquestrador, `ExecuteWithConcurrencyGuard`); `new` só em `ModEntry.cs`.
- Sem estado single-player-only: flags de quest/fallback por fazenda (farm-keyed, `SaveIdProvider`); NPC vanilla já é world-state.
- Nota D3/assessment: primeiro task da spec = spike CP-vs-C# (briefing assume C# default).

## 7. Testes / gates

- `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → build com `GamePath=` (ModBuildConfig exige; ver `AGENTS.md`) → `dotnet test` (baseline 816 testes).
- Novos testes: `Data/Characters` contém `LivingRoots_Elena` com `HomeRegion=Town`, `CanBeRomanced=false`; gift-tastes com Joja em Hate; schedule parseável (5 chaves: 4 estações + rain); chaves i18n presentes em `en.json` + `default.json`.
- Critérios de aceite: NPC spawna no SeedShop dia 1; introductions quest conta Elena; loja abre com 3+ itens; fallback mail funciona se evento pulado; `dotnet format` limpo.
- Armadilhas conhecidas: NPC novo em save existente só segue schedule após 1 sono (limitação wiki); trailing newline em `.md` novos (gate `ArchitecturalPlanTests`); não tocar nos 3 `"(Planned)"` do `README.md` (gate `FeatureStatusTests`).

## 8. Constantes novas (`LivingRoots/Constants.cs`, classe `ModConstants`)

```csharp
public const string ElenaNpcId = "LivingRoots_Elena";
public const string ElenaShopId = "LivingRoots_ElenaShop";
```

## 9. Dependências / ordem

Último do arco Y1 (ordem 80/20 do agrupado: mulch → joja → heirloom → cisterna → kit → **Elena**). Maior asset (sprites + schedule + diálogos + loja) — coroa o arco; loja/itens referenciados vêm dos outros 5 briefings.
