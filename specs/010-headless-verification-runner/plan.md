# Implementation Plan: Headless Game Verification Runner

**Branch**: `010-headless-verification-runner` | **Date**: 2026-10-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-headless-verification-runner/spec.md`

## Summary

Build a reusable, unattended headless verification runner for Gate 3: a **bash orchestrator** (`scripts/verify-scenario.sh`) that reuses the spec-008 `smoke-validation.sh` substrate (Xvfb via `-displayfd`, process-group/signal discipline, run log under XDG state, exit-code taxonomy) to download/verify game files from a private repo (cache-first), launch Stardew Valley under SMAPI, deploy the mod plus a dev/CI-only `LivingRoots.Verification` mod, inject a scenario via SMAPI macros + save seeding + custom console commands, capture the three mandatory signals (SoilHealthState JSON delta, Crop phase delta, SMAPI log), evaluate thresholds, and emit a pass/fail/errored/partial/skipped/flaky verdict plus a MIRROR evidence bundle — updating `test.md`/`ROADMAP.md`/`COMPLETION_SUMMARY.md` on `pass`. The only production-code change is an SMAPI mod-provided API (`ILivingRootsModApi` + `SoilGrowthModifierCompleted` event) raised at the tail of `OnDayEnding`, so the verification mod can dump Crop phase deterministically without shipping debug code in the production binary (CWE-489).

## Technical Context

**Language/Version**: Bash 4+ (orchestrator) + C# / .NET 6 (mod + `LivingRoots.Verification` mod + scenario validation helpers), matching the repo's `net6.0` target and `ModBuildConfig 4.4.0`.

**Primary Dependencies**:
- Host tools: Xvfb, `gh`, `curl`, `sha256sum`, `jq`, `dotnet` SDK, `flock`, `mktemp`, `realpath` (reuse the spec-008 preflight's 31-external set).
- Game: Stardew Valley 1.6.x + SMAPI 4.4.0+ (single pinned stable line in `compat-matrix.json`).
- Mod: `LivingRoots` (production) + `LivingRoots.Verification` (dev/CI-only, depends on LivingRoots via manifest `Dependencies`).
- .NET validation: `JsonSchema.Net` (json-everything, MIT, .NET 6) for scenario validation; `check-jsonschema`/`ajv-cli` in CI/pre-commit.

**Storage**: Filesystem only — milestone saves + JSON save manifests in the private builddeps repo; scenario JSON + optional Markdown companion in the public repo; evidence bundles under `evidence-bundle-<run-id>/`; `compat-matrix.json`, `quarantine.json` in-repo; disposable save copies under per-instance `XDG_CONFIG_HOME` temp dirs.

**Testing**: xUnit + Moq for mod unit tests (`LivingRoots.Tests`); bash test suites under `scripts/tests/` (run-all, launch-free); smoke validation (`scripts/smoke-validation.sh`) for mod-load; manual SC-009 protocol retained as fallback.

**Target Platform**: Linux hosts (Xvfb + software GL) only; self-hosted runners for CI. Windows/macOS use build + unit tests + manual fallback (FR-021).

**Project Type**: CLI tool (bash orchestrator) + two SMAPI mods (production `LivingRoots`, dev-only `LivingRoots.Verification`).

**Performance Goals**: Per-location growth-modifier elapsed ms ≤ 5 ms (FR-013, single shot after one discarded warmup, Gen0 delta recorded); scenario runs complete unattended within the configured timeout.

**Constraints**:
- No proprietary game assemblies distributed in the public repo (NFR-002); acquisition via private repo + fine-grained PAT (`BUILDDEPS_TOKEN`).
- No debug/CWE-489 code in the production binary (FR-004/FR-009); dump hook lives only in the verification mod.
- Linux/Xvfb only (NFR-001); no Docker in Phase 1 (deferred per Regra dos Três).
- All constants from `ModConstants` (Principle V / project convention); no hardcoded health values or tile coordinates.
- Host-authoritative multiplayer preserved (Principle VI); farmhand path exercised via `debug split`.

**Scale/Scope**: Reusable across the 19 roadmap features and, aspirationally, other mods (A8); scenario contract minimal enough for a second feature to adopt without forking the runner (SC-004).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I (DDD)**: PASS. The only production-code change (`ILivingRootsModApi` + `SoilGrowthModifierCompleted` event) follows the DDD layering: interface in `Domain/Interfaces/`, raised from `ModController` (controller layer) after `ApplySoilGrowthModifier`. The verification mod is a separate SMAPI mod, not domain logic in controllers.
- **Principle II (Security-First)**: PASS. All filesystem paths (game archive, save copies, evidence bundle) sanitized/validated; checksum verification (SHA-256) mandatory and treated as a security event on mismatch; fine-grained PAT scoped to one repo; no raw exception messages in logs.
- **Principle III (Async-Only)**: PASS. Mod code uses async/await for I/O; bash orchestrator uses synchronous shell (appropriate for a shell tool, not game code). No `.Result`/`.Wait()`.
- **Principle IV (TDD)**: PASS with note. The verification mod and `ILivingRootsModApi` will have unit tests; the bash orchestrator has launch-free test suites under `scripts/tests/`. Red-Green-Refactor for the mod API.
- **Principle V (Simplicity/YAGNI)**: PASS. Bash orchestrator reuses existing substrate instead of inventing Docker/.NET-infra; Docker deferred (Regra dos Três); scenario contract minimal (Rule of Three for extraction to `schemas/scenarios/v1/`).
- **Principle VI (Multiplayer First-Class)**: PASS. The `SoilGrowthModifierCompleted` event carries `IsMainPlayer`; the farmhand path is tested via `debug split` (honesty level W/R only, E never auto-assigned). Host-authoritative lock per location preserved.

**Gate result: PASS** (no violations; Complexity Tracking empty).

## Project Structure

### Documentation (this feature)

```text
specs/010-headless-verification-runner/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── scenario.schema.json   # (to be authored; Draft 2020-12)
│   └── exit-codes.md          # (runner exit-code contract)
└── tasks.md             # Phase 2 output (/speckit-tasks — not created here)
```

### Source Code (repository root)

```text
scripts/
├── smoke-validation.sh          # existing substrate (reused)
├── verify-scenario.sh           # NEW: headless verification runner (bash orchestrator)
├── verify-evidence-bundle.sh    # NEW (Phase 1 stepping stone, FR-015): offline bundle verifier
└── tests/                       # launch-free test suites

LivingRoots/
├── Domain/
│   └── Interfaces/
│       └── ILivingRootsModApi.cs   # NEW: SMAPI-free mod-provided API (event + EventArgs)
├── Controllers/
│   └── ModController.cs            # MODIFY: raise SoilGrowthModifierCompleted at tail of OnDayEnding
└── (no other production changes)

LivingRoots.Verification/          # NEW: dev/CI-only SMAPI mod (separate csproj, excluded from release zip)
├── LivingRoots.Verification.csproj
├── manifest.json                  # Dependencies: lfgranja.LivingRoots
└── ModEntry.cs                    # subscribes to ILivingRootsModApi; writes Crop-phase dump

specs/010-headless-verification-runner/
├── contracts/
│   └── scenario.schema.json       # NEW: scenario contract (Draft 2020-12)
├── compat-matrix.json             # NEW: version matrix (in-repo, version-controlled)
└── (milestone-save manifests live in private builddeps repo)
```

**Structure Decision**: Single bash orchestrator reusing the spec-008 substrate, plus one new SMAPI mod (`LivingRoots.Verification`) and one new domain interface (`ILivingRootsModApi`). No new .NET console project, no Dockerfile in Phase 1. The verification mod is a peer SMAPI mod, not a .NET app, keeping the runner a single readable shell file and avoiding greenfield infrastructure.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*(No violations — table intentionally empty.)*
