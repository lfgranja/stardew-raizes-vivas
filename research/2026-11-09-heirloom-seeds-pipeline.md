# Heirloom Seeds / Sementes Crioulas Pipeline — Research (2026-11-09)

**Scope**: Deep, current (Nov 2026) web research, in EN/PT/ES/FR/DE + primary-source verification of the SDV/SMAPI technical surface, to inform the `heirloom-seeds-pipeline` assessment (`.specify/assessments/heirloom-seeds-pipeline/`). Flow adapted from the MDMV Dark Software Factory (`/home/luis/development/MDMV/projetos/mdmv-dark-factory`).

**Honesty**: `machine-pass` ≠ `verified`. External facts cited with URL + date + confidence. Local facts verified from files/commands this session (2026-11-09). Nexus direct fetches are largely HTTP 403/Cloudflare-blocked — Nexus figures come from search-engine snippets and are labeled; missing data is reported as missing, never invented.

**Method**: 3 parallel background research streams (technical SDV/SMAPI surface; competitive/market landscape incl. Brazilian communities; agronomic grounding, multilingual EN/PT/ES/DE) — all complete 2026-11-09; §7 carries the consolidated agronomic report. Primary verification done directly by the parent session (decompiled `Stardew Valley.dll` / `StardewValley.GameData.dll` @ `/home/luis/stardew-assemblies`, smapi.io live).

---

## 1) Version & toolchain status (verified live, 2026-11-09)

| Topic | Finding | Source | Confidence |
|---|---|---|---|
| SDV PC stable | **1.6.15** (2024-12-20). **1.6.16** is a *potential* pre-1.7 patch — "no planned release date"; mod-author prep page exists (events overhaul, data-model `Id` fields, internal caching; **no crops changes**). | https://store.steampowered.com/news/app/413150 ; https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6.16 ; https://wiki.stardewvalley.net/Modding:Migrate_to_Stardew_Valley_1.6.16 | High (official + wiki snippets; direct 403) |
| SDV 1.7 | Announced 2025-08-30; no release date; ConcernedApe (Oct 2026, Red Rocks): "probably next year" (~2027). Confirmed: Clint + Sandy marriage candidates, new farm type, children overhaul. **No crop/seed model changes announced.** | https://www.gamesradar.com/…stardew-valley-update-1-7… (2026-11-03) ; https://www.gamingbible.com/news/stardew-valley-1-7-guide-535479-20261007 ; https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.7 | High (announcements) / Medium-High (wiki page) |
| SMAPI | **4.5.2** (current; requires SDV 1.6.14+); 4.5.x = in-game config UI + attested builds + perf. Repo manifest `MinimumApiVersion` 4.4.0 / `MinimumGameVersion` 1.6.14 is correct. | https://smapi.io/ (fetched 2026-11-09) ; https://github.com/Pathoschild/SMAPI/releases | High (official) |
| Toolchain | Pathoschild recommended: .NET 6 + `Pathoschild.Stardew.ModBuildConfig` — exactly this repo. | https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started ; https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md | High |
| 1.7 readiness | `Data/Crops` fields stable across 1.6.16/1.7 as documented; keep to existing fields; don't design against 1.7 `DisplayShops` yet. | https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.7 | Medium-High |

## 2) Registration & assets (verified against decompiled assemblies + wiki snippets + SVE)

| Topic | Finding | Source | Confidence |
|---|---|---|---|
| `Data/Crops` schema (1.6) | Keys = **seed item IDs**. Fields (decompiled `CropData.cs`, verified): `Seasons` (List<Season>), `DaysInPhase` (List<int>; sum = days to harvest; number of growth sprites), `RegrowDays` (default −1), `IsRaised`, `IsPaddyCrop`, `NeedsWatering` (default true), `PlantableLocationRules`, `HarvestItemId`, `HarvestMinStack`/`HarvestMaxStack`, `HarvestMaxIncreasePerFarmingLevel` (float), `ExtraHarvestChance` (double 0–0.9), `HarvestMethod` (Grab/Scythe), `HarvestMinQuality`/`HarvestMaxQuality`, `TintColors` (List<string>), `Texture` (default `TileSheets\crops`), `SpriteIndex` (0-based row), `CountForMonoculture`, `CountForPolyculture`, `CustomFields`. No `CropTexture`/`SeasonOffset` custom fields in 1.6 (older docs stale). | Decompiled `StardewValley.GameData.Crops.CropData` @ `/home/luis/stardew-assemblies/StardewValley.GameData.dll` (this session) ; https://github.com/Dannode36/StardewValleyDecompiled/blob/main/StardewValley.GameData/StardewValley.GameData.Crops/CropData.cs ; https://stardewvalleywiki.com/Modding:Crop_data | High (decompiled model) |
| Crop sheet layout | One **row per crop**; 16×32 px sprites; **first 2 = seed variants** (one phase), then growth phases, then harvestable sprite (not counted in `DaysInPhase`); optional regrowing sprite + tint-overlay sprite for colored crops. | https://stardewmodding.wiki.gg/wiki/Tutorial:_Adding_a_New_Crop (2025-06-04) | High |
| `Data/Objects` seeds | `Type: "Seeds"`, `Category: -74` required to plant; `Texture` may point at `Mods/...`; `SpriteIndex` 0 for single-16×16 sheets. | https://stardewmodding.wiki.gg/wiki/Tutorial:_Adding_a_New_Crop ; https://stardewvalleywiki.com/Modding:Objects (snippets) | High |
| `Data/Objects` produce | `Type: "Basic"`, category per crop (−75 vegetable); `ContextTags` e.g. `category_vegetable`, custom tags; `Edibility`, `CanBeGivenAsGift`; `ExcludeFromShippingCollection` optional. | Decompiled `ObjectData.cs` (this session) ; https://stardewvalleywiki.com/Modding:Objects | High |
| `Data/Shops` | **`Traveler` = traveling cart** (confirmed: string table + wiki "Vanilla shop IDs"). Pierre = `SeedShop`. Shops do **NOT** require an NPC; any ID can open via tile property or C# `Utility.TryOpenShopMenu`. Entries: `ItemId`/`RandomItemId`, `Id` unique, `Price`, `AvailableStock`, `AvailableStockLimit` (**`Global`/`Player`/`None`**), `Condition` (game state queries, e.g. `SEASON Spring`), `TradeItemId`+`TradeItemAmount`, `PriceModifiers`+`PriceModifierMode`, `UseObjectDataPrice`. `TargetField ["SeedShop","Items"]` edit pattern documented for adding to Pierre via CP; via SMAPI `AssetRequested` + `AsDictionary<string, ShopData>` equally valid. | Decompiled `ShopData.cs`/`ShopItemData.cs` (this session) ; https://stardewvalleywiki.com/Modding:Shops (2026-04-13 page; snippets; direct 403) ; string table @ offset 5291542 (this session) ; https://wiki.stardewvalley.net/Modding:Shops | High (decompiled + wiki snippets) |
| Game integration seams | `HoeDirt.plant(string itemId, Farmer who, bool isFertilizer)` — instance method, plant-time hook (verified by decompilation). `Crop.harvest(...)` already Harmony-patched by the mod (`HarvestPatches`). `Crop.newDay()` already used by spec 005 growth modifier. `howDirt.fertilizer`/`Crop.GetData()` for data. `Game1.stats.SeedsSown++` is vanilla-tracked. | Decompiled `HoeDirt.plant` (this session) ; `LivingRoots/Patches/HarvestPatches.cs` ; `LivingRoots/Controllers/ModController.cs` (DayEndingHandler/newDay hook) | High |
| Multiplayer state sync | Item modData syncs automatically in MP (`item.modData`, 1.6). Per-tile seed-lineage state would need farm save data or Item.modData (auto-synced) — **prefer lineage on the seed Item.modData** over new per-tile persistence. | https://stardewvalleywiki.com/Modding:Common_data_field_types (snippets) ; roadmap FR-009/A8 pattern (`SoilHealthSyncMessage`, host→farmhand broadcast, `ModMessageReceived`) | High |
| Harmony guidance | Data (objects/crops/shops) → never Harmony. Behavior → Harmony (like existing harvest seam). | SVE pattern (`code/Items/Crops.json` via CP `EditData`) ; wildroot cropgenics (SpaceCore+C# for breeding) | High |
| Multiplayer verification | **JunimoServer** (`stardew-valley-dedicated-server/server`, ~384★, MIT): Docker dedicated server, `sdvd-server-v1.4.0` (2026-01-17); docs recommend preview; bundles SMAPI + E2E harness (`GameTestClient`, `ServerApiClient`, `ClientPool`, HTTP :5123) — usable for automated farmhand-convergence checks. SteamCMD license required (CI secrets, like `BUILDDEPS_*`). Never PR-gate. | https://github.com/stardew-valley-dedicated-server/server ; https://docs.junimoserver.com/developers/testing/test-harness.html | High (repo/docs) |

## 3) Competitive landscape (November 2026; EN/PT/ES searches; Nexus 403 → snippet-derived, labeled)

**No heirloom/landrace/crioulo/agroecology Stardew mod exists in any searched language (high confidence).**

Closest neighbors:

| Mod | # | Status (Nov 2026) | Relation |
|---|---|---|---|
| Stardew Valley Expanded (#3753) | ~228.5k endorsements / 22.4M DL | v1.15.11 (2025-06-30); 12 custom crops via CP; **no provenance/heirloom mechanics** | Content mod template for crop registration; no lineage |
| Joja Seeds (#8746) | ~621 | v1.1.2; Json Assets; Joja GMO crops framed "can't be used in Seed Maker"; ships "Heirloom Carrot/Potato/Corn" as Pierre-side counterpoint | **Only explicit hybrid-vs-heirloom framing**, but as joja/Monsanto propaganda, no soil/seed-saving loop; broken for 1.6 per current tags |
| Joseph's Seed Shop (#11676) | [unverified] | v2.0.0 SMAPI 1.6-compat; Native North American heirloom seeds (einkorn, Hopi), year-gated | Content pack; closest to "heirloom shop" concept; no mechanics |
| Heirloom Sunflowers (#12299) | ~217 | v2.0.1; Json Assets + Multi Yield Crops; 7 open-pollinated varieties; explicitly "save seeds true-to-parent" UX | Only true open-pollinated/heirloom mod; niche cosmetic; requires Multi Yield Crops |
| Harvest Seeds – Continued (#30400) | ~354 | v1.0.10 (2026-03-22), maintained; flat chance 1–2 seeds on harvest (default 10%), configurable regrowables | **Flat-rate drop, no selection/lineage; silent RNG** (top complaint) |
| Multi Yield Crops (#6069) | ~7.8k | v1.0.6 (2026-03-15); framework: per-crop `HarvestRules.json` extra drops incl. seeds | Canonical 1.6 "extra seed drop" mechanism |
| Cropgenics / Wildroot Chronicles (#48079) | ~132–167 | Active (v1.5.3, 2026); 112-trait breeding, named lineages, collection framing; Korean/Chinese dev | Breeding sim, collection identity; not agroecology framing |
| Compost Pests & Cultivation (#4446) | ~104 | **Abandoned** (SDV 1.4-era); seed-maker trait XP + pests; only mod with cultivation/lineage selection | Closest ancestor; abandonment + MP-unsupported + no heirloom brand |
| ReGen Valley (#39740) | **3 endorsements** | v0.51 (2025-11-28); per-tile soil 25.0 baseline, 70/80/90 thresholds, +1/+2 day speed, water retention, farm-wide 75/90 quality+yield, cover crops (rye/clover/buckwheat), grazing | **Only per-tile soil-health implementation**; alpha, MP "early testing", no seed saving, no rotation; American regenerative-ag/Din-Brown-coded; AI-assisted solo dev |
| Seed Shortage / Seeds are Rare / No Buying Seeds | — | Challenge mods; seeds removed from vendors | Evidence: community loves seed-limitation playstyles |
| Expanded Vanilla Crops (#6515) | n/d | CP; all forage → Seed Maker plantables | Recover-from-harvest precedent |

**Seed-saving UX precedents**: (a) Harvest Seeds silent RNG — criticized for zero discoverability (Reddit: players farm hundreds of seeds without noticing); (b) Multi Yield Crops per-crop explicit rules — best practice; (c) Seed Maker vanilla (Farming 9; 97.51% → 1–3 same-variety seeds) — free volume but not lineage; (d) Compost Pests seed-maker inspection tooltip — best "read the seed" UX ever shipped, abandoned. **Lesson: make the seed-saving rule explicit and discoverable** (tooltip/HUD + item description), not silent RNG.

**Brazilian niche**: zero competition. Communities active: r/StardewValleyModsBR (created 2025-11-24), r/StardewValleyBR (2025-11), Stardew Valley Brasil Discord (disboard ~51k), pt.stardewvalleywiki.com, PTBR translation teams (Maatsuki). **PTBR localization is a lever; agroecology vocabulary (crioulo, guardiões, bancos comunitários de sementes, agrobiodiversidade, soberania alimentar) is widespread in NE/SE/Sul audiences and MST/ASA/Embrapa-adjacent circles** (see §6). Demand signal = identity/playstyle (mixed-seeds no-buy loop is beloved; r/StardewValleyMods 2026-03-15 permaculture thread, 16 pts), not explicit feature requests — demand is latent, unmeasured.

## 4) Seed economy mechanics — vanilla & modded (Nov 2026)

- Vanilla Seed Maker (Farming 9): any same-variety produce → 1–3 seeds (97.51%), rare Mixed/Ancient; **modded crops with a crop entry work** (produces "same variety seed"), so heirloom produce will flow through Seed Maker automatically; volume ~51% of harvest sacrificed per cycle.
- Vanilla: coffee beans = produce == seed (self-seeding crop); Ancient Fruit regrows 7 days.
- Multi Yield Crops (above): targeted extra seed drops with per-crop rules + quality inheritance — practical, maintained mechanism for heirloom seed drops if C# is to be minimized; adds a dependency.
- Item.modData (1.6): survives merge/save/**multiplayer sync** automatically — right channel for per-seed lineage state without new per-tile persistence.

## 5) Sprite/asset reality check (Nov 2026)

- All mainstream custom crops use **hand-drawn 16×32 (seed/produce 16×16) sprites**; procedural generation: none found. SCALE 16 → crop rows of 128+ px; repo has zero crop sheets today (`LivingRoots/Assets/` holds only compost/mulch/bin icons).
- Creating 3 crops = 3×(16×32×~7 sprites) + 6 icons + i18n + `Data/Crops` entries. A **single-crop template slice** (heirloom parsnip: 7-day Spring, slower than vanilla 4-day) proves the pipeline for ~⅓ the cost and can ship behind the same ModConstants seam; remaining 2 crops + Elena distribution become follow-up specs once Elena exists (elena-npc.md briefing unimplemented — Welcome Kit/Shop blocked).
- No image-generation capability exists in the agent toolchain for hand-quality sprites → procedural placeholder art (Python/Pillow) or vanilla-fallback textures (e.g. reuse `TileSheets\crops` SpriteIndex 15/parsnip row) with honest `placeholder` disclosure are the only zero-authoring options; final art = maintainer decision.

## 6) Agronomic & sociopolitical grounding (PT/EN/ES)

*(Consolidated 2026-11-09; claims live-cited in §7.)*

See §7 "Agronomic grounding — landrace/crioulo (2026-11-09)" for the consolidated report. Headline claims (verified in-stream):

- **CONAB relaunched crioulo purchases (PAA-Sementes) April 2026** ("Compra de Sementes Crioulas") — national distribution of adapted genetics; MAPA stewards Lei de Sementes e Mudas (n.º 10.711/2003) recognizing "cultivar local, tradicional ou crioula" (Art. 48 limits restrictions on crioulas in public distribution).
- Embrapa runs multiple active crioulas programs (Semiárido, Tabuleiros Costeiros, RS Rede de Pesquisa de Sementes Crioulas e Agroecológicas; E-Campo online course "Ferramentas Participativas e Agroecológicas para Conservação e Multiplicação de Sementes Crioulas"); 230+ bancos comunitários de sementes in ~61 Sergipe municipalities serving ~6,500 families; ASA-PB 60 BCS / 4,308 families / 26.8 t across 13 species and 167 crioula varieties.
- MST paper "Semente crioula: cuidar, multiplicar e partilhar" (2021); UERJ/Tamoios 2025 frames crioulas as "tecnologia social … soberania alimentar"; FASE note AF_FASE_NOTA_TECNICA-01 (2021).
- Landrace vs hybrid agronomy (international literature): see report in this file.

## 7) Agronomic grounding — landrace/crioulo (2026-11-09) [stream complete — filled in this file]

(Stream 3 completed after v1 of this note; incorporated verbatim below.)

## 7) Agronomic grounding — landrace/crioulo (2026-11-09) [stream complete]

> Stream 3 report (verbatim from subagent `ses_e3f489986ffe8fsfAcOCoeUCZx`, all URLs accessed 2026-11-09). Tags: [peer-reviewed] [government/official] [intergovernmental/FAO] [NGO/advocacy] [university extension] [encyclopedia/aggregated]. **Nothing here should be pasted into in-game text as fact without respecting the confidence levels.**

### Method note / honesty preface
The proposed query "Cavatassi et al." returned no matching landrace paper — likely a garbled reference. Canonical anchors: **Camacho Villa, Maxted, Scholten & Ford-Lloyd (2005)** and **Casañas et al. (2017)**. Most quantified landrace trials involve wheat and maize; vegetable-heirloom economics is mostly qualitative + non-Brazilian consumer studies. Brazilian programs are well documented officially (Planalto, Embrapa, CONAB, IPEA) — the strongest leg of the brief.

### 1. Landrace vs. hybrid vs. modern cultivar — the science

**1.1 Definitions (high confidence)**
- Working scholarly definition (FAO/ECPGR-endorsed): a landrace is *"a dynamic population(s) of a cultivated plant that has historical origin, distinct identity and lacks formal crop improvement, as well as often being genetically diverse, locally adapted and associated with traditional cultivation systems"* — Camacho Villa TC, Maxted N, Scholten MA, Ford-Lloyd BV. "Defining and identifying crop landraces." *Plant Genetic Resources* 3(3), 2005. — [WUR publication record](https://research.wur.nl/en/publications/defining-and-identifying-crop-landraces/) / [FAO ResourceBook §B.1](https://www.fao.org/fileadmin/templates/agphome/documents/PGR/PubPGR/ResourceBook/B.1.pdf) — **peer-reviewed / intergovernmental** — Confidence: **high**.
- Evolved concept (Casañas F et al. "Toward an Evolved Concept of Landrace." *Frontiers in Plant Science* 8:145, 2017). [PMC5296298](https://pmc.ncbi.nlm.nih.gov/articles/PMC5296298/) — **peer-reviewed** — Confidence: **high**.
- Portuguese parallel: crioulo / "cultivar local, tradicional ou crioula" in Lei 10.711/2003 Art. 2º XVI (see §3.1).

**1.2 Yield: the honest gap is real, not uniform (high confidence)**
- **Greek field trial (bread wheat, 2 years)**: Year 1 landraces ~**1.55 t/ha** vs ~**3.24 t/ha** commercial (~half). Year 2: best commercial 'Yecora E' 2.57 t/ha vs best landrace 'Xilokastro Lamias' 2.52 t/ha (**statistically indistinguishable**); remaining landraces **33–85% below** commercial in conventional plots. Yield loss conventional→organic: **~45% for commercial cultivars vs ≈22% for landraces** — direct evidence that landraces **degrade less under low-input/organic conditions**; best landraces had **~1.0% higher protein content**; wide inter-plot variance. — Korpetis E et al. "Bread Wheat Landraces Adaptability to Low-Input Agriculture." *Plants (Basel)* 12(13):2561, 2023. [PMC10347258](https://pmc.ncbi.nlm.nih.gov/articles/PMC10347258/) — **peer-reviewed** — Confidence: **high**.
- **Scottish spring wheat landraces**: landrace ≈2.8 t/ha vs modern ≈3.0 t/ha (**not significant**), protein/nutrient content equal-or-greater particularly at **low N availability** — SRUC [research PDF](https://pure.sruc.ac.uk/files/111250133/Landraces_of_spring_wheat_deliver_multiple_functions_under_low-input_conditions.pdf) — **medium** (snippet-level; PDF blocked for automated fetch).
- **Wheat–legume mixtures 2025 (organic/low-input)**: ~2.8 vs ~3.0 t/ha, not significant — [Scopus/ResearchGate record](https://www.researchgate.net/publication/388651076) — **medium**.
- **Brazilian maize, MG**: crioulo **'BR da Várzea' yielded statistically equal to hybrids and +62% above the municipal average**; crioulo 'Argentino' produced well below hybrids (but above Montes Claros municipal mean). Landraces **more N-efficient under low-N soil** (Machado et al. 2003, Embrapa/UFV low-N trials, cited within). — Araujo AV et al. "Desempenho agronômico de variedades crioulas e híbridos de milho…" *Rev. Ciênc. Agron.* 44(4):2013, DOI 10.1590/S1806-66902013000400027. [SciELO](http://www.scielo.br/j/rca/a/JYbqyLrhjkBz6kQF76fkMGy/?lang=pt) — **peer-reviewed** — Confidence: **high** (parity finding for 'BR da Várzea'; **low** generalizability).
- **General pattern**: under high inputs modern cultivars usually win; under **low-input/organic/stress the gap narrows, disappears, or inverts** — trend, not law; crop-, input- and site-dependent. No defensible single "typical yield gap %" exists (see §8 gaps).

**1.3 Genetic diversity & erosion (high confidence)**
- FAO (1996/1999, still anchor): **~75% of plant genetic diversity lost since the 1900s**; **>90% of crop varieties disappeared from farmers' fields**; **75% of world food from 12 plant + 5 animal species**; rice/maize/wheat ≈60% of plant-derived calories & protein. — FAO "What is happening to agrobiodiversity?" [fao.org/4/y5609e/y5609e02.htm](https://www.fao.org/4/y5609e/y5609e02.htm) — **intergovernmental** — Confidence: **high** (canonical but dated; label "FAO estimate 1996/1999" if quoted).
- Landraces carry far wider genetic variability than elite cultivars; reservoir for climate-adaptation alleles — Korpetis 2023; Casañas 2017 — Confidence: **high**.

**1.4 Quality / nutrition (medium confidence — nuanced!)**
- Landrace wheats tend to show **equal-or-better protein/micronutrients**: "GPC is higher in wild and domesticated landrace wheats than in modern cultivars" — Zeibig F et al. "Grain quality traits within the wheat (Triticum spp.) genepool." *J Sci Food Agric*, 2024. [Wiley](https://scijournals.onlinelibrary.wiley.com/doi/10.1002/jsfa.13328) — **peer-reviewed** — Confidence: **high** (wheat).
- Ficiciyan A et al. 2018 "More than Yield: Ecosystem Services of Traditional versus Modern Crop Varieties Revisited," *Sustainability* 10(8):2834 — landraces often lower yield at optimum, more services; gap in yields is main trade-off. [MDPI](https://www.mdpi.com/2071-1050/10/8/2834) — Confidence: **medium-high**.
- **Do NOT claim "heirloom = always more nutritious"** — crop-specific; vegetables unproven; only cereals/wheat strong. Confidence for blanket claim: **low**.

**1.5 Yield stability under stress**
- Heterogeneity → stable performance across environments (widely replicated); organic-environment yield penalty **halved** for landraces (Korpetis 2023); low-input landrace first in lowest-fertility field — Confidence: **high** (wheat).
- Barley landraces drought-response variation (Shrestha 2026) — **low-medium** (single study).
- Ethiopian preprint: 66.7% decline in traditional wheat landrace cultivation after improved-variety introduction — **low** (preprint; illustrative only).

**Net defensible claims**: (a) landraces = dynamic, heterogeneous, locally adapted; (b) genetic-diversity reservoirs; (c) smaller low-input/organic yield penalties; (d) roughly equal-or-better protein in cereals. "Typical yield gap" **% is crop-, input- and site-dependent** — avoid a single magic number in-game (see §7 design mapping).

### 2. Seed saving — economics, agronomy, risks

**2.1 Why farmers save seed (high confidence)**
- Cashlessness/access: farm-saved seed "requires no cash outlay" — GRAIN "The real seed producers" (2013) [grain.org](https://grain.org/en/article/6035-the-real-seeds-producers-small-scale-farmers-save-use-share-and-enhance-the-seed-diversity-of-the-crops-that-feed-africa) — **NGO/movement** — Confidence: **medium** (corroborated).
- **~80–90%** of seed used by farmers in developing countries comes from **informal/local systems** (FAO compendium) — [fao.org What are seed systems](https://www.fao.org/agriculture/crops/thematic-sitemap/theme/compendium/tools-guidelines/what-are-seed-systems/en/) — **intergovernmental** — Confidence: **high**. FAO also states small-scale farmers "feed 70% of the world's population" (agroecology in action) — Confidence: **medium-high**.
- Local adaptation & resilience: Hlatshwayo et al. 2021 (~60% vegetable farmers save own seed) [PMC7616262](https://pmc.ncbi.nlm.nih.gov/articles/PMC7616262/) — **peer-reviewed** — Confidence: **medium**. Coomes et al. 2015 *World Development* 69:207–222 (farmer seed networks) — [ScienceDirect](https://www.sciencedirect.com/science/article/pii/S030691921500086X) — **peer-reviewed** — Confidence: **high**. McGuire & Sperling 2016, *Food Security* 8:401–411 — [Springer](https://link.springer.com/article/10.1007/s12571-015-0528-8) — Confidence: **high**.
- **Brazil specifically**: crioulas are policy-supported — Conab "PAA Sementes" buys crioulas at premium (~30% above market for transgenic-free maize — "Fubá da Paixão") [AS-PTA 2021](https://aspta.org.br/2021/03/30/sementes-da-paixao-acao-coletiva-na-defesa-da-agrobiodiversidade-no-territorio-da-borborema-na-paraiba/) — **NGO** — Confidence: **high**.

**2.2 Replanting F1 hybrids vs OP/landrace (high confidence — extension consensus)**
- Replanted F1 → **F2 segregation**: progeny "tend to revert back to the parents", "extremely variable" — Missouri IPM "Why Not Save Hybrid Seed?" [ipm.missouri.edu/mpg/index.cfm?ID=19](https://ipm.missouri.edu/mpg/index.cfm?ID=19) — **university extension** — Confidence: **high**.
- Quantified: F1/F2 maize comparison found **F2 saved seed produced ~29% less grain than F1** — Iowa State University Extension "F1/F2 Corn Variety Study" [iastatedigitalpress.com](https://iastatedigitalpress.com/farmreports/article/4962/galley/4828/download/) — **university research** — Confidence: **medium-high**.
- OP/landrace varieties come **true-to-type when selected** — farmer massal selection documented (>10 yrs for 'BR da Várzea') — AS-PTA/Embrapa materials — Confidence: **high**.

**2.3 Risks of saved seed (high confidence)**
- **Seed-borne pathogens travel with the seed** (14 pathogens × 9 crops, Brodal 2021 [PMC8539852](https://pmc.ncbi.nlm.nih.gov/articles/PMC8539852/)) — Colo. State Extension; Kentucky PPFS-VG-09; UVM organic guidance — **peer-reviewed + university extension** — Confidence: **high**.
- **Cross-pollination / contamination**: Borborema documented transgenic contamination of community maize (2012–2016) → campaign "Não Planto Transgênico para não Apagar minha História" — Qualcomm AS-PTA 2021 — Confidence: **high** (as documented event), **low** for general rates.
- **Varietal drift**: crioula seed loses germination vigor with rudimentary storage — Embrapa PIBIC Alice 1106014; Embrapa Circular Técnica 99 (2025) prescribes drying/storage/ear selection — **government research** — Confidence: **high**.

**2.4 Best practices (extension-grade)** — select best plants; harvest at full maturity; dry <13% moisture; clean/grade; store cool, dark, dry, rodent-proof; label by variety & year; test germination pre-plant; isolate outcrossers (maize/squash); rotate germplasm periodically; swap/buy in networks (bancos de sementes) — synthesized from [Embrapa Circular Técnica 99 (2025)](https://www.embrapa.br/busca-de-publicacoes/-/publicacao/1172471/sementes-crioulas-procedimentos-rotineiros-da-colheita-ao-armazenamento) and Kentucky PPFS-VG-09. Confidence: **high**.

### 3. Brazilian context (PT) — sementes crioulas

**3.1 Legal status (high confidence — primary government sources)**
- **Lei nº 10.711/2003** (Sistema Nacional de Sementes e Mudas, "nova lei de sementes") [planalto.gov.br](https://www.planalto.gov.br/ccivil_03/leis/2003/l10.711.htm) — key provisions:
  - **Art. 2º, XVI** — *cultivar local, tradicional ou crioula*: variedade desenvolvida/adaptada/produzida por agricultores familiares, assentados ou indígenas, com "características fenotípicas bem determinadas e reconhecidas pelas respectivas comunidades" e que "**não se caracterizem como substancialmente semelhantes às cultivares comerciais**".
  - **Art. 2º, XLIII** — *semente para uso próprio*: saved by the farmer each season for own next-year/on-property use.
  - **Art. 8º, §3º** — family farmers / settlers / indigenous multiplying for self-distribution/exchange/commercialization among themselves are **exempt from RENARSEM registration**.
  - **Art. 11, §6º** — **RNC inscription NOT mandatory** for local/traditional/crioula cultivars used by these groups.
  - **Art. 48** — "é vedado o estabelecimento de restrições à inclusão de sementes e mudas de cultivar local, tradicional ou crioula em programas de financiamento ou em programas públicos de distribuição ou troca de sementes".
  - Regulated by **Decreto 5.153/2004** ([ABRASEM copy](http://www.abrasem.com.br/wp-content/uploads/2012/10/Decreto-n%C2%BA.-5.153-de-23-de-julho-de-2004-Regulamenta-a-Lei-de-Sementes-e-Mudas.pdf)); FAOLEX consolidates ([bra49379.pdf](https://faolex.fao.org/docs/pdf/bra49379.pdf)). UFSC 2016 review flags ongoing legal tensions — Confidence: **medium** on nuance.
- **PNAPO**: **Decreto nº 7.794/2012** [planalto.gov.br](https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2012/decreto/d7794.htm) — creates Política Nacional de Agroecologia e Produção Orgânica (integrate/articulate/adapt policies for the agroecological transition + organic production). PLANAPO I (2013–2015), II (2016–2019), **PLANAPO 2024–2027** ([PLANAPO 2024-2027 PDF](https://agroecologia.org.br/wp-content/uploads/2024/03/PLANAPO-2024-2027-WEB-2.pdf)); seed flagship = **PNSMAF** (Programa Nacional de Sementes e Mudas da Agricultura Familiar; Portaria Interministerial nº 1/2012 — **medium** on numbering; existence/history **high**); **PNAPO officially retomada 2023** ([agroecologia.org.br 2023-06-29](https://agroecologia.org.br/2023/06/29/politica-nacional-de-agroecologia-e-producao-organica-e-oficialmente-retomada/)).

**3.2 PAA Sementes (CONAB)** — Programa de Aquisição de Alimentos, modalidade PAA Sementes / Compra com Doação Simultânea; buys crioulas from family farmers/co-ops/associations, donates to food-insecure families/indigenous/quilombola — [CONAB chamada 2025](https://www.gov.br/conab/pt-br/atuacao/agricultura_familiar/execucao-do-paa/chamada-de-sementes-2025-criterios) ; [MDS PAA presentation](https://www.mds.gov.br/webarquivos/publicacao/seguranca_alimentar/Apresentacao_PAA.pdf) — **government** — Confidence: **high**. Up to **5% of PAA budget earmarked for seeds** ([AS-PTA 2020](https://aspta.org.br/2020/08/03/projeto-de-lei-735-e-um-pouco-da-historia-do-programa-de-aquisicao-de-alimentos-paa/), **medium-high**). **2026: own annual budget line — R$ 15,000/family cap** (Resolução GGPAA nº 38/2026, [jusrural.com.br](https://jusrural.com.br/artigos/paa-sementes-crioulas-resolucao-ggpaa-38-2026-limite-15-mil-familia-preco-referencia-conab/)) — Confidence: **medium**. Scale example: **44.4 t of crioulas** (maize/beans/rice/cassava) distributed to **~890 families in Pará** ([agrolink.com.br](https://www.agrolink.com.br/noticias/para---sementes-crioulas-chegam-a-agricultores-familiares-de-paragominas_511100.html)) — Confidence: **medium**. **April 2026: PAA-Sementes relaunched** (Broto/AgFeed 2026-04-13).

**3.3 "Sementes da Paixão" & Casa da Semente (Borborema, Paraíba)** — crioulas called "sementes da paixão"; Casa da Semente / Bancos Comunitários de Sementes network; Festa das Sementes da Paixão (since ~2006); 2012–2016 transgenic contamination crisis → "Não Planto Transgênico" campaign; "devolve o gene de volta" seed exchange; "Fubá da Paixão" (transgene-free maize, **+30% price**); book "As Sementes da Paixão e as Políticas de Distribuição das Sementes na Paraíba" (AS-PTA/EUSC, cited 37×). [AS-PTA 2021](https://aspta.org.br/2021/03/30/sementes-da-paixao-acao-coletiva-na-defesa-da-agrobiodiversidade-no-territorio-da-borborema-na-paraiba/) ; [PDF](https://agroecologia.org.br/wp-content/uploads/2014/05/as-sementes-da-paixao-e-as-politicas-de-distribuicao-das-sementes-na-paraiba.pdf) — **NGO primary** — Confidence: **high**. ASA Brasil "Sementes do Semiárido" — [asabrasil.org.br](https://asabrasil.org.br/projeto/sementes-do-semiarido/) — Confidence: **high**.

**3.4 Embrapa** — **Doc 490 "Agrobiodiversidade: sementes crioulas e seus guardiões"** (Recursos Genéticos e Biotecnologia) [infoteca PDF](https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1129536/1/Doc-490-Agrobiodiversidade.pdf) — **government research**. **Circular Técnica 99/2025** "Sementes crioulas: procedimentos rotineiros da colheita ao armazenamento" — *"O conhecimento tradicional das guardiãs e dos guardiões … e o conhecimento técnico de profissionais de ciências agrárias devem ser complementares"* [embrapa.br record 1172471](https://www.embrapa.br/busca-de-publicacoes/-/publicacao/1172471/sementes-crioulas-procedimentos-rotineiros-da-colheita-ao-armazenamento) — **the single best PT citation for extension framing**: traditional + technical knowledge are **complementary**, not scored "peasant vs science". **"Manejo comunitário da agrobiodiversidade"** [embrapa.br 1129395](https://www.embrapa.br/en/busca-de-publicacoes/-/publicacao/1129395/manejo-comunitario-da-agrobiodiversidade-producao-agroecologica-de-sementes-de-variedades-crioulas-por-agricultores-familiares) — Confidence: **high**. **Documentos 269** (Meio-Norte): Fazenda Vista Alegre crioulo amarela **5.4 t/ha** irrigated; sequeiro vermelho 4.2 t/ha — Confidence: **medium**. UFV-led crioulas vs hybrids trial (Araujo 2013, §1.2) — Confidence: **high**. Machado et al. 2003: commercial > crioulas except **under low-N soil where crioulas outyielded** — **medium** (cited within). **MST/Rede Bionatur** — movement seed company, +27 yrs — Confidence: **medium** (existence; figures unverified). **ANA** — [agroecologia.org.br 2023-10-31](https://agroecologia.org.br/2023/10/31/agroecologia-biodiversidade-e-sementes-crioulas-em-debate/) — Confidence: **high** (institutional fact).

**3.5 University extension (PT)** — UFRGS theses frame crioulas as **"autonomia produtiva e segurança alimentar"** [lume.ufrgs.br](https://lume.ufrgs.br/bitstream/handle/10183/221642/00112203.pdf?sequence=1) (note: archived URL corrected); UFSM Extensão Rural; UNESP-FCT (guardiões e troca de sementes); UFV/UFMG participatory crioulo trials; IFPB (Casas da Semente). **UFMT**: no primary source verified 2026-11-09 — **GAP**.

### 4. Seed sovereignty / food sovereignty — claims & counter-claims

**4.1 Pro-sovereignty canon (high confidence)**
- **La Via Campesina**: "Peasants' seeds are a heritage of peoples in the service of humanity" (2003 seed campaign) [LVC Training Module](https://viacampesina.org/en/wp-content/uploads/sites/2/2022/02/LVC-EN02-Training-Module.pdf) — **advocacy** (label in-game if cited).
- **UNDROP** (UNGA res. 73/165, 2018-12-17): **Art. 19 – Right to seeds** — right to traditional knowledge, customary use/exchange/sale of farm-saved material, participation in seed decision-making. [LVC hosted text](https://viacampesina.org/en/wp-content/uploads/sites/2/2020/07/The-UN-Declaration-on-the-Rights-of-Peasants.pdf) ; analyses: Haugen 2020 ([resources.semencespaysannes.org](https://ressources.semencespaysannes.org/docs/undrop_article19.pdf)); Girard 2024 *IJGR* 32(2):344 ([Brill](https://brill.com/view/journals/ijgr/32/2/article-p344_009.pdf)) — **UN / peer-reviewed** — Confidence: **high**.
- **ITPGRFA** (in force 2004): **Art. 9 – Farmers' Rights** (protection of traditional knowledge, benefit-sharing, participation; right to save/use/exchange/sell farm-saved seed "subject to national law") — [FAO Farmers' Rights](https://www.fao.org/plant-treaty/areas-of-work/farmers-rights/overview/en) / [treaty text](https://faolex.fao.org/treaty/docs/tre000005E.pdf) — Confidence: **high**.
- Peschard K (2020) "'Keeping seeds in our hands': the rise of seed activism" *Third World Quarterly* — [T&F](https://www.tandfonline.com/doi/full/10.1080/03066150.2020.1753705) — Confidence: **high**.

**4.2 Counter-position (intellectual honesty) (high confidence)**
- **Certified/improved seed delivers real productivity gains**: USDA AMS "More and Better Choices for Farmers" — [USDA PDF](https://www.ams.usda.gov/sites/default/files/media/SeedsReport.pdf) — **government** — Confidence: **high**.
- Farming First (Jan 2025): certification protects seed quality & food security — [farmingfirst.org](https://farmingfirst.org/2025/01/how-seed-certification-protects-both-farmers-and-food-security/) — **industry-side lobby** (flag provenance) — Confidence: **medium**.
- **FAO nuance**: both systems coexist and complement — formal certified seed for commercial identity/quality; farmer-saved seed dominates smallholder fields; "neither alone feeds the world" — [FAO The state of the seed sector](https://www.fao.org/4/y2722e/y2722e0d.htm) — Confidence: **high**.
- Coomes et al. 2015: community seed networks face scale/maintenance/quality limits; adoption effect limited vs improved seed under some metrics — Confidence: **high** (as position).
- Green Revolution record (wheat 2–2.5× since 1950s) strongest pro-hybrid anchor — Confidence: **high**.
- **Honest synthesis**: crioulas/peasant seeds are *differently excellent* (resilience, autonomy, diversity, cultural value) vs modern varieties (yield ceiling, uniformity, mechanizability). Any honest narrative keeps both true.

**4.3 Seed treaty law tension (medium confidence)**
- Brazil is party to ITPGRFA + WTO/TRIPs; Lei de Cultivares (9.279/96) recognizes farmer's privilege (saved own seed) but not general resale of saved protected varieties; crioulas sit in a distinct legal niche (Art. 48; §3º) — UFSC 2016 review — Confidence: **medium-high** on the outline; verify before citing as legal fact.

### 5. Three Sisters × landrace interplay (brief)
- **Milpa** (maize + beans + squash) is a **landrace-based** intercrop; beans supply N via fixation — **not** maize/squash. Vazeux-Blumental 2024 "The milpa…" *C. R. Biologies* ([CR Biologies](https://comptes-rendus.academie-sciences.fr/biologies/articles/10.5802/crbiol.164/)); Benrey 2024 *Trends in Plant Science*; USDA NAL [Three Sisters](https://www.nal.usda.gov/collections/stories/three-sisters) — Confidence: **high** (intercrop); implication: **nitrogen fixation belongs to the beans**. Heirloom *varieties* interplay is legitimate inspiration (Borborema crioulas maize+beans both documented).

### 6. Extension framing & consumer preference
- Official PT tone: **guardiãs/guardiões**, "autonomia", "segurança alimentar", "tradição + técnica complementares" — [Embrapa Circular Técnica 99](https://www.embrapa.br/busca-de-publicacoes/-/publicacao/1172471/sementes-crioulas-procedimentos-rotineiros-da-colheita-ao-armazenamento) — Confidence: **high**.
- Tourist/gastronomy channels: "Fubá da Paixão" marketed transgene-free; Festa da Semente / Feira Agroecológica premium narrative — AS-PTA 2021 — **medium-high** (qualitative).
- Germany/Austria: Demeter "samenfeste Sorten"; Tagesspiegel old-apple conservation (Oct 2026) — Confidence: **low**.
- **Consumer WTP / price premium**: Meier & Oehen 2019 (Switzerland; EU DIVERSIFOOD; consumers show substantial WTP for farmers' varieties — value framing) *Sustainability* 11(24):7134 [MDPI](https://www.mdpi.com/2071-1050/11/24/7134) — **peer-reviewed** — **medium**. Meta: Maró 2023 *Renew. Agric. Food Syst.* ([Cambridge](https://www.cambridge.org/core/journals/renewable-agriculture-and-food-systems/article/profiling-the-consumers-of-farmers-markets-a-systematic-review-of-surveybased-empirical-evidence/98A468ED46F4A80777A3007DE2A82C99)) — **medium**. US: Bond 2006 Choices 21(4) ([choicesmagazine.org](https://www.choicesmagazine.org/2006-4/produce/2006-4-06.htm)) — **medium**; Carroll 2013 JARE (tomato local WTP) ([ageconsearch PDF](https://ageconsearch.umn.edu/record/165934/files/JARE_Dec2013__6_Carroll_pp379-396.pdf)) — **medium**. Mexico: Valero-Padilla 2024 (artisanal/traditional tortillas premium) ([SciELO MX](http://www.scielo.org.mx/scielo.php?script=sci_arttext&pid=S2007-09342024000200001)) — **medium**. **Brazil-specific quantitative WTP: GAP** — do not state a % as fact; AS-PTA fubá (+30%) is one case study, not a market law.

### 7. Honest game-design mapping — defensible vs. not

| In-game element | Verdict |
|---|---|
| "+2 soil health per heirloom planting" | **Fine as labeled abstraction** — no agronomic mechanism; document as abstraction; never tie to N logic. |
| "Heirloom crops fix nitrogen" | **FALSE** (only legumes) — never as fact; if a bean is heirloom, mechanism = inoculation/legume N. |
| "Heirlooms grow slower" | **Defensible as trend/variety trade-off** (landraces typically later — Korpetis; maize 'Argentino') — not universal. |
| "Higher quality / extra harvest in living soil" | **Half true**: (a) protein ≥ hybrids (Zeibig; Korpetis +~1%); (b) yield **penalty shrinks ~50% low-input/organic vs conventional** (Korpetis); (c) "extra harvest" ≈ longer harvest window / indeterminate habit, not guaranteed yield boost → implement as **yield-stability + quality grade-up in high soil health** (hybrid's peak-yield advantage removed there). |
| Seed-saving replanting loop (OP true-to-type; F1 F2 ~−29% yield) | **Accurate** — teach it: Pierre's hybrids do not come true-to-type (F2 segregates, −29% grain). — Iowa State F1/F2 study; Missouri IPM. |
| Save-seed economics (cashless loop vs annual Pierre purchase; ~80–90% informal seed share [FAO]) | **Accurate** — crioulas = compounding autonomy vs hybrids = annual cash sink. Matches PAA/CONAB narrative. |
| "Heirloom quality > hybrid quality" automatic | **NOT automatic** — cereal-specific protein/flavor evidence only; make crop-specific or omit. |
| "Heirloom always out-performs in poor soil" | **FALSE as universal** — 'Argentino' lost; 'BR da Várzea' tied (+62% municipal). Teach via **chance/variance**, not guaranteed win. |
| Contamination quest (Borborema) | **Documented** (Polo da Borborema 2012–16) — legitimate storyline; specific event, not destiny. |
| "Living soil" synergy label | **Fine** — soil-health/integrated fertility is the experimentally-relevant axis; keep vague-but-labeled. |
| Elena (agroecologist) vs Pierre (hybrid seller) | **Accurate archetype pairing** (EMATER/ASA/Embrapa extension vs agro-input dealership) — but Pierre is **not evil**: his seeds are excellent for high-input systems (USDA AMS). |
| Terminology: "sementes crioulas", "guardiãs/guardiões", "sementes da paixão", "bancos de sementes comunitários" | **Accurate** — use PT terms with PT glossary. |
| Numbers like "+10% quality when soil > 70" | Abstractions — **permitted** if labeled as abstractions (project already labels them); never "agronomic constants". |

**Avoid teaching as fact**: "crioulas never need fertilizer", "crioulas are drought-proof", "crioulas carry zero disease risk", "saved hybrid seed is sterile" (F1 segregates, not sterile), "crioulas are pre-industrial" (actively selected today), "planting crioulas raises soil organic matter" — none survive the evidence.

### 8. Gaps & verify-before-publish

1. "Cavatassi et al." — not found; use Villa et al. 2005 / Casañas 2017 / FAO B.1.
2. Quantified nutritional superiority of crioulas **vegetables** — absent; cereal/wheat evidence only.
3. **Brazilian consumer price-premium quantification** — absent; treat AS-PTA +30% (fubá) as case study.
4. **UFMT crioulas selection/extension program** — not verified 2026-11-09.
5. **PNSMAF current Portaria vehicle** — verify at MAPA/MDA before citing in-game.
6. **Yield gap "typical %"** — no single defensible figure; Korpetis: ~50% (yr 1), parity (yr 2 best-landrace), ~22% organic-environment penalty halving — state context, never a universal number.
7. FAO 75%/90% erosion figures are **1996–99 vintage** — canonical but dated.
8. GMO-contamination — community-reported (AS-PTA), not lab-certified generalizable.

**One-line TL;DR**: heirloom/sementes crioulas are *real, legally recognized in Brazil, policy-subsidized, genetically diverse, autonomy-giving seeds whose honest agronomic selling points are yield stability under low-input/organic conditions and higher grain protein — not universally higher yields, not nitrogen fixation, not automatic nutrition wins; Pierre's hybrids remain the right choice for high-input systems; both statements should ship in the game.*

## 8) Design-relevant conclusions (for define/shape/decide)

1. **Distribution without Elena** is required for a playable slice: the briefing's Elena shop is blocked. Data-driven paths exist: (a) `SeedShop`/`Traveler` via `Data/Shops` entries — **traveler ("Traveler") as the crioulo vector fits the narrative** ("sementes que viajam de mão em mão") and needs no NPC, no tile, no map edits; (b) vanilla Seed Maker already lets heirloom produce → heirloom seeds (97.51%, 1–3) for free; (c) a C# harvest seam can add a guaranteed-but-capped seed drop for heirloom crops (Multi Yield-style but native, no dependency). Pierre can stay hybrid-only — the contrast is preserved **without** touching `SeedShop` (less surface, fewer conflicts).
2. **One-crop 80/20 template** beats three-crop briefing scope for this cycle: the pipeline (registration, growth, soil hook, seed-saving, feedback, i18n, tests, Gate 3) is the deliverable; content breadth follows. Recommend **1 heirloom crop (Spring parsnip "Cenoura/Parsnip Crioulo")** — vanilla-eligible, cheapest art, closest crioulo archetype, already the briefing's year-1 anchor.
3. **Seed-saving is the differentiator** the market lacks (flat-rate silent RNG mods dominate; nobody does lineage selection + sovereignty framing). Minimal honest version: heirloom harvest → guaranteed seed return chance (configurable, default tuned) + explicit player-facing messaging + produce carries `heirloom_seed` lineage tag in Item.modData (optional, MP-safe) — no genetics simulation (YAGNI), but the frame ("cultivar local, tradicional ou crioula") and copy are institutionally real.
4. **Soil coupling**: `+2` on heirloom plant, `+1` on harvest in Healthy band — abstractions, labeled as such; no agronomic claim of the form "heirlooms fix nitrogen" (false except legumes). Honest in-game text: slower-growing, more resilient, "the soil remembers".
5. **Multiplayer (Principle VI)**: implement plant/harvest bonuses through the same host-authoritative pattern (host applies; farmhands apply locally only via host-mediated message or get explicit refusal semantics mirroring the agrochemical slice). `Item.modData` syncs automatically — safe channel for lineage tags.
6. **Gate 3**: new scenario `heirloom-plant-bonus.scenario.json` (plant via `lr_plant_crop` with an heirloom seed id registered verification-only, assert `soilHealthStateDelta` vs baseline, plus `cropPhaseDelta`, `smapiLog` host check) is feasible headless host-only; honesty R; D1/human E remain `pending`.
7. **1.7/1.6.16**: design against stable `Data/Crops` fields only; fine.
8. **Asset reality**: no image-gen toolchain → procedural placeholder PNGs (Python/Pillow) with honest disclosure, or vanilla-fallback `Texture` pointers; final art + any public channel = maintainer decision. No new npm/pip dependencies committed beyond repo norm (stdlib-ish).
9. **Competitive proof**: unclaimed niche confirmed; ReGen Valley proves per-tile soil interest (albeit alpha); Harvest Seeds–Continued proves seed-drop demand (354 endorsements, actively maintained); crioulo framing has institutional grounding in Brazil (CONAB/MAPA/Embrapa/MST). Honest framing wins: never claim agronomic superiority that the literature doesn't support (see §6/§7).

## 9) Open questions carried to define/shape

- Exact slice size: 1 vs 3 crops; seed-saving as built-in vs document-only.
- Whether `SeedShop` gets a token entry (Pilares y contraste Pierre×crioulo) or zero shop edits (Traveler only) — smaller blast radius vs. narrative contrast.
- Whether the plant/harvest soil bonus routes through new Harmony patches (`HoeDirt.plant` postfix + harvest seam extension) or is folded into existing `Crop.newDay`/`HarvestPatches` — new patches need the same `IsMainPlayer` guard + lock discipline.
- Seed-saving UX: chance (RNG) vs guaranteed-but-capped (1 seed per heirloom harvest, "saved seed" framing) — second is more honest (abstraction of selection) and cornier; cheaper to verify.
- Pricing: heirloom produce premium (≥ vanilla, e.g. parsnip 45g vs 35g) justified by quality/soil synergy; seed price ≥ vanilla (25g parsnip seeds → 35g?) with `AvailableStockLimit Player` to prevent farm-wide flood.
- Scope of release: repo-scoped 0.2.0 (precedent 017) or no release this cycle; tag stays maintainer-only.

## Sources

(Keep in one place; all accessed 2026-11-09 unless noted.)

- smapi.io — SMAPI 4.5.2, requires 1.6.14+ — https://smapi.io/
- SDV 1.6.15 patch — https://store.steampowered.com/news/app/413150
- Wiki (403 direct; search snippets + mirrors): https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6.16 ; https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6 ; https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.7 ; https://stardewvalleywiki.com/Modding:Crop_data ; https://stardewvalleywiki.com/Modding:Objects ; https://stardewvalleywiki.com/Modding:Shops ; https://stardewvalleywiki.com/Modding:Items ; https://zh.stardewvalleywiki.com/%E6%A8%A1%E7%BB%84:%E7%89%A9%E5%93%81%E6%95%B0%E6%8D%AE
- Modding wiki.gg (fetched OK): https://stardewmodding.wiki.gg/wiki/Tutorial:_Adding_a_New_Crop (2025-06-04) ; https://stardewmodding.wiki.gg/wiki/Permaculture_Farming
- Decompiled assemblies @ `/home/luis/stardew-assemblies/` (this session, ilspycmd 11.2.0.10050): `CropData`, `ObjectData`, `ShopData`, `ShopItemData`, `HoeDirt.plant`, `Crop`, `ShopBuilder.GetShopStock`
- GitHub: https://github.com/Dannode36/StardewValleyDecompiled ; https://github.com/FlashShifter/StardewValleyExpanded (CP Crops.json pattern) ; https://github.com/MizuJakkaru/Cornucopia ; https://github.com/stardew-valley-dedicated-server/server ; https://docs.junimoserver.com/developers/testing/test-harness.html
- Nexus (403; snippet-derived): https://www.nexusmods.com/stardewvalley/mods/{39740,8746,12299,11676,30400,11773,6069,4446,48079,3753,3012,22311,26712,14236,19608} ; https://stardewvalleyexpanded.wiki.gg/wiki/Crops
- Reddit: https://www.reddit.com/r/StardewValleyMods/comments/1ru3ey9/permaculture_mods/ ; r/StardewValley/9rfp5p ; r/StardewValleyBR ; r/StardewValleyModsBR
- Brazilian institutional: https://www.embrapa.br/ ; https://mst.org.br/download/semente-crioula-cuidar-multiplicar-e-partilhar/ ; https://www.inma .gov.br (MAPA/Lei 10.711/2003 context) ; CONAB PAA-Sementes (Broto/AgFeed 2026-04-13) ; https://www.infoteca.cnptia.embrapa.br/ (CONSERV-USO 2022, 230+ BCS SE)
- Local: `ROADMAP.md`, `OBJETIVO.md`, `specs/future-roadmap/briefings/heirloom-seeds.md`, `LivingRoots/ModEntry.cs` (OnAssetRequested pattern), `LivingRoots/Constants.cs`, `LivingRoots/Domain/Services/HealthCategory.cs`, `LivingRoots.Tests/AssetRegistrationTests.cs`, `LivingRoots.Tests/AgrochemicalRegistrationTests.cs`, `LivingRoots/Patches/HarvestPatches.cs`, `LivingRoots/Controllers/ModController.cs` (OnDayStarted/OnDayEnding/FR-009/A8), `specs/017-joja-agrochemical-tradeoff/`
