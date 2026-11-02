# Idea Intake: Add Lib.Harmony and write a Harmony patch for Crop.harvest (harvest-quality seam)

- **Slug**: harmony-harvest-quality-seam
- **Created**: 2026-11-01
- **Source**: user prompt (pasted text) during spec 014 implementation, following GamePath spike T007
- **Type**: new-capability

## Idea (as captured)

> Adicionar Lib.Harmony (0Harmony) como dependência e escrever um patch Harmony Prefix/Postfix em Crop.harvest que delega a um seam testável HarvestQualityService. O seam contém toda a lógica de tier (testável via xUnit/FsCheck); o patch em si é verificado apenas no Gate 3 in-game. Segue o caminho de fallback da Q2, ativado pelo spike. Conforme SMAPI: code API (não annotations), postfix preferido, try/catch com fallback ao original, EnableHarmony=true. Rationale: é o único mecanismo viável para um método virtual; aedenthorn já prova que funciona em 1.6.15; SMAPI fornece reescrita cross-platform; a lógica fica testável via seam (Princípio IV / revisabilidade).

## Restated

Add the `Lib.Harmony` (0Harmony) NuGet package as a dependency and implement a Harmony `Prefix`/`Postfix` patch on the virtual `StardewValley.Crop.harvest` method that delegates all tier logic to a testable `HarvestQualityService` seam. The seam holds the harvest-quality/extra-harvest rules (xUnit/FsCheck-testable); the patch itself is verified only by the in-game Gate 3 session. Follows the Q2 fallback path activated by the T007 spike, per SMAPI guidance (code API not annotations, prefer postfix, try/catch with fallback to original, `EnableHarmony=true`).

## Origin & Context

- **Raised by**: maintainer (lfgranja), during spec 014 implementation, after the GamePath spike T007 confirmed `Crop.harvest` is a virtual method (public-field manipulation cannot intercept it).
- **Trigger**: T007 spike result (`specs/014-soil-health-crop-quality/spike-t007-gamepath.md`) — `Crop.harvest` signature `public virtual bool harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null, bool isForcedScytheHarvest = false)`; `Object.Quality` is a public get/set property; `HoeDirt.dayUpdate` and `Crop.dayOfCurrentPhase`/`newDay`/`phaseDays` confirmed present. Public-field path is insufficient for harvest interception, so the Q2 fallback (Harmony) is activated.
- **Related specs**: `specs/005-soil-health-to-crop-growth/` (Q1 hook path decision — public fields for growth, Harmony fallback), `specs/014-soil-health-crop-quality/` (Q2 hook names, T007, T017 design, T044–T054 implementation tasks).
- **Related decision**: no formal ADR exists; the decision is documented in `clarify.md`, `concept.md`, `decision.md`, `spec.md` Clarifications Q2, and `spike-t007-gamepath.md`. The user asked whether it was recorded in an ADR — it was not. Recommendation to promote to `docs/adr/001-harmony-for-harvest-interception.md`.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: Confirm maintainer approval to add `Lib.Harmony` (0Harmony) as a NuGet dependency (architectural decision; currently zero Harmony footprint).]
- [NEEDS CLARIFICATION: Confirm `HarvestQualityService` seam API shape (method signatures, inputs: tile health, Farming level, fertilizer tier, `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`, pest state provider) so RED-phase tests (T024–T035) can be written first.]
- [NEEDS CLARIFICATION: Confirm patch style — `Prefix` (return false to suppress original) vs. `Postfix` (modify `__result`/`Object.Quality` after original roll) — per SMAPI guidance prefer postfix; but forcing `Quality = 0` for Poor and boosting `p` for Healthy may require prefix (to alter inputs) or postfix (to alter output). Needs design.]
- [NEEDS CLARIFICATION: Confirm whether the patch should be a `Prefix` returning `false` for Poor (suppress original quality roll) or a `Postfix` that overwrites `Quality` after the vanilla roll — affects composition with vanilla modifiers (FR-003 "compose, don't replace").]
- [NEEDS CLARIFICATION: Confirm testability strategy — how to exercise `HarvestQualityService` in xUnit/FsCheck without a game runtime (pure functions over primitives).]
- [NEEDS CLARIFICATION: Confirm `SC-008` rollback verification status (absolute blocker for yield activation) before any harvest-hook code is written.]
- [NEEDS CLARIFICATION: Confirm whether an ADR document should be created (`docs/adr/001-harmony-for-harvest-interception.md`) to record the decision formally.]
- [NEEDS CLARIFICATION: Confirm Android/cross-platform impact acceptance (SMAPI warns Harmony is higher risk on Android and future game updates; `EnableHarmony=true` uses SMAPI-bundled Harmony 2.2.x).]
- [NEEDS CLARIFICATION: Confirm whether `Lib.Harmony` should be added to `LivingRoots.csproj` via `<EnableHarmony>true</EnableHarmony>` (SMAPI-managed) rather than a direct package reference, per SMAPI guidance.]
- [NEEDS CLARIFICATION: Confirm the seam's host-authority contract — replicated deterministic roll (A8) means the triggering client computes from synced soil health; the seam must be pure/deterministic given inputs so both clients agree.]
