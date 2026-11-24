# Idea Intake: Joja / Agrochemical Tradeoff (destructive soil-decision slice)

- **Slug**: joja-agrochemical-tradeoff
- **Created**: 2026-11-05
- **Source**: pasted text (user request in conversation; builds on repo GDD/roadmap/briefing context)
- **Type**: new-capability

## Idea (as captured)

> “Joja/Agrochemical tradeoff” (mecânica destrutiva/decisão, escopo controlado, first-pass tunable)
> O que é: especificar (não implementar ainda) o primeiro slice de decisão com custo real do GDD: um tradeoff químico/industrial (ex.: fertilizante/insumo Joja-like) que dá ganho curto prazo mas degrada/compromete saúde do solo (e, por consequência, qualidade/crescimento no loop já existente), com regras host-authoritative, persistência/validação, multiplayer-safe, e critérios de Gate 3. Escopo deliberadamente pequeno (1 item/regra + integração com SoilHealth/decay/harvest existentes), TDD e constantes em ModConstants.
>
> Por que é o maior avanço com menor esforço agora
> Fecha o loop de decisão que falta (alto valor de produto): 014/015 já entregam consequência positiva (solo vivo → qualidade/crescimento) em machine-pass. O GDD pede que o solo seja um sistema com escolhas; sem um “o que destrói/compensa”, o manejo tem pouco risco estratégico. Um slice destrutivo torna o sistema consequente e diferencia de mods de buff simples — maior avanço percebido por unidade de design.
> Baixo esforço relativo (especificação, não runtime): escrever um spec bem delimitado é barato e desbloqueado agora (não depende de D1/humano E/ambiente farmhand). Aproveita infraestrutura já pronta (saúde por tile, tiers 31/71, hooks de colheita, decay, sync FR-009/A8, runner/evidência).
> A reconciliação 014↔015 de maior ROI já foi feita: Phase 11 (T061–T065) está concluída; o que resta em 015/014 é sobretudo D1/humano/T110/pest — alto custo operacional, não “pequena ação”. Reanalisar de novo agora tem retorno decrescente.
> Risco controlado se bem escopado: first-pass tunable, sem assets novos obrigatórios, sem reescrever specs históricas, sem promover evidência indevidamente; integração clara com o que já existe.

**Related in-repo context (captured, not endorsed here):** `docs/GDD-living-roots.md` frames Joja/chemical inputs as short-term boost with soil-health cost (e.g., “Joja −5%”, “fertilizante Joja dá boost imediato mas −5% saúde”; `Min 0 / Max 100` context) and the “o que DESTROI” loop (solo nu, monocultura, Joja). `ROADMAP.md` lists Joja Fertilizer/monoculture as unimplemented and a candidate next slice. An early, non-promoted briefing exists at `specs/future-roadmap/briefings/joja-fertilizer.md` (concept: Joja Mart item, −5 soil health per application, `!compostable_item`, Elena hates it, `Data/Objects`/shop append pattern; **not a spec** — do not promote without `/speckit.specify`). `LivingRoots/Constants.cs` (`ModConstants`) already centralizes tunables (e.g., restoration/decay/tier edges/quality boosts) — new constants would follow that pattern if specified.

## Restated

Specify (not implement) a small, first-pass-tunable Living Roots mechanic where using an industrial/Joja-like agrochemical input gives a short-term benefit but applies a host-authoritative, validated, multiplayer-safe penalty to per-tile soil health, integrating with the existing soil-health/decay/harvest/quality loop and defining Gate 3-verifiable criteria.

## Origin & Context

- **Raised by**: [NEEDS CLARIFICATION: explicit requester/owner beyond “maintainer/user in this session”]
- **Trigger**: Post–spec 014/015 progress (014 harvest-quality loop in `machine-pass` host-only, **not verified**; 015 Phase 11 reconciliation T061–T065 done; remaining 014/015 work largely D1/human/T110/pest). Seeking the highest-advance/lowest-effort next step; rationale captured above points to specifying the missing destructive/decision loop.
- **Project constraints (from repo guidance/constitution):** .NET 6/SMAPI 4.x, DDD layering, `ModEntry.cs` sole composition root, async-only I/O, `ModConstants` single source of truth, multiplayer first-class (farm state host-authoritative/farm-scoped, atomic per-tile/first-wins), TDD/property-based where applicable, Gate 3 requires 3 signals and honest `machine-pass` ≠ `verified`.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: What exact “short-term gain” is in scope for MVP — vanilla fertilizer behavior emulation/interop, custom growth/quality buff, yield, or a minimal proxy — and how it composes with existing 014/015 harvest-quality/growth logic without double-counting?]
- [NEEDS CLARIFICATION: Is the item a new custom `Data/Objects` fertilizer sold only at Joja (per briefing/GDD framing), a hook on an existing vanilla/Joja-sold input, or another acquisition path — and what are the 1.6 `HoeDirt`/fertilizer API implications?]
- [NEEDS CLARIFICATION: Penalty model — immediate −X on application, delayed/decay-like, per-harvest, stacking/cooldown, floor/clamp, interaction with compost/mulch/decay and tier boundaries (31/71) — all first-pass tunable; which invariants must hold?]
- [NEEDS CLARIFICATION: Multiplayer authority flow for application — host validates/applies soil-state mutation, farmhand intent messaging, idempotency/first-wins per tile, and relation to existing FR-009/A8 soil-health sync?]
- [NEEDS CLARIFICATION: Gate 3 scenario design (3 signals) given host-only runner today and D1 being blocked — what can be machine-passed host-only vs. what requires human/D1 attestation?]
- [NEEDS CLARIFICATION: UX/feedback and localization scope for MVP — floating text/i18n keys/assets — consistent with “no new assets obligatory / controlled scope” intent?]
- [NEEDS CLARIFICATION: Relationship to “monoculture” (other destructive loop named in GDD/roadmap) — explicitly out of scope for this slice or partially included?]
- [NEEDS CLARIFICATION: Balance/tuning targets (magnitude, stackability, economic price) and how they will be validated without claiming verified gameplay balance?]

## Research context (captured, high-reliability; gaps noted)

> Purpose: inform later stages; **no decisions made here**. Sources accessed 2026-11-05; some pages blocked in this environment (noted). Subagent web research used high-reliability targets (official wiki/SMAPI/GitHub/docs) where accessible.

### Vanilla Stardew/Joja/fertilizer (official wiki context, via research)
- Vanilla 1.6 fertilizers include Basic/Quality/Deluxe Fertilizer, Basic/Quality/Deluxe Retaining Soil, Speed-Gro/Deluxe/Hyper Speed-Gro, and Tree Fertilizer (category fertilizer, `-19` in modding item context). Effects, “when usable”, and acquisition vary (Pierre’s, Oasis, Qi, crafting, etc.). Source context: https://stardewvalleywiki.com/Fertilizer (captured via research; canonical reference).
- **No “Joja Fertilizer” named product exists in vanilla.** JojaMart sells no fertilizers in vanilla 1.6; fertilizers like Basic/Quality Fertilizer, Retaining Soils, Speed-Gro/Deluxe Speed-Gro are sold at Pierre’s (with rules) but not at JojaMart. Source context: https://stardewvalleywiki.com/JojaMart , https://stardewvalleywiki.com/Fertilizer . **Uncertainty:** absolute negative confirmed against fetched wiki sources; verify against 1.6 `Data/Objects` if implementation proceeds.
- 1.6 modding: `HoeDirt.fertilizer.Value` can be `null` and IDs may be qualified/unqualified; fertilizer logic is partly centralized into patchable `HoeDirt` methods (e.g., apply/validate/effect helpers per migration notes). Some placement/error logic may remain elsewhere (e.g., `Utility.tryToPlaceItem` noted in migration context). Sources: https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6 , https://stardewvalleywiki.com/Modding:Items . **Gaps:** several wiki modding pages returned HTTP 403 in this environment; exact 1.6 method signatures/commit paths were **not decompile-verified** in-session.

### SMAPI 1.6 hooks & multiplayer (verified/inferred; gaps noted)
- SMAPI `IWorldEvents` (verified from source) does **not** expose a dedicated fertilizer-applied event; `TerrainFeatureListChanged` is unlikely to catch in-place `HoeDirt` fertilizer mutation. Source: https://raw.githubusercontent.com/Pathoschild/SMAPI/develop/src/SMAPI/Events/IWorldEvents.cs ; events overview: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events (body not fetched due to 403 here).
- Reliable detection/application side-effects typically require Harmony on the actual fertilizer application/validation path (centralized `HoeDirt` methods in 1.6 are candidates) plus MP-safe handling. Harmony tutorial: https://stardewmodding.wiki.gg/wiki/Tutorial:_Harmony_Patching . **Gap:** exact 1.6 patch target/signatures require decompile verification.
- Multiplayer/custom mod state: vanilla sync does not cover custom per-tile soil semantics; host-authoritative apply + `SendMessage`/`ModMessageReceived` + idempotency/first-wins/reconciliation is the safe pattern (consistent with Living Roots constitution/FR-009 patterns). Canonical MP ref (body not fetched due to 403): https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer .

### Ecosystem mods (adjacent; access-limited)
- **ReGen Valley (Nexus #39740):** regenerative soil-health system (per-tile metrics/bonuses and farm-average thresholds described in accessible excerpts); emphasizes regenerative practices. Accessible excerpts did **not** show an explicit Joja-vs-organic or “synthetic fertilizer causes soil degradation/pollution” mechanic, but Nexus direct access returned **HTTP 403** here (content reviewed via proxy excerpt; incomplete). **Treat as context, not disproven/confirmed.** Source: https://www.nexusmods.com/stardewvalley/mods/39740 . Repo prior refs: `research/2026-10-roadmap-feasibility-and-agronomy.md`.
- **Mulch (#7457), Watering Path V2 (#37996), Ultimate Fertilizer (#21318):** adjacent (soil cover, moisture/drought/quality, fertilizer QoL/tuning) — **not** the same as a chemical/Joja tradeoff with custom soil-health penalty. GitHub repo search queries tried returned 0 indexed matches (not proof of absence).
- **Living Roots differentiation (if specified/implemented later):** per-tile 0–100 host-authoritative deterministic soil state, decay/compost/mulch/harvest-quality integration, sparse persistence, Gate 3 evidence discipline — distinct from crop-as-mulch workarounds, moisture-only systems, or fertilizer QoL mods.

### Access/policy notes
- Direct fetches blocked in-session: Nexus mod pages (HTTP 403/Cloudflare in tool/subagent contexts), some `stardewvalleywiki.com` modding pages (HTTP 403). Research used official SMAPI/GitHub source where possible and recorded gaps; proxy-rendered excerpts were used only to identify likely content and are **not** treated as canonical page renders.
- `npx skills find` (2026-11-05) returned no matching installable skill for the specific query attempted; no external skills installed (`npx skills use` not required beyond discovery). Background subagents were used for parallel web research.
