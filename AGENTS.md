# Living Roots — Agent Guide

SMAPI mod for Stardew Valley (.NET 6, C# latest). Layered DDD: `Domain/` (contracts + pure logic, no SMAPI usings) → `Services/` (impls + persistence) → `Controllers/` (SMAPI handlers). Composition root: `LivingRoots/ModEntry.cs` (sole `new` site for services). Authoritative principles: `.specify/memory/constitution.md` (v2.0.0 — Principle VI: multiplayer is first-class). Nested layer notes: `LivingRoots/AGENTS.md`, `LivingRoots/Domain/AGENTS.md`, `LivingRoots/Services/AGENTS.md`.

## Commands

```bash
# Build — requires GamePath (see Prerequisite; check is unconditional)
GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln --configuration Release

# Tests — only LivingRoots.Tests is a test project; Verification/Runner ship no test SDK (build-only)
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release

# Single test subject
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release \
  --filter "FullyQualifiedName~SoilHealthServiceTests"

# Format (pre-commit gate)
dotnet format Stardew-LivingRoots.sln --verify-no-changes

# Restore (locked)
dotnet restore --locked-mode

# Smoke suite (launch-free; a file whose cases all skipped is reported SKIP, never PASS)
SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh
# single file, by bare name or path
scripts/tests/run-all.sh test-cli.sh

# Verification runner (real game, headless under Xvfb) — Gate 3 evidence
./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json \
  --no-acquire --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
# launch-free variants
./scripts/verify-scenario.sh --verify-bundle evidence-bundle-run-<id>
./scripts/verify-scenario.sh --check-docs

# Quality gate — local guide is docs/quality-gate.md; CI mirrors it in .github/workflows/quality-gate.yml
dotnet test Stardew-LivingRoots.sln -c Release --collect:"XPlat Code Coverage" --results-directory ./coverage
dotnet tool restore && dotnet reportgenerator -reports:'**/coverage.cobertura.xml' -targetdir:coverage-report -reporttypes:'JsonSummary;TextSummary'
PYTHONPATH=scripts/quality-gate python -m quality_gate coverage --summary coverage-report/Summary.json --baseline .coverage-baseline
PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests -q
npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 --reporters json --output jscpd-report --silent
PYTHONPATH=scripts/quality-gate python -m quality_gate duplication --report jscpd-report/jscpd-report.json --baseline .jscpd-baseline.json
semgrep scan --config p/csharp --error --severity ERROR --metrics off LivingRoots/
# baselines ratchet only in the improving direction: `python -m quality_gate ratchet-coverage|ratchet-duplication ...`
```

lefthook: `pre-commit` runs `dotnet format --verify-no-changes` + `bash -n`/`shellcheck` on staged `scripts/**/*.sh`; `pre-push` runs `dotnet test` (no config, no `GamePath` — fails unless `GamePath` is in your env) + the smoke suite. This clone's `.git/hooks/` may contain only samples — run `lefthook install` if hooks don't fire.

## Prerequisite: game assemblies

`Pathoschild.Stardew.ModBuildConfig` refuses to build without `GamePath` holding Stardew's managed assemblies; the check is unconditional, so `dotnet test`/`build`/`format` all need it. Point `GamePath` at assemblies (e.g. `/home/luis/stardew-assemblies` or the Steam install) or set `<GamePath>` in `LivingRoots.csproj`. `.gamepath/` is gitignored — never commit assemblies. CI `Build and Test` stays dormant unless `BUILDDEPS_ENABLED=true` + `BUILDDEPS_TOKEN` (private builddeps repo); do not promote `Check Formatting` to a required status check — it is formatting-only and the only always-on workflow. `Verify Scenario` CI is `continue-on-error` best-effort.

## Repo layout & generated artifacts

- Solution: `LivingRoots`, `LivingRoots.Tests`, `LivingRoots.Verification`, `LivingRoots.Verification.Runner` (last two are build-only, no test SDK).
- Repo root is full of gitignored verification artifacts (`evidence-bundle-run-*/`, `.venv-quality-gate/`, `coverage-report/`, `jscpd-report/`, `TestResults/`, most `*.log`). Use targeted `git add <file>` — never `git add -A`.
- `.specify/` holds specs, constitution, and the speckit toolchain; specs live in `specs/`.

## Architecture (non-obvious)

- `LivingRoots/Constants.cs` — `ModConstants` is the single source of all magic numbers; no hardcoded tile coords/health values elsewhere.
- `LivingRoots/Domain/` — contracts (`Interfaces/`) + pure logic (`Services/`, `Visualization/`); no SMAPI using statements.
- `LivingRoots/Services/` — application services implementing domain interfaces; persistence via `ModDataService` + `SanitizePathSegments()`.
- `LivingRoots/Controllers/ModController.cs` — SMAPI event orchestrator; atomic bit-flag state machine; `ExecuteWithConcurrencyGuard` for re-entrancy; snapshot data outside locks, then do I/O. Also owns FR-009/A8 soil-health MP sync: host broadcasts on `DayStarted` and after successful compost apply; farmhands apply via `ModMessageReceived` (`SoilHealthSyncMessage`).
- `LivingRoots/Patches/HarvestPatches.cs` + seam `Domain/IHarvestQualityService.cs` — Harmony harvest patches (static, try/catch fallback); feedback via `Services/Visualization/HarvestFeedbackTextures.cs`.
- `LivingRoots.Tests/` — xUnit + Moq + FsCheck + Microsoft.Coyote; one file per subject `{Subject}Tests.cs`.
- `scripts/verify-scenario.sh` + `scripts/lib/` — headless verification runner (spec 010): launches SMAPI under Xvfb, injects a file queue into `LivingRoots.Verification`, captures 3 signals (soil-health delta, crop phase/phaseDays delta, SMAPI log), evaluates verdict, assembles a schema-valid evidence bundle with detached SHA-256 anchor, and promotes `machine-pass` via `scripts/lib/promotion.sh`.

## Conventions (non-obvious)

- Sparse cache: never store default (0) health values.
- Async-only I/O: never `.Result`/`.Wait()`.
- No expression-bodied methods (`.editorconfig` forbids); overflow checking project-wide (`CheckForOverflowUnderflow=true`).
- `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true` (`Directory.Build.props`, AnalysisLevel 6.0-recommended) — build is strict; restore with `--locked-mode`.
- Multiplayer is first-class (constitution Principle VI): farm state host-authoritative/farm-scoped; atomic per-tile/per-machine locks with first-wins; single-player is the degenerate case. No second source of truth.
- Security: never log raw exception messages; validate soil health 0–100 via `ModConstants`; sanitize all user-influenced paths.
- Verification runner quirk: `save_prepare` rewrites `HOME`/`XDG_*` for the game process; `RUNNER_ORIGINAL_HOME` preserves the original home so Python helpers keep working. `RUNNER_SMAPI_CAPTURE=full|excerpt` (default `full`) controls log capture level.
- Constructor injection only; `ModEntry.cs` is the sole composition root.

## Process

After every file edit run `git add <file> && git commit -m "feat/fix/docs: <summary> — <files> — <why>"` (hook: `.specify/scripts/atomic-commit-hook.sh`, registered `after_analyze` in `.specify/extensions.yml`). Warning: the hook commits *everything staged* — run `git diff --cached --stat` first to confirm only your file is staged.

## Verification

Gate 3 (end-to-end) applies to any change touching runnable code: real SMAPI session + 3 signals (soil-health JSON delta, crop phase/phaseDays delta, SMAPI log). Never claim "verified by build / N passed." See `VERIFICATION_PROTOCOL.md`. `machine-pass` promotion happens only via `scripts/lib/promotion.sh`.

## Where to look

| Task | Location |
|------|----------|
| New game event handler | `Controllers/ModController.cs` |
| New domain concept | `Domain/Interfaces/` (contract) + `Services/` (impl); pure rules in `Domain/Services/` |
| Soil health limits | `Constants.cs` (`ModConstants`) |
| Health band edges (31/71) | `LivingRoots/Domain/AGENTS.md`, `Domain/Services/HealthCategory.cs` |
| New console command | `ModController.RegisterConsoleCommand()` |
| New render effect | `Services/Visualization/` via `IVisualizationService` (+ `Domain/Visualization/`) |
| Security validation | `ModDataService.SanitizePathSegments()` |
| Harmony harvest seam / feedback | `Patches/HarvestPatches.cs`, `Domain/IHarvestQualityService.cs`, `Services/Visualization/HarvestFeedbackTextures.cs` |
| Soil-health MP sync (FR-009/A8) | `Controllers/ModController.cs` |
| Composition / wiring | `ModEntry.cs` |
| Verification runner | `scripts/verify-scenario.sh` + `scripts/lib/` (evidence, save, signals, verdict, promotion) |
| Evidence bundle contract | `specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json` |
| Quality gate local guide | `docs/quality-gate.md` |
| Architecture/flow diagrams | `.archify/*/` — revalidate with `node ~/.agents/skills/archify/bin/archify.mjs finalize <type> <candidate.json> <output.html> --quality <profile> --repo-root .` (use `--out-dir` on reruns) |
