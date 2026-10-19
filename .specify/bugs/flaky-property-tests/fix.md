# Bug Fix: flaky property tests (T031b + T222) — not applied; assessment premise disproven

- **Slug**: flaky-property-tests
- **Fixed**: 2026-10-19
- **Assessment**: ./assessment.md
- **Status**: not-applied

## Summary

The assessment's remediation for **T031b** (constrain the generator to integer health) **does not fix the test**. The property "distinct categories ⇒ distinct colours" is false at the **category boundaries** `(30,31)` and `(70,71)` even for integer health, because the colour palette is a continuous gradient (`Poor → Moderate → Healthy`): the boundary values share the intermediate colour. No source was modified. T222's remediation (a strictly-positive generator) is independent and sound, but per the guardrail the whole fix is halted and re-assessment is recommended.

## Changes

| File | Change | Notes |
|------|--------|-------|
| — | none | No source modified. The proposed fix was disproven before any edit. |

## Evidence (why the proposed fix does not work)

Read-only verification against the built assembly (`ColorInterpolationService`, no repo changes):

```
cat(30)=Poor     cat(31)=Moderate  colorsEqual=True   <- T031b fails here (integers!)
cat(70)=Moderate cat(71)=Healthy   colorsEqual=True   <- T031b fails here (integers!)
cat(29)=Poor     cat(30)=Poor      colorsEqual=False  <- same category, colours differ (fine)
```

Root cause of the equality (from `ColorInterpolationService.InterpolateColor` + `ModConstants`):

- `GetColorForHealth(30)` → `Lerp(PoorColor, ModerateColor, t = 30/30 = 1)` = **ModerateColor**
- `GetColorForHealth(31)` → `Lerp(ModerateColor, HealthyColor, t = (31-31)/39 = 0)` = **ModerateColor**
- `GetColorForHealth(70)` → `Lerp(ModerateColor, HealthyColor, t = (70-31)/39 = 1)` = **HealthyColor**
- `GetColorForHealth(71)` → flat **HealthyColor**

So the two pairs have different categories but identical colours — the property's `categoryA != categoryB ⇒ colorA != colorB` clause is unsatisfiable at the boundaries.

## Deviations from Assessment

The assessment's **Root Cause Hypothesis** for T031b was **incomplete/wrong**. It attributed the failure solely to the generator emitting non-integer values in the `(30,31)`/`(70,71)` gaps, and the confirmed remediation ("constrain the generator to integers") was accepted on that basis. In fact the property fails for **integer** boundary values as well, because the palette is continuous across categories. The generator constraint reduces the *probability* of hitting a counterexample but does not remove it — T031b would remain flaky (and can be made to fail deterministically with the input `(30, 31)`).

Note: the "gap" analysis was also partly moot — `HealthCategoryExtensions.FromHealthValue` and `ColorInterpolationService.GetCategoryForHealth` share the gaps, and `InterpolateColor` has none, but that inconsistency is not what T031b trips on at the boundaries.

## Follow-ups

- **Re-run `/speckit.bug.assess` for `flaky-property-tests`** with this new evidence. The T031b question is not "test-fix vs production-fix of the gaps" but **"is the property itself correct?"** Candidate options for the re-assessment:
  - Refine the property so it does not require distinct colours at a category boundary (e.g. exempt values within the boundary neighbourhood, mirroring the existing `|a-b| < 1` and Healthy exemptions).
  - Or make the palette discontinuous at category boundaries (production change) so distinct categories always have distinct colours — a real visual/UX decision.
- **T222 is separable and sound.** Its remediation (generate strictly positive health, matching the doc "positive finite health (1-100)") does not depend on the disproven T031b reasoning; it can proceed on its own once the assessment is corrected. The existing unused local `PositiveHealthGenerator()` in `OverlayRendererTests` already implements it.
- No files were changed by this command.
