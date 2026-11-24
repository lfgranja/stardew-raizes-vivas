# Quickstart — Playable Compost Loop Validation (004)

**Branch**: `004-playable-compost-loop` | **Purpose**: Prove the feature works end-to-end from a fresh save through the full loop.

> References: [`data-model.md`](./data-model.md) (entity rules / state transitions), [`contracts/contracts.md`](./contracts/contracts.md) (game asset contracts, domain interfaces, localization keys).

---

## Prerequisites

- Stardew Valley 1.6 + SMAPI installed (`STARDEW_GAME_PATH` set for CI; local dev via `F5` / `ModEntry.cs`)
- Build: `dotnet build Stardew-LivingRoots.sln --configuration Release`
- Tests pass: `dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal`
- Format clean: `dotnet format Stardew-LivingRoots.sln --verify-no-changes`
- Mod active: `LivingRoots/ModEntry.cs` composition root wired; `AssetRequested` handler registered; `i18n/default.json` keys present.

---

## Validation Scenarios (In Order)

### A. Craft & Place the Bin (User Story 1 — P1 / SC-001 start)

**Setup**: Fresh save with mod active.

**Run**:
1. Open crafting menu (`E` / crafting tab).
2. Select `LivingRoots.CompostingBinRecipe` (should appear; item exists because `Data/CraftingRecipes` registered).
3. Craft → inventory contains bin (`Data/BigCraftables`).

**Expected**:
- Recipe listed; craft completes; bin appears in inventory.
- Place on farm / Greenhouse valid tile → appears in world; can pick back up.
- Place on water / building → refused; no ghost object (FR-002).

**Verify reference**: [`contracts/contracts.md`](./contracts/contracts.md) §1.2 / §1.4; [`data-model.md`](./data-model.md) Composting Bin placement rules.

---

### B. Load, Process, Collect (User Story 2 — P1 / SC-002)

**Setup**: Placed empty bin on farm.

**Run**:
1. Add qualifying organic waste (inherited qualifying set per `FR-003` / `data-model.md`) to bin.
2. Confirm state moves `Empty → Processing`; `DaysRemaining = 2`; `InputId` serialized.
3. Advance 1 in-game day (`T` / sleep / next day) → `DaysRemaining = 1`; processing advances exactly once regardless of player count (`FR-004`).
4. Advance second day → `Processing → Ready`.
5. Collect → inventory gains exactly 1 `LivingRoots.Compost`; bin resets `Empty`.

**Expected**:
- Batch yield = `1`; no multiplier; no partial loss (FR-005a / FR-005b).
- Full inventory on collect → bin stays `Ready`; batch not destroyed; retry delivers full batch later (FR-005b / `message.harvest.inventory-full`).

**Verify reference**: [`data-model.md`](./data-model.md) Bin state transitions / Batch rules; [`contracts/contracts.md`](./contracts/contracts.md) §1.3 (`DaysUntilReady = 2`).

---

### C. Bin Removal / Progress Preservation (FR-005c / Edge Case)

**Run**:
1. Load bin with active input (`Processing`) or `Ready`.
2. Pick up with tool or bomb / dislodge with explosion.

**Expected**:
- Bin in inventory carries `Item.modData`; `max stack = 1` while progress present.
- Processing paused unplaced (`ProgressPaused = true`).
- If inventory full → drops as floating world debris retaining `modData`.
- Place back → progress restored; cannot shortcut.

**Verify reference**: [`data-model.md`](./data-model.md) Pickup / removal rules; [`contracts/contracts.md`](./contracts/contracts.md) §2.2 (`GetInventoryProgressItems`).

---

### D. Apply Compost to Soil (User Story 3 — P2 / SC-003 / FR-007)

**Setup**: Tilled `HoeDirt` tile (with or without active crop); player has `LivingRoots.Compost`.

**Run**:
1. Select compost; apply to valid tile.

**Expected**:
- `HealthValue = min(100, Health + 15)`; exactly `1` compost consumed from inventory; crop remains (side-dressing permitted per clarification 2026-10-04).
- Tile visually distinguishable (3-tier RGB: Poor 1–33 / Moderate 34–66 / Healthy 67–100; `0` = neutral vanilla dirt, no overlay).
- Overlay updates ≤16.67ms (next frame) without reload (`FR-010`).

---

### E. Refusal Scenarios (FR-008 / FR-008a)

**Run** (each independently):
- Apply to untilled / non-farm / non-greenhouse / building / indoor tile.
- Apply when `Health == 100`.
- Concurrent action by second player; second evaluated after first mutation.

**Expected** (each):
- No compost consumed.
- `Game1.showRedMessage` with correct translation key (`message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`).
- No custom UI / overlay / text-drawing added (FR-008a).
- Invalid drop-in on bin → vanilla machine shake / cancel audio, no text (FR-008a clarification 2026-10-13).

---

### F. Save / Reload Round-Trip (FR-012 / SC-004 / FR-015)

**Run**:
1. Set bin to `Processing` with partial progress; set soil health to non-zero / non-100; save (`S`).
2. Reload.

**Expected**:
- All bin states, `DaysRemaining`, `InputId`, soil health values preserved exactly.
- Corrupt / obsolete / disconnected entries quarantined (skipped, sanitized log, no crash; `FR-012` / `FR-015`).
- Visual overlays invalidated and re-rendered from reloaded cache (`FR-010`).
- Save file does not store `0` health (sparse convention).

---

### G. Multiplayer / Concurrency Stub (FR-019 / SC-010 / Principle VI)

**Run**:
- Execute `MultiplayerConcurrencyTests.cs` / `ThreadSafeGameLoopEventsStub` (automated event simulations in `LivingRoots.Tests`).

**Expected**:
- Concurrent actions = exactly one applied; second refused with explanation; inventory unchanged.
- Host-authoritative atomic lock verified in stub; not presented as live 2-player proof (debt D1; spec Assumptions — multiplayer verification boundary / Constitution §VI).
- `ISoilHealthService.OnTileHealthChanged` triggers immediate cache invalidation (≤16.67ms) in simulated farmhand listener.

---

### H. Documentation Accuracy (FR-016 / FR-017 / SC-006 / SC-007)

**Run**:
- Read `README.md` (under `## Features`) and confirm every capability claim either works or is labelled `(Planned)`.
- Check 8 stale spec/docs for multiplayer scope contradictions against Constitution §VI (`FR-017`).

**Expected**:
- Zero false / unlabelled feature claims (SC-006).
- Zero spec/document declares multiplayer out of scope in contradiction of Constitution (SC-007 / FR-017).
- No redesign of settled requirements in those docs — scope wording correction only.

---

## Pass / Fail Criteria (Per Gate)

| Gate | Check | How |
|---|---|---|
| 1 — Static / Tests | Build + all tests green; format clean | `dotnet build`; `dotnet test`; `dotnet format --verify-no-changes` |
| 2 — Product Quality | Loop playable; health 0–100; save/load preserves; no regressions | Scenarios A–F above |
| 3 — End-to-End | Real-player journey passes (till → modify → save → reload → verify) | Scenarios A, B, D, F performed manually; concurrent stub G verifies mechanism (not live network proof) |

**Note on Gate 3 multi-client**: Multi-player live verification (D1) is a required debt item linked to Roadmap D1. Feature 004 verifies concurrency logic via automated stubs and must NOT be declared complete for multi Gate 3 until D1 runs (Constitution VI; spec Assumptions — multiplayer verification boundary).
