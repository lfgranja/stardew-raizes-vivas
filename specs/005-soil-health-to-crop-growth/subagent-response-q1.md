# Subagent Response — Q1: Hook Mechanism (Option A Evidence)

**Source**: spec.md line 27 / plan.md line 148 (`Clarifications` Q1, 2026-10-15).

**Question**: Which hook applies the soil-health growth modifier — Option A (`DayStarted` via public `Crop.newDay` field scan) or Option B (`HoeDirt.GetFertilizerSpeedBoost()` Harmony patch)?

**Selected option**: **Option A** — `DayStarted` public-field scan through `Crop.newDay`; no `Lib.Harmony` dependency.

**Evidence supporting A**:
- Zero Harmony footprint; `AGENTS.md` anti-pattern rejects Harmony.
- `ModController.cs` 1719–1778 (`OnDayStarted`) provides the existing event hook for per-tick growth scan; no new event mechanism required.
- `LuckSkill` (vanilla) proves direct public-field access to `Crop` fields works without Harmony patches.
- `Crop.cs` decompilation confirms `newDay` and phase-duration fields are public; no private reflection needed.
- `GamePath` spike gate (`A1`, Dependencies 141) required before activation — verifies ordering with vanilla growth (before/after `HoeDirt.GetFertilizerSpeedBoost`). Design permitted in parallel; activation blocked until gate passes.
- Option B (`HoeDirt.GetFertilizerSpeedBoost()` Harmony patch) remains the documented fallback only if ordering verification fails; not implemented per zero-Harmony anti-pattern.

**Status**: Design locked (`plan.md` Phase 1; `subagent-response-q1.md` cited). Implementation deferred to `tasks.md`. `GamePath` gate (`A1`) prerequisite before activation (`SC-008` concurrent, independent). No `NEEDS CLARIFICATION`. `B` fallback preserved (`spec.md` A1 / `plan.md` Dependencies) — not activated.
