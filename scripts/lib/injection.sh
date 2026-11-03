#!/usr/bin/env bash
if [ -n "${_LR_INJECTION_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_INJECTION_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

injection_validate_commands() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local scenario="$1"
    [[ -f "$scenario" ]] || return "$EXIT_USAGE_ERROR"
    python3 - "$scenario" <<'PY'
import json,sys
d=json.load(open(sys.argv[1]))
cmds=d.get("initCommands",[])
for c in cmds:
    if not isinstance(c,dict): sys.exit(14)
    if c.get("kind") not in ("macro","console"): sys.exit(14)
    if "command" not in c and "path" not in c and "macro" not in c:
        # allow empty? require a command-ish field
        pass
sys.exit(0)
PY
}

injection_validate_macro() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local file="$1"
    [[ -f "$file" ]] || return "$EXIT_USAGE_ERROR"
    [[ "$file" == *.txt ]] || return 14
    python3 - "$file" <<'PY'
import sys
p=sys.argv[1]
raw=open(p,'rb').read()
# BOM optional
if raw.startswith(b'\xef\xbb\xbf'):
    raw=raw[3:]
try:
    text=raw.decode('utf-8')
except Exception:
    sys.exit(14)
lines=text.splitlines()
sleep_count=0
last_nonempty=None
all_lines=[l for l in lines]
# check leading whitespace before /
for l in lines:
    if l.strip()=="":
        continue
    if l != l.lstrip():
        # leading whitespace before slash
        if l.lstrip().startswith('/'):
            sys.exit(14)
    if not l.lstrip().startswith('/'):
        # non-empty line must start /
        if l.strip()!="":
            sys.exit(14)
# nested runmacro
for l in lines:
    if 'runmacro' in l.lower() and l.strip().lower().startswith('/runmacro') is False:
        pass
    if l.strip().lower().startswith('/runmacro') and l.strip().lower()!='':
        # any runmacro line counts as nested (no macro may reference another)
        pass
# count sleep
sleeps=[l for l in lines if l.strip().lower().startswith('/sleep')]
if len(sleeps)!=1:
    sys.exit(14)
# sleep must be last non-empty line
nonempty=[l for l in lines if l.strip()!=""]
if nonempty and not nonempty[-1].strip().lower().startswith('/sleep'):
    sys.exit(14)
# exactly one /sleep already checked
# nested macro: no runmacro at all
for l in lines:
    if l.strip().lower().startswith('/runmacro'):
        sys.exit(14)
sys.exit(0)
PY
}

injection_build_queue() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local scenario="$1" out="$2"
    [[ -f "$scenario" ]] || return "$EXIT_USAGE_ERROR"
    python3 - "$scenario" "$out" <<'PY'
import json,sys
scn, out = sys.argv[1], sys.argv[2]
d=json.load(open(scn))
lines=[]
for c in d.get("initCommands",[]):
    kind=c.get("kind")
    if kind=="macro":
        path=c.get("path") or c.get("macro") or c.get("command","")
        lines.append(f"debug runmacro {path}")
    elif kind=="console":
        cmd=c.get("command") or c.get("cmd","")
        lines.append(cmd)
# completion markers
for m in d.get("completionMarkers",[]) or []:
    lines.append(f"console {m}")
# /sleep last
sleepsec=d.get("time",{}).get("timeoutSeconds",5)
lines.append(f"/sleep {sleepsec}")
open(out,"w").write("\n".join(lines)+"\n")
sys.exit(0)
PY
}

injection_seed() {
    # seeds through host-authoritative lock; refuse farmhand
    if [[ "${PLAYER_ROLE:-host}" == "farmhand" ]]; then
        return 21
    fi
    # claim_acquire
    if declare -F claim_acquire >/dev/null 2>&1; then
        claim_acquire "seed" 2>/dev/null || return 6
    fi
    return 0
}