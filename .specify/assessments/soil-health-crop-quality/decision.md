# Decision: Soil Health → Crop Quality

- **Slug**: soil-health-crop-quality
- **Decided**: 2026-11-01
- **Verdict**: go
- **Artifacts reviewed**: intake.md (yes) | research.md (yes) | problem.md (yes) | concept.md (yes) | clarifications (accepted 2026-11-01)

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | The mod's central promise ("Living = faster growth, chance of extra harvests, higher quality") is 1/3 delivered; quality/extra-harvest are 100% vanilla and identical across tiers. Players on Living soil see no harvest payoff. [source: ROADMAP.md, GDD §3.1, problem.md] |
| Evidence strength | medium | Strong internal code evidence (HealthCategory bands, ObjectData has no Quality field, ExtraHarvestChance global, CompostApplicationService feedback model) and official wiki SMAPI/crop data; but runtime hook names (`Crop.harvest`, `Crop.dayUpdate`, `Object.Quality`) are unverified against the 1.6.3/1.6.14 binary, and no in-game run has ever executed. [source: research.md, briefing §5/§9] |
| Value vs. inaction | strong | Closing this loop is the only remaining item that makes Living Soil visibly pay off at harvest; competitor ReGen Valley already ships quality/yield at high soil health. Cost of inaction = mod perceived as diagnostic overlay with a compost buff. [source: problem.md "Cost of Inaction"; ROADMAP "Reference point"] |
| Feasibility / appetite fit | strong | Option A (phased: small slice 1 = transient feedback, medium slice 2 = gameplay hook) is credible and reuses existing persistence/overlay. All 10 blocking questions now have evidence-backed recommendations accepted by the maintainer (Q1-Q10). [source: concept.md, clarifications] |
| Strategic fit | strong | Aligns with constitution (DDD, TDD, multiplayer Principle VI), GDD pillars (solo é ser vivo; recompensar nunca pregar; co-op desde o dia 1), and ROADMAP 80/20 ordering (step 4 after mulch). [source: constitution, GDD §6D, ROADMAP] |
| Risk posture | strong | Key risks identified and mitigated in concept.md and clarifications (Harmony stability, vanilla composition, host-authoritative rolls, CVD non-chromatic channel, latent CollectCompost bug, band scale). All 10 blocking questions have evidence-backed recommendations accepted. [source: concept.md "Rabbit holes"; research "Evidence Against"; clarifications Q1-Q10] |

## Verdict & Rationale

The idea is **promising and strategically central** — it is the second half of the mod's headline promise and the 80/20 step after mulch. All 10 blocking questions from the previous `needs-clarification` verdict have now been researched (internal codebase deep-dive + external research) and their recommendations **accepted by the maintainer** (2026-11-01):

- **Q1 Band scale**: Switch to ROADMAP 0-30/31-70/71-100 (Cornell 2009) — agronomically grounded.
- **Q2 Hook names**: Verify with GamePath build; prefer public-field pass, Harmony as fallback.
- **Q3 Poor "no stars"**: Force `Object.Quality = 0` on Poor only; Medium untouched.
- **Q4 CollectCompost bug**: Fix before any output increase (two-phase commit + idempotency).
- **Q5 CVD channel**: Transient sprite shape + floating text + SFX (event-driven).
- **Q6 Numerical balance**: Keep spec 005 first-pass values (1.15/0.85, +0.08, +0.10) as tunable.
- **Q7 Pests scope**: Separate spec, shipped after crop quality.
- **Q8 "New interactions"**: Defer to own spec; soften ROADMAP/GDD wording.
- **Q9 Persistent marker**: Transient only; no persistent per-tile marker.
- **Q10 Gate 3 run**: Run now (~30 min, rebuild stale dll).

Evidence strength is now **adequate** (not weak/unknown): the unknowns are no longer blocking — they have evidence-backed recommendations that the maintainer accepted. Feasibility is **strong** (Option A phased, with validated assumptions). Risk posture is **strong** (risks identified, mitigated, and now accepted). The honest call is now **go**: the idea is worth specifying, with the accepted recommendations carried forward as resolved decisions and the remaining implementation-level validations (hook names, Gate 3 run, CollectCompost fix, band-scale migration) tracked as tasks within the spec.

## If needs-clarification

N/A — all 10 blocking questions resolved and accepted (2026-11-01). See Verdict & Rationale above.

## If go — Handoff to `/speckit.specify`

- **Problem**: Soil health is a persistent 0–100 value per tile, but only growth speed is hooked to it; harvest quality and extra-harvest probability are 100% vanilla and identical across all tiers. Living Soil (71–100%) must visibly pay off at harvest (silver/gold/iridium + extra harvest), Poor soil (0–30%) must yield no stars, and Medium soil must remain byte-identical to vanilla.
- **Chosen approach**: Option A — phased: Slice 1 (small) = transient tier feedback (`TemporaryAnimatedSprite` + floating text + SFX, event-driven, immune to 1000-tile degradation); Slice 2 (medium) = host-authoritative harvest-quality hook (`Object.Quality = 0` on Poor; +0.08 quality boost + +0.10 extra-harvest on Living), composing with (not replacing) vanilla rolls.
- **Accepted decisions (Q1–Q10)**:
  - Q1: Band scale → ROADMAP 0-30/31-70/71-100 (Cornell 2009), half-open edges at 31/71. Migration required.
  - Q2: Verify hook names with GamePath build; prefer public-field pass, Harmony as fallback.
  - Q3: Poor → force `Object.Quality = 0` at harvest; Medium untouched (guaranteed vanilla by omission).
  - Q4: Fix `CollectCompost` partial-success bug before any output increase (two-phase commit + idempotency flag).
  - Q5: Transient feedback: sprite shape + floating text + SFX (event-driven, immune to degradation).
  - Q6: Keep spec 005 first-pass values (1.15/0.85, +0.08, +0.10) as tunable constants.
  - Q7: Pests → separate spec, shipped after crop quality.
  - Q8: "New interactions" → defer to own spec; soften ROADMAP/GDD wording.
  - Q9: Transient only; no persistent per-tile tier marker.
  - Q10: Run Gate 3 session now (~30 min, rebuild stale dll).
- **In scope**: transient tier feedback; harvest-quality hook (Poor force-0, Living +0.08/+0.10); host-authoritative rolls; band scale migration (34/67 → 31/71); CVD non-chromatic channel (shape + text + SFX); test invariants (Poor→0, Medium≈vanilla).
- **Out of scope**: numerical balance tuning (first-pass values only, tunable); pest system (separate spec); "new interactions" (separate spec); persistent tier marker; `cvdSafe` palette (follow-up); `CollectCompost` fix (prerequisite, separate); Mid/End-game systems; Heirloom pipeline; Joja Fertilizer; Elena; CSA/feira/cozinha.
- **Success metrics**: M1 (in-game observable quality difference across 3 tiers), M2 (unit tests: Poor→Quality==0, Medium≈vanilla), M3 (host-authoritative roll tests with simulated second player), M4 (playtest casal, 1 estação), M5 (transient feedback immune to 16.67 ms budget and 1000-tile degradation).
- **Carried-forward open questions** (implementation-level, not blocking spec):
  - Verify `Crop.harvest`/`Crop.dayUpdate`/`HoeDirt.dayUpdate`/`Object.Quality` names against 1.6.3/1.6.14 binary (GamePath build).
  - Fix `CollectCompost` partial-success bug before output increase.
  - Create missing `color-schemes-cvd.md` briefing for `cvdSafe` palette.
  - Fix stale docs (design system §8.3, tier-outcomes briefing §2.4) claiming tinted patterns.
  - Run Gate 3 in-game session (spec 009 T052) to validate runtime hooks.
  - Reconcile band scale in code (constants + interpolation + ModController + tests + Gate 3 thresholds).
