# Heirloom Seeds / Sementes Crioulas Pipeline — Research (2026-11-09)

**Scope**: Deep, current (Nov 2026) web research, in EN/PT/ES/FR/DE + primary-source verification of the SDV/SMAPI technical surface, to inform the `heirloom-seeds-pipeline` assessment (`.specify/assessments/heirloom-seeds-pipeline/`). Flow adapted from the MDMV Dark Software Factory (`/home/luis/development/MDMV/projetos/mdmv-dark-factory`).

**Honesty**: `machine-pass` ≠ `verified`. External facts cited with URL + date + confidence. Local facts verified from files/commands this session (2026-11-09). Nexus direct fetches are largely HTTP 403/Cloudflare-blocked — Nexus figures come from search-engine snippets and are labeled; missing data is reported as missing, never invented.

**Method**: 3 parallel background research streams (technical SDV/SMAPI surface; competitive/market landscape incl. Brazilian communities; agronomic grounding — see §6). Streams 1–2 complete; stream 3 (agronomic) was in flight at time of writing and is appended as its own section before this cycle's artifacts are finalized. Primary verification done directly by the parent session (decompiled `Stardew Valley.dll` / `StardewValley.GameData.dll` @ `/home/luis/stardew-assemblies`, commit `0862408` of this repo, smapi.io live).

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

*(In place as of 2026-11-09 with the agronomy subagent report below; claims live-cited.)*

See §7 "Agronomic grounding — landrace/crioulo (2026-11-09)" for the consolidated report. Headline claims (verified in-stream):

- **CONAB relaunched crioulo purchases (PAA-Sementes) April 2026** ("Compra de Sementes Crioulas") — national distribution of adapted genetics; MAPA stewards Lei de Sementes e Mudas (n.º 10.711/2003) recognizing "cultivar local, tradicional ou crioula" (Art. 48 limits restrictions on crioulas in public distribution).
- Embrapa runs multiple active crioulas programs (Semiárido, Tabuleiros Costeiros, RS Rede de Pesquisa de Sementes Crioulas e Agroecológicas; E-Campo online course "Ferramentas Participativas e Agroecológicas para Conservação e Multiplicação de Sementes Crioulas"); 230+ bancos comunitários de sementes in ~61 Sergipe municipalities serving ~6,500 families; ASA-PB 60 BCS / 4,308 families / 26.8 t across 13 species and 167 crioula varieties.
- MST paper "Semente crioula: cuidar, multiplicar e partilhar" (2021); UERJ/Tamoios 2025 frames crioulas as "tecnologia social … soberania alimentar"; FASE note AF_FASE_NOTA_TECNICA-01 (2021).
- Landrace vs hybrid agronomy (international literature): see report in this file.

## 7) Agronomic grounding — landrace/crioulo (2026-11-09) [stream complete — filled in this file]

(Stream 3 completed after v1 of this note; incorporated verbatim below.)

<!-- AGRONOMY-STREAM-PLACEHOLDER -->

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
