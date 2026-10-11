# Briefing — Feedback de colheita por tier de solo

> **Status:** briefing (não implementado). Adiado explicitamente para `/speckit.specify`. Este briefing enumera **todos** os assets que o feedback por tier de solo impõe, não especifica a feature.
> **Citações-âncora:** `ROADMAP.md:9-11` · `specs/001-soil-health-visualization/briefings/overlay-procedural.md` · `specs/004-playable-compost-loop/spec.md:57,228` · `LivingRoots/Constants.cs:31-34` · `LivingRoots/Domain/Visualization/HealthCategory.cs` · `LivingRoots/Services/CompostApplicationService.cs:61-75`.
> **Por que este briefing existe:** `ROADMAP.md:9,11` promete "no stars" em solo pobre, "faster growth / chance of extra harvests / silver-gold-iridium" em solo vivo. **Zero cobertura** em todo o repo. Documenta o inventário completo de sprites/SFX/i18n que esses requisitos impostos exigem.
> **Não promover para `specs/` sem `/speckit.specify`.**

---

## 0. Sumário executivo (TL;DR)

Este briefing inventaria tudo que as três promessas de tier do ROADMAP (`ROADMAP.md:9-11`) impõem sobre assets, data entries, hooks, i18n e testes. **Recomendação:** implementar feedback por tier como eventos transientes (`TemporaryAnimatedSprite` + texto flutuante em `Display.Rendered`), dirigidos pelas bandas de `HealthCategory` — **não** como overlay permanente por frame e **não** via data entries. **O que trava:** nenhuma das mecânicas prometidas é alcançável só com data — `Data/Crops.DaysInPhase` (campo real; **não existe** `DaysPerStage` no schema) e a qualidade de `Object` são globais/por-instância, não por-tile, logo crescimento mais rápido e downgrade de qualidade **precisam de código runtime** (Harmony ou manipulação de campos públicos em `DayStarted`/colheita), e rolls de chance precisam ser host-authoritative (Principle VI). Nada está implementado; promover exige `/speckit.specify`.

## 1. As três promessas do ROADMAP restadas como requisitos mecânicos + asset

Promessas em `ROADMAP.md:9-11`; o que destrói/constrói saúde em `ROADMAP.md:13-26` (bare tilled soil, monoculture e Joja Fertilizer destroem; compost, rotação, policultura, adubo verde, mulch e manejo animal constroem — cada um com briefing próprio em `specs/future-roadmap/briefings/`).

**Aviso de reconciliação (decisão de spec, não deste briefing):** as bandas do ROADMAP (0–30 / 31–70 / 71–100) **diferem** das bandas implementadas e travadas pela spec 001 (clarificação 2026-09-05): Poor `[0, 34)`, Moderate `[34, 67)`, Healthy `[67, 100]` (`HealthCategory.cs:29-43`, intervalos half-open). Qualquer spec de tier outcomes deve escolher uma das duas escalas e travar a outra.

**Aviso de modelo:** `MaturationMaxLevel = 5` / `MaturationIncrementDays = 7` (`Constants.cs:60-62`) são o **nível de maturação da composteira** (0–5), **não** tiers de solo. O multiplicador 1–5× de output foi aposentado pela FR-005a da 004 (one-in-one-out); `MaturationLevel` ainda incrementa na coleta (`CompostingBinService.cs:97-100`) mas não altera mais output. A escada de tiers vem **exclusivamente** de `HealthCategory`, nunca do bin.

| Promessa do ROADMAP | Banda implementada | Requisito mecânico | Requisito de asset |
|---|---|---|---|
| **Poor** — "Slow growth, high chance of pests, lower quality produce (no stars)" (`ROADMAP.md:9`) | `HealthCategory.Poor` `[0, 34)` | (a) qualidade de colheita travada em 0 (none) — §5; (b) crescimento mais lento que vanilla — §5; (c) chance alta de pragas — **sem superfície no repo** (dono: `briefings/pest-system-mvp.md`, feature separada) | sprite 16×16 procedural de feedback (§3), SFX de rejeição, `soil-health.poor` (§6) |
| **Medium** — "Similar to the base game" (`ROADMAP.md:10`) | `HealthCategory.Moderate` `[34, 67)` | **zero delta vs vanilla** — é o controle de regressão: nenhuma interceptação pode alterar este tier, ou o baseline some | nenhum asset novo (silêncio = vanilla); tooltip já existe (spec 001) |
| **Living** — "Faster growth, chance of extra harvests, higher quality (silver/gold/iridium), and unlocks new interactions" (`ROADMAP.md:11`) | `HealthCategory.Healthy` `[67, 100]` | (a) multiplicador de velocidade > 1 — §5; (b) roll de colheita extra, host-authoritative — §7; (c) qualidade acima do roll vanilla (prata/ouro/irídio) — §5; (d) "new interactions" — promessa **vaga**, requer spec própria (fora de escopo, §11) | sprite 16×16 procedural + texto flutuante (§3), SFX de recompensa, `soil-health.living` (§6) |

## 2. Ordem de camadas e composição com overlay de saúde (spec 001)

Pilha de render canônica (`overlay-procedural.md` §3.6, wiki `Modding:Modder Guide/APIs/Events`):

```
Rendering → [world: Back→Buildings→Front→Characters→AlwaysFront] → RenderedWorld
  → RenderingActiveMenu → RenderedActiveMenu → RenderingHud → RenderedHud → Rendered
```

| Camada | Onde renderiza | Alpha | Composição |
|---|---|---|---|
| Overlay de saúde do solo (spec 001, **existente**) | `Display.RenderedWorld` | base = `config.Opacity` (default **0.5**, `Constants.cs:37`); padrões = **≥ 0.7** (`PatternMinOpacity`, `Constants.cs:38`; CHK011/CHK012) | `Texture2D` 1×1 branca tintada (`VisualizationTextures.WhiteTexture`), `destRect` 64×64, mesmo `SpriteBatch` do jogo (`e.SpriteBatch`; nenhum `Begin/End` novo — `overlay-procedural.md:374`) |
| Sprite/texto de feedback por tier (**proposto**, event-driven) | `GameLocation.TemporarySprites` — desenhado pelo motor no **world pass** da location, **antes** do hook `RenderedWorld` | alfa via `alphaFade` (**0.02** no precedente `+15`, `CompostApplicationService.cs:76`), transiente (1 loop) | `Texture2D` 16×16 procedural ou `sprite.text` flutuante; desaparece sozinho — **não** é overlay por frame |
| Tooltip / floating text de tier (**proposto**) | `Display.Rendered` (acima de HUD, menus e cursor) | alfa do texto (fade linear, modelo `HoeFeedbackRenderer`) | UI interativa, sempre legível |

Regras de composição:

1. **Ordem no frame:** `TemporarySprites` (world pass) → hook `RenderedWorld` (overlay de saúde tinta por cima, alfa 0.5 — o texto do sprite permanece legível porque a tinta é semi-transparente) → `Rendered` (tooltip/floating text).
2. **Degradation threshold de 1000 tiles:** em modo `auto`, > 1000 tiles visíveis desliga **apenas** os padrões de acessibilidade (`OverlayRenderer.CheckDegradation:53-61` + `GetOverlays:114-132`; cores e opacidade **permanecem** — FR-009). Feedback de tier é event-driven → **imune** ao threshold (não consome o budget de 16.67 ms por frame). Um marcador de tier **persistente** por tile seria overlay por frame e herdaria a degradação (só cor sólida acima de 1000) — decisão de spec pendente, **não** assumida aqui.
3. **Regra "nenhum .png novo":** satisfeita — sprites de tier são procedurais (`new Texture2D(device, 16, 16)` + `SetData`, singleton no padrão `VisualizationTextures`) ou `sprite.text` flutuante. `LoadFromModFile` (o mecanismo de PNG, padrão 004 §7) **não** é usado para tiers.
4. **Restrições de acessibilidade herdadas (CHK010–CHK014):** listras ≥ 4 px de largura, pontos ≥ 4 px de diâmetro com ≥ 4 px de espaçamento (CHK010); alvo de padrão independente da opacidade configurada (CHK011/CHK012); 3:1 contra padrões brancos — `#D97706` e `#15803D` verificam em 3.19:1 e 5.02:1 (CHK013, resolvido 2026-09-13); textura 16×16 de padrão é detalhe de implementação (CHK014). **Atenção (gap #4 do briefing CVD):** a implementação atual tinge os padrões na cor da categoria (`VisualizationService.DrawPatternOverlay:247-266`), o que dá contraste 1.00:1 por construção — CHK013 está insatisfeito como especificado; decisão de spec pendente (CVD briefing §14 R2).
5. **Split pendente:** hoje `ModController.OnRenderedWorld` (`:782-806`) renderiza overlays + tooltip + hoe feedback **tudo** em `RenderedWorld` (com viewport placeholder 100×100 e cursor (0,0)); o split mandatado (overlay em `RenderedWorld`; tooltip/feedback em `Rendered`) está especificado (`overlay-procedural.md` §3.6, decisão de spec 2026-09-11) mas **não implementado**. Tier outcomes herdam o split quando ele existir.

## 3. Inventário de sprites e SFX

| Tier | Asset | Tipo | Pixels | Caminho | Cue / SFX |
|---|---|---|---|---|---|
| Poor | `marker-poor` (nome proposto) | sprite **procedural** em runtime (`new Texture2D(device, 16, 16)` + `SetData`, singleton no padrão `VisualizationTextures`) — zero PNG | 16×16 | nenhum em disco (**não** usar `Assets/*.png` — os placeholders de lá têm dono, a 004) | `cancel` (cue **verificado** no repo, `CompostApplicationService.cs:25`) |
| Medium | — (paridade vanilla) | — | — | — | nenhum (silêncio = vanilla) |
| Living | `sparkle-living` (nome proposto) | sprite **procedural** 16×16 + `sprite.text` flutuante (modelo do `+15`, `CompostApplicationService.cs:64-78`) | 16×16 | nenhum em disco (procedural) | `harvest` / `coin` / `reward` — cues vanilla **candidatos, não verificados no repo**; **verificar no build com `GamePath`**. Cues verificados no repo: `cancel`, `Ship` (`CompostingBinService.cs:102`), `axe` (`Constants.cs:72`) |

Notas:

- **`LoadFromModFile`: não usado** para tiers — é o mecanismo para servir PNGs de asset via `AssetRequested` (padrão 004 §7); texturas procedurais são criadas em runtime com `new Texture2D` + `SetData`.
- **`TemporaryAnimatedSprite`:** construtor `(Texture2D, Rectangle, float, int, int, Vector2, bool, bool)` e campos `.text`/`.color`/`.alphaFade`/`.motion` **verificados** em `CompostApplicationService.cs:64-78`. Campos não usados no repo (ex.: `layerDepth`) — **verificar no build com `GamePath`** antes de usar.
- **Wiki:** `Modding:Temporary_sprites` retornou **404** em todas as mirrors tentadas (`wiki.stardewvalley.net`, `stardewvalleywiki.com`, `stardewmodding.wiki.gg`, API MediaWiki) e não consta em `Modding:Index` — a página não foi verificável. Fatos de `TemporaryAnimatedSprite` vêm do uso verificado no repo e do schema de `Modding:Machines` (campos de `TemporarySprites` em `WorkingEffects`: `Id`, `Texture`, `SourceRect{X,Y,W,H}`, `PositionOffset{X,Y}`, `Color`, `Scale`, `Interval`, `Loops`, `Flicker` — sibling briefing `composting-bin-sheet.md` §2.3).

## 4. Tabelas de data entry no estilo C# `AssetRequested`

Resultado honesto da pesquisa: **tier outcomes não exigem nenhuma entrada nova de data** — as mecânicas prometidas vivem em runtime, não em data. O repo **não usa Content Patcher**; o padrão é `AssetRequested` + `e.Edit` em C# (briefing `data-entries-machines-recipes.md`).

### 4.1 `Data/Crops` — o asset que a promessa de crescimento toca (sem edição)

Schema verificado em `Modding:Crop data` (oldid 186411). Campos relevantes:

| Campo | Tipo | Por que não resolve tier outcomes |
|---|---|---|
| `DaysInPhase` | `int[]` — dias por fase visual, por seed | **Global por seed**: editar via `e.Edit` muda todos os tiles que plantam aquele seed — viola "Medium = vanilla". (O campo real é `DaysInPhase`; **não existe** `DaysPerStage` no schema.) |
| `ExtraHarvestChance` | `double` 0–0.9, por crop | Global por crop; roll geométrico (esperança 1/(1−p)−1). Roll condicional por tier exige runtime. |
| `HarvestMinQuality` / `HarvestMaxQuality` | `int?` | Clamps aplicados **depois** do roll normal de qualidade, globais por crop. Setar `HarvestMaxQuality = 0` mataria a qualidade em **toda** fazenda, não só em solo Poor. |
| `CustomFields` | `Dictionary<string,string>` | **Único vetor de extensão**: o jogo ignora, o mod lê em runtime. Válido para metadado (ex.: `"LivingRoots.TierGrowthMultiplier"`), mas não altera comportamento sozinho. |

### 4.2 `Data/Objects` — nenhuma entrada nova; `Quality` não existe no schema

`ObjectData` (oldid 193966) **não tem campo `Quality`** — campos são `Name`, `DisplayName`, `Description`, `Type`, `Category`, `Price`, `Texture`, `SpriteIndex`, `ColorOverlayFromNextIndex`, `Edibility`, `IsDrink`, `Buffs`, `GeodeDrops`, `ArtifactSpotChances`, `ContextTags`, `CanBeGivenAsGift`, `CanBeTrashed`, `ExcludeFrom*`, `CustomFields`. Qualidade é propriedade de instância de `StardewValley.Object` em runtime (0 = none, 1 = silver, 2 = gold, 3 = iridium; tags `quality_none`/`quality_silver`/`quality_gold`/`quality_iridium` são adicionadas automaticamente — `Modding:Context tags` oldid 193554). Consequência: "no stars" em solo pobre **não pode** vir de data — é runtime (§5).

### 4.3 `Data/Machines` e `Data/CraftingRecipes` — N/A

Nenhuma máquina nova (a composteira já registrada pela 004 é a única do mod) e nenhum craftable novo ("Joja Fertilizer" tem briefing próprio, `joja-fertilizer.md`).

### 4.4 Trecho C# real (template `AssetRequested`, padrão do repo)

```csharp
// Padrão verificado em specs/004-playable-compost-loop/briefings/data-entries-machines-recipes.md §7.
// Para tier outcomes: NENHUM registro novo. O trecho abaixo é o template que uma futura
// spec usaria SE decidir anotar seeds via CustomFields (único vetor de extensão de Data/Crops).
else if (e.NameWithoutLocale.IsEquivalentTo("Data/Crops"))
{
    e.Edit(asset =>
    {
        // Namespace/classe C# de CropData: verificar no build com GamePath
        // (padrão dos irmãos: StardewValley.GameData.<Domínio>.<Classe>Data).
        var data = asset.AsDictionary<string, CropData>().Data;
        // Exemplo de metadado (não altera comportamento sozinho — o mod lê em runtime):
        // data["472"].CustomFields ??= new Dictionary<string, string>();
        // data["472"].CustomFields["LivingRoots.TierGrowthMultiplier"] = "1.5";
    });
}
```

Notas: `e.NameWithoutLocale` para `Data/*` (não localizados); `e.Edit` é declarativo — SMAPI reexecuta a cada load; `LoadFromModFile<Texture2D>` só se houver PNG (não é o caso — §3). IDs de seed em `Data/Crops` são strings não-qualificadas (ex.: `"472"` = Parsnip Seeds).

## 5. Avaliação de invasividade: hooks de velocidade de crescimento e downgrade de qualidade

| Mecânica | O que a pesquisa encontrou | Precisa de Harmony? | Veredito honesto |
|---|---|---|---|
| Crescimento mais rápido (Living) | `DaysInPhase` é dado por seed, global; não existe API SMAPI de velocidade de crescimento por tile | **Sim** — ou manipulação de campos públicos. Opções: (a) Harmony prefix/postfix no caminho de crescimento diário (métodos `Crop.dayUpdate` / `HoeDirt.dayUpdate` — **nomes a verificar no build com `GamePath`**); (b) passagem própria em `DayStarted` que avança fases via campos públicos (`Crop.currentPhase` / `dayOfCurrentPhase` — **verificar nomes no build**). (b) evita a dependência Harmony mas ainda toca estado interno do jogo. | **Precisa** de código runtime. Nenhum caminho data-only. Risco de double-dip com `IsPaddyCrop` (crescimento acelerado vanilla) e `RegrowDays` — o multiplicador deve **compor** com esses, não substituir. |
| Downgrade de qualidade (Poor, "no stars") | `ObjectData` não tem `Quality` (§4.2); `HarvestMin/MaxQuality` são clamps globais por crop (matariam a qualidade em toda fazenda) | **Sim** — o roll de qualidade vanilla ocorre no caminho de colheita (método `Crop.harvest` — **verificar nome no build**). Interceptar o resultado e forçar `Object.Quality = 0` (propriedade pública settable — **verificar no build**) para tiles na banda `[0, 34)`. | **Precisa** de interceptação runtime. Deve **compor** com o roll vanilla (nível de Farming, fertilizer `quality_fertilizer_item`, etc.), não substituí-lo — senão Medium deixa de ser vanilla. |
| Colheita extra (Living) | `ExtraHarvestChance` existe em data mas é global por crop | Roll condicional por tier é runtime; o roll deve ser host-authoritative (§7) | **Precisa** de runtime. Probabilidade numérica: o ROADMAP não dá número — é decisão de balanceamento da `/speckit.specify` (não deste briefing). |
| Pragas (Poor) | **Zero superfície no repo** | — | Feature separada (`briefings/pest-system-mvp.md`); sem ela, a promessa "high pests" não tem implementação possível. |
| "New interactions" (Living) | Promessa vaga do ROADMAP | — | Requer spec própria; fora de escopo (§11). |

**Conclusão:** das mecânicas prometidas, **todas** que alteram gameplay precisam de código runtime; **nenhuma** é entregue por data entries. A única mecânica de baixa invasividade é o feedback visual/SFX (§3), que não altera gameplay.

## 6. i18n

Estado atual verificado: `LivingRoots/i18n/` tem **`default.json` e `en.json`** (13 chaves, conteúdo idêntico — todo o domínio `compost.*`, `composting-bin.*`, `floating-text`, `restoration-text`, `maturity-tooltip`, `message.*`). **Zero chaves `soil-health.*`**. **Nenhum C# carrega i18n hoje** (gap #13 do briefing `color-schemes-cvd.md`; o padrão `translation.Get(...).Default(...)` do 004 ainda não está wired). Não existe `pt.json`.

Chaves novas propostas (convenção existente: dot-case, prefixo por domínio):

| Chave | Uso | Fallback EN |
|---|---|---|
| `soil-health.poor` | nome do tier (tooltip, floating text) | "Poor Soil" |
| `soil-health.medium` | nome do tier | "Medium Soil" |
| `soil-health.living` | nome do tier | "Living Soil" |
| `soil-health.harvest.extra` | feedback de colheita extra | "Extra harvest!" |
| `soil-health.harvest.no-stars` | feedback de qualidade travada | "Soil too poor for quality crops" |

Pré-requisito: um loader `helper.Translation` precisa existir antes que qualquer chave nova renderize (dívida herdada). `Data/Objects`/`Data/BigCraftables` são carregados por locale (`e.NameWithoutLocale` ignora o sufixo) — se troca de idioma em runtime não refletir, `InvalidateCache` em `GameLoop.LocaleChanged` (padrão 004 §8, verificar em jogo).

## 7. Chance host-authoritative sob Principle VI (`.specify/memory/constitution.md:28-37`)

Citação obrigatória — Principle VI (NON-NEGOTIABLE):

- "**Farm state is shared, host-authoritative state.** Soil health values, composting bin state, and all persisted domain state belong to the farm, not to an individual player. Exactly one authority owns the truth for a given farm; clients MUST NOT fork divergent copies of farm state."
- "**Persistence continues to use the Stardew Valley save system only** ... The host's save IS the save."
- "**Divergence handling is explicit.** When client and host state can disagree, the resolution rule MUST be stated in the specification for that feature. Silent last-write-wins on farm tiles is not acceptable without a documented rationale."

Como isso chega no roll de chance:

1. O roll (colheita extra, praga) executa **uma vez, no host** — guarda `Context.IsMainPlayer` (API SMAPI; **verificar no build com `GamePath`**). O cliente nunca rola.
2. O **resultado** (item extra produzido, praga presente naquele tile) é estado da fazenda: persiste via sistema de save do SV (o save do host é o save) e sincroniza pelo fluxo multiplayer do motor — não é re-rolado por cliente.
3. `TemporaryAnimatedSprite` de feedback é **puramente visual e local**: cada cliente mantém os seus (`TemporarySprites` **não** trafegam pela rede — o sprite em si não é estado). O que sincroniza é o efeito (item/qualidade), não o sprite.
4. Precedente no repo: `TryApplyCompostWithLock(location, tile, farmerId, ...)` com `farmerId = Game1.player?.UniqueMultiplayerID ?? 0L` (`ModController.cs:871-872`) e a chave `message.compost.action-conflict` ("Another player is already interacting with this tile") — o padrão de conflito já existe para aplicação de compost; tier outcomes herdam o mesmo modelo.
5. Regra de divergência a escrever na futura spec (exigência de Principle VI): "rolls de tier são host-authoritative; cliente nunca aplica efeito localmente; em divergência, o estado do host vence e o cliente re-sincroniza no próximo `DayStarted`."
6. Gate 3 da constitution: verificação end-to-end **com segundo jogador conectado** — host e farm hand observando o mesmo valor e o mesmo resultado de colheita.

## 8. Plano de testes

Arquivos que mudariam (pós-`/speckit.specify`; TDD — constitution Principle IV, um arquivo por sujeito):

| Arquivo | Mudança esperada |
|---|---|
| `LivingRoots.Tests/SoilTierOutcomesTests.cs` (a criar) | Lógica de tier: banda → efeito (qualidade travada, multiplicador, roll). |
| `LivingRoots.Tests/AssetRegistrationTests.cs` | Só se a futura spec adicionar entries de data (ex.: `CustomFields` em `Data/Crops`) — padrão T010/T022 do briefing 004. |
| `LivingRoots.Tests/CompostApplicationServiceTests.cs` | Se o feedback de tier acoplar à aplicação de compost (o sprite `+15` já é o modelo). |
| `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` | Rolls host-authoritative (Principle VI). |
| `LivingRoots.Tests/SoilHealthVisualizationTests.cs` | Se um marcador de tier persistente for aprovado (hoje não é — §2). |

Teste que **não quebra se a paleta mudar** (exigido): asserts em **lógica**, nunca em cor. Exemplo nomeado: `SoilTierOutcomesTests.PoorTier_HarvestQualityIsAlwaysZero` — dado qualquer valor em `[0, 34)`, o resultado de colheita tem `Quality == 0`, sem referência a `ModConstants.PoorColor` ou a qualquer `Color`. Segundo exemplo: `HealthCategoryExtensions_FromHealthValue_HalfOpenBoundaries` (limiares 34/67, intervalos half-open — `HealthCategory.cs:29-43`), que testa categorias, não pixels. Regra: nenhum assert em literais R/G/B; a paleta é opt-in (`cvdSafe`, briefing `color-schemes-cvd.md`) e o default não se move (CHK013).

Armadilhas de teste existentes: `FeatureStatusTests` lê `README.md` como recurso embutido e asserta 3 strings `"(Planned)"` — editar README quebra a suite; `ArchitecturalPlanTests` exige quebra de linha final em todo `.md` de `docs/architectural_and_refactor_plans/**` (este briefing está em `specs/`, fora da árvore, mas o padrão se aplica a qualquer doc novo naquela árvore).

## 9. Verificação em jogo

Pré-requisito: build Release com `GamePath` (AGENTS.md raiz); `dotnet format --verify-no-changes` verde.

Passos manuais (~30 min; o template de PR exige teste manual — o mod não é totalmente verificável por testes unitários):

1. Console SMAPI: `help` (lista comandos); `list_items LivingRoots` → `(O)LivingRoots.Compost` e `(BC)LivingRoots.CompostingBin` presentes (não Error Item); `debug listtags` com compost na mão → `compost_item` + `category_artisan_goods`. Comando do mod: `lr_version` (único registrado — gap #14 do briefing CVD). **Não existe comando para setar soil health** — o caminho em jogo é aplicar compost (+15 por uso, `RestorationAmount`).
2. Arar 3 tiles; aplicar compost 0× (Poor, 0%), 3× (+45 → Moderate), 5× (+75 → Living). Cada aplicação mostra o sprite `+15` verde flutuante (`CompostApplicationService.cs:64-78`).
3. Hover no tile: tooltip `Soil Health: {n}% ({category})` (formato spec 001); overlay tintado `#B91C1C` / `#D97706` / `#15803D` com alfa 0.5; padrões (listras/pontos/sólido) com alfa ≥ 0.7.
4. Zoom out até > 1000 tiles visíveis: padrões desligam (log `Warn` one-time), cores e opacidade permanecem (FR-009, `OverlayRenderer.cs:114-132`).
5. Plantar a mesma seed nos 3 tiles (mesma estação) e contar dias até colheita — **hoje iguais nos três** (feature não implementada; §5 é o que a spec precisa decidir).
6. Colher: qualidade do item — **hoje o roll vanilla é idêntico** nos três tiers (feature não implementada).
7. Save/reload; depois multiplayer com 2 jogadores (Gate 3): host aplica compost em um tile, farm hand vê o mesmo valor e o mesmo tooltip.

## 10. Riscos

| # | Risco | Gravidade | Mitigação |
|---|---|---|---|
| 1 | **Bug latente de `CollectCompost` com output > 1** (`CompostingBinService.cs:77-89`): `outputCount` é hardcoded 1, mas o loop `for (i < outputCount)` com `TryAddItem` **não faz rollback de sucesso parcial** — se um futuro output multi-item (`MaxStack > 1` ou yield por nível de maturação) encher o inventário no 2º item, o 1º já está no inventário, o bin permanece `Ready` e o retorno é 0 → o jogador fica com o item **e** pode coletar de novo (duplicação). Hoje mascarado por `outputCount = 1`; qualquer aumento de output ativa o bug. | Alta | Corrigir (rollback ou produção atômica) **antes** de qualquer feature que aumente output; cobrir com teste de inventário-cheio multi-item. |
| 2 | **Paleta default documentadamente não é CVD-safe** (medido, briefing `color-schemes-cvd.md` §6.1): deutan Poor/Healthy ΔE₀₀ **8.4**, protan **9.6**, achromatopsia Healthy/Unknown **1.2** com lightness **invertida** (Moderate L 0.653 > Healthy 0.542). Feedback de tier que depende só de cor viola GAG ("no essential information conveyed by a fixed colour alone"). | Alta | Canal não-cromático obrigatório (forma do sprite / texto flutuante); default **não se move** (CHK013 + regra de ouro do briefing CVD); esquema `cvdSafe` é opt-in. |
| 3 | **Descontinuidade da rampa em health = 67** (`ColorInterpolationService.cs:87-110`): a banda `[67, 100]` recomeça de `Moderate`; queda de L −0.008 (default, imperceptível) / **−0.230** (`cvdSafe`, muito visível — tile a 66% "reverte" para âmbar em 67%). | Média | Decisão de **spec** (CVD briefing §14 R1); este briefing registra, não corrige. |
| 4 | **Hooks de growth/quality interagem com modificadores vanilla**: `IsPaddyCrop` (crescimento acelerado + rega automática), `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`, fertilizer `quality_fertilizer_item`. Multiplicador ingênuo double-dips e quebra "Medium = vanilla". | Alta | Compor (multiplicar depois dos modificadores vanilla), nunca substituir; teste de paridade vanilla no tier Medium. |
| 5 | **Rolls de chance em cliente forkiam o estado da fazenda** (Principle VI): colheita extra/praga rolada localmente em cada cliente produz fazendas divergentes. | Alta | Host-authoritative (§7) com regra de divergência escrita na spec; Gate 3 verifica com segundo jogador. |

## 11. Fora de escopo

- `GrowthChance` / balanceamento numérico (velocidade, probabilidades) — decisão da `/speckit.specify`, não de assets.
- Sistema completo de pragas (`briefings/pest-system-mvp.md`), "Joja Fertilizer" (`briefings/joja-fertilizer.md`), rotação de culturas (`rotation-journal.md`), policultura/Three Sisters (`three-sisters-polyculture.md`), adubo verde (`green-manure.md`), mulch (`mulch-soil-cover.md`), NPC Elena (`elena-npc.md`), Welcome Kit (`welcome-kit.md`), Cisterna (`small-cistern.md`) — cada um com briefing próprio.
- "New interactions" do tier Living — promessa vaga do ROADMAP; requer spec própria.
- Arte nova de PNG (regra "nenhum .png novo" — sprites procedurais, §3); `Content Patcher` (o repo registra via `AssetRequested` em C#, padrão 004); catálogo de esquemas CVD (dono: briefing `color-schemes-cvd.md`); legenda/tutorial em jogo (Out of Scope da spec 001); mecanismo de sync multiplayer (D1); mudança da paleta default (CHK013); reconciliação das bandas ROADMAP (0-30/31-70/71-100) vs implementadas ([0,34)/[34,67)/[67,100]) — decisão de spec.
- Itens de prêmio ("Estrela de Terra Viva" etc.) — não prometidos pelo ROADMAP; qualquer item novo seria feature separada com seu próprio registro em `Data/Objects`.

---

Última verificação: 2026-10-05 — subagente de pesquisa (sessão pai). Fontes externas verificadas por fetch direto: `Modding:Crop data` (oldid 186411), `Modding:Objects` (oldid 193966), `Modding:Context tags` (oldid 193554), `Modding:Recipe data` (via briefing 004, rev. 2026-05-20). `Modding:Temporary_sprites` retornou 404 em todas as mirrors — fatos de `TemporaryAnimatedSprite` vêm do uso verificado no repo (`CompostApplicationService.cs:64-78`) e do schema de `Modding:Machines`. Membros de API não usados no repo estão marcados "verificar no build com `GamePath`". Arquivo termina com quebra de linha final.
