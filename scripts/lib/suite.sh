# scripts/lib/suite.sh — suite discovery (spec 010, T061b).

if [ -n "${_LR_SUITE_LOADED:-}" ]; then
    return 0 2>/dev/null || exit 0
fi
_LR_SUITE_LOADED=1

# shellcheck source=common.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

# suite_discover <dir>  -> prints scenario files, one per line, sorted (LC_ALL=C).
suite_discover() {
    if [[ $# -lt 1 ]]; then
        log_error "suite_discover: usage: suite_discover <dir>"
        return "$EXIT_USAGE_ERROR"
    fi
    local dir="$1"
    if [[ ! -d "$dir" ]]; then
        log_error "suite_discover: not a directory: $dir"
        return "$EXIT_USAGE_ERROR"
    fi
    local scen json
    scen="$(find "$dir" -maxdepth 1 -type f -name '*.scenario.json' 2>/dev/null | LC_ALL=C sort)"
    if [[ -n "$scen" ]]; then
        printf '%s\n' "$scen"
        return 0
    fi
    json="$(find "$dir" -maxdepth 1 -type f -name '*.json' 2>/dev/null | LC_ALL=C sort)"
    if [[ -n "$json" ]]; then
        printf '%s\n' "$json"
        return 0
    fi
    log_error "suite_discover: no test files found in $dir (expected *.scenario.json or *.json)"
    return "$EXIT_NO_TESTS"
}

# suite_aggregate <code...>  -> prints aggregate exit code per precedence:
# errored/partial(19) > fail(18) > policy(21) > no-tests(20) > pass(0)
suite_aggregate() {
    local has19=0 has18=0 has21=0 has20=0 has0=0 has_other=0
    local c
    for c in "$@"; do
        case "$c" in
            19) has19=1 ;;
            18) has18=1 ;;
            21) has21=1 ;;
            20) has20=1 ;;
            0) has0=1 ;;
            *) has_other=1 ;;
        esac
    done
    if (( has19 )); then echo 19; return 0; fi
    if (( has18 )); then echo 18; return 0; fi
    if (( has21 )); then echo 21; return 0; fi
    if (( has20 && !has0 )); then echo 20; return 0; fi
    echo 0
    return 0
}

# suite_rerun_cap -> prints RunnerMaxSuiteReruns
suite_rerun_cap() {
    echo "${RunnerMaxSuiteReruns:-4}"
}

export -f suite_discover suite_aggregate suite_rerun_cap
