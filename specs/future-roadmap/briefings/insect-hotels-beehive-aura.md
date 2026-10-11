# Briefing — Insetos Benéficos: Aura da Colmeia + Insect Hotels (Epic 11 · US-11-01 / US-11-02)

> **Escopo:** ROADMAP `ROADMAP.md:49` ("beehives second function: beneficial-insect aura + Insect Hotels craftable for natural pest control, alternative to pesticides") + Epic 11 (Issue #13): US-11-01 estender colmeia (#45), US-11-02 hotel craftável (#46) em `LivingRoots/docs/EPICS_USER_STORIES.md:72-76`.
> **Status:** briefing (não implementado). Insumo para futuro `/speckit.specify` — não é especificação.
> **Agrupado de origem:** `specs/future-roadmap/briefings/mid-game-year2-3.md` Cap. 4. Versão individual canônica.
> **Leitura interna:** padrão `Data/Machines` dominado (CompostingBin com `OutputRules`/`MachineOutputRule`/`MachineItemOutput` em `ModEntry.cs:152-176`), overlay com culling + cache (`Services/Visualization/`, orçamento <4 ms), `Constants.cs` (paleta `Sun #FACC15`, `Healthy #15803D`).

---

## 1. Conceito de jogo

- **Aura (US-11-01):** cada Bee House (vanilla `(BC)10` + futuras do mod) projeta aura de **raio 5 cardeais** — a mesma geometria do mel, que o jogador já conhece — que suprime pragas/doenças nos cultivos do raio e dá `+AuraQualityChance` na colheita. Sem flor no raio = aura fraca (50%); com flor = plena (sinergia flor→mel→aura, um só sistema mental). Inverno/indoor = sem aura (igual ao mel — consistência gratuita).
- **Insect Hotel (US-11-02):** BigCraftable de aura menor (**raio 3**) o ano todo (inclusive inverno sob cobertura? **decisão p/ spec:** sim, metade do efeito — hotéis são abrigo, colmeias são atividade). Recarrega com `Fiber + Wood`. Stack: auras sobrepostas **não somam** (max, não stack — evita hotel-spam; documentar).
- **Dependência explícita:** hoje **não existe sistema de pragas** no mod (só `DailyDecayRate` + `SeasonalDecayMultiplier`). A spec deve definir o **MVP de praga** (ex.: chance diária de "aphids" em solo pobre, suprimida na aura) OU declarar a aura como bônus de qualidade/saúde até pragas existirem. **Não especificar aura sem especificar o que ela suprime** (recomendado: MVP de praga junto).

Referência externa — `Bee_House` (oldid 191201, verificado 2026-10-05): mel base 100g; flor madura ≤**5 tiles cardeais** (diamante, **60 tiles úteis** excluindo o tile da colmeia) vira mel floral (Tulip 160 … Fairy Rose 680); prioridade = flor mais próxima, desempate esquerda-horizontal depois horário; checagem no `DayUpdate`/`RecalculateOnCollect`; forrageáveis (Crocus, Sweet Pea) e Dandelion/Daffodil **não** contam; **não produz indoor nem no inverno** (`PreventTimePass: Inside, Winter`); fora da fazenda = selvagem + risco de destruição em path de NPC. Espantalho: raio 8 (17 Deluxe) via context tag `crow_scare_radius_<n>` (`Object.GetRadiusForScarecrow()`).

## 2. Sprite

| Asset | Spec |
|---|---|
| `Assets/machines/insect_hotel.png` — BigCraftable 16×32 (2 tiles verticais, padrão `TileSheets/Craftables`), caixa de madeira com furos/bambu/pinhas, 2 frames (`ShowNextIndexWhileWorking` = com abelhas/joaninhas visíveis) | `Texture: "LivingRoots/InsectHotel"`, `SpriteIndex: 0`. Fragility 0, indoor+outdoor true, `IsLamp: false` (P2: `LightWhileWorking` âmbar sutil) |
| Partículas de aura | **sem textura nova**: reutilizar `VisualizationTextures` (pontinhos brancos tingidos `Sun`/`Healthy`, abordagem B do relatório XNA) flutuando no raio (respeitar `ReduceMotion` — estático se ativo) |
| Anel de raio | mesmo `IRangeOverlay` de `mobile-coop.md` (diamante 5 p/ colmeia, quadrado 3 p/ hotel) |

## 3. Data

```csharp
// Data/BigCraftables (wiki Modding:Big_craftables, oldid 190701)
data["LivingRoots.InsectHotel"] = new BigCraftableData {
  Name = "Insect Hotel", DisplayName = "{{i18n:insect-hotel.name}}", Description = "{{i18n:insect-hotel.description}}",
  Price = 0, Fragility = 0, CanBePlacedIndoors = true, CanBePlacedOutdoors = true,
  Texture = "LivingRoots/InsectHotel", SpriteIndex = 0,
  ContextTags = new() { "LivingRoots.beneficial_aura_3" } // raio legível por C# sem hardcode reverso
};
// Data/CraftingRecipes (wiki Modding:Recipe_data, oldid 193377)
recipes["LivingRoots.Insect Hotel"] = "388 40 771 30 709 10/Home/LivingRoots.InsectHotel 1/true/s Farming 3/";
// Colmeia: NÃO redefinir (BC)10 — estender por C#: detectar bee houses na location
// (parentSheetIndex 10 / ID (BC)10 + GetMachineData()) e aplicar aura via IBeneficialInsectService.
// Data/Machines p/ o hotel (abrigo passivo, sem output):
machines["(BC)LivingRoots.InsectHotel"] = new MachineData {
  OutputRules = [], // sem output — só aura; InteractMethod opcional p/ "refill"
  PreventTimePass = [], // funciona o ano todo (decisão §1)
  WobbleWhileWorking = false, ShowNextIndexWhileWorking = true
};
```

- Serviço `IBeneficialInsectService`: `DayStarted` computa cobertura de aura por tile (colmeias r5 + hotéis r3, max-não-stack, indoor/inverno filtrados); expõe `GetPestSuppression(tile)` + `GetQualityBonus(tile)`; `RenderedWorld` desenha anéis só dos emissores visíveis (culling existente).
- Referência externa — `Modding:Machines` (oldid 191352): chave = ID **qualificado** (`(BC)…`); `OutputRules[]` (`Triggers`: `ItemPlacedInMachine/OutputCollected/MachinePutDown/DayUpdate`; `RequiredItemId/RequiredTags/Condition` GSQ; tokens `DROP_IN_ID`, `DROP_IN_PRESERVE`, `NEARBY_FLOWER_ID`; `RecalculateOnCollect` padrão da colmeia; `OutputMethod` C#); `PreventTimePass` (`Outside/Inside/Spring…/Sun/Rain`), `LightWhileWorking/WobbleWhileWorking/ShowNextIndexWhileWorking/WhenReady`, `InvalidItemMessage`, `ExperienceGainOnHarvest`.
- Constantes: `BeehiveAuraRadius = 5`, `InsectHotelRadius = 3`, `AuraWeakFactor = 0.5f`, `AuraQualityBonus = 0.08f`, `AuraPestSuppression = 0.75f`, `AuraStacking = Max`.

## 4. UI

- Anel de aura ao passar cursor sobre colmeia/hotel (hover = mostra raio; fora do hover = só partículas sutis — evita poluição; toggle GMCM `ShowAuraRings: hover|always|never`).
- Tooltip em cultivo protegido: `"Protected by beneficials (Bee House)"` + ícone 🐞 (texto, nunca só cor).
- GMCM: `BeneficialsEnabled`, `ShowAuraRings`, `AuraParticles` (default true, desligado por `ReduceMotion` automaticamente).
- Tutorial Elena: `"insects.elena-tip"` — "flores alimentam as abelhas que guardam a horta".
- `IClickableMenu` custom: **não** (só o Journal justifica).

## 5. i18n

```json
{
  "insect-hotel.name": "Insect Hotel",
  "insect-hotel.description": "Shelter for ladybugs and lacewings. Pest control in a {0}-tile radius.",
  "beehive.aura.name": "Beneficial aura",
  "beehive.aura.protected": "Protected by beneficials ({0}).",
  "beehive.aura.weak": "Weak aura — plant flowers nearby.",
  "insects.refill": "Refilled with fiber. The hotel buzzes again.",
  "insects.elena-tip": "Flowers feed the bees that guard your garden. — Elena",
  "message.insects.no-stack": "Auras don't stack — spread them out."
}
```

## 6. Gates

- [ ] Tabela de IDs vanilla (Bee House `(BC)10`, fibra `771`, madeira `388`, hardwood `709`) com fonte + `oldid`.
- [ ] Constantes em `ModConstants`; regra de stacking (max) documentada + testada (dois hotéis sobrepostos = 1× efeito).
- [ ] Detecção robusta da colmeia: `parentSheetIndex`/ID `(BC)10` + `GetMachineData()` — **nunca `Name.Contains`** (localização quebra).
- [ ] MVP de praga definido (ou adiamento explícito de US-11-01 para depois dele).
- [ ] Custo O(emissores × 60 tiles) no `DayStarted`, cacheado; nunca por frame; orçamento de render <4 ms mantido.
- [ ] `OnAssetRequested` edits + teste chave == constante; IDs `LivingRoots.<Nome>`.
- [ ] Multiplayer host-autoritativo; overlay `PerScreen`.
- [ ] Verificação: `dotnet test --filter` + in-game (flor no raio → aura plena; inverno/indoor → sem aura; tooltip "Protected").

## 7. Riscos

1. Sem pragas, a aura é "bônus invisível" — a spec inclui o MVP ou adia.
2. `NEARBY_FLOWER_ID` é lógica do mel; a aura lê flores por conta própria (varredura de `Object` flor madura no diamante, excluindo Crocus/Sweet Pea/Dandelion/Daffodil — mesma lista da wiki).
3. Hotel-spam sem regra anti-stack quebra o balanceamento — max-não-stack desde o dia 1.

## Fontes

- Internas: `ROADMAP.md:49`; `EPICS_USER_STORIES.md:72-76`; `LivingRoots/ModEntry.cs:152-176`; `Services/Visualization/`; `Constants.cs`.
- Externas: `Bee_House` (oldid 191201) — raio 5 cardeais/60 tiles, prioridade, indoor/inverno; `Modding:Machines` (191352); `Modding:Big_craftables` (190701); `Modding:Recipe_data` (193377).
