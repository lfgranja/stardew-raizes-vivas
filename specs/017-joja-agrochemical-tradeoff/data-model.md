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
| `Success` (host, eligible) | Explicit shortcut confirmation feedback (attributed, honest for bounds/C2 unknown baseline); result exposes `baseline_used`, `post_penalty`, and clamp/applied-delta info (`requested_penalty`/`applied_penalty` or equivalent clamped indicator) for honest FR-005/FR-007 messaging/assertions; bounded soil penalty applied as explicit measured value; item consumed by controller **only after** authoritative measured commit; host snapshot sync broadcast by controller after successful commit. | No mechanical buff to 014/015 quality/growth; no vanilla fertilizer-tier interaction; no `HoeDirt.fertilizer` write; no Harmony fertilizer hook; no implication of prior measurement history for C2 unknown baseline. |
| `Refusal` | Explicit localized refusal reason (cause + no-success/non-application + next-step implication where relevant); no misleading success. For unsupported farmhand path: refusal before any soil mutation, item consumption, or false-success feedback. | No unsafe partial soil mutation; no local-only authoritative soil change; no silent no-op presented as success; no item consumption/broadcast/false-success. |

### 6. Verification Evidence (planning reference)

Claim schema is canonical in `contracts/multiplayer-gate3.md`; this section mirrors its vocabulary.

| `evidence_class` | Meaning | `status` notes |
|---|---|---|
| `automated` | Unit/controller/property/invariant tests for defined behavior. | `complete` when passing evidence is recorded; otherwise `pending`/`blocked`. |
| `host-only-machine-pass` | Host-only end-to-end run with required signals/evidence. **Not** `human-verified`, **not** `d1-farmhand-convergence`. | `complete` only for host-only machine evidence; never promoted above class. |
| `human-verified` | Separate human attestation. | `pending` until human attestation is recorded. |
| `d1-farmhand-convergence` | Separate real-farmhand MP convergence evidence. | `pending` until real D1 evidence is recorded. |
| `qualitative-follow-up` | Structured playtest/legibility evidence outside automated/runner path (e.g., SC-004 protocol when run). | `complete` only when that qualitative evidence is recorded; does not substitute for required automated/Gate 3 claim levels unless explicitly stated. |
| `planned-run-not-executed` | A planned verification run was not executed. **Not a pass** and not pass/fail evidence. | `infeasible` or `blocked` with reason in `source_artifact_path`/`scope_note`; lower-level completed evidence may still be claimed separately. |

**Claim record:** `claim + evidence_class + source_artifact_path + status + scope_note`. `evidence_class` ∈ {`automated`, `host-only-machine-pass`, `human-verified`, `d1-farmhand-convergence`, `qualitative-follow-up`, `planned-run-not-executed`}; `status` ∈ {`pending`, `complete`, `blocked`, `infeasible`}. Claims MUST NOT be promoted above their class. Provenance: never rewrite/retroactively promote prior artifacts.

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
10. **Result data for honest bounds (A8):** Success results expose `baseline_used`, `post_penalty`, and clamp/applied-delta information (`requested_penalty`/`applied_penalty` or an equivalent explicit clamped indicator) so a bounds-reduced outcome is not messaged/tested as a full nominal penalty.
11. **One input per success / no hidden state (A12):** Each successful application consumes one agrochemical input; repeated applications require additional inputs and apply the same immediate bounded rule (no hidden cooldown/stacking state).
12. **Soil-at-minimum refusal (017 convergence adjudication, 2026-11-06):** A measured baseline of exactly `0` (`MinSoilHealth`) MUST produce an explicit `RefusedSoilAtMinimum` outcome: no soil write, no item consumption, no FR-009 broadcast, and localized refusal stating cause, no-success/non-application outcome, and next-step implication. A zero-delta `Success` is forbidden for this case. This is the spec-faithful reading of FR-005/FR-008 for the at-minimum case (not a violation); see `contracts/agrochemical-application-service.md` §Adjudication. Distinct from the clamped-to-zero-with-nonzero-baseline outcome (`PostPenalty=0`, `WasClamped=true`, `AppliedPenalty>0`), which remains a bounded Success per rule 10.

## State transitions

```text
ApplicationAttempt
  -> [controller: unsupported authority / ineligible target / missing input / conflict]
       => Refusal (explicit, before mutation/consumption/false-success)
  -> [controller: host + eligible + lock acquired + authoritative preconditions pass]
       => Service/domain: ResolveBaseline(priorMeasured | unmeasured=>100)
       => Service/domain: Compute postPenalty = clamp(baseline - P, 0, 100), P=10 provisional
       => Service/domain: WriteMeasuredSoil(postPenalty)   // absolute measured write
       => Controller (post-commit only): ConsumeInput
       => Controller (post-commit only): EmitConfirmationFeedback (attributed, honest)
       => Controller (post-commit only): BroadcastSoilHealthSnapshotIfHost (FR-009)
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