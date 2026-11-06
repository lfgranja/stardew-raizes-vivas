# scripts/lib/scenario-validate.sh — scenario validation wrapper (spec 010, T060).

if [ -n "${_LR_SCENARIO_VALIDATE_LOADED:-}" ]; then
    return 0 2>/dev/null || exit 0
fi
_LR_SCENARIO_VALIDATE_LOADED=1

# shellcheck source=common.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

scenario_validate() {
    if [[ $# -lt 1 ]]; then
        log_error "scenario_validate: usage: scenario_validate <scenario.json>"
        return "$EXIT_USAGE_ERROR"
    fi
    local scenario="$1"

    if [[ -z "$scenario" || ! -f "$scenario" ]]; then
        log_error "scenario_validate: missing file: $scenario"
        return "$EXIT_USAGE_ERROR"
    fi

    # Invoke .NET helper if available (best-effort; does not override a valid python result).
    local helper_dll=""
    for cand in \
        "LivingRoots.Verification.Runner/bin/Release/net6.0/LivingRoots.Verification.Runner.dll" \
        "LivingRoots.Verification.Runner/bin/Debug/net6.0/LivingRoots.Verification.Runner.dll"; do
        if [[ -f "$cand" ]]; then helper_dll="$cand"; break; fi
    done
    if [[ -n "${SCENARIO_VALIDATOR_HELPER:-}" && -f "${SCENARIO_VALIDATOR_HELPER}" ]]; then
        helper_dll="$SCENARIO_VALIDATOR_HELPER"
    fi
    if [[ -n "$helper_dll" && "${USE_DOTNET_HELPER:-0}" == "1" ]]; then
        dotnet "$helper_dll" validate-scenario "$scenario" >/dev/null 2>&1
        # fall through to python validation regardless
    fi

    python3 - "$scenario" <<'PY'
import sys, json, os
scenario = sys.argv[1]
try:
    import jsonschema
    from jsonschema import Draft202012Validator
except Exception:
    jsonschema = None

def fail(path):
    sys.stderr.write(path + "\n")
    sys.exit(14)

try:
    with open(scenario, encoding="utf-8") as f:
        text = f.read()
except Exception:
    fail("/")

try:
    data = json.loads(text)
except Exception:
    fail("/")

if not isinstance(data, dict):
    fail("/")

# Schema validation (Draft 2020-12) if available.
schema_version = data.get("schemaVersion", 2)
schema_file = "scenario-v3.schema.json" if schema_version == 3 else "scenario.schema.json"
schema_candidates = []
d = os.path.dirname(os.path.abspath(scenario))
while True:
    schema_candidates.append(os.path.join(d, "specs", "010-headless-verification-runner", "contracts", schema_file))
    schema_candidates.append(os.path.join(d, "contracts", schema_file))
    parent = os.path.dirname(d)
    if parent == d:
        break
    d = parent
schema_candidates.append(os.path.join(os.getcwd(), "specs", "010-headless-verification-runner", "contracts", schema_file))
schema_path = None
for c in schema_candidates:
    if os.path.isfile(c):
        schema_path = c
        break

if jsonschema is not None and schema_path is not None:
    try:
        schema = json.loads(open(schema_path, encoding="utf-8").read())
        validator = Draft202012Validator(schema)
        errors = sorted(validator.iter_errors(data), key=lambda e: list(e.path))
        if errors:
            e = errors[0]
            path = "/" + "/".join(str(p) for p in e.path) if e.path else "/"
            fail(path if path != "/" else (e.json_path or "/"))
    except Exception:
        pass

# Cross-field rules (mirror ScenarioValidator).
# expectedVerdict rules
kind = data.get("scenarioKind")
ev = data.get("expectedVerdict")
if kind == "positive":
    if ev is not None and ev != "pass":
        fail("/expectedVerdict")
elif kind == "negative":
    if not ev or ev == "pass":
        fail("/expectedVerdict")
elif kind == "boundary":
    if not ev or ev not in ("pass", "fail"):
        fail("/expectedVerdict")

# steps action-to-required-input
steps = data.get("steps", [])
if isinstance(steps, list):
    for i, step in enumerate(steps):
        if not isinstance(step, dict):
            continue
        action = step.get("action")
        inputs = step.get("inputs")
        if action == "insertWaste":
            if not isinstance(inputs, dict) or "itemId" not in inputs or "count" not in inputs:
                fail(f"/steps/{i}/inputs")
        elif action == "setSoilHealth":
            if not isinstance(inputs, dict) or "soilHealth" not in inputs:
                fail(f"/steps/{i}/inputs")
        elif action == "runMacro":
            if not isinstance(inputs, dict) or "macroFile" not in inputs:
                fail(f"/steps/{i}/inputs")

# thresholds
AllowedFields = {
    "soilHealthStateDelta": {"soilHealth", "healthTier", "soilHealthState"},
    "cropPhaseDelta": {"phaseDays", "currentPhase", "dayOfCurrentPhase", "fullyGrown", "dead", "indexOfHarvest", "healthTier", "soilHealthState", "poorHarvestQuality", "moderateHarvestQuality", "healthyHarvestQuality", "poorExtraCount", "moderateExtraCount", "healthyExtraCount", "healthyExtraQuality", "poorVanillaQuality", "moderateVanillaQuality", "healthyVanillaQuality", "healthyQualityRoll", "healthyExtraRoll"},
    "smapiLog": {"elapsedMilliseconds", "eventName", "locationsProcessed", "isMainPlayer"},
}
def sentinel_ok(sentinel, field):
    if sentinel == "SENTINEL_UNTOUCHED_99999":
        return field == "phaseDays"
    if sentinel in ("NAN_HEALTH", "MISSING_KEY", "HEALTH_LE_ZERO", "POOR_POSITIVE_LT_31", "POOR_POSITIVE_LT_34"):
        return field == "healthTier"
    if sentinel == "PHASE_MINUS_ONE":
        return field == "currentPhase"
    return False

thresholds = data.get("thresholds", [])
if isinstance(thresholds, list):
    for t, thr in enumerate(thresholds):
        if not isinstance(thr, dict):
            continue
        signal = thr.get("signal")
        pred = thr.get("predicate")
        if not isinstance(pred, dict):
            fail(f"/thresholds/{t}/predicate")
        op = pred.get("op")
        field = None
        if op == "cmp":
            field = pred.get("left")
        elif "field" in pred:
            field = pred.get("field")
        if signal is not None and field is not None:
            allowed = AllowedFields.get(signal, set())
            if allowed and field not in allowed:
                fail(f"/thresholds/{t}/predicate")
        if op == "multiplier":
            base_ref = pred.get("base")
            if isinstance(base_ref, str):
                suffix = base_ref[4:] if base_ref.startswith("pre.") else base_ref
                if suffix != field:
                    fail(f"/thresholds/{t}/predicate/base")
            factor = pred.get("factor")
            if isinstance(factor, (int, float)):
                if factor < 0.01 or factor > 100:
                    fail(f"/thresholds/{t}/predicate/factor")
        if op == "between":
            mn = pred.get("min")
            mx = pred.get("max")
            if isinstance(mn, (int, float)) and isinstance(mx, (int, float)):
                if mn > mx:
                    fail(f"/thresholds/{t}/predicate/min")
        if op == "sentinel":
            sentinel = pred.get("sentinel")
            if not sentinel_ok(sentinel, field):
                fail(f"/thresholds/{t}/predicate/sentinel")
        if signal == "smapiLog" and "subject" in thr:
            fail(f"/thresholds/{t}/subject")

# playerRole / hostGuard
role = data.get("playerRole")
if role == "farmhand" and "hostGuard" not in data:
    fail("/hostGuard")
if role == "host" and "hostGuard" in data:
    fail("/hostGuard")

# testMdPath existence + exactly-one GATE3 marker block (fail-fast, FR-008).
tm = data.get("testMdPath")
if not isinstance(tm, str) or not tm.strip():
    fail("/testMdPath")
if not tm.lower().endswith("test.md"):
    fail("/testMdPath")
base_dir = os.path.dirname(os.path.abspath(scenario))
candidate = None
if os.path.isabs(tm):
    if os.path.isfile(tm):
        candidate = tm
else:
    c1 = os.path.join(base_dir, tm)
    if os.path.isfile(c1):
        candidate = c1
    else:
        c2 = os.path.join(os.getcwd(), tm)
        if os.path.isfile(c2):
            candidate = c2
if candidate is None:
    fail("/testMdPath")
try:
    with open(candidate, encoding="utf-8") as f:
        md_content = f.read()
except Exception:
    fail("/testMdPath")
begin_tok = "<!-- GATE3:BEGIN -->"
end_tok = "<!-- GATE3:END -->"
if md_content.count(begin_tok) != 1 or md_content.count(end_tok) != 1:
    fail("/testMdPath")
bi = md_content.find(begin_tok)
ei = md_content.find(end_tok)
if bi < 0 or ei < 0 or bi > ei:
    fail("/testMdPath")

print("OK")
sys.exit(0)
PY
    return $?
}

scenario_validate_file() { scenario_validate "$@"; }

export -f scenario_validate scenario_validate_file
