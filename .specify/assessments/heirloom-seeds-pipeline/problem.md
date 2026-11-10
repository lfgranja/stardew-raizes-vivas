# Problem Definition: Heirloom Seeds / Sementes Crioulas Pipeline

- **Slug**: heirloom-seeds-pipeline
- **Created**: 2026-11-09
- **Inputs used**: intake.md yes | research.md yes | user input (autonomous mandate, this session) yes

## Problem Statement

Living Roots has shipped five gameplay loops (compost, mulch, growth, harvest-quality, Joja tradeoff) and has **no way for a player to acquire, grow, or save an heirloom ("crioulo") seed** — despite heirloom lineages being the narrative and agronomic backbone of the mod's identity (sementes crioulas × Pierre's hybrids; Brazilian agroecology) — so the Y1 promise is undeliverable and the four features that consume this pipeline (Three Sisters, Green Manure, Rotation Journal, CSA) cannot be specified or built.

## Affected Users & Stakeholders

- **Users**: cozy 25–45 players + co-op couples (primary project audience) — they cannot opt into the crioulo path the mod exists to teach; they get Pierre-hybrid parity with vanilla with no contrast decision; [source: OBJETIVO.md §1/§4; research.md §Users] (confidence: high)
- **Users**: educators/agroecology activists (PT/EN) — the institutional frame (CONAB/MAPA/Embrapa/MST: "sementes crioulas", "bancos comunitários", "soberania alimentar") finds no in-game counterpart; the mod's framing pillar (P2: "recompensar, nunca pregar") has nothing to reward. [source: research.md §Users; CONAB April 2026 relaunch] (confidence: high)
- **Users**: future mod consumers of the pipeline (Three Sisters, Green Manure, Rotation Journal, CSA — spec stubs exist as briefings only). [source: ROADMAP.md; briefings/] (confidence: high)
- **Stakeholders**: maintainer (`lfgranja`) — decides release scope (repo-scoped vs external), tag, publish, art direction; sole code owner (CODEOWNERS `@lfgranja`). [source: repo] (confidence: high)
- **Stakeholders**: the co-op second player — any seed/lineage state and soil rules must hold farm-scoped host-authoritative semantics (Principle VI); a broken MP story blocks "multiplayer is first-class" honesty. [source: constitution VI] (confidence: high)
- **Stakeholders**: Brazilian agroecology institutions informally referenced by the mod (MST, ANA, Embrapa, CONAB) — not formal partners; the mod must not imply endorsement; copy must stay factual and honest. [source: research.md §Users] (confidence: medium)

## Goals

- Give players an **obtainable, playable heirloom (crioulo) crop** in Y1 with a real growth/quality/soil tradeoff and a legible crioulo narrative, at 80/20 cost.
- Provide the **generic seed→crop→produce pipeline seam** that Three Sisters, Green Manure, Rotation Journal and CSA will reuse.
- Keep every rule **farm-scoped, host-authoritative, multiplayer-safe**, with explicit refusal semantics where unsupported (mirror agrochemical slice pattern).
- Ship **honest framing**: abstractions labeled as abstractions; no false agronomic claims (nitrogen only via legumes; no "crioulo feeds more people than hybrids" messaging); Pierre stays a valid, non-blocked choice.
- Establish **seed-saving as an explicit, discoverable loop** (not silent RNG), consistent with the ecosystem's top usability complaint.
- Produce **machine-verifiable evidence** (targeted tests + suite green + Gate 3 host-only machine-pass with new scenario) with zero claim promotion (D1/human E stay `pending`).

## Non-Goals

- **No Elena NPC, no Elena shop, no Welcome Kit, no dialogue** — intake shows the briefing depends on unbuilt Elena; distribution must not block on it.
- **No 3-crop content set** this cycle (briefing's Spring/Summer/Fall) — pipeline template + one representative crop; breadth is follow-up specs.
- **No genetics simulation** (trait drift, hybridization spread, population dynamics) — YAGNI; lineage tag in Item.modData at most.
- **No Joja/monoculture completude** — remains out of scope per ROADMAP; only the existing MVP agrochemical item remains tradeoff exemplar.
- **No Three Sisters / Green Manure / Rotation Journal / CSA implementation** — they stay briefings; only the pipeline seam is built.
- **No public release, no tag, no Nexus upload** — release scope decision belongs to the maintainer; repo-scoped handling only at most (precedent 017).
- **No French/Spanish/German UI this cycle** — i18n EN/PT as repo norm.
- **No new mandatory art assets authored by the agent toolchain** — placeholder strategy disclosed; final art = maintainer decision.
- **No changes to the 34/67 contract history or spec 010 scope** — unrelated debts.

## Success Metrics

- **Playable heirloom crop exists in-game data**: `Data/Crops` entry keyed by heirloom seed ID; seed + produce items registered (tests assert contracts). (baseline: 0 entries; today: nothing registers)
- **Soil coupling measurable & bounded**: heirloom plant applies `+HeirloomPlantBonus` (tunable constant) host-authoritatively; `+HeirloomHarvestBonus` on Healthy-band harvest; both bounded, validated, and MP-pattern-compliant (host applies / farmhand path explicit). (baseline: no such rule exists)
- **Seed-saving loop works and is visible**: heirloom harvest yields seed(s) per an explicit, capped, communicated rule (not silent uncapped RNG); player-facing copy states it. (baseline: impossible today)
- **Tests**: targeted heirloom test files all green; full suite ≥ current 1937 passed / 0 failed (no regressions); `dotnet format --verify-no-changes` clean. (baseline: 1937/0/4)
- **Gate 3**: scenario(s) for the slice run headless to `machine-pass` host-only (honesty R) with soil/crop/log signals; `verified` claims remain `pending`. (baseline: no heirloom scenarios)
- **Pipeline reused**: seam documented so a second crop (Green Manure/Three Sisters) could be added without new code paths beyond registration + constants. (baseline: no pipeline)
- **Honesty**: claims validator passes (if used); no claim promoted beyond host-only machine-pass; D1/SC-heirloom-human remain pending. (baseline: same honesty state as 017)

## Cost of Inaction

- The mod's Y1 agroecology promise stays undeliverable — the crioulo half of the "triângulo completo" (Elena/Joja/Pierre contrast) never exists in play.
- Four named features (Three Sisters, Green Manure, Rotation Journal, CSA) stay blocked on a pipeline that never shipped; the roadmap cannot advance past "tradeoff MVP" honesty.
- The unclaimed Brazilian-agroecology niche (CONAB/Embrapa/MST vocabulary; zero competing mods in EN/PT/ES) stays open for the next alpha (ReGen Valley has already shipped 0.51 with per-tile soil + cover crops despite being seedless).
- The maintainer's time is re-spent re-deciding "what's next" every cycle instead of compounding on shipped value.

## Open Questions

- [NEEDS CLARIFICATION: exact slice size — 1 crop (recommended by research §8.2) vs 3 crops from the briefing; decision deferred to `/speckit-assess-shape` + `/speckit-assess-decide`.]
- [NEEDS CLARIFICATION: distribution channel without Elena — `Traveler`-only vs `Traveler` + condition-gated `SeedShop` entry vs pure seed-saving bootstrap; decide in shape.]
- [NEEDS CLARIFICATION: seed-saving rule shape — capped deterministic return ("saved seed", 1 per harvest, YAGNI-simple) vs chance-based (Multi-Yield-style); harvest seam extension vs new patch; decide in shape.]
- [NEEDS CLARIFICATION: asset placeholder strategy — procedural PNGs (disclosed) vs vanilla-fallback texture pointers; maintainer owns final art.]
- [NEEDS CLARIFICATION: manifest bump/release scope for this cycle — repo-scoped 0.2.0 vs none; maintainer owns tag/publish.]
- [NEEDS CLARIFICATION: agronomic claims final wording after agronomic stream consolidates (nitrogen only via legumes; no superiority claims; Pierre stays valid).]
