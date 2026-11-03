#!/usr/bin/env bash
#
# scripts/tests/lib/runner-harness.sh — extends harness.sh with fake Xvfb,
# fake SMAPI log, fake game install, fake milestone save, and a
# run_verify_scenario helper that always isolates HOME/XDG_*.
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/harness.sh"

# Fake Xvfb: a no-op executable placed on PATH.
make_fake_xvfb() {
    local dir="$1"
    mkdir -p "$dir"
    cat > "$dir/Xvfb" <<'EOF'
#!/usr/bin/env bash
exit 0
EOF
    chmod +x "$dir/Xvfb"
}

# Fake game install: reuse the comprehensive implementation from harness.sh.
# runner-harness.sh intentionally does not redefine make_fake_game_dir, so the
# inherited helper (six required assemblies, Mods/, steam_appid.txt, and the
# apphost stub) remains available to runner tests.

# Fake milestone save: an empty save directory.
make_fake_milestone_save() {
    local dir="$1"
    mkdir -p "$dir"
}

# run_verify_scenario: invoke scripts/verify-scenario.sh with isolated env.
run_verify_scenario() {
    local sandbox="$1"
    shift
    HOME="$sandbox/home" XDG_CONFIG_HOME="$sandbox/xdg" XDG_STATE_HOME="$sandbox/state" XDG_RUNTIME_DIR="$sandbox/run" \
        bash scripts/verify-scenario.sh "$@"
}
# ---------------------------------------------------------------------------
# US1 additions (Phase 3). Additive only: existing helpers keep their contract.
# ---------------------------------------------------------------------------

RUNNER_REPO_ROOT="${RUNNER_REPO_ROOT:-$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd -P)}"
export RUNNER_REPO_ROOT

# Per-suite runner sandbox (never the real HOME/XDG roots).
RUNNER_SANDBOX="${HARNESS_SANDBOX}/runner"
mkdir -p -- "$RUNNER_SANDBOX" 2>/dev/null || true

RUN_VERIFY_CODE=0
RUN_VERIFY_STDOUT=""
RUN_VERIFY_STDERR=""

# run_verify [args...] — run scripts/verify-scenario.sh with an isolated
# environment and capture stdout/stderr/exit code. The caller's exported
# STARDEW_GAME_PATH / BUILDDEPS_TOKEN / RUNNER_* are passed through.
run_verify() {
    local out_file err_file rc=0
    out_file="$(mktemp "${TMPDIR:-/tmp}/runner-stdout.XXXXXXXXXX")" || out_file=""
    err_file="$(mktemp "${TMPDIR:-/tmp}/runner-stderr.XXXXXXXXXX")" || err_file=""
    if [[ -z "$out_file" || -z "$err_file" ]]; then
        RUN_VERIFY_CODE=1
        RUN_VERIFY_STDOUT=""
        RUN_VERIFY_STDERR="harness: could not create capture files"
        return 0
    fi
    local script="${RUNNER_REPO_ROOT}/scripts/verify-scenario.sh"
    if [[ ! -f "$script" ]]; then
        RUN_VERIFY_CODE=127
        RUN_VERIFY_STDOUT=""
        RUN_VERIFY_STDERR="harness: ${script}: command not found (exit code 127)"
        rm -f -- "$out_file" "$err_file"
        return 0
    fi
    mkdir -p -- "$RUNNER_SANDBOX/home" "$RUNNER_SANDBOX/xdg" \
        "$RUNNER_SANDBOX/state" "$RUNNER_SANDBOX/run" 2>/dev/null || true
    if env \
        HOME="$RUNNER_SANDBOX/home" \
        XDG_CONFIG_HOME="$RUNNER_SANDBOX/xdg" \
        XDG_STATE_HOME="$RUNNER_SANDBOX/state" \
        XDG_RUNTIME_DIR="$RUNNER_SANDBOX/run" \
        RUNNER_QUIET="${RUNNER_QUIET:-1}" \
        bash "$script" "$@" >"$out_file" 2>"$err_file"; then
        rc=0
    else
        rc=$?
    fi
    RUN_VERIFY_CODE="$rc"
    RUN_VERIFY_STDOUT="$(cat -- "$out_file")"
    RUN_VERIFY_STDERR="$(cat -- "$err_file")"
    rm -f -- "$out_file" "$err_file"
    return 0
}

# assert_verify_code <expected> <label>
assert_verify_code() {
    assert_eq "$1" "$RUN_VERIFY_CODE" "${2-verify-scenario exit code}"
}

# make_fake_mods <mods-dir> — prebuilt production + verification mod artifacts.
make_fake_mods() {
    local dir="${1-}"
    mkdir -p -- "$dir/LivingRoots" "$dir/LivingRoots.Verification" || return 1
    printf 'fake LivingRoots assembly\n' > "$dir/LivingRoots/LivingRoots.dll" || return 1
    printf '{"UniqueID":"lfgranja.LivingRoots","Version":"0.4.0"}\n' \
        > "$dir/LivingRoots/manifest.json" || return 1
    printf 'fake verification assembly\n' \
        > "$dir/LivingRoots.Verification/LivingRoots.Verification.dll" || return 1
    printf '{"UniqueID":"lfgranja.LivingRoots.Verification","Version":"0.1.0"}\n' \
        > "$dir/LivingRoots.Verification/manifest.json" || return 1
}

# make_display_xvfb <dir> — Xvfb stub that records its argv and writes a display
# number to fd 3 (the -displayfd contract), then idles until signalled.
make_display_xvfb() {
    local dir="${1-}"
    mkdir -p -- "$dir" || return 1
    cat > "$dir/Xvfb" <<'STUB'
#!/usr/bin/env bash
printf '%s\n' "$*" > "${FAKE_XVFB_ARGS:-/dev/null}"
printf '99\n' >&3
trap 'exit 0' TERM INT
while :; do sleep 0.2; done
STUB
    chmod +x -- "$dir/Xvfb"
}

# make_failing_xvfb <dir> — Xvfb stub that exits 1 without a display number.
make_failing_xvfb() {
    local dir="${1-}"
    mkdir -p -- "$dir" || return 1
    cat > "$dir/Xvfb" <<'STUB'
#!/usr/bin/env bash
printf 'cannot open display\n' >&2
exit 1
STUB
    chmod +x -- "$dir/Xvfb"
}

# write_min_scenario <path> [kind] [playerRole]
# Minimal schema-valid scenario for CLI/preflight tests. The real feature-004
# scenario is used by suites that exercise thresholds.
write_min_scenario() {
    local path="${1-}" kind="${2-positive}" role="${3-host}"
    local expected=''
    case "$kind" in
        negative) expected=',"expectedVerdict":"fail"' ;;
        boundary) expected=',"expectedVerdict":"pass"' ;;
    esac
    local host_guard=''
    if [[ "$role" == "farmhand" ]]; then
        host_guard=',"hostGuard":{"skipLine":"Soil growth modifier skipped: not main player.","forbiddenEvidence":["elapsedTrace","phaseDaysMutation"]}'
    fi
    cat > "$path" <<JSON
{
  "schemaVersion": 2,
  "scenarioId": "runner-self-test",
  "featureSlug": "010-headless-verification-runner",
  "testMdPath": "specs/005-soil-health-to-crop-growth/test.md",
  "summary": "Minimal runner self-test scenario.",
  "playerRole": "$role",
  "minimumGameVersion": "1.6.14",
  "environment": {
    "gameVersion": "1.6.14",
    "smapiVersion": "4.4.0",
    "modVersion": "0.4.0",
    "apiVersion": "1.0.0",
    "saveId": "runner-self-test",
    "config": {"saveMode": "save", "headless": true, "skipBuild": true}
  },
  "milestoneSave": "runner-self-test",
  "initCommands": [],
  "steps": [{"action": "observe", "target": "farm"}],
  "thresholds": [{"signal":"smapiLog","predicate":{"op":"cmp","left":"isMainPlayer","operator":"eq","right":true},"description":"host"}],
  "expectedSignals": ["soilHealthStateDelta", "cropPhaseDelta", "smapiLog"],
  "rngSeed": 123456789,
  "time": "2026-10-27T12:00:00Z",
  "determinism": {"idempotent": true, "declaredSources": []},
  "scenarioKind": "$kind"$expected$host_guard
}
JSON
}

# make_fake_save <save-dir> <saveId> <soil-json> [compost-json]
make_fake_save() {
    local dir="${1-}" save_id="${2-}" soil="${3-}" compost="${4-}"
    [[ -n "$soil" ]] || soil='{}'
    [[ -n "$compost" ]] || compost='{}'
    mkdir -p -- "$dir/modData" || return 1
    printf '%s\n' "$soil" > "$dir/modData/soil_health_data_${save_id}.json" || return 1
    printf '%s\n' "$compost" > "$dir/modData/composting_bins_${save_id}.json" || return 1
    printf '<SaveGame><name>%s</name></SaveGame>\n' "$save_id" > "$dir/${save_id}" || return 1
}

# make_fake_crop_dump <dir> <dayKey> [done]
make_fake_crop_dump() {
    local dir="${1-}" day="${2-runner-self-test}" is_done="${3-true}"
    mkdir -p -- "$dir" || return 1
    cat > "$dir/crop_phase_dump_${day}.json" <<JSON
{
  "schemaVersion": 1,
  "dayKey": "${day}",
  "capturedAtUtc": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "isMainPlayer": true,
  "locationsProcessed": 1,
  "elapsedMilliseconds": 1,
  "done": ${is_done},
  "crops": [
    {
      "location": "Farm",
      "tileX": -10,
      "tileY": 5,
      "tileKey": "-10,5",
      "seedId": "472",
      "isForageCrop": false,
      "indexOfHarvest": 0,
      "currentPhase": 0,
      "dayOfCurrentPhase": 0,
      "phaseDays": [1, 1, 1, 1, 1],
      "basePhaseDays": [1, 1, 1, 1, 1],
      "fullyGrown": false,
      "dead": false,
      "soilHealth": 20,
      "healthTier": "poor",
      "soilHealthState": "measured",
      "isSentinelPhase": false
    }
  ]
}
JSON
    printf 'done\n' > "$dir/crop_phase_dump_${day}.json.done" || return 1
}

# json_get <file> <jq-filter> — prints the value, or empty when absent.
json_get() {
    jq -r "${2-.}" -- "$1" 2>/dev/null || true
}

# assert_json_eq <file> <jq-filter> <expected> <label>
assert_json_eq() {
    local file="$1" filter="$2" expected="$3" label="$4" actual
    if [[ ! -f "$file" ]]; then
        fail "$label" "missing file: $file"
        return 0
    fi
    actual="$(json_get "$file" "$filter")"
    assert_eq "$expected" "$actual" "$label"
}

# make_gate3_doc <path> <state> — a document with exactly one GATE3 block.
make_gate3_doc() {
    local path="${1-}" state="${2-partial}"
    cat > "$path" <<DOC
# Document

Prose before the block.

<!-- GATE3:BEGIN -->
status: ${state}
evidence: none
reviewed: never
<!-- GATE3:END -->

Prose after the block.
DOC
}

# make_fake_smapi_log <dest> [kind] — alias kept for US1 suites.
make_fake_smapi_log() {
    make_fake_smaapi_log "$@"
}

# require_function <name> — record a failed expectation when a library function
# the suite is about to exercise does not exist yet. Keeps RED-gate output an
# assertion failure rather than a shell "command not found" abort.
require_function() {
    if declare -F "$1" >/dev/null 2>&1; then
        return 0
    fi
    fail "library function $1 is defined" "not defined (scaffold or missing implementation)"
    return 1
}

# call_runner_func <function> [args...] — invoke a runner library function in a
# way that is safe under `set -e` before the implementation exists. When the
# function is missing the suite records an assertion failure (RED-gate output
# must be an assertion failure, not `command not found`), leaves RUNNER_FN_CODE
# at 127, and returns 0 so the rest of the file still runs.
RUNNER_FN_CODE=0
RUNNER_FN_OUT=""
call_runner_func() {
    local fn="${1-}"
    shift || true
    if ! declare -F "$fn" >/dev/null 2>&1; then
        fail "library function $fn is defined" "not defined (scaffold or missing implementation)"
        RUNNER_FN_CODE=127
        RUNNER_FN_OUT=""
        return 0
    fi
    local tmpout rc=0
    tmpout="$(mktemp "${TMPDIR:-/tmp}/rfn.XXXXXXXXXX")" || tmpout=""
    if [[ -z "$tmpout" ]]; then
        RUNNER_FN_CODE=1
        RUNNER_FN_OUT="harness: could not create capture file"
        return 0
    fi
    rc=0
    "$fn" "$@" >"$tmpout" 2>&1 || rc=$?
    RUNNER_FN_CODE=$rc
    RUNNER_FN_OUT="$(cat -- "$tmpout" 2>/dev/null)"
    rm -f -- "$tmpout"
    return 0
}

# assert_fn_code <expected> <label> — assert the code recorded by call_runner_func.
assert_fn_code() {
    assert_eq "${1-}" "$RUNNER_FN_CODE" "${2-call_runner_func exit code}"
}

# Source all runner libs so US1 suites can call their functions directly.
for _lr_lib in "$RUNNER_REPO_ROOT"/scripts/lib/*.sh; do
    # shellcheck disable=SC1090
    source "$_lr_lib" 2>/dev/null || true
done
unset _lr_lib

# assert_contains_file <file> <needle> <label>
assert_contains_file() {
    local file="$1" needle="$2" label="$3"
    if [[ ! -f "$file" ]]; then
        fail "$label" "missing file: $file"
        return 0
    fi
    if grep -Fq -- "$needle" "$file"; then
        pass "$label"
    else
        fail "$label" "expected $(printf '%q' "$needle") in $file"
    fi
}

# make_fake_smaapi_log <dest> [kind]
#
# Explicit fake SMAPI log helper for runner tests. This wraps the inherited
# `make_log_fixture` from harness.sh so Phase 2/US1 suites depend on a
# documented runner-harness entry point rather than an undocumented inherited
# function. Supported runner-specific kinds are:
#   mod_loaded          (default) inherited loaded-mod log
#   mod_skipped         inherited skipped-mod log
#   elapsed             loaded mod plus the elapsed-ms trace line
#   host_guard_skipped  loaded mod plus the not-main-player skip line
# Any other kind is forwarded to `make_log_fixture`.
make_fake_smaapi_log() {
    local dest="${1-}"
    local kind="${2-mod_loaded}"
    if [[ -z "$dest" ]]; then
        printf 'make_fake_smaapi_log: a destination is required\n' >&2
        return 1
    fi
    mkdir -p -- "$(dirname -- "$dest")" || return 1
    case "$kind" in
        mod_loaded)
            make_log_fixture "$dest" mod_loaded
            ;;
        mod_skipped)
            make_log_fixture "$dest" mod_skipped
            ;;
        elapsed)
            {
                _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
                _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
                _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
                _log_line "12:00:00" "TRACE" "LivingRoots" "Soil growth modifier elapsed: 1 ms (location=Farm)."
            } > "$dest"
            ;;
        host_guard_skipped)
            {
                _log_line "12:00:00" "TRACE" "SMAPI" "$HARNESS_RUN_SENTINEL"
                _log_line "12:00:00" "INFO" "SMAPI" "Loaded 3 mods:"
                _log_line "12:00:00" "INFO" "SMAPI" "   ${HARNESS_MOD_NAME} ${HARNESS_MOD_VERSION} by ${HARNESS_MOD_AUTHOR}"
                _log_line "12:00:00" "TRACE" "LivingRoots" "Soil growth modifier skipped: not main player."
            } > "$dest"
            ;;
        *)
            make_log_fixture "$dest" "$kind"
            ;;
    esac
}
