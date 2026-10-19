# Bug Assessment: Flaky property tests redden "Build and Test" (T031b + T222)

- **Slug**: flaky-property-tests
- **Created**: 2026-10-19
- **Source**: pasted text (session report)
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

Pasted: `flaky-property-test bug (T031b + T222)`.

Context from this session: while verifying `build-and-test-failures`, full-suite runs intermittently failed on two FsCheck property tests, and the CI `Build and Test` on PR #285 failed on the first run and passed on re-run:

- `LivingRoots.Tests.Invariants.VisualizationInvariantsTests.T031b_OverlayDistinguishable_DistinctHealth_DistinctColors` — FsCheck counterexample `Original: (87.47000122f, 70.93000031f)`.
- `LivingRoots.Tests.Visualization.OverlayRendererTests.T222_GetOverlays_HealthZero_RendersZeroOverlays` — observed failing once in a full-suite run.

No URL was supplied, so no fetch was performed.

## Symptom

Two property-based tests fail **intermittently and only in full-suite runs** (never in isolation), which makes the `Build and Test` workflow flaky (~1 in 3 full-suite runs observed). Expected: deterministic pass, so a red `Build and Test` means a real regression rather than a seed-dependent counterexample.

## Reproduction

1. `GamePath=<Stardew install> dotnet test Stardew-LivingRoots.sln -c Release`.
2. Repeat the full-suite run; `T031b` fails in roughly 1 of 3 runs (and `T222` rarely). The failure depends on the FsCheck seed the full-suite run happens to use.
3. Isolation does **not** reproduce it: `--filter …T031b_OverlayDistinguishable…` → 0/25 failures; `--filter …T222_GetOverlays_HealthZero…` → 0/40 failures.

## Suspected Code Paths

- `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs:113` — T031b takes raw `float healthA, float healthB` and does **not** call `FsCheckFixture.Register()`, so it uses FsCheck's **default** float generator.
- `LivingRoots/Domain/Visualization/HealthCategory.cs:29` (`FromHealthValue`) and `LivingRoots/Services/Visualization/ColorInterpolationService.cs:52` (`GetCategoryForHealth`) — identical bands `Poor [0,30] / Moderate [31,70] / Healthy [71,100]`, leaving the gaps `(30,31)` and `(70,71)` → `Unknown`.
- `LivingRoots/Services/Visualization/ColorInterpolationService.cs:92` (`InterpolateColor`) — uses `<= 30`, `<= 70`, `> 70` (no gaps), so a gap value gets a real tint while its category is `Unknown`.
- `LivingRoots.Tests/Visualization/OverlayRendererTests.cs:269` — T222 takes raw `float tileHealth`, also without `Register()`, so the default float generator is used.
- `LivingRoots/Services/Visualization/OverlayRenderer.cs:367` — `if (healthValue == 0f) continue;` (health 0 contributes no overlay).
- `LivingRoots.Tests/Invariants/FsCheckFixture.cs:53,143` — the domain `Health` arbitrary (2-decimal values in `[0,100]`) that these two tests do not use.

## Root Cause Hypothesis

Confidence: **high**. Two distinct causes, both from property tests whose generators range over values outside the property's stated domain:

1. **T031b.** The generator produces arbitrary floats, including the `(30,31)`/`(70,71)` values. For those, `GetCategoryForHealth` returns `Unknown` while `GetColorForHealth` returns a real tint. The property "distinct categories ⇒ distinct colours" then fails — counterexample `(87.47, 70.93)`: categoryA `Healthy`, categoryB `Unknown` (gap), both `Healthy` colour. Root cause: the category functions have gaps that `InterpolateColor` does not.
2. **T222.** The generator produces arbitrary floats for the sibling tile. When `tileHealth == 0f`, `BuildOverlays` skips **both** tiles (`healthValue == 0f`), so `overlays.Count == 0` and `Assert.Single` fails. The doc comment says the sibling is "positive finite health (1-100)", but the generator is not constrained.

Both tests also skip `FsCheckFixture.Register()` (the fixture doc names T031a/T031b as consumers). Note: registering **alone would not fix them** — `FsCheckFixture.Health` still generates `0` and the gap values.

## Proposed Remediation

**Preferred** (test-side, minimal — constrain the generators to the property's documented domain):

- **T031b**: generate integer health 0–100 (matching the documented integer bands), e.g. via a `Gen.Choose(0, 100)`-based arbitrary or `[Property(Arbitrary = …)]`. No value then falls in a gap.
- **T222**: generate strictly positive health, e.g. `Gen.Choose(1, 100)` (or the fixture `Health` filtered `> 0`), matching the doc "positive finite health (1-100)".
- Optionally call `FsCheckFixture.Register()` in both classes and add integer/positive `Health` variants to the fixture for reuse.

**Alternative** (production, broader — close the category gaps):

- Make the bands contiguous in **both** `HealthCategoryExtensions.FromHealthValue` and `ColorInterpolationService.GetCategoryForHealth`: Poor `[0,30]`, Moderate `(30,70]`, Healthy `(70,100]`. Update the `HealthCategory.cs` / `AGENTS.md` docs. Trade-off: changes the category label for non-integer health (e.g. 70.5: `Unknown` → `Healthy`). Health is integer-valued in practice (decay −2/day, compost +15), so in-game impact is likely nil — but it removes the latent inconsistency and keeps the property strong.

**Files likely to change**:
- `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs`
- `LivingRoots.Tests/Visualization/OverlayRendererTests.cs`
- (alternative) `LivingRoots/Domain/Visualization/HealthCategory.cs`, `LivingRoots/Services/Visualization/ColorInterpolationService.cs`, `LivingRoots/Domain/Visualization/AGENTS.md`
- (optional) `LivingRoots.Tests/Invariants/FsCheckFixture.cs`

**Tests to add or update**:
- Constrain the T031b and T222 generators (preferred), or close the gaps and add a boundary test (alternative).
- A deterministic `[Fact]`/`[InlineData]` pinning category↔colour consistency at the band boundaries (30, 31, 70, 71) — and for a gap value if the gaps are closed.

## Risks & Considerations

- The gaps only matter for non-integer health, which the game likely never produces — so the production alternative is low-impact, but it changes a documented contract.
- The full-suite flakiness is ~1 in 3 runs; until fixed, a red `Build and Test` may be flakiness rather than a regression (exactly what happened on PR #285).
- `Build and Test` is deliberately **not** a required status check (`AGENTS.md`), so the flakiness never blocked a merge — which is why it persisted.
- Both tests were added in the spec-005 TDD-red wave; other property tests may share the same unconstrained-generator pattern (worth a sweep).

## Open Questions

- [NEEDS CLARIFICATION: Is non-integer soil health in the domain? If yes, the category gaps are a real bug (fix production); if no, constrain the generators to integers (fix tests).]
- [NEEDS CLARIFICATION: Should `FsCheckFixture` gain integer/positive `Health` arbitraries so property tests stop re-deriving domain constraints (and to prevent recurrence in new tests)?]
