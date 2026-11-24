# Problem Definition: Joja / Agrochemical Tradeoff (controlled MVP slice)

- **Slug**: `joja-agrochemical-tradeoff`
- **Created**: 2026-11-06
- **Inputs used**: `intake.md` | `research.md` (including Q1–Q6 deep-dive synthesis) | user input ("Aceito todas as recomendações")

## Problem Statement

Living Roots currently makes healthy soil management visibly rewarding (014/015 host-only `machine-pass`: better soil can improve growth/quality), but it does **not** yet present a consequential destructive choice: players can pursue regenerative play without facing a tempting, short-term industrial/agrochemical path that carries a real, legible cost to soil health. As a result, soil is not yet the strategic decision system the GDD describes, and the mod risks feeling like a one-directional buff rather than a system of meaningful tradeoffs.

## Affected Users & Stakeholders

- **Users (players, cozy/simulation audience):** Experience an incomplete decision loop. Without a destructive option, there is little tension between convenience/short-term payoff and long-term soil stewardship; learning/strategy value is reduced if costs are absent or opaque. (Grounded in `intake.md` / `docs/GDD-living-roots.md` framing and `research.md` evidence on legibility/temporal discounting; persona details [NEEDS CLARIFICATION: which player segments/value propositions to optimize first — e.g., cozy-first, optimization-first, educational-first].)
- **Users (multiplayer farmhands/hosts):** Need soil-affecting choices to behave predictably and safely in co-op. Ambiguous authority or invisible side effects would harm trust in shared farm state (Constitution Principle VI; FR-009/A8 context in `research.md`).
- **Stakeholders (mod author/maintainer):** Must deliver GDD-aligned value with controlled scope and honest verification, reusing existing soil/harvest/decay/sync infrastructure while avoiding risky hooks and false claims (`machine-pass` ≠ `verified`; D1/human evidence constraints).
- **Stakeholders (future spec/implementation/verifier):** Need a bounded, testable problem that can become a small spec slice (TDD, tunables, Gate 3 honesty) without rewriting 014/015 history or blocking on unavailable D1/human environments.

## Goals

- Close the missing **consequential decision loop** in the GDD by defining the problem for a first, controlled Joja/agrochemical tradeoff: a tempting short-term path that imposes a real, understandable cost to soil health.
- Make the cost **legible and non-punitive in tone** (cozy/educational intent: "recompensar, nunca pregar") so players can understand the tradeoff before and after the choice and recover through existing regenerative systems.
- Preserve **trustworthy system behavior**: soil health remains bounded/validated, shared-farm effects remain host-authoritative with no second source of truth, and verification claims remain honest (host-only `machine-pass` possible where scoped; no human `verified`/D1 claim without evidence).
- Keep the slice **small enough to specify now** (one controlled rule/input + integration with existing SoilHealth/decay/harvest concepts), with first-pass tunable parameters and no mandatory new art/audio assets.

## Non-Goals

- **Not implementation.** This stage defines the problem only; no code, APIs, data models, or task breakdown.
- **No Harmony hook on vanilla fertilizer application in the MVP problem framing.** Any future hook that depends on exact SDV 1.6.14+/1.6.15 fertilizer commit/probe semantics is gated on decompile verification of the target build (Q5 gap). The MVP direction accepted is mod-intercepted, not a vanilla fertilizer-slot hook.
- **No double-counting with 014/015.** The short-term gain must not be defined in a way that implicitly stacks with existing 014/015 harvest-quality/growth behavior or vanilla fertilizer-tier heuristics without an explicit, testable composition rule (Q1).
- **Monoculture and other destructive mechanics are out of scope** for this slice (Q7 accepted). They require separate assessment/spec work.
- **No multiplayer farmhand authority/D1 attestation in this MVP framing.** Farmhand intent/arbitration, cross-client idempotency protocol, and real-farmhand convergence are follow-up work; host-only `machine-pass` must not be presented as `verified` or as D1-complete (Q4, Principle VI, runner limits).
- **No full Joja content arc, economy rebalance, or asset production campaign.** One controlled slice only; no obligatory new PNG/SFX beyond what existing patterns already support.
- **No rewriting of historical specs/evidence** (014/015/010 contracts, bundles, or task history). Append/reconcile only, with honest provenance.

## Success Metrics

| Signal | Baseline / current value | Target direction (to be finalized in spec) |
|---|---:|---|
| Existence of a specified destructive tradeoff slice | 0 (no Joja/agrochemical soil-degrading decision specified/implemented) | 1 bounded spec slice approved for the MVP problem boundary, with explicit composition/invariants and Gate 3 criteria. |
| Player comprehension of cost (qualitative) | Unknown; current system communicates only positive soil effects in the implemented loop | Playtest/readability evidence shows players can identify the short-term benefit and the soil cost before/after use without punitive opacity. **[Qualitative; method TBD]** |
| Soil/state safety invariants (specifiable/testable) | Existing: health bounded 0–100, tier edges 31/71, host-authoritative FR-009/A8 patterns, measured-0 vs NaN semantics known (`research.md` Q3) | Spec defines penalty/baseline/idempotency/authority rules that preserve existing invariants; automated tests/property checks cover them in implementation stage. |
| Verification honesty | 014 host-only `machine-pass` 759d/14b6, not `verified`; D1 blocked | Gate 3 criteria distinguish host-only `machine-pass` from human `verified`/D1; no evidence promoted beyond what is attested. |
| Balance/feel of penalty magnitude (first-pass tunable) | Unknown baseline for this mechanic; `DailyDecayRate=2f`, `RestorationAmount=15f` exist for other flows (`Constants.cs`) | A first-pass tunable penalty value is chosen for spec/testing and marked for playtest calibration; no claim of final balance. **[NEEDS CLARIFICATION: initial tuning target/playtest method]** |

## Cost of Inaction

- The soil system remains strategically shallow: players get positive reinforcement without a meaningful cost-bearing alternative, weakening the GDD’s “decisions with consequences” pillar.
- Living Roots remains easier to perceive as a simple buff/quality mod rather than a distinctive agroecological tradeoff system.
- Design/implementation risk compounds later: adding a destructive mechanic after more features may create larger composition, MP-sync, and balance conflicts than a small, controlled slice now.
- Educational/cozy intent is under-realized: the contrast that makes regenerative choices meaningful (and Joja-style shortcuts tempting but costly) stays implicit.

## Open Questions

- [NEEDS CLARIFICATION: Exact short-term gain formulation for MVP] — User accepted mod-intercepted MVP gain with explicit no-double-counting (Q1 recommendation), but the concrete player-facing benefit/proxy and its composition rule must be specified precisely in `shape/specify`.
- [NEEDS CLARIFICATION: Item/content contract details] — Custom mod-intercepted Joja-only item is the accepted MVP direction (Q2), but `Data/Objects`/`Data/Shops`/ID/tag/i18n details, `OrganicWasteValidator` tag alignment, and acquisition UX must be fixed in spec without claiming vanilla fertilizer-slot behavior.
- [NEEDS CLARIFICATION: Unknown-baseline penalty handling] — Penalty is immediate/tunable (Q3), but the spec must define the baseline when a tile is `NaN`/unmeasured so the result is explicit and measured (avoiding accidental invisible no-op from negative `UpdateHealth` on unknown tiles).
- [NEEDS CLARIFICATION: Farmhand interaction path for MVP] — Host-authoritative MVP accepted (Q4), but the exact refusal/UX path for farmhand attempts (and “no local farm mutation/item side effects beyond defined refusal behavior”) must be specified; D1 intent/ack protocol remains follow-up.
- [NEEDS CLARIFICATION: Decompile gate for any future Harmony fertilizer hook] — No Harmony fertilizer hook in MVP (Q5 accepted). If a later slice targets vanilla fertilizer commit/probe paths (e.g., `HoeDirt.plant`/related 1.6 methods), exact target-build signatures and commit semantics must be decompile-verified first.
- [NEEDS CLARIFICATION: Telegraphing copy, localization parity, and feedback moments] — Text/procedural/localized telegraphing before and after use is accepted (Q6), but exact copy, key naming, and `default/en/pt` parity (including known mulch/i18n inconsistencies if touched) must be specified.
- [NEEDS CLARIFICATION: First-pass penalty tuning and playtest calibration] — Magnitude/tier-boundary impact (31/71) needs a tunable starting value and a playtest plan; do not present as balanced/verified.

## Accepted Direction (carried from assessment; still problem/spec boundary, not implementation)

- **MVP:** 1 controlled Joja/agrochemical rule/input, mod-intercepted (not Harmony fertilizer hook), host-authoritative, first-pass tunable constants, FR-009 snapshot sync compatibility, TDD/property tests where applicable, honest Gate 3 criteria, no mandatory new assets, no history rewrites.
- **Out:** monoculture; vanilla fertilizer-hook MVP; D1/`verified` claims from host-only evidence; double-counting with 014/015.
