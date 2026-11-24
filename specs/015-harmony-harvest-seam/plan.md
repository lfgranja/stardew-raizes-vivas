# Implementation Plan: Add Lib.Harmony and Harmony Patch for Crop.harvest (HarvestQualityService Seam)

**Branch**: `015-harmony-harvest-seam` | **Date**: 2026-11-01 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/015-harmony-harvest-seam/spec.md`

---

## Summary

Add `Lib.Harmony` (0Harmony) as a dependency (via `<EnableHarmony>true</EnableHarmony>`, SMAPI-bundled 2.2.x) and implement a Harmony patch on the virtual `Crop.harvest` method that delegates all tier logic to a testable `HarvestQualityService` seam. The seam holds the harvest-quality rules (Poor → force `Quality = 0`; Healthy → `+0.08` on `p` and `+0.10` on `ExtraHarvestChance`; Moderate/Unknown → vanilla) as pure functions (xUnit/FsCheck-testable without a game runtime). The patch itself is verified only by the Gate 3 in-game session. This is the Q2 fallback path activated by the T007 GamePath spike, following SMAPI guidance (code API not annotations, postfix preferred, try/catch with fallback to original, `EnableHarmony=true`). This spec is the implementation enabler for the harvest-hook portion of spec 014 (tasks T044–T054, T007, T017).

Key deliverables (design-locked — zero `NEEDS CLARIFICATION` markers remain; execution deferred to tasks):
1. **EnableHarmony** (FR-001): add `<EnableHarmony>true</EnableHarmony>` to `LivingRoots.csproj` first `<PropertyGroup>`.
2. **Patch registration** (FR-002): register via code API in `ModEntry.Entry` (`new Harmony(ModManifest.UniqueID).Patch(...)`), not annotations/`PatchAll`.
3. **HarvestQualityService seam** (FR-003): pure service in `LivingRoots.Domain.Services`, interface `IHarvestQualityService` in `LivingRoots.Domain`, types in `LivingRoots.Domain.HarvestQualityTypes`. Methods: `Decide`, `ResolveFeedback`, `BoostQualityProbability`, `BoostExtraHarvestChance`. Pure/deterministic given inputs.
4. **Harmony patch** (FR-004): static patch on `Crop.harvest` delegating to the seam; reads soil health from `ISoilHealthService`; sets `Object.Quality` directly; drives `VisualizationService` feedback. Non-suppressing two-phase: Prefix returns `true` (state capture + crop-scope guard + extra-harvest probability adjustment); Postfix forces `Quality = 0` (Poor), applies Healthy boost as conditional upgrade, drives feedback, handles inventory-full.
5. **Safety** (FR-005–FR-006): static methods, try/catch with fallback to original, log errors, prefer postfix, avoid transpilers.
6. **Crop scope** (FR-007): early return for forage crops, fibre seeds, Qi Fruit.
7. **Edge handling** (FR-008–FR-010): inventory-full (main blocked, extra debris), NaN/Unknown → vanilla, measured 0 → Poor.
8. **Determinism** (FR-011): seam pure/deterministic (replicated deterministic roll, A8).
9. **Guard compliance** (FR-012–FR-013): `ProductionSourceGuardTests`, `FeatureStatusTests` README strings.
10. **TDD** (FR-014): RED-phase tests first.

**Deferred prerequisites** (must resolve before yield activation — not before design/implementation):
1. `SC-008` rollback verification (spec 014) — absolute blocker for yield activation.
2. Gate 3 in-game SMAPI session (spec 014 Q10) — capture 3 signals with second player.
3. Maintainer approval to add Harmony dependency (assumption; assessment verdict "go" activates the fallback).

---

## Technical Context

**Language/Version**: .NET 6 with C# latest language version.

**Primary Dependencies**: SMAPI (Stardew Modding API 4.x), `Pathoschild.Stardew.ModBuildConfig` 4.4.0, `Microsoft.Xna.Framework` (MonoGame), `Lib.Harmony` (0Harmony) via `<EnableHarmony>true</EnableHarmony>` (SMAPI-bundled 2.2.x, no separate version pin), xUnit / Moq / FsCheck for unit/property tests.

**Storage**: Stardew Valley save system through `IModDataService` (existing, unchanged). Harvest outcome is **not persisted** — derived from soil health at harvest. No new persistence.

**Testing**: xUnit + Moq + FsCheck; one test file per subject (`{Subject}Tests.cs`); `ThreadSafeGameLoopEventsStub` for concurrent event simulation; `HarvestQualityService` seam tested without a game runtime (pure functions). Harmony patch itself verified only by Gate 3 in-game session (cannot run in headless host).

**Target Platform**: Stardew Valley 1.6 mod via SMAPI (cross-platform desktop: Linux, macOS, Windows). Android: higher risk; warn and accept untested (consistent with existing policy).

**Project Type**: SMAPI mod (desktop game plugin — DLL); purely internal — no public APIs. Internal contracts (`IHarvestQualityService`, `ISoilHealthService`, `ICompostApplicationService`, `IPlayerInventory`, `IHostAtomicLockService`, `IVisualizationService`) documented in `contracts/`.

**Performance Goals**: No new per-frame render pass; transient feedback event-driven only; must not trigger 1000-tile degradation or exceed 16.67 ms/frame (spec 014 SC-005). Harmony patch adds one virtual-method interception per harvest (negligible).

**Constraints**:
- Multiplayer first-class (Principle VI, NON-NEGOTIABLE): host-authoritative persistent soil health; replicated deterministic roll (A8) — seam pure/deterministic given inputs; transient feedback visual/local only (does not sync); first-wins semantics; live 2-player verification owed to roadmap D1.
- Async-only (Principle III): `.Result`/`.Wait()` forbidden; `volatile`/`Interlocked` permitted for atomic flags. Patch is a synchronous game-thread callback (no I/O).
- Security-first (Principle II): all input paths sanitized; bounds-checked; `CheckForOverflowUnderflow` enabled; no raw exception messages to logs. Patch wraps logic in try/catch, logs via `IMonitor`, defaults to original.
- Zero new assets / `i18n` (spec 014 A4): feedback text literal; procedural sprites. No `Data/Crops`/`Data/Objects` edits (spec 014 FR-004).
- No new `ContentPatcher`; no `config.json`/GMCM.
- Sparse save: only non-default health values stored (unchanged).
- DDD layered architecture (Principle I): interface in `Domain/`, impl in `Domain.Services` (pure), patch in `Services/` or `Controllers/` (SMAPI-facing Humble Object). Ubiquitous language (`SoilHealth`, `HarvestQuality`, `HealthCategory`, `LivingSoil`).
- No persistent per-tile tier marker (spec 014 FR-011).
- Harmony as last resort: used only for `Crop.harvest` interception (virtual method); growth modifier stays public-field (no Harmony). Code API not annotations; postfix preferred; avoid transpilers.
- Patch methods static; try/catch with fallback to original (SMAPI best practice).

**Scale/Scope**: Single mod, single feature. Design covers only the Harmony dependency + seam + patch. Broader scope (pest system, "new interactions", numerical tuning, mid/end-game) deferred to separate specs.

**Unknowns / Deferred**:
- No `NEEDS CLARIFICATION` markers remain in `spec.md` (8 open questions resolved in clarify session 2026-11-01).
- Deferred prerequisites (gate yield activation, not design/implementation):
  1. `SC-008` rollback verification (spec 014) — absolute blocker for yield activation.
  2. Gate 3 in-game SMAPI session (spec 014 Q10) — 3 signals + second player.
  3. Maintainer approval for Harmony dependency (assumption; assessment verdict "go" activates the fallback).
  4. ADR creation (recommended, not blocking).

---

## Constitution Check (Pre-Phase 0)

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution version: `2.0.0` (ratified `2026-09-05`; last amended `2026-10-13`).

| Principle | Status | Evidence / Notes |
|-----------|--------|------------------|
| I. Domain-Driven Design (NON-NEGOTIABLE) | PASS | Layered architecture preserved: `Domain/` (interface `IHarvestQualityService`, types, pure `HarvestQualityService` in `Domain.Services`) → SMAPI-facing patch (Humble Object) in `Services/` or `Controllers/`. Ubiquitous language (`SoilHealth`, `HarvestQuality`, `HealthCategory`, `LivingSoil`). Seam is pure domain; patch is the Humble Object. |
| II. Security-First Data Handling (NON-NEGOTIABLE) | PASS | No new file paths or persistence (harvest outcome is transient, not persisted). Health values clamped 0–100 (existing). `CheckForOverflowUnderflow` enabled. Patch try/catch logs via `IMonitor` (no raw exception messages). Crop scope guards (forage/fibre/Qi) prevent invalid application. |
| III. Async-Only Concurrency (NON-NEGOTIABLE) | PASS | Patch is a synchronous game-thread callback (no I/O) — consistent with existing compost/mulch button-press paths. `ModEntry.Dispose()` uses lock + `_disposed`. Event handlers unregistered on disposal. Seam is pure (no I/O). |
| IV. Test-Driven Development | PASS | RED-phase tests (xUnit/FsCheck) written and failing before implementation; seam testable without game runtime. Patch itself verified only by Gate 3 in-game (acknowledged automation gap, not hidden). One test file per subject. |
| V. Simplicity and YAGNI | PASS | Harmony added only because public fields are insufficient (spike-confirmed). Code API (not annotations). Postfix preferred. No transpilers. No premature abstractions (seam is justified by testability). `EnableHarmony` uses SMAPI-bundled (no separate version pin). |
| VI. Multiplayer As A First-Class Requirement (NON-NEGOTIABLE, v2.0.0) | CONDITIONAL PASS | Seam is pure/deterministic given inputs (replicated deterministic roll, A8). Soil health is host-authoritative and synced on `DayStarted`. Patch runs locally on triggering client. Transient feedback visual/local only (does not sync). Live 2-player verification (D1) deferred to roadmap D1. Single-player is the degenerate case. **No MUST violations**; conditional pass justified. |

**Gate Result**: PASS with one documented conditional (Principle VI live 2-player verification deferred to roadmap D1; no MUST violations). Design work (Phase 1) completed.

---

## Project Structure

### Documentation (this feature)

```text
specs/015-harmony-harvest-seam/
├── plan.md              # This file
├── research.md          # Phase 0 output (clarify session 2026-11-01; 8 questions resolved)
├── data-model.md        # Phase 1 output (seam types, inputs/outputs, validation)
├── quickstart.md        # Phase 1 output (runnable validation scenarios)
├── contracts/           # Phase 1 output (internal seam contract)
│   └── i-harvest-quality-seam.md
└── tasks.md             # Phase 2 output ($speckit-tasks — NOT created by $speckit-plan)
```

### Source Code (repository root — `LivingRoots/`)

```text
LivingRoots/
├── LivingRoots.csproj                    # Add <EnableHarmony>true</EnableHarmony> in first <PropertyGroup>
├── ModEntry.cs                           # Register Harmony patch via code API in Entry()
├── Domain/
│   ├── IHarvestQualityService.cs         # NEW: interface (LivingRoots.Domain)
│   ├── HarvestQualityTypes.cs            # NEW: HarvestTier, HarvestFeedbackFlags, HarvestQualityInputs, HarvestQualityDecision
│   └── Services/
│       └── HarvestQualityService.cs      # NEW: pure seam (LivingRoots.Domain.Services)
├── Services/
│   └── (Harmony patch class — Humble Object, SMAPI-facing; location TBD during tasks, likely ModEntry or a dedicated HarmonyPatches.cs)
├── Controllers/
│   └── ModController.cs                  # unchanged (growth modifier stays public-field, no Harmony)
└── (no new assets, no new i18n keys)

LivingRoots.Tests/
└── Domain/
    └── Services/
        └── HarvestQualityServiceTests.cs  # NEW: xUnit + FsCheck (pure seam, no game runtime)
```

**Structure Decision**: Follows existing DDD layered architecture. The seam is a pure domain service (`Domain.Services`, alongside `OrganicWasteValidator`, `SeasonalDecayMultiplier`) — SMAPI-free, constructor-injected via `ModEntry`. The Harmony patch is the Humble Object (SMAPI-facing), registered via code API in `ModEntry.Entry`. `EnableHarmony=true` is the only `.csproj` change (SMAPI-managed Harmony, no separate version pin). No new assets, i18n keys, or data entries. Growth modifier stays public-field (no Harmony regression).

---

## Complexity Tracking

No constitution violations to track. The one conditional (Principle VI live 2-player verification deferred to roadmap D1) is a documented debt, not a violation.

---

## Phase 0: Outline & Research

**Status**: Complete. See `research.md` (Phase 0 output) and the research artifacts in `/tmp/opencode/research-015/` (internal-patchstyle.md, research-patchstyle.md, internal-seam.md, research-seam.md, internal-android.md, research-android.md).

**Resolved unknowns:**
- Patch style → non-suppressing two-phase patch (Prefix returns true for state capture; Postfix for Quality overwrite + feedback + inventory-full). Suppressing prefix never acceptable. Transpiler = documented fallback (last resort). (research-patchstyle.md, internal-patchstyle.md)
- Seam API shape → `IHarvestQualityService` with `Decide`/`ResolveFeedback`/`BoostQualityProbability`/`BoostExtraHarvestChance`; types `HarvestTier`/`HarvestFeedbackFlags`/`HarvestQualityInputs`/`HarvestQualityDecision`. (internal-seam.md §5)
- Android/cross-platform → accept "untested on Android" with documentation (README, Nexus tags, no AndroidCompatible claim, optional runtime guard; best-effort fallback to vanilla on patch failure). Consistent with existing desktop-only policy. (internal-android.md, research-android.md)
- Testability → pure functions over primitives in xUnit/FsCheck; seam has no SMAPI/game dependencies. (research-seam.md)
- Host authority → replicated deterministic roll (A8); seam pure/deterministic. (spec 014 A8)
- SC-008 → absolute blocker for yield activation (not for design/tests). (spec 014)
- ADR → recommended, not blocking. (concept.md)

**No NEEDS CLARIFICATION markers remain** in Technical Context.

---

## Phase 1: Design & Contracts

**Prerequisites**: `research.md` complete (yes).

1. **Entities** → `data-model.md`: `HarvestQualityService` (seam), `HarvestTier` enum, `HarvestFeedbackFlags` flags, `HarvestQualityInputs` record, `HarvestQualityDecision` record. Validation rules: pure/deterministic; no SMAPI deps; references `ModConstants` for boost values. State transitions: none (stateless seam).

2. **Interface contracts** → `contracts/i-harvest-quality-seam.md`: `IHarvestQualityService` (4 methods), patch registration contract (code API, `EnableHarmony`), host-authority contract (replicated deterministic roll), safety contract (try/catch, fallback to original, static methods), Android/cross-platform contract (warn, accept untested).

3. **Quickstart validation guide** → `quickstart.md`: build with `EnableHarmony=true`, SMAPI log inspection (patch registered, stability warning), Gate 3 in-game verification, seam unit test scenarios.

**Post-Design Re-evaluation (Phase 1 Complete)**: all six principles PASS with the same documented conditional (Principle VI live 2-player verification deferred to roadmap D1). Design aligns with all constitutional principles; no new violations introduced.

---

## Done When (Plan)

- [ ] Plan workflow executed and design artifacts generated (`plan.md`, `research.md`, `data-model.md`, `contracts/i-harvest-quality-seam.md`, `quickstart.md`)
- [ ] Extension hooks dispatched or skipped per Mandatory Post-Execution Hooks
- [ ] Completion reported with branch, plan path, and generated artifacts

*Implementation details (tasks, RED-phase tests, hook wiring, NuGet reference) belong in `tasks.md` (Phase 2 — `$speckit-tasks`), not in this plan.*
