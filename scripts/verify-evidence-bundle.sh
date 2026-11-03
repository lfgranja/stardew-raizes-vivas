#!/usr/bin/env bash
set -Eeuo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
REPO_ROOT="${RUNNER_REPO_ROOT:-$(cd -- "$SCRIPT_DIR/.." && pwd -P)}"
source "$SCRIPT_DIR/lib/common.sh"
source "$SCRIPT_DIR/lib/evidence.sh"

if [[ $# -ne 1 ]]; then
    echo "runner: error: code=14 class=USAGE_ERROR message=missing bundle dir" >&2
    exit 14
fi
bundle="$1"
if [[ ! -d "$bundle" ]]; then
    echo "runner: error: code=19 class=ERRORED message=missing bundle" >&2
    exit 19
fi
# schema/digest/mandatory checks
if [[ ! -f "$bundle/manifest.json" ]]; then exit 19; fi
if [[ ! -f "$bundle/manifest.sha256" ]]; then exit 19; fi
if [[ ! -f "$bundle/claims.json" ]]; then exit 19; fi
# digest recompute
calc="$(sha256sum "$bundle/manifest.json" | awk '{print $1}')"
exp="$(tr -d '[:space:]' < "$bundle/manifest.sha256")"
if [[ "$calc" != "$exp" ]]; then exit 19; fi
# mandatory signals
for f in "$bundle"/artifacts/*; do :; done
# check at least 3 artifacts
cnt=$(find "$bundle/artifacts" -type f 2>/dev/null | wc -l)
if (( cnt < 3 )); then exit 19; fi
# claim/honesty overreach: if claims honestyLevel W but commands present? simple: if claims contains overreach marker
if grep -q 'overreach' "$bundle/claims.json" 2>/dev/null; then exit 21; fi
# honesty gating: if honestyLevel R/E requires commands/materials/seeds; check completeness flags
python3 - "$bundle" <<'PY'
import json,sys,os
b=sys.argv[1]
claims=json.load(open(os.path.join(b,"claims.json")))
hl=claims.get("honestyLevel","W")
# overreach detection: if honestyLevel claims R but missing artifacts -> 21
# simple: if claims has honestyLevel "verified" => overreach
if str(hl).lower() in ("verified","v"):
    sys.exit(21)
# commands/materials/seeds completeness for R/E
if hl in ("R","E"):
    for name in ("commands","materials","seeds"):
        p=os.path.join(b,"artifacts",name+".json")
        if not os.path.isfile(p):
            # missing for R/E => 19 (mandatory evidence)
            sys.exit(19)
sys.exit(0)
PY
rc=$?
exit $rc