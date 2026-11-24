# Briefing — A casa da Elena / back room da Seed Shop (interior + tileset)

> **Escopo:** a localização interior que dá casa à Elena e o tileset que a sustenta: `Data/Locations`, mapa `.tmx`, tileset, `Warp`/`Door`/`Action`, luz/música, e a interação com a `Data/Shops` e o schedule JSON já especificados em `elena-npc.md`.
> **Status:** briefing (não implementado). Insumo para futuro `/speckit.specify` — **não é especificação**.
> **Agrupado de origem:** `elena-npc.md` §1 (decisão travada "quarto placeholder no SeedShop") e §3.1 (loja). **Briefing-filho** — não contradiz IDs, chaves de schedule, nem o design da loja do briefing-pai.
> **Por que este briefing existe:** é a **única localização de NPC do roadmap sem briefing de tileset próprio**. Todo outro briefing que cria uma localização (`adobe-shed-green-roof.md:20-22`, `greywater-recycling.md:24,105-107`, `collective-kitchen-meals.md:99,103`, `weekly-fair.md:76`, `mobile-coop.md:20-21`, `csa-box.md:23`, `food-sovereignty-guardian-trophy.md:31,81`) já traz §"Sprite" com inventário de asset.
> **Leitura interna:** `elena-npc.md` (inteiro), `adobe-shed-green-roof.md` (formato canônico), `Constants.cs` (paleta), `ModEntry.cs:31,120` (seam de conteúdo), `LivingRoots.csproj:26` (cópia de `Assets/**`), `AGENTS.md` (GamePath/gates).

**Fontes primárias externas, com data de revisão** (todas lidas em 2026-10-05):

| Página | oldid | Última edição |
|---|---|---|
| `Modding:Location data` | 186972 | 14 jun 2025 |
| `Modding:Maps` | — | ver nota abaixo |
| `Modding:Shops` | 192231 | 13 abr 2026 |
| `Modding:NPC data` | 193090 | 17 mai 2026 |

> `Modding:Locations` **não existe** (404). A página é `Modding:Location data` e traz a lista de locations vanilla na seção *Location names*.
> `Modding:Shop data` **não existe** (404). A página é `Modding:Shops`.
> `Modding:NPC schedules` **não existe** (404). Schedules são `Modding:Schedule data` (delegado a partir de `Modding:NPC data` §1.5).

---

## 1. Conceito de jogo

- **O que a casa é:** o lugar onde a Elena vive e dorme e — funcionalmente — onde ela **ensina as cinco mecânicas de Year 1**. `ROADMAP.md:34` define Elena como "An agronomist and activist who serves as the player's mentor, introducing new mechanics and quests"; as cinco mecânicas de `ROADMAP.md:35-40` (Welcome Kit, Mulch, Three Sisters, Heirloom Seeds, Cistern) são exatamente o repertório das conversas de 1 conceito/semana de `elena-npc.md:14`, e conversa repetível precisa de um lugar calmo — um balcão de loja não é um lugar.
- **Decisão travada no briefing-pai (não reabrir aqui):** `elena-npc.md:17` — "quarto placeholder no SeedShop (**backroom próprio é spec futura**)". `elena-npc.md:42-45` fixa o Y1 como `Home = SeedShop @ (2,8) down` com `// TODO specs: quarto próprio (SeedShop backroom) vs. casa nova`. **Este briefing é aquela spec futura.**
- **A pergunta real não é "qual mapa" — é "quem desenha os pixels".** §6 fecha essa questão: **zero pixels vanilla distribuídos**; um interior novo custa quase nada em arte autoral.
- **Relação com o romance:** `elena-npc.md:17,40` — `CanBeRomanced = false` no Y1, revisitar pós-Year-3. O interior **não** promete romance; é projetado com cama + mesa, que já é o set mínimo de uma rota futura sem rework. Compatível, não contraditório.

---

## 2. Gap table — decidido vs. em aberto

| # | Questão | Estado | Fonte |
|---|---|---|---|
| 1 | A casa é back room do SeedShop ou prédio novo na Town? | **DECIDIDO: back room** (§3) | este briefing |
| 2 | Existe location vanilla reaproveitável? | **RESOLVIDO: `Sunroom`** (§4.1) | `Modding:Location data` |
| 3 | Campos exatos de `Data/Locations` | **RESOLVIDO** (§7) | oldid 186972 |
| 4 | `.tmx` pode referenciar tilesheet vanilla por nome? | **RESOLVIDO: sim** (§6) | `Modding:Maps` §3.2.1, §8.2 |
| 5 | Geometria de `Data/Shops` força interior real? | **RESOLVIDO: não** (§10.2) | `Modding:Shops` |
| 6 | Schedule JSON aceita location custom? | **PARCIAL** — inferido, não confirmado (§9) | `Modding:Schedule data` — **verificar** |
| 7 | Caminho de carga em C# | **RESOLVIDO** (§12) | `ModEntry.cs:31,120` |
| 8 | Invariantes do Principle VI | **LIDO** (§13) | `.specify/memory/constitution.md` |
| 9 | Campos de `Door`/`Action`/`Warp` | **RESOLVIDO** (§8) | `Modding:Maps` §5.4, §5.7, §6.3 |
| 10 | IDs/índices de tilesheet vanilla | **TBD — verificar no build com `GamePath`** | jogo 1.6.x |
| 11 | Arte nova: nº de px genuinamente novos | **PARCIAL** — orçamento em §5 | este briefing |
| 12 | `AmbientLight` em `Data/Locations`? | **RESOLVIDO: não — é map property** (§9) | `Modding:Maps` §5.6 |

---

## 3. Opção A vs. Opção B — e a recomendação

### Opção A — Reusar o interior vanilla do SeedShop + edições de conteúdo

- **O quê:** a Elena fica **dentro do mapa `SeedShop` que já existe** (vanilla), com uma área marcada como "dela" por decoração e por um tile `Action`.
- **Custo de arte:** zero tileset. Zero `.tmx`.
- **Prós:** nenhum asset de mapa; nenhuma entrada nova em `Data/Locations`; nenhum risco de tilesheet malformado; multiplayer trivial.
- **Contras:**
  - **Colide com o backlog.** `elena-npc.md:17` chama o Y1 de "quarto **placeholder** no SeedShop" e diz que o backroom é spec futura. Opção A **é** o placeholder — não entrega o que motivou este briefing.
  - **O `SeedShop` vanilla não tem um quarto.** A tabela de locations vanilla (`Modding:Location data` §Location names) descreve `SeedShop` como "Pierre's general store (also Caroline/Abigail's house and church)" e lista `Sunroom` como location **à parte** — ou seja, o que existe de "cômodo" na Seed Shop já é uma location separada, não um canto do mapa do `SeedShop`.
  - **Editar mapa vanilla in-place é o ativo mais frágil do lote.** `Modding:Maps` §*Editing a vanilla location*: substituir um mapa vanilla inteiro via `Load` "is not recommended in most circumstances **due to low compatibility with other mods**". `EditMap` parcial é mais usado, mas um `EditMap` grande continua colidindo tile a tile com mods de NPC/layout de loja que já patcheiam `Maps/SeedShop`.

### Opção B — Enviar interior novo (`.tmx` + tileset vanilla referenciado + `z_*.png` mínimo)

- **O quê:** `LivingRoots/Assets/maps/elena_home.tmx`, tilesheets vanilla referenciadas **por nome**, mais um `z_elena_home.png` só com os tiles que o vanilla não tem.
- **Prós:**
  - **Arte quase zero** (§6): o mapa parte de uma cópia de um interior vanilla já existente; os pixels de parede/piso/porta/máquina são vanilla **referenciado em runtime**, nunca copiado.
  - Controle total do set (mesa de mudas, bancada de compostagem, estante de sementes, tanque de chuva) sem tocar nenhum mapa vanilla.
  - A cena de ensino funciona: uma sala, não um balcão.
  - Casa isolada = a rota pós-Year-3 (`elena-npc.md:17,40`) não exige rework.
  - Precedente vanilla direto: **`Sunroom`** é exatamente "uma sub-sala da Seed Shop que é sua própria location" (`Modding:Location data` §Location names).
- **Contras:** seam de código para carregar o mapa (§12); `z_` obrigatório para não reordenar tilesheets (§5); risco de warp manual mal configurado (§16.5).

### Recomendação (única, explícita)

> **Opção B — interior novo em `.tmx`, com tilesheets vanilla referenciadas por nome e um overlay `z_` mínimo autoral. Sem copiar nenhum pixel do jogo.**

Justificativa:

1. **Licença (§6) fecha a questão de custo.** Referenciar um tilesheet vanilla por nome dentro do `.tmx` do mod é ato permitido e documentado; a wiki inclusive descreve o truque do prefixo de ponto para trabalhar em Tiled **sem** distribuir a cópia. Isso derruba o único argumento forte da Opção A (custo de arte) e deixa o argumento forte da Opção B intacto (o `SeedShop` vanilla não tem quarto nenhum).
2. **Compatibilidade.** `Modding:Maps` é explícito: substituir mapa vanilla in-place tem "low compatibility with other mods". `Maps/SeedShop` é dos mapas mais patcheados do jogo (mods de NPC, de layout de loja, de shelves). A Opção B **não entra nessa guerra** — é um asset novo com nome único.
3. **Design.** `ROADMAP.md:35-40` — cinco mecânicas ensinadas numa sala. E o precedente `Sunroom` mostra que o jogo já modela "sub-sala da Seed Shop = location própria", então nem a topologia é invention.

> **Contingência registrada:** se o gate "verificar no build com `GamePath`" mostrar que `Maps/SeedShop` **já** tem uma porta interna para um cômodo não-mapeado, a Opção A' (reuso desse cômodo) passa a ser a escolha — mas isso muda a tese e **precisa de nova decisão**, não de TBD silencioso.

---

## 4. Pesquisa — o que foi verificado

### 4.1 Locations vanilla reaproveitáveis (nomes reais, `Modding:Location data` §Location names)

Interiores vanilla úteis como base/inspiração, com classe e descrição reais:

| Name | Classe | Descrição oficial |
|---|---|---|
| **`Sunroom`** | `StardewValley.GameLocation` | "**Caroline's sunroom inside Pierre's General Store**" ← **o precedente** |
| `SebastianRoom` | `GameLocation` | Sebastian's room in the basement of the carpenter house |
| `HarveyRoom` | `GameLocation` | Harvey's room upstairs from the clinic |
| `Trailer` / `Trailer_Big` | `GameLocation` | Pam/Penny's trailer (variante pós-upgrade) |
| `Tent` | `GameLocation` | Linus' tent |
| `IslandHut` / `LeoTreeHouse` | `GameLocation` | Leo's hut / tree house |
| `WizardHouseBasement` | `GameLocation` | basement of the Wizard's Tower |
| `AbandonedJojaMart` | `Locations.AbandonedJojaMart` | "small interior" |
| `MermaidHouse`, `Submarine` | `Locations.*` | interiores do Night Market |
| `BoatTunnel` | `Locations.BoatTunnel` | "in back of the fish shop" |

`Maps/SeedShop` é a location que **contém** `Sunroom` — confirma que a Seed Shop comporta sub-locations, e que o padrão do jogo para "cômodo dentro de loja" é location separada.

### 4.2 Campos de `Data/Locations` (oldid 186972)

A key é o **unique string ID**, que também vira o `Name` (não `DisplayName`) da location. Campos: `DisplayName`, `DefaultArrivalTile`, `CreateOnLoad { MapPath, AlwaysActive, Type }`, `CanPlantHere`, `CanHaveGreenRainSpawns`, `ExcludeFromNpcPathfinding`, `ArtifactSpots`, `FishAreas`, `Fish`, `Forage`, `MinDailyWeeds`/`MaxDailyWeeds`, `FirstDayWeedMultiplier`, `MinDailyForageSpawn`/`MaxDailyForageSpawn`, `MaxSpawnedForageAtOnce`, `ChanceForClay`, `Music`, `MusicDefault`, `MusicContext`, `MusicIgnoredInRain`, `MusicIgnoredInSpring/Summer/Fall/Winter`, `MusicIgnoredInFallDebris`, `MusicIsTownTheme`, `CustomFields`, `FormerLocationNames`.

**Três achados que corrigem suposições comuns:**

1. **Não existe campo `Characters`** em `Data/Locations`. O spawn de NPC vem **só** de `Data/Characters` → `Home`. Duas fontes de spawn seria um bug de warp (§16.4).
2. **Não existe `AmbientLight` em `Data/Locations`.** Luz é **map property** (`Modding:Maps` §5.6), assim como `Outdoors`, `LocationContext`, `AllowBeds`, `CanCaskHere`, `indoorWater`, `PassOutSafe`, `ViewportClamp`, `DefaultWarpLocation`.
3. **A map property `Music` está deprecada** — a wiki manda usar os campos `Music*` de `Data/Locations`.

`Type` em `CreateOnLoad` "**must be one of the vanilla types to avoid a crash when saving**"; os úteis são `StardewValley.GameLocation` (default) e `StardewValley.Locations.DecoratableLocation`. `AlwaysActive` é obrigatório para locations que permitam construir cabines.

### 4.3 Convenções de autoria de mapa (`Modding:Maps`)

- **Sempre partir de um mapa vanilla e editar** — "when making custom maps, **always start with a vanilla map and edit it**. Don't try to create a new map in Tiled; the game needs certain tiles, map properties, etc to be present." Esta é a regra que torna a Opção B barata.
- **Camadas padrão**, back→front: `Back`, `Buildings`, `Paths`, `Front`, `AlwaysFront`. Camadas extras por sufixo numérico (`Back-1`, `Back2`). "The original layers must be used for tile properties and collisions."
- **Largura > 200 tiles quebra o layering.** Altura livre.
- **`.tmx` > `.tbin`**: XML, editável, diffável em source control, suporta tile flip/rotation. Recomendado pela wiki.
- **Animação de tile:** todos os frames com o **mesmo** tempo — "the game can't handle variable frame times".

---

## 5. Inventário de tileset

| Asset | Caminho | Tamanho | Camadas | Origem dos pixels |
|---|---|---|---|---|
| Base do mapa | `LivingRoots/Assets/maps/elena_home.tmx` | derivado de interior vanilla pequeno; alvo **≤ 40×30 tiles** | `Back`, `Buildings`, `Paths`, `Front` | **vanilla, referenciado** (§6) |
| Overlay autoral | `LivingRoots/Assets/tilesets/z_elena_home.png` | grade 16×16 px; **orçamento: 1 folha, ~24–32 tiles** (ex.: 96×64 px) | `Buildings` + `Front` | **AUTORAL (autorais)** |
| Tilesheets vanilla | referenciadas no `.tmx` por nome (`spring_indoorsTileSheet` e afins) | — | — | **vanilla em runtime, NUNCA copiado** |

**Orçamento de tiles autorais (o que realmente precisa ser desenhado):**

| Tile | Camada | Por quê não existe no vanilla |
|---|---|---|
| Mesa de mudas (2 tiles, frente+uso) | `Buildings` | apoio de mudas + etiqueta de sementes |
| Bancada de compostagem | `Buildings` | a cena de ensino de `ROADMAP.md:35-40` |
| Estante de sementes heirloom | `Buildings` | `ROADMAP.md:38` — o estoque que o pai vende |
| Tanque de chuva / cisterna pequena | `Buildings` | `ROADMAP.md:40` |
| Placa "Elena" ou quadro de rotações | `Front` | identificación da casa |

Tudo o mais — parede, piso, porta, máquina, vaso, janela, luz — **reusa tilesheet vanilla**.

**Regras de tileset, ambas obrigatórias (`Modding:Maps` §8.1, §3.2.3):**

- **ID de tilesheet customizado começa com `z_`.** Sem isso, adicionar um tilesheet muda a ordem dos originais e o jogo passa a ler tile do tilesheet errado — de glitch visual a crash.
- No `.tsx` interno do mapa: `Embed in map` marcado.
- O `.png` customizado fica **na mesma pasta do `.tmx`** durante a autoria (o Tiled resolve por caminho relativo).

**Direção de arte** (`Constants.cs:48-53`): paleta já earthly do mod — `Mulch #92400E`, `Sun #FACC15`, `CompostReady #166534`, `Stone #78716C`. O interior deve seguir isso, não a paleta fria dos interiores vanilla.

---

## 6. Copyright-safety: `.tmx` referenciando tilesheet vanilla

**Resposta: sim, e é o caminho documentado.** Citações de `Modding:Maps`:

> **§3.2.1 "Re-using vanilla tilesets"** — "If you want to use a tilesheet like `spring_outdoorsTileSheet.png` in your map… You can avoid this by first exporting them from an existing map, then using **external tilesets**, instead."
> *("using" = referenciar o tilesheet vanilla por nome; a alternativa citada — re-animar e re-adicionar propriedades à mão — é o que se evita.)*

> **§8.2 "Local copy of a vanilla tilesheet"** — "When editing a map in Tiled, you may need to copy vanilla tilesheets like `path.png` or `spring_town.png` into the map folder **for Tiled to find**. If the tilesheet is still there when you load the game, **SMAPI will use it for your map instead of the game's vanilla tilesheet**, which may have unintended effects (e.g., **edits from recolor mods won't work in your map**)."
> "To avoid issues, you can either… **Delete vanilla tilesheets from the folder before testing or releasing the mod.**"
> "**Rename the tilesheet file to start with a dot** (like `.spring_town.png`) and reference that. When SMAPI loads the map in-game, **it'll automatically ignore the dot and look for `spring_town.png` in the local files or `Content/Maps` folder**."

### A distinção, declarada

| Ato | Veredito | Por quê |
|---|---|---|
| **Vendorizar** pixels proprietary — commitar `spring_indoorsTileSheet.png` no repo | ❌ **PROIBIDO** | `specs/004-playable-compost-loop/briefings/compost-icon.png.md:193-197`; e a wiki mostra o efeito técnico: mods de recolor param de funcionar no mapa |
| **Referenciar** o tilesheet vanilla por nome no `.tmx`, resolvido em runtime | ✅ **PERMITIDO** e é o fluxo documentado | §3.2.1 + §8.2. Nenhum byte é distribuído; é o mesmo ato que declarar `"Texture": "Tilesheet"` num `DrawLayer` |
| **`.xnb`** | ❌ **NÃO USAR** | O mod envia `.tmx`/`.png`; xTile empacota. Um `.xnb` autoral exigiria pipeline de build e é onde o `.png` proprietary acabaria vendorizado por acidente |
| **`bundle.json`** | ❌ **NÃO NECESSÁRIO** | É só para empacotar uma content pack de asset com `bundle.json` de dependência; mapa + PNG num mod SMAPI C# é `Load`/`LoadMap` + `<Content Include="Assets\**\*.*">` (`LivingRoots.csproj:26`), que **já copia** `.tmx` e `.png` |
| **Caminho instalado** | `Mods/<id>/assets/maps/elena_home.tmx` | convenção SMAPI; asset name em runtime `Maps/LivingRoots_ElenaHome` |

### Regras concretas de autoria

1. **O `.tmx` do repo NÃO pode conter nenhum PNG vanilla ao lado.** Só o `z_elena_home.png` autoral.
2. Em Tiled, use o truque do ponto (`.spring_indoorsTileSheet.png`) enquanto trabalha, e **apague/renomeie** antes de commitar.
3. Um teste automatizado (§14) faz o gate mecânico disso: *"nenhum arquivo sob `LivingRoots/Assets/` tem o nome de um tilesheet vanilla"*.
4. A LICENSE/README do mod deve dizer, em uma linha, que nenhum asset do jogo é distribuído.

---

## 7. Entry de `Data/Locations` — tabela

Confirmado contra `Modding:Location data` (oldid 186972). **Não há campo `Characters`** — o spawn é `Data/Characters` → `Home`.

```json
// Data/Locations
{
  "LivingRoots_ElenaHome": {
    "DisplayName": "{{i18n:elena.home.name}}",
    "CreateOnLoad": {
      "MapPath": "Maps/LivingRoots_ElenaHome",
      "Type": "StardewValley.GameLocation"
    },
    "DefaultArrivalTile": { "X": 7, "Y": 8 },     // confirmar com o build
    "CanPlantHere": false,
    "ExcludeFromNpcPathfinding": false,
    "MinDailyWeeds": 0,
    "MaxDailyWeeds": 0,
    "Music": [ { "Id": "Default", "Track": "<TBD — cue vanilla de interior calmo>" } ],
    "MusicContext": "Default",
    "MusicIsTownTheme": false,
    "FormerLocationNames": [ "LivingRoots_ElenaBackroom" ]
  }
}
```

| Campo | Valor | Nota |
|---|---|---|
| key | `LivingRoots_ElenaHome` | unique string ID; **vira o `Name` da location** — é esse nome que `Home` e o schedule referenciam |
| `CreateOnLoad.MapPath` | `Maps/LivingRoots_ElenaHome` | asset name do `.tmx` |
| `CreateOnLoad.Type` | `StardewValley.GameLocation` | **só tipos vanilla**, senão crash no save (`Data/Locations` FAQ) |
| `CreateOnLoad.AlwaysActive` | omitir | só obrigatório se a location permitir construir cabins — não é o caso |
| `DisplayName` | `{{i18n:elena.home.name}}` | tokenizável, padrão Dobson |
| `DefaultArrivalTile` | **TBD** | fallback quando o warp não especifica tile |
| `FormerLocationNames` | `LivingRoots_ElenaBackroom` | o mecanismo oficial de save-compat do jogo (§16.3) |

**Map properties ficam no `.tmx`, não aqui** (`Modding:Maps` §5.6, §5.10):

| Map property | Valor | Nota |
|---|---|---|
| `AmbientLight` | `95 95 95` | exemplo da wiki para interior diurno "normal" |
| `AmbientNightLight` | `250 204 21` (`Sun`) | casa acesa à noite — direção de arte do mod |
| `Warp` | `7 10 Town <x> <y>` | saída para a Town (§8) |
| `Doors` | ver §8 | obrigatório junto de `Action Door` |
| `LocationContext` | `Default` | clima, mainland |
| `Outdoors` | omitir | é interior |
| `CanCaskHere` / `AllowBeds` | omitir | YAGNI |
| `ViewportClamp` | **TBD** | limita o scroll num interior pequeno |
| `PassOutSafe` | omitir | a Elena não passa out |

---

## 8. Propriedades de tile: `Warp`, `Door`, `Action`

Todas confirmadas em `Modding:Maps` §5.4, §5.7 e §6.3.

| Onde | Propriedade | Valor | Nota |
|---|---|---|---|
| `Maps/Town` (fora) | map `Warp <fromX> <fromY> <toArea> <toX> <toY>` | `… LivingRoots_ElenaHome 7 10` | entrada pela porta na Town; **`EditMap` no mapa `Town` é o único ponto de toque num mapa vanilla** — e é o mais barato possível (um warp só) |
| dentro | map `Warp <fromX> <fromY> Town <x> <y>` | retorno | volta ao tile da porta |
| `Buildings` (tile de baixo da porta) | `Action Door` | `Action Door` | "If used without NPC names, the door can be opened at any time"; **colocado no tile de baixo dos dois tiles da porta** |
| map property | `Doors <x> <y> <sheetID> <tileID>` | `<x> <y> <sheet> <idx>` | **"both are required for interior doors to function"** — `Doors` é o que dá a animação/funcionalidade |
| `Buildings` | `Action OpenShop LivingRoots_ElenaShop` | + `[from direction]` `[open time]` `[close time]` `[owner tile area]` | §10 |
| `Buildings` | `Action Warp <x> <y> <area>` | **não usar** — `Warp` de map já faz isso | alternativa mais explícita se o warp de map colidir |
| `Back` | `Bed T` + `TouchAction Sleep` | **só jogador** | é a cama do NPC; a Elena dorme via `Home`/schedule, não via `Bed` |
| `Buildings` | `Action Dialogue "{{i18n:…}}"` | rótulo da bancada | o "1 conceito/semana" pode ser lido sem NPC presente |

> **Nota sobre `Doors`:** `<sheetID>` é o **nome do sheet**, `<tileID>` o índice. É o único ponto onde um índice vanilla entra em Disable o briefing — **confirmar o par exato no build com `GamePath`**, não inventar (§16.5).

---

## 9. Luz, música e o schedule JSON

**Luz.** `AmbientLight`/`AmbientNightLight` são map properties (§5.6), não campos de `Data/Locations`. A casa é um interior: escuro à noite por default, aceso pelo `AmbientNightLight` quente. `IgnoreLightingTiles` não usar (não queremos ignorar as luzes Front).

**Música.** Campo `Music` de `Data/Locations` (a map property `Music` está **deprecada**). Reusar cue vanilla de interior calmo — **TBD: escolher o `Track` exato** consultando `Audio` com o jogo aberto; nenhum asset de áudio novo (consistente com `elena-npc.md:77`, "Nenhum custom Y1"). `MusicContext: Default`, `MusicIsTownTheme: false` para a música não vazar para a Town.

**Schedule — não mudar o formato do pai.** `elena-npc.md:59-67` já tem `800 SeedShop 2 8 0 / …`. Duas leituras possíveis:

| Opção | Diff no schedule | Risco |
|---|---|---|
| **B1** — `Location` aceita location custom | muda `SeedShop 2 8` → `LivingRoots_ElenaHome <x> <y>` nas 6 entradas de `elena-npc.md:62-66` | Baixo se confirmado. Os testes do pai (§7, `elena-npc.md:102`) continuam válidos: só o literal muda, as **5 chaves** (4 estações + rain) permanecem |
| **B2** — não aceita | manter `SeedShop` no schedule e warpar de volta | Ruim: obriga `EditMap` no `SeedShop`, que é o mapa que a Opção B existe para não tocar |

**Estado: PARCIAL.** O `Home.Location` de `Data/Characters` é confirmado como "the **internal name** for the home location" (oldid 193090), e o nome interno de uma location custom é exatamente a key de `Data/Locations` (§7) — logo `Home` aceita location custom sem dúvida. Para o schedule, a referência canônica é `Modding:Schedule data`, **não lida nesta sessão**: confirmar que o `Location` do schedule resolve pelo mesmo nome interno. **Gate explícito em §18.**

Consequência de `Home` + save existente: `Data/NPC data` §Spawn rules — `SpawnIfMissing` add o NPC "if the UnlockConditions match and **HomeLocation is valid**". Se a location não existe no save, o NPC **não entra no mundo**. Isso é o risco de save-compat nº1 (§16.3), não um detalhe.

---

## 10. Interação com `Data/Shops` e `Data/Characters`

### 10.1 O que muda: o **ponto de acesso**, não a loja

| Item | Decisão |
|---|---|
| `Data/Shops` key `LivingRoots_ElenaShop` (`elena-npc.md:73`) | **Mantida idêntica.** Zero campo novo, zero `Owners` novo. Uma loja, um ID. |
| `Owners: [{ Name: "LivingRoots_Elena", Portrait, Dialogues }]` | **Manter** — mas ver §10.3 |
| `PriceModifiers` + `Condition: "PLAYER_HEARTS LivingRoots_Elena 4"` | inalterado (`elena-npc.md:73`) |
| `AvailableStockLimit: Player` | inalterado — continua valendo com interior (Principle VI) |
| `OpenSound`/`PurchaseSound` | defaults vanilla já corretos (`elena-npc.md:73`) |
| Acesso via tile | `Action OpenShop LivingRoots_ElenaShop` **migra do `SeedShop` para a mesa de mudas da casa nova** |
| Acesso via diálogo | `Utility.TryOpenShopMenu` inalterado (`elena-npc.md:73`) |

`elena-npc.md:73` já oferece **duas** rotas (diálogo *ou* tile). Com interior próprio, o tile migra; o diálogo continua como segunda rota. Nada se perde.

### 10.2 A geometria da loja **não** força interior real

Achado que fecha uma das perguntas do briefing: `Modding:Shops` diz que uma shop pode ser aberta por `Action OpenShop` em **qualquer** mapa, e que `Owners` suporta `Name: "AnyOrNone"` — "to use this entry **regardless of whether any NPC is within the shop area**". Portanto:

- Uma loja **pode** viver num mapa da farm, ou num mapa custom, ou no `SeedShop`, sem geometria de loja nenhuma.
- O que faz a loja "parecer" uma loja é o `Owners` (retrato + diálogo + `ClosedMessage`), não o mapa.
- Consequência: **a Opção A não seria bloqueada pela loja** — ela é descartada por design e compatibilidade (§3), não por esta regra.

### 10.3 ⚠️ O ponto mais delicado: `Owners` exige a Elena **presente**

Isto é um achado que **não** está no briefing-pai e que muda o desenho:

> `Modding:Shops` §Owners — "When searching for an NPC, each entry checks either the **`[owner tile area]` from the `Action OpenShop` tile property** (if set), **else the whole location**." … "**If no entries are matched, the shop isn't opened.**"
> E `Action OpenShop` §`[owner tile area]` — "If omitted, the shop menu **will force open regardless of whether the shop owner is present and skip any Owners entries containing ClosedMessage**."

O pai define `Owners: [{ Name: "LivingRoots_Elena", … }]` (`elena-npc.md:73`). Numa casa onde ela **dorme**, isso cria dois problemas concretos:

1. **À noite ela está na cama.** Se o tile da mesa estiver no mesmo cômodo e ela for encontrada pelo pathing, a loja abre; se ela estiver fora de alcance/ausente, **a loja simplesmente não abre** — e o jogador não entende por quê.
2. **A Owned-tile-area dá o controle.** `Action OpenShop LivingRoots_ElenaShop down <open> <close> 4 6 3 2` restringe a checagem a uma região (mesa de mudas), que é onde ela fica de dia.

**Recomendação para a spec (não decidir aqui sem o pai):** manter `Name: "LivingRoots_Elena"` **e** adicionar `[owner tile area]` no `Action OpenShop`, cobrindo só a mesa. Preserva a intenção do pai (retrato e diálogo da Elena ao abrir) e elimina o caso "dormindo → loja fechada". Se a Elena **não** estiver na mesa, o jogador ainda tem a rota por diálogo (`TryOpenShopMenu`), que é quem abre a loja sem `Owners`.

> **Este é o único ponto onde este briefing recomenda mudar o briefing-pai.** Mudar `Owners` para `AnyOrNone` **não** é necessário e enfraqueceria a presença dela. Preservar `Name: "LivingRoots_Elena"` + `[owner tile area]` é a correção mínima.

### 10.4 `Data/Characters`

| Campo | Decisão |
|---|---|
| `Home` | `Id: "Default"`, `Location: "LivingRoots_ElenaHome"`, `Tile: { X, Y }` (canto-cama), `Direction: "down"` — formato idêntico ao do pai (`elena-npc.md:42-44`), só o `Location` muda |
| `HomeRegion = "Town"` | inalterado (`elena-npc.md:39`) |
| `CanBeRomanced = false` | inalterado (`elena-npc.md:17,40`) |
| `Home.Condition` | o campo `Condition` por entry existe (GSQ) — é o **seam de save-compat**: uma entry com `Condition` ligada a um mail flag + a entry `Default` apontando `SeedShop` dá o fallback (§16.3). **TBD: qual GSQ expresses "a location nova está ativa"** |

**Não duplicar spawn em `Data/Locations`** — não existe campo para isso (§4.2), o que é uma confirmação estrutural da regra "uma fonte de verdade".

---

## 11. i18n — chaves novas

Prefixo `elena.home.` (mesmo padrão do pai, `elena-npc.md:79-89`):

```json
{
  "elena.home.name": "Elena's House",
  "elena.home.welcome": "Elena's place — soil, seeds, and a stubborn activist.",
  "elena.home.shop.sign": "Heirloom seeds & recipes",
  "elena.home.bench.workbench": "A half-finished compost trial, waiting to be turned.",
  "elena.home.seedshelf": "Heirloom seeds, sorted by rotation.",
  "elena.home.cistern": "Rain, caught and waiting for a thirsty bed.",
  "elena.home.door.closed": "The door's shut. She's out in the field today."
}
```

`DisplayName` da location aceita `{{i18n:}}` (`Data/Locations` §Basic info). Adicionar a `en.json` + `default.json` — gate herdado do pai (`elena-npc.md:102`). PT-BR depois, chaves idênticas. `Action Dialogue` também aceita `{{i18n:}}` (tokenizable string).

---

## 12. Caminho de carga em C#

**Achado interno:** `grep` por `LoadMap|AsMap|RegisterTileAction|IGameContentHelper` em `LivingRoots/` retorna **só** `ModEntry.cs:31` (`helper.Events.Content.AssetRequested += OnAssetRequested`) e `ModEntry.cs:120` (o handler). **Mapas são greenfield neste codebase** — não há `AsMap`, não há `LoadMap`, não há `RegisterTileAction`. O único seam de conteúdo é `OnAssetRequested`, hoje só `Data/*`.

**Duas rotas, e o trade-off real:**

| Rota | Como | Veredito |
|---|---|---|
| **CP `Load` + `EditData` + `EditMap`** | `Load Maps/LivingRoots_ElenaHome FromFile assets/maps/elena_home.tmx`; `EditData Data/Locations`; `EditMap Maps/Town AddWarps` | A wiki diz **"Using Content Patcher to add custom locations is strongly recommended"**, e avisa que em C# "be aware of factors like object persistence and NPC pathfinding that are easy to get wrong" — precisaria do `LoadStageChanged` nos estágios `CreatedInitialLocations`/`SaveAddedLocations`. Mas o mod **não usa CP** |
| **C# no mod** | `helper.GameContent.LoadMap<Map>("assets/maps/elena_home.tmx")` + `OnAssetRequested` para `Data/Locations` e para `Maps/Town` (`e.AsMap()` → `AddWarps`) | Coerente com o default do mod (`elena-npc.md:29`, `ModEntry.cs:120`) e com a decisão transversal do agrupado ("default do mod é C# com bloco CP pronto") |

**Recomendação: rota C#**, coerente com o resto do mod, e **sem** `LoadStageChanged` — porque `CreateOnLoad` do `Data/Locations` faz o jogo criar a location sozinho. O `LoadMap` é necessário **apenas** para satisfazer o `MapPath`.

**Seam de camada (`LivingRoots/AGENTS.md`, `LivingRoots/Services/AGENTS.md`):**

- `ModEntry.cs` é o **sole composition root** — `new` só lá. O carregamento do mapa e os `e.Edit(...)` vivem **inteiros em `ModEntry.OnAssetRequested`**.
- **`Domain/` não toca asset nem asset helper.** Nenhum tipo novo em `Domain/`.
- `Services/` pode receber `IModHelper`/`IGameContentHelper` por DI, mas **não** deve fazer `LoadMap` no `Entry` — para este caso **nem é preciso**: `ModEntry` resolve e `Services/` fica fora. Se surgir lógica (ex.: uma flag "a casa já foi visitada"), ela vai para um serviço de `Services/` com interface em `Domain/`, conforme o padrão do mod.
- Exceção registrada: `Domain/Services/OrganicWasteValidator.cs` faz `using StardewModdingAPI` para `IMonitor` — legado, não precedente.

**Empacotamento:** `<Content Include="Assets\**\*.*">` (`LivingRoots.csproj:26`) já copia `**/*.*` — `.tmx` e `.png` sob `Assets/` entram no build **sem tocar no `.csproj`**. Caminho instalado: `Mods/LivingRoots/assets/maps/elena_home.tmx`.

---

## 13. Interação com Principle VI (`.specify/memory/constitution.md`)

- **VI — conteúdo de farm compartilhado.** A casa da Elena é conteúdo **de NPC**, não da farm, mas é um **interior compartilhado**: todo jogador no mesmo save entra. Se o interior hospedar entrega de item, state machine ou `ActionsOnPurchase`, tudo que for estado é **farm-keyed** (`SaveIdProvider`) — mesma regra de `elena-npc.md:96`. O mapa em si é **world-state**, como qualquer location do jogo: o jogo sincroniza location automaticamente.
- **VI — sem estado single-player-only.** Nada no interior pode estar fora do save do host. `CreateOnLoad.Type` vanilla-only já protege o save (§7).
- **Princípio herdado do pai que continua valendo:** `AvailableStockLimit: Player` na shop (`elena-npc.md:73`) — o interior não pode introduzir stock global.
- **YAGNI.** Um interior, um overlay de ≤32 tiles, uma entrada de `Data/Locations`, um warp. Nenhum mob, nenhum bioma, nenhum sistema, nenhum segundo cômodo.
- **TDD.** Todo campo de `Data/Locations` e toda property do `.tmx` é testável como texto (asset-pipeline), sem subir o jogo (§14).

---

## 14. Plano de testes

Ordem de gate do repo (`AGENTS.md`): `dotnet format` → build → test.

```bash
dotnet format Stardew-LivingRoots.sln --verify-no-changes
GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release
GamePath=$GP dotnet test --no-build --configuration Release   # baseline 816
```

**Testes novos (todos asset-pipeline, sem subir o jogo):**

| # | Teste | Assert |
|---|---|---|
| 1 | `Data/Locations` contém `LivingRoots_ElenaHome` | chave presente; `DisplayName == "{{i18n:elena.home.name}}"`; `CreateOnLoad.MapPath == "Maps/LivingRoots_ElenaHome"` |
| 2 | `CreateOnLoad.Type` é vanilla | `Type ∈ { "StardewValley.GameLocation", "StardewValley.Locations.DecoratableLocation" }` — **trava o crash-de-save** |
| 3 | `FormerLocationNames` presente e não colide | contém `LivingRoots_ElenaBackroom`; nenhum nome colide com outra key de `Data/Locations` |
| 4 | Schedule da Elena referencia a location | 5 chaves (4 estações + `rain`), todas parseáveis, `Location` == `LivingRoots_ElenaHome` — **estende** `elena-npc.md:102`, não substitui |
| 5 | `Home` bate com o schedule | `Data/Characters.Home[Default].Location` == mesmo nome usado no schedule (uma fonte de verdade) |
| 6 | Chaves i18n | `elena.home.*` presentes em `en.json` **e** `default.json` |
| 7 | **Gate de copyright** | nenhum arquivo sob `LivingRoots/Assets/` tem o nome de um tilesheet vanilla; nenhum `.xnb` no repo |
| 8 | Gate de `z_` | todo tilesheet do `.tmx` cujo source está no repo começa com `z_` |
| 9 | Gate de largura | largura do `.tmx` ≤ 200 tiles (`Modding:Maps`: >200 quebra o layering) |
| 10 | Porta | `Action Door` no tile de baixo da porta **e** map property `Doors` correspondente presentes |
| 11 | `Data/Shops` não foi duplicado | exatamente 1 entry `LivingRoots_ElenaShop`; `Name == "LivingRoots_Elena"` no `Owners` (não virou `AnyOrNone`) |
| 12 | `.csproj` não precisou mudar | `Assets/**/*.*` já cobre `.tmx`/`.png` |

**Armadilhas conhecidas (herdadas, `elena-npc.md:104` + `AGENTS.md`):** trailing newline em `.md` novo (gate `ArchitecturalPlanTests`); não tocar nos 3 `"(Planned)"` do `README.md` (gate `FeatureStatusTests` + embedded resource, `--no-build` lê cópia stale); NPC novo em save existente só segue schedule após 1 sono; `GamePath` é obrigatório para build.

---

## 15. Verificação em jogo

1. `debug warp LivingRoots_ElenaHome` — console command explicitamente sugerido pela wiki para testar location nova. A location carrega **sem tile em falta**; nenhum tile `-1`.
2. Dia 1: a Elena spawna **na cama** da casa (`Home`), não no `SeedShop`. Confirmar que o `Home` resolveu.
3. Andar até a mesa de mudas → `Action OpenShop LivingRoots_ElenaShop` abre a loja **com retrato e diálogo da Elena** e ≥3 itens (heirloom, receita mulch, receita cisterna — `elena-npc.md:73`).
4. **Teste do caso §10.3:** às 20h (quando ela deveria estar na cama / fora), clicar na mesa. Se a loja **não** abrir, o `[owner tile area]` não está cobrindo o tile certo — corrigir.
5. Sair pela porta: `Doors` anima, warp para `Town`, o jogador reaparece **no tile da porta** e não no meio da praça.
6. Entrar pela porta da Town: volta ao tile de arrival, sem queda no (0,0).
7. À noite: `AmbientNightLight` quente, NPC na cama. De dia: interior claro.
8. **Multiplayer (Principle VI):** host e farmhand entram; `AvailableStockLimit: Player` — cada um compra a própria unidade do item com stock limitado.
9. Console SMAPI: zero erros de `LoadMap`/map, zero "missing tilesheet", zero `mod reordered the original tilesheets`.
10. Salvar, recarregar, dormir → a Elena continua na casa (prova de que a location **persistiu**, não só carregou).

---

## 16. Riscos

1. **Schedule pode não aceitar location custom.** Se `Modding:Schedule data` exigir location vanilla, o interior fica órfão do schedule (a Elena vai e volta ao `SeedShop`) e a Opção B degrada para um `EditMap` no `SeedShop` — justamente o que ela existe para evitar. **Mitigação: gate §18 antes da spec.**
2. **Conflito com mods que já editam `Maps/SeedShop`.** Mesmo com um interior novo, o tile `Action OpenShop` **sai** do `SeedShop` — isso é um `EditMap` no `SeedShop`. Risco real, baixo (um tile só). **Mitigação:** manter o tile antigo e fazer o interior apenas *adicionar* acesso? Não — duplicaria a loja. Ou: aceitar o `EditMap` de 1 tile, que é o menor possível.
3. **Save-compat / `HomeLocation is valid`.** `Data/NPC data` §Spawn rules: se o `Home` aponta para uma location que não existe, `SpawnIfMissing` **não adiciona o NPC** — o NPC simplesmente não existe no save. Mitigações empilhadas: `FormerLocationNames` (§7), entry `Home` com `Condition` + entry `Default` de fallback (§10.4), e a spec é **Year-1-only** (nenhum save released tem a Elena), o que mantém o risco baixo se a feature entrar antes do lançamento.
4. **Spawn duplicado.** Se alguém "ajudar" colocando um `Characters` em `Data/Locations`… o campo **não existe** (§4.2), então o risco é o oposto: alguém gerar NPC via `Spawn` e a Elena aparecer em duplicata. Regra: uma fonte de verdade (`Home`).
5. **Porta mal configurada.** `Doors` **e** `Action Door` são **ambos** obrigatórios ("both are required"); `Action Door` vai no tile **de baixo** dos dois tiles da porta. Errar o `<tileID>` do `Doors` dá uma porta que não anima ou um tile em branco. `<sheetID>`/`<tileID>` exatos: **verificar no build com `GamePath`** — não inventar. Fallback honesto: **sem porta animada**, só `Warp` de map (mais burro, mais robusto).
6. **`Owners` sem Elena presente ⇒ loja fechada** (§10.3). Já tem mitigação desenhada.
7. **Camadas / draw order.** Um tile do `z_` na layer errada **esconde a Elena** — ela é desenhada entre `Back` e `AlwaysFront`. Autorar o overlay só em `Buildings`/`Front`.
8. **Largura > 200** quebra o layering (`Modding:Maps`). Interior pequeno por construção; teste nº 9 cobre.
9. **Pixels vanilla acidentalmente commitados** (§6) — o erro mais irreversível do lote, porque é *publicar* asset proprietary num repo público. Quatro barreiras: regra de dot- prefixo na autoria, `.gitignore` defensivo, teste nº 7, e a linha no README/LICENSE.
10. **Trailing newline** (§14) — qualquer edição neste arquivo sem `\n` final quebra a suíte.

---

## 17. Fora de escopo

- Conteúdo de diálogo novo — é do pai (`elena-npc.md:69`).
- Design da `Data/Shops` — é do pai (`elena-npc.md:71-73`). Aqui só se migra o ponto de acesso.
- Itens vendidos na loja — vêm dos outros briefings do arco Y1 (mulch, joja, heirloom, cisterna, kit).
- **Romance.** `CanBeRomanced = false` no Y1; revisitar pós-Year-3 (`elena-npc.md:17,40`). O interior é *compatível* com essa rota, **não a promete**.
- Fachada/exterior da Seed Shop na Town, e reinteriorizar o `SeedShop` existente.
- Um segundo cômodo, um andar, ou jardim/varanda.
- Quests e eventos (são do pai); `Data/Quests`, `Data/Events`.
- Capa de instruction manual, snapshot de save, cutscenes.

---

## 18. Gates antes de `/speckit.specify`

- [ ] **Abrir `Modding:Schedule data`** (wiki) e confirmar que o `Location` de um schedule resolve por **nome interno** de location custom. Fecha o gap #6 e o risco nº1. **Este é o primeiro gate** — se falhar, a Opção B muda de forma.
- [ ] **Verificar no build com `GamePath`:** `Maps/SeedShop` tem porta interna para um cômodo não-mapeado? (decide Opção A' — §3 contingência)
- [ ] **Verificar no build com `GamePath`:** par exato `<sheetID> <tileID>` para `Doors` na tilesheet de interiores; confirmar que `Maps/Town` tem tile de porta livre para o `Warp` de entrada.
- [ ] **Confirmar a key da tilesheet vanilla de interiores** usada no `.tmx` (o nome do asset em `Content/Maps`) — não chutar `spring_indoorsTileSheet`.
- [ ] Escolher o `Track` de `Music` consultando `Audio` com o jogo aberto.
- [ ] Confirmar `DefaultArrivalTile` e o tile exato da cama (coordenadas, não estimadas).
- [ ] **Levar a §10.3 ao briefing-pai:** `Owners` mantido + `[owner tile area]` no `Action OpenShop`. É a única mudança proposta fora deste arquivo.
- [ ] Decidir o `Home.Condition` de save-compat (§10.4): qual GSQ?
- [ ] Arte: decidir autor vs. placeholder tingido — mesma política de `adobe-shed-green-roof.md:84`. Orçamento: 1 folha, ~24–32 tiles.
- [ ] Paleta do interior derivada de `Constants.cs:48-53` (`Mulch`, `Sun`, `CompostReady`, `Stone`).
- [ ] **Reforçar a regra de copyright no `.gitignore` e no README** do mod, além do teste nº 7.
- [ ] `Constants.cs` (`ModConstants`): `ElenaHomeLocationId = "LivingRoots_ElenaHome"`, `ElenaHomeMapAssetName = "Maps/LivingRoots_ElenaHome"`. Sem número mágico no `.tmx` (regra de `AGENTS.md`).

---

## Fontes

**Internas:**
`ROADMAP.md:34` (Elena, mentor) · `ROADMAP.md:35-40` (cinco mecânicas Y1) · `elena-npc.md:17` (quarto placeholder / backroom futuro; `CanBeRomanced=false`; `HomeRegion=Town`; `FlowerDanceCanDance=false`) · `elena-npc.md:14` (1 conceito/semana) · `elena-npc.md:39-45` (`Data/Characters` + `Home = SeedShop (2,8) down` + TODO backroom) · `elena-npc.md:59-67` (schedule JSON, 5 chaves) · `elena-npc.md:69` (diálogo) · `elena-npc.md:71-73` (`Data/Shops` `LivingRoots_ElenaShop`, `Owners`, `Action OpenShop`, `PriceModifiers`, `AvailableStockLimit: Player`) · `elena-npc.md:77` (nenhum áudio custom Y1) · `elena-npc.md:79-89` (padrão i18n) · `elena-npc.md:96-104` (Principle VI, testes, armadilhas) · `adobe-shed-green-roof.md:20-22,81,84` (formato de inventário de sprite, spike de mapa, política de arte, gate de IDs) · `greywater-recycling.md:24,105-107` · `collective-kitchen-meals.md:99,103` · `weekly-fair.md:76` · `mobile-coop.md:20-21` · `csa-box.md:23` · `food-sovereignty-guardian-trophy.md:31,81` · `specs/004-playable-compost-loop/briefings/compost-icon.png.md:193-197` (proibição de vendorizar pixels) · `AGENTS.md` (GamePath, ordem de gates, armadilhas de teste) · `LivingRoots/AGENTS.md` · `LivingRoots/Services/AGENTS.md` · `LivingRoots/Constants.cs:31-35,48-53` (paleta, `TileSize`) · `LivingRoots/ModEntry.cs:31,120` (seam `AssetRequested`) · `LivingRoots/LivingRoots.csproj:26` (`<Content Include="Assets\**\*.*">`) · `LivingRoots/Domain/Services/OrganicWasteValidator.cs` (exceção legada de SMAPI em `Domain/`).

**Externas (lidas 2026-10-05; oldid + data de edição):**
`Modding:Location data` (oldid 186972, 14 jun 2025) — key = `Name`; campos completos; **sem `Characters`**; `CreateOnLoad{MapPath,AlwaysActive,Type}` com `Type` vanilla-only; `FormerLocationNames`; `Music*` e depreciação da map property `Music`; lista de locations vanilla (inclui **`Sunroom`** = "Caroline's sunroom inside Pierre's General Store"); FAQ "how do I get to a custom location" (`EditMap AddWarps`, `debug warp`); FAQ "can I make the location conditional" (não condicionar a existência, só o acesso).
`Modding:Maps` (página corrente) — camadas Back/Buildings/Paths/Front/AlwaysFront; §1.4 "Tilesets vs Tilesheets"; §2.3.1 `Load` de mapa vanilla "not recommended… low compatibility"; §2.3.2 "Using Content Patcher to add custom locations is **strongly recommended**" + aviso sobre C# e `LoadStageChanged`; §3.3 "**always start with a vanilla map and edit it**"; §3.2.1 "Re-using vanilla tilesets"; §3.2.3 tilesheet custom (nome descritivo, `Embed in map`); largura >200 quebra; §5.4 `Warp`/`NPCWarp`/`DefaultWarpLocation`; §5.5 `Music` **deprecada**; §5.6 `AmbientLight`/`AmbientNightLight`/`IgnoreLightingTiles`; §5.7 `Doors`/`DayTiles`/`NightTiles`; §5.10 `Outdoors`/`LocationContext`/`AllowBeds`/`CanCaskHere`/`ViewportClamp`/`PassOutSafe`; §6.1 `Bed T`, `NoPath T`, `Passable T`; §6.3 `Action Door`, `Action OpenShop <id> [from dir] [open] [close] [owner tile area]`, `Action Warp`, `Action Dialogue`; §8.1 tilesheet order e prefixo **`z_`**; **§8.2 "Local copy of a vanilla tilesheet"** (prova de copyright, §6); §10 `AssetRequested` + `e.AsMap()` + `map.AddTileSheet(..., "z_custom-tilesheet", ...)`.
`Modding:Shops` (oldid 192231, 13 abr 2026) — `Items`/`AvailableStock`/`AvailableStockLimit` (Global/Player/None); `Owners` **ordenada**, `Name` ∈ NPC interno / `AnyOrNone` / `Any` / `None`, "if no entries are matched, the shop isn't opened", checagem por `[owner tile area]` **ou a location inteira**; `ClosedMessage`; `OpenSound` default `dwop`, `PurchaseSound` default `purchaseClick`; `VisualTheme`; §Open a custom shop (`Action OpenShop`, `Utility.GetShopStock`, `Utility.TryOpenShopMenu`).
`Modding:NPC data` (oldid 193090, 17 mai 2026) — §Spawn rules `Home` (`Id`/`Location`/`Tile`/`Direction`/**`Condition`**), `UnlockConditions`, `SpawnIfMissing` ("if the UnlockConditions match and **HomeLocation is valid**"); §Limitations ("NPC added to an existing save… don't follow their schedule correctly until you've slept once"); `HomeRegion = Town` implying `IntroductionsQuest`/WinterStar/ItemDelivery; sprites 16×32, retratos 64×64; exemplo Dobson.

**Fontes citadas mas não lidas nesta sessão** (marcadas como TBD no texto): `Modding:Schedule data` (§9, §18 — gate nº1), `Modding:Dialogue data`, `Modding:Building data`, `Modding:Events`, `Modding:Tile properties` (seção própria — os campos usados vêm de `Modding:Maps` §6, que é a fonte primária atual), `Modding:Audio` (§18), `Modding:Content Patcher` / `Modder_Guide/APIs/Content`.

---

**Última verificação:** 2026-10-05 — estrutura e integração com `elena-npc.md` a partir de leitura direta; pesquisa externa concluída para `Data/Locations` (oldid 186972), `Modding:Maps` (§copyright fechada: §3.2.1 + §8.2), `Modding:Shops` (oldid 192231) e `Modding:NPC data` (oldid 193090); grep interno em `ModEntry.cs`, `Constants.cs`, `LivingRoots.csproj` (mapas são greenfield; `Assets/**` já copiado). **TBD restantes, todos listados em §18:** o schedule aceitar location custom (§9 — gate nº1), os pares `<sheetID> <tileID>` de `Doors` e o tile de porta em `Town` (§8, §16.5 — verificar no build com `GamePath`), o `Track` de `Music` e `DefaultArrivalTile` (§7, §9), e a autoria dos ~24–32 tiles de `z_elena_home.png` (§5).
