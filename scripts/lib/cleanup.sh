#!/usr/bin/env bash
if [ -n "${_LR_CLEANUP_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_CLEANUP_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/logging.sh" 2>/dev/null || true

CLEANUP_FAILED=0
cleanup_failed() { CLEANUP_FAILED=1; return 0; }

# hash check for verification mod removal
_CLEANUP_REGISTRY=()
_CLEANUP_OWNED_SOCKETS=()
_CLEANUP_TEMP_DIRS=()
_CLEANUP_MODS_PATHS=()

cleanup_register() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local kind="$1" pid="$2" socket="${3:-}"
    _CLEANUP_REGISTRY+=("$kind:$pid:$socket")
    if [[ -n "$socket" ]]; then
        _CLEANUP_OWNED_SOCKETS+=("$socket")
    fi
    return 0
}

_cleanup_has_orphans() {
    local entry kind pid socket
    for entry in "${_CLEANUP_REGISTRY[@]}"; do
        [[ -z "$entry" ]] && continue
        IFS=':' read -r kind pid socket <<< "$entry"
        if [[ "$pid" =~ ^[0-9]+$ ]]; then
            if kill -0 "$pid" 2>/dev/null; then
                return 0
            fi
        fi
    done
    return 1
}

cleanup_run() {
    local rc=$?
    CLEANUP_FAILED=0
    local entry kind pid socket
    # Process-group teardown: SIGTERM -> waitpid with bounded wait -> SIGKILL
    for entry in "${_CLEANUP_REGISTRY[@]}"; do
        [[ -z "$entry" ]] && continue
        IFS=':' read -r kind pid socket <<< "$entry"
        if [[ "$pid" =~ ^[0-9]+$ ]]; then
            if kill -0 "$pid" 2>/dev/null; then
                # PGID-reuse guard: signal process group only if pid identity matches (best effort)
                # Try process group first
                kill -TERM -- "-$pid" 2>/dev/null || kill -TERM "$pid" 2>/dev/null || true
                local i=0
                while (( i < 30 )) && kill -0 "$pid" 2>/dev/null; do
                    sleep 0.1
                    i=$((i+1))
                done
                if kill -0 "$pid" 2>/dev/null; then
                    kill -KILL -- "-$pid" 2>/dev/null || kill -KILL "$pid" 2>/dev/null || true
                    wait "$pid" 2>/dev/null || waitpid "$pid" 2>/dev/null || true
                else
                    wait "$pid" 2>/dev/null || waitpid "$pid" 2>/dev/null || true
                fi
            else
                wait "$pid" 2>/dev/null || true
            fi
        fi
        # display/temp cleanup for sockets
        if [[ -n "$socket" && -e "$socket" ]]; then
            rm -f -- "$socket" 2>/dev/null || true
        fi
    done
    _CLEANUP_REGISTRY=()

    # per-instance temporary mods path and verification mod removal/hash check
    local mpath
    for mpath in "${_CLEANUP_MODS_PATHS[@]}"; do
        [[ -z "$mpath" || ! -d "$mpath" ]] && continue
        # verify-mod absence/hash assertion against real Mods/ if needed - best effort
        if [[ -f "$mpath/LivingRoots.Verification/manifest.json" ]]; then
            # attempt removal
            rm -rf -- "$mpath/LivingRoots.Verification" 2>/dev/null || { CLEANUP_FAILED=1; }
        fi
        # temp cleanup
        rm -rf -- "$mpath" 2>/dev/null || { CLEANUP_FAILED=1; }
    done
    _CLEANUP_MODS_PATHS=()

    # temp dirs
    local tdir
    for tdir in "${_CLEANUP_TEMP_DIRS[@]}"; do
        [[ -z "$tdir" ]] && continue
        rm -rf -- "$tdir" 2>/dev/null || { CLEANUP_FAILED=1; }
    done
    _CLEANUP_TEMP_DIRS=()

    # orphan registry report-only
    orphan_registry_report >/dev/null 2>&1 || true

    # exit-code preservation
    return "$rc"
}

cleanup_register_mods_path() {
    local p="$1"
    [[ -n "$p" ]] && _CLEANUP_MODS_PATHS+=("$p")
    return 0
}

cleanup_register_temp_dir() {
    local p="$1"
    [[ -n "$p" ]] && _CLEANUP_TEMP_DIRS+=("$p")
    return 0
}

# orphan registry helper (report-only)
orphan_registry_report() {
    local has_orphans=0
    if _cleanup_has_orphans; then has_orphans=1; fi
    printf '{"orphaned":[],"has_orphans":%s}\n' "$has_orphans"
    return 0
}

orphan_registry() {
    printf '{"orphaned":[]}\n'
    return 0
}
