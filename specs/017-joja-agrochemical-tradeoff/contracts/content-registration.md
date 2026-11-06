# Contract: Content Registration & Player-Facing Framing (017 MVP)

## Purpose
Defines how the Joja/agrochemical input is represented to players and registered in game content/data, within MVP scope (mod-intercepted, no vanilla fertilizer hook, no mandatory new assets, no full Joja economy expansion).

## Registration boundaries
- **Composition root:** only `LivingRoots/ModEntry.cs` wires services/controllers.
- **Asset/content hooks:** follow existing `OnAssetRequested` patterns (`Data/CraftingRecipes`, `Data/Objects`, textures, `Data/BigCraftables`, `Data/Machines` used today). 017 adds only what is required for MVP.
- **No `Data/Shops` by default in MVP.** Current mod does not edit `Data/Shops`. Acquisition/Joja-only shop specifics may be finalized later; if added, verify exact SDV 1.6 Joja shop ID and keep change scoped.

## Item/content contract (MVP)
| Aspect | Contract |
|---|---|
| Input identity | Single mod-owned Joja/agrochemical input with stable ID(s). Use qualified/unqualified forms correctly for SDV 1.6 where applicable. |
| Framing | Joja/industrial shortcut with clear soil-health cost. Cozy, non-punitive, legible. |
| Application semantics | Mod-intercepted only. No claim of vanilla fertilizer-slot interop. No `HoeDirt.fertilizer` behavior. |
| Compostability/classification | Must not become compostable or be accepted by existing organic-waste/composting validation paths unless explicitly specified. Avoid tags/categories that create unintended regenerative behavior. |
| Assets | **No mandatory new art/audio assets (FR-013).** If object registration needs texture/sprite, planning must choose a minimal safe strategy consistent with no-mandatory-assets and existing asset pipeline conventions. |

## Localization contract
Player-facing text introduced/changed for 017 MUST be localized for supported languages touched by the change.

| Requirement | Contract |
|---|---|
| Files | Update `LivingRoots/i18n/default.json`, `LivingRoots/i18n/en.json`, and `LivingRoots/i18n/pt.json` for new 017 keys (parity). |
| Key style | Stable `message.agrochemical.*` / item name/description keys as finalized in planning; keep keys centralized or consistently referenced. |
| Before use | Item description/framing communicates shortcut + soil consequence. |
| After success | Immediate confirmation attributes soil consequence to the Joja/agrochemical choice (C1 confirmation/proxy only). |
| Refusal | Explicit, localized reason; unsupported farmhand path is clear and non-misleading. |

## Telegraphed tradeoff copy requirements
- **Before:** player can understand this is an industrial shortcut with soil cost.
- **After:** player can understand soil was harmed by that choice and outcome is bounded/recoverable through existing systems.
- **Refusal:** player understands why action did not apply; no false success.
- Tone: informative/cozy/non-punitive ("recompensar, nunca pregar" spirit), no shaming.

## Out of scope
- Full Joja narrative/economy/content expansion.
- Mandatory new PNG/SFX assets.
- Vanilla fertilizer category behavior or Harmony hook integration.
- Data/recipe/shop additions unrelated to this single controlled MVP input.