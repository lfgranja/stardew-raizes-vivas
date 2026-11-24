# Briefing — Green Manure / Adubo Verde (Epic 9 · US-09-01 / US-09-02)

> **Escopo:** ROADMAP `ROADMAP.md:47` ("Elena sells Green Manure seeds — clover/oats — incorporate nutrients, significant Soil Health boost") + Epic 9 (Issue #11): US-09-01 sementes (#40), US-09-02 incorporação (#41) em `LivingRoots/docs/EPICS_USER_STORIES.md:59-63`.
> **Status:** briefing (não implementado). Insumo para futuro `/speckit.specify` — não é especificação.
> **Agrupado de origem:** `specs/future-roadmap/briefings/mid-game-year2-3.md` Cap. 2. Versão individual canônica.
> **Leitura interna:** `LivingRoots/Constants.cs` (`RestorationAmount = 15f` = referência do compost; `+25` aqui precisa justificar-se), `LivingRoots/ModEntry.cs:114-177` (padrão `OnAssetRequested`), `LivingRoots/Domain/Visualization/*` (categorias Poor/Moderate/Healthy), `specs/003-composting-tests/briefings/compost-item.md §2.3` (anti-loop do compost).

---

## 1. Conceito de jogo

Duas culturas de cobertura vendidas pela Elena (quando o NPC existir; antes disso, receita fallback — a spec decide **um** caminho, não os dois):

- **Clover / Trevo** (Legume, fixa N, boost grande, ciclo curto) — sinergia com `rotation-journal.md` (registra Legume no histórico) e com ICLF futuro.
- **Oats / Aveia** (Grain/grass, biomassa, boost médio + supressão de ervas).

Loop: plantar → crescer → **incorporar com a enxada antes da floração/colheita** (mecânica US-09-02: enxada num trevo maduro = "incorporate": some a planta, aplica `+GreenManureHealth` no tile, registra família no histórico). **Colher em vez de incorporar = feno/fibra pequeno, sem boost** (trade-off explícito). P1 opcional: se ignorado após a janela, vira erva (vira weed).

## 2. Sprite

| Asset | Spec |
|---|---|
| `Assets/crops/green_manure.png` — tilesheet custom `LivingRoots/GreenManureCrops`, **2 linhas** (row 0 = clover, row 1 = oats), 5–6 estágios por linha, cada estágio 16×16 (padrão `TileSheets\crops`: 1 crop por linha, N fases) | Estágio final do trevo com pontinhos brancos (flor) = sinal visual de "incorpore AGORA"; aveia dourada alta. Sprites dedicados (não `TintColors`), paleta `Healthy`/`Sun` |
| Ícones de semente 16×16 | `Assets/seeds/clover_seeds.png`, `oats_seeds.png` OU `Texture` custom + `SpriteIndex` em `Data/Objects` (`Texture: "LivingRoots/Seeds"`) |
| Item ceifado (fibra/feno) | reutilizar fibra vanilla (`771`) — sem asset novo |

## 3. Data

```csharp
// Data/Objects — sementes (Type Seeds, Category -74 = aparecem como sementes)
data["LivingRoots.CloverSeeds"] = new ObjectData {
  Name = "Clover Seeds", DisplayName = "{{i18n:green-manure.clover-seeds.name}}",
  Description = "{{i18n:green-manure.clover-seeds.description}}",
  Type = "Seeds", Category = -74, Price = 30,
  Texture = "LivingRoots/Seeds", SpriteIndex = 0,
  ContextTags = new() { "seed_item" }, // família vai no Crop, não no Object
};
// Data/Crops — chaves = IDs das SEMENTES acima (wiki Modding:Crop_data)
crops["LivingRoots.CloverSeeds"] = new CropData {
  Seasons = ["spring","summer","fall"], DaysInPhase = [1,1,1,2], RegrowDays = -1,
  HarvestItemId = "(O)771", HarvestMinStack = 1, HarvestMaxStack = 2, // fibra se ceifado
  HarvestMethod = "Scythe", Texture = "LivingRoots/GreenManureCrops", SpriteIndex = 0,
  CountForMonoculture = false, CountForPolyculture = false, // cobertura não conta p/ conquistas
  CustomFields = new() { ["LivingRoots.Family"] = "Legume", ["LivingRoots.CoverCrop"] = "true" }
};
// Aveia: SpriteIndex = 1, DaysInPhase = [1,2,2,3], Family = "Grain", boost menor
// Data/CraftingRecipes — fallback até Elena existir:
recipes["LivingRoots.Clover Seeds"] = "771 5/Home/LivingRoots.CloverSeeds 4/false/s Farming 2/";
```

- Incorporação: **não é `Data/Machines`** — é interação `ButtonsPressed` + `HoeDirt`/`Crop` no tile (novo `IGreenManureService.TryIncorporate(tile)` chamado do `ModController`, mesmo ponto onde `TryApplyCompost` vive — `ModController.cs:845`). Regra: `crop != null && crop.modData["LivingRoots.CoverCrop"] && mature && tool == Hoe → destroy crop + soilHealth += CloverIncorporateHealth (+25) / OatsIncorporateHealth (+15)`, clamp 0–100.
- Referência externa — `Modding:Recipe_data` (oldid 193377, verificado 2026-10-05): crafting string `"<ingredientes>/<aba>/<itemId> <qtd>/<bigCraftable?>/<unlock>/<nome>"`; unlocks `default`, `s <Skill> <nível>`, `f <NPC> <corações>`, resto = evento/quest. Ex. Bee House `"388 40 382 8 335 1 724 1/Home/10/true/s Farming 3/"`.
- Constantes: `CloverSeedPrice`, `OatsSeedPrice`, `CloverIncorporateHealth = 25f`, `OatsIncorporateHealth = 15f`, `CoverCropWindowDays`.

## 4. UI

- Tooltip no tile com cobertura: `"Clover (Legume cover) — hoe to incorporate +25"`. Hoe feedback reutilizado (`"+25"` flutuante — mesma chave `floating-text` com `{0}`).
- Overlay: tom `Healthy` translúcido + pattern `Solid` quando cobertura viva ("solo vivo" literal).
- Carta/tutorial da Elena (`"green-manure.elena-letter"`) explicando incorporar vs. colher.
- GMCM: `GreenManureEnabled`, `CoverCropWindowDays` (slider), `ShowCoverOverlay`.

## 5. i18n

```json
{
  "green-manure.clover-seeds.name": "Clover Seeds",
  "green-manure.clover-seeds.description": "Fast cover crop. Hoe it under before bloom for +25 soil health.",
  "green-manure.oats-seeds.name": "Oats Seeds",
  "green-manure.oats-seeds.description": "Biomass cover crop. Hoe it under for +15 soil health and fewer weeds.",
  "green-manure.incorporated": "Incorporated! +{0} soil health.",
  "green-manure.too-late": "It went to seed… chop it for fiber instead.",
  "green-manure.elena-letter": "Bare soil bleeds. Sow clover, then hoe it under before bloom. — Elena",
  "message.green-manure.not-cover": "Only cover crops can be incorporated."
}
```

`DisplayName/Description` tokenizáveis desde o dia 1 (lição do compost v0, que hardcodou literais em `OnAssetRequested`).

## 6. Gates

- [ ] Tabela de IDs vanilla usados (fibra `771`, `HarvestItemId` fibra/feno) com fonte wiki + `oldid`.
- [ ] Constantes em `ModConstants` (nome + default + unidade); justificar `+25` vs compost `+15` (ocupa o tile ~5 dias + trabalho + janela) ou marcar `TBD-balance`.
- [ ] `OnAssetRequested` edits + teste chave == constante; IDs `LivingRoots.<Nome>`, `QualifiedItemId` nas duas formas.
- [ ] Serviço `IGreenManureService`: interface em `Domain/Interfaces/`, impl em `Services/`, wiring em `ModEntry.cs`, chamada em `ModController` com `ExecuteWithConcurrencyGuard`.
- [ ] Escrever no histórico de rotação (`rotation-journal.md`): família Legume/Grain registrada na incorporação; teste de integração entre os dois serviços.
- [ ] Coexistência foice × enxada: foice = colhe fibra; enxada = incorpora. Prioridade por ferramenta segurada, especificada + testada.
- [ ] `PlantableLocationRules`: negar `GardenPot` com `DeniedMessage` i18n (cobertura em vaso não faz sentido).
- [ ] Multiplayer host-autoritativo; `PerScreen` onde houver feedback.
- [ ] Verificação: `dotnet test --filter` + in-game (plantar → incorporar → solo +25; colher → fibra sem boost).

## 7. Riscos

1. Balanço: +25 é enorme vs compost +15 — a spec fixa números ou marca `TBD-balance` com playtest.
2. Venda via Elena exige NPC (Epic 5) — fallback de receita deve ser decidido, não acumulado.
3. Janela "incorporar vs. virou erva" (P1) adiciona estado temporal — YAGNI se a spec quiser cortar.

## Fontes

- Internas: `ROADMAP.md:47`; `EPICS_USER_STORIES.md:59-63`; `LivingRoots/Constants.cs:57-58`; `LivingRoots/ModEntry.cs:114-177`.
- Externas: `Modding:Crop_data` (186411) — `HarvestMethod: Scythe`, `CustomFields`, `PlantableLocationRules`+`DeniedMessage`; `Modding:Recipe_data` (193377) — formato crafting + unlocks `s`/`f`; `Modding:Objects` (193966) — `Type: Seeds`, `Category -74`.
