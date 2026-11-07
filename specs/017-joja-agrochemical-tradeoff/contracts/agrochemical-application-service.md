# Contract: Agrochemical Application Service (017 MVP)

## Purpose
Defines the behavioral contract for applying the single Joja/agrochemical input to a soil target in MVP. The contract enforces spec 017 clarifications C1–C4, host authority, and no-double-counting boundaries.

**Canonical/maintenance note (A11):** C1–C4 are canonical in `spec.md` Clarifications; ordering/authority behavior here is coordinated with `contracts/multiplayer-gate3.md`; claim schema is canonical in `contracts/multiplayer-gate3.md`. Duplication is intentional — avoid label drift.

## Placement
- **Domain interface:** `LivingRoots/Domain/Interfaces/` (e.g., `IAgrochemicalApplicationService`), consistent with `ICompostApplicationService`/`IMulchApplicationService`.
- **Implementation:** `LivingRoots/Services/`.
- **Composition:** `LivingRoots/ModEntry.cs` only (no `new` services outside composition root).
- **Controller orchestration (authoritative orchestration owner):** `LivingRoots/Controllers/ModController.cs` performs authority/support checks (C4), per-tile lock acquire/release, player-item presence checks where required by interception, refusal/confirmation display, held-item consumption, and FR-009 broadcast. The controller is the owner of the MVP authoritative success/failure side-effect ordering that touches SMAPI/game/UI boundaries.

## Behavioral contract

### Inputs
- Target `GameLocation` and `Vector2` tile.
- Implicit/player context via existing seams as needed (`IPlayerProvider`/held item validation), following established project patterns.
- Current soil state via `ISoilHealthService`.

### Success (host, eligible, authoritative)
Ordering is requirements-level (not a timing API guarantee) and MUST preserve C4/FR-008 invariants:

1. **Controller preconditions (before domain mutation):** confirm supported authority/path (host-only MVP; farmhand unsupported → refusal), acquire per-tile lock / resolve first-wins contention, and validate the held agrochemical input/eligible target as required. Any refusal at this stage occurs **before** soil mutation, item consumption, or false-success feedback.
2. **Service/domain evaluation (under lock, authoritative context only):** resolve baseline (C2) `baseline = IsNaN(currentSoil) ? ModConstants.MaxSoilHealth : currentSoil`; compute penalty (C3) `postPenalty = clamp(baseline - ModConstants.AgrochemicalSoilPenalty, MinSoilHealth, MaxSoilHealth)` where provisional penalty is `10f`.
3. **Service/domain commit:** write **explicit measured** soil value via absolute `ISoilHealthService.SetSoilHealth(locationName, tile, postPenalty)`. The service MUST NOT rely on `UpdateHealth` negative delta from unknown/unmeasured state.
4. **Controller post-commit side effects (only after authoritative measured commit):** consume the held agrochemical input, emit immediate confirmation/attribution feedback (C1: confirmation/proxy only, no 014/015 mechanical buff), then broadcast `BroadcastSoilHealthSnapshotIfHost()` (FR-009 compatibility). Consumption/feedback/broadcast MUST NOT occur on refusal/conflict/unsupported path.

### Refusal
Return explicit refusal with a stable message key/reason. Refusals MUST:
- not mutate soil state;
- not consume the item;
- not emit false-success feedback;
- be localized via normal translation boundary.
- be attributable to a refusal class with ordering preserved (C4 unsupported path/farmhand refusals occur before domain mutation/consumption/feedback).

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

## Result shape (required minimums)
Implementation may use a service result + controller orchestration (preferred for MVP clarity) or an equivalent explicit result type. The shape MUST expose, or make available for controller/message-selection/assertion logic:
- success/failure/refusal class (or equivalent explicit state);
- localized message key (refusal reason or confirmation/attribution key);
- **`baseline_used`** — the explicit pre-penalty baseline used for computation (C2: `MaxSoilHealth` for unmeasured/`NaN`; measured value otherwise);
- **`post_penalty`** — the explicit bounded post-penalty measured value computed/committed for a success path;
- clamp/applied-delta information sufficient for honest FR-005/FR-007 messaging and assertions: at minimum **`requested_penalty`** and **`applied_penalty`** (or an equivalent explicit **`was_clamped`** indicator plus the applied delta), so bounds-reduced outcomes are not presented as a full nominal penalty;
- enough information for controller tests to assert: no mutation/consumption/broadcast/false-success on refusal; consumption and FR-009 broadcast occur only after authoritative measured commit on success.

The service/domain result MUST NOT be interpreted as having consumed the held item or emitted UI/broadcast side effects by itself; those remain controller-owned MVP side effects. Values may be carried as fields/properties or equivalent accessors; names above describe required data, not a fixed C# API spelling.

## Test contract
Automated tests MUST cover:
- unknown/`NaN` baseline => explicit measured `SetSoilHealth(90)` for `P=10` (or computed clamp);
- measured baseline => correct bounded post-penalty;
- min-bound clamp;
- result/feedback path exposes `baseline_used`, `post_penalty`, and clamp/applied-delta info (`requested_penalty`/`applied_penalty` or equivalent) so FR-005/FR-007 honest bounded outcomes are testable (no false full-penalty claim when clamped);
- refusal before mutation/consumption for unsupported farmhand path;
- no `UpdateHealth(-P)` on penalty path;
- no broadcast, no item consumption, and no success feedback on refusal/failure;
- host success lock acquire/release, item consumption only after authoritative measured commit, and FR-009 broadcast after commit;
- lock contention explicit refusal;
- i18n key/message contract for new player-facing text.

## Adjudication: soil-at-minimum refusal (017 convergence) — Decision 2026-11-06

**Decision:** A measured soil health baseline of exactly `0` (`MinSoilHealth`) MUST produce an explicit `RefusedSoilAtMinimum` outcome. No soil write occurs, no item consumption occurs, no FR-009 broadcast occurs, and the player-facing refusal states the cause (soil already at minimum; the agrochemical would have no effect), that nothing was applied, and the no-success/non-application outcome with a next-step implication. A zero-delta `Success` is forbidden for this case.

**Rationale:**
- **Spec edge-case honesty.** The spec's edge case for bounds-reduced outcomes requires an honest, non-misleading outcome when clamping reduces or eliminates the observed effect; a `Success` with `applied_penalty=0` would present "the agrochemical was applied" feedback for a no-op, which is exactly the misleading-success pattern FR-008 forbids on the refusal path.
- **FR-005 bounds.** FR-005 permits bounds to reduce the observed effect; it does not require a success label when the reduction is total. FR-008 requires explicit refusal when the action cannot be completed under MVP rules. At the hard floor, the action cannot produce any effect, so refusal is the spec-faithful reading.
- **Avoids a misleading "success" for a no-op.** Consuming the item, emitting confirmation feedback, and broadcasting a snapshot for a zero-delta write would spend the player's input and sync state while delivering nothing — a worse outcome than an honest refusal that states the cause.

**Classification:** This refusal is the **spec-faithful reading of FR-005/FR-008 for the at-minimum case — NOT a violation, NOT a divergence to be "fixed" toward zero-delta success.** It is effective with this adjudication (2026-11-06). The `soil-at-minimum` localized refusal keys (`message.agrochemical.soil-at-minimum`) and the pinned tests (T013/T018) already encode this behavior; no code change is implied by this record.

**Interaction with other records:** The refusal does not mutate, does not consume, and does not broadcast; FR-009 shared-state synchronization is unaffected. The clamped-to-zero-with-nonzero-baseline case (e.g. baseline 5, penalty 10 → `PostPenalty=0`, `WasClamped=true`, `AppliedPenalty=5`) remains a **bounded Success** per FR-005's explicit clamp reporting, distinct from the soil-at-minimum refusal (baseline exactly 0). The sparse-cache persistence caveat for measured zeros is recorded separately in `verification/measured-zero-persistence.md` (2026-11-06).

**Provenance:** Append-only adjudication record. Prior sections of this contract are unchanged.

## Corrective amendment (2026-11-06, Phase 10 hygiene) — measured-write seam naming

**Append-only correction; prior sections unchanged.** The measured soil commit for the 017
path goes through **`ISoilHealthService.TrySetSoilHealth`** (the equivalent measured write
seam): a boolean-commit that mirrors the measured-write path (clamp, sparse cache, location/tile
capacity guards) and reports `false` instead of silently dropping the write. A `false` result
MUST surface as a non-success outcome (no item consumption, no confirmation feedback, no FR-009
broadcast). `ISoilHealthService.SetSoilHealth` remains the **legacy void seam** — it shares the
same measured-write internals but cannot report rejection, so it is not the commit seam for the
017 path. Any earlier wording in this document that names bare `SetSoilHealth` as the commit
mechanism refers to the same measured-write semantics now exposed as `TrySetSoilHealth`; the
"equivalent measured write" phrasing above and in `multiplayer-gate3.md` is the accurate name.

**Adjudicated refusal classes (append-only table amendment; the table above is unchanged):**

| Class | Cause | Ordering |
|---|---|---|
| `RefusedSoilAtMinimum` | Measured baseline of exactly `MinSoilHealth` (0); the penalty would have zero effect | Before commit; no soil write, no item consumption, no false-success feedback, no FR-009 broadcast. Player-facing copy states the cause (soil already at minimum), the non-application outcome, and a next-step implication. This class was adjudicated 2026-11-06 (see "Adjudication: soil-at-minimum refusal" section above) and replaces any implied zero-delta `Success` reading for the at-minimum case. |

Prior sections of this contract are unchanged.
