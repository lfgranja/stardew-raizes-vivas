#!/usr/bin/env bash
#
# smoke-validation.sh — headless smoke validation for the Living Roots mod.
#
# Launches Stardew Valley through SMAPI on a private Xvfb display, waits for
# SMAPI to resolve the mods, and reports whether the Living Roots mod loaded
# without errors. The verdict is the exit code; every diagnostic goes to stderr
# and the verdict to stdout.
#
# Spec:   specs/008-smoke-validation/spec.md
# Tasks:  specs/008-smoke-validation/tasks.md
# Status: complete. The full pipeline is implemented: pre-flight, virtual
#         display and software-OpenGL probe, rebuild-and-verify, flock,
#         existing-process check, log baseline, launch, two-phase readiness
#         wait, log gate, termination and cleanup. The verdict is the exit
#         code; the stdout summary carries the verdict, the run ID, the
#         version and the run-log path, and every diagnostic goes to stderr.
#
# Conventions this file establishes for the rest of the implementation:
#
#   * every path is expanded as "$var" and passed to external commands after
#     "--"; user-supplied path roots are never re-parsed by a shell;
#   * every user-visible failure is reported as "What -> Where -> How to fix"
#     on stderr, with an actionable trailing "Fix:" line;
#   * no subprocess-specific status ever becomes the script's own exit code —
#     every child status is translated into one of the documented exit codes;
#   * option names are matched in full: long form only (except "-h"), never by
#     abbreviation, and both "--opt VALUE" and "--opt=VALUE" are accepted.

set -Eeuo pipefail

# The script needs bash 4+ (`wait -f` and the other bash-4 features the
# pipeline uses). The requirement is checked in the PRE-FLIGHT
# (preflight_tools) and NOT here, above the function definitions: T169
# requires the usage reference and the version to stay printable on a host
# whose bash is too old to run the pipeline (FR-015, FR-012 (0) (a)).
#
# Three consequences, and all of them are load-bearing:
#
#   * this file must stay PARSEABLE by bash 3. A syntax error kills the
#     shell before the parser ever ran, so `--help`, `--version` and the
#     exit-14 usage error would be unreachable on exactly the host they are
#     meant for. The file therefore declares NO bash-4-only construct at
#     LOAD time: the associative array that used to hold the tool->package
#     mapping is gone (see tool_package_for), and `wait -f` is a RUNTIME
#     option of the `wait` builtin, so it costs nothing at parse time. The
#     file parses under bash 3.2 (`bash3 -n` exits 0) and the usage paths
#     answer under bash 3.2 — the two properties are verified together,
#     because a parse error would make the usage paths unreachable anyway;
#   * the file must also stay RUNNABLE on bash 3 up to the pre-flight. Bash 3
#     differs from bash 4 in a RUNTIME rule as well: under `set -u`, the
#     expansion of an EMPTY array is an unbound-variable error, so every
#     possibly-empty array is expanded with the `${arr[@]+"${arr[@]}"}` idiom
#     (see parse_args). Without it a run with no arguments at all died with
#     the shell's own message and exit 1 instead of the mandated exit 9;
#   * a run that reaches the pipeline on such a host still fails closed
#     with exit 9 (EX_MISSING_DEPENDENCY), because the pre-flight checks
#     the version before it uses any bash-4 feature (FR-012 item 8).

# FR-014: the umask is set before the script creates any file or directory.
# It is inherited by every child, so the build output, the deployed mod tree
# and the SMAPI log are owner-only as well. It is a default, not a guarantee:
# already-existing files keep their mode, so every artifact the script creates
# must additionally have its mode enforced explicitly with chmod.
umask 077

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

readonly VERSION="0.1.0"
readonly SCRIPT_NAME="smoke-validation.sh"

# The bound on the MSBuild property query that resolves the deploy folder
# (T156). The query is an EVALUATION — it runs no target — so it answers
# in a second or two; the bound is there so a `dotnet` that hangs cannot
# stall the run. It is deliberately SHORT: the pre-flight suite's hung-run
# case gives the script a 20-second deadline, and the run has to reach the
# rebuild inside it.
readonly MSBUILD_PROPERTY_QUERY_TIMEOUT_SECS=10

# The bound on the wait for the mod's OWN success marker (T162).
#
# The marker is emitted by the mod from its `GameLaunched` handler
# (`ModController.cs:959`), which SMAPI reaches only AFTER it resolved the
# mods — so the marker lands in the window between the mod-resolution
# header and the termination, and never in the log at the moment the
# header is observed (FR-003, FR-006).
#
# The wait is bounded twice: by this constant and by the readiness
# deadline, so it can neither run away nor push the run past the
# launch-to-verdict budget FR-009 defines. The marker is a SECONDARY
# signal: the wait ends on the window's expiry and the verdict never
# depends on it.
readonly MOD_SUCCESS_MARKER_WINDOW_SECS=30


# The script's own directory.
#
# The path is derived with the shell's own parameter expansion and NOT with
# `dirname`: the tool sweep is the first thing the script runs, and a host
# without `dirname` must be told the tool is missing (FR-012 item 8) rather
# than dying here with the shell's own 127 — before the sweep ever ran.
_script_source="${BASH_SOURCE[0]}"
case "$_script_source" in
    */*) _script_dir="${_script_source%/*}" ;;
    *)   _script_dir="." ;;
esac
SCRIPT_DIR="$(cd -- "$_script_dir" && pwd)"
readonly SCRIPT_DIR
readonly REPO_ROOT="${SCRIPT_DIR}/.."
unset _script_source _script_dir

# How the script is written in the usage examples: the path it would be
# invoked with from the repository root, which is the form a user types.
SCRIPT_INVOCATION="./scripts/${SCRIPT_NAME}"
if [[ ! -d "${SCRIPT_DIR}/.." ]]; then
    SCRIPT_INVOCATION="${SCRIPT_DIR}/${SCRIPT_NAME}"
fi

# ---------------------------------------------------------------------------
# Exit codes — the FR-007 taxonomy (one code per failure class).
#
# Allocation rule (FR-007): codes are allocated additively and permanently.
# A published code is never re-assigned and a retired code is never reused.
# A new category takes the next value ABOVE the highest published code.
# ---------------------------------------------------------------------------

readonly EX_OK=0
readonly EX_GAME_NOT_FOUND=1
readonly EX_SMAPI_NOT_FOUND=2
readonly EX_GRAPHICS_UNUSABLE=3
readonly EX_TIMEOUT=4
readonly EX_MOD_LOAD_FAILURE=5
readonly EX_LOCK_CONTENTION=6
readonly EX_PROCESS_CONFLICT=7
# Code 8 is RETIRED (round 2, superseded by the POSIX 128+N interruption
# codes). It is never reused and never handed out again.
readonly EX_MISSING_DEPENDENCY=9
readonly EX_DISPLAY_UNAVAILABLE=10
readonly EX_STEAM_PREREQ_MISSING=11
readonly EX_BUILD_ARTIFACT_FAILURE=12
readonly EX_INTERNAL_ERROR=13
readonly EX_USAGE_ERROR=14
readonly EX_NO_LOG_PRODUCED=15
readonly EX_MOD_MANIFEST_INVALID=16
readonly EX_MOD_RESOLUTION_ABORTED=17
readonly EX_SIGHUP=129
readonly EX_SIGINT=130
readonly EX_SIGTERM=143

# ---------------------------------------------------------------------------
# The option surface — the spec's normative Command-Line Options table.
# The table owns the spelling, the argument placeholder and the default;
# `--help` is a rendering of it, and the parser below matches names in full.
# ---------------------------------------------------------------------------

readonly KNOWN_LONG_OPTIONS=(
    --game-path
    --mods-path
    --mod-project
    --timeout
    --build-timeout
    --grace-period
    --smapi-log
    --force
    --help
    --version
)

# ---------------------------------------------------------------------------
# Parsed configuration. Filled in by parse_args()/apply_defaults().
# ---------------------------------------------------------------------------

# The run log is created only after the command line has been accepted, so
# `die` can consult these safely (a usage error must not create the log).
run_log_started=0
run_started_epoch=0
run_log_path=""

game_path=""
mods_path=""
mod_project=""
smapi_log=""
timeout_secs=120
build_timeout_secs=300
grace_period_secs=15
force=0
mods_path_set=0

# ---------------------------------------------------------------------------
# Error reporting
# ---------------------------------------------------------------------------

# die <exit-code> <what> <where> <fix>
#
# Prints the mandated "What -> Where -> How to fix" report on stderr and exits
# with the given code. Every user-visible failure goes through this function,
# so no diagnostic is ever emitted without an actionable remedy.
#
# NOTE: never call this from a command substitution — `exit` inside `$(...)`
# only terminates the subshell. All validation therefore runs in the main
# shell and only assigns to variables from there.
die() {
    local code="$1"
    local what="$2"
    local where="$3"
    local fix="$4"
    local context="${5-}"
    local stamp

    # T168: the report is a CHAIN of commands (the cleanup, the stamp, the
    # report's own pipeline, the summary) and every one of them can fail on
    # its own — the cleanup's `date`, the report's `sed`, the summary's
    # `date`. `set +e` disables errexit for the rest of the handler, so a
    # failure inside it can neither abort the report nor replace the
    # mandated exit code with that command's raw status: a handler that runs
    # a failing command still exits with the code it was given. The function
    # still ends with an explicit `exit`, so the run still aborts.
    set +e

    # T159: the phase the run failed in is closed BEFORE the cleanup appends
    # its own entry, so the failed phase appears in the stdout
    # `phase durations:` block and in the run log, with its end timestamp and
    # its elapsed seconds (FR-014, SC-001).
    close_open_phase

    # FR-010 / FR-006: the cleanup runs BEFORE the report, exactly as on the
    # success path, so (a) the report stays the LAST thing on stderr, (b) the
    # summary of a failing run carries the cleanup's duration, and (c) the
    # cleanup's lifecycle events precede the report in the run log. The
    # cleanup is idempotent, so the EXIT trap's later call is a no-op.
    cleanup
    if ((cleanup_pgid_assertion_failed == 1)); then
        # FR-010: a failed PGID assertion in the cleanup path is a hard
        # failure — the group was left unsignalled. The reason is already in
        # the run log; it is folded into THIS report so a failing run still
        # emits exactly one failure report.
        code="$EX_INTERNAL_ERROR"
        if [[ -n "$context" ]]; then
            context="${context}"$'\n'"the cleanup also refused to signal a process group (see the run log)"
        else
            context="the cleanup also refused to signal a process group (see the run log)"
        fi
    fi

    # FR-014: the failure report carries a timestamp, a level and the run
    # ID, exactly like every other line the script emits. The timestamp is
    # read defensively: `die` is reachable from the tool sweep, and the
    # sweep is what reports a host without `date`.
    stamp="$(date -u +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || printf 'unknown') [ERROR] [${run_id:-no-run-id}]"

    # FR-014: EVERY line of the report carries the stamp — the run log holds
    # no unstamped line. The report is built once and emitted IDENTICALLY to
    # stderr and to the run log, so every stderr line is an exact line of
    # the log.
    local report=""
    report="$(
        {
            printf '%s ERROR: smoke validation failed (exit %s).\n' "$stamp" "$code"
            printf '%s   What: %s\n' "$stamp" "$what"
            printf '%s   Where: %s\n' "$stamp" "$where"
            if [[ -n "$context" ]]; then
                printf '%s   Context:\n' "$stamp"
                printf '%s\n' "$context" | sed "s|^|${stamp}     |"
            fi
            # FR-007: the "Fix:" line is the TRAILING line of the report.
            printf '%s   Fix: %s\n' "$stamp" "$fix"
        }
    )"

    # T118: the run has now reported a failure of its OWN, so the EXIT
    # trap's cleanup-failure report must not add a second, contradictory one.
    failure_report_emitted=1

    printf '%s\n' "$report" >&2

    # The run log holds every line the script emits, so the failure report
    # is teed into it as well. The log is only opened after the command
    # line has been accepted, so a usage error never creates it.
    if ((run_log_started == 1)) && [[ -n "$run_log_path" ]]; then
        printf '%s\n' "$report" >>"$run_log_path" 2>/dev/null || true
    fi

    # FR-014: a failing run prints a summary too, so the caller always has
    # the run ID, the version and the run-log path. A usage error never
    # started a run (no run log exists), so it prints nothing on stdout.
    #
    # The cleanup runs BEFORE the summary, exactly as on the success path, so
    # the summary of a failing run carries the cleanup's duration too and the
    # cleanup's lifecycle events precede the report in the run log. The
    # cleanup is idempotent, so the EXIT trap's later call is a no-op.
    if ((run_log_started == 1)); then
        print_summary "FAILURE: the run failed (exit ${code})." "$code"
    fi

    exit "$code"
}

# run_elapsed_seconds — the seconds since the run started.
#
# The clock is read defensively: a host whose `date` cannot answer must not
# turn the summary into an arithmetic error. The elapsed time is a
# diagnostic, never a verdict, so a broken clock reports 0 rather than an
# arithmetic failure.
run_elapsed_seconds() {
    local now start

    start="${run_started_epoch:-0}"
    now="$(date +%s 2>/dev/null || printf '%s' "$start")"
    if ((now <= start)); then
        printf '0'
        return 0
    fi

    printf '%s' "$((now - start))"
}

# print_summary <verdict> <exit-code>
#
# FR-014: the summary carries the verdict, the run ID, the VERSION and the
# run-log path, plus the total wall time. It goes to stdout AND to the run
# log, so the log holds every line the script emits.
print_summary() {
    local verdict="$1"
    local code="$2"
    local summary

    summary="$(
        {
            printf '%s\n' "$verdict"
            if [[ -n "$mod_success_marker" ]]; then
                printf 'mod-success-marker: %s\n' "$mod_success_marker"
            fi
            printf 'run-id: %s\n' "$run_id"
            printf 'version: %s %s\n' "$SCRIPT_NAME" "$VERSION"
            printf 'run-log: %s\n' "$run_log_path"
            printf 'total wall time: %ss\n' "$(run_elapsed_seconds)"
        }
    )"

    printf '%s\n' "$summary"

    # FR-014: the stdout summary MUST print the total wall time AND each
    # phase's duration, so the same block that goes into the run log is
    # printed here.
    printf 'phase durations:\n'
    print_phase_timings

    # The run log holds every line the script emits, and EVERY line in it
    # carries the timestamp, the level and the run ID (FR-014) — so the
    # summary is written to the log stamped, and to stdout verbatim.
    if ((run_log_started == 1)) && [[ -n "$run_log_path" ]]; then
        while IFS= read -r _summary_line; do
            log_event_quiet INFO "$_summary_line"
        done <<< "$summary"

        log_phase_timings
    fi
}

# log_phase_timings
#
# FR-014: every phase's duration is recorded, and the helper that renders
# them is called once, here.
log_phase_timings() {
    local _timing_line

    ((run_log_started == 1)) || return 0
    [[ -n "$run_log_path" ]] || return 0

    log_event_quiet INFO "phase durations:"
    while IFS= read -r _timing_line; do
        log_event_quiet INFO "$_timing_line"
    done <<< "$(print_phase_timings)"
}

# usage_error <what> <fix>
#
# A CLI-argument error: unknown option, missing required argument or a
# malformed value. Always exit 14 (FR-007).
usage_error() {
    die "$EX_USAGE_ERROR" "$1" "command line" "$2"
}

# on_error <status> <line> <function>
#
# ERR trap handler (FR-014). Reports the script name, the failing line
# number, the enclosing function name and the exit code, then exits 13 —
# never with the raw status of the failing command, which under `set -e`
# would otherwise masquerade as a domain failure.
#
# shellcheck disable=SC2329  # invoked by the ERR trap below, which shellcheck cannot see
on_error() {
    local status="$1"
    local line="$2"
    local func="$3"

    # T158: `set -E` (errtrace) makes the ERR trap fire inside command
    # substitutions and inside every other subshell too. `die` inside such a
    # subshell prints a report and exits ONLY that subshell — the enclosing
    # command then fails on the substitution's status and the OUTER shell
    # runs the handler a SECOND time, so a single failure is reported twice
    # and the two reports contradict each other.
    #
    # In a subshell the handler therefore only RETURNS the status: the outer
    # shell reports the failure, exactly once, with the outer shell's own
    # view of what failed.
    if ((BASH_SUBSHELL > 0)); then
        return "$status"
    fi

    # T168: the handler is a chain (`on_error` -> `die` -> `cleanup` ->
    # report -> summary) and every command in that chain can fail on its
    # own. A failure inside the handler MUST NOT replace the mandated exit
    # code 13 with that command's raw status, so errexit is disabled for the
    # rest of the handler. `die` ends with an explicit `exit`, so the run
    # still aborts.
    set +e

    die "$EX_INTERNAL_ERROR" \
        "unhandled error (the failing command exited with status ${status}) inside '${func}'" \
        "${SCRIPT_NAME} line ${line}" \
        "this is a bug in ${SCRIPT_NAME} — re-run with the run log attached and report it"
}

# `-E` (errtrace) makes the ERR trap fire inside shell functions too, which
# is what lets the handler name the enclosing function.
trap 'on_error "$?" "$LINENO" "${FUNCNAME[0]:-<top-level>}"' ERR

# ---------------------------------------------------------------------------
# Small helpers
# ---------------------------------------------------------------------------

# expand_tilde <path>
#
# Expands a leading "~" to the home directory. The shell does not expand a
# tilde inside quotes, so the script does it explicitly: every path is stored
# expanded, and later passed to external commands as "$var" behind "--".
expand_tilde() {
    local path="$1"

    # A tilde can only be expanded when the home directory is known. When
    # HOME is unset the path is left UNCHANGED: the run is then aborted by
    # the HOME check that follows the parser (13), and the environment fault
    # is reported as what it is. Expanding it anyway would report the
    # environment fault as a usage error (14) for a bare `~`, and would
    # expand `~/x` into the WRONG path (`/x`).
    if [[ -z "${HOME:-}" ]]; then
        printf '%s\n' "$path"
        return 0
    fi

    # shellcheck disable=SC2088  # the tildes below are quoted ON PURPOSE: the shell must not expand them here
    case "$path" in
        '~') printf '%s\n' "$HOME" ;;
        '~/'*) printf '%s\n' "${HOME}/${path:2}" ;;
        *) printf '%s\n' "$path" ;;
    esac
}

# require_arg <remaining-arg-count> <option> <placeholder>
#
# Fails with a usage error when the option's required argument is missing.
require_arg() {
    if (($1 < 2)); then
        usage_error \
            "missing required argument for '$2' (expected: $2 $3)" \
            "pass the argument as '$2 $3' or '$2=<value>' — see '${SCRIPT_NAME} --help'"
    fi
}

# require_path_value <value> <option> <placeholder>
#
# Fails with a usage error when a path option is given an empty value.
require_path_value() {
    if [[ -z "$1" ]]; then
        usage_error \
            "empty value for '$2' (expected: $2 $3)" \
            "pass a non-empty path as '$2 $3' or '$2=<path>' — see '${SCRIPT_NAME} --help'"
    fi
}

# require_seconds <value> <option>
#
# Fails with a usage error when a numeric option is not a positive integer.
require_seconds() {
    local value="$1"
    local opt="$2"

    # The value must be a non-empty run of digits (at most 15 — far beyond any
    # sane timeout, and small enough for the shell's arithmetic to hold
    # exactly) and must not be zero. The zero test is a pattern rather than an
    # arithmetic comparison: a leading zero is an octal prefix in the shell's
    # arithmetic, so "08" would be read as an invalid octal number.
    if [[ ! "$value" =~ ^[0-9]{1,15}$ ]] || [[ "$value" =~ ^0+$ ]]; then
        usage_error \
            "invalid value '${value}' for '${opt}' (expected a positive integer number of SECONDS)" \
            "pass a positive integer, e.g. '${opt}=120' — see '${SCRIPT_NAME} --help'"
    fi
}

# canonical_decimal <digits>
#
# Strips the leading zeros from an already-validated digit string, so the
# stored value is a plain decimal literal. This matters because the shell's
# arithmetic reads a leading zero as an octal prefix — "08" is not a valid
# octal number — and a value that breaks the arithmetic of a later phase
# would surface as an internal error rather than as the malformed value it is.
# A validated value is always greater than zero.
canonical_decimal() {
    local value="$1"

    value="${value#"${value%%[!0]*}"}"
    printf '%s' "${value:-0}"
}

# join_words <word>...
#
# Joins words with commas and a final "and", for readable diagnostics.
join_words() {
    local out=""
    local word=""
    local count="$#"
    local i=0

    for word in "$@"; do
        i=$((i + 1))
        if ((i == 1)); then
            out="$word"
        elif ((i == count)); then
            out="${out} and ${word}"
        else
            out="${out}, ${word}"
        fi
    done

    printf '%s' "$out"
}

# unknown_option <token>
#
# Reports an option that is not in the normative table. Because the parser
# never matches by prefix, `--mod` and `--mods` land here; the hint names the
# full option the caller probably meant, but the abbreviation is NOT accepted.
unknown_option() {
    local token="$1"
    local name="$1"
    local -a candidates=()
    local known=""
    local list=""

    case "$token" in
        *=*) name="${token%%=*}" ;;
    esac

    for known in "${KNOWN_LONG_OPTIONS[@]}"; do
        case "$known" in
            "$name"*) candidates+=("$known") ;;
        esac
    done

    case "${#candidates[@]}" in
        0)
            usage_error \
                "unknown option '${token}'" \
                "run '${SCRIPT_NAME} --help' for the supported options — long form only ('-h' is the only short option), and option names are never abbreviated"
            ;;
        1)
            usage_error \
                "unknown option '${token}' (not an exact option name)" \
                "option names are not abbreviated — use the full name '${candidates[0]}'"
            ;;
        *)
            list="$(join_words "${candidates[@]}")"
            usage_error \
                "unknown option '${token}' (ambiguous abbreviation of ${list})" \
                "option names are not abbreviated — use the full name: ${list}"
            ;;
    esac
}

# ---------------------------------------------------------------------------
# Option parsing
# ---------------------------------------------------------------------------

# parse_args <args...>
#
# Long form only, except "-h". Both "--opt VALUE" and "--opt=VALUE" are
# accepted and equivalent. Only exact long-option names are accepted — the
# GNU getopt_long unique-prefix extension is deliberately NOT enabled, so
# "--mod" (ambiguous) and "--mods" (a prefix of "--mods-path") are rejected.
# "--" terminates option processing; the script takes no positional arguments.
parse_args() {
    local -a argv=()
    local arg=""
    local name=""

    # Normalise "--opt=value" into "--opt value" so that a single case per
    # option handles both spellings. Unknown options and flags are left
    # untouched so that the error names exactly what the caller typed.
    for arg in "$@"; do
        case "$arg" in
            --*=*)
                name="${arg%%=*}"
                case "$name" in
                    --game-path|--mods-path|--mod-project|--timeout|--build-timeout|--grace-period|--smapi-log)
                        argv+=("$name" "${arg#*=}")
                        ;;
                    *)
                        argv+=("$arg")
                        ;;
                esac
                ;;
            *)
                argv+=("$arg")
                ;;
        esac
    done

    # The `+word` form is the bash-3-safe expansion of a POSSIBLY-EMPTY array.
    #
    # T169: under `set -u`, bash 3.2 treats the expansion of an EMPTY array as
    # an unbound variable and ABORTS the script with the shell's own message
    # ("argv[@]: unbound variable"). A run with no arguments at all therefore
    # died with exit 1 on a bash-3 host, before the pre-flight could report
    # the missing requirement (exit 9). The idiom expands to nothing for an
    # empty array and to the elements — with their quoting — otherwise, on
    # every bash version.
    set -- "${argv[@]+"${argv[@]}"}"

    while (($# > 0)); do
        case "$1" in
            --game-path)
                require_arg "$#" "$1" DIR
                require_path_value "$2" "$1" DIR
                game_path="$(expand_tilde "$2")"
                shift 2
                ;;
            --mods-path)
                require_arg "$#" "$1" DIR
                require_path_value "$2" "$1" DIR
                mods_path="$(expand_tilde "$2")"
                mods_path_set=1
                shift 2
                ;;
            --mod-project)
                require_arg "$#" "$1" FILE
                require_path_value "$2" "$1" FILE
                mod_project="$(expand_tilde "$2")"
                shift 2
                ;;
            --timeout)
                require_arg "$#" "$1" SECONDS
                require_seconds "$2" "$1"
                timeout_secs="$(canonical_decimal "$2")"
                shift 2
                ;;
            --build-timeout)
                require_arg "$#" "$1" SECONDS
                require_seconds "$2" "$1"
                build_timeout_secs="$(canonical_decimal "$2")"
                shift 2
                ;;
            --grace-period)
                require_arg "$#" "$1" SECONDS
                require_seconds "$2" "$1"
                grace_period_secs="$(canonical_decimal "$2")"
                shift 2
                ;;
            --smapi-log)
                require_arg "$#" "$1" FILE
                require_path_value "$2" "$1" FILE
                smapi_log="$(expand_tilde "$2")"
                shift 2
                ;;
            --force)
                force=1
                shift
                ;;
            --help|-h)
                print_help
                exit 0
                ;;
            --version)
                printf '%s %s\n' "$SCRIPT_NAME" "$VERSION"
                exit 0
                ;;
            --force=*|--help=*|--version=*)
                name="${1%%=*}"
                usage_error \
                    "option '${name}' does not take an argument (it is a flag)" \
                    "run '${SCRIPT_NAME} ${name}' without a value — see '${SCRIPT_NAME} --help'"
                ;;
            --)
                # Everything after "--" is a non-option argument. The script
                # takes none, so any that follow are a usage error.
                shift
                if (($# > 0)); then
                    usage_error \
                        "unexpected argument '${1}'" \
                        "the script takes no positional arguments — option processing ends at '--'; run '${SCRIPT_NAME} --help' for the supported options"
                fi
                break
                ;;
            -*)
                unknown_option "$1"
                ;;
            *)
                usage_error \
                    "unexpected argument '${1}'" \
                    "the script takes no positional arguments — run '${SCRIPT_NAME} --help' for the supported options"
                ;;
        esac
    done
}

# apply_defaults
#
# Fills in the options the caller did not supply. The `--mods-path` default
# depends on the resolved `--game-path`, so it is applied after parsing and
# not at the time the option is read.
apply_defaults() {
    if [[ -z "$game_path" ]]; then
        game_path="${HOME}/.steam/steam/steamapps/common/Stardew Valley"
    fi

    if ((mods_path_set == 0)); then
        mods_path="${game_path%/}/Mods"
    fi

    # FR-017: a relative --mods-path is resolved against the GAME directory
    # so it does not resolve against the process working directory. The rule
    # applies to a USER-SUPPLIED value only: the derived default is already
    # anchored at the game directory, and applying the rule to it as well
    # would double the game directory ("--game-path ." would yield
    # "././Mods" instead of "./Mods").
    if ((mods_path_set == 1)) && [[ "$mods_path" != /* ]]; then
        mods_path="${game_path%/}/${mods_path}"
    fi

    if [[ -z "$mod_project" ]]; then
        mod_project="${REPO_ROOT}/LivingRoots/LivingRoots.csproj"
    fi

    if [[ -z "$smapi_log" ]]; then
        smapi_log="${HOME}/.config/StardewValley/ErrorLogs/SMAPI-latest.txt"
    fi
}

# ---------------------------------------------------------------------------
# --help / --version
#
# The `--help` text is a rendering of the spec's normative Command-Line
# Options table — the table is the source of truth (the same relation
# help2man establishes between a man page and --help). The order of the
# sections is fixed by FR-015.
# ---------------------------------------------------------------------------

# render_exit_codes
#
# Renders the exit-code section of `--help` from the constants above, so the
# documented codes and the codes the script actually exits with are the same
# values and cannot drift apart. The order is the numeric order of the FR-007
# taxonomy; the retired code 8 is deliberately absent.
render_exit_codes() {
    printf '  %-4s %s\n' "$EX_OK" 'success — the mod loaded without errors'
    printf '  %-4s %s\n' "$EX_GAME_NOT_FOUND" 'game installation not found'
    printf '  %-4s %s\n' "$EX_SMAPI_NOT_FOUND" 'SMAPI not found or not functional'
    printf '  %-4s %s\n' "$EX_GRAPHICS_UNUSABLE" 'graphics environment present but unusable'
    printf '  %-4s %s\n' "$EX_TIMEOUT" 'timeout — no verdict within the deadline'
    printf '  %-4s %s\n' "$EX_MOD_LOAD_FAILURE" 'mod load failure'
    printf '  %-4s %s\n' "$EX_LOCK_CONTENTION" 'lock contention — another smoke test instance is running'
    printf '  %-4s %s\n' "$EX_PROCESS_CONFLICT" 'process conflict — a Stardew Valley/SMAPI process is already running'
    printf '  %-4s %s\n' "$EX_MISSING_DEPENDENCY" 'missing host tool or runtime dependency'
    printf '  %-4s %s\n' "$EX_DISPLAY_UNAVAILABLE" 'virtual display could not be allocated or started'
    printf '  %-4s %s\n' "$EX_STEAM_PREREQ_MISSING" 'Steam API prerequisite missing'
    printf '  %-4s %s\n' "$EX_BUILD_ARTIFACT_FAILURE" 'build or artifact-identity failure'
    printf '  %-4s %s\n' "$EX_INTERNAL_ERROR" 'internal script error'
    printf '  %-4s %s\n' "$EX_USAGE_ERROR" 'usage or CLI-argument error'
    printf '  %-4s %s\n' "$EX_NO_LOG_PRODUCED" 'the game produced no SMAPI log'
    printf '  %-4s %s\n' "$EX_MOD_MANIFEST_INVALID" 'mods path or mod manifest invalid'
    printf '  %-4s %s\n' "$EX_MOD_RESOLUTION_ABORTED" 'SMAPI aborted before mod resolution'
    printf '  %-4s %s\n' "$EX_SIGHUP" 'terminated by SIGHUP (128+1)'
    printf '  %-4s %s\n' "$EX_SIGINT" 'interrupted by SIGINT (128+2)'
    printf '  %-4s %s\n' "$EX_SIGTERM" 'terminated by SIGTERM (128+15)'
}

print_help() {
    local body=""

    IFS= read -r -d '' body <<'HELP_EOF' || true
Usage: smoke-validation.sh [--game-path DIR] [--mods-path DIR] [--mod-project FILE] [--timeout SECONDS] [--build-timeout SECONDS] [--grace-period SECONDS] [--smapi-log FILE] [--force] [--help] [-h] [--version]

Options:
  --game-path DIR            Root of the game installation; also the launch working directory and the base against which a relative mods directory is resolved. (default: ~/.steam/steam/steamapps/common/Stardew Valley/)
  --mods-path DIR            The mods directory handed to SMAPI and pinned as the build's deploy target. (default: <game-path>/Mods)
  --mod-project FILE         The .csproj rebuilt and digest-verified before the launch. (default: <repo-root>/LivingRoots/LivingRoots.csproj)
  --timeout SECONDS          The game-launch-to-verdict deadline (the two-phase wait). (default: 120)
  --build-timeout SECONDS    The bound on the rebuild-and-verify phase; expiry exits 12. (default: 300)
  --grace-period SECONDS     The SIGTERM then SIGKILL escalation window. (default: 15)
  --smapi-log FILE           The SMAPI log polled for readiness and for the verdict. (default: ~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt)
  --force (none — flag)      Terminate an existing game instance (SIGTERM then SIGKILL) instead of aborting; the signal targets the identified PID only. (default: off)
  --help (none — flag)       Print this usage text to stdout and exit 0. (default: —)
  -h (none — flag)           Alias for --help. (default: —)
  --version (none — flag)    Print '<name> <VERSION>' to stdout and exit 0. (default: —)

Environment (set automatically):
  SMAPI_NO_TERMINAL=true      SMAPI runs without opening its own console window.
  SMAPI_DEVELOPER_MODE=true   SMAPI console verbosity (the log file is always verbose).
  LIBGL_ALWAYS_SOFTWARE=1     Force Mesa's software rasteriser.
  GALLIUM_DRIVER=llvmpipe     Select the llvmpipe Gallium driver.
  SDL_VIDEODRIVER=x11         SDL renders into the virtual X display.
  SDL_AUDIODRIVER=dummy       SDL uses the null audio device (the game stays muted).
  ALSOFT_DRIVERS=null         OpenAL Soft uses the null device (the game stays muted).
  DISPLAY                    Set to the display number Xvfb allocates for this run.

  These variables are managed by the script and are not user-configurable.

Run log:
  ${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log
  (file mode 0600; the directories below the XDG base are mode 0700)

Run ID:
  <UTC timestamp>-<PID>, for example 20261019T183302Z-48213

Version:
HELP_EOF
    printf '%s\n' "${body%$'\n'}"
    printf '  %s %s\n' "$SCRIPT_NAME" "$VERSION"

    printf '\nExamples:\n'
    printf '  %s --game-path="/path/to/Stardew Valley"\n' "$SCRIPT_INVOCATION"
    printf '  %s --mods-path="/path/to/mods" --timeout=180\n' "$SCRIPT_INVOCATION"
    printf '  %s --smapi-log="/path/to/SMAPI-latest.txt"\n' "$SCRIPT_INVOCATION"

    printf '\nExit codes:\n'
    render_exit_codes
    printf '\n  Code 8 is retired and is never reused.\n'
}

# ---------------------------------------------------------------------------
# T004 — the FR-007 status translation table.
#
# No subprocess-specific status may ever become the script's own final code:
# every child status is translated into one of the published categories
# before the script exits. The role argument disambiguates 137 (SIGKILL),
# which means "the game was killed" for the game and "SMAPI was killed" for
# the apphost.
# ---------------------------------------------------------------------------

translate_child_status() {
    local status="$1"
    local role="${2:-other}"

    case "$status" in
        0)   printf '%s' "$EX_OK" ;;
        124) printf '%s' "$EX_TIMEOUT" ;;
        125) printf '%s' "$EX_INTERNAL_ERROR" ;;
        126) printf '%s' "$EX_MISSING_DEPENDENCY" ;;
        127) printf '%s' "$EX_MISSING_DEPENDENCY" ;;
        131) printf '%s' "$EX_SMAPI_NOT_FOUND" ;;
        134) printf '%s' "$EX_SMAPI_NOT_FOUND" ;;
        135) printf '%s' "$EX_INTERNAL_ERROR" ;;
        137)
            case "$role" in
                game)    printf '%s' "$EX_MOD_LOAD_FAILURE" ;;
                apphost) printf '%s' "$EX_SMAPI_NOT_FOUND" ;;
                *)       printf '%s' "$EX_INTERNAL_ERROR" ;;
            esac
            ;;
        154) printf '%s' "$EX_SMAPI_NOT_FOUND" ;;
        129) printf '%s' "$EX_SIGHUP" ;;
        130) printf '%s' "$EX_SIGINT" ;;
        143) printf '%s' "$EX_SIGTERM" ;;
        *)   printf '%s' "$EX_INTERNAL_ERROR" ;;
    esac
}

# ---------------------------------------------------------------------------
# T005 — path handling.
#
# `realpath` is used with an explicit mode: the default (-E) requires every
# component but the last to exist and therefore fails on exactly the paths
# this script mutates (the deploy target it deletes, the crash marker it
# deletes, a clean machine's mods directory).
#
# No character allowlist is applied to environment-derived names — the
# user-supplied roots, the game's own file names ("Stardew Valley.dll") and
# everything inherited from them are unrestricted. Only the script's own
# authored literals are constrained.
# ---------------------------------------------------------------------------

canonicalize_existing() {
    realpath -e -- "$1" 2>/dev/null || return 1
}

canonicalize_maybe_missing() {
    realpath -m -- "$1" 2>/dev/null || return 1
}

# path_under_base <canonical-candidate> <canonical-base>
#
# The component-boundary prefix test. The base MUST already be canonical and
# MUST NOT carry a trailing slash: a base that keeps one yields
# ".../Stardew Valley//*", which never matches.
path_under_base() {
    local candidate="$1"
    local base="$2"

    [[ -n "$base" && -n "$candidate" && "$candidate" == "$base"/* ]]
}

# own_literal_ok <name>
#
# The only character test in the script, and it applies ONLY to the script's
# own authored literals. A positive `case` character class would match every
# path (it matches the FIRST character, not the whole string), so the test
# uses a negated glob class.
own_literal_ok() {
    local name="$1"
    [[ -n "$name" && "$name" != *[!A-Za-z0-9._-]* ]]
}

# assert_own_literal <name> <what>
#
# FR-012 item 7: the script's OWN authored names are constants and must
# match [A-Za-z0-9._-]+. The rule makes an accidental space or metacharacter
# in a newly added literal a visible, reviewable edit. It does NOT apply to
# environment-derived names — those are unrestricted.
assert_own_literal() {
    local name="$1"
    local what="${2:-path}"

    if ! own_literal_ok "$name"; then
        die "$EX_INTERNAL_ERROR" \
            "the script's own ${what} literal is not a plain name" \
            "$name" \
            "this is a bug in ${SCRIPT_NAME} — an authored literal must match [A-Za-z0-9._-]+"
    fi
}

# validate_own_literals
#
# FR-012 item 7: the script's own authored names are checked ONCE, against
# the list of literals the script appends to a user-supplied root. A new
# literal is added here, so the check cannot be forgotten.
validate_own_literals() {
    local literal

    for literal in \
        "Mods" \
        "LivingRoots" \
        "smapi-internal" \
        "ErrorLogs" \
        "steam_appid.txt" \
        "StardewModdingAPI" \
        "StardewModdingAPI.dll" \
        "SMAPI.Toolkit.dll" \
        "SMAPI.Toolkit.CoreInterfaces.dll" \
        "SMAPI-latest.txt" \
        "SMAPI-crash.txt" \
        "StardewModdingAPI.crash.marker" \
        "StardewModdingAPI.update.marker" \
        "manifest.json" \
        "bin"
    do
        assert_own_literal "$literal" "path"
    done
}

# The canonicalised path the last containment check resolved.
#
# The containment helper cannot both print the path and fail the run: it is
# called from a command substitution, where `exit` would end only the
# subshell. The resolved path is therefore returned in this global and the
# CALLER reports the failure with `die` in its own shell — so exactly ONE
# failure report is printed (FR-007).
mutating_path_real=""

# check_mutating_path_under_base <path> <base>
#
# FR-012 item 7: a path the script MUTATES is canonicalised with realpath -m
# (it need not exist) and prefix-tested against its base with the
# component-boundary test. The base must already be canonical and must not
# carry a trailing slash.
#
# The canonicalised path is returned in `mutating_path_real` and the
# function returns 0 when the path resolves inside the base, 1 otherwise.
# It NEVER calls die: it is called from a command substitution, where an
# `exit` would end only the subshell and the run would then carry a SECOND,
# contradictory report from the ERR trap.
check_mutating_path_under_base() {
    local path="$1"
    local base="$2"

    mutating_path_real="$(canonicalize_maybe_missing "$path" 2>/dev/null || printf '')"

    [[ -n "$mutating_path_real" ]] && path_under_base "$mutating_path_real" "$base"
}

# read_manifest_field <manifest path> <field>
#
# Reads a string field out of the mod's manifest.json without a JSON parser
# (the script may not add a dependency).
#
# FR-012 item 7: the read is ATTEMPTED — the result of the read decides
# whether the field was found, not an existence test on the file.
read_manifest_field() {
    local manifest="$1"
    local field="$2"
    local value=""

    value="$(sed -n "s/.*\"${field}\"[[:space:]]*:[[:space:]]*\"\([^\"]*\)\".*/\1/p" -- "$manifest" 2>/dev/null | head -1 || printf '')"

    if [[ -z "$value" ]]; then
        return 1
    fi

    printf '%s' "$value"
}

# ---------------------------------------------------------------------------
# T006 — the run log.
#
# The run log is the script's own diagnostic output and is distinct from the
# SMAPI log. It is created AFTER option parsing succeeds, so a rejected
# command line never creates it (seven CLI assertions depend on that).
#
# The file is mode 0600 and EVERY directory level below the XDG base is
# mode 0700: a directory's mode is not inherited by the files inside it, and
# `mkdir -p -m 0700` applies the mode to the final component only, leaving
# the intermediate level world-traversable. So the umask is the default and
# chmod is the guarantee.
# ---------------------------------------------------------------------------

run_id=""
run_log_path=""
run_log_started=0
run_started_epoch=0
run_log_dir=""
declare -a phase_names=()
declare -a phase_elapsed=()
phase_current=""
phase_current_start=0

log_event() {
    local level="$1"
    shift
    local message="$*"
    local line ts

    # FR-014: the line carries the timestamp, the level and the run ID. The
    # stamp comes from the host's `date`, so a host whose `date` cannot
    # answer leaves it empty — the fallback keeps the line's shape. The
    # assignment is NOT guarded: a failing `date` still fails the
    # assignment, so the ERR trap still fires and the run still reports the
    # failure (T168).
    ts="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    if [[ -z "$ts" ]]; then
        ts="unknown"
    fi

    line="${ts} [${level}] [${run_id:-no-run-id}] ${message}"

    if ((run_log_started == 1)) && [[ -n "$run_log_path" ]]; then
        printf '%s\n' "$line" >>"$run_log_path" 2>/dev/null || true
    fi

    # During cleanup the events go to the run log only: the failure report
    # must stay the LAST thing written to stderr.
    if ((in_cleanup == 1)); then
        return 0
    fi

    printf '%s\n' "$line" >&2
}

# log_event_quiet <level> <message> — like log_event, but the run log ONLY.
#
# A child's own output is routed through it: FR-005 requires that output to
# reach the run log, and FR-014 requires EVERY line in the run log to carry
# the timestamp, the level and the run ID. The child's output is not a
# diagnostic the script emitted, so it does not go to stderr.
log_event_quiet() {
    local level="$1"
    shift
    local message="$*"
    local line ts

    ts="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    if [[ -z "$ts" ]]; then
        ts="unknown"
    fi

    line="${ts} [${level}] [${run_id:-no-run-id}] ${message}"

    if ((run_log_started == 1)) && [[ -n "$run_log_path" ]]; then
        printf '%s\n' "$line" >>"$run_log_path" 2>/dev/null || true
    fi
}

# start_stamped_output <fifo> <label> <pid-variable>
#
# Starts the reader that stamps a child's output into the run log. The child
# is the FIFO's WRITER and the stamper is its READER, so the two rendezvous
# when the child opens the FIFO; the stamper ends when the child closes it.
#
# The CALLER owns job control: the stamper is launched with `set -m` already
# enabled, so it gets a process group of its own, and the function must NOT
# toggle it — a `set +m` here would leak to the caller and the CHILD
# launched next would stay in the script's own group.
start_stamped_output() {
    local fifo="$1"
    local label="$2"
    local pid_var="$3"
    local stamper_pid=""
    local _stamp_line=""

    mkfifo -- "$fifo" 2>/dev/null || return 1

    (
        # T170: `read` returns a NON-ZERO status at EOF when it has read a
        # PARTIAL line, so the loop body never runs for a child's last line
        # that was written WITHOUT a trailing newline — the residual was
        # silently dropped, and a child killed mid-write lost its last
        # line. The residual is emitted after the loop instead: the loop
        # body only ever runs for a COMPLETE line, so the residual is
        # emitted EXACTLY ONCE (FR-005, FR-014).
        while IFS= read -r _stamp_line; do
            log_event_quiet INFO "${label}: ${_stamp_line}"
        done < "$fifo"
        if [[ -n "$_stamp_line" ]]; then
            log_event_quiet INFO "${label}: ${_stamp_line}"
        fi
    ) 9<&- &
    stamper_pid=$!

    printf -v "$pid_var" '%s' "$stamper_pid"
    return 0
}

# dump_build_output <build-log>
#
# Writes the build output into the run log, one run-log line per build
# line, each carrying the `build:` label.
#
# T170: `read` returns a NON-ZERO status at EOF when it has read a PARTIAL
# line, so the loop body never runs for the build output's last line when
# the build ends WITHOUT a trailing newline — the residual was silently
# dropped. The residual is emitted after the loop instead: the loop body
# only ever runs for a COMPLETE line, so the residual is emitted EXACTLY
# ONCE (FR-014).
dump_build_output() {
    local build_log="$1"
    local _build_line=""

    [[ -s "$build_log" ]] || return 0

    while IFS= read -r _build_line; do
        log_event INFO "build: ${_build_line}"
    done < "$build_log"

    if [[ -n "$_build_line" ]]; then
        log_event INFO "build: ${_build_line}"
    fi

    return 0
}

run_log_init() {
    local state_base="${XDG_STATE_HOME:-$HOME/.local/state}"
    local state_dir="${state_base}/living-roots-smoke"
    local stamp

    stamp="$(date -u +%Y%m%dT%H%M%SZ)"
    run_id="${stamp}-$$"
    run_log_dir="${state_dir}/runs"
    run_log_path="${run_log_dir}/${run_id}.log"

    # These are deliberately NOT wrapped in a `|| die`: a failure must
    # surface through the ERR trap, which names the failing line and the
    # enclosing function (FR-014).
    mkdir -p -- "$run_log_dir"

    chmod 0700 -- "$state_dir" "$run_log_dir" 2>/dev/null || true

    : >"$run_log_path"

    chmod 0600 -- "$run_log_path" 2>/dev/null || true
    run_log_started=1

    run_started_epoch="$(date +%s)"
    log_event INFO "run ${run_id} starting (${SCRIPT_NAME} ${VERSION})"
    log_event INFO "version: ${VERSION}"
    # T161: this field is the RUN's start, NOT the launch. The launch
    # happens later — after the pre-flight, the display, the rebuild and
    # the lock — and its own instant is logged when it happens, in the
    # launch phase. The field used to be labelled `launch timestamp`,
    # which made it a lie: FR-002's freshness control compares the
    # sentinel's timestamp against the LAUNCH epoch, never against this
    # one, so a run reconstructed from the log alone compared the
    # sentinel against the wrong threshold.
    log_event INFO "run started: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
    log_event INFO "game-path: ${game_path}"
    log_event INFO "game-path (canonical): $(canonicalize_existing "$game_path" || printf '%s' "$game_path")"
    log_event INFO "mods-path: ${mods_path}"
    log_event INFO "mod-project: ${mod_project}"
    log_event INFO "smapi-log: ${smapi_log}"
    log_event INFO "timeout: ${timeout_secs}s  build-timeout: ${build_timeout_secs}s  grace-period: ${grace_period_secs}s"
    log_event INFO "force: $( ((force == 1)) && printf 'on' || printf 'off' )"
}

phase_begin() {
    # The clock is read BEFORE the phase is named: a clock that cannot be
    # read fails the phase before it starts, and the phase is then not
    # recorded at all — rather than recorded with a start of 0, which would
    # report a duration of billions of seconds.
    phase_current_start="$(date +%s)"
    phase_current="$1"
    log_event INFO "phase ${phase_current} starting"
}

# close_open_phase
#
# FR-014 / SC-001: EVERY phase's duration is recorded — INCLUDING the phase
# the run failed in. `phase_end` closes a phase on the success path; this
# helper closes the phase that is still OPEN when the run fails, so the
# failed phase appears in the stdout `phase durations:` block and in the run
# log, with its end timestamp and its elapsed seconds.
#
# It is idempotent: a phase that is already closed has an empty
# `phase_current`, and the helper then does nothing. So `die` and the EXIT
# trap can both call it — only the first one has an open phase to close.
close_open_phase() {
    local now elapsed

    [[ -n "$phase_current" ]] || return 0

    now="$(date +%s 2>/dev/null || printf '0')"
    if ((phase_current_start <= 0)); then
        elapsed=0
    else
        elapsed=$((now - phase_current_start))
    fi
    if ((elapsed < 0)); then
        elapsed=0
    fi

    phase_names+=("$phase_current")
    phase_elapsed+=("$elapsed")
    log_event INFO "phase ${phase_current} finished in ${elapsed}s"
    phase_current=""
}

phase_end() {
    close_open_phase
}

print_phase_timings() {
    local i

    for ((i = 0; i < ${#phase_names[@]}; i++)); do
        printf '  %-22s %ss\n' "${phase_names[i]}" "${phase_elapsed[i]}"
    done
}

# ---------------------------------------------------------------------------
# T007 — the lock file.
#
# The path is stable and per-user: a per-run directory would mean two
# concurrent instances lock different inodes and mutual exclusion is zero.
# It is opened with `exec 9<>` (read-write, create-if-absent) and never
# `exec 9>`, which truncates and follows symlinks. The file is never removed:
# the kernel releases the lock when the descriptor closes, whereas removing
# the name detaches it from the locked inode and re-opens the race window.
# ---------------------------------------------------------------------------

lock_path=""

lock_acquire() {
    local state_base="${XDG_STATE_HOME:-$HOME/.local/state}"
    local lock_dir=""

    if [[ -n "${XDG_RUNTIME_DIR:-}" ]]; then
        mkdir -p -- "$XDG_RUNTIME_DIR" 2>/dev/null || true
        lock_path="${XDG_RUNTIME_DIR}/livingroots-smoke.lock"
    else
        lock_dir="${state_base}/livingroots-smoke"
        mkdir -p -- "$lock_dir" 2>/dev/null || die "$EX_INTERNAL_ERROR" \
            "cannot create the lock directory" "$lock_dir" \
            "check that \$XDG_STATE_HOME (or \$HOME/.local/state) is writable"
        chmod 0700 -- "$lock_dir" 2>/dev/null || true
        lock_path="${lock_dir}/livingroots-smoke.lock"
    fi

    # NB: `exec 9>` is forbidden — it truncates the file and follows symlinks.
    #
    # The writability is probed in a GROUP, not with `exec ... 2>/dev/null`:
    # a redirection on `exec` is PERMANENT for the shell, so it would send
    # every later message to /dev/null and the failure report would be lost.
    local lock_probe="${lock_path}.smoke-probe"
    if ! { : >"$lock_probe"; } 2>/dev/null; then
        die "$EX_INTERNAL_ERROR" \
            "cannot create the lock file" \
            "$lock_path" \
            "check that the lock file's directory is writable"
    fi
    rm -f -- "$lock_probe" 2>/dev/null || true

    exec 9<>"$lock_path" || die "$EX_INTERNAL_ERROR" \
        "cannot open the lock file" "$lock_path" \
        "check that the lock file's directory is writable"

    # The mode is enforced on the OPEN DESCRIPTOR: a path-based chmod follows
    # symlinks and could act on an inode other than the one flock holds.
    chmod 0600 /proc/self/fd/9 2>/dev/null || true

    if ! flock -n 9; then
        die "$EX_LOCK_CONTENTION" \
            "another smoke-validation instance already holds the lock" \
            "$lock_path" \
            "wait for the other instance to finish (the kernel releases the lock when it exits, even after a SIGKILL)"
    fi

    log_event INFO "lock acquired: ${lock_path}"
}

# ---------------------------------------------------------------------------
# T008 — the trap design.
#
# The EXIT trap is registered exactly once, at the top level, and is the
# single cleanup path. The INT/TERM/HUP handlers forward the signal to the
# child process groups and then RESET THEIR OWN TRAP AND RE-RAISE, so the
# script dies *from* the signal and the caller observes a genuine 128+N
# rather than a plain exit that is indistinguishable from one.
# ---------------------------------------------------------------------------

game_pid=""
game_pgid=""
xvfb_pid=""
xvfb_pgid=""
# T103: the rebuild's process group and the PID it was launched with.
#
# The build's group was phase-local to `phase_build`, so the cleanup and
# the signal forwarder never signalled it: an interrupted build outlived
# the run and kept writing into the SHARED build output and the deploy
# target, which FR-016 (b) forbids and FR-010 (partial) requires to be
# stopped. The group is published in a script-global of its own — it is
# NOT the Xvfb's group, which T091 publishes on its own path — so the two
# can never be confused with one another.
build_pid=""
build_pgid=""
display_num=""
temp_dir=""
in_cleanup=0
cleanup_done=0
# The output stampers: they read a child's output from a FIFO and write it
# into the run log with the script's own prefix (FR-005 + FR-014).
game_stamper_pid=""
xvfb_stamper_pid=""
# Set when the cleanup path's PGID assertion fails — the run then aborts
# with 13 instead of claiming success (FR-010).
cleanup_pgid_assertion_failed=0
# Set when the script has printed a failure report of its own. The EXIT
# trap uses it so a run that already reported a failure does not get a
# SECOND, contradictory one from the cleanup-failure path (T118).
failure_report_emitted=0

own_pgid() {
    local pgid

    pgid="$(ps -o pgid= -p "$$" 2>/dev/null || printf '')"
    printf '%s' "${pgid//[[:space:]]/}"
}

# The process group the last child_pgid call resolved, and whether that
# group IS the child's own process group.
child_group=""
child_group_is_own=0

# child_pgid <pid> — the process group of a child launched under job control.
#
# Under `set -m` the child's group id EQUALS its pid, so that is the
# fallback when the lookup races the child's exit. The helper WAITS for the
# invariant to settle: the group is set right after the fork, so the very
# first observation can still be the parent's group.
#
# The group is returned in the global `child_group` and the invariant is
# reported in `child_group_is_own` (1 when the group IS the child's own).
# The helper never calls die: it is called from a command substitution, where
# an `exit` would end only the subshell.
child_pgid() {
    local pid="$1"
    local pgid=""
    local i

    child_group=""
    child_group_is_own=0

    for ((i = 0; i < 20; i++)); do
        pgid="$(ps -o pgid= -p "$pid" 2>/dev/null || printf '')"
        pgid="${pgid//[[:space:]]/}"
        if [[ -n "$pgid" && "$pgid" == "$pid" ]]; then
            child_group="$pgid"
            child_group_is_own=1
            return 0
        fi
        sleep 0.05
    done

    # The lookup window expired. The child may have exited in the meantime:
    # it then has no process group any more, and its own pid is the only
    # group that can be signalled.
    pgid="$(ps -o pgid= -p "$pid" 2>/dev/null || printf '')"
    pgid="${pgid//[[:space:]]/}"

    if [[ -z "$pgid" || "$pgid" == "$pid" ]]; then
        child_group="$pid"
        child_group_is_own=1
        return 0
    fi

    # The child is alive and its group is NOT its own pid: the invariant the
    # cleanup's group signal relies on does not hold.
    child_group="$pgid"
    return 0
}

# assert_child_group_is_own <pid> <what>
#
# FR-005: the group signal the cleanup sends is only safe when the child's
# process group IS the child's own pid. A child that landed in another group
# — the script's own group, for instance — MUST NOT be signalled by group,
# and the run aborts with 13 instead of recording a group the script is
# forbidden to signal.
assert_child_group_is_own() {
    local pid="$1"
    local what="$2"

    if ((child_group_is_own == 1)); then
        return 0
    fi

    die "$EX_INTERNAL_ERROR" \
        "the ${what} child's process group is not the child's own pid" \
        "pid ${pid}, process group ${child_group:-unknown}" \
        "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
}

# assert_signallable_group <pgid> <what>
#
# A group signal is only ever sent to a group the script itself created.
# Signalling the script's own group would terminate the script instead of
# the target; signalling group 1 would signal everything.
assert_signallable_group() {
    local pgid="$1"
    local what="$2"
    local own

    own="$(own_pgid)"

    if [[ -z "$pgid" || "$pgid" == "$own" || "$pgid" == "1" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "refusing to signal the process group of ${what} (pgid '${pgid:-none}', own group '${own:-none}')" \
            "${SCRIPT_NAME}" \
            "this is a bug in ${SCRIPT_NAME} — report it; the group assertion is a safety guard"
    fi
}

# signal_group <pgid> <signal> — best effort, never fatal.
signal_group() {
    local pgid="$1"
    local sig="$2"

    [[ -n "$pgid" ]] || return 0
    kill "-${sig}" -- "-${pgid}" 2>/dev/null || true
}

# reap_child <pid> <what> <max-seconds>
#
# FR-005 / T008: a child is waited on with `wait -f "$pid"`. The poll FIRST
# bounds that wait, so a child stuck in uninterruptible sleep can never hang
# the script past the bound — the script must always reach its verdict.
reap_child() {
    local pid="$1"
    local what="$2"
    local max_secs="$3"
    local i status=0

    [[ -n "$pid" ]] || return 0

    for ((i = 0; i < max_secs * 5; i++)); do
        if ! kill -0 "$pid" 2>/dev/null; then
            wait -f "$pid" 2>/dev/null || status=$?
            return "$status"
        fi
        sleep 0.2
    done

    log_event WARN "the ${what} child ${pid} did not terminate within ${max_secs}s — it is left unreaped"
    return 0
}

# terminate_group <pgid> <pid> <what>
#
# SIGTERM, then the grace period, then SIGKILL — and only then.
#
# FR-014: EVERY signal the script sends is logged with the PID it targets
# and the signal itself. The PID is the child the group belongs to; the
# group alone does not name the process that receives the signal (T125).
terminate_group() {
    local pgid="$1"
    local pid="${2:-unknown}"
    local what="$3"
    local deadline

    [[ -n "$pgid" ]] || return 0
    if ! kill -0 -- "-${pgid}" 2>/dev/null; then
        return 0
    fi

    # The cleanup runs inside the EXIT trap, so it must never call die():
    # an unsafe group is logged and skipped instead — and the run is marked
    # so the exit code becomes 13 (FR-010). Signalling the script's own
    # group would terminate the script instead of the target.
    local own
    own="$(own_pgid)"
    if [[ -z "$pgid" || "$pgid" == "$own" || "$pgid" == "1" ]]; then
        log_event ERROR "refusing to signal the ${what} process group '${pgid:-none}' (pid ${pid}, own group '${own:-none}')"
        cleanup_pgid_assertion_failed=1
        return 0
    fi
    log_event INFO "sending SIGTERM to the ${what} process group ${pgid} (pid ${pid})"
    signal_group "$pgid" TERM

    deadline=$(( $(date +%s 2>/dev/null || printf '0') + grace_period_secs ))
    while (( $(date +%s 2>/dev/null || printf '0') < deadline )); do
        if ! kill -0 -- "-${pgid}" 2>/dev/null; then
            log_event INFO "the ${what} process group ${pgid} (pid ${pid}) exited within the grace period"
            return 0
        fi
        sleep 0.2
    done

    log_event WARN "the ${what} process group ${pgid} (pid ${pid}) is still alive after ${grace_period_secs}s — sending SIGKILL to the process group ${pgid} (pid ${pid})"
    signal_group "$pgid" KILL
}

cleanup() {
    local cleanup_started cleanup_elapsed

    ((cleanup_done == 1)) && return 0
    cleanup_done=1
    ((in_cleanup == 1)) && return 0
    in_cleanup=1

    # The clock is read defensively: the cleanup also runs from the EXIT
    # trap, and the EXIT trap also runs when the tool sweep already failed
    # because the host has no `date`.
    cleanup_started="$(date +%s 2>/dev/null || printf '0')"

    if ((run_log_started == 1)); then
        log_event INFO "cleanup starting"
    fi

    # FR-010: every cleanup step MUST be isolated, so one failure cannot
    # prevent the others. The two group kills are guarded like the rest —
    # a failed PGID assertion inside them still turns the run into exit 13
    # (cleanup_pgid_assertion_failed). The child's PID is passed along with
    # the group, so every signal the cleanup sends is logged with the PID it
    # targets as well as with the group (FR-014, T125).
    #
    # T103: the rebuild's group is terminated FIRST. A build that outlived
    # the run keeps writing into the shared build output and the deploy
    # target, which FR-016 (b) forbids — so it is stopped before the other
    # children, and its group is only published while it can still be
    # alive.
    terminate_group "$build_pgid" "${build_pid:-unknown}" "build" || true
    terminate_group "$game_pgid" "${game_pid:-unknown}" "game" || true
    terminate_group "$xvfb_pgid" "${xvfb_pid:-unknown}" "Xvfb" || true

    # FR-005 / T008: the children are waited on with `wait -f "$pid"`. The
    # Xvfb child is reaped here, after its group was signalled.
    reap_child "$xvfb_pid" "Xvfb" "$(( grace_period_secs + 5 ))" || true

    # The output stampers end when their child closes the FIFO. A stamper
    # whose child never started is still blocked on the FIFO's open, so it is
    # terminated here.
    if [[ -n "$game_stamper_pid" ]]; then
        kill -TERM "$game_stamper_pid" 2>/dev/null || true
        reap_child "$game_stamper_pid" "game output stamper" 2 || true
    fi
    if [[ -n "$xvfb_stamper_pid" ]]; then
        kill -TERM "$xvfb_stamper_pid" 2>/dev/null || true
        reap_child "$xvfb_stamper_pid" "Xvfb output stamper" 2 || true
    fi

    # Only the socket of the display THIS script allocated is removed. The
    # server writes no /tmp/.X{n}-lock file under -displayfd, so the script
    # never creates or removes one.
    if [[ -n "$display_num" ]]; then
        rm -f -- "/tmp/.X11-unix/X${display_num}" 2>/dev/null || true
    fi

    if [[ -n "$temp_dir" && -d "$temp_dir" ]]; then
        rm -rf -- "$temp_dir" 2>/dev/null || true
    fi

    # FR-014: the cleanup phase is timed like every other phase.
    cleanup_elapsed=$(( $(date +%s 2>/dev/null || printf '0') - cleanup_started ))
    phase_names+=("cleanup")
    phase_elapsed+=("$cleanup_elapsed")
    if ((run_log_started == 1)); then
        log_event INFO "phase cleanup finished in ${cleanup_elapsed}s"
    fi

    in_cleanup=0
}

# shellcheck disable=SC2329  # invoked by the EXIT trap below, which shellcheck cannot see
on_exit() {
    local rc="$1"
    local stamp
    local report

    trap - EXIT

    # T159: the phase that is still OPEN when the run ends is closed BEFORE
    # the cleanup appends its own entry, so the phase the run failed in
    # appears in the durations and in the run log (FR-014, SC-001).
    close_open_phase

    cleanup
    # FR-010: a failed PGID assertion in the cleanup path is a hard failure
    # — the group was left unsignalled, so the run cannot claim success.
    if ((cleanup_pgid_assertion_failed == 1)); then
        # The reason is already in the run log. It is repeated on stderr when
        # the run has NOT reported a failure of its own: a run that died from
        # a signal has no report of its own, and its cleanup failure must
        # still be visible — otherwise the run exits 13 with no diagnostic at
        # all (T118). A run that already reported a failure keeps its own
        # report as the last thing on stderr, so exactly ONE report is
        # emitted.
        if ((failure_report_emitted == 0)); then
            stamp="$(date -u +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || printf 'unknown') [ERROR] [${run_id:-no-run-id}]"

            # FR-014: EVERY line of the report carries the stamp — the run
            # log holds no unstamped line. The report is built once and
            # emitted IDENTICALLY to stderr and to the run log.
            report="$(
                {
                    printf '%s ERROR: smoke validation failed (exit %s).\n' "$stamp" "$EX_INTERNAL_ERROR"
                    printf '%s   What: the cleanup refused to signal a process group\n' "$stamp"
                    printf '%s   Where: the run log names the refused group\n' "$stamp"
                    printf '%s   Fix: this is a bug in %s — report it with the run log attached\n' "$stamp" "$SCRIPT_NAME"
                }
            )"

            printf '%s\n' "$report" >&2

            # FR-014: the run log holds EVERY line the script emits, so the
            # cleanup's failure report is written into it as well — with the
            # same stamp discipline the other run-log lines use.
            if ((run_log_started == 1)) && [[ -n "$run_log_path" ]]; then
                printf '%s\n' "$report" >>"$run_log_path" 2>/dev/null || true
            fi
        fi
        rc="$EX_INTERNAL_ERROR"
    fi
    exit "$rc"
}

# forward_signal <signal>
#
# Forward to the child groups, then reset the handler and re-raise the
# signal on this script, so the caller sees a genuine signal death (WIFSIGNALED,
# exit 128+N). After `trap - "$sig"` removes the handler, `kill -sig $$`
# delivers the signal to this shell with no handler installed, so the shell
# terminates from the signal rather than exiting with a plain status.
# shellcheck disable=SC2329  # invoked by the INT/TERM/HUP traps below, which shellcheck cannot see
forward_signal() {
    local sig="$1"

    # FR-005: assert each target group before signalling it — signalling the
    # script's own group would terminate the script instead of the target.
    # An EMPTY group means there is nothing to signal (the child was never
    # launched), so it is not an assertion failure.
    #
    # T103: the rebuild's group is asserted and forwarded too. It was
    # phase-local to `phase_build`, so an interrupted build was never
    # signalled and outlived the run, still writing into the shared build
    # output and the deploy target (FR-016 (b)).
    if [[ -n "$build_pgid" ]]; then
        assert_signallable_group "$build_pgid" "build"
    fi
    if [[ -n "$game_pgid" ]]; then
        assert_signallable_group "$game_pgid" "game"
    fi
    if [[ -n "$xvfb_pgid" ]]; then
        assert_signallable_group "$xvfb_pgid" "Xvfb"
    fi

    # FR-005 / FR-014: the forwarded signal is logged BEFORE it is sent, so
    # an interrupted run's log records WHAT the script forwarded to each
    # child group — and not only the cleanup's own SIGTERM to the groups
    # (T121). The PID is named with the group, so the log says which
    # process the signal reaches.
    if [[ -n "$build_pgid" ]]; then
        log_event INFO "forwarding SIG${sig} to the build process group ${build_pgid} (pid ${build_pid:-unknown})"
    fi
    if [[ -n "$game_pgid" ]]; then
        log_event INFO "forwarding SIG${sig} to the game process group ${game_pgid} (pid ${game_pid:-unknown})"
    fi
    if [[ -n "$xvfb_pgid" ]]; then
        log_event INFO "forwarding SIG${sig} to the Xvfb process group ${xvfb_pgid} (pid ${xvfb_pid:-unknown})"
    fi

    signal_group "$build_pgid" "$sig"
    signal_group "$game_pgid" "$sig"
    signal_group "$xvfb_pgid" "$sig"

    trap - "$sig"
    kill "-${sig}" "$$"
    # If the signal was not delivered (e.g. blocked during trap execution),
    # fall through to exit with the signal-based code so the run terminates.
    exit "$((128 + $(kill -l "$sig" 2>/dev/null || printf '0')))"
}

trap 'on_exit "$?"' EXIT
trap 'forward_signal INT' INT
trap 'forward_signal TERM' TERM
trap 'forward_signal HUP' HUP

# ---------------------------------------------------------------------------
# T013 — the pre-flight.
#
# The order is pinned by FR-012 (0): the cheapest and most dependency-free
# checks first, and no check that depends on a tool or a resource before that
# tool or resource has been verified present.
# ---------------------------------------------------------------------------

# Every external command the script invokes. FR-012 item 8: a tool that is
# used but NOT verified is a tool whose absence is silently misclassified —
# a host without `sort`, for example, reports a healthy installation as
# "SMAPI not functional" (exit 2) instead of as a missing host tool
# (exit 9). The list therefore names every external the script runs, not
# only the ones the pipeline's happy path needs.
#
# T163: `sleep` and `mkfifo` were invoked by the script but swept by
# nobody. `sleep` is called in ten places and `mkfifo` creates the output
# FIFO, so a host without them died at the DISPLAY (exit 10) instead of
# failing closed at the sweep (exit 9) — and the operator's remediation
# would have said `unknown-package`.
#
# T157: the list and tool_package_for's `case` are ONE table. Every entry
# of REQUIRED_TOOLS MUST have a case arm, and every arm MUST name the
# package that actually provides the tool on Fedora — a row without an arm
# reports `unknown-package`, and a row with the WRONG package sends the
# operator after a package that does not ship the tool.
REQUIRED_TOOLS=(
    Xvfb xdpyinfo glxinfo flock timeout mktemp stat realpath
    sha256sum date grep dotnet
    ps readlink sed awk cut head tr
    cp cat sort touch mkdir chmod rm dirname basename tail
    sleep mkfifo
)

# tool_package_for <tool> — the package that provides the tool.
#
# The tool sweep's report must name the PACKAGE that provides a missing
# tool, not just the tool itself (FR-012 item 8, quickstart TC-8).
#
# The mapping is a `case` and NOT an associative array: the file has to stay
# parseable by bash 3 (see the note at the top of the file), and
# `declare -A` is a RUNTIME failure on bash 3 — the file would die at the
# declaration, before the command line was ever parsed, and the usage
# reference would be unreachable on a bash-3 host (T169).
tool_package_for() {
    case "$1" in
        Xvfb)      printf '%s\n' 'xorg-x11-server-Xvfb' ;;
        xdpyinfo)  printf '%s\n' 'mesa-utils' ;;
        glxinfo)   printf '%s\n' 'mesa-utils' ;;
        flock)     printf '%s\n' 'util-linux' ;;
        timeout)   printf '%s\n' 'coreutils' ;;
        mktemp)    printf '%s\n' 'coreutils' ;;
        stat)      printf '%s\n' 'coreutils' ;;
        realpath)  printf '%s\n' 'coreutils' ;;
        sha256sum) printf '%s\n' 'coreutils' ;;
        date)      printf '%s\n' 'coreutils' ;;
        grep)      printf '%s\n' 'grep' ;;
        dotnet)    printf '%s\n' 'dotnet-sdk' ;;
        ps)        printf '%s\n' 'procps-ng' ;;
        readlink)  printf '%s\n' 'coreutils' ;;
        sed)       printf '%s\n' 'sed' ;;
        awk)       printf '%s\n' 'gawk' ;;
        cut)       printf '%s\n' 'coreutils' ;;
        head)      printf '%s\n' 'coreutils' ;;
        tr)        printf '%s\n' 'coreutils' ;;
        cp)        printf '%s\n' 'coreutils' ;;
        cat)       printf '%s\n' 'coreutils' ;;
        sort)      printf '%s\n' 'coreutils' ;;
        touch)     printf '%s\n' 'coreutils' ;;
        mkdir)     printf '%s\n' 'coreutils' ;;
        chmod)     printf '%s\n' 'coreutils' ;;
        rm)        printf '%s\n' 'coreutils' ;;
        dirname)   printf '%s\n' 'coreutils' ;;
        basename)  printf '%s\n' 'coreutils' ;;
        tail)      printf '%s\n' 'coreutils' ;;
        sleep)     printf '%s\n' 'coreutils' ;;
        mkfifo)    printf '%s\n' 'coreutils' ;;
        *)         printf '%s\n' 'unknown-package' ;;
    esac
}

# preflight_tool_sweep
#
# FR-012 item 8: every required host tool is verified with `command -v`, and
# a missing one fails closed with exit 9 naming the tool.
#
# The sweep runs BEFORE the run log is created, because the run log's own
# creation uses several of the tools (date, mkdir, chmod). A sweep that ran
# afterwards would report a missing `date` as the shell's own 127 and a
# missing `mkdir` as an internal error (13) — a missing host tool must be
# reported as a missing host tool.
preflight_tool_sweep() {
    local tool missing="" packages=""

    for tool in "${REQUIRED_TOOLS[@]}"; do
        if ! command -v "$tool" >/dev/null 2>&1; then
            missing="${missing} ${tool}"
            packages="${packages} ${tool}($(tool_package_for "$tool"))"
        fi
    done

    if [[ -n "$missing" ]]; then
        die "$EX_MISSING_DEPENDENCY" \
            "required host tool(s) not found:${missing}" \
            "PATH lookup (command -v)" \
            "install the missing package(s):${packages} — on Fedora: xorg-x11-server-Xvfb, mesa-utils, util-linux, procps-ng, coreutils, grep, gawk, dotnet-sdk"
    fi

    # `pgrep` or `pidof` is acceptable; the spec allows either.
    #
    # T167: the two tools are used for ONE thing only — the pre-flight's
    # detection of an ORPHANED Xvfb left behind by an earlier run (FR-012
    # item 10). The game itself is NEVER found by process name: the
    # kernel's `comm` is truncated to 15 characters, so an exact match on
    # the 17-character apphost name can never succeed, and the run scans
    # `/proc/<pid>/exe` instead (FR-011). The old message named the game
    # detection, which sent the operator to install procps for a check the
    # script does not perform that way.
    if ! command -v pgrep >/dev/null 2>&1 && ! command -v pidof >/dev/null 2>&1; then
        die "$EX_MISSING_DEPENDENCY" \
            "neither pgrep nor pidof is available to detect an orphaned Xvfb instance" \
            "PATH lookup (command -v)" \
            "install procps-ng (Fedora) or psmisc — the tools detect an Xvfb left behind by an earlier run (the game itself is found through a /proc/<pid>/exe scan, not by process name)"
    fi
}

# preflight_tools
#
# The pre-flight's own runtime checks: the bash version and the Xvfb
# runnability probe. Both are kept AFTER the run log exists, because they
# report into it.
preflight_tools() {
    # FR-012 item 8: the spec requires bash 4+, and the script uses
    # `wait -f` and the other bash-4 features older bash does not provide.
    #
    # T169: the check lives HERE, in the pre-flight, and not at the top of
    # the file: the usage reference and the version must stay printable on a
    # host whose bash is too old to run the pipeline (FR-015). The command
    # line is therefore parsed BEFORE the requirement is enforced, and the
    # file itself stays parseable by bash 3.
    if ((BASH_VERSINFO[0] < 4)); then
        die "$EX_MISSING_DEPENDENCY" \
            "bash 4 or newer is required (found ${BASH_VERSION})" \
            "bash version" \
            "install a newer bash — the script needs bash 4+ for 'wait -f' and the other bash-4 features it uses"
    fi

    # FR-012 item 3: Xvfb is present AND runnable. The probe EXECUTES the
    # binary — a wrong-architecture or otherwise unusable binary fails with
    # 126/127, which is a missing host tool (9), not an unusable graphics
    # stack (3). Not every Xvfb build accepts `-version`, so the probe only
    # asserts that the binary runs and answers.
    local xvfb_probe_output="" xvfb_probe_status=0
    xvfb_probe_output="$(Xvfb -version 2>&1)" || xvfb_probe_status=$?
    if ((xvfb_probe_status >= 126)); then
        die "$EX_MISSING_DEPENDENCY" \
            "Xvfb is present but cannot be executed (status ${xvfb_probe_status})" \
            "Xvfb -version" \
            "reinstall xorg-x11-server-Xvfb for this architecture — the binary on the PATH cannot be run"
    fi
    log_event INFO "Xvfb version probe: ${xvfb_probe_output%%$'\n'*}"
}

# smapi_assembly_version <dll> — the assembly's version, or failure.
#
# The file must be a .NET PE image (the MZ magic) and must carry a version
# string. The .NET runtime moniker and the default assembly version are not
# SMAPI's product version, so they are skipped.
smapi_assembly_version() {
    local dll="$1"
    local version

    if ! grep -aqoE '^MZ' -- "$dll" 2>/dev/null; then
        return 1
    fi

    while read -r version; do
        [[ -n "$version" ]] || continue
        case "$version" in
            4.0.30319 | 1.0.0.0 | 1.0.0 | 0.0.0.0) continue ;;
        esac
        printf '%s' "$version"
        return 0
    done < <(grep -aoE '[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?' -- "$dll" 2>/dev/null | sort -u)

    return 1
}

preflight_game() {
    local marker
    local smapi_version
    local real_game real_marker

    if [[ ! -d "$game_path" ]]; then
        die "$EX_GAME_NOT_FOUND" \
            "the game directory does not exist" \
            "$game_path" \
            "pass --game-path <dir> pointing at the Stardew Valley installation"
    fi

    marker="${game_path}/Stardew Valley.dll"
    if [[ ! -f "$marker" ]]; then
        die "$EX_GAME_NOT_FOUND" \
            "the game assembly 'Stardew Valley.dll' is missing" \
            "$marker" \
            "pass --game-path <dir> pointing at the Stardew Valley installation"
    fi

    # The game directory is the base for every path the script CONSTRUCTS
    # inside it, so a constructed path that resolves outside the base is a
    # refused installation rather than a file to read.
    real_game="$(canonicalize_existing "$game_path" || printf '%s' "${game_path%/}")"

    for marker in \
        "${game_path}/StardewModdingAPI" \
        "${game_path}/StardewModdingAPI.dll" \
        "${game_path}/smapi-internal/SMAPI.Toolkit.dll" \
        "${game_path}/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll"; do
        if [[ -e "$marker" ]]; then
            real_marker="$(canonicalize_existing "$marker" || printf '')"
            if [[ -n "$real_marker" ]] && ! path_under_base "$real_marker" "$real_game"; then
                die "$EX_SMAPI_NOT_FOUND" \
                    "the SMAPI installation is not contained in the game directory" \
                    "$marker (resolves to ${real_marker})" \
                    "the path resolves outside the game directory — re-run the SMAPI installer so its assemblies live inside it"
            fi
        fi

        if [[ ! -f "$marker" ]]; then
            die "$EX_SMAPI_NOT_FOUND" \
                "the SMAPI marker file is missing" \
                "$marker" \
                "re-run the SMAPI installer in the game directory"
        fi
    done

    # The game's own assembly is only checked for presence: the user may
    # legitimately keep it anywhere, and the script never writes to it.
    if [[ ! -f "${game_path}/Stardew Valley.dll" ]]; then
        die "$EX_GAME_NOT_FOUND" \
            "the game assembly 'Stardew Valley.dll' is missing" \
            "${game_path}/Stardew Valley.dll" \
            "pass --game-path <dir> pointing at the Stardew Valley installation"
    fi

    if [[ ! -x "${game_path}/StardewModdingAPI" ]]; then
        die "$EX_SMAPI_NOT_FOUND" \
            "the SMAPI launcher is not executable" \
            "${game_path}/StardewModdingAPI" \
            "chmod +x the launcher, or re-run the SMAPI installer"
    fi

    # FR-012 item 2 requires the main assembly's version to be parseable. The
    # assembly is a .NET PE file, so it must carry the MZ magic and a
    # version string; both are read with grep alone, so no new dependency is
    # introduced. The well-known .NET constants are not SMAPI's version.
    if ! smapi_version="$(smapi_assembly_version "${game_path}/StardewModdingAPI.dll")"; then
        die "$EX_SMAPI_NOT_FOUND" \
            "the SMAPI assembly is not a readable .NET assembly, so its version cannot be parsed" \
            "${game_path}/StardewModdingAPI.dll" \
            "re-run the SMAPI installer in the game directory"
    fi
    log_event INFO "SMAPI assembly version: ${smapi_version}"
}

preflight_mods_dir() {
    local real_mods

    if [[ ! -d "$mods_path" ]]; then
        die "$EX_MOD_MANIFEST_INVALID" \
            "the resolved mods directory does not exist" \
            "$mods_path" \
            "create it, or pass --mods-path <dir> pointing at the directory SMAPI should load mods from"
    fi

    if [[ ! -w "$mods_path" ]]; then
        die "$EX_MOD_MANIFEST_INVALID" \
            "the resolved mods directory is not writable" \
            "$mods_path" \
            "make the directory writable — the build deploys the mod into it"
    fi

    real_mods="$(canonicalize_existing "$mods_path" || printf '%s' "$mods_path")"
    if [[ "$real_mods" != "${mods_path%/}" ]]; then
        mods_path="$real_mods"
    fi
}

preflight_steam() {
    local steam_file="${game_path}/steam_appid.txt"
    local observed=""

    if [[ ! -f "$steam_file" ]]; then
        die "$EX_STEAM_PREREQ_MISSING" \
            "steam_appid.txt is missing" \
            "$steam_file" \
            "restore steam_appid.txt (content: 413150) in the game directory"
    fi

    observed="$(<"$steam_file")"
    observed="${observed//[[:space:]]/}"

    if [[ "$observed" != "413150" ]]; then
        die "$EX_STEAM_PREREQ_MISSING" \
            "steam_appid.txt does not contain the Stardew Valley app id" \
            "${steam_file} (expected 413150, found '${observed:-empty}')" \
            "restore steam_appid.txt (content: 413150) in the game directory"
    fi
}

# preflight_mod_project
#
# FR-016 (a): the mod project MUST be resolved with `realpath` and MUST
# satisfy the component-boundary prefix test of FR-012 item 7 against the
# CANONICALIZED repository root. A project outside the repository is
# rejected before the build, and a project that does not exist is rejected
# too — `realpath -e` requires the path to exist.
#
# The RESOLVED path is stored back into `mod_project`, so the build receives
# the canonical path and not the spelling the caller typed.
preflight_mod_project() {
    local repo_real project_real

    repo_real="$(canonicalize_existing "$REPO_ROOT" || printf '')"
    if [[ -z "$repo_real" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "the repository root could not be resolved" \
            "$REPO_ROOT" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi

    project_real="$(canonicalize_existing "$mod_project" || printf '')"
    if [[ -z "$project_real" ]]; then
        die "$EX_BUILD_ARTIFACT_FAILURE" \
            "the mod project does not exist" \
            "$mod_project" \
            "pass --mod-project <file> pointing at the .csproj to rebuild — the file must exist"
    fi

    if ! path_under_base "$project_real" "$repo_real"; then
        die "$EX_BUILD_ARTIFACT_FAILURE" \
            "the mod project is not inside the repository" \
            "${mod_project} (resolves to ${project_real})" \
            "the mod project must live inside the repository (${repo_real}) — pass --mod-project <file> pointing at a project of this repository"
    fi

    mod_project="$project_real"
    log_event INFO "mod project resolved: ${mod_project} (repository root ${repo_real})"
}

# find_conflicting_pids <canonical game dir>
#
# FR-011: a candidate qualifies only when /proc/<pid>/exe resolves to an
# executable UNDER the resolved game directory. The process NAME is never
# used: the kernel's comm is truncated to 15 characters, so an exact match on
# the 17-character apphost name can never succeed.
find_conflicting_pids() {
    local game_dir="${1-}"
    local pid_dir pid exe

    for pid_dir in /proc/[0-9]*; do
        pid="${pid_dir#/proc/}"
        [[ "$pid" == "$$" ]] && continue
        exe="$(readlink -f "$pid_dir/exe" 2>/dev/null || printf '')"
        [[ -n "$exe" ]] || continue
        if path_under_base "$exe" "$game_dir"; then
            printf '%s\n' "$pid"
        fi
    done
}

# preflight_process_conflict <canonical game dir>
#
# Without --force the run aborts with 7; with --force the identified
# processes are terminated. The signal goes to the IDENTIFIED PID only,
# never to a process group this script did not create — a game started from
# the user's shell shares that shell's group, and a group signal would kill
# the user's shell.
preflight_process_conflict() {
    local game_dir="${1-}"
    local pids pid deadline

    pids="$(find_conflicting_pids "$game_dir")"
    [[ -n "$pids" ]] || return 0

    if ((force == 0)); then
        die "$EX_PROCESS_CONFLICT" \
            "a Stardew Valley/SMAPI process is already running" \
            "$(printf '%s' "$pids" | tr '\n' ' ')" \
            "close the running game, or re-run with --force"
    fi

    for pid in $pids; do
        # FR-011 / the Scope's declared side effects: the evidence needed to
        # reconstruct WHICH process was terminated is recorded BEFORE the
        # signal — the executable path and the process start time.
        log_event WARN "--force: terminating the conflicting process ${pid} (exe=$(readlink -f "/proc/${pid}/exe" 2>/dev/null || printf 'unknown'), starttime=$(awk '{print $22}' "/proc/${pid}/stat" 2>/dev/null || printf 'unknown'))"
        kill -TERM -- "$pid" 2>/dev/null || true
    done

    deadline=$(( $(date +%s) + grace_period_secs ))
    while (( $(date +%s) < deadline )); do
        pids="$(find_conflicting_pids "$game_dir")"
        [[ -n "$pids" ]] || break
        sleep 0.2
    done

    for pid in $pids; do
        log_event WARN "--force: the conflicting process ${pid} is still alive — sending SIGKILL (exe=$(readlink -f "/proc/${pid}/exe" 2>/dev/null || printf 'unknown'), starttime=$(awk '{print $22}' "/proc/${pid}/stat" 2>/dev/null || printf 'unknown'))"
        kill -KILL -- "$pid" 2>/dev/null || true
    done
}

# The canonicalised SMAPI log root and the canonical directory that holds
# it.
#
# T120: the `--smapi-log` root is a user-supplied root and therefore its OWN
# base (FR-012 item 7) — it is never prefix-tested against the game
# directory. It is canonicalised, and the directory that holds it becomes
# the base against which every path the script CONSTRUCTS inside it is
# prefix-tested, exactly as the deploy target and the SMAPI markers are
# tested.
smapi_log_real=""
smapi_log_dir_real=""

# resolve_smapi_log_paths
#
# T120 / FR-012 item 7: the SMAPI log root is canonicalised and the paths
# constructed inside it are built from the CANONICAL spelling.
#
# The log FILE need not exist yet — SMAPI creates it — so it is canonicalised
# with realpath -m. The canonical form REPLACES the user-supplied spelling,
# so the removal, the read and the freshness comparison all refer to the
# same file.
#
# The tilde-anonymisation rule of FR-002's header region is untouched by
# this: it concerns the MODS path SMAPI prints (`Mods go here:`), which is
# canonicalised by its own pre-flight step, not this one.
resolve_smapi_log_paths() {
    local dir_real=""

    smapi_log_real=""
    smapi_log_dir_real=""

    dir_real="$(canonicalize_maybe_missing "$(dirname -- "$smapi_log")" || printf '')"
    if [[ -z "$dir_real" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "the SMAPI log directory could not be resolved" \
            "$smapi_log" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi

    # The base must not carry a trailing slash: a base that keeps one yields
    # ".../ErrorLogs//*", which never matches (FR-012 item 7).
    smapi_log_dir_real="${dir_real%/}"
    if [[ -z "$smapi_log_dir_real" ]]; then
        smapi_log_dir_real="/"
    fi

    smapi_log_real="$(canonicalize_maybe_missing "$smapi_log" || printf '')"
    if [[ -z "$smapi_log_real" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "the SMAPI log path could not be resolved" \
            "$smapi_log" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi

    # The log file must resolve INSIDE the log directory: the script
    # REMOVES it, and the removal is applied to the CANONICAL path — a log
    # file that is a symlink to a file outside the directory would otherwise
    # be removed by its resolved name. The refusal is the containment test
    # FR-012 item 7 requires of every constructed path.
    if ! path_under_base "$smapi_log_real" "$smapi_log_dir_real"; then
        die "$EX_INTERNAL_ERROR" \
            "the SMAPI log does not resolve inside its base" \
            "${smapi_log} (resolves to ${smapi_log_real})" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi

    smapi_log="$smapi_log_real"
    log_event INFO "smapi log resolved: ${smapi_log} (directory ${smapi_log_dir_real})"
}

# preflight_crash_markers
#
# FR-012 (0) (d): the crash-marker removal is part of the pre-flight, which
# runs BEFORE the build-and-verify step. The stale SMAPI log removal (the
# log baseline) is a different step and runs AFTER the build — see
# preflight_log_baseline.
#
# The update marker's CONTENT is logged together with its path, so nothing
# is silently discarded (FR-012 item 6).
#
# T100: every removal is ATTEMPTED and its result is the evidence. The
# existence of a crash artifact is never a gate: FR-012 item 7's
# open-without-gating clause forbids deciding whether an operation is
# attempted from a test of the same path. A removal that FAILED is
# reported — an artifact that survived the pre-flight is exactly what makes
# the next run fail identically, so a silently skipped removal produces a
# misleading verdict.
preflight_crash_markers() {
    local log_dir marker removed="" marker_content=""
    local game_real internal_real marker_real
    local crash_file crash_present=0 crash_status=0 crash_error=""

    # T120: the log root is canonicalised first, and the paths constructed
    # inside it are built from the canonical directory.
    resolve_smapi_log_paths
    log_dir="$smapi_log_dir_real"

    # The stale crash file in the log directory. SMAPI aborts at startup
    # when it cannot purge the previous crash file, so a removal that failed
    # is reported instead of being discarded.
    crash_file="${log_dir}/SMAPI-crash.txt"
    if ! check_mutating_path_under_base "$crash_file" "$smapi_log_dir_real"; then
        die "$EX_INTERNAL_ERROR" \
            "the stale SMAPI crash file does not resolve inside its base" \
            "${crash_file} (resolves to ${mutating_path_real:-unresolvable})" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi
    crash_file="$mutating_path_real"

    # The existence test below is a REPORTING aid only — it never decides
    # whether the removal is attempted.
    if [[ -e "$crash_file" ]]; then
        crash_present=1
    fi

    crash_error="$(rm -f -- "$crash_file" 2>&1)" || crash_status=$?
    if ((crash_status != 0)); then
        die "$EX_INTERNAL_ERROR" \
            "the stale SMAPI crash file could not be removed" \
            "${crash_file} (rm exited with status ${crash_status})" \
            "the crash file must be removable — SMAPI aborts at startup when it cannot purge it" \
            "the removal failed: ${crash_error}"
    fi

    if ((crash_present == 1)); then
        removed="${removed} ${crash_file}"
    fi

    # FR-012 item 7: the markers are MUTATING paths — the script deletes
    # them. They are canonicalised with realpath -m (they need not exist)
    # and prefix-tested against the canonicalized game directory with the
    # component-boundary test.
    game_real="$(canonicalize_existing "$game_path" || printf '%s' "$game_path")"
    internal_real="$(canonicalize_maybe_missing "${game_real}/smapi-internal" || printf '')"
    if [[ -z "$internal_real" ]]; then
        internal_real="${game_real}/smapi-internal"
    fi

    for marker in \
        "${internal_real}/StardewModdingAPI.crash.marker" \
        "${internal_real}/StardewModdingAPI.update.marker"; do
        if ! check_mutating_path_under_base "$marker" "$game_real"; then
            die "$EX_INTERNAL_ERROR" \
                "the SMAPI marker does not resolve inside its base" \
                "${marker} (resolves to ${mutating_path_real:-unresolvable})" \
                "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
        fi
        marker_real="$mutating_path_real"

        # T100: the read and the removal are both ATTEMPTED, and their
        # results decide what is reported. The marker's presence is never a
        # gate — a marker that could not be read is still removed, and a
        # marker that could not be removed is reported instead of being
        # skipped into a misleading verdict.
        local marker_present=0 marker_read_status=0 marker_remove_status=0
        local marker_remove_error=""

        # The existence test below is a REPORTING aid only — it never
        # decides whether the read or the removal is attempted.
        if [[ -e "$marker_real" ]]; then
            marker_present=1
        fi

        # Read the content BEFORE removing it — the content is evidence of
        # what SMAPI wanted to tell the user.
        marker_content="$(cat -- "$marker_real" 2>/dev/null)" || marker_read_status=$?

        if ((marker_present == 1)); then
            if ((marker_read_status == 0)); then
                if [[ -n "$marker_content" ]]; then
                    log_event INFO "marker content (${marker_real}): ${marker_content}"
                else
                    log_event INFO "marker content (${marker_real}): <empty>"
                fi
            else
                log_event WARN "marker content (${marker_real}): <unavailable — the marker could not be read>"
            fi
        fi

        marker_remove_error="$(rm -f -- "$marker_real" 2>&1)" || marker_remove_status=$?
        if ((marker_remove_status != 0)); then
            die "$EX_INTERNAL_ERROR" \
                "the leftover SMAPI marker could not be removed" \
                "${marker_real} (rm exited with status ${marker_remove_status})" \
                "the marker must be removable — SMAPI aborts at startup when it finds one" \
                "the removal failed: ${marker_remove_error}"
        fi

        if ((marker_present == 1)); then
            removed="${removed} ${marker_real}"
            log_event INFO "removed leftover SMAPI marker: ${marker_real}"
        fi
    done

    if [[ -n "$removed" ]]; then
        log_event INFO "removed stale crash artifacts:${removed}"
    fi
}

# The inode of the SMAPI log as it was BEFORE the stale log was removed, and
# the inode observed once the log was recreated. FR-002: the baseline MUST
# be recorded before the removal, because recording it afterwards would make
# the baseline "absent" and the comparison vacuous.
smapi_log_baseline_inode=""
smapi_log_recreated_inode=""

# preflight_log_baseline
#
# FR-012 (0): the log baseline runs AFTER the build-and-verify step, the flock
# and the existing-process check, and immediately BEFORE the launch. It is
# what makes a stale log unable to decide the verdict (FR-002).
#
# T100 / T120: the baseline inode is read by ATTEMPTING the read, the stale
# log is removed by ATTEMPTING the removal, and both outcomes are branched
# on — the log's existence is never a gate. The paths are built from the
# CANONICAL log root and prefix-tested against it.
preflight_log_baseline() {
    local log_dir removed=""
    local stale_status=0 stale_error=""
    local probe probe_status=0 probe_error=""

    if [[ -z "$smapi_log_dir_real" ]]; then
        resolve_smapi_log_paths
    fi
    log_dir="$smapi_log_dir_real"

    # The stale log is constructed inside the canonical log directory, so it
    # is prefix-tested against it like every other constructed path.
    if ! check_mutating_path_under_base "$smapi_log" "$smapi_log_dir_real"; then
        die "$EX_INTERNAL_ERROR" \
            "the SMAPI log does not resolve inside its base" \
            "${smapi_log} (resolves to ${mutating_path_real:-unresolvable})" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi
    smapi_log="$mutating_path_real"

    # Record the inode of the existing SMAPI log before removal so we can
    # verify the removal actually took effect (the inode changes when the
    # file is unlinked and a new one is created, or the file is simply gone).
    # The read is ATTEMPTED: a failed read means there was no stale log, and
    # the empty baseline is what FR-002's comparison expects in that case.
    smapi_log_baseline_inode="$(stat -c %i -- "$smapi_log" 2>/dev/null || printf '')"
    if [[ -n "$smapi_log_baseline_inode" ]]; then
        log_event INFO "stale SMAPI log baseline inode: ${smapi_log_baseline_inode}"
        removed="${removed} ${smapi_log}"
    fi

    # The removal is ATTEMPTED and its result is the evidence.
    stale_error="$(rm -f -- "$smapi_log" 2>&1)" || stale_status=$?
    if ((stale_status != 0)); then
        die "$EX_INTERNAL_ERROR" \
            "the stale SMAPI log could not be removed" \
            "${smapi_log} (rm exited with status ${stale_status})" \
            "check that the log directory is writable and that no process holds the file open" \
            "the removal failed: ${stale_error}"
    fi

    # Assert the stale log is absent immediately after removal. A failed
    # removal means a stale log could still decide the verdict (false pass).
    if [[ -f "$smapi_log" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "could not remove the stale SMAPI log" \
            "$smapi_log" \
            "check that the log directory is writable and that no process holds the file open"
    fi

    if [[ -n "$removed" ]]; then
        log_event INFO "removed stale artifacts:${removed}"
    fi

    # SMAPI creates its own log directory, so the script may too — and it
    # must, because the directory is where the log it is about to read lives.
    mkdir -p -- "$log_dir" 2>/dev/null || true

    # T120: the write probe is constructed inside the canonical log
    # directory and prefix-tested against it.
    probe="${log_dir}/.smoke-validation-write-probe"
    if ! check_mutating_path_under_base "$probe" "$smapi_log_dir_real"; then
        die "$EX_INTERNAL_ERROR" \
            "the SMAPI log write probe does not resolve inside its base" \
            "${probe} (resolves to ${mutating_path_real:-unresolvable})" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi
    probe="$mutating_path_real"

    if ! touch -- "$probe" 2>/dev/null; then
        die "$EX_INTERNAL_ERROR" \
            "the SMAPI log directory is not writable" \
            "$log_dir" \
            "make the directory writable — SMAPI aborts at startup if it cannot write its log"
    fi

    # The probe is transient and MUST NOT survive the run, so its removal is
    # attempted and its result is reported rather than discarded.
    probe_error="$(rm -f -- "$probe" 2>&1)" || probe_status=$?
    if ((probe_status != 0)); then
        log_event WARN "the SMAPI log write probe could not be removed: ${probe} (rm exited with status ${probe_status})"
        log_event WARN "the write probe's removal failed: ${probe_error}"
    fi
}

# find_xvfb_pids — the PIDs of the Xvfb processes running on the host.
#
# FR-012 item 8 accepts EITHER `pgrep` OR `pidof`, and item 10 requires the
# detection to work: a scan that uses `pgrep` alone reports nothing at all
# on a `pidof`-only host. Both tools are therefore consulted, and their
# results are merged into one PID per line.
find_xvfb_pids() {
    local raw=""

    if command -v pgrep >/dev/null 2>&1; then
        raw="${raw} $(pgrep -x Xvfb 2>/dev/null || true)"
    fi

    if command -v pidof >/dev/null 2>&1; then
        raw="${raw} $(pidof Xvfb 2>/dev/null || true)"
    fi

    printf '%s\n' "$raw" | tr ' ' '\n' | grep -E '^[0-9]+$' | sort -u || true
}

# preflight_orphan_displays
#
# FR-012 (10): orphaned Xvfb instances are DETECTED AND REPORTED, never
# signalled. Under -displayfd the server writes no lock file, so there is no
# way to prove a foreign X server is this script's own orphan, and a foreign
# Xvfb cannot satisfy FR-011's identity rule.
preflight_orphan_displays() {
    local pid exe start owner

    while read -r pid; do
        [[ -n "$pid" ]] || continue
        exe="$(readlink -f "/proc/${pid}/exe" 2>/dev/null || printf 'unknown')"
        start="$(awk '{print $22}' "/proc/${pid}/stat" 2>/dev/null || printf 'unknown')"
        owner="$(stat -c %u "/proc/${pid}" 2>/dev/null || printf 'unknown')"
        log_event WARN "an Xvfb process is already running: pid=${pid} exe=${exe} starttime=${start} uid=${owner} — it is only reported, never signalled"
    done < <(find_xvfb_pids)
}

phase_preflight() {
    phase_begin "pre-flight"

    preflight_tools
    validate_own_literals
    preflight_game
    preflight_mods_dir
    preflight_steam
    preflight_mod_project
    preflight_crash_markers
    preflight_orphan_displays

    phase_end
}

# phase_lock
#
# FR-012 (0): the flock is acquired AFTER the build-and-verify step, so the
# build is not serialised behind another instance's whole run.
phase_lock() {
    phase_begin "lock"

    lock_acquire

    phase_end
}

# phase_conflict_check
#
# FR-012 (0): the existing-process check runs after the flock (so it is
# serialised) and after the build-and-verify step.
phase_conflict_check() {
    phase_begin "process-conflict-check"

    preflight_process_conflict "$(canonicalize_existing "$game_path" || printf '%s' "$game_path")"

    phase_end
}

# phase_log_baseline
#
# FR-012 (0): the log baseline runs after the flock and the existing-process
# check, immediately before the launch.
phase_log_baseline() {
    phase_begin "log-baseline"

    preflight_log_baseline

    phase_end
}

# ---------------------------------------------------------------------------
# T014 — the virtual display and the software-OpenGL probe.
#
# Xvfb is started with -displayfd so the SERVER allocates the display and
# writes the number back; the script never chooses a display number and
# never creates, probes or removes a /tmp/.X{n}-lock file (the server writes
# none in this mode).
# ---------------------------------------------------------------------------

display_start() {
    local fd_file="${temp_dir}/display-number"
    local deadline

    : >"$fd_file" || die "$EX_DISPLAY_UNAVAILABLE" \
        "cannot create the display-number file" "$fd_file" "check that \$TMPDIR is writable"

    # `set -m` around the launch only: a background child of a
    # non-interactive shell gets NO process group of its own otherwise, and
    # the cleanup's group signal would then hit the script's own group.
    set -m
    # 9<&- closes the lock descriptor in the child: a child that inherited it
    # would keep the lock held after the script exits.
    # stdin comes from /dev/null (FR-005 / T008: a background job that
    # touches the controlling terminal is suspended by SIGTTIN/SIGTTOU), and
    # the output goes to the run log THROUGH the stamper, so every line
    # there carries the timestamp, the level and the run ID (FR-014).
    start_stamped_output "${temp_dir}/xvfb-output.fifo" "Xvfb" xvfb_stamper_pid \
        || die "$EX_DISPLAY_UNAVAILABLE" \
            "cannot create the Xvfb output pipe" "${temp_dir}" \
            "check that \$TMPDIR is writable"
    Xvfb -nolisten tcp -displayfd 3 3>"$fd_file" </dev/null >"${temp_dir}/xvfb-output.fifo" 2>&1 9<&- &
    xvfb_pid=$!
    set +m

    # T091: the process group is resolved and PUBLISHED HERE — before the
    # display-number wait and before the process-group assertion. Both of
    # those paths can end the run (exit 10 and exit 13), and the group is
    # what the cleanup signals: a group published only after them leaves the
    # X server un-signalled, which is exactly the orphan the cleanup exists
    # to prevent (FR-010). The publication therefore precedes every `die`
    # on this path.
    child_pgid "$xvfb_pid"
    xvfb_pgid="$child_group"
    assert_child_group_is_own "$xvfb_pid" "Xvfb"

    deadline=$(( $(date +%s) + 15 ))
    while (( $(date +%s) < deadline )); do
        display_num="$(<"$fd_file")"
        display_num="${display_num//[[:space:]]/}"
        if [[ -n "$display_num" ]]; then
            break
        fi
        if ! kill -0 "$xvfb_pid" 2>/dev/null; then
            break
        fi
        sleep 0.1
    done

    # T171: the deadline test is at the TOP of the loop, so the file is NOT
    # read again once the loop exits: a number written between the last
    # read and the deadline check is lost, and a live X server is reported
    # as "could not be allocated". The file is read ONE more time here,
    # BEFORE the emptiness test, so the last ~100ms of the wait cannot
    # swallow the number the server allocated (FR-012 item 5).
    display_num="$(<"$fd_file")"
    display_num="${display_num//[[:space:]]/}"

    if [[ -z "${display_num:-}" ]]; then
        log_event ERROR "Xvfb did not report a display number (pid ${xvfb_pid})"
        die "$EX_DISPLAY_UNAVAILABLE" \
            "the virtual display could not be allocated" \
            "Xvfb -nolisten tcp -displayfd" \
            "check that Xvfb starts on this host and that the graphics stack is usable (mesa-utils provides glxinfo and xdpyinfo)"
    fi

    export DISPLAY=":${display_num}"
    log_event INFO "virtual display command: Xvfb -nolisten tcp -displayfd 3"
    log_event INFO "virtual display allocated: ${DISPLAY} (Xvfb pid ${xvfb_pid}, group ${xvfb_pgid:-unknown})"
}

display_probe_graphics() {
    local renderer extensions version

    if ! xdpyinfo >/dev/null 2>&1; then
        die "$EX_GRAPHICS_UNUSABLE" \
            "the X display does not answer xdpyinfo" \
            "DISPLAY=${DISPLAY}" \
            "check the Mesa installation (mesa-utils provides xdpyinfo)"
    fi

    local glx="" randr=""
    glx="$(xdpyinfo 2>/dev/null | grep -c 'GLX' || true)"
    randr="$(xdpyinfo 2>/dev/null | grep -c 'RANDR' || true)"
    if [[ "$glx" -lt 1 || "$randr" -lt 1 ]]; then
        die "$EX_GRAPHICS_UNUSABLE" \
            "the X display does not advertise the GLX and RANDR extensions" \
            "DISPLAY=${DISPLAY} (GLX=${glx}, RANDR=${randr})" \
            "check the Mesa installation (mesa-utils and mesa-dri-drivers)"
    fi

    # glxinfo exits 255 on failure — branch on "exit 0", never on a status.
    if ! LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe glxinfo -B >/dev/null 2>&1; then
        die "$EX_GRAPHICS_UNUSABLE" \
            "the software OpenGL probe failed" \
            "DISPLAY=${DISPLAY}" \
            "install Mesa's software rasteriser (mesa-dri-drivers) and confirm 'glxinfo -B' exits 0 under LIBGL_ALWAYS_SOFTWARE=1"
    fi

    # T158: the pipelines are guarded, so a graphics probe that fails is
    # reported as the graphics failure it is (exit 3) and never as an
    # unhandled internal error raised from inside a command substitution.
    renderer="$(LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe glxinfo -B 2>/dev/null \
        | sed -n 's/^OpenGL renderer string: //p' | head -1 || true)"
    if [[ "$renderer" != *llvmpipe* && "$renderer" != *softpipe* ]]; then
        die "$EX_GRAPHICS_UNUSABLE" \
            "the OpenGL renderer is not a software rasteriser (got '${renderer:-unknown}')" \
            "DISPLAY=${DISPLAY}" \
            "the game's MonoGame backend needs llvmpipe or softpipe; check the Mesa installation and that LIBGL_ALWAYS_SOFTWARE=1 is honoured"
    fi

    # The load-bearing layer: the game's backend throws without FBO support,
    # and `glxinfo -B` prints no extension list, so full glxinfo is required.
    extensions="$(LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe glxinfo 2>/dev/null \
        | grep -oE 'GL_(ARB|EXT)_framebuffer_object' | head -1 || true)"
    version="$(LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe glxinfo -B 2>/dev/null \
        | sed -n 's/^OpenGL version string: \([0-9.]*\).*/\1/p' | head -1 || true)"

    if [[ -z "$extensions" ]]; then
        local major="${version%%.*}"
        if [[ -z "$major" || "$major" -lt 3 ]]; then
            die "$EX_GRAPHICS_UNUSABLE" \
                "the OpenGL context has no framebuffer-object support and reports version '${version:-unknown}'" \
                "DISPLAY=${DISPLAY}" \
                "the game's MonoGame backend requires GL_ARB_framebuffer_object or GL_EXT_framebuffer_object, or an OpenGL version of at least 3.0 — update the Mesa installation"
        fi
    fi

    log_event INFO "graphics probe passed: renderer=${renderer} fbo=${extensions:-none} version=${version:-unknown}"
}

phase_display() {
    phase_begin "display"

    temp_dir="$(mktemp -d "${TMPDIR:-/tmp}/livingroots-smoke.XXXXXX" 2>/dev/null || printf '')"
    if [[ -z "$temp_dir" || ! -d "$temp_dir" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "cannot create the temporary directory" \
            "${TMPDIR:-/tmp}" \
            "check that \$TMPDIR (or /tmp) is writable"
    fi
    chmod 0700 -- "$temp_dir" 2>/dev/null || true

    display_start
    display_probe_graphics

    phase_end
}

# ---------------------------------------------------------------------------
# T015 — the build-and-verify phase.
#
# The deploy target is removed with `rm -rf -- "$target"` and NO trailing
# slash: a trailing slash makes rm follow a symlinked final component and
# delete the TARGET'S CONTENTS rather than the link. The build is pinned to
# the resolved mods path and bounded by --build-timeout, and the deployed
# assembly is compared with the freshly built one by SHA-256 digest.
# ---------------------------------------------------------------------------

# resolve_mod_folder_name <project-file> <project-base-name>
#
# FR-016 (b) (d): the deploy target is `<mods-path>/<ModFolderName>/` and
# the deployed assembly is `<mods-path>/<ModFolderName>/<EntryDll>`. The
# folder is the project's EFFECTIVE `ModFolderName` MSBuild property, whose
# default is `$(MSBuildProjectName)` — the project FILE's base name
# (`Pathoschild.Stardew.ModBuildConfig.targets`:
# `<ModFolderName Condition="'$(ModFolderName)' == ''">$(MSBuildProjectName)</ModFolderName>`).
# A project that OVERRIDES the property deploys somewhere ELSE, so the
# base name alone names the WRONG folder: the folder SMAPI actually loads
# would then be neither removed nor digest-verified, and FR-016's
# deploy-effectiveness assertion would lapse (T156).
#
# The value is resolved in three steps, most authoritative first:
#
#   1. MSBUILD'S OWN EVALUATION — `dotnet msbuild -getProperty:ModFolderName`
#      (MSBuild >= 17.8) reports the EFFECTIVE value, whatever declared
#      it: the project itself, a Directory.Build.props, an imported props
#      file. The query is BOUNDED, because an evaluation that does not
#      answer must not be allowed to stall the run.
#   2. THE PROJECT'S OWN DECLARATION — the value MSBuild evaluates when the
#      project declares the property. This step answers on a NEVER-RESTORED
#      tree, where the package's default is not imported yet and step 1
#      therefore reports nothing.
#   3. THE PROJECT FILE'S BASE NAME — `$(MSBuildProjectName)`, the
#      ModBuildConfig default, and therefore the value MSBuild itself uses
#      when the project declares nothing.
#
# WHY THE PROPERTY IS READ AND NOT PINNED. FR-016 (c) pins the two
# properties the spec names (`GameModsPath` and `GamePath`); the deploy
# FOLDER is not among them, and FR-016 (d) defines the deployed assembly
# as `<mods-path>/<ModFolderName>/<EntryDll>` — the project's EFFECTIVE
# property. Pinning `-p:ModFolderName=` would OVERRIDE the project's own
# declaration with the script's reading of it, and the FR-016 (d) digest
# comparison — the deploy-effectiveness assertion itself — would then
# compare the build against itself and could no longer detect a deploy
# that landed somewhere else. Reading the property keeps that comparison a
# REAL check: a misread effective property fails the run loudly with exit
# 12 instead of silently verifying the wrong folder.
#
# The result is published in the script-global `mod_folder_name`, and the
# SOURCE it was taken from in `mod_folder_name_source`, so the run log can
# say where the value came from.
resolve_mod_folder_name() {
    local project="$1"
    local project_base="$2"
    local value=""

    mod_folder_name=""
    mod_folder_name_source=""

    # 1. MSBuild's own evaluation of the effective property. The project
    #    path is passed after a `--` terminator (FR-012 item 7), and the
    #    query's own diagnostics are silenced: a host whose MSBuild is too
    #    old for `-getProperty` is not an error, it is the next step's
    #    turn to answer.
    value="$(timeout "$MSBUILD_PROPERTY_QUERY_TIMEOUT_SECS" \
        dotnet msbuild -getProperty:ModFolderName -nologo -- "$project" 2>/dev/null || printf '')"
    value="${value%$'\n'*}"
    value="${value%$'\r'}"
    if [[ -n "$value" ]]; then
        mod_folder_name="$value"
        mod_folder_name_source="the project's effective ModFolderName property (MSBuild)"
        return 0
    fi

    # 2. The project's own declaration. Only an UNCONDITIONAL element
    #    (`<ModFolderName>…</ModFolderName>`) is read: a declaration with
    #    a condition is not the effective value on its own, and MSBuild's
    #    own evaluation — step 1 — is the authority on it.
    value="$(sed -n 's:.*<ModFolderName>[[:space:]]*\([^<]*\)[[:space:]]*</ModFolderName>.*:\1:p' -- "$project" 2>/dev/null | head -1)"
    value="${value%$'\r'}"
    if [[ -n "$value" ]]; then
        mod_folder_name="$value"
        mod_folder_name_source="the project's own <ModFolderName> declaration"
        return 0
    fi

    # 3. The ModBuildConfig default.
    mod_folder_name="$project_base"
    mod_folder_name_source="the ModBuildConfig default — the project file's base name"
    return 0
}

phase_build() {
    phase_begin "rebuild-and-verify"

    local project_dir project_name built deployed built_digest deployed_digest
    local deadline mods_path_real target_real target_parent
    local project_manifest expected_unique_id entry_dll

    project_dir="$(dirname -- "$mod_project")"
    project_name="$(basename -- "$mod_project")"
    project_name="${project_name%.csproj}"

    # The mod folder name, the expected UniqueID and the entry assembly are
    # DERIVED from the mod project — never hardcoded (Constitution V). The
    # folder is the project's EFFECTIVE `ModFolderName` property — whose
    # ModBuildConfig default is the project FILE's name, NOT the
    # project's directory name — and the project's manifest carries the
    # UniqueID and the entry assembly. The DEPLOYED manifest is then
    # verified against them.
    project_manifest="${project_dir}/manifest.json"
    if [[ ! -f "$project_manifest" ]]; then
        die "$EX_MOD_MANIFEST_INVALID" \
            "the mod project has no manifest.json" \
            "$project_manifest" \
            "the manifest is the source of the mod's UniqueID and entry assembly"
    fi

    expected_unique_id="$(read_manifest_field "$project_manifest" "UniqueID" || printf '')"
    entry_dll="$(read_manifest_field "$project_manifest" "EntryDll" || printf '')"
    if [[ -z "$expected_unique_id" ]]; then
        die "$EX_MOD_MANIFEST_INVALID" \
            "the mod project's manifest carries no UniqueID" \
            "$project_manifest" \
            "the manifest must declare a UniqueID"
    fi
    if [[ -z "$entry_dll" ]]; then
        die "$EX_MOD_MANIFEST_INVALID" \
            "the mod project's manifest carries no EntryDll" \
            "$project_manifest" \
            "the manifest must declare an EntryDll"
    fi
    # T156: the deploy folder is the project's EFFECTIVE `ModFolderName`
    # property, not the project file's base name — a project that
    # overrides the property deploys into a DIFFERENT folder, and the
    # folder SMAPI actually loads is the one that must be removed and
    # digest-verified.
    resolve_mod_folder_name "$mod_project" "$project_name"
    if [[ -z "$mod_folder_name" ]]; then
        die "$EX_BUILD_ARTIFACT_FAILURE" \
            "the mod project's deploy folder could not be determined" \
            "$mod_project" \
            "the project's ModFolderName property must resolve to a folder name"
    fi
    log_event INFO "mod identity derived: folder='${mod_folder_name}' (${mod_folder_name_source}) UniqueID='${expected_unique_id}' EntryDll='${entry_dll}'"

    # FR-016 (d): the built assembly is `<mod-project-dir>/bin/<EntryDll>` —
    # the entry assembly's NAME comes from the manifest, never from the
    # project's file name.
    built="${project_dir}/bin/${entry_dll}"

    # T126: the manifest-derived name is a PATH the script constructs, so
    # it is canonicalised and prefix-tested like every other constructed
    # path (FR-012 item 7). The built assembly need not exist before the
    # build, so it is canonicalised with realpath -m. A manifest that
    # names an assembly OUTSIDE the project directory — a ".." segment, an
    # absolute path — is refused here instead of being built and digested
    # against a path the run never owned.
    local project_dir_real
    project_dir_real="$(canonicalize_existing "$project_dir" || printf '')"
    if [[ -z "$project_dir_real" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "the mod project's directory could not be resolved" \
            "$project_dir" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi

    if ! check_mutating_path_under_base "$built" "$project_dir_real"; then
        die "$EX_INTERNAL_ERROR" \
            "the built assembly path does not resolve inside its base" \
            "${built} (resolves to ${mutating_path_real:-unresolvable})" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi
    built="$mutating_path_real"

    local target="${mods_path%/}/${mod_folder_name}"

    # FR-012 item 7: the deploy target is a MUTATING path — the script
    # deletes it. It is canonicalised with realpath -m (it need not exist)
    # and prefix-tested against the resolved mods path with the
    # component-boundary test.
    mods_path_real="$(canonicalize_existing "$mods_path" || printf '%s' "$mods_path")"
    if ! check_mutating_path_under_base "$target" "$mods_path_real"; then
        die "$EX_INTERNAL_ERROR" \
            "the deploy target does not resolve inside its base" \
            "${target} (resolves to ${mutating_path_real:-unresolvable})" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi
    target_real="$mutating_path_real"
    target="${target_real}"

    # FR-016 (b) / T058: the target's PARENT is re-canonicalised immediately
    # before the removal — the pre-flight canonicalised it seconds earlier,
    # and the parent may have been replaced since (a symlink swapped under
    # the path). The removal carries NO trailing slash: a trailing slash
    # makes rm follow a symlinked final component and delete the target's
    # contents instead of the target.
    target_parent="$(canonicalize_maybe_missing "$(dirname -- "$target_real")" || printf '')"
    if [[ -n "$target_parent" ]]; then
        target="${target_parent}/$(basename -- "$target_real")"
    fi

    deployed="${target}/${entry_dll}"

    # T126: the deployed assembly is constructed under the deploy target,
    # which is already validated against the resolved mods path. The
    # manifest-derived name is canonicalised and prefix-tested against that
    # same base, so a manifest that names an assembly OUTSIDE the mods
    # directory is refused here rather than being read and digested.
    if ! check_mutating_path_under_base "$deployed" "$mods_path_real"; then
        die "$EX_INTERNAL_ERROR" \
            "the deployed assembly path does not resolve inside its base" \
            "${deployed} (resolves to ${mutating_path_real:-unresolvable})" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi
    deployed="$mutating_path_real"

    log_event INFO "removing the deploy target: ${target}"

    # T160: the removal's RESULT is the evidence, not a pre-check. The
    # former `rm -rf -- "$target" 2>/dev/null || true` discarded both the
    # status and the diagnostic, so a target that could not be removed left
    # a stale tree behind — the very stale file FR-016 (b) exists to
    # remove — and the run still reported success. The digest comparison
    # only ever covered the ENTRY assembly, so a stale `i18n/*.json` or
    # `Assets/*.png` survived undetected.
    #
    # The removal is therefore attempted, and its own error is carried into
    # the failure report: `rm` prefixes every message with its own name,
    # and that prefix is the locale-independent part of the diagnostic.
    local removal_status=0 removal_error="" removal_context="the removal failed"
    removal_error="$(rm -rf -- "$target" 2>&1)" || removal_status=$?
    if ((removal_status != 0)); then
        if [[ -n "$removal_error" ]]; then
            removal_context="the removal failed: ${removal_error}"
        fi
        die "$EX_BUILD_ARTIFACT_FAILURE" \
            "the deploy target could not be removed" \
            "${target} (rm exited with status ${removal_status})" \
            "the deploy target must be removable — a stale file from a previous build must not survive this run" \
            "$removal_context"
    fi

    local build_log="${temp_dir}/build-output.log"
    : >"$build_log"

    log_event INFO "building ${mod_project} (timeout ${build_timeout_secs}s)"

    # T172: the build's clock starts HERE, so the expiry can name the time
    # the run ACTUALLY consumed and not only the configured bound. FR-016
    # (b) requires the expiry message to name the elapsed time: the bound
    # alone says nothing about how long the rebuild really took, which is
    # the number an operator needs to size `--build-timeout`.
    local build_started_epoch
    build_started_epoch="$(date +%s)"
    deadline=$(( build_started_epoch + build_timeout_secs ))

    # Redirect to a file instead of a pipeline: with `exec dotnet build`
    # replacing the subshell, $! IS dotnet build's PID (not a log-pump loop
    # inside a pipeline), so kill -TERM/$build_pid actually stops the build.
    #
    # The build gets its OWN process group (`set -m` around the launch, as
    # for every other child): `dotnet build` spawns MSBuild worker processes
    # of its own, and signalling only the muxer would leave them running —
    # they would keep writing into the shared build output and the deploy
    # target after the script exited, which FR-016 (b) forbids.
    #
    # FR-012 item 7: the project path is a path argument and is passed after
    # a `--` terminator. The terminator ends the dotnet CLI's own option
    # parsing, so the build's own options come FIRST.
    #
    # T123: FR-016 (c) pins the build to the RESOLVED game path, and the
    # resolved path is the CANONICAL one — its sibling option,
    # `-p:GameModsPath=`, already receives the canonicalised mods path
    # (the pre-flight replaces it with the resolved one). The raw
    # `--game-path` spelling would let the build validate against a
    # different installation than the one the script is about to launch:
    # `ModBuildConfig` takes its reference assemblies from `$(GamePath)`.
    local game_path_real
    game_path_real="$(canonicalize_existing "$game_path" || printf '')"
    if [[ -z "$game_path_real" ]]; then
        die "$EX_GAME_NOT_FOUND" \
            "the game directory could not be resolved for the rebuild" \
            "$game_path" \
            "the build is pinned to the resolved game path (FR-016 (c)) — the installation must resolve before the rebuild"
    fi

    local -a build_options=(
        --configuration Release
        --no-incremental
        "-p:GameModsPath=$mods_path"
        "-p:GamePath=$game_path_real"
    )
    set -m
    (
        cd -- "$project_dir" || exit 1
        exec dotnet build "${build_options[@]}" -- "$mod_project"
    ) >"$build_log" 2>&1 9<&- &
    build_pid=$!
    set +m

    # T103: the group is published in the SCRIPT-GLOBAL, so the cleanup and
    # the signal forwarder can stop an interrupted build.
    #
    # T091: the publication comes BEFORE the assertion, so the assertion's
    # own failure path can still stop the build: a group published after
    # the assertion leaves the build un-signalled, which is exactly the
    # orphan the cleanup exists to prevent (FR-010). The cleanup's own
    # guard refuses to signal the script's own group and group 1, so
    # publishing the group's value early cannot make the script signal
    # itself.
    child_pgid "$build_pid"
    build_pgid="$child_group"
    assert_child_group_is_own "$build_pid" "build"

    while kill -0 "$build_pid" 2>/dev/null; do
        if (( $(date +%s) >= deadline )); then
            log_event WARN "the rebuild exceeded its ${build_timeout_secs}s bound — sending SIGTERM to the build process group ${build_pgid} (pid ${build_pid})"
            if [[ -n "$build_pgid" ]]; then
                assert_signallable_group "$build_pgid" "build"
                signal_group "$build_pgid" TERM
            fi
            log_event INFO "the build process group ${build_pgid} has the grace period of 1s to exit"
            sleep 1
            if [[ -n "$build_pgid" ]]; then
                log_event WARN "the build process group ${build_pgid} (pid ${build_pid}) is still alive after the grace period of 1s — sending SIGKILL to the group"
                signal_group "$build_pgid" KILL
            fi
            reap_child "$build_pid" "build" 5 || true
            # The group was signalled (SIGTERM, then SIGKILL) and the build
            # was reaped, so the publication is withdrawn: a process-group
            # id can be RECYCLED by an unrelated process, and the cleanup
            # must never signal such a stranger.
            build_pgid=""
            # T102: the build output is dumped into the run log BEFORE the
            # report is written. The report names the run log as the place
            # to find the build output, but the build output's OWN file
            # lives in the run's private temp directory, which the cleanup
            # deletes — so a report written first would promise output
            # that no longer exists by the time anyone reads it (FR-014,
            # FR-016 (b)).
            dump_build_output "$build_log"
            # T172: the elapsed seconds are the time the rebuild ACTUALLY
            # consumed, measured from the build's own start. The message
            # names both numbers: the configured bound (what the operator
            # asked for) and the elapsed time (what the rebuild cost).
            local build_elapsed=$(( $(date +%s) - build_started_epoch ))
            die "$EX_BUILD_ARTIFACT_FAILURE" \
                "the rebuild exceeded its ${build_timeout_secs}s bound after ${build_elapsed}s" \
                "dotnet build ${mod_project} (elapsed ${build_elapsed}s, bound ${build_timeout_secs}s)" \
                "raise --build-timeout, or fix the build (the run log has the build output)"
        fi
        sleep 0.3
    done

    local build_status=0
    reap_child "$build_pid" "build" 5 || build_status=$?

    # T103: the group stays published while it can still be alive — an
    # MSBuild worker that outlived the muxer is still writing into the
    # shared build output — and the publication is withdrawn once the group
    # is gone: a process-group id can be RECYCLED by an unrelated process,
    # and the cleanup must never signal such a stranger.
    if [[ -n "$build_pgid" ]] && ! kill -0 -- "-${build_pgid}" 2>/dev/null; then
        build_pgid=""
    fi

    # Log build output after completion (was streamed live via the pipeline).
    # T170: the dump is a helper, so the residual partial line (a build
    # output that ends WITHOUT a trailing newline) is emitted on this path
    # too — and the same helper serves the build-timeout path (T102).
    dump_build_output "$build_log"

    # FR-016 (d): the built assembly is SNAPSHOT into the run's private temp
    # directory immediately after the build. The shared <project>/bin/ can be
    # rewritten by a concurrent rebuild between the build and the comparison,
    # and then the comparison would decide on an artifact this run never
    # built. `cp -p` preserves the mtime, so the "strictly older" rule still
    # compares the deployed file against the mtime of the artifact the build
    # actually produced.
    local built_snapshot="${temp_dir}/built-assembly.dll"
    if [[ -f "$built" ]]; then
        cp -p -- "$built" "$built_snapshot" 2>/dev/null || true
    fi

    if ((build_status != 0)); then
        # The digests are named even when the build failed, because a failed
        # deploy is the most common cause and the pair identifies it.
        local built_d deployed_d
        built_d="$([[ -f "$built_snapshot" ]] && sha256sum -- "$built_snapshot" | cut -d' ' -f1 || printf 'unavailable')"
        deployed_d="$([[ -f "$deployed" ]] && sha256sum -- "$deployed" | cut -d' ' -f1 || printf 'unavailable')"
        die "$EX_BUILD_ARTIFACT_FAILURE" \
            "the rebuild failed (dotnet build exited with status ${build_status})" \
            "built: ${built} (${built_d}) / deployed: ${deployed} (${deployed_d})" \
            "fix the build error — the run log has the full build output"
    fi

    if [[ ! -f "$built_snapshot" ]]; then
        die "$EX_BUILD_ARTIFACT_FAILURE" \
            "the rebuild produced no assembly" \
            "built: ${built} (unavailable) / deployed: ${deployed} ($( [[ -f "$deployed" ]] && sha256sum -- "$deployed" | cut -d' ' -f1 || printf 'unavailable'))" \
            "check the build output in the run log"
    fi

    if [[ ! -f "$deployed" ]]; then
        die "$EX_BUILD_ARTIFACT_FAILURE" \
            "the build did not deploy the assembly into the resolved mods path" \
            "built: ${built} ($(sha256sum -- "$built_snapshot" | cut -d' ' -f1)) / deployed: ${deployed} (unavailable)" \
            "check that the resolved mods path is writable and that ModBuildConfig deployed into it"
    fi

    # T158: the digest pipelines are guarded, so a failing `sha256sum` is
    # reported as the internal error it is instead of raising an unhandled
    # error from inside a command substitution. An EMPTY digest is caught
    # below, so the deploy verification can never pass on a digest that was
    # never computed.
    built_digest="$(sha256sum -- "$built_snapshot" | cut -d' ' -f1 || printf '')"
    deployed_digest="$(sha256sum -- "$deployed" | cut -d' ' -f1 || printf '')"

    if [[ -z "$built_digest" || -z "$deployed_digest" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "the assembly's digest could not be computed" \
            "built: ${built} / deployed: ${deployed}" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi

    if [[ "$built_digest" != "$deployed_digest" ]]; then
        die "$EX_BUILD_ARTIFACT_FAILURE" \
            "the deployed assembly is not the one this run built" \
            "built: ${built} (${built_digest}) / deployed: ${deployed} (${deployed_digest})" \
            "the deploy did not take effect — check that the resolved mods path is writable and that the deploy target is pinned to it"
    fi

    if [[ "$deployed" -ot "$built_snapshot" ]]; then
        die "$EX_BUILD_ARTIFACT_FAILURE" \
            "the deployed assembly is strictly older than the one this run built" \
            "built: ${built} (${built_digest}) / deployed: ${deployed} (${deployed_digest})" \
            "the deploy did not take effect — check that the resolved mods path is writable"
    fi

    log_event INFO "deploy verified: sha256 ${deployed_digest}"

    # FR-017's post-build check: the mod folder, its manifest and its UniqueID.
    local manifest="${target}/manifest.json"
    local unique_id

    if [[ ! -f "$manifest" ]]; then
        die "$EX_MOD_MANIFEST_INVALID" \
            "the deployed mod folder has no manifest.json" \
            "$manifest" \
            "check the deploy target and the mod project's manifest"
    fi

    unique_id="$(read_manifest_field "$manifest" "UniqueID" || printf '')"
    if [[ "$unique_id" != "$expected_unique_id" ]]; then
        die "$EX_MOD_MANIFEST_INVALID" \
            "the deployed manifest's UniqueID does not match the mod project's" \
            "${manifest} (UniqueID='${unique_id:-none}', expected '${expected_unique_id}')" \
            "the deploy target must contain this mod's manifest (UniqueID ${expected_unique_id})"
    fi

    mod_name="$(read_manifest_field "$manifest" "Name" || printf '')"
    if [[ -z "$mod_name" ]]; then
        mod_name="Stardew Valley - Living Roots"
    fi
    log_event INFO "mod manifest verified: UniqueID=${unique_id} Name='${mod_name}'"

    phase_end
}

# ---------------------------------------------------------------------------
# T016 — the log gate.
#
# The log is normalised (the trailing CR stripped) and split into records of
# <level>\t<source>\t<text>. The level and the source are bound BY LABEL,
# never by a fixed column: SMAPI pads the level to 5 characters for the six
# public levels, but its internal CRITICAL level renders 8 and shifts the
# source column by three. A record may be followed by continuation lines
# that carry no prefix; they inherit no level and no source.
#
# The gate has three scoped legs, each evaluated only against its own
# section's records. The manifest name is never searched across the whole
# log: SMAPI's skip entry and its content-pack entry both contain it, so an
# unscoped search would let a SKIPPED mod satisfy the "listed" leg.
# ---------------------------------------------------------------------------

mod_name="Stardew Valley - Living Roots"
# The mod folder name, derived from the mod project (Constitution V).
# T156: it is the project's EFFECTIVE `ModFolderName` MSBuild property, not
# the project file's base name — a project that overrides the property
# deploys into a different folder.
mod_folder_name=""
# Where the folder name was resolved from, for the run log.
mod_folder_name_source=""
# The mod own success marker (secondary, non-gating signal, FR-006).
mod_success_marker=""
log_context=""

# shellcheck disable=SC2016  # the awk program is single-quoted ON PURPOSE: its $0/$1 must reach awk unexpanded
readonly AWK_LOG_GATE='
BEGIN {
    seen_sentinel = 0
    loaded_header = 0
    loaded_count = -1
    in_loaded = 0
    mod_listed = 0
    skipped_header = 0
    in_skipped = 0
    mod_skipped = 0
    skip_reason = ""
    mod_error = ""
    mod_error_rec = 0
    rec_count = 0
    pre_count = 0
    graphics_fail = ""
    graphics_rec = 0
    cur_rec = 0
    block = ""
    mod_success_marker = ""

    # T155: the graphics-device markers, matched against the message of a
    # record TOGETHER WITH its continuation lines.
    #
    # The first marker is the one MonoGame itself raises:
    # `NoSuitableGraphicsDeviceException` is thrown as `new
    # NoSuitableGraphicsDeviceException("Failed to create graphics device!",
    # ex)`, and SMAPI renders the exception through
    # `ExceptionHelper.GetLogSummary()`, which is `exception.ToString()`.
    # The second marker is the text SMAPI itself emits (SMAPI 4.5.2,
    # `SCore.cs`, `catch (NoSuitableGraphicsDeviceException ex)`): the
    # record message reads "Your graphics card", " isn\047t compatible with
    # Stardew Valley. ", " https://smapi.io/troubleshoot/no-suitable-gpu"
    # and "Technical details:", and the exception summary follows it.
    #
    # Both phrases land in the CONTINUATION lines, never in the record text
    # a record-only match could see: SMAPI applies the record prefix ONCE,
    # to the whole multi-line message. The marker "GPU: Could not detect"
    # that this pair replaces exists in NO SMAPI and NO MonoGame string, so
    # a real graphics failure fell through the ladder to the "aborted before
    # mod resolution" verdict (17) instead of the graphics verdict (3).
    graphics_marker = "Failed to create graphics device"
    graphics_marker2 = "is\047t compatible with Stardew Valley"
}
{
    if (match($0, /^\[[^]]*\] /)) {
        prefix = substr($0, 1, RLENGTH)
        text = substr($0, RLENGTH + 1)
        inner = substr(prefix, 2, length(prefix) - 3)
        sp = index(inner, " ")
        rest = substr(inner, sp + 1)
        lvl = ""
        src = ""
        if (match(rest, /^[A-Za-z]+/)) {
            lvl = substr(rest, 1, RLENGTH)
            after = substr(rest, RLENGTH + 1)
            sub(/^ +/, "", after)
            src = after
            sub(/^screen_[0-9]+ +/, "", src)
        }
        is_record = 1
    } else {
        lvl = ""
        src = ""
        text = $0
        is_record = 0
    }

    # Everything before the run sentinel belongs to a previous run: it is
    # never evaluated. It is recorded, though, because it is the CONTEXT of
    # the no-sentinel verdict: a log that carries no run sentinel belongs to
    # a previous run in its entirety, so its trailing records are what the
    # report must carry (FR-007).
    if (!seen_sentinel) {
        if (is_record && text ~ /^Log started at /) {
            seen_sentinel = 1
            sentinel_text = text
        }
        pre_text[++pre_count] = text
        pre_lvl[pre_count] = lvl
        pre_src[pre_count] = src
        next
    }

    # Every physical line after the sentinel is recorded — including the
    # continuation lines, which carry no prefix and therefore no level.
    rec_text[++rec_count] = text
    rec_lvl[rec_count] = lvl
    rec_src[rec_count] = src

    # T155: the graphics-device markers are matched against the message of a
    # record TOGETHER WITH its continuation lines. `block` holds the message
    # of the current record plus every continuation line that follows it, so
    # a marker that lands in a continuation line is found — the text of the
    # record alone never carries it.
    if (is_record) {
        cur_rec = rec_count
        block = text
    } else {
        block = block "\n" text
    }
    if (graphics_fail == "" && (block ~ graphics_marker || block ~ graphics_marker2)) {
        graphics_fail = text
        graphics_rec = cur_rec
    }

    if (!is_record) {
        next
    }

    # --- leg 1: the mod is listed in the loaded-mods block ---
    if (lvl == "INFO" && src == "SMAPI" && text ~ /^Loaded [0-9]+ mods[.:]$/) {
        loaded_header = 1
        in_loaded = 1
        loaded_count = text
        gsub(/[^0-9]/, "", loaded_count)
        next
    }
    if (in_loaded) {
        if (lvl == "INFO" && src == "SMAPI" && text ~ /^   /) {
            if (text ~ (mod "([ ]|$)")) {
                mod_listed = 1
            }
            next
        }
        in_loaded = 0
    }

    # --- leg 2: the mod is absent from the skipped-mods section ---
    if (lvl == "ERROR" && src == "SMAPI" && text == "   Skipped mods") {
        skipped_header = 1
        in_skipped = 1
        next
    }
    if (in_skipped) {
        if (text == "") {
            next
        }
        if (text ~ /^   /) {
            if (text ~ /^      - / && index(text, " because ") > 0 && text ~ (mod "([ ]|$)")) {
                mod_skipped = 1
                p = index(text, " because ")
                skip_reason = substr(text, p + 9)
            }
            next
        }
        in_skipped = 0
    }

    # --- leg 3: no mod-scoped ERROR/ALERT record ---
    if ((lvl == "ERROR" || lvl == "ALERT") && src == mod && mod_error == "") {
        mod_error = text
        mod_error_rec = rec_count
    }

    # --- the own success marker of the mod (secondary, non-gating) ---
    # Exact, case-sensitive match on the mod success line. The apostrophes
    # are written as \047 because this program lives inside a single-quoted
    # shell string.
    if (is_record && mod_success_marker == "") {
        marker_pat = "^The \047" mod "\047 mod was loaded successfully!$"
        if (text ~ marker_pat) {
            mod_success_marker = text
        }
    }
}
END {
    if (graphics_fail != "") {
        code = 3
        reason = graphics_fail
    } else if (mod_error != "") {
        code = 5
        reason = mod_error
    } else if (mod_skipped) {
        code = 5
        reason = skip_reason
    } else if (loaded_header && !mod_listed) {
        code = 5
        if (loaded_count + 0 == 0) {
            reason = "SMAPI loaded 0 mods — the mod was not among them"
        } else {
            reason = "the mod is not listed in SMAPI\047s loaded-mods block"
        }
    } else if (!loaded_header) {
        if (seen_sentinel) {
            code = 17
            reason = "SMAPI started (the run sentinel was written) but never resolved any mods"
        } else {
            code = 13
            reason = "the log carries no run sentinel, so it does not belong to this run"
        }
    } else {
        code = 0
        reason = ""
    }

    print code
    print reason
    print mod_success_marker

    if (code == 3 && graphics_rec > 0) {
        # FR-007: the graphics failure IS a matched failure record, so the
        # context is that record TOGETHER WITH its continuation lines — the
        # very lines the failure text lands in (T155).
        print rec_lvl[graphics_rec] "\t" rec_src[graphics_rec] "\t" rec_text[graphics_rec]
        for (i = graphics_rec + 1; i <= rec_count; i++) {
            if (rec_lvl[i] == "") {
                print rec_text[i]
            } else {
                break
            }
        }
    } else if (code == 5 && mod_error != "") {
        # FR-007: the context is the matched record TOGETHER WITH its
        # continuation lines — the message of the matched record first, then
        # every continuation line that follows it, up to (but excluding) the
        # next record that carries a prefix.
        print rec_lvl[mod_error_rec] "\t" rec_src[mod_error_rec] "\t" mod_error
        for (i = mod_error_rec + 1; i <= rec_count; i++) {
            if (rec_lvl[i] == "") {
                print rec_text[i]
            } else {
                break
            }
        }
        # FR-003 / FR-007: every Trace record of the mod that follows the
        # failure record is part of the context too — those records carry
        # the exception type and the HResult.
        for (i = mod_error_rec + 1; i <= rec_count; i++) {
            if (rec_lvl[i] == "TRACE" && rec_src[i] == mod) {
                print rec_lvl[i] "\t" rec_src[i] "\t" rec_text[i]
            }
        }
    } else if (code != 0) {
        # FR-007: when no failure record matched, the context is the
        # trailing RECORDS of the normalised stream, capped at 30.
        #
        # T098: the window is taken from the region the gate evaluated — the
        # lines that FOLLOW the run sentinel. A log that carries NO run
        # sentinel has no such region (the whole file belongs to a previous
        # run), so the trailing records of the WHOLE normalised file are the
        # context there: the file itself is the thing under suspicion.
        if (seen_sentinel) {
            start = rec_count - 29
            if (start < 1) {
                start = 1
            }
            for (i = start; i <= rec_count; i++) {
                if (rec_lvl[i] == "") {
                    print rec_text[i]
                } else {
                    print rec_lvl[i] "\t" rec_src[i] "\t" rec_text[i]
                }
            }
        } else {
            start = pre_count - 29
            if (start < 1) {
                start = 1
            }
            for (i = start; i <= pre_count; i++) {
                if (pre_lvl[i] == "") {
                    print pre_text[i]
                } else {
                    print pre_lvl[i] "\t" pre_src[i] "\t" pre_text[i]
                }
            }
        }
    }
}
'

# report_skipped_checks
#
# FR-004: the save-data persistence check is ALWAYS skipped — the automated
# script cannot load a save (SMAPI has no command-line save selection and
# the mod only reacts to events after a save is loaded through the title
# screen), so there is nothing for the check to inspect. A skipped check
# MUST be labelled `SKIPPED:` and counted separately from the passes, and it
# does NOT gate the verdict (FR-006).
#
# The label appears EXACTLY ONCE per run, and the same line reaches stderr
# and the run log (FR-014).
report_skipped_checks() {
    log_event WARN "SKIPPED: the mod-data save-persistence check was not run — the automated script cannot load a save, so the mod never writes its data files"
}

phase_log_gate() {
    phase_begin "verdict"

    # T124: the `SKIPPED:` label is emitted BEFORE the verdict is decided, so
    # a FAILING run's log also records that the save check was skipped —
    # today only the success path carries the label (FR-004). The label still
    # appears exactly once per run: the gate either fails after it or
    # succeeds after it, and neither path emits it again.
    report_skipped_checks

    log_gate
    phase_end
}

log_gate() {
    local normalized="${temp_dir}/smapi-log.normalized"
    local out_file="${temp_dir}/gate.out"
    local code reason

    if [[ ! -f "$smapi_log" ]]; then
        die "$EX_NO_LOG_PRODUCED" \
            "the game produced no SMAPI log" \
            "$smapi_log" \
            "check that the game started and that SMAPI could write its log"
    fi

    # Strip the trailing CR: GNU grep does not strip it on Linux, so any
    # pattern ending in $ would never match a CRLF-terminated line.
    sed 's/\r$//' -- "$smapi_log" >"$normalized" 2>/dev/null || true

    awk -v mod="$mod_name" -- "$AWK_LOG_GATE" "$normalized" >"$out_file" 2>/dev/null || true

    if [[ ! -s "$out_file" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "the log gate produced no verdict" \
            "$smapi_log" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi

    code="$(head -1 -- "$out_file")"
    reason="$(sed -n '2p' -- "$out_file")"
    mod_success_marker="$(sed -n '3p' -- "$out_file")"
    # Everything after the marker line is the failure context: the matched
    # record's continuation lines.
    log_context="$(sed -n '4,$p' -- "$out_file")"

    case "$code" in
        0)
            log_event INFO "log gate: the mod is listed, not skipped, and has no Error/Alert record"
            ;;
        3)
            # T098: the verdict carries the gate's context — for a graphics
            # failure that is the matched failure record together with its
            # continuation lines, which is where the failure text lands.
            die "$EX_GRAPHICS_UNUSABLE" \
                "the game could not create a graphics device" \
                "$smapi_log" \
                "install a usable software OpenGL stack (Mesa's llvmpipe); the pre-flight probe should have caught this" \
                "${log_context}"
            ;;
        5)
            die "$EX_MOD_LOAD_FAILURE" \
                "the mod did not load cleanly" \
                "${smapi_log} — ${reason}" \
                "read the failure context above; the SMAPI log has the detail" \
                "${log_context}"
            ;;
        13)
            # T098: the verdict carries the gate's context — for a log with
            # no run sentinel that is the trailing records of the whole
            # normalised file, capped at 30.
            die "$EX_INTERNAL_ERROR" \
                "the SMAPI log does not belong to this run" \
                "$smapi_log" \
                "the run sentinel is missing — the log was not written by this run" \
                "${log_context}"
            ;;
        17)
            # T098: the verdict carries the gate's context — for an abort
            # before the mod resolution that is the trailing records of the
            # normalised stream, capped at 30.
            die "$EX_MOD_RESOLUTION_ABORTED" \
                "SMAPI aborted before it resolved any mods" \
                "${smapi_log} — ${reason}" \
                "check for a leftover SMAPI marker in the game directory and for a graphics-device failure in the log" \
                "${log_context}"
            ;;
        *)
            die "$EX_INTERNAL_ERROR" \
                "the log gate returned an unknown verdict" \
                "verdict ${code}" \
                "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
            ;;
    esac
}

# ---------------------------------------------------------------------------
# T017 — the launch, the two-phase wait and the termination.
#
# Phase 1 of the wait is the run sentinel: it establishes that SMAPI has
# started and is writing the log. Phase 2 waits for the mod-resolution
# header, which SMAPI writes only after the graphics device is created —
# tens of seconds later. Evaluating the gate at the sentinel would report a
# missing header on every run.
#
# The header's PRESENCE is what distinguishes "SMAPI resolved the mods and
# ours was not among them" (5) from "SMAPI never got that far" (4 / 17).
# ---------------------------------------------------------------------------

launch_game() {
    export SMAPI_NO_TERMINAL=true
    export SMAPI_DEVELOPER_MODE=true
    export LIBGL_ALWAYS_SOFTWARE=1
    export GALLIUM_DRIVER=llvmpipe
    export SDL_VIDEODRIVER=x11
    # The game must be MUTED: Xvfb virtualizes the display, not the audio
    # device, so without these the run would play sound on the host.
    export SDL_AUDIODRIVER=dummy
    export ALSOFT_DRIVERS=null

    log_event INFO "launch contract:"
    log_event INFO "  command: ./StardewModdingAPI --no-terminal --mods-path ${mods_path}"
    log_event INFO "  working directory (cwd): ${game_path}"
    log_event INFO "  environment: SMAPI_NO_TERMINAL=true SMAPI_DEVELOPER_MODE=true LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe SDL_VIDEODRIVER=x11 SDL_AUDIODRIVER=dummy ALSOFT_DRIVERS=null DISPLAY=${DISPLAY}"
    log_event INFO "launching ./StardewModdingAPI --no-terminal in ${game_path}"

    # `set -m` is enabled around the launch ONLY: job control gives the child
    # its own process group (whose id equals its pid), and a background child
    # of a non-interactive shell gets none otherwise. It is disabled again
    # immediately, because leaving it on would move this script out of the
    # terminal's foreground group and a terminal SIGINT would then reach the
    # poll loop's sleep instead of the script.
    set -m
    start_stamped_output "${temp_dir}/game-output.fifo" "game" game_stamper_pid \
        || die "$EX_INTERNAL_ERROR" \
            "cannot create the game output pipe" "${temp_dir}" \
            "check that \$TMPDIR is writable"
    (
        cd -- "$game_path" || exit 1
        exec ./StardewModdingAPI --no-terminal --mods-path "$mods_path"
    ) </dev/null >"${temp_dir}/game-output.fifo" 2>&1 9<&- &
    game_pid=$!
    set +m

    # T091: the group is published BEFORE the assertion, so the assertion's
    # OWN failure path can still stop the child: a group published after the
    # assertion leaves the game un-signalled, which is exactly the orphan
    # the cleanup exists to prevent (FR-010). The cleanup's own guard
    # refuses to signal the script's own group and group 1, so publishing
    # the group's value early cannot make the script signal itself.
    child_pgid "$game_pid"
    game_pgid="$child_group"
    assert_child_group_is_own "$game_pid" "game"
    log_event INFO "game launched: pid ${game_pid}, process group ${game_pgid:-unknown}"
}

# The line number of the current run's sentinel record in the SMAPI log.
#
# T173: it is the boundary the second readiness probe is scoped to, so a
# stale header that a previous run left BEFORE the sentinel cannot end the
# wait. It is empty until the readiness wait has accepted a sentinel of this
# run.
run_sentinel_line=""

# The launch epoch — the value FR-002's freshness control compares the run
# sentinel against — and the same instant in the UTC form the run log's
# other timestamps use.
#
# T161: both forms are captured in ONE `date` invocation, so they describe
# the same moment, and both are written to the run log: the epoch is the
# value the comparison uses, and the UTC form is the one a reader can
# place on a timeline. They are empty until the launch phase takes them.
launch_t0=""
launch_iso=""

# The prefixed records whose message begins with "Log started at ", one per
# line, as "<line number>\t<timestamp>".
#
# T173: the run sentinel MUST be a prefixed record. An unanchored search over
# the raw file matched any line that merely carried the text — a
# continuation line included — and a stale line then stood in for the run's
# own boundary.
#
# The timestamp is extracted the way the freshness rule reads it: the token
# that follows the marker, and it counts as parseable only when the record
# carries the " UTC" suffix the sentinel shape requires. An empty timestamp
# therefore means "the sentinel carries no parseable timestamp".
# shellcheck disable=SC2016  # the awk program is single-quoted ON PURPOSE: its $0/$1 must reach awk unexpanded
readonly AWK_SENTINEL_RECORDS='
{
    if (match($0, /^\[[^]]*\] /)) {
        text = substr($0, RLENGTH + 1)
        if (text !~ /^Log started at /) {
            next
        }
        ts = ""
        marker = "Log started at "
        if (match(text, "^" marker "[^ ]+ UTC")) {
            ts = substr(text, length(marker) + 1, RLENGTH - length(marker) - 4)
        }
        print NR "\t" ts
    }
}
'

# log_sentinel_records — the prefixed records whose message begins with
# "Log started at ", as "<line number>\t<timestamp>" lines.
log_sentinel_records() {
    [[ -f "$smapi_log" ]] || return 0
    awk -- "$AWK_SENTINEL_RECORDS" "$smapi_log" 2>/dev/null || true
}

# log_has_sentinel — does the log carry a run sentinel?
#
# T173: the sentinel MUST be a PREFIXED record (FR-002). The probe used to
# be an unanchored `grep -q "Log started at "` over the whole raw file, so a
# line that merely carried the text — a continuation line, or a stale line
# a previous run left behind — satisfied the first phase of the readiness
# wait.
log_has_sentinel() {
    [[ -n "$(log_sentinel_records)" ]]
}

# log_has_mod_resolution — does the log carry a mod-resolution header AFTER
# the run sentinel?
#
# T173 / FR-002: the header must FOLLOW the sentinel of this run. The probe
# used to be an unscoped search over the whole raw file, so a stale header
# that a previous run left behind ended the second phase of the wait before
# SMAPI had resolved any mod.
#
# The probe FAILS CLOSED when the run sentinel is not known yet: without a
# boundary there is no region to search.
log_has_mod_resolution() {
    [[ -n "$run_sentinel_line" ]] || return 1

    awk -v sentinel_line="$run_sentinel_line" -- '
        NR <= (sentinel_line + 0) { next }
        match($0, /^\[[^]]*\] /) {
            text = substr($0, RLENGTH + 1)
            if (text ~ /^Loaded [0-9]+ mods[.:]/) {
                found = 1
                exit
            }
        }
        END { exit(found ? 0 : 1) }
    ' "$smapi_log" 2>/dev/null
}

# check_log_inode_freshness
#
# FR-002's inode control, evaluated INDEPENDENTLY of the sentinel wait.
#
# FR-002: the log file's inode is recorded BEFORE the stale log is removed
# and MUST have changed once the log was recreated — the order of the two
# steps is normative, because recording the inode after the removal would
# make the baseline "absent" and the comparison vacuous. The UNCHANGED
# inode is the only signal that SMAPI's purge failed silently:
# `PurgeNormalLogs` swallows an IOException, SMAPI falls back to
# `SMAPI-latest.player-{n}.txt`, and the STALE log stays at the default
# path — where the sentinel of this run never appears.
#
# T122: the comparison used to be reached only AFTER the readiness wait had
# succeeded, so a purge that failed silently cost the whole --timeout
# budget and was then reported as a TIMEOUT (4). The comparison is now
# evaluated on EVERY poll of the wait — in BOTH of its phases — so the
# stale-log case is reported as the stale-log internal error (13) as soon
# as the evidence is there.
#
# The comparison is STRICT: an inode that did not change fails the run,
# whatever else the file at the path looks like. FR-002 states the rule
# normatively ("verify it changed once the log was recreated"), and the
# final assertion (verify_log_freshness) applies the same rule — a guard
# that let a changed modification time excuse an unchanged inode would
# disagree with the assertion that ends the run.
check_log_inode_freshness() {
    local current_inode=""

    # No baseline: there was no stale log to remove, so there is no inode to
    # compare against. The wait continues.
    if [[ -z "$smapi_log_baseline_inode" ]]; then
        return 0
    fi

    # The log does not exist (yet): SMAPI has not created it. There is
    # nothing to compare. The wait continues.
    if [[ ! -f "$smapi_log" ]]; then
        return 0
    fi

    current_inode="$(stat -c %i -- "$smapi_log" 2>/dev/null || printf '')"
    if [[ -z "$current_inode" ]]; then
        # The inode could not be read. The final assertion
        # (verify_log_freshness) reports it once the run sentinel appeared.
        return 0
    fi

    # The inode CHANGED: the log was recreated, which is what FR-002
    # requires. The control passes.
    if [[ "$current_inode" != "$smapi_log_baseline_inode" ]]; then
        return 0
    fi

    # The inode is UNCHANGED: the log at the default path was not recreated,
    # so the file there is the stale log and this run's sentinel will never
    # appear in it. The recreated inode is recorded BEFORE the report, so the
    # run log carries BOTH the baseline inode and the inode observed at the
    # log path — the evidence the comparison rests on.
    smapi_log_recreated_inode="$current_inode"
    log_event INFO "recreated SMAPI log inode: ${smapi_log_recreated_inode}"
    log_event INFO "log freshness: the log at the path still carries the stale log's inode (${current_inode}) — the stale log survived the removal"

    die "$EX_INTERNAL_ERROR" \
        "the SMAPI log was not recreated — the stale log survived the removal" \
        "$smapi_log (inode ${current_inode} is the stale log's inode)" \
        "SMAPI fell back to a player-specific log name — remove the stale log by hand and re-run"
}

# verify_log_freshness
#
# FR-002's third freshness control: the log file's inode is recorded BEFORE
# the stale log is removed, and it MUST have changed once the log was
# recreated. The UNCHANGED inode is the only signal that SMAPI's
# `player-N` fallback happened — SMAPI writes to
# `SMAPI-latest.player-{n}.txt` when the default path still holds the stale
# log, so the default path keeps the STALE content and the run would decide
# the verdict on a previous run's log.
#
# The comparison is ALSO evaluated during the wait, independently of it
# (check_log_inode_freshness, T122); this function is the final assertion
# that runs once the log this run produced has been seen.
verify_log_freshness() {
    if [[ -z "$smapi_log_baseline_inode" ]]; then
        # There was no stale log, so there is no baseline to compare against:
        # the log this run reads is the only log that was ever there.
        log_event INFO "log freshness: no baseline inode was recorded (there was no stale log to remove)"
        return 0
    fi

    smapi_log_recreated_inode="$(stat -c %i -- "$smapi_log" 2>/dev/null || printf '')"
    log_event INFO "recreated SMAPI log inode: ${smapi_log_recreated_inode:-unknown}"

    if [[ -z "$smapi_log_recreated_inode" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "the SMAPI log's inode could not be read after the run sentinel appeared" \
            "$smapi_log" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi

    if [[ "$smapi_log_recreated_inode" == "$smapi_log_baseline_inode" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "the SMAPI log was not recreated — the stale log survived the removal" \
            "$smapi_log (inode ${smapi_log_recreated_inode} is the stale log's inode)" \
            "SMAPI fell back to a player-specific log name — remove the stale log by hand and re-run"
    fi

    log_event INFO "log freshness verified: the log inode changed (${smapi_log_baseline_inode} -> ${smapi_log_recreated_inode})"
}

# sentinel_is_fresh <launch-t0> — is the log's run sentinel this run's?
#
# FR-002's freshness control: a sentinel older than T0 belongs to a previous
# run and MUST be ignored, so the caller keeps waiting. An UNPARSEABLE
# timestamp is not evidence of staleness, so the sentinel is accepted.
#
# T173: the log can carry MORE THAN ONE sentinel — a stale one a previous
# run left behind, and the one this run wrote. Every prefixed sentinel is
# examined in the order it appears, and the FIRST one that belongs to this
# run is the run boundary: the stale one is skipped, and the line the
# boundary sits on is published, so the second readiness probe can be
# scoped to the region that follows it.
sentinel_is_fresh() {
    local launch_t0="${1:-0}"
    local line ts epoch candidates

    candidates="$(log_sentinel_records)"

    while IFS=$'\t' read -r line ts; do
        [[ -n "$line" ]] || continue

        if [[ -z "$ts" ]]; then
            # The sentinel carries no parseable timestamp: not evidence of
            # staleness, so it is accepted as the run boundary.
            run_sentinel_line="$line"
            return 0
        fi

        # The sentinel carries NO offset, so it MUST be parsed as UTC explicitly.
        # Parsing it as local time shifts the comparison by the local UTC offset,
        # which on a host west of UTC would make every sentinel look hours in the
        # FUTURE and the staleness check would never fire (FR-002).
        epoch="$(date -u -d "${ts}Z" +%s 2>/dev/null || printf '')"
        if [[ -z "$epoch" ]]; then
            log_event WARN "the run sentinel ($ts) has no parseable UTC timestamp — accepting it as this run's boundary"
            run_sentinel_line="$line"
            return 0
        fi

        if (( epoch >= launch_t0 )); then
            run_sentinel_line="$line"
            return 0
        fi

        log_event WARN "stale run sentinel ($ts) predates launch T0 — ignoring"
    done <<< "$candidates"

    return 1
}

wait_for_sentinel() {
    local deadline="$1"
    local launch_t0="${2:-0}"

    while :; do
        # T122: FR-002's inode control is evaluated FIRST, on EVERY poll and
        # independently of the sentinel wait — the comparison is no longer
        # reached only after the sentinel appeared. A stale log that
        # survived the removal is therefore reported as the stale-log
        # internal error (13) as soon as the evidence is there, instead of
        # burning the whole --timeout budget and being reported as a
        # timeout (4).
        check_log_inode_freshness

        if [[ -f "$smapi_log" ]] && log_has_sentinel; then
            if sentinel_is_fresh "$launch_t0"; then
                log_event INFO "run sentinel observed in the SMAPI log"
                # FR-002: the log the run reads MUST be a log this run
                # produced — the inode check is the third freshness control.
                verify_log_freshness
                return 0
            fi
        fi

        if ! kill -0 "$game_pid" 2>/dev/null; then
            log_event WARN "the game exited before the run sentinel appeared"
            return 1
        fi

        if (( $(date +%s) >= deadline )); then
            die "$EX_TIMEOUT" \
                "no SMAPI run sentinel appeared within ${timeout_secs}s of the launch" \
                "$smapi_log" \
                "raise --timeout, or check the run log for why SMAPI did not start"
        fi

        sleep 0.2
    done
}

# The crash file is a COPY of the SMAPI log, so it is parsed with the same
# record shape: the level and the source are bound BY LABEL, and a
# continuation line carries no prefix and therefore no level.
#
# The program finds the first ERROR/ALERT record whose SOURCE is the mod and
# prints it together with its continuation lines. It prints nothing when no
# such record matched — the caller then falls back to the trailing records
# of the stream.
# shellcheck disable=SC2016  # the awk program is single-quoted ON PURPOSE: its $0/$1 must reach awk unexpanded
readonly AWK_CRASH_CONTEXT='
BEGIN {
    matched = 0
    rec_count = 0
}
{
    if (match($0, /^\[[^]]*\] /)) {
        prefix = substr($0, 1, RLENGTH)
        text = substr($0, RLENGTH + 1)
        inner = substr(prefix, 2, length(prefix) - 3)
        sp = index(inner, " ")
        rest = substr(inner, sp + 1)
        lvl = ""
        src = ""
        if (match(rest, /^[A-Za-z]+/)) {
            lvl = substr(rest, 1, RLENGTH)
            after = substr(rest, RLENGTH + 1)
            sub(/^ +/, "", after)
            src = after
            sub(/^screen_[0-9]+ +/, "", src)
        }
        is_record = 1
    } else {
        lvl = ""
        src = ""
        text = $0
        is_record = 0
    }

    rec_text[++rec_count] = text
    rec_lvl[rec_count] = lvl
    rec_src[rec_count] = src

    if (is_record && !matched && (lvl == "ERROR" || lvl == "ALERT") && src == mod) {
        matched = 1
        matched_rec = rec_count
    }
}
END {
    if (!matched) {
        exit 0
    }
    print rec_text[matched_rec]
    for (i = matched_rec + 1; i <= rec_count; i++) {
        if (rec_lvl[i] != "") {
            break
        }
        print rec_text[i]
    }
}
'

# The line the trailing window of the crash stream starts on.
#
# T099: the window is the trailing RECORDS of the normalised stream, capped
# at 30 records — a record is a prefixed line together with the continuation
# lines that follow it. The start line is therefore the first line of the
# 30th record from the end, NOT a fixed physical-line offset: a .NET stack
# trace is routinely 20-40 lines long, so a physical-line window can consist
# entirely of stack frames while omitting the record that names the failure
# (FR-007).
#
# The program prints nothing when the stream holds no record at all.
# shellcheck disable=SC2016  # the awk program is single-quoted ON PURPOSE: its $0/$1 must reach awk unexpanded
readonly AWK_CRASH_WINDOW='
BEGIN {
    rec_total = 0
}
{
    if (match($0, /^\[[^]]*\] /)) {
        rec_total++
        rec_first[rec_total] = NR
    }
}
END {
    if (rec_total == 0) {
        exit 0
    }
    start = rec_total - 29
    if (start < 1) {
        start = 1
    }
    print rec_first[start]
}
'

# check_crash_file
#
# The crash-override rule (FR-006, the data-model's Crash File entity):
# SMAPI-crash.txt, when present, overrides SMAPI-latest.txt and indicates
# FAILURE. The pre-flight deletes a leftover crash file, so a crash file
# present after the launch belongs to THIS run — a crash after the
# mod-resolution header must not be reported as a success.
check_crash_file() {
    local crash_file crash_context="" crash_window_start="" normalized_crash

    # T120: the crash file lives in the CANONICAL log directory — the same
    # directory the pre-flight removed a leftover crash file from.
    crash_file="${smapi_log_dir_real:-$(dirname -- "$smapi_log")}/SMAPI-crash.txt"

    [[ -f "$crash_file" ]] || return 0

    log_event ERROR "SMAPI wrote a crash log: ${crash_file}"

    # FR-007: the context is the matched failure record TOGETHER WITH ITS
    # CONTINUATION LINES — a fixed `tail -N` window MUST NOT be used, because
    # a .NET stack trace is routinely 20-40 lines long and such a window can
    # consist entirely of stack frames while omitting the record that names
    # the failure. The crash file is a copy of the SMAPI log, so it is
    # normalised the same way the main log is (the trailing CR is stripped)
    # and the context is read from that normalised record stream.
    normalized_crash="${temp_dir}/smapi-crash.normalized"
    sed 's/\r$//' -- "$crash_file" >"$normalized_crash" 2>/dev/null || true

    crash_context="$(awk -v mod="$mod_name" -- "$AWK_CRASH_CONTEXT" "$normalized_crash" 2>/dev/null || printf '')"

    if [[ -z "$crash_context" ]]; then
        # T099: no failure record matched, so the context is the trailing
        # RECORDS of the normalised stream, capped at 30 records. The window
        # is computed on the RECORD stream — a record is a prefixed line
        # together with the continuation lines that follow it — so the stack
        # frames that follow the failure can never squeeze the record that
        # names it out of the context. A fixed `tail -n 30` window over the
        # raw file could do exactly that (FR-007).
        crash_window_start="$(awk -- "$AWK_CRASH_WINDOW" "$normalized_crash" 2>/dev/null || printf '')"
        if [[ -n "$crash_window_start" ]]; then
            # The window is read from the crash file itself and the trailing
            # CR is stripped, so the context is the normalised text of
            # exactly those records (FR-003).
            crash_context="$(tail -n +"${crash_window_start}" -- "$crash_file" 2>/dev/null | sed 's/\r$//' || printf '')"
        fi
    fi

    die "$EX_MOD_LOAD_FAILURE" \
        "the game crashed during the run" \
        "$crash_file" \
        "read the crash log above — the crash overrides the mod-resolution output per FR-006" \
        "$crash_context"
}

wait_for_mod_resolution() {
    local deadline="$1"

    while :; do
        # T122: FR-002's inode control is evaluated on EVERY poll of BOTH
        # phases of the readiness wait, independently of the sentinel wait.
        # The second phase is polled here as well, so a log that is replaced
        # between the two phases is caught by the same control.
        check_log_inode_freshness

        if log_has_mod_resolution; then
            log_event INFO "SMAPI's mod-resolution output observed"
            return 0
        fi

        if ! kill -0 "$game_pid" 2>/dev/null; then
            log_event WARN "the game exited before SMAPI resolved any mods"
            return 1
        fi

        if (( $(date +%s) >= deadline )); then
            die "$EX_TIMEOUT" \
                "SMAPI did not resolve any mods within ${timeout_secs}s of the launch" \
                "$smapi_log" \
                "raise --timeout, or check the run log — a graphics-device failure also stops SMAPI before it resolves mods"
        fi

        sleep 0.2
    done
}

# The mod's own success marker, matched against a record's message.
#
# T162: the same exact, case-sensitive match the log gate uses, so the
# probe and the gate agree on what the marker is. The apostrophes are
# written as \047 because this program lives inside a single-quoted shell
# string.
# shellcheck disable=SC2016  # the awk program is single-quoted ON PURPOSE: its $0/$1 must reach awk unexpanded
readonly AWK_MOD_SUCCESS_MARKER='
BEGIN {
    marker_pat = "^The \047" mod "\047 mod was loaded successfully!$"
    found = 0
}
{
    # T162: only the records that FOLLOW the sentinel of this run are
    # examined, exactly like the mod-resolution probe: a marker a previous
    # run left in the log must not be mistaken for the marker of this run.
    if (NR <= (sentinel_line + 0)) {
        next
    }
    if (match($0, /^\[[^]]*\] /)) {
        text = substr($0, RLENGTH + 1)
        # The log is written with CRLF terminators, and the trailing CR
        # would defeat the `$` anchor of the pattern.
        sub(/\r$/, "", text)
        if (text ~ marker_pat) {
            found = 1
            exit
        }
    }
}
END { exit(found ? 0 : 1) }
'

# log_has_mod_success_marker — does the log carry the mod's own success
# marker, after this run's sentinel?
#
# T162: the probe is scoped to the region that follows the run sentinel,
# for the same reason the mod-resolution probe is.
log_has_mod_success_marker() {
    [[ -n "$run_sentinel_line" ]] || return 1
    [[ -f "$smapi_log" ]] || return 1

    awk -v mod="$mod_name" -v sentinel_line="$run_sentinel_line" \
        -- "$AWK_MOD_SUCCESS_MARKER" "$smapi_log" 2>/dev/null
}

# wait_for_mod_success_marker <readiness-deadline>
#
# T162: the bounded wait for the mod's own success marker.
#
# The marker is emitted by the mod from its `GameLaunched` handler, which
# SMAPI reaches only AFTER it resolved the mods — so the marker lands in
# the window between the mod-resolution header and the termination, and
# the log the gate read at the header does not carry it yet. The wait
# polls the log again for that window.
#
# The marker is a SECONDARY, non-gating signal (FR-003, FR-006): the wait
# ends when the marker is seen, when the game exits, or when the window
# expires, and the verdict NEVER depends on it. The function therefore
# always returns 0 — a non-zero return would be a failed command, and
# the ERR trap would report it as an internal error.
wait_for_mod_success_marker() {
    local deadline="$1"
    local now window_deadline window_secs=0 max_polls=0 poll_now

    # The window is bounded twice: by the constant and by the readiness
    # deadline, so the wait can never push the run past the
    # launch-to-verdict budget FR-009 defines.
    now="$(date +%s 2>/dev/null || printf '0')"
    window_deadline=$(( now + MOD_SUCCESS_MARKER_WINDOW_SECS ))
    if (( window_deadline > deadline )); then
        window_deadline="$deadline"
    fi
    window_secs=$(( window_deadline - now ))
    if (( window_secs < 0 )); then
        window_secs=0
    fi

    # The poll count is the loop's backstop: the window is a number of
    # 0.2 s sleeps, so the loop ends even when the clock cannot be read.
    max_polls=$(( window_secs * 5 + 1 ))

    # Without the mod-resolution header there is no window to wait in: the
    # marker cannot appear before SMAPI resolved the mods.
    if ! log_has_mod_resolution; then
        log_event INFO "the mod's own success marker was not waited for: SMAPI never resolved any mods"
        return 0
    fi

    while (( max_polls > 0 )); do
        max_polls=$(( max_polls - 1 ))

        if log_has_mod_success_marker; then
            log_event INFO "the mod's own success marker was observed in the SMAPI log"
            return 0
        fi

        # The marker is written by the game, so a game that has exited
        # cannot write it any more.
        if ! kill -0 "$game_pid" 2>/dev/null; then
            log_event INFO "the game exited before the mod's own success marker appeared"
            return 0
        fi

        poll_now="$(date +%s 2>/dev/null || printf '0')"
        if (( poll_now >= window_deadline )); then
            break
        fi

        sleep 0.2
    done

    log_event INFO "the mod's own success marker did not appear within ${window_secs}s — it is a secondary signal and does not gate the verdict"
    return 0
}

game_exit_status=0

terminate_game() {
    local deadline status=0

    if [[ -n "$game_pgid" ]]; then
        assert_signallable_group "$game_pgid" "game"
        log_event INFO "terminating the game process group ${game_pgid} (pid ${game_pid}): sending SIGTERM to the group"
        signal_group "$game_pgid" TERM

        log_event INFO "the game process group ${game_pgid} has the grace period of ${grace_period_secs}s to exit"
        deadline=$(( $(date +%s) + grace_period_secs ))
        while (( $(date +%s) < deadline )); do
            if ! kill -0 -- "-${game_pgid}" 2>/dev/null; then
                break
            fi
            sleep 0.2
        done

        if kill -0 -- "-${game_pgid}" 2>/dev/null; then
            log_event WARN "the game process group ${game_pgid} (pid ${game_pid}) is still alive after the grace period of ${grace_period_secs}s — sending SIGKILL to the group"
            signal_group "$game_pgid" KILL
        fi
    fi

    # FR-005 / T008: the child is waited on with `wait -f "$pid"`.
    status=0
    reap_child "$game_pid" "game" "$(( grace_period_secs + 5 ))" || status=$?
    log_event INFO "the game exited with status ${status}"
    game_exit_status="$status"
}

phase_launch() {
    # T103 (lint): `game_status` was a write-only local — nothing ever read
    # it. The constitution's Principle V (Simplicity and YAGNI) forbids
    # keeping it, exactly as T088 removed the three locals of the same
    # kind.
    local deadline
    local launch_stamp

    phase_begin "launch"

    # T161: the launch instant is captured ONCE, in ONE `date` invocation, in
    # two forms: the epoch FR-002's freshness control compares the sentinel
    # against, and the UTC stamp the run log's other timestamps use. One
    # invocation, so the two forms describe the same moment.
    launch_stamp="$(date -u '+%s %Y-%m-%dT%H:%M:%SZ')"
    launch_t0="${launch_stamp%% *}"
    launch_iso="${launch_stamp#* }"
    deadline=$(( launch_t0 + timeout_secs ))

    # T161: the run log MUST carry the launch epoch the freshness comparison
    # actually used — the value taken immediately before the game is
    # launched — so the threshold is reconstructable from the run log alone
    # (FR-002, FR-014, SC-014). The epoch is the value the comparison uses;
    # the UTC form is the same instant, readable on a timeline.
    log_event INFO "launch timestamp: ${launch_iso}"
    log_event INFO "launch epoch: ${launch_t0}"

    launch_game

    # The launch phase is closed before the readiness wait supersedes it, so
    # every phase contributes its own duration (FR-014).
    phase_end

    phase_begin "readiness wait"

    if ! wait_for_sentinel "$deadline" "$launch_t0"; then
        terminate_game
        local mapped
        mapped="$(translate_child_status "$game_exit_status" game)"
        if ((game_exit_status == 0)); then
            # A clean exit that produced no log is the "no log" case, not
            # the success case (FR-002).
            mapped="$EX_NO_LOG_PRODUCED"
        fi
        die "$mapped" \
            "the game exited (status ${game_exit_status}) without producing a SMAPI log" \
            "$smapi_log" \
            "re-run the SMAPI installer in the game directory, then re-run"
    fi

    wait_for_mod_resolution "$deadline" || true

    # T162: the mod's own success marker is emitted by the mod from its
    # `GameLaunched` handler, which SMAPI reaches only AFTER it resolved
    # the mods — so the marker lands after the mod-resolution header and
    # before the termination, and the log the gate reads at the header
    # does not carry it yet. The wait for it is bounded and never gates
    # the verdict (FR-003, FR-006).
    wait_for_mod_success_marker "$deadline"

    # A crash after the mod-resolution header is still a failure: the crash
    # file overrides the mod-resolution output (FR-006).
    check_crash_file

    phase_end

    phase_log_gate

    phase_begin "termination"
    terminate_game
    phase_end
}

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

main() {
    # T101: the command line is parsed FIRST, so that `--help`, `--version`
    # and a usage error are answered BEFORE the host-tool sweep runs. On a
    # host that is missing one of the tools, the usage reference and the
    # exit-14 usage error must still be reachable — the sweep used to
    # preempt the parser, so both of them reported the missing tool (9)
    # instead (FR-012 item 8, FR-015).
    #
    # The parser itself needs no external command: `print_help` and the
    # `--version` line are built from the shell's own builtins, so the
    # usage paths stay reachable on a host without a single one of the
    # tools the sweep looks for. The failure report's clock is read
    # DEFENSIVELY (`date` may be missing as well), so the exit-14 usage
    # error is reported even on a host that has no `date` at all.
    parse_args "$@"

    # FR-012 item 8: the host-tool sweep runs before the run log is created
    # and before anything else invokes an external command. The run log's
    # own creation uses several of the tools (date, mkdir, chmod), and the
    # script's own directory is derived with the shell's parameter
    # expansion, so a missing tool is reported as a missing host tool (9) and
    # never as the shell's own 127 or as an internal error (13).
    preflight_tool_sweep

    # The wall time covers the whole run, including a failure before the run
    # log exists.
    run_started_epoch="$(date +%s)"

    if [[ -z "${HOME:-}" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "HOME is not set in the environment" \
            "environment" \
            "export HOME=\"/home/<user>\" and re-run"
    fi

    apply_defaults

    # The run log is created only after the command line has been accepted,
    # so a usage error never creates it.
    run_log_init

    # FR-012 (0) pins the order: pre-flight (tools, game, SMAPI, Steam,
    # crash markers) -> display -> build-and-verify -> flock ->
    # existing-process check -> log baseline -> launch.
    phase_preflight
    phase_display
    phase_build
    phase_lock
    phase_conflict_check
    phase_log_baseline
    phase_launch

    # The crash file overrides the mod-resolution output: re-check it right
    # before the verdict, so a crash in the last moments of the run is not
    # reported as a success (FR-006).
    check_crash_file

    # FR-010 / FR-006: the cleanup runs BEFORE the verdict, so a cleanup
    # failure can change the exit code without a success verdict already
    # being on stdout. The EXIT trap's cleanup is idempotent (it is guarded
    # on `cleanup_done`), so the trap's second call is a no-op.
    cleanup

    if ((cleanup_pgid_assertion_failed == 1)); then
        die "$EX_INTERNAL_ERROR" \
            "the cleanup refused to signal a process group" \
            "the run log names the refused group" \
            "this is a bug in ${SCRIPT_NAME} — report it with the run log attached"
    fi

    print_summary "SUCCESS: Mod loaded without errors." "$EX_OK"
    log_event INFO "run ${run_id} finished successfully"
    log_event INFO "total wall time: $(run_elapsed_seconds)s"
    exit "$EX_OK"
}

main "$@"
