# Briefing — Pest System MVP (Pulgões em Solo Pobre)

> **Escopo:** ROADMAP `ROADMAP.md:9` ("Poor Soil (0-30%): Slow growth, high chance of pests, lower quality produce") + o lado "controle" de `ROADMAP.md:49` (auras de inseto como alternativa a pesticidas).
> **Status:** briefing (não implementado). Insumo para futuro `/speckit.specify` — não é especificação.
> **Épico/US:** **NENHUM — gap de governança confirmado.** Verificado 2026-10-05: `EPICS_USER_STORIES.md` tem Epic 1–19 e **nenhum** épico de pragas. As duas únicas menções a "pest" são `EPICS_USER_STORIES.md:73` (descrição do Epic 11, "Using insects for natural **pest control**") e `EPICS_USER_STORIES.md:68` (US-10-01, "Animals Eating **Weeds and Insects**") — ambas descrevem **controle** de praga; nenhuma cria a praga. Ver G-01; proposta de épico em §16.
> **Agrupado de origem:** `specs/future-roadmap/briefings/mid-game-year2-3.md` Cap. 4 (ordem de dependência, passo #1).
> **Fontes primárias (externas, revisadas nesta sessão):** ver §14.
> **Fontes internas:** ver §13. Todas as afirmações sobre o jogo ou sobre o repo citam `file:line` ou URL+oldid.
> **Última verificação:** ver rodapé do arquivo.

---

## 0. TL;DR para quem não vai ler tudo

O ROADMAP promete pragas em solo poor (0–30%) desde o dia 1 do design. Quatro briefings irmãos já **dependem** de um sistema de pragas existir — a aura da colmeia (`insect-hotels-beehive-aura.md:14` diz literalmente *"Não especificar aura sem especificar o que ela suprime"*), a rotação (`rotation-journal.md:86`), os sprays (`living-pharmacy-tonics-sprays.md:93`) e o multiplayer/animais (`mobile-coop.md:12`). Nenhum deles pode ser implementado sem este. **Este é o asset mais bloqueante do roadmap que nunca foi briefado.**

O MVP proposto aqui é deliberadamente mínimo: **uma única praga (pulgão), um único vetor (solo < 30%), um único sinalizador (overlay de tile), uma única mitigação (aura + spray, que já têm dono em outros briefings).** Sem combate direto no MVP — o jogador não "mata" pulgões, ele previne.

---

## 1. Conceito de jogo

- **Pulgão (Aphid)** é a praga do MVP. Única praga, único sprite, única cor de estado. Escolha deliberada: `mid-game-year2-3.md:263,322,494` pede "pest MVP" como passo #1, e um MVP com duas pragas já é duas vezes o artwork, duas vezes o balanceamento e duas vezes o suporte de i18n.
- **Vetor:** solo com saúde **abaixo de 30%** (`ROADMAP.md:9` — "Poor Soil (0-30%)"). Solo saudável **não** gera pulgão. Isso amarra o sistema ao `SoilHealthService` já existente e faz a promessa do ROADMAP literalmente verdadeira.
- **Sorteio:** **uma vez por dia**, no `DayStarted`, por tile que tenha crop. Não há propagação de tile para tile no MVP (ver §12, fora de escopo).
- **Efeito no jogador:** o tile infestado **não morre** (o ROADMAP não promete morte) — ele rende:
  - **-1 na qualidade efetiva** via clamping de `Quality` no `harvest` (o pulgão é "pior que adubo", então a perda é de qualidade, não de produção);
  - **não reduz a saúde do solo** — a praga é estado de **crop**, não de solo; solo ruim é o *gatilho*, não a consequência. Spray e aura mexem em saúde do solo de outra forma (`living-pharmacy-tonics-sprays.md:93` — "sprays suprimem sem tocar a saúde do solo").
  - **Recuperabilidade:** o tile infestado pode continuar sendo tratado com compost normalmente (o jogo precisa ser recuperável).
- **Mitigação (implementada por briefings irmãos; aqui só a regra e a seam):**
  - **Aura de insetos benéficos** (`insect-hotels-beehive-aura.md:50` — `AuraPestSuppression = 0.75f`, colmeia r5 / hotel r3, max-não-stack) **suprime o sorteio** no tile coberto. Este briefing **não** implementa a aura; apenas **publica a seam** que a aura consome.
  - **Spray/pesticida de farmácia viva** (`living-pharmacy-tonics-sprays.md`) **elimina** pulgões de tiles já infestados.
  - **Animais** num raio (`mobile-coop.md:12`, US-10-01) suprimem pragas — mesma seam, terceiro consumidor.
- **Sazonalidade:** pulgão aparece **primavera/verão/outono**, nunca inverno. Consistente com "inverno/indoor = sem atividade" da colmeia (`insect-hotels-beehive-aura.md:16`) e com a regra de que a saúde do solo também decai por estação (`SeasonalDecayMultiplier.cs`).

---

## 2. Onde a praga NÃO pode ser (fronteiras explícitas)

| Fronteira | Regra |
|---|---|
| Solo saudável | Solo ≥30 → chance baixa; solo 0–30 → chance alta. **TBD p/ spec:** a rampa exata entre 30 e 100. Note que o solo vai de `MinSoilHealth` a `MaxSoilHealth` = **0 a 100** (`AGENTS.md`), então "0-30%" de `ROADMAP.md:9` mapeia direto para os valores da constante. |
| Lavoura não-vulnerável | semente sem `CustomFields["LivingRoots/PestVulnerable"] = "1"` → **imune** (T-14). Default imune, não vulnerável: um crop de terceiro não deve pegar praga sem opt-in. |
| Fora da fazenda / locais não-fazenda | `location.IsOutdoors && location.IsFarm`-style guard, mesma regra usada pelo resto do mod. **TBD:** nome exato do helper de localização já existente. |
| Tiles sem crop | pulgão precisa de um **crop** (`crop.harvestRow`/soil tile com `crop != null`). Grama/jardim sem cultura não infesta. |
| Tiles já infestados | não re-sorteia (idempotência: um tile infestado não pode virar 2 pulgões). |
| Safra colhida no mesmo dia | o sorteio é pós-colheita ou pré-colheita? **TBD p/ spec** — afeta colhimento e testes de determinismo. |
| Multiplayer | authority do host; clientes recebem estado por sync (seam multiplaayer ver `multiplayer-sync-assets.md`). |

---

## 3. Gap table (severidade → consequência em jogo)

| # | Gap | Sev | Consequência se não fechado |
|---|---|---|---|
| **G-01** | **Não existe Epic/US** para o sistema de pragas em `EPICS_USER_STORIES.md` | **Crítico** | Trabalho de gameplay aparece sem épico, sem critério de pronto, fora da governança do repo. **Verificado 2026-10-05:** o arquivo tem 19 épicos (Epic 1 a Epic 19) e a única menção a "pest" é *descritiva* do controle natural — `EPICS_USER_STORIES.md:73` ("Using insects for natural pest control") e `EPICS_USER_STORIES.md:68` (US-10-01 "Animals Eating **Weeds and Insects**"). Nenhum épico **cria** a praga. `mobile-coop.md:12` cita US-10-01 como se a praga existisse — ela não existe ainda. |
| **G-02** | Nenhum teste de governança: nenhum `specs/00N-pest*/` e nenhum `LivingRoots.Tests/Pest*Tests.cs` | Alto | Bug de supressão de aura entra em produção sem rede: aura que não suprime, ou pulgão que não some. |
| **G-03** | Módulo inexistente: não há `IPestService`, nem pasta `Domain/Pests`, nem constante de praga | Alto | Auras/sprays/animais vão cada um inventar sua própria flag de "praga suprimida" → 3 fontes de verdade. |
| **G-04** | Sem sprite de inseto. Nenhum `Assets/pests/*` existe | Alto | Nenhum feedback visual → o jogador não sabe que tem praga. Sistema invisível = bug reports. |
| **G-05** | Sem SFX de infestação/eliminação | Médio | Falta o "elo sensorial" que torna infestação sentida como evento. |
| **G-06** | Sem entrada de i18n para o nome/descrição do pulgão e o aviso de infestado | Médio | Texto hardcoded quebra localização. |
| **G-07** | Overlay de praga **não** foi comparado com o overlay de tint da spec 001 | Alto | Dois overlays no mesmo tile → z-fighting / custo de frame dobrado / double-darkening. |
| **G-08** | Nenhuma lavoura marcada como vulnerável — o `HashSet` de sementes lido de `CustomFields` não existe | Médio | Ou tudo infesta (só solo poor, sem controle fino), ou nada infesta e o sistema é invisível. |
| **G-09** | Ordem de dependência com rotação/green-manure (`rotation-journal.md:86`) não foi transformada em gate | Médio | Três briefings puxam o mesmo serviço e cada um inventa a interface. |
| **G-10** | Sem verificação in-game documentada (console commands) | Médio | "Funciona no teste unitário mas não no jogo" — clássico. |
| **G-11** | Balanceamento numérico (chance base, rampa) não decidido | Médio | Ou pestes-REI (jogador nunca vê) ou pestes-inferno (jogador desiste). |
| **G-12** | Sem regra de "pest pressure" global (dias ruins) | Baixo (MVP) | Poderia ser adicionado depois; registrar como dívida consciente, não como bug. |

---

## 4. Decisões de design travadas

> "Travada" = já decidida neste briefing; a spec **herda** em vez de re-litigar.

| # | Decisão | Justificativa / citação |
|---|---|---|
| D-01 | **Uma única praga no MVP: pulgão.** | `mid-game-year2-3.md:263,322,494` — "pest MVP" passo #1 da ordem de dependência. Escopo mínimo. |
| D-02 | **Vetor único: solo com saúde < 30%.** | `ROADMAP.md:9` — "Poor Soil (0-30%): … high chance of pests". |
| D-03 | **Sorteio diário único, por tile com crop, no `DayStarted`.** | Consistente com `insect-hotels-beehive-aura.md:48` (`DayStarted` computa aura) e com `SoilDecayService`/`SeasonalDecayMultiplier` que já são diários. |
| D-04 | **Pulgão reduz qualidade da colheita, não mata o crop.** | ROADMAP diz "lower quality produce" (`ROADMAP.md:9`), não "withering". |
| D-05 | **Pulgão não altera a saúde do solo.** | `living-pharmacy-tonics-sprays.md:93` distingue "sprays suprimem sem tocar a saúde do solo". Praga é estado de **crop**, não de solo. |
| D-06 | **Aura suprime o sorteio (preventivo); spray elimina infestado (curativo).** | `insect-hotels-beehive-aura.md:14` — "Não especificar aura sem especificar o que ela suprime". |
| D-07 | **O tint de solo e o marcador de praga são mutuamente exclusivos em intensidade**: o tile com praga desenha o tint a `×PestTintIntensityFactor` (não a 100%) e o marcador por cima com alpha baixo. O corpo do pulgão é a única camada que pode ser descartada por degradação. | Ver §7 e `specs/001-soil-health-visualization/briefings/overlay-procedural.md:4,54,99` (procedural-only + culling/cache + degradação em 1000 tiles). |
| D-08 | **Persistência: o tile infestado é persistido no save** (sparse, como o resto do mod). | Sem isso, save/load limpa as pragas e o sistema vira bug de "praga some ao dormir". |
| D-09 | **Uma fonte de verdade:** o estado de praga é lido/escrito **só** por `IPestService`. `IPestService` **não** conhece `IBeneficialInsectService` — o `ModController` monta o mapa de supressão e o passa como **parâmetro** (§8.2 opção A). | Evita ciclo de DI. `LivingRoots/AGENTS.md` — `ModEntry.cs` é o **único** composition root; `LivingRoots/Domain/AGENTS.md` — contratos em `Domain/`. |
| D-10 | **Multiplayer: host autoritativo.** | `insect-hotels-beehive-aura.md:83` — "Multiplayer host-autoritativo; overlay `PerScreen`". |
| D-11 | **Sem combate direto no MVP.** O jogador não "mata" pulgão com ferramenta. | YAGNI (`.specify/memory/constitution.md`). Mitigação pertence a outros épicos. |
| D-12 | **Nenhuma propagação tile-a-tile no MVP.** | Escopo. Registrado em §12 como dívida consciente. |

---

## 5. Inventário de assets

| Asset | Tipo | Dimensões / formato | Caminho no mod | Nomenclatura de ID | Nota |
|---|---|---|---|---|---|
| Pulgão (corpo do inseto) | frames de `TemporaryAnimatedSprite` | 16×16 por frame, alinhados horizontalmente numa faixa | `LivingRoots/Assets/pests/aphid.png` | **Sem ID de data asset** — `TemporaryAnimatedSprite` recebe `textureName` + `sourceRect`, não uma chave `Data/…` | Anim de **4 frames** (tremer/caminhar). `interval` = ms por frame, `animationLength` = nº de frames, `numberOfLoops` = `int.MaxValue` enquanto o tile estiver infestado (loop eterno). |
| Pulgão (overlay no tile) | desenhado pelo renderer (blend) | 16×16, 1 frame, com tint | `LivingRoots/Assets/pests/aphid_overlay.png` | Sem ID (é pixel, não data) | Camada de feedback; **ver D-07** — marca, não escurece. |
| Ícone de praga (tooltip/UI) | `Texture2D` | 16×16 | `LivingRoots/Assets/pests/icons/aphid_icon.png` | Sem ID se carregado por arquivo; nome de asset apenas se entrar num `AssetRequested` | Ícone estático. Ícone ≠ só cor (acessibilidade). |
| SFX "infestação apareceu" | som | `.ogg` | `LivingRoots/Assets/pests/sfx/aphid_appear.ogg` | Categoria SMAPI — **TBD p/ spec** | `TemporaryAnimatedSprite` expõe `startSound`/`endSound` (`string`), o que permite amarrar o som **à criação do sprite** sem inventar catálogo. Categoria exata = **verificar no build com `GamePath`**. |
| SFX "infestação eliminada" | som | `.ogg` | `LivingRoots/Assets/pests/sfx/aphid_cleared.ogg` | Categoria **TBD p/ spec** | Disparado por `ClearPest` (spray/animais), não pelo sprite. |
| Entrada de vulnerabilidade de lavoura | `CustomFields` em `Data/Crops` | — | (não é asset de arquivo) | Chave `LivingRoots/PestVulnerable` = `"1"` | **Escolhido** — ver §6. |
| Entrada de i18n | JSON | — | `LivingRoots/i18n/default.json` | `pest.aphid.name`, `pest.aphid.infested`, … | Ver nota i18n abaixo. |

**Nota i18n (armadilha conhecida):** `LivingRoots/i18n/*.json` e `LivingRoots/Assets/` **são copiados para o build output pelo `.csproj`, mas nenhum C# os carrega hoje** (`AGENTS.md` — "Assets and i18n are placeholders, not wired"). Todo asset acima é portanto **placeholder até o pipeline de conteúdo existir**. Não assumir que o arquivo existir significa que está carregado; este briefing **não** implementa o carregamento.

**Nota `FeatureStatusTests`:** se a spec tocar o `README.md` (ex.: marcar o sistema como implementado), o teste lê o README como **embedded resource** e exige três strings literais `"(Planned)"` — com `--no-build` ele lê a cópia *stale*. Reconstruir antes de rodar.

### Proibição de vendorizar pixels vanilla — **crítica**

> **Regra dura deste repo:** `specs/004-playable-compost-loop/briefings/compost-icon.png.md:193-197` — o repo proíbe vendorizar/copiar pixels vanilla do jogo. Todos os pixels de inseto/pulgão aqui são **originais**, desenhados em estilo 16×16 flat com a paleta do mod (`Constants.cs`: `Sun #FACC15`, `Healthy #15803D`). **Não** recortar sprite de abelha/bicho do `TileSheets` vanilla. A wiki serve só como **referência de convenção/estilo**, não de asset.

---

## 6. Schemas de data entry (estilo `AssetRequested` — **não** Content Patcher)

> Este repo **não** usa Content Patcher. Os data entries são feitos em C# via `IModHelper.Content.AssetRequested` / `AssetReady`, seguindo o padrão já existente em `LivingRoots/ModEntry.cs:152-176` (registro de `Data/Machines` com `OutputRules`/`MachineOutputRule`/`MachineItemOutput`) e o exemplo de `data-entries-machines-recipes.md`. Ver `specs/004-playable-compost-loop/briefings/data-entries-machines-recipes.md`.

**Abordagem escolhida:** o estado de praga **não** é um data entry — é estado de **save** (D-08). O único data entry é a **declarativa de vulnerabilidade por lavoura**.

### 6.1 Como a wiki descreve `Data/Crops` (verificado 2026-10-05)

`Data\Crops` é um lookup `string → model`; a **chave é o ID do item-semente não qualificado** (`Modding:Crop data`, oldid **186411**). Campos relevantes, transcritos:

| Campo vanilla | Semântica exata (wiki) | Relevância para a praga |
|---|---|---|
| `CustomFields` | "The custom fields for this entry" | **É o ponto de extensão oficial.** Chave `LivingRoots/PestVulnerable` = `"1"`. |
| `HarvestMinQuality` / `HarvestMaxQuality` | "a constraint that's applied **after** the quality is calculated normally" — default `0` / `null` | **Mecanismo nativo de perda de qualidade.** Ver D-04 / 6.3. |
| `HarvestMethod` | `Grab` (mão) ou `Scythe`, default `Grab` | Afeta **onde** o pulgão é desenhado (a fileira do crop na textura) e como o tile é mirado. |
| `ExtraHarvestChance` | probabilidade 0–0.9, série geométrica | **Não usar** para o pulgão: muda *quantidade*, não *qualidade*. |
| `DestroyOnClear` | **NÃO existe** em `Data/Crops` | Correção: este campo **não** aparece no schema de crops 1.6 documentado. Não basear "pulgão mata o crop" nele. |
| `ProducesDailyChance` | **NÃO existe** em `Data/Crops` | Correção: idem — não existe esse campo em `Data/Crops`. |

> **Duas armadilhas de premissa** que o briefing corrigiu: `DestroyOnClear` e `ProducesDailyChance` **não** são campos de `Data/Crops` na wiki 1.6 (oldid 186411). A busca por "um trigger de praga em `Data/Crops`" não acha o que não existe — a extensão real é `CustomFields`.

### 6.2 Esquema escolhido (C# `AssetRequested`)

| Entrada | Asset | Chave | Modelo C# | Campo | Como lido |
|---|---|---|---|---|---|
| Vulnerabilidade de lavoura | `Data/Crops` | chave de cada crop semente | `CropData` | `CustomFields["LivingRoots/PestVulnerable"] = "1"` | no `AssetReady`, cachear um `HashSet<string>` de sementes vulneráveis |

**Sem context tag para isto.** A wiki de context tags é explícita: tags são rótulos em **itens** (`Data/Objects`, `Data/BigCraftables`) e `ItemContextTagManager` expõe `GetBaseContextTags(id)` para `Data/Objects`/`Data/BigCraftables` — **não** para `Data/Crops`. Tags custom devem ser lowercase, alfanuméricas com `_` (`Modding:Context tags`, oldid **193554**). Logo `LivingRoots.pest_aphid` (com ponto) seria **inválido** como tag; se alguma tag for necessária, o formato seria `living_roots_pest_aphid`. **Decisão: `CustomFields`, não tag** — o TBD-2 está resolvido.

### 6.3 Como a perda de qualidade acontece (D-04, agora ancorado)

A wiki diz que `HarvestMinQuality`/`HarvestMaxQuality` aplicam a restrição **depois** do cálculo normal de qualidade. Isso valida o mecanismo de D-04, mas **não** dá um gancho por-tile: esses campos são por-**espécie**, não por-**tile**. Logo:

- **Não** via data field (todos os crops de uma espécie perderiam qualidade, mesmo em solo bom).
- **Sim** por C# no evento de colheita: `Display.Rendered` não serve; o gancho é o evento SMAPI de **`ItemPickedUp`** / ou um patch no `Farmer.tryToHarvest` via harmony. **TBD p/ spec:** identificar o gancho exato no build com `GamePath` — o briefing **não** inventa assinatura de API.

**Constantes candidatas (em `LivingRoots/Constants.cs` — regra: "All magic numbers go in `Constants.cs`", `AGENTS.md`):**

| Constante | Valor | Nota |
|---|---|---|
| `PestInfestedSoilThreshold` | `30` | boundary 0–30 de `ROADMAP.md:9`. |
| `PestBaseChancePoorSoil` | **TBD** | chance por tile/dia em solo <30. |
| `PestBaseChanceMidSoil` | **TBD** | solo 30–99. |
| `AphidSpriteFrameTime` | **TBD** | ms por frame da animação. |
| `AphidFrameCount` | `4` | frames da faixa `aphid.png`. |
| `PestQualityPenalty` | `1` | níveis de qualidade removidos (D-04). |
| `PestSuppressionFromAura` | `0.75f` | **Já declarado** em `insect-hotels-beehive-aura.md:50`. Este briefing só consome, não re-declara. |
| `PestSeasons` | `{Spring, Summer, Fall}` | Sem inverno. |

---

## 7. Camadas: praga vs. o overlay de tint da spec 001 (ordenação)

A spec 001 (`specs/001-soil-health-visualization/briefings/overlay-procedural.md`) entrega um overlay de solo **procedural-only** com frame budget e degradação em 1000 tiles. Este precisa **compor** com ele, não duplicar.

**Regra de composição (D-07):**

1. O **tint de solo** (spec 001) é desenhado no tile **sempre** quando há solo amostrado — é a saúde do solo.
2. A **cama de praga** desenha **por cima do tint**, em duas sub-camadas:
   - **Marcador** — retângulo abstrato com tint de alerta, desenhado com a mesma textura branca 1×1 em runtime da spec 001 (`BlendState.AlphaBlend` + `Color` de alpha baixo). **Não** multiplica o darkening do tint: o pulgão é *marcado*, não *escurecido*.
   - **Exclusão mútua de intensidade**: se o tile tem praga, a **intensidade** do tint de solo daquele tile cai para `TBD` (sug.: ×0.6) para o marcador não esconder o dado de saúde. O dado continua visível, a praga é legível, e o darkening total é limitado.
3. **Frame budget combinado:** o overlay procedural da spec 001 tem teto (degradação em 1000 tiles). O overlay de praga é **aditivo ao pior caso** — em stress de 1000 tiles, se a prune de 001 já degradou, a camada de praga é a **primeira a ser descartada** (é a menos crítica). **TBD p/ spec:** ordem exata de degradação.
4. **Orçamento:** o briefing irmão impõe "orçamento de render <4 ms mantido" (`insect-hotels-beehive-aura.md:81`), e a spec 001 usa `MaxRenderTimeMs=16.67` com `DegradationTileThreshold=1000` (`overlay-procedural.md:99`, `contracts/IVisualizationService.md` citado em `:111` — SC-002 60fps/1000 tiles). **Conflito de orçamento a resolver na spec:** 4 ms (aura) vs 16.67 ms (spec 001). A camada de praga entra **dentro** do teto mais apertado (4 ms), não num extra. `Services/Visualization/AGENTS.md` é a referência interna de frame budget.

**Consequência de "zero PNG" (spec 001):** `overlay-procedural.md:4` é uma restrição **dura** — *"NENHUM `.png` novo. NENHUMA textura embarcada em `Assets/`"* para o **overlay de solo**. Isso **não** se aplica ao corpo do inseto: um pulgão é conteúdo diegético, não um retângulo abstrato, e nenhum procedural produz "perceber que é um pulgão". A separação final:

| Camada | Textura | Por quê |
|---|---|---|
| Tint de solo (spec 001) | 1×1 branca em runtime, `Color` tint | dado abstrato → procedural (`overlay-procedural.md:4`) |
| Corpo do pulgão (inseto) | PNG 16×16 original | conteúdo diegético reconhecível |
| Marcador de praga (overlay) | reusar a mesma 1×1 branca da spec 001 com tint de alerta | é um retângulo abstrato → **procedural**, sem PNG |

Ou seja: **1 PNG a mais para a praga (o inseto), 0 PNGs para o marcador.** Isso mantém o orçamento de assets e respeita a intenção de `overlay-procedural.md` (que é sobre *overlays*, não sobre sprites de criatura).

**Snippet C# — sketch da ordenação de camadas:**

```csharp
// Pseudocode / esqueleto de composição (camadas no RenderedWorld).
// Referência interna: LivingRoots/Services/Visualization/ (culling + cache existentes).
void DrawTile(Vector2 tileScreen, SoilSample sample, PestState? pest)
{
    // 1) Camada base: tint de solo da spec 001 (procedural-only, já tem culling/cache/frame-budget).
    int tintIntensity = sample.HasCrop ? ModConstants.TintIntensityFor(sample) : 0;
    if (pest.HasValue)
    {
        // D-07: com praga presente, reduz a intensidade do tint para o pulgão não esconder o dado.
        tintIntensity = (int)(tintIntensity * ModConstants.PestTintIntensityFactor); // TBD p/ spec (sug.: 0.6f)
    }
    if (tintIntensity > 0)
        _soilOverlay.Draw(tileScreen, sample, tintIntensity);

    // 2) Camada 2a — MARCADOR de praga: retângulo abstrato, então é PROCEDURAL
    //    (mesma textura 1x1 branca em runtime da spec 001, tint de alerta). Zero PNG.
    if (pest.HasValue)
        _pestOverlay.DrawMarker(tileScreen, tint: ModConstants.PestMarkerColor,
                                alpha: ModConstants.PestMarkerAlpha);

    // 3) Camada 2b — CORPO do pulgão: sprite diegético, 1 draw. Só se não degradado.
    if (pest.HasValue && !IsPestLayerSuppressed(pestsRenderState))
        _pestOverlay.DrawAphidSprite(tileScreen, pest.Value,
            sourceRect: new Rectangle(frameIndex * ModConstants.AphidFrameSize, 0,
                                      ModConstants.AphidFrameSize, ModConstants.AphidFrameSize));
}
```

**Sobre `TemporaryAnimatedSprite` vs. `SpriteBatch.Draw` direto:** `TemporaryAnimatedSprite` é a opção quando o sprite **pertence à location** e precisa do ciclo de vida do jogo (gravidade, colisão, `id` para remoção, `startSound`/`endSound`). Para um pulgão **preso ao tile** e com estado próprio (save/load, supressão, sincronia multiplayer), `SpriteBatch.Draw` no `Display.Rendered` é mais barato e mais fácil de sincronizar (D-10) — o sprite é reconstruído a partir do estado de praga, não gerenciado por `location.temporarySprites`. **Decisão p/ spec:** `SpriteBatch.Draw` direto; `TemporaryAnimatedSprite` fica como fallback se o pulgão precisar de comportamento físico.

> Fontes de forma: `TemporaryAnimatedSprite` expõe `textureName`/`sourceRect`/`interval`/`animationLength`/`totalNumberOfLoops`/`id`/`startSound`/`endSound`/`layerDepth`/`alpha`/`color` (`Modding:Event data`, campo `temporaryAnimatedSprite`, 17 campos obrigatórios: textura, retângulo 1–4, intervalo, frames, loops, tile 8–9, flicker, flip, sort tile Y, alpha fade, scale, scale change, rotation, rotation change). Espelho decompilado disponível em `WeDias/StardewValley` mas na **1.5.6** — **não** usar como authority de 1.6; confirmar no build com `GamePath`.

---

## 8. Snippet C# — registro + seam de serviço

> Estilo: registro via `AssetRequested`/`AssetReady` (não Content Patcher); constantes em `Constants.cs`; DI/wiring no **único** composition root (`ModEntry.cs`); `ModController` para eventos SMAPI (padrão de wiring + `ExecuteWithConcurrencyGuard` em `LivingRoots/Controllers/ModController.cs`).

### 8.1 Registro (ilustração do padrão `AssetRequested`)

```csharp
// Em LivingRoots/ModEntry.cs (composition root) ou no helper de content dedicado,
// seguindo o padrão existente de Data/Machines (ModEntry.cs:152-176).
helper.Content.AssetRequested += OnAssetRequested;
helper.Content.AssetReady += OnAssetReady;

private void OnAssetRequested(string path, AssetRequestedEventArgs args)
{
    if (path == ModConstants.AssetPathCrops /* "Data/Crops" */)
    {
        // Marca lavouras vulneraveis: CustomFields["LivingRoots/PestVulnerable"] = "1".
        // Ver secao 6. Chave de custom field no formato LivingRoots/<Nome> (regra do repo).
        // NAO usar context tag: tags sao de Data/Objects e Data/BigCraftables, nao de Data/Crops.
    }
}

private void OnAssetReady(string path, AssetReadyEventArgs args)
{
    if (path != ModConstants.AssetPathCrops)
        return;

    // Le CustomFields uma vez e cacheia num HashSet<string> de sementes vulneraveis.
    // Nao consultar CustomFields por tile por frame (violaria o orcamento de render).
}
```

**Infra de teste reaproveitável:** `data-entries-machines-recipes.md:317` documenta que já existem `CreateAssetRequestedEventArgs`, `GetEditOperations`, `ApplyEditOperation<TKey,TValue>` (mock de `IAssetData`) em `LivingRoots.Tests`, e `:334` confirma que `InternalsVisibleTo("LivingRoots.Tests")` já expõe `OnAssetRequested` (internal) sem mudança de visibilidade. **Estender esses helpers, não reescrever** — e adicionar o teste que trava o conteúdo da chave `LivingRoots/PestVulnerable`, no mesmo espírito do gate de `data-entries-machines-recipes.md:10`.

### 8.2 A seam — `IPestService`

**Esta é a interface que os briefings irmãos esperam.** Três consumidores já a declaram (ver §13 e a tabela de seams abaixo). O MVP implementa:

```csharp
namespace LivingRoots.Domain.Pests
{
    public interface IPestService
    {
        // --- Estado por tile (leitura; usada por overlay, tooltip, harvest) ---
        bool IsPestPresent(Vector2 tile);
        PestKind? GetPestAt(Vector2 tile);

        // --- Escrita restrita ao proprio servico (sorteio no DayStarted / spray) ---
        void InfestTile(Vector2 tile, PestKind kind);      // chamada interna do sorteio diario
        void ClearPest(Vector2 tile);                      // usada por spray (living-pharmacy) e animais (mobile-coop)

        // --- Prevencao: consultas usadas pelas mitigacoes (aura, spray, animais) ---
        // "Quanto de chance de infestacao este tile tem HOJE, ja considerando mitigacao?"
        float GetInfestationChanceToday(Vector2 tile, float soilHealth);
        // "Este tile esta imune hoje por alguma razao (aura / estacao / etc.)?"
        bool IsPestSuppressedToday(Vector2 tile);

        // --- Inspecao (debug / tooltip) ---
        string DescribeTilePestState(Vector2 tile);
    }
}
```

**Seams por consumidor (o que cada um espera, exatamente):**

| Consumidor (briefing irmão) | O que espera da seam | Citação |
|---|---|---|
| **Aura colmeia/hotel** | Expõe `GetPestSuppression(tile)` (= `AuraPestSuppression = 0.75f`) e `GetQualityBonus(tile)`. O `ModController` **combina a aura no mapa de supressão** e passa para `RollInfestations(...)`. A aura **não** conhece `IPestService` — é ela que é consultada, não o contrário. | `insect-hotels-beehive-aura.md:48,50` |
| **Sprays (farmácia viva)** | Chamar `ClearPest(tile)` (elimina infestado) sem tocar a saúde do solo. | `living-pharmacy-tonics-sprays.md:93` |
| **Animais (multiplayer/coop)** | Suprimem em raio — via `IsPestSuppressedToday` (mesma query) OU o `DayStarted` limpa num raio. Epic 10 US-10-01 é literalmente *"Implement Animals Eating **Weeds and Insects**"* — ou seja, o animal **come** a praga, não só a suprime. | `mobile-coop.md:12` (US-10-01); `EPICS_USER_STORIES.md:68` |
| **Overlay de solo (spec 001)** | Query `IsPestPresent(tile)` para aplicar o fator de intensidade (D-07) ao tint. | `specs/001-soil-health-visualization/briefings/overlay-procedural.md` |
| **Rotação journal / green manure** | Dependem do pest MVP existir como passo #1; consomem indiretamente (lavoura rotacionada tem menos praga). | `rotation-journal.md:86` |

**Anti-ciclo de DI (D-09):** `IPestService` **não** referencia `IBeneficialInsectService` diretamente — isso criaria ciclo de dependência (aura precisa de praga para saber o que suprime; praga precisa de aura para saber se está suprimida). Duas saídas:

| Opção | Forma | Custo |
|---|---|---|
| **A — parâmetro** (recomendada) | `ModController.OnDayStarted` computa as mitigações e passa para um método de sorteio: `RollInfestations(IReadOnlyDictionary<Vector2,float> suppressionByTile)` | `IPestService` não sabe que aura existe. Testável sem SMAPI. |
| **B — injeção** | `IPestService` recebe `IBeneficialInsectService` no construtor | Cria o ciclo acima; **não** recomendado. |

Escolha recomendada: **A**. `IPestService` fica dependente só de `Domain/` (sazonalidade, solo, RNG), o que mantém a regra de layering do `LivingRoots/Domain/AGENTS.md` e torna T-01..T-06 testes puros sem stub de SMAPI.

### 8.3 Onde o sorteio diário mora (candidatos internos)

- `LivingRoots/Controllers/ModController.cs` — o handler `DayStarted`/`OnDayStarted`; padrão de wiring + `ExecuteWithConcurrencyGuard`.
- `LivingRoots/Domain/Services/SeasonalDecayMultiplier.cs` — precedente de multiplicador sazonal em `Domain/`.
- `LivingRoots/Services/SoilDecayService.cs` — precedente de serviço que roda diariamente sobre tiles de solo.
- **Regra:** o sorteio em si é **lógica pura** (em `Domain/Pests/`, testável sem SMAPI); o `ModController` só orquestra e persiste.

---

## 9. Plano de testes (`LivingRoots.Tests/`)

> Convenções: `InternalsVisibleTo("LivingRoots.Tests")`; test doubles em `{Fixtures,Stubs,TestHelpers}/`; nomes descritivos given/when/then (ver `AGENTS.md` e `~/development/AGENTS.md`).

| # | Teste | Tipo | Onde |
|---|---|---|---|
| T-01 | Sorteio em solo <30 com crop gera pulgão conforme a chance (determinístico com RNG seedado) | unit | `PestServiceTests` |
| T-02 | Sorteio em solo ≥30 **não** gera pulgão | unit | `PestServiceTests` |
| T-03 | Solo sem crop nunca infesta | unit | `PestServiceTests` |
| T-04 | Sorteio respeita a supressão: tile com `suppressionByTile` alto não infesta | unit | `PestServiceTests` |
| T-05 | Sazonalidade: inverno nunca sorteia (loop 4 estações) | unit | `PestServiceTests` |
| T-06 | Idempotência: re-sorteio não duplica pulgão no mesmo tile | unit | `PestServiceTests` |
| T-07 | `ClearPest` (spray) limpa e **não** mexe na saúde do solo | unit | `PestServiceTests` |
| T-08 | Persistência: salvar e recarregar preserva o tile infestado (round-trip do save) | integration | `PestPersistenceTests` |
| T-09 | Exclusão mútua de camadas: tile com praga tem `tintIntensity` reduzida pelo fator (D-07) | unit | `PestOverlayCompositionTests` |
| T-10 | Marcador não desenha fora de crop; corpo do pulgão não desenha se a camada foi degradada | unit | `PestOverlayCompositionTests` |
| T-11 | Save/load de `ModController` não limpa pragas via `SaveLoad` re-entry | integration | `PestPersistenceTests` |
| T-12 | `ExecuteWithConcurrencyGuard` impede corrida se `DayStarted` disparar duas vezes | unit | `ModControllerPestGuardTests` |
| T-13 | `OnAssetRequested` enfileira a edição com `CustomFields["LivingRoots/PestVulnerable"] = "1"`; `AssetReady` popula o `HashSet` de sementes vulneráveis | unit | `PestDataEntryTests` (reusar os helpers de `data-entries-machines-recipes.md:317`) |
| T-14 | Lavoura **não** marcada em `CustomFields` não é vulnerável (default = imune) | unit | `PestDataEntryTests` |
| T-15 | Qualidade da colheita: tile infestado perde exatamente `PestQualityPenalty` níveis; solo 100 não perde | unit | `PestHarvestQualityTests` |
| T-16 | Determinismo: mesma seed + mesmo estado → mesmo resultado do sorteio | unit | `PestServiceTests` |
| T-17 | FeatureStatus: as três strings `"(Planned)"` do README continuam presentes | guard | `FeatureStatusTests` (existente) — **lembrete:** o README é **embedded resource**; rodar com rebuild, não `--no-build`, ao mexer no README. |

**Gate institucional que já roda:** `LivingRoots.Tests/ArchitecturalPlanTests.cs` exige que **todo** `.md` sob `LivingRoots/docs/architectural_and_refactor_plans/**` termine com newline final. Este briefing está em `specs/`, fora dessa árvore — mas qualquer doc novo em `LivingRoots/docs/` que o sistema de pragas gerar precisa do `\n` final ou a suíte quebra.

**Comandos de gate (a ordem importa, `AGENTS.md`):**

```bash
GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release
GamePath=$GP dotnet test --no-build -c Release --filter "FullyQualifiedName~Pest"
dotnet format Stardew-LivingRoots.sln --verify-no-changes
```

---

## 10. Verificação em jogo

**Console commands (SMAPI):** **TBD p/ spec.** O padrão correto é registrar comandos via `IModHelper.ConsoleCommands.Add` no `ModEntry`, seguindo o que o mod já fizer. Comandos candidatos que a spec **deve** registrar para tornar a verificação em jogo viável:

| Comando | Efeito | Por que |
|---|---|---|
| `addpest <x> <y> [aphid]` | força infestação num tile | sem ele, o teste manual depende de RNG e leva dias de jogo |
| `clearpest [x y]` | limpa tudo (ou um tile) | isola D-05 (spray não mexe no solo) |
| `pestinfo [x y]` | imprime saúde do solo, chance de infestação de hoje, estado de praga | valida D-02/D-06 sem abrir save |
| `soilhealth <x> <y> <valor>` | força solo poor/saudável | sem isso não dá para testar o vetor 0–30 |

Nenhum desses comandos é inventado aqui como existente — são **requisitos** que a spec deve criar. Confirmar o padrão de registro no build com `GamePath`. Até existirem, a verificação manual abaixo é a fonte de verdade.

**Referência de console da wiki:** a wiki lista `listtags` como comando de debug nativo que lista as tags do item na mão (`Modding:Context tags`, oldid 193554) — precedente de que a wiki considera debug commands parte do escopo normal de mod.

**Passos manuais:**
1. Carregar save, plantar um crop num tile de solo pobre (forçar com GMCM, ou deixar a saúde do solo em 0–30).
2. Dormir → esperar 1 dia → checar: o tile mostra o overlay de pulgão? (sistema visível)
3. Colher: a qualidade está reduzida (D-04)? o crop **não** morreu (D-04)?
4. Colocar uma Bee House a ≤5 tiles (aura r5) com flor madura → repetir → o pulgão **não** aparece (D-06 preventiva).
5. Usar o spray (quando `living-pharmacy` existir) num tile infestado → pulgão some, **saúde do solo inalterada** (D-05).
6. Verificar que o tint de solo (spec 001) **ainda é visível** sob o pulgão (D-07, T-09).
7. Dormir em **inverno** → nenhum pulgão novo (sazonalidade).
8. **Multiplayer:** host e cliente veem o mesmo estado de praga (D-10).

**Stress de frame budget:** infestar intencionalmente 1000 tiles e confirmar que o render fica <4 ms e que a degradação da spec 001 (T-10) protege.

---

## 11. Riscos e armadilhas

1. **Ciclo de DI** entre `IPestService` e `IBeneficialInsectService` (aura ↔ praga). Mitigação: D-09 + §8.2 **opção A** (parâmetro, não injeção). **Risco nº 1.**
2. **Sobreposição com o tint da spec 001** → z-fighting / double-darkening. Mitigação: D-07 (exclusão mútua de intensidade) + T-09. **Complicador novo:** conflito de orçamento 4 ms vs `MaxRenderTimeMs=16.67` (§7.4) — a spec precisa escolher o teto.
3. **Mitigações já briefadas** (spray, animais) podem virar a **mesma** flag de praga duplicada. Mitigação: fonte única (`IPestService`) + seam declarada em §8.2.
4. **Persistência esquecida** → praga some ao save/load. Mitigação: D-08 + T-08/T-11.
5. **Balanceamento** (G-11): chance baixa demais = sistema invisível (reporte "nunca vi praga"); alta demais = punição desproporcional por solo poor. **TBD p/ spec** — precisa de playtest, não de teste unitário.
6. **Pixels** — tentação de recortar inseto vanilla. Proibido: `compost-icon.png.md:193-197` exige artwork original, "do not copy, trace, recolor, or vendor any vanilla pixel". A wiki serve de referência de estilo, não de asset.
7. **Mitigações / rotação** podem re-introduzir a "praga" como ad-hoc (G-09). Mitigação: gate explícito — qualquer consumidor novo passa por `IPestService`.
8. **`ExecuteWithConcurrencyGuard`** — `DayStarted` rodando duas vezes (edge de save/load) duplica o sorteio. Coberto por T-12 + T-06.
9. **Fronteira 0–30% é promessa documentada** (D-02) — implementar 0–40 quebra o ROADMAP, não só o código.
10. **Premissa falsa de schema (armadilha que este briefing já desarmou):** procurar `DestroyOnClear` / `ProducesDailyChance` em `Data/Crops` **não encontra** — não existem no schema 1.6 documentado (§6.1). O ponto de extensão é `CustomFields`. Uma spec que assuma esses campos vai codificar contra o nada.
11. **Conflito com "zero PNG" da spec 001** — a restrição dura de `overlay-procedural.md:4` é sobre **overlays**, não sprites de criatura. Se lida literalmente ("nenhum PNG novo no mod"), o pulgão vira um retângulo abstrato e o sistema fica ilegível. Mitigação: a separação em §7 (marcador procedural, inseto diegético com 1 PNG).
12. **`Assets/` e `i18n/` não estão ligados a nenhum `Content.Load`** (`AGENTS.md`) — todo asset deste briefing é placeholder. Adicionar `Content.Load` sem textura real = crash de runtime.

---

## 12. Fora de escopo (explícito)

- **Combate direto / ferramenta de inseticida** (D-11) — o jogador previne, não mata.
- **Segunda praga** (D-01) — pulgão é a única no MVP.
- **Propagação tile-a-tile / contágio** (D-12) — dívida consciente.
- **Estado global de "pest pressure" / dias ruins** (G-12) — dívida consciente.
- **Aura, sprays e animais em si** — este briefing só publica a **seam**; a implementação é dos briefings irmãos.
- **UI de menu dedicado** (`IClickableMenu`) — não. Tooltip + GMCM bastam (`insect-hotels-beehive-aura.md:58`).
- **Áudio ambiente** — só os dois SFX pontuais de §5.
- **Animação contínua por frame** — o pulgão é 1 draw com índice de frame, não uma `location.temporarySprites` gerenciada por tick.
- **Conteúdo do pipeline** (carregar `Assets/` e `i18n/` de verdade) — placeholder até existir o pipeline (`AGENTS.md`).
- **Praga em crop de outras lavouras** (heirloom, polyculture) — o MVP usa as vulnerabilidades em `CustomFields`; cada briefing irmão marca as suas.
- **Praga como `Object` coletável** — o pulgão não é um item que se pega.

---

## 13. Fontes internas

| Fonte | `file:line` / caminho | O que sustenta |
|---|---|---|
| Promessa de praga em solo poor | `ROADMAP.md:9` | D-02, G-11 |
| Controle natural como alternativa a pesticidas | `ROADMAP.md:49` | §1 (mitigação) |
| Dependência dura: aura sem praga | `specs/future-roadmap/briefings/insect-hotels-beehive-aura.md:14` | D-06, ordem |
| Ordem de dependência (pest MVP é passo #1) | `specs/future-roadmap/briefings/mid-game-year2-3.md:263,322,494` | D-01, G-09 |
| Animais suprimem pragas (US-10-01) | `specs/future-roadmap/briefings/mobile-coop.md:12` + `LivingRoots/docs/EPICS_USER_STORIES.md:68` | §8.2 (3º consumidor) |
| Sprays suprimem sem tocar saúde do solo | `specs/future-roadmap/briefings/living-pharmacy-tonics-sprays.md:93` | D-05, T-07 |
| Ordem pest→rotation→green manure | `specs/future-roadmap/briefings/rotation-journal.md:86` | G-09 |
| Supressão de aura (constante) + multiplicidade | `insect-hotels-beehive-aura.md:50` | §6 (constante consumida) |
| Padrão de registro `AssetRequested` (não Content Patcher) | `LivingRoots/ModEntry.cs:152-176` + `data-entries-machines-recipes.md:3,106,264` | §6, §8.1 |
| Helpers de teste de data entry já prontos | `data-entries-machines-recipes.md:317` (`CreateAssetRequestedEventArgs`, `GetEditOperations`, `ApplyEditOperation<TKey,TValue>`) | §8.1, T-13 |
| `InternalsVisibleTo` já expõe `OnAssetRequested` | `data-entries-machines-recipes.md:334` | T-13 |
| Composition root único / DI | `LivingRoots/AGENTS.md` | D-09 |
| Padrão de wiring + `ExecuteWithConcurrencyGuard` | `LivingRoots/Controllers/ModController.cs` | §8.3, T-12 |
| Onde um sorteio diário moraria | `LivingRoots/Domain/Services/SeasonalDecayMultiplier.cs`, `LivingRoots/Services/SoilDecayService.cs` | §8.3 |
| Regra dura "NENHUM `.png` novo" (overlays) | `specs/001-soil-health-visualization/briefings/overlay-procedural.md:4` | §7 (conflito de assets) |
| Frame budget / degradação: `DegradationTileThreshold=1000`, `MaxRenderTimeMs=16.67` | `overlay-procedural.md:99`; SC-002 em `:111` | §7.4 (conflito 4 ms vs 16.67 ms) |
| Evento de render e snapshot por referência | `overlay-procedural.md:54` (`Display.Rendered`, early-return se pausado, snapshot sob `_dataLock`) | §7 (padrão a seguir) |
| Proibição de vendorizar pixels vanilla (artwork original obrigatório) | `specs/004-playable-compost-loop/briefings/compost-icon.png.md:193-197` | §5 (regra dura), risco 6 |
| Constantes em `Constants.cs`; paleta `Sun #FACC15`, `Healthy #15803D` | `LivingRoots/Constants.cs`, `AGENTS.md` | §5, §6 |
| `Assets/` e `i18n/` **não carregados** por nenhum C# | `AGENTS.md` (raiz), seção "Assets and i18n are placeholders, not wired" | §5 (nota), risco 12 |
| `FeatureStatusTests` lê README embedded; `ArchitecturalPlanTests` exige `\n` final em docs | `LivingRoots.Tests/FeatureStatusTests.cs`, `LivingRoots.Tests/ArchitecturalPlanTests.cs` | §9 |
| **Gap de governança: sem Epic/US de pragas** | `LivingRoots/docs/EPICS_USER_STORIES.md` — Epic 1–19, nenhuma entrada de praga; única menção descritiva em `:73` | G-01, §16 |

---

## 14. Fontes primárias externas (com data de revisão)

> Revisadas nesta sessão, 2026-10-05. Todas as afirmações sobre o jogo (membros de API, campos de data entry) que **não** apareçam aqui estão marcadas como **TBD** / "verificar no build com `GamePath`".

| Fonte | URL | oldid | Revisado | Usado para |
|---|---|---|---|---|
| `Modding:Crop data` (`Data/Crops`) | `https://stardewvalleywiki.com/Modding:Crop_data` | **186411** | 2026-10-05 | Schema de `Data/Crops`: chave = ID do item-semente **não qualificado**; campos `CustomFields`, `HarvestMethod`, `HarvestMinQuality`/`HarvestMaxQuality`, `ExtraHarvestChance`. **Confirmou que `DestroyOnClear` e `ProducesDailyChance` NÃO existem** (§6.1). |
| `Modding:Context tags` | `https://stardewvalleywiki.com/Modding:Context_tags` | **193554** | 2026-10-05 | Tags são de **itens** (`Data/Objects`, `Data/BigCraftables`); `ItemContextTagManager.GetBaseContextTags(id)` só cobre esses dois. Tags custom devem ser lowercase alfanuméricas com `_`. **Desarmou a ideia de tag em `Data/Crops`.** Também: `listtags` é debug command nativo. |
| `Modding:Event data` (`temporaryAnimatedSprite`) | `https://stardewvalleywiki.com/Modding:Event_data` | (não anotado) | 2026-10-05 | Os 17 campos obrigatórios de `temporaryAnimatedSprite`: textura, rect 1–4, `interval`, `frames`, `loops`, tile 8–9, `flicker`, `flip`, `sort tile Y`, `alpha fade`, `scale`, `scale change`, `rotation`, `rotation change`; flags `color`, `ping_pong`, `hold_last_frame`, `alpha`, `light_id`, `x_stop_coordinate`, `shkake_intensity`, `start_sound`, `end_sound`, `text`, `parent_sprite`, `removeSpriteById` via `id`. |
| `Modding:Modder Guide/APIs/Events` | `https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events` | (não anotado) | 2026-10-05 | `Display.Rendered` = "raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened… **the game will draw over it**" → **é o gancho errado** para um overlay que precisa ficar *acima* do crop. O gancho de overlay é o usado pela spec 001 (`Display.Rendered` **só** para texto/efeitos; sobreposições de tile exigem o gancho de render que a spec 001 já resolve — reaproveitar, não redescobrir). |
| `Modding:Objects` | `https://stardewvalleywiki.com/Modding:Objects` | **não lida** | — | Seria para avaliar o pulgão como `Object`; **descartado** (§12 — o pulgão não é item coletável). Não foi necessário abrir. |
| Convenções de sprite 16×16 | — | — | 2026-10-05 | Só convenção de estilo/escala (`compost-icon.png.md:191` — 16×16 no mundo, 64×64 no inventário, luz no topo/esquerda). **Pixels vanilla proibidos.** |

> **Duas fontes que 404aram** e portanto **não** são authority: `Modding:Temporary_sprites` e `Modding:Temporary_Sprites` retornaram **HTTP 404** em 2026-10-05 — esse título de página **não existe** na wiki. A forma do `TemporaryAnimatedSprite` foi obtida de `Modding:Event data` (o comando que o cria) e de um espelho decompilado **`WeDias/StardewValley`, que é 1.5.6** — portanto **não** usável como authority de 1.6. Marcado "verificar no build com `GamePath`".

> **Nota de disciplina:** a wiki serve de referência factual. Nenhum membro de API foi inventado; onde a certeza depende do build (assinatura exata de `TemporaryAnimatedSprite` em 1.6, categoria de SFX, gancho de colheita), o briefing marca **TBD / "verificar no build com `GamePath`"**.

---

## 15. Lacunas TBD (para a spec fechar)

**Fechadas nesta revisão (eram TBD, agora decididas):**

- ~~TBD-2~~ → **`CustomFields["LivingRoots/PestVulnerable"] = "1"` em `Data/Crops`**. Context tags **não** servem: são de itens, não de crops (`Modding:Context tags`, oldid 193554). §6.2.
- ~~TBD-4~~ → **Opção A (parâmetro)**: o `ModController` passa as mitigações para `RollInfestations(...)`; `IPestService` não conhece a aura. §8.2.
- ~~TBD-6~~ (parcial) → oldids de `Crop_data` (186411) e `Context_tags` (193554) confirmados; `Modding:Temporary_sprites` **não existe** (404).

**Abertas:**

- **TBD-1:** balanceamento — `PestBaseChancePoorSoil`, `PestBaseChanceMidSoil`, `PestTintIntensityFactor`, `PestMarkerAlpha`, `AphidSpriteFrameTime`, `PestQualityPenalty`. **Precisa de playtest, não de teste unitário** (risco 5). (§6)
- **TBD-3:** o sorteio roda antes ou depois da colheita do dia? Afeta determinismo e se colher no dia da infestação salva a colheita. (§2)
- **TBD-5:** ordem exata de degradação quando o frame budget estourar, e **resolução do conflito 4 ms (aura) vs 16.67 ms (spec 001)**. (§7.4)
- **TBD-7:** console commands de debug (`addpest`/`clearpest`/`pestinfo`/`soilhealth`) — precisam ser **criados**; padrão de registro a confirmar no build com `GamePath`. (§10)
- **TBD-8:** Epic/US formal para o sistema de pragas (G-01) — decisão de governança, ver §16.
- **TBD-9:** gancho exato de colheita para aplicar a perda de qualidade por-tile (`HarvestMinQuality`/`HarvestMaxQuality` é por-espécie e não serve). **Verificar no build com `GamePath`** — o briefing não inventa assinatura. (§6.3)
- **TBD-10:** assinatura de `TemporaryAnimatedSprite` em **1.6** e categoria SMAPI dos SFX. **Verificar no build com `GamePath`.** (§5, §14)
- **TBD-11:** `ModEntry.cs:152-176` citado pelo briefing consumidor como registro de `Data/Machines` — **não foi relido nesta sessão**. Confirmar o intervalo exato ao implementar. (§8.1)

---

## 16. Proposta de épico (fecha G-01)

Não é decisão do briefing — é uma sugestão para quem tem o produto na mão. Baseado na forma dos épicos existentes (`EPICS_USER_STORIES.md`, Epic 1–19):

> **Epic N: Pest System (Issue #NN)**
> - **Description:** Crops in poor soil (0–30%) risk aphid infestation, lowering harvest quality until suppressed.
> - US-N-01: Daily infestation roll driven by soil health (Issue #NN)
> - US-N-02: Visual aphid overlay + affected-tile indicator (Issue #NN)
> - US-N-03: `IPestService` seam for suppression sources (aura, spray, animals) (Issue #NN)

**Por que isto importa:** quatro briefings irmãos (`insect-hotels`, `living-pharmacy`, `mobile-coop`, `rotation-journal`) já citam US/Issue de **controle** de praga como se a praga existisse (`EPICS_USER_STORIES.md:68,73`). Sem este épico, o primeiro a ser implementado inventa a interface e os outros três reimplementam. A ordem correta é: **este épico primeiro**, e os outros quatro consomem `IPestService`.

---

**Última verificação:** 2026-10-05 — escrito a partir de `ROADMAP.md:9,49`, 5 briefings irmãos, `EPICS_USER_STORIES.md` (19 épicos, conferido), `AGENTS.md` (raiz + `LivingRoots/`, `Domain/`, `Services/`, `Controllers/`), `overlay-procedural.md`, `compost-icon.png.md:185-197`, `data-entries-machines-recipes.md:3,106,264,317,333-334`, e o consumidor `insect-hotels-beehive-aura.md` (lido integralmente). Pesquisa externa: `Modding:Crop_data` (oldid 186411), `Modding:Context_tags` (oldid 193554), `Modding:Event_data`, `Modding:Modder_Guide/APIs/Events` — todas 2026-10-05. **Nenhum membro de API foi inventado**; `Modding:Temporary_sprites` retornou 404 (página inexistente) e o espelho decompilado disponível é **1.5.6**, então a forma do `TemporaryAnimatedSprite` fica marcada "verificar no build com `GamePath`". Lacunas abertas: TBD-1, 3, 5, 7, 8, 9, 10, 11.
