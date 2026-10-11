# Briefing — Mobile Coop / Manejo Animal Integrado (Epic 10 · US-10-01 / US-10-02 / US-10-03)

> **Escopo:** ROADMAP `ROADMAP.md:48` ("animals outside unfenced eat weeds/insects + manure: mulched improves / bare compacts + Mobile Coop for rotational grazing") + Epic 10 (Issue #12): US-10-01 pasto/pragas (#42), US-10-02 esterco (#43), US-10-03 Mobile Coop (#44) em `LivingRoots/docs/EPICS_USER_STORIES.md:65-70`.
> **Status:** briefing (não implementado). Insumo para futuro `/speckit.specify` — não é especificação.
> **Agrupado de origem:** `specs/future-roadmap/briefings/mid-game-year2-3.md` Cap. 3. Versão individual canônica.
> **Leitura interna:** `Services/` providers existentes (`LocationProvider`, `PlayerProvider`, `TimeProvider`, `SeasonProvider` — seams testáveis), `ModController` (`ExecuteWithConcurrencyGuard`), `ModDataService` + chaves `*_KeyPrefix`, cache esparso, `Constants.cs` (`MulchColor #92400E`).

---

## 1. Conceito de jogo

- **US-10-01 (pasto):** cada animal externo/dia consome N weeds num raio R (remove `Object` erva) e suprime evento de praga no raio (hook no futuro sistema de pragas; v1 = +felicidade animal + −ervas). **Só sem cerca** = comportamento ativo; com cerca = vanilla (nunca quebrar fazendas existentes).
- **US-10-02 (esterco):** cada animal externo/dia deixa 0–1 `manure tile` (estado volátil em `ModData`, expira em 2 dias se ignorado). Com enxada/manejo: tile com mulch → `+ManureMulchedHealth` (+6); solo nu → `−ManureBarePenalty` (−4 compactação) + mensagem. Com compost/mulch antes do vencimento = bônus; sem manejo = penalidade. É o "mulch matters" do mod em forma jogável.
- **US-10-03 (Mobile Coop):** building `LivingRoots.MobileCoop` — galinheiro pequeno móvel (2–4 galinhas, sem incubadora), reposicionável barato (custo de movimento, não de construção), que define o **centro do raio de pastejo**. Mover = rotacionar o pasto (sinergia com `rotation-journal.md` + `green-manure.md`: pasto recém-saído recebe trevo).

## 2. Sprite

| Asset | Spec |
|---|---|
| Exterior Mobile Coop | `Assets/buildings/mobile_coop.png` — 4×3 tiles (pequeno vs. Coop vanilla 6×4), madeira + lona listrada, **rodas visíveis** (leitura "móvel"), 1 textura P1 (sem `SeasonOffset` — YAGNI) + `DrawShadow: true`, `FadeWhenBehind: true` |
| Interior | reutilizar mapa `Coop` vanilla (`IndoorMap: "Coop"`, `IndoorMapType: StardewValley.AnimalHouse`, `MaxOccupants: 4`) — **zero asset de interior no v1** |
| Esterco (tile) | `Assets/objects/manure.png` 16×16 (monte marrom, 2 variantes via `ColorOverlayFromNextIndex` ou 2 `SpriteIndex`) |
| Pegadas/pasto | reutilizar overlay de raio (`insect-hotels-beehive-aura.md`, `IRangeOverlay` compartilhado) — sem asset novo |

Textura sob `Content` via asset custom (`Texture: "LivingRoots/MobileCoop"`), `SourceRect` exato, `CollisionMap` (`"XXXX\nXOOX"`-style com porta), `HumanDoor` + `AnimalDoor` (abrir de manhã = animais saem; fechar = recolhem — acoplar ao raio: porta fechada = sem esterco/pasto).

## 3. Data

```csharp
// Data/Buildings (wiki Modding:Buildings, oldid 191928 — verificado 2026-10-05)
data["LivingRoots.MobileCoop"] = new BuildingData {
  Name = "{{i18n:coop.name}}", Description = "{{i18n:coop.description}}",
  Texture = "LivingRoots/MobileCoop", Builder = "Robin",
  BuildCost = 8000, BuildMaterials = [ { ItemId = "(O)388", Amount = 150 }, { ItemId = "(O)390", Amount = 50 }, { ItemId = "(O)771", Amount = 30 } ],
  BuildDays = 1, Size = new(4, 3), CollisionMap = "XXXX\nXXXX\nXOOX",
  HumanDoor = new(1, 2), AnimalDoor = new …,
  IndoorMap = "Coop", IndoorMapType = "StardewValley.AnimalHouse", MaxOccupants = 4,
  HayCapacity = 0, // sem silo próprio — depende de silo existente (trade-off vs. Coop vanilla)
  MagicalConstruction = false
};
// mover = custo de movimento (ex. 500g, instantâneo, via Robin ou menu do building por ActionTiles) —
// NÃO demolir+reconstruir com desconto; especificar MoveCost na spec.
```

- Pastagem/esterco: serviço `IGrazingService` (`DayStarted`: para cada `FarmAnimal` externo, rolar consumo de weeds + chance de esterco; `ButtonsPressed`: aplicar mulch/interagir). Persistência: `grazing_manure_<saveId>` esparso com expiração (`DayStarted` purga vencidos).
- Referência externa — `Modding:Buildings`: `Builder` (`Robin`/`Wizard`/null), `BuildCost` + `BuildMaterials[]` (`ItemId` qualificado + `Amount`), `BuildDays`, `BuildCondition` (GSQ), `Size`, `CollisionMap` (`X/O`), `HumanDoor`/`AnimalDoor` (+ sons/durações), `SourceRect`/`SeasonOffset`/`Skins[]`/`DrawLayers[]`, `IndoorMap`/`IndoorMapType` (**usar só tipos vanilla** — `StardewValley.AnimalHouse`, `Shed`, `DecoratableLocation`…), `BuildingToUpgrade`, `HayCapacity`/`ItemConversions`/`Chests`, `ActionTiles`/`DefaultAction`/`TileProperties`, `Metadata`/`ModData`. **Aviso da wiki: NÃO criar `BuildingType` custom** (crash de save/multiplayer; usar `Building` genérico + `GetMetadata()`/`ModData`).
- Constantes: `GrazingRadius = 6`, `WeedsEatenPerAnimal = 3`, `ManureChance = 0.5f`, `ManureMulchedHealth = +6f`, `ManureBarePenalty = −4f`, `ManureExpiryDays = 2`, `MobileCoopCapacity = 4`, `MobileCoopMoveCost = 500`.

## 4. UI

- **Anel de pastejo:** overlay de raio (mesmo `IRangeOverlay` de `insect-hotels-beehive-aura.md`: círculo/diamante tiled, cor `Sun #FACC15` 0.35, `ReduceMotion`-safe, toggle GMCM). Padrão comprovado: mod Range Highlight.
- **Tile com esterco:** tooltip `"Fresh manure — mulch it (+6) or it compacts (−4 in 2d)"` + mini-ícone; hoe feedback adaptado.
- **Menu do Coop:** `ActionTiles` na porta → diálogo estilo Robin "Move coop (500g)?" + seletor de local (reutiliza menu de construção vanilla se possível; custom só se inevitável).
- GMCM: `GrazingEnabled`, `ManureExpiryDays`, `ShowGrazingRadius`. `IClickableMenu` custom: **não** (só o Journal justifica menu custom).

## 5. i18n

```json
{
  "coop.name": "Mobile Coop",
  "coop.description": "A small coop on wheels. Move it to rotate pasture.",
  "coop.move.prompt": "Move the coop here for {0}g?",
  "coop.move.done": "Coop moved. Fresh pasture!",
  "manure.fresh.name": "Fresh Manure",
  "manure.mulched": "Mulched! +{0} soil health.",
  "manure.compacted": "Trampled bare… {0} soil health.",
  "manure.expired": "The manure dried out.",
  "grazing.weeds-eaten": "The hens cleared weeds and bugs."
}
```

Nomes vanilla em `Data/Buildings` usam `Strings/Buildings` tokenizável — apontar para i18n do mod desde o dia 1.

## 6. Gates

- [ ] Tabela de IDs vanilla (Wood `388`, Stone `390`, Fiber `771`, mapa `Coop`, classe `StardewValley.AnimalHouse`, `FarmAnimal` occupant types) com fonte + `oldid`.
- [ ] Constantes em `ModConstants` (nome + default + unidade).
- [ ] `OnAssetRequested` edits + teste chave == constante; `Building` genérico (sem `BuildingType` custom); `ValidOccupantTypes: ["Coop"]`; `AllowAnimalPregnancy: false` (v1).
- [ ] `IGrazingService`: `Domain/Interfaces/` → `Services/` → `ModEntry.cs`; `DayStarted` para varredura (nunca no `RenderedWorld`); `ButtonsPressed` para interação com mulch.
- [ ] Persistência esparsa `grazing_manure_<saveId>` + expiração + purga; limites `MaxTiles*`.
- [ ] Esterco como **tile-state, não item** no v1 (se virar item coletável, vira fertilizer loop — YAGNI explícito).
- [ ] Multiplayer host-autoritativo (writes do host; `Context.IsMainPlayer` guards; mutação diária).
- [ ] Verificação: `dotnet test --filter` + in-game (animal externo → weeds somem; esterco + mulch = +6; esterco nu expirado = −4).

## 7. Riscos

1. `BuildingType` custom crasha save/multiplayer — regra inegociável da wiki.
2. Varredura diária O(animais × weeds) deve rodar em `DayStarted`, cacheada, nunca por frame.
3. Porta fechada × raio de pasto: acoplar ou documentar (evita "meu coop não produz esterco" como bug fantasma).

## Fontes

- Internas: `ROADMAP.md:48`; `EPICS_USER_STORIES.md:65-70`; providers em `Services/`; `ModController`; `ModDataService`.
- Externas: `Modding:Buildings` (oldid 191928) — `IndoorMapType` vanilla-only, `BuildingType` warning, `ActionTiles`, `Metadata`/`ModData`; Range Highlight (padrão de overlay de raio).
