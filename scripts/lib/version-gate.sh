# scripts/lib/version-gate.sh — SMAPI SemanticVersion classification (spec 010, T059).

if [ -n "${_LR_VERSION_GATE_LOADED:-}" ]; then
    return 0 2>/dev/null || exit 0
fi
_LR_VERSION_GATE_LOADED=1

# shellcheck source=common.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

# version_gate_classify <game_version> <smapi_version> <mod_min_game> <mod_min_api> [matrix_file]
# Also accepts file paths: if an argument is an existing file it is resolved:
#   - manifest.json -> MinimumGameVersion / MinimumApiVersion
#   - *.dll -> version string extracted via strings (best-effort)
# Prints tier: HARD-FAIL | WARN | INFO. Returns 19 on hard-fail.
version_gate_classify() {
    if [[ $# -lt 2 ]]; then
        log_error "version_gate_classify: usage: version_gate_classify <game> <smapi> [mod_min_game] [mod_min_api] [matrix]"
        return "$EXIT_USAGE_ERROR"
    fi

    local matrix="${5:-specs/010-headless-verification-runner/compat-matrix.json}"
    # If matrix not provided but arg4 looks like a file path to matrix, adjust.
    if [[ $# -ge 4 && "$4" == *.json && -f "$4" ]]; then
        matrix="$4"
        set -- "$1" "$2" "$3" "" "$matrix"
    fi

    local game_ver="$1"
    local smapi_ver="$2"
    local mod_min_game="${3:-}"
    local mod_min_api="${4:-}"

    # Resolve file paths.
    _vg_resolve() {
        local v="$1"
        if [[ -f "$v" ]]; then
            case "$v" in
                *.json)
                    python3 - "$v" <<'PY'
import json,sys
d=json.load(open(sys.argv[1]))
print(d.get("MinimumGameVersion") or d.get("minimumGameVersion") or d.get("Version") or "")
PY
                    ;;
                *.dll)
                    strings "$v" 2>/dev/null | grep -E '^[0-9]+\.[0-9]+(\.[0-9]+)?(-[A-Za-z0-9.]+)?$' | head -1
                    ;;
                *)
                    cat "$v" 2>/dev/null | head -1
                    ;;
            esac
        else
            printf '%s' "$v"
        fi
    }

    if [[ -f "$game_ver" ]]; then game_ver="$(_vg_resolve "$game_ver")"; fi
    if [[ -f "$smapi_ver" ]]; then smapi_ver="$(_vg_resolve "$smapi_ver")"; fi

    # Manifest-derived minimums if mod_min_game empty and a manifest path was given.
    if [[ -z "$mod_min_game" && -f "${3:-}" ]]; then
        mod_min_game="$(python3 - "$3" <<'PY'
import json,sys
d=json.load(open(sys.argv[1]))
print(d.get("MinimumGameVersion") or d.get("minimumGameVersion") or "")
PY
)"
        mod_min_api="$(python3 - "$3" <<'PY'
import json,sys
d=json.load(open(sys.argv[1]))
print(d.get("MinimumApiVersion") or d.get("minimumApiVersion") or "")
PY
)"
    fi

    # Defaults from compat matrix if still empty.
    if [[ -z "$mod_min_game" ]]; then
        mod_min_game="$(python3 - "$matrix" <<'PY'
import json,sys
try:
    d=json.load(open(sys.argv[1]))
except Exception:
    d={}
print(d.get("stardewValley",{}).get("minimumGameVersion","1.6.14"))
PY
)"
    fi
    if [[ -z "$mod_min_api" ]]; then
        mod_min_api="$(python3 - "$matrix" <<'PY'
import json,sys
try:
    d=json.load(open(sys.argv[1]))
except Exception:
    d={}
print(d.get("smapi",{}).get("minimumApiVersion","4.4.0"))
PY
)"
    fi

    python3 - "$game_ver" "$smapi_ver" "$mod_min_game" "$mod_min_api" "$matrix" <<'PY'
import sys, json, re

def parse(v):
    v = (v or "").strip()
    channel = ""
    if "-" in v:
        v, channel = v.split("-", 1)
    parts = v.split(".")
    nums = []
    for p in parts:
        # strip non-numeric suffix
        m = re.match(r"(\d+)", p)
        nums.append(int(m.group(1)) if m else 0)
    while len(nums) < 3:
        nums.append(0)
    # prerelease: unofficial sorts before release (empty channel)
    pre_key = 0 if channel == "" else 1
    return (nums[0], nums[1], nums[2], pre_key, channel)

game = parse(sys.argv[1])
smapi = parse(sys.argv[2])
mod_min_game = parse(sys.argv[3])
mod_min_api = parse(sys.argv[4])
matrix_path = sys.argv[5]

try:
    with open(matrix_path) as f:
        matrix = json.load(f)
except Exception:
    matrix = {}

# Minimum checks (compare core triple + prerelease)
if game[:3] < mod_min_game[:3] or (game[:3] == mod_min_game[:3] and game[3] > mod_min_game[3]):
    # game below min OR prerelease when min is release -> hard fail
    pass

# Hard-fail if game core < min core, or smapi core < min core
if game[:3] < mod_min_game[:3] or smapi[:3] < mod_min_api[:3]:
    print("HARD-FAIL")
    sys.exit(19)

# prerelease below release minimum: if min has no channel and observed has channel -> warn? spec: below minimums hard-fail; treat prerelease as below
if mod_min_game[3] == 0 and game[3] == 1 and game[:3] == mod_min_game[:3]:
    print("HARD-FAIL")
    sys.exit(19)
if mod_min_api[3] == 0 and smapi[3] == 1 and smapi[:3] == mod_min_api[:3]:
    print("HARD-FAIL")
    sys.exit(19)

# Null MaximumGameVersion => upper-bound deviation => warn
max_game_raw = None
has_max_key = False
for key in ("MaximumGameVersion", "maximumGameVersion", "maxGameVersion"):
    if key in matrix or key in matrix.get("stardewValley", {}):
        has_max_key = True
        v = matrix.get(key, matrix.get("stardewValley", {}).get(key))
        max_game_raw = v
        break

tier = "INFO"

# pinned game version exact match
pinned = None
for key in ("pinnedGameVersion", "pinned"):
    if key in matrix:
        pinned = matrix.get(key)
    elif key in matrix.get("stardewValley", {}):
        pinned = matrix.get("stardewValley", {}).get(key)
if pinned:
    if parse(pinned)[:3] != game[:3] or parse(pinned)[3] != game[3]:
        # newer or different -> warn (exact match info)
        tier = "WARN"

# Null maximum => warn
if has_max_key and max_game_raw is None:
    tier = "WARN"

# If maximum present and observed exceeds => warn
if max_game_raw is not None:
    try:
        mx = parse(max_game_raw)
        if game[:3] > mx[:3]:
            tier = "WARN"
    except Exception:
        pass

# If no pinned but matrix minimum line differs, newer-than-supported => warn
# Newer unverified: if game greater than matrix's known supported (minimum line) but not exact pinned, warn
# (already handled by pinned mismatch)

# Unverified newer: if game core > supported max inferred from line? keep simple.

print(tier)
PY
    return $?
}

version_gate() { version_gate_classify "$@"; }

export -f version_gate_classify version_gate
