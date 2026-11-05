#!/usr/bin/env bash
if [ -n "${_LR_VERDICT_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_VERDICT_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

# verdict_from_exit: map exit code to verdict class (best effort)
# partial is set by signal layer; not derived here
verdict_from_exit() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    case "$1" in
        0) echo "pass" ;;
        18) echo "fail" ;;
        19) echo "errored" ;;
        20) echo "skipped" ;;
        4) echo "errored" ;; # timeout -> errored class in this mapping context; policy notes inherited
        21) echo "policy" ;;
        22) echo "reserved" ;;
        *) echo "errored" ;;
    esac
    return 0
}

# verdict_exit_for: map verdict to exit code
# Includes code 21 POLICY and reserved code 22 is never emitted by normal mapping
verdict_exit_for() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    case "$1" in
        pass) echo 0 ;;
        fail) echo 18 ;;
        errored) echo 19 ;;
        partial) echo 19 ;;
        skipped) echo 20 ;;
        policy) echo 21 ;;
        flaky) echo 0 ;; # derived flaky on pass exits 0
        reserved) echo 22 ;; # reserved, never emitted by runner paths
        *) echo 19 ;;
    esac
    return 0
}

verdict_expected_match() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local actual="$1" expected="$2"
    # missing expected for positive => pass
    if [[ -z "$expected" ]]; then expected="pass"; fi
    if [[ "$actual" == "$expected" ]]; then return 0; else return 1; fi
}

# verdict_evaluate: core evaluation
# signals_complete: 1 if all three mandatory signal artifacts present/valid
# signals_agree: 1 if signals agree/divergent resolved
# thresholds_ok: 1 if all thresholds pass
# expected: expectedVerdict (may be empty)
# farmhand_breach: 1 if host-guard breach
verdict_evaluate() {
    [[ $# -eq 5 ]] || return "$EXIT_USAGE_ERROR"
    local signals_complete="$1" signals_agree="$2" thresholds_ok="$3" expected="$4" farmhand_breach="$5"
    # missing signals => partial
    if [[ "$signals_complete" != "1" ]]; then echo "partial"; return 0; fi
    if [[ "$farmhand_breach" == "1" ]]; then echo "fail"; return 0; fi
    if [[ "$signals_agree" != "1" ]]; then echo "fail"; return 0; fi
    if [[ "$thresholds_ok" != "1" ]]; then echo "fail"; return 0; fi
    echo "pass"
    return 0
}

# verdict_classify_missing_artifacts
# Args: signals_complete(0/1), manifest_ok(0/1), claims_ok(0/1), anchor_ok(0/1), attempts_ok(0/1)
# Returns verdict on stdout; uses exit codes semantics
verdict_classify_missing_artifacts() {
    [[ $# -eq 5 ]] || return "$EXIT_USAGE_ERROR"
    local signals_complete="$1" manifest_ok="$2" claims_ok="$3" anchor_ok="$4" attempts_ok="$5"
    # missing bundle/contract artifacts => errored (higher priority than signal completeness in bundle trust)
    if [[ "$manifest_ok" != "1" || "$claims_ok" != "1" || "$anchor_ok" != "1" || "$attempts_ok" != "1" ]]; then
        echo "errored"
        return 0
    fi
    if [[ "$signals_complete" != "1" ]]; then
        echo "partial"
        return 0
    fi
    echo "ok"
    return 0
}

# verdict_apply_expected: apply expectedVerdict
# Args: actual_verdict, expected_verdict, is_integrity_or_claim(0/1)
# Prints final verdict and returns exit code on stdout? returns via echo final verdict; caller uses verdict_exit_for
# Mismatch => errored (exit 19). Matched fail preserved.
verdict_apply_expected() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local actual="$1" expected="$2" integrity_claim="$3"
    if [[ -z "$expected" ]]; then
        expected="pass"
    fi
    if [[ "$actual" == "$expected" ]]; then
        echo "$actual"
        return 0
    fi
    # mismatch
    if [[ "$integrity_claim" == "1" ]]; then
        # integrity/security or claim overreach - still errored, not demotable
        echo "errored"
        return 0
    fi
    # mismatch => scenario-contract failure => errored
    echo "errored"
    return 0
}

# verdict_derive_flaky: derive flaky metadata
# Args: first_attempt_failed_retryable(0/1), later_pass_same_context(0/1)
# Returns 1 if flaky derived, 0 if not
verdict_derive_flaky() {
    [[ $# -eq 2 ]] || return 0
    local first_failed_retryable="$1" later_pass="$2"
    if [[ "$first_failed_retryable" == "1" && "$later_pass" == "1" ]]; then
        return 1
    fi
    return 0
}

# verdict_aggregate: suite aggregation 19 > 18 > 21 > 20 > 0
verdict_aggregate() {
    # prints aggregate exit code
    local has19=0 has18=0 has21=0 has20=0 has0=0
    local c
    for c in "$@"; do
        case "$c" in
            19) has19=1 ;;
            18) has18=1 ;;
            21) has21=1 ;;
            20) has20=1 ;;
            0) has0=1 ;;
        esac
    done
    if (( has19 )); then echo 19; return 0; fi
    if (( has18 )); then echo 18; return 0; fi
    if (( has21 )); then echo 21; return 0; fi
    if (( has20 == 1 && has0 == 0 )); then echo 20; return 0; fi
    echo 0
    return 0
}

verdict_apply_ignore_exit_code() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local code="$1" list="$2" integrity_flag="$3"
    # validate list entries
    local entry
    IFS=',' read -ra entries <<< "$list"
    for entry in "${entries[@]}"; do
        entry="${entry//[[:space:]]/}"
        case "$entry" in
            4|6|7|9|10|13|14|18|19|20|21) ;;
            0|22) 
                return "$EXIT_USAGE_ERROR"
                ;;
            *) 
                # check if in allowed set
                if [[ ! " 4 6 7 9 10 13 14 18 19 20 21 " =~ " $entry " ]]; then
                    return "$EXIT_USAGE_ERROR"
                fi
                ;;
        esac
    done
    # integrity or policy 21 refusal
    if [[ "$integrity_flag" == "1" ]]; then
        printf '%s\n' "$code"
        return 0
    fi
    if [[ "$code" == "21" ]]; then
        # policy overreach never demotable
        printf '%s\n' "$code"
        return 0
    fi
    # check if code in list
    if [[ ",$list," == *",$code,"* ]]; then
        echo 0
        return 0
    fi
    printf '%s\n' "$code"
    return 0
}

# verdict_evaluate_bundle <bundle> <scenario...>
#
# Derive the five inputs the pure verdict_evaluate expects, evaluate the
# scenario thresholds against the captured artifacts, and write verdict.json.
# The evaluator is the .NET PredicateEvaluator CLI; when it is unavailable the
# result is errored (fail closed), never a silent pass.
verdict_evaluate_bundle() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1"
    shift
    local artifacts="$bundle/artifacts"
    local evaluation="$bundle/evaluation"
    mkdir -p "$evaluation" || return "$EXIT_INTERNAL_ERROR"

    local signals_complete=0
    if [[ -s "$artifacts/soil-health-before-after.json" \
        && -s "$artifacts/crop-phase-dump.json" \
        && -s "$artifacts/smapi-log.txt" ]]; then
        signals_complete=1
    fi

    local signals_agree=1
    shopt -s nullglob
    local anomalies=("$artifacts"/anomaly-report*.json)
    shopt -u nullglob
    if (( ${#anomalies[@]} > 0 )); then
        signals_agree=0
    fi
    if [[ -f "$artifacts/crop-phase-cross-check.json" ]]; then
        if ! python3 - "$artifacts/crop-phase-cross-check.json" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1], encoding="utf-8"))
except Exception:
    sys.exit(1)
sys.exit(0 if d.get("agree") is True else 1)
PY
        then
            signals_agree=0
        fi
    fi

    local farmhand_breach=0
    local role
    role="$(python3 - "$1" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1], encoding="utf-8"))
except Exception:
    sys.exit(0)
print(d.get("playerRole", "host"))
PY
)"
    if [[ "$role" == "farmhand" && -f "$artifacts/crop-phase-dump.json" ]]; then
        if python3 - "$artifacts/crop-phase-dump.json" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1], encoding="utf-8"))
except Exception:
    sys.exit(0)
# A farmhand run must not claim host-side mutation. isMainPlayer=true in the
# dump is the clearest breach signal; crops with a mutated phaseDays list are
# the secondary one.
if d.get("isMainPlayer") is True:
    sys.exit(0)
for c in d.get("crops", []) or []:
    if c.get("phaseDays") != c.get("basePhaseDays"):
        sys.exit(0)
sys.exit(1)
PY
        then
            farmhand_breach=1
        fi
    fi

    # Derive a flat actual/pre record for the scenario target tile. The
    # evaluator speaks scalars (`phaseDays` is phaseDays[currentPhase]), while
    # the dump carries the whole list.
    local target_json="$evaluation/actual.json"
    local pre_json="$evaluation/pre.json"
    local derive_rc=0
    python3 - "$artifacts" "$1" "$target_json" "$pre_json" <<'PY' || derive_rc=$?
import json, math, os, re, sys
artifacts, scenario_path, actual_out, pre_out = sys.argv[1:5]

def load(path, default):
    try:
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return default

scenario = load(scenario_path, {})
after_dump = load(os.path.join(artifacts, "crop-phase-dump.json"), {})
before_dump = load(os.path.join(artifacts, "crop-phase-before.json"), {})
delta = load(os.path.join(artifacts, "soil-health-before-after.json"), {})

tile = None
location = "Farm"
for c in scenario.get("initCommands", []) or []:
    cmd = str(c.get("command") or "")
    m = re.match(r"lr_set_soil_health\s+(-?\d+)\s+(-?\d+)", cmd)
    if m:
        tile = (int(m.group(1)), int(m.group(2)))
for step in scenario.get("steps", []) or []:
    target = step.get("target")
    if isinstance(target, str):
        m = re.match(r"^(-?\d+),(-?\d+)$", target.strip())
        if m:
            tile = (int(m.group(1)), int(m.group(2)))
        elif target.lower() in ("farm", "greenhouse"):
            location = target.capitalize()
if tile is None:
    tile = (0, 0)

def crop_entry(dump):
    for c in dump.get("crops", []) or []:
        if c.get("tileX") == tile[0] and c.get("tileY") == tile[1]:
            if str(c.get("location", "")).lower() == location.lower():
                return c
    return None

def scalar_phase_days(entry):
    if not entry:
        return None
    days = entry.get("phaseDays") or []
    phase = entry.get("currentPhase")
    try:
        phase = int(phase)
    except (TypeError, ValueError):
        return None
    if 0 <= phase < len(days):
        return days[phase]
    if entry.get("isSentinelPhase") is True:
        return 99999
    return None

def tier_for(value, state):
    if state in ("missing", "nan"):
        return "unknown"
    if value is None:
        return "unknown"
    try:
        v = float(value)
    except (TypeError, ValueError):
        return "unknown"
    if math.isnan(v):
        return "unknown"
    if v <= 0:
        return "nonParticipant"
    if v < 34:
        return "poor"
    if v < 67:
        return "moderate"
    return "healthy"

def soil_for(tile_key):
    locs = delta.get("locations", {}) if isinstance(delta, dict) else {}
    loc = locs.get(location) or {}
    rec = loc.get(tile_key) if isinstance(loc, dict) else None
    if not isinstance(rec, dict):
        return None
    value = rec.get("after")
    try:
        value = float(value)
    except (TypeError, ValueError):
        return None
    if math.isnan(value):
        return None
    return value

def build(dump, include_crop):
    entry = crop_entry(dump) if include_crop else None
    tile_key = f"{tile[0]},{tile[1]}"
    health = None
    state = "missing"
    if entry:
        state = entry.get("soilHealthState") or "missing"
        if state == "measured" and entry.get("soilHealth") is not None:
            try:
                health = float(entry.get("soilHealth"))
            except (TypeError, ValueError):
                health = None
        elif state == "nan":
            health = None
    if health is None and state not in ("nan", "missing"):
        health = soil_for(tile_key)
    if health is None and state == "missing" and entry is None:
        health = soil_for(tile_key)
        if health is not None:
            state = "measured"
    rec = {
        "soilHealth": health,
        "soilHealthState": state,
        "healthTier": tier_for(health, state),
    }
    if entry:
        rec.update({
            "phaseDays": scalar_phase_days(entry),
            "currentPhase": entry.get("currentPhase"),
            "dayOfCurrentPhase": entry.get("dayOfCurrentPhase"),
            "fullyGrown": entry.get("fullyGrown"),
            "dead": entry.get("dead"),
            "indexOfHarvest": entry.get("indexOfHarvest"),
        })
    return rec

actual = build(after_dump, True)

def harvest_record(probe):
    for h in after_dump.get("harvests", []) or []:
        if str(h.get("probe", "")) == probe:
            return h
    return None

def harvest_value(record, key):
    if record is None:
        return None
    return record.get(key)

poor = harvest_record("poor")
moderate = harvest_record("moderate")
healthy_quality = harvest_record("healthy-quality")
healthy_extra = harvest_record("healthy-extra")

actual.update({
    "isMainPlayer": after_dump.get("isMainPlayer", False),
    "locationsProcessed": after_dump.get("locationsProcessed", 0),
    "elapsedMilliseconds": after_dump.get("elapsedMilliseconds", 0),
    "eventName": "DayEnding",
    "poorHarvestQuality": harvest_value(poor, "mainQuality"),
    "moderateHarvestQuality": harvest_value(moderate, "mainQuality"),
    "healthyHarvestQuality": harvest_value(healthy_quality, "mainQuality"),
    "poorExtraCount": harvest_value(poor, "extraCount"),
    "moderateExtraCount": harvest_value(moderate, "extraCount"),
    "healthyExtraCount": harvest_value(healthy_extra, "extraCount"),
    "healthyExtraQuality": harvest_value(healthy_extra, "extraQuality"),
    "poorVanillaQuality": harvest_value(poor, "vanillaQuality"),
    "moderateVanillaQuality": harvest_value(moderate, "vanillaQuality"),
    "healthyVanillaQuality": harvest_value(healthy_quality, "vanillaQuality"),
    "healthyQualityRoll": harvest_value(healthy_quality, "qualityRoll"),
    "healthyExtraRoll": harvest_value(healthy_extra, "extraRoll"),
})
pre = build(before_dump, True)

with open(actual_out, "w", encoding="utf-8") as f:
    json.dump(actual, f, indent=2)
with open(pre_out, "w", encoding="utf-8") as f:
    json.dump(pre, f, indent=2)
PY
    if (( derive_rc != 0 )); then
        signals_complete=0
    fi

    local evaluator_dll=""
    local candidate
    for candidate in \
        "${RUNNER_EVALUATOR_DLL:-}" \
        "LivingRoots.Verification.Runner/bin/Release/net6.0/LivingRoots.Verification.Runner.dll" \
        "LivingRoots.Verification.Runner/bin/Debug/net6.0/LivingRoots.Verification.Runner.dll"; do
        if [[ -n "$candidate" && -f "$candidate" ]]; then
            evaluator_dll="$candidate"
            break
        fi
    done

    local thresholds_ok=1 evaluator_missing=0
    local details="$evaluation/thresholds.jsonl"
    : > "$details" || true
    local scenario
    local threshold_index=0
    for scenario in "$@"; do
        [[ -f "$scenario" ]] || { thresholds_ok=0; continue; }
        local pred_dir="$evaluation/predicates"
        mkdir -p "$pred_dir"
        local pred_files
        mapfile -t pred_files < <(python3 - "$scenario" "$pred_dir" <<'PY'
import json, os, sys
scenario, outdir = sys.argv[1:3]
try:
    d = json.load(open(scenario, encoding="utf-8"))
except Exception:
    sys.exit(0)
for i, t in enumerate(d.get("thresholds", []) or []):
    p = t.get("predicate")
    if not isinstance(p, dict):
        continue
    path = os.path.join(outdir, f"predicate-{i}.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump(p, f)
    print(path)
PY
)
        local pred
        for pred in "${pred_files[@]}"; do
            [[ -n "$pred" ]] || continue
            threshold_index=$((threshold_index + 1))
            local passed=0 reason="evaluator-unavailable"
            if [[ -n "$evaluator_dll" ]] && command -v dotnet >/dev/null 2>&1; then
                local eval_out
                eval_out="$(dotnet "$evaluator_dll" evaluate "$pred" "$target_json" "$pre_json" 2>/dev/null)" || true
                reason="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1]).get("reason",""))' "$eval_out" 2>/dev/null || echo "evaluation-failed")"
                if printf '%s' "$eval_out" | grep -q '"passed":true'; then
                    passed=1
                fi
            else
                evaluator_missing=1
            fi
            if (( passed == 0 )); then
                thresholds_ok=0
            fi
            python3 - "$details" "$scenario" "$pred" "$passed" "$reason" <<'PY'
import json, sys
path, scenario, predicate, passed, reason = sys.argv[1:6]
rec = {"scenario": scenario, "predicate": predicate, "passed": passed == "1", "reason": reason}
with open(path, "a", encoding="utf-8") as f:
    f.write(json.dumps(rec) + "\n")
PY
        done
    done

    local expected=""
    expected="$(python3 - "$1" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1], encoding="utf-8"))
except Exception:
    sys.exit(0)
print(d.get("expectedVerdict", "pass") or "pass")
PY
)"

    local actual_verdict final_verdict
    actual_verdict="$(verdict_evaluate "$signals_complete" "$signals_agree" "$thresholds_ok" "$expected" "$farmhand_breach")"
    # FR-009/C-REL-12: a divergent signal set is `partial`, not `fail`. The
    # pure function classifies divergence as a gameplay failure; the wrapper
    # keeps the signal-layer diagnosis. Exit code is 19 either way.
    if [[ "$signals_complete" == "1" && "$signals_agree" != "1" && "$farmhand_breach" != "1" ]]; then
        actual_verdict="partial"
    fi
    if (( evaluator_missing == 1 )); then
        # The thresholds were never judged: fail closed, never pass.
        actual_verdict="errored"
    fi
    if [[ "$actual_verdict" == "partial" ]]; then
        # An incomplete/divergent signal set is a capture failure, not an
        # expected-verdict contract failure. Preserve the diagnostic verdict
        # (exit 19 either way); only fully-evaluated pass/fail verdicts are
        # compared against expectedVerdict.
        final_verdict="partial"
    else
        final_verdict="$(verdict_apply_expected "$actual_verdict" "$expected" "0")"
    fi

    python3 - "$bundle/verdict.json" "$final_verdict" "$signals_complete" "$signals_agree" \
        "$thresholds_ok" "$expected" "$farmhand_breach" "$evaluator_missing" "$actual_verdict" <<'PY'
import json, sys
path, final_verdict, complete, agree, thresholds, expected, breach, missing, actual = sys.argv[1:10]
d = {
    "schemaVersion": 1,
    "verdict": final_verdict,
    "actualVerdict": actual,
    "signalsComplete": complete == "1",
    "signalsAgree": agree == "1",
    "thresholdsOk": thresholds == "1",
    "expectedVerdict": expected,
    "farmhandBreach": breach == "1",
    "evaluatorMissing": missing == "1",
}
with open(path, "w", encoding="utf-8") as f:
    json.dump(d, f, indent=2)
PY

    local exit_code
    exit_code="$(verdict_exit_for "$final_verdict")"
    printf '%s\n' "$final_verdict"
    return "$exit_code"
}

verdict_write_attempt() {
    [[ $# -eq 6 ]] || return "$EXIT_USAGE_ERROR"
    local attempt_dir="$1" attempt="$2" verdict="$3" failureClass="$4" signals="$5" exitCode="$6"
    mkdir -p "$attempt_dir"
    python3 - "$attempt_dir/attempt.json" "$attempt" "$verdict" "$failureClass" "$signals" "$exitCode" <<'PY'
import json,sys
path,attempt,verdict,fc,signals,ec=sys.argv[1:]
d={"attempt":int(attempt),"verdict":verdict,"failureClass":fc,"signals":signals,"exitCode":int(ec)}
json.dump(d,open(path,"w"),indent=2)
PY
    return 0
}