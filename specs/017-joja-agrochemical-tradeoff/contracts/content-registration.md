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
- **After (immediate, attributed, legible):** feedback is shown immediately after a supported authoritative outcome is known and attributes the soil consequence to the Joja/agrochemical choice (C1 confirmation/proxy only). Required content elements: action/choice attribution, soil-health consequence direction, and no implication of a mechanical 014/015/vanilla-tier buff.
- **Unknown-baseline legibility (C2):** when prior soil was unmeasured/`NaN`, player-facing feedback MUST remain understandable and MUST NOT imply prior measurement history, a prior known soil value, or an invisible no-op. Copy/feedback should describe the current measured outcome/result without inventing a remembered baseline.
- **Boundary honesty:** when the observed effect is reduced by valid bounds/tier context (e.g., soil already at minimum), feedback/outcome remains honest and non-misleading; it does not claim full nominal penalty impact as if it occurred.
- **Refusal (minimum structure):** refusal feedback states (1) the refusal cause/class in player-understandable terms, (2) that the agrochemical was not applied / no success occurred, and (3) a next-step implication where relevant (e.g., host-only MVP path, invalid target). No false success.
- **Bounded/recoverable player legibility:** player-facing requirements communicate that the consequence is bounded and recoverable through existing soil-care systems (no irreversible dead-end implied), distinct from internal numeric validity rules.
- **Accessibility/legibility (player-facing, text/procedural):** shortcut, cost, attribution, and refusal information is presented as clear text/procedural feedback; critical outcome meaning is not conveyed by color/asset presence alone. Tone guidance does not replace this legibility requirement.
- **Fallback:** if intended player-facing feedback cannot be shown through the normal path, the system MUST still preserve a non-misleading outcome record/log-safe diagnostic path and MUST NOT present false success. Implementation may choose the minimal safe fallback consistent with SMAPI/game constraints; absence of a HUD message is not success.
- **Localization parity (touched 017 keys):** `default.json`, `en.json`, and `pt.json` contain parity for new/touched keys. Parity includes equivalent meaning for shortcut/cost/attribution/refusal and equivalent cozy/non-punitive tone intent; exact wording may differ by locale.
- Tone: informative/cozy/non-punitive ("recompensar, nunca pregar" spirit), no shaming or moralizing. Acceptable copy states facts/consequences and options; unacceptable copy insults, shames, or implies player failure.

## Out of scope
- Full Joja narrative/economy/content expansion.
- Mandatory new PNG/SFX assets.
- Vanilla fertilizer category behavior or Harmony hook integration.
- Data/recipe/shop additions unrelated to this single controlled MVP input.
## Icon strategy (017 convergence) — Decision 2026-11-06

**Decision:** The 017 Joja/agrochemical input ships **WITHOUT a dedicated 017 texture**. The item is registered as an untextured `Type="Basic"` object and explicitly accepts the **vanilla `springobjects` fallback sprite (`SpriteIndex 0`)** for rendering at MVP. No 017-specific PNG is introduced.

**Rationale:**
- **FR-013 forbids required new art.** A mandatory 16×16 icon would contradict the no-mandatory-assets constraint; a disposable one-off asset would add asset-pipeline risk (csproj copy rules, content-pipeline load path, fallback-failure modes) for zero gameplay value at MVP.
- **No player-facing hex claim is made.** The item is not a fertilizer-slot item, not compostable, and not promoted on any visual identity; its telegraphing is entirely textual/procedural (item name/description keys and feedback messages), so the placeholder sprite carries no balance, quality, or trust claim.
- **Registration is honest as-is.** The vanilla fallback is the same pattern already documented inline in `LivingRoots/ModEntry.cs` for untextured `Data/Objects` entries (see the compost registration note describing the `springobjects`/`SpriteIndex 0` fallback); the 017 item deliberately takes that path rather than fork a new texture asset.
- **Pipeline stays minimal.** Shipping without the PNG avoids a second source of truth for item identity and avoids a texture-registration test/asset-pairing obligation the MVP does not need.

**Verification status (honest):** Render legibility of the fallback sprite **in-game is UNVERIFIED at decision time**. 016/017 verification did not screenshot or assert the 017 item's inventory/shop-tooltip presentation. Critical meaning is **text-only** (FR-002/FR-013): before-use framing, attribution, and refusal meaning do not depend on the sprite. **Revisit with playtest** (or a host-only session observation) before promoting any claim about the item's visual presentation; if the fallback reads poorly, the revisit task is to add one 16×16 icon through the existing asset pipeline, not to change the gameplay contract.

**Provenance:** This section is append-only and dated 2026-11-06. It records a decision; it does not modify the registration boundaries above. `LivingRoots/ModEntry.cs` already documents the `springobjects` fallback inline (see the `Data/Objects` edit branch); no code change is implied by this decision.
