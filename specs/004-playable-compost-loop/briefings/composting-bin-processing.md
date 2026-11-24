# Briefing — `composting_bin_processing.png` (estado Processing da composteira)

**Feature:** `specs/004-playable-compost-loop` (Playable Compost Loop)
**Asset em foco:** `LivingRoots/Assets/composting_bin_processing.png`
**Estados irmãos:** `composting_bin_empty.png` · `composting_bin_ready.png` · `compost.png`
**Data:** 2026-10-05 · **Status:** briefing de pesquisa (sem código alterado)
**Spec dominante:** `spec.md` — máquina nativa (`FR-001a`), 2 dias de processamento
(`FR-001b`, `MaturationDays = 2`), um-dentro-um-fora (`FR-005a`), progresso
preservado em `Item.modData` ao recolher (`FR-005c`).

---

## 1. Estado atual (verificado no repo — não presumido)

### 1.1. Os PNGs existem, têm o tamanho certo, mas estão *unwired*

| Fato | Evidência |
|---|---|
| Os 4 PNGs existem em `LivingRoots/Assets/` | `ls LivingRoots/Assets/` — `compost.png`, `composting_bin_empty/processing/ready.png` |
| Cada binário de estado tem **16×32 px, RGBA** (tamanho nativo exato de um BigCraftable) | `PIL: (16, 32) mode=RGBA` para os três; `compost.png` é 16×16 (tamanho nativo de objeto pequeno) |
| O `.csproj` **copia** `Assets/**/*.*` para o output | `LivingRoots/LivingRoots.csproj:26-28` — `<Content Include="Assets\**\*.*">` |
| **Nenhum C# carrega os PNGs** — sem `ModContent.Load<Texture2D>`, sem `GameContent.Load`, sem `e.LoadFromModFile`, sem `AsImage().PatchImage` | `grep` por `Content\.Load\|LoadFromModFile\|PatchImage\|Texture2D.*Assets` em `LivingRoots/` retorna zero ocorrências fora do renderer de visualização de solo (que cria textura branca 1×1 procedural, `ModController.cs:788-792`) |
| `manifest.json` não declara dependência de Content Patcher nem de Alternative Textures | `LivingRoots/manifest.json` — só `Name/Author/Version/UniqueID/EntryDll/MinimumApiVersion` |

Conclusão: os arquivos chegam à pasta do mod compilado, mas **o jogo nunca os vê**.
É exatamente o "placeholder não wired" descrito nas notas do repo.

### 1.2. Anatomia pixel a pixel dos três estados (medição real, 2026-10-05)

Leitura via Pillow dos três PNGs (16 colunas × 32 linhas; `.` = transparente):

- **Base comum (linhas 08–30):** caixa de madeira com ripas — moldura externa escura
  `rgb(43,29,14)`, corpo `rgb(133,87,44)`, destaques `rgb(168,114,62)` / `rgb(92,58,30)`.
  Tampa abaulada nas linhas 08–12. Idêntica nos três arquivos (o artista reutilizou a base).
- **Empty (270 px opacos, 6 cores):** interior das ripas vazio/escuro, sem conteúdo visível.
- **Processing (273 px opacos, 12 cores — o mais rico):** montinho verde visível através
  das frestas (`rgb(85,139,47)` ×17 px + `rgb(124,179,66)` ×7 px — verde "vivo",
  coerente com `CompostProcessingColor #A16207`? **não** — ver §4.3) nas linhas 14–18 e
  23–26, **mais 3 pontos de vapor** acima da tampa (linhas 04–06, coluna ~8, cor clara
  `*`). É o único dos três com *algo acima da silhueta* — boa intuição de "calor/vapor",
  mas em **frame único estático**.
- **Ready (274 px opacos, 10 cores):** conteúdo interno mais escuro e "curtido"
  (`rgb(78,52,30)` / `rgb(55,36,20)`), **mais 4 px dourados de faísca**
  (`rgb(255,214,0)` ×4 + `rgb(255,235,59)` ×2) nas linhas 04–06 — convenção vanilla de
  "pronto para colher" (o jogo usa `showNextIndexWhenReady` + partículas, não brilho
  pintado; ver §3.3).
- **Margens:** linha 31 e linhas 00–03 (exceto vapor/faísca em 04–06) totalmente
  transparentes — correto, evita sangramento no tile vizinho quando desenhado a 4×
  (64×128 px na tela).

Limitações reais do asset atual:

1. **Frame único por estado.** Não há faixa de animação (spritesheet horizontal);
   cada PNG é 16×32 isolado. Qualquer `WorkingEffects.Frames` com mais de um offset
   exigiria fundi-los numa folha única (§5.1).
2. **Vapor de 3 px não anima.** O motor só anima vapor se receber `Frames` (troca de
   sprite) ou `TemporarySprites` (partículas sobrepostas) — um PNG estático com
   pontinhos é lido pelo jogador como "sujeira no sprite", não como movimento.
3. **Verde do heap destoa da paleta declarada.** `Constants.cs:49` fixa
   `CompostProcessingColor #A16207` (ocre/marrom) para overlays; o heap verde
   `rgb(85,139,47)` pertence à família do `CompostReadyColor #166534`. Não é erro
   funcional (sprites ≠ overlays), mas vale alinhar na direção de arte (§4.3).

### 1.3. Registro da máquina hoje (`ModEntry.cs:151-175`) — esqueleto mínimo

O handler `OnAssetRequested` registra em `Data/Machines` a chave
`(BC)LivingRoots.CompostingBin` com **apenas** isto:

```csharp
OutputRules = [ { Id = "Default",
                  OutputItem = [ { ItemId = "(O)LivingRoots.Compost" } ] } ]
```

Faltando (todos opcionais no schema, mas obrigatórios para o comportamento da spec):

- `Triggers` — sem gatilho `ItemPlacedInMachine`, a regra nunca dispara por input;
  sem `RequiredTags`/`RequiredItemId`, qualquer item (ou nenhum) seria aceito.
- `DaysUntilReady = 2` / `MinutesUntilReady` — sem tempo, o output sai **instantâneo**.
- `ShowNextIndexWhileWorking` / `ShowNextIndexWhenReady` /
  `IncrementMachineParentSheetIndex` — sem eles, o sprite nunca troca de estado.
- `WorkingEffects` / `LoadEffects` / `WorkingEffectChance` — sem eles, sem vapor,
  som ou tremor durante o processamento.
- `InvalidItemMessage` — sem ela, item inválido cai no comportamento genérico.
- `AllowFairyDust`, `ReadyTimeModifiers` — defaults (`true`/none) aceitáveis, mas
  deveriam ser decisão explícita (fairy dust encurtando 2 dias quebra `FR-001b`?).

### 1.4. Achado principal da pesquisa: o bin está registrado no asset errado

`ModEntry.cs:126-150` registra a composteira em **`Data/Objects`**
(`Type = "Crafting"`), mas a regra de máquina usa a chave **`(BC)…`** — prefixo de
**BigCraftable**. Pela referência oficial (`Modding:Big craftables`, fetch
verificado em 2026-10-05):

- Máquinas **só** podem ser BigCraftables (`(BC)`, dados em `Data/BigCraftables`,
  sprites 16×32, código em `StardewValley.Object` com `bigCraftable = true`).
- `Data/Objects` é para itens `(O)` de 16×16 (o `compost.png` está correto lá;
  a composteira, não).
- `Data/Machines` resolve pela chave `(BC)<id>` **idêntica** à de
  `Data/BigCraftables` (tutorial oficial: *"The key of Data/Machines must be
  identical to Data/BigCraftables, but with the (BC) identifier added"*).

Hoje **não existe entrada em `Data/BigCraftables`**, logo a chave `(BC)` em
`Data/Machines` aponta para um item fantasma, e a entrada em `Data/Objects`
nunca será consultada como máquina. Este é o defeito bloqueante da US1
(a spec já prevê o spike de registro; ver Assumptions em `spec.md:294-296`).
O wiring do `processing.png` passa necessariamente por criar a entrada
`Data/BigCraftables` com `Texture` + `SpriteIndex` (§6.1).

### 1.5. O que o serviço C# já sabe fazer (base pronta para o wiring)

- `CompostingBinStateModel.PopulateFromModData / WriteToModData`
  (`Domain/Models/CompostingBinStateModel.cs:40-100`) — serializa
  `InputItemId` + `DaysRemaining` de/para `IDictionary<string,string>`, com
  `Remove` das chaves quando vazio. É o formato exigido por `FR-005c`.
- `CompostingBinService` lê/escreve `item.modData[LivingRoots.InputId|DaysRemaining]`
  (`Services/CompostingBinService.cs:190-231`) e impõe stack máximo 1 com progresso.
- `ModController.cs:1198-1199` documenta dislodge com preservação + debris flutuante
  (`Game1.createItemDebris`) se inventário cheio.
- `Constants.cs:59,77-78` — `MaturationDays = 2`, chaves
  `LivingRoots.InputId` / `LivingRoots.DaysRemaining`.
- `i18n/default.json` — chaves de recusa já registradas
  (`message.compost.*`, `message.harvest.inventory-full`).

Ou seja: **persistência e regras de negócio existem; falta só a camada
conteúdo→motor** (textura + `Data/BigCraftables` + `Data/Machines` completo).

---

## 2. Como `Data/Machines` representa o estado intermediário (referência 1.6)

Fonte primária: `stardewvalleywiki.com/Modding:Machines` (fetch integral em
2026-10-05; última edição wiki 2026-02-27). Campos que importam para Processing:

### 2.1. Tempo — o que faz "estar processando" existir

| Campo | Efeito |
|---|---|
| `DaysUntilReady` | Dias de jogo até o output poder ser coletado. **Se `Days` e `Minutes` coexistirem, dias vencem.** |
| `MinutesUntilReady` | Minutos de jogo (10h–2h contam 60 min/h; 2h–6h contam 100 min/h). |
| (nenhum) | Output **instantâneo** — não há estado Processing. É o caso atual do mod. |
| `OnlyCompleteOvernight` | Se terminar de dia, pausa até o próximo day-update. Útil para composteira "lenta" que só amanhece pronta. |
| `PreventTimePass` | Pausa o timer em condições (`Outside`, `Inside`, estações, `Sun`/`Rain`, `Always`). Compostagem real pausa no frio? Decisão de design — default (ausente) = conta sempre. |
| `AllowLoadWhenFull` | Default `false`: não aceita novo input com batch em curso (correto para "no máximo um batch", `spec.md:257`). |

Para a spec: **`"DaysUntilReady": 2`** na regra, sem `MinutesUntilReady`.
`FR-001b` proíbe o mod de acelerar/encurtar — então `AllowFairyDust` merece
`"AllowFairyDust": false` explícito (fairy dust é mecânica vanilla que encurta
qualquer máquina com default `true`), e nenhum `ReadyTimeModifier` redutor.
`RecalculateOnCollect` (estilo bee-house) deve ficar ausente/`false`: o output é
determinístico (1 composto), não re-sorteado na colheita (`FR-005a`).

### 2.2. Gatilhos — como um batch começa

`OutputRules[].Triggers[]` (qualquer trigger casando seleciona a regra; primeira
regra disponível de cima para baixo vence — ordem da lista importa):

| Trigger | Uso |
|---|---|
| `ItemPlacedInMachine` | **O nosso caso.** Jogador larga resíduo → batch começa. Combina com `RequiredTags` (conjunto herdado da spec 002, `FR-003` — este briefing **não** lista itens) e/ou `RequiredItemId`, `RequiredCount: 1`, `Condition` (game-state query). |
| `MachinePutDown` | Ao colocar a máquina (worm bin usa para auto-iniciar). Candidato a *fallback* se o spike concluir que input manual + modData não coexistem (§7). |
| `DayUpdate` | Todo amanhecer, se ociosa (soda machine, worm bin deluxe). Alternativa "passiva", mas remove o gesto de alimentar a composteira — contraria US2. |
| `OutputCollected` | Ao colher output anterior (crystalarium recarrega). Não se aplica (output é terminal). |

Regra-alvo (forma Content Patcher; equivalente C# em §6.2):

```json
{
  "Id": "LivingRoots_CompostBatch",
  "Triggers": [{
    "Id": "LivingRoots_OrganicInput",
    "Trigger": "ItemPlacedInMachine",
    "RequiredTags": [ "<tags herdados da spec 002 — NÃO restatar aqui, FR-003>" ],
    "RequiredCount": 1
  }],
  "OutputItem": [{
    "Id": "LivingRoots_CompostOutput",
    "ItemId": "(O)LivingRoots.Compost",
    "MinStack": 1,
    "MaxStack": 1,
    "IncrementMachineParentSheetIndex": 1
  }],
  "DaysUntilReady": 2
}
```

### 2.3. Troca de sprite por estado — as três alavancas declarativas

O motor oferece **três mecanismos combináveis**, todos no `Data/Machines`
(sem uma linha de `draw()` custom — preferência da spec: `FR-001a`, YAGNI):

1. **`ShowNextIndexWhileWorking` / `ShowNextIndexWhenReady`** (bool, top-level).
   Exibe `SpriteIndex + 1` enquanto processa / quando pronto.
2. **`IncrementMachineParentSheetIndex`** (int, por `OutputItem`).
   Soma N ao índice **durante o processamento E quando pronto** daquela regra.
3. **`WorkingEffects` / `LoadEffects`** (lista de `{ Frames, Interval, Sounds,
   ShakeDuration, TemporarySprites }`). `Frames` = offsets **relativos ao sprite
   base** ciclados a cada `Interval` ms; `WorkingEffectChance` (default **0.33**)
   = probabilidade de (re)disparar a cada tick do relógio/amanhecer — fixar em
   `1` para animação contínua.

**Receita canônica de 3 sprites distintos** (documentada no tutorial oficial
`wiki.gg — Adding New Machines`, § *Show different sprites for idle, processing
and ready*, fetch verificado):

> Folha com sprite 0 = idle, 1 = working, 2 = ready →
> `IncrementMachineParentSheetIndex: 1` no output (0→1 ao processar **e** ao
> aprontar) **+** `ShowNextIndexWhenReady: true` (1→2 só quando pronto).

Mapeamento direto para os nossos PNGs: folha `00 empty | 01 processing |
02 ready`, `SpriteIndex: 0`, regra com incremento 1, máquina com
`ShowNextIndexWhenReady: true`. **É exatamente o layout a adotar** (§5.1).

### 2.4. Fumaça/vapor, tremor, som ambiente e luz

| Recurso | Campo | Notas para a composteira |
|---|---|---|
| Tremor ao carregar | `LoadEffects[].ShakeDuration` (ms) + `Sounds[]` | Feedback de "aceitou o input". Som: cue vanilla curta (ex. `woodchipper`, `compost`? — validar IDs em `Data/Audio`; **não** referenciar os `.ogg` de `Assets/Audio/` até o pipeline de áudio existir — nota do repo: áudio placeholder não wired). |
| Tremor/animação contínua | `WorkingEffects[]` com `Frames` + `Interval` + `WorkingEffectChance: 1` | Ciclar frames de vapor (ex. `[1,2,3,2]`, `Interval: 500` — vapor lento; 80–100 ms é para moinho/pilha rápida, agitadod+ demais para compostagem). |
| Partículas de vapor | `WorkingEffects[].TemporarySprites[]` (`Texture`, `SourceRect`, `PositionOffset`, `Interval`, `Loops`, `Scale`) | Alternativa/complemento a frames: reutiliza textura vanilla de fumaça (ex. sprites de `TileSheets/Projectiles` ou fumaça do `Fish Smoker`) sem pintar pixel novo. **Preferível se a arte de vapor própria não convencer** — zero asset novo. |
| Som ambiente | `WorkingEffects[].Sounds[]` com `Delay` | Cuidado: `WorkingEffects` com som + `Chance 1` toca **a cada tick** — irritante. Padrão vanilla: som só em `LoadEffects`. Manter processamento silencioso ou com `WorkingEffectChance` baixo. |
| Luz | `LightWhileWorking: { Radius, Color }` | Compostagem não brilha; **omitir** (luz quente sugeriria fornalha). |
| "Respiro" do sprite | `WobbleWhileWorking` (default: pulsar ligado) | O default já pulsa; setar `false` se o teste em jogo mostrar que o pulsar + vapor polui. Decisão pós-playtest, não antecipar. |
| Estrelinhas de pronto | Nativas | Colheita pronta já emite partículas/som vanilla ao interagir; o dourado pintado no `ready.png` é **reforço**, não substituto — não pintar demais para não brigar com as partículas do motor. |

### 2.5. Mensagens ao jogador (sem UI própria — `FR-008a`)

- `InvalidItemMessage` (+ `InvalidItemMessageCondition`): toast vanilla quando o
  item não casa nenhum trigger. É o canal correto para "isso não compostável"
  (**sem** `Game1.showRedMessage` — a spec reserva HUD vermelha para aplicação
  em tile, inventário cheio e colisão multiplayer; rejeição de input segue o
  comportamento vanilla: cue de cancel + shake).
- `InvalidCountMessage` (top-level e/ou por regra): toast quando falta
  quantidade (com token `[ItemCount]`).
- Colheita com inventário cheio: o motor **mantém o output na máquina** por
  padrão; o mod adiciona a `showRedMessage` localizada (`FR-005b`) no handler —
  comportamento já previsto em `T024`.

### 2.6. Lógica avançada (quando o declarativo não basta)

- `OutputMethod: "<Tipo>: <Método>"` (delegate `MachineOutputDelegate`) — decide o
  output em C#; pode sobrescrever o tempo via `overrideMinutesUntilReady`.
  **Só** se o spike provar que `DaysUntilReady: 2` + modData não coexistem.
- `InteractMethod` (delegate `MachineInteractDelegate`) — intercepta interação sem
  output pronto. Candidato para re-hidratar timer a partir de modData ao colocar
  (§7).
- Campos de leitura C#: `Object.lastOutputRuleId`, `Object.lastInputItem`,
  `IsMachineWorking()`, `PlaceInMachine()`, `OutputMachine()`,
  `GetMachineData()`, classe `MachineDataUtility`. São os pontos de inspeção para
  testes e para o fallback SMAPI (`ModController.OnDayStarted/OnButtonPressed`).

---

## 3. Sprites multi-frame para Processing (o que pintar e por quê)

### 3.1. Restrições rígidas do formato BigCraftable

- **Célula nativa 16×32 px** (`wiki.gg`, § *Load its sprite*: BC = 1×2 tiles de
  16 px). Desenhado a **4×** (64×128 px na tela). Qualquer pixel torto aparece
  quadruplicado — pintar com zoom 800% e revisar a 400%.
- **Índices sequenciais horizontais** na mesma textura: sprite N ocupa
  `(N*16, 0, 16, 32)`. Largura total = `16 × nº de frames`.
- **Fundo 100% transparente** fora da silhueta (os PNGs atuais já respeitam).
- Uma célula nunca "transborda": vapor/faísca devem caber nos 16×32 da própria
  célula (linhas 00–07 livres acima da tampa nos PNGs atuais — espaço suficiente).

### 3.2. Layout de spritesheet recomendado

**Folha mínima (3 células, 48×32 px)** — cobre o trio idle/processing/ready com a
receita do §2.3:

```
| 0: empty (atual) | 1: processing (atual) | 2: ready (atual) |
```

**Folha animada (6 células, 96×32 px)** — adiciona 3 frames de vapor ao
Processing via `WorkingEffects.Frames: [1, 3, 4, 5]` (offsets relativos à base 0;
frames 3–5 = mesma caixa com vapor em posições alternadas):

```
| 0 empty | 1 proc-A (vapor ⬆ esq) | 2 ready | 3 proc-B (vapor ⬆ centro) | 4 proc-C (vapor ⬆ dir) | 5 proc-A' (vapor baixo/dissipado) |
```

Por que 3–4 frames e não 2 ou 8:

- 2 frames = pisca-pisca binário (parece bug, não vapor).
- 3–4 frames a 400–600 ms = deriva lenta convincente de fumaça de compostagem
  (processo lento → animação lenta; coerência semântica).
- 8+ frames = custo de arte sem ganho perceptível a 4× + folha larga demais para
  `PatchImage` parcial eficiente.
- Manter `ready` como **célula única** (índice 2): pronto é estado de espera, não
  de movimento; o motor já adiciona partículas.

Alternativa sem arte nova: manter a folha de 3 células e obter o vapor via
`TemporarySprites` com textura vanilla (§2.4). **Recomendação: implementar
primeiro a folha de 3 + TemporarySprites vanilla; pintar frames 3–5 só se o
playtest julgar o vapor vanilla deslocado.** (YAGNI; a spec proíbe arte
desnecessária para refusos em `FR-008a` — o espírito vale para efeitos.)

### 3.3. Exemplos de outros mods e do vanilla (trabalho relacionado)

- **Compost Bin (`nexusmods.com/stardewvalley/mods/48318`)** — mod existente que
  adiciona máquina craftável de compostagem (fibra, algas, lixo, vegetais, frutas
  → fertilizante). Detalhes da página não puderam ser extraídos (403 no fetch
  direto em 2026-10-05); fica como referência de design de inputs, não de sprite.
  Citar como prior-art na revisão, não copiar assets (licença de terceiros).
- **Worm Bin vanilla** — o análogo mais próximo no jogo base: produz isca
  regularmente via triggers `MachinePutDown` + `DayUpdate` (sem input por item).
  Nossa composteira difere por exigir input (`ItemPlacedInMachine`) + 2 dias.
- **Furnace / Keg / Preserve Jar / Crystalarium / Cask** — cobrem o vocabulário
  completo: `AdditionalConsumedItems` (fornalha: carvão), `AllowLoadWhenFull`
  (crystalarium: recarrega por cima), `OutputMethod` (cask: envelhecimento),
  `CopyColor/CopyPrice/CopyQuality` + `PreserveId: DROP_IN` (artesanais
  aromatizados — **fora de escopo** aqui: output é item fixo, §8).
- **Fish Smoker (1.6)** — referência de animação "fumaça sobre a máquina"
  persistente; inspecionar seus `WorkingEffects`/`TemporarySprites` nos dados
  desempacotados como modelo de vapor.
- **Automate (Pathoschild)** — puxa de baús adjacentes para máquinas; nossa
  máquina deve funcionar com ele sem código extra **se** as regras forem vanilla
  puras (mais um motivo para preferir declarativo). Nada a implementar, apenas
  não quebrar (evitar `InteractMethod` que assuma `player` humano).

---

## 4. Direção de arte do frame Processing (conteúdo parcial visível, calor/vapor)

### 4.1. Princípio: três estados distinguíveis sem ler número nenhum (`SC-002`)

Jogador a 2–3 tiles de distância, com iluminação noturna/chuva, deve responder:

- *Vazia?* → ripas escuras, interior oco (empty atual: OK).
- *Trabalhando?* → **conteúdo visível + movimento acima** (processing atual:
  conteúdo OK, movimento ausente — é o gap).
- *Pronta?* → **marcador luminoso + silhueta estável** (ready atual: faísca OK;
  segurar 4 px dourados, não aumentar).

Nunca diferenciar estados **só por matiz** (acessibilidade: daltonismo; noite
dessatura tudo). O trio atual já acerta nisso: vazio = ausência, processando =
textura orgânica + vapor, pronto = pontos luminosos. Manter a gramática.

### 4.2. Conteúdo parcial visível (o heap)

- O heap verde através das frestas (linhas 14–18, 23–26) comunica "há matéria
  em decomposição" sem abrir a tampa (abrir a tampa exigiria célula maior ou
  redesenho da base — custo sem benefício).
- **Alinhar o verde à paleta do mod:** migrar o heap de `rgb(85,139,47)` para a
  família ocre `CompostProcessingColor #A16207 rgb(161,98,7)` com destaques
  `rgb(194,150,80)` (já presente ×5 no PNG atual — o artista deixou a ponte).
  Verde-vivo sugere "planta saudável", leitura oposta a "matéria em
  decomposição"; ocre/marrom = terra/húmus, coerente com o tema Living Roots.
  Reservar verde `CompostReadyColor #166534` para o estado Ready (composto
  "vivo", microrganismos — casa com a descrição *"rich in microbial life"*).
- Preenchimento parcial (~30–40% das frestas) está correto: batch único
  (`FR-005a`, um-dentro-um-fora) não enche a caixa; caixa cheia sugeriria
  múltiplos inputs e confundiria a regra de 1 batch.

### 4.3. Calor/vapor (o movimento)

- Vapor branco-acinzentado semi-opaco, 2–4 px por frame, **sempre dentro das
  linhas 00–07** (acima da tampa, abaixo do teto da célula).
- 3 posições alternadas (esq/centro/dir, deslocamento de 1–2 px entre frames) +
  1 frame de dissipação (vapor mais baixo e claro) = ciclo `[1,3,4,5]`.
- **Não** usar amarelo/laranja no vapor: cor quente + caixa de madeira = leitura
  de "pegando fogo" (fornalha). Calor de compostagem é vapor frio/claro.
- `LightWhileWorking`: omitir (§2.4). Se o playtest pedir "vida" noturna, preferir
  `TemporarySprites` de brilho suave a luz real (luz altera gameplay — atrai
  atenção como poste/lampião).

### 4.4. Checklist de QA de pixel-art (antes de commitar qualquer PNG)

1. Dimensões exatas (3-células: 48×32; 6-células: 96×32), RGBA, fundo limpo.
2. Base da caixa **bit-idêntica** entre células (diff binário das linhas 08–30
   deve dar zero fora das áreas de conteúdo/vapor/faísca — garante que
   `ShowNextIndex*` não "pula" a caixa).
3. Revisão a 100% (tamanho real de arquivo), 400% (tamanho em jogo) e com filtro
   de daltonismo (deuteranopia/protanopia) — estados ainda distinguíveis?
4. Teste noturno/chuva/inverno no jogo: vapor visível sem virar ruído branco?
5. Arquivo final termina com newline? (Não se aplica a PNG — mas o teste
   `ArchitecturalPlanTests` exige `\n` final em `docs/architectural_and_refactor_plans/**`;
   este briefing **não** vive lá, então sem restrição.)

---

## 5. Wiring de código (SMAPI C#, sem Content Patcher)

Tudo abaixo é `AssetRequested` (padrão `Modding:Modder Guide/APIs/Content`,
fetch verificado) — o mod já usa `e.Edit` para `Data/CraftingRecipes`,
`Data/Objects` e `Data/Machines`; acrescenta-se `Data/BigCraftables` + textura
custom + regra completa. **Nenhuma dependência nova** (a spec só admite
Content Patcher como fallback do spike; o caminho primário é C# puro).

### 5.1. Passo 0 — fundir os PNGs numa folha e expô-la como asset

Criar `LivingRoots/Assets/BigCraftables.png` (48×32 inicial; 96×32 quando os
frames extras existirem) com as células do §3.2, e registrar:

```csharp
if (e.NameWithoutLocale.IsEquivalentTo("Mods/lfgranja.LivingRoots/BigCraftables"))
{
    e.LoadFromModFile<Texture2D>("Assets/BigCraftables.png", AssetLoadPriority.Medium);
}
```

Notas:

- Nome do asset com prefixo `Mods/<UniqueID>/` = convenção anti-colisão (tutorial
  oficial usa `Mods/{{ModId}}/BigCraftables`).
- `LoadFromModFile` lê do **diretório do mod**, não do `Content` do jogo —
  é assim que PNGscopiados pelo `.csproj` entram no pipeline.
- Guardar `helper.ModContent.GetActualAssetKey(...)` se algum código C# precisar
  referenciar a textura fora do `Texture` de `Data/BigCraftables` (não deve
  precisar — o motor resolve `Texture` sozinho).
- Nomes de arquivo: manter `composting_bin_{empty,processing,ready}.png`
  originais como **fonte** e gerar `BigCraftables.png` a partir deles (script ou
  camada do editor); nunca editar a folha final à mão sem atualizar as fontes.

### 5.2. Passo 1 — registrar o BigCraftable (o que falta hoje)

```csharp
else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, StardewValley.GameData.BigCraftables.BigCraftableData>().Data;
        data[ModConstants.MachineCompostingBinId] = new StardewValley.GameData.BigCraftables.BigCraftableData
        {
            Name = "Composting Bin",
            DisplayName = "Composting Bin",          // + i18n via tokenizable string (seguir padrão de Data/Objects atual)
            Description = "Converts organic waste into rich compost over 2 days.",
            Texture = "Mods/lfgranja.LivingRoots/BigCraftables",
            SpriteIndex = 0,
            Price = 0,
            Fragility = 0,                           // recolhível com qualquer ferramenta (FR-005c exige pickup)
            CanBePlacedIndoors = true,
            CanBePlacedOutdoors = true,
        };
    });
}
```

E **remover/migrar** a entrada atual em `Data/Objects` para a composteira
(manter só o composto `(O)LivingRoots.Compost` lá). A receita em
`Data/CraftingRecipes` deve produzir o ID do BigCraftable, não um `(O)`.

### 5.3. Passo 2 — regra de máquina completa (substitui o esqueleto atual)

```csharp
else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, StardewValley.GameData.Machines.MachineData>().Data;
        var binId = $"(BC){ModConstants.MachineCompostingBinId}";
        data[binId] = new StardewValley.GameData.Machines.MachineData
        {
            OutputRules = new List<MachineOutputRule>
            {
                new MachineOutputRule
                {
                    Id = "LivingRoots_CompostBatch",
                    Triggers = new List<MachineOutputTrigger>
                    {
                        new MachineOutputTrigger
                        {
                            Id = "LivingRoots_OrganicInput",
                            Trigger = MachineOutputTriggerRule.ItemPlacedInMachine,
                            RequiredTags = new List<string> { /* herdado da spec 002 (FR-003) */ },
                            RequiredCount = 1,
                        },
                    },
                    OutputItem = new List<MachineItemOutput>
                    {
                        new MachineItemOutput
                        {
                            ItemId = ModConstants.QualifiedCompostItemId, // "(O)LivingRoots.Compost"
                            MinStack = 1,
                            MaxStack = 1,
                            IncrementMachineParentSheetIndex = 1,          // 0→1 ao processar e ao aprontar
                        },
                    },
                    DaysUntilReady = ModConstants.MaturationDays,          // 2 (FR-001b)
                },
            },
            ShowNextIndexWhenReady = true,                                 // 1→2 só quando pronto
            WorkingEffectChance = 1.0f,                                   // vapor contínuo (default 0.33 = intermitente)
            WorkingEffects = new List<MachineEffects>
            {
                new MachineEffects
                {
                    Frames = new List<int> { 1, 3, 4, 5 },                 // offsets relativos (folha de 6)
                    Interval = 500,                                       // vapor lento; validar em jogo
                    // Sounds: AUSENTE de propósito — som só no LoadEffects (§2.4)
                },
            },
            LoadEffects = new List<MachineEffects>
            {
                new MachineEffects
                {
                    Sounds = new List<MachineSound> { new MachineSound { Id = "<cue vanilla curta>" } },
                    ShakeDuration = 300,
                },
            },
            AllowFairyDust = false,                                        // fairy dust quebraria os 2 dias fixos
            InvalidItemMessage = "…",                                     // tokenizable/i18n; toast vanilla
            WobbleWhileWorking = false,                                   // decidir no playtest (default pulsa)
        };
    });
}
```

Variante folha-de-3 (sem frames extras): `Frames = new List<int> { 1 }` ou omitir
`WorkingEffects` e usar `TemporarySprites` com textura vanilla de fumaça (§3.2).

### 5.4. Passo 3 — `Item.modData` (já existe; ligar à colocação/retirada)

O motor vanilla **não** persiste chaves custom de `modData` no timer da máquina:
ao recolher um `(BC)` em processamento, o jogo larga o `heldItem` e zera
`minutesUntilReady`. O contrato `FR-005c` exige que o progresso sobreviva, então:

1. **Ao recolher/dislodgar** (já implementado em `CompostingBinService` + handler
   de dislodge em `ModController`): `WriteToModData` (`InputId`, `DaysRemaining`)
   + stack 1 + pausa (fora do mundo, o dia não avança para o item).
2. **Ao colocar** (gap a fechar, `T015/T026`): ler `PopulateFromModData`; se houver
   progresso, re-hidratar a máquina — `PlaceInMachine` com o input restaurado e
   timer proporcional aos dias restantes, **ou** `OutputMethod` custom que lê o
   modData e devolve `overrideMinutesUntilReady`. Se o spike (`spec.md:294-296`)
   concluir que nenhum dos dois preserva `modData` de forma save-compatível, o
   fallback especificado é interceptação SMAPI (`OnDayStarted`/`OnButtonPressed`
   no `ModController`) com definições de dados vanilla — a arte e este briefing
   continuam válidos, só muda o driver do timer.
3. **Nunca** confiar no timer vanilla como fonte de verdade para `DaysRemaining`:
   fonte = `CompostingBinStateModel`; timer vanilla = espelho.

### 5.5. Como alternar sprite: conteúdo vs. C# (regra de decisão)

| Precisa… | Via | Custo |
|---|---|---|
| Trocar empty→processing→ready; vapor cíclico; som de carga; shake | **Conteúdo** (`ShowNextIndex*`, `Increment*`, `WorkingEffects`) | Zero código de desenho; multiplayer-safe; testável por `AssetRegistrationTests` |
| Sprite proporcional ao progresso (ex. heap enchendo dia 1 → dia 2) | **C#** (`OutputMethod`/`InteractMethod` ajustando índice, ou Harmony no draw) | Quebra Automate/hopper se mal feito; exige sincronia multiplayer; foge de `FR-001a` |
| Partículas além do vanilla | `TemporarySprites` (conteúdo, textura vanilla ou custom) | Médio: exige `SourceRect`/offsets calibrados |

**Decisão:** tudo por conteúdo. C# só no fallback do timer (§5.4), nunca no desenho.

---

## 6. Verificação (como provar que o Processing funciona)

### 6.1. Manual em jogo (obrigatório — `spec.md:300-302`, template de PR exige)

1. Craftar bin (50 madeira / 25 pedra / 15 fibra) → entra no inventário como `(BC)`.
2. Colocar na fazenda → sprite **empty**, recolhível.
3. Largar item inválido (ex. pedra) → **shake + cue de cancel**, sem texto, sem
   consumo (vanilla; `FR-008a` não exige HUD aqui).
4. Largar resíduo válido → **tremor + som curto**, sprite vira **processing**
   (heap + vapor), `heldItem` interno = input.
5. Dormir 1 noite → ainda processing (nada coletável; tentar coletar = sem output).
6. Dormir 2ª noite → sprite **ready** (faísca), som/partículas vanilla.
7. Colher → 1 composto `(O)` no inventário, bin volta a empty (`FR-005a`).
8. Repetir 4–5 e **recolher a máquina no meio** → item com modData, stack 1;
   recolocar → retoma de onde parou (`FR-005c`, `SC-009`).
9. Colher com inventário cheio → nada perdido, `showRedMessage` localizada
   (`message.harvest.inventory-full`).
10. Save/reload em cada estado (empty, processing dia 1, processing dia 2, ready)
    → estados idênticos (`SC-004`).
11. Noite/chuva: vapor legível sem poluir; sem `LightWhileWorking` a caixa não
    deve brilhar.

### 6.2. Automatizada (porta de entrada: `tasks.md` T010/T012/T016–T021)

- `AssetRegistrationTests` (T010): `Data/BigCraftables` contém o bin com
  `Texture`/`SpriteIndex` corretos; `Data/Machines[(BC)…]` tem 1 regra,
  `Trigger == ItemPlacedInMachine`, `DaysUntilReady == 2`,
  `ShowNextIndexWhenReady == true`, `IncrementMachineParentSheetIndex == 1`.
- `CompostingMachineInvariantsTests` (T016, FsCheck): Empty + resíduo →
  Processing; Processing + 2 day-updates → Ready; quantidade de output == 1
  para qualquer input válido.
- `BinPickupTests` (T012): pickup mid-processing serializa modData, stack 1,
  debris com modData se inventário cheio.
- Gates finais: `dotnet format --verify-no-changes` → build Release com
  `GamePath` → suite 100% (`tasks.md` T048–T051).

---

## 7. Riscos e fallback (assunções da spec, `spec.md:294-298`)

1. **`Item.modData` × timer nativo.** O motor não foi feito para pausar/retomar
   timers custom via modData. Se `PlaceInMachine`/`OutputMethod` não re-hidratarem
   de forma save-compatível dentro do spike limitado: fallback para
   `ModController` event-interceptado (já arquitetado) mantendo as definições
   `Data/*` vanilla — sprites e sons deste briefing **não mudam**.
2. **Registro programático vs. save vanilla.** `AssetRequested` em C# é o caminho
   primário; Content Patcher como dependência é fallback explícito da spec.
3. **IDs de cue de áudio.** `LoadEffects.Sounds[].Id` deve ser cue vanilla
   existente (`Data/Audio`); os `.ogg` em `Assets/Audio/` são placeholders sem
   pipeline (nota do repo) — **não referenciá-los** até existir loader.
4. **Conjuntoqualificável (FR-003).** As `RequiredTags` vêm da spec 002; qualquer
   divergência (ex. artesanais excluídos sem enforcement — divergência conhecida,
   `spec.md:323-326`) resolve-se **naquela spec**, nunca aqui.

---

## 8. Fora de escopo (não pintar, não codar, não prometer)

- Outputs aromatizados (`FLAVORED_ITEM`, `PreserveId: DROP_IN`, `CopyColor/Price`)
  — output é item fixo único.
- Combustíveis por receita (`AdditionalConsumedItems`) ou por-regra
  (framework Extra Machine Config) — sem carvão/lenha na compostagem.
- `RecalculateOnCollect`, `AllowLoadWhenFull: true`, múltiplas regras de output.
- Sprites HD / `Alternative Textures` / re-texturas de terceiros.
- UI própria, desenho de texto custom, sons `.ogg` custom.
- Mudança no conjunto compostável, no `MaturationDays`, no `+15`/`100`.
- Drop-on-ground com perda de progresso (comportamento antigo **superseded** por
  `FR-005c`; `spec.md:317-320`).
- Verificação live com 2 jogadores (roadmap D1; aqui: stubs automatizados
  `MultiplayerConcurrencyTests`, `SC-005/SC-010`).
- Reivindicações de README sem `(Planned)` (US4, `FR-016` — tarefa separada).

---

## 9. Fontes (todas verificadas por fetch em 2026-10-05)

1. `Modding:Machines` — stardewvalleywiki.com — schema completo de
   `Data/Machines` (`OutputRules`, `Triggers`, `DaysUntilReady`,
   `WorkingEffects`, `ShowNextIndex*`, `TemporarySprites`, delegates C#).
2. `Tutorial: Adding New Machines` — stardewmodding.wiki.gg — BigCraftable 16×32,
   índices sequenciais, `Texture`/`SpriteIndex`, receita de 3 sprites com
   `IncrementMachineParentSheetIndex + ShowNextIndexWhenReady`, animação por
   `Frames`+`Interval`+`WorkingEffectChance: 1`.
3. `Modding:Big craftables` — stardewvalleywiki.com — `(BC)`, `Data/BigCraftables`,
   `Fragility`, `CanBePlacedIndoors/Outdoors`.
4. `Modder Guide/APIs/Content` — stardewvalleywiki.com — `AssetRequested`,
   `LoadFromModFile<Texture2D>`, `AsDictionary`, `PatchImage`, `Mods/<id>/`
   custom assets.
5. `Compost Bin` — nexusmods.com/stardewvalley/mods/48318 — prior-art de máquina
   de compostagem (só metadados públicos; detalhe bloqueado por 403 — não usar
   como spec).
6. Evidência local: `ModEntry.cs:115-177`, `Constants.cs:47-78`,
   `CompostingBinStateModel.cs:40-100`, `CompostingBinService.cs:190-231`,
   `ModController.cs:788-792,1198-1199`, `LivingRoots.csproj:26-28`,
   `manifest.json`, `i18n/default.json`, medição Pillow dos 4 PNGs (§1.2),
   `specs/004-playable-compost-loop/{spec,plan,tasks}.md`.
