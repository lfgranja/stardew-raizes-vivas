# Briefing — Organic Waste Validator + `RequiredTags` (filtro de input do Composting Bin)

**Data:** 2026-10-05 · **Feature:** `specs/002-soil-decay-compost/` (FR-008, Organic Waste) · **Tipo:** regra de domínio C# + espelho declarativo em `Data/Machines`
**Status:** pesquisa completa (repo + wiki revisada) · **Ação:** manter paridade entre `OrganicWasteValidator.cs` e os 6 triggers `RequiredTags`; travar com teste T-NEW
**Fontes primárias:** [Modding:Items](https://stardewvalleywiki.com/Modding:Items) (rev. 2026-08-04, tabela Categories), [Modding:Context tags](https://stardewvalleywiki.com/Modding:Context_tags) (rev. 2026-06-13), [Modding:Machines](https://stardewvalleywiki.com/Modding:Machines) (rev. 2026-02-27, §Item processing rules), [Modding:Objects](https://stardewvalleywiki.com/Modding:Objects) (rev. 2026-08-08, §Context tags & exclusions), [Modding:Common data field types](https://stardewvalleywiki.com/Modding:Common_data_field_types) (rev. 2026-01-03, §Custom fields / §Context tag / §Item ID).
**Fontes internas:** `LivingRoots/Domain/Services/OrganicWasteValidator.cs:1-54`, `LivingRoots/Domain/Interfaces/IOrganicWasteValidator.cs:1-16`, `LivingRoots/ModEntry.cs:55,58,114-177`, `LivingRoots/Constants.cs:63-74`, `LivingRoots/Services/CompostingBinService.cs:37-73`, `LivingRoots/Services/CompostApplicationService.cs:45-54`, `LivingRoots/Controllers/ModController.cs:862-883`, `LivingRoots.Tests/OrganicWasteValidatorTests.cs:1-124`, `LivingRoots.Tests/Fixtures/{ItemFactory.cs,GameStateFixture.cs}`, specs/002 `spec.md:93,104,140,161` + `research.md:149-179` (§8) + `data-model.md:118-129` + `contracts/machine-interaction.md:16-27` + `plan.md:77,92`, specs/004 `briefings/data-entries-machines-recipes.md` (§4, §6.2, §8 T-NEW), specs/003 `briefings/compost-item.md` (§2.3 anti-loop), specs/004 `briefings/composting-bin-sheet.md` (§FR-003).

---

## 1. O que é este asset e onde vive

O "organic waste validator" é **uma política de aceitação com duas encarnações que devem concordar item a item**:

| Encarnação | Arquivo | Papel |
|---|---|---|
| **Regra imperativa (C#)** | `LivingRoots/Domain/Services/OrganicWasteValidator.cs` (impl) + `LivingRoots/Domain/Interfaces/IOrganicWasteValidator.cs` (contrato `bool IsValidOrganicWaste(Item)`) | Portão usado por `CompostingBinService.AddWaste` (`CompostingBinService.cs:58` — `if (!validator) return;`, silencioso, sem som). Única autoridade quando o service roda por fora da máquina nativa. |
| **Regra declarativa (data)** | `Data/Machines`, chave `(BC)LivingRoots.CompostingBin`, 1 `OutputRule` com **6 triggers `ItemPlacedInMachine`**, cada um com `RequiredTags` (dono do desenho: briefing 004 `data-entries-machines-recipes.md` §6.2) | Portão usado pelo **jogo nativo** (`Object.PlaceInMachine` / `MachineDataUtility`) quando o jogador faz drop-in, e pelo **Automate** (que lê `Data/Machines` nativamente — spec 002 clarificação Automate, `spec.md:96`). |

Divergência entre as duas = bug de UX em uma das duas direções: máquina aceita e service rejeita (item some sem efeito), ou máquina rejeita o que o service aceitaria (falso negativo visível). O briefing 004 já prescreve o teste de paridade **T-NEW `MachineTagsAgreeWithValidator`** (`data-entries-machines-recipes.md:327`) — este briefing é o dono da **semântica** que esse teste trava.

Fiação (composition root — `ModEntry.cs:55,58`):

```csharp
var organicWasteValidator = new OrganicWasteValidator(this.Monitor);
var compostingBinService = new CompostingBinService(modDataService, saveIdProvider, organicWasteValidator, ...);
```

`CompostingBinService` recebe o validador por injeção de construtor; os demais testes de `CompostingBinService*` usam `Mock<IOrganicWasteValidator>` (5 arquivos sob `LivingRoots.Tests/`), de modo que a política real só é exercitada em `OrganicWasteValidatorTests`.

Entidade de dados (spec 002 `data-model.md:118-129`, `OrganicWasteDefinition` — configuração estática única, sem persistência):

| Campo | Valor travado | Dono |
|---|---|---|
| `ValidCategories` | `[-74, -75, -79, -80, -81]` | spec 002 FR-008 |
| `InclusionTag` | `compostable_item` | research 002 §8 |
| `ExclusionTag` | `not_compostable` | spec 002 FR-008 ("MUST be rejected even if they match a category") |

---

## 2. A regra, exatamente como implementada

`OrganicWasteValidator.cs:18-53`, ordem de avaliação **intencional** (exclusão → inclusão → categoria):

```csharp
public bool IsValidOrganicWaste(Item item)
{
    if (item == null) return false;                                        // (0) null → false
    if (item.HasContextTag("not_compostable")) return false;              // (1) exclusão vence tudo
    if (item.HasContextTag("compostable_item")) return true;              // (2) inclusão custom vence categoria
    return item.Category switch                                          // (3) fallback vanilla
    {
        -74 => true,  // Seeds
        -75 => true,  // Vegetables
        -79 => true,  // Fruits
        -80 => true,  // Flowers
        -81 => true,  // Forage/Greens
        _ => false
    };
}
```

Cada chamada gera um `IMonitor.Log(..., LogLevel.Trace)` (spec 002 FR-022, clarificação `spec.md:83` — só operações que mudam estado, sem spam). Três mensagens distintas identificam qual ramo decidiu: `(not_compostable tag)`, `(compostable_item tag)`, `(category N) accepted/rejected`.

### 2.1 Tabela de decisão (ordens de precedência)

| # | Condição | Resultado | Exemplo |
|---|---|---|---|
| 0 | `item == null` | `false` | chamada defensiva de `AddWaste` com slot vazio |
| 1 | tem `not_compostable` | **`false`, mesmo com categoria válida ou `compostable_item`** | Ancient Fruit marcada, item de quest, produto valioso de mod |
| 2 | tem `compostable_item` (e não tem `not_compostable`) | **`true`, independente da categoria** | tábua de mod (`-12` stone-like) que o autor quer compostável; artisan good com tag explícita (comportamento correto — tag do autor vence, cf. 004 §6.2) |
| 3a | categoria ∈ {-74,-75,-79,-80,-81} | `true` | Parsnip (-75), Blueberry (-79), Fairy Rose (-80), Wild Horseradish (-81), Wild Seeds (-74) |
| 3b | qualquer outra categoria | `false` | Wood (-14), Stone (-12), artisan goods (-26), cooking (-7), fish (-4), gem (-2), junk (-20) |

### 2.2 O que a spec diz em cada revisão (arqueologia curta, para não reabrir)

- `spec.md:104` (sessão 2026-09-06, **superseded**): só Seeds + Vegetables não-artisan. Ampliada depois.
- `spec.md:93` (sessão 2026-09-09, **canônica**): híbrido categorias + `compostable_item`, com `not_compostable` override, mais nota de "Cooking Waste" futuro (byproduct de cozinhar — **não implementar agora**).
- `spec.md:140` (FR-008) + `spec.md:161` (Key Entities/Organic Waste): texto normativo atual; `contracts/machine-interaction.md:16-27` repete a tabela; `research.md:151-176` (§8) dá o pseudocódigo que o `.cs` segue literalmente; `data-model.md:122-128` fixa os três campos.
- specs/004 herdou o conjunto como FR-003 ("referenciar, nunca recopiar listas" — `composting-bin-sheet.md:71`) e o traduziu para tags em `data-entries-machines-recipes.md` §6.2.

---

## 3. Fundação externa (wiki) — categorias, context tags, `RequiredTags`, `CustomFields`, `ItemRegistry`

### 3.1 `Modding:Items` §Categories — as 5 categorias aceitas e o que cada uma significa

Fonte: tabela oficial (wiki Items §Categories). Colunas relevantes: `valor → constante C# → context tag automática → tradução → propriedades`.

| Categoria | Constante (`StardewValley.Object.*`) | Context tag automática | Tradução | Propriedades que importam aqui |
|---|---|---|---|---|
| **-74** | `SeedsCategory` | `category_seeds` | Seed | `Is Placeable, is always passable` |
| **-75** | `VegetableCategory` | `category_vegetable` (**singular** — armadilha de digitação) | Vegetable | Afetado por Tiller; usável em slingshot |
| **-79** | `FruitsCategory` | `category_fruits` | Fruit | Afetado por Tiller (se não foraged); slingshot |
| **-80** | `flowersCategory` (minúsculo no fonte) | `category_flowers` | Flower | Afetado por Tiller |
| **-81** | `GreensCategory` | `category_greens` | Forage | — |

Notas de fidelidade:

1. **Singular vs. plural é prescrição da wiki, não intuição.** `category_vegetable` (singular) vs. `category_fruits/flowers/greens/seeds` (plural). O briefing 004 §12.4 lista isso como armadilha de revisão — copiar da tabela oficial.
2. **Categoria é `item.Category` (int negativo); `0` é "blank".** O validador lê o int; a máquina nativa lê a **tag** (`category_*`). As duas leituras são geradas do mesmo `Category` do `ObjectData`, mas por caminhos diferentes (§3.2 vs. §3.3) — é por isso que a paridade precisa de teste.
3. **Exemplos vanilla por categoria** (para fixtures do T-NEW; confirmar IDs no jogo, não decorar): -74 = Wild Seeds / Tree Seeds; -75 = Parsnip (24), Potato; -79 = Ancient Fruit, Blueberry, Apple; -80 = Fairy Rose, Tulip; -81 = Wild Horseradish, Leek, Dandelion (forage). Ancient Fruit (-79) é o caso canônico para demonstrar o override `not_compostable`.

### 3.2 `Modding:Context tags` — o mecanismo das duas tags custom

- **Formato:** alfanumérico + underscore (+ parênteses em tags geradas como `id_(o)128`); **case-insensitive na maioria dos caminhos, mas custom tags devem ser minúsculas por segurança** (wiki §Format). `compostable_item` / `not_compostable` já seguem a convenção nos dois lados (C# e data) — manter.
- **Tags automáticas relevantes:** `category_<categoria>` (para todos os itens, derivada do `Category`), `item_<nome>`, `id_<tipo>_<id>`, `quality_*`, `item_type_<Type>`, `preserve_*`/`jelly_item`/`wine_item` (flavored), `id_(o)<id>` (qualified-ID sanitizada — útil para excluir **um item específico** por tag em vez de categoria).
- **Como um mod autor declara as tags:** campo `ContextTags: string[]` em `Data/Objects` (wiki Objects §Context tags & exclusions: *"custom context tags to add … in addition to the tags added automatically"*). Exemplo para um item de mod compostável:
  ```json
  "MyMod.RottenPumpkin": { "Category": -75, "ContextTags": [ "compostable_item" ], ... }
  ```
  e para blindar um item valioso:
  ```json
  "MyMod.GoldenAncientFruit": { "ContextTags": [ "not_compostable" ], ... }
  ```
  Nenhum código do LivingRoots precisa mudar para aceitar esses itens — esse é o ponto do desenho híbrido (research 002 §8 "mod extensibility").
- **`Item.HasContextTag(tag)` (C#):** resolve via `ItemContextTagManager` + `Game1.content` + `ItemRegistry`. Implicações práticas:
  - Precisa de estado global de jogo; nos testes isso é suprido por `GameStateFixture.Install()` (`MinimalContentManager` vazio + `Game1.objectData` vazio + `ObjectDataDefinition` registrada), o que significa que **tags custom nunca existem no host de teste atual** — todo item de teste responde "sem tags" (§6.1).
  - `ItemContextTagManager.DoAllTagsMatch / DoAnyTagsMatch / DoesTagMatch` (wiki §ItemContextTagManager class) implementam a semântica de negação `!tag` usada nos triggers. O T-NEW deve reutilizar esses helpers (ou reimplementar o OR-dos-6 com string match) em vez de inventar um terceiro avaliador.

### 3.3 `Modding:Machines` §Item processing rules — os 6 triggers `RequiredTags`

Semântica wiki, literal:

- **AND dentro do trigger, OR entre triggers:** *"An item must have all of the listed tags to select this rule"*; *"When to apply … any number of triggers; the output will apply if any of them match."*
- **Negação com `!`:** `"RequiredTags": [ "bone_item", "!fossil_item" ]`.
- **`Trigger` default `ItemPlacedInMachine`** (keg/jar pattern — nosso caso); `RequiredCount` default 1 (= exatamente 1 item por interação, spec 002 FR-008); sem `MinutesUntilReady`/`DaysUntilReady` o output é **instantâneo** (gap G3 do briefing 004 — hoje o `ModEntry.cs:151-175` está exatamente nesse estado quebrado: 1 regra, sem triggers, sem tempo).
- **Tradução prescrita** (briefing 004 §6.2, dono do C# — aqui só a correspondência):

  | # | `RequiredTags` | Espelha (validador) |
  |---|---|---|
  | 1 | `["category_seeds", "!not_compostable"]` | `-74` |
  | 2 | `["category_vegetable", "!not_compostable"]` | `-75` |
  | 3 | `["category_fruits", "!not_compostable"]` | `-79` |
  | 4 | `["category_flowers", "!not_compostable"]` | `-80` |
  | 5 | `["category_greens", "!not_compostable"]` | `-81` |
  | 6 | `["compostable_item", "!not_compostable"]` | tag custom |

  `!not_compostable` nos **6** triggers (FR-008 "MUST be rejected even if they match a category"). `UseFirstValidOutput`, `Condition` (GSQ), `RequiredItemId` — não usados (YAGNI).

### 3.4 `CustomFields` vs `ContextTags` — não confundir (lição explícita do prompt)

- **`CustomFields`** (wiki Common data field types §Custom fields; presente em `ObjectData`, `MachineData`, buffs, etc.): **ignorado pelo jogo**, lido só por frameworks/mods via `data.CustomFields.TryGetValue(...)`. **Não serve como filtro de input de máquina** — `RequiredTags` nunca lê `CustomFields`. Se um autor de mod marcar compostabilidade num custom field (`"MyMod/Compostable": "true"`), o LivingRoots **não verá** — o contrato público é a context tag.
- **`ContextTags`** (`ObjectData.ContextTags: List<string>`): é o canal que o jogo indexa, auto-estende (`category_*`) e expõe em `RequiredTags`, `ITEM_CONTEXT_TAG` (GSQ), `Data/FishPondData`, `Data/SpecialOrders` (`AcceptedContextTags`), tailoring e gift tastes (wiki Context tags §Usage in data). É onde `compostable_item` / `not_compostable` vivem.
- Regra de bolso: *filtro e matchmaking → `ContextTags`; metadado opaco para código próprio → `CustomFields`.*

### 3.5 `ItemRegistry` + `QualifiedItemId` — identificação sem ambiguidade

- **Duas formas de ID** (wiki Items §Item IDs / §Identify items): `ItemId` (não-qualificado, `"LivingRoots.Compost"`, pode colidir entre tipos em vanilla legada) vs. `QualifiedItemId` (globalmente único, `"(O)LivingRoots.Compost"`, prefixo = `TypeDefinitionId`, cf. tabela `(O)/(BC)/(B)/…`).
- **Criação:** `ItemRegistry.Create("(O)128")` (forma preferida); `new Object("128", 1)` exige o ID **não-qualificado** no construtor. ID inexistente → **Error Item** (a menos de `allowNull: true`).
- **Uso no repo:** o compost é `CompostItemId = "LivingRoots.Compost"` + `QualifiedCompostItemId = "(O)LivingRoots.Compost"` (`Constants.cs:63-64`). `CompostApplicationService.cs:47-49` e `ModController.cs:862-864` fazem **comparação tripla** (`Qualified == CompostItemId || Qualified == QualifiedCompostItemId || ItemId == CompostItemId`) — redundância defensiva exigida pelo `AGENTS.md` raiz (as duas formas devem ser comparadas). `CompostingBinService.cs:61` persiste `item.QualifiedItemId` em `InputItemId` (para drop-on-break); `CompostApplicationService` compara `heldItem.QualifiedItemId` (não `ItemRegistry.HasItemId` — equivalência intencionalmente estrita).
- **`ItemFactory` nos testes** (`ItemFactory.cs:22-29`): construtor sem parâmetros + `ItemId` atribuído direto, de modo que `QualifiedItemId` reporte o ID pedido **sem precisar de `ItemRegistry` populado** — é assim que os testes de categoria funcionam sem bootar o jogo.

---

## 4. Artisan exclusion gap (análise fechada)

A spec diz duas coisas que parecem redundantes — não são:

1. FR-008/Key Entities: **"Artisan goods are excluded."**
2. FR-008: **itens com `not_compostable` são rejeitados mesmo com categoria.**

O mecanismo é diferente nos dois casos:

| Caso | Mecanismo | Onde |
|---|---|---|
| Artisan goods vanilla (categoria **-26**, `category_artisan_goods`) | **Exclusão por ausência** — nenhum ramo positivo casa: nem o `switch` do C# (só -74/-75/-79/-80/-81 → `_ => false`), nem nenhum dos 6 triggers (`category_artisan_goods` não aparece em nenhum). **Nenhum trigger negativo extra necessário (YAGNI)** — conclusão já registrada no briefing 004 §6.2. | `OrganicWasteValidator.cs:41-49` + triggers 1–6 |
| Item valioso/frágil de categoria válida (ex.: Ancient Fruit -79, quest item, produto de mod -75 que não deve ser destruído) | **Exclusão por veto** — `not_compostable` (ramo 1 do C#, `!not_compostable` nos 6 triggers). Sem o veto, a categoria aceitaria. | `OrganicWasteValidator.cs:27-31` + `!not_compostable` em cada trigger |
| Artisan good com `compostable_item` explícita | **Aceito (correto)** — tag do autor vence a ausência. Exceção conhecida e documentada (004 §6.2). | ramo 2 do C# + trigger 6 |

Categorias vizinhas que **devem continuar rejeitadas** (não adicionar sem emenda à spec 002):

| Categoria | Tag | Por que fora |
|---|---|---|
| -26 artisan goods | `category_artisan_goods` | regra explícita; inclui Wine/Juice/Jelly/Pickle (**flavored items** `(O)348` etc. com `preserve_*` + `*_item` tags) — compostar vinho seria destruição de valor e contraria "Artisan goods are excluded" |
| -27 syrup | `category_syrup` (Tapper) | profissão Tapper; seiva/xarope não são resto de colheita |
| -7 cooking | `category_cooking` | comida pronta; o futuro "Cooking Waste" será **um byproduct novo**, não a comida em si (spec 002 clarificação) |
| -25 ingredients | `category_ingredients` | insumo de cozinha, não sobra |
| -8 crafting / -14 meat / -20 junk / -4 fish / -2 gem … | — | nunca foram vegetais; junk (-20) é tentador ("lixo compostável?") mas fora do conjunto travado |
| **-19 fertilizer** | `category_fertilizer` (`Is Placeable, always passable`) | fertilizante vanilla não é sobra; e há risco de interação com clique-no-solo (§5.3) |
| Compost do próprio mod (-26) | `compost_item` (output, briefing 004 §3) | **anti-loop**: compost **não** carrega `compostable_item` (só `compost_item`, de propósito distinto), e -26 não é categoria aceita — compostar compost é impossível pelos dois portões. O briefing 003 (`compost-item.md` §2.3) travou a mesma invariante sob a hipótese antiga de categoria -2; a conclusão sobrevive à mudança para -26: qualquer categoria fora do conjunto de 5 + ausência da tag = rejeitado. |

> Nota histórica: `research.md:111-117` e `plan.md:117` chamam -26 de "fertilizer-like" — **incorreto** contra a tabela oficial atual (-26 = artisan goods, profissão Artisan; fertilizer é -19). O briefing 004 §7 já corrige e recomenda **manter -26** (spec-locked; trocar é emenda à spec 002, e -19 traria risco de plantio vanilla competindo com `OnButtonPressed`). Não reabrir aqui.

---

## 5. Interações com o resto do mod (contratos que tocam este asset)

### 5.1 `CompostingBinService.AddWaste` — o portão imperativo

`CompostingBinService.cs:37-73`: lock atômico do host → garante `State == Empty` → **`IsValidOrganicWaste(item)` → `return` silencioso se inválido** → `Processing` + `InputItemId = item.QualifiedItemId` + `InputTimestamp` + log Trace. Rejeição do service é **silenciosa por desenho** (o feedback sonoro/visual de drop-in inválido — shake + `cancel`, sem texto, FR-008a — é responsabilidade da **máquina nativa**, não do service; ver briefing 004 §5 e `composting-bin-*`).

### 5.2 `ModController.OnButtonPressed` — roteamento equip-to-add / empty-collect

`ModController.cs:862-883` (pattern furnace/keg, `spec.md:126`): com compost na mão → `TryApplyCompost`; senão, lê `GetBinState` e despacha `AddWaste(location, tile, heldItem)` (bin vazio + waste válido) ou `CollectCompost` (bin ready). O validador **não** é chamado no controller — só dentro de `AddWaste`. Mockar `IOrganicWasteValidator` nos testes de controller/service é o padrão (5 arquivos); a política real só vive em `OrganicWasteValidatorTests`.

### 5.3 Compost output nunca é input (anti-loop, as duas direções)

- Direção C#: compost é `Category = CompostCategoryId (-26)` (`Constants.cs:73`, `ModEntry.cs:136,146`) → cai no `_ => false` (§4). Nenhum `ContextTags` com `compostable_item` é registrado no compost (só o aditivo `compost_item` proposto no briefing 004 §3, que **não** é lido por nenhum trigger).
- Direção data: trigger 6 exige `compostable_item`; compost não tem → sem match.
- Consequência para futuros itens do mod (lição do roadmap `mid-game-year2-3.md:348,393`): **toda erva/produto novo precisa de decisão explícita de categoria** — matéria-prima compostável (`-81`) vs. produto processado (`-7` cooking ou `-26`, nunca compostável). Travar com teste por item (mesmo padrão do anti-loop do compost).

### 5.4 Localização do compost e `QualifiedItemId` triplo

`CompostApplicationService.cs:45-54` + `ModController.cs:862-864`: aceita o held item se **qualquer** das três comparações bater (`Qualified == "LivingRoots.Compost"` — forma legada sem prefixo — OU `Qualified == "(O)LivingRoots.Compost"` OU `ItemId == "LivingRoots.Compost"`). Exigência do `AGENTS.md` raiz (Stardew 1.6 qualifica como `(O)LivingRoots.Compost`; ambas as constantes existem e ambas devem ser comparadas). Não simplificar para uma forma só sem atualizar os dois call sites + testes.

---

## 6. Testes — estado, gaps e o que travar

### 6.1 O que existe (`OrganicWasteValidatorTests.cs`)

8 testes, todos **só-categoria** via `ItemFactory.CreateItem(id, category)`: 5 aceites (-74/-75/-79/-80/-81), stone (-12) e wood (-14) rejeitados, null rejeitado. `GameStateFixture.Install()` no construtor fornece o `Game1.content` mínimo para `HasContextTag` responder sem bootar o jogo.

**Gap explícito e assumido** (`OrganicWasteValidatorTests.cs:118-123`, NOTE): testes de context tag (FR-4.1a/4.2a) **ausentes** — `Item.ContextTags` é interno, sem setter público nessa versão da API; cobrir exigiria reflection (vetado por NFR-4). O host atual ainda responde "sem tags" para tudo (`MinimalContentManager` vazio + `objectData` vazio — `GameStateFixture.cs:24-28` documenta que isso é "a resposta verdadeira para um item sem tags", não um mock mentiroso).

### 6.2 O que falta (prescrição — implementar junto dos 6 triggers)

| # | Teste | Arquivo | Asserts |
|---|---|---|---|
| T-NEW | `MachineTagsAgreeWithValidator` (paridade C# ↔ data) | `AssetRegistrationTests.cs` ou `OrganicWasteValidatorTests` | Para amostra de IDs vanilla por categoria (fixtures reais do repo) + item `not_compostable` + item `compostable_item`: `validator.IsValidOrganicWaste(item) == tagsCasam(item)` (OR dos 6 triggers via `ItemContextTagManager.DoAllTagsMatch`-like). Dono formal: briefing 004 §10; semântica: este briefing §3.3. |
| T-VAL-EXCL | exclusão vence inclusão | `OrganicWasteValidatorTests` | item com categoria válida **e** `not_compostable` → `false` (requer seam de tags: `ObjectData.ContextTags` populado no `GameStateFixture.objectData` + `ItemFactory` criando por ID registrado, em vez de reflection — caminho limpo quando o fixture evoluir). |
| T-VAL-INCL | inclusão custom | idem | item categoria inválida (`-12`) **com** `compostable_item` → `true` (mesmo seam). |
| T-ART | artisan gap | idem + T022a | `-26` sem tag → `false` nos dois portões; `-26` **com** `compostable_item` → `true` (exceção documentada §4). |
| Existentes | stone/wood/null + 5 categorias | — | manter; são o contrato mínimo se todo o resto for adiado. |

### 6.3 Armadilhas de teste (não reintroduzir)

1. `GameStateFixture.Install()` é **obrigatório** no construtor de qualquer teste que toque `HasContextTag` — sem ele, `Game1.content` nulo explode antes do validador.
2. `ItemFactory.CreateItem` com categoria direta **não** exercita tags — um teste "de tag" que só muda a categoria prova zero sobre tags.
3. `xUnit` roda classes em paralelo — `GameStateFixture` já é idempotente sob lock; não duplicar a instalação com escrita concorrente em `objectData`.

---

## 7. Exceção arquitetural conhecida (não repetir)

`Domain/` é puro por regra (`LivingRoots/Domain/AGENTS.md`: **nenhum `using StardewModdingAPI`** — só `StardewValley.*` / XNA). `OrganicWasteValidator.cs:2` faz `using StardewModdingAPI` para `IMonitor` — **exceção legada explícita**, registrada no `AGENTS.md` raiz ("Treat it as legacy, not precedent") e no roadmap (`mid-game-year2-3.md:28`). Consequência: todo teste do validador precisa de `Mock<IMonitor>`; futuros domain services devem receber log via abstração ou não logar, nunca importar SMAPI de novo.

---

## 8. Contrato para autores de mods (documentação pública mínima)

Para tornar um item compostável, o autor declara no seu `Data/Objects` (ou equivalente):

```json
"ContextTags": [ "compostable_item" ]
```

Para blindar um item (valioso, quest, frágil):

```json
"ContextTags": [ "not_compostable" ]
```

Garantias do LivingRoots: matching é **OR** (categoria vanilla OU tag), veto é **absoluto** (vale contra categoria e contra a outra tag), `CustomFields` são ignorados para este fim (§3.4), e a máquina nativa + Automate enxergam a mesma política sem código extra (via `Data/Machines`).

---

## 9. Riscos e não-escopo

1. **Trigger sem `!not_compostable`** = Ancient Fruit compostável pela máquina (G-divergência §6.2 do briefing 004). Revisores: todo trigger positivo novo nasce com a negação junto.
2. **`category_vegetable` singular** — grep de revisão deve procurar `category_vegetables` (plural errado) como bug certo.
3. **Flavored items** (`wine_item`, `preserve_*`, `DROP_IN`): são -26 por categoria → rejeitados por ausência; não criar regra especial para aceitá-los sem emenda à spec (destruição de valor).
4. **BigCraftables / Tools / Weapons como input:** `item.Category` fora de `Object` não segue a mesma tabela; o validador lê `Category` de `Item` base — comportamento para não-Objects é "rejeita salvo tag", o que é o default seguro. Não estender sem spec.
5. **"Cooking Waste" byproduct** (spec 002 clarificação `spec.md:93`, último período): **fora de escopo** — quando existir, será um item novo com categoria/tag própria, não uma mudança nesta regra.
6. **Maturação 1–5x** (`data-model.md:59-77`, spec 002 FR-013): aposentada pela spec 004 (FR-005a one-in-one-out, leitura tolerante de saves legados — `composting-bin-sheet.md:71`). Nada neste briefing depende do multiplicador.

---

## 10. Checklist de implementação (quando tocar neste asset)

- [ ] Qualquer mudança na lista `[-74,-75,-79,-80,-81]` ou nas tags toca **os dois portões juntos** (`OrganicWasteValidator.cs` + 6 triggers em `OnAssetRequested`) + T-NEW verde.
- [ ] Literais `"compostable_item"` / `"not_compostable"` vivem hoje em `OrganicWasteValidator.cs:27,34` + triggers — se centralizar em constantes, centralizar nos dois lugares na mesma mudança (briefing 004 §9: `List<string>` mutável não cabe em `const`; decisão fora do escopo mínimo).
- [ ] `dotnet format` → build com `GamePath` → `dotnet test` (gate `format → build → test`; `GamePath` obrigatório — ver `AGENTS.md` raiz).
- [ ] Verificação in-game (espelha briefing 004 §11): drop-in de item de cada categoria entra e inicia; wood/stone e artisan good sem tag = shake + `cancel` sem texto; item com `not_compostable` rejeitado mesmo sendo -79; `debug listtags` mostra as tags esperadas.

---

*Última verificação: `OrganicWasteValidator.cs:1-54`, `IOrganicWasteValidator.cs:1-16`, `ModEntry.cs:55,58,114-177`, `Constants.cs:55-79`, `CompostingBinService.cs:37-73`, `CompostApplicationService.cs:18-54`, `ModController.cs:862-883`, `OrganicWasteValidatorTests.cs:1-124`, `ItemFactory.cs:1-48`, `GameStateFixture.cs:1-154`; specs/002 (`spec.md` FR-008/FR-018/FR-019 + clarificações 2026-09-06/09, `research.md` §6–§8, `data-model.md` §§4–6, `contracts/machine-interaction.md` §§Input/Triggers, `plan.md:77,92`, `tasks.md` T010/T011/T018); specs/004 (`data-entries-machines-recipes.md` §§4/6.2/8/10, `composting-bin-sheet.md` §§FR-003/gaps); specs/003 (`compost-item.md` §§2.3/3, `README.md` gate 798/12); futuro (`mid-game-year2-3.md:18,28,348,393`); wiki revs. Items 2026-08-04, Context tags 2026-06-13, Machines 2026-02-27, Objects 2026-08-08, Common data field types 2026-01-03. Verificação em jogo (§10) e IDs vanilla de exemplo pendentes de confirmação via `list_items` — marcados como verificação, não assumidos.*
