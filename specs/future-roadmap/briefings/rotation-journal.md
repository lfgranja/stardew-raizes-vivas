# Briefing — Rotation Journal + Famílias Botânicas (Epic 8 · US-08-01 / US-08-02)

> **Escopo:** ROADMAP `ROADMAP.md:46` ("Crop Rotation… Rotation Journal… Brassicas / Solanaceae / Legumes… quality bonuses + Soil Health") + Epic 8 (Issue #10): US-08-01 Journal (#38), US-08-02 famílias/bônus (#39) em `LivingRoots/docs/EPICS_USER_STORIES.md:53-57`.
> **Status:** briefing (não implementado). Insumo para futuro `/speckit.specify` — não é especificação. Não promover para `specs/` sem specify.
> **Agrupado de origem:** `specs/future-roadmap/briefings/mid-game-year2-3.md` Cap. 1 (§§1.1–1.6). Este arquivo é a versão individual e canônica — o agrupado permanece como índice.
> **Leitura interna:** `LivingRoots/Constants.cs` (esparso, `MaxTilesPerLocation/MaxTilesPerSave`, `DailyDecayRate`, paleta), `LivingRoots/ModEntry.cs:114-177` (padrão `OnAssetRequested`), `LivingRoots/i18n/en.json` (chaves `snake.case`), design system `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` §§4/5/8/11, `specs/003-composting-tests/briefings/compost-item.md` (lição dos dois IDs qualificados).

---

## 1. Conceito de jogo

Classificar plantas por família botânica e premiar rotação correta. Regra mental: **mesma família no mesmo tile em sequência = monocultura** (−saúde, sem estrela); **família diferente = rotação** (+saúde, +chance de qualidade). **Legume após qualquer família = bônus extra de N** (fixa nitrogênio — sinergia direta com `green-manure.md`: trevo é Legume).

Famílias v1 (enum `CropFamily` em `Domain/`): `Brassica, Solanaceae, Legume, Cucurbit, Allium, Root, Grain, Flower, Unknown`. Tabela vanilla inicial (a spec confirma cada seedId):

| Família | Sementes vanilla (exemplos) |
|---|---|
| Brassica | Cauliflower, Cabbage |
| Solanaceae | Tomato, Pepper, Potato, Eggplant |
| Legume | Green Bean, Pea (+ Clover do mod) |
| Cucurbit | Pumpkin, Melon |
| Neutras/bônus próprio | Corn + Pumpkin (Three Sisters, Epic 3) |
| Unknown | flores vanilla/forrageáveis (não afetam rotação nem mel) |

## 2. Sprite

| Asset | Spec | Notas |
|---|---|---|
| Ícones de família 16×16 (9) | `Assets/rotation/family_<brassica\|solanaceae\|legume\|cucurbit\|allium\|root\|grain\|flower\|unknown>.png`, pixel-art 16×16, fundo transparente, paleta do design system (`Mulch #92400E`, `Healthy #15803D`, `Sun #FACC15`) | Preferível: textura custom única `LivingRoots/RotationIcons` + `SpriteIndex` 0–8 (1 bind, sem troca de textura — cf. `research/xna-monogame-color-rendering-report.md` abordagem B). Fallback: quadrado branco tingido via `ColorDTO` se textura ausente (null-guard em `GetTexture()`, nunca crashar — cf. `remediation-visualization-rendering.md` FR-004) |
| Selo de tooltip | reutilizar pattern textures (stripes/dots/solid) — sem asset novo | Brassica=stripes, Solanaceae=dots, Legume=solid+folha (§8 design system) |
| Capa do Journal | `Assets/ui/journal_cover.png` 64×64 (livro + raiz), estilo `TileSheets` | Opcional P1; menu funciona sem |

## 3. Data

- `Data/Crops` (sementes **do mod**, ver `green-manure.md` / `living-pharmacy-tonics-sprays.md`): `"CustomFields": { "LivingRoots.Family": "Legume" }` etc. Para sementes **vanilla, NÃO editar `Data/Crops`** — lookup C# (`CropFamilyRegistry`, `Domain/Services/`, puro, testável sem jogo).
- Journal como item? **Não.** Journal é menu, não item — abre via tecla configurável + aba no menu do mod, não via inventário (YAGNI: evita `SpriteIndex`/receita à toa).
- `Data/CraftingRecipes`: nenhuma (journal concedido pela Elena em evento Year 2 via `mail` + flag `LivingRoots.RotationUnlocked` em `ModData`).
- Armazenamento: anelar `soil_health_data_<saveId>` com `rotation_history_<saveId>`: `Dictionary<location, Dictionary<"X,Y", Queue<familyId>>>`, profundidade 2–3 safras (`RotationHistoryDepth`). **Esparso**: tile sem histórico não entra no save (mesmo princípio dos zeros omitidos no solo). Respeitar `MaxTilesPerLocation`/`MaxTilesPerSave`.
- Constantes novas (`ModConstants`, `LivingRoots/Constants.cs`): `RotationHistoryDepth = 3`, `RotationBonusHealth = +8f`, `MonoculturePenaltyHealth = −6f`, `RotationQualityBonusChance = 0.10f`, `RotationModDataKey = "LivingRoots.RotationHistory"`.

Referência externa — `Modding:Crop_data` (oldid 186411, verificado 2026-10-05): `Data/Crops` tem chave = ID da **semente**; campos `Seasons[]`, `DaysInPhase[]`, `HarvestItemId`, `HarvestMethod (Grab/Scythe)`, `ExtraHarvestChance` (geométrica `1/(1-p)−1`), `CountForMonoculture/CountForPolyculture`, `PlantableLocationRules`, `CustomFields` (**aqui vai a família botânica**). `CustomFields` é o campo canônico para metadados de mod sem poluir o schema vanilla.

## 4. UI

- **Custom `RotationJournalMenu : IClickableMenu`** — única exceção à regra "GMCM para config" (GMCM é formulário; journal é browser tabular navegável). Página por estação/talhão, cada tile com últimas N famílias (ícone + nome), alerta "repita / rote" e média de saúde do talhão. Navegação: setas + `drawHoverText`; fonte `dialogueFont` no título, `smallFont` no corpo; respeitar zoom/UI scale (`Game1.GlobalToLocal()`, `Utility.ModifyCoordinatesForUIScale()`); fecha com `Esc/E`; tecla de atalho configurável (default `J`).
- **HUD/tooltip**: estender `FormatTooltipText` para `"Soil Health: 72% (Healthy) · Last: Solanaceae → plant Legume"` (largura máx 200px + wrap, §4 design system).
- **Hoe feedback**: manter flash 300 ms + texto 1000 ms; cor = categoria de saúde (não de família — família vai no texto).
- **GMCM página "Rotation"** (config, não conteúdo): `RotationBonusesEnabled`, `JournalToastsEnabled`, `RotationStrictness` (leniente/estrito), slider opacidade do selo (0.2–0.8). Registrar via API GMCM com fallback silencioso se GMCM ausente (nunca dependência dura no `manifest.json`).
- Acessibilidade (Game Accessibility Guidelines, não WCAG): cor nunca sozinha — padrão + texto + ícone; patterns ≥0.7 opacidade mesmo com overlay em 0.2; `ReduceMotion` mata flash/movimento; fontes só `Game1.smallFont/tinyFont/dialogueFont` com `MeasureString`, sombra 1px; tooltip nativo via `IClickableMenu.drawHoverText`.

## 5. i18n (seguir `i18n/en.json`, chaves minúsculas com pontos)

```json
{
  "rotation.journal.title": "Rotation Journal",
  "rotation.family.brassica": "Brassicas",
  "rotation.family.solanaceae": "Nightshades",
  "rotation.family.legume": "Legumes",
  "rotation.verdict.rotate": "Rotate ✓ ({0} → {1})",
  "rotation.verdict.repeat": "Same family — soil tires ({0})",
  "rotation.toast.bonus": "Good rotation! +{0} soil health.",
  "rotation.toast.monoculture": "Monoculture… soil loses health.",
  "rotation.config.strictness": "Rotation strictness"
}
```

`DisplayName/Description` de sementes com família usam string tokenizável (`{{i18n:…}}`, padrão Dobson em `Modding:NPC_data`) ou chave i18n; texto de veredito ≥ contraste 4.5:1 sobre `Color(0,0,0,200)`. `default.json` = EN; `pt.json` P2.

## 6. Gates (o que a futura spec deve conter — checklist)

- [ ] Tabela de IDs vanilla seed→família (**HarvestItemId vs seedId** — a chave de `Data/Crops` é a **semente**), com fonte wiki 1.6.x + `oldid`; teste de contrato de regressão.
- [ ] Constantes em `ModConstants` listadas (nome + default + unidade).
- [ ] `OnAssetRequested` edits (qual asset, qual chave, payload C#) + teste chave == constante; convenção de IDs `LivingRoots.<Nome>`, comparação contra `QualifiedItemId` nas duas formas `(O)…`.
- [ ] Serviço: interface em `Domain/Interfaces/`, impl em `Services/`, `new` só em `ModEntry.cs`, eventos em `ModController` com `ExecuteWithConcurrencyGuard` onde houver reentrância.
- [ ] Persistência: chave `ModData`, formato, esparsidade, limites `MaxTiles*`; purga/cap documentados.
- [ ] UI: GMCM (toggles) vs. custom (só Journal) + tooltip/overlay/hoe-feedback + `ReduceMotion` + contraste + larguras.
- [ ] Multiplayer (Principle VI, host-autoritativo): histórico por tile com merge por `PerScreen`/host (`HostAtomicLockService` existente — reutilizar); nada single-player-only.
- [ ] Verificação: gate unitário (`GamePath=$GP dotnet test --filter …`) + gate in-game (SMAPI sem vermelho, journal abre, veredito correto num tile de teste) — padrão `compost-item.md §4`.
- [ ] Fora de escopo explícito (YAGNI): detecção automática de "talhão" por proximidade é P2; v1 = por tile + média da location.

## 7. Riscos

1. Mapear IDs vanilla corretamente (semente vs. colheita) — erro mais barato de prevenir, mais caro de corrigir.
2. Não prometer talhões auto-detectados no v1.
3. Pré-requisito transversal: D3 (sem superfície jogável) e D1 (multiplayer) do ROADMAP §Deferred Intents — estado novo por tile nasce multiplayer-safe.
4. Dependências: este briefing é a **base de "o que foi plantado onde"** — `green-manure.md` escreve no histórico; ordem sugerida: pragas MVP → este → green manure.

## Fontes

- Internas: `ROADMAP.md:46`; `LivingRoots/docs/EPICS_USER_STORIES.md:53-57`; `LivingRoots/Constants.cs`; `LivingRoots/ModEntry.cs:114-177`; `LivingRoots/i18n/en.json`; design system §§4/5/8/11; `research/xna-monogame-color-rendering-report.md`; `specs/003-composting-tests/briefings/compost-item.md`.
- Externas (verificadas 2026-10-05): `Modding:Crop_data` (oldid 186411) — chave=semente, `CustomFields`, `CountForMonoculture/Polyculture`; `Modding:Objects` (oldid 193966) — DisplayName/Description tokenizáveis, `ContextTags`; Range Highlight / `Modding:Common_tasks` (GMCM vs `IClickableMenu`).
