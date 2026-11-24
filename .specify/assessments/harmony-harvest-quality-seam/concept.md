# Concept: Add Lib.Harmony (0Harmony) and a Harmony patch for Crop.harvest delegating to a testable HarvestQualityService seam

- **Slug**: harmony-harvest-quality-seam
- **Created**: 2026-11-01
- **Recommended option**: Option A — Add Lib.Harmony + testable seam + patch (fallback path activated by the T007 spike)

## Options

### Option A — Add Lib.Harmony (0Harmony) and write a code-API patch on `Crop.harvest` delegating to a testable `HarvestQualityService` seam

- **Sketch**: Add `<EnableHarmony>true</EnableHarmony>` to the `.csproj` (SMAPI-managed Harmony 2.2.x). Implement a `HarvestQualityService` (pure, testable via xUnit/FsCheck) holding all tier logic (Poor → force `Quality = 0`; Moderate → vanilla; Healthy → `+0.08` on `p` and `+0.10` on `ExtraHarvestChance`; extra items normal quality; inventory-full debris; crop-scope guard; feedback conditionality/co-occurrence). Write a static patch method (prefer postfix, code API not annotations, try/catch with fallback to original) that delegates to the seam. The seam is verified by unit tests; the patch itself is verified only by the Gate 3 in-game session. Single-player is the degenerate case of multiplayer (replicated deterministic roll, A8). This is the Q2 fallback path activated by the T007 spike.
- **Appetite**: medium (weeks) — dependency addition + seam design + RED-phase tests + patch + Gate 3 in-game verification + Android/cross-platform risk acceptance.
- **Trade-offs**: Wins the only viable interception mechanism for a virtual method; delivers the central promise (harvest payoff); keeps logic testable via the seam (Principle IV); follows SMAPI guidance (code API, postfix preferred, try/catch, `EnableHarmony`). Sacrifices: introduces the first Harmony dependency (currently zero footprint), accepts cross-platform fragility (Android, future game updates), SMAPI stability warning to players, potential conflicts with other Harmony mods, debugger-attachment friction during testing, and a runtime-only verification gap (patch can't run in xUnit).
- **Rabbit holes**: (1) Harmony transpiler temptation — aedenthorn's `Crop Quality` uses transpilers; SMAPI warns transpilers are fragile and discouraged; must stick to prefix/postfix. (2) Android compatibility — `EnableHarmony` uses SMAPI-bundled Harmony but rewriting may not cover all patch shapes; must test or accept untested. (3) Inlining — small/simple methods may be inlined by the JIT, bypassing the patch; the `harvest` method is large enough but must be verified. (4) Multiplayer desync — replicated deterministic roll (A8) must keep the seam pure/deterministic given shared inputs. (5) `SC-008` rollback verification is an absolute blocker for yield activation — must not ship quality effects before it is green. (6) Prefix vs. postfix choice for forcing `Quality = 0` (Poor) vs. boosting `p` (Healthy) affects composition with vanilla modifiers (FR-003 "compose, don't replace").

### Option B — Do nothing: keep the central promise half-delivered

- **Sketch**: Ship only the already-completed band-scale migration (31/71) and CollectCompost fix; do not add Harmony; do not intercept `Crop.harvest`. Soil health continues to drive growth speed only; harvest quality remains vanilla on all tiers. The overlay/feedback layer (if any) stays diagnostic. The "cost of inaction" framing from the problem definition applies.
- **Appetite**: small (days) — no new dependency, no runtime interception, no Gate 3 addition beyond what already exists.
- **Trade-offs**: Wins zero new risk: no Harmony dependency, no cross-platform fragility, no stability warning, no debugger friction, full reviewability. Sacrifices: the central promise stays half-delivered (no harvest-time payoff for Living Soil, no visible consequence for Poor); the loop build → protect → harvest remains open; ReGen Valley keeps the "quality at high soil health" differentiator; the mod risks being perceived as a diagnostic overlay with a compost buff rather than an agroecological system.
- **Rabbit holes**: None significant — this is the "do nothing" baseline. Risk is product/positioning, not technical.

### Option C — Defer / rely on existing mods for the harvest-quality half

- **Sketch**: Do not implement harvest-quality interception in this mod. Instead, document compatibility with aedenthorn's `Crop Quality` (which already patches `Crop.harvest` via Harmony transpiler) and let that mod provide the quality stars, while Living Roots contributes soil health measurement, overlay, compost, and growth speed. Optionally soften ROADMAP/GDD wording further.
- **Appetite**: small (days) — documentation + compatibility notes only.
- **Trade-offs**: Wins zero implementation risk and leverages a proven, 1.6.15-compatible mechanism. Sacrifices: dependency on a third-party mod for the core promise (not self-contained), no control over quality numbers/bands/feedback, no integration with soil health tiers (the whole point), and a weaker product identity. Conflicts with the mod's "vanilla-first, self-contained" posture and with Constitution I (DDD — the domain concept `SoilHealth → CropQuality` would be split across mods).
- **Rabbit holes**: Compatibility matrix maintenance; version drift between Living Roots and `Crop Quality`; player confusion about which mod does what.

## Recommendation

**Option A** — add `Lib.Harmony` + seam + patch, behind RED-phase tests. It is the only option that delivers the central promise, and the fallback path was explicitly pre-approved by the Q2 decision (spec 005 Q1 for growth, spec 014 Q2 for harvest) once the spike confirmed public fields are insufficient. Option B and C both leave the promise half-delivered, which the problem definition's cost-of-inaction analysis shows is unacceptable for the mod's positioning.

The "no Harmony unless necessary" posture is preserved in spirit: Harmony is used **only** where public fields are insufficient (the `harvest` method), while the growth modifier keeps the public-field path. The logic is extracted into a testable seam (`HarvestQualityService`) so TDD (Principle IV) and reviewability (problem.md) are satisfied, and the patch itself follows SMAPI's documented best practices (code API, postfix preferred, try/catch with fallback to original, `EnableHarmony=true`).

## Out of Scope (for the recommended option)

- Replacing the public-field growth-modifier path with Harmony (growth stays no-Harmony).
- Pest system, "new interactions," numerical tuning beyond first-pass values, mid/end-game systems (separate specs, already out of scope for 014).
- Live 2-player verification beyond the existing D1 roadmap obligation (deferred).
- Android compatibility guarantee (Harmony is higher risk on Android; warn and accept untested, per SMAPI guidance).
- Formal ADR document (recommended but not blocking — can be added in polish).

## Assumptions to Validate

- Maintainer approves adding `Lib.Harmony` (0Harmony) as a dependency (currently zero Harmony footprint; first Harmony use in the project).
- `HarvestQualityService` seam API shape is defined before RED-phase tests (T024–T035): inputs tile health, Farming level, fertilizer tier, `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`, pest state provider; outputs primary quality, extra-harvest count, feedback flags.
- Patch style is decided (Prefix returning false for Poor vs. Postfix overwriting `Quality`) before implementation — affects FR-003 "compose, don't replace."
- `SC-008` rollback verification is green before yield activation (absolute blocker, not before design/tests).
- `EnableHarmony=true` uses SMAPI-bundled Harmony 2.2.x (no separate package version to manage).
- The seam is pure/deterministic given inputs so the replicated deterministic roll (A8) keeps both clients in agreement.
- `harvest` is large enough not to be inlined by the JIT (per SMAPI guidance, small/simple methods may be inlined, bypassing the patch).
- An ADR is created in `docs/adr/` to record the decision formally (documentation hygiene, not a blocker).
