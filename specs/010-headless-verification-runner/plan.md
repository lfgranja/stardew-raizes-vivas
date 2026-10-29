# Implementation Plan: Headless Game Verification Runner

**Branch**: `010-headless-verification-runner` | **Date**: 2026-10-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-headless-verification-runner/spec.md`

## Summary

Build a reusable, unattended headless verification runner for Gate 3: a **bash orchestrator** (`scripts/verify-scenario.sh`, composed from the modules in `scripts/lib/`) that reuses the spec-008 `smoke-validation.sh` substrate (Xvfb via `-displayfd`, process-group/signal discipline, run log under XDG state, exit-code taxonomy) to download/verify game files from a private repo (cache-first), launch Stardew Valley under SMAPI, deploy the mod plus a dev/CI-only `LivingRoots.Verification` capture mod, inject a scenario via SMAPI macros + save seeding + custom console commands, capture the three mandatory signals (SoilHealthState JSON delta, Crop phase delta, SMAPI log), evaluate thresholds, and emit one of five base verdicts (`pass`, `fail`, `errored`, `partial`, `skipped`) plus the derived `flaky` mark, with a MIRROR evidence bundle. On a clean non-flaky machine-pass, the runner updates the feature `test.md` GATE3 block and the root GATE3 blocks; the documented status is `machine-pass`, never `verified`. `verified` is a maintainer attestation recorded afterward, never emitted by the machine.

Two supporting components keep the orchestrator thin without creating a second runner:

- `LivingRoots.Verification.Runner/` — a **net6.0 console helper** that performs deterministic, non-orchestrating work only: scenario validation, predicate evaluation, save diffing, archive extraction, and manifest building. It never launches the game, allocates a display, selects a verdict, or promotes documentation.
- `LivingRoots.Verification/` — a **capture-only SMAPI mod** that subscribes to `ILivingRootsModApi.SoilGrowthModifierCompleted` and writes the Crop-phase dump. It contains no scenario validation, threshold evaluation, manifest building, sanitization, save diffing, or extraction.

The only production-code change is an SMAPI mod-provided API (`ILivingRootsModApi` + `SoilGrowthModifierCompleted` event): the interface lives in `Domain/Interfaces/`, the implementation in `Services/`, `ModEntry.GetApi()` returns it, and `ModController` raises it at the tail of `OnDayEnding` after `ApplySoilGrowthModifier`. This lets the verification mod dump Crop phase deterministically without shipping debug code in the production binary (CWE-489).

## Technical Context

**Language/Version**: Bash 4+ (orchestrator + `scripts/lib/` modules) + C# / .NET 6 (production mod, capture mod, and helper console), matching the repo's `net6.0` target and `ModBuildConfig 4.4.0`.

**Primary Dependencies**:
- Host tools: Xvfb, `gh`, `curl`, `sha256sum`, `jq`, `dotnet` SDK, `flock`, `mktemp`, `realpath` (reuse the spec-008 preflight's 31-external set).
- Game: Stardew Valley 1.6.x + SMAPI 4.4.0+ (single pinned stable line in `compat-matrix.json`).
- Mod: `LivingRoots` (production) + `LivingRoots.Verification` (dev/CI-only, depends on LivingRoots via manifest `Dependencies`). The production manifest MUST declare `"MinimumGameVersion": "1.6.14"` and `"MinimumApiVersion": "4.4.0"`, matching the pinned compatibility line (A15/FR-002).
- .NET helper: `JsonSchema.Net` (json-everything, MIT, .NET 6) for Draft 2020-12 scenario and evidence-schema validation. `System.CommandLine` is **optional**: use it only if the helper's CLI surface outgrows hand-rolled argument parsing; it is not required for correctness.
- CI/pre-commit validation: `check-jsonschema` and `ajv-cli` alongside `JsonSchema.Net` (cross-validator agreement, C-SCN-38).

**Storage**: Filesystem for run-time state — milestone saves + JSON save manifests in the private builddeps repo; scenario JSON + optional Markdown companion in the public repo; evidence bundles under `evidence-bundle-<run-id>/`; `compat-matrix.json`, `quarantine.json`, `never-quarantine.json`, and `promotion-triggers.json` in-repo; disposable save copies under per-instance `XDG_CONFIG_HOME` temp dirs. **Retention**: audit logs and evidence bundles MUST be exported to a durable **WORM/object-lock or equivalent append-only store** for **≥ 365 days**, content-addressed via the manifest `subject` array plus the detached `manifest.sha256` anchor; application/step logs are retained **≥ 30 days**; GitHub Actions' 90-day artifact retention is not the durable store. Deletion before expiry requires documented human approval plus an audit event and is never automatic (FR-027/C-SEC-30). `scripts/lib/retention.sh` owns export metadata and the retention receipt; it never deletes early.

**Testing**:
- xUnit + Moq for mod unit tests (`LivingRoots.Tests`), including the `ILivingRootsModApi` contract and subscriber-exception isolation.
- **Microsoft Coyote** for deterministic concurrency scheduling/interleaving exploration of the managed API contract; **FsCheck** for property-based invariants over predicate truth tables, `SaveComparator`, sanitization, manifest digests, and the verdict state machine (all .NET 6-compatible; no Rust-only tooling).
- Bash launch-free test suites under `scripts/tests/` (run-all, no game launch), including module-level tests for each `scripts/lib/` unit.
- **Concurrency stress harness**: **K ≥ 30 concurrent instances** against the C-CON-21 aggregate criteria (`interleaving_rate = 0 ∧ correctness_rate = 1.0 ∧ sentinel_order_violations = 0`), plus a **1,000-iteration lock/rename loop** exercising `flock`, claim acquisition, atomic replace, and cleanup races (C-CON-19).
- JSON Schema contract tests over `fixtures/valid|invalid` with two independent validators; smoke validation (`scripts/smoke-validation.sh`) for mod-load; manual SC-009 protocol retained as fallback.
- Real-host end-to-end verification is a maintainer-run gate; absence of a host or required assemblies is recorded as `BLOCKED`, never as a pass.

**Target Platform**: Linux hosts (Xvfb + software GL) only; self-hosted runners for CI. Windows/macOS use build + unit tests + manual fallback (FR-021).

**Project Type**: CLI tool (bash orchestrator + modules) + one non-orchestrating .NET helper console + two SMAPI mods (production `LivingRoots`, dev-only `LivingRoots.Verification`).

**Performance Goals**: Per-location growth-modifier elapsed ms ≤ 5 ms (FR-013, single shot after one discarded warmup, Gen0 delta recorded); scenario runs complete unattended within the configured timeout.

**Constraints**:
- The helper is **not a second runner**: no game launch, no Xvfb/display ownership, no scenario orchestration, no verdict authority, and no documentation promotion. Verdict authority stays in `scripts/lib/verdict.sh`.
- No proprietary game assemblies distributed in the public repo (NFR-002); acquisition via private repo + fine-grained PAT (`BUILDDEPS_TOKEN`).
- No debug/CWE-489 code in the production binary (FR-004/FR-009); the dump hook lives only in the capture mod.
- Archive extraction MUST be in-process relative to the extracting component (FR-025): the helper's `ArchiveExtractor` performs member validation and extraction itself and MUST NOT shell out to `unzip`/`7z`/`tar`.
- Linux/Xvfb only (NFR-001); no Docker in Phase 1 (deferred per Regra dos Três).
- All constants from `ModConstants` (Principle V / project convention); no hardcoded health values or tile coordinates.
- Host-authoritative multiplayer preserved (Principle VI); farmhand path exercised via `debug split`.

**Scale/Scope**: Reusable across the 19 roadmap features and, aspirationally, other mods (A8); the scenario contract is minimal enough for a second feature to adopt without forking the runner (SC-004). **US1 targets feature `004-playable-compost-loop`; US2 targets a different feature slug, `005-soil-health-to-crop-growth`**, with its own `test.md` GATE3 block and scenario. US2 MUST NOT reuse feature 004's slug.

## Build/Test Prerequisite and `BLOCKED` Semantics

`Pathoschild.Stardew.ModBuildConfig` hard-fails when `STARDEW_GAME_PATH` does not point at a Stardew Valley install with SMAPI. The build/test prerequisite is therefore explicit:

- `STARDEW_GAME_PATH` MUST resolve to a directory containing the **six required assemblies**: `Stardew Valley.dll`, `StardewModdingAPI.dll`, `StardewValley.GameData.dll`, `MonoGame.Framework.dll`, `xTile.dll`, and `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll`.
- `scripts/lib/preflight.sh` verifies the path, the six assemblies, and the version matrix before any launch. It names the missing prerequisite but never a secret value.
- When the assemblies are absent, **T131 (build), T132 (test), and T133 (format)** MUST record **`BLOCKED`** with the exact missing prerequisite and the path/variable checked. `BLOCKED` is not a pass, not a waiver, and not a `verified` claim. The helper and any other independently buildable project MAY be built separately, but the solution-level result is still `BLOCKED`.
- A `BLOCKED` Gate 1 result MUST NOT be added to branch-protection required checks and MUST NOT be described as a green run.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I (DDD)**: PASS. The production API follows the layering: `Domain/Interfaces/ILivingRootsModApi.cs` defines the SMAPI-free contract (with `SoilGrowthModifierCompletedEventArgs`), `Services/LivingRootsModApi.cs` implements it with subscriber isolation, `ModEntry.GetApi()` returns it, and `ModController` calls `RaiseSoilGrowthModifierCompleted(args)` after the `ApplySoilGrowthModifier` loop. The helper console and capture mod are tooling/verification components outside the production domain, not domain logic in controllers.
- **Principle II (Security-First)**: PASS. All filesystem paths (game archive, save copies, evidence bundle) are sanitized/validated; checksum verification (SHA-256) is mandatory and treated as a security event on mismatch; extraction validates members in-process; the fine-grained PAT is scoped to one repo; no raw exception messages or secrets reach logs. C# remains authoritative for mod data; the shared `scripts/lib/sanitize.sh` implements the same contract for runner-owned inputs (C-SEC-31).
- **Principle III (Async-Only)**: PASS. Mod and helper code use async/await for I/O; bash uses synchronous shell (appropriate for a shell tool). No `.Result`/`.Wait()`.
- **Principle IV (TDD)**: PASS with note. The `ILivingRootsModApi` contract, capture mod, helper evaluator/validator/extractor/manifest builder, and bash modules each have launch-free tests; Red-Green-Refactor applies to the managed code. Real-host verification remains a separate attestation gate.
- **Principle V (Simplicity/YAGNI)**: PASS. The orchestrator remains the single entry point, decomposed into `scripts/lib/` modules rather than a framework. The helper exists only for work bash cannot do safely or precisely: Draft 2020-12 validation via `JsonSchema.Net`, numeric predicate/SaveComparator semantics, in-process archive extraction, and a single manifest producer. It does not orchestrate or decide. Docker remains deferred (Regra dos Três).
- **Principle VI (Multiplayer First-Class)**: PASS. The `SoilGrowthModifierCompleted` event carries `IsMainPlayer`; the farmhand path is tested via `debug split` (honesty level W/R only, E never auto-assigned). Host-authoritative lock per location is preserved. Such runs are documented as "host-authority guard exercised, transport not".

### Quality Gates (constitution rows, not only principles)

| Gate | Plan-specific criteria | Status at design time |
|------|------------------------|-----------------------|
| **Gate 1 — Static checks and tests** | `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` cover the solution including **both** `LivingRoots` and `LivingRoots.Verification` (and the helper); `bash -n` + `shellcheck` cover `scripts/`; schema fixtures pass both validators; if `STARDEW_GAME_PATH`/SMAPI assemblies are absent, T131/T132/T133 record `BLOCKED` with the missing prerequisite. T131/T132 MUST build/test/format both mod projects, not just the production mod. | PASS by design; execution attestation owed to T131–T133. |
| **Gate 2 — Product-level quality bar** | No verification/debug code ships in the production binary; `LivingRoots.Verification` is excluded from the release zip and verified absent; save/load round-trips preserve all data; no regression in the existing suite; user-facing behaviour is unchanged except the additive API/manifest metadata. | PASS by design; regression evidence owed to T132. |
| **Gate 3 — End-to-end verification** | Multi-signal in-game evidence under maintenance: SoilHealthState delta + Crop phase delta + SMAPI log. The machine records `partial` or `machine-pass` and writes paths/digests; it **never** writes `verified`. `verified` is a **maintainer-verified** attestation recorded after reviewing a clean non-flaky machine-pass with all three signals agreeing and honesty level ≥ R. | PASS by contract; real-host session and human attestation owed to T137/T148. |

**Gate result: PASS at design time** (no violations; Complexity Tracking empty). Gate execution is not claimed here.

## Project Structure

### Documentation (this feature)

```text
specs/010-headless-verification-runner/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── GOVERNANCE-ANNEX.md  # Phase 1 output: R/A/C/I ownership + tiered review cadence
├── traceability.md      # Phase 1 output: CHK → requirement → test case → code location
├── SCENARIO-AUTHORING.md # NEW: JSON-first scenario authoring, subject selection, expected verdict rules
├── VERIFICATION.md      # NEW: exact verification commands, expected outputs, bundle path
├── contracts/           # Phase 1 output
│   ├── scenario.schema.json          # Authored; Draft 2020-12, schemaVersion 2
│   ├── evidence-bundle.schema.json   # NEW: machine-readable projection of the spec-009 bundle layout
│   ├── exit-codes.md                 # Runner exit-code contract
│   └── test-md-template.md           # GATE3 block template for feature test.md files
├── fixtures/            # Scenario fixtures for contract tests
│   ├── valid/           # ≥5 schema-valid scenarios
│   └── invalid/         # ≥5 schema-invalid scenarios (one failure each)
├── scenarios/           # NEW: runner self-test/reference scenarios (feature scenarios live with their feature)
├── never-quarantine.json # NEW: closed non-quarantinable class set (C-REL-23), CODEOWNERS-gated
├── promotion-triggers.json # NEW: machine-readable FR-017/FR-015/C-SEC-23 promotion triggers
├── compat-matrix.json   # Pinned game/SMAPI/mod compatibility line + failure classes
├── gamepath.lock.json   # Authoritative game-archive SHA-256 + per-file digest map
├── quarantine.json      # Committed quarantine registry
└── tasks.md             # Phase 2 output (/speckit-tasks — not created here)
```

`contracts/evidence-bundle.schema.json` is a **projection**, not a second evidence-bundle contract: the authoritative bundle layout remains `specs/009-end-to-end-game-verification/contracts/evidence-bundle.md`, and the schema is kept in lockstep with `data-model.md` and `C-BND-01`/`C-SEC-29`. It validates field presence/types/strictness; it does not define different semantics.

### Source Code (repository root)

```text
scripts/
├── smoke-validation.sh          # existing substrate (reused)
├── verify-scenario.sh           # NEW: single entry point; parses args and dispatches to scripts/lib/*
├── verify-evidence-bundle.sh    # NEW (Phase 1 stepping stone, FR-015): offline bundle verifier
├── lib/                         # NEW: modular implementation of the single orchestrator
│   ├── common.sh                # shared constants, log init, die/exit helpers
│   ├── atomic.sh                # temp → fsync → rename → fsync(parent); torn-write prevention
│   ├── concurrency.sh           # advisory locks, claim files, PID identity tokens
│   ├── sanitize.sh              # NFC, traversal, drive letters, backslash, NUL/control, homoglyph
│   ├── logging.sh               # redaction, JSONL audit log vs application log
│   ├── version-gate.sh          # compat-matrix classification (SemanticVersion)
│   ├── scenario-validate.sh     # invokes helper ScenarioValidator + fixture parity
│   ├── acquisition.sh           # cache-first GitHub Releases download, checksum, retry
│   ├── preflight.sh             # host tools, STARDEW_GAME_PATH, six assemblies
│   ├── display.sh               # Xvfb -displayfd lifecycle and cleanup
│   ├── save.sh                  # disposable copy, hash/restore, sidecar backup
│   ├── launch.sh                # SMAPI launch and ready-state sentinels
│   ├── injection.sh             # macros, console commands, save seeding
│   ├── signals.sh               # collect 3 signals + dump/save cross-check
│   ├── verdict.sh               # five base verdicts + derived flaky + exit mapping
│   ├── evidence.sh              # bundle/attempt dirs, artifact collection, helper invocation
│   ├── promotion.sh             # GATE3/root-doc marker injection, --check-docs
│   ├── cleanup.sh               # traps, process-group teardown, temp purge
│   ├── retry.sh                 # classification, 3 attempts, 5 s cooldown
│   ├── quarantine.sh            # registry, time-box, shadow passes
│   ├── performance.sh           # warmup discard, 5 ms gate, Gen0 delta
│   ├── suite.sh                 # discovery, aggregation, suite rerun cap
│   ├── ci.sh                    # CI-mode detection/flags
│   ├── reporters.sh             # JUnit XML + Allure JSON
│   └── retention.sh             # durable WORM export + retention metadata
└── tests/                       # launch-free test suites per module + schema/stress tests

LivingRoots/
├── ModEntry.cs                     # MODIFY: GetApi() returns ILivingRootsModApi
├── Domain/
│   └── Interfaces/
│       └── ILivingRootsModApi.cs   # NEW: SMAPI-free interface + SoilGrowthModifierCompletedEventArgs
├── Services/
│   └── LivingRootsModApi.cs        # NEW: implementation; isolates subscriber exceptions
├── Controllers/
│   └── ModController.cs            # MODIFY: calls RaiseSoilGrowthModifierCompleted(args) at OnDayEnding tail
└── (no other production changes)

LivingRoots.Verification/          # NEW: capture-only dev/CI SMAPI mod (excluded from release zip)
├── LivingRoots.Verification.csproj
├── manifest.json                  # Dependencies: lfgranja.LivingRoots; no update keys
├── ModEntry.cs                    # subscribes to ILivingRootsModApi; write dump; isolate exceptions
├── Evidence/
│   ├── CropPhaseDump.cs           # dump payload shape and enumeration
│   └── CropPhaseDumpWriter.cs     # temp → fsync → atomic rename → parent fsync → .done last
└── Validation/
    └── DumpSchema.cs              # dump payload shape/enum constants; no scenario or threshold logic

LivingRoots.Verification.Runner/   # NEW: net6.0 console helper; NOT a runner
├── LivingRoots.Verification.Runner.csproj  # net6.0, Exe, Nullable, CheckForOverflowUnderflow
├── Program.cs                     # CLI verbs: validate-scenario, evaluate, extract, build-manifest
├── Validation/
│   └── ScenarioValidator.cs       # Draft 2020-12 via JsonSchema.Net; field-level errors
├── Evaluation/
│   ├── PredicateEvaluator.cs      # cmp/between/multiplier/sentinel/unchanged/allOf/anyOf/not
│   └── SaveComparator.cs          # 0.0001f tolerance, missing→0 only at persistence layer, NaN ignored
├── Extraction/
│   └── ArchiveExtractor.cs        # in-process member validation + extraction; no shell-out
└── Evidence/
    └── ManifestBuilder.cs         # SOLE producer of manifest.json + manifest.sha256 + per-attempt manifests

specs/004-playable-compost-loop/
├── test.md                        # existing GATE3 target (US1)
└── scenarios/
    └── compost-growth-hardening.scenario.json  # US1 scenario

specs/005-soil-health-to-crop-growth/   # US2 second feature; different slug from 004
├── test.md                        # NEW: exactly one GATE3 marker block
└── scenarios/
    └── soil-health-growth.scenario.json      # NEW: US2 scenario, no runner-source edits

specs/010-headless-verification-runner/
├── contracts/                     # see Documentation above
├── fixtures/                      # see Documentation above
├── scenarios/                     # reference/self-test scenarios
├── never-quarantine.json          # closed class set; human-approved changes only
├── promotion-triggers.json        # deferred-component triggers (no new thresholds)
├── compat-matrix.json
├── gamepath.lock.json
└── quarantine.json
```

### Structure Decision

The bash orchestrator remains the **single entry point** (`scripts/verify-scenario.sh`) for acquiring, launching, injecting, capturing, evaluating, deciding, and promoting. It is **modular, not one literal file**: argument parsing and dispatch live in the entry point, and each concern lives in its own `scripts/lib/*.sh` unit so the implementation can be reviewed and tested without a monolith. The orchestrator, not the helper, owns process lifetime, display allocation, retry, verdict selection, and documentation promotion.

The helper `LivingRoots.Verification.Runner` is a narrowly scoped console process, not a second runner: it has no launch/orchestration/verdict authority and is invoked as a deterministic subprocess for validation, evaluation, extraction, and manifest building. `LivingRoots.Verification` is a peer SMAPI mod with capture-only responsibilities. Docker remains out of Phase 1 (Regra dos Três), and the second-feature proof uses `specs/005-soil-health-to-crop-growth/` without changing runner source.

### Phase 1 — Build Wiring and Gate 1

1. Add the **two new C# projects** to `Stardew-LivingRoots.sln`: `LivingRoots.Verification` and `LivingRoots.Verification.Runner`. Add `LivingRoots.Verification` as a `ProjectReference` of `LivingRoots.Tests` so its contract tests run in the normal suite.
2. T131 (build), T132 (test), and T133 (format) operate on the solution and MUST explicitly build/test/format **both** mod projects (`LivingRoots` and `LivingRoots.Verification`), plus the helper. Record a per-project result, not only an aggregate.
3. If the game/SMAPI assemblies are absent, T131/T132/T133 record `BLOCKED` with the exact missing prerequisite (see above). A partially buildable solution is reported as `BLOCKED`, never as a green Gate 1.
4. Keep `LivingRoots.Verification` out of the release zip. The build may produce it locally; packaging and CI MUST verify it is absent from the shipped archive (C-SEC-32).

## Evidence Bundle Production

**One producer rule:** `LivingRoots.Verification.Runner/Evidence/ManifestBuilder.cs` is the **sole producer** of `manifest.json`, the detached `manifest.sha256`, and each per-attempt `attempts/<n>/manifest.json`. `scripts/lib/evidence.sh` only orchestrates: it creates the run/attempt directories, gathers artifact paths, invokes the helper, and checks the helper's exit status. It MUST NOT hand-write manifest JSON, compute a competing digest, or edit the manifest after finalization. `LivingRoots.Verification` MUST NOT write the manifest, claims, verdict, or digest anchor.

The manifest includes at least:

- identity/environment: `schemaVersion`, `runId`, `timestamp`, `toolVersions`, `gameVersion`, `modVersion`, `environment`, `environmentOverrides`, `expectedSignals` (copied from the scenario);
- integrity/subject: `subject[]` (`{name, digest:{sha256}}`) covering every artifact, including per-attempt manifests; `files` only as the deprecated one-major alias;
- evidence provenance: `compatibility` (observed/minimum/maximum/tier per component, `modSelfCheck`, deviation), `commands` (argv arrays, workingDir, timestamps, exitCode, stdout/stderr refs), `materials` (ResourceDescriptor with roles `game-file`, `mod-manifest`, `tool`, `seed-save`, `dataset-instance`), `seeds` (≥1 for R/E, with `seedId`, `source`, `baseCommit`/`baseSaveHash`), `determinism`, and completeness flags (`commandsCompleteness`, `materialsCompleteness`, `seedsCompleteness`);
- outcome: `verdict`, `honestyLevel`, `attempts[]` (only `attempts/<n>/attempt.json` paths), optional `integrityFailureRef`.

`manifest.sha256` is written **after** the top-level manifest is finalized and lives outside `manifest.json`. Each attempt also has its own `attempts/<n>/manifest.json`; per-attempt manifests are listed in the top-level `subject`, never in `attempts[]`. `attempts/warmup/` is retained but is not an attempt and never appears in `attempts[]` (C-BND-01).

## Verdict and Promotion Semantics

- The verdict set is closed: `pass`, `fail`, `errored`, `partial`, `skipped`; `flaky` is derived metadata, not a sixth verdict (C-REL-01/C-REL-26).
- The machine's terminal documentation states are **`partial`** and **`machine-pass`**. `machine-pass` is a documentation status, not a verdict; the underlying verdict is `pass`. The machine never writes `verified`.
- On a clean non-flaky `machine-pass` with all three signals agreeing, honesty level ≥ R, and matching `expectedVerdict`, `scripts/lib/promotion.sh` updates only the `<!-- GATE3:BEGIN -->` … `<!-- GATE3:END -->` blocks in the feature `test.md`, `ROADMAP.md`, and `COMPLETION_SUMMARY.md`, recording evidence paths and digests.
- **`verified` is a maintainer attestation** recorded after reviewing that machine-pass. It requires the same evidence preconditions; it is never auto-assigned and never written by the runner. A flaky pass, a partial/errored/fail run, or a quarantined run stays at `partial`.
- This preserves FR-016's promotion preconditions (three agreeing signals, clean non-flaky pass, honesty ≥ R) while fixing the actor/status wording: the machine produces `machine-pass`; the human records `verified`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*(No violations — table intentionally empty.)*
