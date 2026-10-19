# Bug Fix: "Build and Test" red — repaired the stale guard path, wired the dead constant, and implemented fullwidth normalization

- **Slug**: build-and-test-failures
- **Fixed**: 2026-10-19
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

All three failing tests are fixed. `ProductionSourceGuardTests` now reads `ModConstants.cs` (the file was renamed from `Constants.cs` in `9661ea8`), the constant that fix exposed as dead (`MaxTilledTiles`) is now wired into the growth scan as spec 005 T071's chunk-activation threshold, and the Unicode normalizer now maps the fullwidth ASCII block so `"ＡＢＣ"` normalizes to `"ABC"`. The full suite is green.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots.Tests/ProductionSourceGuardTests.cs` | modified | `Constants.cs` → `ModConstants.cs` (10 occurrences); removed stale `:98-100` line references and the outdated "fails today: CompostCraftingSound, …" list. |
| `LivingRoots/Controllers/ModController.cs` | modified | `ApplySoilGrowthModifier` now wires `ModConstants.MaxTilledTiles` as the chunk-activation threshold (T071): chunk at ~1,000 tiles only at/above the 40,000-tile budget, single pass below it. |
| `LivingRoots/Domain/UnicodeNormalizationService.cs` | modified | `SimplifyCharacter` maps the fullwidth ASCII block `U+FF01–U+FF5E → U+0021–U+007E`; `<remarks>` conversion rules updated. |
| `LivingRoots.Tests/Invariants/SecuritySanitizationInvariantsTests.cs` | modified | Added 2 tests: fullwidth digits/punctuation → ASCII, and non-Latin (CJK) preservation. |

## Diff Highlights

```csharp
// ProductionSourceGuardTests.cs — the rename that broke the guard
var constantsPath = Path.Combine(productionRoot, "ModConstants.cs");
Assert.True(File.Exists(constantsPath), $"ModConstants.cs not found at {constantsPath}.");
```

```csharp
// ModController.ApplySoilGrowthModifier — wire MaxTilledTiles (T071)
int chunkSize = tilledTiles.Count >= ModConstants.MaxTilledTiles
    ? 1000
    : Math.Max(1, tilledTiles.Count);
var chunkStarts = GetChunkStartIndices(tilledTiles.Count, chunkSize);
foreach (var chunkStart in chunkStarts)
{
    int chunkEnd = Math.Min(chunkStart + chunkSize, tilledTiles.Count);
```

```csharp
// UnicodeNormalizationService.SimplifyCharacter — fullwidth → ASCII
if (c >= '\uFF01' && c <= '\uFF5E')
{
    return ((char)(c - 0xFEE0)).ToString();
}
```

## Tests Added or Updated

- `SecuritySanitizationInvariantsTests.UnicodeNormalizer_NormalizesFullwidthDigitsAndPunctuation` — pins the full-block mapping (`１２３．４` → `123.4`), not just letters.
- `SecuritySanitizationInvariantsTests.UnicodeNormalizer_PreservesNonLatinScript` — guards against over-normalization (CJK passes through unchanged).
- The three previously-failing tests now pass unchanged: `UnicodeNormalizer_NormalizesFullwidthCharacters`, `ProductionSourceGuardTests.FalseComment_InvalidDropInShake_MatchesCode`, `ProductionSourceGuardTests.ProductionSources_NoUnreferencedConstantsSurvive`.

## Local Verification

- Build: `GamePath=… dotnet build Stardew-LivingRoots.sln -c Release` → **0 errors**.
- Targeted: `dotnet test --filter "FullyQualifiedName~UnicodeNormalizer|FullyQualifiedName~ProductionSourceGuardTests"` → **47/47 passed** (was 3 failing).
- Full suite: `dotnet test --no-build -c Release` → **1268 passed, 1 skipped, 0 failed** (green). An earlier run showed only the pre-existing flaky `T031b_OverlayDistinguishable_DistinctHealth_DistinctColors`, which passes in isolation.
- Format gate: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → **exit 0**.
- Manual: read-only confirmation that `MaxTilledTiles` is now referenced by production (it was the sole unreferenced constant of 60 parsed), so `ProductionSources_NoUnreferencedConstantsSurvive` is no longer vacuous or red.

## Deviations from Assessment

- **Followed the preferred remediation** (wire `MaxTilledTiles`; Unicode option (a)), so no material deviation. Two implementation choices worth recording:
  1. The wiring expresses T071's "activate only at/above 40,000 tiles" as a tile-count gate (`>= ModConstants.MaxTilledTiles`) rather than the Stopwatch-based measurement T071 describes. The output is identical (chunking only reorders independent per-tile work); a Stopwatch gate would be a larger, perf-sensitive change out of this bug's scope.
  2. The fullwidth fix is a **targeted block mapping**, not a switch to `FormKC` — this keeps the existing FormC contract (and its Cyrillic/Greek preservation rules) untouched.
- Cleaned two stale artifacts the assessment noted in passing: the `:98-100` line references (the file has since changed) and the "fails today: CompostCraftingSound, …" list (those constants no longer exist).

## Follow-ups

- `GrowthPerformanceTests.cs` still hardcodes `40000` in several places; T071 intended it to use `ModConstants.MaxTilledTiles`. Consider replacing the literals with the constant.
- Pre-existing flaky property tests unrelated to this fix: `T031b_OverlayDistinguishable_DistinctHealth_DistinctColors` (a real `ColorInterpolationService` category gap at `(70, 71)`) and `T222_GetOverlays_HealthZero_RendersZeroOverlays`.
- 11 `CS8602` (possible null dereference) warnings in `LivingRoots.Tests/Invariants/SoilHealthPersistenceInvariantsTests.cs` — worth a separate cleanup.
- CI confirmation: the local suite is green; the `Build and Test` workflow should turn green on the next push (verify in `/speckit.bug.test`).
