# Concept: Soil Health → Crop Growth

- **Slug**: soil-health-to-crop-growth
- **Created**: 2026-10-15
- **Inputs used**: problem.md (problem definition) | research.md (evidence, sources, gaps) | intake.md (user criteria / scoping) | ROADMAP.md (goals / non-goals)
- **Recommended option**: Option A — Small: hook only (no new asset / item / recipe), with the `CollectCompost` rollback fixed first and the band scale decided before any code

> Reconciliação com a premissa: o problema é que a saúde do solo não afeta a colheita; a solução de conceito não pode ser "faça uma especificação completa" — ela precisa ser "escolha o caminho de menor risco que conecta o que já existe à decisão do jogador".

---

## Options

### Option A — Small: Hook-only (public `Crop.newDay` or `HoeDirt.GetFertilizerSpeedBoost()` patch)
**Sketch**: Connect the existing per-tile `SoilHealthService` (0–100) to the existing `Crop` growth mechanism by applying a multiplicative speed/quality adjustment per `HealthCategory`. No new item, no new PNG, no new recipe, no new UI — only a service, a constant per band, a hook, and pure-logic tests. The player's experience: apply compost → tile changes color → crop grows/harvests differently on that tile. The rest of the loop (overlay, tooltip, hoe feedback) stays untouched.

**Appetite**: **small** (days to ~2 weeks of focused work — not an estimate, a budget range; the user's criteria say "low-moderate"). This matches the evidence from `research.md`: the API is public, the persistence is sparse and already works, the `DayStarted` scan exists (`SoilDecayService.ProcessDayStart`), and the overlay already renders.

**Trade-offs**:
- **Wins**: Converts measurement into gameplay — the single change that justifies 19 unimplemented mechanics; zero asset cost; reuses all existing patterns (`IVisualizationService.InvalidateCache`, `HostAtomicLockService`, `ModConstants`); correct multiplayer path is clear (host-authoritative roll, same model as `TryApplyCompostWithLock`).
- **Sacrifices**: Only growth/quality changes — no new crop, no new item, no new NPC, no new building. The experience is a single-tile improvement, not a system.
- **Risks**: The validation risk is real and dominant (mod never executed — `problem.md` §Evidence Against; `research.md` §Gaps: no SMAPI log, newest save `MDMV_424795694` from 2026-01-06 has zero `LivingRoots` data). Changing live `Crop.phaseDays` without any prior execution is the highest-stakes action in the repo. The `ReGen Valley` comparison (`research.md` §B) confirms the premise is viable — not novel — so failure here is not "feature not needed" but "hook wrong."
- **Unknowns**: Composition with vanilla (`IsPaddyCrop`, `RegrowDays`, `Agriculturist`, `Speed-Gro` double-dip) must be verified per-tile; `HealthCategory` bands must be reconciled (`ROADMAP.md` 0-30/31-70/71-100 vs. `spec-001` `[0,34)/[34,67)/[67,100]`); whether `Crop.newDay` public-field approach has ordering issues with vanilla growth needs a build-with-`GamePath` spike.

**Rabbit holes**:
- **Band reconciliation**: if not decided before spec, every metric, constant, and test must change. Deciding now avoids rework.
- **Validation of live crop-state change**: with 0 prior executions, the first test of this feature must be manual (`T034` / `SC-008` of spec 004 — the only window). If it breaks, diagnosis is by eye, not by `dotnet test`. The `problem.md` §Open Questions carries this explicitly.
- **`ReGen Valley` interaction**: both mods alter `Crop`; if both apply multiplicative adjustments without a composition rule, one may suppress or double the other. Not solved by this option — requires a spec-level note.
- **Rollback of `CollectCompost`**: if not corrected, any yield increase activates a hidden duplication bug (`research.md` §Evidence Against, risk #1). Must come before or with, not after.

### Option B — Medium: Full feature (growth + quality + extra yield + mulch integration + heirloom pipeline start)
**Sketch**: Everything in Option A plus harvest-time quality (hook in `Crop.harvest` — requires postfix/transpiler or a new mechanism) and the mulch item/recipe (`briefings/mulch-soil-cover.md` — 1 object + 1 recipe + decay hook), plus the first step of the heirloom pipeline (seed → crop → produce, `briefings/heirloom-seeds.md`). Would deliver the early-game tier (mulch as defense, soil response to compost, quality at harvest) as a coherent package.

**Appetite**: **medium** (weeks to ~2 months — involves 3 asset categories from briefings: crop sheet + item + seed, plus quality hook, plus decay integration). This was the implicit scope of the user's criteria table, which lists "service + hook + fix + tests + band reconciliation" — but does not include asset creation.

**Trade-offs**:
- **Wins**: The player's loop is complete (apply → see → defend with mulch) rather than partial; the Three Sisters / green-manure / rotation features have a pipeline to consume (even if they ship later); the demonstration of coherence is stronger.
- **Sacrifices**: Adding crop assets means asset-registration verification (3 PNGs per crop, 3 textures, `Data/Crops` edit at `ModEntry.cs:228`) — a new failure surface; adding mulch means a new item/recipe, new i18n keys, new refusal messages; the validation risk multiplies because each new surface needs its own manual verification.
- **Risks**: The user's criteria say the validation risk is the dominant one — multiplying surfaces multiplies that risk. Also, `ReGen Valley` already has this level of completeness; competing on completeness is not the advantage.
- **Rabbit holes**: The Three Sisters briefing explicitly notes that vanilla crops (`Data/Crops` keys `487`/`473`/`490`) must never be edited; new heirloom crops require new sheets, new `CustomFields` for companion detection, and a lookup table for equivalence — the briefing estimates this as a "spike" before spec. The `soil-tier-outcomes-harvest-feedback.md` briefing also notes the `OverlayRenderer` split (`RenderedWorld` vs. `Rendered`) is not yet implemented — adding tier feedback (sprites of tier outcome) requires that split, which is out of scope for this concept.

**Out of scope for this option (explicit)**: No new NPC (Elena), no welcome kit, no cistern, no pest system, no multiplayer live session (D1), no GMCM page (only `i18n` + persistence, per existing pattern), no new building (adobe / green roof), no end-game items (CSA / kitchen / fairness / sovereignty).

### Option C — Small: Do nothing / verify first / buy instead of build
**Sketch**: Do not build the hook. Instead, confirm that the existing loop is playable (T034/T037 of spec 004) by launching the game with `GamePath` set, running the compost loop, and recording whether the mod loads, the bin works, and the overlay renders. If it passes, the decision to build is deferred to a future assessment. If it fails, the fix is the failure mode — not the growth hook.

**Appetite**: **small** (30–60 minutes — one manual run; zero code).

**Trade-offs**:
- **Wins**: Eliminates the dominant validation risk before any state-change code is written; if the mod never loads or the bin never works, the growth hook is irrelevant. Also, the `git` / filesystem / `COMPLETION_SUMMARY.md` / `AGENTS.md` / 7-DLL issues can be repaired with confidence that the baseline works.
- **Sacrifices**: The user's criteria explicitly ask for impact — this option delivers zero gameplay change. It is a prerequisite, not the answer.
- **Risks**: The user's request is specifically for this feature; recommending "do nothing" must be justified with the evidence that (a) the problem is validated but (b) the validation environment is broken (0 executions, broken index, false summary doc). The evidence supports that.

**Rabbit holes**: None — this is deliberately the smallest option.

---

## Recommendation

**Recommend Option A — Small (hook only) — but with Option C as a hard prerequisite.**

**Reasoning tied to goals / metrics from `problem.md`:**

- The problem is "soil health doesn't affect gameplay." Option A directly solves it with the smallest surface (service + constant + hook + pure-logic tests). Option B solves it with more surface and more validation risk — not what the problem demands.
- The success metric "growth speed / quality / extra yield change on Poor / Living tiles" is achievable with Option A; Option B does not change those metrics — it changes only the completeness of the package.
- The evidence (`research.md`) confirms the hook is technically feasible (public `newDay`, `GetFertilizerSpeedBoost()` extension point, no Harmony required for the minimal path). The main unknown is ordering with vanilla growth — which a spike (not a spec) resolves.
- The non-goals from `problem.md` (no new NPC, no new item/recipe, no multiplayer sync, no teaching redesign) are preserved.
- The cost of inaction is "19 mechanics have no reason" — Option A is the smallest thing that gives them a reason.

**Why Option B is not recommended**: The user's criteria table values impact over completeness, and the validation risk dominates. Option B multiplies the surfaces that must be manually verified (new crop, new item, new i18n, new refusal, new persistence for mulch duration) — exactly the wrong trade-off given the dominant risk.

**Why Option C is required first**: The `problem.md` §Evidence Against says the dominant risk is "never executed in game." Option A touches live `Crop.phaseDays` — that requires the baseline (composite loop) to be verified first. Option C (manual run, ~30 min, zero code) is the prerequisite gate.

**Explicit out-of-scope for the recommended Option A** (inherited from `problem.md` non-goals + new exclusions):

- No new assets / PNG / texture / sprite / sound (only procedural `Texture2D` if tier feedback is added — and even that is excluded from this option; the overlay already exists).
- No new item / recipe / craftable (mulch, heirloom seeds, Joja Fertilizer — all deferred).
- No new `Data/Crops`, `Data/Characters`, `Data/Buildings`, `Data/Shopping`, `Data/Quests` edits.
- No `IMulchApplicationService` or `ICompanionPlantingService` (those are Option B).
- No `Elena` NPC, no welcome kit, no cistern.
- No `ContentPatcher` — the repo uses only `AssetRequested` + `e.Edit` (confirmed in `ModEntry.cs` and briefings).
- No `config.json` / GMCM page — persistence stays per-save via `IModDataService` (existing pattern from `VisualizationConfigurationService`).
- No new `i18n` keys for growth — the existing 5 (`message.compost.*` + `inventory-full`) are sufficient for refusal messages; growth feedback can reuse `floating-text` / `restoration-text` pattern (already wired in `CompostApplicationService.cs:64-78`) without new keys.
- No change to `HealthCategory` bands in this option — but the reconciliation (ROADMAP vs. spec-001) must be decided and locked before any metric uses them.
- No multiplayer transport change — Principle VI requires host-authoritative roll; the sync mechanism is D1 debt from spec 004, out of scope here.

---

## Assumptions the recommendation depends on

1. **Public-field path works** (assumption — not verified in build with `GamePath` yet): `Crop.newDay` uses only public fields (`currentPhase`, `phaseDays`, `dayOfCurrentPhase`) and no private state that a prefix/postfix needs; ordering with vanilla growth is correct when applied at `DayStarted` before vanilla growth runs (or after, with the correct offset). The alternative (`HoeDirt.GetFertilizerSpeedBoost()`) removes this assumption — but requires Harmony / new package.
2. **Composition with vanilla is achievable** (assumption — the briefing notes double-dip risk; the research notes the formula `p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01` and that `HoeDirt.GetFertilizerSpeedBoost()` already centralizes fertilizer logic): a multiplicative adjustment entering that method (rather than a separate roll) avoids double-dip.
3. **Band reconciliation is a design decision, not technical** (assumption — both scales are documented; neither is "correct"; the user's criteria say decide one): once decided, all constants, all tests, and both `ROADMAP.md` and `spec-001` must agree.
4. **The `ReGen Valley` interaction is a composition rule, not a blocking issue** (assumption — not yet specified; requires a spec note, not a code change): if both mods modify `Crop`, they must compose multiplicatively; the spec must state which applies first.
5. **The manual play-through gate (T034 / SC-008) is achievable** (assumption — the only verification of live crop-state change): with a valid `GamePath` and `dotnet build`, launching the mod, planting 3 tiles, applying compost, and observing growth difference is the gate. The `research.md` notes the save file `MDMV_424795694` is from 2026-01-06 (before source edits) — so any verification needs a fresh run, not an old save.
6. **The `CollectCompost` rollback is fixable independently** (assumption — the bug is masked today because `outputCount = 1`; fixing it means adding rollback when count > 1): this can ship with or without the growth feature, but should come first per the user's criteria.

---

*Written to `.specify/assessments/soil-health-to-crop-growth/concept.md`. No source files modified. No architecture, no data model, no API — only 3 option sketches with trade-offs, one recommendation (Option A, small, with Option C as prerequisite gate), out-of-scope list, and the 6 assumptions that must be validated in `/speckit.assess.decide` or the spec that follows. Next: `/speckit.assess.decide slug=soil-health-to-crop-growth` (requires the spike of hook + rollback fix + band decision, carried as open questions).*
