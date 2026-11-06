# Living Roots — Agent Guide

SMAPI mod for Stardew Valley (.NET 6, C# latest). Layered DDD: `Domain/` (contracts + pure logic) → `Services/` (impls + persistence) → `Controllers/` (SMAPI handlers). Composition root: `LivingRoots/ModEntry.cs` (sole `new` site for services). Authoritative principles: `.specify/memory/constitution.md`. Nested layer notes: `LivingRoots/AGENTS.md`, `LivingRoots/Domain/AGENTS.md`, `LivingRoots/Services/AGENTS.md`.

## Commands

```bash
# Build — requires GamePath (see Prerequisite below)
GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln --configuration Release

# Tests — only LivingRoots.Tests is a test project; Verification/Runner are build-only
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release

# Single test subject
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release \
  --filter "FullyQualifiedName~SoilHealthServiceTests"

# Format (pre-commit gate)
dotnet format Stardew-LivingRoots.sln --verify-no-changes

# Restore (locked)
dotnet restore --locked-mode

# Smoke suite (launch-free)
SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh
# single file
scripts/tests/run-all.sh test-cli.sh

# Verification runner (real game, headless)
./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
# offline bundle check
./scripts/verify-scenario.sh --verify-bundle evidence-bundle-run-<id>
# docs marker check
./scripts/verify-scenario.sh --check-docs

# Quality gate (merge-blocking; needs coverage + reportgenerator first)
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m pytest scripts/quality-gate/tests -q
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m quality_gate coverage --summary coverage-report/Summary.json --baseline .coverage-baseline
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m quality_gate duplication --report jscpd-report/jscpd-report.json --baseline .jscpd-baseline.json
.venv-quality-gate/bin/semgrep scan --config p/csharp --error --severity ERROR --metrics off LivingRoots/
```

lefthook: pre-commit runs `dotnet format --verify-no-changes` + shell syntax/static checks; pre-push runs `dotnet test` + smoke suite (`SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh`).

## Prerequisite: game assemblies

`Pathoschild.Stardew.ModBuildConfig` refuses to build without `GamePath` holding Stardew's managed assemblies. Local: set env `GamePath` (e.g. `/home/luis/stardew-assemblies` or Steam install) or `<GamePath>` in `LivingRoots.csproj`. The `.gamepath/` dir is gitignored — never commit assemblies. CI `Build and Test` stays dormant unless `BUILDDEPS_ENABLED=true` + `BUILDDEPS_TOKEN`; do not make `Check Formatting` a required status check alone.

## Architecture

- `LivingRoots/Constants.cs` — `ModConstants` is the single source of all magic numbers; no hardcoded tile coords/health values elsewhere.
- `LivingRoots/Domain/` — contracts (`Interfaces/`) + pure logic (`Services/`, `Visualization/`); no SMAPI using statements.
- `LivingRoots/Services/` — application services implementing domain interfaces; persistence via `ModDataService` + `SanitizePathSegments()`.
- `LivingRoots/Controllers/ModController.cs` — SMAPI event orchestrator; atomic bit-flag state machine; `ExecuteWithConcurrencyGuard` for re-entrancy; snapshot data outside locks, then do I/O.
- `LivingRoots/Patches/HarvestPatches.cs` + seam `Domain/IHarvestQualityService.cs` — Harmony harvest patches (static, try/catch fallback); scope guards (forage/Qi/fibre) and tier outcomes; feedback via `Services/Visualization/HarvestFeedbackTextures.cs` (deterministic 16×16 procedural pixels, headless-safe lazy `Texture2D`).
- `LivingRoots/Controllers/ModController.cs` also owns FR-009/A8 soil-health sync: host broadcasts snapshot on `DayStarted` and after successful compost apply; farmhands apply via `ModMessageReceived` (`SoilHealthSyncMessage`), host guards farm-state mutation.
- `LivingRoots.Tests/` — xUnit + Moq + FsCheck + Microsoft.Coyote; one file per subject `{Subject}Tests.cs`.
- `scripts/verify-scenario.sh` — headless verification runner (spec 010): launches SMAPI under Xvfb, injects a file queue into `LivingRoots.Verification`, captures 3 signals (soil-health delta, crop phase/phaseDays delta, SMAPI log), evaluates verdict, assembles a schema-valid evidence bundle with detached SHA-256 anchor, and promotes `machine-pass` via `scripts/lib/promotion.sh`.
- `scripts/lib/` — shared runner libraries (evidence, save, signals, verdict, promotion, retention, cleanup, quit, etc.). `ModEntry.cs` is the sole composition root for services; `scripts/lib/evidence.sh` is the sole manifest producer's input assembler.
- `specs/010-headless-verification-runner/` — spec, contracts, fixtures, verification logs, Gate 3 evidence.

## Conventions (non-obvious)

- Sparse cache: never store default (0) health values.
- Async-only I/O: never `.Result`/`.Wait()`.
- No expression-bodied methods (`.editorconfig` forbids); overflow checking project-wide (`CheckForOverflowUnderflow=true`).
- `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true` — build is strict; restore with `--locked-mode`.
- Multiplayer is first-class (constitution Principle VI): farm state host-authoritative/farm-scoped; atomic per-tile/per-machine locks with first-wins; single-player is the degenerate case. No second source of truth.
- Security: never log raw exception messages; validate soil health 0–100 via `ModConstants`; sanitize all user-influenced paths.
- Verification runner quirk: `save_prepare` rewrites `HOME`/`XDG_*` for the game process; `RUNNER_ORIGINAL_HOME` preserves the original home so Python helpers (e.g., the offline verifier's `jsonschema` import) keep working. `RUNNER_SMAPI_CAPTURE=full|excerpt` (default `full`) controls log capture level.
- Constructor injection only; `ModEntry.cs` is the sole composition root; primary constructors (C# 12) on controllers.
- Don't: `new` services outside `ModEntry.cs`; mutate `_state` without `Interlocked`; hold a lock during I/O; implement domain logic in controllers; suppress overflow checks; expose raw exception messages in logs.

## Process

After every file edit run `git add <file> && git commit -m "feat/fix/docs: <summary> — <files> — <why>"` (hook: `.specify/scripts/atomic-commit-hook.sh`, registered `after_analyze` in `.specify/extensions.yml`). Warning: the hook commits *everything staged* — run `git diff --cached --stat` first to confirm only your file is staged.

## Verification

Gate 3 (end-to-end) applies to any change touching runnable code: real SMAPI session + 3 signals (soil-health JSON delta, crop phase/phaseDays delta, SMAPI log). Never claim "verified by build / N passed." See `VERIFICATION_PROTOCOL.md`.

## Where to look

| Task | Location |
|------|----------|
| New game event handler | `Controllers/ModController.cs` |
| New domain concept | `Domain/Interfaces/` (contract) + `Services/` (impl); pure rules in `Domain/Services/` |
| Soil health limits | `Constants.cs` (`ModConstants`) |
| New console command | `ModController.RegisterConsoleCommand()` |
| New render effect | `Services/Visualization/` via `IVisualizationService` (+ `Domain/Visualization/`) |
| Security validation | `ModDataService.SanitizePathSegments()` |
| Harmony harvest seam / feedback | `Patches/HarvestPatches.cs`, `Domain/IHarvestQualityService.cs`, `Services/Visualization/HarvestFeedbackTextures.cs` |
| Soil-health MP sync (FR-009/A8) | `Controllers/ModController.cs` (`BroadcastSoilHealthSnapshotIfHost`, `OnModMessageReceived`) |
| Composition / wiring | `ModEntry.cs` |
| Verification runner | `scripts/verify-scenario.sh` + `scripts/lib/` (evidence, save, signals, verdict, promotion) |
| Evidence bundle contract | `specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json` |
| Gate 3 evidence | `specs/010-headless-verification-runner/gate3-evidence/` + `verification/` |
| Nested layer notes | `LivingRoots/AGENTS.md`, `LivingRoots/Domain/AGENTS.md`, `LivingRoots/Services/AGENTS.md` |
