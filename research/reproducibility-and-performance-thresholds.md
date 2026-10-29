# Reproducibility Measurement & Performance Thresholds for SMAPI/Stardew Valley Mods

**Status:** Research deliverable (informational — no code edited)
**Date:** 2026-10-25
**Context:** `stardew-raizes-vivas` (SMAPI / Stardew Valley mod, .NET 6). Resolves two open gaps flagged by `specs/009-end-to-end-game-verification` checklists: CHK006/CHK022/CHK024 (≤30 min reproducibility has no measurement method) and CHK028/CHK036 (performance requirements for the DayEnding growth-modifier tile scan are unspecified). Companion to `research/verification-evidence-format-gate3.md` (evidence format) and `research/subagent-evidence-thresholds-report.md` (threshold conventions).

---

## 0. Executive summary (5 recommendations)

1. **Define "reproducibility time" as a bounded, stopwatch-timed interval with explicit inclusions/exclusions** — start at the moment the verifier opens the documented checklist (before-state captured), stop at the moment the verdict is recorded. Exclude environment setup that is not feature-specific (SMAPI install, .NET SDK). Include all feature-specific steps (till, place bin, apply waste, advance days, harvest, save, reload, compare). Target ≤30 min for a single feature; if it exceeds 30 min on two consecutive runs, the protocol must be simplified or the feature re-scoped (not the threshold relaxed).
2. **Assign a single named timer (the verifier) using a wall-clock stopwatch, not in-game time.** In-game days are not a proxy for wall-clock reproducibility time (a "3-day wait" can be 30 seconds of in-game sleep or 3 real days if the player plays normally). Wall-clock seconds are the only reproducible unit.
3. **Quantify the DayEnding growth-modifier performance budget at ≤5 ms for ≤40,000 tilled tiles**, consistent with the existing `spec-002`/`spec-005` contract (linear scan, no chunking needed below the threshold; chunking contingency activates only above). Reuse the established `MaxTilesPerSave = 30,000` / `MaxTilesPerLocation = 500` / `MaxLocationsPerSave = 50` DoS ceilings as the worst-case input bound.
4. **Adopt the existing visualization frame budget (16.67 ms @ 60 FPS, `MaxRenderTimeMs`) as the cross-cutting performance oracle**, with the DayEnding scan measured against the stricter 5 ms sub-budget (not the full frame budget), because DayEnding runs inside the same synchronous game tick that also drives `newDayAfterFade`/`Crop.newDay` and other mod day-start handlers.
5. **Anchor both measurements to standards correctly**: IEEE 1012-2024 (V&V task reports, anomaly/deviation policy) and ISO/IEC/IEEE 29119-3 (test procedure spec, traceability) define **evidence and procedure structure**, not numeric thresholds. Numeric thresholds (30 min, 5 ms, 16.67 ms) are **project conventions** that must be labeled as such (following the precedent already set for `DegradationRecoveryThreshold` "NOT A STANDARD"), with the convention origin documented.

---

## 1. Question 1 — How "reproducibility time" is measured in verification protocols

### 1.1 What the standards actually say (and do not say)

**IEEE 1012-2024** (System, Software, and Hardware V&V) and **ISO/IEC/IEEE 29119** (Software Testing) — the two standards the project already cites for Gate 3 evidence format — define *reporting structure* and *traceability*, not numeric reproducibility-time limits.

- **IEEE 1012 §3.6** reporting structure: Task Report (per-task interim results/status), V&V Phase Summary Report, Anomaly Report, V&V Final Report. Administrative procedures (§3.7): anomaly reporting/resolution with criticality levels and timelines; task iteration policy (re-perform when inputs change); **deviation policy** (documented departure with rationale, effect on quality, approver); control procedures (configuration/data management status). None of these prescribe a minute count.
- **ISO/IEC/IEEE 29119-3** (Test Documentation): Test Plan, Test Design Specification, Test Case Specification, **Test Procedure Specification** (steps to execute + pass/fail criteria), Test Report (summary of execution), Test Log (chronological record), Traceability (test basis → test cases → results). The "Test Procedure Specification" is where reproducibility time lives — as *estimated duration* and *resource requirements* fields, not as a pass/fail threshold.
- **NIST AI RMF 1.0 §9–11** (cited via ITEA 2026): evidence adequacy/completeness/traceability — again structural, not temporal.

**Conclusion:** "≤30 min reproducibility" is not a value any standard sets. It is a **project-defined service-level objective** (SLO) for contributor ergonomics, and must be treated as a convention, measured and reported, not as an externally validated constant. This mirrors the project's own precedent for `DegradationRecoveryThreshold` (labeled "NOT A STANDARD / judgement call").

### 1.2 Standard methods for measuring reproducibility time

From the triangulation literature (BetterEvaluation, M&E Studio), JOSS/NeurIPS/AAAI reproducibility checklists, and the project's own `research/verification-evidence-format-gate3.md` §3.1, the established method components are:

**A. Start point — "time zero"**
- The moment the verifier **opens the documented checklist/contract** (`specs/<slug>/checklists/gate3-verification-contract.md`) in a state where the before-snapshot has not yet been captured. Everything before this (SMAPI install, .NET SDK, game launch to title screen, mod build) is **environment setup**, which is captured once in the Environment block but is **not counted** in reproducibility time — otherwise the metric would be dominated by machine-specific install friction and would not measure protocol reproducibility.
- Rationale: JOSS and NeurIPS reproducibility checklists separate "environment readiness" (prerequisite, captured once per machine) from "procedure execution" (the reproducible part). IEEE 1012's control procedures (§3.7) similarly distinguish configuration/data-management status from task execution.

**B. End point — "verdict recorded"**
- The moment the verifier writes the verdict line (`verified` / `partial` / `failed`) plus the signal-by-signal table in the evidence file (`gate3-evidence-<date>.md`). Hashing/committing artifacts is part of execution and is counted; CI push is not (it is asynchronous and environment-dependent).

**C. What activities count (inclusions)**
- Capturing the before-save snapshot (path + SHA-256) and recording its hash.
- Running the numbered procedure steps (till, place bin, apply waste, advance days, harvest, save, reload, compare).
- Capturing the after-save snapshot + SMAPI log excerpt + (optional) visual.
- Computing the structured save delta (ADDED/REMOVED/CHANGED/TYPE) with attribution.
- Filling the evidence template (environment, inputs, procedure table, signals, anomalies, residual risks, recommendation, verdict).
- Any re-run of a step because a signal diverged (divergence handling is part of the protocol, not an exception).

**D. What does not count (exclusions)**
- Environment setup: SMAPI/.NET/game install, `STARDEW_GAME_PATH` resolution, mod build/deploy (these are captured in the Environment block once per machine, not timed).
- Time spent reading background documentation (the contract is assumed already read before time-zero; reading it is part of the "design-in" phase, not execution).
- Time spent debugging an unexpected crash that is **not** a protocol step (recorded as an anomaly/issue, not counted toward the 30-min SLO; if crashes recur, the protocol is not reproducible and the SLO is violated by definition).
- Waiting for in-game time to pass when the verifier is not actively observing (e.g., sleeping through a day) — count only active observation/interaction time, not passive wait. Document the passive-wait duration separately in the evidence file so that "wall-clock reproducibility time" and "in-game days elapsed" are never conflated.

**E. Who times**
- The verifier (the person running the protocol) times themselves with a wall-clock stopwatch (phone, `time` command, or a simple `Stopwatch` in a helper script). Self-timing is acceptable for a small team because the metric is a *process SLO*, not an audit of individual performance. IEEE 1012's anomaly/deviation policy (§3.7.3) is the right tool if a run exceeds the budget: record the overrun as a deviation with rationale and approver, do not silently drop it.
- For the scripted pipeline (Q10→A, local Xvfb runner), timing is captured automatically by the script (`date +%s` before/after, or `Stopwatch`), which removes observer variance and is the eventual source of truth for the automation track.

**F. Environmental conditions (captured, not controlled)**
- OS + version; CPU architecture; .NET SDK version (`dotnet --version`); SMAPI version; Stardew Valley version; `STARDEW_GAME_PATH` (set/unset, resolves); mod commit SHA + branch; save ID + save file names; observer identity + date (UTC, ISO 8601 with offset); non-default settings (mods loaded, debug flags).
- **Controlled condition:** a single dedicated test save profile (SMAPI profile isolation per `VERIFICATION_PROTOCOL.md` §6). Multi-mod interactions are a known confound; the protocol should be run on a clean profile with only Living Roots + its declared dependencies loaded, and any deviation (extra mods) recorded.
- **Not controlled (recorded only):** machine load, Stardew version patch, in-game season/weather (these affect vanilla growth rates and thus the observation, but not the protocol's reproducibility).

**G. Pass/fail rule for the 30-min SLO**
- A run is "reproducible within budget" if wall-clock execution time (start→verdict) ≤ 30 min. If a run exceeds 30 min, the evidence file records the actual duration and the cause (protocol complexity, environment, anomaly). Two consecutive over-budget runs on the same protocol version trigger a mandatory protocol-simplification review (IEEE 1012 task iteration policy §3.7.2) — the threshold is not relaxed; the steps are reduced (e.g., defer the vanilla control run, which is already classified as an "upgrade" for the automation track, not a `verified` requirement).

### 1.3 Practical recommendation for the small team

Adopt a one-line addition to the `test.md`/evidence template (already present in `research/verification-evidence-format-gate3.md` §5.3): add a **Timing** block:

```
## Timing
- Time zero (UTC): <ISO 8601>
- Verdict recorded (UTC): <ISO 8601>
- Wall-clock reproducibility time: <mm:ss>  (target ≤30:00)
- Passive in-game wait (not counted): <mm:ss>  (e.g., sleeping through 3 days)
- Environment setup (not counted): <mm:ss>  (captured once per machine)
- Over budget? yes/no — if yes, cause + deviation record per IEEE 1012 §3.7.3
```

This is the missing measurement method that closes CHK006/CHK022/CHK024. It requires no new tooling (a stopwatch + one template block) and is consistent with the existing evidence template.

---

## 2. Question 2 — Performance thresholds/budgets for game mod tick-time

### 2.1 Existing project thresholds (already established, reusable)

| Threshold | Value | Source | Applies to |
|---|---|---|---|
| Frame budget (60 FPS) | 16.67 ms/frame | `ModConstants.MaxRenderTimeMs = 16.67`; spec 001 SC-002 | All per-frame work (render, UpdateTicked) |
| Visualization sub-budget | <4 ms (<25% of frame) | `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` §506 | Overlay rendering only |
| Visualization degradation threshold | 1,000 visible tiles | spec 001 FR-009; `DegradationTileThreshold = 1000` | Pattern rendering disable point |
| DayStarted growth scan (target) | ≤5 ms @ 40,000 tilled tiles | spec 005 Q7 clarification; spec 002 line 176 | `ApplySoilGrowthModifier` linear scan |
| DayStarted chunking contingency | ~1,000 tiles/chunk, ~2 ms/chunk, prime-modulus stride (e.g., 61), spread across ticks if total >5 ms | spec 005 Q7 clarification | Activates only above 5 ms threshold |
| SoilDecayService scan | 10,000 tiles target; 40,000+ in 2–4 ms | spec 002 briefings | Daily decay at day start |
| DoS ceilings (worst-case input bound) | `MaxTilesPerLocation = 500`; `MaxLocationsPerSave = 50`; `MaxTilesPerSave = 30,000` | `ModConstants` lines 20–28 | Input validation, not performance targets |
| SMAPI game tick rate | ~60 ticks/sec (synchronous) | spec 005 Q7 research (disco-science-lite) | Context: DayStarted/DayEnding are sync, not async |

### 2.2 What the DayEnding scan specifically needs

The plan.md for spec 009 already states the performance goal: "Growth modifier must scan all farm-location tiles within game tick budget (no chunking needed for typical farm sizes < 40k tiles)." The two checklists (growth-modifier-correction CHK028, requirements-quality CHK036) correctly flag this as a gap — the goal exists but is not quantified as a measurable threshold.

**Recommended quantification (closing the gap):**

- **Primary budget: ≤5 ms wall-clock for the full DayEnding scan**, measured from entry to `ApplySoilGrowthModifier()` (or its corrected successor) return, on a save with up to 40,000 tilled tiles across all farm locations (`Farm`, `Greenhouse`, plus the player's current location per the `farmLocationNames` set in `OnDayStarted`).
  - Justification: this reuses the already-established `spec-002`/`spec-005` contract (≤5 ms @ 40,000 tiles, linear scan, no chunking needed below threshold). Consistency with an existing, research-backed number is more defensible than inventing a new one.
  - The scan is O(n) over `terrainFeatures` per location (dict lookup + `is HoeDirt` + `crop != null` + `GetSoilHealth` cache lookup), matching the `SoilDecayService` pattern that already achieves 40,000 tiles in 2–4 ms. The growth modifier's per-tile work is comparable (one health read + one branch + one `dayOfCurrentPhase` write), so the same budget applies.

- **Sub-budget relationship to the frame budget:** DayEnding runs synchronously inside the game tick (SMAPI calls `EventManager.DayEnding.RaiseEmpty()` before vanilla `newDayAfterFade`/`Crop.newDay`). It shares the tick with `CompostingBinService.ProcessDayStart` and `SoilDecayService.ProcessDayStart` (both already loop over `farmLocationNames`). Therefore the DayEnding scan's 5 ms is a **sub-budget of the per-tick work**, not of the 16.67 ms frame budget. The frame budget (16.67 ms) applies to `UpdateTicked`/`RenderedWorld` (rendering), which is a separate code path. Conflating the two (as the spec 009 plan's "game tick budget" phrasing risks) would overstate the available headroom.

- **Measurement method:** Use `System.Diagnostics.Stopwatch` around the scan (start at method entry, stop at return, including the per-location loop and the terrain-features iteration). Log the elapsed ms at `LogLevel.Trace` (not `Info`/`Warn` — the project's anti-pattern is exposing raw timing in player-facing logs; trace is the correct level for perf diagnostics). For the scripted pipeline, capture the trace line and assert ≤5 ms.

- **Chunking contingency (already specified, keep as-is):** If a single run exceeds 5 ms, activate the spec-005 Q7 contingency: chunk size ~1,000 tiles/tick, ~2 ms/chunk budget, prime-modulus stride (e.g., 61), spread across ticks. This is a defensive measure; for typical farms (<40,000 tiles) it should not activate. The contingency must be verified by a test that injects a 40,001-tile synthetic location (the project already has `ThreadSafeGameLoopEventsStub` for simulating game-loop events without a booted game).

- **Worst-case input bound:** The DoS ceilings (`MaxTilesPerSave = 30,000`, `MaxTilesPerLocation = 500`, `MaxLocationsPerSave = 50`) define the maximum processable input. The performance budget (5 ms @ 40,000) is deliberately set **above** the DoS ceiling (30,000) so that the performance contract holds for all inputs the security layer permits, with margin. This is the correct relationship: security bounds input size; performance bounds execution time for inputs within that size.

- **What "within game tick budget" must NOT mean:** It must not be interpreted as "within 16.67 ms" (the frame budget), because that would allow a 16-ms DayEnding scan that starves the render path. It must not be interpreted as "within one SMAPI tick" without a number, because "one tick" is not a measurable quantity for a reviewer. The 5 ms number is the measurable, testable form.

### 2.3 Cross-cutting performance oracle summary

For the small team, the practical rule is:

> **Every synchronous game-loop handler (DayEnding, DayStarted, UpdateTicked, RenderedWorld) must declare a numeric budget in ms, measured with `Stopwatch`, logged at `LogLevel.Trace`, and backed by a test that injects the worst-case input size.** The frame budget (16.67 ms) is the ceiling; sub-budgets (5 ms for day-start scans, 4 ms for visualization, 2 ms per chunk) are the enforceable contracts. No handler may consume the full frame budget.

This is consistent with the project's existing practice (`MaxRenderTimeMs = 16.67`, `<4 ms` visualization, `≤5 ms` day-start scan) and with the SMAPI community norm (mods that block the game thread for >~5 ms cause visible stutter at 60 FPS, since a single long tick delays both logic and render).

### 2.4 Note on the 40,000-tile figure

The "40,000 tiles" figure originates in spec 002 (soil decay) and spec 005 (growth), not in Stardew's actual map sizes (a standard farm is ~2,500–5,000 tiles; modded/expanded farms can reach 10,000–40,000). It is a **stress-test input size**, not a typical farm size. Performance claims should always state the input size they were measured at (e.g., "≤5 ms @ 40,000 tiles"), never "≤5 ms" alone. This follows the project's own convention of documenting measurement conditions (see `spec 002 briefings/soil-decay-mechanics.md`: "alvo 10k tiles, medido 40k em ~2–4ms").

---

## 3. Question 3 — Standards that define reproducibility measurement

### 3.1 IEEE 1012-2024 (System, Software, and Hardware V&V)

- **What it defines:** V&V as providing *objective evidence* that products conform to requirements and satisfy intended use. Structures V&V into integrity levels (1–4) with larger task sets at higher integrity. Gate 3 maps to medium-low criticality (user-facing gameplay effect), but the *claim* ("mod changes gameplay") is the asset being verified.
- **Reporting structure (§3.6):** Task Report → V&V Phase Summary Report → Anomaly Report → V&V Final Report. The project's three-artifact Gate 3 package (Verification Contract / Task Report / Phase Summary) mirrors this scaled to a feature.
- **Administrative procedures (§3.7):** anomaly reporting/resolution (criteria, distribution, authority, timelines, criticality — **critical anomalies must be resolved before proceeding**); task iteration policy (re-perform when inputs change); **deviation policy** (documented departure: task ID, rationale, effect on quality, approver); control procedures (configuration/data management status).
- **What it does NOT define:** numeric reproducibility-time limits, millisecond budgets, tile counts. Those are project conventions.
- **Relevant to the 30-min SLO:** the deviation policy (§3.7.3) is the right mechanism for recording an over-budget run — not a silent threshold relaxation.

### 3.2 ISO/IEC/IEEE 29119 (Software Testing)

- **What it defines:** a family (Parts 1–5 + 10) covering testing concepts, processes, documentation, techniques, keyword-driven frameworks. Part 3 (Test Documentation) is the artifact spec.
- **Relevant artifacts:** Test Plan, Test Design Specification, Test Case Specification, **Test Procedure Specification** (steps + pass/fail criteria — this is where reproducibility time is estimated as a *resource requirement*, not a pass/fail threshold), Test Report, Test Log, Traceability.
- **Key contribution to Gate 3:** *no `verified` claim without a trace link from requirement → test basis → evidence artifact → result.* This is the single most important 29119 contribution — it is what makes "verified" auditable rather than asserted.
- **What it does NOT define:** numeric reproducibility-time limits or performance budgets. ISO 29119-3's Test Procedure Specification includes *estimated duration* and *resource requirements* fields, but these are estimates for planning, not pass/fail criteria.

### 3.3 NIST AI RMF 1.0 (2023, NIST AI 100-1) — via ITEA 2026

- Evidence quality: an artifact supports a claim only if it is **adequate** (matched to claim), **complete** (covers dimensions), and **traceable** (provenance + audit trail + independent review). Save-only or visual-only evidence fails 2+ of these — the exact false-green risk documented in `problem.md` and `COMPLETION_SUMMARY.md`.
- Pollner (ITEA J. 47(2), 2026, DOI 10.61278/itea.47.2.1008): false confidence from metric-only evidence; ~1/3 incorrect assertions (Bhatia et al. 2023); independent judgment required for evidence authorization.

### 3.4 What "reproducibility" means in the scientific/computational literature (cited in project research)

- **JOSS review checklist / NeurIPS / AAAI reproducibility checklists:** environment, data, code, methods, analysis steps; flag missing documentation. The project's reproducibility header (§3.1 of `research/verification-evidence-format-gate3.md`) is derived from these.
- **PLOS Computational Biology 2014** (Rule of Three / automation norm, DOI 10.1371/journal.pcbi.1003506): "once you have done the same thing twice already, automate it" — applies to the automation gate (≥2 consistent sessions), not to single-feature `verified`.
- **BetterEvaluation / M&E Studio (triangulation):** design-in, opposite weaknesses, divergence informative, audit trail. The 3-signal design (Crop.phase + SoilHealthState + SMAPI log) is validated by this framework.

### 3.5 Correct framing for the small team

The correct framing — which the project already partially applies (e.g., `DegradationRecoveryThreshold` labeled "NOT A STANDARD") but inconsistently (34/67 edges and ±1 delta were not labeled until `subagent-evidence-thresholds-report.md` recommended it) — is:

> **IEEE 1012 and ISO 29119 govern the *structure* of verification (reports, traceability, deviation policy, anomaly handling). They do not set numeric thresholds. The project's numeric thresholds (30 min reproducibility, 5 ms scan, 16.67 ms frame, 34/67 category edges, ±1 delta) are *project conventions* that must be labeled as convention-derived, with the convention origin documented, so that future maintainers do not mistake them for externally validated constants.**

This framing should be added to `ModConstants.cs` (for the performance constants) and to `spec.md`/`plan.md` (for the 30-min SLO), following the exact precedent set by `DegradationRecoveryThreshold`'s comment block (lines 57–66 of `Constants.cs`).

---

## 4. Recommended concrete changes (documentation only, no code)

1. **Add a `## Timing` block to the Gate 3 evidence template** (`research/verification-evidence-format-gate3.md` §5.3) with time-zero, verdict-recorded, wall-clock reproducibility time, passive-wait, environment-setup (not counted), and over-budget deviation record. This closes CHK006/CHK022/CHK024.
2. **Quantify the DayEnding scan budget in `spec 009 plan.md` Technical Context** as "≤5 ms wall-clock (Stopwatch) for ≤40,000 tilled tiles across all farm locations, per the spec-002/spec-005 contract; chunking contingency activates above 5 ms." This closes CHK028/CHK036.
3. **Add a `Stopwatch`-based trace log at `LogLevel.Trace`** in `ApplySoilGrowthModifier` (or its corrected successor), measuring entry-to-return including the per-location loop. Add a test injecting a 40,001-tile synthetic location (via `ThreadSafeGameLoopEventsStub` or a direct service test) asserting ≤5 ms.
4. **Label the 30-min SLO and 5-ms budget as conventions** in `spec.md`/`plan.md` with a comment block mirroring `DegradationRecoveryThreshold`'s "NOT A STANDARD" precedent, citing the convention origin (spec-002/spec-005 contract for 5 ms; contributor-ergonomics SLO for 30 min).
5. **Keep the frame budget (16.67 ms) and visualization sub-budget (4 ms) as the cross-cutting oracle** — do not create a third, conflicting "tick budget" number. The DayEnding scan's 5 ms is a sub-budget of per-tick work, not of the frame budget.

---

## 5. Citations

### Standards
1. **IEEE 1012-2024** (System, Software, and Hardware V&V) — IEEE SA. Reporting structure §3.6 (Task Report, V&V Phase Summary, Anomaly Report, V&V Final Report); administrative procedures §3.7 (anomaly reporting/resolution, task iteration, deviation policy, control procedures). https://standards.ieee.org/ieee/1012/7324/
2. **IEEE Std 1012-2016 / Cor1-2017** (superseded) — integrity levels, V&V task criteria, evidence sufficiency trade-off. https://raw.githubusercontent.com/Orthant/IEEE/master/1012-2016.pdf
3. **FIPS PUB 132** (IEEE standard for software V&V plans) — SVVP/SVVR format and content. https://nvlpubs.nist.gov/nistpubs/Legacy/FIPS/fipspub132.pdf
4. **ISO/IEC/IEEE 29119** (Software Testing) Parts 1–5 + 10; Part 3 (Test Documentation): Test Plan, Test Design Specification, Test Case Specification, Test Procedure Specification (steps + pass/fail criteria + estimated duration/resources), Test Report, Test Log, traceability. https://committee.iso.org/sites/jtc1sc7/home/projects/flagship-standards/isoiecieee-29119-series.html
5. **NIST AI RMF 1.0 (2023, NIST AI 100-1)** §§9–11 — evidence adequacy/completeness/traceability; builder/verifier separation. (Cited via ITEA 2026.)
6. **Pollner, A.** *Human Oversight for AI-Generated Test Artifacts*, ITEA Journal Vol 47 Issue 2 (June 2026), DOI 10.61278/itea.47.2.1008 — evidence quality, false confidence from metrics, oracle validation, ~1/3 incorrect assertions (Bhatia et al. 2023).

### Reproducibility / open source
7. **JOSS review checklist** — checklist-driven review; evidence reproducible from repository. https://joss.readthedocs.io/en/latest/review_checklist.html
8. **NeurIPS Paper Checklist Guidelines** — environment, data, code, methods, analysis steps. https://neurips.cc/public/guides/PaperChecklist
9. **AAAI Reproducibility Checklist.** https://aaai.org/conference/aaai/aaai-23/reproducibility-checklist/
10. **World Bank Reproducible Research Repository checklist.** https://worldbank.github.io/wb-reproducible-research-repository/reproducibility_package_checklist.html
11. **IEEE Author Center** — Research Reproducibility checklist. https://journals.ieeeauthorcenter.ieee.org/create-your-ieee-journal-article/research-reproducibility/
12. **PLOS Computational Biology (2014)** — "Ten Simple Rules for Effective Computational Research" (Rule of Three / automation norm), DOI 10.1371/journal.pcbi.1003506.

### Triangulation / verification
13. **BetterEvaluation / M&E Studio** — *Triangulation: Using Multiple Sources and Methods* (design-in, opposite weaknesses, divergence informative, audit trail). https://betterevaluation.org/methods-approaches/methods/triangulation
14. **Huang, Meyer, Oriol** — *Combining Tests and Proofs with Contracts for Better Software Verification*, CACM / arXiv:2601.16239 v2 (2026). https://cacm.acm.org/research/combining-tests-and-proofs-with-contracts-for-better-software-verification

### Save-file comparison
15. **Save Compatibility Lab** — snapshot digest (SHA-256), before/after comparison, two-pass check, numeric relative tolerance. https://bjorv.com/assets/save-compatibility-lab/docs/
16. **SaveDelta** (KanadeK) — before/after capture, structured diff, `--fail-on-change` CI gate. https://github.com/KanadeK/savedelta
17. **Zion Save Inspector** — save diff (ADDED/REMOVED/CHANGED/TYPE, dotted paths). https://ziongamesrpg.itch.io/zion-save-inspector

### Project-internal (primary sources)
18. `specs/009-end-to-end-game-verification/spec.md` — Gate 3 protocol, 3-signal mandatory, FR-001a/b/c, FR-002a/b, thresholds, replication scope.
19. `specs/009-end-to-end-game-verification/plan.md` — Performance Goals line 19 ("scan all farm-location tiles within game tick budget (no chunking needed for typical farm sizes < 40k tiles)"); Technical Context.
20. `specs/005-soil-health-to-crop-growth/spec.md` — Q7 clarification: ≤5 ms @ 40,000 tiles; chunking contingency (1,000 tiles/chunk, 2 ms/chunk, prime stride 61); SMAPI ~60 ticks/sec sync.
21. `specs/002-soil-decay-compost/spec.md` line 176 — 10,000 tilled tiles target, 40,000+ in <5 ms.
22. `specs/002-soil-decay-compost/briefings/soil-decay-mechanics.md` — 10k target, 40k in 2–4 ms, linear O(n), no spatial partitioning (YAGNI).
23. `specs/001-soil-health-visualization/spec.md` — SC-002: 60 FPS (16.67 ms) with 1,000 visible tiles; FR-009 degradation at >1,000 tiles.
24. `research/verification-evidence-format-gate3.md` — IEEE 1012/ISO 29119 evidence format; reproducibility header §3.1; save-delta protocol §2.
25. `research/subagent-evidence-thresholds-report.md` — threshold conventions; convention-origin documentation; replication scope.
26. `research/triangulated-verification-gate3.md` — 3-signal sufficiency; false-positive analysis; 4th signal before automation.
27. `VERIFICATION_PROTOCOL.md` — manual-first + multi-signal evidence; automation thresholds (reuse ≥3, stability ≥2); SMAPI-specific practices.
28. `LivingRoots/Constants.cs` (a.k.a. `ModConstants`) — `MaxRenderTimeMs = 16.67`; `DegradationRecoveryThreshold = 980` "NOT A STANDARD" precedent; `MaxTilesPerLocation = 500`, `MaxLocationsPerSave = 50`, `MaxTilesPerSave = 30,000`; `PoorModerateBandEdge = 34`, `ModerateHealthyBandEdge = 67`.
29. `LivingRoots/Controllers/ModController.cs` lines 1716–1765 — `ApplySoilGrowthModifier()` (current `DayStarted` scan; correction to `DayEnding` pending); `OnDayStarted` lines 1767–1825 (`farmLocationNames` set: current location + Greenhouse + Farm).
30. `.specify/memory/constitution.md` v2.0.0 — Gate 3 definition; Principle VI (multiplayer host-authority).
31. `AGENTS.md` — conventions, anti-patterns, CI governance (Build and Test dormant).

---

*End of research deliverable. No code edited; no commit made. Companion to `research/verification-evidence-format-gate3.md` and `research/subagent-evidence-thresholds-report.md`. Closes checklist gaps CHK006/CHK022/CHK024 (reproducibility measurement method) and CHK028/CHK036 (performance requirements for DayEnding tile scan).*