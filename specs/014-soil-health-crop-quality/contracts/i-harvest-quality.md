# Internal Contract: Harvest Quality Interception (spec 014)

**Feature**: `014-soil-health-crop-quality` | **Type**: Internal (no public API)

---

## 1. Hook Strategy (Q2)

- **Preferred**: public-field manipulation (proven for growth via `Crop.newDay`/`dayOfCurrentPhase`/`phaseDays`).
- **Fallback**: Harmony only if public fields are insufficient for harvest-quality interception.
- **Verification gate**: member names (`Crop.harvest`, `Crop.dayUpdate`, `HoeDirt.dayUpdate`, `Object.Quality`) must be verified against the 1.6.3/1.6.14 binary via a `GamePath` build before writing hooks. No hook code is written until this gate passes.

## 2. Host Authority (FR-009)

- Quality roll and extra-harvest roll execute **once on the host** (`Context.IsMainPlayer`/`IsMasterGame`).
- Result persists via the Stardew save (host's save = the save); client never rolls.
- Client re-syncs on next `DayStarted`.
- Divergence rule: host state wins.

## 3. Tier Rules (FR-001, FR-002, FR-003)

- `Poor [0,31)`: force `Object.Quality = 0` after the vanilla roll (only tier with override); no extra-harvest boost.
- `Moderate [31,71)`: no modification — vanilla roll byte-identical; no extra-harvest boost; no feedback.
- `Healthy [71,100]`: additive `+0.08` on vanilla quality probability roll `p` for the **primary** item only; additive `+0.10` on `Data/Crops.ExtraHarvestChance`.
- `Unknown` (NaN/Inf): no modification (vanilla).
- Compose with (not replace) vanilla modifiers: Farming level, fertilizer (`quality_fertilizer_item` → num2 1/2/3), `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`.
- Medium tier must be byte-identical to vanilla (SC-003).

## 4. Extra-Harvest Items (research-c2)

- Extra items are **normal quality (0)** (vanilla behavior); do not inherit the primary item's quality; no per-extra re-roll.
- Only the primary item receives the boosted quality roll.
- Extra-harvest count is host-authoritative.

## 5. Inventory Full at Harvest (research-c1)

- Main harvested item: vanilla behavior (blocked with red message if inventory full; crop stays in ground).
- Extra-harvest items: dropped as ground debris via `Game1.createItemDebris` (vanilla); never silently destroyed; never duplicated; no retryable state.
- Reuse existing i18n key `message.harvest.inventory-full` for the red message (if shown).

## 6. Transient Feedback (FR-006, FR-007, FR-008)

- Event-driven: `TemporaryAnimatedSprite` on `TemporarySprites` + floating text on `Display.Rendered`; immune to 1000-tile degradation and 16.67 ms/frame.
- Non-chromatic: sprite shape + floating text + SFX (color alone must not convey essential info; GAG 1.4.1 / XAG 103).
- No persistent per-tile marker (FR-011).
- **Conditionality**: feedback shows only when the tier changes the observable outcome:
  - Poor → always (forced 0).
  - Living → "Living Soil" only when enhanced quality (silver/gold/iridium); "Extra harvest!" when extra item produced.
  - Living harvest yielding normal quality with no extra → no feedback (silence = vanilla).
  - Moderate → no feedback (silence = vanilla).
- **Co-occurrence**: when both enhanced quality and extra harvest occur, show both "Living Soil" and "Extra harvest!" as two separate sprites broadcast together via `Game1.multiplayer.broadcastSprites(location, sprite1, sprite2)`.
- **Visibility (hybrid)**: broadcast transient feedback when it represents a shared farm-state outcome (harvest quality boost, extra harvest, compost "+15", machine ready, lock collision) via `broadcastSprites` + `location.playSound`; keep local-only when it is the triggering player's own input (hoe flash/text) via `SpriteBatch`; state-derived rendering (overlay/tooltip) is not a sprite. Single-player is the degenerate case. Host is the only caller of `broadcastSprites` (host-scoped guard).
- Text is literal (no new i18n keys); refusal messages keep existing i18n keys.
- Duration ~1s; `alphaFade` 0.02; upward drift (reuse compost/mulch pattern).

## 7. Pest Integration (optional, future)

- Quality hook reads pest state through optional `IPestStateProvider` resolved in `ModEntry` composition.
- When pest system not installed, provider is null → no clamp (no hard dependency).
- Interface contract finalized in the pest spec when it ships.
- `PestQualityPenalty` constant (future) clamps quality at harvest (one-directional: pest reads soil; quality reads pest).

## 8. Band Scale Migration (FR-013)

- `PoorModerateBandEdge = 31f`, `ModerateHealthyBandEdge = 71f`, `ModerateBandWidth = 40f`.
- Update `HealthCategory`, `ColorInterpolationService`, `ModController` growth modifier, tests, Gate 3 thresholds, docs.
- No 34/67 references remain (except migration history).

## 9. Constraints

- No `Data/Crops`/`Data/Objects` edits (FR-004) — all effects runtime.
- No new PNG assets (FR-005) — procedural `Texture2D` + `SetData`.
- No new i18n keys for feedback text (spec 005 A4).
- CollectCompost fix (Q4) is a prerequisite task (two-phase commit, dry-run capacity, rollback, `OutputGranted` idempotency).
- TDD Red-Green-Refactor (FR-014); one test file per subject.
- Host-authoritative (FR-009); client never rolls.
