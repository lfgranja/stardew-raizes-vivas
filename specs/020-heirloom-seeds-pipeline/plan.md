# Implementation Plan: Heirloom Seeds / Sementes Crioulas Pipeline (Crioulo Parsnip — one-crop template)

**Branch**: `020-heirloom-seeds-pipeline` | **Date**: 2026-11-09 | **Spec**: [specs/020-heirloom-seeds-pipeline/spec.md](spec.md)

**Input**: Feature specification from `specs/020-heirloom-seeds-pipeline/spec.md`

## Summary

Add a reusable heirloom/crioulo seed→crop→produce pipeline, proven end-to-end by exactly one Spring crop — **Crioulo Parsnip** (`LivingRoots_HeirloomParsnipSeeds` → `LivingRoots_HeirloomParsnip`, 7-day Spring cycle, slower than vanilla parsnip) — distributed in-game **without** any Elena dependency: Traveler's cart special-stock entry (`Data/Shops` key `Traveler`, `AvailableStockLimit: Player`, `SEASON Spring` condition) plus guaranteed seed-saving (1 saved seed per harvest, non-RNG). Soil coupling: `+HeirloomPlantBonus` (2.0, labeled abstraction) on heirloom plant via `HoeDirt.plant` postfix (host-authoritative; farmhands via typed host-mediated request or explicit authority refusal); `+HeirloomHarvestBonus` (1.0) on Healthy-band harvest through the existing 014 harvest Harmony seam; 014 decision service is reused untouched (no double-counting): crioulo parsnip rides the same tiers (31/71) and vanilla-math composition as every crop. Farm-scoped host-authoritative (Constitution VI), additive-only save data, placeholder procedural art disclosed, EN/PT/default localization, full TDD, Gate 3-verifiable (`heirloom-plant-bonus.scenario.json`, host-only machine-pass, honesty R); no manifest bump/tag/release this slice (maintainer actions).

## Technical Context

**Language/Version**: C# (latest) on .NET 6 (manifest floor: SDV `MinimumGameVersion` 1.6.14, SMAPI `MinimumApiVersion` 4.4.0); builds against `/home/luis/stardew-assemblies` via `GamePath`.

**Primary Dependencies**: `Pathoschild.Stardew.ModBuildConfig` (permanent), Harmony (`lib.harmony` via NuGet), SMAPI managed assemblies; xUnit + Moq + FsCheck + Microsoft.Coyote for tests.

**Storage**: Stardew Valley save system only (sparse `SoilHealthState` JSON via `ISoilHealthService`/`ModDataService`); additive item definitions; no new persistence format.

**Testing**: xUnit (`LivingRoots.Tests`); targeted `{Subject}Tests.cs` naming; existing registration-test pattern (`AssetRegistrationTests`, `AgrochemicalRegistrationTests`); headless verification runner (`LivingRoots.Verification`, `scripts/verify-scenario.sh`, scenario schema v3) for Gate 3.

**Target Platform**: Stardew Valley 1.6 (PC Linux/macOS/Windows) mod via SMAPI; in-game singleplayer (authoritative degenerate case) + co-op.

**Project Type**: SMAPI game mod (single project + verification harness).

**Performance Goals**: No per-tick new work beyond existing hooks; `HoeDirt.plant` postfix does ≤ O(1) work (early-out on non-heirloom IDs); harvest-seam extension adds one string/ID check per harvest; memory neutral (item modData ≤ 64B/seed).

**Constraints**: `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `CheckForOverflowUnderflow`, async-only I/O, `ModConstants` single source for tunables, host-authoritative writes only, bounded 0–100 soil health, NaN→100 neutral baseline, no new runtime dependencies, no public release/tag, placeholder art disclosed.

**Scale/Scope**: 1 feature of 19 named roadmap features; ~2 new assets + 2 new items + 1 crop row + 1 scenario + 1 debug command; suite stays ≥ 1937 passing; ~30–60 new targeted tests expected; zero promoted claims.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate (how this slice complies) | Status |
|---|---|---|
| I. DDD (layering) | New concepts as domain seams: `Heirloom*`/`Crioulo*` constants in `ModConstants`; no new domain services required (reuse `ISoilHealthService`, `IHarvestQualityService`, `HostAtomicLockService`); no new persistence; crop data via `Data/*`. | PASS |
| II. Security-First | No new filesystem paths, no new save-key parsing (reuse validated `ModDataService`); soil writes via existing validated service; no secrets; string item IDs exact-match only. | PASS |
| III. Async-only | No new async I/O; no `.Result`/`.Wait()`; synchronous game-tick paths unchanged in shape (postfix/prefix pattern already synchronous by design, snapshot-diff conventions respected). | PASS |
| IV. TDD | Tests written before/with implementation (registration contracts first, then seam math, then integration/Gate 3); Red→Green→Refactor; targeted suite green before broad suite. | PASS |
| V. YAGNI / Simplicity | Exactly one crop (not 3); no genetics sim; no shop beyond `Traveler`; no new generic pipeline framework abstraction — constants + one conditional seam; no Achievements wiring (`CountForMonoculture` reuses vanilla achievement). | PASS |
| VI. Multiplayer First-Class | Host-authoritative + farm-scoped; atomic per-tile first-wins lock reused; farmhand = explicit `HeirloomPlantBonusRequest` → host validates/applies/broadcasts `SoilHealthSyncMessage` (or explicit authority-unsupported refusal copy — substituted only with disclosed deviation); no second source of truth; D1/human-E claims stay `pending` (stubs ≠ live proof). | PASS |

TDD/QA gate re-check after Phase 1 design: unchanged — no technology decision introduced; see Post-Design Re-Check below.

## Project Structure

### Documentation (this feature)

```text
specs/020-heirloom-seeds-pipeline/
├── spec.md              # Feature specification (done)
├── plan.md              # This file
├── research.md          # Phase 0 output — decisions & consolidated evidence
├── data-model.md        # Phase 1 output — entities, states, rules
├── quickstart.md        # Phase 1 output — runnable validation guide
├── contracts/
│   ├── heirloom-seed-registration.md   # data contract: items/crops/shops + contract tests
│   └── heirloom-soil-effects.md        # behavior contract: plant/harvest/seed-save seams + MP authority
├── checklists/
│   └── requirements.md  # Spec quality checklist (done)
├── scenarios/
│   └── heirloom-plant-bonus.scenario.json  # Gate 3 scenario (created in tasks)
├── claim-records.md     # claims discipline artifact (tasks phase)
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
LivingRoots/
├── Assets/
│   ├── heirloom_seeds.png          # 16×16 placeholder icon (NEW)
│   ├── heirloom_produce.png        # 16×16 placeholder icon (NEW)
│   └── Crops/
│       └── heirloom_crops.png      # 128×32 ×phases spritesheet row (NEW)
├── Constants.cs                    # + Heirloom* constants block (MODIFIED)
├── ModEntry.cs                     # + OnAssetRequested branches (Objects/Crops/Shops/textures) (MODIFIED)
├── i18n/{default,en,pt}.json       # + item.heirloom.*, message.heirloom.* keys (MODIFIED)
├── i18n parity test hook           # existing parity test extended (Tests)
├── Patches/
│   └── HeirloomPatches.cs          # NEW — HoeDirt.plant postfix + harvest-seam extension (or folded into HarvestPatches with narrow heirloom identity filter — decided in tasks, minimal-surface preference)
├── Domain/
│   ├── IHeirloomSeedService.cs     # NEW (optional if no logic beyond seam math; prefer minimal — see tasks)
│   └── HeirloomIdentity.cs         # NEW — pure heirloom identity/tag rules (testable without SMAPI)
├── Domain/Interfaces/IModDataService.cs  # unchanged
├── Services/Visualization/...      # unchanged (feedback reuses existing harvest feedback; no new overlay)
└── Controllers/ModController.cs    # unchanged except broadcast call-site if reused as-is

LivingRoots.Tests/
├── HeirloomRegistrationTests.cs    # NEW — Data contract assertions (objects/crops/shops/textures present)
├── HeirloomSoilEffectsTests.cs     # NEW — plant/harvest bonus math, bounds, NaN baseline, healthy-band gate, cap rule
├── HeirloomSeedSaveTests.cs        # NEW — saved-seed yield=1, inventory-full path, Junimo exemption
├── HeirloomI18nTests.cs            # NEW — key presence/parity EN/PT/default; no nitrogen vocabulary in crioulo strings
└── HeirloomLeaseSyncTests.cs       # (fold into existing sync stub tests if pattern supports)

LivingRoots.Verification/
└── ModEntry.cs + Queue/QueueCommandParser.cs  # + lr_give_heirloom_seed command (additive, verification-only)

scripts/tests/
└── (existing suite untouched except new shell wrapper test-020-heirloom.sh if pattern kept)
```

**Structure Decision**: Single mod project + tests + verification harness mirrors repository reality; no new project; no new package dependencies; all textures served by mod's own loader (no Content Patcher dependency); the only new executable surfaces are one registration branch set, one Harmony postfix (`HoeDirt.plant`), one narrow heirloom filter inside the existing harvest seam, one debug verification command, and one scenario JSON.

## Post-Design Constitution Re-Check (after Phase 0/1 artifacts)

| Principle | Status | Notes |
|---|---|---|
| I. DDD | PASS | data-model.md keeps heirloom identity as pure data + constants; no new service tier required; seam reuse confirmed. |
| II. Security-First | PASS | no new paths/keys/parsers; bounded writes only. |
| III. Async-only | PASS | no I/O introduced; hooks stay tick-synchronous. |
| IV. TDD | PASS | contract test matrix fixed in contracts/*.md; red-first order in tasks. |
| V. YAGNI | PASS | one crop; no genetics; no GM/SVE-style content pack; minimal message surface (one new typed message). |
| VI. Multiplayer | PASS | host-apply/refusal semantics; sync unchanged; D1 pending honestly. |

**No gate regressions post-design.** Proceed to `/speckit-checklist` and `/speckit-analyze`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | — | — |
