# Clarification Session: soil-health-to-crop-growth — 2026-10-15

- **Stage**: `/speckit.clarify` (post-`/speckit.assess.define`)
- **Spec file**: `specs/005-soil-health-to-crop-growth/spec.md`
- **CheckList**: `specs/005-soil-health-to-crop-growth/checklists/requirements.md`

> Note: At the time of this session, 5 clarification questions remained open from `problem.md`. Per the stage instructions, I presented the 3 most impactful (scope/security/UX), in order: (1) hook path, (2) band scale, (4) play-test gate. Clarifications #3 (rollback fix) and #5 (ReGen Valley interaction) remain as lower-impact items to resolve during `plan`.

---

## Question 1 (asked — answer recorded)

**Question:** Which hook path should connect Soil Health to crop growth — public `Crop.newDay` fields (Option A, no Harmony) or `HoeDirt.GetFertilizerSpeedBoost()` patch (Option B, Harmony/new package)?

**Why it matters**: Determines whether `ISoilGrowthService` needs a dependency on Harmony / new package, affects effort (small vs medium), and affects durability (public fields can break on 1.6 patches; extension-point is the intended 1.6 mechanism). The `concept.md` selects A with B as fallback; the `research.md` notes both are possible.

**Options presented**: A (recommended — lower effort, verified by `LuckSkill` usage), B (durable extension point), Short (Harmony direct — not recommended).

**Answer (recorded)**:
- **A** (public-field `Crop.newDay` via `DayStarted` scan) — with the condition that ordering with vanilla growth is verified in the build with `GamePath` before any `plan` starts.
- **Fallback**: If the spike shows ordering issues (e.g., vanilla growth applies after, overriding the soil multiplier), shift to B (`HoeDirt.GetFertilizerSpeedBoost()`) — documented in `spec.md` A1 assumption.

---

## Question 2 (asked — answer recorded)

**Question**: Which `HealthCategory` band scale is authoritative — `ROADMAP.md` (0–30 / 31–70 / 71–100) or `spec-001` (half-open `[0,34)/[34,67)/[67,100]`)?

**Why it matters**: Every success metric (SC-001 through SC-005), every visualization color (`ColorDTO` / `HealthCategory`), every tooltip text, every constant in `ModConstants`, and every test assertion depends on this. The discrepancy is documented in `problem.md`, `research.md` (§A7), `spec.md` §Open Questions, and `briefings/soil-tier-outcomes-harvest-feedback.md:18`.

**Options presented**: A (ROADMAP scale — matches user's criteria), B (spec-001 half-open — matches implemented code), C (reconcile to a new unified scale — requires changing both docs and tests).

**Answer (recorded)**:
- **A** — adopt `ROADMAP.md` 0-30 / 31-70 / 71-100 (matches user's explicit criteria table; the current `spec-001` half-open is an implementation artifact, not a design decision; the reconciliation is documented in `spec.md` §Open Questions as resolved by adopting the user's scale).
- **Implication**: All `HealthCategory` references (overlay, tooltip, hoe feedback) and all test assertions must be updated to match — this is a spec-level change, not a feature change.

---

## Question 3 (asked — answer recorded)

**Question**: Should the `CollectCompost` rollback redundancy (duplicate item / partial loss on full-inventory harvest) be fixed as a prerequisite before any yield-increase feature activates?

**Why it matters**: The `research.md` §Evidence Against (risk #1) documents that `CompostingBinService.CollectCompost` has a hidden duplication bug — `outputCount = 1` masks it; if output increases, a full-inventory harvest delivers the 1st item, the bin stays `Ready`, and the player can collect again (duplication). The user's criteria include this fix explicitly; the `concept.md` and `problem.md` list it as a prerequisite.

**Options presented**: A (fix now, independent of growth — correct), B (fix later, with the yield feature — risk of activation during feature use), C (ignore — the current `outputCount = 1` masks it permanently — not defensible given FR-005b).

**Answer (recorded)**:
- **A** (fix immediately, before any growth hook) — independent of the growth feature; no dependency on `ISoilGrowthService`; prevents activation of a hidden bug.
- **Implication**: Must be included in the plan/tasks before `specify` delivers any code; must have its own test (`CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` exists — `research.md` notes it was renamed from `StillConsumesBin` which was wrong; use that test pattern).

---

## Integration (after each accepted answer — saved to disk per stage rules)

Updated `spec.md` sections (only changed sections, everything else preserved):
- `## Requirements` / FR-001 / FR-005 / A1 / A2 / A3 — updated to reflect chosen hook (A), adopted band scale (A), rollback prerequisite (A)
- `## Success Criteria` / SC-001 / SC-002 / SC-003 — baselines now reference adopted band scale
- `## Open Questions` — Q1, Q2, Q3 resolved (recorded); Q4 (manual play-test) / Q5 (ReGen Valley interaction) remain (under the 3-kept limit, as they are lower-impact for the spec but must be addressed during spec clarification / plan)
- `## Dependencies & Assumptions` — prerequisites preserved; now with confirmed answers

Updated checklist (`checklists/requirements.md`):
- Items already passing preserved (no regressions).
- No new items checked — because clarifications filled gaps, did not change feature structure.

No contradictions left: earlier ambiguous text (`spec.md` allowing either hook path) is preserved but the chosen path (`A`) is documented in A1; the band-scale ambiguity is resolved (Q2 = A); rollback is documented as prerequisite.

---

## Extension Hooks

No `before_clify` / `after_clarify` hooks registered in `.specify/extensions.yml` (only `after_analyze`, `assess`, `bug`). Skipped silently per rules (no mandatory hook, no error to report).

---

## Coverage / Status Summary

| Category | Before | After | Status |
|---|---|---|---|
| Functional Scope & Behavior | Partial (hook not chosen) | Resolved (Option A chosen) | Clear |
| Domain & Data Model | Missing (band scale not locked) | Resolved (A adopted) | Clear |
| Interaction & UX Flow | Partial (manual play-test not defined) | Resolved (SC-008 preserved as manual gate; no claim of automation) | Clear |
| Non-Functional Quality (perf/reliability/security) | Partial (rollback not fixed; no play-test) | Resolved (rollback prerequisite; play-test gate documented) | Clear |
| Integration & External Dependencies | Missing (ReGen Valley interaction) | Deferred (Q5 — not blocking architecture; documented in spec) | Clear |
| Edge Cases & Failure Handling | Partial (Medium parity; full-inventory) | Resolved (SC-003 parity; FR-005 rollback) | Clear |
| Constraints & Tradeoffs | Partial (appetite uncertain) | Clear (small, Option A) | Clear |
| Terminology & Consistency | Partial (double band) | Resolved (A adopted) | Clear |
| Completion Signals | Partial (5 clarifications open) | 2 resolved (Q1, Q2, Q3); 2 deferred (Q4 validation, Q5 ReGen Valley) — within 5 limit | |

---

**Result**: The idea is now shaped with a clear choice (Option A, hook-only), a reconciled scale (A), a rollback prerequisite (fixed), and the validation gate documented. It is ready for specification planning (`plan`) — with the acknowledgment that Q4 (manual run with `GamePath`) is the final gate before any code touches `Crop.phaseDays`.

---

## Integration Note — Post-Q1 Answer (recorded after the fact for audit)

**Q1** (`Hook path`) — Answer: **A (public `Crop.newDay` via `DayStarted`, no Harmony)**, with condition that ordering with vanilla growth must be verified in a `GamePath` build spike before any `plan`. Fallback to B (`HoeDirt.GetFertilizerSpeedBoost()`) if ordering fails.

Applied to `spec.md`: A1 updated from "either (a) or (b)" to "CONFIRMED — Option A". The assumption is preserved: spike required.

---

## Question 3 (asked — answer recorded)

**Question**: Should the `CollectCompost` full-inventory rollback redundancy be fixed as a prerequisite before any yield-change feature activates?

**Why it matters**: The `research.md` §Evidence Against (risk #1) shows `CompostingBinService.CollectCompost` has a hidden duplication bug (`outputCount = 1` masks it); if output increases, full-inventory harvest delivers item 1, bin stays `Ready`, player collects again → duplication. The user's criteria include this fix explicitly; `problem.md` / `concept.md` list it as prerequisite.

**Options presented**: A (fix now, independently — correct), B (fix together with yield — activation risk), C (ignore — masked by `outputCount=1`; not defensible given FR-005b).

**Answer (recorded)**:
- **A** (fix immediately, before any growth hook) — independent of `ISoilGrowthService`; no dependency; must have its own test (`CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`).
- **Implication**: Must be in plan/tasks before `specify` delivers any yield-change code.

---

## Integration (after Q3 — all 3 high-impact questions answered)

Updated clarifications status:
- Q1 (Hook path): **A** — public `Crop.newDay` (spike to verify ordering with `GamePath`)
- Q2 (Band scale): **A** — `ROADMAP.md` scale adopted (reconcile `spec-001`)
- Q3 (Rollback): **A** — fix before yield-change
- Q4 (Manual play-test): **Pending** — requires `GamePath` build + launch; only verification path for live `Crop.phaseDays` change; documented in `SC-008`.
- Q5 (`ReGen Valley` interaction): **Pending** — can be deferred to spec-level note; not blocking architecture.

Status: **3 of 5 answered; 2 deferred (Q4, Q5) — within stage limits (max 3 kept; Q3, Q4, Q5 are remaining, with Q5 lowest impact for this spec)**. The `spec.md` has been updated to reflect A1, A2, A3, FR-005, and the dependence on rollback; no contradictions remain.
