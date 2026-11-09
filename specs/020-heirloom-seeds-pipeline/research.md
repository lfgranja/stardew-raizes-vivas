# Phase 0 Research: Heirloom Seeds / Sementes Crioulas Pipeline (Crioulo Parsnip)

**Feature**: `020-heirloom-seeds-pipeline` | **Date**: 2026-11-09
**Sources**: consolidated from `research/2026-11-09-heirloom-seeds-pipeline.md` (multilingual web research + primary-source decompile verification, 2026-11-09) and `.specify/assessments/heirloom-seeds-pipeline/{intake,research,problem,concept,decision}.md` (assess GO, Option B). Nothing in this file contradicts them.

## Decisions (each: Decision / Rationale / Alternatives considered)

### D1 — Distribution channel
- **Decision**: `Data/Shops` entry on the traveling cart (`Traveler`), `AvailableStockLimit: Player`, `Condition: SEASON Spring`, price 35g; Pierre (`SeedShop`) and Joja untouched; no Elena anywhere.
- **Rationale**: Only crioulo-compatible channel with zero NPC/map/quest work (verified: shops are data-driven string→model assets; `Traveler` confirmed by decompiled string table + wiki "Vanilla shop IDs"). Narrative fit: "sementes crioulas viajam de mão em mão" — exactly how Brazilian bancos comunitários work (AS-PTA seeds exchange). Player-specific stock limit prevents co-op farm flood.
- **Alternatives considered**: `LivingRoots_ElenaShop` (blocked: Elena unimplemented — rejected); `SeedShop` condition entry (rejected this slice: Pierre is deliberately untouched — less blast radius, contrast preserved via Traveler + copy); pure seed-saving bootstrap with no shop (rejected: zero organic acquisition path, unverifiable for new players); Seed Maker as sole path (kept as *additional* free path — it compounds, not replaces).

### D2 — One crop, not three
- **Decision**: Exactly one Y1 crop — Crioulo Parsnip (Spring, 7 days, `DaysInPhase=[1,2,2,2]`).
- **Rationale**: 80/20 — pipeline breadth is a fill-in cost on a proven seam; three crops before the seam is machine-passed is Option A scope creep (failed feasibility gate). Parsnip is the briefing's Y1 anchor and the cheapest art/asset row; vanilla parsnip gives a direct measured comparison (4d vs 7d; 20g vs 35g seeds; 35g vs 45g produce).
- **Alternatives considered**: three crops Spring/Summer/Fall (rejected: 3× art + appetite blowout); zero-crop seed-saving-only (rejected: no crioulo content — the point of the slice); corn crioulo (deferred: crioulo maize is agronomically iconic but heavier art + Three Sisters adjacency; future spec).

### D3 — Soil-health coupling magnitudes (labeled abstractions)
- **Decision**: `+HeirloomPlantBonus = 2.0` (plant), `+HeirloomHarvestBonus = 1.0` (Healthy band only, ≥ 71); both 0–100 bounded, host-applied, NaN→100 neutral baseline (017 C2 convention), atomic first-wins lock; NOT agronomic claims; labeled in dev docs/lore only.
- **Rationale**: Consistent with existing mod scale (compost +15, mulch 0 decay, agrochemical −10); small deliberate magnitudes keep crioulo a *catalytic* choice, not an optimization dominant strategy; Healthy-band gating rewards sustained care (the real farming practice) and reuses 014 thresholds; agronomy audit: planting≠fertility (labeled abstraction), no nitrogen for non-legumes, crioulo advantage = low-input stability + protein tendency (Zeibig 2024; Korpetis 2023), never "always better".
- **Alternatives considered**: larger bonuses (rejected: breaks 017-style calibration; crioulo must not out-economy 017); per-harvest quality bonus directly (rejected: double-counts 014; 014 remains the single quality authority); zero bonuses (rejected: makes crioulo a pure narrative toy; goals demand soil coupling).

### D4 — Saved seed rule (deterministic cap, not RNG)
- **Decision**: Every completed heirloom harvest returns exactly **1** `LivingRoots_HeirloomParsnipSeeds` to the harvesting player's inventory (or full-inventory refusal per existing convention). No probability, no farming-level scaling, no junimo yield (Junimo exempt; no player inventory exists there).
- **Rationale**: Loud and discoverable (ecosystem's top complaint is silent RNG); keeps farm economics near-neutral (1:1 seed reproduction models OP true-to-type selection — agronomically accurate); cheapest to verify deterministically; honest cap protects co-op economies alongside `AvailableStockLimit: Player`.
- **Alternatives considered**: 50–100% chance drop (rejected: probabilistic UX + variance, same failure mode as Harvest Seeds–Continued); 2+ seeds per harvest (rejected: exponential snowball in co-op); Seed-Maker-only return (rejected: adds mandatory machine hop for the core maintenance loop; kept optional).

### D5 — Quality/extra-harvest math: delegate to 014, never fork
- **Decision**: Heirloom crops ride the existing `IHarvestQualityService` decision path unmodified (31/71 tiers, composition with vanilla math, Junimo guard, scope filters). The slice adds only the flat `+HeirloomHarvestBonus` soil write and item-identity filtering.
- **Rationale**: Constitution I (single source of quality truth); double-counting risks were already adjudicated in 017/C1 (no double-counting policy); smallest surface to verify.
- **Alternatives considered**: heirloom-specific tier table (rejected: second quality authority; V/YAGNI); regrow variants (rejected: `RegrowDays=-1`; future crop class).

### D6 — Multiplayer authority for plant/harvest effects
- **Decision**: Host applies all soil writes. Farmhands plant/harvest game-side locally; for the soil bonus they send typed `HeirloomPlantBonusRequest` (`modIDs`-scoped envelope on the existing messaging bus) or, if the honest MVP cannot guarantee host correlation, the farmhand path explicitly refuses with authority-unsupported copy (mirror of 017 C4:A). Harvest `+HeirloomHarvestBonus` follows the same pattern; produce/seed items flow normally (client-local items). `SoilHealthSyncMessage` broadcast already propagates host state to farmhands.
- **Rationale**: Principle VI (host-authoritative, farm-scoped, no second source of truth, first-wins); 017 proved the refusal grammar; no new persistence; item modData (if lineage tag used later) syncs natively in 1.6.
- **Alternatives considered**: farmhand-applies-locally then reconciles (rejected: violates Principle VI first-wins semantics); host-only heirlooms (rejected: not first-class co-op — violates VI); no seed save for farmhands only (rejected: inconsistent rules per role).

### D7 — Art/assets strategy
- **Decision**: Procedural placeholder sprites (Python/Pillow) for seed icon (16×16), produce icon (16×16), and crioulo crop row (16×32 sprites; 2 seed variants + 4 phases × 16×32); clearly disclosed; final art = maintainer deliverable. No AI-image-generated assets; no copying of vanilla art or other mods' art.
- **Rationale**: Agent toolchain has no image-generation capability; placeholders keep the slice self-contained and honest; repo already ships procedural feedback textures (`HarvestFeedbackTextures`).
- **Alternatives considered**: vanilla `TileSheets\crops` fallback row (rejected as MVP visual: hides the crioulo identity; kept as documented deviation fallback only if art pipeline fails); purchase/commission art (rejected: out of autonomous scope, license review needed); block release on art (rejected: no release this slice anyway).

### D8 — Gate 3 scenario & claims
- **Decision**: New scenario `specs/020-heirloom-seeds-pipeline/scenarios/heirloom-plant-bonus.scenario.json` (schema v3): verification-only `lr_give_heirloom_seed` + `lr_plant_crop`-style plant on tile 60,15; asserts `soilHealthStateDelta` ≥ baseline + `HeirloomPlantBonus` bounded window, `cropPhaseDelta` increment ≥ 0 advancing over sleep days, `smapiLog` host/main-player check; machine-pass host-only, honesty R; D1/human/SC-heirloom claims `pending`.

### D9 — Release scope
- **Decision**: No manifest bump, no tag, no public channel this slice. `release.md` not cut; validation still recorded (`validate.md` by this cycle's validation step, with explicit "no release actions taken" line).
- **Rationale**: AGENTS/ROADMAP place publish/tag under maintainer actions; precedent 017 (repo-scoped release had to be explicitly recorded); nothing about this slice adds external distribution claims.

### D10 — i18n
- **Decision**: Keys `item.heirloom.parsnip(-seed).name/.description`, `message.heirloom.seed-saved`, `message.heirloom.seed-saved-inventory-full`, `message.heirloom.plant-bonus-authority-unsupported` (final naming in plan/tasks), parity EN/PT/default asserted by automated test; wording factual, warm, zero nitrogen claims, Pierre unvillified.

## Open items (validated during implement/verify — not NEEDS CLARIFICATION, recorded for honesty)

- `SEASON Spring` GameStateQuery on the Traveler special-stock entry behaves as documented in 1.6.15 (wiki snippets; validate by headless log inspection + fallback = unconditional cart entry, disclosed).
- `lr_give_heirloom_seed` queue plumbing mirrors `lr_give_mulch` pattern (additive, verification-only).
- Seed Maker accepts crioulo parsnip produce automatically (crop entry exists) — optional check, never a claim.
- `Item.modData` lineage tag: **not used in MVP** (no per-seed UUID); if future genetics slice needs it, reevaluate (1.6 item modData syncs; cited).
