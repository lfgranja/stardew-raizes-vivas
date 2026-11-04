#!/usr/bin/env bash
if [ -n "${_LR_SIGNALS_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_SIGNALS_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

signals_soil_health_delta() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local before="$1" after="$2" out="$3"
    python3 - "$before" "$after" "$out" <<'PY'
import json,sys
def load(p):
    try:
        with open(p) as f:
            return json.load(f)
    except Exception:
        return {}
b=load(sys.argv[1]); a=load(sys.argv[2])
def get(d,loc,tile):
    try:
        v=d.get(loc,{}).get(tile,0)
    except Exception:
        v=0
    if v is None: v=0
    try:
        import math
        if isinstance(v,float) and math.isnan(v): return 0
    except Exception: pass
    return v
locs=set(b)|set(a)
out={"locations":{}}
for loc in locs:
    tiles=set()
    if isinstance(b.get(loc),dict): tiles|=set(b[loc])
    if isinstance(a.get(loc),dict): tiles|=set(a[loc])
    out["locations"][loc]={}
    for t in tiles:
        bv=get(b,loc,t); av=get(a,loc,t)
        try:
            delta=av-bv
        except Exception:
            delta=0
        # tolerance 0.0001: if abs(delta)<0.0001 treat as 0
        if abs(delta)<0.0001:
            delta=0
        out["locations"][loc][t]={"before":bv,"after":av,"delta":delta}
with open(sys.argv[3],"w") as f:
    json.dump(out,f,indent=2)
sys.exit(0)
PY
}

signals_crop_phase_dump() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local dump_dir="$1" out="$2" timeout="${3:-5}"
    # Wait for a completed dump. Prefer the explicit `after` generation: the
    # queue writes a `before` dump first, and `find | head -1` used to pick
    # whichever inode happened to sort first, so the captured signal could be
    # the pre-run crop state.
    local i=0 found=""
    while (( i < timeout*2+1 )); do
        found="$(find "$dump_dir" -maxdepth 1 -name 'crop_phase_dump_after.json.done' 2>/dev/null | head -1)"
        if [[ -z "$found" ]]; then
            found="$(find "$dump_dir" -maxdepth 1 -name '*.json.done' -printf '%T@ %p\n' 2>/dev/null | sort -nr | head -1 | cut -d' ' -f2-)"
        fi
        [[ -n "$found" ]] && break
        sleep 0.2
        i=$((i+1))
    done
    [[ -n "$found" ]] || return 19
    local json_file="${found%.done}"
    [[ -f "$json_file" ]] || return 19
    python3 - "$json_file" "$out" <<'PY'
import json,sys,shutil
try:
    d=json.load(open(sys.argv[1]))
except Exception:
    sys.exit(19)
if d.get("done") is not True:
    sys.exit(19)
# sidecar/field agreement: locationsProcessed == len(locations?) best-effort
shutil.copyfile(sys.argv[1],sys.argv[2])
sys.exit(0)
PY
}

signals_smapi_log_excerpt() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local log="$1" out="$2"
    [[ -f "$log" ]] || return 19
    # This used to keep only elapsed/DayEnding/host-guard lines. That filter
    # turned a save-load failure into what looked like a five-second stall and
    # cost a full investigation (T137), so the bundle now keeps the whole log.
    # The function name is retained for contract compatibility.
    cp -f "$log" "$out" 2>/dev/null || return 19
    return 0
}

signals_cross_check() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local dump="$1" save="$2" out="$3"
    python3 - "$dump" "$save" "$out" <<'PY'
import json,sys
try:
    with open(sys.argv[1]) as f:
        d=json.load(f)
    with open(sys.argv[2]) as f:
        s=json.load(f)
except Exception as e:
    rec={"agree":False,"divergences":[{"error":str(e)}]}
    with open(sys.argv[3],"w") as f:
        json.dump(rec,f)
    sys.exit(1)
# The persisted mod data is wrapped in {"LocationHealthData": {loc: {tile: v}}}.
# Unwrap it before comparing, otherwise every tile looks absent and the
# cross-check reports agreement without actually checking anything.
if isinstance(s, dict) and isinstance(s.get("LocationHealthData"), dict):
    s = s["LocationHealthData"]
rec={"agree":False,"divergences":[]}
try:
    lp=d.get("locationsProcessed")
except Exception:
    lp=None
crops=d.get("crops",[]) or []
div=False
for c in crops:
    try:
        loc=c.get("location"); tx=c.get("tileX"); ty=c.get("tileY")
        tile=f"{tx},{ty}"
        sh=c.get("soilHealth")
        sav=None
        if isinstance(s,dict):
            if isinstance(s.get(loc),dict):
                sav=s.get(loc).get(tile)
        if sav is not None and sh is not None:
            try:
                if abs(float(sav)-float(sh))>0.0001:
                    div=True
                    rec["divergences"].append({"location":loc,"tile":tile,"dump":sh,"save":sav})
            except Exception:
                div=True
                rec["divergences"].append({"location":loc,"tile":tile,"dump":sh,"save":sav,"error":"coerce"})
    except Exception:
        continue
if div:
    rec["agree"]=False
    with open(sys.argv[3],"w") as f:
        json.dump(rec,f)
    sys.exit(1)
rec["agree"]=True
with open(sys.argv[3],"w") as f:
    json.dump(rec,f)
sys.exit(0)
PY
}

signals_write_anomaly() {
    [[ $# -eq 8 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" runId="$2" signal="$3" kind="$4" observed="$5" expected="$6" owner="$7" rootCause="$8"
    mkdir -p "$bundle/artifacts"
    # One file per anomaly: a fixed name would let the last missing signal
    # overwrite the earlier ones, so a run missing all three reported only one.
    local out="$bundle/artifacts/anomaly-report.json"
    if [[ -e "$out" ]]; then
        local slug
        slug="$(printf '%s' "$signal" | tr -c 'A-Za-z0-9._-' '-')"
        out="$bundle/artifacts/anomaly-report-$slug.json"
    fi
    python3 - "$out" "$runId" "$signal" "$kind" "$observed" "$expected" "$owner" "$rootCause" <<'PY'
import json,sys,datetime
path,runId,signal,kind,observed,expected,owner,rootCause=sys.argv[1:]
d={"runId":runId,"signal":signal,"kind":kind,"observed":observed,"expected":expected,
   "owner":owner,"date":datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
   "rootCause":rootCause}
with open(path,"w") as f:
    json.dump(d,f,indent=2)
PY
    return 0
}

signals_all_present() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1"
    local a="$bundle/artifacts"
    [[ -d "$a" ]] || return 1
    # Exact artifact names, not "any three non-empty files". The anomaly
    # reports live in the same directory and their filenames contain the
    # signal names, so a count-based check reported a run missing all three
    # signals as complete once the anomalies had been written.
    [[ -s "$a/soil-health-before-after.json" ]] || return 1
    [[ -s "$a/crop-phase-dump.json" ]] || return 1
    [[ -s "$a/smapi-log.txt" ]] || return 1
    return 0
}

# signals_require_expected_signals <bundle_dir> <runId> <owner>
# Verify presence of all three expected signals: soilHealthStateDelta, cropPhaseDelta, smapiLog
# Write anomaly report on any missing/mismatch/divergence and return non-zero (divergence/missing => partial)
signals_require_expected_signals() {
    [[ $# -ge 3 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" runId="$2" owner="${3:-unknown}"
    local a="$bundle/artifacts"
    mkdir -p "$a"

    # Exact artifact names. The earlier fuzzy match looked for *soil*/*crop*/
    # *smapi* anywhere in the filename, which made `anomaly-report-<signal>.json`
    # count as the signal itself: after the first pass wrote three anomalies,
    # the second pass saw them and returned success. Missing signals must never
    # satisfy their own requirement.
    local missing=0
    if [[ ! -s "$a/soil-health-before-after.json" ]]; then
        signals_write_anomaly "$bundle" "$runId" "soilHealthStateDelta" "missing" "" "" "$owner" "missing soil health delta" >/dev/null 2>&1 || true
        missing=1
    fi
    if [[ ! -s "$a/crop-phase-dump.json" ]]; then
        signals_write_anomaly "$bundle" "$runId" "cropPhaseDelta" "missing" "" "" "$owner" "missing crop phase dump" >/dev/null 2>&1 || true
        missing=1
    fi
    if [[ ! -s "$a/smapi-log.txt" ]]; then
        signals_write_anomaly "$bundle" "$runId" "smapiLog" "missing" "" "" "$owner" "missing smapi log excerpt" >/dev/null 2>&1 || true
        missing=1
    fi

    if (( missing == 1 )); then
        return "$EXIT_ERRORED"
    fi
    return 0
}

# signals_process <bundle_dir> <runId> <owner> [before.json] [after.json] [dump_dir] [save.json] [smapi.log]
# Orchestrate all three signals with cross-checks; write anomalies on any divergence/missing/mismatch; divergence => partial
signals_process() {
    local bundle="${1:-}" runId="${2:-}" owner="${3:-unknown}"
    shift 3 || true
    local before="${1:-}" after="${2:-}" dump_dir="${3:-}" save="${4:-}" smapi_log="${5:-}"

    [[ -n "$bundle" && -n "$runId" ]] || return "$EXIT_USAGE_ERROR"
    local a="$bundle/artifacts"
    mkdir -p "$a"

    local issues=0

    # Soil health delta
    if [[ -n "$before" && -n "$after" ]]; then
        local sdelta="$a/soil-health-before-after.json"
        if signals_soil_health_delta "$before" "$after" "$sdelta"; then
            [[ -s "$sdelta" ]] || { signals_write_anomaly "$bundle" "$runId" "soilHealthStateDelta" "missing" "" "" "$owner" "soil delta missing after generation" >/dev/null 2>&1 || true; issues=1; }
        else
            signals_write_anomaly "$bundle" "$runId" "soilHealthStateDelta" "mismatch" "" "" "$owner" "soil delta generation failed" >/dev/null 2>&1 || true
            issues=1
        fi
    else
        signals_write_anomaly "$bundle" "$runId" "soilHealthStateDelta" "missing" "" "" "$owner" "missing before/after for soil health delta" >/dev/null 2>&1 || true
        issues=1
    fi

    # Crop phase dump + cross-check with save
    if [[ -n "$dump_dir" ]]; then
        local cdump="$a/crop-phase-dump.json"
        if signals_crop_phase_dump "$dump_dir" "$cdump" 5; then
            [[ -s "$cdump" ]] || { signals_write_anomaly "$bundle" "$runId" "cropPhaseDelta" "missing" "" "" "$owner" "crop dump missing after copy" >/dev/null 2>&1 || true; issues=1; }
            if [[ -n "$save" && -f "$save" ]]; then
                local cchk="$a/crop-phase-cross-check.json"
                if ! signals_cross_check "$cdump" "$save" "$cchk"; then
                    signals_write_anomaly "$bundle" "$runId" "cropPhaseDelta" "divergence" "$(cat "$cchk" 2>/dev/null | tr -d '\n' | head -c 200)" "" "$owner" "dump vs save divergence" >/dev/null 2>&1 || true
                    issues=1
                fi
            fi
        else
            signals_write_anomaly "$bundle" "$runId" "cropPhaseDelta" "missing" "" "" "$owner" "crop phase dump not found/completed" >/dev/null 2>&1 || true
            issues=1
        fi
    else
        signals_write_anomaly "$bundle" "$runId" "cropPhaseDelta" "missing" "" "" "$owner" "missing crop phase dump dir" >/dev/null 2>&1 || true
        issues=1
    fi

    # SMAPI log excerpt
    if [[ -n "$smapi_log" && -f "$smapi_log" ]]; then
        local sexcerpt="$a/smapi-log.txt"
        if signals_smapi_log_excerpt "$smapi_log" "$sexcerpt"; then
            [[ -s "$sexcerpt" ]] || { signals_write_anomaly "$bundle" "$runId" "smapiLog" "missing" "" "" "$owner" "smapi log excerpt empty" >/dev/null 2>&1 || true; issues=1; }
        else
            signals_write_anomaly "$bundle" "$runId" "smapiLog" "missing" "" "" "$owner" "smapi log excerpt failed" >/dev/null 2>&1 || true
            issues=1
        fi
    else
        signals_write_anomaly "$bundle" "$runId" "smapiLog" "missing" "" "" "$owner" "missing smapi log" >/dev/null 2>&1 || true
        issues=1
    fi

    signals_require_expected_signals "$bundle" "$runId" "$owner" >/dev/null 2>&1 || issues=1

    if (( issues == 1 )); then
        return "$EXIT_ERRORED"
    fi
    return 0
}

# signals_capture <bundle_dir> <scenario...>
#
# Locate the artifacts the verification mod wrote into the per-run injection
# directory and hand them to signals_process. The mod owns the capture; the
# runner owns the location and the judgement. When the session is missing the
# call still goes through signals_process with empty inputs so all three
# mandatory anomalies are written (one per signal, not one shared file).
signals_capture() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1"
    shift
    local owner="${RUNNER_QUARANTINE_OWNER:-runner}"
    local dir="${RUNNER_INJECTION_DIR:-${RUNNER_RUN_ROOT:-/tmp}/injection}"
    local session="$dir/session.json"

    local dump_dir="" smapi_log="" save_file=""
    if [[ -f "$session" ]]; then
        local -a fields=()
        mapfile -t fields < <(python3 - "$session" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1], encoding="utf-8"))
except Exception:
    sys.exit(1)
print(d.get("dumpDir", ""))
print(d.get("smapiLogPath", ""))
PY
)
        if (( ${#fields[@]} >= 2 )); then
            dump_dir="${fields[0]}"
            smapi_log="${fields[1]}"
        fi
    fi
    [[ -n "$smapi_log" ]] || smapi_log="${RUNNER_SMAPI_LOG:-}"

    # The mod persists soil health through SMAPI's data helper under the mod
    # folder. Pick the newest file: the save id is sanitized inside the mod and
    # the runner must not re-implement that sanitizer here.
    if [[ -n "${RUNNER_MODS_DIR:-}" && -d "$RUNNER_MODS_DIR/LivingRoots/data" ]]; then
        save_file="$(find "$RUNNER_MODS_DIR/LivingRoots/data" -maxdepth 1 -name 'soil_health_data_*.json' -printf '%T@ %p\n' 2>/dev/null | sort -nr | head -1 | cut -d' ' -f2-)"
    fi

    local before="" after=""
    if [[ -n "$dump_dir" ]]; then
        [[ -f "$dump_dir/soil_health_before.json" ]] && before="$dump_dir/soil_health_before.json"
        [[ -f "$dump_dir/soil_health_after.json" ]] && after="$dump_dir/soil_health_after.json"
        # Preserve the pre-run captures in the bundle too: the multiplier
        # threshold is `after == pre * factor`, so a bundle that only carries
        # the after dump cannot be re-evaluated later.
        mkdir -p "$bundle/artifacts" 2>/dev/null || true
        [[ -f "$dump_dir/crop_phase_dump_before.json" ]] && cp -f "$dump_dir/crop_phase_dump_before.json" "$bundle/artifacts/crop-phase-before.json" 2>/dev/null || true
        [[ -n "$before" ]] && cp -f "$before" "$bundle/artifacts/soil-health-before.json" 2>/dev/null || true
        [[ -n "$after" ]] && cp -f "$after" "$bundle/artifacts/soil-health-after.json" 2>/dev/null || true
    fi

    local rc=0
    signals_process "$bundle" "${RUN_ID:-${RUNNER_RUN_ID:-unknown}}" "$owner" \
        "$before" "$after" "$dump_dir" "$save_file" "$smapi_log" || rc=$?
    return "$rc"
}
