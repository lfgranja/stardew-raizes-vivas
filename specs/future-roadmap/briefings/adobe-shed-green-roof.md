# Briefing — Bio-construção: Adobe Shed + Green Roof (Epic 13 · US-13-01 / US-13-02)

> **Escopo:** ROADMAP `ROADMAP.md:51-53` ("Robin learns from Elena: Adobe Shed — cheaper, clay-heavy, ideal temp ripens artisan goods faster + Green Roof house upgrade for Living Pharmacy herbs") + Epic 13 (Issue #15): US-13-01 shed (#49), US-13-02 telhado verde (#50) em `LivingRoots/docs/EPICS_USER_STORIES.md:84-88`.
> **Status:** briefing (não implementado). Insumo para futuro `/speckit.specify` — não é especificação. Contém os maiores riscos do lote — ler §7 antes de especificar.
> **Agrupado de origem:** `specs/future-roadmap/briefings/mid-game-year2-3.md` Cap. 6. Versão individual canônica.
> **Leitura interna:** `manifest.json` (sem `Content Patches`/`Dependencies` — conteúdo via `OnAssetRequested` em C#), `.csproj` (copia `Assets/**` + `i18n/**/*.json`), `Constants.cs`, `ModEntry.cs`.

---

## 1. Conceito de jogo

- **Adobe Shed (US-13-01):** shed alternativo: custo-ouro menor que o Shed vanilla deslocado para Clay; interior `Shed` vanilla; mecânica `Cask`-like — máquinas com `CustomData: { AgingMultiplier }` dentro do shed envelhecem X% mais rápido (queijos/vinhos). Implementação provável: `DayStarted`, para cada máquina em location-interior de Adobe Shed, avançar `minutesUntilReady` extra (hook via evento SMAPI, **sem Harmony se possível — a spec deve provar sem-Harmony ou justificar Harmony**).
- **Green Roof (US-13-02):** upgrade da FarmHouse que adiciona 6–8 plantadores no telhado, **só para ervas de `living-pharmacy-tonics-sprays.md`** (allow-list `LivingRoots.*` herb seeds; negar resto com `DeniedMessage`). Rega: manual (v1) — especificar.
- Desbloqueio narrativo: quest Elena→Robin (mail + evento), `BuildCondition` GSQ (`HasFlag LivingRoots.AdobeLearned`); sem quest no v1 = `s Farming 6 + YEAR ≥ 2` (GSQ `PLAYER_YEAR`/`YEAR`).

## 2. Sprite

| Asset | Spec |
|---|---|
| `Assets/buildings/adobe_shed.png` — exterior em adobe (tons `Mulch #92400E` + palha `Sun`), telhado vegetal parcial; **espelhar dimensões do Shed vanilla** (7×3 + portas) para reaproveitar `CollisionMap`/portas | `Texture: "LivingRoots/AdobeShed"`, `SourceRect` cheio; `DrawLayers[]` p/ trepadeira animada = P2; `SeasonOffset` = P2; P1 = 1 textura. `DrawShadow` + `FadeWhenBehind` |
| Telhado verde (upgrade) | `Assets/buildings/green_roof_overlay.png` — **não retexturizar a casa inteira**: `Skins[]` ou `DrawLayers` com 8 canteiros 16×16 no topo + escada. Requer spike do layout da textura FarmHouse (Tiled/TMX) na spec. Fallback P1: canteiros como vasos vanilla — ver §7.2 |
| Vasos do telhado | reutilizar Garden Pot vanilla (`(BC)62`) como placeholder até asset próprio — YAGNI explícito |

## 3. Data

```csharp
// Data/Buildings — Adobe Shed (wiki Modding:Buildings, oldid 191928)
data["LivingRoots.AdobeShed"] = new BuildingData {
  Name = "{{i18n:adobe-shed.name}}", Description = "{{i18n:adobe-shed.description}}",
  Texture = "LivingRoots/AdobeShed", Builder = "Robin",
  BuildCost = 8000, // < Shed vanilla 15000
  BuildMaterials = [ Clay 300, Stone 50, Fiber 50 ], // CHECAR IDs na spec: Clay (O)330, Stone (O)390, Fiber (O)771
  BuildDays = 2, BuildCondition = "YEAR 2, PLAYER_HAS_MAIL Current …", // GSQ real na spec (HasFlag LivingRoots.AdobeLearned)
  Size = new(7, 3), CollisionMap = "…", // espelhar Shed vanilla — confirmar
  HumanDoor = …, IndoorMap = "Shed", IndoorMapType = "StardewValley.Shed",
  Metadata = new() { ["LivingRoots.RipeningBonus"] = "0.25" } // lido por C#, sem subclasse
};
// Data/Buildings — Green Roof (upgrade)
data["LivingRoots.GreenRoof"] = new BuildingData {
  BuildingToUpgrade = "Farm House", // confirmar ID vanilla na spec
  Builder = "Robin", BuildCost = 15000, BuildMaterials = [ Wood 200, Fiber 100, Clay 100 ],
  BuildDays = 2, // sem Size/Collision (usa o da casa)
  ModData = new() { ["LivingRoots.HasGreenRoof"] = "true" }
};
// Aceleração: sem Data/Machines novo — C# lê Metadata do building pai da location e aplica multiplicador.
```

- Referência externa — `Modding:Buildings`: `BuildingToUpgrade` (ex. Big Coop→"Coop"; vários upgrades do mesmo building = o jogador escolhe o path), `IndoorMap` + `IndoorMapType` (**só tipos vanilla**; lista útil: `AnimalHouse`, `Cabin`, `Cellar`, `DecoratableLocation`, `FarmCave`, `FarmHouse`, `Shed`, `SlimeHutch`), `Metadata` (lido via `building.GetMetadata(key)` — inclui `ChimneyPosition…`), `ModData`, `Skins[]`, `DrawLayers[]` (com `FrameCount/FramesPerRow/FrameDuration`), `HumanDoor`/`AnimalDoor`, `BuildCondition` (GSQ), `ActionTiles`, `Chests`/`ItemConversions`, `HayCapacity`. **Aviso: `BuildingType` custom crasha save/multiplayer** — usar `Building` genérico + metadata.
- Referência externa — GSQ (`Modding:Game_state_queries`, verificado 2026-10-05): `YEAR <min> [max]`, `PLAYER_HEARTS Current <npc> <n>`, `PLAYER_HAS_MAIL Current <id>`, `SEASON …`, `WEATHER Here Rain`, `LOCATION_SEASON`, `IS_HOST`, `ANY …`. `BuildCondition`/`Condition` aceitam GSQ — usar para gates de ano/quest/estação.
- Referência externa — `Modding:Machines`: `OutputMethod` C# (`"<tipo>: <método>"`, ex. Cask `AgingMultiplier` via `CustomData`) é o precedente canônico do ripening — a spec do Adobe estuda o Cask antes de inventar.
- Serviço `IBioConstructionService`: `DayStarted` (bônus de ripening nos interiores Adobe) + plantio restrito nos canteiros do telhado. Plantio: interceptar uso de semente em tile de canteiro — **a spec precisa de spike de mapa** (onde ficam os tiles plantáveis).
- Constantes: `AdobeRipeningBonus = 0.25f`, `AdobeClayCost = 300`, `GreenRoofPlanterCount = 8`, `GreenRoofHerbOnly = true`.

## 4. UI

- Interior Adobe: placa/tooltip `"Adobe coolness: ripening +25%"` (P1: só carta + GMCM; hover nas máquinas P2).
- Telhado: tooltip por canteiro `"Roof planter — herbs only"`; negar semente não-erva com toast i18n.
- Menu Robin: entradas novas aparecem sozinhas via `Data/Buildings` (`Builder: Robin` + `BuildCondition`) — sem UI custom. `BuildMenuDrawOffset` para enquadrar sprite no catálogo.
- GMCM: `AdobeBonus` (0–50%, default 25%), `GreenRoofHerbOnly` (bool). `IClickableMenu` custom: **não**.

## 5. i18n

```json
{
  "adobe-shed.name": "Adobe Shed",
  "adobe-shed.description": "Cool clay walls. Artisan goods ripen 25% faster inside.",
  "green-roof.name": "Green Roof",
  "green-roof.description": "A living roof for your house. Grow pharmacy herbs up top.",
  "green-roof.planter.herbs-only": "Roof planters only take pharmacy herbs.",
  "green-roof.quest.elena-letter": "Robin… clay breathes. Let's build cool. — Elena",
  "adobe.ripening": "Ripening {0}% faster in adobe coolness."
}
```

`Strings/Buildings` é o padrão vanilla para Name/Description — tokenizável apontando p/ i18n do mod desde o dia 1.

## 6. Gates

- [ ] **Tabela de IDs vanilla** (Clay/Stone/Fiber/Wood, `Shed`, `Farm House`, `Coop`, Garden Pot `(BC)62`) contra `Data/Objects` + `Data/Buildings` 1.6.15 antes da spec — artefato mais barato, erro mais caro.
- [ ] Spike sem-Harmony do ripening (avançar `minutesUntilReady`/`daysUntilReady` de fora no `DayStarted`; conflitos com `OnlyCompleteOvernight`/`PreventTimePass`/Automate mapeados). Fallback honesto se inviável: bônus de **qualidade de output** em vez de velocidade — decidir na spec, não aqui.
- [ ] Spike de mapa do telhado: onde ficam os tiles plantáveis (recomendação deste briefing: **interior `DecoratableLocation` "roof interior" com 8 vasos**, acessado por escada — narrativa + overlay, sem brigar com o mapa vanilla; alternativa (b) tiles reais no TMX = frágil).
- [ ] `Building` genérico + `Metadata`/`ModData` (sem `BuildingType` custom); `IndoorMapType` vanilla-only.
- [ ] Allow-list de ervas do telhado = IDs de `living-pharmacy-tonics-sprays.md`; `DeniedMessage` i18n.
- [ ] Arte de buildings orçada: exterior multi-tile + sombras + `FadeWhenBehind` + catálogo Robin é o asset mais pesado — artista ou placeholder tingido, explícito na spec.
- [ ] Constantes em `ModConstants`; `OnAssetRequested` edits + teste chave == constante.
- [ ] Multiplayer host-autoritativo; interiores instanciados por building (atenção a `NonInstancedIndoorLocation`).
- [ ] Verificação: `dotnet test --filter` + in-game (queijo no Adobe amadurece +25%; semente não-erva negada no telhado; Robin lista os dois).

## 7. Riscos (ler antes de especificar)

1. **Acelerar máquinas sem Harmony:** campos `minutesUntilReady`/`daysUntilReady` + `OnlyCompleteOvernight` + Automate = spike obrigatório.
2. **Telhado plantável:** telhado da FarmHouse não é `GameLocation` com `hoeDirt`. Recomendação (a) roof-interior; (b) TMX real só se o spike provar.
3. **IDs vanilla** — confirmar todos (Clay `(O)330`? Stone `(O)390`? FarmHouse ID? Shed Size/Collision?) antes da spec.
4. Ordem: **por último** no Mid Game (usa ervas do Cap. 5; ripening é o spike mais arriscado) — ver `mid-game-year2-3.md §8`.

## Fontes

- Internas: `ROADMAP.md:51-53`; `EPICS_USER_STORIES.md:84-88`; `manifest.json`; `.csproj:19-29`; `Constants.cs`; `ModEntry.cs`.
- Externas: `Modding:Buildings` (oldid 191928) — `BuildingToUpgrade`, `IndoorMapType` vanilla-only, `BuildingType` warning, `Metadata`/`ModData`, `Skins`/`DrawLayers`; `Modding:Game_state_queries` (193977) — `YEAR`, `PLAYER_HEARTS`, `PLAYER_HAS_MAIL`, `WEATHER`; `Modding:Machines` (191352) — Cask `OutputMethod`+`CustomData` precedent; `Modding:Objects` (193966) — Garden Pot `(BC)62` context.
