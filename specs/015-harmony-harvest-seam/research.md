# Phase 0 Research: Add Lib.Harmony and Harmony Patch for Crop.harvest (HarvestQualityService Seam)

**Feature**: `015-harmony-harvest-seam` | **Branch**: `015-harmony-harvest-seam`
**Based on**: `spec.md` (clarify session 2026-11-01, 8 questions resolved) + assessment `harmony-harvest-quality-seam` (intake, problem, concept, decision) + spec 014 (spike T007, A6/A7/A8, B1/B2) + SMAPI Harmony guide

---

## Summary

All `NEEDS CLARIFICATION` markers are resolved. Evidence was gathered via 6 parallel research agents (internal codebase deep-dives + online research) plus the T007 GamePath spike and the SMAPI Harmony guide (EN + ZH).

## Resolved Decisions

| # | Topic | Decision | Rationale | Evidence |
|---|-------|----------|-----------|----------|
| 1 | Maintainer approval for Lib.Harmony | Approved via assessment verdict "go" (2026-11-01) and Q2 fallback path activated by spike | The assessment pipeline (intake → research → define → shape → decide) reached "go" with Option A; the spike confirmed public fields are insufficient | decision.md (go); spike-t007-gamepath.md |
| 2 | Crop.harvest member names | Confirmed: `public virtual bool harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null, bool isForcedScytheHarvest = false)`; `Object.Quality` get/set; `HoeDirt.dayUpdate`; `Crop.dayOfCurrentPhase`/`newDay`/`phaseDays` | T007 spike against 1.6.x binary; cached decompilation | spike-t007-gamepath.md |
| 3 | Patch style (Prefix vs Postfix) | **Non-suppressing two-phase patch**: Prefix returns `true` (state capture + crop-scope guard + extra-harvest probability adjustment); Postfix forces `Quality = 0` (Poor), applies Healthy boost as conditional upgrade, drives feedback, handles inventory-full. Suppressing prefix never acceptable. Transpiler = documented fallback (last resort). | `Crop.harvest` quality is a local variable (not field/return); `__result` is the bool success flag, not the item. A pure Postfix can't modify the in-method roll, so the seam decides final quality and the patch applies it post-hoc. Prefix-returning-false would skip the entire harvest pipeline (item creation, quality roll, extra-harvest roll, inventory-full handling, regrow/removal) — violates FR-003 "compose, don't replace." | internal-patchstyle.md §1/§4; research-patchstyle.md §1/§4/§6; SMAPI Harmony guide ("prefer postfix over prefix"; "default to original game code"; "avoid transpilers unless no alternative") |
| 4 | Seam API shape | `IHarvestQualityService` with `Decide`/`ResolveFeedback`/`BoostQualityProbability`/`BoostExtraHarvestChance`; types `HarvestTier`/`HarvestFeedbackFlags`/`HarvestQualityInputs`/`HarvestQualityDecision` in `LivingRoots.Domain`; impl in `LivingRoots.Domain.Services`. | Follows existing pure-domain-service pattern (`OrganicWasteValidator`, `SeasonalDecayMultiplier`); providers wrap game singletons (ITimeProvider, ISeasonProvider); constructor injection via ModEntry (sole composition root). Feathers seams + Humble Object + DI patterns. | internal-seam.md §5; research-seam.md (Feathers, Meszaros xUnitpatterns, Pathoschild Tests.Common) |
| 5 | Testability strategy | Pure functions over primitives in xUnit/FsCheck; seam has no SMAPI/game dependencies; patch verified only by Gate 3 in-game. | Harmony patches can't run in headless xUnit host (no game runtime). Seam is the testability anchor (Humble Object pattern). | research-seam.md; AGENTS.md (TDD, one test file per subject) |
| 6 | SC-008 status | Absolute blocker for yield activation (not for design/tests). | spec 014 Dependencies 136-137; FR-005 78; clarification 2026-11-01 | spec 014 |
| 7 | Android/cross-platform acceptance | Accept "untested on Android" with documentation (README, Nexus page: do not tick "Android Compatible"; optional runtime guard; best-effort fallback to vanilla on patch failure). Consistent with existing desktop-only policy. | No platform checks in code (delegate to SMAPI/MonoGame); build platform-neutral (net6.0, no RuntimeIdentifier); GDD states "Sem console/mobile no v1"; Android SMAPI is community port (NRTnarathip, v4.3.2 vs official 4.5.2); Harmony rewriting can't rewrite annotations; MonoMod.RuntimeDetour backend on ARM. | internal-android.md; research-android.md |
| 8 | Host-authority contract | Replicated deterministic roll (spec 014 A8): soil health synced host→client on `DayStarted` via `SendMessage`; modifier applied locally on triggering client; seam pure/deterministic given inputs so both clients agree. | spec 014 A8; Constitution Principle VI (host-authoritative farm state, no second source of truth) | spec 014 |

## Alternatives Considered (patch style)

- **Suppressing Prefix (return false)**: rejected — would skip the entire harvest pipeline (item creation, quality roll, extra-harvest roll, inventory-full handling, regrow/removal) and force re-implementing vanilla harvest, violating FR-003 "compose, don't replace" and breaking other mods' patches (SMAPI guidance: first `false` skips remaining prefixes + original).
- **Pure Postfix**: insufficient on its own — `Crop.harvest`'s quality is a local variable, not a field or `__result` (which is the `bool` success flag). A postfix can overwrite `Object.Quality` post-hoc (Poor → 0) and spawn extras manually, but cannot inject `+0.08` into the in-method roll. Hence the two-phase (non-suppressing prefix + postfix) design.
- **Transpiler**: documented fallback (last resort) if exact in-method roll modification is required; SMAPI warns transpilers are fragile and discouraged; aedenthorn's `Crop Quality` mod uses transpilers on 1.6.15 (proves feasibility but not recommended as default).
- **No Harmony (Option B/C from concept)**: rejected — leaves the central promise half-delivered.

## Evidence Against (Harmony dependency)

- SMAPI Harmony guide: Harmony is a last resort — crashes, memory corruption, SMAPI can't detect incompatible Harmony code, can't rewrite Harmony patches for cross-platform, patches may conflict, may prevent debugger attachment, errors logged under other mods' names, SMAPI shows stability warning.
- Android: community port, experimental, Harmony patching is the biggest risk (MonoMod.RuntimeDetour backend); many mods fail on Android.
- Future game updates: patch may break on 1.6.x patches.
- No existing Harmony footprint in the project (zero `Lib.Harmony` references today).

## Gaps & Open Questions

- [NEEDS CLARIFICATION: Confirm maintainer approval to add `Lib.Harmony` (0Harmony) as a dependency (first Harmony use in the project).] — recommended: approve, since spike confirmed it's the only viable path.
- [NEEDS CLARIFICATION: Confirm `HarvestQualityService` seam API shape — adopt recommended design from internal-seam.md §5 (inputs/outputs above).]
- [NEEDS CLARIFICATION: Confirm patch style — adopt non-suppressing two-phase patch (Prefix returns true + Postfix) per research; or choose pure Postfix with manual extra spawn (deviation from strict +0.08 on p).]
- [NEEDS CLARIFICATION: Confirm testability strategy — pure functions over primitives in xUnit/FsCheck (recommended).]
- [NEEDS CLARIFICATION: Confirm `SC-008` rollback verification status (absolute blocker for yield activation).]
- [NEEDS CLARIFICATION: Confirm whether an ADR document should be created (`docs/adr/001-harmony-for-harvest-interception.md`).]
- [NEEDS CLARIFICATION: Confirm Android/cross-platform impact acceptance (warn + accept untested, per existing policy).]
- [NEEDS CLARIFICATION: Confirm seam host-authority contract — replicated deterministic roll (A8); seam pure/deterministic given inputs.]

## Sources

- SMAPI Harmony guide (EN): https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony (Wayback 2025-11-18)
- SMAPI Harmony guide (ZH): https://wiki.biligame.com/stardewvalley/模组:制作指南/APIs/Harmony (Wayback 2025-12-22)
- Skill `stardew-valley-smapi` (cylixlee) — Harmony policy, code API, EnableHarmony, cross-platform
- Skill `harmony-patching-mastery` (meyverick) — static methods, prefer postfixes, transpilers last resort, try/catch
- aedenthorn `StardewValleyMods` (Crop Quality, Crop Variation) — Harmony transpiler on `Crop.harvest` on 1.6.15
- Spike T007: `/tmp/opencode/decomp-crop/StardewValley.Crop.decompiled.cs` (cached), `strings` on Stardew Valley.dll
- Internal reports: `/tmp/opencode/research-015/{internal-patchstyle.md, research-patchstyle.md, internal-seam.md, research-seam.md, internal-android.md, research-android.md}`
- Project artifacts: `specs/014-soil-health-crop-quality/{spec.md, spike-t007-gamepath.md, contracts/i-harvest-quality.md}`, `AGENTS.md`, `LivingRoots/AGENTS.md`, `LivingRoots/Domain/AGENTS.md`, `LivingRoots/Services/AGENTS.md`
- Constitution v2.0.0 (Principles I–VI)
- Michael Feathers, *Working Effectively with Legacy Code* (2004) — seams, Humble Object
- Meszaros, *xUnit Test Patterns* (2007) — Humble Object variants, Dependency Injection
- FsCheck docs (QuickStart, StatefulTesting)
- Pathoschild StardewMods (Tests.Common/Tests.Mods pattern)
