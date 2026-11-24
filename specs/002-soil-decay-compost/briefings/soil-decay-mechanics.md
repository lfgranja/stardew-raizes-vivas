# Briefing — Soil Decay Mechanics (002-soil-decay-compost)

**Asset:** soil decay mechanics (`DailyDecayRate=2f` `Constants.cs:57`, `SeasonalDecayMultiplier` em `Domain/Services/`, bare tilled soil, monoculture penalty, `HoeDirt crop==null`/`dead`, `IsFarm||Greenhouse`, save schema `soil_health_data_{saveId}`).
**Escopo deste briefing:** só lógica + tuning + testes. Sem PNG, sem sprites, sem áudio, sem tooltip.
**Fontes internas lidas:** `specs/002-soil-decay-compost/spec.md`, `plan.md`, `research.md:27-39`, `data-model.md`, `contracts/save-data-schema.md`, `contracts/machine-interaction.md`, `LivingRoots/Constants.cs:55-62`, `LivingRoots/Services/SoilDecayService.cs`, `LivingRoots/Domain/Services/SeasonalDecayMultiplier.cs`, `LivingRoots/Domain/Interfaces/ISoilDecayService.cs`, `LivingRoots/Services/CompostApplicationService.cs`, `LivingRoots/Services/SoilHealthService.cs`, `LivingRoots/Services/SeasonProvider.cs`, `LivingRoots/Services/LocationProvider.cs`, `LivingRoots/Controllers/ModController.cs:826-890`, `LivingRoots.Tests/SoilDecayServiceTests.cs`, `SeasonalDecayMultiplierTests.cs`, `CompostApplicationServiceTests.cs`, `Tests/Fixtures/GameLocationFixture.cs`.
**Fontes externas consultadas:** `StardewValley/TerrainFeatures/HoeDirt.cs` (raw, repo `veywrn/StardewValley`, ~1022 linhas), wiki `Modding:Modder Guide/APIs/Events` (GameLoop), wiki `Crops` (dead crops keep soil tilled; scythe removal), wiki `Modding:Crop data` (`CountForMonoculture`), Nexus `Tilled Soil Decay` / `No Soil Decay On Farm` (vanilla untill 10%).

---

## 1. Definição canônica (o que "decay" significa aqui)

Existe **colisão terminológica** que este briefing resolve de forma explícita:

| Termo | Vanilla SDV | Este mod (002) |
|---|---|---|
| "Soil decay" vanilla | `HoeDirt` some do mapa overnight (untill). Na farm, chance base ~10% por noite para solo **sem água** (mods Nexus `Tilled Soil Decay`/`No Soil Decay` documentam e patcheiam exatamente isso; config `DryingRateMultiplier`/`Delay`). Solo some → fertilizante junto. | **Não é isso.** O mod não mexe em untill, `state` (dry/watered), nem `fertilizer`. |
| "Soil decay" do mod | Inexistente no vanilla. | Decremento numérico de **health 0–100** (`float`) por tile arado e **bare**, uma vez por dia, em `GameLoop.DayStarted`. Valor persiste em `soil_health_data_{saveId}` via `SoilHealthService`. Visualização (001) apenas lê esse número. |

Regra canônica (spec FR-001/FR-002/FR-003, `spec.md:133`):

```
se (tile é HoeDirt && crop == null) em Farm (qualquer IsFarm) → health -= DailyDecayRate * SeasonalMultiplier
teto/piso: clamp [0, 100] (piso em FR-002, teto em FR-005 para restauração)
Greenhouse: isento de decay (FR-001), mas compost é permitido lá (FR-015)
fertilizante sozinho NÃO isenta; só crop vivo OU resíduo morto isenta
```

"Bare" resolvido em `spec.md:76,124`: **bare = sem crop vivo E sem resíduo morto**. Resíduo = `crop != null && crop.dead` (efeito mulch, agroecologia real). Essa é a única semântica de "bare" válida; qualquer check `crop == null` sem considerar `.dead` está incompleto por spec, mesmo que o resultado prático coincida (ver §5).

---

## 2. Onde a lógica mora hoje (código real, não plano)

### 2.1 Constantes — `LivingRoots/Constants.cs:55-62`

```csharp
public const float DailyDecayRate = 2f;          // :57
public const float RestorationAmount = 15f;      // :58
public const int MaturationDays = 2;
public const int MaturationMaxLevel = 5;
public const int MaturationIdleResetDays = 14;
public const int MaturationIncrementDays = 7;
public const float MinSoilHealth = 0f;           // :10
public const float MaxSoilHealth = 100f;         // :11 (era 10, mudado p/ 100 p/ alinhar docs)
```

Tudo que é tuning de decay/restauração **MUST** vir daqui (constitution check `plan.md:29`). Nenhum número mágico nos services.

### 2.2 `SeasonalDecayMultiplier` — `LivingRoots/Domain/Services/SeasonalDecayMultiplier.cs:18-27`

Classe pura, sem SMAPI (conforme `Domain/AGENTS.md`: framework-agnostic). `switch` case-insensitive:

| season string | retorno |
|---|---|
| `"spring"` | `0.5f` |
| `"summer"` | `1.5f` |
| `"fall"` | `0.5f` |
| `"winter"` | `0.0f` |
| qualquer outro | `0.5f` (default) |

Fonte da string: `SeasonProvider.CurrentSeason => Game1.currentSeason` (`Services/SeasonProvider.cs:13`), lowercased no call-site (`SoilDecayService.cs:42`).

**Nuance externa:** o jogo tem `GameLocation.GetSeasonForLocation()` (usado por `HoeDirt.dayUpdate` e `plant()` para Greenhouse/Island/Desert). O mod usa `Game1.currentSeason` global, **não** por-location. Para Farm isso é equivalente; para Greenhouse o código nem chega a consultar a estação (early-return); para Island/Desert/custom locations com estação própria, o multiplicador global pode divergir do `GetSeasonForLocation()`. Nenhum teste cobre isso. Se um dia o decay for estendido além de Farm+Greenhouse, trocar `ISeasonProvider` por lookup por location.

### 2.3 `SoilDecayService.ProcessDayStart` — `LivingRoots/Services/SoilDecayService.cs:27-80`

Fluxo exato, na ordem:

1. `if (locationName == "Greenhouse")` → log Trace + return. **Comparação por nome literal**, não `location.IsGreenhouse`. Funciona para a Greenhouse vanilla; Greenhouse custom/mod com outro nome não é isenta.
2. `location = _locationProvider.GetLocationByName(locationName)`; se `null` → Trace + return.
3. `season = _seasonProvider.CurrentSeason.ToLowerInvariant()` → `multiplier = GetMultiplier(season)`.
4. `if (multiplier == 0f)` → Trace "Winter - no decay" + return. Ou seja, **inverno short-circuita antes de iterar tiles** (economia correta; nenhum read/write).
5. `decayAmount = ModConstants.DailyDecayRate * multiplier` (float; `CheckForOverflowUnderflow=true` project-wide).
6. Loop `foreach (var kvp in location.terrainFeatures.Pairs)`:
   - `if (kvp.Value is HoeDirt hoeDirt)` → `bool isBare = hoeDirt.crop == null`.
   - Se bare: `currentHealth = GetSoilHealth(locationName, tile)` → `newHealth = Math.Max(current - decay, 0f)` → `actualDecay = current - new` → só chama `UpdateHealth(locationName, tile, -actualDecay)` se `actualDecay > 0f`.
   - Contadores `tilesProcessed++` (todos os bare, mesmo já zerados) e `totalHealthLost += actualDecay`.
7. Log Trace único com `tilesProcessed, totalHealthLost:F1, season, multiplier, decayAmount:F1` (FR-022: só Trace/Debug, sem spam).

Detalhes que importam:

- **Iteração é sobre `terrainFeatures.Pairs`** (Netcode `NetVector2Dictionary`), não sobre o cache de health. Tiles arados que nunca tiveram health registrada retornam `0f` de `GetSoilHealth` e são pulados sem write (bom: não polui o sparse cache — ver §4).
- **Zero é esparso:** `UpdateHealth` com delta que levaria a 0 remove a entrada do cache (`SoilHealthService.SetHealthInternal`, `SoilHealthService.cs:663-672`). Save não guarda zeros. Decay até 0 é, portanto, também "limpeza".
- **`UpdateHealth` recebe `-actualDecay`**, não `-decayAmount`. Isso faz o clamp em duas camadas (service calcula `Max(current-decay,0)` e `SoilHealthService.ClampHealthValue` re-clampa). Redundante mas inofensivo; garante que `NaN/Infinity` vindos de save corrompido não propagam (clamp trata em `SoilHealthService.cs:698-717`).
- **Sem async, sem lock próprio:** handler é síncrono dentro de `DayStarted` (SMAPI roda handlers sincronamente; ver §6). `SoilHealthService` tem lock interno próprio.
- **Sem filtro de `state`/`fertilizer`/`isGreenhouseDirt`:** correto por spec (fertilizante não isenta). `state` (dry=0/watered=1) é ignorado — decay independe de rega. Decisão de design, não bug: documentado aqui porque jogador vanilla espera que "regar protege"; neste mod, não.

### 2.4 Quem chama — `ModController.OnDayStarted` (`ModController.cs:826-843`)

```csharp
var locationName = Game1.currentLocation?.Name;
if (!string.IsNullOrEmpty(locationName))
{
    _compostingBinService.ProcessDayStart(locationName);
    _soilDecayService.ProcessDayStart(locationName);
}
```

**Discrepância load-bearing (documentada, não corrigida neste briefing):** o método recebe **só `Game1.currentLocation`** (onde o jogador está dormindo/acordando), não todas as farm locations. Spec FR-001 fala "all tilled tiles", `plan.md:25` fala "up to 10,000 tilled tiles per farm", e `tasks.md:T022` diz "iterates `location.terrainFeatures`" (singular). Na prática: Farm principal é coberta quando o jogador acorda na Farm/casa; tiles em outras locations `IsFarm` (ex.: segunda fazenda modded, Island farm?) **não decaem** se o jogador acordar fora delas. Testes unitários não pegam isso porque injetam `ILocationProvider` mockado por nome. Ação recomendada (fora do escopo deste briefing): iterar `Game1.locations` filtrando `IsFarm` (excluindo Greenhouse por nome/`IsGreenhouse`) ou esclarecer na spec que decay é só-current-location.

Ordem dentro do handler: bins primeiro, decay depois. Sem dependência entre eles hoje.

`OnButtonPressed` (`ModController.cs:845-890`): right-click com compost no cursor (`GrabTile`) → `TryApplyCompost(location, tile)`; senão, lógica de bin (add waste se `Empty` + held item; collect se `Ready` + mãos vazias). Roteamento por `QualifiedItemId`/`ItemId` aceitando `LivingRoots.Compost` e `(O)LivingRoots.Compost`.

### 2.5 Contrapartida (restauração) — `CompostApplicationService.TryApplyCompost` (`CompostApplicationService.cs:18-81`)

Gate order (falha rápida, cada uma com `Game1.playSound("cancel")` + Trace, FR-017):

1. `location.IsFarm || Name == "Greenhouse"` senão rejeita sem consumir.
2. `terrainFeatures.TryGetValue(tile) is HoeDirt` senão rejeita (não precisa ser bare — compost pode ir em tile com crop; spec não proíbe).
3. `currentHealth >= MaxSoilHealth` senão rejeita sem consumir (FR-007).
4. Held item é compost (`QualifiedItemId` == `LivingRoots.Compost` ou `(O)LivingRoots.Compost`, ou `ItemId` igual) senão rejeita.
5. Sucesso: `UpdateHealth(+RestorationAmount)` (clamp interno garante teto 100), `heldItem.Stack--`, sprite `TemporaryAnimatedSprite` texto `"+15"` verde, `alphaFade 0.02f`, `motion (0,-0.5)`, `location.TemporarySprites.Add` (nota: spec FR-012 manda `Game1.multiplayer.broadcastSprites`; implementação usa `Add` local — funciona SP, que é o escopo declarado single-player; MP exigiria broadcast).

---

## 3. Referência externa SDV 1.6 (o que o vanilla garante)

### 3.1 `HoeDirt` — campos relevantes (decompiled 1.6, `TerrainFeatures/HoeDirt.cs`)

- `public Crop crop { get => netCrop; set => netCrop.Value = value; }` — `NetRef<Crop>`, sincronizado em MP. `null` = sem planta. **Qualquer `crop != null`, vivo ou morto, mantém o tile como "ocupado"** para efeitos de `isBare` do mod.
- `public readonly NetInt state` — `dry=0 / watered=1 / invisible=2`. Controlado por rega/chuva/`dayUpdate` (reseta para 0 salvo paddy/fertilizante retenção). O mod ignora.
- `public readonly NetInt fertilizer` — `0` = nenhum; senão ID do fertilizante (368/369/465/466/918/919/370/371/920). Em 1.6, volta a `null` internamente quando vazio (nota de migração 1.6). O mod ignora (correto por spec).
- `public readonly NetBool isGreenhouseDirt` — setado em `initialize(location)` a partir de `location.IsGreenhouse`, reforçado em `dayUpdate` (`if (environment.IsGreenhouse) isGreenhouseDirt = true`). Poderia ser usado como isenção alternativa/complementar ao nome literal (ver §7).
- `performToolAction`: scythe (`MeleeWeapon.isScythe()`) com `crop.dead == true` → `destroyCrop` (vira bare). Pickaxe com `crop == null` → retorna `true` (untill — remove o próprio HoeDirt). Hoe em crop → `hitWithHoe` pode destruir.

### 3.2 `Crop.dead` — semântica

- `NetBool dead`. Setado por `Crop.newDay` (chamado de `HoeDirt.dayUpdate`) quando: mudança de estação para cultura fora de estação (1º do mês mata tudo não-multissazonal), raio em tempestade, ou dano 50. `readyForHarvest()` exige `!crop.dead`.
- Wiki `Crops` confirma comportamento que a spec usa como premissa agroecológica: **planta morta deixada no tile mantém o solo arado** (não precisa re-arar no dia 1), removível com scythe. É exatamente o "mulch protege" de FR-003. Truque conhecido de trigo (plantar antes do dia 25 do verão, colher com scythe no dia 1 do outono) depende disso.
- Implicação para tuning/testes: no dia 1 de cada estação, N tiles viram `crop.dead != null` de uma vez → N tiles saem do pool de decay automaticamente. Decay de outono "poupa" quem deixou resíduo — estratégia válida, intencional.

### 3.3 `GameLocation.terrainFeatures` — coleção

- `NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>>`: chave `Vector2` (coordenada tile), valor `TerrainFeature`. `HoeDirt` é um dos subtipos (outros: `Tree`, `FruitTree`, `Grass`, `Flooring`, `HoeDirt`). Iteração `.Pairs` no service é o padrão vanilla (o próprio `draw`/`dayUpdate` do jogo itera assim).
- `GameLocationFixture` (`Tests/Fixtures/GameLocationFixture.cs:38-72`) documenta por que testes usam `new GameLocation()` parameterless (construtor com mapa chama `reloadMap` → NRE sem `Game1`) e populam `name`/`isFarm` via backing `NetFields` (`Location.name.Value`, `Location.isFarm.Value`) porque 1.6 não expõe setter. `AddHoeDirtTile(x, y, hasCrop)` cria `HoeDirt` real; `hasCrop:true` põe `new Crop()` (vivo). **Não há helper para crop morto** — gap de teste (§5).

### 3.4 Day-update loop vanilla vs. hook do mod

Vanilla (noite→manhã), simplificado:

```
Game1.newDay (overnight) → para cada location: terrainFeatures.dayUpdate (HoeDirt.dayUpdate: crop.newDay, winter-kill, dry-out)
  → season change (dia 1): seasonUpdate (limpa fertilizer se crop null/dead/fora de estação), wither em massa
  → SMAPI GameLoop.DayStarted (DEPOIS de tudo vanilla; docs: "Everything has already been initialised")
```

O mod roda em `DayStarted`, ou seja, **vê o estado pós-vanilla**: `crop` já é o do novo dia, `currentSeason` já é a nova estação, dead-crops do dia 1 já estão `dead`. Isso valida a decisão `research.md:47-55` (ler `Game1.currentSeason` no day-start) e a edge "transição cropped→bare no mesmo dia só decai no próximo day-start" (`spec.md:74`).

SMAPI garante (wiki Events): handlers rodam **sincronamente**, jogo pausado, sem concorrência entre mods; código rápido não é percebido. `DayStarted` também dispara ao conectar em MP como farmhand — testes `MultiplayerDayAdvanceTests_T019` existem para bins; decay em MP herda single-player-only assumido (`plan.md:31`, `spec.md:213`).

---

## 4. Persistência (save schema) — `soil_health_data_{saveId}`

Contrato em `contracts/save-data-schema.md:11-58`; implementação em `SoilHealthService.cs`.

- Chave: `ModConstants.KeyPrefix ("soil_health_data_") + sanitized saveId` (`Constants.cs:9`, `SoilHealthService.GetSaveKey`, `SoilHealthService.cs:719-782`). Sanitização: FormC, limite `MaxSaveIdLength=200`, `MaxDataKeyLength=200`, sem `ToLowerInvariant` (case-sensitive FS).
- Formato: `{ LocationHealthData: { "<location>": { "X,Y": float } } }`. Coordenadas como string `"X,Y"` (int parse invariante, `ProcessTileEntry`, `:805-851`); health `float` clampado `[0,100]`, `NaN→0`, `±Infinity→piso/teto`.
- **Esparso:** zeros (`|v|<0.0001`) nunca são armazenados nem em runtime (`SetHealthInternal`, `:663`) nem no snapshot de save (`ProcessLocationForSave`, `:483-487`). Locations vazias são removidas. Efeito no decay: tile que chega a 0 some do save (economia) e `GetSoilHealth` retorna 0 para ausente (`:521-542`).
- Limites DoS: `MaxTilesPerLocation=500`, `MaxLocationsPerSave=50`, `MaxTilesPerSave=30000`, coordenadas `±10000` (`Constants.cs:14-22`). Load aborta e **recusa o Save subsequente** (`_loadAbortedForLimits`) para não sobrescrever disco com estado parcial (`:362-403`). Decay respeita indiretamente: `UpdateHealth` recusa criar tile além do limite (`TileLimitExceeded`, `:689`), log Warn.
- Threading: lock em `_lock`, snapshot-fora-do-lock no save (`CreateSaveSnapshot`, `:405`), sem `.Result/.Wait()` em nenhum caminho.
- Bins usam chave separada `composting_bins_{saveId}_{location}` (fora do escopo deste briefing, mas acoplada no mesmo `OnDayStarted`).

---

## 5. "Monoculture penalty", dead-residue e o estado real do código

Três afirmações do asset precisam de correção/precisão:

1. **Monoculture penalty NÃO existe em 002.** Busca na codebase retorna zero implementações fora de `specs/future-roadmap/briefings/mid-game-year2-3.md:75` (`MonoculturePenaltyHealth = −6f`, `RotationHistoryDepth = 3` — roadmap futuro) e `ROADMAP.md:16` (bullet aspiracional). O único "monoculture" vanilla é o flag `Data/Crops.CountForMonoculture` (conquistas de monocultura do jogo, sem efeito em soil). **Qualquer plano de tuning que assuma penalidade por repetição de cultura em 002 está errado.** Registrar como deferred, não como bug.
2. **Dead-residue é protegido por implicação, não por branch explícito.** `SoilDecayService.cs:59` testa só `hoeDirt.crop == null`. Como `crop.dead` vive dentro de `crop != null`, tile com resíduo cai no `isBare=false` e não decai — comportamento conforme FR-003, mas **sem mencionar `.dead` no código**. `tasks.md:T022` descreve "`crop.dead` for residue", o que o código não contém literalmente. Risco: refatoração futura que queira tratar "morta conta como bare" quebraria a spec silenciosamente. Recomendação: adicionar `bool hasResidue = hoeDirt.crop != null && hoeDirt.crop.dead;` explícito (igual ao snippet de `research.md:33-38`) + teste dedicado, sem mudar comportamento.
3. **Cobertura de teste confirma o gap do item 2.** `SoilDecayServiceTests` (6 testes, `SoilDecayServiceTests.cs:63-161`): bare-spring decai `-1f`; low-health summer `-3f` (clamp delegado); zero-health sem write; spring-vs-summer 1f-vs-3f; winter sem write; **covered (crop vivo) sem write**. Não há teste com `crop.dead == true`. `GameLocationFixture.AddHoeDirtTile` nem aceita criar dead crop. `tasks.md:T013` reivindica "assert crop and dead crop mulch prevent decay" como `[x]` feito — **parcialmente impreciso**: só o caso vivo está assertado. Ação: 1 teste `ProcessDayStart_DeadCrop_NoDecay` (setar `hoeDirt.crop.dead.Value = true` — `NetBool`, precisa de `GameStateFixture` como os demais).

---

## 6. Tuning — números fechados e o que significam em jogo

Base: `DailyDecayRate = 2f`, `RestorationAmount = 15f`, `MaxSoilHealth = 100f`.

### 6.1 Decay efetivo por dia/estação

| Estação | mult | decay/dia bare | dias p/ zerar 100→0 | perda em 28 dias |
|---|---|---|---|---|
| Spring | 0.5x | **1.0** | 100 dias (~3.5 estações) | **28** |
| Summer | 1.5x | **3.0** | ~34 dias (~1.2 estação) | **84** |
| Fall | 0.5x | **1.0** | 100 dias | **28** |
| Winter | 0.0x | **0** | ∞ (congelado) | **0** |
| Desconhecida | 0.5x | 1.0 | — | — |

Leitura de design (intenção da spec `spec.md:101,107`): casual-friendly na primavera/outono (perde ~1/4 da barra no mês), verão pune negligência (quase zera em 1 estação bare), inverno é pausa gratuita. Coerente com "small enough to not punish casual players, large enough to matter over a season (≈28 points/month)" — o "≈28" é exatamente spring/fall; summer é 3x isso por heat-stress deliberado.

### 6.2 Economia compost vs. decay (break-even)

`15f` por aplicação ⇒ 1 compost cobre:

- spring/fall: **15 dias** de bare-decay (15/1.0).
- summer: **5 dias** (15/3.0).
- Para segurar 1 tile bare o mês inteiro sem plantar: ~2 composts (spring/fall, 28/15) ou ~6 composts (summer, 84/15).
- Bin produz 1 waste → 1–5 composts em 2 dias (maturação). Em steady-state maduro (5x), 1 ciclo de 2 dias banca ~2.5 meses de spring-decay de 1 tile, ou ~25 dias de summer. Early-game (1x), 1 ciclo banca exatamente o break-even de spring (15 ≈ 15 dias). **Tuning fecha:** o jogador que compostar continuamente desde a semana 1 nunca perde para o decay fora do verão; no verão precisa de escala (2+ bins ou plantar para cobrir).

### 6.3 Performance

Linear `O(n)` sobre `terrainFeatures.Pairs` com 1 branch `is` + 1 dict lookup por tile. Spec `spec.md:113`/`plan.md:25`: alvo 10k tiles, medido 40k em ~2–4ms — folga de >4x dentro do frame de 16.67ms (que De qualquer forma não se aplica: `DayStarted` não é per-frame). Sem partição espacial, sem cache — YAGNI correto. O early-return de winter e o skip-write-em-zero (§2.3) são as únicas otimizações e bastam.

### 6.4 Sensibilidade (o que mexer se playtest reclamar)

- Decay muito punitivo no verão → mexer no **multiplicador** (1.5→1.25), não na base 2: preserva o "≈28/mês" de spring/fall que ancora SC-001.
- Compost fraco → `RestorationAmount` 15→20 muda break-even de summer 5→~7 dias; 15 é múltiplo "limpo" para o floating text `"+15"`.
- Granularidade: tudo `float`; `totalHealthLost:F1` no log. Tiles com health fracionária (ex. 72.5 no schema example) decaem normalmente.

---

## 7. Riscos, edge cases e decisões a registrar

1. **Escopo de locations do decay (`ModController.cs:832`).** Só `currentLocation`. Aceitar como "farm principal coberta no caso comum" ou corrigir para loop em `IsFarm`. Qualquer uma das duas exige atualizar FR-001 ("all tilled tiles") para dizer qual conjunto.
2. **Isenção Greenhouse por nome (`== "Greenhouse"` em 2 lugares: `SoilDecayService.cs:29`, `CompostApplicationService.cs:22`).** Alternativa vanilla: `location.IsGreenhouse` / `HoeDirt.isGreenhouseDirt`. Nome literal quebra para Greenhouse renomeada/modded; `IsGreenhouse` cobre. Compost usa `IsFarm || nome` — consistente entre os dois services, mas inconsistente com o resto do jogo. Recomendação: centralizar `IsFarmOrGreenhouse(location)` num helper/domain service.
3. **Fertilizante vs. decay.** Ignorado por design (FR-003). Jogador pode achar bug ("adubei e mesmo assim decay"). Mitigação é UX/docs, não código.
4. **Fruit trees / indoor pots / Garden Pots.** FR-003 cita "fruit tree" como isento, mas `HoeDirt.crop` nunca é fruit tree (árvores são `FruitTree` terrain feature separada, não `HoeDirt.crop`) — a isenção é automática/vacuosa. Sem ação, mas a FR induz leitura errada.
5. **Save/load do decay em si:** nenhum timestamp; decay é função pura do estado no `DayStarted`. Reload no meio do dia não duplica decay (só roda no day-start). Morrer/sleep multi-day? Vanilla avança 1 dia por sleep; cada `DayStarted` aplica 1x — correto.
6. **Log sem spam:** 1 linha Trace/dia/location com agregados. Em farm com 0 bare tiles, ainda loga `Processed 0` — aceitável e útil para debug.
7. **Segurança:** `GetSoilHealth/UpdateHealth` validam `locationName` (null/whitespace/len>100) e coordenadas (NaN/Inf/bounds ±10000) — decay com tile key vindo do próprio jogo sempre passa; o risco real (save malicioso) é tratado no load, não no loop.

---

## 8. Matriz de testes (o que existe e o que falta) — só decay

| # | Teste | Arquivo:linha | Cobre | Status |
|---|---|---|---|---|
| D1 | `BareTile_DecaysHealth` (spring 50→`-1f`) | `SoilDecayServiceTests.cs:64` | FR-001, SC-001 | ✅ |
| D2 | `LowHealth_DoesNotGoNegative` (summer 1→`-1f`) | `:79` | FR-002 (piso, clamp delegado) | ✅ |
| D3 | `AtZeroHealth_StaysAtZero` (sem write) | `:94` | FR-002 + sparse (§4) | ✅ |
| D4 | `SummerDecay_HigherThanSpring` (`-1f` vs `-3f`) | `:109` | SC-001 (2 estações) | ✅ |
| D5 | `Winter_NoDecay` (sem write) | `:130` | mult 0x | ✅ |
| D6 | `CoveredTile_NoDecay` (crop vivo) | `:145` | FR-003 (metade) | ✅ |
| D7 | `GetMultiplier_*` ×5 (spring/summer/fall/winter/invalid) | `SeasonalDecayMultiplierTests.cs:15-43` | SC-001 base | ✅ |
| D8 | Compost válida `+RestorationAmount`, max rejeita, location inválida, sem compost, non-HoeDirt, cap 90→ok | `CompostApplicationServiceTests.cs:59-156` | FR-004–007, FR-015, SC-002/003 | ✅ |
| **G1** | `DeadCrop_NoDecay` (`crop.dead=true` → sem write) | — | FR-003 (outra metade), `tasks.md:T013` | ❌ ausente |
| **G2** | Fall-multiplier e2e (só unitário D7 cobre fall) | — | SC-001 "all four seasons" | ⚠️ parcial (unit ok, e2e não) |
| **G3** | Multi-location (decay só-current-location, §7.1) | — | FR-001 "all tilled" | ❌ ausente (é o bug candidato) |
| **G4** | 10k-tile perf assert | — | SC-006 | ⚠️ alegado em spec (`~2-4ms`), sem teste de perf no repo |

Critério de aceite deste briefing para SC-001/SC-003/SC-006: D1–D8 passam; G1 é o único gap unitário barato (1 teste + flag `hasDeadCrop` no fixture); G3 exige decisão de escopo antes de teste.

---

## 9. Referências cruzadas (navegação)

- Spec: `specs/002-soil-decay-compost/spec.md` — FR-001/002/003 (decay), FR-004–007 (restore), FR-015 (farm+Greenhouse), SC-001/003/006, clarifications 2026-09-06/09 (2/dia, 15/aplicação, Greenhouse isenta, bare=mulch).
- Plan: `specs/002-soil-decay-compost/plan.md:22-34,104-117` — alvos perf, research outcomes (HoeDirt check, `Game1.currentSeason` timing).
- Research: `specs/002-soil-decay-compost/research.md:25-61` — snippet `HoeDirt`/`crop.dead`, tabela de seasons (spring 0.5/summer 1.5/fall 0.5/winter 0).
- Data model: `specs/002-soil-decay-compost/data-model.md:7-27,153-169` — `SoilHealthTile`, exemplo JSON `soil_health_data`.
- Contracts: `contracts/save-data-schema.md` (JSON schema + sparse + clamps), `contracts/machine-interaction.md` (fora do escopo decay, lido para contexto).
- Código: `LivingRoots/Constants.cs:55-62` · `LivingRoots/Services/SoilDecayService.cs` · `LivingRoots/Domain/Services/SeasonalDecayMultiplier.cs` · `LivingRoots/Domain/Interfaces/ISoilDecayService.cs` · `LivingRoots/Services/CompostApplicationService.cs` · `LivingRoots/Services/SoilHealthService.cs` (persistência/clamp/sparse/DoS) · `LivingRoots/Controllers/ModController.cs:826-890` (wiring) · `LivingRoots/Services/SeasonProvider.cs` · `LivingRoots/Services/LocationProvider.cs`.
- Testes: `LivingRoots.Tests/SoilDecayServiceTests.cs` · `SeasonalDecayMultiplierTests.cs` · `CompostApplicationServiceTests.cs` · `Tests/Fixtures/GameLocationFixture.cs`.
- Externo: `HoeDirt.cs` (`crop` NetRef, `state`, `fertilizer`, `isGreenhouseDirt`, `dayUpdate`, `seasonUpdate`, scythe-clears-dead) · wiki Events (`DayStarted` pós-vanilla, síncrono) · wiki `Crops` (dead keeps tilled) · `Modding:Crop data` (`CountForMonoculture` ≠ penalty) · Nexus `Tilled Soil Decay` (untill 10% vanilla — não confundir).
