# Concept: Mulch / Soil Cover

- **Slug**: mulch-soil-cover
- **Created**: 2026-10-30
- **Recommended option**: Option A — Item-Applied Mulch (Briefing Design)

## Options

### Option A — Item-Applied Mulch (Briefing Design)
- **Sketch**: Mulch is a craftable item (fiber ×10) applied to a tilled tile by right-click, like vanilla fertilizer. It zeroes soil decay for 7 days, prevents weeds, and shows a brown tinted overlay with a daltônico-friendly Dots pattern. After 7 days it expires silently. No new crop, no new machine — just an item + a decay hook + a tile-state flag.
- **Appetite**: small (days to a week)
- **Trade-offs**: lowest cost (1 object, 1 recipe, 1 hook, 1 persisted flag); reuses all existing infra (decay, overlay, i18n, persistence, multiplayer guards); strongest agronomic evidence (runoff −47%, soil loss −76%). Sacrifices: no "planting" fantasy (mulch is applied, not grown); no visual of mulch breaking down; no interaction with rain beyond "rain doesn't remove".
- **Rabbit holes**: Elena shop dependency (recipe sale blocked until Elena exists — craftable recipe works without her); overlay z-order vs. spec 001 health overlays; `MulchDecayMultiplier` balance (0f vs 0.25f); sprite creation (16×16, but colors specified so placeholder viable).

### Option B — Mulch as a Crop (Nexus Mod 7457 approach)
- **Sketch**: Mulch is a plantable crop that takes 2 days to grow, then is scythed for fiber. It protects soil while growing. Reuses vanilla crop pipeline (no new item, no new service).
- **Appetite**: small (days)
- **Trade-offs**: zero new code (leverages existing crop system); familiar to players of the Nexus mod. Sacrifices: clunky UX (watering, scything, 2-day wait); conflicts with real crops on the same tile; doesn't teach "cover the soil" as a deliberate action; no persistent tile state; no multiplayer host-authority model.
- **Rabbit holes**: crop growth timing (minimum 2 days in JA); crow vulnerability; conflicts with real crops; doesn't integrate with SoilDecayService (decay still runs under the "crop").

### Option C — Do Nothing / Buy Instead of Build
- **Sketch**: Skip mulch entirely. Rely on compost (reactive) and the crop-growth hook. Document the agroecological concept in tooltips/educational text only.
- **Appetite**: trivial (hours)
- **Trade-offs**: zero implementation cost; no risk. Sacrifices: the "build → protect → harvest" loop stays incomplete; the strongest-evidence mechanic (mulch) is never shipped; differentiation from ReGen Valley limited to compost alone; 80/20 stalls at step 2.
- **Rabbit holes**: none — but the opportunity cost is high (roadmap's own ordering says mulch is step 3).

## Recommendation

**Option A (Item-Applied Mulch).** It is the cheapest complete feature that closes the "protect" gap in the agroecological loop, has the strongest scientific backing, and reuses all existing infrastructure. Option B's crop-based approach is a worse UX and doesn't integrate with our soil-decay system. Option C abandons the 80/20. Option A's main risk (Elena dependency) is mitigated by making the shop criterion conditional on Elena's existence and shipping the craftable recipe first.

## Out of Scope (for the recommended option)

- Elena NPC and her shop (deferred; recipe is craftable with fiber).
- New crop or plantable mulch (mulch is an applied item, not a crop).
- Weed prevention as a core mechanic in Y1 (visual/secondary only).
- New `.ogg` audio (reuse vanilla `hoeHit`/`sandyStep`/`axe`).
- Mutation testing (Stryker.NET) — non-goal per spec 011.
- Multiplayer live verification (roadmap D1) — mechanism only via stubs.
- SonarCloud/CI changes (handled by spec 011).
- Crop quality and extra harvest (separate specs, later in 80/20 order).

## Assumptions to Validate

- `MulchDecayMultiplier = 0f` (full protection) is balanced with 7-day duration + 10 fiber cost. Needs playtesting (0 in-game runs exist).
- The `mulch.png` 16×16 sprite can be created (placeholder viable with specified colors `#92400E`/`#A16207`/`#78716C`).
- `tile.HasMulch` state persists correctly via `ModDataService` with sparse cache (zeros omitted) and respects `MaxTilesPerLocation`/`MaxTilesPerSave`.
- The mulch overlay tint + `Dots` pattern does not conflict with spec 001 health overlays (z-order).
- `ExecuteWithConcurrencyGuard` + `Context.IsMainPlayer` guard suffice for multiplayer correctness (Principle VI) without live testing.
- Elena's absence does not block the feature (craftable recipe is the primary path).