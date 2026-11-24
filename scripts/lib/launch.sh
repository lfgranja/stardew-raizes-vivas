#!/usr/bin/env bash
if [ -n "${_LR_LAUNCH_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_LAUNCH_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

LAUNCH_PID=""; _LAUNCH_L1=""; _LAUNCH_INODE=""; _LAUNCH_LOG_INODE=""

launch_start() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local launcher="$1" log_path="$2" timeout="${3:-30}"
    if [[ -z "${DISPLAY:-}" ]]; then return 10; fi
    if [[ ! -x "$launcher" && ! -f "$launcher" ]]; then return 9; fi
    # remove stale log
    rm -f "$log_path" 2>/dev/null || true
    _LAUNCH_L1="$(date -u +%s)"
    # record inode (none yet)
    _LAUNCH_INODE=""
    _LAUNCH_LOG_INODE=""
    # Set SMAPI/headless environment variables
    export SMAPI_NO_TERMINAL="${SMAPI_NO_TERMINAL:-1}"
    export LIBGL_ALWAYS_SOFTWARE="${LIBGL_ALWAYS_SOFTWARE:-1}"
    export GALLIUM_DRIVER="${GALLIUM_DRIVER:-llvmpipe}"
    export MESA_GL_VERSION_OVERRIDE="${MESA_GL_VERSION_OVERRIDE:-3.3COMPAT}"
    export SDL_VIDEODRIVER="${SDL_VIDEODRIVER:-x11}"
    export SDL_AUDIODRIVER="${SDL_AUDIODRIVER:-dummy}"
    export PULSE_SERVER="${PULSE_SERVER:-/dev/null}"
    export NO_AT_BRIDGE="${NO_AT_BRIDGE:-1}"
    # start launcher in own process group
    if command -v setsid >/dev/null 2>&1; then
        setsid env \
            SMAPI_NO_TERMINAL="$SMAPI_NO_TERMINAL" \
            LIBGL_ALWAYS_SOFTWARE="$LIBGL_ALWAYS_SOFTWARE" \
            GALLIUM_DRIVER="$GALLIUM_DRIVER" \
            MESA_GL_VERSION_OVERRIDE="$MESA_GL_VERSION_OVERRIDE" \
            SDL_VIDEODRIVER="$SDL_VIDEODRIVER" \
            SDL_AUDIODRIVER="$SDL_AUDIODRIVER" \
            PULSE_SERVER="$PULSE_SERVER" \
            NO_AT_BRIDGE="$NO_AT_BRIDGE" \
            DISPLAY="${DISPLAY:-}" \
            bash -c "exec '$launcher'" </dev/null >/dev/null 2>&1 &
    else
        env \
            SMAPI_NO_TERMINAL="$SMAPI_NO_TERMINAL" \
            LIBGL_ALWAYS_SOFTWARE="$LIBGL_ALWAYS_SOFTWARE" \
            GALLIUM_DRIVER="$GALLIUM_DRIVER" \
            MESA_GL_VERSION_OVERRIDE="$MESA_GL_VERSION_OVERRIDE" \
            SDL_VIDEODRIVER="$SDL_VIDEODRIVER" \
            SDL_AUDIODRIVER="$SDL_AUDIODRIVER" \
            PULSE_SERVER="$PULSE_SERVER" \
            NO_AT_BRIDGE="$NO_AT_BRIDGE" \
            DISPLAY="${DISPLAY:-}" \
            bash -c "exec '$launcher'" </dev/null >/dev/null 2>&1 &
    fi
    LAUNCH_PID=$!
    disown "$LAUNCH_PID" 2>/dev/null || true
    return 0
}

launch_wait_ready() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local log_path="$1" pid="$2" timeout="$3"
    local i=0
    local phase1_timeout=$((timeout * 2))
    # phase 1: log-sentinel (Log started at) + inode freshness
    while (( i < phase1_timeout )); do
        if [[ -f "$log_path" ]]; then
            local cur_inode
            cur_inode="$(stat -c %i "$log_path" 2>/dev/null || stat -f %i "$log_path" 2>/dev/null || echo "")"
            if [[ -n "$cur_inode" && "$cur_inode" != "${_LAUNCH_LOG_INODE:-}" ]]; then
                _LAUNCH_LOG_INODE="$cur_inode"
            fi
            if grep -q "Log started at" "$log_path" 2>/dev/null; then
                break
            fi
        fi
        # process exited and no log => 15
        if ! kill -0 "$pid" 2>/dev/null; then
            if [[ ! -f "$log_path" ]]; then return 15; fi
        fi
        sleep 0.5
        i=$((i+1))
    done
    if [[ ! -f "$log_path" ]]; then
        # process exited?
        if ! kill -0 "$pid" 2>/dev/null; then return 15; fi
        return 4
    fi
    if ! grep -q "Log started at" "$log_path" 2>/dev/null; then
        return 4
    fi
    # phase 2: Loaded N mods:, skipped-mod check, GameLaunched marker
    # Check for skipped mods (LivingRoots specifically)
    if grep -qi "skipped" "$log_path" 2>/dev/null && grep -qi "LivingRoots" "$log_path" 2>/dev/null; then
        # only fail if a skipped line mentions LivingRoots
        if grep -i "LivingRoots" "$log_path" | grep -qi "skip"; then
            return 5
        fi
    fi
    if grep -q "Skipped mods" "$log_path" 2>/dev/null && grep -q "LivingRoots" "$log_path" 2>/dev/null; then
        if grep -q "LivingRoots.*needs SMAPI\|LivingRoots.*Skipped" "$log_path" 2>/dev/null; then
            return 5
        fi
    fi
    # phase 2 bound: poll for the mod-resolution header (C-LCH-01). The header
    # appears ~20 s after the sentinel on a real install, so a single grep here
    # would report 17 on every healthy launch.
    local j=0
    local phase2_timeout=$((timeout * 2))
    while (( j < phase2_timeout )); do
        if grep -Eq "Loaded [0-9]+ mods:" "$log_path" 2>/dev/null; then
            break
        fi
        sleep 0.5
        j=$((j+1))
    done
    if ! grep -Eq "Loaded [0-9]+ mods:" "$log_path" 2>/dev/null; then
        return 17
    fi
    if grep -q "GameLaunched" "$log_path" 2>/dev/null; then
        : # GameLaunched marker present
    fi
    return 0
}

launch_assert_no_stdin_events() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local log="$1"
    [[ -f "$log" ]] || return 19
    if grep -qi "stdin" "$log"; then
        return 19
    fi
    return 0
}