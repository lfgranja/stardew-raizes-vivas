#!/usr/bin/env bash
#
# scripts/tests/test-logparse.sh — T011: the FR-003 record-shape and gate-leg
# property suite for the smoke-validation script.
#
# WHAT IS ASSERTED
# -----------------
# The log gate of `scripts/smoke-validation.sh` is a three-legged AND over the
# NORMALISED SMAPI record stream (spec.md FR-003 and FR-006):
#
#   1. the mod is listed in SMAPI's loaded-mods block;
#   2. the mod is NOT in SMAPI's skipped-mods section;
#   3. no record whose LEVEL label is ERROR/ALERT and whose SOURCE label is the
#      mod's manifest name.
#
# The suite is TABLE-DRIVEN: one table maps a synthetic log fixture to the
# exact exit code the script must produce for it, and the SAME invariant is
# asserted for every row:
#
#   "the script exits <code>; the report carries the expected content; and a
#    non-zero verdict never claims success"
#
# Three further properties are asserted outside the table:
#
#   * the FALSE-PASS GUARD — the mod's name must never be searched across the
#     whole log. A log whose only occurrence of the name is inside the
#     skipped-mods entry must still fail (5), never pass (0);
#   * the SENTINEL PROPERTY — the run boundary is a TRACE-level record, so a
#     parser that reads only INFO-and-above would never find it;
#   * the INDENTATION PROPERTY — the skipped-mods header's three leading
#     spaces live INSIDE the message, after the record prefix, so an anchor of
#     the form "^   Skipped mods" matches nothing in the raw file and the
#     implementation must match the MESSAGE, not the whole line.
#
# HOW THE SCRIPT IS DRIVEN
# -------------------------
# The script is invoked with `--smapi-log` pointing at a synthetic fixture and
# with a small `--timeout`, so a script that polls for the sentinel gives up
# quickly instead of blocking the suite. Every fixture lives under the
# harness's per-test sandbox; the script under test never sees the real game
# installation.
#
# TDD STATE
# ---------
# This suite is written BEFORE the implementation (T016/T017) and is expected
# to FAIL until the log gate exists. Every failure is a clean assertion
# failure: the suite never crashes, never trips an unbound variable and never
# hangs.

set -Eeuo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/harness.sh"

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

# The success message FR-006 mandates, byte for byte.
readonly SUCCESS_LINE="SUCCESS: Mod loaded without errors."

# SMAPI's stated skip reason with the leading "because " marker stripped: the
# report must quote the text that FOLLOWS the " because " marker of the skip
# entry (FR-003, success gate leg 2). The harness constant carries the marker
# itself, without the leading space the entry has in front of it.
readonly SKIP_REASON_TEXT="${HARNESS_SKIP_REASON#because }"

# The deadline handed to the script under test. Small on purpose: the suite
# must not block while the script polls for a sentinel it will never see.
readonly RUN_TIMEOUT=2

# The fake game installation and the directory the synthetic logs live in.
readonly GAME_DIR="${FIXTURE_ROOT}/game"
readonly LOG_DIR="${FIXTURE_ROOT}/logs"

# ---------------------------------------------------------------------------
# The table
#
#   <kind> | <expected exit code> | <expected report content> | <forbidden code>
#
# The columns are separated by "|". The content column may be empty, and it may
# hold several needles separated by the "@@" token. The forbidden-code column
# may be empty. Adding a row is one edit of this array.
#
# The "forbidden code" column carries the misclassification each fixture is
# specifically there to exclude: a `Loaded 0 mods.` log is a mod-load failure
# (5) and not a malformed log (13); a stale marker before the sentinel is a
# mod-load failure (5) and not a pass (0).
# ---------------------------------------------------------------------------

readonly LOG_GATE_TABLE=(
    # kind                 | code | expected report content                                   | forbidden
    "mod_loaded|0|${SUCCESS_LINE}|"
    "mod_skipped|5|${SKIP_REASON_TEXT}|"
    "mod_absent|5||"
    "zero_mods|5||13"
    "no_header|13||"
    "critical_level|0|${SUCCESS_LINE}|"
    "continuation_lines|5|NullReferenceException: Object reference not set to an instance of an object.@@at StardewValley.Game1.DayStarted()|"
    "crlf_only|0|${SUCCESS_LINE}|"
    "stale_marker|5||0"
)

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

# _run_output — the script's stdout and stderr, combined.
#
# The verdict goes to stdout and the diagnostics to stderr (FR-006, FR-007),
# but the spec does not say which of the two carries a given element of a
# failure report, so a content assertion looks at both streams.
_run_output() {
    printf '%s\n%s' "$RUN_STDOUT" "$RUN_STDERR"
}

# _assert_report_contains <output> <cell> <label-prefix>
#
# Asserts that the output contains every needle of the table's content cell.
# The cell's needles are separated by the "@@" token. An empty cell asserts
# nothing.
_assert_report_contains() {
    local output="${1-}"
    local cell="${2-}"
    local prefix="${3-}"
    local expanded=""
    local needle=""

    [[ -n "$cell" ]] || return 0

    expanded="${cell//@@/$'\n'}"
    while IFS= read -r needle; do
        [[ -n "$needle" ]] || continue
        assert_contains "$output" "$needle" "${prefix}: mentions '${needle}'"
    done <<< "$expanded"

    return 0
}

# ---------------------------------------------------------------------------
# The gate-leg property table
#
# The invariant asserted for every row: the exit code is the expected one, the
# verdict stream carries the expected content, and a non-zero verdict never
# prints the success message.
# ---------------------------------------------------------------------------

make_fake_game_dir "$GAME_DIR"

for row in "${LOG_GATE_TABLE[@]}"; do
    IFS='|' read -r kind expected_code expected_content forbidden_code <<< "$row"

    fixture="${LOG_DIR}/${kind}.txt"
    make_log_fixture "$fixture" "$kind"

    run_script \
        --game-path "$GAME_DIR" \
        --mods-path "$GAME_DIR/Mods" \
        --smapi-log "$fixture" \
        --timeout "$RUN_TIMEOUT"

    output="$(_run_output)"

    assert_exit "$expected_code" "log gate [$kind] exits $expected_code"

    if [[ "$expected_code" == "0" ]]; then
        # The verdict goes to stdout (FR-006); the success message is the
        # content cell of this row.
        _assert_report_contains "$RUN_STDOUT" "$expected_content" "log gate [$kind] stdout"
    else
        # A failing run must never claim success, on either stream.
        assert_not_contains "$output" "$SUCCESS_LINE" \
            "log gate [$kind] never claims success"
        _assert_report_contains "$output" "$expected_content" "log gate [$kind] report"
    fi

    if [[ -n "$forbidden_code" ]]; then
        assert_ne "$forbidden_code" "$RUN_CODE" \
            "log gate [$kind] does not exit $forbidden_code"
    fi
done

# ---------------------------------------------------------------------------
# The false-pass guard — the unscoped-search property
#
# `make_stale_log` writes a log in which the mod's name appears ONLY inside
# the skipped-mods entry. A script that searched the whole log for the name
# would find it there and report 0 — a FALSE PASS. The mod was not loaded, so
# the verdict must be 5, the run must never claim success, and the report must
# carry SMAPI's stated reason.
# ---------------------------------------------------------------------------

false_pass_fixture="${LOG_DIR}/only_in_skipped.txt"
make_stale_log "$false_pass_fixture"

run_script \
    --game-path "$GAME_DIR" \
    --mods-path "$GAME_DIR/Mods" \
    --smapi-log "$false_pass_fixture" \
    --timeout "$RUN_TIMEOUT"

false_pass_output="$(_run_output)"

assert_exit 5 "false-pass guard: a log whose only occurrence of the mod name is a skip entry exits 5"
assert_ne 0 "$RUN_CODE" "false-pass guard: a log whose only occurrence of the mod name is a skip entry never exits 0"
assert_not_contains "$false_pass_output" "$SUCCESS_LINE" \
    "false-pass guard: the run never claims success"
_assert_report_contains "$false_pass_output" "$SKIP_REASON_TEXT" "false-pass guard report"

# ---------------------------------------------------------------------------
# The sentinel property
#
# The run boundary is a TRACE-level record with source SMAPI. A parser that
# reads only INFO-and-above records would never find it, so the script MUST
# read TRACE. The shape is asserted against the raw fixture with grep — this
# suite does not parse the log itself.
# ---------------------------------------------------------------------------

sentinel_fixture="${LOG_DIR}/sentinel_probe.txt"
make_log_fixture "$sentinel_fixture" mod_loaded

if grep -qF "$HARNESS_RUN_SENTINEL" "$sentinel_fixture"; then
    pass "sentinel: the fixture carries the run sentinel"
else
    fail "sentinel: the fixture carries the run sentinel" \
        "no '${HARNESS_RUN_SENTINEL}' line in ${sentinel_fixture}"
fi

if grep -qE '^\[[0-9]{2}:[0-9]{2}:[0-9]{2}[[:space:]]+TRACE[[:space:]]+SMAPI\] Log started at ' "$sentinel_fixture"; then
    pass "sentinel: the run sentinel is a TRACE-level SMAPI record"
else
    fail "sentinel: the run sentinel is a TRACE-level SMAPI record" \
        "no '[HH:mm:ss TRACE SMAPI] Log started at ...' line in ${sentinel_fixture}"
fi

if grep -qE '^\[[0-9]{2}:[0-9]{2}:[0-9]{2}[[:space:]]+(INFO|WARN |ERROR|ALERT)[[:space:]]+SMAPI\] Log started at ' "$sentinel_fixture"; then
    fail "sentinel: the run sentinel is not an INFO-and-above record" \
        "an INFO-and-above sentinel line is present in ${sentinel_fixture}"
else
    pass "sentinel: the run sentinel is not an INFO-and-above record"
fi

# ---------------------------------------------------------------------------
# The indentation property
#
# SMAPI applies the record prefix ONCE to the whole message, so the message's
# own leading whitespace sits AFTER the prefix. The skipped-mods header
# therefore appears on the wire as "[12:00:00 ERROR SMAPI]    Skipped mods" —
# the three leading spaces are INSIDE the message — and an anchor of the form
# "^   Skipped mods" matches nothing in the raw file. The implementation must
# match the MESSAGE, not the whole line.
# ---------------------------------------------------------------------------

indent_fixture="${LOG_DIR}/indent_probe.txt"
make_log_fixture "$indent_fixture" mod_skipped

if grep -qxF '[12:00:00 ERROR SMAPI]    Skipped mods' "$indent_fixture"; then
    pass "indentation: the skipped-mods header line is '[12:00:00 ERROR SMAPI]    Skipped mods'"
else
    fail "indentation: the skipped-mods header line is '[12:00:00 ERROR SMAPI]    Skipped mods'" \
        "no such line in ${indent_fixture}"
fi

if grep -q '^   Skipped mods' "$indent_fixture"; then
    fail "indentation: an anchor of the form '^   Skipped mods' matches nothing in the raw file" \
        "'^   Skipped mods' matched a line in ${indent_fixture}"
else
    pass "indentation: an anchor of the form '^   Skipped mods' matches nothing in the raw file"
fi

# The skip entries carry SIX leading spaces inside the message; together with
# the prefix's own separator that is seven spaces before the "- ".
if grep -qE '^\[12:00:00 ERROR SMAPI\] {7}- ' "$indent_fixture"; then
    pass "indentation: the skip entry carries six leading spaces inside the message"
else
    fail "indentation: the skip entry carries six leading spaces inside the message" \
        "no '[12:00:00 ERROR SMAPI]       - ...' line in ${indent_fixture}"
fi

# ---------------------------------------------------------------------------
# The CRLF property (fixture shape)
#
# SMAPI terminates every record with CRLF, and GNU grep does NOT strip the CR
# on Linux, so a pattern ending in "$" never matches a CRLF-terminated line.
# The script MUST strip the trailing CR into a working copy before matching.
# These assertions document the shape that makes the `crlf_only` table row
# meaningful; the row itself (expected exit 0) is what proves the script
# strips the CR.
# ---------------------------------------------------------------------------

crlf_fixture="${LOG_DIR}/crlf_probe.txt"
make_log_fixture "$crlf_fixture" crlf_only

crlf_cr_lines="$(grep -c $'\r$' "$crlf_fixture" || true)"
crlf_total_lines="$(wc -l < "$crlf_fixture" | tr -d '[:space:]')"
assert_eq "$crlf_total_lines" "$crlf_cr_lines" \
    "crlf: every line of the CRLF fixture is CR-terminated"

if grep -q 'Loaded 3 mods:$' "$crlf_fixture"; then
    fail "crlf: a \$-anchored pattern does not match the raw CRLF file" \
        "'Loaded 3 mods:\$' matched a line in ${crlf_fixture}"
else
    pass "crlf: a \$-anchored pattern does not match the raw CRLF file"
fi

crlf_stripped="${LOG_DIR}/crlf_probe.stripped.txt"
sed 's/\r$//' "$crlf_fixture" > "$crlf_stripped"
if grep -q 'Loaded 3 mods:$' "$crlf_stripped"; then
    pass "crlf: the same pattern matches once the CR is stripped"
else
    fail "crlf: the same pattern matches once the CR is stripped" \
        "'Loaded 3 mods:\$' did not match the CR-stripped copy of ${crlf_fixture}"
fi

# ---------------------------------------------------------------------------

cleanup_fixtures
finish_tests
