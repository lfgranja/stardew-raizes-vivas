# Research: Playable Compost Loop (004)

**Branch**: `004-playable-compost-loop` | **Date**: 2026-10-13
**Status**: All clarifications resolved (sessions 2026-10-03, 2026-10-04, 2026-10-13). No open NEEDS CLARIFICATION.

---

## Decisions

### 1. Item / Machine Registration Route (Settled — not unsettled)
- **Decision**: Native SMAPI `AssetRequested` against `Data/Objects` (Compost), `Data/BigCraftables` (Bin), `Data/CraftingRecipes`, and `Data/Machines` (Triggers + `DaysUntilReady`).
- **Rationale**: T053 spike completed; `ContentPatcher` excluded per `manifest.json` / `ModEntry.cs:297-301`; FR-001a native-machine requirement forces this.
- **Alternatives**: ContentPatcher (rejected — dependency, does not provide native machine interface); bespoke UI (rejected — Principle V / FR-008a).

### 2. Batch Yield (Supersedes earlier multiplier)
- **Decision**: One completed batch = exactly 1 compost item (`FR-005a`). Retirement of 1–5 maturation-level multiplier confirmed by maintainer 2026-10-03.
- **Rationale**: Native machine performs the whole batch; mod never resizes output.
- **Alternatives**: Scaled yield by days active (rejected — earlier spec superseded).

### 3. Processing Duration
- **Decision**: 2 in-game days (`MaturationDays = 2` in `ModConstants`).
- **Rationale**: Harmonizes User Story 2 with FR-001b and existing `Constants.cs`; 2026-10-04 clarification settled.
- **Alternatives**: 1 day (rejected — breaks FR-001b / User Story 2 alignment).

### 4. Bin Removal / Pickup Behavior
- **Decision**: Bin retains contents + progress in `Item.modData` (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`), max stack 1 while carrying progress; processing pauses unplaced; dislodgement via tool/explosion keeps modData (floating debris if inventory full).
- **Rationale**: FR-005c and 2026-10-03 clarification ("game keeps the goods"). Earlier spec's drop-on-ground behavior superseded.
- **Alternatives**: Drop waste/compost on ground, lose progress (rejected — superseded by FR-005c).

### 5. Refusal Delivery Channel
- **Decision**: Game's own on-screen message system — `Game1.showRedMessage(...)` in game font / player's language via SMAPI `ITranslationHelper`; translation keys in `i18n/default.json` (`message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`, `message.harvest.inventory-full`). No bespoke UI component.
- **Rationale**: FR-008a + Principle V (YAGNI); 2026-10-03 clarification.
- **Alternatives**: Custom overlay/drawing (rejected — violates FR-008a "MUST NOT introduce its own UI").

### 6. Invalid Drop-in Signal
- **Decision**: Vanilla machine rejection (audio cancel cue / machine shake) without text overlay.
- **Rationale**: FR-008a scopes HUD text to tile-application / full-inventory / concurrency cases; drop-in refusal follows native machine behavior.

### 7. Concurrent Action Resolution
- **Decision**: Host-authoritative atomic lock per tile / machine instance; first action mutates state; second evaluated against updated state, refused with visible reason, inventory intact, never last-write-wins.
- **Rationale**: Principle VI (non-negotiable); FR-019; 2026-10-03 clarification.
- **Alternatives**: Last-write-wins (rejected — violates Principle VI / FR-019).

### 8. Visual Soil Health Differentiation (0–100)
- **Decision**: Inherits Feature 001's 3-tier continuous RGB interpolation (Poor 1–33, Moderate 34–66, Healthy 67–100), rendered under ambient lighting; 0 = neutral vanilla dirt, no overlay. Reactivity via `ISoilHealthService.OnTileHealthChanged` → `VisualizationService` cache invalidation (≤16.67ms, zero polling).
- **Rationale**: FR-009, FR-010; 2026-10-03/04 clarifications.
- **Alternatives**: Binary on/off overlay (rejected — breaks 3-tier continuous spec).

### 9. Save / Load Fault Isolation
- **Decision**: Tolerant reader with per-entry try-catch; obsolete fields (`MaturationLevel`, `ConsecutiveActiveDays`, `ConsecutiveIdleDays`) ignored; corrupt records quarantined (sanitized log, `FR-015`); orphaned entries (no matching `GameLocation.Objects`) pruned (FR-002 / FR-012).
- **Rationale**: FR-012, FR-014, FR-015.
- **Alternatives**: Fail-fast on any corrupt record (rejected — violates FR-014 / FR-012).

### 10. Multiplayer Verification Boundary (Formal, not implicit)
- **Decision**: Feature 004 verifies concurrency via automated multi-threaded stubs / event simulations in `LivingRoots.Tests` (SC-010 stub-bounded). Live 2-player verification deferred to Roadmap D1; 004 MUST NOT declare Gate 3 multi-complete until D1 runs. No presentation of stubs as live proof.
- **Rationale**: Constitution VI; spec Assumptions (multiplayer verification boundary) and Clarifications; CHESS/CTrigger/Jepsen evidence.
- **Alternatives**: Declare stub-bound complete (rejected — violates Principle VI / spec debt D1).

### 11. Guard Mechanism: `lock` + Dictionary over `Interlocked.CompareExchange` (T041 — accepted deviation)
- **Decision**: The host-authoritative first-wins lock (`HostAtomicLockService.TryAcquireLock`) and the exactly-once-per-day-per-location guard (`CompostingBinService.ProcessDayStart`, mirrored by `SoilDecayService`) are implemented with `lock` plus a per-key `Dictionary`, not the `Interlocked.CompareExchange`-on-a-dedicated-flag mechanism named by FR-001b's fallback text and tasks T011c/T018. This is a documented, accepted deviation of mechanism, not a functional defect.
- **Rationale**: (a) the required BEHAVIOUR is fully satisfied — first-wins mutual exclusion and exactly-once-per-day-per-location are proven by `MultiplayerConcurrencyTests`, `MultiplayerInvariantsTests`, `SingleMultiplayerParityInvariantsTests` and `DailyDecayTests`; (b) Constitution III permits lock-based synchronisation for game-loop coordination and reserves lock-free atomics for where snapshot-diff is insufficient — a Monitor critical section provides the atomicity both guards need; (c) `Interlocked.CompareExchange` on a scalar flag cannot express the per-location dictionary these guards need without an additional container, so the named primitive would not simplify the implementation; (d) the two guards are already independent mechanisms on separate fields (`_lockObject`/`_locks` versus `_dayLock`/`_lastProcessedDay`), which is the separation T011c and T018 actually require.
- **Alternatives**: Rewriting both guards to scalar `Interlocked.CompareExchange` flags (rejected — would add a per-location container anyway, risks regressing 1053 green tests, buys no behavioural change); leaving the deviation undocumented (rejected — the spec and task text name a primitive the code does not use, so silence would leave the codebase dishonest about its own design).
- **Status / debt**: Accepted deviation. NOT a functional defect; does not weaken FR-004 / FR-019 / Principle VI — the behaviour is verified. The remaining spec/task wording naming the primitive is reconciled through the spec-change process (a human gate), tracked as task T041 in `tasks.md`.
- **Verification boundary** (mirroring decision 10): the guards are attested by automated stubs within their bounds; live 2-player verification remains owed to roadmap D1.

---

## Outstanding Dependencies / Integration Patterns
- SMAPI `AssetRequested` event → `ModEntry.OnAssetRequested` (existing)
- `IModDataService` persistence (`composting_bins_{saveId}`, `soil_health_data_{saveId}`)
- `ITranslationHelper` + `i18n/default.json`
- `ThreadSafeGameLoopEventsStub` for concurrent action simulations
- `VisualizationService` reactive subscription to `ISoilHealthService.OnTileHealthChanged`
