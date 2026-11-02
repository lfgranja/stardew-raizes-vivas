# Problem Definition: Adding Lib.Harmony (0Harmony) to intercept Crop.harvest for harvest-quality seam

- **Slug**: harmony-harvest-quality-seam
- **Created**: 2026-11-01
- **Inputs used**: intake.md (yes) | research.md (no — not yet produced; claims below grounded in the spec 014 assessment artifacts, the T007 spike result, and the SMAPI Harmony guidance cited in prior stages)

## Problem Statement

The mod's central promise — "Living Soil = faster growth, lower quality on Poor, higher quality + extra harvest on Living" — cannot be delivered for the harvest half without intercepting `StardewValley.Crop.harvest`, which is a `public virtual` method. The mod's established posture ("vanilla-first, no Harmony unless necessary," Constitution V, SMAPI best practices) has no public-field workaround for a method call, so the maintainer must either accept a Harmony dependency or leave the central promise half-undelivered. The cost of choosing wrongly (or not deciding) is either a broken cross-platform/reviewability posture or a feature that ships nothing observable at harvest.

## Affected Users & Stakeholders

- **Users — players**: receive no harvest-time payoff for investing in soil health today (growth speed only); cannot attribute harvest quality to soil health. [source: spec 014 problem.md; ROADMAP.md "growth + protection shipped; quality/harvest still missing"]
- **Users — co-op farmhands**: tier effects must reach farmhands deterministically; a Harmony patch on a synchronous harvest path risks desync if not host-authoritative. [source: Constitution Principle VI; spec 014 FR-009, A8]
- **Stakeholders — maintainer (lfgranja)**: owns scope, Gate 3 verification, the "no Harmony unless necessary" posture, and decides whether to accept the dependency. [source: spec 014 problem.md stakeholders; AGENTS.md]
- **Stakeholders — contributors/reviewers**: expect DDD layering, TDD, FsCheck invariants, and documented divergence rules; an undocumented Harmony hook breaks reviewability. [source: AGENTS.md; constitution I, IV; problem.md "Users — modders/contributors"]
- **Stakeholders — other mod authors (ReGen Valley, etc.)**: Harmony patches may conflict with or be blamed for other mods' crashes; SMAPI warns players about stability. [source: SMAPI Harmony guide (stardewvalleywiki.com, Wayback 2025-11-18)]
- **Stakeholders — Android players**: Harmony is higher risk on Android (SMAPI rewriting limitations). [source: SMAPI Harmony guide]

## Goals

- Deliver the harvest half of the mod's central promise (Poor → no stars; Living → silver/gold/iridium + extra harvest; Medium → vanilla byte-identical) using a mechanism that actually intercepts `Crop.harvest`.
- Keep the "vanilla-first, no Harmony unless necessary" posture intact wherever a non-Harmony path exists (growth modifier already uses public fields; only harvest interception needs Harmony).
- Preserve testability: all tier logic must be verifiable without a game runtime (xUnit/FsCheck), with the Harmony patch itself verified only by Gate 3 in-game.
- Preserve cross-platform safety and reviewability: code API (not annotations), postfix preferred, try/catch with fallback to original, `EnableHarmony=true` (SMAPI-bundled Harmony).
- Document the decision so it is discoverable (no formal ADR exists today; promote to `docs/adr/`).

## Non-Goals

- Replacing the public-field growth-modifier path with Harmony (growth already works without Harmony; do not regress it).
- Using Harmony annotations or `PatchAll` (SMAPI cannot rewrite them — explicitly discouraged by SMAPI guidance).
- Using transpilers unless there is no alternative (higher fragility, harder for other modders to help).
- Delivering the pest system or "new interactions" (separate specs, out of scope for 014).
- Numerical balance tuning beyond first-pass values (deferred to playtest).
- Live 2-player verification beyond the existing D1 roadmap obligation.

## Success Metrics

- `Crop.harvest` is intercepted and tier rules apply: Poor → `Quality = 0`; Moderate → byte-identical vanilla roll; Healthy → `+0.08` on `p` and `+0.10` on `ExtraHarvestChance` (SC-001–SC-004, SC-009).
- All tier logic is exercisable by xUnit/FsCheck without a game runtime (seam `HarvestQualityService`), so TDD (Principle IV) is satisfied for the logic even though the patch itself is runtime-only.
- Gate 3 in-game session captures the 3 signals (observation + save diff + SMAPI log) for the growth+quality loop, with a second player for multiplayer verification (SC-008, D1) — this is the only attestation of "verified" status.
- `dotnet build -c Release` and `dotnet test -c Release` pass with 0 failures; `dotnet format --verify-no-changes` clean (SC-007).
- No regression of the "no Harmony unless necessary" posture: Harmony is used only for `Crop.harvest` interception; growth modifier remains public-field.
- ADR (or equivalent) documents the decision and the "Harmony only as last resort, behind a testable seam" rule.

## Cost of Inaction

If Harmony is never adopted, the central promise stays half-delivered: soil health drives growth speed only, and the player has no harvest-time payoff for investing in Living Soil (silver/gold/iridium, extra harvest) nor visible consequence for neglecting it (no stars on Poor). The loop build → protect → harvest remains open; the competitor ReGen Valley keeps the "quality at high soil health" differentiator. The mod risks being perceived as a diagnostic overlay with a compost buff rather than an agroecological system, and the 31 design briefings for unbuilt features lose their anchor (soil health as the currency of progression). [source: spec 014 problem.md; ROADMAP.md "growth + protection shipped; quality/harvest still missing"]

If Harmony is adopted recklessly (annotations/`PatchAll`, no try/catch, no seam), the cost is crashes, memory corruption, cross-platform breakage (Android), silent failures logged under other mods' names, and loss of reviewability — directly violating Constitution II, IV, V and SMAPI best practices.

## Open Questions

- [NEEDS CLARIFICATION: Confirm maintainer approval to add `Lib.Harmony` (0Harmony) as a NuGet dependency (architectural decision; currently zero Harmony footprint).]
- [NEEDS CLARIFICATION: Confirm `HarvestQualityService` seam API shape (inputs: tile health, Farming level, fertilizer tier, `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`, pest state provider) so RED-phase tests (T024–T035) can be written first.]
- [NEEDS CLARIFICATION: Confirm patch style — `Prefix` (return false to suppress original quality roll) vs. `Postfix` (modify `Object.Quality` after the vanilla roll) — affects composition with vanilla modifiers (FR-003 "compose, don't replace").]
- [NEEDS CLARIFICATION: Confirm testability strategy — how to exercise `HarvestQualityService` in xUnit/FsCheck without a game runtime (pure functions over primitives).]
- [NEEDS CLARIFICATION: Confirm `SC-008` rollback verification status (absolute blocker for yield activation) before any harvest-hook code is written.]
- [NEEDS CLARIFICATION: Confirm whether an ADR document should be created (`docs/adr/001-harmony-for-harvest-interception.md`) to record the decision formally.]
- [NEEDS CLARIFICATION: Confirm Android/cross-platform impact acceptance (SMAPI warns Harmony is higher risk on Android and future game updates; `EnableHarmony=true` uses SMAPI-bundled Harmony 2.2.x).]
- [NEEDS CLARIFICATION: Confirm seam's host-authority contract — replicated deterministic roll (A8) means the triggering client computes from synced soil health; the seam must be pure/deterministic given inputs so both clients agree.]
