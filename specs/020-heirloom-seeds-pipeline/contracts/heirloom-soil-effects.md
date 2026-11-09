# Contract: Heirloom Soil Effects (behavior)

**Feature**: `020-heirloom-seeds-pipeline` | **Consumers**: harvest Harmony postfix/prefix, `HoeDirt.plant` postfix, `ModController` broadcast path, Verification runner

Conventions mirror the 017 phase-order grammar: authority checks → per-tile lock/first-wins → measured read → bounded write (clamp 0–100) → feedback after commit → broadcast after commit; refusals before any mutation/consumption/feedback-of-success.

## HE-1 Plant bonus (`HeirloomPlantBonus`)

- **Trigger**: `HoeDirt.plant(itemId, who, isFertilizer)` returns `true`; `itemId` resolves to `LivingRoots_HeirloomParsnipSeeds` (exact ID or qualified equivalent) AND `!isFertilizer`.
- **Host path** (`Context.IsMainPlayer == true`):
  1. Inside per-tile lock, read measured soil health (`float`, may be `NaN`/missing).
  2. `baseline = IsNaN ? MaxSoilHealth : measured`; `newValue = clamp(baseline + HeirloomPlantBonus (2.0), 0, 100)`.
  3. Persist bounded write (`SetSoilHealth`) — first-wins semantics; on conflict, re-read and re-evaluate; second concurrent requester gets explicit refusal (no silent last-write-wins).
  4. Broadcast `SoilHealthSyncMessage` (host → farmhands).
  5. Feedback: none mandatory (plant already has its own dirtyHit); optional terse copy `message.heirloom.plant-bonus` — not required; absence keeps the slice minimal.
- **Farmhand path**: no local soil write. Client sends `HeirloomPlantBonusRequest { saveId, dayKey, x, y, itemId }` (typed `"HeirloomPlantBonusRequest"`). Host: if valid (same farm, in-bounds, host turn, not already applied `(tile,day)`/request correlation per chosen idempotency guard), apply as host path; then broadcast. If invalid/unreachable: **no mutation**, farmhand shows `message.heirloom.plant-bonus-authority-unsupported` copy. (Substitution of the "explicit refusal only" MVP is permitted if implementation finds the message path infeasible — must be disclosed in validate.md, never silent.)

## HE-2 Harvest bonus (`HeirloomHarvestBonus`)

- **Trigger**: existing `Crop.harvest` Harmony seam, post-decision, heirloom crop identity only (`crop.indexOfHarvest.Value == "LivingRoots_HeirloomParsnip"` or crop seed ID == heirloom seed — final predicate decided in tasks).
- **Healthy-band gate**: apply only if measured soil health `≥ ModerateHealthyBandEdge` (71). Below → no write; 014 path unchanged.
- **Write**: flat `+HeirloomHarvestBonus` (1.0) through the same lock/bounds/NaN→100 semantics; broadcast as HE-1.
- **014 interplay**: this seam MUST NOT alter `IHarvestQualityService.Decide` inputs/outputs; quality/extra-harvest resolve exactly as vanilla crops. `+HeirloomHarvestBonus` is a distinct flat write recorded in soil state only.

## HE-3 Seed save (`HeirloomSavedSeedYield = 1`)

- **Trigger**: any completed heirloom harvest (any soil band), harvesting player non-Junimo.
- **Behavior**: add exactly 1 `LivingRoots_HeirloomParsnipSeeds` to the harvesting player inventory; full → localized refusal copy (`message.harvest.inventory-full` conventions), no seed lost, no silent destruction.
- **Junimo**: exempt (no player inventory).
- **Not probability; not quality-scaled; not farming-level-scaled.**

## HE-4 Inventory-full & feedback conventions (reused, not reinvented)

- Seed/produce grants use the same full-inventory detection as 014 (`HarvestInventoryDelta`/`IPlayerInventory`); refusal copy reuses/extends `message.harvest.inventory-full` pattern with heirloom-specific `seed-saved-inventory-full` key (EN/PT/default).

## HE-5 Bounds & invariants

- `HeirloomPlantBonus`, `HeirloomHarvestBonus` are 0–100 bounded writes; clamping preserves `[0,100]`; NaN baseline = `MaxSoilHealth` (100); measured result recorded; no exception escapes hooks (try/catch → log trace + skip, mirroring existing patch hygiene); no allocation-heavy steady-state work.
- Idempotency: `HeirloomPlantBonusRequest` correlation must prevent double-apply on retry; HB harvest path is inherently once-per-harvest-event.

## HE-6 Honesty / verification contract

- Claims from this contract may be promoted only to `host-only machine-pass` (Gate 3 scenario `heirloom-plant-bonus`, `lr_give_heirloom_seed` grant, honesty R). `human-verified` / `d1-farmhand-convergence` remain `pending`. No component of this contract asserts nutritional/chemical superiority of crioulo over hybrids (not in scope, not agronomically defensible universally).
