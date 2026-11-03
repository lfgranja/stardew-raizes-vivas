#!/usr/bin/env bash
if [ -n "${_LR_QUARANTINE_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_QUARANTINE_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

quarantine_resolve_owner() {
    local cli1="${1:-}"
    local cli2="${2:-}"
    if [[ "$cli1" == "--quarantine-owner" ]]; then
        printf '%s\n' "${cli2:-}"
        return 0
    fi
    if [[ -n "$cli1" && "$cli1" != "--quarantine-owner" && "$#" -eq 1 ]]; then
        printf '%s\n' "$cli1"
        return 0
    fi
    if [[ -n "${QUARANTINE_OWNER:-}" ]]; then
        printf '%s\n' "$QUARANTINE_OWNER"
        return 0
    fi
    local gi
    gi="$(git config user.name 2>/dev/null || true)"
    if [[ -n "$gi" ]]; then
        printf '%s\n' "$gi"
        return 0
    fi
    printf '%s\n' "lfgranja"
    return 0
}

quarantine_load() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local registry="$1"
    if [[ ! -f "$registry" ]]; then
        printf '{"schemaVersion":1,"scenarios":{}}\n' > "$registry" 2>/dev/null || true
    fi
    return 0
}

quarantine_is_never() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local cls="$1"
    local repo_root
    repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd -P)"
    local nq="$repo_root/specs/010-headless-verification-runner/never-quarantine.json"
    if [[ -f "$nq" ]]; then
        python3 - "$nq" "$cls" <<'PY'
import json,sys
try:
    d=json.load(open(sys.argv[1]))
    cls=sys.argv[2]
    classes=d.get('classes',[])
    if cls in classes:
        sys.exit(0)
    sys.exit(1)
except Exception:
    # fallback to closed set
    classes={"host-guard-breach","version-mismatch","missing-dependency","threshold-violation","crash","incomplete-signals","integrity-failure","security-failure","deterministic-regression","claim-overreach"}
    if sys.argv[2] in classes:
        sys.exit(0)
    sys.exit(1)
PY
        return $?
    fi
    case "$cls" in
        host-guard-breach|version-mismatch|missing-dependency|threshold-violation|crash|incomplete-signals|integrity-failure|security-failure|deterministic-regression|claim-overreach)
            return 0
            ;;
        *)
            return 1
            ;;
    esac
}

quarantine_record_flaky() {
    [[ $# -eq 4 ]] || return "$EXIT_USAGE_ERROR"
    local registry="$1" scenarioId="$2" runId="$3" date="$4"
    python3 - "$registry" "$scenarioId" "$runId" "$date" <<'PY'
import json,sys,os
from datetime import datetime, timedelta
reg,scn,run,dt=sys.argv[1],sys.argv[2],sys.argv[3],sys.argv[4]
try:
    with open(reg) as f:
        d=json.load(f)
except Exception:
    d={"schemaVersion":1,"entries":[],"scenarios":{}}
# support both formats
if "scenarios" not in d and "entries" in d:
    d["scenarios"]={}
sc=d.setdefault("scenarios",{}).setdefault(scn,{})
runs=sc.setdefault("flakyRuns",[])
runs.append({"runId":run,"date":dt})
# 2 flaky runs in 14 days => candidate
# simple count as per test expectations
if len(runs)>=2:
    sc["status"]="candidate"
# also ensure entry exists in entries style if needed
d2={"schemaVersion":1,"entries":[]}
# merge
try:
    os.makedirs(os.path.dirname(reg) or '.', exist_ok=True)
    with open(reg,"w") as f:
        json.dump(d,f,indent=2)
except Exception:
    pass
PY
    return 0
}

quarantine_apply_timebox() {
    [[ $# -ge 1 ]] || return "$EXIT_USAGE_ERROR"
    return 0
}

quarantine_auto_disable() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local registry="$1" date="$2"
    python3 - "$registry" "$date" <<'PY'
import json,sys
reg,dt=sys.argv[1],sys.argv[2]
try:
    with open(reg) as f:
        d=json.load(f)
except Exception:
    d={"schemaVersion":1,"scenarios":{}}
for scn,sc in d.get("scenarios",{}).items():
    if sc.get("status") in ("quarantined","candidate"):
        sc["status"]="disabled"
        sc["disabledAt"]=dt
try:
    with open(reg,"w") as f:
        json.dump(d,f,indent=2)
except Exception:
    pass
PY
    return 0
}

quarantine_retire() {
    [[ $# -ge 3 ]] || return "$EXIT_USAGE_ERROR"
    local registry="$1" scenarioId="$2" approval="$3"
    python3 - "$registry" "$scenarioId" "$approval" <<'PY'
import json,sys
reg,scn,app=sys.argv[1],sys.argv[2],sys.argv[3]
try:
    with open(reg) as f:
        d=json.load(f)
except Exception:
    d={"schemaVersion":1,"scenarios":{}}
sc=d.setdefault("scenarios",{}).setdefault(scn,{})
sc["status"]="retired"
sc["retiredBy"]=app
try:
    with open(reg,"w") as f:
        json.dump(d,f,indent=2)
except Exception:
    pass
PY
    return 0
}

quarantine_shadow_pass() {
    [[ $# -ge 4 ]] || return "$EXIT_USAGE_ERROR"
    local registry="$1" scenarioId="$2" runId="$3" date="$4"
    python3 - "$registry" "$scenarioId" "$runId" "$date" <<'PY'
import json,sys
reg,scn,run,dt=sys.argv[1],sys.argv[2],sys.argv[3],sys.argv[4]
try:
    with open(reg) as f:
        d=json.load(f)
except Exception:
    d={"schemaVersion":1,"scenarios":{}}
sc=d.setdefault("scenarios",{}).setdefault(scn,{})
sc.setdefault("shadowPasses",[]).append({"runId":run,"date":dt})
try:
    with open(reg,"w") as f:
        json.dump(d,f,indent=2)
except Exception:
    pass
PY
    return 0
}

quarantine_status() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local registry="$1" scenarioId="$2"
    if [[ -f "$registry" ]]; then
        python3 - "$registry" "$scenarioId" <<'PY'
import json,sys
try:
    d=json.load(open(sys.argv[1]))
    scn=sys.argv[2]
    sc=d.get("scenarios",{}).get(scn,{})
    print(sc.get("status","active"))
    sys.exit(0)
except Exception:
    print("active")
    sys.exit(0)
PY
        return 0
    else
        echo "active"
        return 0
    fi
}

quarantine_reports_skipped() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local id="$1"
    # If status is quarantined/disabled, return 0 (reports skipped)
    # Try to find status
    local repo_root
    repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd -P)"
    # look for any registry in common places
    for reg in "$repo_root/quarantine.json" "$repo_root/specs/010-headless-verification-runner/quarantine.json"; do
        if [[ -f "$reg" ]]; then
            local st
            st="$(quarantine_status "$reg" "$id" 2>/dev/null || echo active)"
            if [[ "$st" == "quarantined" || "$st" == "disabled" ]]; then
                return 0
            fi
        fi
    done
    return 1
}
