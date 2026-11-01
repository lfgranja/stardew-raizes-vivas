# Problem Definition: Soil Health → Crop Quality

- **Slug**: soil-health-crop-quality
- **Created**: 2026-11-01
- **Inputs used**: intake.md (yes) | research.md (yes) | user input only (no)

## Problem Statement

The mod promises that Living Soil (71–100%) yields "faster growth, chance of extra harvests, higher quality (silver/gold/iridium)" and that Poor Soil (0–30%) yields "lower quality produce (no stars)" (ROADMAP.md:9–11, GDD §3.1), but the only soil-driven gameplay effect shipped today is growth speed (spec 005). Quality, extra harvest, and growth speed beyond the growth driver are 100% vanilla and identical across all soil tiers, so the central promise is two-thirds undelivered and players on Living soil see no harvest benefit beyond speed. The problem is how to make soil health a real, observable, per-tile determinant of harvest outcome without breaking the vanilla baseline on Medium soil, without forking farm state in multiplayer, and without violating the mod's vanilla-first, low-invasiveness posture.

## Affected Users & Stakeholders

- **Users — cozy players (primary, 25–45)**: experience a soil-health number that does not change what they harvest; the "Living Soil" tier is a visual label with no economic payoff. [source: ROADMAP.md "Core Concept"; GDD §2.1/§2.3]
- **Users — co-op couples (founder case: maintainer + spouse)**: any per-tile effect must be farm-keyed and host-authoritative; client-side rolls would fork the farm. [source: constitution Principle VI; GDD §3.2]
- **Users — modders/contributors**: expect DDD layering, TDD, FsCheck invariants, and a documented divergence rule; an undocumented Harmony hook breaks reviewability. [source: AGENTS.md; constitution I, IV]
- **Stakeholders — maintainer (lfgranja)**: owns scope, Gate 3 verification, and the "no Harmony unless necessary" posture; decides band scale and balance numbers.
- **Stakeholders — community (Nexus/ModDrop players)**: competitor ReGen Valley already ships quality/yield at high average soil health; expectation of parity on coherence. [source: ROADMAP.md "Reference point"]

## Goals

- Make Living Soil (71–100% / [67,100]) produce measurably better harvests than Medium soil (silver/gold/iridium quality + chance of extra harvest) in a way a player can observe and attribute to soil.
- Make Poor Soil (0–30% / [0,34)) produce measurably worse harvests (no stars) without making Medium soil worse than vanilla.
- Keep Medium soil byte-identical to vanilla (regression control), including composition with `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`, and `quality_fertilizer_item`.
- Preserve the existing per-tile persistence and overlay (reuse, do not duplicate state).
- Keep chance rolls host-authoritative and deterministic in divergence handling (Principle VI).
- Provide non-color-only feedback so tiers are distinguishable for CVD users (GAG compliance).
- Keep the feature unit-testable for logic (invariants on bands and outcomes, not on pixels/palette).

## Non-Goals

- Numerical balance (growth multiplier, extra-harvest probability, quality odds) — deferred to `/speckit.specify`.
- Pest system for Poor soil — separate feature (`briefings/pest-system-mvp.md`), no surface in repo.
- "New interactions" unlocked by Living soil — vague promise; requires its own spec.
- A persistent per-tile tier marker overlay (would inherit the 1000-tile degradation threshold and the per-frame budget) — decision deferred; transient feedback only is the briefing's recommendation.
- Changing the default color palette (CHK013) or reconciling the band scale (ROADMAP 0-30/31-70/71-100 vs spec 001 half-open) — decisions of spec, not of this problem definition.
- Content Patcher / PNG assets — repo uses `AssetRequested` + procedural sprites.
- Console/mobile, localization beyond PT/EN, romance Elena, Joja Fertilizer, Heirloom pipeline, Mid/End-game systems — out of scope per GDD §6.2.
- Fixing the latent `CollectCompost` partial-success duplication bug — required prerequisite, tracked separately, but not part of this problem.

## Success Metrics

- **M1 (measurable, in-game)**: A fresh save with 3 tiles at 0%, ~50%, and ~90% soil health, planted with the same seed in the same season, yields measurably different quality distributions and harvest counts at harvest — observable without reading a number. (baseline: today identical across all three tiers)
- **M2 (measurable, test)**: Unit tests assert that any soil value in `[0,34)` produces `Quality == 0` at harvest, and that any value in `[34,67)` produces a distribution statistically indistinguishable from vanilla (within test tolerance) for the same seed/day/fertilizer. (baseline: no such tests exist)
- **M3 (measurable, test)**: Host-authoritative roll tests pass with a simulated second player; no client-side roll occurs. (baseline: no multiplayer roll tests exist)
- **M4 (qualitative)**: Playtest (casal, 1 estação) reports the soil tier "mattered" at harvest without consulting a number. (baseline: not measured — 0 in-game runs)
- **M5 (measurable, perf)**: Tier feedback (transient sprites) does not consume the 16.67 ms/frame budget or trigger the 1000-tile degradation path. (baseline: overlay already budgeted; feedback is event-driven)

## Cost of Inaction

If this is never built, the mod's headline promise ("Living Soil = faster growth, chance of extra harvests, higher quality") remains one-third delivered: soil health drives growth speed only, and the player's only reason to compost is speed, not quality. The loop build → protect (mulch) → harvest stays open, and the competitor ReGen Valley keeps the "quality at high soil health" differentiator. The mod risks being perceived as a diagnostic overlay with a compost buff rather than an agroecological system, and the 31 design briefings for unbuilt features lose their anchor (soil health as the currency of progression). Additionally, the band-scale ambiguity and the CVD-unsafe palette remain unaddressed, and the harvest hook's prior over-claim history (spec 005 T110/T113/T115) erodes trust in completion claims.

## Open Questions

- [NEEDS CLARIFICATION: band scale reconciliation — ROADMAP 0-30/31-70/71-100 vs spec 001 half-open [0,34)/[34,67)/[67,100] — must be settled before or with this feature.]
- [NEEDS CLARIFICATION: numerical balance (growth multiplier, extra-harvest probability, quality odds per tier) — not specified anywhere.]
- [NEEDS CLARIFICATION: Harmony vs. public-field `DayStarted` pass for growth/quality hooks — verify method names (`Crop.dayUpdate`, `HoeDirt.dayUpdate`, `Crop.harvest`, `Object.Quality`) against the 1.6.3/1.6.14 binary with GamePath.]
- [NEEDS CLARIFICATION: composition rule for "no stars" on Poor with vanilla quality roll (Farming level, fertilizer) without breaking "Medium = vanilla".]
- [NEEDS CLARIFICATION: pest system scope — separate feature or bundled?]
- [NEEDS CLARIFICATION: definition of "new interactions" unlocked by Living soil.]
- [NEEDS CLARIFICATION: persistent per-tile tier marker vs. transient feedback only (1000-tile degradation threshold).]
- [NEEDS CLARIFICATION: whether `CollectCompost` partial-success bug is fixed before this feature ships.]
- [NEEDS CLARIFICATION: non-chromatic feedback channel (sprite shape / floating text) — required for CVD compliance.]
- [NEEDS CLARIFICATION: no `/speckit.specify` has run; briefing explicitly says "do not promote to specs/ without /speckit.specify".]