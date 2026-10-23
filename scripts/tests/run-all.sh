#!/usr/bin/env bash
#
# Test runner for the 008 "smoke validation" bash suite.
#
#   ./scripts/tests/run-all.sh                                   # run every test-*.sh
#   ./scripts/tests/run-all.sh test-cli.sh                       # run one file by bare name
#   ./scripts/tests/run-all.sh scripts/tests/test-cli.sh         # ...or by path
#
# Each test file runs in its own subshell with:
#
#   - TMPDIR pointing at a fresh directory INSIDE the runner's own scratch
#     directory, so one file's temporary files can never leak into the next,
#     and the whole tree — the runner's scratch directory, every file's
#     temporary directory, and every sandbox the harness creates inside it —
#     is reclaimed by a single removal however the run ends;
#   - SMOKE_SKIP_INTEGRATION unset, so the launch-free suite runs on any host
#     — unless the caller already set it, in which case the caller's value is
#     passed through unchanged;
#   - the repository root as the working directory, so the relative paths the
#     tests use resolve.
#
# The stdout and stderr of every file are captured and printed indented under
# that file's own heading, so one file's output is never mistaken for
# another's, and each file is followed by a one-line verdict:
#
#   PASS <file>   the file ran at least one case and every case passed
#   SKIP <file>   every case in the file was skipped — nothing was tested
#   FAIL <file>   the file failed
#
# A file whose every case was skipped is never reported as PASS. The harness
# counts the skipped cases in a counter of their own and reports that count
# on a line of its own (`# skipped: N`); the runner compares it with the
# number of cases the file ran, and a file that ran nothing at all is treated
# the same way.
#
# The runner never stops at the first failure: every file runs and the full
# set of failures is reported in the summary.
#
# Exit status: 0 when no test file failed — a file whose cases were all
# skipped is not a failure; 1 when a file failed, when no test file was
# found, when an argument matched no test file, or when an unknown option was
# given. An interrupted run exits 130 (SIGINT) or 143 (SIGTERM); the summary
# is printed before the runner leaves, so an interrupted run never leaves the
# reader without a verdict.

set -Eeuo pipefail

# Job control is enabled so that every test file runs in a process group of
# its own. An interrupted run can then signal that whole group, and no test
# file's child — a sleeping stub, a helper, a stub host tool — is left behind
# as an orphan. The runner only ever signals a group whose leader is the file
# it started, never its own group.
set -m

readonly RUNNER_NAME="run-all.sh"

# The runner lives in <repo>/scripts/tests/, so the repository root is two
# levels up. It is resolved structurally rather than with `git rev-parse`, so
# the runner also works in a copied tree.
# The declaration and the assignment are separate: `readonly X="$(cmd)"` masks
# the exit status of `cmd` (SC2155), so a failed `cd` would be silently
# swallowed and the runner would carry on with an empty path.
RUNNER_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
readonly RUNNER_DIR
readonly TESTS_DIR="${RUNNER_DIR}"
REPO_ROOT="$(cd -- "${RUNNER_DIR}/../.." && pwd -P)"
readonly REPO_ROOT

# The value run_test_file returns for a file whose every case was skipped. It
# is the runner's own marker; 77 is the conventional "skipped" status.
readonly SKIP_VERDICT=77

# The runner's own scratch directory. Every test file's temporary directory
# lives inside it, so one removal reclaims the whole tree. Removed on exit.
RUNNER_TMP=""

# The running totals. They live at the top level rather than in main(), so
# that the signal handlers can report them even when the run is interrupted
# in the middle of a file.
RUNNER_TOTAL=0
RUNNER_PASSED=0
RUNNER_FAILED=0
RUNNER_SKIPPED=0

# Set to 1 once the totals have been printed, so the summary is printed once.
RUNNER_SUMMARY_PRINTED=0

# The label, the output file and the process of the test file being run, so
# that an interrupt can stop it and report what the run reached.
RUNNER_CURRENT_LABEL=""
RUNNER_CURRENT_OUTPUT=""
RUNNER_CURRENT_PID=""
RUNNER_CURRENT_ACTIVE=0

cleanup() {
    if [[ -n "${RUNNER_TMP}" && -d "${RUNNER_TMP}" ]]; then
        rm -rf -- "${RUNNER_TMP}" || true
    fi
    RUNNER_TMP=""
    return 0
}

# current_process_group <pid> — the process group the process leads, when it
# leads one of its own. Fails when the group cannot be identified, so the
# caller falls back to signalling the single process. A group the runner did
# not create is never reported: signalling the runner's own group would take
# the runner down with it.
current_process_group() {
    local pid="${1-}"
    [[ -n "$pid" ]] || return 1
    local pgid
    pgid="$(ps -o pgid= -p "$pid" 2>/dev/null | tr -d '[:space:]' || true)"
    [[ -n "$pgid" ]] || return 1
    if [[ "$pgid" == "$pid" ]]; then
        printf '%s' "$pgid"
        return 0
    fi
    return 1
}

# stop_current_file — stop the test file that is running and wait for it, so
# an interrupted run leaves neither a stray process nor a half-written
# temporary tree behind.
stop_current_file() {
    local pid="${RUNNER_CURRENT_PID}"
    [[ -n "$pid" ]] || return 0
    RUNNER_CURRENT_PID=""

    # The group is signalled when the file leads one of its own, so the
    # file's own children go with it.
    local pgid=""
    pgid="$(current_process_group "$pid" || true)"

    if [[ -n "$pgid" ]]; then
        kill -TERM -- "-${pgid}" 2>/dev/null || true
    else
        kill -TERM "$pid" 2>/dev/null || true
    fi

    local waited=0
    while (( waited < 20 )) && kill -0 "$pid" 2>/dev/null; do
        sleep 0.1 2>/dev/null || sleep 1
        waited=$(( waited + 1 ))
    done
    if kill -0 "$pid" 2>/dev/null; then
        if [[ -n "$pgid" ]]; then
            kill -KILL -- "-${pgid}" 2>/dev/null || true
        else
            kill -KILL "$pid" 2>/dev/null || true
        fi
    fi
    wait "$pid" 2>/dev/null || true
    return 0
}

# print_summary — the totals. The line keeps the shape it has always had;
# the skipped count is added to it only when a file was skipped, so a run
# without a skipped file prints exactly what it always printed.
print_summary() {
    if (( RUNNER_SUMMARY_PRINTED == 1 )); then
        return 0
    fi
    RUNNER_SUMMARY_PRINTED=1
    if (( RUNNER_SKIPPED > 0 )); then
        printf '%d files, %d passed, %d failed, %d skipped\n' \
            "${RUNNER_TOTAL}" "${RUNNER_PASSED}" "${RUNNER_FAILED}" "${RUNNER_SKIPPED}"
    else
        printf '%d files, %d passed, %d failed\n' \
            "${RUNNER_TOTAL}" "${RUNNER_PASSED}" "${RUNNER_FAILED}"
    fi
    return 0
}

# on_signal <name> <code> — the SIGINT and SIGTERM handler. The file that is
# running is stopped, what the run reached is reported, the summary is
# printed, and the runner leaves with the status the signal implies. The
# summary is never left unprinted: an interrupted run still says what it did
# and what it did not finish.
on_signal() {
    local signal_name="${1-}"
    local code="${2-1}"

    # A second signal must not re-enter the handler.
    trap - INT TERM

    stop_current_file

    if (( RUNNER_CURRENT_ACTIVE == 1 )); then
        printf '=== %s ===\n' "${RUNNER_CURRENT_LABEL}"
        printf '    [interrupted by SIG%s]\n' "${signal_name}"
        local captured=""
        if [[ -n "${RUNNER_CURRENT_OUTPUT}" && -f "${RUNNER_CURRENT_OUTPUT}" ]]; then
            captured="$(cat -- "${RUNNER_CURRENT_OUTPUT}" 2>/dev/null || true)"
        fi
        if [[ -n "${captured}" ]]; then
            printf '%s\n' "${captured}" | sed 's/^/    /'
        else
            printf '    (no output)\n'
        fi
        printf 'FAIL %s\n' "${RUNNER_CURRENT_LABEL}"
        printf '\n'
        # The interrupted file did not complete, so it counts as failed.
        RUNNER_FAILED=$(( RUNNER_FAILED + 1 ))
    fi

    printf '%s: interrupted by SIG%s\n' "${RUNNER_NAME}" "${signal_name}" >&2
    print_summary
    exit "${code}"
}

trap cleanup EXIT
trap 'on_signal INT 130' INT
trap 'on_signal TERM 143' TERM

usage() {
    cat <<'EOF'
Usage: scripts/tests/run-all.sh [TEST_FILE...]

Runs the smoke-validation bash suite and reports a per-file verdict plus an
overall total.

Arguments:
  TEST_FILE            A test file to run. Either a bare name (test-cli.sh) or
                       a path (scripts/tests/test-cli.sh, or an absolute path).
                       When no file is given, every scripts/tests/test-*.sh is
                       run, in sorted order.

Environment:
  SMOKE_SKIP_INTEGRATION
                       Unset for the test files by default, so the launch-free
                       suite runs on any host. When the caller has set it, the
                       caller's value is passed through unchanged.

Verdicts:
  PASS <file>          the file ran at least one case and every case passed
  SKIP <file>          every case in the file was skipped — nothing was tested
  FAIL <file>          the file failed

Exit status:
  0   no test file failed (a file whose cases were all skipped is not a
      failure)
  1   a test file failed, no test file was found, an argument matched no
      test file, or an unknown option was given
  130 the run was interrupted with SIGINT
  143 the run was interrupted with SIGTERM
EOF
}

# Absolute path of a file, given any path that reaches it.
abspath() {
    local path="$1"
    local dir base
    dir="$(dirname -- "${path}")"
    base="$(basename -- "${path}")"
    printf '%s/%s\n' "$(cd -- "${dir}" && pwd -P)" "${base}"
}

# The absolute paths of every test-*.sh in the tests directory, sorted.
# Prints nothing (and succeeds) when the directory is missing or empty.
discover() {
    find "${TESTS_DIR}" -maxdepth 1 -type f -name 'test-*.sh' -print 2>/dev/null \
        | LC_ALL=C sort || true
}

# Resolve a requested test file. Accepts a path as given, a bare name inside
# the tests directory, or a path relative to the repository root. Fails when
# the argument matches no file.
resolve_test_file() {
    local arg="$1"
    local candidate

    if [[ -f "${arg}" ]]; then
        abspath "${arg}"
        return 0
    fi

    candidate="${TESTS_DIR}/${arg}"
    if [[ -f "${candidate}" ]]; then
        abspath "${candidate}"
        return 0
    fi

    candidate="${REPO_ROOT}/${arg}"
    if [[ -f "${candidate}" ]]; then
        abspath "${candidate}"
        return 0
    fi

    return 1
}

# The number of cases the last read summary reported, and the number of them
# that were skipped. -1 means "the file printed no such summary".
SUMMARY_TESTS=-1
SUMMARY_SKIPPED=-1

# read_summary_counts <text> — read the harness's own summary out of the text
# a test file produced. The harness prints
#
#     # tests: N, passed: N, failed: 0
#     # skipped: N
#
# and the second line only when something was skipped. The last summary in
# the text wins, so a file that prints more than one cannot be misread.
read_summary_counts() {
    local text="${1-}"
    local re_tests='^# tests: ([0-9]+), passed: ([0-9]+), failed: ([0-9]+)$'
    local re_skipped='^# skipped: ([0-9]+)$'
    local line

    SUMMARY_TESTS=-1
    SUMMARY_SKIPPED=-1

    while IFS= read -r line; do
        if [[ "$line" =~ $re_tests ]]; then
            SUMMARY_TESTS="${BASH_REMATCH[1]}"
        fi
    done <<< "${text}"

    while IFS= read -r line; do
        if [[ "$line" =~ $re_skipped ]]; then
            SUMMARY_SKIPPED="${BASH_REMATCH[1]}"
        fi
    done <<< "${text}"
    return 0
}

# all_cases_skipped — true when the file that produced the summary ran no
# case at all, or when every case it ran was skipped. Either way nothing was
# tested, so the file must not be reported as a pass.
all_cases_skipped() {
    if (( SUMMARY_TESTS < 0 )); then
        # The file printed no summary at all: nothing can be said about it.
        return 1
    fi
    if (( SUMMARY_TESTS == 0 )); then
        return 0
    fi
    if (( SUMMARY_SKIPPED == SUMMARY_TESTS )); then
        return 0
    fi
    return 1
}

# Run one test file and report it. Returns 0 when the file passed, the
# SKIP_VERDICT when every case in it was skipped, and 1 when it failed.
run_test_file() {
    local test_file="$1"
    local label="$2"

    local tmpdir=""
    local output_file=""
    local status=0
    local output=""
    local verdict=1

    # The file's temporary directory lives INSIDE the runner's own scratch
    # directory, so the runner's cleanup reclaims it together with everything
    # the harness creates inside it: an interrupted run cannot leak it.
    if [[ -n "${RUNNER_TMP}" && -d "${RUNNER_TMP}" ]]; then
        tmpdir="$(mktemp -d "${RUNNER_TMP}/file.XXXXXXXX" 2>/dev/null || true)"
    fi
    if [[ -z "${tmpdir}" || ! -d "${tmpdir}" ]]; then
        tmpdir="$(mktemp -d 2>/dev/null || mktemp -d -p /tmp smoke-tests.XXXXXX 2>/dev/null || true)"
    fi

    if [[ -n "${tmpdir}" && -d "${tmpdir}" ]]; then
        output_file="${tmpdir}/output.log"

        # The file runs as a background job so that a signal delivered to the
        # runner is handled at once: `wait` returns as soon as the signal
        # arrives, and the handler can stop the file instead of waiting for
        # it to finish on its own.
        (
            cd -- "${REPO_ROOT}" || exit 127
            TMPDIR="${tmpdir}" exec bash -- "${test_file}"
        ) > "${output_file}" 2>&1 &
        RUNNER_CURRENT_PID="$!"
        RUNNER_CURRENT_OUTPUT="${output_file}"
        RUNNER_CURRENT_ACTIVE=1

        local wait_status=0
        wait "${RUNNER_CURRENT_PID}" || wait_status=$?
        RUNNER_CURRENT_PID=""

        status="${wait_status}"
        output="$(cat -- "${output_file}" 2>/dev/null || true)"
    else
        status=1
        output="${RUNNER_NAME}: could not create a temporary directory for ${label}"
    fi

    printf '=== %s ===\n' "${label}"
    printf '    [exit status: %d]\n' "${status}"
    if [[ -n "${output}" ]]; then
        printf '%s\n' "${output}" | sed 's/^/    /'
    else
        printf '    (no output)\n'
    fi

    read_summary_counts "${output}"
    if (( status == 0 )); then
        if all_cases_skipped; then
            printf 'SKIP %s\n' "${label}"
            verdict="${SKIP_VERDICT}"
        else
            printf 'PASS %s\n' "${label}"
            verdict=0
        fi
    else
        printf 'FAIL %s\n' "${label}"
        verdict=1
    fi
    printf '\n'

    RUNNER_CURRENT_ACTIVE=0
    RUNNER_CURRENT_OUTPUT=""

    return "${verdict}"
}

main() {
    local runner_tmp
    runner_tmp="$(mktemp -d 2>/dev/null || mktemp -d -p /tmp smoke-tests.XXXXXX 2>/dev/null || true)"
    if [[ -z "${runner_tmp}" || ! -d "${runner_tmp}" ]]; then
        printf '%s: could not create a temporary directory\n' "${RUNNER_NAME}" >&2
        return 1
    fi
    RUNNER_TMP="${runner_tmp}"

    local requested_file="${RUNNER_TMP}/requested.list"
    : > "${requested_file}"
    local has_requested=0
    local line

    while (( $# > 0 )); do
        case "$1" in
            --)
                shift
                break
                ;;
            -*)
                printf '%s: unknown option: %s\n' "${RUNNER_NAME}" "$1" >&2
                usage >&2
                return 1
                ;;
            *)
                printf '%s\n' "$1" >> "${requested_file}"
                has_requested=1
                ;;
        esac
        shift
    done

    # Everything after `--` is a test file.
    while (( $# > 0 )); do
        printf '%s\n' "$1" >> "${requested_file}"
        has_requested=1
        shift
    done

    # The integration suite is opt-out: the flag is unset for the test files
    # unless the caller set it, in which case the caller's value is passed
    # through unchanged.
    if [[ -n "${SMOKE_SKIP_INTEGRATION+x}" ]]; then
        export SMOKE_SKIP_INTEGRATION
    else
        unset SMOKE_SKIP_INTEGRATION || true
    fi

    local run_list="${RUNNER_TMP}/run.list"
    : > "${run_list}"

    if (( has_requested == 1 )); then
        # Resolve every requested argument. An argument that matches no file is
        # a usage error, reported before anything is run.
        local arg resolved
        local missing=0
        while IFS= read -r arg; do
            if [[ -z "${arg}" ]]; then
                continue
            fi
            if resolved="$(resolve_test_file "${arg}")"; then
                printf '%s\n' "${resolved}" >> "${run_list}"
            else
                printf '%s: no such test file: %s\n' "${RUNNER_NAME}" "${arg}" >&2
                missing=1
            fi
        done < "${requested_file}"

        if (( missing != 0 )); then
            printf '%s: the known test files are:\n' "${RUNNER_NAME}" >&2
            while IFS= read -r line; do
                if [[ -n "${line}" ]]; then
                    printf '  %s\n' "${line##*/}" >&2
                fi
            done <<< "$(discover)"
            return 1
        fi
    else
        discover > "${run_list}"
    fi

    if [[ ! -s "${run_list}" ]]; then
        printf '%s: no test files found in %s (expected test-*.sh)\n' \
            "${RUNNER_NAME}" "${TESTS_DIR}" >&2
        return 1
    fi

    local test_file label verdict

    printf 'Running test files from %s\n\n' "${TESTS_DIR}"

    while IFS= read -r test_file; do
        if [[ -z "${test_file}" ]]; then
            continue
        fi
        label="${test_file##*/}"
        RUNNER_TOTAL=$(( RUNNER_TOTAL + 1 ))
        RUNNER_CURRENT_LABEL="${label}"
        if run_test_file "${test_file}" "${label}"; then
            verdict=0
        else
            verdict=$?
        fi
        RUNNER_CURRENT_LABEL=""
        case "${verdict}" in
            0)
                RUNNER_PASSED=$(( RUNNER_PASSED + 1 ))
                ;;
            "${SKIP_VERDICT}")
                RUNNER_SKIPPED=$(( RUNNER_SKIPPED + 1 ))
                ;;
            *)
                RUNNER_FAILED=$(( RUNNER_FAILED + 1 ))
                ;;
        esac
    done < "${run_list}"

    print_summary

    if (( RUNNER_FAILED > 0 )); then
        return 1
    fi
    return 0
}

main "$@"
