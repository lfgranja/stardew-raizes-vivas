# Concept: Soil Health → Crop Quality

- **Slug**: soil-health-crop-quality
- **Created**: 2026-11-01
- **Recommended option**: Option A — Transient tier feedback + host-authoritative quality/extra-harvest hook (phased)

## Options

### Option A — Transient tier feedback + host-authoritative quality/extra-harvest hook (phased)
- **Sketch**: Deliver in two slices. Slice 1 (small): ship only the *observable* half — transient feedback (`TemporaryAnimatedSprite` + floating text) on harvest that announces the tier and its effect, plus non-chromatic cues, with no change to actual quality or growth. Slice 2 (medium): add the gameplay half — a harvest-time hook that sets `Object.Quality` to 0 on Poor and rolls silver/gold/iridium + extra harvest on Living, host-authoritative, composing with (not replacing) vanilla rolls. Reuses existing persistence + overlay; no new data entries, no PNGs.
- **Appetite**: small (slice 1, days) + medium (slice 2, weeks)
- **Trade-offs**: Wins the player-visible payoff early and de-risks the invasive hook behind a working feedback layer; keeps "Medium = vanilla" as an explicit regression gate. Sacrifices: two delivery passes, and the gameplay half still needs Harmony or a `DayStarted`/harvest pass that must be verified against the 1.6.3/1.6.14 binary. Does not resolve the band-scale ambiguity or the pest system.
- **Rabbit holes**: (1) Harmony method-name verification against the actual binary (high risk — names unverified); (2) composition with vanilla modifiers (`IsPaddyCrop`, `RegrowDays`, fertilizer) — naive multiplier double-dips; (3) host-authoritative roll sync and divergence rule; (4) CVD-safe non-chromatic feedback design; (5) the latent `CollectCompost` partial-success bug if any output increase is involved.

### Option B — Data-only metadata + deferred gameplay
- **Sketch**: Ship only `CustomFields` metadata on `Data/Crops` (e.g. `LivingRoots.TierGrowthMultiplier`) plus transient feedback sprites, with zero change to actual harvest quality or growth. Defer all gameplay hooks to a later feature. Effectively "label only."
- **Appetite**: small (days)
- **Trade-offs**: Lowest invasiveness and lowest risk; no Harmony, no multiplayer divergence, no regression risk to vanilla. But it does **not** close the loop build → protect → harvest — the player still sees no economic payoff, so the core promise stays undelivered. Essentially a documentation/overlay feature dressed as gameplay.
- **Rabbit holes**: None significant, but the feature would likely be perceived as a no-op and could erode trust given the spec 005 over-claim history.

### Option C — Full invasive overhaul (Harmony on growth + harvest + pest system + interactions)
- **Sketch**: One feature that delivers everything the ROADMAP promises for tiers: faster growth, no stars on Poor, silver/gold/iridium + extra harvest on Living, pest chance on Poor, and "new interactions" on Living. Full Harmony patching across `Crop.dayUpdate`/`HoeDirt.dayUpdate`/`Crop.harvest`, plus a pest system, plus interaction unlocks.
- **Appetite**: large (months)
- **Trade-offs**: Maximally delivers the promise in one pass and beats ReGen Valley on completeness. Sacrifices: highest risk (Harmony stability, cross-platform rewriting, multiplayer divergence, CVD, performance), longest time to any player-visible payoff, and bundles an undefined "new interactions" promise plus a non-existent pest system. Directly conflicts with Constitution V (YAGNI) and the mod's vanilla-first posture.
- **Rabbit holes**: every item in Option A's list, plus pest system design, interaction unlock design, balance numbers, and band-scale reconciliation — all undefined. Very high probability of scope blowout and of never shipping.

## Recommendation

Recommend **Option A** (phased). It is the only option that (a) delivers a player-visible payoff in the short term via transient feedback, (b) keeps the invasive gameplay hook isolated, testable, and reversible in a second slice, and (c) respects the constitution's TDD (RED before GREEN) and multiplayer (host-authoritative) constraints by forcing the hook to be specified and tested before implementation. Option B fails the problem's core goal (Living soil must pay off at harvest). Option C violates YAGNI and bundles undefined promises (pests, interactions, balance) with no evidence they are buildable — and the repo's history of over-claimed completion (spec 005 T110/T113/T115) makes a large all-at-once feature especially risky.

The phased approach also lets the Gate 3 in-game session (spec 009 T052) and the band-scale reconciliation (spec 005 T117–T119) land in parallel with slice 1, de-risking slice 2.

## Out of Scope (for the recommended option)

- Numerical balance (growth multiplier, extra-harvest probability, quality odds) — deferred to `/speckit.specify`.
- Pest system for Poor soil — separate feature (`briefings/pest-system-mvp.md`).
- "New interactions" unlocked by Living soil — vague; separate spec.
- Persistent per-tile tier marker overlay (1000-tile degradation threshold) — transient feedback only.
- Changing the default color palette (CHK013) — non-chromatic channel added, default untouched.
- Band-scale reconciliation (ROADMAP vs spec 001) — must be settled before or with slice 2, but is tracked separately (spec 005 T117–T119).
- `CollectCompost` partial-success bug fix — prerequisite, tracked separately.
- Mid/End-game systems, Heirloom pipeline, Joja Fertilizer, Elena, CSA/feira/cozinha — out of scope per GDD §6.2.

## Assumptions (resolved 2026-11-01 — see decision.md)

All assumptions resolved and accepted by the maintainer:

- **A1** `Crop.harvest`/`Crop.dayUpdate`/`HoeDirt.dayUpdate`/`Object.Quality` names → to be verified with a GamePath build (Q2); public-field manipulation already proven for growth (spec 005).
- **A2** Harmony vs public-field → prefer public-field pass (proven), Harmony as fallback only (Q2).
- **A3** Transient `TemporaryAnimatedSprite` feedback sufficient → yes, recommended and accepted (Q5, Q9); immune to 1000-tile degradation.
- **A4** Host-authoritative rolls via `Context.IsMainPlayer` + `TryApplyCompostWithLock` → yes, accepted (Q3 composition rule + Q7 pest separation).
- **A5** `CollectCompost` bug fixed before slice 2 → accepted as prerequisite (Q4).
- **A6** Band scale reconciled → accepted: ROADMAP 0-30/31-70/71-100 (Cornell 2009), half-open edges 31/71 (Q1). Migration required before slice 2 tests.