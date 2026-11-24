#!/usr/bin/env bash
if [ -n "${_LR_PERFORMANCE_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_PERFORMANCE_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

PERF_GC_DELTA=0
PERF_GC_ATTRIBUTED=""

performance_evaluate() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local jsonl="$1"
    [[ -f "$jsonl" ]] || return 19
    python3 - "$jsonl" "$RunnerPerLocationBudgetMs" <<'PY'
import json,sys
path,budget=sys.argv[1],int(sys.argv[2])
lines=[l for l in open(path) if l.strip()]
# discard one warmup line
if lines:
    lines=lines[1:]
gc_delta=0
for l in lines:
    try:
        o=json.loads(l)
    except Exception:
        continue
    ms=o.get("elapsedMilliseconds",0)
    gc=o.get("gen0Collections",0)
    gc_delta+=gc if isinstance(gc,int) else 0
    if ms is not None and ms>budget:
        print("GC_DELTA="+str(gc_delta))
        sys.exit(18)
# output gc delta to env via stdout marker
print("GC_DELTA="+str(gc_delta))
sys.exit(0)
PY
    local rc=$?
    # capture GC delta from output? store in global via re-run
    local out
    out="$(python3 - "$jsonl" "$RunnerPerLocationBudgetMs" <<'PY'
import json,sys
path,budget=sys.argv[1],int(sys.argv[2])
lines=[l for l in open(path) if l.strip()]
if lines: lines=lines[1:]
gc=0
for l in lines:
    try: o=json.loads(l)
    except: continue
    g=o.get("gen0Collections",0)
    if isinstance(g,int): gc+=g
print(gc)
PY
)"
    PERF_GC_DELTA="$out"
    if (( PERF_GC_DELTA > 0 )); then PERF_GC_ATTRIBUTED="gc-attributed"; else PERF_GC_ATTRIBUTED=""; fi
    return $rc
}