#!/usr/bin/env bash
#
# scripts/tests/test-cli.sh — the command-line surface of scripts/smoke-validation.sh.
#
# What this file covers:
#
#   TC-18  `--version` prints exactly "<name> <VERSION>" on stdout, exits 0 and
#          writes nothing to stderr; `--help` exits 0 with an empty stderr; and
#          the `-h` alias produces byte-identical output to `--help`.
#
#   TC-19  A rejected command line is a usage error: exit 14, a report that
#          names the offending option, and NOTHING launched — no run-log
#          directory under the sandbox's state root, no SMAPI log, no game
#          installation touched and no launch/display activity in the report.
#          The rejected forms are: an unknown option, a malformed numeric
#          value, a non-positive numeric value, a missing required argument
#          and a stray positional argument.
#
# Plus eight properties:
#
#   1. the option-surface property: every row of the spec's normative
#      "Command-Line Options" table is rendered by `--help` with the same option
#      spelling, the same argument placeholder and the same default, and the
#      FR-015 sections (usage line, environment, run log, run id, version and
#      the whole exit-code table) are present;
#
#   2. the no-abbreviation property: only exact long-option names are accepted:
#      `--mod` and `--mods` are rejected with 14, while the exact name
#      `--mod-project` is not rejected as unknown.
#
#   3. the `--opt=VALUE` equivalence: the `--opt=VALUE` form is accepted
#      wherever `--opt VALUE` is, and it reaches the same validator.
#
#   4. the example-form property (T077): every option occurrence in the
#      `--help` examples uses the `--opt=VALUE` form.
#
#   5. the relative-`--mods-path` rule (T068): a relative `--mods-path` is
#      resolved against the game directory, and the rule applies to a
#      user-supplied value only, never to the derived default.
#
#   6. the bounded-invocation property (T128): every invocation of the script
#      under test runs under a hard deadline, so a script that hangs, and the
#      child (and the X server) it left behind, cannot outlive the suite.
#
#   7. the single-failure-report property (T138): a failing command
#      substitution produces EXACTLY ONE failure report on stderr, and the run
#      exits with that report's own exit code — both for the run's start
#      timestamp and for the sentinel's timestamp extraction, whose grep
#      matches NOTHING when the sentinel carries no parseable timestamp;
#
#   8. the ERR-trap exit-code property (T149): the ERR-trap path always exits
#      13, even when a command inside the handler fails.
#
# Style: the shared harness is sourced (its assertions are used, never
# re-implemented) and every invocation of the script under test goes through
# the suite's `run_cli` helper, which applies the hard deadline, so the script
# always runs inside the per-test sandbox and never touches the real game
# installation or the real state roots.

set -Eeuo pipefail

# The harness lives next to this file.
#
# The harness is linted IN ITS OWN RIGHT by the same hook step (it matches
# `scripts/tests/**/*.sh`), so it is deliberately not followed from here:
# `-x` would pull it in a second time and report its findings against this
# file.
# shellcheck disable=SC1091  # the harness is linted separately, by the same step
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/harness.sh"

# ---------------------------------------------------------------------------
# Expectations
# ---------------------------------------------------------------------------

# The script's own name, as the caller sees it on the command line.
# The declaration and the assignment are separate: `readonly X="$(cmd)"` masks
# the exit status of `cmd` (SC2155).
CLI_SCRIPT_NAME="$(basename -- "$HARNESS_SCRIPT")"
readonly CLI_SCRIPT_NAME

# The version the script reports (the spec's first release).
readonly CLI_EXPECTED_VERSION="0.1.0"

# The run-log directory the script MUST NOT create when the command line is
# rejected. FR-014 puts the run log under ${XDG_STATE_HOME:-$HOME/.local/state},
# and the harness points XDG_STATE_HOME inside the sandbox.
readonly CLI_RUN_LOG_DIR="${HARNESS_XDG_STATE_HOME}/living-roots-smoke"

# The default SMAPI log path, inside the sandboxed HOME. A rejected command
# line must not create it.
readonly CLI_SMAPI_LOG="${HARNESS_HOME}/.config/StardewValley/ErrorLogs/SMAPI-latest.txt"

# The default game installation, inside the sandboxed HOME. A rejected command
# line must not create it (a build or a launch would).
readonly CLI_GAME_DIR="${HARNESS_HOME}/.steam/steam/steamapps/common/Stardew Valley"

# Words that would betray a launch (or a virtual display being started) in a
# report. A usage error mentions none of them.
readonly CLI_LAUNCH_INDICATORS=("launch" "Launch" "Xvfb" "xvfb" "DISPLAY")

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

# The hard deadline, in seconds, applied to EVERY invocation of the script
# under test (T128). It is deliberately larger than any budget the script
# enforces itself, so the script's own verdict is what a run reports; the
# deadline only fires when the script under test HANGS.
#
# The deadline is a HARNESS-level guard: `timeout` runs the script in its own
# process group and signals the group, so a hung script can neither outlive
# the suite nor leak the child it started (the X server among them).
CLI_DEADLINE_SECS=120

# run_cli [args...] — run the script under test under the suite's hard
# deadline.
#
# The harness's `run_script_deadline` helper is the invocation path: it wraps
# the call in `timeout --kill-after=5 <deadline>`, so a script that hangs is
# ended by the deadline and reports `timeout`'s own 124 — a code that is in
# no FR-007 category, so a deadline that fired is visible as such.
run_cli() {
    run_script_deadline "$CLI_DEADLINE_SECS" "$@"
}

# run_cli_env <env-assignments...> -- [args...]
#
# Like run_cli, but with explicit environment assignments (e.g. PATH=...).
#
# The harness's `run_script_deadline` takes no environment assignments, so
# the assignments are exported into THIS shell for the duration of the
# invocation: the harness hands the child the environment of this shell (plus
# its own sandbox overrides for HOME and the XDG roots), so an exported PATH
# reaches the script under test. Every exported name is restored afterwards,
# so the suite's own environment is unchanged by the call.
run_cli_env() {
    local -a envs=()
    local -a args=()
    local seen=0
    local item
    local name
    local i

    for item in "$@"; do
        if (( seen == 1 )); then
            args+=("$item")
        elif [[ "$item" == "--" ]]; then
            seen=1
        else
            envs+=("$item")
        fi
    done

    local -a names=()
    local -a values=()
    local -a was_set=()
    local -a saved=()

    for item in "${envs[@]+"${envs[@]}"}"; do
        name="${item%%=*}"
        names+=("$name")
        values+=("${item#*=}")
        if [[ -v "$name" ]]; then
            was_set+=(1)
            saved+=("${!name}")
        else
            was_set+=(0)
            saved+=("")
        fi
    done

    for i in "${!names[@]}"; do
        export "${names[$i]}=${values[$i]}"
    done

    run_script_deadline "$CLI_DEADLINE_SECS" "${args[@]+"${args[@]}"}"

    for i in "${!names[@]}"; do
        if (( was_set[i] == 1 )); then
            export "${names[$i]}=${saved[$i]}"
        else
            unset -v "${names[$i]}" 2>/dev/null || true
        fi
    done

    return 0
}

# assert_nothing_launched <label>
#
# Asserts that the last `run_script` invocation launched nothing: no run-log
# directory, no SMAPI log, no game installation, and no launch/display
# activity in the report on stderr.
assert_nothing_launched() {
    local label="${1-}"
    local indicator=""

    assert_file_absent "$CLI_RUN_LOG_DIR" \
        "$label: no run-log directory was created under the sandbox state root"
    assert_file_absent "$CLI_SMAPI_LOG" \
        "$label: no SMAPI log was created"
    assert_file_absent "$CLI_GAME_DIR" \
        "$label: no game installation was created"

    for indicator in "${CLI_LAUNCH_INDICATORS[@]}"; do
        assert_not_contains "$RUN_STDERR" "$indicator" \
            "$label: the report does not mention '$indicator'"
    done
}

# ---------------------------------------------------------------------------
# TC-18 — --version, --help and the -h alias
# ---------------------------------------------------------------------------

run_cli --version
assert_exit 0 "--version exits 0"
assert_eq "${CLI_SCRIPT_NAME} ${CLI_EXPECTED_VERSION}" "$RUN_STDOUT" \
    "--version prints exactly '<name> <VERSION>' on stdout"
assert_eq "" "$RUN_STDERR" "--version writes nothing to stderr"

# The capture strips the trailing newline, so a second output line would still
# be counted here.
CLI_VERSION_LINES="$(printf '%s\n' "$RUN_STDOUT" | wc -l | tr -d ' ')"
assert_eq "1" "$CLI_VERSION_LINES" "--version prints exactly one line"

CLI_VERSION_REGEX="^${CLI_SCRIPT_NAME//./\\.} ${CLI_EXPECTED_VERSION//./\\.}\$"
assert_matches "$RUN_STDOUT" "$CLI_VERSION_REGEX" \
    "--version stdout matches '<name> <VERSION>'"

run_cli --help
assert_exit 0 "--help exits 0"
assert_eq "" "$RUN_STDERR" "--help writes nothing to stderr"
CLI_HELP="$RUN_STDOUT"

run_cli -h
assert_exit 0 "-h exits 0"
assert_eq "$RUN_STDOUT" "$CLI_HELP" "-h stdout is byte-identical to --help stdout"
assert_eq "" "$RUN_STDERR" "-h writes nothing to stderr"

# ---------------------------------------------------------------------------
# TC-19 — usage errors exit 14, name the option and launch nothing
# ---------------------------------------------------------------------------

run_cli --no-such-option
assert_exit 14 "an unknown option exits 14"
assert_contains "$RUN_STDERR" "--no-such-option" \
    "the report names the unknown option"
assert_eq "" "$RUN_STDOUT" "a usage error writes nothing to stdout"
assert_nothing_launched "an unknown option"

run_cli --timeout abc
assert_exit 14 "a malformed --timeout value exits 14"
assert_contains "$RUN_STDERR" "--timeout" \
    "the report names the option with the malformed value"
assert_eq "" "$RUN_STDOUT" "a usage error writes nothing to stdout"
assert_nothing_launched "a malformed --timeout value"

run_cli --timeout 0
assert_exit 14 "a non-positive --timeout value exits 14"
assert_contains "$RUN_STDERR" "--timeout" \
    "the report names the option with the non-positive value"
assert_eq "" "$RUN_STDOUT" "a usage error writes nothing to stdout"
assert_nothing_launched "a non-positive --timeout value"

run_cli --game-path
assert_exit 14 "a missing required argument exits 14"
assert_contains "$RUN_STDERR" "--game-path" \
    "the report names the option whose argument is missing"
assert_eq "" "$RUN_STDOUT" "a usage error writes nothing to stdout"
assert_nothing_launched "a missing required argument"

run_cli somearg
assert_exit 14 "a stray positional argument exits 14"
assert_contains "$RUN_STDERR" "somearg" \
    "the report names the stray positional argument"
assert_eq "" "$RUN_STDOUT" "a usage error writes nothing to stdout"
assert_nothing_launched "a stray positional argument"

# ---------------------------------------------------------------------------
# The option-surface property — --help renders the spec's normative table
# ---------------------------------------------------------------------------

# The usage line is the first line of the help.
CLI_USAGE_LINE="${CLI_HELP%%$'\n'*}"

assert_contains "$CLI_HELP" "Usage:" "the help opens with a Usage: line"
assert_contains "$CLI_HELP" "Options:" "the help has an Options: section"
assert_contains "$CLI_HELP" "Environment (set automatically)" \
    "the help has the Environment (set automatically) section"
assert_contains "$CLI_HELP" "managed by the script" \
    "the Environment section states that the variables are script-managed"
assert_contains "$CLI_HELP" "not user-configurable" \
    "the Environment section states that the variables are not user-configurable"

# Every row of the spec's "Command-Line Options (normative)" table:
# option spelling | argument placeholder | default.
#
# The table is the source of truth; --help is its rendering. The rows are held
# in an array (rather than a heredoc) because the count of the rows is itself
# an expectation: FR-015 requires EXACTLY one entry per row and no option that
# is not in the table.
CLI_OPTION_TABLE=(
    "--game-path|DIR|~/.steam/steam/steamapps/common/Stardew Valley/"
    "--mods-path|DIR|<game-path>/Mods"
    "--mod-project|FILE|<repo-root>/LivingRoots/LivingRoots.csproj"
    "--timeout|SECONDS|120"
    "--build-timeout|SECONDS|300"
    "--grace-period|SECONDS|15"
    "--smapi-log|FILE|~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt"
    "--force|none — flag|off"
    "--help|none — flag|—"
    "-h|none — flag|—"
    "--version|none — flag|—"
)

# The Options: section of the help — the rendering of the table above. The
# checks are scoped to it, because the Usage: line repeats every option and
# would otherwise satisfy a containment check for an option whose entry is
# missing from the table.
CLI_OPTIONS_SECTION="${CLI_HELP#*Options:}"
CLI_OPTIONS_SECTION="${CLI_OPTIONS_SECTION%%Environment (set automatically):*}"

# The spellings the table declares, as a space-separated list.
CLI_OPTION_NAMES=""
for cli_row in "${CLI_OPTION_TABLE[@]}"; do
    IFS='|' read -r cli_opt cli_arg cli_default <<< "$cli_row"
    CLI_OPTION_NAMES="${CLI_OPTION_NAMES} ${cli_opt}"
done

for cli_row in "${CLI_OPTION_TABLE[@]}"; do
    IFS='|' read -r cli_opt cli_arg cli_default <<< "$cli_row"

    assert_contains "$CLI_OPTIONS_SECTION" "$cli_opt" \
        "--help documents the option '$cli_opt'"
    assert_contains "$CLI_OPTIONS_SECTION" "$cli_arg" \
        "--help documents the argument placeholder '$cli_arg' of '$cli_opt'"
    assert_contains "$CLI_OPTIONS_SECTION" "$cli_default" \
        "--help documents the default '$cli_default' of '$cli_opt'"

    if [[ "$cli_arg" == "none — flag" ]]; then
        cli_rendered="${cli_opt} (${cli_arg})"
    else
        cli_rendered="${cli_opt} ${cli_arg}"
    fi
    assert_contains "$CLI_OPTIONS_SECTION" "$cli_rendered" \
        "--help renders the table row of '$cli_opt' as '$cli_rendered'"
    assert_contains "$CLI_USAGE_LINE" "$cli_opt" \
        "the Usage: line shows the option '$cli_opt'"
done

# Exactly one entry per table row, and no option that is not in the table.
CLI_OPTION_ENTRY_COUNT=0
CLI_DOCUMENTED_OPTIONS=""
while IFS= read -r cli_line; do
    if [[ "$cli_line" =~ ^[[:blank:]]+(-{1,2}[^[:space:]]+) ]]; then
        CLI_OPTION_ENTRY_COUNT=$((CLI_OPTION_ENTRY_COUNT + 1))
        CLI_DOCUMENTED_OPTIONS="${CLI_DOCUMENTED_OPTIONS} ${BASH_REMATCH[1]}"
    fi
done <<< "$CLI_OPTIONS_SECTION"

assert_eq "${#CLI_OPTION_TABLE[@]}" "$CLI_OPTION_ENTRY_COUNT" \
    "the Options: section has exactly one entry per row of the Command-Line Options table"

for cli_documented in $CLI_DOCUMENTED_OPTIONS; do
    if [[ " ${CLI_OPTION_NAMES} " == *" ${cli_documented} "* ]]; then
        pass "the Options: section documents only options from the normative table ('$cli_documented' is one of them)"
    else
        fail "the Options: section documents only options from the normative table" \
            "'$cli_documented' is not a row of the Command-Line Options table"
    fi
done

# The eight managed environment variables.
for cli_var in SMAPI_NO_TERMINAL SMAPI_DEVELOPER_MODE LIBGL_ALWAYS_SOFTWARE \
    GALLIUM_DRIVER SDL_VIDEODRIVER SDL_AUDIODRIVER ALSOFT_DRIVERS DISPLAY; do
    assert_contains "$CLI_HELP" "$cli_var" \
        "the Environment section names $cli_var"
done

# The run-log location, the run-ID format and the version.
# The single quotes are intentional: the assertion is about the LITERAL text
# the help prints, so `${XDG_STATE_HOME:-$HOME/.local/state}` must NOT be
# expanded by the shell that runs the assertion.
# shellcheck disable=SC2016  # the literal text is asserted, so it must not expand
assert_contains "$CLI_HELP" '${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log' \
    "the help gives the run-log location"
assert_contains "$CLI_HELP" "<UTC timestamp>-<PID>" \
    "the help gives the run-ID format"
assert_matches "$CLI_HELP" '[0-9]{8}T[0-9]{6}Z-[0-9]+' \
    "the help's run-ID example matches '<UTC>-<PID>'"
assert_contains "$CLI_HELP" "Version:" "the help has a Version: section"
assert_contains "$CLI_HELP" "$CLI_EXPECTED_VERSION" \
    "the help states the script VERSION"

# Every exit code of the FR-007 taxonomy, except the retired code 8.
assert_contains "$CLI_HELP" "Exit codes:" "the help has an Exit codes: section"
CLI_EXIT_SECTION="${CLI_HELP#*Exit codes:}"
for cli_code in 0 1 2 3 4 5 6 7 9 10 11 12 13 14 15 16 17 129 130 143; do
    # The code must appear as a whole token at the start of a line of the
    # exit-code table. The pattern is built in a variable on purpose: an
    # inline $'...' on the right-hand side of =~ would be matched literally.
    cli_code_regex=$'\n[[:blank:]]*'"${cli_code}"$'[[:blank:]]+[^0-9]'
    assert_matches "$CLI_EXIT_SECTION" "$cli_code_regex" \
        "the help documents exit code $cli_code"
done

# ---------------------------------------------------------------------------
# The no-abbreviation property — only exact long-option names are accepted
# ---------------------------------------------------------------------------

run_cli --mod
assert_exit 14 "the ambiguous abbreviation --mod is rejected with 14"
assert_contains "$RUN_STDERR" "--mod" \
    "the report names the ambiguous abbreviation"
assert_not_contains "$RUN_STDERR" "missing required argument" \
    "--mod is rejected because of the option NAME, not because of a missing value"
assert_nothing_launched "the ambiguous abbreviation --mod"

run_cli --mods
assert_exit 14 "the prefix --mods is rejected with 14"
assert_contains "$RUN_STDERR" "--mods" \
    "the report names the prefix"
assert_not_contains "$RUN_STDERR" "missing required argument" \
    "--mods is rejected because of the option NAME, not because of a missing value"
assert_nothing_launched "the prefix --mods"

# The abbreviations must also be rejected when a value follows them: an
# implementation that silently expanded --mod to --mods-path would accept
# these two invocations and only fail later, for a different reason.
run_cli --mod "$HARNESS_SANDBOX"
assert_exit 14 "the ambiguous abbreviation --mod with a value is rejected with 14"
assert_nothing_launched "the ambiguous abbreviation --mod with a value"

run_cli --mods "$HARNESS_SANDBOX"
assert_exit 14 "the prefix --mods with a value is rejected with 14"
assert_nothing_launched "the prefix --mods with a value"

# The exact name is accepted by the parser, so the run must not be a usage
# error. It fails later (the pipeline is not implemented yet), which is
# exactly the point: it fails for another reason than the name.
run_cli --mod-project "${HARNESS_SANDBOX}/no-such-project/LivingRoots.csproj"
assert_ne 14 "$RUN_CODE" \
    "the exact option name --mod-project is not a usage error"
assert_not_contains "$RUN_STDERR" "unknown option" \
    "the report does not call --mod-project an unknown option"
assert_not_contains "$RUN_STDERR" "abbreviat" \
    "the report does not call --mod-project an abbreviation"

# ---------------------------------------------------------------------------
# The --opt=VALUE form is equivalent to --opt VALUE
# ---------------------------------------------------------------------------

run_cli --timeout=90 --help
assert_exit 0 "--timeout=90 --help exits 0"
assert_eq "$CLI_HELP" "$RUN_STDOUT" \
    "--timeout=90 --help prints the same help as --help"
assert_contains "$RUN_STDOUT" "(default: 120)" \
    "--timeout=90 --help still shows the default timeout 120"

run_cli --timeout=abc
assert_exit 14 "--timeout=abc is rejected with 14"
assert_contains "$RUN_STDERR" "--timeout" \
    "the = form reaches the validator, which names the option"

# ---------------------------------------------------------------------------
# T077 — the --help examples use the --opt=VALUE form
# ---------------------------------------------------------------------------
#
# The spec's normative "Option-syntax rules" say: "The `--opt=VALUE` form is
# the one to use in examples and in the `--help` text." The property is
# therefore per-example-line: an example may name an option, but EVERY
# option occurrence on it must be in the `--opt=VALUE` form — never a bare
# `--opt VALUE` pair.
#
# The scan is line-oriented and token-oriented: a token that starts with
# `--`, is followed by letters and carries no `=` is a bare option
# occurrence; a token that carries an `=` is the `--opt=VALUE` form. Globbing
# is switched off around the scan, so a value that contains a wildcard is
# never expanded against the file system.
#
# The section is isolated first: the Usage: line repeats every option in the
# `[--opt VALUE]` bracket form, so a scan over the whole help would report
# the usage line as a violation.

CLI_EXAMPLES_SECTION="${CLI_HELP#*Examples:}"
CLI_EXAMPLES_SECTION="${CLI_EXAMPLES_SECTION%%Exit codes:*}"

assert_contains "$CLI_HELP" "Examples:" \
    "the help has an Examples: section"

CLI_EXAMPLE_COUNT=0
CLI_EXAMPLE_OPTION_COUNT=0

set -f
while IFS= read -r cli_example; do
    # A blank line is the separator between the section header and the
    # examples, not an example.
    [[ -n "${cli_example//[[:space:]]/}" ]] || continue
    CLI_EXAMPLE_COUNT=$((CLI_EXAMPLE_COUNT + 1))

    cli_bare=""
    cli_options=0
    for cli_token in ${cli_example}; do
        case "${cli_token}" in
            --*=*)
                # the --opt=VALUE form
                cli_options=$((cli_options + 1))
                ;;
            --[A-Za-z][A-Za-z0-9-]*)
                # a bare --opt occurrence: the VALUE follows in a separate token
                cli_options=$((cli_options + 1))
                cli_bare="${cli_bare} ${cli_token}"
                ;;
        esac
    done
    CLI_EXAMPLE_OPTION_COUNT=$((CLI_EXAMPLE_OPTION_COUNT + cli_options))

    if [[ -z "${cli_bare}" ]]; then
        pass "the example uses the --opt=VALUE form: ${cli_example}"
    else
        fail "the example uses the --opt=VALUE form: ${cli_example}" \
            "bare option occurrence(s):${cli_bare}"
    fi
done <<< "$CLI_EXAMPLES_SECTION"
set +f

if (( CLI_EXAMPLE_COUNT > 0 )); then
    pass "the help has at least one example"
else
    fail "the help has at least one example" "the Examples: section is empty"
fi

if (( CLI_EXAMPLE_OPTION_COUNT > 0 )); then
    pass "the examples name at least one option"
else
    fail "the examples name at least one option" \
        "no option occurrence was found in the Examples: section"
fi

# ---------------------------------------------------------------------------
# T068 — the relative --mods-path rule applies to a user-supplied value only
# ---------------------------------------------------------------------------
#
# FR-017: "A relative `--mods-path` value MUST be resolved against the game
# directory." The rule applies to a USER-SUPPLIED value only: the derived
# default is already anchored at the game directory, so applying the rule to
# the derived default as well DOUBLES the game directory — "--game-path ."
# yields "././Mods" instead of "./Mods".
#
# The resolved configuration is recorded by the run itself: the run log
# carries "mods-path: <path>" and the same line reaches stderr. The value
# under test is therefore read back from the run's own diagnostics, not
# recomputed by the test.
#
# The table:
#
#   | case               | --game-path      | --mods-path | expected          |
#   |--------------------|------------------|-------------|-------------------|
#   | relative game dir  | <relative>       | (default)   | "<relative>/Mods" |
#   | relative mods path | <absolute>       | "Mods"      | "<absolute>/Mods"|
#   | absolute mods path | <absolute>       | <absolute>  | <absolute>       |
#   | absolute default   | <absolute>       | (default)   | "<absolute>/Mods"|
#
# The "relative game dir" row is the only row whose --game-path is relative,
# and it used to be "." — the process's CURRENT WORKING DIRECTORY. Through the
# runner that is the repository root; run directly it is wherever the user
# happens to be. A relative --game-path is resolved against the working
# directory, so "." pointed a filesystem-mutating tool at an arbitrary
# directory OUTSIDE the sandbox. The row now points at a relative path that
# resolves INTO the harness sandbox: the path is computed from the working
# directory at run time, so it is correct wherever the suite is run from, and
# the directory it names is always the sandbox's own fixture.
#
# The runs use a failing Xvfb stub in front of the PATH, so a run that gets
# as far as the display phase never starts a real X server.

CLI_PATH_FIXTURE_ROOT="${FIXTURE_ROOT}/cli-paths"
CLI_ABSOLUTE_GAME="${CLI_PATH_FIXTURE_ROOT}/game"
CLI_ABSOLUTE_MODS="${CLI_PATH_FIXTURE_ROOT}/absolute-mods"

make_fake_game_dir "${CLI_ABSOLUTE_GAME}" || return 1
mkdir -p -- "${CLI_ABSOLUTE_MODS}"

CLI_STUB_BIN_DIR="${FIXTURE_ROOT}/cli-bin"
mkdir -p -- "${CLI_STUB_BIN_DIR}"
make_stub_xvfb "${CLI_STUB_BIN_DIR}"
readonly CLI_NO_DISPLAY_PATH="${CLI_STUB_BIN_DIR}:${PATH}"

# resolved_mods_path <stderr> — the resolved mods path the run recorded.
#
# The run logs its resolved configuration as
#
#   <timestamp> [INFO] [<run-id>] mods-path: <path>
#
# and the same line reaches stderr, so the value is read back from there.
resolved_mods_path() {
    local text="${1-}"
    local line

    while IFS= read -r line || [[ -n "$line" ]]; do
        case "$line" in
            *" mods-path: "*)
                printf '%s' "${line##* mods-path: }"
                return 0
                ;;
        esac
    done <<< "$text"

    return 1
}

# relative_to_cwd <absolute-path> — the path expressed relative to the
# current working directory.
#
# A relative path is resolved against the working directory, so the relative
# path this suite hands to the script under test is computed FROM the working
# directory: it names the same directory whichever directory the suite was
# started from.
relative_to_cwd() {
    local target="${1-}"
    local base="$PWD"
    local up=""
    local rest="$base"

    if [[ -z "$target" ]]; then
        return 1
    fi

    # The target is under the working directory: the relative path is the tail.
    if [[ "$target" == "$base/"* ]]; then
        printf '%s' "${target#"$base/"}"
        return 0
    fi

    # Otherwise: as many "../" steps as the working directory has levels,
    # followed by the target's own path.
    while [[ -n "$rest" && "$rest" != "/" ]]; do
        up="../${up}"
        rest="${rest%/*}"
    done

    printf '%s%s' "$up" "${target#/}"
}

# The relative --game-path the "relative game dir" row uses: the sandbox's own
# fake game installation, named relative to the working directory.
CLI_RELATIVE_GAME="$(relative_to_cwd "${CLI_ABSOLUTE_GAME}")" || CLI_RELATIVE_GAME="."

# | case | --game-path | --mods-path | expected resolved mods path |
CLI_MODS_PATH_TABLE=(
    "relative game dir|${CLI_RELATIVE_GAME}|(default)|${CLI_RELATIVE_GAME}/Mods"
    "relative mods path|${CLI_ABSOLUTE_GAME}|Mods|${CLI_ABSOLUTE_GAME}/Mods"
    "absolute mods path|${CLI_ABSOLUTE_GAME}|${CLI_ABSOLUTE_MODS}|${CLI_ABSOLUTE_MODS}"
    "absolute default|${CLI_ABSOLUTE_GAME}|(default)|${CLI_ABSOLUTE_GAME}/Mods"
)

# T152: the relative row's --game-path is RELATIVE, it is not the working
# directory, and it resolves to the sandbox's own fixture — so the case can
# never point a filesystem-mutating tool outside the sandbox.
case "$CLI_RELATIVE_GAME" in
    /*)
        fail "T152: the relative --game-path is a relative path" \
            "got the absolute path '${CLI_RELATIVE_GAME}'"
        ;;
    *)
        pass "T152: the relative --game-path is a relative path"
        ;;
esac

CLI_RELATIVE_GAME_RESOLVED="$(cd -- "$CLI_RELATIVE_GAME" 2>/dev/null && pwd -P || printf '')"
CLI_ABSOLUTE_GAME_RESOLVED="$(cd -- "${CLI_ABSOLUTE_GAME}" 2>/dev/null && pwd -P || printf '')"

# Both sides are resolved the same way, so a symlinked temporary directory
# (which resolves to a different absolute path) cannot make the comparison
# fail for a reason that has nothing to do with the property under test.
assert_eq "$CLI_ABSOLUTE_GAME_RESOLVED" "$CLI_RELATIVE_GAME_RESOLVED" \
    "T152: the relative --game-path resolves to the sandbox's own fixture"

assert_ne "$PWD" "$CLI_RELATIVE_GAME_RESOLVED" \
    "T152: the relative --game-path does not name the working directory"

for cli_row in "${CLI_MODS_PATH_TABLE[@]}"; do
    IFS='|' read -r cli_case cli_game cli_mods cli_expected <<< "$cli_row"

    if [[ "$cli_mods" == "(default)" ]]; then
        run_cli_env "PATH=${CLI_NO_DISPLAY_PATH}" -- --game-path "$cli_game"
    else
        run_cli_env "PATH=${CLI_NO_DISPLAY_PATH}" -- \
            --game-path "$cli_game" --mods-path "$cli_mods"
    fi

    cli_actual="$(resolved_mods_path "$RUN_STDERR" || printf '')"

    assert_eq "$cli_expected" "$cli_actual" \
        "T068 [${cli_case}]: the resolved mods path is '${cli_expected}'"

    # The doubling itself: the game directory must not appear twice in the
    # resolved mods path. This invariant holds even if the implementation
    # canonicalises the game path first.
    assert_not_contains "$cli_actual" "${cli_game}/${cli_game}" \
        "T068 [${cli_case}]: the resolved mods path does not repeat the game directory"
done

# ---------------------------------------------------------------------------
# T128 — the suite's HARD DEADLINE bounds a hung script under test
# ---------------------------------------------------------------------------
#
# Every invocation in this suite goes through the harness's
# `run_script_deadline` helper (see run_cli above), so a script under test
# that HANGS can neither outlive the suite nor leak the child it started.
#
# The two cases below prove it.
#
#   * Case 1 hangs in the PRE-FLIGHT: a `date` that never answers, so the
#     run blocks on its first timestamp and never starts a child. The
#     deadline's group signal ends it.
#
#   * Case 2 hangs in the DISPLAY phase: an X server that never reports a
#     display number, so the run DID start the X server and then waited for
#     the script's own 15-second display bound. The suite's deadline is
#     shorter than that bound, so the deadline — not the script — is what
#     ends the run.
#
# `timeout` reports 124 when the command answered the signal and 137 when it
# had to be killed with SIGKILL (`--kill-after`); both are the DEADLINE's own
# codes, and neither is in the FR-007 taxonomy — the script translates every
# child status, so a run that ends with 124 or 137 was ended by the
# deadline and not by a verdict of the script's own.

# assert_le <actual> <limit> <label> — the actual number must not exceed the
# limit. Both arguments must be plain non-negative integers; anything else
# fails the assertion rather than passing it vacuously.
assert_le() {
    local actual="${1-}"
    local limit="${2-}"
    local label="${3-}"

    if [[ ! "$actual" =~ ^[0-9]+$ ]] || [[ ! "$limit" =~ ^[0-9]+$ ]]; then
        fail "$label" "expected a number <= ${limit}, got '${actual}'"
        return 0
    fi

    if (( actual <= limit )); then
        pass "$label"
    else
        fail "$label" "expected a number <= ${limit}, got '${actual}'"
    fi
    return 0
}

# assert_exit_one_of <csv> <actual> <label> — the exit code must be one of
# the listed values.
assert_exit_one_of() {
    local allowed="${1-}"
    local actual="${2-}"
    local label="${3-}"
    local candidate

    for candidate in ${allowed//,/ }; do
        if [[ "$actual" == "$candidate" ]]; then
            pass "$label"
            return 0
        fi
    done
    fail "$label" "expected one of [${allowed}], got '${actual}'"
    return 0
}

# make_stub_hanging_date <path>
#
# A stub `date` that never answers: the script under test blocks on its
# first timestamp, so the run HANGS before it has started any child.
make_stub_hanging_date() {
    local path="${1-}"

    if [[ -z "$path" ]]; then
        printf 'make_stub_hanging_date: a path is required\n' >&2
        return 1
    fi

    mkdir -p -- "$(dirname -- "$path")" || return 1
    rm -f -- "$path" || return 1
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stub date: never answers. The script under test blocks on its first
# timestamp, so the run hangs before it has started any child.
while true; do
    sleep 1
done
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# make_stub_xvfb_hanging <path>
#
# A stub Xvfb that answers the runnability probe and then HANGS: it never
# reports a display number and never exits, so the script under test polls
# for the number until the suite's deadline ends the run.
make_stub_xvfb_hanging() {
    local path="${1-}"

    if [[ -z "$path" ]]; then
        printf 'make_stub_xvfb_hanging: a path is required\n' >&2
        return 1
    fi

    mkdir -p -- "$(dirname -- "$path")" || return 1
    rm -f -- "$path" || return 1
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stub Xvfb: answers the runnability probe and then HANGS — it never reports
# a display number and never exits, so the script under test waits in its
# display phase until the suite's deadline ends the run.
set -u

case "${1-}" in
    -version|--version)
        printf 'Xvfb version 21.1.24\n'
        exit 0
        ;;
esac

while true; do
    sleep 1
done
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

CLI_HANG_BIN_DIR="${FIXTURE_ROOT}/cli-hang-bin"
mkdir -p -- "${CLI_HANG_BIN_DIR}"
make_stub_xvfb_hanging "${CLI_HANG_BIN_DIR}/Xvfb"
readonly CLI_HANGING_PATH="${CLI_HANG_BIN_DIR}:${PATH}"

CLI_DATE_HANG_BIN_DIR="${FIXTURE_ROOT}/cli-hang-date-bin"
mkdir -p -- "${CLI_DATE_HANG_BIN_DIR}"
make_stub_hanging_date "${CLI_DATE_HANG_BIN_DIR}/date"
readonly CLI_HANGING_DATE_PATH="${CLI_DATE_HANG_BIN_DIR}:${PATH}"

# The deadline is deliberately short: the run is SUPPOSED to hang, so the
# deadline is the only thing that can end it. The script's own display bound
# is 15 seconds, so a run that ended later than that would have been ended
# by the SCRIPT and not by the deadline.
CLI_SAVED_DEADLINE="${CLI_DEADLINE_SECS}"
CLI_DEADLINE_SECS=5

# --- Case 1: the run hangs before it starts any child -----------------------
CLI_HANG_START="$(date +%s)"
run_cli_env "PATH=${CLI_HANGING_DATE_PATH}" -- --game-path "${CLI_ABSOLUTE_GAME}"
CLI_HANG_ELAPSED=$(( $(date +%s) - CLI_HANG_START ))

assert_exit_one_of "124,137" "$RUN_CODE" \
    "T128: the hung run was ended by the suite's deadline, not by the script's own verdict"

assert_le "$CLI_HANG_ELAPSED" 14 \
    "T128: the hung run was ended by the deadline, not by the script's own 15s display bound"

# The child the run started is gone: the deadline signals the whole process
# group, so the hanging `date` died with the run.
assert_eq "" "$(pgrep -f "${CLI_DATE_HANG_BIN_DIR}/date" 2>/dev/null || printf '')" \
    "T128: the child the hung run started did not outlive the suite"

# --- Case 2: the run hangs in the display phase, with an X server -----------
CLI_HANG_START="$(date +%s)"
run_cli_env "PATH=${CLI_HANGING_PATH}" -- --game-path "${CLI_ABSOLUTE_GAME}"
CLI_HANG_ELAPSED=$(( $(date +%s) - CLI_HANG_START ))

CLI_DEADLINE_SECS="${CLI_SAVED_DEADLINE}"

# The run REACHED the display phase: a run that had failed earlier would
# have returned the script's own verdict and the deadline would never have
# fired.
assert_contains "$RUN_STDERR" "phase display starting" \
    "T128: the hung run reached the display phase"

# The deadline ended the run, not the script: the script's own verdict for
# this path is 10 (the virtual display could not be allocated), and it would
# have come after its 15-second display bound. `timeout` reports 124 when
# the command answered the signal and 137 when it had to be killed, so both
# are the deadline's own codes and neither is in the FR-007 taxonomy.
assert_exit_one_of "124,137" "$RUN_CODE" \
    "T128: the hung display run was ended by the deadline, not by the script's own verdict"

assert_le "$CLI_HANG_ELAPSED" 14 \
    "T128: the hung display run was ended by the deadline, not by the script's own 15s display bound"

# The X server the run started is a child of its OWN process group, and the
# script under test publishes that group only once the display number
# arrives — so the script cannot signal it on this path. The SUITE reaps it
# instead, scoped to its own fixture directory, so nothing outside this run
# is ever touched.
CLI_HANG_STUB_PIDS="$(pgrep -f "${CLI_HANG_BIN_DIR}/Xvfb" 2>/dev/null || printf '')"
for cli_hang_pid in $CLI_HANG_STUB_PIDS; do
    kill -TERM -- "$cli_hang_pid" 2>/dev/null || true
done
sleep 1

assert_eq "" "$(pgrep -f "${CLI_HANG_BIN_DIR}/Xvfb" 2>/dev/null || printf '')" \
    "T128: the X server the hung run started did not outlive the suite"

# Nothing of the hung run may outlive the suite at all: every process it
# started — the script under test and the stubs it launched — names THIS
# run's sandbox, so the reaper is scoped to the sandbox and touches nothing
# outside it.
CLI_HANG_SURVIVORS="$(pgrep -f "${HARNESS_SANDBOX}" 2>/dev/null || printf '')"
for cli_hang_pid in $CLI_HANG_SURVIVORS; do
    kill -TERM -- "$cli_hang_pid" 2>/dev/null || true
done
sleep 1
for cli_hang_pid in $CLI_HANG_SURVIVORS; do
    kill -KILL -- "$cli_hang_pid" 2>/dev/null || true
done
sleep 1

assert_eq "" "$(pgrep -f "${HARNESS_SANDBOX}" 2>/dev/null || printf '')" \
    "T128: no process of the hung run outlived the suite"

# ---------------------------------------------------------------------------
# T138 — a failing command substitution yields EXACTLY ONE failure report
# ---------------------------------------------------------------------------
#
# FR-007's unexpected-error edge case: the ERR trap reports an unhandled
# error and the script exits 13.
#
# The trap is inherited by command substitutions (`set -E`), so a command
# substitution whose inner command FAILS runs the handler INSIDE the
# substitution's subshell: the handler prints a failure report and exits, the
# subshell dies with that status, and the enclosing command sees the
# substitution's status and fails too — which runs the handler a SECOND time,
# in the outer shell. The two reports CONTRADICT each other (the first names
# the inner command's status, the second the substitution's), so a single
# failure is reported twice.
#
# The sentinel's timestamp extraction is the shipped instance of this:
#
#     sentinel_ts="$(grep -o 'Log started at [^ ]* UTC' -- "$smapi_log" \
#                    | head -1 | sed 's/Log started at //;s/ UTC//')"
#
# A log whose sentinel line carries no parseable timestamp makes the grep
# match NOTHING, and under `set -o pipefail` the pipeline then exits 1 — so
# the handler fires from inside the command substitution, and the failure is
# reported twice.
#
# Reaching the readiness wait that evaluates that extraction needs a launch
# (the log-gate suite's job: it rebuilds the mod and starts the game), which
# the CLI suite must not do. The case therefore drives the SAME mechanism
# through the earliest command substitution the script evaluates — the run's
# start timestamp — with a `date` that always fails, exactly like a grep
# that matched nothing. The case at the END of this file drives the
# sentinel's own extraction, with the launch simulated.

# count_failure_reports <text> — the number of failure reports in the text.
#
# A failure report opens with the line
#
#   <stamp> ERROR: smoke validation failed (exit <code>).
#
# so the marker is the phrase "ERROR: smoke validation failed". The run log
# holds a copy of the report too, but the log is a FILE: only the text on
# stderr is counted here.
count_failure_reports() {
    local text="${1-}"
    local count=0
    local line

    while IFS= read -r line || [[ -n "$line" ]]; do
        case "$line" in
            *"ERROR: smoke validation failed"*)
                count=$((count + 1))
                ;;
        esac
    done <<< "$text"

    printf '%s' "$count"
}

# reported_exit_code <text> — the exit code the failure report announces.
#
# The report carries the code in its first line ("… failed (exit 13)."), so
# the code the run PROMISED is read back from the report itself rather than
# assumed. Returns 1 when the text holds no report.
reported_exit_code() {
    local text="${1-}"
    local line

    while IFS= read -r line || [[ -n "$line" ]]; do
        case "$line" in
            *"ERROR: smoke validation failed (exit "*)
                line="${line#*\(exit }"
                printf '%s' "${line%%\)*}"
                return 0
                ;;
        esac
    done <<< "$text"

    return 1
}

CLI_STUB_FAIL_BIN_DIR="${FIXTURE_ROOT}/cli-fail-bin"
mkdir -p -- "${CLI_STUB_FAIL_BIN_DIR}"
cat > "${CLI_STUB_FAIL_BIN_DIR}/date" <<'STUB'
#!/usr/bin/env bash
# Stub date: always fails, like a grep that matched nothing. The script
# under test's first command substitution therefore fails, and the ERR trap
# fires from inside it.
printf 'date stub: failing on purpose\n' >&2
exit 1
STUB
chmod 0755 -- "${CLI_STUB_FAIL_BIN_DIR}/date" 2>/dev/null || true

run_cli_env "PATH=${CLI_STUB_FAIL_BIN_DIR}:${PATH}" -- --game-path "${CLI_ABSOLUTE_GAME}"

CLI_T138_REPORTS="$(count_failure_reports "$RUN_STDERR")"
CLI_T138_CODE="$(reported_exit_code "$RUN_STDERR" || printf '')"

# Exactly ONE report: the handler must not fire a second time from inside the
# command substitution.
assert_eq "1" "$CLI_T138_REPORTS" \
    "T138: exactly ONE failure report reaches stderr"

# The report announces the internal-error code, and the run exits with THAT
# code — the two can never contradict each other.
assert_eq "13" "$CLI_T138_CODE" \
    "T138: the failure report announces the internal-error exit code 13"

assert_eq "$CLI_T138_CODE" "$RUN_CODE" \
    "T138: the run's exit code is the failure report's own exit code"

# ---------------------------------------------------------------------------
# T149 — the ERR-trap path ALWAYS exits 13
# ---------------------------------------------------------------------------
#
# FR-007's unexpected-error edge case: the ERR trap reports the failure and
# the script exits 13 — NEVER with the raw status of the failing command,
# which under `set -e` would otherwise masquerade as a domain failure
# (`dotnet build` exits 1, an unset variable exits 1, a syntax error exits 2).
#
# The handler is a CHAIN: `on_error` calls `die`, and `die` runs the cleanup,
# builds the report, writes the summary and finally exits 13. Every command
# in that chain can fail on its own — the cleanup's own log line needs
# `date`, the report pipeline needs `sed`, the summary needs `date` again.
# A failure inside the handler MUST NOT replace the mandated exit code with
# that command's raw status: the run still exits 13, and the report it
# printed still matches the code it announced.
#
# The case makes the handler's own `date` fail — a stub that refuses the ISO
# stamp format the log lines use — so the cleanup inside `die` fails while
# the handler is already running.

CLI_REAL_DATE="$(command -v date || printf '/usr/bin/date')"

CLI_STUB_STAMP_BIN_DIR="${FIXTURE_ROOT}/cli-stamp-bin"
mkdir -p -- "${CLI_STUB_STAMP_BIN_DIR}"
cat > "${CLI_STUB_STAMP_BIN_DIR}/date" <<STUB
#!/usr/bin/env bash
# Stub date: refuses the ISO-8601 stamp format the log lines use, so the
# handler's OWN log line fails while the handler is running. Every other
# format is answered by the real date.
for cli_stub_arg in "\$@"; do
    case "\${cli_stub_arg}" in
        +%Y-%m-%dT%H:%M:%SZ)
            printf 'date stub: refusing the ISO stamp format\n' >&2
            exit 1
            ;;
    esac
done
exec "${CLI_REAL_DATE}" "\$@"
STUB
chmod 0755 -- "${CLI_STUB_STAMP_BIN_DIR}/date" 2>/dev/null || true

run_cli_env "PATH=${CLI_STUB_STAMP_BIN_DIR}:${PATH}" -- --game-path "${CLI_ABSOLUTE_GAME}"

CLI_T149_REPORTS="$(count_failure_reports "$RUN_STDERR")"
CLI_T149_CODE="$(reported_exit_code "$RUN_STDERR" || printf '')"

assert_exit 13 \
    "T149: the ERR-trap path exits 13 even when a command inside the handler fails"

assert_eq "1" "$CLI_T149_REPORTS" \
    "T149: exactly ONE failure report reaches stderr"

assert_eq "13" "$CLI_T149_CODE" \
    "T149: the failure report announces the internal-error exit code 13"

assert_eq "$CLI_T149_CODE" "$RUN_CODE" \
    "T149: the run's exit code is the failure report's own exit code"

# ---------------------------------------------------------------------------
# T138 — the sentinel's UNPARSEABLE timestamp yields EXACTLY ONE report
# ---------------------------------------------------------------------------
#
# The case above drives the double-report mechanism through the earliest
# command substitution the script evaluates. This one drives the case the
# task names: the SENTINEL's timestamp extraction, in the readiness wait.
#
#     sentinel_ts="$(grep -o 'Log started at [^ ]* UTC' -- "$smapi_log" \
#                    | head -1 | sed 's/Log started at //;s/ UTC//')"
#
# A log whose sentinel line carries NO parseable timestamp makes the grep
# match NOTHING, and under `set -o pipefail` the pipeline then exits 1 — so
# the handler can fire from inside the command substitution, and a single
# failure would be reported twice.
#
# Reaching the readiness wait needs a LAUNCH, so the launch is SIMULATED:
# the X server, the graphics probe and the rebuild are stubbed, and the
# "game" is a stub that produces the log. The mod project lives in a
# sandbox "repository" and the script under test is invoked through a
# symlink inside it, so the repository root the script derives from its own
# location is the sandbox — and NOTHING is written into the real repository
# tree.
#
# The stub X server, the graphics stubs and the build stub are the same
# shape as the ones the pre-flight suite uses; they are written here so
# this suite stays self-contained.

# The sandbox repository: the script under test is reached through a symlink
# inside it, so the repository root it derives from its OWN location is the
# sandbox and the fixture project stays inside the harness sandbox.
CLI_T138_REPO="${FIXTURE_ROOT}/fakerepo"
CLI_T138_REPO_SCRIPT="${CLI_T138_REPO}/scripts/smoke-validation.sh"
mkdir -p -- "${CLI_T138_REPO}/scripts" "${CLI_T138_REPO}/LivingRoots"
ln -sf -- "$HARNESS_SCRIPT" "$CLI_T138_REPO_SCRIPT"
printf '<Project Sdk="Microsoft.NET.Sdk" />\n' > "${CLI_T138_REPO}/LivingRoots/LivingRoots.csproj"
cat > "${CLI_T138_REPO}/LivingRoots/manifest.json" <<'MANIFEST'
{
  "Name": "Stardew Valley - Living Roots",
  "Author": "lfgranja",
  "Version": "0.0.1",
  "UniqueID": "lfgranja.LivingRoots",
  "EntryDll": "LivingRoots.dll",
  "MinimumApiVersion": "4.0.0"
}
MANIFEST

# The game fixture. The harness's fake apphost exits as soon as it has
# written the log, and the script under test would then see the game gone
# before it read the log — so the apphost is replaced with one that stays
# alive until the script's cleanup signals its process group.
CLI_T138_GAME="${FIXTURE_ROOT}/t138/game"
make_fake_game_dir "$CLI_T138_GAME" || return 1
cat > "${CLI_T138_GAME}/StardewModdingAPI" <<'STUB'
#!/usr/bin/env bash
# Stub apphost: produces the SMAPI log the script under test polls for and
# then stays alive until the script's cleanup signals its process group.
set -u

if [[ -n "${FAKE_SMAPI_LOG_TEMPLATE:-}" && -n "${FAKE_SMAPI_LOG_TARGET:-}" \
      && -f "${FAKE_SMAPI_LOG_TEMPLATE}" ]]; then
    mkdir -p -- "${FAKE_SMAPI_LOG_TARGET%/*}" 2>/dev/null || true
    cp -- "${FAKE_SMAPI_LOG_TEMPLATE}" "${FAKE_SMAPI_LOG_TARGET}" 2>/dev/null || true
fi

while true; do
    sleep 1
done
STUB
chmod 0755 -- "${CLI_T138_GAME}/StardewModdingAPI" 2>/dev/null || true

# The log the launch produces. Its sentinel line carries NO parseable
# timestamp — the " UTC" suffix the extraction's pattern requires is
# missing — so the extraction's grep matches nothing.
CLI_T138_LOG_DIR="${FIXTURE_ROOT}/t138"
CLI_T138_LOG_TEMPLATE="${CLI_T138_LOG_DIR}/log-template.txt"
CLI_T138_LOG_TARGET="${CLI_T138_LOG_DIR}/smapi-log.txt"
mkdir -p -- "$CLI_T138_LOG_DIR"
{
    _log_line "12:00:00" "TRACE" "SMAPI" "Log started at 2026-10-20T00:00:00"
    _log_line "12:00:00" "INFO" "SMAPI" "Loaded 0 mods."
} > "$CLI_T138_LOG_TEMPLATE"

stage_log_for_launch "$CLI_T138_LOG_TEMPLATE" "$CLI_T138_LOG_TARGET" || return 1

# The PATH profile: every host tool, plus the stubs for the X server, the
# graphics probe and the rebuild.
CLI_T138_BIN_DIR="${CLI_T138_LOG_DIR}/bin"
mkdir -p -- "$CLI_T138_BIN_DIR"
for cli_dir in /usr/local/bin /usr/bin /bin /usr/sbin /sbin; do
    [[ -d "$cli_dir" ]] || continue
    ln -sf -- "$cli_dir"/* "$CLI_T138_BIN_DIR/" 2>/dev/null || true
done
for cli_tool in bash dotnet; do
    cli_found="$(command -v "$cli_tool" 2>/dev/null || true)"
    [[ -n "$cli_found" ]] || continue
    ln -sf -- "$cli_found" "${CLI_T138_BIN_DIR}/${cli_tool}" 2>/dev/null || true
done

# The X server: it reports a display number on the descriptor the script
# passes it and stays alive until the cleanup signals it.
rm -f -- "${CLI_T138_BIN_DIR}/Xvfb"
cat > "${CLI_T138_BIN_DIR}/Xvfb" <<'STUB'
#!/usr/bin/env bash
# Stub Xvfb: answers the runnability probe, reports a display number on the
# descriptor the script passes (-displayfd 3) and then stays alive.
set -u

case "${1-}" in
    -version|--version)
        printf 'Xvfb version 21.1.24\n'
        exit 0
        ;;
esac

printf '99\n' >&3 2>/dev/null || true
while true; do
    sleep 1
done
STUB
chmod 0755 -- "${CLI_T138_BIN_DIR}/Xvfb" 2>/dev/null || true

# The graphics probe: the GLX and RANDR extensions and a software
# rasteriser with a framebuffer-object extension.
rm -f -- "${CLI_T138_BIN_DIR}/xdpyinfo" "${CLI_T138_BIN_DIR}/glxinfo"
cat > "${CLI_T138_BIN_DIR}/xdpyinfo" <<'STUB'
#!/usr/bin/env bash
printf 'name of display: :99\n'
printf 'number of extensions: 2\n'
printf 'GLX\n'
printf 'RANDR\n'
exit 0
STUB
cat > "${CLI_T138_BIN_DIR}/glxinfo" <<'STUB'
#!/usr/bin/env bash
printf 'OpenGL vendor string: Mesa\n'
printf 'OpenGL renderer string: llvmpipe (LLVM 15.0, 256 bits)\n'
printf 'OpenGL version string: 3.3 (Core Profile) Mesa 23.0.0\n'
printf ' GL_ARB_framebuffer_object\n'
exit 0
STUB
chmod 0755 -- "${CLI_T138_BIN_DIR}/xdpyinfo" "${CLI_T138_BIN_DIR}/glxinfo" 2>/dev/null || true

# The rebuild: it simulates the deploy ModBuildConfig performs, so the
# rebuild-and-verify phase succeeds without a real build.
rm -f -- "${CLI_T138_BIN_DIR}/dotnet"
cat > "${CLI_T138_BIN_DIR}/dotnet" <<'STUB'
#!/usr/bin/env bash
# Stub dotnet: simulates `dotnet build` and the deploy ModBuildConfig
# performs, so the rebuild-and-verify phase succeeds without a real build
# and without writing into the repository.
set -u

mods_path=""
project=""
for arg in "$@"; do
    case "$arg" in
        -p:GameModsPath=*) mods_path="${arg#-p:GameModsPath=}" ;;
        *.csproj) project="$arg" ;;
    esac
done

if [[ -z "$project" || -z "$mods_path" ]]; then
    printf 'dotnet stub: no project or no mods path\n' >&2
    exit 1
fi

project_dir="$(dirname -- "$project")"
folder="$(basename -- "$project")"
folder="${folder%.csproj}"
manifest="${project_dir}/manifest.json"

field() {
    sed -n "s/.*\"$1\"[[:space:]]*:[[:space:]]*\"\([^\"]*\)\".*/\1/p" -- "$2" 2>/dev/null | head -1
}

entry_dll="$(field EntryDll "$manifest")"
if [[ -z "$entry_dll" ]]; then
    printf 'dotnet stub: the project manifest has no EntryDll\n' >&2
    exit 1
fi

mkdir -p -- "${project_dir}/bin" || exit 1
printf 'fake built assembly\n' > "${project_dir}/bin/${entry_dll}" || exit 1

mkdir -p -- "${mods_path}/${folder}" || exit 1
printf 'fake built assembly\n' > "${mods_path}/${folder}/${entry_dll}" || exit 1
cp -- "$manifest" "${mods_path}/${folder}/manifest.json" || exit 1

exit 0
STUB
chmod 0755 -- "${CLI_T138_BIN_DIR}/dotnet" 2>/dev/null || true

# The fixture's own precondition: the extraction's pattern matches NOTHING
# in the log the launch produces.
assert_eq "" "$(grep -o 'Log started at [^ ]* UTC' -- "$CLI_T138_LOG_TEMPLATE" 2>/dev/null | head -1 || printf '')" \
    "T138: the fixture's sentinel timestamp cannot be parsed (the grep matches nothing)"

# The run. The script under test is invoked through the sandbox
# repository's symlink, so the repository root it derives from its own
# location is the sandbox.
CLI_T138_SAVED_SCRIPT="${HARNESS_SCRIPT}"
HARNESS_SCRIPT="$CLI_T138_REPO_SCRIPT"
run_cli_env "PATH=${CLI_T138_BIN_DIR}:${PATH}" -- \
    --game-path "$CLI_T138_GAME" \
    --smapi-log "$CLI_T138_LOG_TARGET" \
    --timeout 20 \
    --build-timeout 30 \
    --grace-period 2
HARNESS_SCRIPT="$CLI_T138_SAVED_SCRIPT"

# The run reached the sentinel: without this the case would be vacuous.
assert_contains "$RUN_STDERR" "run sentinel observed in the SMAPI log" \
    "T138: the run reached the sentinel's timestamp extraction"

CLI_T138_SENTINEL_REPORTS="$(count_failure_reports "$RUN_STDERR")"
CLI_T138_SENTINEL_CODE="$(reported_exit_code "$RUN_STDERR" || printf '')"

assert_eq "1" "$CLI_T138_SENTINEL_REPORTS" \
    "T138: the unparseable sentinel yields EXACTLY ONE failure report"

assert_eq "$CLI_T138_SENTINEL_CODE" "$RUN_CODE" \
    "T138: the unparseable sentinel's run exits with that report's own exit code"

finish_tests
