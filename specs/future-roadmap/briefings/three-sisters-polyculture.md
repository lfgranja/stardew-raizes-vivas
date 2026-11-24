# Briefing 1:1 — Three Sisters / Simple Polyculture (Epic 3)

> Status: briefing (não implementado). Único asset do ROADMAP Early sem briefing individual dedicado (nenhum `*sister*/*polyculture*/*companion*` em `specs/` — glob vazio na origem).
> Escopo interno: `ROADMAP.md:38` ("Simple Polyculture (Companion Planting): Introduction to 'The Three Sisters' concept (Corn, Snap Beans, and Pumpkin) for growth bonuses and soil improvement") + `AGROECOLOGY_CONCEPTS.md:32-34` (policultura/companion planting, sinergia) · Epic 3 (Issue #3) — US-03-01 Detect Three Sisters Configuration (#28) / US-03-02 Implement Three Sisters Bonuses (#29) em `LivingRoots/docs/EPICS_USER_STORIES.md:21-25`.
> Não promover para `specs/` sem `/speckit.specify`.
> Nome do arquivo: `three-sisters-polyculture.md` (segue a convenção kebab dos irmãos `heirloom-seeds.md`, `mulch-soil-cover.md`, `rotation-journal.md`, `green-manure.md`; alternativa `three-sisters-companion-planting.md` rejeitada — "polyculture" é o termo do ROADMAP:38 e de `AGROECOLOGY_CONCEPTS.md:33`).

## 1. Conceito

Milho tutor + feijão fixa N + abóbora cobre o solo — o consórcio indígena meso/ norte-americano (milpa; Haudenosaunee *Diohe'ko*, "as que nos sustentam"). Funções no jogo, espelhando a ecologia real (USDA-NAL; Mt. Pleasant; Cornell Garden-Based Learning):

| Irmã | Papel ecológico | Papel no jogo |
|---|---|---|
| Corn (milho) | tutor — estrutura p/ o feijão subir | centro estrutural do trio; `IsRaised=false`, atravessável |
| Snap Beans (feijão) | fixa N via rizóbios; estabiliza o milho no vento | fonte do bônus de N: +solo e +crescimento vizinhos |
| Pumpkin (abóbora) | folhas largas = living mulch: sombra, umidade, anti-ervas | cobre o solo: reduz decay no micro-trio + suprime weeds (spec detalha) |

Regra mental (US-03-01 → US-03-02): **trio completo adjacente = bônus pleno** (crescimento + qualidade + solo); **dupla = bônus parcial**; **solitária = neutra** (nunca penalidade — policultura premia, rotação pune; cf. `rotation-journal.md`).

### Decisão central: sementes heirloom sisters NOVAS, não reutilizar as vanilla

Reutilizar as três culturas vanilla (tese 80/20 óbvia: zero asset novo) é **rejeitado** como caminho principal, por três motivos verificados:

1. **Incompatibilidade sazonal vanilla** (wiki `Corn` / `Green_Bean` / `Pumpkin`, verificadas 2026-10-05): milho = summer+fall (14d, regrow 4d); feijão verde = **spring-only** (10d, regrow 3d, `IsRaised=true` trellis); abóbora = **fall-only** (13d, giant-capável). O trio completo só coexiste… nunca no campo aberto Y1 (feijão × abóbora não têm estação comum; milho+abóbora só no fall). "Plante as Three Sisters" seria missão impossível 2/3 do ano.
2. **Trellis bloqueia consórcio**: `Green Bean` vanilla é `IsRaised=true` (não-atravessável) — o feijão-tutorado-em-monte histórico é arbustivo/trepador no próprio milho, não treliça intransponível. Uma heirloom *bush/pole bean* (`IsRaised=false`) resolve pathing + leitura visual do monte.
3. **Nunca editar `Data/Crops` vanilla** (regra transversal de `rotation-journal.md:35` e `mid-game-year2-3.md:72`): dar `Seasons` comuns ou `CustomFields` às culturas vanilla exigiria `EditData` sobre chaves vanilla (`487`/`473`/`490`) — proibido. Sementes do mod com `CustomFields: { "LivingRoots.Companion": "SisterCorn|SisterBean|SisterPumpkin" }` seguem o padrão canônico sem tocar no vanilla.

**Decisão**: criar 3 culturas heirloom sisters via o pipeline de `heirloom-seeds.md` (semente→crop→produto), `Seasons: ["summer", "fall"]` unificadas (janela jogável Y1 após a primavera tutorial; spec trava — alternativa `spring+summer+fall` se playtest pedir), com o serviço de detecção aceitando **tanto os IDs heirloom quanto os vanilla** (tabela de equivalência por espécie, não por ID — milho vanilla no fall ao lado de abóbora vanilla conta como dupla parcial; spec define a matriz completa).

## 2. Sprite specs (wiki `Modding:Crop_data` + `Modding:Objects`, verificadas 2026-10-05)

| Asset | Spec | Qtd |
|---|---|---|
| Semente (item) | **16×16** cada; envelope de papel com broto verde (mesma identidade `heirloom-seeds.md` — irmãs = sub-linha heirloom, envelope com faixa tricolor 🌽🫘🎃) | 3 |
| Produto | **16×16** cada; variação heirloom sutil (milho multicolorido parcial, feijão em vagem, moranga listrada — charme crioulo vs. vanilla) | 3 |
| Cultura (crop sheet) | 1 linha por cultura em `Assets/Crops/sisters_crops.png`; fases = `DaysInPhase.Count` sprites lado a lado (padrão `TileSheets\crops`: cada fase ~16×32; 1 crop por linha via `SpriteIndex`) | 3 linhas |

Arquivos: `LivingRoots/Assets/sisters_seeds.png` (3 ícones), `LivingRoots/Assets/sisters_produce.png` (3 ícones), `LivingRoots/Assets/Crops/sisters_crops.png` (`Texture` custom por cultura, `SpriteIndex` = linha 0/1/2).
Overlays/feedback: reutilizar `IVisualizationService` (borda `HealthyColor #15803D` em tile com trio completo; selo parcial `SunColor #FACC15`; pattern `Solid` ≥0.7 p/ daltônicos — padrão `mulch-soil-cover.md`). Sem asset novo de partícula (reutilizar partícula marrom do compost no plantio).

## 3. Data/* — JSON/C# (registro via `ModEntry.OnAssetRequested`, padrão `ModEntry.cs:114-177`)

```csharp
// Data/Objects — semente + produto (exemplo milho-irmã; replicar ×3: milho/feijão/abóbora)
data["LivingRoots_SisterCornSeeds"] = new ObjectData
{
    Name = "Sister Corn Seeds",
    DisplayName = "{{i18n:sisters.corn.seeds.name}}",
    Description = "{{i18n:sisters.corn.seeds.description}}",
    Type = "Seeds", Category = -74, // seed category vanilla (cf. heirloom-seeds.md:30)
    Price = 40,
    Texture = "Mods/LivingRoots/SistersSeeds", SpriteIndex = 0,
    ContextTags = new[] { "heirloom_seed", "sister_seed", "seed_item" },
};
data["LivingRoots_SisterCorn"] = new ObjectData
{
    Name = "Sister Corn",
    DisplayName = "{{i18n:sisters.corn.name}}",
    Description = "{{i18n:sisters.corn.description}}",
    Type = "Basic", Category = -75, // vegetable
    Price = 60, // ~milho vanilla 50 + prêmio consórcio/solo
    Texture = "Mods/LivingRoots/SistersProduce", SpriteIndex = 0,
    ContextTags = new[] { "sister_produce", "compostable_item" }, // colheita compostável (regra OrganicWasteValidator)
};
// Data/Crops — chave = ID da SEMENTE (wiki Modding:Crop_data, oldid 186411)
data["LivingRoots_SisterCornSeeds"] = new CropData
{
    Seasons = new[] { "summer", "fall" }, // unificadas — resolve §1
    DaysInPhase = new[] { 2, 3, 3, 3, 3 }, // 14d espelhando o milho vanilla (Corn wiki: 2+3+3+3+3)
    RegrowDays = 4, // milho vanilla regrow 4d (Corn wiki) — feijão-irmã 3d, abóbora-irmã -1
    IsRaised = false, // NUNCA trellis nas irmãs (cf. §1.2)
    HarvestItemId = "LivingRoots_SisterCorn",
    HarvestMethod = "Grab",
    ExtraHarvestChance = 0.0, // bônus dinâmico via C# (não data) — US-03-02
    Texture = "Mods/LivingRoots/SistersCrops", SpriteIndex = 0,
    CountForMonoculture = true, CountForPolyculture = true, // policultura conta p/ conquistas vanilla!
    CustomFields = new Dictionary<string, string>
    {
        ["LivingRoots.Companion"] = "SisterCorn", // SisterBean | SisterPumpkin nas demais
        ["LivingRoots.Family"] = "Grain", // Grain | Legume | Cucurbit — alimenta rotation-journal.md Epic 8!
    },
};
```

Tabela de equivalência vanilla (lookup C#, **sem** `EditData` vanilla — padrão `rotation-journal.md:35`):

| Espécie | Semente vanilla (chave `Data/Crops`) | Colheita | Seasons vanilla | Papel na detecção |
|---|---|---|---|---|
| Corn | `487` (Corn Seeds) | `270` | summer, fall | conta p/ dupla/parcial |
| Snap Beans | `473` (Bean Starter) | `188` | spring | conta p/ dupla/parcial |
| Pumpkin | `490` (Pumpkin Seeds) | `276` | fall | conta p/ dupla/parcial (giant quebra o trio — ver §9.4) |

IDs de semente/colheita vanilla via stardewvalleyids + wiki (growth times confirmados nas páginas `Corn`/`Green_Bean`/`Pumpkin`); **a spec reconfirma cada ID contra `Data/Crops` 1.6.x + `oldid` antes de travar** (tabela de IDs é o artefato mais barato e o erro mais caro — lição `mid-game-year2-3.md:469`).

### Detecção (US-03-01) — `ICompanionPlantingService`

Sem API vanilla pronta (não existe "aura de consórcio" — Bee House chega perto p/ mel, mas é lógica de máquina, não de cultivo). Serviço novo:

- **Geometria: adjacência 8-way** (anel de Chebyshev 1 = 8 vizinhos), não raio. Justificativa: (a) corresponde ao monte histórico (milho+feijão no mesmo monte, abóbora entre montes — escala de tiles adjacentes); (b) O(8) por tile vs. O(24) de raio 2 ou O(60) do diamante-5 da colmeia — trivial no `DayStarted`, nunca por frame; (c) legível p/ o jogador ("plante grudado").
- **Vereditos por tile com cultura-irmã** (espécie resolvida via `CustomFields` mod + tabela vanilla): `Complete` (≥1 vizinho vivo de **cada** uma das outras duas espécies) / `Partial` (vizinho de 1 espécie) / `Alone` (nenhum). Somente culturas vivas contam (morta/colhida = ausente); erva daninha não conta.
- **Quando computar**: `DayStarted` (fora do frame, como `IGrazingService`/`IBeneficialInsectService` do Mid Game) + re-checagem no plantio/colheita do tile (`ModController` mesmo ponto de `TryApplyCompost`, `ModController.cs:845`, com `ExecuteWithConcurrencyGuard`). Cache por location em memória; **nada persistido** (configuração é derivada do estado atual dos cultivos — YAGNI: sem `ModData` novo; solo continua no `soil_health_data_<saveId>` esparso existente).

### Bônus (US-03-02)

| Bônus | Complete (trio) | Partial (dupla) | Implementação |
|---|---|---|---|
| Solo | +pequeno/dia em cada tile do trio (TBD-balance, ordem de `HeirloomHarvestBonus +1`) + manutenção: decay reduzido no micro-trio (abóbora = living mulch) | metade do Complete | `ISoilHealthService` existente; constante `SisterSoilPerDay` / `SisterMulchDecayMultiplier` |
| Crescimento | roll diário de fase extra (~equivalente Speed-Gro 10–25%; spec fixa ou `TBD-balance`) | sem bônus crescimento (só solo) | C# no `DayStarted` avança `dayOfCurrentPhase` — **spec faz spike sem-Harmony** (mesmo risco documentado em `mid-game-year2-3.md:467` p/ Adobe Shed); fallback honesto: `DaysInPhase` das irmãs já 1d mais curtas que vanilla (bônus estático, sem runtime) |
| Qualidade/extra | +chance qualidade e/ou `ExtraHarvestChance` efetivo na colheita dos 3 tiles | sem bônus | flag `modData["LivingRoots.SisterBoost"]="complete"` lida na colheita; nunca `HarvestMinQuality` em data (cálculo vanilla de qualidade usa fertilizante+nível — o mod soma por fora; spec prova o hook) |

Anti-farm: bônus exige 3 **espécies distintas vivas** (3 milhos juntos = nada); `Alone` = neutra (sem penalidade).

### Loja / quest (Elena)

- Elena shop (`LivingRoots_ElenaShop`, cf. `early-game-year1.md` Cap. 1/4): vende o **kit Three Sisters** (1 semente de cada) + sementes avulsas; `AvailableStockLimit: Player`; preço ~Pierre (acessível Y1 — é tutorial, não luxo).
- Quest-gating (já previsto em `early-game-year1.md:87` e `elena-npc.md:14`): **"plantar Three Sisters" → desconto heirloom** (`PriceModifiers` + `Condition: "PLAYER_HEARTS LivingRoots_Elena 4"`-style). A detecção US-03-01 é o próprio verificador da quest (sinergia: Epic 3 implementa o sensor que o Epic 5 consome).
- Pierre continua vendendo milho/feijão/abóbora vanilla (contraste, não substituição — como as híbridas em `heirloom-seeds.md:63`).

## 4. Sons

Só cues vanilla Y1 (precedente compost/heirloom): plantio `seeds`/`sandyStep`, colheita `harvest`, quest completa `questStarted`, compra `purchaseClick`/`dwop`. Sem `.ogg` custom (precedente: customs só p/ bin/cisterna).

## 5. i18n (adicionar a `en.json` + `default.json`; `snake.case` com prefixo, padrão `early-game-year1.md:39`)

```json
{ "sisters.corn.seeds.name": "Sister Corn Seeds",
  "sisters.corn.seeds.description": "Heirloom corn,holy companion of beans and squash. Plant close together.",
  "sisters.corn.name": "Sister Corn",
  "sisters.bean.seeds.name": "Sister Bean Seeds",
  "sisters.bean.seeds.description": "Climbing heirloom bean. Fixes nitrogen for her sisters. Needs corn nearby, not a trellis.",
  "sisters.bean.name": "Sister Bean",
  "sisters.pumpkin.seeds.name": "Sister Pumpkin Seeds",
  "sisters.pumpkin.seeds.description": "Heirloom squash. Broad leaves shade the soil like living mulch.",
  "sisters.pumpkin.name": "Sister Pumpkin",
  "sisters.kit.name": "Three Sisters Kit",
  "sisters.kit.description": "Corn, beans, squash — plant them side by side.",
  "message.sisters.complete": "The Three Sisters thrive together!",
  "message.sisters.partial": "Two sisters met — one is missing.",
  "message.sisters.quest-done": "Elena smiles: companionship feeds the soil. Heirloom discount unlocked!" }
```

`DisplayName`/`Description` tokenizáveis desde o dia 1 (`{{i18n:…}}`, padrão Dobson — lição do compost v0 que hardcodou literais em `OnAssetRequested`, cf. `mid-game-year2-3.md:34`).

## 6. Arquitetura / multiplayer (Principle VI)

- DDD: `ICompanionPlantingService` (+ `ISisterBonusService` se a spec preferir separar sensor/efeito) em `Domain/Interfaces/`, impl em `Services/`, eventos em `Controllers/` (`ModController`, `ExecuteWithConcurrencyGuard`), `new` só em `ModEntry.cs` (cf. `LivingRoots/AGENTS.md`). Lógica de veredito pura em `Domain/Services/` (testável sem jogo, como `CropFamilyRegistry` de `rotation-journal.md:35`).
- Farm-keyed + host-autoritativo: scan `DayStarted` roda no host; writes de solo via `ISoilHealthService` (já host-safe); feedback visual via `PerScreen` (nunca estado compartilhado). Nada single-player-only (D1/Principle VI).
- Integrações: escreve família no histórico futuro do Epic 8 (`LivingRoots.Family` já carimbado em `CustomFields` — `rotation-journal.md` consome de graça); bônus de solo respeitam clamp 0–100 (`MinSoilHealth`/`MaxSoilHealth`) e `CheckForOverflowUnderflow=true`; logs sanitizados (`ex.GetType().FullName` + `HResult`, `Trace` apenas); sem `.Result`/`.Wait()`, sem lock através de I/O.
- Overlay: alimentar `IVisualizationService` (borda/selo por veredito), nunca hardcodar em controller.

## 7. Testes / gates

- `dotnet format --verify-no-changes` → build com `GamePath=` → `dotnet test` (baseline 816). `GamePath` obrigatório (`Pathoschild.Stardew.ModBuildConfig` 4.4.0 hard-fail sem ele); `dotnet format` standalone.
- Novos testes: 3 sementes + 3 produtos + 3 `Data/Crops` (chave = ID da semente; `Seasons` contém summer+fall; `IsRaised=false` nas 3; `HarvestItemId` resolve p/ produto existente; `CountForPolyculture=true`; `CustomFields` Companion+Família presentes); vereditos `Complete/Partial/Alone` sobre grade 3×3 (incl. diagonal = vizinha; morta/colhida = ausente; 3 iguais = `Alone`); bônus solo só em Complete/Partial (Alone = zero); flag `SisterBoost` setada/limpa no plantar/colher; loja Elena lista kit + 3 sementes com `AvailableStockLimit: Player`; chaves i18n ×N em ambos JSONs; **regressão vanilla**: nenhuma chave vanilla (`487`/`473`/`490`) editada em `Data/Crops`.
- Critérios de aceite: plantar trio adjacente → toast `message.sisters.complete` + borda verde; dupla → parcial; solteira → nada; quest Elena completa ao formar trio e libera desconto; `dotnet format` limpo; trailing `\n` final (ArchitecturalPlanTests).

## 8. Constantes novas (`LivingRoots/Constants.cs`, classe `ModConstants` — arquivo chama-se `Constants.cs`)

```csharp
public const string SisterSeedTag = "sister_seed";
public const string SisterProduceTag = "sister_produce";
public const string CompanionFieldKey = "LivingRoots.Companion";
public const string SisterCornId = "LivingRoots.SisterCornSeeds";
public const string SisterBeanId = "LivingRoots.SisterBeanSeeds";
public const string SisterPumpkinId = "LivingRoots.SisterPumpkinSeeds";
// (produtos + qualificados (O)… idem — regra dos dois IDs do AGENTS.md raiz)
public const float SisterSoilPerDay = 1f; // TBD-balance: ordem do HeirloomHarvestBonus
public const float SisterPartialFactor = 0.5f;
public const float SisterMulchDecayMultiplier = 0.5f; // abóbora = living mulch parcial (mulch item = 0f)
public const float SisterGrowthBonusChance = 0.2f; // TBD-balance: roll diário fase extra ≈ Speed-Gro
public const float SisterQualityBonusChance = 0.1f; // TBD-balance, ordem do RotationQualityBonusChance
public const string SisterBoostModDataKey = "LivingRoots.SisterBoost";
public const string SisterKitShopId = "LivingRoots_ElenaShop"; // reutiliza shop da Elena (sem constante nova se já existir)
```

## 9. Dependências / ordem 80/20

Posição: **imediatamente após `heirloom-seeds.md`** (reusa o pipeline semente→crop→produto; este briefing é o primeiro consumidor do template — manter `HeirloomSeedTag` genérico foi decisão para este momento, cf. `heirloom-seeds.md:81`), **antes de `green-manure.md` e `rotation-journal.md`** (as irmãs já carimbam `LivingRoots.Family`, então o Epic 8 nasce alimentado; trevo-legume do green-manure é o "primo" do feijão-irmã). Ordem Y1 atualizada: Mulch → Joja → Heirloom ×3 → **Three Sisters** → Cistern → Welcome Kit → Elena NPC (kit/quest das irmãs entram no Welcome Kit e na loja da Elena quando existirem).

## 10. Riscos

1. **Balanceamento** (maior risco): 3 bônus empilhados (solo+crescimento+qualidade) podem dominar o meta Y1 vs. compost `+15`/heirloom `+2/+1`. Mitigação: constantes `TBD-balance` explícitas + playtest; bônus crescimento só no trio vivo completo (frágil: 1 colheita quebra o trio).
2. **Compatibilidade vanilla — nunca editar `Data/Crops` vanilla** (chaves `487`/`473`/`490`): lookup C# de equivalência, como `rotation-journal.md:35`. Teste de regressão trava (§7).
3. **Hook de crescimento sem Harmony**: avançar fase de `Crop` por fora pode conflitar com `RegrowDays` (milho/feijão rebrota 4d/3d) e fertilizantes Speed-Gro. Spike obrigatório na spec; fallback honesto = bônus estático em `DaysInPhase` (§3).
4. **Giant crop da abóbora** (3×3, vanilla): monte gigante engole vizinhos/quebra adjacência. Spec decide: irmã-abóbora suprime giant (`CustomFields` não tem flag giant — verificar `Modding:Giant_crops`; provável: giant só dispara p/ IDs vanilla listados, então a heirloom já é imune — confirmar) ou giant dissolve o trio com toast.
5. **Hook de qualidade na colheita**: cálculo vanilla (fertilizante + nível Farming) não tem evento "modifique minha estrela" — similar ao risco Adobe (`mid-game-year2-3.md:467`). Spike: pós-colheita upgrade (troca item) vs. `HarvestMinQuality` em data (piso, não bônus). Decidir na spec, não aqui.
6. **Quest sensor como dependência do Epic 5**: se Epic 3 atrasar, a quest Elena "plante as Sisters" fica sem verificador — fallback: quest conta plantio (3 sementes usadas) em vez de configuração detectada. Registrar, não resolver aqui.

## Fontes

- Internas: `ROADMAP.md:38`; `LivingRoots/docs/EPICS_USER_STORIES.md:21-25`; `LivingRoots/docs/AGROECOLOGY_CONCEPTS.md:32-40`; `LivingRoots/Constants.cs:53-74` (`MulchColor`, `DailyDecayRate`, IDs compost); `LivingRoots/ModEntry.cs:114-177` (padrão `OnAssetRequested`); briefings irmãos `early-game-year1.md:10-11,87,187,371`, `heirloom-seeds.md:9,81,103`, `elena-npc.md:14`, `rotation-journal.md:22,35,41`, `mid-game-year2-3.md:45,58,467,469`, `mulch-soil-cover.md`, `green-manure.md:43`.
- Externas (verificadas 2026-10-05): `Modding:Crop_data` (oldid 186411) — chave=semente, `DaysInPhase`, `RegrowDays`, `IsRaised`, `HarvestMethod Grab/Scythe`, `ExtraHarvestChance` geométrica `1/(1-p)−1`, `Texture` default `TileSheets\crops`, `SpriteIndex` 1 linha/cultura, `CountForMonoculture/Polyculture`, `PlantableLocationRules`, `CustomFields`; `Modding:Objects` (oldid 193966) — chave não-qualificada, `Type Seeds/Crafting/Basic`, `Category -74/-75`, sprite **16×16**, `ContextTags`, `DisplayName/Description` tokenizáveis; `Modding:Shops` (oldid 192231) — `SeedShop`/`Joja`, `AvailableStockLimit: Player`, `PriceModifiers`+`Condition`, `TargetField` append; `Corn` (14d 2+3+3+3+3, summer+fall, regrow 4d) / `Green_Bean` (10d spring-only, trellis, regrow 3d) / `Pumpkin` (13d fall-only, giant-capável); Three Sisters: Wikipedia `Three_Sisters_(agriculture)`, USDA-NAL, NPS Tonto, Cornell Garden-Based Learning (montes, 4–5 sementes/monte, feijão 2–3 sem. após milho, N só disponível ano seguinte — tradeoff real que o jogo abstrai em bônus imediato pequeno), Mt. Pleasant / Zhang et al. (partição de nicho radicular), Ruel et al. 2016 (trio rende +energia/proteína que monoculturas).
