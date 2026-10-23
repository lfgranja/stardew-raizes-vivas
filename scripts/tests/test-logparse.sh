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
# The script under test is a PIPELINE, not a log parser: it pre-flights,
# builds, launches the game and then POLLS the log for the run sentinel before
# it evaluates the gate. Pointing `--smapi-log` at a static fixture therefore
# measures nothing at all — the fixture carries a fixed, past sentinel, the
# script classifies it as stale (FR-002's freshness rule: only a sentinel not
# older than the launch timestamp T0 counts) and then waits for a sentinel
# that never arrives.
#
# The suite therefore drives the REAL pipeline and lets the LAUNCH produce the
# log:
#
#   * the fixture is written to a TEMPLATE path, never to the log path, so the
#     script's own pre-launch cleanup (which removes a stale log) cannot
#     delete the fixture before the launch;
#   * the template is staged with `stage_log_for_launch`, and the fake
#     apphost the script launches copies it to the LOG path, stamping the run
#     sentinel with the CURRENT UTC time. Because the apphost starts after
#     the script recorded T0, the sentinel it writes is newer than T0 by
#     construction, and FR-002's freshness rule is satisfied naturally;
#   * the script is then run with `--game-path`/`--mods-path` pointing at the
#     synthetic game installation, `--smapi-log` pointing at the path the
#     apphost writes, and small `--timeout`/`--grace-period` values so a run
#     that never produces a verdict gives up quickly.
#
# Every fixture lives under the harness's per-test sandbox; the script under
# test never sees the real game installation. A hard harness-level deadline
# wraps every invocation, so a script that hangs can never outlive the suite.
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

# The game-launch-to-verdict deadline handed to the script under test. The
# fake apphost writes the log the moment it starts, so the wait is short; the
# value only matters when a run never produces a verdict.
readonly RUN_TIMEOUT=20

# The SIGTERM-then-SIGKILL escalation window. Short on purpose: the suite
# must not linger on a child that ignores SIGTERM.
readonly GRACE_PERIOD=2

# The bound on the rebuild-and-verify phase. The script's own default (300 s)
# is stated explicitly so the run is reproducible and so the harness-level
# deadline below can be set above it.
readonly BUILD_TIMEOUT=300

# The hard harness-level deadline. It is deliberately ABOVE every budget the
# script itself enforces (the build bound plus the launch-to-verdict window
# plus the grace period), so the script's own timeout fires first and the run
# reports the code the script chose. It exists only so that a script that
# hangs cannot outlive the suite; `timeout`'s 124 is reported verbatim and is
# therefore visible as a deadline that fired.
readonly HARNESS_DEADLINE=360

# The fake game installation, the directory the fixture TEMPLATES live in and
# the directory the script under test POLLS as the SMAPI log.
#
# The template directory and the log directory are deliberately different:
# the script removes a stale log before it launches, so a fixture that lived
# at the log path would be deleted before the apphost could copy it.
readonly GAME_DIR="${FIXTURE_ROOT}/game"
readonly TEMPLATE_DIR="${FIXTURE_ROOT}/templates"
readonly LIVE_DIR="${FIXTURE_ROOT}/live"

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

# _stage_and_run <template> <target> [args...] — the one place a run happens.
#
# It stages the template, so the fake apphost the script launches copies it
# to <target> with a FRESH run sentinel, and then runs the script with the
# pipeline's own options. The epoch recorded just before the run is left in
# RUN_STARTED_EPOCH, so a caller can assert that the log the launch produced
# belongs to THIS run.
RUN_STARTED_EPOCH=0
_stage_and_run() {
    local template="${1-}"
    local target="${2-}"
    shift 2

    stage_log_for_launch "$template" "$target"
    RUN_STARTED_EPOCH="$(date -u +%s)"

    run_script_deadline "$HARNESS_DEADLINE" \
        --game-path "$GAME_DIR" \
        --mods-path "$GAME_DIR/Mods" \
        --smapi-log "$target" \
        --timeout "$RUN_TIMEOUT" \
        --grace-period "$GRACE_PERIOD" \
        --build-timeout "$BUILD_TIMEOUT" \
        "$@"
}

# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------

make_fake_game_dir "$GAME_DIR"
mkdir -p -- "$TEMPLATE_DIR" "$LIVE_DIR"

# Set once the staging check has run, so it is performed exactly once.
staging_checked=0

# _assert_staging <target> <not-older-than-epoch> <label-prefix>
#
# Asserts that the LAUNCH produced a log at the log path and that the log
# belongs to the run that is starting now:
#
#   * the log path holds a log at all — a script that never launched the
#     apphost leaves the path empty, and that is the reason this assertion
#     gives;
#   * the log's run sentinel is a TRACE-level SMAPI record;
#   * the sentinel's timestamp is not older than the epoch the run started
#     at, which is FR-002's freshness rule (a sentinel not older than T0).
#
# The mechanism it checks does not depend on the fixture, so the caller runs
# it once.
_assert_staging() {
    local target="${1-}"
    local not_older_than="${2-}"
    local prefix="${3-}"
    local sentinel=""
    local epoch=""

    if [[ ! -f "$target" ]]; then
        fail "${prefix}: the launch wrote a log at the log path" \
            "no log at ${target} — the apphost was never launched, or it did not produce a log"
        return 0
    fi
    pass "${prefix}: the launch wrote a log at the log path"

    sentinel="$(grep -oE 'Log started at [0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}' "$target" 2>/dev/null | head -n 1 || true)"

    if grep -qE '^\[[0-9]{2}:[0-9]{2}:[0-9]{2}[[:space:]]+TRACE[[:space:]]+SMAPI\] Log started at [0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2} UTC' "$target"; then
        pass "${prefix}: the produced log's sentinel is a TRACE-level SMAPI record"
    else
        fail "${prefix}: the produced log's sentinel is a TRACE-level SMAPI record" \
            "no '[HH:mm:ss TRACE SMAPI] Log started at <s-format> UTC' line in ${target}"
    fi

    if [[ -z "$sentinel" ]]; then
        fail "${prefix}: the produced log's sentinel is not older than the run" \
            "no 'Log started at <s-format>' sentinel in ${target}"
        return 0
    fi

    epoch="$(date -u -d "${sentinel#Log started at }" +%s 2>/dev/null || printf '')"
    if [[ -n "$epoch" ]] && (( epoch >= not_older_than )); then
        pass "${prefix}: the produced log's sentinel is not older than the run"
    else
        fail "${prefix}: the produced log's sentinel is not older than the run" \
            "sentinel '${sentinel}' is older than the run's start (${not_older_than})"
    fi

    return 0
}

# ---------------------------------------------------------------------------
# The gate-leg property table
#
# The invariant asserted for every row: the exit code is the expected one, the
# verdict stream carries the expected content, and a non-zero verdict never
# prints the success message.
#
# The `no_header` row deserves a word, because three DIFFERENT situations
# produce a log with no `Loaded N mods:` header, and they are not the same
# failure:
#
#   * the log is COMPLETE and carries no header at all — the game resolved
#     nothing and never wrote the block, so the gate has nothing to evaluate.
#     This is the row: a malformed/empty gate input, reported as 13.
#   * the header has simply NOT ARRIVED YET — the game is still resolving the
#     mods when the deadline expires. That is the TIMEOUT (4), because the
#     script was still waiting for the block (FR-002, phase 2).
#   * the game ABORTED before mod resolution — the sentinel is there, the
#     header never comes and the process is gone. That is 17 (FR-007's
#     log-based classification, which takes precedence over the status
#     translation).
#
# The distinction is a property of the TIMING, not of the content, so only the
# first situation is reachable from a fixture; the row therefore asserts 13
# and the other two are recorded here.
# ---------------------------------------------------------------------------

for row in "${LOG_GATE_TABLE[@]}"; do
    IFS='|' read -r kind expected_code expected_content forbidden_code <<< "$row"

    template="${TEMPLATE_DIR}/${kind}.txt"
    target="${LIVE_DIR}/${kind}.txt"

    # The template is the FIXTURE; the target is the log the launch produces.
    # They are different paths, so the script's pre-launch cleanup cannot
    # remove the fixture before the apphost has copied it.
    rm -f -- "$target"
    make_log_fixture "$template" "$kind"

    case_started="$(date -u +%s)"
    _stage_and_run "$template" "$target"
    case_finished="$(date -u +%s)"

    printf '#   [%s] expected %s, took %ss\n' \
        "$kind" "$expected_code" "$((case_finished - case_started))"

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

    # The template must survive the run: it is the fixture, and the log path
    # is a different file. This is the property that keeps the pre-launch
    # cleanup from deleting the fixture the launch is supposed to copy.
    assert_file_exists "$template" "log gate [$kind] the fixture template survives the run"

    # The staging check runs ONCE, on the first row: the mechanism it proves
    # (the launch produces a fresh, current-run log) does not depend on the
    # fixture, and every extra invocation of the script under test costs a
    # real rebuild.
    if (( staging_checked == 0 )); then
        staging_checked=1
        _assert_staging "$target" "$RUN_STARTED_EPOCH" "staging [$kind]"
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

false_pass_template="${TEMPLATE_DIR}/only_in_skipped.txt"
false_pass_target="${LIVE_DIR}/only_in_skipped.txt"
rm -f -- "$false_pass_target"
make_stale_log "$false_pass_template"

_stage_and_run "$false_pass_template" "$false_pass_target"

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

sentinel_fixture="${TEMPLATE_DIR}/sentinel_probe.txt"
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

indent_fixture="${TEMPLATE_DIR}/indent_probe.txt"
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

crlf_fixture="${TEMPLATE_DIR}/crlf_probe.txt"
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

crlf_stripped="${TEMPLATE_DIR}/crlf_probe.stripped.txt"
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
