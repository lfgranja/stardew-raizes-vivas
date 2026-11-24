# Idea Intake: Soil Health → Crop Growth

- **Slug**: soil-health-to-crop-growth
- **Created**: 2026-10-15
- **Source**: /home/luis/development/stardew-raizes-vivas/ROADMAP.md (line 7–11 core-concept promises) + spec 004 clarification session + user request (pasted block with criteria table)
- **Type**: new-capability (connects existing measurement/display layer to gameplay layer — no new assets, no new items, no new recipes)

## Idea (as captured)

> "Soil Health → Crop Growth" — make the mod's per-tile 0–100 soil-health value actually affect crop outcomes (growth speed, harvest quality, possible extra harvest) rather than being only a visual overlay and compost-application score. The user provided the criteria table: Impact (first real gameplay effect), Effort (zero new asset / PNG / item / recipe; reuses `DayStarted` tile scan + persistence + overlay), Technical Risk (low-moderate — compose with vanilla modifiers without double-dip), Validation Risk (real — mod never executed in game), and a minimal-scoped task list (constant per band, `ISoilGrowthService`, hook in `Crop.newDay`, fix `CollectCompost` rollback, pure-logic tests, reconcile bands).

The idea is captured verbatim from the user's table and from the verified code audit that found zero references to growth/quality/harvest in the production tree (`LivingRoots/` — confirmed by grep, not inferred).

## Restated

Implement a host-authoritative, per-tile soil-health multiplier that modifies `Crop` growth speed and harvest quality, using only existing persistence/overlay infrastructure and no new assets, with the reconciliation of the two conflicting band definitions (ROADMAP 0-30/31-70/71-100 vs. spec-001 [0,34)/[34,67)/[67,100]) as part of the spec.

## Origin & Context

- **Raised by**: User (this session, 2026-10-15) — explicitly referencing the verified audit just completed on the repo (build/test clean, 1063/1063 green, 2 of 21 roadmap features shipped, core concept 4 of 13 promises, 0 gameplay effects from soil). The user also explicitly referenced `/speckit.specify`, `/speckit.assess.intake` and the investigation pipeline.
- **Trigger**: The audit showed the gap is structural, not cosmetic — soil health exists but is inert. The user asks for the 80/20 next step.
- **Evidence gathered in this session** (not invented):
  - `dotnet build` / `dotnet test / --no-build -c Release` results (verified)
  - Source audit of `ModController`, `ModEntry`, `SoilHealthService`, `CompostingBinService`, `SoilDecayService` (verified — all 12 event subscriptions, no growth/quality hooks)
  - `ROADMAP.md` annotated (committed `5ac6576`, corrected `f3869ac`) with verified status per item
  - `specs/future-roadmap/briefings/soil-tier-outcomes-harvest-feedback.md` (verified — the briefing explicitly states zero coverage of the tier-outcome mechanism in the repo and requires a `/speckit.specify` before promotion)
  - External research (verified URLs): SMAPI 4.5.2 (Mar 2026), `Crop.newDay` / `Crop.harvest` / `HoeDirt.GetFertilizerSpeedBoost()` documentation, `ReGen Valley` (Nexus 39740, Nov 2025 — direct competitor cycling per-tile soil health into faster growth at 70+/90+), meta-analysis mulch Geoderma 2023 (runoff −47.4% / soil loss −76.2%), Three Sisters peer-reviewed evidence (Annals of Botany 2014; 2024 Catawba study; 2007 Wisconsin study — all showing yield/timeline trade-offs, not mutual soil-improvement magic), insect-hotel evidence (MacIvor & Packer PLoS ONE 2015 — "bee-washing"; NC State Extension — inconclusive).

## First-Glance Unknowns

- [NEEDS CLARIFICATION] Is the hook a public-field approach (`Crop.newDay` via a `DayStarted` scan, no new dependency) or a Harmony patch (requires adding `Lib.Harmony` and a new package reference)? The briefing `soil-tier-outcomes-harvest-feedback.md` flags both paths and asks for a spike. The user's criteria prefer "menor esforço" — this should be settled before `specify`.
- [NEEDS CLARIFICATION] The user's table specifies reconciliation of the banding discrepancy (ROADMAP vs. spec-001). Which scale is authoritative — the user's intent (ROADMAP's 0-30/31-70/71-100) or the implemented and tested (spec-001's half-open intervals)? A decision is required to write the spec; neither is "wrong", but they must converge.
- [NEEDS CLARIFICATION] How is "extra harvest" (Living tier) to be implemented — via `ExtraHarvestChance` in `Data/Crops` (global, not per-tile) or via a per-harvest roll in `Crop.harvest`? The briefing notes `Data/Crops` fields are global per seed and cannot give per-tile extra harvest; this is a spec-level choice.
- [NEEDS CLARIFICATION] Is `CollectCompost` rollback fix a strict pre-requisite (as the user's criteria say) or can the growth feature ship independently? The rollback bug is masked today (outputCount=1) and activates only when output increases; if growth feature does not touch output quantity, order is flexible.
- [NEEDS CLARIFICATION] Validation plan: the mod has never executed in-game. Before any state-change hook touches live `Crop` objects, what is the manual play-test gate? (Spec-004's `SC-008` / T034 already requires manual playthrough — can reuse that gate.)

## Suggested next step

`/speckit.assess.research slug=soil-health-to-crop-growth` — but only for the **spike** (compare `Crop.newDay` vs. `HoeDirt.GetFertilizerSpeedBoost()` in the build with `GamePath` set, measure neither's blast radius, decide the hook; verify `ReGen Valley`'s approach is reachable; resolve the banding discrepancy). The idea is already well-understood — this is not an open-ended exploration. Do not proceed to `/speckit.specify` until:

1. Spike completes (hook choice + composition rule with vanilla modifiers)
2. `CollectCompost` rollback fix confirmed
3. Band scale decided and written down
4. The mod has been launched at least once and the compost loop verified (T034 dependency)

---

*Created via /speckit.assess.intake as instructed. Path: `.specify/assessments/soil-health-to-crop-growth/intake.md`.*
