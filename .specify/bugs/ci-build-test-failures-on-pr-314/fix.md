# Bug Fix: CI build/test failures on PR #314 (applied to run 40793022307 / PR #518)

- **Slug**: ci-build-test-failures-on-pr-314
- **Fixed**: 2026-10-28
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The assessment on disk targets an older CI run (run `39952935432`, PR #314) whose
symptom was a compile failure. The failure this fix actually remediates is run
`40793022307` (job `123182606520`, PR #518, branch
`story/010-spec-and-contracts`, commit `758325f`): the test project compiled but
**83 of 1,425 tests failed**. The production and test sources were reconciled to
the branch's governing contracts — `HealthCategory` bands `[0,34) [34,67)
[67,100]`, measured `0` versus unknown `NaN`, the validator-owned compostable
set, host-authoritative lock semantics, and the source-guard contracts — until
the full local suite passed.

## Changes

### Production

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Constants.cs` | modified | Added `FloatComparisonEpsilon`; removed 12 unreferenced constants and six palette colors that no production consumer referenced; kept `CompostSpriteColor`. |
| `LivingRoots/Controllers/ModController.cs` | modified | Growth modifiers now use `PoorModerateBandEdge`/`ModerateHealthyBandEdge`; building-footprint refusal in `TryPlaceBin`; absent-lock paths fail closed with the conflict key; dislodgement routes to the acting farmer's inventory and clamps progress bins to one; `OnObjectListChanged` reconstructs the dislodged batch before pruning and copies its modData; corrected false comments; shake timer uses `BinInvalidDropInShakeTimerMs`; sanitized exception logging. |
| `LivingRoots/Domain/Interfaces/ICompostingBinService.cs` | modified | Corrected XML contracts: exactly-one batch yield, exactly-once day guard, no ground drop on removal. |
| `LivingRoots/Domain/Models/CompostingBinData.cs` | modified | `[JsonIgnore]` on derived runtime counters (`MaturationLevel`, `ConsecutiveIdleDays`, `ConsecutiveActiveDays`) so they are not persisted as derived state. |
| `LivingRoots/Domain/Services/OrganicWasteValidator.cs` | modified | Hybrid validation: inclusion tag `compostable_item`, exclusion tag `not_compostable` override, and vanilla category IDs (`-74`, `-75`, `-79`, `-80`, `-81`) with tag/category consistency. |
| `LivingRoots/Domain/UnicodeNormalizationService.cs` | modified | Fullwidth ASCII (U+FF01–U+FF5E) and ideographic space (U+3000) normalize to ASCII, closing a homoglyph path without broad NFKC corruption. |
| `LivingRoots/Domain/Visualization/IAccessibilityNotifier.cs` | modified | Removed the unused SMAPI `using`, restoring the domain-layer boundary. |
| `LivingRoots/ModEntry.cs` | modified | `Entry` catches wiring failures, logs a generic Error line plus sanitized type/HResult at Trace, and delegates to `WireCompositionRoot`; machine triggers derive from the validator-owned waste set with `!not_compostable`. |
| `LivingRoots/Services/CompostingBinService.cs` | modified | State-machine rewrite: no monitor/audio/HUD calls inside `lock`; temp-cache load/swap then prune outside the lock; invariant `ParseTileKey`/`GetTileKey`; duplicate day-start logs `already processed`; rejection log says `rejected invalid drop-in`; shake timer from `ModConstants`. |
| `LivingRoots/Services/FarmerInventoryAdapter.cs` | modified | Inserts into `Farmer.Items` directly (stack-compatible slot, first empty slot, append under `Farmer.maxInventorySpace`) because `addItemToInventoryBool` dereferences the unset `Game1.player` in the headless host. |
| `LivingRoots/Services/SaveIdProvider.cs` | modified | Exception log uses `GetType().FullName` (type detail without raw message). |
| `LivingRoots/Services/Visualization/OverlayRenderer.cs` | modified | Cache key is the visible tile-health snapshot compared with `FloatComparisonEpsilon`; explicit NaN/infinity handling; per-screen cache and invalidation. |
| `LivingRoots/Services/Visualization/VisualizationService.cs` | modified | Emits `Overlay drawn: {value}` and `Overlay tint drawn: RGBA(...)`; config changes invalidate the cache; patterns are suppressed below `PatternMinOpacity`. |
| `LivingRoots/i18n/default.json`, `en.json`, `pt.json` | modified | Added `message.accessibility.patterns-hidden` with translated fallback text. |
| `specs/001-soil-health-visualization/spec.md` | modified | Corrected the single-player assumption wording to the host-authoritative/multiplayer-degenerate-case contract. |

### Tests

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots.Tests/BinPickupTests.cs` | modified | Stubs `GetMaxStackSize` = 1 for the progress bin so the production clamp bound is actually exercised (Moq's default `0` is not a valid cap). |
| `LivingRoots.Tests/BinPlacementTests.cs` | unchanged this pass | The building-footprint and batch-preservation red tests now pass against production. |
| `LivingRoots.Tests/CompostingBinServiceTests.cs` | modified | Interaction fixtures inject a real `HostAtomicLockService`; HUD count delta replaced with translated-text presence. |
| `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` | modified | Absent-lock paths assert fail-closed refusal and translated conflict text by presence, not count delta. |
| `LivingRoots.Tests/ProductionSourceGuardTests.cs` | modified | Stale `ModConstants.cs` paths corrected to `Constants.cs`; corrected-comment contracts aligned. |
| `LivingRoots.Tests/Architecture/NoHardcodedVisualizationLiteralsTests.cs` | modified | Allowlist reconciled with the deleted speculative palette entries and the live `CompostSpriteColor`. |
| `LivingRoots.Tests/Invariants/SingleMultiplayerParityInvariantsTests.cs` | modified | Allow-list matches the production gate line (not a line number); scan hits include the trimmed source line; stale “EMPTY” comment corrected. |
| `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` | modified | Tier expectations updated to 33/66 band edges. |
| `LivingRoots.Tests/Invariants/SoilHealthCategoryInvariantsTests.cs` | modified | Category expectations updated to 34/67. |
| `LivingRoots.Tests/Invariants/SoilHealthBoundsInvariantsTests.cs` | modified | Bounds semantics: measured `0` is Poor; unmeasured/unknown reads back as `NaN`. |
| `LivingRoots.Tests/ContractTests/SoilHealthServiceContractTests.cs` | modified | Contract expectations aligned to the `0`/`NaN` distinction. |
| `LivingRoots.Tests/SaveReloadConsistencyTests.cs` | modified | Save/reload round-trip expectations aligned to the same distinction. |
| `LivingRoots.Tests/SoilHealthServiceTests.cs` | modified | Explicit-zero round-trip and NaN-unknown expectations. |
| `LivingRoots.Tests/SoilHealthServiceDosProtectionTests.cs` | modified | Tile-filter expectations aligned to measured-zero handling. |
| `LivingRoots.Tests/Visualization/ColorInterpolationServiceTests.cs` | modified | Band edges 34/67. |
| `LivingRoots.Tests/Visualization/HealthCategoryTests.cs` | modified | Band edges 34/67. |
| `LivingRoots.Tests/VisualizationServiceTests.cs` | modified | Draw value/tint log expectations. |
| `LivingRoots.Tests/AssetPresenceTests.cs`, `AssetRegistrationTests.cs` | modified | Asset/source paths corrected (`Constants.cs`). |

## Diff Highlights

Band edges replace the superseded 30/70 literals:

```csharp
float multiplier = (health < ModConstants.PoorModerateBandEdge)
    ? ModConstants.SoilGrowthPoorMultiplier
    : (health >= ModConstants.ModerateHealthyBandEdge)
        ? ModConstants.SoilGrowthHealthyMultiplier
        : 1.00f;
```

Buildings are invalid terrain across their whole footprint, not only the origin:

```csharp
foreach (var building in location.buildings)
{
    if (building != null && building.occupiesTile(tile))
    {
        return false;
    }
}
```

A tool/explosion removal reconstructs the batch before the record is pruned:

```csharp
var dislodgedItem = _compostingBinService.CreateDislodgedBinItem(e.Location.Name, removed.Key);
// copy ModDataInputIdKey / ModDataDaysRemainingKey onto removed.Value
_compostingBinService.OnObjectRemoved(e.Location.Name, removed.Key);
```

An absent lock no longer means “no contention”:

```csharp
if (_hostAtomicLockService == null)
{
    _monitor.Log("Bin interaction refused: no host atomic lock service is injected ...", LogLevel.Warn);
    refusalHandler?.Invoke(ModConstants.TranslationKeyCompostActionConflict);
    return false;
}
```

## Tests Added or Updated

No new test cases were added. Existing red tests were aligned to the branch's
governing contracts and kept at contract level:

- `BinPickupTests.DislodgedProgressCarryingBin_ThroughProductionPath_ResolvesAsSingleUnstackableItem` — production stack clamp now has a valid `GetMaxStackSize` stub.
- `BinPlacementTests.TryPlaceBin_ForAnyBuildingFootprint_RefusesEveryFootprintTile` — now covered by the production footprint check.
- `BinPlacementTests.WorldObjectListChanged_ForToolOrExplosionRemovedProgressBin_PreservesBatchModData` — now covered by reconstruct-before-prune.
- `CompostingBinServiceTests.CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`, `MultiplayerConcurrencyTests.*AbsentLockService*` — presence-of-translated-text assertions, because `Game1.addHUDMessage` deduplicates identical `whatType` + message pairs in the process-global HUD list.
- Health-category/bounds/save-reload tests — 34/67 bands and measured-`0` vs unknown-`NaN`.
- Source/architecture guards — corrected paths, comments, allow-lists and live palette entries.

## Local Verification

- `dotnet build LivingRoots.Tests/LivingRoots.Tests.csproj --nologo -v q` → **success** (warnings only, no errors).
- `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build --nologo` → **1423 passed, 4 skipped, 0 failed, 1427 total**, ~30 s.
  - Intermediate run after the service rewrite: 17 failed; targeted fixes reduced it to 3 failed; final run is green.
- Targeted three-test run (dislodge clamp, building footprint, batch preservation) → **3 passed**.
- `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → **exit 0**.
- Manual/game-session verification: **not performed**. These changes are covered by the unit/invariant suite only; no Gate 3 gameplay claim is made.

## Deviations from Assessment

1. **Wrong run / wrong failure class.** The assessment cites run `39952935432` on PR #314 and a test-project compile failure. The failure fixed here is run `40793022307` on PR #518: compilation succeeded and 83 tests failed. The remediation was applied in spirit to the actual failing run.
2. **Wrong constants path.** The assessment names `LivingRoots/ModConstants.cs`; the class `ModConstants` lives in `LivingRoots/Constants.cs`. Test and production references were corrected accordingly.
3. **Assessment-listed missing members were not restored.** `ComputeHarvestQualityRoll`, `ComputeExtraHarvestProb`, `IHudNotifier` constructor changes and the old `OnBinPlaced` signature were not compile errors on this branch. Restoring them would have violated the unreferenced-constant source guard. Instead, the unreferenced constants (`SoilQualityHealthyBoost`, `SoilExtraHarvestHealthyBoost`, `MaxPathSegmentLength`, `MaxPathDepth`, `MaxDataValueSizeBytes`, `CompostCraftingSound`, six palette colors, six `RunnerLock*` constants) were deleted and the palette allowlist was reconciled to live entries plus `CompostSpriteColor`.
4. **Scope expansion.** Actual failing assertions exercised visualization cache/log contracts, lock fail-closed semantics, building-footprint placement, tool/explosion batch preservation, farmer inventory delivery, Unicode normalization, and i18n fallback. Those production paths were changed and are listed above; no unrelated refactor was performed.
5. **HUD count assertions became presence assertions.** `Game1.addHUDMessage` deduplicates identical `whatType` + message text in the process-global list, so a count delta is test-order dependent. Presence still pins the actual contract: translated text is shown and the raw key never leaks.
6. **Only batch keys are copied on removal.** `OnObjectListChanged` copies `ModDataInputIdKey` and `ModDataDaysRemainingKey` onto the removed world object instead of replacing its whole modData, so other mods' keys on the same object are not clobbered.

## Follow-ups

- Re-run `__SPECKIT_COMMAND_BUG_ASSESS__` for run `40793022307` / PR #518 (or the current branch head), then `__SPECKIT_COMMAND_BUG_TEST__ slug=ci-build-test-failures-on-pr-314`. `test.md` in this folder still describes the older compile-error fix and must be regenerated.
- Push and re-run CI. Per `AGENTS.md`, `Build and Test` is dormant without `STARDEW_GAME_PATH`; do not add it to required checks until it produces one genuinely green run. A green local suite is the available evidence on a GitHub-hosted runner.
- If gameplay behavior claims are needed for placement, dislodgement or tool/explosion removal, run the manual Gate 3 protocol (`VERIFICATION_PROTOCOL.md` §3) — unit results alone are not Gate 3 evidence.
- Optionally add a focused regression test asserting the batch modData actually lands on the removed world object; the current test pins the reconstruction and prune calls, not the copied key values.
