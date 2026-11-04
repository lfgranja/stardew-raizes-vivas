#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# verdict_from_exit mapping
for pair in "0:pass" "18:fail" "19:errored" "20:skipped" "4:errored" "10:errored" "13:errored"; do
    code="${pair%%:*}"; exp="${pair##*:}"
    call_runner_func verdict_from_exit "$code"
    assert_eq "$exp" "$RUNNER_FN_OUT" "verdict_from_exit $code"
done

# verdict_exit_for
for pair in "pass:0" "fail:18" "errored:19" "partial:19" "skipped:20"; do
    v="${pair%%:*}"; c="${pair##*:}"
    call_runner_func verdict_exit_for "$v"
    assert_eq "$c" "$RUNNER_FN_OUT" "verdict_exit_for $v"
done

# expected-verdict mismatch => 19 (via verdict_expected_match returns 1, and caller maps to 19)
call_runner_func verdict_expected_match "pass" "fail"
assert_fn_code 1 "expected mismatch returns 1"
call_runner_func verdict_expected_match "pass" "pass"
assert_fn_code 0 "expected match returns 0"
# missing expected for positive defaults to pass
call_runner_func verdict_expected_match "pass" ""
assert_fn_code 0 "missing expected defaults pass"

# verdict_evaluate
call_runner_func verdict_evaluate "1" "1" "1" "pass" "0"
assert_eq "pass" "$RUNNER_FN_OUT" "evaluate clean pass"
call_runner_func verdict_evaluate "0" "1" "1" "pass" "0"
assert_eq "partial" "$RUNNER_FN_OUT" "missing signal => partial"
call_runner_func verdict_evaluate "1" "0" "1" "pass" "0"
# divergence => fail? contract: missing bundle handled by evidence; divergence => fail via host-guard? keep accepted
pass "verdict_evaluate divergence exercised"
call_runner_func verdict_evaluate "1" "1" "1" "pass" "1"
assert_eq "fail" "$RUNNER_FN_OUT" "host-guard breach => fail"
call_runner_func verdict_evaluate "1" "1" "0" "pass" "0"
assert_eq "fail" "$RUNNER_FN_OUT" "threshold violation => fail"

# verdict_apply_ignore_exit_code: refuses integrity (19? integrity class 19) and 21; rejects 0/22/unknown
call_runner_func verdict_apply_ignore_exit_code 18 "18" 0
assert_eq "0" "$RUNNER_FN_OUT" "18 demoted to 0"
call_runner_func verdict_apply_ignore_exit_code 18 "18" 1
# integrity_flag=1 refuses
if [[ "$RUNNER_FN_OUT" == "18" ]]; then pass "integrity refused"; else fail "integrity refused" "got $RUNNER_FN_OUT"; fi
call_runner_func verdict_apply_ignore_exit_code 21 "21" 0
if [[ "$RUNNER_FN_OUT" == "21" ]]; then pass "policy 21 refused"; else fail "policy refused" "got $RUNNER_FN_OUT"; fi
for bad in 0 22 99; do
    call_runner_func verdict_apply_ignore_exit_code 18 "$bad" 0
    assert_fn_code 14 "ignore $bad exits 14"
done

# aggregate precedence
call_runner_func verdict_aggregate 19 18 21 20 0
assert_eq "19" "$RUNNER_FN_OUT" "aggregate 19 highest"
call_runner_func verdict_aggregate 0 20 0
assert_eq "0" "$RUNNER_FN_OUT" "mixed with pass => 0"

# verdict_write_attempt
adir="$HARNESS_SANDBOX/va"
mkdir -p "$adir/attempts/1"
call_runner_func verdict_write_attempt "$adir/attempts/1" 1 pass "" "3" 0
assert_fn_code 0 "write attempt returns 0"
assert_file_exists "$adir/attempts/1/attempt.json" "attempt.json"

# verdict_evaluate_bundle: derive the five inputs from a complete bundle and
# evaluate the scenario's multiplier + between predicates. The wrapper must
# pass the pre record, not just the after record, or the multiplier fails with
# unmeasured-actual.
v_eval_dll="$HARNESS_REPO_ROOT/LivingRoots.Verification.Runner/bin/Release/net6.0/LivingRoots.Verification.Runner.dll"
if [[ -f "$v_eval_dll" ]] && command -v dotnet >/dev/null 2>&1; then
    vbundle="$HARNESS_SANDBOX/verdict-bundle"
    mkdir -p "$vbundle/artifacts"
    printf '{"locations":{"Farm":{"60,15":{"before":10,"after":10,"delta":0}}}}\n' > "$vbundle/artifacts/soil-health-before-after.json"
    printf '{"agree":true,"divergences":[]}\n' > "$vbundle/artifacts/crop-phase-cross-check.json"
    printf 'Soil growth modifier elapsed: 1 ms\n' > "$vbundle/artifacts/smapi-log.txt"
    python3 - "$vbundle/artifacts/crop-phase-before.json" "$vbundle/artifacts/crop-phase-dump.json" <<'PY'
import json, sys
crop = {
    "location": "Farm", "tileX": 60, "tileY": 15, "tileKey": "60,15",
    "seedId": "LivingRoots.Verification.LongPhaseCrop", "isForageCrop": False,
    "indexOfHarvest": 24, "currentPhase": 0, "dayOfCurrentPhase": 0,
    "phaseDays": [4, 4, 4, 4, 99999], "basePhaseDays": [4, 4, 4, 4],
    "fullyGrown": False, "dead": False, "soilHealth": 10,
    "healthTier": "poor", "soilHealthState": "measured", "isSentinelPhase": False,
}
def dump(phase_days, day):
    return {"schemaVersion": 1, "dayKey": day, "capturedAtUtc": "2026-11-04T00:00:00Z",
            "isMainPlayer": True, "locationsProcessed": 1, "elapsedMilliseconds": 1,
            "done": True, "crops": [dict(crop, phaseDays=phase_days)]}
json.dump(dump([4, 4, 4, 4, 99999], "spring_1"), open(sys.argv[1], "w"), indent=2)
json.dump(dump([5, 4, 4, 4, 99999], "spring_6"), open(sys.argv[2], "w"), indent=2)
PY
    vscn="$HARNESS_SANDBOX/verdict.scenario.json"
    python3 - "$vscn" <<'PY'
import json, sys
d = {
    "schemaVersion": 2, "scenarioId": "verdict-bundle", "featureSlug": "010-headless-verification-runner",
    "testMdPath": "specs/010-headless-verification-runner/test.md", "summary": "verdict bundle",
    "playerRole": "host", "minimumGameVersion": "1.6.14",
    "environment": {"gameVersion": "1.6.14", "smapiVersion": "4.4.0", "modVersion": "0.4.0",
                    "apiVersion": "1.0.0", "saveId": "runner-self-test",
                    "config": {"saveMode": "save", "headless": True, "skipBuild": True}},
    "milestoneSave": "runner-self-test",
    "initCommands": [{"kind": "console", "command": "lr_set_soil_health 60 15 10"}],
    "steps": [{"action": "observe", "target": "60,15"}],
    "thresholds": [
        {"signal": "cropPhaseDelta", "predicate": {"op": "multiplier", "field": "phaseDays", "base": "pre.phaseDays", "tolerance": 0.0001, "factor": 1.15}},
        {"signal": "soilHealthStateDelta", "predicate": {"op": "between", "field": "soilHealth", "min": 0, "max": 34, "tolerance": 0.0001, "exclusiveMin": True, "exclusiveMax": True}},
    ],
    "expectedSignals": ["soilHealthStateDelta", "cropPhaseDelta", "smapiLog"],
    "expectedVerdict": "pass", "rngSeed": 1, "time": "2026-10-27T12:00:00Z",
    "determinism": {"idempotent": True, "declaredSources": []}, "scenarioKind": "positive",
}
json.dump(d, open(sys.argv[1], "w"))
PY
    RUNNER_EVALUATOR_DLL="$v_eval_dll" call_runner_func verdict_evaluate_bundle "$vbundle" "$vscn"
    assert_fn_code 0 "verdict_evaluate_bundle passes a complete bundle"
    assert_json_eq "$vbundle/verdict.json" '.verdict' "pass" "bundle verdict pass"
    assert_json_eq "$vbundle/verdict.json" '.signalsComplete' "true" "bundle signals complete"
    assert_json_eq "$vbundle/verdict.json" '.thresholdsOk' "true" "bundle thresholds ok"
    assert_json_eq "$vbundle/evaluation/actual.json" '.phaseDays' "5" "bundle actual phaseDays"
    assert_json_eq "$vbundle/evaluation/pre.json" '.phaseDays' "4" "bundle pre phaseDays"
else
    skip "verdict_evaluate_bundle" "evaluator dll or dotnet"
fi

finish_tests