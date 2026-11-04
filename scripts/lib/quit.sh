#!/usr/bin/env bash
if [ -n "${_LR_QUIT_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_QUIT_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

CLEAN_QUIT=0

# quit_request <pid> [save_mode] [injection_dir] [generation]
# Prefer the mod-side queue: the launcher runs with stdin closed, so there is
# no console to type into. The legacy two-argument form stays a no-op request
# so existing callers/tests keep working.
quit_request() {
    [[ $# -ge 1 ]] || return "$EXIT_USAGE_ERROR"
    local pid="$1" save_mode="${2:-save}" injection_dir="${3:-}" generation="${4:-2}"
    if [[ -n "$injection_dir" && -d "$injection_dir" ]]; then
        injection_quit "$injection_dir" "$generation" "${RUNNER_QUIT_ACK_TIMEOUT:-60}"
        return $?
    fi
    # No queue channel: the caller will use the process backstop.
    return 0
}

# save_signature <save_dir> — path + size + mtime for every file, hashed.
save_signature() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local dir="$1"
    if [[ -z "$dir" || ! -d "$dir" ]]; then
        printf 'missing\n'
        return 0
    fi
    find "$dir" -type f -printf '%p %s %T@\n' 2>/dev/null | LC_ALL=C sort | sha256sum | cut -d' ' -f1
}

# quit_wait_clean <pid> <save_dir>
# Wait for the process to exit, then require the save tree to stay unchanged
# for RunnerCleanQuitStabilitySeconds. SIGKILL the process group at
# RunnerCleanQuitBackstopSeconds; that path leaves CLEAN_QUIT=0.
quit_wait_clean() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local pid="$1" save_dir="$2"
    CLEAN_QUIT=0
    local stability="${RunnerCleanQuitStabilitySeconds:-30}"
    local backstop="${RunnerCleanQuitBackstopSeconds:-120}"
    [[ "$stability" =~ ^[0-9]+$ ]] || stability=30
    [[ "$backstop" =~ ^[0-9]+$ ]] || backstop=120

    local start now elapsed
    start="$(date -u +%s)"
    # Phase 1: wait for the process to exit.
    while kill -0 "$pid" 2>/dev/null; do
        now="$(date -u +%s)"
        elapsed=$((now - start))
        if (( elapsed >= backstop )); then
            # SIGKILL the process group first, then the pid; the launcher was
            # started with setsid so the group id equals the pid.
            kill -KILL -- "-$pid" 2>/dev/null || kill -KILL "$pid" 2>/dev/null || true
            wait "$pid" 2>/dev/null || true
            CLEAN_QUIT=0
            return 0
        fi
        sleep 1
    done

    # Phase 2: mtime/size stability window. A save that is still being written
    # after the process exits is not a clean save.
    local prev="" stable_since=""
    start="$(date -u +%s)"
    while :; do
        now="$(date -u +%s)"
        elapsed=$((now - start))
        local sig
        sig="$(save_signature "$save_dir")"
        if [[ "$sig" == "$prev" && -n "$sig" ]]; then
            if [[ -z "$stable_since" ]]; then
                stable_since="$now"
            fi
            if (( now - stable_since >= stability )); then
                CLEAN_QUIT=1
                return 0
            fi
        else
            stable_since=""
            prev="$sig"
        fi
        if (( elapsed >= backstop )); then
            CLEAN_QUIT=0
            return 0
        fi
        sleep 1
    done
}

# quit_run — drive generation 2 (lr_quit) and wait for a clean exit.
quit_run() {
    local dir="${RUNNER_INJECTION_DIR:-${RUNNER_RUN_ROOT:-/tmp}/injection}"
    local pid="${LAUNCH_PID:-}"
    if [[ -z "$pid" ]]; then
        CLEAN_QUIT=0
        return 0
    fi
    if [[ -d "$dir" ]]; then
        quit_request "$pid" "${RUNNER_SAVE_MODE:-save}" "$dir" 2 || true
    fi
    quit_wait_clean "$pid" "${RUNNER_SAVE_DIR:-}"
    local rc=$?
    if [[ -n "${RUNNER_RUN_ROOT:-}" ]]; then
        mkdir -p "$RUNNER_RUN_ROOT" 2>/dev/null || true
        printf '{"cleanQuit":%s}\n' "$([[ "$CLEAN_QUIT" == "1" ]] && echo true || echo false)" \
            > "$RUNNER_RUN_ROOT/clean-quit.json" 2>/dev/null || true
    fi
    return "$rc"
}
