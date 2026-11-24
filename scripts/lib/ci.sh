#!/usr/bin/env bash
#
# scripts/lib/ci.sh — CI mode helpers for verify-scenario (US3).
#

if [[ -n "${_LR_CI_SH_LOADED:-}" ]]; then
    return 0 2>/dev/null || exit 0
fi
_LR_CI_SH_LOADED=1

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

# CI_MODE defaults off; set by --ci flag or env.
CI_MODE="${CI_MODE:-0}"

ci_is_ci() {
    if [[ "${CI_MODE}" == "1" ]]; then return 0; fi
    if [[ -n "${CI:-}" || -n "${GITHUB_ACTIONS:-}" || -n "${GITLAB_CI:-}" ]]; then return 0; fi
    return 1
}

ci_enable() {
    CI_MODE=1
    export CI_MODE
}

ci_noninteractive() {
    # Disable prompts; batch mode.
    export DEBIAN_FRONTEND=noninteractive
    export RUNNER_NONINTERACTIVE=1
}

ci_detect() {
    if ci_is_ci; then
        ci_enable
        ci_noninteractive
        return 0
    fi
    return 1
}
