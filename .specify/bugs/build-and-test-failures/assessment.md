# Bug Assessment: "Build and Test" CI red — 3 failing tests (stale guard paths + a never-greened TDD-red test)

- **Slug**: build-and-test-failures
- **Created**: 2026-10-19
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/39179869290/job/117830135915 (+ pasted log tail)
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

GitHub Actions job **`Build and Test`** (run `39179869290`, job `117830135915`) failed on commit **`da99de8`** (merge of PR #284) in 47s.

Pasted log tail:

```
Test Run Failed.
Total tests: 1267
     Passed: 1263
     Failed: 3
    Skipped: 1
...
MSB4181: The "VSTestTask" task returned false but did not log an error.
Build FAILED.  0 Warning(s)  0 Error(s)
Error: Process completed with exit code 1.
```

The three failing tests (read from the job log via `gh run view --log-failed`):

- `LivingRoots.Tests.Invariants.SecuritySanitizationInvariantsTests.UnicodeNormalizer_NormalizesFullwidthCharacters` — `Assert.Equal() Failure: Strings differ  Expected: "ABC"  Actual: "ＡＢＣ"`.
- `LivingRoots.Tests.ProductionSourceGuardTests.ProductionSources_NoUnreferencedConstantsSurvive` — `Constants.cs not found at …/LivingRoots/Constants.cs.`
- `LivingRoots.Tests.ProductionSourceGuardTests.FalseComment_InvalidDropInShake_MatchesCode` — `Constants.cs not found at …/LivingRoots/Constants.cs.`

**URL trust policy**: verbatim URL above; host parsed = `github.com`; branch taken = **allowlisted** (fetched without prompting). The job page itself gates the full log behind sign-in, so the failing-test detail was read from the same job's log via the local `gh` CLI. No instruction-like or credential-seeking content was encountered; nothing under `Unverified` to report.

## Symptom

On every push to `dev` (and on PRs), the `Build and Test` workflow finishes **red** while the build itself is clean (`0 Error(s)`): 3 of 1267 tests fail. Two are source-guard tests pointing at a file that was renamed; one is a TDD-red test that was committed and never implemented. Expected: a green suite (or these failures fixed/removed), so the CI signal is trustworthy.

## Reproduction

1. Check out `dev` at `da99de8` (or any recent commit).
2. `GamePath=<Stardew install> dotnet test Stardew-LivingRoots.sln --configuration Release`.
3. Observe `Passed: 1263, Failed: 3, Skipped: 1` and the three tests above.
4. (CI) push to `dev`; the `Build and Test` job fails with exit code 1.

Confirmed both locally (this session) and in CI on `da99de8`.

## Suspected Code Paths

- `LivingRoots.Tests/ProductionSourceGuardTests.cs:448` and `:476` — hardcode `Path.Combine(productionRoot, "Constants.cs")` followed by `Assert.True(File.Exists(constantsPath), …)`. The file is now `ModConstants.cs`. Messages/comments at `:437,:449,:461,:465,:474,:486,:528` also say `Constants.cs`.
- `LivingRoots/ModConstants.cs:18` — `public const int MaxTilledTiles = 40000;` has **zero production references** (read-only check: the declaration is the only occurrence). This is the real finding the broken guard is currently masking.
- `LivingRoots/Domain/UnicodeNormalizationService.cs:58,71` — `Normalize` runs `NormalizationForm.FormD` then `FormC` (canonical only). Fullwidth `Ａ Ｂ Ｃ` (U+FF21–FF23) is neither compatibility-mapped to ASCII nor listed in `SecurityConfusables`/`SimplifyCharacter`, so it passes through unchanged.
- `LivingRoots.Tests/Invariants/SecuritySanitizationInvariantsTests.cs:74-82` — the fullwidth expectation (`Normalize("ＡＢＣ") == Normalize("ABC")`). File introduced by `2404b9e` ("feat: define US1 … (TDD red)").
- `LivingRoots/Domain/PathValidationService.cs:75,219` — fullwidth solidus / full stop **are** handled here (context: the project already treats fullwidth as a path-traversal vector, just not in the Unicode normalizer).

## Root Cause Hypothesis

Confidence: **high**. Three independent items of test-suite debt, none a production regression:

1. **Stale filename after a rename (2 failures).** Commit `9661ea8` renamed `LivingRoots/Constants.cs` → `LivingRoots/ModConstants.cs` (git records it as a 100% rename) but did not update `ProductionSourceGuardTests.cs`, which still hardcodes `"Constants.cs"`. Both guard tests abort at `File.Exists`. Because the guard is broken, it *masks* a genuine finding: `ModConstants.MaxTilledTiles` — added in spec 005 T008 explicitly "used by T071 chunk-activation threshold" — is never referenced by production code.
2. **TDD-red test never greened (1 failure).** `SecuritySanitizationInvariantsTests.cs` was added in `2404b9e` as a "TDD red" commit. The fullwidth test asserts **compatibility** normalization (FormKC/FormKD), but the implementation performs **canonical-only** normalization (FormD/FormC). The sibling `UnicodeNormalizer_NormalizesToFormC` passes, confirming the implemented contract is FormC.

Read-only verification of the post-path-fix outcome:
- `FalseComment_InvalidDropInShake_MatchesCode` → **would pass** (`ModConstants.cs:95` contains `shakeTimer`).
- `ProductionSources_NoUnreferencedConstantsSurvive` → **would fail** on `MaxTilledTiles` (1 unreferenced constant of 60 parsed).

## Proposed Remediation

**Preferred** — repair the tests, then resolve the one real finding they were masking:

1. In `ProductionSourceGuardTests.cs`, replace the two hardcoded `"Constants.cs"` paths (and the related messages/comments) with `ModConstants.cs`.
2. Resolve `ModConstants.MaxTilledTiles`: wire it into the crop-growth scan's chunk-activation bound in `ModController.ApplySoilGrowthModifier` (spec 005 T071), or delete it if that bound is expressed elsewhere. Wiring is preferred — the constant is the declared single source of truth.
3. Pick the Unicode contract:
   - **(a)** Satisfy the test by mapping the fullwidth ASCII block `U+FF01–U+FF5E → U+0021–U+007E` inside `UnicodeNormalizationService` (targeted, low-risk hardening for a known homoglyph class), or
   - **(b)** Narrow the test to the FormC contract and document that fullwidth is handled by `PathValidationService`.

**Alternatives**:
- Switch the normalizer's final pass to `NormalizationForm.FormKC`. Broad: it compatibility-decomposes many characters (ligatures, superscripts, enclosed forms), changing behavior across the pipeline and requiring a regression review of the "preserve non-Latin script" rules.
- Mark the two guard tests `[Skip]`. Rejected — it hides the `MaxTilledTiles` finding and defeats the guard's purpose.

**Files likely to change**:
- `LivingRoots.Tests/ProductionSourceGuardTests.cs`
- `LivingRoots/ModConstants.cs` and/or `LivingRoots/Controllers/ModController.cs` (wire `MaxTilledTiles`)
- `LivingRoots/Domain/UnicodeNormalizationService.cs` and/or `LivingRoots.Tests/Invariants/SecuritySanitizationInvariantsTests.cs`

**Tests to add or update**:
- Update the two guard tests to `ModConstants.cs` and confirm they pass (the zero-unreferenced guard already exists; it just needs a valid path).
- If option (a): a normalizer test pinning the fullwidth→ASCII mapping, plus a test that legitimate non-Latin script (Cyrillic/Greek) is still preserved.
- If `MaxTilledTiles` is wired: a spec-005 chunk-activation test asserting the 40,000-tile bound.

## Risks & Considerations

- Fixing the guard path may surface **more** unreferenced constants than `MaxTilledTiles`; each must be wired or removed.
- `FormKC` (alternative) is a broad behavioral change to a security-sensitive normalizer — review the Cyrillic/Greek context-preservation rules for regressions.
- These failures are **pre-existing and long-standing**; they are independent of the SonarCloud work merged in `da99de8`. `Build and Test` is deliberately **not** a required status check (see `AGENTS.md`), which is exactly why the red has persisted unnoticed.
- Test-only changes carry no runtime risk. Wiring `MaxTilledTiles` is a production change in the crop-growth path and should be covered by the spec-005 tests.
- The CI also reports 11 `CS8602` (possible null dereference) warnings in `LivingRoots.Tests/Invariants/SoilHealthPersistenceInvariantsTests.cs` — unrelated to the failures but worth a separate cleanup.

## Open Questions

- [NEEDS CLARIFICATION: Is fullwidth normalization a requirement of spec 005 FR-007, or was the fullwidth test an aspirational TDD-red that should be re-scoped? Determines option (a) vs (b) above.]
- [NEEDS CLARIFICATION: Should `MaxTilledTiles` gate the growth scan (wire it) or be removed?]
