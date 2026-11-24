# Contract: Heirloom Seed Registration (data layer)

**Feature**: `020-heirloom-seeds-pipeline` | **Consumers**: C# registration in `ModEntry.OnAssetRequested`, `AssetRequestedTests`, Gate 3 runner

## C-1 `Data/Objects` — seed entry

```jsonc
// LivingRoots_HeirloomParsnipSeeds
{
  "Name": "LivingRoots_HeirloomParsnipSeeds",
  "DisplayName": "<i18n: item.heirloom.parsnip-seed.name>",
  "Description": "<i18n: item.heirloom.parsnip-seed.description>",
  "Type": "Seeds",
  "Category": -74,
  "Price": 35,
  "Texture": "Mods/lfgranja.LivingRoots/HeirloomSeeds",
  "SpriteIndex": 0,
  "ContextTags": ["heirloom_seed", "seed_item"]
}
```

Contract tests (MUST): key present; `Type="Seeds"`; `Category=-74`; `Price=35` (provisional); `Texture` points to a mod-served asset (served via `LoadFromModFile`); `DisplayName`/`Description` resolve through active translator; `ContextTags` contain `heirloom_seed`.

## C-2 `Data/Objects` — produce entry

```jsonc
// LivingRoots_HeirloomParsnip
{
  "Name": "LivingRoots_HeirloomParsnip",
  "DisplayName": "<i18n: item.heirloom.parsnip.name>",
  "Description": "<i18n: item.heirloom.parsnip.description>",
  "Type": "Basic",
  "Category": -75,
  "Price": 45,
  "Texture": "Mods/lfgranja.LivingRoots/HeirloomProduce",
  "SpriteIndex": 0,
  "ContextTags": ["heirloom_produce", "compostable_item", "category_vegetable"]
}
```

Contract tests (MUST): key present; `Type="Basic"`; `Category=-75`; passes `OrganicWasteValidator` (simulated via tag rules); `Texture` served.

## C-3 `Data/Crops` — crop entry (key = seed ID)

```jsonc
// key: LivingRoots_HeirloomParsnipSeeds
{
  "Seasons": ["spring"],
  "DaysInPhase": [1, 2, 2, 2],          // sum == 7 (invariant)
  "RegrowDays": -1,
  "IsRaised": false,
  "IsPaddyCrop": false,
  "NeedsWatering": true,
  "HarvestItemId": "LivingRoots_HeirloomParsnip",
  "HarvestMinStack": 1,
  "HarvestMaxStack": 1,
  "ExtraHarvestChance": 0.1,
  "HarvestMethod": "Grab",
  "HarvestMinQuality": 0,
  "TintColors": [],
  "Texture": "Mods/lfgranja.LivingRoots/HeirloomCrops",
  "SpriteIndex": 0,
  "CountForMonoculture": true,
  "CountForPolyculture": true
}
```

Contract tests (MUST): key == seed item ID (exact); `DaysInPhase` sums to 7; `HarvestItemId` resolves to a registered object key; `Texture` asset served; `Seasons` = only `spring`.

## C-4 `Data/Shops` — `Traveler` entry

```jsonc
// <existing Traveler ShopData>.Items += {
//   "Id": "LivingRoots_HeirloomParsnipSeeds",
//   "ItemId": "(O)LivingRoots_HeirloomParsnipSeeds",
//   "Price": 35,
//   "AvailableStock": 5,
//   "AvailableStockLimit": "Player",
//   "Condition": "SEASON Spring"
// }
```

Contract tests (MUST): the entry is present with `AvailableStockLimit == "Player"`; `Condition` is documented and covered (or disclosed deviation fallback to unconditional entry); Pierre `"SeedShop"` asset is **not** modified (diff assertion or absence test on the seeds' absence from SeedShop — verify by unit: no SeedShop edit branch registered).

## C-5 Texture assets

Served via `e.LoadFromModFile<Texture2D>("Assets/heirloom_seeds.png", AssetLoadPriority.Medium)` etc.; must be 16×16 (icons) / 16×32-per-frame spritesheet (crop row: 8 frames), 32-bit RGBA PNG, committed to repo.

## C-6 i18n keys

`item.heirloom.parsnip.name`, `item.heirloom.parsnip.description`, `item.heirloom.parsnip-seed.name`, `item.heirloom.parsnip-seed.description`, `message.heirloom.seed-saved`, `message.heirloom.seed-saved-inventory-full`, `message.heirloom.plant-bonus-authority-unsupported` — present in `default.json`, `en.json`, `pt.json` (and only these); parity + tone checks by test.
