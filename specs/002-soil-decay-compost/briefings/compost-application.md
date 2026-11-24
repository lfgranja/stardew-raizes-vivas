# Briefing: Compost Application — restauração +15 (FR-004–FR-007, FR-012, FR-015, FR-017)

**Status**: research complete · **Type**: application-service + feedback visual/sonoro · **Priority**: P1 — fecha o loop decay/restoration (US2)
**Revisado em**: 2026-10-05 · **Idioma do briefing**: pt-BR
**Destino**: `specs/002-soil-decay-compost/briefings/compost-application.md` (asset da 002; complementa o briefing i18n da 004)
**Sources**:
- repo evidence citada inline como `file:line` (todas re-inspecionadas em 2026-10-05),
- `specs/002-soil-decay-compost/spec.md` (FR-004–FR-007, FR-012, FR-015, FR-017, FR-019),
- `specs/002-soil-decay-compost/{data-model.md, contracts/machine-interaction.md, contracts/save-data-schema.md, tasks.md}`,
- `specs/004-playable-compost-loop/briefings/i18n-localization.md` (11 chaves, bug `showRedMessage` literal),
- SMAPI wiki verificada em 2026-10-05: [Translation](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation), [Multiplayer](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer), [Events](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events) (Input.ButtonPressed, GameLoop.DayStarted, Content.AssetRequested/LocaleChanged).

> **Escopo deste asset**: só a aplicação do composto no solo (segurar composto + right-click no tile → +15, consome 1, texto flutuante verde, `cancel` nas recusas). Produção do composto (bin Empty→Processing→Ready, `AddWaste`/`CollectCompost`) aparece apenas como contexto de origem do item + §7 (maturação) porque o enunciado do asset lista `MaturationDays/MaxLevel/IdleReset/Increment` e `CompostingBinService.cs:61-131` como referência cruzada.

---

## 0. TL;DR para o implementador

1. Constantes canônicas em `LivingRoots/Constants.cs:55-74`: `RestorationAmount = 15f` (`:58`), `MaxSoilHealth = 100f` (`:11`), `MinSoilHealth = 0f` (`:10`), `MaturationDays = 2` (`:59`), `MaturationMaxLevel = 5` (`:60`), `MaturationIdleResetDays = 14` (`:61`), `MaturationIncrementDays = 7` (`:62`), `CompostItemId = "LivingRoots.Compost"` (`:63`), `QualifiedCompostItemId = "(O)LivingRoots.Compost"` (`:64`), `TextDurationMs = 1000` (`:40`).
2. Contrato: `ICompostApplicationService.TryApplyCompost(GameLocation, Vector2) -> bool` (`LivingRoots/Domain/Interfaces/ICompostApplicationService.cs:9-20`). Única impl: `LivingRoots/Services/CompostApplicationService.cs:18-81`. Único chamador produtivo: `ModController.OnButtonPressed` (`LivingRoots/Controllers/ModController.cs:861-870`) — checa item segurado **antes** de delegar, e só tenta o bin (`AddWaste`/`CollectCompost`, `:872-884`) se o composto não aplicou.
3. Ordem de validação no service (`CompostApplicationService.cs:22-54`): localização (farm/Greenhouse) → `HoeDirt` → saúde < 100 → segurar composto. Qualquer falha = `Game1.playSound("cancel")` + `return false`, **sem visual** (FR-017). Sucesso = `UpdateHealth(+15)` + `heldItem.Stack--` + sprite `"+15"` verde + `return true`.
4. **4 divergências spec-vs-código registradas no §6** (nenhuma quebra teste hoje, todas viram gate da 004 ou follow-up): (a) FR-012 exige `Game1.multiplayer.broadcastSprites` mas o código faz `location.TemporarySprites.Add`; (b) FR-012 exige `alphaFade = 0.0167f` mas o código usa `0.02f`; (c) duração `1000f` no construtor é interpretada como lifetime em ms por convenção, mas o parâmetro formal é `interval` — funciona por acidente de overload; (d) `message.*` ainda sem `showRedMessage` nos 3 ramos de recusa (gap FR-008a da 004, detalhado no briefing i18n).
5. Testes: 6 fatos em `LivingRoots.Tests/CompostApplicationServiceTests.cs:60-156` cobrem todos os ramos; `CompostingBinConstantsTests.cs:17-19` ancora `RestorationAmount == 15f`. `GameStateFixture` + `NoOpSoundsHelperStub` são load-bearing (sem eles `HoeDirt`/`playSound` quebram os testes — ver §8).

---

## 1. Constantes (única fonte de verdade)

`LivingRoots/Constants.cs:55-79`:

| Constante | Linha | Valor | Usada em |
|-----------|-------|-------|----------|
| `MaxSoilHealth` | `:11` | `100f` | teto em `TryApplyCompost` (`:38`) e clamp em `SoilHealthService` |
| `MinSoilHealth` | `:10` | `0f` | piso (decay; restauração não precisa) |
| `RestorationAmount` | `:58` | `15f` | delta (`:56-57`), texto `"+15"` (`:71`), log (`:77-79`) |
| `TextDurationMs` | `:40` | `1000` | duração nominal do floating text (FR-012; código passa `1000f` literal no ctor, não a constante — dívida §6.3) |
| `MaturationDays` | `:59` | `2` | transição Processing→Ready (contexto bin, §7) |
| `MaturationMaxLevel` | `:60` | `5` | teto de maturação (§7) |
| `MaturationIdleResetDays` | `:61` | `14` | reset 1x após 14 dias vazio (§7) |
| `MaturationIncrementDays` | `:62` | `7` | +1 nível / 7 dias ativos (§7) |
| `CompostItemId` | `:63` | `"LivingRoots.Compost"` | match de item (`:47,49`, `ModController.cs:862-864`, `ModEntry.cs:140`) |
| `QualifiedCompostItemId` | `:64` | `"(O)LivingRoots.Compost"` | match de item (`:48,63`, `ModEntry.cs:168`) |
| `CompostCategoryId` | `:73` | `-26` | categoria Fertilizer em `Data/Objects` (`ModEntry.cs:136,146`) |
| `CompostStackMax` | `:74` | `999` | stack máximo |
| `CompostCraftingTab` | `:68` | `"Home"` | aba da receita |

`description` em `i18n/default.json:3` e `ModEntry.cs:144` contém `"Restores 15 soil health"` hardcoded — diverge de `RestorationAmount` se a constante mudar (mesma fragilidade do `"2 days"` vs `MaturationDays`, documentada no briefing i18n §1.1 #2/#4).

---

## 2. Contrato e fluxo de chamada

### 2.1 Interface

`LivingRoots/Domain/Interfaces/ICompostApplicationService.cs:9-20`:

```csharp
bool TryApplyCompost(StardewValley.GameLocation location, Vector2 tile);
```

Doc do contrato já fixa o comportamento: valida tile tillado em farm/Greenhouse sem teto de saúde; sucesso = aumenta saúde + consome + floating text; falha = som cancel, sem visual.

### 2.2 Chamador: `ModController.OnButtonPressed`

`LivingRoots/Controllers/ModController.cs:845-890`:

1. `:848` — só `SButton.MouseRight`.
2. `:852-856` — tile = `cursorPos.GrabTile` (tile sob o mouse, padrão hoe/watering-can — spec `spec.md:125`), location = `Game1.currentLocation`.
3. `:859-865` — lê `Game1.player.CurrentItem` e testa as **3 formas** do ID (`CompostItemId` bare, `QualifiedCompostItemId`, `ItemId == CompostItemId`). Nota: checagem duplicada com a do service (`CompostApplicationService.cs:46-49`) — defesa em profundidade; o controller evita chamar o service quando a mão não tem composto, o service re-valida via `IPlayerProvider.CurrentItem` (testável sem `Game1.player`).
4. `:866-870` — `if (TryApplyCompost(...) == true) return;` — composto tem prioridade sobre o bin no mesmo tile.
5. `:872-884` — fallthrough para o bin: `GetBinState` → `AddWaste` (mão ocupada + Empty) ou `CollectCompost` (mão vazia + Ready).

Convenção de controllers (`LivingRoots/Controllers/AGENTS.md`): handlers deveriam usar `ExecuteWithConcurrencyGuard`, mas `OnButtonPressed` não usa — dívida pré-existente, fora deste asset.

### 2.3 Passo a passo do service

`LivingRoots/Services/CompostApplicationService.cs:18-81` (ctor injeta `ISoilHealthService`, `IPlayerProvider`, `IMonitor` — `new` só em `ModEntry.cs`, convenção `LivingRoots/AGENTS.md`):

| Passo | Linhas | Código | Spec |
|-------|--------|--------|------|
| 1. Gate de localização | `:22-27` | `if (!location.IsFarm && locationName != "Greenhouse")` → log Trace + `playSound("cancel")` + `false` | FR-015 (`IsFarm == true OR Name == "Greenhouse"`, `spec.md:147`). `location.Name ?? "Unknown"` (`:20`) evita NRE |
| 2. Gate de solo | `:29-35` | `terrainFeatures.TryGetValue(tile) is not HoeDirt` → cancel + `false` | FR-007 (não-tillado não consome), US2 cenário 3 (`spec.md:43`) |
| 3. Gate de teto | `:37-43` | `currentHealth >= MaxSoilHealth` → cancel + `false` | FR-005 (teto 100), FR-007 (teto não consome), US2 cenário 5 (`spec.md:45`) |
| 4. Gate de item | `:45-54` | `heldItem == null \|\| (QualifiedItemId != CompostItemId && != QualifiedCompostItemId && ItemId != CompostItemId)` → cancel + `false` | FR-006/FR-007 (só consome em sucesso), US2 cenário 4 (`spec.md:44`) |
| 5. Aplica | `:56-57` | `newHealth = Min(current + RestorationAmount, Max)` (só p/ log); `UpdateHealth(locationName, tile, RestorationAmount)` | FR-004 (+15), FR-005 (clamp delegado ao `SoilHealthService`) |
| 6. Consome | `:59` | `heldItem.Stack--` | FR-006 (1 por aplicação). **Sem `RemoveIfEmpty`/null de stack 0** — o jogo limpa stack 0 no tick seguinte; em teste usa stub |
| 7. Feedback visual | `:61-75` | `TemporaryAnimatedSprite` texto `"+15"` verde (detalhe §3) | FR-012 |
| 8. Log + retorno | `:77-80` | `"{current:F1} -> {new:F1} (+15)"` em Trace + `true` | FR-022 (só state-changing, Trace sem `ex.Message`) |

Ordem dos gates é significativa: localização e tipo de tile vêm **antes** de ler saúde ou mão — evita I/O e consumo em tile inválido. Saúde vem antes do item: jogador segurando composto num tile 100 ouve `cancel` sem consumir (correto), mas jogador de mão vazia num tile válido também ouve `cancel` (ruído menor; a 004 decide em §2.2 manter esse ramo silencioso — ver §4.2).

---

## 3. Feedback visual de sucesso — `TemporaryAnimatedSprite` "+15"

### 3.1 Código atual

`CompostApplicationService.cs:61-75`:

```csharp
var worldPosition = new Vector2(tile.X * 64f + 32f, tile.Y * 64f);
var sprite = new TemporaryAnimatedSprite(
    null,               // texture — null + text = sprite só-texto
    Rectangle.Empty,    // sourceRect vazio
    1000f,              // interval / lifetime nominal 1000ms
    1, 1,               // animationLength, numberOfLoops
    worldPosition,      // tile*64 + (32, 0): centrado em X, topo do tile
    false, false);      // flicker, flipped
sprite.text = $"+{ModConstants.RestorationAmount:F0}";  // "+15"
sprite.color = Color.Green;
sprite.alphaFade = 0.02f;
sprite.motion = new Vector2(0, -0.5f);                  // sobe 0.5px/frame
location.TemporarySprites.Add(sprite);
```

Posição: `tile * 64` converte tile→pixel (`TileSize = 64`, `Constants.cs:35`); `+32f` em X centraliza; Y no topo do tile para o texto subir sem cobrir a enxada. `motion (0,-0.5)` + `alphaFade` reproduz a convenção vanilla de stat-change (mesmo padrão do flash 300ms + texto 1000ms do `HoeFeedbackRenderer`).

### 3.2 O que a FR-012 exige

`spec.md:144` (FR-012): texto `"+15"` verde acima do tile, `TemporaryAnimatedSprite.text` em world space, visível por `TextDurationMs` (1000ms) com fade linear `alphaFade = 0.0167f`, multiplayer via `Game1.multiplayer.broadcastSprites`. Clarificação 2026-09-06 (`spec.md:84`) fixa essa abordagem como convenção vanilla para stat-change.

### 3.3 Divergências (registradas, não silenciosas)

| # | Spec | Código | Severidade |
|---|------|--------|------------|
| D1 | `Game1.multiplayer.broadcastSprites` (FR-012) | `location.TemporarySprites.Add(sprite)` (`:75`) — só tela local | **Média**: em multiplayer o farmhand aplicador vê, os demais não. Fix = trocar por `broadcastSprites(location, sprite)`; checar assinatura 1.6 (`Game1.multiplayer.broadcastSprites(GameLocation, TemporaryAnimatedSprite)`). Assunção `spec.md:214` diz "single-player only", mas FR-012 exige broadcast — contradição herdada da spec; o fix alinha com a FR |
| D2 | `alphaFade = 0.0167f` (FR-012) | `0.02f` (`:73`) | **Baixa**: fade ~17% mais rápido (≈830ms efetivos vs 1000ms). Fix = `0.0167f` ou `1f/60f` documentado |
| D3 | duração via `ModConstants.TextDurationMs` | literal `1000f` no ctor (`:65`) | **Baixa**: mesma fragilidade do `"15"`/`"2 days"` em texto. Fix = passar `ModConstants.TextDurationMs` |
| D4 | `sprite.text` localizado (`floating-text`/`restoration-text`, FR-021) | `$"+{RestorationAmount:F0}"` hardcoded | **Dívida documentada**: briefing i18n §2.3 recomenda **não** traduzir número puro via dicionário; fusão das duas chaves (`+{{amount}}`) é follow-up. Manter paridade das chaves até lá |

### 3.4 Externo: `TemporaryAnimatedSprite` API (1.6)

Sem página wiki dedicada; comportamento consolidado por uso vanilla + overloads observados no repo:

- Overload texto-só-texto: `(Texture2D textureNull, Rectangle sourceRect, float interval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped)` — com `texture == null` e `text != ""` o `Draw` renderiza string com `Game1.smallFont`/`dialogueFont` na `color`, ignorando frames. É o padrão vanilla para `"+1"`, `"Miss!"`, `"Too far!"`.
- Campos relevantes: `text: string`, `color: Color`, `alphaFade: float` (decremento de alpha por frame — `0.0167 ≈ 1/60`), `motion: Vector2` (deslocamento por frame), `interval` (ms por frame de animação; com `animationLength=1` equivale ao lifetime visível na prática).
- Entrega: `location.TemporarySprites: List<TemporaryAnimatedSprite>` (só tela local) vs `Game1.multiplayer.broadcastSprites(GameLocation, TemporaryAnimatedSprite)` (propaga a farmhands na mesma location — é o que a FR-012 exige e o que falta em D1).
- Confiança: **alta** para overload/campos (assinatura idêntica à chamada no repo); **média** para lifetime exato do `interval` com `animationLength=1` (convenção, não contrato documentado — gate em jogo: cronometrar visibilidade ≈1000ms).

---

## 4. Feedback de recusa — `cancel` sem visual + `message.*` (ponte com a 004)

### 4.1 Estado atual (spec 002)

FR-017 (`spec.md:149`): `cancel` sutil, **sem visual** — minimalismo vanilla para "ação impossível". Os 4 ramos (`:25,33,41,52`) cumprem: só `playSound("cancel")`, nenhum sprite/mensagem. Logs em Trace identificam o ramo (`"not farm or Greenhouse"`, `"not tilled soil"`, `"already at maximum health"`, `"not holding compost"`).

### 4.2 O que a 004 adiciona (sem contradizer)

Briefing i18n (`specs/004-playable-compost-loop/briefings/i18n-localization.md:147-186`, §2.2) mapeia cada ramo a uma chave, cumulativo ao `cancel` (áudio + banner coexistem):

| Ramo (`CompostApplicationService.cs`) | Chave | Via |
|--------------------------------------|-------|-----|
| `:22-27` mapa não-fazenda/estufa | `message.compost.invalid-tile` (`default.json:9`) | `Game1.showRedMessage` a criar |
| `:29-35` tile não-tillado | `message.compost.invalid-tile` | idem |
| `:37-43` saúde 100 | `message.compost.max-health` (`default.json:10`) | idem |
| `:45-54` mão sem composto | — (pré-condição de input, silencioso ok) | manter só `cancel` |
| conflito multiplayer (lock atômico futuro) | `message.compost.action-conflict` (`default.json:11`) | nasce no `HostAtomicLockService` |
| inventário cheio na coleta (`CompostingBinService.cs:95`) | `message.harvest.inventory-full` (`default.json:12`) | **bug hoje**: chave literal exibida crua (i18n §1.3) |

Exceção explícita da 004 (`spec.md:53`, i18n §2.2): drop-in inválido no bin (`ModController.cs:877-883`) permanece silencioso — `showRedMessage` só nos caminhos de composto/coleta/conflito.

### 4.3 Externo: `Game1.showRedMessage` / `Game1.playSound`

- `Game1.showRedMessage(string text)`: banner vermelho inferior na fonte nativa do jogo; é o veículo FR-008a ("na fonte do jogo, sem UI própria"). Estático → exige seam `IMessageHud` para teste (receita pronta no i18n §7, precedente `IPlayerInventory` em `CompostingBinService.cs:18`). **Nunca passar a chave crua** — sempre `_translation.Get(chave).Default(fallback)` (o bug `CompostingBinService.cs:95` é o contra-exemplo).
- `Game1.playSound(string cueName)`: cue vanilla `"cancel"` (rejeição), `"Ship"` (coleta/pronto em `CompostingBinService.cs:112,164`). Testes instalam `NoOpSoundsHelperStub` via `GameStateFixture` (§8) porque o helper real exige MonoGame bootado.
- Confiança: **alta** (assinaturas observadas no repo + convenção vanilla estável entre 1.5–1.6).

---

## 5. Onde a aplicação toca a persistência

`TryApplyCompost` não persiste diretamente — delega a `ISoilHealthService.UpdateHealth(locationName, tile, delta)` (`:57`):

- `SoilHealthService.UpdateHealth` (`LivingRoots/Services/SoilHealthService.cs:565-600`): rejeita `NaN/Infinity`, valida tile (`IsValidTile`), lê saúde atual do cache esparso (ausente = `0f`, `:585-590`), soma delta, grava via `SetHealthInternal` (clamp `[MinSoilHealth, MaxSoilHealth]`, `:659,716`) fora de lock para I/O. O clamp é o que realiza FR-005 (teto) e FR-002 (piso) — o `newHealth` local em `CompostApplicationService.cs:56` serve só ao log, não ao write (duplicação intencional leitura-log vs escrita-clampada; valor final idêntico porque ambos usam `Min(+15, 100)`).
- Cache esparso (`Services/AGENTS.md`): zeros omitidos — tile restaurado de 0→15 cria entrada; tile em 100 rejeitado antes (sem escrita, sem consumo).
- Chave de save: `soil_health_data_{saveId}` (FR-010, `data-model.md:26`, `save-data-schema.md:9-58`).
- Atenção a `GetSoilHealth` default `0f` (`SoilHealthService.cs:538`): tile recém-tillado sem registro lê `0`, então a primeira aplicação 0→15 funciona; o gate de teto (`>= 100`) nunca dispara falsamente para tile novo.

---

## 6. Gaps e dívidas (para não reintroduzir)

1. **D1 broadcastSprites** (§3.3): trocar `location.TemporarySprites.Add` por `Game1.multiplayer.broadcastSprites(location, sprite)`. Verificar em multiplayer 2 clientes (§9.3).
2. **D2/D3 fade e duração**: `alphaFade 0.02f → 0.0167f`, `1000f → ModConstants.TextDurationMs`. Teste visual: cronometrar ≈1000ms.
3. **`heldItem.Stack--` sem limpeza** (`:59`): stack 0 permanece até o tick do jogo; se futuro código ler `CurrentItem.Stack == 0` como válido, vira bug. Vanilla limpa sozinho — aceitar, mas não replicar o padrão em código novo.
4. **Dupla checagem de ID** (controller + service): intencional (UX rápida + testabilidade). Ao adicionar novo formato de ID, atualizar **ambos** (`ModController.cs:861-864` e `CompostApplicationService.cs:46-49`) + `ModConstants`.
5. **`OnButtonPressed` sem `ExecuteWithConcurrencyGuard`** (convenção `Controllers/AGENTS.md`): duplo right-click no mesmo tick pode aplicar 2× (consome 2, +30). Fora deste asset, mas o gate §9.2 o captura.
6. **Contradição spec**: assunção "single-player only" (`spec.md:214`) vs FR-012 `broadcastSprites` + `message.compost.action-conflict` (multiplayer). Adotar a FR (código futuro é multiplayer-aware: `HostAtomicLockService`, `TryAcquireLock` em `CompostingBinService.cs:41,122`).

---

## 7. Contexto de origem do item — bin e maturação (resumo cruzado)

O composto aplicado nasce em `CompostingBinService.CollectCompost` (`LivingRoots/Services/CompostingBinService.cs:75-116`): só do estado `Ready`, rendimento fixo `outputCount = 1` (`:87` — **diverge** de `machine-interaction.md:65` que promete `1 × MaturationLevel`; ou a spec ou o código está defasado — marcar para reconciliação, não mexer neste asset), via `new Object(CompostItemId, 1)` + `IPlayerInventory.TryAddItem`; inventário cheio = `Game1.showRedMessage("message.harvest.inventory-full")` literal (`:95`, bug i18n §1.3) + bin permanece `Ready` + `return 0`. Sucesso = bin→`Empty`, `MaturationLevel++` (teto 5, `:107-110`), som `"Ship"`.

- `AddWaste` (`:37-73`): cria bin via `CompostingBinFactory.CreateBin` (`LivingRoots/Services/CompostingBinFactory.cs:7-17`, default `Empty/Level 1/Idle 0`), exige `Empty` + `IOrganicWasteValidator`, carimba `InputTimestamp = TotalDays`, zera idle, sob lock atômico host (`TryAcquireLock`/`ReleaseLock`).
- `ProcessDayStart` (`:142-190`): Processing→Ready quando `elapsed >= MaturationDays` (`:161`); `ConsecutiveActiveDays >= MaturationIncrementDays (7)` → `MaturationLevel++` (`:170-176`); Empty → `ConsecutiveIdleDays++`, reset a 1 após `MaturationIdleResetDays (14)` (`:178-187`). `data-model.md:68-74` traduz em semanas (7/14/21/28 dias → níveis 2–5).
- `ModController.OnDayStarted` (`ModController.cs:826-843`) chama `ProcessDayStart` do bin e do decay sob o mesmo tick; `TryDislodgeBin` (`:1203-1248`) usa `Game1.createItemDebris(dislodgedItem, tile * 64f, -1, location)` (`:1243`) para dropar no chão quando o inventário está cheio — assinatura externa `createItemDebris(Item, Vector2 pixelPos, int direction, GameLocation)`, `-1` = direção aleatória (confiança alta, observada no repo; padrão vanilla de quebra de máquina).
- FR-019 (tooltip `Maturity: {0}x` via `Data/Machines`, `spec.md:151`): registrada em `ModEntry.cs:151-175`, mas a descrição do hover ainda não exibe a maturação — wiring pendente na 004, fora deste asset.

---

## 8. Testes e fixtures (o que cobre este asset)

`LivingRoots.Tests/CompostApplicationServiceTests.cs:37-156` — 6 fatos, todos com `GameStateFixture.Install()` no ctor (`:47`):

| Teste | Linha | Cobre |
|-------|-------|-------|
| `ValidTile_IncreasesHealth` | `:60-74` | saúde 50 → `UpdateHealth(+15)` 1×, `true` |
| `AtMaxHealth_ReturnsFalse` | `:78-92` | saúde 100 → `false`, `UpdateHealth` nunca |
| `InvalidLocation_ReturnsFalse` | `:96-106` | `Town/isFarm:false` → `false` (FR-015) |
| `NoCompostHeld_ReturnsFalse` | `:110-121` | mão `null` → `false` |
| `NonHoeDirtTile_ReturnsFalse` | `:125-135` | tile sem `HoeDirt` → `false` |
| `HealthCappedAtMax` | `:142-156` | saúde 90 → `true` + `UpdateHealth(+15)` (clamp a 100 no `SoilHealthService`) |

Load-bearing (doc no header `:15-36`): `GameStateFixture` + `NoOpSoundsHelperStub` (sem eles `playSound("cancel")` lança fora do jogo); `HoeDirt` exige `Game1.game1` alocado sem ctor (boot MonoGame); `ItemFactory` registra o tipo `(O)` para `QualifiedItemId == "(O)LivingRoots.Compost"`; `PlayerProviderStub.CurrentItem` substitui `Game1.player`. Âncora de constante: `CompostingBinConstantsTests.cs:17-19` (`RestorationAmount == 15f`).

---

## 9. Verificação (gate comportamental)

`dotnet build`/`dotnet test` cobrem lógica; feedback visual/sonoro exige jogo:

1. **Aplicação válida**: tile farm 50 + composto na mão + right-click → saúde 65, −1 composto, `"+15"` verde ≈1000ms subindo, sem `cancel`. Repetir em 90 → 100 (teto) e em 100 → `cancel` sem consumo.
2. **Recusas**: grama/não-tillado, `Town`, mão vazia → `cancel`, sem texto, sem consumo (pós-004: com `showRedMessage` nas 3 chaves, exceto mão-vazia).
3. **Multiplayer (pós-D1)**: host + farmhand na mesma location; farmhand aplica → ambos veem `"+15"` (prova `broadcastSprites`); duplo-clique simultâneo → 1 aplicação (prova lock; hoje sem guard — esperado 2× até o fix §6.5).
4. **Regressão grep**:
   ```bash
   grep -rn 'TemporarySprites.Add' LivingRoots/ --include="*.cs"   # deve sumir pós-D1 (broadcastSprites)
   grep -rn 'alphaFade = 0.02f' LivingRoots/ --include="*.cs"       # deve ser 0.0167f pós-D2
   grep -rn 'showRedMessage("[a-z]' LivingRoots/ --include="*.cs"  # nunca chave literal (bug §4.2)
   ```

---

## Apêndice A — mapa de evidências

| Afirmação | Evidência |
|-----------|-----------|
| `RestorationAmount = 15f`, `Maturation* = 2/5/14/7` | `LivingRoots/Constants.cs:58-62` |
| `TextDurationMs = 1000`, `TileSize = 64` | `Constants.cs:40,35` |
| Fluxo `TryApplyCompost` + 4 gates + sprite | `LivingRoots/Services/CompostApplicationService.cs:18-81` (gates `:22-54`, sprite `:61-75`) |
| Contrato | `LivingRoots/Domain/Interfaces/ICompostApplicationService.cs:9-20` |
| Chamador compost-first, fallthrough bin | `LivingRoots/Controllers/ModController.cs:861-884` |
| `CollectCompost` rendimento 1, bug chave crua, `Ship` | `LivingRoots/Services/CompostingBinService.cs:75-116` (`:87,95,112`) |
| `AddWaste` + lock atômico, `ProcessDayStart` maturação | `CompostingBinService.cs:37-73,142-190` |
| Factory default Empty/1/0 | `LivingRoots/Services/CompostingBinFactory.cs:7-17` |
| `createItemDebris` fallback em dislodge | `ModController.cs:1243` |
| `UpdateHealth` clamp, default 0 | `LivingRoots/Services/SoilHealthService.cs:565-600,521-542` |
| 6 testes + fixture load-bearing | `LivingRoots.Tests/CompostApplicationServiceTests.cs:37-156` |
| FR-004–FR-007 / FR-012 / FR-015 / FR-017 / FR-019 / FR-021 | `specs/002-soil-decay-compost/spec.md:131-154` (`:144,147,149,151,153`) |
| US2 cenários + edge cases + clarificações i18n/sprite/cancel | `spec.md:31-46,67-76,82-90` |
| Modelo `CompostingBinState`, maturação semanal, save esparso | `data-model.md:30-77`, `contracts/save-data-schema.md:150-170` |
| Interação bin, output `1×level` (diverge do código) | `contracts/machine-interaction.md:54-72,128-146` |
| T024–T026 (US2), T032 (i18n), gates | `specs/002-soil-decay-compost/tasks.md:99,129,139-142` |
| 11 chaves `message.*`, bug literal, receita `IMessageHud` | `specs/004-playable-compost-loop/briefings/i18n-localization.md:41-95,147-186,410-450` |
| Registro `Data/Objects|CraftingRecipes|Machines` via C# | `LivingRoots/ModEntry.cs:114-176` |
| i18n atual 11 chaves | `LivingRoots/i18n/default.json:1-13` |
| Translation fallback `pt-BR→pt→default`, `pt.json`, `reload_i18n`, `Get.Tokens.Default` | wiki Translation (rev. 2026-01-07, relida 2026-10-05) |
| `ButtonPressed`/`DayStarted`/`AssetRequested`/`LocaleChanged` semantics | wiki Events (relida 2026-10-05) |
| SMAPI multiplayer `SendMessage`/`PerScreen` (contexto p/ D1) | wiki Multiplayer (relida 2026-10-05) |

*Pesquisa: inspeção direta do repo (`CompostApplicationService.cs`, `CompostingBinService.cs`, `CompostingBinFactory.cs`, `ModController.cs:826-890,1203-1248`, `ModEntry.cs:114-176`, `SoilHealthService.cs:521-600`, `Constants.cs`, `i18n/default.json`, `CompostApplicationServiceTests.cs`, spec/plan/tasks/contracts/data-model 002, briefing i18n 004) + docs SMAPI wiki (Translation/Multiplayer/Events, rev. 2026-01-07, relidas integral em 2026-10-05). Verificação em jogo (duração do sprite, `broadcastSprites` 2 clientes, `pt.json` vs `pt-BR.json`) pendente — marcada como gate §9, não assumida.*
