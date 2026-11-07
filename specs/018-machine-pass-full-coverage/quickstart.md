# Quickstart Validation Guide: 018 — Machine-Pass Full Coverage

**Purpose**: Runnable validation scenarios and commands for the full-coverage plan (research-driven).
**Repo**: `/home/luis/development/stardew-raizes-vivas`
**Status**: research + plan + tasks authored; implementation is Waves 0–5 (not started). Nothing here attests a pass.

## Prerequisites (all waves)

- .NET 6 SDK; Stardew/SMAPI build dependencies via `GamePath` (per `AGENTS.md`).
- Feature branches aligned to the relevant spec/plan dirs.
- `scripts/verify-scenario.sh` (010 base + 016 hardening) and `scripts/lib/*`.
- `GamePath` env set for builds (e.g. `/home/luis/stardew-assemblies` or the Steam install).

## Static / automated foundation (Gate 1)

```bash
GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln --configuration Release
GamePath=/home/luis/stardew-assemblies dotnet test  Stardew-LivingRoots.sln --configuration Release
dotnet format Stardew-LivingRoots.sln --verify-no-changes
SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh
bash scripts/tests/test-017-claims.sh
```

Expected baseline (current tree, pre-Wave-0): build 0 errors; tests 1875 passed / 0 failed / 4 skipped; format exit 0; smoke 49 passed / 0 failed / 1 skipped; claims validator PASS (12 rows).

## Wave 0 — infra verification (must pass before Wave 1)

```bash
# T002: confirm debug split works headless
./scripts/verify-scenario.sh --scenario specs/010-headless-verification-runner/fixtures/valid/farmhand-host-guard.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
# T003: confirm place-bin fixture validates
./scripts/verify-scenario.sh --scenario specs/010-headless-verification-runner/fixtures/valid/actions-coverage.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
# T004: scenario schema + modVersion re-check
bash scripts/tests/test-017-agrochemical.sh
```

## Host-path scenarios (Wave 1)

```bash
# 017 unknown baseline (NaN -> 90)
./scripts/verify-scenario.sh --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-unknown-baseline.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
# 017 clamped (5 -> 0)
./scripts/verify-scenario.sh --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-clamped.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
# 017 measured-zero refusal (soil 0 -> refused, soil unchanged)
./scripts/verify-scenario.sh --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-refused-at-minimum.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
# 017 conflict (two applies, second refused)
./scripts/verify-scenario.sh --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-conflict.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
```

Each run's expected verdict: `pass` for the first two; `fail`/`partial` (matched expectedVerdict) for the refusal/conflict ones. Promote `test.md` only via the runner on a clean host pass; record bundles append-only.

## Farmhand-guard scenarios (Wave 1/2/3/4, only if W0.1 succeeds)

```bash
./scripts/verify-scenario.sh --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-farmhand-guard.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
```

Expected verdict: `partial` (capped by design — honesty W/R only, never `machine-pass`). Record as `partial` with the host-authority-guard-exercised note; never promote.

## 005 / 014 / 004 (Waves 2-3)

```bash
# 005 (currently partial -> run first)
./scripts/verify-scenario.sh --scenario specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
# 014 harvest matrix (already machine-pass; re-run only if code changed)
./scripts/verify-scenario.sh --scenario specs/014-soil-health-crop-quality/scenarios/soil-health-harvest-quality.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
# 004 compost hardening
./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
```

## 013 mulch (Wave 4, after W0 + new commands)

```bash
./scripts/verify-scenario.sh --scenario specs/013-mulch-soil-cover/scenarios/mulch-decay-exemption.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
```

## Offline verification of any bundle

```bash
./scripts/verify-scenario.sh --verify-bundle evidence-bundle-run-<id>
```

## Claims validator

```bash
bash scripts/tests/test-017-claims.sh   # PASS expected; 12+ rows; D1/human/SC-004 non-complete
```

## What this guide does NOT validate

- `d1-farmhand-convergence` (real farmhand) — blocked unless a two-instance harness is funded.
- `human-verified` — requires human attestation.
- SC-004 comprehension playtest — requires a real participant run with denominator/pass count.
- Render legibility / a11y / font fit — manual walk only.
- Balance (`P=10` provisional) — playtest only.
- Cross-machine determinism — same-machine only.

Record every run in the feature's `test.md`/`verification/` artifacts via the runner's promotion only; never hand-write `machine-pass`. Every number reported carries its blind spot (host-only, measured path only, etc.).