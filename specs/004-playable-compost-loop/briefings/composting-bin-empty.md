# Briefing: `composting_bin_empty.png` (Empty-state sprite)

**Feature:** `specs/004-playable-compost-loop` (playable compost loop, native machine, 2-day maturation)
**Asset:** `LivingRoots/Assets/composting_bin_empty.png` (16×32 RGBA, ~170 bytes)
**Status:** placeholder exists, **not wired** — no C# loads it, game never sees it
**Date:** 2026-10-05 | **Game:** Stardew 1.6 | **API:** SMAPI 4.0 | **TFM:** net6.0

---

## 1. Estado atual (verificado no repo)

| Fato | Evidência |
|------|-----------|
| Os 3 PNGs do bin + `compost.png` existem em `LivingRoots/Assets/` | `16×32` (bins), `16×16` (compost), RGBA 8-bit |
| Nenhum C# referencia os PNGs | grep por `Texture`, `Load<Texture`, `composting_bin`, `Texture2D` no mod só acha texturas brancas 1×1 de debug/visualização; zero `Content.Load` / `ModContent.Load` de assets |
| Os PNGs **são** copiados para o build output | `LivingRoots.csproj:26-28` — `<Content Include="Assets\**\*.*">` com `PreserveNewest` |
| `ModEntry.cs:125-150` registra o bin em `Data/Objects` com `Type = "Crafting"` | **errado para uma máquina placeable** (ver §2) |
| `ModEntry.cs:152-176` registra `(BC)LivingRoots.CompostingBin` em `Data/Machines` com **só** `OutputRules`, sem timing, sem triggers, sem sprites | resultado: a máquina aceita qualquer coisa e fica pronta na hora |
| `Constants.cs:65-66` define `CompostingBinItemId` / `MachineCompostingBinId` (= `LivingRoots.CompostingBin`) | IDs existem, wiring não |
| Receita em `ModEntry.cs:121` usa yield `LivingRoots.CompostingBin 1` com flag `false` | **errado**: bin é big-craftable, a flag deve ser `true` (ver §2) |
| Bin tem 3 estados lógicos | Empty=0, Processing=1, Ready=2 (todos os estados têm PNG 16×32 próprio) |

Visual atual dos placeholders (ampliados): `empty` = tábuas marrons verticais genéricas;
`processing`/`ready` = variações mínimas do mesmo desenho; `compost.png` (16×16) = borrão
escuro. Nenhum deles comunica "vazio vs. cheio vs. pronto" hoje — são marcadores de posição,
não arte final.

---

## 2. Achado crítico: o bin precisa ser `(BC)`, não `(O)`

A chave de `Data/Machines` já diz: `(BC)LivingRoots.CompostingBin`. Pela wiki oficial
([Modding:Machines](https://wiki.stardewvalley.net/Modding:Machines)), a chave de
`Data/Machines` é o **qualified item ID da máquina** — `(BC)` = big-craftable. Mas o item
está registrado em `Data/Objects` (tipo `(O)`), onde **nenhum item com esse ID existe** do
ponto de vista do resolvedor: `ItemRegistry` resolve IDs não-qualificados na ordem
object → big-craftable → … ([Modding:Items](https://wiki.stardewvalley.net/Modding:Items)),
e `(BC)LivingRoots.CompostingBin` exige uma entrada em **`Data/BigCraftables`**.

Consequências práticas:

- **Mover o registro do bin de `Data/Objects` para `Data/BigCraftables`** usando
  `StardewValley.GameData.BigCraftables.BigCraftableData`
  ([campos](https://github.com/Dannode36/StardewValleyDecompiled/blob/main/StardewValley.GameData/StardewValley.GameData.BigCraftables/BigCraftableData.cs)):
  `Name`, `DisplayName`, `Description`, `Price` (0), `Texture`, `SpriteIndex`.
  Big-craftables são "two tiles tall" por definição
  ([Modding:Big craftables](https://wiki.stardewvalley.net/Modding:Big_craftables)) —
  é exatamente o que um bin placeable precisa; `Data/Objects` com `Type = "Crafting"`
  é para itens 1-tile (categoria `-8`, `category_crafting`), não para máquinas de chão.
- **Receita:** o formato crafting é
  `ingredientes/tab/yield/isBigCraftable/unlock/displayName`
  ([Modding:Recipe data](https://wiki.stardewvalley.net/Modding:Recipe_data)).
  Exemplos vanilla: `"Worm Bin":"709 15 336 1 335 1 771 50/Home/154/true/s Fishing 4/"`,
  `"Chest":"388 50/Home/130/true/default/"`. O `true` na 4ª posição é obrigatório para
  produtos big-craftable; o yield usa o ID **não-qualificado** (`LivingRoots.CompostingBin`,
  sem `(BC)`, quantidade 1 pode ser omitida). A string atual (`.../LivingRoots.CompostingBin 1/false/...`)
  produz um `(O)`, que nunca vai casar com a chave `(BC)` da máquina.
- **Compost (output) fica onde está:** `(O)LivingRoots.Compost` em `Data/Objects` com
  `Type = "Basic"` está correto — é um item de inventário 1-tile, ícone 16×16
  (default `Maps/springobjects`, override via `Texture` + `SpriteIndex`; ver briefing do
  `compost.png`, fora do escopo aqui).

---

## 3. Spec técnica do sprite

### 3.1 Dimensões e formato

- **16×32 px, PNG RGBA 8-bit, fundo transparente.** Confirmado correto:
  PNGs extraídos do jogo para big-craftables são um-por-arquivo de **16×32**
  (pipeline comunitário documenta `bigcraftables/ — one PNG per craftable (16×32)`);
  sprites overworld de NPC também são 16×32; objetos de inventário são 16×16.
  O `composting_bin_empty.png` atual já está no tamanho certo — **não redimensionar**.
- Paleta contida: 3–4 tons por cor (padrão SDV para sprites; personagens usam 3–4 tons
  em roupa/pele — [Tutorial: How to make pixel art](https://stardewmodding.wiki.gg/wiki/Tutorial:_How_to_make_pixel_art)).
- **Não** embutir sombra de chão no PNG. Sombras seguem convenção própria
  (~40% preto, 1 px à esquerda / 2 px abaixo para furniture) e máquinas têm desenho
  próprio de sombra pelo jogo; sombra assada no sprite duplica/escurece.
- Manter os 3 estados no **mesmo grid e mesma caixa de colisão visual** (só o conteúdo/
  tampa muda) para o bin não "pular" na troca de estado.

### 3.2 Path e asset name

- Disco (mod folder): `Assets/composting_bin_empty.png` (já existe; **case-sensitive
  no Linux** — o `LoadFromModFile` deve grafar igual).
- Asset name no content pipeline (sem `Content/`, sem extensão, barras `/`):
  `Mods/lfgranja.LivingRoots/CompostingBin` (convenção `Mods/<UniqueID>/<Nome>`;
  `UniqueID` real = `lfgranja.LivingRoots` por `manifest.json`).
  Um asset por estado **ou** um spritesheet combinado (recomendação no §3.4).

### 3.3 Wiring SMAPI (sem Content Patcher)

O spike da 004 decidiu: registro 100% programático via `AssetRequested` em C#, sem
dependência de Content Patcher. O equivalente exato do `Load … FromFile` do CP
([author-guide/action-load](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide/action-load.md))
em C# SMAPI é `e.LoadFromModFile<T>` + `e.Edit(...)`
([Content API](https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Content)):

```csharp
// 1. Servir a textura (uma vez por asset; SMAPI chama o evento a cada load — wiring é declarativo)
if (e.NameWithoutLocale.IsEquivalentTo("Mods/lfgranja.LivingRoots/CompostingBin"))
{
    e.LoadFromModFile<Texture2D>("Assets/composting_bin.png", AssetLoadPriority.Medium);
}

// 2. Apontar o item para ela
if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, BigCraftableData>().Data;
        data[ModConstants.MachineCompostingBinId] = new BigCraftableData
        {
            Name = ModConstants.MachineCompostingBinId,
            DisplayName = "Composting Bin",
            Description = "Converts organic waste into rich compost over 2 days.",
            Price = 0,
            Texture = "Mods/lfgranja.LivingRoots/CompostingBin",
            SpriteIndex = 0
        };
    });
}
```

Notas de wiring:

- Usar `e.NameWithoutLocale` para `Data/*` (receitas/objetos/máquinas não são localizados;
  o `ModEntry` atual já faz isso — manter o padrão).
- `LoadFromModFile` é **deferred**: só declara como carregar; não chamar `ModContent.Load`
  dentro de código de draw (custo de GPU; carregar uma vez e reutilizar).
- `manifest.json` já declara `MinimumApiVersion: 4.0.0` — `AssetRequested` + genéricos
  `LoadFromModFile<Texture2D>` estão disponíveis.
- Não é preciso `InvalidateCache` para este asset estático (só para texturas que mudam
  em runtime, ex. troca de roupa de NPC).

### 3.4 Os 3 estados: spritesheet único + flags nativas (recomendado)

O jogo **não** tem campo "textura por estado" em `Data/Machines`. O mecanismo vanilla
para máquina que muda de aparência é deslocamento de índice no **mesmo** spritesheet:

- `ShowNextIndexWhileWorking` / `ShowNextIndexWhenReady`: mostra `SpriteIndex + 1`
  enquanto processa / quando pronto;
- `IncrementMachineParentSheetIndex`: soma um offset ao índice enquanto processa
  determinada regra (acumula com os flags acima).

Como cada estado já existe como PNG 16×32 avulso, a forma mais barata e mais vanilla é
**combinar os 3 em um spritesheet** (`Assets/composting_bin.png`):

- Layout: faixa vertical **16×96** (empty no topo, processing no meio, ready embaixo)
  **ou** horizontal **48×32**. Índice 0 = top-left, incrementa atravessando e depois
  descendo ([Modding:Items § Item sprites](https://wiki.stardewvalley.net/Modding:Items)).
  Com faixa vertical 16×96: índice 0 = empty, 1 = processing, 2 = ready. Com faixa
  horizontal 48×32: mesma ordem 0/1/2.
- `BigCraftableData.SpriteIndex = 0`, `Texture` = asset combinado.
- Regra candidata (a validar em jogo, ver §5): `ShowNextIndexWhileWorking = true` no
  `MachineData` + `IncrementMachineParentSheetIndex = 1` na regra de output, de modo que
  vazio = 0, trabalhando = 0+1, pronto = 0+1+1. **A semântica exata de empilhamento
  desses dois campos precisa de verificação empírica** — a wiki documenta que eles
  acumulam, mas a interação `WhileWorking` × `WhenReady` × regra ativa só se confirma
  com o bin rodando (§5 lista o teste). Alternativa descartada: 3 assets separados com
  desenho custom via Harmony — viola YAGNI e a FR-001a (máquina nativa, sem interface
  própria).

### 3.5 `Data/Machines`: o que falta além do sprite

O registro atual só tem `OutputRules → OutputItem`. Para o loop jogável (2 dias,
só resíduo orgânico), a entrada precisa no mínimo de:

- `OutputRules[0].Trigger = ItemPlacedInMachine` (comportamento padrão mais comum),
  `RequiredTags` = conjunto de tags de contexto herdado da spec 002 (FR-003 — **não**
  redefinir aqui, só referenciar),
- `DaysUntilReady = 2` (`ModConstants.MaturationDays`; se `MinutesUntilReady` e dias
  coexistirem, **dias vencem**),
- `InvalidItemMessage` (tokenizable string) para item não-aceito — toast nativo;
  rejeição com shake/cue de áudio é comportamento vanilla automático (FR-008a: sem
  artwork nova para recusas).

---

## 4. Direção de arte (guia para o `empty`)

Referências: [Sundrop Art Guide](https://web.archive.org/web/20220204164234/https://sundrop.kvdk.net/files/SundropArtGuide.pdf)
(via [External Tutorials](https://stardewmodding.wiki.gg/wiki/External_Tutorials_and_Guides)),
[Pixel Art Infographics](https://stardewmodding.wiki.gg/wiki/Pixel_Art_Infographics),
thread [Creating Pixel Art](https://pixeljoint.com/forum/forum_posts.asp?TID=11299&PID=139322)
(seção *Things to Avoid*).

- **Silhueta:** caixa de compostagem de madeira (slats horizontais como a Worm Bin
  vanilla — primo visual mais próximo; receita-irmã: 25 hardwood + barras + 50 fiber).
  Ocupar ~14 dos 16 px de largura; base assentada nos últimos 2 px.
- **Outline:** 1 px, **não preto puro** — usar o tom mais escuro do fill deslocado no matiz
  (marrom-arroxeado para madeira). Outline preto chapado é o erro nº 1 de iniciante
  (Pixeljoint *Things to Avoid*).
- **Luz:** fonte de cima-direita levemente frontal (padrão SDV para retratos e objetos):
  highlight no topo e na borda direita, sombra à esquerda/embaixo. Sombrear com
  **hue shifting**: sombras para o frio (marrom → roxo-acinzentado), highlights para o
  quente (marrom → laranja-amarelado) — evita lama.
- **Perspectiva:** frontal com leve vista de cima (vê-se a borda interna da abertura).
- **"Vazio" legível à distância (o requisito do estado):**
  - tampa **aberta/encostada** ou abertura frontal visível mostrando o **interior oco
    escuro** (marrom quase-preto, 1–2 tons);
  - madeira **dessaturada**, sem conteúdo, sem brilho, sem vapor, sem estrela;
  - contraste deliberado com os irmãos: `processing` = tampa fechada (+ opcional
    `WobbleWhileWorking`); `ready` = cheio até a borda com composto escuro fofo +
    1 estrela/sparkle vanilla de "pronto para colher".
- **O que NÃO fazer:** dithering pesado (SDV usa shading em bandas limpas), ruído
  aleatório, simetria perfeita espelhada, banda de highlight de 1 px em tudo
  (*banding*), interior claro (pareceria cheio).

---

## 5. Verificação in-game (checklist)

1. `GamePath=… dotnet build` limpo; `dotnet format --verify-no-changes` verde.
2. Rodar com SMAPI, console: `list_items LivingRoots` — devem aparecer
   `(BC)LivingRoots.CompostingBin` e `(O)LivingRoots.Compost`, sem Error Item 🛇.
3. Craftar (50 wood / 25 stone / 15 fiber, tab Home), posicionar na fazenda:
   sprite = empty (tampa aberta, interior escuro). Screenshot.
4. Largar resíduo válido → sprite muda para processing (sem reload de cena).
5. Avançar 2 dias → sprite muda para ready; coletar → volta para empty + 1 compost
   no inventário (FR-005a: exatamente um).
6. Save/reload em cada estado — sprite e progresso idênticos (FR-012).
7. SMAPI log: zero `[CP]`/vermelho de asset (`LoadFromModFile` com path errado aparece
   como erro de load; no Linux, conferir caixa de `Assets/...`).
8. Comparar lado a lado com a Worm Bin vanilla (mesma iluminação/estação) — o bin não
   deve destoar em saturação nem em escala.
9. **Spike explícito:** confirmar a combinação `ShowNextIndexWhileWorking` +
   `IncrementMachineParentSheetIndex` produz 0/1/2 nos três estados; se a wiki estiver
   imprecisa, registrar o comportamento real e ajustar este briefing.

---

## 6. Out-of-scope (explicitamente NÃO deste briefing)

- Arte final de `composting_bin_processing.png` e `composting_bin_ready.png`
  (briefings irmãos; aqui só o contrato de índices que eles ocupam).
- Arte/ícone de `compost.png` (item `(O)` 16×16, briefing próprio).
- Variantes sazonais do bin — máquinas não seasonalizam (seasonal switching existe
  para tilesheets de mapa via `fall_`/`spring_` prefixo, não para `Data/Machines`).
- `IsLamp`/luz noturna, sons custom, `TemporarySprites`/animação por frames,
  `WobbleWhileWorking` além do flag vanilla.
- Mudança no conjunto de resíduos aceitos (dono: spec 002, FR-003).
- Qualquer UI própria, texto desenhado ou artwork para mensagens de recusa
  (FR-008a: `Game1.showRedMessage` + `i18n`, shake/cue vanilla).
- Dependência de Content Patcher (spike 004 manda SMAPI puro; CP aparece aqui só
  como analogia `Load/FromFile`).
- Correção de `Data/Objects`→`Data/BigCraftables` em si (tarefa de código T013/T022;
  este briefing registra o requisito, não o implementa).

---

## Fontes

- Modding:Machines — https://wiki.stardewvalley.net/Modding:Machines
- Modding:Big craftables — https://wiki.stardewvalley.net/Modding:Big_craftables
- Modding:Items (tipos, sprites, `Texture`/`SpriteIndex`) — https://wiki.stardewvalley.net/Modding:Items
- Modding:Recipe data (flag big-craftable, yields) — https://wiki.stardewvalley.net/Modding:Recipe_data
- Content API (`AssetRequested`, `LoadFromModFile`, `AsDictionary`, `PatchImage`) — https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Content
- `BigCraftableData.cs` decompilado — https://github.com/Dannode36/StardewValleyDecompiled/blob/main/StardewValley.GameData/StardewValley.GameData.BigCraftables/BigCraftableData.cs
- Content Patcher `Load`/`FromFile` (analogia) — https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide/action-load.md
- Tutorial: How to make pixel art (shades, luz, hue shifting) — https://stardewmodding.wiki.gg/wiki/Tutorial:_How_to_make_pixel_art
- Pixel Art Infographics — https://stardewmodding.wiki.gg/wiki/Pixel_Art_Infographics
- Sundrop Art Guide (via Wayback) — https://web.archive.org/web/20220204164234/https://sundrop.kvdk.net/files/SundropArtGuide.pdf
