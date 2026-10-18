## Clarification Batch — Feature 005 Soil Health → Crop Growth

**Context:** Structured ambiguity scan completed against taxonomy (Functional Scope / Domain & Data / Interaction / Non-Functional / Integration / Edge Cases / Constraints / Terminology / Completion). 8 of 9 clarification questions from assessment resolved (Q1 hook / Q2 scale / Q3 growth / Q4 quality-extra / Q6 growth-application / Q7 chunk-guard / harvest-feedback Q3-A4). 1 deferred (Q5 rollback / SC-008). Performance contingency (Q7) fully specified but unverified/unimplemented in code. Two clarification candidates could not be solved autonomously from agent evidence (rollback gate requires user decision; performance mechanism requires user confirmation of contingency activation). Harvest feedback (Q3/A4) resolved by agents (Option A: floating-text reuse, new harvest-intercept hook deferred to plan, zero new assets/i18n keys).

Agent execution notes: rollback deep-dive completed (`DEFERRED_SC008_EXPLANATION.md`); performance deep-dive completed (`subagent-response-q7.md`); harvest-feedback deep-dive completed (`/tmp/report_harvest_feedback.txt`). Two `/research` agents failed (model unavailable: `command-code-zdr/inclusionai/ling-3.1-flash:free` unavailable for both rollback and performance research). Evidence from completed agents incorporated below. No `before_clarify` hooks registered (`.specify/extensions.yml` has only `after_analyze` hook). No `after_clarify` hooks registered. Proceeding to user batch presentation per instruction.

---

### Question 1 — Deferred rollback verification gate (SC-008 / Q5 / FR-005)

**Question:** Should the deferred rollback verification gate (`SC-008` from spec 004 — `CompostingBinService` manual play-through + `StillConsumesBin` automated test) be treated as an absolute prerequisite that blocks all feature 005 yield-activation (`FR-001`, `FR-002`, `FR-003`), allowing concurrent work on growth/harvest mechanism design only after it passes? (FR-005 / Dependencies / SC-008 / Q5)

**Why it matters:** This is the dominant risk (`problem.md` #1; `FR-009` line 82). Without `SC-008` green, any `Crop.phaseDays` touch (`FR-001`) or harvest-quality/extra-yield activation (`FR-002`/`FR-003`) activates unverified code — the hidden duplication bug (`CompostingBinService.cs` partial-delivery loop at `outputCount > 1` with no rollback) could corrupt farm state. The spec explicitly states: "both required — do not proceed to plan/tasks until green" (`Dependencies` 136-137; `FR-005` 78). It also determines whether manual verification (`SC-008` / T034) remains the only activation gate (`FR-009` 83) or whether a reduced gate could suffice.

**Findings:** Codebase agent (`DEFERRED_SC008_EXPLANATION.md`) confirms: `CompostingBinService.cs` 189-264 (`CollectCompost`) hardcodes `outputCount = 1` (line 211) and consumes bin state only after `TryAddItem` (229-243), masking the duplication hazard (`briefings/soil-tier-outcomes-harvest-feedback.md` 89-97; `research.md` 92-97). `ModController.cs` rollback patterns (566-598) do not cover inventory-full partial delivery. `SC-008` verification (`spec.md` 114, `Dependencies` 136-137) requires both manual `GamePath` play-through (apply compost, observe growth difference, save/reload) and automated `StillConsumesBin` (`CompostingBinServiceTests.cs` 339). The `ReGen Valley` interaction (`Assumes` 145) is documentation-only (`Q5` 31-32; `Clarifications` 31), confirmed — no new mechanism needed. Zero in-game executions (`problem.md`) mean `Crop.phaseDays` modification remains unverified. `/research` agent failed (model unavailable); no scientific research evidence available for software rollback verification specifically — engineering evidence from codebase only.

**Options:**

| Option | Description |
|--------|-------------|
| A (Recommended) | Treat `SC-008` as absolute blocker: no `FR-002`/`FR-003` activation until rollback verified; design work on `Crop.newDay` / `Crop.harvest` hooks can proceed in parallel, but no yield-increase activation. Matches `Dependencies` 136-137 and `FR-005` exactly. |
| B | Relax `SC-008` to concurrent prerequisite: activate yield features as `SC-008` verification proceeds in parallel; accept duplication risk during verification window. Reduces schedule dependency but violates `FR-005` safety gate. |
| C | Defer `SC-008` to post-plan verification only: design/spec complete without rollback confirmation; rely solely on automated tests (`StillConsumesBin`) and skip manual `GamePath` verification. Contradicts `FR-009` (manual play-through required) and `SC-008` (manual is the dominant verification path). |

**Recommended:** Option A — `SC-008` must pass first; this aligns with the explicit verification gate (`Dependencies` 136-137; `FR-005` 78; `Open Questions` 156), protects against the documented duplication bug, and preserves the manual observation requirement (`FR-009` 82; `SC-008` 114) that no automated test can replace. Concurrent design work on hook mechanism (`Q1` Option A) is permitted; activation is not.

---

### Question 2 — Performance contingency mechanism activation (Q7 / FR-001 / Dependencies)

**Question:** Should the performance contingency mechanism (`FR-001` line 74; `Dependencies` 139; `Clarifications` Q7 line 34) — combined chunk phase (~1,000 tiles/chunk, ~2 ms budget, prime-modulus stride 61, spread across ticks, `ExecuteWithConcurrencyGuard` at `OnDayStarted`) — be implemented as the design decision for the `DayStarted` growth scan, accepting that no batch mechanism exists today and the contingency activates only when the linear scan exceeds the `<5 ms` target at 40,000 tiles? (Performance / Deployment / FR-001)

**Why it matters:** This defines whether the `DayStarted` linear scan (`SoilDecayService.cs` 86; `FR-001`) has a bounded fallback. Without the contingency, host stalls at extreme tile counts (`≤40k`). It also defines whether `ExecuteWithConcurrencyGuard` is added at `OnDayStarted` (matching the `OnSaveLoaded` atomic-flag state-machine `Interlocked.CompareExchange` pattern at `ModController.cs` 2100 / 978 / 1019). The spec explicitly notes: "no batch mechanism exists today — this is a specification-level contingency decision" (`FR-001` 74; `Clarifications` Q7 34).

**Findings:** Codebase agent (`subagent-response-q7.md`) confirms: ONLY linear scan exists (`SoilDecayService.cs` 86-105: plain `foreach` over `terrainFeatures.Pairs`, no chunking, no tick-spread, no stride mechanism). `ModController.OnDayStarted` (1719-1778) applies NO `ExecuteWithConcurrencyGuard` (guard exists at 2100 for `OnSaveLoaded`/`OnSaving` only). `MaxTilesPerSave=30000` (`Constants.cs` 14) is a persistence cap, not a chunk mechanism. `/research` agent failed (model unavailable); available research is engineering practice (`SoilDecayService` linear pattern at `spec-002` `:113`/`:176`; SMAPI tick-rate context ~60 ticks/sec). `research.md` (agronomy / `Lal 2020` / `Cornell`) supports multiplicative tiered model (`Clarifications` Q3 29) but does not address performance chunking specifically. The contingency parameters (`~1,000` tiles/chunk, `~2 ms` budget, stride `61`) are specification-level design choices (`Clarifications` Q7 34) — no code implementation exists to verify them, but the design is unambiguous (`FR-001` 74; `Dependencies` 139; checklist `growth-performance.md` CHK-303/304/1401 confirms completeness).

**Options:**

| Option | Description |
|--------|-------------|
| A (Recommended) | Confirm the defined contingency (Option A from Q7): combined chunk phase, ~1,000 tiles/chunk, ~2 ms budget, prime-modulus stride 61, spread across ticks when linear exceeds `<5 ms`; add `ExecuteWithConcurrencyGuard` at `OnDayStarted`. Matches spec exactly (`FR-001` 74; `Clarifications` Q7 34; `Dependencies` 139). Implementation deferred to plan/tasks; design locked. |
| B | Simplify to linear-only: remove chunk contingency from spec; rely solely on `<5 ms` linear contract with no fallback. Reduces complexity but eliminates bounded behavior at 40k tiles — contradicts `FR-001` contingency clause. |
| C | Replace combined chunk with separate decay/growth chunk phases: separate `SoilDecayService` chunks from `Crop.newDay` chunks. Increases complexity (two chunk mechanisms); contradicts Q7's "combined chunk phase" confirmation. |

**Recommended:** Option A — the contingency mechanism is fully parameterized in the spec (`FR-001` 74; `Clarifications` Q7 34; `Dependencies` 139), consistent with `spec-002` performance contract (`≤10k` target / `≤40k` extreme), and matches the existing `SoilDecayService` linear pattern (`:86`). Implementation is deferred (checklist `growth-performance.md` CHK-1401 notes this as expected gap); no design change is needed. Confirming it locks the architecture before plan/tasks begin.

---

### Additional note — Harvest feedback (Q3/A4) — RESOLVED

**Status:** Both codebase (`/tmp/report_harvest_feedback.txt`) and research (`research-harvest-feedback.md`) agents confirm Option A: message shown when tier modifies harvest outcome; reuse existing `floating-text` / `TemporaryAnimatedSprite` (`VisualizationService.cs` 247-285; `CompostApplicationService.cs` 178-191); no new `Assets/*.png`, no new `i18n` key (`A4` 89; `Non-Goals` #6 126; contradiction with `soil-tier-outcomes-harvest-feedback.md` resolved by reuse). **New dependency identified:** harvest-intercept hook at `Crop.harvest` does not exist (`ModController` has no harvest hook; `LivingRoots/*.cs` has zero `Crop.harvest` references beyond `message.harvest.inventory-full` at `Constants.cs` 56). The hook is deferred to plan/tasks (`Clarifications` Q3/A4 33-34). **No user clarification needed** — the design is resolved; the dependency is noted for plan/task decomposition.

---

### Next step (per clarification loop rules)

**Requested action:** Confirm which question to address interactively (per sequential loop: exactly one at a time; `<=5` total). I recommend starting with **Question 1 (Rollback verification gate)** because it is the absolute prerequisite (`Dependencies` 136-137; `FR-005` 78) and determines whether any yield feature activation is permitted; once resolved, **Question 2 (Performance contingency)** confirms the architecture before `/speckit.plan`.

If you confirm the recommendations (`"yes"` / `"recommended"`), I will integrate each answer into `spec.md` (under `## Clarifications`, `### Session 2026-10-16` or new session) and update the checklist (`checklists/requirements.md`) before proceeding. If you provide a different answer, I will validate it against the `<=5` word constraint (if short-answer style) or map it to the option table, disambiguate if needed, and then integrate.

**Signals that terminate the loop:** If you reply `"done"`, `"good"`, `"no more"`, or `"proceed"`, I will stop asking, record the final state, and generate the Completion Report with coverage summary, checklist status, sections touched, and next command (`/speckit.plan` or re-clarify recommendation).

Please respond with either: the option letter (`A`, `B`, `C`), `"yes"` / `"recommended"`, or a short answer (`<=5` words). Which question first — rollback verification (`A` recommended) or performance contingency (`A` recommended)?