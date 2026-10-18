# Decision: Soil Health → Crop Growth

- **Slug**: soil-health-to-crop-growth
- **Decided**: 2026-10-15
- **Verdict**: **needs-clarification** (not kill — the idea is sound; the evidence supports it; the validation gate is unclosed — blocking, not rejecting)
- **Artifacts reviewed**: intake.md? | research.md? | problem.md | concept.md?

> Note: this is not a "no." It is a "yes — but the dominant risk (validation of live crop-state change, with 0 prior executions) is unmitigated, and 5 open clarification questions from the assessment must be answered before `/speckit.specify` receives it." The `go` recommendation from `concept.md` (Option A) is preserved; only the hand-off is held.

---

## Scorecard

| Criterion | Rating | Justification (drawn from artifacts, not invented) |
|---|---|---|
| Problem validity | **strong** | `problem.md`: core concept (soil health 0–100, persistent, displayed, compost-adjustable) has zero gameplay effect — 2 of 21 roadmap features exist; all 18 unimplemented depend on this connection. `spec.md` §Problem confirms: "reports success while being unplayable." The gap is structural, not cosmetic. |
| Evidence strength | **adequate** (not weak; not fully verified) | `research.md`: build/test verified (0 errors / 0 warnings / 1063 passing); source audit of `ModController` + `ModEntry`; 15 primary sources (SMAPI 4.5.2 release, `Crop.newDay` / `Crop.harvest` decompiled source, `ReGen Valley` product, 3-15 DOI studies, SciELO/PT, wiki). The 3 background subagents hit provider rate limits — replaced by direct search; no claim is unsourced. The only `unknown` is external user demand (`[NEEDS CLARIFICATION: has anyone outside the maintainer played?]`). |
| Value vs. cost of inaction | **strong** | `problem.md` §Cost of Inaction: without this, 19 mechanics have no reason; the assessment stream produces a `go` that never converts to gameplay; `ReGen Valley` (2025) proves the premise is viable — differentiation is coherence (agroecology, multiplayer, honest docs), not novelty. |
| Feasibility / appetite | **strong** | `concept.md` Option A: zero new asset / item / recipe; uses `DayStarted` scan + persistence + overlay; `Crop.newDay` is public virtual (confirmed by source + `LuckSkill` usage); `HoeDirt.GetFertilizerSpeedBoost()` is the 1.6 extension point for composition. Appetite `small`. |
| Strategic fit | **adequate** | Constitution Principle VI (multiplayer first-class) respected by design (`HostAtomicLockService`, `TryApplyCompostWithLock`, host-authoritative roll). Extends `spec-004` (playable loop) and feeds `spec-001` (visualization). No contradiction with `AGENTS.md`. |
| Risk posture | **weak** (honest — not hidden) | `problem.md` §Evidence Against + `research.md` §Evidence Against: (1) 0 in-game executions — newest save `MDMV_424795694` (2026-01-06) has zero `LivingRoots`; SMAPI log dir empty; (2) live `Crop.phaseDays` touch is untested; (3) `ReGen Valley` interaction unspecified; (4) 7 proprietary DLLs tracked (`git ls-files .gamepath`); (5) `.git/index` broken (`btrfs` inode exhaustion); (6) `COMPLETION_SUMMARY.md` false (`26 failed` → 0); (7) `CollectCompost` rollback bug masked (activates on any output > 1). All 7 documented, none glossed. The risk is **understood** but **not mitigated** — the manual play-through (`T034` / `SC-008`) has not happened. |

**Verdict logic**: Evidence is `adequate+` on problem, prior art, feasibility, strategic fit, and value. The verdict is **not `kill`** (problem is real, competitive product proves demand, feasibility is sound) and **not `go`** (validation is unmitigated and the 5 clarification questions — especially the spike of hook + the manual run — are unresolved). It is **`needs-clarification`**, with the clarifications being exactly the 5 open questions from `problem.md`.

---

## Verdict & Rationale

**Verdict: `needs-clarification`.**

Reasoning, in order of the scorecard:

1. **Problem is real and worth solving** (strong). The measurement layer works (`SoilHealthService`, overlays, persistence, decay); the gameplay layer is missing (`Crop.newDay` never consulted, `Crop.harvest` never consulted). Without this, every other roadmap item (mulch, rotation, heirloom, Three Sisters, Joja, Elena, CSA, kitchen, sovereignty) is a feature that modifies a number with no consequence — decorative, not systemic.
2. **Evidence supports the idea** (adequate). The external research (`research.md`) confirms: `Crop.newDay` and `Crop.harvest` are public/virtual; `ReGen Valley` already delivers per-tile growth/quality; mulch (Geoderma 2023 meta-analysis) and green manure (34-yr trial) are strongly supported; Three Sisters is real but over-stated (bean ~10× lower yield — must not be taught as mutual magic); insect hotels are not supported (MacIvor 2015 — "bee-washing"). The repo's own audit confirms zero gameplay effect. No evidence contradicts the idea.
3. **Feasibility fits the appetite** (strong). Option A (hook-only, `small`) uses zero assets. The user's criteria say "zero new asset / PNG / item / recipe" — Option A satisfies exactly. The technical risk (composition with vanilla) is bounded by the `GetFertilizerSpeedBoost()` extension point.
4. **The risk posture is the blocker** (weak → requires clarification). The dominant risk is validation (`problem.md` §Evidence Against: never executed; `research.md`: no SMAPI log; `COMPLETION_SUMMARY.md` false; `.git/index` broken — all make verification unreliable). Changing live `Crop.phaseDays` without any prior execution is not "test and deploy" — it is "launch and observe with no baseline." That is acceptable only if documented, gated, and reversible.
5. **Strategic fit is good** (adequate). The idea extends `spec-004` (completes its loop) and feeds `spec-001` (makes visualization meaningful). No contradiction with Principle VI (host-authoritative roll via `HostAtomicLockService`, same model as compost concurrency). The competitive product (`ReGen Valley`) does not make this worse — it makes the claim achievable.

**Why not `go`?** Because the `problem.md` requires a manual play-through (`SC-008`) to confirm the base loop before any state-change hook is applied, and that gate has not been passed. A `go` that hands `specify` an unverified live-state-change feature would violate the assessment's own guardrail ("never over-claim a `go`: if evidence is thin or no concept was shaped, the honest verdict is `needs-clarification`"). The concept *is* shaped (`concept.md` — Option A recommended), but the evidence for the validation gate is `unknown`, not `adequate`.

**Why not `kill`?** The problem is structurally real (the measurement layer is complete, the gameplay layer missing, the competitive product proves it). Killing it means accepting that the 2 delivered features (compost + visualization) will remain the only 2, and that the 19 planned features have no mechanism — which contradicts the assessment's own `go` verdict (`next-feature-80-20/decision.md`, 2026-10-03) and the user's stated intent.

---

## If needs-clarification — Blocking questions (must resolve before `/speckit.specify`)

- **[NEEDS CLARIFICATION — must resolve before spec] Spike of hook (#1)**: Confirm `Crop.newDay` public-field path works correctly with vanilla growth (no Harmony needed for minimal) — test in build with `GamePath` (the `research.md` notes the `ModBuildConfig` hard-fails without `GamePath`; the build environment must be set). If the public-field approach fails (ordering issues with vanilla growth), the decision shifts to `HoeDert.GetFertilizerSpeedBoost()` (Harmony / new package) — which changes the effort from `small` to `medium`.
- **[NEEDS CLARIFICATION — must resolve before code] Band reconciliation (#2)**: Lock `HealthCategory` bands: adopt `ROADMAP.md` 0-30 / 31-70 / 71-100, or `spec-001` half-open `[0,34)/[34,67)/[67,100]`? `problem.md` and `research.md` both carry this; `concept.md` excludes it from Option A's scope (decide first) but notes it must be settled before any metric uses them.
- **[NEEDS CLARIFICATION — must resolve before any yield change] `CollectCompost` rollback fix (#3)**: The hidden duplication bug (`research.md` risk #1, `problem.md` prerequisite) activates on any output > 1. Must be fixed independently — no dependency on growth hook, and no reason to delay.
- **[NEEDS CLARIFICATION — must resolve before touch] Play-test gate (#4)**: Launch the mod (with `GamePath`), confirm compost loop (craft → place → load → advance 2 days → collect), observe overlay/tooltips/hoe, then apply growth hook to one tile and observe change. Document in `SC-008` format. The environment (`.git/index` broken, `COMPLETION_SUMMARY.md` false, 7 DLLs in repo) should be repaired first.
- **[NEEDS CLARIFICATION — must resolve before multi-mod claim] `ReGen Valley` interaction (#5)**: If both mods modify `Crop`, is the composition multiplicative (safe) or suppressive (one wins)? The `problem.md` notes this is out of scope for Option A but must be documented — the `specify` stage needs the rule.

**Revisit stage**: `/speckit.assess.research slug=soil-health-to-crop-growth` — specifically for the **spike (#1)** and the **manual-run gate (#4)**. Once both return `adequate`, retest the scorecard; if risk posture improves to `adequate` (mitigated), the verdict converts to `go`. The `intake.md` and `problem.md` do not need to be revised — only the evidence for #1 and #4 is missing.

---

## If go — Handoff to `/speckit.specify` (documented, not executed — blocked by clarifications)

- **Problem**: Soil health (0–100, per-tile, persistent, displayed) is measured and compost-adjustable but never affects crop growth, harvest quality, or extra-yield — all 18 unimplemented roadmap mechanics depend on this connection; 2 features exist; 4 of 13 core promises exist.
- **Chosen approach**: **Option A — Small** (`concept.md`): `ISoilGrowthService` + constant `SoilGrowthMultiplier` per `HealthCategory` band + hook (`Crop.newDay` or `HoeDirt.GetFertilizerSpeedBoost()`) + pure-logic tests + vanilla-parity test on Medium band; **no new asset/item/recipe**; `CollectCompost` rollback fixed first; bands reconciled.
- **In scope / out of scope** (inherited from `concept.md` §Out of Scope + `problem.md` non-goals):
  - **In**: hook only; constant; pure-logic tests; band reconciliation; rollback fix.
  - **Out**: No new `Data/Crops` / `Data/Objects`; no `IMulchApplicationService`; no `ICompanionPlantingService`; no NPC / welcome kit / cistern; no Three Sisters / green-manure / rotation / Joja / pharmacy / co-op / ICLF / greywater / fair / CSA / kitchen / sovereignty; no `ContentPatcher`; no `config.json`; no `gmcm-config-menus`; no multiplayer transport change; no teaching-material redesign (Three Sisters / insect hotels must stay caveat-only; see `research.md` §C).
- **Success metrics**: 4 measurable (`problem.md`): growth-speed change on Poor/Live; harvest-quality roll change; extra-yield per-tile; manual play-through (T034 / SC-008). All except last verify by `dotnet test`; last requires `GamePath` launch.
- **Carried-forward open questions** (5 — must answer before `specify` writes tasks): hook path (#1); band scale (#2); extra-harvest mechanism (#3); play-test gate (#4); `ReGen Valley` interaction (#5).
- **Assumptions to validate during spec** (6 — from `concept.md`): (1) public-field hook works; (2) vanilla composition works; (3) band decision is design, not evidence; (4) competitor interaction is spec-level note; (5) manual play-test achievable; (6) rollback fix independent.

---

## Audit Trail

- `intake.md` (48l) → `research.md` (97l, 15 primary sources, `Evidence Against` present, 5 gaps) → `problem.md` (63l, 7 clarifications, strong/adequate/low confidence tagged) → `concept.md` (100l, 3 options, recommendation A, 10 exclusions, 6 assumptions, 4 rabbit holes) → **this `decision.md`** (verdict `needs-clarification`, not `go`, with documented reason, 5 blocking questions, handoff to `specify` preserved, no source modified, no architecture proposed).
- Previous commits: `5ac6576` (ROADMAP status annotated); `f3869ac` (research + false-citation fix) — both loose-ref only (`.git/index` broken — plumbing used for commit, not for push).
- No `specify/` source edited; no `.github/workflows/` edited; no `.specify/extensions.yml` changed; `assess` stage registered, used correctly.

---

*Verdict: `needs-clarification`. The idea is sound, the evidence supports Option A, the competitive product (`ReGen Valley`) validates the premise, the agronomic grounding corrects the briefing's overstatements — but the validation gate is open, the environment (index + DLLs + false docs) needs repair, and 5 clarification questions must be answered. The pipeline does not kill the idea; it holds the hand-off until the gate closes. The record stays for revisiting.*
