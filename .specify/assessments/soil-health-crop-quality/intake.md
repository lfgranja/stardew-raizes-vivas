# Idea Intake: Soil Health → Crop Quality

- **Slug**: soil-health-crop-quality
- **Created**: 2026-11-01
- **Source**: codebase pointer — `specs/future-roadmap/briefings/soil-tier-outcomes-harvest-feedback.md` (briefing, not implemented; explicitly deferred to `/speckit.specify`); also `ROADMAP.md` §"Core Concept" promises (Poor = no stars, Living = silver/gold/iridium + extra harvest) and `specs/005-soil-health-to-crop-growth/` (growth driver shipped, quality side not).
- **Type**: new-capability

## Idea (as captured)

> "Soil Health → Crop Quality" — a segunda metade da promessa central (Living Soil 71–100% finalmente paga na colheita). Reusa persistência + overlay existentes; fecha o loop build → protect → harvest.

The briefing inventories everything the three ROADMAP tier promises impose on assets, data entries, hooks, i18n and tests. It recommends implementing tier feedback as transient events (`TemporaryAnimatedSprite` + floating text on `Display.Rendered`), driven by `HealthCategory` bands — **not** as a permanent per-frame overlay and **not** via data entries. It explicitly states that none of the promised mechanics is achievable with data alone: `Data/Crops.DaysInPhase` and `Object` quality are global/per-instance, not per-tile, so faster growth and quality downgrade **require runtime code** (Harmony or public-field manipulation in `DayStarted`/harvest), and chance rolls must be host-authoritative (Principle VI). Nothing is implemented; promotion requires `/speckit.specify`.

Key anchors: `ROADMAP.md:9-11` (tier promises) · `specs/001-soil-health-visualization/briefings/overlay-procedural.md` · `specs/004-playable-compost-loop/spec.md:57,228` · `LivingRoots/Constants.cs:31-34` · `LivingRoots/Domain/Visualization/HealthCategory.cs` · `LivingRoots/Services/CompostApplicationService.cs:61-75`.

## Restated

Implement the quality half of the soil-health promise: per-tile soil health (already persisted 0–100, farm-keyed) must affect harvest outcomes — Poor soil yields no-star produce, Living soil yields silver/gold/iridium quality plus a chance of extra harvest — reusing the existing persistence and overlay rather than adding new data entries. This closes the loop build → protect (mulch) → harvest and is the only remaining item that makes Living Soil visibly pay off at harvest.

## Origin & Context

- **Raised by**: maintainer (lfgranja), via ROADMAP 80/20 ordering and GDD §2.1/§3.1 (Living = faster growth + extra harvest + silver/gold/iridium). Briefing authored as explicit deferral to `/speckit.specify`.
- **Trigger**: Soil-health growth driver shipped (spec 005, hook on `Crop.newDay`); mulch shipped (spec 013). The defensive side now exists, so the quality side becomes the highest-impact remaining item. Constitution Principle IV (TDD) and Principle VI (multiplayer) gate any implementation. ROADMAP 80/20 step 4 (updated 2026-11-01) names this as the next 80/20.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: which band scale wins — ROADMAP 0-30/31-70/71-100 vs spec 001 half-open [0,34)/[34,67)/[67,100]? Reconciliation is pending (spec 005 T117–T119).]
- [NEEDS CLARIFICATION: growth speed multiplier values and extra-harvest probability numbers — balance, not specified anywhere.]
- [NEEDS CLARIFICATION: Harmony vs. public-field manipulation in `DayStarted`/harvest for growth/quality hooks — feasibility and risk.]
- [NEEDS CLARIFICATION: how "no stars" on Poor composes with vanilla quality roll (Farming level, fertilizer) without breaking "Medium = vanilla" baseline.]
- [NEEDS CLARIFICATION: pest system for Poor soil (high pests) — separate feature (`briefings/pest-system-mvp.md`), no surface in repo.]
- [NEEDS CLARIFICATION: "new interactions" unlocked by Living soil — vague ROADMAP promise, needs its own spec.]
- [NEEDS CLARIFICATION: host-authoritative chance rolls under Principle VI — divergence rule to be stated in spec.]
- [NEEDS CLARIFICATION: banding mismatch reconciliation must land before or with this feature.]
- [NEEDS CLARIFICATION: whether a persistent per-tile tier marker (overlay per frame) is approved, given the 1000-tile degradation threshold (FR-009).]
- [NEEDS CLARIFICATION: latent `CollectCompost` partial-success bug (risk #1 in briefing) must be fixed before any output increase.]
- [NEEDS CLARIFICATION: CVD-safe feedback channel (color alone is insufficient per GAG) — non-chromatic cue required.]
- [NEEDS CLARIFICATION: no assessment exists yet for this idea — `/speckit.assess.research` should follow.]
- [NEEDS CLARIFICATION: no `/speckit.specify` has run; briefing explicitly says "do not promote to specs/ without /speckit.specify".]