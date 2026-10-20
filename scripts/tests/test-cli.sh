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
# Plus two properties:
#
#   * the option-surface property — every row of the spec's normative
#     "Command-Line Options" table is rendered by `--help` with the same option
#     spelling, the same argument placeholder and the same default, and the
#     FR-015 sections (usage line, environment, run log, run id, version and
#     the whole exit-code table) are present;
#   * the no-abbreviation property — only exact long-option names are accepted:
#     `--mod` and `--mods` are rejected with 14, while the exact name
#     `--mod-project` is not rejected as unknown.
#
# The `--opt=VALUE` form is checked for equivalence with `--opt VALUE`.
#
# Style: the shared harness is sourced (its assertions are used, never
# re-implemented) and every invocation of the script under test goes through
# its `run_script` helper, so the script always runs inside the per-test
# sandbox and never touches the real game installation or the real state roots.

set -Eeuo pipefail

# The harness lives next to this file.
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/harness.sh"

# ---------------------------------------------------------------------------
# Expectations
# ---------------------------------------------------------------------------

# The script's own name, as the caller sees it on the command line.
readonly CLI_SCRIPT_NAME="$(basename -- "$HARNESS_SCRIPT")"

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

run_script --version
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

run_script --help
assert_exit 0 "--help exits 0"
assert_eq "" "$RUN_STDERR" "--help writes nothing to stderr"
CLI_HELP="$RUN_STDOUT"

run_script -h
assert_exit 0 "-h exits 0"
assert_eq "$RUN_STDOUT" "$CLI_HELP" "-h stdout is byte-identical to --help stdout"
assert_eq "" "$RUN_STDERR" "-h writes nothing to stderr"

# ---------------------------------------------------------------------------
# TC-19 — usage errors exit 14, name the option and launch nothing
# ---------------------------------------------------------------------------

run_script --no-such-option
assert_exit 14 "an unknown option exits 14"
assert_contains "$RUN_STDERR" "--no-such-option" \
    "the report names the unknown option"
assert_eq "" "$RUN_STDOUT" "a usage error writes nothing to stdout"
assert_nothing_launched "an unknown option"

run_script --timeout abc
assert_exit 14 "a malformed --timeout value exits 14"
assert_contains "$RUN_STDERR" "--timeout" \
    "the report names the option with the malformed value"
assert_eq "" "$RUN_STDOUT" "a usage error writes nothing to stdout"
assert_nothing_launched "a malformed --timeout value"

run_script --timeout 0
assert_exit 14 "a non-positive --timeout value exits 14"
assert_contains "$RUN_STDERR" "--timeout" \
    "the report names the option with the non-positive value"
assert_eq "" "$RUN_STDOUT" "a usage error writes nothing to stdout"
assert_nothing_launched "a non-positive --timeout value"

run_script --game-path
assert_exit 14 "a missing required argument exits 14"
assert_contains "$RUN_STDERR" "--game-path" \
    "the report names the option whose argument is missing"
assert_eq "" "$RUN_STDOUT" "a usage error writes nothing to stdout"
assert_nothing_launched "a missing required argument"

run_script somearg
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

run_script --mod
assert_exit 14 "the ambiguous abbreviation --mod is rejected with 14"
assert_contains "$RUN_STDERR" "--mod" \
    "the report names the ambiguous abbreviation"
assert_not_contains "$RUN_STDERR" "missing required argument" \
    "--mod is rejected because of the option NAME, not because of a missing value"
assert_nothing_launched "the ambiguous abbreviation --mod"

run_script --mods
assert_exit 14 "the prefix --mods is rejected with 14"
assert_contains "$RUN_STDERR" "--mods" \
    "the report names the prefix"
assert_not_contains "$RUN_STDERR" "missing required argument" \
    "--mods is rejected because of the option NAME, not because of a missing value"
assert_nothing_launched "the prefix --mods"

# The abbreviations must also be rejected when a value follows them: an
# implementation that silently expanded --mod to --mods-path would accept
# these two invocations and only fail later, for a different reason.
run_script --mod "$HARNESS_SANDBOX"
assert_exit 14 "the ambiguous abbreviation --mod with a value is rejected with 14"
assert_nothing_launched "the ambiguous abbreviation --mod with a value"

run_script --mods "$HARNESS_SANDBOX"
assert_exit 14 "the prefix --mods with a value is rejected with 14"
assert_nothing_launched "the prefix --mods with a value"

# The exact name is accepted by the parser, so the run must not be a usage
# error. It fails later (the pipeline is not implemented yet), which is
# exactly the point: it fails for another reason than the name.
run_script --mod-project "${HARNESS_SANDBOX}/no-such-project/LivingRoots.csproj"
assert_ne 14 "$RUN_CODE" \
    "the exact option name --mod-project is not a usage error"
assert_not_contains "$RUN_STDERR" "unknown option" \
    "the report does not call --mod-project an unknown option"
assert_not_contains "$RUN_STDERR" "abbreviat" \
    "the report does not call --mod-project an abbreviation"

# ---------------------------------------------------------------------------
# The --opt=VALUE form is equivalent to --opt VALUE
# ---------------------------------------------------------------------------

run_script --timeout=90 --help
assert_exit 0 "--timeout=90 --help exits 0"
assert_eq "$CLI_HELP" "$RUN_STDOUT" \
    "--timeout=90 --help prints the same help as --help"
assert_contains "$RUN_STDOUT" "(default: 120)" \
    "--timeout=90 --help still shows the default timeout 120"

run_script --timeout=abc
assert_exit 14 "--timeout=abc is rejected with 14"
assert_contains "$RUN_STDERR" "--timeout" \
    "the = form reaches the validator, which names the option"

finish_tests
