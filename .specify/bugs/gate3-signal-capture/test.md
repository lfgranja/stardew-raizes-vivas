# Bug Verification: Gate 3 runner now injects, captures, and judges

- **Slug**: gate3-signal-capture
- **Tested**: 2026-11-04
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The fix holds: the five orchestration gates now exist and are direct calls, the verification mod consumes the file queue and writes atomic dumps, and the final headless bundle records a `pass` verdict with three agreeing signals. A fresh end-to-end (Gate 3) re-run was **not** performed, because the milestone save `farm-year1-day5` was removed during cleanup. Without it, the fail-closed path returns `19` with `milestone save unavailable (farm-year1-day5)` — loud instead of silent, but not a full reproduction.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Orchestration wiring | grep `declare -F`, `save_prepare`, `injection_run`, `signals_capture`, `quit_run`, `verdict_evaluate_bundle` in `scripts/verify-scenario.sh` | pass | All five stages are direct calls with documented exit-code mapping; `verdict_evaluate` remains the tested 5-argument pure function. |
| Final bundle evidence | cat `evidence-bundle-run-20261104T082444Z-4acd/verdict.json`, `claims.json`, `evaluation/actual.json`, `evaluation/pre.json`, `artifacts/crop-phase-cross-check.json` | pass | verdict `pass`, `signalsComplete`/`signalsAgree`/`thresholdsOk` true, `farmhandBreach` false, honesty `R`; `phaseDays` 4→5, cross-check `agree: true`. |
| Promotion traceability | parse GATE3 block in `specs/004-playable-compost-loop/test.md`, resolve each signal path | pass | `machine-pass`, run ID `run-20261104T082444Z-4acd`, commit `d908b19` (exists in history), all four bundle paths resolve. |
| Reproduction (post-fix, headless) | `bash scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json --game-path "/home/luis/.steam/steam/steamapps/common/Stardew Valley" --skip-build --no-acquire --timeout 240` | skipped | Requires the host-local `farm-year1-day5` fixture, intentionally removed during cleanup; fails closed with exit `19`. Citing the committed bundle as evidence instead of a fresh run. |
| Targeted .NET | `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj -c Release --filter "FullyQualifiedName~QueueCommandParserTests\|FullyQualifiedName~HealthTierClassifierTests\|FullyQualifiedName~CropPhaseDump\|FullyQualifiedName~ModApiContractTests"` | pass | 43 passed, 0 failed. |
| Full .NET suite | `dotnet test Stardew-LivingRoots.sln -c Release --no-build` | partial | 1621 passed, 4 skipped, 1 failed — the pre-existing `ProductionSourceGuardTests.ProductionSources_ExceptionDetailOnlyAtTrace` at `ModController.cs:399`, untouched by this fix. |
| Shell suites | `test-injection.sh`, `test-signals.sh`, `test-verdict.sh`, `test-evaluation.sh`, `test-save-preservation.sh`, `test-verify-cli.sh`, `test-promotion.sh`, `test-cleanup.sh` (each in isolation) | pass | 24/24, 31/31, 36/36, 10/10, 19/19, 19/19, 8/8, 11/11 — 158/158. |
| Lint / type-check | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | pass | exit 0. |
| Host-fixture absence (fail-closed proof) | `ls ~/.config/StardewValley/Saves/ \| grep -c farm-year1-day5` | pass | 0 — the fixture is absent, matching the reported state. |

## Output Excerpts

```text
=== verdict ===
{
  "verdict": "pass",
  "actualVerdict": "pass",
  "signalsComplete": true,
  "signalsAgree": true,
  "thresholdsOk": true,
  "expectedVerdict": "pass",
  "farmhandBreach": false,
  "evaluatorMissing": false
}
=== cross-check ===
{"agree": true, "divergences": []}
```

```text
Aprovado!  – Com falha:     0, Aprovado:    43, Ignorado:     0, Total:    43
Com falha! – Com falha:     1, Aprovado:  1621, Ignorado:     4, Total:  1626
```

```text
$ bash scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json --game-path "/home/luis/.steam/steam/steamapps/common/Stardew Valley" --skip-build --no-acquire --timeout 240
runner: error: code=19 class=ERRORED message=milestone save unavailable (farm-year1-day5)
REPRO_EXIT=19
```

## Residual Risks

- The committed bundle proves the fix on this host, but a clean machine or CI checkout cannot reproduce it: `farm-year1-day5` is host-local, not a committed fixture or builddeps asset.
- Farmhand refusal is enforced at the verdict layer, not at runtime in the verification mod.
- One pre-existing full-suite failure (`ProductionSourceGuardTests`, `ModController.cs:399`) predates this fix and is documented in `fix.md` follow-ups.
- The scenario target moved from `-10,5` to `60,15`, and the stale `Farm/-10,5` entry remains in the committed bundle delta — it does not participate in either threshold.
- Other shell suites (`test-cli.sh`, `test-logparse.sh`, `test-preflight.sh`) have pre-existing failures unrelated to this fix; `test-display-launch.sh` can leak an Xvfb child.

## Recommendation

Hold the bug as fixed — the committed evidence is strong and every non-destructive check passes — but do not re-claim a fresh `verified` until the milestone fixture is staged or committed. To close the loop, re-stage `farm-year1-day5` from `Agroeco_421179919` (or pin it as a committed/builddeps fixture), re-run the Gate 3 command above, and confirm exit `0` with three agreeing signals; that run is the only item not actually exercised here.
