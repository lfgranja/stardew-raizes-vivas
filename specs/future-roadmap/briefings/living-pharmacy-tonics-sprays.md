# Briefing — Living Pharmacy: Ervas, Tônicos e Sprays (Epic 12 · US-12-01 / US-12-02)

> **Escopo:** ROADMAP `ROADMAP.md:50` ("learn from Elena/Linus: Rue, Boldo, Chamomile → tonics/energy buffs + Natural Sprays pest control without soil damage") + Epic 12 (Issue #14): US-12-01 cultivo (#47), US-12-02 receitas (#48) em `LivingRoots/docs/EPICS_USER_STORIES.md:78-82`.
> **Status:** briefing (não implementado). Insumo para futuro `/speckit.specify` — não é especificação.
> **Agrupado de origem:** `specs/future-roadmap/briefings/mid-game-year2-3.md` Cap. 5. Versão individual canônica.
> **Leitura interna:** `Domain/Services/OrganicWasteValidator.cs` (aceita `-74/-75/-79/-80/-81`, rejeita compost `-2` — anti-loop em `specs/003/.../compost-item.md §2.3`), `ObjectData` com `Edibility`/`Buffs` nunca usado até aqui, `ModController.cs:845` (ponto do `TryApplyCompost` — spray entra no mesmo gesto), `Constants.cs` (paleta `Water #2563EB`).

---

## 1. Conceito de jogo

- **US-12-01 (cultivo):** 3 ervas, 3 perfis — **Chamomile** (calmante — energia/regen; fácil, todas as estações menos inverno); **Rue/Arruda** (amarga — base dos sprays; verão/outono); **Boldo** (digestivo — tônico forte; primavera/verão). Colheita `Grab`, sem `Regrow` (replantar = rotaciona — sinergia com `rotation-journal.md`; família `Herb` → neutra/`Unknown` na rotação, a spec decide e documenta).
- **US-12-02 (receitas):** 2 tônicos (`Data/CookingRecipes`, `IsDrink`, `Buffs`: energia + `MaxStamina/Speed/MagneticRadius` leve) + 2 sprays (`Data/Objects`, `Type: Basic`, clique no cultivo = suprime praga 3 dias **sem tocar na saúde do solo** — o anti-pesticida; contraste explícito com o futuro "Joja Fertilizer −5%" do ROADMAP).
- Desbloqueio: corações com Elena/Linus (`f Elena 4` / `f Linus 6` — padrão `Recipe_data`) + carta; fallback `s Foraging 5` se NPC indisponível no v1 (**especificar um, não os dois**).

## 2. Sprite

| Asset | Spec |
|---|---|
| Tilesheet `Assets/crops/pharmacy.png` — 3 linhas × 4–5 estágios (camomila com florzinhas brancas/amarelas; arruda verde-azulada; boldo folha larga) | `Texture: "LivingRoots/PharmacyCrops"`, `SpriteIndex` 0/1/2; 16×16 por estágio; sementes 16×16 cada (`Assets/seeds/rue_seeds.png` etc. ou tilesheet única de sementes) |
| Frascos 16×16 | `Assets/objects/tonic_chamomile.png` (xícara fumegante), `tonic_boldo.png` (garrafa verde), `spray_rue.png` (borrifador) + 4º produto da spec (2+2) — estilo `springobjects`: objeto centralizado, outline 1px, paleta terrosa + `Water #2563EB` p/ spray |
| Partícula de aplicação | reutilizar ponto branco tingido `Water`/`Healthy` — sem asset novo |

## 3. Data

```csharp
// Data/Objects — ervas colhidas + produtos (wiki Modding:Objects, oldid 193966)
data["LivingRoots.Chamomile"] = new ObjectData { … Type = "Basic", Category = -81, Edibility = 10, Price = 40, … };
// ATENÇÃO Category × OrganicWasteValidator: -74/-75/-79/-80/-81 SÃO compostáveis.
// Regra a travar na spec: matéria-prima (erva) Category -81 = compostável (resto); PRODUTO
// (tônico/spray) Category -7 (Cooking) ou -26 = NÃO compostável. Tabela explícita + teste anti-loop.
// Tônico: Edibility alta + Buffs (Duration em minutos in-game; -2 = resto do dia)
data["LivingRoots.ChamomileTea"] = new ObjectData { … Type = "Cooking", Category = -7, Edibility = 60, IsDrink = true,
  Buffs = [ { Id = "LivingRoots.calm", Duration = 180, CustomAttributes = { MaxStamina = 40, Speed = 0 } } ],
  Price = 120, … };
// Spray: NÃO comestível, aplica em cultivo
data["LivingRoots.RueSpray"] = new ObjectData { … Type = "Basic", Category = -26, Edibility = -300, Price = 80,
  ContextTags = new() { "LivingRoots.natural_spray" }, … };
// Data/Crops — 3 entradas (Seeds; HarvestMethod Grab; CustomFields Family=Herb; Seasons por erva)
// Data/CookingRecipes — tônicos (formato Cooking "ing/par/out/unlock/"):
recipes["LivingRoots.Chamomile Tea"] = "LivingRoots.Chamomile 3 423 1/…/…/f Elena 4/";
// Sprays são CRAFTING (não vão à panela):
craft["LivingRoots.Rue Spray"] = "LivingRoots.Rue 2 388 5 724 1/Home/LivingRoots.RueSpray 2/false/f Linus 6/";
```

- Referência externa — `Modding:Objects`: `Edibility` (−300 = não-comestível; `×2.5` energia, `×1.125` vida), `IsDrink`, `Buffs[]` (`Id/Duration` ou `BuffId` de `Data/Buffs`; `CustomAttributes`: `FarmingLevel/LuckLevel/Speed/MaxStamina/MagneticRadius/Attack/Defense…`; `Duration: -2` = resto do dia). `Type: Cooking` + `Category: -7` conta p/ "Gourmet Chef"/Perfection — decidir se tônicos contam (recomendado: sim, −7).
- Referência externa — `Modding:Recipe_data` (oldid 193377): Cooking `"ingredientes/par1 par2/yield/unlock/"` (ex. Farmer's Lunch `"195 1 24 1/60 1/240/s Farming 3/"`); unlocks `f <NPC> <corações>` (carta `<NPC>Cooking` no dia seguinte), `s <Skill> <nível>`, `default`, resto = evento/quest.
- Aplicação do spray: `IPharmacyService.TryApplySpray(tile)` no mesmo ponto do compost (`ModController` botão-direito; checar `QualifiedItemId` contra **ambas** as formas `(O)LivingRoots.RueSpray` — regra dos dois IDs do `AGENTS.md` raiz). Efeito: `spray_protection_<saveId>` esparso `{tile: expiryDay}` (3 dias), `DayStarted` purga; **não altera `soil_health`** (assert de regressão: solo antes == depois).
- Constantes: `SprayProtectionDays = 3`, chaves `ModDataSprayKey`, preços/estações por erva.

## 4. UI

- Erva madura: tooltip `"Chamomile (medicinal) — harvest"`; cultivo protegido: selo `"Sprayed (2d left)"` + gota `Water`-tingida (cor+texto, nunca só cor).
- Tônico: buff vanilla (`Data/Buffs` ou `CustomAttributes`) = ícone automático no relógio — sem UI custom.
- Telhado verde futuro (`adobe-shed-green-roof.md`): plantadores do telhado aceitam **só** estas ervas (allow-list `LivingRoots.*` herb seeds) — dependência registrada, não implementada aqui.
- GMCM: `PharmacyEnabled`, `SprayDays` (1–5, default 3), `ShowSpraySeals`. `IClickableMenu` custom: **não**.

## 5. i18n

```json
{
  "herb.chamomile.name": "Chamomile",
  "herb.chamomile.description": "Soothing flowers. Brews a calming tea.",
  "herb.rue.name": "Rue",
  "herb.rue.description": "Bitter leaves. Base for natural pest spray.",
  "herb.boldo.name": "Boldo",
  "herb.boldo.description": "Digestive leaves. Brews a hearty tonic.",
  "tonic.chamomile-tea.name": "Chamomile Tea",
  "tonic.chamomile-tea.description": "Warm and calming. Restores energy.",
  "spray.rue-spray.name": "Rue Spray",
  "spray.rue-spray.description": "Keeps pests off crops for 3 days. Harmless to the soil.",
  "spray.applied": "Sprayed. Protected for {0} days — soil untouched.",
  "message.spray.not-crop": "Spray only works on growing crops."
}
```

Nomes BR (Boldo) vs. EN: chave i18n única, `default.json` = EN, `pt.json` futuro — **não hardcodar "Boldo" em `DisplayName`** (tokenizável desde o dia 1, ao contrário do compost v0).

## 6. Gates

- [ ] Tabela Category explícita (matéria-prima compostável × produto não) + teste anti-loop (mesma família do teste do compost §2.3).
- [ ] Tabela de IDs vanilla (ingredientes das receitas, `423` açúcar etc. confirmados) com fonte + `oldid`.
- [ ] `Buffs`: `Duration`/`CustomAttributes` balanceados (`-2` resto-do-dia é forte — balancear `Edibility`/preço ou `TBD-balance`).
- [ ] Constantes em `ModConstants`; `OnAssetRequested` edits + teste chave == constante; dois IDs qualificados.
- [ ] `IPharmacyService`: `Domain/Interfaces/` → `Services/` → `ModEntry.cs`; `DayStarted` purga expirações; regressão solo-intocado.
- [ ] Família `Herb` no `CropFamilyRegistry` (`rotation-journal.md`) — neutra, documentada.
- [ ] Multiplayer host-autoritativo; `AvailableStockLimit: Player` se houver loja.
- [ ] Verificação: `dotnet test --filter` + in-game (spray → 3 dias protegido, solo igual; tônico → buff no relógio).

## 7. Riscos

1. `Category` errada transforma tônico em composto (loop econômico) ou erva em lixo não-compostável (quebra o ciclo) — a tabela é o artefato mais barato e o erro mais caro.
2. Sprays dependem do MVP de pragas (`insect-hotels-beehive-aura.md` §1): sem praga, spray é "selo invisível" — coordenar as specs.
3. `Duration: -2` + `MaxStamina` alto = power creep de energia — playtest ou nerf.

## Fontes

- Internas: `ROADMAP.md:50`; `EPICS_USER_STORIES.md:78-82`; `OrganicWasteValidator`; `ModController.cs:845`; `Constants.cs`.
- Externas: `Modding:Objects` (193966) — `Edibility`/`IsDrink`/`Buffs`/`CustomAttributes`; `Modding:Recipe_data` (193377) — Cooking vs Crafting + unlocks `f`/`s`; `Modding:Crop_data` (186411) — `HarvestMethod: Grab`, `CustomFields`.
