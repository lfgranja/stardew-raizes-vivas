# Asset Briefing: `LivingRoots.Compost` item definition

**Priority**: P0 — blocking · **Type**: Content Patcher data patch · **Status**: not started
**Blocks**: `CompostApplicationServiceTests` (2 tests), all in-game compost collection and application

---

## 1. What exists today

`ModConstants.cs:63` declares the id:

```csharp
public const string CompostItemId = "LivingRoots.Compost";
```

Two production sites depend on it:

| Site | Code | Fails today |
|------|------|-------------|
| `CompostingBinService.cs:72` | `new StardewValley.Object(ModConstants.CompostItemId, 1)` | `KeyNotFoundException` — no type definition registered |
| `CompostApplicationService.cs:46` | `heldItem.QualifiedItemId != ModConstants.CompostItemId` | comparison never true; compost can never be applied |

But `LivingRoots/manifest.json` declares **no `Content Patches`**, and no `content.json` exists.
The item is referenced but never defined.

## 2. What must be delivered

### 2.1 `LivingRoots/manifest.json` — add the patch entry

```json
{
  "Name": "Stardew Valley - Living Roots",
  "Author": "lfgranja",
  "Version": "0.0.1",
  "Description": "Agroecological Expansion Pack for Stardew Valley",
  "UniqueID": "lfgranja.LivingRoots",
  "EntryDll": "LivingRoots.dll",
  "MinimumApiVersion": "4.0.0",
  "Content Patches": {
    "i18n/default": "content.json"
  }
}
```

> **This requires the `Content Patcher` dependency.** Add to the manifest:
> `"Dependencies": { "ContentPatcher": "2.2.0" }`. Content Patcher is the standard way to add items
> and is what `Pathoschild.Stardew.ModBuildConfig` expects; there is no alternative for `Data/Objects`.

### 2.2 `LivingRoots/content.json` — the item entry

```json
{
  "Format": "2.3.0",
  "Changes": {
    "Data/Objects": {
      "LivingRoots.Compost": {
        "DisplayName": "Compost",
        "Description": "Rich organic compost. Apply it to tilled soil to restore its health.",
        "Category": "-2",
        "ItemType": "Material",
        "Price": 10,
        "Stack": 999,
        "Icon": "i",
        "DisplayCategory": "pantry",
        "Color": "37,67,41"
      }
    }
  }
}
```

### 2.3 Field rationale

| Field | Value | Why |
|-------|-------|-----|
| `Category` | `-2` | Stardew's `Crafting` category. Keeps compost out of the `OrganicWasteValidator` accept-list (`-74`, `-75`, `-79`, `-80`, `-81`) so compost itself is not compostable — prevents a self-feeding loop. |
| `ItemType` | `Material` | In 1.6 this is what makes the item work with `Data/Objects`. |
| `Price` | `10` | Placeholder. The mod is not economy-balanced yet; see ROADMAP.md. |
| `Stack` | `999` | `CompostingBinService.CollectCompost` yields `MaturationLevel` items (1–5). Stacking is cosmetic but expected for a bulk material. |
| `Icon` | `i"` | References a sprite in the `i.png` spritesheet. **Requires briefing #2** (`compost-icon.md`). Vanilla falls back to a placeholder if the sprite is missing, so the item still works. |
| `DisplayCategory` | `pantry` | 1.6 menu grouping. Cosmetic. |
| `Color` | `37,67,41` | R,G,B used for the 1.6 item colour tint. Chosen to sit near the earth-tone palette in `ModConstants` (`HealthyColor` = `#15803D` = 21,128,61). |

## 3. ⚠️ The id must be `LivingRoots.Compost`, NOT `lfgranja.LivingRoots_Compost`

Two conventions compete in 1.6 modding:

| Convention | Example | `Item.QualifiedItemId` returns |
|------------|---------|---------------------------------|
| Legacy `UniqueID_ItemName` | `lfgranja.LivingRoots_Compost` | the same string |
| Namespace-prefixed | `LivingRoots.Compost` | the same string |

**This project uses the second form** — `ModConstants.CompostItemId = "LivingRoots.Compost"` and
`CompostingBinItemId = "LivingRoots.CompostingBin"`. Because `CompostApplicationService.cs:46`
compares against `QualifiedItemId` directly, the `content.json` key **must match the constant
exactly**, or the comparison silently fails at runtime with no error.

`DisplayName` and `Description` should still be routed through i18n (`DisplayName` may be omitted —
Content Patcher falls back to the raw key). Because the mod declares no `i18n/` folder yet, hardcoded
English strings are acceptable for now; add `i18n/default.json` when a second locale is needed.

## 4. Verification

### 4.1 Unit test gate

```bash
cd /home/luis/development/stardew-raizes-vivas
dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build \
  --filter "FullyQualifiedName~CompostApplicationServiceTests"
```

Expected: `TryApplyCompost_ValidTile_IncreasesHealth` and `TryApplyCompost_HealthCappedAtMax`
flip to passing. **Note:** these run in a unit-test host where `content.json` is *not* loaded, so
these two tests will keep failing after this asset lands. See §6.

### 4.2 In-game gate (authoritative)

1. Launch the mod; confirm SMAPI logs no red `content.json` parse error.
2. `player_add` / creative menu search for `Compost` → item exists, icon renders.
3. Right-click a filled compost bin → 1–5 compost items land in the inventory.
4. Hold compost, click tilled soil → soil health rises by `ModConstants.RestorationAmount` (15).

## 5. Related constants

`ModConstants.cs:64` declares `CompostingBinItemId = "LivingRoots.CompostingBin"` but **no code
reads it** — see [`composting-bin-item.md`](./composting-bin-item.md). Do not add it to
`content.json` in the same change; it would register an item the mod never gives, sells, or places.

## Why the other 10 failures are separate

`CompostingBinServiceTests` has 10 failures with a completely different root cause:

```
System.NullReferenceException
  at StardewValley.BellsAndWhistles.PlayerStatusList.AddSpriteDefinition(...)
  at StardewValley.Farmer..ctor()
  at LivingRoots.Tests.CompostingBinServiceTests.<test>()
```

`CollectCompost(string, Vector2, Farmer)` takes a concrete `Farmer`, so the test must call
`new Farmer()`. That constructor dereferences sprite data that only exists once the game has booted,
and `Farmer.addItemToInventoryBool` is **not virtual**, so Moq cannot substitute it.

This needs a production change, not an asset: extract an inventory seam so `CollectCompost` does not
depend on a concrete `Farmer`. Recommended shape:

```csharp
// LivingRoots/Domain/IPlayerInventory.cs
public interface IPlayerInventory
{
    /// <summary>Adds an item to the player's inventory.</summary>
    /// <param name="item">Item to add.</param>
    /// <returns>True if the item was added.</returns>
    bool TryAddItem(Item item);
}
```

`CollectCompost` would then build the item and hand it to `IPlayerInventory`, letting tests inject a
recording fake. **Do not attempt to solve this with an asset or a content patch.**

## Out of scope

- Economy balance (`Price`) — no values in `ModConstants` yet
- Crafting recipe (`Data/Recipes`) — compost is produced by the bin, not crafted
- `i18n/` localization — see §3
- Sprites — see [`compost-icon.md`](./compost-icon.md)