# Data Model: 017 Joja / Agrochemical Tradeoff (MVP)

**Phase 1 output** | **Date**: 2026-11-06 | **Spec**: `spec.md`

This model is behavioral/domain-oriented (not a database schema). It describes entities, validation, and state transitions the implementation must preserve.

## Entities

### 1. Agrochemical Input (logical)
The single Joja-framed shortcut input/rule for MVP.

| Aspect | Definition |
|---|---|
| Identity | Stable mod-owned item identity (qualified/unqualified forms as appropriate for SDV 1.6). Exact ID string finalized in planning; must not collide with existing IDs. |
| Player-facing framing | Before-use name/description clearly communicates industrial shortcut + soil-health cost (cozy, non-punitive). Localized for touched languages (`default`/`en`/`pt`). |
| Acquisition | Joja-aligned framing in MVP. Exact catalog/shop mechanics may be finalized later; no full Joja economy/content expansion required. |
| Constraints | Mod-intercepted application only; **not** vanilla fertilizer-slot interop; **not** compostable/regenerative input by accident; no mandatory new art/audio assets. |

### 2. Soil Target
A farm soil location eligible for agrochemical evaluation.

| Aspect | Definition |
|---|---|
| Identity | `(locationName, tile)` consistent with `ISoilHealthService` keys. |
| Eligibility | Must satisfy normal soil-target validity rules (valid farm/location context and tillable/soil target as defined by implementation; ineligible targets refused). |
| Prior state | May be **measured** (bounded 0–100) or **unmeasured** (`NaN`/no record). |

### 3. Soil Health Record
Existing Living Roots soil state for a tile.

| Field/state | Meaning |
|---|---|
| Missing / `NaN` | Unmeasured/unknown, not 0%. Sparse storage may omit entries. |
| Measured value | Explicit bounded value in `[MinSoilHealth, MaxSoilHealth]` (`[0,100]`). Explicit `0` is a measured Poor reading (not Unknown). |
| Categories/tier edges | Existing edges remain authoritative: Poor `<31`, Moderate `<71`, Healthy `>=71` for downstream 014/015 behavior. 017 does not redefine tiers. |

### 4. Application Attempt
One player-initiated mod-intercepted use of the Agrochemical Input on a Soil Target.

| Attribute | Definition |
|---|---|
| Actor | Host or farmhand in shared-farm context; MVP supports authoritative host path only. |
| Input present | Player is holding/selecting the Agrochemical Input as defined by controller interception. |
| Authority | Host-authoritative for shared soil mutation. |
| Outcome | `Success` or `Refusal` (never ambiguous). |

### 5. Application Outcome
Player-visible and system-recorded result.

| Outcome | Required effects | Forbidden effects |
|---|---|---|
| `Success` (host, eligible) | Explicit shortcut confirmation feedback; bounded soil penalty applied as explicit measured value; item consumed according to authoritative success ordering; host snapshot sync broadcast after successful commit. | No mechanical buff to 014/015 quality/growth; no vanilla fertilizer-tier interaction; no `HoeDirt.fertilizer` write; no Harmony fertilizer hook. |
| `Refusal` | Explicit localized refusal reason; no misleading success. For unsupported farmhand path: refusal before any soil mutation, item consumption, or false-success feedback. | No unsafe partial soil mutation; no local-only authoritative soil change; no silent no-op presented as success. |

### 6. Verification Evidence (planning reference)
| Level | Meaning |
|---|---|
| Automated | Unit/controller/property-style tests for logic/invariants. |
| Host-only machine evidence | End-to-end run in host-only context; **not** human `verified`, **not** D1. |
| Human `verified` | Separate human attestation. |
| D1 farmhand convergence | Separate real-farmhand MP attestation; follow-up. |

## Validation rules

1. **Bounds:** All soil writes remain within `[0,100]`.
2. **C2 baseline:** If prior soil is unmeasured/`NaN`, baseline is `MaxSoilHealth` (100). `postPenalty = clamp(baseline - penalty, MinSoilHealth, MaxSoilHealth)`.
3. **Absolute measured write:** Penalty path uses absolute `SetSoilHealth` (or equivalent measured write). **Never** `UpdateHealth(..., -penalty)` from unknown/unmeasured state.
4. **No invisible no-op:** A successful eligible application always records an explicit measured post-penalty result and produces feedback.
5. **No double-counting (C1):** No change to harvest quality outcomes, extra-harvest boosts, growth `phaseDays`/multipliers, or vanilla fertilizer-tier heuristics as an MVP gain. Soil penalty may affect later outcomes only through normal existing soil rules.
6. **Authority (C4):** Farmhand/unsupported attempts are refused before mutation/consumption/false-success. Host commits are authoritative.
7. **Conflict safety:** Per-tile contention resolves first-wins with explicit refusal; no silent last-write-wins corruption.
8. **Persistence:** Updated soil state remains compatible with existing save/load and sparse storage semantics.
9. **Localization:** New/touched player-facing text keys are present for supported languages touched (`default`/`en`/`pt` parity expected).

## State transitions

```text
ApplicationAttempt
  -> [unsupported authority / ineligible target / missing input / conflict]
       => Refusal (explicit, before mutation/consumption/false-success)
  -> [host + eligible + lock acquired]
       => ResolveBaseline(priorMeasured | unmeasured=>100)
       => Compute postPenalty = clamp(baseline - P, 0, 100), P=10 provisional
       => WriteMeasuredSoil(postPenalty)   // absolute measured write
       => ConsumeInput (authoritative success ordering)
       => EmitConfirmationFeedback
       => BroadcastSoilHealthSnapshotIfHost (FR-009)
       => Success
```

## Tunables (planning constants)

| Name (proposed) | Provisional value | Notes |
|---|---:|---|
| Agrochemical soil penalty | `10f` | C3:A, tunable, 0–100, playtest/tier-boundary review. |
| Agrochemical item IDs/keys | TBD | Stable mod-owned IDs; qualified/unqualified forms; no collision. |
| Translation keys | TBD | Before-use description, refusal reasons, success confirmation/attribution. |

## Out-of-model (MVP)
- Vanilla `HoeDirt.fertilizer` slot state.
- Harmony fertilizer hook state/probe-commit detection.
- Farmhand intent/ack message protocol (D1 follow-up).
- Monoculture/history state.
- Final balance curves or delayed/stacking penalty state.