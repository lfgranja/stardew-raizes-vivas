# Pesquisa externa — viabilidade de implementação e base agronômica do ROADMAP

> **Data:** 2026-10-15 · **Método:** buscas web diretas, fontes primárias (documentação oficial SMAPI/wiki do
> Stardew, código-fonte decompilado de `Crop`, journals peer-reviewed, agências de extensão).
> **Idiomas:** EN, PT (fontes SciELO/EMBRAPA).
> **Escopo:** (A) qual API do SMAPI 1.6 permite ligar saúde do solo a resultado de colheita; (B) baseline
> competitivo de mods de solo; (C) o que a agronomia realmente sustenta de cada mecânica do ROADMAP.
>
> **Nota de integridade:** uma versão anterior deste arquivo foi citada no ROADMAP.md antes de existir.
> Este é o arquivo real; as citações no ROADMAP foram corrigidas para apontar aqui.

---

## A. API do SMAPI / Stardew 1.6 — como ligar solo a colheita

### A.1 Estado da plataforma (verificado)

| Fato | Valor | Fonte |
|---|---|---|
| SMAPI estável atual | **4.5.2**, de 14/03/2026 | https://github.com/Pathoschild/SMAPI/releases/latest |
| Requisito de jogo | Stardew Valley **1.6.14+** | mesma release |
| Target .NET do mod | **net6.0 continua correto** (SMAPI 4.x roda em .NET 6) | https://github.com/Pathoschild/SMAPI/blob/stable/docs/release-notes.md — *"For mod authors: Updated to .NET 6"* |
| ModBuildConfig em uso | `4.4.0` (verificar se é a última) | `LivingRoots/LivingRoots.csproj:14` |

**Conclusão A.1:** nenhuma migração de framework é necessária. O alvo `net6.0` está correto em 2026.

### A.2 O ponto de extensão para velocidade de crescimento — `Crop.newDay`

`StardewValley.Crop.newDay` é **público e virtual**:

```csharp
public virtual void newDay(int state, int fertilizer, int xTile, int yTile, GameLocation environment)
```

Fonte: código decompilado (espelho público `WeDias/StardewValley`), corroborado pelo uso em mods reais —
o mod *LuckSkill* (spacechase0) chama `dirt.crop.newDay(HoeDirt.watered, dirt.fertilizer.Value, x, y, loc)`
diretamente para "avançar as colheitas" dos junimos: https://github.com/spacechase0/LuckSkill/blob/master/Mod.cs

O corpo do método mostra que o crescimento é **determinístico e baseado em campos públicos**:

```csharp
if (!this.fullyGrown)
    this.dayOfCurrentPhase.Value = Math.Min(this.dayOfCurrentPhase + 1,
        this.phaseDays.Count > 0 ? this.phaseDays[Math.Min(this.phaseDays.Count - 1, this.currentPhase)] : 0);
else
    --this.dayOfCurrentPhase.Value;
if (this.dayOfCurrentPhase >= phaseDays[currentPhase] && this.currentPhase < phaseDays.Count - 1) { ++currentPhase; dayOfCurrentPhase = 0; }
```

**Duas rotas de implementação, ambas de baixo risco:**

| Rota | Mecanismo | Risco |
|---|---|---|
| **A — sem Harmony** | Própria passagem em `DayStarted`/`DayEnding` iterando `location.terrainFeatures` → `HoeDirt.crop` e ajustando `dayOfCurrentPhase`/`phaseDays` | Ordenação vs. crescimento vanilla precisa de verificação em jogo. Mas é padrão comprovado (LuckSkill) e usa só campos públicos |
| **B — com Harmony** | Postfix em `Crop.newDay` (assinatura estável, método virtual) ou em `HoeDirt.GetFertilizerSpeedBoost()` | Precisão por-tile e ordenação garantidas. Exige adicionar `Lib.Harmony` (hoje **zero** Harmony no repo) |

**B é semanticamente superior para o nosso caso:** o método 1.6 `HoeDirt.GetFertilizerSpeedBoost()` existe
justamente porque a ConcernApe *"centralizou quase toda a lógica de fertilizante em um novo conjunto de
métodos patchable de `HoeDirt`"* — https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6.
Compor nossa multiplicação de saúde do solo com o Speed-Gro vanilla dentro desse método evita
double-dipping, que é o risco #4 do briefing `soil-tier-outcomes-harvest-feedback.md`.

### A.3 Qualidade de colheita — `Crop.harvest` é virtual e patchável

```csharp
public virtual bool harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null)
```

O roll de qualidade acontece **dentro** deste método e é determinístico por tile+dia:

```csharp
Random random = new Random(xTile * 7 + yTile * 11 + (int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame);
switch ((int)soil.fertilizer) { case 368: num2 = 1; break; case 369: num2 = 2; break; case 919: num2 = 3; break; }
double num3 = 0.2 * (Game1.player.FarmingLevel / 10.0) + 0.2 * num2 * ((Game1.player.FarmingLevel + 2.0) / 12.0) + 0.01;
double num4 = Math.Min(0.75, num3 * 2.0);
if (num2 >= 3 && random.NextDouble() < num3 / 2.0) quality = 4;      // iridium
else if (random.NextDouble() < num3) quality = 2;                    // gold
else if (random.NextDouble() < num4 || num2 >= 3) quality = 1;      // silver
```

Por receber `xTile`, `yTile` e `soil`, um **postfix/transpiler em `Crop.harvest` tem tudo que precisa**
para consultar `ISoilHealthService.GetSoilHealth(location, tile)` e ajustar `Object.Quality`.
Precedente de mod real: *Crop Quality* (aedenthorn) e *Crop Variation* — este último injuta qualidade via
**transpiler** localizando exatamente a instrução que grava a qualidade
(https://deepwiki.com/aedenthorn/StardewValleyMods/7.2-crop-variation).

**Importante:** `Data/Crops` tem `HarvestMinQuality`/`HarvestMaxQuality`, mas a wiki é explícita:
*"These fields set a constraint that's applied after the quality is calculated normally, they don't
affect the initial quality logic"* — são **clamps globais por cultura**, não por tile. Logo, "sem
estrelas em solo pobre" **não** pode sair de data; é runtime. Confirma o briefing interno.

### A.4 Um bug latente já conhecido, que a feature nova acionaria

`soil-tier-outcomes-harvest-feedback.md` risco #1 aponta que `CompostingBinService.CollectCompost`
tem `outputCount` hardcoded 1 e **não faz rollback parcial**: se o output aumentar e o 2º item encher o
inventário, o 1º já foi entregue, o bin continua `Ready`, retorno 0 → **duplicação**. Hoje mascarado;
qualquer feature que aumente output o ativa. Corrigir antes, não depois.

---

## B. Baseline competitivo (mods de solo em 2026)

| Mod | Estado | O que faz de fato | Relevância |
|---|---|---|---|
| **ReGen Valley** (Nexus 39740) | Ativo, nov/2025 | **Nosso concorrente direto.** Saúde de solo por tile, cover crops, gado rotacionado, *"At 70+ Soil Health: crops on those tiles effectively grow an extra day each night. At 90+: up to two extra days"*; qualidade e yield farm-wide em média 75+/90+ | **A promessa nuclear do nosso ROADMAP já existe no mercado.** Diferenciação nossa não pode ser novidade |
| **Ultimate Fertilizer** (Nexus 21318) | Muito ativo (jun/2026), 852 endorsements | Multi-fertilizante,Speed-Gro após plantio, valores configuráveis | Prova que hooks em fertilizante são terreiro comum |
| **Watering Path V2** (Nexus 37996) | Ativo, mai/2026 | Sistema de umidade por tile que **altera crescimento e qualidade** | Prova que crescimento por estado de tile é padrão da categoria |
| **Crop Quality** (aedenthorn) | Ativo, jun/2026 | Patch nas chances de qualidade vanilla; GMCM | Referência de hook de qualidade |
| **Tilled Soil Decay Options** / **Simple Lasting Soil** / **Retain Tilled Soil** | Ativos, 2026 | Só decaimento de solo arado | Nosso decaimento já é mais rico que esses |

**Conclusão B:** nossa vantagem competitiva **não é** a mecânica solo→colheita. É a **fundamentação
agroecológica**, o **loop de compostagem acumulativo**, a **correção multiplayer** (Princípio VI) e a
**honestidade documental**. Qualquer spec nova deve competir em coerência, não em originalidade.

---

## C. Base agronômica de cada mecânica do ROADMAP

### C.1 Completamente sustentado — pode ser ensinado como fato

| Mecânica | Evidência | Números |
|---|---|---|
| **Composto / esterco** | Agronomy 2021 (Po Valley, ~50 anos) | Esterco: SOC **+13%**, N extraível **+19%**, P disponível **+47%**, biomassa microbiana **+34%** — https://www.mdpi.com/2073-4395/11/4/636 |
| **Cobertura morta / mulch** | **Meta-análise global** Geoderma 2023 (90 publicações, 512 obs. de perda de solo) | Runoff **−47,4%**, perda de solo **−76,2%**; cobertura ≥60% → ~50%/80%. Palha 0,3–0,4 kg/m² — https://doi.org/10.1016/j.geoderma.2023.116643 |
| **Cobertura morta (PT)** | SciELO / EMBRAPA | Umidade **+2,0%**, temperatura **−3,5 °C**, supressão de invasoras efetiva; produtividade da cenoura 112,6 vs. controle |
| **Rotação de culturas** | Soil & Tillage Res. 2021 (21 e 36 anos) | Indicadores de saúde do solo **+32%/+49%**, produtividade **+16%/+29%**; milho **+25%** com alfafa — https://doi.org/10.1016/j.still.2021.105121 |
| **Rotação (52 anos)** | Agriculture 2024 | Rotação Norfolk: SOC **+46,7%** vs. pousio contínuo; CMI 475,6 vs. 225,3 |
| **Adubo verde** | Agronomy 2019 (34 anos, arroz) | Rotação com *milkvetch*: produtividade **+45%/+46%**, MOS **+40,6%**, N total **+23%** — https://www.mdpi.com/2073-4395/9/12/780 |

**Ressalva honesta do mulch:** a revisão *Water* 2022 registra que palha pode **aumentar a abundância de
insetos** — mulch tem trade-off real de pragas. Ótimo material de design de jogo (decisão, não bônus grátis).

### C.2 Sustentado apenas em parte — narrativa popular exagerada

**Three Sisters (policultura).** A vantagem em *base de terra equivalente* (LER > 1) é real e tem
mecanismo identificado (complementaridade de forrageamento radicular — Zhang et al. 2014, *Annals of
Botany*). **Mas:**

- Mt. Pleasant 2010: rendimento de feijão na policultura foi **~10× menor** que em monocultura; abóbora **~20× menor**; milho pouco afetado.
- Estudo 2024 com a Nação Catawba: rendimento total de milho equivalente; **abóbora rendeu mais em monocultura**; rendimento por hora de trabalho foi maior em monocultura.
- Estudo de Wisconsin 2007: **nenhuma diferença significativa** em rendimento por planta nem em N total do solo.

Ou seja: o consórcio é vantajoso **por área total**, não por planta, e o benefício de solo é fraco/indireto.
Ensinar "as irmãs se beneficiam mutuamente e melhoram o solo" é simplificação grosseira.

### C.3 NÃO sustentado — não ensinar como fato

**Hotéis de insetos / "insect hotels" (Mid Game).**

- MacIvor & Packer 2015, *PLoS ONE* (~600 hotéis, Toronto, 3 anos): *"bee hotels appear to differentially
  augment populations of **wasps** rather than those of native bees"*; abelhas nativas **mais
  parasitadas** que introduzidas; cunhou o termo **"bee-washing"** — https://doi.org/10.1371/journal.pone.0122126
- Rahimi et al. 2021 (revisão de 50 estudos): ocupação média 37,1%; características estruturais **não**
  afetam a eficiência — https://link.springer.com/article/10.1186/s41610-021-00192-z
- NC State Extension 2019: *"we really don't have strong evidence for or against bee hotels as a tool for
  insect conservation"*.

**Veredicto:** o ROADMAP promete *"attract beneficial insects for natural pest control"* sem lastro.
Se implementado, deve ser explicitamente **questionado no jogo** (ex.: Elena explica o que a ciência
sabe e não sabe) — o que é uma oportunidade educacional genuína, não um bônus de mecânica.

### C.4 O problema da escala de tempo

Os benefícios de rotação se manifestam em **21–52 anos** de campo. Uma temporada de Stardew tem 28 dias.
Qualquer modelagem fiel é impossível; o mod deve tratar rotação como **abstração de jogo**, não como
simulação agronômica. Nosso `+15` de compostagem por aplicação está dentro de uma ordem de grandeza
defensável (o compostagem real aumenta SOC na casa de dezenas de por cento ao longo de anos, não em 15 pontos instantâneos).

---

## D. Veredicto

1. **Viabilidade técnica:** a mecânica "solo → crescimento" é **barata e de baixo risco** (campos públicos
   de `Crop`, sem asset novo, reusando o scan de `DayStarted` já existente). A mecânica "solo → qualidade"
   é mais cara (patch em `Crop.harvest`, transpiler ou postfix) mas tem precedente sólido. **Corrige** a
   premissa do briefing interno de que tudo exigiria Harmony.
2. **Baseline competitivo:** o concorrente direto (ReGen Valley) já entrega a promessa nuclear.Competir
   por originalidade é perdido na largada.
3. **Agronomia:** compostagem, mulch e adubo verde são **ensinaíveis como fato**. Three Sisters é
   **narrativa exagerada** — modelo como vantagem por área, não por planta. Hotéis de insetos são
   **falsos** — não ensinar como benefício de pragas.
4. **Recomendação de próxima spec:** "Soil Health → Crop Growth" (fatia de crescimento), sem asset novo,
   sem Harmony obrigatório, compondo com `GetFertilizerSpeedBoost()` — e corrigindo antes o bug de
   rollback parcial de `CollectCompost`.

---

## Fontes primárias citadas

**API/plataforma:** SMAPI releases GitHub · `Modding:Migrate_to_Stardew_Valley_1.6` (wiki) ·
`Modding:Crop data` (wiki) · espelho decompilado `WeDias/StardewValley` (Crop.cs) · `spacechase0/LuckSkill/Mod.cs` ·
DeepWiki aedenthorn/StardewValleyMods (Crop Variation, Harmony strategies) · Nexus (ReGen Valley, Ultimate
Fertilizer, Watering Path V2, Crop Quality).

**Agronomia:** Geoderma 2023 (mulch meta-analysis) · Annals of Botany 2014 (Three Sisters roots) ·
Mt. Pleasant 2010 (Iroquoian yields) · doi 10.1002/ppp3.10576 (Catawba, 2024) · UW-Stevens Point 2007
(Wisconsin Three Sisters) · Soil & Tillage Research 2021 (rotation, 21/36 yr) · Agriculture 2024 (52 yr) ·
Agronomy 2021 (Po Valley 50 yr) · Agronomy 2019 (green manure 34 yr) · Water 2022 (straw mulching review) ·
PLoS ONE 2015 (bee hotels) · J Ecology & Environment 2021 (artificial nests review) · NC State Extension 2019 ·
SciELO (cobertura morta, bananeira e cenoura) — idioma PT.
