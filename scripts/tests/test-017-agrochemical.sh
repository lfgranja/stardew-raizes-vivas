#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

repo="${HARNESS_REPO_ROOT:-$HARNESS_REPO_ROOT}"
stage="$HARNESS_SANDBOX/017"
mkdir -p "$stage/macros"

# A macro that grants and applies the 017 validation-only agrochemical input.
# /sleep is required as the final line by injection_validate_macro and is
# dropped from the queue.
macro="$stage/macros/agrochemical-apply.txt"
printf '/lr_give_agrochemical 1\n/lr_apply_agrochemical 60 15\n/sleep\n' > "$macro"
call_runner_func injection_validate_macro "$macro"
assert_fn_code 0 "017 apply macro is a valid runner macro"

scenario="$stage/apply.scenario.json"
python3 - "$scenario" "macroFile" <<'PY'
import json,sys
d={
 "schemaVersion":3,
 "scenarioId":"agrochemical-runner-test",
 "featureSlug":"017-joja-agrochemical-tradeoff",
 "testMdPath":"specs/017-joja-agrochemical-tradeoff/test.md",
 "summary":"Agrochemical runner mapping test.",
 "playerRole":"host",
 "minimumGameVersion":"1.6.14",
 "environment":{"gameVersion":"1.6.14","smapiVersion":"4.4.0","modVersion":"0.4.0","apiVersion":"1.0.0","saveId":"farm-year1-day5","config":{"saveMode":"save","headless":True,"skipBuild":True}},
 "milestoneSave":"farm-year1-day5",
 "initCommands":[{"kind":"console","command":"lr_plant_crop 60 15 LivingRoots.Verification.LongPhaseCrop"},{"kind":"console","command":"lr_set_soil_health 60 15 50"}],
 "steps":[{"action":"runMacro","target":"60,15","inputs":{"macroFile":"macros/agrochemical-apply.txt"}},{"action":"observe","target":"60,15"}],
 "thresholds":[{"signal":"soilHealthStateDelta","predicate":{"op":"multiplier","field":"soilHealth","base":"pre.soilHealth","factor":0.8,"tolerance":0.0001},"description":"P=10 on a 50 measured baseline gives 40"}],
 "expectedSignals":["soilHealthStateDelta","cropPhaseDelta","smapiLog"],
 "timeoutSeconds":300,
 "inputs":{"season":"spring","day":5,"weather":"sunny","money":500},
 "rngSeed":123456789,
 "time":"2026-11-06T12:00:00Z",
 "scenarioKind":"positive",
 "expectedVerdict":"pass",
 "determinism":{"idempotent":True,"declaredSources":[]}
}
json.dump(d,open(sys.argv[1],"w"))
PY
call_runner_func scenario_validate "$scenario"
assert_fn_code 0 "runMacro scenario validates"

steps_queue="$stage/steps.queue"
call_runner_func injection_build_queue "$scenario" "$steps_queue" steps
assert_fn_code 0 "steps phase builds for a runMacro agrochemical step"
if [[ -s "$steps_queue" ]]; then
    grep -qx 'lr_give_agrochemical 1' "$steps_queue" && pass "steps grants the agrochemical" || fail "steps grant" "missing lr_give_agrochemical"
    grep -qx 'lr_apply_agrochemical 60 15' "$steps_queue" && pass "steps applies the agrochemical" || fail "steps apply" "missing lr_apply_agrochemical"
    grep -q 'sleep' "$steps_queue" && fail "sleep dropped" "sleep leaked into queue" || pass "sleep dropped from queue"
    grep -q 'runMacro' "$steps_queue" && fail "runMacro not queued as command" "runMacro leaked into queue" || pass "runMacro not queued as command"
else
    fail "steps queue created" "queue file missing"
fi

init_queue="$stage/init.queue"
call_runner_func injection_build_queue "$scenario" "$init_queue" init
assert_fn_code 0 "init phase still builds"
if [[ -s "$init_queue" ]]; then
    grep -qx 'lr_plant_crop 60 15 LivingRoots.Verification.LongPhaseCrop' "$init_queue" && pass "init plants the fixture crop" || fail "init crop" "missing fixture crop"
    grep -qx 'lr_set_soil_health 60 15 50' "$init_queue" && pass "init seeds measured 50" || fail "init soil" "missing measured 50"
    grep -q 'lr_give_agrochemical\|lr_apply_agrochemical' "$init_queue" && fail "init excludes grant/apply" "step leaked into init" || pass "init excludes grant/apply"
fi

# Property-style macro layouts: LF, CRLF, blank lines, and trailing spaces
# must all flatten to the same executable queue (apart from the dropped sleep).
for variant in lf crlf blank; do
    vmacro="$stage/macros/variant-$variant.txt"
    case "$variant" in
        lf)    printf '/lr_give_agrochemical 1\n/lr_apply_agrochemical 60 15\n/sleep\n' > "$vmacro" ;;
        crlf)  printf '/lr_give_agrochemical 1\r\n/lr_apply_agrochemical 60 15\r\n/sleep\r\n' > "$vmacro" ;;
        blank) printf '\n/lr_give_agrochemical 1\n\n/lr_apply_agrochemical 60 15\n\n/sleep\n' > "$vmacro" ;;
    esac
    vscn="$stage/variant-$variant.scenario.json"
    src="$stage/apply.scenario.json"
    python3 - "$src" "$vscn" "macros/variant-$variant.txt" <<'PY'
import json,sys
d=json.load(open(sys.argv[1]))
d["steps"]=[{"action":"runMacro","target":"60,15","inputs":{"macroFile":sys.argv[3]}},{"action":"observe","target":"60,15"}]
json.dump(d,open(sys.argv[2],"w"))
PY
    vq="$stage/variant-$variant.queue"
    call_runner_func injection_build_queue "$vscn" "$vq" steps
    assert_fn_code 0 "runMacro $variant variant builds"
    if [[ -s "$vq" ]]; then
        if diff -q <(grep -E '^lr_(give|apply)_agrochemical ' "$steps_queue") <(grep -E '^lr_(give|apply)_agrochemical ' "$vq") >/dev/null 2>&1; then
            pass "runMacro $variant variant flattens to the same commands"
        else
            fail "runMacro $variant variant" "commands differ"
        fi
    fi
done

# Missing macro file: loud failure, not an empty/partial queue.
missing="$stage/missing.scenario.json"
python3 - "$scenario" "$missing" <<'PY'
import json,sys
d=json.load(open(sys.argv[1]))
d["steps"]=[{"action":"runMacro","target":"60,15","inputs":{"macroFile":"macros/does-not-exist.txt"}}]
json.dump(d,open(sys.argv[2],"w"))
PY
call_runner_func injection_build_queue "$missing" "$stage/missing.queue" steps
if (( RUNNER_FN_CODE == 19 )); then pass "missing runMacro exits 19"; else fail "missing runMacro" "got $RUNNER_FN_CODE"; fi

# The real 017 scenario and its test record.
real_scenario="$repo/specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json"
real_test_md="$repo/specs/017-joja-agrochemical-tradeoff/test.md"
assert_file_exists "$real_scenario" "017 scenario exists"
assert_file_exists "$real_test_md" "017 test record exists"
if [[ -f "$real_test_md" ]]; then
    begin=$(grep -c 'GATE3:BEGIN' "$real_test_md" || true)
    end=$(grep -c 'GATE3:END' "$real_test_md" || true)
    assert_eq "1" "$begin" "test.md has exactly one GATE3:BEGIN"
    assert_eq "1" "$end" "test.md has exactly one GATE3:END"
fi
if [[ -f "$real_scenario" ]]; then
    call_runner_func scenario_validate "$real_scenario"
    assert_fn_code 0 "017 scenario validates"
fi

finish_tests
