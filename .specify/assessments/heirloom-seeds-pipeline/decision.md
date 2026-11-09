# Decision: Heirloom Seeds / Sementes Crioulas Pipeline

- **Slug**: heirloom-seeds-pipeline
- **Decided**: 2026-11-09
- **Verdict**: **go**
- **Artifacts reviewed**: intake.md yes | research.md yes (+ consolidated agronomy stream) | problem.md yes | concept.md yes | raw report `research/2026-11-09-heirloom-seeds-pipeline.md` (commits `31924e5`, `13b92a0`) yes

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | **strong** | Verified, not inferred: zero heirloom/crioulo/seed-saving content exists anywhere in the mod tree (no `Data/Crops` entries, no heirloom constants, no crioulo copy); the roadmap/OBJETIVO refresh (2026-11-09, item 6) names this exact slice as the next 80/20; the Y1 promise and 4 downstream briefings are blocked on it. |
| Evidence strength | **adequate** | Market lens high (no crioulo/heirloom mod in EN/PT/ES/FR/DE; ReGen Valley alpha-3-endorsements; Harvest Seeds–Continued ~354 maintained); technical lens high (decompiled `CropData`, `HoeDirt.plant`, `ShopData`; SMAPI 4.5.2/SDV 1.6.15 live); agronomic lens high (Korpetis 2023, Zeibig 2024, Lei 10.711/2003, CONAB PAA 2026); demand is **latent** (identity/playstyle signal, no explicit requests; no YouTube coverage) — adequate, not strong. |
| Value vs. inaction | **strong** | Inaction = Y1 promise undeliverable, Three Sisters/Green Manure/Rotation Journal/CSA blocked, niche preempted, repeat "what's next" debate. One slice unblocks four briefings and proves the pipeline. |
| Feasibility / appetite | **strong** | Option B credible inside days–weeks at medium appetite: reuses `OnAssetRequested` registration pattern (014/017 precedents), 005/014 seams, `Data/Shops` (`Traveler`, no NPC), `Item.modData` (native MP sync), existing test conventions; headless Gate 3 story via `lr_plant_crop` is available (host-only honesty R). |
| Strategic fit | **strong** | Living Roots pillars 1 (soil is alive), 2 (recompensar, nunca pregar — Pierre stays valid), 5 (acochego seguro; bounded, recoverable), Principle VI (farm-scoped host-authoritative), roadmap item 6 mandatory-next; honest framing matches the project's hard-won agronomy honesty line. |
| Risk posture | **adequate** | Key risks identified and credibly mitigated *inside Option B*: asset placeholder disclosed (procedural art OK for unreleased repo-scoped slice; final art = maintainer); MP path = host-applies/explicit farmhand refusal (agrochemical precedent) — but **live verification remains impossible autonomously** (D1/human E stay `pending`, as in 017); seed-economy flood mitigated by `AvailableStockLimit Player` + capped "saved seed" rule — unproven until playtest (flagged). |

**Unknowns acknowledged, not glossed:**
- No direct player demand signal (stated wants absent); value rests on first-party intent + adjacent behavior.
- `Item.modData` multiplayer per-item sync is documented by wiki snippets (high confidence) but not unit-verified in this repo — carried into spec as validation task, not an assumption of record.
- No gameplay verification ever (Gate 3 machine-pass host-only only; human E and D1 out of autonomous scope).
- Content breadth deliberately sacrificed (1 of 3 Y1 crops) — accepted trade-off, recorded.

## Verdict & Rationale

**go — with Option B (one-crop template slice) as the specified approach.** Every gate criterion clears: problem validity `strong`, evidence strength `adequate` (≥ adequate, ≠ weak/unknown), feasibility `strong`, strategic fit `strong`, risk posture `adequate` with real mitigations, and the concept is well-shaped (bounded scope, non-goals restated, rabbit holes named, recommendation tied to goals/metrics). The honest outcome class for this cycle is a playable crioulo loop plus its pipeline seam with machine-pass evidence and explicit `pending` status for human/D1 — exactly the 017 pattern, with the delta that this slice *adds a real acquisition path in-game* (Traveler special-stock + seed-saving), closing 017's "no in-game acquisition" descope gap for the heirloom vertical.

Option A fails on feasibility and scope (Elena blocked, 3× assets, no end-to-end proof); Option C ships mechanics without the crioulo content (misses the point); Option D violates the explicit selection mandate.

## If needs-clarification

- Not applicable (verdict is `go`). Blocking questions from earlier stages were resolved or converted into validation tasks carried forward; none survive as gate-blockers.

## If go — Handoff to `/speckit-specify`

- **Problem**: the mod ships five loops but no crioulo/heirloom seed acquisition, growth, quality/soil, or seed-saving — the Y1 agroecology promise is undeliverable and four consumer features are blocked.
- **Chosen approach**: **Option B — "Sementes Crioulas: Crioulo Parsnip"** (one-crop template slice; Traveler special-stock distribution; slower 7-day Spring growth; +2 abstract plant bonus / +1 abstract Healthy-band harvest bonus (labeled abstractions); capped, communicated seed-saving; Pierre hybrids untouched; host-authoritative with explicit farmhand semantics; placeholder art disclosed; EN/PT honest copy; no Elena, no 3 crops, no genetics sim, no public release/tag).
- **In scope**: 1 crioulo parsnip crop (seed+produce+crop row), `Data/Objects`+`Data/Crops` registration (`OnAssetRequested`), `Data/Shops` `Traveler` entry (`AvailableStockLimit Player`, `Condition SEASON Spring`), `ModConstants` heirloom block, plant/harvest soil-coupling seams, seed-saving rule (capped, explicit, communicated), i18n EN/PT, full test suite green, new Gate 3 scenario (`heirloom-plant-bonus`, `lr_plant_crop`, host-only honesty R), validation + repo-scoped release bookkeeping only (manifest/tag = maintainer decision, not automatic).
- **Out of scope**: Elena NPC/shop/discount; 2nd/3rd crops; genetics sim; Three Sisters/Green Manure/Rotation Journal/CSA; Joja/monoculture completeness; Nexus/tag/public release; final art; D1/human E claims; sibling debts (spec 010, 34/67).
- **Success metrics**: items registered (tests assert); +2/+1 seams verified (unit + Gate 3 machine-pass); seed-saving works and is visible in copy; suite ≥ 1937 green, format clean; zero claims promoted; docs honest (D1/human E `pending`); pipeline seam documented for reuse.
- **Carried-forward open questions**: exact seed-saving rule shape (capped deterministic vs chance) — resolve in specify from MP-economy + agronomy framing; farmhand path (host-apply message vs explicit refusal) — resolve in specify after reading `ModMessageReceived`/compat handlers; Traveler entry exact `Condition`/`AvailableStock` values — validate against 1.6 GameStateQuery docs + a host-only check; whether Seed Maker handles the crioulo produce (expected yes via crop entry; validate, don't block); release scope for this cycle (0.2.0 repo-scoped vs none — maintainer call, default = no manifest bump this cycle unless maintainer directs); final i18n copy wording with PT glossary (guardiões, sementes crioulas, etc.) for maintainer review.
