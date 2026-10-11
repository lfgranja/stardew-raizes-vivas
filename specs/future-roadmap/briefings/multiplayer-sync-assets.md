# Briefing — Multiplayer Support: a superfície de assets que ela impõe a todos os outros

**Data:** 2026-10-05 · **Feature:** ROADMAP Deferred Intent **D1** (`ROADMAP.md:77-88`) · **Tipo:** pesquisa de escopo transversal — **ASSETS**, não especificação de feature
**Status:** nenhum código de multiplayer existe; este briefing **enumera as divergências** e **não especifica** o multiplayer. O follow-up declarado é `/speckit.specify`.
**Gating pela Constituição:** **Gate 2** ("No feature ships with a known single-player-only code path") e **Gate 3** ("Where the change touches shared farm state, the journey MUST additionally be verified with a second player connected") passam a ser **inalcançáveis** para todo o resto do roadmap até D1 fechar. Princípio VI é NON-NEGOTIABLE.
**Última verificação:** 2026-10-05 — wiki relida; assemblies do jogo inspecionados por reflexão (`.gamepath/`, SMAPI 4.5.2); suíte executada 23× (ver §8.2 — **a suíte está instável**, achado novo e relevante).

> **Trap de nomenclatura (ler antes de qualquer coisa):** `specs/future-roadmap/briefings/mobile-coop.md` é o **Mobile Coop building** (ROADMAP.md:48, pastejo rotacionado, Mid Game Year 2–3). **Não** tem relação nenhuma com multiplayer de rede. É o asset #1 que se confunde com D1. Este arquivo é o briefing de multiplayer.

---

## 1. Escopo, tese e o que este documento **não** é

### 1.1 A tese

O Constitution v2.0.0 (Princípio VI) tornou multiplayer **obrigatório**, mas o ROADMAP o classificou como "deferred intent" e **oito documentos ainda afirmam "single-player only"**. A maior parte do passivo do mod é **asset**, não lógica: overlays de tile, tooltip, feedback de enxada, sprites de mundo, banners de HUD e cues de áudio. Cada um desses tem um **alcance de entrega distinto** em multiplayer, e essa distinção — *local à tela / replicado pela rede / por-jogador* — é a única coisa que este briefing precisa fixar.

O erro de design mais caro que o mod pode cometer aqui é tratar "multiplayer" como um interruptor booleano. Não é. São **três eixos ortogonais**:

| Eixo | Pergunta | Quem decide |
|---|---|---|
| **Alcance** (quem *ouve/vê*) | local à tela, replicado pela rede, ou por-jogador? | `broadcastSprites` / `GameLocation.playSound` / `Game1.playSound` / `PerScreen<T>` |
| **Autoridade** (quem *decide*) | o dado é do host, do jogador, ou da tela? | Princípio VI: host-authoritative para estado de fazenda |
| **Persistência** (onde *sobrevive*) | save do host, modData de entidade, ou config por cliente? | `WriteSaveData` (host-only!) vs `modData` (sync) vs config local |

Um asset que resolve os três eixos corretamente em single-player pode errar **qualquer um** deles em multiplayer sem dar erro, sem exceção, sem crash — apenas com o dado errado na tela errada.

### 1.2 O que este briefing **não** é

- **Não** é a spec de multiplayer. `ROADMAP.md:82-83` é explícito: o follow-up é `/speckit.specify`, porque é capability net-new. Aqui só se decide **qual asset precisa existir a mais** e **por quê**.
- **Não** substitui o estudo de arquitetura da spec. As decisões de modelo de propriedade (host-autoritativo, resolução de divergência por tile, migração) são do `/speckit.specify`.
- **Não** é o briefing de `mobile-coop.md`. Ver o trap acima.
- **Não** fecha D3 (`ROADMAP.md:97-110`, nenhum wiring de content em jogo). D3 é pré-requisito prático de D1, e vários gates de asset aqui **não são verificáveis em jogo** até D3 fechar. Isso está registrado como dependência, não resolvido.

### 1.3 Por que a ordem importa (evidência)

`ROADMAP.md:112-115` recomenda fechar D3 antes de D1. A razão é verificável no código: **`LivingRoots/Assets/` não tem nada carregado.** Não existe `Content.Load`, `Load<Texture2D>` ou `LoadFromModFile` em `LivingRoots/` — os 3 PNGs do bin e o `.ogg` são copiados para a saída de build pelo `.csproj` e nunca lidos. Um briefing de assets de multiplayer escrito contra um mod que não desenha nada no mundo é um briefing contra o vazio. **Gate: nenhum teste in-game de duas telas deste documento é executável antes de D3.**

---

## 2. Fontes primárias (externas) — com revisão e data

Todas relidas em **2026-10-05**. Onde a wiki e o build divergem, o build ganha e a divergência é anotada.

| Fonte | Rev. | URL | Uso neste briefing |
|---|---|---|---|
| `Modding:Modder Guide/APIs/Multiplayer` | rev. oldid=190140, editada 2026-01-07 | https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer | `GetConnectedPlayers`, `GetActiveLocations`, `SendMessage`, `PerScreen<T>`, tabela `IMultiplayerPeer` |
| `Modding:Modder Guide/APIs/Events` | oldid=190140, 2026-01-07 | https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events | `RenderedWorld`, `SaveLoaded`, `DayStarted`, `PeerConnected`, `ModMessageReceived`, `Warped` |
| `Modding:Modder Guide/APIs/Utilities` | oldid=190154, 2026-01-09 | https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Utilities | tabela `Context` (`IsMultiplayer`, `IsMainPlayer`, `IsSplitScreen`, `ScreenId`, `IsOnHostComputer`) |
| `Modding:Modder Guide/APIs/Data` | oldid=190136, 2026-01-07 | https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Data | **três** storages: JSON file / `ReadSaveData` / `ReadGlobalData`; e a seção **Mod data** |
| `Modding:Audio` §Manage audio in C# | lida 2026-10-05 | https://stardewvalleywiki.com/Modding:Audio | os 4 métodos de playback e **seus alcances**; `Data/AudioChanges` |
| `Modding:Common tasks` §Banner message | rev. 2026-04-09 | https://wiki.stardewvalley.net/Modding:Common_tasks | `HUDMessage`, construtores, `noIcon`, `timeLeft`, `whatType` |
| `Modding:Objects` / `Big craftables` | rev. 2026-08-08 / 2026-01-12 | wiki | ids qualificados, `Category`, spritesheet do bin |
| `Stardew Valley Wiki — Multiplayer` | lida 2026-10-05 | https://stardewvalleywiki.com/Multiplayer | swap de estado entre jogadores locais |
| Decompilado 1.6 (`Dannode36/StardewValleyDecompiled`) | `main` | https://github.com/Dannode36/StardewValleyDecompiled | corpo de `Multiplayer.broadcastSprites`; `locationSprites = 7` |
| Decompilado 1.5.6 (`WeDias/StardewValley`) | 1.5.6 | https://github.com/WeDias/StardewValley | `FarmerTeam.OnRequestHorseWarp` — **uso vanilla de `broadcastSprites`** |
| **Assemblies do jogo em `.gamepath/`** | SMAPI **4.5.2** | local | **fonte primária de verdade** para existência de membro (ver §2.1) |

### 2.1 Método de verificação (e o que ele já derrubou)

Não confiei na wiki nem no enunciado do pedido. Escrevi um probe de reflexão (`MetadataLoadContext`, sem executar o jogo) sobre os assemblies reais em `.gamepath/` e consultei ~100 membros. **Isso já invalidou três coisas queCirculam no repo e no briefing de origem:**

| Membro afirmado em | Realidade no build | Impacto |
|---|---|---|
| `Game1.getFarmerUniqueID` (no enunciado desta tarefa, como 1º arg de `broadcastSprites`) | ❌ **não existe** | A assinatura real (§3.1) não leva farmer id nenhum. Quem "é" o jogador é implícito: `Game1.player`. |
| `Game1.ambientSound` (enunciado, como "global") | ❌ **não existe** | O mais próximo é `Game1.ambientCategory` (um `IAudioCategory`, não um método de play). Audio ambient real é `Data/LocationContexts`. |
| `GameLocation.playSoundAt(cue, tile)` (usado no decompile 1.6) | ❌ **não existe em 4.5.2** | Existe `playSound`/`localSound` com `Nullable<Vector2> position`. Cite a forma com `Nullable`, que é a que compila. |
| `Farmer.IsSplitScreen` (usado em `research-split-screen-coop.md:103`) | ❌ **não existe** | Split-screen é `Context.IsSplitScreen` / `IMultiplayerPeer.IsSplitScreen`. O research está errado aqui. |
| `Context_applied` (enunciado, como caminho de replicação) | ❌ **não existe** em SMAPI nem no jogo | `Context_applied` é evento do **Content Patcher**, não SMAPI. O caminho SMAPI equivalente é `GameLoop.SaveLoaded` + `Multiplayer.PeerConnected`. Ver §6.2. |
| `IMultiplayerHelper.GetUniqueID()` | ❌ é `GetNewID()` | — |
| `Game1.playSoundPitched` | ❌ | `Game1.playSound(cue, int? pitch)`. |
| `Farmer.MonitorName` / `getDisplayName()` | ❌ | `Farmer.displayName` / `Farmer.Name` (`NetString`, **sincronizado**). |

**Membros confirmados como existentes** (a usar com confiança): `Multiplayer.broadcastSprites(GameLocation, TemporaryAnimatedSprite[])` e `(GameLocation, TemporaryAnimatedSpriteList)`; `Game1.addHUDMessage(HUDMessage)`; `Game1.showRedMessage(string, bool)`; `Game1.getFarmer(long)`, `getOnlineFarmers()`; `Game1.viewport`, `currentCursorTile`, `instanceIndex`, `IsMasterGame`, `IsMultiplayer`; `GameLocation.localSound/playSound(string, Vector2?, int?, SoundContext)`; `GameLocation.TemporarySprites` (tipo `TemporaryAnimatedSpriteList`); `DelayedAction.playSoundAfterDelay(..., bool local)`; `TemporaryAnimatedSprite.{text,color,alphaFade,motion,interval,animationLength,layerDepth,id,local,bigCraftable,startSound,endSound,scale}`; `Item.modData`, `Farmer.modData`, `Farmer.displayName`; `Context.{IsMultiplayer,IsMainPlayer,IsSplitScreen,ScreenId,IsWorldReady,IsOnHostComputer}`; `PerScreen<T>` (namespace real: **`StardewModdingAPI.Utilities`**, não `StardewModdingAPI`); `IMultiplayerHelper.{GetNewID,GetActiveLocations,GetConnectedPlayers,GetConnectedPlayer,SendMessage}`; `IMultiplayerPeer.{PlayerID,IsHost,IsSplitScreen,ScreenID}`; `ModMessageReceivedEventArgs.{FromPlayerID,FromModID,Type}` + `ReadAs<T>()`; `IDataHelper.{ReadSaveData,WriteSaveData,ReadGlobalData,WriteGlobalData,ReadJsonFile,WriteJsonFile}`; `StardewValley.GameData.AudioCueData`.

> **Regra para o `/speckit.specify`:** nada nesta lista pode ser assumido de memória. Qualquer membro novo citado na spec tem que passar pelo probe (ou por `grep` nos assemblies) antes de virar requirement. Três anos de modding já produziram citações erradasEnough para uma regra.

---

## 3. Fontes internas (com file:line)

| Arquivo:linha | Fato que sustenta |
|---|---|
| `ROADMAP.md:77-88` | D1: status "no code, no spec, no plan"; follow-up `/speckit.specify` |
| `ROADMAP.md:84-87` | Os 8 docs que ainda dizem "single-player only" |
| `ROADMAP.md:97-110`, `:112-115` | D3 (sem wiring de content) e a recomendação de ordem D3→D1 |
| `.specify/memory/constitution.md:28-37` | Princípio VI completo, incluindo "divergence handling is explicit" |
| `.specify/memory/constitution.md:44` | "Single-player is a supported degenerate case of multiplayer, never the other way around" |
| `.specify/memory/constitution.md:56` | Gate 2: "No feature ships with a known single-player-only code path" |
| `.specify/memory/constitution.md:59` | Gate 3: verificação com segundo jogador conectado |
| `.specify/memory/constitution.md:76` | A restrição v1.x é **revogada** e não pode ser citada |
| `specs/001-soil-health-visualization/spec.md:110,147` | Tile identificado por (X,Y) "consistent with ... single-player scope" |
| `specs/001-soil-health-visualization/spec.md:178` | "Single-player game context only ... multiplayer sync is Out of Scope" |
| `specs/001-soil-health-visualization/spec.md:190` | Out of Scope: "Multiplayer synchronization of visualization settings" |
| `specs/001-soil-health-visualization/spec.md:124` (FR-009) | Toast **uma vez por evento de degradação** — o gancho de §6.3 |
| `specs/002-soil-decay-compost/spec.md:144` (FR-012) | `"+15"` **deve** usar `Game1.multiplayer.broadcastSprites` |
| `specs/002-soil-decay-compost/spec.md:214` | Contradiz a própria FR-012 ("single-player only") |
| `specs/002-soil-decay-compost/plan.md:30` | "Single-player only (no multiplayer sync)" |
| `specs/003-composting-tests/plan.md:21` | "Target Platform: ... single-player" |
| `specs/004-playable-compost-loop/spec.md:290` (SC-010) | "end-to-end live multi-client network packet transport testing remains owed" |
| `docs/design-system/REALITY-CHECK-EVALUATION.md:38-44` | Loop single-threaded; "Multiplayer scenarios where farmhand data syncs asynchronously" |
| `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md:228-231` | `PerScreen<T>` e "Farmhand data syncs asynchronously" listados como necessidade de thread-safety |
| `specs/001-soil-health-visualization/checklists/research-split-screen-coop.md:19` | "the game **swaps the entire game state** between each player" |
| `.../research-split-screen-coop.md:152-169` | Superfície de `PerScreen<T>` |
| `.../research-split-screen-coop.md:103` | **`Game1.player.IsSplitScreen()` — não existe** (ver §2.1) |
| `research-report-ui-overlays-tooltips.md:558-561` | `PerScreen<T>` como padrão de estado por jogador |
| `research-report-ui-overlays-tooltips.md:942` | Tabela de padrões de thread-safety (`PerScreen`, `Mutex`) |
| `specs/002-soil-decay-compost/briefings/compost-application.md:124-129` | **Divergência D1 já catalogada**: `location.TemporarySprites.Add` vs `broadcastSprites` |
| `specs/004-playable-compost-loop/briefings/audio-ready.md:189-223` | §6: semântica de playback multiplayer; matriz de teste manual com farmhand |
| `specs/004-playable-compost-loop/briefings/audio-ready.md:249-258` | §7.2: remoção de `Ship` como gate |
| `specs/004-playable-compost-loop/briefings/composting-bin-sheet.md:104` | "HUD vermelha reserva-se a ... colisão multiplayer" |
| `LivingRoots/Services/CompostApplicationService.cs:63-75` | **D1 viva**: `location.TemporarySprites.Add(sprite)` (local) |
| `LivingRoots/Services/CompostingBinService.cs:112` | `Game1.playSound("Ship")` na coleta |
| `LivingRoots/Services/CompostingBinService.cs:120-122` | `GetBinState` adquire lock com `Game1.player?.UniqueMultiplayerID` |
| `LivingRoots/Services/HostAtomicLockService.cs:17-36` | `TryAcquireLock` first-wins, chave `(location, tile)` → `farmerId` |
| `LivingRoots/Services/HostAtomicLockService.cs:64-77` | `GetLockHolder` — **já existe, e ninguém consome** |
| `LivingRoots/Domain/Interfaces/IHostAtomicLockService.cs:44` | Contrato de `GetLockHolder` documentado |
| `LivingRoots/Controllers/ModController.cs:782-805` | `OnRenderedWorld` — viewport **fixa** `(0,0,100,100)` |
| `LivingRoots/Controllers/ModController.cs:797-799` | Três chamadas de render por frame |
| `LivingRoots/Controllers/ModController.cs:848-882` | `OnButtonPressed`; `:876` passa `msg => Game1.showRedMessage(msg)` — **chave crua** |
| `LivingRoots/Controllers/ModController.cs:1283-1287` | Recusa de conflito → `message.compost.action-conflict` |
| `LivingRoots/Services/ModDataService.cs:55`, `:128` | `WriteJsonFile`/`ReadJsonFile` — **pasta de mod, não save** |
| `LivingRoots/Services/Visualization/OverlayRenderer.cs:53-61` | `CheckDegradation` — limiar **por cliente** |
| `LivingRoots/Services/Visualization/OverlayRenderer.cs:114-132` | `_degradationNotificationSent` — flag local |
| `LivingRoots/Services/Visualization/OverlayRenderer.cs:95-101` | Cache por `viewport` — viewport **falso** quebra o cache (§6.2) |
| `LivingRoots/Services/Visualization/VisualizationService.cs:31` | `_cursorTile` — **campo único, não `PerScreen`** |
| `LivingRoots/Services/Visualization/VisualizationService.cs:65-71` | `SetTileHealthData` substitui o dicionário inteiro |
| `LivingRoots/Services/Visualization/VisualizationService.cs:303-329` | Tooltip clampado a `Game1.viewport` (o do jogador **atual**) |
| `LivingRoots/Services/Visualization/HoeFeedbackRenderer.cs:176-180` | `TileToScreenPosition` **ignora o viewport** |
| `LivingRoots/Services/Visualization/VisualizationConfigurationService.cs:16` | Prefixo `visualization_config_` — chaveado por save, **não por jogador** |
| `LivingRoots/Domain/ISoilHealthService.cs:7` | `OnTileHealthChanged` — evento **in-process** |
| `LivingRoots/Services/SoilHealthService.cs:11` | Implementação; `CS0067` = **nunca levantado** (aviso de build) |
| `LivingRoots/Constants.cs:35-48` | Constantes de viz (TileSize 64, threshold 1000, PatternMinOpacity 0.7) |
| `LivingRoots/Constants.cs:76-77` | `ModDataInputIdKey`, `ModDataDaysRemainingKey` |
| `LivingRoots/Domain/Models/CompostingBinStateModel.cs:44-100` | `PopulateFromModData` / `WriteToModData` |
| `LivingRoots/manifest.json` | Sem `Dependencies` → **sem GMCM** |
| `LivingRoots.Tests/MultiplayerConcurrencyTests.cs:147-220` | Testes de day-advance com 2 e 4 jogadores |
| `LivingRoots.Tests/MultiplayerConcurrencyTests.cs:271-339` | First-wins com zero perda de item |
| `LivingRoots.Tests/Invariants/CompostingMachineInvariantsTests.cs` | Invariantes de máquina (sem rede) |
| `LivingRoots.Tests/TestHelpers/ThreadSafeGameLoopEventsStub.cs` | Stub de `IGameLoopEvents` — **não implementa `IMultiplayerEvents`** |
| `LivingRoots.Tests/Stubs/MultiplayerFarmerStub.cs:286` | `AsyncLocal<MultiplayerFarmerStub?>` para farmer ambiente |
| `LivingRoots.Tests/Stubs/MultiplayerFarmerStub.cs:504-520` | `Barrier` + `TaskCompletionSource` para "simultaneidade" |

### 3.1 Contradições documentadas que D1 **deve** reconciliar (não duplicar)

Estas já existem em specs e briefings. Este briefing **as cataloga com uma coluna nova** (o asset que cada uma exige) e **não repete a prosa**:

| # | Documento | Afirma | Realidade |
|---|---|---|---|
| C1 | `spec 001:178` + `spec 001:190` | "single-player only"; sync de viz é Out of Scope | Princípio VI (`constitution.md:44`) diz o oposto. **Revogar.** |
| C2 | `spec 001:110`, `:147` | Tile = (X,Y) "consistent with single-player scope" | A *identidade* do tile continua (X,Y) — mas o tile deixa de ser privado. Ver §6.2. |
| C3 | `spec 002:144` (FR-012) | `+15` via `broadcastSprites` | Correto e **ainda não implementado** (`CompostApplicationService.cs:75`). |
| C4 | `spec 002:214` | "single-player only ... all state is local per save file" | Contradiz C3 **dentro da própria spec 002**. |
| C5 | `spec 002/plan.md:30`, `spec 003/plan.md:21` | "single-player" como Target Platform | Contradiz `constitution.md:44`. |
| C6 | `docs/.../REALITY-CHECK-EVALUATION.md:40` | Thread safety desnecessária "for single-player" | O próprio doc já admite a exceção multiplayer na linha seguinte. |
| C7 | `docs/.../DESIGN-SYSTEM.md:228-231` | `PerScreen<T>` + "Farmhand data syncs asynchronously" | **Já está correto** — o design system está *à frente* das specs. Nenhuma ação além de citar. |
| C8 | `spec 004:290` (SC-010) | "verified by automated multi-threaded test stubs" | Verdade **parcial** — e §8.2 mostra que os stubs são instáveis (8/10 execuções) e não cobrem transporte. |
| C9 | `spec 001 research-split-screen-coop.md:103` | `Game1.player.IsSplitScreen()` | **API inexistente** (§2.1). Corrigir o research ao tocar nele. |

---

## 4. Tabela de divergências — o coração do briefing

**Legenda de severidade:** 🔴 **P0** = viola Princípio VI agora (dado errado na tela errada, ou estado duplicado) · 🟠 **P1** = quebra a experiência de um jogador, mas é recuperável · 🟡 **P2** = debt/qualidade.

| # | Asset | Single-player | Multiplayer | Asset extra necessário | Sev |
|---|---|---|---|---|---|
| A1 | `+15` flutuante (`CompostApplicationService.cs:63-75`) | `location.TemporarySprites.Add` — só a tela local | `Game1.multiplayer.broadcastSprites(location, sprite)` replica para quem está na location. **O farm hand que aplicou vê; os outros não.** | Nenhum sprite novo — **trocar o canal de entrega**. Confirmar `sprite.id` estável para o fade não duplicar | 🔴 |
| A2 | Overlay de tile (`OverlayRenderer` + `OnRenderedWorld`) | `SetTileHealthData` alimentado localmente | O `Dictionary<Point,float>` é **do processo host**. O cliente não tem os dados → FR-014 pinta cinza #808080 em toda a fazenda. O host vê verde; o farm hand vê um cemitério cinza. | **1 replication channel** (tile→health), + decidir se a fazenda inteira é replicada ou só o viewport | 🔴 |
| A3 | `_degradationNotificationSent` (`OverlayRenderer.cs:29,117-124`) | 1 toast por evento | `_monitor.Log` por cliente. O aviso é **por viewport**, então host e farm hand degradam em frames diferentes. | Toast **por cliente, sobre a própria viewport** — a inaccessible de um não é a do outro | 🟠 |
| A4 | `checkDegradation` (limiar 1000) | 1 decisão | **Divergência visual entre clientes**: host tem 400 tiles visíveis (padrões ON), farm hand tem 1200 (padrões OFF). Dois "% de saúde" visualmente diferentes para a mesma fazenda. | Nenhum asset novo. **Reclamação de decisão:** degradação deve ser por **viewport do cliente** (correto, defensável) **declarada na spec** — senão vira bug report | 🟠 |
| A5 | Bin state: sprite de frame (`SpriteIndex`/`ShowNextIndex*`) | `Data/BigCraftables` desenha local | `Object.bigCraftable` é `NetBool` e o frame deriva de `readyForHarvest`/`lastOutputRuleId` (**todos `Net*`, verificados**). O frame **é replicado de graça** pelo `locationDelta`. | Nenhum — se D3 usar `Data/Machines` de verdade. **Hoje não**: `ModEntry.cs:152-176` não seta `ShowNextIndex*`, então não há frame de ready | 🟠 |
| A6 | Cue de coleta (`"Ship"`, `CompostingBinService.cs:112`) | `Game1.playSound` — local, instantâneo | Host coleta → **só o host ouve**. O farm hand vê o bin virar vazio sem feedback sonoro. E o cue está errado em semântica (ver `audio-ready.md` §7.2). | **Nenhum asset novo**; troca de método → `GameLocation.playSound` (todos na location) | 🟠 |
| A7 | Som de ready no `ProcessDayStart` | `Game1.playSound` no day-start | O day-start roda no **host**. O som não viaja. O farm hand nunca sabe que o bin ficou pronto. | Candidato a `startSound` do `Data/Machines` (replicado com o state delta) **ou** gatilho reativo na entrada de location | 🟠 |
| A8 | `showRedMessage` de recusa (`ModController.cs:876`) | Banner vermelho no cliente | Banner **é player-local** (por desenho: `Game1.hudMessages` é um `List` do cliente, sem `Net*`). Correto — cada um vê a própria recusa. **Mas** a chave crua `"message.compost.action-conflict"` é exibida literalmente (bug i18n pré-existente, agora visível por 2 jogadores). | Nenhum asset novo; **seam `IMessageHud`** para i18n + teste | 🟡 |
| A9 | Indicador de "outro jogador está aqui" | Inexistente | `HostAtomicLockService.GetLockHolder` **já existe** (`HostAtomicLockService.cs:64-77`) e **ninguém consome**. Sem ele, o farm hand clica, leva `cancel`, e não descobre *quem* bloqueou. | **1 nameplate/badge no tile travado** — 1 sprite + 1 lookup `Game1.getFarmer(lockHolder)` | 🟠 |
| A10 | `HoeFeedbackRenderer.TileToScreenPosition` (`:176-180`) | `tile * 64`, sem viewport | Usa coordenadas **absolutas**, não relativas ao viewport. Em split-screen o flash sai na posição errada. O overlay tem o mesmo bug em `VisualizationService.DrawOverlay:228-229`. | Correção, não asset. Mas **quebra em 2 dos 2 renderers** | 🔴 |
| A11 | `_cursorTile` (`VisualizationService.cs:31`) | 1 cursor | Split-screen: `UpdateCursorTile` (via `OnButtonReleased`) e `RenderTooltip` sãocalled em telas alternadas → **tooltip do jogador A renderiza com o cursor do jogador B**. | Mover para `PerScreen<Point>`. Nenhum asset novo | 🔴 |
| A12 | Tooltip clampado a `Game1.viewport` (`:322-329`) | 1 viewport | Em split-screen, o clamp usa o viewport do jogador **atual** — correto por acidente, desde que A11 seja consertado. | Nenhum | 🟡 |
| A13 | Cache de overlay por viewport (`OverlayRenderer.cs:95-101`) | 1 viewport | `ModController.cs:795` passa viewport **constante** `(0,0,100,100)`. Em split-screen, o jogador 2 com viewport ≠ jogador 1 **reusa o cache errado** — overlays no lugar errado. | Nenhum asset; **consumir `Game1.viewport` de verdade** | 🔴 |
| A14 | Config de viz (`VisualizationConfigurationService.cs:16`) | Chave `visualization_config_{saveId}` | **Farmado por save, não por jogador.** O farm hand carrega o save **do host** e grava por cima no `Saving` dele (`ModController.cs:750-779`) — o `Saving` **é levantado em farm hands** (confirmado na wiki). Duas janelas, um arquivo, last-write-wins. | Nenhum asset; **decisão de política de config** (§6.3) | 🔴 |
| A15 | `modData` do bin no item (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`) | Round-trip local | `modData` **é sincronizado** (wiki Data §Mod data: "persisted to the save file and synchronized in multiplayer"). E `Object` tem ~35 campos `Net*` — mas `modData` **não** é `Net*`: é serializado no save. Dois jogadores pegando o mesmo bin obtêm **cópias idênticas legítimas** do mesmo lote. | Nenhum asset. **Mas a regra de divergência tem de ser escrita** (§6.7) | 🟠 |
| A16 | GMCM / config in-game | `manifest.json` sem `Dependencies` | **GMCM não está instalado.** Nenhum cliente tem UI de config. A pergunta "o config do farm hand não pode bifurcar" é hoje **moot** — não há UI, só o console e o arquivo. | Nenhum asset; se GMCM entrar, é **a** superfície de config-por-jogador | 🟡 |
| A17 | Split-screen: `RenderedWorld` por tela | 1 chamada/frame | Com 4 jogadores locais, `UpdateTicked` dispara 4×/tick (wiki Multiplayer). `OnRenderedWorld` roda 4×/frame. O overlay é **culled 4×** com 4 viewports — correto, mas 4× o custo. O `_degradationNotificationSent` é **1 flag para 4 telas**. | Nenhum asset; **`PerScreen<T>` para o counter** | 🟠 |
| A18 | `OnTileHealthChanged` (`ISoilHealthService.cs:7`) | In-process | `CS0067` = **nunca levantado** em produção. `spec 004:290` já fala em "simulated client listener reception" — mas **não existe listener algum**. O canal de invalidação que SC-010 descreve está **escrito no spec, ausente no código**. | Nenhum asset; é o **seam** que A2/A4 dependem | 🔴 |
| A19 | `SetTileHealthData` (`VisualizationService.cs:65`) | Chamado por `ModController.cs:797` com um dicionário **vazio** (nenhum caller em produção — só `VisualizationServiceOverlayTests.cs:34`) | Idem, mais grave: o overlay não tem dados **em single-player também**. Não é divergência multiplayer — é uma lacuna de wiring que D1 vai tropeçar na hora de verificar. | Nenhum asset. **Bloqueia a verificação visual de A2** | 🔴 |

**Leitura da tabela:** 🔴 = 8 linhas. Quase nenhuma pede arte nova — pedem **troca de canal de entrega** ou **consumir uma API que já existe**. Isso é a boa notícia: D1, no escopo de assets, é majoritariamente um trabalho de *seam*, não de pipeline de arte. A exceção real é A9 (1 nameplate), e ela é pequena e de alta legibilidade.

**A19 merece ênfase:** a tabela inteira de divergências multiplayer pressupõe que existe algo renderizando hoje. **Não existe.** Nenhum caller de produção alimenta o cache de tile. Isto é D3-adjacente, mas **não** é D3 (que é sobre registro de item), e por isso é fácil de perder: um briefing de multiplayer que assuma "o overlay já funciona, só precisa sincronizar" descreveria um sistema que ainda não tem entrada. **A spec de D1 deve tratar A19 como pré-requisito de verificação, não como bug de multiplayer.**

---

## 5. Asset inventory — o que existe e o que falta

### 5.1 O que existe hoje (e está unwired)

| Caminho | Tipo | Tamanho | Consumido por C#? | Estado |
|---|---|---|---|---|
| `Assets/compost.png` | sprite 16×16 | — | ❌ | placeholder, briefing `compost-icon.png.md` |
| `Assets/composting_bin_empty.png` | sprite 16×32 | — | ❌ | placeholder |
| `Assets/composting_bin_processing.png` | sprite 16×32 | — | ❌ | placeholder |
| `Assets/composting_bin_ready.png` | sprite 16×32 | — | ❌ | placeholder |
| `Assets/Audio/composting_bin_ready.ogg` | áudio | **24 bytes** (stub) | ❌ | falha o gate de `audio-ready.md` §7.1 por ~3 ordens de grandeza |
| `Assets/Audio/composting_bin_processing.ogg` | áudio | stub | ❌ | idem |
| Textura branca 1×1 (`VisualizationTextures.WhiteTexture`) | runtime | gerada | ✅ | criada em `ModController.cs:790-792` |
| Ícones de HUD | vanilla `Game1.mouseCursors` | — | ❌ | não usados |

**Nenhum arquivo de `Assets/` é carregado por código.** A textura branca é gerada em runtime; o resto é copiado pelo `.csproj` e nunca lido. Isso é D3, não D1 — mas significa que **toda** a coluna "asset" desta tabela é hoje teórica.

### 5.2 O que D1 exige que **não** existe

| # | Novo asset | Tipo | Justificativa | Sev |
|---|---|---|---|---|
| N1 | Badge de tile travado (outro jogador ativo) | sprite 16×16 + nameplate de texto | Materializa `GetLockHolder` (A9). Sem ele a recusa é opaca. | 🟠 |
| N2 | `Data/AudioChanges` handler | código, não arte | `StardewValley.GameData.AudioCueData` (confirmado) — sem isso nenhum cue `LivingRoots_*` existe. | 🟠 |
| N3 | Canal de replicação de tile→health | código (SMAPI `SendMessage`/`ModMessageReceived`) | A2. **O asset de maior valor do briefing.** | 🔴 |
| N4 | `PerScreen` para cursor + cache + counter de degradação | código | A11, A13, A17. | 🔴 |
| N5 | `IMessageHud` seam | código | A8 — torna recusa testável e traduzível. Precedente: `IPlayerInventory` em `CompostingBinService.cs:18`. | 🟡 |
| N6 | Decisão documentada: config por jogador | docs | A14. | 🔴 |

**N1 é o único item que é genuinamente arte nova.** Estimate: 1 frame 16×16 (cadeado/badge) + 1 nameplate reutilizando `Game1.smallFont`. Nenhum pipeline de conteúdo necessário.

---

## 6. As 9 perguntas, respondidas

### 6.1 Broadcast vs local sprite insertion — quais dos nossos sprites precisam trocar, e por quê

**Resposta: exatamente um — e ele já é obrigado por uma FR existente.**

A assinatura real (verificada por reflexão, não de memória):

```csharp
// StardewValley.Multiplayer — ambos overloads existem, ambos virtual
public virtual void broadcastSprites(GameLocation location, params TemporaryAnimatedSprite[] sprites);
public virtual void broadcastSprites(GameLocation location, TemporaryAnimatedSpriteList sprites);
```

O prompt de origem sugeriu `broadcastSprites(Game1.getFarmerUniqueID, TemporaryAnimatedSprite)`. **Isso está errado em dois eixos:** não existe `getFarmerUniqueID`, e o overload não leva farmer id — a autoria é implícita em `Game1.player`. O corpo do decompile 1.6 é explícito sobre o que muda:

```csharp
// Dannode36/StardewValleyDecompiled — Multiplayer.cs
public virtual void broadcastSprites(GameLocation location, params TemporaryAnimatedSprite[] sprites) {
    location.temporarySprites.AddRange(sprites);   // ← o host SEMPRE adiciona local
    if (sprites.Length == 0 || !Game1.IsMultiplayer) return;
    ... broadcastLocationBytes(location, 7 /* locationSprites */, ...);
}
public virtual void broadcastLocationMessage(GameLocation loc, OutgoingMessage message) {
    if (Game1.IsClient) { Game1.client.sendMessage(message); return; }
    if (isAlwaysActiveLocation(loc)) { foreach (var f in Game1.otherFarmers.Values) TellFarmer(f); return; }
    foreach (Farmer farmer in loc.farmers) TellFarmer(farmer);          // ← só quem está NA location
    foreach (Building b in loc.buildings) { var indoors = b.GetIndoors(); if (indoors != null) foreach (var f in indoors.farmers) TellFarmer(f); }
    void TellFarmer(Farmer f) { if (f != Game1.player) Game1.server.sendMessage(f.UniqueMultiplayerID, message); }
}
```

Três consequências que a spec precisa saber:

1. **É auto-idempotente em single-player.** `AddRange` roda sempre; a saída de rede só acontece com `Game1.IsMultiplayer`. Então a troca é **segura por construção** — não precisa de `if (Context.IsMultiplayer)`. Isso é raro emerece praise explícito na spec.
2. **O alcance é `loc.farmers` + interiores dos buildings** — quem está *naquela location*. Um farm hand no Waldorf Academy **não** vê o `+15` da fazenda. Isso está correto (é como o jogo entrega qualquer sprite temporário) e deve ser **declarado** para que ninguém leia como bug.
3. **O caller é quem importa.** `broadcastLocationMessage` tem `if (Game1.IsClient) { Game1.client.sendMessage(...) }` — o cliente **encaminha para o host**, que retransmite. Ou seja: o farm hand que aplica composto **não pode** simplesmente chamar `broadcastSprites` esperando que o host faça o broadcast. O caminho é: cliente manda mensagem ao host → host chama `broadcastSprites` → host retransmite para todos. **Isto é uma assimetria de arquitetura, não um detalhe de asset** — é aforma mais barata deatisfazer Princípio VI sem duplicar sprites.

**Uso vanilla como precedente** (`FarmerTeam.OnRequestHorseWarp`, decompile 1.5.6): 8 sprites de faísca + 3 de varinha, cada um `broadcastSprites(currentLocation, new TemporaryAnimatedSprite(...))`, guardados por `if (!Game1.IsMasterGame) return;`. É o mesmo padrão que precisamos — **só o host transmite**.

| Sprite nosso | Local hoje | Precisa broadcast? | Racional |
|---|---|---|---|
| `+15` (`+15` flutuante) | `CompostApplicationService.cs:75` | **Sim** — FR-012 já exige | É reação a uma ação de fazenda observável por todos. Sem isso, o outro jogador vê o solo mudar sem explicação. |
| Flash de enxada (300ms) | `SpriteBatch.Draw` em `HoeFeedbackRenderer` | **Não** | É feedback da **ação da sua própria mão**, e é desenhado direto no `SpriteBatch`, não via `TemporarySprites`. Escopo local é o comportamento correto — o outro jogador vê a enxada funcionando. |
| Texto flutuante de enxada | idem | **Não** | idem |
| Overlay de tile | `SpriteBatch.Draw` | **Não** — mas precisa de **dados** | Não é um sprite de mundo; é re-derivado do estado. O caminho de dados é N3, não broadcast. |
| Smoke/vapor do bin | `Data/Machines.WorkingEffects` | **A definir** | Se vier de `WorkingEffects` vanilla, a engine replica junto com o state delta. Se for `TemporarySprites` nosso, precisa de broadcast. `composting-bin-sheet.md:259` recomenda vapor vanilla — o que, incidentalmente, resolve A5 de graça. |
| Debris de item (`createItemDebris`) | `ModController.cs:1235` | **Não** | Vanilla já sincroniza debris; `Game1.createItemDebris` → `World.DebrisListChanged`. |

### 6.2 Tile overlays — o que o cliente renderiza, e por qual caminho o cache é replicado

**Resposta: hoje, o cliente renderiza cinza em toda a fazenda. E nenhum dos dois caminhos sugeridos no prompt funciona como descrito.**

Primeiro, o estado do código:

- `VisualizationService.SetTileHealthData` (`:65-71`) recebe o `Dictionary<Point,float>` **inteiro** e o substitui. **Verificado por `grep` em `LivingRoots/` e `LivingRoots.Tests/`: o único caller é o teste** (`VisualizationServiceOverlayTests.cs:34`). Não existe nenhum consumidor em produção.

Isso é mais grave do que "o cliente vê cinza" (A2): em **single-player** o overlay também não tem dados, porque **ninguém chama `SetTileHealthData`**. `ModController.OnRenderedWorld` (`:797`) chama `RenderOverlays` com um dicionário vazio, que `OverlayRenderer.cs:89-92` trata como no-op por FR-021. O passivo de assets do §4 está, hoje, em **zero assets visíveis em jogo** — e isso não é D3, é uma lacuna de wiring distinta que a spec de D1 vai encontrar na hora de verificar. Registrado como A19 abaixo.

Segundo, os dois caminhos propostos:

| Caminho proposto | Veredicto |
|---|---|
| `ContextApplied` + invalidação `Data/...` | ❌ **`Context_applied` não existe em SMAPI.** É evento do Content Patcher. E `Data/...` não é onde a saúde mora — a saúde mora num JSON em `data/{key}.json` (ver `ModDataService.cs:55`). Invalidation de asset não invalida nada nosso. |
| Eventos `Game1.multiplayer` | ⚠️ **parcialmente certo, mas não como "eventos".** `Game1.multiplayer` não emite eventos para mods. O que existe é `SMAPI: Multiplayer.ModMessageReceived` — um canal **pull** que o mod usa para trocar mensagens, não um fluxo de dados de estado. |

O caminho real, em três partes:

**(a) Quando popular o cache.** SMAPI: `GameLoop.SaveLoaded` — "Raised after loading a save (including the first day after creating a new save), **or connecting to a multiplayer world**". É o gancho de replicação que `Context_applied` teria sido em outro framework. Complementar: `Multiplayer.PeerConnected` (um farm hand que conecta **depois** de o save ter carregado precisa de um catch-up).

**(b) O que replicar.** A saúde é um mapa esparso de 0–100 por location. Duas opções, e a escolha é da spec, não deste briefing:

| Opção | Payload | Custo | Risco |
|---|---|---|---|
| **Delta** | só tiles alterados, via `SendMessage` | baixo | precisa de-lossless ordering; `spec 004:290` já promete "zero polling lag" (≤16.67ms) |
| **Snapshot** | mapa inteiro no `PeerConnected` | ~30k tiles máx. (`ModConstants.MaxTilesPerSave`) | grande, mas **simples e aditivo**; e 1× por conexão, não por ação |

Não cabe a este briefing recomendar: **delta durante a sessão, snapshot no join**. É o que respeita `SC-010` sem transformar cada aplicação de composto num broadcast de mapa.

**(c) Quando invalidar.** `InvalidateCache` é local e hoje **nunca é chamado de produção** — `ISoilHealthService.OnTileHealthChanged` tem `CS0067` (nunca levantado), ou seja **não existe consumidor**. A spec 004:290 fala em "simulated client listener reception of `OnTileHealthChanged` triggering immediate cache invalidation". **Isso não existe em lugar nenhum do repo.** A18 é esse buraco.

E há um bug que precede a sincronização: `ModController.cs:795` passa `var viewport = new Rectangle(0, 0, 100, 100)` — uma constante. `OverlayRenderer.cs:95-101` usa o viewport como **chave de cache**. Então o cache nunca invalida por movimento de câmera em single-player (funciona por acidente, porque a chave é constante), e em split-screen os jogadores 2..4 herdam um cache construído com o viewport do jogador 1. **Corrigir A13 é pré-requisito de A2**, não consequência.

### 6.3 HUD degradation notification — deve espaçar todo cliente, ou não?

**Resposta: deve, mas a *razão* da spec está errada, e por isso a conclusão vira o contrário do que FR-009 pede.**

FR-009 (`spec 001:124`) exige: "MUST display a **one-time** toast notification to the player". A spec assume **um** jogador. Com dois, "one-time" deixa de ser bem definido, porque:

- `OverlayRenderer._degradationNotificationSent` (`:29`) é uma **flag de instância única** por serviço. O serviço é singleton por composition root → **1 flag para N clientes**.
- A degradação é função da **viewport local** (`CullToViewport`), não da fazenda. Host olhando para a horta vê 400 tiles; farm hand na mina vê 1200. Eles degradam em **momentos diferentes**, e isso está **correto**.

**A decisão:**

1. **Cada cliente notifica sobre a sua própria viewport.** É semanticamente o que FR-009 quer ("notify the user when their accessibility aids are reduced") — WCAG 1.4.1 é sobre o *usuário afetado*, e o usuário afetado é quem tem a tela degradada. Um host com 300 tiles não deve ser avisado de que os padrões foram desligados, porque não foram.
2. **`Game1.addHUDMessage` é o canal certo** — é player-local por construção (`Game1.hudMessages` é um `List` sem `Net*`), então **cada cliente mostra o seu sem duplicar para o outro**. Nenhuma replicação é necessária. Isto é uma boa notícia e deve ser dito na spec para evitar que alguém "conserte" isto com um broadcast.
3. **Mas a flag tem de ser por tela.** Em split-screen (A17), uma flag única para 4 telas significa que o jogador 3 degrada em silêncio se o jogador 1 já degradou. `PerScreen<bool>` resolve — e a pesquisa do próprio repo já endorsa `PerScreen<T>` (`research-split-screen-coop.md:152`, `research-report-ui-overlays-tooltips.md:558-561`).
4. **A spec precisa declarar que a degradação pode divergir entre clientes** (A4) e que isso é **aceito**, com a justificativa de viewport-local. Sem essa linha, qualquer jogador que reportar "no meu ecrã não há padrões" gera bug. Isto é a divergência que a Princípio VI exige que seja explícita.

Nota lateral: a spec diz "toast". O veículo vanilla mais próximo é `Game1.addHUDMessage(new HUDMessage(text, HUDMessage.error_type))` (wiki Common tasks §Banner message), que dá ícone. Para um aviso de acessibilidade, `noIcon = true` lê melhor e não compete com o ícone de erro — que é o canal reservado para recusa de ação (A8, `composting-bin-sheet.md:104`: "HUD vermelha reserva-se a ... colisão multiplayer"). **Decisão de design a levar para a spec, não um default meu.**

### 6.4 Refusal messages — precisa de indicador de lock por jogador, ou de nameplate "quem está fazendo"?

**Resposta: nameplate. O indicador de lock sozinho é insuficiente, e a boa notícia é que a API já existe e está documentada.**

O que existe: `IHostAtomicLockService.GetLockHolder(location, tile) → long?` (`IHostAtomicLockService.cs:44`), implementado em `HostAtomicLockService.cs:64-77`. É uma API **feita exatamente para isto** — devolve o `farmerId` do dono do lock. E `Game1.getFarmer(long) → Farmer` (verificado) transforma o id em um `Farmer`, cujo `displayName` é um `NetString` (**sincronizado** — confirmado na reflexão: `Farmer.name : NetString`).

**Por que nameplate e não só um ícone de cadeado:** um cadeado diz "ocupado". Um nome diz "ocupado **pelo Alex**". Em uma fazenda com 2–8 pessoas, a segunda informação é a que evita a próxima tentativa e a que sustenta o senso de exploração compartilhada que é a tese do mod. E o dado de custo é zero — a lock table já guarda o id.

**Onde a nameplate vive:** **não** no tile overlay (que é por-client e stateless), mas em um sprite de mundo com `broadcastSprites` — porque é informação de **estado compartilhado**, e todos na location precisam vê-la. É o mesmo canal de A1.

**Ciclo de vida (o ponto que a spec precisa fixar):** o lock é **transiente** — `TryInteractWithBin` adquire e libera em `finally` (`ModController.cs:1289-1320`). Ou seja: no caminho de código atual, o lock dura **microsegundos**, e uma nameplate keyed em `IsLocked` piscaria e nunca apareceria.

Isso é uma **descoberta de design, não só de asset**, e muda a pergunta: a nameplate não pode ser "o lock está preso" (transiente), tem que ser "**esta ação está em andamento**" (uma janela de interesse). Duas saídas, para a spec:

| Opção | Semântica | Custo |
|---|---|---|
| **Estender a janela do lock** | O lock cobre a interação inteira (inclusive o `playSound` e a escrita de modData) | Muda semântica de `IHostAtomicLockService`; `GetBinState` (`CompostingBinService.cs:120-122`) **já** adquire lock, então há risco de reentrada a resolver |
| **Estado de interesse separado** | Um segundo mapa `tile → (farmerId, expiry)` que o `TryAcquireLock` escreve e um timer limpa | Novo estado, nova expiração, mas **não** interfere na semântica de first-wins já testada |

A segunda é mais segura e mais barata de testar. **A spec escolhe; este briefing registra que a escolha precisa existir** — sem ela, A9 é irreduzível.

**Sobre a mensagem em si:** `message.compost.action-conflict` está em `i18n/default.json:11` e hoje **não é traduzida** — `ModController.cs:876` passa a chave crua para `showRedMessage`, que exibe literalmente. Isto já é bug em single-player; com 2 jogadores vira bug **por jogador**. O conserto é a seam N5.

### 6.5 Per-farmer cursor / attribution badges — o que o farm hand precisa ver

**Resposta: o farm hand precisa de duas coisas, e uma delas já está errada no código.**

**O que precisa:**
1. **O cursor dele mesmo** — para saber em que tile ele está agindo. Isso é `PerScreen<Point>` (A11). Hoje `_cursorTile` é um campo único (`:31`) e `UpdateCursorTile`/`RenderTooltip` são called em telas alternadas → **tooltip do jogador A com o cursor do jogador B**. Não é só feio: é informação errada sobre a fazenda, num HUD que o jogador lê para decidir.
2. **O cursor do outro** — opcional, e YAGNI por ora. A nameplate de N1 (A9) cobre o caso que importa ("não age neste tile"). Nameplates de cursor para todos os jogadores seria clutter e custo de upkeep.

**O que a spec NÃO deve fazer:** transformar o overlay em "atribuição" (marcar de quem é o tile). Solo é do **fazenda**, não do jogador (Princípio VI: "soil health is a property of the *farm*"). Atribuir tiles a jogadores seria uma violação conceitual, não uma melhoria de UX.

**Sobre `PerScreen<T>`** — corrigindo o research: o tipo real é **`StardewModdingAPI.Utilities.PerScreen<T>`** (verificado; `research-split-screen-coop.md:152` não dá o namespace, e minha primeira sondagem por `StardewModdingAPI.PerScreen\`1` retornou `MISSING-TYPE` — quem procurar sem o namespace vai achar que ele não existe). A regra da wiki: campos **`readonly`**, e sobrescrever o campo inteiro em vez de `.Value` limpa o dado de **todos** os jogadores. Com 3 instâncias a mover (`Point` de cursor, cache de overlay, flag de degradação), os 3 campos devem ser `readonly PerScreen<T>` desde o primeiro commit.

**`Game1.player.IsSplitScreen()` não existe** (`research-split-screen-coop.md:103`). O correto é `Context.IsSplitScreen` (SMAPI) ou `IMultiplayerPeer.IsSplitScreen`. Quem copiar aquele snippet recebe um erro de compilação — o que é, na verdade, uma boa notícia: quebra alto e cedo.

### 6.6 Split-screen — o que quebra, o que é culled duas vezes

**Resposta: o culling está correto por construção; o *cache* e as *flags* é que não estão.**

O mechanism (confirmado em `research-split-screen-coop.md:19` e na wiki): *"Split-screen multiplayer swaps the entire game state between each player"*. Com 4 jogadores locais, `UpdateTicked` dispara 4× por tick.

| Item | Comportamento em split-screen | Veredicto |
|---|---|---|
| `RenderedWorld` | dispara 1× por **tela** por frame = 4×/frame | Correto. É o gancho certo (a pesquisa recomenda, `:93`) |
| `CullToViewport` (`OverlayRenderer.cs:165-177`) | roda 4×, cada uma com o viewport do jogador atual | **Correto.** Cada tela vê o que está no seu viewport. É o comportamento desejado, não um bug |
| Cache de overlay (`:95-101`) | 4 chamadas, **1 chave** (a constante `(0,0,100,100)`) | 🔴 **Bug** — jogadores 2–4 herdam o cache do jogador 1. A13 |
| `_cursorTile` (`:31`) | 4 escritas, 1 campo | 🔴 **Bug** — tooltip cruzado. A11 |
| `_degradationNotificationSent` (`:29`) | 4 telas, 1 flag | 🟠 **Bug** — 3 telas degradam em silêncio. A17 |
| `VisualizationTextures.WhiteTexture` | `static`, criado 1× | Correto (GPU resource, não precisa ser por tela) |
| `HoeFeedbackRenderer._whiteTexture` | instância própria, `Game1.graphics.GraphicsDevice` | ⚠️ Criar `Texture2D` em split-screen pode falhar se o device não estiver pronto no 1º tick. Fora do escopo D1, mas registrar |
| `DrawOverlay` (`VisualizationService.cs:228-229`) | converte `tile → screen` **sem subtrair viewport** | 🔴 **Bug** — mesma classe de A10 |
| `DrawTooltip` clamp (`:322-329`) | usa `Game1.viewport` | 🟡 Correto por acidente, **se** A11 for consertado |
| `Game1.playSound` | local — correto para UI | Correto |

**"O que é culled duas vezes?"** — em split-screen de 2 jogadores com viewports sobrepostos, um tile visível para os dois é **culled duas vezes** e desenhado duas vezes, o que é **o comportamento correto** (cada tela precisa do seu draw). O que **não** pode é o resultado ser *compartilhado*: e é exatamente isso que o cache de chave-constante faz. Ou seja: **culling duplicado é correto; cache compartilhado entre viewports é o defeito.** A distinção é o ponto inteiro de A13.

### 6.7 Item `modData` no big craftable — é sincronizado, e o que acontece se dois jogadores pegam o mesmo bin

**Resposta: sim, `modData` é sincronizado, e a consequência é que o bug aqui não é duplicação — é o oposto.**

Fatos verificados:
- Wiki Data §Mod data: `modData` existe em NPC, Character, GameLocation, Item e TerrainFeature, e é *"persisted to the save file and **synchronized in multiplayer**"*.
- Por reflexão: `Item.modData`, `Farmer.modData`, `TerrainFeature.modData` são todos `ModDataDictionary`. **Não** são `Net*` — a sincronização vem do **save** (host → save → clients), não de um delta ao vivo.
- `StardewValley.Object` tem ~35 campos `Net*` (`bigCraftable`, `readyForHarvest`, `lastInputRuleId`, `minutesUntilReady`, …) — o **estado da máquina** é replicado ao vivo por `locationDelta`. `modData` não está nessa lista.

**Consequência 1 — o round-trip do bin funciona, e já é um feature.** `TryDislodgeBin` (`ModController.cs:1187-1188`) preserva o progresso em `modData` e, se o inventário está cheio, **dropa o bin como debris com o `modData` intacto**. Como `modData` é sincronizado e debris é sincronizado, **o lote de compostagem sobrevive a uma travessia de tela entre jogadores.** Isso funciona. Nenhum asset novo.

**Consequência 2 — dois jogadores pegando o mesmo bin NÃO é uma corrida.** Se host e farm hand obtiverem ambos o bin com o mesmo `modData`, ambos têm uma cópia **legítima** do mesmo estado. Não há duplicação de item (o `Object` subjacente é um só, replicado), e não há perda: o `DaysRemaining` é idêntico nos dois lados. O que existe é **duas representações de UI do mesmo objeto** — e aqui entra o asset: se o `Data/Machines` desenhar frames por estado, os dois clientes mostram o mesmo frame (o estado é `Net*`). Consistente. 

**Consequência 3 — a divergência real é temporal, não de cópia.** O `modData` reflete **o estado no momento do último save**, não ao vivo. Se o host aplica composto, coleta de um bin, e o dia **não** acaba antes do cliente reconectar, o `modData` do item no cliente pode estar **atrasado** em relação ao `Data/Machines` state (que é ao vivo). Ou seja: **o mesmo objeto pode render frames diferentes em dois clientes durante a janela antes do save.**

**Isto é a divergência que a Princípio VI exige que a spec declare por escrito** ("Silent last-write-wins on farm tiles is not acceptable without a documented rationale"), e a resposta honesta é:

> O `modData` de um big craftable é **eventual-consistency bounded by the save cycle**. O estado ao vivo (`Data/Machines`, `Net*`) é a fonte de verdade para **renderização**; o `modData` é a fonte de verdade para **persistência de posse** (o que acontece se o jogador levar o bin embora). Divergência é esperada entre um save e o próximo, é limitada por `DaysRemaining` (2 dias), e **não** pode causar duplicação nem perda — apenas um frame possivelmente desatualizado se um cliente reconectar no meio do dia. Aceito, com a regra: **um item com `modData` divergente é reconciliado pelo host no próximo save, e o `modData` do host sempre prevalece** (first-write-wins sobre o estado do host, nunca last-write-wins do cliente).

Isso é uma linha de spec, não código — e é *exatamente* o tipo de coisa que a constituição exige que esteja escrita antes da implementação.

### 6.8 Som — quais cues são por-jogador e quais não podem dobrar

**Resposta: os quatro métodos têm alcances distintos, e a escolha errada é a que já está no código.**

Confirmado na wiki `Modding:Audio` §Playing sounds, e **verificado por reflexão** contra 4.5.2:

| Método | Alcance | Assinatura real |
|---|---|---|
| `Game1.playSound(cue, int? pitch)` | **só o jogador local**, sem sync, sem posição | ✅ existe |
| `GameLocation.localSound(cue, Vector2?, int?, SoundContext)` | jogador atual **se estiver na location**, com atenuação por distância | ✅ existe |
| `GameLocation.playSound(cue, Vector2?, int?, SoundContext)` | **todos** os jogadores na location | ✅ existe |
| `DelayedAction.playSoundAfterDelay(cue, delay, location, Vector2?, pitch, bool local)` | configurável | ✅ existe |

⚠️ `Game1.ambientSound` **não existe** — o enunciado da tarefa o tratava como método global. O membro mais próximo é `Game1.ambientCategory`, que é um `IAudioCategory` (um canal de volume), não um método de play. Audio ambient de verdade é resolvido por `Data/LocationContexts`. Não há áudio ambient no escopo deste mod, e o briefing `audio-ready.md` §8 já põe ambient fora de escopo. **Não há divergência de asset a resolver aqui** — a premissa do enunciado estava errada.

**Classificação dos cues de `composting_bin_*`:**

| Cue | Quando | Alcance correto | Hoje | Sev |
|---|---|---|---|---|
| coleta (`"Ship"`, `CompostingBinService.cs:112`) | alguém coleta | **`GameLocation.playSound`** — todos na location, posicional | `Game1.playSound` (local) | 🟠 |
| ready (day-start) | bin matura | **reativo**: cada cliente toca ao *entrar* na location. **Não** broadcast no day-start | `Game1.playSound` no `ProcessDayStart` | 🟠 |
| `cancel` | recusa | **`Game1.playSound`** — local, por definição | ✅ já local | ✅ |
| `+15` (se algum dia tiver som) | aplicação | `GameLocation.playSound` | — | — |

**Por que ready não pode ser broadcast:** `ProcessDayStart` roda no host. O `Game1.playSound` do host toca **no host**. Se o host usasse `playSound` (todos), o farm hand que está na **mina** ouviria o ding de um bin que nem está vendo. O briefing `audio-ready.md:202-205` já chegou a essa conclusão; a matriz de teste manual com farmhand (`:223`) é exatamente o gate que D1 precisa executar. **Este briefing não repete a prosa — confirma-a e adiciona:** o `locationSprites`/state-delta path garante que **o sprite** do bin chega ao cliente, mas **não** que o **som** chegue. Audio e sprite são canais separados, e só o sprite é replicado automaticamente.

**`Ship` é duplo-erro** (semântica + alcance), já registrado em `audio-ready.md` §7.2 como gate. Este briefing adiciona a consequência de multiplayer: como o cue está em `Game1.playSound`, o **farm hand nunca ouve a coleta que o host fez** — o que torna o gate de `audio-ready.md` **pré-requisito** de D1, não paralelo a ele. Remover `Ship` primeiro.

**O `.ogg` de 24 bytes é o bloqueador raiz.** Nenhum dos cues existe de fato: o gate `§7.1` exige >10 KB, e o stub falha por 3 ordens de grandeza. **Não há som para sincronizar.** Este é um item de pipeline de conteúdo, não de multiplayer — mas ele está **na frente** de qualquer verificação de áudio em duas telas.

### 6.9 GMCM/config — o config do farm hand não pode bifurcar o config da fazenda

**Resposta: hoje não há UI nenhuma, e o arquivo de config é uma bomba de relógio que só precisa de duas janelas para explodir.**

**Estado real:** `manifest.json` tem `{Name, Author, Version, Description, UniqueID, EntryDll, MinimumApiVersion}` — **sem bloco `Dependencies`**, portanto **sem GMCM**. Não existe `AddGenericModConfigMenu`, `AddPage`, nem `config.json`. O único caminho de config é o arquivo JSON + console.

**A bomba:** `VisualizationConfigurationService` (`:16`) usa `DataKeyPrefix = "visualization_config_"` + saveId sanitizado. O saveId vem de `Constants.SaveFolderName` (`SaveIdProvider.cs:22`), que é **o mesmo** para host e farm hand (é o nome da pasta do save). Então:

- Host e farm hand **leem e escrevem o mesmo arquivo**.
- O arquivo está em `data/{key}.json` via **`helper.Data.WriteJsonFile`** (`ModDataService.cs:55`) — isto é, **na pasta do mod, no disco de cada cliente**, não no save. (Verificado: `WriteJsonFile` vs `WriteSaveData` são APIs distintas; a primeira é "arbitrary .json files in your mod folder" e é **perdida se o jogador apagar os arquivos do mod**.)
- `GameLoop.Saving` é levantado **em farm hands** (wiki Events: *"This is also raised for farmhands in multiplayer"*). Então o `OnSaving` de cada cliente (`ModController.cs:750-779`) **grava por cima**.

**Resultado: dois clientes, um arquivo, last-write-wins.** O farm hand que tem `ShowPatterns: false` salva por cima do host que tem `true`, e o host volta do dia seguinte com os padrões desligados — sem aviso, sem log, e o dono do save nem é o dono do arquivo. Isto viola Gate 2 ("no feature ships with a known single-player-only code path") na direção oposta: um cliente **reescreve** a config de outro.

**A decisão que a spec precisa tomar (é uma linha, mas é dela):**

A tensão real é entre dois requisitos que hoje colidem:
- Princípio VI: *"Farm state is shared, host-authoritative... clients MUST NOT fork divergent copies of farm state."*
- E, simultaneamente: opacidade e acessibilidade são **preferências de jogador**, não de fazenda. Forçar o config do host sobre um farm hand com baixa visão seria umΠαραβίαção de agente do usuário (e a própria FR-009 cita "Game Accessibility Guidelines: player agency over accessibility settings").

**Recomendação (a spec confirma ou troca):**

| Categoria de config | Dono | Onde persiste | Porquê |
|---|---|---|---|
| **Semântica** — `OverlaysEnabled`, cores, `Opacity`, `AccessibilityDegradation` | **fazenda (host)** | **save** (`WriteSaveData`) | É a leitura da fazenda. Dois clientes vendo cores diferentes para o mesmo solo é exatamente o que o Constitution proíbe. |
| **Preferência de dispositivo** — `ShowPatterns`, `TooltipsEnabled` (se ligada/desligada por.accessibilidade individual) | **cliente** | config local do cliente, **nunca** no save | Acessibilidade é pessoal. `WriteGlobalData` ou o `config.json` do GMCM. |

O **bug de hoje** é que as duas categorias estão no **mesmo objeto** (`VisualizationConfiguration`) e no **mesmo arquivo**. Nenhuma das duasmetades está marcada. A spec precisa **particionar o schema** — e isso é uma emenda à spec 001, que a torna dependente de D1 (é a "reconciliation required" de `ROADMAP.md:84-87`).

Se GMCM entrar depois (A16), essa é a superfície: cada cliente vê os **seus** toggles de acessibilidade e os toggles **da fazenda** como read-only, rotulados como tales. **Nenhum asset novo** — a UI é do GMCM.

---

## 7. Seams e APIs recomendados

Cada um é mínimo, testável sem rede, e Consistent com o layering (contrato em `Domain/`, impl em `Services/`, orquestração em `Controllers/`).

| # | Seam | Onde | Assinatura | Porquê |
|---|---|---|---|---|
| S1 | `ITileHealthReplicator` | `Domain/Interfaces/` | `event Action<string, IReadOnlyList<TileDelta>> TileHealthChanged;` + `Task RequestSnapshotAsync(long peerId)` | Isola o transporte. `SoilHealthService` publica; a viz consome. **Permite testar A2 sem rede.** |
| S2 | `IMessageHud` | `Domain/Interfaces/` | `void Show(string translationKey, params string[] args)` | Already precedido por `IPlayerInventory` (`CompostingBinService.cs:18`). Torna A8 testável e forcing i18n (a chave crua hoje) impossível por construção. |
| S3 | `ISoundPlayer` | `Domain/Interfaces/` | `void PlayLocal(string cue); void PlayInLocation(string cue, Vector2 tile, int? pitch)` | Os **dois** alcances de §6.8 em um nome. Testa a escolha de canal sem `Game1`. Precedente: `NoOpSoundsHelperStub` já existe. |
| S4 | `ILockIndicator` | `Domain/Interfaces/` | `bool TryGetHolder(string location, Vector2 tile, out long farmerId)` | Consome o `GetLockHolder` **já existente** (A9) sem que a viz conheça o lock service. |
| S5 | `IInterestRegistry` | `Domain/Interfaces/` | `void Mark(string location, Vector2 tile, long farmerId, int ttlMs); void Clear(...); IReadOnlyList<...> ActiveIn(Rectangle viewport)` | A janela de interesse de §6.4 — separada da semântica de first-wins. |
| S6 | `PerScreen<T>` no `VisualizationService` | `Services/Visualization/` | `readonly PerScreen<Point> _cursorTile`, `readonly PerScreen<bool> _degradationNotified` | A11, A17. **`readonly` é obrigatório** (a wiki é explícita: sobrescrever o campo limpa todos). |
| S7 | Viewport real | `Controllers/ModController.cs:795` | `var viewport = Game1.viewport;` | A13. Uma linha, mas é pré-requisito de A2. |
| S8 | Conversão tile→screen com viewport | `HoeFeedbackRenderer.TileToScreenPosition`, `VisualizationService.DrawOverlay` | `screen = tile * TileSize - Game1.viewport.Position` | A10. A fórmula é da wiki Game Fundamentals: `screenPos = (tilePos * tileSize) - viewport`. |
| S9 | Host-broadcast wrapper | `Services/` | `void BroadcastToLocation(GameLocation loc, TemporaryAnimatedSprite sprite)` | Centraliza o `if (!Game1.IsMasterGame) return;` do padrão vanilla (`FarmerTeam.OnRequestHorseWarp`) e a regra de "só o host transmite" de §6.1. |

**Princípio de S9:** `broadcastSprites` é seguro em single-player (o corpo sempre faz `AddRange` local). A assimetria que **não** é segura é o *caller* — cliente deve encaminhar ao host. S9 deve ser a **única** porta de entrada, e o `Game1.IsMasterGame` check deve estar **dentro** dela, não espalhado pelos callers.

---

## 8. Estratégia de testes

### 8.1 O que já existe (e o que já está errado)

`LivingRoots.Tests/MultiplayerConcurrencyTests.cs` tem 9 testes cobrindo 4 axes: day-advance com 2 e 4 jogadores (`:147`, `:186`), day-advance concorrente (`:227`), first-wins com zero perda de item (`:271`, `:347`, `:451`, `:417`) e decay 1×/dia (`:549`). `MultiplayerFarmerStub` usa `AsyncLocal` para o farmer ambiente (`:286`) e `Barrier` + `TaskCompletionSource` para "simultaneidade" (`:504-520`).

**Limitação estrutural, e é importante ser preciso:** `ThreadSafeGameLoopEventsStub` implementa `IGameLoopEvents` — **não** `IMultiplayerEvents`. Então **não existe nenhum stub de `Multiplayer.PeerConnected` / `ModMessageReceived`** no repo. Testar os seams de sync (S1) exige criá-los do zero. `spec 004:290` é honesto ao dizer "automated multi-threaded test stubs" e "live multi-client network packet transport testing remains owed" — mas o que existe cobre **a lock**, não **o transporte**.

### 8.2 ⚠️ Achado novo: a suíte está instável, e a instabilidade está nos testes de multiplayer

Isto não estava em nenhum briefing e precisa entrar em D1, porque muda o que "verified" significa em `spec 004:290`.

**Medição (2026-10-05, 13 execuções da suíte completa, 913 testes cada):**

| Faixa observada | Falhas por execução | Total de testes |
|---|---|---|
| mínimo | 1 | 913 |
| máximo | 5 | 913 |

**Em 23 execuções acumuladas, os únicos testes que falharam de forma repetida foram 3 de `MultiplayerConcurrencyTests`:**

| Teste | Runs com falha |
|---|---|
| `ConcurrentDepositWaste_TwoFarmersOnSameBin_...` | **8–10 de 10** (falha praticamente em toda execução) |
| `ConcurrentCompostApplication_TwoFarmersOnSameTile_...` | 4–5 de 10 |
| `ConcurrentCollectCompost_TwoFarmersOnSameBin_...` | 4–5 de 10 |

**Ressalva de honestidade:** em **uma** execução também falhou `SoilHealthVisualizationTests.OnTileHealthChanged_...WithinFrameBudget`, que **não** reproduziu em 12 execuções subsequentes nem em 6 execuções isoladas. A causa é visível no teste (`SoilHealthVisualizationTests.cs:84`): ele assere `stopwatch.Elapsed <= 16.67ms`, uma **afirmação de wall-clock** executada sob carga de xUnit paralelo. É uma **segunda classe** de instabilidade — sensível à carga da máquina, não à lógica — e é distinta da falha de lock abaixo. Registrada porque o próximo a investigar a suíte vai tropeçar nela, e porque reforça o argumento: **a suíte mede tempo de wall-clock num gate de CI**, o que é frágil por construção.

**E, criticamente: em isolamento, os 9 `MultiplayerConcurrencyTests` passam — 4 execuções seguidas, 0 falhas.**

```
GamePath=$GP dotnet test --no-build --filter "FullyQualifiedName~MultiplayerConcurrencyTests"
  → Aprovado! Com falha: 0, Aprovado: 9  (×4 runs consecutivos)
```

**Causa raiz (lida no código, não adivinhada).** O teste assume que o perdedor **sempre** recebe a recusa:

```csharp
// MultiplayerConcurrencyTests.cs:315-329
Assert.Equal(1, successes);
Assert.Equal(1, refusals);
var refusedFarmer = host.RefusedCount > 0 ? host : farmhand;
Assert.Single(refusedFarmer.RefusalMessages);   // ← falha aqui: "collection was empty"
```

Mas `TryInteractWithBin` tem **dois** caminhos de `return false` e **só um** emite recusa:

```csharp
// ModController.cs:1283-1315
if (!_hostAtomicLockService.TryAcquireLock(...)) {
    refusalHandler?.Invoke("message.compost.action-conflict");   // ← caminho A: recusa
    return false;
}
// ... adquire o lock
var state = _compostingBinService.GetBinState(...);
if (heldItem != null) { if (state == Empty) { AddWaste(...); return true; } }
//                                                                              ↓
//                                            caminho B: return false SEM recusa
return false;
```

O `Barrier` + `TaskCompletionSource` (`:504-520`) dá **início simultâneo**, não **execução simultânea**. Se o winner completa o `try/finally` (que **libera** o lock, `ModController.cs:1317-1320`) antes do loser chamar `TryAcquireLock`, o loser **adquire** o lock, cai no caminho B, e `return false` **sem tocar no `refusalHandler`**. Logo: 1 success, 1 refusal — as asserções de contagem passam — mas `RefusalMessages` fica **vazio**, e `Assert.Single` estoura.

**Por que isso importa para D1, e não é só um teste quebrado:** o caminho B é **o caminho comum em jogo**. A janela de lock é de microsegundos. Duas interações simultâneas de verdade (o caso que a lock existe para resolver) são uma **medida-zero** do tempo de jogo real; o caso **frequente** é o loser chegando "logo depois", adquire o lock livre, e cai no caminho B. Ou seja: **a lock raramente é o que impede a ação dupla — o estado (`state == Empty`) é.** E quando isso acontece, **o jogador recebe `false` e nenhum feedback** — o que torna §6.4 (a nameplate) ainda mais necessária, e adiciona um requisito que não estava na lista: **todo caminho de `false` tem de produzir um asset de feedback.**

Isto é um achado de **correção de bug** disfarçado de briefing de assets, e o `speckit.bug.*` loop é o dono dele — não este documento. Mas ele tem que estar registrado, porque muda a premissa de `spec 004:290` ("Verified in Feature 004 by automated multi-threaded test stubs").

### 8.3 Estratégia proposta para D1

| Camada | Cobre | Precisa de rede? | Ferramenta |
|---|---|---|---|
| **L1 — unit** | Mapping tile→delta, partição de config (§6.9), regras de alcanço de som (§6.8), formatação de nameplate | ❌ | xUnit + `Moq`; `NoOpSoundsHelperStub` para S3 |
| **L2 — seam** | `ISoundPlayer` escolhe `local` vs `location`; `IMessageHud` nunca vê chave crua; `S1` serializa→desserializa sem perda | ❌ | xUnit; stub de `IMultiplayerEvents` **precisa ser criado** (não existe) |
| **L3 — integração host** | Host roda o dia uma vez com 4 jogadores conectados; delta aplicado; cache invalidado; overlay reflete no frame seguinte | ❌ (1 processo) | `MultiplayerFarmerStub` + `Barrier` — **mas corrigir a flakiness de §8.2 primeiro** |
| **L4 — 2 clientes reais** | O gate de `Constitution.md:59` | ✅ | **manual, 2 máquinas** — o gate de `audio-ready.md:223` |

**Correção de L3 (pré-requisito, não opcional):** o `Barrier` de `RunSimultaneouslyAsync` deve ser trocado por um **mecanismo de overlap real** — ex.: um segundo `Barrier` *dentro* do bloco de lock, ou `ManualResetEventSlim` que o teste controla — de modo que a asserção "o perdedor recebeu recusa" seja garantida **pelo test harness**, e não pela sorte do scheduler. Alternativamente (e melhor): **corrigir o `ModController` para que todo `return false` emita recusa** (§8.2), e então o teste passa a ser uma asserção **determinística** sobre comportamento correto, em vez de uma corrida de timing.

**Regra de gate:** nenhuma task de D1 entra em "complete" enquanto a suíte for instável. Gate 1 (`constitution.md:53`) exige `dotnet test` **para passar** — e uma suíte que passa 8/10 vezes não passa. Um gate que falha 1 em 5 execuções treina a equipe a ignorar o gate, que é pior do que não tê-lo.

### 8.4 Testes de asset (o que é verificável sem rede)

| Teste | Asserção | Fecha |
|---|---|---|
| `ConfigSchema_PartitionsFarmAndDeviceSettings` | toda chave de `VisualizationConfiguration` está em exatamente uma das duas listas de §6.9 | A14, N6 |
| `ConfigSchema_NeverPersistsDeviceSettingsToSave` | a config de dispositivo nunca vai para `WriteSaveData` | A14 |
| `SoundPlayer_CollectionCue_UsesLocationScope` | S3 emite `PlayInLocation` (não `PlayLocal`) para o cue de coleta | A6 |
| `SoundPlayer_ReadyCue_UsesLocalScope` | e para o de ready, que é reativo | A7 |
| `CompostApplication_EmitsBroadcastableSprite` | o sprite tem `text`/`color`/`motion`/`alphaFade` e **id estável** (para o fade não duplicar) | A1 |
| `Nameplate_ResolvesFarmerDisplayName` | `farmerId` → `Game1.getFarmer` → `displayName`; id desconhecido → fallback, **não crash** | A9 |
| `PerScreenFields_AreReadonly` | asserção de **reflexão** sobre os campos de `VisualizationService` | A11, A17 |
| `RenderedWorld_UsesLiveViewport` | o viewport passado ao `RenderOverlays` é `Game1.viewport`, não uma constante | A13 |
| `TileToScreen_SubtractsViewport` | fórmula da wiki: `tile * 64 - viewport.Position` | A10 |
| `EveryFalseReturn_EmitsFeedback` | instrumenta `TryInteractWithBin`: todo `return false` chamou o `refusalHandler` | §8.2 |

O penúltimo bloco é o que impede a regressão de §8.2 de voltar. E `PerScreenFields_AreReadonly` é uma asserção de **forma**, não de comportamento — mas a wiki é explícita sobre a consequência de ignorar, e é o tipo de coisa que um refactor silencioso quebra.

---

## 9. Verificação in-game (2 clientes + 1 farm hand)

O gate de `constitution.md:59`. **Não automatizável** — é a razão de `spec 004:290` dizer "owed". Roteiro; cada passo tem um **asset observável**, não um feeling.

### 9.1 Pré-requisitos (bloqueiam tudo)

| # | Pré-req | Porquê | Status |
|---|---|---|---|
| P1 | **D3 fechado** (`ROADMAP.md:97-110`) | Nada é desenhado hoje. `Assets/` não é carregado. | ❌ bloqueia **todo** o roteiro |
| P1b | **`SetTileHealthData` tem caller de produção** (A19) | Sem isso o overlay não tem dados **nem em single-player**; V1 não tem o que observar | ❌ |
| P2 | `ModEntry.cs:152-176` sem `ShowNextIndex*`/`DaysUntilReady` | Sem isso não há frame de ready, nem maturação de 2 dias | ❌ |
| P3 | `Ship` removido (`CompostingBinService.cs:112`) | Gate de `audio-ready.md` §7.2; pré-req de áudio multiplayer | ❌ |
| P4 | `.ogg` real (>10 KB, estéreo, <1 s — `audio-ready.md` §7.1) | Stub de 24 bytes = não há som para sincronizar | ❌ |
| P5 | Suíte estável (§8.2) | Gate 1 exige passar, não passar 80% | ❌ |
| P6 | `GamePath` válido em ambas as máquinas | `.gamepath/` é local | manual |

### 9.2 Roteiro

| # | Passo | Host | Farm hand | **Asset a observar** |
|---|---|---|---|---|
| V1 | Till + compost num tile | vê overlay | **deve ver a mesma cor** (A2) | Se o farm hand vê cinza → N3 ausente. **A falha mais provável.** |
| V2 | Farm hand aplica compost | vê `+15` | **vê `+15`** (A1) | Se só um vê → `broadcastSprites` não aplicado (C3) |
| V3 | Ambos no mesmo tile, clique duplo | 1 aplicação | 1 recusa, **com motivo** | 2 aplicações → lock furada. 0 recusa visível → caminho B de §8.2 |
| V4 | Reduzir o zoom/pan para >1000 tiles | padrão → toast | **o seu próprio** toast (A3) | 2 toasts no host = flag não-por-tela. 0 no farm hand = `_degradationNotificationSent` compartilhado |
| V5 | Coleta de um bin pronto | som | **som** (A6) | Silêncio no farm hand → `Game1.playSound` em vez de `location.playSound` |
| V6 | Bin fica pronto durante o dia | vê sprite ready | **vê sprite ready** (A5) | Verifica se `Net*` state chega antes do save |
| V7 | Entrar/sair da location repetidamente | 1 ding ao entrar, 0 dentro do cooldown | idem | 2 dings = ready está em broadcast, não reativo (A7) |
| V8 | Pega o bin (drag) com `modData` | — | **mesmo `DaysRemaining`** (§6.7) | Divergência = save cycle, esperado e documentado |
| V9 | Salvar; alterar config; salvar no farm hand | config volta **do host** na config de fazenda | prefs de dispositivo **não** voltam (A14) | Prefs do farm hand sobrescrevendo host = last-write-wins |
| V10 | Split-screen 2 jogadores locais | tooltip do P1 com cursor do P1 | **idem P2** | Tooltip cruzado = A11. Cache de overlay deslocado = A13 |
| V11 | Farm hand sai da fazenda e volta | sprite some | — | Sprite persistente = bug de lifecycle |
| V12 | `debug logSounds` durante V5/V7 | 1 cue por evento | 0 cues fantasma | Dupla = double-fire |

**Gravação:** vídeo curto por passo ou checklist assinado — `audio-ready.md:223` já estabelece que isso basta. A PR template do repo pede **in-game manual testing** explicitamente, e este roteiro é a instância concreta.

### 9.3 O que este roteiro **não** prova

- **Latência real de rede.** Dois clientes em LAN com 0 ms de RTT não testam interpolação (`Multiplayer.defaultInterpolationTicks = 15` no decompile). O `spec 004:290` fala de "packet transport" — isso exige WAN ou um link artificial.
- **Detecção de host trocado.** Mid-session host migration não é suportada pelo jogo base; não é escopo.
- **Combinações de mods.** Um mod que também use `TemporaryAnimatedSprite` com o mesmo `id` colide. Fora de escopo, mas registrar.

---

## 10. Riscos

| # | Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|---|
| R1 | **Nada renderiza até D3 fechar** — todo gate de asset é teórico | **Certa** | O roteiro de §9 não executa | Roadmap já recomenda D3→D1 (`ROADMAP.md:112-115`). **Não inverter.** |
| R2 | A suíte instável (§8.2) torna "verified" em `spec 004:290` **indefensável** | **Certa** | Gate 1 não passa | Corrigir caminho B + harness de overlap (§8.3) **antes** de D1 |
| R2b | Testes que asserem **wall-clock** (`stopwatch.Elapsed <= 16.67ms`) num gate de CI paralelo são sensíveis à carga da máquina | Média | Falha intermitente sem causa de lógica | Substituir por asserção de **resultado** (cache invalidado), não de tempo; o tempo é verificado no roteiro in-game (§9) |
| R3 | Payload de sync grande demais (30k tiles) | Média | Lag de cliente | Delta + snapshot-on-join (§6.2b) |
| R4 | `modData` divergir do state ao vivo (A15) | Média | Frame desatualizado num cliente | Declarar em spec (§6.7); `DaysRemaining` limita a janela a 2 dias |
| R5 | Config bifurcada / last-write-wins (A14) | **Alta** | Dono do save perde config sem aviso | Particionar schema (§6.9) — **mudança de schema, precisa de migração aditiva** (Princípio VI: "Migration MUST be additive and lossless") |
| R6 | Asset novo (nameplate) vira escopo infinito | Média | Feature spec cresce | N1 é o **único** asset novo. Congelar a lista em §5.2 |
| R7 | Citações de API erradas se propagam (§2.1 — **3 já existem no repo**) | **Alta** | Código que não compila, ou pior, que compila errado | Regra: todo membro novo passa pelo probe (§2.1) antes de virar requirement |
| R8 | Split-screen treated como "multiplayer suportado" por acidente | Baixa | Falsos verdes | `Context.IsSplitScreen` é separado; tratar como **eixo próprio** na spec |
| R9 | GMCM entra tarde e expõe config de fazenda como editável | Baixa | Bifurcação de volta | §6.9: toggles de fazenda read-only no GMCM |
| R10 | `broadcastSprites` chamado de cliente retransmite errado | Média | Sprites duplicados ou ausentes | S9: check `Game1.IsMasterGame` **dentro** da seam, não nos callers |

---

## 11. Fora de escopo

Explicitamente **não** coberto por este briefing:

1. **A spec de multiplayer.** Modelo de propriedade, resolução de divergência por tile, migração de save — `ROADMAP.md:82-83` diz `/speckit.specify`.
2. **O mecanismo de lock** (`HostAtomicLockService` existe e tem testes). Este briefing consome `GetLockHolder`; não redesenha a lock.
3. **O bug de flakiness (§8.2).** É um bug, com dono próprio (`speckit.bug.*`). Registrado aqui porque muda a leitura de `spec 004:290`, não porque este briefing o vá corrigir.
4. **A correção das 8 docs que dizem "single-player only".** Tabela §3.1 cataloga; a emenda é da spec proprietária de cada uma.
5. **D3 — wiring de content.** Dependência (§1.3), não escopo.
6. **Host migration, WAN/latência, combinações de mods** (§9.3).
7. **Pipeline de conteúdo** (arte final, `.ogg` real). O que importa aqui é *qual* asset, não produzi-lo.
8. **Áudio ambient / música** — já fora de escopo em `audio-ready.md` §8, e `Game1.ambientSound` não existe (§2.1).
9. **O game path / build config.** `GamePath` é obrigatório e não está em nenhum `.csproj` (ver `AGENTS.md` raiz) — logística, não asset.

---

## 12. Fechamento

O que este briefing estabelece, e o que ele **não** faz:

**Estabelece:** que multiplayer impõe **três eixos ortogonais** (alcance / autoridade / persistência) e que o passivo de assets do mod já erra em **8 linhas vermelhas** sem crash nem erro; que o overlay **não tem dados em single-player hoje** (A19 — nenhum caller de produção de `SetTileHealthData`), o que torna toda a tabela de divergências uma descrição de um sistema ainda não alimentado; que `broadcastSprites` é seguro em single-player **por construção** (o corpo sempre adiciona local) mas o *caller* precisa ser host-only; que a nameplate de lock (N1) é o **único** asset genuinamente novo, e ela depende de uma decisão de janela de interesse que a spec tem de tomar; que config de fazenda e config de dispositivo hoje são **o mesmo objeto no mesmo arquivo** com last-write-wins entre clientes; e que a suíte de testes de multiplayer é **instável** — o teste de depósito falhou em 8 de 10 execuções, e a suíte passa 9/9 em isolamento; e que a suíte tem uma **segunda** classe de instabilidade, em testes que asserem wall-clock num gate de CI paralelo.

**Não faz:** não especifica o multiplayer, não reescreve as 8 specs, não fecha D3, não desenha o nameplate, e não substitui o `speckit.bug.*` para a flakiness.

**A única conclusão que muda o plano:** `ROADMAP.md:112-115` recomenda D3 antes de D1 por razões práticas. Este briefing concorda e **acrescenta** que existe uma dependência interna a D1 que o roadmap não registra — **a suíte de testes de multiplayer precisa consertar o caminho B de `TryInteractWithBin` antes que qualquer requirement de multiplayer possa ser considerado "verified"**, porque `spec 004:290` já afirma essa verificação e ela não se sustenta.

**Última verificação:** 2026-10-05 — wiki relida (Multiplayer/Events/Utilities/Data/Audio/Common tasks), assemblies do jogo inspecionados por reflexão contra `.gamepath/` (SMAPI 4.5.2; ~100 membros; 4 APIs afirmadas no enunciado desta tarefa **não existem** — `Game1.getFarmerUniqueID`, `Game1.ambientSound`, `GameLocation.playSoundAt`, `Context_applied` — e mais 2 no repo, `Farmer.IsSplitScreen` e `IMultiplayerHelper.GetUniqueID`), `SetTileHealthData` confirmado sem caller de produção, suíte executada 23× (913 testes; 1–5 falhas por execução, concentradas em 3 `MultiplayerConcurrencyTests`; 0 falhas em isolamento ×4 e ×6).
