# Data Model: Mulch / Soil Cover

## Entities

### Mulch (item)
- **ID**: `LivingRoots.Mulch` (qualified: `(O)LivingRoots.Mulch`)
- **Category**: −26 (same as compost, `ModConstants.CompostCategoryId`)
- **ContextTags**: `{"mulch_item", "compostable_item"}`
- **Price**: 5g
- **Texture**: `Mods/LivingRoots/Mulch`, SpriteIndex 0
- **Sprite**: `LivingRoots/Assets/mulch.png` (16×16)

### Mulch Tile State
- **HasMulch**: bool (default false, omitted in sparse cache)
- **DaysRemaining**: int (default 0, omitted in sparse cache)
- **Persistence**: via `ModDataService` with `*_KeyPrefix` keys (e.g., `mulch_state_<saveId>`)
- **Scope**: per-tile, farm-keyed via `SaveIdProvider`
- **Bounds**: respects `MaxTilesPerLocation` / `MaxTilesPerSave`

### IMulchApplicationService (interface)
- **Location**: `Domain/Interfaces/IMulchApplicationService.cs`
- **Implementation**: `Services/MulchApplicationService.cs`
- **Methods**: `TryApplyMulch(Farmer who, HoeDirt hoeDirt, GameLocation location) → bool` + refusal message key
- **Pattern**: mirrors `ICompostApplicationService` (DDD, constructor injection, `new` only in `ModEntry.cs`)

### SoilDecayService extension
- **Change**: `ProcessDayStart` checks `tile.HasMulch` → `decay *= MulchDecayMultiplier (0f)`
- **Winter handling**: mulch day-countdown must run **outside** the seasonal early-return guard (`if (multiplier == 0f) return;`) so expiry is day-based, not season-based.
- **Planted tiles**: mulch applies to `HoeDirt` regardless of crop presence (no `crop == null` guard).

### OverlayRenderer extension
- **Mulch tint**: `MulchColor` (#92400E) at 0.5 opacity (`DefaultOpacity`) + `Dots` pattern
- **Composite render order**: health color base → mulch tint/pattern → dotted border (for mulch+plant)
- **Integration**: via `IVisualizationService` (never hardcoded in controller)

## Constants (ModConstants / Constants.cs)

```csharp
public const string MulchItemId = "LivingRoots.Mulch";
public const string QualifiedMulchItemId = "(O)LivingRoots.Mulch";
public const int MulchDurationDays = 7;
public const float MulchDecayMultiplier = 0f;
public const string MulchTag = "mulch_item";
public const Color MulchColor = new(146, 64, 14); // #92400E
```

## Crafting Recipe

- **Key**: `LivingRoots_MulchRecipe`
- **Format**: legacy string `"<ingredientes>/<aba>/<item qtd>/<craftable?>/<condição>/<nome>"`
- **Recipe**: `"771 10/Home/LivingRoots.Mulch 1/false/null/Mulch"` (Fiber ×10)
- **Available**: day 1, craftable at crafting menu
- **Elena shop**: deferred (Elena NPC not implemented); recipe sale is non-blocking

## i18n Keys

```json
{
  "mulch.name": "Mulch",
  "mulch.description": "Dry cover that keeps tilled soil alive. Blocks decay and weeds.",
  "message.mulch.invalid-tile": "Mulch can only be applied to tilled soil.",
  "message.mulch.already": "This tile is already mulched."
}
```

## Relationships

- Mulch tile state is independent of SoilHealthState (separate persistence key).
- Mulch state is independent of CompostingBinState (orthogonal mechanisms).
- Compost and mulch can coexist on the same tile (complementary: restore + protect).
- Mulch does NOT modify SoilHealth value (pure decay protection, no health boost).