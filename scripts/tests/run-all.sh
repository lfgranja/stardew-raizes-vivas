#!/usr/bin/env bash
#
# Test runner for the 008 "smoke validation" bash suite.
#
#   ./scripts/tests/run-all.sh                                   # run every test-*.sh
#   ./scripts/tests/run-all.sh test-cli.sh                       # run one file by bare name
#   ./scripts/tests/run-all.sh scripts/tests/test-cli.sh         # ...or by path
#   ./scripts/tests/run-all.sh --list                            # list the discovered files
#   ./scripts/tests/run-all.sh --help                            # usage
#
# Each test file runs in its own subshell with:
#
#   - TMPDIR pointing at a fresh `mktemp -d` that is removed afterwards, so one
#     file's temporary files can never leak into the next;
#   - SMOKE_SKIP_INTEGRATION unset, so the launch-free suite runs on any host
#     — unless the caller already set it, in which case the caller's value is
#     passed through unchanged;
#   - the repository root as the working directory, so the relative paths the
#     tests use resolve.
#
# The stdout and stderr of every file are captured and printed indented under
# that file's own heading, so one file's output is never mistaken for
# another's, and each file is followed by a one-line verdict.
#
# The runner never stops at the first failure: every file runs and the full
# set of failures is reported in the summary.
#
# Exit status: 0 when every test file passed; 1 when a file failed, when no
# test file was found, or when an argument matched no test file.

set -Eeuo pipefail

readonly RUNNER_NAME="run-all.sh"

# The runner lives in <repo>/scripts/tests/, so the repository root is two
# levels up. It is resolved structurally rather than with `git rev-parse`, so
# the runner also works in a copied tree.
readonly RUNNER_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
readonly TESTS_DIR="${RUNNER_DIR}"
readonly REPO_ROOT="$(cd -- "${RUNNER_DIR}/../.." && pwd -P)"

# The runner's own scratch directory. Removed on exit.
RUNNER_TMP=""

cleanup() {
    if [[ -n "${RUNNER_TMP}" && -d "${RUNNER_TMP}" ]]; then
        rm -rf -- "${RUNNER_TMP}" || true
    fi
}
trap cleanup EXIT

usage() {
    cat <<'EOF'
Usage: scripts/tests/run-all.sh [--help] [--list] [TEST_FILE...]

Runs the smoke-validation bash suite and reports a per-file verdict plus an
overall total.

Arguments:
  TEST_FILE            A test file to run. Either a bare name (test-cli.sh) or
                       a path (scripts/tests/test-cli.sh, or an absolute path).
                       When no file is given, every scripts/tests/test-*.sh is
                       run, in sorted order.

Options:
  --help, -h           Print this help and exit 0.
  --list               Print the discovered test files, one per line, and exit 0.

Environment:
  SMOKE_SKIP_INTEGRATION
                       Unset for the test files by default, so the launch-free
                       suite runs on any host. When the caller has set it, the
                       caller's value is passed through unchanged.

Exit status:
  0   every test file passed
  1   a test file failed, no test file was found, or an argument matched no
      test file
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

# Run one test file and report it. Returns the file's exit status.
run_test_file() {
    local test_file="$1"
    local label="$2"

    local tmpdir
    tmpdir="$(mktemp -d 2>/dev/null || mktemp -d -p /tmp smoke-tests.XXXXXX 2>/dev/null || true)"

    local output=""
    local status=0

    if [[ -n "${tmpdir}" && -d "${tmpdir}" ]]; then
        # The subshell is the isolation boundary: the test gets its own
        # process, a private TMPDIR and the repository root as the working
        # directory. stdout and stderr are captured together.
        if output="$( cd -- "${REPO_ROOT}" && TMPDIR="${tmpdir}" bash -- "${test_file}" 2>&1 )"; then
            status=0
        else
            status=$?
        fi
        rm -rf -- "${tmpdir}" || true
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

    if (( status == 0 )); then
        printf 'PASS %s\n' "${label}"
    else
        printf 'FAIL %s\n' "${label}"
    fi
    printf '\n'

    return "${status}"
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
    local list_mode=0
    local has_requested=0
    local line

    while (( $# > 0 )); do
        case "$1" in
            --help|-h)
                usage
                return 0
                ;;
            --list)
                list_mode=1
                ;;
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

    if (( list_mode == 1 )); then
        local discovered
        discovered="$(discover)"
        if [[ -z "${discovered}" ]]; then
            printf '%s: no test files found in %s (expected test-*.sh)\n' \
                "${RUNNER_NAME}" "${TESTS_DIR}" >&2
            return 1
        fi
        while IFS= read -r line; do
            if [[ -n "${line}" ]]; then
                printf '%s\n' "${line##*/}"
            fi
        done <<< "${discovered}"
        return 0
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

    local total=0
    local passed=0
    local failed=0
    local test_file label

    printf 'Running test files from %s\n\n' "${TESTS_DIR}"

    while IFS= read -r test_file; do
        if [[ -z "${test_file}" ]]; then
            continue
        fi
        label="${test_file##*/}"
        total=$(( total + 1 ))
        if run_test_file "${test_file}" "${label}"; then
            passed=$(( passed + 1 ))
        else
            failed=$(( failed + 1 ))
        fi
    done < "${run_list}"

    printf '%d files, %d passed, %d failed\n' "${total}" "${passed}" "${failed}"

    if (( failed > 0 )); then
        return 1
    fi
    return 0
}

main "$@"
