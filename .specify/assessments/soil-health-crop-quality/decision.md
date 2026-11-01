# Decision: Soil Health → Crop Quality

- **Slug**: soil-health-crop-quality
- **Decided**: 2026-11-01
- **Verdict**: needs-clarification
- **Artifacts reviewed**: intake.md (yes) | research.md (yes) | problem.md (yes) | concept.md (yes)

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | The mod's central promise ("Living = faster growth, chance of extra harvests, higher quality") is 1/3 delivered; quality/extra-harvest are 100% vanilla and identical across tiers. Players on Living soil see no harvest payoff. [source: ROADMAP.md, GDD §3.1, problem.md] |
| Evidence strength | medium | Strong internal code evidence (HealthCategory bands, ObjectData has no Quality field, ExtraHarvestChance global, CompostApplicationService feedback model) and official wiki SMAPI/crop data; but runtime hook names (`Crop.harvest`, `Crop.dayUpdate`, `Object.Quality`) are unverified against the 1.6.3/1.6.14 binary, and no in-game run has ever executed. [source: research.md, briefing §5/§9] |
| Value vs. inaction | strong | Closing this loop is the only remaining item that makes Living Soil visibly pay off at harvest; competitor ReGen Valley already ships quality/yield at high soil health. Cost of inaction = mod perceived as diagnostic overlay with a compost buff. [source: problem.md "Cost of Inaction"; ROADMAP "Reference point"] |
| Feasibility / appetite fit | adequate | Option A (phased: small slice 1 = transient feedback, medium slice 2 = gameplay hook) is credible and reuses existing persistence/overlay; but it depends on unvalidated assumptions (Harmony feasibility, band scale, CollectCompost bug). [source: concept.md] |
| Strategic fit | strong | Aligns with constitution (DDD, TDD, multiplayer Principle VI), GDD pillars (solo é ser vivo; recompensar nunca pregar; co-op desde o dia 1), and ROADMAP 80/20 ordering (step 4 after mulch). [source: constitution, GDD §6D, ROADMAP] |
| Risk posture | adequate | Key risks identified and mitigated in concept.md (Harmony stability, vanilla composition, host-authoritative rolls, CVD non-chromatic channel, latent CollectCompost bug), but several remain unvalidated and one (band-scale ambiguity) is a blocking decision, not just a risk. [source: concept.md "Rabbit holes"; research "Evidence Against"] |

## Verdict & Rationale

The idea is **promising and strategically central** — it is the second half of the mod's headline promise and the 80/20 step after mulch — but it is **blocked on specific unknowns that must be answered before `/speckit.specify`**. Evidence strength is medium (not weak), but feasibility rests on unvalidated assumptions: the exact Harmony/public-field hook names are unverified against the shipped binary, the band scale (ROADMAP 0-30/31-70/71-100 vs. spec 001 half-open [0,34)/[34,67)/[67,100]) is unresolved, the latent `CollectCompost` partial-success bug is unfixed, and the "new interactions" promise is undefined. A `go` here would hand `/speckit.specify` an unshaped idea with blocking open questions, repeating the spec 005 pattern where T110/T113/T115 were marked complete without evidence. The honest call is `needs-clarification`: answer the blocking questions first, then re-run decide.

## If needs-clarification

- **Blocking questions**:
  - [NEEDS CLARIFICATION: band scale reconciliation — ROADMAP 0-30/31-70/71-100 vs spec 001 half-open [0,34)/[34,67)/[67,100]. Must be settled before spec; tracked as spec 005 T117–T119.]
  - [NEEDS CLARIFICATION: verify Harmony/public-field hook names (`Crop.harvest`, `Crop.dayUpdate`, `HoeDirt.dayUpdate`, `Object.Quality`) against the 1.6.3/1.6.14 binary using a build with `GamePath`.]
  - [NEEDS CLARIFICATION: composition rule for "no stars" on Poor with the vanilla quality roll (Farming level, fertilizer `quality_fertilizer_item`) without breaking "Medium = vanilla".]
  - [NEEDS CLARIFICATION: fix the latent `CollectCompost` partial-success duplication bug before any output increase.]
  - [NEEDS CLARIFICATION: non-chromatic feedback channel (sprite shape / floating text) for CVD compliance — design required.]
  - [NEEDS CLARIFICATION: numerical balance (growth multiplier, extra-harvest probability, quality odds) — at least a first-pass range.]
  - [NEEDS CLARIFICATION: scope of "pests on Poor" — separate feature or bundled?]
  - [NEEDS CLARIFICATION: definition of "new interactions" on Living, or explicit deferral.]
  - [NEEDS CLARIFICATION: persistent per-tile tier marker vs. transient feedback only (1000-tile degradation threshold).]
  - [NEEDS CLARIFICATION: Gate 3 in-game run (spec 009 T052) to validate runtime hooks against the real binary.]
- **Revisit stage**: research (for unverified runtime hooks and balance) → define (to restate goals once band scale and composition rule are settled) → shape (to finalize Option A slices) → decide.

## If go — Handoff to `/speckit.specify`

N/A — verdict is needs-clarification. Once the blocking questions above are answered, re-run `/speckit.assess.decide slug=soil-health-crop-quality`; on a `go`, the handoff will carry: problem statement (soil health as per-tile determinant of harvest outcome), recommended concept Option A (phased: transient feedback slice 1, host-authoritative quality/extra-harvest hook slice 2), in/out of scope (per problem.md and concept.md), success metrics M1–M5, and the carried-forward open questions.