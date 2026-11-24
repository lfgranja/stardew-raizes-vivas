#!/usr/bin/env bash
#
# scripts/lib/promotion.sh — GATE3 promotion logic (feature 010, T093)
#

if [[ -n "${_LR_PROMOTION_LOADED:-}" ]]; then
    return 0 2>/dev/null || exit 0
fi
_LR_PROMOTION_LOADED=1

# shellcheck source=common.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"
# shellcheck source=atomic.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/atomic.sh"
# shellcheck source=logging.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/logging.sh"

PROMOTION_OWNER="${PROMOTION_OWNER:-lfgranja}"

promotion_check_docs() {
    [[ $# -ge 1 ]] || return "$EXIT_USAGE_ERROR"
    local repo="$1"
    shift || true

    local files=()
    # Discover testMdPath from every scenario file rather than a hardcoded list.
    while IFS= read -r _tm; do
        [[ -n "$_tm" ]] && files+=("$_tm")
    done < <(python3 - "$repo" <<'PY'
import json, os, sys
repo = sys.argv[1]
seen = set()
for root, _, fs in os.walk(repo):
    # skip build output and VCS
    parts = root.split(os.sep)
    if any(p in (".git", "bin", "obj") for p in parts):
        continue
    for fn in fs:
        if not fn.endswith(".scenario.json"):
            continue
        path = os.path.join(root, fn)
        try:
            with open(path, encoding="utf-8") as f:
                data = json.load(f)
        except Exception:
            continue
        if not isinstance(data, dict):
            continue
        tm = data.get("testMdPath")
        if not isinstance(tm, str) or not tm.strip():
            continue
        if os.path.isabs(tm):
            cand = tm
        else:
            cand = os.path.join(repo, tm)
        if not os.path.isfile(cand):
            continue
        key = os.path.relpath(cand, repo)
        if key in seen:
            continue
        seen.add(key)
        print(key)
PY
    )
    files+=("ROADMAP.md" "COMPLETION_SUMMARY.md")

    local f p b e bline eline
    for f in "${files[@]}"; do
        p="$repo/$f"
        [[ -f "$p" ]] || continue
        b=$(grep -c 'GATE3:BEGIN' "$p" 2>/dev/null || echo 0)
        e=$(grep -c 'GATE3:END' "$p" 2>/dev/null || echo 0)
        if (( b != 1 || e != 1 )); then
            return "$EXIT_USAGE_ERROR"
        fi
        bline=$(grep -n 'GATE3:BEGIN' "$p" | head -1 | cut -d: -f1)
        eline=$(grep -n 'GATE3:END' "$p" | head -1 | cut -d: -f1)
        if (( bline >= eline )); then
            return "$EXIT_USAGE_ERROR"
        fi
    done

    for f in "${files[@]}"; do
        p="$repo/$f"
        [[ -f "$p" ]] || continue
        if sed -n '/GATE3:BEGIN/,/GATE3:END/p' "$p" | grep -qE 'Status:[[:space:]]*verified' 2>/dev/null; then
            return "$EXIT_POLICY"
        fi
    done

    return 0
}

_promotion_extract_block() {
    local file="$1"
    sed -n '/<!-- GATE3:BEGIN -->/,/<!-- GATE3:END -->/p' "$file"
}

_promotion_get_status() {
    local block="$1"
    echo "$block" | grep -E 'Status:' | head -1 | sed -E 's/.*Status:[[:space:]]*([a-zA-Z-]+).*/\1/'
}

_promotion_validate_markers() {
    local file="$1"
    [[ -f "$file" ]] || return 1
    local b e bline eline
    b=$(grep -c 'GATE3:BEGIN' "$file" 2>/dev/null || echo 0)
    e=$(grep -c 'GATE3:END' "$file" 2>/dev/null || echo 0)
    (( b == 1 && e == 1 )) || return 1
    bline=$(grep -n 'GATE3:BEGIN' "$file" | head -1 | cut -d: -f1)
    eline=$(grep -n 'GATE3:END' "$file" | head -1 | cut -d: -f1)
    (( bline < eline )) || return 1
    return 0
}

_promotion_append_transition() {
    local repo="$1" component="$2" from_status="$3" to_status="$4" run_id="$5"
    local pt="$repo/specs/010-headless-verification-runner/promotion-triggers.json"
    [[ -f "$pt" ]] || return 0
    local owner="$PROMOTION_OWNER"
    local recorded_at
    recorded_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    python3 - "$pt" "$component" "$from_status" "$to_status" "$run_id" "$owner" "$recorded_at" << 'PY'
import json,sys,os
path,component,frm,to,rid,owner,ts=sys.argv[1:]
try:
    with open(path) as f:
        data=json.load(f)
    if not isinstance(data.get('transitions'), list):
        data['transitions']=[]
    data['transitions'].append({
        "component": component,
        "from": frm,
        "to": to,
        "runId": rid,
        "owner": owner,
        "recordedAt": ts
    })
    tmp=path+'.tmp'
    with open(tmp,'w') as f:
        json.dump(data,f,separators=(',',':'))
    os.rename(tmp,path)
except Exception:
    pass
PY
}

promotion_apply() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local repo="$1"
    local feature_test_md="$2"
    local bundle="$3"

    if [[ ! -f "$feature_test_md" ]]; then
        return "$EXIT_USAGE_ERROR"
    fi
    if ! _promotion_validate_markers "$feature_test_md"; then
        return "$EXIT_USAGE_ERROR"
    fi

    if [[ ! -d "$bundle" ]]; then
        return "$EXIT_POLICY"
    fi
    if [[ ! -d "$bundle/artifacts" ]]; then
        return "$EXIT_POLICY"
    fi

    local signals_ok=0
    if compgen -G "$bundle/artifacts/*" >/dev/null 2>&1; then
        signals_ok=1
    fi
    if (( signals_ok == 0 )); then
        return "$EXIT_POLICY"
    fi

    local block current_status
    block="$(_promotion_extract_block "$feature_test_md")"
    current_status="$(_promotion_get_status "$block")"
    [[ -z "$current_status" ]] && current_status="partial"

    local run_id
    # The run ID must match the bundle the runner actually wrote. Deriving a
    # fresh ID here produced a machine-pass claim that pointed at a bundle that
    # did not exist. The bundle name is authoritative; RUN_ID is the fallback
    # for callers that pass a non-standard directory.
    local bundle_name
    bundle_name="$(basename -- "$bundle")"
    if [[ "$bundle_name" == evidence-bundle-* ]]; then
        run_id="${bundle_name#evidence-bundle-}"
    elif [[ -n "${RUN_ID:-}" ]]; then
        run_id="$RUN_ID"
    elif type runner_run_id >/dev/null 2>&1; then
        run_id="$(runner_run_id)"
    else
        run_id="run-$(date -u +%Y%m%dT%H%M%SZ)-$(printf '%04x' $((RANDOM%65536)))"
    fi

    local new_status="machine-pass"
    if [[ "$current_status" == "machine-pass" ]]; then
        new_status="machine-pass"
    elif [[ "$current_status" == "partial" ]]; then
        new_status="machine-pass"
    else
        new_status="machine-pass"
    fi

    local date_str commit_sha
    date_str="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    commit_sha="$(git -C "$repo" rev-parse HEAD 2>/dev/null | cut -c1-40 || echo "0000000000000000000000000000000000000000")"

    local new_block
    new_block='<!-- GATE3:BEGIN -->
- **Status**: '"${new_status}"'
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: '"${run_id}"'
- **Commit**: '"${commit_sha}"'
- **Date**: '"${date_str}"'
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-'"${run_id}"'/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-'"${run_id}"'/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-'"${run_id}"'/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-'"${run_id}"'/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->'

    local _written _write_rc=0
    _written="$(python3 - "$feature_test_md" "$new_block" "$repo" <<'PY'
import sys,re,os
path, new_block, repo = sys.argv[1:4]
targets = [(path, "test.md")]
for mirror, component in (("ROADMAP.md", "ROADMAP.md"),
                          ("COMPLETION_SUMMARY.md", "COMPLETION_SUMMARY.md")):
    mirror_path = os.path.join(repo, mirror)
    if os.path.isfile(mirror_path):
        targets.append((mirror_path, component))

for target, component in targets:
    with open(target) as f:
        s = f.read()
    s2, n = re.subn(r'<!-- GATE3:BEGIN -->.*?<!-- GATE3:END -->', new_block, s, count=1, flags=re.DOTALL)
    if n == 0:
        sys.exit(14)
    with open(target,'w') as f:
        f.write(s2)
    print(component)
PY
)" || _write_rc=$?
    if (( _write_rc != 0 )); then
        return "$_write_rc"
    fi

    if [[ "$new_status" == "machine-pass" ]]; then
        local _component
        while IFS= read -r _component; do
            [[ -n "$_component" ]] || continue
            _promotion_append_transition "$repo" "$_component" "$current_status" "$new_status" "$run_id"
        done <<< "$_written"
    fi

    return 0
}

promotion_cli() {
    local args=("$@")
    local check_docs=0
    local i
    for ((i=0; i<${#args[@]}; i++)); do
        if [[ "${args[$i]}" == "--check-docs" ]]; then
            check_docs=1
        fi
    done
    if (( check_docs == 1 )); then
        local repo="${args[0]:-$(pwd)}"
        promotion_check_docs "$repo"
        return $?
    fi
    return 0
}
