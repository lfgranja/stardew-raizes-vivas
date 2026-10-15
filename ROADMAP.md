# ROADMAP.md - Stardew Valley: Living Roots

This document outlines the development roadmap for the "Living Roots" mod, detailing the progression of features from early to late game, based on agroecological principles.

> **STATUS ANNOTATION — verified 2026-10-15**
> Every roadmap item below is marked with what actually exists in code, verified by build, test run and
> source audit — not by spec claims. Legend:
> ✅ implemented & verified (build + tests green) · 🟡 partial / inert · ❌ not implemented ·
> ⚠️ implemented but **has no effect on gameplay**.
> The original design text is preserved unchanged; annotations were added, nothing was deleted.
> **Deep-dive evidence:** `research/smapi-1.6-crop-and-soil-api-2026.md`,
> `research/comparable-soil-farming-mods-2026.md`, `research/agroecology-feature-grounding-2026.md`.

## Where we actually are (verified, not inferred)

| Metric | Value | How verified |
|---|---|---|
| Build | **0 errors, 0 warnings** | `dotnet build -c Release` on `dev` @ `0e7b468` |
| Test suite | **1063 passed / 0 failed** | `dotnet test -c Release --no-build` |
| Format gate | clean | `dotnet format --verify-no-changes` |
| Specs closed | 002 (38/38); 004 (65/67) | `specs/*/tasks.md` |
| Roadmap items shipped | **2 of 21** | source audit, this file |
| **In-game runs** | **0 — never executed** | no SMAPI log, no save with mod data |
| Proprietary DLLs tracked in git | **7, pushed to `origin/dev`** | `git ls-files .gamepath` |

**The one-sentence truth:** the mod can *measure* soil, *display* it, and *change* it with compost —
but **soil health currently changes nothing about how the farm plays**. That single gap is the
difference between a diagnostic overlay and a game.

---

## Core Concept: The Soil Health Bar

All mod mechanics will revolve around "Soil Health," a new property for each tillable tile on the farm (visible when using a hoe or hovering the mouse, ranging from 0% to 100%).

*   **Poor Soil (0-30%):** Slow growth, high chance of pests, lower quality produce (no stars).
*   **Medium Soil (31-70%):** Similar to the base game.
*   **Living Soil (71-100%):** Faster growth, chance of extra harvests, higher quality (silver/gold/iridium), and unlocks new interactions.

**What DESTROYS Soil Health:**

*   Leaving soil tilled and bare (without cover or plants).
*   Monoculture (planting the same crop repeatedly).
*   Using chemical fertilizers (new "Joja Fertilizer" items, which provide an initial boost but cost 5% soil health).

**What BUILDS Soil Health:**

*   Composting and organic fertilizer application.
*   Crop Rotation.
*   Polyculture / Companion Planting.
*   Green Manure.
*   Soil Cover (Mulch).
*   Animal Management.

**Status of the core concept:**

| Promise | Status | Reality |
|---|---|---|
| Value per tile, 0-100, persisted | ✅ | `SoilHealthService`, sparse save, DoS-bounded |
| Visible via hoe / hover | ✅ | overlay + tooltip + hoe feedback (spec 001) |
| Poor = slow growth | ❌ | **no code reads soil health at growth time** — verified: zero references to growth/quality/harvest in `LivingRoots/` |
| Poor = pests | ❌ | pest system does not exist (`briefings/pest-system-mvp.md`) |
| Poor = no stars | ❌ | quality roll is 100% vanilla |
| Medium = vanilla | ✅ | true today *because nothing is hooked at all* |
| Living = faster growth | ❌ | not implemented |
| Living = extra harvest | ❌ | not implemented |
| Living = better quality | ❌ | not implemented |
| DESTROYS: bare tilled soil | ✅ | `SoilDecayService`, seasonal multiplier, greenhouse exempt |
| DESTROYS: monoculture | ❌ | only *bare* tiles decay; a monoculture never triggers it |
| DESTROYS: Joja Fertilizer | ❌ | item does not exist (`briefings/joja-fertilizer.md`) |
| BUILDS: compost | ✅ | bin → compost → +15/application, clamped (spec 004) |
| BUILDS: rotation / polyculture / green manure / mulch / animals | ❌ | all six unimplemented |

> **Banding mismatch to settle in the next spec:** the roadmap says 0-30 / 31-70 / 71-100; the
> implemented (spec 001) bands are `[0,34) / [34,67) / [67,100]` in
> `LivingRoots/Domain/Visualization/HealthCategory.cs`. Nothing reconciles them yet.

---

## Game Progression

### 🎨 Early Game (Year 1 - Learning the Fundamentals)

The focus is on survival and learning the new soil mechanics, being introduced to the basic concepts of agroecology.

*   **New NPC: Elena:** An agronomist and activist who serves as the player's mentor, introducing new mechanics and quests.
    ❌ **Not implemented.** No NPC, no dialogue, no schedule anywhere in the repo. Briefing exists: `specs/future-roadmap/briefings/elena-npc.md`. Note: an NPC is the single most content-expensive item on this roadmap (dialogue, schedules, portraits, festivals, hearts) and is *not* on the cheapest path — see the 80/20 order below.
*   **Agroecological Welcome Kit:** Replaces the initial parsnip seeds with a kit that includes a Composter and heirloom seeds.
    ❌ **Not implemented** (`briefings/welcome-kit.md`). Blocked by Elena (who gives the kit) and heirloom seeds (which the kit contains).
*   **Composting:** Unlocks the Composter (new machine) to transform organic waste into "Compost" (basic fertilizer).
    ✅ **Implemented and verified.** Crafting recipe, `Data/BigCraftables` bin, `Data/Machines` rule
    (`Triggers` + `DaysUntilReady = 2`), compost item with its own icon, pickup that preserves
    progress in `Item.modData` (FR-005c), tolerant save reader, refusal messages via
    `Game1.showRedMessage` in EN/PT. This is the only fully delivered gameplay loop in the mod.
    ⚠️ **But it has never been executed inside the game** — see "Where we actually are".
*   **Soil Cover (Mulch):** Ability to craft and apply mulch to prevent weeds and (most importantly) maintain Soil Health.
    ❌ **Not implemented**, but fully briefed (`briefings/mulch-soil-cover.md`): 1 object, 1 recipe
    (fiber ×10), one hook into the existing `SoilDecayService`. This is the cheapest *complete*
    feature on the roadmap and is the first defensive tool the player gets.
*   **Simple Polyculture (Companion Planting):** Introduction of "The Three Sisters" concept (Corn, Snap Beans, and Pumpkin) for growth bonuses and soil improvement.
    ❌ **Not implemented** (`briefings/three-sisters-polyculture.md`). The briefing explicitly
    rejects reusing the three vanilla crops: Green Bean is spring-only and trellised (`IsRaised`),
    Pumpkin fall-only, so a complete trio **never coexists** in an open field. Requires 3 new
    crops + 3 seeds + a crop sheet — real asset work.
*   **Heirloom Seeds:** Elena starts selling heirloom seeds, which are more sustainable and beneficial for the soil, in contrast to Pierre's hybrid seeds.
    ❌ **Not implemented** (`briefings/heirloom-seeds.md`). It is the *pipeline* brief (seed → crop →
    produce) that Three Sisters, Green Manure and Rotation Journal all consume.
*   **Rainwater Harvesting:** New crafting item: Small Cistern, to collect water on rainy days and reduce reliance on external sources.
    ❌ **Not implemented** (`briefings/small-cistern.md`).

**Early-game subtotal: 1 of 7 done** (composting). Note that the one item that is done is the only
one the player can act on; the other six are unreachable.

---

### 🎯 Mid Game (Year 2-3 - Building the System)

The focus is on integrating systems and creating a self-sustaining cycle on the farm, deepening agroecological practices.

*   **Crop Rotation:** Introduction of a "Rotation Journal" and classification of plants by families (e.g., Brassicas - Cauliflower, Cabbage; Solanaceae - Tomato, Pepper; Legumes - Beans, Peas). Proper rotation offers quality bonuses and improves Soil Health.
    ❌ **Not implemented** (`briefings/rotation-journal.md`). Needs per-tile crop history — the mod
    already persists per-tile state, so the persistence cost is low; the family registry and UI are the cost.
*   **Green Manure:** Elena sells "Green Manure" seeds (like clover or oats) to incorporate nutrients into the soil and give a significant boost to Soil Health.
    ❌ **Not implemented** (`briefings/green-manure.md`). Depends on the heirloom pipeline.
*   **Integrated Animal Management:** Animals left outside in un-fenced areas now eat weeds and insects (Pest Control) and leave manure that increases Soil Health (if mulched) or compacts it (if bare).
    ❌ **Not implemented** (`briefings/pest-system-mvp.md`, `briefings/mobile-coop.md`).
*   **Beneficial Insects and Beekeeping:** Beehives now have a second function: in addition to honey, they create an aura of "Beneficial Insects." "Insect Hotels" (new crafting) also attract beneficial insects for natural pest control.
    ❌ **Not implemented** (`briefings/insect-hotels-beehive-aura.md`). ⚠️ Caution: the entomological
    evidence for insect hotels is weak-to-negative — see `research/agroecology-feature-grounding-2026.md`.
    Do not present the pest-control benefit as established agronomy.
*   **Living Pharmacy:** Learn from Elena or Linus to grow medicinal herbs (Rue, Boldo, Chamomile). These can be used to create tonics (energy buffs) or "Natural Sprays" (Pest Control without soil damage).
    ❌ **Not implemented** (`briefings/living-pharmacy-tonics-sprays.md`). Needs 3 new crops + crafting + buffs.
*   **Bio-construction:** Robin learns from Elena to offer new construction upgrades: Adobe Shed, Green Roof.
    ❌ **Not implemented** (`briefings/adobe-shed-green-roof.md`).

**Mid-game subtotal: 0 of 6 done.**

---

### 🏆 End Game (Year 4+ - Sovereignty and Community)

The focus is on expanding the agroecological philosophy throughout the Valley, replacing the Joja system and achieving "Food Sovereignty."

*   **ICLF (Integrated Crop-Livestock-Forestry):** new trees (e.g., Acacia, Gliricidia) that fix nitrogen, allowing cultivation between them for drought immunity and faster growth.
    ❌ **Not implemented** (`briefings/iclf-trees.md`).
*   **Greywater Recycling:** House upgrade that collects greywater into tanks as a passive water source.
    ❌ **Not implemented** (`briefings/greywater-recycling.md`).
*   **Short Supply Chains (The Stardew Fair):** a Weekly Fair (every Saturday) in the town square; residents want variety, +25% profit and friendship.
    ❌ **Not implemented** (`briefings/weekly-fair.md`).
*   **Community Supported Agriculture (CSA):** residents subscribe to your CSA Box; deliver a varied basket weekly.
    ❌ **Not implemented** (`briefings/csa-box.md`).
*   **Collective Kitchen (The New Community Center Quest):** weekly event, cooking minigame, Community Meals buffs.
    ❌ **Not implemented** (`briefings/collective-kitchen-meals.md`).
*   **Food Sovereignty (The Final 'Victory'):** journal metric requiring >80% average Soil Health, 10 heirloom seed types, Collective Kitchen operational, 5 CSA families; reward: Pierre stops selling Joja hybrids, Morris leaves, Elena gives the "Guardian of Living Roots" trophy.
    ❌ **Not implemented** (`briefings/food-sovereignty-guardian-trophy.md`, `end-game-year4-plus.md`).
    Note: three of its four conditions (heirloom seeds, kitchen, CSA) are themselves unimplemented —
    the victory condition is the *last* thing that can ship, not the first.

**End-game subtotal: 0 of 6 done.**

---

## Progress summary

| Phase | Items | Shipped | Verdict |
|---|---|---|---|
| Core concept (soil health bar) | 13 promises | 2 measured + 1 displayed + 1 destroyed | **the substance is missing** |
| Early Game (Y1) | 7 | 1 (composting) | the tutorial tier is thin |
| Mid Game (Y2-3) | 6 | 0 | not started |
| End Game (Y4+) | 6 | 0 | not started |
| **Total** | **21 named features + 13 core promises** | **2 features + 4 of 13 promises** | |

All 31 design briefings for the unbuilt items already exist under
`specs/future-roadmap/briefings/`. What is missing is not design — it is **implementation**, and the
first implementation that makes soil health *matter*.

## Roadmap order — the 80/20

Ordering by *felt player impact per unit of implementation cost*, not by in-game year. Rationale and
cost analysis in the answer that produced this file.

1. ⚠️ **Prerequisite, not a feature:** launch the mod once and confirm the compost loop runs
   (`T034`/`T037` of spec 004 are blocked on exactly this). It has never been executed in a game.
   ~30 minutes, no code.
2. **`/speckit.specify` — Soil Health → Crop Growth.** The core concept's first real payoff. No new
   assets, no new items. Verified hook options: public `Crop.newDay(state, fertilizer, xTile, yTile,
   environment)` (virtual, already called by other mods to advance crops), or a Harmony postfix on
   the 1.6 fertilizer method `HoeDirt.GetFertilizerSpeedBoost()` which the game made an explicit
   extension point. Reuses the existing `DayStarted` tile scan, the per-tile persistence and the
   overlay. This is the 80/20: it is the smallest change that converts soil health from a number
   into a decision.
3. **Mulch** (`briefings/mulch-soil-cover.md`) — 1 object + 1 recipe + one decay hook. Once step 2
   exists, protecting soil has a reason to exist.
4. **Soil Health → Crop Quality** — the second half of the promise; needs a harvest-time hook
   (`Crop.harvest` is virtual and patchable; `HarvestMinQuality`/`HarvestMaxQuality` are data clamps,
   not per-tile), so cost is higher than step 2.
5. **Heirloom pipeline** → then Three Sisters, Green Manure, Rotation Journal (all consume it).
6. **Joja Fertilizer** — closes the "what DESTROYS" loop on the player's side.
7. **Elena + Welcome Kit + Cistern** — the content-heavy NPC epic; do it once there is a game worth
   tutoring.
8. Mid-game and end-game items, as designed.
9. **Multiplayer live verification (D1)** — owed since spec 004; Constitution Principle VI forbids
   declaring multiplayer complete without it.

### Reference point (external)

`ReGen Valley` (Nexus, Nov 2025) already ships this premise end to end — per-tile soil health,
cover crops, grazing, faster growth at 70+/90+ ("an extra day of growth per night") and
farm-wide quality/yield at 75+/90+ average. Our differentiation is therefore **not** the
soil-health mechanic; it is the agroecological grounding, the compounding compost loop, the
educational framing and multiplayer correctness. Design steps 2-4 to beat it on *coherence*, not on
novelty. See `research/comparable-soil-farming-mods-2026.md`.

### Blockers that are not roadmap items

* **7 proprietary game DLLs are committed and pushed to `origin/dev`** while `.gitignore:482` says
  they must never be. This is a licensing exposure, not a feature.
* `.github/workflows/protected-paths.yml` calls `scripts/check-protected-paths.sh`, which does not
  exist on `dev` — the workflow is broken by reference.
* `COMPLETION_SUMMARY.md` still claims 26 failing tests; the suite is green.
* Root `AGENTS.md` points at `ModConstants.cs` (the class lives in `Constants.cs`), an old branch
  name, and a retired CI variable.
