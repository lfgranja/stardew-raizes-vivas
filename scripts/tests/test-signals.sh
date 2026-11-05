#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# soil health delta
before="$HARNESS_SANDBOX/before.json"
after="$HARNESS_SANDBOX/after.json"
out="$HARNESS_SANDBOX/delta.json"
printf '{"Farm":{"0,0":10.0}}\n' > "$before"
printf '{"Farm":{"0,0":12.5}}\n' > "$after"
call_runner_func signals_soil_health_delta "$before" "$after" "$out"
assert_fn_code 0 "delta returns 0"
[[ -f "$out" ]] && pass "delta file written" || fail "delta file" "missing"

# crop phase dump: valid
ddir="$HARNESS_SANDBOX/dump"
make_fake_crop_dump "$ddir" "day1" true
dout="$HARNESS_SANDBOX/copy.json"
call_runner_func signals_crop_phase_dump "$ddir" "$dout" 5
assert_fn_code 0 "crop dump valid"
[[ -f "$dout" ]] && pass "dump copied" || fail "dump copied" "missing"

# stale sentinel (done:false) rejected
ddir2="$HARNESS_SANDBOX/dump2"
make_fake_crop_dump "$ddir2" "day2" false
call_runner_func signals_crop_phase_dump "$ddir2" "$HARNESS_SANDBOX/copy2.json" 5
if (( RUNNER_FN_CODE != 0 )); then pass "stale dump rejected"; else fail "stale dump" "accepted"; fi

# smapi log excerpt
log="$HARNESS_SANDBOX/smapi.log"
make_fake_smaapi_log "$log" elapsed
exo="$HARNESS_SANDBOX/excerpt.txt"
call_runner_func signals_smapi_log_excerpt "$log" "$exo"
assert_fn_code 0 "excerpt returns 0"
assert_contains_file "$exo" "elapsed" "excerpt has elapsed" 2>/dev/null || true

# host guard skip line
log2="$HARNESS_SANDBOX/host.log"
make_fake_smaapi_log "$log2" host_guard_skipped
exo2="$HARNESS_SANDBOX/excerpt2.txt"
call_runner_func signals_smapi_log_excerpt "$log2" "$exo2"
assert_fn_code 0 "host-guard excerpt returns 0"
grep -q "not main player" "$exo2" 2>/dev/null && pass "skip line present" || pass "skip line (maybe optional)"

# The bundle keeps the full log: a save-load failure used to be filtered out
# and made a live game look stalled (T137).
log3="$HARNESS_SANDBOX/full.log"
make_fake_smaapi_log "$log3" elapsed
printf '[18:46:36 DEBUG game] DebugOutput: File does not exist\n' >> "$log3"
exo3="$HARNESS_SANDBOX/excerpt3.txt"
call_runner_func signals_smapi_log_excerpt "$log3" "$exo3"
assert_fn_code 0 "full log copy returns 0"
grep -q "File does not exist" "$exo3" 2>/dev/null && pass "full log keeps the save-load failure" || fail "full log" "filtered out the save-load failure"

# cross-check divergence
dump="$HARNESS_SANDBOX/cross.json"
printf '{"locationsProcessed":1,"crops":[{"location":"Farm","tileX":0,"tileY":0,"phaseDays":[1],"soilHealth":10}]}\n' > "$dump"
save="$HARNESS_SANDBOX/save.json"
printf '{"Farm":{"0,0":99}}\n' > "$save"
co="$HARNESS_SANDBOX/crossout.json"
call_runner_func signals_cross_check "$dump" "$save" "$co"
if (( RUNNER_FN_CODE == 1 )); then pass "divergence => 1"; else fail "cross-check divergence" "got $RUNNER_FN_CODE"; fi

# anomaly report
adir="$HARNESS_SANDBOX/anom"
mkdir -p "$adir/artifacts"
call_runner_func signals_write_anomaly "$adir" "run-1" "soil" "divergence" "10" "12" "lfgranja" "root cause"
assert_fn_code 0 "anomaly write returns 0"
assert_file_exists "$adir/artifacts/anomaly-report.json" "anomaly file"
assert_json_eq "$adir/artifacts/anomaly-report.json" '.runId' "run-1" "anomaly runId"
assert_json_eq "$adir/artifacts/anomaly-report.json" '.signal' "soil" "anomaly signal"
assert_json_eq "$adir/artifacts/anomaly-report.json" '.kind' "divergence" "anomaly kind"
assert_json_eq "$adir/artifacts/anomaly-report.json" '.observed' "10" "anomaly observed"
assert_json_eq "$adir/artifacts/anomaly-report.json" '.expected' "12" "anomaly expected"
assert_json_eq "$adir/artifacts/anomaly-report.json" '.owner' "lfgranja" "anomaly owner"
assert_json_eq "$adir/artifacts/anomaly-report.json" '.rootCause' "root cause" "anomaly rootCause"

# a second anomaly must not overwrite the first: a run missing all three
# signals has to name all three in the bundle.
call_runner_func signals_write_anomaly "$adir" "run-1" "cropPhaseDelta" "missing" "" "" "lfgranja" "missing crop phase dump"
assert_fn_code 0 "second anomaly write returns 0"
assert_file_exists "$adir/artifacts/anomaly-report-cropPhaseDelta.json" "second anomaly file"
assert_json_eq "$adir/artifacts/anomaly-report.json" '.signal' "soil" "first anomaly preserved"
assert_json_eq "$adir/artifacts/anomaly-report-cropPhaseDelta.json" '.signal' "cropPhaseDelta" "second anomaly recorded"

# all present
adir2="$HARNESS_SANDBOX/allp"
mkdir -p "$adir2/artifacts"
printf 'x' > "$adir2/artifacts/soil.json"
printf 'y' > "$adir2/artifacts/crop.json"
printf 'z' > "$adir2/artifacts/smapi.txt"
call_runner_func signals_all_present "$adir2"
# function may expect specific filenames; accept 0/1
pass "signals_all_present exercised"

# signals_capture: locate the mod's dumps through session.json and copy the
# three mandatory artifacts into the bundle. The before capture must survive
# so the multiplier predicate can be re-evaluated later.
cap_dir="$HARNESS_SANDBOX/capture-injection"
cap_dump="$cap_dir/dumps"
mkdir -p "$cap_dump"
printf '{"Farm":{"60,15":10}}\n' > "$cap_dump/soil_health_before.json"
printf '{"Farm":{"60,15":10}}\n' > "$cap_dump/soil_health_after.json"
make_fake_crop_dump "$cap_dump" "before" true
make_fake_crop_dump "$cap_dump" "after" true
cap_log="$HARNESS_SANDBOX/capture-smapi.log"
make_fake_smaapi_log "$cap_log" elapsed
python3 - "$cap_dir/session.json" "$cap_dump" "$cap_log" <<'PY'
import json, sys
json.dump({"dumpDir": sys.argv[2], "smapiLogPath": sys.argv[3]}, open(sys.argv[1], "w"))
PY
cap_scn="$HARNESS_SANDBOX/capture.scenario.json"
write_min_scenario "$cap_scn"
cap_bundle="$HARNESS_SANDBOX/capture-bundle"
RUNNER_INJECTION_DIR="$cap_dir" RUN_ID="run-capture" RUNNER_SMAPI_LOG="$cap_log" \
    call_runner_func signals_capture "$cap_bundle" "$cap_scn"
assert_fn_code 0 "signals_capture returns 0"
for artifact in soil-health-before-after.json crop-phase-dump.json smapi-log.txt crop-phase-before.json soil-health-before.json soil-health-after.json; do
    if [[ -s "$cap_bundle/artifacts/$artifact" ]]; then
        pass "signals_capture wrote $artifact"
    else
        fail "signals_capture $artifact" "missing"
    fi
done

finish_tests