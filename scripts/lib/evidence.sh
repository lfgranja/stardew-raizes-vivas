#!/usr/bin/env bash
if [ -n "${_LR_EVIDENCE_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_EVIDENCE_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/retention.sh"

evidence_run_id() { runner_run_id; }

evidence_bundle_dir() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    printf '%s/evidence-bundle-%s\n' "$1" "$2"
    return 0
}

evidence_init() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local root="$1" runId="$2"
    local bdir
    bdir="$(evidence_bundle_dir "$root" "$runId")"
    mkdir -p "$bdir/artifacts" "$bdir/attempts"
    return 0
}

evidence_add_artifact() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" path="$2"
    [[ -f "$path" ]] || return 19
    local name
    name="$(basename "$path")"
    cp -f "$path" "$bundle/artifacts/$name"
    return 0
}

evidence_attempt_dir() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    printf '%s/attempts/%s\n' "$1" "$2"
    return 0
}

evidence_record_attempt() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" attemptNum="$2"
    local verdict="${3:-errored}"
    local failureClass="${4:-}"
    local signals="${5:-}"
    local exitCode="${6:-19}"
    local attempt_dir
    attempt_dir="$(evidence_attempt_dir "$bundle" "$attemptNum")"
    mkdir -p "$attempt_dir"
    python3 - "$attempt_dir/attempt.json" "$attemptNum" "$verdict" "$failureClass" "$signals" "$exitCode" <<'PY'
import json,sys
path,attempt,verdict,fc,signals,ec=sys.argv[1:7]
d={"attempt":int(attempt),"verdict":verdict,"failureClass":fc,"signals":signals,"exitCode":int(ec)}
json.dump(d,open(path,"w"),indent=2)
PY
    # append to attempts[] in a tracking file for manifest input (append-only)
    local attempts_list="$bundle/.attempts_list"
    local entry="attempts/${attemptNum}/attempt.json"
    if [[ -f "$attempts_list" ]]; then
        if grep -Fxq "$entry" "$attempts_list"; then
            return 0
        fi
    fi
    printf '%s\n' "$entry" >> "$attempts_list"
    return 0
}

evidence_finalize() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" input="$2"
    mkdir -p "$bundle"
    local manifest_input="$bundle/.manifest-input.json"
    # Build manifest input from provided input or synthesize
    if [[ -f "$input" ]]; then
        cp -f "$input" "$manifest_input"
    else
        printf '{"runId":""}\n' > "$manifest_input"
    fi
    # Ensure attempts[] is populated if tracked
    if [[ -f "$bundle/.attempts_list" ]]; then
        python3 - "$manifest_input" "$bundle/.attempts_list" <<'PY'
import json,sys
mip=sys.argv[1]
lst=sys.argv[2]
try:
    d=json.load(open(mip))
except Exception:
    d={}
attempts=[]
try:
    with open(lst) as f:
        for line in f:
            line=line.strip()
            if line:
                attempts.append(line)
except Exception:
    attempts=[]
# append-only semantics
if "attempts" not in d or not isinstance(d.get("attempts"), list):
    d["attempts"]=attempts
else:
    # ensure append-only (only add missing)
    existing=set(d["attempts"])
    for a in attempts:
        if a not in existing:
            d["attempts"].append(a)
json.dump(d,open(mip,"w"),indent=2)
PY
    fi
    # invoke ManifestBuilder (the single producer)
    if command -v dotnet >/dev/null 2>&1; then
        local dll
        for cand in "LivingRoots.Verification.Runner/bin/Release/net6.0/LivingRoots.Verification.Runner.dll" "LivingRoots.Verification.Runner/bin/Debug/net6.0/LivingRoots.Verification.Runner.dll" "$(dirname "$SCRIPT_DIR")/LivingRoots.Verification.Runner/bin/Release/net6.0/LivingRoots.Verification.Runner.dll"; do
            [[ -f "$cand" ]] && dll="$cand" && break
        done
        if [[ -n "${dll:-}" ]]; then
            dotnet "$dll" build-manifest "$bundle" "$manifest_input" >/dev/null 2>&1
            local rc=$?
            if (( rc == 0 )); then
                # manifest.sha256 must be written only after finalization by ManifestBuilder
                # wire retention receipt/export
                retention_receipt "$bundle" >/dev/null 2>&1 || true
                retention_export "$bundle" "$bundle" >/dev/null 2>&1 || true
                return 0
            fi
        fi
    fi
    return 13
}

evidence_write_claims() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" runId="$2" honesty="$3"
    python3 - "$bundle/claims.json" "$runId" "$honesty" <<'PY'
import json,sys
path,runId,honesty=sys.argv[1],sys.argv[2],sys.argv[3]
d={"schemaVersion":1,"runId":runId,"honestyLevel":honesty}
json.dump(d,open(path,"w"),indent=2)
PY
    return 0
}

evidence_write_integrity_failure() {
    [[ $# -ge 3 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" class="$2" artifact="$3" detail="${4:-}"
    mkdir -p "$bundle"
    python3 - "$bundle/integrity-failure.json" "$class" "$artifact" "$detail" <<'PY'
import json,sys,datetime
path,cls,art,detail=sys.argv[1],sys.argv[2],sys.argv[3],sys.argv[4]
d={"schemaVersion":1,"runId":"","class":cls,"artifact":art,"recordedAt":datetime.datetime.now(datetime.timezone.utc).isoformat().replace("+00:00","Z")}
if detail: d["detail"]=detail
json.dump(d,open(path,"w"),indent=2)
PY
    return 0
}

evidence_validate() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1"
    [[ -f "$bundle/manifest.json" ]] || return 19
    [[ -f "$bundle/manifest.sha256" ]] || return 19
    [[ -f "$bundle/claims.json" ]] || return 19
    return 0
}

export -f evidence_run_id evidence_bundle_dir evidence_init evidence_add_artifact \
       evidence_attempt_dir evidence_record_attempt evidence_finalize \
       evidence_write_claims evidence_write_integrity_failure evidence_validate
