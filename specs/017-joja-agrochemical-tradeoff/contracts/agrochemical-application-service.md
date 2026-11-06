# Contract: Agrochemical Application Service (017 MVP)

## Purpose
Defines the behavioral contract for applying the single Joja/agrochemical input to a soil target in MVP. The contract enforces spec 017 clarifications C1–C4, host authority, and no-double-counting boundaries.

## Placement
- **Domain interface:** `LivingRoots/Domain/Interfaces/` (e.g., `IAgrochemicalApplicationService`), consistent with `ICompostApplicationService`/`IMulchApplicationService`.
- **Implementation:** `LivingRoots/Services/`.
- **Composition:** `LivingRoots/ModEntry.cs` only (no `new` services outside composition root).
- **Controller orchestration:** `LivingRoots/Controllers/ModController.cs` performs authority/support checks, per-tile lock, refusal display, and FR-009 broadcast.

## Behavioral contract

### Inputs
- Target `GameLocation` and `Vector2` tile.
- Implicit/player context via existing seams as needed (`IPlayerProvider`/held item validation), following established project patterns.
- Current soil state via `ISoilHealthService`.

### Success (host, eligible, authoritative)
1. Validate target/input eligibility. Invalid targets return refusal (no mutation/consumption/false success).
2. Resolve baseline (C2): `baseline = IsNaN(currentSoil) ? ModConstants.MaxSoilHealth : currentSoil`.
3. Compute penalty (C3): `postPenalty = clamp(baseline - ModConstants.AgrochemicalSoilPenalty, MinSoilHealth, MaxSoilHealth)` where provisional penalty is `10f`.
4. Write **explicit measured** soil value via absolute `ISoilHealthService.SetSoilHealth(locationName, tile, postPenalty)`.
5. Consume held agrochemical input only as part of authoritative success ordering (never on refusal/conflict/unsupported path).
6. Provide immediate confirmation result/message key for controller/UI feedback (C1: confirmation only, no 014/015 mechanical buff).
7. Controller broadcasts `BroadcastSoilHealthSnapshotIfHost()` after successful host commit (FR-009 compatibility).

### Refusal
Return explicit refusal with a stable message key/reason. Refusals MUST:
- not mutate soil state;
- not consume the item;
- not emit false-success feedback;
- be localized via normal translation boundary.

#### Refusal classes (minimum)
| Class | Example cause | Ordering |
|---|---|---|
| Unsupported authority (C4) | Farmhand/unsupported MVP path | **Before** service execution/mutation/consumption/feedback |
| Invalid target | Non-farm/unsupported location, not eligible soil target | Before mutation/consumption |
| Missing/invalid input | Not holding agrochemical item | Before mutation/consumption |
| Conflict | Per-tile lock contention / first-wins conflict | Before service mutation/consumption (controller lock path) |

## Non-negotiable constraints
- **No `UpdateHealth` negative delta from unknown/unmeasured state.** Use absolute measured `SetSoilHealth`.
- **No double-counting (C1):** no changes to `HarvestPatches` fertilizer tier heuristic inputs, no `HoeDirt.fertilizer` writes, no harvest quality/extra-harvest boost logic, no `Crop.phaseDays`/growth modifier logic.
- **No Harmony fertilizer hook** in MVP.
- **No second source of truth.** Host authoritative; farmhand unsupported path refused.
- **Idempotence/safety:** repeated valid applications each apply the same bounded immediate rule (MVP no hidden stacking/cooldown state). Conflicts are explicit first-wins refusals.

## Result shape (guidance)
Implementation may use `bool + out messageKey` (mulch pattern) or an equivalent explicit result type. The shape MUST expose:
- success/failure;
- localized message key (refusal reason or confirmation/attribution key);
- enough information for controller tests to assert no mutation/consumption/broadcast on refusal.

## Test contract
Automated tests MUST cover:
- unknown/`NaN` baseline => explicit measured `SetSoilHealth(90)` for `P=10` (or computed clamp);
- measured baseline => correct bounded post-penalty;
- min-bound clamp;
- refusal before mutation/consumption for unsupported farmhand path;
- no `UpdateHealth(-P)` on penalty path;
- no broadcast on refusal/failure;
- host success lock acquire/release and FR-009 broadcast;
- lock contention explicit refusal;
- i18n key/message contract for new player-facing text.