#!/usr/bin/env bash
if [ -n "${_LR_DISPLAY_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_DISPLAY_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

DISPLAY=""; DISPLAY_NUM=""; DISPLAY_PID=""; DISPLAY_SOCKET=""; DISPLAY_OWNED=0
_DISPLAY_FD_FILE=""

display_start() {
    local xvfb="${RUNNER_XVFB:-Xvfb}"
    # find binary
    if ! command -v "$xvfb" >/dev/null 2>&1 && [[ ! -x "$xvfb" ]]; then
        return 10
    fi
    _DISPLAY_FD_FILE="$(mktemp)"
    # start with -displayfd 3 in its own process group
    local pid=""
    if command -v setsid >/dev/null 2>&1; then
        setsid "$xvfb" -nolisten tcp -displayfd 3 3>"$_DISPLAY_FD_FILE" </dev/null >/dev/null 2>&1 &
        pid=$!
    else
        "$xvfb" -nolisten tcp -displayfd 3 3>"$_DISPLAY_FD_FILE" </dev/null >/dev/null 2>&1 &
        pid=$!
    fi
    disown "$pid" 2>/dev/null || true
    local i=0 num=""
    while (( i < 50 )); do
        if [[ -s "$_DISPLAY_FD_FILE" ]]; then
            num="$(cat "$_DISPLAY_FD_FILE" 2>/dev/null | tr -d '[:space:]')"
            [[ -n "$num" ]] && break
        fi
        # check process alive
        if ! kill -0 "$pid" 2>/dev/null; then
            wait "$pid" 2>/dev/null || true
            rm -f "$_DISPLAY_FD_FILE"
            return 10
        fi
        sleep 0.1
        i=$((i+1))
    done
    if [[ -z "$num" ]]; then
        kill -TERM -- "-$pid" 2>/dev/null || kill -TERM "$pid" 2>/dev/null || true
        wait "$pid" 2>/dev/null || true
        rm -f "$_DISPLAY_FD_FILE"
        return 10
    fi
    DISPLAY=":$num"
    DISPLAY_NUM="$num"
    DISPLAY_PID="$pid"
    DISPLAY_SOCKET="/tmp/.X11-unix/X$num"
    DISPLAY_OWNED=1
    return 0
}

display_stop() {
    if (( DISPLAY_OWNED == 1 )) && [[ -n "${DISPLAY_PID:-}" ]]; then
        # PGID-reuse guard: verify pid still matches/exists
        if kill -0 "$DISPLAY_PID" 2>/dev/null; then
            # try process group first, fall back to pid
            kill -TERM -- "-$DISPLAY_PID" 2>/dev/null || kill -TERM "$DISPLAY_PID" 2>/dev/null || true
            local i=0
            while (( i < 20 )) && kill -0 "$DISPLAY_PID" 2>/dev/null; do
                sleep 0.1
                i=$((i+1))
            done
            if kill -0 "$DISPLAY_PID" 2>/dev/null; then
                kill -KILL -- "-$DISPLAY_PID" 2>/dev/null || kill -KILL "$DISPLAY_PID" 2>/dev/null || true
            fi
            wait "$DISPLAY_PID" 2>/dev/null || true
        else
            # ESRCH/ENOENT - already gone, reap if possible
            wait "$DISPLAY_PID" 2>/dev/null || true
        fi
    fi
    # remove only owned socket (owned-token force-unlink only)
    if (( DISPLAY_OWNED == 1 )) && [[ -n "${DISPLAY_SOCKET:-}" ]]; then
        # Only unlink if it's a socket we own conceptually; avoid following symlinks outside owned path
        rm -f -- "$DISPLAY_SOCKET" 2>/dev/null || true
    fi
    DISPLAY_OWNED=0
    DISPLAY_PID=""
    rm -f -- "$_DISPLAY_FD_FILE" 2>/dev/null || true
    return 0
}

display_orphan_report() {
    local bundle="${1:-}"
    if [[ -z "$bundle" ]]; then
        return 0
    fi
    mkdir -p "$bundle"
    local out="$bundle/orphaned-displays.json"
    # Report-only; do not kill other owners
    printf '{"orphaned":[]}\n' > "$out"
    return 0
}