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
# Eight further properties are asserted outside the table:
#
#   * the FALSE-PASS GUARD — the mod's name must never be searched across the
#     whole log. A log whose only occurrence of the name is inside the
#     skipped-mods entry must still fail (5), never pass (0);
#   * the SENTINEL PROPERTY — the run boundary is a TRACE-level record, so a
#     parser that reads only INFO-and-above would never find it;
#   * the SENTINEL-FRESHNESS PROPERTY — the sentinel's timestamp carries no
#     offset, so the comparison against the launch timestamp MUST be made as
#     UTC, and a malformed timestamp is reported rather than treated as
#     stale;
#   * the INDENTATION PROPERTY — the skipped-mods header's three leading
#     spaces live INSIDE the message, after the record prefix, so an anchor of
#     the form "^   Skipped mods" matches nothing in the raw file and the
#     implementation must match the MESSAGE, not the whole line;
#   * the CRLF PROPERTY — SMAPI terminates every record with CRLF and GNU
#     grep does not strip the CR, so the script MUST strip it into a working
#     copy before it matches anything;
#   * the CRASH-CONTEXT PROPERTY — the context of a failure must come from
#     the NORMALISED record stream and must be the matched failure record
#     together with its continuation lines, never a fixed `tail -N` window;
#   * the INODE-GUARD PROPERTY — the log's inode is recorded BEFORE the
#     stale log is removed and must have changed once the log was recreated;
#   * the STALE-LINE PROPERTY — a stale `Log started at` line and a stale
#     `Loaded N mods:` header left BEFORE the run sentinel can neither
#     satisfy the readiness wait nor end it early: only the post-sentinel
#     lines decide the wait.
#
# Four of the table's rows carry assertions the table's columns cannot
# express: the `mod_trace_after_error` row asserts that the mod's Trace
# record is part of the reported CONTEXT, the `mod_success_marker` row
# asserts the `mod-success-marker:` line on stdout, the `mod_absent` row
# asserts the shape of the context of a run in which no failure record
# matched, and the `mod_absent_long_tail` row asserts the 30-record cap of
# that context against a fixture whose trailing part is LONGER than the cap.
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
# One case needs a STALE sentinel, which the harness's apphost cannot produce
# (it rewrites every `Log started at ... UTC` line to the current time), so
# that case drives its own apphost stub in its own game installation.
#
# Every fixture lives under the harness's per-test sandbox; the script under
# test never sees the real game installation. A hard harness-level deadline
# wraps every invocation, so a script that hangs can never outlive the suite.
#
# TDD STATE
# ---------
# The log gate EXISTS (T016/T017 were implemented), so this suite is no longer
# expected to fail wholesale: it runs against the shipped script and guards
# the behaviour that script must keep.
#
# TWO of the properties asserted here are RED on purpose — they are written
# BEFORE the implementation that satisfies them, so the suite stays red until
# that implementation lands:
#
#   * the `graphics_device` row — a graphics-device failure whose text spans
#     CONTINUATION lines must be classified as 3 (a graphics failure) and not
#     as 17 (an abort before mod resolution). The implementation matches a
#     record's message alone, so the failure text that lands in the
#     continuation lines is missed today;
#   * the stale-lines case — the readiness probes must be scoped to the
#     POST-SENTINEL region. The implementation reads the FIRST sentinel of the
#     log, so a stale sentinel shadows the current run's own sentinel and the
#     wait never reaches phase 2.
#
# Every failure is a clean assertion failure: the suite never crashes, never
# trips an unbound variable and never hangs.

set -Eeuo pipefail

# The harness is linted IN ITS OWN RIGHT by the same hook step (it matches
# `scripts/tests/**/*.sh`), so it is deliberately not followed from here:
# `-x` would pull it in a second time and report its findings against this
# file.
# shellcheck disable=SC1091  # the harness is linted separately, by the same step
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

# The message of the mod-scoped ALERT record the alert fixture carries. The
# failure-marker rule names ERROR *and* ALERT (FR-003), so an ALERT record is
# a failure marker exactly like an ERROR one.
readonly MOD_ALERT_MESSAGE="The mod's alert handler threw an InvalidOperationException"

# The mod's own TRACE diagnostic line. FR-003's "Trace diagnostics (MUST)":
# the exception type and the HResult come from the MOD's own Trace records.
# A Trace record never fails the run — but after a failure record it MUST be
# part of the reported context (FR-007).
readonly MOD_TRACE_DIAGNOSTIC="RegisterEvents exception type: System.InvalidOperationException (HResult: 0x80131509)"

# The graphics-device failure SMAPI 4.5.2 reports: ONE ERROR record whose
# message ends at "Technical details:", with the failure text in the
# CONTINUATION lines that follow it. The record prefix is applied once to the
# whole message, so the continuation lines carry no prefix (FR-003).
readonly GRAPHICS_DEVICE_MESSAGE="Failed to initialize the graphics device. Technical details:"
readonly GRAPHICS_DEVICE_DETAIL="Failed to create graphics device!"

# The cap FR-007 puts on the context of a run in which no failure record
# matched: the trailing RECORDS of the normalised stream, at most 30 of them.
readonly MAX_CONTEXT_RECORDS=30

# The number of trailing records the long-tail fixture carries. It is
# deliberately GREATER than the cap, so the cap is observable: a context that
# held every trailing record would exceed it, and one that held none of the
# earlier ones would not be the TRAILING window.
readonly TRAILING_TAIL_RECORDS=40

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
    "mod_absent|5|Loaded 1 mods:@@Another Mod 1.2.3 by someone-else|"
    "zero_mods|5||13"
    "no_header|17||"
    "critical_level|0|${SUCCESS_LINE}|"
    "continuation_lines|5|NullReferenceException: Object reference not set to an instance of an object.@@at StardewValley.Game1.DayStarted()|"
    "crlf_only|0|${SUCCESS_LINE}|"
    "stale_marker|5||0"
    "mod_alert|5|${MOD_ALERT_MESSAGE}|"
    "mod_warn|0|${SUCCESS_LINE}|"
    "mod_trace_after_error|5|${MOD_TRACE_DIAGNOSTIC}|"
    "mod_trace_only|0|${SUCCESS_LINE}|"
    "mod_success_marker|0|${SUCCESS_LINE}|"
    "mod_absent_long_tail|5||"
    "graphics_device|3|the game could not create a graphics device|17"
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

    _stage_and_run_in "$GAME_DIR" "$template" "$target" "$@"
}

# _stage_and_run_in <game-dir> <template> <target> [args...] — the same run,
# against a game installation of the caller's choosing.
#
# A case that needs its OWN apphost stub (one that does not rewrite the run
# sentinel) needs its own game directory too, because the apphost lives in
# the game installation the script launches.
_stage_and_run_in() {
    local game_dir="${1-}"
    local template="${2-}"
    local target="${3-}"
    shift 3

    stage_log_for_launch "$template" "$target"
    RUN_STARTED_EPOCH="$(date -u +%s)"

    run_script_deadline "$HARNESS_DEADLINE" \
        --game-path "$game_dir" \
        --mods-path "$game_dir/Mods" \
        --smapi-log "$target" \
        --timeout "$RUN_TIMEOUT" \
        --grace-period "$GRACE_PERIOD" \
        --build-timeout "$BUILD_TIMEOUT" \
        "$@"
}

# _report_context <report> — the text of the report's CONTEXT section: the
# lines between the "Context:" line and the trailing "Fix:" line.
#
# The report is the script's failure report (FR-007), whose shape is
#
#     What:  ...
#     Where: ...
#     Context:
#       <the context>
#     Fix:  ...
#
# so the section is delimited by the two markers.
_report_context() {
    local report="${1-}"
    local line
    local collecting=0

    while IFS= read -r line || [[ -n "$line" ]]; do
        if (( collecting == 0 )); then
            if [[ "$line" == *"Context:"* ]]; then
                collecting=1
            fi
            continue
        fi
        if [[ "$line" == *"Fix:"* ]]; then
            break
        fi
        printf '%s\n' "$line"
    done <<< "$report"

    return 0
}

# _count_lines <text> — the number of physical lines in the text.
#
# The empty text has no lines at all, which is why the empty case is answered
# before the count is taken.
_count_lines() {
    local text="${1-}"

    if [[ -z "$text" ]]; then
        printf '0'
        return 0
    fi

    printf '%s' "$text" | awk 'END { print NR + 0 }'
    return 0
}

# _normalised_records_after_sentinel <template> — the messages of the records
# that FOLLOW the run sentinel in the fixture, one per line.
#
# This is the trailing part of the fixture's NORMALISED record stream
# (FR-003): a record is a physical line that carries the record prefix, and
# its message is everything after that prefix; a continuation line carries no
# prefix and IS its own message.
_normalised_records_after_sentinel() {
    local template="${1-}"
    local line
    local seen_sentinel=0

    [[ -f "$template" ]] || return 0

    while IFS= read -r line || [[ -n "$line" ]]; do
        if (( seen_sentinel == 0 )); then
            if [[ "$line" == *"Log started at "* ]]; then
                seen_sentinel=1
            fi
            continue
        fi
        if [[ "$line" =~ ^\[[^]]*\]\ (.*)$ ]]; then
            printf '%s\n' "${BASH_REMATCH[1]}"
        else
            printf '%s\n' "$line"
        fi
    done < "$template"

    return 0
}

# TRAILING_RECORDS — the records that follow the run sentinel in the fixture
# whose trailing window is being examined. Loaded by _load_trailing_records.
TRAILING_RECORDS=()

# _load_trailing_records <template> — load the records that follow the run
# sentinel in the fixture into the array TRAILING_RECORDS.
_load_trailing_records() {
    local template="${1-}"
    local record

    TRAILING_RECORDS=()
    while IFS= read -r record; do
        [[ -n "$record" ]] || continue
        TRAILING_RECORDS+=("$record")
    done < <(_normalised_records_after_sentinel "$template")

    return 0
}

# _trailing_window <template> — the trailing records the context MUST hold:
# the LAST MAX_CONTEXT_RECORDS of the fixture's trailing records, or all of
# them when the fixture carries fewer than the cap.
_trailing_window() {
    # The array is named `window_records` and not `window`: the caller holds
    # the helper's OUTPUT in a variable called `window`, and one name for an
    # array in one scope and a string in another is a trap (SC2178/SC2128).
    local -a window_records=()

    _load_trailing_records "${1-}"

    if (( ${#TRAILING_RECORDS[@]} <= MAX_CONTEXT_RECORDS )); then
        window_records=("${TRAILING_RECORDS[@]+"${TRAILING_RECORDS[@]}"}")
    else
        window_records=("${TRAILING_RECORDS[@]: -MAX_CONTEXT_RECORDS}")
    fi

    if (( ${#window_records[@]} > 0 )); then
        printf '%s\n' "${window_records[@]}"
    fi

    return 0
}

# _records_before_window <template> — the trailing records that the FR-007
# cap leaves OUT of the context: every trailing record but the last
# MAX_CONTEXT_RECORDS of them.
#
# The set is EMPTY when the fixture carries no more trailing records than the
# cap — then the cap excludes nothing.
_records_before_window() {
    # The array is named `omitted_records` and not `omitted`: the caller holds
    # the helper's OUTPUT in a variable called `omitted`, and one name for an
    # array in one scope and a string in another is a trap (SC2178/SC2128).
    local -a omitted_records=()
    local total

    _load_trailing_records "${1-}"
    total=${#TRAILING_RECORDS[@]}

    if (( total <= MAX_CONTEXT_RECORDS )); then
        return 0
    fi

    omitted_records=("${TRAILING_RECORDS[@]:0:total - MAX_CONTEXT_RECORDS}")
    if (( ${#omitted_records[@]} > 0 )); then
        printf '%s\n' "${omitted_records[@]}"
    fi

    return 0
}

# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------

make_fake_game_dir "$GAME_DIR"
mkdir -p -- "$TEMPLATE_DIR" "$LIVE_DIR"

# ---------------------------------------------------------------------------
# The log fixtures this file adds
#
# The harness's builders cover the shapes the ORIGINAL table needed. The
# fixtures below are the ones this file adds, so they are built HERE: a
# fixture is a fixture, wherever the builder that writes it lives, and the
# harness is left untouched.
#
# Every builder writes the run sentinel as its FIRST record, so the sentinel
# the launch stamps is the only one the normalised stream sees.
# ---------------------------------------------------------------------------

# _write_log_mod_alert <dest> — a mod-scoped ALERT record after the
# mod-resolution block.
#
# The failure-marker rule names ERROR *and* ALERT (FR-003), so an ALERT
# record is a failure marker exactly like an ERROR one.
_write_log_mod_alert() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
        _log_line "12:00:00" "ALERT" "$HARNESS_MOD_NAME" "$MOD_ALERT_MESSAGE"
    } > "$dest"
}

# _write_log_mod_warn <dest> — a mod-scoped WARN record.
#
# FR-003: "WARN never fails." The record is diagnostic only, so the run must
# still report success.
_write_log_mod_warn() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
        _log_line "12:00:00" "WARN" "$HARNESS_MOD_NAME" "The mod's warning handler recovered on its own"
    } > "$dest"
}

# _write_log_mod_trace_after_error <dest> — a mod-scoped ERROR record
# followed by a mod-scoped TRACE record.
#
# FR-003's "Trace diagnostics (MUST)": the exception type and the HResult
# come from the MOD's own Trace records. A Trace record never fails the run —
# but FR-007 requires every mod Trace record that FOLLOWS a failure record to
# be part of the reported context.
_write_log_mod_trace_after_error() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
        _log_line "12:00:00" "ERROR" "$HARNESS_MOD_NAME" "The mod's entry handler threw an exception"
        _log_line "12:00:00" "TRACE" "$HARNESS_MOD_NAME" "$MOD_TRACE_DIAGNOSTIC"
    } > "$dest"
}

# _write_log_mod_trace_only <dest> — a mod-scoped TRACE record and NO
# mod-scoped ERROR record.
#
# The Trace record is diagnostic only, so the run must still report success.
_write_log_mod_trace_only() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
        _log_line "12:00:00" "TRACE" "$HARNESS_MOD_NAME" "$MOD_TRACE_DIAGNOSTIC"
    } > "$dest"
}

# _write_log_mod_success_marker <dest> — the mod's OWN success marker, AFTER
# the run sentinel.
#
# FR-006: the marker is a SECONDARY signal — it is reported in the result
# output, but it does not gate the verdict. The `stale_marker` fixture holds
# the NEGATIVE case (a marker from a PREVIOUS run, before the sentinel);
# this one holds the positive case.
_write_log_mod_success_marker() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
        _log_line "12:00:00" "INFO" "$HARNESS_MOD_NAME" "$HARNESS_MOD_SUCCESS_MARKER"
    } > "$dest"
}

# _write_log_mod_absent_long_tail <dest> — the `mod_absent` shape with a long
# tail of trailing records.
#
# FR-007 caps the context of a run in which no failure record matched at 30
# records. A fixture whose trailing part is SHORTER than the cap cannot
# observe the cap at all — every window that holds the trailing records would
# satisfy it — so this fixture carries MORE trailing records than the cap:
# the context MUST hold the LAST 30 of them and NONE of the ones before them.
#
# The tail records are benign SMAPI INFO records: they neither name the mod
# (which would satisfy the "the mod is listed" leg) nor carry an ERROR/ALERT
# level (which would match a failure record and change the context's shape).
_write_log_mod_absent_long_tail() {
    local dest="${1-}"
    local i

    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 1 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   Another Mod 1.2.3 by someone-else"
        for ((i = 1; i <= TRAILING_TAIL_RECORDS; i++)); do
            _log_line "12:00:00" "INFO" "SMAPI" "$(printf 'trailing record %03d' "$i")"
        done
    } > "$dest"
}

# _write_log_graphics_device <dest> — a graphics-device failure whose text
# SPANS continuation lines.
#
# SMAPI 4.5.2 emits ONE ERROR record whose message ends at "Technical
# details:", and the "Failed to create graphics device!" text lands in the
# CONTINUATION lines that follow it. The record prefix is applied once to the
# whole message, so the continuation lines carry no prefix (FR-003).
#
# The fixture carries NO mod-resolution block: SMAPI creates the graphics
# device BEFORE it resolves any mod, so a real graphics failure never writes
# the `Loaded N mods:` block.
_write_log_graphics_device() {
    local dest="${1-}"
    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "ERROR" "SMAPI" "$GRAPHICS_DEVICE_MESSAGE"
        printf '%s\n' "$GRAPHICS_DEVICE_DETAIL"
        printf '%s\n' "GPU: Could not detect."
    } > "$dest"
}

# _make_log_fixture <dest> <kind> — the fixture builders of this file.
#
# The harness's own builders cover the kinds the ORIGINAL table needed; the
# kinds this file adds are built by the builders above. The dispatch keeps
# the table's "adding a row is one edit of this array" property.
_make_log_fixture() {
    local dest="${1-}"
    local kind="${2-}"

    case "$kind" in
        mod_alert)             _write_log_mod_alert "$dest" ;;
        mod_warn)              _write_log_mod_warn "$dest" ;;
        mod_trace_after_error) _write_log_mod_trace_after_error "$dest" ;;
        mod_trace_only)        _write_log_mod_trace_only "$dest" ;;
        mod_success_marker)    _write_log_mod_success_marker "$dest" ;;
        mod_absent_long_tail)  _write_log_mod_absent_long_tail "$dest" ;;
        graphics_device)       _write_log_graphics_device "$dest" ;;
        *)                     make_log_fixture "$dest" "$kind" ;;
    esac
}

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
#   * the log carries NO RUN SENTINEL — it is a foreign or truncated log, not
#     this run's log at all. That is 13.
#   * the header has simply NOT ARRIVED YET — the game is still resolving the
#     mods when the deadline expires. That is the TIMEOUT (4), because the
#     script was still waiting for the block (FR-002, phase 2).
#   * the game ABORTED before mod resolution — the sentinel is there, the
#     header never comes and the process is gone. That is 17 (FR-007's
#     log-based classification, which takes precedence over the status
#     translation).
#
# The fixture carries the sentinel, so it is the THIRD situation and the row
# asserts 17. The first (13) is reachable only with a log that has no
# sentinel at all; the second (4) is a property of the TIMING and is not
# reachable from a fixture.
# ---------------------------------------------------------------------------

for row in "${LOG_GATE_TABLE[@]}"; do
    IFS='|' read -r kind expected_code expected_content forbidden_code <<< "$row"

    template="${TEMPLATE_DIR}/${kind}.txt"
    target="${LIVE_DIR}/${kind}.txt"

    # The template is the FIXTURE; the target is the log the launch produces.
    # They are different paths, so the script's pre-launch cleanup cannot
    # remove the fixture before the apphost has copied it.
    rm -f -- "$target"
    _make_log_fixture "$template" "$kind"

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

    # The per-kind properties the table's columns cannot express: where in the
    # report a text must appear, and how many records the context may hold.
    case "$kind" in
        mod_trace_after_error)
            # FR-003's "Trace diagnostics (MUST)" and FR-007: the mod's own
            # Trace records that FOLLOW the failure record carry the exception
            # type and the HResult, so they MUST be part of the reported
            # CONTEXT — not merely present somewhere in the report.
            context="$(_report_context "$RUN_STDERR")"
            assert_contains "$context" "$MOD_TRACE_DIAGNOSTIC" \
                "log gate [$kind] the context carries the mod's Trace record"
            ;;
        mod_success_marker)
            # FR-006: the mod's own success marker is a SECONDARY signal. It
            # is reported on stdout — as a `mod-success-marker:` line — and it
            # does not gate the verdict.
            assert_contains "$RUN_STDOUT" "mod-success-marker:" \
                "log gate [$kind] stdout carries a mod-success-marker line"
            assert_contains "$RUN_STDOUT" "$HARNESS_MOD_SUCCESS_MARKER" \
                "log gate [$kind] stdout carries the mod's success marker"
            ;;
        mod_absent)
            # FR-007: "When no record matched, the context MUST be the
            # trailing RECORDS of the normalised stream, capped at 30
            # records." No record matched here — the mod is simply absent from
            # the loaded-mods block — so the context is the trailing part of
            # the fixture's normalised stream.
            context="$(_report_context "$RUN_STDERR")"

            if [[ -n "$context" ]]; then
                pass "log gate [$kind] the report carries a Context section"
            else
                fail "log gate [$kind] the report carries a Context section" \
                    "no context section in the failure report"
            fi

            trailing_records="$(_normalised_records_after_sentinel "$template")"
            while IFS= read -r record; do
                [[ -n "$record" ]] || continue
                assert_contains "$context" "$record" \
                    "log gate [$kind] the context carries the record '${record}'"
            done <<< "$trailing_records"

            context_records="$(_count_lines "$context")"
            if (( context_records <= MAX_CONTEXT_RECORDS )); then
                pass "log gate [$kind] the context holds no more than ${MAX_CONTEXT_RECORDS} records"
            else
                fail "log gate [$kind] the context holds no more than ${MAX_CONTEXT_RECORDS} records" \
                    "the context holds ${context_records} records"
            fi
            ;;
        mod_absent_long_tail)
            # FR-007: "When no record matched, the context MUST be the
            # trailing RECORDS of the normalised stream, capped at 30
            # records." This fixture carries MORE trailing records than the
            # cap, so both halves of the rule are observable: the context
            # holds the LAST 30 of them, and it holds NONE of the ones
            # before them.
            context="$(_report_context "$RUN_STDERR")"

            if [[ -n "$context" ]]; then
                pass "log gate [$kind] the report carries a Context section"
            else
                fail "log gate [$kind] the report carries a Context section" \
                    "no context section in the failure report"
            fi

            window="$(_trailing_window "$template")"
            while IFS= read -r record; do
                [[ -n "$record" ]] || continue
                assert_contains "$context" "$record" \
                    "log gate [$kind] the context carries the record '${record}'"
            done <<< "$window"

            context_records="$(_count_lines "$context")"
            if (( context_records <= MAX_CONTEXT_RECORDS )); then
                pass "log gate [$kind] the context holds no more than ${MAX_CONTEXT_RECORDS} records"
            else
                fail "log gate [$kind] the context holds no more than ${MAX_CONTEXT_RECORDS} records" \
                    "the context holds ${context_records} records"
            fi

            omitted="$(_records_before_window "$template")"
            while IFS= read -r record; do
                [[ -n "$record" ]] || continue
                assert_not_contains "$context" "$record" \
                    "log gate [$kind] the context omits the record '${record}' that the cap excludes"
            done <<< "$omitted"
            ;;
    esac

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
# The sentinel-freshness property
#
# FR-002: the script compares the run sentinel's timestamp against the
# launch timestamp T0 AS UTC, and ignores a sentinel that is older. The
# sentinel carries NO offset, so the comparison MUST parse it as UTC
# explicitly: parsing it as local time shifts the result by the local UTC
# offset, and on a host west of UTC that makes every sentinel look hours in
# the FUTURE — the staleness rule would then never fire.
#
# The property is asserted over a TABLE of sentinel timestamps, each with the
# verdict the script must reach. The comparison is the script's own.
# ---------------------------------------------------------------------------

_sentinel_verdict() {
    local sentinel="$1"
    local t0="$2"
    local epoch=""

    epoch="$(date -u -d "${sentinel}Z" +%s 2>/dev/null || printf '')"
    if [[ -z "$epoch" ]]; then
        printf 'unparseable'
    elif (( epoch < t0 )); then
        printf 'stale'
    else
        printf 'fresh'
    fi
}

_sentinel_t0="$(date +%s)"

# The table: <label>|<sentinel>|<expected verdict>
while IFS='|' read -r _s_label _s_ts _s_expected; do
    [[ -n "${_s_label:-}" ]] || continue
    _s_actual="$(_sentinel_verdict "$_s_ts" "$_sentinel_t0")"
    if [[ "$_s_actual" == "$_s_expected" ]]; then
        pass "sentinel freshness: $_s_label"
    else
        fail "sentinel freshness: $_s_label" \
            "expected '$_s_expected', got '$_s_actual' for the sentinel '$_s_ts' (T0=${_sentinel_t0}, local offset $(date '+%z'))"
    fi
done << SENTINEL_TABLE
a sentinel stamped now is fresh|$(date -u '+%Y-%m-%dT%H:%M:%S')|fresh
a sentinel stamped an hour ago is stale|$(date -u -d '-1 hour' '+%Y-%m-%dT%H:%M:%S')|stale
a sentinel stamped an hour ahead is fresh|$(date -u -d '+1 hour' '+%Y-%m-%dT%H:%M:%S')|fresh
a malformed sentinel is reported, not treated as stale|not-a-timestamp|unparseable
SENTINEL_TABLE

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

# ===========================================================================
# T070 — the crash-file context comes from the NORMALISED record stream
#
# FR-007's "Log context (MUST)" states two things about the context the
# script reports:
#
#   * it MUST be the matched failure record TOGETHER WITH ITS CONTINUATION
#     LINES, and a fixed `tail -N` window MUST NOT be used — a .NET stack
#     trace is routinely 20-40 lines long, so such a window can consist
#     entirely of stack frames while omitting the record that names the
#     failure;
#   * it MUST be emitted from the NORMALISED record stream (FR-003), so the
#     trailing carriage return is not rendered as control noise.
#
# The crash file is a COPY of the SMAPI log, and SMAPI terminates every
# record with CRLF — so the crash file is CR-terminated on EVERY line. The
# fixture therefore has exactly the shape the two rules are about: a
# mod-scoped ERROR record followed by a long stack trace, preceded by other
# records, all of them CR-terminated.
#
# THE MECHANISM
# -------------
# The pre-flight deletes a leftover `SMAPI-crash.txt` (FR-012 item 6), so a
# crash file written BEFORE the run is gone by the time the script looks for
# it. The script re-checks `<dirname $smapi_log>/SMAPI-crash.txt` AFTER the
# readiness wait, so the fixture has to appear AFTER the pre-flight and
# BEFORE that check. The window between the two is the display, build, lock,
# conflict-check and log-baseline phases — seconds long — so a watcher that
# fires on the run log's "phase pre-flight finished" marker has a margin
# that is not a race.
#
# The crash case gets its OWN log directory, so its crash file can never be
# confused with the log of another case in this file.
# ---------------------------------------------------------------------------

# The message of the mod-scoped ERROR record the crash fixture carries. It
# is the record the reported context must name: with a fixed 30-line window
# the context is 30 stack frames and the record is not in it at all.
readonly CRASH_FAILURE_MESSAGE="NullReferenceException: Object reference not set to an instance of an object."

# The number of continuation lines (the stack trace) that follow the failure
# record. The crash fixture therefore holds 4 + 40 = 44 lines, and the last
# 30 of them are stack frames ONLY.
readonly CRASH_STACK_DEPTH=40

# The run-log line the script MUST emit for the log inode it recorded BEFORE
# the stale log was removed. The script emits it today; the text is part of
# the contract and MUST NOT change.
readonly INODE_BASELINE_PREFIX="stale SMAPI log baseline inode: "

# The run-log line the script MUST emit for the log inode it observed AFTER
# the log was recreated. The script does NOT emit it today — it is the
# missing half of FR-002's "record the log file's inode BEFORE the stale log
# is removed and verify it changed once the log is recreated".
readonly INODE_RECREATED_PREFIX="recreated SMAPI log inode: "

# _make_crash_fixture <dest> — a crash file: a COPY of the SMAPI log that
# carries a mod-scoped ERROR record followed by a long stack trace, with
# every line terminated by CRLF (SMAPI terminates every record with CRLF).
#
# The harness has no builder for the crash file, so it is built here. The
# record shape itself comes from the harness's own `_log_line`, so the
# fixture is byte-compatible with the log fixtures.
_make_crash_fixture() {
    local dest="${1-}"
    local i

    if [[ -z "$dest" ]]; then
        printf '_make_crash_fixture: a destination is required\n' >&2
        return 1
    fi

    {
        _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
        _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
        _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
        _log_line "12:00:00" "ERROR" "$HARNESS_MOD_NAME" "$CRASH_FAILURE_MESSAGE"
        for ((i = 1; i <= CRASH_STACK_DEPTH; i++)); do
            printf '   at StardewValley.Game1.StackFrame%02d()\n' "$i"
        done
    } > "$dest"

    # SMAPI terminates every record with CRLF.
    sed -i 's/$/\r/' -- "$dest"
    return 0
}

# _crash_watcher <runs-dir> <source> <destination> [marker...]
#
# Waits until a run log in <runs-dir> carries one of the markers and then
# copies <source> to <destination>. It is the mechanism that puts a crash
# file in place AFTER the pre-flight has deleted any leftover one and BEFORE
# the script's post-readiness crash check.
#
# Every marker is emitted AFTER the pre-flight's crash-marker removal — the
# run log's "phase pre-flight finished" line and the display phase's "phase
# display starting" line are both safely after it.
#
# The runs directory is emptied before the run, so the only log in it is the
# current run's.
_crash_watcher() {
    local runs_dir="${1-}"
    local source="${2-}"
    local destination="${3-}"
    shift 3
    local deadline
    local log=""
    local marker
    local -a marker_args=()

    for marker in "$@"; do
        [[ -n "$marker" ]] || continue
        marker_args+=(-e "$marker")
    done

    deadline=$(( $(date +%s) + 90 ))
    while (( $(date +%s) < deadline )); do
        for log in "${runs_dir}"/*.log; do
            [[ -f "$log" ]] || continue
            if grep -qF "${marker_args[@]}" -- "$log" 2>/dev/null; then
                cp -f -- "$source" "$destination" 2>/dev/null || true
                return 0
            fi
        done
        sleep 0.05
    done
    return 1
}

# ---------------------------------------------------------------------------
# The crash case
# ---------------------------------------------------------------------------

CRASH_LOG_DIR="${FIXTURE_ROOT}/crash-live"
CRASH_LOG_PATH="${CRASH_LOG_DIR}/SMAPI-latest.txt"
CRASH_FILE="${CRASH_LOG_DIR}/SMAPI-crash.txt"
CRASH_TEMPLATE="${TEMPLATE_DIR}/crash_fixture.txt"
CRASH_LOG_TEMPLATE="${TEMPLATE_DIR}/crash_log.txt"

mkdir -p -- "$CRASH_LOG_DIR"
rm -f -- "$CRASH_LOG_PATH" "$CRASH_FILE"
_make_crash_fixture "$CRASH_TEMPLATE"

# The fixture is CRLF: the shape that makes the two FR-007 rules meaningful.
crash_total_lines="$(wc -l < "$CRASH_TEMPLATE" | tr -d '[:space:]')"
crash_cr_lines="$(grep -c $'\r$' "$CRASH_TEMPLATE" || true)"
assert_eq "$crash_total_lines" "$crash_cr_lines" \
    "crash context: every line of the crash fixture is CR-terminated"
assert_eq "44" "$crash_total_lines" \
    "crash context: the crash fixture holds the failure record, its 40 stack frames and the records before them"

# The watcher must find the CURRENT run's log, so the runs directory — which
# holds the logs of the earlier cases in this file — is emptied first.
rm -rf -- "${HARNESS_XDG_STATE_HOME}/living-roots-smoke/runs"

_crash_watcher "${HARNESS_XDG_STATE_HOME}/living-roots-smoke/runs" \
    "$CRASH_TEMPLATE" "$CRASH_FILE" \
    "phase pre-flight finished" "phase display starting" &
crash_watcher_pid=$!

# The log the launch produces is a clean one: the mod loads, so the ONLY
# source of failure in this run is the crash file.
make_log_fixture "$CRASH_LOG_TEMPLATE" mod_loaded
_stage_and_run "$CRASH_LOG_TEMPLATE" "$CRASH_LOG_PATH"

# The watcher fires during the build phase, long before the crash check; the
# wait only makes sure it is finished before the fixture tree is inspected.
wait "$crash_watcher_pid" 2>/dev/null || true

crash_output="$(_run_output)"
crash_report="$RUN_STDERR"
crash_context="$(_report_context "$crash_report")"

assert_file_exists "$CRASH_FILE" \
    "crash context: the crash file was in place when the script looked for it"

# The crash-override rule: a crash file present after the readiness wait
# belongs to this run and FORCES a failure.
assert_exit 5 "crash context: a crash file present after the readiness wait exits 5"
assert_not_contains "$crash_output" "$SUCCESS_LINE" \
    "crash context: the run never claims success"
assert_contains "$crash_report" "$CRASH_FILE" \
    "crash context: the report carries the crash file's path"
assert_contains "$crash_report" "Context:" \
    "crash context: the report carries a context section"

# FR-007: the context MUST be the matched failure record together with its
# continuation lines. A fixed `tail -N` window over the crash file holds
# stack frames only and squeezes the record that names the failure out.
assert_contains "$crash_context" "$CRASH_FAILURE_MESSAGE" \
    "crash context: the context carries the matched failure record's message"

# FR-007: the context MUST be emitted from the normalised record stream, so
# the trailing carriage return is not rendered as control noise.
assert_not_contains "$crash_context" $'\r' \
    "crash context: the context carries no carriage return"
assert_not_contains "$crash_report" $'\r' \
    "crash context: the report carries no carriage return"

# ===========================================================================
# T061 — the log-freshness inode guard
#
# FR-002 "Log freshness (MUST)": the script MUST record the log file's
# inode (`stat -c %i`) BEFORE the stale log is removed and verify it changed
# once the log is recreated — the order of these two steps is normative.
#
# The inode COMPARISON is what detects SMAPI's `player-N` fallback: SMAPI
# writes to `SMAPI-latest.player-{n}.txt` when the default path still holds
# the stale log, and in that case the UNCHANGED inode is the only signal
# that the purge failed. Today the baseline inode is RECORDED (and logged)
# but never COMPARED.
#
# THE CONTRACT — the exact run-log lines the assertions below require:
#
#   "stale SMAPI log baseline inode: <N>"   the inode recorded BEFORE the
#                                           removal (the script emits this
#                                           today; the text MUST NOT change)
#   "recreated SMAPI log inode: <N>"        the inode observed AFTER the log
#                                           was recreated (the script does
#                                           NOT emit this today)
#
# Both lines are written into the per-run diagnostic log; the script may add
# a comparison line of its own, but the two lines above are the contract.
# ===========================================================================

# _run_log_path — the path of the run log the last run wrote, taken from the
# script's own stdout summary ("run-log: <path>"). The summary is printed
# both for a successful run and for a failing one, so it names the run's
# log in every case.
_run_log_path() {
    local line

    while IFS= read -r line || [[ -n "$line" ]]; do
        if [[ "$line" == "run-log: "* ]]; then
            printf '%s' "${line#run-log: }"
            return 0
        fi
    done <<< "${RUN_STDOUT-}"

    return 0
}

# _run_log_text <path> — the content of a run log (the empty string when
# there is none).
_run_log_text() {
    local path="${1-}"

    [[ -f "$path" ]] || { printf ''; return 0; }
    cat -- "$path" 2>/dev/null || printf ''
    return 0
}

# _inode_from_run_log <run-log-text> <prefix> — the inode the run log
# records for <prefix>, or the empty string when it records none.
#
# The value is the run of DIGITS that follows the prefix, so a line that
# carries a trailing remark after the number ("… inode: 12345 (changed)")
# still yields the inode.
_inode_from_run_log() {
    local text="${1-}"
    local prefix="${2-}"
    local line
    local value=""

    while IFS= read -r line || [[ -n "$line" ]]; do
        if [[ "$line" == *"${prefix}"* ]]; then
            value="${line#*"${prefix}"}"
            if [[ "$value" =~ ^([0-9]+) ]]; then
                printf '%s' "${BASH_REMATCH[1]}"
            fi
            return 0
        fi
    done <<< "$text"

    printf '%s' ""
    return 0
}

# _line_index_of <run-log-text> <substring> — the 1-based index of the first
# line carrying <substring>, or 0 when there is none.
_line_index_of() {
    local text="${1-}"
    local needle="${2-}"
    local line
    local index=0

    while IFS= read -r line || [[ -n "$line" ]]; do
        index=$(( index + 1 ))
        if [[ "$line" == *"${needle}"* ]]; then
            printf '%d' "$index"
            return 0
        fi
    done <<< "$text"

    printf '0'
    return 0
}

# The table:
#
#   <label>|<kind>|<dir mode>|<expected exit>|<forbidden exit>
#
# The two rows are the two halves of the invariant:
#
#   * "replaced" — the stale log IS removed and a fresh log takes its place:
#     the run must record BOTH inodes, and they must DIFFER.
#   * "survived" — the stale log SURVIVES the removal (the log directory is
#     not writable, so the unlink fails): the run must ABORT rather than let
#     the stale log decide the verdict.
#
# The stale log is the harness's `make_stale_log`, whose ONLY occurrence of
# the mod's name is a skip entry: if it were ever allowed to decide the
# verdict, the report would carry SMAPI's skip reason — which is exactly
# what the "survived" row asserts it does NOT.
readonly INODE_GUARD_TABLE=(
    "the stale log is removed and replaced|replaced|0755|0|"
    "the stale log survives the removal|survived|0555|13|0"
)

for row in "${INODE_GUARD_TABLE[@]}"; do
    IFS='|' read -r label kind dir_mode expected_code forbidden_code <<< "$row"

    case_dir="${FIXTURE_ROOT}/inode-${kind}"
    case_log="${case_dir}/SMAPI-latest.txt"
    case_template="${TEMPLATE_DIR}/inode-${kind}.txt"
    case_keepalive="${case_dir}/.stale-inode-keepalive"

    rm -rf -- "$case_dir"
    mkdir -p -- "$case_dir"
    rm -f -- "$case_log"

    make_stale_log "$case_log"

    # Keep the stale log's inode alive: the log is unlinked and a new one is
    # created in its place, and a filesystem that reused the just-freed
    # inode number would make "the inode changed" meaningless.
    rm -f -- "$case_keepalive"
    ln -f -- "$case_log" "$case_keepalive" 2>/dev/null || true

    if [[ "$case_keepalive" -ef "$case_log" ]]; then
        pass "inode guard [$label] the fixture keeps the stale log's inode alive"
    else
        fail "inode guard [$label] the fixture keeps the stale log's inode alive" \
            "could not hard-link ${case_log} to ${case_keepalive}"
    fi

    stale_inode="$(stat -c %i -- "$case_log" 2>/dev/null || printf '')"

    # A read-only log directory makes the unlink fail, so the stale log
    # SURVIVES the removal — the negative half of the invariant. The mode is
    # restored after the run so the fixture tree can still be cleaned up.
    chmod -- "$dir_mode" "$case_dir"

    make_log_fixture "$case_template" mod_loaded
    _stage_and_run "$case_template" "$case_log"

    chmod -- 0755 "$case_dir"

    output="$(_run_output)"
    run_log_path="$(_run_log_path)"
    run_log_text="$(_run_log_text "$run_log_path")"

    baseline_inode="$(_inode_from_run_log "$run_log_text" "$INODE_BASELINE_PREFIX")"
    recreated_inode="$(_inode_from_run_log "$run_log_text" "$INODE_RECREATED_PREFIX")"

    assert_eq "$expected_code" "$RUN_CODE" "inode guard [$label] exits $expected_code"

    if [[ -n "$forbidden_code" ]]; then
        assert_ne "$forbidden_code" "$RUN_CODE" \
            "inode guard [$label] does not exit $forbidden_code"
    fi

    # A failing run must never claim success. A run that is EXPECTED to
    # succeed says so — the assertion does not apply to it.
    if [[ "$expected_code" != "0" ]]; then
        assert_not_contains "$output" "$SUCCESS_LINE" \
            "inode guard [$label] never claims success"
    fi

    # The baseline inode was recorded BEFORE the removal: its value is the
    # inode of the file that was at the log path.
    if [[ -n "$baseline_inode" ]]; then
        pass "inode guard [$label] the run log records the baseline inode"
    else
        fail "inode guard [$label] the run log records the baseline inode" \
            "no '${INODE_BASELINE_PREFIX}<inode>' line in ${run_log_path}"
    fi
    assert_eq "$stale_inode" "$baseline_inode" \
        "inode guard [$label] the recorded baseline inode is the stale log's inode"

    case "$kind" in
    replaced)
        # The stale log is a skip log: had it decided the verdict, the run
        # would have failed with SMAPI's skip reason. It did not — the FRESH
        # log did.
        assert_not_contains "$output" "$SKIP_REASON_TEXT" \
            "inode guard [$label] the stale log did not decide the verdict"

        if [[ -n "$recreated_inode" ]]; then
            pass "inode guard [$label] the run log records the recreated inode"
        else
            fail "inode guard [$label] the run log records the recreated inode" \
                "no '${INODE_RECREATED_PREFIX}<inode>' line in ${run_log_path}"
        fi

        live_inode="$(stat -c %i -- "$case_log" 2>/dev/null || printf '')"
        assert_eq "$live_inode" "$recreated_inode" \
            "inode guard [$label] the recorded recreated inode is the live log's inode"

        if [[ -n "$baseline_inode" && -n "$recreated_inode" && "$baseline_inode" != "$recreated_inode" ]]; then
            pass "inode guard [$label] the recreated inode differs from the baseline inode"
        else
            fail "inode guard [$label] the recreated inode differs from the baseline inode" \
                "the baseline inode is '${baseline_inode}' and the recreated inode is '${recreated_inode}'"
        fi

        baseline_index="$(_line_index_of "$run_log_text" "$INODE_BASELINE_PREFIX")"
        recreated_index="$(_line_index_of "$run_log_text" "$INODE_RECREATED_PREFIX")"
        if (( baseline_index > 0 && recreated_index > baseline_index )); then
            pass "inode guard [$label] the baseline inode is recorded before the recreated inode"
        else
            fail "inode guard [$label] the baseline inode is recorded before the recreated inode" \
                "the baseline line is at ${baseline_index} and the recreated line at ${recreated_index}"
        fi
        ;;
    survived)
        # The stale log is still there: the removal failed, so the inode
        # could not change — and the run must have ABORTED rather than let
        # the stale log decide the verdict.
        assert_file_exists "$case_log" \
            "inode guard [$label] the stale log survived the removal"
        assert_not_contains "$output" "$SKIP_REASON_TEXT" \
            "inode guard [$label] the stale log did not decide the verdict"
        ;;
    esac
done

# ===========================================================================
# T151 — the readiness probes are scoped to the post-sentinel region
#
# FR-002: every signal MUST be evaluated only against the log lines that
# FOLLOW the run sentinel, and the sentinel itself MUST be a prefixed record
# of THIS run. A log that carries a STALE `Log started at` line and a STALE
# `Loaded N mods:` header BEFORE the current run's sentinel must therefore
# neither satisfy phase 1 of the readiness wait nor end phase 2 early — only
# the post-sentinel lines decide the wait.
#
# THE FIXTURE
# -----------
# The stale lines have to be physically BEFORE the current run's sentinel, and
# the harness's own apphost stub REWRITES every `Log started at ... UTC` line
# to the current time — so it cannot produce a stale one. This case therefore
# uses its OWN apphost stub, in its OWN game installation, which copies the
# template VERBATIM and appends a FRESH sentinel at the end.
#
# The stale header names the mod itself, so a probe that let the stale lines
# decide the outcome would report a FALSE PASS (0): the stale block would
# satisfy the mod-resolution wait, and the gate would read the stale block.
#
# THE VERDICT
# -----------
# The current run's sentinel satisfies phase 1. Phase 2 then waits for a
# mod-resolution header that FOLLOWS it — and there is none — so the deadline
# expires and the run reports the TIMEOUT (4). Any other verdict means a
# stale line decided the outcome:
#
#   0   the stale header satisfied the wait and the stale block decided the
#       verdict — the false pass the post-sentinel rule exists to prevent;
#   5   the stale header ended phase 2 early and the gate read a stale block
#       in which the mod is absent;
#   15  the run was reported as having lost its log;
#   17  the stale header ended phase 2 early and the gate found no header of
#       its own to read.
#
# The phase the run reached is observable in the run log: phase 1 records
# "run sentinel observed in the SMAPI log" when the CURRENT run's sentinel
# satisfies the readiness wait, and a phase that timed out reports which
# signal never arrived.
# ===========================================================================

# The sentinel of the PREVIOUS run: a timestamp from the past, so the
# freshness rule (FR-002) rejects it as this run's boundary.
readonly STALE_SENTINEL_TIMESTAMP="2020-01-01T00:00:00 UTC"

# _write_verbatim_apphost <path> — an apphost stub that copies the staged
# template to the log path VERBATIM and appends a FRESH run sentinel.
#
# The harness's own stub rewrites every `Log started at ... UTC` line to the
# current time, so it cannot produce a STALE one. This stub leaves the
# template's own lines untouched and appends the fresh sentinel, which is
# exactly the shape this case needs: the stale sentinel and the stale header
# sit BEFORE the fresh sentinel.
#
# The stub then stays alive, so the script's readiness wait runs to its
# deadline instead of noticing an already-dead game — the run must fail on
# the missing mod-resolution header, not on a vanished process.
_write_verbatim_apphost() {
    local path="${1-}"
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Test stub standing in for SMAPI's native apphost (./StardewModdingAPI).
# It copies the staged log template VERBATIM and appends a fresh run
# sentinel, so the template's own (possibly stale) lines are preserved.
# It launches nothing and touches nothing outside the fixture tree.
set -u

template="${FAKE_SMAPI_LOG_TEMPLATE:-}"
target="${FAKE_SMAPI_LOG_TARGET:-}"

if [[ -n "$template" && -n "$target" && -f "$template" ]]; then
    parent="${target%/*}"
    if [[ -n "$parent" && "$parent" != "$target" ]]; then
        mkdir -p -- "$parent" 2>/dev/null || true
    fi
    now="$(date -u '+%Y-%m-%dT%H:%M:%S' 2>/dev/null || printf '')"
    clock="$(date -u '+%H:%M:%S' 2>/dev/null || printf '00:00:00')"
    cat -- "$template" > "$target" 2>/dev/null || true
    if [[ -n "$now" ]]; then
        printf '[%s TRACE SMAPI] Log started at %s UTC\n' "$clock" "$now" \
            >> "$target" 2>/dev/null || true
    fi
fi

# Stay alive so the script's readiness wait reaches its deadline.
sleep 120

exit 0
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
}

STALE_GAME_DIR="${FIXTURE_ROOT}/stale-game"
STALE_LOG_DIR="${FIXTURE_ROOT}/stale-live"
STALE_TEMPLATE="${TEMPLATE_DIR}/stale_pre_sentinel.txt"
STALE_TARGET="${STALE_LOG_DIR}/SMAPI-latest.txt"

make_fake_game_dir "$STALE_GAME_DIR"
_write_verbatim_apphost "${STALE_GAME_DIR}/StardewModdingAPI"
mkdir -p -- "$STALE_LOG_DIR"
rm -f -- "$STALE_TARGET"

# The fixture: a stale sentinel and a stale mod-resolution block, and
# NOTHING else. The current run's sentinel is appended by the stub.
{
    _log_line "11:59:00" "TRACE" "SMAPI" "Log started at ${STALE_SENTINEL_TIMESTAMP}"
    _log_line "11:59:00" "INFO" "SMAPI" "Loaded 3 mods:"
    _log_line "11:59:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
} > "$STALE_TEMPLATE"

_stage_and_run_in "$STALE_GAME_DIR" "$STALE_TEMPLATE" "$STALE_TARGET"

stale_output="$(_run_output)"
stale_run_log_path="$(_run_log_path)"
stale_run_log_text="$(_run_log_text "$stale_run_log_path")"

# The fixture's own shape: the stale lines come first and the current run's
# sentinel is the last line of the produced log.
stale_sentinel_count="$(grep -c 'Log started at ' "$STALE_TARGET" 2>/dev/null || true)"
assert_eq "2" "$stale_sentinel_count" \
    "stale lines: the produced log carries the stale sentinel and the current run's sentinel"

stale_last_line="$(tail -n 1 -- "$STALE_TARGET" 2>/dev/null || printf '')"
assert_matches "$stale_last_line" 'Log started at [0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2} UTC' \
    "stale lines: the current run's sentinel is the last line of the produced log"

# The verdict: the stale lines decided NOTHING.
assert_exit 4 "stale lines: the run reports the timeout"
assert_ne 0 "$RUN_CODE" "stale lines: the run never reports success"
assert_ne 5 "$RUN_CODE" "stale lines: the run does not report a mod-load failure"
assert_ne 15 "$RUN_CODE" "stale lines: the run does not report a missing log"
assert_ne 17 "$RUN_CODE" "stale lines: the run does not report an aborted mod resolution"
assert_not_contains "$stale_output" "$SUCCESS_LINE" \
    "stale lines: the run never claims success"

# Phase 1 was satisfied by the CURRENT run's sentinel — the stale sentinel
# did not stand in for it, and it did not stand in for the run's boundary
# either.
assert_contains "$stale_run_log_text" "run sentinel observed in the SMAPI log" \
    "stale lines: phase 1 was satisfied by the current run's sentinel"

# Phase 2 ran to its deadline: the stale header did not end it early, so the
# failure names the mod-resolution wait.
assert_contains "$stale_output" "resolve any mods" \
    "stale lines: phase 2 timed out waiting for a post-sentinel mod-resolution header"

# ---------------------------------------------------------------------------

cleanup_fixtures
finish_tests
