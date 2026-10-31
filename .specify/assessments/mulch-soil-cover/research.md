# Idea Research: Mulch / Soil Cover

- **Slug**: mulch-soil-cover
- **Created**: 2026-10-30
- **Evidence confidence (overall)**: high

## Users & Demand

- Stardew Valley players actively seek soil-protection mechanics: a Reddit thread "Does a more realistic soil mod exist?" (r/StardewValleyMods) asks for soil pH, nitrogen content, crop rotation and fertilizer management — indicating latent demand for agroecological soil mechanics. — [source: https://www.reddit.com/r/StardewValleyMods/comments/1izx6qk/does_a_more_realistic_soil_mod_exist/] (confidence: medium)
- ReGen Valley (Nexus, Nov 2025) ships per-tile soil health, cover crops, grazing, faster growth at 70+/90+ and farm-wide quality/yield at 75+/90+ average — confirming the premise is commercially viable and that players install it. — [source: https://www.nexusmods.com/stardewvalley/mods/39740] (confidence: high)
- The existing "Mulch" mod (Nexus #7457, 2021) has 42 endorsements and treats mulch as a 2-day crop scythed for fiber — a different (and clunkier) UX than our item-applied cover; this is an opportunity to differentiate. — [source: https://www.nexusmods.com/stardewvalley/mods/7457] (confidence: high)
- Our own codebase already implements soil health, decay, compost, and crop-growth hook (spec 005), so mulch now has a concrete reason to exist (protecting soil that drives growth). — [source: repo `LivingRoots/Controllers/ModController.cs` growth modifier] (confidence: high)

## Prior Art

- **Nexus "Mulch" (mod 7457, 2021)**: treats mulch as a crop (2-day growth, scythe for fiber). Requires Json Assets. Clunky UX (watering, scything). Our design (item applied to tilled soil, 7-day duration, zero decay) is a genuine improvement. — [source: https://www.nexusmods.com/stardewvalley/mods/7457] (confidence: high)
- **ReGen Valley**: cover crops + grazing + soil health. Differentiates on agroecological grounding, educational framing, multiplayer correctness (our strengths). — [source: research/2026-10-roadmap-feasibility-and-agronomy.md §B] (confidence: high)
- **Internal**: `SoilDecayService` with `DailyDecayRate = 2/day`, `SeasonalDecayMultiplier` (Spring 0.5×, Summer 1.5×), `MaxTilesPerLocation`/`MaxTilesPerSave`, sparse persistence. Mulch hooks directly into this — low integration cost. — [source: repo `LivingRoots/Services/SoilDecayService.cs`, `ModConstants`] (confidence: high)
- **Internal**: `ICompostApplicationService` + `CompostingBinService` provide the exact pattern to mirror for `IMulchApplicationService` (DDD, Domain/Interfaces + Services). — [source: repo] (confidence: high)

## Market & Context

- Mulch/soil cover has the **strongest agronomic evidence** in the entire roadmap: a global meta-analysis (90 studies) shows runoff −47% and soil loss −76%. This is the most defensible claim we can teach. — [source: research/2026-10-roadmap-feasibility-and-agronomy.md §C.2] (confidence: high)
- Protecting tilled soil is a real player pain: bare soil decays at 2/day, and with crop growth now hooked, losing soil health has a gameplay cost. Mulch gives the player a defensive tool. — [source: repo `Constants.cs:57`, spec 005] (confidence: high)
- Cost of doing nothing: without mulch, the only way to maintain soil is compost (reactive, not preventive). Mulch adds a preventive layer, completing the "build → protect → harvest" loop. — [source: ROADMAP.md] (confidence: medium)

## Data & Constraints

- **Implementation cost**: 1 object (16×16 sprite) + 1 recipe + 1 hook in `SoilDecayService` + persistence (`HasMulch` + days remaining, sparse). Estimated smallest complete feature on the roadmap. — [source: briefing §1, §3, §9] (confidence: high)
- **Asset**: `LivingRoots/Assets/mulch.png` (16×16). Tile overlay reuses existing `Dots` pattern + `MulchColor` tint at 0.5 opacity — no new overlay asset. Particle reuses compost particle. — [source: briefing §2] (confidence: high)
- **Persistence**: `ModDataService` with `*_KeyPrefix` keys, sparse (zeros omitted), respecting `MaxTilesPerLocation`/`MaxTilesPerSave`. — [source: briefing §3, repo `SoilHealthService`] (confidence: high)
- **Multiplayer (Principle VI)**: state is farm-keyed via `SaveIdProvider`, writes host-authoritative, guards `Context.IsMainPlayer`, no `.Result`/`.Wait()`, no lock across I/O. — [source: briefing §6, constitution VI] (confidence: high)
- **Elena dependency**: recipe sold at "Elena shop day 1" — but Elena NPC is not implemented. The craftable recipe (fiber ×10) works without Elena; the shop sale is blocked. — [source: briefing §3 "Loja", ROADMAP.md Elena status ❌] (confidence: high)
- **Balance open question**: `MulchDecayMultiplier = 0f` (zero decay) vs `0.25f` (partial). Not decided. — [source: briefing §3, §8] (confidence: high)

## Evidence Against the Idea

- **Elena dependency blocks the "shop" acceptance criterion**: the briefing's store criterion ("receita comprável na Elena dia 1") cannot be satisfied until Elena is implemented. The craftable recipe (fiber) works, but the shop sale is a documented acceptance criterion that will fail. Mitigation: spec should make the shop criterion conditional on Elena or defer it. — [source: briefing §3, §7; ROADMAP.md Elena ❌] (confidence: high)
- **Asset creation is a real dependency**: the 16×16 sprite must be created. If no artist is available, this blocks the feature despite low code cost. The briefing specifies exact colors (`#92400E`, `#A16207`, `#78716C`) so a placeholder is feasible. — [source: briefing §2] (confidence: medium)
- **Mulch as a "crop" (Nexus mod 7457) is a competing UX**: some players may expect mulch to behave like the existing Nexus mod (plant, water, scythe). Our item-applied model is different and may confuse. — [source: https://www.nexusmods.com/stardewvalley/mods/7457] (confidence: medium)
- **Balance risk**: `0f` decay multiplier makes mulch too powerful (free soil protection with fiber). `0.25f` is safer but less satisfying. Needs playtesting — but no in-game runs exist yet. — [source: briefing §3; ROADMAP.md "In-game runs: 0"] (confidence: medium)
- **Overlay z-order**: the mulch tile overlay (tint + Dots pattern) must not conflict with existing health overlays (spec 001). Not verified. — [source: briefing §2] (confidence: low)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: `MulchDecayMultiplier` value — 0f vs 0.25f vs configurable?]
- [NEEDS CLARIFICATION: Elena dependency — make shop criterion conditional or defer?]
- [NEEDS CLARIFICATION: who creates the `mulch.png` sprite and when?]
- [NEEDS CLARIFICATION: 7-day duration — balanced? Configurable?]
- [NEEDS CLARIFICATION: overlay z-order compatibility with spec 001 health overlays?]
- [NEEDS CLARIFICATION: `MulchTag` propagation to other compostable items?]
- [NEEDS CLARIFICATION: should mulch prevent weeds (briefing says "também previne ervas") — is weed prevention in scope for Y1 or deferred?]

## Sources

- https://www.nexusmods.com/stardewvalley/mods/7457 (host: nexusmods.com, policy: allowlisted) — existing "Mulch" mod (prior art)
- https://www.nexusmods.com/stardewvalley/mods/39740 (host: nexusmods.com, policy: allowlisted) — ReGen Valley (competitor)
- https://www.reddit.com/r/StardewValleyMods/comments/1izx6qk/does_a_more_realistic_soil_mod_exist/ (host: reddit.com, policy: unrecognized — fetched as public source) — user demand signal
- https://www.crojfe.com/archive/volume-42-no.2/evaluating-the-effectiveness-of-mulching-for-reducing-soil-erosion-in-cut-slope-and-fill-slope-of-forest-roads-in-hyrcanian/ (host: crojfe.com, policy: unrecognized — fetched as public source) — mulch erosion reduction evidence
- https://www.sciencedirect.com/science/article/pii/S2666154326000724 (host: sciencedirect.com, policy: unrecognized — fetched as public source) — Palash mulch runoff/soil loss reduction
- Repo: `specs/future-roadmap/briefings/mulch-soil-cover.md`, `ROADMAP.md`, `research/2026-10-roadmap-feasibility-and-agronomy.md`, `LivingRoots/Services/SoilDecayService.cs`, `LivingRoots/ModConstants.cs`, `LivingRoots/Controllers/ModController.cs`, `.specify/memory/constitution.md` (Principle VI)
EOF
echo "written"