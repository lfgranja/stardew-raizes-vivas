# Rotation / Polyculture MVP — Prior Art & Design Considerations (Living Roots)

> **Date:** 2026-11-12  
> **Purpose:** inform future `/speckit.specify` for a Living Roots rotation/polycluture MVP (0.3.0 candidate).  
> **Method:** synthesis of repo constraints/briefs + web research (search/snippets/fetched pages where accessible). Nexus mod pages and some wiki pages were partially inaccessible (HTTP 403) in this environment; claims depending on those are marked **M/L**. This is research input, not a spec and not a verification claim.
>
> **Confidence:** **H** = directly observed (fetched page or repo code/docs), **M** = corroborated by snippets/multiple results but not full primary page, **L** = inference/gap.

## 1) Living Roots constraints the MVP must fit (H, repo)

| Constraint | Evidence (repo) | Implication |
|---|---|---|
| Per-tile soil health is core primitive: `float` 0–100, keyed `(locationName, Point tile)`, sparse cache (near-zero omitted), clamped/validated, `OnTileHealthChanged`, snapshots. | `LivingRoots/Domain/ISoilHealthService.cs`, `LivingRoots/Services/SoilHealthService.cs`, `LivingRoots/Domain/AGENTS.md` (Poor `[0,31)`, Moderate `[31,71)`, Healthy `[71,100]`; measured `0` = Poor, missing = unknown). | Map effects to **soil-health deltas/tier outcomes/feedback** rather than a parallel progression system. |
| Persistence/safety: SMAPI data API, `SanitizePathSegments`, bounds (`MaxLocationsPerSave`/`MaxTilesPerLocation`/`MaxTilesPerSave`), no raw exception logs, snapshot-outside-lock, no `.Result/.Wait()`. | `SoilHealthService.cs`, `LivingRoots/Services/AGENTS.md`, `AGENTS.md`. | Any per-tile history must be **sparse, bounded, save-scoped, tolerant** (separate store/service, same discipline). |
| MP first-class / host-authoritative farm state (Principle VI); first-wins; farm-scoped. **Live D1 not attested.** | Root/`LivingRoots/AGENTS.md`, `ModController` FR-009/A8, `README.md`. | History/bonuses **deterministic and host-owned**; no per-client-only divergent state; spec must state source of truth/conflict rule. |
| No Harmony-first for new logic unless necessary; narrow seams fallback safely. | `AGENTS.md`, `Patches/HarvestPatches.cs`. | Prefer day/plant/harvest events + service rules over patching crop internals for MVP. |
| i18n parity `default,en,pt`; no vanilla `Data/Crops` edits (briefing rule). | `AGENTS.md`, future briefings. | Tag mod crops via `CustomFields`; C# lookup for vanilla family/identity. |
| Roadmap/briefs (unimplemented): **Rotation Journal + botanical families** (per-tile recent-family history, sparse `rotation_history_<saveId>`), **Three Sisters polyculture** (adjacency verdicts derived/cached not persisted, neutral when alone), **green manure**. | `ROADMAP.md`, `specs/future-roadmap/briefings/rotation-journal.md`, `three-sisters-polyculture.md`, `green-manure.md`. | Reuse/validate concepts selectively; **temporal rotation = persisted history**, **spatial polyculture = recomputed from live state** (H, briefs). |

## 2) Prior-art landscape (2026-11-12 snapshot)

| Mod / system | Signal | What it suggests (evidence) | Relevance | MP | Conf. |
|---|---:|---|---|---|---|
| **Harvest Ledger – balance economy** (Nexus 48951) | Listing/snippets surfaced; full page 403 | Records **previous crop category per farm tile**; planting a **different category** on same tile ties to rotation/streak/economy framing. Exact formula/decay/UI/save unknown. | **Closest prior art for per-tile rotation memory** (category-level simplification). | Unknown | **M** |
| **Allellopathy** (Nexus 46221) | 1.6-compatible snippets; page 403 | Neighbor crops help/hinder (companion planting): growth/quality/yield; some tile “soil effects” (auto-water/dry) per snippets; per-crop/season matrix incl. modded crops (e.g. Cornucopia). | **Adjacency/polyculture prior art** (spatial) — high content/balance surface. | Unknown | **M** |
| **ReGen Valley – Realistic Dirt to Soil** (Nexus 39740, robertsvalley) | Fetched listing: v0.51, uploaded 2025-11-28 | Per-tile soil health (baseline 25), cover crops, grazing/beacons, water perk >80/tile, faster growth 70+/90+/tile, yield/quality farm-wide at avg 75+/90+ (author posts); “MP-compatible (early testing ongoing)”. | Demand for per-tile soil metrics + cover/rotation-adjacent practices; split **per-tile vs farm-average** effects; MP early-risk signal. | Early MP testing | **H** (fetched claims) |
| **Polyculture Needs More Crops** (Nexus 26437) | v1.0.0 2024-07-28 | Extends vanilla Polyculture/Monoculture achievement lists via CP/GMCM; no mechanics. | Naming confusion only (not gameplay prior art). | N/A | **H** |
| **Internal briefs** (LR) | Repo | Rotation: family enum + per-tile recent-family history (depth 2–3), sparse store, bonuses via soil/quality, no vanilla edits. Three Sisters: 8-way adjacency, recomputed/cached, new heirloom crops via `CustomFields`, neutral when alone. | Feasible split (temporal persisted vs spatial recomputed); spikes flagged (hooks/balance/MP). | Host-authoritative intent | **H** |

**Takeaway:** direct 1.6 “rotation by tile history” prior art is **thin but real** (Harvest Ledger, M). Adjacency (Allellopathy) and soil+cover (ReGen Valley) are better evidenced but broader scope. No verified turnkey pattern combining **LR per-tile soil health + persisted rotation history + host-authoritative MP** (L).

## 3) Agroecology vs game abstraction (honest framing)

- **Rotation (real):** breaks pest/disease/weed cycles, balances nutrients, improves soil/yields over multi-year horizons. **Game abstraction:** track previous crop **family/category** per tile; reward switching / neutralize repeats via soil-health or quality/growth modifiers; compress to 28-day seasons. State “inspired by rotation principles, not a simulation.” (H principles, M/L exact excerpts)
- **Polyculture/companions:** spatial diversity can aid partitioning/pest suppression/microclimate but outcomes are species/context-specific (not universally positive). Prefer small explicit combos or capped bonuses; avoid “always improves.” Three Sisters: complementarity plausible but not simple guaranteed mutual benefit/in-season N transfer (H, repo brief). (M)
- **Green manure/cover crops:** biomass/N/protection when timed; abstract as occupy tile → incorporate → bounded soil-health boost + history tag. (M)

## 4) MVP design constraints & questions (fit per-tile soil health)

1. **Axis:** pick **one** — **temporal rotation** fits best (history → soil delta) and aligns with briefs; adjacency needs crop scans/content matrices (risk). (H)
2. **Granularity:** exact crop vs **family/category** vs vanilla category. Harvest Ledger signals category (M); LR brief proposes botanical family enum + vanilla-ID lookup in C# (H). Default-safe: small set + `Unknown` neutral.
3. **State:** **last/last-N families per tile+location** (sparse) in a **separate bounded store/service** (not overloading soil dict), same sanitization/limits discipline. Rotation needs persisted history. (H)
4. **Timing:** record/apply on plant/day-start/harvest — must be deterministic, MP-idempotent, aligned with decay/compost timing (avoid double-apply). (H)
5. **Effect channel:** **soil-health delta** native/easy to persist/explain (MVP-friendly) vs soil+quality/growth (hook/Harmony risk). (H)
6. **Neutral vs penalty:** repeat = **no bonus** (forgiving) vs penalty (stacks with decay conceptually; cap/communicate). (M/L balance)
7. **Scope:** Farm + Greenhouse first? Ginger Island/buildings/mod locations mirror existing validity patterns. (H)
8. **Data:** key `(saveId, locationName, "X,Y")`; payload ring depth **1–2** `{familyId,...}` sparse; migration-tolerant reader (ignore unknown, cap, never block load). (H)
9. **Mapping:** fixed vanilla seed→family/category table in code/data, `Unknown` fallback; **no vanilla `Data/Crops` edits**. Modded: `Unknown` neutral or opt-in `CustomFields` convention (e.g. `LivingRoots.Family`). (H/M)
10. **MP:** host computes/commits on canonical events; farmhands do not fork; sync/reconcile per existing patterns; deterministic (no unsynced RNG). Design/automated coverage only until D1 attested. (H)
11. **Balance/UX:** small capped bonuses coherent with compost/mulch/decay/heirloom/Joja; visible cause→effect (tooltip/existing feedback), no spreadsheet UI for MVP. (M)

## 5) Suggested MVP scope for `/speckit.specify`

### In scope (minimal, testable)
- **Temporal rotation by family/category** using per-tile recent history (depth 1–2) → **bounded soil-health bonus** (and/or explicit no-bonus on repeat) at one deterministic trigger (plant or day-start — decide in spec).
- **Coverage:** Farm + Greenhouse (Island farm only if low-risk parity) — explicit.
- **Persistence:** sparse, bounded, save-tolerant per-save history store + migration-safe reader; host-authoritative.
- **Mapping:** built-in vanilla mapping + `Unknown` neutral; optional `CustomFields` read for mod-added crops (no vanilla edits).
- **Feedback:** minimal (tooltip line/existing feedback/message) — **no Rotation Journal UI**.
- **Verification:** unit/property tests (mapping, verdict, bounds/sparsity, idempotency, MP model as testable); Gate 3 scenario plan (host-only machine-pass acceptable initially).

### Out of scope (defer)
- Adjacency/companion matrices (Allellopathy-style), yield/quality/water side effects per combo.
- Rotation Journal UI / browsable history / auto-detected fields.
- Growth-speed/quality via Harmony unless proven safe seam (spike).
- Pest/disease, green manure incorporation, animal/grazing integration.
- Vanilla `Data/Crops` edits, hard GMCM dependency, broad modded-crop matrices.
- Claims of agroecological simulation accuracy or live-MP convergence without evidence.

### Success criteria (candidate)
- Rotating to a **different family/category** on a previously-used tile yields a **predictable, capped soil-health benefit** consistent with LR rules; repeat yields **no bonus** (or specified small penalty) — deterministic and save/load-stable.
- History **sparse, bounded, host-authoritative**, no regression to existing soil-health saves/performance.
- Explainable in one tooltip sentence; framed as abstraction of rotation benefits.

## 6) Sources & gaps

- **H (repo, 2026-11-12):** `README.md`, `AGENTS.md`, `LivingRoots/{AGENTS.md,Domain/AGENTS.md,Services/AGENTS.md}`, `LivingRoots/Domain/ISoilHealthService.cs`, `LivingRoots/Services/SoilHealthService.cs`, `ROADMAP.md`, `docs/GDD-living-roots.md`, `docs/0.2.0-beta-prep.md`, `specs/future-roadmap/briefings/{rotation-journal.md,three-sisters-polyculture.md,green-manure.md}`.
- **H (fetched 2026-11-12):** ReGen Valley (Nexus 39740) https://www.nexusmods.com/stardewvalley/mods/39740 (v0.51, 2025-11-28).
- **M/L (snippets/blocked 403):** Harvest Ledger 48951, Allellopathy 46221, Polyculture Needs More Crops 26437 (v1.0.0 2024-07-28). https://www.nexusmods.com/stardewvalley/mods/48951 https://www.nexusmods.com/stardewvalley/mods/46221 https://www.nexusmods.com/stardewvalley/mods/26437
- **M (principles):** Wikipedia *Crop rotation*, Rodale Institute *Crop Rotations*, NCAT ATTRA/UMN companion planting — principle-level framing (some pages fetch-limited).

**Gaps:** no verified Harvest Ledger schema/MP/exact bonuses; no evidenced non-EN rotation mod found in this pass (L coverage). Manual browser check of Nexus 48951/46221 and any public source repos would raise confidence.
