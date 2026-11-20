# Repository Guidelines

## Project Overview

**Living Roots** (`lfgranja.LivingRoots`) is a Stardew Valley SMAPI mod — an "agroecological expansion pack" in C#/.NET 6. It adds a per-tile **soil-health** stat (0–100) on tilled soil with color overlays/tooltips/hoe feedback, craftable **compost** and a multi-day **composting bin** (Empty → Processing → Ready), **mulch**, seasonal decay, a Joja **agrochemical** tradeoff (−10 health), and open-pollinated **crioulo/heirloom** parsnip seeds with saved-seed-per-harvest. Soil health also drives harvest quality and crop growth through Harmony seams.

Multiplayer is first-class (constitution v2.0.0, Principle VI): farm state is host-authoritative and farm-scoped; single-player is the degenerate case. Deliberate domain vocabulary: `SoilHealth`, `Compost`, `LivingSoil`, `HeirloomSeed`, `CompanionPlanting`.

Two facts that drive most logic and are easy to get wrong:
- **Unknown soil is `float.NaN`, not 0.** A real 0 is a "Poor" reading. Sparse caches omit default/0 entries; `TryGetSoilHealth` returns false for absent tiles.
- **Health bands are half-open: Poor [0,31), Moderate [31,71), Healthy [71,100]**; NaN/Inf → Unknown. Older 0–10 / 34-67 / 30-70 docs are superseded.

The solution has 4 projects: `LivingRoots` (the mod), `LivingRoots.Tests` (the **only** test project), `LivingRoots.Verification` and `LivingRoots.Verification.Runner` (build-only headless verification tooling, no test SDK).

## Architecture & Data Flow

Strict layering, enforced by `Architecture/DomainLayeringTests.cs`:
- `Domain/` — framework-free contracts + pure rules (no `using StardewModdingAPI` anywhere).
- `Services/` — application implementations + SMAPI persistence.
- `Controllers/ModController.cs` — all SMAPI event handlers.
- `LivingRoots/ModEntry.cs` — **sole composition root**; the only place services are constructed (`WireCompositionRoot`). All dependencies flow in via C# primary constructors. Fail closed on null deps — never `?? new` a second lock registry or service.

**Typical request flow** (right-click with compost/mulch/bin/agrochemical): `ModController.OnButtonPressed` → `TryApplyCompostWithLock` acquires a per-tile `IHostAtomicLockService` lock → `ICompostApplicationService.TryApplyCompost` → release in `finally` → host-only `BroadcastSoilHealthSnapshotIfHost()`. Refusals are translation keys localized via `TranslateRefusalMessage` then shown with `Game1.showRedMessage`. Held items always come from the single `IPlayerProvider.CurrentItem` source.

**Persistence:** `SoilHealthService.SetSoilHealth` mutates the cache under `_lock`, then snapshots/locks out before logging and raising `OnTileHealthChanged` (wired to `visualizationService.InvalidateCache()`). `IModDataService.SaveData` writes `"data/{sanitized}.json"` via SMAPI's `WriteJsonFile`; keys go through `SanitizePathSegments` → domain path-validation adapters. Same snapshot-then-IO pattern in `CompostingBinService`, `MulchApplicationService`.

**Multiplayer:** host broadcasts on `DayStarted`, after successful compost, and after heirloom writes; `SoilHealthSync.Apply` rejects other save IDs ("host wins, never across saves"), farmhands never mutate shared soil locally. Farmhand heirloom plant/harvest requests go to `ModController.HandleHeirloomBonusRequest` — validated, deduped (cap 256), host-locked, computed by pure `HeirloomSoilMath`, then broadcast.

**Two distinct locks — do not conflate:** the atomic `_state` bit-flag guard (`ExecuteWithConcurrencyGuard`, re-entrancy/double-dispatch protection via `Volatile.Read`/`Interlocked.CompareExchange`) and the gameplay `IHostAtomicLockService` per-tile/per-machine first-wins lock.

**Harmony seams:** `HarvestPatches` (`Crop.harvest` prefix/postfix) and `HeirloomPatches` (`HoeDirt.plant` postfix). Patch classes are static, `Initialize(...)`-wired from the composition root; bodies are always try/catch + `PatchTraceLog.Log` and never throw into the game.

**Content injection:** `ModEntry.OnAssetRequested` edits `Data/CraftingRecipes`, `Data/Objects`, `Data/Crops`, `Data/Shops` and registers named textures. `GetApi()` returns the stable `LivingRootsModApi` consumed by the verification mod.

## Key Directories

| Path | Purpose |
|---|---|
| `LivingRoots/` | Mod source. `ModEntry.cs` (entry/composition), `Constants.cs`, `Controllers/`, `Domain/`, `Services/`, `Patches/`, `i18n/`, `manifest.json`. |
| `LivingRoots/Domain/` | Contracts + pure logic at root (`ISoilHealthService.cs`, `SoilHealthSync.cs`, `HeirloomIdentity.cs`); `Interfaces/` (~11 contracts), `Models/` (persistence DTOs), `Services/` (pure math/rules), `Visualization/` (render DTOs). Not everything lives in a subfolder — check root first. |
| `LivingRoots/Services/` | Application services (`SoilHealthService`, `CompostingBinService`, `ModDataService`, providers, security adapters); `Visualization/` holds XNA renderers. |
| `LivingRoots/Controllers/`, `Patches/` | `ModController.cs` (~3.7k lines — use symbol names, not line guesses); Harmony seams. |
| `LivingRoots/i18n/` | `default.json`, `en.json`, `pt.json` only — all player-facing strings ship in all three. |
| `LivingRoots.Tests/` | xUnit tests: root files + subfolders (`Invariants/`, `Architecture/`, `Verification/`, `HarvestQuality/`, `Fixtures/`, `Stubs/`). |
| `LivingRoots.Verification/`, `LivingRoots.Verification.Runner/` | Dev/CI-only in-game mod + headless CLI. Reach production only via `ILivingRootsModApi` — do not edit for gameplay changes. |
| `scripts/` | `tests/` (bash smoke suite), `lib/` (runner libs), `quality-gate/` (Python gate), `ci/`, top-level gates (`release-gate.sh`, `verify-scenario.sh`). |
| `specs/`, `.specify/` | Feature specs `NNN-slug/` and SDD tooling/constitution/assessments/validations/releases. |
| `docs/` | `quality-gate.md`, verification playbooks, ADRs, governance manifests. |
| `.archify/*/` | Generated architecture/workflow diagrams (never hand-edit HTML; regenerate with `finalize`). |

## Development Commands

```bash
# Build (GamePath must hold game assemblies; -m:1 avoids host SIGABRT under memory pressure)
dotnet build Stardew-LivingRoots.sln --configuration Release -m:1
# On another host: GamePath=/path/to/assemblies dotnet build ...  (or restore builddeps zip into .gamepath/)

# Test (only LivingRoots.Tests has a test SDK)
dotnet test Stardew-LivingRoots.sln --configuration Release -m:1
dotnet test Stardew-LivingRoots.sln --configuration Release -m:1 --filter "FullyQualifiedName~SoilHealthServiceTests"

# Format / restore (locked restore is a hard gate)
dotnet format Stardew-LivingRoots.sln --verify-no-changes
dotnet restore --locked-mode

# Launch-free smoke suite (an all-skipped file reports SKIP, never PASS)
SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh
scripts/tests/run-all.sh test-cli.sh        # bare-name selection

# Real-game Gate 3 verification (headless SMAPI under Xvfb)
./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json \
  --no-acquire --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
./scripts/verify-scenario.sh --verify-bundle evidence-bundle-run-<id>   # offline verification
./scripts/verify-scenario.sh --check-docs                              # GATE3 marker check

# Authoritative local release gate (7 steps; no game launch, no Gate 3)
GamePath=/path/to/assemblies scripts/release-gate.sh
```

`-m:1` limits **MSBuild** node parallelism (a swap-exhausted host hit `SIGABRT` / `MSB4166` on parallel Release builds) — it is not xUnit parallelism.

## Code Conventions & Common Patterns

- **Constructor injection only**; `ModEntry.WireCompositionRoot` is the sole `new` site. Required deps use `?? throw new ArgumentNullException(...)`; optional deps fail closed.
- **Never log raw exception messages.** Log a generic Error/Warn line plus a Trace line carrying only `ex.GetType().FullName` and `HResult`. Sanitize any key before logging.
- **Snapshot under lock, then do I/O / logging / callbacks outside the lock.** Keep `lock` bodies short.
- **Sparse state:** never persist default/0 health; NaN means unknown. Never collapse unknown to zero except at explicit producer baselines (first compost, agrochemical/crioulo baseline = 100).
- **Async-only I/O:** no `.Result` / `.Wait()` in mod source.
- **Concurrency:** `Interlocked` / `Volatile` for the controller `_state`; `lock` for caches and dedupe maps; first-wins per-tile/machine locks.
- **Style:** 4-space indent, LF, UTF-8, final newline; `dotnet_sort_system_directives_first=true`. **No expression-bodied methods** (`.editorconfig` is `silent`; `dotnet format --verify-no-changes` enforces it in gates). No `max_line_length` is configured — don't invent one. Overflow checking is on project-wide; the only `unchecked(...)` is the intentional `unchecked(~flag)` flag-clear in `ModController`.
- **Strict analyzers:** `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisLevel=6.0-recommended`. Suppressions require a rule ID + documented reason; blanket `NoWarn` / `nosemgrep` / `.semgrepignore` and root-level severity `none` are forbidden.
- **Naming:** one subject per file, `{Subject}Tests.cs`; test methods `Method_Scenario_ExpectedResult`; spec/T numbers in comments (`T118`, `FR-009/A8`). No hard-coded magic numbers — all tunables/IDs/keys/bands live in `ModConstants` (`LivingRoots/Constants.cs`). Changing heirloom/agrochemical magnitudes means syncing Constants + all three locales + tests.
- **i18n parity:** every player-facing string ships in `default`, `en`, `pt`; tests assert identical key sets.
- **Commit process:** after each edit, `git add <file> && git commit -m "feat/fix/docs: <summary> — <file> — <why>"`. The `after_analyze` atomic-commit hook commits **everything staged** — run `git diff --cached --stat` first. Use targeted `git add`; never `git add -A` (repo root is full of gitignored artifacts). **Never hand-edit `GATE3:BEGIN`/`END` blocks** — runner-maintained.
- **Stored handler fields** (`_onButtonPressedHandler`, etc.) let `Dispose` detach the exact delegate; don't convert them to method groups.

## Important Files

| Path | Role |
|---|---|
| `LivingRoots/ModEntry.cs` | Entry point, composition root, asset edits, Harmony setup, `GetApi`, disposal (`Entry` swallows wiring failures — missing services degrade to fail-closed, not crash). |
| `LivingRoots/Constants.cs` | `ModConstants` — all tunables, IDs, i18n keys, band edges, MP message types. (Nested AGENTS.md wrongly calls it `ModConstants.cs`.) |
| `LivingRoots/Controllers/ModController.cs` | Event orchestration, `_state` machine, host locks, MP sync, input/rendering, console command `lr_version`. |
| `LivingRoots/Services/SoilHealthService.cs`, `ModDataService.cs` | Soil cache + sparse persistence; JSON persistence + path sanitization. |
| `LivingRoots/Domain/SoilHealthSync.cs`, `HeirloomIdentity.cs`, `Services/HeirloomSoilMath.cs`, `Visualization/HealthCategory.cs` | Pure MP envelope/rules, crioulo identity/write math, category classification. |
| `LivingRoots/Patches/{HarvestPatches,HeirloomPatches,PatchTraceLog}.cs` | Harmony seams + never-throw trace logging. |
| `LivingRoots/manifest.json`, `i18n/{default,en,pt}.json` | SMAPI metadata; three shipped locales. |
| `Stardew-LivingRoots.sln`, `Directory.Build.props`, `.editorconfig` | Gate target; strict analyzer policy; formatting rules. |
| `LivingRoots/LivingRoots.csproj` | net6.0, `EnableHarmony=true`, machine-specific `GamePath` default. |
| `dotnet-tools.json` (repo **root**, not `.config/`) | Pins `dotnet-reportgenerator-globaltool` 5.5.12. |
| `scripts/release-gate.sh`, `scripts/ci/aggregate-gate.sh` | 7-step local release gate; 4-step no-toolchain CI aggregate. |
| `.specify/memory/constitution.md`, `VERIFICATION_PROTOCOL.md`, `docs/quality-gate.md` | Authoritative principles, Gate-3 doctrine, local gate guide. |
| `specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json` | Evidence-bundle contract (detached SHA-256 is the offline trust root). |

## Runtime/Tooling Preferences

- **.NET:** no `global.json`; CI pins SDK **10.0.x** (verification workflow also installs 6.0.x); target framework `net6.0`, `LangVersion=latest`, `RollForward=Major`. Game assemblies come from private builddeps (`BUILDDEPS_TOKEN`); `GamePath` is mandatory for build/test/format.
- **NuGet** via `dotnet` CLI, default feeds, no central package management; `packages.lock.json` + `dotnet restore --locked-mode` are hard gates. Key pins: `Pathoschild.Stardew.ModBuildConfig 4.4.0`, xUnit 2.9.2, Moq 4.20.72, FsCheck 2.16.6, coverlet 10.1.0, `Microsoft.Coyote` 1.7.11 (referenced but unused).
- **Local tools:** `dotnet tool restore` then `dotnet reportgenerator ...` (manifest at root `dotnet-tools.json` — verify resolution in your shell). No `package.json`; `npx --yes jscpd@5.4.2` is used only by the duplication gate. Python 3.12 gate scripts run with `PYTHONPATH=scripts/quality-gate` (`semgrep==1.182.1` pinned in `requirements-ci.txt`).
- **Hooks:** lefthook — pre-commit runs `dotnet format --verify-no-changes` + `bash -n`/shellcheck on staged scripts; pre-push runs `dotnet test` + smoke. Run `lefthook install` if hooks don't fire.
- **CI trust boundary:** `sdd-gate` is the only required branch check on `dev` and runs with **no .NET toolchain and no secrets** — a green `sdd-gate` does not prove build/test/format. `build_and_test.yml` and `release.yml` are gated on `BUILDDEPS_ENABLED` + token; a red check saying `BUILDDEPS_TOKEN is not set` means provisioning, not a source regression. `verify-scenario.yml` is `continue-on-error` best-effort.
- **Diagrams:** regenerate with `node ~/.agents/skills/archify/bin/archify.mjs finalize <type> <candidate.json> <output.html> --quality <profile> --repo-root .` (add `--out-dir` on reruns).

## Testing & QA

- **Frameworks:** xUnit + `Microsoft.NET.Test.Sdk` (global `using Xunit;`), Moq, FsCheck/FsCheck.Xunit (`[Property(MaxTest=100)]`), coverlet (cobertura). `Microsoft.Coyote` is package-only — no systematic exploration. `LivingRoots.Tests/Usings.cs` and `LivingRoots.Tests/.editorconfig` (suppresses analyzer rules for tests) are notable.
- **Organization:** one file per subject (`CompostingBinServiceTests.cs`, `SoilHealthServiceTests.cs`, `ModDataServiceTests.cs`); methods `Method_Scenario_ExpectedResult`. Root files use `namespace LivingRoots.Tests;`; subfolders often use matching namespaces (`LivingRoots.Tests.Invariants`, `.Verification`, `.Architecture`, `.HarvestQuality`). A few legacy singular `*Test.cs` files remain — leave them.

  Test categories: unit (Moq + `Stubs/`), integration/contract, **architecture/governance source-scanning** (`Architecture/DomainLayeringTests.cs`, `ConstitutionGovernanceTests.cs`), **property-based** (`Invariants/` — arbitrary bounds derived from `ModConstants`), **concurrency/multiplayer** (`MultiplayerConcurrencyTests.cs`, `HostAtomicLockServiceTests.cs`), and **i18n parity** (`HeirloomI18nTests.cs`, `AgrochemicalContentParityTests.cs`).

- **Test gotchas:** `GameStateFixture.Install()` installs process-global `Game1` statics and is required in many constructors — omitting it fails inside game code. xUnit parallelism is disabled assembly-wide (`AssemblyInfo.cs`) because of that global state. `VerificationPipelineTests.cs` is *not* runner tests despite the name (it is SC-008 rollback tests); runner tests live in `LivingRoots.Tests/Verification/`.

- **Coverage & baselines:** `.coverage-baseline` (lines=76.9, branches=67.6); `.jscpd-baseline.json` (0% duplication). Baselines ratchet only in the improving direction (`ratchet-coverage` / `ratchet-duplication`; measured worse than baseline → exit 2). `coverlet.runsettings` forces cobertura and **excludes** verification projects, `ModController`, `ModDataService`, `HarvestPatches`, and other hard-to-test classes — the 76.9/67.6 figure does not measure them.

- **Gate 3 (end-to-end):** any change to runnable code needs a real SMAPI session with three independent signals — `soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog` (names in `scripts/lib/signals.sh`). Promotion to `machine-pass` is only via `scripts/lib/promotion.sh`; **`verified` is a human-only E attestation** and is never auto-assigned. Never claim "verified by build / N passed"; the "1447 passed / 0 failed" figure in specs is a documented false-confidence example, not a target. Runner `partial` (missing signal) → exit 19, never promoted. `RUNNER_SMAPI_CAPTURE=excerpt` is effectively a no-op (log is always fully captured), and `save_prepare` rewrites `HOME`/`XDG_*` (`RUNNER_ORIGINAL_HOME` preserves the real home for Python helpers).
