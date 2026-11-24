# Asset Briefing: `LivingRoots.CompostingBin` item definition

**Priority**: P3 — deferred · **Type**: Content Patcher data patch + object spritesheet · **Status**: blocked on a design decision
**Blocks**: nothing. No code reads this constant.

---

## 1. Current state

`ModConstants.cs:64` declares:

```csharp
public const string CompostingBinItemId = "LivingRoots.CompostingBin";
```

A repo-wide search finds **zero reads** of this constant outside its own declaration. The only
mention in any spec is `specs/002-soil-decay-compost/tasks.md:30`, which lists it among constants to
add.

`CompostingBinService` does **not** place, placeable-check, or spawn a bin. It tracks state keyed by
`(locationName, tile)` in an in-memory cache and a JSON sidecar:

```
_runtimeCache: Dictionary<string, Dictionary<string, CompostingBinStateModel>>
LoadData / SaveData via IModDataService
```

The bin is addressed purely by coordinates. `ModController.OnButtonPressed` reads the bin state at
the cursor's tile and adds waste or collects compost accordingly — there is no object in the world.

**Consequence:** registering this item in `content.json` today would produce an item the mod never
gives the player, never places in the world, and never sells. It would be dead content.

## 2. Why it is deferred

The road to a placeable composter requires a design decision that has not been made:

| Question | Options |
|----------|---------|
| What kind of object? | `Data/Objects` placeable (BigCraftable-style), `Data/BigCraftables`, or a `Object` with `"Placeable": true` |
| Who places it? | Player buys it and places it (shop entry + placement hook), or a quest/recipe grants it (ROADMAP mentions an "Agroecological Welcome Kit" replacing the parsnip seeds) |
| How does placement interact with `_runtimeCache`? | Currently keyed by tile coordinates from a right-click. A real placed object needs a `placed` state and a link from the object's tile to the cached bin. |
| Does the item survive save/load? | `CompostingBinData` persists per location; a placed object must resolve back to the same cache entry, or state desyncs between world and data. |

Until that is decided, any `content.json` entry would be a guess that likely has to be rewritten.

## 3. What would need delivering *when unblocked*

Not authored now — recorded so the eventual work is not rediscovered from scratch.

### 3.1 Content

```json
{
  "Format": "2.3.0",
  "Changes": {
    "Data/Objects": {
      "LivingRoots.CompostingBin": {
        "DisplayName": "Composting Bin",
        "Description": "Turns organic waste into rich compost over several days.",
        "Category": "-5",
        "ItemType": "Crafting",
        "Placeable": true,
        "Stack": 1,
        "Icon": "i",
        "DisplayCategory": "crafting",
        "Color": "87,68,51"
      }
    }
  }
}
```

### 3.2 Sprites

Unlike the compost icon, a placeable object **requires** sprites for every animation frame — it
cannot fall back to a placeholder.

| Sprite | Size | Notes |
|--------|------|-------|
| `CompostingBin` | 16 × 32 | Single tile, drawn on the ground |
| `CompostingBin_empty` | 16 × 32 | No visible contents |
| `CompostingBin_filling` | 16 × 32 | Partial contents — 3 or 4 frames suggested |
| `CompostingBin_ready` | 16 × 32 | Full contents, slight visual "glow" or steam |

All four map to the bin's three states plus the transition. `CompostingBinState` is
`Empty=0, Processing=1, Ready=2` (`LivingRoots/Domain/AGENTS.md`), so four sprites cover
Empty → Processing → Ready → collected-to-Empty.

### 3.3 Code

- Remove the orphaned constant, or start consuming it.
- Add placement handling in `ModController` (currently `OnButtonPressed` assumes the bin already
  exists at the cursor tile).
- Reconcile placed objects with `CompostingBinData` on save/load.

## 4. Recommendation

**Do not deliver now.** Either

1. Delete `ModConstants.CompostingBinItemId` as dead code (Principle V — YAGNI), **or**
2. Open a spec for the placeable composter and deliver asset + code together.

Shipping an unreferenced item into the game's item table is worse than shipping nothing: it shows up
in the creative menu and in search, inviting players to use a feature that does not exist.

## 5. Out of scope

Everything in §3 — blocked on the design decision in §2.