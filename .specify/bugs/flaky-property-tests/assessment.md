# Bug Assessment: Flaky property tests redden "Build and Test" (T031b + T222) — re-assessment

- **Slug**: flaky-property-tests
- **Created**: 2026-10-19
- **Source**: session context (re-assessment requested by `./fix.md`, which recorded that the first assessment's T031b premise was disproven)
- **Verdict**: valid
- **Severity**: medium

> Re-assessment. This file replaces the earlier assessment (same slug). The prior remediation was rejected by `/speckit.bug.fix` after empirical verification showed the T031b root cause was elsewhere.

## Clarifications

### Session 2026-10-19

- Q: Must each health category (Poor / Moderate / Healthy) always show a visually distinct tint, or is it acceptable that the tint changes smoothly and the Healthy tier is a single flat colour? → A: Smooth is acceptable — keep the continuous gradient and the flat Healthy tier (deliberate per T253/FR-010); correct the T031b property rather than changing the palette.
- Q: Should the health→category bands be made contiguous so every value in `[0, 100]` maps to Poor/Moderate/Healthy, or is it fine that non-integer values in `(30,31)` and `(70,71)` map to `Unknown`? → A: Leave as-is — health is integer-valued in practice (decay `2×{0.5,1.5,0}`, compost `+15`, clamp `[0,100]`), so the gaps are unreachable and the corrected tests won't depend on them; no production change to the shipped mapping.

## Report (verbatim or summarized)

Two FsCheck property tests fail intermittently in full-suite runs, reddening `Build and Test` (~1 in 3 runs observed; the CI job failed on PR #285 and passed on re-run):

- `VisualizationInvariantsTests.T031b_OverlayDistinguishable_DistinctHealth_DistinctColors` — counterexample `(87.47000122f, 70.93000031f)`.
- `OverlayRendererTests.T222_GetOverlays_HealthZero_RendersZeroOverlays` — observed failing once.

The prior fix attempt (`./fix.md`) disproved the first assessment's T031b premise. New evidence gathered for this re-assessment, against the built assembly:

```
cat(30)=Poor     cat(31)=Moderate  colorsEqual=True    <- boundary seam
cat(70)=Moderate cat(71)=Healthy   colorsEqual=True    <- boundary seam
cat(29)=Poor     cat(30)=Poor      colorsEqual=False   <- same-tier gradient (fine)
```

Exhaustive integer scan (all pairs 0..100):

```
violations: 29
  DIFFCAT 70(Moderate) vs 72(Healthy)
  DIFFCAT 70(Moderate) vs 73(Healthy)
  ... (all 70 vs k, k in 72..100)
```

## Symptom

`T031b` asserts that distinct health readings produce distinct tints, including "readings in different categories always differ". That assertion is **false against the actual palette**, so the property fails whenever FsCheck happens to generate one of the violating pairs (e.g. `70` with any Healthy reading). `T222` asserts a non-zero sibling tile draws exactly one overlay, which fails when the generator emits `0`.

## Reproduction

1. `GamePath=<Stardew install> dotnet test Stardew-LivingRoots.sln -c Release` (full suite), repeated — intermittent failures.
2. Deterministic proof of the T031b defect: with the palette, `GetColorForHealth(70f) == GetColorForHealth(72f)` (both `HealthyColor`) while their categories differ (`Moderate` vs `Healthy`).
3. Deterministic proof of the T222 defect: `GetOverlays` with `{zeroTile: 0f, sibling: 0f}` returns **0** overlays (both tiles skipped), so `Assert.Single` fails.

## Suspected Code Paths

- `LivingRoots/Services/Visualization/ColorInterpolationService.cs:92` (`InterpolateColor`) — continuous gradient `Poor→Moderate→Healthy`; at `70` the lerp reaches `HealthyColor`.
- `LivingRoots/Services/Visualization/ColorInterpolationService.cs:110-120` — the Healthy tier is held **flat** (`healthy` colour) for all `> 70`. So `70` (Moderate) and every Healthy reading share a colour.
- `LivingRoots/Services/Visualization/ColorInterpolationService.cs:52` (`GetCategoryForHealth`) and `LivingRoots/Domain/Visualization/HealthCategory.cs:29` (`FromHealthValue`) — bands `[0,30]/[31,70]/[71,100]`, leaving the gaps `(30,31)`/`(70,71)` → `Unknown` (the original non-integer counterexample's second cause).
- `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs:113` (T031b) — the property that encodes the false assumption.
- `LivingRoots.Tests/Visualization/OverlayRendererTests.cs:269` (T222) — raw `float tileHealth` (default FsCheck generator, which emits `0`); the class already has an unused `PositiveHealthGenerator()` at `:46`.
- `LivingRoots/Services/Visualization/OverlayRenderer.cs:367` — `if (healthValue == 0f) continue;` (health 0 draws no overlay).

## Root Cause Hypothesis

Confidence: **high**.

1. **T031b — the property is wrong, not the generator.** The palette is a **continuous** gradient that *reaches* the Healthy colour at 70 and then holds the Healthy tier **flat** (T253/FR-010, deliberate). Consequently `GetColorForHealth(70)` equals the colour of every Healthy reading, and `GetColorForHealth(30)` equals `GetColorForHealth(31)`. So "different categories ⇒ different colours" is unsatisfiable at both seams — **29 integer pairs** violate it. The non-integer category gaps `(30,31)`/`(70,71)` add more violations (the original `70.93` counterexample) by making the category `Unknown` while the colour stays real. Constraining the generator to integers (the rejected prior fix) would *raise* the failure rate (the 29 violating pairs are all integers).

2. **T222 — the generator is wrong.** `BuildOverlays` skips `healthValue == 0f`, so a generated `0` sibling yields 0 overlays and `Assert.Single` fails. The doc says the sibling is "positive finite health (1-100)", but the generator is unconstrained.

## Proposed Remediation

**Preferred** (correct the tests to match the actual, intended palette):

- **T031b**: drop the false "different categories always differ" clause and keep only the invariants the palette actually guarantees:
  - the tint is a deterministic function of health (same input → same colour);
  - within the gradient tiers (Poor, Moderate), readings **≥ 1 apart differ** (verified: 0 violations today);
  - cross-tier distinctness is **not** required (the palette is continuous and the Healthy tier is flat by design).
  Also constrain the generator to the real domain — integer health `0–100` — so the property no longer depends on the unreachable non-integer gaps.
- **T222**: generate strictly positive health (`1–100`), matching the doc, using the existing `PositiveHealthGenerator()` (or a shared `FsCheckFixture` positive arbitrary).

**Alternatives**:
- Make the palette **discontinuous** at the seams so distinct categories always have distinct colours (production change). **Rejected** (Clarifications 2026-10-19): contradicts T253/FR-010 (flat Healthy tier, monotone palette) and changes player-visible tints.
- Constrain only the T031b generator (no property change). Rejected: leaves the 29 integer violations → T031b fails ~40% of runs with integer health.

**Files likely to change**:
- `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` (T031b property)
- `LivingRoots.Tests/Visualization/OverlayRendererTests.cs` (T222 generator; use/remove `PositiveHealthGenerator`)
- `LivingRoots.Tests/Invariants/FsCheckFixture.cs` (optional: shared positive/integer `Health` arbitraries)

**Tests to add or update**:
- A deterministic `[Theory]` pinning the palette's **design** at the seams — `GetColorForHealth(30) == GetColorForHealth(31)` and `GetColorForHealth(70) == GetColorForHealth(71)` — so the continuity/flat behaviour is documented and intentional rather than accidental.
- The corrected T031b property (same-tier gradient distinctness).
- The corrected T222 generator.

## Risks & Considerations

- T031b currently fails intermittently; correcting the property is test-only and low-risk, but it *removes* an assertion ("distinct categories differ") that some may read as a UX requirement — hence the open question below.
- The non-integer category gaps `(30,31)`/`(70,71)` remain a latent inconsistency between `GetCategoryForHealth`/`FromHealthValue` and `InterpolateColor`; **left as-is** per Clarifications 2026-10-19 (unreachable for integer health).
- `Build and Test` is not a required status check (`AGENTS.md`), so the flakiness never blocked merges — which is why it persisted.
- Both tests are from the spec-005 TDD-red wave; other property tests may share the "assert a stronger property than the code guarantees" pattern.

## Open Questions

None — both items are resolved (see Clarifications).
