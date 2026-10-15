# Briefing — Shaded Milk / Soft Wool (Epic 14 · US-14-03 #53)

> **Escopo:** `ROADMAP.md:59` — "Animals grazing under these trees are happier and produce 'Shaded Milk' or 'Soft Wool' (higher quality)".
> **Epics:** Epic 14 (Issue #16): US-14-03 (#53). Depende de `iclf-trees.md` (copa/sombra). Ponte com Mid Game (Mobile Coop + pastejo rotacionado, Epic 10).
> **Status:** sem spec/plan/tasks. Insumo para `speckit.specify`.
> **Leitura interna:** agrupado §3, `AGROECOLOGY_CONCEPTS.md:41` (ICLF), `EPICS_USER_STORIES.md:92-97`, `LivingRoots/Constants.cs:63-74` (padrão item/máquina), `LivingRoots/ModEntry.cs` (composition root único).

---

## 1. Conceito de jogo

Animais pastando sob copa ICLF ficam mais felizes e produzem variantes premium: **Shaded Milk** e **Soft Wool**. É a recompensa do manejo rotacionado — o jogador que integra árvore + pasto + rotação (em vez de confinar no barn) ganha preço premium sem matar o loop artesanal (queijo/tecido ainda valem mais processados).

Decisão de fantasia: premium é **variante de item** (novo `Data/Objects`), não só qualidade iridium do produto vanilla — garante identidade (nome, sprite, tags) e permite `Gourmet Chef`/Perfection sem ambiguidade.

---

## 2. Sprites

| Asset | Spec | Arquivo sugerido |
|---|---|---|
| Shaded Milk (item) | **16×16**, default `Maps/springobjects` ou spritesheet própria `Mods/LivingRoots/ShadedProduce` | `Assets/shaded_milk.png` — jarra de leite com folha/ sombra; gota azulada |
| Soft Wool (item) | **16×16** | `Assets/soft_wool.png` — novelo creme macio vs. wool vanilla áspera |
| Partícula/buff pasto sombreado | reutilizar coração vanilla; opcional overlay sombra | sem asset novo (YAGNI) |
| Ordenha/tosa (animação) | vanilla (`Milk Pail`/`Shears` tools `(T)`) | nenhum sprite novo |

Não criar animal novo (`Data/FarmAnimals` inalterado — vaca/cabra/ovelha vanilla produzem a variante por C#). Alternativa descartada: EAC (Extra Animal Configs) como dependência — YAGNI, adiciona dependência externa para o que C# puro resolve.

---

## 3. Data JSON

```csharp
// Data/Objects — Shaded Milk (réplica categoria vanilla do leite p/ não quebrar achievements)
data["LivingRoots.ShadedMilk"] = new ObjectData {
    Name = "Shaded Milk", DisplayName = "{{i18n:shaded-milk.name}}",
    Description = "{{i18n:shaded-milk.description}}",
    Type = "Basic", Category = -6, // mesma categoria do Milk vanilla (verificar ID exato na spec — atenção Perfection!)
    Price = 300, // vaca large milk ~190-280 → premium +50-100% mas < queijo gold
    Texture = "Mods/LivingRoots/ShadedProduce", SpriteIndex = 0,
    Edibility = 40, // se consumível; senão -300
    Buffs = null,   // leite não dá buff; manter null
    ContextTags = new[] { "milk_item", "shaded_produce" }, // milk_item = Cheese Press aceita!
};

// Data/Objects — Soft Wool
data["LivingRoots.SoftWool"] = new ObjectData {
    Name = "Soft Wool", DisplayName = "{{i18n:soft-wool.name}}",
    Description = "{{i18n:soft-wool.description}}",
    Type = "Basic", Category = -18, // mesma da Wool vanilla (confirmar na spec)
    Price = 750, // wool 340-680 → premium < cloth alta qualidade
    Texture = "Mods/LivingRoots/ShadedProduce", SpriteIndex = 1,
    ContextTags = new[] { "wool_item", "shaded_produce" }, // Loom aceita
};
```

Lógica de produção (C# SMAPI, recomendado — `Data/FarmAnimals` puro não expressa "sob copa"):

```csharp
// Pseudo-contrato p/ spec: IShadedProduceService (Domain/Interfaces → Services/)
// DayStarted/DayUpdate do animal: se AnimalShadeChecker.IsShaded(animal, iclfPositions, radius)
//   && animal.friendship >= threshold → troca ProduceItemIds por variante com prob. P
// Determinismo p/ teste: seed diária + hash animal ID; chance escalonada por felicidade
//   ex.: felicidade max → 100% leite sombreado; média → 50%; baixa → 0%
```

Opções de `Data/FarmAnimals` (se a spec preferir data-only parcial): `ProduceItemIds[]` com `Condition: "PLAYER_SPECIAL_ORDER_RULE_ACTIVE LivingRoots_Shade"` + `MinimumFriendship` — mas condição "sob copa" não existe em GSQ vanilla, então C# é obrigatório para o gatilho espacial. Registrar como decisão travada: **gatilho em C#, itens em Data**.

Preços (spec calibra): Shaded Milk ~1.5× large milk; Soft Wool ~1.5× wool iridium; ambos **abaixo** do artesanal processado (queijo/pano) para não canibalizar Cheese Press/Loom.

---

## 4. Mapa / Evento

- **Sem mapa novo.** Gatilho espacial = posição do animal vs. posições ICLF (`GameLocation.terrainFeatures`, raio `IclfAuraRadius` reutilizado). Vale dentro da fazenda; estabulação (barn fechado) **não** conta — só pasto aberto (decisão travada, spec confirma; evita farm passivo indoor).
- **Mobile Coop (Mid, Epic 10):** coop móvel sob copa = bônus dobrado? Sugerido: raio de checagem +2 se animal saiu de Mobile Coop nas últimas 24h (sinergia rotacional).
- **Evento:** nenhum evento dedicado. Tutorial via carta Elena (`Data/Mail`) ao produzir 1º premium: `Condition: "PLAYER_HAS_ITEM (O)LivingRoots.ShadedMilk"` → mail + `addConversationTopic`. Preconditions padrão (`!FestivalDay` n/a — mail não precisa).
- **ActionTiles:** nenhum. Ordenha/tosa com ferramentas vanilla.

---

## 5. UI / Interação / Sons

- **UI:** nenhuma janela nova. Nome/sprite/price aparecem em inventário, shipping bin, Cheese Press/Loom (via tags). Tooltip vanilla + linha i18n ("from shaded pasture").
- **Overlay (opcional):** coração extra / brilho verde no animal sombreado ao hover (extensão não-prometida; cortar no clarify se estourar).
- **Sons:** vanilla (`Milking`, `shears`, `purchaseClick` na venda). Sem `.ogg` custom.
- **Bundles/Special Orders:** tags garantem compat (Cheese Press, Loom, bundles que filtram `milk_item`/`wool_item`). Decisão explícita: premium **é processável** (vira queijo/pano premium via `FLAVORED_ITEM`? — spec verifica se máquina preserva ID custom; se não, documentar como venda-direta-only e ajustar preço para compensar).

---

## 6. i18n

```json
{
  "shaded-milk.name": "Shaded Milk",
  "shaded-milk.description": "Cool, sweet milk from cows grazed under living canopy.",
  "soft-wool.name": "Soft Wool",
  "soft-wool.description": "Cloud-soft wool from sheep shaded by nitrogen-fixers.",
  "message.shaded.first": "Your animals loved the shade! (Premium produce)",
  "mail.shaded.title": "Shade works! — Elena",
  "mail.shaded.text": "See? Happy animals under living trees make richer milk and softer wool. Keep rotating your pasture.^  -Elena"
}
```

---

## 7. Gates / desbloqueio / balanceamento

| Gate | Proposta |
|---|---|
| Desbloqueio | Automático com ICLF adulta (árvore estágio Tree num raio do pasto) — sem gate de Year além do da árvore. Carta tutorial no 1º premium |
| Gatilho "sob sombra" | Animal outdoor + árvore `LivingRoots.*` estágio ≥ Sapling num raio 6 (constante compartilhada `IclfAuraRadius`) + felicidade ≥ 150/1000 (spec calibra) |
| Probabilidade | Escalonada por felicidade (determinística por seed diária — testável); 100% só com felicidade max — evita RNG frustrante |
| Preço | Milk 300g / Wool 750g iniciais; < artesanal processado; `AvailableStockLimit` n/a (não vendido em loja) |
| Multiplayer | Produção por animal (host-side `DayUpdate`); ouro por jogador que coleta; `Context.IsMainPlayer` guards |
| Categorias | Replicar categorias vanilla exatas (leite/lã) — categoria errada quebra Perfection/achievements e bundles |
| Overflow/logs | Clamp preços; `checked` global; logs Trace só tipo+HResult |

---

## 8. Constantes novas

```csharp
public const string ShadedMilkId = "LivingRoots.ShadedMilk";
public const string QualifiedShadedMilkId = "(O)LivingRoots.ShadedMilk";
public const string SoftWoolId = "LivingRoots.SoftWool";
public const string QualifiedSoftWoolId = "(O)LivingRoots.SoftWool";
public const int ShadedMilkPrice = 300;
public const int SoftWoolPrice = 750;
public const int ShadeMinFriendship = 150;
public const int ShadeCheckRadius = 6; // = IclfAuraRadius
```

## 9. Riscos e teste

- **Riscos:** categoria errada quebra Perfection; produto processado (queijo de Shaded Milk?) pode gerar item vanilla e apagar o premium — verificar `FLAVORED_ITEM`/`Preserve` pipeline na spec; EAC tentador (cortar).
- **Teste:** unit (probabilidade por felicidade/seed; `IsShaded` por distância/estágio/indoor); integração (registro Objects + tags aceitas por Cheese Press/Loom; mail do 1º premium); manual (pastar sob copa 3 dias, comparar produção dentro/fora da sombra).

## Fontes

- Internas: `ROADMAP.md:59`, Epic 14 US-14-03, agrupado §3.
- Externas: `Modding:Objects` (Name/DisplayName/Type/Category/Price/Texture/SpriteIndex/Edibility/Buffs/ContextTags); `Modding:Animal_data` (`Data/FarmAnimals`: ProduceItemIds/DeluxeProduce/MinimumFriendship/DaysToProduce/HarvestTool); `Modding:Buffs` (se consumível); `Modding:Maps` (sem mapa novo — posição via terrainFeatures).
