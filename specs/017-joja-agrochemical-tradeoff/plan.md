# Implementation Plan: 017 Joja / Agrochemical Tradeoff (MVP)

**Branch**: `017-joja-agrochemical-tradeoff` | **Date**: 2026-11-06 | **Spec**: [`spec.md`](spec.md)

**Input**: Feature specification from `/specs/017-joja-agrochemical-tradeoff/spec.md`

## Summary

Deliver the first controlled GDD destructive-decision slice: one Joja-framed agrochemical input, applied through a **mod-intercepted** path (no vanilla fertilizer hook), that gives an immediate **confirmation/proxy-only** short-term gain (C1:A; no 014/015 quality/growth or vanilla fertilizer-tier mechanical buff) and applies an immediate bounded soil penalty (**provisional `P=10`**, tunable, C3:A). Unmeasured/`NaN` soil uses an explicit neutral baseline of **`MaxSoilHealth` (100)** and records an explicit measured post-penalty result via absolute soil write (C2:B). Shared-farm mutation is **host-authoritative MVP**; unsupported farmhand attempts are refused **before** soil mutation, item consumption, or false-success feedback (C4:A), with FR-009 snapshot sync compatibility. Scope is small (1 item/rule), TDD-first, no mandatory new assets, honest Gate 3 (host-only `machine-pass` only when evidenced; no D1/`verified` overclaim).

## Technical Context

**Language/Version**: C# (latest, per project) on .NET 6  
**Primary Dependencies**: SMAPI mod APIs, `Pathoschild.Stardew.ModBuildConfig` (permanent), Stardew Valley 1.6.x game assemblies via `GamePath`, xUnit + Moq in `LivingRoots.Tests`  
**Storage**: Stardew Valley save/mod data through existing project patterns only; soil state via `ISoilHealthService`/existing persistence; no external database  
**Testing**: `dotnet test` (xUnit), Moq; property/invariant tests where valuable; Gate 3 host-only machine evidence only when separately run (not claimed by default)  
**Target Platform**: Stardew Valley 1.6 + SMAPI (Windows/macOS/Linux where game/SMAPI supported); development/build requires `GamePath`  
**Project Type**: SMAPI mod (domain-driven layered codebase)  
**Performance Goals**: Negligible overhead on player input/intercept and soil mutation paths; reuse existing per-tile lock/snapshot patterns; no heavy per-tick work  
**Constraints**: DDD layering (Domain/Services/Controllers); `ModEntry.cs` sole composition root; host-authoritative shared farm state (Constitution VI); no Harmony fertilizer hook in MVP; no `HoeDirt.fertilizer` writes; no 014/015 double-counting; explicit measured soil writes for C2 (never negative `UpdateHealth` from unknown); first-wins conflict refusal; `ModConstants` for tunables; i18n parity for touched keys (`default/en/pt`); Gate 3 honesty  
**Scale/Scope**: 1 controlled MVP input/rule; localized soil penalty on eligible targets; host-supported authoritative path; farmhand unsupported path refused; provisional `P=10`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. DDD (NON-NEGOTIABLE) | **PASS** | New behavior as domain interface + service implementation + controller orchestration; ubiquitous agroecology/soil language preserved; no domain logic in SMAPI patch/controller beyond coordination. |
| II. Security-First Data Handling | **PASS** | Soil values bounded 0–100 via existing validation/clamp semantics; no new user-influenced filesystem paths; no raw exception leakage in player-facing messages. |
| III. Async-Only Concurrency | **PASS** | No blocking `.Result`/`.Wait()` introduced; uses existing synchronous snapshot/lock patterns where appropriate; event handlers remain registered/unregistered per project patterns. |
| IV. Test-Driven Development | **PASS (planned)** | TDD required: failing tests first for C1–C4 invariants, refusals, C2 baseline, host lock/broadcast, farmhand refusal ordering. One test file per subject where practical. |
| V. Simplicity and YAGNI | **PASS** | 1 item/rule, no Harmony fertilizer hook, no `Data/Shops`/asset expansion unless scoped, no farmhand intent/ack protocol, no stacking/delayed state. |
| VI. Multiplayer First-Class (NON-NEGOTIABLE) | **PASS with explicit MVP boundary** | Host-authoritative mutation, no second source of truth, per-tile lock/first-wins, explicit refusal (inventory intact/no unsafe mutation), FR-009 snapshot sync compatibility. Unsupported farmhand path explicitly refused in MVP; D1 live farmhand convergence is follow-up evidence, not claimed. |

**Gate result (pre-Phase 0): PASS.**  
**Gate result (post-Phase 1): PASS.** Design artifacts keep host authority, C2 absolute measured write, C4 refusal-before-mutation/consumption, C1 no-double-counting, and honest Gate 3 boundaries explicit.

## Phase 0: Research (complete)

See [`research.md`](research.md). All technical clarifications for MVP planning are resolved or explicitly bounded:
- C1 confirmation-only gain, no 014/015/vanilla fertilizer-tier buff;
- C2 neutral baseline 100 + absolute `SetSoilHealth` measured write;
- C3 provisional `P=10` tunable;
- C4 host-authoritative MVP with farmhand refusal before mutation/consumption/false-success;
- no Harmony fertilizer hook; mod-intercepted pattern; FR-009 broadcast reuse; i18n parity.

## Phase 1: Design & Contracts (complete)

- [`data-model.md`](data-model.md) — entities, validation rules, state transitions.
- [`contracts/agrochemical-application-service.md`](contracts/agrochemical-application-service.md) — domain/service/controller behavioral contract.
- [`contracts/content-registration.md`](contracts/content-registration.md) — item/content/i18n/telegraphing contract.
- [`contracts/multiplayer-gate3.md`](contracts/multiplayer-gate3.md) — authority, refusal, FR-009, and verification honesty contract.
- [`quickstart.md`](quickstart.md) — runnable validation scenarios and commands.

## Project Structure

### Documentation (this feature)

```text
specs/017-joja-agrochemical-tradeoff/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── agrochemical-application-service.md
│   ├── content-registration.md
│   └── multiplayer-gate3.md
└── tasks.md             # Phase 2 output (/speckit.tasks - NOT created here)
```

### Source Code (repository root)

```text
LivingRoots/
├── ModEntry.cs                               # composition root wiring only
├── Constants.cs                              # ModConstants additions (IDs/penalty/i18n keys)
├── Controllers/
│   └── ModController.cs                      # input intercept, host/farmhand gate, lock, refusal display, FR-009 broadcast
├── Domain/
│   └── Interfaces/
│       └── IAgrochemicalApplicationService.cs   # new contract (name finalized in implementation)
├── Services/
│   └── AgrochemicalApplicationService.cs        # new implementation (name finalized)
└── i18n/
    ├── default.json
    ├── en.json
    └── pt.json

LivingRoots.Tests/
└── *Agrochemical* / *ModController* tests    # TDD coverage for C1-C4, refusals, locks, broadcast, NaN baseline
```

**Structure Decision**: Extend the existing SMAPI mod solution in place. Domain contract in `LivingRoots/Domain/Interfaces`, implementation in `LivingRoots/Services`, orchestration in `LivingRoots/Controllers`, wiring/constants/i18n in their existing project locations. Tests in `LivingRoots.Tests`. No new projects/solutions; no Harmony fertilizer patch files.

## Complexity Tracking

No constitution violations requiring justification. MVP boundaries (host-only farmhand refusal, no Harmony hook, no `Data/Shops`/asset expansion by default, provisional tuning) are deliberate simplifications aligned with Principles V and VI.