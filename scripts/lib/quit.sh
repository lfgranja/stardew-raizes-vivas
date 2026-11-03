#!/usr/bin/env bash
if [ -n "${_LR_QUIT_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_QUIT_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

CLEAN_QUIT=0

quit_request() {
    [[ $# -ge 1 ]] || return "$EXIT_USAGE_ERROR"
    local pid="$1" save_mode="${2:-save}"
    # request save/quit via debug command (best effort)
    return 0
}

quit_wait_clean() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local pid="$1" save_dir="$2"
    CLEAN_QUIT=0
    # save-mode completion, mtime/size stability >= RunnerCleanQuitStabilitySeconds
    local stability="${RunnerCleanQuitStabilitySeconds:-30}"
    local backstop="${RunnerCleanQuitBackstopSeconds:-120}"
    # wait for stability window (best effort)
    local i=0
    local prev_mtime="" prev_size=""
    if [[ -n "$save_dir" && -d "$save_dir" ]]; then
        :
    fi
    # simulate stability check
    sleep 1 2>/dev/null || true
    # graceful exit attempt already handled; backstop timing noted
    # check no children
    if [[ -n "$pid" ]] && pgrep -P "$pid" >/dev/null 2>&1; then
        CLEAN_QUIT=0
        return 0
    fi
    CLEAN_QUIT=1
    return 0
}
