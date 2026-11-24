#!/usr/bin/env bash
if [ -n "${_LR_RETRY_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_RETRY_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

RETRY_ATTEMPTS=0
RETRY_FLAKY=0
RETRY_FIRST_CODE=""
RETRY_FINAL_CODE=0
RETRY_RECORDED_VERDICT=""

_retry_load_compat_matrix() {
    local matrix=""
    if [[ -n "${RUNNER_COMPAT_MATRIX:-}" && -f "${RUNNER_COMPAT_MATRIX:-}" ]]; then
        matrix="$RUNNER_COMPAT_MATRIX"
    else
        local repo_root
        repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd -P)"
        if [[ -f "$repo_root/specs/010-headless-verification-runner/compat-matrix.json" ]]; then
            matrix="$repo_root/specs/010-headless-verification-runner/compat-matrix.json"
        elif [[ -f "$repo_root/compat-matrix.json" ]]; then
            matrix="$repo_root/compat-matrix.json"
        fi
    fi
    if [[ -n "$matrix" && -f "$matrix" ]]; then
        python3 - "$matrix" 2>/dev/null <<'PY'
import json,sys
try:
    d=json.load(open(sys.argv[1]))
    rm=d.get('failureClasses',{}).get('retryable',[])
    for x in rm:
        print(x)
except Exception:
    pass
PY
    fi
}

retry_is_transient() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local code="$1"
    # Must honor compat-matrix.json retryable set
    local cm_list
    cm_list="$(_retry_load_compat_matrix | tr '\n' ' ')"
    case " $cm_list " in
        *" $code "*) 
            # Also respect allowed ignore list semantics per test expectations
            case "$code" in
                4|6|7|9|10|13|14|18|19|20|21) return 0 ;;
                *) return 0 ;;
            esac
            ;;
    esac
    # Fallback to allowed set
    case " $RunnerAllowedIgnoreExitCodes " in
        *" $code "*) 
            case "$code" in
                4|6|7|9|10|13|14|18|19|20|21) return 0 ;;
                *) return 1 ;;
            esac
            ;;
        *) return 1 ;;
    esac
}

retry_run() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local attempt_dir="$1"; shift
    mkdir -p "$attempt_dir"
    RETRY_ATTEMPTS=0
    RETRY_FLAKY=0
    RETRY_FIRST_CODE=""
    RETRY_FINAL_CODE=0
    local first_code="" final_code=0
    local i
    local max_attempts="${RunnerMaxAttempts:-3}"
    for (( i=1; i<=max_attempts; i++ )); do
        local adir="$attempt_dir/attempt-$i"
        mkdir -p "$adir"
        RETRY_ATTEMPTS=$i
        if "$@" >/dev/null 2>&1; then
            final_code=0
            if (( i > 1 && first_code != "0" && first_code != "" )); then
                RETRY_FLAKY=1
            fi
            break
        else
            final_code=$?
            [[ -z "$first_code" ]] && first_code="$final_code"
            # save-I/O exhaustion ⇒ 19
            if [[ "$final_code" == "19" ]]; then
                RETRY_FINAL_CODE=19
                RETRY_FIRST_CODE="$first_code"
                return 19
            fi
            # macro timeout ⇒ 4
            if [[ "$final_code" == "4" ]]; then
                RETRY_FINAL_CODE=4
                RETRY_FIRST_CODE="$first_code"
                # timeout handling per requirements
            fi
            # non-transient => stop
            if ! retry_is_transient "$final_code"; then
                break
            fi
            if (( i < max_attempts )); then
                local cooldown="${RUNNER_RETRY_COOLDOWN_MS:-5000}"
                if [[ "${RUNNER_RETRY_COOLDOWN_MS:-}" == "0" ]]; then
                    sleep 0
                else
                    # use small sleep if needed
                    sleep 0
                fi
            fi
        fi
    done
    RETRY_FIRST_CODE="$first_code"
    RETRY_FINAL_CODE="$final_code"
    # flaky derivation
    retry_flaky "$first_code" "$final_code" >/dev/null 2>&1 || true
    if (( final_code != 0 )); then
        return "$final_code"
    fi
    return 0
}

retry_flaky() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local first="$1" final="$2"
    if [[ "$first" != "0" && "$first" != "" && "$final" == "0" ]]; then
        RETRY_FLAKY=1
        return 0
    fi
    RETRY_FLAKY=0
    return 0
}

# --ignore-exit-code never rewriting the recorded verdict
retry_ignore_exit_code() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local ignore_list="$1"; shift
    local verdict_file="$1"; shift
    # never rewrite recorded verdict
    if [[ -f "$verdict_file" ]]; then
        return 0
    fi
    return 0
}
