# Decision: Mulch / Soil Cover

- **Slug**: mulch-soil-cover
- **Decided**: 2026-10-30
- **Verdict**: go
- **Artifacts reviewed**: intake.md | research.md | problem.md | concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Bare soil decays at 2/day with no preventive tool; crop growth now depends on soil health (spec 005 shipped), so protection has a concrete reason to exist. |
| Evidence strength | high | Agronomic meta-analysis (90 studies): runoff −47%, soil loss −76%. Competitor ReGen Valley validates demand. Prior art (Nexus Mulch mod, 42 endorsements) confirms player interest. |
| Value vs. inaction | strong | Without mulch the loop stays incomplete (build→recover only, no protect); differentiation from ReGen Valley limited to compost; 80/20 stalls at step 2. |
| Feasibility / appetite | strong | Smallest complete feature on the roadmap (1 object + 1 recipe + 1 hook + 1 persisted flag). Reuses all existing infra (decay, overlay, i18n, persistence, multiplayer guards). Small appetite (days). |
| Strategic fit | strong | Aligns with constitution (DDD, async-only, security-first, multiplayer Principle VI), ROADMAP 80/20 ordering (step 3), and the agroecological mission. |
| Risk posture | adequate | Key risks identified: Elena dependency (mitigated — shop criterion deferred, craftable recipe works without her), asset creation (placeholder viable with specified colors), balance (0f vs 0.25f deferred to playtesting), overlay z-order (to be verified during implementation). No unmitigated serious risks. |

## Verdict & Rationale

**Go.** The problem is real and well-evidenced: mulch is the strongest agronomic claim in the roadmap (meta-analysis −47% runoff, −76% soil loss), the demand signal is confirmed by ReGen Valley and the Nexus Mulch mod, and the implementation cost is the lowest of any remaining feature. The concept (Option A — item-applied mulch) is concrete, scoped, and reuses all existing infrastructure. The main risks (Elena dependency, asset creation, balance) are identified and mitigated or deferrable. Killing this idea would stall the 80/20 at step 2 and abandon the most defensible agroecological mechanic.

## If go — Handoff to `/speckit.specify`

- **Problem**: Players have no preventive tool against soil decay; soil health is a treadmill (lose 2/day, recover with compost) with no protection, and the agroecological lesson "cover the soil" cannot be taught.
- **Chosen approach**: Option A — Item-Applied Mulch. Craftable item (fiber ×10) applied to tilled tile; zeroes decay for 7 days; tile overlay with `MulchColor` tint + `Dots` pattern; persists `HasMulch` + days remaining via `ModDataService`.
- **In scope**: item (16×16 sprite), recipe, `IMulchApplicationService` (Domain/Interfaces + Services), hook in `ModController.OnButtonPressed`, `MulchDecayMultiplier = 0f`, constants, i18n (en + default), overlay integration, multiplayer guards (Principle VI), tests (data, recipe, decay, duration, rain, invalid-tile, already-mulched, overlay).
- **Out of scope**: Elena NPC/shop (deferred; craftable recipe works without her), new crop/plantable mulch, weed prevention as core mechanic Y1, new .ogg audio, mutation testing, multiplayer live verification, SonarCloud/CI changes, crop quality/extra harvest (later specs).
- **Success metrics**: build 0/0; tests baseline (1436) + new mulch tests; format clean; craft day 1 with fiber; mulched tile maintains health 7 days; `decay *= 0` on mulched tile (incl. Spring 0.5×/Summer 1.5× synergy); rain does not remove; invalid-tile and already-mulched messages.
- **Carried-forward open questions**:
  1. `MulchDecayMultiplier` final value — `0f` for Y1, subject to playtesting (no in-game runs yet).
  2. Elena dependency — shop sale criterion deferred; craftable recipe (fiber ×10) is the primary path.
  3. Who creates the `mulch.png` sprite 16×16 and when? (placeholder viable with specified colors `#92400E`/`#A16207`/`#78716C`.)
  4. 7-day duration — balanced? Configurable in a later pass?
  5. Overlay z-order compatibility with spec 001 health overlays — to be verified during implementation.
  6. Weed prevention scope — visual/secondary in Y1 or deferred?
  7. `MulchTag` propagation to other compostable items?