# Decision: Add Lib.Harmony (0Harmony) and a Harmony patch for Crop.harvest delegating to a testable HarvestQualityService seam

- **Slug**: harmony-harvest-quality-seam
- **Decided**: 2026-11-01
- **Verdict**: go
- **Artifacts reviewed**: intake.md (yes) | research.md (no — not produced; evidence grounded in spike T007, SMAPI Harmony wiki, skills, prior spec 005/014 artifacts) | problem.md (yes) | concept.md (yes)

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | The problem is real and specific: `Crop.harvest` is a `public virtual` method, so the preferred public-field path cannot intercept it; without Harmony the harvest half of the central promise (Poor → no stars, Living → silver/gold/iridium + extra harvest) cannot ship. Confirmed by the T007 GamePath spike against the 1.6.x binary. [source: spike-t007-gamepath.md; problem.md] |
| Evidence strength | adequate | Multi-source and cited (SMAPI Harmony wiki EN/ZH, aedenthorn `Crop Quality` mod on 1.6.15, `harmony-patching-mastery` and `stardew-valley-smapi` skills, Constitution, specs 005/014, T007 spike), but no dedicated `research.md` was produced for this assessment; evidence is assembled from prior-stage artifacts. Tagged adequate (never weak/unknown). |
| Value vs. inaction | strong | Cost of inaction (problem.md): central promise half-delivered, ReGen Valley keeps the "quality at high soil health" differentiator, mod risks being perceived as a diagnostic overlay. Option A is the only option that delivers the promise; B/C leave it undelivered. |
| Feasibility / appetite | adequate | Option A is credible within a `medium` appetite (weeks): `EnableHarmony=true` (SMAPI-bundled 2.2.x), code API (not annotations), postfix preferred, try/catch with fallback, testable seam. `aedenthorn` already proves the mechanism on 1.6.15. Main unknowns are seam API shape and patch style (Prefix vs. Postfix). |
| Strategic fit | strong | Aligns with Constitution I (DDD — seam in Services, patch in ModEntry/ModController), IV (TDD — seam testable via xUnit/FsCheck), V (YAGNI — Harmony only where public fields are insufficient; growth modifier stays no-Harmony), VI (multiplayer — replicated deterministic roll, A8). Aligns with spec 014 goals and the "no Harmony unless necessary" posture (Harmony is necessary here, and only here). |
| Risk posture | adequate | Key risks identified and mitigated: crashes/memory corruption → try/catch + default to original; cross-platform/Android → SMAPI rewriting + warning, accept untested; conflicts → code API (rewritable), postfix preferred; debugger friction → acknowledge; inlining → `harvest` is large enough; multiplayer desync → seam pure/deterministic (A8); SC-008 rollback blocker → acknowledged as absolute gate; testability gap → seam `HarvestQualityService`. Serious risks are understood and mitigated, not eliminated. |

## Verdict & Rationale

**go.** The problem is real and well-understood (spike confirmed `Crop.harvest` is a virtual method, so the preferred public-field path cannot intercept it). The evidence is multi-source and cited (adequate, not weak/unknown). Option A is the only option that delivers the central promise; B and C leave it half-delivered, which the cost-of-inaction analysis shows is unacceptable. The risks are identified and acceptably mitigated (try/catch, code API, postfix, `EnableHarmony`, testable seam, Gate 3 verification). The decision activates the Q2 fallback path that was pre-approved in specs 005 and 014, conditional on the spike — the spike is now complete and confirms Harmony is necessary. Hand off to `/speckit.specify` to produce the specification for adding `Lib.Harmony` and the seam + patch.

## If go — Handoff to `/speckit.specify`

- **Problem**: The mod cannot deliver the harvest half of its central promise (Poor → no stars; Living → silver/gold/iridium + extra harvest) because `Crop.harvest` is a `public virtual` method that public-field manipulation cannot intercept; Harmony is the only viable interception mechanism, and the project currently has zero Harmony footprint.
- **Chosen approach**: Option A — add `Lib.Harmony` (0Harmony) via `<EnableHarmony>true</EnableHarmony>` (SMAPI-bundled 2.2.x); implement a `HarvestQualityService` seam (pure, testable via xUnit/FsCheck) holding all tier logic; write a static Harmony patch (code API, not annotations; prefer postfix; try/catch with fallback to original) that delegates to the seam; verify the patch only via Gate 3 in-game.
- **In scope / out of scope**: In scope — Harmony dependency, seam `HarvestQualityService`, patch on `Crop.harvest`, tier rules (Poor force 0; Healthy +0.08/+0.10; extra items normal quality), inventory-full debris handling, crop-scope guard (A6), feedback conditionality/co-occurrence (B1/B2), hybrid multiplayer visibility, host-authority (A8 replicated deterministic roll). Out of scope — replacing the public-field growth modifier with Harmony; pest system and "new interactions" (separate specs); numerical tuning beyond first-pass; live 2-player verification (D1, deferred); Android compatibility guarantee (warn, accept untested); formal ADR (recommended but not blocking).
- **Success metrics**: SC-001–SC-010 (spec 014) — Poor always 0 stars; Moderate byte-identical to vanilla; Living enhanced quality + extra harvest measurable; transient feedback ≤1000 ms; host-authoritative rolls; band 31/71; build/test/format clean; Gate 3 3-signal capture with second player. Seam logic exercisable by xUnit/FsCheck without a game runtime.
- **Carried-forward open questions**:
  - [NEEDS CLARIFICATION: Confirm maintainer approval to add `Lib.Harmony` (0Harmony) as a dependency (currently zero Harmony footprint).]
  - [NEEDS CLARIFICATION: Confirm `HarvestQualityService` seam API shape (inputs: tile health, Farming level, fertilizer tier, `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`, pest state provider).]
  - [NEEDS CLARIFICATION: Confirm patch style — `Prefix` (return false to suppress original quality roll) vs. `Postfix` (modify `Object.Quality` after the vanilla roll) — affects FR-003 "compose, don't replace."]
  - [NEEDS CLARIFICATION: Confirm testability strategy — pure functions over primitives in xUnit/FsCheck.]
  - [NEEDS CLARIFICATION: Confirm `SC-008` rollback verification status (absolute blocker for yield activation).]
  - [NEEDS CLARIFICATION: Confirm whether an ADR document should be created (`docs/adr/001-harmony-for-harvest-interception.md`).]
  - [NEEDS CLARIFICATION: Confirm Android/cross-platform impact acceptance (SMAPI warns Harmony is higher risk on Android).]
  - [NEEDS CLARIFICATION: Confirm seam's host-authority contract — replicated deterministic roll (A8); seam must be pure/deterministic given inputs.]
