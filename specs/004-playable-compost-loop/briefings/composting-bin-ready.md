# Briefing — `composting_bin_ready.png` (Feature 004, Compost Loop)

**Data:** 2026-10-05 · **Feature:** `specs/004-playable-compost-loop/` · **Asset:** `LivingRoots/Assets/composting_bin_ready.png`
**Status do asset:** existe no disco, copiado para o build, **mas unwired** (nenhum C# o carrega).
**Decisão vigente:** 004, FR-005a — *one in, one out*: 1 lote pronto = exatamente 1 compost, sem multiplicador 1–5.

Este briefing responde: como indicar visualmente "bin Ready=2, compost pronto para coletar"
de forma vanilla-consistente, acessível (não-só-cor) e verificável in-game.

---

## 1. Estado atual (verificado no repo)

### 1.1 Arquivos e dimensões

| Arquivo | Dimensão real (`file`) | Padrão esperado | Situação |
|---|---|---|---|
| `LivingRoots/Assets/composting_bin_empty.png` | 16×32 RGBA | BigCraftable = 16×32 | OK de tamanho, unwired |
| `LivingRoots/Assets/composting_bin_processing.png` | 16×32 RGBA | BigCraftable = 16×32 | OK de tamanho, unwired |
| `LivingRoots/Assets/composting_bin_ready.png` | 16×32 RGBA | BigCraftable = 16×32 | OK de tamanho, **unwired — objeto deste briefing** |
| `LivingRoots/Assets/compost.png` | 16×16 RGBA | Object = 16×16 | OK de tamanho, unwired |

Verificação executada em 2026-10-05 via `file` + cabeçalho PNG: os três bins têm
16×32 e o compost 16×16, que é exatamente o padrão vanilla (ver §3.1).

O `.csproj` copia os arquivos para o output mas não os registra no pipeline
(`LivingRoots/LivingRoots.csproj:26-28` — `Content Include="Assets\**\*.*"`,
`CopyToOutputDirectory=PreserveNewest`). Copiar ≠ wirar: **não há nenhum
`ModContent.Load<Texture2D>`, `e.Load`, `AsImage().PatchImage`, `Texture`
em `Data/BigCraftables`/`Data/Objects`, nem `Content.Load` em todo o C#**.
`grep` por `Texture|SpriteIndex|ModContent|PatchImage|AsImage` nos `Services/`,
`Controllers/` e `ModEntry.cs` retorna zero para esses assets. É por isso que o
repo chama `Assets/` e `i18n/` de *placeholders, not wired* (AGENTS.md raiz).

### 1.2 Registro atual em `ModEntry.cs:151-175` — o que falta

O bloco `Data/Machines` hoje registra apenas:

```csharp
// LivingRoots/ModEntry.cs:152-175 (estado em 2026-10-05)
data[binId] = new MachineData          // binId = "(BC)LivingRoots.CompostingBin"
{
    OutputRules = [ new MachineOutputRule { Id = "Default",
        OutputItem = [ new MachineItemOutput {
            ItemId = ModConstants.QualifiedCompostItemId }] } ]
};
```

Problemas concretos (todos bloqueiam qualquer indicação visual de ready):

1. **Sem tempo de processamento.** Não há `DaysUntilReady` nem `MinutesUntilReady`.
   Sem isso a regra produz output *instantâneo* (wiki: "If none are specified, the
   item will be ready instantly"). O MaturationDays=2 (`Constants.cs:59`) não chega
   ao jogo — vive só no `CompostingBinService` paralelo.
2. **Sem trigger de entrada.** Não há `Triggers` (`ItemPlacedInMachine`,
   `MachinePutDown`/`DayUpdate`/`OutputCollected`). Sem trigger, o jogo não sabe
   quando iniciar a regra nem como recarregar (cf. worm bin = `MachinePutDown`,
   crystalarium = `OutputCollected`, soda machine = `DayUpdate`).
3. **Sem sinal visual de ready.** Não há `ShowNextIndexWhenReady`,
   `ShowNextIndexWhileWorking`, `WobbleWhileWorking`, `LightWhileWorking`,
   `WorkingEffects`/`LoadEffects`, `ShakeDuration`/`Frames`. O registro é
   funcionalmente mudo e parado.
4. **Sem mensagem de recusa.** Não há `InvalidItemMessage` /
   `InvalidCountMessage`. A spec (FR-008a, clarificação 2026-10-04) manda rejeição
   de item inválido seguir o comportamento vanilla (audio `cancel` + shake, sem
   texto), mas isso precisa estar declarado ou documentado como fallback.
5. **Inconsistência de catálogo (bug latente).** O bin é registrado em
   `Data/Objects` (`ModEntry.cs:125-140`, chave `LivingRoots.CompostingBin`) mas a
   chave de máquina é `(BC)LivingRoots.CompostingBin` — prefixo `(BC)` = *big
   craftable*, cujo catálogo é `Data/BigCraftables`, não `Data/Objects`
   (wiki `Modding:Big_craftables`: "their data in `Data/BigCraftables`").
   As entradas `ObjectData` também não definem `Texture`/`SpriteIndex`, então o
   jogo resolve o sprite para o default (`Maps/springobjects` /
   `TileSheets/Craftables`) — os PNGs de 16×32 nunca são endereçados.
6. **Descrições desatualizadas.** `ModEntry.cs:133` diz "Output increases with
   continuous use" e `i18n/default.json:5` repete — resquício do multiplicador
   1–5 que a decisão 004 aposentou (FR-005a). O texto do bin precisa dizer
   "1 waste → 1 compost in 2 days".

### 1.3 Máquina de estados paralela vs. máquina nativa

- `LivingRoots/Domain/CompostingBinState.cs:7-17` — `Empty=0, Processing=1, Ready=2`.
- `LivingRoots/Services/CompostingBinService.cs` — cache paralelo
  `_runtimeCache`, `AddWaste` (`:33-59`), `CollectCompost` (`:61-98`),
  `ProcessDayStart` (`:112-157`), persistência `modData`
  (`OnBinPlaced` `:173-203`, `CreateDislodgedBinItem` `:205-225`).
- `LivingRoots/Controllers/ModController.cs:848-893` — `OnButtonPressed`
  (right-click): aplica compost se estiver segurando compost (`:864-873`),
  senão `AddWaste` se `Empty` (`:879-882`), `CollectCompost` se `Ready` e mão
  vazia (`:884-887`). **Não consulta `Object.readyForHarvest` nem
  `IsMachineWorking()`** — o estado exibido nunca vem do jogo.
- Sons atuais (`CompostingBinService.cs:93,128`): `Game1.playSound("Ship")` no
  collect **e** no `ProcessDayStart`. O segundo é suspeito: toca um som de
  "item caindo na caixa" no virar do dia sem contexto de jogador/localização
  (ver §5 — proposta: remover e deixar o jogo sinalizar via efeitos de máquina).

Conclusão do estado atual: o loop jogável roda **apesar** do jogo, não **através**
dele. O `composting_bin_ready.png` é o símbolo desse gap: o artefato existe, o
fio não.

---

## 2. Pesquisa — como vanilla e mods indicam "machine ready"

Fontes primárias: `wiki.stardewvalley.net/Modding:Machines` (revisão 2026-02-27),
`Modding:Big_craftables`, `Modding:Objects`, `Modding:Audio`, `Modding:Modder
Guide/APIs/Content`, código decompilado `StardewValley/Object.cs` (veywrn),
mods `Crop Harvest Bubbles`, `Crop Irrigation and Harvest Indicator`,
`Informant`, e wiki de acessibilidade (`stardewmodding.wiki.gg`).

### 2.1 O vocabulário vanilla de "pronto" (5 canais, não 1)

O jogo nunca sinaliza ready só com cor. O padrão observado em furnace, keg,
preserves jar, worm bin, crystalarium e nas bolhas de colheita:

| # | Canal | Implementação vanilla / dado | Exemplo |
|---|---|---|---|
| 1 | **Bolha de output acima da máquina** | `Object.readyForHarvest=true` + `heldObject != null` → o jogo desenha o sprite do output flutuando/balançando (`bobbing`) acima da máquina | Furnace com barra pronta; keg com vinho pronto. Mods `Crop Harvest Bubbles` / `Crop Irrigation and Harvest Indicator` imitam exatamente isso ("native-style visual bubbles", "similar to those shown for big craftable workstations") |
| 2 | **Troca de sprite da máquina** | `ShowNextIndexWhenReady=true` (mostra sprite N+1 quando pronta), `ShowNextIndexWhileWorking`, `IncrementMachineParentSheetIndex` por regra | Máquinas que "enchem"/mudam de tampa quando trabalhando vs. prontas |
| 3 | **Movimento** | `WobbleWhileWorking` (incha/encolhe), `ShakeDuration` no load/ready, bounce da bolha de output, `Frames`+`Interval` para animar o sprite | Shake ao carregar; bolha que quica suavemente |
| 4 | **Luz / partículas / vapor** | `LightWhileWorking` (`Radius`+`Color`), `WorkingEffects`/`LoadEffects` (`Sounds`, `TemporarySprites` com `Texture`+`SourceRect`, `Flicker`, `Scale`), `WorkingEffectChance` (default 0.33) | Vapor da furnace, `steam` cue, brilho de máquina ativa |
| 5 | **Áudio** | `LoadEffects.Sounds[]` (`Id` = cue + `Delay` ms), sons hardcoded vanilla (`bubbles`+`Ship` ao carregar keg) | `Ship` = "items dropping into a wooden box"; `cancel` = "short shaker… insufficient resources" (wiki Audio) |

Implicação para o bin: **`composting_bin_ready.png` deve ser o canal 2 (sprite N+1
via `ShowNextIndexWhenReady`), e a bolha de output (canal 1) vem de graça quando
`heldObject` for o compost** — desde que `Data/Machines` declare tempo e output
corretos. Os canais 3–5 são o "glow/steam sutil" (§6.2): opcionais, baratos e
nativos.

### 2.2 Como o SMAPI / jogo sinaliza ready para C# (interop)

Campos e métodos em `StardewValley.Object` (1.6), todos acessíveis sem Harmony:

- **Campos de estado:** `readyForHarvest` (`NetBool`), `heldObject` (`Object`),
  `minutesUntilReady` / `daysUntilReady`, `showNextIndex` (`NetBool`),
  `lastOutputRuleId` + `lastInputItem` (novos na 1.6 para inspeção).
- **Métodos:** `IsMachineWorking()`, `ShouldTimePassForMachine(location)`,
  `GetMachineData()`, `PlaceInMachine(…)`, `OutputMachine(…)`; classe utilitária
  `MachineDataUtility` (consultar output sem mutar — ideal para probes/tooltips).
- **Regra de ouro:** `probe=true` em `OutputMethod` **não deve** mutar máquina nem
  tocar som/animação. O `CompostingBinService` atual não tem conceito de probe —
  ao migrar para máquina nativa, qualquer validação antecipada deve usar
  `MachineDataUtility` ou `PlaceInMachine` com probe, nunca `AddWaste` direto.
- **Leitura recomendada no controller:** em `OnButtonPressed`, antes do cache
  próprio, ler o `Object` no tile (`location.Objects[tile]`) e checar
  `readyForHarvest && heldObject != null` como fonte de verdade do "pronto
  coletável". O cache paralelo (`GetBinState`) passa a ser *espelho/confirmação*
  (necessário para `modData` e multiplayer), não a única fonte.
- **Automate/Informant-compatibilidade:** mods de automação e tooltip leem esses
  mesmos campos. Se o bin só existe no cache privado, Automate nunca vai puxar o
  compost e o Informant nunca vai mostrar "2 days left" — mais um motivo para a
  migração nativa.

### 2.3 Análogos mais próximos (receitas prontas para copiar)

1. **Worm Bin (o gêmeo funcional).** Produz isca todo dia sem input contínuo via
   triggers `MachinePutDown` + `DayUpdate`; em 1.6 gera 4–5 bait/dia. É o modelo
   para um bin "passive + 2-day batch": trigger de load + `DaysUntilReady=2`.
2. **Keg / Preserves Jar (o modelo de load).** `PlaceInMachine` → `bubbles` +
   `Ship`, `minutesUntilReady` setado, `showNextIndex` durante trabalho. Copiar o
   par sonoro no `LoadEffects`.
3. **Crystalarium (o modelo de reload).** Regra `OutputCollected` recarrega
   sozinha após a coleta. Se o design futuro quiser "bin contínuo", é esse
   trigger — **hoje fora de escopo** (one-shot por lote, FR-005a).
4. **Soda Machine (o modelo DayUpdate).** Produz no virar do dia se ociosa.
   Útil como referência de `DayUpdate` sem input.

### 2.4 Requisitos de spritesheet (o que o jogo exige)

- **BigCraftable (bin):** tile lógico **16×32 px** (largura × altura), 2 tiles
  verticais de 16×16. Confirmado em três vias: (a) dimensões reais dos PNGs;
  (b) wiki `Modding:Big_craftables` (`Texture` + `SpriteIndex`, default
  `TileSheets/Craftables`); (c) comunidade (`Sprites in Detail`: "sprites are
  limited to a 16x32 rectangle"). `SpriteIndex` conta da esquerda para a
  direita, de cima para baixo, a partir de 0 no topo-esquerdo da `Texture`.
- **Object (compost):** **16×16 px** (`Maps/springobjects` default;
  `Modding:Objects`: `Texture` + `SpriteIndex`, `ColorOverlayFromNextIndex`).
- **Texture custom:** apontar para um asset SMAPI, convenção
  `Mods/<UniqueID>/<Nome>` (ex. `Mods/lfgranja.LivingRoots/CompostingBin`).
  O asset precisa ser **fornecido via `AssetRequested`** (`e.Load<Texture2D>`
  para asset novo, `e.Edit(AsImage().PatchImage…)` para patch) — `ModContent.Load`
  sozinho não publica no content pipeline do jogo.
- **Transparência:** PNG-32 com alfa; fundo 100% transparente fora da silhueta.
  Nada de JPG, nada de premultiplied estranho — o XNA/MonoGame espera alfa reto.
- **Escala de render:** o jogo desenha com `pixelZoom` (tipicamente 4×). 1 px de
  sprite = 4 px de tela. Detalhes < 2 px de sprite somem; linhas de contorno de
  1 px viram 4 px — pixel art precisa ser legível em 16×32 *e* em 64×128.
- **Iluminação ambiente:** sprites de mundo passam por passe de luz ambiente
  (noite/chuva/inverno escurecem). Cores muito escuras somem à noite; é por isso
  que `LightWhileWorking` existe — um raio pequeno compensa sem virar lanterna.

---

## 3. Spec técnica — do unwired ao nativo

### 3.1 Dados: `Data/BigCraftables` (novo) + `Data/Machines` (reescrito)

**Passo 0 — mover o bin para o catálogo certo.** Registrar em
`Data/BigCraftables` (não `Data/Objects`):

```csharp
// Esqueleto — nomes de campos conforme BigCraftableData 1.6
data["LivingRoots.CompostingBin"] = new BigCraftableData
{
    Name = "Composting Bin",
    DisplayName = "Composting Bin",           // depois: token i18n
    Description = "... 1 waste → 1 compost in 2 days ...",
    Price = 0,
    Fragility = 0,                            // pode recolher com qualquer ferramenta
    CanBePlacedIndoors = true,
    CanBePlacedOutdoors = true,
    Texture = "Mods/lfgranja.LivingRoots/CompostingBin",  // spritesheet próprio
    SpriteIndex = 0,                          // 0=empty (base), 1=processing (N+1 working), 2=ready (N+1 ready)*
};
```

\* **Restrição real do motor:** `ShowNextIndexWhileWorking` /
`ShowNextIndexWhenReady` mostram **o sprite imediatamente seguinte (N+1)**,
não um índice arbitrário. Com um único `SpriteIndex` base, só há *um* slot N+1.
Três estados (empty/processing/ready) em sprites separados exigem **spritesheet
combinado de 3 frames verticais (16×96)** OU aceitar 2 estados nativos
(base + N+1) e deixar o terceiro para `WorkingEffects`/`LightWhileWorking`.
Ver recomendação em §6.1 (spritesheet vertical de 3 frames, `SpriteIndex=0`,
com `ShowNextIndexWhileWorking=true` + `ShowNextIndexWhenReady=true` — o motor
resolve N+1 por contexto; frames extras além de N+1 são ignorados pelo motor,
mas ficam disponíveis para fallback custom se um dia necessário).

**Passo 1 — reescrever `Data/Machines` (contrato mínimo jogável):**

```csharp
data["(BC)LivingRoots.CompostingBin"] = new MachineData
{
    OutputRules =
    [
        new MachineOutputRule
        {
            Id = "LivingRoots_CompostBatch",
            Triggers = [ new MachineOutputTrigger
            {
                Trigger = "ItemPlacedInMachine",
                // RequiredTags/RequiredItemId herdam o conjunto FR-003
                // (não restatar a lista aqui — referenciar a spec dona).
            } ],
            OutputItem = new MachineItemOutput
            {
                ItemId = "(O)LivingRoots.Compost",
            },
            MinutesUntilReady = -1,   // ignorado quando DaysUntilReady presente
            DaysUntilReady = 2,        // MaturationDays (Constants.cs:59)
        }
    ],
    ShowNextIndexWhileWorking = true,
    ShowNextIndexWhenReady = true,
    WobbleWhileWorking = false,        // default; habilitar só se o teste de
                                       // legibilidade (§8) aprovar (risco de flicker)
    AllowFairyDust = false,            // sem acelerador: 2 dias são regra (FR-001b)
    // LoadEffects: som vanilla de carga (keg-like)
    // WorkingEffects: steam sutil (WorkingEffectChance ~0.33 default)
    InvalidItemMessage = "...",        // token i18n; condição FR-008a
};
```

Notas de conformidade com a spec 004:

- **FR-001a/001b:** sem `OutputMethod` custom, sem `InteractMethod` custom, sem
  `RecalculateOnCollect`, sem `ReadyTimeModifiers`. O mod **não** acelera,
  completa ou redimensiona — o motor faz tudo.
- **FR-005a:** output fixo 1× `(O)LivingRoots.Compost`, sem `CopyQuality` /
  `CopyPrice` / qualidade aleatória. `UseFirstValidOutput` irrelevante (1 regra).
- **FR-003:** `RequiredTags` deve espelhar o conjunto dono (spec soil-decay);
  este briefing **não** lista itens (proibido restatar pela spec).
- **`OnlyCompleteOvernight`:** default `false` está correto para 2-day batch
  (o motor pausa fim-de-dia e completa no `DayUpdate`); mudar só com teste.
- **`PreventTimePass`:** não declarar (bin funciona dentro/fora, toda estação —
  sem regra contrária na spec).

### 3.2 Textura: publicar os 3 PNGs como 1 asset SMAPI

```csharp
// No OnAssetRequested existente (ModEntry.cs:114)
if (e.NameWithoutLocale.IsEquivalentTo("Mods/lfgranja.LivingRoots/CompostingBin"))
    e.LoadFromModFile<Texture2D>("Assets/composting_bin_sheet.png", ...);
// e equivalentemente "Mods/lfgranja.LivingRoots/Compost" <- Assets/compost.png
```

Pré-requisito de arte: montar `composting_bin_sheet.png` 16×96 (3 frames de
16×32 empilhados: empty / processing / ready) a partir dos três PNGs atuais —
ou, se preferido, publicar 3 assets de 16×32 e remapear via patch. A forma
canônica Content Patcher/SMAPI é **1 texture + N SpriteIndex**, não N textures.

Manter os arquivos individuais como fonte (`_src/`) e gerar o sheet no build
ou commitar o sheet — decisão de pipeline, sem impacto no motor.

### 3.3 Estado Ready=2 ↔ `readyForHarvest` (tabela de correspondência)

| `CompostingBinState` (mod) | `Object` nativo | Sprite exibido | Coleta |
|---|---|---|---|
| `Empty=0` | `heldObject==null`, `readyForHarvest=false` | frame 0 (empty) | `PlaceInMachine` aceita waste; inválido → `cancel`+shake |
| `Processing=1` | `heldObject==waste-derived`, `minutesUntilReady>0`, `showNextIndex` | frame N+1 (processing) + `WorkingEffects` ocasionais | interagir → nada (ou `InteractMethod` default = ignora) |
| `Ready=2` | `heldObject==compost`, **`readyForHarvest=true`** | frame N+1 (ready) + **bolha do compost quicando acima** | right-click → coleta 1 compost, `heldObject=null`, volta a `Empty` |

O `CompostingBinService` continua existindo como **camada de espelho** para:
`modData` (FR-005c), persistência tolerante (FR-012), lock multiplayer (FR-019)
e sons/mensagens localizadas (FR-008a/005b). Mas a *leitura de exibição* deve
preferir o `Object` — é o que Automate, Informant e o próprio renderer veem.

---

## 4. Interop com `Game1.playSound("Ship")` / `cancel` atuais

### 4.1 Semântica vanilla (wiki `Modding:Audio` + `Object.cs` decompilado)

- **`Ship`** (Wavebank 96): "items dropping into a wooden box" — usado no
  shipping bin **e** no carregamento de keg (`Ship` + `bubbles`). É som de
  *entrada/depósito*, não de "pronto". Tocar `Ship` no collect é aceitável
  (item entrando no inventário), mas genérico.
- **`cancel`** (Wavebank 353): "short shaker… cancellation / insufficient
  resources". É o som canônico de *recusa* — exatamente o FR-008a para item
  inválido no bin.
- Família de coleta vanilla: `harvest` (pop de colheita), `coin` (moedas,
  com delay 260 ms em forrage), `bubbles` (processamento), `dirtyHit`,
  `hammer`/`axe`/`pickaxe` por ferramenta. Nenhum é "som de ready" — **o jogo
  não toca som quando a máquina fica pronta durante a noite**; o jogador
  descobre pelo sprite/bolha na manhã seguinte.

### 4.2 Diagnóstico do código atual

| Local | Som | Veredito |
|---|---|---|
| `CompostingBinService.cs:93` (`CollectCompost`) | `Ship` | **Manter (provisório).** Vanilla-plausível para coleta. Candidato futuro: `harvest` ou `coin` se o playtest achar `Ship` "pesado" para 1 compost. |
| `CompostingBinService.cs:128` (`ProcessDayStart`, transição Processing→Ready) | `Ship` | **Remover.** Toca no virar do dia, sem jogador por perto, possivelmente multiplas vezes (1 por bin), e mente sobre a semântica (`Ship` = depósito). O ready noturno deve ser *visual* (sprite N+1 + bolha na manhã seguinte). |
| Rejeição de item inválido | nenhum (silencioso) | **Adicionar `cancel` + shake.** É o FR-008a literal ("vanilla audio cancel cue and machine shake"). Hoje o `AddWaste` retorna silencioso (`:48-49`). |
| Carga válida | nenhum | **Adicionar `bubbles` (+`Ship` opcional) via `LoadEffects`.** Espelha o keg e dá feedback imediato de "aceito". |

### 4.3 Proposta de mapa sonoro (mínimo, 100% vanilla)

| Evento | Som | Via | Observação |
|---|---|---|---|
| Waste aceito | `bubbles` (+ `Ship` se o playtest pedir peso) | `LoadEffects.Sounds` em `Data/Machines` | Motor toca; sem código |
| Item inválido | `cancel` + `ShakeDuration` ~300–500 ms | `Data/Machines` / fallback `location.playSound("cancel")` + `shakeTimer` no controller | Sem texto (FR-008a) |
| Lote pronto (overnight) | *(silêncio)* | — | Descoberta visual na manhã seguinte, como vanilla |
| Coleta com sucesso | `Ship` (manter) ou `harvest` | `CollectCompost` | 1 toque, não por bin |
| Coleta com inventário cheio | `cancel` + `Game1.showRedMessage(message.harvest.inventory-full)` | `ModController`/`CompostingBinService` (FR-005b+008a) | Nada é consumido/duplicado |

Volume/categoria: cues vanilla respeitam o slider de efeitos; nenhum ajuste custom.

---

## 5. Direção de arte

### 5.1 Princípios (Stardew-look em 16×32)

1. **Silhueta primeiro, detalhe depois.** A 4× de zoom, a forma do bin (caixa de
   ripas com tampa) deve ser reconhecível a 3 tiles de distância. Não afinar
   ripas além de 2 px de sprite.
2. **Paleta do jogo, não do flat-design.** Tons terrosos dessaturados, contorno
   escuro de 1 px, 1–2 tons de highlight. Evitar gradientes suaves e neon — o
   `CompostReadyColor #166534` (`Constants.cs:50`) é cor de *overlay de solo*,
   não cor de sprite: aplicada pura no bin ela "grita" contra a fazenda.
3. **Luz vem de cima-esquerda.** Highlight no topo/esquerda das ripas, sombra na
   base direita — igual às crates e furnaces vanilla.
4. **Estados contam história de matéria:** vazio = madeira seca + interior
   escuro vazio; processando = tampa fechada + fresta quente; pronto = tampa
   entreaberta/erguida + massa escura rica visível + sinal luminoso sutil.

### 5.2 Os três frames (full + variantes)

**Frame 0 — Empty (`composting_bin_empty.png`, base).**
Manter o existente como referência; garantir: ripas em marrom médio
(~`#8A5A2B` ± textura), interior do compartimento em sombra neutra
(`#3A2E22`, **não** usar `CompostEmptyColor #78716C` como fill — é cinza de
overlay, no sprite vira "cimento"), tampa alinhada/fechada, sem brilho.

**Frame 1 — Processing (`composting_bin_processing.png`).**
Diferenciar por *forma*, não só matiz: tampa fechada com fresta âmbar fina
(eco de `CompostProcessingColor #A16207`, mas em faixa de 1–2 px, não banho de
cor), 2–3 pixels de vapor claro saindo da fresta (pixels soltos, não nuvem),
laterais com 1 px de sombra quente. Opcional nativo: `WobbleWhileWorking=false`
por default; se habilitado, validar contra enjoo (ver §7).

**Frame 2 — Ready (`composting_bin_ready.png`, o herói).**
Direção aprovada: **full + glow/steam sutil**, em camadas:

1. *Full (base legível sem efeitos):* tampa entreaberta (2–3 px de abertura
   mostrando massa de compost escura quase-preta esverdeada `#1E2B1F` com 3–5
   pixels de highlight `#3E5A34` — "terra rica"), uma pá de compost dourada
   inclinada para fora OU montinho transbordando pela borda (forma distinta do
   frame 1 mesmo em grayscale).
2. *Glow sutil (não-bloqueante):* 1 px de rim-light verde-amarelado
   (`#9ACD32`-ish, alfa ~40%) só na borda da abertura — nunca contorno inteiro.
   No jogo, reforçado por `LightWhileWorking`-like na coleta? Não: glow do
   sprite deve ser *pintado*, o `LightWhileWorking` é para Working, não Ready
   (não há `LightWhenReady` no schema — alternativa: `TemporarySprites` no
   collect).
3. *Steam sutil (vida, não festa):* 2 pixels ascendentes em alfa baixo acima da
   abertura, espaçados para não confundir com o vapor quente do processing
   (ready = vapor frio, branco-azulado, 1 frame; processing = âmbar).
4. *Marcador de forma (acessibilidade, §7):* **estrela/sparkle de 3–4 px** em
   amarelo quente (`SunColor #FACC15`, `Constants.cs:52`) no canto superior
   direito do bin + montinho transbordando. É o canal não-cromático que sobrevive
   a qualquer daltonismo e a screenshots P&B.

Anti-padrões vetados: ponto de exclamação gigante flutuante (conflita com
quests/NPC `!`), mudança de cor do corpo inteiro, glow que vaza para tiles
vizinhos, animação > 2 frames no sprite estático (o motor anima via
`Frames`/`TemporarySprites`, não via PNG animado).

### 5.3 `CompostReadyColor #166534` — onde usar e onde NÃO usar

- **Usar:** overlay de solo saudável (já é o uso em `Constants.cs:50`), cor de
  `LightWhileWorking.Color` se habilitada, tint de `TemporarySprites` no collect.
- **NÃO usar:** como fill do sprite do bin (verde-bandeira saturado em 4× vira
  "lixeira tóxica", some à noite e colide com plantações saudáveis ao redor).
  No sprite, verdes devem ser dessaturados e escuros (`#2D4A22`–`#3E5A34`).

### 5.4 Requisitos de entrega do sheet

- `composting_bin_sheet.png`: **16×96**, frames na ordem empty/processing/ready,
  mesma paleta/largura de contorno nos 3, fundo transparente, sem metadados
  estranhos (exportar como PNG-8/32 com alfa, sem interlace esquisito).
- Manter os 3 PNGs individuais como `_src` para diff.
- Teste de redução: encolher o sheet para 50% — os 3 estados ainda distinguíveis?
  Se não, voltar para forma (tampa/montinho), não para saturação.

---

## 6. Wiring — do PNG ao pixel na tela

### 6.1 Checklist de implementação (ordem)

1. **Arte:** montar o sheet 16×96 (§5.4); atualizar `composting_bin_ready.png`
   com tampa entreaberta + montinho + sparkle (§5.2).
2. **`ModEntry.OnAssetRequested`:** publicar
   `Mods/lfgranja.LivingRoots/CompostingBin` (`e.LoadFromModFile<Texture2D>`)
   e `Mods/lfgranja.LivingRoots/Compost`; mover bin para `Data/BigCraftables`
   com `Texture`+`SpriteIndex`; reescrever `Data/Machines` (§3.1) com
   `DaysUntilReady=2`, triggers, `ShowNextIndex*`, `LoadEffects`, mensagens.
3. **Textos:** corrigir descrições (one-in-one-out), adicionar
   `InvalidItemMessage` via i18n, manter as 4 chaves `message.*` existentes
   (`i18n/default.json:9-12`).
4. **Controller:** `OnButtonPressed` passa a resolver o `Object` no tile e
   priorizar `readyForHarvest/heldObject` para exibição/coleta; `AddWaste` vira
   `PlaceInMachine` (ou validação + `PlaceInMachine`); rejeição toca `cancel` +
   shake; inventário cheio mantém no bin + `showRedMessage` (FR-005b/008a).
5. **Serviço:** `CompostingBinService` vira espelho (modData, tolerância,
   locks); remover `playSound("Ship")` do `ProcessDayStart`; remover lógica de
   `MaturationLevel` 1–5 do caminho de yield (FR-005a) mantendo o reader
   tolerante a campos legados (`MaturationLevel`, `ConsecutiveActiveDays`,
   `ConsecutiveIdleDays` ignorados — spec 004 clarificação 2026-10-04).
6. **Perf/multiplayer:** sem polling; locks host-autoritativos existentes
   (`HostAtomicLockService`, FR-019) envolvem a interação; overlay reage a
   `OnTileHealthChanged` (FR-010, ≤16.67 ms).

### 6.2 Efeitos "glow/steam sutil" via dados (sem código custom)

```jsonc
// Ilustrativo — traduzir para MachineData C# em ModEntry.cs
"WorkingEffects": [{
    "Condition": null,
    "Sounds": [],
    "TemporarySprites": [{
        "Id": "LivingRoots_Steam",
        "Texture": "TileSheets/animations",   // spritesheet vanilla de vapor
        "SourceRect": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
        "PositionOffset": { "X": 0, "Y": -24 },
        "Color": "White",
        "Scale": 1, "Interval": 150, "Loops": 1
    }],
    "ShakeDuration": 0, "Frames": null, "Interval": 100
}],
"WorkingEffectChance": 0.33,   // default vanilla — sutil por construção
"LightWhileWorking": [{ "Radius": 0.5, "Color": "Warm Yellow" }]
```

Princípio: reusar `TileSheets/animations` e cues vanilla antes de inventar
partícula própria (YAGNI + compatibilidade com content packs de retexture).

---

## 7. Acessibilidade — nunca só cor

Referências: `SDV Colorblind Mod` (retextura de plantas indistinguíveis para
daltônicos vermelho-verde), `Better Crop Visibility` (pronto-colheita
distinguível), `Stardew Access` (screen reader — posição/saúde/tempo narrados),
`A Less Flashy Stardew` (reduz flashes), guias NIH (deuteranomalia ~6% homens,
protanopia/protanomalia ~2%, tritanopia raríssima).

### 7.1 Por que cor pura falha aqui

- Processing `#A16207` (âmbar-oliva) vs. Ready `#166534` (verde-bandeira): para
  deuteranopia/protanopia (cegueira vermelho-verde total ou parcial), **âmbar
  escuro vs. verde escuro colapsam em luminosidade similar** — especialmente à
  noite/chuva, quando o passe de luz achata tudo. O par sobrevive em visão
  típica e morre em ~8% dos homens.
- Tritanopia (azul-amarelo) é rara, mas o sparkle `SunColor #FACC15` sobre fundo
  verde pode vibrar — por isso o sparkle tem *forma* (estrela de 4 pontas com
  contorno escuro), não só brilho.

### 7.2 Matriz de redundância (cada estado usa ≥3 canais)

| Estado | Cor | Forma | Movimento | Áudio | Texto |
|---|---|---|---|---|---|
| Empty | madeira seca, interior sombra | tampa fechada alinhada, sem montinho | parado | — | tooltip "Empty" (Informant-compatível via `heldObject==null`) |
| Processing | fresta âmbar fina | tampa fechada, vapor quente | `WorkingEffects` ocasionais (chance 0.33) | `bubbles` no load | dias restantes via mods de tooltip (lê `minutesUntilReady`) |
| Ready | abertura escura rica + rim sutil | **tampa entreaberta + montinho + sparkle ★** | **bolha do compost quicando acima** (canal vanilla mais forte) | `Ship`/`harvest` no collect; silêncio no overnight | bolha mostra o próprio item (leitura sem cor) |

Regras:

1. **Forma carrega o significado; cor confirma.** Qualquer jogador deve
   distinguir os 3 estados numa screenshot em grayscale.
2. **Movimento é o canal mais democrático** (daltonismo não afeta): a bolha
   quicando é o sinal primário de ready, o sprite N+1 é o secundário.
3. **Áudio é redundância, não requisito:** pronto overnight é silencioso
   (vanilla); recusa e coleta têm sons distintos (`cancel` vs. `Ship`).
4. **Sem pisca-pisca:** nada de `Flicker=true` em alta frequência ou
   `WobbleWhileWorking` agressivo — respeitar `A Less Flashy Stardew`
   (fotossensibilidade). `Wobble` default off; habilitar só com teste.
5. **Screen reader (Stardew Access):** estados derivam de `heldObject`/
   `readyForHarvest`, que leitores de mundo já conseguem narrar — mais um motivo
   para não esconder estado só no cache privado.
6. **Contraste mínimo do sparkle:** estrela `#FACC15` sempre com contorno de
   1 px escuro (`#3A2E22`); testar sobre grama clara, terra, neve e à noite.

---

## 8. Verificação in-game (roteiro manual, ~25 min)

Pré-condição: `GamePath` válido, build Release, save novo, 1 bin craftado
(50 wood / 25 stone / 15 fiber — `Constants.cs:69-71`).

### 8.1 Loop base (SC-001/SC-002)

1. Craft → place na fazenda → sprite = **frame empty**, sem bolha, sem som
   residual.
2. Segurar waste válido → right-click → `bubbles` (+`Ship` se declarado),
   shake curto, sprite = **frame processing**.
3. Segurar item inválido (ex. pedra/pau) → right-click → **`cancel` + shake**,
   sem texto, nada consumido, sprite inalterado.
4. Dormir 1 dia → ainda processing, sem som noturno.
5. Dormir 2º dia → manhã: sprite = **frame ready** + **bolha do compost
   quicando acima** (o momento que este briefing entrega). Sem som ao acordar.
6. Mão vazia → right-click no bin ready → som de coleta, **1 compost**
   `(O)LivingRoots.Compost` no inventário, bin volta a empty, bolha some.

### 8.2 Casos-limite (FR-005b/005c/012, SC-004/SC-009)

7. Inventário cheio + coleta → `cancel` + `showRedMessage(message.harvest.
   inventory-full)`, **compost permanece no bin**, retry posterior entrega
   integral (nada perdido/duplicado).
8. Pickup do bin mid-processing → vira item não-empilhável (stack 1) com
   `modData` (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`);
   recolocar restaura o progresso (sem reset Atalho).
9. Machadada/explosão com inventário cheio → dropa debris flutuante com
   `modData` intacto (`Game1.createItemDebris`).
10. Save/reload em cada estado (empty/processing/ready) → estados e progresso
    idênticos; entradas corruptas/legadas quarentenadas sem crash (FR-012/015).
11. Dia avança com jogador fora da fazenda → progresso avança exatamente 1×
    (FR-004).

### 8.3 Legibilidade (arte + acessibilidade)

12. Screenshots dos 3 estados em: meio-dia claro, noite, chuva, inverno.
    Critério: distinção sem ler cor (grayscale test).
13. Zoom 100% e 200%, com e sem `LightWhileWorking`: glow nunca vaza para tile
    vizinho; sparkle legível sobre neve e terra.
14. Sessão de 10 min com os 3 bins lado a lado (empty/processing/ready):
    nenhum relato de "qual está pronto?" após 3 s de olhar.
15. Multiplayer (automatizado em 004, live em D1): dois jogadores no mesmo bin —
    primeiro vence, segundo recebe recusa explicada (FR-019); overlay do
    farmhand invalida em ≤1 frame via `OnTileHealthChanged` (FR-010).

Critério de aceite do asset: **passos 5–6 e 12–14 verdes**. Sem isso, o PNG é
decoração.

---

## 9. Out-of-scope (explicitamente NÃO fazer aqui)

1. **Redesign do conjunto compostável (FR-003).** Herdar por referência da spec
   soil-decay; nem listar itens, nem corrigir o gap "artisan goods" (assunção
   registrada na spec 004).
2. **Multiplicador 1–5 / `MaturationLevel` como yield.** Aposentado pela decisão
   004 (FR-005a). Manter apenas leitura tolerante de saves legados.
3. **Reload automático (`OutputCollected` crystalarium-like).** Bin é one-shot
   por lote; recarga contínua é design futuro.
4. **UI própria.** Nenhum painel, texto desenhado, ou artwork explicativa de
   recusa (FR-008a proíbe; refusal = som+shake ou `showRedMessage` i18n).
5. **`Fairy Dust`, `ReadyTimeModifiers`, `RecalculateOnCollect`,
   `OutputMethod`/`InteractMethod` customs.** Aceleradores e lógica C# custom
   contradizem FR-001b.
6. **Transporte multiplayer (D1).** Estado continua farm-scoped/host-local;
   004 exige apenas semântica first-wins + stubs (FR-018/019, SC-005/010).
7. **Retextura de máquinas vanilla ou temas sazonais do bin.** Sheet único;
   variantes (Alternative Textures, sazonais) são packs futuros.
8. **Mudança na paleta de overlay de solo.** `CompostReadyColor` e tiers
   001 permanecem intactos; este briefing só disciplina seu (não-)uso no sprite.

---

## 10. Riscos e decisões abertas

| # | Risco / pergunta | Impacto | Mitigação / dono |
|---|---|---|---|
| 1 | `Data/Objects` vs `Data/BigCraftables` — save existente com o bin como `(O)` quebra ao migrar para `(BC)`? | Save-compat | Tolerant reader + nota de migração; testar upgrade de save no roteiro §8.2 |
| 2 | Um único N+1 comporta 3 estados? (`ShowNextIndex*` só enxerga N+1) | Arte/dados | Sheet 16×96 + base N resolve working/ready por contexto; validar em jogo antes de fechar arte |
| 3 | `Ship` no collect vs `harvest`/`coin` | Feel | Playtest cego A/B; trocar é 1 linha |
| 4 | `WobbleWhileWorking` / `LightWhileWorking` causam ruído visual ou custo de frame? | Perf/acess. | Default off; habilitar atrás de teste §8.3; orçamento ≤16.67 ms (FR-010) |
| 5 | `RequiredTags` do trigger divergem da spec dona | FR-003 | Referenciar, nunca copiar; linkar a spec dona no comentário do código |
| 6 | Automate puxa output antes do jogador ver a bolha | Experiência | Aceitável (vanilla-equivalente); documentar no README se necessário |

---

## 11. Referências (consultadas 2026-10-05)

- Wiki — `Modding:Machines` (schema completo: `OutputRules`, `Triggers`,
  `DaysUntilReady`, `ShowNextIndexWhen/WhileWorking`, `WobbleWhileWorking`,
  `LightWhileWorking`, `Working/LoadEffects`, `InvalidItemMessage`,
  `MachinePutDown`/`DayUpdate`/`OutputCollected`): <https://wiki.stardewvalley.net/Modding:Machines>
- Wiki — `Modding:Big_craftables` (`Texture`, `SpriteIndex`, `TileSheets/Craftables`):
  <https://wiki.stardewvalley.net/Modding:Big_craftables>
- Wiki — `Modding:Objects` (`Texture`, `SpriteIndex`, `ColorOverlayFromNextIndex`):
  <https://wiki.stardewvalley.net/Modding:Objects>
- Wiki — `Modding:Audio` (`Ship` = wooden box drop; `cancel` = short shaker /
  insufficient resources): <https://wiki.stardewvalley.net/Modding:Audio>
- Wiki — `Modding:Modder Guide/APIs/Content` (`ModContent.Load`, `AssetRequested`,
  `AsImage/PatchImage`): <https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Content>
- `StardewValley/Object.cs` decompilado (`readyForHarvest`, `heldObject`,
  `showNextIndex`, `PlaceInMachine`, `IsMachineWorking`):
  <https://github.com/veywrn/StardewValley/blob/master/StardewValley/Object.cs>
- `BigCraftableData.cs` (`Texture`, `SpriteIndex`): Dannode36/StardewValleyDecompiled
- Mods-espelho: `Crop Harvest Bubbles` (bolha = padrão workstation),
  `Crop Irrigation and Harvest Indicator` (native-style bubbles + bobbing),
  `Informant` (tooltip de máquina: conteúdo + tempo restante)
- Acessibilidade: `SDV Colorblind Mod`, `Better Crop Visibility`,
  `Stardew Access`, `A Less Flashy Stardew`
  (<https://stardewmodding.wiki.gg/wiki/Accessibility_and_Ease_of_Play>),
  tipos de daltonismo — NIH/National Eye Institute
- Repo (estado em 2026-10-05): `LivingRoots/ModEntry.cs:114-176`,
  `LivingRoots/Constants.cs:47-79`, `LivingRoots/Controllers/ModController.cs:848-893`,
  `LivingRoots/Services/CompostingBinService.cs:33-98,112-157,173-225`,
  `LivingRoots/Domain/CompostingBinState.cs:7-17`,
  `LivingRoots/Domain/Models/CompostingBinStateModel.cs:36-103`,
  `LivingRoots/i18n/default.json:1-13`, `LivingRoots/LivingRoots.csproj:26-28`,
  `specs/004-playable-compost-loop/{spec,plan,tasks}.md`

*Notas de honestidade: dimensões dos PNGs medidas localmente (`file` + header
PNG); ausência de wiring verificada por busca no C#; semânticas de áudio e schema
`Data/Machines` citadas da wiki vigente. Comportamentos de motor não cobertos
por teste local (ex. N+1 exato, `WorkingEffectChance` default 0.33) estão marcados
como "validar em jogo" no §8 e não devem ser afirmados como garantidos.*
