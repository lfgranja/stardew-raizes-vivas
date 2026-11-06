# Research: 017 Joja / Agrochemical Tradeoff (MVP)

**Phase 0 output** for `specs/017-joja-agrochemical-tradeoff/plan.md`  
**Date**: 2026-11-06  
**Spec**: `specs/017-joja-agrochemical-tradeoff/spec.md` (clarified C1–C4)  
**Assessment**: `.specify/assessments/joja-agrochemical-tradeoff/` (GO, Option A)

## Decision summary

| Topic | Decision | Rationale | Alternatives considered |
|---|---|---|---|
| Application path | **Mod-intercepted** held-item interaction (controller/service pattern), not vanilla fertilizer slot/hook | Lowest risk; reuses compost/mulch/controller patterns; avoids Harmony fertilizer commit/probe ambiguity and `HoeDirt.fertilizer` composition with 014/015 tier heuristics | Vanilla `HoeDirt.fertilizer` interop + Harmony on `HoeDirt.plant`/fertilizer commit path — deferred; requires exact-build decompile gate |
| Short-term gain (C1:A) | **Confirmation/proxy only**: immediate attributable shortcut confirmation; **no** mechanical buff to 014/015 soil-driven quality/growth, **no** vanilla fertilizer-tier interaction | Prevents double-counting; keeps composition testable; temptation validated by playtest | Small explicit mechanical buff outside 014/015 paths — follow-up only with written no-double-counting rule; vanilla fertilizer interop — out of MVP |
| Unknown/`NaN` baseline (C2:B) | Unmeasured/`NaN` soil → explicit neutral baseline **`MaxSoilHealth` (100)**, compute `postPenalty = clamp(baseline - penalty, Min, Max)`, write **explicit measured** result via absolute `SetSoilHealth` | Satisfies FR-006 (no invisible no-op); avoids `UpdateHealth` negative-delta-on-unknown drop/misleading behavior; preserves Unknown-vs-measured semantics | Treat unknown as 0 — unfair/misleading; refuse when unknown — poor UX/MVP blocker; negative `UpdateHealth` from unknown — unsafe given `SoilHealthService` sparse/drop behavior |
| Penalty magnitude (C3:A) | Provisional **`P=10`** soil-health points, tunable in central constants, bounded 0–100; playtest/tier-boundary review (31/71) required | Legible and recoverable starting point (~1 compost equivalent delta class) without punitive cliff by default; evidence supports incremental/recoverable patterns, not universal number | `P=5` trivial; `P=15` stronger edge crossings; `P>=20` higher cozy-cliff risk |
| Multiplayer authority (C4:A) | **Host-authoritative MVP**. Unsupported farmhand attempts refused **before** soil mutation, item consumption, or false-success feedback; host success uses per-tile lock + FR-009 snapshot broadcast | Constitution VI; `HostAtomicLockService` is in-process only; existing compost flow consumes/mutates locally — farmhand path must not execute service/mutation/consumption | Farmhand intent/ack host-mediated commit — correct but D1/follow-up; local farmhand apply + snapshot overwrite — unsafe desync/item loss/false success |

## Codebase findings (internal)

### Integration points
- **`LivingRoots/ModEntry.cs`**: sole composition root; `WireCompositionRoot` constructs services and passes optional dependencies to `ModController` (shared `HostAtomicLockService`, `IPlayerProvider`, translation/helper seams, soil/save providers). `OnAssetRequested` registers `Data/CraftingRecipes`, `Data/Objects` (compost/mulch), textures, `Data/BigCraftables`, `Data/Machines`; **no `Data/Shops` edit today**. 017 should follow same registration/wiring discipline and avoid `Data/Shops` unless acquisition UX is explicitly scoped.
- **`LivingRoots/Controllers/ModController.cs`**: right-click `OnButtonPressed` intercept resolves held item via `IPlayerProvider` and routes to compost/mulch patterns. Compost path uses `TryApplyCompostWithLock` (lock, fail-closed when lock missing, conflict refusal, service call, host broadcast after success). `BroadcastSoilHealthSnapshotIfHost` sends `SoilHealthSyncMessage` (`SoilHealthSyncMessageType`) after host DayStarted soil processing and successful locked compost apply; farmhands apply via `OnModMessageReceived` only for matching save and only when not host. `IsMainPlayerCheck`/`GetCurrentDay` are injectable for tests.
- **Constants/i18n**: `LivingRoots/Constants.cs` (`ModConstants`) is the single source for IDs, translation keys, bounds, compost/mulch tunables. `LivingRoots/i18n/default.json`, `en.json`, `pt.json` exist; `pt.json` currently lacks mulch key parity — 017 must keep `default/en/pt` parity for touched/new keys.

### Service/domain patterns
- **Compost**: `ICompostApplicationService.TryApplyCompost(GameLocation, Vector2)` returns `bool`; service validates, resolves NaN to `0f` for additive restoration arithmetic, applies `UpdateHealth(+RestorationAmount)`, consumes item, shows feedback. Controller wraps with lock/broadcast.
- **Mulch**: `IMulchApplicationService.TryApplyMulch(Farmer, GameLocation, Vector2, out string messageKey)` returns `bool + messageKey`; controller translates/displays refusal.
- **Recommendation for 017**: use a new domain interface in `Domain/Interfaces/` and implementation in `Services/`, wired only in `ModEntry`. Prefer `bool + messageKey` (mulch-style) or equivalent explicit result so controller owns refusal display and host/farmhand gating is unambiguous. Service must not be the authority gate for MP.

### Soil mutation invariants (C2 critical)
- **`ISoilHealthService`**: `GetSoilHealth` returns `float.NaN` for valid missing tile (unknown, not 0); `TryGetSoilHealth` distinguishes missing vs stored; `SetSoilHealth`/`UpdateHealth` clamp and fire `OnTileHealthChanged`.
- **`SetHealthInternal`**: drops near-zero results only when clamped from non-explicit inputs (`value != 0f`), preserving explicit measured `0f` as Poor. `UpdateHealth` uses absent current as `0f` for arithmetic; a negative delta from unknown can clamp/drop and leave tile unmeasured — therefore **017 penalty path MUST compute baseline/result and call `SetSoilHealth` with absolute measured value**. Never `UpdateHealth(location, tile, -penalty)` from unknown.
- **Existing NaN handling precedent**: compost resolves NaN to `0f` before additive restoration and writes a concrete result; decay skips unknown tiles when no actual decay applies — different from a deliberate player action that FR-006 requires to record a measured result.

### No double-counting boundaries
- **Harvest**: `HarvestPatches` reads `HoeDirt.fertilizer.Value` and infers vanilla fertilizer tier by string heuristic; uses pure `IHarvestQualityService` seam; composes post-vanilla/non-suppressing. 017 MUST NOT write `HoeDirt.fertilizer`, MUST NOT patch fertilizer application, MUST NOT add harvest/growth buffs.
- **Growth**: `ModController` host DayEnding flow applies tier multipliers from soil health (`<31` slower 1.15, `>=71` faster 0.85, middle 1.00) after `ResetPhaseDays()` idempotency pattern. 017 penalty is a normal soil-health mutation only; no direct `Crop.phaseDays` manipulation.
- **Quality**: `HarvestQualityService` maps Poor `<31` force quality zero/no boosts, Moderate `<71` vanilla/no boosts, Healthy `>=71` additive boosts (`+0.08`/`+0.10`). 017’s soil change may affect later outcomes through existing rules — intended consequence, not double-counting.

### MP/lock/sync
- **`HostAtomicLockService`** is in-memory, in-process, shared within one mod instance; first-wins per `(location,tile)` with farmer holder. It is not cross-client arbitration.
- **FR-009**: host broadcasts full soil snapshot (`SoilHealthSync.Create/Apply`) on DayStarted and after successful locked compost apply; farmhand applies only matching save and not host.
- **Gap for 017**: compost controller path does not explicitly refuse farmhands before lock/service; compost service consumes item after mutation. 017 MUST add explicit host/farmhand support refusal before service execution/mutation/consumption.

## External findings (high-reliability, access-constrained where noted)

### SDV 1.6 content patterns
- **Objects**: `Data/Objects` is unqualified string-keyed object data; objects are `(O)`/`ItemRegistry.type_object`. Relevant fields include name/display/description, type/category/price, texture/sprite index, context tags. Canonical: https://stardewvalleywiki.com/Modding:Objects (fetched via curl/mirror in-session; `webfetch` 403 observed). **Confidence: high**.
- **Shops**: `Data/Shops` edits target shop IDs and `Items` entries; `ItemId` can be qualified/unqualified. Exact Joja shop ID must be verified during planning/implementation. https://stardewvalleywiki.com/Modding:Shops . **Confidence: high for pattern, medium pending exact Joja ID confirmation**.
- **IDs/i18n**: SDV 1.6 uses qualified item IDs (`(O)...`) and unqualified data keys; SMAPI translations use `i18n/*.json` with `default.json` fallback and tokens. https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6 , https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation . **Confidence: high**.
- **Asset gap**: MVP requires no mandatory new assets; if a custom `Data/Objects` sprite is needed, planning must choose a minimal explicit strategy (optional asset, existing texture, or scoped exception). **Confidence: medium (design gap)**.

### Mod-intercepted application (no vanilla fertilizer hook)
- SMAPI input/events support button/cursor interception (`ButtonPressed`, cursor tile). Intercepting mod-owned item use allows validate → refuse/apply → consume sequencing without vanilla fertilizer probe/commit ambiguity. https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events . **Confidence: high for API capability; medium for exact 1.6 item-use suppression edge cases**.
- Harmony/vanilla fertilizer hooks increase compatibility/probe risk and are out of MVP. Canonical caution reference: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony (access constrained in-session). **Confidence: medium-high**.

### Multiplayer authority/refusal
- SMAPI multiplayer APIs expose host/peer info and `SendMessage`/`ModMessageReceived`; `GetActiveLocations` reflects sync scope. Host-authoritative request/validate/apply is the safe pattern for shared-state mutation. https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer , https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals . **Confidence: high/medium-high**.
- General authority principle: clients should not unilaterally commit shared mutations; validate at authority and reconcile explicitly. https://gafferongames.com/post/what_every_programmer_needs_to_know_about_game_networking/ . **Confidence: high (general), applied by analogy**.
- Refusal UX: explicit, proximal, non-blaming, actionable, preserve user effort/inventory on refusal. https://www.nngroup.com/articles/error-message-guidelines/ , https://www.nngroup.com/articles/ten-usability-heuristics/ . **Confidence: high**.

### Legible tradeoff / cozy constraints
- Temporal discounting makes immediate payoff salient; educational/cozy designs need immediate attributable costs/feedback, recoverability, and non-punitive framing. Farias et al. 2021 https://www.mdpi.com/2071-1050/13/13/7130 (access constrained), Lefebvre & Stenger 2020 https://pmc.ncbi.nlm.nih.gov/articles/PMC6968839/ , Hollweck et al. 2026 https://www.frontiersin.org/journals/education/articles/10.3389/feduc.2026.1813464/full , Chen et al. 2024 https://pmc.ncbi.nlm.nih.gov/articles/PMC11439678/ , Bickel et al. 2014 https://pmc.ncbi.nlm.nih.gov/articles/PMC3858579/ . **Confidence: medium-high directionally**.
- Penalty magnitude/recoverability patterns favor incremental, legible, recoverable setbacks and telegraphed thresholds; exact numbers require playtest. Krzywinska et al. cozy framing https://pmc.ncbi.nlm.nih.gov/articles/PMC13197054/ , Nabong & Opdyke 2024 https://iopscience.iop.org/article/10.1088/2515-7620/ad6f75 , Das et al. 2025 https://ecologyandsociety.org/vol30/iss4/art5/ , Carswell et al. 2025 https://www.nature.com/articles/s44264-025-00098-6 , Sajjadi et al. 2022 https://www.frontiersin.org/journals/environmental-science/articles/10.3389/fenvs.2022.957204/full , SEGAE https://www.mdpi.com/2071-1050/12/11/4351 (metadata-level), FORCES https://doi.org/10.1016/j.agsy.2023.103782 . **Confidence: medium-high for patterns; low for numeric tuning**.

### Unknown-state / no silent no-op
- Explicit unknown/special-case handling, fail-fast/no-silent-no-op, and concrete post-state after mutation are established software/state practices. Terraform lifecycle https://github.com/hashicorp/terraform/blob/main/docs/resource-instance-change-lifecycle.md , Fowler Special Case https://martinfowler.com/eaaCatalog/specialCase.html , Fowler Fail Fast https://martinfowler.com/ieeeSoftware/failFast.pdf , KEVOS https://kevos.com/reados/collections/sql-queries-and-data-correctness/designing-database-triggers-with-predictable-effects/ , StateWORKS https://www.stateworks.com/technology/handling-unknown-undefined-states . **Confidence: high-medium by analogy**.

## Planning gaps (resolved by convention or explicit follow-up)

| Gap | Plan disposition |
|---|---|
| Exact Joja `Data/Shops` ID / acquisition UX | **Out of MVP implementation unless separately scoped**; spec allows Joja-framed acquisition details to be finalized later. If added, verify vanilla 1.6 shop ID before editing `Data/Shops`. |
| Custom object sprite with “no mandatory assets” | Prefer MVP path that avoids new required PNG/SFX. If `Data/Objects` registration is used, planning must choose a minimal safe texture strategy consistent with FR-013. |
| Exact 1.6 input suppression/consumption edge cases | Validate in implementation tests/manual spike; contracts require no consumption/false success on refusal. |
| Neutral baseline `MaxSoilHealth` (100) vs gameplay expectation | Accepted clarification C2:B; planning must confirm no conflict with existing semantics and document in tests. |
| `pt.json` mulch parity | Pre-existing gap; 017 must add parity for new 017 keys; fixing mulch keys is optional cleanup if touched. |
| D1 intent/ack, Harmony fertilizer hook, monoculture | Explicitly deferred/gated. |
