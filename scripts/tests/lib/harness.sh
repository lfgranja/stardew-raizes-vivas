#!/usr/bin/env bash
#
# scripts/tests/lib/harness.sh — shared test library for feature 008 (smoke validation).
#
# This file is SOURCED by every `scripts/tests/test-*.sh` file; it is never executed
# on its own. It provides:
#
#   * TAP-ish assertions — assert_eq, assert_ne, assert_contains, assert_not_contains,
#     assert_exit, assert_file_exists, assert_file_absent, assert_file_mode,
#     assert_dir_mode, assert_matches, assert_contains_line. Each one prints
#     `ok - <label>` or `not ok - <label>` and updates the TESTS_RUN / TESTS_PASSED /
#     TESTS_FAILED counters. An assertion NEVER aborts the calling test: a failed
#     assertion records a failure and the test continues, so one run reports every
#     broken expectation instead of only the first.
#
#   * run_script / run_script_env — invoke `scripts/smoke-validation.sh` with a
#     SANITISED environment and capture stdout → RUN_STDOUT, stderr → RUN_STDERR and
#     the exit status → RUN_CODE. The script's own output is captured, never leaked
#     into the test's stdout.
#
#   * Fixture builders — a synthetic game installation, a synthetic SMAPI log and
#     stub host tools, all created under a per-test temporary directory.
#
#   * finish_tests — prints the summary and exits non-zero when any assertion failed.
#     Every test file MUST end with it.
#
# EXIT TRAP
# ---------
# Sourcing this library installs an EXIT trap that removes the sandbox and turns
# a failed assertion into a non-zero exit even if the test file forgot to call
# finish_tests. A trap the caller had installed BEFORE sourcing is run at exit as
# well. A trap installed AFTER sourcing replaces the harness's own, and the
# harness's defensive exit-code guarantee is then lost — which is why
# finish_tests remains the contract.
#
# SAFETY
# ------
# Nothing in this library ever writes to the real game installation, the real
# ~/.config/StardewValley or the real mods directory. Every synthetic artefact lives
# under a per-test `mktemp -d` sandbox, and the script under test is always launched
# with HOME, XDG_STATE_HOME, XDG_RUNTIME_DIR and XDG_CONFIG_HOME pointed inside that
# sandbox, so even a buggy script cannot reach the user's real state. The only path
# the script under test may touch outside the sandbox is the one it is explicitly
# given on the command line (--game-path, --mods-path, --smapi-log, --mod-project).
#
# USAGE
# -----
#     #!/usr/bin/env bash
#     set -Eeuo pipefail
#     source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/harness.sh"
#
#     make_fake_game_dir "$FIXTURE_ROOT/game"
#     run_script --game-path "$FIXTURE_ROOT/game"
#     assert_exit 2 "missing SMAPI assembly is reported as 2"
#     finish_tests
#
# Requires bash 4.4+ (empty arrays are expanded with the ${arr[@]+"${arr[@]}"} idiom so
# that `set -u` stays usable).

set -Eeuo pipefail

# ---------------------------------------------------------------------------
# Constants shared with the tests
# ---------------------------------------------------------------------------

# The mod's manifest name, as SMAPI writes it in the log's source column.
HARNESS_MOD_NAME="Stardew Valley - Living Roots"
HARNESS_MOD_VERSION="0.0.1"
HARNESS_MOD_AUTHOR="lfgranja"

# The mod's own success marker — reported by the script, never gating.
HARNESS_MOD_SUCCESS_MARKER="The 'Stardew Valley - Living Roots' mod was loaded successfully!"

# The per-run sentinel SMAPI writes as a TRACE-level record.
HARNESS_RUN_SENTINEL="Log started at 2026-10-20T00:00:00 UTC"

# The Steam app id the game installation must declare.
HARNESS_STEAM_APP_ID="413150"

# The reason SMAPI states when it skips a mod for an unmet MinimumApiVersion.
HARNESS_SKIP_REASON="because it needs SMAPI 4.6.0 or later. Please update SMAPI to the latest version to use this mod."

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

# Directory of this library (…/scripts/tests/lib).
HARNESS_LIB_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"

# Repository root (…/scripts/tests/lib → …/) and the script under test.
HARNESS_REPO_ROOT="$(cd -- "${HARNESS_LIB_DIR}/../../.." && pwd -P)"
HARNESS_SCRIPT="${HARNESS_REPO_ROOT}/scripts/smoke-validation.sh"

# ---------------------------------------------------------------------------
# Per-test sandbox
#
# Everything synthetic lives here. The sandbox itself is a `mktemp -d` directory
# created once per test file; the fixture tree and the redirected HOME/XDG roots
# are created on demand inside it. $FIXTURE_ROOT is always a usable path (never
# the empty string) so a test can interpolate it before calling a builder.
# ---------------------------------------------------------------------------

HARNESS_SANDBOX="$(mktemp -d "${TMPDIR:-/tmp}/livingroots-smoke-harness.XXXXXXXXXX" 2>/dev/null || true)"
if [[ -z "$HARNESS_SANDBOX" || ! -d "$HARNESS_SANDBOX" ]]; then
    # Without a sandbox the state roots below would point at absolute paths such
    # as /state, so the library refuses to load rather than risk touching them.
    printf 'harness: could not create the per-test sandbox under %s\n' "${TMPDIR:-/tmp}" >&2
    return 1 2>/dev/null || exit 1
fi
chmod 0700 -- "$HARNESS_SANDBOX" 2>/dev/null || true

# The fixture tree (created lazily by the builders).
FIXTURE_ROOT="${HARNESS_SANDBOX}/fixtures"

# The redirected state roots handed to the script under test.
HARNESS_HOME="${HARNESS_SANDBOX}/home"
HARNESS_XDG_STATE_HOME="${HARNESS_SANDBOX}/state"
HARNESS_XDG_RUNTIME_DIR="${HARNESS_SANDBOX}/runtime"
HARNESS_XDG_CONFIG_HOME="${HARNESS_SANDBOX}/config"

# The values inherited from the real environment at load time. A caller that
# changes one of these variables before calling run_script has overridden it
# deliberately, and the override is honoured; otherwise the sandbox value wins.
HARNESS_ORIGINAL_HOME="${HOME-}"
HARNESS_ORIGINAL_XDG_STATE_HOME="${XDG_STATE_HOME-}"
HARNESS_ORIGINAL_XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR-}"
HARNESS_ORIGINAL_XDG_CONFIG_HOME="${XDG_CONFIG_HOME-}"

# ---------------------------------------------------------------------------
# Counters and captured results
# ---------------------------------------------------------------------------

TESTS_RUN=0
TESTS_PASSED=0
TESTS_FAILED=0

RUN_STDOUT=""
RUN_STDERR=""
RUN_CODE=0
RUN_PERFORMED=0

# The path most recently created by a fixture builder. It is part of the
# library's public surface (a test may read it), hence the SC2034 exemption.
# shellcheck disable=SC2034
LAST_FIXTURE=""

# Set to 1 by finish_tests so the EXIT trap does not report a second summary.
HARNESS_FINISHED=0

# The EXIT trap the caller had installed before sourcing this library.
#
# `$(trap -p EXIT)` cannot be used here: a command substitution runs in a
# subshell, and bash resets traps to their inherited values in a subshell, so the
# substitution would always yield the empty string. The definition is therefore
# redirected to a file in the current shell and read back from there.
HARNESS_PREVIOUS_EXIT_TRAP=""
_harness_trap_capture_file="$(mktemp "${TMPDIR:-/tmp}/livingroots-smoke-trap.XXXXXXXXXX" 2>/dev/null || true)"
if [[ -n "$_harness_trap_capture_file" ]]; then
    trap -p EXIT > "$_harness_trap_capture_file" 2>/dev/null || true
    HARNESS_PREVIOUS_EXIT_TRAP="$(cat -- "$_harness_trap_capture_file" 2>/dev/null || true)"
    rm -f -- "$_harness_trap_capture_file" 2>/dev/null || true
fi
unset _harness_trap_capture_file

# ---------------------------------------------------------------------------
# Reporting primitives
# ---------------------------------------------------------------------------

# pass <label> — record a successful expectation.
pass() {
    local label="${1-}"
    TESTS_RUN=$((TESTS_RUN + 1))
    TESTS_PASSED=$((TESTS_PASSED + 1))
    printf 'ok - %s\n' "$label"
}

# fail <label> <reason> — record a failed expectation.
fail() {
    local label="${1-}"
    local reason="${2-}"
    TESTS_RUN=$((TESTS_RUN + 1))
    TESTS_FAILED=$((TESTS_FAILED + 1))
    printf 'not ok - %s\n' "$label"
    printf '#   reason: %s\n' "$reason"
}

# skip <label> <tool> — record a skipped expectation (reported as an `ok`, because
# a skipped test is not a failure).
skip() {
    local label="${1-}"
    local tool="${2-}"
    TESTS_RUN=$((TESTS_RUN + 1))
    TESTS_PASSED=$((TESTS_PASSED + 1))
    printf 'ok - %s (skipped: %s missing)\n' "$label" "$tool"
}

# _truncate <text> [limit] — shorten a diagnostic so one failure cannot flood the
# report.
_truncate() {
    local text="${1-}"
    local limit="${2:-200}"
    if (( ${#text} > limit )); then
        printf '%s... [truncated]' "${text:0:limit}"
    else
        printf '%s' "$text"
    fi
}

# ---------------------------------------------------------------------------
# Assertions
#
# Every assertion returns 0 so that `set -e` never aborts a test file on the
# first failed expectation.
# ---------------------------------------------------------------------------

# assert_eq <expected> <actual> <label>
assert_eq() {
    local expected="${1-}"
    local actual="${2-}"
    local label="${3-}"
    if [[ "$expected" == "$actual" ]]; then
        pass "$label"
    else
        fail "$label" "expected '$(_truncate "$expected")', got '$(_truncate "$actual")'"
    fi
    return 0
}

# assert_ne <unexpected> <actual> <label>
assert_ne() {
    local unexpected="${1-}"
    local actual="${2-}"
    local label="${3-}"
    if [[ "$unexpected" != "$actual" ]]; then
        pass "$label"
    else
        fail "$label" "expected anything but '$(_truncate "$unexpected")'"
    fi
    return 0
}

# assert_contains <haystack> <needle> <label>
assert_contains() {
    local haystack="${1-}"
    local needle="${2-}"
    local label="${3-}"
    if [[ "$haystack" == *"$needle"* ]]; then
        pass "$label"
    else
        fail "$label" "expected to contain '$(_truncate "$needle")'"
    fi
    return 0
}

# assert_not_contains <haystack> <needle> <label>
assert_not_contains() {
    local haystack="${1-}"
    local needle="${2-}"
    local label="${3-}"
    if [[ "$haystack" != *"$needle"* ]]; then
        pass "$label"
    else
        fail "$label" "expected NOT to contain '$(_truncate "$needle")'"
    fi
    return 0
}

# assert_exit <expected_code> <label> — assert $RUN_CODE.
assert_exit() {
    local expected="${1-}"
    local label="${2-}"
    if (( RUN_PERFORMED == 0 )); then
        fail "$label" "no script has been run yet (call run_script first)"
        return 0
    fi
    if [[ "$RUN_CODE" == "$expected" ]]; then
        pass "$label"
    else
        fail "$label" "expected exit code $expected, got $RUN_CODE"
    fi
    return 0
}

# assert_file_exists <path> <label>
assert_file_exists() {
    local path="${1-}"
    local label="${2-}"
    if [[ -f "$path" ]]; then
        pass "$label"
    else
        fail "$label" "expected a regular file at: $path"
    fi
    return 0
}

# assert_file_absent <path> <label>
assert_file_absent() {
    local path="${1-}"
    local label="${2-}"
    if [[ ! -e "$path" && ! -L "$path" ]]; then
        pass "$label"
    else
        fail "$label" "expected the path to be absent: $path"
    fi
    return 0
}

# _normalize_octal <value> — drop the leading zeros so that `600`, `0600` and
# `000600` all compare equal.
_normalize_octal() {
    local value="${1-}"
    value="${value#0}"
    value="${value#0}"
    value="${value#0}"
    printf '%s' "${value:-0}"
}

# assert_file_mode <path> <expected_octal> <label> — the expected mode may be
# written with or without leading zeros (`600` or `0600`).
assert_file_mode() {
    local path="${1-}"
    local expected="${2-}"
    local label="${3-}"
    if [[ ! -e "$path" ]]; then
        fail "$label" "path does not exist: $path"
        return 0
    fi
    local actual
    actual="$(stat -c %a -- "$path" 2>/dev/null || true)"
    if [[ -z "$actual" ]]; then
        fail "$label" "could not read the mode of: $path"
        return 0
    fi
    if [[ "$(_normalize_octal "$actual")" == "$(_normalize_octal "$expected")" ]]; then
        pass "$label"
    else
        fail "$label" "expected mode $expected, got $actual for $path"
    fi
    return 0
}

# assert_dir_mode <path> <expected_octal> <label>
assert_dir_mode() {
    local path="${1-}"
    local expected="${2-}"
    local label="${3-}"
    if [[ ! -d "$path" ]]; then
        fail "$label" "not a directory: $path"
        return 0
    fi
    local actual
    actual="$(stat -c %a -- "$path" 2>/dev/null || true)"
    if [[ -z "$actual" ]]; then
        fail "$label" "could not read the mode of: $path"
        return 0
    fi
    if [[ "$(_normalize_octal "$actual")" == "$(_normalize_octal "$expected")" ]]; then
        pass "$label"
    else
        fail "$label" "expected mode $expected, got $actual for $path"
    fi
    return 0
}

# assert_matches <string> <extended_regex> <label>
assert_matches() {
    local string="${1-}"
    local regex="${2-}"
    local label="${3-}"
    if [[ "$string" =~ $regex ]]; then
        pass "$label"
    else
        fail "$label" "'$(_truncate "$string")' does not match /$(_truncate "$regex" 120)/"
    fi
    return 0
}

# assert_contains_line <text> <exact_line> <label> — a line-exact check: the text
# must contain a physical line identical to <exact_line>.
assert_contains_line() {
    local text="${1-}"
    local exact_line="${2-}"
    local label="${3-}"
    local line
    while IFS= read -r line || [[ -n "$line" ]]; do
        if [[ "$line" == "$exact_line" ]]; then
            pass "$label"
            return 0
        fi
    done <<< "$text"
    fail "$label" "no line exactly equal to '$(_truncate "$exact_line" 120)'"
    return 0
}

# ---------------------------------------------------------------------------
# Invoking the script under test
# ---------------------------------------------------------------------------

# The env overrides for the current invocation; populated by run_script_env.
RUN_ENV_ASSIGNMENTS=()

# _sandbox_home / _sandbox_state_home / _sandbox_runtime_dir / _sandbox_config_home —
# the sanitised value for each state-affecting variable. A caller that changed the
# variable since the harness was loaded has overridden it deliberately; otherwise
# the sandbox value is used, so a test can never touch the real user's state.
_sandbox_home() {
    local current="${HOME-}"
    if [[ -n "$current" && "$current" != "$HARNESS_ORIGINAL_HOME" ]]; then
        printf '%s' "$current"
        return 0
    fi
    mkdir -p -- "$HARNESS_HOME" 2>/dev/null || true
    chmod 0700 -- "$HARNESS_HOME" 2>/dev/null || true
    printf '%s' "$HARNESS_HOME"
}

_sandbox_state_home() {
    local current="${XDG_STATE_HOME-}"
    if [[ -n "$current" && "$current" != "$HARNESS_ORIGINAL_XDG_STATE_HOME" ]]; then
        printf '%s' "$current"
        return 0
    fi
    mkdir -p -- "$HARNESS_XDG_STATE_HOME" 2>/dev/null || true
    chmod 0700 -- "$HARNESS_XDG_STATE_HOME" 2>/dev/null || true
    printf '%s' "$HARNESS_XDG_STATE_HOME"
}

_sandbox_runtime_dir() {
    local current="${XDG_RUNTIME_DIR-}"
    if [[ -n "$current" && "$current" != "$HARNESS_ORIGINAL_XDG_RUNTIME_DIR" ]]; then
        printf '%s' "$current"
        return 0
    fi
    mkdir -p -- "$HARNESS_XDG_RUNTIME_DIR" 2>/dev/null || true
    chmod 0700 -- "$HARNESS_XDG_RUNTIME_DIR" 2>/dev/null || true
    printf '%s' "$HARNESS_XDG_RUNTIME_DIR"
}

_sandbox_config_home() {
    local current="${XDG_CONFIG_HOME-}"
    if [[ -n "$current" && "$current" != "$HARNESS_ORIGINAL_XDG_CONFIG_HOME" ]]; then
        printf '%s' "$current"
        return 0
    fi
    mkdir -p -- "$HARNESS_XDG_CONFIG_HOME" 2>/dev/null || true
    chmod 0700 -- "$HARNESS_XDG_CONFIG_HOME" 2>/dev/null || true
    printf '%s' "$HARNESS_XDG_CONFIG_HOME"
}

# _run_script_impl [args...] — run the script under test with the sanitised
# environment plus the assignments collected by run_script_env. Captures stdout,
# stderr and the exit status; never lets the script's output reach the test's
# own stdout.
_run_script_impl() {
    local out_file err_file
    out_file="$(mktemp "${TMPDIR:-/tmp}/livingroots-smoke-stdout.XXXXXXXXXX")" || out_file=""
    err_file="$(mktemp "${TMPDIR:-/tmp}/livingroots-smoke-stderr.XXXXXXXXXX")" || err_file=""

    if [[ -z "$out_file" || -z "$err_file" ]]; then
        RUN_CODE=1
        RUN_STDOUT=""
        RUN_STDERR="harness: could not create the capture files"
        RUN_PERFORMED=1
        return 0
    fi

    if [[ ! -f "$HARNESS_SCRIPT" ]]; then
        # The script does not exist yet: behave like the shell's own "command not
        # found" and say so on stderr.
        RUN_CODE=127
        RUN_STDOUT=""
        RUN_STDERR="harness: ${HARNESS_SCRIPT}: command not found (exit code 127)"
        rm -f -- "$out_file" "$err_file"
        RUN_PERFORMED=1
        return 0
    fi

    local -a env_args=()
    env_args+=("HOME=$(_sandbox_home)")
    env_args+=("XDG_STATE_HOME=$(_sandbox_state_home)")
    env_args+=("XDG_RUNTIME_DIR=$(_sandbox_runtime_dir)")
    env_args+=("XDG_CONFIG_HOME=$(_sandbox_config_home)")
    if (( ${#RUN_ENV_ASSIGNMENTS[@]} > 0 )); then
        env_args+=("${RUN_ENV_ASSIGNMENTS[@]}")
    fi

    if env "${env_args[@]}" "$HARNESS_SCRIPT" "$@" >"$out_file" 2>"$err_file"; then
        RUN_CODE=0
    else
        RUN_CODE=$?
    fi

    RUN_STDOUT="$(cat -- "$out_file")"
    RUN_STDERR="$(cat -- "$err_file")"
    rm -f -- "$out_file" "$err_file"
    RUN_PERFORMED=1
    return 0
}

# run_script [args...] — run `scripts/smoke-validation.sh` with the given arguments
# and capture its output and exit status.
run_script() {
    RUN_ENV_ASSIGNMENTS=()
    _run_script_impl "$@"
}

# run_script_env <env-assignments...> -- [args...] — like run_script, but with
# explicit environment assignments (e.g. `PATH=...`). The assignments replace the
# sanitised values for that invocation.
run_script_env() {
    RUN_ENV_ASSIGNMENTS=()
    local seen_separator=0
    local arg
    local -a script_args=()
    for arg in "$@"; do
        if (( seen_separator == 1 )); then
            script_args+=("$arg")
        elif [[ "$arg" == "--" ]]; then
            seen_separator=1
        else
            RUN_ENV_ASSIGNMENTS+=("$arg")
        fi
    done
    _run_script_impl "${script_args[@]+"${script_args[@]}"}"
}

# ---------------------------------------------------------------------------
# Fixture builders
# ---------------------------------------------------------------------------

# fixture_root — create the fixture tree if it does not exist yet and print its
# path. The tree is created lazily: a test that never builds a fixture leaves no
# directory behind.
fixture_root() {
    if [[ ! -d "$FIXTURE_ROOT" ]]; then
        mkdir -p -- "$FIXTURE_ROOT" 2>/dev/null || true
        chmod 0700 -- "$FIXTURE_ROOT" 2>/dev/null || true
    fi
    printf '%s' "$FIXTURE_ROOT"
}

# _write_apphost_stub <path> — a stand-in for SMAPI's native apphost. It never
# touches a game installation and never launches the game.
_write_apphost_stub() {
    local path="${1-}"
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Test stub standing in for SMAPI's native apphost (./StardewModdingAPI).
# It launches nothing and touches nothing outside the fixture tree.
printf 'StardewModdingAPI stub: no game launched\n'
exit 0
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
}

# make_fake_game_dir <dest> — a game installation containing the files the
# pre-flight looks for:
#
#   <dest>/Stardew Valley.dll
#   <dest>/StardewModdingAPI                              (executable stub)
#   <dest>/StardewModdingAPI.dll
#   <dest>/smapi-internal/SMAPI.Toolkit.dll
#   <dest>/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll
#   <dest>/steam_appid.txt                                (413150)
#   <dest>/Mods/
make_fake_game_dir() {
    local dest="${1-}"
    if [[ -z "$dest" ]]; then
        printf 'make_fake_game_dir: a destination is required\n' >&2
        return 1
    fi
    fixture_root >/dev/null
    mkdir -p -- "$dest/smapi-internal" "$dest/Mods" || return 1
    : > "$dest/Stardew Valley.dll" || return 1
    : > "$dest/StardewModdingAPI.dll" || return 1
    : > "$dest/smapi-internal/SMAPI.Toolkit.dll" || return 1
    : > "$dest/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll" || return 1
    printf '%s\n' "$HARNESS_STEAM_APP_ID" > "$dest/steam_appid.txt" || return 1
    _write_apphost_stub "$dest/StardewModdingAPI"
    LAST_FIXTURE="$dest"
    return 0
}

# make_broken_smapi_dir <dest> — the same tree minus
# smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll (a broken SMAPI installation).
make_broken_smapi_dir() {
    local dest="${1-}"
    if [[ -z "$dest" ]]; then
        printf 'make_broken_smapi_dir: a destination is required\n' >&2
        return 1
    fi
    make_fake_game_dir "$dest" || return 1
    rm -f -- "$dest/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll" || return 1
    LAST_FIXTURE="$dest"
    return 0
}

# make_bad_steam_dir <dest> — the same tree but with a wrong Steam app id.
make_bad_steam_dir() {
    local dest="${1-}"
    if [[ -z "$dest" ]]; then
        printf 'make_bad_steam_dir: a destination is required\n' >&2
        return 1
    fi
    make_fake_game_dir "$dest" || return 1
    printf '99999\n' > "$dest/steam_appid.txt" || return 1
    LAST_FIXTURE="$dest"
    return 0
}

# make_stub_xvfb <dest> — an executable stub that fails and prints NO display
# number, so a caller cannot read one from stdout. <dest> is the path of the stub
# itself; when <dest> is an existing directory the stub is created as
# <dest>/Xvfb instead.
make_stub_xvfb() {
    local dest="${1-}"
    if [[ -z "$dest" ]]; then
        printf 'make_stub_xvfb: a destination is required\n' >&2
        return 1
    fi
    local path="$dest"
    if [[ -d "$dest" || "$dest" == */ ]]; then
        path="${dest%/}/Xvfb"
    fi
    mkdir -p -- "$(dirname -- "$path")" || return 1
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stub Xvfb: fails to start and prints NO display number on stdout, so the
# caller cannot read one from there. The message goes to stderr on purpose.
printf 'Xvfb stub: failed to start the virtual display\n' >&2
exit 1
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    LAST_FIXTURE="$path"
    return 0
}

# ---------------------------------------------------------------------------
# Synthetic SMAPI logs
#
# A SMAPI record is one physical line "[HH:mm:ss LEVEL SOURCE] message", where
# the level is padded to five characters (so `INFO ` keeps a trailing space and
# the raw line shows `INFO  SMAPI`) and the message's own leading whitespace
# sits AFTER the prefix (so the skipped-mods header appears on the wire as
# `[<ts> ERROR SMAPI]    Skipped mods`).
# ---------------------------------------------------------------------------

# _log_line <time> <level> <source> <message> — emit one record line. The level
# is padded to five characters; a longer level (CRITICAL) is NOT truncated, which
# is exactly how SMAPI renders it and what shifts the source column.
_log_line() {
    printf '[%s %-5s %s] %s\n' "$1" "$2" "$3" "$4"
}

# _write_log_mod_loaded <dest>
_write_log_mod_loaded() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
    } > "$dest"
}

# _write_log_mod_skipped <dest>
_write_log_mod_skipped() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 0 mods."
        _log_line "12:00:00" "ERROR" "SMAPI" "   Skipped mods"
        _log_line "12:00:00" "ERROR" "SMAPI" "      - ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} ${HARNESS_SKIP_REASON}"
    } > "$dest"
}

# _write_log_mod_absent <dest>
_write_log_mod_absent() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 1 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   Another Mod 1.2.3 by someone-else"
    } > "$dest"
}

# _write_log_zero_mods <dest>
_write_log_zero_mods() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 0 mods."
    } > "$dest"
}

# _write_log_no_header <dest>
_write_log_no_header() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
    } > "$dest"
}

# _write_log_critical_level <dest> — the CRITICAL level is eight characters and
# is NOT padded, so it shifts the source column by three.
_write_log_critical_level() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 1 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
        _log_line "12:00:00" "CRITICAL" "SMAPI" "A critical failure occurred"
    } > "$dest"
}

# _write_log_continuation_lines <dest> — a mod-scoped ERROR record followed by
# two unprefixed continuation lines (a stack trace).
_write_log_continuation_lines() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "ERROR" "$HARNESS_MOD_NAME" "NullReferenceException: Object reference not set to an instance of an object."
        printf '   at StardewValley.Game1.DayStarted()\n'
        printf '   at StardewValley.Game1.Update(GameTime time)\n'
    } > "$dest"
}

# _write_log_crlf_only <dest> — the loaded-mods content plus a continuation-style
# structure, with every line terminated by CRLF. The continuation attaches to a
# non-mod record so the log still yields the same verdict as `mod_loaded`.
_write_log_crlf_only() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
        _log_line "12:00:00" "ERROR" "SMAPI" "A stack trace follows this record"
        printf '   at Example.Method()\n'
        printf '   at Example.Other()\n'
    } > "$dest"
    sed -i 's/$/\r/' "$dest"
}

# _write_log_stale_marker <dest> — the mod's success marker from a PREVIOUS run,
# before the sentinel, followed by an empty current run.
_write_log_stale_marker() {
    local dest="${1-}"
    {
        _log_line "11:59:00" "INFO" "$HARNESS_MOD_NAME" "$HARNESS_MOD_SUCCESS_MARKER"
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 0 mods."
    } > "$dest"
}

# make_log_fixture <dest> <kind> — write a synthetic SMAPI log to <dest>.
#
# Kinds:
#   mod_loaded          the mod is listed in the loaded-mods block
#   mod_skipped         the mod is in the skipped-mods section
#   mod_absent          the block is present but names a different mod
#   zero_mods           the `Loaded 0 mods.` form
#   no_header           the sentinel only — no mod-resolution header
#   critical_level      a CRITICAL (eight-character) level record
#   continuation_lines  an ERROR record followed by unprefixed stack-trace lines
#   crlf_only           the loaded-mods content with CRLF line terminators
#   stale_marker        a previous run's success marker before the sentinel
make_log_fixture() {
    local dest="${1-}"
    local kind="${2-}"
    if [[ -z "$dest" || -z "$kind" ]]; then
        printf 'make_log_fixture: a destination and a kind are required\n' >&2
        return 1
    fi
    fixture_root >/dev/null
    mkdir -p -- "$(dirname -- "$dest")" || return 1
    case "$kind" in
        mod_loaded)         _write_log_mod_loaded "$dest" ;;
        mod_skipped)        _write_log_mod_skipped "$dest" ;;
        mod_absent)         _write_log_mod_absent "$dest" ;;
        zero_mods)          _write_log_zero_mods "$dest" ;;
        no_header)          _write_log_no_header "$dest" ;;
        critical_level)     _write_log_critical_level "$dest" ;;
        continuation_lines) _write_log_continuation_lines "$dest" ;;
        crlf_only)          _write_log_crlf_only "$dest" ;;
        stale_marker)       _write_log_stale_marker "$dest" ;;
        *)
            printf 'make_log_fixture: unknown kind: %s\n' "$kind" >&2
            return 1
            ;;
    esac
    LAST_FIXTURE="$dest"
    return 0
}

# make_stale_log <dest> — a log whose ONLY occurrence of the mod's name is inside
# the skipped-mods entry. It proves that an unscoped whole-log search for the mod
# name is wrong: the name is present, yet the run must fail.
make_stale_log() {
    local dest="${1-}"
    if [[ -z "$dest" ]]; then
        printf 'make_stale_log: a destination is required\n' >&2
        return 1
    fi
    fixture_root >/dev/null
    mkdir -p -- "$(dirname -- "$dest")" || return 1
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 0 mods."
        _log_line "12:00:00" "ERROR" "SMAPI" "   Skipped mods"
        _log_line "12:00:00" "ERROR" "SMAPI" "      - ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} ${HARNESS_SKIP_REASON}"
    } > "$dest"
    LAST_FIXTURE="$dest"
    return 0
}

# ---------------------------------------------------------------------------
# Cleanup
# ---------------------------------------------------------------------------

# cleanup_fixtures — remove the fixture tree. Idempotent.
cleanup_fixtures() {
    if [[ -n "$FIXTURE_ROOT" && -d "$FIXTURE_ROOT" ]]; then
        rm -rf -- "$FIXTURE_ROOT" 2>/dev/null || true
    fi
    return 0
}

# cleanup_sandbox — remove the whole per-test sandbox (fixtures plus the
# redirected state roots). Idempotent.
cleanup_sandbox() {
    cleanup_fixtures
    if [[ -n "$HARNESS_SANDBOX" && -d "$HARNESS_SANDBOX" ]]; then
        rm -rf -- "$HARNESS_SANDBOX" 2>/dev/null || true
    fi
    return 0
}

# ---------------------------------------------------------------------------
# Skipping
# ---------------------------------------------------------------------------

# require_tool <tool> <label> — returns 0 when <tool> is on PATH. When it is
# absent the calling test is skipped: a `ok - <label> (skipped: <tool> missing)`
# line is printed and the function returns 1, so the caller can bail out with
#
#     if ! require_tool Xvfb "Xvfb is available"; then
#         return 0
#     fi
require_tool() {
    local tool="${1-}"
    local label="${2-}"
    if [[ -z "$label" ]]; then
        label="${tool} is available"
    fi
    if command -v "$tool" >/dev/null 2>&1; then
        return 0
    fi
    skip "$label" "$tool"
    return 1
}

# integration_skipped — true when the integration suite was asked to be skipped.
# T012 declares SMOKE_SKIP_INTEGRATION=1; SKIP_INTEGRATION is accepted as an
# alias.
integration_skipped() {
    case "${SMOKE_SKIP_INTEGRATION:-${SKIP_INTEGRATION:-}}" in
        1|true|yes|on) return 0 ;;
        *) return 1 ;;
    esac
}

# ---------------------------------------------------------------------------
# Summary and termination
# ---------------------------------------------------------------------------

# assert_summary — print the totals.
assert_summary() {
    printf '# tests: %d, passed: %d, failed: %d\n' "$TESTS_RUN" "$TESTS_PASSED" "$TESTS_FAILED"
}

# finish_tests — print the summary and exit: 1 when any assertion failed, 0
# otherwise. Every test file MUST end with this call.
finish_tests() {
    HARNESS_FINISHED=1
    assert_summary
    if (( TESTS_FAILED > 0 )); then
        exit 1
    fi
    exit 0
}

# ---------------------------------------------------------------------------
# EXIT trap
#
# Registered once, at load time. It removes the sandbox and guarantees a
# non-zero exit when an assertion failed but the test file forgot to call
# finish_tests. A trap the caller had installed before sourcing this library is
# run as well.
# ---------------------------------------------------------------------------

# _harness_run_previous_trap <definition> — run the action of a trap definition
# captured with `trap -p EXIT`. The definition looks like
#
#     trap -- 'echo bye' EXIT
#
# so the action is recovered by letting the shell re-parse it. The action runs in
# a subshell: an `exit` inside it must not pre-empt the harness's own exit code.
_harness_run_previous_trap() {
    local definition="${1-}"
    [[ -n "$definition" ]] || return 0
    local rest="${definition#trap }"
    rest="${rest#-- }"
    local action=""
    eval "set -- $rest" 2>/dev/null || return 0
    action="${1-}"
    [[ -n "$action" ]] || return 0
    ( eval "$action" ) || true
    return 0
}

# shellcheck disable=SC2317  # only ever reached through the EXIT trap
_harness_exit_trap() {
    local rc=$?
    trap - EXIT
    cleanup_sandbox
    if (( HARNESS_FINISHED == 0 )); then
        # The test file did not call finish_tests. A failed assertion must still
        # make the run fail, and the summary explains what happened.
        if (( TESTS_FAILED > 0 )); then
            rc=1
        fi
        if (( rc != 0 || TESTS_FAILED > 0 )); then
            assert_summary
        fi
    fi
    _harness_run_previous_trap "$HARNESS_PREVIOUS_EXIT_TRAP"
    exit "$rc"
}

trap _harness_exit_trap EXIT
