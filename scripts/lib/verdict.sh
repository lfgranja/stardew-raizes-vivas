#!/usr/bin/env bash
if [ -n "${_LR_VERDICT_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_VERDICT_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

# verdict_from_exit: map exit code to verdict class (best effort)
# partial is set by signal layer; not derived here
verdict_from_exit() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    case "$1" in
        0) echo "pass" ;;
        18) echo "fail" ;;
        19) echo "errored" ;;
        20) echo "skipped" ;;
        4) echo "errored" ;; # timeout -> errored class in this mapping context; policy notes inherited
        21) echo "policy" ;;
        22) echo "reserved" ;;
        *) echo "errored" ;;
    esac
    return 0
}

# verdict_exit_for: map verdict to exit code
# Includes code 21 POLICY and reserved code 22 is never emitted by normal mapping
verdict_exit_for() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    case "$1" in
        pass) echo 0 ;;
        fail) echo 18 ;;
        errored) echo 19 ;;
        partial) echo 19 ;;
        skipped) echo 20 ;;
        policy) echo 21 ;;
        flaky) echo 0 ;; # derived flaky on pass exits 0
        reserved) echo 22 ;; # reserved, never emitted by runner paths
        *) echo 19 ;;
    esac
    return 0
}

verdict_expected_match() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local actual="$1" expected="$2"
    # missing expected for positive => pass
    if [[ -z "$expected" ]]; then expected="pass"; fi
    if [[ "$actual" == "$expected" ]]; then return 0; else return 1; fi
}

# verdict_evaluate: core evaluation
# signals_complete: 1 if all three mandatory signal artifacts present/valid
# signals_agree: 1 if signals agree/divergent resolved
# thresholds_ok: 1 if all thresholds pass
# expected: expectedVerdict (may be empty)
# farmhand_breach: 1 if host-guard breach
verdict_evaluate() {
    [[ $# -eq 5 ]] || return "$EXIT_USAGE_ERROR"
    local signals_complete="$1" signals_agree="$2" thresholds_ok="$3" expected="$4" farmhand_breach="$5"
    # missing signals => partial
    if [[ "$signals_complete" != "1" ]]; then echo "partial"; return 0; fi
    if [[ "$farmhand_breach" == "1" ]]; then echo "fail"; return 0; fi
    if [[ "$signals_agree" != "1" ]]; then echo "fail"; return 0; fi
    if [[ "$thresholds_ok" != "1" ]]; then echo "fail"; return 0; fi
    echo "pass"
    return 0
}

# verdict_classify_missing_artifacts
# Args: signals_complete(0/1), manifest_ok(0/1), claims_ok(0/1), anchor_ok(0/1), attempts_ok(0/1)
# Returns verdict on stdout; uses exit codes semantics
verdict_classify_missing_artifacts() {
    [[ $# -eq 5 ]] || return "$EXIT_USAGE_ERROR"
    local signals_complete="$1" manifest_ok="$2" claims_ok="$3" anchor_ok="$4" attempts_ok="$5"
    # missing bundle/contract artifacts => errored (higher priority than signal completeness in bundle trust)
    if [[ "$manifest_ok" != "1" || "$claims_ok" != "1" || "$anchor_ok" != "1" || "$attempts_ok" != "1" ]]; then
        echo "errored"
        return 0
    fi
    if [[ "$signals_complete" != "1" ]]; then
        echo "partial"
        return 0
    fi
    echo "ok"
    return 0
}

# verdict_apply_expected: apply expectedVerdict
# Args: actual_verdict, expected_verdict, is_integrity_or_claim(0/1)
# Prints final verdict and returns exit code on stdout? returns via echo final verdict; caller uses verdict_exit_for
# Mismatch => errored (exit 19). Matched fail preserved.
verdict_apply_expected() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local actual="$1" expected="$2" integrity_claim="$3"
    if [[ -z "$expected" ]]; then
        expected="pass"
    fi
    if [[ "$actual" == "$expected" ]]; then
        echo "$actual"
        return 0
    fi
    # mismatch
    if [[ "$integrity_claim" == "1" ]]; then
        # integrity/security or claim overreach - still errored, not demotable
        echo "errored"
        return 0
    fi
    # mismatch => scenario-contract failure => errored
    echo "errored"
    return 0
}

# verdict_derive_flaky: derive flaky metadata
# Args: first_attempt_failed_retryable(0/1), later_pass_same_context(0/1)
# Returns 1 if flaky derived, 0 if not
verdict_derive_flaky() {
    [[ $# -eq 2 ]] || return 0
    local first_failed_retryable="$1" later_pass="$2"
    if [[ "$first_failed_retryable" == "1" && "$later_pass" == "1" ]]; then
        return 1
    fi
    return 0
}

# verdict_aggregate: suite aggregation 19 > 18 > 21 > 20 > 0
verdict_aggregate() {
    # prints aggregate exit code
    local has19=0 has18=0 has21=0 has20=0 has0=0
    local c
    for c in "$@"; do
        case "$c" in
            19) has19=1 ;;
            18) has18=1 ;;
            21) has21=1 ;;
            20) has20=1 ;;
            0) has0=1 ;;
        esac
    done
    if (( has19 )); then echo 19; return 0; fi
    if (( has18 )); then echo 18; return 0; fi
    if (( has21 )); then echo 21; return 0; fi
    if (( has20 == 1 && has0 == 0 )); then echo 20; return 0; fi
    echo 0
    return 0
}

verdict_apply_ignore_exit_code() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local code="$1" list="$2" integrity_flag="$3"
    # validate list entries
    local entry
    IFS=',' read -ra entries <<< "$list"
    for entry in "${entries[@]}"; do
        entry="${entry//[[:space:]]/}"
        case "$entry" in
            4|6|7|9|10|13|14|18|19|20|21) ;;
            0|22) 
                return "$EXIT_USAGE_ERROR"
                ;;
            *) 
                # check if in allowed set
                if [[ ! " 4 6 7 9 10 13 14 18 19 20 21 " =~ " $entry " ]]; then
                    return "$EXIT_USAGE_ERROR"
                fi
                ;;
        esac
    done
    # integrity or policy 21 refusal
    if [[ "$integrity_flag" == "1" ]]; then
        printf '%s\n' "$code"
        return 0
    fi
    if [[ "$code" == "21" ]]; then
        # policy overreach never demotable
        printf '%s\n' "$code"
        return 0
    fi
    # check if code in list
    if [[ ",$list," == *",$code,"* ]]; then
        echo 0
        return 0
    fi
    printf '%s\n' "$code"
    return 0
}

verdict_write_attempt() {
    [[ $# -eq 6 ]] || return "$EXIT_USAGE_ERROR"
    local attempt_dir="$1" attempt="$2" verdict="$3" failureClass="$4" signals="$5" exitCode="$6"
    mkdir -p "$attempt_dir"
    python3 - "$attempt_dir/attempt.json" "$attempt" "$verdict" "$failureClass" "$signals" "$exitCode" <<'PY'
import json,sys
path,attempt,verdict,fc,signals,ec=sys.argv[1:]
d={"attempt":int(attempt),"verdict":verdict,"failureClass":fc,"signals":signals,"exitCode":int(ec)}
json.dump(d,open(path,"w"),indent=2)
PY
    return 0
}