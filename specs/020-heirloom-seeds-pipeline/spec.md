# Feature Specification: Heirloom Seeds / Sementes Crioulas Pipeline (Crioulo Parsnip — one-crop template slice)

**Feature Branch**: `020-heirloom-seeds-pipeline`

**Created**: 2026-11-09

**Status**: Draft

**Input**: User description: "Specify and implement the next 80/20 gameplay slice (2026-11-09 refresh, ROADMAP/OBJETIVO item 6): the heirloom/landrace (\"sementes crioulas\") seed→crop→produce pipeline as a reusable template, proven end-to-end by exactly one crop — a Crioulo Parsnip (Spring, slower than vanilla) — distributed in-game **without** requiring the Elena NPC (which is unimplemented and out of scope), with soil-health coupling consistent with the existing compost/mulch/growth/harvest seams, host-authoritative multiplayer semantics (Constitution Principle VI), honest framing (abstractions labeled; nitrogen only via legumes; Pierre hybrids remain a valid, non-blocked choice), seed saving as an explicit, capped, communicated mechanic (not silent RNG), EN/PT localization, full test coverage, and Gate 3-verifiable criteria. Grounded in `.specify/assessments/heirloom-seeds-pipeline/` (verify: intake, research, problem, concept, decision — verdict GO, Option B)."

## Clarifications

### Session 2026-11-09 (assessment-driven; spec decisions, not re-asked)

- Q: Distribution path without Elena (Elena NPC unimplemented; briefing's `LivingRoots_ElenaShop` blocked) → A: **Traveling-cart special-stock entry** (`Data/Shops` key `Traveler`) with `AvailableStockLimit: Player` and (planned, validated in plan) `Condition: SEASON Spring`, plus the vanilla Seed Maker path (produce of a crop entry) — crioulo parsnip seeds are cart-acquired and seed-saving-sustained, never Pierre-stock and never Elena-stock. **Pierre sells nothing crioulo** this slice.
- Q: Seed-saving rule shape → A: **Capped, communicated, one saved seed per fully-grown harvest per plant** (integer), messaged post-harvest (e.g., `message.heirloom.seed-saved`), no probability, no RNG, no farming-level modifiers. Rationale: harvest cadence parity with "1 crop → 1 saved seed" keeps farm economics neutral-to-slightly-restrictive even in co-op; cheapest to verify; loudest UX (the ecosystem's anti-pattern is silent RNG).
- Q: Farmhand (non-host) path for the soil-health plant bonus → A: **Host-authoritative mirror of the FR-009/A8 pattern.** Host applies `+HeirloomPlantBonus` when `HoeDirt.plant` postfix detects an heirloom seed; farmhands do **not** mutate soil state — they send a `HeirloomPlantBonusRequest` (new typed message, `modIDs`-scoped) to the host; host validates & applies & broadcasts the existing `SoilHealthSyncMessage`. If the host is unreachable/unreachable-correlation, the plant still happens (game-side) but the soil bonus is **not applied** and the farmhand sees the standard authority-unsupported refusal copy (no false success, no silent divergence).
- Q: Extra-credit harvest coupling (+1 healthy-band bonus) → A: **Delegate to the existing 014 seam**: the heirloom parsnip produce enters the same `IHarvestQualityService` path as any crop (tiers 31/71); the slice adds **no new quality math**. `+HeirloomHarvestBonus` is applied as an *additional flat soil-health write* on heirloom harvests occurring in the Healthy band — same lock/first-wins semantics, same NaN→100 neutral baseline as 017/C2; it does **not** stack a double 014 tier boost (014 computes quality; the slice adds at most +1 flat health, recorded in-state via the existing service).
- Q: Elena/Pierre/Tom arbitration → A: **Pierre untouched. No `SeedShop` edit this slice.** The Pierre↔crioulo contrast is expressed via Traveler + copy, not via shop removal or gating. Elena remains unimplemented; no NPC/shop wiring this slice.
- Q: Art → A: **Procedural placeholder sprites** (Python/Pillow-friendly 16×16 seed/produce icons + one 16×32×~7 crop row), clearly disclosed as placeholders; final art is a maintainer deliverable; no AI-image-generated art; no tracing/copying of vanilla art. Vanilla-fallback `Texture` is rejected as MVP (it hides the feature; the player must see *something* crioulo).
- Q: Economics → A: seed price 35g (vs vanilla parsnip 20g), produce sell 45g (vs 35g), premium justified by slow crop + soil synergy; `AvailableStockLimit: Player`; no availability outside Spring this slice; no gift-taste/ailment-of-love data this slice (CanBeGivenAsGift default = true; explicit `love` list NOT set — acceptable and recorded).
- Q: Release scope this slice → A: **No manifest bump, no tag, no public channel.** Evidence and docs only; release/tag stay maintainer actions (precedent and constitution). A SemVer 0.2.0 decision is a maintainer call recorded later.
- Q: Growth speed → A: 7 days to harvest (`DaysInPhase = [1,2,2,2]`, 4 phases; sum 7), 1 phase behind vanilla parsnip pacing but with crioulo payoffs — mirrors Korpetis 2023 (landraces typically later); labeled as in-game abstraction in copy.

### Session 2026-11-09 (autonomous clarify; resolved from repo patterns + decompiled 1.6 mechanics)

- Q: Does the capped "saved seed" return fire on Junimo (automated) harvests, where there is no harvesting player inventory? → A: **No.** Saved seed is granted only to the harvesting player's inventory (Grab by `Farmer`, or via 014-supported Junimo path with a known harvester reference). Junimo harvests follow 014 junimo semantics; no seed refund is produced, and no workaround grafts seeds onto Junimo inventory (does not exist). A player-facing note is not required (Junimos do not surface heirloom-specific copy).
- Q: May heirloom crops be planted in the Greenhouse (season-override location), and do they behave identically? → A: **Yes.** The slice does not restrict locations; `Seasons=["spring"]` planting legality is enforced by the base game via `Crop.IsInSeason`/greenhouse override as usual; soil bonus, seed-save, and harvest bonus apply identically. Decay/mulch greenhouse exemption semantics are unchanged.
- Q: Does this slice add debug/verification commands for heirloom (like the `lr_give_mulch`/`lr_plant_crop` family)? → A: **Yes, verification-only:** `lr_give_heirloom_seed <x> <y> [location]` grants the seed to the host inventory, and `lr_sow_heirloom <x> <y> [seedId] [location]` plants through the real game path (`HoeDirt.plant`) so the Harmony plant-bonus postfix fires (queue parser + commands, additive only, never compiled into release routing beyond verification). Used by the new `heirloom-plant-bonus` scenario. This keeps headless Gate 3 possible without relying on cart RNG and exercises the production code path rather than the direct-Crop bypass.
- Q: If the `Traveler` `SEASON Spring` condition renders the seed unobtainable in the installed game state at runtime (e.g., cart week placement), is the acquisition path still considered verified? → A: **No unconditional claim.** Verification of obtainability uses (a) unit contract tests on the `Data/Shops` entry shape, and (b) `lr_give_heirloom_seed` for end-to-end Gate 3 (host-only machine-pass). Cart natural-acquisition remains an honest playtest observation item (P2 follow-up), never claimed from this cycle.
- Q: Are French/Spanish/German or Portuguese-formal localization required for this slice? → A: **No.** Locales = `default` (EN), `en`, `pt` only, per repo norm; parity enforced by tests as in 014/017.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Acquire and Sow a Crioulo Seed (Priority: P1)

As a player who cares about soil health, I can find a crioulo parsnip seed at the Traveler's cart (and sustain it by saving seeds), sow it on tilled farm soil, and see a distinct crioulo crop sprout and grow slower than the hybrid equivalent — with the soil responding positively, in a way I understand.

**Why this priority**: No acquisition path = no feature (the 017 descope learned this the hard way). This is the difference between a mod with heirlooms in data files and a mod where heirlooms exist.

**Independent Test**: In a host-controlled save, the Traveler's cart deterministic configuration (or debug grant in verification) provides the crioulo parsnip seed; the player plants it; the crop appears as a distinct crioulo crop row; after 7 in-game days it is harvestable; the soil-health tile value increased by `HeirloomPlantBonus` post-plant; the event is logged/synced host-side.

**Acceptance Scenarios**:

1. **Given** the Traveler's cart is open in Spring, **When** the player views the cart's special-stock section, **Then** a `LivingRoots_HeirloomParsnipSeeds` entry is offered (price 35g; player-specific stock limit; correct crioulo icon/name/description in the active locale).
2. **Given** the player holds a crioulo parsnip seed and interacts with tilled soil, **When** the plant completes, **Then** the crop registered for the heirloom seed ID appears from the crioulo crop sheet, and the host records measured soil health `+HeirloomPlantBonus` on that tile.
3. **Given** a farmhand plants the crioulo seed (non-host), **When** the plant completes game-side, **Then** the farmhand's client sends the host a bounded `HeirloomPlantBonusRequest`; the host applies `+HeirloomPlantBonus` authoritatively and broadcasts `SoilHealthSyncMessage`; the farmhand's view converges on snapshot delivery. If the host is unreachable, the farmhand receives explicit authority-unsupported copy and **no** false "soil improved" signal is shown.

---

### User Story 2 — Harvest, Get Paid, and Save the Seed (Priority: P1)

As a player, when I harvest a crioulo parsnip, I want the produce to sell at a premium, the soil to be rewarded for being Healthy, and at least one crioulo seed to come back so my crioulo line stays alive without rebuying — the seed-sovereignty loop, legible and honest.

**Why this priority**: The harvest is where the entire loop pays off; kills "plant → harvest → compost bin" mundanity; it's the agronomic soul of the feature (true-to-type saved seeds; Pierre's hybrids do *not* come true-to-type).

**Independent Test**: Host plants crioulo parsnip on a Healthy-band tile; 7 days pass; harvest yields 1 produce + 1 crioulo seed (capped "saved seed"); soil health reflects `+HeirloomHarvestBonus` written host-authoritatively; item/name/icon resolve in active locale; compost-bin path accepts the produce.

**Acceptance Scenarios**:

1. **Given** a Healthy-band (≥ `ModerateHealthyBandEdge`, i.e. ≥ 71) tile with a mature crioulo parsnip, **When** the player harvests, **Then** the player receives the crioulo produce; the host records `+HeirloomHarvestBonus` on the tile (bounded, validated); existing 014 quality/extra-harvest rules apply unchanged to the heirloom crop (no double-counting); and **one** `LivingRoots_HeirloomParsnipSeeds` is added to the harvesting inventory (or a non-misleading full-inventory refusal copy if full, matching existing inventory-full feedback conventions).
2. **Given** the same harvest under Poor/Moderate band, **When** the harvest completes, **Then** the crioulo produce and the saved seed still drop (seed-saving is not soil-gated — the seed line deserves a fighting chance), but `+HeirloomHarvestBonus` does **not** apply; 014 quality math runs unchanged.
3. **Given** a co-op session where a farmhand harvests the crioulo parsnip, **When** the harvest completes, **Then** the produce/saved-seed items flow normally on the farmhand's client; the soil `+HeirloomHarvestBonus` write is host-mediated (host applies and broadcasts sync), never locally mutated by the farmhand.
4. **Given** the harvest is performed by a Junimo (for completeness), **When** the 014 junimo-guard applies, **Then** behavior matches 014 (as-designed); no crioulo-specific bypass is introduced.

---

### User Story 3 — Understand the Crioulo Choice (Priority: P2)

As a player, I want the crioulo option to read as a real agronomic decision — slower but richer and kinder — with honest text, correct EN/PT parity, and no moralizing, so the mechanic educates without preaching.

**Why this priority**: The agroecology framing *is* the product differentiator; bad copy either alienates or mis-teaches (the project treats framing as a safety property).

**Independent Test**: Read the seed/produce descriptions and the harvest message in EN and PT; verify vocabulary (sementes crioulas / crioulo; guardian/guardião semantics *not* over-claimed), the labeled-abstraction reading, absence of nitrogen claims (not a legume), and Pierre's hybrids are never slandered — factual, warm, cozy.

**Acceptance Scenarios**:

1. **Given** the player inspects the crioulo parsnip seed, **When** they read its description in EN or PT, **Then** it communicates: slow, kind to the living soil, saved seed keeps the line alive, not a miracle fertilizer; Pierre hybrids remain valid for high-input systems (copy never says hybrids are "bad").
2. **Given** any crioulo UI/feedback string is shown, **When** comparing `en.json` and `pt.json` (and `default.json`), **Then** meaning and tone parity hold (no untranslated leakage; PT uses correct agroecology register: "sementes crioulas", "solo vivo", "guardar a semente").
3. **Given** after-harvest feedback is shown, **When** the seed is saved, **Then** a short localized message confirms the save (`message.heirloom.seed-saved`); if the inventory was full, a localized non-misleading full-inventory message is shown instead.

---

### User Story 4 — Verify the Slice Honestly (Priority: P2)

As a maintainer/verifier, I can evaluate this slice with explicit, testable acceptance criteria and honest evidence, so progress is real and not overstated.

**Why this priority**: TDD + Gate 3 honesty are non-negotiable project rules; this slice must not repeat 003/010 coverage theater.

**Independent Test**: Review the spec's ACs and verification notes: unit/property-style checks cover registration contracts and rule math; a headless host-only Gate 3 scenario exists for the plant bonus; D1 and human-E remain `pending`.

**Acceptance Scenarios**:

1. **Given** the implementation is complete, **When** the full suite runs, **Then** all tests pass (no regressions from baseline 1937/0/4); targeted heirloom tests cover: `Data/Objects` registration (seed+produce), `Data/Crops` entry (key = seed ID; `DaysInPhase` sums to 7; `HarvestItemId` resolves to the produce ID; `HarvestMaxStack` = 1), `Data/Shops` `Traveler` entry contract (Player-limited stock), plant/harvest bonus math incl. bounds/NaN/unknown band, seed-save cap behavior, and i18n key presence + parity checks (EN/PT/default).
2. **Given** the headless runner, **When** the new `heirloom-plant-bonus` scenario runs host-only, **Then** it reaches `machine-pass` with `verdict: pass` (soil-health delta signal + crop-phase delta + SMAPI log host check), honesty R; **no** `human-verified`/`d1-farmhand-convergence` claim is produced or implied.
3. **Given** the verification record, **When** claims are audited, **Then** the claim records mark D1/farmhand/human/SC-heirloom-E as `pending`, consistent with 017's pattern; nothing is promoted.

---

### Edge Cases

- Unmeasured/NaN soil at plant/harvest → neutral baseline resolves to `MaxSoilHealth` (100) per the 017 C2 convention; measured post-write is recorded; no invisible no-op.
- Soil already at Max (100) at plant/harvest bonus time → bounded write clamps at 100; result records clamped amount; messaging does not imply "you gained more than possible".
- Inventory full on harvest (produce or saved seed) → 014's inventory-full feedback convention applies (existing `message.harvest.inventory-full` keys reused); saved seed follows the same rule; no silent item destruction.
- Cart special stock not offered that day (CART random) → Players are not hard-blocked: `lr_give_heirloom_seed` (verification) and Seed Maker produce-recovery path allow verification/continuation; cart odds are tuning, not correctness.
- Farmhand plants with host unavailable → explicit authority-unsupported refusal feedback; **no** soil mutation client-side; plant itself completes (game) unchanged.
- Concurrent plant/harvest on same tile (host vs farmhand) → atomic first-wins lock semantics (existing `HostAtomicLockService`) unchanged; second actor gets explicit refusal or is re-evaluated against new state; never silent last-write-wins.
- Heirloom produce in `OrganicWasteValidator` → must pass as `compostable_item` (category/tag) — verified by test.
- New item ID collision with vanilla or other mods → namespaced `LivingRoots_Heirloom*` IDs, qualified `(O)LivingRoots_Heirloom…` forms, and tests assert uniqueness against vanilla numeric IDs.
- Crop `CountForMonoculture/Polyculture` → **both true** (crioulo crops are real food crops for achievements — no false UI promises).
- Save from a build without the mod → vanilla Error Item path applies (documented 1.6 behavior); no save corruption; no migration code required (items are additive).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The mod MUST register exactly three new data-layer items for this slice — `LivingRoots_HeirloomParsnipSeeds` (seed; `Type: Seeds`, `Category: -74`), `LivingRoots_HeirloomParsnip` (produce; `Type: Basic`, `Category: -75`), and the `Data/Crops` entry keyed by the **seed** ID — via `ModEntry.OnAssetRequested`, following the existing registration conventions (texture assets served via `LoadFromModFile`, EN/PT/default localization, `ModConstants` for IDs).
- **FR-002**: The `Data/Crops` entry MUST define: `Seasons = ["spring"]`, `DaysInPhase = [1,2,2,2]` (7 days; sum invariant asserted by test), `RegrowDays = -1`, `IsRaised = false`, `NeedsWatering = true`, `HarvestItemId = "LivingRoots_HeirloomParsnip"`, `HarvestMinStack = 1`, `HarvestMaxStack = 1`, `ExtraHarvestChance = 0.1`, `HarvestMethod = "Grab"`, `HarvestMinQuality = 0`, `TintColors = []`, `Texture` = mod-served crioulo crop sheet (custom `Mods/lfgranja.LivingRoots/HeirloomCrops`), `SpriteIndex = 0`, `CountForMonoculture = true`, `CountForPolyculture = true`.
- **FR-003**: The Traveler's cart (`Data/Shops` key `Traveler`) MUST offer the crioulo parsnip seed with `AvailableStockLimit: Player`, a Spring `Condition` (`SEASON Spring`), and a price of 35 (documented provisional/tunable first-pass; not a balance claim). If `SEASON Spring` cannot express via the selected game-state-query implementation, the spec MUST fall back to an unconditional Traveler entry — the acquisition path must be provably reachable; the choice is recorded in the plan/validate artifacts, never silently dropped.
- **FR-004**: Pierre's shop (`SeedShop`) and the Joja shop MUST NOT be edited by this slice (no entries, no conditions, no reorderings).
- **FR-005**: The mod MUST register a `HeirloomSeedSurface` constant block (strategy: mirror the `MulchTag`/agrochemical patterns): item/qualified IDs, price, texture asset names, `HeirloomSeedTag`/`HeirloomProduceTag` context tags (`heirloom_seed`, `heirloom_produce`), `HeirloomPlantBonus` (provisional 2.0, 0–100 bounded, labeled abstraction), `HeirloomHarvestBonus` (provisional 1.0, 0–100 bounded, labeled abstraction), `HeirloomSavedSeedYield` (= 1), and `TranslationKeyHeirloomSeedSaved` etc. — all `ModConstants`, no new hardcoded literals elsewhere.
- **FR-006**: On an **heirloom seed plant event** (detected via postfix on `HoeDirt.plant` scoped to heirloom seed IDs only, or an equivalent explicit plant-time seam), the host MUST apply `+HeirloomPlantBonus` to the tile soil health using the existing atomic lock/first-wins + validated bounded write (NaN→MaxSoilHealth neutral baseline, clamp 0–100), following the 017 phase-order conventions; farmhands MUST NOT mutate soil state directly — they trigger the host-authoritative request path (new typed message, `modIDs`-scoped) or receive explicit authority-unsupported feedback.
- **FR-007**: On an **heirloom crop harvest event** (via the existing Harmony harvest seam, extended for heirloom crop identity only — no new patch on unrelated crops), the host MUST apply `+HeirloomHarvestBonus` when the tile's measured soil health is in the Healthy band (≥ `ModerateHealthyBandEdge` = 71); below Healthy, no bonus is written and the existing 014 decision math runs unchanged.
- **FR-008**: The existing 014 harvest seam MUST remain the single authority for quality/extra-harvest decisions; this slice MUST NOT fork, replace, or re-tune the 014 decision service. Heirloom crops participate in 014 exactly as vanilla crops do (tiers, composition with vanilla math, Junimo guard).
- **FR-009**: On every heirloom harvest that completes (any band), the harvesting player's inventory MUST receive exactly `HeirloomSavedSeedYield` (1) heirloom parsnip seed(s) if space permits; otherwise the existing `message.harvest.inventory-full` feedback convention applies and no seed is silently destroyed. The seed drop MUST NOT be a probability and MUST NOT be farming-level-scaled.
- **FR-010**: The crioulo produce MUST carry context tag `compostable_item` and MUST pass `OrganicWasteValidator` checks (categories accepted today: −74/−75/−79/−80/−81); a unit test MUST assert this.
- **FR-011**: The mod MUST ship EN, PT (`pt.json`), and default locale strings for all new keys (`item.heirloom.parsnip.name`, `item.heirloom.parsnip.description`, `item.heirloom.parsnip-seed.name`, `item.heirloom.parsnip-seed.description`, `message.heirloom.seed-saved`, `message.heirloom.seed-saved-inventory-full`, `message.heirloom.plant-bonus-authority-unsupported` — final key naming per plan), with meaning parity across the three locales and factual, warm, non-moralizing copy (cozy tone; no nitrogen claims; Pierre explicitly unvilified; abstractions labeled in dev docs only).
- **FR-012**: Multiplayer correctness: host-authoritative mutation, per-tile atomic first-wins, no second source of truth, no client-side soil mutation by farmhands, no fabricated movement; the 017 refusal grammar (authority-unsupported, conflict, commit-failed) is reused pattern-wise for the farmhand path; `SoilHealthSyncMessage` broadcast covers new writes; item modData lineage (if used) MUST sync as per game behavior and MUST be covered by a unit test using the existing sync stubs.
- **FR-013**: Persistence: additive items only; zero measured (0) soil values remain unsupported for save/load by the sparse-cache limitation (pre-existing, documented, not introduced here); no save migration code; no changes to `SoilHealthState` file format.
- **FR-014**: Verification: this spec MUST define testable ACs (this document), a headless host-only Gate 3 scenario (`heirloom-plant-bonus`), and a claims discipline identical to 017 (`claim-records.md` style; D1/human/SC-004-class claims `pending`).
- **FR-015**: The release boundary: no manifest bump and no tag in this slice unless the maintainer explicitly directs one; the `.specify/releases/` bookkeeping for this slice (if any) MUST record "no public channel, no tag" or be omitted entirely — never invented.
- **FR-016**: Documentation: `ROADMAP.md`, `OBJETIVO.md`, and (at cycle end) `COMPLETION_SUMMARY.md` gain append-only 2026-11-09 annotations marking this spec's artifacts and verification status — no rewriting of existing annotations.
- **FR-017**: The crioulo crop sheet and two item icons are **placeholder art** produced procedurally (or a disclosed fallback texture pointer as recorded deviation in validate.md); the deficiency is disclosed in artifacts and release/validate notes; final art is a maintainer deliverable.
- **FR-018**: All tuning values (prices, bonuses, `ExtraHarvestChance`) are first-pass tunables recorded in `ModConstants` with the 017-style "tuning requires sync" comments; none is presented as a balance claim.

### Key Entities *(include if feature involves data)*

- **Heirloom Seed (`LivingRoots_HeirloomParsnipSeeds`)**: `Data/Objects` (Seeds/−74) + `Data/Crops` key; context tag `heirloom_seed`; sells 35g; plantable only in Spring; slow (7d); guaranteed saved-seed return on harvest.
- **Heirloom Produce (`LivingRoots_HeirloomParsnip`)**: `Data/Objects` (Basic/−75); `compostable_item` tag; 45g; premium; crioulo texture.
- **Crioulo Crop Row**: custom `Data/Crops` texture asset (`Mods/lfgranja.LivingRoots/HeirloomCrops`); 4 visual phases × 2 seed variants; `SpriteIndex = 0`; crioulo look via deliberate hue/till contrast vs vanilla parsnip row (placeholder permitting).
- **Soil Coupling Seams** (reuses existing): `HoeDirt.plant` (plant-time hook), harvest Harmony seam (`HarvestPatches`, crop-identity filter), Healthy band classifier (`HealthCategory`, 31/71), atomic host lock (`HostAtomicLockService`), sync broadcast (`SoilHealthSyncMessage`), inventory-full feedback conventions.
- **Saved Seed Contract**: `HeirloomSavedSeedYield = 1` per harvest; integrated with the full-inventory refusal conventions; no probability; no persistence format change (item only).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A player (host) can acquire the crioulo seed from the Traveler's cart in Spring, plant it on tilled soil, and observe a distinct crioulo crop; measured soil health on that tile increases by `+HeirloomPlantBonus` (validated bounded write of 2.0 unless tuned; exact value documented as provisional). (countable: 1/1 acquisition path in-scope)
- **SC-002**: After 7 in-game days from planting, the crioulo parsnip is harvestable; the player receives the crioulo produce (45g sell value) plus exactly one heirloom seed returned to inventory. (countable)
- **SC-003**: Soil health on Healthy-band tiles receives `+HeirloomHarvestBonus` (provisional 1.0) per heirloom harvest; no soil write occurs off-band; 014 quality math composes unchanged. (assertion/unit + Gate 3 machine-pass)
- **SC-004**: All targeted heirloom tests green; full suite passes with no regressions vs baseline 1937 passed / 0 failed / 4 skipped; format gate clean; targeted scenario reaches `machine-pass` host-only (honesty R) with verdict `pass`.
- **SC-005**: EN/PT/default locale parity for all new keys asserted by automated checks; copy contains no nitrogen claims and no Pierre vilification; facts match the agronomy audit (labeled abstractions only in dev docs).
- **SC-006**: Zero claims promoted beyond `host-only machine-pass`: D1, `human-verified`, and SC (comprehension) for this slice are recorded `pending` in claim records/validate artifacts.
- **SC-007**: `Data/Crops` integrity at runtime (headless where possible): entry loads, `HarvestItemId` resolves to a registered produce item, `DaysInPhase` sums to 7 — covered by unit + smoke (or explicit recorded deviation if unreachable headless).

## Assumptions

- SDV 1.6.14+ / SMAPI ≥ 4.4.0 (manifest floor) — this host builds against 1.6.14+ assemblies; 1.6.15 compatibility confirmed via current stable; 1.6.16/1.7 designs untouched.
- The Traveler's cart `Traveler` key and `AvailableStockLimit` semantics are as decompiled/verified 2026-11-09 (`ShopData`/`ShopItemData`, wiki snippets); if stock-limit behavior diverges in-game, the fallback (unconditional cart entry or `SeedShop` condition entry — **only with explicit maintainer approval**, since SeedShop is out of this slice's mandate) is recorded; Pierre is never edited autonomously.
- `HoeDirt.plant` postfix detection can identify heirloom seeds in all supported paths (right-click, controller, Junimo replant if any); if a plant path bypasses the postfix, it is documented as a known limitation with a manual verification note rather than silently accepted.
- The 017 verification infrastructure (runner, scenario schema v3, bundle promotion, claims validator) is reused as-is; no runner changes beyond a new scenario + tests.
- Farmhand plant-bonus message schema (`HeirloomPlantBonusRequest`) reuses the `SoilHealthSyncMessage` envelope conventions; if simpler (e.g., refuse with explicit copy, as Agrochemical C4:A) proves the only honest MVP, the refusal path is substituted with explicit disclosure — final call recorded in the plan/validate artifacts, never silent.
- Procedural placeholder art is acceptable to the project for this un-released, repo-scoped slice; final art is maintainer-owned and tracked as follow-up.
- Players/holdings: crioulo seeds are additive items; no save migration; sparse-zero persistence limitation carries over unchanged.
- Spanish/French/German localization is out of scope this slice (repo i18n norm: default/en/pt).
- No new dependencies (no Content Patcher requirement; pure SMAPI + Harmony as existing).
