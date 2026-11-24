# Decision: Joja / Agrochemical Tradeoff (controlled MVP slice)

- **Slug**: `joja-agrochemical-tradeoff`
- **Decided**: 2026-11-06
- **Verdict**: **go**
- **Artifacts reviewed**: `intake.md` | `research.md` (incl. Q1–Q6 deep-dive synthesis) | `problem.md` | `concept.md`

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | **strong** | 014/015 make soil stewardship rewarding (host-only `machine-pass`, not `verified`), but the GDD’s consequential destructive choice is still missing; `problem.md` ties this to a real strategic/identity gap with clear cost of inaction. |
| Evidence strength | **adequate** | `research.md` provides high-confidence internal codebase evidence for soil/harvest/growth/FR-009/composition patterns and medium/adequate external evidence with explicit gaps (Nexus/wiki 403, decompile must target exact build, D1 blocked, playtest/tuning unknown). Evidence is sufficient for a bounded spec decision; remaining unknowns are carried forward, not hidden. |
| Value vs. inaction | **strong** | A small controlled slice closes the missing decision loop and differentiates the mod from positive-only buffs; deferring keeps high opportunity cost while 014/015 operational follow-ups remain expensive/non-80/20 for product depth. |
| Feasibility / appetite | **adequate** | Option A is a small spec/appetite by reusing existing soil, mod-intercepted application, feedback, and FR-009 sync concepts; Harmony vanilla-fertilizer interop is explicitly deferred behind a decompile gate; host-only verification is feasible now under honest Gate 3 rules. |
| Strategic fit | **strong** | Aligns with GDD tradeoff/educational-cozy framing, Constitution Principle VI (host authority, no second source of truth, honest verification), DDD/layer boundaries, and 014/015/010 evidence/provenance constraints. |
| Risk posture | **adequate** | Key risks are identified and mitigated at MVP boundary: no Harmony fertilizer hook, no double-counting without explicit composition, explicit unknown/`NaN` baseline handling, host-authoritative scope with farmhand refusal/follow-up path, first-pass tunable penalty, no mandatory assets, and no `verified`/D1 claims from host-only evidence. Residual risks (balance, legibility, tag/i18n/content details) are manageable in specify/implementation. |

## Verdict & Rationale

**Go** — specify **Option A: Mod-intercepted Joja input with legible soil cost (MVP)** as a controlled Living Roots slice. The problem is valid and strategically important, evidence is adequate (not weak/unknown) for a bounded design/spec decision, and the recommended concept fits a small appetite while preserving constitution/verification honesty. The assessment should **not** produce implementation detail here; `/speckit.specify` must carry forward the open questions, decompile/D1 boundaries, and non-goals as explicit constraints and acceptance criteria.

## If needs-clarification

- N/A (verdict is `go`).

## If go — Handoff to `/speckit.specify`

- **Problem**: Living Roots rewards healthy soil (014/015) but lacks a tempting, short-term industrial/agrochemical choice with a real, legible soil-health cost, leaving the GDD’s consequential decision loop incomplete.
- **Chosen approach**: **Option A — Mod-intercepted Joja input with legible soil cost (MVP)**. 1 controlled Joja/agrochemical rule/input, mod-intercepted (not a Harmony vanilla-fertilizer hook), host-authoritative, first-pass tunable `ModConstants`, compatible with existing SoilHealth/decay/harvest concepts and FR-009 snapshot sync, cozy legible before/after telegraphing, no mandatory new assets, TDD/property coverage where applicable, honest Gate 3 criteria.
- **In scope / out of scope**:
  - **In scope:** a small spec for one MVP tradeoff slice; explicit short-term gain with **no double-counting** against 014/015/vanilla fertilizer-tier heuristics unless a composition rule is stated and testable; immediate bounded tunable soil penalty with explicit `NaN`/unmeasured baseline handling; host-authoritative mutation and FR-009-compatible sync boundary; clear refusal/UX path for unsupported farmhand attempts (no unsafe local mutation); textual/procedural/localized telegraphing; host-only `machine-pass` criteria where feasible.
  - **Out of scope:** monoculture/other destructive mechanics; Harmony hook on vanilla fertilizer application/commit/probe paths in MVP; true vanilla `HoeDirt.fertilizer`-slot interop as required MVP behavior; farmhand intent/ack/idempotency protocol, D1 convergence, or any `verified`/production MP claim without evidence; full Joja economy/content/asset campaign; rewrites of 014/015/010 historical specs/bundles/tasks; final balance claims.
- **Success metrics**:
  - 1 bounded spec slice exists with clear composition, invariants, authority/sync, telegraphing, and Gate 3 criteria.
  - Qualitative readability goal: players can identify benefit and soil cost before/after use without punitive opacity (method TBD).
  - Specifiable/testable safety invariants: bounded health (0–100), tier-edge awareness (31/71), explicit measured-vs-`NaN` baseline behavior, idempotent/authoritative mutation boundary, FR-009 compatibility.
  - Verification honesty: host-only `machine-pass` distinguished from human `verified`/D1; no over-promotion.
  - First-pass tunable penalty selected for testing and marked for playtest calibration (not balanced/verified).
- **Carried-forward open questions**:
  - Exact MVP short-term gain formulation/proxy and explicit composition rule (no double-counting with 014/015).
  - Item/content contract details for the mod-intercepted Joja-only direction (`Data/Objects`/`Data/Shops`/ID/tag/i18n, `OrganicWasteValidator` tag alignment, acquisition UX), without claiming vanilla fertilizer-slot behavior.
  - Unknown-baseline penalty handling when a tile is `NaN`/unmeasured (explicit measured result; avoid invisible no-op).
  - Farmhand interaction/refusal path for host-authoritative MVP; D1 intent/ack protocol is follow-up.
  - Decompile gate for any future Harmony fertilizer hook (exact 1.6.14+/1.6.15 commit/probe semantics) — **not in MVP**.
  - Telegraphing copy, localization parity (`default/en/pt` as applicable), and before/after feedback moments.
  - First-pass penalty tuning and playtest calibration (including 31/71 tier-boundary impact).
