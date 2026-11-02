# Internal Contract: Harmony Patch for Crop.harvest (spec 015)

**Feature**: `015-harmony-harvest-seam` | **Type**: Internal (no public API)

---

## 1. EnableHarmony (.csproj)

- Add `<EnableHarmony>true</EnableHarmony>` to the first `<PropertyGroup>` in `LivingRoots/LivingRoots.csproj`.
- Uses SMAPI-bundled Harmony 2.2.x (no separate version pin).
- SMAPI shows a stability warning at load (expected and acceptable for Harmony mods).

## 2. Patch Registration (ModEntry.Entry)

- Use the code API (not annotations or `PatchAll`):
  ```csharp
  var harmony = new Harmony(this.ModManifest.UniqueID);
  harmony.Patch(
      original: AccessTools.Method(typeof(StardewValley.Crop), nameof(StardewValley.Crop.harvest)),
      prefix: new HarmonyMethod(typeof(HarvestPatches), nameof(HarvestPatches.HarvestPrefix)),
      postfix: new HarmonyMethod(typeof(HarvestPatches), nameof(HarvestPatches.HarvestPostfix)));
  ```
- Patch methods MUST be `static`.
- Wrap patch logic in `try/catch`; log errors via `IMonitor`; default to original game logic on failure (prefix returns `true`; postfix skips modification).
- Prefer postfix over prefix (SMAPI guidance); avoid transpilers unless no alternative.

## 3. Patch Style: Non-Suppressing Two-Phase

- **Prefix** (returns `true` — original still runs):
  - Apply crop-scope guard early (return early / set a skip flag for forage/fibre/Qi).
  - Capture inputs (soil health, Farming level, fertilizer tier, etc.) into `__state` for the postfix.
  - Adjust effective extra-harvest probability for Healthy tier before vanilla consumes it (if feasible; otherwise spawn extra manually in postfix).
- **Postfix** (runs after original):
  - Force `Quality = 0` for Poor tier (overwrite the rolled quality on the harvested `Object`).
  - Apply Healthy boost as a conditional upgrade of the already-rolled `Quality` (documented deviation from strict "+0.08 on p").
  - Drive `VisualizationService` feedback per conditionality (B1/B2): Poor always; Living only on enhanced quality or extra; both as two sprites when both hold; Moderate/Unknown none.
  - Handle inventory-full: main item vanilla-blocked (red message, crop stays); extra item dropped as debris via `Game1.createItemDebris` (vanilla semantics).
- **Suppressing prefix (return false) is never acceptable** — would skip the entire harvest pipeline and force re-implementing vanilla harvest (violates FR-003 "compose, don't replace" and breaks other mods' patches).
- **Transpiler** is the documented fallback (last resort) if exact in-method roll modification is later required (SMAPI warns transpilers are fragile and discouraged).

## 4. Crop Scope (A6)

- Early return (no modification, no feedback) for forage crops (`crop.forageCrop == true`), fibre seeds, Qi Fruit.
- Fruit trees, giant crops, tea bushes, ground-picked forage are not `Crop` instances (separate terrain features) — out of scope; the patch only fires inside `Crop.harvest`, invoked by `HoeDirt.performUseAction`, `HoeDirt.performToolAction`, `JunimoHarvester.update`.
- Flowers, multi-harvest crops, paddy crops, regrowing crops are in scope.

## 5. Seam Contract (IHarvestQualityService)

- Location: `LivingRoots/Domain/IHarvestQualityService.cs` (interface), `LivingRoots/Domain/Services/HarvestQualityService.cs` (impl), `LivingRoots/Domain/HarvestQualityTypes.cs` (types).
- Namespace: `LivingRoots.Domain` (interface + types), `LivingRoots.Domain.Services` (impl).
- Wired in `ModEntry` as `new HarvestQualityService()` (constructor injection).
- Pure: no SMAPI/game dependencies, no I/O, no RNG, no inventory mutation. Testable in headless host.
- References `ModConstants.SoilQualityHealthyBoost` and `ModConstants.SoilExtraHarvestHealthyBoost` (satisfies `ProductionSourceGuardTests`).
- Host-authority: replicated deterministic roll (A8) — seam pure/deterministic given inputs so both clients agree.

### Types

```csharp
public enum HarvestTier { Unknown = 0, Poor = 1, Moderate = 2, Healthy = 3, OutOfScope = 4 }
[Flags] public enum HarvestFeedbackFlags { None = 0, Poor = 1, LivingQuality = 2, ExtraHarvest = 4 }
public readonly record struct HarvestQualityInputs(int FarmingLevel, int FertilizerTier, bool IsPaddyCrop, int RegrowDays, int HarvestMaxIncreasePerFarmingLevel, bool IsForageCrop, bool IsFibreSeed, bool IsQiFruit, int? PestQualityPenalty, bool IsInventoryFull);
public readonly record struct HarvestQualityDecision(HarvestTier Tier, bool IsInScope, bool ForceQualityZero, float QualityBoost, float ExtraHarvestBoost, int? PestQualityPenalty, bool MainItemBlocked, bool ExtraItemDroppedAsDebris);
```

### Interface

```csharp
public interface IHarvestQualityService
{
    HarvestQualityDecision Decide(float soilHealth, HarvestQualityInputs inputs);
    HarvestFeedbackFlags ResolveFeedback(HarvestQualityDecision decision, bool enhancedQualityOccurred, bool extraHarvestOccurred);
    float BoostQualityProbability(float vanillaProbability, HarvestQualityDecision decision);
    float BoostExtraHarvestChance(float vanillaChance, HarvestQualityDecision decision);
}
```

## 6. Feedback Visibility (Hybrid Model)

- **Broadcast** transient feedback when it represents a shared farm-state outcome observable by all players (harvest quality boost, extra harvest, compost "+15", machine ready, lock collision) via `Game1.multiplayer.broadcastSprites` + `location.playSound`.
- **Local-only** when it is the triggering player's own input (hoe flash 300 ms, hoe floating text) via `SpriteBatch`.
- State-derived rendering (overlay, tooltip) is not a sprite — re-derived per client.
- Host is the only caller of `broadcastSprites` (host-scoped guard, matching vanilla `FarmerTeam.OnRequestHorseWarp`); farmhands forward intent via SMAPI `SendMessage`.
- Single-player is the degenerate case (broadcast is a no-op locally).

## 7. Safety Contract

- Patch wraps logic in `try/catch`; logs errors via `IMonitor`; defaults to original game logic on failure.
- No raw exception messages to logs (Principle II).
- Crop scope guards prevent invalid application.
- `CheckForOverflowUnderflow` enabled project-wide (no suppression).
- Async-only (Principle III): patch is synchronous game-thread callback (no I/O).

## 8. Android / Cross-Platform Contract

- Accept "untested on Android" with documentation (README, Nexus page: do not tick "Android Compatible"; optional runtime guard; best-effort fallback to vanilla on patch failure).
- Consistent with existing desktop-only policy (GDD: "Sem console/mobile no v1").
- SMAPI rewrites mods but cannot rewrite annotation-based patches/`PatchAll` (hence code API + static + try/catch).
- Harmony on Android uses MonoMod.RuntimeDetour backend (ARM); higher risk. Warn and accept untested.

## 9. Host Authority Contract (A8)

- Replicated deterministic roll: soil health synced host→client on `DayStarted` via `SendMessage`; modifier applied locally on triggering client reading synced soil health; SV single-harvest guarantee ensures extra-harvest roll fires once; results deterministic given shared inputs (DaysPlayed, uniqueID, tile, fertilizer net field).
- Host takes same local path (no message round-trip when `Context.IsMainPlayer`).
- Shared farm state (soil health) remains host-authoritative; per-player harvest execution is client-local.
- Divergence rule: host state wins; client re-syncs soil health on next `DayStarted`.
- Seam must be pure/deterministic given inputs so both clients agree.

## 10. Guard Compliance

- `ProductionSourceGuardTests.NoUnreferencedConstantsSurvive`: seam references `SoilQualityHealthyBoost`/`SoilExtraHarvestHealthyBoost`.
- `FeatureStatusTests`: do not modify README " (Planned)" strings without updating the test.
- No new `Assets/*.png`, no new `i18n` keys for feedback text (literal), no `Data/Crops`/`Data/Objects` edits.
- No persistent per-tile tier marker.
- `SC-008` rollback verification (spec 014) is an absolute blocker for yield activation (not for design/tests).
- Gate 3 in-game session (spec 014 Q10) captures 3 signals with second player for multiplayer verification (D1).
