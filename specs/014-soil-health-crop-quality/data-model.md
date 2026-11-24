# Phase 1 Design: Data Model — Soil Health → Crop Quality

**Feature**: `014-soil-health-crop-quality` | **Branch**: `014-soil-health-crop-quality`

---

## Entities

### Soil Health (`SoilHealth` / `ISoilHealthService`) — existing, unchanged

| Field | Type | Constraint | Source |
|-------|------|-----------|--------|
| `LocationName` | `string` | Non-empty; `FormC` normalized | `SoilHealthState` |
| `TileKey` | `(int x, int y)` | Within valid tile bounds; unique per location | `SoilHealthState` dictionary key |
| `HealthValue` | `float` (0–100) | Clamped 0–100; `NaN`/`±Inf` guarded → `Unknown` sentinel for missing records | `SoilHealthService.GetSoilHealth` (NaN sentinel, T135/FR-014) |
| `HealthCategory` | `enum` | Derived: `Poor` `[0,31)`, `Moderate` `[31,71)`, `Healthy` `[71,100]`, `Unknown` (NaN/Inf) | `HealthCategory.FromHealthValue`; migration 34/67→31/71 (FR-013) |

### Health Category (`HealthCategory`) — migration 34/67 → 31/71

| Category | Range | Growth (spec 005) | Harvest Quality (this spec) | Extra Harvest (this spec) | Feedback |
|----------|-------|-------------------|------------------------------|---------------------------|----------|
| `Poor` | `[0, 31)` | 1.15× slower | Force `Quality = 0` (only tier with override) | None (base chance) | `marker-poor` + "Soil too poor for quality crops" + `cancel` |
| `Moderate` | `[31, 71)` | 1.00× (vanilla parity) | Vanilla roll (no modification) | None | None (silence = vanilla) |
| `Healthy` | `[71, 100]` | 0.85× faster | Additive `+0.08` on vanilla quality roll `p` | Base + `0.10` | `sparkle-living` + "Living Soil" (if enhanced) and/or "Extra harvest!" (if extra), two sprites when both |
| `Unknown` | NaN/Inf only | Skip → vanilla (no change) | No modification (vanilla roll, no force, no boost, no feedback) | None | None |

### Harvest Outcome (new, transient) — not persisted

| Field | Type | Constraint | Notes |
|-------|------|-----------|-------|
| `PrimaryQuality` | `int` (0–3) | none/silver/gold/iridium | Rolled once for the primary item; Poor forces 0; Moderate vanilla; Healthy `p + 0.08`. Extra items are **normal quality (0)** (vanilla behavior) — not boosted. |
| `ExtraHarvestCount` | `int` (≥0) | Host-authoritative roll | `Data/Crops.ExtraHarvestChance` + 0.10 for Healthy; extras spawned as debris when inventory full |
| `Tier` | `HealthCategory` | Derived from soil health at harvest | NaN/Inf → Unknown → no modification |
| `FeedbackShown` | `bool` + messages | Event-driven; visual/local only | "Living Soil" only when enhanced; "Extra harvest!" when extra occurs; both as two sprites when both hold; Poor always (forced 0); Moderate never |

### Tier Feedback (new, transient) — not persisted

| Field | Type | Constraint | Notes |
|-------|------|-----------|-------|
| `Sprite` | `TemporaryAnimatedSprite` (16×16 procedural) | `marker-poor` / `sparkle-living` | Created via `new Texture2D(device, 16, 16)` + `SetData`; singleton pattern per `VisualizationTextures` |
| `Text` | `string` (literal) | "Soil too poor for quality crops" / "Living Soil" / "Extra harvest!" | Literal (no new i18n keys); refusal messages keep existing i18n keys |
| `SFX` | `string` | `cancel` (Poor) / reward SFX (Living) | |
| `Duration` | ~1s | `alphaFade` 0.02; upward drift | Immune to 1000-tile degradation (event-driven on `Display.Rendered`) |
| `Location` | `GameLocation` | Broadcast via `Game1.multiplayer.broadcastSprites` when multiple | MP-safe; visual/local only (does not sync game state) |

### CollectCompost outcome (prerequisite fix, Q4)

| Field | Type | Constraint | Notes |
|-------|------|-----------|-------|
| `OutputGranted` | `bool` (idempotency flag) | New | Two-phase commit: dry-run capacity check + compensating rollback |
| `BinState` | `Empty/Processing/Ready` | Stays `Ready` on inventory refusal | `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` pinned |

---

## Validation Rules

- Health clamped 0–100 (`ModConstants.MinSoilHealth`/`MaxSoilHealth`); `NaN`/`±Inf` → `Unknown` → no modification.
- Finite out-of-range → boundary category (below 0 → Poor → force 0; above 100 → Healthy → +0.08).
- Measured `0` (sparse-cache floor) → Poor → force `Quality = 0`.
- Poor override is the **only** tier that touches `Quality`; Medium/Living/Unknown never touch `Quality` (vanilla by omission).
- Extra-harvest items are normal quality (0); only primary item receives boosted roll.
- Inventory full at harvest: main item vanilla-blocked (red message, crop stays); extra item dropped as debris (vanilla); no duplication, no retryable state.
- Host-authoritative rolls (`Context.IsMainPlayer`); client never rolls; client re-syncs on next `DayStarted`.
- No `Data/Crops`/`Data/Objects` edits (FR-004); all effects runtime.

## State Transitions

- **Harvest quality**: re-rolled on each harvest; soil tier applies independently per harvest (regrowing crops).
- **Extra harvest**: per-harvest roll; independent of quality roll outcome.
- **Feedback**: transient; no persistent state; no per-tile marker.
- **Band migration**: 34/67 → 31/71 touches `HealthCategory`, `ColorInterpolationService`, `ModController` growth modifier, tests, Gate 3 thresholds, docs.
- **CollectCompost**: `Ready` → (inventory full) stays `Ready` (retryable); (success) → `Empty` + 1 compost granted; idempotency flag prevents duplicate grant on retry.
