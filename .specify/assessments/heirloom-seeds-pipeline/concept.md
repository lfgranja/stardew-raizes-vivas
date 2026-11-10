# Concept: Heirloom Seeds / Sementes Crioulas Pipeline

- **Slug**: heirloom-seeds-pipeline
- **Created**: 2026-11-09
- **Recommended option**: Option B — One-crop template slice ("Sementes Crioulas: Crioulo Parsnip")

## Options

### Option A — Briefing-as-written: 3 crops + 3 seeds + 3 produce + Elena shop
- **Sketch**: Ship the full Epic-6 briefing: crioulo parsnip (spring), crioulo summer + fall crops, `LivingRoots_ElenaShop` selling them with friendship-discount `PriceModifiers`, `Condition: "PLAYER_HEARTS LivingRoots_Elena 4"`, full i18n, prices ~1.25× Pierre, feeding Welcome Kit/Elena later.
- **Appetite**: large (weeks; 3 crop asset sets + Elena dependency + shop UX)
- **Trade-offs**: Wins completeness and fully staged Three Sisters. Sacrifices everything the 80/20 rule stands for: Elena NPC does not exist (briefing blocked at its distribution layer); shipped content would be unverifiable end-to-end (no shop can be opened without the NPC/map/action wiring); asset burden 3× without validating the pipeline once; fails the "demonstrable loop" test that 004/013/017 all passed.
- **Rabbit holes**: jumping to content breadth before the pipeline seam is proven; disappearing into sprite authoring; resurrecting Elena by stealth (a hard non-goal); trying to fix Seed Maker UI while at it.

### Option B — One-crop template slice: "Sementes Crioulas — Crioulo Parsnip" (RECOMMENDED)
- **Sketch**: What the player experiences: a Spring crioulo parsnip seed appears in the **Traveler's cart** (a crioulo travando de mão em mão — the cart is the smuggler of farmer genetics; no NPC needed, data-driven `Data/Shops` entry with `AvailableStockLimit Player`, `Condition: SEASON Spring`); plant it (soil likes it: **+2 abstract**, labeled as a game rule), it grows slower than vanilla (7 days vs 4 — the crioulo tradeoff, mirroring real landrace lateness), in Healthy soil the harvest pays back (**+1 abstract** + quality/extra-harvest synergy via the existing 014 seam — no double-counting), and the harvest **saves a seed** back (capped, communicated — the seed sovereignty loop; not silent RNG). Produce is compostable (fits the waste validator) and ships as premium (45g vs 35g). Pierre keeps hybrids. No Elena. No 3-crop set — one template proves the pipeline; the next two crops are a fill-in exercise on the same seam.
- **Appetite**: medium (days) — one crop row of sprites (or disclosed placeholder), registration + const + seams + tests + one Gate 3 scenario; comparable to the 017 MVP in effort class.
- **Trade-offs**: Wins a playable, honest crioulo loop with template seam; zero Elena/map/quest dependencies; reuses 014/005/013 seams; machine-pass possible host-only; PT/EN honest copy (abstractions labeled, nitrogen only for legumes, Pierre stays valid). Sacrifices content breadth (1 of 3 Y1 crioulo crops), the Elena discount narrative (deferred until Elena ships — by design), and requires the seed-saving rule to be designed narrowly to avoid MP economy flood.
- **Rabbit holes**: generic-registration temptation (hardcode nothing generic this cycle — two more constants ≠ premature abstraction); sprite polish spiral (placeholder stays disclosed); Seed Maker interop scope (vanilla path works; no custom machine work); "just one more crop"; accidental scope into Three Sisters via `isRaised`/adjacency code.

### Option C — Cross-cutting seed-saving only (no new content)
- **Sketch**: No new crop. Extend the harvest seam so *any* harvest of an open-pollinated-compatible item can drop a saved seed (maybe vanilla parsnip only), with a HUD/message. Everything else stays the same.
- **Appetite**: small (days)
- **Trade-offs**: Wins shippable seed-saving mechanics cheaply; forfeits the crioulo content itself (no heirloom crop to save seeds *from*), misses the narrative core, doesn't unblock Three Sisters/Green Manure/CSA, and flirts with vanilla-economy disruption (parsnip seed economy) that Pierre explicitly relies on.
- **Rabbit holes**: rule matrix for every crop (open-pollinated or not?); economy balance vs Pierre; farming-level modifiers interacting with 014 quality boosts.

### Option D — Do nothing / wait for Elena
- **Sketch**: Keep heirloom pipeline as briefing until the Elena epic is built; meanwhile nothing ships.
- **Appetite**: none now; cost = roadmap stall + niche preemption (ReGen Valley + Wildroot can claim adjacent space).
- **Trade-offs**: avoids all risk; guarantees zero value; violates the explicit 80/20 selection mandate.

## Recommendation

**Option B.** It is the only option that satisfies the problem goals at honest cost: it ships a playable crioulo loop, proves the reusable pipeline seam, requires zero NPC/map work, remains fully host-authoritative multiplayer-safe (Principle VI pattern: host-applies or explicit farmhand-refusal; `Item.modData` lineage syncs natively in MP), is fully test-greppable at the registration layer (existing `OnAssetRequested` test pattern), and has a clear headless Gate 3 story (plant bonus via `lr_plant_crop` on a crioulo seed ID + soil delta assertion). It is exactly what Research §8 concluded: template first, breadth later, Elena never required for the core proof.

Why not A (blocked distribution + 3× asset cost + no end-to-end playtest of the core seam), not C (no crioulo content — the whole point), not D (opportunity cost; explicit mandate).

## Out of Scope (for the recommended option)

- Elena NPC/shop/discount; Welcome Kit; any ref to `LivingRoots_ElenaShop`.
- Second and third crioulo crops (Summer/Fall) — follow-up fill-in on the proven seam.
- Genetics/trait simulation (drift, hybridization, naming) — `Item.modData` tag at most; nothing else.
- Three Sisters, Green Manure, Rotation Journal, CSA implementation — consumers only after this seam merges.
- Joja/monoculture completeness; Fertilizer hooks; pest system; Ring of price modifiers.
- Public release, tags, Nexus upload, storefront copy — maintainer decision; at most repo-scoped evidence like 017 (and only if the cycle chooses to cut 0.2.0; see decisions to be made in `/speckit-specify`).
- Any claim of multiplayer convergence — D1 remains environment-blocked; farmhand path covered by stubs + explicit refusal messaging only.
- Final art — placeholder or disclosed fallback; final sprites = maintainer deliverable.
- Changes to historical contracts (34/67), spec 010 scope, or the agrochemical slice.

## Assumptions to Validate

- Vanilla `Seed Maker` will process crioulo parsnip produce (it has a `Data/Crops` entry) — validate in a host-only run (not shippable-claim critical; nice-to-have).
- `Data/Shops` `Traveler` entries accept `AvailableStockLimit: Player` and show the seed with `Condition: SEASON Spring` — verify by decompile/test fixture; fallback = unconditional cart entry (simpler, still fine).
- `HoeDirt.plant` postfix + harvest-seam extension can query `ItemRegistry`/crop identity without touching 014/017 seams' invariants; farmhand-plant path either messages host or refuses explicitly (pick in `/speckit-specify` after reading multiplayer handlers).
- `Item.modData` lineage tag (`"harvestedFrom"`/`"heirloom"`) syncs in 1.6 multiplayer — verify against SMAPI docs (snippets show item modData sync; flag as assumption until unit-tested).
- Procedural placeholder sprites are acceptable to the project for an unreleased repo-scoped slice — final art stays a maintainer deliverable; disclose in release/validate docs.
- Tests stay ≥ 1937 passing with no regressions; strict build holds; `-m:1` mitigation continues in use on this host.
- Traveling cart in 1.6.15 sells normal-quality stock from its random pool; special-stock seeds with `Condition SEASON Spring` are supported by `Data/Shops` 1.6 GameStateQuery (`SEASON`).
