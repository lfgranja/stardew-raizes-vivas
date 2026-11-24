# Briefing — "Cooking Waste": o item byproduct e o ciclo que o alimenta a composteira

> **Status:** briefing (não implementado). Adiado explicitamente pela spec 002. Ativa junto com `specs/004-playable-compost-loop` (contrato de `Data/Machines`) e com `pest-system-mvp.md` (via `living-pharmacy-tonics-sprays.md:93`).
> **Citações-âncora da spec 002:** `specs/002-soil-decay-compost/spec.md:93` (canônica) · `specs/002-soil-decay-compost/briefings/organic-waste-validator.md:77,176,264` (as três reanotações).
> **Por que este briefing existe:** é o **único** item do mod que criaria itens a partir de uma ação do jogador em vez de exigir uma máquina. Nenhum outro briefing futuro tem precedente de `Data/` equivalente.
> **Não promover para `specs/` sem `/speckit.specify`.**

---

## 0. Sumário executivo (TL;DR)

Item **Cooking Waste** (`LivingRoots.CookingWaste`): resíduo de cozinha que aterrissa no inventário de quem cozinhou e alimenta a composteira como qualquer resto orgânico. **Hook escolhido:** não existe evento SMAPI de cozimento (verificado no índice completo de eventos — wiki rev 190144 — e no assembly do jogo); observa-se o stat vanilla `ItemsCooked` (`Modding:Stats`, rev 191160) por **delta** em `GameLoop.UpdateTicked` — puro SMAPI, sem Harmony, sem dependência nova. **Aceitação pela composteira:** via tag `compostable_item` (trigger 6 já existente — **nenhum 7º trigger**). **O que trava a implementação:** sprite 16×16 autoral (`Assets/cooking_waste.png`), o serviço de delta com seeding correto no `SaveLoaded` (armadilha de concessão em lote, §9), e a decisão de spec sobre `Type: Basic` vs `Cooking` (§6).

## 1. Gap table

| # | Gap | Evidência | Impacto | Esforço |
|---|-----|-----------|---------|---------|
| G1 | Hook de cozimento inexistente | Índice completo de eventos SMAPI (`Modding:Modder Guide/APIs/Events`, rev 190144): 41 eventos, **nenhum** observa cozimento; scan do assembly `.gamepath/Stardew Valley.dll`: `CookingRecipeMenu`/`CookingMenu` = 0 ocorrências (a UI de cozinha é o `CraftingPage`) | Sem hook, o item nunca nasce — é o gap espinha (§4) | Baixo — stat `ItemsCooked` + `UpdateTicked` por delta |
| G2 | Item por jogador vs por farm | `Modding:Stats`: stats são por jogador e **não** são sincronizados em multiplayer; Principle VI (`constitution.md:32`) exige autoridade única só para estado de **farm** | Byproduct é item **pessoal** (quem cozinhou recebe); vira estado de farm apenas ao entrar na bin (`InputItemId` host-side) | Nenhum — inventário já é por jogador; o jogo sincroniza em co-op |
| G3 | Idempotência — cada ato de cozinhar concede exatamente uma vez | O stat é um contador monótono `uint` ("The number of items created from cooking recipes", `Modding:Stats`); cada cozinha incrementa exatamente 1 | Delta = concessão exata: 2 cozinhas → 2 byproducts (um por refeição), nunca 0 nem 3; re-entrancy/reload/tick storm não duplicam | Baixo — matemática de delta + reseed no load (§9) |
| G4 | Aceitação pela composteira | `OrganicWasteValidator.cs:41-49` rejeita -7 por ausência; triggers 1–6 (`data-entries-machines-recipes.md:196-203`) não cobrem `category_cooking` | Byproduct precisa de portão: tag `compostable_item` (trigger 6) — **não** precisa de 7º trigger nem de `CustomFields` (que nenhum portão lê) | Nenhum — só declarar a tag no item (§6) |
| G5 | O que acontece se plantado/dado/consumido | -7 não tem propriedade `Is Placeable` (só -8/-9/-19/-74 têm — wiki `Modding:Items` categorias); `Edibility -300` = não-comestível (`Modding:Objects`) | Não planta, não come; presente e lixo são ciclo de vida vanilla de item — `CanBeGivenAsGift=false` recomendado, decisão da spec (§6) | Nenhum — comportamento vanilla de item |
| G6 | Save-compat | `ItemsCooked` persiste por jogador no save; o estado do serviço (último valor observado) é efêmero e vive só em memória | Reload não pode re-conceder: reseed do último valor no `SaveLoaded`/primeiro tick, **nunca** seed 0 (§9, risco R2) | Baixo — um reseed + teste |

## 2. Fontes (primárias com data de revisão / internas)

### Primárias (externas)

| Página (stardewvalleywiki.com) | oldid | Última edição | O que fornece |
|---|---|---|---|
| `Modding:Objects` | 193966 | 2026-08-08 | 23 campos do `Data/Objects` (`Name`…`CustomFields`); `Edibility -300` = não-comestível; `Type: Cooking` + `Category: -7` contam para Gourmet Chef/Perfection |
| `Modding:Context tags` | 193554 | 2026-06-13 | `ItemContextTagManager.GetBaseContextTags(id)` cobre **só** `Data/Objects` e `Data/BigCraftables`; `DoAllTagsMatch`/`DoesTagMatch` com negação `!tag` |
| `Modding:Recipe data` | 193377 | 2026-05-20 | Formato de `Data/CookingRecipes` (`"ingredientes/par-unused/yield/unlock/name"`); unlocks `f NPC corações` / `s Skill nível` / `default`; Fried Egg é hard-coded desde o início |
| `Modding:Items` | 193945 | 2026-08-04 | Tabela de categorias: -7 = `Object.CookingCategory` / `category_cooking`; -20 junk; -25 ingredients; -26 artisan; quais têm `Is Placeable` |
| `Modding:Stats` | 191160 | 2026-02-16 | **`ItemsCooked`**: "The number of items created from cooking recipes by the player"; leitura C# `Game1.player.stats.Get(StatKeys.ItemsCooked)`; console `debug getStat ItemsCooked`; query `PLAYER_STAT Current ItemsCooked N`; **stats são por jogador e não sincronizam em multiplayer** |
| `Modding:Modder Guide/APIs/Events` | 190144 | 2026-01-07 | Índice completo dos 41 eventos SMAPI (Content/Display/GameLoop/Input/Multiplayer/Player/World) — **nenhum** de cozimento; handlers são síncronos; `UpdateTicked` exige cache só para ops caras |
| `Modding:Big craftables` | 190701 | 2026-01-12 | `(BC)` type, `Data/BigCraftables`, `TileSheets/Craftables` — contexto da composteira, não do byproduct |
| `Modding:Trigger actions` | 190039 | 2026-01-02 | "C# mods should usually use SMAPI events instead"; nenhum trigger action observa cozimento |
| `Modding:Audio` | 193491 | 2026-06-02 | Cues vanilla: `coin` ("pop… when the player picks up objects"), `crafting` ("hammer clanking… Rock Crab"), `dirtyHit`, `harvest`, `cancel`; `Data/AudioChanges` para cues custom |

> **Correção de premissa:** as páginas `Modding:Cooking recipes` e `Modding:Triggered tiles` **não existem** na wiki (verificado via `action=query` + `list=allpages` no namespace 570, 2026-10-05). O conteúdo de "cooking recipes" vive em `Modding:Recipe data`; o de "triggered tiles" em `Modding:Trigger actions`. Citações aqui usam os nomes reais.

### Internas

| Fonte | O que fornece |
|---|---|
| `specs/002-soil-decay-compost/spec.md:93` | Clarificação canônica: validação híbrida (categorias -74/-75/-79/-80/-81 OU `compostable_item`, veto `not_compostable`) + menção explícita: "Future planned feature (not now): 'Cooking Waste' byproduct item from cooking actions" |
| `specs/002-soil-decay-compost/briefings/organic-waste-validator.md:166-182` | §4: -7 cooking é "comida pronta" e **deve continuar rejeitada**; exceção por tag explícita do autor (linha 168: "tag do autor vence"); `:176` — o futuro byproduct "será um byproduct novo, não a comida em si"; `:264` — fora de escopo até existir como item com categoria/tag própria |
| `LivingRoots/Domain/Services/OrganicWasteValidator.cs:27-49` | Os três ramos reais: `not_compostable` (veto) → `compostable_item` (opt-in) → switch de categorias (`_ => false` para -7) |
| `LivingRoots/Controllers/ModController.cs:807-811` | `OnUpdateTicked` — assinatura existente que carregará o polling do stat |
| `LivingRoots/Controllers/ModController.cs:829-883` | `OnDayStarted` / `OnButtonPressed` (superfície de interação bin + compostagem) |
| `LivingRoots/Controllers/ModController.cs:891-940` | `ExecuteWithConcurrencyGuard` — padrão de guard re-entrante (§16.6 justa por que não se aplica aqui) |
| `LivingRoots/ModEntry.cs:120-182` | `OnAssetRequested` — os ramos `Data/CraftingRecipes`, `Data/Objects`, `Data/Machines` que crescerão |
| `LivingRoots/Constants.cs:55-79` | Bloco "Decay & Compost Constants" — onde as novas constantes entram (`:63-64` = par de IDs qualificados do compost, padrão a seguir) |
| `LivingRoots/i18n/default.json:1-13` | Chaves existentes (`compost.name`, `message.*`) — padrão para as novas (§12) |
| `specs/004-playable-compost-loop/briefings/data-entries-machines-recipes.md:192-253` | §6.2 — os 6 triggers `RequiredTags` (trigger 6 = `["compostable_item", "!not_compostable"]`); §6.3 — C# real da máquina |
| `specs/004-playable-compost-loop/briefings/data-entries-machines-recipes.md:327` | T-NEW `MachineTagsAgreeWithValidator` — o teste de paridade prescrito (§14) |
| `specs/004-playable-compost-loop/briefings/compost-icon.png.md:193-197` | Restrição dura de licensing: proibido vendorizar pixels vanilla |
| `.specify/memory/constitution.md:28-37` | Principle VI — "Exactly one authority owns the truth"; farm state host-authoritative; divergência explícita |
| `LivingRoots/Services/PlayerProvider.cs:10-17` | Seam `IPlayerProvider.CurrentPlayer` — como o serviço lê `Game1.player` de forma testável |

## 3. Conceito de jogo e fronteiras

**Cooking Waste** é o resíduo da cozinha — raspas, cascas, aparas — em agroecologia: matéria orgânica que a cozinha **devolve** ao ciclo em vez de descartar. No mod, é o vértice que faltava no loop:

```
cultivo → colheita → cozinha → [Cooking Waste] → Composting Bin (2 dias) → Compost → solo (＋15 saúde) → cultivo
```

A spec 002 fechou o loop solo→compost→solo e a 004 o tornou jogável (máquina `Data/Machines`); a cozinha era a única fonte orgânica que **não** alimentava a composteira — comida cozida (-7) é deliberadamente rejeitada (`organic-waste-validator.md:176`), porque compostar um prato pronto seria destruição de valor. O byproduct resolve a assimetria sem abrir essa porta: o que nasce da cozinha como **resto** (não como prato) volta ao solo.

**Fronteiras com o que já existe:**

- **Não é comida cozida.** Salada, Pizza etc. (categoria -7, sem tag) continuam rejeitados nos dois portões — o byproduct é aceito **apenas** pela sua tag explícita `compostable_item` (§6).
- **Não é o compost.** O compost (`LivingRoots.Compost`, -26, tag `compost_item`) é o *output* da bin; o byproduct é um *input*. Anti-loop preservado: compost nunca é compostável (`organic-waste-validator.md:180`).
- **Não é artisan good.** -26/-27 continuam fora por ausência (`organic-waste-validator.md:174-175`); vinho e seiva não viram waste.
- **Não é ingrediente de cozinha.** -25 (`category_ingredients`) é insumo, não sobra — e o byproduct não é ingrediente de receita nenhuma (§8, §17).
- **É o único item do mod que nasce de uma ação do jogador sem máquina** (cabeçalho deste briefing) — daí a exigência de um ponto de captura próprio (§4) e de propriedade por jogador (§9), coisas que nenhum outro briefing futuro precisou resolver.

## 4. Ponto de captura — qual evento observa a refeição

**Resposta direta: não existe.** Nem evento SMAPI, nem classe `CookingRecipeMenu` no jogo. O que existe de fato, verificado:

1. **Índice completo de eventos SMAPI** (`Modding:Modder Guide/APIs/Events`, rev 190144): Content (4) + Display (11) + GameLoop (10) + Input (4) + Multiplayer (4) + Player (3) + World (10) + 2 avançados = **41 eventos, nenhum observa cozimento**. Não há evento de cozinha em `StardewModdingAPI.Events`, nem `DayUpdate` de objeto exposto — `Object.dayUpdate` é **método do jogo**, não evento SMAPI, e dispara no rollover do dia para objetos colocados: granularidade errada para uma ação intra-dia.
2. **Assembly do jogo** (`.gamepath/Stardew Valley.dll`, scan de strings, 2026-10-05): `CookingRecipeMenu` = **0 ocorrências**; `CookingMenu` = 0. A UI de cozimento é o **`CraftingPage`** (presente; a cozinha é uma aba do menu de crafting — campo `cookingMenu`, *verificar no build com `GamePath`*), e o discriminador de receita é `CraftingRecipe.isCookingRecipe` (presente).
3. **O stat que observa cozimento — e é sanctioned:** `Modding:Stats` (rev 191160) documenta **`ItemsCooked`**: *"The number of items created from cooking recipes by the player."* Caminho C# documentado: `Game1.player.stats.Get(StatKeys.ItemsCooked)` (`StatKeys` "has constants for all of the base game's stats" — `StatKeys` verificado presente no assembly). Console: `debug getStat ItemsCooked`. Game state query: `PLAYER_STAT Current ItemsCooked N`.

**Hook escolhido: polling do stat por delta em `GameLoop.UpdateTicked`.** A assinatura já existe no repo (`ModController.cs:807-811`). O serviço guarda o último valor de `ItemsCooked` observado por jogador; a cada tick lê o stat; `delta = atual − último > 0` ⇒ `delta` refeições cozidas ⇒ concede `delta` byproducts ao jogador local e atualiza o último valor. **Delta de contador monótono = idempotente por construção** (G3): cada refeição incrementa o stat exatamente uma vez, logo cada cozinha concede exatamente uma vez, e reload de save não pode duplicar (o stat persiste; o último valor é reseeded — §9). Custo: uma leitura de `uint` por tick por jogador — o wiki (rev 190144) pede cache apenas para operações *caras* em eventos frequentes; leitura de stat não é.

**Alternativas rejeitadas (e por quê):**

| Alternativa | Por que rejeitada |
|---|---|
| **Harmony em `CraftingRecipe.CreateItem`** (postfix checando `isCookingRecipe` — ambos verificados presentes no assembly) | Não é evento SMAPI — é patch de interno. Assinaturas de métodos internos mudam entre versões do jogo (churn 1.6.x quebraria o mod silenciosamente); o repo **não tem** pacote Harmony; o stat é superfície *documentada e estável*. Rejeitado por fragilidade + dependência nova. |
| **`Player.InventoryChanged`** (heurística) | Dispara para **toda** mutação de inventário (compra na loja, presente, baú); não distingue refeição cozida de comprada sem rastrear o menu aberto — falsos positivos quebram a garantia de exatidão-concessão (G3). Rejeitado. |
| **`GameLoop.DayStarted`** (liquidação por diff diário) | Dado certo, timing errado: o byproduct chegaria na manhã seguinte, e a concessão no start do dia pode atingir inventário cheio (perda de item). Rejeitado por UX; serve como reconciliação em spec futura, se necessário. |
| **`World.ObjectListChanged`** | Só dispara para objetos em *locations*; a refeição vai direto ao inventário do jogador e nunca toca uma location. Rejeitado. |
| **`Display.MenuChanged`** observando `CraftingPage` | Fechar o menu não diz *o que* nem *quanto* foi cozido; dispara para todo menu. Rejeitado. |
| **`Modding:Trigger actions`** (rev 190039) | A própria wiki diz "C# mods should usually use SMAPI events instead" — e nenhum trigger action existente observa cozimento. Rejeitado. |

> Nota de nomenclatura: a wiki lista o evento por tick como `UpdateTicking`; o repo usa `UpdateTickedEventArgs` (`ModController.cs:807`). **Verificar o nome exato contra a API SMAPI 4 no build com `GamePath`.**

## 5. Onde o item aterrissa

**No inventário do jogador que cozinhou** — `Game1.player.addItemToInventoryBool(byproduct)` (método verificado presente no assembly), com fallback `Game1.createItemDebris(byproduct, playerTile)` se o inventário estiver cheio (também verificado; `createItemDebris` é o padrão vanilla para produção sem espaço — item nunca é destruído silenciosamente).

Por quê, contra os análogos vanilla:

- **Leite do estábulo (analogia positiva):** ordenhar a vaca coloca o leite **diretamente no inventário de quem ordenhou** — o ator da ação recebe o produto. Cozinhar tem a mesma forma: ação do jogador, sem máquina intermediária.
- **Maionese / vinho (artisan goods):** o output **fica na máquina** até o jogador coletar — porque há um container (`Data/Machines`). Cozinhar não tem máquina; não há onde segurar o byproduct.
- **Trufa de porco:** o porco deixa a trufa como **objeto no chão** da farm. Rejeitado: objetos no chão são estado da *location* — sob Principle VI (`constitution.md:32`) isso é estado de farm, host-autoritativo; spawnar um ground object a partir de um evento de cozimento *per-player* cruzaria a fronteira de autoridade sem necessidade (o jogo já sincroniza inventário em co-op).
- **Slot de máquina / tile de estação:** não se aplica — não há máquina nem estação no caminho do cozimento (a cozinha é uma aba do `CraftingPage`, não um big craftable).

**Consequência multiplayer (Principle VI):** cada cliente observa o **próprio** stat (stats são por jogador e não sincronizam — `Modding:Stats`) e concede ao **próprio** jogador local; o host nunca precisa observar estatísticas de farmhand, e nenhuma concessão cruzada acontece. O byproduct vira estado de farm apenas quando entra na composteira (`InputItemId` persistido host-side, `CompostingBinService`).

## 6. Categoria e context tags — e o trigger que passa a aceitá-lo

**Categoria: `-7` (Cooking)** — `Object.CookingCategory`, tag automática `category_cooking` (wiki `Modding:Items`, rev 193945). **Mas** `Type = "Basic"` (não `"Cooking"`): `Type: Cooking` + `Category: -7` contam para a coleção de pratos / Gourmet Chef / Perfection (`Modding:Objects`) — o byproduct é resíduo, não prato; a decisão final cabe à spec. `Edibility = -300` (não-comestível — "An item with an edibility of -300 can't be eaten", `Modding:Objects`).

**Context tags:** `compostable_item` (funcional — o opt-in de autor de mod que a spec 002 criou para exatamente isto, `spec.md:93`) + `cooking_waste` (semântica, reservada para consultas futuras; nenhum trigger a lê hoje).

**Trigger: o 6º já aceita — não precisa de 7º.** Trigger 6 (`["compostable_item", "!not_compostable"]`, `data-entries-machines-recipes.md:203`) casa o byproduct no lado data; no lado C#, o ramo de tag do validador (`OrganicWasteValidator.cs:34-37`) aceita **antes** do switch de categorias. Paridade preservada sem tocar nos 6 triggers nem no validador. Um 7º trigger (`category_cooking` + algo) só faria sentido para aceitar *toda* comida cozida — o que a spec 002 proíbe explicitamente — ou uma categoria customada modded, para a qual a wiki não documenta faixa sancionada (a tabela de categorias lista só valores vanilla) e que acrescentaria um caso novo ao switch do validador. YAGNI. `CustomFields` não é lido por nenhum trigger nem pelo validador (`organic-waste-validator.md:254`: "CustomFields são ignorados para este fim") — não serve como portão.

**Contraste com `organic-waste-validator.md:176`:** a tabela diz que `-7` (`category_cooking`) é "comida pronta" e que o futuro Cooking Waste será "**um byproduct novo, não a comida em si**". É exatamente o que acontece: uma Salada (-7, sem tag) continua **rejeitada** nos dois portões (switch `_ => false`; nenhum trigger positivo casa `category_cooking`); o byproduct é aceito **apenas** pela tag explícita do autor — o mesmo mecanismo de exceção já documentado para artisan good com `compostable_item` (`organic-waste-validator.md:168`: "tag do autor vence"). A categoria -7 é **identidade** (de onde vem); a tag é o **portão** (o que a composteira aceita).

**Se plantado / dado / consumido (G5):** -7 não tem propriedade `Is Placeable` (só -8/-9/-19/-74 têm, wiki `Modding:Items`) ⇒ não planta nem vira tile; `Edibility -300` ⇒ não pode ser comido; presente: `CanBeGivenAsGift` default true — **recomendar `false`** na spec (ninguém recebe resíduo de presente; gift taste fica para a spec decidir); lixo/chão: ciclo de vida vanilla de item, podendo reentrar na composteira depois.

## 7. Inventário de assets

- **Sprite 16×16:** `LivingRoots/Assets/cooking_waste.png` (frame único, 16×16 — igual ao `Assets/compost.png`), registrado como asset custom `LivingRoots/CookingWaste` via `e.LoadFromModFile<Texture2D>("Assets/cooking_waste.png", AssetLoadPriority.Medium)` em `OnAssetRequested` (padrão SMAPI 4, mesmo do 004 §7); `ObjectData.Texture = "LivingRoots/CookingWaste"`, `SpriteIndex = 0`. Arte: **autoral** — raspas/cascas de cozinha em paleta terrosa (amarroentado em `MulchColor #92400E`, `Constants.cs:53`, com verde `CompostReadyColor #166534`), 3–4 shades por hue, outline 1 px (padrão `compost-icon.png.md:180-191`).
- **Licensing (restrição dura, `compost-icon.png.md:193-197`):** arte de Stardew Valley e `springobjects.png` são proprietárias (ConcernedApe LLC); **proibido** copiar, tracejar, recolorir ou vendorizar pixels vanilla em `LivingRoots/Assets/`. Referenciar estilo é permitido; copiar pixels não. Ferramentas: Aseprite/GIMP/Krita/Paint.NET. (O repo já quarentena assemblies proprietários fora da árvore via `.gamepath/` ignorado — mesma lógica.)
- **SFX:** cue vanilla **`coin`** (`Modding:Audio`, rev 193491: "A 'pop' sound… when the player picks up objects") — o pop de "item apareceu no inventário", coerente com a concessão; precedente do repo: `CompostCraftingSound = "axe"` (`Constants.cs:72`). Alternativa `crafting` — mas a wiki o documenta como "hammer clanking on a nail… Rock Crab", semanticamente fora. Confirmar o cue no build/jogo; reproduzir via `Game1.playSound(ModConstants.CookingWasteSoundCue)`.
- **Áudio próprio `.ogg`:** não necessário (cue vanilla basta); se a spec quiser um cue custom, via `Data/AudioChanges` (rev 193491) — YAGNI. Os `.ogg` existentes em `LivingRoots/Assets/` são placeholders não carregados (AGENTS.md raiz).

## 8. Data entries

**`Data/Objects`** (novo item) — no `OnAssetRequested` (`ModEntry.cs:130-156`); **este repo NÃO usa Content Patcher**:

```csharp
data[ModConstants.CookingWasteItemId] = new StardewValley.GameData.Objects.ObjectData
{
    Name = "Cooking Waste",
    DisplayName = _translation.Get("cooking-waste.name"),
    Description = _translation.Get("cooking-waste.description"),
    Type = "Basic",
    Category = ModConstants.CookingWasteCategoryId,   // -7
    Price = 0,
    Edibility = -300,                                 // não-comestível
    Texture = ModConstants.CookingWasteTextureName,   // "LivingRoots/CookingWaste"
    SpriteIndex = 0,
    ContextTags = new List<string> { "compostable_item", ModConstants.CookingWasteTag },
    CanBeGivenAsGift = false,
};
```

**`Data/Machines`: nenhum acréscimo** — trigger 6 já aceita `compostable_item` (§6); os 6 triggers e `DaysUntilReady = 2` ficam como estão (T022a continua travando `Triggers.Count == 6`).

**`Data/CookingRecipes`: nenhum acréscimo** — o byproduct **não** é yield de receita (formato `"ingredientes/par-unused/yield/unlock/name"`, `Modding:Recipe data` rev 193377); ele nasce do stat de cozimento, não de uma receita. Adicionar uma receita que o produzisse seria outra mecânica e quebraria a garantia de idempotência por stat.

Registro do asset de textura (antes dos ramos `e.Edit`):

```csharp
if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.CookingWasteTextureName))
{
    e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>(
        "Assets/cooking_waste.png", StardewModdingAPI.Events.AssetLoadPriority.Medium);
}
```

Notas de implementação: `_translation` exige guardar o helper no composition root (`ModEntry.Entry` não retém o helper hoje — mesma mudança pedida por 004 §7 / `i18n-localization.md` §2.1). **Verificar no build com `GamePath`:** o tipo de `ObjectData.ContextTags` (esperado `List<string>`) e o nome exato do evento por tick (§4).

## 9. Idempotência e concorrência (Principle VI)

**Dono: o jogador que cozinhou.** O byproduct é item de inventário pessoal — como o leite de quem ordenhou — **não** estado de farm. Base: o stat observado é por jogador ("Stats are not synced in multiplayer, so farmhands can only access their own stats", `Modding:Stats`), e a concessão usa a API de inventário do próprio jogador local, que o jogo já sincroniza em co-op.

**O que Principle VI obriga** (`constitution.md:28-37`): "Exactly one authority owns the truth for a given farm" — mas isso é sobre **estado de farm** (saúde do solo, bins). O byproduct não é estado de farm até entrar na composteira; depois vira (`InputItemId` persistido host-side, `CompostingBinService`). Consequências de design:

1. **Nenhuma concessão cruzada:** o host **nunca** concede para um farmhand — o client do farmhand observa o próprio stat e concede ao próprio jogador. Sem `ModMessageReceived`, sem host round-trip: a fronteira de autoridade não é cruzada.
2. **O save do host é a única verdade persistida** (`constitution.md:33`): o byproduct persiste como Object no save do jogador (mecanismo vanilla); o stat `ItemsCooked` persiste por jogador. O estado do serviço (último valor observado) é **efêmero** — reseed no `SaveLoaded`/primeiro tick com o valor **corrente** do stat, senão um farmhand que entra no meio do jogo com seed 0 receberia um lote gigante de uma vez (armadilha real — risco R2).
3. **Idempotência por construção:** delta de contador monótono (`uint`, 0 a 4.2 bilhões). Cozinhar duas vezes ⇒ stat +2 ⇒ **duas** concessões, uma por refeição — nunca zero, nunca três. Concessão duplicada por re-entrancy é impossível porque não há evento a reentrar: o handler síncrono lê, concede e atualiza o último valor na mesma passagem (handlers SMAPI rodam síncronos, rev 190144).
4. **Divergência explícita** (`constitution.md:34`): se client e host divergirem (impossível por design aqui — nenhum estado compartilhado é escrito), a regra é: inventário local é a verdade para itens pessoais; o save do host é a verdade para o farm. Registrado aqui basta.

## 10. Efeito sobre `MaturationDays` / `RestorationAmount`

**Nada muda.** `MaturationDays = 2` (`Constants.cs:59`) e `RestorationAmount = 15f` (`Constants.cs:58`) são **input-agnósticos** (spec 002: 1 waste → 1 compost em 2 dias, +15 saúde — clarificações 2026-09-06/09). O byproduct é apenas mais um input válido: entra na bin vazia (portão `IsValidOrganicWaste`), processa 2 dias (`DaysUntilReady = ModConstants.MaturationDays` no trigger 6), sai 1 compost com restauração de 15. Nenhuma constante, nenhum campo de `Data/Machines`, nenhum teste existente muda.

**Por que não diferenciar:** acelerar resíduo de cozinha (ex.: 1 dia, "green waste composta mais rápido") exigiria **regra por input** — o modelo de máquina suporta (`OutputItem` por trigger, 004 §6.1), mas é emenda à spec 002 (valores travados), não a este briefing. YAGNI até playtest mostrar que importa.

## 11. Posicionamento contra as mecânicas concorrentes

- **`joja-fertilizer.md` (antagonista):** Joja é o polo destrutivo — boost imediato de crescimento a **−5 de saúde do solo** por aplicação (`JojaFertilizerSoilCost`, §1 do briefing), vendido só no Joja Mart, odiado pela Elena. O Cooking Waste é o polo oposto do mesmo eixo: **não consome solo — devolve valor a ele** via o loop cozinha → composteira → solo. Ensinam juntos o tradeoff central do mod (input × processo, `AGROECOLOGY_CONCEPTS.md:28`, citado lá): um destrói solo por conveniência; o outro recupera solo por processo. Diferença de arquitetura: Joja é hook **inverso** ao do compost (aplicação no solo, `ModController.OnButtonPressed`); Cooking Waste é hook **na origem** (produção), alimentando a mesma composteira.
- **`living-pharmacy-tonics-sprays.md` (mecânica concorrente "natural"):** tônicos/sprays de ervas (Elena/Linus) são uma **linha de produtos paralela** — ervas → tônicos (buffs, `Data/CookingRecipes`, Category -7) e sprays (proteção de cultivo, Category -26, solo intocado). Cooking Waste não compete: fecha o **loop da cozinha** (o que a cozinha *devolve* ao sistema), enquanto a farmácia acrescenta o que o pomar *produz*. Fronteira importante: o briefing de tônicos já trava "matéria-prima (erva) Category -81 = compostável; PRODUTO (tônico/spray) Category -7 ou -26 = NÃO compostável" (§3) — o Cooking Waste é a **exceção deliberada** a essa regra de produto: um item -7 que **é** compostável via tag explícita, porque é resíduo, não produto. As duas mecânicas coexistem sem sobreposição de tag (nenhuma usa `cooking_waste`).

## 12. i18n

```json
{
  "cooking-waste.name": "Cooking Waste",
  "cooking-waste.description": "Kitchen scraps from cooking. Feeds the Composting Bin like any organic waste.",
  "message.cooking-waste.inventory-full": "Inventory full — cooking waste dropped at your feet."
}
```

Padrão do repo: `compost.name` / `composting-bin.description` para itens (`i18n/default.json:1-5`), `message.*` para feedback (`default.json:9-12`). `DisplayName`/`Description` via `_translation.Get(chave).Default(fallbackEN)` (padrão 004 §8); tokenizáveis desde o dia 1 (lição de `living-pharmacy-tonics-sprays.md:77`: não hardcodar em `DisplayName`).

## 13. Constantes

```csharp
// Decay & Compost Constants — Cooking Waste (LivingRoots/Constants.cs, classe ModConstants)
public const string CookingWasteItemId = "LivingRoots.CookingWaste";
public const string QualifiedCookingWasteItemId = "(O)LivingRoots.CookingWaste";
public const int CookingWasteCategoryId = -7;        // Object.CookingCategory (wiki Modding:Items)
public const string CookingWasteTag = "cooking_waste";
public const string CookingWasteTextureName = "LivingRoots/CookingWaste";
public const string CookingWasteSoundCue = "coin";   // Modding:Audio rev 193491
```

Convenção: tudo em `LivingRoots/Constants.cs`, classe `ModConstants` (igual ao par `CompostItemId`/`QualifiedCompostItemId`, `Constants.cs:63-64` — ambas as formas existem e ambas devem ser comparadas em `heldItem.QualifiedItemId`, AGENTS.md raiz). **Não** constantizar as listas de `RequiredTags` (`List<string>` mutável não cabe em `const` — decisão 004 §9); os literais `compostable_item`/`not_compostable` continuam em `OrganicWasteValidator.cs:27,34` + trigger 6, e se um dia centralizarem, centralizam nos dois lugares na mesma mudança (004 §9).

## 14. Plano de testes

| Arquivo (`LivingRoots.Tests/`) | O que muda |
|---|---|
| `AssetRegistrationTests.cs` | Estender o teste de contrato de `Data/Objects`: entrada `LivingRoots.CookingWaste` existe, `Category == -7`, `Type == "Basic"`, `Edibility == -300`, `ContextTags` contém `compostable_item` e `cooking_waste`, `Texture` não-vazia. T022a (`Machines_FullRuleContract`) **não muda** — continua travando `Triggers.Count == 6`; **adicionar** assert de que o byproduct casa o trigger 6 (união dos positivos inalterada). |
| `OrganicWasteValidatorTests.cs` | Novo: item -7 **com** `compostable_item` → `true` (caso do byproduct); item -7 **sem** tag (Salada) → `false` (trava o contraste §6). Requer o seam de tags (`GameStateFixture.objectData` + `ItemFactory` por ID registrado) prescrito em `organic-waste-validator.md` §6.2 (T-VAL-INCL). |
| `CookingWasteServiceTests.cs` (**novo** — um arquivo por assunto, constitution IV) | Matemática do delta: stat 10→11 concede 1; 10→10 não concede; 10→12 concede 2; seeding no load **não** concede; inventário cheio → fallback debris; guarda `IsDisposed()`. |
| **T-NEW `MachineTagsAgreeWithValidator`** (teste de **paridade** prescrito em `data-entries-machines-recipes.md:327`) | Para amostra de IDs vanilla por categoria (fixtures reais do repo) **+ o byproduct** + item `not_compostable` + item `compostable_item`: `validator.IsValidOrganicWaste(item) == tagsCasam(item)` (OR dos 6 triggers). O byproduct entra na amostra como caso verdadeiro-em-ambos-os-lados — é o item que mais depende da paridade (aceito só por tag, nos dois portões). |

Não mudam: `FeatureStatusTests.cs` (README não editado), `ArchitecturalPlanTests.cs` (nenhum arquivo novo em `docs/architectural_and_refactor_plans/`), `ModControllerTests.cs` (o handler de tick existente só ganha uma chamada de serviço — mock do serviço novo no construtor, padrão dos mocks de `IOrganicWasteValidator`).

## 15. Verificação em jogo

1. Build: `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` (`.gamepath/` local; ver AGENTS.md raiz).
2. Rodar via SMAPI, save de teste.
3. `debug getStat ItemsCooked` (comando de console SMAPI, documentado em `Modding:Stats`) — anotar o valor antes.
4. Cozinhar **Fried Egg** (desbloqueada desde o início — `Modding:Recipe data` §Exceptions) na cozinha da farmhouse → byproduct aparece no inventário com cue `coin`; `debug getStat ItemsCooked` +1.
5. Idempotência: cozinhar duas Fried Eggs seguidas → **dois** byproducts (não um, não três).
6. Reload do save → sem concessão duplicada (o stat persistiu; o seeding é silencioso).
7. Compostagem: segurar o byproduct + clique-direito em Composting Bin vazia → entra em processamento (2 dias), igual a um vegetal.
8. Multiplayer (Principle VI / Gate 3 da constitution): farmhand cozinha → **farmhand** recebe o byproduct; o host não recebe nada; só vira estado de farm quando entra na bin.
9. Inventário cheio: cozinhar com inventário full → byproduct vira debris aos pés + mensagem `message.cooking-waste.inventory-full`.
10. Log SMAPI: linhas Trace do serviço (concessões, seeding) sem exceção crua (regra de segurança: `ex.GetType().FullName` + HResult apenas).

## 16. Riscos

1. **Detecção por stat tem cego:** cozinhar por menus de cozinha custom de **outros mods** que adicionam itens diretamente ao inventário sem passar pelo sistema de receitas **não** incrementa `ItemsCooked` — o byproduct não nasce. Limitação aceita (superfície documentada vs patch de interno); documentar no README da feature quando implementado.
2. **Armadilha de seeding:** seed 0 no `SaveLoaded` de um farmhand que já cozinhou muito no save ⇒ concessão em lote. Mitigação travada em teste (§14).
3. **Receitas com yield > 1:** o stat conta *itens criados*; receita com yield 2 concederia 2 byproducts (1 por item) — semanticamente correto ("um resíduo por refeição produzida"), mas nenhuma receita vanilla de cozimento tem yield > 1 (dados brutos, `Modding:Recipe data`). Revisitar se mod de receitas adicionar yields.
4. **Churn de versão do jogo:** `StatKeys.ItemsCooked` é API documentada (rev 191160), mas nomes de stat podem mudar entre grandes versões — o build com `GamePath` e o update check do SMAPI cobrem; `CraftingPage`/`isCookingRecipe` só importam se um dia alguém trocar o polling por Harmony.
5. **Nomenclatura do evento por tick:** a wiki lista `UpdateTicking`; o repo usa `UpdateTickedEventArgs` — confirmar no build (§4).
6. **Concorrência:** handlers SMAPI são síncronos (rev 190144), então o delta não sofre race entre ticks; o serviço deve respeitar `IsDisposed()` e o padrão de guard do controller (`ModController.cs:809`). Nenhuma escrita de estado de farm acontece no hook — só inventário local —, logo `ExecuteWithConcurrencyGuard` não é necessário para a concessão; se a spec mover a concessão para o host, aí sim.
7. **Gift:** `CanBeGivenAsGift = false` é recomendação; gift taste de NPCs para o item é neutral por default até a spec decidir.

## 17. Fora de escopo

- **Velocidade de compostagem diferenciada** para cooking waste (ex.: 1 dia) — emenda à spec 002 (§10).
- **Byproduct como ingrediente de cozinha** (receita em `Data/CookingRecipes` que o consome) — mecânica futura possível; YAGNI.
- **Máquina/estação de cozinha custom** (big craftable, `Modding:Big craftables` rev 190701) ou **triggered tiles** (map actions, `Modding:Trigger actions` rev 190039) para produzir o byproduct — desnecessário: o cozimento vanilla já é observável via stat.
- **Content Patcher** — o repo não usa; tudo via `AssetRequested` (AGENTS.md raiz).
- **Byproduct comestível / com buffs** — território de `living-pharmacy-tonics-sprays.md`; `Edibility -300` é travado aqui.
- **Sincronização de stats em multiplayer** — stats são por jogador por desenho vanilla; não tentar sincronizá-los (`constitution.md:33` proíbe segunda fonte de verdade).
- **Tag `cooking_waste` consumida por outra máquina** — reservada; nenhum consumo hoje.

---

Última verificação: 2026-10-05 — wiki revs citadas via API MediaWiki (stardewvalleywiki.com); scan de strings do assembly `.gamepath/Stardew Valley.dll` (`ItemsCooked`/`ItemsCrafted`/`StatKeys`/`CraftingPage`/`isCookingRecipe`/`CreateItem`/`addItemToInventory(Bool)`/`createItemDebris`/`GetBaseContextTags`/`DoAllTagsMatch` presentes; `CookingRecipeMenu`/`CookingMenu`/`ClickCraftingRecipe` **ausentes**); internos lidos (`ModEntry.cs:120-182`, `ModController.cs:807-883`, `OrganicWasteValidator.cs:1-54`, `Constants.cs:55-79`, `i18n/default.json`, `constitution.md:28-37`, `data-entries-machines-recipes.md:192-253,327`, `compost-icon.png.md:193-197`, `joja-fertilizer.md`, `living-pharmacy-tonics-sprays.md`, `PlayerProvider.cs:10-17`). Membros marcados "verificar no build com `GamePath`" (nome do evento por tick, tipo de `ObjectData.ContextTags`, campo `cookingMenu`) permanecem por confirmar contra os assemblies no build.
