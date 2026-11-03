#!/usr/bin/env bash
if [ -n "${_LR_PREFLIGHT_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_PREFLIGHT_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

PREFLIGHT_GAME_PATH=""; PREFLIGHT_MISSING=""

preflight_resolve_game_path() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local val="$1"
    # trim
    val="${val#"${val%%[![:space:]]*}"}"
    val="${val%"${val##*[![:space:]]}"}"
    if [[ -z "$val" ]]; then return "$EXIT_USAGE_ERROR"; fi
    if [[ -d "$val" ]]; then PREFLIGHT_GAME_PATH="$val"; return 0; fi
    return "$EXIT_MISSING_DEPENDENCY"
}

preflight_check_assemblies() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local dir="$1"
    local missing=""
    for f in "Stardew Valley.dll" "StardewModdingAPI" "StardewModdingAPI.dll" \
             "smapi-internal/SMAPI.Toolkit.dll" "smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll" "steam_appid.txt"; do
        if [[ ! -e "$dir/$f" ]]; then
            missing="$missing $dir/$f"
        fi
    done
    if [[ -n "$missing" ]]; then
        PREFLIGHT_MISSING="$missing"
        printf '%s\n' "$missing" >&2
        return 9
    fi
    PREFLIGHT_MISSING=""
    return 0
}

preflight_check_deployment() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local mods="$1"
    local missing=""
    [[ -f "$mods/LivingRoots/LivingRoots.dll" ]] || missing="$missing $mods/LivingRoots/LivingRoots.dll"
    [[ -f "$mods/LivingRoots.Verification/LivingRoots.Verification.dll" ]] || missing="$missing $mods/LivingRoots.Verification/LivingRoots.Verification.dll"
    if [[ -n "$missing" ]]; then
        printf '%s\n' "$missing" >&2
        return 12
    fi
    return 0
}

preflight_host_tools() {
    local missing=""
    for t in "$@"; do
        command -v "$t" >/dev/null 2>&1 || missing="$missing $t"
    done
    if [[ -n "$missing" ]]; then
        printf '%s\n' "$missing" >&2
        return 9
    fi
    return 0
}

preflight_non_root() {
    if (( EUID == 0 )); then return 21; fi
    return 0
}

preflight_record_blocked() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" prereq="$2" checked="$3"
    mkdir -p "$bundle"
    python3 - "$bundle/blocked.json" "$prereq" "$checked" <<'PY'
import json,sys,datetime
path,prereq,checked=sys.argv[1],sys.argv[2],sys.argv[3]
d={"schemaVersion":1,"status":"BLOCKED","missingPrerequisite":prereq,"checkedPath":checked,
   "recordedAt":datetime.datetime.now(datetime.timezone.utc).isoformat().replace("+00:00","Z")}
json.dump(d,open(path,"w"),indent=2)
PY
    return 0
}

preflight_version_gate() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local dir="$1" manifest="$2"
    # delegate to version-gate.sh classify if possible
    if declare -F version_gate_classify >/dev/null 2>&1; then
        version_gate_classify "$dir" "$manifest" >/dev/null 2>&1
        local rc=$?
        if (( rc == 19 )); then return 19; fi
    fi
    return 0
}

# Comprehensive preflight orchestrator
preflight_run() {
    local game_path="${1:-}"
    local mods_dir="${2:-}"
    local bundle_dir="${3:-}"
    local skip_build="${4:-0}"
    
    # Non-root guard first (POLICY)
    if (( EUID == 0 )); then
        return "$EXIT_POLICY"
    fi
    
    # Handle --game-path / STARDEW_GAME_PATH
    if [[ -z "$game_path" ]]; then
        game_path="${STARDEW_GAME_PATH:-}"
    fi
    
    if [[ -z "$game_path" ]]; then
        if [[ -n "$bundle_dir" ]]; then
            preflight_record_blocked "$bundle_dir" "STARDEW_GAME_PATH" "${STARDEW_GAME_PATH:-}" || true
        fi
        return "$EXIT_MISSING_DEPENDENCY"
    fi
    
    # Trim whitespace
    game_path="${game_path#"${game_path%%[![:space:]]*}"}"
    game_path="${game_path%"${game_path##*[![:space:]]}"}"
    
    if [[ -z "$game_path" ]]; then
        if [[ -n "$bundle_dir" ]]; then
            preflight_record_blocked "$bundle_dir" "STARDEW_GAME_PATH" "" || true
        fi
        return "$EXIT_USAGE_ERROR"
    fi
    
    if [[ ! -d "$game_path" ]]; then
        if [[ -n "$bundle_dir" ]]; then
            preflight_record_blocked "$bundle_dir" "STARDEW_GAME_PATH" "$game_path" || true
        fi
        return "$EXIT_MISSING_DEPENDENCY"
    fi
    
    PREFLIGHT_GAME_PATH="$game_path"
    
    # Check six required assemblies
    local missing_asm=""
    for f in "Stardew Valley.dll" "StardewModdingAPI" "StardewModdingAPI.dll" \
             "smapi-internal/SMAPI.Toolkit.dll" "smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll" "steam_appid.txt"; do
        if [[ ! -e "$game_path/$f" ]]; then
            missing_asm="$missing_asm $game_path/$f"
        fi
    done
    if [[ -n "$missing_asm" ]]; then
        PREFLIGHT_MISSING="$missing_asm"
        if [[ -n "$bundle_dir" ]]; then
            preflight_record_blocked "$bundle_dir" "$missing_asm" "$game_path" || true
        fi
        return "$EXIT_MISSING_DEPENDENCY"
    fi
    
    # Check deployment
    local mods_check="${mods_dir:-$game_path/Mods}"
    if [[ $skip_build -eq 1 ]]; then
        # Check if artifacts exist for skip-build case
        if [[ ! -f "$mods_check/LivingRoots/LivingRoots.dll" ]] || [[ ! -f "$mods_check/LivingRoots.Verification/LivingRoots.Verification.dll" ]]; then
            if [[ -n "$bundle_dir" ]]; then
                preflight_record_blocked "$bundle_dir" "build-artifact" "$mods_check" || true
            fi
            return "$EXIT_BUILD_ARTIFACT_FAILURE"
        fi
    else
        local missing_dep=""
        [[ -f "$mods_check/LivingRoots/LivingRoots.dll" ]] || missing_dep="$missing_dep $mods_check/LivingRoots/LivingRoots.dll"
        [[ -f "$mods_check/LivingRoots.Verification/LivingRoots.Verification.dll" ]] || missing_dep="$missing_dep $mods_check/LivingRoots.Verification/LivingRoots.Verification.dll"
        if [[ -n "$missing_dep" ]]; then
            if [[ -n "$bundle_dir" ]]; then
                preflight_record_blocked "$bundle_dir" "$missing_dep" "$mods_check" || true
            fi
            return "$EXIT_BUILD_ARTIFACT_FAILURE"
        fi
    fi
    
    # Version gate before Xvfb
    local manifest_check="$mods_check/LivingRoots/manifest.json"
    if [[ ! -f "$manifest_check" ]]; then
        manifest_check="$game_path/Mods/LivingRoots/manifest.json"
    fi
    if declare -F version_gate_classify >/dev/null 2>&1; then
        version_gate_classify "$game_path" "$manifest_check" >/dev/null 2>&1
        local vrc=$?
        if (( vrc == 19 )); then
            if [[ -n "$bundle_dir" ]]; then
                preflight_record_blocked "$bundle_dir" "version-mismatch" "$manifest_check" || true
            fi
            return 19
        fi
    fi
    
    return 0
}
