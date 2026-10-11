# Briefing — Weekly Fair / Short Supply Chains (Epic 16 · US-16-01 #56 / US-16-02 #57)

> **Escopo:** `ROADMAP.md:61` — "Weekly Fair (every Saturday) in the town square. +25% profit and friendship. Residents want variety (polyculture); won't buy 100 Ancient Fruit Wines — want 2 Breads, 1 Milk, 3 Tomatoes, 1 Jelly".
> **Epics:** Epic 16 (Issue #18): US-16-01 (#56) feira semanal, US-16-02 (#57) lucro+amizade diretos.
> **Status:** sem spec/plan/tasks. Insumo para `speckit.specify`.
> **Leitura interna:** agrupado §5, `AGROECOLOGY_CONCEPTS.md:55` (economia solidária: feiras, cooperativas, CSA), `EPICS_USER_STORIES.md:105-109`. Antecede `csa-box.md` (lógica variedade/cotas reutilizada na cesta CSA).
> **Tese:** o assassino mecânico da monocultura late-game — o vanilla colapsa em Ancient Fruit Wine; a Fair paga mais mas só por variedade.

---

## 1. Conceito de jogo

Todo **sábado na praça**: vender direto = **+25% lucro + amizade**. Contrapeso: cada morador tem **cotas por categoria/semana** (ex.: até N por categoria; excedente recusado ou a preço base sem bônus). Demanda determinística por seed semanal + rotativa por estação (especificar). Fair suspensa em dia de festival anual (`!FestivalDay`) — especialmente a Fair de outono que usa a praça.

Desbloqueio recomendado: por **progresso, não relógio** (ex.: solo médio 40%+ ou 1º premium sombreado ou mail Elena) + `YEAR 4` como fallback — trava por ano puro pune quem chega atrasado e presenteia quem rusha sem agroecologia.

---

## 2. Sprites

| Asset | Spec | Arquivo sugerido |
|---|---|---|
| Banca feira (big-craftable/building prop) | **16×32** big-craftable ou tile mapa | `Assets/fair_stall.png` — banca listrada verde-terracota com toldo |
| Sacola cesta feira (item token, opcional) | **16×16** | `Assets/fair_basket.png` |
| Decoração praça (bandeirinhas, placas) | tiles `.tmx` | tileset `z_fair.png` |
| Retrato extra NPCs feirantes | reutilizar `Portraits/*` vanilla | nenhum novo |

Sem NPC novo; barracas via `EditMap` temporário (sábado) ou `temporarySprite`/`addProp` em evento — nunca mapa permanente que conflite com festivais.

---

## 3. Data JSON

```csharp
// Data/Shops — Fair como loja temporária (COMPRA pelo jogador continua vanilla;
// VENDA jogador→loja exige menu C# — ver §5. Shop cobre o lado "moradores vendem ingredientes raros"?)
data["LivingRoots.WeeklyFair"] = new ShopData {
    Owners = new[] {
        new ShopOwnerData { Name = "Gus", Dialogues = new[] {
            "{{i18n:fair.gus.open}}", "{{i18n:fair.gus.bye}}" } },
        new ShopOwnerData { Name = "Evelyn" },
    },
    Currency = 0, // gold
    PriceModifiers = new[] {
        new QuantityModifier { Modification = "Multiply", Amount = 1.25f,
            Condition = "DAY_OF_WEEK Saturday, LOCATION_IS Town" },
    },
    Items = new[] {
        // estoque rotativo/limitado: sementes heirloom, receitas, ingredientes sazonais
        new ShopItemData { Id = "LivingRoots.FairHeirloom", ItemId = "(O)LivingRoots.HeirloomParsnipSeeds",
            Price = 30, AvailableStock = 10, AvailableStockLimit = "Player",
            Condition = "SEASON spring" },
        // ... (spec lista 6-10 itens sazonais com Condition SEASON/PLAYER_HEARTS/mail)
    },
    // Condition de abertura: sábado + sem festival + progresso
    // (Conditions em ShopData? se não houver, gating via Action OpenShop tile + C# horário)
};

// Anti-monocultura (C# — coração do design, NÃO é data-only):
// IFairQuotaService (Domain/Interfaces → Services/):
//   cotas: Dictionary<(npcId, categoriaOuTag, semanaId), vendidos>
//   categorias = Category + ContextTags dos Data/Objects
//   ex.: Gus: Bread×2, Milk×1, Tomato×3, Jelly×1/semana a +25%; excedente → preço base, sem amizade
//   semanaId = Year*100+SeasonIndex*30+WeekOfSeason (determinístico, reseta sábado)
// Demanda rotativa: tabela por estação (seed semanal): ex. semana 1 Gus quer Tomato, semana 2 quer Corn...
```

Fórmula +25%: `soldPrice = floor(vanillaSell * 1.25)` por item dentro da cota, `checked`, arredondamento documentado. Base = preço de venda (não preço Pierre). Amizade: `changeFriendship(npc, +30/entrega dentro da cota)` (250 = 1 coração), cap semanal por NPC (ex.: 120) anti-farm.

---

## 4. Mapa / Evento

- **NÃO usar `Data/Festivals`** para todo sábado — colidiria com Luau/Dance/Fair anual e `FestivalDay` preconditions. Implementar como **evento recorrente de localização**: NPCs agendados na praça sábado (`Characters/schedules/<ID>` patch: `Sat: "1000 Town 47 87 0/..."`), barracas via `EditMap` com `When: DAY_OF_WEEK Saturday` (Content Patcher condicional) ou props temporários via C# `DayStarted`.
- **Local:** praça (Town), sub-área centro (tiles 40–55 × 80–95 — spec confirma coordenadas no `.tmx` real). Suspensa se festival anual (`!FestivalDay`, `!WeddingDay`?).
- **Tiles:** `EditMap Maps/Town`: `AddWarps` n/a; `MapTiles` (Buildings layer: bancas com `Action OpenShop LivingRoots.WeeklyFair` + `Action SellToFair` custom C#); `CollisionMap`/`NoSpawn` temporários; música via `Data/Locations` music override sábado (`MusicContext`? spec verifica campo).
- **Evento de inauguração (one-shot):** `Data/Events/Town` — Elena + Lewis abrem a feira. Preconditions: `Year 4 / mail livingroots_fair_unlocked / !SawEvent <id> / Season spring? / Time 900 1200 / !FestivalDay`. Script: `speak/move/emote/pause/question fork (participar?)/friendship/addConversationTopic/mail/skippable/setSkipActions (flag + presentear 1 cesta)/broadcastEvent`.
- **Semanais seguintes:** sem cutscene (só schedules + loja) — repetir cutscene todo sábado seria insuportável; opcional diálogo rotativo via `addConversationTopic` semanal.

---

## 5. UI / Interação / Sons

- **Venda (jogador→morador):** shops vanilla são **compra**; venda direta exige C# — opções: (a) **menu custom SMAPI** (recomendado: controle total variedade/+25%/amizade; `IClickableMenu`, suporte controller+teclado, WCAG gaming: fonte escalável, contraste, sem info só-por-cor); (b) clones `Shipping Bin` com taxa (menos controle). Híbrido sugerido: banca central (menu único rápido) + diálogos de entrega para CSA (charme) — sem duplicar com `csa-box.md` (entrega CSA **não** gera +25% — anti-double-dip).
- **Cotas visíveis:** menu mostra `2/2 Breads — Gus full` (progresso por NPC/categoria); excedente cinzento com tooltip "quota filled — base price".
- **Overlay:** ícone feira no calendário + marca d'água sábado na praça (bubble map). Sem HUD novo.
- **Sons:** `dwop` (abrir loja), `purchaseClick` (venda), `gift` (amizade), `festivalScore`? (confirmar cues). Sem `.ogg` custom.

---

## 6. i18n

```json
{
  "fair.title": "Weekly Fair",
  "fair.gus.open": "Fresh from your soil, eh? Show me what you've got.",
  "fair.gus.bye": "Variety is the spice, farmer!",
  "fair.quota.full": "{0} already bought enough {1} this week (base price only).",
  "fair.sold.bonus": "+25% direct sale! +{0} friendship with {1}.",
  "fair.closed.festival": "The fair pauses for today's festival.",
  "fair.event.intro": "Elena: No more shipping everything to nowhere. Sell faces, not boxes.#$e#",
  "message.fair.week-reset": "The fair restocked its cravings."
}
```

---

## 7. Gates / desbloqueio / balanceamento

| Gate | Proposta |
|---|---|
| Desbloqueio | Progresso (`AVG_SOIL >= 40` OU 1º premium OU 10 heirlooms colhidas) + mail Elena + `YEAR >= 3` (chega antes do Year 4 para rampar; Year 4+ é a fase, não a tranca dura) |
| Horário | Sábado 9h–16h, praça; `!FestivalDay` → fechada com aviso |
| Cotas | Ex.: por NPC/semana: 2 pães, 1 leite, 3 vegetais, 1 geleia/artesanal (números na spec por estação); excedente = preço base, zero amizade |
| +25% | Sobre sell price vanilla, `floor`, `checked`; cap ouro/dia anti-exploit (ex.: 20k bônus/dia? spec calibra) |
| Amizade | +30/cota preenchida, cap 120/NPC/semana; falha n/a (feira não pune — CSA pune) |
| Multiplayer | Ouro por vendedor; amizade por jogador que vendeu; banca compartilhada (cotas por **time**? ou por jogador? — recomendado por time p/ forçar cooperação; spec decide e documenta) |
| Saves | Flag `livingroots_fair_open`; schedules patchados só com flag (evita NPCs presos em save antigo) |

---

## 8. Constantes novas

```csharp
public const string FairShopId = "LivingRoots.WeeklyFair";
public const double FairBonusMultiplier = 1.25;
public const int FairFriendshipPerQuota = 30;
public const int FairFriendshipWeeklyCap = 120;
public const int FairMaxBonusGoldPerDay = 20000;
public const string FairOpenMail = "livingroots_fair_open";
```

## 9. Riscos e teste

- **Riscos:** menu custom = maior custo UI do End Game (controller, i18n, acessibilidade); conflito praça×festivais anuais; cotas por time vs. jogador (decisão multiplayer com playtest); preço base errado (Pierre vs. sell) gera exploit +25% sobre margem errada.
- **Teste:** unit (cotas reset semanal, determinismo seed, fórmula +25% + caps, amizade caps); integração (shop abre só sábado/sem-festival, schedules NPCs, excedente sem bônus); manual (vender 100 vinhos vs. cesta variada — confirmar recusa/bônus, festival sábado pula feira).

## Fontes

- Internas: `ROADMAP.md:61`, Epic 16, agrupado §5, `AGROECOLOGY_CONCEPTS.md:55`.
- Externas: `Modding:Shops` (Owners/Currency/PriceModifiers/Items Condition/AvailableStock/AvailableStockLimit Player/TargetField append/Action OpenShop); `Modding:Maps` (EditMap/AddWarps/MapTiles/Action/CollisionMap); `Modding:Event_data` + `Modding:Festival_data` (por que NÃO festival); `Modding:Objects` (Category/ContextTags p/ cotas); `Modding:Game_state_queries` (`DAY_OF_WEEK`/`FestivalDay`/`LOCATION_IS`/amizade).
