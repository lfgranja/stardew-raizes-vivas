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
# Status: skeleton — the option parser, --help and --version only. The run
#         pipeline (pre-flight, display, build, log gate, launch) is still to
#         be implemented; see the phase placeholders near the bottom of this
#         file.
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

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR
readonly REPO_ROOT="${SCRIPT_DIR}/.."

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

    {
        printf 'ERROR: smoke validation failed (exit %s).\n' "$code"
        printf '  What: %s\n' "$what"
        printf '  Where: %s\n' "$where"
        printf '  Fix: %s\n' "$fix"
    } >&2

    exit "$code"
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
on_error() {
    local status="$1"
    local line="$2"
    local func="$3"

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

    case "$path" in
        '~') printf '%s\n' "${HOME:-}" ;;
        '~/'*) printf '%s\n' "${HOME:-}/${path:2}" ;;
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

    set -- "${argv[@]}"

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

    if [[ -z "$mod_project" ]]; then
        mod_project="${REPO_ROOT}/LivingRoots/LivingRoots.csproj"
    fi

    if [[ -z "$smapi_log" ]]; then
        smapi_log="${HOME}/.config/StardewValley/ErrorLogs/SMAPI-latest.txt"
    fi
}

# print_resolved_configuration
#
# Prints the configuration the run would use. It makes the parser observable
# — the CLI tests assert on the resolved values, and a maintainer can see what
# the defaults resolved to without reading the source — and it is the shape
# the run log's header will take once T006 adds the run log.
print_resolved_configuration() {
    printf 'resolved configuration:\n'
    printf '  game-path:     %s\n' "$game_path"
    printf '  mods-path:     %s\n' "$mods_path"
    printf '  mod-project:   %s\n' "$mod_project"
    printf '  smapi-log:     %s\n' "$smapi_log"
    printf '  timeout:       %s s\n' "$timeout_secs"
    printf '  build-timeout: %s s\n' "$build_timeout_secs"
    printf '  grace-period:  %s s\n' "$grace_period_secs"

    if ((force == 1)); then
        printf '  force:         on\n'
    else
        printf '  force:         off\n'
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

    printf '\nExit codes:\n'
    render_exit_codes
    printf '\n  Code 8 is retired and is never reused.\n'
}

# ---------------------------------------------------------------------------
# Phases — NOT YET IMPLEMENTED.
#
# The skeleton above is complete; everything below is the remaining pipeline.
# Each phase gets its own function so the next implementation step has an
# obvious home, and the ordering is fixed by FR-012 (0):
#
#   T004  exit-code taxonomy and the FR-007 status translation table
#   T005  path handling (realpath canonicalisation, component-boundary test)
#   T006  the run log (run id, header, modes, per-phase timings)
#   T007  the lock file (exec 9<> + flock -n 9)
#   T008  the trap design (EXIT/INT/TERM/HUP)
#   T013  pre-flight: host tools, game install, SMAPI markers, Steam
#         prerequisite, stale-log and marker removal
#   T014  the virtual display (Xvfb -displayfd) and the software-OpenGL probe
#   T015  build-and-verify (dotnet build -p:GameModsPath=..., SHA-256 compare)
#   T016  the log gate and the verdict (SUCCESS: Mod loaded without errors.)
#   T017  the launch, the two-phase wait and the termination
# ---------------------------------------------------------------------------

# phase_preflight — T013
phase_preflight() {
    :
}

# phase_display — T014
phase_display() {
    :
}

# phase_build — T015
phase_build() {
    :
}

# phase_log_gate — T016
phase_log_gate() {
    :
}

# phase_launch — T017
phase_launch() {
    :
}

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

main() {
    parse_args "$@"

    # The defaults are derived from $HOME, so the variable must exist before
    # they are computed. `--help` and `--version` short-circuit above and
    # therefore still work without it.
    if [[ -z "${HOME:-}" ]]; then
        die "$EX_INTERNAL_ERROR" \
            "HOME is not set in the environment" \
            "environment" \
            "export HOME=\"/home/<user>\" and re-run"
    fi

    apply_defaults

    phase_preflight
    phase_display
    phase_build
    phase_log_gate
    phase_launch

    # The phases above are placeholders. Returning 0 here would be a FALSE
    # PASS — exit 0 means "the mod loaded without errors" — so the skeleton
    # aborts explicitly instead, after reporting what it resolved.
    print_resolved_configuration >&2

    die "$EX_INTERNAL_ERROR" \
        "the smoke-validation pipeline is not implemented in this build (option parsing, --help and --version only)" \
        "${SCRIPT_NAME} — the pre-flight, display, build, log-gate and launch phases are pending" \
        "see specs/008-smoke-validation/tasks.md (T013-T017) and re-run once the pipeline is implemented"
}

main "$@"
