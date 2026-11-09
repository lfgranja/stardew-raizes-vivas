# Idea Research: Heirloom Seeds / Sementes Crioulas Pipeline

- **Slug**: heirloom-seeds-pipeline
- **Created**: 2026-11-09
- **Evidence confidence (overall)**: medium–high for market/technical lenses (primary sources + decompiled assemblies; Nexus stats snippet-derived labeled) ; agronomic lens being consolidated from dedicated multilingual stream (see Gaps)

Full raw report with citations lives in-repo at `research/2026-11-09-heirloom-seeds-pipeline.md` (commit `31924e5`); this file is the assessment-stage synthesis.

## Users & Demand

- **Latent, identity-driven demand — no explicit feature requests.** Players self-describe beloved "no-buy seeds / mixed-seeds preservation" playloops (r/StardewValleyMods permaculture megathread, 2026-03-15, 16 pts); "No Buying Seeds" challenge suite exists as a wiki category; Harvest Seeds family >1,300 combined endorsements (957 + ~354 maintained as of 2026-03). [source: reddit search 2026-11-09; https://stardewmodding.wiki.gg/wiki/Permaculture_Farming ; Nexus #11773/#30400 snippets] (confidence: high)
- **Silent-RNG seed drops are a top usability complaint** — players grind hundreds of Seed Maker cycles unable to tell if cultivation traits moved (r/SMAPI "Help using Compost, Pests, and Cultivation", 2022-01-17; r/SMAPI HybridCropEngine thread). Implication: any heirloom/seed-saving mechanic must be loud and discoverable, not opaque RNG. [source: reddit threads] (confidence: high)
- **Brazilian audience has real institutional vocabulary** (sementes crioulas, bancos comunitários de sementes, guardiões, agrobiodiversidade, soberania alimentar): Portaria/CONAB PAA-Sementes bought crioulas since 2003; **relaunched April 2026**; Embrapa programs; MST 2021 paper "Semente crioula: cuidar, multiplicar e partilhar"; r/StardewValleyBR (2025-11), r/StardewValleyModsBR (2025-11-24), Stardew Valley Brasil Discord. [source: Broto/AgFeed 2026-04-13; https://mst.org.br/download/semente-crioula-cuidar-multiplicar-e-partilhar/ ; reddit] (confidence: high)
- **No loud mainstream demand** ("wanted: mayor, pregnancy…" 2024-10-09 most-wanted list lacks seeds); YouTube/Twitch coverage of seed-saving: none found. The signal is behavioral/identity, not request-board. [source: reddit searches] (confidence: medium)
- Primary design users: cozy 25–45 players + co-op couples (project audience), educators/agroecology activists (PT/EN), plus the maintainer's goals (damage-free 80/20 slice). [source: OBJETIVO.md audience section] (confidence: high)

## Prior Art

- **Joja Seeds (Nexus #8746, ~621 endorsements)** — Joja "GMO" crops framed anti-seed-maker; ships "Heirloom Carrots/Potato/Corn" as Pierre's counterpoint. Proof the hybrid×heirloom contrast is legible to players; mechanics absent (no soil, no saving). Broken for 1.6 per current tags. [source: Nexus #8746 snippets; https://stardewmodding.wiki.gg/wiki/Modular_SVE] (confidence: high for design, low for stats)
- **Joseph's Seed Shop (Nexus #11676)** — Native North American heirloom seeds shop, year-gated, v2.0.0 1.6-compatible; no mechanics beyond stock. Closest "heirloom distribution" precedent; North-American provenance only. [Nexus #11676 snippets] (confidence: medium)
- **Heirloom Sunflowers (Nexus #12299, ~217)** — 7 open-pollinated varieties; "save seeds true-to-parent" UX; depends on Multi Yield Crops. Only true open-pollinated-theme mod. [Nexus #12299 snippets] (confidence: high)
- **Harvest Seeds – Continued (Nexus #30400, ~354)** — flat-chance harvest seed drop (default 10%, configurable, regrowable toggle), v1.0.10 (2026-03-22), actively maintained. The demand anchor for "harvest → seeds". [Nexus #30400 snippets] (confidence: high)
- **Multi Yield Crops (Nexus #6069, ~7.8k)** — framework: per-crop harvest rules yielding extra items incl. seeds; maintained (2026-03-15). Canonical 1.6 seed-drop mechanism. [Nexus #6069; github.com/ChroniclerCherry/stardew-valley-mods/blob/master/MultiYieldCrops/docs/README.md] (confidence: high)
- **Compost Pests and Cultivation (Nexus #4446, ~104)** — seed-maker cultivation XP + pests; only lineage/selection ancestor; abandoned at SDV 1.4; MP unsupported; PT translation exists but stale. [Nexus #4446; r/StardewValleyMods 2026-03-15] (confidence: high)
- **Cropgenics/Wildroot Chronicles (Nexus #48079)** — 112-trait breeding, named lineages, Korean/Chinese dev, active 2026. Breeding-collection identity, not agroecology. [Nexus #48079; r/StardewValleyMods 2026-09-29] (confidence: high)
- **Expanded Vanilla Crops (Nexus #6515)** — all forageable items seed-makable; Y2+ shops. Recover-from-harvest precedent. [Nexus #6515 snippets] (confidence: medium)
- **Internal prior art**: `specs/future-roadmap/briefings/heirloom-seeds.md` (this repo; Epic 6 US-06-01/02); `specs/014-soil-health-crop-quality` (harvest seam, tiers), `specs/005-soil-health-to-crop-growth` (Crop.newDay hook), `specs/013-mulch-soil-cover` (defensive slice), `specs/017-joja-agrochemical-tradeoff` (destructive slice; repo-scoped 0.1.0); `ModEntry.OnAssetRequested` registration pattern; `OrganicWasteValidator` (compostable categories); FR-009/A8 (`SoilHealthSyncMessage`) host-broadcast pattern. [source: repo tree] (confidence: high)
- **Portuguese translations exist** for #4446 (11714) and "Mix All The Seeds" PTBR (45892) — evidence of an active PTBR volunteer translation layer to plug into. [Nexus snippets] (confidence: medium)

## Market & Context

- **The niche is unclaimed in every searched language (EN/PT/ES/FR/DE)**: no mod implements *heirloom/landrace/crioulo seed → soil/growth/quality consequence → seed saving/selection* with Brazilian agroecology framing. High confidence (dedicated searches returned zero hits).
- Ecosystem anchor: SDV 1.6.15 stable / SMAPI 4.5.2 (requires 1.6.14+) / 1.6.16 potential pre-1.7 patch (dateless) / 1.7 ~2027 (no crop changes announced). Target versions of this repo are correct. [smapi.io live 2026-11-09; Steam 1.6.15; wiki snippets] (confidence: high)
- ReGen Valley (Nexus #39740, 3 endorsements, alpha 0.51) proves the per-tile soil premise sells to *someone* — and ships zero seed/lineage/rotation/heirloom. Living Roots is strictly larger in coherence; crioulo identity is unclaimed. [Nexus #39740; reddit] (confidence: high)
- PTBR distribution channels: pt.stardewvalleywiki.com, r/StardewValleyModsBR (2025-11-24), Stardew Valley Brasil Discord, PTBR translation teams — near-zero-cost adjacency for a Brazilian-framed mod; mainstream Western press unlikely to care. [web research 2026-11-09] (confidence: high)
- Institutional tailwinds: CONAB PAA-Sementes 2026 relaunch; Embrapa E-Campo crioulas course; PNAPO/Lei 10.711/2003 — agroecology vocabulary is institutionally current in Brazil, not a niche meme. [Broto/AgFeed; Embrapa project 216299] (confidence: high)
- **Cost of doing nothing**: roadmap stalls after Joja MVP (decision loop shipped but not the growth loop); Three Sisters/Green Manure/Rotation Journal/CSA all block on this pipeline; the Brazilian-agroecology differentiator (the only unclaimed niche) stays unbuilt while competitors (ReGen Valley, Wildroot) occupy adjacent space. [source: ROADMAP.md; OBJETIVO.md; web research] (confidence: high)

## Data & Constraints

- Build/test host: `GamePath=/home/luis/stardew-assemblies` required; build strict (TreatWarningsAsErrors); suite 1937/0/4; `-m:1` workaround for host SIGABRT (`.specify/bugs/msbuild-release-build-sigabrt`). [source: AGENTS.md; bugs dir] (confidence: high)
- Repo constraint: Constitution VI (multiplayer first-class); ModConstants single source; async-only; `.specify/scripts/atomic-commit-hook.sh` per edit; never `git add -A`. [source: constitution; AGENTS.md] (confidence: high)
- Asset constraint: no image-generation toolchain in agent host → procedural placeholder sprites (Python/Pillow) or vanilla-fallback Texture pointers; final art = maintainer decision; disclosure required. [source: stardew-pixel-art skill; assets inventory] (confidence: high)
- Distribution constraint: **Elena NPC unimplemented** → briefing's `LivingRoots_ElenaShop` path blocked; data-driven alternatives verified: `Data/Shops` entry (NPC-less), `Traveler` (traveling cart; confirmed shop ID), `SeedShop` (Pierre; `TargetField ["SeedShop","Items"]` edit pattern, condition-gated), vanilla Seed Maker (heirloom produce → seeds, 97.51%, 1–3; free). [decompiled ShopData; wiki snippets; §2 of research/2026-11-09-heirloom-seeds-pipeline.md] (confidence: high)
- MP verification: JunimoServer (sdvd-server-v1.4.0, preview recommended; E2E harness `:5123`) exists but needs Steam account + is never PR-gate; D1 remains environment/human-blocked regardless; farmhand C# paths can be unit-tested with stubs only (honesty: stubs ≠ live proof). [JunimoServer repo/docs] (confidence: high)
- Save compatibility: additive items only; no format changes; no migration needed; sparse-cache zero-persistence limitation known/pre-existing. [source: repo notes; measured-zero-persistence.md] (confidence: high)

## Evidence Against the Idea

- **Y1 content bloat risk.** The briefing asks for 3 crops + 3 seeds + 3 produce + 1 crop sheet + Elena shop — a week of someone's content work hidden inside a "small slice" framing. Honest appetite says 1 crop template proves the pipeline. [source: briefing; ASSESSMENT of costs] (confidence: high)
- **No measured player demand.** Zero requests anywhere; the "demand" is inferred from adjacent behaviors. If the mod ships heirlooms and nobody buys crioulo parsnips, nothing broke — but if the slice balloons before the core loop proves value, it fails the 80/20 test. [source: demand signals] (confidence: medium)
- **Asset quality bar.** Placeholder/invisible-art pets the project's cozy-game quality bar; hand-quality sprites can't be produced by the agent toolchain; shipping weak art undermines the "acochego seguro" pillar for zero gameplay return. [source: OBJETIVO.md pillars; asset verification] (confidence: high)
- **Scope gravity toward Three Sisters.** The 3 named consumer features will tempt bundling; mixing kills the slice (V. YAGNI). Any bundle = no MVP. [source: constitution V; briefing dependency list] (confidence: high)
- **Honesty traps**: (a) "+2 soil health per heirloom planting" is an abstraction, not agronomy — must be labeled as such in UI copy; (b) "heirloom produces more quality" is a game rule, not a nutritional claim; (c) nitrogen-fixation language is valid **only for legumes** — never for the parsnip slice; (d) "crioulo is better" framing must stay honest about trade-offs (slower growth; Pierre hybrids remain the efficient choice for some play styles — the mod must keep Pierre valid, echoing the agrochemical "never blocked, only costlier" pillar). [source: agronomic grounding stream; OBJETIVO.md pillars 2 & 5] (confidence: high)
- **ReGen Valley technically preempts "per-tile soil" as an idea**; differentiation rests on execution + coherence + framing, not novelty of the bar. If the slice is executed sloppily, the niche is theirs by default. [source: §3 research] (confidence: medium)
- **Seeds in MP farm economics**: unlimited crioulo stock could flood multiplayer co-op economies; `AvailableStockLimit Player` mitigates; still needs honest tuning and a refusal story. [source: ShopItemData decompile; design analysis] (confidence: medium)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: agronomic stream verdict on crioulo nutritional/yield/stability evidence — which claims are defensible for in-game text (see research §6/§7)? Stream was in flight at time of writing; the assessment synthesis above will be finalized with it in this file.]
- [NEEDS CLARIFICATION: exact slice — 1 vs 3 crops; seed-saving as built-in seam vs documented follow-up.]
- [NEEDS CLARIFICATION: distribution — Traveler-only vs Traveler + SeedShop condition entry vs pure seed-saving with no shop at all (initial seeds source problem).]
- [NEEDS CLARIFICATION: release scope — repo-scoped 0.2.0 (precedent 017) or no release/mmanifest bump this cycle; tag remains maintainer-only.]
- [NEEDS CLARIFICATION: asset strategy — procedural placeholders (disclosed) vs reuse vanilla parsnip crop row (identical look, different name/timing) vs defer all art to maintainer.]

## Sources

- In-repo raw report: `research/2026-11-09-heirloom-seeds-pipeline.md` (commit `31924e5`; agronomy section filled when stream 3 completes — tracked in-file).
- Nexus (403; snippet-derived): #39740 ReGen Valley; #8746 Joja Seeds; #12299 Heirloom Sunflowers; #11676 Joseph's Seed Shop; #30400 Harvest Seeds–Continued; #6069 Multi Yield Crops; #4446 Compost Pests and Cultivation; #48079 Cropgenics; #3753 SVE; #3012/#22311/#14236/#26712/#6515.
- Wiki/docs (403 direct; snippets + mirrors + decompiled models): Modding:Crop_data; Modding:Objects; Modding:Shops; Modding:Items; Modding:Migrate_to_Stardew_Valley_1.6(.16/.7); Tutorial:_Adding_a_New_Crop (wiki.gg)
- Decompiled @ `/home/luis/stardew-assemblies/` (ilspycmd 11.2.0.10050, 2026-11-09): `CropData`, `ObjectData`, `ShopData`, `ShopItemData`, `HoeDirt.plant(string, Farmer, bool)`, `Crop.harvest`, `ShopBuilder.GetShopStock`
- https://smapi.io/ (live, 2026-11-09) ; https://github.com/Pathoschild/SMAPI/releases
- https://github.com/stardew-valley-dedicated-server/server ; https://docs.junimoserver.com/developers/testing/test-harness.html
- https://mst.org.br/download/semente-crioula-cuidar-multiplicar-e-partilhar/ ; https://www.embrapa.br/ (project 216299) ; Broto/AgFeed 2026-04-13 (CONAB PAA-Sementes relaunch)
- https://stardewmodding.wiki.gg/wiki/Permaculture_Farming ; https://stardewmodding.wiki.gg/wiki/No_Buying_Seeds
- r/StardewValleyMods (2026-03-15, 2026-09-29); r/StardewValleyBR; r/StardewValleyModsBR (2025-11-24); discord.me/stardewvalleybrasil
