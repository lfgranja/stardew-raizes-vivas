# Gate 3 Evidence — Spec 014 Soil Health → Crop Quality (T130)

**Status:** `machine-pass` evidence recorded for host-only spec 014 scenario. **NOT `verified`** (human E pending per `VERIFICATION_PROTOCOL.md` §3; agent must not write `verified`).  
**T109 D1 (real second player) remains BLOCKED** — headless runner is host-only; FR-009/A8 replicated deterministic sync implemented and unit-tested, but live farmhand convergence is not attested here.

## T130 verification commands (2026-11-06)

| Check | Result |
|---|---|
| `GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln --configuration Release` | PASS (0 errors) |
| `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --no-build --configuration Release` | PASS — **1739 passed, 0 failed, 4 skipped** (1743 total) |
| `dotnet format LivingRoots/LivingRoots.csproj --verify-no-changes --no-restore` | PASS (workspace warnings only) |
| `dotnet format whitespace` applied to affected projects; import order fixed in `ModController.cs` | PASS |
| `bash scripts/tests/test-band-scale-31-71.sh` (34/67 outside migration history) | PASS |

## Spec 014 Gate 3 scenario

Command:
```bash
./scripts/verify-scenario.sh --scenario specs/014-soil-health-crop-quality/scenarios/soil-health-harvest-quality.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
```

Evidence bundle: `evidence-bundle-run-20261106T005420Z-13f3` (duplicate run `evidence-bundle-run-20261106T005231Z-605c` also produced `verdict: pass`).

Bundle verification:
```bash
scripts/verify-evidence-bundle.sh evidence-bundle-run-20261106T005420Z-13f3
# exit 0
```

`verdict.json`: `{"verdict":"pass","actualVerdict":"pass","signalsComplete":true,"signalsAgree":true,"thresholdsOk":true,"expectedVerdict":"pass","farmhandBreach":false,"evaluatorMissing":false}`

`evaluation/actual.json` (key harvest-quality signals): poor forces quality 0 (vanilla 1, no extra); moderate preserves vanilla quality 1 (no extra); healthy upgrades quality 0→1 (`healthyQualityRoll` 0.2744) and grants extra harvest count 1 with extra quality 0 (`healthyExtraRoll` 0.0847). Threshold predicates in `evaluation/predicates/` match scenario expectations.

## T133 rerun (2026-11-06, after T131/T132)

**Status:** `machine-pass` evidence recorded (VERIFICATION ONLY). **NOT `verified`**. **T109 D1 remains BLOCKED** (real second-player/farmhand).

| Check | Result |
|---|---|
| `GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln --configuration Release` | PASS (0 errors) |
| `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --no-build --configuration Release` | PASS — **1747 passed, 0 failed, 4 skipped** (1751 total) after T131 FR-018 allow-list fix |
| `dotnet format LivingRoots/LivingRoots.csproj --verify-no-changes --no-restore` | PASS |
| `dotnet format LivingRoots.Tests/LivingRoots.Tests.csproj` applied (T131 test whitespace fixed), `--verify-no-changes --no-restore` | PASS |
| `bash scripts/tests/test-band-scale-31-71.sh` | PASS |

Spec 014 Gate 3 rerun:
```bash
./scripts/verify-scenario.sh --scenario specs/014-soil-health-crop-quality/scenarios/soil-health-harvest-quality.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
scripts/verify-evidence-bundle.sh evidence-bundle-run-20261106T014215Z-759d  # exit 0
```

`evidence-bundle-run-20261106T014215Z-759d` `verdict.json`: `{"verdict":"pass","actualVerdict":"pass","signalsComplete":true,"signalsAgree":true,"thresholdsOk":true,"expectedVerdict":"pass","farmhandBreach":false,"evaluatorMissing":false}`. `evaluation/actual.json` matches expected host-only signals (poor quality 0 / vanilla 1, no extra; moderate quality 1, no extra; healthy quality 1, extra 1, healthy extra quality 0; deterministic rolls `healthyQualityRoll` 0.2744, `healthyExtraRoll` 0.0847).

## Honest caveats

- **Not `verified`**: agent-recorded machine-pass only; no human E confirmation.
- **T109 D1 blocked**: real second-player/farmhand convergence not tested by headless runner.
- **Bundle metadata sparse**: `manifest.json`/`claims.json` may not populate all spec-016 honesty/claim fields in runner output; verifier accepted bundles (exit 0). Interpret as scenario machine-pass with limited claim metadata.
- Pre-existing unrelated worktree files were not committed by this work (`COMPLETION_SUMMARY.md`, `ROADMAP.md`, `specs/010-headless-verification-runner/promotion-triggers.json`, `specs/014-soil-health-crop-quality/test.md` remain modified/uncommitted).
