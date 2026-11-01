# Idea Research: Soil Health → Crop Quality

- **Slug**: soil-health-crop-quality
- **Created**: 2026-11-01
- **Evidence confidence (overall)**: medium (strong internal code evidence + official wiki SMAPI/crop data; runtime hook feasibility remains unverified without a build with GamePath)

## Users & Demand

- **Stated player desire is real and quantified**: Nexus "Crop Quality" mod (aedenthorn, v0.2.0, 2026-06-04) — 13 endorsements — exists solely to *display* harvest quality and tweak quality chances; its description confirms players cannot see quality before harvest and want control over it. [source: https://www.nexusmods.com/stardewvalley/mods/47141] (confidence: high, cited)
- **Vanilla quality is opaque and random**: quality is determined at harvest from day-played + tile coords + Farming level + fertilizer; same crop harvested on different days yields different quality. Players actively game this (potato extra-harvest math, quality-fertilizer trade-offs). [source: https://www.superjumpmagazine.com/stardew-valley-on-crops-and-randomness; https://stardewvalleywiki.com/Crops] (confidence: high, cited)
- **Soil-health-as-quality-driver is an established competitor premise**: ReGen Valley (Nexus, Nov 2025) ships "farm-wide quality/yield at 75+/90+ average soil health" end-to-end. This is the bar to beat on coherence, not novelty. [source: ROADMAP.md "Reference point (external)"; research/2026-10-roadmap-feasibility-and-agronomy.md §B] (confidence: high, cited)
- **Demand for "soil matters" is concrete in this repo's own roadmap**: GDD §3.1 promises "Living = faster growth, chance of extra harvests, higher quality (silver/gold/iridium)" and ROADMAP.md:9–11 lists it as a core promise. Zero coverage exists today (verified: grep for growth/quality/harvest in `LivingRoots/` returns nothing except spec 005 growth hook). [source: internal code audit, ROADMAP.md, GDD] (confidence: high, cited)
- **Observed vs. stated**: stated demand is strong (roadmap, GDD, competitor). Observed in-game demand cannot be measured — 0 in-game runs to date (ROADMAP "In-game runs: 0"). [source: ROADMAP.md] (confidence: high, cited)

## Prior Art

- **Internal — spec 005 (Soil Health → Crop Growth)**: shipped the growth driver via `ModController` hook on `Crop.newDay` with per-tile persistence + overlay reuse. This is the direct predecessor and the pattern to mirror for the quality side. 143/146 tasks done (3 doc-only). [source: specs/005-soil-health-to-crop-growth/tasks.md; ROADMAP.md] (confidence: high, cited)
- **Internal — spec 004 (Playable Compost Loop)**: established the `AssetRequested` + `e.Edit` C# pattern, `Item.modData` progress persistence, host-authoritative `TryApplyCompostWithLock`, `Game1.showRedMessage` + `ITranslationHelper` refusal messages, and the `TemporaryAnimatedSprite` feedback model (`+15` floating text, `alphaFade 0.02`). All reusable for tier feedback. [source: specs/004-playable-compost-loop/spec.md; LivingRoots/Services/CompostApplicationService.cs:61-78] (confidence: high, cited)
- **Internal — spec 001 (Visualization)**: `HealthCategory` bands `[0,34)/[34,67)/[67,100]` (half-open), overlay tint `#B91C1C`/`#D97706`/`#15803D` at opacity 0.5, accessibility patterns ≥0.7, 1000-tile degradation threshold. Tier outcomes must compose with, not replace, this overlay. [source: LivingRoots/Domain/Visualization/HealthCategory.cs; LivingRoots/Services/Visualization/OverlayRenderer.cs] (confidence: high, cited)
- **External — "Crops grow in one day" (Nexus, nastapolka, 2026)**: patches growth via SMAPI (not Harmony annotations) and rewrites data on save-load, not game launch. Demonstrates that growth-speed mods are feasible but fragile with other growth mods. [source: https://www.nexusmods.com/stardewvalley/mods/40070] (confidence: medium, cited)
- **External — "Seasonal Crop Scaling" (Nexus, 2026)**: stretches growth/regrow times with configurable multipliers; warns that if another mod edits crop growth data, load order matters. Confirms data-edit approach is order-sensitive and global. [source: https://www.nexusmods.com/stardewvalley/mods/46185] (confidence: medium, cited)
- **External — "Crop Quality" (Nexus, aedenthorn, 2026)**: displays quality stars pre-harvest and tweaks quality chances via config; uses Harmony. Shows demand for quality visibility and that quality is rolled, not stored, until harvest. [source: https://www.nexusmods.com/stardewvalley/mods/47141] (confidence: medium, cited)
- **External — SMAPI Harmony guidance**: Harmony is a last resort; SMAPI rewrites mods for cross-platform compatibility but cannot rewrite annotation-based patches; patches must handle errors and default to original code. [source: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony; https://wiki.biligame.com/stardewvalley/模组:制作指南/APIs/Harmony] (confidence: high, cited)
- **Prior attempt (internal, spec 005 convergence)**: T114 registered a real `Crop.harvest` intercept calling `ModController.ApplyHarvestModifiers` — but it was defined-but-uncalled at convergence time; T110 marked complete without evidence. Shows the harvest hook is the hard part and was previously over-claimed. [source: specs/005-soil-health-to-crop-growth/tasks.md Phase 13] (confidence: high, cited)

## Market & Context

- **Competitor baseline (ReGen Valley)** already delivers per-tile soil health + cover crops + faster growth at 70+/90+ + farm-wide quality/yield at 75+/90+. Our differentiation is agroecological grounding, compounding compost loop, educational framing, and multiplayer correctness — not the mechanic itself. [source: ROADMAP.md "Reference point (external)"] (confidence: high, cited)
- **Cost of doing nothing**: without quality/harvest payoff, Living Soil (71–100%) only speeds growth; the mod's central promise ("Living = faster growth, chance of extra harvests, higher quality") is 1/3 delivered. Players on Living soil see no harvest benefit beyond speed — the loop build → protect → harvest stays open. [source: ROADMAP.md, GDD §3.1, briefing §1] (confidence: high, cited)
- **Alternative users cope with today**: vanilla quality is a pure random roll (≈46% regular / 33% silver / 20% gold at Farming 10, with fertilizer modifiers); players optimize via quality fertilizer + Farming level, not soil. [source: https://www.superjumpmagazine.com/stardew-valley-on-crops-and-randomness; https://stardewvalleywiki.com/Crops] (confidence: high, cited)
- **Trend**: SMAPI 4.5.2 (Mar 2026) stable on .NET 6; Stardew 1.6.16 in preparation (event overhaul, Id fields in data models, internal caching). A quality-hook mod built now must be re-verified on 1.6.16. [source: https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6.16; https://smapi.io] (confidence: high, cited)

## Data & Constraints

- **Quality is rolled at harvest, not stored**: `Crop.harvest(int xTile, int yTile, HoeDirt soil, ...)` computes quality from a `Random(xTile*7 + yTile*11 + DaysPlayed + uniqueID)` plus Farming level and fertilizer (368/369/919 → num2 1/2/3), with clamps `HarvestMinQuality`/`HarvestMaxQuality` applied *after* the roll. Iridium only possible with Deluxe Fertilizer (num2≥3). [source: briefing §5 citing decompiled `Crop.harvest`; https://stardewvalleywiki.com/Crops; https://stardewvalleywiki.com/Modding:Crop_data] (confidence: high, cited)
- **`ObjectData` has no `Quality` field** (fields: Name, DisplayName, Description, Type, Category, Price, Texture, SpriteIndex, ColorOverlayFromNextIndex, Edibility, IsDrink, Buffs, GeodeDrops, ArtifactSpotChances, ContextTags, CanBeGivenAsGift, CanBeTrashed, ExcludeFrom*, CustomFields). Quality is an instance property of `StardewValley.Object` at runtime (0=none,1=silver,2=gold,3=iridium; tags auto-added). Therefore "no stars on Poor" **cannot** come from data — it requires runtime interception. [source: briefing §4.2 citing `Modding:Objects` oldid 193966; `Modding:Context tags` oldid 193554] (confidence: high, cited)
- **`Data/Crops` fields are global per seed**: `DaysInPhase` (int[]), `ExtraHarvestChance` (double 0–0.9, geometric series), `HarvestMin/MaxQuality` (int?), `CustomFields` (Dictionary<string,string>). Editing via `e.Edit` affects every tile planting that seed — violates "Medium = vanilla". `CustomFields` is the only extension vector (game ignores it; mod reads at runtime). [source: briefing §4.1 citing `Modding:Crop data` oldid 186411] (confidence: high, cited)
- **No SMAPI API for per-tile growth speed**: growth is deterministic from `DaysInPhase` data + `currentPhase`/`dayOfCurrentPhase` fields. Per-tile speed requires either Harmony on the daily growth path or a `DayStarted` pass advancing public fields. `IsPaddyCrop` (accelerated growth + auto-water) and `RegrowDays` are vanilla modifiers that must be composed with, not replaced. [source: briefing §5; research/2026-10-roadmap-feasibility-and-agronomy.md §A.2] (confidence: high, cited)
- **Extra harvest**: `ExtraHarvestChance` 0–0.9, geometric roll (expected value 1/(1−p)−1). Per-tier conditional roll requires runtime; probability numbers are a balance decision, not specified anywhere. [source: https://stardewvalleywiki.com/Modding:Crop_data] (confidence: high, cited)
- **Platform**: PC only (Linux/macOS/Windows); Android SMAPI is experimental/community. Multiplayer is host-authoritative; farmhands need identical mods. [source: https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Android; https://arcadelords.net/stardew-valley-multiplayer-the-complete-guide-to-co-op-farming-in-2026] (confidence: high, cited)
- **Net fields / multiplayer**: farm state is shared via net fields; `Context.IsMainPlayer`/`IsMasterGame` identify host; `Helper.Multiplayer.SendMessage`/`ModMessageReceived` for host-mediated sync; `PerScreen<T>` for split-screen. [source: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer; https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Utilities] (confidence: high, cited)
- **Constitution constraints**: Principle I (DDD, interface in `Domain/`, impl in `Services/`); II (security, bounds 0–100, overflow checking); III (async-only, no `.Result`/`.Wait()`); IV (TDD, RED before GREEN); VI (multiplayer first-class, host-authoritative, first-wins, no last-write-wins, Gate 3 with second player). [source: .specify/memory/constitution.md] (confidence: high, cited)
- **Performance**: overlay budget 16.67 ms/frame; 1000-tile degradation disables only accessibility patterns (colors/opacity remain); tier feedback must be event-driven (TransientAnimatedSprite) to be immune to the threshold. [source: briefing §2; specs/001] (confidence: high, cited)

## Evidence Against the Idea

- **High invasiveness**: every gameplay promise (faster growth, no stars, extra harvest) requires runtime interception of game internals; no data-only path exists. This is the single largest technical risk and contradicts the mod's "vanilla-first, no Harmony unless necessary" posture (Constitution V / SMAPI best practices). [source: briefing §5; https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony] (confidence: high, cited)
- **"Medium = vanilla" is a hard regression constraint**: any interception must leave tier 2 (34–67 or 31–70) byte-identical to vanilla, including interaction with `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`, and `quality_fertilizer_item`. A naive multiplier double-dips and breaks the baseline. [source: briefing §5 risk #4; research/2026-10-roadmap-feasibility-and-agronomy.md §A.2] (confidence: high, cited)
- **Band-scale ambiguity**: ROADMAP says 0-30/31-70/71-100; spec 001 implemented [0,34)/[34,67)/[67,100]. Unresolved; any spec must pick one and retire the other. Three doc-only convergence tasks (spec 005 T117–T119) are open. [source: briefing §1 "Aviso de reconciliação"; specs/005/tasks.md Phase 14] (confidence: high, cited)
- **CVD-unsafe default palette**: measured ΔE₀₀ deutan Poor/Healthy 8.4, protan 9.6, achromatopsia Healthy/Unknown 1.2 with inverted lightness (Moderate L 0.653 > Healthy 0.542). Tier feedback relying on color alone violates GAG ("no essential information conveyed by a fixed colour alone"). Non-chromatic channel (sprite shape / floating text) is mandatory; default palette must not move (CHK013). [source: briefing §10 risk #2 citing color-schemes-cvd.md §6.1] (confidence: high, cited)
- **Latent duplication bug**: `CollectCompost` has `outputCount` hardcoded to 1 but the loop uses `TryAddItem` without partial-success rollback; any future multi-item output would let the player keep the item AND re-collect. Must be fixed before any output increase. [source: briefing §10 risk #1; LivingRoots/Services/CompostingBinService.cs:77-89] (confidence: high, cited)
- **Pest system does not exist**: "high chance of pests on Poor" has zero surface in the repo (`briefings/pest-system-mvp.md` is the owner). Shipping the promise requires a separate feature. [source: briefing §1; ROADMAP.md "Poor = pests ❌"] (confidence: high, cited)
- **"New interactions" is vague**: ROADMAP promises "unlocks new interactions" for Living soil with no concrete definition. Risk of scope creep. [source: ROADMAP.md:11; briefing §1/§11] (confidence: high, cited)
- **0 in-game runs**: no SMAPI session has ever executed this mod; Gate 3 evidence is pending. Runtime hooks (Harmony, field names) are unverified against the actual 1.6.3/1.6.14 binary. [source: ROADMAP.md "In-game runs: 0"] (confidence: high, cited)
- **Multiplayer divergence**: chance rolls on clients would fork farm state, violating Principle VI. Must be host-authoritative with explicit divergence rule and Gate 3 with a second player. [source: .specify/memory/constitution.md Principle VI; briefing §7] (confidence: high, cited)
- **Prior over-claim history**: spec 005 T110/T113/T115 were marked complete without evidence (harvest hook defined-but-uncalled, RED test files not created). Shows risk of premature completion claims on this exact hook. [source: specs/005-soil-health-to-crop-growth/tasks.md Phases 13–14] (confidence: high, cited)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: band scale reconciliation (ROADMAP 0-30/31-70/71-100 vs spec 001 half-open) — must be settled before or with this feature.]
- [NEEDS CLARIFICATION: numerical balance — growth multiplier, extra-harvest probability, quality odds per tier. Not specified anywhere.]
- [NEEDS CLARIFICATION: Harmony vs. public-field `DayStarted` pass for growth/quality hooks — verify method names (`Crop.dayUpdate`, `HoeDirt.dayUpdate`, `Crop.harvest`, `Object.Quality`) against the 1.6.3/1.6.14 binary with GamePath.]
- [NEEDS CLARIFICATION: how "no stars" on Poor composes with vanilla quality roll without breaking "Medium = vanilla".]
- [NEEDS CLARIFICATION: pest system scope — separate feature or bundled?]
- [NEEDS CLARIFICATION: definition of "new interactions" for Living soil.]
- [NEEDS CLARIFICATION: persistent per-tile tier marker vs. transient feedback only (1000-tile degradation threshold).]
- [NEEDS CLARIFICATION: whether `CollectCompost` partial-success bug is fixed before this feature ships.]
- [NEEDS CLARIFICATION: no assessment existed before this intake; research confidence is medium pending a build with GamePath.]
- [NEEDS CLARIFICATION: no `/speckit.specify` has run; briefing explicitly says "do not promote to specs/ without /speckit.specify".]

## Sources

- `.specify/assessments/soil-health-crop-quality/intake.md` (host: local, policy: n/a) — origin and first-glance unknowns
- `specs/future-roadmap/briefings/soil-tier-outcomes-harvest-feedback.md` (host: local) — primary asset/hook/i18n/risk inventory
- `ROADMAP.md` (host: local) — tier promises, status annotation, 80/20 ordering, competitor reference
- `docs/GDD-living-roots.md` (host: local) — verbs, win/loss, economy, tiers, progression
- `.specify/memory/constitution.md` v2.0.0 (host: local) — Principles I–VI, Gate 3
- `specs/005-soil-health-to-crop-growth/tasks.md` (host: local) — predecessor, convergence over-claims
- `specs/004-playable-compost-loop/spec.md` (host: local) — compost loop, AssetRequested pattern, multiplayer lock
- `specs/001-soil-health-visualization/` (host: local) — HealthCategory bands, overlay, accessibility
- `LivingRoots/` source (host: local) — verified absence of growth/quality/harvest references; `HealthCategory.cs`, `OverlayRenderer.cs`, `CompostApplicationService.cs`, `CompostingBinService.cs`
- https://stardewvalleywiki.com/Modding:Crop_data (host: stardewvalleywiki.com, policy: unrecognized — fetched, read-only) — Crop data schema, ExtraHarvestChance, HarvestMin/MaxQuality
- https://stardewvalleywiki.com/Crops (host: stardewvalleywiki.com) — quality roll, fertilizer effects
- https://stardewvalleywiki.com/Modding:Objects (host: stardewvalleywiki.com) — ObjectData has no Quality field
- https://stardewvalleywiki.com/Modding:Context_tags (host: stardewvalleywiki.com) — quality tags
- https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony (host: stardewvalleywiki.com) — Harmony as last resort, SMAPI rewriting
- https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer (host: stardewvalleywiki.com) — host-authoritative, SendMessage/ModMessageReceived
- https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Utilities (host: stardewvalleywiki.com) — Context.IsMainPlayer, PerScreen
- https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6.16 (host: stardewvalleywiki.com) — upcoming event overhaul, Id fields, caching
- https://smapi.io (host: smapi.io) — SMAPI 4.5.2 (Mar 2026), .NET 6
- https://www.nexusmods.com/stardewvalley/mods/47141 (host: nexusmods.com, policy: unrecognized — fetched, read-only) — Crop Quality mod (display + tweak chances)
- https://www.nexusmods.com/stardewvalley/mods/40070 (host: nexusmods.com) — Crops grow in one day (SMAPI patches, save-load rewrite)
- https://www.nexusmods.com/stardewvalley/mods/46185 (host: nexusmods.com) — Seasonal Crop Scaling (global data edits, order-sensitive)
- https://www.superjumpmagazine.com/stardew-valley-on-crops-and-randomness (host: superjumpmagazine.com, policy: unrecognized — fetched, read-only) — vanilla quality randomness analysis
- https://github.com/WeDias/StardewValley; https://github.com/tiwe0/StardewValley; https://github.com/lowingly/StardewValley163 (host: github.com, policy: allowlisted) — decompiled 1.5.6/1.6.3 source for Crop.harvest verification
- research/2026-10-roadmap-feasibility-and-agronomy.md (host: local) — SMAPI 1.6 API, competitor baseline, agronomic grounding